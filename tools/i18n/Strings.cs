// PROTOTYPE (tools/i18n, jamais compilé dans DRAFT) de la lecture du catalogue côté programme. Voir docs/i18n/STRATEGIE.md § 2.
// Le programme lit le même fichier que la page (web/i18n/fr.js, en.js), avec le même parseur strict que les données
// (DataScripts.Canonical) : un catalogue qui contiendrait autre chose qu'un objet JSON est refusé.
// Écrit en C# 5 pour le compilateur de Windows utilisé par build.ps1 (csc .NET Framework 4).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;

namespace DraftMid
{
    public static class Strings
    {
        public static readonly string[] Supported = { "fr", "en" };
        static Dictionary<string, object> reference = new Dictionary<string, object>();
        static Dictionary<string, object> current = new Dictionary<string, object>();
        static string lang = "fr";
        // Mode test (--selftest, pages de test) : chaque clé ou variable absente est notée.
        public static bool TestMode;
        public static readonly List<string> Missing = new List<string>();
        public static string Lang { get { return lang; } }

        // Langue de Windows au premier lancement : fr-* donne le français, tout le reste l'anglais.
        public static string Detect(CultureInfo ui)
        {
            return ui != null && ui.TwoLetterISOLanguageName == "fr" ? "fr" : "en";
        }

        // Réglage enregistré (settings.json, champ Language) : "fr", "en", "auto" ou absent (anciennes installations).
        public static string Resolve(string setting, CultureInfo ui)
        {
            if (setting != null && Array.IndexOf(Supported, setting) >= 0) return setting;
            return Detect(ui);
        }

        public static string VariableName(string code) { return "DRAFT_I18N_" + code.ToUpperInvariant(); }

        // Null si le texte n'est pas exactement un catalogue de données (même règle que les fichiers de la page).
        public static Dictionary<string, object> Parse(string code, string text)
        {
            string safe = DataScripts.Canonical(VariableName(code), text);
            if (safe == null) return null;
            int eq = safe.IndexOf('=');
            string body = safe.Substring(eq + 1).Trim().TrimEnd(';');
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 8 }.DeserializeObject(body) as Dictionary<string, object>;
        }

        // read(code) renvoie le texte du catalogue (ressource intégrée « web/i18n/{code}.js » dans DRAFT). Faux si le français manque.
        public static bool Load(string code, Func<string, string> read)
        {
            Dictionary<string, object> fr = Parse("fr", read("fr"));
            if (fr == null) return false;
            reference = fr; current = fr; lang = "fr";
            if (code != "fr" && Array.IndexOf(Supported, code) >= 0)
            {
                Dictionary<string, object> other = Parse(code, read(code));
                if (other != null) { current = other; lang = code; }
                else Note("catalogue " + code + " refusé : repli sur le français");
            }
            return true;
        }

        static void Note(string what) { if (TestMode && !Missing.Contains(what)) Missing.Add(what); }

        static object Find(string key)
        {
            object v;
            if (current.TryGetValue(key, out v)) return v;
            Note("clé absente (" + lang + ") : " + key);
            if (reference.TryGetValue(key, out v)) return v;
            Note("clé absente (fr) : " + key);
            return null;
        }

        public static string T(string key) { return T(key, null); }

        public static string T(string key, IDictionary vars)
        {
            object v = Find(key);
            if (v == null) return "[" + key + "]";
            string s = v as string;
            if (s == null) { Dictionary<string, object> forms = v as Dictionary<string, object>; object o; s = forms != null && forms.TryGetValue("other", out o) ? o as string : null; }
            return s == null ? "[" + key + "]" : Format(s, vars);
        }

        // Pluriel : forme choisie comme Intl.PluralRules côté page ; « count » est ajouté aux variables.
        public static string P(string key, double count, IDictionary vars)
        {
            Hashtable all = new Hashtable();
            if (vars != null) foreach (DictionaryEntry e in vars) all[e.Key] = e.Value;
            all["count"] = count;
            object v = Find(key);
            Dictionary<string, object> forms = v as Dictionary<string, object>;
            if (forms == null) return v is string ? Format((string)v, all) : "[" + key + "]";
            object form;
            string cat = PluralCategory(lang, count);
            if (!forms.TryGetValue(cat, out form) && !forms.TryGetValue("other", out form)) return "[" + key + "]";
            return Format((string)form, all);
        }

        // Règles CLDR (celles d'Intl.PluralRules) pour les langues livrées. Vérifiées contre la page par tools/i18n/run-i18n-tests.ps1.
        public static string PluralCategory(string code, double n)
        {
            double a = Math.Abs(n);
            bool integer = a == Math.Floor(a);
            if (code == "fr")
            {
                if (a < 2) return "one";                                  // i = 0 ou 1 : « 0 partie », « 1,5 partie »
                if (integer && a % 1000000 == 0) return "many";            // « 1 000 000 de parties »
                return "other";
            }
            if (code == "en") return a == 1 && integer ? "one" : "other";
            return "other";
        }

        // Nombres : virgule décimale en français, point en anglais (comme le texte actuel, sans séparateur de milliers ajouté).
        public static string Number(double n)
        {
            string s = n.ToString("0.##", CultureInfo.InvariantCulture);
            return lang == "fr" ? s.Replace('.', ',') : s;
        }

        // {nom} remplacé par sa valeur ; {{ et }} donnent une accolade. Une variable absente reste visible et est notée en mode test.
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
                            sb.Append(val is double || val is float || val is decimal ? Number(Convert.ToDouble(val)) : Convert.ToString(val, CultureInfo.InvariantCulture));
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
}
