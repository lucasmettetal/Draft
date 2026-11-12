// Familiarité, difficulté et présentation des compromis. PROTOTYPE : chargé par aucune page de l'application.
// Pur et déterministe : lit le moteur sans le modifier.
//
// Le score du moteur décide. La personnalisation sert d'abord à expliquer :
//   - le meilleur choix stratégique et le champion le plus familier sont présentés séparément ;
//   - la familiarité dit combien tu as joué un champion, jamais si tu le joues bien ;
//   - la difficulté de Riot n'alimente que des conseils, jamais le score ;
//   - seule une vraie égalité peut être départagée par l'expérience.
//
// Rien ne compare à d'autres joueurs, et aucun niveau global n'est calculé.

const MASTERY_DAY = 86400000;
const FAMILIARITY_HALF_LIFE = 120;     // jours : une partie compte moitié moins après ce délai
const FAMILIARITY_OLD_FLOOR = .25;     // une expérience ancienne compte encore : on ne redevient pas novice
const FAMILIARITY_STEPS = { discovery:5, learning:20, familiar:60 };
const FAMILIARITY_RUSTY_DAYS = 180;
const FAMILIARITY_RECENT_DAYS = 30;
const FAMILIAR_MIN_GAMES = 5;          // en dessous, personne n'est présenté comme « le plus familier »
const FAMILIAR_CLEAR_GAP = 5;          // parties utiles d'écart pour parler d'une vraie différence d'expérience
const RESULT_MIN_GAMES = 30, RESULT_SHRINK = 50;
const DIFFICULTY_DEMANDING = 8, DIFFICULTY_ACCESSIBLE = 3;
const TREND_MIN_GAMES = 40, TREND_MIN_Z = 2;
const KDA_MIN_GAMES = 20;
const DUEL_SEEN_GAMES = 5;
// Ces deux réglages ne servent que dans les pages d'audit, avec experimental:true.
const EXPERIMENT_EXPERIENCE_MAX = 1.5, EXPERIMENT_EXPERIENCE_SCALE = 25, EXPERIMENT_RESULT_MAX = .75, EXPERIMENT_RESULT_FULL_GAP = .1;

// ---------- Noms : l'API Riot écrit KogMaw, Chogath ou MonkeyKing ----------
function masteryName(riotChampion) {
  const key = norm(String(riotChampion || ""));
  if (!masteryName.map) { masteryName.map = {}; NAMES.forEach(n => { masteryName.map[riotKey(n)] = n; masteryName.map[norm(n)] = n; }); }
  return masteryName.map[key] || null;
}
const MASTERY_ROLES = { TOP:"top", JUNGLE:"jungle", MIDDLE:"mid", MID:"mid", BOTTOM:"adc", ADC:"adc", UTILITY:"support", SUPPORT:"support" };

// Historique disponible : celui de l'import depuis l'application, sinon l'ancien import. Lecture seule.
function importedHistory() {
  if (window.DRAFT_HISTORY && Array.isArray(window.DRAFT_HISTORY.rows)) return { source:"application", rows:window.DRAFT_HISTORY.rows, generated:window.DRAFT_HISTORY.generated || null };
  if (window.DRAFT_STATS && Array.isArray(window.DRAFT_STATS.rows)) return { source:"script", rows:window.DRAFT_STATS.rows, generated:window.DRAFT_STATS.generated || null };
  return null;
}

// ---------- Complexité : cinq notions distinctes, et « inconnu » quand rien ne les mesure ----------
const DIFFICULTY_LIMIT = I18N.t("mastery.difficulty.limit");
function championComplexity(name) {
  const official = typeof CHAMPION_OFFICIAL !== "undefined" ? CHAMPION_OFFICIAL.champions[name] : null;
  // Riot publie 0 pour quelques champions : cela veut dire absent, pas facile.
  const note = official && official.difficulty > 0 ? official.difficulty : null;
  return {
    champion: name,
    official: { value:note, scale:"1-10", source:I18N.t("mastery.difficulty.source"), version:official ? CHAMPION_OFFICIAL.version : null, status:note === null ? "unknown" : "known", limit:DIFFICULTY_LIMIT },
    mechanical: { value:null, status:"unknown", limit:I18N.t("mastery.difficulty.mechanicalLimit") },
    pickup: { tier: note === null ? null : note <= DIFFICULTY_ACCESSIBLE ? "accessible" : note < DIFFICULTY_DEMANDING ? "moderate" : "demanding", status:note === null ? "unknown" : "proxy",
      limit:I18N.t("mastery.difficulty.pickupLimit") },
    curve: { value:null, status:"unmeasured", limit:I18N.t("mastery.difficulty.curveLimit") }
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
    e.changed = change && change.time ? { label:change.label || I18N.t("mastery.changed.default"), gamesSince:g.filter(x => (x.time || 0) >= change.time).length } : null;
    e.versus = {};
    g.forEach(x => (x.enemies || []).forEach(en => { const n = masteryName(en); if (n) e.versus[n] = (e.versus[n] || 0) + 1; }));
    delete e.games;
  });
  return { available:list.length > 0, games:list.length, entries, unknown, officialMastery:options.officialMastery || null };
}

