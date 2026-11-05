# Lance la candidate V2.15 à côté de la V2.14, sans la remplacer. N'installe rien, et ne lance aucun import.
#
# Données : %LOCALAPPDATA%\DraftMid-essai-v215 (profil, préférences, journal et historique séparés).
# Raccourci : jamais enregistré par cet essai, pour rester à la V2.14. Les panneaux s'ouvrent par le menu de l'icône.
# Panneaux automatiques : coupés si une autre version de DraftMid tourne, pour éviter leur superposition.
#
# -Reset efface uniquement le dossier d'essai. -Simulation affiche ce qui serait lancé, sans rien lancer.
param(
  [string]$Exe = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-v215\cand\DraftMid-Portable.exe'),
  [string]$DataDir = (Join-Path $env:LOCALAPPDATA 'DraftMid-essai-v215'),
  [string]$RunKey = 'Software\DraftMid-Essai\Run',
  [switch]$Reset,
  [switch]$Simulation,
  [int]$AutoExit = 0 # Réservé aux tests.
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Exe)) { throw "Candidate introuvable : $Exe" }
$Exe = (Resolve-Path $Exe).Path
if (Get-Process 'League of Legends' -ErrorAction SilentlyContinue) { throw "Une partie est en cours : essai refusé. Relance après la partie." }
$running = @(Get-Process DraftMid, DraftMid-Portable -ErrorAction SilentlyContinue)
if ($running | Where-Object { $_.Path -eq $Exe }) { throw "La candidate tourne déjà : utilise son icône près de l'horloge." }
$others = @($running | Where-Object { $_.Path -ne $Exe })
$arguments = @('--data', "`"$DataDir`"", '--run-key', $RunKey, '--no-hotkey')
if ($others.Count) {
  $arguments += '--no-auto-panels'
  Write-Warning "Une autre version de DraftMid tourne ($($others[0].Path)). Elle garde Ctrl+Alt+D et ses panneaux."
  Write-Warning "Les panneaux automatiques de l'essai sont coupés. Si tu les affiches pendant une draft, ils peuvent se superposer à ceux de l'autre version."
}
if ($Reset -and -not $Simulation -and (Test-Path $DataDir)) { Remove-Item -Recurse -Force $DataDir }
if ($AutoExit) { $arguments += @('--auto-exit', "$AutoExit", '--out', "`"$DataDir-sortie.txt`"") }
Write-Host "Candidate : $Exe"
Write-Host "Données d'essai : $DataDir"
Write-Host "Arguments : $($arguments -join ' ')"
if ($Simulation) { return }
$process = Start-Process $Exe -ArgumentList $arguments -PassThru
if ($AutoExit) { [void]$process.WaitForExit(($AutoExit + 60) * 1000); Write-Host "Code : $($process.ExitCode)" }
else { Write-Host "Lancé. Clic droit sur la nouvelle icône DraftMid près de l'horloge pour ouvrir les réglages et les panneaux. Aucun import n'est lancé sans ton action." }
