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
            r.UserAgent = "DraftMid";
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
        public bool Saved, Cancelled;
        public string Error = "";
    }

    public class RoleRatesUpdater
    {
        static readonly string[] Roles = { "TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY" };
        static readonly string[] Tiers = { "SILVER", "GOLD", "PLATINUM", "EMERALD" };
        const int QueueSoloDuo = 420, MatchesPerPlayer = 5, Days = 14, MinDurationSec = 300, MaxFailures = 5;

        readonly IRiotFetcher fetcher;
        readonly RiotRegion region;
        readonly string outPath;
        readonly Dictionary<string, int[]> counts = new Dictionary<string, int[]>();
        int counted, failures;

        public int Counted { get { return counted; } }
        public int TargetGames = 1000;
        // Limite d'une clé personnelle : 100 requêtes par 2 minutes.
        public int DelayMs = 1300;
        // Un calcul interrompu trop tôt ne remplace pas de meilleures données.
        public int MinGamesToSave = 200;
        public Action<int, int> Progress;
        public Func<int, CancellationToken, Task> Wait = (ms, ct) => Task.Delay(ms, ct);

        public RoleRatesUpdater(IRiotFetcher fetcher, RiotRegion region, string outPath)
        {
            this.fetcher = fetcher;
            this.region = region;
            this.outPath = outPath;
        }

        class StopRequest : Exception { public StopRequest(string text) : base(text) { } }

        async Task<string> Get(string url, string what, CancellationToken ct)
        {
            while (true)
            {
                await Wait(DelayMs, ct);
                RiotReply reply = await fetcher.GetAsync(url);
                if (reply.Status == 200) { failures = 0; return reply.Body; }
                if (reply.Status == 429) { await Wait(1000 * (reply.RetryAfterSeconds > 0 ? reply.RetryAfterSeconds : 120), ct); continue; }
                if (reply.Status == 401 || reply.Status == 403) throw new StopRequest("Clé Riot refusée ou expirée (code " + reply.Status + ").");
                if (reply.Status == 404) return null;
                if (++failures >= MaxFailures) throw new StopRequest("Riot a refusé " + MaxFailures + " requêtes de suite (" + what + ", code " + reply.Status + ").");
                return null;
            }
        }

        static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 }; }

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
            foreach (object o in (IEnumerable)players)
            {
                Dictionary<string, object> p = o as Dictionary<string, object>;
                object position, champion;
                if (p == null || !p.TryGetValue("teamPosition", out position) || !p.TryGetValue("championName", out champion)) continue;
                int role = Array.IndexOf(Roles, position as string);
                string key = Regex.Replace((champion as string ?? "").ToLowerInvariant(), "[^a-z0-9]", "");
                if (role < 0 || key == "") continue;
                int[] row;
                if (!counts.TryGetValue(key, out row)) counts[key] = row = new int[Roles.Length];
                row[role]++;
            }
            counted++;
        }

        public void Save()
        {
            string rows = string.Join(",", counts.OrderBy(c => c.Key, StringComparer.Ordinal).Select(c => "\"" + c.Key + "\":[" + string.Join(",", c.Value) + "]"));
            string text = "// Parties par rôle : top, jungle, mid, adc, support.\nwindow.ROLE_RATES_LIVE = {\"updated\":\"" + DateTime.UtcNow.ToString("yyyy-MM-dd")
                + "\",\"games\":" + counted + ",\"champions\":{" + rows + "}};\n";
            string temp = outPath + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(temp, text, new UTF8Encoding(false));
            if (File.Exists(outPath)) File.Delete(outPath);
            File.Move(temp, outPath);
        }

        public async Task<RoleUpdateResult> Run(CancellationToken ct)
        {
            RoleUpdateResult result = new RoleUpdateResult();
            string platformUrl = "https://" + region.Platform + ".api.riotgames.com", routingUrl = "https://" + region.Routing + ".api.riotgames.com";
            try
            {
                List<string> players = new List<string>();
                foreach (string tier in Tiers)
                {
                    string body = await Get(platformUrl + "/lol/league-exp/v4/entries/RANKED_SOLO_5x5/" + tier + "/II?page=1", "liste de joueurs", ct);
                    if (body == null) continue;
                    foreach (object o in (IEnumerable)Json().DeserializeObject(body))
                    {
                        Dictionary<string, object> entry = o as Dictionary<string, object>;
                        object id;
                        if (entry != null && entry.TryGetValue("puuid", out id) && id is string) players.Add((string)id);
                    }
                }
                if (players.Count == 0) throw new StopRequest("Aucun joueur lu : le format de réponse de Riot a peut-être changé.");

                Random random = new Random();
                long since = new DateTimeOffset(DateTime.UtcNow.AddDays(-Days)).ToUnixTimeSeconds();
                HashSet<string> seen = new HashSet<string>();
                foreach (string player in players.OrderBy(_ => random.Next()))
                {
                    if (counted >= TargetGames) break;
                    string list = await Get(routingUrl + "/lol/match/v5/matches/by-puuid/" + Uri.EscapeDataString(player) + "/ids?queue=" + QueueSoloDuo + "&startTime=" + since + "&start=0&count=" + MatchesPerPlayer, "historique d'un joueur", ct);
                    if (list == null) continue;
                    foreach (object idObject in (IEnumerable)Json().DeserializeObject(list))
                    {
                        string id = idObject as string;
                        if (counted >= TargetGames) break;
                        if (id == null || !seen.Add(id)) continue;
                        string match = await Get(routingUrl + "/lol/match/v5/matches/" + Uri.EscapeDataString(id), "détail d'une partie", ct);
                        if (match == null) continue;
                        AddMatch(match);
                        if (Progress != null) Progress(counted, TargetGames);
                    }
                }
            }
            catch (OperationCanceledException) { result.Cancelled = true; }
            catch (StopRequest e) { result.Error = e.Message; }
            catch (Exception) { result.Error = "Réponse de Riot illisible."; }

            result.Games = counted;
            result.Champions = counts.Count;
            if (counted >= MinGamesToSave)
            {
                try { Save(); result.Saved = true; }
                catch (Exception) { result.Error = "Écriture du fichier impossible."; }
            }
            return result;
        }
    }

    // Demande la clé pour ce calcul : elle n'est gardée nulle part.
    public class RiotKeyDialog : Form
    {
        readonly TextBox keyBox = new TextBox { UseSystemPasswordChar = true, Width = 340 };
        readonly ComboBox regionBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };

        public string Key { get { return keyBox.Text.Trim(); } }
        public RiotRegion SelectedRegion { get { return (RiotRegion)regionBox.SelectedItem; } }

        public RiotKeyDialog(Icon icon, Action<string> openLink)
        {
            Text = "DraftMid : mettre à jour les rôles";
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
            Label intro = new Label { AutoSize = true, MaximumSize = new Size(340, 0), Margin = new Padding(0, 0, 0, 10), Text = "Compte les rôles joués sur 1 000 parties récentes, pour mieux deviner le mid adverse. Le calcul prend environ 40 minutes et tourne en arrière-plan." };
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
            root.Controls.AddRange(new Control[] { intro, link, keyLabel, keyBox, regionLabel, regionBox, buttons });
            Controls.Add(root);
            AcceptButton = start;
            CancelButton = cancel;
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
