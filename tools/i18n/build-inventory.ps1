# Construit l'inventaire des chaînes visibles pour l'internationalisation (docs/i18n/inventaire.json et INVENTAIRE.md).
# Ne refait pas l'audit : reprend le catalogue de la PR #9 (bloc « catalog » de web/draft-messages-tests.html)
# et les textes natifs N-01..N-26 de docs/moteur/AUDIT-VOCABULAIRE.md § 8, puis complète les textes natifs (src/*.cs)
# avec le détecteur (tools/i18n/detect-hardcoded.ps1). Propose une clé et une catégorie pour chaque entrée.
# Les clés sont des propositions : I18N-1 peut les renommer, mais une fois livrées elles ne changent plus.
# Lecture seule sur le code. Écrit seulement les deux fichiers de sortie.
# Exemple : tools\i18n\build-inventory.ps1
param(
  [string]$Root = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
  [string]$OutJson = '',
  [string]$OutMd = ''
)
$ErrorActionPreference = 'Stop'
if (-not $OutJson) { $OutJson = Join-Path $Root 'docs\i18n\inventaire.json' }
if (-not $OutMd) { $OutMd = Join-Path $Root 'docs\i18n\INVENTAIRE.md' }
$utf8 = [Text.UTF8Encoding]::new($false)

# ---------- Outils ----------
function Fold([string]$s) {
  $d = $s.Normalize([Text.NormalizationForm]::FormD)
  $sb = [Text.StringBuilder]::new()
  foreach ($ch in $d.ToCharArray()) { if ([Globalization.CharUnicodeInfo]::GetUnicodeCategory($ch) -ne 'NonSpacingMark') { [void]$sb.Append($ch) } }
  return $sb.ToString().Replace('œ', 'oe').Replace('Œ', 'Oe')
}
$stop = @('les', 'des', 'une', 'pour', 'sur', 'avec', 'dans', 'par', 'est', 'pas', 'que', 'qui', 'ton', 'tes', 'ses', 'son', 'leur', 'leurs', 'aux', 'mais', 'sans', 'plus', 'tres', 'non', 'aria', 'title', 'titre', 'the')
function SlugOr([string]$s, [string]$fallback, [int]$n = 3) { $r = Slug $s $n; if ($r -eq 'texte') { $r = Slug $fallback 3 }; return $r }
function Slug([string]$s, [int]$n = 3) {
  $w = [regex]::Matches((Fold $s), '[A-Za-z]{2,}') | ForEach-Object { $_.Value.ToLowerInvariant() } | Where-Object { $_.Length -ge 3 -and $stop -notcontains $_ } | Select-Object -First $n
  $w = @($w)
  if (-not $w.Count) { return 'texte' }
  $out = $w[0]
  for ($i = 1; $i -lt $w.Count; $i++) { $out += $w[$i].Substring(0, 1).ToUpperInvariant() + $w[$i].Substring(1) }
  return $out
}
# Texte lisible d'un gabarit du catalogue (y compris « re: »), pour les clés et la recherche de ligne.
function Readable([string]$tpl) {
  $t = $tpl
  if ($t.StartsWith('re:')) {
    $t = $t.Substring(3) -replace '\\d\+?', 'N' -replace '\\s', ' ' -replace '\\(.)', '$1' -replace '\(\?:', '(' -replace '[\^\$]', '' -replace '\.\+\??|\.\*', '…'
    $t = $t -replace '[()]', '' -replace '\?', ''
  }
  return $t
}
function Fragments([string]$tpl) {
  if ($tpl.StartsWith('re:')) {
    $parts = [regex]::Split($tpl.Substring(3), '\\[dswbDSWB]|[\^\$()|?*+\[\]{}]|\\') | Where-Object { $_ }
  } else {
    $parts = [regex]::Split($tpl, '\{[^}]*\}|\[\[|\]\]')
  }
  return @($parts | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -ge 5 } | Sort-Object Length -Descending)
}
# Ligne actuelle d'un texte, la plus proche de la ligne connue.
$fileLines = @{}
function Locate([string]$rel, [string[]]$frags, [int]$hint) {
  if (-not $fileLines.ContainsKey($rel)) {
    $p = Join-Path $Root $rel
    $fileLines[$rel] = if (Test-Path $p) { [IO.File]::ReadAllLines($p) } else { @() }
  }
  $lines = $fileLines[$rel]
  foreach ($f in $frags) {
    $best = $null
    for ($i = 0; $i -lt $lines.Count; $i++) {
      if ($lines[$i].Contains($f)) { if ($null -eq $best -or [math]::Abs($i + 1 - $hint) -lt [math]::Abs($best - $hint)) { $best = $i + 1 } }
    }
    if ($best) { return $best }
  }
  return $null
}

