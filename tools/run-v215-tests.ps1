# Tests de la V2.15 sur la vraie page : égalités, conseils, expérience, réglages. Copie de travail, historiques fictifs.
param([string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-v215\v215'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
Copy-Item (Join-Path $repo 'web') $Work -Recurse
# Comme la compilation candidate : sans Meraki.
Remove-Item (Join-Path $Work 'draft-role-rates.js') -ErrorAction SilentlyContinue
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
$prof = Join-Path $Work 'pf'; $out = Join-Path $Work 'r.dom'; $url = 'file:///' + ((Join-Path $Work 'draft-v215-tests.html') -replace '\\', '/')
$a = @('--headless=new', '--disable-gpu', '--force-prefers-reduced-motion', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--window-size=1400,900', '--virtual-time-budget=400000', '--dump-dom', "`"$url`"")
$p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
[void]$p.WaitForExit(500000)
Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
$m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>')
if (-not $m.Groups[1].Value) { throw 'Aucun résultat : erreur dans la page ou délai dépassé.' }
$r = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value) | ConvertFrom-Json
"V2.15 : $($r.ok) réussis, $(@($r.ko).Count) échecs"
@($r.ko) | ForEach-Object { "  ÉCHEC : $_" }
"Égalités en tête sur 60 drafts : " + (($r.notes.PSObject.Properties | ForEach-Object { "$($_.Name -replace '_egalites_sur_60','') $($_.Value)" }) -join ', ')
