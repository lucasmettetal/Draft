# Banc des prototypes d'internationalisation (docs/i18n/TESTS.md). Ne touche ni à web/ ni à src/ : copie sous %TEMP%\draft-i18n-prep.
# 1. Vérifie les catalogues d'exemple (check-catalogs.ps1) et génère la pseudo-langue (pseudo-localize.ps1).
# 2. Ouvre le banc de la page (i18n-tests.html) dans Chrome sans écran, hors ligne, en fr, en et qps.
# 3. Compile le prototype natif (Strings.cs + src/DataScripts.cs) avec le csc de Windows, comme build.ps1, et compare
#    ses pluriels à ceux d'Intl.PluralRules relevés dans Chrome.
# N'arrête que les Chrome lancés avec son propre profil. Aucun réseau.
param([string]$Work = (Join-Path $env:TEMP 'draft-i18n-prep'))
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path (Split-Path $here -Parent) -Parent
$page = Join-Path $Work 'page'
New-Item -ItemType Directory -Force $page | Out-Null
$fails = 0

'--- Catalogues'
& (Join-Path $here 'check-catalogs.ps1') -Reference (Join-Path $here 'exemples\fr.js') -Target (Join-Path $here 'exemples\en.js') -AllowMissing -Quiet
if ($LASTEXITCODE) { $fails++ }
Copy-Item (Join-Path $here 'exemples\fr.js'), (Join-Path $here 'exemples\en.js'), (Join-Path $here 'i18n-runtime.js'), (Join-Path $here 'i18n-tests.js'), (Join-Path $here 'i18n-tests.html') $page -Force
& (Join-Path $here 'pseudo-localize.ps1') -Source (Join-Path $here 'exemples\fr.js') -Out (Join-Path $page 'qps.js') | Out-Null

'--- Page (Chrome sans écran)'
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
$table = Join-Path $Work 'pluriels-page.json'
if (-not $chrome) { 'Chrome introuvable : banc de la page sauté.'; $fails++ }
else {
  $prof = Join-Path $Work 'profil-chrome'
  foreach ($l in 'fr', 'en', 'qps') {
    $dom = Join-Path $Work "page-$l.dom"
    $url = 'file:///' + ((Join-Path $page 'i18n-tests.html') -replace '\\', '/') + "?lang=$l&i18ncheck=1"
    $a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', '--host-resolver-rules="MAP * ~NOTFOUND"', "--user-data-dir=`"$prof`"", '--virtual-time-budget=5000', '--dump-dom', "`"$url`"")
    $p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $dom -RedirectStandardError "$dom.err"
    [void]$p.WaitForExit(60000)
    Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    $m = [regex]::Match([IO.File]::ReadAllText($dom), '<pre id="out">([\s\S]*?)</pre>')
    if (-not $m.Groups[1].Value) { "ÉCHEC page $l : aucun résultat"; $fails++; continue }
    $r = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value) | ConvertFrom-Json
    foreach ($c in $r.checks) { if ($c.ok) { "ok    [$l] $($c.name)" } else { "ÉCHEC [$l] $($c.name)"; $fails++ } }
    if ($l -eq 'fr') { [IO.File]::WriteAllText($table, ($r.plural | ConvertTo-Json -Compress), [Text.UTF8Encoding]::new($false)) }
  }
}

'--- Programme (csc de Windows, comme build.ps1)'
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$exe = Join-Path $Work 'strings-prototype.exe'
$src = @((Join-Path $repo 'src\DataScripts.cs'), (Join-Path $here 'Strings.cs'), (Join-Path $here 'StringsHarness.cs'))
$out = & $csc /nologo /target:exe /utf8output /codepage:65001 "/out:$exe" /reference:System.Web.Extensions.dll $src 2>&1
if ($LASTEXITCODE) { $out; 'ÉCHEC compilation'; $fails++ }
else {
  $old = [Console]::OutputEncoding; [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
  & $exe (Join-Path $here 'exemples\fr.js') (Join-Path $here 'exemples\en.js') $table
  if ($LASTEXITCODE) { $fails++ }
  [Console]::OutputEncoding = $old
}
if ($fails) { "Banc i18n : $fails échec(s)."; exit 1 } else { 'Banc i18n : tout est correct.'; exit 0 }