$securityRx = "clé|jamais enregistr|n'est pas enregistr|Rien n'est envoyé|autoris|Riot n'a pas confirmé|non approuvé|signature|sign[ée]e|empreinte|Rien n'a été (modifié|changé)|rien n'a été modifié|J'accepte|Aucun mot de passe|administrateur|droits|consent|vérifi(er|cation) de la nouvelle|à tes risques|n'est pas un (import|journal) DRAFT|site inconnu"
$errorRx = "illisible|invalide|refus|impossible|échou|échec|erreur|ne peut pas|introuvable|n'est pas un|mal formé|interrompu|n'a pas pu|bloqu|ne répond pas|inattendu|incomplet|n'a pas démarré"

# ---------- 1. Page : catalogue de la PR #9 ----------
$catText = [IO.File]::ReadAllText((Join-Path $Root 'web\draft-messages-tests.html'))
$block = [regex]::Match($catText, '<script type="text/plain" id="catalog">([\s\S]*?)</script>').Groups[1].Value
$fileOf = @{ 'index.html' = 'web/index.html'; 'draft-engine.js' = 'web/draft-engine.js'; 'draft-coaching.js' = 'web/draft-coaching.js'; 'draft-mastery.js' = 'web/draft-mastery.js'; 'draft-data.js' = 'web/draft-data.js'; 'draft-profiles.js' = 'web/draft-profiles.js' }

# Zone de la page selon le contexte (premier motif qui correspond). Zone -> catégorie plus bas.
$uiZones = @(
  @('^Erreur de démarrage', 'error.startup'), @('^Tes parties', 'stats.personal'), @('^Bilan V/D', 'stats.record'),
  @('^Changement de conseil|^Bandeau de changement', 'advice.change'), @('^Action / notice|^Notice', 'draft.notice'),
  @('^Champ de recherche|^Recherche sans', 'draft.search'), @('^Infobulle « Je joue »|^Infobulle « Choix serré »', 'advice.help'),
  @('^Infobulle ligne adversaire|^Étiquette de l.adversaire|^Ligne adversaire|^Rôle adverse|^Bouton « placer|^Bouton retirer|^Rôle visé|^Rôle vide', 'enemy.slot'),
  @('^Profil d.un adversaire|^Jauge|^Profil adverse|^Titre colonne|^Aide|^Titre des jauges', 'enemy.profile'),
  @('^Illustration', 'advice.art'), @('^Lecteur d.écran|^Égalité', 'advice.tie'), @('^Conseil pendant|^Lien vers l.onglet', 'advice.coach'),
  @('^Infobulle « Le plus joué »|^Ligne « Le plus joué »|^Réglage du niveau|^Onglet Conseils', 'coachTab'),
  @('^Zone de recommandation|^Aucun pick|^Badge|^Alternative|^Aucune raison|^Risque principal|^Repli|^Picks à éviter|^Bilan personnel|^Pick sans frontline|^Bouton principal', 'advice.card'),
  @('^Étiquette du pool|^Titre du pool|^Sous-titre du pool|^Pool', 'pool'), @('^Résultat de partie', 'journal.result'),
  @('^Titre du plan|^Condition de victoire|^Plan|^Phases|^Dernière partie', 'plan'), @('^Matrice|^Analyse', 'analysis'),
  @('^Comparer', 'compare'), @('^Duel', 'duel'), @('^Statistiques|^Barre de victoires', 'stats'), @('^Journal', 'journal'),
  @('^Règles', 'rules'), @('^Mention légale', 'legal'), @('^Vues d.Analyse', 'nav.analysisView'), @('^Onglets', 'nav.tab'),
  @('^Barre mobile', 'nav.mobile'), @('^Aucun profil|^Rôle sans champion|^Tous les picks', 'advice.empty'),
  @('^Sélecteur de rôle|^Rôle attribué', 'role.switch'), @('^Logo|^Marque', 'brand'), @('^Barre des panneaux', 'nav.panels'),
  @('^Panneau sans profil|^Panneau droit|^Bouton vers l.analyse', 'panel'), @('^Lecture du client', 'settings.lcu'),
  @('^Export des profils|^Import|^Annulation de l.import|^Boutons des profils', 'settings.profiles'),
  @('^Réglages avancés', 'settings.advanced'), @('^Réglages', 'settings'), @('^Barre du haut', 'nav.top'), @('^Noms des rôles', 'role.name'),
  @('^Statistiques : nom', 'stats'), @('^Simulateur', 'draft.simulator')
)
function ZoneCategory([string]$zone) {
  switch -Regex ($zone) {
    '^(enemy|role)\.' { return 'rôles' }
    '^(settings|data|profiles)' { return 'réglages' }
    '^error' { return 'erreurs' }
    '^(nav|brand|legal|draft|stats|journal)' { return 'navigation' }
    default { return 'conseil/moteur' }
  }
}
$levelWord = @{ 'titre' = 'title'; 'débutant' = 'level1'; 'intermédiaire' = 'level2'; 'avancé' = 'level3'; 'limite' = 'limit'; 'risque' = 'risk' }

