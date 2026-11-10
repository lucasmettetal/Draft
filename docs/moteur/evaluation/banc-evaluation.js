// Banc d'évaluation du moteur DRAFT, sur scénarios SYNTHÉTIQUES.
// Se charge après web/draft-role-sheet.js, draft-champions.js, draft-engine.js et draft-data.js.
// Lecture seule : le banc appelle les fonctions du moteur, sans rien enregistrer dans le dépôt.
// Il reproduit le chemin de computeVerdict (web/index.html) sans correction manuelle et avec la frontline alliée supposée.

const BANC_VERSION = "1.0";
const BANC_GRAINE = 4242;
const SUPPOSE_FRONT = true;
// Menaces évidentes, choisies par l'annotateur (et non par le profil ROSTER du moteur).
const MENACES = {
  plongeur: { noms:["Zed", "Kha'Zix", "Talon", "Rengar", "Evelynn", "Qiyana", "Naafiri"], trait:"i", sens:"ne_monte_pas", texte:"un plongeur ou assassin de plus ne doit pas faire monter un pick immobile (i)" },
  tank: { noms:["Ornn", "Sion", "Sejuani", "Zac", "K'Sante", "Rammus"], trait:"k", sens:"ne_baisse_pas", texte:"un tank de plus ne doit pas faire baisser un tueur de tanks (k)" },
  cible: { noms:["Jinx", "Caitlyn", "Jhin", "Aphelios", "Xayah", "Varus"], trait:"h", sens:"ne_baisse_pas", texte:"une cible fragile de plus ne doit pas faire baisser un assassin (h)" }
};

function mulberry(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let x = Math.imul(a ^ a >>> 15, 1 | a); x = x + Math.imul(x ^ x >>> 7, 61 | x) ^ x; return ((x ^ x >>> 14) >>> 0) / 4294967296; }; }
const permuter = (l, rng) => { const c = l.slice(); for (let i = c.length - 1; i > 0; i--) { const k = Math.floor(rng() * (i + 1)); [c[i], c[k]] = [c[k], c[i]]; } return c; };
const arrondi = (v, d = 4) => Math.round(v * 10 ** d) / 10 ** d;
const pct = (a, b) => b ? arrondi(100 * a / b, 1) : null;

// Une exécution du moteur, comme l'interface. midFixe : évaluer contre un adversaire direct imposé, sans hypothèses.
function executer(scn, o = {}) {
  const adv = o.adversaires || scn.adversaires;
  applyProfile({ role: scn.joueur.role, pool: o.pool || scn.joueur.pool });
  const n = adv.length;
  const belief = opponentBelief(adv, {});
  const hypotheses = n ? beliefScenarios(belief) : null;
  const fixe = Object.prototype.hasOwnProperty.call(o, "midFixe");
  const mid = fixe ? o.midFixe : identifiedOpponent(adv, {});
  const res = analyze(adv, mid, SUPPOSE_FRONT, fixe ? null : hypotheses);
  const rb = roleBelief(adv, {});
  return {
    mid, confiance: belief.confidence, candidat: belief.candidates[0] ? belief.candidates[0].name : null,
    hypotheses: hypotheses ? hypotheses.map(h => ({ mid: h.mid, p: arrondi(h.p) })) : null,
    marginales: Object.fromEntries(rb.team.map((nom, i) => [nom, rb.marginals[i].map(v => arrondi(v))])),
    parts: rb.team.map((_, i) => rb.evidence[i].shares),
    equipe: rb.team,
    classement: res.ranking.map(p => ({ pick: label(p.pick), score: p.score, criteres: Object.fromEntries(p.criteria.map(c => [c.key, arrondi(c.impact, 6)])) })),
    egalites: res.ties.map(t => t.picks.map(label).sort())
  };
}
const signature = r => JSON.stringify({ c: r.classement.map(p => [p.pick, tieKey(p.score)]), e: r.egalites });
const signatureRoles = r => JSON.stringify({ mid: r.mid, conf: r.confiance, m: Object.keys(r.marginales).sort().map(k => [k, r.marginales[k]]) });

