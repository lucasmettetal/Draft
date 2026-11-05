# Compare le classement du moteur de référence et du moteur actuel, draft par draft, et classe chaque différence.
param([string]$BaseRef = '95164df', [string]$DataDir = (Join-Path $env:LOCALAPPDATA 'DraftMid'), [string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-coaching\compare'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent; $web = Join-Path $repo 'web'
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force $Work | Out-Null
foreach ($f in 'draft-engine.js', 'draft-data.js', 'draft-champions.js', 'draft-role-sheet.js') { Copy-Item (Join-Path $web $f) $Work }
Copy-Item (Join-Path $DataDir 'draft-role-rates-live.js') $Work
$p = Start-Process git -ArgumentList '-C', "`"$repo`"", 'show', "${BaseRef}:web/draft-engine.js" -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $Work 'engine-base.js') -Wait
if ($p.ExitCode -ne 0) { throw 'Moteur de référence introuvable.' }
# Liste figée hors du navigateur : exactement les mêmes drafts pour les deux versions.
$live = [IO.File]::ReadAllText((Join-Path $DataDir 'draft-role-rates-live.js')); $j = ($live.Substring($live.IndexOf('{'))).TrimEnd().TrimEnd(';') | ConvertFrom-Json
$engine = [IO.File]::ReadAllText((Join-Path $web 'draft-engine.js')); $names = [regex]::Match($engine, 'ROSTER = Object\.fromEntries\("([^"]+)"').Groups[1].Value -split '\|' | ForEach-Object { ($_ -split ':')[0] }
$map = @{}; foreach ($n in $names) { $map[($n.ToLower() -replace '[^a-z0-9]', '')] = $n }; $map['monkeyking'] = 'Wukong'; $map['renata'] = 'Renata Glasc'
$rand = [Random]::new(20261105); $rows = @(foreach ($r in 0..4) { , @($j.champions.PSObject.Properties | Where-Object { $map[$_.Name] -and $_.Value[$r] -gt 0 } | ForEach-Object { $map[$_.Name]; $_.Value[$r] }) })
function Draw($row) { $total = 0; for ($i = 1; $i -lt $row.Count; $i += 2) { $total += $row[$i] }; $v = $rand.NextDouble() * $total; for ($i = 1; $i -lt $row.Count; $i += 2) { $v -= $row[$i]; if ($v -lt 0) { return $row[$i - 1] } }; $row[$row.Count - 2] }
$list = New-Object Collections.ArrayList
while ($list.Count -lt 1500) { $t = @(foreach ($r in 0..4) { Draw $rows[$r] }); if (@($t | Select-Object -Unique).Count -ne 5) { continue }; $size = 1 + $list.Count % 5; $c = $t | Sort-Object { $rand.Next() }; [void]$list.Add(@($c | Select-Object -First $size)) }
$pools = '{"Mid (tes 4)":["mid",["TF","Fizz","Kog''Maw","Xerath"]],"Jungle (Cho''Gath)":["jungle",["Cho''Gath","Viego","Master Yi","Briar","Lee Sin"]],"Jungle (classique)":["jungle",["Vi","Amumu","Graves","Kha''Zix","Hecarim"]],"Top":["top",["Darius","Garen","Malphite","Ornn","Camille"]],"ADC":["adc",["Jinx","Caitlyn","Kog''Maw","Ezreal","Vayne"]],"Support":["support",["Thresh","Leona","Soraka","Lulu","Lux"]]}'
[IO.File]::WriteAllText((Join-Path $Work 'fixed.js'), 'const FIXED=' + (ConvertTo-Json -InputObject @($list | ForEach-Object { , @($_) }) -Compress -Depth 3) + ';const POOLS=' + $pools + ';', [Text.UTF8Encoding]::new($false))
$tpl = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'experiments\ties-compare.html'))
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
function Run($engineFile, $name) {
  [IO.File]::WriteAllText((Join-Path $Work "$name.html"), $tpl.Replace('ENGINE', $engineFile), [Text.UTF8Encoding]::new($false))
  $prof = Join-Path $Work "pf-$name"; $out = Join-Path $Work "$name.dom"; $url = 'file:///' + ((Join-Path $Work "$name.html") -replace '\\', '/')
  $a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--virtual-time-budget=120000', '--dump-dom', "`"$url`"")
  $q = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"; [void]$q.WaitForExit(200000)
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  $m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>'); if (-not $m.Groups[1].Value) { throw "Aucun résultat : $name" }
  [Net.WebUtility]::HtmlDecode($m.Groups[1].Value) | ConvertFrom-Json
}
$old = Run 'engine-base.js' 'old'; $new = Run 'draft-engine.js' 'new'
"Drafts : $($list.Count), de 1 à 5 adversaires, tirées des fréquences observées. Référence : $BaseRef"
foreach ($k in $old.PSObject.Properties.Name) {
  $a = $old.$k; $b = $new.$k; $same = 0; $scoreDiff = 0; $orderOnly = 0; $topChange = 0; $ex = @()
  for ($i = 0; $i -lt $a.Count; $i++) {
    if ($a[$i] -ceq $b[$i]) { $same++; continue }
    $sa = ($a[$i] -split '\|' | Sort-Object) -join '|'; $sb = ($b[$i] -split '\|' | Sort-Object) -join '|'
    if ($sa -cne $sb) { $scoreDiff++; continue }
    $orderOnly++
    if (($a[$i] -split '=')[0] -cne ($b[$i] -split '=')[0]) { $topChange++; if ($ex.Count -lt 2) { $ex += "$($list[$i] -join '+') : $(($a[$i] -split '=')[0]) -> $(($b[$i] -split '=')[0]), $($b[$i])" } }
  }
  "$k : identiques $same, scores différents $scoreDiff, seul l'ordre des ex æquo change $orderOnly, dont premier différent $topChange"
  $ex | ForEach-Object { "    $_" }
}