$keys = [Collections.Generic.HashSet[string]]::new()
function Unique([string]$k) { $b = $k; $i = 2; while (-not $keys.Add($k)) { $k = "$b$i"; $i++ }; return $k }

$entries = [Collections.Generic.List[object]]::new()
$seenIds = @{}
foreach ($raw in ($block -split "`n")) {
  $l = $raw.Trim()
  if (-not $l -or $l.StartsWith('#')) { continue }
  $p = $l -split ' \| '
  if ($p.Count -lt 4 -or $p[0] -notmatch '^[A-Z]+-\d+') { continue }
  $id = $p[0]; $loc = $p[1]; $ctx = $p[2]; $tpl = $p[3]
  if ($p.Count -gt 4) { $tpl = $tpl + ' | ' + (($p[4..($p.Count - 1)]) -join ' | ') }
  if ($seenIds.ContainsKey($id)) { $seenIds[$id]++; $id = "$id-bis" } else { $seenIds[$id] = 1 }
  $fname = ($loc -split '[:,]')[0].Trim()
  $rel = $fileOf[$fname]
  $hint = 0; if ($loc -match ':(\d+)') { $hint = [int]$Matches[1] }
  $prefix = ($id -split '-')[0]
  $readable = Readable $tpl
  $ctxHead = ($ctx -split ',')[0].Trim()
  $ctxRest = (($ctx -split ',', 2) | Select-Object -Skip 1) -join ''
  switch ($prefix) {
    'UI' {
      $zone = 'ui'
      foreach ($z in $uiZones) { if ($ctx -match $z[0]) { $zone = $z[1]; break } }
      $key = "$zone." + (SlugOr $readable $ctx 3)
    }
    'EN' {
      $sub = switch -Regex ($ctx) {
        '^Règles, source' { 'criteria.source' } '^Critère|^Noms des critères|^Analyse, critère' { 'criteria' } '^Raison du ban|ban' { 'ban' }
        '^Raison' { 'reason' } '^Condition de victoire' { 'winCondition' } '^Plan' { 'plan' } '^Danger' { 'danger' }
        '^Analyse' { 'position' } 'whenBetter' { 'whenBetter' } default { Slug $ctxHead 2 }
      }
      $key = "engine.$sub." + (Slug ($ctx -replace '^[^,]*,?', '') 2) + '.' + (Slug $readable 3)
      $key = $key -replace '\.texte\.', '.'
      $zone = 'engine'
    }
    'CO' {
      if ($ctx -match '^Principe débutant : (.+)$') { $key = 'coach.principle.' + (Slug $Matches[1] 2) + '.' + (Slug $readable 2) }
      elseif ($ctxHead -match '^[a-z\-]+\.[a-z\-]+') {
        $rule = ($ctxHead -replace '-(\w)', { $_.Groups[1].Value.ToUpperInvariant() })
        $slot = 'text'
        foreach ($w in $levelWord.Keys) { if ($ctxRest -match "\b$w") { $slot = $levelWord[$w]; break } }
        $key = "coach.$rule.$slot." + (Slug $readable 2)
      } else { $key = 'coach.' + (Slug $ctxHead 2) + '.' + (Slug $readable 3) }
      $zone = 'coach'
    }
    'MA' { $key = 'mastery.' + (Slug $ctxHead 2) + '.' + (Slug $readable 3); $zone = 'mastery' }
    'DA' { $key = 'data.' + (Slug $ctxHead 2) + '.' + (Slug $readable 3); $zone = 'data' }
    'PR' { $key = 'profiles.' + (Slug $ctxHead 2) + '.' + (Slug $readable 3); $zone = 'profiles' }
  }
  $cat = ZoneCategory $zone
  if ($ctx -match "^Noms des rôles|rôle" -and $cat -eq 'navigation') { $cat = 'rôles' }
  $sec = ($readable -match $securityRx) -or ($ctx -match 'Mention légale|avertissement Riot')
  $err = $readable -match $errorRx
  if ($err -and $cat -ne 'rôles') { $cat = 'erreurs' }
  if ($sec) { $cat = 'sécurité' }
  # Libellés regroupés dans une seule entrée (alternance simple) : une sous-clé par libellé.
  $sub = @()
  if ($tpl -match '^re:\^\(([^()]+)\)\$$') { $sub = @($Matches[1] -split '\|' | ForEach-Object { (Slug ($_ -replace '\\(.)', '$1') 3) }) }
  $line = if ($rel) { Locate $rel (Fragments $tpl) $hint } else { $null }
  $entries.Add([ordered]@{
    id = $id; origine = 'page'; fichier = $rel; emplacementCatalogue = $loc; ligneActuelle = $line; contexte = $ctx; gabarit = $tpl
    cle = (Unique $key); sousCles = $sub; categorie = $cat; securite = [bool]$sec; erreur = [bool]$err; nonAffiche = [bool]($ctx -match '\[non affiché\]')
  })
}

