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
using System.Text.RegularExpressions;
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
        // Beta.3 : troisième adresse, changement explicite de la règle « deux adresses » (docs/technique/LCU.md). Bilan de fin de
        // partie, lu seulement en PreEndOfGame / EndOfGame pour la partie suivie : gameId, équipe du joueur, équipe gagnante, remake.
        public const string EndOfGame = "/lol-end-of-game/v1/eog-stats-block";
        public static bool IsAllowed(string path) { return path == Phase || path == Session || path == EndOfGame; }
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
        // Délai d'une lecture. Le diagnostic essaie aussi un délai plus long, pour repérer un client lent.
        public int TimeoutMs = 1500;
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
            // Le mot de passe du client ne suit jamais une redirection.
            r.AllowAutoRedirect = false;
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

    // Dossier du client à partir de son processus.
    // Process.MainModule demande un accès complet au processus : il est refusé quand le client tourne avec plus de droits
    // que DRAFT (client lancé en administrateur, par exemple). La requête limitée, elle, est permise dans ce cas.
    public static class ClientProcess
    {
        const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder name, ref int size);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        // Null si le chemin est introuvable ; error donne alors la raison, sans chemin ni identifiant.
        public static string FolderOf(int pid, out Msg error)
        {
            error = null;
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) { error = new Msg("native.diag.processDenied", "code", System.Runtime.InteropServices.Marshal.GetLastWin32Error()); return null; }
            try
            {
                StringBuilder s = new StringBuilder(1024);
                int n = s.Capacity;
                if (!QueryFullProcessImageName(h, 0, s, ref n)) { error = new Msg("native.diag.pathUnreadable", "code", System.Runtime.InteropServices.Marshal.GetLastWin32Error()); return null; }
                return Path.GetDirectoryName(s.ToString(0, n));
            }
            finally { CloseHandle(h); }
        }
    }

    public static class LcuFactory
    {
        // Étape où la dernière ouverture a échoué, pour le journal de diagnostic : jamais de chemin, de port ni de mot de passe.
        public static string LastOpenError = "";

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
                    LastOpenError = i == null ? "lockfile invalide" : "";
                    return i == null ? null : new LocalHttpsTransport(i);
                }
            }
            catch (FileNotFoundException) { LastOpenError = "lockfile absent"; return null; }
            catch (DirectoryNotFoundException) { LastOpenError = "lockfile absent"; return null; }
            catch (UnauthorizedAccessException) { LastOpenError = "lockfile refusé"; return null; }
            catch (IOException) { LastOpenError = "lockfile verrouillé"; return null; }
            catch (Exception e) { LastOpenError = "lockfile : " + e.GetType().Name; return null; }
        }
    }

    // Diagnostic de connexion, lancé par l'utilisateur depuis le menu de l'icône. Lecture seule, adresse locale uniquement.
    // Le texte produit ne contient jamais le mot de passe, le port, le chemin du client ni aucun identifiant de joueur.
    public static class LcuDiagnostic
    {
        public static bool IsElevated()
        {
            try { return new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator); }
            catch { return false; }
        }

        // clientPid : processus de la fenêtre du client, 0 si aucune. folderForTests : dossier imposé par les tests.
        public static async Task<string> RunAsync(bool optionEnabled, int clientPid, string version, string folderForTests = null)
        {
            StringBuilder sb = new StringBuilder();
            // Une ligne par clé du catalogue (native.diag.*), dans la langue du programme : le rapport se lit et s'envoie tel quel.
            Action<string, object[]> line = (key, pairs) => sb.AppendLine(Strings.T(key, pairs));
            line("native.diag.title", new object[] { "version", version, "windows", Environment.OSVersion.Version.ToString() });
            line("native.diag.noSecrets", null);
            line(IsElevated() ? "native.diag.elevatedYes" : "native.diag.elevatedNo", null);
            if (!LcuGate.Allowed(optionEnabled)) { line("native.diag.off", null); line("native.diag.offConclusion", null); return sb.ToString(); }
            string folder = folderForTests;
            if (folder == null)
            {
                if (clientPid <= 0) { line("native.diag.noWindow", null); line("native.diag.noWindowConclusion", null); return sb.ToString(); }
                line("native.diag.window", null);
                bool full;
                try { using (Process p = Process.GetProcessById(clientPid)) { string m = p.MainModule.FileName; full = m != null; } } catch { full = false; }
                line(full ? "native.diag.fullAccessYes" : "native.diag.fullAccessNo", null);
                Msg err;
                folder = ClientProcess.FolderOf(clientPid, out err);
                if (folder == null) { line("native.diag.noFolder", new object[] { "error", err }); line("native.diag.noFolderConclusion", null); return sb.ToString(); }
                line("native.diag.folder", null);
            }
            string path = Path.Combine(folder, "lockfile");
            if (!File.Exists(path)) { line("native.diag.noLockfile", null); line("native.diag.noLockfileConclusion", null); return sb.ToString(); }
            LockfileInfo info;
            try { using (FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (StreamReader r = new StreamReader(f)) info = LockfileInfo.Parse(r.ReadToEnd()); }
            catch (Exception e) { line("native.diag.lockfileUnreadable", new object[] { "error", e.GetType().Name }); line("native.diag.lockfileUnreadableConclusion", null); return sb.ToString(); }
            if (info == null) { line("native.diag.lockfileFormat", null); line("native.diag.lockfileFormatConclusion", null); return sb.ToString(); }
            line("native.diag.lockfileOk", null);
            Uri local = new Uri("https://127.0.0.1:" + info.Port + "/");
            bool proxied = false;
            try { IWebProxy px = WebRequest.DefaultWebProxy; proxied = px != null && !px.IsBypassed(local); } catch { }
            line(proxied ? "native.diag.proxyUsed" : "native.diag.proxyNotUsed", null);
            Stopwatch clock = Stopwatch.StartNew();
            Msg tcp;
            bool tcpOpen = false;
            using (TcpClient c = new TcpClient())
            {
                try
                {
                    Task connect = c.ConnectAsync(IPAddress.Loopback, info.Port);
                    if (await Task.WhenAny(connect, Task.Delay(2000)) != connect) tcp = new Msg("native.diag.tcpTimeout");
                    else if (connect.IsFaulted) tcp = new Msg("native.diag.tcpRefused");
                    else { tcp = new Msg("native.diag.tcpOpen", "ms", clock.ElapsedMilliseconds); tcpOpen = true; }
                }
                catch (Exception e) { tcp = new Msg("native.diag.tcpError", "error", e.GetType().Name); }
            }
            line("native.diag.tcp", new object[] { "result", tcp });
            foreach (int timeout in new[] { 1500, 6000 })
            {
                LocalHttpsTransport t = new LocalHttpsTransport(info) { TimeoutMs = timeout };
                clock.Restart();
                string body = await t.GetAsync(LcuEndpoints.Phase);
                string phase = body == null ? "" : LcuParser.ParsePhase(body);
                if (body != null && phase != "") { line("native.diag.phaseOk", new object[] { "timeout", timeout, "ms", clock.ElapsedMilliseconds, "phase", phase }); line(timeout == 1500 ? "native.diag.phaseOkConclusion" : "native.diag.phaseSlowConclusion", null); return sb.ToString(); }
                // L'erreur réseau est un nom technique (type d'exception, code HTTP), identique dans toutes les langues.
                line("native.diag.phaseFailed", new object[] { "timeout", timeout, "error", body == "" ? (object)new Msg("native.diag.emptyOr404") : LocalHttpsTransport.LastError, "ms", clock.ElapsedMilliseconds });
            }
            line(proxied ? "native.diag.proxyConclusion" : tcpOpen ? "native.diag.refusedConclusion" : "native.diag.blockedConclusion", null);
            return sb.ToString();
        }
    }

    // Sessions de sélection au FORMAT RÉEL du client (champs, imbrications, nombres longs), valeurs fictives : selftest et e2e seulement.
    // localCell : localPlayerCellId ; playerCell : case du joueur dans myTeam (une autre valeur = joueur absent de son équipe).
    public static class LcuSamples
    {
        // Bilan de fin de partie (/lol-end-of-game/v1/eog-stats-block) au format réel, RECONSTITUÉ d'après les champs connus du client
        // (non documentés par Riot) : aucune capture réelle n'est versionnée. Tous les joueurs portent des noms, puuid, summonerId et
        // statistiques fictifs (Joueur, Allie1.., Adverse1..) : rien de cela ne doit se retrouver dans ce qui est lu.
        // win : ton équipe gagne ; remake : partie terminée par un remake (gameEndedInEarlySurrender) ; tag : change noms, puuid et
        // statistiques de tous les joueurs (test négatif) ; localTeam : false = sans localPlayer.teamId (repli isPlayerTeam).
        public static string EndOfGameBlock(long gameId, bool win, bool remake, string tag = "A", bool localTeam = true)
        {
            Func<string, int, int, bool, string> player = (name, team, champ, me) => "{\"botPlayer\":false,\"championId\":" + champ + ",\"championName\":\"Champion" + champ + "\",\"detectedTeamPosition\":\"MIDDLE\",\"gameId\":" + gameId
                + ",\"isLocalPlayer\":" + (me ? "true" : "false") + ",\"leaver\":false,\"leaves\":0,\"level\":18,\"mapId\":11,\"puuid\":\"" + tag + "-puuid-" + name + "-0000-4000-8000\",\"riotIdGameName\":\"" + name + tag + "\",\"riotIdTagLine\":\"" + tag + "TAG\",\"selectedPosition\":\"MIDDLE\",\"skinSplashPath\":\"\",\"spell1Id\":4,\"spell2Id\":14,"
                + "\"stats\":{\"ASSISTS\":" + (tag.Length + 3) + ",\"CHAMPIONS_KILLED\":" + (tag.Length * 7) + ",\"GOLD_EARNED\":" + (tag.Length * 12345) + ",\"LOSE\":" + (win == (team == 100) ? 0 : 1) + ",\"NUM_DEATHS\":" + (tag.Length + 1) + ",\"TOTAL_DAMAGE_DEALT_TO_CHAMPIONS\":" + (tag.Length * 23456) + ",\"WIN\":" + (win == (team == 100) ? 1 : 0) + "},"
                + "\"summonerId\":" + (2834567890000L + champ) + ",\"summonerName\":\"" + name + tag + "\",\"teamId\":" + team + "}";
            string mine = player("Joueur", 100, 4, true);
            string allies = string.Join(",", new[] { 266, 64, 22, 412 }.Select((c, i) => player("Allie" + i, 100, c, false)));
            string enemies = string.Join(",", new[] { 103, 516, 254, 222, 117 }.Select((c, i) => player("Adverse" + i, 200, c, false)));
            Func<int, bool, string, string> side = (id, mineTeam, list) => "{\"fullId\":\"\",\"isBottomTeam\":" + (id == 100 ? "true" : "false") + ",\"isPlayerTeam\":" + (mineTeam ? "true" : "false") + ",\"isWinningTeam\":" + ((id == 100) == win ? "true" : "false")
                + ",\"memberStatusString\":\"\",\"name\":\"\",\"players\":[" + list + "],\"stats\":{\"CHAMPIONS_KILLED\":" + (tag.Length * 30) + "},\"tag\":\"\",\"teamId\":" + id + "}";
            string local = localTeam ? mine : mine.Replace(",\"teamId\":100}", "}");
            return "{\"accountId\":2834567890999,\"basePoints\":0,\"battleBoostIpEarned\":0,\"causedEarlySurrender\":false,\"championId\":4,\"currentLevel\":212,\"difficulty\":\"\",\"earlySurrenderAccomplice\":false,"
                + "\"experienceEarned\":0,\"gameEndedInEarlySurrender\":" + (remake ? "true" : "false") + ",\"gameId\":" + gameId + ",\"gameLength\":" + (remake ? 210 : 1834) + ",\"gameMode\":\"CLASSIC\",\"gameMutators\":[],\"gameType\":\"MATCHED_GAME\","
                + "\"invalid\":false,\"isRankedGame\":true,\"leveledUp\":false,\"localPlayer\":" + local + ",\"myTeamStatus\":\"\",\"queueType\":\"RANKED_SOLO_5x5\",\"ranked\":true,\"reportGameId\":" + gameId + ",\"roomName\":\"\","
                + "\"summonerId\":2834567890004,\"summonerName\":\"Joueur" + tag + "\",\"teamBoost\":null,\"teamEarlySurrendered\":" + (remake ? "true" : "false") + ",\"teams\":[" + side(100, true, mine + "," + allies) + "," + side(200, false, enemies) + "],"
                + "\"timeUntilNextFirstWinBonus\":0,\"userId\":2834567890999}";
        }

        public static string RealSession(string position, int localCell, int playerCell, params int[] enemies)
        {
            string member = "\"cellId\":{0},\"championId\":0,\"championPickIntent\":{1},\"entitledFeatureType\":\"NONE\",\"nameVisibilityType\":\"{2}\",\"obfuscatedPuuid\":\"\",\"obfuscatedSummonerId\":0,\"puuid\":\"{3}\",\"selectedSkinId\":0,\"spell1Id\":11,\"spell2Id\":4,\"summonerId\":{4},\"team\":2,\"wardSkinId\":-1";
            string ally = "{\"assignedPosition\":\"top\"," + string.Format(member, localCell == 5 ? 6 : 5, 0, "HIDDEN", "", 0) + "}";
            string me = "{\"assignedPosition\":\"" + position + "\"," + string.Format(member, playerCell, 64, "VISIBLE", "00000000-0000-4000-8000-000000000000", 2834567890123L) + "}";
            string their = string.Join(",", enemies.Select((id, i) => "{\"assignedPosition\":\"\",\"cellId\":" + i + ",\"championId\":" + id + ",\"championPickIntent\":0,\"summonerId\":0,\"team\":1}"));
            return "{\"actions\":[[{\"actorCellId\":" + localCell + ",\"championId\":0,\"completed\":false,\"id\":1,\"isAllyAction\":true,\"isInProgress\":true,\"pickTurn\":1,\"type\":\"ban\"}]],"
                + "\"allowBattleBoost\":false,\"allowRerolling\":false,\"bans\":{\"myTeamBans\":[],\"numBans\":10,\"theirTeamBans\":[]},\"benchChampions\":[],\"benchEnabled\":false,"
                + "\"chatDetails\":{\"mucJwtDto\":{\"channelClaim\":\"\",\"domain\":\"\",\"jwt\":\"\",\"targetRegion\":\"\"},\"multiUserChatId\":\"\",\"multiUserChatPassword\":\"\"},"
                + "\"counter\":-1,\"gameId\":7012345678,\"hasSimultaneousBans\":true,\"hasSimultaneousPicks\":false,\"isCustomGame\":false,\"isSpectating\":false,"
                + "\"localPlayerCellId\":" + localCell + ",\"lockedEventIndex\":-1,\"myTeam\":[" + ally + "," + me + "],\"pickOrderSwaps\":[],\"recoveryCounter\":0,\"rerollsRemaining\":0,"
                + "\"skipChampionSelect\":false,\"theirTeam\":[" + their + "],\"timer\":{\"adjustedTimeLeftInPhase\":29854,\"internalNowInEpochMs\":1763290000000,\"isInfinite\":false,\"phase\":\"BAN_PICK\",\"totalTimeInPhase\":30000},\"trades\":[]}";
        }

        // Sélection classique au format réel avec TON action de pick (beta.2). Un coéquipier a lui aussi survolé, verrouillé et une
        // intention (allyChampion, allyIntent) : ces valeurs ne doivent jamais se retrouver dans ce qui est lu. position vide : sans poste.
        // pickAction faux : aucune action de pick (mode à banc, ARAM). gameId 0 : absent.
        public static string PickSession(string position, int localCell, int actionChampion, bool inProgress, bool completed, int intent, int myChampion, string timerPhase, long gameId, int allyChampion, int allyIntent, bool pickAction, params int[] enemies)
        {
            int allyCell = localCell == 5 ? 6 : 5;
            string member = "\"cellId\":{0},\"championId\":{1},\"championPickIntent\":{2},\"entitledFeatureType\":\"NONE\",\"nameVisibilityType\":\"{3}\",\"obfuscatedPuuid\":\"\",\"obfuscatedSummonerId\":0,\"puuid\":\"{4}\",\"selectedSkinId\":0,\"spell1Id\":11,\"spell2Id\":4,\"summonerId\":{5},\"team\":2,\"wardSkinId\":-1";
            string ally = "{\"assignedPosition\":\"top\"," + string.Format(member, allyCell, allyChampion, allyIntent, "HIDDEN", "", 0) + "}";
            string me = "{\"assignedPosition\":\"" + position + "\"," + string.Format(member, localCell, myChampion, intent, "VISIBLE", "00000000-0000-4000-8000-000000000000", 2834567890123L) + "}";
            string their = string.Join(",", enemies.Select((id, i) => "{\"assignedPosition\":\"\",\"cellId\":" + i + ",\"championId\":" + id + ",\"championPickIntent\":0,\"summonerId\":0,\"team\":1}"));
            Func<int, int, bool, bool, string, string> action = (actor, champ, done, busy, type) => "{\"actorCellId\":" + actor + ",\"championId\":" + champ + ",\"completed\":" + (done ? "true" : "false") + ",\"id\":" + (actor * 10 + (type == "pick" ? 2 : 1)) + ",\"isAllyAction\":true,\"isInProgress\":" + (busy ? "true" : "false") + ",\"pickTurn\":1,\"type\":\"" + type + "\"}";
            string actions = "[[" + action(localCell, 0, true, false, "ban") + "," + action(allyCell, 0, true, false, "ban") + "]"
                + (pickAction ? ",[" + action(allyCell, allyChampion, allyChampion > 0, allyChampion == 0, "pick") + "],[" + action(localCell, actionChampion, completed, inProgress, "pick") + "]" : "") + "]";
            return "{\"actions\":" + actions + ",\"allowBattleBoost\":false,\"allowRerolling\":false,\"bans\":{\"myTeamBans\":[],\"numBans\":10,\"theirTeamBans\":[]},\"benchChampions\":[],\"benchEnabled\":" + (pickAction ? "false" : "true") + ","
                + "\"counter\":-1," + (gameId > 0 ? "\"gameId\":" + gameId + "," : "") + "\"hasSimultaneousBans\":true,\"hasSimultaneousPicks\":false,\"isCustomGame\":false,\"isSpectating\":false,"
                + "\"localPlayerCellId\":" + localCell + ",\"lockedEventIndex\":-1,\"myTeam\":[" + ally + "," + me + "],\"pickOrderSwaps\":[],\"recoveryCounter\":0,\"rerollsRemaining\":0,"
                + "\"skipChampionSelect\":false,\"theirTeam\":[" + their + "],\"timer\":{\"adjustedTimeLeftInPhase\":29854,\"internalNowInEpochMs\":1763290000000,\"isInfinite\":false,\"phase\":\"" + timerPhase + "\",\"totalTimeInPhase\":30000},\"trades\":[]}";
        }

        // Sélection classique au format réel avec les bans et les picks des deux équipes (beta.2, disponibilité). Ton équipe : cases 0 à 4
        // si localCell < 5 (côté bleu), sinon 5 à 9. team : championId des cinq cases de ton équipe, dans l'ordre (survol ou verrouillage,
        // comme le client l'affiche) ; intents : leurs championPickIntent. Chaque coéquipier porte un nom, un tag, un puuid et un
        // summonerId fictifs (Coequipier, SEC, 2222-4333, 28345678901..) : ils ne doivent jamais se retrouver dans ce qui est lu.
        // actions : groupes d'actions « acteur:type:champion:état », état done (terminée), busy (en cours) ou wait (à venir).
        // enemies : championId des cases adverses (0 : pas encore choisi).
        public static string DraftSession(int localCell, string timerPhase, string[][] actions, int[] myTeamBans, int[] theirTeamBans, int[] team, int[] intents, params int[] enemies)
        {
            int mine = localCell < 5 ? 0 : 5, theirs = 5 - mine;
            string[] positions = { "top", "jungle", "middle", "bottom", "utility" };
            string member = "{{\"assignedPosition\":\"{0}\",\"cellId\":{1},\"championId\":{2},\"championPickIntent\":{3},\"entitledFeatureType\":\"NONE\",\"gameName\":\"{4}\",\"internalName\":\"\",\"isHumanoid\":false,\"nameVisibilityType\":\"{5}\",\"obfuscatedPuuid\":\"\",\"obfuscatedSummonerId\":0,\"pickMode\":0,\"pickTurn\":0,\"playerAlias\":\"\",\"playerType\":\"PLAYER\",\"puuid\":\"{6}\",\"selectedSkinId\":0,\"spell1Id\":4,\"spell2Id\":14,\"summonerId\":{7},\"tagLine\":\"{8}\",\"team\":{9},\"wardSkinId\":-1}}";
            string my = string.Join(",", Enumerable.Range(0, 5).Select(i => string.Format(member, positions[i], mine + i, team[i], intents[i], mine + i == localCell ? "Joueur" : "Coequipier" + i,
                mine + i == localCell ? "VISIBLE" : "HIDDEN", "1111111" + i + "-2222-4333-8444-555555555555", 2834567890100L + i, "SEC" + i, mine == 0 ? 1 : 2)));
            string their = string.Join(",", enemies.Select((id, i) => "{\"assignedPosition\":\"\",\"cellId\":" + (theirs + i) + ",\"championId\":" + id + ",\"championPickIntent\":0,\"entitledFeatureType\":\"\",\"gameName\":\"\",\"isHumanoid\":false,\"nameVisibilityType\":\"UNHIDDEN\",\"obfuscatedPuuid\":\"\",\"obfuscatedSummonerId\":0,\"puuid\":\"\",\"selectedSkinId\":0,\"spell1Id\":0,\"spell2Id\":0,\"summonerId\":0,\"tagLine\":\"\",\"team\":" + (mine == 0 ? 2 : 1) + ",\"wardSkinId\":-1}"));
            int n = 0;
            Func<string, string> action = spec =>
            {
                string[] p = spec.Split(':');
                int actor = int.Parse(p[0]);
                bool ally = actor >= mine && actor < mine + 5;
                n++;
                return "{\"actorCellId\":" + actor + ",\"championId\":" + p[2] + ",\"completed\":" + (p[3] == "done" ? "true" : "false") + ",\"id\":" + n + ",\"isAllyAction\":" + (ally ? "true" : "false")
                    + ",\"isInProgress\":" + (p[3] == "busy" ? "true" : "false") + ",\"pickTurn\":" + n + ",\"type\":\"" + p[1] + "\"}";
            };
            string acts = "[" + string.Join(",", actions.Select(g => "[" + string.Join(",", g.Select(action)) + "]")) + "]";
            return "{\"actions\":" + acts + ",\"allowBattleBoost\":false,\"allowDuplicatePicks\":false,\"allowLockedEvents\":false,\"allowRerolling\":false,\"allowSkinSelection\":true,"
                + "\"bans\":{\"myTeamBans\":[" + string.Join(",", myTeamBans) + "],\"numBans\":10,\"theirTeamBans\":[" + string.Join(",", theirTeamBans) + "]},\"benchChampions\":[],\"benchEnabled\":false,\"boostableSkinCount\":1,"
                + "\"chatDetails\":{\"mucJwtDto\":{\"channelClaim\":\"\",\"domain\":\"\",\"jwt\":\"\",\"targetRegion\":\"\"},\"multiUserChatId\":\"\",\"multiUserChatPassword\":\"\"},"
                + "\"counter\":-1,\"gameId\":7012345678,\"hasSimultaneousBans\":true,\"hasSimultaneousPicks\":false,\"isCustomGame\":false,\"isLegacyChampSelect\":false,\"isSpectating\":false,"
                + "\"localPlayerCellId\":" + localCell + ",\"lockedEventIndex\":-1,\"myTeam\":[" + my + "],\"pickOrderSwaps\":[],\"positionSwaps\":[],\"recoveryCounter\":0,\"rerollsRemaining\":0,"
                + "\"showQuitButton\":false,\"skipChampionSelect\":false,\"theirTeam\":[" + their + "],\"timer\":{\"adjustedTimeLeftInPhase\":29854,\"internalNowInEpochMs\":1763290000000,\"isInfinite\":false,\"phase\":\"" + timerPhase + "\",\"totalTimeInPhase\":30000},\"trades\":[]}";
        }
    }

    // Résultat lu dans le bilan de fin de partie (beta.3). État : lu, absent (pas de bilan), illisible (tronqué, champs absents),
    // remake (aucun résultat). GameId et Win seulement : aucun nom, aucun identifiant de joueur, aucune statistique.
    public class EndOfGameResult
    {
        public const string Read = "lu", Missing = "absent", Unreadable = "illisible", Remake = "remake";
        public string State = Missing;
        public long GameId;
        public bool Win;
    }

    // Les SEULS champs du bilan connus de DRAFT. Le reste de la réponse (noms, riotId, puuid, summonerId, statistiques de chaque
    // joueur, y compris les tiennes) n'a aucun champ ici : le désérialiseur l'écarte, le code ne le lit jamais.
    class EogBlock
    {
        public long gameId { get; set; }
        public EogLocal localPlayer { get; set; }
        public List<EogTeam> teams { get; set; }
        public bool? gameEndedInEarlySurrender { get; set; }
        public bool? invalid { get; set; }
    }
    class EogLocal { public int? teamId { get; set; } }
    class EogTeam
    {
        public int? teamId { get; set; }
        public bool? isWinningTeam { get; set; }
        public bool? isPlayerTeam { get; set; }
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
        // Ton pick (beta.2, journal automatique) : ta case seulement. Null hors draft ou session illisible.
        public LocalPick Pick;
        // Champions indisponibles (beta.2, disponibilité) : bannis, pris par un allié, pris en face. Null hors draft ou session illisible.
        public List<UnavailableChampion> Unavailable;
    }

    // Champion qui n'est plus disponible dans la sélection (beta.2). Identifiant de champion et raison SEULEMENT : jamais de nom,
    // d'identifiant de joueur, de puuid, de case ni d'intention. Raisons : banni, pris par un allié, pris par l'adversaire.
    public class UnavailableChampion
    {
        public const string Ban = "ban", Ally = "ally", Enemy = "enemy";
        public int Id;
        public string Reason;
    }

    // Champions indisponibles de la sélection suivie (beta.2) : liste versionnée, comme « Dans League ». V augmente à chaque changement ;
    // une page ignore un état plus ancien que le sien. Session illisible : la dernière liste connue reste. Lecture perdue : elle reste
    // aussi (un ban ou un pick déjà vu ne revient pas). Nouvelle sélection : liste vide.
    public class AvailabilityTracker
    {
        public int V { get; private set; }
        public string Key { get; private set; }
        List<UnavailableChampion> items = new List<UnavailableChampion>();
        public IList<UnavailableChampion> Items { get { return items.AsReadOnly(); } }

        public bool Begin(string key)
        {
            if (key == Key) return false;
            Key = key; items = new List<UnavailableChampion>();
            V++;
            return true;
        }

        // La liste lue remplace l'ancienne (un échange entre alliés la modifie). Même contenu, dans un autre ordre : rien ne change.
        public bool Read(List<UnavailableChampion> list)
        {
            if (list == null || Signature(list) == Signature(items)) return false;
            items = list.Select(u => new UnavailableChampion { Id = u.Id, Reason = u.Reason }).ToList();
            V++;
            return true;
        }

        static string Signature(IEnumerable<UnavailableChampion> list) { return string.Join(",", list.OrderBy(u => u.Id).Select(u => u.Id + ":" + u.Reason)); }
        public int Count(string reason) { return items.Count(u => u.Reason == reason); }

        public Dictionary<string, object> ToState()
        {
            return new Dictionary<string, object> { { "v", V }, { "key", Key },
                { "items", items.Select(u => new Dictionary<string, object> { { "id", u.Id }, { "reason", u.Reason } }).ToList() } };
        }
    }

    // Pick du joueur local, lu dans la session déjà lue (beta.2, journal automatique). Aucune donnée de coéquipier, aucun nom.
    // Champs non documentés par Riot : au moindre doute, l'état reste « aucun » et « Je joue » sert de repli.
    public class LocalPick
    {
        public const string None = "none", Hover = "hover", Locked = "locked", Final = "final";
        // Joueur trouvé dans son équipe.
        public bool Readable;
        // Draft classique : une action de pick du joueur et un poste attribué. Les autres modes (ARAM, aveugle...) ne sont pas pris
        // en charge : rien n'est noté seul. Ajouter un mode = élargir cette règle, le reste suit.
        public bool Supported;
        public string State = None;
        // Champion affiché : survolé (action en cours, sinon présélection), verrouillé, ou final après un échange.
        public int Champion;
        public int Intent, LockedId, FinalId;
        public string TimerPhase = "";
        public long GameId;
    }

    // « Dans League » pendant une sélection : état du pick local, premier verrouillage, échange, identifiant de partie.
    // Versionné : V augmente à chaque changement, une page ignore un état plus ancien que le sien. Identifiants de champions seulement.
    public class LeaguePickTracker
    {
        public int V { get; private set; }
        public string Key { get; private set; }
        public bool Supported { get; private set; }
        public bool Readable { get; private set; }
        public bool Lost { get; private set; }
        public string State { get; private set; }
        public int Champion { get; private set; }
        public int LockedFirst { get; private set; }
        public bool Traded { get; private set; }
        public long GameId { get; private set; }

        public LeaguePickTracker() { State = LocalPick.None; }

        // Nouvelle sélection (clé du suivi de l'issue) : tout repart de zéro.
        public bool Begin(string key)
        {
            if (key == Key) return false;
            Key = key; Supported = Readable = Lost = Traded = false; State = LocalPick.None; Champion = LockedFirst = 0; GameId = 0;
            V++;
            return true;
        }

        // Session lue. Joueur introuvable : le dernier état connu reste.
        public bool Read(LocalPick p)
        {
            string before = Signature();
            Lost = false;
            if (p != null && p.Readable)
            {
                Readable = true; Supported = p.Supported; State = p.State; Champion = p.Champion;
                if (p.GameId > 0) GameId = p.GameId;
                if (LockedFirst == 0 && p.LockedId > 0) LockedFirst = p.LockedId;
                Traded = LockedFirst > 0 && Champion > 0 && (State == LocalPick.Locked || State == LocalPick.Final) && Champion != LockedFirst;
            }
            return Changed(before);
        }

        // Lecture du client perdue pendant la sélection : « Je joue » revient. Le dernier pick lu reste connu.
        public bool Lose() { string before = Signature(); Lost = true; return Changed(before); }

        string Signature() { return string.Join("|", Supported, Readable, Lost, State, Champion, LockedFirst, Traded, GameId); }
        bool Changed(string before) { if (Signature() == before) return false; V++; return true; }

        public Dictionary<string, object> ToState()
        {
            return new Dictionary<string, object> { { "v", V }, { "key", Key }, { "supported", Supported }, { "readable", Readable }, { "lost", Lost }, { "state", State },
                { "champion", Champion }, { "lockedFirst", LockedFirst }, { "traded", Traded }, { "gameId", GameId } };
        }

        // Diagnostic : l'état seulement, sans nom ni identifiant.
        public string DiagWord { get { return Word(State); } }
        public static string Word(string state) { return state == LocalPick.Hover ? "survolé" : state == LocalPick.Locked ? "verrouillé" : state == LocalPick.Final ? "final" : "aucun"; }
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

        // Bilan de fin de partie (beta.3). Équipe du joueur : localPlayer.teamId, sinon la seule équipe isPlayerTeam. Résultat :
        // isWinningTeam de cette équipe. Remake (gameEndedInEarlySurrender) ou partie invalide : aucun résultat. Bilan vide : absent.
        // Tronqué, type inattendu, gameId ou équipe introuvable : illisible. Rien n'est deviné.
        public static EndOfGameResult ParseEndOfGame(string json)
        {
            EndOfGameResult r = new EndOfGameResult();
            if (string.IsNullOrWhiteSpace(json)) return r;
            EogBlock b;
            try { b = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.Deserialize<EogBlock>(json); }
            catch { r.State = EndOfGameResult.Unreadable; return r; }
            if (b == null) { r.State = EndOfGameResult.Unreadable; return r; }
            if (b.gameEndedInEarlySurrender == true || b.invalid == true) { r.State = EndOfGameResult.Remake; return r; }
            if (b.gameId <= 0 || b.teams == null) { r.State = EndOfGameResult.Unreadable; return r; }
            int? mine = b.localPlayer != null ? b.localPlayer.teamId : null;
            EogTeam team = mine.HasValue ? b.teams.FirstOrDefault(x => x != null && x.teamId == mine) : null;
            if (team == null && !mine.HasValue) { List<EogTeam> own = b.teams.Where(x => x != null && x.isPlayerTeam == true).ToList(); if (own.Count == 1) team = own[0]; }
            if (team == null || !team.isWinningTeam.HasValue) { r.State = EndOfGameResult.Unreadable; return r; }
            r.State = EndOfGameResult.Read; r.GameId = b.gameId; r.Win = team.isWinningTeam.Value;
            return r;
        }

        public static string ParsePhase(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "";
            try { return new JavaScriptSerializer().Deserialize<string>(json) ?? ""; } catch { return ""; }
        }

        // Ne lit volontairement ni nom, ni identifiant de joueur, ni sort, ni intention. Seul TON pick est lu (ParseLocalPick) ; d'un
        // coéquipier, seul le champion qu'il a VERROUILLÉ compte, comme champion indisponible (ParseUnavailable).
        // Poste du joueur (myTeam[cellId == localPlayerCellId].assignedPosition). Champ non documenté par Riot.
        // Vide en sélection aveugle ou dans les modes sans poste : on renvoie null, rien n'est deviné.
        public static string ParseAssignedRole(string json)
        {
            try
            {
                // Même limite que ParseEnemies : une session lue pour les adversaires l'est aussi pour le poste.
                Dictionary<string, object> root = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.DeserializeObject(json) as Dictionary<string, object>;
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

        // Ton pick (beta.2, journal automatique). Seulement :
        // - l'action « pick » dont actorCellId == localPlayerCellId : survolée si isInProgress et pas completed, verrouillée si completed ;
        // - ta case de myTeam (cellId == localPlayerCellId) : championPickIntent (présélection), championId (final, après échange),
        //   assignedPosition ;
        // - timer.phase et gameId de la session.
        // Les autres cases de myTeam ne sont pas ouvertes : seule leur cellId est comparée. Aucun nom, aucun identifiant de joueur.
        public static LocalPick ParseLocalPick(string json)
        {
            LocalPick p = new LocalPick();
            if (string.IsNullOrWhiteSpace(json)) return p;
            try
            {
                Dictionary<string, object> root = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.DeserializeObject(json) as Dictionary<string, object>;
                object cellO, teamO;
                if (root == null || !root.TryGetValue("localPlayerCellId", out cellO) || !(cellO is int) || !root.TryGetValue("myTeam", out teamO) || !(teamO is object[])) return p;
                int cell = (int)cellO;
                Dictionary<string, object> me = null;
                foreach (object o in (object[])teamO)
                {
                    Dictionary<string, object> m = o as Dictionary<string, object>;
                    object id;
                    if (m != null && m.TryGetValue("cellId", out id) && id is int && (int)id == cell) { me = m; break; }
                }
                if (me == null) return p;
                p.Readable = true;
                p.Intent = PositiveInt(me, "championPickIntent");
                p.FinalId = PositiveInt(me, "championId");
                object pos;
                bool position = me.TryGetValue("assignedPosition", out pos) && pos is string && ((string)pos).Trim() != "";
                bool hasPick = false;
                int hover = 0;
                object actionsO;
                if (root.TryGetValue("actions", out actionsO) && actionsO is object[])
                    foreach (object group in (object[])actionsO)
                    {
                        if (!(group is object[])) continue;
                        foreach (object a in (object[])group)
                        {
                            Dictionary<string, object> m = a as Dictionary<string, object>;
                            object actor, type, done, busy;
                            if (m == null || !m.TryGetValue("actorCellId", out actor) || !(actor is int) || (int)actor != cell) continue;
                            if (!m.TryGetValue("type", out type) || !"pick".Equals(type)) continue;
                            hasPick = true;
                            int champion = PositiveInt(m, "championId");
                            bool completed = m.TryGetValue("completed", out done) && done is bool && (bool)done;
                            bool inProgress = m.TryGetValue("isInProgress", out busy) && busy is bool && (bool)busy;
                            if (completed && champion > 0) p.LockedId = champion;
                            else if (inProgress && champion > 0) hover = champion;
                        }
                    }
                p.Supported = hasPick && position;
                object timerO, phaseO, gameO;
                Dictionary<string, object> timer = root.TryGetValue("timer", out timerO) ? timerO as Dictionary<string, object> : null;
                if (timer != null && timer.TryGetValue("phase", out phaseO) && phaseO is string) p.TimerPhase = (string)phaseO;
                if (root.TryGetValue("gameId", out gameO)) p.GameId = gameO is int ? (int)gameO : gameO is long ? (long)gameO : 0;
                if (p.GameId < 0) p.GameId = 0;
                if (p.LockedId > 0)
                {
                    p.State = p.TimerPhase == "FINALIZATION" || p.TimerPhase == "GAME_STARTING" ? LocalPick.Final : LocalPick.Locked;
                    p.Champion = p.FinalId > 0 ? p.FinalId : p.LockedId;
                }
                else if (hover > 0 || p.Intent > 0) { p.State = LocalPick.Hover; p.Champion = hover > 0 ? hover : p.Intent; }
            }
            catch { return new LocalPick(); }
            return p;
        }

        static int PositiveInt(Dictionary<string, object> m, string key)
        {
            object v;
            return m.TryGetValue(key, out v) && v is int && (int)v > 0 ? (int)v : 0;
        }

        // Champions indisponibles (beta.2, disponibilité), dans la session déjà lue. Identifiants de champions seulement :
        // - bannis : actions « ban » TERMINÉES (completed) des deux équipes, et bans.myTeamBans / bans.theirTeamBans ;
        // - pris par un allié : action « pick » TERMINÉE d'une autre case de ton équipe (cellId de myTeam différente de
        //   localPlayerCellId). Champion : myTeam[sa case].championId (à jour après un échange), sinon celui de l'action ;
        // - pris en face : theirTeam[].championId, comme ParseEnemies.
        // Un survol (action en cours), une présélection (championPickIntent), un ban à venir ou un championId sans action terminée ne
        // rendent JAMAIS un champion indisponible. Dans les cases alliées, seuls cellId et, après verrouillage, championId sont lus :
        // ni nom, ni identifiant de joueur, ni puuid, ni intention, ni sort, ni poste. Ton propre pick n'est pas indisponible.
        // Null : session absente ou abîmée (la dernière liste connue reste).
        public static List<UnavailableChampion> ParseUnavailable(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                Dictionary<string, object> root = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null) return null;
                List<UnavailableChampion> list = new List<UnavailableChampion>();
                Action<int, string> add = (id, reason) => { if (id > 0 && !list.Any(u => u.Id == id)) list.Add(new UnavailableChampion { Id = id, Reason = reason }); };
                object cellO, teamO, actionsO, bansO, theirO;
                // Cases alliées, repérées par leur cellId. Sans ta case, aucun pick allié n'est lu (rien n'est deviné).
                Dictionary<int, Dictionary<string, object>> allies = new Dictionary<int, Dictionary<string, object>>();
                if (root.TryGetValue("localPlayerCellId", out cellO) && cellO is int && root.TryGetValue("myTeam", out teamO) && teamO is object[])
                    foreach (object o in (object[])teamO)
                    {
                        Dictionary<string, object> m = o as Dictionary<string, object>;
                        object id;
                        if (m != null && m.TryGetValue("cellId", out id) && id is int && (int)id != (int)cellO) allies[(int)id] = m;
                    }
                List<int> allyPicks = new List<int>();
                if (root.TryGetValue("actions", out actionsO) && actionsO is object[])
                    foreach (object group in (object[])actionsO)
                    {
                        if (!(group is object[])) continue;
                        foreach (object a in (object[])group)
                        {
                            Dictionary<string, object> m = a as Dictionary<string, object>;
                            object type, done, actor;
                            if (m == null || !m.TryGetValue("completed", out done) || !(done is bool) || !(bool)done || !m.TryGetValue("type", out type)) continue;
                            int champion = PositiveInt(m, "championId");
                            if ("ban".Equals(type)) add(champion, UnavailableChampion.Ban);
                            else if ("pick".Equals(type) && champion > 0 && m.TryGetValue("actorCellId", out actor) && actor is int && allies.ContainsKey((int)actor))
                            {
                                int now = PositiveInt(allies[(int)actor], "championId");
                                allyPicks.Add(now > 0 ? now : champion);
                            }
                        }
                    }
                Dictionary<string, object> bans = root.TryGetValue("bans", out bansO) ? bansO as Dictionary<string, object> : null;
                if (bans != null)
                    foreach (string side in new[] { "myTeamBans", "theirTeamBans" })
                    {
                        object ids;
                        if (bans.TryGetValue(side, out ids) && ids is object[]) foreach (object id in (object[])ids) if (id is int) add((int)id, UnavailableChampion.Ban);
                    }
                foreach (int id in allyPicks) add(id, UnavailableChampion.Ally);
                if (root.TryGetValue("theirTeam", out theirO) && theirO is object[])
                    foreach (object o in (object[])theirO)
                    {
                        Dictionary<string, object> m = o as Dictionary<string, object>;
                        if (m != null) add(PositiveInt(m, "championId"), UnavailableChampion.Enemy);
                    }
                return list;
            }
            catch { return null; }
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
                if (s.EnemyChampionIds != null) { s.AssignedRole = LcuParser.ParseAssignedRole(session); s.Pick = LcuParser.ParseLocalPick(session); s.Unavailable = LcuParser.ParseUnavailable(session); }
            }
            catch { }
            return s;
        }

        // Bilan de fin de partie (beta.3) : demandé par le programme seulement, en PreEndOfGame / EndOfGame, pour la partie suivie.
        public async Task<EndOfGameResult> ReadEndOfGameAsync()
        {
            string body;
            try { body = await transport.GetAsync(LcuEndpoints.EndOfGame); }
            catch { return new EndOfGameResult(); }
            return LcuParser.ParseEndOfGame(body);
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

        public async Task<EndOfGameResult> ReadEndOfGameAsync()
        {
            if (reader == null) return new EndOfGameResult();
            Task<EndOfGameResult> read = reader.ReadEndOfGameAsync();
            if (await Task.WhenAny(read, Task.Delay(PollTimeoutMs)) != read) return new EndOfGameResult();
            return await read;
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
        // Bilan de fin de partie (beta.3) : "" = pas de bilan.
        public string EndOfGameJson = "";
        public bool Offline;
        public bool Hang;

        public Task<string> GetAsync(string path)
        {
            Requests.Add(path);
            if (!LcuEndpoints.IsAllowed(path)) throw new InvalidOperationException("Endpoint non autorisé");
            if (Hang) return new TaskCompletionSource<string>().Task;
            if (Offline) return Task.FromResult<string>(null);
            return Task.FromResult(path == LcuEndpoints.Phase ? PhaseJson : path == LcuEndpoints.EndOfGame ? EndOfGameJson : SessionJson);
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

            t(LcuEndpoints.IsAllowed(LcuEndpoints.Phase) && LcuEndpoints.IsAllowed(LcuEndpoints.Session) && LcuEndpoints.IsAllowed("/lol-end-of-game/v1/eog-stats-block"), "LCU : les trois endpoints documentés sont autorisés (beta.3 : bilan de fin de partie)");
            t(!LcuEndpoints.IsAllowed("/lol-summoner/v1/current-summoner") && !LcuEndpoints.IsAllowed("/lol-champ-select/v1/session/actions/1") && !LcuEndpoints.IsAllowed("/lol-end-of-game/v1/eog-stats-block/1")
                && !LcuEndpoints.IsAllowed("/lol-end-of-game/v1/gameclient-eog-stats-block") && !LcuEndpoints.IsAllowed("/lol-match-history/v1/products/lol/current-summoner/matches") && !LcuEndpoints.IsAllowed("/lol-end-of-game/v1/EOG-STATS-BLOCK"), "LCU : tout autre endpoint est refusé");
            // Bilan de fin de partie (beta.3), format réel reconstitué.
            {
                EndOfGameResult win = LcuParser.ParseEndOfGame(LcuSamples.EndOfGameBlock(7012345678, true, false)), loss = LcuParser.ParseEndOfGame(LcuSamples.EndOfGameBlock(7012345679, false, false));
                t(win.State == EndOfGameResult.Read && win.Win && win.GameId == 7012345678 && loss.State == EndOfGameResult.Read && !loss.Win && loss.GameId == 7012345679, "Bilan de fin de partie : victoire et défaite lues, avec le gameId");
                EndOfGameResult fallback = LcuParser.ParseEndOfGame(LcuSamples.EndOfGameBlock(7012345680, true, false, "A", false));
                t(fallback.State == EndOfGameResult.Read && fallback.Win, "Bilan : sans localPlayer.teamId, équipe du joueur par isPlayerTeam");
                t(LcuParser.ParseEndOfGame(LcuSamples.EndOfGameBlock(7012345681, true, true)).State == EndOfGameResult.Remake, "Bilan : remake, aucun résultat");
                t(LcuParser.ParseEndOfGame("").State == EndOfGameResult.Missing && LcuParser.ParseEndOfGame(null).State == EndOfGameResult.Missing, "Bilan absent : aucun résultat");
                string full = LcuSamples.EndOfGameBlock(7012345682, true, false);
                t(LcuParser.ParseEndOfGame(full.Substring(0, full.Length / 2)).State == EndOfGameResult.Unreadable && LcuParser.ParseEndOfGame("{\"gameId\":7012345682}").State == EndOfGameResult.Unreadable
                    && LcuParser.ParseEndOfGame("{\"gameId\":7012345682,\"localPlayer\":{\"teamId\":100},\"teams\":[{\"teamId\":100}]}").State == EndOfGameResult.Unreadable
                    && LcuParser.ParseEndOfGame("{\"gameId\":\"abc\",\"teams\":[]}").State == EndOfGameResult.Unreadable && LcuParser.ParseEndOfGame("[1,2]").State == EndOfGameResult.Unreadable, "Bilan tronqué ou illisible (gameId, équipe ou isWinningTeam absents) : aucun résultat");
                // Test négatif : noms, riotId, puuid, summonerId et statistiques de tous les joueurs changés : même résultat, et rien de tout
                // cela dans ce qui est gardé.
                EndOfGameResult other = LcuParser.ParseEndOfGame(LcuSamples.EndOfGameBlock(7012345678, true, false, "ZZZZZZ"));
                string keptA = new JavaScriptSerializer().Serialize(win), keptZ = new JavaScriptSerializer().Serialize(other);
                t(keptA == keptZ && !Regex.IsMatch(keptA + keptZ, "Joueur|Allie|Adverse|puuid|summoner|TAG|stats|CHAMPIONS|2834567890", RegexOptions.IgnoreCase)
                    && typeof(EndOfGameResult).GetFields().Where(f => !f.IsLiteral).Select(f => f.Name).OrderBy(n => n).SequenceEqual(new[] { "GameId", "State", "Win" }), "Bilan : noms, puuid et statistiques des joueurs changés, même résultat ; rien d'autre n'est gardé (" + keptA + ")");
                t(typeof(EogBlock).GetProperties().Select(f => f.Name).OrderBy(n => n).SequenceEqual(new[] { "gameEndedInEarlySurrender", "gameId", "invalid", "localPlayer", "teams" }) && typeof(EogLocal).GetProperties().Length == 1
                    && typeof(EogTeam).GetProperties().Select(f => f.Name).OrderBy(n => n).SequenceEqual(new[] { "isPlayerTeam", "isWinningTeam", "teamId" }), "Bilan : seuls gameId, équipes (teamId, isWinningTeam, isPlayerTeam), localPlayer.teamId et remake sont désérialisés");
            }
            t(typeof(ILcuTransport).GetMethods().Length == 1 && typeof(ILcuTransport).GetMethods()[0].Name == "GetAsync", "LCU : l'interface ne permet qu'une lecture");

            LockfileInfo li = LockfileInfo.Parse("LeagueClient:1234:50000:motdepasse:https");
            t(li != null && li.Port == 50000 && li.Password == "motdepasse", "LCU : lecture d'un lockfile simulé");
            t(!li.ToString().Contains("motdepasse"), "LCU : le mot de passe n'apparaît jamais dans un journal");
            t(LockfileInfo.Parse("abimé") == null && LockfileInfo.Parse("a:b:port:c:https") == null && LockfileInfo.Parse("a:1:50000:c:http") == null, "LCU : un lockfile invalide est refusé");
            // Dossier du client : requête limitée, sans Process.MainModule.
            Msg err;
            string self = ClientProcess.FolderOf(Process.GetCurrentProcess().Id, out err);
            t(self != null && string.Equals(self, Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), StringComparison.OrdinalIgnoreCase), "LCU : dossier du client lu par requête limitée");
            t(ClientProcess.FolderOf(-1, out err) == null && err.Key == "native.diag.processDenied" && !err.ToString().Contains("\\"), "LCU : processus introuvable, raison donnée sans chemin");
            // Processus de l'utilisateur lancé avec plus de droits (comme un client lancé en administrateur) : MainModule est refusé.
            int elevated = -1;
            foreach (Process p in Process.GetProcesses())
                using (p)
                {
                    if (elevated > 0) continue;
                    try { string m = p.MainModule.FileName; } catch { Msg e2; if (p.SessionId == Process.GetCurrentProcess().SessionId && ClientProcess.FolderOf(p.Id, out e2) != null) elevated = p.Id; }
                }
            t(true, elevated > 0 ? "LCU : processus à droits supérieurs (pid " + elevated + ") : MainModule refusé, dossier lu quand même" : "LCU : aucun processus à droits supérieurs sur ce PC, cas non rejoué");
            string lockDir = Path.Combine(Path.GetTempPath(), "draft-lcu-test-" + Process.GetCurrentProcess().Id);
            Directory.CreateDirectory(lockDir);
            try
            {
                t(LcuFactory.CreateReal(true, lockDir) == null && LcuFactory.LastOpenError == "lockfile absent", "LCU : lockfile absent signalé dans le diagnostic");
                File.WriteAllText(Path.Combine(lockDir, "lockfile"), "abimé");
                t(LcuFactory.CreateReal(true, lockDir) == null && LcuFactory.LastOpenError == "lockfile invalide", "LCU : lockfile invalide signalé dans le diagnostic");
                File.WriteAllText(Path.Combine(lockDir, "lockfile"), "LeagueClient:1234:50000:secretlcu:https");
                t(LcuFactory.CreateReal(true, lockDir) != null && LcuFactory.LastOpenError == "", "LCU : lockfile valide, diagnostic remis à zéro");
                File.WriteAllText(Path.Combine(lockDir, "lockfile"), "abimé:secretlcu");
                LcuFactory.CreateReal(true, lockDir);
                t(!LcuFactory.LastOpenError.Contains("secretlcu") && !LcuFactory.LastOpenError.Contains(lockDir), "LCU : le diagnostic ne contient ni mot de passe ni chemin");
            }
            finally { Directory.Delete(lockDir, true); LcuFactory.LastOpenError = ""; }

            // Diagnostic utilisateur : verrou respecté, aucun secret, réponse lente repérée.
            string off = LcuDiagnostic.RunAsync(false, 1234, "test").GetAwaiter().GetResult();
            // QA 3.0 : textes attendus pris dans le catalogue de la langue du programme (le test passe en français et en anglais).
            t(off.Contains(Strings.T("native.diag.off")) && off.Contains(Strings.T("native.diag.offConclusion")) && !off.Contains("Lockfile"), "LCU : diagnostic, option décochée : le lockfile n'est pas lu");
            string noClient = LcuDiagnostic.RunAsync(true, 0, "test").GetAwaiter().GetResult();
            t(noClient.Contains(Strings.T("native.diag.noWindow")) && noClient.Contains(Strings.T("native.diag.noWindowConclusion")), "LCU : diagnostic, client fermé : consigne simple");
            TcpListener mute = new TcpListener(IPAddress.Loopback, 0);
            mute.Start();
            string diagDir = Path.Combine(Path.GetTempPath(), "draft-lcu-diag-" + Process.GetCurrentProcess().Id);
            Directory.CreateDirectory(diagDir);
            try
            {
                int port = ((IPEndPoint)mute.LocalEndpoint).Port;
                File.WriteAllText(Path.Combine(diagDir, "lockfile"), "LeagueClient:1:" + port + ":secretlcu:https");
                string text = Task.Run(() => LcuDiagnostic.RunAsync(true, 0, "test", diagDir)).GetAwaiter().GetResult();
                string tcpOpen = Strings.T("native.diag.tcp", "result", "") + Strings.T("native.diag.tcpOpen", "ms", 0).Split('(')[0];
                t(text.Contains(Strings.T("native.diag.lockfileOk")) && text.Contains(tcpOpen) && (text.Contains(Strings.T("native.diag.refusedConclusion")) || text.Contains(Strings.T("native.diag.proxyConclusion"))),"LCU : diagnostic, client qui ne répond pas : étapes et conclusion");
                t(!text.Contains("secretlcu") && !text.Contains(port.ToString()) && !text.Contains(diagDir), "LCU : diagnostic sans mot de passe, port ni chemin");
            }
            finally { mute.Stop(); Directory.Delete(diagDir, true); }

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
            // Format réel du client : case du joueur 7 (côté rouge), summonerId long, puuid, actions imbriquées, minuteur.
            Func<string, string> real = pos => LcuSamples.RealSession(pos, 7, 7, 875, 254);
            t(LcuParser.ParseAssignedRole(real("jungle")) == "jungle" && LcuParser.ParseAssignedRole(real("JUNGLE")) == "jungle" && LcuParser.ParseAssignedRole(real("Middle")) == "mid", "LCU : format réel, poste en minuscules ou majuscules (jungle, JUNGLE, Middle)");
            t(LcuParser.ParseAssignedRole(real("utility")) == "support" && LcuParser.ParseAssignedRole(LcuSamples.RealSession("bottom", 0, 0)) == "adc", "LCU : format réel, autofill : le poste attribué est lu tel quel (utility, bottom), côté bleu ou rouge");
            t(LcuParser.ParseAssignedRole(real("")) == null, "LCU : format réel, mode sans poste : aucun rôle");
            t(LcuParser.ParseAssignedRole(LcuSamples.RealSession("jungle", 7, 8)) == null, "LCU : format réel, joueur absent de myTeam : aucun rôle");
            t(LcuParser.ParseAssignedRole(real("jungle").Substring(0, 700)) == null && LcuParser.ParseEnemies(real("jungle").Substring(0, 700)) == null, "LCU : format réel tronqué : réponse abîmée, rien n'est lu");
            t(string.Join(",", LcuParser.ParseEnemies(real("jungle"))) == "875,254", "LCU : format réel, adversaires lus à côté du poste");
            // Beta.2 : TON pick seulement (journal automatique). Format réel, valeurs fictives : Ahri 103 et Syndra 134 pour toi,
            // Zed 238 verrouillé et Lux 99 en intention pour un coéquipier.
            JavaScriptSerializer ser = new JavaScriptSerializer();
            Func<int, bool, bool, int, int, string, string> pick = (action, busy, done, intent, mine, timer) => LcuSamples.PickSession("middle", 7, action, busy, done, intent, mine, timer, 7012345678, 238, 99, true, 875);
            LocalPick hover = LcuParser.ParseLocalPick(pick(103, true, false, 0, 0, "BAN_PICK"));
            t(hover.Readable && hover.Supported && hover.State == LocalPick.Hover && hover.Champion == 103, "LCU : ton tour, Ahri survolée (action en cours, pas terminée)");
            LocalPick intentOnly = LcuParser.ParseLocalPick(pick(0, false, false, 103, 0, "BAN_PICK"));
            t(intentOnly.State == LocalPick.Hover && intentOnly.Champion == 103 && intentOnly.Intent == 103, "LCU : avant ton tour, présélection d'Ahri (championPickIntent)");
            LocalPick locked = LcuParser.ParseLocalPick(pick(103, false, true, 0, 103, "BAN_PICK"));
            t(locked.State == LocalPick.Locked && locked.Champion == 103 && locked.LockedId == 103 && locked.GameId == 7012345678 && locked.TimerPhase == "BAN_PICK", "LCU : Ahri verrouillée (action terminée), gameId et timer.phase lus");
            LocalPick swapped = LcuParser.ParseLocalPick(pick(103, false, true, 0, 134, "FINALIZATION"));
            t(swapped.State == LocalPick.Final && swapped.Champion == 134 && swapped.LockedId == 103 && swapped.FinalId == 134, "LCU : échange accepté en fin de sélection : champion final Syndra (myTeam[toi].championId), verrouillage Ahri gardé");
            LocalPick nothing = LcuParser.ParseLocalPick(pick(0, false, false, 0, 0, "PLANNING"));
            t(nothing.Readable && nothing.Supported && nothing.State == LocalPick.None && nothing.Champion == 0, "LCU : rien survolé, coéquipier verrouillé : aucun pick pour toi");
            // Test négatif : tout ce que fait le coéquipier (survol, verrouillage, intention) change, ce qui est lu ne change pas.
            bool sameAllyChange = true;
            foreach (int[] other in new[] { new[] { 0, 0 }, new[] { 64, 157 }, new[] { 103, 103 } })
                foreach (string[] c in new[] { new[] { "103", "1", "0", "0", "0", "BAN_PICK" }, new[] { "103", "0", "1", "0", "103", "BAN_PICK" }, new[] { "0", "0", "0", "103", "0", "PLANNING" }, new[] { "103", "0", "1", "0", "134", "FINALIZATION" } })
                {
                    Func<int, int, string> s2 = (ac, ai) => LcuSamples.PickSession("middle", 7, int.Parse(c[0]), c[1] == "1", c[2] == "1", int.Parse(c[3]), int.Parse(c[4]), c[5], 7012345678, ac, ai, true, 875);
                    if (ser.Serialize(LcuParser.ParseLocalPick(s2(238, 99))) != ser.Serialize(LcuParser.ParseLocalPick(s2(other[0], other[1])))) sameAllyChange = false;
                }
            string allRead = ser.Serialize(new[] { hover, intentOnly, locked, swapped, nothing });
            t(sameAllyChange && !allRead.Contains("238") && !allRead.Contains(":99,") && !allRead.Contains("Puuid") && !allRead.Contains("2834567890123"), "LCU : aucune donnée de coéquipier lue (test négatif : son survol, son verrouillage et son intention changent, la lecture non) ; aucun identifiant de joueur");
            // Modes : seule la draft classique (action de pick ET poste attribué) est prise en charge.
            LocalPick bench = LcuParser.ParseLocalPick(LcuSamples.PickSession("", 7, 0, false, false, 0, 103, "BAN_PICK", 0, 64, 0, false));
            t(bench.Readable && !bench.Supported && bench.GameId == 0, "LCU : mode à banc (ARAM), sans action de pick ni poste : non pris en charge, gameId absent");
            LocalPick blind = LcuParser.ParseLocalPick(LcuSamples.PickSession("", 7, 103, true, false, 0, 0, "BAN_PICK", 0, 0, 0, true));
            t(blind.Readable && !blind.Supported && blind.State == LocalPick.Hover, "LCU : sélection sans poste (aveugle) : survol lu, mode non pris en charge");
            t(!LcuParser.ParseLocalPick(LcuSamples.RealSession("middle", 7, 7, 875)).Supported, "LCU : session sans action de pick pour toi : non prise en charge");
            t(!LcuParser.ParseLocalPick(LcuSamples.RealSession("jungle", 7, 8)).Readable, "LCU : joueur absent de son équipe : rien n'est lu");
            LocalPick cut = LcuParser.ParseLocalPick(pick(103, false, true, 0, 103, "BAN_PICK").Substring(0, 700));
            t(!cut.Readable && cut.State == LocalPick.None && !LcuParser.ParseLocalPick("").Readable && !LcuParser.ParseLocalPick("{abîmé").Readable, "LCU : réponse tronquée ou abîmée : rien n'est lu");
            // « Dans League » : versionné, premier verrouillage, échange, perte et retour, DRAFT lancé après le verrouillage.
            LeaguePickTracker lt = new LeaguePickTracker();
            lt.Begin("k1"); int v0 = lt.V;
            bool c1 = lt.Read(hover), c2 = lt.Read(hover);
            t(c1 && !c2 && lt.V == v0 + 1 && lt.State == LocalPick.Hover && lt.Champion == 103 && lt.LockedFirst == 0, "Dans League : survol, version augmentée une fois ; même lecture : rien ne change");
            lt.Read(locked); lt.Read(swapped);
            t(lt.State == LocalPick.Final && lt.Champion == 134 && lt.LockedFirst == 103 && lt.Traded && lt.GameId == 7012345678, "Dans League : verrouillage puis échange : final Syndra, premier verrouillage Ahri, échange noté");
            int vLost = lt.V;
            t(lt.Lose() && lt.Lost && lt.V == vLost + 1 && lt.Champion == 134, "Dans League : lecture perdue : signalée, dernier pick gardé");
            lt.Read(LcuParser.ParseLocalPick(LcuSamples.RealSession("jungle", 7, 8)));
            t(!lt.Lost && lt.Champion == 134, "Dans League : retour de la lecture, joueur introuvable : le dernier pick reste");
            t(lt.Begin("k2") && lt.State == LocalPick.None && lt.LockedFirst == 0 && !lt.Traded && lt.GameId == 0 && !lt.Begin("k2"), "Dans League : nouvelle sélection, tout repart de zéro");
            LeaguePickTracker late = new LeaguePickTracker();
            late.Begin("k3"); late.Read(locked);
            t(late.State == LocalPick.Locked && late.LockedFirst == 103 && !late.Traded, "Dans League : DRAFT lancé après le verrouillage : verrouillage lu tel quel");
            t(lt.DiagWord == "aucun" && late.DiagWord == "verrouillé", "Dans League : diagnostic par état seulement (aucun, survolé, verrouillé, final), sans nom");

            // Beta.2 : disponibilité. Format réel, valeurs fictives. Toi : case 2 (mid, côté bleu) ; alliés 0, 1, 3, 4 ; adversaires 5 à 9.
            // Zed 238, Yasuo 157, Akali 84, Fizz 105, Talon 91, Katarina 55, Sylas 517 (bans) ; Aatrox 266 (allié verrouillé) ;
            // Lee Sin 64 (allié survolé) ; Lux 99 et Thresh 412 (intentions d'alliés) ; Ahri 103 (ta présélection) ; TF 4 (ton pick) ; Galio 3 (en face).
            Func<List<UnavailableChampion>, string> un = l => l == null ? "null" : string.Join(",", l.OrderBy(u => u.Id).Select(u => u.Reason + ":" + u.Id));
            int[] noBans = new int[0];
            string[] banning = { "0:ban:238:done", "1:ban:157:busy", "2:ban:0:busy", "3:ban:0:busy", "4:ban:84:done", "5:ban:0:busy", "6:ban:0:busy", "7:ban:0:busy", "8:ban:0:busy", "9:ban:0:busy" };
            string[] banned = { "0:ban:238:done", "1:ban:157:done", "2:ban:0:done", "3:ban:0:done", "4:ban:84:done", "5:ban:105:done", "6:ban:91:done", "7:ban:0:done", "8:ban:55:done", "9:ban:517:done" };
            int[] myBans = { 238, 157, 84 }, theirBans = { 105, 91, 55, 517 };
            const string AllBans = "ban:55,ban:84,ban:91,ban:105,ban:157,ban:238,ban:517";
            Func<int[], int[], string[][], int[], int[], string, string> draft = (team, intents, acts, mb, tb, timer) => LcuSamples.DraftSession(2, timer, acts, mb, tb, team, intents, 3, 0, 0, 0, 0);
            string s1 = LcuSamples.DraftSession(2, "BAN_PICK", new[] { banning }, noBans, noBans, new[] { 0, 0, 0, 0, 0 }, new[] { 0, 0, 103, 99, 0 }, 0, 0, 0, 0, 0);
            t(un(LcuParser.ParseUnavailable(s1)) == "ban:84,ban:238", "Disponibilité : bans en cours (simultanés) : seuls les bans terminés comptent (Zed, Akali) ; le ban survolé d'un allié (Yasuo), les bans adverses cachés, ta présélection et l'intention d'un allié, non (lu " + un(LcuParser.ParseUnavailable(s1)) + ")");
            string sBans = LcuSamples.DraftSession(2, "BAN_PICK", new[] { banned }, myBans, theirBans, new[] { 0, 0, 0, 0, 0 }, new[] { 0, 0, 103, 99, 0 }, 0, 0, 0, 0, 0);
            string sBansActions = LcuSamples.DraftSession(2, "BAN_PICK", new[] { banned }, noBans, noBans, new[] { 0, 0, 0, 0, 0 }, new[] { 0, 0, 0, 0, 0 }, 0, 0, 0, 0, 0);
            string sBansObject = LcuSamples.DraftSession(2, "BAN_PICK", new[] { new[] { "2:ban:0:done" } }, myBans, theirBans, new[] { 0, 0, 0, 0, 0 }, new[] { 0, 0, 0, 0, 0 }, 0, 0, 0, 0, 0);
            t(un(LcuParser.ParseUnavailable(sBans)) == AllBans && un(LcuParser.ParseUnavailable(sBansActions)) == AllBans && un(LcuParser.ParseUnavailable(sBansObject)) == AllBans,
                "Disponibilité : bans révélés des deux équipes, lus dans les actions terminées OU dans bans.myTeamBans / theirTeamBans ; bans passés (champion 0) ignorés (lu " + un(LcuParser.ParseUnavailable(sBans)) + " / " + un(LcuParser.ParseUnavailable(sBansActions)) + " / " + un(LcuParser.ParseUnavailable(sBansObject)) + ")");
            string[][] picking = { banned, new[] { "0:pick:266:done", "5:pick:3:done", "6:pick:0:wait" }, new[] { "1:pick:64:busy", "2:pick:0:wait" }, new[] { "3:pick:0:wait", "4:pick:0:wait" } };
            string s3 = draft(new[] { 266, 64, 0, 0, 0 }, new[] { 0, 0, 103, 99, 412 }, picking, myBans, theirBans, "BAN_PICK");
            t(un(LcuParser.ParseUnavailable(s3)) == "enemy:3,ban:55,ban:84,ban:91,ban:105,ban:157,ban:238,ally:266,ban:517",
                "Disponibilité : pick allié verrouillé (Aatrox) et pick adverse (Galio) ; le survol d'un allié (Lee Sin, action en cours et case affichée) et les intentions (Lux, Thresh, Ahri) ne rendent rien indisponible (lu " + un(LcuParser.ParseUnavailable(s3)) + ")");
            string[][] locks = { banned, new[] { "0:pick:266:done", "5:pick:3:done" }, new[] { "1:pick:64:done", "2:pick:4:done" } };
            string s4 = draft(new[] { 266, 64, 4, 0, 0 }, new[] { 0, 0, 0, 0, 0 }, locks, myBans, theirBans, "BAN_PICK");
            List<UnavailableChampion> l4 = LcuParser.ParseUnavailable(s4);
            t(l4.Count(u => u.Reason == UnavailableChampion.Ally) == 2 && l4.Any(u => u.Id == 64 && u.Reason == UnavailableChampion.Ally) && !l4.Any(u => u.Id == 4), "Disponibilité : un allié verrouille Lee Sin, tu verrouilles TF : Lee Sin pris par un allié, ton propre pick n'est jamais indisponible pour toi");
            // Échange en fin de sélection : Aatrox (allié, case 0) et TF (toi) échangés. Les cases suivent l'échange, les actions non.
            List<UnavailableChampion> l5 = LcuParser.ParseUnavailable(draft(new[] { 4, 64, 266, 0, 0 }, new[] { 0, 0, 0, 0, 0 }, locks, myBans, theirBans, "FINALIZATION"));
            t(l5.Any(u => u.Id == 4 && u.Reason == UnavailableChampion.Ally) && !l5.Any(u => u.Id == 266), "Disponibilité : échange accepté avec un allié : son champion final (myTeam[sa case].championId) est pris, celui que tu joues désormais ne l'est pas");
            List<UnavailableChampion> l6 = LcuParser.ParseUnavailable(draft(new[] { 0, 0, 0, 0, 0 }, new[] { 0, 0, 0, 0, 0 }, locks, myBans, theirBans, "BAN_PICK"));
            t(l6.Any(u => u.Id == 266 && u.Reason == UnavailableChampion.Ally) && l6.Any(u => u.Id == 64 && u.Reason == UnavailableChampion.Ally), "Disponibilité : action d'un allié terminée, case pas encore à jour : le champion de l'action est pris");
            // Test négatif : survol, intentions, noms, tags, puuid et identifiants des coéquipiers changent, ce qui est lu ne change pas.
            bool sameAlly = true;
            foreach (int[] h in new[] { new[] { 0, 0 }, new[] { 64, 64 }, new[] { 134, 0 }, new[] { 0, 7 } })
            {
                string[][] acts = { banned, new[] { "0:pick:266:done", "5:pick:3:done" }, new[] { "1:pick:" + h[0] + ":busy" } };
                string a1 = un(LcuParser.ParseUnavailable(draft(new[] { 266, h[0], 0, 0, 0 }, new[] { 0, h[1], 103, 99, 412 }, acts, myBans, theirBans, "BAN_PICK")));
                string a2 = un(LcuParser.ParseUnavailable(draft(new[] { 266, 0, 0, 0, 0 }, new[] { 0, 0, 0, 0, 0 }, new[] { banned, new[] { "0:pick:266:done", "5:pick:3:done" }, new[] { "1:pick:0:busy" } }, myBans, theirBans, "BAN_PICK")));
                if (a1 != a2) sameAlly = false;
            }
            string renamed = s3.Replace("Coequipier", "Autre").Replace("\"SEC", "\"XYZ").Replace("2222-4333", "9999-4333").Replace("283456789010", "183456789010");
            if (renamed == s3 || un(LcuParser.ParseUnavailable(renamed)) != un(LcuParser.ParseUnavailable(s3))) sameAlly = false;
            string read = ser.Serialize(new[] { LcuParser.ParseUnavailable(s1), LcuParser.ParseUnavailable(s3), l4, l5 });
            System.Reflection.FieldInfo[] fields = typeof(UnavailableChampion).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            t(sameAlly && !read.Contains("Coequipier") && !read.Contains("Joueur") && !read.Contains("SEC") && !read.Contains("2222-4333") && !read.Contains("283456789010") && !read.Contains("412") && !read.Contains(":99,") && !read.Contains("103")
                && fields.Length == 2 && fields.All(f => f.Name == "Id" || f.Name == "Reason") && typeof(UnavailableChampion).GetProperties().Length == 0,
                "Disponibilité : test négatif au format réel : le survol, l'intention, le nom, le tag, le puuid et le summonerId d'un allié changent, la lecture non ; seuls un identifiant de champion et une raison sont gardés");
            string noLocal = s3.Replace("\"localPlayerCellId\":2,", "");
            t(un(LcuParser.ParseUnavailable(noLocal)) == "enemy:3," + AllBans, "Disponibilité : sans ta case (localPlayerCellId absent), aucun pick allié n'est déduit ; bans et adversaires restent lus");
            t(un(LcuParser.ParseUnavailable(LcuSamples.PickSession("middle", 7, 103, true, false, 0, 0, "BAN_PICK", 7012345678, 238, 99, true, 875))) == "ally:238,enemy:875"
                && un(LcuParser.ParseUnavailable(LcuSamples.PickSession("middle", 7, 103, true, false, 0, 0, "BAN_PICK", 7012345678, 0, 99, true, 875))) == "enemy:875",
                "Disponibilité : format réel de la beta.2 (côté rouge) : allié verrouillé (Zed) pris, allié qui n'a pas verrouillé (intention Lux) rien");
            t(LcuParser.ParseUnavailable(s3.Substring(0, 900)) == null && LcuParser.ParseUnavailable("") == null && LcuParser.ParseUnavailable("{abîmé") == null && LcuParser.ParseUnavailable("[1]") == null,
                "Disponibilité : réponse tronquée, vide ou abîmée : illisible (null), la dernière liste connue reste");
            // Suivi versionné : une nouvelle sélection vide la liste, une même liste (autre ordre) ne change pas la version.
            AvailabilityTracker at = new AvailabilityTracker();
            int av0 = at.V;
            bool b1 = at.Begin("k1"), b2 = at.Begin("k1");
            bool r1 = at.Read(LcuParser.ParseUnavailable(s1)), r2 = at.Read(LcuParser.ParseUnavailable(s1)), r3 = at.Read(null);
            t(b1 && !b2 && r1 && !r2 && !r3 && at.V == av0 + 2 && at.Items.Count == 2, "Disponibilité : sélection suivie, liste lue (version +1) ; même lecture ou session illisible : rien ne change");
            List<UnavailableChampion> reversed = LcuParser.ParseUnavailable(s3); reversed.Reverse();
            bool r4 = at.Read(LcuParser.ParseUnavailable(s3)), r5 = at.Read(reversed);
            Dictionary<string, object> st = at.ToState();
            string stJson = ser.Serialize(st);
            t(r4 && !r5 && at.Count(UnavailableChampion.Ban) == 7 && at.Count(UnavailableChampion.Ally) == 1 && at.Count(UnavailableChampion.Enemy) == 1 && st.Count == 3 && (string)st["key"] == "k1" && (int)st["v"] == at.V
                && stJson.Contains("{\"id\":266,\"reason\":\"ally\"}") && stJson.Contains("{\"id\":3,\"reason\":\"enemy\"}") && !stJson.Contains("Coequipier"),
                "Disponibilité : état transmis aux pages : version, clé, et pour chaque champion son identifiant et sa raison (" + stJson.Length + " caractères)");
            t(at.Begin("k2") && at.Items.Count == 0 && at.Key == "k2", "Disponibilité : nouvelle sélection, la liste repart vide");

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
            t(a.Unavailable == null && b.Unavailable != null && b.Unavailable.Count == 4 && b.Unavailable.All(u => u.Reason == UnavailableChampion.Enemy) && sim.Requests.Count(r => r == LcuEndpoints.Session) == 1,
                "Disponibilité : lue dans la même session (une seule requête), seulement pendant une draft");
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
