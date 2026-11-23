# Banc ciblé « rafales » : e2e arrêté juste après l'étape 7 (DRAFT_RAFALES_ONLY = nombre de tours), en alternant main et branche.
# Bureau caché, dossier de données neuf, clé de test retirée. Un seul lancement à la fois. Journaux conservés dans -Out.
param([string[]]$Plan = @('main:en', 'branche:en', 'main:en', 'branche:en'), [int]$Tours = 5, [string]$Out = '%SCRATCH%\panneaux\rafales\journaux', [string]$Tag = '')
$R = '%SCRATCH%\panneaux\rafales'
$bc = '%SCRATCH%\bureau-cache.ps1'
$runKey = 'Software\DRAFT-Test-Panneaux\Run'
New-Item -ItemType Directory -Force $Out | Out-Null
$n = 0
foreach ($step in $Plan) {
  $n++
  $v, $lang = $step.Split(':')
  $exe = "$R\exe-$v\DRAFT-Portable.exe"
  $data = Join-Path $env:TEMP ("draft-rafales-" + $PID + "-" + $n); Remove-Item -Recurse -Force $data -ErrorAction SilentlyContinue; New-Item -ItemType Directory $data | Out-Null
  $log = Join-Path $Out ("{0:D2}-{1}-{2}{3}.txt" -f $n, $v, $lang, $Tag)
  $env:DRAFT_RAFALES_ONLY = "$Tours"
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $res = & $bc -Exe $exe -Arguments "--e2e --lang $lang --data `"$data`" --out `"$log`" --run-key $runKey" -TimeoutSec 240 -Desktop DraftPanneaux
  Remove-Item Env:\DRAFT_RAFALES_ONLY
  $lignes = if (Test-Path $log) { @(Get-Content $log | Where-Object { $_ -like 'TOUR*' }) } else { @() }
  $vis = if (Test-Path $log) { (Get-Content $log | Where-Object { $_ -like 'MESURE : panneaux visibles*' }) } else { '' }
  "$n. $v $lang ($([int]$sw.Elapsed.TotalSeconds) s) $vis"
  foreach ($t in $lignes) { "   " + ($t -replace ' ; détail .*\|\|', ' ||') }
  Remove-Item -Recurse -Force $data -ErrorAction SilentlyContinue
}
Remove-Item ('HKCU:\' + ($runKey -replace '\\Run$', '')) -Recurse -ErrorAction SilentlyContinue