# ---------- 2. Natif : N-01..N-26 de l'audit, complétés par le détecteur ----------
$audit = [IO.File]::ReadAllText((Join-Path $Root 'docs\moteur\AUDIT-VOCABULAIRE.md'))
$nRows = @([regex]::Matches($audit, '(?m)^\| (N-\d\d) \| ([^|]+) \| ([^|]+) \|') | ForEach-Object { [pscustomobject]@{ id = $_.Groups[1].Value; loc = $_.Groups[2].Value.Trim(); text = $_.Groups[3].Value.Trim() } })

$tmp = Join-Path ([IO.Path]::GetTempPath()) ('draft-i18n-detect-' + [guid]::NewGuid().ToString('N') + '.json')
& (Join-Path $PSScriptRoot 'detect-hardcoded.ps1') -Root $Root -Json $tmp | Out-Null
$det = Get-Content -Raw -Encoding UTF8 $tmp | ConvertFrom-Json
Remove-Item $tmp

$areaOfClass = @{
  'PhaseText' = 'phase'; 'Layout' = 'layout'; 'Installer' = 'install'; 'Program' = 'app'; 'RuntimeHelp' = 'app'
  'AppUpdater' = 'update.error'; 'GitHubUpdateSource' = 'update.error'; 'LocalUpdateSource' = 'update.error'
  'HistoryImporter' = 'history'; 'HistoryImportDialog' = 'history.dialog'
  'LcuDiagnostic' = 'lcuDiag'; 'ClientProcess' = 'lcu.reason'; 'LcuFactory' = 'lcu.reason'; 'LockfileInfo' = 'lcu'; 'LocalHttpsTransport' = 'lcu'
  'RoleRatesUpdater' = 'riot'; 'MatchupStatsUpdater' = 'riot'; 'RiotRegion' = 'riot.region'; 'RiotKeyDialog' = 'riot.dialog'; 'RiotWebFetcher' = 'riot'; 'RiotCrawler' = 'riot'
  'PanelVisibilityController' = 'detection'
}
$areaOfMember = @{
  'CompanionApp' = 'menu'; 'RefreshUi' = 'menu'; 'LcuStatus' = 'lcu.status'; 'RunLcuDiagnostic' = 'lcuDiag'; 'ImportHistory' = 'history.status'; 'RunHistoryImport' = 'history.status'; 'ShowLcuDiagnostic' = 'lcuDiag'; 'CreateEnv' = 'app'; 'ToggleHistoryImport' = 'history.status'
  'ToggleRiotUpdate' = 'riot.status'; 'Teach' = 'balloon'; 'RunUpdate' = 'update'; 'CheckForUpdate' = 'update'; 'UpdateBlocked' = 'update'; 'AnnounceRolledBack' = 'update'; 'AnnounceUpdated' = 'update'
  'ImportStats' = 'dialog'; 'Start' = 'app'; 'Main' = 'app'
}
$groups = $det.chaines | Where-Object { $_.fichier -like 'src/*' } | Group-Object { $_.fichier + ':' + $_.ligne }
$native = foreach ($g in $groups) {
  $first = $g.Group[0]
  $cls = @($first.portee -split '/' | Where-Object { $_ })
  $last = if ($cls.Count) { $cls[-1] } else { '' }
  $area = $null
  if ($last -eq 'CompanionApp' -and $first.membre -and $areaOfMember.ContainsKey($first.membre)) { $area = $areaOfMember[$first.membre] }
  if (-not $area -and $areaOfClass.ContainsKey($last)) { $area = $areaOfClass[$last] }
  if (-not $area -and $first.membre -and $areaOfMember.ContainsKey($first.membre)) { $area = $areaOfMember[$first.membre] }
  if (-not $area) { $area = 'app' }
  $frags = @($g.Group | ForEach-Object { $_.texte })
  $text = $frags -join ' {…} '
  [pscustomobject]@{ fichier = $first.fichier; ligne = [int]$first.ligne; classe = $last; membre = $first.membre; area = $area; fragments = $frags; texte = $text }
}
$native = @($native | Sort-Object fichier, ligne)
$n = 0
foreach ($e in $native) {
  $n++
  $audId = $null
  foreach ($r in $nRows) {
    foreach ($f in $e.fragments) {
      $core = ($f -replace '\{\}', '').Trim(' ', ':', '.', '(', ')')
      if (($core.Length -ge 8 -and $r.text.Contains($core)) -or $core -eq $r.text) { $audId = $r.id; break }
    }
    if (-not $audId) {
      foreach ($piece in ($r.text -split ' · ')) {
        $pc = ($piece -replace '\{[^}]*\}.*$', '' -replace '….*$', '').Trim(' ', '.', '«', '»')
        if ($pc.Length -ge 10 -and $e.texte.Contains($pc)) { $audId = $r.id; break }
      }
    }
    if ($audId) { break }
  }
  $inPage = $e.area -match '^(phase|layout|history\.status|riot\.status|update|update\.error|lcu\.status|detection)$'
  $sec = $e.texte -match $securityRx
  $err = $e.texte -match $errorRx
  $entries.Add([ordered]@{
    id = ('NA-{0:000}' -f $n); origine = 'natif'; fichier = $e.fichier; emplacementCatalogue = $null; ligneActuelle = $e.ligne
    contexte = ($e.classe + $(if ($e.membre) { '.' + $e.membre } else { '' })); gabarit = $e.texte; fragments = $e.fragments
    cle = (Unique ("native.$($e.area)." + (Slug $e.texte 3))); sousCles = @(); categorie = 'natif'; securite = [bool]$sec; erreur = [bool]$err
    affichage = $(if ($inPage) { 'page (état transmis par HostJson), parfois aussi en bulle' } else { 'fenêtre Windows (menu, bulle, boîte, dialogue, diagnostic)' })
    audit = $audId; nonAffiche = $false
  })
}