const familiarityOf = (profile, name, role) => profile && profile.entries[`${name}|${role}`] || null;

// Expérience utile : parties récentes, plancher pour le passé. Ne dépend jamais des victoires.
// Un champion modifié garde l'expérience gagnée depuis le changement, plus une partie du passé.
function usefulExperience(entry) {
  if (!entry) return 0;
  if (!entry.changed) return entry.effective;
  return Math.min(entry.effective, entry.changed.gamesSince + entry.effective * FAMILIARITY_OLD_FLOOR);
}

// ---------- Fournisseur de matchups interchangeable ----------
// Aucune source n'est branchée (docs/SOURCES-MATCHUPS.md). Ces valeurs ne changent aucun score : elles ne peuvent qu'être affichées.
// lookup(pick, adversaire, { role }) renvoie { games, wins, baseline, role, patch, rank, region } ou null.
const MATCHUP_STAT_PROVIDERS = [];
function registerMatchupProvider(provider) {
  if (!provider || !provider.id || !provider.license || !provider.terms || typeof provider.lookup !== "function") throw new Error("Fournisseur incomplet : id, license, terms et lookup sont obligatoires.");
  MATCHUP_STAT_PROVIDERS.push(provider);
}
function providedMatchup(pick, opponent, role) {
  if (!opponent) return null;
  for (const provider of MATCHUP_STAT_PROVIDERS) {
    const row = provider.lookup(pick, opponent, { role });
    // Une ligne sans rôle exact, sans référence ou avec trop peu de parties ne compte pas : pas de valeur inventée.
    if (!row || row.role !== role || !row.games || row.games < MATCHUP_MIN_GAMES || typeof row.baseline !== "number" || typeof row.wins !== "number") continue;
    return Object.assign({ provider:provider.id, license:provider.license, gap:(row.wins + RESULT_SHRINK * row.baseline) / (row.games + RESULT_SHRINK) - row.baseline }, row);
  }
  return null;
}

