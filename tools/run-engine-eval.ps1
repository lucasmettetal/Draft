# Banc d'évaluation du moteur stratégique, sur scénarios SYNTHÉTIQUES (tests/moteur). Chrome headless, aucun appel réseau,
# aucune donnée personnelle : seuls les fichiers livrés dans web/ sont copiés dans le dossier de travail. Rien n'est écrit dans le dépôt.
#   -Work <dossier>       : dossier de travail (par défaut dist-engine-eval, ignoré par git).
#   -AvecEvaluation       : mesure aussi le jeu d'évaluation GELÉ. À réserver aux jalons de version (docs/technique/VALIDATION.md).
#   -Instantane <json>    : instantané de référence à comparer (par défaut celui du manifeste). -SansInstantane pour ne rien comparer.
#   -Criteres front,map   : critères que la PR déclare modifier. Tout autre changement est « INATTENDU » et fait échouer le banc.
#   -Generer              : régénère les scénarios (graine fixe) dans -Work\generes, sans le moteur, et vérifie que le jeu gelé est reproduit.
#   -Etudes               : ajoute les études de décision (règle peel, critère map), calculées en mémoire.
#   -NouvelInstantane <f> : enregistre l'instantané produit dans ce fichier (pour créer une nouvelle référence, sous un nouveau nom).
# Code de sortie 1 si : empreinte ou séparation des jeux non conforme, invariance cassée, égalité attendue ou pick pris non respecté,
# changement INATTENDU par rapport à l'instantané, ou jeu gelé non reproduit par le générateur (-Generer).
param(
  [string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-engine-eval'),
  [switch]$AvecEvaluation,
  [string]$Instantane = '',
  [switch]$SansInstantane,
  [string[]]$Criteres = @(),
  [switch]$Generer,
  [switch]$Etudes,
  [string]$NouvelInstantane = ''
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent; $web = Join-Path $repo 'web'; $tests = Join-Path $repo 'tests\moteur'
$utf8 = [Text.UTF8Encoding]::new($false)
$echecs = [Collections.Generic.List[string]]::new()
$Criteres = @($Criteres | ForEach-Object { $_ -split ',' } | Where-Object { $_ } | ForEach-Object { $_.Trim() })

function Lire([string]$chemin) { [IO.File]::ReadAllText($chemin, $utf8) }
# Empreinte canonique : SHA-256 du texte UTF-8 avec fins de ligne LF (indépendante de core.autocrlf).
function Empreinte([string]$texte) {
  $b = $utf8.GetBytes($texte.Replace("`r`n", "`n"))
  [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($b)).Replace('-', '')
}
function Compact($objet) { $objet | ConvertTo-Json -Depth 50 -Compress }

# ---------- 1. Contrôles structurels, sans navigateur ni moteur ----------
$manifeste = Lire (Join-Path $tests 'manifeste.json') | ConvertFrom-Json -Depth 20
$jeux = @{}
foreach ($k in 'developpement', 'evaluation_gele') {
  $m = $manifeste.$k
  $texte = Lire (Join-Path $tests $m.fichier)
  $h = Empreinte $texte
  if ($h -ne $m.sha256) { $echecs.Add("$($m.fichier) : empreinte $h, le manifeste attend $($m.sha256)") }
  $jeu = $texte | ConvertFrom-Json -Depth 30
  if ($jeu.synthetique -ne $true -or @($jeu.scenarios | Where-Object { $_.synthetique -ne $true }).Count) { $echecs.Add("$($m.fichier) : champ synthetique absent") }
  if (@($jeu.scenarios).Count -ne $m.scenarios) { $echecs.Add("$($m.fichier) : $(@($jeu.scenarios).Count) scénarios, le manifeste en annonce $($m.scenarios)") }
  $jeux[$k] = @{ texte = $texte; jeu = $jeu; empreinte = $h }
}
if ($jeux.evaluation_gele.jeu.jeu -ne 'evaluation_gele' -or $jeux.developpement.jeu.jeu -ne 'developpement') { $echecs.Add('Champ « jeu » inattendu dans un fichier de scénarios') }
# Séparation : aucun identifiant commun, aucune draft identique (mêmes adversaires, même pool, même rôle), sans tenir compte de l'ordre.
$cle = { param($s) "$($s.joueur.role)|$((@($s.joueur.pool) | Sort-Object) -join ',')|$((@($s.adversaires) | Sort-Object) -join ',')" }
$idsDev = @{}; $clesDev = @{}
foreach ($s in $jeux.developpement.jeu.scenarios) {
  if ($idsDev.ContainsKey($s.id)) { $echecs.Add("Identifiant en double dans le développement : $($s.id)") }
  $idsDev[$s.id] = 1; $clesDev[(& $cle $s)] = $s.id
}
$communsId = 0; $communsDraft = 0
foreach ($s in $jeux.evaluation_gele.jeu.scenarios) {
  if ($idsDev.ContainsKey($s.id)) { $communsId++; $echecs.Add("Identifiant commun aux deux jeux : $($s.id)") }
  $k = & $cle $s
  if ($clesDev.ContainsKey($k)) { $communsDraft++; $echecs.Add("Draft identique dans les deux jeux : dev $($clesDev[$k]) / éval $($s.id)") }
}
# Indépendance des réponses attendues : le générateur ne doit appeler aucune fonction ni donnée du moteur évalué.
$gen = (Lire (Join-Path $tests 'generer-scenarios.js')) -replace '(?m)//.*$', ''
$interdits = 'analyze', 'evaluatePick', 'evaluateScenarios', 'roleBelief', 'opponentBelief', 'beliefScenarios', 'identifiedOpponent', 'guessMid', 'roleEvidence', 'roleShares', 'RULES', 'CRITERIA', 'ROSTER', 'MATCHUPS', 'ROLE_SHEET', 'ROLE_RATES', 'applyProfile'
$trouves = @($interdits | Where-Object { $gen -match "\b$_\b" })
if ($trouves.Count) { $echecs.Add("generer-scenarios.js utilise le moteur : $($trouves -join ', ')") }
"Jeux de scénarios SYNTHÉTIQUES : développement $(@($jeux.developpement.jeu.scenarios).Count), évaluation gelé $(@($jeux.evaluation_gele.jeu.scenarios).Count)"
"  Empreintes SHA-256 (texte LF) : développement $(if ($jeux.developpement.empreinte -eq $manifeste.developpement.sha256) { 'conforme' } else { 'NON CONFORME' }), évaluation gelé $(if ($jeux.evaluation_gele.empreinte -eq $manifeste.evaluation_gele.sha256) { 'conforme' } else { 'NON CONFORME' })"
"  Séparation : $communsId identifiant commun, $communsDraft draft identique entre les deux jeux"
"  Générateur : $(if ($trouves.Count) { 'UTILISE LE MOTEUR (' + ($trouves -join ', ') + ')' } else { 'aucune fonction du moteur appelée' })"

# ---------- Chrome headless ----------
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome introuvable.' }
Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Work | Out-Null
function Page([string]$dossier, [string]$nom) {
  $prof = Join-Path $dossier "pf-$nom"; $out = Join-Path $dossier "$nom.dom"
  $url = 'file:///' + ((Join-Path $dossier $nom) -replace '\\', '/')
  $a = @('--headless=new', '--disable-gpu', '--no-first-run', '--disable-extensions', '--allow-file-access-from-files', "--user-data-dir=`"$prof`"", '--virtual-time-budget=600000', '--dump-dom', "`"$url`"")
  $p = Start-Process $chrome -ArgumentList $a -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError "$out.err"
  $fini = $p.WaitForExit(900000)
  # On n'arrête que les Chrome lancés ici (reconnus à leur dossier de profil).
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  if (-not $fini) { throw "$nom : délai dépassé." }
  Lire $out
}
function Bloc([string]$dom, [string]$id) {
  $m = [regex]::Match($dom, "<pre id=`"$id`">([\s\S]*?)</pre>")
  if (-not $m.Success) { return '' }
  [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
}

# ---------- 2. Régénération (sans le moteur) ----------
if ($Generer) {
  $g = Join-Path $Work 'generer'; New-Item -ItemType Directory -Force $g | Out-Null
  Copy-Item (Join-Path $web 'draft-champions.js') $g
  foreach ($f in 'generer-scenarios.js', 'generer.html') { Copy-Item (Join-Path $tests $f) $g }
  $dom = Page $g 'generer.html'
  $resume = Bloc $dom 'resume'
  if (-not $resume -or $resume -match '^\{"erreur"') { throw "Générateur : $resume" }
  $dest = Join-Path $Work 'generes'; New-Item -ItemType Directory -Force $dest | Out-Null
  $devGen = Bloc $dom 'dev'; $evalGen = Bloc $dom 'eval'
  [IO.File]::WriteAllText((Join-Path $dest 'scenarios-dev.json'), $devGen, $utf8)
  [IO.File]::WriteAllText((Join-Path $dest 'scenarios-eval.json'), $evalGen, $utf8)
  $evalOk = (Compact ($evalGen | ConvertFrom-Json -Depth 30)) -eq (Compact $jeux.evaluation_gele.jeu)
  $devOk = (Compact ($devGen | ConvertFrom-Json -Depth 30)) -eq (Compact $jeux.developpement.jeu)
  if (-not $evalOk) { $echecs.Add('Le générateur ne reproduit plus le jeu d''évaluation gelé') }
  $r = $resume | ConvertFrom-Json
  "Régénération sans le moteur (page sans draft-engine.js ni draft-data.js) : $($r.total) scénarios, $($r.developpement) en développement dont $($r.ajoutsDeveloppement) ajouts V2.17, $($r.evaluation) en évaluation"
  "  Jeu gelé reproduit à l'identique : $(if ($evalOk) { 'oui' } else { 'NON' }) (empreinte du texte généré $(Empreinte $evalGen))"
  "  Jeu de développement identique au fichier du dépôt : $(if ($devOk) { 'oui' } else { 'non (voir ' + $dest + ')' }) (empreinte du texte généré $(Empreinte $devGen))"
}

# ---------- 3. Banc ----------
$run = Join-Path $Work 'run'; New-Item -ItemType Directory -Force $run | Out-Null
foreach ($f in 'draft-role-sheet.js', 'draft-champions.js', 'draft-engine.js', 'draft-data.js') { Copy-Item (Join-Path $web $f) $run }
foreach ($f in 'generer-scenarios.js', 'banc-evaluation.js', 'banc-etudes.js', 'banc.html') { Copy-Item (Join-Path $tests $f) $run }
$cfg = "window.SCENARIOS_DEV = $($jeux.developpement.texte);`n"
if ($AvecEvaluation) { $cfg += "window.SCENARIOS_EVAL = $($jeux.evaluation_gele.texte);`n" }
if (-not $SansInstantane) {
  if (-not $Instantane) { $Instantane = Join-Path $tests $manifeste.instantane_reference.fichier }
  $cfg += "window.INSTANTANE_REF = $(Lire (Resolve-Path $Instantane));`n"
}
$crit = ($Criteres | ForEach-Object { "`"$_`"" }) -join ','
$cfg += "window.BANC_CONFIG = { avecEvaluation:$(if ($AvecEvaluation) { 'true' } else { 'false' }), etudes:$(if ($Etudes) { 'true' } else { 'false' }), criteresDeclares:[$crit] };"
[IO.File]::WriteAllText((Join-Path $run 'banc-config.js'), $cfg, $utf8)
$debut = Get-Date
$dom = Page $run 'banc.html'
$json = Bloc $dom 'out'
if (-not $json) { throw 'Banc : aucun résultat, erreur dans la page.' }
if ($json -match '^\{"erreur"') { throw $json }
$res = $json | ConvertFrom-Json -Depth 50
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$fRes = Join-Path $Work "resultats-$stamp.json"; $fInst = Join-Path $Work "instantane-$stamp.json"
[IO.File]::WriteAllText($fRes, ($res | ConvertTo-Json -Depth 50), $utf8)
[IO.File]::WriteAllText($fInst, (Bloc $dom 'inst'), $utf8)
if ($NouvelInstantane) { Copy-Item $fInst $NouvelInstantane -Force; "Nouvel instantané enregistré : $NouvelInstantane" }

# ---------- 4. Résumé lisible ----------
""
"Banc $($res.banc), moteur $($res.moteur), en $([int]((Get-Date) - $debut).TotalSeconds) s. $($res.avertissement)"
foreach ($p in $res.jeux.PSObject.Properties) {
  $j = $p.Value
  "[$($p.Name)] $($j.scenarios) scénarios"
  "  Rôles : argmax $($j.roles.precisionArgmax) %, MAP $($j.roles.precisionMAP) %, affectation complète $($j.roles.affectationCompleteJuste), Brier $($j.roles.brier), ECE $($j.roles.ece)"
  "  Par niveau : " + (($j.roles.parNiveau.PSObject.Properties | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ' ; ')
  "  Adversaire direct : juste $($j.adversaireDirect.juste) %, faux $($j.adversaireDirect.faux) %, abstention $($j.adversaireDirect.abstention) % ; non révélé $($j.adversaireDirect.nonRevele) ; confiance élevée interdite : $($j.adversaireDirect.confianceEleveeInterdite)"
  if ($j.flexReveleSeul) { "  Flex révélé seul : $($j.flexReveleSeul.roleDuJoueurPlausible) ; $($j.flexReveleSeul.roleDuJoueurNonPlausible)" }
  "  Invariances : " + (($j.invariance.PSObject.Properties | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ' ; ')
  "  Égalités : $($j.egalites)"
  $j.monotonie.PSObject.Properties | ForEach-Object { "  Monotonie ($($_.Name)) : $($_.Value)" }
  "  Remplacement même archétype : $($j.remplacementMemeArchetype)"
}
"Déterminisme global (deux passages) : $($res.memeVersion.inattendus) écart"
"Sensibilité aux poids (±25 %, développement, changements du 1er choix) : " + (($res.sensibilitePoids.criteres.PSObject.Properties | ForEach-Object { "$($_.Name) $($_.Value.'+25 %'.taux)/$($_.Value.'-25 %'.taux) %" }) -join ' ; ')
if ($res.contreReference) {
  foreach ($p in $res.contreReference.PSObject.Properties) {
    $d = $p.Value
    "Contre l'instantané [$($p.Name)] (critères déclarés : $(if ($Criteres.Count) { $Criteres -join ', ' } else { 'aucun' })) : $($d.scenarios) comparés, 1er choix changé $($d.top1), ordre $($d.ordre), scores $($d.scores), adversaire $($d.adversaire), rôles $($d.roles) ; attendus $($d.attendus), INATTENDUS $($d.inattendus)"
    if (@($d.absentsDeLaReference).Count) { "  Scénarios absents de la référence (non comparés) : $(@($d.absentsDeLaReference).Count)" }
    @($d.details) | ForEach-Object { "  $_" }
  }
}
if ($res.etudes) { "Études peel et map : voir $fRes (clé etudes)" }
@($res.verdict.echecs) | ForEach-Object { $echecs.Add($_) }
"Résultats : $fRes"
"Instantané produit : $fInst"
if ($echecs.Count) { ""; "ÉCHEC du banc :"; $echecs | ForEach-Object { "  - $_" }; exit 1 }
"Banc : OK"
exit 0
