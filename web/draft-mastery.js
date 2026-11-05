// Complexité, familiarité et classement personnalisé. PROTOTYPE : chargé par aucune page de l'application.
// Pur et déterministe : lit le moteur sans le modifier. Le classement par défaut reste le modèle A, celui du moteur.
//
// Quatre éléments restent séparés et ne se paient jamais deux fois :
//   matchup + composition : score du moteur
//   expérience du joueur  : nombre de parties, jamais les victoires
//   résultats du joueur   : victoires, ramenées vers sa moyenne, jamais le nombre de parties seul
//   complexité            : ne joue que là où l'expérience manque, et disparaît quand elle est là
//
// Ce n'est pas une évaluation du joueur : rien ne compare à d'autres joueurs, et aucun niveau global n'est calculé.
// Tous les réglages ci-dessous sont des choix écrits à la main, non validés sur de vraies parties.

const MASTERY_MODELS = ["A", "B", "C", "D"], MASTERY_DEFAULT_MODEL = "A";
const MASTERY_DAY = 86400000;
const FAMILIARITY_HALF_LIFE = 120;     // jours : une partie compte moitié moins après ce délai
const FAMILIARITY_OLD_FLOOR = .25;     // une expérience ancienne compte encore : on ne redevient pas novice
const FAMILIARITY_STEPS = { discovery:5, learning:20, familiar:60 };
const FAMILIARITY_RUSTY_DAYS = 180;
const FAMILIARITY_RECENT_DAYS = 30;
const EXPERIENCE_MAX = 1.5, EXPERIENCE_SCALE = 25;  // points, et parties récentes pour en atteindre 63 %
const RESULT_MAX = .75, RESULT_FULL_GAP = .1, RESULT_MIN_GAMES = 30, RESULT_SHRINK = 50;
const COMPLEXITY_MAX = .75, COMPLEXITY_FROM = 7, COMPLEXITY_SAFE_GAMES = 40;
const TREND_MIN_GAMES = 40, TREND_MIN_Z = 2;
const KDA_MIN_GAMES = 20;
const DUEL_SEEN_GAMES = 5;

// ---------- Noms : l'API Riot écrit KogMaw, Chogath ou MonkeyKing ----------
function masteryName(riotChampion) {
  const key = norm(String(riotChampion || ""));
  if (!masteryName.map) { masteryName.map = {}; NAMES.forEach(n => { masteryName.map[riotKey(n)] = n; masteryName.map[norm(n)] = n; }); }
  return masteryName.map[key] || null;
}
const MASTERY_ROLES = { TOP:"top", JUNGLE:"jungle", MIDDLE:"mid", MID:"mid", BOTTOM:"adc", ADC:"adc", UTILITY:"support", SUPPORT:"support" };

// ---------- Complexité : cinq notions distinctes, et « inconnu » quand rien ne les mesure ----------
function championComplexity(name, options = {}) {
  const official = typeof CHAMPION_OFFICIAL !== "undefined" ? CHAMPION_OFFICIAL.champions[name] : null;
  // Riot publie 0 pour quelques champions, en attaque comme en difficulté : cela veut dire absent, pas facile.
  const note = official && official.difficulty > 0 ? official.difficulty : null;
  const overrides = (options.complexity && options.complexity[name]) || {};
  return {
    champion: name,
    official: { value:note, scale:"1-10", source:"Riot, Data Dragon", version:official ? CHAMPION_OFFICIAL.version : null, status:note === null ? "unknown" : "known",
      limit:"Appréciation de Riot, non documentée, parfois ancienne, et qui peut contredire la note du client." },
    mechanical: overrides.mechanical || { value:null, status:"unknown", limit:"Aucune source autorisée ne la mesure." },
    pickup: { tier: note === null ? null : note <= 3 ? "accessible" : note < COMPLEXITY_FROM + 1 ? "moderate" : "demanding", status:note === null ? "unknown" : "proxy",
      limit:"Déduit de la seule note de Riot : ce n'est pas une mesure de la prise en main." },
    curve: overrides.curve || { value:null, status:"unmeasured", limit:"Aucune courbe de maîtrise mesurée. Il en faudrait une par champion, sur beaucoup de joueurs." }
  };
}

