// Connecteur de lecture du client League of Legends, en lecture seule et VERROUILLÉ.
// Riot indique que cette API n'est pas prise en charge pour les applications tierces.
// Voir docs/LCU.md : ne pas déverrouiller sans réponse écrite de Riot.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DraftMid
{
    public static class LcuGate
    {
        // À passer à true uniquement après confirmation écrite de Riot, en recompilant.
        public static readonly bool RiotConfirmed = false;

        // L'option de l'utilisateur ne suffit jamais seule.
        public static bool Allowed(bool optionEnabled) { return RiotConfirmed && optionEnabled; }
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
        readonly LockfileInfo info;
        public LocalHttpsTransport(LockfileInfo info) { this.info = info; }

        public async Task<string> GetAsync(string path)
        {
            if (!LcuEndpoints.IsAllowed(path)) throw new InvalidOperationException("Endpoint non autorisé");
            // Adresse locale fixe : le mot de passe ne peut pas partir ailleurs.
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create("https://127.0.0.1:" + info.Port + path);
            r.Method = "GET";
            r.Timeout = 800;
            r.AllowAutoRedirect = false;
            r.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("riot:" + info.Password));
            // Certificat auto-signé du client, accepté pour cette seule requête locale.
            r.ServerCertificateValidationCallback = (s, c, ch, e) => true;
            try
            {
                using (WebResponse w = await r.GetResponseAsync())
                using (StreamReader rd = new StreamReader(w.GetResponseStream(), Encoding.UTF8))
                    return await rd.ReadToEndAsync();
            }
            catch (WebException e)
            {
                HttpWebResponse h = e.Response as HttpWebResponse;
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
        // Seuls les champions verrouillés et visibles de l'équipe adverse sont conservés.
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
        public static List<int> ParseEnemies(string json)
        {
            List<int> ids = new List<int>();
            if (string.IsNullOrWhiteSpace(json)) return ids;
            try
            {
                Dictionary<string, object> root = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.Deserialize<Dictionary<string, object>>(json);
                object team;
                if (root == null || !root.TryGetValue("theirTeam", out team)) return ids;
                IEnumerable list = team as IEnumerable;
                if (list == null) return ids;
                foreach (object o in list)
                {
                    Dictionary<string, object> m = o as Dictionary<string, object>;
                    object c;
                    if (m != null && m.TryGetValue("championId", out c) && c is int && (int)c > 0 && !ids.Contains((int)c)) ids.Add((int)c);
                }
            }
            catch { }
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
            try { s.EnemyChampionIds = LcuParser.ParseEnemies(await transport.GetAsync(LcuEndpoints.Session)); }
            catch { }
            return s;
        }
    }

    // Transport de test : données simulées, aucune connexion.
    public class SimulatedLcuTransport : ILcuTransport
    {
        public readonly List<string> Requests = new List<string>();
        public string PhaseJson = "\"None\"";
        public string SessionJson = "";
        public bool Offline;

        public Task<string> GetAsync(string path)
        {
            Requests.Add(path);
            if (!LcuEndpoints.IsAllowed(path)) throw new InvalidOperationException("Endpoint non autorisé");
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
            t(LcuGate.RiotConfirmed == false, "LCU : le verrou est fermé dans cette version");
            t(!LcuGate.Allowed(false) && !LcuGate.Allowed(true), "LCU : même l'option cochée ne permet aucune connexion");
            t(LcuFactory.CreateReal(true, Path.GetTempPath()) == null, "LCU : aucun connecteur réel n'est créé, verrou fermé");

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

            List<int> ids = LcuParser.ParseEnemies(Session);
            t(string.Join(",", ids) == "875,254,3,40", "LCU : seuls les champions adverses verrouillés sont lus");
            t(!ids.Contains(157) && !ids.Contains(222), "LCU : les intentions de pick ne sont jamais lues");
            t(!ids.Contains(105), "LCU : ton équipe n'est pas mélangée aux adversaires");
            t(LcuParser.ParseEnemies("").Count == 0 && LcuParser.ParseEnemies("{").Count == 0 && LcuParser.ParseEnemies("{\"theirTeam\":3}").Count == 0, "LCU : une session absente ou abîmée donne une liste vide");

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
        }
    }
}
