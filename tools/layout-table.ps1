# Tailles calculées par le placement de DRAFT (Layout.Compute / Layout.Narrowest, appelés dans l'exe compilé), sans fenêtre.
# Windows PowerShell 5.1 (.NET Framework) : charge l'assembly de l'exe (fichier enregistré avec BOM pour 5.1). Exemple :
#   powershell.exe -File tools\layout-table.ps1 -Exe dist\DRAFT-Portable.exe [-Physique]
# Écran : barre des tâches de 40 px logiques en bas.
# Client LoL centré, en pixels logiques (physiques = logiques x échelle). Sortie : une ligne par cas, en pixels CSS (logiques).
# -Variantes (panneaux primaires, docs/technique/PANNEAUX-PROPOSITION.md) : pour 1366 x 768 et 1920 x 1080 à 100, 125 et 150 %, et deux écrans,
#   V1 = ce placement ; V2 = étroit de 256 et hauteur agrandie (écran utile pour l'étroit, 1,25 x client à côté du client) ;
#   V3 = V1 avec la vue « Détails » élargie à 360 pendant la lecture approfondie. Taille, zone utile et part du client couverte.
param([Parameter(Mandatory = $true)][string]$Exe, [switch]$Physique, [switch]$Variantes)
$asm = [Reflection.Assembly]::LoadFrom($Exe)
$layout = $asm.GetType('DraftMid.Layout'); $rectT = $asm.GetType('DraftMid.Native+RECT'); $monT = $asm.GetType('DraftMid.Monitor')
function Rect($l, $t, $r, $b) { [Activator]::CreateInstance($rectT, [object[]]@([int]$l, [int]$t, [int]$r, [int]$b)) }
if ($Variantes) {
  function Mon($l, $t, $w, $h, $sc, $prim) { $m = [Activator]::CreateInstance($monT); $m.Work = Rect $l $t ($l + $w) ($t + $h); $m.Scale = $sc; $m.Primary = $prim; $m }
  # Configuration ; écrans « x,y,largeur,hauteur utile,échelle » séparés par « ; » (pixels physiques) ; client centré sur le premier.
  $configs = @(@('1366x768 100 %', '0,0,1366,728,1.0', 1024, 576), @('1366x768 125 %', '0,0,1366,718,1.25', 1024, 576), @('1366x768 150 %', '0,0,1366,708,1.5', 1024, 576),
    @('1920x1080 100 %', '0,0,1920,1040,1.0', 1280, 720), @('1920x1080 125 %', '0,0,1920,1030,1.25', 1280, 720), @('1920x1080 150 %', '0,0,1920,1020,1.5', 1280, 720),
    @('2 écrans : 1366x768 125 % (client) + 1920x1080 100 % à droite', '0,0,1366,718,1.25;1366,0,1920,1040,1.0', 1024, 576),
    @('2 écrans : 1920x1080 150 % (client) + 1920x1080 100 % à gauche', '0,0,1920,1020,1.5;-1920,0,1920,1040,1.0', 1280, 720))
  $rows = @()
  foreach ($c in $configs) {
    $list = [Activator]::CreateInstance([Collections.Generic.List``1].MakeGenericType($monT))
    $first = $true; foreach ($ms in $c[1].Split(';')) { $m = $ms.Split(',') | ForEach-Object { [double]::Parse($_, [Globalization.CultureInfo]::InvariantCulture) }; $list.Add((Mon $m[0] $m[1] $m[2] $m[3] $m[4] $first)); $first = $false }
    $homeM = $list[0]; $sc = $homeM.Scale; $cw = $c[2]; $ch = $c[3]
    $l = $homeM.Work.Left + [int](($homeM.Work.W - $cw) / 2); $t = $homeM.Work.Top + [int](($homeM.Work.H - $ch) / 2); $client = Rect $l $t ($l + $cw) ($t + $ch)
    foreach ($compact in $false, $true) {
      $p = $layout.GetMethod('Compute').Invoke($null, @($client, $list, $compact))
      $boxes = @()
      if ($p.Mode -eq 'two') { $boxes += , @('gauche', $p.Left); $boxes += , @('droit', $p.Right) } elseif ($p.Mode -eq 'compact') { $boxes += , @('unique', $p.Single) } else { $boxes += , @('étroit', $layout.GetMethod('Narrowest').Invoke($null, @($client, $list)).Single) }
      foreach ($b in $boxes) {
        $r = $b[1]; $mon = $list | Where-Object { $_.Work.Left -le $r.Left -and $r.Left -lt $_.Work.Right } | Select-Object -First 1; if (-not $mon) { $mon = $homeM }; $s2 = $mon.Scale
        $du = if ($b[0] -eq 'gauche' -or $b[0] -eq 'droit') { 4 } else { 8 }
        $cover = { param($left, $w) if ($mon -ne $homeM -or $r.Top -ge $client.Bottom -or $r.Bottom -le $client.Top) { 0 } else { [Math]::Max(0, [Math]::Min($client.Right, $left + $w) - [Math]::Max($client.Left, $left)) } }
        $add = { param($v, $w, $h, $left) $cv = & $cover $left $w; $wC = [Math]::Round($w / $s2); $hC = [Math]::Round($h / $s2); "$($c[0])`t$(if ($compact) { 'unique demandé' } else { 'deux demandés' })`t$($b[0])`t$v`t${wC}x${hC}`t$($wC - $du)x$($hC - 9)`tcouvre $([Math]::Round($cv / $sc)) ($([Math]::Round(100 * $cv / $cw)) % du client)" }
        $rows += & $add 'V1' $r.W $r.H $r.Left
        $w2 = if ($b[0] -eq 'étroit') { [Math]::Min([int](256 * $s2), $mon.Work.W) } else { $r.W }
        $h2 = if ($b[0] -eq 'étroit') { [Math]::Min($mon.Work.H, [int](900 * $s2)) } else { [Math]::Min($mon.Work.H, [Math]::Max($r.H, [int]($client.H * 1.25))) }
        $left2 = if ($b[0] -eq 'étroit') { $mon.Work.Right - $w2 } elseif ($b[0] -eq 'gauche') { $r.Right - $w2 } else { $r.Left }
        $rows += & $add 'V2' $w2 $h2 $left2
        if ($b[0] -ne 'gauche') { $w3 = [Math]::Max($r.W, [int](360 * $s2)); $left3 = if ($b[0] -eq 'unique' -and $r.Right -le $client.Left) { $r.Left } else { $r.Right - $w3 }; $rows += & $add 'V3 (Détails)' $w3 $r.H $left3 }
      }
    }
  }
  $rows; return
}$out = @()
foreach ($res in @(@(1366, 768), @(1600, 900), @(1920, 1080), @(2560, 1440))) {
  foreach ($sc in @(1.0, 1.25, 1.5)) {
    $bar = [int](40 * $sc); $workH = $res[1] - $bar
    foreach ($cl in @(@(1024, 576), @(1280, 720), @(1600, 900))) {
      if ($Physique) { $cw = $cl[0]; $ch = $cl[1] } else { $cw = [int]($cl[0] * $sc); $ch = [int]($cl[1] * $sc) }
      $logW = [Math]::Round($res[0] / $sc); $logH = [Math]::Round($workH / $sc)
      if ($cw -gt $res[0] -or $ch -gt $workH) { $out += "$($res[0])x$($res[1])`t$([int]($sc*100))`t$($cl[0])x$($cl[1])`tne tient pas (écran utile $logW x $logH logiques)`t`t`t"; continue }
      $mon = [Activator]::CreateInstance($monT); $mon.Work = Rect 0 0 $res[0] $workH; $mon.Scale = $sc; $mon.Primary = $true
      $list = [Activator]::CreateInstance([Collections.Generic.List``1].MakeGenericType($monT)); $list.Add($mon)
      $l = [int](($res[0] - $cw) / 2); $t = [int](($workH - $ch) / 2)
      $client = Rect $l $t ($l + $cw) ($t + $ch)
      $free = [Math]::Round(($res[0] - $cw) / 2 / $sc)
      $two = $layout.GetMethod('Compute').Invoke($null, @($client, $list, $false))
      $one = $layout.GetMethod('Compute').Invoke($null, @($client, $list, $true))
      $desc = { param($p) if ($p.Mode -eq 'two') { "deux $([Math]::Round($p.Right.W / $sc))x$([Math]::Round($p.Right.H / $sc))" } elseif ($p.Mode -eq 'compact') { "unique $([Math]::Round($p.Single.W / $sc))x$([Math]::Round($p.Single.H / $sc))" } else { $n = $layout.GetMethod('Narrowest').Invoke($null, @($client, $list)); $cover = [Math]::Max(0, $n.Single.Right - $n.Single.W - 0); $ov = [Math]::Max(0, ($client.Right - ($n.Single.Left))); "étroit $([Math]::Round($n.Single.W / $sc))x$([Math]::Round($n.Single.H / $sc)), couvre $([Math]::Round($ov / $sc)) du client" } }
      $out += "$($res[0])x$($res[1])`t$([int]($sc*100))`t$($cl[0])x$($cl[1])`tlibre $free de chaque côté`t$(& $desc $two)`t$(& $desc $one)"
    }
  }
}
$out