// ---------- Familiarité, par champion ET par rôle ----------
function wilsonInterval(wins, games) {
  if (!games) return [0, 1];
  const z = 1.96, p = wins / games, d = 1 + z * z / games, c = p + z * z / (2 * games), m = z * Math.sqrt((p * (1 - p) + z * z / (4 * games)) / games);
  return [(c - m) / d, (c + m) / d];
}

// Évolution : seulement si l'écart entre les deux moitiés ressemble peu au hasard.
function performanceTrend(games) {
  if (games.length < TREND_MIN_GAMES) return { status:"insufficient" };
  const sorted = games.slice().sort((a, b) => (a.time || 0) - (b.time || 0)), half = Math.floor(sorted.length / 2);
  const older = sorted.slice(0, half), newer = sorted.slice(half);
  const p1 = older.filter(g => g.win).length / older.length, p2 = newer.filter(g => g.win).length / newer.length;
  const p = sorted.filter(g => g.win).length / sorted.length, se = Math.sqrt(p * (1 - p) * (1 / older.length + 1 / newer.length));
  const z = se ? (p2 - p1) / se : 0;
  return { status:Math.abs(z) >= TREND_MIN_Z ? (z > 0 ? "up" : "down") : "flat", older:p1, newer:p2, z };
}

// KDA : comparé seulement à soi-même, sur ce champion et ce rôle. Il dépend trop du rôle et du style pour dire autre chose.
function contextKda(games) {
  const rated = games.filter(g => typeof g.k === "number" && typeof g.d === "number" && typeof g.a === "number").sort((a, b) => (a.time || 0) - (b.time || 0));
  if (rated.length < KDA_MIN_GAMES) return { status:"insufficient", games:rated.length };
  const kda = l => l.reduce((s, g) => s + g.k + g.a, 0) / Math.max(1, l.reduce((s, g) => s + g.d, 0));
  const half = Math.floor(rated.length / 2);
  return { status:"known", games:rated.length, overall:kda(rated), older:kda(rated.slice(0, half)), newer:kda(rated.slice(half)) };
}

// rows : lignes de l'import Riot. La maîtrise officielle est gardée pour information : elle n'entre dans aucun calcul.
function familiarityProfile(rows, options = {}) {
  let list = (rows || []).filter(r => r && r.champ);
  if (options.account) list = list.filter(r => r.acct === options.account);
  const now = options.now || Date.now();
  const entries = {}, byRole = {}, unknown = {};
  list.forEach(r => {
    const name = masteryName(r.champ), role = MASTERY_ROLES[String(r.pos || "").toUpperCase()] || null;
    if (!name) { unknown[r.champ] = (unknown[r.champ] || 0) + 1; return; }
    if (!role) return;   // Rôle absent dans Riot : la partie ne dit rien de l'expérience dans ce rôle.
    (entries[`${name}|${role}`] = entries[`${name}|${role}`] || { champion:name, role, games:[] }).games.push(r);
    const roleRow = byRole[role] || (byRole[role] = { games:0, wins:0 });
    roleRow.games++; if (r.win) roleRow.wins++;
  });
  const changes = options.changes || {};
  Object.keys(entries).forEach(key => {
    const e = entries[key], g = e.games, roleRow = byRole[e.role];
    e.count = g.length;
    e.wins = g.filter(x => x.win).length;
    e.last = Math.max(...g.map(x => x.time || 0));
    e.daysSince = e.last ? Math.floor((now - e.last) / MASTERY_DAY) : null;
    e.recentGames = g.filter(x => x.time && now - x.time <= FAMILIARITY_RECENT_DAYS * MASTERY_DAY).length;
    e.weighted = g.reduce((s, x) => s + (x.time ? Math.pow(.5, Math.max(0, now - x.time) / MASTERY_DAY / FAMILIARITY_HALF_LIFE) : 1), 0);
    // Expérience utile : les parties récentes, avec un plancher pour le passé.
    e.effective = Math.max(e.weighted, e.count * FAMILIARITY_OLD_FLOOR);
    e.step = e.count < FAMILIARITY_STEPS.discovery ? "discovery" : e.count < FAMILIARITY_STEPS.learning ? "learning" : e.count < FAMILIARITY_STEPS.familiar ? "familiar" : "experienced";
    e.rusty = e.count >= FAMILIARITY_STEPS.learning && e.daysSince !== null && e.daysSince >= FAMILIARITY_RUSTY_DAYS;
    e.interval = wilsonInterval(e.wins, e.count);
    // Référence : ses autres champions dans ce rôle, pas 50 %.
    const otherGames = roleRow.games - e.count;
    e.reference = otherGames >= RESULT_MIN_GAMES ? (roleRow.wins - e.wins) / otherGames : null;
    e.resultGap = null; e.clear = 0;
    if (e.reference !== null && e.count >= RESULT_MIN_GAMES) {
      e.resultGap = (e.wins + RESULT_SHRINK * e.reference) / (e.count + RESULT_SHRINK) - e.reference;
      if (e.interval[0] > e.reference) e.clear = 1;
      if (e.interval[1] < e.reference) e.clear = -1;
    }
    e.trend = performanceTrend(g);
    e.kda = contextKda(g);
    const change = changes[e.champion];
    e.changed = change && change.time ? { label:change.label || "modifié", gamesSince:g.filter(x => (x.time || 0) >= change.time).length } : null;
    e.versus = {};
    g.forEach(x => (x.enemies || []).forEach(en => { const n = masteryName(en); if (n) e.versus[n] = (e.versus[n] || 0) + 1; }));
    delete e.games;
  });
  return { available:list.length > 0, games:list.length, entries, unknown, officialMastery:options.officialMastery || null };
}

