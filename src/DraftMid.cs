// DRAFT : application compagnon Windows. Fenêtres natives autour de l'interface web existante.
// Suit la fenêtre du client via l'API Windows. Si l'option est cochée, lit la phase et les champions adverses
// via l'API locale du client, en lecture seule. Aucune injection ni lecture de mémoire.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

[assembly: AssemblyTitle("DRAFT")]
[assembly: AssemblyDescription("DRAFT : ton assistant stratégique pour League of Legends. Non approuvé par Riot Games.")]
[assembly: AssemblyProduct("DRAFT")]
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]
// Seule donnée qui fixe l'édition (classe Edition) : un « - » signifie préversion. Doit être égale à APP_VERSION (web/index.html).
[assembly: AssemblyInformationalVersion("3.0.0-beta.1")]

namespace DraftMid
{
    public static class Native
    {
        public delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc f, IntPtr l);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);

        // Barre de titre sombre (UX-7) : DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Windows 10 20H1 et 11), 19 avant. Repli silencieux :
        // sur un Windows qui ne le connaît pas, la barre reste claire, rien d'autre ne change. Vrai si Windows a accepté.
        public static bool DarkTitleBar(IntPtr hwnd)
        {
            int on = 1;
            try { return DwmSetWindowAttribute(hwnd, 20, ref on, 4) == 0 || DwmSetWindowAttribute(hwnd, 19, ref on, 4) == 0; }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }
        [DllImport("user32.dll")] public static extern IntPtr MonitorFromRect(ref RECT r, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool GetMonitorInfo(IntPtr m, ref MONITORINFO i);
        [DllImport("user32.dll")] public static extern bool EnumDisplayMonitors(IntPtr d, IntPtr c, MonitorProc f, IntPtr l);
        [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr m, int type, out uint x, out uint y);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int ht, uint flags);
        public delegate bool MonitorProc(IntPtr m, IntPtr dc, ref RECT r, IntPtr l);
        public const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10, SWP_NOZORDER = 0x4, SWP_SHOWWINDOW = 0x40;
        public static readonly IntPtr PMV2 = new IntPtr(-4);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int W { get { return Right - Left; } }
            public int H { get { return Bottom - Top; } }
            public RECT(int l, int t, int r, int b) { Left = l; Top = t; Right = r; Bottom = b; }
            public bool Overlaps(RECT o) { return Left < o.Right && o.Left < Right && Top < o.Bottom && o.Top < Bottom; }
            public bool Inside(RECT o) { return Left >= o.Left && Top >= o.Top && Right <= o.Right && Bottom <= o.Bottom; }
            public override string ToString() { return Left + "," + Top + " " + W + "x" + H; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor, rcWork;
            public uint dwFlags;
        }
    }

    public class Monitor { public Native.RECT Work; public double Scale = 1; public bool Primary; }

    public class Placement
    {
        public string Mode;              // "two", "compact" ou "manual"
        public Native.RECT Left, Right, Single;
        public Msg Note;
    }

    // Calcul pur, sans fenêtre : testé par --selftest sur plusieurs écrans et résolutions.
    public static class Layout
    {
        const int MinW = 230, IdealW = 340, MinH = 520, Gap = 0;

        // QA-4 : une position enregistrée par l'utilisateur n'est reprise que si le panneau reste utilisable sur un écran présent :
        // au moins la moitié de sa largeur et toute sa barre de titre (40 px à l'échelle de l'écran) visibles. Sinon (écran débranché,
        // résolution réduite, écran déplacé), la place par défaut. Avant : un seul pixel commun suffisait.
        public static bool KeepSaved(Native.RECT saved, IList<Monitor> monitors)
        {
            if (saved.W <= 0 || saved.H <= 0) return false;
            foreach (Monitor m in monitors)
            {
                int visible = Math.Min(saved.Right, m.Work.Right) - Math.Max(saved.Left, m.Work.Left);
                if (visible * 2 >= saved.W && saved.Top >= m.Work.Top && saved.Top + (int)(40 * m.Scale) <= m.Work.Bottom) return true;
            }
            return false;
        }

        public static Placement Compute(Native.RECT client, IList<Monitor> monitors, bool preferCompact)
        {
            Monitor home = monitors.OrderByDescending(m => OverlapArea(m.Work, client)).First();
            int leftFree = client.Left - home.Work.Left - Gap;
            int rightFree = home.Work.Right - client.Right - Gap;
            int min = (int)(MinW * home.Scale);
            int ideal = (int)(IdealW * home.Scale);

            if (!preferCompact && leftFree >= min && rightFree >= min)
            {
                int y, h;
                Vertical(client, home, out y, out h);
                return new Placement
                {
                    Mode = "two",
                    Left = new Native.RECT(client.Left - Gap - Math.Min(ideal, leftFree), y, client.Left - Gap, y + h),
                    Right = new Native.RECT(client.Right + Gap, y, client.Right + Gap + Math.Min(ideal, rightFree), y + h),
                    Note = new Msg("native.placement.two")
                };
            }
            if (Math.Max(leftFree, rightFree) >= min)
            {
                int y, h;
                Vertical(client, home, out y, out h);
                Native.RECT r = rightFree >= leftFree
                    ? new Native.RECT(client.Right + Gap, y, client.Right + Gap + Math.Min((int)(ideal * 1.1), rightFree), y + h)
                    : new Native.RECT(client.Left - Gap - Math.Min((int)(ideal * 1.1), leftFree), y, client.Left - Gap, y + h);
                return new Placement { Mode = "compact", Single = r, Note = new Msg(preferCompact ? "native.placement.compactChosen" : "native.placement.compactNoRoom") };
            }
            foreach (Monitor other in monitors.Where(m => m != home && !m.Work.Overlaps(client)).OrderByDescending(m => m.Work.W))
            {
                int w = (int)(IdealW * other.Scale), h = Math.Min(other.Work.H, (int)(900 * other.Scale));
                if (!preferCompact && other.Work.W >= 2 * w + Gap)
                {
                    bool nearRight = other.Work.Left < home.Work.Left;
                    int x = nearRight ? other.Work.Right - 2 * w - Gap : other.Work.Left;
                    return new Placement
                    {
                        Mode = "two",
                        Left = new Native.RECT(x, other.Work.Top, x + w, other.Work.Top + h),
                        Right = new Native.RECT(x + w + Gap, other.Work.Top, x + 2 * w + Gap, other.Work.Top + h),
                        Note = new Msg("native.placement.otherScreenTwo")
                    };
                }
                if (other.Work.W >= w)
                {
                    int x = other.Work.Left < home.Work.Left ? other.Work.Right - w : other.Work.Left;
                    return new Placement { Mode = "compact", Single = new Native.RECT(x, other.Work.Top, x + w, other.Work.Top + h), Note = new Msg("native.placement.otherScreenOne") };
                }
            }
            return new Placement { Mode = "manual", Note = new Msg("native.placement.manual") };
        }

        // Aucune place libre : un seul panneau étroit, au bord de l'écran, couvre le moins possible du client.
        public static Placement Narrowest(Native.RECT client, IList<Monitor> monitors)
        {
            Monitor home = monitors.OrderByDescending(m => OverlapArea(m.Work, client)).First();
            int y, h, w = Math.Min((int)(MinW * home.Scale), home.Work.W);
            Vertical(client, home, out y, out h);
            return new Placement
            {
                Mode = "compact",
                Single = new Native.RECT(home.Work.Right - w, y, home.Work.Right, y + h),
                Note = new Msg("native.placement.narrow")
            };
        }

        static void Vertical(Native.RECT client, Monitor m, out int y, out int h)
        {
            h = Math.Min(Math.Max(client.H, (int)(MinH * m.Scale)), m.Work.H);
            // Centré sur le client, pour rester aligné même quand le panneau est plus haut que lui.
            y = Math.Max(m.Work.Top, Math.Min(client.Top - (h - client.H) / 2, m.Work.Bottom - h));
        }

        static long OverlapArea(Native.RECT a, Native.RECT b)
        {
            long w = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left);
            long h = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);
            return w > 0 && h > 0 ? w * h : 0;
        }
    }

    public class SavedRect { public int X, Y, W, H; }

    public class UserPreferences
    {
        public const int MaxSamples = 12;
        public bool Compact;
        public bool Follow = true;
        public bool AutoDraft = true;
        public bool Topmost;
        public bool Diagnostics;
        // Lecture du client : désactivée par défaut, à cocher dans les réglages.
        public bool LcuEnabled;
        // Langue (I18N-3) : "auto" (langue de Windows), "fr" ou "en". Absent (installations 2.x) : comme "auto".
        public string Language;
        // Installation déjà passée aux profils : sinon, elle garde le profil d'origine sans rien refaire.
        public bool ProfileKnown;
        // Copie du profil pour le calcul des matchups, en noms de Riot. Aucune donnée personnelle.
        public string ProfileRole;
        public string[] ProfilePool;
        public Dictionary<string, SavedRect> Rects = new Dictionary<string, SavedRect>();
        // Signatures réduites apprises localement : aucune image n'est conservée.
        public int[] NavBaseline;
        public List<int[]> DraftSamples = new List<int[]>();
        public List<int[]> OtherSamples = new List<int[]>();

        public static UserPreferences Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    JavaScriptSerializer js = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
                    UserPreferences p = js.Deserialize<UserPreferences>(File.ReadAllText(path));
                    if (p != null)
                    {
                        if (p.DraftSamples == null) p.DraftSamples = new List<int[]>();
                        if (p.OtherSamples == null) p.OtherSamples = new List<int[]>();
                        if (p.Rects == null) p.Rects = new Dictionary<string, SavedRect>();
                        return p;
                    }
                }
            }
            catch { }
            return new UserPreferences();
        }

        public void Save(string path)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 }.Serialize(this));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }

    // Édition de l'exécutable, déduite d'une seule donnée de build : AssemblyInformationalVersion. Un « - » (3.0.0-beta.1) signifie
    // préversion : dossier de données, instance unique, démarrage automatique et raccourci séparés de la 2.x installée, ni
    // installation ni mise à jour automatique. Sans « - », l'édition stable garde exactement les noms d'avant (DraftMid).
    public static class Edition
    {
        public const string StableName = "DraftMid", BetaName = "DRAFT-beta";
        public static readonly string Version = InformationalOf(typeof(Edition).Assembly);
        public static readonly bool Prerelease = IsPrerelease(Version);

        public static string InformationalOf(Assembly a)
        {
            AssemblyInformationalVersionAttribute info = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(a, typeof(AssemblyInformationalVersionAttribute));
            return info != null && info.InformationalVersion != "" ? info.InformationalVersion : a.GetName().Version.ToString(3);
        }
        public static bool IsPrerelease(string version) { return version != null && version.IndexOf('-') >= 0; }

        // Noms propres à chaque édition. Stable : ceux de la 2.x, inchangés.
        public static string Name(bool prerelease) { return prerelease ? BetaName : StableName; }
        public static string DefaultDataDir(bool prerelease) { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name(prerelease)); }
        public static string MutexName(bool prerelease, string dataDirOption) { return Name(prerelease) + (dataDirOption == null ? ".Instance" : ".Test." + dataDirOption.GetHashCode()); }
        public static string RunValue(bool prerelease) { return Name(prerelease); }

        // Préversion : ni --install, ni --uninstall, ni --test-install, ni installation automatique d'un exe nommé « Setup ».
        public static bool RefusesInstall(bool prerelease, Options o, string self)
        {
            return prerelease && (o.Install || o.Uninstall || o.TestInstall || Path.GetFileName(self ?? "").IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Préversion : Ctrl+Alt+D laissé à la 2.x (« instance » : un autre DRAFT tourne dans la session ; « autostart » : la 2.x
        // démarre avec Windows), sauf si le joueur l'a demandé dans Réglages > Affichage. Null : le raccourci est tenté.
        public static string HotkeyLeftReason(bool prerelease, bool forced, bool otherDraftRunning, bool legacyAutostart)
        {
            if (!prerelease || forced) return null;
            if (otherDraftRunning) return "instance";
            if (legacyAutostart) return "autostart";
            return null;
        }

        // Autres processus DRAFT ou DraftMid de la session (noms et numéros seulement, aucun échange avec eux).
        public static List<string> OtherDraftProcesses()
        {
            List<string> found = new List<string>();
            using (Process me = Process.GetCurrentProcess())
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        string n = p.ProcessName;
                        if (p.Id != me.Id && p.SessionId == me.SessionId && (n.StartsWith("DraftMid", StringComparison.OrdinalIgnoreCase) || n.StartsWith("DRAFT", StringComparison.OrdinalIgnoreCase))) found.Add(n + /* i18n-ignore : nom et numéro de processus, pour les rapports de test */ ".exe pid " + p.Id);
                    }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                    finally { p.Dispose(); }
                }
            }
            return found;
        }
    }

    // Préversion : état propre à la bêta (copie initiale des données 2.x, choix explicite de Ctrl+Alt+D), dans son propre dossier.
    public class BetaState
    {
        public const string FileName = "draft-beta.json";
        public string CopiedAt;
        public bool SourceFound;
        public int Files, Skipped;
        public bool Hotkey;

        public static BetaState Load(string dataDir)
        {
            try
            {
                string path = Path.Combine(dataDir, FileName);
                if (File.Exists(path)) return new JavaScriptSerializer().Deserialize<BetaState>(File.ReadAllText(path, Encoding.UTF8)) ?? new BetaState();
            }
            catch (Exception e) { if (e is OutOfMemoryException) throw; }
            return new BetaState();
        }

        public void Save(string dataDir)
        {
            try
            {
                string path = Path.Combine(dataDir, FileName), tmp = path + ".tmp";
                File.WriteAllText(tmp, new JavaScriptSerializer().Serialize(this), new UTF8Encoding(false));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    // Préversion, premier lancement : COPIE des données d'une 2.x (réglages, profils, pools, journal, historique) vers le dossier
    // de la bêta. La source est seulement lue : chaque fichier est ouvert en lecture partagée (la 2.x peut écrire en même temps),
    // jamais écrit, renommé, déplacé ni verrouillé. Un fichier qu'une autre application tient en exclusivité est sauté et compté.
    public static class LegacyCopy
    {
        // Fichiers de données à la racine du dossier 2.x : réglages, profils, historique, statistiques, rôles, matchups, relevés.
        static readonly string[] Extensions = { ".json", ".js", ".csv", ".precedent" };
        // Stockage de la page (pools, journal, profils) : seul ce dossier de WebView2 est repris, sans cache ni fichier de verrou.
        public static readonly string StorageRelative = Path.Combine("webview", "EBWebView", "Default", /* i18n-ignore : nom de dossier de WebView2 */ "Local Storage");

        public static bool IsDataFile(string name)
        {
            string n = name.ToLowerInvariant();
            return !n.EndsWith(".tmp") && !n.StartsWith("selftest") && Extensions.Any(n.EndsWith);
        }

        public static bool IsEmpty(string dir) { return !Directory.Exists(dir) || !Directory.EnumerateFileSystemEntries(dir).Any(); }

        public static BetaState Run(string source, string target)
        {
            BetaState st = new BetaState { CopiedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) };
            if (source == null || !Directory.Exists(source)) return st;
            st.SourceFound = true;
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
                if (IsDataFile(Path.GetFileName(file))) CopyOne(file, Path.Combine(target, Path.GetFileName(file)), st);
            string storage = Path.Combine(source, StorageRelative);
            if (Directory.Exists(storage))
                foreach (string file in Directory.GetFiles(storage, "*", SearchOption.AllDirectories))
                {
                    if (string.Equals(Path.GetFileName(file), "LOCK", StringComparison.OrdinalIgnoreCase)) continue;
                    CopyOne(file, Path.Combine(target, StorageRelative, file.Substring(storage.Length + 1)), st);
                }
            return st;
        }

        static void CopyOne(string from, string to, BetaState st)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                using (FileStream input = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (FileStream output = new FileStream(to, FileMode.Create, FileAccess.Write, FileShare.None)) input.CopyTo(output);
                st.Files++;
            }
            catch (IOException) { st.Skipped++; }
            catch (UnauthorizedAccessException) { st.Skipped++; }
        }
    }

public static class Autostart
    {
        public const string DefaultKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static string Key = DefaultKey;
        // Stable : « DraftMid », comme la 2.x. Préversion : « DRAFT-beta ».
        static readonly string Value = Edition.RunValue(Edition.Prerelease);
        public static string ValueName { get { return Value; } }
        // Préversion : existence de la valeur de la 2.x, par son nom seulement. Sa donnée n'est ni lue ni modifiée.
        public static bool LegacyValueExists()
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Key))
                return k != null && k.GetValueNames().Any(n => string.Equals(n, Edition.StableName, StringComparison.OrdinalIgnoreCase));
        }
        public static bool IsOn() { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Key)) return k != null && k.GetValue(Value) != null; }
        public static string Target() { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Key)) { object v = k == null ? null : k.GetValue(Value); return v == null ? null : v.ToString().Trim('"'); } }
        // Un seul démarrage automatique : s'il visait l'ancien fichier, il vise désormais le nouveau.
        public static void Repoint(string oldExe, string newExe)
        {
            string current = Target();
            if (current != null && string.Equals(Path.GetFullPath(current), Path.GetFullPath(oldExe), StringComparison.OrdinalIgnoreCase)) Set(true, newExe);
        }
        public static void Set(bool on, string exe)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Key))
            {
                if (on) k.SetValue(Value, "\"" + exe + "\"");
                else k.DeleteValue(Value, false);
            }
        }
    }

    public static class ClientWindow
    {
        public static List<Monitor> Monitors()
        {
            List<Monitor> list = new List<Monitor>();
            Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr m, IntPtr dc, ref Native.RECT r, IntPtr l) =>
            {
                Native.MONITORINFO i = new Native.MONITORINFO();
                i.cbSize = Marshal.SizeOf(typeof(Native.MONITORINFO));
                Native.GetMonitorInfo(m, ref i);
                uint dx, dy;
                double scale = Native.GetDpiForMonitor(m, 0, out dx, out dy) == 0 ? dx / 96.0 : 1;
                list.Add(new Monitor { Work = i.rcWork, Scale = scale, Primary = (i.dwFlags & 1) != 0 });
                return true;
            }, IntPtr.Zero);
            return list;
        }
    }

    public class PanelForm : Form
    {
        public readonly string Id;
        public WebView2 View;
        public Task Ready = Task.FromResult(true);
        readonly CompanionApp app;
        const int Border = 6;

        public PanelForm(string id, CompanionApp app)
        {
            Id = id;
            this.app = app;
            // La fenêtre complète est une vraie fenêtre Windows : barre de titre, déplacement et fermeture.
            FormBorderStyle = id == "full" ? FormBorderStyle.Sizable : FormBorderStyle.None;
            ShowInTaskbar = id == "full";
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(10, 15, 22);
            // La marge sert à redimensionner : aucune du côté collé au client, pour ne pas laisser de cadre visible.
            Padding = id == "full" ? Padding.Empty : id == "left" ? new Padding(Border, Border, 0, Border) : id == "right" ? new Padding(0, Border, Border, Border) : new Padding(Border);
            Text = id == "full" ? "DRAFT" : "DRAFT : " + id;
            if (app.AppIcon != null) Icon = app.AppIcon;
            View = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.FromArgb(10, 15, 22) };
            Controls.Add(View);
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        public bool DarkTitle;
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (Id == "full") DarkTitle = Native.DarkTitleBar(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84, WM_ENTERSIZEMOVE = 0x231, WM_EXITSIZEMOVE = 0x232, WM_DPICHANGED = 0x2E0;
            if (m.Msg == WM_DPICHANGED) { base.WndProc(ref m); app.OnDpiChanged(); return; }
            if (m.Msg == WM_ENTERSIZEMOVE) beforeMove = Bounds;
            if (m.Msg == WM_NCHITTEST && Id != "full")
            {
                Point p = PointToClient(new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                bool l = p.X < Border, r = p.X >= ClientSize.Width - Border, t = p.Y < Border, b = p.Y >= ClientSize.Height - Border;
                int hit = t && l ? 13 : t && r ? 14 : b && l ? 16 : b && r ? 17 : l ? 10 : r ? 11 : t ? 12 : b ? 15 : 0;
                if (hit != 0) { m.Result = new IntPtr(hit); return; }
            }
            base.WndProc(ref m);
            if (m.Msg == WM_EXITSIZEMOVE && Id != "full")
            {
                // Un simple clic sur un bord ne doit pas couper le suivi du client.
                if (MovedOnPurpose(beforeMove, Bounds)) app.PanelMovedByUser(this);
                else app.UpdatePlacement(true);
            }
        }

        Rectangle beforeMove;

        public static bool MovedOnPurpose(Rectangle before, Rectangle after)
        {
            const int Tolerance = 8;
            return Math.Abs(before.Left - after.Left) > Tolerance || Math.Abs(before.Top - after.Top) > Tolerance
                || Math.Abs(before.Right - after.Right) > Tolerance || Math.Abs(before.Bottom - after.Bottom) > Tolerance;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!app.Quitting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); if (Id == "full") app.OnMainWindowClosed(); return; }
            base.OnFormClosing(e);
        }

        public void DisposeForRelease() { app.Quitting = true; try { Close(); } finally { app.Quitting = false; } Dispose(); }

        public void PlaceNoActivate(Native.RECT r, bool topmost)
        {
            IntPtr after = topmost ? new IntPtr(-1) : new IntPtr(-2);
            Native.SetWindowPos(Handle, after, r.Left - 0, r.Top, r.W, r.H, Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
        }
    }

    public class HotkeyWindow : NativeWindow, IDisposable
    {
        public const uint Alt = 0x0001, Ctrl = 0x0002, Shift = 0x0004, NoRepeat = 0x4000;
        public const int WM_HOTKEY = 0x0312, AlreadyRegistered = 1409;
        public event Action Pressed;
        public readonly bool Registered;
        // Code Win32 de l'échec (0 si enregistré). 1409 (ERROR_HOTKEY_ALREADY_REGISTERED) : un autre programme tient la combinaison.
        public readonly int Error;
        public readonly int Id;
        public int Received;
        // Ctrl + Alt + D, sans répétition automatique.
        public HotkeyWindow() : this(Ctrl | Alt | NoRepeat, (uint)Keys.D, 1) { }
        // QA-6 : une autre combinaison (essai isolé, sans toucher au Ctrl+Alt+D d'une autre instance).
        public HotkeyWindow(uint modifiers, uint key, int id)
        {
            Id = id;
            CreateHandle(new CreateParams());
            Registered = NativeHook.RegisterHotKey(Handle, id, modifiers, key);
            Error = Registered ? 0 : System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt64() == Id) { Received++; Action p = Pressed; if (p != null) p(); }
            base.WndProc(ref m);
        }
        public void Dispose() { if (Registered) NativeHook.UnregisterHotKey(Handle, Id); DestroyHandle(); }

    }

    // Libellés du menu de l'icône : catalogue (native.menu.*, I18N-3), dans la langue du programme au moment de l'affichage.
    static class MenuText
    {
        public static string Open { get { return Strings.T("native.menu.open"); } }
        public static string ShowPanels { get { return Strings.T("native.menu.showPanels"); } }
        public static string HidePanels { get { return Strings.T("native.menu.hidePanels"); } }
        public static string Simulate { get { return Strings.T("native.menu.simulate"); } }
        public static string Detection { get { return Strings.T("native.menu.detection"); } }
        public static string TeachDraft { get { return Strings.T("native.menu.teachDraft"); } }
        public static string TeachNotDraft { get { return Strings.T("native.menu.teachNotDraft"); } }
        public static string LcuDiagnostic { get { return Strings.T("native.menu.lcuDiagnostic"); } }
        public static string Settings { get { return Strings.T("native.menu.settings"); } }
        public static string CheckUpdate { get { return Strings.T("native.menu.checkUpdate"); } }
        public static string Quit { get { return Strings.T("native.menu.quit"); } }
    }

    public class CompanionApp : ApplicationContext
    {
        const int SlowPollMs = 2000, SampleMs = 1000, MoveSettleMs = 150, ModeSettleMs = 350;
        bool movePosted;
        public double LastFollowLagMs = -1;
        readonly string dataDir, webDir, settingsPath, exePath;
        readonly Options opts;
        readonly UserPreferences settings;
        readonly NotifyIcon tray;
        readonly System.Windows.Forms.Timer slowPoll, sampler;
        readonly Dictionary<string, PanelForm> panels = new Dictionary<string, PanelForm>();
        readonly ClientWindowTracker tracker;
        readonly DraftPhaseDetector detector;
        readonly PhaseMachine machine = new PhaseMachine();
        readonly PanelVisibilityController visibility = new PanelVisibilityController();
        readonly FrameGrabber grabber;
        readonly Frame frame = new Frame();
        HotkeyWindow hotkey;
        CoreWebView2Environment env;
        Task envTask;
        string shownMode = "", pendingMode = "";
        long pendingSince;
        bool panelsShown, frameValid;
        Msg placementNote = new Msg("native.placement.noClient");
        public bool Quitting;
        public Icon AppIcon;
        ToolStripMenuItem miUpdate, miShow, miPhase, miOpen, miSimulate, miDetection, miTeachDraft, miTeachNotDraft, miLcuDiagnostic, miSettings, miQuit;
        public long MoveCount;

        public Phase CurrentPhase { get { return machine.Current; } }
        public bool PanelsShown { get { return panelsShown; } }
        public string ShownMode { get { return shownMode; } }
        public ClientWindowTracker Tracker { get { return tracker; } }
        public bool HotkeyRegistered { get { return hotkey != null && hotkey.Registered; } }
        public bool HotkeyCreated { get { return hotkey != null; } }
        public int HotkeyError { get { return hotkey != null ? hotkey.Error : 0; } }
        // Préversion : raison pour laquelle Ctrl+Alt+D est laissé à la 2.x (« instance », « autostart »), null sinon.
        public string HotkeyLeftReason { get { return hotkeyLeft; } }
        string hotkeyLeft;
        BetaState beta;

        // Préversion : Ctrl+Alt+D laissé à la 2.x si elle tourne dans la session ou démarre avec Windows (existence de la valeur
        // seulement), sauf choix explicite du joueur. Stable : toujours tenté, comme avant.
        void ApplyHotkeyPolicy()
        {
            hotkeyLeft = Edition.Prerelease ? Edition.HotkeyLeftReason(true, beta != null && beta.Hotkey, Edition.OtherDraftProcesses().Count > 0, Autostart.LegacyValueExists()) : null;
            if (hotkeyLeft == null && hotkey == null)
            {
                hotkey = new HotkeyWindow();
                hotkey.Pressed += ToggleManual;
            }
            else if (hotkeyLeft != null && hotkey != null) { hotkey.Dispose(); hotkey = null; }
        }

        // Réglages > Affichage, préversion : activer ou rendre Ctrl+Alt+D. Un échec (1409) reste signalé comme dans la 2.x.
        public void SetBetaHotkey(bool on)
        {
            if (!Edition.Prerelease || beta == null || opts.NoHotkey) return;
            beta.Hotkey = on;
            beta.Save(dataDir);
            if (on && hotkey != null && !hotkey.Registered) { hotkey.Dispose(); hotkey = null; }
            ApplyHotkeyPolicy();
            RefreshUi();
        }

        // Préversion : une autre DRAFT ouverte affiche aussi ses panneaux pendant une draft. C'est signalé (Accueil, Réglages >
        // Affichage, bulle au premier passage en draft de la session), avec un conseil ; aucune action sur l'autre processus.
        // Liste relue au démarrage, toutes les 10 s et à chaque début de draft. Source remplaçable par les tests.
        public Func<List<string>> OtherDraftSource = Edition.OtherDraftProcesses;
        public string OtherDraft { get { return otherDraft; } }
        string otherDraft = "";
        bool otherDraftWarned;
        int otherDraftTicks;

        string ComputeOtherDraft()
        {
            if (!Edition.Prerelease) return "";
            return string.Join(", ", OtherDraftSource().Select(n => n.Split(' ')[0]).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        }

        public void RefreshOtherDraft()
        {
            string names = ComputeOtherDraft();
            if (names != otherDraft) { otherDraft = names; RefreshUi(); }
        }

        void WarnOtherPanels()
        {
            if (!Edition.Prerelease) return;
            RefreshOtherDraft();
            if (otherDraft == "" || otherDraftWarned) return;
            otherDraftWarned = true;
            Notify(new Msg("native.beta.otherPanels", "names", otherDraft), ToolTipIcon.Info);
        }

        // Préversion, premier lancement : la copie des données 2.x est annoncée par une bulle, et l'Accueil s'ouvre pour l'expliquer.
        public void AnnounceBetaCopy(BetaState copied)
        {
            beta = copied;
            if (!opts.Silent) firstRun = true;
            if (copied.SourceFound) Notify(new Msg("native.beta.copied"), ToolTipIcon.Info);
            RefreshUi();
        }
        public UserPreferences Prefs { get { return settings; } }

        public PanelForm PanelFor(string id) { PanelForm f; return panels.TryGetValue(id, out f) ? f : null; }

        public CompanionApp(Options o, string dataDir, string webDir, string exePath, Func<IntPtr, bool> clientMatcher)
        {
            opts = o; this.dataDir = dataDir; this.webDir = webDir; this.exePath = exePath;
            settingsPath = Path.Combine(dataDir, "settings.json");
            bool hadSettings = File.Exists(settingsPath);
            firstRun = !hadSettings && !o.Silent;
            settings = UserPreferences.Load(settingsPath);
            if (o.NoAutoPanels) settings.AutoDraft = false;
            legacyProfile = (hadSettings && !settings.ProfileKnown) || o.SelfTest || o.EndToEnd;
            if (!hadSettings) { settings.ProfileKnown = true; try { settings.Save(settingsPath); } catch { } }
            detector = new DraftPhaseDetector(settings);
            grabber = new FrameGrabber();
            try { AppIcon = Icon.ExtractAssociatedIcon(exePath); } catch { }

            // Menu de l'icône : 8 entrées au plus (OVERLAY-V3 § 5), sans perte de fonctionnalité. Ce qui en sort est dans la fenêtre :
            // Réglages > Affichage et fenêtres (affichage automatique, collage, panneau unique, replacer), Réglages > Général (démarrage),
            // Réglages > Données (rôles, matchups), Mes statistiques (ancien fichier draft-stats.js). Libellés identiques à ceux de la page.
            ContextMenuStrip menu = new ContextMenuStrip();
            miPhase = new ToolStripMenuItem("") { Enabled = false };
            miOpen = new ToolStripMenuItem(MenuText.Open, null, (s, e) => OpenFull("home"));
            miOpen.Font = new Font(miOpen.Font, FontStyle.Bold);
            miShow = new ToolStripMenuItem(Strings.T("native.menu.showPanelsHotkey"), null, (s, e) => ToggleManual());
            miSimulate = new ToolStripMenuItem(MenuText.Simulate, null, (s, e) => OpenFull("simulate"));
            miDetection = new ToolStripMenuItem(MenuText.Detection);
            miTeachDraft = new ToolStripMenuItem(MenuText.TeachDraft, null, (s, e) => Teach(true));
            miTeachNotDraft = new ToolStripMenuItem(MenuText.TeachNotDraft, null, (s, e) => Teach(false));
            miLcuDiagnostic = new ToolStripMenuItem(MenuText.LcuDiagnostic, null, (s, e) => ShowLcuDiagnostic());
            miDetection.DropDownItems.Add(miTeachDraft);
            miDetection.DropDownItems.Add(miTeachNotDraft);
            miDetection.DropDownItems.Add(new ToolStripSeparator());
            miDetection.DropDownItems.Add(miLcuDiagnostic);
            miUpdate = new ToolStripMenuItem(MenuText.CheckUpdate, null, (s, e) => CheckForUpdate(true));
            miSettings = new ToolStripMenuItem(MenuText.Settings, null, (s, e) => OpenFull("settings"));
            miQuit = new ToolStripMenuItem(MenuText.Quit, null, (s, e) => Quit());
            menu.Items.Add(miPhase);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miOpen);
            menu.Items.Add(miShow);
            menu.Items.Add(miSimulate);
            menu.Items.Add(miDetection);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miSettings);
            menu.Items.Add(miUpdate);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miQuit);
            tray = new NotifyIcon { Icon = AppIcon ?? SystemIcons.Application, Text = "DRAFT", Visible = true, ContextMenuStrip = menu };
            openFromTray = () => OpenFull("home");
            // Double-clic : ouvrir DRAFT (décision de l'utilisateur). Les panneaux restent sur Ctrl+Alt+D et l'entrée « Afficher les panneaux ».
            tray.DoubleClick += (s, e) => openFromTray();

            // Essai à côté d'une autre version : le raccourci reste à celle-ci, sans même tenter de l'enregistrer.
            // Préversion : il reste aussi à la 2.x quand elle tourne ou démarre avec Windows, sauf choix du joueur (Réglages > Affichage).
            if (Edition.Prerelease) beta = BetaState.Load(dataDir);
            if (!o.NoHotkey) ApplyHotkeyPolicy();

            tracker = new ClientWindowTracker(clientMatcher, false);
            // Réaction immédiate au premier événement, puis au plus une mise à jour toutes les 16 ms.
            tracker.Changed += () =>
            {
                // Une seule mise à jour par passage de la file de messages, avec la dernière position connue :
                // une rafale se regroupe seule, sans attendre un minuteur qui laisserait les panneaux en retard.
                if (movePosted) return;
                movePosted = true;
                SynchronizationContext.Current.Post(_ => { movePosted = false; OnClientChanged(); }, null);
            };
            tracker.BroughtToFront += RaisePanels;
            slowPoll = new System.Windows.Forms.Timer { Interval = SlowPollMs };
            slowPoll.Tick += (s, e) => { tracker.Refresh(); OnClientChanged(); if (Edition.Prerelease && ++otherDraftTicks % 5 == 0) RefreshOtherDraft(); };
            otherDraft = ComputeOtherDraft();
            sampler = new System.Windows.Forms.Timer { Interval = SampleMs };
            sampler.Tick += (s, e) => Sample();
            SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
            RefreshUi();

            tracker.Refresh();
            slowPoll.Start();
            if (!opts.SimulatedPhase) sampler.Start();
            OnClientChanged();
            if (opts.Script != null) RunTestScript();
            else if (opts.AutoExit > 0) { System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = opts.AutoExit * 1000 }; t.Tick += (s, e) => { t.Stop(); Quit(); }; t.Start(); }
        }

        void OnDisplayChanged(object s, EventArgs e) { UpdatePlacement(true); }
        public void OnDpiChanged() { UpdatePlacement(true); }

        static bool GameRunning()
        {
            Process[] p = Process.GetProcessesByName("League of Legends");
            bool any = p.Length > 0;
            foreach (Process x in p) x.Dispose();
            return any;
        }

        bool ClientOpen { get { return tracker.Handle != IntPtr.Zero; } }
        bool ClientVisible { get { return ClientOpen && !tracker.Minimized; } }

        // Une lecture par seconde au maximum, et seulement si une conclusion est possible.
        async void Sample()
        {
            if (firstRun) { firstRun = false; OpenOnFirstLaunch(); }
            if (await PollLcuOnce())
            {
                // Le client donne la phase exacte : rien à estimer ni à apprendre à partir de l'image.
                frameValid = false;
                if (settings.Diagnostics) Diagnose(Evidence.Unknown);
                NotifyLcuStatus();
                return;
            }
            Evidence ev = Evidence.Unknown;
            frameValid = false;
            bool game = GameRunning();
            if (!game && ClientVisible && FrameGrabber.IsUnobstructed(tracker.Handle, tracker.Rect) && grabber.Grab(tracker.Rect, frame))
            {
                frameValid = true;
                ev = detector.Classify(frame);
            }
            StepPhase(ClientOpen, game, ev, SampleMs / 1000.0);
            if (machine.DraftTimedOut) detector.ForgetBaseline();
            if (settings.Diagnostics) Diagnose(ev);
            NotifyLcuStatus();
        }

        void Diagnose(Evidence ev)
        {
            // Chiffres uniquement : aucune image, aucun nom. Le journal repart de zéro au-delà de 2 Mo.
            string logPath = Path.Combine(dataDir, "diagnostic.log");
            try { if (File.Exists(logPath) && new FileInfo(logPath).Length > 2 * 1024 * 1024) File.Delete(logPath); } catch { }
            try { File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss") + " " + machine.Current + " preuve=" + ev + " panneaux=" + panelsShown + " image=" + frameValid + " lcu=" + LcuDiag() + " " + detector.LastMetrics + Environment.NewLine); } catch { }
        }

        // ---------- Lecture LCU : verrouillée, voir docs/LCU.md ----------
        LcuSession lcu;
        bool lcuBusy;
        string lastEnemies = "";
        string lcuInfo = "";

        bool legacyProfile;
        bool firstRun;
        string shownLcuStatus = "";

        string LcuStatus()
        {
            if (!LcuGate.Allowed(settings.LcuEnabled)) return "off";
            if (lcu != null && lcu.Connected) return "connected";
            return ClientOpen ? "failed" : "waiting";
        }

        void NotifyLcuStatus()
        {
            string now = LcuStatus();
            if (now != shownLcuStatus) { shownLcuStatus = now; RefreshUi(); }
        }

        // Phase et nombre d'adversaires uniquement : aucun identifiant, aucun nom.
        string LcuDiag()
        {
            if (lcu == null) return "off";
            if (lcu.Connected) return lcuInfo;
            // Ouverture (processus, lockfile) puis requête : la première étape en échec suffit à comprendre.
            return "absent(" + (LcuFactory.LastOpenError != "" ? LcuFactory.LastOpenError : LocalHttpsTransport.LastError) + ")";
        }

        public void TestUseLcu(Func<ILcuTransport> open) { lcu = new LcuSession(open) { RetryMs = 0, PollTimeoutMs = 400 }; lastEnemies = ""; }
        public LcuSession Lcu { get { return lcu; } }

        // Verrou fermé : la session n'est jamais créée, le lockfile n'est donc jamais ouvert.
        void EnsureLcu()
        {
            if (lcu == null && LcuGate.Allowed(settings.LcuEnabled)) lcu = new LcuSession(OpenRealLcu);
        }

        ILcuTransport OpenRealLcu()
        {
            if (!ClientOpen) return null;
            try
            {
                uint pid;
                Native.GetWindowThreadProcessId(tracker.Handle, out pid);
                Msg error;
                string folder = ClientProcess.FolderOf((int)pid, out error);
                // Journal de diagnostic interne : la raison est notée par sa clé, sans chemin ni identifiant.
                if (folder == null) { LcuFactory.LastOpenError = error.Key; return null; }
                return LcuFactory.CreateReal(settings.LcuEnabled, folder);
            }
            catch (Exception e) { LcuFactory.LastOpenError = "ouverture : " + e.GetType().Name; return null; }
        }

        // Diagnostic lancé à la demande : le résultat s'affiche et se copie, sans aucun secret, pour être envoyé tel quel.
        public async Task<string> LcuDiagnosticText()
        {
            int pid = 0;
            if (ClientOpen) { uint p; Native.GetWindowThreadProcessId(tracker.Handle, out p); pid = (int)p; }
            return await LcuDiagnostic.RunAsync(settings.LcuEnabled, pid, typeof(CompanionApp).Assembly.GetName().Version.ToString(3));
        }

        async void ShowLcuDiagnostic()
        {
            string text;
            try { text = await LcuDiagnosticText(); }
            catch (Exception e) { text = Strings.T("native.diag.interrupted", "error", e.GetType().Name); }
            bool copied = false;
            try { Clipboard.SetText(text); copied = true; } catch { }
            MessageBox.Show(text + (copied ? "\r\n" + Strings.T("native.diag.copied") : ""), Strings.T("native.diag.dialogTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Vrai si le client a répondu. Sinon, l'estimation visuelle et le mode manuel prennent le relais.
        public async Task<bool> PollLcuOnce()
        {
            EnsureLcu();
            if (lcu == null || lcuBusy) return false;
            lcuBusy = true;
            try
            {
                LcuSnapshot s = await lcu.PollAsync();
                if (!s.Available) { UpdateAssignedRole(null); return false; }
                lcuInfo = s.PhaseName + "/" + (s.EnemyChampionIds == null ? "illisible" : s.EnemyChampionIds.Count.ToString());
                Phase before = machine.Current;
                machine.Force(s.Phase == Phase.Uncertain && before == Phase.Draft ? Phase.Draft : s.Phase);
                AfterPhase(before, machine.Current);
                ApplyVisibility();
                PostEnemies(s);
                // Session illisible : on garde la dernière valeur connue. Hors draft : plus de rôle attribué.
                if (s.Phase != Phase.Draft) UpdateAssignedRole(null);
                else if (s.EnemyChampionIds != null) UpdateAssignedRole(s.AssignedRole);
                return true;
            }
            finally { lcuBusy = false; }
        }

        // Rôle attribué au joueur pendant la sélection, si le client l'indique. Null sinon : rien n'est deviné.
        string assignedRole;
        void UpdateAssignedRole(string role)
        {
            if (role == assignedRole) return;
            assignedRole = role;
            BroadcastHost();
        }
        public void TestSetAssignedRole(string role) { UpdateAssignedRole(role); }
        public string AssignedRole { get { return assignedRole; } }

        void PostEnemies(LcuSnapshot s)
        {
            if (s.Phase != Phase.Draft) { lastEnemies = ""; return; }
            // Réponse incomplète ou expirée : on garde la dernière composition connue.
            if (s.EnemyChampionIds == null) return;
            string key = string.Join(",", s.EnemyChampionIds);
            if (key == lastEnemies || pendingStart) return;
            PanelForm target = panels.Values.FirstOrDefault(f => f.Id != "full" && f.Ready.IsCompleted && f.View.CoreWebView2 != null);
            if (target == null) return;
            target.View.CoreWebView2.PostWebMessageAsJson("{\"kind\":\"lcuEnemies\",\"ids\":[" + key + "]}");
            lastEnemies = key;
        }

        public void StepPhase(bool open, bool game, Evidence ev, double seconds)
        {
            Phase before = machine.Current;
            Phase now = machine.Step(open, game, ev, seconds);
            AfterPhase(before, now);
            ApplyVisibility();
        }

        void AfterPhase(Phase before, Phase now)
        {
            if (now != before)
            {
                if (now == Phase.Draft && before != Phase.Draft) { NotifyDraftStart(); WarnOtherPanels(); }
                else if (before == Phase.Draft) { validationPending = true; SendAll("{\"kind\":\"draftEnd\"}"); }
                if (env == null && now != Phase.ClientClosed) Prewarm();
                // Le mot de passe change à chaque lancement du client : il est oublié dès sa fermeture.
                if (now == Phase.ClientClosed && lcu != null) lcu.Forget();
                if (now == Phase.ClientClosed && !opts.SimulatedPhase && !panelsShown) ReleaseWeb();
                RefreshUi();
            }
        }

        void NotifyDraftStart() { pendingStart = true; if (panelsShown) { pendingStart = false; PostStart(); } }
        bool pendingStart;

        void OnClientChanged()
        {
            if (!opts.SimulatedPhase) { Phase b = machine.Current; if ((!ClientOpen) != (b == Phase.ClientClosed)) StepPhase(ClientOpen, GameRunning(), Evidence.Unknown, 0); }
            ApplyVisibility();
            if (panelsShown) UpdatePlacement(false);
        }

        void ApplyVisibility()
        {
            bool show = visibility.ShouldShow(machine.Current, ClientVisible, settings.AutoDraft);
            if (show == panelsShown) return;
            if (show) ShowPanels(); else HidePanels();
        }

        public int ShowCount;

        // Relevé de validation : une ligne par sélection, dans un fichier local. mid_reel, roles_reels et remarques se remplissent à la main.
        // roles_predits vient en dernier (ajout 2.17) : les anciens fichiers, sans cette colonne, restent lisibles.
        const string ValidationHeader = "date;version;role;pool;adversaires;mid_affiche;confiance;hypotheses;recommande;alternative;provisoire;corrections;joue;phase_suivante;mid_reel;roles_reels;remarques;roles_predits";
        bool validationPending;
        public string ValidationPath { get { return Path.Combine(dataDir, "validation-drafts.csv"); } }
        public void TestSetDiagnostics(bool on) { settings.Diagnostics = on; RefreshUi(); }

        // Un tableur exécuterait une cellule commençant par = + - @ comme une formule.
        public static string CsvCell(string v) { return v.Length > 0 && "=+-@\t".IndexOf(v[0]) >= 0 ? "'" + v : v; }

        void WriteValidationRow(System.Collections.IEnumerable values, string predicted)
        {
            List<string> cells = new List<string> { DateTime.Now.ToString("yyyy-MM-dd HH:mm"), typeof(CompanionApp).Assembly.GetName().Version.ToString(3) };
            foreach (object v in values) cells.Add(CsvCell(Regex.Replace(Convert.ToString(v), "[;\\r\\n\"]", " ")));
            cells.Add(machine.Current.ToString());
            string line = string.Join(";", cells) + ";;;;" + CsvCell(Regex.Replace(predicted ?? "", "[;\\r\\n\"]", " ")) + "\r\n";
            // Fichier ouvert dans Excel : la ligne part dans un fichier voisin plutôt que d'être perdue.
            foreach (string path in new[] { ValidationPath, Path.Combine(dataDir, "validation-drafts-attente.csv") })
                try
                {
                    if (!File.Exists(path)) File.WriteAllText(path, ValidationHeader + "\r\n", new UTF8Encoding(true));
                    File.AppendAllText(path, line, new UTF8Encoding(false));
                    return;
                }
                catch (IOException) { }
        }

        async void ShowPanels()
        {
            ShowCount++;
            panelsShown = true;
            shownMode = "";
            RefreshUi();
            await EnsurePanels(settings.Compact ? "compact" : "two");
            if (!panelsShown) return;
            UpdatePlacement(true);
            if (pendingStart) { pendingStart = false; PostStart(); }
        }

        void PostStart()
        {
            foreach (PanelForm f in panels.Values)
                if (f.Id != "full" && f.View.CoreWebView2 != null) { f.View.CoreWebView2.PostWebMessageAsJson("{\"kind\":\"draftStart\"}"); break; }
        }

        // ---------- Mise à jour des rôles, en arrière-plan ----------
        CancellationTokenSource rolesCts;
        Msg rolesStatus;
        bool rolesReloadPending;

        // ---------- Copie de secours des profils, dans le dossier de données ----------
        // Profils et préférences de pool seulement : la page n'y met ni historique ni clé.
        public const string ProfilesFile = "draft-profiles.json";
        string profilesCopy;
        bool profilesCopyRead;
        string ProfilesCopy()
        {
            if (!profilesCopyRead) { profilesCopyRead = true; try { string path = Path.Combine(dataDir, ProfilesFile); if (File.Exists(path)) profilesCopy = File.ReadAllText(path, Encoding.UTF8); } catch (IOException) { } }
            return profilesCopy;
        }
        public bool SaveProfilesCopy(string json)
        {
            if (json == null || json.Length > 256 * 1024) return false;
            try { if (!(new JavaScriptSerializer().DeserializeObject(json) is Dictionary<string, object>)) return false; } catch { return false; }
            try
            {
                string path = Path.Combine(dataDir, ProfilesFile), temp = path + ".tmp";
                File.WriteAllText(temp, json, new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temp, path, path + ".precedent"); else File.Move(temp, path);
                profilesCopy = json; profilesCopyRead = true;
                return true;
            }
            catch (IOException) { return false; }
        }

        // ---------- Import de l'historique, seulement à la demande du joueur ----------
        CancellationTokenSource historyCts;
        Msg historyStatus;
        bool historyHasMore;

        // La clé reste dans le fetcher : ni l'importeur, ni les journaux, ni la page ne la voient.
        public async Task<HistoryImportResult> RunHistoryImport(IRiotFetcher fetcher, RiotRegion region, List<string> accounts, int limit, int delayMs)
        {
            string path = Path.Combine(dataDir, HistoryImporter.FileName);
            CancellationTokenSource cts = new CancellationTokenSource();
            historyCts = cts;
            historyStatus = new Msg("native.history.starting");
            RefreshUi();
            SynchronizationContext ui = SynchronizationContext.Current;
            HistoryImporter importer = new HistoryImporter(fetcher, region, path, accounts, HistoryImporter.OldestSupported) { MaxGamesPerRun = limit, DelayMs = delayMs };
            importer.Status = text => ui.Post(_ => { if (historyCts != null && !historyCts.IsCancellationRequested) { historyStatus = text; RefreshUi(); } }, null);
            importer.Progress = (done, total, account) =>
            {
                if (done % 5 != 0 && done != total) return;
                ui.Post(_ => { if (historyCts != null && !historyCts.IsCancellationRequested) { historyStatus = new Msg("native.history.progress", "done", done, "count", total); RefreshUi(); } }, null);
            };
            HistoryImportResult r = await Task.Run(() => importer.Run(cts.Token));
            historyCts = null;
            cts.Dispose();
            if (Quitting) return r;
            if (r.Saved)
            {
                try
                {
                    // Comme au démarrage : relu et réécrit avant d'atteindre la page.
                    if (!DataScripts.Publish(HistoryImporter.FileName, dataDir, webDir)) Program.Log(dataDir, "Fichier de données ignoré, contenu non valide : " + HistoryImporter.FileName);
                    rolesReloadPending = true;
                    ReloadRolesIfIdle();
                }
                catch (IOException) { r.Error = new Msg("native.history.notVisible"); }
            }
            historyHasMore = r.HasMore;
            // Clé refusée ou Riot indisponible : les lectures suivantes ne sont pas lancées avec la même clé.
            if (r.Error != null && !r.Saved) riotQueueStopped = true;
            Msg summary = new Msg("native.history.summary", "count", r.Added, "total", r.Total);
            historyStatus = r.Error != null ? new Msg("native.status.join", "first", r.Error, "second", summary) : new Msg(r.Cancelled ? "native.history.stopped" : "native.history.done", "summary", summary);
            // Les comptes sont des noms Riot (Nom#TAG), identiques dans toutes les langues.
            if (r.NotFound.Count > 0) historyStatus = new Msg("native.history.notFound", "status", historyStatus, "accounts", string.Join(", ", r.NotFound));
            Notify(historyStatus, r.Error == null ? ToolTipIcon.Info : ToolTipIcon.Warning);
            RefreshUi();
            return r;
        }

        // Une seule saisie de la clé pour les trois lectures (UX-5). Un bouton en cours d'exécution arrête sa lecture et la suite.
        bool riotQueueStopped;

        void ToggleHistoryImport()
        {
            if (historyCts != null)
            {
                riotQueueStopped = true;
                historyCts.Cancel();
                historyStatus = new Msg("native.history.stopping");
                RefreshUi();
                return;
            }
            OpenRiotTasks(RiotTask.History);
        }

        void ToggleRiotUpdate(bool matchups)
        {
            if (rolesCts != null)
            {
                riotQueueStopped = true;
                rolesCts.Cancel();
                rolesStatus = new Msg("native.roles.stopping");
                RefreshUi();
                return;
            }
            OpenRiotTasks(matchups ? RiotTask.Matchups : RiotTask.Roles);
        }

        // Rôle et pool envoyés par la page pour les matchups ; une ancienne installation garde le pool Mid d'origine.
        void MatchupTarget(out string role, out string[] pool)
        {
            role = settings.ProfileRole;
            pool = settings.ProfilePool;
            if (pool == null && legacyProfile) { role = "mid"; pool = MatchupStatsUpdater.DefaultPool; }
        }

        async void OpenRiotTasks(RiotTask initial)
        {
            if (historyCts != null) { rolesStatus = new Msg("native.roles.busyHistory"); RefreshUi(); return; }
            if (rolesCts != null) { historyStatus = new Msg("native.history.busyRoles"); RefreshUi(); return; }
            if ((initial & RiotTask.History) != 0 && machine.Current == Phase.Draft) { historyStatus = new Msg("native.history.busyDraft"); RefreshUi(); return; }
            string path = Path.Combine(dataDir, HistoryImporter.FileName);
            string role; string[] pool;
            MatchupTarget(out role, out pool);
            RiotTask tasks;
            string key;
            RiotRegion region;
            int limit;
            List<string> accounts;
            using (RiotKeyDialog dialog = new RiotKeyDialog(AppIcon, OpenExternal, HistoryImporter.SavedAccounts(path), initial, pool == null ? null : RoleLabel(role), pool == null ? 0 : pool.Length))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                tasks = dialog.Tasks;
                key = dialog.Key;
                region = dialog.SelectedRegion;
                limit = dialog.GamesToRead;
                accounts = dialog.Accounts;
                if ((tasks & RiotTask.History) != 0 && !dialog.Consent) tasks &= ~RiotTask.History;
            }
            // Un seul fetcher : la clé n'existe qu'ici, en mémoire, le temps des lectures choisies.
            await RunRiotTasks(new RiotWebFetcher(key), tasks, region, accounts, limit, role, pool, 1300);
        }

        static string RoleLabel(string role)
        {
            switch (role) { case "top": return "Top"; case "jungle": return "Jungle"; case "mid": return "Mid"; case "adc": return "ADC"; case "support": return "Support"; default: return role ?? ""; }
        }

        // Lectures l'une après l'autre, avec la même clé. Testable avec un fetcher simulé.
        public async Task RunRiotTasks(IRiotFetcher fetcher, RiotTask tasks, RiotRegion region, List<string> accounts, int limit, string role, string[] pool, int delayMs)
        {
            riotQueueStopped = false;
            if ((tasks & RiotTask.History) != 0 && !Quitting) await RunHistoryImport(fetcher, region, accounts, limit, delayMs);
            if ((tasks & RiotTask.Roles) != 0 && !riotQueueStopped && !Quitting) await RunRiotUpdate(false, fetcher, region, role, pool);
            if ((tasks & RiotTask.Matchups) != 0 && pool != null && !riotQueueStopped && !Quitting) await RunRiotUpdate(true, fetcher, region, role, pool);
        }

        public async Task<RoleUpdateResult> RunRiotUpdate(bool matchups, IRiotFetcher fetcher, RiotRegion region, string role, string[] pool)
        {
            string fileName = matchups ? "draft-matchups-live.js" : "draft-role-rates-live.js";
            CancellationTokenSource cts = new CancellationTokenSource();
            rolesCts = cts;
            rolesStatus = new Msg(matchups ? "native.roles.startingMatchups" : "native.roles.startingRoles");
            RefreshUi();
            SynchronizationContext ui = SynchronizationContext.Current;
            RiotCrawler updater = matchups
                ? (RiotCrawler)new MatchupStatsUpdater(fetcher, region, Path.Combine(dataDir, fileName), pool, role) { StatePath = Path.Combine(dataDir, "draft-matchups-state-" + role + ".json") }
                : new RoleRatesUpdater(fetcher, region, Path.Combine(dataDir, fileName)) { StatePath = Path.Combine(dataDir, "draft-role-rates-state.json") };
            int shown = 0;
            updater.Progress = (done, total) =>
            {
                if (done - shown < 10 && done < total) return;
                shown = done;
                ui.Post(_ => { if (rolesCts == null || rolesCts.IsCancellationRequested) return; rolesStatus = new Msg(matchups ? "native.roles.progressMatchups" : "native.roles.progressRoles", "done", done, "count", total); RefreshUi(); }, null);
            };
            RoleUpdateResult r = await Task.Run(() => updater.Run(cts.Token));
            rolesCts = null;
            cts.Dispose();
            if (Quitting) return r;
            if (r.Saved)
            {
                // Les noms de champions viennent de l'API Riot : le fichier est relu et réécrit avant d'atteindre la page.
                if (!DataScripts.Publish(fileName, dataDir, webDir)) Program.Log(dataDir, "Fichier de données ignoré, contenu non valide : " + fileName);
                rolesReloadPending = true;
                ReloadRolesIfIdle();
            }
            rolesStatus = r.UpToDate ? new Msg(matchups ? "native.roles.upToDateMatchups" : "native.roles.upToDateRoles")
                : r.Saved ? new Msg(matchups ? "native.roles.savedMatchups" : "native.roles.savedRoles", "games", r.Games, "count", r.Champions)
                : r.Error != null ? r.Error
                : new Msg("native.roles.stoppedFew", "count", r.Games);
            if (r.Saved && r.Error != null) rolesStatus = new Msg("native.status.join", "first", rolesStatus, "second", r.Error);
            if (r.Error != null && !r.Saved) riotQueueStopped = true;
            Notify(rolesStatus, r.Saved || r.UpToDate ? ToolTipIcon.Info : ToolTipIcon.Warning);
            RefreshUi();
            return r;
        }

        // Recharger les panneaux efface leur état : jamais pendant une draft.
        void ReloadRolesIfIdle()
        {
            ReloadLanguageIfIdle();
            if (!rolesReloadPending || panelsShown || machine.Current == Phase.Draft) return;
            rolesReloadPending = false;
            foreach (PanelForm f in panels.Values) if (f.View.CoreWebView2 != null) f.View.CoreWebView2.Reload();
        }

        void HidePanels()
        {
            panelsShown = false;
            shownMode = "";
            foreach (PanelForm f in panels.Values.Where(p => p.Id != "full")) if (f.Visible) f.Hide();
            ReloadRolesIfIdle();
            RefreshUi();
        }

        // ---------- WebView2, créé seulement quand le client est là ----------

        // Simule une fermeture puis un retour du client, comme en usage réel.
        public async Task<bool> TestReleaseAndReopen()
        {
            ReleaseWeb();
            await EnsurePanels("two");
            return env != null && PanelFor("left") != null && PanelFor("right") != null;
        }

        Task EnsureEnv()
        {
            if (envTask == null) envTask = CreateEnv();
            return envTask;
        }

        async Task CreateEnv()
        {
            try
            {
                // WebView2 n'accepte cet appel qu'une fois par processus, or l'environnement est recréé à chaque retour du client.
                RuntimeHelp.EnsureLoader(webDir);
                env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(dataDir, "webview"), new CoreWebView2EnvironmentOptions());
            }
            catch (Exception ex)
            {
                Program.Log(dataDir, ex.ToString());
                if (!opts.Silent)
                {
                    if (ex is WebView2RuntimeNotFoundException) RuntimeHelp.Explain();
                    else MessageBox.Show(Strings.T("native.dialog.startFailed", "path", Path.Combine(dataDir, "draftmid.log")), "DRAFT", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Quit();
            }
        }

        async void Prewarm()
        {
            try { await EnsurePanels(settings.Compact ? "compact" : "two"); }
            catch (Exception e) { Program.Log(dataDir, e.ToString()); }
        }

        // Sans client ouvert, les processus WebView2 sont libérés.
        void ReleaseWeb()
        {
            foreach (PanelForm f in panels.Values.ToList()) { try { f.DisposeForRelease(); } catch { } }
            panels.Clear();
            env = null;
            envTask = null;
        }

        async Task EnsurePanels(string mode)
        {
            await EnsureEnv();
            if (env == null) return;
            if (mode == "two") { await Ensure("left", "panel=left"); await Ensure("right", "panel=right"); }
            else await Ensure("compact", "panel=compact");
        }

        async Task<PanelForm> Ensure(string id, string query)
        {
            PanelForm f;
            if (panels.TryGetValue(id, out f)) { await f.Ready; return f; }
            f = new PanelForm(id, this);
            panels[id] = f;
            TaskCompletionSource<bool> done = new TaskCompletionSource<bool>();
            f.Ready = done.Task;
            IntPtr force = f.Handle; force = f.View.Handle;
            await f.View.EnsureCoreWebView2Async(env);
            CoreWebView2 w = f.View.CoreWebView2;
            w.SetVirtualHostNameToFolderMapping("draftmid.local", webDir, CoreWebView2HostResourceAccessKind.Allow);
            w.Settings.IsStatusBarEnabled = false;
            w.Settings.AreDefaultContextMenusEnabled = false;
            w.Settings.AreDevToolsEnabled = opts.Script != null;
            w.Settings.IsZoomControlEnabled = false;
            w.Settings.IsNonClientRegionSupportEnabled = true;
            // Seule la page de DRAFT peut envoyer des commandes au code natif.
            w.WebMessageReceived += (s, e) => { if (IsAppPage(e.Source)) OnMessage(f, e.WebMessageAsJson); };
            // Les liens externes s'ouvrent dans le navigateur par défaut, jamais dans un panneau, et seulement vers des sites connus.
            w.NewWindowRequested += (s, e) => { e.Handled = true; OpenFromPage(e.Uri); };
            w.NavigationStarting += (s, e) => { if (!IsAppPage(e.Uri)) { e.Cancel = true; OpenFromPage(e.Uri); } };
            w.FrameNavigationStarting += (s, e) => { if (!IsAppPage(e.Uri)) e.Cancel = true; };
            TaskCompletionSource<bool> nav = new TaskCompletionSource<bool>();
            w.NavigationCompleted += (s, e) => nav.TrySetResult(true);
            w.Navigate(PageUrl(query));
            await nav.Task;
            done.TrySetResult(true);
            return f;
        }

        public const string AppOrigin = "https://draftmid.local/";
        // Adresse d'une page de DRAFT, dans la langue du programme (I18N-3 : &lang=fr|en, lu par draft-i18n.js).
        public static string PageUrl(string query) { return AppOrigin + "index.html?" + query + "&lang=" + Strings.Lang; }

        // ---------- Langue (I18N-3) ----------
        // Réglages > Général > Langue : "auto" (langue de Windows), "fr" ou "en", enregistré dans settings.json (Language).
        // La fenêtre principale change tout de suite ; les panneaux seulement hors draft, car les recharger efface leur état.
        bool langReloadPending;
        public int LanguageReloads;
        public bool SetLanguage(string value)
        {
            // QA 3.0 : commande de la page validée. Une valeur hors liste (ni langue livrée, ni « auto ») est refusée : rien ne change.
            if (value != "auto" && (value == null || Array.IndexOf(Strings.Supported, value) < 0)) return false;
            settings.Language = Strings.NormalizeSetting(value);
            Persist();
            string next = Strings.Resolve(settings.Language, System.Globalization.CultureInfo.CurrentUICulture);
            if (next != Strings.Lang)
            {
                Strings.Use(next);
                ApplyMenuTexts();
                PanelForm full = PanelFor("full");
                if (full != null && full.View.CoreWebView2 != null) { LanguageReloads++; full.View.CoreWebView2.Navigate(PageUrl("panel=full&tab=settings&section=general")); }
                langReloadPending = true;
                ReloadLanguageIfIdle();
            }
            RefreshUi();
            return true;
        }

        public bool LanguagePending { get { return langReloadPending; } }
        public string MenuOpenText { get { return miOpen.Text; } }
        // Essais : commande reçue comme si elle venait d'une page de DRAFT, et retour au réglage de départ.
        public void TestMessage(string json) { PanelForm f = panels.Values.FirstOrDefault(p => !p.IsDisposed); if (f != null) OnMessage(f, json); }
        public void TestRestoreLanguageSetting(string value) { settings.Language = value; Persist(); }
        public void TestOpenFull(string tab) { OpenFull(tab); }

        void ReloadLanguageIfIdle()
        {
            if (!langReloadPending || panelsShown || machine.Current == Phase.Draft) return;
            langReloadPending = false;
            foreach (PanelForm f in panels.Values.ToList()) if (f.Id != "full" && f.View.CoreWebView2 != null) { LanguageReloads++; f.View.CoreWebView2.Navigate(PageUrl("panel=" + f.Id)); }
        }

        // Libellés fixes du menu de l'icône ; ceux qui changent avec l'état (phase, panneaux, mise à jour) sont dans RefreshUi.
        void ApplyMenuTexts()
        {
            miOpen.Text = MenuText.Open; miSimulate.Text = MenuText.Simulate; miDetection.Text = MenuText.Detection;
            miTeachDraft.Text = MenuText.TeachDraft; miTeachNotDraft.Text = MenuText.TeachNotDraft; miLcuDiagnostic.Text = MenuText.LcuDiagnostic;
            miSettings.Text = MenuText.Settings; miQuit.Text = MenuText.Quit;
        }
        public static bool IsAppPage(string uri) { return uri != null && uri.StartsWith(AppOrigin, StringComparison.Ordinal); }

        // Sites que la page peut ouvrir dans le navigateur.
        static readonly string[] PageLinkHosts = { "lolalytics.com", "developer.riotgames.com", "www.draft-app.fr", "draft-app.fr" };

        public static bool AllowedPageLink(string uri)
        {
            Uri u;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out u) || u.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(u.UserInfo)) return false;
            string host = u.Host.ToLowerInvariant();
            return PageLinkHosts.Any(h => host == h || host.EndsWith("." + h));
        }

        void OpenFromPage(string uri) { if (AllowedPageLink(uri)) OpenExternal(uri); }

        void OpenExternal(string uri)
        {
            if (!uri.StartsWith("https://")) return;
            try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); } catch { }
        }

        // ---------- Placement ----------
        public void UpdatePlacement(bool force)
        {
            if (!panelsShown) return;
            if (!ClientVisible || !settings.Follow) { PlaceManual(); return; }
            Native.RECT client = tracker.Rect;
            // Le panneau étroit reste calé sur l'écran : il ne suit pas le client.
            if (!force && !narrowPlacement && TranslatePanels(client)) return;
            Placement p = Layout.Compute(client, ClientWindow.Monitors(), settings.Compact);
            placementNote = p.Note;
            if (p.Mode == "manual")
            {
                string wanted = settings.Compact ? "compact" : "two";
                bool saved = wanted == "two" ? settings.Rects.ContainsKey("left") && settings.Rects.ContainsKey("right") : settings.Rects.ContainsKey("compact");
                if (saved) { PlaceManual(); RefreshUi(); return; }
                p = Layout.Narrowest(client, ClientWindow.Monitors());
                placementNote = p.Note;
                narrowPlacement = true;
                pendingMode = "";
                Place(p);
                RefreshUi();
                return;
            }
            narrowPlacement = false;
            // Un changement de disposition attend que le client ne bouge plus, pour ne pas sauter pendant un déplacement.
            if (!force && shownMode != "" && p.Mode != shownMode)
            {
                long now = Environment.TickCount;
                if (pendingMode != p.Mode) { pendingMode = p.Mode; pendingSince = now; }
                if (now - pendingSince < ModeSettleMs) { System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = ModeSettleMs }; t.Tick += (s, e) => { t.Stop(); t.Dispose(); UpdatePlacement(false); }; t.Start(); return; }
            }
            pendingMode = "";
            Place(p);
        }

        bool narrowPlacement;
        Native.RECT placedClient;
        readonly Dictionary<string, Native.RECT> placedRects = new Dictionary<string, Native.RECT>();
        System.Windows.Forms.Timer settleTimer;

        // Pendant un déplacement, les panneaux glissent sans changer de taille : un redimensionnement forcerait
        // WebView2 à tout redessiner à chaque pas, d'où un retard visible. La taille est recalculée à l'arrêt.
        bool TranslatePanels(Native.RECT client)
        {
            if (shownMode == "" || !PanelsReady(shownMode) || client.W != placedClient.W || client.H != placedClient.H) return false;
            int dx = client.Left - placedClient.Left, dy = client.Top - placedClient.Top;
            // Mise à jour sans mouvement pendant un déplacement : surtout ne pas recalculer la taille maintenant.
            if (dx == 0 && dy == 0) return settleTimer != null && settleTimer.Enabled;
            string[] ids = shownMode == "two" ? new[] { "left", "right" } : new[] { "compact" };
            if (ids.Any(id => !placedRects.ContainsKey(id))) return false;
            PanelForm[] forms = ids.Select(id => panels[id]).ToArray();
            Native.RECT[] rects = ids.Select(id => { Native.RECT r = placedRects[id]; return new Native.RECT(r.Left + dx, r.Top + dy, r.Right + dx, r.Bottom + dy); }).ToArray();
            MoveTogether(forms, rects, true);
            for (int i = 0; i < ids.Length; i++) placedRects[ids[i]] = rects[i];
            placedClient = client;
            MoveCount++;
            LastFollowLagMs = (Stopwatch.GetTimestamp() - tracker.LastEventTicks) * 1000.0 / Stopwatch.Frequency;
            if (settleTimer == null)
            {
                settleTimer = new System.Windows.Forms.Timer { Interval = MoveSettleMs };
                settleTimer.Tick += (s, e) => { settleTimer.Stop(); UpdatePlacement(true); };
            }
            settleTimer.Stop();
            settleTimer.Start();
            return true;
        }

        void RememberPlacement(string[] ids, Native.RECT[] rects)
        {
            placedClient = tracker.Rect;
            placedRects.Clear();
            for (int i = 0; i < ids.Length; i++) placedRects[ids[i]] = rects[i];
        }

        bool PanelsReady(string mode)
        {
            foreach (string id in mode == "two" ? new[] { "left", "right" } : new[] { "compact" })
            {
                PanelForm f;
                if (!panels.TryGetValue(id, out f) || !f.Ready.IsCompleted) return false;
            }
            return true;
        }

        async void Place(Placement p)
        {
            // Pendant un déplacement, les panneaux sont déjà prêts : aucun passage asynchrone qui retarderait le suivi.
            if (!PanelsReady(p.Mode)) await EnsurePanels(p.Mode);
            if (!panelsShown) return;
            shownMode = p.Mode;
            if (p.Mode == "two")
            {
                PanelForm l = panels["left"], r = panels["right"];
                HideOne("compact");
                MoveTogether(new[] { l, r }, new[] { p.Left, p.Right });
                RememberPlacement(new[] { "left", "right" }, new[] { p.Left, p.Right });
            }
            else
            {
                HideOne("left"); HideOne("right");
                MoveTogether(new[] { panels["compact"] }, new[] { p.Single });
                RememberPlacement(new[] { "compact" }, new[] { p.Single });
            }
            MoveCount++;
        }

        // Retour sur le client depuis une autre application : sans cela, les panneaux restent derrière elle.
        // Jamais au-dessus des autres applications, et sans prendre le focus.
        public void RaisePanels()
        {
            if (!panelsShown || settings.Topmost) return;
            foreach (PanelForm f in panels.Values)
                if (f.Id != "full" && f.Visible) Native.SetWindowPos(f.Handle, IntPtr.Zero, 0, 0, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOMOVE | Native.SWP_NOACTIVATE);
        }

        void HideOne(string id) { PanelForm f; if (panels.TryGetValue(id, out f) && f.Visible) f.Hide(); }

        void MoveTogether(PanelForm[] forms, Native.RECT[] rects, bool keepSize = false)
        {
            IntPtr after = settings.Topmost ? new IntPtr(-1) : new IntPtr(-2);
            uint flags = Native.SWP_NOACTIVATE | (settings.Topmost ? 0 : Native.SWP_NOZORDER) | (keepSize ? Native.SWP_NOSIZE : 0);
            for (int i = 0; i < forms.Length; i++) if (!forms[i].Visible) { forms[i].Show(); }
            IntPtr dwp = NativeHook.BeginDeferWindowPos(forms.Length);
            for (int i = 0; i < forms.Length; i++) dwp = NativeHook.DeferWindowPos(dwp, forms[i].Handle, after, rects[i].Left, rects[i].Top, rects[i].W, rects[i].H, flags);
            NativeHook.EndDeferWindowPos(dwp);
        }

        void PlaceManual()
        {
            string mode = settings.Compact ? "compact" : "two";
            Place2(mode);
        }

        async void Place2(string mode)
        {
            await EnsurePanels(mode);
            if (!panelsShown) return;
            shownMode = mode;
            List<Monitor> mons = ClientWindow.Monitors();
            Monitor main = mons.FirstOrDefault(m => m.Primary) ?? mons[0];
            int w = (int)(340 * main.Scale), h = Math.Min(main.Work.H, (int)(860 * main.Scale));
            foreach (string id in mode == "two" ? new[] { "left", "right" } : new[] { "compact" })
            {
                PanelForm f = panels[id];
                Native.RECT r = id == "left" ? new Native.RECT(main.Work.Left, main.Work.Top, main.Work.Left + w, main.Work.Top + h)
                    : new Native.RECT(main.Work.Right - w, main.Work.Top, main.Work.Right, main.Work.Top + h);
                SavedRect sv;
                if (settings.Rects.TryGetValue(id, out sv))
                {
                    Native.RECT saved = new Native.RECT(sv.X, sv.Y, sv.X + sv.W, sv.Y + sv.H);
                    // Une position sur un écran débranché, ou presque hors de tout écran, est ignorée (QA-4).
                    if (Layout.KeepSaved(saved, mons)) r = saved;
                }
                if (!f.Visible) f.Show();
                f.PlaceNoActivate(r, settings.Topmost);
            }
            if (mode == "two") HideOne("compact"); else { HideOne("left"); HideOne("right"); }
        }

        public void PanelMovedByUser(PanelForm f)
        {
            if (f.Id == "full") return;
            Rectangle b = f.Bounds;
            settings.Rects[f.Id] = new SavedRect { X = b.X, Y = b.Y, W = b.Width, H = b.Height };
            settings.Follow = false;
            placementNote = new Msg("native.placement.manualKept");
            Persist();
            RefreshUi();
        }

        void Persist() { try { settings.Save(settingsPath); } catch (Exception e) { Program.Log(dataDir, e.ToString()); } }

        // ---------- Actions ----------
        public void ToggleManual()
        {
            bool target = !panelsShown;
            // Afficher les panneaux apprend cet écran. Les masquer n'apprend rien : on peut vouloir les cacher en pleine draft.
            // Pour une fausse détection, le menu propose « Ce n'est pas une draft ».
            if (target && frameValid && machine.Current != Phase.InGame && machine.Current != Phase.Draft && detector.Teach(frame, true)) Persist();
            visibility.Manual = target;
            ApplyVisibility();
            RefreshUi();
        }

        void Teach(bool isDraft)
        {
            if (!frameValid) { tray.ShowBalloonTip(3000, "DRAFT", Strings.T("native.balloon.teachNeedClient"), ToolTipIcon.Info); return; }
            if (!detector.Teach(frame, isDraft)) { tray.ShowBalloonTip(3000, "DRAFT", Strings.T("native.balloon.teachNoPortrait"), ToolTipIcon.Info); return; }
            Persist();
        }

        public void SetAutoDraft(bool v) { settings.AutoDraft = v; visibility.Manual = null; Persist(); ApplyVisibility(); RefreshUi(); }

        public async void SetCompact(bool v)
        {
            settings.Compact = v;
            Persist();
            shownMode = "";
            if (panelsShown) { await EnsurePanels(v ? "compact" : "two"); UpdatePlacement(true); }
            RefreshUi();
        }

        public void SetFollow(bool v)
        {
            settings.Follow = v;
            if (v) settings.Rects.Clear();
            Persist();
            UpdatePlacement(true);
            RefreshUi();
        }

        void RefreshUi()
        {
            string phase = PhaseText.Of(machine.Current).ToString();
            miPhase.Text = phase;
            miUpdate.Enabled = updateCts == null;
            miUpdate.Text = updateCts != null ? Strings.T("native.menu.updating") : MenuText.CheckUpdate;

            bool withHotkey = hotkey != null && hotkey.Registered;
            miShow.Text = Strings.T(panelsShown ? (withHotkey ? "native.menu.hidePanelsHotkey" : "native.menu.hidePanels") : (withHotkey ? "native.menu.showPanelsHotkey" : "native.menu.showPanels"));

            string tip = Strings.T("native.tray.tip", "phase", phase);
            tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
            BroadcastHost();
        }

        string HostJson()
        {
            Dictionary<string, object> state = HostState();
            // Préversion seulement : l'édition stable envoie exactement l'état d'avant.
            if (Edition.Prerelease)
            {
                state["prerelease"] = true;
                state["hotkeyLeft"] = hotkeyLeft ?? "";
                state["hotkeyForced"] = beta != null && beta.Hotkey;
                state["otherDraft"] = otherDraft;
                state["betaCopy"] = beta == null ? null : new Dictionary<string, object> { { "copiedAt", beta.CopiedAt }, { "sourceFound", beta.SourceFound }, { "files", beta.Files }, { "skipped", beta.Skipped } };
            }
            return new JavaScriptSerializer().Serialize(state);
        }

        Dictionary<string, object> HostState()
        {
            return new Dictionary<string, object> {
                { "kind", "host" }, { "autostart", Autostart.IsOn() }, { "follow", settings.Follow }, { "autoDraft", settings.AutoDraft },
                { "topmost", settings.Topmost }, { "compact", settings.Compact }, { "placement", Msg.JsonOrEmpty(placementNote) }, { "clientOpen", ClientOpen }, { "phase", PhaseText.Of(machine.Current).Json() }, { "language", settings.Language ?? "auto" }, { "lang", Strings.Lang }, { "windowsLang", Strings.Detect(System.Globalization.CultureInfo.CurrentUICulture) },
                { "hotkey", hotkey != null && hotkey.Registered }, { "hotkeyError", hotkey != null ? hotkey.Error : 0 },{ "diagnostics", settings.Diagnostics }, { "lcuEnabled", settings.LcuEnabled }, { "lcuConfirmed", LcuGate.ReadingEnabled }, { "lcuStatus", LcuStatus() }, { "legacyProfile", legacyProfile }, { "rolesRunning", rolesCts != null }, { "rolesStatus", Msg.JsonOrEmpty(rolesStatus) }, { "assignedRole", assignedRole }, { "savedProfiles", ProfilesCopy() }, { "historyRunning", historyCts != null }, { "historyStatus", Msg.JsonOrEmpty(historyStatus) }, { "historyHasMore", historyHasMore }, { "updateRunning", updateCts != null }, { "updateStatus", Msg.JsonOrEmpty(updateStatus) }, { "updateChecked", updateChecked }, { "panelsShown", panelsShown }, { "demoRunning", demoRunning } };
        }

        void BroadcastHost() { SendAll(HostJson()); }

        void SendAll(string json)
        {
            foreach (PanelForm f in panels.Values) if (f.View.CoreWebView2 != null) f.View.CoreWebView2.PostWebMessageAsJson(json);
        }

        class SimRiot : IRiotFetcher
        {
            public Func<string, RiotReply> Answer;
            public Task<RiotReply> GetAsync(string url) { return Task.FromResult(Answer(url)); }
        }

        // Le vrai parcours après la fenêtre de saisie : import simulé, copie, rechargement, lecture par la page, absence de trace.
        public async Task<string> TestHistoryImport()
        {
            const string fakeKey = "RGAPI-fausse-cle-de-test";
            string match = "{\"metadata\":{\"matchId\":\"%ID%\"},\"info\":{\"queueId\":420,\"gameCreation\":1790000000000,\"gameDuration\":1800,\"gameVersion\":\"16.22.1\",\"participants\":[{\"puuid\":\"secret-moi\",\"riotIdGameName\":\"NomPrive\",\"teamId\":100,\"championName\":\"Fizz\",\"teamPosition\":\"MIDDLE\",\"win\":true,\"kills\":3,\"deaths\":2,\"assists\":5},{\"puuid\":\"secret-autre\",\"riotIdGameName\":\"NomAdverse\",\"teamId\":200,\"championName\":\"Ahri\",\"teamPosition\":\"MIDDLE\",\"win\":false,\"kills\":1,\"deaths\":1,\"assists\":1}]}}";
            SimRiot sim = new SimRiot { Answer = url =>
            {
                if (url.Contains("/by-riot-id/")) return new RiotReply { Status = 200, Body = "{\"puuid\":\"secret-moi\"}" };
                if (url.Contains("/ids?")) return new RiotReply { Status = 200, Body = url.Contains("start=0&") ? "[\"E1\",\"E2\",\"E3\"]" : "[]" };
                return new RiotReply { Status = 200, Body = match.Replace("%ID%", url.Substring(url.LastIndexOf('/') + 1)) };
            } };
            string statsPath = Path.Combine(dataDir, "draft-stats.js");
            bool hadStats = File.Exists(statsPath);
            if (!hadStats) File.WriteAllText(statsPath, "window.DRAFT_STATS = {\"rows\":[]};");
            string statsBefore = File.ReadAllText(statsPath);
            HistoryImportResult first = await RunHistoryImport(sim, RiotRegion.All[0], new List<string> { "Test#EUW" }, 2, 0);
            if (first.Added != 2 || !first.HasMore) return "(premier import : " + first.Added + ")";
            if (!historyHasMore || historyStatus == null || historyStatus.Key != "native.history.done" || !(historyStatus.Vars["summary"] is Msg) || Convert.ToInt32(((Msg)historyStatus.Vars["summary"]).Vars["count"]) != 2) return "(état : " + Msg.TextOrEmpty(historyStatus) + ")";
            HistoryImportResult second = await RunHistoryImport(sim, RiotRegion.All[0], new List<string> { "Test#EUW" }, 2, 0);
            if (second.Added != 1 || second.Total != 3 || second.HasMore) return "(suite : " + second.Added + "/" + second.Total + ")";
            if (!File.Exists(Path.Combine(webDir, HistoryImporter.FileName))) return "(fichier absent de la page)";
            if (File.ReadAllText(statsPath) != statsBefore) return "(draft-stats.js modifié)";
            if (!hadStats) File.Delete(statsPath);

            // La vraie fenêtre, ouverte par le vrai bouton des réglages. Une fausse clé est saisie pour la capture, puis on annule : aucun appel.
            string windowProblem = await TestHistoryWindow(fakeKey);
            if (windowProblem != "") return windowProblem;
            // UX-5 : la même fenêtre de clé s'ouvre depuis le bouton des rôles, avec cette lecture cochée ; annuler ne lance rien.
            string rolesWindow = await TestRiotWindowFor("update-roles", RiotTask.Roles);
            if (rolesWindow != "") return rolesWindow;
            // Une seule clé pour plusieurs lectures : si Riot la refuse, la lecture suivante n'est pas lancée et rien n'est écrit.
            int calls = 0;
            SimRiot refused = new SimRiot { Answer = url => { calls++; return new RiotReply { Status = 403 }; } };
            string matchupsPath = Path.Combine(dataDir, "draft-matchups-live.js"), ratesPath = Path.Combine(dataDir, "draft-role-rates-live.js");
            bool hadMatchups = File.Exists(matchupsPath), hadRates = File.Exists(ratesPath);
            await RunRiotTasks(refused, RiotTask.Roles, RiotRegion.All[0], null, 0, "mid", new[] { "Fizz" }, 0);
            int rolesOnly = calls;
            calls = 0;
            await RunRiotTasks(refused, RiotTask.Roles | RiotTask.Matchups, RiotRegion.All[0], null, 0, "mid", new[] { "Fizz" }, 0);
            if (rolesOnly == 0 || calls != rolesOnly || rolesStatus == null || rolesStatus.Key != "native.riot.keyRefused") return "(clé refusée : la lecture suivante est lancée quand même, " + calls + " appels contre " + rolesOnly + " ; " + Msg.TextOrEmpty(rolesStatus) + ")";
            if (File.Exists(matchupsPath) != hadMatchups || File.Exists(ratesPath) != hadRates) return "(clé refusée : un fichier de calcul a été créé)";
            foreach (string file in Directory.GetFiles(dataDir, "*", SearchOption.TopDirectoryOnly))
            {
                string text;
                try { using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (StreamReader r = new StreamReader(fs)) text = r.ReadToEnd(); }
                catch (IOException) { continue; }
                if (text.Contains(fakeKey) || text.Contains("secret-moi") || text.Contains("NomPrive")) return "(trace dans " + Path.GetFileName(file) + ")";
            }
            return "";
        }

        // Vrai dans toutes les pages chargées (fenêtre principale et panneaux), au plus 5 secondes.
        async Task<bool> AllPages(string code)
        {
            for (int i = 0; i < 100; i++)
            {
                bool all = true;
                foreach (PanelForm p in panels.Values.ToList())
                {
                    if (p.IsDisposed || !p.Ready.IsCompleted || p.View.CoreWebView2 == null) continue;
                    if (await p.View.CoreWebView2.ExecuteScriptAsync("typeof PREFS !== 'undefined' && !!(" + code + ")") != "true") { all = false; break; }
                }
                if (all) return true;
                await Task.Delay(50);
            }
            return false;
        }

        public async Task<string> TestProfilesAndRole()
        {
            OpenFull("");
            PanelForm f = null;
            for (int i = 0; i < 300 && !(panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted && f.View.CoreWebView2 != null); i++) await Task.Delay(50);
            if (f == null || f.View.CoreWebView2 == null) return "(fenêtre absente)";
            Func<string, Task<string>> run = async code => await f.View.CoreWebView2.ExecuteScriptAsync(code);
            Func<string, Task<bool>> wait = async code => { for (int i = 0; i < 100; i++) { if (await run("!!(" + code + ")") == "true") return true; await Task.Delay(50); } return false; };
            if (!await wait("typeof PROFILES !== 'undefined' && hostState.available")) return "(page non prête)";
            await run("saveMyProfile('mid', ['Fizz','Xerath']); saveMyProfile('jungle', ['Viego','Lee Sin']); switchRole('mid','manual'); 1");
            if (!await wait("myRole()==='mid' && PICKS.join()==='Fizz,Xerath'")) return "(changement de rôle)";
            await Task.Delay(400);
            string copy = ProfilesCopy() ?? "";
            if (!copy.Contains("Viego") || !copy.Contains("Xerath")) return "(copie de secours absente)";
            await run("location.reload(); 1");
            await Task.Delay(600);
            if (!await wait("typeof PROFILES !== 'undefined' && myRole()==='mid' && PICKS.join()==='Fizz,Xerath' && PROFILES.roles.jungle.pool.join()==='Viego,Lee Sin'")) return "(profils perdus au rechargement)";
            // Stockage du navigateur effacé : la copie du dossier de données les rend.
            await run("localStorage.removeItem('draftProfiles'); localStorage.removeItem('draftProfile'); location.reload(); 1");
            await Task.Delay(600);
            if (!await wait("typeof PROFILES !== 'undefined' && hostState.available && PROFILES.roles.jungle.pool.join()==='Viego,Lee Sin'")) return "(copie de secours non relue)";
            await run("savePrefs({autoRole:'suggest'}); switchRole('mid','manual'); resetRoleSession(); publish(null); 1");
            TestSetAssignedRole("jungle");
            if (!await wait("state.assignedRole==='jungle' && myRole()==='mid' && document.querySelector('[data-test=\"role-hint\"]')")) return "(proposition absente)";
            await run("savePrefs({autoRole:'auto'}); 1");
            // Toutes les pages (fenêtre et panneaux, même masqués) doivent connaître la préférence et l'absence de rôle attribué
            // avant le nouveau rôle : sinon une page en retard peut diffuser l'ancien état (échec intermittent, 1 sur 8).
            if (!await AllPages("PREFS.autoRole === 'auto'")) return "(préférence autoRole non transmise à toutes les pages)";
            TestSetAssignedRole(null);
            if (!await AllPages("!state.assignedRole")) return "(rôle attribué non effacé dans toutes les pages)";
            TestSetAssignedRole("jungle");
            if (!await wait("myRole()==='jungle'")) return "(changement automatique absent)";
            await run("switchRole('mid','manual'); 1");
            TestSetAssignedRole("support"); TestSetAssignedRole("jungle");
            if (!await wait("myRole()==='mid'")) return "(la correction manuelle n'est pas respectée)";
            TestSetAssignedRole(null);
            await run("savePrefs({autoRole:'suggest'}); resetRoleSession(); 1");
            // Après un rechargement, les réglages doivent toujours s'ouvrir par le bouton du panneau.
            OpenFull("settings");
            if (!await wait("document.querySelector('[data-test=\"settings\"]')")) return "(réglages impossibles à ouvrir après rechargement : " + await run("location.search + ' ' + document.readyState") + ")";
            await run("(document.querySelector('[data-test=\"settings-close\"]') || document.querySelector('[data-test=\"nav-sim\"]')).click(); 1");
            return "";
        }

        // Protections de la page, vérifiées dans la vraie WebView2 : CSP réellement appliquée, eval interdit,
        // réseau et cadres limités, navigation et fenêtres vers un site inconnu refusées. Aucun accès réseau réel (.invalid).
        public string DetailOpenReport = "non mesuré", SimulationReport = "non mesuré";

        // Perte de draft (audit UX, P0) : ouvrir Analyse détaillée ou Comparer depuis les panneaux ne doit jamais vider ni écraser
        // la vraie draft. 50 ouvertures successives, chacune recharge la grande fenêtre. Puis le simulateur, qui reste local.
        public async Task<string> TestDraftSurvivesDetail(int openings)
        {
            string problem = "";
            bool? manualBefore = visibility.Manual;
            visibility.Manual = true;
            ApplyVisibility();
            Func<PanelForm, string, Task<string>> run = async (p, code) => await p.View.CoreWebView2.ExecuteScriptAsync(code);
            Func<PanelForm, string, int, Task<bool>> waitIn = async (p, code, ms) =>
            {
                for (int i = 0; i < ms / 50; i++)
                {
                    if (p != null && p.Ready.IsCompleted && p.View.CoreWebView2 != null && await run(p, "!!(" + code + ")") == "true") return true;
                    await Task.Delay(50);
                }
                return false;
            };
            Func<List<PanelForm>> shownPanels = () => panels.Values.Where(p => p.Id != "full" && p.Visible).ToList();
            for (int i = 0; i < 200 && (!panelsShown || shownPanels().Count == 0 || !shownPanels().All(p => p.Ready.IsCompleted)); i++) await Task.Delay(50);
            List<PanelForm> sources = shownPanels();
            if (sources.Count == 0) { visibility.Manual = manualBefore; ApplyVisibility(); return "(aucun panneau affiché)"; }
            foreach (PanelForm p in sources) await waitIn(p, "typeof draftApi !== 'undefined' && typeof state !== 'undefined'", 6000);
            const string Expected = "\"Ahri,Sion,Vi\"";
            await run(sources[0], "draftApi.reset(); ['Ahri','Sion','Vi'].forEach(c => draftApi.add(c)); 1");
            foreach (PanelForm p in sources)
                if (!await waitIn(p, "state.enemies.join() === 'Ahri,Sion,Vi'", 3000)) problem += "draft de départ absente du panneau " + p.Id + " ; ";
            if (problem != "") { visibility.Manual = manualBefore; ApplyVisibility(); return problem; }

            int lost = 0, notShown = 0, lostOnClose = 0, reloadsBefore = -1;
            for (int k = 0; k < openings; k++)
            {
                if (k == 1) reloadsBefore = FullReloads;
                PanelForm full;
                if (panels.TryGetValue("full", out full) && full.Visible)
                {
                    // Fermeture de la grande fenêtre : les panneaux gardent la draft.
                    full.Hide();
                    await Task.Delay(150);
                    foreach (PanelForm p in sources)
                        if (await run(p, "state.enemies.join()") != Expected) { lostOnClose++; await run(sources[0], "draftApi.reset(); ['Ahri','Sion','Vi'].forEach(c => draftApi.add(c)); 1"); await Task.Delay(300); break; }
                }
                // Comme « Détails » et « Comparer » d'un panneau : source « live ».
                OpenFull(k % 2 == 0 ? "analyse" : "compare", "live");
                for (int i = 0; i < 200 && !(panels.TryGetValue("full", out full) && full.Visible && full.Ready.IsCompleted); i++) await Task.Delay(50);
                // La grande fenêtre doit afficher la vraie draft, puis laisser le temps à une éventuelle diffusion parasite.
                if (!await waitIn(full, "state.enemies.join() === 'Ahri,Sion,Vi'", 3000)) notShown++;
                await Task.Delay(400);
                foreach (PanelForm p in sources)
                    if (await run(p, "state.enemies.join()") != Expected) { lost++; await run(sources[0], "draftApi.reset(); ['Ahri','Sion','Vi'].forEach(c => draftApi.add(c)); 1"); await Task.Delay(300); break; }
            }
            int reloads = reloadsBefore < 0 ? 0 : FullReloads - reloadsBefore;
            DetailOpenReport = openings + " ouvertures (Analyse détaillée / Comparer), " + lost + " perte(s) de draft, " + lostOnClose + " perte(s) à la fermeture, " + notShown + " fenêtre(s) sans la draft, " + reloads + " rechargement(s) de la page";
            if (reloads > 0) problem += reloads + " ouverture(s) rechargent la fenêtre principale au lieu de changer d'onglet ; ";
            if (lost > 0) problem += lost + " perte(s) de draft sur " + openings + " ouvertures ; ";
            if (lostOnClose > 0) problem += lostOnClose + " perte(s) de draft à la fermeture de la grande fenêtre ; ";
            if (notShown > 0) problem += notShown + " ouverture(s) sans la draft en cours ; ";

            // Simulateur : Draft aléatoire reste dans la grande fenêtre, la draft en cours n'est pas touchée.
            PanelForm f;
            panels.TryGetValue("full", out f);
            if (f != null && await waitIn(f, "document.querySelector('[data-test=\"random-draft\"]')", 3000))
            {
                await run(f, "document.querySelector('[data-test=\"random-draft\"]').click(); 1");
                if (!await waitIn(f, "state.enemies.length === 5 && !!document.querySelector('[data-test=\"simulation\"]')", 3000)) problem += "le simulateur ne se lance pas ou n'est pas signalé ; ";
                await Task.Delay(400);
                foreach (PanelForm p in sources) if (await run(p, "state.enemies.join()") != Expected) { problem += "Draft aléatoire écrase la draft du panneau " + p.Id + " ; "; break; }
                // La draft aléatoire peut contenir Jinx : on compare toute la simulation, avant et après l'ajout dans le panneau.
                string simulated = await run(f, "state.enemies.join()");
                await run(sources[0], "draftApi.add('Jinx'); 1");
                await Task.Delay(300);
                if (await run(f, "state.enemies.join()") != simulated) problem += "la simulation est modifiée par la draft en cours ; ";
                await run(f, "document.querySelector('[data-test=\"leave-simulation\"]').click(); 1");
                if (!await waitIn(f, "state.enemies.join() === 'Ahri,Sion,Vi,Jinx' && !document.querySelector('[data-test=\"simulation\"]')", 3000)) problem += "Revenir à la draft en cours ne la retrouve pas ; ";

                // UX-2 : simulation vide, 20 actions, aucune ne touche la draft en cours des panneaux.
                const string Live4 = "\"Ahri,Sion,Vi,Jinx\"";
                await run(f, "document.querySelector('[data-test=\"source-sim\"]').click(); 1");
                if (!await waitIn(f, "simulating && state.enemies.length === 0 && !!document.querySelector('[data-test=\"simulation\"]')", 3000)) problem += "le bouton Simulation n'ouvre pas une simulation vide ; ";
                string[] actions = { "draftApi.add('Garen')", "draftApi.add('Lux')", "document.querySelector('[data-test=\"random-draft\"]').click()", "draftApi.remove(state.enemies[0])",
                    "document.querySelector('[data-test=\"new-draft\"]').click()", "undoLast()", "document.querySelector('[data-test=\"resume-live\"]').click()", "addEnemy('Zed')", "state.enemies.length && removeLastEnemy()", "draftApi.reset()" };
                int touched = 0;
                for (int a = 0; a < 20; a++)
                {
                    await run(f, actions[a % actions.Length] + "; 1");
                    await Task.Delay(120);
                    foreach (PanelForm p in sources) if (await run(p, "state.enemies.join()") != Live4) { touched++; break; }
                }
                if (touched > 0) problem += touched + " action(s) de simulation sur 20 changent la draft des panneaux ; ";
                SimulationReport = "20 actions de simulation, " + touched + " changement(s) des panneaux";
                // « Reprendre la draft en cours » copie la vraie draft ; la suite ne change que la simulation.
                await run(f, "draftApi.reset(); document.querySelector('[data-test=\"resume-live\"]').click(); 1");
                if (!await waitIn(f, "simulating && state.enemies.join() === 'Ahri,Sion,Vi,Jinx'", 2000)) problem += "Reprendre la draft en cours ne copie pas la draft (" + await run(f, "state.enemies.join()") + ") ; ";
                await run(f, "draftApi.add('Garen'); 1");
                await Task.Delay(300);
                foreach (PanelForm p in sources) if (await run(p, "state.enemies.join()") != Live4) { problem += "la copie reste liée à la draft en cours ; "; break; }
                // « Détails » depuis un panneau pendant la simulation : la vraie draft, sans recharger la page.
                await run(f, "window.keepMark2 = 1; 1");
                OpenFull("analyse", "live");
                if (!await waitIn(f, "!simulating && state.enemies.join() === 'Ahri,Sion,Vi,Jinx' && window.keepMark2 === 1", 3000)) problem += "Détails pendant une simulation ne montre pas la vraie draft sans recharger (" + await run(f, "state.enemies.join() + ' ' + simulating + ' ' + window.keepMark2") + ") ; ";
            }
            else problem += "(grande fenêtre absente pour le simulateur) ";

            await run(sources[0], "draftApi.reset(); 1");
            if (panels.TryGetValue("full", out f) && f.Visible) f.Hide();
            visibility.Manual = manualBefore;
            ApplyVisibility();
            return problem;
        }

        public async Task<string> TestPageSecurity()
        {
            OpenFull("");
            PanelForm f = null;
            for (int i = 0; i < 300 && !(panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted && f.View.CoreWebView2 != null); i++) await Task.Delay(50);
            if (f == null || f.View.CoreWebView2 == null) return "(fenêtre absente)";
            Func<string, Task<string>> run = async code => await f.View.CoreWebView2.ExecuteScriptAsync(code);
            string problem = "";
            string csp = await run("(document.querySelector('meta[http-equiv=\"Content-Security-Policy\"]') || {}).content || ''");
            if (!csp.Contains("default-src 'none'") || csp.Contains("unsafe-eval") || !csp.Contains("frame-src 'none'")) problem += "(CSP absente ou affaiblie : " + csp + ") ";
            // Le code injecté par ExecuteScriptAsync est privilégié et échappe à la CSP. On rejoue donc l'attaque d'origine : un draft-stats.js
            // piégé posé directement dans le dossier de la page, comme si la relecture stricte avait été contournée, puis chargé par la page.
            // Seule la CSP le retient ici : c'est elle que l'on vérifie, en défense en profondeur.
            string probe = Path.Combine(webDir, "draft-stats.js");
            File.WriteAllText(probe, "window.DRAFT_STATS = {\"rows\":[]}; window.__csp = []; document.addEventListener('securitypolicyviolation', e => window.__csp.push(e.effectiveDirective || e.violatedDirective)); " +
                "try { eval('1'); window.__eval = 'permis'; } catch (e) { window.__eval = 'bloque'; } " +
                "window.addEventListener('load', () => { fetch('https://exemple.invalid/fuite').catch(() => 0); const i = new Image(); i.src = 'https://exemple.invalid/fuite.png'; " +
                "const fr = document.createElement('iframe'); fr.src = 'https://exemple.invalid/'; document.body.appendChild(fr); });\n");
            await run("location.reload(); 1");
            await Task.Delay(600);
            string evalResult = "";
            for (int i = 0; i < 60 && evalResult != "\"bloque\"" && evalResult != "\"permis\""; i++) { evalResult = await run("window.__eval || ''"); await Task.Delay(100); }
            if (evalResult != "\"bloque\"") problem += "(eval dans un script de la page : " + evalResult + ") ";
            string seen = "";
            for (int i = 0; i < 40; i++)
            {
                seen = await run("window.__csp.join(',')");
                if (seen.Contains("connect-src") && seen.Contains("img-src") && seen.Contains("frame-src")) break;
                await Task.Delay(100);
            }
            if (!seen.Contains("connect-src")) problem += "(requête vers un site inconnu non bloquée) ";
            if (!seen.Contains("img-src")) problem += "(image vers un site inconnu non bloquée) ";
            if (!seen.Contains("frame-src")) problem += "(cadre vers un site inconnu non bloqué) ";
            await run("document.querySelectorAll('iframe').forEach(x => x.remove()); window.open('https://exemple.invalid/fenetre'); location.href = 'https://exemple.invalid/navigation'; 1");
            await Task.Delay(800);
            string origin = await run("location.origin");
            if (origin != "\"https://draftmid.local\"") problem += "(navigation vers un site inconnu acceptée : " + origin + ") ";
            // Remise en état : le fichier de la page redevient la copie relue des données.
            DataScripts.Publish("draft-stats.js", dataDir, webDir);
            await run("location.reload(); 1");
            await Task.Delay(600);
            return problem;
        }

        // Ouvre la fenêtre de clé par un bouton de la page (Réglages > Données), vérifie la lecture cochée et la mise en page, puis annule.
        async Task<string> TestRiotWindowFor(string button, RiotTask expected)
        {
            OpenFull("settings");
            PanelForm f = null;
            for (int i = 0; i < 200 && !(panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted); i++) await Task.Delay(50);
            if (f == null || f.View.CoreWebView2 == null) return "(réglages absents)";
            for (int i = 0; i < 120 && await f.View.CoreWebView2.ExecuteScriptAsync("typeof goView === 'function'") != "true"; i++) await Task.Delay(50);
            await f.View.CoreWebView2.ExecuteScriptAsync("goView('settings', 'data'); 1");
            string find = "document.querySelector('[data-test=\"" + button + "\"]')";
            bool found = false;
            for (int i = 0; i < 120 && !found; i++) { found = await f.View.CoreWebView2.ExecuteScriptAsync("!!(" + find + ")") == "true"; if (!found) await Task.Delay(50); }
            if (!found) return "(bouton " + button + " absent)";
            bool seen = false; RiotTask got = RiotTask.None; string layout = "";
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 300 };
            timer.Tick += (s, e) =>
            {
                RiotKeyDialog d = Application.OpenForms.OfType<RiotKeyDialog>().FirstOrDefault();
                if (d == null || !d.Visible) return;
                timer.Stop();
                seen = true;
                got = d.TestPreselected();
                layout = d.LayoutProblem();
                if (opts.CaptureDir != null) d.SaveCapture(Path.Combine(opts.CaptureDir, "cle-riot-" + button + ".png"));
                d.Close();
            };
            timer.Start();
            await f.View.CoreWebView2.ExecuteScriptAsync(find + ".click(); 1");
            for (int i = 0; i < 200 && !seen; i++) await Task.Delay(50);
            timer.Stop();
            timer.Dispose();
            await Task.Delay(200);
            if (!seen) return "(fenêtre de clé jamais ouverte par " + button + ")";
            if (got != expected) return "(fenêtre de clé : " + got + " coché au lieu de " + expected + ")";
            if (layout != "") return "(fenêtre de clé : " + layout + ")";
            if (rolesCts != null || historyCts != null) return "(annuler a lancé un calcul)";
            return "";
        }

        async Task<string> TestHistoryWindow(string fakeKey)
        {
            string path = Path.Combine(dataDir, HistoryImporter.FileName), before = File.ReadAllText(path), layout = "";
            Msg status = historyStatus;
            bool seen = false, blocked = false, allowed = false;
            OpenFull("settings");
            PanelForm f = null;
            for (int i = 0; i < 200 && !(panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted); i++) await Task.Delay(50);
            if (f == null || f.View.CoreWebView2 == null) return "(réglages absents)";
            for (int i = 0; i < 120 && await f.View.CoreWebView2.ExecuteScriptAsync("typeof goView === 'function' && !!document.querySelector('[data-test=\"settings\"]')") != "true"; i++) await Task.Delay(50);
            await f.View.CoreWebView2.ExecuteScriptAsync("goView('settings', 'data'); 1");
            const string Find = "document.querySelector('[data-test=\"import-history\"]')";
            bool found = false;
            for (int i = 0; i < 120 && !found; i++) { found = await f.View.CoreWebView2.ExecuteScriptAsync("!!(" + Find + ")") == "true"; if (!found) await Task.Delay(50); }
            if (!found)
            {
                string pageText = await f.View.CoreWebView2.ExecuteScriptAsync("(document.querySelector('[data-test=\"settings\"]') ? 'réglages ouverts : ' : 'réglages fermés : ') + location.search + ' | ' + document.body.innerText.slice(0, 160)");
                return "(bouton absent des réglages : " + pageText + ")";
            }
            string file = await f.View.CoreWebView2.ExecuteScriptAsync("(window.DRAFT_HISTORY && window.DRAFT_HISTORY.rows.length) || 0");
            if (file != "3") return "(la page lit " + file + " parties)";
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 300 };
            timer.Tick += (s, e) =>
            {
                RiotKeyDialog d = Application.OpenForms.OfType<RiotKeyDialog>().FirstOrDefault();
                if (d == null || !d.Visible) return;
                timer.Stop();
                seen = true;
                layout = d.LayoutProblem();
                if (opts.CaptureDir != null) d.SaveCapture(Path.Combine(opts.CaptureDir, "import-vide.png"));
                blocked = !d.TestCanStart("Test#EUW", fakeKey, false);
                allowed = d.TestCanStart("Test#EUW", fakeKey, true);
                if (layout == "") layout = d.LayoutProblem();
                if (opts.CaptureDir != null) d.SaveCapture(Path.Combine(opts.CaptureDir, "import-rempli.png"));
                d.Close();
            };
            timer.Start();
            await f.View.CoreWebView2.ExecuteScriptAsync(Find + ".click(); 1");
            for (int i = 0; i < 200 && !seen; i++) await Task.Delay(50);
            timer.Stop();
            timer.Dispose();
            await Task.Delay(200);
            if (!seen) return "(fenêtre d'import jamais ouverte)";
            if (layout != "") return "(fenêtre d'import : " + layout + ")";
            if (!blocked || !allowed) return "(consentement mal appliqué)";
            if (historyCts != null || historyStatus != status || File.ReadAllText(path) != before) return "(annuler a lancé quelque chose)";
            return "";
        }

        // Compte ce que la page retrouve dans ce dossier de données, sans aucun nom de champion ni de joueur. Pour tester une migration.
        public async void ReportStateAndQuit(string path)
        {
            string result = "{\"erreur\":\"page absente\"}";
            try
            {
                await Task.Delay(1500);
                OpenFull("");
                PanelForm f = null;
                for (int i = 0; i < 300 && !(panels.TryGetValue("full", out f) && f.Ready.IsCompleted && f.View.CoreWebView2 != null); i++) await Task.Delay(50);
                for (int i = 0; i < 100 && f != null && await f.View.CoreWebView2.ExecuteScriptAsync("typeof PROFILES !== 'undefined' && hostState.available") != "true"; i++) await Task.Delay(100);
                if (f != null && f.View.CoreWebView2 != null)
                {
                    string js = "JSON.stringify({version:typeof APP_VERSION!=='undefined'?APP_VERSION:null,actif:PROFILES.active,parRole:Object.fromEntries(PROFILE_ROLES.map(r=>[r,PROFILES.roles[r].pool.length])),exclusParRole:Object.fromEntries(PROFILE_ROLES.map(r=>[r,PROFILES.roles[r].excluded.length])),favorisParRole:Object.fromEntries(PROFILE_ROLES.map(r=>[r,PROFILES.roles[r].favorite?1:0])),exclusCalcul:ENGINE_PREFS.excluded.length,favoriCalcul:ENGINE_PREFS.favorite?1:0,ancienProfil:!!localStorage.getItem('draftProfile'),profilsEnregistres:!!localStorage.getItem('draftProfiles'),historique:window.DRAFT_HISTORY&&Array.isArray(window.DRAFT_HISTORY.rows)?window.DRAFT_HISTORY.rows.length:0,journal:loadJournal().length,niveau:PREFS.coachLevel,role:myRole(),langue:typeof I18N!=='undefined'?I18N.lang:null,langueHtml:document.documentElement.lang})";
                    result = new JavaScriptSerializer().Deserialize<string>(await f.View.CoreWebView2.ExecuteScriptAsync(js));
                }
            }
            catch (Exception e) { if (e is OutOfMemoryException) throw; result = "{\"erreur\":\"" + e.GetType().Name + "\"}"; }
            try { File.WriteAllText(path, result, new UTF8Encoding(false)); } catch { }
            Quit();
        }

        // Ouvre les réglages comme le bouton du panneau, puis vérifie qu'ils se ferment des deux façons.
        public string KeyStressReport = "non mesuré", DpiReport = "non mesuré";

        static class CloseProbe
        {
            [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        }

        // Fenêtre principale et réglages, avec de vraies frappes envoyées à la page.
        public async Task<string> TestFullWindow()
        {
            string problem = "";
            PanelForm f = null;
            int keyPause = 40;
            Func<string, Task<string>> run = async code => await f.View.CoreWebView2.ExecuteScriptAsync(code);
            Func<string, Task<bool>> waitFor = async code =>
            {
                for (int i = 0; i < 120; i++)
                {
                    if (f != null && f.Ready.IsCompleted && f.View.CoreWebView2 != null && await run("!!(" + code + ")") == "true") return true;
                    await Task.Delay(50);
                }
                return false;
            };
            Func<Task> findWindow = async () => { for (int i = 0; i < 200 && !(OpenPending == 0 && panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted); i++) await Task.Delay(50); };
            Func<string, string, int, string, Task> press = async (key, code, vk, text) =>
            {
                string common = "\"key\":\"" + key + "\",\"code\":\"" + code + "\",\"windowsVirtualKeyCode\":" + vk;
                await f.View.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", "{\"type\":\"" + (text == "" ? "rawKeyDown" : "keyDown") + "\"," + common + (text == "" ? "" : ",\"text\":\"" + text + "\"") + "}");
                await f.View.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", "{\"type\":\"keyUp\"," + common + "}");
                await Task.Delay(keyPause);
            };
            Func<string, Task> type = async word => { foreach (char c in word) await press(c.ToString(), "Key" + char.ToUpperInvariant(c), char.ToUpperInvariant(c), c.ToString()); };
            const string Search = "document.querySelector('[data-test=\"pool-search\"]')", Shown = "document.querySelector('[data-test=\"settings\"]')";
            // UX-7 : plus de bouton « Fermer » dans les Réglages, sauf ouverts seuls pendant une draft (solo). Sinon, on revient par la navigation.
            const string Leave = "(document.querySelector('[data-test=\"settings-close\"]') || document.querySelector('[data-test=\"nav-sim\"]'))";

            // 1. Fenêtre principale ouverte, puis Réglages : elle n'est ni rechargée, ni masquée.
            OpenFull("");
            await findWindow();
            if (!await waitFor("document.querySelector('[data-test=\"app\"]')")) return "fenêtre principale absente ; ";
            string profileBefore = await run("localStorage.getItem('draftProfile')");
            await run("window.keepMark = 1; state.enemies.length || addEnemy('Sett'); 1");
            await waitFor("state.enemies.length > 0");
            OpenFull("settings");
            if (!await waitFor(Shown)) return "réglages absents ; ";
            // UX-7 : la page suit l'échelle Windows de son écran (devicePixelRatio = DPI de la fenêtre / 96). Barre de titre sombre si Windows l'accepte.
            {
                string dpr = (await run("devicePixelRatio")).Trim('"');
                double page, scale = Native.GetDpiForWindow(f.Handle) / 96.0;
                bool read = double.TryParse(dpr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out page);
                DpiReport = "page à " + dpr + ", fenêtre à " + scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ", barre de titre " + (f.DarkTitle ? "sombre" : "claire (Windows ne le permet pas)");
                if (!read || Math.Abs(page - scale) > 0.01) problem += "la page ne suit pas l'échelle Windows (" + DpiReport + ") ; ";
            }
            if (await run("window.keepMark === 1") != "true") problem += "la page principale est rechargée à l'ouverture ; ";
            // UX-3 : les réglages sont une section à six rubriques ; la mise à jour est dans « Mises à jour », le pool dans Mon pool.
            await run("goView('settings', 'updates'); 1");
            if (Edition.Prerelease)
            {
                // Préversion : pas de bouton, la consigne « télécharge les nouvelles versions à la main ».
                if (!await waitFor("!!document.querySelector('[data-test=\"update-prerelease\"]') && !document.querySelector('[data-test=\"check-update\"]') && document.querySelector('[data-test=\"update-prerelease\"]').textContent.includes(I18N.t('home.update.prerelease'))")) problem += "préversion : consigne de mise à jour à la main absente, ou bouton présent ; ";
            }
            else if (!await waitFor("!!document.querySelector('[data-test=\"check-update\"]')")) problem += "bouton de mise à jour absent ; ";
            await run("goView('pool'); 1");
            if (!await waitFor(Search)) problem += "Mon pool ne s'ouvre pas depuis la navigation ; ";

            // 2. Les frappes vont au champ actif, et nulle part ailleurs.
            await waitFor("state.enemies.length > 0 || addEnemy('Sett') || true");
            string enemiesBefore = await run("state.enemies.join()");
            await run(Search + ".focus(); 1");
            // Frappes perdues : des séries complètes, à deux vitesses, dans la vraie page.
            int sent = 0, lost = 0, roundsWrong = 0;
            string firstWrong = "";
            foreach (int[] speed in new[] { new[] { 40, 25 }, new[] { 15, 15 } })
            {
                keyPause = speed[0];
                for (int round = 0; round < speed[1]; round++)
                {
                    const string Word = "malzahar";
                    await type(Word);
                    sent += Word.Length;
                    string got = (await run(Search + ".value")).Trim('"');
                    if (got != Word) { lost += Word.Length - got.Length; roundsWrong++; if (firstWrong == "") firstWrong = "tour " + round + " à " + speed[0] + " ms : " + got; }
                    for (int k = got.Length; k > 0; k--) await press("Backspace", "Backspace", 8, "");
                    if (await run(Search + ".value") != "\"\"") { problem += "la recherche ne se vide pas au clavier ; "; break; }
                }
            }
            keyPause = 40;
            KeyStressReport = sent + " frappes, " + lost + " perdues, " + roundsWrong + " séries incorrectes" + (firstWrong == "" ? "" : " (" + firstWrong + ")");
            if (lost != 0 || roundsWrong != 0) problem += "frappes perdues : " + KeyStressReport + " ; ";
            await type("ahr");
            if (await run(Search + ".value") != "\"ahr\"") problem += "le texte n'arrive pas dans la recherche (" + await run(Search + ".value") + ") ; ";
            if (await run("(document.getElementById('add') || {}).value || ''") != "\"\"") problem += "le texte part dans la recherche derrière ; ";
            if (await run("document.activeElement === " + Search) != "true") problem += "la recherche perd le focus ; ";
            await press("Backspace", "Backspace", 8, "");
            if (await run(Search + ".value") != "\"ah\"") problem += "Retour arrière n'efface pas la recherche ; ";
            if (await run("state.enemies.join()") != enemiesBefore) problem += "Retour arrière supprime un adversaire de la draft ; ";

            // 3. Recherche, sélection et enregistrement du champion.
            await run("(() => { const v = " + Search + "; v.focus(); return 1; })()");
            await type("ri");
            await press("Enter", "Enter", 13, "\\r");
            if (!await waitFor("!!document.querySelector('[data-test=\"pool-remove\"][data-champion=\"Ahri\"]')")) problem += "Entrée ne sélectionne pas le champion ; ";
            if (!(await run("localStorage.getItem('draftProfile')")).Contains("Ahri")) problem += "le champion n'est pas enregistré ; ";
            if (await run(Search + ".value") != "\"\"") problem += "la recherche ne se vide pas ; ";
            if (await run("document.activeElement === " + Search) != "true") problem += "le focus quitte la recherche après la sélection ; ";
            await run("document.querySelector('[data-test=\"pool-remove\"][data-champion=\"Ahri\"]').click(); 1");
            await Task.Delay(150);
            if (await run("localStorage.getItem('draftProfile')") != profileBefore) problem += "le profil n'est pas rétabli après retrait ; ";

            // 4. Échap et Fermer ne ferment que les réglages, et le focus revient à la fenêtre principale (le Simulateur).
            await run("goView('sim'); 1");
            OpenFull("settings");
            await waitFor(Shown);
            await run("document.querySelector('[data-test=\"settings-section-general\"]').focus(); 1");
            await press("Escape", "Escape", 27, "");
            if (!await waitFor("!document.querySelector('[data-test=\"settings\"]')")) problem += "Échap ne ferme pas les réglages ; ";
            if (await run("state.enemies.join()") != enemiesBefore) problem += "Échap efface la draft ; ";
            OpenFull("settings");
            await waitFor(Shown);
            await run(Leave + ".click(); 1");
            if (!await waitFor("!document.querySelector('[data-test=\"settings\"]')")) problem += "Fermer ne ferme pas les réglages ; ";
            await Task.Delay(200);
            if (!f.Visible) problem += "Fermer masque la fenêtre principale ; ";
            if (await run("!!document.querySelector('[data-test=\"app\"]') && window.keepMark === 1") != "true") problem += "la page principale est perdue ; ";
            await press("a", "KeyA", 65, "a");
            if (await run("(document.getElementById('add') || {}).value") != "\"a\"") problem += "la fenêtre principale ne reçoit plus le clavier ; ";

            // 5. Ouverts depuis le menu, fenêtre masquée : la fenêtre principale reste ouverte derrière les réglages.
            f.Hide();
            OpenFull("settings");
            await findWindow();
            await waitFor(Shown);
            await run(Leave + ".click(); 1");
            await waitFor("!document.querySelector('[data-test=\"settings\"]')");
            await Task.Delay(300);
            if (!f.Visible) problem += "depuis le menu, Fermer masque toute la fenêtre ; ";

            // 6. Pendant une draft, depuis un panneau : rien ne doit rester devant le client.
            // Panneaux réellement affichés (affichage manuel), comme pendant une draft.
            f.Hide();
            bool? manualBefore = visibility.Manual;
            Func<bool, Task> showPanels = async on => { visibility.Manual = on; ApplyVisibility(); for (int i = 0; i < 100 && panelsShown != on; i++) await Task.Delay(50); };
            await showPanels(true);
            OpenFull("settings");
            await findWindow();
            await waitFor(Shown);
            await run(Leave + ".click(); 1");
            for (int i = 0; i < 60 && f.Visible; i++) await Task.Delay(50);
            if (!panelsShown) problem += "les panneaux de test ne sont pas restés affichés ; ";
            if (f.Visible) problem += "pendant une draft, Fermer laisse la grande fenêtre ouverte ; ";

            // 6 bis. Fenêtre principale déjà ouverte pendant une draft, page en plein rechargement (après un import, par exemple) :
            //    fermer les réglages doit ramener à la fenêtre principale, pas la faire disparaître.
            OpenFull("");
            await findWindow();
            await waitFor("document.querySelector('[data-test=\"app\"]')");
            f.View.CoreWebView2.Reload();
            OpenFull("settings");
            await waitFor(Shown);
            await Task.Delay(300);
            await waitFor(Shown);
            await run(Leave + ".click(); 1");
            await waitFor("!document.querySelector('[data-test=\"settings\"]')");
            await Task.Delay(300);
            if (!f.Visible) problem += "fenêtre principale ouverte, page en rechargement : Fermer fait disparaître la fenêtre principale ; ";

            // 6 ter. Réglages ouverts depuis un panneau pendant une draft, puis la draft se termine avant la fermeture :
            //    plus aucun panneau à l'écran, DRAFT ne doit pas sembler fermé.
            f.Hide();
            OpenFull("settings");
            await findWindow();
            await waitFor(Shown);
            await showPanels(false);
            await run(Leave + ".click(); 1");
            for (int i = 0; i < 20; i++) await Task.Delay(50);
            if (!panels.Values.Any(p => p.Visible)) problem += "draft terminée pendant les réglages : Fermer ne laisse plus aucune fenêtre DRAFT ; ";
            visibility.Manual = manualBefore;
            ApplyVisibility();

            // Simulation : le bouton de la page de draft donne une draft complète, différente à chaque clic.
            OpenFull("");
            await findWindow();
            await waitFor("document.querySelector('[data-test=\"random-draft\"]')");
            await run("document.querySelector('[data-test=\"random-draft\"]').click(); 1");
            if (!await waitFor("state.enemies.length === 5 && V.shown")) problem += "Draft aléatoire ne remplit pas la draft ; ";
            string firstDraft = await run("state.enemies.join()");
            bool changed = false;
            for (int i = 0; i < 5 && !changed; i++)
            {
                await run("document.querySelector('[data-test=\"random-draft\"]').click(); 1");
                await Task.Delay(100);
                changed = await run("state.enemies.join()") != firstDraft && await run("state.enemies.length") == "5";
            }
            if (!changed) problem += "Draft aléatoire redonne toujours la même draft ; ";
            await run("resetDraft(); 1");

            // Les mots disent ce que fait l'application, et suivent le rôle du joueur.
            await run("resetDraft(); ['Yasuo','Yone','Lee Sin','Jinx','Thresh'].forEach(addEnemy); 1");
            await waitFor("state.enemies.length === 5 && document.querySelector('[data-test=\"stage\"] [data-test=\"choose-pick\"]')");
            if (await run("document.querySelector('[data-test=\"stage\"] [data-test=\"choose-pick\"]').textContent.includes(I18N.t('advice.cta.play', { champion: '' }).trim())") != "true") problem += "le bouton de choix ne dit pas « Je joue » ; ";
            if (await run("![...document.querySelectorAll('[data-test=\"badge\"]')].some(b => b.textContent.includes('Provisoire'))") != "true") problem += "un badge mélange encore les incertitudes ; ";
            if (await run("!document.querySelector('[data-test=\"result-buttons\"]') || !!state.current") != "true") problem += "les boutons de résultat s'affichent sans choix ; ";
            string support = await run("(() => { const before = PROFILE; applyProfile({ role:'support', pool:['Lux', 'Soraka'] }); computeVerdict(); const text = hypothesisText(); applyProfile(before); computeVerdict(); return text; })()");
            if (!support.Contains("Support") || support.Contains("Mid")) problem += "le texte d'adversaire ignore le rôle (" + support + ") ; ";
            await run("resetDraft(); 1");

            // 7. La croix masque la fenêtre sans quitter l'application, et le dit par une bulle : une fois par session (UX-1).
            OpenFull("");
            await findWindow();
            int balloons = BalloonCount;
            f.Close();
            await Task.Delay(200);
            if (f.Visible || f.IsDisposed || Quitting) problem += "la croix ne masque pas proprement ; ";
            if (BalloonCount != balloons + 1 || LastBalloonKey != "native.balloon.closed") problem += "fermée par la croix, aucune bulle ne dit que DRAFT reste actif (" + LastBalloon + ") ; ";
            OpenFull("");
            await findWindow();
            if (!f.Visible || !await waitFor("document.querySelector('[data-test=\"app\"]')")) problem += "réouverture impossible ; ";
            f.Close();
            await Task.Delay(200);
            if (BalloonCount != balloons + 1) problem += "la bulle de fermeture revient à chaque fermeture ; ";
            f.Hide();

            // 7 bis. Fermeture par un autre programme (WM_CLOSE brut) : la fenêtre doit seulement être masquée,
            //        l'application continue, et Réglages se rouvre normalement.
            OpenFull("");
            await findWindow();
            PanelForm closed = f;
            CloseProbe.SendMessage(closed.Handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            await Task.Delay(300);
            if (Quitting) problem += "WM_CLOSE fait quitter l'application ; ";
            if (closed.Visible || closed.IsDisposed) problem += "WM_CLOSE ne masque pas proprement (détruite=" + closed.IsDisposed + ") ; ";
            f = null;
            OpenFull("settings");
            for (int i = 0; i < 200 && !(panels.TryGetValue("full", out f) && !f.IsDisposed && f.Visible && f.Ready.IsCompleted); i++) await Task.Delay(50);
            if (f == null || f.IsDisposed || !f.Visible) problem += "après une fermeture par WM_CLOSE, les réglages ne se rouvrent plus ; ";
            else
            {
                if (!await waitFor(Shown)) problem += "fenêtre recréée sans les réglages ; ";
                else { await run(Leave + ".click(); 1"); await waitFor("!document.querySelector('[data-test=\"settings\"]')"); }
                f.Hide();
            }
            return problem;
        }

        int openViewToken;
        // Ouvertures d'onglet en attente de la page (tests : attendre la fin avant d'agir sur la fenêtre).
        public int OpenPending;
        public int FullReloads;

        // source "live" : ouvert depuis un panneau (« Détails », « Comparer ») : la fenêtre montre la vraie draft, simulation quittée.
        // section : rubrique des Réglages (« display » depuis l'outil du panneau). Onglet et rubrique : lettres et tirets seulement.
        async void OpenFull(string tab, string source = "", string section = null)
        {
            if (tab == null || !Regex.IsMatch(tab, "^[a-z-]{0,20}$")) tab = "";
            if (section != null && !Regex.IsMatch(section, "^[a-z]{1,20}$")) section = null;
            bool exists = panels.ContainsKey("full");
            PanelForm open;
            // Fenêtre déjà chargée, visible ou masquée : l'onglet s'ouvre sans recharger la page, donc sans perdre ni réinitialiser
            // la draft affichée (UX-2). Si la page ne répond pas (rechargement en cours), on retombe sur la navigation.
            if (tab != "settings" && exists && panels.TryGetValue("full", out open) && !open.IsDisposed && open.Ready.IsCompleted && open.View.CoreWebView2 != null)
            {
                int token = ++openViewToken;
                OpenPending++;
                try
                {
                    open.View.CoreWebView2.PostWebMessageAsJson(new JavaScriptSerializer().Serialize(new Dictionary<string, object> { { "kind", "openView" }, { "tab", tab ?? "" }, { "source", source ?? "" }, { "token", token } }));
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(50);
                        if (open.IsDisposed || open.View.CoreWebView2 == null) break;
                        if (await open.View.CoreWebView2.ExecuteScriptAsync("window.__openViewSeen || 0") == token.ToString())
                        {
                            ShowFull(open);
                            return;
                        }
                    }
                }
                finally { OpenPending--; }
                if (open.IsDisposed) return;
            }
            // Fenêtre principale déjà à l'écran : les réglages s'ouvrent par-dessus, sans la recharger.
            if (tab == "settings" && exists && panels.TryGetValue("full", out open) && open.Visible && open.Ready.IsCompleted && open.View.CoreWebView2 != null)
            {
                // Jeton (UX-8) : la page dit qu'elle a reçu CE message. Vérifier seulement la présence des réglages laissait une course :
                // si la page changeait de section avant la première vérification, la fenêtre était rechargée sur les réglages.
                int settingsToken = ++openViewToken;
                open.View.CoreWebView2.PostWebMessageAsJson("{\"kind\":\"openSettings\",\"token\":" + settingsToken + (section == null ? "" : ",\"section\":\"" + section + "\"") + "}");
                if (opts.Script == null) open.Activate();
                // Une page en plein rechargement (après un import, par exemple) perd ce message : on vérifie, puis on recharge sur les réglages.
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(50);
                    if (open.IsDisposed || open.View.CoreWebView2 == null) return;
                    if (await open.View.CoreWebView2.ExecuteScriptAsync("window.__openSettingsSeen || 0") == settingsToken.ToString()) return;
                }
                // La fenêtre principale était déjà à l'écran : fermer les réglages doit y revenir, jamais la masquer (pas de solo=1).
                open.View.CoreWebView2.Navigate(PageUrl("panel=full&tab=settings" + (section == null ? "" : "&section=" + section)));
                return;
            }
            // Pendant une draft, la fenêtre ne doit pas rester devant le client une fois les réglages fermés.
            string q = "panel=full" + (tab == "" ? "" : "&tab=" + tab) + (section == null ? "" : "&section=" + section) + (tab == "settings" && panelsShown ? "&solo=1" : "");
            await EnsureEnv();
            if (env == null) return;
            PanelForm f = await Ensure("full", q);
            if (exists)
            {
                FullReloads++;
                // La fenêtre n'apparaît qu'une fois la page chargée : sinon un clic sur l'ancienne page se perdrait.
                TaskCompletionSource<bool> loaded = new TaskCompletionSource<bool>();
                EventHandler<CoreWebView2NavigationCompletedEventArgs> done = null;
                done = (s, e) => { f.View.CoreWebView2.NavigationCompleted -= done; loaded.TrySetResult(true); };
                f.View.CoreWebView2.NavigationCompleted += done;
                f.View.CoreWebView2.Navigate(PageUrl(q));
                await Task.WhenAny(loaded.Task, Task.Delay(3000));
                if (f.IsDisposed) return;
            }
            ShowFull(f);
        }

        void ShowFull(PanelForm f)
        {
            Monitor m = ClientWindow.Monitors().OrderByDescending(x => x.Work.W).First();
            if (!f.Visible)
            {
                f.Show();
                f.PlaceNoActivate(new Native.RECT(m.Work.Left + 40, m.Work.Top + 20, m.Work.Left + 40 + Math.Min((int)(1280 * m.Scale), m.Work.W - 80), m.Work.Top + 20 + Math.Min((int)(820 * m.Scale), m.Work.H - 40)), false);
            }
            if (opts.Script == null) f.Activate();
        }

        void ImportStats()
        {
            using (OpenFileDialog d = new OpenFileDialog { Title = Strings.T("native.dialog.statsTitle"), Filter = Strings.T("native.dialog.statsFilter") })
            {
                if (d.ShowDialog() != DialogResult.OK) return;
                // Seules des données passent : le fichier est relu comme du JSON, puis réécrit par DRAFT.
                string safe = DataScripts.ReadCanonical("DRAFT_STATS", d.FileName);
                if (safe == null) { MessageBox.Show(Strings.T("native.dialog.statsInvalid"), "DRAFT"); return; }
                File.WriteAllText(Path.Combine(dataDir, "draft-stats.js"), safe, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(webDir, "draft-stats.js"), safe, new UTF8Encoding(false));
                foreach (PanelForm f in panels.Values) if (f.View.CoreWebView2 != null) f.View.CoreWebView2.Reload();
                Notify(new Msg("native.balloon.statsImported"), ToolTipIcon.Info);
            }
        }

        void OnMessage(PanelForm from, string json)
        {
            Dictionary<string, object> m;
            try { m = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json); } catch { return; }
            object cmdO;
            if (m == null || !m.TryGetValue("cmd", out cmdO)) return;
            bool val = m.ContainsKey("value") && m["value"] is bool && (bool)m["value"];
            switch (cmdO as string)
            {
                case "getHost": if (from.View.CoreWebView2 != null) from.View.CoreWebView2.PostWebMessageAsJson(HostJson()); break;
                case "hideAll":
                {
                    bool wasShown = panelsShown;
                    ToggleManual();
                    // Premier masquage depuis l'outil du panneau dans la session : on dit comment les rappeler (une fois).
                    if (wasShown && !panelsShown && !hideHintShown) { hideHintShown = true; Notify(new Msg(hotkey != null && hotkey.Registered ? "native.balloon.hiddenHotkey" : "native.balloon.hiddenMenu"), ToolTipIcon.Info); }
                    break;
                }
                case "replacePanels": SetFollow(true); UpdatePlacement(true); break;
                case "importStats": ImportStats(); break;
                case "lcuDiagnostic": ShowLcuDiagnostic(); break;
                case "updateRoles": ToggleRiotUpdate(false); break;
                case "updateMatchups": ToggleRiotUpdate(true); break;
                case "checkUpdate": CheckForUpdate(false); break;
                case "setHotkeyBeta": SetBetaHotkey(val); break;
                case "importHistory": ToggleHistoryImport(); break;
                case "tryPanels": TryPanels(); break;
                case "setLanguage": SetLanguage(m.ContainsKey("value") ? m["value"] as string : null); break;
                case "setProfile":
                {
                    object roleO, poolO;
                    if (!m.TryGetValue("role", out roleO) || !m.TryGetValue("pool", out poolO)) break;
                    string role = roleO as string;
                    System.Collections.IEnumerable list = poolO as System.Collections.IEnumerable;
                    if (MatchupStatsUpdater.PositionOf(role) == null || list == null || poolO is string) break;
                    string[] pool = list.Cast<object>().Select(x => RiotCrawler.ChampionKey(x as string)).Where(x => x != "").ToArray();
                    if (pool.Length == 0 || (role == settings.ProfileRole && settings.ProfilePool != null && pool.SequenceEqual(settings.ProfilePool))) break;
                    settings.ProfileRole = role; settings.ProfilePool = pool; Persist();
                    break;
                }
                case "logDraft":
                {
                    object rowO;
                    if (!validationPending || !settings.Diagnostics || !m.TryGetValue("row", out rowO) || !(rowO is System.Collections.IEnumerable)) break;
                    validationPending = false;
                    object predO; m.TryGetValue("predicted", out predO);
                    WriteValidationRow((System.Collections.IEnumerable)rowO, predO as string);
                    break;
                }
                case "profileAdopted": legacyProfile = false; settings.ProfileKnown = true; Persist(); break;
                // Réglages ouverts seuls pendant une draft : on les referme sans rien laisser devant le client.
                // Si la draft est finie entre-temps (plus aucun panneau), la fenêtre principale reste : sinon DRAFT semblerait fermé.
                case "closeFull": { PanelForm full; if (panelsShown && panels.TryGetValue("full", out full)) full.Hide(); break; }
                case "toggleCompact": SetCompact(!settings.Compact); break;
                case "openFull":
                {
                    string tab = m.ContainsKey("tab") ? m["tab"] as string ?? "" : "";
                    string section = m.ContainsKey("section") ? m["section"] as string : null;
                    OpenFull(tab, tab == "settings" ? "" : "live", section);
                    break;
                }
                case "setAutostart": Autostart.Set(val, exePath); RefreshUi(); break;
                case "setFollow": SetFollow(val); break;
                case "setAutoDraft": SetAutoDraft(val); break;
                case "setDiagnostics": settings.Diagnostics = val; Persist(); RefreshUi(); break;
                case "setLcu": settings.LcuEnabled = val; if (!val) { lcu = null; assignedRole = null; } Persist(); RefreshUi(); break;
                case "saveProfiles": SaveProfilesCopy(m.ContainsKey("json") ? m["json"] as string : null); break;
                case "setTopmost": settings.Topmost = val; Persist(); UpdatePlacement(true); RefreshUi(); break;
            }
        }

        async void RunTestScript()
        {
            // Mode de test uniquement : exécute un script dans un panneau, écrit le résultat, capture chaque panneau.
            StringBuilder log = new StringBuilder();
            try
            {
                visibility.Manual = true;
                ApplyVisibility();
                for (int i = 0; i < 200 && !(panels.ContainsKey("right") || panels.ContainsKey("compact")); i++) await Task.Delay(50);
                PanelForm target = panels.ContainsKey("right") ? panels["right"] : panels["compact"];
                await target.Ready;
                for (int i = 0; i < 120; i++)
                {
                    if (await target.View.CoreWebView2.ExecuteScriptAsync("typeof V !== \"undefined\" && !!document.querySelector('[data-test=\"panel\"]')") == "true") break;
                    await Task.Delay(50);
                }
                log.AppendLine("PRET apres " + Program.Clock.ElapsedMilliseconds + " ms depuis le lancement");
                await Task.Delay(opts.SettleMs);
                string script = File.ReadAllText(opts.Script);
                await target.View.CoreWebView2.ExecuteScriptAsync(script);
                string res = "null";
                for (int i = 0; i < 120 && res == "null"; i++) { await Task.Delay(250); res = await target.View.CoreWebView2.ExecuteScriptAsync("window.__result || null"); }
                log.AppendLine("RESULT " + new JavaScriptSerializer().Deserialize<object>(res));
                foreach (PanelForm f in panels.Values.Where(p => p.Visible))
                {
                    log.AppendLine("PANEL " + f.Id + " " + f.Bounds);
                    if (opts.CaptureDir == null) continue;
                    using (FileStream fs = File.Create(Path.Combine(opts.CaptureDir, "app-" + f.Id + ".png")))
                        await f.View.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);
                }
                log.AppendLine("CLIENT " + (ClientOpen ? tracker.Rect.ToString() : "absent") + " | " + (placementNote == null ? "" : placementNote.Key));
            }
            catch (Exception e) { log.AppendLine("EXC " + e); }
            if (opts.Out != null) File.WriteAllText(opts.Out, log.ToString());
            Quit();
        }

        // ---------- Accueil et premier lancement (UX-8) ----------
        // Premier lancement : la fenêtre s'ouvre sur l'Accueil, qui guide en trois étapes au plus. Plus jamais sur Réglages > Général.
        public void OpenOnFirstLaunch() { OpenFull("home"); }

        // « Essayer les panneaux » (étape 3 du premier lancement) : affiche les panneaux et y joue la draft de démonstration
        // de la page (DEMO_SCRIPT, MockDraftProvider). Seulement hors draft et hors partie, panneaux masqués : une vraie draft n'est
        // jamais remplacée. Rien n'est appris de l'écran (contrairement à ToggleManual) et aucun réglage ne change.
        bool demoRunning;
        public int DemoCount;
        public async void TryPanels()
        {
            if (demoRunning || panelsShown || machine.Current == Phase.Draft || machine.Current == Phase.InGame) return;
            demoRunning = true;
            try
            {
                visibility.Manual = true;
                ApplyVisibility();
                RefreshUi();
                for (int i = 0; i < 200; i++)
                {
                    PanelForm p = new[] { "right", "compact" }.Select(PanelFor).FirstOrDefault(x => x != null && !x.IsDisposed && x.Visible);
                    if (p != null && !p.IsDisposed && p.Visible && p.Ready.IsCompleted && p.View.CoreWebView2 != null
                        && await p.View.CoreWebView2.ExecuteScriptAsync("typeof runDemo === 'function' && synced") == "true")
                    {
                        p.View.CoreWebView2.PostWebMessageAsJson("{\"kind\":\"demo\",\"stepMs\":900}");
                        DemoCount++;
                        return;
                    }
                    await Task.Delay(50);
                }
            }
            finally { demoRunning = false; RefreshUi(); }
        }

        // ---------- Mise à jour à la demande : voir AppUpdater ----------
        CancellationTokenSource updateCts;
        Msg updateStatus;
        // Fin de mise à jour sans message (annulation).
        static readonly Msg Cancelled = new Msg("");
        // Heure de la dernière réponse de GitHub (format ISO local), connue pour cette session seulement : rien n'est enregistré.
        string updateChecked = "";

        void SetUpdateStatus(Msg text) { updateStatus = text; RefreshUi(); }

        // ---------- Retours visibles hors de la fenêtre (audit UX, problème 7) ----------
        // Textes du catalogue (native.balloon.*). La dernière bulle est retenue pour les tests : texte affiché et clé.
        public string LastBalloon = "", LastBalloonKey = "";
        public int BalloonCount;
        bool closeHintShown, hideHintShown;
        Action openFromTray;

        // Inventaire du menu de l'icône (tests, rendu de PR) : « entrée » ou « entrée > sous-entrée », séparateurs exclus.
        public List<string> MenuInventory()
        {
            List<string> list = new List<string>();
            foreach (ToolStripItem i in tray.ContextMenuStrip.Items)
            {
                if (i is ToolStripSeparator) continue;
                list.Add(i.Text);
                ToolStripMenuItem m = i as ToolStripMenuItem;
                if (m != null) foreach (ToolStripItem s in m.DropDownItems) if (!(s is ToolStripSeparator)) list.Add(i.Text + " > " + s.Text);
            }
            return list;
        }

        // QA-6 : mécanisme du raccourci prouvé sans toucher au Ctrl+Alt+D d'une autre instance. Combinaison libre et improbable
        // (Ctrl+Alt+Maj+F11) : RegisterHotKey réel, WM_HOTKEY envoyé à la fenêtre (aucune frappe simulée), bascule des panneaux.
        // Puis conflit réel : la même combinaison une seconde fois échoue en 1409, sans plantage, et l'utilisateur en est averti
        // (menu sans raccourci annoncé, consigne dans Réglages > Affichage).
        public string HotkeyReport = "non mesuré";
        public async Task<string> TestHotkeyMechanism()
        {
            string problem = "";
            uint mods = HotkeyWindow.Ctrl | HotkeyWindow.Alt | HotkeyWindow.Shift | HotkeyWindow.NoRepeat, key = (uint)Keys.F11;
            IntPtr lParam = (IntPtr)(int)((key << 16) | (mods & 0x7));
            HotkeyWindow probe = new HotkeyWindow(mods, key, 0x5151);
            if (!probe.Registered) { probe.Dispose(); HotkeyReport = "Ctrl+Alt+Maj+F11 refusé (Win32 " + probe.Error + ")"; return "combinaison d'essai non enregistrée (Win32 " + probe.Error + ") ; "; }
            probe.Pressed += ToggleManual;
            bool? manualBefore = visibility.Manual;
            bool before = panelsShown;
            CloseProbe.SendMessage(probe.Handle, HotkeyWindow.WM_HOTKEY, (IntPtr)0x5151, lParam);
            for (int i = 0; i < 100 && panelsShown == before; i++) await Task.Delay(50);
            bool first = panelsShown;
            CloseProbe.SendMessage(probe.Handle, HotkeyWindow.WM_HOTKEY, (IntPtr)0x5151, lParam);
            for (int i = 0; i < 100 && panelsShown != before; i++) await Task.Delay(50);
            bool second = panelsShown;
            CloseProbe.SendMessage(probe.Handle, HotkeyWindow.WM_HOTKEY, (IntPtr)0x7777, lParam);
            await Task.Delay(150);
            if (probe.Received != 2) problem += probe.Received + " WM_HOTKEY reçu(s) pour 2 envoyés (un autre identifiant doit être ignoré) ; ";
            if (first == before || second != before || panelsShown != before) problem += "bascule des panneaux absente (" + before + " > " + first + " > " + second + ") ; ";
            // Conflit : même combinaison pour une seconde fenêtre.
            HotkeyWindow clash = null;
            try { clash = new HotkeyWindow(mods, key, 0x5152); }
            catch (Exception e) { if (e is OutOfMemoryException) throw; problem += "plantage au second enregistrement (" + e.GetType().Name + ") ; "; }
            if (clash != null)
            {
                if (clash.Registered || clash.Error != HotkeyWindow.AlreadyRegistered) problem += "conflit non détecté (Win32 " + clash.Error + ") ; ";
                HotkeyWindow real = hotkey;
                hotkey = clash;
                RefreshUi();
                string expected = Strings.T(panelsShown ? "native.menu.hidePanels" : "native.menu.showPanels");
                if (miShow.Text != expected) problem += "menu : raccourci encore annoncé (" + miShow.Text + ") ; ";
                if (!HostJson().Contains("\"hotkeyError\":1409") || HostJson().Contains("\"hotkey\":true")) problem += "état envoyé à la page sans le conflit ; ";
                OpenFull("settings");
                PanelForm f = null;
                for (int i = 0; i < 200 && !(OpenPending == 0 && panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted && f.View.CoreWebView2 != null); i++) await Task.Delay(50);
                bool hint = false;
                if (f != null && f.View.CoreWebView2 != null)
                {
                    await f.View.CoreWebView2.ExecuteScriptAsync("goView('settings', 'display'); 1");
                    for (int i = 0; i < 100 && !hint; i++) { hint = await f.View.CoreWebView2.ExecuteScriptAsync("!!document.querySelector('[data-test=\"hotkey-taken\"]') && document.body.innerText.includes(I18N.t('settings.display.hotkeyTaken'))") == "true"; if (!hint) await Task.Delay(50); }
                    f.Hide();
                }
                if (!hint) problem += "Réglages > Affichage n'explique pas le raccourci pris ; ";
                hotkey = real;
                RefreshUi();
                clash.Dispose();
            }
            probe.Dispose();
            visibility.Manual = manualBefore;
            ApplyVisibility();
            HotkeyReport = "Ctrl+Alt+Maj+F11 enregistré, " + probe.Received + " WM_HOTKEY, panneaux " + before + " > " + first + " > " + second + ", second enregistrement : Win32 " + (clash == null ? -1 : clash.Error);
            return problem;
        }

        // Préversion : mise à jour à la main (aucune vérification, aucun accès réseau) ; Ctrl+Alt+D laissé à la 2.x avec sa raison
        // dans Réglages > Affichage, puis activé explicitement par la case de la page (vraie tentative : enregistré ou conflit 1409
        // signalé), puis rendu. Le choix est enregistré dans le dossier de la bêta seulement.
        public string PrereleaseReport = "non mesuré";
        public async Task<string> TestPrereleaseEdition()
        {
            string problem = "";
            int balloons = BalloonCount;
            string checkedBefore = updateChecked;
            CheckForUpdate(true);
            if (updateCts != null || updateChecked != checkedBefore || LastBalloonKey != "native.update.prerelease" || BalloonCount != balloons + 1 || updateStatus == null || updateStatus.Key != "native.update.prerelease")
                problem += "mise à jour depuis le menu : vérification lancée ou bulle absente (" + LastBalloonKey + ") ; ";
            string left = hotkeyLeft ?? "aucune";
            OpenFull("settings");
            PanelForm f = null;
            for (int i = 0; i < 200 && !(OpenPending == 0 && panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted && f.View.CoreWebView2 != null); i++) await Task.Delay(50);
            if (f == null || f.View.CoreWebView2 == null) return problem + "réglages absents ; ";
            Func<string, Task<string>> run = async code => await f.View.CoreWebView2.ExecuteScriptAsync(code);
            Func<string, Task<bool>> waitFor = async code => { for (int i = 0; i < 100; i++) { if (await run("!!(" + code + ")") == "true") return true; await Task.Delay(50); } return false; };
            // La rubrique est redemandée tant qu'elle n'est pas affichée : l'ouverture des réglages peut arriver après le premier appel.
            await waitFor("document.querySelector('[data-test=\"replace-panels\"]') || (goView('settings', 'display'), false)");
            string forced = "non essayé", back = "non essayé";
            if (hotkeyLeft != null)
            {
                string key = hotkeyLeft == "instance" ? "settings.display.hotkeyLeftInstance" : "settings.display.hotkeyLeftAutostart";
                if (!await waitFor("document.querySelector('[data-test=\"hotkey-left\"]') && document.querySelector('[data-test=\"hotkey-left\"]').textContent.includes(I18N.t('" + key + "')) && document.querySelector('[data-test=\"hotkey-use\"]') && !document.querySelector('[data-test=\"hotkey-use\"]').checked"))
                {
                    string seen = await run("JSON.stringify({ left: hostState.hotkeyLeft, pre: hostState.prerelease, forced: hostState.hotkeyForced, settings: !!document.querySelector('[data-test=\"settings\"]'), text: ((document.querySelector('main') || document.body).innerText || '').slice(0, 300) })");
                    problem += "Réglages > Affichage n'explique pas pourquoi Ctrl+Alt+D est laissé à la 2.x (" + seen + ") ; ";
                }
                await run("document.querySelector('[data-test=\"hotkey-use\"]').click(); 1");
                for (int i = 0; i < 60 && hotkey == null; i++) await Task.Delay(50);
                if (hotkey == null || hotkeyLeft != null || !BetaState.Load(dataDir).Hotkey) problem += "activation explicite : aucune tentative d'enregistrement ; ";
                else
                {
                    forced = hotkey.Registered ? "Ctrl+Alt+D enregistré" : "Win32 " + hotkey.Error + (hotkey.Error == HotkeyWindow.AlreadyRegistered ? " (déjà pris : autres instances " + string.Join(", ", Edition.OtherDraftProcesses()) + ")" : "");
                    string expect = hotkey.Registered ? "!document.querySelector('[data-test=\"hotkey-taken\"]')" : hotkey.Error == HotkeyWindow.AlreadyRegistered ? "document.querySelector('[data-test=\"hotkey-taken\"]')" : "true";
                    if (!await waitFor(expect + " && document.querySelector('[data-test=\"hotkey-use\"]').checked")) problem += "activation explicite : état de la page faux (" + forced + ") ; ";
                }
                await run("document.querySelector('[data-test=\"hotkey-use\"]').click(); 1");
                for (int i = 0; i < 60 && hotkey != null; i++) await Task.Delay(50);
                back = hotkey == null && hotkeyLeft != null ? "rendu à la 2.x (" + hotkeyLeft + ")" : "gardé (" + (hotkeyLeft ?? "aucune raison") + ")";
                if (BetaState.Load(dataDir).Hotkey || (hotkeyLeft != null && hotkey != null)) problem += "désactivation : Ctrl+Alt+D pas rendu ; ";
                if (!await waitFor("document.querySelector('[data-test=\"hotkey-left\"]') && !document.querySelector('[data-test=\"hotkey-use\"]').checked")) problem += "désactivation : la page ne le montre pas ; ";
            }
            // Autre DRAFT ouverte (source simulée « DraftMid.exe ») : notice et conseil dans Réglages > Affichage et sur l'Accueil,
            // puis plus rien quand elle disparaît. Aucune action sur un autre processus.
            Func<List<string>> realSource = OtherDraftSource;
            OtherDraftSource = () => new List<string> { "DraftMid.exe pid 1", "DraftMid.exe pid 2" };
            RefreshOtherDraft();
            string notice = "document.body.innerText.includes(I18N.t('prerelease.otherDraft', { names: 'DraftMid.exe' }))";
            if (!await waitFor("document.querySelector('[data-test=\"other-draft\"]') && " + notice)) problem += "autre DRAFT : Réglages > Affichage ne le signale pas ; ";
            await run("goView('home'); 1");
            if (!await waitFor("document.querySelector('[data-test=\"home-other-draft\"]') && " + notice)) problem += "autre DRAFT : l'Accueil ne le signale pas ; ";
            OtherDraftSource = () => new List<string>();
            RefreshOtherDraft();
            if (!await waitFor("!document.querySelector('[data-test=\"home-other-draft\"]')")) problem += "autre DRAFT fermée : la notice reste ; ";
            string otherReport = "notice « " + Strings.T("prerelease.otherDraft", "names", "DraftMid.exe").Substring(0, 40) + "… » affichée puis retirée";
            OtherDraftSource = realSource;
            RefreshOtherDraft();
            f.Hide();
            PrereleaseReport = "mise à jour : bulle « " + LastBalloon + " », aucune vérification ; Ctrl+Alt+D laissé : " + left + " ; activation explicite : " + forced + " ; puis " + back + " ; " + otherReport;
            return problem;
        }

        // Menu et overlay (UX-6) : 8 entrées au plus, double-clic qui ouvre DRAFT, fonctions sorties du menu présentes dans la fenêtre,
        // bulle au premier masquage depuis un panneau.
        // I18N-3 : changement de langue sans redémarrage, depuis le programme puis depuis le vrai sélecteur de la page.
        // Menu de l'icône, fenêtre principale (rechargée tout de suite) et panneaux (rechargés hors draft) ; réglage enregistré.
        public string LanguageReport = "non mesuré";
        public async Task<string> TestLanguageSwitch()
        {
            string problem = "", before = Strings.Lang, other = before == "fr" ? "en" : "fr", saved = settings.Language;
            PanelForm f = PanelFor("full");
            if (f == null || f.View.CoreWebView2 == null) return "fenêtre principale absente ; ";
            Func<string, Task<bool>> waitFor = async code => { for (int i = 0; i < 120; i++) { try { if (await f.View.CoreWebView2.ExecuteScriptAsync("!!(" + code + ")") == "true") return true; } catch { } await Task.Delay(50); } return false; };
            int reloads = LanguageReloads;
            SetLanguage(other);
            if (Strings.Lang != other) problem += "langue du programme inchangée ; ";
            if (miOpen.Text != Strings.T("native.menu.open") || !MenuInventory().Contains(MenuText.Settings)) problem += "menu non traduit ; ";
            if (!await waitFor("typeof I18N!=='undefined' && I18N.lang==='" + other + "' && document.documentElement.lang==='" + other + "' && !!document.querySelector('[data-test=\"language-select\"]')")) problem += "fenêtre principale pas rechargée en " + other + " ; ";
            if (!await waitFor("I18N.missing().length===0")) problem += "clés absentes dans la page ; ";
            if (UserPreferences.Load(settingsPath).Language != other) problem += "réglage non enregistré ; ";
            bool panelsDone = !langReloadPending;
            foreach (PanelForm p in panels.Values.ToList().Where(x => x.Id != "full" && x.View.CoreWebView2 != null))
            {
                bool ok = false;
                for (int i = 0; i < 120 && !ok; i++) { try { ok = await p.View.CoreWebView2.ExecuteScriptAsync("typeof I18N!=='undefined' && I18N.lang") == "\"" + other + "\""; } catch { } if (!ok) await Task.Delay(50); }
                if (panelsDone && !ok) problem += "panneau " + p.Id + " pas rechargé ; ";
            }
            // Retour par le vrai sélecteur de Réglages > Général (commande setLanguage envoyée par la page).
            await f.View.CoreWebView2.ExecuteScriptAsync("(()=>{const s=document.querySelector('[data-test=\"language-select\"]');const set=Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype,'value').set;set.call(s,'" + before + "');s.dispatchEvent(new Event('change',{bubbles:true}));return 1})()");
            for (int i = 0; i < 100 && Strings.Lang != before; i++) await Task.Delay(50);
            if (Strings.Lang != before) problem += "le sélecteur de la page ne change pas la langue ; ";
            if (!await waitFor("typeof I18N!=='undefined' && I18N.lang==='" + before + "'")) problem += "fenêtre principale pas revenue en " + before + " ; ";
            settings.Language = saved;
            Persist();
            LanguageReport = before + " > " + other + " > " + before + ", " + (LanguageReloads - reloads) + " rechargement(s), panneaux " + (panelsDone ? "rechargés hors draft" : "en attente de la fin de la draft");
            return problem;
        }

        public async Task<string> TestMenuAndOverlay()
        {
            string problem = "";
            List<string> inv = MenuInventory();
            int top = inv.Count(x => !x.Contains(" > "));
            if (top > 8) problem += top + " entrées au premier niveau du menu ; ";
            foreach (string need in new[] { MenuText.Open, MenuText.Simulate, MenuText.Detection + " > " + MenuText.TeachDraft, MenuText.Detection + " > " + MenuText.TeachNotDraft, MenuText.Detection + " > " + MenuText.LcuDiagnostic, MenuText.Settings, MenuText.CheckUpdate, MenuText.Quit })
                if (!inv.Contains(need)) problem += "menu sans « " + need + " » ; ";
            if (!inv.Any(x => x.StartsWith(MenuText.ShowPanels) || x.StartsWith(MenuText.HidePanels))) problem += "menu sans « Afficher les panneaux » ; ";
            MenuReport = string.Join(" | ", inv);
            // Double-clic : la fenêtre principale s'ouvre, sur l'Accueil.
            PanelForm f;
            if (panels.TryGetValue("full", out f) && f.Visible) f.Hide();
            openFromTray();
            f = null;
            for (int i = 0; i < 200 && !(OpenPending == 0 && panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted); i++) await Task.Delay(50);
            if (f == null || !f.Visible) return problem + "le double-clic n'ouvre pas la fenêtre principale ; ";
            Func<string, Task<bool>> waitFor = async code => { for (int i = 0; i < 100; i++) { if (await f.View.CoreWebView2.ExecuteScriptAsync("!!(" + code + ")") == "true") return true; await Task.Delay(50); } return false; };
            if (!await waitFor("document.querySelector('[data-test=\"view-home\"]')")) problem += "le double-clic n'ouvre pas l'Accueil ; ";
            // Ce qui sort du menu reste dans la fenêtre.
            string[][] homes = {
                new[] { "settings", "display", "replace-panels" }, new[] { "settings", "display", "x" }, new[] { "settings", "general", "x" },
                new[] { "settings", "data", "update-roles" }, new[] { "settings", "data", "update-matchups" }, new[] { "settings", "diag", "lcu-diagnostic" } };
            foreach (string[] h in homes)
            {
                await f.View.CoreWebView2.ExecuteScriptAsync("goView('" + h[0] + "', '" + h[1] + "'); 1");
                if (h[2] != "x" && !await waitFor("document.querySelector('[data-test=\"" + h[2] + "\"]')")) problem += h[2] + " absent de Réglages > " + h[1] + " ; ";
            }
            await f.View.CoreWebView2.ExecuteScriptAsync("goView('settings', 'display'); 1");
            if (!await waitFor("['settings.display.auto','settings.display.follow','settings.display.compact'].every(k => document.body.innerText.includes(I18N.t(k)))")) problem += "affichage automatique, collage ou panneau unique absents de Réglages > Affichage ; ";
            await f.View.CoreWebView2.ExecuteScriptAsync("goView('settings', 'general'); 1");
            if (!await waitFor("document.body.innerText.includes(I18N.t('settings.general.autostart'))")) problem += "« Démarrer avec Windows » absent de Réglages > Général ; ";
            await f.View.CoreWebView2.ExecuteScriptAsync("goView('stats'); 1");
            await waitFor("document.querySelector('[data-test=\"stats-tab-history\"]')");
            await f.View.CoreWebView2.ExecuteScriptAsync("document.querySelector('[data-test=\"stats-tab-history\"]').click(); 1");
            if (!await waitFor("document.querySelector('[data-test=\"import-stats\"]')")) problem += "import de draft-stats.js absent de Mes statistiques ; ";
            await f.View.CoreWebView2.ExecuteScriptAsync("goView('sim'); 1");
            f.Hide();
            // Masquer depuis l'outil du panneau : une bulle la première fois dans la session, plus ensuite.
            bool? manualBefore = visibility.Manual;
            visibility.Manual = true; ApplyVisibility();
            for (int i = 0; i < 100 && !panelsShown; i++) await Task.Delay(50);
            PanelForm panel = panels.Values.FirstOrDefault(p => p.Id != "full" && p.Visible);
            int balloons = BalloonCount;
            if (panel != null) OnMessage(panel, "{\"cmd\":\"hideAll\"}");
            if (panelsShown || BalloonCount != balloons + 1 || !(LastBalloonKey == "native.balloon.hiddenHotkey" || LastBalloonKey == "native.balloon.hiddenMenu")) problem += "masquer depuis le panneau : pas de bulle « Panneaux masqués » (" + LastBalloon + ") ; ";
            visibility.Manual = true; ApplyVisibility();
            for (int i = 0; i < 100 && !panelsShown; i++) await Task.Delay(50);
            panel = panels.Values.FirstOrDefault(p => p.Id != "full" && p.Visible);
            if (panel != null) OnMessage(panel, "{\"cmd\":\"hideAll\"}");
            if (BalloonCount != balloons + 1) problem += "la bulle de masquage revient à chaque fois ; ";
            visibility.Manual = manualBefore; ApplyVisibility();
            return problem;
        }
        public string MenuReport = "";

        // UX-8 : Accueil et premier lancement, dans la vraie fenêtre. Le premier lancement ouvre l'Accueil (plus Réglages > Général) ;
        // l'Accueil suit l'état réel transmis par le programme (lecture du client sur un LCU simulé, mise à jour vérifiée) ;
        // « Activer la lecture du client » et « Essayer les panneaux » passent par les vraies commandes de la page. Aucun appel réseau.
        public string HomeReport = "non mesuré";
        public async Task<string> TestHome()
        {
            string problem = "";
            PanelForm f;
            if (panels.TryGetValue("full", out f) && f.Visible) f.Hide();
            OpenOnFirstLaunch();
            f = null;
            for (int i = 0; i < 200 && !(OpenPending == 0 && panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted && f.View.CoreWebView2 != null); i++) await Task.Delay(50);
            if (f == null || !f.Visible) return "le premier lancement n'ouvre pas la fenêtre principale ; ";
            Func<string, Task<string>> run = async code => await f.View.CoreWebView2.ExecuteScriptAsync(code);
            Func<string, Task<bool>> waitFor = async code => { for (int i = 0; i < 100; i++) { if (await run("!!(" + code + ")") == "true") return true; await Task.Delay(50); } return false; };
            if (!await waitFor("document.querySelector('[data-test=\"view-home\"]') && document.querySelector('[data-test=\"home\"]')")) problem += "le premier lancement n'ouvre pas l'Accueil (" + await run("location.search + ' ' + (document.querySelector('main') || {}).className") + ") ; ";
            if (await run("!!document.querySelector('[data-test=\"settings\"]')") == "true") problem += "le premier lancement ouvre encore les Réglages ; ";
            // Préversion : ni heure ni bouton de vérification, la consigne de mise à jour à la main et l'état de la copie des données.
            if (Edition.Prerelease)
            {
                if (!await waitFor("document.querySelector('[data-test=\"home-update-prerelease\"]') && !document.querySelector('[data-test=\"home-check-update\"]') && document.querySelector('[data-test=\"home-update\"]').textContent.includes(I18N.t('home.update.prerelease'))")) problem += "préversion : l'Accueil n'affiche pas la consigne de mise à jour à la main, ou garde le bouton ; ";
                if (!await waitFor("document.querySelector('[data-test=\"home-beta-copy\"]')")) problem += "préversion : l'Accueil ne dit pas d'où viennent les données ; ";
            }
            // Mise à jour vérifiée plus tôt (source simulée) : résultat et heure sur l'Accueil.
            else if (updateChecked != "" &&!await waitFor("(() => { const s = (document.querySelector('[data-test=\"home-update-checked\"]') || {}).textContent || ''; return s.startsWith(I18N.t('home.update.checked', { time: '@' }).split('@')[0]) && /\\d:\\d\\d/.test(s); })()")) problem += "l'Accueil n'affiche pas l'heure de la dernière vérification ; ";
            if (!Edition.Prerelease && updateStatus != null && !await waitFor("((document.querySelector('[data-test=\"home-update-status\"]') || {}).textContent || '').length > 5")) problem += "l'Accueil n'affiche pas le résultat de la mise à jour ; ";

            // Lecture du client : activée par le bouton de l'Accueil, puis LCU simulé connecté, puis client injoignable.
            bool lcuBefore = settings.LcuEnabled;
            LcuSession lcuSaved = lcu;
            settings.LcuEnabled = false; lcu = null; RefreshUi();
            if (!await waitFor("document.querySelector('[data-test=\"home-enable-lcu\"]')")) problem += "lecture désactivée : pas de bouton « Activer la lecture du client » ; ";
            else
            {
                await run("document.querySelector('[data-test=\"home-enable-lcu\"]').click(); 1");
                for (int i = 0; i < 60 && !settings.LcuEnabled; i++) await Task.Delay(50);
                if (!settings.LcuEnabled) problem += "« Activer la lecture du client » ne l'active pas ; ";
            }
            SimulatedLcuTransport sim = new SimulatedLcuTransport { PhaseJson = "\"Lobby\"" };
            TestUseLcu(() => sim);
            await PollLcuOnce(); NotifyLcuStatus();
            if (!await waitFor("document.querySelector('[data-test=\"home-league\"][data-state=\"connected\"]') && document.querySelector('[data-test=\"home-hero\"]').textContent.includes(I18N.t('home.hero.ready.connected'))")) problem += "LCU simulé connecté : l'Accueil ne le montre pas (" + LcuStatus() + ") ; ";
            await CaptureHome(f, "connecte");
            sim.Offline = true;
            await PollLcuOnce(); NotifyLcuStatus();
            string after = LcuStatus();
            if (!await waitFor("document.querySelector('[data-test=\"home-league\"][data-state=\"" + after + "\"]')")) problem += "client injoignable : l'Accueil ne suit pas (" + after + ") ; ";
            if (after == "failed" && !await waitFor("document.querySelector('[data-test=\"home-diagnostic\"]')")) problem += "connexion impossible : pas d'action « Diagnostic de connexion LoL » ; ";
            if (after == "waiting") await CaptureHome(f, "lol-ferme");
            settings.LcuEnabled = lcuBefore; lcu = lcuSaved; Persist(); RefreshUi();

            // « Essayer les panneaux » : panneaux affichés, draft de démonstration jouée, aucun réglage modifié, rien appris de l'écran.
            string settingsBefore = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : "";
            bool? manualBefore = visibility.Manual;
            int demos = DemoCount;
            if (panelsShown || machine.Current == Phase.Draft) problem += "(essai des panneaux : panneaux déjà affichés, essai non vérifié) ";
            else
            {
                await run("hostSend({ cmd:'tryPanels' }); 1");
                PanelForm shown = null;
                for (int i = 0; i < 200 && DemoCount == demos; i++) await Task.Delay(50);
                shown = new[] { "right", "compact" }.Select(PanelFor).FirstOrDefault(x => x != null && !x.IsDisposed && x.Visible);
                if (DemoCount != demos + 1 || !panelsShown || shown == null) problem += "« Essayer les panneaux » n'affiche pas les panneaux avec la démonstration ; ";
                else
                {
                    bool seen = false;
                    for (int i = 0; i < 80 && !seen; i++) { seen = await shown.View.CoreWebView2.ExecuteScriptAsync("state.enemies.includes('Sett')") == "true"; if (!seen) await Task.Delay(100); }
                    if (!seen) problem += "démonstration absente des panneaux ; ";
                    // Fin de la démonstration (8 étapes de 900 ms), puis panneaux rendus à leur état et draft vidée.
                    for (int i = 0; i < 120 && await shown.View.CoreWebView2.ExecuteScriptAsync("!!state.sessionActive") == "true"; i++) await Task.Delay(100);
                    await shown.View.CoreWebView2.ExecuteScriptAsync("draftApi.reset(); 1");
                }
                string settingsAfter = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : "";
                if (settingsAfter != settingsBefore) problem += "« Essayer les panneaux » modifie les réglages ; ";
                visibility.Manual = manualBefore; ApplyVisibility(); RefreshUi();
            }
            HomeReport = "premier lancement sur l'Accueil, lecture du client " + (problem.Contains("LCU") ? "?" : "connectée puis " + after) + ", " + (DemoCount - demos) + " essai(s) des panneaux";
            if (f.Visible) f.Hide();
            return problem;
        }

        // Capture de l'Accueil (e2e avec --capture) : vraie WebView2, zone cliente en pixels physiques, données de test neutres.
        async Task CaptureHome(PanelForm f, string name)
        {
            if (opts.CaptureDir == null) return;
            Size keep = f.ClientSize;
            foreach (Size size in new[] { new Size(1366, 768), new Size(1920, 1080) })
            {
                f.ClientSize = size;
                await Task.Delay(700);
                using (FileStream fs = File.Create(Path.Combine(opts.CaptureDir, "ux8-e2e-accueil-" + name + "-" + f.ClientSize.Width + "x" + f.ClientSize.Height + ".png")))
                    await f.View.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);
            }
            f.ClientSize = keep;
        }

        // Une bulle près de l'horloge. Retenue pour les tests ; jamais affichée en mode silencieux.
        void Notify(Msg msg, ToolTipIcon icon)
        {
            string text = msg.ToString();
            LastBalloon = text;
            LastBalloonKey = msg.Key;
            BalloonCount++;
            if (!opts.Silent && tray != null) tray.ShowBalloonTip(5000, "DRAFT", text, icon);
        }

        // La croix masque la fenêtre principale sans quitter : on le dit une fois par session, pas à chaque fermeture.
        public void OnMainWindowClosed()
        {
            if (closeHintShown || Quitting) return;
            closeHintShown = true;
            Notify(new Msg("native.balloon.closed"), ToolTipIcon.Info);
        }

        // Jamais pendant une draft, une partie, un import ou un calcul : l'application redémarre.
        Msg UpdateBlocked()
        {
            if (machine.Current == Phase.Draft || machine.Current == Phase.InGame) return new Msg("native.update.blockedDraft");
            if (rolesCts != null || historyCts != null) return new Msg("native.update.blockedTask");
            return null;
        }

        // fromMenu : lancée depuis le menu de l'icône, la recherche répond par une bulle, réglages ouverts ou non.
        public void CheckForUpdate(bool fromMenu)
        {
            // Préversion de test : aucune vérification ni installation automatique, aucun accès à GitHub. Mise à jour à la main.
            if (Edition.Prerelease)
            {
                Msg manual = new Msg("native.update.prerelease");
                SetUpdateStatus(manual);
                if (fromMenu) Notify(manual, ToolTipIcon.Info);
                return;
            }
            RunUpdate(new GitHubUpdateSource(), fromMenu, info => MessageBox.Show(Strings.T("native.update.confirm", "version", info.Version.ToString(3), "current", AppUpdater.Current.ToString(3)), "DRAFT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
        }

        // Relance avec les mêmes options : un essai isolé reste sur son dossier de données.
        static string RelaunchArguments(params string[] extra)
        {
            List<string> args = new List<string>();
            string[] all = Environment.GetCommandLineArgs();
            for (int i = 1; i < all.Length; i++)
            {
                if (all[i] == "--test-update" || all[i] == "--after-update") { i++; continue; }
                if (all[i] == "--update-rolled-back") continue;
                args.Add("\"" + all[i].Replace("\"", "\\\"") + "\"");
            }
            args.AddRange(extra);
            return string.Join(" ", args);
        }

        async void RunUpdate(IUpdateSource source, bool notify, Func<UpdateInfo, bool> confirm)
        {
            if (updateCts != null) return;
            Msg blocked = UpdateBlocked();
            if (blocked != null) { SetUpdateStatus(blocked); if (notify) Notify(blocked, ToolTipIcon.Warning); return; }
            CancellationTokenSource cts = new CancellationTokenSource();
            updateCts = cts;
            // done : message de fin ; null tant que rien n'est fini, Silent (clé vide) pour une annulation sans message.
            Msg done = null;
            try
            {
                SetUpdateStatus(new Msg("native.update.checking"));
                UpdateInfo info = await source.LatestAsync(cts.Token);
                updateChecked = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                if (!AppUpdater.IsNewer(AppUpdater.Current, info.Version)) { done = new Msg("native.update.upToDate", "version", AppUpdater.Current.ToString(3)); return; }
                if (!confirm(info)) { done = new Msg("native.update.available", "version", info.Version.ToString(3)); return; }
                int shown = -1;
                Progress<long> progress = new Progress<long>(bytes =>
                {
                    int percent = (int)(bytes * 100 / Math.Max(1, info.Size));
                    if (percent / 5 != shown && updateCts != null) { shown = percent / 5; SetUpdateStatus(new Msg("native.update.downloading", "percent", percent)); }
                });
                IProgress<long> checking = new Progress<long>(_ => SetUpdateStatus(new Msg("native.update.verifying")));
                string candidate = await AppUpdater.Prepare(source, info, exePath, dataDir, progress, (c, d, ct) => { checking.Report(0); AppUpdater.SelfCheck(c, d, ct); }, cts.Token);
                blocked = UpdateBlocked();
                if (blocked != null) { File.Delete(candidate); done = blocked; return; }
                AppUpdater.Swap(exePath, candidate);
                // La nouvelle version est lancée une fois l'application fermée : voir Program.Run.
                Program.Relaunch = new[] { exePath, RelaunchArguments("--after-update", Process.GetCurrentProcess().Id.ToString()), RelaunchArguments("--update-rolled-back") };
                updateCts = null;
                Quit();
            }
            catch (OperationCanceledException) { done = Cancelled; }
            catch (Exception e)
            {
                if (e is OutOfMemoryException) throw;
                Program.Log(dataDir, "Mise à jour : " + e.GetType().Name);
                done = cts.IsCancellationRequested ? Cancelled : AppUpdater.FriendlyError(e);
            }
            finally
            {
                if (updateCts == cts) updateCts = null;
                if (done != null && !Quitting)
                {
                    SetUpdateStatus(done == Cancelled ? null : done);
                    if (notify && done != Cancelled) Notify(done, done.Key == "native.update.upToDate" || done.Key == "native.update.available" ? ToolTipIcon.Info : ToolTipIcon.Warning);
                    if (opts.TestUpdate != null) { if (opts.Out != null) File.WriteAllText(opts.Out, "RESULTAT : " + (done == Cancelled ? "" : done.ToString())); Quit(); }
                }
            }
        }

        public async void TestUpdate(string localFile) { await Task.Delay(100); RunUpdate(new LocalUpdateSource(localFile), false, info => true); }

        // Mise à jour lancée « depuis le menu », sur une source simulée : renvoie la bulle affichée (vide si aucune).
        public async Task<string> TestUpdateFromMenu(IUpdateSource source)
        {
            int before = BalloonCount;
            RunUpdate(source, true, info => false);
            for (int i = 0; i < 100 && (updateCts != null || BalloonCount == before); i++) await Task.Delay(30);
            return BalloonCount == before ? "" : LastBalloon;
        }

        public void AnnounceRolledBack()
        {
            updateStatus = new Msg("native.update.rolledBack");
            RefreshUi();
            if (!opts.Silent) tray.ShowBalloonTip(5000, "DRAFT", updateStatus.ToString(), ToolTipIcon.Warning);
        }

        public void AnnounceUpdated()
        {
            updateStatus = new Msg("native.update.installed", "version", AppUpdater.Current.ToString(3));
            RefreshUi();
            if (!opts.Silent) tray.ShowBalloonTip(5000, "DRAFT", updateStatus.ToString(), ToolTipIcon.Info);
        }

        public void Quit()
        {
            Quitting = true;
            if (rolesCts != null) rolesCts.Cancel();
            if (historyCts != null) historyCts.Cancel();
            if (updateCts != null) updateCts.Cancel();
            slowPoll.Stop(); sampler.Stop();
            SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
            Persist();
            tracker.Dispose();
            grabber.Dispose();
            if (hotkey != null) hotkey.Dispose();
            tray.Visible = false;
            tray.Dispose();
            foreach (PanelForm f in panels.Values.ToList()) { try { f.Close(); f.Dispose(); } catch { } }
            ExitThread();
        }
    }

    // ---------- Scénario de bout en bout, avec une fausse fenêtre de client et des phases simulées ----------
    static class EndToEnd
    {
        public const string FakeTitle = "DraftMid-FauxClient-Test";

        // Source de mise à jour simulée : aucune connexion, aucun téléchargement.
        class StubUpdateSource : IUpdateSource
        {
            public Version Latest = AppUpdater.Current;
            public Exception Fail;
            public string PublicKey { get { return ""; } }
            public Task<string> SignatureAsync(UpdateInfo info, CancellationToken ct) { return Task.FromResult(""); }
            public Task<UpdateInfo> LatestAsync(CancellationToken ct) { if (Fail != null) throw Fail; return Task.FromResult(new UpdateInfo { Version = Latest, Tag = "v" + Latest.ToString(3) }); }
            public Task DownloadAsync(UpdateInfo info, string path, IProgress<long> progress, CancellationToken ct) { throw new InvalidOperationException("aucun téléchargement en test"); }
        }

        class FakeClient : Form
        {
            public FakeClient()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                BackColor = Color.FromArgb(30, 30, 40);
                ShowInTaskbar = false;
                Text = FakeTitle;
            }
            protected override bool ShowWithoutActivation { get { return true; } }
        }

        static bool IsFake(IntPtr h)
        {
            StringBuilder t = new StringBuilder(64);
            Native.GetWindowText(h, t, 64);
            return t.ToString() == FakeTitle;
        }

        // La fenêtre active peut changer si l'utilisateur clique ailleurs : seul compte que DRAFT ne la prenne pas.
        // La fausse fenêtre LoL peut avoir le premier plan : seuls les panneaux ne doivent jamais le prendre.
        static bool IsAbove(IntPtr a, IntPtr b)
        {
            for (IntPtr h = NativeHook.GetWindow(a, 2); h != IntPtr.Zero; h = NativeHook.GetWindow(h, 2)) if (h == b) return true;
            return false;
        }

        // Plusieurs parties de suite sur un faux client : la mémoire et les poignées ne doivent pas monter à chaque cycle.
        public static async void RunSoak(CompanionApp app, Options o)
        {
            StringBuilder s = new StringBuilder();
            try
            {
                await Task.Delay(1000);
                Monitor main = ClientWindow.Monitors().First(m => m.Primary);
                Process me = Process.GetCurrentProcess();
                for (int cycle = 1; cycle <= o.Soak; cycle++)
                {
                    FakeClient fake = new FakeClient();
                    fake.SetBounds(main.Work.Left + 400, main.Work.Top + 20, Math.Min(1000, main.Work.W - 900), Math.Min(720, main.Work.H - 40));
                    fake.Show();
                    await Task.Delay(100);
                    app.Tracker.Refresh();
                    Action<Evidence, int> feed = (ev, n) => { for (int i = 0; i < n; i++) app.StepPhase(true, false, ev, 1); };
                    feed(Evidence.NotDraft, 3);
                    feed(Evidence.Draft, 2);
                    for (int i = 0; i < 300 && !(app.PanelFor("left") != null && app.PanelFor("left").Visible); i++) await Task.Delay(50);
                    PanelForm left = app.PanelFor("left");
                    if (left == null || !left.Visible) { s.AppendLine("ECHEC : cycle " + cycle + " : panneaux absents"); break; }
                    await Task.Delay(1000);
                    await left.View.CoreWebView2.ExecuteScriptAsync("['Sett','Vi','Galio','Jinx','Janna'].forEach(n => addEnemy(n))");
                    await Task.Delay(1000);
                    // Premier cycle : une sélection immobile, pour mesurer le processeur de l'extérieur.
                    if (cycle == 1) { File.WriteAllText(o.Out + ".idle", ""); await Task.Delay(22000); File.Delete(o.Out + ".idle"); }
                    me.Refresh();
                    string during = (me.PrivateMemorySize64 / 1048576) + " Mo, " + me.HandleCount + " poignées, " + me.Threads.Count + " threads";
                    feed(Evidence.NotDraft, 3);
                    app.StepPhase(false, true, Evidence.Unknown, 1);
                    await Task.Delay(500);
                    feed(Evidence.NotDraft, 1);
                    fake.Close(); fake.Dispose();
                    app.Tracker.Refresh();
                    app.StepPhase(false, false, Evidence.Unknown, 1);
                    await Task.Delay(2500);
                    me.Refresh();
                    s.AppendLine("CYCLE " + cycle + " : draft " + during + " | client fermé " + (me.PrivateMemorySize64 / 1048576) + " Mo, " + me.HandleCount + " poignées, " + me.Threads.Count + " threads");
                }
                s.AppendLine("OK : " + o.Soak + " cycles terminés");
            }
            catch (Exception e) { s.AppendLine("ECHEC : " + e); }
            File.WriteAllText(o.Out, s.ToString());
            Environment.Exit(0);
        }

        static bool PanelHasFocus(CompanionApp app)
        {
            IntPtr front = NativeHook.GetForegroundWindow();
            return new[] { "left", "right", "compact" }.Select(app.PanelFor).Any(p => p != null && p.Handle == front);
        }

        // Autres processus DRAFT de la session (lecture seule des noms) : le détenteur probable d'un raccourci déjà pris.
        public static string OtherInstances()
        {
            int self = Process.GetCurrentProcess().Id, session = Process.GetCurrentProcess().SessionId;
            List<string> found = new List<string>();
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    string n = p.ProcessName;
                    if (p.Id != self && p.SessionId == session && (n.StartsWith("DraftMid", StringComparison.OrdinalIgnoreCase) || n.StartsWith("DRAFT", StringComparison.OrdinalIgnoreCase))) found.Add(n + ".exe pid " + p.Id);
                }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
                finally { p.Dispose(); }
            }
            return found.Count == 0 ? "aucune" : string.Join(", ", found);
        }

        public static Func<IntPtr, bool> Matcher { get { return IsFake; } }

        static string TheirTeam(params int[] ids)
        {
            return "{\"theirTeam\":[" + string.Join(",", ids.Select(i => "{\"championId\":" + i + "}")) + "]}";
        }

        // Déroulé complet d'une draft sur un faux client LCU : aucune connexion réelle.
        static async Task RunLcuScenario(CompanionApp app, Action<bool, string> t, Func<string, Task<string>> js)
        {
            const int Sett = 875, Vi = 254, Galio = 3, Janna = 40, Aphelios = 523, Ahri = 103, Syndra = 134, Jinx = 222;
            int opens = 0;
            SimulatedLcuTransport sim = new SimulatedLcuTransport();
            app.TestUseLcu(() => { opens++; return sim; });
            Func<Task> poll = async () => { await app.PollLcuOnce(); await Task.Delay(120); };
            Func<Task<string>> enemies = async () => await js("state.enemies.join()");
            Func<string, Task> phase = async name => { sim.PhaseJson = "\"" + name + "\""; await poll(); };

            sim.PhaseJson = "\"None\"";
            await poll();
            t(!app.PanelsShown, "Scénario 2. Accueil du client : aucun panneau");
            await phase("Lobby");
            t(!app.PanelsShown, "Scénario 3. Lobby : aucun panneau");
            await phase("ReadyCheck");
            t(!app.PanelsShown, "Scénario : partie trouvée, aucun panneau avant la sélection");

            sim.SessionJson = TheirTeam();
            await phase("ChampSelect");
            for (int i = 0; i < 40 && !app.PanelsShown; i++) await Task.Delay(50);
            await Task.Delay(200);
            t(app.PanelsShown && (await enemies()) == "\"\"", "Scénario 4. Début de sélection : panneaux affichés, composition vide");
            t((await js("V.bans.length > 0")) == "true", "Scénario 4. Un ban est conseillé avant les picks");

            sim.SessionJson = TheirTeam(Sett); await poll();
            sim.SessionJson = TheirTeam(Sett, Vi); await poll();
            string two = await enemies();
            t(two.Contains("Sett") && two.Contains("Vi"), "Scénario 5. Adversaires ajoutés un par un (" + two + ")");
            t((await js("state.manualMid === null")) == "true", "Scénario 7. Mid non confirmé : il reste une supposition");
            string shownBefore = await js("state.shown");

            sim.SessionJson = TheirTeam(Sett, Galio); await poll();
            string changed = await enemies();
            t(changed.Contains("Galio") && !changed.Contains("Vi"), "Scénario 6. Changement de champion (" + changed + ")");

            sim.SessionJson = "{\"theirTeam\":[{\"champ"; await poll();
            t((await enemies()) == changed, "Réponse tronquée : composition conservée");
            sim.SessionJson = ""; await poll();
            t((await enemies()) == changed && app.PanelsShown, "Session absente : composition et panneaux conservés");

            sim.SessionJson = TheirTeam(Sett, Galio, Ahri); await poll();
            string midGuess = await js("currentMid()");
            // Les deux sont 100 % mid dans Meraki : personne n'est désigné. Les rôles écrits à la main, eux, les départagent.
            bool withMeraki = (await js("typeof ROLE_RATES !== 'undefined'")) == "true";
            t(withMeraki
                ? midGuess == "null" && (await js("V.confidence === 'low' && V.res.ranking[0].byScenario.length >= 2 && !V.hypothesis.main && unnamedState() === 'unidentified'")) == "true"
                : midGuess.Contains("Ahri") && (await js("V.confidence !== 'high'")) == "true", "Scénario 7. Sett, Galio, Ahri : aucune certitude affichée (" + midGuess + ")");
            t((await js("state.manualMid === null")) == "true", "Scénario 7. Aucune correction manuelle inventée");
            t((await js("V.reasons.length > 0")) == "true", "Le verdict affiche ses raisons (d'abord " + shownBefore + ")");

            sim.SessionJson = TheirTeam(Sett, Galio, Ahri, Jinx, Janna); await poll();
            t((await js("state.enemies.length")) == "5" && app.PanelsShown, "Scénario 8. Draft complète");

            int opensBefore = opens;
            sim.Hang = true;
            Stopwatch frozen = Stopwatch.StartNew();
            bool answered = await app.PollLcuOnce();
            t(!answered && frozen.ElapsedMilliseconds < 1500 && app.PanelsShown, "Scénario 13. Requête bloquée : abandon en " + frozen.ElapsedMilliseconds + " ms, panneaux conservés");
            sim.Hang = false;
            await poll();
            t(opens == opensBefore + 1 && (await js("state.enemies.length")) == "5", "Scénario 13. Reprise après blocage, composition conservée");

            sim.Offline = true;
            t(!await app.PollLcuOnce() && app.PanelsShown && !app.Lcu.Connected, "Scénario 11. Client déconnecté : panneaux conservés, identifiants oubliés");
            sim.Offline = false;
            SimulatedLcuTransport restarted = new SimulatedLcuTransport { PhaseJson = "\"Lobby\"" };
            sim = restarted;
            await poll();
            t(!app.PanelsShown && app.Lcu.Connected, "Scénario 12. Client redémarré : reconnexion, retour au lobby, panneaux masqués");

            sim.SessionJson = TheirTeam(Syndra);
            await phase("ChampSelect");
            for (int i = 0; i < 40 && !app.PanelsShown; i++) await Task.Delay(50);
            await poll();
            await Task.Delay(150);
            string fresh = await enemies();
            t(fresh.Contains("Syndra") && !fresh.Contains("Sett") && !fresh.Contains("Janna"), "Nouvelle sélection : la draft précédente est effacée (" + fresh + ")");

            await phase("Lobby");
            t(!app.PanelsShown, "Scénario 10. Sélection annulée : panneaux masqués");
            sim.SessionJson = TheirTeam(Aphelios);
            await phase("ChampSelect");
            for (int i = 0; i < 40 && !app.PanelsShown; i++) await Task.Delay(50);
            await phase("GameStart");
            t(!app.PanelsShown && app.CurrentPhase == Phase.InGame, "Scénario 9. Fin de sélection : chargement sans panneau");
            await phase("InProgress");
            t(!app.PanelsShown, "Scénario : partie en cours, aucun panneau");
            await phase("EndOfGame");
            t(!app.PanelsShown, "Scénario : fin de partie, aucun panneau");

            // Partie complète, draft annulée comprise : les panneaux suivent exactement les phases, sans aucun clignotement.
            string[][] journey = {
                new[] { "None", "non" }, new[] { "Lobby", "non" }, new[] { "Matchmaking", "non" }, new[] { "ReadyCheck", "non" }, new[] { "ChampSelect", "oui" }, new[] { "ChampSelect", "oui" },
                new[] { "Lobby", "non" }, new[] { "Matchmaking", "non" }, new[] { "ReadyCheck", "non" }, new[] { "ChampSelect", "oui" }, new[] { "GameStart", "non" }, new[] { "InProgress", "non" },
                new[] { "Reconnect", "non" }, new[] { "InProgress", "non" }, new[] { "WaitingForStats", "non" }, new[] { "PreEndOfGame", "non" }, new[] { "EndOfGame", "non" }, new[] { "Lobby", "non" },
                new[] { "ChampSelect", "oui" }, new[] { "None", "non" }
            };
            string wrong = "";
            int showsBefore = app.ShowCount;
            app.TestSetDiagnostics(true);
            await Task.Delay(100);
            await phase("ChampSelect");
            sim.SessionJson = TheirTeam(Sett, Vi, Galio, Jinx, Janna); await poll();
            await phase("GameStart");
            await Task.Delay(300);
            string[] rows = File.Exists(app.ValidationPath) ? File.ReadAllLines(app.ValidationPath) : new string[0];
            t(rows.Length == 2 && rows[0].StartsWith("date;version;role") && rows[1].Contains("Sett,Vi,Galio,Jinx,Janna") && rows[1].Contains(";InGame;;;;") && rows[0].EndsWith(";remarques;roles_predits") && Regex.IsMatch(rows[1], ";;;;Sett=[a-z]+:[01]\\.\\d\\d(,[^=,;]+=[a-z]+:[01]\\.\\d\\d){4}$"), "Relevé : une ligne par sélection, colonnes réelles à remplir, rôles prédits en fin de ligne (" + rows.Length + " lignes)");
            app.TestSetDiagnostics(false);
            await phase("None");
            showsBefore = app.ShowCount;
            foreach (string[] step in journey)
            {
                await phase(step[0]);
                if (app.PanelsShown != (step[1] == "oui")) wrong += step[0] + " ";
            }
            t(wrong == "", "Parcours complet : panneaux visibles seulement en sélection " + wrong);
            t(app.ShowCount - showsBefore == 3, "Parcours complet : 3 sélections, 3 apparitions, aucun clignotement (" + (app.ShowCount - showsBefore) + ")");
            sim.SessionJson = "";

            app.StepPhase(false, false, Evidence.Unknown, 1);
            t(!app.PanelsShown && app.CurrentPhase == Phase.ClientClosed && !app.Lcu.Connected, "Scénario 1. Client fermé : veille, identifiants oubliés");
            t(sim.Requests.All(LcuEndpoints.IsAllowed), "Scénario : seuls les deux endpoints documentés sont appelés");
            t(await app.TestReleaseAndReopen(), "Client fermé puis rouvert : l'application reste ouverte et recrée ses panneaux");
            string fullProblem = await app.TestFullWindow();
            s_keyReport = app.KeyStressReport;
            t(fullProblem == "", "Réglages et fenêtre principale : frappes, sélection, enregistrement et fermeture ; échelle (" + app.DpiReport + ") " + fullProblem);
            string menuProblem = await app.TestMenuAndOverlay();
            t(menuProblem == "", "Menu de l'icône (8 entrées au plus, rien de perdu), double-clic, bulle de masquage (" + app.MenuReport + ") " + menuProblem);
            if (Strings.Lang == "en") t(app.MenuReport.Contains("Open DRAFT") && app.MenuReport.Contains("Settings") && !app.MenuReport.Contains("Réglages"), "Langue : menu de l'icône en anglais");
            else t(app.MenuReport.Contains("Ouvrir DRAFT") && app.MenuReport.Contains("Réglages"), "Langue : menu de l'icône en français");
            string hotkeyProblem = await app.TestHotkeyMechanism();
            t(hotkeyProblem == "", "QA-6. Mécanisme du raccourci, essai isolé sur Ctrl+Alt+Maj+F11 : enregistrement, WM_HOTKEY, bascule, conflit 1409 signalé sans plantage (" + app.HotkeyReport + ") " + hotkeyProblem);
            string profileProblem = await app.TestProfilesAndRole();
            t(profileProblem == "", "Profils par rôle et rôle attribué par LoL, dans la vraie fenêtre " + profileProblem);
            string historyProblem = await app.TestHistoryImport();
            t(historyProblem == "", "Import Riot simulé depuis l'application, sans appel réel, sans trace de clé " + historyProblem);
            string detailProblem = await app.TestDraftSurvivesDetail(50);
            t(detailProblem == "", "Analyse détaillée, Comparer et simulateur n'effacent jamais la draft en cours (" + app.DetailOpenReport + " ; " + app.SimulationReport + ") " + detailProblem);
            string securityProblem = await app.TestPageSecurity();
            t(securityProblem == "", "Protections de la page dans la vraie WebView2 : CSP, eval, réseau, cadres, navigation " + securityProblem);
            // Mise à jour lancée depuis le menu de l'icône : chaque issue répond par une bulle, réglages fermés (UX-1).
            string upToDate = await app.TestUpdateFromMenu(new StubUpdateSource());
            t(upToDate == Strings.T("native.update.upToDate", "version", AppUpdater.Current.ToString(3)), "Mise à jour depuis le menu : bulle « DRAFT est à jour » (" + upToDate + ")");
            string available = await app.TestUpdateFromMenu(new StubUpdateSource { Latest = new Version(99, 0, 0) });
            t(available == Strings.T("native.update.available", "version", "99.0.0"), "Mise à jour depuis le menu, installation refusée : bulle « version disponible » (" + available + ")");
            string failed = await app.TestUpdateFromMenu(new StubUpdateSource { Fail = new System.Net.WebException("test") });
            t(failed == Strings.T("native.update.offline"), "Mise à jour depuis le menu, réseau absent : bulle d'erreur en clair (" + failed + ")");
            // UX-8 : la dernière vérification réussie (« à jour ») est rejouée pour que l'Accueil ait une heure et un résultat à montrer.
            await app.TestUpdateFromMenu(new StubUpdateSource());
            if (Edition.Prerelease)
            {
                string preProblem = await app.TestPrereleaseEdition();
                t(preProblem == "", "Préversion " + Edition.Version + " : mise à jour à la main, Ctrl+Alt+D laissé à la 2.x et activable dans Réglages > Affichage (" + app.PrereleaseReport + ") " + preProblem);
            }
            string homeProblem = await app.TestHome();
            t(homeProblem == "", "Accueil et premier lancement : premier lancement sur l'Accueil, état League réel (LCU simulé), activation et essai des panneaux par la page (" + app.HomeReport + ") " + homeProblem);
        }

        static string s_keyReport = "";

        // Session LCU simulée avec le poste du joueur (myTeam[localPlayerCellId].assignedPosition) et un champion allié choisi
        // exprès dans un autre rôle : le rôle doit venir du poste seul, jamais du champion.
        static string SessionWithPosition(string position, int myChampion, params int[] enemies)
        {
            return "{\"localPlayerCellId\":2,\"myTeam\":[{\"cellId\":1,\"assignedPosition\":\"middle\",\"championId\":103},{\"cellId\":2,\"assignedPosition\":\"" + position + "\",\"championId\":" + myChampion + "}],\"theirTeam\":[" + string.Join(",", enemies.Select(i => "{\"championId\":" + i + "}")) + "]}";
        }

        static async Task<bool> Until(Func<Task<bool>> ok, int ms)
        {
            for (int i = 0; i < ms / 50; i++) { try { if (await ok()) return true; } catch (Exception e) { if (e is OutOfMemoryException) throw; } await Task.Delay(50); }
            return false;
        }

        // QA A2 : rôle attribué par League, de bout en bout (LCU simulé > programme > pages). Pools fictifs, un par rôle.
        public static string RoleReport = "non mesuré";
        static async Task<string> TestAssignedRoleThroughLcu(CompanionApp app)
        {
            string p = "";
            SimulatedLcuTransport sim = new SimulatedLcuTransport { PhaseJson = "\"Lobby\"" };
            app.TestUseLcu(() => sim);
            await app.PollLcuOnce();
            app.TestOpenFull("pool");
            PanelForm f = null;
            await Until(async () => { f = app.PanelFor("full"); return app.OpenPending == 0 && f != null && f.Visible && f.Ready.IsCompleted && f.View.CoreWebView2 != null && await f.View.CoreWebView2.ExecuteScriptAsync("typeof PROFILES !== 'undefined' && hostState.available") == "true"; }, 15000);
            if (f == null || f.View.CoreWebView2 == null) return "(fenêtre absente)";
            Func<string, Task<string>> run = async code => await f.View.CoreWebView2.ExecuteScriptAsync(code);
            Func<string, Task<bool>> wait = code => Until(async () => await run("!!(" + code + ")") == "true", 5000);
            Func<string, Task<string>> everywhere = async code => { List<string> r = new List<string> { await run(code) }; foreach (string id in new[] { "left", "right", "compact" }) { PanelForm pf = app.PanelFor(id); if (pf != null && !pf.IsDisposed && pf.Ready.IsCompleted && pf.View.CoreWebView2 != null) r.Add(id + ":" + await pf.View.CoreWebView2.ExecuteScriptAsync(code)); } return string.Join(" ", r); };
            // La fenêtre vient de s'ouvrir : tant qu'elle n'est pas synchronisée (1,5 s au plus), elle ne diffuse rien aux panneaux.
            await wait("synced");
            await run("saveMyProfile('top',['Sett','Ornn']); saveMyProfile('jungle',['Viego','Lee Sin']); saveMyProfile('mid',['Fizz','Xerath']); saveMyProfile('adc',['Jinx','Caitlyn']); saveMyProfile('support',['Janna','Lulu']); savePrefs({autoRole:'auto'}); switchRole('mid','manual'); resetRoleSession(); publish(null); 1");
            if (!await wait("PREFS.autoRole==='auto' && myRole()==='mid'")) return "(préparation)";
            if (!await Until(async () => !(await everywhere("typeof PROFILES!=='undefined' && ['top','jungle','mid','adc','support'].every(r => PROFILES.roles[r].pool.length===2)")).Contains("false"), 8000)) return "(profils non partagés par toutes les pages : " + await everywhere("PROFILE_ROLES.map(r => PROFILES.roles[r].pool.length).join()") + ")";
            string[][] cases = { new[] { "TOP", "top", "Sett,Ornn" }, new[] { "JUNGLE", "jungle", "Viego,Lee Sin" }, new[] { "MIDDLE", "mid", "Fizz,Xerath" }, new[] { "BOTTOM", "adc", "Jinx,Caitlyn" }, new[] { "UTILITY", "support", "Janna,Lulu" } };
            int ok = 0;
            // Champion allié du joueur : Janna (support) quel que soit le poste ; il ne doit jamais décider du rôle.
            foreach (string[] c in cases)
            {
                sim.PhaseJson = "\"Lobby\""; sim.SessionJson = ""; await app.PollLcuOnce(); await Task.Delay(150);
                sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = SessionWithPosition(c[0], 40, 875);
                await app.PollLcuOnce(); await app.PollLcuOnce();
                bool good = app.AssignedRole == c[1] && await wait("state.assignedRole==='" + c[1] + "' && myRole()==='" + c[1] + "' && PICKS.join()==='" + c[2] + "'");
                if (good) ok++; else p += c[0] + " : rôle " + app.AssignedRole + " / page " + await run("myRole() + ' ' + PICKS.join()") + " ; ";
            }
            // Poste vide (sélection aveugle) ou inconnu : aucun rôle deviné, le rôle choisi reste.
            foreach (string pos in new[] { "", "NOUVEAU" })
            {
                sim.PhaseJson = "\"Lobby\""; sim.SessionJson = ""; await app.PollLcuOnce(); await Task.Delay(150);
                await run("switchRole('jungle','manual'); resetRoleSession(); 1");
                sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = SessionWithPosition(pos, 103, 875);
                await app.PollLcuOnce(); await app.PollLcuOnce(); await Task.Delay(300);
                if (app.AssignedRole != null || !await wait("!state.assignedRole && myRole()==='jungle' && PICKS.join()==='Viego,Lee Sin'")) p += "poste « " + pos + " » : un rôle a été deviné (" + app.AssignedRole + ") ; ";
            }
            // Changement de rôle entre deux drafts (MIDDLE puis TOP), sans reste de la première.
            sim.PhaseJson = "\"Lobby\""; sim.SessionJson = ""; await app.PollLcuOnce(); await Task.Delay(150);
            sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = SessionWithPosition("MIDDLE", 40, 875, 254); await app.PollLcuOnce(); await app.PollLcuOnce();
            bool firstMid = await wait("myRole()==='mid'");
            sim.PhaseJson = "\"Lobby\""; sim.SessionJson = ""; await app.PollLcuOnce(); await Task.Delay(150);
            bool cleared = app.AssignedRole == null && await wait("!state.assignedRole");
            sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = SessionWithPosition("TOP", 40, 3); await app.PollLcuOnce(); await app.PollLcuOnce();
            if (!firstMid || !cleared || !await wait("myRole()==='top' && PICKS.join()==='Sett,Ornn'")) p += "changement de rôle entre deux drafts (" + firstMid + ", " + cleared + ", " + await run("myRole()") + ") ; ";
            // Réglage autoRole : « suggest » propose sans changer, « off » ne fait rien.
            sim.PhaseJson = "\"Lobby\""; sim.SessionJson = ""; await app.PollLcuOnce(); await Task.Delay(150);
            await run("savePrefs({autoRole:'suggest'}); switchRole('mid','manual'); resetRoleSession(); 1");
            sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = SessionWithPosition("JUNGLE", 40, 875); await app.PollLcuOnce(); await app.PollLcuOnce();
            if (!await wait("state.assignedRole==='jungle' && myRole()==='mid' && roleSuggestion()==='jungle'")) p += "autoRole suggest : " + await run("myRole() + ' ' + roleSuggestion()") + " ; ";
            sim.PhaseJson = "\"Lobby\""; sim.SessionJson = ""; await app.PollLcuOnce(); await Task.Delay(150);
            await run("savePrefs({autoRole:'off'}); switchRole('mid','manual'); resetRoleSession(); 1");
            sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = SessionWithPosition("JUNGLE", 40, 875); await app.PollLcuOnce(); await app.PollLcuOnce(); await Task.Delay(300);
            if (!await wait("myRole()==='mid' && roleSuggestion()===null")) p += "autoRole off : " + await run("myRole() + ' ' + roleSuggestion()") + " ; ";
            // Sans LCU : le rôle choisi à la main reste, aucun rôle attribué.
            sim.PhaseJson = "\"Lobby\""; sim.SessionJson = ""; await app.PollLcuOnce();
            sim.Offline = true; await app.PollLcuOnce();
            // Toutes les pages (fenêtre et panneaux) doivent d'abord partager le même état : une page en retard rediffuserait l'ancien rôle.
            await Until(async () => !(await everywhere("typeof PROFILES!=='undefined' && myRole()===PROFILES.active && !state.assignedRole")).Contains("false"), 5000);
            await run("savePrefs({autoRole:'auto'}); switchRole('support','manual'); resetRoleSession(); 1");
            await Until(async () => !(await everywhere("typeof PROFILES!=='undefined' && myRole()==='support'")).Contains("false"), 5000);
            await Task.Delay(300);
            if (app.AssignedRole != null || !await wait("!state.assignedRole && myRole()==='support' && PICKS.join()==='Janna,Lulu'")) p += "sans LCU : rôle manuel non respecté (programme " + app.AssignedRole + ", page " + await run("myRole() + ' ' + PICKS.join() + ' attribué ' + state.assignedRole + ' auto ' + PREFS.autoRole") + " ; toutes les pages : " + await everywhere("myRole()") + ") ; ";
            await run("savePrefs({autoRole:'suggest'}); switchRole('mid','manual'); resetRoleSession(); 1");
            bool getOnly = sim.Requests.All(LcuEndpoints.IsAllowed);
            if (!getOnly) p += "endpoint non autorisé appelé ; ";
            RoleReport = ok + "/5 postes, vide et inconnu sans déduction, changement entre drafts, suggest/off, sans LCU ; " + sim.Requests.Count + " requêtes, toutes autorisées : " + getOnly;
            f.Hide();
            return p;
        }

        // QA A1 : changement de langue PENDANT une draft (LCU simulé). Les panneaux gardent la draft et leur langue jusqu'à la fin
        // de la sélection, puis se rechargent ; la draft suivante est dans la nouvelle langue, sans reste de la précédente.
        // Menu et bulles changent tout de suite. Valeur hors liste refusée, « auto » = langue de Windows.
        public static string LanguageDraftReport = "non mesuré";
        static async Task<string> TestLanguageDuringDraft(CompanionApp app)
        {
            string p = "", before = Strings.Lang, other = before == "fr" ? "en" : "fr", saved = app.Prefs.Language;
            SimulatedLcuTransport sim = new SimulatedLcuTransport { PhaseJson = "\"Lobby\"" };
            app.TestUseLcu(() => sim);
            await app.PollLcuOnce(); await Task.Delay(200);
            sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = TheirTeam(875, 254, 3);
            await app.PollLcuOnce();
            await Until(() => Task.FromResult(app.PanelsShown), 10000);
            Func<string, Task<string>> inPanels = async code =>
            {
                List<string> r = new List<string>();
                foreach (string id in new[] { "left", "right", "compact" }) { PanelForm f = app.PanelFor(id); if (f != null && f.Visible && f.View.CoreWebView2 != null) r.Add(await f.View.CoreWebView2.ExecuteScriptAsync(code)); }
                return string.Join("|", r);
            };
            Func<string, Task<bool>> allPanels = code => Until(async () => { string v = await inPanels("!!(" + code + ")"); return v != "" && v.Split('|').All(x => x == "true"); }, 8000);
            await app.PollLcuOnce();
            if (!await allPanels("typeof state!=='undefined' && state.enemies.length===3 && I18N.lang==='" + before + "'")) return "(draft simulée absente : " + await inPanels("state.enemies.join() + ' ' + I18N.lang") + ")";
            // Correction manuelle dans la première draft : elle ne doit pas survivre à la draft suivante.
            await inPanels("setMid('Sett'); 1");
            await Task.Delay(300);
            int reloads = app.LanguageReloads;
            app.SetLanguage(other);
            await Task.Delay(1000);
            if (Strings.Lang != other) p += "langue du programme inchangée ; ";
            if (!app.LanguagePending) p += "aucune attente signalée pendant la draft ; ";
            string during = await inPanels("I18N.lang + ':' + state.enemies.length");
            if (during.Split('|').Any(x => x != "\"" + before + ":3\"")) p += "panneaux rechargés ou draft perdue en pleine sélection (" + during + ") ; ";
            if (app.MenuOpenText != Strings.T("native.menu.open")) p += "menu non traduit pendant la draft ; ";
            string balloon = await app.TestUpdateFromMenu(new StubUpdateSource { Latest = new Version(99, 0, 0) });
            if (balloon != Strings.T("native.update.blockedDraft")) p += "bulle pas dans la nouvelle langue (" + balloon + ") ; ";
            // Fin de la sélection : rechargement dans la nouvelle langue.
            sim.PhaseJson = "\"Lobby\""; sim.SessionJson = "";
            await app.PollLcuOnce();
            await Task.Delay(300);
            if (app.LanguagePending) p += "rechargement toujours en attente après la draft ; ";
            sim.PhaseJson = "\"ChampSelect\""; sim.SessionJson = TheirTeam(134);
            await app.PollLcuOnce();
            await Until(() => Task.FromResult(app.PanelsShown), 10000);
            await app.PollLcuOnce();
            if (!await allPanels("typeof I18N!=='undefined' && I18N.lang==='" + other + "' && document.documentElement.lang==='" + other + "' && state.enemies.join()==='Syndra' && state.manualMid===null && I18N.missing().length===0")) p += "draft suivante pas dans la nouvelle langue ou reste de la précédente (" + await inPanels("I18N.lang + ' ' + state.enemies.join() + ' mid ' + state.manualMid") + ") ; ";
            // Retour hors draft, valeur hors liste refusée, « auto ».
            sim.PhaseJson = "\"Lobby\""; sim.SessionJson = ""; await app.PollLcuOnce(); await Task.Delay(200);
            string setting = app.Prefs.Language;
            app.TestMessage("{\"cmd\":\"setLanguage\",\"value\":\"de\"}");
            app.TestMessage("{\"cmd\":\"setLanguage\",\"value\":\"<img src=x>\"}");
            app.TestMessage("{\"cmd\":\"setLanguage\",\"value\":3}");
            app.TestMessage("{\"cmd\":\"setLanguage\"}");
            if (Strings.Lang != other || app.Prefs.Language != setting) p += "valeur hors liste acceptée (" + app.Prefs.Language + ") ; ";
            app.TestMessage("{\"cmd\":\"setLanguage\",\"value\":\"auto\"}");
            string windows = Strings.Detect(System.Globalization.CultureInfo.CurrentUICulture);
            if (app.Prefs.Language != "auto" || Strings.Lang != windows) p += "auto : " + app.Prefs.Language + "/" + Strings.Lang + " ; ";
            app.SetLanguage(before);
            await Task.Delay(300);
            sim.PhaseJson = "\"None\""; await app.PollLcuOnce();
            await Task.Delay(1500);
            app.TestRestoreLanguageSetting(saved);
            LanguageDraftReport = before + " > " + other + " pendant la draft (panneaux gardés : " + during + "), " + (app.LanguageReloads - reloads) + " rechargement(s) après, bulle « " + balloon + " »";
            return p;
        }

        public static async void Run(CompanionApp app, Options o)
        {
            StringBuilder s = new StringBuilder();
            int fails = 0;
            Action<bool, string> t = (ok, text) => { if (!ok) fails++; s.AppendLine((ok ? "OK : " : "ECHEC : ") + text); };
            Func<Native.RECT> rectOfLeft = () => { Rectangle b = app.PanelFor("left").Bounds; return new Native.RECT(b.Left, b.Top, b.Right, b.Bottom); };
            Func<Native.RECT> rectOfRight = () => { Rectangle b = app.PanelFor("right").Bounds; return new Native.RECT(b.Left, b.Top, b.Right, b.Bottom); };
            FakeClient fake = null;
            Action<Evidence, int> feed = (ev, n) => { for (int i = 0; i < n; i++) app.StepPhase(fake != null && !fake.IsDisposed, false, ev, 1); };
            try
            {
                List<Monitor> mons = ClientWindow.Monitors();
                Monitor main = mons.FirstOrDefault(m => m.Primary) ?? mons[0];
                Monitor second = mons.FirstOrDefault(m => m != main);
                int cw = Math.Min(1000, main.Work.W - 900), ch = Math.Min(720, main.Work.H - 40);
                int cx = main.Work.Left + (main.Work.W - cw) / 2, cy = main.Work.Top + (main.Work.H - ch) / 2;

                await Task.Delay(300);
                t(!app.PanelsShown && app.CurrentPhase == Phase.ClientClosed, "1. Lancé sans client : aucun panneau, phase client fermé");
                t(app.PanelFor("left") == null, "1. Sans client : WebView2 n'est pas chargé");

                fake = new FakeClient();
                fake.SetBounds(cx, cy, cw, ch);
                fake.Show();
                await Task.Delay(100);
                app.Tracker.Refresh();
                feed(Evidence.NotDraft, 3);
                await Task.Delay(200);
                t(!app.PanelsShown && app.CurrentPhase == Phase.OutOfDraft, "2. Client sur l'accueil : aucun panneau");
                feed(Evidence.NotDraft, 5);
                t(!app.PanelsShown, "3. Client dans un lobby : aucun panneau");
                feed(Evidence.Unknown, 3);
                t(!app.PanelsShown && app.CurrentPhase == Phase.Uncertain, "Transition incertaine : aucun panneau");
                feed(Evidence.Draft, 1);
                t(!app.PanelsShown, "Une seule lecture positive ne suffit pas");
                feed(Evidence.NotDraft, 1);

                // Préversion : une autre DRAFT simulée (« DraftMid.exe ») doit être signalée au premier passage en draft.
                int balloonsBeforeDraft = app.BalloonCount;
                if (Edition.Prerelease) app.OtherDraftSource = () => new List<string> { "DraftMid.exe pid 1" };
                Stopwatch appear = Stopwatch.StartNew();
                feed(Evidence.Draft, 2);
                if (Edition.Prerelease) t(app.LastBalloonKey == "native.beta.otherPanels" && app.LastBalloon.Contains("DraftMid.exe") && app.BalloonCount == balloonsBeforeDraft + 1 && app.OtherDraft == "DraftMid.exe",
                    "Préversion : autre DRAFT ouverte signalée par une bulle au premier passage en draft (« " + app.LastBalloon + " »)");
                for (int i = 0; i < 200 && !(app.PanelFor("left") != null && app.PanelFor("left").Visible); i++) await Task.Delay(50);
                s.AppendLine("MESURE : panneaux visibles " + appear.ElapsedMilliseconds + " ms après la détection");
                await Task.Delay(300);
                t(app.PanelsShown && app.CurrentPhase == Phase.Draft, "4. Début de draft : panneaux affichés");
                string blockedBalloon = await app.TestUpdateFromMenu(new StubUpdateSource { Latest = new Version(99, 0, 0) });
                t(blockedBalloon == Strings.T("native.update.blockedDraft"), "Mise à jour depuis le menu pendant une draft : bulle « relance après » (" + blockedBalloon + ")");
                t(app.PanelFor("left").Visible && app.PanelFor("right").Visible, "4. Les deux panneaux sont visibles");
                Native.RECT c = app.Tracker.Rect, l = rectOfLeft(), r = rectOfRight();
                t(l.Right <= c.Left && r.Left >= c.Right && c.Left - l.Right <= 10 && r.Left - c.Right <= 10, "4. Panneaux juste de chaque côté, sans chevauchement (" + l + " | " + c + " | " + r + ")");
                t(l.Top == c.Top && l.H == c.H, "4. Hauteur et alignement sur le client");
                t(!PanelHasFocus(app), "Aucun vol de focus à l'apparition");
                // QA-6 : l'échec reste toujours visible, avec sa cause mesurée (code Win32 et autres instances DRAFT de la session).
                if (o.NoHotkey) t(!app.HotkeyCreated, "Raccourci laissé à l'autre version : aucune tentative");
                // Préversion : l'échec reste visible, mais distingué du conflit 1409 (aucune tentative, donc aucun conflit).
                else if (app.HotkeyLeftReason != null) t(false, "Raccourci Ctrl+Alt+D non enregistré : laissé volontairement à la 2.x par la préversion (raison « " + app.HotkeyLeftReason + " » ; autres instances DRAFT de la session : " + OtherInstances() + ") ; aucune tentative, donc pas de conflit 1409");
                else t(app.HotkeyRegistered, "Raccourci Ctrl+Alt+D enregistré" + (app.HotkeyRegistered ? "" : " (Win32 " + app.HotkeyError + (app.HotkeyError == HotkeyWindow.AlreadyRegistered ? " : déjà pris par un autre programme" : "") + " ; autres instances DRAFT de la session : " + OtherInstances() + ")"));

                feed(Evidence.NotDraft, 1); feed(Evidence.Draft, 1); feed(Evidence.Unknown, 2); feed(Evidence.NotDraft, 2); feed(Evidence.Draft, 1);
                t(app.PanelsShown, "Lectures fugitives : pas de clignotement");

                long movesBefore = app.MoveCount, evBefore = app.Tracker.EventCount;
                Stopwatch clock = Stopwatch.StartNew();
                int steps = 40;
                for (int i = 1; i <= steps; i++) { fake.Location = new Point(cx + (i % 2 == 0 ? 6 : -6) * i / 4, cy + i); await Task.Delay(8); }
                await Task.Delay(150);
                c = app.Tracker.Rect; l = rectOfLeft(); r = rectOfRight();
                t(l.Right <= c.Left && c.Left - l.Right <= 10 && r.Left >= c.Right && l.Top == c.Top, "7. Déplacement : les panneaux suivent (" + c + ")");
                s.AppendLine("MESURE : " + (app.Tracker.EventCount - evBefore) + " événements Windows, " + (app.MoveCount - movesBefore) + " déplacements de panneaux sur " + clock.ElapsedMilliseconds + " ms");
                // Une mise à jour par étape au plus, plus le recalcul à l'arrêt et la dernière mise à jour en attente.
                t(app.MoveCount - movesBefore <= steps + 3, "7. Déplacements limités à une mise à jour par étape");

                // Rafales plus rapides qu'une image : combien de temps les panneaux restent-ils en retard sur la dernière position ?
                {
                    double total = 0, worst = 0;
                    int missed = 0;
                    for (int b = 0; b < 40; b++)
                    {
                        await Task.Delay(30);
                        app.LastFollowLagMs = -1;
                        for (int k = 1; k <= 3; k++) fake.Location = new Point(cx + (b % 7) * 3 + k + 40 * (b % 2), cy + (b % 3));
                        await Task.Delay(60);
                        if (app.LastFollowLagMs < 0 || fake.Left - rectOfLeft().Right > 10 || fake.Left < rectOfLeft().Right) { missed++; continue; }
                        total += app.LastFollowLagMs;
                        worst = Math.Max(worst, app.LastFollowLagMs);
                    }
                    s.AppendLine("MESURE : rafales : du dernier événement au déplacement, moyenne " + (total / Math.Max(1, 40 - missed)).ToString("0.0") + " ms, maximum " + worst.ToString("0.0") + " ms (fenêtre simulée, sans le client réel ni l'affichage)");
                    t(missed == 0, "7. Rafales : les panneaux rattrapent toujours la dernière position");
                }

                // Retour sur le client : les panneaux repassent devant une autre fenêtre, sans prendre le focus.
                Form otherApp = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, Text = "Autre application" };
                otherApp.Bounds = app.PanelFor("left").Bounds;
                otherApp.Show();
                Native.SetWindowPos(otherApp.Handle, IntPtr.Zero, 0, 0, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOMOVE | Native.SWP_NOACTIVATE);
                await Task.Delay(50);
                bool behind = !IsAbove(app.PanelFor("left").Handle, otherApp.Handle);
                app.RaisePanels();
                await Task.Delay(50);
                t(behind && IsAbove(app.PanelFor("left").Handle, otherApp.Handle), "Retour sur le client : les panneaux repassent devant l'autre fenêtre");
                t(!PanelHasFocus(app), "Retour sur le client : aucun vol de focus");
                t((NativeHook.GetWindowLong(app.PanelFor("left").Handle, -20) & 0x8) == 0, "Panneaux non épinglés au-dessus des autres applications par défaut");
                otherApp.Close(); otherApp.Dispose();

                // Client large, comme un 1280 sur 1920 : la place disponible change à chaque pas.
                int ideal = (int)(340 * main.Scale), wide = main.Work.W - 2 * (ideal - 40), wx = main.Work.Left + (main.Work.W - wide) / 2;
                fake.SetBounds(wx, cy, wide, ch);
                await Task.Delay(500);
                int widthBefore = rectOfLeft().W, widthChanges = 0, gapErrors = 0;
                for (int i = 1; i <= 12; i++)
                {
                    fake.Location = new Point(wx - i * 3, cy);
                    await Task.Delay(30);
                    if (rectOfLeft().W != widthBefore) widthChanges++;
                    if (rectOfLeft().Right != app.Tracker.Rect.Left) gapErrors++;
                }
                t(widthChanges == 0, "Déplacement : les panneaux gardent leur taille, sans redessin à chaque pas (" + widthChanges + " changement(s))");
                t(gapErrors == 0, "Déplacement : le panneau reste collé au client à chaque pas (" + gapErrors + " écart(s))");
                await Task.Delay(400);
                c = app.Tracker.Rect; l = rectOfLeft(); r = rectOfRight();
                t(l.Right == c.Left && r.Left == c.Right && l.Inside(main.Work) && r.Inside(main.Work) && l.W < widthBefore, "À l'arrêt : la taille est recalculée et les panneaux restent sur l'écran (" + l + " | " + r + ")");
                fake.SetBounds(cx, cy, cw, ch);
                await Task.Delay(400);

                fake.SetBounds(cx - 60, cy - 10, cw - 120, ch - 20);
                await Task.Delay(200);
                c = app.Tracker.Rect; l = rectOfLeft(); r = rectOfRight();
                t(c.W == cw - 120 && l.H == c.H && r.H == c.H && l.Right <= c.Left && r.Left >= c.Right, "8. Redimensionnement : les panneaux s'adaptent (" + c + ")");

                // Plus assez de place pour deux panneaux : passage en panneau unique, sans couvrir le client.
                fake.SetBounds(main.Work.Left, cy, main.Work.W - (int)(300 * main.Scale), ch);
                for (int i = 0; i < 100 && !(app.ShownMode == "compact" && app.PanelFor("compact") != null && app.PanelFor("compact").Visible); i++) await Task.Delay(50);
                t(app.ShownMode == "compact" && app.PanelFor("compact") != null && app.PanelFor("compact").Visible && !app.PanelFor("left").Visible, "Place insuffisante : passage en panneau unique");
                Rectangle cb = app.PanelFor("compact").Bounds;
                t(cb.Left >= app.Tracker.Rect.Right, "Place insuffisante : le panneau ne couvre pas le client (" + cb + ")");
                fake.SetBounds(cx, cy, cw, ch);
                await Task.Delay(800);
                t(app.ShownMode == "two" && app.PanelFor("left").Visible, "Place retrouvée : retour à deux panneaux");

                if (second != null)
                {
                    int sw = Math.Min(1280, second.Work.W - 700), sh = Math.Min(720, second.Work.H - 40);
                    fake.SetBounds(second.Work.Left + (second.Work.W - sw) / 2, second.Work.Top + (second.Work.H - sh) / 2, sw, sh);
                    await Task.Delay(600);
                    c = app.Tracker.Rect; l = rectOfLeft(); r = rectOfRight();
                    bool inside = l.Inside(second.Work) && r.Inside(second.Work);
                    t(inside && l.Right <= c.Left && r.Left >= c.Right, "9. Écran secondaire à l'échelle " + second.Scale + " : panneaux sur le même écran (" + l + " | " + r + ")");
                    fake.SetBounds(cx, cy, cw, ch);
                    await Task.Delay(600);
                    c = app.Tracker.Rect; l = rectOfLeft();
                    t(l.Inside(main.Work) && l.Right <= c.Left, "9. Retour sur l'écran principal à l'échelle " + main.Scale + " (" + l + ")");
                }
                else s.AppendLine("NON TESTE : deux écrans, un seul présent.");

                fake.WindowState = FormWindowState.Minimized;
                await Task.Delay(250);
                t(!app.PanelsShown, "10. Client réduit : panneaux masqués");
                fake.WindowState = FormWindowState.Normal;
                fake.SetBounds(cx, cy, cw, ch);
                await Task.Delay(400);
                t(app.PanelsShown && app.PanelFor("left").Visible, "10. Client restauré pendant la draft : panneaux restaurés");
                await Task.Delay(200);

                app.ToggleManual();
                await Task.Delay(150);
                t(!app.PanelsShown, "12. Raccourci manuel : panneaux masqués pendant la draft");
                feed(Evidence.Draft, 3);
                t(!app.PanelsShown, "12. Le masquage manuel tient tant que la phase ne change pas");
                app.ToggleManual();
                await Task.Delay(200);
                t(app.PanelsShown, "12. Raccourci manuel : panneaux réaffichés");

                feed(Evidence.NotDraft, 3);
                await Task.Delay(150);
                t(!app.PanelsShown && app.CurrentPhase == Phase.OutOfDraft, "5. Fin de draft : panneaux masqués");

                Stopwatch warm = Stopwatch.StartNew();
                feed(Evidence.Draft, 2);
                for (int i = 0; i < 200 && !app.PanelFor("left").Visible; i++) await Task.Delay(5);
                s.AppendLine("MESURE : panneaux déjà préparés, visibles " + warm.ElapsedMilliseconds + " ms après la détection");
                await Task.Delay(150);
                t(app.PanelsShown, "Nouvelle draft : panneaux affichés");
                feed(Evidence.NotDraft, 3); await Task.Delay(150);
                t(!app.PanelsShown, "6. Draft annulée : panneaux masqués");

                app.ToggleManual();
                await Task.Delay(250);
                t(app.PanelsShown, "12. Affichage manuel hors draft");
                feed(Evidence.Unknown, 1); await Task.Delay(100);
                t(!app.PanelsShown, "12. L'affichage manuel se termine au changement de phase");

                feed(Evidence.Draft, 2); await Task.Delay(150);
                t(app.PanelsShown, "Draft relancée");
                app.StepPhase(true, true, Evidence.Unknown, 1); await Task.Delay(100);
                t(!app.PanelsShown && app.CurrentPhase == Phase.InGame, "Partie en cours : masquage immédiat");
                app.StepPhase(true, true, Evidence.Draft, 1);
                t(!app.PanelsShown, "Partie en cours : aucune réapparition");
                app.StepPhase(true, false, Evidence.NotDraft, 1);

                app.SetAutoDraft(false);
                feed(Evidence.Draft, 3); await Task.Delay(100);
                t(!app.PanelsShown, "Activation automatique désactivée : pas d'affichage");
                app.SetAutoDraft(true); await Task.Delay(200);
                t(app.PanelsShown, "Activation automatique réactivée pendant la draft");

                fake.Close(); fake.Dispose(); fake = null;
                await Task.Delay(400);
                app.Tracker.Refresh();
                app.StepPhase(false, false, Evidence.Unknown, 1);
                await Task.Delay(100);
                t(!app.PanelsShown && app.CurrentPhase == Phase.ClientClosed, "11. Client fermé : panneaux masqués");

                fake = new FakeClient();
                fake.SetBounds(cx, cy, cw, ch);
                fake.Show();
                await Task.Delay(150);
                app.Tracker.Refresh();
                feed(Evidence.NotDraft, 2);
                t(!app.PanelsShown, "11. Client relancé : aucun panneau hors draft");
                feed(Evidence.Draft, 2); await Task.Delay(300);
                c = app.Tracker.Rect; l = rectOfLeft();
                t(app.PanelsShown && l.Right <= c.Left, "11. Client relancé puis draft : panneaux replacés");
                t(!PanelHasFocus(app), "Aucun vol de focus sur tout le scénario");

                // Connecteur LCU sur données simulées : aucune connexion au vrai client.
                feed(Evidence.NotDraft, 3); await Task.Delay(150);
                SimulatedLcuTransport sim = new SimulatedLcuTransport { PhaseJson = "\"Lobby\"" };
                app.TestUseLcu(() => sim);
                await app.PollLcuOnce(); await Task.Delay(100);
                t(!app.PanelsShown && app.CurrentPhase == Phase.OutOfDraft, "LCU simulé : lobby, aucun panneau");
                sim.PhaseJson = "\"ChampSelect\"";
                sim.SessionJson = "{\"theirTeam\":[{\"championId\":875},{\"championId\":254,\"championPickIntent\":0},{\"championId\":3},{\"championId\":0,\"championPickIntent\":157}]}";
                await app.PollLcuOnce();
                t(app.PanelsShown && app.CurrentPhase == Phase.Draft, "LCU simulé : ChampSelect affiche les panneaux dès la première lecture");
                PanelForm rp = app.PanelFor("right");
                Func<string, Task<string>> js = async code => await rp.View.CoreWebView2.ExecuteScriptAsync(code);
                for (int i = 0; i < 100 && await js("typeof V !== \"undefined\" && !!document.querySelector('[data-test=\"panel\"]')") != "true"; i++) await Task.Delay(50);
                await Task.Delay(400);
                await app.PollLcuOnce();
                string enemies = "";
                for (int i = 0; i < 40 && !enemies.Contains("Galio"); i++) { await Task.Delay(50); enemies = await js("state.enemies.join()"); }
                t(enemies.Contains("Sett") && enemies.Contains("Vi") && enemies.Contains("Galio") && !enemies.Contains("Xerath"), "LCU simulé : champions reçus, intention de pick ignorée (" + enemies + ")");
                await js("addEnemy(\"Janna\"); setMid(\"Sett\"); 1");
                await Task.Delay(100);
                sim.SessionJson = "{\"theirTeam\":[{\"championId\":875},{\"championId\":254},{\"championId\":3},{\"championId\":523}]}";
                await app.PollLcuOnce();
                await Task.Delay(300);
                enemies = await js("state.enemies.join()");
                string mid = await js("currentMid()");
                t(enemies.Contains("Janna") && enemies.Contains("Aphelios") && mid.Contains("Sett"), "LCU simulé : corrections manuelles conservées (" + enemies + ", mid " + mid + ")");
                sim.SessionJson = "{\"theirTeam\":[{\"championId\":875},{\"championId\":3},{\"championId\":523}]}";
                await app.PollLcuOnce();
                await Task.Delay(300);
                enemies = await js("state.enemies.join()");
                t(!enemies.Contains("Vi,") && !enemies.EndsWith("Vi\"") && enemies.Contains("Janna"), "LCU simulé : un champion détecté puis disparu est retiré (" + enemies + ")");
                sim.Offline = true;
                bool answered = await app.PollLcuOnce();
                t(!answered && app.PanelsShown, "LCU simulé : client injoignable, repli sans masquer la draft");
                sim.Offline = false;
                sim.PhaseJson = "\"Lobby\"";
                await app.PollLcuOnce(); await Task.Delay(100);
                t(!app.PanelsShown, "LCU simulé : draft annulée, panneaux masqués immédiatement");
                sim.PhaseJson = "\"ChampSelect\"";
                await app.PollLcuOnce(); await Task.Delay(100);
                sim.PhaseJson = "\"InProgress\"";
                await app.PollLcuOnce(); await Task.Delay(100);
                t(!app.PanelsShown && app.CurrentPhase == Phase.InGame, "LCU simulé : partie lancée, panneaux masqués");
                t(sim.Requests.All(LcuEndpoints.IsAllowed), "LCU simulé : seuls les deux endpoints documentés sont appelés");

                await RunLcuScenario(app, t, js);
                string roleProblem = await TestAssignedRoleThroughLcu(app);
                t(roleProblem == "", "QA A2. Rôle attribué par League (LCU simulé, de bout en bout) : 5 postes, poste vide ou inconnu, changement entre deux drafts, autoRole, sans LCU ; jamais déduit du champion (" + RoleReport + ") " + roleProblem);
                string langDraftProblem = await TestLanguageDuringDraft(app);
                t(langDraftProblem == "", "QA A1. Langue changée pendant une draft : panneaux gardés jusqu'à la fin, draft suivante dans la nouvelle langue, menu et bulle traduits, valeur hors liste refusée, auto (" + LanguageDraftReport + ") " + langDraftProblem);
                // En dernier : recharger les pages efface l'état des panneaux, que les étapes précédentes vérifient.
                string languageProblem = await app.TestLanguageSwitch();
                t(languageProblem == "", "Langue : changement sans redémarrage, menu, fenêtre et panneaux (" + app.LanguageReport + ") " + languageProblem);
            }
            catch (Exception e) { fails++; s.AppendLine("EXC " + e); }
            if (fake != null && !fake.IsDisposed) { fake.Close(); fake.Dispose(); }
            s.AppendLine("MESURE : recherche de champions : " + s_keyReport);
            List<string> missing; lock (Strings.Missing) missing = Strings.Missing.ToList();
            t(missing.Count == 0, "Langue " + Strings.Lang + " : aucune clé du programme absente pendant le scénario" + (missing.Count == 0 ? "" : " (" + string.Join(" | ", missing) + ")"));
            s.AppendLine(fails == 0 ? "TOUT EST BON" : fails + " ECHEC(S)");
            if (o.Out != null) File.WriteAllText(o.Out, s.ToString());
            app.Quit();
        }
    }

