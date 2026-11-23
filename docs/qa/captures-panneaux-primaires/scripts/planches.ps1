# Windows PowerShell 5.1. Planches de la V2 DÉFINITIVE, à partir des captures WebView2 de captures-panneaux.ps1.
#   -Dir   : dossier v2-definitif (sous-dossiers fr/ et en/ ; planches écrites dans planches/).
#   -Avant : captures « avant » de main 4fb621f (V1), réutilisées telles quelles (par défaut ../avant/).
# Version précédente (planches V1, variantes V2 et V3 de test, échelles 100 et 150 %) : commit 5e07ce7.
# Images à leur taille réelle (pixels physiques) : la différence de taille entre V1 et V2 se voit. Chaque planche porte un en-tête avec
# la résolution, l'échelle et « données fictives / client simulé » ; chaque image garde son bandeau.
param([Parameter(Mandatory = $true)][string]$Dir, [string]$Avant = '')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$Dir = (Resolve-Path $Dir).Path
if (-not $Avant) { $Avant = Join-Path (Split-Path $Dir) 'avant' }
$out = Join-Path $Dir 'planches'; New-Item -ItemType Directory -Force $out | Out-Null
$FICTIF = 'Données fictives / client simulé (aucun client LoL, état du programme injecté).'
$SOURCES = 'Avant : main 4fb621f (V1). Après : branche feat/panneaux-primaires 5e07ce7 (V2).'

# Planche : en-tête (titre, puis lignes), images à la taille réelle, légende au-dessus de chaque image (deux lignes au plus).
function Sheet([string]$name, [string[]]$head, [object[]]$items, [int]$cols) {
  $imgs = @(); $labels = @(); $kinds = @()
  foreach ($it in $items) {
    $f = if ($it[0] -like 'avant-*') { Join-Path $Avant $it[0] } elseif ($it[0] -like 'apres-en-*') { Join-Path $Dir "en\$($it[0])" } else { Join-Path $Dir "fr\$($it[0])" }
    if (Test-Path $f) { $imgs += [Drawing.Image]::FromFile($f); $labels += ($it[1] -replace ' %', "$([char]0xA0)%"); $kinds += $(if ($it[0] -like 'avant-*') { 'avant' } else { 'apres' }) } else { Write-Warning "absent : $f" }
  }
  if (-not $imgs.Count) { return }
  $head = @($head | ForEach-Object { $_ -replace ' %', "$([char]0xA0)%" })   # « 125 % » jamais coupé avant « % »
  try {
    $pad = 16; $labelH = 42; $cellW = ($imgs | Measure-Object Width -Maximum).Maximum; $cellH = ($imgs | Measure-Object Height -Maximum).Maximum
    $rows = [int][Math]::Ceiling($imgs.Count / $cols)
    $tf = New-Object Drawing.Font('Segoe UI', [single]20, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
    $hf = New-Object Drawing.Font('Segoe UI', [single]15, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)
    $lf = New-Object Drawing.Font('Segoe UI', [single]14, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
    $W = $pad + $cols * ($cellW + $pad)
    $headH = 12 + 28 + 21 * ($head.Count - 1) + 8
    $bmp = New-Object Drawing.Bitmap ([int]$W), ([int]($headH + $rows * ($labelH + $cellH + $pad) + $pad))
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.Clear([Drawing.Color]::FromArgb(18, 22, 28)); $g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $txt = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(230, 236, 242)); $sub = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(176, 186, 198))
    $cAvant = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(214, 190, 150)); $cApres = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(121, 198, 218))
    $g.DrawString($head[0], $tf, $txt, (New-Object Drawing.RectangleF($pad, 12, ($W - 2 * $pad), 28)))
    for ($k = 1; $k -lt $head.Count; $k++) { $g.DrawString($head[$k], $hf, $sub, (New-Object Drawing.RectangleF($pad, (40 + 21 * ($k - 1)), ($W - 2 * $pad), 21))) }
    $fmt = New-Object Drawing.StringFormat; $fmt.Trimming = 'EllipsisWord'
    for ($k = 0; $k -lt $imgs.Count; $k++) {
      $cx = $pad + ($k % $cols) * ($cellW + $pad); $cy = [int]($headH + [Math]::Floor($k / $cols) * ($labelH + $cellH + $pad))
      $g.DrawString($labels[$k], $lf, $(if ($kinds[$k] -eq 'avant') { $cAvant } else { $cApres }), (New-Object Drawing.RectangleF($cx, $cy, $cellW, $labelH)), $fmt)
      $g.DrawImage($imgs[$k], [int]$cx, [int]($cy + $labelH), [int]$imgs[$k].Width, [int]$imgs[$k].Height)
    }
    $g.Dispose(); $tf.Dispose(); $hf.Dispose(); $lf.Dispose(); $txt.Dispose(); $sub.Dispose(); $cAvant.Dispose(); $cApres.Dispose()
    $bmp.Save((Join-Path $out $name), [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
  } finally { $imgs | ForEach-Object { $_.Dispose() } }
  "$name : $($imgs.Count) images"
}
$sc = [ordered]@{ '01-debut' = '1. début'; '03-complete' = '3. complète'; '04-consulte' = '4. consulté'; '05-egalite' = '5. égalité';
  '07-role-main' = '7. rôle à la main'; '08-sans-lecture' = '8. sans lecture'; '09-compact-league' = '9. Dans League survolé'; '11-league-verrouille' = '11. Dans League verrouillé' }
