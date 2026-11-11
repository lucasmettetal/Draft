# Fonctions de tools/run-webview-shots.ps1 : fenêtres d'UN processus (pid), capture PrintWindow, petit client CDP local.
# Pour Windows PowerShell 5.1 (powershell.exe). Jamais l'écran entier : chaque capture vise une fenêtre dont le pid est vérifié.
Add-Type -AssemblyName System.Web.Extensions
Add-Type -ReferencedAssemblies System.Drawing @'
using System; using System.Runtime.InteropServices; using System.Text; using System.Collections.Generic; using System.Drawing; using System.Drawing.Imaging;
public static class DraftShots {
  public delegate bool P(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(P p, IntPtr l);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [StructLayout(LayoutKind.Sequential)] public struct R { public int L, T, Ri, B; }
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out R r);
  [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out R r);
  [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
  [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hh, uint f);
  [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr c);
  [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);
  [DllImport("user32.dll")] static extern uint GetDpiForSystem();
  public static void DpiAware() { SetProcessDpiAwarenessContext(new IntPtr(-4)); }
  public static uint SystemDpi() { return GetDpiForSystem(); }
  public static uint WindowDpi(IntPtr h) { return GetDpiForWindow(h); }
  public static uint PidOf(IntPtr h) { uint p; GetWindowThreadProcessId(h, out p); return p; }
  // "hwnd|titre|x,y|LxH" pour chaque fenêtre visible du processus.
  public static List<string> List(uint pid) {
    var o = new List<string>();
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) { var s = new StringBuilder(256); GetWindowText(h, s, 256); R r; GetWindowRect(h, out r); o.Add(h.ToInt64() + "|" + s + "|" + r.L + "," + r.T + "|" + (r.Ri - r.L) + "x" + (r.B - r.T)); } return true; }, IntPtr.Zero);
    return o;
  }
  public static string Client(IntPtr h) { R c; GetClientRect(h, out c); return (c.Ri - c.L) + "x" + (c.B - c.T); }
  // Zone cliente demandée en pixels physiques : la fenêtre grandit de la taille de ses bords et de sa barre de titre.
  public static string SetClient(IntPtr h, int x, int y, int w, int hh) {
    R wr, cr; GetWindowRect(h, out wr); GetClientRect(h, out cr);
    int dw = (wr.Ri - wr.L) - (cr.Ri - cr.L), dh = (wr.B - wr.T) - (cr.B - cr.T);
    SetWindowPos(h, IntPtr.Zero, x, y, w + dw, hh + dh, 0x0010 | 0x0004); // SWP_NOACTIVATE | SWP_NOZORDER
    return Client(h);
  }
  // PW_RENDERFULLCONTENT (2) : le contenu composé par DWM, WebView2 compris. Renvoie "LxH|luminosité moyenne|couleurs distinctes".
  public static string Shot(IntPtr h, uint expectedPid, string file) {
    if (PidOf(h) != expectedPid) throw new Exception("fenêtre étrangère au processus lancé");
    R r; GetWindowRect(h, out r);
    int w = r.Ri - r.L, hh = r.B - r.T;
    using (var b = new Bitmap(w, hh, PixelFormat.Format32bppArgb)) {
      using (var g = Graphics.FromImage(b)) { IntPtr dc = g.GetHdc(); bool ok = PrintWindow(h, dc, 2); g.ReleaseHdc(dc); if (!ok) throw new Exception("PrintWindow a échoué"); }
      b.Save(file, ImageFormat.Png);
      return w + "x" + hh + "|" + Stats(b);
    }
  }
  public static string Stats(Bitmap b) {
    long sum = 0, n = 0; var colors = new HashSet<int>();
    for (int y = 0; y < b.Height; y += 7) for (int x = 0; x < b.Width; x += 7) { Color c = b.GetPixel(x, y); sum += (c.R + c.G + c.B) / 3; n++; if (colors.Count < 5000) colors.Add(c.ToArgb()); }
    return (n == 0 ? 0 : sum / n) + "|" + colors.Count;
  }
  public static string FileStats(string file) { using (var b = new Bitmap(file)) return b.Width + "x" + b.Height + "|" + Stats(b); }
  // Planche : images réduites côte à côte, avec leur nom.
  public static void Sheet(string[] files, string[] labels, int cols, int cellW, string outFile) {
    int rows = (files.Length + cols - 1) / cols, labelH = 34, pad = 16;
    var sizes = new List<Size>(); int cellH = 0;
    foreach (var f in files) using (var i = Image.FromFile(f)) { int hh = (int)Math.Round(i.Height * (double)cellW / i.Width); sizes.Add(new Size(cellW, hh)); cellH = Math.Max(cellH, hh); }
    using (var sheet = new Bitmap(pad + cols * (cellW + pad), pad + rows * (cellH + labelH + pad)))
    using (var g = Graphics.FromImage(sheet))
    using (var font = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
    using (var brush = new SolidBrush(Color.FromArgb(230, 236, 242))) {
      g.Clear(Color.FromArgb(18, 22, 28));
      g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
      g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
      for (int k = 0; k < files.Length; k++) {
        int cx = pad + (k % cols) * (cellW + pad), cy = pad + (k / cols) * (cellH + labelH + pad);
        g.DrawString(labels[k], font, brush, cx, cy + 6);
        using (var i = Image.FromFile(files[k])) g.DrawImage(i, new Rectangle(cx, cy + labelH, sizes[k].Width, sizes[k].Height));
      }
      sheet.Save(outFile, ImageFormat.Png);
    }
  }
}
'@
[DraftShots]::DpiAware()