const familiarityOf = (profile, name, role) => profile && profile.entries[`${name}|${role}`] || null;

// ---------- Effet sur le classement ----------
// Un champion modifié garde l'expérience gagnée depuis le changement, plus une partie du passé.
function usefulExperience(entry) {
  if (!entry) return 0;
  if (!entry.changed) return entry.effective;
  return Math.min(entry.effective, entry.changed.gamesSince + entry.effective * FAMILIARITY_OLD_FLOOR);
}

function masteryEffects(name, role, profile, options = {}) {
  const entry = familiarityOf(profile, name, role), learning = (options.learning || []).includes(name);
  const experience = useFul => EXPERIENCE_MAX * (1 - Math.exp(-useFul / EXPERIENCE_SCALE));
  const useful = usefulExperience(entry);
  const effects = { experience:0, result:0, complexity:0 };
  if (profile && profile.available) {
    effects.experience = experience(useful);
    if (entry && entry.resultGap !== null) effects.result = Math.max(-RESULT_MAX, Math.min(RESULT_MAX, entry.resultGap / RESULT_FULL_GAP * RESULT_MAX));
  }
  // Complexité : aucune baisse sans raison de penser que l'expérience manque.
  // Sans import, on ne sait rien : il faut que le joueur se dise débutant. Un champion en apprentissage est un choix assumé.
  const note = championComplexity(name, options).official.value;
  const knowsLack = profile && profile.available || options.level === "beginner";
  if (note !== null && note > COMPLEXITY_FROM && knowsLack && !learning) {
    const missing = Math.max(0, 1 - useful / COMPLEXITY_SAFE_GAMES);
    effects.complexity = -COMPLEXITY_MAX * ((note - COMPLEXITY_FROM) / (10 - COMPLEXITY_FROM)) * missing;
  }
  return effects;
}

