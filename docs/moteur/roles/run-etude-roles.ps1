# Étude des rôles adverses (agent 1). Rejoue les deux pages de ce dossier sur une copie du moteur, dans %TEMP%.
# Aucun fichier du dépôt n'est modifié. Aucun appel réseau. Aucune donnée live n'est lue : seuls les rôles écrits à la main servent.
# Usage : pwsh -File docs/moteur/roles/run-etude-roles.ps1 [-Page cas-roles|synth-roles]
param([string[]]$Page = @('cas-roles', 'synth-roles'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$web = Join-Path $repo 'web'
$W = Join-Path $env:TEMP 'draft-etude-agent1'
New-Item -ItemType Directory -Force $W | Out-Null
foreach ($f in 'draft-champions.js', 'draft-role-sheet.js', 'draft-engine.js', 'draft-data.js') { Copy-Item (Join-Path $web $f) $W -Force }
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
foreach ($p in $Page) {
  Copy-Item (Join-Path $PSScriptRoot "$p.html") $W -Force
  $prof = Join-Path $W "pf-$p"; $out = Join-Path $W "$p.dom"
  $url = 'file:///' + ((Join-Path $W "$p.html") -replace '\\', '/')
  $a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--virtual-time-budget=60000', '--dump-dom', "`"$url`"")
  $q = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
  [void]$q.WaitForExit(180000)
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  Start-Sleep -Milliseconds 500
  $m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>')
  "===== $p"
  [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
}
