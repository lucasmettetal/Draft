# Détecteur de textes visibles écrits en dur (préparation de l'internationalisation, voir docs/i18n/TESTS.md).
# Lit web/*.js, web/index.html et src/*.cs, sans rien modifier, sans réseau, sans exécuter la page.
# Une chaîne est « candidate » si elle ressemble à un texte montré au joueur : lettres accentuées, au moins deux mots,
# texte entre balises d'un gabarit html``, attribut title / aria-label / placeholder, ou argument d'un appel d'affichage
# natif (MessageBox.Show, ToolStripMenuItem, ShowBalloonTip, Text = ...). Les exclusions sont dans exclusions.json.
# Garde-fou de CI : depuis I18N-1, 0 chaîne candidate hors exclusions dans web/ (-Only web/ -Max 0) ; src/ suit en I18N-3.
# Exemples :
#   tools\i18n\detect-hardcoded.ps1                         (résumé par fichier)
#   tools\i18n\detect-hardcoded.ps1 -Detail                 (chaque chaîne, fichier:ligne)
#   tools\i18n\detect-hardcoded.ps1 -Json sortie.json       (rapport machine)
#   tools\i18n\detect-hardcoded.ps1 -Max 0                  (échoue s'il reste une chaîne en dur)
#   tools\i18n\detect-hardcoded.ps1 -Baseline ancien.json -Max 0   (échoue seulement sur les chaînes nouvelles)
#   tools\i18n\detect-hardcoded.ps1 -Only web/ -Max 0       (page seulement : garde-fou de CI depuis I18N-1 ; le natif suit en I18N-3)
#   tools\i18n\detect-hardcoded.ps1 -SelfTest               (vérifie l'analyseur sur des cas écrits à la main)
param(
  [string]$Root = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
  [string]$Exclusions = (Join-Path $PSScriptRoot 'exclusions.json'),
  [string]$Json = '',
  [switch]$Detail,
  [switch]$ShowExcluded,
  [int]$Max = -1,
  [string]$Baseline = '',
  [switch]$SelfTest,
  [string]$Only = ''
)
$ErrorActionPreference = 'Stop'

