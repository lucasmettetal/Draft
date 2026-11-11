# Vérifie des catalogues de traduction (format : docs/i18n/FORMAT-CATALOGUE.md ; tests : docs/i18n/TESTS.md).
# Contrôles : forme « window.DRAFT_I18N_XX = {objet JSON}; » précédée seulement de commentaires « // », une clé par ligne,
# pas de clé en double, noms de clés, pas de HTML, valeurs non vides, mêmes clés que la référence,
# mêmes {variables} par clé, formes de pluriel requises par la langue, longueurs maximales par emplacement.
# Lecture seule. Code de sortie 1 s'il y a une erreur (une clé manquante n'est qu'un avertissement avec -AllowMissing).
# Exemples :
#   tools\i18n\check-catalogs.ps1 -Reference web\i18n\fr.js -Target web\i18n\en.js
#   tools\i18n\check-catalogs.ps1 -Reference tools\i18n\exemples\fr.js -Target tools\i18n\exemples\en.js -AllowMissing
param(
  [Parameter(Mandatory = $true)][string]$Reference,
  [string[]]$Target = @(),
  [string]$Limits = '',
  [switch]$AllowMissing,
  [switch]$Quiet
)
$ErrorActionPreference = 'Stop'
if (-not $Limits) { $Limits = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'longueurs.json' }
$script:errors = 0; $script:warnings = 0
function Err([string]$m) { $script:errors++; if (-not $Quiet) { "ERREUR  $m" } }
function Warn([string]$m) { $script:warnings++; if (-not $Quiet) { "ALERTE  $m" } }

# Catégories de pluriel exigées (Intl.PluralRules) : « many » existe en français (1 000 000) mais retombe sur « other ».
$pluralRequired = @{ fr = @('one', 'other'); en = @('one', 'other'); qps = @('one', 'other') }
$pluralAllowed = @('zero', 'one', 'two', 'few', 'many', 'other')
$keyRx = '^(_meta|[a-z][a-zA-Z0-9]*(\.[a-zA-Z][a-zA-Z0-9]*)+)$'

function Read-Catalog([string]$path) {
  $text = [IO.File]::ReadAllText((Resolve-Path $path))
  $lines = $text -split "`n"
  $i = 0
  while ($i -lt $lines.Count -and ($lines[$i].Trim() -eq '' -or $lines[$i].TrimStart().StartsWith('//'))) { $i++ }
  if ($i -ge $lines.Count) { Err "$path : fichier vide"; return $null }
  $m = [regex]::Match($lines[$i], '^window\.(DRAFT_I18N_([A-Z]+))\s*=\s*\{\s*$')
  if (-not $m.Success) { Err "$path : la première ligne de code doit être « window.DRAFT_I18N_XX = { »"; return $null }
  $lang = $m.Groups[2].Value.ToLowerInvariant()
  $body = ($lines[$i..($lines.Count - 1)] -join "`n").Substring($lines[$i].IndexOf('=') + 1).Trim()
  if ($body.EndsWith(';')) { $body = $body.Substring(0, $body.Length - 1).TrimEnd() }
  if (-not ($body.StartsWith('{') -and $body.EndsWith('}'))) { Err "$path : l'objet doit se terminer par « }; » sans rien après"; return $null }
  # Une clé par ligne : permet de repérer les doublons (JSON les accepte, le dernier gagnerait sans bruit).
  $seen = @{}
  for ($k = $i + 1; $k -lt $lines.Count; $k++) {
    $l = $lines[$k].Trim()
    if ($l -eq '' -or $l -match '^\};?$') { continue }
    $km = [regex]::Match($l, '^"((?:[^"\\]|\\.)*)"\s*:')
    if (-not $km.Success) { Err "${path}:$($k + 1) : ligne qui ne commence pas par une clé (une clé par ligne)"; continue }
    $key = $km.Groups[1].Value
    if ($seen.ContainsKey($key)) { Err "${path}:$($k + 1) : clé en double « $key » (déjà ligne $($seen[$key]))" } else { $seen[$key] = $k + 1 }
  }
  try { $obj = $body | ConvertFrom-Json } catch { Err "$path : JSON invalide ($($_.Exception.Message))"; return $null }
  $map = [ordered]@{}
  foreach ($p in $obj.PSObject.Properties) { $map[$p.Name] = $p.Value }
  return [pscustomobject]@{ Path = $path; Lang = $lang; Map = $map; Name = $m.Groups[1].Value }
}