// ---------- Classement ----------
// A : le moteur, inchangé. T : le moteur, et l'expérience seulement entre champions vraiment à égalité.
// B reste une expérience d'audit : sans experimental:true, il donne A. Il n'existe plus de terme de difficulté.
const masteryKey = v => Math.round(v * 1e6);
const riskOf = entry => masteryKey(Math.min(0, ...entry.why.map(w => w.v)));
function personalizedRanking(input) {
  const asked = input.model === "T" ? "T" : input.model === "B" && input.experimental === true ? "B" : "A";
  const enemies = input.enemies || [], fixed = input.fixedRoles || {};
  const manual = input.manualOpponent && enemies.includes(input.manualOpponent) ? input.manualOpponent : null;
  const opponent = manual || identifiedOpponent(enemies, fixed);
  const belief = opponentBelief(enemies, fixed);
  const scenarios = manual || !enemies.length ? null : beliefScenarios(belief);
  const base = analyze(enemies, opponent, input.hasFront !== false, scenarios);
  const role = myRole(), profile = input.profile || null;
  const rows = base.ranking.map((entry, strategicRank) => {
    const name = label(entry.pick), familiarity = familiarityOf(profile, name, role);
    return { pick:entry.pick, name, strategicRank, score:entry.score, total:entry.score, risk:riskOf(entry), tiedWith:entry.tiedWith.map(label), reasons:entry.criteria, familiarity, experience:usefulExperience(familiarity), complexity:championComplexity(name), learning:(input.learning || []).includes(name) };
  });
  let ranking = rows, tieBreak = null;
  if (asked === "T" && rows.length) {
    // Vraie égalité : même score ET même risque, sans favori choisi. Le meneur doit avoir une vraie avance d'expérience.
    const group = rows.filter(r => masteryKey(r.score) === masteryKey(rows[0].score) && r.risk === rows[0].risk);
    const byExperience = group.slice().sort((a, b) => b.experience - a.experience);
    const favorite = group.some(r => r.pick === ENGINE_PREFS.favorite);
    if (group.length > 1 && !favorite && byExperience[0].experience - byExperience[1].experience >= FAMILIAR_CLEAR_GAP) {
      const leader = byExperience[0];
      ranking = [leader].concat(rows.filter(r => r !== leader));
      tieBreak = { applied:leader !== rows[0], winner:leader.name, group:group.map(r => r.name) };
    }
  }
  if (asked === "B") {
    ranking = rows.map(r => {
      const e = r.familiarity;
      const experience = profile && profile.available ? EXPERIMENT_EXPERIENCE_MAX * (1 - Math.exp(-r.experience / EXPERIMENT_EXPERIENCE_SCALE)) : 0;
      const result = e && e.resultGap !== null ? Math.max(-EXPERIMENT_RESULT_MAX, Math.min(EXPERIMENT_RESULT_MAX, e.resultGap / EXPERIMENT_RESULT_FULL_GAP * EXPERIMENT_RESULT_MAX)) : 0;
      return Object.assign({}, r, { total:r.score + experience + result });
    }).sort((a, b) => masteryKey(b.total) - masteryKey(a.total) || a.strategicRank - b.strategicRank);
  }
  return { model:asked, role, opponent, belief, manual, ranking, strategicRanking:rows, tieBreak, strategicTies:base.ties || [], history:!!(profile && profile.available), matchupProviders:MATCHUP_STAT_PROVIDERS.map(p => p.id) };
}

// Le champion le plus joué récemment dans ce rôle, dans le pool. Le nombre de victoires n'intervient pas.
function mostFamiliar(rows) {
  const played = rows.filter(r => r.familiarity && r.familiarity.count >= FAMILIAR_MIN_GAMES).sort((a, b) => b.experience - a.experience || a.strategicRank - b.strategicRank);
  if (!played.length) return null;
  const near = played.filter(r => played[0].experience - r.experience < FAMILIAR_CLEAR_GAP);
  return { row:played[0], close:near.slice(1).map(r => r.name) };
}

// ---------- Présentation : théorie, expérience et incertitude restent séparées ----------
const MASTERY_REVIEWED = "2026-11-05";
const MASTERY_BASIS = { official:I18N.t("mastery.basis.official"), measured:I18N.t("mastery.basis.measured"), smallSample:I18N.t("mastery.basis.smallSample"), estimate:I18N.t("mastery.basis.estimate"), heuristic:I18N.t("mastery.basis.heuristic"), general:I18N.t("mastery.basis.general") };
// Nombres de parties : pluriels du catalogue (tp).
const masteryPct = v => I18N.t("mastery.percent", { value: Math.round(v * 100) });
// Décimales : virgule en français, point en anglais (I18N.fmtDecimal).
const masteryDecimal = (v, n = 1) => I18N.fmtDecimal(v, n);
const masteryItem = (id, section, basis, title, text, limits, extra) => Object.assign({ id, section, title, text, basis, basisLabel:MASTERY_BASIS[basis], limits:limits || null, reviewed:MASTERY_REVIEWED, verification:"human" }, extra || {});
const byLevel = (level, texts) => texts[Math.min(level, texts.length) - 1];
const nameList = names => I18N.listAnd(names);

