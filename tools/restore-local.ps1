# Revient à la version et aux données d'une sauvegarde faite par install-local.ps1. Ferme d'abord DraftMid.
param(
  [Parameter(Mandatory = $true)][string]$Sauvegarde,
  [string]$Destination = (Join-Path ([Environment]::GetFolderPath('Desktop')) 'DraftMid.exe'),
  [string]$DataDir = (Join-Path $env:LOCALAPPDATA 'DraftMid'),
  [switch]$SansLancer,
  [switch]$IgnorerClient # Réservé aux tests sur des dossiers isolés.
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path (Join-Path $Sauvegarde 'infos.txt'))) { throw "Ce dossier n'est pas une sauvegarde : $Sauvegarde" }
if (-not $IgnorerClient -and (Get-Process LeagueClient, LeagueClientUx, 'League of Legends' -ErrorAction SilentlyContinue)) { throw 'Le client LoL est ouvert : rien n''est modifié.' }
if (Get-Process DraftMid -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $Destination }) { throw 'DraftMid est ouvert : ferme-le d''abord.' }

$exe = Join-Path $Sauvegarde 'DraftMid.exe'
if (Test-Path $exe) { Copy-Item $exe $Destination -Force }
$saved = Join-Path $Sauvegarde 'donnees'
New-Item -ItemType Directory -Force $DataDir | Out-Null
Get-ChildItem $saved -File | Copy-Item -Destination $DataDir -Force
# Les fichiers ajoutés depuis restent : ce sont des données de calcul, jamais lues par une version plus ancienne.
$pathFile = Join-Path $Sauvegarde 'chemin-local-storage.txt'
if ((Test-Path $pathFile) -and (Test-Path (Join-Path $saved 'Local Storage'))) {
  $target = (Get-Content $pathFile -Raw).Trim()
  if (Test-Path $target) { Remove-Item $target -Recurse -Force }
  New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
  Copy-Item (Join-Path $saved 'Local Storage') $target -Recurse
}
if (-not $SansLancer) { Start-Process $Destination }
"Restauré : $((Get-Item $Destination).VersionInfo.FileVersion)"