function Vars([string]$s) {
  $clean = $s.Replace('{{', '').Replace('}}', '')
  return @([regex]::Matches($clean, '\{([a-zA-Z][a-zA-Z0-9]*)(:[a-z]+)?\}') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
}
function Forms($v) {
  if ($v -is [string]) { return @{ '' = $v } }
  $f = @{}
  foreach ($p in $v.PSObject.Properties) { $f[$p.Name] = $p.Value }
  return $f
}

$limitRules = @()
if ($Limits -and (Test-Path $Limits)) { $limitRules = (Get-Content -Raw -Encoding UTF8 $Limits | ConvertFrom-Json).regles }
function Check-Values($cat) {
  foreach ($key in $cat.Map.Keys) {
    if ($key -notmatch $keyRx) { Err "$($cat.Path) : nom de clé invalide « $key »" }
    if ($key -eq '__proto__') { Err "$($cat.Path) : clé __proto__ interdite" }
    if ($key -eq '_meta') { continue }
    $v = $cat.Map[$key]
    if (-not ($v -is [string])) {
      $names = @($v.PSObject.Properties.Name)
      foreach ($n in $names) { if ($pluralAllowed -notcontains $n) { Err "$($cat.Path) : « $key » : forme de pluriel inconnue « $n »" } }
      $req = $pluralRequired[$cat.Lang]; if (-not $req) { $req = @('other') }
      foreach ($r in $req) { if ($names -notcontains $r) { Err "$($cat.Path) : « $key » : forme de pluriel « $r » absente" } }
      if ((Vars ([string]$v.other)) -notcontains 'count') { Warn "$($cat.Path) : « $key » : pluriel sans {count} dans la forme other" }
    }
    foreach ($e in (Forms $v).GetEnumerator()) {
      $s = [string]$e.Value
      if ($s.Trim() -eq '') { Err "$($cat.Path) : « $key » : valeur vide" }
      if ($s -match '<\s*/?\s*[a-zA-Z]') { Err "$($cat.Path) : « $key » : balise HTML interdite dans une valeur" }
      if (($s.Replace('{{', '').Replace('}}', '') -replace '\{[a-zA-Z][a-zA-Z0-9]*(:[a-z]+)?\}', '') -match '[{}]') { Err "$($cat.Path) : « $key » : accolade seule (écrire {{ ou }})" }
      foreach ($r in $limitRules) {
        if ($key -like $r.cle) {
          $len = ($s -replace '\{[a-zA-Z][a-zA-Z0-9]*(:[a-z]+)?\}', ('x' * [int]$r.variable)).Length
          if ($len -gt [int]$r.max) { Err "$($cat.Path) : « $key » : $len caractères pour $($r.max) au plus ($($r.emplacement))" }
          break
        }
      }
    }
  }
}

$ref = Read-Catalog $Reference
if (-not $ref) { "Référence illisible."; exit 1 }
Check-Values $ref
foreach ($t in $Target) {
  $cat = Read-Catalog $t
  if (-not $cat) { continue }
  Check-Values $cat
  foreach ($key in $ref.Map.Keys) {
    if ($key -eq '_meta') { continue }
    if (-not $cat.Map.Contains($key)) {
      if ($AllowMissing) { Warn "$($cat.Path) : clé manquante « $key » (repli sur le français)" } else { Err "$($cat.Path) : clé manquante « $key »" }
      continue
    }
    $rv = $ref.Map[$key]; $tv = $cat.Map[$key]
    $rvars = @(); foreach ($e in (Forms $rv).Values) { $rvars += Vars ([string]$e) }; $rvars = @($rvars | Sort-Object -Unique)
    $tvars = @(); foreach ($e in (Forms $tv).Values) { $tvars += Vars ([string]$e) }; $tvars = @($tvars | Sort-Object -Unique)
    if (($rvars -join ',') -ne ($tvars -join ',')) { Err "$($cat.Path) : « $key » : variables {$($tvars -join ', ')} au lieu de {$($rvars -join ', ')}" }
    if (($rv -is [string]) -ne ($tv -is [string])) { Err "$($cat.Path) : « $key » : pluriel d'un côté seulement" }
  }
  foreach ($key in $cat.Map.Keys) { if (-not $ref.Map.Contains($key)) { Err "$($cat.Path) : clé en trop « $key » (absente de la référence)" } }
}
"Catalogues vérifiés : $(1 + $Target.Count). Erreurs : $script:errors. Alertes : $script:warnings."
if ($script:errors) { exit 1 } else { exit 0 }
