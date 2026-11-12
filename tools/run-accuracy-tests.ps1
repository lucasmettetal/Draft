# Mesure l'attribution des rôles adverses, pour les cinq rôles, sans modifier le moteur. Aucun appel réseau, aucune clé.
# Le moteur est copié tel quel dans un dossier de travail et lancé dans Chrome sans interface.
#
# Drafts mesurées :
# - par défaut : équipes SYNTHÉTIQUES tirées depuis la fiche écrite (web/draft-role-sheet.js). Le générateur est le modèle
#   lui-même : c'est une borne haute, pas la précision réelle.
# - avec -StatePath (ou -LocalState) : les vraies compositions mises de côté par le calcul des rôles (réserve), jamais
#   comptées dans les rôles. Le fichier est lu en lecture seule, via une copie dans le dossier de travail.
# Données des rôles (le modèle) : la fiche écrite seule, et en plus le calcul live si -LiveFile est donné.
# Aucune composition n'est affichée ni écrite hors du dossier de travail : seulement des agrégats.
#
# Exemples :
#   pwsh -File tools/run-accuracy-tests.ps1
#   pwsh -File tools/run-accuracy-tests.ps1 -LocalState -LiveFile "$env:LOCALAPPDATA\DraftMid\draft-role-rates-live.js"
#   pwsh -File tools/run-accuracy-tests.ps1 -CompareRef main
param(
  [string]$StatePath,
  [switch]$LocalState,
  [string]$LiveFile,
  [string]$CompareRef,
  [int]$Teams = 2000,
  [int]$MaxHoldout = 4000,
  [int]$Seed = 20261110,
  [string]$Work = (Join-Path $env:TEMP 'draft-role-accuracy'),
  [string]$OutJson,
  [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$web = Join-Path $repo 'web'
$cases = Join-Path $repo 'tests\roles'
$appData = Join-Path $env:LOCALAPPDATA 'DraftMid'

# Le dossier de l'application n'est jamais un dossier de travail : rien n'y est écrit.
$fullWork = [IO.Path]::GetFullPath($Work)
if ($fullWork.StartsWith([IO.Path]::GetFullPath($appData), [StringComparison]::OrdinalIgnoreCase)) { throw "Dossier de travail refusé : $Work est dans le dossier de l'application." }

if ($LocalState -and -not $StatePath) { $StatePath = Join-Path $appData 'draft-role-rates-state.json' }
if ($StatePath -and -not (Test-Path -LiteralPath $StatePath -PathType Leaf)) {
  Write-Host "Réserve introuvable : $StatePath"
  Write-Host "Ce fichier n'existe qu'après un calcul « Mettre à jour les rôles » fait avec la version 2.12 ou plus récente."
  Write-Host "Aucune mesure sur de vraies parties n'est possible sur ce PC. Rien n'a été mesuré."
  exit 2
}
if ($LiveFile -and -not (Test-Path -LiteralPath $LiveFile -PathType Leaf)) { throw "Fichier de rôles live introuvable : $LiveFile" }

$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }

Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)

if (-not $SkipTests) {
  & (Join-Path $PSScriptRoot 'run-role-tests.ps1') -Work (Join-Path $Work 'tests')
  if ($LASTEXITCODE) { throw 'Tests de non-régression des rôles en échec : mesure arrêtée.' }
  ''
}