// ---------- Fournisseur de matchups interchangeable ----------
// Aucune source tierce n'est branchée : aucune n'a de conditions claires autorisant cette réutilisation (docs/SOURCES-MATCHUPS.md).
// lookup(pick, adversaire, { role }) renvoie { games, wins, baseline, role, patch, rank, region } ou null.
const MATCHUP_STAT_PROVIDERS = [];
function registerMatchupProvider(provider) {
  if (!provider || !provider.id || !provider.license || !provider.terms || typeof provider.lookup !== "function") throw new Error("Fournisseur incomplet : id, license, terms et lookup sont obligatoires.");
  MATCHUP_STAT_PROVIDERS.push(provider);
}
function providedMatchup(pick, opponent, role) {
  for (const provider of MATCHUP_STAT_PROVIDERS) {
    const row = provider.lookup(pick, opponent, { role });
    // Une ligne sans rôle exact, sans référence ou avec trop peu de parties ne compte pas : pas de valeur inventée.
    if (!row || row.role !== role || !row.games || row.games < MATCHUP_MIN_GAMES || typeof row.baseline !== "number" || typeof row.wins !== "number") continue;
    const gap = (row.wins + MATCHUP_SHRINK * row.baseline) / (row.games + MATCHUP_SHRINK) - row.baseline;
    return Object.assign({ provider:provider.id, gap }, row);
  }
  return null;
}
// Remplace le critère de matchup du moteur, sans s'y ajouter. Même conversion que le moteur.
function matchupReplacement(entry, opponent, role) {
  if (!opponent || liveMatchup(entry.pick, opponent)) return null;  // Bilans du joueur déjà dans le score : on ne les paie pas deux fois.
  const row = providedMatchup(label(entry.pick), opponent, role);
  if (!row) return null;
  const old = entry.criteria.find(c => c.key === "matchup"), weight = CRITERIA.find(c => c.key === "matchup").weight;
  const note = Math.max(-4 / 3, Math.min(4 / 3, row.gap / MATCHUP_FULL_GAP));
  return { row, delta:note * weight - (old ? old.impact : 0) };
}

// ---------- Classement comparé ----------
const masteryKey = v => Math.round(v * 1e6);
function personalizedRanking(input) {
  const model = MASTERY_MODELS.includes(input.model) ? input.model : MASTERY_DEFAULT_MODEL;
  const enemies = input.enemies || [], fixed = input.fixedRoles || {};
  const manual = input.manualOpponent && enemies.includes(input.manualOpponent) ? input.manualOpponent : null;
  const opponent = manual || identifiedOpponent(enemies, fixed);
  const scenarios = manual || !enemies.length ? null : beliefScenarios(opponentBelief(enemies, fixed));
  const base = analyze(enemies, opponent, input.hasFront !== false, scenarios);
  const role = myRole(), profile = input.profile || null;
  const rows = base.ranking.map((entry, strategicRank) => {
    const name = label(entry.pick), effects = masteryEffects(name, role, profile, input);
    const parts = { strategic:entry.score, experience:0, result:0, complexity:0, matchup:0 };
    if (model !== "A") { parts.experience = effects.experience; parts.result = effects.result; }
    if (model === "C" || model === "D") parts.complexity = effects.complexity;
    let matchup = null;
    if (model === "D") { const r = matchupReplacement(entry, opponent, role); if (r) { parts.matchup = r.delta; matchup = r.row; } }
    const total = parts.strategic + parts.experience + parts.result + parts.complexity + parts.matchup;
    return { pick:entry.pick, name, strategicRank, strategicScore:entry.score, tiedWith:entry.tiedWith, total, parts, matchup, familiarity:familiarityOf(profile, name, role), complexity:championComplexity(name, input), learning:(input.learning || []).includes(name) };
  });
  // Le modèle A garde exactement l'ordre du moteur. Les autres n'ajoutent aucun autre critère de tri que le score, puis l'ordre du moteur.
  const ranking = model === "A" ? rows : rows.slice().sort((a, b) => masteryKey(b.total) - masteryKey(a.total) || a.strategicRank - b.strategicRank);
  ranking.forEach(r => { r.tiedInModel = ranking.filter(x => x !== r && masteryKey(x.total) === masteryKey(r.total)).map(x => x.name); });
  const draftBest = rows[0] || null, playerBest = ranking[0] || null;
  return { model, role, opponent, ranking, draftBest:draftBest && draftBest.name, playerBest:playerBest && playerBest.name, differs:!!draftBest && !!playerBest && draftBest.name !== playerBest.name,
    strategicTies:base.ties || [], dataStatus:{ history:!!(profile && profile.available), matchupProviders:MATCHUP_STAT_PROVIDERS.map(p => p.id) } };
}
const compareModels = input => { const out = {}; MASTERY_MODELS.forEach(m => { out[m] = personalizedRanking(Object.assign({}, input, { model:m })); }); return out; };

