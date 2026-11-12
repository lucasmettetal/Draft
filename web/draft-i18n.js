// Traduction des textes de la page : t(), tp(), tx(). Format des catalogues : docs/i18n/FORMAT-CATALOGUE.md.
// Chargé par une balise <script src> avant le script principal (CSP inchangée : ni eval, ni script en ligne nouveau).
// Les catalogues (i18n/fr.js) ne sont que des données : le programme les relit comme du JSON strict (DataScripts.Canonical).
// I18N-1 : tous les textes de la page passent par ici (index.html et moteur : I18N.t, tp, tx). Seul le français existe.
(function (root) {
  "use strict";
  // Langues proposées. L'anglais n'est ajouté qu'une fois complet et testé (décision du 11/11/2026).
  const SUPPORTED = ["fr"];
  const CATALOGS = { fr: root.DRAFT_I18N_FR || {}, qps: root.DRAFT_I18N_QPS || null };
  const params = new URLSearchParams(root.location ? root.location.search : "");
  // Mode test (&i18ncheck=1) : les clés et variables manquantes sont relevées (I18N.missing()) et signalées dans la console.
  const TEST = params.get("i18ncheck") === "1";
  const asked = params.get("lang");
  let lang = SUPPORTED.includes(asked) || (TEST && asked === "qps" && CATALOGS.qps) ? asked : "fr";
  const missing = new Set();
  const own = (o, k) => Object.prototype.hasOwnProperty.call(o, k);

  function note(what) {
    if (missing.has(what)) return;
    missing.add(what);
    if (TEST && root.console) root.console.warn("[i18n] " + what);
  }
  // Langue choisie, puis français, sinon null (affiché « [clé] »).
  function find(key) {
    const cur = CATALOGS[lang] || {};
    if (own(cur, key)) return cur[key];
    if (lang !== "fr") note(`clé absente (${lang}) : ${key}`);
    if (own(CATALOGS.fr, key)) return CATALOGS.fr[key];
    note(`clé absente (fr) : ${key}`);
    return null;
  }
  // Découpe un gabarit en morceaux : texte et noms de variables. {{ et }} donnent une accolade.
  // Le moteur appelle t() des milliers de fois par draft : chaque gabarit n'est découpé qu'une fois.
  const parsed = new Map();
  function parts(template) {
    if (parsed.has(template)) return parsed.get(template);
    const out = [];
    let text = "";
    for (let i = 0; i < template.length; i++) {
      const c = template[i];
      if ((c === "{" || c === "}") && template[i + 1] === c) { text += c; i++; continue; }
      if (c === "{") {
        const end = template.indexOf("}", i + 1);
        if (end > i) { out.push(text); text = ""; out.push({ name: template.slice(i + 1, end).split(":")[0] }); i = end; continue; }
      }
      text += c;
    }
    out.push(text);
    parsed.set(template, out);
    return out;
  }
  const fmtNumber = n => typeof n === "number" ? (lang === "en" ? String(n) : String(n).replace(".", ",")) : n;
  // Une variable absente reste visible ({nom}) et est relevée : elle n'est jamais remplacée par une chaîne vide.
  function fill(template, vars, keep) {
    return parts(template).map(p => {
      if (typeof p === "string") return p;
      if (vars && own(vars, p.name)) return keep ? vars[p.name] : String(fmtNumber(vars[p.name]));
      note(`variable absente : ${p.name}`);
      return "{" + p.name + "}";
    });
  }
  const rules = new Intl.PluralRules(lang === "qps" ? "fr" : lang);
  function pick(v, count) {
    if (v === null || v === undefined || typeof v === "string") return v;
    const cat = rules.select(typeof count === "number" ? count : 0);
    return own(v, cat) ? v[cat] : v.other;
  }
  // t : texte simple. tp : pluriel ({count} fourni). tx : tableau de morceaux pour htm/React (une variable peut être un élément).
  function t(key, vars) { const v = pick(find(key), vars && vars.count); return v === null || v === undefined ? `[${key}]` : fill(v, vars, false).join(""); }
  function tp(key, count, vars) { return t(key, Object.assign({}, vars, { count })); }
  function tx(key, vars) {
    const v = pick(find(key), vars && vars.count);
    if (v === null || v === undefined) return [`[${key}]`];
    return fill(v, vars, true).reduce((acc, x) => { if (typeof x === "string" && typeof acc[acc.length - 1] === "string") acc[acc.length - 1] += x; else acc.push(x); return acc; }, []).filter(x => x !== "");
  }
  // Dates : identiques au code actuel en français.
  function fmtDate(d) { return new Date(d).toLocaleDateString("fr-FR"); }
  root.I18N = Object.freeze({ t, tp, tx, fmtDate, fmtNumber, get lang() { return lang; }, get test() { return TEST; }, missing: () => Array.from(missing) });
})(window);