function Invoke-Page($dir, $name, $budget) {
  $prof = Join-Path $Work "pf-$((Split-Path $dir -Leaf))-$name"; $out = Join-Path $dir "$name.dom"
  $url = 'file:///' + ((Join-Path $dir "$name.html") -replace '\\', '/')
  $a = @('--headless=new', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", "--virtual-time-budget=$budget", '--dump-dom', "`"$url`"")
  $q = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
  [void]$q.WaitForExit(600000)
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  $m = [regex]::Match([IO.File]::ReadAllText($out), '<pre id="out">([\s\S]*?)</pre>')
  if (-not $m.Groups[1].Value) { throw "Aucun résultat pour $name ($dir)." }
  $r = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value) | ConvertFrom-Json -Depth 20
  if ($r.erreur) { throw "Erreur dans la page $name : $($r.erreur)" }
  $r
}
$head = '<!DOCTYPE html><html><head><meta charset="utf-8"></head><body><pre id="out"></pre><script>try { localStorage.clear(); } catch (e) {} window.addEventListener("error", e => { document.getElementById("out").textContent = JSON.stringify({ erreur: e.message + " (" + (e.filename || "").split("/").pop() + ":" + e.lineno + ")" }); });</script>'
$engineFiles = 'draft-champions.js', 'draft-role-sheet.js', 'draft-engine.js', 'draft-data.js'
$engineTags = '<script src="i18n/fr.js"></script><script src="draft-i18n.js"></script>' + ($engineFiles | ForEach-Object { "<script src=`"$_`"></script>" }) -join ''

function New-EngineDir($dir, $ref) {
  New-Item -ItemType Directory -Force $dir | Out-Null
  # Traduction (I18N-1) : toujours celle du dépôt actuel ; une version plus ancienne du moteur l'ignore.
  Copy-Item (Join-Path $web 'draft-i18n.js') $dir; Copy-Item (Join-Path $web 'i18n') (Join-Path $dir 'i18n') -Recurse -Force
  foreach ($f in $engineFiles) {
    if (-not $ref) { Copy-Item (Join-Path $web $f) $dir; continue }
    # Copie octet pour octet : un tube PowerShell abîmerait les accents.
    $p = Start-Process git -ArgumentList '-C', "`"$repo`"", 'show', "`"${ref}:web/$f`"" -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $dir $f) -RedirectStandardError (Join-Path $dir "$f.err") -Wait
    if ($p.ExitCode -ne 0) { throw "web/$f introuvable dans $ref." }
  }
}

# ---------- 1. Drafts figées ----------
$gen = Join-Path $Work 'generation'
New-EngineDir $gen $null
$genConf = [ordered]@{ mode = 'fiche'; seed = $Seed; count = $Teams }
$sourceText = "SYNTHÉTIQUE : $Teams équipes tirées depuis la fiche écrite (générateur = modèle, borne haute)."
if ($StatePath) {
  $copy = Join-Path $Work 'etat-copie.json'
  Copy-Item -LiteralPath $StatePath $copy
  $state = Get-Content -LiteralPath $copy -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 10
  $holdout = @($state.Holdout | Where-Object { $_ })
  if (-not $holdout.Count) {
    Write-Host "Le fichier d'état existe, mais sa réserve est vide : $StatePath"
    Write-Host 'Les parties mises de côté n''existent qu''après un calcul de rôles fait avec la version 2.12 ou plus récente. Rien n''a été mesuré.'
    exit 2
  }
  $genConf = [ordered]@{ mode = 'reserve'; seed = $Seed; max = $MaxHoldout; synthetique = [bool]$state.Synthetique; holdout = $holdout }
  $sourceText = if ($state.Synthetique) { "RÉSERVE SYNTHÉTIQUE (fichier de test marqué « Synthetique »), $($holdout.Count) équipes." } else { "RÉSERVE RÉELLE (données locales réelles), $($holdout.Count) équipes lues ; ordre de révélation tiré au hasard (graine $Seed)." }
}
[IO.File]::WriteAllText((Join-Path $gen 'gen-conf.js'), 'window.ROLE_GEN = ' + (ConvertTo-Json -InputObject $genConf -Depth 5 -Compress) + ';', $utf8)
Copy-Item (Join-Path $cases 'generer-drafts.js') $gen
[IO.File]::WriteAllText((Join-Path $gen 'gen.html'), $head + $engineTags + '<script src="gen-conf.js"></script><script src="generer-drafts.js"></script></body></html>', $utf8)
$drafts = Invoke-Page $gen 'gen' 60000
if (-not @($drafts.teams).Count) { throw 'Aucune équipe utilisable : rien à mesurer.' }
$draftsJs = 'window.ROLE_DRAFTS = ' + (ConvertTo-Json -InputObject $drafts -Depth 6 -Compress) + ';'