# Analyseur lexical écrit en C# 5 (compatible Windows PowerShell 5.1 et PowerShell 7) : chaînes, gabarits, commentaires, expressions régulières.
if (-not ('DraftI18n.Lexer' -as [type])) {
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
namespace DraftI18n
{
    public class Lit
    {
        public int Offset; public int Line; public string Text; public string Kind; public string Attr = ""; public string Before = ""; public string Scope = ""; public string Member = "";
    }

    public class Lexer
    {
        readonly string s; readonly int n;
        public readonly List<Lit> Out = new List<Lit>();
        readonly List<string> classStack = new List<string>();
        readonly List<int> classDepth = new List<int>();
        readonly List<string> memberStack = new List<string>();
        readonly List<int> memberDepth = new List<int>();
        string pendingClass, pendingMember, lastIdent; int depth;
        const char Hole = '\u0001';
        static readonly Regex VisibleAttr = new Regex(@"(?<![\w-])(title|aria-label|placeholder|alt|label|aria-description)\s*=\s*(""([^""]*)""|'([^']*)')");

        public Lexer(string src) { s = src; n = src.Length; }

        // ---------- Fichier HTML : scripts en ligne en JavaScript, le reste en balisage ----------
        public void Html()
        {
            int pos = 0;
            Regex open = new Regex(@"<(script|style)\b([^>]*)>", RegexOptions.IgnoreCase);
            while (pos < n)
            {
                Match m = open.Match(s, pos);
                int stop = m.Success ? m.Index : n;
                Markup(pos, stop);
                if (!m.Success) break;
                string tag = m.Groups[1].Value.ToLowerInvariant();
                int bodyStart = m.Index + m.Length;
                int close = s.IndexOf("</" + tag, bodyStart, StringComparison.OrdinalIgnoreCase);
                if (close < 0) close = n;
                bool js = tag == "script" && !Regex.IsMatch(m.Groups[2].Value, @"\bsrc\s*=|type\s*=\s*""(text/plain|application/json)""", RegexOptions.IgnoreCase);
                if (js) Js(bodyStart, close);
                int after = s.IndexOf('>', close);
                pos = after < 0 ? n : after + 1;
            }
        }

        void Markup(int start, int end)
        {
            StringBuilder sb = new StringBuilder(); List<int> map = new List<int>();
            for (int i = start; i < end; i++) { sb.Append(s[i]); map.Add(i); }
            MarkupText(sb.ToString(), map, "html-text", "html-attr");
        }

        // ---------- JavaScript ----------
        public void Js(int start, int end) { int i = start; JsCode(ref i, end, false); }

        void JsCode(ref int i, int end, bool stopAtBrace)
        {
            int d = 0; char prev = '\0'; string prevWord = "";
            while (i < end)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '/' && i + 1 < end && s[i + 1] == '/') { while (i < end && s[i] != '\n') i++; continue; }
                if (c == '/' && i + 1 < end && s[i + 1] == '*') { int e = s.IndexOf("*/", i + 2, StringComparison.Ordinal); i = e < 0 ? end : e + 2; continue; }
                if (c == '"' || c == '\'') { int st = i; string t = JsString(ref i, end, c); Emit(st, t, "js-string", ""); prev = 'a'; prevWord = ""; continue; }
                if (c == '`') { string tag = PrevIdent(i); JsTemplate(ref i, end, tag); prev = 'a'; prevWord = ""; continue; }
                if (c == '/')
                {
                    bool regex = prev == '\0' || "(,=:[!&|?{};+-*%<>~^".IndexOf(prev) >= 0 || prevWord == "return" || prevWord == "typeof" || prevWord == "case" || prevWord == "in" || prevWord == "of";
                    if (regex) { SkipRegex(ref i, end); prev = 'a'; prevWord = ""; continue; }
                    i++; prev = '/'; prevWord = ""; continue;
                }
                if (char.IsLetterOrDigit(c) || c == '_' || c == '$')
                {
                    int st = i;
                    while (i < end && (char.IsLetterOrDigit(s[i]) || s[i] == '_' || s[i] == '$')) i++;
                    prevWord = s.Substring(st, i - st); prev = 'a'; continue;
                }
                if (c == '{') d++;
                else if (c == '}') { if (d == 0 && stopAtBrace) { i++; return; } d--; }
                prev = c; prevWord = ""; i++;
            }
        }

        string JsString(ref int i, int end, char q)
        {
            StringBuilder sb = new StringBuilder(); i++;
            while (i < end)
            {
                char ch = s[i];
                if (ch == '\\' && i + 1 < end) { i = Escape(i, sb); continue; }
                if (ch == q) { i++; break; }
                if (ch == '\n') break;
                sb.Append(ch); i++;
            }
            return sb.ToString();
        }

        // Décode une séquence d'échappement JavaScript ou C# et renvoie la position suivante.
        int Escape(int i, StringBuilder sb)
        {
            char e = s[i + 1];
            if (e == 'n') { sb.Append('\n'); return i + 2; }
            if (e == 'r') { sb.Append('\r'); return i + 2; }
            if (e == 't') { sb.Append('\t'); return i + 2; }
            if (e == 'u' && i + 2 < n && s[i + 2] == '{') { int c = s.IndexOf('}', i + 3); if (c > 0) { try { sb.Append(char.ConvertFromUtf32(Convert.ToInt32(s.Substring(i + 3, c - i - 3), 16))); } catch { } return c + 1; } }
            if (e == 'u' && i + 6 <= n) { try { sb.Append((char)Convert.ToInt32(s.Substring(i + 2, 4), 16)); return i + 6; } catch { } }
            if (e == 'x' && i + 4 <= n) { try { sb.Append((char)Convert.ToInt32(s.Substring(i + 2, 2), 16)); return i + 4; } catch { } }
            if (e == '\r' || e == '\n') return i + 2;
            sb.Append(e); return i + 2;
        }

        void JsTemplate(ref int i, int end, string tag)
        {
            int start = i; i++;
            StringBuilder sb = new StringBuilder(); List<int> map = new List<int>();
            while (i < end)
            {
                char ch = s[i];
                if (ch == '\\' && i + 1 < end) { int at = i, before = sb.Length; i = Escape(i, sb); for (int k = before; k < sb.Length; k++) map.Add(at); continue; }
                if (ch == '`') { i++; break; }
                if (ch == '$' && i + 1 < end && s[i + 1] == '{') { sb.Append(Hole); map.Add(i); i += 2; JsCode(ref i, end, true); continue; }
                sb.Append(ch); map.Add(i); i++;
            }
            if (tag == "html") MarkupText(sb.ToString(), map, "html-text", "html-attr");
            else Emit(start, sb.ToString(), "js-template", "");
        }

        void SkipRegex(ref int i, int end)
        {
            i++; bool cls = false;
            while (i < end)
            {
                char ch = s[i];
                if (ch == '\\') { i += 2; continue; }
                if (ch == '\n') break;
                if (ch == '[') cls = true;
                else if (ch == ']') cls = false;
                else if (ch == '/' && !cls) { i++; break; }
                i++;
            }
            while (i < end && char.IsLetter(s[i])) i++;
        }

        string PrevIdent(int pos)
        {
            int k = pos - 1;
            while (k >= 0 && char.IsWhiteSpace(s[k])) k--;
            int e = k + 1;
            while (k >= 0 && (char.IsLetterOrDigit(s[k]) || s[k] == '_' || s[k] == '$')) k--;
            return s.Substring(k + 1, e - k - 1);
        }

        // Texte entre balises et attributs visibles d'un balisage (page ou gabarit html``).
        void MarkupText(string t, List<int> map, string textKind, string attrKind)
        {
            StringBuilder txt = new StringBuilder(); int txtStart = -1;
            int k = 0;
            while (k < t.Length)
            {
                char ch = t[k];
                if (ch == '<' && k + 1 < t.Length && (char.IsLetter(t[k + 1]) || t[k + 1] == '/' || t[k + 1] == Hole || t[k + 1] == '!'))
                {
                    FlushText(txt, txtStart, map, textKind); txt.Length = 0; txtStart = -1;
                    if (k + 3 < t.Length && t.Substring(k, 4) == "<!--") { int c = t.IndexOf("-->", k + 4, StringComparison.Ordinal); k = c < 0 ? t.Length : c + 3; continue; }
                    int e = k + 1; char q = '\0';
                    while (e < t.Length)
                    {
                        char x = t[e];
                        if (q != '\0') { if (x == q) q = '\0'; }
                        else if (x == '"' || x == '\'') q = x;
                        else if (x == '>') break;
                        e++;
                    }
                    int stop = Math.Min(e + 1, t.Length);
                    string tagText = t.Substring(k, stop - k);
                    foreach (Match m in VisibleAttr.Matches(tagText))
                    {
                        Group g = m.Groups[3].Success ? m.Groups[3] : m.Groups[4];
                        if (g.Length == 0) continue;
                        Emit(map[k + g.Index], g.Value, attrKind, m.Groups[1].Value);
                    }
                    k = stop; continue;
                }
                if (txtStart < 0) txtStart = k;
                txt.Append(ch); k++;
            }
            FlushText(txt, txtStart, map, textKind);
        }

        void FlushText(StringBuilder txt, int txtStart, List<int> map, string kind)
        {
            if (txtStart < 0) return;
            string raw = txt.ToString();
            int lead = 0;
            while (lead < raw.Length && char.IsWhiteSpace(raw[lead])) lead++;
            if (lead == raw.Length) return;
            Emit(map[txtStart + lead], raw, kind, "");
        }

        // ---------- C# ----------
        public void Cs() { int i = 0; CsCode(ref i, n, false); }

        void CsCode(ref int i, int end, bool stopAtBrace)
        {
            int d = 0;
            while (i < end)
            {
                char c = s[i];
                if (c == '/' && i + 1 < end && s[i + 1] == '/') { while (i < end && s[i] != '\n') i++; continue; }
                if (c == '/' && i + 1 < end && s[i + 1] == '*') { int e = s.IndexOf("*/", i + 2, StringComparison.Ordinal); i = e < 0 ? end : e + 2; continue; }
                if (c == '\'') { i++; while (i < end && s[i] != '\'' && s[i] != '\n') { if (s[i] == '\\') i++; i++; } i++; continue; }
                if (c == '"') { CsString(ref i, end, false, false); continue; }
                if (c == '@' && i + 1 < end && s[i + 1] == '"') { i++; CsString(ref i, end, true, false); continue; }
                if (c == '$' && i + 1 < end && s[i + 1] == '"') { i++; CsString(ref i, end, false, true); continue; }
                if ((c == '$' && i + 2 < end && s[i + 1] == '@' && s[i + 2] == '"') || (c == '@' && i + 2 < end && s[i + 1] == '$' && s[i + 2] == '"')) { i += 2; CsString(ref i, end, true, true); continue; }
                if (char.IsLetter(c) || c == '_')
                {
                    int st = i;
                    while (i < end && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    string w = s.Substring(st, i - st);
                    lastIdent = w;
                    if (w == "class" || w == "struct" || w == "interface")
                    {
                        int k = i; while (k < end && char.IsWhiteSpace(s[k])) k++;
                        int b = k; while (k < end && (char.IsLetterOrDigit(s[k]) || s[k] == '_')) k++;
                        if (k > b) pendingClass = s.Substring(b, k - b);
                    }
                    continue;
                }
                if (c == '{')
                {
                    d++; depth++;
                    if (pendingClass != null) { classStack.Add(pendingClass); classDepth.Add(depth); pendingClass = null; pendingMember = null; }
                    else if (pendingMember != null && classDepth.Count > 0 && depth == classDepth[classDepth.Count - 1] + 1) { memberStack.Add(pendingMember); memberDepth.Add(depth); pendingMember = null; }
                }
                else if (c == '(' && classDepth.Count > 0 && depth == classDepth[classDepth.Count - 1] && pendingMember == null) pendingMember = lastIdent;
                else if (c == '}')
                {
                    if (d == 0 && stopAtBrace) { i++; return; }
                    d--;
                    if (memberDepth.Count > 0 && memberDepth[memberDepth.Count - 1] == depth) { memberStack.RemoveAt(memberStack.Count - 1); memberDepth.RemoveAt(memberDepth.Count - 1); }
                    if (classDepth.Count > 0 && classDepth[classDepth.Count - 1] == depth) { classStack.RemoveAt(classStack.Count - 1); classDepth.RemoveAt(classDepth.Count - 1); }
                    depth--;
                }
                else if (c == ';') { pendingClass = null; if (classDepth.Count > 0 && depth == classDepth[classDepth.Count - 1]) pendingMember = null; }
                i++;
            }
        }

        void CsString(ref int i, int end, bool verbatim, bool interp)
        {
            int start = i; i++;
            StringBuilder sb = new StringBuilder();
            while (i < end)
            {
                char ch = s[i];
                if (!verbatim && ch == '\\' && i + 1 < end) { i = Escape(i, sb); continue; }
                if (ch == '"')
                {
                    if (verbatim && i + 1 < end && s[i + 1] == '"') { sb.Append('"'); i += 2; continue; }
                    i++; break;
                }
                if (interp && ch == '{')
                {
                    if (i + 1 < end && s[i + 1] == '{') { sb.Append('{'); i += 2; continue; }
                    sb.Append(Hole); i++; CsCode(ref i, end, true); continue;
                }
                if (interp && ch == '}' && i + 1 < end && s[i + 1] == '}') { sb.Append('}'); i += 2; continue; }
                if (!verbatim && ch == '\n') break;
                sb.Append(ch); i++;
            }
            Emit(start, sb.ToString(), "cs-string", "");
        }

        void Emit(int offset, string text, string kind, string attr)
        {
            Lit l = new Lit();
            l.Offset = offset; l.Kind = kind; l.Attr = attr;
            l.Text = Regex.Replace(text.Replace(Hole.ToString(), "{}"), @"\s+", " ").Trim();
            int ls = offset > 0 ? s.LastIndexOf('\n', offset - 1) : -1;
            int pls = ls > 0 ? s.LastIndexOf('\n', ls - 1) : -1;
            l.Before = s.Substring(pls + 1, offset - pls - 1).Replace("\r", "");
            l.Scope = string.Join("/", classStack.ToArray());
            l.Member = memberStack.Count > 0 ? memberStack[memberStack.Count - 1] : "";
            Out.Add(l);
        }

        public void Lines()
        {
            int line = 1, pos = 0;
            List<Lit> sorted = new List<Lit>(Out);
            sorted.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            foreach (Lit l in sorted)
            {
                for (; pos < l.Offset && pos < n; pos++) if (s[pos] == '\n') line++;
                l.Line = line;
            }
        }
    }
}
'@
}

