# Relevé anglais (I18N-2) : cherche du français resté visible dans une page ouverte en anglais.
# Entrée : le résultat de tools\extract-ui-messages.ps1 -Lang en -All (textes réellement affichés, attributs title/aria-label/placeholder).
# Un texte est signalé s'il contient un mot ou une lettre propres au français, une fois retirés les noms de champions
# et les exclusions justifiées (noms propres, codes techniques) listées ci-dessous. Lecture seule.
# Exemple : tools\i18n\check-english.ps1 -Messages messages-en.json
param(
  [Parameter(Mandatory = $true)][string]$Messages,
  [string]$Repo = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
  [int]$Show = 40
)
$ErrorActionPreference = 'Stop'
# -AsHashtable : un relevé peut contenir deux textes qui ne diffèrent que par la casse (PowerShell 7).
$r = Get-Content -Raw -Encoding UTF8 $Messages | ConvertFrom-Json -AsHashtable
if (-not $r.observed) { throw 'Relevé sans textes : lancer extract-ui-messages.ps1 avec -All.' }
# Noms de champions (fiches) : ce sont des noms propres, identiques dans les deux langues.
$champs = [regex]::Matches([IO.File]::ReadAllText((Join-Path $Repo 'web\draft-champions.js')), '"([^"]+)":"[^"]*"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object Length -Descending
# Exclusions justifiées : noms propres et codes techniques qui ne se traduisent pas.
$exclusions = @(
  @{ texte = 'Data Dragon'; raison = 'nom du service de Riot' },
  @{ texte = 'LoLalytics'; raison = 'nom du site' },
  @{ texte = 'Français'; raison = 'nom de la langue écrit dans sa langue (sélecteur, I18N-3)' },
  @{ texte = 'draft-stats.js'; raison = 'nom de fichier' }
)
# Mots et lettres propres au français. « de », « la », « le » isolés suffisent : l'anglais ne les emploie pas.
$frWords = '\b(le|la|les|des|du|de|et|ou|pas|pour|avec|sur|dans|ton|ta|tes|une|est|sont|contre|partie|parties|joueur|choix|aucun|aucune|rien|encore|déjà|réglages|sélection|adversaire|adversaires|conseil|égalité|parce|que|qui|mais|plus|moins)\b'
$frLetters = '[àâçéèêëîïôûùüÿœæ«»]'
# Textes natifs : depuis I18N-3, le programme envoie des clés (native.*) et le banc les simule ainsi : aucune exception.
$natifs = @()
$hits = @(); $nativeHits = @()
foreach ($o in $r.observed) {
  $text = [string]$o[0]; $s = $text
  if ($natifs -contains $text) { $nativeHits += $text; continue }
  foreach ($e in $exclusions) { $s = $s.Replace($e.texte, ' ') }
  foreach ($c in $champs) { if ($s.Contains($c)) { $s = $s.Replace($c, ' ') } }
  if ($s -cmatch $frLetters -or $s -match $frWords) { $hits += [pscustomobject]@{ Texte = $text; Fois = $o[1]; Ou = ($o[2] -join ', ') } }
}
$missing = @(); if ($r.i18nMissing) { $missing = @($r.i18nMissing.Keys) }
"Langue du relevé : $($r.lang) ; textes distincts : $(@($r.observed).Count)"
"Clés ou variables absentes (repli sur le français) : $($missing.Count)"
$missing | Select-Object -First $Show | ForEach-Object { "  $_" }
"Textes avec du français visible : $($hits.Count)"
$hits | Sort-Object Fois -Descending | Select-Object -First $Show | ForEach-Object { "  [$($_.Fois)] $($_.Texte.Substring(0, [Math]::Min(160, $_.Texte.Length)))  <= $($_.Ou)" }
"Textes natifs encore en français (I18N-3) : $($nativeHits.Count)" + $(if ($nativeHits.Count) { ' : ' + ($nativeHits -join ' | ') } else { '' })
"Exclusions appliquées : " + (($exclusions | ForEach-Object { "$($_.texte) ($($_.raison))" }) -join ' ; ')
if ($r.lang -ne 'en') { 'Ce relevé n''est pas en anglais.'; exit 1 }
if ($missing.Count -or $hits.Count) { exit 1 } else { exit 0 }
