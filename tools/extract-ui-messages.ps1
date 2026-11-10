# Relève les messages réellement affichés par la page, sur une série de drafts, et les rattache à l'inventaire
# (catalogue dans web/draft-messages-tests.html). Signale aussi les explications qui contredisent le calcul (constats B..).
# Copie de travail sous %TEMP% : le banc interroge la page avec eval, autorisé dans cette copie seulement, jamais dans l'application.
# Aucun réseau n'est nécessaire (les portraits manquants n'empêchent rien). Aucune donnée personnelle n'est lue.
# Exemples :
#   tools\extract-ui-messages.ps1                                   (drafts intégrées seulement)
#   tools\extract-ui-messages.ps1 -Scenarios chemin\scenarios-dev.json -Out resultat.json
#   tools\extract-ui-messages.ps1 -Limit 40 -Quick                  (essai rapide)
param(
  [string]$Work = (Join-Path $env:TEMP 'draft-v217-vocab'),
  [string]$Scenarios = '',
  [int]$Limit = 0,
  [switch]$Quick,
  [switch]$All,
  [string]$Out = ''
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (-not $Scenarios) {
  $Scenarios = @('tests\moteur\scenarios-dev.json', 'tests\moteur\evaluation\scenarios-dev.json', 'docs\moteur\evaluation\scenarios-dev.json') | ForEach-Object { Join-Path $repo $_ } | Where-Object { Test-Path $_ } | Select-Object -First 1
}
$web = Join-Path $Work 'web'
Remove-Item -Recurse -Force $web -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
Copy-Item (Join-Path $repo 'web') $web -Recurse
# Comme la compilation candidate : sans référence Meraki.
Remove-Item (Join-Path $web 'draft-role-rates.js') -ErrorAction SilentlyContinue
$index = Join-Path $web 'index.html'
[IO.File]::WriteAllText($index, [IO.File]::ReadAllText($index).Replace("script-src 'self' 'unsafe-inline'", "script-src 'self' 'unsafe-inline' 'unsafe-eval'"), [Text.UTF8Encoding]::new($false))
$scn = Join-Path $web 'draft-messages-scenarios.js'
if ($Scenarios -and (Test-Path $Scenarios)) {
  $json = (Get-Content -Raw -Encoding UTF8 $Scenarios | ConvertFrom-Json).scenarios | ConvertTo-Json -Depth 8 -Compress
  [IO.File]::WriteAllText($scn, "window.SCENARIOS_DEV = $json;", [Text.UTF8Encoding]::new($false))
  "Scénarios : $Scenarios"
} else { [IO.File]::WriteAllText($scn, 'window.SCENARIOS_DEV = [];', [Text.UTF8Encoding]::new($false)); 'Scénarios : drafts intégrées seulement' }

# Seconde copie avec des statistiques importées SYNTHÉTIQUES (draft-stats.js est lu une seule fois au chargement de la page).
$stats = Join-Path $web 'stats'
New-Item -ItemType Directory -Force $stats | Out-Null
Get-ChildItem $web | Where-Object { $_.Name -ne 'stats' } | Copy-Item -Destination $stats -Recurse
$rows = @(); $champs = @('TwistedFate', 'Fizz', 'KogMaw', 'Xerath', 'Ahri'); $mids = @('Ahri', 'Zed', 'Malzahar', 'Syndra', 'Yasuo', 'Orianna')
for ($i = 0; $i -lt 160; $i++) { $rows += @{ acct = 'Test#TEST'; champ = $champs[$i % 5]; mid = $mids[$i % 6]; win = (($i * 7) % 10) -lt 5; pos = $(if ($i % 9 -eq 0) { 'TOP' } else { 'MIDDLE' }); time = 1760000000000 + $i * 86400000 } }
$fake = @{ generated = '2026-11-01T10:00:00Z'; accounts = @('Test#TEST'); rows = $rows } | ConvertTo-Json -Depth 4 -Compress
[IO.File]::WriteAllText((Join-Path $stats 'draft-stats.js'), "// SYNTHÉTIQUE : statistiques inventées pour le relevé des messages.`nwindow.DRAFT_STATS = $fake;", [Text.UTF8Encoding]::new($false))

$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
$prof = Join-Path $Work 'profil-chrome'; $dom = Join-Path $Work 'messages.dom'
$query = @(); if ($Limit) { $query += "limit=$Limit" }; if ($Quick) { $query += 'quick=1' }; if ($All) { $query += 'all=1' }
$url = 'file:///' + ((Join-Path $web 'draft-messages-tests.html') -replace '\\', '/') + ($(if ($query) { '?' + ($query -join '&') } else { '' }))
# Hors ligne : les portraits de Data Dragon ne sont pas chargés.
$a = @('--headless=new', '--disable-gpu', '--force-prefers-reduced-motion', '--no-first-run', '--allow-file-access-from-files', '--host-resolver-rules="MAP * ~NOTFOUND"', "--user-data-dir=`"$prof`"", '--window-size=1400,900', '--virtual-time-budget=60000000', '--dump-dom', "`"$url`"")
$watch = [Diagnostics.Stopwatch]::StartNew()
$p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $dom -RedirectStandardError "$dom.err"
[void]$p.WaitForExit(3600000)
# On n'arrête que les Chrome lancés avec ce profil de travail.
Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
$m = [regex]::Match([IO.File]::ReadAllText($dom), '<pre id="out">([\s\S]*?)</pre>')
if (-not $m.Groups[1].Value) { throw 'Aucun résultat : erreur dans la page ou délai dépassé.' }
$raw = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
if (-not $Out) { $Out = Join-Path $Work 'messages.json' }
[IO.File]::WriteAllText($Out, $raw, [Text.UTF8Encoding]::new($false))
$r = $raw | ConvertFrom-Json
$s = $r.summary
"Durée : $([int]$watch.Elapsed.TotalSeconds) s, $($r.runs) drafts, $($r.steps) étapes"
if ($r.checked) { "Étapes vérifiées (fenêtre complète) : $($r.checked.steps), dont $($r.checked.incomplete) avec une draft incomplète, $($r.checked.ties) égalités affichées, $($r.checked.noFront) avec l'option frontline décochée ; adversaire direct étiqueté : $($r.checked.assumedShown) fois, dont $($r.checked.assumedThin) avec 1 ou 2 champions révélés" }
"Inventaire : $($s.inventaire) entrées dont $($s.inventaireVisible) visibles ; observées à l'écran : $($s.inventaireObserve) ; non déclenchées : $(@($s.nonDeclenches).Count)"
"Textes distincts relevés dans le DOM : $($s.observesDistincts) ; gabarits hors inventaire : $($s.nonInventories)"
"Constats (explication contraire au calcul) :"
$r.constats.PSObject.Properties | Sort-Object Name | ForEach-Object { "  $($_.Name) : $($_.Value.count)" }
if (@($r.errors).Count) { "Erreurs :"; @($r.errors) | ForEach-Object { "  $_" } }
"Résultat complet : $Out"
