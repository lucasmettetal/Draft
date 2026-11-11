# Résume le relevé de vraies drafts : validation-drafts.csv, complété à la main après la partie.
# Ne conclut pas sur trop peu de drafts : voir docs/technique/PROTOCOLE-DRAFTS-REELLES.md.
# Lecture seule : le fichier n'est jamais modifié. Rien n'est envoyé.
# La colonne historique « mid_affiche » contient l'adversaire direct affiché, quel que soit ton rôle ;
# « mid_reel » contient l'adversaire direct réel (le joueur d'en face sur ton rôle).
param([string]$Csv = (Join-Path $env:LOCALAPPDATA 'DraftMid\validation-drafts.csv'))
if (-not (Test-Path $Csv)) { throw "Aucun relevé : $Csv" }
$rows = @(Get-Content $Csv -Encoding UTF8 | ConvertFrom-Csv -Delimiter ';')
function Wilson($k, $n) { if (-not $n) { return '-' }; $z = 1.96; $p = $k / $n; $d = 1 + $z * $z / $n; $c = ($p + $z * $z / (2 * $n)) / $d; $m = $z * [Math]::Sqrt($p * (1 - $p) / $n + $z * $z / (4 * $n * $n)) / $d; '{0:0} % (entre {1:0} et {2:0})' -f (100 * $p), (100 * ($c - $m)), (100 * ($c + $m)) }
function Direct($g, $titre) {
  $named = @($g | Where-Object { $_.mid_affiche }); $ok = @($named | Where-Object { $_.mid_affiche -eq $_.mid_reel }).Count
  $listed = @($g | Where-Object { ($_.mid_affiche -eq $_.mid_reel) -or (($_.hypotheses -split ',') -contains $_.mid_reel) }).Count
  "$titre : $($g.Count) drafts | nom donné $($named.Count) | juste $(Wilson $ok $named.Count) | vrai adversaire cité $(Wilson $listed $g.Count)"
}
$played = @($rows | Where-Object { $_.phase_suivante -eq 'InGame' })
# « ? » = rôle réel douteux (échange de lanes) : la partie n'est pas comptée.
$filled = @($played | Where-Object { $_.mid_reel -and $_.mid_reel -ne '?' })
"Sélections relevées : $($rows.Count), jouées : $($played.Count), adversaire direct réel renseigné : $($filled.Count)"
if ($filled.Count -lt 50) { "ATTENTION : moins de 50 drafts renseignées. Les chiffres ci-dessous sont indicatifs, sans conclusion possible." }
if (-not $filled.Count) { return }

"--- Adversaire direct, par confiance affichée"
foreach ($c in 'élevée', 'moyenne', 'faible', 'manuel') {
  $g = @($filled | Where-Object { $_.confiance -eq $c }); if ($g.Count) { Direct $g "Confiance $c" }
}
"--- Adversaire direct, par rôle joué"
foreach ($r in 'top', 'jungle', 'mid', 'adc', 'support') {
  $g = @($filled | Where-Object { $_.role -eq $r }); if ($g.Count) { Direct $g "Rôle $r" }
}
Direct $filled 'Ensemble'
$wrong = @($filled | Where-Object { $_.mid_affiche -and $_.mid_affiche -ne $_.mid_reel })
if ($wrong.Count) {
  "--- Adversaires nommés à tort (affiché -> réel)"
  $wrong | Group-Object { "$($_.mid_affiche) -> $($_.mid_reel)" } | Sort-Object Count -Descending | Select-Object -First 15 | ForEach-Object { "  $($_.Name) : $($_.Count)" }
}

# Rôles réels des 5 adversaires, quand ils ont été notés (top=…,jungle=…,mid=…,adc=…,support=…).
$five = @($filled | Where-Object { $_.roles_reels -and $_.roles_reels -notmatch '\?' })
if ($five.Count) {
  "--- Rôles réels notés : $($five.Count) drafts"
  $seen = @{}
  foreach ($row in $five) {
    foreach ($pair in $row.roles_reels -split ',') {
      $kv = $pair -split '=', 2; if ($kv.Count -ne 2 -or -not $kv[1].Trim()) { continue }
      $champ = $kv[1].Trim(); $role = $kv[0].Trim().ToLowerInvariant()
      if (-not $seen.ContainsKey($champ)) { $seen[$champ] = @{} }
      $seen[$champ][$role] = 1 + [int]$seen[$champ][$role]
    }
  }
  $flex = @($seen.Keys | Where-Object { $seen[$_].Count -gt 1 } | Sort-Object)
  "Champions vus sur plusieurs rôles (flex observés) : $($flex.Count)"
  foreach ($champ in $flex) { "  $champ : " + (($seen[$champ].GetEnumerator() | Sort-Object Value -Descending | ForEach-Object { "$($_.Key) $($_.Value)" }) -join ', ') }
  "Le relevé n'enregistre pas encore les rôles prédits pour les 5 adversaires : la comparaison complète demande la colonne roles_predits (voir le protocole)."
}

"--- Conseil"
"Corrections manuelles : $(@($filled | Where-Object { $_.corrections }).Count)"
"Conseil provisoire : $(@($filled | Where-Object { $_.provisoire -eq 'oui' }).Count)"
"Champion joué égal au conseil : $(@($filled | Where-Object { $_.joue -and (($_.recommande -split '=') -contains $_.joue) }).Count) sur $(@($filled | Where-Object { $_.joue }).Count) renseignés"