// ---------- Explications : des compromis, jamais une interdiction ----------
const MASTERY_REVIEWED = "2026-11-05";
const pluralGames = n => `${n} partie${n > 1 ? "s" : ""}`;
const MASTERY_RULES = [
  { id:"mastery.compromise", priority:95, basis:"measured",
    when: x => x.compare && x.compare.differs && x.entry.name === x.compare.playerBest ? { draft:x.compare.draftBest } : null,
    build: (x, d) => ({ title:`${d.draft} pour la draft, ${x.entry.name} pour ton historique`,
      text:[`Le choix t'appartient : ${d.draft} répond mieux à la draft, et ton historique penche vers ${x.entry.name}.`,
        `Écart stratégique en faveur de ${d.draft}, compensé ici par ton expérience sur ${x.entry.name}. Les deux restent défendables.`,
        `${x.entry.name} passe devant avec les réglages de personnalisation, qui ne sont pas validés sur de vraies parties. ${d.draft} reste premier du moteur.`],
      limits:"Le poids donné à l'expérience est un choix, pas une mesure." }) },
  { id:"mastery.tie", priority:90, basis:"measured",
    when: x => { const t = x.entry.tiedWith || []; if (!t.length || !x.profile || !x.profile.available) return null; const mine = usefulExperience(x.entry.familiarity), others = t.map(n => label(n)).map(n => ({ n, e:usefulExperience(familiarityOf(x.profile, n, x.role)) })); const best = others.sort((a, b) => b.e - a.e)[0]; return Math.abs(mine - best.e) >= 5 ? { mine, best } : null; },
    build: (x, d) => { const more = d.mine > d.best.e ? x.entry.name : d.best.n; return { title:`Égalité stratégique : ton expérience peut trancher`,
      text:[`Le classement ne sépare pas ${x.entry.name} et ${d.best.n}. Tu as davantage d'expérience sur ${more} : c'est un argument, si ce champion te convient.`,
        `Égalité sur la draft entre ${x.entry.name} et ${d.best.n}. L'expérience récente est plus grande sur ${more}.`,
        `Scores du moteur identiques pour ${x.entry.name} et ${d.best.n} : l'écart ne vient que de l'expérience récente, qui penche vers ${more}.`],
      limits:"L'expérience ne dit pas à elle seule si tu joues bien ce champion." }; } },
  { id:"mastery.accessible", priority:50, basis:"official", maxLevel:1,
    when: x => x.entry.complexity.pickup.tier === "accessible" && (!x.entry.familiarity || x.entry.familiarity.count < FAMILIARITY_STEPS.learning) ? {} : null,
    build: x => ({ title:`${x.entry.name} paraît accessible`, text:[`Riot note sa difficulté ${x.entry.complexity.official.value}/10 : ce champion peut convenir pour découvrir le rôle. Cela ne garantit pas qu'il soit fort dans cette draft.`],
      limits:"Note de Riot, pas une mesure de la prise en main." }) },
  { id:"mastery.demanding", priority:72, basis:"official",
    when: x => x.entry.complexity.pickup.tier === "demanding" && (!x.entry.familiarity || x.entry.familiarity.count < FAMILIARITY_STEPS.learning) && !x.entry.learning && (x.profile && x.profile.available || x.level === "beginner") ? {} : null,
    build: x => ({ title:`${x.entry.name} demande souvent de la pratique`,
      text:[`Riot note sa difficulté ${x.entry.complexity.official.value}/10 : il faut parfois plusieurs parties pour en tirer le meilleur. Si tu as envie de l'apprendre, c'est un choix valable.`,
        `Note Riot ${x.entry.complexity.official.value}/10 et peu d'expérience récente dans ce rôle : compte sur quelques erreurs le temps de le prendre en main.`,
        `Note Riot ${x.entry.complexity.official.value}/10, moins de ${FAMILIARITY_STEPS.learning} parties dans ce rôle. La vitesse d'apprentissage réelle n'est pas mesurée.`],
      limits:"Note parfois contredite par celle du client, et aucune courbe de maîtrise mesurée : une indication, pas une prédiction." }) },
  { id:"mastery.complex-known", priority:70, basis:"measured",
    when: x => x.entry.complexity.pickup.tier === "demanding" && x.entry.familiarity && usefulExperience(x.entry.familiarity) >= COMPLEXITY_SAFE_GAMES ? {} : null,
    build: x => ({ title:`${x.entry.name} : ton expérience compte plus que sa difficulté`,
      text:[`${pluralGames(x.entry.familiarity.count)} dans ce rôle : la difficulté du champion ne pèse pas dans ce conseil.`,
        `Note Riot ${x.entry.complexity.official.value}/10, mais expérience suffisante : aucune baisse n'est appliquée.`,
        `Terme de complexité nul au-delà de ${COMPLEXITY_SAFE_GAMES} parties utiles. Aucun bonus n'est donné parce que le champion est difficile.`] }) },
  { id:"mastery.learning", priority:68, basis:"general",
    when: x => x.entry.learning ? {} : null,
    build: x => ({ title:`Tu apprends ${x.entry.name}`,
      text:[`Tu l'as marqué en apprentissage : le classement ne le pénalise pas pour sa difficulté. Quelques parties plus difficiles sont normales.`,
        `Marqué en apprentissage : aucune baisse liée à la complexité.`, `Choix manuel respecté : terme de complexité à zéro, résultats récents à lire avec prudence.`] }) },
  { id:"mastery.alternative", priority:66, basis:"measured",
    when: x => { if (!x.rival || !x.profile || !x.profile.available) return null; const a = x.entry.familiarity, b = x.rival.familiarity, na = a ? a.count : 0, nb = b ? b.count : 0; return nb >= FAMILIARITY_STEPS.learning && nb >= 3 * Math.max(1, na) ? { na, nb } : null; },
    build: (x, d) => ({ title:`Tu as davantage d'expérience sur ${x.rival.name}`,
      text:[`${pluralGames(d.nb)} sur ${x.rival.name}, ${pluralGames(d.na)} sur ${x.entry.name} dans ce rôle. À toi de voir si cela compte plus que la draft.`,
        `${x.rival.name} : ${pluralGames(d.nb)}, contre ${d.na}. Argument à peser, sans preuve que tu y réussisses mieux.`,
        `Écart d'exposition ${d.nb} contre ${d.na} dans ce rôle. Le lien avec tes résultats n'est pas établi par ce chiffre.`],
      limits:"Le nombre de parties ne dit pas si tu joues bien ce champion." }) },
  { id:"mastery.small-sample", priority:64, basis:"smallSample",
    when: x => { const e = x.entry.familiarity; return e && e.count < RESULT_MIN_GAMES ? {} : null; },
    build: x => { const e = x.entry.familiarity; return { title:`${pluralGames(e.count)} sur ${x.entry.name} : trop peu pour conclure`,
      text:[`${e.wins} victoire${e.wins > 1 ? "s" : ""} sur ${e.count} : trop peu pour savoir si ce champion te réussit.`,
        `Intervalle à 95 % : ${Math.round(e.interval[0] * 100)} à ${Math.round(e.interval[1] * 100)} %. Le résultat n'entre pas dans le classement sous ${RESULT_MIN_GAMES} parties.`,
        `Intervalle à 95 % : ${Math.round(e.interval[0] * 100)} à ${Math.round(e.interval[1] * 100)} %. Seul le volume compte ici ; le résultat ne compte qu'à partir de ${RESULT_MIN_GAMES} parties.`] }; } },
  { id:"mastery.results", priority:62, basis:"measured",
    when: x => { const e = x.entry.familiarity; return e && e.resultGap !== null ? {} : null; },
    build: x => { const e = x.entry.familiarity, pct = v => `${Math.round(v * 100)} %`, clear = e.clear !== 0; return { title:clear ? `${x.entry.name} se distingue de tes autres champions` : `${x.entry.name} : pas d'écart net avec tes autres champions`,
      text:[`${e.wins} victoires sur ${e.count}, contre ${pct(e.reference)} sur tes autres champions de ce rôle. ${clear ? "L'écart semble réel, mais d'autres raisons peuvent l'expliquer." : "L'écart peut venir du hasard."}`,
        `${pct(e.wins / e.count)} sur ${e.count} parties (intervalle ${pct(e.interval[0])} à ${pct(e.interval[1])}), contre ${pct(e.reference)} sur tes autres champions.`,
        `Écart ramené vers ta moyenne : ${e.resultGap >= 0 ? "+" : ""}${(e.resultGap * 100).toFixed(1).replace(".", ",")} points de pourcentage. Il ne contrôle ni la date, ni le matchup, ni le niveau des parties.`],
      limits:"Corrélation sur tes parties : ce n'est pas une preuve de cause à effet." }; } },
  { id:"mastery.rusty", priority:60, basis:"measured",
    when: x => x.entry.familiarity && x.entry.familiarity.rusty ? {} : null,
    build: x => { const e = x.entry.familiarity; return { title:`Tu n'as pas joué ${x.entry.name} récemment`,
      text:[`Ta dernière partie dans ce rôle remonte à ${e.daysSince} jours. Ton expérience compte encore, mais le jeu a pu changer entre-temps.`,
        `${pluralGames(e.count)}, mais la dernière il y a ${e.daysSince} jours : l'expérience est comptée en partie seulement.`,
        `Expérience utile ${e.effective.toFixed(1).replace(".", ",")} pour ${e.count} parties (demi-vie ${FAMILIARITY_HALF_LIFE} jours, plancher ${FAMILIARITY_OLD_FLOOR * 100} %).`] }; } },
  { id:"mastery.changed", priority:74, basis:"estimate",
    when: x => x.entry.familiarity && x.entry.familiarity.changed && x.entry.familiarity.changed.gamesSince < FAMILIARITY_STEPS.learning ? {} : null,
    build: x => { const e = x.entry.familiarity; return { title:`${x.entry.name} a changé depuis une partie de tes parties`,
      text:[`${e.changed.label} : ${pluralGames(e.changed.gamesSince)} seulement depuis. Ton expérience passée n'est peut-être plus aussi utile.`,
        `${e.changed.label} : ${pluralGames(e.changed.gamesSince)} depuis. L'expérience antérieure est comptée en partie.`,
        `${e.changed.label} : expérience plafonnée aux parties récentes, plus ${FAMILIARITY_OLD_FLOOR * 100} % du reste.`],
      limits:"La liste des changements est fournie à la main : elle n'existe pas dans Data Dragon." }; } },
  { id:"mastery.trend", priority:56, basis:"measured", minLevel:2,
    when: x => { const e = x.entry.familiarity; return e && e.trend && (e.trend.status === "up" || e.trend.status === "down") ? {} : null; },
    build: x => { const t = x.entry.familiarity.trend, pct = v => `${Math.round(v * 100)} %`; return { title:`${x.entry.name} : résultats ${t.status === "up" ? "en hausse" : "en baisse"}`,
      text:[``, `${pct(t.older)} sur tes parties les plus anciennes, ${pct(t.newer)} sur les plus récentes.`, `${pct(t.older)} puis ${pct(t.newer)} (z = ${t.z.toFixed(1).replace(".", ",")}). À lire seul, sans contrôle du niveau ni des patchs.`],
      limits:"Le niveau des parties et le patch peuvent aussi l'expliquer." }; } },
  { id:"mastery.matchup-unknown", priority:58, basis:"estimate",
    when: x => { if (!x.opponent) return null; const n = championComplexity(x.opponent, x.input).official.value, e = x.entry.familiarity, seen = e && e.versus[x.opponent] || 0; return x.profile && x.profile.available && n !== null && n >= COMPLEXITY_FROM && seen < DUEL_SEEN_GAMES ? { seen } : null; },
    build: (x, d) => ({ title:`${x.opponent} peut surprendre`, text:[`Ce matchup peut être difficile si tu ne connais pas les compétences de ${x.opponent}. ${d.seen ? `Rencontres avec ${x.entry.name} dans ton historique : ${d.seen}.` : `Aucune rencontre avec ${x.entry.name} dans ton historique.`}`,
      `Peu de parties contre ${x.opponent} avec ${x.entry.name} (${d.seen}). Regarde ses sorts clés avant de lancer.`, `${d.seen ? `Seulement ${d.seen} sur ce couple` : "Aucune rencontre sur ce couple"} : aucune tendance personnelle n'est tirée.`], limits:"Ta connaissance réelle n'est pas mesurée : tu as pu le jouer ou le rencontrer ailleurs." }) },
  { id:"mastery.matchup-hard-to-use", priority:76, basis:"measured",
    when: x => { const m = x.entry.matchup; return m && m.gap > 0 && usefulExperience(x.entry.familiarity) < COMPLEXITY_SAFE_GAMES && x.entry.complexity.pickup.tier === "demanding" ? { m } : null; },
    build: (x, d) => ({ title:`Matchup favorable, mais à exploiter`, text:[`Les statistiques sont favorables à ${x.entry.name} contre ${x.opponent}, mais elles viennent de joueurs qui peuvent mieux le connaître que toi.`,
      `Écart favorable sur ${d.m.games} parties, source ${d.m.provider}. Cet avantage suppose de savoir jouer ${x.entry.name}.`, `Écart de ${(d.m.gap * 100).toFixed(1).replace(".", ",")} points, ${d.m.games} parties. Aucune donnée ne dit quelle part en reviendrait à un joueur peu expérimenté.`],
      limits:"Ces statistiques ne sont pas ajustées à ton expérience." }) },
  { id:"mastery.no-history", priority:42, basis:"general",
    when: x => !x.profile || !x.profile.available ? {} : null,
    build: () => ({ title:"Aucun historique importé", text:[`Sans historique, DraftMid ne sait pas ce que tu connais déjà. Le conseil ne porte que sur la draft.`,
      `Sans import, aucune expérience ni aucun résultat personnel n'entre en compte.`, `Pas de données personnelles : l'absence d'historique n'est jamais lue comme un manque d'expérience, sauf si tu te déclares débutant.`] }) },
  { id:"mastery.no-matchup-data", priority:38, basis:"general", minLevel:2,
    when: x => x.opponent && !x.entry.matchup && !liveMatchup(x.entry.pick, x.opponent) && !matchupOf(x.opponent) ? {} : null,
    build: x => ({ title:`${x.entry.name} contre ${x.opponent} : aucune donnée fiable`, text:[``, `Ni bilan calculé, ni tableau pour ce couple : le matchup n'entre pas dans le conseil.`, `Critère de matchup absent : aucune valeur n'est estimée à sa place.`] }) }
];

