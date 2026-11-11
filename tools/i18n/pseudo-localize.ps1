# Pseudo-langue « qps » pour les tests (jamais livrée) : chaque texte du catalogue français est accentué,
# allongé d'environ 35 % et encadré de crochets : « Choix net » devient « [Çĥôîx ñéţ ·····] ».
# À l'écran, un texte sans crochets est un texte resté en dur ; un crochet fermant absent signale un texte coupé.
# Les variables {nom}, les accolades doublées et les retours à la ligne sont gardés tels quels.
# Exemple : tools\i18n\pseudo-localize.ps1 -Source web\i18n\fr.js -Out $env:TEMP\draft-i18n-prep\qps.js
param(
  [Parameter(Mandatory = $true)][string]$Source,
  [string]$Out = '',
  [double]$Growth = 0.35
)
$ErrorActionPreference = 'Stop'
if (-not $Out) { $dir = Join-Path $env:TEMP 'draft-i18n-prep'; New-Item -ItemType Directory -Force $dir | Out-Null; $Out = Join-Path $dir 'qps.js' }
$from = 'aeiouncysthAEIOUCNSD'; $to = 'àéîôüñçýšţĥÅÉÎÖÛÇÑŠÐ'

function Pseudo([string]$s) {
  $parts = [regex]::Split($s, '(\{\{|\}\}|\{[a-zA-Z][a-zA-Z0-9]*(?::[a-z]+)?\}|\n)')
  $sb = [Text.StringBuilder]::new()
  $letters = 0
  foreach ($p in $parts) {
    if ($p -match '^(\{\{|\}\}|\{[a-zA-Z][a-zA-Z0-9]*(:[a-z]+)?\}|\n)$') { [void]$sb.Append($p); continue }
    foreach ($ch in $p.ToCharArray()) {
      $ix = $from.IndexOf($ch)
      if ($ix -ge 0) { [void]$sb.Append($to[$ix]) } else { [void]$sb.Append($ch) }
      if ([char]::IsLetter($ch)) { $letters++ }
    }
  }
  $pad = [math]::Max(1, [math]::Ceiling($s.Length * $Growth))
  return '[' + $sb.ToString() + ' ' + ('·' * $pad) + ']'
}

$text = [IO.File]::ReadAllText((Resolve-Path $Source))
$lines = $text -split "`n"
$outLines = [Collections.Generic.List[string]]::new()
$outLines.Add('// Pseudo-langue générée par tools/i18n/pseudo-localize.ps1 : test seulement, jamais livrée.')
$started = $false
foreach ($l in $lines) {
  $t = $l.TrimEnd("`r")
  if (-not $started) {
    if ($t -match '^window\.DRAFT_I18N_[A-Z]+\s*=') { $outLines.Add('window.DRAFT_I18N_QPS = {'); $started = $true }
    continue
  }
  $m = [regex]::Match($t, '^("(?:[^"\\]|\\.)*")\s*:\s*(.+?)(,?)$')
  if (-not $m.Success) { $outLines.Add($t); continue }
  $key = $m.Groups[1].Value
  if ($key -eq '"_meta"') { $outLines.Add('"_meta": {"lang": "qps", "name": "Pseudo", "version": 1}' + $m.Groups[3].Value); continue }
  $val = ('{"v":' + $m.Groups[2].Value + '}' | ConvertFrom-Json).v
  if ($val -is [string]) { $new = Pseudo $val }
  else { $o = [ordered]@{}; foreach ($p in $val.PSObject.Properties) { $o[$p.Name] = Pseudo ([string]$p.Value) }; $new = $o }
  $outLines.Add($key + ': ' + ($new | ConvertTo-Json -Compress) + $m.Groups[3].Value)
}
[IO.File]::WriteAllText($Out, ($outLines -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
"Pseudo-langue écrite : $Out"