function Pairs([string[]]$keys, [string]$panel, [string]$res, [string]$v1, [string]$v2) {
  $it = @()
  foreach ($k in $keys) {
    $it += , @("avant-fr-$k-$panel-$res.png", "$($sc[$k]) · avant (V1 $v1)")
    $it += , @("apres-fr-$k-$panel-$res.png", "$($sc[$k]) · après (V2 $v2)$(if ($k -in '09-compact-league', '11-league-verrouille') { ' · test' })")
  }
  , $it
}

# 1366 x 768 à 125 % : panneau étroit (compact), avant / après, FR.
$h = @('Avant / après V2 · panneau étroit (compact) · 1366 × 768 à 125 % · FR', "$SOURCES Zone utile : 222 × 511 (V1) puis 248 × 565 (V2).", $FICTIF)
Sheet 'planche-1366-125-avant-apres-fr-1.png' $h (Pairs '01-debut', '03-complete', '04-consulte', '05-egalite' 'compact' '1366-125' '222×511' '248×565') 4
Sheet 'planche-1366-125-avant-apres-fr-2.png' $h (Pairs '07-role-main', '08-sans-lecture', '09-compact-league', '11-league-verrouille' 'compact' '1366-125' '222×511' '248×565') 4
# 1920 x 1080 à 125 % : panneau droit, avant / après, FR.
$h = @('Avant / après V2 · panneau droit · 1920 × 1080 à 125 % · FR', "$SOURCES Zone utile : 252 × 567 (V1) puis 252 × 711 (V2).", $FICTIF)
Sheet 'planche-1920-125-droit-avant-apres-fr-1.png' $h (Pairs '01-debut', '03-complete', '04-consulte', '05-egalite' 'droit' '1920-125' '252×567' '252×711') 4
Sheet 'planche-1920-125-droit-avant-apres-fr-2.png' $h (Pairs '07-role-main', '08-sans-lecture', '11-league-verrouille' 'droit' '1920-125' '252×567' '252×711') 4
# 1920 x 1080 à 125 % : panneau gauche, avant / après, FR (5 : après seulement, pas de capture avant).
$h = @('Avant / après V2 · panneau gauche · 1920 × 1080 à 125 % · FR', "$SOURCES Zone utile : 252 × 567 (V1) puis 252 × 711 (V2).", $FICTIF)
$it = Pairs '01-debut', '03-complete', '08-sans-lecture' 'gauche' '1920-125' '252×567' '252×711'
$it += , @('apres-fr-05-egalite-gauche-1920-125.png', '5. égalité · après (V2 252×711), pas de capture avant')
Sheet 'planche-1920-125-gauche-avant-apres-fr.png' $h $it 4
# Français / anglais, après V2.
$fe = { param($keys, $panel, $res) $r = @(); foreach ($k in $keys) { $r += , @("apres-fr-$k-$panel-$res.png", "$($sc[$k]) · FR"); $r += , @("apres-en-$k-$panel-$res.png", "$($sc[$k]) · EN") }; , $r }
Sheet 'planche-fr-en-apres-v2-1366-125.png' @('FR / EN après V2 · panneau étroit (compact) · 1366 × 768 à 125 % · zone 248 × 565', 'Branche feat/panneaux-primaires 5e07ce7 (V2).', $FICTIF) (& $fe @('04-consulte', '05-egalite', '07-role-main', '08-sans-lecture') 'compact' '1366-125') 4
Sheet 'planche-fr-en-apres-v2-1920-125.png' @('FR / EN après V2 · panneau droit · 1920 × 1080 à 125 % · zone 252 × 711', 'Branche feat/panneaux-primaires 5e07ce7 (V2).', $FICTIF) (& $fe @('04-consulte', '05-egalite', '07-role-main', '11-league-verrouille') 'droit' '1920-125') 4
# Compact V2 : panneau étroit à 1366 x 768 et panneau unique demandé à 1920 x 1080, 125 %, FR.
$it = @(); foreach ($k in '01-debut', '04-consulte', '08-sans-lecture', '09-compact-league') { $it += , @("apres-fr-$k-compact-1366-125.png", "$($sc[$k]) · 1366 × 768, 125 %"); $it += , @("apres-fr-$k-compact-1920-125.png", "$($sc[$k]) · 1920 × 1080, 125 %") }
Sheet 'planche-compact-v2.png' @('Compact après V2 · 1366 × 768 à 125 % (étroit, zone 248 × 565) et 1920 × 1080 à 125 % (unique demandé, zone 248 × 711) · FR', 'Branche feat/panneaux-primaires 5e07ce7 (V2).', $FICTIF) $it 4
# « Dans League » (donnée de test) ou choix « à la main ».
Sheet 'planche-dans-league-ou-a-la-main-v2.png' @('« Dans League » ou « à la main » · après V2 · 1366 × 768 et 1920 × 1080 à 125 %', '« Dans League » : DONNÉE DE TEST (leagueSlot) ; la lecture réelle viendra de la PR #40. Branche 5e07ce7 (V2) ; avant : main 4fb621f.', $FICTIF) @(
  @('apres-fr-09-compact-league-compact-1366-125.png', 'Dans League survolé (test) · compact 1366, 125 %'), @('apres-fr-11-league-verrouille-compact-1366-125.png', 'Dans League verrouillé (test) · compact 1366, 125 %'),
  @('apres-fr-04-consulte-compact-1366-125.png', 'À la main, lecture simulée · compact 1366, 125 %'), @('apres-fr-08-sans-lecture-compact-1366-125.png', 'À la main, sans lecture · compact 1366, 125 %'),
  @('apres-fr-11-league-verrouille-droit-1920-125.png', 'Dans League verrouillé (test) · droit 1920, 125 %'), @('apres-fr-08-sans-lecture-droit-1920-125.png', 'À la main, sans lecture · droit 1920, 125 %'),
  @('apres-en-09-compact-league-compact-1366-125.png', 'EN : In League (test) · compact 1366, 125 %'), @('avant-fr-09-compact-league-compact-1366-125.png', 'Avant (V1 222×511), même draft · compact 1366, 125 %')) 4
