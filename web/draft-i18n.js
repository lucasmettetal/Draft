// Traduction des textes de la page : t(), tp(), tx(). Format des catalogues : docs/i18n/FORMAT-CATALOGUE.md.
// Chargé par une balise <script src> avant le script principal (CSP inchangée : ni eval, ni script en ligne nouveau).
// Les catalogues (i18n/fr.js, i18n/en.js) ne sont que des données : le programme les relit comme du JSON strict (DataScripts.Canonical).
// I18N-1 : tous les textes de la page passent par ici (index.html et moteur : I18N.t, tp, tx).
// I18N-2 : catalogue anglais complet. I18N-3 : l'anglais est proposé au joueur ; le programme passe la langue choisie (&lang=fr|en).
(function (root) {
  "use strict";
  // Langues proposées au joueur (I18N-3 : Réglages > Général > Langue). Sans paramètre, la page reste en français.
  const SUPPORTED = ["fr", "en"];
  // Pseudo-langue : mode test seulement, dans une copie de test (jamais livrée ni proposée).
  const TEST_ONLY = ["qps"];
  const CATALOGS = { fr: root.DRAFT_I18N_FR || {}, en: root.DRAFT_I18N_EN || null, qps: root.DRAFT_I18N_QPS || null };
  const params = new URLSearchParams(root.location ? root.location.search : "");
  // Mode test (&i18ncheck=1) : les clés et variables manquantes sont relevées (I18N.missing()) et signalées dans la console.
  const TEST = params.get("i18ncheck") === "1";
  const asked = params.get("lang");
  let lang = SUPPORTED.includes(asked) || (TEST && TEST_ONLY.includes(asked) && CATALOGS[asked]) ? asked : "fr";
  // Langue des formats (nombres, dates, pluriels) : la pseudo-langue suit le français.
  const locale = lang === "en" ? "en" : "fr";
  if (root.document && root.document.documentElement && lang !== "qps") root.document.documentElement.lang = lang;
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
  // Nombres : virgule décimale en français (comme avant l'extraction), point en anglais. Pas de séparateur de milliers ajouté.
  const fmtNumber = n => typeof n === "number" ? (locale === "en" ? String(n) : String(n).replace(".", ",")) : n;
  // Nombre à décimales fixes (« 1,5 » / « 1.5 ») : remplace les anciens coachNum et masteryDecimal, même rendu en français.
  const fmtDecimal = (n, digits) => { const s = n.toFixed(digits === undefined ? 1 : digits); return locale === "en" ? s : s.replace(".", ","); };
  // Une variable absente reste visible ({nom}) et est relevée : elle n'est jamais remplacée par une chaîne vide.
  function fill(template, vars, keep) {
    return parts(template).map(p => {
      if (typeof p === "string") return p;
      if (vars && own(vars, p.name)) return keep ? vars[p.name] : String(fmtNumber(vars[p.name]));
      note(`variable absente : ${p.name}`);
      return "{" + p.name + "}";
    });
  }
  const rules = new Intl.PluralRules(locale);
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
  // Dates et heures. Français : identiques au code d'avant (« 11/11/2026 », « 14:05 »).
  // Anglais : « Nov 11, 2026 » (mois écrit : pas d'ambiguïté jour/mois) et « 2:05 PM ».
  function fmtDate(d) { return locale === "en" ? new Date(d).toLocaleDateString("en-US", { day: "numeric", month: "short", year: "numeric" }) : new Date(d).toLocaleDateString("fr-FR"); }
  function fmtTime(d) { return locale === "en" ? new Date(d).toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" }) : new Date(d).toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" }); }
  // Listes : « A ou B ou C » (list.or appliqué de proche en proche) ; « A, B et C » (list.and). L'ordre des noms ne change jamais.
  function listOr(names) { return names.length ? names.reduce((a, b) => t("list.or", { a, b })) : ""; }
  function listAnd(names) { return names.length < 2 ? names.join("") : t("list.and", { first: names.slice(0, -1).join(t("list.comma")), last: names[names.length - 1] }); }
  // Casse d'une phrase du moteur placée en tête (majuscule) ou au milieu d'une autre phrase (minuscule).
  // Le français et l'anglais suivent la même règle (sentence case). Un nom de champion ou un sigle garde sa majuscule (voir coachReason).
  const upperFirst = s => s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
  const lowerFirst = s => s ? s.charAt(0).toLowerCase() + s.slice(1) : s;
  root.I18N = Object.freeze({ t, tp, tx, fmtDate, fmtTime, fmtNumber, fmtDecimal, listOr, listAnd, upperFirst, lowerFirst,
    get lang() { return lang; }, get locale() { return locale; }, get test() { return TEST; }, missing: () => Array.from(missing) });
})(window);