function Get-Literals([string]$path, [string]$text) {
  $lx = [DraftI18n.Lexer]::new($text)
  if ($path -like '*.cs') { $lx.Cs() } elseif ($path -like '*.html') { $lx.Html() } else { $lx.Js(0, $text.Length) }
  $lx.Lines()
  return $lx.Out
}

$cfg = Get-Content -Raw -Encoding UTF8 $Exclusions | ConvertFrom-Json

# Classes CSS de la page : une chaîne faite uniquement de ces noms est une liste de classes, pas un texte.
function Get-CssClasses([string]$root) {
  $set = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  $idx = Join-Path $root 'web\index.html'
  if (Test-Path $idx) {
    $html = [IO.File]::ReadAllText($idx)
    foreach ($st in [regex]::Matches($html, '<style[^>]*>([\s\S]*?)</style>')) {
      foreach ($m in [regex]::Matches($st.Groups[1].Value, '\.([A-Za-z_][\w-]*)')) { [void]$set.Add($m.Groups[1].Value) }
    }
  }
  return ,$set
}

# Noms et identifiants de champions : jamais traduits (noms anglais de Riot dans les deux langues).
function Get-ChampionNames([string]$root) {
  $set = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  foreach ($f in 'web\draft-champions.js', 'web\draft-champion-ids.js', 'web\draft-champion-official.js', 'web\draft-role-sheet.js') {
    $p = Join-Path $root $f
    if (-not (Test-Path $p)) { continue }
    foreach ($m in [regex]::Matches([IO.File]::ReadAllText($p), '"([A-Z][A-Za-z'' .&]{1,20})"\s*[:,}\]]')) { [void]$set.Add($m.Groups[1].Value) }
  }
  # Noms affichés avec espace ou apostrophe (label()) : ajoutés depuis le moteur s'ils y sont écrits.
  foreach ($n in "Twisted Fate", "Kog'Maw", "Master Yi", "Lee Sin", "Miss Fortune", "Dr. Mundo", "Aurelion Sol", "Jarvan IV", "Tahm Kench", "Xin Zhao", "Renata Glasc", "Nunu & Willump", "Bel'Veth", "Cho'Gath", "Kai'Sa", "Kha'Zix", "Rek'Sai", "Vel'Koz", "K'Sante", "Wukong") { [void]$set.Add($n) }
  foreach ($x in $cfg.nonChampions.valeurs) { [void]$set.Remove($x) }
  return ,$set
}