function theorySection(x) {
  const { top, level, opponent } = x, out = [];
  const best = top.reasons.filter(c => c.impact > 0 && c.text).sort((a, b) => b.impact - a.impact)[0];
  if (x.names.length > 1) out.push(masteryItem("theory.tie", "theory", "heuristic", I18N.t("mastery.theory.tie.title", { champions: nameList(x.names) }), byLevel(level, [
    I18N.t("mastery.theory.tie.l1"), I18N.t("mastery.theory.tie.l2"), I18N.t("mastery.theory.tie.l3", { score: masteryDecimal(top.score) })]), I18N.t("mastery.theory.tie.limit")));
  if (best) out.push(masteryItem("theory.reason", "theory", "heuristic", I18N.t("mastery.theory.reason.title", { champion: top.name }), byLevel(level, [
    I18N.t("mastery.theory.reason.l1", { reason: coachReason(signedText(best, 1) || best.text) }), I18N.t("mastery.theory.reason.l2", { criterion: best.label, reason: coachReason(signedText(best, 1) || best.text) }), I18N.t("mastery.theory.reason.l3", { criterion: best.label, impact: masteryDecimal(best.impact) })]), I18N.t("mastery.limit.rule")));
  else if (!x.enemiesCount) out.push(masteryItem("theory.empty", "theory", "general", I18N.t("mastery.theory.empty.title"), I18N.t("mastery.theory.empty.text"), null));
  else out.push(masteryItem("theory.none", "theory", "heuristic", I18N.t("mastery.theory.none.title", { champion: top.name }), I18N.t("mastery.theory.none.text"), I18N.t("mastery.theory.none.limit")));
  const worst = top.reasons.filter(c => c.impact <= -2).sort((a, b) => a.impact - b.impact)[0];
  if (worst) out.push(masteryItem("theory.risk", "theory", "heuristic", I18N.t("mastery.theory.risk.title", { champion: top.name }), I18N.t("mastery.sentence", { text: I18N.upperFirst(signedText(worst, -1) || worst.text) }), I18N.t("mastery.limit.rule")));
  const stat = providedMatchup(top.name, opponent, x.role);
  if (stat) {
    out.push(masteryItem("theory.matchup-stat", "theory", "measured", I18N.t("mastery.theory.stat.title", { champion: top.name, opponent }), byLevel(level, [
      I18N.t(stat.gap > 0 ? "mastery.theory.stat.l1.favorable" : "mastery.theory.stat.l1.unfavorable", { games: stat.games }), I18N.t("mastery.theory.stat.l2", { gap: masteryDecimal(stat.gap * 100), games: stat.games, provider: stat.provider }), I18N.t("mastery.theory.stat.l3", { gap: masteryDecimal(stat.gap * 100), games: stat.games, patch: stat.patch || I18N.t("mastery.unknown"), rank: stat.rank || I18N.t("mastery.unknown"), region: stat.region || I18N.t("mastery.unknownF"), provider: stat.provider })]), I18N.t("mastery.theory.stat.limit"), { provider:stat.provider, license:stat.license }));
    if (stat.gap > 0 && (!top.familiarity || top.familiarity.count < FAMILIARITY_STEPS.learning)) out.push(masteryItem("theory.matchup-to-exploit", "theory", "estimate", I18N.t("mastery.theory.exploit.title"), I18N.t("mastery.theory.exploit.text", { champion: top.name }), I18N.t("mastery.theory.exploit.limit")));
  }
  // La difficulté enrichit le conseil, sans jamais changer le score.
  const tier = top.complexity.pickup.tier, note = top.complexity.official.value;
  if (tier === "accessible" && (!top.familiarity || top.familiarity.count < FAMILIARITY_STEPS.learning)) out.push(masteryItem("theory.accessible", "theory", "official", I18N.t("mastery.theory.accessible.title", { champion: top.name }), byLevel(level, [
    I18N.t("mastery.theory.accessible.l1", { note }), I18N.t("mastery.theory.accessible.l2", { note }), I18N.t("mastery.theory.difficulty.l3", { note })]), DIFFICULTY_LIMIT));
  if (tier === "demanding" && !top.learning && (!top.familiarity || top.familiarity.count < FAMILIARITY_STEPS.learning)) out.push(masteryItem("theory.demanding", "theory", "official", I18N.t("mastery.theory.demanding.title", { champion: top.name }), byLevel(level, [
    I18N.t("mastery.theory.demanding.l1", { note }), I18N.t("mastery.theory.demanding.l2", { note }), I18N.t("mastery.theory.demanding.l3", { note })]), DIFFICULTY_LIMIT));
  if (tier === null && !top.familiarity && CHAMPION_OFFICIAL.champions[top.name]) out.push(masteryItem("theory.difficulty-unknown", "theory", "official", I18N.t("mastery.theory.difficultyUnknown.title", { champion: top.name }), I18N.t("mastery.theory.difficultyUnknown.text"), null));
  return out;
}

