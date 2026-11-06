# Vérifie qu'une candidate retrouve les données d'un dossier existant, en travaillant UNIQUEMENT sur une copie.
# L'original n'est jamais ouvert en écriture. Le rapport ne contient que des nombres, sans nom de champion ni de joueur.
param(
  [Parameter(Mandatory = $true)][string]$Source,
  [string]$Exe = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-beta\cand\DRAFT-Portable.exe'),
  [string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-beta\migration')
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Source)) { throw "Dossier introuvable : $Source" }
$name = Split-Path $Source -Leaf; $copy = Join-Path $Work $name
Remove-Item -Recurse -Force $copy -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $copy | Out-Null
$before = @{}; Get-ChildItem $Source -File | ForEach-Object { $before[$_.Name] = (Get-FileHash $_.FullName).Hash }
Get-ChildItem $Source -File | Where-Object { $_.Name -notlike 'diagnostic*' -and $_.Name -notlike '*.log' } | Copy-Item -Destination $copy
$storage = Get-ChildItem (Join-Path $Source 'webview') -Recurse -Directory -Filter 'Local Storage' -ErrorAction SilentlyContinue | Select-Object -First 1
$skipped = 0
if ($storage) {
  $target = Join-Path $copy ($storage.FullName.Substring($Source.Length + 1))
  New-Item -ItemType Directory -Force $target | Out-Null
  # Un fichier verrouillé par une version ouverte est sauté et compté : l'original n'est pas forcé.
  Get-ChildItem $storage.FullName -Recurse -File | ForEach-Object {
    $dest = Join-Path $target ($_.FullName.Substring($storage.FullName.Length + 1)); New-Item -ItemType Directory -Force (Split-Path $dest) | Out-Null
    try { [IO.File]::Copy($_.FullName, $dest, $true) } catch { $skipped++ }
  }
}
$rows = 0; $history = Join-Path $copy 'draft-history.js'
if (Test-Path $history) { $t = [IO.File]::ReadAllText($history); $rows = ([regex]::Matches($t, '"id":"')).Count }
$report = Join-Path $Work "$name-rapport.json"; Remove-Item $report -ErrorAction SilentlyContinue
$p = Start-Process $Exe -ArgumentList '--data', "`"$copy`"", '--run-key', 'Software\DRAFT-Migration-Test\Run', '--no-hotkey', '--no-auto-panels', '--report-state', "`"$report`"" -PassThru
if (-not $p.WaitForExit(90000)) { Stop-Process -Id $p.Id -Force; throw 'Délai dépassé.' }
Remove-Item 'HKCU:\Software\DRAFT-Migration-Test' -Recurse -ErrorAction SilentlyContinue
$changed = @(Get-ChildItem $Source -File | Where-Object { $before.ContainsKey($_.Name) -and $before[$_.Name] -ne (Get-FileHash $_.FullName).Hash -and $_.Name -notlike 'diagnostic*' -and $_.Name -notlike '*.log' })
"Source : $Source"
"Fichiers du stockage sautés car verrouillés : $skipped"
"Parties dans la copie : $rows"
"Lu par la candidate : " + $(if (Test-Path $report) { Get-Content $report -Raw } else { 'aucun rapport' })
"Original modifié pendant le test : " + $(if ($changed.Count) { $changed.Name -join ', ' } else { 'non' })
