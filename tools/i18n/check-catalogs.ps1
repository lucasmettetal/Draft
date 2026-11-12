# Vérifie des catalogues de traduction (format : docs/i18n/FORMAT-CATALOGUE.md ; tests : docs/i18n/TESTS.md).
# Contrôles : forme « window.DRAFT_I18N_XX = {objet JSON}; » précédée seulement de commentaires « // », une clé par ligne,
# pas de clé en double, noms de clés, pas de HTML, valeurs non vides, mêmes clés que la référence,
# mêmes {variables} par clé, formes de pluriel requises par la langue, longueurs maximales par emplacement.
# Lecture seule. Code de sortie 1 s'il y a une erreur (une clé manquante n'est qu'un avertissement avec -AllowMissing).
# Exemples :
#   tools\i18n\check-catalogs.ps1 -Reference web\i18n\fr.js -Target web\i18n\en.js
#   tools\i18n\check-catalogs.ps1 -Reference tools\i18n\exemples\fr.js -Target tools\i18n\exemples\en.js -AllowMissing
#   tools\i18n\check-catalogs.ps1 -Reference web\i18n\fr.js -Code .   (I18N-1 : clés utilisées par la page = clés du catalogue, variables fournies)
param(
  [Parameter(Mandatory = $true)][string]$Reference,
  [string[]]$Target = @(),
  [string]$Limits = '',
  [switch]$AllowMissing,
  [string]$Code = '',
  [string]$Security = '',
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

# ---------- Clés utilisées par le code de la page (-Code <racine du dépôt>) ----------
# Chaque appel t(), tp(), tx() (ou I18N.t...) est relu : les clés citées dans son premier argument doivent exister,
# et l'objet de variables doit fournir toutes les {variables} de ces clés. Une clé du catalogue que le code ne cite
# nulle part est une clé orpheline. Une clé construite (gabarit, concaténation) n'est pas vérifiable : elle est interdite.
if ($Code) {
  if (-not ('DraftI18nKeys.Scanner' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
namespace DraftI18nKeys
{
    // Un appel t(), tp() ou tx() : clés citées dans le premier argument, variables fournies (null si inconnues).
    public class Call
    {
        public string Fn; public int Line; public List<string> Keys = new List<string>(); public List<string> Vars; public bool Plural;
    }
    public class Scanner
    {
        static readonly Regex KeyRx = new Regex(@"^[a-z][a-zA-Z0-9]*(\.[a-zA-Z][a-zA-Z0-9]*)+$");
        readonly string s; readonly int n;
        public readonly List<Call> Calls = new List<Call>();
        public readonly List<string> Literals = new List<string>();
        public Scanner(string src) { s = src; n = src.Length; }
        int LineOf(int pos) { int l = 1; for (int i = 0; i < pos && i < n; i++) if (s[i] == '\n') l++; return l; }

        public void Run(int start, int end) { int i = start; Code(ref i, end, false); }

        // Parcourt du code JavaScript ; s'arrête sur une accolade fermante de niveau 0 si stopAtBrace (fin de ${...}).
        void Code(ref int i, int end, bool stopAtBrace)
        {
            int depth = 0; char prev = '\0'; string prevWord = "";
            while (i < end)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '/' && i + 1 < end && s[i + 1] == '/') { while (i < end && s[i] != '\n') i++; continue; }
                if (c == '/' && i + 1 < end && s[i + 1] == '*') { int e = s.IndexOf("*/", i + 2, StringComparison.Ordinal); i = e < 0 ? end : e + 2; continue; }
                if (c == '"' || c == '\'') { string lit = Str(ref i, end, c); Literals.Add(lit); prev = 'a'; prevWord = ""; continue; }
                if (c == '`') { Template(ref i, end); prev = 'a'; prevWord = ""; continue; }
                if (c == '/')
                {
                    bool regex = prev == '\0' || "(,=:[!&|?{};+-*%<>~^".IndexOf(prev) >= 0 || prevWord == "return" || prevWord == "typeof" || prevWord == "case";
                    if (regex) { SkipRegex(ref i, end); prev = 'a'; prevWord = ""; continue; }
                    i++; prev = '/'; prevWord = ""; continue;
                }
                if (char.IsLetter(c) || c == '_' || c == '$')
                {
                    int st = i;
                    while (i < end && (char.IsLetterOrDigit(s[i]) || s[i] == '_' || s[i] == '$')) i++;
                    string w = s.Substring(st, i - st);
                    bool dotted = st > 0 && s[st - 1] == '.';
                    bool viaI18n = dotted && st >= 5 && s.Substring(st - 5, 5) == "I18N.";
                    if ((w == "t" || w == "tp" || w == "tx") && (!dotted || viaI18n))
                    {
                        int k = i; while (k < end && char.IsWhiteSpace(s[k])) k++;
                        if (k < end && s[k] == '(') { ParseCall(w, st, ref k, end); i = k; prev = ')'; prevWord = ""; continue; }
                    }
                    prev = 'a'; prevWord = w; continue;
                }
                if (char.IsDigit(c)) { i++; prev = 'a'; prevWord = ""; continue; }
                if (c == '{' || c == '(' || c == '[') depth++;
                else if (c == '}' || c == ')' || c == ']')
                {
                    if (c == '}' && depth == 0 && stopAtBrace) { i++; return; }
                    depth--;
                }
                prev = c; prevWord = ""; i++;
            }
        }

        // Arguments de niveau 0 d'un appel : positions [début, fin) de chacun.
        void ParseCall(string fn, int at, ref int i, int end)
        {
            int open = i; i++;
            List<int[]> args = new List<int[]>(); int argStart = i;
            int depth = 0;
            while (i < end)
            {
                char c = s[i];
                if (c == '"' || c == '\'') { Literals.Add(Str(ref i, end, c)); continue; }
                if (c == '`') { SkipTemplate(ref i, end); continue; }
                if (c == '/' && i + 1 < end && s[i + 1] == '/') { while (i < end && s[i] != '\n') i++; continue; }
                if (c == '(' || c == '{' || c == '[') { depth++; i++; continue; }
                if (c == ')' || c == '}' || c == ']')
                {
                    if (depth == 0) { args.Add(new int[] { argStart, i }); i++; break; }
                    depth--; i++; continue;
                }
                if (c == ',' && depth == 0) { args.Add(new int[] { argStart, i }); i++; argStart = i; continue; }
                i++;
            }
            Call call = new Call { Fn = fn, Line = LineOf(at), Plural = fn == "tp" };
            if (args.Count > 0)
            {
                foreach (Match m in Regex.Matches(s.Substring(args[0][0], args[0][1] - args[0][0]), "\"([^\"\\\\]*)\""))
                    if (KeyRx.IsMatch(m.Groups[1].Value)) call.Keys.Add(m.Groups[1].Value);
            }
            int varsIdx = fn == "tp" ? 2 : 1;
            if (args.Count > varsIdx) call.Vars = ObjectNames(args[varsIdx][0], args[varsIdx][1]);
            else call.Vars = new List<string>();
            Calls.Add(call);
            // Les arguments peuvent contenir d'autres appels (t() imbriqués, gabarits html``) : on les parcourt aussi.
            foreach (int[] a in args) { int k = a[0]; Code(ref k, a[1], false); }
        }

        // Noms de propriétés de niveau 1 d'un objet littéral { a: x, b, c: y } ; null si l'argument n'est pas un objet littéral.
        List<string> ObjectNames(int st, int en)
        {
            string arg = s.Substring(st, en - st).Trim();
            if (!arg.StartsWith("{") || !arg.EndsWith("}")) return null;
            List<string> names = new List<string>();
            int depth = 0; bool expectName = true; int i = 1;
            while (i < arg.Length - 1)
            {
                char c = arg[i];
                if (c == '"' || c == '\'') { char q = c; i++; while (i < arg.Length && arg[i] != q) { if (arg[i] == '\\') i++; i++; } i++; continue; }
                if (c == '`') { int d2 = 0; i++; while (i < arg.Length) { if (arg[i] == '\\') { i += 2; continue; } if (arg[i] == '$' && i + 1 < arg.Length && arg[i + 1] == '{') { d2++; i += 2; continue; } if (arg[i] == '}' && d2 > 0) { d2--; i++; continue; } if (arg[i] == '`' && d2 == 0) { i++; break; } i++; } continue; }
                if (c == '{' || c == '(' || c == '[') { depth++; i++; continue; }
                if (c == '}' || c == ')' || c == ']') { depth--; i++; continue; }
                if (depth == 0 && c == ',') { expectName = true; i++; continue; }
                if (depth == 0 && expectName && (char.IsLetter(c) || c == '_' || c == '$'))
                {
                    int b = i; while (i < arg.Length && (char.IsLetterOrDigit(arg[i]) || arg[i] == '_' || arg[i] == '$')) i++;
                    names.Add(arg.Substring(b, i - b)); expectName = false; continue;
                }
                if (char.IsWhiteSpace(c)) { i++; continue; }
                expectName = false; i++;
            }
            return names;
        }

        string Str(ref int i, int end, char q)
        {
            StringBuilder sb = new StringBuilder(); i++;
            while (i < end)
            {
                char ch = s[i];
                if (ch == '\\' && i + 1 < end) { sb.Append(s[i + 1]); i += 2; continue; }
                if (ch == q) { i++; break; }
                if (ch == '\n') break;
                sb.Append(ch); i++;
            }
            return sb.ToString();
        }

        void Template(ref int i, int end)
        {
            i++;
            while (i < end)
            {
                char ch = s[i];
                if (ch == '\\') { i += 2; continue; }
                if (ch == '`') { i++; return; }
                if (ch == '$' && i + 1 < end && s[i + 1] == '{') { i += 2; Code(ref i, end, true); continue; }
                i++;
            }
        }

        // Saute un gabarit sans l'analyser (il le sera avec les arguments de l'appel).
        void SkipTemplate(ref int i, int end)
        {
            i++; int d = 0;
            while (i < end)
            {
                char ch = s[i];
                if (ch == '\\') { i += 2; continue; }
                if (d == 0 && ch == '`') { i++; return; }
                if (ch == '$' && i + 1 < end && s[i + 1] == '{') { d++; i += 2; continue; }
                if (d > 0 && (ch == '"' || ch == '\'')) { Str(ref i, end, ch); continue; }
                if (d > 0 && ch == '`') { SkipTemplate(ref i, end); continue; }
                if (d > 0 && ch == '{') { d++; i++; continue; }
                if (d > 0 && ch == '}') { d--; i++; continue; }
                i++;
            }
        }

        void SkipRegex(ref int i, int end)
        {
            i++; bool cls = false;
            while (i < end)
            {
                char ch = s[i];
                if (ch == '\\') { i += 2; continue; }
                if (ch == '\n') break;
                if (ch == '[') cls = true; else if (ch == ']') cls = false;
                else if (ch == '/' && !cls) { i++; break; }
                i++;
            }
            while (i < end && char.IsLetter(s[i])) i++;
        }
    }
}

'@
  }
  $web = Join-Path (Resolve-Path $Code) 'web'
  $skipFiles = @('draft-i18n.js', 'draft-champions.js', 'draft-champion-ids.js', 'draft-champion-official.js', 'draft-role-sheet.js', 'draft-vendor', 'draft-stats.js', 'draft-history.js', 'draft-role-rates.js', 'draft-role-rates-live.js', 'draft-matchups-live.js')
  $sources = @(Get-ChildItem $web -File -Filter '*.js' | Where-Object { $skipFiles -notcontains $_.Name }) + @(Get-Item (Join-Path $web 'index.html'))
  $used = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  $nCalls = 0; $nDynamic = 0
  foreach ($f in $sources) {
    $text = [IO.File]::ReadAllText($f.FullName)
    $sc = [DraftI18nKeys.Scanner]::new($text)
    if ($f.Name -eq 'index.html') { foreach ($m in [regex]::Matches($text, '<script>([\s\S]*?)</script>')) { $sc.Run($m.Groups[1].Index, $m.Groups[1].Index + $m.Groups[1].Length) } }
    else { $sc.Run(0, $text.Length) }
    foreach ($l in $sc.Literals) { [void]$used.Add($l) }
    foreach ($call in $sc.Calls) {
      $nCalls++
      $where = "web/$($f.Name):$($call.Line)"
      if ($call.Keys.Count -eq 0) { $nDynamic++; continue }
      foreach ($k in $call.Keys) { if (-not $ref.Map.Contains($k)) { Err "$where : clé absente du catalogue « $k »" } }
      if ($null -eq $call.Vars) { continue }
      $given = @($call.Vars); if ($call.Plural) { $given += 'count' }
      $needAll = @()
      foreach ($k in $call.Keys) {
        if (-not $ref.Map.Contains($k)) { continue }
        $need = @(); foreach ($e in (Forms $ref.Map[$k]).Values) { $need += Vars ([string]$e) }
        $need = @($need | Sort-Object -Unique); $needAll += $need
        foreach ($v in $need) { if ($given -notcontains $v) { Err "$where : « $k » attend {$v}, non fourni" } }
        if (($ref.Map[$k] -isnot [string]) -and -not $call.Plural) { Err "$where : « $k » est un pluriel, à appeler avec tp()" }
      }
      foreach ($v in $call.Vars) { if ($needAll -notcontains $v) { Warn "$where : variable {$v} fournie mais inutilisée par $($call.Keys -join ', ')" } }
    }
  }
  $orphans = @($ref.Map.Keys | Where-Object { $_ -ne '_meta' -and -not $used.Contains($_) })
  foreach ($o in $orphans) { Err "clé orpheline (citée nulle part dans web/) : « $o »" }
  "Code relu : $($sources.Count) fichiers, $nCalls appels t/tp/tx, dont $nDynamic avec une clé en variable (vérifiée en mode test de la page), $(@($orphans).Count) clé(s) orpheline(s)."
}

# ---------- Messages de sécurité (-Security tools\i18n\securite.json) ----------
# Ces clés sont traduites mot pour mot et relues par deux personnes (STRATEGIE.md § 5) : elles doivent exister.
if ($Security) {
  $sec = Get-Content -Raw -Encoding UTF8 $Security | ConvertFrom-Json
  foreach ($e in $sec.cles) { if (-not $ref.Map.Contains($e.cle)) { Err "$Security : clé de sécurité absente du catalogue « $($e.cle) »" } }
  "Messages de sécurité marqués : $(@($sec.cles).Count)."
}
"Catalogues vérifiés : $(1 + $Target.Count). Erreurs : $script:errors. Alertes : $script:warnings."
if ($script:errors) { exit 1 } else { exit 0 }
