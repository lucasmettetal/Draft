# Banc d'évaluation du moteur DRAFT sur scénarios SYNTHÉTIQUES. Lecture seule : rien n'est écrit dans le dépôt.
# Tout se passe dans -Work (par défaut %TEMP%\draft-etude-agent3). Chrome headless, aucun appel réseau.
#   -Generer            : régénère les scénarios (graine fixe) dans -Work\generes, sans toucher aux fichiers du dépôt.
#   -AvecEvaluation     : mesure aussi le jeu d'évaluation gelé (à réserver aux jalons, voir RAPPORT-VALIDATION.md).
#   -Instantane <json>  : compare les sorties actuelles à un instantané enregistré, et liste les différences.
#   -Criteres front,... : critères que la PR déclare modifier ; les autres changements sont signalés comme inattendus.
param(
  [string]$Work = (Join-Path $env:TEMP 'draft-etude-agent3'),
  [switch]$Generer, [switch]$AvecEvaluation,
  [string]$Instantane = '', [string[]]$Criteres = @()
)
$ErrorActionPreference = 'Stop'
$ici = $PSScriptRoot
$repo = Split-Path (Split-Path (Split-Path $ici -Parent) -Parent) -Parent
$web = Join-Path $repo 'web'
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
$run = Join-Path $Work 'run'
Remove-Item -Recurse -Force $run -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $run | Out-Null
foreach ($f in 'draft-role-sheet.js', 'draft-champions.js', 'draft-engine.js', 'draft-data.js') { Copy-Item (Join-Path $web $f) $run }
foreach ($f in 'generer-scenarios.js', 'banc-evaluation.js', 'banc.html') { Copy-Item (Join-Path $ici $f) $run }
$utf8 = [Text.UTF8Encoding]::new($false)

function Lancer([string]$config) {
  [IO.File]::WriteAllText((Join-Path $run 'banc-config.js'), $config, $utf8)
  $prof = Join-Path $run 'profil'; $out = Join-Path $run 'banc.dom'
  $url = 'file:///' + ((Join-Path $run 'banc.html') -replace '\\', '/')
  $a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--virtual-time-budget=300000', '--dump-dom', "`"$url`"")
  $p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
  [void]$p.WaitForExit(400000)
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  $m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>')
  if (-not $m.Groups[1].Value) { throw 'Aucun résultat : erreur dans la page.' }
  $json = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
  if ($json -match '^\{"erreur"') { throw $json }
  $json
}

if ($Generer) {
  $json = Lancer 'window.BANC_CONFIG = { mode:"generer" };' | ConvertFrom-Json -Depth 30
  $dest = Join-Path $Work 'generes'; New-Item -ItemType Directory -Force $dest | Out-Null
  [IO.File]::WriteAllText((Join-Path $dest 'scenarios-dev.json'), ($json.dev | ConvertTo-Json -Depth 30), $utf8)
  [IO.File]::WriteAllText((Join-Path $dest 'scenarios-eval.json'), ($json.evaluation | ConvertTo-Json -Depth 30), $utf8)
  "Scénarios régénérés dans $dest : $($json.resume.total) au total, $($json.resume.developpement) en développement, $($json.resume.evaluation) en évaluation."
  return
}

$dev = [IO.File]::ReadAllText((Join-Path $ici 'scenarios-dev.json'), $utf8)
$data = "window.SCENARIOS_DEV = $dev;`n"
if ($AvecEvaluation) { $data += "window.SCENARIOS_EVAL = " + [IO.File]::ReadAllText((Join-Path $ici 'scenarios-eval.json'), $utf8) + ";`n" }
if ($Instantane) { $data += "window.INSTANTANE_REF = " + [IO.File]::ReadAllText((Resolve-Path $Instantane), $utf8) + ";`n" }
$crit = ($Criteres | ForEach-Object { "`"$_`"" }) -join ','
$cfg = $data + "window.BANC_CONFIG = { mode:`"banc`", avecEvaluation:$(if ($AvecEvaluation) { 'true' } else { 'false' }), criteresDeclares:[$crit] };"
$res = Lancer $cfg | ConvertFrom-Json -Depth 40
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
[IO.File]::WriteAllText((Join-Path $Work "resultats-$stamp.json"), ($res.sortie | ConvertTo-Json -Depth 30), $utf8)
[IO.File]::WriteAllText((Join-Path $Work "instantane-$stamp.json"), ($res.instantane | ConvertTo-Json -Depth 30 -Compress), $utf8)
"Résultats : $(Join-Path $Work "resultats-$stamp.json")"
"Instantané : $(Join-Path $Work "instantane-$stamp.json")"
$res.sortie | ConvertTo-Json -Depth 30
