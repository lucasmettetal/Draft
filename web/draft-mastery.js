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
const DIFFICULTY_LIMIT = "Note de Riot non documentée et parfois contredite par celle du client : une indication, pas une mesure.";
function championComplexity(name) {
  const official = typeof CHAMPION_OFFICIAL !== "undefined" ? CHAMPION_OFFICIAL.champions[name] : null;
  // Riot publie 0 pour quelques champions : cela veut dire absent, pas facile.
  const note = official && official.difficulty > 0 ? official.difficulty : null;
  return {
    champion: name,
    official: { value:note, scale:"1-10", source:"Riot, Data Dragon", version:official ? CHAMPION_OFFICIAL.version : null, status:note === null ? "unknown" : "known", limit:DIFFICULTY_LIMIT },
    mechanical: { value:null, status:"unknown", limit:"Aucune source autorisée ne la mesure." },
    pickup: { tier: note === null ? null : note <= DIFFICULTY_ACCESSIBLE ? "accessible" : note < DIFFICULTY_DEMANDING ? "moderate" : "demanding", status:note === null ? "unknown" : "proxy",
      limit:"Déduit de la seule note de Riot : ce n'est pas une mesure de la prise en main." },
    curve: { value:null, status:"unmeasured", limit:"Aucune courbe de maîtrise mesurée. Il en faudrait une par champion, sur beaucoup de joueurs." }
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
    e.changed = change && change.time ? { label:change.label || "Modifié", gamesSince:g.filter(x => (x.time || 0) >= change.time).length } : null;
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
const MASTERY_BASIS = { official:"Donnée officielle de Riot", measured:"Mesuré sur tes parties", smallSample:"Petit échantillon de tes parties", estimate:"Estimation", heuristic:"Règle écrite à la main", general:"Principe général" };
const pluralGames = n => `${n} partie${n > 1 ? "s" : ""}`;
const masteryPct = v => `${Math.round(v * 100)} %`;
const masteryDecimal = (v, n = 1) => v.toFixed(n).replace(".", ",");
const masteryItem = (id, section, basis, title, text, limits, extra) => Object.assign({ id, section, title, text, basis, basisLabel:MASTERY_BASIS[basis], limits:limits || null, reviewed:MASTERY_REVIEWED, verification:"human" }, extra || {});
const byLevel = (level, texts) => texts[Math.min(level, texts.length) - 1];
const nameList = names => names.length < 2 ? names.join("") : `${names.slice(0, -1).join(", ")} et ${names[names.length - 1]}`;

function theorySection(x) {
  const { top, level, opponent } = x, out = [];
  const best = top.reasons.filter(c => c.impact > 0 && c.text).sort((a, b) => b.impact - a.impact)[0];
  if (x.names.length > 1) out.push(masteryItem("theory.tie", "theory", "heuristic", `Égalité : ${nameList(x.names)}`, byLevel(level, [
    "Les règles de DRAFT ne les séparent pas dans cette draft.", "Même score sur la draft : ces règles ne permettent pas de les distinguer.", `Score identique (${masteryDecimal(top.score)}). L'ordre affiché ne porte aucune information.`]), "Ce n'est pas une preuve qu'ils se valent en jeu."));
  if (best) out.push(masteryItem("theory.reason", "theory", "heuristic", `Pourquoi ${top.name} en théorie`, byLevel(level, [
    `La raison principale : ${coachReason(best.text)}.`, `« ${best.label} » pèse le plus : ${coachReason(best.text)}.`, `Critère le plus lourd : « ${best.label} » (+${masteryDecimal(best.impact)}).`]), "Règle écrite à la main, pas une statistique."));
  else if (!x.enemiesCount) out.push(masteryItem("theory.empty", "theory", "general", "Pas encore d'adversaire", "Sans adversaire visible, la théorie n'a presque rien à comparer.", null));
  else out.push(masteryItem("theory.none", "theory", "heuristic", `${top.name} sur cette draft`, "Aucune règle ne le met particulièrement en avant ici.", "Les fiches sont écrites à la main et parfois incomplètes."));
  const worst = top.reasons.filter(c => c.impact <= -2).sort((a, b) => a.impact - b.impact)[0];
  if (worst) out.push(masteryItem("theory.risk", "theory", "heuristic", `Le risque de ${top.name}`, `${worst.text.charAt(0).toUpperCase()}${worst.text.slice(1)}.`, "Règle écrite à la main, pas une statistique."));
  const stat = providedMatchup(top.name, opponent, x.role);
  if (stat) {
    out.push(masteryItem("theory.matchup-stat", "theory", "measured", `${top.name} contre ${opponent} : statistique externe`, byLevel(level, [
      `Les statistiques sont ${stat.gap > 0 ? "plutôt favorables" : "plutôt défavorables"} sur ${stat.games} parties. Elles n'entrent pas dans le classement.`, `Écart ramené vers la référence : ${masteryDecimal(stat.gap * 100)} points, ${stat.games} parties, source ${stat.provider}.`, `Écart ${masteryDecimal(stat.gap * 100)} points, ${stat.games} parties, patch ${stat.patch || "inconnu"}, rang ${stat.rank || "inconnu"}, région ${stat.region || "inconnue"}, source ${stat.provider}.`]), "Ces joueurs peuvent connaître ce champion mieux que toi.", { provider:stat.provider, license:stat.license }));
    if (stat.gap > 0 && (!top.familiarity || top.familiarity.count < FAMILIARITY_STEPS.learning)) out.push(masteryItem("theory.matchup-to-exploit", "theory", "estimate", "Favorable, mais à exploiter", `Ces statistiques supposent de savoir jouer ${top.name}. Aucune donnée ne dit quelle part de cet avantage revient à un joueur qui le découvre.`, "Statistiques non ajustées à ton expérience."));
  }
  // La difficulté enrichit le conseil, sans jamais changer le score.
  const tier = top.complexity.pickup.tier, note = top.complexity.official.value;
  if (tier === "accessible" && (!top.familiarity || top.familiarity.count < FAMILIARITY_STEPS.learning)) out.push(masteryItem("theory.accessible", "theory", "official", `${top.name} paraît accessible`, byLevel(level, [
    `Riot note sa difficulté ${note}/10 : ce champion peut convenir pour découvrir le rôle.`, `Note Riot ${note}/10. Cela ne dit rien de sa force dans cette draft.`, `Note Riot ${note}/10, sans effet sur le score.`]), DIFFICULTY_LIMIT));
  if (tier === "demanding" && !top.learning && (!top.familiarity || top.familiarity.count < FAMILIARITY_STEPS.learning)) out.push(masteryItem("theory.demanding", "theory", "official", `${top.name} peut demander de la pratique`, byLevel(level, [
    `Riot note sa difficulté ${note}/10 : il faut parfois plusieurs parties pour en tirer le meilleur. Si tu as envie de l'apprendre, c'est un choix valable.`, `Note Riot ${note}/10 : prévois un temps de prise en main si tu le découvres. Ce champion reste un choix valable.`, `Note Riot ${note}/10, sans effet sur le score. La vitesse d'apprentissage réelle n'est pas mesurée.`]), DIFFICULTY_LIMIT));
  if (tier === null && !top.familiarity && CHAMPION_OFFICIAL.champions[top.name]) out.push(masteryItem("theory.difficulty-unknown", "theory", "official", `Difficulté de ${top.name} inconnue`, "Riot ne publie pas de note exploitable dans ces données. Ce n'est ni un signe de facilité, ni de difficulté.", null));
  return out;
}

function experienceSection(x) {
  const { top, familiar, level, profile } = x, out = [];
  if (!profile || !profile.available) return out;
  const describe = r => r.familiarity ? `${pluralGames(r.familiarity.count)} sur ${r.name} dans ce rôle${r.familiarity.recentGames ? `, dont ${r.familiarity.recentGames} ces 30 derniers jours` : ""}.` : `Aucune partie importée sur ${r.name} dans ce rôle.`;
  const same = familiar && familiar.row === top;
  out.push(masteryItem("experience.strategic", "experience", top.familiarity && top.familiarity.count < RESULT_MIN_GAMES ? "smallSample" : "measured", `Ton expérience sur ${top.name}`, describe(top), top.familiarity ? "Le nombre de parties ne dit pas si tu joues bien ce champion." : "Tes parties plus anciennes ou dans d'autres modes ne sont pas visibles ici."));
  if (familiar && !same) out.push(masteryItem("experience.familiar", "experience", "measured", `Le plus joué : ${familiar.row.name}`, describe(familiar.row), "Le plus joué n'est pas forcément celui qui te réussit le mieux."));
  if (top.learning) out.push(masteryItem("experience.learning", "experience", "general", `Tu apprends ${top.name}`, "Tu l'as marqué en apprentissage : quelques parties plus difficiles sont normales.", null));
  const e = top.familiarity;
  if (e && e.resultGap !== null) out.push(masteryItem("experience.results", "experience", "measured", e.clear ? `Résultats de ${top.name} : écart à vérifier` : `Résultats de ${top.name} : pas d'écart net`, byLevel(level, [
    `${e.wins} victoires sur ${e.count}, contre ${masteryPct(e.reference)} sur tes autres champions de ce rôle. ${e.clear ? "L'écart semble réel, mais d'autres raisons peuvent l'expliquer." : "L'écart peut venir du hasard."}`,
    `${masteryPct(e.wins / e.count)} sur ${e.count} parties (intervalle ${masteryPct(e.interval[0])} à ${masteryPct(e.interval[1])}), contre ${masteryPct(e.reference)} sur tes autres champions.`,
    `Écart ramené vers ta moyenne : ${e.resultGap >= 0 ? "+" : ""}${masteryDecimal(e.resultGap * 100)} points de pourcentage. Sans contrôle de la date, du matchup ou du niveau.`]), "Corrélation sur tes parties : pas une preuve de cause à effet."));
  if (e && e.trend && (e.trend.status === "up" || e.trend.status === "down") && level >= 2) out.push(masteryItem("experience.trend", "experience", "measured", `${top.name} : résultats ${e.trend.status === "up" ? "en hausse" : "en baisse"}`, byLevel(level, ["", `${masteryPct(e.trend.older)} sur tes parties les plus anciennes, ${masteryPct(e.trend.newer)} sur les plus récentes.`, `${masteryPct(e.trend.older)} puis ${masteryPct(e.trend.newer)} (z = ${masteryDecimal(e.trend.z)}).`]), "Le niveau des parties et le patch peuvent aussi l'expliquer."));
  return out;
}

function uncertaintySection(x) {
  const { top, familiar, level, profile, result, opponent, enemiesCount } = x, out = [];
  if (!profile || !profile.available) out.push(masteryItem("uncertainty.no-history", "uncertainty", "general", "Aucun historique importé", byLevel(level, [
    "DRAFT ne sait pas ce que tu connais déjà : ce conseil ne porte que sur la draft.", "Sans import, aucune expérience n'est connue.", "L'absence d'historique n'est pas lue comme un manque d'expérience."]), null));
  if (enemiesCount > 0 && enemiesCount < 5) out.push(masteryItem("uncertainty.incomplete", "uncertainty", "general", `Il reste ${5 - enemiesCount} adversaire${5 - enemiesCount > 1 ? "s" : ""} à voir`, "Ce conseil peut encore changer.", null));
  if (opponent && !result.manual && result.belief.confidence !== "élevée") out.push(masteryItem("uncertainty.opponent", "uncertainty", "estimate", `Adversaire supposé : ${opponent}`, "DRAFT le déduit des rôles habituels. Si tu sais que c'est un autre champion, corrige-le.", "Ce n'est pas une information du client."));
  if (enemiesCount > 0 && !opponent) out.push(masteryItem("uncertainty.no-opponent", "uncertainty", "estimate", "Adversaire direct inconnu", "Le conseil ne peut pas encore porter sur un duel précis.", null));
  if (opponent && !liveMatchup(top.pick, opponent) && !matchupOf(opponent) && !providedMatchup(top.name, opponent, x.role)) out.push(masteryItem("uncertainty.no-matchup", "uncertainty", "general", `${top.name} contre ${opponent} : aucune donnée`, "Ni statistique, ni tableau pour ce couple : le matchup n'entre pas dans ce conseil. Rien n'est estimé à sa place.", null));
  const e = top.familiarity;
  if (e && e.count < RESULT_MIN_GAMES) out.push(masteryItem("uncertainty.small-sample", "uncertainty", "smallSample", `${pluralGames(e.count)} sur ${top.name} : trop peu pour conclure`, byLevel(level, [
    `${e.wins} victoire${e.wins > 1 ? "s" : ""} sur ${e.count} : trop peu pour savoir si ce champion te réussit.`, `Intervalle à 95 % : ${masteryPct(e.interval[0])} à ${masteryPct(e.interval[1])}.`, `Intervalle à 95 % : ${masteryPct(e.interval[0])} à ${masteryPct(e.interval[1])}. Aucun résultat n'est interprété sous ${RESULT_MIN_GAMES} parties.`]), null));
  [top, familiar && familiar.row].filter((r, i, l) => r && l.indexOf(r) === i).forEach(r => {
    const f = r.familiarity;
    if (f && f.rusty) out.push(masteryItem(`uncertainty.rusty.${r.name}`, "uncertainty", "measured", `Pas de partie récente sur ${r.name}`, `Ta dernière partie importée dans ce rôle date de ${f.daysSince} jours. Le jeu a pu changer entre-temps.`, null));
    if (f && f.changed && f.changed.gamesSince < FAMILIARITY_STEPS.learning) out.push(masteryItem(`uncertainty.changed.${r.name}`, "uncertainty", "estimate", `${r.name} a changé`, `${f.changed.label} : ${pluralGames(f.changed.gamesSince)} seulement depuis. Ton expérience passée n'est peut-être plus aussi utile.`, "La liste des changements est fournie à la main."));
  });
  if (opponent && profile && profile.available) {
    const note = championComplexity(opponent).official.value, seen = e && e.versus[opponent] || 0;
    if (note !== null && note >= DIFFICULTY_DEMANDING && seen < DUEL_SEEN_GAMES) out.push(masteryItem("uncertainty.opponent-unknown", "uncertainty", "estimate", `${opponent} peut surprendre`, byLevel(level, [
      `Ce matchup peut être difficile si tu ne connais pas ses compétences. Regarde ses sorts avant de lancer la partie.`, `${seen ? `${seen} rencontre${seen > 1 ? "s" : ""}` : "Aucune rencontre"} avec ${top.name} dans ton historique.`, `${seen} rencontre${seen > 1 ? "s" : ""} sur ce couple : aucune tendance personnelle n'est tirée.`]), "Tu as pu jouer ou rencontrer ce champion ailleurs."));
  }
  return out;
}

function compromiseOf(x) {
  const { best:top, familiar, level, names, profile, result, chosen } = x;
  if (!top) return { kind:"none", text:"Aucun champion disponible dans ton pool." };
  if (chosen && !names.includes(chosen.name)) return { kind:"chosen", text:`Tu regardes ${chosen.name}. La draft met plutôt en avant ${nameList(names)} : ton choix reste possible, voici ce qu'il implique.` };
  if (!profile || !profile.available) return { kind:"no-history", text:"Sans historique, seule la lecture de la draft est disponible." };
  if (!familiar) return { kind:"no-familiar", text:`Aucun champion de ce pool n'a au moins ${FAMILIAR_MIN_GAMES} parties dans ce rôle : seule la lecture de la draft est disponible.` };
  const f = familiar.row.name, severalFamiliar = familiar.close.length ? ` ${nameList(familiar.close)} ${familiar.close.length > 1 ? "sont" : "est"} presque au même niveau d'expérience.` : "";
  if (familiar.row === top) return { kind:"same", text:`${f} ressort sur la draft, et c'est aussi ton champion le plus joué.${severalFamiliar}` };
  if (names.includes(f) && names.length > 1) {
    const near = familiar.close.filter(n => names.includes(n));
    return { kind:"tie", tieBreak:result.tieBreak, text:near.length
      ? `Égalité sur la draft entre ${nameList(names)}, et ton expérience sur ${nameList([f].concat(near))} est proche. Rien ne les sépare : à toi de voir.`
      : `Égalité sur la draft entre ${nameList(names)}. ${f} est ton champion le plus joué : c'est un argument, à toi de voir.` };
  }
  const gap = top.score - familiar.row.score, big = gap >= HOLD_GAP, head = nameList(names);
  return { kind:big ? "gap-large" : "gap-small", gap:+gap.toFixed(2), text:byLevel(level, [
    big ? `La draft penche nettement vers ${head}. ${f} est simplement le champion que tu as le plus joué : à toi de peser.` : `La draft penche légèrement vers ${head}. ${f} est le champion que tu as le plus joué : les deux lectures se défendent.`,
    `${big ? "Écart net" : "Écart faible"} sur la draft en faveur de ${head}. ${f} a plus de parties, ce qui ne prédit pas son efficacité ici.`,
    `Écart moteur de ${masteryDecimal(gap)} points en faveur de ${head} (seuil de ${HOLD_GAP} pour un choix net). Aucune conversion entre parties jouées et points : c'est à toi de trancher.`]) + severalFamiliar };
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
  const x = { input, level, top, best, names, chosen, familiar, profile, result, role:result.role, opponent:result.opponent, enemiesCount:(input.enemies || []).length };
  if (!top) return { version:1, level:input.level || "beginner", strategic:null, familiar:null, compromise:compromiseOf(x), theory:[], experience:[], uncertainty:[], ranking:[], scoresChanged:false, offline:true };
  return {
    version:1, level:input.level || "beginner", role:result.role, opponent:result.opponent, confidence:result.manual ? "manuel" : result.belief.confidence,
    // Le meilleur choix de la draft, et lui seul : le score ne dépend ni du joueur, ni de la difficulté.
    strategic:{ champions:names, score:+best.score.toFixed(2), tied:names.length > 1, label:names.length > 1 ? "Égalité sur la draft" : "Meilleur choix pour la draft", source:MASTERY_BASIS.heuristic },
    // Le plus joué : ce n'est pas une évaluation de performance.
    familiar:familiar ? { champion:familiar.row.name, games:familiar.row.familiarity.count, recentGames:familiar.row.familiarity.recentGames, daysSince:familiar.row.familiarity.daysSince, rusty:familiar.row.familiarity.rusty, usefulExperience:+familiar.row.experience.toFixed(1), strategicRank:familiar.row.strategicRank + 1, close:familiar.close, label:"Le plus joué", source:MASTERY_BASIS.measured, isPerformance:false } : null,
    explained:top.name, compromise:compromiseOf(x), tieBreak:result.tieBreak,
    theory:theorySection(x), experience:experienceSection(x), uncertainty:uncertaintySection(x),
    experienceTable:rows.map(r => ({ champion:r.name, games:r.familiarity ? r.familiarity.count : 0, recentGames:r.familiarity ? r.familiarity.recentGames : 0, daysSince:r.familiarity ? r.familiarity.daysSince : null, usefulExperience:+r.experience.toFixed(1) })),
    ranking:rows.map(r => ({ champion:r.name, score:+r.score.toFixed(2), tiedWith:r.tiedWith })), offline:true
  };
}