// Affectation conjointe la plus probable (produit des parts), comme roleBelief la pèse.
function affectationMAP(r) {
  let best = null;
  const place = (i, used, w, choix) => {
    if (i === r.equipe.length) { if (!best || w > best.w) best = { w, choix: choix.slice() }; return; }
    ROLE_ORDER.forEach((_, role) => { if (!(used & (1 << role))) { choix.push(role); place(i + 1, used | (1 << role), w * r.parts[i][role], choix); choix.pop(); } });
  };
  place(0, 0, 1, []);
  return best ? Object.fromEntries(r.equipe.map((n, i) => [n, ROLE_ORDER[best.choix[i]]])) : {};
}

function evaluerJeu(scenarios) {
  const rng = mulberry(BANC_GRAINE);
  const m = {
    roles: { parRole: {}, parNiveau: {}, total: 0, justesArgmax: 0, justesMAP: 0, completes: 0, completesJustes: 0, brier: 0, nBrier: 0, bins: Array.from({ length: 10 }, () => ({ n: 0, p: 0, ok: 0 })) },
    adversaire: { avecNom: 0, juste: 0, faux: 0, abstention: 0, candidatJuste: 0, nonRevele: 0, nonReveleRespecte: 0, parConfiance: {}, confianceEleveeInterdite: 0, confianceEleveeViolee: 0 },
    invariance: { determinisme: [0, 0], ordrePool: [0, 0], ordreAdversaires: [0, 0], ordreAdversairesRoles: [0, 0], exclusionHorsPool: [0, 0], favoriHorsEgalite: [0, 0] },
    egalites: { attendues: 0, respectees: 0, absents: 0, absentsRespectes: 0 },
    monotonie: {},
    remplacement: { essais: 0, top1Change: 0, kendall: 0 },
    exemples: { ordreAdversaires: [], monotonie: [], adversaire: [], roles: [] }
  };
  Object.keys(MENACES).forEach(k => { m.monotonie[k] = { texte: MENACES[k].texte, verifsMidFixe: 0, violationsMidFixe: 0, verifsComplet: 0, violationsComplet: 0, top1VersPickFragile: 0 }; });
  const snapshot = {};
  const add = (pair, ok) => { pair[1]++; if (ok) pair[0]++; };

  scenarios.forEach(scn => {
    const a = scn.annotations;
    const base = executer(scn);
    snapshot[scn.id] = { mid: base.mid, confiance: base.confiance, hypotheses: base.hypotheses, marginales: base.marginales,
      classement: base.classement.map(p => ({ pick: p.pick, score: arrondi(p.score, 6), criteres: p.criteres })), egalites: base.egalites };

    // --- Rôles ---
    const map = affectationMAP(base);
    const annot = Object.keys(a.roles_attendus);
    annot.forEach(nom => {
      const att = a.roles_attendus[nom], marg = base.marginales[nom];
      if (!marg) return;
      const idx = ROLE_ORDER.indexOf(att.role);
      const top = marg.indexOf(Math.max(...marg));
      const ok = top === idx;
      m.roles.total++; if (ok) m.roles.justesArgmax++; if (map[nom] === att.role) m.roles.justesMAP++;
      const pr = m.roles.parRole[att.role] = m.roles.parRole[att.role] || [0, 0]; add(pr, ok);
      const pn = m.roles.parNiveau[att.niveau] = m.roles.parNiveau[att.niveau] || [0, 0]; add(pn, ok);
      marg.forEach((p, i) => { m.roles.brier += (p - (i === idx ? 1 : 0)) ** 2; });
      m.roles.nBrier++;
      const b = m.roles.bins[Math.min(9, Math.floor(marg[top] * 10))]; b.n++; b.p += marg[top]; if (ok) b.ok++;
      if (!ok && m.exemples.roles.length < 12) m.exemples.roles.push(`${scn.id} ${nom} : attendu ${att.role} (${att.niveau}), moteur ${ROLE_ORDER[top]} à ${marg[top]}`);
    });
    if (scn.adversaires.length === 5 && annot.length === 5) {
      m.roles.completes++;
      if (annot.every(nom => map[nom] === a.roles_attendus[nom].role)) m.roles.completesJustes++;
    }

    // --- Adversaire direct ---
    const d = a.adversaire_direct_attendu;
    if (d && d.nom) {
      m.adversaire.avecNom++;
      if (base.mid === d.nom) m.adversaire.juste++; else if (base.mid) m.adversaire.faux++; else m.adversaire.abstention++;
      if (base.candidat === d.nom) m.adversaire.candidatJuste++;
      const c = m.adversaire.parConfiance[base.confiance] = m.adversaire.parConfiance[base.confiance] || { n: 0, nomme: 0, juste: 0 };
      c.n++; if (base.mid) c.nomme++; if (base.mid === d.nom) c.juste++;
      if (base.mid && base.mid !== d.nom && m.exemples.adversaire.length < 12) m.exemples.adversaire.push(`${scn.id} [${scn.joueur.role}] ${scn.adversaires.join(", ")} : attendu ${d.nom}, moteur ${base.mid} (${base.confiance})`);
    } else if (d && d.niveau === "non_revele") {
      m.adversaire.nonRevele++; if (!base.mid) m.adversaire.nonReveleRespecte++;
      else if (m.exemples.adversaire.length < 12) m.exemples.adversaire.push(`${scn.id} [${scn.joueur.role}] ${scn.adversaires.join(", ")} : adversaire direct pas encore révélé, moteur nomme ${base.mid}`);
    }
    if (a.confiance_elevee_interdite) { m.adversaire.confianceEleveeInterdite++; if (base.confiance === "élevée") m.adversaire.confianceEleveeViolee++; }

    // --- Invariances ---
    add(m.invariance.determinisme, signature(executer(scn)) === signature(base));
    for (let k = 0; k < 4; k++) add(m.invariance.ordrePool, signature(executer(scn, { pool: permuter(scn.joueur.pool, rng) })) === signature(base));
    if (scn.adversaires.length > 1) for (let k = 0; k < 4; k++) {
      const perm = permuter(scn.adversaires, rng), r = executer(scn, { adversaires: perm });
      const ok = signature(r) === signature(base);
      add(m.invariance.ordreAdversaires, ok);
      add(m.invariance.ordreAdversairesRoles, signatureRoles(r) === signatureRoles(base));
      if (!ok && m.exemples.ordreAdversaires.length < 10) m.exemples.ordreAdversaires.push(`${scn.id} [${scn.joueur.role}] ordre ${scn.adversaires.join(", ")} -> ${base.mid || "aucun"} / ordre ${perm.join(", ")} -> ${r.mid || "aucun"} ; top ${base.classement[0] && base.classement[0].pick} -> ${r.classement[0] && r.classement[0].pick}`);
    }
    const horsPool = NAMES.find(n => !scn.joueur.pool.includes(n) && !scn.adversaires.includes(n));
    ENGINE_PREFS.excluded = [pickId(horsPool)];
    add(m.invariance.exclusionHorsPool, signature(executer(scn)) === signature(base));
    ENGINE_PREFS.excluded = [];
    const seul = base.classement.slice().reverse().find(p => !base.egalites.some(g => g.includes(p.pick)));
    if (seul) { ENGINE_PREFS.favorite = pickId(seul.pick); add(m.invariance.favoriHorsEgalite, signature(executer(scn)) === signature(base)); ENGINE_PREFS.favorite = null; }

    // --- Égalités et picks absents ---
    a.egalites_attendues.forEach(g => { m.egalites.attendues++; if (base.egalites.some(x => g.every(n => x.includes(n)))) m.egalites.respectees++; });
    a.absents_attendus.forEach(n => { m.egalites.absents++; if (!base.classement.some(p => p.pick === n)) m.egalites.absentsRespectes++; });

    // --- Monotonie : ajouter une menace évidente à une draft incomplète ---
    if (scn.adversaires.length <= 4) Object.keys(MENACES).forEach(k => {
      const men = MENACES[k], nom = men.noms.find(n => !scn.adversaires.includes(n) && !scn.joueur.pool.includes(n));
      const concernes = scn.joueur.pool.filter(p => (CHAMPION_TRAITS[p] || "").includes(men.trait));
      if (!nom || !concernes.length) return;
      const adv2 = scn.adversaires.concat(nom);
      const fixeAvant = executer(scn, { midFixe: base.mid }), fixeApres = executer(scn, { adversaires: adv2, midFixe: base.mid });
      const complet = executer(scn, { adversaires: adv2 });
      const st = m.monotonie[k];
      concernes.forEach(p => {
        const s0 = fixeAvant.classement.find(x => x.pick === p), s1 = fixeApres.classement.find(x => x.pick === p);
        const c0 = base.classement.find(x => x.pick === p), c1 = complet.classement.find(x => x.pick === p);
        if (!s0 || !s1 || !c0 || !c1) return;
        const viole = (x, y) => men.sens === "ne_monte_pas" ? y.score > x.score + 1e-9 : y.score < x.score - 1e-9;
        st.verifsMidFixe++; if (viole(s0, s1)) { st.violationsMidFixe++; if (m.exemples.monotonie.length < 10) m.exemples.monotonie.push(`${scn.id} +${nom} (${k}) : ${p} ${arrondi(s0.score, 2)} -> ${arrondi(s1.score, 2)} à mid fixé`); }
        st.verifsComplet++; if (viole(c0, c1)) { st.violationsComplet++; if (m.exemples.monotonie.length < 10) m.exemples.monotonie.push(`${scn.id} +${nom} (${k}) : ${p} ${arrondi(c0.score, 2)} -> ${arrondi(c1.score, 2)} chemin complet (mid ${base.mid || "aucun"} -> ${complet.mid || "aucun"})`); }
      });
      if (men.sens === "ne_monte_pas" && complet.classement[0] && concernes.includes(complet.classement[0].pick) && base.classement[0] && base.classement[0].pick !== complet.classement[0].pick) st.top1VersPickFragile++;
    });

    // --- Remplacement par un champion de même rôle et même archétype (sensibilité, pas une erreur en soi) ---
    const groupe = Object.values(ARCHETYPES).find(g => scn.adversaires.some(n => g.noms.includes(n)));
    if (groupe && scn.adversaires.length === 5) {
      const ancien = scn.adversaires.find(n => groupe.noms.includes(n));
      const nouveau = groupe.noms.find(n => n !== ancien && !scn.adversaires.includes(n) && !scn.joueur.pool.includes(n));
      if (nouveau) {
        const r = executer(scn, { adversaires: scn.adversaires.map(n => n === ancien ? nouveau : n) });
        m.remplacement.essais++;
        if (r.classement[0] && base.classement[0] && r.classement[0].pick !== base.classement[0].pick) m.remplacement.top1Change++;
        m.remplacement.kendall += kendall(base.classement.map(p => p.pick), r.classement.map(p => p.pick));
      }
    }
  });
  return { m, snapshot };
}

