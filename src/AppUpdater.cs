// Mise à jour à la demande, depuis les releases GitHub du projet. Rien ne part sans un clic de l'utilisateur.
// Le nouvel exécutable est téléchargé à côté de l'actuel, vérifié, testé dans un dossier isolé, puis échangé.
// Tant que l'échange n'a pas eu lieu, la version installée n'est pas touchée.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DraftMid
{
    public class UpdateInfo
    {
        public Version Version;
        public string Tag, AssetUrl, Sha256, SignatureUrl;
        public long Size;
    }

    // Message destiné à l'utilisateur.
    public class UpdateError : Exception { public UpdateError(string text) : base(text) { } }

    public interface IUpdateSource
    {
        string PublicKey { get; }
        Task<string> SignatureAsync(UpdateInfo info, CancellationToken ct);
        Task<UpdateInfo> LatestAsync(CancellationToken ct);
        Task DownloadAsync(UpdateInfo info, string path, IProgress<long> progress, CancellationToken ct);
    }

    public static class AppUpdater
    {
        public const string LatestUrl = "https://api.github.com/repos/lucasmettetal/Draft/releases/latest";
        public const string AssetPrefix = "https://github.com/lucasmettetal/Draft/releases/download/";
        // Partie publique de la clé de signature. La clé privée reste sur le PC du mainteneur : voir tools/sign-release.ps1.
        public const string ReleaseKey = "<RSAKeyValue><Modulus>rrTfauqIObcEqi5BOf2jkB3TW50jqIrDNoTVTKbg9OeSSgBu8n2b6ihOzwC02/nCGIIKYymCJNmBQ7RLAMmOTD5eGPe+pGoShXIzDtCLXTNCuU+Vq1MYQEKAuZ6FTjavNcnCg8czmFci//MvUjjtOMtvxcmW2N4s/+2L1wQzwglWOsX3yIbvgSK35y3iDlP4rd+DDcNXD2IrbD2KrJk4C9SiUmM1AX6MgyEHUAZY9Ubcm+HFhTqZG3RXcGYah5zYeIdcyXA1KKqjIE3zBRcFiANJh/B2xGUxD1dhurZQ4tIrE4PLM8an9wdIoCcQV2XUMbmSS3aiu5x54HEdejSc7wLX8KUv039Y/++xcYOacL85RkhGEbN60e6vuLwwEyvokbV4e2jXdzM/x4LYRqXhh8nRFiNs4HqHcjlS+wW3krMt+mmPYTEciryioBzLuy7NtBBW95sLwTMwhEh3m9EYzeJw8BOpy3aSmi5Cza0kfkRB8g778/ucwjav7KXTl/lh</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        const long MinSize = 300 * 1024, MaxSize = 60 * 1024 * 1024;
        const int SelfCheckMs = 180000;

        public static Version Current { get { return Short(typeof(AppUpdater).Assembly.GetName().Version); } }
        static Version Short(Version v) { return new Version(v.Major, v.Minor, Math.Max(v.Build, 0)); }

        // Seules les versions finales « v1.2.3 » sont acceptées.
        public static Version ParseTag(string tag)
        {
            Match m = Regex.Match(tag ?? "", @"^v?(\d{1,4})\.(\d{1,4})\.(\d{1,4})$");
            return m.Success ? new Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value)) : null;
        }

        public static bool IsNewer(Version current, Version remote) { return remote != null && Short(remote) > Short(current); }

        // Null si la release ne convient pas : brouillon, préversion, adresse étrangère, empreinte absente.
        public static UpdateInfo ParseRelease(string json)
        {
            Dictionary<string, object> root;
            try { root = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.DeserializeObject(json) as Dictionary<string, object>; }
            catch (Exception e) { if (e is OutOfMemoryException) throw; return null; }
            object o;
            if (root == null) return null;
            if ((root.TryGetValue("draft", out o) && o is bool && (bool)o) || (root.TryGetValue("prerelease", out o) && o is bool && (bool)o)) return null;
            if (!root.TryGetValue("tag_name", out o)) return null;
            string tag = Convert.ToString(o);
            Version version = ParseTag(tag);
            if (version == null || !root.TryGetValue("assets", out o) || !(o is object[])) return null;
            List<Dictionary<string, object>> assets = ((object[])o).OfType<Dictionary<string, object>>().ToList();
            foreach (Dictionary<string, object> asset in assets)
            {
                object name, url, size, digest;
                if (!asset.TryGetValue("name", out name) || !Convert.ToString(name).EndsWith("-Portable.exe", StringComparison.OrdinalIgnoreCase)) continue;
                if (!asset.TryGetValue("browser_download_url", out url) || !asset.TryGetValue("size", out size) || !asset.TryGetValue("digest", out digest)) return null;
                string link = Convert.ToString(url), hash = Convert.ToString(digest);
                long bytes = Convert.ToInt64(size);
                if (!link.StartsWith(AssetPrefix + tag + "/", StringComparison.Ordinal) || bytes < MinSize || bytes > MaxSize) return null;
                Match sha = Regex.Match(hash, "^sha256:([0-9a-fA-F]{64})$");
                if (!sha.Success) return null;
                string signature = null;
                foreach (Dictionary<string, object> other in assets)
                {
                    object otherName, otherUrl;
                    if (other.TryGetValue("name", out otherName) && Convert.ToString(otherName) == Convert.ToString(name) + ".sig" && other.TryGetValue("browser_download_url", out otherUrl) && Convert.ToString(otherUrl).StartsWith(AssetPrefix + tag + "/", StringComparison.Ordinal)) signature = Convert.ToString(otherUrl);
                }
                return new UpdateInfo { Version = version, Tag = tag, AssetUrl = link, SignatureUrl = signature, Size = bytes, Sha256 = sha.Groups[1].Value.ToLowerInvariant() };
            }
            return null;
        }

        public static string CandidatePath(string exe) { return Path.Combine(Path.GetDirectoryName(exe), Path.GetFileNameWithoutExtension(exe) + ".maj.exe"); }
        public static string OldPath(string exe) { return exe + ".ancien"; }

        public static string Sha256Of(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream f = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(f).Select(b => b.ToString("x2")));
        }

        public static void CheckFile(string path, UpdateInfo info)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists || file.Length != info.Size) throw new UpdateError("Le téléchargement est incomplet. Rien n'a été changé.");
            using (FileStream f = File.OpenRead(path))
                if (f.ReadByte() != 'M' || f.ReadByte() != 'Z') throw new UpdateError("Le fichier reçu n'est pas un exécutable. Rien n'a été changé.");
            if (!string.Equals(Sha256Of(path), info.Sha256, StringComparison.OrdinalIgnoreCase)) throw new UpdateError("L'empreinte du fichier reçu ne correspond pas à celle de GitHub. Rien n'a été changé.");
            Version inside;
            try { inside = Short(new Version(FileVersionInfo.GetVersionInfo(path).FileVersion)); }
            catch (Exception e) { if (e is OutOfMemoryException) throw; inside = null; }
            if (inside == null || inside != Short(info.Version)) throw new UpdateError("La version du fichier reçu ne correspond pas à celle annoncée. Rien n'a été changé.");
        }

        // La signature couvre le tag et l'empreinte : un ancien fichier signé ne peut pas passer pour une version plus récente.
        public static string SignedText(string tag, string sha256) { return "DRAFT-update\n" + tag + "\n" + sha256.ToLowerInvariant(); }

        public static bool ValidSignature(string tag, string sha256, string signature, string publicKey)
        {
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(publicKey);
                    return rsa.VerifyData(Encoding.UTF8.GetBytes(SignedText(tag, sha256)), "SHA256", Convert.FromBase64String((signature ?? "").Trim()));
                }
            }
            catch (FormatException) { return false; }
            catch (CryptographicException) { return false; }
        }

        // Le candidat passe ses propres tests, sur un dossier de données à part.
        public static void SelfCheck(string candidate, string tempDir, CancellationToken ct)
        {
            string result = Path.Combine(tempDir, "resultat.txt");
            ProcessStartInfo start = new ProcessStartInfo(candidate, "--selftest --data \"" + Path.Combine(tempDir, "donnees") + "\" --out \"" + result + "\"") { UseShellExecute = false, CreateNoWindow = true };
            using (Process p = Process.Start(start))
            {
                Stopwatch clock = Stopwatch.StartNew();
                while (!p.WaitForExit(250))
                {
                    if (ct.IsCancellationRequested || clock.ElapsedMilliseconds > SelfCheckMs)
                    {
                        try { p.Kill(); } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
                        ct.ThrowIfCancellationRequested();
                        throw new UpdateError("La nouvelle version n'a pas fini ses tests à temps. Rien n'a été changé.");
                    }
                }
                if (p.ExitCode != 0) throw new UpdateError("La nouvelle version a échoué à ses tests sur ce PC. Rien n'a été changé.");
            }
        }

        // Télécharge, vérifie et teste. Renvoie le chemin du candidat prêt à être échangé.
        public static async Task<string> Prepare(IUpdateSource source, UpdateInfo info, string exe, string tempRoot, IProgress<long> progress, Action<string, string, CancellationToken> selfCheck, CancellationToken ct)
        {
            string candidate = CandidatePath(exe), temp = Path.Combine(tempRoot, "maj-test");
            bool ready = false;
            try
            {
                DeleteQuietly(candidate);
                await source.DownloadAsync(info, candidate, progress, ct).ConfigureAwait(false);
                CheckFile(candidate, info);
                string signature = await source.SignatureAsync(info, ct).ConfigureAwait(false);
                if (!ValidSignature(info.Tag, Sha256Of(candidate), signature, source.PublicKey)) throw new UpdateError("La signature de cette version n'est pas valide. Rien n'a été changé.");
                ct.ThrowIfCancellationRequested();
                DeleteDirectoryQuietly(temp);
                Directory.CreateDirectory(temp);
                await Task.Run(() => selfCheck(candidate, temp, ct)).ConfigureAwait(false);
                ready = true;
                return candidate;
            }
            finally
            {
                DeleteDirectoryQuietly(temp);
                if (!ready) DeleteQuietly(candidate);
            }
        }

        // Windows permet de renommer un exécutable en cours d'exécution, pas de l'écraser.
        public static void Swap(string exe, string candidate)
        {
            string old = OldPath(exe);
            DeleteQuietly(old);
            File.Move(exe, old);
            try { File.Move(candidate, exe); }
            catch (Exception) { File.Move(old, exe); throw; }
        }

        public static void Rollback(string exe)
        {
            string old = OldPath(exe);
            if (!File.Exists(old)) return;
            // Le fichier qui vient de s'arrêter peut rester verrouillé un court instant.
            for (int i = 0; ; i++)
            {
                try
                {
                    if (File.Exists(exe)) File.Delete(exe);
                    File.Move(old, exe);
                    return;
                }
                catch (Exception e)
                {
                    if (!(e is IOException || e is UnauthorizedAccessException) || i >= 20) throw;
                    Thread.Sleep(300);
                }
            }
        }

        public static string ReadyName(int pid) { return "DRAFT-maj-" + pid; }

        public static void SignalReady(int pid)
        {
            EventWaitHandle ready;
            if (EventWaitHandle.TryOpenExisting(ReadyName(pid), out ready)) using (ready) ready.Set();
        }

        // Lancé par l'ancienne version, une fois sa place libérée. Faux si la nouvelle n'a pas tenu : l'ancienne est alors remise en place et relancée.
        public static bool LaunchAndWatch(string exe, string arguments, string rollbackArguments, int timeoutMs)
        {
            using (EventWaitHandle ready = new EventWaitHandle(false, EventResetMode.ManualReset, ReadyName(Process.GetCurrentProcess().Id)))
            {
                try
                {
                    using (Process p = Process.Start(new ProcessStartInfo(exe, arguments) { UseShellExecute = false }))
                    {
                        Stopwatch clock = Stopwatch.StartNew();
                        while (clock.ElapsedMilliseconds < timeoutMs)
                        {
                            if (ready.WaitOne(200)) return true;
                            if (p.HasExited)
                            {
                                if (ready.WaitOne(0)) return true;
                                break;
                            }
                        }
                        if (!p.HasExited) { p.Kill(); p.WaitForExit(5000); }
                    }
                }
                catch (System.ComponentModel.Win32Exception) { }
                catch (InvalidOperationException) { }
            }
            Rollback(exe);
            Process.Start(new ProcessStartInfo(exe, rollbackArguments) { UseShellExecute = false }).Dispose();
            return false;
        }

        // Restes d'une mise à jour terminée ou interrompue.
        public static void CleanUp(string exe)
        {
            DeleteQuietly(CandidatePath(exe));
            string old = OldPath(exe);
            for (int i = 0; i < 10 && File.Exists(old); i++)
            {
                DeleteQuietly(old);
                if (File.Exists(old)) Thread.Sleep(300);
            }
        }

        public static void WaitForExit(int pid)
        {
            try { using (Process p = Process.GetProcessById(pid)) p.WaitForExit(20000); }
            catch (ArgumentException) { } catch (InvalidOperationException) { }
        }

        public static string FriendlyError(Exception e)
        {
            if (e is UpdateError) return e.Message;
            if (e is UnauthorizedAccessException) return "DRAFT ne peut pas écrire dans son dossier : télécharge la mise à jour depuis GitHub.";
            if (e is IOException) return "Un fichier de DRAFT est bloqué. Réessaie dans un instant.";
            if (e is WebException) return "GitHub ne répond pas. Vérifie ta connexion, puis réessaie.";
            return "La mise à jour a échoué. Rien n'a été changé.";
        }

        static void DeleteQuietly(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        static void DeleteDirectoryQuietly(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    public class GitHubUpdateSource : IUpdateSource
    {
        const int TimeoutMs = 20000;
        public GitHubUpdateSource() { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
        public string PublicKey { get { return AppUpdater.ReleaseKey; } }

        // Une réponse plus grosse qu'attendu est refusée avant d'être lue en entier.
        public static async Task<string> ReadLimited(Stream input, int max, CancellationToken ct)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                byte[] chunk = new byte[16 * 1024];
                int n;
                while ((n = await input.ReadAsync(chunk, 0, chunk.Length, ct)) > 0)
                {
                    if (buffer.Length + n > max) throw new UpdateError("La réponse de GitHub est anormalement grosse. Rien n'a été changé.");
                    buffer.Write(chunk, 0, n);
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        public async Task<string> SignatureAsync(UpdateInfo info, CancellationToken ct)
        {
            if (info.SignatureUrl == null) throw new UpdateError("Cette version n'est pas encore signée. Réessaie dans un moment.");
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(info.SignatureUrl);
            r.UserAgent = "DRAFT";
            r.ReadWriteTimeout = TimeoutMs;
            using (ct.Register(r.Abort))
            using (new Timer(_ => r.Abort(), null, TimeoutMs, Timeout.Infinite))
            using (HttpWebResponse w = (HttpWebResponse)await r.GetResponseAsync())
                return await ReadLimited(w.GetResponseStream(), 16 * 1024, ct);
        }

        public async Task<UpdateInfo> LatestAsync(CancellationToken ct)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(AppUpdater.LatestUrl);
            r.UserAgent = "DRAFT";
            r.Accept = "application/vnd.github+json";
            using (ct.Register(r.Abort))
            using (new Timer(_ => r.Abort(), null, TimeoutMs, Timeout.Infinite))
            {
                try
                {
                    using (HttpWebResponse w = (HttpWebResponse)await r.GetResponseAsync())
                    {
                        UpdateInfo info = AppUpdater.ParseRelease(await ReadLimited(w.GetResponseStream(), 2 * 1024 * 1024, ct));
                        if (info == null) throw new UpdateError("La dernière version publiée n'est pas utilisable par la mise à jour automatique.");
                        return info;
                    }
                }
                catch (WebException e)
                {
                    ct.ThrowIfCancellationRequested();
                    HttpWebResponse h = e.Response as HttpWebResponse;
                    if (h != null && h.StatusCode == HttpStatusCode.NotFound) throw new UpdateError("Aucune version publiée pour le moment.");
                    if (h != null && h.StatusCode == HttpStatusCode.Forbidden) throw new UpdateError("GitHub limite les requêtes pour le moment. Réessaie dans une heure.");
                    throw;
                }
            }
        }

        public async Task DownloadAsync(UpdateInfo info, string path, IProgress<long> progress, CancellationToken ct)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(info.AssetUrl);
            r.UserAgent = "DRAFT";
            r.ReadWriteTimeout = TimeoutMs;
            using (ct.Register(r.Abort))
            using (HttpWebResponse w = (HttpWebResponse)await r.GetResponseAsync())
            {
                string host = w.ResponseUri.Host.ToLowerInvariant();
                if (w.ResponseUri.Scheme != "https" || !(host == "github.com" || host.EndsWith(".githubusercontent.com"))) throw new UpdateError("Le téléchargement a été redirigé vers un site inconnu. Rien n'a été changé.");
                using (Stream input = w.GetResponseStream())
                using (FileStream output = File.Create(path))
                {
                    byte[] buffer = new byte[64 * 1024];
                    long total = 0;
                    int n;
                    while ((n = await input.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        total += n;
                        if (total > info.Size) throw new UpdateError("Le fichier reçu est plus gros qu'annoncé. Rien n'a été changé.");
                        await output.WriteAsync(buffer, 0, n, ct);
                        if (progress != null) progress.Report(total);
                    }
                }
            }
        }
    }

    // Pour les essais : une « release » faite d'un fichier local.
    public class LocalUpdateSource : IUpdateSource
    {
        readonly string file;
        readonly RSACryptoServiceProvider key = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false };
        public LocalUpdateSource(string file) { this.file = file; }
        public string PublicKey { get { return key.ToXmlString(false); } }
        public Task<string> SignatureAsync(UpdateInfo info, CancellationToken ct)
        {
            return Task.FromResult(Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(AppUpdater.SignedText(info.Tag, info.Sha256)), "SHA256")));
        }
        public Task<UpdateInfo> LatestAsync(CancellationToken ct)
        {
            Version v = new Version(FileVersionInfo.GetVersionInfo(file).FileVersion);
            return Task.FromResult(new UpdateInfo { Version = new Version(v.Major, v.Minor, v.Build), Tag = "v" + v.ToString(3), AssetUrl = file, Size = new FileInfo(file).Length, Sha256 = AppUpdater.Sha256Of(file) });
        }
        public Task DownloadAsync(UpdateInfo info, string path, IProgress<long> progress, CancellationToken ct)
        {
            File.Copy(file, path, true);
            return Task.FromResult(0);
        }
    }

    static class AppUpdaterTests
    {
        class Sim : IUpdateSource
        {
            public byte[] Bytes; public bool Fail; public RSACryptoServiceProvider Key; public string Forced;
            public string PublicKey { get { return Key.ToXmlString(false); } }
            public Task<string> SignatureAsync(UpdateInfo info, CancellationToken ct) { return Task.FromResult(Forced ?? Sign(Key, info.Tag, info.Sha256)); }
            public Task<UpdateInfo> LatestAsync(CancellationToken ct) { return Task.FromResult<UpdateInfo>(null); }
            public Task DownloadAsync(UpdateInfo info, string path, IProgress<long> progress, CancellationToken ct)
            {
                if (Fail) throw new WebException("simulée");
                File.WriteAllBytes(path, Bytes);
                return Task.FromResult(0);
            }
        }

        static string Sign(RSACryptoServiceProvider key, string tag, string sha) { return Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(AppUpdater.SignedText(tag, sha)), "SHA256")); }

        static string Release(string tag, string name, string url, long size, string digest, bool draft = false, bool pre = false)
        {
            return new JavaScriptSerializer().Serialize(new Dictionary<string, object> { { "tag_name", tag }, { "draft", draft }, { "prerelease", pre }, { "assets", new object[] {
                new Dictionary<string, object> { { "name", "DRAFT-Setup.exe" }, { "size", size }, { "digest", digest }, { "browser_download_url", AppUpdater.AssetPrefix + tag + "/DRAFT-Setup.exe" } },
                new Dictionary<string, object> { { "name", name }, { "size", size }, { "digest", digest }, { "browser_download_url", url } },
                new Dictionary<string, object> { { "name", name + ".sig" }, { "size", 500 }, { "browser_download_url", url + ".sig" } } } } });
        }

        public static void Run(Action<bool, string> t, string dataDir, string self)
        {
            t(AppUpdater.ParseTag("v2.16.1") == new Version(2, 16, 1) && AppUpdater.ParseTag("2.9.0") == new Version(2, 9, 0), "Mise à jour : numéro de version lu");
            t(AppUpdater.ParseTag("v2.16.0-beta.1") == null && AppUpdater.ParseTag("nimporte") == null && AppUpdater.ParseTag(null) == null, "Mise à jour : préversions et noms libres ignorés");
            Version now = new Version(2, 16, 0);
            t(AppUpdater.IsNewer(now, new Version(2, 16, 1)) && AppUpdater.IsNewer(now, new Version(3, 0, 0)), "Mise à jour : une version plus récente est proposée");
            t(!AppUpdater.IsNewer(now, new Version(2, 16, 0)) && !AppUpdater.IsNewer(now, new Version(2, 9, 9)) && !AppUpdater.IsNewer(now, null), "Mise à jour : jamais de retour en arrière (2.9 < 2.16)");

            string hash = new string('a', 64), tag = "v2.17.0", good = AppUpdater.AssetPrefix + tag + "/DRAFT-Portable.exe";
            long size = 2000000;
            UpdateInfo info = AppUpdater.ParseRelease(Release(tag, "DRAFT-Portable.exe", good, size, "sha256:" + hash));
            t(info != null && info.Version == new Version(2, 17, 0) && info.AssetUrl == good && info.Size == size && info.Sha256 == hash && info.SignatureUrl == good + ".sig", "Mise à jour : la version portable et sa signature sont choisies, jamais l'installateur");
            t(AppUpdater.ReleaseKey.StartsWith("<RSAKeyValue>") && !AppUpdater.ReleaseKey.Contains("<D>"), "Mise à jour : seule la clé publique est embarquée");
            t(AppUpdater.ParseRelease(Release(tag, "DRAFT-Portable.exe", good, size, "sha256:" + hash, draft: true)) == null, "Mise à jour : brouillon refusé");
            t(AppUpdater.ParseRelease(Release(tag, "DRAFT-Portable.exe", good, size, "sha256:" + hash, pre: true)) == null, "Mise à jour : préversion refusée");
            t(AppUpdater.ParseRelease(Release(tag, "DRAFT-Portable.exe", "https://exemple.test/DRAFT-Portable.exe", size, "sha256:" + hash)) == null, "Mise à jour : adresse étrangère refusée");
            t(AppUpdater.ParseRelease(Release(tag, "DRAFT-Portable.exe", AppUpdater.AssetPrefix + "v1.0.0/DRAFT-Portable.exe", size, "sha256:" + hash)) == null, "Mise à jour : fichier d'une autre version refusé");
            t(AppUpdater.ParseRelease(Release(tag, "DRAFT-Portable.exe", good, size, "")) == null, "Mise à jour : sans empreinte, refusé");
            t(AppUpdater.ParseRelease(Release(tag, "DRAFT-Portable.exe", good, 500L * 1024 * 1024, "sha256:" + hash)) == null, "Mise à jour : taille aberrante refusée");
            t(AppUpdater.ParseRelease(Release(tag, "Autre.zip", good, size, "sha256:" + hash)) == null && AppUpdater.ParseRelease("{pas du json") == null, "Mise à jour : release inutilisable ignorée");

            string dir = Path.Combine(dataDir, "test-maj");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            byte[] real = File.ReadAllBytes(self);
            Version selfVersion = new Version(FileVersionInfo.GetVersionInfo(self).FileVersion);
            UpdateInfo ok = new UpdateInfo { Version = new Version(selfVersion.Major, selfVersion.Minor, selfVersion.Build), Tag = "v" + selfVersion.ToString(3), Size = real.Length, Sha256 = AppUpdater.Sha256Of(self) };
            Func<string, UpdateInfo, bool> refused = (path, i) => { try { AppUpdater.CheckFile(path, i); return false; } catch (UpdateError) { return true; } };
            string copy = Path.Combine(dir, "copie.exe");
            File.WriteAllBytes(copy, real);
            t(!refused(copy, ok), "Mise à jour : fichier conforme accepté");
            t(refused(Path.Combine(dir, "absent.exe"), ok), "Mise à jour : fichier absent refusé");
            t(refused(copy, new UpdateInfo { Version = ok.Version, Size = ok.Size + 1, Sha256 = ok.Sha256 }), "Mise à jour : taille différente refusée");
            t(refused(copy, new UpdateInfo { Version = ok.Version, Size = ok.Size, Sha256 = new string('0', 64) }), "Mise à jour : empreinte différente refusée");
            t(refused(copy, new UpdateInfo { Version = new Version(9, 9, 9), Size = ok.Size, Sha256 = ok.Sha256 }), "Mise à jour : version interne différente refusée");
            byte[] notExe = (byte[])real.Clone(); notExe[0] = 0;
            string fake = Path.Combine(dir, "faux.exe");
            File.WriteAllBytes(fake, notExe);
            t(refused(fake, new UpdateInfo { Version = ok.Version, Size = ok.Size, Sha256 = AppUpdater.Sha256Of(fake) }), "Mise à jour : fichier non exécutable refusé");

            string exe = Path.Combine(dir, "DRAFT.exe"), candidate = AppUpdater.CandidatePath(exe), old = AppUpdater.OldPath(exe);
            Action<string> place = text => { foreach (string f in new[] { exe, candidate, old }) if (File.Exists(f)) File.Delete(f); File.WriteAllText(exe, text); };
            Func<Exception, Func<Task>, Exception> caught = (none, f) => { try { Task.Run(f).GetAwaiter().GetResult(); return none; } catch (Exception e) { return e; } };
            place("ancien");
            File.WriteAllText(candidate, "nouveau");
            AppUpdater.Swap(exe, candidate);
            t(File.ReadAllText(exe) == "nouveau" && File.ReadAllText(old) == "ancien" && !File.Exists(candidate), "Mise à jour : échange, avec l'ancienne version gardée");
            AppUpdater.Rollback(exe);
            t(File.ReadAllText(exe) == "ancien" && !File.Exists(old), "Mise à jour : retour à l'ancienne version");
            place("ancien");
            bool failed = false;
            try { AppUpdater.Swap(exe, candidate); } catch (FileNotFoundException) { failed = true; }
            t(failed && File.ReadAllText(exe) == "ancien" && !File.Exists(old), "Mise à jour : échange raté, version actuelle remise en place");
            File.WriteAllText(old, "x"); File.WriteAllText(candidate, "y");
            AppUpdater.CleanUp(exe);
            t(!File.Exists(old) && !File.Exists(candidate) && File.Exists(exe), "Mise à jour : restes supprimés au démarrage");

            using (RSACryptoServiceProvider key = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false }, other = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false })
            {
                string pub = key.ToXmlString(false), sha = new string('b', 64), signed = Sign(key, "v2.17.0", sha);
                t(AppUpdater.ValidSignature("v2.17.0", sha, signed, pub), "Mise à jour : signature valide acceptée");
                t(!AppUpdater.ValidSignature("v2.18.0", sha, signed, pub), "Mise à jour : ancien fichier signé présenté comme une autre version, refusé");
                t(!AppUpdater.ValidSignature("v2.17.0", new string('c', 64), signed, pub), "Mise à jour : autre fichier, refusé");
                t(!AppUpdater.ValidSignature("v2.17.0", sha, Sign(other, "v2.17.0", sha), pub), "Mise à jour : signature d'une autre clé refusée");
                t(!AppUpdater.ValidSignature("v2.17.0", sha, "pas de la base64 !", pub) && !AppUpdater.ValidSignature("v2.17.0", sha, null, pub), "Mise à jour : signature absente ou illisible refusée");

            Action<string, string, CancellationToken> pass = (c, d, ct) => { }, stop = (c, d, ct) => { throw new UpdateError("tests"); };
            exe = Path.Combine(dir, "DRAFT.exe"); place("ancien");
            string prepared = Task.Run(() => AppUpdater.Prepare(new Sim { Bytes = real, Key = key }, ok, exe, dir, null, pass, CancellationToken.None)).GetAwaiter().GetResult();
            t(prepared == candidate && File.Exists(candidate) && File.ReadAllText(exe) == "ancien" && !Directory.Exists(Path.Combine(dir, "maj-test")), "Mise à jour : préparation sans toucher à la version actuelle");
            place("ancien");
            Exception bad = caught(null, () => AppUpdater.Prepare(new Sim { Bytes = real, Key = key }, ok, exe, dir, null, stop, CancellationToken.None));
            t(bad is UpdateError && !File.Exists(candidate) && File.ReadAllText(exe) == "ancien", "Mise à jour : tests ratés, candidat supprimé et version actuelle intacte");
            bad = caught(null, () => AppUpdater.Prepare(new Sim { Fail = true, Key = key }, ok, exe, dir, null, pass, CancellationToken.None));
            t(bad is WebException && !File.Exists(candidate) && File.ReadAllText(exe) == "ancien", "Mise à jour : réseau coupé, version actuelle intacte");
            bad = caught(null, () => AppUpdater.Prepare(new Sim { Bytes = notExe, Key = key }, ok, exe, dir, null, pass, CancellationToken.None));
            t(bad is UpdateError && !File.Exists(candidate) && File.ReadAllText(exe) == "ancien", "Mise à jour : fichier abîmé, version actuelle intacte");
            place("ancien");
            bool ran = false;
            bad = caught(null, () => AppUpdater.Prepare(new Sim { Bytes = real, Key = key, Forced = Sign(other, ok.Tag, ok.Sha256) }, ok, exe, dir, null, (c, d, ct) => { ran = true; }, CancellationToken.None));
            t(bad is UpdateError && !ran && !File.Exists(candidate) && File.ReadAllText(exe) == "ancien", "Mise à jour : fausse signature, fichier supprimé sans jamais être lancé");
            }
            bool big = false;
            try { GitHubUpdateSource.ReadLimited(new MemoryStream(new byte[40000]), 16 * 1024, CancellationToken.None).GetAwaiter().GetResult(); } catch (UpdateError) { big = true; }
            t(big && GitHubUpdateSource.ReadLimited(new MemoryStream(Encoding.UTF8.GetBytes("abc")), 16 * 1024, CancellationToken.None).GetAwaiter().GetResult() == "abc", "Mise à jour : réponse réseau plafonnée");
            t(AppUpdater.FriendlyError(new WebException("x")).Contains("GitHub") && AppUpdater.FriendlyError(new UpdateError("message")) == "message", "Mise à jour : erreurs compréhensibles");
            Directory.Delete(dir, true);
        }
    }
}
