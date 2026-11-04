// DraftMid : application compagnon Windows. Fenêtres natives autour de l'interface web existante.
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
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

[assembly: AssemblyTitle("DraftMid")]
[assembly: AssemblyProduct("DraftMid")]
[assembly: AssemblyDescription("Compagnon de draft League of Legends. Non approuvé par Riot Games.")]
[assembly: AssemblyVersion("2.8.0.0")]
[assembly: AssemblyFileVersion("2.8.0.0")]

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
        public const uint SWP_NOSIZE = 0x1, SWP_NOACTIVATE = 0x10, SWP_NOZORDER = 0x4, SWP_SHOWWINDOW = 0x40;
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
            Text = id == "full" ? "DraftMid" : "DraftMid : " + id;
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
        const int SlowPollMs = 2000, SampleMs = 1000, MoveCoalesceMs = 16, MinUpdateMs = 8, MoveSettleMs = 150, ModeSettleMs = 350;
        long lastClientUpdate;
        readonly string dataDir, webDir, settingsPath, exePath;
        readonly Options opts;
        readonly UserPreferences settings;
        readonly NotifyIcon tray;
        readonly System.Windows.Forms.Timer slowPoll, sampler, moveTimer;
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
        ToolStripMenuItem miRoles, miCompact, miFollow, miAutoDraft, miAutostart, miShow, miPhase;
        public long MoveCount;

        public Phase CurrentPhase { get { return machine.Current; } }
        public bool PanelsShown { get { return panelsShown; } }
        public string ShownMode { get { return shownMode; } }
        public ClientWindowTracker Tracker { get { return tracker; } }
        public bool HotkeyRegistered { get { return hotkey != null && hotkey.Registered; } }
        public UserPreferences Prefs { get { return settings; } }

        public PanelForm PanelFor(string id) { PanelForm f; return panels.TryGetValue(id, out f) ? f : null; }

        public CompanionApp(Options o, string dataDir, string webDir, string exePath, Func<IntPtr, bool> clientMatcher)
        {
            opts = o; this.dataDir = dataDir; this.webDir = webDir; this.exePath = exePath;
            settingsPath = Path.Combine(dataDir, "settings.json");
            settings = UserPreferences.Load(settingsPath);
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
            menu.Items.Add("Importer mes statistiques...", null, (s, e) => ImportStats());
            miRoles = new ToolStripMenuItem("Mettre à jour les rôles...", null, (s, e) => ToggleRolesUpdate());
            menu.Items.Add(miRoles);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miAutostart);
            menu.Items.Add("Quitter", null, (s, e) => Quit());
            tray = new NotifyIcon { Icon = AppIcon ?? SystemIcons.Application, Text = "DraftMid", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += (s, e) => ToggleManual();

            hotkey = new HotkeyWindow();
            hotkey.Pressed += ToggleManual;

            tracker = new ClientWindowTracker(clientMatcher, false);
            // Réaction immédiate au premier événement, puis au plus une mise à jour toutes les 16 ms.
            tracker.Changed += () =>
            {
                long now = Environment.TickCount;
                if (now - lastClientUpdate >= MinUpdateMs) { lastClientUpdate = now; OnClientChanged(); }
                else if (!moveTimer.Enabled) moveTimer.Start();
            };
            moveTimer = new System.Windows.Forms.Timer { Interval = MoveCoalesceMs };
            moveTimer.Tick += (s, e) => { moveTimer.Stop(); lastClientUpdate = Environment.TickCount; OnClientChanged(); };
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
            if (await PollLcuOnce())
            {
                // Le client donne la phase exacte : rien à estimer ni à apprendre à partir de l'image.
                frameValid = false;
                if (settings.Diagnostics) Diagnose(Evidence.Unknown);
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

        // Phase et nombre d'adversaires uniquement : aucun identifiant, aucun nom.
        string LcuDiag()
        {
            if (lcu == null) return "off";
            return lcu.Connected ? lcuInfo : "absent(" + LocalHttpsTransport.LastError + ")";
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
                using (Process p = Process.GetProcessById((int)pid))
                    return LcuFactory.CreateReal(settings.LcuEnabled, Path.GetDirectoryName(p.MainModule.FileName));
            }
            catch { return null; }
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
                if (!s.Available) return false;
                lcuInfo = s.PhaseName + "/" + (s.EnemyChampionIds == null ? "illisible" : s.EnemyChampionIds.Count.ToString());
                Phase before = machine.Current;
                machine.Force(s.Phase == Phase.Uncertain && before == Phase.Draft ? Phase.Draft : s.Phase);
                AfterPhase(before, machine.Current);
                ApplyVisibility();
                PostEnemies(s);
                return true;
            }
            finally { lcuBusy = false; }
        }

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
                else if (before == Phase.Draft) SendAll("{\"kind\":\"draftEnd\"}");
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

        async void ShowPanels()
        {
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

        async void ToggleRolesUpdate()
        {
            if (rolesCts != null)
            {
                rolesCts.Cancel();
                rolesStatus = "Arrêt du calcul en cours...";
                RefreshUi();
                return;
            }
            string key;
            RiotRegion region;
            using (RiotKeyDialog dialog = new RiotKeyDialog(AppIcon, OpenExternal))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                key = dialog.Key;
                region = dialog.SelectedRegion;
            }
            CancellationTokenSource cts = new CancellationTokenSource();
            rolesCts = cts;
            rolesStatus = "Calcul des rôles : démarrage...";
            RefreshUi();
            SynchronizationContext ui = SynchronizationContext.Current;
            RoleRatesUpdater updater = new RoleRatesUpdater(new RiotWebFetcher(key), region, Path.Combine(dataDir, "draft-role-rates-live.js"));
            int shown = 0;
            updater.Progress = (done, total) =>
            {
                if (done - shown < 10 && done < total) return;
                shown = done;
                ui.Post(_ => { if (rolesCts == null || rolesCts.IsCancellationRequested) return; rolesStatus = "Calcul des rôles : " + done + " sur " + total + " parties"; RefreshUi(); }, null);
            };
            RoleUpdateResult r = await Task.Run(() => updater.Run(cts.Token));
            rolesCts = null;
            cts.Dispose();
            if (Quitting) return;
            if (r.Saved)
            {
                File.Copy(Path.Combine(dataDir, "draft-role-rates-live.js"), Path.Combine(webDir, "draft-role-rates-live.js"), true);
                rolesReloadPending = true;
                ReloadRolesIfIdle();
            }
            rolesStatus = r.Saved ? "Rôles mis à jour : " + r.Games + " parties, " + r.Champions + " champions."
                : r.Error != "" ? r.Error
                : "Calcul arrêté : " + r.Games + " parties, trop peu pour remplacer les données actuelles.";
            if (r.Saved && r.Error != "") rolesStatus += " " + r.Error;
            tray.ShowBalloonTip(5000, "DraftMid", rolesStatus, r.Saved ? ToolTipIcon.Info : ToolTipIcon.Warning);
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
        static bool loaderFolderSet;

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
                if (!loaderFolderSet) { CoreWebView2Environment.SetLoaderDllFolderPath(webDir); loaderFolderSet = true; }
                env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(dataDir, "webview"), new CoreWebView2EnvironmentOptions());
            }
            catch (Exception ex)
            {
                Program.Log(dataDir, ex.ToString());
                string text = ex is WebView2RuntimeNotFoundException
                    ? "DraftMid a besoin du composant Microsoft Edge WebView2, absent de ce PC. Installe-le depuis le site de Microsoft, puis relance DraftMid."
                    : "DraftMid n'a pas pu démarrer. Détail dans " + Path.Combine(dataDir, "draftmid.log");
                if (!opts.Silent) MessageBox.Show(text, "DraftMid", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            w.WebMessageReceived += (s, e) => OnMessage(f, e.WebMessageAsJson);
            // Les liens externes s'ouvrent dans le navigateur par défaut, jamais dans un panneau.
            w.NewWindowRequested += (s, e) => { e.Handled = true; OpenExternal(e.Uri); };
            w.NavigationStarting += (s, e) => { if (!e.Uri.StartsWith("https://draftmid.local/")) { e.Cancel = true; OpenExternal(e.Uri); } };
            TaskCompletionSource<bool> nav = new TaskCompletionSource<bool>();
            w.NavigationCompleted += (s, e) => nav.TrySetResult(true);
            w.Navigate("https://draftmid.local/index.html?" + query);
            await nav.Task;
            done.TrySetResult(true);
            return f;
        }

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
            if (!force && TranslatePanels(client)) return;
            Placement p = Layout.Compute(client, ClientWindow.Monitors(), settings.Compact);
            placementNote = p.Note;
            if (p.Mode == "manual") { PlaceManual(); RefreshUi(); return; }
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

        Native.RECT placedClient;
        readonly Dictionary<string, Native.RECT> placedRects = new Dictionary<string, Native.RECT>();
        System.Windows.Forms.Timer settleTimer;

        // Pendant un déplacement, les panneaux glissent sans changer de taille : un redimensionnement forcerait
        // WebView2 à tout redessiner à chaque pas, d'où un retard visible. La taille est recalculée à l'arrêt.
        bool TranslatePanels(Native.RECT client)
        {
            if (shownMode == "" || !PanelsReady(shownMode) || client.W != placedClient.W || client.H != placedClient.H) return false;
            int dx = client.Left - placedClient.Left, dy = client.Top - placedClient.Top;
            if (dx == 0 && dy == 0) return false;
            string[] ids = shownMode == "two" ? new[] { "left", "right" } : new[] { "compact" };
            if (ids.Any(id => !placedRects.ContainsKey(id))) return false;
            PanelForm[] forms = ids.Select(id => panels[id]).ToArray();
            Native.RECT[] rects = ids.Select(id => { Native.RECT r = placedRects[id]; return new Native.RECT(r.Left + dx, r.Top + dy, r.Right + dx, r.Bottom + dy); }).ToArray();
            MoveTogether(forms, rects, true);
            for (int i = 0; i < ids.Length; i++) placedRects[ids[i]] = rects[i];
            placedClient = client;
            MoveCount++;
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
            if (!frameValid) { tray.ShowBalloonTip(3000, "DraftMid", "Le client doit être visible à l'écran pour retenir cette vue.", ToolTipIcon.Info); return; }
            if (!detector.Teach(frame, isDraft)) { tray.ShowBalloonTip(3000, "DraftMid", "Aucun portrait au centre : cet écran n'a pas été retenu comme une draft.", ToolTipIcon.Info); return; }
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
            miRoles.Text = rolesCts != null ? "Arrêter la mise à jour des rôles" : "Mettre à jour les rôles...";
            miShow.Text = (panelsShown ? "Masquer" : "Afficher") + " les panneaux" + (hotkey != null && hotkey.Registered ? " (Ctrl+Alt+D)" : "");
            miCompact.Checked = settings.Compact;
            miFollow.Checked = settings.Follow;
            miAutoDraft.Checked = settings.AutoDraft;
            miAutostart.Checked = Autostart.IsOn();
            string tip = "DraftMid : " + phase;
            tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
            BroadcastHost();
        }

        string HostJson()
        {
            return new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
                { "kind", "host" }, { "autostart", Autostart.IsOn() }, { "follow", settings.Follow }, { "autoDraft", settings.AutoDraft },
                { "topmost", settings.Topmost }, { "compact", settings.Compact }, { "placement", placementNote }, { "phase", PhaseText.Of(machine.Current) },
                { "hotkey", hotkey != null && hotkey.Registered }, { "diagnostics", settings.Diagnostics }, { "lcuEnabled", settings.LcuEnabled }, { "lcuConfirmed", LcuGate.ReadingEnabled }, { "rolesRunning", rolesCts != null }, { "rolesStatus", rolesStatus } });
        }

        void BroadcastHost() { SendAll(HostJson()); }

        void SendAll(string json)
        {
            foreach (PanelForm f in panels.Values) if (f.View.CoreWebView2 != null) f.View.CoreWebView2.PostWebMessageAsJson(json);
        }

        // Ouvre les réglages comme le bouton du panneau, puis vérifie qu'ils se ferment des deux façons.
        public async Task<string> TestFullWindow()
        {
            Func<Task<PanelForm>> open = async () =>
            {
                OpenFull("settings");
                PanelForm w = null;
                for (int i = 0; i < 200 && !(panels.TryGetValue("full", out w) && w.Visible && w.Ready.IsCompleted); i++) await Task.Delay(50);
                for (int i = 0; i < 100 && await w.View.CoreWebView2.ExecuteScriptAsync("!!document.querySelector('.settings .ghost-btn')") != "true"; i++) await Task.Delay(50);
                return w;
            };
            PanelForm f = await open();
            string problem = f.FormBorderStyle == FormBorderStyle.Sizable && f.ControlBox ? "" : "pas de barre de titre ; ";
            await f.View.CoreWebView2.ExecuteScriptAsync("document.querySelector('.settings .ghost-btn').click()");
            await Task.Delay(300);
            if (f.Visible) problem += "le bouton Fermer ne ferme pas ; ";
            f = await open();
            if (!f.Visible) problem += "réouverture impossible ; ";
            f.Close();
            await Task.Delay(200);
            if (f.Visible || f.IsDisposed || Quitting) problem += "la croix ne masque pas proprement ; ";
            return problem;
        }

        async void OpenFull(string tab)
        {
            bool exists = panels.ContainsKey("full");
            string q = "panel=full" + (tab == "" ? "" : "&tab=" + tab);
            await EnsureEnv();
            if (env == null) return;
            PanelForm f = await Ensure("full", q);
            if (exists) f.View.CoreWebView2.Navigate("https://draftmid.local/index.html?" + q);
            Monitor m = ClientWindow.Monitors().OrderByDescending(x => x.Work.W).First();
            if (!f.Visible) f.Show();
            f.PlaceNoActivate(new Native.RECT(m.Work.Left + 40, m.Work.Top + 20, m.Work.Left + 40 + Math.Min((int)(1280 * m.Scale), m.Work.W - 80), m.Work.Top + 20 + Math.Min((int)(820 * m.Scale), m.Work.H - 40)), false);
            if (opts.Script == null) f.Activate();
        }

        void ImportStats()
        {
            using (OpenFileDialog d = new OpenFileDialog { Title = "Choisir draft-stats.js", Filter = "Statistiques DraftMid|draft-stats.js|Tous les fichiers|*.*" })
            {
                if (d.ShowDialog() != DialogResult.OK) return;
                string head = File.ReadAllText(d.FileName);
                if (!head.StartsWith("window.DRAFT_STATS")) { MessageBox.Show("Ce fichier n'est pas un import DraftMid : rien n'a été modifié.", "DraftMid"); return; }
                File.Copy(d.FileName, Path.Combine(dataDir, "draft-stats.js"), true);
                File.Copy(d.FileName, Path.Combine(webDir, "draft-stats.js"), true);
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
                case "updateRoles": ToggleRolesUpdate(); break;
                case "closeFull": { PanelForm full; if (panels.TryGetValue("full", out full)) full.Hide(); break; }
                case "toggleCompact": SetCompact(!settings.Compact); break;
                case "openFull": OpenFull(m.ContainsKey("tab") ? m["tab"] as string ?? "" : ""); break;
                case "setAutostart": Autostart.Set(val, exePath); RefreshUi(); break;
                case "setFollow": SetFollow(val); break;
                case "setAutoDraft": SetAutoDraft(val); break;
                case "setDiagnostics": settings.Diagnostics = val; Persist(); RefreshUi(); break;
                case "setLcu": settings.LcuEnabled = val; if (!val) lcu = null; Persist(); RefreshUi(); break;
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

        public void Quit()
        {
            Quitting = true;
            if (rolesCts != null) rolesCts.Cancel();
            slowPoll.Stop(); sampler.Stop(); moveTimer.Stop();
            SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
            Persist();
            tracker.Dispose();
            grabber.Dispose();
            hotkey.Dispose();
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

        // La fenêtre active peut changer si l'utilisateur clique ailleurs : seul compte que DraftMid ne la prenne pas.
        // La fausse fenêtre LoL peut avoir le premier plan : seuls les panneaux ne doivent jamais le prendre.
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
            t(midGuess.Contains("Ahri") && (await js("state.manualMid === null")) == "true", "Scénario 7. Mid supposé sans le présenter comme certain (" + midGuess + ")");
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

            app.StepPhase(false, false, Evidence.Unknown, 1);
            t(!app.PanelsShown && app.CurrentPhase == Phase.ClientClosed && !app.Lcu.Connected, "Scénario 1. Client fermé : veille, identifiants oubliés");
            t(sim.Requests.All(LcuEndpoints.IsAllowed), "Scénario : seuls les deux endpoints documentés sont appelés");
            t(await app.TestReleaseAndReopen(), "Client fermé puis rouvert : l'application reste ouverte et recrée ses panneaux");
            string fullProblem = await app.TestFullWindow();
            t(fullProblem == "", "Réglages : barre de titre, bouton Fermer et croix fonctionnent " + fullProblem);
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
                t(app.HotkeyRegistered, "Raccourci Ctrl+Alt+D enregistré");

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
            s.AppendLine(fails == 0 ? "TOUT EST BON" : fails + " ECHEC(S)");
            if (o.Out != null) File.WriteAllText(o.Out, s.ToString());
            app.Quit();
        }
    }

public class Options
    {
        public string Script, CaptureDir, Out, DataDir, InstallDir;
        public bool Silent, SelfTest, Install, Uninstall, TestInstall, EndToEnd, SimulatedPhase;
        public int AutoExit, SettleMs = 2500;
        public string RunKey;

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
                    case "--e2e": o.EndToEnd = true; o.SimulatedPhase = true; o.Silent = true; break;
                    case "--install": o.Install = true; break;
                    case "--uninstall": o.Uninstall = true; break;
                    case "--install-dir": o.InstallDir = next(); break;
                    case "--test-install": o.TestInstall = true; o.Silent = true; break;
                    case "--run-key": o.RunKey = next(); break;
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
        static string LinkPath(Options o, string dir) { return o.TestInstall ? Path.Combine(dir, "DraftMid.lnk") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "DraftMid.lnk"); }

        public static void Install(Options o, string self)
        {
            string dir = o.InstallDir ?? DefaultDir;
            Directory.CreateDirectory(dir);
            string target = Path.Combine(dir, "DraftMid.exe");
            File.Copy(self, target, true);
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            dynamic link = shell.CreateShortcut(LinkPath(o, dir));
            link.TargetPath = target;
            link.WorkingDirectory = dir;
            link.Description = "DraftMid";
            link.Save();
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(o.TestInstall ? TestUninstallKey : UninstallKey))
            {
                k.SetValue("DisplayName", "DraftMid");
                k.SetValue("DisplayVersion", "2.8.0");
                k.SetValue("Publisher", "DraftMid");
                k.SetValue("DisplayIcon", target);
                k.SetValue("InstallLocation", dir);
                k.SetValue("UninstallString", "\"" + target + "\" --uninstall");
                k.SetValue("NoModify", 1); k.SetValue("NoRepair", 1);
            }
            if (o.TestInstall) return;
            MessageBox.Show("DraftMid est installé. Tu le retrouves dans le menu Démarrer.", "DraftMid");
            Process.Start(new ProcessStartInfo(target) { WorkingDirectory = dir, UseShellExecute = true });
        }

        public static void Uninstall(Options o, string self)
        {
            string dir = o.InstallDir ?? DefaultDir;
            if (!o.TestInstall && MessageBox.Show("Désinstaller DraftMid ? Ton journal et tes préférences sont conservés.", "DraftMid", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try { File.Delete(LinkPath(o, dir)); } catch { }
            if (o.TestInstall) Registry.CurrentUser.DeleteSubKeyTree(@"Software\DraftMid-SelfTest", false);
            else { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); Autostart.Set(false, self); }
            string target = Path.Combine(dir, "DraftMid.exe");
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
            string stats = Path.Combine(dataDir, "draft-stats.js");
            if (File.Exists(stats)) File.Copy(stats, Path.Combine(dir, "draft-stats.js"), true);
            string liveRoles = Path.Combine(dataDir, "draft-role-rates-live.js");
            if (File.Exists(liveRoles)) File.Copy(liveRoles, Path.Combine(dir, "draft-role-rates-live.js"), true);
            return dir;
        }

        [STAThread]
        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbedded;
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
            Directory.CreateDirectory(dataDir);
            if (o.RunKey != null) Autostart.Key = o.RunKey;

            if (o.SelfTest) return SelfTest.Run(o, dataDir, self);
            if (o.Uninstall) { Installer.Uninstall(o, self); return 0; }
            if (o.Install || (!o.TestInstall && Path.GetFileName(self).IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0)) { Installer.Install(o, self); return 0; }
            if (o.TestInstall) { Installer.Install(o, self); return 0; }

            bool created;
            using (Mutex single = new Mutex(true, o.DataDir == null ? "DraftMid.Instance" : "DraftMid.Test." + o.DataDir.GetHashCode(), out created))
            {
                if (!created)
                {
                    if (!o.Silent) MessageBox.Show("DraftMid est déjà ouvert : utilise son icône près de l'horloge.", "DraftMid");
                    return 0;
                }
                string webDir = Extract(dataDir);
                CompanionApp app = new CompanionApp(o, dataDir, webDir, self, o.EndToEnd ? EndToEnd.Matcher : ClientWindowTracker.IsRealClient);
                if (o.EndToEnd) EndToEnd.Run(app, o);
                Application.Run(app);
            }
            return 0;
        }
    }
}
