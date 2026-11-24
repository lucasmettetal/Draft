# Panneaux primaires (docs/technique/PANNEAUX-PROPOSITION.md) : mesures des variantes V1, V2 et V3 dans Chrome sans interface.
# web/draft-panneaux-tests.html en mode « lot » : chaque cas est un panneau à sa zone utile (pixels CSS), dans un scénario FICTIF.
# Tailles : V1 = placement actuel (Layout.Compute / Narrowest de l'exe, voir tools/layout-table.ps1 -Variantes) ; V2 = panneau étroit
# de 256 et hauteur agrandie ; V3 = vue étendue « Détails » de 360 pendant la lecture approfondie (simulée : elle n'est pas dans l'application).
# Copie de web/ avec 'unsafe-eval' dans la CSP de la copie seulement (comme run-tailles-panneaux.ps1). Rien n'est changé dans l'application.
# Exemple : tools\run-panneaux-tests.ps1 -Work $env:TEMP\panneaux [-Csv docs\qa\captures-panneaux-primaires\mesures-variantes.csv]
param([string]$Root = (Split-Path $PSScriptRoot -Parent), [string]$Work = (Join-Path $env:TEMP 'draft-panneaux-mesures'), [string]$Csv = '')
$ErrorActionPreference = 'Stop'
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
$web = Join-Path $Work 'web'; Remove-Item -Recurse -Force $web -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force $Work | Out-Null
Copy-Item (Join-Path $Root 'web') $web -Recurse
$idx = Join-Path $web 'index.html'
[IO.File]::WriteAllText($idx, [IO.File]::ReadAllText($idx).Replace("script-src 'self' 'unsafe-inline'", "script-src 'self' 'unsafe-inline' 'unsafe-eval'"), [Text.UTF8Encoding]::new($false))
$base = 'file:///' + ((Join-Path $web 'draft-panneaux-tests.html') -replace '\\', '/')
# Échelle ; configuration ; variante ; panneau ; zone utile (CSS). Zone utile = taille du panneau moins 4 (droit, gauche) ou 8 (unique,
# étroit) en largeur et 9 en hauteur, comme mesuré dans la WebView2 de l'e2e.
$sizes = @(
  @(1.25, '1366x768 125 %', 'V1', 'compact', '222x511'), @(1.25, '1366x768 125 %', 'V2', 'compact', '248x565'), @(1.25, '1366x768 125 %', 'V3', 'compact', '352x511'),
  @(1.25, '1920x1080 125 %', 'V1', 'right', '252x567'), @(1.25, '1920x1080 125 %', 'V2', 'right', '252x711'), @(1.25, '1920x1080 125 %', 'V3', 'right', '356x567'),
  @(1.25, '1920x1080 125 %', 'V1', 'left', '252x567'), @(1.25, '1920x1080 125 %', 'V2', 'left', '252x711'),
  @(1.25, '1920x1080 125 % unique', 'V1', 'compact', '248x567'), @(1.25, '1920x1080 125 % unique', 'V2', 'compact', '248x711'), @(1.25, '1920x1080 125 % unique', 'V3', 'compact', '352x567'),
  @(1.0, '1366x768 100 %', 'V1', 'compact', '222x567'), @(1.0, '1366x768 100 %', 'V2', 'compact', '248x719'), @(1.0, '1366x768 100 %', 'V3', 'compact', '352x567'),
  @(1.0, '1920x1080 100 %', 'V1', 'right', '316x711'), @(1.0, '1920x1080 100 %', 'V2', 'right', '316x891'), @(1.0, '1920x1080 100 %', 'V3', 'right', '356x711'),
  @(1.0, '1920x1080 100 %', 'V1', 'left', '316x711'), @(1.0, '1920x1080 100 %', 'V2', 'left', '316x891'),
  @(1.0, '2 écrans 1366 125 % + 1920 100 %', 'V1', 'right', '336x891'), @(1.0, '2 écrans 1366 125 % + 1920 100 %', 'V1', 'compact', '332x891'),
  @(1.5, '1366x768 150 %', 'V1', 'compact', '222x463'), @(1.5, '1366x768 150 %', 'V2', 'compact', '248x463'), @(1.5, '1366x768 150 %', 'V3', 'compact', '352x463'),
  @(1.5, '1920x1080 150 %', 'V1', 'compact', '222x511'), @(1.5, '1920x1080 150 %', 'V2', 'compact', '248x671'), @(1.5, '1920x1080 150 %', 'V3', 'compact', '352x511')
)
# Lecture immédiate : scénarios mesurés pour V1 et V2 (V3 garde la lecture immédiate de V1). Panneau gauche : ses propres scénarios.
$immediate = @('consulte:fr', 'connecte:fr', 'debut:fr', 'partielle:fr', 'egalite:fr', 'egalitevide:fr', 'rolemain:fr', 'longs:en', 'sanslecture:fr')
$leftSc = @('partielle:fr', 'complete:fr', 'sanslecture:fr', 'longs:en')
$rows = @()
foreach ($scale in 1.0, 1.25, 1.5) {
  $cases = @()
  foreach ($s in $sizes | Where-Object { $_[0] -eq $scale }) {
    $list = if ($s[3] -eq 'left') { $leftSc } elseif ($s[2] -eq 'V3') { @() } else { $immediate }
    foreach ($x in $list) { $p = $x.Split(':'); $cases += "$($s[3]):$($s[4]):$($p[0]):$($p[1]):v1" }
    # Lecture approfondie : toutes les sections ouvertes (champion consulté). V3 : la vue étendue « Détails ».
    if ($s[3] -ne 'left') { $cases += "$($s[3]):$($s[4]):consulte:fr:$(if ($s[2] -eq 'V3') { 'v3' } else { 'v1' }):all" }
  }
  $url = "$base`?lot=$([uri]::EscapeDataString($cases -join '~'))"
  $pf = Join-Path $Work "pf-$scale"
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $dom = & $chrome --headless=new --disable-gpu --no-first-run --allow-file-access-from-files "--user-data-dir=$pf" --window-size=900,1000 "--force-device-scale-factor=$scale" --virtual-time-budget=900000 --dump-dom $url 2>$null | Out-String
  $json = [Net.WebUtility]::HtmlDecode([regex]::Match($dom, '<pre id="out">([\s\S]*?)</pre>').Groups[1].Value)
  if (-not $json) { Write-Host "ECHEC : échelle $scale : aucune mesure"; continue }
  $res = $json | ConvertFrom-Json
  if ($res -isnot [array] -and $res.erreur) { Write-Host "ECHEC : échelle $scale : $($res.erreur)"; continue }
  Write-Host "Échelle $scale : $($cases.Count) cas en $([int]$sw.Elapsed.TotalSeconds) s"
  foreach ($s in $sizes | Where-Object { $_[0] -eq $scale }) {
    $mine = @($res | Where-Object { $_.panel -eq $s[3] -and $_.taille -eq $s[4] })
    $immOnly = @($mine | Where-Object { $_.ouvert -ne 'all' })
    $deep = $mine | Where-Object { $_.ouvert -eq 'all' } | Select-Object -First 1
    $hidden = @($immOnly | Where-Object { $_.caches.Count } | ForEach-Object { "$($_.scenario) ($($_.lang)) : $($_.caches -join ', ')" })
    $long = $immOnly | Where-Object { $_.scenario -eq 'longs' } | Select-Object -First 1
    $rows += [pscustomobject]@{
      configuration = $s[1]; variante = $s[2]; panneau = $s[3]; zone_utile = $s[4]
      essentiel_visible = if ($s[2] -eq 'V3') { 'comme V1' } else { "$(@($immOnly | Measure-Object -Property visibles -Sum).Sum)/$(@($immOnly | Measure-Object -Property essentiel -Sum).Sum)" }
      caches = if ($hidden.Count) { $hidden -join ' ; ' } else { '' }
      approfondie_hauteur = if ($deep) { $deep.approfondie.hauteur } else { '' }
      approfondie_fenetre = if ($deep) { $deep.approfondie.fenetre } else { '' }
      approfondie_part = if ($deep) { $deep.approfondie.part } else { '' }
      bouton_long_lignes = if ($long -and $long.bouton) { $long.bouton.lignes } else { '' }
      police_min = (@($mine | ForEach-Object { $_.police.min }) | Measure-Object -Minimum).Minimum
      debordement_x = (@($mine | ForEach-Object { $_.debordementX }) | Measure-Object -Maximum).Maximum
      cles_absentes = (@($mine | ForEach-Object { $_.cles }) | Select-Object -Unique) -join ' '
    }
  }
}
if ($Csv) { $rows | Export-Csv $Csv -NoTypeInformation -Encoding UTF8 -Delimiter ';' }
$rows | Format-Table configuration, variante, panneau, zone_utile, essentiel_visible, approfondie_hauteur, approfondie_fenetre, approfondie_part, bouton_long_lignes, police_min, debordement_x -AutoSize | Out-String -Width 220
$rows | Where-Object { $_.caches } | ForEach-Object { "Caché ($($_.configuration) $($_.variante) $($_.panneau) $($_.zone_utile)) : $($_.caches)" }