# ---------- 3. Contrôle : chaînes de la page vues par le détecteur sans entrée du catalogue ----------
$pageEntries = @($entries | Where-Object { $_.origine -eq 'page' })
$rx = foreach ($e in $pageEntries) {
  $t = $e.gabarit
  try {
    if ($t -match ' \| re:(.+)$') { [regex]::new($Matches[1]) }
    elseif ($t.StartsWith('re:')) { [regex]::new($t.Substring(3)) }
    else { [regex]::new(([regex]::Escape(($t -replace '\{[^}]*\}', '\u0001' -replace '\[\[|\]\]', ''))).Replace('\u0001', '.+?')) }
  } catch { $null }
}
$rx = @($rx | Where-Object { $_ })
$allTpl = ($pageEntries | ForEach-Object { $_.gabarit }) -join "`n"
$unmatched = [Collections.Generic.List[object]]::new()
$webCand = @($det.chaines | Where-Object { $_.fichier -like 'web/*' })
foreach ($c in $webCand) {
  $sample = $c.texte -replace '\{\}', 'Ahri'
  $ok = $false
  foreach ($r in $rx) { if ($r.IsMatch($sample)) { $ok = $true; break } }
  if (-not $ok) {
    foreach ($f in (($c.texte -split '\{\}') | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -ge 4 })) { if ($allTpl.Contains($f)) { $ok = $true; break } }
  }
  if (-not $ok) { $unmatched.Add([ordered]@{ fichier = $c.fichier; ligne = $c.ligne; texte = $c.texte }) }
}

