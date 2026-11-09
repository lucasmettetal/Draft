# Essai complet de la mise à jour, dans des dossiers isolés (dist-beta\maj-*). Ne touche ni à une version installée, ni aux données.
# Une vraie 2.16.0 se met à jour vers une version plus récente construite ici, puis redémarre. Enfin, lecture réelle de la dernière release GitHub.
param([string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-beta\maj'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
$fails = 0
function Check($ok, $text) { if ($ok) { "OK : $text" } else { "ECHEC : $text"; $script:fails++ } }
function Version($file) { (Get-Item $file).VersionInfo.FileVersion }
function Build($dir, $version, $inject = '') {
  $src = Join-Path $Work "src-$version"
  New-Item -ItemType Directory -Force $src | Out-Null
  foreach ($item in 'src', 'web', 'lib', 'build.ps1') { Copy-Item (Join-Path $repo $item) $src -Recurse }
  $cs = Join-Path $src 'src\DraftMid.cs'
  $text = [IO.File]::ReadAllText($cs).Replace('AssemblyVersion("2.16.0.0")', "AssemblyVersion(`"$version.0`")").Replace('AssemblyFileVersion("2.16.0.0")', "AssemblyFileVersion(`"$version.0`")")
  if ($inject) { $text = $text.Replace('string webDir = Extract(dataDir);', "$inject string webDir = Extract(dataDir);") }
  [IO.File]::WriteAllText($cs, $text, [Text.UTF8Encoding]::new($false))
  & (Join-Path $src 'build.ps1') -Out $dir | Out-Null
  Remove-Item -Recurse -Force $src
}
function Scenario($name, $source) {
  $dir = Join-Path $Work $name
  New-Item -ItemType Directory -Force "$dir\app" | Out-Null
  Copy-Item (Join-Path $Work 'a\DRAFT-Portable.exe') "$dir\app\DRAFT-Portable.exe"
  $state = "$dir\etat.json"; $out = "$dir\resultat.txt"
  $p = Start-Process "$dir\app\DRAFT-Portable.exe" -ArgumentList '--data', "`"$dir\data`"", '--no-hotkey', '--no-auto-panels', '--run-key', 'Software\DRAFT-MajTest\Run', '--test-update', "`"$source`"", '--report-state', "`"$state`"", '--out', "`"$out`"" -PassThru
  $clock = [Diagnostics.Stopwatch]::StartNew()
  while ($clock.Elapsed.TotalSeconds -lt 150 -and -not ((Test-Path $state) -or (Test-Path $out))) { Start-Sleep -Milliseconds 500 }
  [void]$p.WaitForExit(30000)
  Get-CimInstance Win32_Process -Filter "Name='DRAFT-Portable.exe'" | Where-Object { $_.CommandLine -like "*$dir*" } | ForEach-Object { [void](Get-Process -Id $_.ProcessId -ErrorAction SilentlyContinue | Wait-Process -Timeout 20 -ErrorAction SilentlyContinue) }
  Start-Sleep -Seconds 1
  return $dir
}

Build (Join-Path $Work 'a') '2.16.0'
Build (Join-Path $Work 'b') '2.16.1'
Build (Join-Path $Work 'c') '2.15.9'
Check ((Version "$Work\b\DRAFT-Portable.exe") -eq '2.16.1.0') 'version 2.16.1 construite'

$dir = Scenario 'plus-recente' "$Work\b\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq '2.16.1.0') 'plus récente : exécutable remplacé en place'
Check (Test-Path "$dir\etat.json") 'plus récente : la nouvelle version a redémarré et lu ses données'
Check (-not (Test-Path "$dir\app\DRAFT-Portable.exe.ancien")) 'plus récente : ancienne version supprimée après le redémarrage'
Check (-not (Test-Path "$dir\app\DRAFT-Portable.maj.exe")) 'plus récente : aucun fichier temporaire restant'
Check (-not (Test-Path "$dir\data\maj-test")) 'plus récente : dossier de test supprimé'

$dir = Scenario 'identique' "$Work\a\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq '2.16.0.0') 'même version : rien de remplacé'
Check ((Get-Content "$dir\resultat.txt" -Raw -Encoding utf8) -match 'à jour') 'même version : annonce à jour'

$dir = Scenario 'plus-ancienne' "$Work\c\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq '2.16.0.0') 'version plus ancienne : aucun retour en arrière'

Build (Join-Path $Work 'plante') '2.16.2' 'if (o.AfterUpdate > 0) Environment.Exit(3);'
$dir = Scenario 'plantage' "$Work\plante\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq '2.16.0.0') 'plantage à l''ouverture : ancienne version remise en place'
Check (Test-Path "$dir\etat.json") 'plantage à l''ouverture : ancienne version relancée'
Check (-not (Test-Path "$dir\app\DRAFT-Portable.exe.ancien")) 'plantage à l''ouverture : aucun reste'

Build (Join-Path $Work 'fige') '2.16.3' 'if (o.AfterUpdate > 0) Thread.Sleep(Timeout.Infinite);'
$dir = Scenario 'blocage' "$Work\fige\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq '2.16.0.0') 'blocage à l''ouverture : ancienne version remise en place'
Check (Test-Path "$dir\etat.json") 'blocage à l''ouverture : ancienne version relancée'

# Clé réelle : le fichier signé par l'outil est accepté par la clé publique embarquée, et seulement pour son tag.
$key = Join-Path $env:APPDATA 'DRAFT-signature\draft-update-private.xml'
if (Test-Path $key) {
  $exe = Join-Path $Work 'a\DRAFT-Portable.exe'; $sig = Join-Path $Work 'essai.sig'
  & (Join-Path $PSScriptRoot 'sign-release.ps1') -Tag 'v9.9.9' -File $exe -Out $sig | Out-Null
  $good = Start-Process $exe -ArgumentList '--test-signature', "`"$exe`"", 'v9.9.9', "`"$sig`"", '--data', "`"$Work\sig-data`"" -PassThru -Wait
  Check ($good.ExitCode -eq 0) 'signature réelle : acceptée par la clé embarquée'
  $bad = Start-Process $exe -ArgumentList '--test-signature', "`"$exe`"", 'v9.9.8', "`"$sig`"", '--data', "`"$Work\sig-data`"" -PassThru -Wait
  Check ($bad.ExitCode -ne 0) 'signature réelle : refusée pour un autre tag'
} else { 'NON TESTE : clé privée absente de ce PC.' }

$live = Join-Path $Work 'github'
New-Item -ItemType Directory -Force $live | Out-Null
$p = Start-Process (Join-Path $Work 'a\DRAFT-Portable.exe') -ArgumentList '--test-update-live', '--data', "`"$live\data`"", '--out', "`"$live\resultat.txt`"" -PassThru
[void]$p.WaitForExit(90000)
$text = Get-Content "$live\resultat.txt" -Raw -Encoding utf8
"GitHub : $text"
Check ($p.ExitCode -eq 0 -and $text -match '^OK v\d') 'GitHub : dernière release lue, exécutable téléchargé et vérifié'
Check (-not (Test-Path "$live\data\essai-maj.exe")) 'GitHub : fichier d''essai supprimé'
Check ($null -eq (Get-ItemProperty 'HKCU:\Software\DRAFT-MajTest\Run' -ErrorAction SilentlyContinue)) 'aucun démarrage automatique créé'
"Echecs : $fails"
exit $fails
