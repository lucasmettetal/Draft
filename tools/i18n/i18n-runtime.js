// PROTOTYPE (tools/i18n, jamais chargé par DRAFT) des fonctions de traduction de la page. Voir docs/i18n/FORMAT-CATALOGUE.md.
// En I18N-1, ce code entre dans le script principal de web/index.html (aucun script en ligne nouveau, aucun eval).
(function (root) {
  "use strict";
  const SUPPORTED = ["fr", "en"];
  const CATALOGS = { fr: root.DRAFT_I18N_FR || {}, en: root.DRAFT_I18N_EN || {}, qps: root.DRAFT_I18N_QPS || null };
  const params = new URLSearchParams(root.location ? root.location.search : "");
  // Langue transmise par le programme (&lang=), liste blanche ; « qps » seulement en mode test.
  const TEST = params.get("i18ncheck") === "1";
  const asked = params.get("lang");
  let lang = SUPPORTED.includes(asked) || (TEST && asked === "qps" && CATALOGS.qps) ? asked : "fr";
  const missing = new Set();
  const own = (o, k) => Object.prototype.hasOwnProperty.call(o, k);

  function find(key) {
    const cur = CATALOGS[lang] || {};
    if (own(cur, key)) return cur[key];
    if (lang !== "fr") note(`clé absente (${lang}) : ${key}`);
    if (own(CATALOGS.fr, key)) return CATALOGS.fr[key];
    note(`clé absente (fr) : ${key}`);
    return null;
  }
  function note(what) {
    if (!TEST || missing.has(what)) return;
    missing.add(what);
    console.warn("[i18n] " + what);
  }
  // Découpe un gabarit en morceaux : texte et noms de variables. {{ et }} donnent une accolade.
  function parts(template) {
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
    return out;
  }
  const fmtNumber = n => typeof n === "number" ? (lang === "fr" ? String(n).replace(".", ",") : String(n)) : n;
  function fill(template, vars, keep) {
    return parts(template).map(p => {
      if (typeof p === "string") return p;
      if (vars && own(vars, p.name)) return keep ? vars[p.name] : String(fmtNumber(vars[p.name]));
      note(`variable absente : ${p.name}`);
      return "{" + p.name + "}";
    });
  }
  function pick(v, count) {
    if (v === null || typeof v === "string") return v;
    const cat = new Intl.PluralRules(lang === "qps" ? "fr" : lang).select(count);
    return own(v, cat) ? v[cat] : v.other;
  }
  // t : texte simple. tp : pluriel ({count} ajouté). tx : tableau de morceaux pour htm/React, une variable peut être un élément (nom en gras).
  function t(key, vars) { const v = pick(find(key), vars && vars.count); return v === null || v === undefined ? `[${key}]` : fill(v, vars, false).join(""); }
  function tp(key, count, vars) { return t(key, Object.assign({}, vars, { count })); }
  function tx(key, vars) { const v = pick(find(key), vars && vars.count); return v === null || v === undefined ? [`[${key}]`] : fill(v, vars, true).reduce((acc, x) => { if (typeof x === "string" && typeof acc[acc.length - 1] === "string") acc[acc.length - 1] += x; else acc.push(x); return acc; }, []).filter(x => x !== ""); }
  // Dates : identiques à aujourd'hui en français (toLocaleDateString("fr-FR")) ; format sans ambiguïté en anglais (Nov 11, 2026).
  function fmtDate(d) { return lang === "en" ? new Date(d).toLocaleDateString("en", { day: "numeric", month: "short", year: "numeric" }) : new Date(d).toLocaleDateString("fr-FR"); }
  root.I18N = { t, tp, tx, fmtDate, fmtNumber, get lang() { return lang; }, setLang(l) { if (SUPPORTED.includes(l)) lang = l; }, missing: () => Array.from(missing) };
  if (root.document) root.document.documentElement.lang = lang === "qps" ? "fr" : lang;
})(window);
