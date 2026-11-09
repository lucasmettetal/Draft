# Lance la candidate DRAFT à côté des versions existantes, sans les remplacer. N'installe rien, ne lance aucun import.
#
# Données : %LOCALAPPDATA%\DRAFT-essai. Au premier lancement, une COPIE des données de l'essai V2.15
# (profil, historique Riot, réglages) y est faite. L'original n'est jamais modifié.
# Raccourci : jamais enregistré par l'essai. Panneaux automatiques : coupés si une autre version tourne.
#
# -Reset efface uniquement le dossier d'essai DRAFT. -SansCopie démarre vide. -Simulation montre ce qui serait fait.
param(
  [string]$Exe = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-beta\cand\DRAFT-Portable.exe'),
  [string]$DataDir = (Join-Path $env:LOCALAPPDATA 'DRAFT-essai'),
  [string]$Source = (Join-Path $env:LOCALAPPDATA 'DraftMid-essai-v215'),
  [string]$RunKey = 'Software\DRAFT-Essai\Run',
  [switch]$Reset,
  [switch]$SansCopie,
  [switch]$Simulation,
  [int]$AutoExit = 0 # Réservé aux tests.
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Exe)) { throw "Candidate introuvable : $Exe" }
$Exe = (Resolve-Path $Exe).Path
if (Get-Process 'League of Legends' -ErrorAction SilentlyContinue) { throw 'Une partie est en cours : essai refusé. Relance après la partie.' }
$running = @(Get-Process DraftMid, DraftMid-Portable, DRAFT, DRAFT-Portable -ErrorAction SilentlyContinue)
if ($running | Where-Object { $_.Path -eq $Exe }) { throw "La candidate tourne déjà : utilise son icône près de l'horloge." }
$others = @($running | Where-Object { $_.Path -ne $Exe })

if ($Reset -and -not $Simulation -and (Test-Path $DataDir)) { Remove-Item -Recurse -Force $DataDir }
$seed = -not $SansCopie -and -not (Test-Path $DataDir) -and (Test-Path $Source)
if ($seed) {
  # Une copie cohérente demande que la V2.15 d'essai soit fermée : son stockage est verrouillé tant qu'elle tourne.
  # Windows ne donne pas toujours le chemin d'un exécutable lancé ailleurs : toute candidate DraftMid ouverte bloque la copie.
  if ($running | Where-Object { $_.ProcessName -eq 'DraftMid-Portable' -or $_.Path -like '*dist-v215*' }) { throw "Ferme d'abord la V2.15 d'essai (icône près de l'horloge, Quitter), pour copier ses données sans risque. Ou relance avec -SansCopie." }
  Write-Host "Copie des données de l'essai V2.15 : $Source"
  if (-not $Simulation) {
    $before = @{}; Get-ChildItem $Source -File | ForEach-Object { $before[$_.Name] = (Get-FileHash $_.FullName).Hash }
    New-Item -ItemType Directory -Force $DataDir | Out-Null
    Get-ChildItem $Source -File | Where-Object { $_.Name -notlike 'diagnostic*' -and $_.Name -notlike '*.log' } | Copy-Item -Destination $DataDir
    foreach ($folder in 'sauvegardes-historique') { if (Test-Path (Join-Path $Source $folder)) { Copy-Item (Join-Path $Source $folder) $DataDir -Recurse } }
    $storage = Get-ChildItem (Join-Path $Source 'webview') -Recurse -Directory -Filter 'Local Storage' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($storage) {
      $target = Join-Path $DataDir ($storage.FullName.Substring($Source.Length + 1))
      New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
      Copy-Item $storage.FullName $target -Recurse
    }
    $changed = @(Get-ChildItem $Source -File | Where-Object { $before[$_.Name] -ne (Get-FileHash $_.FullName).Hash })
    if ($changed.Count) { throw "L'original a changé pendant la copie : $($changed.Name -join ', '). Rien n'est lancé." }
    Write-Host "Copie faite. L'essai V2.15 reste intact."
  }
}

$arguments = @('--data', "`"$DataDir`"", '--run-key', $RunKey, '--no-hotkey')
if ($others.Count) {
  $arguments += '--no-auto-panels'
  Write-Warning "Une autre version tourne ($($others[0].Path)). Elle garde Ctrl+Alt+D et ses panneaux."
  Write-Warning "Les panneaux automatiques de l'essai sont coupés. Affichés pendant une draft, ils peuvent se superposer à ceux de l'autre version."
}
if ($AutoExit) { $arguments += @('--auto-exit', "$AutoExit", '--out', "`"$DataDir-sortie.txt`"") }
Write-Host "Candidate : $Exe"
Write-Host "Données d'essai : $DataDir"
Write-Host "Arguments : $($arguments -join ' ')"
if ($Simulation) { return }
$process = Start-Process $Exe -ArgumentList $arguments -PassThru
if ($AutoExit) { [void]$process.WaitForExit(($AutoExit + 60) * 1000); Write-Host "Code : $($process.ExitCode)" }
else { Write-Host "Lancé. Clic droit sur la nouvelle icône DRAFT près de l'horloge pour ouvrir les réglages et les panneaux. Aucun import n'est lancé sans ton action." }
