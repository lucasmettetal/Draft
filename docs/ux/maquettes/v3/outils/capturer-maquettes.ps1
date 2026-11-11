param([string]$Only = "")
# Captures des maquettes en Chrome headless. Profil dédié, réseau coupé, aucun autre processus touché.
$ErrorActionPreference = "Stop"
$chrome = "C:\Program Files\Google\Chrome\Application\chrome.exe"
$src = Split-Path $PSScriptRoot -Parent
$out = Join-Path $src "captures"
$prof = Join-Path $env:TEMP "draft-ux-v3-chrome"
New-Item -ItemType Directory -Force $out | Out-Null
New-Item -ItemType Directory -Force $prof | Out-Null

# nom de page, ancre, largeur, hauteur, échelle, nom de sortie
$jobs = @()
foreach ($p in @("accueil", "accueil-draft-en-cours", "accueil-premier-lancement", "mon-pool", "simulateur", "statistiques", "reglages")) {
  $jobs += ,@($p, "", 1366, 768, 1, "$p-1366x768")
  $jobs += ,@($p, "", 1920, 1080, 1, "$p-1920x1080")
  $jobs += ,@($p, "", 2560, 1440, 1, "$p-2560x1440")
  # 1920 x 1080 à 175 % : fenêtre principale ouverte par OpenFull, 1840 x 1035 physiques environ.
  $jobs += ,@($p, "", 1840, 1035, 1.75, "$p-1920x1080-175pc")
  # 1366 x 768 à 125 % : 1286 x 728 physiques, soit environ 1029 x 582 logiques.
  $jobs += ,@($p, "", 1286, 728, 1.25, "$p-1366x768-125pc")
}
foreach ($a in @("connexion", "affichage", "donnees", "mises-a-jour", "diagnostic")) {
  $jobs += ,@("reglages", "#$a", 1366, 768, 1, "reglages-$a-1366x768")
  $jobs += ,@("reglages", "#$a", 1840, 1035, 1.75, "reglages-$a-1920x1080-175pc")
}
# Overlay à sa taille réelle : 340 px de page par panneau, 374 px en panneau unique, hauteur du client 1280 x 720.
$jobs += ,@("overlay-deux-panneaux", "", 704, 748, 1, "overlay-deux-panneaux-340x720")
$jobs += ,@("overlay-deux-panneaux", "", 880, 935, 1.25, "overlay-deux-panneaux-125pc")
$jobs += ,@("overlay-panneau-unique", "", 374, 748, 1, "overlay-panneau-unique-374x720")
$jobs += ,@("overlay-panneau-unique", "", 655, 1309, 1.75, "overlay-panneau-unique-175pc")
$jobs += ,@("overlay-etats", "", 1150, 700, 1, "overlay-etats")
$jobs += ,@("menu-icone", "", 660, 560, 1, "menu-icone")
$jobs += ,@("dialogue-cle-riot", "", 580, 740, 1, "dialogue-cle-riot")
$jobs += ,@("composants", "", 1366, 1580, 1, "composants")

foreach ($j in $jobs) {
  if ($Only -and $j[5] -notlike "*$Only*") { continue }
  $url = "file:///" + ($src -replace "\\", "/") + "/" + $j[0] + ".html" + $j[1]
  $png = Join-Path $out ($j[5] + ".png")
  $cargs = @("--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run", "--no-default-browser-check",
    "--disable-background-networking", "--disable-component-update", "--disable-sync", "--disable-extensions",
    "`"--host-resolver-rules=MAP * ~NOTFOUND`"", "--user-data-dir=$prof",
    "--force-device-scale-factor=$($j[4])", "--window-size=$([int]($j[2] / $j[4])),$([int]($j[3] / $j[4]))",
    "--virtual-time-budget=1500", "--screenshot=$png", $url)
  $p = Start-Process -FilePath $chrome -ArgumentList $cargs -PassThru -WindowStyle Hidden
  if (-not $p.WaitForExit(30000)) { $p.Kill() }
  Write-Output ("{0} {1}" -f $j[5], (Test-Path $png))
}
# Ne reste aucun Chrome lancé avec ce profil.
Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