function Test-Like([string]$rel, [string]$motif) { return $rel -like $motif }

function Classify($lit, [string]$rel, $css, $champs) {
  $t = $lit.Text
  $plain = ($t -replace '\{\}', ' ').Trim()
  if ($plain -notmatch '\p{L}{2}') { return $null }
  $accent = $plain -match "[À-ÖØ-öø-ÿŒœ«»’…]"
  $words = [regex]::Matches($plain, "\p{L}[\p{L}'’\-]*").Count
  $markup = $lit.Kind -like 'html-*'
  $sink = $lit.Before -match $cfg.puits
  $cand = $markup -or $accent -or ($words -ge 2 -and $plain -match '\p{L} +\p{L}') -or ($sink -and $plain -cmatch '^\p{Lu}\p{Ll}')
  if (-not $cand) { return $null }
  $scopes = @($lit.Scope -split '/' | Where-Object { $_ })
  foreach ($p in $cfg.portees) {
    foreach ($sc in $scopes) { if ($p.classe -and $sc -cmatch $p.classe) { return 'portee:' + $p.nom } }
    if ($p.membre -and $lit.Member -and $lit.Member -cmatch $p.membre) { return 'portee:' + $p.nom }
  }
  $line = $lit.Before
  foreach ($r in $cfg.lignes) {
    if ($r.motif -and $line -match $r.motif) { return $r.nom }
    if ($r.avant -and $line -match $r.avant) { return $r.nom }
  }
  if ($cfg.exacts.valeurs -contains $plain) { return 'exact' }
  foreach ($r in $cfg.litteraux) { if ($plain -cmatch $r.motif) { return $r.nom } }
  $tokens = @($plain -split '[\s,·/]+' | Where-Object { $_ })
  $common = @($tokens | Where-Object { $cfg.motsCourants.valeurs -contains $_ }).Count
  if ($tokens.Count -and -not $common -and @($tokens | Where-Object { -not $css.Contains($_) }).Count -eq 0) { return 'css' }
  $rest = $plain
  foreach ($c in ($champs | Sort-Object Length -Descending)) { if ($rest.Contains($c)) { $rest = $rest.Replace($c, ' ') } }
  if ($rest -notmatch '\p{L}{2}') { return 'champion' }
  if (-not $markup -and -not $common -and $words -le 3 -and $plain -cmatch '^[a-z0-9\- ]+$' -and -not $sink -and -not $accent) {
    # Petites chaînes ASCII en minuscules (classes, clés, valeurs d'énumération) : techniques sauf preuve du contraire.
    if (@($tokens | Where-Object { $_ -match '-' -or $css.Contains($_) }).Count) { return 'css' }
  }
  return 'candidate'
}