function Get-ProcWindow([int]$ProcessId, [string]$Title) {
  foreach ($l in [DraftShots]::List([uint32]$ProcessId)) {
    $p = $l.Split('|')
    if ($p[1] -eq $Title) { return [pscustomobject]@{ Handle = [long]$p[0]; Title = $p[1]; Pos = $p[2]; Size = $p[3] } }
  }
}

# ---------- CDP sur 127.0.0.1 (port ouvert pour CE processus seulement) ----------
$script:CdpId = 0
$script:Json = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$script:Json.MaxJsonLength = [int]::MaxValue

function Get-CdpTargets([int]$Port) {
  try { $raw = (New-Object Net.WebClient).DownloadString("http://127.0.0.1:$Port/json/list") } catch { return @() }
  @($script:Json.DeserializeObject($raw) | Where-Object { $_['type'] -eq 'page' })
}

function Get-CdpTarget([int]$Port, [string]$Panel, [int]$TimeoutSec = 30) {
  $end = (Get-Date).AddSeconds($TimeoutSec)
  while ((Get-Date) -lt $end) {
    $t = Get-CdpTargets $Port | Where-Object { $_['url'] -match "[?&]panel=$Panel(&|$)" } | Select-Object -First 1
    if ($t) { return $t }
    Start-Sleep -Milliseconds 300
  }
}

function Invoke-Cdp($Target, [string]$Method, $Params = @{}) {
  $ws = New-Object System.Net.WebSockets.ClientWebSocket
  $ct = [Threading.CancellationToken]::None
  $ws.ConnectAsync([Uri]$Target['webSocketDebuggerUrl'], $ct).Wait()
  try {
    $script:CdpId++
    $id = $script:CdpId
    $bytes = [Text.Encoding]::UTF8.GetBytes($script:Json.Serialize(@{ id = $id; method = $Method; params = $Params }))
    $ws.SendAsync((New-Object 'ArraySegment[byte]' (, $bytes)), 'Text', $true, $ct).Wait()
    $buf = New-Object byte[] 1048576
    while ($true) {
      $ms = New-Object IO.MemoryStream
      do {
        $r = $ws.ReceiveAsync((New-Object 'ArraySegment[byte]' (, $buf)), $ct).Result
        $ms.Write($buf, 0, $r.Count)
      } while (-not $r.EndOfMessage)
      $o = $script:Json.DeserializeObject([Text.Encoding]::UTF8.GetString($ms.ToArray()))
      if ($o['id'] -eq $id) { return $o }
    }
  } finally { try { $ws.CloseAsync('NormalClosure', '', $ct).Wait() } catch {} ; $ws.Dispose() }
}