# ---------- 2. Mesure, variante par variante, sur les mêmes drafts ----------
$variants = [ordered]@{ 'actuel-fiche' = @{ ref = $null; live = $false } }
if ($LiveFile) { $variants['actuel-live'] = @{ ref = $null; live = $true } }
if ($CompareRef) {
  $variants["$CompareRef-fiche"] = @{ ref = $CompareRef; live = $false }
  if ($LiveFile) { $variants["$CompareRef-live"] = @{ ref = $CompareRef; live = $true } }
}
$results = [ordered]@{}
foreach ($name in $variants.Keys) {
  $v = $variants[$name]; $dir = Join-Path $Work ($name -replace '[^\w-]', '_')
  New-EngineDir $dir $v.ref
  [IO.File]::WriteAllText((Join-Path $dir 'drafts.js'), $draftsJs, $utf8)
  Copy-Item (Join-Path $cases 'mesure-roles.js'), (Join-Path $cases 'cas-atypiques.js') $dir
  $liveTag = ''
  if ($v.live) { Copy-Item -LiteralPath $LiveFile (Join-Path $dir 'draft-role-rates-live.js'); $liveTag = '<script src="draft-role-rates-live.js"></script>' }
  [IO.File]::WriteAllText((Join-Path $dir 'mesure.html'), $head + $liveTag + $engineTags + '<script src="drafts.js"></script><script src="cas-atypiques.js"></script><script src="mesure-roles.js"></script></body></html>', $utf8)
  $results[$name] = Invoke-Page $dir 'mesure' 600000
}

