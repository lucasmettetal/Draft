# Tests de non-régression de l'attribution des rôles adverses (tests/roles), dans Chrome sans interface, sur une copie du moteur.
# Propriétés (sommes à 1, corrections, aucun adversaire inventé) + instantané du comportement actuel sur les cas atypiques SYNTHÉTIQUES.
# -UpdateSnapshot : réécrit tests/roles/instantane-roles.js d'après le moteur actuel. À faire seulement pour un changement voulu.
# Code de sortie : 0 si tout passe, 1 sinon.
param([string]$Work = (Join-Path $env:TEMP 'draft-role-tests'), [switch]$UpdateSnapshot)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent; $web = Join-Path $repo 'web'; $cases = Join-Path $repo 'tests\roles'
$snapFile = Join-Path $cases 'instantane-roles.js'
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
foreach ($f in 'draft-champions.js', 'draft-role-sheet.js', 'draft-engine.js', 'draft-data.js') { Copy-Item (Join-Path $web $f) $Work }
foreach ($f in 'cas-atypiques.js', 'roles-tests.js') { Copy-Item (Join-Path $cases $f) $Work }
$snapTag = ''
if ((Test-Path $snapFile) -and -not $UpdateSnapshot) { Copy-Item $snapFile $Work; $snapTag = '<script src="instantane-roles.js"></script>' }
$html = '<!DOCTYPE html><html><head><meta charset="utf-8"></head><body><pre id="out"></pre><script>try { localStorage.clear(); } catch (e) {} window.addEventListener("error", e => { document.getElementById("out").textContent = JSON.stringify({ erreur: e.message + " (" + (e.filename || "").split("/").pop() + ":" + e.lineno + ")" }); });</script>' +
  '<script src="draft-champions.js"></script><script src="draft-role-sheet.js"></script><script src="draft-engine.js"></script><script src="draft-data.js"></script>' +
  '<script src="cas-atypiques.js"></script>' + $snapTag + '<script src="roles-tests.js"></script></body></html>'
[IO.File]::WriteAllText((Join-Path $Work 'roles-tests.html'), $html, [Text.UTF8Encoding]::new($false))
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
$prof = Join-Path $Work 'pf-roles'; $out = Join-Path $Work 'roles-tests.dom'
$url = 'file:///' + ((Join-Path $Work 'roles-tests.html') -replace '\\', '/')
$a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--virtual-time-budget=120000', '--dump-dom', "`"$url`"")
$p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
[void]$p.WaitForExit(200000)
Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
$m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>')
if (-not $m.Groups[1].Value) { Write-Host 'Rôles : aucun résultat, erreur dans la page.'; exit 1 }
$r = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value) | ConvertFrom-Json -Depth 20
if ($r.erreur) { Write-Host "Rôles : test interrompu : $($r.erreur)"; exit 1 }
if ($UpdateSnapshot) {
  # Une ligne par cas : un changement se lit facilement dans le diff.
  $snap = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value) | ConvertFrom-Json -AsHashtable -Depth 20
  $lines = foreach ($id in ($r.instantane.PSObject.Properties.Name)) { '  ' + (ConvertTo-Json -InputObject $id -Compress) + ':' + (ConvertTo-Json -InputObject $snap.instantane[$id] -Depth 10 -Compress) }
  $body = "// Instantané du comportement ACTUEL du moteur sur les cas atypiques SYNTHÉTIQUES (tests/roles/cas-atypiques.js).`n// Ce n'est pas une vérité : il sert à repérer un changement. Généré par tools/run-role-tests.ps1 -UpdateSnapshot.`nconst ROLE_SNAPSHOT = {`n" + ($lines -join ",`n") + "`n};`n"
  [IO.File]::WriteAllText($snapFile, $body, [Text.UTF8Encoding]::new($false))
  "Instantané réécrit : $snapFile ($(@($lines).Count) cas)."
  & $PSCommandPath -Work $Work
  exit $LASTEXITCODE
}
"Rôles : $($r.ok) réussis, $(@($r.ko).Count) échecs"
@($r.ko) | ForEach-Object { "  ÉCHEC : $_" }
if (@($r.ko).Count) { exit 1 }
exit 0
