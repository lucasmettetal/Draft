// Textes du programme (I18N-3) : menu de l'icône, bulles, boîtes de message, états envoyés à la page, diagnostic.
// Le programme lit le même catalogue que la page (web/i18n/fr.js, en.js, clés native.*), avec le même parseur strict que
// les fichiers de données (DataScripts.Canonical) : un catalogue qui contiendrait autre chose qu'un objet JSON est refusé.
// Repli : langue choisie, puis français, puis « [clé] ». En mode test (--selftest, --e2e), chaque absence est notée.
// Écrit en C# 5 pour le compilateur de Windows utilisé par build.ps1 (csc .NET Framework 4). Format : docs/i18n/FORMAT-CATALOGUE.md.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace DraftMid
{
    public static class Strings
    {
        public static readonly string[] Supported = { "fr", "en" };
        static Dictionary<string, object> reference = new Dictionary<string, object>();
        static Dictionary<string, object> current = new Dictionary<string, object>();
        static readonly Dictionary<string, Dictionary<string, object>> loaded = new Dictionary<string, Dictionary<string, object>>();
        static string lang = "fr";
        // Mode test (--selftest, --e2e) : chaque clé ou variable absente est notée (Missing).
        public static bool TestMode;
        public static readonly List<string> Missing = new List<string>();
        public static string Lang { get { return lang; } }
        public static event Action Changed;

        // Langue de Windows : fr-* donne le français, tout le reste l'anglais.
        public static string Detect(CultureInfo ui)
        {
            return ui != null && ui.TwoLetterISOLanguageName == "fr" ? "fr" : "en";
        }

        // Réglage enregistré (settings.json, champ Language) : "fr", "en", "auto" ou absent (installations 2.x) : détection.
        public static string Resolve(string setting, CultureInfo ui)
        {
            if (setting != null && Array.IndexOf(Supported, setting) >= 0) return setting;
            return Detect(ui);
        }

        // Valeur acceptée pour le réglage : une langue livrée, ou "auto". Toute autre valeur redevient "auto".
        public static string NormalizeSetting(string setting)
        {
            return setting != null && Array.IndexOf(Supported, setting) >= 0 ? setting : "auto";
        }

        public static string VariableName(string code) { return "DRAFT_I18N_" + code.ToUpperInvariant(); }

        // Null si le texte n'est pas exactement un catalogue de données (même règle que les fichiers de la page).
        public static Dictionary<string, object> Parse(string code, string text)
        {
            string safe = DataScripts.Canonical(VariableName(code), text);
            if (safe == null) return null;
            int eq = safe.IndexOf('=');
            string body = safe.Substring(eq + 1).Trim().TrimEnd(';');
            try { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 8 }.DeserializeObject(body) as Dictionary<string, object>; }
            catch (Exception e) { if (e is OutOfMemoryException) throw; return null; }
        }

        // Catalogue intégré à l'exe (ressource « web/i18n/{code}.js »).
        public static string Embedded(string code)
        {
            using (Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("web/i18n/" + code + ".js"))
            {
                if (s == null) return null;
                using (StreamReader r = new StreamReader(s, Encoding.UTF8)) return r.ReadToEnd();
            }
        }

        // read(code) renvoie le texte du catalogue. Faux si le français manque : le programme garde alors « [clé] ».
        public static bool Load(string code, Func<string, string> read)
        {
            Dictionary<string, object> fr = Parse("fr", read("fr"));
            if (fr == null) { Note("catalogue fr refusé"); return false; }
            loaded.Clear();
            loaded["fr"] = fr;
            reference = fr; current = fr; lang = "fr";
            foreach (string other in Supported.Where(c => c != "fr"))
            {
                Dictionary<string, object> cat = Parse(other, read(other));
                if (cat != null) loaded[other] = cat; else Note("catalogue " + other + " refusé : repli sur le français");
            }
            Use(code);
            return true;
        }

        // Change la langue du programme sans relire les fichiers. Une langue absente ou refusée garde le français.
        public static void Use(string code)
        {
            Dictionary<string, object> cat;
            string before = lang;
            if (code != null && loaded.TryGetValue(code, out cat)) { current = cat; lang = code; }
            else { current = reference; lang = "fr"; if (code != null && code != "fr") Note("langue " + code + " indisponible : repli sur le français"); }
            if (before != lang) { Action a = Changed; if (a != null) a(); }
        }

        static void Note(string what) { if (TestMode) lock (Missing) if (!Missing.Contains(what)) Missing.Add(what); }

        static object Find(string key)
        {
            object v;
            if (current.TryGetValue(key, out v)) return v;
            if (lang != "fr") Note("clé absente (" + lang + ") : " + key);
            if (reference.TryGetValue(key, out v)) return v;
            Note("clé absente (fr) : " + key);
            return null;
        }

        public static bool Has(string code, string key) { Dictionary<string, object> c; return loaded.TryGetValue(code, out c) && c.ContainsKey(key); }

        // Texte d'une clé. Variables par paires : T("native.update.available", "version", "3.0.1", "current", "3.0.0").
        // Une variable « count » choisit la forme de pluriel, comme t() dans la page.
        public static string T(string key, params object[] pairs) { return Text(key, Pairs(pairs)); }

        public static IDictionary Pairs(object[] pairs)
        {
            Hashtable vars = new Hashtable();
            if (pairs != null) for (int i = 0; i + 1 < pairs.Length; i += 2) vars[pairs[i]] = pairs[i + 1];
            return vars;
        }

        public static string Text(string key, IDictionary vars)
        {
            object v = Find(key);
            if (v == null) return "[" + key + "]";
            string s = v as string;
            if (s == null)
            {
                Dictionary<string, object> forms = v as Dictionary<string, object>;
                object form = null;
                if (forms != null)
                {
                    object count = vars != null && vars.Contains("count") ? vars["count"] : null;
                    double n = count == null ? 0 : Convert.ToDouble(count, CultureInfo.InvariantCulture);
                    if (!forms.TryGetValue(PluralCategory(lang, n), out form)) forms.TryGetValue("other", out form);
                }
                s = form as string;
            }
            return s == null ? "[" + key + "]" : Format(s, vars);
        }

        // Règles CLDR (celles d'Intl.PluralRules) pour les langues livrées. Comparées à la page par --selftest (table relevée dans Chrome).
        public static string PluralCategory(string code, double n)
        {
            double a = Math.Abs(n);
            bool integer = a == Math.Floor(a);
            if (code == "fr")
            {
                if (a < 2) return "one";                                  // « 0 partie », « 1,5 partie »
                if (integer && a != 0 && a % 1000000 == 0) return "many"; // « 1 000 000 de parties »
                return "other";
            }
            if (code == "en") return a == 1 && integer ? "one" : "other";
            return "other";
        }

        // Nombres : virgule décimale en français, point en anglais, sans séparateur de milliers (comme la page).
        public static string Number(double n)
        {
            string s = n.ToString("0.##", CultureInfo.InvariantCulture);
            return lang == "fr" ? s.Replace('.', ',') : s;
        }

        // {nom} remplacé par sa valeur ; {{ et }} donnent une accolade. Une variable absente reste visible et est notée en mode test.
        // Une valeur Msg est traduite à son tour (message dans un message).
        public static string Format(string template, IDictionary vars)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];
                if (c == '{' && i + 1 < template.Length && template[i + 1] == '{') { sb.Append('{'); i++; continue; }
                if (c == '}' && i + 1 < template.Length && template[i + 1] == '}') { sb.Append('}'); i++; continue; }
                if (c == '{')
                {
                    int end = template.IndexOf('}', i + 1);
                    if (end > i)
                    {
                        string name = template.Substring(i + 1, end - i - 1);
                        int colon = name.IndexOf(':');
                        if (colon >= 0) name = name.Substring(0, colon);
                        if (vars != null && vars.Contains(name))
                        {
                            object val = vars[name];
                            sb.Append(val is Msg ? val.ToString() : val is double || val is float || val is decimal ? Number(Convert.ToDouble(val)) : Convert.ToString(val, CultureInfo.InvariantCulture));
                        }
                        else { Note("variable absente : " + name); sb.Append(template, i, end - i + 1); }
                        i = end; continue;
                    }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static IEnumerable<string> Keys { get { return reference.Keys; } }
    }

    // Message du programme : une clé du catalogue et ses variables. Traduit au moment de l'affichage (ToString), dans la langue
    // du moment ; envoyé à la page tel quel ({ key, vars }), pour qu'elle l'affiche dans sa langue (nativeText, index.html).
    public sealed class Msg
    {
        public readonly string Key;
        public readonly Dictionary<string, object> Vars = new Dictionary<string, object>();
        public Msg(string key, params object[] pairs)
        {
            Key = key;
            if (pairs != null) for (int i = 0; i + 1 < pairs.Length; i += 2) Vars[(string)pairs[i]] = pairs[i + 1];
        }
        public override string ToString() { return Strings.Text(Key, Vars); }
        // Forme envoyée à la page : seulement des clés, des nombres et des textes techniques (noms, versions, codes).
        public Dictionary<string, object> Json()
        {
            Dictionary<string, object> vars = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> v in Vars) vars[v.Key] = v.Value is Msg ? (object)((Msg)v.Value).Json() : v.Value;
            return new Dictionary<string, object> { { "key", Key }, { "vars", vars } };
        }
        public static object JsonOrEmpty(Msg m) { return m == null ? (object)"" : m.Json(); }
        public static string TextOrEmpty(Msg m) { return m == null ? "" : m.ToString(); }
    }
}