// Distance de Kendall normalisée entre deux ordres des mêmes éléments (0 = identiques, 1 = inversés).
function kendall(a, b) {
  const commun = a.filter(x => b.includes(x));
  if (commun.length < 2) return 0;
  let d = 0, n = 0;
  for (let i = 0; i < commun.length; i++) for (let j = i + 1; j < commun.length; j++) {
    n++; if ((b.indexOf(commun[i]) < b.indexOf(commun[j])) !== (a.indexOf(commun[i]) < a.indexOf(commun[j]))) d++;
  }
  return d / n;
}

// Rapport chiffré lisible, à partir des compteurs.
function resumer(m, n) {
  const r = m.roles, ad = m.adversaire, inv = m.invariance;
  let ece = 0; r.bins.forEach(b => { if (b.n) ece += (b.n / r.total) * Math.abs(b.ok / b.n - b.p / b.n); });
  return {
    scenarios: n,
    roles: {
      precisionArgmax: pct(r.justesArgmax, r.total), precisionMAP: pct(r.justesMAP, r.total), champions: r.total,
      parRole: Object.fromEntries(Object.entries(r.parRole).map(([k, v]) => [k, `${pct(v[0], v[1])} % (${v[1]})`])),
      parNiveau: Object.fromEntries(Object.entries(r.parNiveau).map(([k, v]) => [k, `${pct(v[0], v[1])} % (${v[1]})`])),
      affectationCompleteJuste: `${pct(r.completesJustes, r.completes)} % (${r.completes} drafts complètes entièrement annotées)`,
      brier: arrondi(r.brier / r.nBrier, 4), ece: arrondi(ece, 4),
      fiabilite: r.bins.map((b, i) => b.n ? `${i / 10}-${(i + 1) / 10} : n=${b.n}, p moyen ${arrondi(b.p / b.n, 2)}, juste ${arrondi(b.ok / b.n, 2)}` : null).filter(Boolean)
    },
    adversaireDirect: {
      attenduNomme: ad.avecNom, juste: pct(ad.juste, ad.avecNom), faux: pct(ad.faux, ad.avecNom), abstention: pct(ad.abstention, ad.avecNom),
      candidatPrincipalJuste: pct(ad.candidatJuste, ad.avecNom),
      parConfiance: Object.fromEntries(Object.entries(ad.parConfiance).map(([k, v]) => [k, `n=${v.n}, nommé ${v.nomme}, juste ${v.juste}`])),
      nonRevele: `${ad.nonReveleRespecte}/${ad.nonRevele} sans adversaire inventé`,
      confianceEleveeInterdite: `${ad.confianceEleveeViolee} violation(s) sur ${ad.confianceEleveeInterdite}`
    },
    invariance: Object.fromEntries(Object.entries(inv).map(([k, v]) => [k, `${v[0]}/${v[1]} identiques (${pct(v[0], v[1])} %)`])),
    egalites: `${m.egalites.respectees}/${m.egalites.attendues} égalités attendues respectées ; ${m.egalites.absentsRespectes}/${m.egalites.absents} picks pris bien absents`,
    monotonie: Object.fromEntries(Object.entries(m.monotonie).map(([k, v]) => [k, `${v.texte} : mid fixé ${v.violationsMidFixe}/${v.verifsMidFixe} violations, chemin complet ${v.violationsComplet}/${v.verifsComplet} violations, top 1 basculé vers un pick fragile ${v.top1VersPickFragile} fois`])),
    remplacementMemeArchetype: `${m.remplacement.top1Change}/${m.remplacement.essais} changements du 1er choix (${pct(m.remplacement.top1Change, m.remplacement.essais)} %), distance de Kendall moyenne ${arrondi(m.remplacement.kendall / (m.remplacement.essais || 1), 3)}`,
    exemples: m.exemples
  };
}

