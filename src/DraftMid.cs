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
[assembly: AssemblyVersion("2.16.2.0")]
[assembly: AssemblyFileVersion("2.16.2.0")]

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
        public string Note;
    }

    // Calcul pur, sans fenêtre : testé par --selftest sur plusieurs écrans et résolutions.
    public static class Layout
    {
        const int MinW = 230, IdealW = 340, MinH = 520, Gap = 0;

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
                    Note = "Panneaux placés de chaque côté du client."
                };
            }
            if (Math.Max(leftFree, rightFree) >= min)
            {
                int y, h;
                Vertical(client, home, out y, out h);
                Native.RECT r = rightFree >= leftFree
                    ? new Native.RECT(client.Right + Gap, y, client.Right + Gap + Math.Min((int)(ideal * 1.1), rightFree), y + h)
                    : new Native.RECT(client.Left - Gap - Math.Min((int)(ideal * 1.1), leftFree), y, client.Left - Gap, y + h);
                return new Placement { Mode = "compact", Single = r, Note = preferCompact ? "Panneau unique à côté du client." : "Pas assez de place pour deux panneaux : panneau unique." };
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
                        Note = "Pas de place autour du client : panneaux sur l'autre écran."
                    };
                }
                if (other.Work.W >= w)
                {
                    int x = other.Work.Left < home.Work.Left ? other.Work.Right - w : other.Work.Left;
                    return new Placement { Mode = "compact", Single = new Native.RECT(x, other.Work.Top, x + w, other.Work.Top + h), Note = "Pas de place autour du client : panneau sur l'autre écran." };
                }
            }
            return new Placement { Mode = "manual", Note = "Pas de place autour du client : place les panneaux à la main pour ne rien couvrir." };
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
                Note = "Pas de place autour du client : panneau étroit au bord de l'écran, il en couvre une partie. Déplace-le si besoin."
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

