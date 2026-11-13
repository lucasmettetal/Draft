// Outils communs des maquettes : langue, textes (même logique que draft-i18n.js), formats, portraits, coque de l'application.
// Paramètres d'adresse : ?etat=vide|drafts5|riot93|riche&lang=fr|en
(function () {
  "use strict";
  const P = new URLSearchParams(location.search);
  const LANG = P.get("lang") === "en" ? "en" : "fr";
  const ETATS = ["vide", "drafts5", "riot93", "riche"];
  const ETAT = ETATS.includes(P.get("etat")) ? P.get("etat") : "riot93";
  const TXT = window.MAQ_TEXTES[LANG];
  const plural = new Intl.PluralRules(LANG);
  const missing = new Set();

  const esc = s => String(s).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  // Comme draft-i18n.js : seuls les nombres prennent la virgule ; un patch (« 16.22 ») reste une chaîne.
  const fmtNumber = n => typeof n === "number" ? (LANG === "en" ? String(n) : String(n).replace(".", ",")) : n;
  function raw(key) {
    if (!(key in TXT)) { missing.add(key); return "[" + key + "]"; }
    return TXT[key];
  }
  function fill(s, vars, html) {
    return s.replace(/\{\{|\}\}|\{([a-zA-Z0-9]+)\}/g, (m, name) => {
      if (m === "{{") return "{"; if (m === "}}") return "}";
      if (!vars || !(name in vars)) return m;
      const v = vars[name];
      return html && v && v.__html ? v.__html : (html ? esc(fmtNumber(v)) : String(fmtNumber(v)));
    });
  }
  const pick = (key, count) => { const v = raw(key); return typeof v === "string" ? v : (v[plural.select(count)] || v.other); };
  // t : texte brut. tp : pluriel. tx : HTML, variables déjà en HTML via h().
  const t = (key, vars) => { const v = raw(key); return fill(typeof v === "string" ? v : v.other, vars, false); };
  const tp = (key, count, vars) => fill(pick(key, count), Object.assign({ count }, vars || {}), false);
  const tx = (key, vars, count) => {
    const s = count === undefined ? (typeof raw(key) === "string" ? raw(key) : pick(key, 1)) : pick(key, count);
    const v = Object.assign(count === undefined ? {} : { count }, vars || {});
    // Le texte hors variables est échappé ; les variables HTML passent telles quelles.
    return s.split(/(\{[a-zA-Z0-9]+\})/).map(part => /^\{[a-zA-Z0-9]+\}$/.test(part) ? fill(part, v, true) : esc(part)).join("");
  };
  const h = s => ({ __html: s });
  const fmtDate = ms => LANG === "en" ? new Date(ms).toLocaleDateString("en-US", { day: "numeric", month: "short", year: "numeric" }) : new Date(ms).toLocaleDateString("fr-FR");
  const pct = rate => t("stats.percent", { value: Math.round(rate * 100) });
  const dec = (x, n) => { const s = x.toFixed(n === undefined ? 1 : n); return LANG === "en" ? s : s.replace(".", ","); };
  const record = b => t("stats.record", { wins: b.victoires, losses: b.defaites });
  const mins = sec => t("stats.minutes", { count: Math.round(sec / 60) });

  // Noms : l'import garde les clés de l'API Riot (LeeSin, Leblanc) ; le journal garde les noms affichés.
  const NOMS = { LeeSin: "Lee Sin", Nunu: "Nunu & Willump", AurelionSol: "Aurelion Sol", JarvanIV: "Jarvan IV", XinZhao: "Xin Zhao", MissFortune: "Miss Fortune",
    Kaisa: "Kai'Sa", KSante: "K'Sante", Renata: "Renata Glasc", Leblanc: "LeBlanc", Khazix: "Kha'Zix", Velkoz: "Vel'Koz" };
  const DD = Object.fromEntries(Object.entries(NOMS).map(([k, v]) => [v, k]));
  const nom = key => NOMS[key] || key;
  const ddId = name => DD[name] || name.replace(/[^A-Za-z0-9]/g, "");
  const DD_VERSION = "16.22.1";
  const ROLE_OF_POS = { TOP: "top", JUNGLE: "jungle", MIDDLE: "mid", BOTTOM: "adc", UTILITY: "support" };
  const roleName = role => role && role !== "none" ? t("role.name." + role) : t("stats.roles.none");

  function portrait(name, size, opts) {
    opts = opts || {};
    const n = nom(name), cls = "portrait" + (opts.advice ? " advice" : "") + (opts.thin ? " thin" : "");
    const slot = opts.role && !opts.advice ? ` data-slot="${opts.role}"` : "";
    return `<span class="${cls}"${slot} style="width:${size}px;height:${size}px" title="${esc(n)}"><span class="init" aria-hidden="true">${esc(n.slice(0, 2))}</span>` +
      `<img src="https://ddragon.leagueoflegends.com/cdn/${DD_VERSION}/img/champion/${ddId(n)}.png" alt="" onerror="this.remove()"></span>`;
  }
  const ROLE_PATHS = {
    top: "M3 3h14v4H7v10H3z M11 11h6v6h-6z", jungle: "M10 2c1 5 5 6 5 11a5 5 0 0 1-10 0c0-5 4-6 5-11z",
    mid: "M3 17 15 3h2v2L5 17z M3 3h6v3H6v3H3z M17 17h-6v-3h3v-3h3z", adc: "M17 17H3v-4h10V3h4z M3 3h6v6H3z", support: "M10 2l7 3v5c0 4-3 7-7 8-4-1-7-4-7-8V5z"
  };
  const roleIcon = role => ROLE_PATHS[role] ? `<span class="role-ico" data-slot="${role}"><svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="${ROLE_PATHS[role]}"/></svg></span>` : "";
  const MIN = window.MAQ_CALCULS.SEUILS.pourcentage;
  function gauge(count, min) {
    min = min || MIN;
    const on = Math.min(count, min);
    return `<span class="notches" role="img" aria-label="${esc(tp("stats.gauge.aria", count, { min }))}">${Array.from({ length: min }, (_, i) => `<i class="${i < on ? "on" : ""}"></i>`).join("")}</span>`;
  }
  // Barre de victoires : au-dessus du seuil seulement ; sinon la jauge. Sous 50 %, la barre quitte le vert (règle QA-1).
  function winbar(b) {
    if (b.parties < MIN) return gauge(b.parties);
    return `<span class="wbar${b.taux < 0.5 ? " under" : ""}" role="img" aria-label="${esc(record(b) + ", " + pct(b.taux))}"><i style="width:${(b.taux * 100).toFixed(1)}%"></i><b></b></span>`;
  }

  const NAV = { home: "M3.5 9.5 10 4l6.5 5.5V16a1 1 0 0 1-1 1H12v-4.5H8V17H4.5a1 1 0 0 1-1-1z", pool: "M3.5 3.5h5v5h-5zM11.5 3.5h5v5h-5zM3.5 11.5h5v5h-5zM11.5 11.5h5v5h-5z",
    sim: "M6.5 4.2v11.6L16 10z", stats: "M4 16.5v-6M10 16.5v-12M16 16.5v-8",
    settings: "M10 7a3 3 0 1 0 0 6 3 3 0 1 0 0-6zM10 2v2.2M10 15.8V18M2 10h2.2M15.8 10H18M4.3 4.3l1.6 1.6M14.1 14.1l1.6 1.6M4.3 15.7l1.6-1.6M14.1 5.9l1.6-1.6" };
  const navIcon = n => `<svg class="nav-icon" viewBox="0 0 20 20" aria-hidden="true"><path d="${NAV[n]}" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>`;
  const LOGO = `<svg viewBox="0 0 24 24" aria-hidden="true"><path fill="none" stroke="currentColor" stroke-width="2.2" stroke-linejoin="round" d="M4 20V9l6-5h10v5l-6 11z M9 15l6-6"/></svg>`;

  function shell(proposal, content) {
    const link = (etat, lang) => `?etat=${etat}&lang=${lang}`;
    const states = ETATS.map(e => `<a class="${e === ETAT ? "on" : ""}" href="${link(e, LANG)}">${esc(t("maquette.state." + e))}</a>`).join("");
    const langs = ["fr", "en"].map(l => `<a class="${l === LANG ? "on" : ""}" href="${link(ETAT, l)}">${l.toUpperCase()}</a>`).join("");
    const item = (v, key) => `<button type="button" class="nav-link"${v === "stats" ? ' aria-current="page"' : ""}>${navIcon(v)}<span>${esc(t(key))}</span></button>`;
    document.documentElement.lang = LANG;
    document.body.innerHTML = `
<div class="maq-band" role="note"><span class="maq-flag">${esc(t("maquette.band"))}</span><span class="maq-flag">${esc(t("maquette.bandEn"))}</span>
  <span class="maq-what">${esc(t(proposal === "A" ? "maquette.proposalA" : "maquette.proposalB"))}</span><span class="maq-state">${esc(t("maquette.state." + ETAT))}</span>
  <span class="grow"></span>${states}<span class="maq-state">·</span>${langs}</div>
<div class="shell">
  <nav class="nav" aria-label="Navigation">
    <div class="nav-brand">${LOGO}<span class="brand">DRAFT</span><small>${esc(t("nav.beta"))}</small></div>
    <div class="nav-list">${item("home", "nav.home")}${item("pool", "nav.pool")}${item("sim", "nav.sim")}${item("stats", "nav.stats")}</div>
    <span class="nav-grow"></span>${item("settings", "nav.settings")}
    <div class="nav-state"><p class="nav-line"><i class="nav-dot"></i><span class="lbl">${esc(t("nav.state.league"))}</span><span class="val">${esc(t("nav.state.lcu.off"))}</span></p>
      <p class="nav-line"><span class="nav-role-icon">${roleIcon("mid").replace('class="role-ico"', 'class="role-ico" style="color:var(--text)"')}</span><span class="lbl">${esc(t("nav.state.role"))}</span><span class="val">${esc(t("role.name.mid"))}</span></p></div>
    <p class="nav-legal">${LANG === "en" ? "Version 3.0 beta. Not endorsed by Riot Games." : "Version 3.0 bêta. Non approuvé par Riot Games."}</p>
  </nav>
  <main class="view" data-scroll><div class="view-page">${content}</div></main>
</div>`;
    if (missing.size) console.warn("Clés absentes : " + [...missing].join(", "));
    window.MAQ_MISSING = [...missing];
  }

  // Conclusions mises en texte. La page n'interprète rien : elle lit le statut et les chiffres de MAQ_CALCULS.conclusions.
  // Renvoie { tone: good|threat|neutral|wait, badge, title, proof, rule, have, need }. scope : "riot" ou "journal".
  function describe(found, scope) {
    const out = [], flatChamps = [], flatDuels = [];
    const rec = m => record({ victoires: m.victoires, defaites: m.parties - m.victoires });
    found.forEach(f => {
      if ((scope === "journal") !== (f.type === "conseil")) return;
      const role = roleName(f.role);
      if (f.type === "champion" && f.statut === "etabli") out.push({ tone: f.sens === "fort" ? "good" : "threat", badge: t("stats.findings.established"),
        title: t(f.sens === "fort" ? "stats.findings.strength" : "stats.findings.weakness", { champion: nom(f.sujet), role }),
        proof: t("stats.findings.championProof", { record: rec(f.mesure), games: f.mesure.parties, rate: pct(f.mesure.taux), low: pct(f.mesure.intervalle[0]), high: pct(f.mesure.intervalle[1]), role, ref: pct(f.reference.taux), refGames: f.reference.parties }),
        rule: t("stats.findings.championRule", { min: f.regle.min }) });
      else if (f.type === "champion" && f.statut === "non-concluant") flatChamps.push(f);
      else if (f.type === "champion") out.push({ tone: "wait", badge: t("stats.findings.notYet"), have: f.mesure.parties, need: f.regle.min,
        title: f.sujet ? t("stats.withheld.champion", { min: f.regle.min, champion: nom(f.sujet), games: f.mesure.parties }) : t("stats.withheld.noData") });
      else if (f.type === "tendance" && f.statut === "insuffisant") out.push({ tone: "wait", badge: t("stats.findings.notYet"), have: f.parties, need: f.regle.min,
        title: t("stats.withheld.trend", { min: f.regle.min, games: f.parties, role }) });
      else if (f.type === "tendance") out.push({ tone: f.statut === "etabli" ? (f.sens === "hausse" ? "good" : "threat") : "neutral",
        badge: t(f.statut === "etabli" ? "stats.findings.established" : "stats.findings.inconclusive"),
        title: t(f.statut !== "etabli" ? "stats.findings.trendFlat" : f.sens === "hausse" ? "stats.findings.trendUp" : "stats.findings.trendDown", { role }),
        proof: t("stats.findings.trendProof", { older: pct(f.avant.taux), olderGames: f.avant.parties, newer: pct(f.apres.taux), newerGames: f.apres.parties }),
        rule: t("stats.findings.trendRule", { min: f.regle.min }) });
      else if (f.type === "duel" && f.statut === "insuffisant") out.push({ tone: "wait", badge: t("stats.findings.notYet"), have: f.mesure.parties, need: f.regle.min,
        title: f.sujet ? t("stats.withheld.duel", { min: f.regle.min, champion: nom(f.sujet), games: f.mesure.parties }) : t("stats.duels.noMid") });
      else if (f.type === "duel" && f.statut === "etabli") out.push({ tone: f.sens === "difficile" ? "threat" : "good", badge: t("stats.findings.established"),
        title: t(f.sens === "difficile" ? "stats.findings.duelHard" : "stats.findings.duelEasy", { champion: nom(f.sujet) }),
        proof: t("stats.findings.duelProof", { record: rec(f.mesure), champion: nom(f.sujet), rate: pct(f.mesure.taux), high: pct(f.mesure.intervalle[1]), ref: pct(f.reference.taux), refGames: f.reference.parties }),
        rule: t("stats.findings.duelRule", { min: f.regle.min, count: f.regle.testes }) });
      else if (f.type === "duel") flatDuels.push(f);
      else if (f.type === "conseil" && f.statut === "insuffisant") out.push({ tone: "wait", badge: t("stats.findings.notYet"), have: Math.min(f.suivies.parties, f.autres.parties), need: f.regle.min,
        title: t("stats.withheld.advice", { min: f.regle.min, followed: f.suivies.parties, other: f.autres.parties }) });
      else if (f.type === "conseil") out.push({ tone: "neutral", badge: t(f.statut === "etabli" ? "stats.findings.established" : "stats.findings.inconclusive"), title: t("stats.findings.adviceFlat"),
        proof: t("stats.findings.adviceProof", { followed: record(f.suivies) + ", " + pct(f.suivies.victoires / f.suivies.parties), other: record(f.autres) + ", " + pct(f.autres.victoires / f.autres.parties) }),
        rule: t("stats.journal.compare.warning") });
    });
    flatChamps.forEach(f => out.push({ tone: "neutral", badge: t("stats.findings.inconclusive"), title: t("stats.findings.championFlat", { champion: nom(f.sujet), role: roleName(f.role) }),
      proof: t("stats.findings.flatProof", { record: rec(f.mesure), rate: pct(f.mesure.taux), low: pct(f.mesure.intervalle[0]), high: pct(f.mesure.intervalle[1]) }) }));
    if (flatDuels.length) out.push({ tone: "neutral", badge: t("stats.findings.inconclusive"), title: tp("stats.findings.duelFlat", flatDuels.length),
      proof: flatDuels.map(f => nom(f.sujet) + " " + rec(f.mesure)).join(" · ") });
    // Limite connue de l'import, dite une fois : ce n'est pas un manque d'échantillon, c'est une donnée absente.
    if (scope === "riot" && found.some(f => f.type === "tendance" && f.parties)) out.push({ tone: "wait", badge: t("stats.findings.notYet"), title: t("stats.withheld.otherLanes") });
    const order = { good: 0, threat: 0, neutral: 1, wait: 2 };
    return out.sort((a, b) => order[a.tone] - order[b.tone]);
  }

  const D = window.MAQ_DONNEES.etats[ETAT];
  window.MAQ = Object.freeze({ LANG, ETAT, t, tp, tx, h, esc, fmtNumber, fmtDate, pct, dec, record, mins, nom, roleName, ROLE_OF_POS, portrait, roleIcon, gauge, winbar, shell, describe, MIN,
    D, rows: D.history ? D.history.rows : null, history: D.history, journal: D.journal, C: window.MAQ_CALCULS, missing: () => [...missing] });
})();
