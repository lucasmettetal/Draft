# Tailles calculées par le placement de DRAFT (Layout.Compute / Layout.Narrowest, appelés dans l'exe compilé), sans fenêtre.
# Windows PowerShell 5.1 (.NET Framework) : charge l'assembly de l'exe (fichier enregistré avec BOM pour 5.1). Exemple :
#   powershell.exe -File tools\layout-table.ps1 -Exe dist\DRAFT-Portable.exe [-Physique]
# Écran : barre des tâches de 40 px logiques en bas.
# Client LoL centré, en pixels logiques (physiques = logiques x échelle). Sortie : une ligne par cas, en pixels CSS (logiques).
param([Parameter(Mandatory = $true)][string]$Exe, [switch]$Physique)
$asm = [Reflection.Assembly]::LoadFrom($Exe)
$layout = $asm.GetType('DraftMid.Layout'); $rectT = $asm.GetType('DraftMid.Native+RECT'); $monT = $asm.GetType('DraftMid.Monitor')
function Rect($l, $t, $r, $b) { [Activator]::CreateInstance($rectT, [object[]]@([int]$l, [int]$t, [int]$r, [int]$b)) }
$out = @()
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
