# Compare les modèles A, B, C et D sur des historiques fictifs, et génère docs/MAITRISE-EXEMPLES.md.
# Lit le calcul de rôles local en lecture seule. Ne mesure aucune victoire.
param([string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-coaching\mastery'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$json = & (Join-Path $PSScriptRoot 'run-engine-audit.ps1') -Page 'mastery-compare.html' -Work $Work
$o = "$json" | ConvertFrom-Json -Depth 10
$b = New-Object Text.StringBuilder
function L($t = '') { [void]$b.AppendLine($t) }
L '# Exemples du classement personnalisé'; L
L 'Généré par `tools/run-mastery-compare.ps1`. **Tous les historiques sont fictifs.** Ces chiffres décrivent le comportement du prototype sur des drafts simulées : ils ne mesurent aucune victoire.'; L
L '- **A :** moteur actuel, sans personnalisation.'; L '- **B :** A + expérience + résultats.'; L '- **C :** B + complexité, seulement là où l''expérience manque.'; L '- **D :** C + statistiques de matchups. **Aucune source autorisée n''est branchée, donc D donne le même résultat que C.**'; L
L '## Comportement sur 2 000 drafts'; L
L 'Part en tête, égalités partagées. « Changé » compte les drafts où le premier diffère de A sans être déjà à égalité avec lui.'; L
foreach ($s in $o.stats) {
  L "### $($s.nom)"; L
  L '| Modèle | Répartition | Égalité en tête | Changé |'; L '|---|---|---|---|'
  foreach ($m in 'A', 'B', 'C', 'D') { $x = $s.models.$m; $parts = ($x.part.PSObject.Properties | Sort-Object { - [double]$_.Value } | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ', '; L "| $m | $parts | $($x.egalite) % | $($x.changeHorsEgalite) % |" }
  L
}
L '## Exemples détaillés'; L
foreach ($e in $o.examples) {
  L "### $($e.nom)"; L
  L "- **Adversaires visibles :** $(@($e.enemies) -join ', ')"
  L "- **En tête :** A $($e.tops.A), B $($e.tops.B), C $($e.tops.C), D $($e.tops.D)"; L
  L '| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |'; L '|---|---|---|---|---|---|---|---|---|'
  foreach ($r in $e.view) { $note = if ($null -eq $r.note) { 'inconnue' } else { "$($r.note)/10" }; L "| $($r.n) | $note | $($r.parties) | $($r.eff) | $($r.strat) | $($r.exp) | $($r.res) | $($r.cpx) | **$($r.tot)** |" }
  L; L '**Explications, niveau Débutant :**'; L
  foreach ($i in $e.debutant) { L "1. **$($i[0])** $($i[1])$(if ($i[2]) { " *Limite : $($i[2])*" })" }
  L; L '**Niveau Avancé, les trois premières :**'; L
  foreach ($i in $e.avance) { L "1. **$($i[0])** $($i[1])" }
  L
}
[IO.File]::WriteAllText((Join-Path $repo 'docs\MAITRISE-EXEMPLES.md'), $b.ToString(), [Text.UTF8Encoding]::new($false))
$o.stats | ForEach-Object { $s = $_; "[$($s.nom)]"; foreach ($m in 'A', 'B', 'C') { $x = $s.models.$m; "  $m " + (($x.part.PSObject.Properties | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ', ') + " | eg $($x.egalite) | change $($x.changeHorsEgalite)" } }