public static class Autostart
    {
        public const string DefaultKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static string Key = DefaultKey;
        const string Value = "DraftMid";
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
            if (!app.Quitting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
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
        public event Action Pressed;
        public readonly bool Registered;
        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
            // Ctrl + Alt + D, sans répétition automatique.
            Registered = NativeHook.RegisterHotKey(Handle, 1, 0x0002 | 0x0001 | 0x4000, (uint)Keys.D);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) { Action p = Pressed; if (p != null) p(); }
            base.WndProc(ref m);
        }
        public void Dispose() { NativeHook.UnregisterHotKey(Handle, 1); DestroyHandle(); }
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
        readonly HotkeyWindow hotkey;
        CoreWebView2Environment env;
        Task envTask;
        string shownMode = "", pendingMode = "";
        long pendingSince;
        bool panelsShown, frameValid;
        string placementNote = "Client LoL non détecté.";
        public bool Quitting;
        public Icon AppIcon;
        ToolStripMenuItem miUpdate, miRoles, miMatchups, miCompact, miFollow, miAutoDraft, miAutostart, miShow, miPhase;
        public long MoveCount;

        public Phase CurrentPhase { get { return machine.Current; } }
        public bool PanelsShown { get { return panelsShown; } }
        public string ShownMode { get { return shownMode; } }
        public ClientWindowTracker Tracker { get { return tracker; } }
        public bool HotkeyRegistered { get { return hotkey != null && hotkey.Registered; } }
        public bool HotkeyCreated { get { return hotkey != null; } }
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

            ContextMenuStrip menu = new ContextMenuStrip();
            miPhase = new ToolStripMenuItem("") { Enabled = false };
            miShow = new ToolStripMenuItem("Afficher les panneaux (Ctrl+Alt+D)", null, (s, e) => ToggleManual());
            miAutoDraft = new ToolStripMenuItem("Activation automatique pendant la draft", null, (s, e) => SetAutoDraft(!settings.AutoDraft));
            miFollow = new ToolStripMenuItem("Positionnement automatique", null, (s, e) => SetFollow(!settings.Follow));
            miCompact = new ToolStripMenuItem("Panneau unique", null, (s, e) => SetCompact(!settings.Compact));
            miAutostart = new ToolStripMenuItem("Démarrer avec Windows", null, (s, e) => { Autostart.Set(!Autostart.IsOn(), exePath); RefreshUi(); });
            menu.Items.Add(miPhase);
            menu.Items.Add(miShow);
            menu.Items.Add("C'est une draft : retenir cet écran", null, (s, e) => Teach(true));
            menu.Items.Add("Ce n'est pas une draft : retenir cet écran", null, (s, e) => Teach(false));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miAutoDraft);
            menu.Items.Add(miFollow);
            menu.Items.Add(miCompact);
            menu.Items.Add("Replacer les panneaux", null, (s, e) => { SetFollow(true); UpdatePlacement(true); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Analyse détaillée", null, (s, e) => OpenFull(""));
            menu.Items.Add("Réglages", null, (s, e) => OpenFull("settings"));
            menu.Items.Add("Diagnostic de connexion LoL...", null, (s, e) => ShowLcuDiagnostic());
            menu.Items.Add("Importer mes statistiques...", null, (s, e) => ImportStats());
            miRoles = new ToolStripMenuItem("Mettre à jour les rôles...", null, (s, e) => ToggleRiotUpdate(false));
            miMatchups = new ToolStripMenuItem("Mettre à jour les matchups...", null, (s, e) => ToggleRiotUpdate(true));
            menu.Items.Add(miRoles);
            menu.Items.Add(miMatchups);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miAutostart);
            miUpdate = new ToolStripMenuItem("Rechercher une mise à jour...", null, (s, e) => CheckForUpdate());
            menu.Items.Add(miUpdate);
            menu.Items.Add("Quitter", null, (s, e) => Quit());
            tray = new NotifyIcon { Icon = AppIcon ?? SystemIcons.Application, Text = "DRAFT", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += (s, e) => ToggleManual();

            // Essai à côté d'une autre version : le raccourci reste à celle-ci, sans même tenter de l'enregistrer.
            if (!o.NoHotkey)
            {
                hotkey = new HotkeyWindow();
                hotkey.Pressed += ToggleManual;
            }

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
            slowPoll.Tick += (s, e) => { tracker.Refresh(); OnClientChanged(); };
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
            if (firstRun) { firstRun = false; OpenFull("settings"); }
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
                string error;
                string folder = ClientProcess.FolderOf((int)pid, out error);
                if (folder == null) { LcuFactory.LastOpenError = error; return null; }
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
            catch (Exception e) { text = "Diagnostic interrompu : " + e.GetType().Name; }
            bool copied = false;
            try { Clipboard.SetText(text); copied = true; } catch { }
            MessageBox.Show(text + (copied ? "\r\nCe texte est copié : colle-le dans ton message." : ""), "DRAFT : diagnostic de connexion LoL", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                if (now == Phase.Draft && before != Phase.Draft) NotifyDraftStart();
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
        string rolesStatus = "";
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
        string historyStatus = "";
        bool historyHasMore;

        async void ToggleHistoryImport()
        {
            if (historyCts != null)
            {
                historyCts.Cancel();
                historyStatus = "Arrêt de l'import : les parties déjà lues sont conservées...";
                RefreshUi();
                return;
            }
            if (machine.Current == Phase.Draft) { historyStatus = "Une draft est en cours : lance l'import après."; RefreshUi(); return; }
            if (rolesCts != null) { historyStatus = "Un calcul Riot est déjà en cours : attends sa fin pour ne pas dépasser les limites de ta clé."; RefreshUi(); return; }
            string path = Path.Combine(dataDir, HistoryImporter.FileName);
            string key;
            RiotRegion region;
            int limit;
            List<string> accounts;
            using (HistoryImportDialog dialog = new HistoryImportDialog(AppIcon, OpenExternal, HistoryImporter.SavedAccounts(path)))
            {
                if (dialog.ShowDialog() != DialogResult.OK || !dialog.Consent) return;
                key = dialog.Key;
                region = dialog.SelectedRegion;
                limit = dialog.GamesToRead;
                accounts = dialog.Accounts;
            }
            await RunHistoryImport(new RiotWebFetcher(key), region, accounts, limit, 1300);
        }

        // La clé reste dans le fetcher : ni l'importeur, ni les journaux, ni la page ne la voient.
        public async Task<HistoryImportResult> RunHistoryImport(IRiotFetcher fetcher, RiotRegion region, List<string> accounts, int limit, int delayMs)
        {
            string path = Path.Combine(dataDir, HistoryImporter.FileName);
            CancellationTokenSource cts = new CancellationTokenSource();
            historyCts = cts;
            historyStatus = "Import de l'historique : démarrage...";
            RefreshUi();
            SynchronizationContext ui = SynchronizationContext.Current;
            HistoryImporter importer = new HistoryImporter(fetcher, region, path, accounts, HistoryImportDialog.OldestSupported) { MaxGamesPerRun = limit, DelayMs = delayMs };
            importer.Status = text => ui.Post(_ => { if (historyCts != null && !historyCts.IsCancellationRequested) { historyStatus = text; RefreshUi(); } }, null);
            importer.Progress = (done, total, account) =>
            {
                if (done % 5 != 0 && done != total) return;
                ui.Post(_ => { if (historyCts != null && !historyCts.IsCancellationRequested) { historyStatus = "Import de l'historique : " + done + " sur " + total + " parties"; RefreshUi(); } }, null);
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
                catch (IOException) { r.Error = "Historique enregistré, mais pas encore visible : relance DRAFT."; }
            }
            historyHasMore = r.HasMore;
            string summary = r.Added + (r.Added > 1 ? " parties ajoutées, " : " partie ajoutée, ") + r.Total + " au total.";
            historyStatus = r.Error != "" ? r.Error + " " + summary : (r.Cancelled ? "Import arrêté : " : "Import terminé : ") + summary;
            if (r.NotFound.Count > 0) historyStatus += " Compte introuvable : " + string.Join(", ", r.NotFound) + ".";
            if (tray != null) tray.ShowBalloonTip(5000, "DRAFT", historyStatus, r.Error == "" ? ToolTipIcon.Info : ToolTipIcon.Warning);
            RefreshUi();
            return r;
        }

        async void ToggleRiotUpdate(bool matchups)
        {
            if (historyCts != null && rolesCts == null) { rolesStatus = "Un import de l'historique est en cours : attends sa fin."; RefreshUi(); return; }
            if (rolesCts != null)
            {
                rolesCts.Cancel();
                rolesStatus = "Arrêt du calcul en cours...";
                RefreshUi();
                return;
            }
            string key;
            RiotRegion region;
            string what = matchups ? "matchups" : "rôles";
            string role = settings.ProfileRole;
            string[] pool = settings.ProfilePool;
            if (matchups && pool == null)
            {
                if (legacyProfile) { role = "mid"; pool = MatchupStatsUpdater.DefaultPool; }
                else { OpenFull("settings"); return; }
            }
            string fileName = matchups ? "draft-matchups-live.js" : "draft-role-rates-live.js";
            string intro = matchups
                ? "Calcule les bilans de ton pool contre chaque adversaire direct, à ton rôle, sur des parties des 30 derniers jours. Plus ton pool est grand, moins chaque champion aura de parties. Le calcul prend environ 65 minutes et tourne en arrière-plan."
                : "Compte les rôles joués sur 1 000 parties récentes, pour mieux deviner le mid adverse. Le calcul prend environ 40 minutes et tourne en arrière-plan.";
            using (RiotKeyDialog dialog = new RiotKeyDialog(AppIcon, OpenExternal, "DRAFT : mettre à jour les " + what, intro))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                key = dialog.Key;
                region = dialog.SelectedRegion;
            }
            CancellationTokenSource cts = new CancellationTokenSource();
            rolesCts = cts;
            rolesStatus = "Calcul des " + what + " : démarrage...";
            RefreshUi();
            SynchronizationContext ui = SynchronizationContext.Current;
            RiotCrawler updater = matchups
                ? (RiotCrawler)new MatchupStatsUpdater(new RiotWebFetcher(key), region, Path.Combine(dataDir, fileName), pool, role) { StatePath = Path.Combine(dataDir, "draft-matchups-state-" + role + ".json") }
                : new RoleRatesUpdater(new RiotWebFetcher(key), region, Path.Combine(dataDir, fileName)) { StatePath = Path.Combine(dataDir, "draft-role-rates-state.json") };
            int shown = 0;
            updater.Progress = (done, total) =>
            {
                if (done - shown < 10 && done < total) return;
                shown = done;
                ui.Post(_ => { if (rolesCts == null || rolesCts.IsCancellationRequested) return; rolesStatus = "Calcul des " + what + " : " + done + " sur " + total + " parties"; RefreshUi(); }, null);
            };
            RoleUpdateResult r = await Task.Run(() => updater.Run(cts.Token));
            rolesCts = null;
            cts.Dispose();
            if (Quitting) return;
            if (r.Saved)
            {
                // Les noms de champions viennent de l'API Riot : le fichier est relu et réécrit avant d'atteindre la page.
                if (!DataScripts.Publish(fileName, dataDir, webDir)) Program.Log(dataDir, "Fichier de données ignoré, contenu non valide : " + fileName);
                rolesReloadPending = true;
                ReloadRolesIfIdle();
            }
            rolesStatus = r.UpToDate ? (matchups ? "Matchups" : "Rôles") + " déjà à jour : aucune requête dépensée."
                : r.Saved ? (matchups ? "Matchups mis à jour : " : "Rôles mis à jour : ") + r.Games + " parties, " + r.Champions + " champions."
                : r.Error != "" ? r.Error
                : "Calcul arrêté : " + r.Games + " parties, trop peu pour remplacer les données actuelles.";
            if (r.Saved && r.Error != "") rolesStatus += " " + r.Error;
            tray.ShowBalloonTip(5000, "DRAFT", rolesStatus, r.Saved || r.UpToDate ? ToolTipIcon.Info : ToolTipIcon.Warning);
            RefreshUi();
        }

        // Recharger les panneaux efface leur état : jamais pendant une draft.
        void ReloadRolesIfIdle()
        {
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
                    else MessageBox.Show("DRAFT n'a pas pu démarrer. Détail dans " + Path.Combine(dataDir, "draftmid.log"), "DRAFT", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            w.Navigate("https://draftmid.local/index.html?" + query);
            await nav.Task;
            done.TrySetResult(true);
            return f;
        }

        public const string AppOrigin = "https://draftmid.local/";
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
                    // Une position sur un écran débranché est ignorée.
                    if (mons.Any(m => m.Work.Overlaps(saved))) r = saved;
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
            placementNote = "Position manuelle conservée. Réactive le positionnement automatique dans les réglages.";
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
            if (!frameValid) { tray.ShowBalloonTip(3000, "DRAFT", "Le client doit être visible à l'écran pour retenir cette vue.", ToolTipIcon.Info); return; }
            if (!detector.Teach(frame, isDraft)) { tray.ShowBalloonTip(3000, "DRAFT", "Aucun portrait au centre : cet écran n'a pas été retenu comme une draft.", ToolTipIcon.Info); return; }
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
            string phase = PhaseText.Of(machine.Current);
            miPhase.Text = phase;
            miUpdate.Enabled = updateCts == null;
            miUpdate.Text = updateCts != null ? "Mise à jour en cours..." : "Rechercher une mise à jour...";
            miRoles.Text = rolesCts != null ? "Arrêter le calcul en cours" : "Mettre à jour les rôles...";
            miMatchups.Visible = rolesCts == null;
            miShow.Text = (panelsShown ? "Masquer" : "Afficher") + " les panneaux" + (hotkey != null && hotkey.Registered ? " (Ctrl+Alt+D)" : "");
            miCompact.Checked = settings.Compact;
            miFollow.Checked = settings.Follow;
            miAutoDraft.Checked = settings.AutoDraft;
            miAutostart.Checked = Autostart.IsOn();
            string tip = "DRAFT : " + phase;
            tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
            BroadcastHost();
        }

        string HostJson()
        {
            return new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
                { "kind", "host" }, { "autostart", Autostart.IsOn() }, { "follow", settings.Follow }, { "autoDraft", settings.AutoDraft },
                { "topmost", settings.Topmost }, { "compact", settings.Compact }, { "placement", placementNote }, { "phase", PhaseText.Of(machine.Current) },
                { "hotkey", hotkey != null && hotkey.Registered }, { "diagnostics", settings.Diagnostics }, { "lcuEnabled", settings.LcuEnabled }, { "lcuConfirmed", LcuGate.ReadingEnabled }, { "lcuStatus", LcuStatus() }, { "legacyProfile", legacyProfile }, { "rolesRunning", rolesCts != null }, { "rolesStatus", rolesStatus }, { "assignedRole", assignedRole }, { "savedProfiles", ProfilesCopy() }, { "historyRunning", historyCts != null }, { "historyStatus", historyStatus }, { "historyHasMore", historyHasMore }, { "updateRunning", updateCts != null }, { "updateStatus", updateStatus } });
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
            if (!historyHasMore || !historyStatus.Contains("2 parties ajoutées")) return "(état : " + historyStatus + ")";
            HistoryImportResult second = await RunHistoryImport(sim, RiotRegion.All[0], new List<string> { "Test#EUW" }, 2, 0);
            if (second.Added != 1 || second.Total != 3 || second.HasMore) return "(suite : " + second.Added + "/" + second.Total + ")";
            if (!File.Exists(Path.Combine(webDir, HistoryImporter.FileName))) return "(fichier absent de la page)";
            if (File.ReadAllText(statsPath) != statsBefore) return "(draft-stats.js modifié)";
            if (!hadStats) File.Delete(statsPath);

            // La vraie fenêtre, ouverte par le vrai bouton des réglages. Une fausse clé est saisie pour la capture, puis on annule : aucun appel.
            string windowProblem = await TestHistoryWindow(fakeKey);
            if (windowProblem != "") return windowProblem;
            foreach (string file in Directory.GetFiles(dataDir, "*", SearchOption.TopDirectoryOnly))
            {
                string text;
                try { using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (StreamReader r = new StreamReader(fs)) text = r.ReadToEnd(); }
                catch (IOException) { continue; }
                if (text.Contains(fakeKey) || text.Contains("secret-moi") || text.Contains("NomPrive")) return "(trace dans " + Path.GetFileName(file) + ")";
            }
            return "";
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
            if (!await wait("state.assignedRole==='jungle' && myRole()==='mid' && document.querySelector('.role-hint')")) return "(proposition absente)";
            await run("savePrefs({autoRole:'auto'}); 1");
            TestSetAssignedRole(null);
            TestSetAssignedRole("jungle");
            if (!await wait("myRole()==='jungle'")) return "(changement automatique absent)";
            await run("switchRole('mid','manual'); 1");
            TestSetAssignedRole("support"); TestSetAssignedRole("jungle");
            if (!await wait("myRole()==='mid'")) return "(la correction manuelle n'est pas respectée)";
            TestSetAssignedRole(null);
            await run("savePrefs({autoRole:'suggest'}); resetRoleSession(); 1");
            // Après un rechargement, les réglages doivent toujours s'ouvrir par le bouton du panneau.
            OpenFull("settings");
            if (!await wait("document.querySelector('.settings')")) return "(réglages impossibles à ouvrir après rechargement : " + await run("location.search + ' ' + document.readyState") + ")";
            await run("document.querySelector('.settings .sec-title .ghost-btn').click(); 1");
            return "";
        }

        // Protections de la page, vérifiées dans la vraie WebView2 : CSP réellement appliquée, eval interdit,
        // réseau et cadres limités, navigation et fenêtres vers un site inconnu refusées. Aucun accès réseau réel (.invalid).
        public string DetailOpenReport = "non mesuré";

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

            int lost = 0, notShown = 0, lostOnClose = 0;
            for (int k = 0; k < openings; k++)
            {
                PanelForm full;
                if (panels.TryGetValue("full", out full) && full.Visible)
                {
                    // Fermeture de la grande fenêtre : les panneaux gardent la draft.
                    full.Hide();
                    await Task.Delay(150);
                    foreach (PanelForm p in sources)
                        if (await run(p, "state.enemies.join()") != Expected) { lostOnClose++; await run(sources[0], "draftApi.reset(); ['Ahri','Sion','Vi'].forEach(c => draftApi.add(c)); 1"); await Task.Delay(300); break; }
                }
                OpenFull(k % 2 == 0 ? "analyse" : "compare");
                for (int i = 0; i < 200 && !(panels.TryGetValue("full", out full) && full.Visible && full.Ready.IsCompleted); i++) await Task.Delay(50);
                // La grande fenêtre doit afficher la vraie draft, puis laisser le temps à une éventuelle diffusion parasite.
                if (!await waitIn(full, "state.enemies.join() === 'Ahri,Sion,Vi'", 3000)) notShown++;
                await Task.Delay(400);
                foreach (PanelForm p in sources)
                    if (await run(p, "state.enemies.join()") != Expected) { lost++; await run(sources[0], "draftApi.reset(); ['Ahri','Sion','Vi'].forEach(c => draftApi.add(c)); 1"); await Task.Delay(300); break; }
            }
            DetailOpenReport = openings + " ouvertures (Analyse détaillée / Comparer), " + lost + " perte(s) de draft, " + lostOnClose + " perte(s) à la fermeture, " + notShown + " fenêtre(s) sans la draft";
            if (lost > 0) problem += lost + " perte(s) de draft sur " + openings + " ouvertures ; ";
            if (lostOnClose > 0) problem += lostOnClose + " perte(s) de draft à la fermeture de la grande fenêtre ; ";
            if (notShown > 0) problem += notShown + " ouverture(s) sans la draft en cours ; ";

            // Simulateur : Draft aléatoire reste dans la grande fenêtre, la draft en cours n'est pas touchée.
            PanelForm f;
            panels.TryGetValue("full", out f);
            if (f != null && await waitIn(f, "document.querySelector('[data-random-draft]')", 3000))
            {
                await run(f, "document.querySelector('[data-random-draft]').click(); 1");
                if (!await waitIn(f, "state.enemies.length === 5 && !!document.querySelector('[data-simulation]')", 3000)) problem += "le simulateur ne se lance pas ou n'est pas signalé ; ";
                await Task.Delay(400);
                foreach (PanelForm p in sources) if (await run(p, "state.enemies.join()") != Expected) { problem += "Draft aléatoire écrase la draft du panneau " + p.Id + " ; "; break; }
                await run(sources[0], "draftApi.add('Jinx'); 1");
                await Task.Delay(300);
                if (await run(f, "state.enemies.includes('Jinx')") == "true") problem += "la simulation est modifiée par la draft en cours ; ";
                await run(f, "document.querySelector('[data-simulation] button').click(); 1");
                if (!await waitIn(f, "state.enemies.join() === 'Ahri,Sion,Vi,Jinx' && !document.querySelector('[data-simulation]')", 3000)) problem += "Revenir à la draft en cours ne la retrouve pas ; ";
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

        async Task<string> TestHistoryWindow(string fakeKey)
        {
            string path = Path.Combine(dataDir, HistoryImporter.FileName), before = File.ReadAllText(path), status = historyStatus, layout = "";
            bool seen = false, blocked = false, allowed = false;
            OpenFull("settings");
            PanelForm f = null;
            for (int i = 0; i < 200 && !(panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted); i++) await Task.Delay(50);
            if (f == null || f.View.CoreWebView2 == null) return "(réglages absents)";
            const string Find = "[...document.querySelectorAll('.settings button')].find(b => /historique Riot|plus de parties/.test(b.textContent))";
            bool found = false;
            for (int i = 0; i < 120 && !found; i++) { found = await f.View.CoreWebView2.ExecuteScriptAsync("!!(" + Find + ")") == "true"; if (!found) await Task.Delay(50); }
            if (!found)
            {
                string pageText = await f.View.CoreWebView2.ExecuteScriptAsync("(document.querySelector('.settings') ? 'réglages ouverts : ' : 'réglages fermés : ') + location.search + ' | ' + document.body.innerText.slice(0, 160)");
                return "(bouton absent des réglages : " + pageText + ")";
            }
            string file = await f.View.CoreWebView2.ExecuteScriptAsync("(window.DRAFT_HISTORY && window.DRAFT_HISTORY.rows.length) || 0");
            if (file != "3") return "(la page lit " + file + " parties)";
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 300 };
            timer.Tick += (s, e) =>
            {
                HistoryImportDialog d = Application.OpenForms.OfType<HistoryImportDialog>().FirstOrDefault();
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
                    string js = "JSON.stringify({version:typeof APP_VERSION!=='undefined'?APP_VERSION:null,actif:PROFILES.active,parRole:Object.fromEntries(PROFILE_ROLES.map(r=>[r,PROFILES.roles[r].pool.length])),ancienProfil:!!localStorage.getItem('draftProfile'),profilsEnregistres:!!localStorage.getItem('draftProfiles'),historique:window.DRAFT_HISTORY&&Array.isArray(window.DRAFT_HISTORY.rows)?window.DRAFT_HISTORY.rows.length:0,journal:loadJournal().length,niveau:PREFS.coachLevel,role:myRole()})";
                    result = new JavaScriptSerializer().Deserialize<string>(await f.View.CoreWebView2.ExecuteScriptAsync(js));
                }
            }
            catch (Exception e) { if (e is OutOfMemoryException) throw; result = "{\"erreur\":\"" + e.GetType().Name + "\"}"; }
            try { File.WriteAllText(path, result, new UTF8Encoding(false)); } catch { }
            Quit();
        }

        // Ouvre les réglages comme le bouton du panneau, puis vérifie qu'ils se ferment des deux façons.
        public string KeyStressReport = "non mesuré";

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
            Func<Task> findWindow = async () => { for (int i = 0; i < 200 && !(panels.TryGetValue("full", out f) && f.Visible && f.Ready.IsCompleted); i++) await Task.Delay(50); };
            Func<string, string, int, string, Task> press = async (key, code, vk, text) =>
            {
                string common = "\"key\":\"" + key + "\",\"code\":\"" + code + "\",\"windowsVirtualKeyCode\":" + vk;
                await f.View.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", "{\"type\":\"" + (text == "" ? "rawKeyDown" : "keyDown") + "\"," + common + (text == "" ? "" : ",\"text\":\"" + text + "\"") + "}");
                await f.View.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", "{\"type\":\"keyUp\"," + common + "}");
                await Task.Delay(keyPause);
            };
            Func<string, Task> type = async word => { foreach (char c in word) await press(c.ToString(), "Key" + char.ToUpperInvariant(c), char.ToUpperInvariant(c), c.ToString()); };
            const string Search = "document.querySelector('.pool-search')", Close = "document.querySelector('.settings .sec-title .ghost-btn')";

            // 1. Fenêtre principale ouverte, puis Réglages : elle n'est ni rechargée, ni masquée.
            OpenFull("");
            await findWindow();
            if (!await waitFor("document.querySelector('.app')")) return "fenêtre principale absente ; ";
            string profileBefore = await run("localStorage.getItem('draftProfile')");
            await run("window.keepMark = 1; state.enemies.length || addEnemy('Sett'); 1");
            await waitFor("state.enemies.length > 0");
            OpenFull("settings");
            if (!await waitFor(Close)) return "réglages absents ; ";
            if (await run("window.keepMark === 1") != "true") problem += "la page principale est rechargée à l'ouverture ; ";
            if (await run("[...document.querySelectorAll('.settings button')].some(b => b.textContent.includes('Rechercher une mise à jour'))") != "true") problem += "bouton de mise à jour absent ; ";

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
            if (!await waitFor("[...document.querySelectorAll('.pool-chip.on')].some(b => b.textContent.includes('Ahri'))")) problem += "Entrée ne sélectionne pas le champion ; ";
            if (!(await run("localStorage.getItem('draftProfile')")).Contains("Ahri")) problem += "le champion n'est pas enregistré ; ";
            if (await run(Search + ".value") != "\"\"") problem += "la recherche ne se vide pas ; ";
            if (await run("document.activeElement === " + Search) != "true") problem += "le focus quitte la recherche après la sélection ; ";
            await run("document.querySelector('.pool-chip.on[aria-label=\"Retirer Ahri\"]').click(); 1");
            await Task.Delay(150);
            if (await run("localStorage.getItem('draftProfile')") != profileBefore) problem += "le profil n'est pas rétabli après retrait ; ";

            // 4. Échap et Fermer ne ferment que les réglages, et le focus revient à la fenêtre principale.
            await run("document.querySelector('.settings .sec-title .ghost-btn').focus(); 1");
            await press("Escape", "Escape", 27, "");
            if (!await waitFor("!document.querySelector('.settings')")) problem += "Échap ne ferme pas les réglages ; ";
            if (await run("state.enemies.join()") != enemiesBefore) problem += "Échap efface la draft ; ";
            OpenFull("settings");
            await waitFor(Close);
            await run(Close + ".click(); 1");
            if (!await waitFor("!document.querySelector('.settings')")) problem += "Fermer ne ferme pas les réglages ; ";
            await Task.Delay(200);
            if (!f.Visible) problem += "Fermer masque la fenêtre principale ; ";
            if (await run("!!document.querySelector('.app') && window.keepMark === 1") != "true") problem += "la page principale est perdue ; ";
            await press("a", "KeyA", 65, "a");
            if (await run("(document.getElementById('add') || {}).value") != "\"a\"") problem += "la fenêtre principale ne reçoit plus le clavier ; ";

            // 5. Ouverts depuis le menu, fenêtre masquée : la fenêtre principale reste ouverte derrière les réglages.
            f.Hide();
            OpenFull("settings");
            await findWindow();
            await waitFor(Close);
            await run(Close + ".click(); 1");
            await waitFor("!document.querySelector('.settings')");
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
            await waitFor(Close);
            await run(Close + ".click(); 1");
            for (int i = 0; i < 60 && f.Visible; i++) await Task.Delay(50);
            if (!panelsShown) problem += "les panneaux de test ne sont pas restés affichés ; ";
            if (f.Visible) problem += "pendant une draft, Fermer laisse la grande fenêtre ouverte ; ";

            // 6 bis. Fenêtre principale déjà ouverte pendant une draft, page en plein rechargement (après un import, par exemple) :
            //    fermer les réglages doit ramener à la fenêtre principale, pas la faire disparaître.
            OpenFull("");
            await findWindow();
            await waitFor("document.querySelector('.app')");
            f.View.CoreWebView2.Reload();
            OpenFull("settings");
            await waitFor(Close);
            await Task.Delay(300);
            await waitFor(Close);
            await run(Close + ".click(); 1");
            await waitFor("!document.querySelector('.settings')");
            await Task.Delay(300);
            if (!f.Visible) problem += "fenêtre principale ouverte, page en rechargement : Fermer fait disparaître la fenêtre principale ; ";

            // 6 ter. Réglages ouverts depuis un panneau pendant une draft, puis la draft se termine avant la fermeture :
            //    plus aucun panneau à l'écran, DRAFT ne doit pas sembler fermé.
            f.Hide();
            OpenFull("settings");
            await findWindow();
            await waitFor(Close);
            await showPanels(false);
            await run(Close + ".click(); 1");
            for (int i = 0; i < 20; i++) await Task.Delay(50);
            if (!panels.Values.Any(p => p.Visible)) problem += "draft terminée pendant les réglages : Fermer ne laisse plus aucune fenêtre DRAFT ; ";
            visibility.Manual = manualBefore;
            ApplyVisibility();

            // Simulation : le bouton de la page de draft donne une draft complète, différente à chaque clic.
            OpenFull("");
            await findWindow();
            await waitFor("document.querySelector('[data-random-draft]')");
            await run("document.querySelector('[data-random-draft]').click(); 1");
            if (!await waitFor("state.enemies.length === 5 && V.shown")) problem += "Draft aléatoire ne remplit pas la draft ; ";
            string firstDraft = await run("state.enemies.join()");
            bool changed = false;
            for (int i = 0; i < 5 && !changed; i++)
            {
                await run("document.querySelector('[data-random-draft]').click(); 1");
                await Task.Delay(100);
                changed = await run("state.enemies.join()") != firstDraft && await run("state.enemies.length") == "5";
            }
            if (!changed) problem += "Draft aléatoire redonne toujours la même draft ; ";
            await run("resetDraft(); 1");

            // Les mots disent ce que fait l'application, et suivent le rôle du joueur.
            await run("resetDraft(); ['Yasuo','Yone','Lee Sin','Jinx','Thresh'].forEach(addEnemy); 1");
            await waitFor("state.enemies.length === 5 && document.querySelector('.stage-body .cta')");
            if (await run("document.querySelector('.stage-body .cta').textContent.includes('Je joue')") != "true") problem += "le bouton de choix ne dit pas « Je joue » ; ";
            if (await run("![...document.querySelectorAll('.badge')].some(b => b.textContent.includes('Provisoire'))") != "true") problem += "un badge mélange encore les incertitudes ; ";
            if (await run("!document.querySelector('.res') || !!state.current") != "true") problem += "les boutons de résultat s'affichent sans choix ; ";
            string support = await run("(() => { const before = PROFILE; applyProfile({ role:'support', pool:['Lux', 'Soraka'] }); computeVerdict(); const text = hypothesisText(); applyProfile(before); computeVerdict(); return text; })()");
            if (!support.Contains("Support") || support.Contains("Mid")) problem += "le texte d'adversaire ignore le rôle (" + support + ") ; ";
            await run("resetDraft(); 1");

            // 7. La croix masque la fenêtre sans quitter l'application.
            OpenFull("");
            await findWindow();
            f.Close();
            await Task.Delay(200);
            if (f.Visible || f.IsDisposed || Quitting) problem += "la croix ne masque pas proprement ; ";
            OpenFull("");
            await findWindow();
            if (!f.Visible || !await waitFor("document.querySelector('.app')")) problem += "réouverture impossible ; ";
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
                if (!await waitFor(Close)) problem += "fenêtre recréée sans les réglages ; ";
                else { await run(Close + ".click(); 1"); await waitFor("!document.querySelector('.settings')"); }
                f.Hide();
            }
            return problem;
        }

        async void OpenFull(string tab)
        {
            bool exists = panels.ContainsKey("full");
            PanelForm open;
            // Fenêtre principale déjà à l'écran : les réglages s'ouvrent par-dessus, sans la recharger.
            if (tab == "settings" && exists && panels.TryGetValue("full", out open) && open.Visible && open.Ready.IsCompleted && open.View.CoreWebView2 != null)
            {
                open.View.CoreWebView2.PostWebMessageAsJson("{\"kind\":\"openSettings\"}");
                if (opts.Script == null) open.Activate();
                // Une page en plein rechargement (après un import, par exemple) perd ce message : on vérifie, puis on recharge sur les réglages.
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(50);
                    if (open.IsDisposed || open.View.CoreWebView2 == null) return;
                    if (await open.View.CoreWebView2.ExecuteScriptAsync("!!document.querySelector('.settings')") == "true") return;
                }
                // La fenêtre principale était déjà à l'écran : fermer les réglages doit y revenir, jamais la masquer (pas de solo=1).
                open.View.CoreWebView2.Navigate("https://draftmid.local/index.html?panel=full&tab=settings");
                return;
            }
            // Pendant une draft, la fenêtre ne doit pas rester devant le client une fois les réglages fermés.
            string q = "panel=full" + (tab == "" ? "" : "&tab=" + tab) + (tab == "settings" && panelsShown ? "&solo=1" : "");
            await EnsureEnv();
            if (env == null) return;
            PanelForm f = await Ensure("full", q);
            if (exists)
            {
                // La fenêtre n'apparaît qu'une fois la page chargée : sinon un clic sur l'ancienne page se perdrait.
                TaskCompletionSource<bool> loaded = new TaskCompletionSource<bool>();
                EventHandler<CoreWebView2NavigationCompletedEventArgs> done = null;
                done = (s, e) => { f.View.CoreWebView2.NavigationCompleted -= done; loaded.TrySetResult(true); };
                f.View.CoreWebView2.NavigationCompleted += done;
                f.View.CoreWebView2.Navigate("https://draftmid.local/index.html?" + q);
                await Task.WhenAny(loaded.Task, Task.Delay(3000));
                if (f.IsDisposed) return;
            }
            Monitor m = ClientWindow.Monitors().OrderByDescending(x => x.Work.W).First();
            if (!f.Visible) f.Show();
            f.PlaceNoActivate(new Native.RECT(m.Work.Left + 40, m.Work.Top + 20, m.Work.Left + 40 + Math.Min((int)(1280 * m.Scale), m.Work.W - 80), m.Work.Top + 20 + Math.Min((int)(820 * m.Scale), m.Work.H - 40)), false);
            if (opts.Script == null) f.Activate();
        }

        void ImportStats()
        {
            using (OpenFileDialog d = new OpenFileDialog { Title = "Choisir draft-stats.js", Filter = "Statistiques DRAFT|draft-stats.js|Tous les fichiers|*.*" })
            {
                if (d.ShowDialog() != DialogResult.OK) return;
                // Seules des données passent : le fichier est relu comme du JSON, puis réécrit par DRAFT.
                string safe = DataScripts.ReadCanonical("DRAFT_STATS", d.FileName);
                if (safe == null) { MessageBox.Show("Ce fichier n'est pas un import DRAFT valide : rien n'a été modifié.", "DRAFT"); return; }
                File.WriteAllText(Path.Combine(dataDir, "draft-stats.js"), safe, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(webDir, "draft-stats.js"), safe, new UTF8Encoding(false));
                foreach (PanelForm f in panels.Values) if (f.View.CoreWebView2 != null) f.View.CoreWebView2.Reload();
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
                case "hideAll": ToggleManual(); break;
                case "updateRoles": ToggleRiotUpdate(false); break;
                case "updateMatchups": ToggleRiotUpdate(true); break;
                case "checkUpdate": CheckForUpdate(); break;
                case "importHistory": ToggleHistoryImport(); break;
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
                case "openFull": OpenFull(m.ContainsKey("tab") ? m["tab"] as string ?? "" : ""); break;
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
                    if (await target.View.CoreWebView2.ExecuteScriptAsync("typeof V !== \"undefined\" && !!document.querySelector(\".panel\")") == "true") break;
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
                log.AppendLine("CLIENT " + (ClientOpen ? tracker.Rect.ToString() : "absent") + " | " + placementNote);
            }
            catch (Exception e) { log.AppendLine("EXC " + e); }
            if (opts.Out != null) File.WriteAllText(opts.Out, log.ToString());
            Quit();
        }

        // ---------- Mise à jour à la demande : voir AppUpdater ----------
        CancellationTokenSource updateCts;
        string updateStatus = "";

        void SetUpdateStatus(string text) { updateStatus = text; RefreshUi(); }

        // Jamais pendant une draft, une partie, un import ou un calcul : l'application redémarre.
        string UpdateBlocked()
        {
            if (machine.Current == Phase.Draft || machine.Current == Phase.InGame) return "Une draft ou une partie est en cours : relance la mise à jour après.";
            if (rolesCts != null || historyCts != null) return "Un import ou un calcul est en cours : attends sa fin.";
            return null;
        }

        public void CheckForUpdate()
        {
            RunUpdate(new GitHubUpdateSource(), info => MessageBox.Show("La version " + info.Version.ToString(3) + " est disponible (tu as la " + AppUpdater.Current.ToString(3) + ").\n\nDRAFT va la télécharger, la vérifier, puis redémarrer. Tes profils, ton historique et tes réglages sont conservés.\n\nInstaller maintenant ?", "DRAFT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
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

        async void RunUpdate(IUpdateSource source, Func<UpdateInfo, bool> confirm)
        {
            if (updateCts != null) return;
            string blocked = UpdateBlocked();
            if (blocked != null) { SetUpdateStatus(blocked); return; }
            CancellationTokenSource cts = new CancellationTokenSource();
            updateCts = cts;
            string done = null;
            try
            {
                SetUpdateStatus("Recherche d'une mise à jour...");
                UpdateInfo info = await source.LatestAsync(cts.Token);
                if (!AppUpdater.IsNewer(AppUpdater.Current, info.Version)) { done = "DRAFT est à jour (version " + AppUpdater.Current.ToString(3) + ")."; return; }
                if (!confirm(info)) { done = "La version " + info.Version.ToString(3) + " est disponible."; return; }
                int shown = -1;
                Progress<long> progress = new Progress<long>(bytes =>
                {
                    int percent = (int)(bytes * 100 / Math.Max(1, info.Size));
                    if (percent / 5 != shown && updateCts != null) { shown = percent / 5; SetUpdateStatus("Téléchargement : " + percent + " %"); }
                });
                IProgress<long> checking = new Progress<long>(_ => SetUpdateStatus("Vérification de la nouvelle version..."));
                string candidate = await AppUpdater.Prepare(source, info, exePath, dataDir, progress, (c, d, ct) => { checking.Report(0); AppUpdater.SelfCheck(c, d, ct); }, cts.Token);
                blocked = UpdateBlocked();
                if (blocked != null) { File.Delete(candidate); done = blocked; return; }
                AppUpdater.Swap(exePath, candidate);
                // La nouvelle version est lancée une fois l'application fermée : voir Program.Run.
                Program.Relaunch = new[] { exePath, RelaunchArguments("--after-update", Process.GetCurrentProcess().Id.ToString()), RelaunchArguments("--update-rolled-back") };
                updateCts = null;
                Quit();
            }
            catch (OperationCanceledException) { done = ""; }
            catch (Exception e)
            {
                if (e is OutOfMemoryException) throw;
                Program.Log(dataDir, "Mise à jour : " + e.GetType().Name);
                done = cts.IsCancellationRequested ? "" : AppUpdater.FriendlyError(e);
            }
            finally
            {
                if (updateCts == cts) updateCts = null;
                if (done != null && !Quitting)
                {
                    SetUpdateStatus(done);
                    if (opts.TestUpdate != null) { if (opts.Out != null) File.WriteAllText(opts.Out, "RESULTAT : " + done); Quit(); }
                }
            }
        }

        public async void TestUpdate(string localFile) { await Task.Delay(100); RunUpdate(new LocalUpdateSource(localFile), info => true); }

        public void AnnounceRolledBack()
        {
            updateStatus = "La nouvelle version n'a pas démarré : la précédente a été remise en place.";
            RefreshUi();
            if (!opts.Silent) tray.ShowBalloonTip(5000, "DRAFT", updateStatus, ToolTipIcon.Warning);
        }

        public void AnnounceUpdated()
        {
            updateStatus = "Mise à jour installée : version " + AppUpdater.Current.ToString(3) + ".";
            RefreshUi();
            if (!opts.Silent) tray.ShowBalloonTip(5000, "DRAFT", updateStatus, ToolTipIcon.Info);
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
                ? midGuess == "null" && (await js("V.confidence === 'faible' && V.res.ranking[0].byScenario.length >= 2 && hypothesisText().includes('pas identifié')")) == "true"
                : midGuess.Contains("Ahri") && (await js("V.confidence !== 'élevée'")) == "true", "Scénario 7. Sett, Galio, Ahri : aucune certitude affichée (" + midGuess + ")");
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
            t(fullProblem == "", "Réglages et fenêtre principale : frappes, sélection, enregistrement et fermeture " + fullProblem);
            string profileProblem = await app.TestProfilesAndRole();
            t(profileProblem == "", "Profils par rôle et rôle attribué par LoL, dans la vraie fenêtre " + profileProblem);
            string historyProblem = await app.TestHistoryImport();
            t(historyProblem == "", "Import Riot simulé depuis l'application, sans appel réel, sans trace de clé " + historyProblem);
            string detailProblem = await app.TestDraftSurvivesDetail(50);
            t(detailProblem == "", "Analyse détaillée, Comparer et simulateur n'effacent jamais la draft en cours (" + app.DetailOpenReport + ") " + detailProblem);
            string securityProblem = await app.TestPageSecurity();
            t(securityProblem == "", "Protections de la page dans la vraie WebView2 : CSP, eval, réseau, cadres, navigation " + securityProblem);
        }

        static string s_keyReport = "";

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

                Stopwatch appear = Stopwatch.StartNew();
                feed(Evidence.Draft, 2);
                for (int i = 0; i < 200 && !(app.PanelFor("left") != null && app.PanelFor("left").Visible); i++) await Task.Delay(50);
                s.AppendLine("MESURE : panneaux visibles " + appear.ElapsedMilliseconds + " ms après la détection");
                await Task.Delay(300);
                t(app.PanelsShown && app.CurrentPhase == Phase.Draft, "4. Début de draft : panneaux affichés");
                t(app.PanelFor("left").Visible && app.PanelFor("right").Visible, "4. Les deux panneaux sont visibles");
                Native.RECT c = app.Tracker.Rect, l = rectOfLeft(), r = rectOfRight();
                t(l.Right <= c.Left && r.Left >= c.Right && c.Left - l.Right <= 10 && r.Left - c.Right <= 10, "4. Panneaux juste de chaque côté, sans chevauchement (" + l + " | " + c + " | " + r + ")");
                t(l.Top == c.Top && l.H == c.H, "4. Hauteur et alignement sur le client");
                t(!PanelHasFocus(app), "Aucun vol de focus à l'apparition");
                if (o.NoHotkey) t(!app.HotkeyCreated, "Raccourci laissé à l'autre version : aucune tentative"); else t(app.HotkeyRegistered, "Raccourci Ctrl+Alt+D enregistré");

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
                for (int i = 0; i < 100 && await js("typeof V !== \"undefined\" && !!document.querySelector(\".panel\")") != "true"; i++) await Task.Delay(50);
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
            }
            catch (Exception e) { fails++; s.AppendLine("EXC " + e); }
            if (fake != null && !fake.IsDisposed) { fake.Close(); fake.Dispose(); }
            s.AppendLine("MESURE : recherche de champions : " + s_keyReport);
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
            DialogResult r = MessageBox.Show("DRAFT a besoin du composant gratuit Microsoft Edge WebView2, absent de ce PC.\n\nOuvrir la page de Microsoft pour l'installer ? Choisis « Evergreen Bootstrapper », puis relance DRAFT.", "DRAFT", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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
                k.SetValue("DisplayVersion", "2.16.2");
                k.SetValue("Publisher", "DRAFT");
                k.SetValue("DisplayIcon", target);
                k.SetValue("InstallLocation", dir);
                k.SetValue("UninstallString", "\"" + target + "\" --uninstall");
                k.SetValue("NoModify", 1); k.SetValue("NoRepair", 1);
            }
            if (o.TestInstall) return;
            MessageBox.Show("DRAFT est installé. Tu le retrouves dans le menu Démarrer.", "DRAFT");
            Process.Start(new ProcessStartInfo(target) { WorkingDirectory = dir, UseShellExecute = true });
        }

        public static void Uninstall(Options o, string self)
        {
            string dir = o.InstallDir ?? DefaultDir;
            if (!o.TestInstall && MessageBox.Show("Désinstaller DRAFT ? Ton journal et tes préférences sont conservés.", "DRAFT", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
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

        static int Run(string[] args)
        {
            Options o = Options.Parse(args);
            Native.SetProcessDpiAwarenessContext(Native.PMV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string self = Application.ExecutablePath;
            string dataDir = o.DataDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DraftMid");
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
                if (!o.Silent) MessageBox.Show("DRAFT ne peut pas écrire dans ce dossier :\n" + dataDir + "\n\nVérifie ses droits, ou que le disque n'est pas plein.", "DRAFT", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 2;
            }
            logDir = dataDir;
            if (o.RunKey != null) Autostart.Key = o.RunKey;

#if DRAFTMID_PROTOTYPES
            if (o.OpggPrototype || o.OpggTest)
            {
                // Prototype isolé : jamais sur les données de l'application, car l'extraction y remplacerait la version installée.
                if (o.DataDir == null) { if (!o.Silent) MessageBox.Show("Prototype OP.GG : indique un dossier de test avec --data.", "DRAFT"); return 2; }
                RuntimeHelp.EnsureLoader(Extract(dataDir));
                if (o.OpggTest) { OpggTests.Run(o, dataDir); Application.Run(); return 0; }
                Application.Run(new OpggBrowserForm(OpggLinks.Home, Path.Combine(dataDir, "wv"), Path.Combine(dataDir, "opgg-profile.json")));
                return 0;
            }
#endif
            if (o.SelfTest) return SelfTest.Run(o, dataDir, self);
            if (o.Uninstall) { Installer.Uninstall(o, self); return 0; }
            if (o.Install || (!o.TestInstall && Path.GetFileName(self).IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0)) { Installer.Install(o, self); return 0; }
            if (o.TestInstall) { Installer.Install(o, self); return 0; }
            if (o.TestUpdateLive) return TestUpdateLive(o, dataDir);
            if (o.TestSignature != null) return AppUpdater.ValidSignature(o.TestSignature[1], AppUpdater.Sha256Of(o.TestSignature[0]), File.ReadAllText(o.TestSignature[2]), AppUpdater.ReleaseKey) ? 0 : 1;

            bool created;
            using (Mutex single = new Mutex(true, o.DataDir == null ? "DraftMid.Instance" : "DraftMid.Test." + o.DataDir.GetHashCode(), out created))
            {
                if (!created)
                {
                    if (!o.Silent) MessageBox.Show("DRAFT est déjà ouvert : utilise son icône près de l'horloge.", "DRAFT");
                    return 0;
                }
                string webDir = Extract(dataDir);
                string runtime = RuntimeHelp.Version(webDir);
                if (o.CheckRuntime) { File.WriteAllText(o.Out, runtime ?? "absent"); return 0; }
                if (runtime == null && !o.Silent) { RuntimeHelp.Explain(); return 0; }
                CompanionApp app = new CompanionApp(o, dataDir, webDir, self, o.EndToEnd ? EndToEnd.Matcher : ClientWindowTracker.IsRealClient);
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
