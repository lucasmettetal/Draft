# Essai complet de la mise à jour, dans des dossiers isolés (dist-beta\maj-*). Ne touche ni à une version installée, ni aux données.
# La version de départ est soit construite depuis ces sources, soit un exécutable donné par -Base (par exemple la release 2.16.0 officielle),
# ce qui teste la logique de mise à jour réellement distribuée. Les candidats sont construits ici, en versions plus récentes.
# La source locale (--test-update) remplace GitHub et signe avec une clé jetable : les refus de signature, d'empreinte et de version
# sont couverts par les tests natifs (--selftest) de chaque version, et la clé réelle est vérifiée à part ci-dessous.
param(
  [string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-beta\maj'),
  [string]$Base
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
$fails = 0
function Check($ok, $text) { if ($ok) { "OK : $text" } else { "ECHEC : $text"; $script:fails++ } }
function Version($file) { (Get-Item $file).VersionInfo.FileVersion }
$cs0 = [IO.File]::ReadAllText((Join-Path $repo 'src\DraftMid.cs'))
$current = [regex]::Match($cs0, 'AssemblyFileVersion\("(\d+\.\d+\.\d+)\.0"\)').Groups[1].Value
if (-not $current) { throw 'Version introuvable dans src\DraftMid.cs.' }
function Bump($v, $n) { $p = $v.Split('.'); "$($p[0]).$($p[1]).$([int]$p[2] + $n)" }
function Build($dir, $version, $inject = '') {
  $src = Join-Path $Work "src-$version"
  New-Item -ItemType Directory -Force $src | Out-Null
  foreach ($item in 'src', 'web', 'lib', 'build.ps1') { Copy-Item (Join-Path $repo $item) $src -Recurse }
  $cs = Join-Path $src 'src\DraftMid.cs'
  $text = [IO.File]::ReadAllText($cs).Replace("AssemblyVersion(`"$current.0`")", "AssemblyVersion(`"$version.0`")").Replace("AssemblyFileVersion(`"$current.0`")", "AssemblyFileVersion(`"$version.0`")")
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
  $p = Start-Process "$dir\app\DRAFT-Portable.exe" -ArgumentList '--data', "`"$dir\data`"", '--no-hotkey', '--no-auto-panels', '--run-key', 'Software\DRAFT-MajTest\Run', '--lang', 'fr', '--test-update', "`"$source`"", '--report-state', "`"$state`"", '--out', "`"$out`"" -PassThru
  $clock = [Diagnostics.Stopwatch]::StartNew()
  while ($clock.Elapsed.TotalSeconds -lt 150 -and -not ((Test-Path $state) -or (Test-Path $out))) { Start-Sleep -Milliseconds 500 }
  [void]$p.WaitForExit(30000)
  Get-CimInstance Win32_Process -Filter "Name='DRAFT-Portable.exe'" | Where-Object { $_.CommandLine -like "*$dir*" } | ForEach-Object { [void](Get-Process -Id $_.ProcessId -ErrorAction SilentlyContinue | Wait-Process -Timeout 20 -ErrorAction SilentlyContinue) }
  Start-Sleep -Seconds 1
  return $dir
}

# Version de départ.
if ($Base) {
  New-Item -ItemType Directory -Force (Join-Path $Work 'a') | Out-Null
  Copy-Item $Base (Join-Path $Work 'a\DRAFT-Portable.exe')
  $newer = $current
  "Départ : $Base ($(Version $Base)), candidate : sources en $newer"
} else {
  Build (Join-Path $Work 'a') $current
  $newer = Bump $current 1
  "Départ : sources en $current, candidate : $newer"
}
$start = Version (Join-Path $Work 'a\DRAFT-Portable.exe')
Build (Join-Path $Work 'b') $newer
Build (Join-Path $Work 'c') '2.15.9'
Check ((Version "$Work\b\DRAFT-Portable.exe") -eq "$newer.0" -and [version]$newer -gt [version]$start) "candidate $newer construite, plus récente que $start"

$dir = Scenario 'plus-recente' "$Work\b\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq "$newer.0") 'plus récente : exécutable remplacé en place'
Check (Test-Path "$dir\etat.json") 'plus récente : la nouvelle version a redémarré et lu ses données'
Check (-not (Test-Path "$dir\app\DRAFT-Portable.exe.ancien")) 'plus récente : ancienne version supprimée après le redémarrage'
Check (-not (Test-Path "$dir\app\DRAFT-Portable.maj.exe")) 'plus récente : aucun fichier temporaire restant'
Check (-not (Test-Path "$dir\data\maj-test")) 'plus récente : dossier de test supprimé'

$dir = Scenario 'identique' "$Work\a\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq $start) 'même version : rien de remplacé'
Check ((Get-Content "$dir\resultat.txt" -Raw -Encoding utf8) -match 'à jour') 'même version : annonce à jour'

$dir = Scenario 'plus-ancienne' "$Work\c\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq $start) 'version plus ancienne : aucun retour en arrière'

Build (Join-Path $Work 'plante') (Bump $newer 1) 'if (o.AfterUpdate > 0) Environment.Exit(3);'
$dir = Scenario 'plantage' "$Work\plante\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq $start) 'plantage à l''ouverture : ancienne version remise en place'
Check (Test-Path "$dir\etat.json") 'plantage à l''ouverture : ancienne version relancée'
Check (-not (Test-Path "$dir\app\DRAFT-Portable.exe.ancien")) 'plantage à l''ouverture : aucun reste'

Build (Join-Path $Work 'fige') (Bump $newer 2) 'if (o.AfterUpdate > 0) Thread.Sleep(Timeout.Infinite);'
$dir = Scenario 'blocage' "$Work\fige\DRAFT-Portable.exe"
Check ((Version "$dir\app\DRAFT-Portable.exe") -eq $start) 'blocage à l''ouverture : ancienne version remise en place'
Check (Test-Path "$dir\etat.json") 'blocage à l''ouverture : ancienne version relancée'

# Clé réelle : un fichier signé par l'outil est accepté par la clé publique embarquée dans la version de départ ET dans la candidate,
# et seulement pour son tag. La clé n'est ni affichée ni copiée.
$key = Join-Path $env:APPDATA 'DRAFT-signature\draft-update-private.xml'
if (Test-Path $key) {
  foreach ($who in 'a', 'b') {
    $exe = Join-Path $Work "$who\DRAFT-Portable.exe"; $sig = Join-Path $Work "essai-$who.sig"
    & (Join-Path $PSScriptRoot 'sign-release.ps1') -Tag 'v9.9.9' -File (Join-Path $Work 'b\DRAFT-Portable.exe') -Out $sig | Out-Null
    $good = Start-Process $exe -ArgumentList '--test-signature', "`"$(Join-Path $Work 'b\DRAFT-Portable.exe')`"", 'v9.9.9', "`"$sig`"", '--data', "`"$Work\sig-data`"" -PassThru -Wait
    Check ($good.ExitCode -eq 0) "signature réelle : acceptée par la clé embarquée ($(Version $exe))"
    $bad = Start-Process $exe -ArgumentList '--test-signature', "`"$(Join-Path $Work 'b\DRAFT-Portable.exe')`"", 'v9.9.8', "`"$sig`"", '--data', "`"$Work\sig-data`"" -PassThru -Wait
    Check ($bad.ExitCode -ne 0) "signature réelle : refusée pour un autre tag ($(Version $exe))"
    $other = Start-Process $exe -ArgumentList '--test-signature', "`"$(Join-Path $Work 'c\DRAFT-Portable.exe')`"", 'v9.9.9', "`"$sig`"", '--data', "`"$Work\sig-data`"" -PassThru -Wait
    Check ($other.ExitCode -ne 0) "signature réelle : refusée pour un autre fichier ($(Version $exe))"
  }
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
