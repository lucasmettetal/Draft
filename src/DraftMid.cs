// DraftMid : application compagnon Windows. Fenêtres natives autour de l'interface web existante.
// Ne lit que la position de la fenêtre du client via l'API Windows : aucune injection, aucune lecture
// de mémoire, de fichier ou d'API du client League of Legends.
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
[assembly: AssemblyVersion("2.2.0.0")]
[assembly: AssemblyFileVersion("2.2.0.0")]

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
        public const uint SWP_NOACTIVATE = 0x10, SWP_NOZORDER = 0x4, SWP_SHOWWINDOW = 0x40;
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
        const int MinW = 230, IdealW = 340, MinH = 760, Gap = 6;

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

    public class Settings
    {
        public bool Compact;
        public bool Follow = true;
        public bool Topmost;
        public Dictionary<string, SavedRect> Rects = new Dictionary<string, SavedRect>();

        public static Settings Load(string path)
        {
            try { if (File.Exists(path)) return new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings(); }
            catch { }
            return new Settings();
        }

        public void Save(string path)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, new JavaScriptSerializer().Serialize(this));
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
        // Seul le titre, le processus et le rectangle de la fenêtre sont lus.
        public static bool Find(out Native.RECT rect)
        {
            Native.RECT best = new Native.RECT();
            long area = 0;
            Native.EnumWindows((h, l) =>
            {
                if (!Native.IsWindowVisible(h) || Native.IsIconic(h)) return true;
                StringBuilder t = new StringBuilder(64);
                Native.GetWindowText(h, t, 64);
                if (t.ToString() != "League of Legends") return true;
                uint pid;
                Native.GetWindowThreadProcessId(h, out pid);
                try { using (Process p = Process.GetProcessById((int)pid)) if (!p.ProcessName.Equals("LeagueClientUx", StringComparison.OrdinalIgnoreCase)) return true; }
                catch { return true; }
                Native.RECT r;
                if (Native.GetWindowRect(h, out r) && (long)r.W * r.H > area) { best = r; area = (long)r.W * r.H; }
                return true;
            }, IntPtr.Zero);
            rect = best;
            return area > 0;
        }

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
        readonly CompanionApp app;
        const int Border = 6;

        public PanelForm(string id, CompanionApp app)
        {
            Id = id;
            this.app = app;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = id == "full";
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(10, 15, 22);
            Padding = new Padding(Border);
            Text = id == "full" ? "DraftMid" : "DraftMid : " + id;
            if (app.AppIcon != null) Icon = app.AppIcon;
            View = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.FromArgb(10, 15, 22) };
            Controls.Add(View);
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84, WM_EXITSIZEMOVE = 0x232;
            if (m.Msg == WM_NCHITTEST)
            {
                Point p = PointToClient(new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                bool l = p.X < Border, r = p.X >= ClientSize.Width - Border, t = p.Y < Border, b = p.Y >= ClientSize.Height - Border;
                int hit = t && l ? 13 : t && r ? 14 : b && l ? 16 : b && r ? 17 : l ? 10 : r ? 11 : t ? 12 : b ? 15 : 0;
                if (hit != 0) { m.Result = new IntPtr(hit); return; }
            }
            base.WndProc(ref m);
            if (m.Msg == WM_EXITSIZEMOVE) app.PanelMovedByUser(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!app.Quitting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
            base.OnFormClosing(e);
        }

        public void PlaceNoActivate(Native.RECT r, bool topmost)
        {
            IntPtr after = topmost ? new IntPtr(-1) : new IntPtr(-2);
            Native.SetWindowPos(Handle, after, r.Left - 0, r.Top, r.W, r.H, Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
        }
    }

    public class CompanionApp : ApplicationContext
    {
        readonly string dataDir, webDir, settingsPath, exePath;
        readonly Options opts;
        readonly Settings settings;
        readonly NotifyIcon tray;
        readonly System.Windows.Forms.Timer poll;
        readonly Dictionary<string, PanelForm> panels = new Dictionary<string, PanelForm>();
        CoreWebView2Environment env;
        Native.RECT lastClient;
        bool hadClient, panelsVisible = true;
        string placementNote = "Client LoL non détecté : mode manuel.";
        public bool Quitting;
        public Icon AppIcon;
        ToolStripMenuItem miCompact, miFollow, miAutostart, miShow;

        public CompanionApp(Options o, string dataDir, string webDir, string exePath)
        {
            opts = o; this.dataDir = dataDir; this.webDir = webDir; this.exePath = exePath;
            settingsPath = Path.Combine(dataDir, "settings.json");
            settings = Settings.Load(settingsPath);
            try { AppIcon = Icon.ExtractAssociatedIcon(exePath); } catch { }

            ContextMenuStrip menu = new ContextMenuStrip();
            miShow = new ToolStripMenuItem("Masquer les panneaux", null, (s, e) => ToggleVisible());
            miCompact = new ToolStripMenuItem("Panneau unique", null, (s, e) => SetCompact(!settings.Compact));
            miFollow = new ToolStripMenuItem("Placer autour du client LoL", null, (s, e) => SetFollow(!settings.Follow));
            miAutostart = new ToolStripMenuItem("Démarrer avec Windows", null, (s, e) => { Autostart.Set(!Autostart.IsOn(), exePath); RefreshUi(); });
            menu.Items.Add(miShow);
            menu.Items.Add(miCompact);
            menu.Items.Add(miFollow);
            menu.Items.Add("Replacer les panneaux", null, (s, e) => { SetFollow(true); Reposition(true); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Analyse détaillée", null, (s, e) => OpenFull(""));
            menu.Items.Add("Réglages", null, (s, e) => OpenFull("settings"));
            menu.Items.Add("Lancer une draft de démonstration", null, (s, e) => RunDemo(900));
            menu.Items.Add("Importer mes statistiques...", null, (s, e) => ImportStats());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miAutostart);
            menu.Items.Add("Quitter", null, (s, e) => Quit());
            tray = new NotifyIcon { Icon = AppIcon ?? SystemIcons.Application, Text = "DraftMid", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += (s, e) => ToggleVisible();
            RefreshUi();

            poll = new System.Windows.Forms.Timer { Interval = 1000 };
            poll.Tick += (s, e) => Reposition(false);
            Start();
        }

        async void Start()
        {
            try
            {
                CoreWebView2Environment.SetLoaderDllFolderPath(webDir);
                string ud = Path.Combine(dataDir, "webview");
                env = await CoreWebView2Environment.CreateAsync(null, ud, new CoreWebView2EnvironmentOptions());
                await ShowLayout();
                poll.Start();
                Reposition(true);
                if (opts.Script != null) RunTestScript();
                else if (opts.AutoExit > 0) { await Task.Delay(opts.AutoExit * 1000); Quit(); }
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

        async Task<PanelForm> Ensure(string id, string query)
        {
            PanelForm f;
            if (panels.TryGetValue(id, out f)) return f;
            f = new PanelForm(id, this);
            panels[id] = f;
            f.Show();
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
            w.Navigate("https://draftmid.local/index.html?" + query);
            return f;
        }

        void OpenExternal(string uri)
        {
            if (!uri.StartsWith("https://")) return;
            try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); } catch { }
        }

        async Task ShowLayout()
        {
            if (settings.Compact)
            {
                PanelForm c = await Ensure("compact", "panel=compact");
                Hide("left"); Hide("right");
                if (panelsVisible) c.Show();
            }
            else
            {
                PanelForm l = await Ensure("left", "panel=left");
                PanelForm r = await Ensure("right", "panel=right");
                Hide("compact");
                if (panelsVisible) { l.Show(); r.Show(); }
            }
            ApplySavedOrDefault();
        }

        void Hide(string id) { PanelForm f; if (panels.TryGetValue(id, out f)) f.Hide(); }

        void ApplySavedOrDefault()
        {
            List<Monitor> mons = ClientWindow.Monitors();
            Monitor main = mons.FirstOrDefault(m => m.Primary) ?? mons[0];
            int w = (int)(340 * main.Scale), h = Math.Min(main.Work.H, (int)(860 * main.Scale));
            foreach (PanelForm f in panels.Values.Where(p => p.Id != "full"))
            {
                Native.RECT r = f.Id == "left" ? new Native.RECT(main.Work.Left, main.Work.Top, main.Work.Left + w, main.Work.Top + h)
                    : new Native.RECT(main.Work.Right - w, main.Work.Top, main.Work.Right, main.Work.Top + h);
                SavedRect sv;
                if (settings.Rects.TryGetValue(f.Id, out sv))
                {
                    Native.RECT saved = new Native.RECT(sv.X, sv.Y, sv.X + sv.W, sv.Y + sv.H);
                    // Une position sur un écran débranché est ignorée.
                    if (mons.Any(m => m.Work.Overlaps(saved))) r = saved;
                }
                if (f.Visible) f.PlaceNoActivate(r, settings.Topmost);
            }
        }

        void Reposition(bool force)
        {
            Native.RECT client;
            bool found = ClientWindow.Find(out client);
            if (!found)
            {
                if (hadClient) { hadClient = false; placementNote = "Client LoL non détecté : mode manuel."; BroadcastHost(); }
                return;
            }
            bool changed = !hadClient || client.Left != lastClient.Left || client.Top != lastClient.Top || client.W != lastClient.W || client.H != lastClient.H;
            hadClient = true;
            lastClient = client;
            if (!settings.Follow || !panelsVisible || (!changed && !force)) return;
            Placement p = Layout.Compute(client, ClientWindow.Monitors(), settings.Compact);
            placementNote = p.Note;
            ApplyPlacement(p);
            BroadcastHost();
        }

        async void ApplyPlacement(Placement p)
        {
            if (p.Mode == "two")
            {
                PanelForm l = await Ensure("left", "panel=left");
                PanelForm r = await Ensure("right", "panel=right");
                Hide("compact");
                l.PlaceNoActivate(p.Left, settings.Topmost);
                r.PlaceNoActivate(p.Right, settings.Topmost);
            }
            else if (p.Mode == "compact")
            {
                PanelForm c = await Ensure("compact", "panel=compact");
                Hide("left"); Hide("right");
                c.PlaceNoActivate(p.Single, settings.Topmost);
            }
        }

        public void PanelMovedByUser(PanelForm f)
        {
            if (f.Id == "full") return;
            Rectangle b = f.Bounds;
            settings.Rects[f.Id] = new SavedRect { X = b.X, Y = b.Y, W = b.Width, H = b.Height };
            settings.Follow = false;
            placementNote = "Position manuelle conservée.";
            Persist();
            RefreshUi();
        }

        void Persist() { try { settings.Save(settingsPath); } catch (Exception e) { Program.Log(dataDir, e.ToString()); } }

        void ToggleVisible()
        {
            panelsVisible = !panelsVisible;
            foreach (PanelForm f in panels.Values.Where(p => p.Id != "full")) f.Hide();
            if (panelsVisible) { Task t = ShowLayout(); Reposition(true); }
            RefreshUi();
        }

        async void SetCompact(bool v)
        {
            settings.Compact = v;
            Persist();
            await ShowLayout();
            Reposition(true);
            RefreshUi();
        }

        void SetFollow(bool v)
        {
            settings.Follow = v;
            if (v) settings.Rects.Clear();
            Persist();
            if (v) Reposition(true);
            RefreshUi();
        }

        void RefreshUi()
        {
            miShow.Text = panelsVisible ? "Masquer les panneaux" : "Afficher les panneaux";
            miCompact.Checked = settings.Compact;
            miFollow.Checked = settings.Follow;
            miAutostart.Checked = Autostart.IsOn();
            BroadcastHost();
        }

        string HostJson()
        {
            return new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
                { "kind", "host" }, { "autostart", Autostart.IsOn() }, { "follow", settings.Follow },
                { "topmost", settings.Topmost }, { "compact", settings.Compact }, { "placement", placementNote } });
        }

        void BroadcastHost()
        {
            string json = HostJson();
            foreach (PanelForm f in panels.Values) if (f.View.CoreWebView2 != null) f.View.CoreWebView2.PostWebMessageAsJson(json);
        }

        async void OpenFull(string tab)
        {
            PanelForm f;
            bool exists = panels.TryGetValue("full", out f);
            string q = "panel=full" + (tab == "" ? "" : "&tab=" + tab);
            f = await Ensure("full", q);
            if (exists) f.View.CoreWebView2.Navigate("https://draftmid.local/index.html?" + q);
            f.ClientSize = new Size(1280, 820);
            f.StartPosition = FormStartPosition.Manual;
            Monitor m = ClientWindow.Monitors().OrderByDescending(x => x.Work.W).First();
            f.PlaceNoActivate(new Native.RECT(m.Work.Left + 40, m.Work.Top + 20, m.Work.Left + 40 + Math.Min(1280, m.Work.W - 80), m.Work.Top + 20 + Math.Min(820, m.Work.H - 40)), false);
            f.Show();
            if (opts.Script == null) f.Activate();
        }

        void RunDemo(int stepMs)
        {
            foreach (PanelForm f in panels.Values)
                if (f.Id != "full" && f.Visible && f.View.CoreWebView2 != null) { f.View.CoreWebView2.PostWebMessageAsJson("{\"kind\":\"demo\",\"stepMs\":" + stepMs + "}"); break; }
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
                case "hideAll": ToggleVisible(); break;
                case "toggleCompact": SetCompact(!settings.Compact); break;
                case "openFull": OpenFull(m.ContainsKey("tab") ? m["tab"] as string ?? "" : ""); break;
                case "setAutostart": Autostart.Set(val, exePath); RefreshUi(); break;
                case "setFollow": SetFollow(val); break;
                case "setTopmost": settings.Topmost = val; Persist(); Reposition(true); RefreshUi(); break;
            }
        }

        async void RunTestScript()
        {
            // Mode de test uniquement : exécute un script dans un panneau, écrit le résultat, capture chaque panneau.
            StringBuilder log = new StringBuilder();
            try
            {
                PanelForm target = panels.ContainsKey("right") && panels["right"].Visible ? panels["right"] : panels["compact"];
                // Attend que l'interface soit réellement prête au lieu d'un délai fixe.
                for (int i = 0; i < 120; i++)
                {
                    if (target.View.CoreWebView2 != null && await target.View.CoreWebView2.ExecuteScriptAsync("typeof V !== \"undefined\" && !!document.querySelector(\".panel\")") == "true") break;
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
                Native.RECT cl;
                log.AppendLine("CLIENT " + (ClientWindow.Find(out cl) ? cl.ToString() : "absent") + " | " + placementNote);
            }
            catch (Exception e) { log.AppendLine("EXC " + e); }
            if (opts.Out != null) File.WriteAllText(opts.Out, log.ToString());
            Quit();
        }

        public void Quit()
        {
            Quitting = true;
            if (poll != null) poll.Stop();
            tray.Visible = false;
            tray.Dispose();
            foreach (PanelForm f in panels.Values.ToList()) { try { f.Close(); f.Dispose(); } catch { } }
            ExitThread();
        }
    }

    public class Options
    {
        public string Script, CaptureDir, Out, DataDir, InstallDir;
        public bool Silent, SelfTest, Install, Uninstall, TestInstall;
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
                k.SetValue("DisplayVersion", "2.2.0");
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
            check("1 écran 4K à 200 %, client 2560x1440", new Native.RECT(640, 300, 3200, 1740), new List<Monitor> { mon(0, 0, 3840, 2100, 2) }, false, "two");
            check("1 écran 1920 à 125 %, client 1280x720", new Native.RECT(320, 160, 1600, 880), new List<Monitor> { mon(0, 0, 1920, 1030, 1.25) }, false, "two");
            check("Client petit : hauteur minimale respectée", new Native.RECT(320, 300, 1600, 500), fhd, false, "two");
            List<Monitor> dual = new List<Monitor> { mon(0, 0, 1920, 1040, 1.25), mon(-1920, 87, 1920, 1040, 1) };
            check("2 écrans, client plein écran sur le principal", new Native.RECT(0, 0, 1920, 1040), dual, false, "two");
            check("2 écrans, client 1280 sur l'écran secondaire", new Native.RECT(-1600, 300, -320, 1020), dual, false, "two");
            check("2 écrans, client à cheval : l'écran le plus couvert compte", new Native.RECT(-300, 100, 1300, 1000), dual, false, "compact");
            check("2 écrans, plein écran en panneau unique", new Native.RECT(0, 0, 1920, 1040), dual, true, "compact");

            string sp = Path.Combine(dataDir, "selftest-settings.json");
            Settings a = new Settings { Compact = true, Follow = false, Topmost = true };
            a.Rects["left"] = new SavedRect { X = -100, Y = 20, W = 300, H = 700 };
            a.Save(sp);
            Settings b = Settings.Load(sp);
            t(b.Compact && !b.Follow && b.Topmost && b.Rects["left"].X == -100, "préférences relues à l'identique");
            File.WriteAllText(sp, "pas du json");
            Settings c = Settings.Load(sp);
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

            Stopwatch clock = Stopwatch.StartNew();
            Native.RECT cl;
            bool found = false;
            for (int i = 0; i < 100; i++) found = ClientWindow.Find(out cl);
            s.AppendLine("MESURE : détection de la fenêtre du client " + (clock.ElapsedMilliseconds / 100.0) + " ms en moyenne, client " + (found ? "détecté" : "absent"));
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
                Application.Run(new CompanionApp(o, dataDir, webDir, self));
            }
            return 0;
        }
    }
}
