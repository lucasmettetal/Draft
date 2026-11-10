// Fichiers de données chargés par la page (draft-stats.js, draft-history.js, rôles et matchups calculés).
// La page les charge comme des scripts : avant de les lui donner, DRAFT les relit comme du JSON strict et les réécrit lui-même.
// Rien d'autre que des données ne peut donc atteindre la page, quel que soit le contenu du fichier d'origine.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace DraftMid
{
    public static class DataScripts
    {
        public const int MaxChars = 64 * 1024 * 1024;

        // Fichier de la page et nom de la variable qu'il définit.
        public static readonly Dictionary<string, string> Files = new Dictionary<string, string>
        {
            { "draft-stats.js", "DRAFT_STATS" },
            { HistoryImporter.FileName, "DRAFT_HISTORY" },
            { "draft-role-rates-live.js", "ROLE_RATES_LIVE" },
            { "draft-matchups-live.js", "MATCHUPS_LIVE" }
        };

        static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 64 }; }

        // Null si le texte n'est pas exactement « window.NOM = {objet JSON}; », précédé au plus de commentaires « // ».
        public static string Canonical(string name, string text)
        {
            if (text == null || text.Length > MaxChars) return null;
            int i = 0;
            while (true)
            {
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '/')
                {
                    int end = text.IndexOf('\n', i);
                    if (end < 0) return null;
                    i = end + 1;
                    continue;
                }
                break;
            }
            string head = "window." + name;
            if (string.CompareOrdinal(text, i, head, 0, head.Length) != 0) return null;
            i += head.Length;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i >= text.Length || text[i] != '=') return null;
            string body = text.Substring(i + 1).Trim();
            if (body.EndsWith(";")) body = body.Substring(0, body.Length - 1).TrimEnd();
            if (!body.StartsWith("{") || !body.EndsWith("}")) return null;
            Dictionary<string, object> value;
            try { value = Json().DeserializeObject(body) as Dictionary<string, object>; }
            catch (Exception e) { if (e is OutOfMemoryException) throw; return null; }
            if (value == null) return null;
            return "window." + name + " = " + Json().Serialize(Clean(value)) + ";\n";
        }

        // Une clé __proto__ changerait le prototype de l'objet créé par la page : elle est retirée.
        static object Clean(object v)
        {
            Dictionary<string, object> d = v as Dictionary<string, object>;
            if (d != null) return d.Where(p => p.Key != "__proto__").ToDictionary(p => p.Key, p => Clean(p.Value));
            object[] a = v as object[];
            if (a != null) return a.Select(Clean).ToArray();
            return v;
        }

        public static string ReadCanonical(string name, string path)
        {
            FileInfo f = new FileInfo(path);
            if (!f.Exists || f.Length > MaxChars) return null;
            return Canonical(name, File.ReadAllText(path));
        }

        // Copie vers le dossier de la page, sous forme sûre. Un fichier refusé n'y est jamais laissé.
        public static bool Publish(string fileName, string dataDir, string webDir)
        {
            string source = Path.Combine(dataDir, fileName), target = Path.Combine(webDir, fileName);
            string safe = File.Exists(source) ? ReadCanonical(Files[fileName], source) : null;
            if (safe == null)
            {
                if (File.Exists(target)) File.Delete(target);
                return !File.Exists(source);
            }
            File.WriteAllText(target, safe, new System.Text.UTF8Encoding(false));
            return true;
        }
    }

    static class DataScriptsTests
    {
        public static void Run(Action<bool, string> t, string dataDir)
        {
            Func<string, string> stats = text => DataScripts.Canonical("DRAFT_STATS", text);
            t(stats("window.DRAFT_STATS = {\"rows\":[{\"champ\":\"Ahri\",\"win\":true,\"k\":3}]};") == "window.DRAFT_STATS = {\"rows\":[{\"champ\":\"Ahri\",\"win\":true,\"k\":3}]};\n", "Données de la page : un fichier valide est accepté tel quel");
            t(stats("// généré le 05/11\n// par l'outil\nwindow.DRAFT_STATS={\"rows\":[]}") != null, "Données de la page : commentaires et point-virgule facultatifs acceptés");
            t(stats("window.DRAFT_STATS = {\"rows\":[]}; window.chrome.webview.postMessage({cmd:\"setDiagnostics\",value:true});") == null, "Données de la page : code ajouté après les données refusé");
            t(stats("window.DRAFT_STATS = {\"rows\":[]}, alert(1);") == null && stats("window.DRAFT_STATS = (function(){return {}})();") == null, "Données de la page : expression ou fonction refusées");
            t(stats("alert(1); window.DRAFT_STATS = {\"rows\":[]};") == null && stats("window.DRAFT_STATSX = {}") == null && stats("") == null && stats(null) == null, "Données de la page : code avant les données, autre variable ou fichier vide refusés");
            t(stats("window.DRAFT_STATS = {\"rows\":[\"</script><script>alert(1)</script>\"]};").Contains("\\u003c/script\\u003e"), "Données de la page : chevrons neutralisés dans les textes");
            string proto = stats("window.DRAFT_STATS = {\"__proto__\":{\"x\":1},\"rows\":[{\"__proto__\":{}}]};");
            t(proto != null && !proto.Contains("__proto__"), "Données de la page : clés __proto__ retirées");
            t(stats("window.DRAFT_STATS = [1,2];") == null && stats("window.DRAFT_STATS = \"texte\";") == null, "Données de la page : seul un objet est accepté");
            t(stats("window.DRAFT_STATS = " + string.Concat(Enumerable.Repeat("{\"a\":", 200)) + "1" + new string('}', 200) + ";") == null, "Données de la page : imbrication excessive refusée sans planter");

            string dir = Path.Combine(dataDir, "test-donnees"), web = Path.Combine(dir, "web");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(web);
            File.WriteAllText(Path.Combine(dir, "draft-stats.js"), "window.DRAFT_STATS = {\"rows\":[]}; fetch('https://exemple.test');");
            File.WriteAllText(Path.Combine(web, "draft-stats.js"), "ancienne copie");
            t(!DataScripts.Publish("draft-stats.js", dir, web) && !File.Exists(Path.Combine(web, "draft-stats.js")) && File.Exists(Path.Combine(dir, "draft-stats.js")), "Données de la page : fichier piégé ni copié, ni laissé, ni effacé des données");
            File.WriteAllText(Path.Combine(dir, "draft-matchups-live.js"), "// Bilans\nwindow.MATCHUPS_LIVE = {\"role\":\"mid\",\"pool\":{}};\n");
            t(DataScripts.Publish("draft-matchups-live.js", dir, web) && File.ReadAllText(Path.Combine(web, "draft-matchups-live.js")).StartsWith("window.MATCHUPS_LIVE = {"), "Données de la page : fichier valide publié");
            t(DataScripts.Publish("draft-role-rates-live.js", dir, web), "Données de la page : fichier absent sans erreur");
            Directory.Delete(dir, true);
        }
    }
}