function experienceSection(x) {
  const { top, familiar, level, profile } = x, out = [];
  if (!profile || !profile.available) return out;
  const describe = r => r.familiarity ? I18N.tp(r.familiarity.recentGames ? "mastery.experience.gamesRecent" : "mastery.experience.games", r.familiarity.count, { champion: r.name, recent: r.familiarity.recentGames }) : I18N.t("mastery.experience.none", { champion: r.name });
  const same = familiar && familiar.row === top;
  out.push(masteryItem("experience.strategic", "experience", top.familiarity && top.familiarity.count < RESULT_MIN_GAMES ? "smallSample" : "measured", I18N.t("mastery.experience.strategic.title", { champion: top.name }), describe(top), I18N.t(top.familiarity ? "mastery.experience.strategic.limit" : "mastery.experience.strategic.limitNone")));
  if (familiar && !same) out.push(masteryItem("experience.familiar", "experience", "measured", I18N.t("mastery.experience.familiar.title", { champion: familiar.row.name }), describe(familiar.row), I18N.t("mastery.experience.familiar.limit")));
  if (top.learning) out.push(masteryItem("experience.learning", "experience", "general", I18N.t("mastery.experience.learning.title", { champion: top.name }), I18N.t("mastery.experience.learning.text"), null));
  const e = top.familiarity;
  if (e && e.resultGap !== null) out.push(masteryItem("experience.results", "experience", "measured", I18N.t(e.clear ? "mastery.experience.results.clear" : "mastery.experience.results.unclear", { champion: top.name }), byLevel(level, [
    I18N.t(e.clear ? "mastery.experience.results.l1.clear" : "mastery.experience.results.l1.unclear", { wins: e.wins, count: e.count, reference: masteryPct(e.reference) }),
    I18N.t("mastery.experience.results.l2", { rate: masteryPct(e.wins / e.count), count: e.count, low: masteryPct(e.interval[0]), high: masteryPct(e.interval[1]), reference: masteryPct(e.reference) }),
    I18N.t("mastery.experience.results.l3", { gap: `${e.resultGap >= 0 ? "+" : ""}${masteryDecimal(e.resultGap * 100)}` })]), I18N.t("mastery.experience.results.limit")));
  if (e && e.trend && (e.trend.status === "up" || e.trend.status === "down") && level >= 2) out.push(masteryItem("experience.trend", "experience", "measured", I18N.t(e.trend.status === "up" ? "mastery.experience.trend.up" : "mastery.experience.trend.down", { champion: top.name }), byLevel(level, ["", I18N.t("mastery.experience.trend.l2", { older: masteryPct(e.trend.older), newer: masteryPct(e.trend.newer) }), I18N.t("mastery.experience.trend.l3", { older: masteryPct(e.trend.older), newer: masteryPct(e.trend.newer), z: masteryDecimal(e.trend.z) })]), I18N.t("mastery.experience.trend.limit")));
  return out;
}

