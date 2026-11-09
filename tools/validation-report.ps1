# Résume le relevé de vraies drafts : validation-drafts.csv, complété à la main avec le mid réel.
# Ne conclut pas sur trop peu de drafts : voir docs/VALIDATION-REELLE.md.
param([string]$Csv = (Join-Path $env:LOCALAPPDATA 'DraftMid\validation-drafts.csv'))
if (-not (Test-Path $Csv)) { throw "Aucun relevé : $Csv" }
$rows = @(Get-Content $Csv -Encoding UTF8 | ConvertFrom-Csv -Delimiter ';')
function Wilson($k, $n) { if (-not $n) { return '-' }; $z = 1.96; $p = $k / $n; $d = 1 + $z * $z / $n; $c = ($p + $z * $z / (2 * $n)) / $d; $m = $z * [Math]::Sqrt($p * (1 - $p) / $n + $z * $z / (4 * $n * $n)) / $d; '{0:0} % (entre {1:0} et {2:0})' -f (100 * $p), (100 * ($c - $m)), (100 * ($c + $m)) }
$played = @($rows | Where-Object { $_.phase_suivante -eq 'InGame' })
$filled = @($played | Where-Object { $_.mid_reel })
"Sélections relevées : $($rows.Count), jouées : $($played.Count), mid réel renseigné : $($filled.Count)"
if ($filled.Count -lt 50) { "ATTENTION : moins de 50 drafts renseignées. Les chiffres ci-dessous sont indicatifs, sans conclusion possible." }
if (-not $filled.Count) { return }
foreach ($c in 'élevée', 'moyenne', 'faible', 'manuel') {
  $g = @($filled | Where-Object { $_.confiance -eq $c }); if (-not $g.Count) { continue }
  $named = @($g | Where-Object { $_.mid_affiche }); $ok = @($named | Where-Object { $_.mid_affiche -eq $_.mid_reel }).Count
  $listed = @($g | Where-Object { ($_.mid_affiche -eq $_.mid_reel) -or (($_.hypotheses -split ',') -contains $_.mid_reel) }).Count
  "Confiance $c : $($g.Count) drafts | nom donné $($named.Count) | juste $(Wilson $ok $named.Count) | vrai mid cité $(Wilson $listed $g.Count)"
}
$named = @($filled | Where-Object { $_.mid_affiche }); $ok = @($named | Where-Object { $_.mid_affiche -eq $_.mid_reel }).Count
"Ensemble : nom donné sur $($named.Count) drafts, juste $(Wilson $ok $named.Count), aucun nom sur $($filled.Count - $named.Count)"
"Corrections manuelles : $(@($filled | Where-Object { $_.corrections }).Count)"
"Conseil provisoire : $(@($filled | Where-Object { $_.provisoire -eq 'oui' }).Count)"
"Champion joué égal au conseil : $(@($filled | Where-Object { $_.joue -and (($_.recommande -split '=') -contains $_.joue) }).Count) sur $(@($filled | Where-Object { $_.joue }).Count) renseignés"
