// Suivi de la fenêtre du client et estimation de la phase de draft : deux responsabilités indépendantes.
// Aucune injection, lecture mémoire, interception réseau ni API du client.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DraftMid
{
    public enum Phase { ClientClosed, OutOfDraft, Draft, Uncertain, InGame }
    public enum Evidence { Unknown, NotDraft, Draft }

    public static class PhaseText
    {
        public static string Of(Phase p)
        {
            switch (p)
            {
                case Phase.ClientClosed: return "Client LoL fermé";
                case Phase.OutOfDraft: return "Client ouvert, hors draft (estimation)";
                case Phase.Draft: return "Draft en cours (estimation)";
                case Phase.InGame: return "Partie en cours";
                default: return "Phase incertaine";
            }
        }
    }

    // ---------- Fenêtre du client ----------
    public class ClientWindowTracker : IDisposable
    {
        const uint EVENT_MIN = 0x0016, EVENT_MAX = 0x0017, EVENT_MOVESIZEEND = 0x000B, EVENT_LOCATION = 0x800B, EVENT_DESTROY = 0x8001, EVENT_SHOW = 0x8002, EVENT_HIDE = 0x8003;
        const uint OUT_OF_CONTEXT = 0, SKIP_OWN = 0x2;

        readonly Func<IntPtr, bool> matcher;
        readonly List<IntPtr> hooks = new List<IntPtr>();
        readonly NativeHook.WinEventProc proc;
        readonly bool skipOwnProcess;
        uint hookedPid;
        public IntPtr Handle { get; private set; }
        public Native.RECT Rect { get; private set; }
        public bool Minimized { get; private set; }
        public long EventCount;
        public event Action Changed;

        public ClientWindowTracker(Func<IntPtr, bool> matcher, bool skipOwnProcess)
        {
            this.matcher = matcher;
            this.skipOwnProcess = skipOwnProcess;
            proc = OnEvent;
        }

        // Vrai client : processus, classe et fenêtre visible, pas seulement le titre.
        public static bool IsRealClient(IntPtr h)
        {
            StringBuilder c = new StringBuilder(32);
            NativeHook.GetClassName(h, c, 32);
            if (c.ToString() != "RCLIENT") return false;
            uint pid;
            Native.GetWindowThreadProcessId(h, out pid);
            try { using (Process p = Process.GetProcessById((int)pid)) return p.ProcessName.Equals("LeagueClientUx", StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        // Appelé lentement : la fenêtre n'est recherchée que pour détecter son apparition.
        public void Refresh()
        {
            if (Handle != IntPtr.Zero && NativeHook.IsWindow(Handle) && Native.IsWindowVisible(Handle)) { Update(); return; }
            IntPtr found = IntPtr.Zero;
            long area = -1;
            Native.EnumWindows((h, l) =>
            {
                if (!Native.IsWindowVisible(h) || !matcher(h)) return true;
                Native.RECT r;
                Native.GetWindowRect(h, out r);
                long a = (long)r.W * r.H;
                if (a > area) { area = a; found = h; }
                return true;
            }, IntPtr.Zero);
            if (found != Handle) Attach(found);
            Update();
        }

        void Attach(IntPtr h)
        {
            Unhook();
            Handle = h;
            if (h == IntPtr.Zero) return;
            uint pid;
            Native.GetWindowThreadProcessId(h, out pid);
            hookedPid = pid;
            uint flags = OUT_OF_CONTEXT | (skipOwnProcess ? SKIP_OWN : 0);
            foreach (uint[] range in new[] { new[] { EVENT_LOCATION, EVENT_LOCATION }, new[] { EVENT_MIN, EVENT_MAX }, new[] { EVENT_MOVESIZEEND, EVENT_MOVESIZEEND }, new[] { EVENT_DESTROY, EVENT_HIDE } })
                hooks.Add(NativeHook.SetWinEventHook(range[0], range[1], IntPtr.Zero, proc, pid, 0, flags));
        }

        void OnEvent(IntPtr hook, uint ev, IntPtr hwnd, int obj, int child, uint thread, uint time)
        {
            if (hwnd != Handle || obj != 0) return;
            EventCount++;
            if (ev == EVENT_DESTROY || ev == EVENT_HIDE) { Handle = IntPtr.Zero; Minimized = false; Unhook(); }
            else Update();
            Action c = Changed;
            if (c != null) c();
        }

        void Update()
        {
            if (Handle == IntPtr.Zero) { Minimized = false; return; }
            Minimized = Native.IsIconic(Handle);
            if (Minimized) return;
            Native.RECT r;
            // Limites visibles réelles, sans l'ombre invisible des fenêtres.
            if (NativeHook.DwmGetWindowAttribute(Handle, 9, out r, Marshal.SizeOf(typeof(Native.RECT))) != 0) Native.GetWindowRect(Handle, out r);
            Rect = r;
        }

        void Unhook()
        {
            foreach (IntPtr h in hooks) if (h != IntPtr.Zero) NativeHook.UnhookWinEvent(h);
            hooks.Clear();
            hookedPid = 0;
        }

        public void Dispose() { Unhook(); }
    }

    public static class NativeHook
    {
        public delegate void WinEventProc(IntPtr hook, uint ev, IntPtr hwnd, int obj, int child, uint thread, uint time);
        [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr mod, WinEventProc f, uint pid, uint tid, uint flags);
        [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out Native.RECT r, int size);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Pt p);
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint flags);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFO info, uint usage, out IntPtr bits, IntPtr section, uint offset);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] public static extern int SetStretchBltMode(IntPtr dc, int mode);
        [DllImport("gdi32.dll")] public static extern bool StretchBlt(IntPtr d, int x, int y, int w, int h, IntPtr s, int sx, int sy, int sw, int sh, uint rop);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] public static extern IntPtr BeginDeferWindowPos(int n);
        [DllImport("user32.dll")] public static extern IntPtr DeferWindowPos(IntPtr info, IntPtr h, IntPtr after, int x, int y, int w, int ht, uint flags);
        [DllImport("user32.dll")] public static extern bool EndDeferWindowPos(IntPtr info);

        [StructLayout(LayoutKind.Sequential)] public struct Pt { public int X, Y; public Pt(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public int biSize, biWidth, biHeight;
            public short biPlanes, biBitCount;
            public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
            public int colors;
        }
    }

    // ---------- Image réduite du client ----------
    // Rien n'est conservé ni transmis : 96x54 pixels en mémoire, remplacés à chaque lecture.
    public class Frame
    {
        public const int W = 96, H = 54;
        public readonly byte[] Rgb = new byte[W * H * 3];
        public int Lum(int x, int y) { int i = (y * W + x) * 3; return (Rgb[i] * 3 + Rgb[i + 1] * 6 + Rgb[i + 2]) / 10; }
        public int Sat(int x, int y) { int i = (y * W + x) * 3; int a = Math.Max(Rgb[i], Math.Max(Rgb[i + 1], Rgb[i + 2])); int b = Math.Min(Rgb[i], Math.Min(Rgb[i + 1], Rgb[i + 2])); return a - b; }
    }

    public class FrameGrabber : IDisposable
    {
        readonly IntPtr memDc, dib, old, bits;

        public FrameGrabber()
        {
            NativeHook.BITMAPINFO bi = new NativeHook.BITMAPINFO { biSize = 40, biWidth = Frame.W, biHeight = -Frame.H, biPlanes = 1, biBitCount = 32 };
            IntPtr screen = NativeHook.GetDC(IntPtr.Zero);
            memDc = NativeHook.CreateCompatibleDC(screen);
            IntPtr b;
            dib = NativeHook.CreateDIBSection(memDc, ref bi, 0, out b, IntPtr.Zero, 0);
            bits = b;
            old = NativeHook.SelectObject(memDc, dib);
            NativeHook.SetStretchBltMode(memDc, 4);
            NativeHook.ReleaseDC(IntPtr.Zero, screen);
        }

        // Vrai seulement si le client est réellement visible aux points testés : sinon on ne conclut rien.
        public static bool IsUnobstructed(IntPtr client, Native.RECT r)
        {
            int[][] points = {
                new[] { r.Left + r.W / 2, r.Top + r.H / 2 }, new[] { r.Left + r.W / 10, r.Top + r.H / 12 }, new[] { r.Right - r.W / 10, r.Top + r.H / 12 },
                new[] { r.Left + r.W / 10, r.Bottom - r.H / 12 }, new[] { r.Right - r.W / 10, r.Bottom - r.H / 12 } };
            foreach (int[] p in points)
                if (NativeHook.GetAncestor(NativeHook.WindowFromPoint(new NativeHook.Pt(p[0], p[1])), 2) != client) return false;
            return true;
        }

        public bool Grab(Native.RECT r, Frame into)
        {
            IntPtr screen = NativeHook.GetDC(IntPtr.Zero);
            bool ok = NativeHook.StretchBlt(memDc, 0, 0, Frame.W, Frame.H, screen, r.Left, r.Top, r.W, r.H, 0x00CC0020);
            NativeHook.ReleaseDC(IntPtr.Zero, screen);
            if (!ok) return false;
            byte[] raw = new byte[Frame.W * Frame.H * 4];
            Marshal.Copy(bits, raw, 0, raw.Length);
            for (int i = 0, j = 0; i < raw.Length; i += 4, j += 3) { into.Rgb[j] = raw[i + 2]; into.Rgb[j + 1] = raw[i + 1]; into.Rgb[j + 2] = raw[i]; }
            return true;
        }

        public void Dispose() { NativeHook.SelectObject(memDc, old); NativeHook.DeleteObject(dib); NativeHook.DeleteDC(memDc); }
    }

    // ---------- Estimation de la phase ----------
    // HYPOTHÈSE à confirmer sur le vrai client : la barre de navigation disparaît pendant la sélection.
    // L'application apprend cette barre seule, et apprend aussi de tes corrections manuelles.
    public class DraftPhaseDetector
    {
        const int NavRows = 6, NavCols = 43, StableFrames = 8, MinDetail = 6;
        const double NavDiff = 16, SampleMatch = 9, StableDiff = 4, MinSat = 22, MinEdges = 4;
        readonly UserPreferences prefs;
        int[] lastNav;
        int stable;
        public string LastMetrics = "";

        public DraftPhaseDetector(UserPreferences prefs) { this.prefs = prefs; }

        public static int[] NavBand(Frame f)
        {
            int[] v = new int[NavRows * NavCols];
            for (int y = 0; y < NavRows; y++) for (int x = 0; x < NavCols; x++) v[y * NavCols + x] = f.Lum(x, y);
            return v;
        }

        // Signature locale : bandes du haut et du bas, très réduites. Impossible d'y retrouver une image lisible.
        public static int[] Signature(Frame f)
        {
            List<int> v = new List<int>();
            foreach (int[] rows in new[] { new[] { 0, 7 }, new[] { 46, 54 } })
                for (int x = 0; x + 4 <= Frame.W; x += 4)
                {
                    int s = 0;
                    for (int y = rows[0]; y < rows[1]; y++) for (int k = 0; k < 4; k++) s += f.Lum(x + k, y);
                    v.Add(s / ((rows[1] - rows[0]) * 4));
                }
            return v.ToArray();
        }

        public static double Dist(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return 999;
            double s = 0;
            for (int i = 0; i < a.Length; i++) s += Math.Abs(a[i] - b[i]);
            return s / a.Length;
        }

        static double Detail(int[] v) { double m = v.Average(); return v.Average(x => Math.Abs(x - m)); }

        // Le centre d'une sélection est chargé en portraits colorés : un écran noir de chargement ne compte pas.
        public static bool BusyCenter(Frame f)
        {
            double sat = 0, edges = 0;
            int n = 0;
            for (int y = 10; y < 44; y++) for (int x = 20; x < 76; x++) { sat += f.Sat(x, y); edges += Math.Abs(f.Lum(x, y) - f.Lum(x + 1, y)); n++; }
            return sat / n >= MinSat && edges / n >= MinEdges;
        }

        public Evidence Classify(Frame f)
        {
            int[] sig = Signature(f);
            double pos = prefs.DraftSamples.Count == 0 ? 999 : prefs.DraftSamples.Min(s => Dist(s, sig));
            double neg = prefs.OtherSamples.Count == 0 ? 999 : prefs.OtherSamples.Min(s => Dist(s, sig));
            int[] nav = NavBand(f);
            double navDiff = Dist(nav, prefs.NavBaseline);
            bool busy = BusyCenter(f);
            LastMetrics = string.Format("pos={0:0.0} neg={1:0.0} nav={2:0.0} centre={3} detail={4:0.0} stable={5}", pos, neg, navDiff, busy, Detail(nav), stable);

            // Les corrections de l'utilisateur passent avant toute règle.
            if (neg < SampleMatch && neg <= pos) return Evidence.NotDraft;
            if (pos < SampleMatch) return Evidence.Draft;

            LearnNav(nav);
            if (prefs.NavBaseline == null) return Evidence.Unknown;
            if (Dist(nav, prefs.NavBaseline) < NavDiff) return Evidence.NotDraft;
            return busy ? Evidence.Draft : Evidence.Unknown;
        }

        void LearnNav(int[] nav)
        {
            stable = Dist(nav, lastNav) < StableDiff ? stable + 1 : 0;
            lastNav = nav;
            if (prefs.NavBaseline == null && stable >= StableFrames && Detail(nav) >= MinDetail) prefs.NavBaseline = nav;
        }

        // Une draft ne dure pas indéfiniment : la référence apprise était sûrement fausse.
        public void ForgetBaseline() { prefs.NavBaseline = null; stable = 0; }

        public void Teach(Frame f, bool isDraft)
        {
            List<int[]> list = isDraft ? prefs.DraftSamples : prefs.OtherSamples;
            list.Add(Signature(f));
            if (list.Count > UserPreferences.MaxSamples) list.RemoveAt(0);
        }
    }

    // Anti-clignotement : il faut plusieurs lectures concordantes, sauf pour les signaux certains.
    public class PhaseMachine
    {
        public const int EnterCount = 2, ExitCount = 3;
        public const double MaxDraftSeconds = 480;
        public Phase Current = Phase.ClientClosed;
        public bool DraftTimedOut;
        int yes, no;
        double inDraft;

        public Phase Step(bool clientOpen, bool inGame, Evidence e, double seconds)
        {
            DraftTimedOut = false;
            if (inGame) { Reset(); return Current = Phase.InGame; }
            if (!clientOpen) { Reset(); return Current = Phase.ClientClosed; }
            if (Current == Phase.ClientClosed || Current == Phase.InGame) Current = Phase.Uncertain;
            yes = e == Evidence.Draft ? yes + 1 : 0;
            no = e == Evidence.NotDraft ? no + 1 : 0;
            if (Current == Phase.Draft)
            {
                inDraft += seconds;
                if (no >= ExitCount) { Current = Phase.OutOfDraft; inDraft = 0; }
                else if (inDraft >= MaxDraftSeconds) { Current = Phase.Uncertain; inDraft = 0; yes = 0; DraftTimedOut = true; }
                return Current;
            }
            if (yes >= EnterCount) { Current = Phase.Draft; inDraft = 0; }
            else if (e == Evidence.NotDraft) Current = Phase.OutOfDraft;
            else if (e == Evidence.Unknown) Current = Phase.Uncertain;
            return Current;
        }

        // Source certaine : pas besoin de lectures concordantes.
        public Phase Force(Phase p) { Reset(); DraftTimedOut = false; return Current = p; }

        void Reset() { yes = no = 0; inDraft = 0; }
    }

    // Règle unique d'affichage, testable sans fenêtre.
    public class PanelVisibilityController
    {
        public bool? Manual;
        public static readonly string Doc = "Visibles seulement pendant une draft, avec le client restauré. Le raccourci manuel décide jusqu'au prochain changement de phase.";
        Phase lastPhase = Phase.ClientClosed;

        public bool ShouldShow(Phase phase, bool clientVisible, bool autoEnabled)
        {
            if (phase != lastPhase) { Manual = null; lastPhase = phase; }
            if (phase == Phase.InGame) return false;
            if (Manual.HasValue) return Manual.Value && (clientVisible || phase == Phase.ClientClosed);
            return autoEnabled && phase == Phase.Draft && clientVisible;
        }
    }
}