function uncertaintySection(x) {
  const { top, familiar, level, profile, result, opponent, enemiesCount } = x, out = [];
  if (!profile || !profile.available) out.push(masteryItem("uncertainty.no-history", "uncertainty", "general", I18N.t("mastery.uncertainty.noHistory.title"), byLevel(level, [
    I18N.t("mastery.uncertainty.noHistory.l1"), I18N.t("mastery.uncertainty.noHistory.l2"), I18N.t("mastery.uncertainty.noHistory.l3")]), null));
  if (enemiesCount > 0 && enemiesCount < 5) out.push(masteryItem("uncertainty.incomplete", "uncertainty", "general", I18N.tp("mastery.uncertainty.incomplete.title", 5 - enemiesCount), I18N.t("mastery.uncertainty.incomplete.text"), null));
  // Même échelle que l'interface : probable, ou hypothèse avec 1 ou 2 champions vus ; pas identifié, pas encore choisi.
  const thin = enemiesCount <= 2;
  if (opponent && !result.manual && (thin || result.belief.confidence !== "high")) out.push(masteryItem("uncertainty.opponent", "uncertainty", "estimate", I18N.t(thin ? "mastery.uncertainty.opponent.thin" : "mastery.uncertainty.opponent.probable", { role: oppName(), champion: opponent }), I18N.t("mastery.uncertainty.opponent.text"), I18N.t("mastery.uncertainty.opponent.limit")));
  if (enemiesCount > 0 && !opponent) {
    const named = result.belief.candidates.some(c => c.known && c.p >= .2);
    out.push(masteryItem("uncertainty.no-opponent", "uncertainty", "estimate", I18N.t(!named && result.belief.unrevealed >= .2 ? "mastery.uncertainty.noOpponent.notYet" : "mastery.uncertainty.noOpponent.unknown", { role: oppName() }), I18N.t("mastery.uncertainty.noOpponent.text"), null));
  }
  if (opponent && !liveMatchup(top.pick, opponent) && !matchupOf(opponent) && !providedMatchup(top.name, opponent, x.role)) out.push(masteryItem("uncertainty.no-matchup", "uncertainty", "general", I18N.t("mastery.uncertainty.noMatchup.title", { champion: top.name, opponent }), I18N.t("mastery.uncertainty.noMatchup.text"), null));
  const e = top.familiarity;
  if (e && e.count < RESULT_MIN_GAMES) out.push(masteryItem("uncertainty.small-sample", "uncertainty", "smallSample", I18N.tp("mastery.uncertainty.smallSample.title", e.count, { champion: top.name }), byLevel(level, [
    I18N.tp("mastery.uncertainty.smallSample.l1", e.wins, { games: e.count }), I18N.t("mastery.uncertainty.smallSample.l2", { low: masteryPct(e.interval[0]), high: masteryPct(e.interval[1]) }), I18N.t("mastery.uncertainty.smallSample.l3", { low: masteryPct(e.interval[0]), high: masteryPct(e.interval[1]), min: RESULT_MIN_GAMES })]), null));
  [top, familiar && familiar.row].filter((r, i, l) => r && l.indexOf(r) === i).forEach(r => {
    const f = r.familiarity;
    if (f && f.rusty) out.push(masteryItem(`uncertainty.rusty.${r.name}`, "uncertainty", "measured", I18N.t("mastery.uncertainty.rusty.title", { champion: r.name }), I18N.t("mastery.uncertainty.rusty.text", { days: f.daysSince }), null));
    if (f && f.changed && f.changed.gamesSince < FAMILIARITY_STEPS.learning) out.push(masteryItem(`uncertainty.changed.${r.name}`, "uncertainty", "estimate", I18N.t("mastery.uncertainty.changed.title", { champion: r.name }), I18N.tp("mastery.uncertainty.changed.text", f.changed.gamesSince, { label: f.changed.label }), I18N.t("mastery.uncertainty.changed.limit")));
  });
  if (opponent && profile && profile.available) {
    const note = championComplexity(opponent).official.value, seen = e && e.versus[opponent] || 0;
    if (note !== null && note >= DIFFICULTY_DEMANDING && seen < DUEL_SEEN_GAMES) out.push(masteryItem("uncertainty.opponent-unknown", "uncertainty", "estimate", I18N.t("mastery.uncertainty.opponentUnknown.title", { champion: opponent }), byLevel(level, [
      I18N.t("mastery.uncertainty.opponentUnknown.l1"), seen ? I18N.tp("mastery.uncertainty.opponentUnknown.l2", seen, { champion: top.name }) : I18N.t("mastery.uncertainty.opponentUnknown.l2None", { champion: top.name }), I18N.tp("mastery.uncertainty.opponentUnknown.l3", seen)]), I18N.t("mastery.uncertainty.opponentUnknown.limit")));
  }
  return out;
}

function compromiseOf(x) {
  const { best:top, familiar, level, names, profile, result, chosen } = x;
  if (!top) return { kind:"none", text:I18N.t("mastery.compromise.none") };
  if (chosen && !names.includes(chosen.name)) return { kind:"chosen", text:I18N.t("mastery.compromise.chosen", { champion: chosen.name, champions: nameList(names) }) };
  if (!profile || !profile.available) return { kind:"no-history", text:I18N.t("mastery.compromise.noHistory") };
  if (!familiar) return { kind:"no-familiar", text:I18N.t("mastery.compromise.noFamiliar", { min: FAMILIAR_MIN_GAMES }) };
  const f = familiar.row.name, severalFamiliar = familiar.close.length ? I18N.tp("mastery.compromise.severalFamiliar", familiar.close.length, { champions: nameList(familiar.close) }) : "";
  // Le nom affiché peut être maintenu alors qu'un autre mène de peu : la lecture de la draft parle du vrai 1er.
  const lead = !chosen && x.lead ? x.lead : top, heads = !chosen && x.lead ? [x.lead.name].concat(x.lead.tiedWith) : names;
  if (familiar.row === lead) return { kind:"same", text:I18N.t("mastery.compromise.same", { champion: f, more: severalFamiliar }) };
  if (heads.includes(f) && heads.length > 1) {
    const near = familiar.close.filter(n => heads.includes(n));
    return { kind:"tie", tieBreak:result.tieBreak, text:near.length
      ? I18N.t("mastery.compromise.tieNear", { champions: nameList(heads), near: nameList([f].concat(near)) })
      : I18N.t("mastery.compromise.tie", { champions: nameList(heads), champion: f }) };
  }
  const gap = lead.score - familiar.row.score, big = gap >= HOLD_GAP, head = nameList(heads);
  return { kind:big ? "gap-large" : "gap-small", gap:+gap.toFixed(2), text:byLevel(level, [
    I18N.t(big ? "mastery.compromise.gapLarge.l1" : "mastery.compromise.gapSmall.l1", { champions: head, champion: f }),
    I18N.t(big ? "mastery.compromise.gapLarge.l2" : "mastery.compromise.gapSmall.l2", { champions: head, champion: f }),
    I18N.tp("mastery.compromise.gap.l3", Math.abs(gap), { gap: masteryDecimal(gap), champions: head, threshold: HOLD_GAP })]) + severalFamiliar };
}

