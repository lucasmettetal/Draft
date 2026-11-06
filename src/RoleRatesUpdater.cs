// Calcule les rôles joués par chaque champion sur des parties récentes, via l'API officielle de Riot.
// Ne garde que des compteurs champion/rôle : aucun nom ni identifiant de joueur n'est écrit.
// La clé reste en mémoire le temps du calcul : elle n'est ni enregistrée, ni journalisée.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace DraftMid
{
    public class RiotReply
    {
        public int Status;
        public string Body = "";
        public int RetryAfterSeconds;
    }

    public interface IRiotFetcher
    {
        Task<RiotReply> GetAsync(string url);
    }

    public class RiotWebFetcher : IRiotFetcher
    {
        const int TimeoutMs = 15000;
        readonly string key;

        public RiotWebFetcher(string key)
        {
            this.key = key;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public async Task<RiotReply> GetAsync(string url)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);
            r.Method = "GET";
            r.UserAgent = "DRAFT";
            r.Headers["X-Riot-Token"] = key;
            using (new System.Threading.Timer(_ => r.Abort(), null, TimeoutMs, Timeout.Infinite))
            {
                try
                {
                    using (HttpWebResponse w = (HttpWebResponse)await r.GetResponseAsync())
                    using (StreamReader rd = new StreamReader(w.GetResponseStream(), Encoding.UTF8))
                        return new RiotReply { Status = (int)w.StatusCode, Body = await rd.ReadToEndAsync() };
                }
                catch (WebException e)
                {
                    HttpWebResponse h = e.Response as HttpWebResponse;
                    if (h == null) return new RiotReply { Status = 0 };
                    int retry;
                    int.TryParse(h.Headers["Retry-After"], out retry);
                    return new RiotReply { Status = (int)h.StatusCode, RetryAfterSeconds = retry };
                }
            }
        }
    }

    public class RiotRegion
    {
        public string Label, Platform, Routing;
        public override string ToString() { return Label; }

        public static readonly RiotRegion[] All = {
            new RiotRegion { Label = "Europe Ouest (EUW)", Platform = "euw1", Routing = "europe" },
            new RiotRegion { Label = "Europe Nord et Est (EUNE)", Platform = "eun1", Routing = "europe" },
            new RiotRegion { Label = "Turquie (TR)", Platform = "tr1", Routing = "europe" },
            new RiotRegion { Label = "Amérique du Nord (NA)", Platform = "na1", Routing = "americas" },
            new RiotRegion { Label = "Corée (KR)", Platform = "kr", Routing = "asia" }
        };
    }

    public class RoleUpdateResult
    {
        public int Games, Champions;
        public bool Saved, Cancelled, UpToDate;
        public string Error = "";
    }

    // Socle commun : limite de requêtes, refus de Riot, écriture du résultat.
    public abstract class RiotCrawler
    {
        protected static readonly string[] Tiers = { "SILVER", "GOLD", "PLATINUM", "EMERALD" };
        protected const int QueueSoloDuo = 420, MinDurationSec = 300;
        const int MaxFailures = 5;

        readonly IRiotFetcher fetcher;
        readonly RiotRegion region;
        readonly string outPath;
        int failures, sinceCheckpoint;
        const int CheckpointEvery = 25, MaxSeen = 40000;

        // Parties déjà lues, pour ne jamais les redemander. Ce sont des numéros de partie, sans joueur.
        protected readonly HashSet<string> Seen = new HashSet<string>();
        protected readonly List<string> Regions = new List<string>();
        // Avec ce chemin, un calcul reprend où il s'est arrêté et s'ajoute aux précédents. Fichier local, jamais publié.
        public string StatePath;
        // Rien à faire : on ne dépense aucune requête.
        protected bool UpToDate;

        public int Requests { get; private set; }
        // Limite d'une clé personnelle : 100 requêtes par 2 minutes.
        public int DelayMs = 1300;
        // Un calcul interrompu trop tôt ne remplace pas de meilleures données.
        public int MinGamesToSave = 200;
        public Action<int, int> Progress;
        public Func<int, CancellationToken, Task> Wait = (ms, ct) => Task.Delay(ms, ct);

        protected RiotCrawler(IRiotFetcher fetcher, RiotRegion region, string outPath)
        {
            this.fetcher = fetcher;
            this.region = region;
            this.outPath = outPath;
        }

        protected string PlatformUrl { get { return "https://" + region.Platform + ".api.riotgames.com"; } }
        protected string RoutingUrl { get { return "https://" + region.Routing + ".api.riotgames.com"; } }

        protected class StopRequest : Exception { public StopRequest(string text) : base(text) { } }

        protected static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 }; }
        public static string ChampionKey(string name) { return Regex.Replace((name ?? "").ToLowerInvariant(), "[^a-z0-9]", ""); }

        protected async Task<List<string>> LoadPlayers(CancellationToken ct)
        {
            List<string> players = new List<string>();
            foreach (string tier in Tiers)
            {
                string body = await Get(PlatformUrl + "/lol/league-exp/v4/entries/RANKED_SOLO_5x5/" + tier + "/II?page=1", "liste de joueurs", ct);
                if (body == null) continue;
                foreach (object o in (IEnumerable)Json().DeserializeObject(body))
                {
                    Dictionary<string, object> entry = o as Dictionary<string, object>;
                    object id;
                    if (entry != null && entry.TryGetValue("puuid", out id) && id is string) players.Add((string)id);
                }
            }
            if (players.Count == 0) throw new StopRequest("Aucun joueur lu : le format de réponse de Riot a peut-être changé.");
            return players;
        }

        protected virtual void RestoreState(string state, string previousOutput) { }
        protected virtual string StateJson() { return "{}"; }

        // Numéro court du patch, pris dans la partie elle-même. Sans cette information, on ne l'invente pas.
        protected static string PatchOf(Dictionary<string, object> info)
        {
            object version;
            if (!info.TryGetValue("gameVersion", out version)) return "?";
            Match m = Regex.Match(version as string ?? "", @"^(\d+)\.(\d+)");
            return m.Success ? m.Groups[1].Value + "." + m.Groups[2].Value : "?";
        }

        protected static long PatchOrder(string patch)
        {
            Match m = Regex.Match(patch ?? "", @"^(\d+)\.(\d+)");
            return m.Success ? long.Parse(m.Groups[1].Value) * 100 + long.Parse(m.Groups[2].Value) : -1;
        }

        protected static string Today { get { return DateTime.UtcNow.ToString("yyyy-MM-dd"); } }
        protected static bool RecentDate(string date, int days)
        {
            DateTime when;
            return DateTime.TryParse(date, out when) && (DateTime.UtcNow - when).TotalDays < days;
        }

        protected static Dictionary<string, object> ParseOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int start = text.IndexOf("= {"), end = text.LastIndexOf('}');
            if (start < 0 || end < start) return null;
            try { return Json().Deserialize<Dictionary<string, object>>(text.Substring(start + 2, end - start - 1)); } catch { return null; }
        }

        void LoadSaved()
        {
            if (StatePath == null) return;
            string previous = null;
            if (File.Exists(outPath))
            {
                previous = File.ReadAllText(outPath);
                // Une seule sauvegarde, celle d'avant le premier calcul cumulé.
                if (!File.Exists(outPath + ".bak")) File.Copy(outPath, outPath + ".bak");
            }
            string state = File.Exists(StatePath) ? File.ReadAllText(StatePath) : null;
            try { RestoreState(state, previous); }
            catch (Exception)
            {
                // Fichier abîmé : on le garde de côté et on recompte. Sans la liste des parties lues, garder les compteurs les doublerait.
                if (File.Exists(StatePath)) { File.Delete(StatePath + ".abime"); File.Move(StatePath, StatePath + ".abime"); }
                if (File.Exists(outPath)) File.Copy(outPath, outPath + ".abime", true);
                RestoreState(null, previous);
            }
            if (!Regions.Contains(region.Label)) Regions.Add(region.Label);
        }

        protected void SaveState()
        {
            if (StatePath == null) return;
            try
            {
                string temp = StatePath + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
                File.WriteAllText(temp, StateJson(), new UTF8Encoding(false));
                if (File.Exists(StatePath)) File.Delete(StatePath);
                File.Move(temp, StatePath);
            }
            catch (IOException) { }
        }

        protected void Checkpoint()
        {
            if (++sinceCheckpoint < CheckpointEvery) return;
            sinceCheckpoint = 0;
            SaveState();
        }

        protected List<string> SeenList() { return Seen.Skip(Math.Max(0, Seen.Count - MaxSeen)).ToList(); }

        protected abstract int GamesCounted { get; }
        protected abstract int ChampionsCounted { get; }
        protected abstract string Serialize();
        protected abstract Task Crawl(CancellationToken ct);

        public void Save()
        {
            string temp = outPath + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(temp, Serialize(), new UTF8Encoding(false));
            if (File.Exists(outPath)) File.Delete(outPath);
            File.Move(temp, outPath);
        }

        public async Task<RoleUpdateResult> Run(CancellationToken ct)
        {
            RoleUpdateResult result = new RoleUpdateResult();
            try { LoadSaved(); await Crawl(ct); }
            catch (OperationCanceledException) { result.Cancelled = true; }
            catch (StopRequest e) { result.Error = e.Message; }
            catch (Exception) { result.Error = "Réponse de Riot illisible."; }

            SaveState();
            result.Games = GamesCounted;
            result.Champions = ChampionsCounted;
            result.UpToDate = UpToDate;
            if (result.Games >= MinGamesToSave && !UpToDate)
            {
                try { Save(); result.Saved = true; }
                catch (Exception) { result.Error = "Écriture du fichier impossible."; }
            }
            return result;
        }

        protected async Task<string> Get(string url, string what, CancellationToken ct)
        {
            while (true)
            {
                await Wait(DelayMs, ct);
                Requests++;
                RiotReply reply = await fetcher.GetAsync(url);
                if (reply.Status == 200) { failures = 0; return reply.Body; }
                if (reply.Status == 429) { await Wait(1000 * (reply.RetryAfterSeconds > 0 ? reply.RetryAfterSeconds : 120), ct); continue; }
                if (reply.Status == 401 || reply.Status == 403) throw new StopRequest("Clé Riot refusée ou expirée (code " + reply.Status + ").");
                if (reply.Status == 404) return null;
                if (++failures >= MaxFailures) throw new StopRequest("Riot a refusé " + MaxFailures + " requêtes de suite (" + what + ", code " + reply.Status + ").");
                return null;
            }
        }
    }

    public class PatchRoles
    {
        public int Games;
        public string Collected;
        public SortedDictionary<string, int[]> Champions = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
    }

    public class RoleState
    {
        public int Version = 2;
        public List<string> Regions, Seen;
        public SortedDictionary<string, PatchRoles> Patches;
        // Compositions réelles jamais comptées dans les rôles : elles servent à mesurer la détection, sans joueur.
        public List<string[]> Holdout;
        public Dictionary<string, object> Legacy;
    }

    public class RoleRatesUpdater : RiotCrawler
    {
        static readonly string[] Roles = { "TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY" };
        const int MatchesPerPlayer = 5, Days = 14, FreshDays = 3;

        SortedDictionary<string, PatchRoles> patches = new SortedDictionary<string, PatchRoles>(StringComparer.Ordinal);
        Dictionary<string, object> legacy;
        List<string[]> holdout = new List<string[]>();
        const int HoldoutEvery = 5, MaxHoldout = 4000;
        public int HoldoutCount { get { return holdout.Count; } }

        public int Counted { get { return patches.Values.Sum(p => p.Games); } }
        // Parties voulues sur le patch le plus récent : les patchs déjà couverts ne sont pas redemandés.
        public int TargetGames = 1000;

        public RoleRatesUpdater(IRiotFetcher fetcher, RiotRegion region, string outPath) : base(fetcher, region, outPath) { }

        protected override int GamesCounted { get { return Counted; } }
        protected override int ChampionsCounted { get { return patches.Values.SelectMany(p => p.Champions.Keys).Distinct().Count(); } }
        public int GamesOfPatch(string patch) { PatchRoles p; return patches.TryGetValue(patch, out p) ? p.Games : 0; }

        PatchRoles Newest() { return patches.OrderByDescending(p => PatchOrder(p.Key)).Select(p => p.Value).FirstOrDefault(); }
        int NewestGames() { PatchRoles p = Newest(); return p == null ? 0 : p.Games; }

        protected override void RestoreState(string state, string previousOutput)
        {
            patches = new SortedDictionary<string, PatchRoles>(StringComparer.Ordinal);
            legacy = null;
            holdout = new List<string[]>();
            Seen.Clear();
            Regions.Clear();
            if (state != null)
            {
                RoleState saved = Json().Deserialize<RoleState>(state);
                if (saved.Holdout != null) holdout = saved.Holdout;
                if (saved.Patches != null) patches = new SortedDictionary<string, PatchRoles>(saved.Patches, StringComparer.Ordinal);
                if (saved.Seen != null) foreach (string id in saved.Seen) Seen.Add(id);
                if (saved.Regions != null) Regions.AddRange(saved.Regions);
                legacy = saved.Legacy;
            }
            // Ancien fichier, sans patch : il reste une référence, jamais comptée comme du patch actuel.
            Dictionary<string, object> old = ParseOutput(previousOutput);
            if (legacy == null && old != null && old.ContainsKey("legacy")) legacy = old["legacy"] as Dictionary<string, object>;
            if (legacy == null && old != null && !old.ContainsKey("patches") && old.ContainsKey("champions"))
                legacy = new Dictionary<string, object> { { "updated", old.ContainsKey("updated") ? old["updated"] : null }, { "games", old.ContainsKey("games") ? old["games"] : 0 }, { "champions", old["champions"] } };
        }

        protected override string StateJson()
        {
            return Json().Serialize(new RoleState { Regions = Regions, Seen = SeenList(), Patches = patches, Legacy = legacy, Holdout = holdout });
        }

        // Une partie sur cinq, choisie d'après son numéro : le choix ne dépend ni du résultat, ni des champions.
        public static bool IsHoldout(string id) { return id.Sum(c => (int)c) % HoldoutEvery == 0; }

        public void AddHoldout(string json)
        {
            Dictionary<string, object> root;
            try { root = Json().Deserialize<Dictionary<string, object>>(json); } catch { return; }
            object infoObject, duration, players;
            if (root == null || !root.TryGetValue("info", out infoObject)) return;
            Dictionary<string, object> info = infoObject as Dictionary<string, object>;
            if (info == null || !info.TryGetValue("gameDuration", out duration) || !(duration is int) || (int)duration < MinDurationSec) return;
            if (!info.TryGetValue("participants", out players) || !(players is IEnumerable) || holdout.Count >= MaxHoldout) return;
            Dictionary<string, string[]> teams = new Dictionary<string, string[]>();
            foreach (object o in (IEnumerable)players)
            {
                Dictionary<string, object> p = o as Dictionary<string, object>;
                object position, champion, team;
                if (p == null || !p.TryGetValue("teamPosition", out position) || !p.TryGetValue("championName", out champion) || !p.TryGetValue("teamId", out team)) continue;
                int role = Array.IndexOf(Roles, position as string);
                if (role < 0) continue;
                string key = Convert.ToString(team);
                if (!teams.ContainsKey(key)) teams[key] = new string[Roles.Length];
                teams[key][role] = ChampionKey(champion as string);
            }
            foreach (string[] team in teams.Values) if (team.All(c => !string.IsNullOrEmpty(c))) holdout.Add(team);
        }

        public void AddMatch(string json)
        {
            Dictionary<string, object> root;
            try { root = Json().Deserialize<Dictionary<string, object>>(json); } catch { return; }
            object infoObject;
            if (root == null || !root.TryGetValue("info", out infoObject)) return;
            Dictionary<string, object> info = infoObject as Dictionary<string, object>;
            object duration, players;
            if (info == null || !info.TryGetValue("gameDuration", out duration) || !(duration is int) || (int)duration < MinDurationSec) return;
            if (!info.TryGetValue("participants", out players) || !(players is IEnumerable)) return;
            string patch = PatchOf(info);
            PatchRoles bucket;
            if (!patches.TryGetValue(patch, out bucket)) patches[patch] = bucket = new PatchRoles();
            foreach (object o in (IEnumerable)players)
            {
                Dictionary<string, object> p = o as Dictionary<string, object>;
                object position, champion;
                if (p == null || !p.TryGetValue("teamPosition", out position) || !p.TryGetValue("championName", out champion)) continue;
                int role = Array.IndexOf(Roles, position as string);
                string key = ChampionKey(champion as string);
                if (role < 0 || key == "") continue;
                int[] row;
                if (!bucket.Champions.TryGetValue(key, out row)) bucket.Champions[key] = row = new int[Roles.Length];
                row[role]++;
            }
            bucket.Games++;
            bucket.Collected = Today;
        }

        protected override string Serialize()
        {
            SortedDictionary<string, int[]> total = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
            foreach (PatchRoles p in patches.Values)
                foreach (KeyValuePair<string, int[]> c in p.Champions)
                {
                    int[] row;
                    if (!total.TryGetValue(c.Key, out row)) total[c.Key] = row = new int[Roles.Length];
                    for (int i = 0; i < row.Length; i++) row[i] += c.Value[i];
                }
            Dictionary<string, object> file = new Dictionary<string, object>
            {
                { "version", 2 }, { "updated", Today }, { "games", Counted }, { "days", Days }, { "regions", Regions },
                { "champions", total },
                { "patches", patches.ToDictionary(p => p.Key, p => (object)new Dictionary<string, object> { { "games", p.Value.Games }, { "collected", p.Value.Collected }, { "champions", p.Value.Champions } }) }
            };
            if (legacy != null) file["legacy"] = legacy;
            return "// Parties par rôle et par patch : top, jungle, mid, adc, support.\nwindow.ROLE_RATES_LIVE = " + Json().Serialize(file) + ";\n";
        }

        protected override async Task Crawl(CancellationToken ct)
        {
            // Déjà assez de parties, lues il y a peu : aucune requête n'est utile.
            PatchRoles newest = Newest();
            if (StatePath != null && newest != null && newest.Games >= TargetGames && RecentDate(newest.Collected, FreshDays)) { UpToDate = true; return; }
            List<string> players = await LoadPlayers(ct);
            Random random = new Random();
            long since = new DateTimeOffset(DateTime.UtcNow.AddDays(-Days)).ToUnixTimeSeconds();
            foreach (string player in players.OrderBy(_ => random.Next()))
            {
                if (NewestGames() >= TargetGames) break;
                string list = await Get(RoutingUrl + "/lol/match/v5/matches/by-puuid/" + Uri.EscapeDataString(player) + "/ids?queue=" + QueueSoloDuo + "&startTime=" + since + "&start=0&count=" + MatchesPerPlayer, "historique d'un joueur", ct);
                if (list == null) continue;
                foreach (object idObject in (IEnumerable)Json().DeserializeObject(list))
                {
                    string id = idObject as string;
                    if (NewestGames() >= TargetGames) break;
                    if (id == null || Seen.Contains(id)) continue;
                    string match = await Get(RoutingUrl + "/lol/match/v5/matches/" + Uri.EscapeDataString(id), "détail d'une partie", ct);
                    if (match == null) continue;
                    Seen.Add(id);
                    if (StatePath != null && IsHoldout(id)) AddHoldout(match); else AddMatch(match);
                    Checkpoint();
                    if (Progress != null) Progress(NewestGames(), TargetGames);
                }
            }
        }
    }

    public class PickRow
    {
        public int Wins, Games;
        public SortedDictionary<string, int[]> Vs = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
    }

    public class MatchupState
    {
        public int Version = 2, Counted;
        public List<string> Regions, Seen;
        public SortedDictionary<string, SortedDictionary<string, PickRow>> Patches;
    }

    // Bilans du pool contre chaque adversaire direct, au rôle du joueur. L'API ne permet pas de chercher un champion : on part de joueurs
    // au hasard, puis on suit ceux qui jouent un champion du pool, dont l'historique en contient bien plus.
    // Ces joueurs gagnent souvent plus que la moyenne : seul l'écart à la moyenne du champion a un sens.
    public class MatchupStatsUpdater : RiotCrawler
    {
        const int SeedGames = 5, HistoryGames = 20, Days = 30, SeedEvery = 4;
        // Noms de Riot : le profil d'origine, pour une installation qui n'a pas encore repris son profil.
        public static readonly string[] DefaultPool = { "TwistedFate", "Fizz", "KogMaw", "Xerath" };

        class PickStats
        {
            public int Wins, Games;
            public readonly Dictionary<string, int[]> Vs = new Dictionary<string, int[]>();
            public readonly Queue<string> Players = new Queue<string>();
        }

        readonly Dictionary<string, PickStats> pool = new Dictionary<string, PickStats>();
        readonly HashSet<string> knownPlayers = new HashSet<string>();
        SortedDictionary<string, SortedDictionary<string, PickRow>> patches = new SortedDictionary<string, SortedDictionary<string, PickRow>>(StringComparer.Ordinal);
        int counted;

        public int TargetPerPick = 800;
        // Environ 65 minutes avec une clé personnelle.
        public int MaxRequests = 3000;

        readonly string role, position;

        // Rôles du profil et leur nom dans les parties Riot.
        public static string PositionOf(string role)
        {
            switch (role)
            {
                case "top": return "TOP";
                case "jungle": return "JUNGLE";
                case "mid": return "MIDDLE";
                case "adc": return "BOTTOM";
                case "support": return "UTILITY";
                default: return null;
            }
        }

        public MatchupStatsUpdater(IRiotFetcher fetcher, RiotRegion region, string outPath, IEnumerable<string> picks, string role = "mid") : base(fetcher, region, outPath)
        {
            this.role = role;
            position = PositionOf(role) ?? "MIDDLE";
            foreach (string pick in picks) pool[ChampionKey(pick)] = new PickStats();
        }

        protected override int GamesCounted { get { return counted; } }
        protected override int ChampionsCounted { get { return pool.Values.SelectMany(p => p.Vs.Keys).Distinct().Count(); } }
        public int GamesOf(string pick) { return pool[ChampionKey(pick)].Games; }

        string NewestPatch() { return patches.Keys.OrderByDescending(PatchOrder).FirstOrDefault(); }
        int NewestGames(string pick)
        {
            string patch = NewestPatch();
            SortedDictionary<string, PickRow> rows;
            PickRow row;
            return patch != null && patches.TryGetValue(patch, out rows) && rows.TryGetValue(pick, out row) ? row.Games : 0;
        }

        protected override void RestoreState(string state, string previousOutput)
        {
            patches = new SortedDictionary<string, SortedDictionary<string, PickRow>>(StringComparer.Ordinal);
            Seen.Clear();
            Regions.Clear();
            counted = 0;
            foreach (PickStats p in pool.Values) { p.Wins = 0; p.Games = 0; p.Vs.Clear(); }
            if (state == null) return;
            MatchupState saved = Json().Deserialize<MatchupState>(state);
            counted = saved.Counted;
            if (saved.Seen != null) foreach (string id in saved.Seen) Seen.Add(id);
            if (saved.Regions != null) Regions.AddRange(saved.Regions);
            if (saved.Patches == null) return;
            foreach (KeyValuePair<string, SortedDictionary<string, PickRow>> bucket in saved.Patches)
            {
                patches[bucket.Key] = bucket.Value;
                foreach (KeyValuePair<string, PickRow> row in bucket.Value)
                {
                    PickStats stats;
                    if (!pool.TryGetValue(row.Key, out stats)) continue;
                    stats.Wins += row.Value.Wins;
                    stats.Games += row.Value.Games;
                    foreach (KeyValuePair<string, int[]> v in row.Value.Vs)
                    {
                        int[] total;
                        if (!stats.Vs.TryGetValue(v.Key, out total)) stats.Vs[v.Key] = total = new int[2];
                        total[0] += v.Value[0];
                        total[1] += v.Value[1];
                    }
                }
            }
        }

        protected override string StateJson()
        {
            return Json().Serialize(new MatchupState { Regions = Regions, Seen = SeenList(), Patches = patches, Counted = counted });
        }

        // Ne retient que le rôle du joueur : un Fizz joué ailleurs, ou sans adversaire direct connu, n'est pas un matchup.
        public void AddMatch(string json)
        {
            Dictionary<string, object> root;
            try { root = Json().Deserialize<Dictionary<string, object>>(json); } catch { return; }
            object infoObject;
            if (root == null || !root.TryGetValue("info", out infoObject)) return;
            Dictionary<string, object> info = infoObject as Dictionary<string, object>;
            object duration, playersObject;
            if (info == null || !info.TryGetValue("gameDuration", out duration) || !(duration is int) || (int)duration < MinDurationSec) return;
            if (!info.TryGetValue("participants", out playersObject) || !(playersObject is IEnumerable)) return;
            string patch = PatchOf(info);
            List<Dictionary<string, object>> mids = new List<Dictionary<string, object>>();
            foreach (object o in (IEnumerable)playersObject)
            {
                Dictionary<string, object> p = o as Dictionary<string, object>;
                object position;
                if (p != null && p.TryGetValue("teamPosition", out position) && position as string == this.position) mids.Add(p);
            }
            if (mids.Count != 2) return;
            bool useful = false;
            for (int i = 0; i < 2; i++)
            {
                PickStats stats;
                object champion, opponent, win, puuid;
                if (!mids[i].TryGetValue("championName", out champion) || !pool.TryGetValue(ChampionKey(champion as string), out stats)) continue;
                if (!mids[1 - i].TryGetValue("championName", out opponent) || !mids[i].TryGetValue("win", out win) || !(win is bool)) continue;
                string other = ChampionKey(opponent as string);
                if (other == "") continue;
                int[] row;
                if (!stats.Vs.TryGetValue(other, out row)) stats.Vs[other] = row = new int[2];
                if ((bool)win) { row[0]++; stats.Wins++; }
                row[1]++;
                stats.Games++;
                useful = true;
                SortedDictionary<string, PickRow> bucket;
                if (!patches.TryGetValue(patch, out bucket)) patches[patch] = bucket = new SortedDictionary<string, PickRow>(StringComparer.Ordinal);
                PickRow patchRow;
                string pickKey = ChampionKey(champion as string);
                if (!bucket.TryGetValue(pickKey, out patchRow)) bucket[pickKey] = patchRow = new PickRow();
                int[] versus;
                if (!patchRow.Vs.TryGetValue(other, out versus)) patchRow.Vs[other] = versus = new int[2];
                if ((bool)win) { versus[0]++; patchRow.Wins++; }
                versus[1]++;
                patchRow.Games++;
                if (mids[i].TryGetValue("puuid", out puuid) && puuid is string && knownPlayers.Add((string)puuid)) stats.Players.Enqueue((string)puuid);
            }
            if (useful) counted++;
        }

        protected override string Serialize()
        {
            string picks = string.Join(",", pool.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p =>
                "\"" + p.Key + "\":{\"wins\":" + p.Value.Wins + ",\"games\":" + p.Value.Games + ",\"vs\":{"
                + string.Join(",", p.Value.Vs.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => "\"" + v.Key + "\":[" + v.Value[0] + "," + v.Value[1] + "]")) + "}}"));
            return "// Bilans [victoires, parties] de chaque pick contre chaque adversaire direct.\nwindow.MATCHUPS_LIVE = {\"role\":\"" + role + "\",\"updated\":\"" + DateTime.UtcNow.ToString("yyyy-MM-dd")
                + "\",\"days\":" + Days + ",\"games\":" + counted + ",\"regions\":" + Json().Serialize(Regions) + ",\"patches\":" + Json().Serialize(PatchesForPool()) + ",\"pool\":{" + picks + "}};\n";
        }

        // Le format lu par le moteur : wins, games et vs, pour les seuls champions du pool actuel.
        Dictionary<string, object> PatchesForPool()
        {
            return patches.ToDictionary(b => b.Key, b => (object)b.Value.Where(r => pool.ContainsKey(r.Key)).ToDictionary(r => r.Key, r => (object)new Dictionary<string, object> { { "wins", r.Value.Wins }, { "games", r.Value.Games }, { "vs", r.Value.Vs } }));
        }

        async Task ReadHistory(string player, int games, long since, CancellationToken ct)
        {
            string list = await Get(RoutingUrl + "/lol/match/v5/matches/by-puuid/" + Uri.EscapeDataString(player) + "/ids?queue=" + QueueSoloDuo + "&startTime=" + since + "&start=0&count=" + games, "historique d'un joueur", ct);
            if (list == null) return;
            foreach (object idObject in (IEnumerable)Json().DeserializeObject(list))
            {
                string id = idObject as string;
                if (Requests >= MaxRequests) return;
                if (id == null || Seen.Contains(id)) continue;
                string match = await Get(RoutingUrl + "/lol/match/v5/matches/" + Uri.EscapeDataString(id), "détail d'une partie", ct);
                if (match == null) continue;
                    Seen.Add(id);
                AddMatch(match);
                Checkpoint();
                if (Progress != null) Progress(pool.Keys.Sum(p => Math.Min(NewestGames(p), TargetPerPick)), TargetPerPick * pool.Count);
            }
        }

        protected override async Task Crawl(CancellationToken ct)
        {
            Random random = new Random();
            Stack<string> seeds = new Stack<string>((await LoadPlayers(ct)).OrderBy(_ => random.Next()));
            long since = new DateTimeOffset(DateTime.UtcNow.AddDays(-Days)).ToUnixTimeSeconds();
            int tick = 0, seedRequests = Requests;
            while (Requests < MaxRequests)
            {
                // Seul le patch le plus récent compte pour décider de ce qui manque encore.
                List<KeyValuePair<string, PickStats>> wanted = pool.Where(p => NewestGames(p.Key) < TargetPerPick).ToList();
                if (wanted.Count == 0)
                {
                    if (tick == 0 && StatePath != null && Requests == seedRequests) UpToDate = true;
                    return;
                }
                List<PickStats> needy = wanted.Select(p => p.Value).ToList();
                PickStats next = wanted.Where(p => p.Value.Players.Count > 0).OrderBy(p => NewestGames(p.Key)).Select(p => p.Value).FirstOrDefault();
                // Un champion rare a besoin de nouveaux joueurs, même si les autres avancent bien.
                bool wantSeed = needy.Any(p => p.Players.Count == 0) && ++tick % SeedEvery == 0;
                if (next != null && (!wantSeed || seeds.Count == 0)) await ReadHistory(next.Players.Dequeue(), HistoryGames, since, ct);
                else if (seeds.Count > 0) await ReadHistory(seeds.Pop(), SeedGames, since, ct);
                else return;
            }
        }
    }

    // Demande la clé pour ce calcul : elle n'est gardée nulle part.
    public class RiotKeyDialog : Form
    {
        readonly TextBox keyBox = new TextBox { UseSystemPasswordChar = true, Width = 340 };
        readonly ComboBox regionBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };

        public string Key { get { return keyBox.Text.Trim(); } }
        public RiotRegion SelectedRegion { get { return (RiotRegion)regionBox.SelectedItem; } }

        public RiotKeyDialog(Icon icon, Action<string> openLink, string title, string intro)
        {
            Text = title;
            if (icon != null) Icon = icon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            regionBox.Items.AddRange(RiotRegion.All);
            regionBox.SelectedIndex = 0;

            FlowLayoutPanel root = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Padding = new Padding(14) };
            Label introLabel = new Label { AutoSize = true, MaximumSize = new Size(340, 0), Margin = new Padding(0, 0, 0, 10), Text = intro };
            LinkLabel link = new LinkLabel { AutoSize = true, Margin = new Padding(0, 0, 0, 10), Text = "Obtenir une clé sur developer.riotgames.com" };
            link.LinkClicked += (s, e) => openLink("https://developer.riotgames.com/");
            Label keyLabel = new Label { AutoSize = true, Margin = new Padding(0, 0, 0, 3), Text = "Clé Riot (masquée, jamais enregistrée) :" };
            keyBox.Margin = new Padding(0, 0, 0, 10);
            Label regionLabel = new Label { AutoSize = true, Margin = new Padding(0, 0, 0, 3), Text = "Région :" };
            regionBox.Margin = new Padding(0, 0, 0, 14);
            FlowLayoutPanel buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Width = 340, Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            Button start = new Button { Text = "Lancer", DialogResult = DialogResult.OK, AutoSize = true, Enabled = false };
            Button cancel = new Button { Text = "Annuler", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(start);
            buttons.Controls.Add(cancel);
            keyBox.TextChanged += (s, e) => start.Enabled = Key != "";
            root.Controls.AddRange(new Control[] { introLabel, link, keyLabel, keyBox, regionLabel, regionBox, buttons });
            Controls.Add(root);
            AcceptButton = start;
            CancelButton = cancel;
        }
    }

    static class MatchupStatsTests
    {
        static string Game(string pick, bool win, string opponent, string puuid, int duration = 1800)
        {
            return "{\"info\":{\"gameDuration\":" + duration + ",\"participants\":["
                + "{\"puuid\":\"" + puuid + "\",\"riotIdGameName\":\"SecretName\",\"championName\":\"" + pick + "\",\"teamPosition\":\"MIDDLE\",\"teamId\":100,\"win\":" + (win ? "true" : "false") + "},"
                + "{\"puuid\":\"autre\",\"championName\":\"" + opponent + "\",\"teamPosition\":\"MIDDLE\",\"teamId\":200,\"win\":" + (win ? "false" : "true") + "},"
                + "{\"puuid\":\"top\",\"championName\":\"Xerath\",\"teamPosition\":\"TOP\",\"teamId\":100,\"win\":" + (win ? "true" : "false") + "}]}}";
        }

        class Sim : IRiotFetcher
        {
            public Func<string, RiotReply> Answer;
            public readonly List<string> Urls = new List<string>();
            public Task<RiotReply> GetAsync(string url) { Urls.Add(url); return Task.FromResult(Answer(url)); }
        }

        public static void Run(Action<bool, string> t, string dir)
        {
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "matchups-test.js");
            if (File.Exists(path)) File.Delete(path);
            Func<IRiotFetcher, MatchupStatsUpdater> make = f => new MatchupStatsUpdater(f, RiotRegion.All[0], path, new[] { "Fizz", "KogMaw" }) { DelayMs = 0, MinGamesToSave = 1, Wait = (ms, ct) => Task.FromResult(0) };

            MatchupStatsUpdater top = new MatchupStatsUpdater(new Sim(), RiotRegion.All[0], path, new[] { "Xerath", "Fizz" }, "top");
            top.AddMatch(Game("Fizz", true, "Ahri", "p1"));
            t(top.GamesOf("Fizz") == 0, "Matchups : un champion joué à un autre rôle ne compte pas");
            top.AddMatch("{\"info\":{\"gameDuration\":1800,\"participants\":[{\"championName\":\"Fizz\",\"teamPosition\":\"TOP\",\"win\":true},{\"championName\":\"Darius\",\"teamPosition\":\"TOP\",\"win\":false}]}}");
            t(top.GamesOf("Fizz") == 1, "Matchups : le rôle du profil est celui qui compte");
            t(MatchupStatsUpdater.PositionOf("adc") == "BOTTOM" && MatchupStatsUpdater.PositionOf("support") == "UTILITY" && MatchupStatsUpdater.PositionOf("x") == null, "Matchups : les rôles suivent les noms de Riot");
            top.Save();
            t(File.ReadAllText(path).Contains("\"role\":\"top\""), "Matchups : le fichier indique son rôle");
            File.Delete(path);

            // Les bilans gardent leur patch : le moteur peut privilégier le plus récent.
            string mState = Path.Combine(dir, "matchups-state.json"), mPath = Path.Combine(dir, "matchups-resume.js");
            foreach (string old in new[] { mState, mPath, mPath + ".bak" }) if (File.Exists(old)) File.Delete(old);
            string gamePatched = Game("Fizz", true, "Ahri", "p1").Replace("\"gameDuration\":1800", "\"gameDuration\":1800,\"gameVersion\":\"16.21.1\"");
            int detailCalls = 0;
            Func<MatchupStatsUpdater> mMake = () => new MatchupStatsUpdater(new Sim { Answer = url => { if (url.Contains("league-exp")) return new RiotReply { Status = 200, Body = "[{\"puuid\":\"seed\"}]" }; if (url.Contains("/ids")) return new RiotReply { Status = 200, Body = "[\"Q1\",\"Q2\"]" }; detailCalls++; return new RiotReply { Status = 200, Body = gamePatched }; } }, RiotRegion.All[0], mPath, new[] { "Fizz" }) { StatePath = mState, TargetPerPick = 2, MinGamesToSave = 1, DelayMs = 0, Wait = (ms, ct) => Task.FromResult(0) };
            MatchupStatsUpdater mFirst = mMake();
            RoleUpdateResult mDone = Task.Run(() => mFirst.Run(CancellationToken.None)).GetAwaiter().GetResult();
            string mFile = File.ReadAllText(mPath);
            t(mDone.Saved && mFile.Contains("\"patches\":{\"16.21\":{\"fizz\":{\"wins\":2,\"games\":2") && mFile.Contains("\"role\":\"mid\""), "Matchups : chaque bilan garde son patch");
            MatchupStatsUpdater mSecond = mMake();
            int detailBefore = detailCalls;
            RoleUpdateResult mAgain = Task.Run(() => mSecond.Run(CancellationToken.None)).GetAwaiter().GetResult();
            t(detailCalls == detailBefore && mSecond.GamesOf("Fizz") == 2, "Matchups : la reprise relit les bilans sans redemander les parties");

            MatchupStatsUpdater u = make(new Sim());
            u.AddMatch(Game("Fizz", true, "Ahri", "p1"));
            u.AddMatch(Game("Fizz", false, "Ahri", "p1"));
            u.AddMatch(Game("Fizz", true, "Ahri", "p1", 120));
            u.AddMatch(Game("Sett", true, "Vi", "p2"));
            u.AddMatch("pas du json");
            t(u.GamesOf("Fizz") == 2 && u.GamesOf("KogMaw") == 0, "Matchups : seuls les mids du pool comptent, sans remake ni réponse abîmée");
            u.Save();
            string file = File.ReadAllText(path);
            t(file.Contains("\"fizz\":{\"wins\":1,\"games\":2,\"vs\":{\"ahri\":[1,2]}}") && file.Contains("window.MATCHUPS_LIVE"), "Matchups : les bilans sont corrects, au format lu par le moteur");
            t(!file.Contains("p1") && !file.Contains("Secret") && !file.Contains("autre"), "Matchups : aucun nom ni identifiant de joueur dans le fichier");

            // Un joueur croisé par hasard sur Kog'Maw est ensuite suivi : son historique en contient bien plus.
            int historyOfMain = 0;
            Sim sim = new Sim();
            sim.Answer = url =>
            {
                if (url.Contains("league-exp")) return new RiotReply { Status = 200, Body = "[{\"puuid\":\"seed\"}]" };
                if (url.Contains("by-puuid/seed/")) return new RiotReply { Status = 200, Body = "[\"S1\"]" };
                if (url.Contains("by-puuid/main/")) { historyOfMain++; return new RiotReply { Status = 200, Body = "[\"K1\",\"K2\",\"K3\",\"K4\"]" }; }
                if (url.Contains("/ids")) return new RiotReply { Status = 200, Body = "[]" };
                return new RiotReply { Status = 200, Body = Game("KogMaw", !url.EndsWith("K2"), "Lux", "main") };
            };
            MatchupStatsUpdater follow = make(sim);
            follow.TargetPerPick = 3;
            RoleUpdateResult r = Task.Run(() => follow.Run(CancellationToken.None)).GetAwaiter().GetResult();
            t(historyOfMain == 1 && follow.GamesOf("KogMaw") == 5 && r.Saved, "Matchups : un joueur du pool est suivi une seule fois, et son historique est lu");
            t(sim.Urls.All(x => x.StartsWith("https://euw1.api.riotgames.com/") || x.StartsWith("https://europe.api.riotgames.com/")), "Matchups : seuls les serveurs officiels de Riot sont appelés");

            int calls = 0;
            MatchupStatsUpdater capped = make(new Sim { Answer = url => { calls++; return url.Contains("league-exp") ? new RiotReply { Status = 200, Body = "[{\"puuid\":\"a\"},{\"puuid\":\"b\"}]" } : url.Contains("/ids") ? new RiotReply { Status = 200, Body = "[\"M" + calls + "\",\"N" + calls + "\"]" } : new RiotReply { Status = 200, Body = Game("Fizz", true, "Lux", "m" + calls) }; } });
            capped.MaxRequests = 25;
            Task.Run(() => capped.Run(CancellationToken.None)).GetAwaiter().GetResult();
            t(capped.Requests <= 27, "Matchups : le calcul respecte son budget de requêtes (" + capped.Requests + ")");

            string kept = File.ReadAllText(path);
            MatchupStatsUpdater refused = make(new Sim { Answer = url => new RiotReply { Status = 403 } });
            RoleUpdateResult bad = Task.Run(() => refused.Run(CancellationToken.None)).GetAwaiter().GetResult();
            t(bad.Error.Contains("Clé Riot refusée") && File.ReadAllText(path) == kept, "Matchups : une clé refusée n'abîme pas les données existantes");
            File.Delete(path);
        }
    }

    static class RoleRatesTests
    {
        const string Match = "{\"metadata\":{\"matchId\":\"M\"},\"info\":{\"gameDuration\":1800,\"participants\":[{\"puuid\":\"secret-puuid\",\"riotIdGameName\":\"SecretName\",\"championName\":\"Galio\",\"teamPosition\":\"MIDDLE\"},{\"puuid\":\"x\",\"championName\":\"MonkeyKing\",\"teamPosition\":\"JUNGLE\"},{\"championName\":\"Yuumi\",\"teamPosition\":\"\"}]}}";

        class SimFetcher : IRiotFetcher
        {
            public Func<string, RiotReply> Answer;
            public readonly List<string> Urls = new List<string>();
            public Task<RiotReply> GetAsync(string url) { Urls.Add(url); return Task.FromResult(Answer(url)); }
        }

        static RiotReply Ok(string body) { return new RiotReply { Status = 200, Body = body }; }

        public static void Run(Action<bool, string> t, string dir)
        {
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "roles-test.js");
            if (File.Exists(path)) File.Delete(path);
            Func<IRiotFetcher, int, int, RoleRatesUpdater> make = (f, target, min) => new RoleRatesUpdater(f, RiotRegion.All[0], path) { TargetGames = target, MinGamesToSave = min, DelayMs = 0, Wait = (ms, ct) => Task.FromResult(0) };
            Func<RoleRatesUpdater, RoleUpdateResult> run = u => Task.Run(() => u.Run(CancellationToken.None)).GetAwaiter().GetResult();
            int matchId = 0;
            Func<string, RiotReply> normal = url => url.Contains("league-exp") ? Ok("[{\"puuid\":\"aaa\"},{\"puuid\":\"bbb\"}]")
                : url.Contains("/ids") ? Ok("[\"M" + (++matchId) + "\",\"M" + (++matchId) + "\"]") : Ok(Match);

            SimFetcher sim = new SimFetcher { Answer = normal };
            RoleUpdateResult ok = run(make(sim, 3, 2));
            t(ok.Saved && ok.Games == 3 && ok.Error == "", "Rôles : le calcul s'arrête au nombre de parties voulu et enregistre le résultat");
            t(sim.Urls.All(u => u.StartsWith("https://euw1.api.riotgames.com/lol/league-exp/v4/") || u.StartsWith("https://europe.api.riotgames.com/lol/match/v5/")), "Rôles : seuls les serveurs officiels de Riot sont appelés");
            string file = File.ReadAllText(path);
            t(file.Contains("\"galio\":[0,0,3,0,0]") && file.Contains("\"monkeyking\":[0,3,0,0,0]") && file.Contains("window.ROLE_RATES_LIVE") && file.Contains("\"games\":3"), "Rôles : les compteurs sont corrects, au format lu par le moteur");
            t(!file.Contains("yuumi"), "Rôles : un joueur sans rôle n'est pas compté");
            t(!file.Contains("secret") && !file.Contains("Secret"), "Rôles : aucun nom ni identifiant de joueur dans le fichier");

            // Reprise : les parties déjà lues ne sont jamais redemandées, et chaque patch garde ses compteurs.
            string statePath = Path.Combine(dir, "roles-state.json"), resumePath = Path.Combine(dir, "roles-resume.js");
            foreach (string old in new[] { statePath, resumePath, resumePath + ".bak" }) if (File.Exists(old)) File.Delete(old);
            File.WriteAllText(resumePath, "// ancien\nwindow.ROLE_RATES_LIVE = {\"updated\":\"2026-11-04\",\"games\":1000,\"champions\":{\"ahri\":[2,0,117,2,2]}};\n");
            string patched = Match.Replace("\"gameDuration\":1800", "\"gameDuration\":1800,\"gameVersion\":\"16.22.712.345\"");
            int detail = 0, listed = 0;
            bool stopFirst = true;
            CancellationTokenSource stop = new CancellationTokenSource();
            Func<string, RiotReply> resumable = url =>
            {
                if (url.Contains("league-exp")) return Ok("[{\"puuid\":\"aaa\"}]");
                if (url.Contains("/ids")) { listed++; return Ok("[\"R1\",\"R2\",\"R3\",\"R4\"]"); }
                detail++;
                if (detail == 2 && stopFirst) stop.Cancel();
                return Ok(patched);
            };
            Func<RoleRatesUpdater> resumed = () => new RoleRatesUpdater(new SimFetcher { Answer = resumable }, RiotRegion.All[0], resumePath) { StatePath = statePath, TargetGames = 4, MinGamesToSave = 1, DelayMs = 0, Wait = (ms, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(0); } };
            RoleRatesUpdater first = resumed();
            RoleUpdateResult cut = Task.Run(() => first.Run(stop.Token)).GetAwaiter().GetResult();
            t(cut.Cancelled && File.Exists(statePath), "Collecte : un calcul interrompu garde ce qu'il a lu");
            t(File.Exists(resumePath + ".bak") && File.ReadAllText(resumePath + ".bak").Contains("ancien"), "Collecte : l'ancien fichier est sauvegardé");
            stopFirst = false;
            int before2 = detail;
            RoleRatesUpdater second = resumed();
            RoleUpdateResult again = run(second);
            t(again.Saved && second.GamesOfPatch("16.22") == 4 && detail - before2 == 2, "Collecte : la reprise ne redemande pas les parties déjà lues");
            string resumedFile = File.ReadAllText(resumePath);
            t(resumedFile.Contains("\"16.22\"") && resumedFile.Contains("\"legacy\"") && resumedFile.Contains("\"ahri\":[2,0,117,2,2]") && resumedFile.Contains(RiotRegion.All[0].Label), "Collecte : patch, région et anciennes données sont conservés");
            t(!File.ReadAllText(statePath).Contains("secret") && !resumedFile.Contains("secret"), "Collecte : aucun identifiant de joueur n'est conservé");
            int beforeThird = listed + detail;
            RoleUpdateResult third = run(resumed());
            t(third.UpToDate && listed + detail == beforeThird, "Collecte : déjà à jour, aucune requête n'est dépensée");
            File.WriteAllText(statePath, "{abimé");
            RoleRatesUpdater repaired = resumed();
            RoleUpdateResult afterDamage = run(repaired);
            t(afterDamage.Error == "" && File.Exists(statePath + ".abime") && File.Exists(resumePath + ".abime") && File.ReadAllText(resumePath).Contains("\"legacy\""), "Collecte : un fichier de reprise abîmé ne bloque rien et n'efface rien");

            // Mesure indépendante : une partie réservée n'entre jamais dans les compteurs.
            string fullTeam = "{\"info\":{\"gameDuration\":1800,\"gameVersion\":\"16.22.1\",\"participants\":[";
            string[] positions = { "TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY" }, champs = { "Sett", "Vi", "Ahri", "Jinx", "Thresh" };
            for (int i = 0; i < 5; i++) fullTeam += (i > 0 ? "," : "") + "{\"puuid\":\"secret\",\"teamId\":100,\"championName\":\"" + champs[i] + "\",\"teamPosition\":\"" + positions[i] + "\"}";
            fullTeam += "]}}";
            string hPath = Path.Combine(dir, "roles-holdout.js"), hState = Path.Combine(dir, "roles-holdout.json");
            foreach (string old in new[] { hPath, hState, hPath + ".bak" }) if (File.Exists(old)) File.Delete(old);
            RoleRatesUpdater reserving = new RoleRatesUpdater(new SimFetcher { Answer = url => url.Contains("league-exp") ? Ok("[{\"puuid\":\"aaa\"}]") : url.Contains("/ids") ? Ok("[\"R5\",\"R1\"]") : Ok(fullTeam) }, RiotRegion.All[0], hPath) { StatePath = hState, TargetGames = 1, MinGamesToSave = 1, DelayMs = 0, Wait = (ms, ct) => Task.FromResult(0) };
            run(reserving);
            string hSaved = File.ReadAllText(hState);
            t(RoleRatesUpdater.IsHoldout("R5") && !RoleRatesUpdater.IsHoldout("R1") && reserving.HoldoutCount == 1 && reserving.Counted == 1, "Mesure : une partie réservée n'entre pas dans les compteurs de rôles");
            t(hSaved.Contains("\"sett\",\"vi\",\"ahri\",\"jinx\",\"thresh\"") && !hSaved.Contains("secret"), "Mesure : la composition réelle est gardée, sans aucun joueur");

            RoleRatesUpdater remake = make(new SimFetcher(), 1, 1);
            remake.AddMatch("{\"info\":{\"gameDuration\":120,\"participants\":[{\"championName\":\"Sett\",\"teamPosition\":\"TOP\"}]}}");
            remake.AddMatch("pas du json");
            t(remake.Counted == 0, "Rôles : les remakes et les réponses abîmées sont ignorés");
            t(run(make(new SimFetcher { Answer = u => new RiotReply { Status = 403 } }, 1, 1)).Error.Contains("Clé Riot refusée"), "Rôles : une clé refusée arrête tout de suite le calcul");

            string before = File.ReadAllText(path);
            int calls = 0;
            RoleUpdateResult refused = run(make(new SimFetcher { Answer = u => { calls++; return u.Contains("league-exp") ? Ok("[{\"puuid\":\"a\"},{\"puuid\":\"b\"},{\"puuid\":\"c\"},{\"puuid\":\"d\"},{\"puuid\":\"e\"},{\"puuid\":\"f\"}]") : new RiotReply { Status = 400 }; } }, 5, 1));
            t(refused.Error.Contains("5 requêtes") && File.ReadAllText(path) == before, "Rôles : plusieurs refus de suite arrêtent le calcul sans abîmer les données existantes");

            RoleUpdateResult tooFew = run(make(new SimFetcher { Answer = normal }, 3, 50));
            t(!tooFew.Saved && File.ReadAllText(path) == before, "Rôles : un calcul trop court ne remplace pas de meilleures données");

            bool waited = false;
            int attempt = 0;
            RoleRatesUpdater limited = make(new SimFetcher { Answer = u => { if (attempt++ == 0) return new RiotReply { Status = 429, RetryAfterSeconds = 7 }; return normal(u); } }, 1, 1);
            limited.Wait = (ms, ct) => { if (ms == 7000) waited = true; return Task.FromResult(0); };
            t(run(limited).Saved && waited, "Rôles : la limite de requêtes de Riot est respectée");

            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.Cancel();
                RoleRatesUpdater stopped = make(new SimFetcher { Answer = normal }, 5, 1);
                stopped.Wait = (ms, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(0); };
                RoleUpdateResult r = Task.Run(() => stopped.Run(cts.Token)).GetAwaiter().GetResult();
                t(r.Cancelled && !r.Saved, "Rôles : le calcul peut être arrêté");
            }
            File.Delete(path);
        }
    }
}
