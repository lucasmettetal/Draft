# Liste et capture les fenêtres visibles d'UN processus donné (jamais l'écran entier).
# À lancer avec Windows PowerShell 5.1 (powershell.exe), qui compile ce code sans référence supplémentaire.
# Usage : . .\docs\ux\outils\fenetres.ps1 ; Get-ProcWindows -ProcessId 1234 ; Save-WindowShot -Handle h -File x.png ; Close-Window -Handle h
Add-Type -ReferencedAssemblies System.Drawing @'
using System; using System.Runtime.InteropServices; using System.Text; using System.Collections.Generic; using System.Drawing; using System.Drawing.Imaging;
public static class DraftUxWin {
  public delegate bool P(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(P p, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [StructLayout(LayoutKind.Sequential)] public struct R { public int L, T, Ri, B; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr c);
  public static List<string> List(uint pid) {
    var o = new List<string>();
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) { var s = new StringBuilder(256); GetWindowText(h, s, 256); R r; GetWindowRect(h, out r); o.Add(h + "|" + s + "|" + r.L + "," + r.T + "|" + (r.Ri - r.L) + "x" + (r.B - r.T)); } return true; }, IntPtr.Zero);
    return o;
  }
  public static void Shot(IntPtr h, string file) {
    R r; GetWindowRect(h, out r);
    using (var b = new Bitmap(r.Ri - r.L, r.B - r.T)) { using (var g = Graphics.FromImage(b)) { IntPtr dc = g.GetHdc(); PrintWindow(h, dc, 2); g.ReleaseHdc(dc); } b.Save(file, ImageFormat.Png); }
  }
  public static void Close(IntPtr h) { SendMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero); }
  // Comme un clic sur la croix ou Alt+F4 : WM_SYSCOMMAND SC_CLOSE (WinForms y voit CloseReason.UserClosing).
  public static void CloseAsUser(IntPtr h) { SendMessage(h, 0x0112, (IntPtr)0xF060, IntPtr.Zero); }
}
'@
# Coordonnées physiques : sans cela, une fenêtre mise à l'échelle par Windows serait coupée à la capture.
[void][DraftUxWin]::SetProcessDpiAwarenessContext([IntPtr](-4))
function Get-ProcWindows([int]$ProcessId) { [DraftUxWin]::List([uint32]$ProcessId) }
function Save-WindowShot([long]$Handle, [string]$File) { [DraftUxWin]::Shot([IntPtr]$Handle, $File) }
# WM_CLOSE brut : WinForms y voit CloseReason.None et détruit la fenêtre. À réserver aux dialogues (équivaut à Annuler).
function Close-Window([long]$Handle) { [DraftUxWin]::Close([IntPtr]$Handle) }
function Close-WindowAsUser([long]$Handle) { [DraftUxWin]::CloseAsUser([IntPtr]$Handle) }