// Comparaison de deux instantanés. Un changement est « attendu » si seuls les critères déclarés par la PR ont bougé.
// L'instantané de référence n'est pas une vérité : il dit seulement ce qui a changé.
function comparerInstantanes(avant, apres, criteresDeclares) {
  const diff = { scenarios: 0, top1: 0, ordre: 0, scores: 0, adversaire: 0, roles: 0, attendus: 0, inattendus: 0, details: [] };
  Object.keys(avant).forEach(id => {
    const a = avant[id], b = apres[id];
    if (!b) return;
    diff.scenarios++;
    const ordreA = a.classement.map(p => p.pick).join(), ordreB = b.classement.map(p => p.pick).join();
    const top = a.classement[0] && b.classement[0] && a.classement[0].pick !== b.classement[0].pick;
    const scoreBouge = a.classement.some(p => { const q = b.classement.find(x => x.pick === p.pick); return !q || Math.abs(q.score - p.score) > 1e-6; });
    const advBouge = a.mid !== b.mid || a.confiance !== b.confiance;
    const rolesBougent = JSON.stringify(a.marginales) !== JSON.stringify(b.marginales);
    if (top) diff.top1++; if (ordreA !== ordreB) diff.ordre++; if (scoreBouge) diff.scores++; if (advBouge) diff.adversaire++; if (rolesBougent) diff.roles++;
    if (!top && ordreA === ordreB && !scoreBouge && !advBouge && !rolesBougent) return;
    const criteres = new Set();
    a.classement.forEach(p => { const q = b.classement.find(x => x.pick === p.pick); if (!q) return; new Set(Object.keys(p.criteres).concat(Object.keys(q.criteres))).forEach(k => { if (Math.abs((p.criteres[k] || 0) - (q.criteres[k] || 0)) > 1e-6) criteres.add(k); }); });
    const explique = !advBouge && !rolesBougent && [...criteres].every(k => criteresDeclares.includes(k));
    if (top || ordreA !== ordreB) { if (explique) diff.attendus++; else diff.inattendus++; }
    if (diff.details.length < 15 && (top || ordreA !== ordreB)) diff.details.push(`${id} : ${ordreA} -> ${ordreB} ; critères modifiés : ${[...criteres].join(", ") || "aucun"}${advBouge ? " ; adversaire direct modifié" : ""}${explique ? " (attendu)" : " (INATTENDU)"}`);
  });
  diff.tauxChangementTop1 = pct(diff.top1, diff.scenarios);
  diff.tauxInattendu = pct(diff.inattendus, diff.scenarios);
  return diff;
}

