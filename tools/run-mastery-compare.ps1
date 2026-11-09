# Compare A, T et l'ancien B sur des historiques fictifs, et génère docs/MAITRISE-EXEMPLES.md.
# Lit le calcul de rôles local en lecture seule. Ne mesure aucune victoire.
param([string]$Work = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist-coaching\mastery'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$json = & (Join-Path $PSScriptRoot 'run-engine-audit.ps1') -Page 'mastery-compare.html' -Work $Work
$o = "$json" | ConvertFrom-Json -Depth 10
$b = New-Object Text.StringBuilder
function L($t = '') { [void]$b.AppendLine($t) }
L '# Exemples de recommandations personnalisées'; L
L 'Généré par `tools/run-mastery-compare.ps1`. **Tous les historiques sont fictifs**, et les conseils restent à relire par un joueur.'; L
L 'Le classement présenté reste toujours celui du moteur. Le champion le plus joué est affiché à côté, sans décider à ta place.'; L
foreach ($e in $o.examples) {
  L "## $($e.nom)"; L
  L "**Contexte :** $($e.role), pool $(@($e.pool) -join ', '). Adversaires visibles : $(@($e.enemies) -join ', ')."; L
  if ($e.strategic) { L "- **$($e.strategic.label) :** $(@($e.strategic.champions) -join ', ')" }
  if ($e.familiar) { L "- **Le plus joué :** $($e.familiar.champion), $($e.familiar.games) parties" } else { L '- **Le plus joué :** aucun, données insuffisantes' }
  L "- **Compromis :** $($e.compromise)"; L
  foreach ($s in @(@('Théorie', $e.theory), @('Expérience', $e.experience), @('Incertitude', $e.uncertainty))) {
    L "**$($s[0]) :**"; L
    if (@($s[1]).Count -eq 0) { L '- Rien à signaler.' }
    foreach ($i in @($s[1])) { L "- **$($i[0]).** $($i[1])$(if ($i[2]) { " *Limite : $($i[2])*" })" }
    L
  }
}
L '## Effet sur le classement'; L
L 'Sur 2 000 drafts complètes simulées. « Hors égalité » compte les drafts où le premier change sans être à égalité avec celui du moteur. Le modèle B n''existe plus que pour cette comparaison.'; L
foreach ($s in $o.stats) {
  L "### $($s.nom)"; L
  L '| Modèle | Part en tête | Nom en tête changé | Hors égalité |'; L '|---|---|---|---|'
  foreach ($m in 'A', 'T', 'B') { $x = $s.models.$m; $parts = ($x.part.PSObject.Properties | Sort-Object { - [double]$_.Value } | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ', '; L "| $m | $parts | $($x.nomChange) % | $($x.horsEgalite) % |" }
  L
}
[IO.File]::WriteAllText((Join-Path $repo 'docs\MAITRISE-EXEMPLES.md'), $b.ToString(), [Text.UTF8Encoding]::new($false))
$o.stats | ForEach-Object { $s = $_; "[$($s.nom)]"; foreach ($m in 'A', 'T', 'B') { $x = $s.models.$m; "  $m " + (($x.part.PSObject.Properties | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ', ') + " | nom change $($x.nomChange) | hors egalite $($x.horsEgalite)" } }
