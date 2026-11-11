// Banc du prototype Strings.cs (tools/i18n/test-strings.ps1). Compilé avec src/DataScripts.cs, hors de DRAFT.
// Préfigure les contrôles à ajouter à --selftest en I18N-3 : chargement, repli, pluriels, variables, refus d'un catalogue piégé.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

namespace DraftMid
{
    // Seule dépendance de DataScripts.cs hors de ce banc.
    public static class HistoryImporter { public const string FileName = "draft-history.js"; }

    static class StringsHarness
    {
        static int fails;
        static void Check(bool ok, string name) { Console.WriteLine((ok ? "ok    " : "ÉCHEC ") + name); if (!ok) fails++; }

        static int Main(string[] args)
        {
            Console.OutputEncoding = new System.Text.UTF8Encoding(false);
            string frPath = args[0], enPath = args[1], table = args.Length > 2 ? args[2] : null;
            Func<string, string> read = code => code == "fr" ? File.ReadAllText(frPath) : code == "en" ? File.ReadAllText(enPath) : null;

            Check(Strings.Detect(new CultureInfo("fr-FR")) == "fr" && Strings.Detect(new CultureInfo("fr-CA")) == "fr" && Strings.Detect(new CultureInfo("fr-BE")) == "fr", "Langue : fr-* donne le français");
            Check(Strings.Detect(new CultureInfo("en-US")) == "en" && Strings.Detect(new CultureInfo("de-DE")) == "en" && Strings.Detect(CultureInfo.InvariantCulture) == "en", "Langue : toute autre langue donne l'anglais");
            Check(Strings.Resolve(null, new CultureInfo("fr-FR")) == "fr" && Strings.Resolve("auto", new CultureInfo("es-ES")) == "en", "Réglage absent ou auto : détection");
            Check(Strings.Resolve("en", new CultureInfo("fr-FR")) == "en" && Strings.Resolve("fr", new CultureInfo("ja-JP")) == "fr", "Réglage explicite : prioritaire sur Windows");
            Check(Strings.Resolve("de", new CultureInfo("fr-FR")) == "fr" && Strings.Resolve("<script>", new CultureInfo("en-US")) == "en", "Réglage inconnu : ignoré, détection");

            Strings.TestMode = true;
            Check(Strings.Load("fr", read) && Strings.Lang == "fr", "Chargement du français");
            Check(Strings.T("native.menu.quit") == "Quitter", "Texte simple (fr)");
            Check(Strings.Load("en", read) && Strings.Lang == "en", "Chargement de l'anglais");
            Check(Strings.T("native.menu.quit") == "Quit", "Texte simple (en)");
            Strings.Missing.Clear();
            Check(Strings.T("native.only.fr") == "Clé présente seulement en français (test du repli)" && Strings.Missing.Count == 1, "Clé absente en anglais : repli sur le français, signalé en mode test");
            Check(Strings.T("native.nexiste.pas") == "[native.nexiste.pas]", "Clé absente partout : [clé]");
            Hashtable v = new Hashtable { { "version", "3.0.1" }, { "current", "3.0.0" } };
            Check(Strings.T("native.dialog.updateAvailable", v).StartsWith("Version 3.0.1 is available (you have 3.0.0).\n\n"), "Variables nommées et retours à la ligne");
            Strings.Missing.Clear();
            Check(Strings.T("native.history.status.progress", new Hashtable { { "done", 3 } }) == "Importing history: 3 of {total} games" && Strings.Missing.Count == 1, "Variable absente : laissée visible et signalée");
            Check(Strings.T("literal.braces") == "Braces { and } are written doubled.", "Accolades doublées");
            Check(Strings.P("settings.journal.imported", 1, null) == "1 game added to your journal." && Strings.P("settings.journal.imported", 0, null) == "0 games added to your journal.", "Pluriel anglais : 1 / 0");
            Strings.Load("fr", read);
            Check(Strings.P("settings.journal.imported", 0, null) == "0 partie ajoutée à ton journal." && Strings.P("settings.journal.imported", 2, null) == "2 parties ajoutées à ton journal.", "Pluriel français : 0 et 1 au singulier");
            Check(Strings.P("settings.journal.imported", 1000000, null) == "1000000 parties ajoutées à ton journal.", "Pluriel français : « many » absent retombe sur « other »");
            Check(Strings.P("settings.journal.imported", 1.5, null) == "1,5 partie ajoutée à ton journal.", "Nombre décimal français : virgule");

            // Un catalogue piégé est refusé comme un fichier de données : le programme garde le français.
            Func<string, string> evil = code => code == "fr" ? File.ReadAllText(frPath) : "window.DRAFT_I18N_EN = {\"native.menu.quit\":\"x\"}; fetch('https://exemple.invalid');";
            Strings.Missing.Clear();
            Check(Strings.Load("en", evil) && Strings.Lang == "fr" && Strings.T("native.menu.quit") == "Quitter", "Catalogue piégé : refusé, repli sur le français");
            Check(Strings.Parse("en", "window.DRAFT_I18N_FR = {}") == null, "Catalogue sous un autre nom de variable : refusé");
            Check(Strings.Parse("fr", "window.DRAFT_I18N_FR = {\"__proto__\":{\"x\":1},\"a.b\":\"c\"};") != null && !Strings.Parse("fr", "window.DRAFT_I18N_FR = {\"__proto__\":{\"x\":1},\"a.b\":\"c\"};").ContainsKey("__proto__"), "Clé __proto__ retirée");
            Check(!Strings.Load("fr", c => "alert(1)"), "Français illisible : échec signalé (DRAFT refuserait de démarrer en --selftest)");

            // Même règle de pluriel que la page (table relevée dans Chrome par run-i18n-tests.ps1).
            if (table != null && File.Exists(table))
            {
                Dictionary<string, object> t = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(table)) as Dictionary<string, object>;
                int diff = 0, n = 0;
                foreach (string code in new[] { "fr", "en" })
                {
                    Dictionary<string, object> row = t[code] as Dictionary<string, object>;
                    foreach (KeyValuePair<string, object> p in row)
                    {
                        n++;
                        string mine = Strings.PluralCategory(code, double.Parse(p.Key, CultureInfo.InvariantCulture));
                        if (mine != (string)p.Value) { diff++; Console.WriteLine("  " + code + " " + p.Key + " : page " + p.Value + ", programme " + mine); }
                    }
                }
                Check(diff == 0 && n > 0, "Pluriels : programme identique à Intl.PluralRules sur " + n + " nombres");
            }
            Console.WriteLine(fails == 0 ? "Prototype Strings : tout est correct." : fails + " échec(s).");
            return fails == 0 ? 0 : 1;
        }
    }
}