// Point d'entrée de la présentation. Le classement renvoyé reste celui du moteur : rien n'est décidé à la place du joueur.
function playerChoices(input) {
  const level = COACH_LEVELS[input.level] || COACH_LEVELS.beginner;
  const result = personalizedRanking(Object.assign({}, input, { model:"T", experimental:false }));
  const rows = result.strategicRanking, profile = input.profile || null;
  const chosen = input.pick ? rows.find(r => r.name === input.pick) || null : null;
  // displayed : le nom que le verdict affiche, stabilisé pendant la draft. Une seule histoire à l'écran.
  const best = (input.displayed && rows.find(r => r.pick === input.displayed)) || rows[0] || null, top = chosen || best;
  const names = best ? [best.name].concat(best.tiedWith) : [];
  const familiar = mostFamiliar(rows);
  // Nom affiché maintenu pendant la draft alors qu'un autre pick mène de peu : celui-là est le vrai 1er.
  const lead = best && rows[0] && rows[0] !== best && masteryKey(rows[0].score) > masteryKey(best.score) ? rows[0] : null;
  const x = { input, level, top, best, lead, names, chosen, familiar, profile, result, role:result.role, opponent:result.opponent, enemiesCount:(input.enemies || []).length };
  if (!top) return { version:1, level:input.level || "beginner", strategic:null, familiar:null, compromise:compromiseOf(x), theory:[], experience:[], uncertainty:[], ranking:[], scoresChanged:false, offline:true };
  return {
    version:1, level:input.level || "beginner", role:result.role, opponent:result.opponent, confidence:result.manual ? "manuel" : result.belief.confidence,
    // Le meilleur choix de la draft, et lui seul : le score ne dépend ni du joueur, ni de la difficulté.
    strategic:{ champions:names, score:+best.score.toFixed(2), tied:names.length > 1, label:names.length > 1 ? I18N.t("mastery.strategic.tie") : lead ? I18N.t("mastery.strategic.held", { champion: lead.name }) : I18N.t("mastery.strategic.best"), source:MASTERY_BASIS.heuristic, leader:lead ? lead.name : null },
    // Le plus joué : ce n'est pas une évaluation de performance.
    familiar:familiar ? { champion:familiar.row.name, games:familiar.row.familiarity.count, recentGames:familiar.row.familiarity.recentGames, daysSince:familiar.row.familiarity.daysSince, rusty:familiar.row.familiarity.rusty, usefulExperience:+familiar.row.experience.toFixed(1), strategicRank:familiar.row.strategicRank + 1, close:familiar.close, label:I18N.t("mastery.familiar.label"), source:MASTERY_BASIS.measured, isPerformance:false } : null,
    explained:top.name, compromise:compromiseOf(x), tieBreak:result.tieBreak,
    theory:theorySection(x), experience:experienceSection(x), uncertainty:uncertaintySection(x),
    experienceTable:rows.map(r => ({ champion:r.name, games:r.familiarity ? r.familiarity.count : 0, recentGames:r.familiarity ? r.familiarity.recentGames : 0, daysSince:r.familiarity ? r.familiarity.daysSince : null, usefulExperience:+r.experience.toFixed(1) })),
    ranking:rows.map(r => ({ champion:r.name, score:+r.score.toFixed(2), tiedWith:r.tiedWith })), offline:true
  };
}