function lancerBanc() {
  const sortie = { synthetique: true, avertissement: "Chiffres mesurés sur des scénarios SYNTHÉTIQUES, avec des annotations écrites à la main. Ils ne mesurent pas la qualité du moteur sur de vraies parties.", banc: BANC_VERSION, moteur: ENGINE_VERSION, jeux: {} };
  const cfg = window.BANC_CONFIG || {};
  const instantanes = {};
  [["developpement", window.SCENARIOS_DEV], ["evaluation_gele", cfg.avecEvaluation ? window.SCENARIOS_EVAL : null]].forEach(([nom, jeu]) => {
    if (!jeu) return;
    if (!jeu.synthetique) throw new Error("Jeu sans le champ synthetique : refusé.");
    const { m, snapshot } = evaluerJeu(jeu.scenarios);
    sortie.jeux[nom] = resumer(m, jeu.scenarios.length);
    instantanes[nom] = snapshot;
  });
  // Démonstration du rapport de différences : variante fictive, en mémoire seulement (poids « Tanks et bruisers » 2 -> 1,5).
  const front = CRITERIA.find(c => c.key === "front"), poids = front.weight;
  front.weight = 1.5;
  const variante = evaluerJeu(window.SCENARIOS_DEV.scenarios).snapshot;
  front.weight = poids;
  sortie.demoVariante = {
    description: "Variante fictive en mémoire : poids du critère front 2 -> 1,5. Sert seulement à montrer le rapport de différences.",
    declareFront: comparerInstantanes(instantanes.developpement, variante, ["front"]),
    declareRien: comparerInstantanes(instantanes.developpement, variante, [])
  };
  sortie.memeVersion = comparerInstantanes(instantanes.developpement, evaluerJeu(window.SCENARIOS_DEV.scenarios).snapshot, []);
  if (window.INSTANTANE_REF) sortie.contreReference = Object.fromEntries(Object.keys(instantanes).filter(k => window.INSTANTANE_REF.jeux && window.INSTANTANE_REF.jeux[k]).map(k => [k, comparerInstantanes(window.INSTANTANE_REF.jeux[k], instantanes[k], cfg.criteresDeclares || [])]));
  return { sortie, instantane: { synthetique: true, avertissement: "Sorties du moteur à un instant donné. Ce n'est pas une vérité de référence.", moteur: ENGINE_VERSION, banc: BANC_VERSION, jeux: instantanes } };
}
