# Arrête uniquement le DRAFT lancé par lancer-audit.ps1 (pid enregistré, exe du dossier de travail).
param([string]$Work = (Join-Path $env:TEMP 'draft-ux'))
$pidFile = Join-Path $Work 'pid.txt'
if (-not (Test-Path $pidFile)) { 'Aucun pid enregistré.'; return }
$id = [int](Get-Content $pidFile)
$p = Get-Process -Id $id -ErrorAction SilentlyContinue
if ($p -and $p.Path -like (Join-Path $Work 'build\*')) { Stop-Process -Id $id -Force; "Arrêté : $id" } else { "Rien à arrêter (pid $id absent ou étranger au dossier de travail)." }
