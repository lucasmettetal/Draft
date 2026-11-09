// Connecteur de lecture du client League of Legends, en lecture seule.
// Riot indique que cette API n'est pas prise en charge pour les applications tierces.
// Option décochée par défaut, sans confirmation de Riot : voir docs/LCU.md.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DraftMid
{
    public static class LcuGate
    {
        // Décision du propriétaire du projet, sans réponse de Riot à ce jour.
        // À repasser à false si Riot refuse cet usage.
        public static readonly bool ReadingEnabled = true;

        // L'interrupteur de compilation et l'option de l'utilisateur sont tous deux nécessaires.
        public static bool Allowed(bool optionEnabled) { return ReadingEnabled && optionEnabled; }
    }

    public static class LcuEndpoints
    {
        public const string Phase = "/lol-gameflow/v1/gameflow-phase";
        public const string Session = "/lol-champ-select/v1/session";
        public static bool IsAllowed(string path) { return path == Phase || path == Session; }
    }

    // Seule opération possible : une lecture. Aucune écriture n'existe dans cette interface.
    public interface ILcuTransport
    {
        // Renvoie le corps de la réponse, "" si la ressource n'existe pas, null si le client ne répond pas.
        Task<string> GetAsync(string path);
    }

    public class LockfileInfo
    {
        public int Port;
        public string Password;

        // Identifiants temporaires gardés en mémoire seulement, jamais écrits ni journalisés.
        public static LockfileInfo Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            string[] p = content.Trim().Split(':');
            int port;
            if (p.Length != 5 || p[4] != "https" || !int.TryParse(p[2], out port) || port <= 0 || port > 65535 || p[3].Length == 0) return null;
            return new LockfileInfo { Port = port, Password = p[3] };
        }

        public override string ToString() { return "port " + Port + ", mot de passe masqué"; }
    }

    public class LocalHttpsTransport : ILcuTransport
    {
        const int TimeoutMs = 1500;
        readonly LockfileInfo info;
        // Nature de la dernière erreur réseau, pour le journal : jamais d'identifiant.
        public static string LastError = "";

        public LocalHttpsTransport(LockfileInfo info)
        {
            this.info = info;
            // Sans cela, .NET Framework peut proposer des protocoles que le client refuse.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public async Task<string> GetAsync(string path)
        {
            if (!LcuEndpoints.IsAllowed(path)) throw new InvalidOperationException("Endpoint non autorisé");
            // Adresse locale fixe : le mot de passe ne peut pas partir ailleurs.
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create("https://127.0.0.1:" + info.Port + path);
            r.Method = "GET";
            r.AllowAutoRedirect = false;
            r.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("riot:" + info.Password));
            // Certificat auto-signé du client, accepté pour cette seule requête locale.
            r.ServerCertificateValidationCallback = (s, c, ch, e) => true;
            // HttpWebRequest.Timeout ne s'applique pas aux appels asynchrones : un client figé bloquerait les lectures suivantes.
            using (new Timer(_ => r.Abort(), null, TimeoutMs, Timeout.Infinite))
            try
            {
                using (WebResponse w = await r.GetResponseAsync())
                using (StreamReader rd = new StreamReader(w.GetResponseStream(), Encoding.UTF8))
                {
                    string body = await rd.ReadToEndAsync();
                    LastError = "";
                    return body;
                }
            }
            catch (WebException e)
            {
                HttpWebResponse h = e.Response as HttpWebResponse;
                LastError = h != null ? "http " + (int)h.StatusCode : e.Status.ToString();
                return h != null && h.StatusCode == HttpStatusCode.NotFound ? "" : null;
            }
        }
    }

    public static class LcuFactory
    {
        // Le lockfile n'est jamais ouvert tant que le verrou est fermé.
        public static ILcuTransport CreateReal(bool optionEnabled, string clientProcessFolder)
        {
            if (!LcuGate.Allowed(optionEnabled) || clientProcessFolder == null) return null;
            string path = Path.Combine(clientProcessFolder, "lockfile");
            try
            {
                using (FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (StreamReader r = new StreamReader(f))
                {
                    LockfileInfo i = LockfileInfo.Parse(r.ReadToEnd());
                    return i == null ? null : new LocalHttpsTransport(i);
                }
            }
            catch { return null; }
        }
    }

    public class LcuSnapshot
    {
        public bool Available;
        public string PhaseName = "";
        public Phase Phase = Phase.Uncertain;
        // Ton poste dans cette sélection, si le client l'indique. Aucun autre joueur de ton équipe n'est lu.
        public string AssignedRole;
        // Seuls les champions verrouillés et visibles de l'équipe adverse sont conservés.
        // Null pendant une draft : session illisible ou expirée, à ne pas confondre avec une équipe vide.
        public List<int> EnemyChampionIds = new List<int>();
    }

    public static class LcuParser
    {
        public static Phase MapPhase(string name)
        {
            switch (name)
            {
                case "ChampSelect": return Phase.Draft;
                case "GameStart": case "InProgress": case "Reconnect": return Phase.InGame;
                case "None": case "Lobby": case "Matchmaking": case "ReadyCheck": case "WaitingForStats":
                case "PreEndOfGame": case "EndOfGame": case "TerminatedInError": case "CheckedIntoTournament":
                    return Phase.OutOfDraft;
                default: return Phase.Uncertain;
            }
        }

        public static string ParsePhase(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "";
            try { return new JavaScriptSerializer().Deserialize<string>(json) ?? ""; } catch { return ""; }
        }

        // Ne lit volontairement ni nom, ni identifiant de joueur, ni intention de pick, ni sort.
        // Poste du joueur (myTeam[cellId == localPlayerCellId].assignedPosition). Champ non documenté par Riot.
        // Vide en sélection aveugle ou dans les modes sans poste : on renvoie null, rien n'est deviné.
        public static string ParseAssignedRole(string json)
        {
            try
            {
                Dictionary<string, object> root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                object cell, team;
                if (root == null || !root.TryGetValue("localPlayerCellId", out cell) || !(cell is int) || !root.TryGetValue("myTeam", out team) || !(team is object[])) return null;
                foreach (object o in (object[])team)
                {
                    Dictionary<string, object> m = o as Dictionary<string, object>;
                    object id, pos;
                    if (m == null || !m.TryGetValue("cellId", out id) || !(id is int) || (int)id != (int)cell) continue;
                    if (!m.TryGetValue("assignedPosition", out pos) || !(pos is string)) return null;
                    switch (((string)pos).ToLowerInvariant())
                    {
                        case "top": return "top";
                        case "jungle": return "jungle";
                        case "middle": case "mid": return "mid";
                        case "bottom": case "adc": return "adc";
                        case "utility": case "support": return "support";
                        default: return null;
                    }
                }
            }
            catch { }
            return null;
        }

        public static List<int> ParseEnemies(string json)
        {
            List<int> ids = new List<int>();
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                Dictionary<string, object> root = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.Deserialize<Dictionary<string, object>>(json);
                object team;
                if (root == null || !root.TryGetValue("theirTeam", out team)) return null;
                IEnumerable list = team as IEnumerable;
                if (list == null || team is string) return null;
                foreach (object o in list)
                {
                    Dictionary<string, object> m = o as Dictionary<string, object>;
                    object c;
                    if (m != null && m.TryGetValue("championId", out c) && c is int && (int)c > 0 && !ids.Contains((int)c)) ids.Add((int)c);
                }
            }
            catch { return null; }
            return ids;
        }
    }

    public class LcuReader
    {
        readonly ILcuTransport transport;
        public LcuReader(ILcuTransport transport) { this.transport = transport; }

        public async Task<LcuSnapshot> PollAsync()
        {
            LcuSnapshot s = new LcuSnapshot();
            string phase;
            try { phase = await transport.GetAsync(LcuEndpoints.Phase); }
            catch { return s; }
            if (phase == null) return s;
            s.PhaseName = LcuParser.ParsePhase(phase);
            if (s.PhaseName == "") return s;
            s.Available = true;
            s.Phase = LcuParser.MapPhase(s.PhaseName);
            if (s.Phase != Phase.Draft) return s;
            // La session n'est demandée que pendant une draft.
            s.EnemyChampionIds = null;
            try
            {
                string session = await transport.GetAsync(LcuEndpoints.Session);
                s.EnemyChampionIds = LcuParser.ParseEnemies(session);
                if (s.EnemyChampionIds != null) s.AssignedRole = LcuParser.ParseAssignedRole(session);
            }
            catch { }
            return s;
        }
    }

    // Durée de vie de la connexion : les identifiants ne vivent que le temps d'une connexion qui répond.
    public class LcuSession
    {
        readonly Func<ILcuTransport> open;
        LcuReader reader;
        int nextAttempt = Environment.TickCount;
        public int RetryMs = 3000;
        public int PollTimeoutMs = 3500;

        public LcuSession(Func<ILcuTransport> open) { this.open = open; }
        public bool Connected { get { return reader != null; } }

        public async Task<LcuSnapshot> PollAsync()
        {
            if (reader == null)
            {
                if (unchecked(Environment.TickCount - nextAttempt) < 0) return new LcuSnapshot();
                ILcuTransport t = open();
                if (t == null) { Forget(); return new LcuSnapshot(); }
                reader = new LcuReader(t);
            }
            Task<LcuSnapshot> poll = reader.PollAsync();
            if (await Task.WhenAny(poll, Task.Delay(PollTimeoutMs)) != poll) { Forget(); return new LcuSnapshot(); }
            LcuSnapshot s = await poll;
            if (!s.Available) Forget();
            return s;
        }

        public void Forget()
        {
            reader = null;
            nextAttempt = unchecked(Environment.TickCount + RetryMs);
        }
    }

    // Transport de test : données simulées, aucune connexion.
    public class SimulatedLcuTransport : ILcuTransport
    {
        public readonly List<string> Requests = new List<string>();
        public string PhaseJson = "\"None\"";
        public string SessionJson = "";
        public bool Offline;
        public bool Hang;

        public Task<string> GetAsync(string path)
        {
            Requests.Add(path);
            if (!LcuEndpoints.IsAllowed(path)) throw new InvalidOperationException("Endpoint non autorisé");
            if (Hang) return new TaskCompletionSource<string>().Task;
            if (Offline) return Task.FromResult<string>(null);
            return Task.FromResult(path == LcuEndpoints.Phase ? PhaseJson : SessionJson);
        }
    }

    static class LcuTests
    {
        const string Session = "{\"localPlayerCellId\":2,\"myTeam\":[{\"cellId\":2,\"championId\":105,\"summonerId\":123456,\"gameName\":\"secret\"}],"
            + "\"theirTeam\":[{\"cellId\":5,\"championId\":875,\"championPickIntent\":157,\"summonerId\":999,\"gameName\":\"cache\"},{\"cellId\":6,\"championId\":254},{\"cellId\":7,\"championId\":3},{\"cellId\":8,\"championId\":0,\"championPickIntent\":222},{\"cellId\":9,\"championId\":40}],\"bans\":{}}";

        public static void Run(Action<bool, string> t)
        {
            t(!LcuGate.Allowed(false), "LCU : option décochée, aucune connexion");
            t(LcuFactory.CreateReal(false, Path.GetTempPath()) == null, "LCU : option décochée, le lockfile n'est pas ouvert");
            t(LcuFactory.CreateReal(true, Path.GetTempPath()) == null, "LCU : sans lockfile, aucun connecteur n'est créé");

            t(LcuEndpoints.IsAllowed(LcuEndpoints.Phase) && LcuEndpoints.IsAllowed(LcuEndpoints.Session), "LCU : les deux endpoints documentés sont autorisés");
            t(!LcuEndpoints.IsAllowed("/lol-summoner/v1/current-summoner") && !LcuEndpoints.IsAllowed("/lol-champ-select/v1/session/actions/1"), "LCU : tout autre endpoint est refusé");
            t(typeof(ILcuTransport).GetMethods().Length == 1 && typeof(ILcuTransport).GetMethods()[0].Name == "GetAsync", "LCU : l'interface ne permet qu'une lecture");

            LockfileInfo li = LockfileInfo.Parse("LeagueClient:1234:50000:motdepasse:https");
            t(li != null && li.Port == 50000 && li.Password == "motdepasse", "LCU : lecture d'un lockfile simulé");
            t(!li.ToString().Contains("motdepasse"), "LCU : le mot de passe n'apparaît jamais dans un journal");
            t(LockfileInfo.Parse("abimé") == null && LockfileInfo.Parse("a:b:port:c:https") == null && LockfileInfo.Parse("a:1:50000:c:http") == null, "LCU : un lockfile invalide est refusé");
            string prefs = new JavaScriptSerializer().Serialize(new UserPreferences());
            t(!prefs.Contains("Password") && !prefs.Contains("Port") && prefs.Contains("\"LcuEnabled\":false"), "LCU : option désactivée par défaut, aucun identifiant dans les préférences");

            t(LcuParser.MapPhase("ChampSelect") == Phase.Draft, "LCU : ChampSelect devient draft");
            t(LcuParser.MapPhase("InProgress") == Phase.InGame && LcuParser.MapPhase("GameStart") == Phase.InGame, "LCU : la partie est reconnue");
            t(LcuParser.MapPhase("Lobby") == Phase.OutOfDraft && LcuParser.MapPhase("ReadyCheck") == Phase.OutOfDraft && LcuParser.MapPhase("None") == Phase.OutOfDraft, "LCU : lobby et file d'attente ne sont pas une draft");
            t(LcuParser.MapPhase("PhaseInconnue") == Phase.Uncertain, "LCU : une phase inconnue reste incertaine");
            t(LcuParser.ParsePhase("pas du json") == "", "LCU : une réponse abîmée ne provoque pas d'erreur");

            t(LcuParser.ParseAssignedRole(Session) == null, "LCU : sans poste indiqué, aucun rôle n'est deviné");
            Func<string, string, string> withPos = (mine, other) => "{\"localPlayerCellId\":2,\"myTeam\":[{\"cellId\":1,\"assignedPosition\":\"" + other + "\",\"championId\":64},{\"cellId\":2,\"assignedPosition\":\"" + mine + "\",\"championId\":105}],\"theirTeam\":[]}";
            t(LcuParser.ParseAssignedRole(withPos("jungle", "middle")) == "jungle", "LCU : le poste lu est celui du joueur, pas d'un allié");
            t(LcuParser.ParseAssignedRole(withPos("middle", "jungle")) == "mid" && LcuParser.ParseAssignedRole(withPos("bottom", "")) == "adc" && LcuParser.ParseAssignedRole(withPos("utility", "")) == "support" && LcuParser.ParseAssignedRole(withPos("top", "")) == "top", "LCU : les cinq postes sont reconnus");
            t(LcuParser.ParseAssignedRole(withPos("", "jungle")) == null && LcuParser.ParseAssignedRole(withPos("nouveau", "")) == null, "LCU : poste vide ou inconnu, rôle inconnu");
            t(LcuParser.ParseAssignedRole("{abimé") == null && LcuParser.ParseAssignedRole("{\"myTeam\":[]}") == null && LcuParser.ParseAssignedRole("{\"localPlayerCellId\":2,\"myTeam\":[{\"cellId\":3,\"assignedPosition\":\"top\"}]}") == null, "LCU : réponse abîmée ou joueur absent, rôle inconnu");
            List<int> ids = LcuParser.ParseEnemies(Session);
            t(string.Join(",", ids) == "875,254,3,40", "LCU : seuls les champions adverses verrouillés sont lus");
            t(!ids.Contains(157) && !ids.Contains(222), "LCU : les intentions de pick ne sont jamais lues");
            t(!ids.Contains(105), "LCU : ton équipe n'est pas mélangée aux adversaires");
            t(LcuParser.ParseEnemies("") == null && LcuParser.ParseEnemies("{") == null && LcuParser.ParseEnemies("{\"theirTeam\":3}") == null && LcuParser.ParseEnemies("{\"theirTeam\":\"x\"}") == null && LcuParser.ParseEnemies("{}") == null, "LCU : une session absente ou abîmée est signalée comme illisible");
            t(LcuParser.ParseEnemies("{\"theirTeam\":[]}").Count == 0, "LCU : une équipe vide reste une réponse valide");

            SimulatedLcuTransport sim = new SimulatedLcuTransport();
            LcuReader reader = new LcuReader(sim);
            LcuSnapshot a = reader.PollAsync().GetAwaiter().GetResult();
            t(a.Available && a.Phase == Phase.OutOfDraft && !sim.Requests.Contains(LcuEndpoints.Session), "LCU : hors draft, la session n'est pas demandée");
            sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = Session;
            LcuSnapshot b = reader.PollAsync().GetAwaiter().GetResult();
            t(b.Phase == Phase.Draft && b.EnemyChampionIds.Count == 4, "LCU : en draft, phase et champions sont lus");
            t(sim.Requests.All(LcuEndpoints.IsAllowed), "LCU : seuls les endpoints autorisés sont appelés");
            sim.Offline = true;
            t(!reader.PollAsync().GetAwaiter().GetResult().Available, "LCU : client injoignable signalé sans erreur");

            int opens = 0;
            SimulatedLcuTransport current = null;
            LcuSession session = new LcuSession(() => { opens++; return current; }) { RetryMs = 0, PollTimeoutMs = 100 };
            Func<LcuSnapshot> pollSession = () => Task.Run(() => session.PollAsync()).GetAwaiter().GetResult();
            t(!pollSession().Available && !session.Connected, "LCU : aucun lockfile disponible, nouvel essai plus tard");
            current = new SimulatedLcuTransport { PhaseJson = "\"Lobby\"" };
            t(pollSession().Available && session.Connected && opens == 2, "LCU : connexion dès que le client est disponible");
            pollSession();
            t(opens == 2, "LCU : les identifiants ne sont relus qu'en cas de besoin");
            current.Offline = true;
            t(!pollSession().Available && !session.Connected, "LCU : déconnexion, identifiants oubliés");
            current = new SimulatedLcuTransport { PhaseJson = "\"None\"" };
            t(pollSession().Available && opens == 3, "LCU : reconnexion avec de nouveaux identifiants");
            current.Hang = true;
            t(!pollSession().Available && !session.Connected, "LCU : requête bloquée abandonnée sans figer la lecture");
            session.RetryMs = 60000; session.Forget();
            int opensNow = opens;
            t(!pollSession().Available && opens == opensNow, "LCU : les nouvelles tentatives sont espacées");
            current = new SimulatedLcuTransport { PhaseJson = "\"ChampSelect\"", SessionJson = "{\"theirTeam\":[{\"champ" };
            t(new LcuReader(current).PollAsync().GetAwaiter().GetResult().EnemyChampionIds == null, "LCU : session tronquée pendant une draft signalée comme illisible");

            // Faux serveur local qui ne répond jamais : aucun lien avec le vrai client.
            TcpListener silent = new TcpListener(IPAddress.Loopback, 0);
            silent.Start();
            try
            {
                LocalHttpsTransport frozen = new LocalHttpsTransport(new LockfileInfo { Port = ((IPEndPoint)silent.LocalEndpoint).Port, Password = "test" });
                Stopwatch clock = Stopwatch.StartNew();
                string answer = Task.Run(() => frozen.GetAsync(LcuEndpoints.Phase)).GetAwaiter().GetResult();
                t(answer == null && clock.ElapsedMilliseconds < 4000, "LCU : un client figé est abandonné après le délai (" + clock.ElapsedMilliseconds + " ms)");
            }
            finally { silent.Stop(); }
        }
    }
}