function Invoke-Detector([string]$root) {
  $css = Get-CssClasses $root
  $champs = Get-ChampionNames $root
  $files = foreach ($g in $cfg.fichiers) { Get-ChildItem (Join-Path $root $g) -File -ErrorAction SilentlyContinue }
  $report = [ordered]@{ root = $root; date = (Get-Date).ToString('s'); fichiers = [ordered]@{}; chaines = @(); exclues = @() }
  foreach ($f in ($files | Sort-Object FullName -Unique)) {
    $rel = $f.FullName.Substring($root.Length).TrimStart('\', '/').Replace('\', '/')
    if ($Only -and -not $rel.StartsWith($Only)) { continue }
    $skip = $cfg.fichiersIgnores | Where-Object { Test-Like $rel $_.motif } | Select-Object -First 1
    if ($skip) { $report.fichiers[$rel] = [ordered]@{ ignore = $skip.raison }; continue }
    $text = [IO.File]::ReadAllText($f.FullName)
    $cand = 0; $ex = [ordered]@{}
    foreach ($lit in (Get-Literals $rel $text)) {
      $k = Classify $lit $rel $css $champs
      if (-not $k) { continue }
      $row = [ordered]@{ fichier = $rel; ligne = $lit.Line; type = $lit.Kind; attribut = $lit.Attr; texte = $lit.Text; portee = $lit.Scope; membre = $lit.Member }
      if ($k -eq 'candidate') { $cand++; $report.chaines += [pscustomobject]$row }
      else { $ex[$k] = 1 + [int]$ex[$k]; $row.regle = $k; $report.exclues += [pscustomobject]$row }
    }
    $report.fichiers[$rel] = [ordered]@{ chaines = $cand; exclues = $ex }
  }
  return $report
}

if ($SelfTest) {
  $fails = 0
  function Check([bool]$ok, [string]$name) { if ($ok) { "ok    $name" } else { "ÉCHEC $name"; $script:fails++ } }
  $js = Get-Literals 'a.js' @'
const a = "Bonjour le monde"; // "commentaire ignoré"
const r = /"pas une chaîne"/g, d = x / 2 / "y";
const v = html`<div className="card big" title="Infobulle visible">Texte ${name} affiché <${Comp} label=${"Libellé dans un trou"} /></div>`;
const k = t("nav.home"), u = `Gabarit ${n} parties`;
'@
  $texts = @($js | ForEach-Object { $_.Kind + '|' + $_.Text })
  Check ($texts -contains 'js-string|Bonjour le monde') 'JS : chaîne simple'
  Check (-not ($texts -match 'commentaire')) 'JS : commentaire ignoré'
  Check (-not ($texts -match 'pas une chaîne')) 'JS : expression régulière ignorée'
  Check ($texts -contains 'html-attr|Infobulle visible') 'html`` : attribut title'
  Check ($texts -contains 'html-text|Texte {} affiché') 'html`` : texte entre balises avec trou'
  Check ($texts -contains 'js-string|Libellé dans un trou') 'html`` : chaîne dans un trou'
  Check ($texts -contains 'js-template|Gabarit {} parties') 'gabarit simple'
  Check (-not ($texts -match 'card big')) 'html`` : className non relevé'
  $cs = Get-Literals 'a.cs' @'
class App { void M() { MessageBox.Show("Texte natif", "DRAFT"); string s = $"Version {v.ToString(3)} prête"; string p = @"C:\chemin ""cité"""; char c = '"'; } }
static class AppTests { void T() { t(true, "Description de test visible"); } }
'@
  $ct = @($cs | ForEach-Object { $_.Scope + '|' + $_.Text })
  Check ($ct -contains 'App|Texte natif') 'C# : chaîne et portée'
  Check ($ct -contains 'App|Version {} prête') 'C# : chaîne interpolée'
  Check ($ct -contains 'App|C:\chemin "cité"') 'C# : chaîne verbatim'
  Check ($ct -contains 'AppTests|Description de test visible') 'C# : portée de test'
  $cm = Get-Literals 'b.cs' @'
class CompanionApp { string a = "x"; public async Task<string> TestPage(int n) { var s = "Texte de test"; if (n > 0) { s = "Autre texte"; } return s; } void Menu() { Add("Texte du menu"); } }
'@
  $cmt = @($cm | ForEach-Object { $_.Member + '|' + $_.Text })
  Check (($cmt -contains 'TestPage|Texte de test') -and ($cmt -contains 'TestPage|Autre texte') -and ($cmt -contains 'Menu|Texte du menu')) 'C# : méthode englobante'
  $css = [Collections.Generic.HashSet[string]]::new(); [void]$css.Add('card'); [void]$css.Add('big')
  $champs = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase); [void]$champs.Add('Ahri'); [void]$champs.Add("Kog'Maw"); [void]$champs.Add('Twisted Fate')
  $mk = { param($text, $kind, $before, $scope, $member) [pscustomobject]@{ Text = $text; Kind = $kind; Before = $before; Scope = $scope; Attr = ''; Member = "$member" } }
  Check ((Classify (& $mk 'Ajouter un champion' 'js-string' 'x = ' '') 'w.js' $css $champs) -eq 'candidate') 'classement : texte visible'
  Check ((Classify (& $mk 'card big' 'js-string' 'className: ' '') 'w.js' $css $champs) -eq 'css') 'classement : classes CSS'
  Check ((Classify (& $mk 'Twisted Fate' 'js-string' '' '') 'w.js' $css $champs) -eq 'champion') 'classement : nom de champion'
  Check ((Classify (& $mk "Ahri, Kog'Maw" 'js-string' '' '') 'w.js' $css $champs) -eq $null) 'classement : liste de noms sans texte'
  Check ((Classify (& $mk 'nav.home' 'js-string' 't(' '') 'w.js' $css $champs) -eq $null) 'classement : clé du catalogue'
  Check ((Classify (& $mk 'Échec de lecture' 'js-string' 'console.warn(' '') 'w.js' $css $champs) -eq 'journal') 'classement : journal'
  Check ((Classify (& $mk 'Description de test' 'cs-string' 't(true, ' 'AppTests') 'a.cs' $css $champs) -eq 'portee:test') 'classement : autotest natif'
  Check ((Classify (& $mk 'Quitter' 'cs-string' 'new ToolStripMenuItem(' 'CompanionApp') 'a.cs' $css $champs) -eq 'candidate') 'classement : menu natif d''un seul mot'
  Check ((Classify (& $mk 'Voir https://developer.riotgames.com/' 'cs-string' '' 'X') 'a.cs' $css $champs) -eq 'url') 'classement : adresse'
  Check ((Classify (& $mk 'Texte de test' 'cs-string' 'var s = ' 'CompanionApp' 'TestPage') 'a.cs' $css $champs) -eq 'portee:test') 'classement : méthode de test natif'
  if ($fails) { "$fails échec(s)"; exit 1 } else { 'Autotest du détecteur : tout est correct.'; exit 0 }
}