// WebView2 est fourni par Windows 11 et la plupart des Windows 10, mais il peut manquer.
    public static class RuntimeHelp
    {
        public const string DownloadUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";
        static bool loaderSet;

        public static void EnsureLoader(string webDir)
        {
            // WebView2 n'accepte cet appel qu'une fois par processus.
            if (loaderSet) return;
            CoreWebView2Environment.SetLoaderDllFolderPath(webDir);
            loaderSet = true;
        }

        // Version installée, ou null si le composant est absent.
        public static string Version(string webDir)
        {
            try
            {
                EnsureLoader(webDir);
                return CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (WebView2RuntimeNotFoundException) { return null; }
        }

        public static void Explain()
        {
            DialogResult r = MessageBox.Show(Strings.T("native.dialog.webView2Missing"), "DRAFT", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;
            try { Process.Start(new ProcessStartInfo(DownloadUrl) { UseShellExecute = true }); } catch { }
        }
    }

    public class Options
    {
        public string Script, CaptureDir, Out, DataDir, InstallDir;
        public bool NoHotkey, NoAutoPanels;
        public string ReportState;
        public bool Silent, SelfTest, Install, Uninstall, TestInstall, EndToEnd, SimulatedPhase;
        public int AutoExit, SettleMs = 2500, Soak;
        public bool CheckRuntime, OpggPrototype, OpggTest, OpggLive;
        public string RunKey, TestUpdate;
        // Préversion : --legacy-data <dossier> remplace la source de la copie initiale (essais : jamais le vrai dossier 2.x).
        public string LegacyData;
        // --lang fr|en : langue imposée pour un essai (tests), sans toucher au réglage enregistré.
        public string Lang;
        public int AfterUpdate;
        public bool TestUpdateLive, UpdateRolledBack;
        public string[] TestSignature;

        public static Options Parse(string[] a)
        {
            Options o = new Options();
            for (int i = 0; i < a.Length; i++)
            {
                Func<string> next = () => ++i < a.Length ? a[i] : "";
                switch (a[i])
                {
                    case "--script": o.Script = next(); o.Silent = true; break;
                    case "--capture": o.CaptureDir = next(); break;
                    case "--out": o.Out = next(); break;
                    case "--data": o.DataDir = next(); break;
                    case "--legacy-data": o.LegacyData = next(); break;
                    case "--auto-exit": o.AutoExit = int.Parse(next()); o.Silent = true; break;
                    case "--settle": o.SettleMs = int.Parse(next()); break;
                    case "--selftest": o.SelfTest = true; o.Silent = true; break;
#if DRAFTMID_PROTOTYPES
                    case "--opgg-prototype": o.OpggPrototype = true; break;
                    case "--opgg-test": o.OpggTest = true; o.Silent = true; break;
                    case "--opgg-live": o.OpggLive = true; break;
#endif
                    case "--check-runtime": o.CheckRuntime = true; o.Silent = true; break;
                    case "--soak": o.Soak = int.Parse(next()); o.EndToEnd = true; o.SimulatedPhase = true; o.Silent = true; break;
                    case "--e2e": o.EndToEnd = true; o.SimulatedPhase = true; o.Silent = true; break;
                    case "--install": o.Install = true; break;
                    case "--uninstall": o.Uninstall = true; break;
                    case "--install-dir": o.InstallDir = next(); break;
                    case "--test-install": o.TestInstall = true; o.Silent = true; break;
                    case "--run-key": o.RunKey = next(); break;
                    case "--no-hotkey": o.NoHotkey = true; break;
                    case "--lang": o.Lang = next(); break;
                    case "--report-state": o.ReportState = next(); o.Silent = true; break;
                    case "--no-auto-panels": o.NoAutoPanels = true; break;
                    case "--after-update": o.AfterUpdate = int.Parse(next()); break;
                    case "--test-update": o.TestUpdate = next(); o.Silent = true; break;
                    case "--test-update-live": o.TestUpdateLive = true; o.Silent = true; break;
                    case "--update-rolled-back": o.UpdateRolledBack = true; break;
                    case "--test-signature": o.TestSignature = new[] { next(), next(), next() }; o.Silent = true; break;
                }
            }
            return o;
        }
    }

    static class Installer
    {
        const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DraftMid";
        public static string DefaultDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "DraftMid"); } }

        const string TestUninstallKey = @"Software\DraftMid-SelfTest\Uninstall";
        public const string ExeName = "DRAFT.exe", OldExeName = "DraftMid.exe";
        static string OldLinkPath(Options o, string dir) { return o.TestInstall ? Path.Combine(dir, "DraftMid.lnk") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "DraftMid.lnk"); }
        static string LinkPath(Options o, string dir) { return o.TestInstall ? Path.Combine(dir, "DRAFT.lnk") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "DRAFT.lnk"); }

        public static void Install(Options o, string self)
        {
            string dir = o.InstallDir ?? DefaultDir;
            Directory.CreateDirectory(dir);
            string target = Path.Combine(dir, ExeName), old = Path.Combine(dir, OldExeName);
            File.Copy(self, target, true);
            // Mise à jour depuis DraftMid : un seul exécutable, un seul raccourci, et le démarrage automatique suit le nouveau fichier.
            Autostart.Repoint(old, target);
            try { if (File.Exists(old) && !string.Equals(Path.GetFullPath(old), Path.GetFullPath(self), StringComparison.OrdinalIgnoreCase)) File.Delete(old); } catch { }
            try { File.Delete(OldLinkPath(o, dir)); } catch { }
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            dynamic link = shell.CreateShortcut(LinkPath(o, dir));
            link.TargetPath = target;
            link.WorkingDirectory = dir;
            link.Description = "DRAFT";
            link.Save();
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(o.TestInstall ? TestUninstallKey : UninstallKey))
            {
                k.SetValue("DisplayName", "DRAFT");
                k.SetValue("DisplayVersion", Edition.Version);
                k.SetValue("Publisher", "DRAFT");
                k.SetValue("DisplayIcon", target);
                k.SetValue("InstallLocation", dir);
                k.SetValue("UninstallString", "\"" + target + "\" --uninstall");
                k.SetValue("NoModify", 1); k.SetValue("NoRepair", 1);
            }
            if (o.TestInstall) return;
            MessageBox.Show(Strings.T("native.dialog.installed"), "DRAFT");
            Process.Start(new ProcessStartInfo(target) { WorkingDirectory = dir, UseShellExecute = true });
        }

        public static void Uninstall(Options o, string self)
        {
            string dir = o.InstallDir ?? DefaultDir;
            if (!o.TestInstall && MessageBox.Show(Strings.T("native.dialog.uninstall"), "DRAFT", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try { File.Delete(LinkPath(o, dir)); } catch { }
            try { File.Delete(OldLinkPath(o, dir)); } catch { }
            if (o.TestInstall) Registry.CurrentUser.DeleteSubKeyTree(@"Software\DraftMid-SelfTest", false);
            else { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); Autostart.Set(false, self); }
            try { string old = Path.Combine(dir, OldExeName); if (File.Exists(old) && !string.Equals(Path.GetFullPath(old), Path.GetFullPath(self), StringComparison.OrdinalIgnoreCase)) File.Delete(old); } catch { }
            string target = Path.Combine(dir, ExeName);
            if (!File.Exists(target) && File.Exists(Path.Combine(dir, OldExeName))) target = Path.Combine(dir, OldExeName);
            if (string.Equals(Path.GetFullPath(self), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                // Un exe ne peut pas se supprimer lui-même pendant qu'il tourne.
                Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 3 > nul & del /q \"" + target + "\" & rmdir \"" + dir + "\"") { CreateNoWindow = true, UseShellExecute = false });
            else if (File.Exists(target)) { File.Delete(target); try { Directory.Delete(dir); } catch { } }
        }
    }

    // Images synthétiques : elles testent la logique, PAS la reconnaissance du vrai client.
    static class DetectionTests
    {
        static Frame Make(Func<int, int, int[]> px)
        {
            Frame f = new Frame();
            for (int y = 0; y < Frame.H; y++) for (int x = 0; x < Frame.W; x++)
            {
                int[] c = px(x, y); int i = (y * Frame.W + x) * 3;
                f.Rgb[i] = (byte)c[0]; f.Rgb[i + 1] = (byte)c[1]; f.Rgb[i + 2] = (byte)c[2];
            }
            return f;
        }
        static int[] Nav(int x, int y) { return (x / 5) % 2 == 0 ? new[] { 200, 170, 90 } : new[] { 20, 30, 40 }; }
        static int[] Lattice(int x, int y) { return (x / 4 + y / 4) % 2 == 0 ? new[] { 220, 60, 60 } : new[] { 30, 90, 200 }; }

        public static void Run(Action<bool, string> t, StringBuilder s, string dir)
        {
            Frame home = Make((x, y) => y < 6 ? Nav(x, y) : new[] { 15, 20, 28 });
            Frame lobby = Make((x, y) => y < 6 ? Nav(x, y) : (x > 20 && x < 76 && y > 10 && y < 44 ? Lattice(x, y) : new[] { 15, 20, 28 }));
            Frame draft = Make((x, y) => y < 6 ? new[] { 10, 14, 20 } : (x > 18 && x < 78 && y > 8 && y < 46 ? Lattice(x, y) : new[] { 12, 16, 22 }));
            Frame loading = Make((x, y) => new[] { 3, 3, 4 });
            Frame draftTitled = Make((x, y) => y < 6 ? ((x / 9) % 2 == 0 ? new[] { 190, 150, 70 } : new[] { 12, 16, 22 }) : (x > 18 && x < 78 && y > 8 && y < 46 ? Lattice(x, y) : new[] { 12, 16, 22 }));

            UserPreferences p = new UserPreferences();
            DraftPhaseDetector d = new DraftPhaseDetector(p);
            t(d.Classify(loading) == Evidence.NotDraft, "Détecteur sans référence : un écran noir n'est pas une sélection");
            for (int i = 0; i < 12; i++) d.Classify(home);
            t(p.NavBaseline != null, "Détecteur : la barre stable est apprise seule");
            t(d.Classify(home) == Evidence.NotDraft, "Détecteur : accueil reconnu hors draft");
            t(d.Classify(lobby) == Evidence.NotDraft, "Détecteur : un centre chargé avec la barre reste hors draft");
            t(d.Classify(draft) == Evidence.Draft, "Détecteur : barre absente et centre chargé donnent une draft");
            t(d.Classify(loading) == Evidence.NotDraft, "Détecteur : écran de chargement sans panneau");

            // Comme sur le vrai client : même haut et même bas que la sélection, mais aucun portrait au centre.
            Frame salon = Make((x, y) => y < 6 ? new[] { 10, 14, 20 } : new[] { 12, 16, 22 });
            UserPreferences lp = new UserPreferences();
            DraftPhaseDetector ld = new DraftPhaseDetector(lp);
            t(!ld.Teach(salon, true) && lp.DraftSamples.Count == 0, "Un salon n'est pas retenu comme une draft");
            lp.DraftSamples.Add(DraftPhaseDetector.Signature(salon));
            t(ld.Classify(salon) == Evidence.NotDraft, "Réglages existants : un salon appris par erreur n'affiche plus les panneaux");
            t(ld.Classify(draft) == Evidence.Draft, "Réglages existants : la sélection reste reconnue dès le premier passage");

            UserPreferences q = new UserPreferences();
            DraftPhaseDetector dq = new DraftPhaseDetector(q);
            for (int i = 0; i < 12; i++) dq.Classify(draftTitled);
            t(q.NavBaseline == null && dq.Classify(draftTitled) == Evidence.Unknown, "Lancé en pleine draft : aucune mauvaise référence n'est apprise");
            dq.Teach(home, false);
            t(dq.Classify(home) == Evidence.NotDraft, "Une correction manuelle passe avant la règle");
            dq.Teach(draft, true);
            t(dq.Classify(draft) == Evidence.Draft, "Une draft enseignée est reconnue ensuite");
            dq.Teach(draft, false);
            t(q.DraftSamples.Count == 0 && dq.Classify(draft) == Evidence.NotDraft, "La dernière correction remplace la correction contraire sur le même écran");
            dq.Teach(draft, true);
            t(q.OtherSamples.All(x => DraftPhaseDetector.Dist(x, DraftPhaseDetector.Signature(draft)) >= 3) && dq.Classify(draft) == Evidence.Draft, "Reconfirmer une draft annule la correction contraire");
            int[] sig = DraftPhaseDetector.Signature(draft);
            t(sig.Length <= 60, "La signature retenue ne fait que " + sig.Length + " valeurs : aucune image conservée");
            dq.ForgetBaseline();
            t(q.NavBaseline == null, "Une référence douteuse peut être oubliée");

            PhaseMachine m = new PhaseMachine();
            t(m.Step(false, false, Evidence.Unknown, 1) == Phase.ClientClosed, "Machine : client fermé");
            t(m.Step(true, false, Evidence.Unknown, 1) == Phase.Uncertain, "Machine : client ouvert n'est jamais une draft");
            t(m.Step(true, false, Evidence.NotDraft, 1) == Phase.OutOfDraft, "Machine : hors draft");
            t(m.Step(true, false, Evidence.Draft, 1) != Phase.Draft, "Machine : une lecture ne suffit pas");
            t(m.Step(true, false, Evidence.Draft, 1) == Phase.Draft, "Machine : deux lectures entrent en draft");
            t(m.Step(true, false, Evidence.NotDraft, 1) == Phase.Draft && m.Step(true, false, Evidence.Unknown, 1) == Phase.Draft, "Machine : pas de sortie sur une lecture isolée");
            m.Step(true, false, Evidence.NotDraft, 1); m.Step(true, false, Evidence.NotDraft, 1);
            t(m.Step(true, false, Evidence.NotDraft, 1) == Phase.OutOfDraft, "Machine : trois lectures quittent la draft");
            m.Step(true, false, Evidence.Draft, 1); m.Step(true, false, Evidence.Draft, 1);
            t(m.Step(true, true, Evidence.Draft, 1) == Phase.InGame, "Machine : la partie passe immédiatement avant tout");
            t(m.Step(false, false, Evidence.Draft, 1) == Phase.ClientClosed, "Machine : la fermeture est immédiate");
            m.Step(true, false, Evidence.Draft, 1); m.Step(true, false, Evidence.Draft, 1);
            m.Step(true, false, Evidence.Draft, PhaseMachine.MaxDraftSeconds + 1);
            t(m.DraftTimedOut && m.Current == Phase.Uncertain, "Machine : une draft trop longue redevient incertaine");

            PanelVisibilityController v = new PanelVisibilityController();
            t(!v.ShouldShow(Phase.ClientClosed, false, true), "Affichage : rien au lancement");
            t(!v.ShouldShow(Phase.OutOfDraft, true, true), "Affichage : rien hors draft");
            t(v.ShouldShow(Phase.Draft, true, true), "Affichage : draft et client visible");
            t(!v.ShouldShow(Phase.Draft, false, true), "Affichage : client réduit");
            t(!v.ShouldShow(Phase.Draft, true, false), "Affichage : automatique désactivé");
            t(!v.ShouldShow(Phase.InGame, true, true), "Affichage : partie en cours");
            v.ShouldShow(Phase.OutOfDraft, true, true); v.Manual = true;
            t(v.ShouldShow(Phase.OutOfDraft, true, true), "Affichage : le manuel force l'affichage");
            t(!v.ShouldShow(Phase.Uncertain, true, true), "Affichage : le manuel s'arrête au changement de phase");

            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++) d.Classify(lobby);
            s.AppendLine("MESURE : analyse d'une image réduite " + (w.Elapsed.TotalMilliseconds / 2000).ToString("0.000") + " ms");
            using (FrameGrabber g = new FrameGrabber())
            {
                Frame fr = new Frame();
                Stopwatch w2 = Stopwatch.StartNew();
                for (int i = 0; i < 20; i++) g.Grab(new Native.RECT(0, 0, 800, 450), fr);
                s.AppendLine("MESURE : capture réduite d'une zone d'écran " + (w2.Elapsed.TotalMilliseconds / 20).ToString("0.00") + " ms");
            }
        }
    }

    // I18N-3 : textes du programme (src/Strings.cs). Catalogue intégré, repli, catalogue piégé, détection, réglage, pluriels.
    static class StringsTests
    {
        public static void Run(Action<bool, string> t, string dataDir)
        {
            string start = Strings.Lang;
            bool test = Strings.TestMode;
            Strings.TestMode = true;
            t(Strings.Load("fr", Strings.Embedded) && Strings.Lang == "fr" && Strings.Has("fr", "native.menu.open") && Strings.Has("en", "native.menu.open"), "Langue : catalogues français et anglais du programme chargés");
            int missingKeys = Strings.Keys.Count(k => k.StartsWith("native.") && !Strings.Has("en", k));
            t(missingKeys == 0, "Langue : chaque clé native.* existe en anglais (" + missingKeys + " absente(s))");
            t(MenuText.Open == "Ouvrir DRAFT" && Strings.T("native.update.downloading", "percent", 45) == "Téléchargement : 45 %", "Langue : textes français du programme identiques à avant");
            Strings.Use("en");
            t(Strings.Lang == "en" && MenuText.Open == "Open DRAFT" && Strings.T("native.update.downloading", "percent", 45) == "Downloading: 45%", "Langue : textes anglais du programme");
            t(new Msg("native.history.summary", "count", 1, "total", 3).ToString() == "1 game added, 3 in total." && new Msg("native.history.summary", "count", 0, "total", 3).ToString() == "0 games added, 3 in total.", "Langue : pluriel anglais (0 au pluriel)");
            t(new Msg("native.history.done", "summary", new Msg("native.history.summary", "count", 2, "total", 5)).ToString() == "Import complete: 2 games added, 5 in total.", "Langue : message dans un message");
            t(new Msg("native.history.progress", "done", 1, "count", 1).ToString() == "Importing history: 1 of 1 game" && new Msg("native.roles.savedMatchups", "games", 200, "count", 1).ToString() == "Matchups updated: 200 games, 1 champion." && new Msg("native.history.rateLimit", "count", 1).ToString() == "Riot rate limit reached: resuming in 1 second. Nothing is lost.", "Langue : singulier anglais des états du programme");
            Strings.Use("fr");
            t(new Msg("native.history.done", "summary", new Msg("native.history.summary", "count", 0, "total", 5)).ToString() == "Import terminé : 0 partie ajoutée, 5 au total.", "Langue : pluriel français (0 au singulier), texte identique à avant");
            t(new Msg("native.history.rateLimit", "count", 1).ToString() == "Limite de requêtes de Riot atteinte : reprise dans 1 seconde. Rien n'est perdu." && new Msg("native.roles.stoppedFew", "count", 1).ToString() == "Calcul arrêté : 1 partie, trop peu pour remplacer les données actuelles." && new Msg("native.history.progress", "done", 40, "count", 120).ToString() == "Import de l'historique : 40 sur 120 parties", "Langue : singulier français des états du programme, pluriel inchangé");
            Dictionary<string, object> json = new Msg("native.history.done", "summary", new Msg("native.history.summary", "count", 2, "total", 5)).Json();
            t((string)json["key"] == "native.history.done" && ((Dictionary<string, object>)json["vars"])["summary"] is Dictionary<string, object>, "Langue : un état est envoyé à la page en { key, vars }");
            // Repli : clé absente en anglais -> français, signalé ; absente partout -> [clé].
            Strings.Missing.Clear();
            Strings.Load("en", code => code == "en" ? "window.DRAFT_I18N_EN = {\"native.menu.quit\":\"Quit\"};" : Strings.Embedded(code));
            t(Strings.Lang == "en" && Strings.T("native.menu.quit") == "Quit" && Strings.T("native.menu.open") == "Ouvrir DRAFT" && Strings.Missing.Contains("clé absente (en) : native.menu.open"), "Langue : clé absente en anglais, repli sur le français signalé en mode test");
            t(Strings.T("inconnue.cle.test") == "[inconnue.cle.test]" && Strings.Missing.Contains("clé absente (fr) : inconnue.cle.test"), "Langue : clé absente partout, [clé] visible et signalée");
            // Catalogue piégé : refusé, tout le programme reste en français.
            Strings.Missing.Clear();
            Strings.Load("en", code => code == "en" ? "window.DRAFT_I18N_EN = {\"native.menu.quit\":\"Quit\"}; fetch('https://exemple.test');" : Strings.Embedded(code));
            t(Strings.Lang == "fr" && Strings.T("native.menu.quit") == "Quitter" && Strings.Missing.Any(m => m.StartsWith("catalogue en refusé")), "Langue : catalogue anglais piégé refusé, repli sur le français");
            Strings.Load("en", code => code == "en" ? "window.DRAFT_I18N_FR = {\"native.menu.quit\":\"Quit\"};" : Strings.Embedded(code));
            t(Strings.Lang == "fr", "Langue : catalogue anglais qui se déclare français refusé");
            Strings.Missing.Clear();
            Strings.Load("fr", Strings.Embedded);
            // Détection : langue de Windows au premier lancement.
            Func<string, string> detect = name => Strings.Detect(new System.Globalization.CultureInfo(name));
            t(detect("fr-FR") == "fr" && detect("fr-CA") == "fr" && detect("fr-BE") == "fr" && detect("en-US") == "en" && detect("de-DE") == "en" && Strings.Detect(System.Globalization.CultureInfo.InvariantCulture) == "en", "Langue : détection (fr-* : français, sinon anglais)");
            System.Globalization.CultureInfo fr = new System.Globalization.CultureInfo("fr-FR"), us = new System.Globalization.CultureInfo("en-US");
            t(Strings.Resolve(null, fr) == "fr" && Strings.Resolve("auto", us) == "en" && Strings.Resolve("xx", fr) == "fr" && Strings.Resolve("en", fr) == "en" && Strings.Resolve("fr", us) == "fr", "Langue : réglage absent ou auto = détection, fr/en prioritaire, valeur inconnue = détection");
            t(Strings.NormalizeSetting("en") == "en" && Strings.NormalizeSetting("xx") == "auto" && Strings.NormalizeSetting(null) == "auto", "Langue : seules auto, fr et en sont enregistrées");
            // Réglages : une installation 2.x (sans Language) reste lisible ; le champ est relu à l'identique.
            string sp = Path.Combine(dataDir, "selftest-langue.json");
            File.WriteAllText(sp, "{\"Compact\":true,\"Follow\":false,\"LcuEnabled\":true,\"ProfileKnown\":true}");
            UserPreferences old = UserPreferences.Load(sp);
            t(old.Compact && !old.Follow && old.LcuEnabled && old.Language == null && Strings.Resolve(old.Language, fr) == "fr", "Langue : réglages 2.x sans Language relus, langue détectée");
            old.Language = "en"; old.Save(sp);
            UserPreferences back = UserPreferences.Load(sp);
            t(back.Language == "en" && back.Compact && back.LcuEnabled, "Langue : réglage enregistré et relu à l'identique, autres réglages conservés");
            File.Delete(sp);
            // QA 3.0 : langue au démarrage, telle que la lit un vrai lancement (sans --selftest ni --e2e), d'après settings.json.
            string ld = Path.Combine(dataDir, "selftest-langue-dossier");
            Directory.CreateDirectory(ld);
            string lp = Path.Combine(ld, "settings.json"), windows = Strings.Detect(System.Globalization.CultureInfo.CurrentUICulture);
            Func<string, Options, string> startWith = (content, opt) => { if (content == null) { if (File.Exists(lp)) File.Delete(lp); } else File.WriteAllText(lp, content); return Program.InitialLanguage(opt ?? new Options(), ld); };
            string sEn = startWith("{\"Language\":\"en\",\"Compact\":true}", null), sFr = startWith("{\"Language\":\"fr\"}", null), sAuto = startWith("{\"Language\":\"auto\"}", null);
            string s2x = startWith("{\"Compact\":true,\"Follow\":false,\"ProfileKnown\":true}", null), sBad = startWith("pas du json", null), sNone = startWith(null, null), sOdd = startWith("{\"Language\":\"de\"}", null);
            string sForced = startWith("{\"Language\":\"fr\"}", new Options { Lang = "en" }), sForcedBad = startWith("{\"Language\":\"en\"}", new Options { Lang = "xx" });
            t(sEn == "en" && sFr == "fr" && sAuto == windows && s2x == windows && sBad == windows && sNone == windows && sOdd == windows && sForced == "en" && sForcedBad == "en",
                "Langue au démarrage : réglage relu après redémarrage (en " + sEn + ", fr " + sFr + "), auto, 2.x sans Language, fichier abîmé, absent, valeur inconnue : langue de Windows (" + windows + ") ; --lang prioritaire, --lang inconnu ignoré");
            Directory.Delete(ld, true);
            // Pluriels : mêmes catégories que Intl.PluralRules (relevées dans Chrome) pour 15 nombres et 2 langues.
            double[] numbers = { 0, 1, 1.5, 2, 3, 5, 10, 21, 100, 101, 1000, 0.5, 2.5, 1000000, 2000000 };
            string[] frWant = { "one", "one", "one", "other", "other", "other", "other", "other", "other", "other", "other", "one", "other", "many", "many" };
            string[] enWant = { "other", "one", "other", "other", "other", "other", "other", "other", "other", "other", "other", "other", "other", "other", "other" };
            int diff = 0;
            for (int i = 0; i < numbers.Length; i++) { if (Strings.PluralCategory("fr", numbers[i]) != frWant[i]) diff++; if (Strings.PluralCategory("en", numbers[i]) != enWant[i]) diff++; }
            t(diff == 0, "Langue : pluriels identiques à Intl.PluralRules (30 cas, " + diff + " différence(s))");
            Strings.Use(start);
            Strings.TestMode = test;
        }
    }

    // Édition (stable ou préversion) : noms séparés, copie initiale en lecture seule, raccourci, installation refusée.
    // Tout se passe dans le dossier de test et sur la clé de registre de test : ni le vrai dossier 2.x, ni la vraie valeur Run.
    static class EditionTests
    {
        static Dictionary<string, string> Snapshot(string dir)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                using (FileStream s = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    d[f.Substring(dir.Length + 1)] = BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(s)) + "|" + File.GetLastWriteTimeUtc(f).Ticks + "|" + File.GetCreationTimeUtc(f).Ticks;
            foreach (string sub in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)) d[sub.Substring(dir.Length + 1) + "\\"] = "dossier";
            return d;
        }

        public static void Run(Action<bool, string> t, string dataDir, string self)
        {
            string v = Edition.Version;
            t(Edition.IsPrerelease("3.0.0-beta.1") && !Edition.IsPrerelease("3.0.0") && !Edition.IsPrerelease("2.16.2") && !Edition.IsPrerelease(null), "Édition : un « - » dans la version signifie préversion (3.0.0-beta.1 oui ; 3.0.0, 2.16.2 non)");
            Version file = new Version(FileVersionInfo.GetVersionInfo(self).FileVersion);
            t(v.StartsWith(file.ToString(3)) && Edition.Prerelease == Edition.IsPrerelease(v) && FileVersionInfo.GetVersionInfo(self).ProductVersion == v, "Édition : version " + v + " (fichier " + file + "), " + (Edition.Prerelease ? "préversion" : "stable"));
            string page;
            using (Stream res = typeof(EditionTests).Assembly.GetManifestResourceStream("web/index.html"))
            using (StreamReader r = new StreamReader(res, Encoding.UTF8)) page = r.ReadToEnd();
            string appVersion = Regex.Match(page, "const APP_VERSION = \"([^\"]+)\";").Groups[1].Value;
            t(appVersion == v, "Édition : APP_VERSION de la page (" + appVersion + ") égale à la version de l'exe (" + v + ")");
            // Stable : exactement les noms de la 2.x. Préversion : des noms à elle.
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            t(Edition.DefaultDataDir(false) == Path.Combine(local, "DraftMid") && Edition.MutexName(false, null) == "DraftMid.Instance" && Edition.MutexName(false, @"C:\essai") == "DraftMid.Test." + @"C:\essai".GetHashCode() && Edition.RunValue(false) == "DraftMid",
                "Édition stable : dossier %LOCALAPPDATA%\\DraftMid, Mutex DraftMid.Instance (DraftMid.Test.* avec --data), valeur Run DraftMid : inchangés");
            t(Edition.DefaultDataDir(true) == Path.Combine(local, "DRAFT-beta") && Edition.MutexName(true, null) == "DRAFT-beta.Instance" && Edition.MutexName(true, @"C:\essai") == "DRAFT-beta.Test." + @"C:\essai".GetHashCode() && Edition.RunValue(true) == "DRAFT-beta",
                "Édition préversion : dossier %LOCALAPPDATA%\\DRAFT-beta, Mutex DRAFT-beta.Instance (DRAFT-beta.Test.* avec --data), valeur Run DRAFT-beta");
            t(Autostart.ValueName == Edition.RunValue(Edition.Prerelease), "Édition : valeur Run utilisée par cet exe : " + Autostart.ValueName);
            // Installation : refusée en préversion seulement (y compris un exe renommé « Setup »).
            Func<bool, Options, string, bool> refuses = Edition.RefusesInstall;
            bool betaRefuses = refuses(true, new Options { Install = true }, "DRAFT.exe") && refuses(true, new Options { Uninstall = true }, "DRAFT.exe") && refuses(true, new Options { TestInstall = true }, "DRAFT.exe") && refuses(true, new Options(), @"C:\x\DRAFT-Setup.exe") && !refuses(true, new Options(), @"C:\x\DRAFT-Portable.exe");
            bool stableAllows = !refuses(false, new Options { Install = true }, "DRAFT.exe") && !refuses(false, new Options { Uninstall = true }, "DRAFT.exe") && !refuses(false, new Options(), @"C:\x\DRAFT-Setup.exe");
            t(betaRefuses && stableAllows, "Édition : --install, --uninstall, --test-install et exe « Setup » refusés en préversion, permis en stable");
            // Raccourci : laissé à la 2.x en préversion seulement, sauf choix explicite.
            t(Edition.HotkeyLeftReason(true, false, true, false) == "instance" && Edition.HotkeyLeftReason(true, false, false, true) == "autostart" && Edition.HotkeyLeftReason(true, false, true, true) == "instance"
                && Edition.HotkeyLeftReason(true, false, false, false) == null && Edition.HotkeyLeftReason(true, true, true, true) == null && Edition.HotkeyLeftReason(false, false, true, true) == null,
                "Édition : Ctrl+Alt+D laissé à la 2.x en préversion (autre DRAFT ouvert, ou 2.x au démarrage de Windows), sauf choix du joueur ; stable : toujours tenté");
            // Valeur « DraftMid » de la 2.x : existence seulement, sur la clé de test (Autostart.Key), jamais sur la vraie clé Run.
            bool onTestKey = Autostart.Key != Autostart.DefaultKey;
            if (onTestKey)
            {
                bool before = Autostart.LegacyValueExists();
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Autostart.Key)) k.SetValue(Edition.StableName, "\"essai.exe\"");
                bool seen = Autostart.LegacyValueExists();
                string kept;
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Autostart.Key)) kept = Convert.ToString(k.GetValue(Edition.StableName));
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Autostart.Key)) k.DeleteValue(Edition.StableName, false);
                t(!before && seen && kept == "\"essai.exe\"" && !Autostart.LegacyValueExists(), "Édition : valeur Run DraftMid de la 2.x vue par son nom seulement, sans être modifiée (clé de test)");
            }
            else t(false, "Édition : clé Run de test absente, essai de la valeur DraftMid non fait");

            // Copie initiale : faux dossier 2.x, un fichier tenu en écriture partagée (2.x ouverte), un autre en exclusivité.
            string root = Path.Combine(dataDir, "test-edition");
            if (Directory.Exists(root)) Directory.Delete(root, true);
            string src = Path.Combine(root, "DraftMid"), dst = Path.Combine(root, "DRAFT-beta");
            string storage = Path.Combine(src, LegacyCopy.StorageRelative, "leveldb");
            Directory.CreateDirectory(storage);
            Directory.CreateDirectory(Path.Combine(src, "app", "abc"));
            Directory.CreateDirectory(Path.Combine(src, "webview", "EBWebView", "Default", "Cache"));
            Dictionary<string, string> files = new Dictionary<string, string> {
                { "settings.json", "{\"Compact\":true,\"ProfileKnown\":true}" }, { "draft-profiles.json", "{\"roles\":{}}" }, { "draft-profiles.json.precedent", "{}" },
                { "draft-history.js", "window.DRAFT_HISTORY = {\"rows\":[]};" }, { "draft-history.js.precedent", "window.DRAFT_HISTORY = {\"rows\":[]};" }, { "draft-stats.js", "window.DRAFT_STATS = {\"rows\":[]};" },
                { "validation-drafts.csv", "a;b" }, { "draftmid.log", "journal" }, { "diagnostic.log", "diag" }, { "settings.json.tmp", "x" },
                { Path.Combine("app", "abc", "index.html"), "<p>" }, { Path.Combine("webview", "EBWebView", "Default", "Cache", "f_000001"), "cache" },
                { Path.Combine(LegacyCopy.StorageRelative, "leveldb", "000003.log"), "journal-de-la-page" }, { Path.Combine(LegacyCopy.StorageRelative, "leveldb", "CURRENT"), "MANIFEST-000001" },
                { Path.Combine(LegacyCopy.StorageRelative, "leveldb", "MANIFEST-000001"), "manifeste" }, { Path.Combine(LegacyCopy.StorageRelative, "leveldb", "LOCK"), "" },
                { Path.Combine(LegacyCopy.StorageRelative, "leveldb", "000005.ldb"), "table" } };
            foreach (KeyValuePair<string, string> p in files) File.WriteAllText(Path.Combine(src, p.Key), p.Value);
            DateTime old = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            foreach (string f in Directory.GetFiles(src, "*", SearchOption.AllDirectories)) { File.SetLastWriteTimeUtc(f, old); File.SetCreationTimeUtc(f, old); }
            Dictionary<string, string> before2 = Snapshot(src);
            BetaState st;
            using (new FileStream(Path.Combine(storage, "000003.log"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (new FileStream(Path.Combine(storage, "000005.ldb"), FileMode.Open, FileAccess.Read, FileShare.None))
            using (new FileStream(Path.Combine(storage, "LOCK"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                st = LegacyCopy.Run(src, dst);
            string[] expected = { "settings.json", "draft-profiles.json", "draft-profiles.json.precedent", "draft-history.js", "draft-history.js.precedent", "draft-stats.js", "validation-drafts.csv",
                Path.Combine(LegacyCopy.StorageRelative, "leveldb", "000003.log"), Path.Combine(LegacyCopy.StorageRelative, "leveldb", "CURRENT"), Path.Combine(LegacyCopy.StorageRelative, "leveldb", "MANIFEST-000001") };
            string[] got = Directory.GetFiles(dst, "*", SearchOption.AllDirectories).Select(f => f.Substring(dst.Length + 1)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            bool sameContent = expected.All(e => File.Exists(Path.Combine(dst, e)) && File.ReadAllText(Path.Combine(dst, e)) == files[e]);
            t(st.SourceFound && st.Files == expected.Length && st.Skipped == 1 && got.SequenceEqual(expected.OrderBy(x => x, StringComparer.Ordinal)) && sameContent,
                "Copie initiale : " + st.Files + " fichiers de données copiés à l'identique (réglages, profils, historique, statistiques, relevé, stockage de la page), " + st.Skipped + " sauté (tenu en exclusivité) ; ni journal, ni cache, ni verrou, ni fichier temporaire");
            Dictionary<string, string> after2 = Snapshot(src);
            bool untouched = before2.Count == after2.Count && before2.All(p => after2.ContainsKey(p.Key) && after2[p.Key] == p.Value);
            bool unlocked = true;
            foreach (string f in Directory.GetFiles(src, "*", SearchOption.AllDirectories)) try { using (new FileStream(f, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } } catch (IOException) { unlocked = false; }
            t(untouched && unlocked, "Copie initiale : dossier source identique octet pour octet (" + before2.Count + " entrées, empreintes et dates), aucun fichier ajouté, aucun verrou laissé");
            BetaState again = LegacyCopy.IsEmpty(dst) ? LegacyCopy.Run(src, dst) : null;
            BetaState none = LegacyCopy.Run(Path.Combine(root, "absent"), Path.Combine(root, "vide"));
            t(again == null && !LegacyCopy.IsEmpty(dst) && LegacyCopy.IsEmpty(Path.Combine(root, "pas-encore")) && !none.SourceFound && none.Files == 0 && !Directory.Exists(Path.Combine(root, "vide")),
                "Copie initiale : seulement si le dossier de la bêta est vide ; source absente : rien de copié, rien de créé");
            st.Hotkey = true; st.Save(dst);
            BetaState back = BetaState.Load(dst);
            t(back.Hotkey && back.Files == st.Files && back.CopiedAt == st.CopiedAt && BetaState.Load(Path.Combine(root, "absent")).Files == 0, "Préversion : état de la copie et choix de Ctrl+Alt+D relus à l'identique (" + BetaState.FileName + ")");
            Directory.Delete(root, true);
        }
    }

    static class SelfTest
    {
        public static int Run(Options o, string dataDir, string self)
        {
            StringBuilder s = new StringBuilder();
            int fails = 0;
            Action<bool, string> t = (ok, text) => { if (!ok) fails++; s.AppendLine((ok ? "OK : " : "ECHEC : ") + text); };

            Func<int, int, int, int, double, Monitor> mon = (x, y, w, h, sc) => new Monitor { Work = new Native.RECT(x, y, x + w, y + h), Scale = sc };
            Action<string, Native.RECT, List<Monitor>, bool, string> check = (name, client, mons, compact, expected) =>
            {
                Placement p = Layout.Compute(client, mons, compact);
                t(p.Mode == expected, name + " : mode " + p.Mode);
                List<Native.RECT> rects = new List<Native.RECT>();
                if (p.Mode == "two") { rects.Add(p.Left); rects.Add(p.Right); }
                if (p.Mode == "compact") rects.Add(p.Single);
                t(rects.All(r => !r.Overlaps(client)), name + " : aucun panneau ne couvre le client");
                t(rects.All(r => mons.Any(m => r.Inside(m.Work))), name + " : les panneaux restent sur un écran");
                if (rects.Count == 2) t(!rects[0].Overlaps(rects[1]), name + " : les panneaux ne se chevauchent pas");
            };

            List<Monitor> fhd = new List<Monitor> { mon(0, 0, 1920, 1040, 1) };
            check("1 écran 1920, client 1280x720 centré", new Native.RECT(320, 160, 1600, 880), fhd, false, "two");
            check("1 écran 1920, client 1280x720 en mode compact", new Native.RECT(320, 160, 1600, 880), fhd, true, "compact");
            check("1 écran 1920, client 1600x900 centré, trop large", new Native.RECT(160, 70, 1760, 970), fhd, false, "manual");
            check("1 écran 1920, client décalé à gauche", new Native.RECT(0, 100, 1280, 820), fhd, false, "compact");
            check("1 écran 1366, client 1024x576", new Native.RECT(171, 96, 1195, 672), new List<Monitor> { mon(0, 0, 1366, 728, 1) }, false, "manual");
            check("1 écran 2560, client 1600x900", new Native.RECT(480, 200, 2080, 1100), new List<Monitor> { mon(0, 0, 2560, 1400, 1) }, false, "two");
            Rectangle placed = new Rectangle(0, 155, 320, 720);
            t(!PanelForm.MovedOnPurpose(placed, new Rectangle(0, 155, 320, 721)), "Un clic sur un bord (1 pixel) ne coupe pas le suivi");
            t(PanelForm.MovedOnPurpose(placed, new Rectangle(0, 300, 320, 720)) && PanelForm.MovedOnPurpose(placed, new Rectangle(0, 155, 260, 720)), "Un vrai déplacement ou redimensionnement reste mémorisé");
            // Toutes les combinaisons de résolution, d'échelle et de taille du client. Calcul seul : ce n'est pas un test sur de vrais écrans.
            int layoutCases = 0, narrowCases = 0;
            string layoutProblem = "";
            foreach (int[] res in new[] { new[] { 1920, 1080 }, new[] { 2560, 1440 }, new[] { 1366, 768 }, new[] { 1280, 720 }, new[] { 3840, 2160 } })
                foreach (double scale in new[] { 1, 1.25, 1.5, 1.75 })
                    foreach (int[] size in new[] { new[] { 1024, 576 }, new[] { 1280, 720 }, new[] { 1600, 900 } })
                        foreach (bool compact in new[] { false, true })
                        {
                            int bar = (int)(40 * scale);
                            if (size[0] > res[0] || size[1] > res[1] - bar) continue;
                            layoutCases++;
                            List<Monitor> one = new List<Monitor> { mon(0, 0, res[0], res[1] - bar, scale) };
                            int left = (res[0] - size[0]) / 2, top = (res[1] - bar - size[1]) / 2;
                            Native.RECT win = new Native.RECT(left, top, left + size[0], top + size[1]);
                            string label2 = res[0] + "x" + res[1] + "@" + scale + " client " + size[0] + (compact ? " compact" : "") + " ";
                            Placement computed = Layout.Compute(win, one, compact);
                            int minWidth = (int)(230 * scale);
                            List<Native.RECT> boxes = new List<Native.RECT>();
                            if (computed.Mode == "two") { boxes.Add(computed.Left); boxes.Add(computed.Right); }
                            else if (computed.Mode == "compact") boxes.Add(computed.Single);
                            else
                            {
                                narrowCases++;
                                Native.RECT narrow = Layout.Narrowest(win, one).Single;
                                if (!narrow.Inside(one[0].Work) || narrow.W != Math.Min(minWidth, one[0].Work.W)) layoutProblem += label2 + "panneau étroit hors écran ; ";
                                continue;
                            }
                            foreach (Native.RECT box in boxes)
                            {
                                if (box.Overlaps(win)) layoutProblem += label2 + "couvre le client ; ";
                                if (!box.Inside(one[0].Work)) layoutProblem += label2 + "hors écran ; ";
                                if (box.W < minWidth) layoutProblem += label2 + "trop étroit ; ";
                            }
                            if (boxes.Count == 2 && boxes[0].Overlaps(boxes[1])) layoutProblem += label2 + "panneaux superposés ; ";
                        }
            t(layoutProblem == "", "Placement : " + layoutCases + " combinaisons résolution, échelle et client, dont " + narrowCases + " sans place libre " + layoutProblem);
            List<Monitor> mixed = new List<Monitor> { mon(0, 0, 1920, 1040, 1), mon(-2560, 0, 2560, 1400, 1.5) };
            Placement toOther = Layout.Compute(new Native.RECT(0, 0, 1920, 1040), mixed, false);
            t(toOther.Mode == "two" && toOther.Left.W == 510 && toOther.Left.Right <= 0 && toOther.Right.Right <= 0, "Placement : client plein écran à 100 %, panneaux sur l'autre écran à 150 %, à sa propre échelle");

            List<Monitor> mixedDpi = new List<Monitor> { mon(0, 0, 1920, 1040, 1), mon(1920, 0, 3840, 2120, 2) };
            check("Scénario 15. Deux écrans 100 % et 200 %, client sur le 200 %", new Native.RECT(2560, 340, 5120, 1780), mixedDpi, false, "two");
            check("Scénario 15. Deux écrans 100 % et 200 %, client sur le 100 %", new Native.RECT(320, 160, 1600, 880), mixedDpi, false, "two");
            t(Layout.Compute(new Native.RECT(2560, 340, 5120, 1780), mixedDpi, false).Left.W == 640 && Layout.Compute(new Native.RECT(320, 160, 1600, 880), mixedDpi, false).Left.W == 320, "Scénario 15. La largeur suit le DPI de l'écran du client");
            Native.RECT sameClient = new Native.RECT(171, 96, 1451, 816);
            check("Scénario 14. Résolution 1920 : deux panneaux", new Native.RECT(320, 160, 1600, 880), fhd, false, "two");
            check("Scénario 14. Résolution réduite à 1600 : panneau unique ou manuel, sans couvrir le client", sameClient, new List<Monitor> { mon(0, 0, 1600, 860, 1) }, false, "manual");
            check("1 écran 4K à 200 %, client 2560x1440", new Native.RECT(640, 300, 3200, 1740), new List<Monitor> { mon(0, 0, 3840, 2100, 2) }, false, "two");
            check("1 écran 1920 à 125 %, client 1280x720", new Native.RECT(320, 160, 1600, 880), new List<Monitor> { mon(0, 0, 1920, 1030, 1.25) }, false, "two");
            check("Client petit : hauteur minimale respectée", new Native.RECT(320, 300, 1600, 500), fhd, false, "two");
            List<Monitor> dual = new List<Monitor> { mon(0, 0, 1920, 1040, 1.25), mon(-1920, 87, 1920, 1040, 1) };
            check("2 écrans, client plein écran sur le principal", new Native.RECT(0, 0, 1920, 1040), dual, false, "two");
            check("2 écrans, client 1280 sur l'écran secondaire", new Native.RECT(-1600, 300, -320, 1020), dual, false, "two");
            check("2 écrans, client à cheval : l'écran le plus couvert compte", new Native.RECT(-300, 100, 1300, 1000), dual, false, "compact");
            check("2 écrans, plein écran en panneau unique", new Native.RECT(0, 0, 1920, 1040), dual, true, "compact");

            // QA-4 (simulable sur un seul écran) : position enregistrée sur un écran débranché, presque hors écran, barre de titre
            // au-dessus de l'écran ; position valide sur un second écran à 150 %. Entre parenthèses : la règle d'avant (un pixel commun).
            {
                List<Monitor> two = new List<Monitor> { mon(0, 0, 1920, 1040, 1.25), mon(-2560, 0, 2560, 1400, 1.5) };
                List<Monitor> onlyMain = new List<Monitor> { two[0] };
                Func<int, int, int, int, Native.RECT> rr = (x, y, w, h) => new Native.RECT(x, y, x + w, y + h);
                Native.RECT onSecond = rr(-600, 100, 510, 1200), sliver = rr(-330, 100, 340, 700), aboveTop = rr(100, -200, 340, 700), onMain = rr(1500, 100, 400, 800), halfOut = rr(1750, 100, 340, 700);
                Func<Native.RECT, List<Monitor>, string> before216 = (box, ms) => ms.Any(m => m.Work.Overlaps(box)) ? "gardée" : "ignorée";
                t(Layout.KeepSaved(onSecond, two) && !Layout.KeepSaved(onSecond, onlyMain) && Layout.KeepSaved(onMain, onlyMain) && Layout.KeepSaved(halfOut, onlyMain),
                    "QA-4 : position sur le second écran gardée tant qu'il est branché, ignorée quand il est retiré ; position sur l'écran principal gardée");
                t(!Layout.KeepSaved(sliver, onlyMain), "QA-4 : panneau dont 10 px seulement restent sur l'écran : place par défaut (avant : " + before216(sliver, onlyMain) + ")");
                t(!Layout.KeepSaved(aboveTop, onlyMain), "QA-4 : barre de titre au-dessus de l'écran : place par défaut (avant : " + before216(aboveTop, onlyMain) + ")");
            }

            string sp = Path.Combine(dataDir, "selftest-settings.json");
            UserPreferences a = new UserPreferences { Compact = true, Follow = false, Topmost = true, AutoDraft = false };
            a.Rects["left"] = new SavedRect { X = -100, Y = 20, W = 300, H = 700 };
            a.Save(sp);
            UserPreferences b = UserPreferences.Load(sp);
            t(b.Compact && !b.Follow && b.Topmost && !b.AutoDraft && b.Rects["left"].X == -100, "préférences relues à l'identique");
            File.WriteAllText(sp, "{\"Compact\":true,\"Follow\":false,\"Topmost\":false,\"Rects\":{\"left\":{\"X\":1,\"Y\":2,\"W\":3,\"H\":4}}}");
            UserPreferences old = UserPreferences.Load(sp);
            t(old.Compact && !old.Follow && old.AutoDraft && old.Rects["left"].W == 3 && old.DraftSamples != null, "préférences de la V2.2 conservées, nouvelles options activées par défaut");
            File.WriteAllText(sp, "pas du json");
            UserPreferences c = UserPreferences.Load(sp);
            t(c != null && c.Follow && !c.Compact, "un fichier de préférences abîmé redonne les valeurs par défaut");
            File.Delete(sp);

            Autostart.Key = o.RunKey ?? @"Software\DraftMid-SelfTest\Run";
            t(!Autostart.IsOn(), "démarrage automatique désactivé par défaut");
            Autostart.Set(true, self);
            t(Autostart.IsOn(), "activation du démarrage automatique");
            Autostart.Set(false, self);
            t(!Autostart.IsOn(), "désactivation du démarrage automatique");
            EditionTests.Run(t, dataDir, self);
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\DraftMid-SelfTest", false);
            Autostart.Key = Autostart.DefaultKey;

            DetectionTests.Run(t, s, dataDir);
            LcuTests.Run(t);
            RoleRatesTests.Run(t, dataDir);
            MatchupStatsTests.Run(t, dataDir);
            HistoryImporterTests.Run(t, dataDir);
            AppUpdaterTests.Run(t, dataDir, self);
            DataScriptsTests.Run(t, dataDir);
            SecurityTests.Run(t);
            StringsTests.Run(t, dataDir);
            t(CompanionApp.IsAppPage("https://draftmid.local/index.html?panel=full") && !CompanionApp.IsAppPage("https://draftmid.local.exemple.test/") && !CompanionApp.IsAppPage("https://draftmid.local@exemple.test/") && !CompanionApp.IsAppPage(null), "Page de confiance : seule l'origine de DRAFT est reconnue");
            t(CompanionApp.AllowedPageLink("https://lolalytics.com/lol/ahri/build/") && !CompanionApp.AllowedPageLink("https://lolalytics.com.exemple.test/") && !CompanionApp.AllowedPageLink("http://lolalytics.com/") && !CompanionApp.AllowedPageLink("https://exemple.test/?q=lolalytics.com") && !CompanionApp.AllowedPageLink("https://user@lolalytics.com/") && !CompanionApp.AllowedPageLink("file:///C:/Windows/"), "Liens de la page : seuls des sites connus, en HTTPS");
            t(CompanionApp.CsvCell("=HYPERLINK(\"x\")") == "'=HYPERLINK(\"x\")" && CompanionApp.CsvCell("+1") == "'+1" && CompanionApp.CsvCell("Ahri") == "Ahri" && CompanionApp.CsvCell("") == "", "Relevé CSV : formules neutralisées");

            ClientWindowTracker tr = new ClientWindowTracker(ClientWindowTracker.IsRealClient, false);
            Stopwatch clock = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++) { tr.Refresh(); if (tr.Handle != IntPtr.Zero) break; }
            s.AppendLine("MESURE : recherche de la fenêtre du client " + (clock.ElapsedMilliseconds / 50.0) + " ms au maximum, client " + (tr.Handle != IntPtr.Zero ? (tr.Minimized ? "réduit" : "visible") : "absent"));
            tr.Dispose();
            List<Monitor> real = ClientWindow.Monitors();
            s.AppendLine("ECRANS : " + string.Join(" ; ", real.Select(m => m.Work + " x" + m.Scale)));

            s.AppendLine((fails == 0 ? "TOUT EST BON" : fails + " ECHEC(S)"));
            if (o.Out != null) File.WriteAllText(o.Out, s.ToString());
            return fails == 0 ? 0 : 1;
        }
    }

    static class Program
    {
        public static readonly Stopwatch Clock = Stopwatch.StartNew();
        public static void Log(string dir, string text)
        {
            try { File.AppendAllText(Path.Combine(dir, "draftmid.log"), DateTime.Now + " " + text + Environment.NewLine); } catch { }
        }

        static Assembly ResolveEmbedded(object s, ResolveEventArgs e)
        {
            string name = new AssemblyName(e.Name).Name + ".dll";
            using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream("lib/" + name))
            {
                if (st == null) return null;
                byte[] b = new byte[st.Length];
                st.Read(b, 0, b.Length);
                return Assembly.Load(b);
            }
        }

        // Déploie l'interface intégrée à l'exe. Une nouvelle version ne touche ni au journal ni aux préférences.
        static string Extract(string dataDir)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string dir = Path.Combine(dataDir, "app", asm.ManifestModule.ModuleVersionId.ToString("N").Substring(0, 12));
            if (!File.Exists(Path.Combine(dir, ".ok")))
            {
                foreach (string res in asm.GetManifestResourceNames().Where(n => n.StartsWith("web/") || n == "lib/WebView2Loader.dll"))
                {
                    string rel = res.StartsWith("web/") ? res.Substring(4) : "WebView2Loader.dll";
                    string path = Path.Combine(dir, rel.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    // Catalogue de langue : relu comme du JSON strict et réécrit par DRAFT, comme les fichiers de données.
                    string catalog;
                    if (res.StartsWith("web/") && DataScripts.Catalogs.TryGetValue(rel, out catalog))
                    {
                        string text;
                        using (Stream st = asm.GetManifestResourceStream(res))
                        using (StreamReader r = new StreamReader(st, Encoding.UTF8)) text = r.ReadToEnd();
                        string safe = DataScripts.Canonical(catalog, text);
                        if (safe == null) { Log(dataDir, "Catalogue de langue non valide, remplacé par un catalogue vide : " + rel); safe = DataScripts.EmptyCatalog(catalog); }
                        File.WriteAllText(path, safe, new UTF8Encoding(false));
                        continue;
                    }
                    using (Stream st = asm.GetManifestResourceStream(res))
                    using (FileStream fs = File.Create(path)) st.CopyTo(fs);
                }
                File.WriteAllText(Path.Combine(dir, ".ok"), "");
                foreach (string old in Directory.GetDirectories(Path.Combine(dataDir, "app")).Where(d => d != dir))
                    try { Directory.Delete(old, true); } catch { }
            }
            // Les données sont relues et réécrites, jamais copiées telles quelles : un fichier piégé n'atteint pas la page.
            foreach (string file in DataScripts.Files.Keys)
                if (!DataScripts.Publish(file, dataDir, dir)) Log(dataDir, "Fichier de données ignoré, contenu non valide : " + file);
            return dir;
        }

        // Essai réel, sans rien remplacer : lit la dernière release sur GitHub, télécharge son exécutable et le vérifie.
        static int TestUpdateLive(Options o, string dataDir)
        {
            string file = Path.Combine(dataDir, "essai-maj.exe");
            try
            {
                GitHubUpdateSource source = new GitHubUpdateSource();
                UpdateInfo info = source.LatestAsync(CancellationToken.None).GetAwaiter().GetResult();
                source.DownloadAsync(info, file, null, CancellationToken.None).GetAwaiter().GetResult();
                AppUpdater.CheckFile(file, info);
                File.WriteAllText(o.Out, "OK " + info.Tag + " taille=" + info.Size + " plus-recent=" + AppUpdater.IsNewer(AppUpdater.Current, info.Version) + " signee=" + (info.SignatureUrl != null));
                return 0;
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException) throw;
                File.WriteAllText(o.Out, "ECHEC " + AppUpdater.FriendlyError(e));
                return 1;
            }
            finally { try { File.Delete(file); } catch (IOException) { } }
        }

        // Exécutable, arguments de relance, arguments en cas de retour en arrière.
        public static string[] Relaunch;

        static string logDir;

        [STAThread]
        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbedded;
            // Une erreur imprévue est journalisée : elle ne doit ni interrompre une draft, ni rester silencieuse.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => { if (logDir != null) Log(logDir, e.Exception.ToString()); };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { if (logDir != null) Log(logDir, Convert.ToString(e.ExceptionObject)); };
            return Run(args);
        }

        // Langue du programme (I18N-3). Essai : --lang, sinon le français pour --selftest et --e2e (résultats comparables d'une
        // machine à l'autre). Sinon le réglage de settings.json (Language) ; absent ou "auto" : la langue de Windows.
        public static string InitialLanguage(Options o, string dataDir)
        {
            if (o.Lang != null && Array.IndexOf(Strings.Supported, o.Lang) >= 0) return o.Lang;
            if (o.SelfTest || o.EndToEnd) return "fr";
            string setting = null;
            if (dataDir != null) { try { string path = Path.Combine(dataDir, "settings.json"); if (File.Exists(path)) setting = UserPreferences.Load(path).Language; } catch (Exception e) { if (e is OutOfMemoryException) throw; } }
            return Strings.Resolve(setting, System.Globalization.CultureInfo.CurrentUICulture);
        }

        static int Run(string[] args)
        {
            Options o = Options.Parse(args);
            // Textes du programme : catalogue intégré, relu comme des données. Mode test : chaque clé absente est notée.
            Strings.TestMode = o.SelfTest || o.EndToEnd;
            Strings.Load(InitialLanguage(o, null), Strings.Embedded);
            Native.SetProcessDpiAwarenessContext(Native.PMV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string self = Application.ExecutablePath;
            // Préversion : portable seulement. Rien n'est créé ni modifié avant ce refus (ni dossier, ni registre, ni raccourci).
            if (Edition.RefusesInstall(Edition.Prerelease, o, self))
            {
                if (!o.Silent) MessageBox.Show(Strings.T("native.beta.noInstall"), "DRAFT", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 3;
            }
            // Stable : %LOCALAPPDATA%\DraftMid, comme la 2.x. Préversion : %LOCALAPPDATA%\DRAFT-beta. --data reste prioritaire.
            string dataDir = o.DataDir ?? Edition.DefaultDataDir(Edition.Prerelease);
            // Préversion, premier lancement (dossier absent ou vide) : copie des données 2.x. Source : --legacy-data, sinon, sans
            // --data, le dossier de la 2.x. Avec --data seul, aucune copie : un essai ne lit jamais le vrai dossier 2.x.
            string legacySource = !Edition.Prerelease ? null : o.LegacyData ?? (o.DataDir == null ? Edition.DefaultDataDir(false) : null);
            bool copyLegacy = legacySource != null && !o.SelfTest && LegacyCopy.IsEmpty(dataDir);
            try
            {
                Directory.CreateDirectory(dataDir);
                string probe = Path.Combine(dataDir, "ecriture.tmp");
                File.WriteAllText(probe, "");
                File.Delete(probe);
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException) throw;
                if (!o.Silent) MessageBox.Show(Strings.T("native.dialog.cannotWrite", "path", dataDir), "DRAFT", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 2;
            }
            logDir = dataDir;
            Strings.Use(InitialLanguage(o, dataDir));
            if (o.RunKey != null) Autostart.Key = o.RunKey;

#if DRAFTMID_PROTOTYPES
            if (o.OpggPrototype || o.OpggTest)
            {
                // Prototype isolé : jamais sur les données de l'application, car l'extraction y remplacerait la version installée.
                if (o.DataDir == null) { if (!o.Silent) MessageBox.Show(Strings.T("native.dialog.prototypeData"), "DRAFT"); return 2; }
                RuntimeHelp.EnsureLoader(Extract(dataDir));
                if (o.OpggTest) { OpggTests.Run(o, dataDir); Application.Run(); return 0; }
                Application.Run(new OpggBrowserForm(OpggLinks.Home, Path.Combine(dataDir, "wv"), Path.Combine(dataDir, "opgg-profile.json")));
                return 0;
            }
#endif
            if (o.SelfTest) return SelfTest.Run(o, dataDir, self);
            // Inaccessible en préversion (refus plus haut) : installation, désinstallation et migration de l'ancien exe.
            if (o.Uninstall) { Installer.Uninstall(o, self); return 0; }
            if (o.Install || (!o.TestInstall && Path.GetFileName(self).IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0)) { Installer.Install(o, self); return 0; }
            if (o.TestInstall) { Installer.Install(o, self); return 0; }
            if (o.TestUpdateLive) return TestUpdateLive(o, dataDir);
            if (o.TestSignature != null) return AppUpdater.ValidSignature(o.TestSignature[1], AppUpdater.Sha256Of(o.TestSignature[0]), File.ReadAllText(o.TestSignature[2]), AppUpdater.ReleaseKey) ? 0 : 1;

            bool created;
            // Stable : « DraftMid.Instance », comme la 2.x. Préversion : « DRAFT-beta.Instance » : la bêta et une 2.x ne se bloquent pas.
            using (Mutex single = new Mutex(true, Edition.MutexName(Edition.Prerelease, o.DataDir), out created))
            {
                if (!created)
                {
                    if (!o.Silent) MessageBox.Show(Strings.T("native.dialog.alreadyOpen"), "DRAFT");
                    return 0;
                }
                BetaState copied = null;
                if (copyLegacy && LegacyCopy.IsEmpty(Path.Combine(dataDir, "webview")) && !File.Exists(Path.Combine(dataDir, "settings.json")))
                {
                    copied = LegacyCopy.Run(legacySource, dataDir);
                    copied.Save(dataDir);
                    // La langue choisie dans la version copiée vaut dès ce premier lancement (avant la copie, le dossier était vide).
                    Strings.Use(InitialLanguage(o, dataDir));
                    Log(dataDir, "Préversion : copie des données 2.x, " + copied.Files + " fichier(s) copié(s), " + copied.Skipped + " sauté(s), source " + (copied.SourceFound ? "trouvée" : "absente"));
                }
                else if (Edition.Prerelease && !File.Exists(Path.Combine(dataDir, BetaState.FileName))) new BetaState().Save(dataDir);
                string webDir = Extract(dataDir);
                string runtime = RuntimeHelp.Version(webDir);
                if (o.CheckRuntime) { File.WriteAllText(o.Out, runtime ?? "absent"); return 0; }
                if (runtime == null && !o.Silent) { RuntimeHelp.Explain(); return 0; }
                CompanionApp app = new CompanionApp(o, dataDir, webDir, self, o.EndToEnd ? EndToEnd.Matcher : ClientWindowTracker.IsRealClient);
                if (copied != null) app.AnnounceBetaCopy(copied);
                if (o.AfterUpdate <= 0) AppUpdater.CleanUp(self);
                if (o.UpdateRolledBack) app.AnnounceRolledBack();
                // Après une mise à jour : quelques secondes sans plantage valent confirmation pour l'ancienne version, qui surveille.
                bool confirmed = false;
                Action confirm = () =>
                {
                    if (confirmed) return;
                    confirmed = true;
                    AppUpdater.SignalReady(o.AfterUpdate);
                    AppUpdater.WaitForExit(o.AfterUpdate);
                    AppUpdater.CleanUp(self);
                };
                if (o.AfterUpdate > 0)
                {
                    System.Windows.Forms.Timer settled = new System.Windows.Forms.Timer { Interval = 5000 };
                    settled.Tick += (s, e) => { settled.Stop(); confirm(); app.AnnounceUpdated(); };
                    settled.Start();
                }
                if (o.TestUpdate != null) { if (o.DataDir == null) return 2; app.TestUpdate(o.TestUpdate); }
                else if (o.Soak > 0) EndToEnd.RunSoak(app, o);
                else if (o.EndToEnd) EndToEnd.Run(app, o);
                else if (o.ReportState != null) app.ReportStateAndQuit(o.ReportState);
                Application.Run(app);
                if (o.AfterUpdate > 0) confirm();
            }
            if (Relaunch != null) AppUpdater.LaunchAndWatch(Relaunch[0], Relaunch[1], Relaunch[2], 60000);
            return 0;
        }
    }
}
