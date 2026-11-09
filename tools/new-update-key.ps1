# Crée la clé qui signe les mises à jour de DRAFT. À ne lancer qu'une fois : la clé privée reste sur ce PC, hors du dépôt.
# Sans elle, plus aucune mise à jour automatique n'est possible : garde-en une copie sur un support à part.
param([string]$KeyFile = (Join-Path $env:APPDATA 'DRAFT-signature\draft-update-private.xml'))
$ErrorActionPreference = 'Stop'
if (Test-Path $KeyFile) { throw "Une clé existe déjà : $KeyFile. Elle n'est pas remplacée." }
$dir = Split-Path $KeyFile -Parent
New-Item -ItemType Directory -Force $dir | Out-Null
& icacls $dir /inheritance:r /grant:r "$($env:USERNAME):(OI)(CI)F" | Out-Null
$rsa = [Security.Cryptography.RSA]::Create(3072)
[IO.File]::WriteAllText($KeyFile, $rsa.ToXmlString($true), [Text.UTF8Encoding]::new($false))
Write-Host "Clé privée : $KeyFile"
Write-Host 'Clé publique, à placer dans src/AppUpdater.cs :'
$rsa.ToXmlString($false)
