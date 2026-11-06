# Installation locale d'une version compilée, sans publication. Ne fait rien si DRAFT, DraftMid ou le client LoL sont ouverts,
# sauf -FermerDraftMid, qui ne s'applique jamais pendant que le client est ouvert.
# Sauvegarde d'abord l'exécutable et les données ; tools/restore-local.ps1 revient en arrière.
param(
  [Parameter(Mandatory = $true)][string]$Source,
  [string]$Destination = (Join-Path ([Environment]::GetFolderPath('Desktop')) 'DraftMid.exe'),
  [string]$DataDir = (Join-Path $env:LOCALAPPDATA 'DraftMid'),
  [string]$BackupRoot = (Join-Path $env:LOCALAPPDATA 'DraftMid-sauvegardes'),
  [switch]$FermerDraftMid,
  [switch]$SansLancer,
  [switch]$IgnorerClient # Réservé aux tests sur des dossiers isolés.
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Source)) { throw "Version introuvable : $Source" }
if (-not $IgnorerClient -and (Get-Process LeagueClient, LeagueClientUx, 'League of Legends' -ErrorAction SilentlyContinue)) { throw 'Le client LoL est ouvert : rien n''est modifié. Relance quand il est fermé.' }
$running = @(Get-Process DraftMid, DRAFT -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $Destination })
if ($running.Count -and -not $FermerDraftMid) { throw 'DRAFT (ou DraftMid) est ouvert : rien n''est modifié. Ferme-le, ou utilise -FermerDraftMid.' }

$running | ForEach-Object { [void]$_.CloseMainWindow(); $_ | Stop-Process -Force -ErrorAction SilentlyContinue }
if ($running.Count) { Start-Sleep -Seconds 2 }

$oldVersion = if (Test-Path $Destination) { (Get-Item $Destination).VersionInfo.FileVersion } else { 'aucune' }
$backup = Join-Path $BackupRoot ('avant-installation-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force (Join-Path $backup 'donnees') | Out-Null
if (Test-Path $Destination) { Copy-Item $Destination (Join-Path $backup 'DraftMid.exe') }
if (Test-Path $DataDir) {
  Get-ChildItem $DataDir -File | Where-Object { $_.Name -notlike 'diagnostic*' } | Copy-Item -Destination (Join-Path $backup 'donnees')
  $storage = Get-ChildItem (Join-Path $DataDir 'webview') -Recurse -Directory -Filter 'Local Storage' -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($storage) {
    Copy-Item $storage.FullName (Join-Path $backup 'donnees\Local Storage') -Recurse
    [IO.File]::WriteAllText((Join-Path $backup 'chemin-local-storage.txt'), $storage.FullName)
  }
}
[IO.File]::WriteAllText((Join-Path $backup 'infos.txt'), "Version remplacée : $oldVersion`r`nDestination : $Destination`r`nDonnées : $DataDir`r`n")
if ((Test-Path $Destination) -and (Get-FileHash $Destination).Hash -ne (Get-FileHash (Join-Path $backup 'DraftMid.exe')).Hash) { throw 'Sauvegarde invalide : rien n''est modifié.' }

Copy-Item $Source $Destination -Force
if ((Get-FileHash $Source).Hash -ne (Get-FileHash $Destination).Hash) { throw "Copie invalide. Sauvegarde : $backup" }
if (-not $SansLancer) { Start-Process $Destination }
"Installé : $oldVersion -> $((Get-Item $Destination).VersionInfo.FileVersion)"
"Sauvegarde : $backup"
"Retour arrière : tools\restore-local.ps1 -Sauvegarde `"$backup`""