function Invoke-CdpEval($Target, [string]$Expr) {
  $r = Invoke-Cdp $Target 'Runtime.evaluate' @{ expression = $Expr; returnByValue = $true; awaitPromise = $true }
  $res = $r['result']
  if ($res -and $res['exceptionDetails']) { throw ('Exception dans la page : ' + $script:Json.Serialize($res['exceptionDetails'])) }
  if ($res -and $res['result']) { return $res['result']['value'] }
}

function Save-CdpShot($Target, [string]$File) {
  $r = Invoke-Cdp $Target 'Page.captureScreenshot' @{ format = 'png' }
  [IO.File]::WriteAllBytes($File, [Convert]::FromBase64String($r['result']['data']))
}

# ---------- Données de test et lancement isolé ----------
# Copie neuve des données d'essai ; settings.json retiré pour que DRAFT ouvre seul sa fenêtre (premier lancement).
# Le compte du journal d'essai est remplacé par un nom neutre, dans la copie seulement.
function New-ShotData([string]$Source, [string]$Dest) {
  if (Test-Path $Dest) { throw "Le dossier existe déjà : $Dest" }
  Copy-Item $Source $Dest -Recurse
  Remove-Item (Join-Path $Dest 'settings.json') -ErrorAction SilentlyContinue
  Get-ChildItem $Dest -Filter 'draft-history.js*' | ForEach-Object {
    $t = [IO.File]::ReadAllText($_.FullName, [Text.Encoding]::UTF8)
    $t = [regex]::Replace($t, '"(acct|accounts)":\s*("[^"]*"|\[[^\]]*\])', { param($m) if ($m.Groups[1].Value -eq 'acct') { '"acct":"Joueur#TEST"' } else { '"accounts":["Joueur#TEST"]' } })
    $t = [regex]::Replace($t, '[^"\[,|]+#[A-Za-z0-9]+\|', 'Joueur#TEST|')
    [IO.File]::WriteAllText($_.FullName, $t, (New-Object Text.UTF8Encoding($false)))
  }
}

function Start-ShotApp([string]$Exe, [string]$Data, [int]$Port, [string]$RunKey, [switch]$NativeScale) {
  $wvArgs = "--remote-debugging-port=$Port --remote-debugging-address=127.0.0.1 --host-resolver-rules=`"MAP * ~NOTFOUND`" --disable-features=CalculateNativeWinOcclusion"
  if (-not $NativeScale) { $wvArgs += ' --force-device-scale-factor=1' }
  $psi = New-Object Diagnostics.ProcessStartInfo $Exe
  $psi.Arguments = "--data `"$Data`" --no-hotkey --run-key `"$RunKey`""
  $psi.UseShellExecute = $false
  $psi.WorkingDirectory = Split-Path $Exe
  # Variable donnée à CE processus seulement (et à sa WebView2), jamais à la session.
  $psi.EnvironmentVariables['WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS'] = $wvArgs
  [Diagnostics.Process]::Start($psi)
}

# Arrête uniquement l'instance lancée : pid ET chemin d'exe vérifiés.
function Stop-ShotApp([int]$ProcessId, [string]$Exe) {
  $p = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
  if ($p -and $p.Path -and ([IO.Path]::GetFullPath($p.Path) -eq [IO.Path]::GetFullPath($Exe))) { Stop-Process -Id $ProcessId -Force; $p.WaitForExit(10000) | Out-Null; return $true }
  return $false
}
