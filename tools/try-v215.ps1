# Lance la candidate V2.15 avec des données entièrement séparées. N'installe rien.
# Tes préférences, ton profil, ton journal, tes sauvegardes et le démarrage automatique de la V2.14 restent intacts.
# -Reset efface uniquement le dossier d'essai.
param([string]$Exe = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-v215\cand\DraftMid-Portable.exe'), [switch]$Reset)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Exe)) { throw "Candidate introuvable : $Exe" }
$data = Join-Path $env:LOCALAPPDATA 'DraftMid-essai-v215'
if ($Reset -and (Test-Path $data)) { Remove-Item -Recurse -Force $data }
if (@(Get-Process DraftMid -ErrorAction SilentlyContinue).Count) {
  Write-Warning "La V2.14 tourne aussi : ne lance pas de draft avec les deux, leurs panneaux se superposeraient. Le raccourci Ctrl+Alt+D reste à la V2.14."
}
Write-Host "Données d'essai : $data"
Start-Process $Exe -ArgumentList '--data', "`"$data`"", '--run-key', 'Software\DraftMid-Essai\Run'