const MASTERY_BASIS = { official:"Donnée officielle de Riot", measured:"Mesuré sur tes parties", smallSample:"Petit échantillon de tes parties", estimate:"Estimation", general:"Principe général" };

// Le joueur reste libre : pick permet de faire expliquer n'importe quel champion, sans jamais le retirer du classement.
function masteryAdvice(input) {
  const level = COACH_LEVELS[input.level] || COACH_LEVELS.beginner;
  const compare = personalizedRanking(Object.assign({}, input, { model:input.model || "C" }));
  const entry = (input.pick && compare.ranking.find(r => r.name === input.pick)) || compare.ranking[0];
  if (!entry) return { version:1, level:input.level || "beginner", pick:null, items:[], offline:true };
  const rival = compare.ranking.find(r => r !== entry) || null;
  const ctx = { input, level:input.level || "beginner", entry, rival, profile:input.profile || null, role:compare.role, opponent:compare.opponent, compare };
  const items = [];
  MASTERY_RULES.forEach(rule => {
    if ((rule.minLevel && level < rule.minLevel) || (rule.maxLevel && level > rule.maxLevel)) return;
    const data = rule.when(ctx);
    if (!data) return;
    const built = rule.build(ctx, data), text = built.text[Math.min(level, built.text.length) - 1];
    if (!text) return;
    items.push({ id:rule.id, category:"mastery", level:ctx.level, title:built.title, text, priority:rule.priority, basis:rule.basis, basisLabel:MASTERY_BASIS[rule.basis], limits:built.limits || null, reviewed:MASTERY_REVIEWED, verification:"human" });
  });
  items.sort((a, b) => b.priority - a.priority || (a.id < b.id ? -1 : 1));
  return { version:1, level:ctx.level, model:compare.model, pick:entry.name, draftBest:compare.draftBest, playerBest:compare.playerBest, items, offline:true };
}
