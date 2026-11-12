# Planche des nouvelles sections (captures 1366 x 768 de run-webview-shots.ps1 -Version apres), assemblée avec System.Drawing.
# Usage (Windows PowerShell 5.1) : powershell -NoProfile -File tools\shots\planche.ps1 -Dir docs\ux\captures-webview2\apres
param([Parameter(Mandatory = $true)][string]$Dir, [string]$Size = '1366x768')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ShotsLib.ps1')
$Dir = (Resolve-Path $Dir).Path
$items = [ordered]@{
  'accueil' = 'Accueil'; 'mon-pool' = 'Mon pool'; 'simulateur' = 'Simulateur'; 'mes-statistiques' = 'Mes statistiques'
  'reglages-general' = 'Réglages : Général'; 'reglages-connexion' = 'Réglages : Connexion League'; 'reglages-affichage' = 'Réglages : Affichage et fenêtres'
  'reglages-donnees' = 'Réglages : Données et confidentialité'; 'reglages-mises-a-jour' = 'Réglages : Mises à jour'; 'reglages-diagnostic' = 'Réglages : Diagnostic et informations'
  'simulateur-criteres' = 'Simulateur : Analyse > Critères'; 'simulateur-conseils' = 'Simulateur : Conseils'
}
$files = [string[]]($items.Keys | ForEach-Object { Join-Path $Dir "apres-$_-$Size.png" })
$labels = [string[]]($items.Values | ForEach-Object { "$_ ($($Size.Replace('x', ' x ')), vraie WebView2)" })
$out = Join-Path $Dir 'planche-sections.png'
[DraftShots]::Sheet($files, $labels, 3, 640, $out)
"$out : " + [DraftShots]::FileStats($out)