$watch = [Diagnostics.Stopwatch]::StartNew()
$r = Invoke-Detector $Root
$total = @($r.chaines).Count
"Racine : $Root"
"{0,-34} {1,8} {2,8}  {3}" -f 'Fichier', 'En dur', 'Exclues', 'Détail des exclusions'
foreach ($k in $r.fichiers.Keys) {
  $v = $r.fichiers[$k]
  if ($v.ignore) { "{0,-34} {1,8} {2,8}  ignoré : {3}" -f $k, '-', '-', $v.ignore; continue }
  $exs = ($v.exclues.Keys | ForEach-Object { "$_ $($v.exclues[$_])" }) -join ', '
  $nEx = 0; foreach ($e in $v.exclues.Values) { $nEx += $e }
  "{0,-34} {1,8} {2,8}  {3}" -f $k, $v.chaines, $nEx, $exs
}
"Total : $total chaîne(s) en dur hors exclusions, $(@($r.exclues).Count) exclue(s). Durée : $([math]::Round($watch.Elapsed.TotalSeconds, 1)) s."
if ($Detail) { $r.chaines | ForEach-Object { "{0}:{1} [{2}{3}] {4}" -f $_.fichier, $_.ligne, $_.type, $(if ($_.attribut) { "@$($_.attribut)" } else { '' }), $_.texte } }
if ($ShowExcluded) { $r.exclues | ForEach-Object { "{0}:{1} <{2}> {3}" -f $_.fichier, $_.ligne, $_.regle, $_.texte } }
if ($Json) { [IO.File]::WriteAllText($Json, ($r | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false)); "Rapport : $Json" }

$counted = $total
if ($Baseline -and (Test-Path $Baseline)) {
  $old = Get-Content -Raw -Encoding UTF8 $Baseline | ConvertFrom-Json
  $known = [Collections.Generic.HashSet[string]]::new()
  foreach ($c in $old.chaines) { [void]$known.Add($c.fichier + '|' + $c.texte) }
  $new = @($r.chaines | Where-Object { -not $known.Contains($_.fichier + '|' + $_.texte) })
  "Nouvelles par rapport à la référence : $($new.Count)"
  $new | ForEach-Object { "  {0}:{1} {2}" -f $_.fichier, $_.ligne, $_.texte }
  $counted = $new.Count
}
if ($Max -ge 0 -and $counted -gt $Max) { "Échec : $counted chaîne(s) en dur, maximum autorisé $Max."; exit 1 }
exit 0
