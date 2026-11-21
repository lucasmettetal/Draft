# Windows PowerShell 5.1. Planches de comparaison à partir des captures WebView2 (ShotsLib : DraftShots::Sheet).
param([Parameter(Mandatory = $true)][string]$Dir)
$ErrorActionPreference = 'Stop'
. 'C:\Users\lucas\Downloads\DRAFT-outil-captures\tools\shots\ShotsLib.ps1'
$out = Join-Path $Dir 'planches'; New-Item -ItemType Directory -Force $out | Out-Null
function Sheet([string]$name, [object[]]$items, [int]$cols, [int]$cellW) {
  $files = @(); $labels = @()
  foreach ($it in $items) { $f = Join-Path $Dir $it[0]; if (Test-Path $f) { $files += $f; $labels += $it[1] } else { Write-Warning "absent : $($it[0])" } }
  if (-not $files.Count) { return }
  [DraftShots]::Sheet([string[]]$files, [string[]]$labels, $cols, $cellW, (Join-Path $out $name))
  "$name : $($files.Count) images"
}
$sc = [ordered]@{ '01-debut' = '1. début'; '02-partielle' = '2. partielle'; '03-complete' = '3. complète'; '04-consulte' = '4. consulté'; '05-egalite' = '5. égalité';
  '06-grandpool' = '6. pool de 9'; '07-role-main' = '7. rôle à la main'; '08-sans-lecture' = '8. sans lecture'; '09-compact-league' = '9. compact, Dans League'; '10-textes-longs' = '10. textes longs'; '11-league-verrouille' = 'Dans League verrouillé' }
# 1366 x 768 à 125 % : panneau unique 222 x 511 (cas de l'utilisateur), avant / après, FR.
$it = @(); foreach ($k in $sc.Keys) { if ($k -eq '11-league-verrouille') { continue }; $it += , @("avant-fr-$k-compact-1366-125.png", "$($sc[$k]) : avant"); $it += , @("apres-fr-$k-compact-1366-125.png", "$($sc[$k]) : après") }
Sheet 'planche-1366-125-avant-apres-fr.png' $it[0..9] 4 260
Sheet 'planche-1366-125-avant-apres-fr-suite.png' $it[10..19] 4 260
# 1920 x 1080 à 125 % : panneau droit 252 x 567, avant / après, FR.
$it = @(); foreach ($k in $sc.Keys) { if ($k -eq '09-compact-league') { continue }; $it += , @("avant-fr-$k-droit-1920-125.png", "$($sc[$k]) : avant"); $it += , @("apres-fr-$k-droit-1920-125.png", "$($sc[$k]) : après") }
Sheet 'planche-1920-125-droit-avant-apres-fr.png' $it[0..9] 4 280
Sheet 'planche-1920-125-droit-avant-apres-fr-suite.png' $it[10..19] 4 280
# Panneau gauche, 1920 x 1080 à 125 %.
$it = @(); foreach ($k in '01-debut', '02-partielle', '03-complete', '08-sans-lecture', '10-textes-longs') { $it += , @("avant-fr-$k-gauche-1920-125.png", "$($sc[$k]) : avant"); $it += , @("apres-fr-$k-gauche-1920-125.png", "$($sc[$k]) : après") }
Sheet 'planche-1920-125-gauche-avant-apres-fr.png' $it 4 280
# Variantes V1, V2, V3 (et lecture approfondie après un clic sur Détails), scénario 4.
Sheet 'planche-variantes-v1-v2-v3.png' @(
  @('apres-fr-04-consulte-compact-1366-125.png', 'V1 1366 125 % : 222x511'), @('apres-fr-04-consulte-compact-1366-125-V2.png', 'V2 : 248x565 (test)'), @('apres-fr-04-consulte-compact-1366-125-V3.png', 'V3 Détails : 352x511 (test)'), @('apres-fr-04-consulte-compact-1366-125-details.png', 'V1 après « Détails »'),
  @('apres-fr-04-consulte-droit-1920-125.png', 'V1 1920 125 % : 252x567'), @('apres-fr-04-consulte-droit-1920-125-V2.png', 'V2 : 252x711 (test)'), @('apres-fr-04-consulte-droit-1920-125-V3.png', 'V3 Détails : 356x567 (test)'), @('apres-fr-04-consulte-droit-1920-125-details.png', 'V1 après « Détails »')) 4 300
# Échelles 100, 125 et 150 %, scénario 4, avant / après.
$it = @(); foreach ($v in 'avant', 'apres') { foreach ($s in @(@('compact-1366-100', '1366 100 %'), @('compact-1366-125', '1366 125 %'), @('compact-1366-150', '1366 150 %'), @('compact-1920-150', '1920 150 %'), @('droit-1920-100', '1920 100 % droit'), @('droit-1920-125', '1920 125 % droit'))) { $it += , @("$v-fr-04-consulte-$($s[0]).png", "$(if ($v -eq 'avant') { 'avant' } else { 'après' }) $($s[1])") } }
Sheet 'planche-echelles-100-125-150.png' $it 6 230
# Français / anglais, après.
Sheet 'planche-fr-en-apres.png' @(
  @('apres-fr-04-consulte-compact-1366-125.png', 'FR compact 4'), @('apres-en-04-consulte-compact-1366-125.png', 'EN compact 4'), @('apres-fr-10-textes-longs-compact-1366-125.png', 'FR compact 10'), @('apres-en-10-textes-longs-compact-1366-125.png', 'EN compact 10'),
  @('apres-fr-04-consulte-droit-1920-125.png', 'FR droit 4'), @('apres-en-04-consulte-droit-1920-125.png', 'EN droit 4'), @('apres-fr-10-textes-longs-droit-1920-125.png', 'FR droit 10'), @('apres-en-10-textes-longs-droit-1920-125.png', 'EN droit 10')) 4 260
# Dans League (donnée de test) et choix à la main.
Sheet 'planche-dans-league-ou-a-la-main.png' @(
  @('apres-fr-09-compact-league-compact-1366-125.png', 'Dans League survolé (test)'), @('apres-fr-11-league-verrouille-compact-1366-125.png', 'Dans League verrouillé (test)'), @('apres-fr-04-consulte-compact-1366-125.png', 'À la main, lecture simulée'), @('apres-fr-08-sans-lecture-compact-1366-125.png', 'À la main, sans lecture'),
  @('apres-fr-11-league-verrouille-droit-1920-125.png', 'Droit : verrouillé (test)'), @('apres-en-09-compact-league-compact-1366-125.png', 'EN : In League (test)'), @('apres-fr-08-sans-lecture-droit-1920-125.png', 'Droit : sans lecture'), @('avant-fr-09-compact-league-compact-1366-125.png', 'Avant : même draft')) 4 260
