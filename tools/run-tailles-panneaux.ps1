# Mesures et captures de la proposition de tailles : web/draft-tailles-tests.html dans Chrome sans interface, à 125 %.
# Exemple : tools\run-tailles-panneaux.ps1 -Root . -Work $env:TEMP\tailles -Shots docs\qa\captures-tailles-panneaux
# Copie de web/ avec 'unsafe-eval' dans la CSP de la copie seulement (comme run-v215-tests.ps1). Rien n'est changé dans l'application.
param([Parameter(Mandatory = $true)][string]$Root, [Parameter(Mandatory = $true)][string]$Work, [string]$Shots = '')
$ErrorActionPreference = 'Stop'
$chrome = "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
$web = Join-Path $Work 'web'; Remove-Item -Recurse -Force $web -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force $Work | Out-Null
Copy-Item (Join-Path $Root 'web') $web -Recurse
$idx = Join-Path $web 'index.html'
[IO.File]::WriteAllText($idx, [IO.File]::ReadAllText($idx).Replace("script-src 'self' 'unsafe-inline'", "script-src 'self' 'unsafe-inline' 'unsafe-eval'"), [Text.UTF8Encoding]::new($false))
$base = 'file:///' + ((Join-Path $web 'draft-tailles-tests.html') -replace '\\', '/')
# Cas : nom, panneau, largeur et hauteur utiles (px CSS), état, langue.
$cases = @(
  @('1366-125-actuel', 'compact', 222, 511), @('1366-125-propose', 'compact', 264, 565, 600),
  @('1920-125-actuel', 'right', 252, 567), @('1920-125-propose', 'right', 252, 631, 600))
$res = @()
foreach ($c in $cases) {
  foreach ($st in 'draft', 'consult') {
    foreach ($lang in 'fr', 'en') {
      $url = "${base}?panel=$($c[1])&w=$($c[2])&h=$($c[3])&state=$st&lang=$lang&seuil=$($c[4])"
      $pf = Join-Path $Work ("pf-" + $c[0] + "-$st-$lang")
      $dom = & $chrome --headless=new --disable-gpu --no-first-run --allow-file-access-from-files "--user-data-dir=$pf" --window-size=600,1000 --force-device-scale-factor=1.25 --virtual-time-budget=20000 --dump-dom $url 2>$null | Out-String
      $j = [Net.WebUtility]::HtmlDecode([regex]::Match($dom, '<pre id="out">([\s\S]*?)</pre>').Groups[1].Value)
      $res += "$($c[0]) $st $lang : $j"
    }
  }
  if ($Shots) {
    New-Item -ItemType Directory -Force $Shots | Out-Null
    $png = Join-Path $Shots ("panneau-" + $c[0] + "-fr.png")
    $pf = Join-Path $Work ("pf-shot-" + $c[0])
    & $chrome --headless=new --disable-gpu --no-first-run --hide-scrollbars --allow-file-access-from-files "--user-data-dir=$pf" "--window-size=$($c[2]),$($c[3])" --force-device-scale-factor=1.25 --virtual-time-budget=20000 "--screenshot=$png" "${base}?panel=$($c[1])&w=$($c[2])&h=$($c[3])&state=consult&lang=fr&seuil=$($c[4])" 2>$null | Out-Null
    $res += "capture $png : $((Get-Item $png -ErrorAction SilentlyContinue).Length) octets"
  }
}
$res