# ---------- 3. Rapport ----------
$Roles = 'top', 'jungle', 'mid', 'adc', 'support'
$RoleLabel = @{ top = 'Top'; jungle = 'Jungle'; mid = 'Mid'; adc = 'ADC'; support = 'Support' }
function Wilson($k, $n) {
  if (-not $n) { return '-' }
  $z = 1.96; $p = $k / $n; $d = 1 + $z * $z / $n; $c = ($p + $z * $z / (2 * $n)) / $d; $m = $z * [Math]::Sqrt($p * (1 - $p) / $n + $z * $z / (4 * $n * $n)) / $d
  '{0:0.0} % [{1:0.0}-{2:0.0}]' -f (100 * $p), (100 * [Math]::Max([double]0, $c - $m)), (100 * [Math]::Min([double]1, $c + $m))
}
function Pct($k, $n) { if (-not $n) { '-' } else { '{0:0.0} %' -f (100 * $k / $n) } }
function Sum-Bins($list) { $b = 0..9 | ForEach-Object { @{ n = 0; p = 0.0; y = 0.0 } }; foreach ($x in $list) { for ($i = 0; $i -lt 10; $i++) { $b[$i].n += $x[$i].n; $b[$i].p += $x[$i].p; $b[$i].y += $x[$i].y } }; $b }
function Show-Bins($b) {
  '    tranche de p   | n        | p moyenne | fréquence observée'
  for ($i = 0; $i -lt 10; $i++) { if ($b[$i].n) { '    [{0:0.0} ; {1:0.0}{2}   | {3,-8} | {4:0.000}     | {5:0.000}' -f ($i / 10), (($i + 1) / 10), $(if ($i -eq 9) { ']' } else { '[' }), $b[$i].n, ($b[$i].p / $b[$i].n), ($b[$i].y / $b[$i].n) } }
}
$summary = [ordered]@{}
$report = foreach ($name in $results.Keys) {
  $r = $results[$name]
  "=================== Variante $name"
  "Drafts : $sourceText"
  "Modèle : fiche écrite" + $(if ($r.live.charge) { " + calcul live" + $(if ($r.live.synthetique) { ' (fichier SYNTHÉTIQUE de test)' } else { " ($($r.live.parties) parties)" }) } else { ' seule' })
  "Équipes mesurées : $($r.equipes)" + $(if ($r.ecartees) { " (écartées : $($r.ecartees), champion inconnu ou équipe incomplète)" } else { '' })
  ''
  '--- Rôle par rôle : le 1er candidat (ou « pas encore choisi ») est-il le vrai titulaire ? (égalités exactes : crédit partagé)'
  foreach ($s in 1..5) {
    $t = $r.tailles."$s"
    $cells = for ($k = 0; $k -lt 5; $k++) { "$($RoleLabel[$Roles[$k]]) $(Wilson $t.role[$k].ok $t.role[$k].n)" + $(if ($t.role[$k].ties) { " (égalités $($t.role[$k].ties))" } else { '' }) }
    "  $s révélé(s), n=$($t.n) : " + ($cells -join ' | ')
    "     Brier moyen $('{0:0.000}' -f ($t.brier / (5 * $t.n))) | log-loss moyenne $('{0:0.000}' -f ($t.logloss / (5 * $t.n))) | champions au rôle « probable » $(Pct $t.status.probable.n ($t.status.probable.n + $t.status.indetermine.n)), justes $(Wilson $t.status.probable.ok $t.status.probable.n) | « indéterminé » justes $(Wilson $t.status.indetermine.ok $t.status.indetermine.n)"
  }
  $c = $r.tailles.'5'.complete
  "  Assignation complète juste (5 révélés, répartition la plus probable sur 120) : $(Wilson $c.ok $c.n)" + $(if ($c.ties) { " (égalités exactes : $($c.ties))" } else { '' })
  "     par rôle : " + ((0..4 | ForEach-Object { "$($RoleLabel[$Roles[$_]]) $(Pct $r.tailles.'5'.completeRole[$_] $c.n)" }) -join ' | ')
  ''
  '--- Classes de MESURE (seuils : probable ≥ 0,6 ; possible 0,2 à 0,6 ; écarté < 0,2), toutes tailles, paires champion × rôle'
  foreach ($k in 'probable', 'possible', 'ecarte') {
    $n = 0; $y = 0; foreach ($s in 1..5) { $n += $r.tailles."$s".pairs.$k.n; $y += $r.tailles."$s".pairs.$k.y }
    "  $(@{ probable = 'probable'; possible = 'possible'; ecarte = 'écarté' }[$k]) : $n paires ($(Pct $n (5 * ((1..5 | ForEach-Object { $r.tailles."$_".n * $_ }) | Measure-Object -Sum).Sum))) | rôle réel $(Wilson $y $n)"
  }
  ''
  '--- Calibration rôle par rôle (toutes tailles, options = champions révélés + « pas encore choisi »)'
  Show-Bins (Sum-Bins (1..5 | ForEach-Object { , $r.tailles."$_".calib }))
  ''
  '--- Adversaire direct, par rôle du joueur'
  foreach ($role in $Roles) {
    "  Profil $($RoleLabel[$role])"
    $agg = @{ n = 0; named = 0; namedOk = 0; hidden = 0; hiddenNamed = 0; faible = 0; brier = 0.0; ll = 0.0; top1 = 0.0 }
    foreach ($s in 1..5) {
      $p = $r.profils.$role."$s"
      foreach ($f in 'n', 'named', 'namedOk', 'hidden', 'hiddenNamed', 'top1') { $agg[$f] += $p.$f }; $agg.faible += $p.conf.faible.n; $agg.brier += $p.brier; $agg.ll += $p.logloss
      "    $s révélé(s) n=$($p.n) : 1er candidat juste $(Wilson $p.top1 $p.n) | confiance faible $(Wilson $p.conf.faible.n $p.n) | moyenne $($p.conf.moyenne.n) | élevée $($p.conf.'élevée'.n) | nommé $(Pct $p.named $p.n), juste $(Wilson $p.namedOk $p.named) | pas révélé $($p.hidden) -> nommé à tort $(Wilson $p.hiddenNamed $p.hidden) | vrai cas gardé $(Pct $p.covered $p.n) | Brier $('{0:0.000}' -f ($p.brier / $p.n)) | log-loss $('{0:0.000}' -f ($p.logloss / $p.n))"
    }
    $sl = @{}; foreach ($k in 'probable', 'possible', 'ecarte') { $sl[$k] = @{ n = 0; y = 0 }; foreach ($s in 1..5) { $sl[$k].n += $r.profils.$role."$s".slot.$k.n; $sl[$k].y += $r.profils.$role."$s".slot.$k.y } }
    "    1er candidat nommé par classe : probable $($sl.probable.n), juste $(Wilson $sl.probable.y $sl.probable.n) | possible $($sl.possible.n), juste $(Wilson $sl.possible.y $sl.possible.n) | sous 0,2 : $($sl.ecarte.n), juste $(Wilson $sl.ecarte.y $sl.ecarte.n)"
    $summary["$name|$role"] = [ordered]@{ variante = $name; profil = $role; n = $agg.n; top1 = $agg.top1 / $agg.n; faible = $agg.faible / $agg.n; nomme = $agg.named / $agg.n; nommeJuste = $(if ($agg.named) { $agg.namedOk / $agg.named } else { $null }); nommeATort = $(if ($agg.hidden) { $agg.hiddenNamed / $agg.hidden } else { $null }); brier = $agg.brier / $agg.n; logloss = $agg.ll / $agg.n; top1_5 = $r.profils.$role.'5'.top1 / $r.profils.$role.'5'.n }
  }
  '  (La calibration de l''adversaire direct est celle de la colonne du même rôle ci-dessus : mêmes options, mêmes probabilités.)'
  ''
  '--- Champions suivis, par rôle réel (toutes tailles ; 5 révélés entre parenthèses)'
  foreach ($f in $r.focus) {
    $rows = @($r.champions | Where-Object { $_.name -eq $f.name })
    $parts = if ($f.parts) { ((0..4 | ForEach-Object { "$($RoleLabel[$Roles[$_]]) $('{0:0.00}' -f $f.parts[$_])" }) -join ' ') } else { 'aucune' }
    "  $($f.name) [parts du modèle : $parts ; qualité $($f.qualite)]"
    if (-not $rows.Count) { '     aucune apparition dans ces drafts' }
    foreach ($x in $rows) { "     joué $($RoleLabel[$x.role]) : n=$($x.n), rôle juste $(Wilson $x.ok $x.n), p moyenne du vrai rôle $('{0:0.00}' -f ($x.p / $x.n))" + $(if ($x.n5) { " (5 révélés : n=$($x.n5), juste $(Pct $x.ok5 $x.n5))" } else { '' }) }
  }
  $worst = @($r.champions | Where-Object { $_.n -ge 30 } | Sort-Object { $_.ok / $_.n }, { -$_.n } | Select-Object -First 12)
  '  Champion × rôle les moins bien attribués (n ≥ 30)'
  foreach ($x in $worst) { "     $($x.name) joué $($RoleLabel[$x.role]) : n=$($x.n), rôle juste $(Wilson $x.ok $x.n), p moyenne $('{0:0.00}' -f ($x.p / $x.n))" }
  ''
  '--- Champions flex'
  "  Fiche écrite : $($r.ficheTotal) champions, $($r.ficheUnRole) avec un seul rôle, $(@($r.flexFiche).Count) avec plusieurs : " + ((@($r.flexFiche) | ForEach-Object { "$($_.name) ($($_.code))" }) -join ', ')
  "  Sans fiche : $(@($r.sansFiche) -join ', ')"
  "  Flex selon le modèle (au moins deux rôles à p ≥ 0,2) : $(@($r.flexModele).Count) : " + ((@($r.flexModele) | ForEach-Object { "$($_.name) ($((@($_.roles) | ForEach-Object { $RoleLabel[$_] }) -join '/'))" }) -join ', ')
  '  Égalités exactes (parts identiques, impossibles à départager sans autre information) :'
  foreach ($g in $r.egalites) { "     $((0..4 | ForEach-Object { '{0:0.00}' -f $g.parts[$_] }) -join '/') : $(@($g.champions).Count) champions : $(@($g.champions) -join ', ')" }
  ''
  '--- Cas atypiques SYNTHÉTIQUES (rôles attendus annotés à la main)'
  $ok = 0; $tot = 0; $oppOk = 0; $wrong = 0
  foreach ($x in $r.cas) {
    $lines = @($x.lignes | Where-Object { $null -ne $_.ok })
    $ok += ($lines | Measure-Object ok -Sum).Sum; $tot += $lines.Count; if ($x.adversaireJuste) { $oppOk++ }; if ($x.nommeATort) { $wrong++ }
    "  $($x.id) [profil $($RoleLabel[$x.profil])$(if ($x.corrections) { ', correction' })] : adversaire attendu $(if ($x.adversaireAttendu) { $x.adversaireAttendu } else { 'pas encore choisi' }), affiché $(if ($x.identifie) { $x.identifie } else { 'personne' }) (confiance $($x.confiance)) | " + (($lines | ForEach-Object { "$($_.name)=$($RoleLabel[$_.attendu]) p $('{0:0.00}' -f $_.pAttendu) $(@{ probable = 'probable'; possible = 'possible'; ecarte = 'écarté' }[$_.classe])" }) -join ', ')
  }
  "  Rôles annotés retrouvés en 1er : $('{0:0.0}' -f $ok) sur $tot | adversaire affiché conforme à l'annotation (ou personne si non révélé) : $oppOk sur $(@($r.cas).Count) | adversaire nommé à tort : $wrong"
  $summary["$name|cas"] = [ordered]@{ variante = $name; rolesJustes = $ok; roles = $tot; adversaireConforme = $oppOk; cas = @($r.cas).Count; nommeATort = $wrong }
  ''
}
$report
if (@($results.Keys).Count -gt 1) {
  '=================== Comparaison des variantes (toutes tailles, mêmes drafts)'
  foreach ($role in $Roles) {
    "  Profil $($RoleLabel[$role])"
    foreach ($name in $results.Keys) { $s = $summary["$name|$role"]; '    {0,-22} 1er candidat {1:0.0} % (5 révélés {2:0.0} %) | faible {3:0.0} % | nommé juste {4} | nommé à tort {5} | Brier {6:0.000} | log-loss {7:0.000}' -f $name, (100 * $s.top1), (100 * $s.top1_5), (100 * $s.faible), $(if ($null -ne $s.nommeJuste) { '{0:0.0} %' -f (100 * $s.nommeJuste) } else { '-' }), $(if ($null -ne $s.nommeATort) { '{0:0.0} %' -f (100 * $s.nommeATort) } else { '-' }), $s.brier, $s.logloss }
  }
}
[IO.File]::WriteAllLines((Join-Path $Work 'rapport.txt'), [string[]]$report, $utf8)
if ($OutJson) {
  $agg = [ordered]@{ date = (Get-Date -Format 'yyyy-MM-dd'); source = $sourceText; seed = $Seed; variantes = $results; resume = $summary }
  [IO.File]::WriteAllText($OutJson, (ConvertTo-Json -InputObject $agg -Depth 20), $utf8)
}
"Rapport complet : $(Join-Path $Work 'rapport.txt')"
