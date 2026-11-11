// Tests de non-régression de l'attribution des rôles adverses, lancés par tools/run-role-tests.ps1.
// Deux familles :
// - propriétés : vraies quel que soit le réglage du moteur (probabilités qui somment à 1, correction prise en compte,
//   aucun adversaire inventé). Un échec signale un défaut.
// - instantané : le comportement ACTUEL sur les cas atypiques SYNTHÉTIQUES (tests/roles/cas-atypiques.js), figé dans
//   tests/roles/instantane-roles.js. Ce n'est pas une vérité : un écart signale un changement de comportement, à examiner,
//   puis à accepter avec tools/run-role-tests.ps1 -UpdateSnapshot s'il est voulu.
(function () {
  const ok = [], ko = [];
  const t = (cond, label) => (cond ? ok : ko).push(label);
  const R = ROLE_ORDER, EPS = 1e-9;
  const near = (a, b, e) => Math.abs(a - b) <= (e || 1e-9);
  function mulberry(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let x = Math.imul(a ^ a >>> 15, 1 | a); x = x + Math.imul(x ^ x >>> 7, 61 | x) ^ x; return ((x ^ x >>> 14) >>> 0) / 4294967296; }; }
  const rng = mulberry(20261110);
  const pickTeam = size => { const list = []; while (list.length < size) { const n = NAMES[Math.floor(rng() * NAMES.length)]; if (!list.includes(n)) list.push(n); } return list; };

  // ---------- Propriétés, sur des équipes tirées au hasard (graine fixe) et sur les cas atypiques ----------
  const teams = [];
  for (let i = 0; i < 240; i++) teams.push(pickTeam(i % 6));
  ROLE_CASES.forEach(c => teams.push(c.equipe.slice()));
  teams.push(["ChampionInconnu", "Vi"], ["ChampionInconnu", "Vi", "Garen", "Jinx", "Thresh"]);
  const bad = { rows:0, cols:0, vacant:0, opp:0, scen:0, invented:0, nan:0, determinism:0 };
  teams.forEach(team => {
    const b = roleBelief(team);
    if (b.marginals.some(row => !near(row.reduce((a, v) => a + v, 0), 1, 1e-9))) bad.rows++;
    if (R.some((_, r) => !near(b.marginals.reduce((a, row) => a + row[r], 0) + b.vacant[r], 1, 1e-9))) bad.cols++;
    if (!near(b.vacant.reduce((a, v) => a + v, 0), R.length - team.length, 1e-9)) bad.vacant++;
    if (b.marginals.some(row => row.some(v => isNaN(v) || v < -EPS || v > 1 + EPS))) bad.nan++;
    R.forEach(role => {
      applyProfile({ role, pool:["Garen"] });
      const ob = opponentBelief(team), sc = beliefScenarios(ob), id = identifiedOpponent(team);
      if (!near(ob.candidates.reduce((a, c) => a + c.p, 0) + ob.unrevealed, 1, 1e-9)) bad.opp++;
      if (!near(sc.reduce((a, s) => a + s.p, 0), 1, 1e-9)) bad.scen++;
      const known = n => ob.candidates.some(c => c.name === n && c.known);
      if ((id && !known(id)) || sc.some(s => s.mid !== null && !known(s.mid)) || (guessMid(team) && !team.includes(guessMid(team)))) bad.invented++;
      if (JSON.stringify(opponentBelief(team)) !== JSON.stringify(ob)) bad.determinism++;
    });
  });
  t(!bad.rows, `chaque champion révélé a des probabilités de rôle qui somment à 1 (${teams.length} équipes)`);
  t(!bad.cols, "pour chaque rôle, champions révélés + « pas encore choisi » somment à 1");
  t(!bad.vacant, "le nombre de rôles libres attendu vaut 5 moins le nombre de champions révélés");
  t(!bad.nan, "aucune probabilité hors de [0 ; 1] ni NaN");
  t(!bad.opp, "adversaire direct : candidats + « pas encore choisi » somment à 1, pour les cinq rôles du joueur");
  t(!bad.scen, "les hypothèses gardées somment à 1");
  t(!bad.invented, "aucun adversaire inventé : seul un champion révélé et connu peut être nommé ou évalué");
  t(!bad.determinism, "même draft, même réponse");

  R.forEach(role => {
    applyProfile({ role, pool:["Garen"] });
    const ob = opponentBelief([]);
    t(ob.candidates.length === 0 && near(ob.unrevealed, 1) && identifiedOpponent([]) === null && guessMid([]) === null, `rien de révélé (profil ${role}) : personne n'est nommé`);
    const sc = beliefScenarios(ob);
    t(sc.length === 1 && sc[0].mid === null && near(sc[0].p, 1), `rien de révélé (profil ${role}) : une seule hypothèse, « pas encore choisi »`);
    t(identifiedOpponent(["ChampionInconnu"]) === null && guessMid(["ChampionInconnu"]) === null, `champion inconnu seul (profil ${role}) : jamais nommé`);
  });

  // Corrections manuelles : un rôle imposé est respecté, et contraint les autres champions.
  let fixedBad = 0, oppBad = 0, contraBad = 0;
  for (let i = 0; i < 120; i++) {
    const size = 1 + (i % 5), team = pickTeam(size), who = team[Math.floor(rng() * size)], role = R[Math.floor(rng() * 5)];
    const b = roleBelief(team, { [who]:role }), k = team.indexOf(who), r = R.indexOf(role);
    if (!near(b.marginals[k][r], 1, 1e-9) || !near(b.vacant[r], 0, 1e-9) || b.marginals.some((row, j) => j !== k && !near(row[r], 0, 1e-9))) fixedBad++;
    applyProfile({ role, pool:["Garen"] });
    const ob = opponentBelief(team, { [who]:role });
    if (ob.candidates[0].name !== who || !near(ob.candidates[0].p, 1, 1e-9)) oppBad++;
    if (size >= 2) {
      const other = team.find(n => n !== who), b2 = roleBelief(team, { [who]:role, [other]:role });
      if (b2.marginals.some(row => !near(row.reduce((a, v) => a + v, 0), 1, 1e-9))) contraBad++;
    }
  }
  t(!fixedBad, "correction manuelle : le champion corrigé a son rôle à p = 1, et personne d'autre ne l'occupe");
  t(!oppBad, "correction au rôle du joueur : ce champion devient l'adversaire direct, p = 1");
  t(!contraBad, "deux corrections contradictoires : le calcul répond quand même, sans NaN");
  applyProfile({ role:"mid", pool:["Garen"] });
  const before = opponentBelief(["Garen", "Vi", "Kog'Maw", "Jinx", "Thresh"]), after = opponentBelief(["Garen", "Vi", "Kog'Maw", "Jinx", "Thresh"], { "Jinx":"adc" });
  const pk = b => b.candidates.find(c => c.name === "Kog'Maw").p;
  t(pk(after) > pk(before) && after.candidates[0].name === "Kog'Maw", "corriger « Jinx = ADC » fait monter Kog'Maw comme mid adverse");

  // ---------- Rôles affichés (likelyAssignment, PR E) : la répartition la plus probable, sans rien changer au calcul ----------
  // Recherche exhaustive indépendante : produit des parts maximal, corrections respectées.
  const bestProduct = (team, fixed) => {
    const b = roleBelief(team, fixed), f = fixed || {};
    let best = 0;
    const walk = (i, used, w) => {
      if (i === team.length) { best = Math.max(best, w); return; }
      R.forEach((role, r) => { if (!(used & (1 << r)) && (!f[team[i]] || f[team[i]] === role)) walk(i + 1, used | (1 << r), w * b.evidence[i].shares[r]); });
    };
    walk(0, 0, 1);
    return best;
  };
  const productOf = (team, slotsByName) => { const b = roleBelief(team); return team.reduce((w, n, i) => w * b.evidence[i].shares[R.indexOf(slotsByName[n])], 1); };
  let mapBad = 0, orderBad = 0, untouched = 0, fixBad = 0, fullBad = 0;
  for (let i = 0; i < 160; i++) {
    const size = 1 + (i % 5), team = pickTeam(size);
    const before = JSON.stringify(roleBelief(team));
    const la = likelyAssignment(team, {}), roles = Object.fromEntries(team.map(n => [n, la.byName[n].role]));
    if (new Set(Object.values(roles)).size !== team.length || Object.keys(la.slots).length !== team.length) fullBad++;
    if (Math.abs(productOf(team, roles) - bestProduct(team, {})) > 1e-12) mapBad++;
    if (JSON.stringify(likelyAssignment(team.slice().reverse(), {}).slots) !== JSON.stringify(la.slots) && JSON.stringify(Object.entries(likelyAssignment(team.slice().reverse(), {}).slots).sort()) !== JSON.stringify(Object.entries(la.slots).sort())) orderBad++;
    if (JSON.stringify(roleBelief(team)) !== before) untouched++;
    const who = team[0], role = R[i % 5], lf = likelyAssignment(team, { [who]:role });
    if (lf.byName[who].role !== role || lf.byName[who].uncertain) fixBad++;
  }
  t(!fullBad, "rôles affichés : chaque champion révélé a un rôle, jamais deux sur le même rôle");
  t(!mapBad, "rôles affichés = répartition la plus probable (produit des parts maximal, recherche exhaustive indépendante)");
  t(!orderBad, "rôles affichés : l'ordre de saisie ne change rien, même à égalité exacte");
  t(!untouched, "likelyAssignment ne modifie aucune probabilité de roleBelief");
  t(!fixBad, "rôles affichés : une correction est respectée et n'est pas marquée incertaine");
  // Flex : égalité exacte Kog'Maw / Jinx avec mid et ADC libres.
  const kj = likelyAssignment(["Garen", "Vi", "Kog'Maw", "Jinx", "Nami"], {});
  const kog = kj.byName["Kog'Maw"], jinx = kj.byName["Jinx"];
  t(kog.uncertain && jinx.uncertain && [kog.role].concat(kog.close.slice(0, 1)).sort().join() === "adc,mid" && [jinx.role].concat(jinx.close.slice(0, 1)).sort().join() === "adc,mid", `flex Kog'Maw / Jinx : affichés « ADC ou Mid » (${kog.role}/${kog.close}, ${jinx.role}/${jinx.close})`);
  t(!kj.byName["Garen"].uncertain && kj.byName["Garen"].role === "top", "rôle net (Garen top) : pas de marque d'incertitude");
  const xl = likelyAssignment(["Xerath", "Lux"], {});
  t(xl.byName.Xerath.uncertain && xl.byName.Lux.uncertain, "flex Xerath / Lux (égalité exacte) : les deux restent incertains");
  // Relevé de validation : roles_predits bien formée, rôle le plus probable de chaque adversaire.
  const pr = predictedRolesText(["Garen", "Kog'Maw", "Jinx"], {});
  t(/^[^=,;]+=(top|jungle|mid|adc|support):[01]\.\d\d(,[^=,;]+=(top|jungle|mid|adc|support):[01]\.\d\d)*$/.test(pr) && pr.split(",").length === 3 && pr.startsWith("Garen=top:"), `roles_predits bien formée (${pr})`);
  t(predictedRolesText([], {}) === "", "roles_predits vide sans adversaire");

  // ---------- Instantané du comportement actuel ----------
  const r4 = v => Math.round(v * 1e4) / 1e4;
  const snapshot = {};
  ROLE_CASES.forEach(c => {
    t(c.synthetique === true, `${c.id} : cas marqué synthetique`);
    applyProfile({ role:c.profil, pool:["Garen"] });
    const b = roleBelief(c.equipe, c.corrections), ob = opponentBelief(c.equipe, c.corrections);
    snapshot[c.id] = {
      marginaux: b.marginals.map(row => row.map(r4)), libres: b.vacant.map(r4), confiance: ob.confidence,
      nomme: identifiedOpponent(c.equipe, c.corrections), suppose: guessMid(c.equipe, c.corrections),
      hypotheses: beliefScenarios(ob).map(s => [s.mid, r4(s.p)])
    };
  });
  const saved = typeof ROLE_SNAPSHOT !== "undefined" ? ROLE_SNAPSHOT : null;
  if (!saved) ko.push("instantané absent : lance tools/run-role-tests.ps1 -UpdateSnapshot pour le créer");
  else {
    const same = (a, b) => {
      if (typeof a === "number" && typeof b === "number") return Math.abs(a - b) <= 5e-4;
      if (Array.isArray(a) && Array.isArray(b)) return a.length === b.length && a.every((x, i) => same(x, b[i]));
      if (a && b && typeof a === "object" && typeof b === "object") { const k = Object.keys(a); return k.length === Object.keys(b).length && k.every(x => same(a[x], b[x])); }
      return a === b;
    };
    Object.keys(snapshot).forEach(id => t(saved[id] && same(snapshot[id], saved[id]), `instantané ${id} : comportement inchangé`));
    Object.keys(saved).filter(id => !snapshot[id]).forEach(id => ko.push(`instantané ${id} : cas disparu de cas-atypiques.js`));
  }
  document.getElementById("out").textContent = JSON.stringify({ ok:ok.length, ko, reussis:ok, instantane:snapshot });
})();
