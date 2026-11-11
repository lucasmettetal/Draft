// Importe l'historique classé du joueur à sa demande, via l'API officielle de Riot : aucun appel sans action de sa part.
// La clé reste dans le RiotWebFetcher, en mémoire, le temps de l'import : l'importeur ne la voit jamais.
// Écrit un seul fichier, à part : draft-stats.js et les autres données personnelles ne sont ni lus, ni modifiés.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace DraftMid
{
    public class HistoryImportResult
    {
        public int Added, Total, Read;
        public bool Saved, Cancelled, HasMore;
        public string Error = "";
        public readonly List<string> NotFound = new List<string>();
    }

    public class HistoryImporter
    {
        // Plus ancienne date que l'API Riot sert pour l'historique (ex-HistoryImportDialog.OldestSupported).
        public static readonly DateTime OldestSupported = new DateTime(2021, 6, 16, 0, 0, 0, DateTimeKind.Utc);
        public const string FileName = "draft-history.js";
        public const string BackupFolder = "sauvegardes-historique";
        const string Prefix = "window.DRAFT_HISTORY = ";
        const int QueueSoloDuo = 420, MinDurationSec = 300, PageSize = 100, MaxFailures = 5;

        readonly IRiotFetcher fetcher;
        readonly RiotRegion region;
        readonly string outPath;
        readonly List<string> accounts;
        readonly DateTime sinceUtc;
        readonly List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
        readonly HashSet<string> known = new HashSet<string>();
        // Parties lues mais écartées (remakes) : à ne pas redemander.
        readonly List<string> skipped = new List<string>();
        bool backedUp, dirty;
        readonly List<string> allAccounts = new List<string>();
        int failures;

        // Limite d'une clé personnelle : 100 requêtes par 2 minutes.
        public int DelayMs = 1300;
        public int CheckpointEvery = 25;
        // Un import commence par les parties récentes, et le suivant va plus loin dans le passé.
        public int MaxGamesPerRun = 100;
        public Action<int, int, string> Progress;
        public Action<string> Status;
        public Func<int, CancellationToken, Task> Wait = (ms, ct) => Task.Delay(ms, ct);
        public int Requests { get; private set; }

        class StopImport : Exception { public StopImport(string text) : base(text) { } }

        public HistoryImporter(IRiotFetcher fetcher, RiotRegion region, string outPath, IEnumerable<string> accounts, DateTime sinceUtc)
        {
            this.fetcher = fetcher;
            this.region = region;
            this.outPath = outPath;
            this.accounts = accounts.Select(a => a.Trim()).Where(a => a != "").Distinct().ToList();
            this.sinceUtc = sinceUtc;
        }

        public static bool ValidAccount(string account)
        {
            string[] parts = (account ?? "").Trim().Split('#');
            return parts.Length == 2 && parts[0].Trim() != "" && parts[1].Trim() != "";
        }

        static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = 64 * 1024 * 1024 }; }

        // Comptes déjà importés, pour préremplir la fenêtre. Un fichier abîmé n'est jamais une erreur ici.
        public static List<string> SavedAccounts(string path)
        {
            try
            {
                Dictionary<string, object> saved = ReadFile(path);
                object list;
                if (saved != null && saved.TryGetValue("accounts", out list)) return ((object[])list).Select(Convert.ToString).ToList();
            }
            catch (Exception e) { if (e is OutOfMemoryException) throw; }
            return new List<string>();
        }

        static Dictionary<string, object> ReadFile(string path)
        {
            if (!File.Exists(path)) return null;
            string text = File.ReadAllText(path, Encoding.UTF8).Trim();
            if (!text.StartsWith(Prefix)) throw new FormatException();
            return (Dictionary<string, object>)Json().DeserializeObject(text.Substring(Prefix.Length).TrimEnd(';'));
        }

        // Un fichier abîmé est mis de côté, jamais écrasé.
        void LoadPrevious()
        {
            try
            {
                Dictionary<string, object> saved = ReadFile(outPath);
                if (saved == null) return;
                foreach (object o in (object[])saved["rows"])
                {
                    Dictionary<string, object> row = (Dictionary<string, object>)o;
                    if (known.Add(row["acct"] + "|" + row["id"])) rows.Add(row);
                }
                foreach (object a in (object[])saved["accounts"]) if (!allAccounts.Contains(Convert.ToString(a))) allAccounts.Add(Convert.ToString(a));
                object old;
                if (saved.TryGetValue("skipped", out old)) foreach (object o in (object[])old) { skipped.Add(Convert.ToString(o)); known.Add(Convert.ToString(o)); }
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException) throw;
                rows.Clear();
                known.Clear();
                skipped.Clear();
                allAccounts.Clear();
                string aside = outPath + ".abime";
                if (File.Exists(aside)) File.Delete(aside);
                File.Move(outPath, aside);
            }
        }

        // Une copie datée avant le premier changement de chaque import. Les 5 dernières sont gardées.
        void BackupOnce()
        {
            if (backedUp || !File.Exists(outPath)) return;
            backedUp = true;
            string dir = Path.Combine(Path.GetDirectoryName(outPath), BackupFolder);
            Directory.CreateDirectory(dir);
            File.Copy(outPath, Path.Combine(dir, "draft-history-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".js"), true);
            foreach (string old in Directory.GetFiles(dir, "draft-history-*.js").OrderByDescending(f => f).Skip(5)) File.Delete(old);
        }

        void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            BackupOnce();
            string temp = outPath + ".tmp";
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "generated", DateTime.UtcNow.ToString("o") },
                { "accounts", allAccounts },
                { "skipped", skipped },
                { "rows", rows.OrderByDescending(r => Convert.ToInt64(r["time"])).ToList() }
            };
            File.WriteAllText(temp, Prefix + Json().Serialize(payload) + ";\n", new UTF8Encoding(false));
            if (File.Exists(outPath)) File.Replace(temp, outPath, outPath + ".precedent");
            else File.Move(temp, outPath);
        }

        // Ne renvoie jamais le texte d'une réponse, ni la clé, dans les messages.
        async Task<string> Get(string url, CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                await Wait(DelayMs, ct);
                Requests++;
                RiotReply reply = await fetcher.GetAsync(url);
                if (reply.Status == 200) { failures = 0; return reply.Body; }
                if (reply.Status == 404) return null;
                if (reply.Status == 401 || reply.Status == 403) throw new StopImport("Clé Riot refusée ou expirée. Crée une nouvelle clé, puis relance : les parties déjà lues sont conservées.");
                if (reply.Status == 429)
                {
                    int seconds = reply.RetryAfterSeconds > 0 ? reply.RetryAfterSeconds : 120;
                    if (Status != null) Status("Limite de requêtes de Riot atteinte : reprise dans " + seconds + " secondes. Rien n'est perdu.");
                    await Wait(seconds * 1000, ct);
                    continue;
                }
                if (++failures >= MaxFailures) throw new StopImport("Riot ne répond pas pour le moment. Relance plus tard : les parties déjà lues sont conservées.");
                await Wait(2000, ct);
            }
        }

        public static Dictionary<string, object> ConvertMatch(Dictionary<string, object> match, string puuid, string account)
        {
            Dictionary<string, object> meta = (Dictionary<string, object>)match["metadata"], info = (Dictionary<string, object>)match["info"];
            object queue;
            if (info.TryGetValue("queueId", out queue) && Convert.ToInt32(queue) != QueueSoloDuo) return null;
            if (Convert.ToInt32(info["gameDuration"]) < MinDurationSec) return null;
            List<Dictionary<string, object>> players = ((object[])info["participants"]).Cast<Dictionary<string, object>>().ToList();
            Dictionary<string, object> me = players.FirstOrDefault(p => Convert.ToString(p["puuid"]) == puuid);
            if (me == null) return null;
            List<Dictionary<string, object>> enemies = players.Where(p => Convert.ToInt32(p["teamId"]) != Convert.ToInt32(me["teamId"])).ToList();
            Dictionary<string, object> enemyMid = enemies.FirstOrDefault(p => Convert.ToString(p["teamPosition"]) == "MIDDLE");
            string[] version = Convert.ToString(info["gameVersion"]).Split('.');
            // Aucun identifiant ni nom d'un autre joueur : seulement les champions adverses.
            return new Dictionary<string, object>
            {
                { "id", Convert.ToString(meta["matchId"]) },
                { "acct", account },
                { "time", Convert.ToInt64(info["gameCreation"]) },
                { "patch", version.Length >= 2 ? version[0] + "." + version[1] : "" },
                { "sec", Convert.ToInt32(info["gameDuration"]) },
                { "champ", Convert.ToString(me["championName"]) },
                { "pos", Convert.ToString(me["teamPosition"]) },
                { "win", Convert.ToBoolean(me["win"]) },
                { "k", Convert.ToInt32(me["kills"]) },
                { "d", Convert.ToInt32(me["deaths"]) },
                { "a", Convert.ToInt32(me["assists"]) },
                { "mid", enemyMid != null ? Convert.ToString(enemyMid["championName"]) : "" },
                { "enemies", enemies.Select(p => Convert.ToString(p["championName"])).ToList() }
            };
        }

        async Task ImportAccount(string account, HistoryImportResult result, CancellationToken ct)
        {
            string[] parts = account.Split('#');
            string routing = "https://" + region.Routing + ".api.riotgames.com";
            string body = await Get(routing + "/riot/account/v1/accounts/by-riot-id/" + Uri.EscapeDataString(parts[0].Trim()) + "/" + Uri.EscapeDataString(parts[1].Trim()), ct);
            if (body == null) { result.NotFound.Add(account); return; }
            string puuid = Convert.ToString(((Dictionary<string, object>)Json().DeserializeObject(body))["puuid"]);
            if (!allAccounts.Contains(account)) { allAccounts.Add(account); dirty = true; }
            long since = (long)(sinceUtc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            int room = MaxGamesPerRun - result.Read;
            if (room <= 0) { result.HasMore = true; return; }
            // Du plus récent au plus ancien, sans lister plus que ce que cet import peut lire.
            List<string> missing = new List<string>();
            bool exhausted = false;
            for (int start = 0; ; start += PageSize)
            {
                string page = await Get(routing + "/lol/match/v5/matches/by-puuid/" + puuid + "/ids?queue=" + QueueSoloDuo + "&startTime=" + since + "&start=" + start + "&count=" + PageSize, ct);
                object[] found = page == null ? new object[0] : (object[])Json().DeserializeObject(page);
                missing.AddRange(found.Select(Convert.ToString).Where(id => !known.Contains(account + "|" + id)));
                if (found.Length < PageSize) { exhausted = true; break; }
                if (missing.Count >= room) break;
            }
            if (!exhausted || missing.Count > room) result.HasMore = true;
            int goal = Math.Min(room, missing.Count), done = 0;
            foreach (string id in missing.Take(goal))
            {
                string text = await Get(routing + "/lol/match/v5/matches/" + id, ct);
                done++;
                result.Read++;
                if (text != null)
                {
                    Dictionary<string, object> row = ConvertMatch((Dictionary<string, object>)Json().DeserializeObject(text), puuid, account);
                    known.Add(account + "|" + id);
                    if (row != null) { rows.Add(row); result.Added++; }
                    else skipped.Add(account + "|" + id);
                    dirty = true;
                }
                if (Progress != null) Progress(done, goal, account);
                if (done % CheckpointEvery == 0 && dirty) Save();
            }
        }

        public async Task<HistoryImportResult> Run(CancellationToken ct)
        {
            HistoryImportResult result = new HistoryImportResult();
            if (accounts.Count == 0 || !accounts.All(ValidAccount)) { result.Error = "Compte invalide : le format attendu est Nom#TAG."; return result; }
            try
            {
                LoadPrevious();
                foreach (string account in accounts) await ImportAccount(account, result, ct);
            }
            catch (OperationCanceledException) { result.Cancelled = true; }
            catch (StopImport e) { result.Error = e.Message; }
            catch (Exception e) { if (e is OutOfMemoryException) throw; result.Error = "Réponse de Riot illisible : les parties déjà lues sont conservées."; }
            // Rien de nouveau : le fichier existant ne bouge pas. Rien du tout : pas de fichier vide.
            if (dirty && (rows.Count > 0 || File.Exists(outPath))) { Save(); result.Saved = true; }
            result.Total = rows.Count;
            return result;
        }
    }

    // La fenêtre de saisie est commune aux trois lectures Riot : voir RiotKeyDialog.cs (UX-5).

    static class HistoryImporterTests
    {
        const string Me = "secret-puuid-moi";

        class Sim : IRiotFetcher
        {
            public Func<string, RiotReply> Answer;
            public readonly List<string> Urls = new List<string>();
            public Task<RiotReply> GetAsync(string url) { Urls.Add(url); return Task.FromResult(Answer(url)); }
        }

        static string Match(string id, int duration = 1800, int queue = 420, bool win = true)
        {
            return "{\"metadata\":{\"matchId\":\"" + id + "\"},\"info\":{\"queueId\":" + queue + ",\"gameCreation\":1790000000000,\"gameDuration\":" + duration + ",\"gameVersion\":\"16.22.712.345\",\"participants\":["
                + "{\"puuid\":\"" + Me + "\",\"riotIdGameName\":\"NomPrive\",\"teamId\":100,\"championName\":\"KogMaw\",\"teamPosition\":\"MIDDLE\",\"win\":" + (win ? "true" : "false") + ",\"kills\":3,\"deaths\":2,\"assists\":5},"
                + "{\"puuid\":\"secret-puuid-adverse\",\"riotIdGameName\":\"NomAdverse\",\"teamId\":200,\"championName\":\"Ahri\",\"teamPosition\":\"MIDDLE\",\"win\":false,\"kills\":1,\"deaths\":1,\"assists\":1},"
                + "{\"puuid\":\"secret-puuid-allie\",\"riotIdGameName\":\"NomAllie\",\"teamId\":100,\"championName\":\"Thresh\",\"teamPosition\":\"UTILITY\",\"win\":true,\"kills\":0,\"deaths\":1,\"assists\":9}]}}";
        }

        static Func<string, RiotReply> Riot(params string[] ids)
        {
            return url =>
            {
                if (url.Contains("/by-riot-id/")) return new RiotReply { Status = 200, Body = "{\"puuid\":\"" + Me + "\"}" };
                if (url.Contains("/ids?")) return new RiotReply { Status = 200, Body = url.Contains("start=0&") ? "[\"" + string.Join("\",\"", ids) + "\"]" : "[]" };
                string id = url.Substring(url.LastIndexOf('/') + 1);
                return new RiotReply { Status = 200, Body = Match(id, id == "REMAKE" ? 120 : 1800, id == "FLEX" ? 440 : 420) };
            };
        }

        static string Sum(string path) { return File.Exists(path) ? Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path))) : ""; }

        public static void Run(Action<bool, string> t, string dir)
        {
            string home = Path.Combine(dir, "history-test");
            if (Directory.Exists(home)) Directory.Delete(home, true);
            Directory.CreateDirectory(home);
            string path = Path.Combine(home, HistoryImporter.FileName);
            Func<IRiotFetcher, HistoryImporter> make = f => new HistoryImporter(f, RiotRegion.All[0], path, new[] { "Test#EUW" }, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)) { DelayMs = 0, Wait = (ms, ct) => Task.FromResult(0) };

            // Données existantes : fictives, dans ce dossier de test.
            string[] personal = { "draft-stats.js", "draft-stats-cache.json", "draft-accounts.txt", "settings.json", "draft-role-rates-live.js" };
            foreach (string p in personal) File.WriteAllText(Path.Combine(home, p), "contenu existant " + p);
            Dictionary<string, string> before = personal.ToDictionary(p => p, p => Sum(Path.Combine(home, p)));

            t(HistoryImporter.ValidAccount("Nom#EUW") && !HistoryImporter.ValidAccount("Nom") && !HistoryImporter.ValidAccount("#EUW"), "historique : format de compte vérifié");
            Sim bad = new Sim { Answer = Riot("A") };
            HistoryImportResult invalid = new HistoryImporter(bad, RiotRegion.All[0], path, new[] { "SansTag" }, DateTime.UtcNow) { DelayMs = 0 }.Run(CancellationToken.None).Result;
            t(invalid.Error != "" && bad.Urls.Count == 0, "historique : compte invalide, aucune requête");

            Sim sim = new Sim { Answer = Riot("A", "B", "REMAKE", "FLEX") };
            HistoryImportResult r = make(sim).Run(CancellationToken.None).Result;
            string text = File.ReadAllText(path);
            t(r.Saved && r.Added == 2 && r.Total == 2, "historique : parties importées, remake et autre file écartés");
            t(text.StartsWith("window.DRAFT_HISTORY = "), "historique : fichier lisible par la page");
            t(text.Contains("\"champ\":\"KogMaw\"") && text.Contains("\"pos\":\"MIDDLE\"") && text.Contains("\"k\":3") && text.Contains("\"patch\":\"16.22\"") && text.Contains("\"enemies\":[\"Ahri\"]"), "historique : champion, rôle, KDA, patch et adversaires");
            t(!text.Contains("puuid") && !text.Contains("NomPrive") && !text.Contains("NomAdverse") && !text.Contains("NomAllie"), "historique : aucun identifiant ni nom de joueur");
            t(sim.Urls.All(u => u.Contains("europe.api.riotgames.com") && !u.Contains("api_key")), "historique : la clé ne passe jamais dans une adresse");
            t(typeof(HistoryImporter).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).All(f => f.Name.IndexOf("key", StringComparison.OrdinalIgnoreCase) < 0 || f.Name == "known"), "historique : l'importeur ne garde aucune clé");
            t(sim.Urls.Any(u => u.Contains("queue=420") && u.Contains("startTime=1767225600")), "historique : solo/duo, depuis la date choisie");

            Sim again = new Sim { Answer = Riot("A", "B", "NEW") };
            HistoryImportResult r2 = make(again).Run(CancellationToken.None).Result;
            t(r2.Added == 1 && r2.Total == 3 && !again.Urls.Any(u => u.EndsWith("/A")), "historique : reprise sans redemander ni doubler");
            t(!again.Urls.Any(u => u.EndsWith("/REMAKE") || u.EndsWith("/FLEX")), "historique : une partie écartée n'est pas redemandée");
            t(Directory.GetFiles(Path.Combine(home, HistoryImporter.BackupFolder), "draft-history-*.js").Length == 1, "historique : copie datée avant de modifier");
            t(HistoryImporter.SavedAccounts(path).SequenceEqual(new[] { "Test#EUW" }), "historique : comptes préremplis");

            string saved = File.ReadAllText(path);
            Sim same = new Sim { Answer = Riot("A", "B", "NEW") };
            HistoryImportResult r3 = make(same).Run(CancellationToken.None).Result;
            t(r3.Added == 0 && !r3.Saved && File.ReadAllText(path) == saved, "historique : rien de nouveau, fichier inchangé");
            string progressive = Path.Combine(home, "progressif.js");
            Func<int, string[], HistoryImporter> step = (limit, ids) => new HistoryImporter(new Sim { Answer = Riot(ids) }, RiotRegion.All[0], progressive, new[] { "Test#EUW" }, HistoryImporter.OldestSupported) { DelayMs = 0, MaxGamesPerRun = limit, Wait = (ms, ct) => Task.FromResult(0) };
            HistoryImportResult first = step(2, new[] { "P1", "P2", "P3" }).Run(CancellationToken.None).Result;
            t(first.Added == 2 && first.HasMore && File.ReadAllText(progressive).Contains("\"P1\"") && !File.ReadAllText(progressive).Contains("\"P3\""), "historique : premier import limité aux parties récentes");
            HistoryImportResult second = step(2, new[] { "P1", "P2", "P3" }).Run(CancellationToken.None).Result;
            t(second.Added == 1 && second.Total == 3 && !second.HasMore, "historique : enrichissement progressif, puis plus rien à lire");
            HistoryImportResult refused = make(new Sim { Answer = u => new RiotReply { Status = 403, Body = "RGAPI-fausse-cle" } }).Run(CancellationToken.None).Result;
            t(refused.Error.Contains("refusée") && !refused.Error.Contains("RGAPI") && File.ReadAllText(path).Contains("\"NEW\"") && refused.Total == 3, "historique : clé refusée, rien de perdu, rien de divulgué");
            int waits = 0;
            bool limited = false;
            Sim wait = new Sim();
            wait.Answer = u => { if (!limited && u.EndsWith("/C")) { limited = true; return new RiotReply { Status = 429, RetryAfterSeconds = 7 }; } return Riot("C")(u); };
            HistoryImporter patient = make(wait);
            patient.Wait = (ms, ct) => { if (ms == 7000) waits++; return Task.FromResult(0); };
            string quota = "";
            patient.Status = text0 => quota = text0;
            t(patient.Run(CancellationToken.None).Result.Added == 1 && waits == 1, "historique : limite de Riot respectée");
            t(quota.Contains("7 secondes"), "historique : attente due au quota expliquée");
            Sim gone = new Sim { Answer = u => u.Contains("/by-riot-id/") ? new RiotReply { Status = 404 } : new RiotReply { Status = 200, Body = "[]" } };
            HistoryImportResult missing = make(gone).Run(CancellationToken.None).Result;
            t(missing.NotFound.Count == 1 && missing.Total == 4, "historique : compte introuvable signalé");
            Sim down = new Sim { Answer = u => new RiotReply { Status = 500 } };
            t(make(down).Run(CancellationToken.None).Result.Error != "" && down.Urls.Count == 5, "historique : Riot indisponible, arrêt après 5 essais");
            Sim garbage = new Sim { Answer = u => new RiotReply { Status = 200, Body = "{abimé" } };
            HistoryImportResult unreadable = make(garbage).Run(CancellationToken.None).Result;
            t(unreadable.Error.Contains("illisible") && unreadable.Total == 4, "historique : réponse illisible, rien de perdu");
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.Cancel();
                t(make(new Sim { Answer = Riot("X") }).Run(cts.Token).Result.Cancelled, "historique : arrêt à la demande");
            }

            File.WriteAllText(path, "{fichier abimé");
            HistoryImportResult fresh = make(new Sim { Answer = Riot("A") }).Run(CancellationToken.None).Result;
            t(File.ReadAllText(path + ".abime") == "{fichier abimé" && fresh.Total == 1, "historique : fichier abîmé mis de côté, jamais écrasé");
            t(HistoryImporter.SavedAccounts(path + ".abime").Count == 0, "historique : lecture d'un fichier abîmé sans erreur");
            string onlyDir = Path.Combine(home, "vide");
            Directory.CreateDirectory(onlyDir);
            string none = Path.Combine(onlyDir, HistoryImporter.FileName);
            new HistoryImporter(new Sim { Answer = u => new RiotReply { Status = 403 } }, RiotRegion.All[0], none, new[] { "Test#EUW" }, DateTime.UtcNow) { DelayMs = 0, Wait = (ms, ct) => Task.FromResult(0) }.Run(CancellationToken.None).Wait();
            t(!File.Exists(none), "historique : aucune partie lue, aucun fichier créé");

            t(personal.All(p => Sum(Path.Combine(home, p)) == before[p]), "historique : données personnelles existantes inchangées");
            t(!Directory.GetFiles(home).Any(f => File.ReadAllText(f).Contains("RGAPI")), "historique : aucune clé sur le disque");
            using (RiotKeyDialog dialog = new RiotKeyDialog(null, _ => { }, new[] { "Test#EUW", "Autre#EUW" }, RiotTask.History, "mid", 3))
            {
                dialog.CreateControl();
                dialog.CreateControl();
                t(dialog.ConsentFits(), "historique : texte du consentement entier");
                t(dialog.Accounts.Count == 2 && dialog.Key == "" && dialog.SelectedRegion != null && dialog.GamesToRead == 100, "historique : fenêtre préremplie, clé vide, 100 parties par défaut");
                t(dialog.TestPreselected() == RiotTask.History, "clé Riot : la lecture demandée est cochée, et elle seule");
                t(!dialog.TestCanStart("Test#EUW", "fausse", false), "historique : aucun import sans consentement");
                t(!dialog.TestCanStart("SansTag", "fausse", true), "historique : compte invalide refusé");
                t(dialog.TestCanStart("Test#EUW", "fausse", true), "historique : consentement, compte et clé suffisent");
                t(!dialog.TestCanStartTasks(RiotTask.None, "fausse"), "clé Riot : aucune lecture cochée, rien ne se lance");
                t(dialog.TestCanStartTasks(RiotTask.Roles | RiotTask.Matchups, "fausse") && !dialog.TestCanStartTasks(RiotTask.Roles, ""), "clé Riot : rôles et matchups avec une seule saisie de clé ; sans clé, rien");
            }
            using (RiotKeyDialog noPool = new RiotKeyDialog(null, _ => { }, new string[0], RiotTask.Matchups, null, 0))
            {
                noPool.CreateControl();
                t(!noPool.TestMatchupsAvailable() && noPool.TestPreselected() == RiotTask.None, "clé Riot : matchups indisponibles sans pool, et dit pourquoi");
            }
            // La clé ne vit que dans la fenêtre puis dans le fetcher : aucun champ ni propriété statique ne la garde.
            t(typeof(RiotKeyDialog).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).All(f => f.FieldType != typeof(string) || f.IsLiteral), "clé Riot : aucune variable statique ne peut garder la clé");
            Directory.Delete(home, true);
        }
    }
}
