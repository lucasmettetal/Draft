# Audit chiffré du moteur, reproductible (graines fixes). Ne modifie rien : la page travaille sur une copie.
# Lit le calcul de rôles local, s'il existe, en lecture seule.
param(
  [string]$DataDir = (Join-Path $env:LOCALAPPDATA 'DraftMid'),
  [string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-coaching\audit'),
  [string]$Page = 'engine-audit.html'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent; $web = Join-Path $repo 'web'
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
foreach ($f in 'draft-engine.js', 'draft-data.js', 'draft-champions.js', 'draft-role-sheet.js') { Copy-Item (Join-Path $web $f) $Work }
Copy-Item (Join-Path $PSScriptRoot "experiments\$Page") $Work
$live = Join-Path $DataDir 'draft-role-rates-live.js'
if (-not (Test-Path $live)) { throw "Aucun calcul de rôles dans $DataDir." }
Copy-Item $live (Join-Path $Work 'draft-role-rates-live.js')
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
$prof = Join-Path $Work 'pf'; $out = Join-Path $Work 'r.dom'
$url = 'file:///' + ((Join-Path $Work $Page) -replace '\\', '/')
$a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--virtual-time-budget=240000', '--dump-dom', "`"$url`"")
$p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
if (-not $p.WaitForExit(280000)) { 'Délai dépassé.' }
Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
$m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>')
if (-not $m.Groups[1].Value) { throw 'Aucun résultat : erreur dans la page.' }
$json = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
[IO.File]::WriteAllText((Join-Path $Work 'audit.json'), $json, [Text.UTF8Encoding]::new($false))
$json