# ---------- 4. Sorties ----------
$byCat = [ordered]@{}; $byFile = [ordered]@{}
foreach ($e in $entries) { $byCat[$e.categorie] = 1 + [int]$byCat[$e.categorie]; $byFile[[string]$e.fichier] = 1 + [int]$byFile[[string]$e.fichier] }
$resume = [ordered]@{
  page = $pageEntries.Count; natif = @($entries | Where-Object { $_.origine -eq 'natif' }).Count; total = $entries.Count
  securite = @($entries | Where-Object { $_.securite }).Count; nativeVuesDansLaPage = @($entries | Where-Object { $_.affichage -like 'page*' }).Count
  natifsRattachesAudit = @($entries | Where-Object { $_.audit }).Count; groupesAuditN = $nRows.Count
  idsEnDouble = @($seenIds.Keys | Where-Object { $seenIds[$_] -gt 1 })
  pageSansLigneActuelle = @($pageEntries | Where-Object { -not $_.ligneActuelle }).Count
  candidatsPageDetecteur = $webCand.Count; candidatsPageHorsCatalogue = $unmatched.Count
  parCategorie = $byCat; parFichier = $byFile
}
$doc = [ordered]@{
  _lisezmoi = "Inventaire des chaînes visibles de DRAFT pour l'internationalisation. Généré par tools/i18n/build-inventory.ps1 ; ne pas éditer à la main. Sources : catalogue de la PR #9 (web/draft-messages-tests.html), docs/moteur/AUDIT-VOCABULAIRE.md § 8, détecteur tools/i18n/detect-hardcoded.ps1."
  base = (git -C $Root rev-parse --short HEAD 2>$null)
  resume = $resume
  entrees = $entries
  candidatsPageHorsCatalogue = $unmatched
}
[IO.File]::WriteAllText($OutJson, ($doc | ConvertTo-Json -Depth 6), $utf8)

