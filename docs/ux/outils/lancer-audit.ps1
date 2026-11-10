# Compile la copie de travail et lance DRAFT dans sa vraie WebView2, sur un dossier de données temporaire et vide.
# Le port CDP n'écoute que sur 127.0.0.1. Rien n'est écrit dans %LOCALAPPDATA%\DraftMid ni dans la clé Run réelle.
# Exemple : .\docs\ux\outils\lancer-audit.ps1 ; puis . .\docs\ux\outils\cdp.ps1 ; Get-CdpTargets
# Sans settings.json, DRAFT croit à un premier lancement et ouvre ses réglages : c'est le seul moyen d'avoir une page sans cliquer sur l'icône.
param([string]$Work = (Join-Path $env:TEMP 'draft-ux'), [int]$Port = 9339, [switch]$NoBuild, [switch]$FirstRun)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
if (-not $NoBuild) { & (Join-Path $repo 'build.ps1') -Out (Join-Path $Work 'build') | Out-Null }
$data = Join-Path $Work 'data'
New-Item -ItemType Directory -Force $data | Out-Null
if ($FirstRun) { Remove-Item (Join-Path $data 'settings.json') -ErrorAction SilentlyContinue }
$env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = "--remote-debugging-port=$Port --remote-debugging-address=127.0.0.1"
try {
  $p = Start-Process (Join-Path $Work 'build\DRAFT-Portable.exe') -ArgumentList @('--data', "`"$data`"", '--no-hotkey', '--run-key', 'Software\DRAFT-UX-Audit\Run') -PassThru
} finally { Remove-Item Env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS }
$p.Id | Set-Content (Join-Path $Work 'pid.txt')
"DRAFT d'audit lancé : pid $($p.Id), données $data, CDP 127.0.0.1:$Port"
