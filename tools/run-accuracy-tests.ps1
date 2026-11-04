# Mesure la détection de l'adversaire direct, sur des données séparées de celles du modèle. Rien n'est publié.
# - Parties réservées par le calcul des rôles, s'il y en a : de vraies compositions, jamais comptées dans le modèle.
# - Sinon, simulation : les observations sont coupées en deux, l'une pour le modèle, l'autre pour tirer les drafts.
#   Les rôles y sont tirés indépendamment : ce n'est PAS une mesure sur de vraies drafts, et le résultat le dit.
param(
  [string]$DataDir = (Join-Path $env:LOCALAPPDATA 'DraftMid'),
  [string]$BaselineTag = 'v2.10.0',
  [string]$Work = (Join-Path $env:TEMP 'draftmid-accuracy'),
  [int]$PerSize = 600
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$web = Join-Path $repo 'web'
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
$liveFile = Join-Path $DataDir 'draft-role-rates-live.js'
if (-not (Test-Path $liveFile)) { throw "Aucun calcul de rôles dans $DataDir : lance d'abord « Mettre à jour les rôles »." }

Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
foreach ($f in 'draft-engine.js', 'draft-data.js', 'draft-champions.js', 'draft-role-sheet.js', 'draft-role-rates.js') { Copy-Item (Join-Path $web $f) $Work }
Copy-Item $liveFile (Join-Path $Work 'live-all.js')
# Copie octet pour octet : un tube PowerShell abîmerait les accents.
$p = Start-Process git -ArgumentList '-C', "`"$repo`"", 'show', "${BaselineTag}:web/draft-engine.js" -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $Work 'engine-base.js') -Wait
if ($p.ExitCode -ne 0) { throw "Moteur $BaselineTag introuvable." }

$holdout = @()
$statePath = Join-Path $DataDir 'draft-role-rates-state.json'
if (Test-Path $statePath) { $state = Get-Content $statePath -Raw -Encoding UTF8 | ConvertFrom-Json; if ($state.Holdout) { $holdout = @($state.Holdout) } }
[IO.File]::WriteAllText((Join-Path $Work 'holdout.js'), 'const HOLDOUT = ' + ($(if ($holdout.Count) { ConvertTo-Json -InputObject $holdout -Depth 3 -Compress } else { '[]' })) + ';', [Text.UTF8Encoding]::new($false))

function Invoke-Page($name, $html, $budget) {
  [IO.File]::WriteAllText((Join-Path $Work "$name.html"), $html, [Text.UTF8Encoding]::new($false))
  $prof = Join-Path $Work "pf-$name"; $out = Join-Path $Work "$name.dom"
  $url = 'file:///' + ((Join-Path $Work "$name.html") -replace '\\', '/')
  $a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", "--virtual-time-budget=$budget", '--dump-dom', "`"$url`"")
  $q = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
  [void]$q.WaitForExit(240000)
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  $m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>')
  if (-not $m.Groups[1].Value) { throw "Aucun résultat pour $name." }
  [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
}

$profileTag = '<script>localStorage.setItem("draftProfile", JSON.stringify({ role:"mid", pool:["TF","Fizz","Kog''Maw","Xerath"] }));localStorage.setItem("draftPatch","16.22");</script>'
# Générateur : graine fixe, liste figée dans un fichier, identique pour toutes les versions comparées.
$gen = @'
<!DOCTYPE html><html><head><meta charset="utf-8"></head><body><pre id="out"></pre>
<script src="live-all.js"></script><script src="holdout.js"></script><script src="draft-champions.js"></script>
PROFILE<script src="engine-base.js"></script>
<script>
function mulberry(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
const rng = mulberry(20261104), int = n => Math.floor(rng() * n);
const display = {}; NAMES.forEach(n => { const k = norm(n); display[({ wukong:"monkeyking", renataglasc:"renata" })[k] || k] = n; });
const live = window.ROLE_RATES_LIVE.champions, ids = Object.keys(live).filter(k => display[k]);
const train = {}, test = {};
ids.forEach(k => { train[k] = [0,0,0,0,0]; test[k] = [0,0,0,0,0]; live[k].forEach((v, r) => { for (let i = 0; i < v; i++) (rng() < .5 ? train : test)[k][r]++; }); });
const real = HOLDOUT.length > 0;
const pick = r => { const total = ids.reduce((s, k) => s + test[k][r], 0); let x = rng() * total; for (const k of ids) { x -= test[k][r]; if (x < 0) return k; } return ids[ids.length - 1]; };
const team = () => { if (real) return HOLDOUT[int(HOLDOUT.length)]; for (;;) { const t = [0,1,2,3,4].map(pick); if (new Set(t).size === 5) return t; } };
const drafts = [];
[1,2,3,4,5].forEach(size => { for (let i = 0; i < PERSIZE; i++) {
  const t = team(), order = [0,1,2,3,4]; for (let j = 4; j > 0; j--) { const k = int(j + 1); [order[j], order[k]] = [order[k], order[j]]; }
  if (!t.every(k => display[k])) { i--; continue; }
  const shown = order.slice(0, size).map(r => display[t[r]]);
  drafts.push({ team:shown, truth:display[t[2]], revealed:shown.includes(display[t[2]]), size });
} });
document.getElementById("out").textContent = JSON.stringify({ real, drafts, train: { updated: window.ROLE_RATES_LIVE.updated, games: Math.round(window.ROLE_RATES_LIVE.games / 2), champions: real ? live : train } });
</script></body></html>
'@
$fixed = (Invoke-Page 'gen' ($gen.Replace('PROFILE', $profileTag).Replace('PERSIZE', "$PerSize")) 30000) | ConvertFrom-Json
$drafts = $fixed.drafts
[IO.File]::WriteAllText((Join-Path $Work 'fixed.js'), 'const FIXED = ' + (ConvertTo-Json -InputObject $drafts -Depth 4 -Compress) + ';', [Text.UTF8Encoding]::new($false))
$trainJson = ConvertTo-Json -InputObject $fixed.train -Depth 4 -Compress
[IO.File]::WriteAllText((Join-Path $Work 'live-train.js'), "window.ROLE_RATES_LIVE = $trainJson;", [Text.UTF8Encoding]::new($false))
# Même données au format V2.12 : la V2.10 doit les lire à l'identique si tu reviens en arrière.
$v2 = $fixed.train | Select-Object *, @{n='version';e={2}}, @{n='regions';e={@('Test')}}, @{n='patches';e={ @{ '16.22' = @{ games = $fixed.train.games; collected = $fixed.train.updated; champions = $fixed.train.champions } } }}
[IO.File]::WriteAllText((Join-Path $Work 'live-v2.js'), "window.ROLE_RATES_LIVE = $(ConvertTo-Json -InputObject $v2 -Depth 6 -Compress);", [Text.UTF8Encoding]::new($false))

$eval = @'
<!DOCTYPE html><html><head><meta charset="utf-8"></head><body><pre id="out"></pre>
RATES LIVE<script src="draft-champions.js"></script><script src="draft-role-sheet.js"></script>
PROFILE<script src="ENGINE"></script>EXTRA<script src="fixed.js"></script>
<script>
const isNew = typeof opponentBelief === "function";
const rows = FIXED.map(d => {
  const guess = guessMid(d.team), truthMid = d.revealed ? d.truth : null;
  const row = { g: guess };
  // Mêmes règles, adversaire imposé : doit rester identique d'une version à l'autre.
  row.rules = analyze(d.team, truthMid, true).ranking.map(p => p.pick + ":" + p.score.toFixed(3)).join();
  let ranking;
  if (isNew) {
    const b = opponentBelief(d.team), s = beliefScenarios(b);
    row.i = identifiedOpponent(d.team); row.c = b.confidence; row.h = s.map(x => x.mid); row.p = b.candidates.length ? b.candidates[0].p : 0;
    ranking = analyze(d.team, row.i, true, s).ranking;
  } else ranking = analyze(d.team, guess, true).ranking;
  // Regret : ce que le pick choisi perd, une fois le vrai adversaire connu, avec les mêmes règles.
  const truthRank = analyze(d.team, truthMid, true).ranking;
  row.pick = ranking.length ? ranking[0].pick : null;
  const chosen = truthRank.find(p => p.pick === row.pick);
  row.regret = chosen ? truthRank[0].score - chosen.score : 0;
  return row;
});
document.getElementById("out").textContent = JSON.stringify(rows);
</script></body></html>
'@
function Measure-Variant($name, $engine, $rates, $live, $isNew) {
  $page = $eval.Replace('RATES', $(if ($rates) { '<script src="draft-role-rates.js"></script>' } else { '' })).Replace('LIVE', $(if ($live) { "<script src=`"$live`"></script>" } else { '' })).Replace('PROFILE', $profileTag).Replace('ENGINE', $engine).Replace('EXTRA', $(if ($isNew) { '<script src="draft-data.js"></script>' } else { '' }))
  , ((Invoke-Page $name $page 120000) | ConvertFrom-Json)
}
function Wilson($k, $n) { if (-not $n) { return '-' }; $z = 1.96; $p = $k / $n; $d = 1 + $z * $z / $n; $c = ($p + $z * $z / (2 * $n)) / $d; $m = $z * [Math]::Sqrt($p * (1 - $p) / $n + $z * $z / (4 * $n * $n)) / $d; '{0:0} % [{1:0}-{2:0}]' -f (100 * $p), (100 * ($c - $m)), (100 * ($c + $m)) }
function Report($title, $rows, $field) {
  "--- $title"
  foreach ($size in 1..5) {
    $ok = 0; $bad = 0; $none = 0; $hiddenNamed = 0; $hidden = 0; $regret = 0.0; $n = 0
    for ($i = 0; $i -lt $rows.Count; $i++) {
      $d = $drafts[$i]; if ($d.size -ne $size) { continue }; $n++; $regret += $rows[$i].regret; $v = $rows[$i].$field
      if (-not $d.revealed) { $hidden++; if ($v) { $hiddenNamed++ }; continue }
      if (-not $v) { $none++ } elseif ($v -eq $d.truth) { $ok++ } else { $bad++ }
    }
    $named = $ok + $bad
    "  $size champion(s) : revele $($ok+$bad+$none) -> juste $ok, faux $bad, aucun $none | juste quand un nom est donne : $(Wilson $ok $named) | pas revele $hidden -> nomme a tort $(Wilson $hiddenNamed $hidden) | regret moyen $('{0:0.00}' -f ($regret / [Math]::Max(1, $n)))"
  }
}
"Source des drafts : " + $(if ($fixed.real) { "VRAIES compositions réservées, ordre de révélation tiré au hasard." } else { "SIMULATION : observations séparées, rôles tirés indépendamment. Pas une mesure sur de vraies drafts." })
"Drafts : $($drafts.Count), modele : $($fixed.train.games) parties"
$v = [ordered]@{}
$v['base-live'] = Measure-Variant 'base-live' 'engine-base.js' $true 'live-train.js' $false
$v['new-live'] = Measure-Variant 'new-live' 'draft-engine.js' $true 'live-train.js' $true
$v['base-ref'] = Measure-Variant 'base-ref' 'engine-base.js' $true $null $false
$v['new-ref'] = Measure-Variant 'new-ref' 'draft-engine.js' $true $null $true
$v['new-sheet'] = Measure-Variant 'new-sheet' 'draft-engine.js' $false $null $true
$v['new-sheet-live'] = Measure-Variant 'new-sheet-live' 'draft-engine.js' $false 'live-train.js' $true
$v['base-v2'] = Measure-Variant 'base-v2' 'engine-base.js' $true 'live-v2.js' $false

Report "$BaselineTag, avec parties, suppose" $v['base-live'] 'g'
Report 'V2.12, avec parties, affiche' $v['new-live'] 'i'
Report "$BaselineTag, Meraki seul, suppose" $v['base-ref'] 'g'
Report 'V2.12, Meraki seul, affiche' $v['new-ref'] 'i'
Report 'V2.12, sans Meraki ni parties (roles ecrits), affiche' $v['new-sheet'] 'i'
Report 'V2.12, sans Meraki, avec parties, affiche' $v['new-sheet-live'] 'i'
foreach ($k in 'new-live', 'new-ref', 'new-sheet') {
  "--- Confiance $k (toutes tailles) : le nom affiche est-il juste, le vrai cas est-il dans les hypotheses ?"
  foreach ($c in 'élevée', 'moyenne', 'faible') {
    $n = 0; $hit = 0; $cov = 0; $rows = $v[$k]
    for ($i = 0; $i -lt $rows.Count; $i++) { if ($rows[$i].c -ne $c) { continue }; $n++; if ($rows[$i].i -and $rows[$i].i -eq $drafts[$i].truth) { $hit++ }; if ((@($rows[$i].h) -contains $drafts[$i].truth) -or (-not $drafts[$i].revealed -and (@($rows[$i].h) -contains $null))) { $cov++ } }
    if ($n) { "  $c : $n drafts | nom juste $(Wilson $hit $n) | vrai cas garde $(Wilson $cov $n)" }
  }
}
'--- Non-regression'
$same = 0; for ($i = 0; $i -lt $drafts.Count; $i++) { if ($v['base-live'][$i].rules -eq $v['new-live'][$i].rules) { $same++ } }
"Scores identiques a adversaire impose ($BaselineTag / V2.12) : $same sur $($drafts.Count)"
$same = 0; for ($i = 0; $i -lt $drafts.Count; $i++) { if ($v['base-live'][$i].g -eq $v['base-v2'][$i].g -and $v['base-live'][$i].rules -eq $v['base-v2'][$i].rules) { $same++ } }
"$BaselineTag lit le nouveau format de donnees a l'identique : $same sur $($drafts.Count)"
$diff = 0; for ($i = 0; $i -lt $drafts.Count; $i++) { if ($v['base-live'][$i].pick -ne $v['new-live'][$i].pick) { $diff++ } }
"Pick conseille different ($BaselineTag / V2.12, avec parties) : $diff sur $($drafts.Count)"