function Cell([string]$s, [int]$max = 0) {
  if ($null -eq $s) { return '' }
  $s = $s.Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ')
  if ($max -and $s.Length -gt $max) { $s = $s.Substring(0, $max) + '…' }
  return $s
}
$md = [Text.StringBuilder]::new()
$null = $md.AppendLine('# Inventaire des chaînes visibles (FR → clés)')
$null = $md.AppendLine()
$null = $md.AppendLine("Fichier généré par ``tools/i18n/build-inventory.ps1`` (base ``$($doc.base)``). Ne pas éditer : corriger les règles du script, puis le relancer. Version machine : [``inventaire.json``](inventaire.json).")
$null = $md.AppendLine()
$null = $md.AppendLine("L'audit n'est pas refait. La page reprend les $($resume.page) entrées du catalogue de la PR #9 (``web/draft-messages-tests.html``, bloc ``catalog``). Le natif complète les groupes N-01 à N-26 de ``docs/moteur/AUDIT-VOCABULAIRE.md`` § 8 par une lecture outillée de ``src/*.cs`` (détecteur), une entrée par ligne de code.")
$null = $md.AppendLine()
$null = $md.AppendLine('## Résumé')
$null = $md.AppendLine()
$null = $md.AppendLine('| Mesure | Valeur |')
$null = $md.AppendLine('|---|---|')
$null = $md.AppendLine("| Entrées de la page (catalogue PR #9) | **$($resume.page)** |")
$null = $md.AppendLine("| Entrées natives (src/*.cs, une par ligne de code) | **$($resume.natif)** |")
$null = $md.AppendLine("| dont rattachées à un groupe N-xx de l'audit | $($resume.natifsRattachesAudit) (sur $($resume.groupesAuditN) groupes) |")
$null = $md.AppendLine("| dont affichées dans la page (état transmis par le programme) | $($resume.nativeVuesDansLaPage) |")
$null = $md.AppendLine("| Entrées marquées « sécurité » (sens à garder mot pour mot) | $($resume.securite) |")
$null = $md.AppendLine("| Entrées de la page sans ligne retrouvée dans le code actuel | $($resume.pageSansLigneActuelle) |")
$null = $md.AppendLine("| Identifiants en double dans le catalogue | $(if ($resume.idsEnDouble.Count) { ($resume.idsEnDouble -join ', ') + ' (le second reçoit le suffixe -bis)' } else { 'aucun' }) |")
$null = $md.AppendLine("| Chaînes de la page vues par le détecteur | $($resume.candidatsPageDetecteur) |")
$null = $md.AppendLine("| dont sans gabarit correspondant dans le catalogue | $($resume.candidatsPageHorsCatalogue) (à vérifier en I18N-1, liste dans le JSON) |")
$null = $md.AppendLine()
$null = $md.AppendLine('| Catégorie | Entrées |')
$null = $md.AppendLine('|---|---|')
foreach ($k in $byCat.Keys) { $null = $md.AppendLine("| $k | $($byCat[$k]) |") }
$null = $md.AppendLine()
$null = $md.AppendLine('| Fichier | Entrées |')
$null = $md.AppendLine('|---|---|')
foreach ($k in $byFile.Keys) { $null = $md.AppendLine("| ``$k`` | $($byFile[$k]) |") }
$null = $md.AppendLine()
$null = $md.AppendLine('## Règles de nommage des clés')
$null = $md.AppendLine()
$null = $md.AppendLine('- Forme : `zone.sousZone.libellé`, en ASCII, `camelCase` pour chaque segment. Le dernier segment vient des premiers mots du texte français de référence (sans accents). Il identifie le texte ; il ne le traduit pas.')
$null = $md.AppendLine('- Zones de la page : `nav`, `brand`, `legal`, `draft` (saisie, notices, simulateur), `enemy` (lignes et profil adverses), `role`, `advice` (conseil affiché, égalités, carte), `pool`, `plan`, `analysis`, `compare`, `duel`, `coachTab`, `stats`, `journal`, `rules`, `panel`, `settings`, `error`. Moteur : `engine`, `coach` (identifiant de règle repris de `draft-coaching.js`, puis `title`, `level1`, `level2`, `level3`, `limit`, `risk`), `mastery`, `data`, `profiles`.')
$null = $md.AppendLine('- Natif : `native.<zone>.libellé` ; zones `menu`, `balloon`, `dialog`, `app`, `install`, `phase`, `layout`, `detection`, `lcu`, `lcu.status`, `lcu.reason`, `lcuDiag`, `history`, `history.dialog`, `history.status`, `riot`, `riot.dialog`, `riot.region`, `riot.status`, `update`, `update.error`.')
$null = $md.AppendLine('- Une entrée du catalogue qui regroupe plusieurs libellés (alternance `^(A|B|C)$`) reçoit une clé de groupe et des sous-clés (colonne « sous-clés » du JSON).')
$null = $md.AppendLine('- Les clés proposées ici sont stables dès leur livraison en I18N-1 : un texte qui change garde sa clé ; une clé ne se réutilise jamais pour un autre sens.')
$null = $md.AppendLine()
$null = $md.AppendLine('## Catégories')
$null = $md.AppendLine()
$null = $md.AppendLine('| Catégorie | Contenu |')
$null = $md.AppendLine('|---|---|')
$null = $md.AppendLine('| navigation | Structure et saisie : onglets, panneaux, barre du haut, saisie de la draft, notices, journal, statistiques |')
$null = $md.AppendLine('| conseil/moteur | Recommandation, égalités, plan, analyse, comparaison, duel, règles, raisons du moteur, conseils, maîtrise |')
$null = $md.AppendLine('| rôles | Lignes adverses, adversaire direct, noms de rôles, rôle attribué |')
$null = $md.AppendLine('| réglages | Réglages, sources de données, profils |')
$null = $md.AppendLine('| erreurs | Refus, échecs, fichiers illisibles (texte qui contient un mot d''erreur) |')
$null = $md.AppendLine('| natif | Tout texte écrit par le programme Windows (src/*.cs) ; les drapeaux « sécurité » et « erreur » restent dans le JSON |')
$null = $md.AppendLine('| sécurité | Clé jamais enregistrée, rien n''est envoyé, rien n''a été modifié, signature et empreinte, consentement, mention Riot : sens à garder mot pour mot (voir STRATEGIE.md § 5) |')
$null = $md.AppendLine()
$null = $md.AppendLine('## Textes natifs (complément de l''audit § 8)')
$null = $md.AppendLine()
$null = $md.AppendLine('| ID | Fichier:ligne | Classe.méthode | Audit | Affichage | Clé proposée | Texte (fragments séparés par {…}) |')
$null = $md.AppendLine('|---|---|---|---|---|---|---|')
foreach ($e in ($entries | Where-Object { $_.origine -eq 'natif' })) {
  $aff = if ($e.affichage -like 'page*') { 'page' } else { 'Windows' }
  $flag = @(); if ($e.securite) { $flag += 'S' }; if ($e.erreur) { $flag += 'E' }
  $null = $md.AppendLine("| $($e.id)$(if ($flag.Count) { ' (' + ($flag -join ',') + ')' }) | ``$($e.fichier):$($e.ligneActuelle)`` | $(Cell $e.contexte) | $($e.audit) | $aff | ``$($e.cle)`` | $(Cell $e.gabarit 140) |")
}
$null = $md.AppendLine()
$null = $md.AppendLine('S : sécurité, E : erreur. « page » : le texte est envoyé à la page (statut d''import, de calcul, de mise à jour, phase, placement) et doit donc exister dans les deux langues du même catalogue.')
$null = $md.AppendLine()
$null = $md.AppendLine('## Page (catalogue de la PR #9)')
$null = $md.AppendLine()
$null = $md.AppendLine('Ligne : ligne actuelle retrouvée dans le code (le catalogue date de `78519fb`, DRAFT 2.16.1).')
$null = $md.AppendLine()
$null = $md.AppendLine('| ID | Ligne | Catégorie | Clé proposée | Gabarit |')
$null = $md.AppendLine('|---|---|---|---|---|')
foreach ($e in $pageEntries) {
  $where = if ($e.ligneActuelle) { "$(($e.fichier -replace '^web/', '')):$($e.ligneActuelle)" } else { "$($e.emplacementCatalogue) (non retrouvée)" }
  $null = $md.AppendLine("| $($e.id) | ``$(Cell $where)`` | $($e.categorie) | ``$($e.cle)`` | $(Cell $e.gabarit 110) |")
}
[IO.File]::WriteAllText($OutMd, $md.ToString(), $utf8)
"Inventaire : $($resume.page) entrées de la page, $($resume.natif) entrées natives ($($resume.natifsRattachesAudit) rattachées à N-xx), $($resume.securite) marquées sécurité."
"Détecteur, page : $($resume.candidatsPageDetecteur) chaînes, dont $($resume.candidatsPageHorsCatalogue) sans gabarit du catalogue."
"Sorties : $OutJson ; $OutMd"
