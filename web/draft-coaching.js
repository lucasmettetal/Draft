// Moteur de conseils : explique une draft sans changer le classement. Déterministe, hors ligne, sans réseau.
// Lit le moteur stratégique (analyze, compoOf, opponentBelief...) et n'écrit jamais dedans.
// Conseils pour préparer et comprendre une draft : aucune consigne pendant une partie, aucune action dans le client.
// Interface et types : docs/COACHING.md.

const COACH_REVIEWED = "2026-11";
const COACH_LEVELS = { beginner:1, intermediate:2, advanced:3 };
// Nature de ce qui fonde un conseil. Une règle n'est jamais présentée comme une statistique.
const COACH_BASIS = {
  official:I18N.t("coach.basis.official"),
  measured:I18N.t("coach.basis.measured"),
  smallSample:I18N.t("coach.basis.smallSample"),
  estimate:I18N.t("coach.basis.estimate"),
  heuristic:I18N.t("coach.basis.heuristic"),
  general:I18N.t("coach.basis.general")
};

// Peu de dégâts par eux-mêmes : ils ne comptent pas dans la répartition.
const LOW_DAMAGE = new Set("Alistar,Braum,Janna,Karma,Leona,Lulu,Milio,Nami,Nautilus,Rakan,Rell,Renata Glasc,Shen,Sona,Soraka,Taric,Thresh,Yuumi,Zilean".split(","));
// Les notes de Riot se trompent ou manquent pour ceux-ci : correction écrite à la main.
const DAMAGE_OVERRIDES = {
  "Akshan":"physical", "Corki":"magic", "Diana":"magic", "Elise":"magic", "Ezreal":"physical", "Fizz":"magic", "Gnar":"physical", "Gwen":"magic",
  "Hwei":"magic", "Jax":"mixed", "K'Sante":"physical", "Kayle":"mixed", "Kennen":"magic", "Kog'Maw":"mixed", "Nasus":"physical", "Rammus":"magic",
  "Senna":"physical", "Seraphine":"magic", "Shaco":"mixed", "Skarner":"mixed", "Smolder":"mixed", "Twisted Fate":"magic", "Vex":"magic", "Dr. Mundo":"magic"
};

const COACH_TERMS = {
  frontline:I18N.t("coach.term.frontline"),
  carry:I18N.t("coach.term.carry"),
  engage:I18N.t("coach.term.engage"),
  poke:I18N.t("coach.term.poke"),
  contrôle:I18N.t("coach.term.control"),
  assassin:I18N.t("coach.term.assassin"),
  roam:I18N.t("coach.term.roam"),
  vague:I18N.t("coach.term.wave"),
  farm:I18N.t("coach.term.farm"),
  échange:I18N.t("coach.term.trade"),
  portée:I18N.t("coach.term.range"),
  objectif:I18N.t("coach.term.objective")
};

function coachOfficial(name) { return typeof CHAMPION_OFFICIAL !== "undefined" ? CHAMPION_OFFICIAL.champions[name] || null : null; }
const coachKnown = name => ROSTER[name] !== undefined && ROSTER[name] !== "?";
function coachRange(name) { const o = coachOfficial(name); return o ? o.range : null; }
function coachDamage(name) {
  if (LOW_DAMAGE.has(name)) return "low";
  if (DAMAGE_OVERRIDES[name]) return DAMAGE_OVERRIDES[name];
  const o = coachOfficial(name);
  if (!o || !(o.attack || o.magic)) return null;
  return o.magic - o.attack >= 2 ? "magic" : o.attack - o.magic >= 2 ? "physical" : "mixed";
}
// Le moteur compte comme frontline tout tank ou bruiser, y compris Yasuo ou Irelia. Pour parler de joueurs « solides »,
// il faut un tank, ou une note de défense élevée donnée par Riot.
function coachIsSolid(name) {
  const o = coachOfficial(name);
  return /t/.test(ROSTER[name] || "") || (!/a/.test(ROSTER[name] || "") && !!o && o.defense >= 6);
}
// Une décimale, virgule en français et point en anglais (I18N.fmtDecimal).
const coachNum = v => I18N.fmtDecimal(v, 1);
// Phrases complètes du catalogue, mises bout à bout avec une espace (même rendu qu'avant l'extraction).
const coachSentences = (...list) => list.filter(Boolean).join(" ");
// Classes Riot traduites pour l'affichage. La donnée (o.tags) reste celle de Riot.
const COACH_RIOT_CLASSES = { Fighter:I18N.t("coach.riotClass.fighter"), Marksman:I18N.t("coach.riotClass.marksman"), Mage:I18N.t("coach.riotClass.mage"), Assassin:I18N.t("coach.riotClass.assassin"), Tank:I18N.t("coach.riotClass.tank"), Support:I18N.t("coach.riotClass.support") };
const coachRiotClass = tag => COACH_RIOT_CLASSES[tag] || tag;
const coachList = names => I18N.listAnd(names);
const coachRangeWord = range => range === null ? null : I18N.t(range >= 500 ? "coach.range.long" : range >= 300 ? "coach.range.medium" : "coach.range.melee");

// Trois noms au plus, sans cacher qu'il y en a d'autres.
const coachNames = names => names.length <= 3 ? coachList(names) : I18N.tp("coach.names.more", names.length - 3, { names: names.slice(0, 3).join(I18N.t("list.comma")) });
// Raison écrite par le moteur, à placer au milieu d'une phrase : sans majuscule isolée, et sans laisser croire qu'une liste coupée est complète.
function coachReason(text, fallback) {
  if (!text) return fallback;
  // Liste coupée à trois noms par le moteur (listedText, draft-engine.js) : le nombre de noms cachés vient du moteur, pas du texte.
  const hidden = hiddenNames(text);
  const out = hidden ? I18N.tp("coach.reason.more", hidden, { text }) : text;
  // Un nom de champion ou un sigle (CC, ADC, AP) garde sa majuscule.
  return NAMES.some(n => out.startsWith(n)) || /^[A-Z]{2,}\b/.test(out) ? out : I18N.lowerFirst(out);
}
// Pourquoi un pick est affiché en premier à score égal (tieCause, draft-engine.js). shown : le nom affiché en premier.
function tieCauseText(cause, shown) {
  if (!cause) return "";
  const first = label(cause.first), second = label(cause.second);
  if (shown && shown !== cause.first) return I18N.t("coach.tie.held", { champion: label(shown) });
  if (cause.kind === "risk") return cause.firstRisk
    ? I18N.t("coach.tie.riskBoth", { first, firstRisk: cause.firstRisk, secondRisk: cause.secondRisk, second })
    : I18N.t("coach.tie.riskNone", { first, second, secondRisk: cause.secondRisk });
  if (cause.kind === "favorite") return I18N.t("coach.tie.favorite", { first });
  if (cause.kind === "mixed") return I18N.t("coach.tie.mixed");
  return I18N.t("coach.tie.alpha");
}
const coachRangeWords = names => { const w = names.map(n => coachRangeWord(coachRange(n)) || I18N.t("coach.range.unknown")); return names.length === 2 && w[0] === w[1] ? I18N.t("coach.range.both", { range: w[0] }) : names.map((n, i) => I18N.t("coach.range.each", { champion: n, range: w[i] })).join(I18N.t("list.comma")); };
// Champions qui changent de forme : une seule portée de base ne les décrit pas.
const COACH_RANGE_CHANGES = ["Jayce", "Nidalee", "Elise", "Gnar", "Kayle"];
// La portée d'attaque n'aide à préparer la voie que dans un vrai duel, entre champions dont elle décrit le jeu.
function coachRangeDuel(c) {
  if (!c.opponent || !c.pick || !["top", "mid", "adc"].includes(c.role)) return null;
  if (COACH_RANGE_CHANGES.includes(c.pick) || COACH_RANGE_CHANGES.includes(c.opponent)) return null;
  const mine = coachRange(c.pick), theirs = coachRange(c.opponent);
  if (mine === null || theirs === null || Math.abs(mine - theirs) < 200) return null;
  const longer = theirs > mine;
  // Avoir plus de portée qu'un champion fait pour arriver sur toi n'est pas un avantage à conseiller.
  if (!longer && /[am]/.test(ROSTER[c.opponent] || "")) return null;
  return { mine, theirs, longer };
}

// Même échelle que l'interface pour ton vis-à-vis nommé : presque sûr, probable, ou hypothèse avec 1 ou 2 champions vus.
// Seul le mot change : le seuil pour nommer l'adversaire reste celui du moteur. Codes guess, likely, sure ; mot par COACH_OPPONENT_WORD.
const COACH_OPPONENT_WORD = { guess:I18N.t("coach.opponentWord.guess"), likely:I18N.t("coach.opponentWord.likely"), sure:I18N.t("coach.opponentWord.sure") };
const coachOpponentLevel = c => c.manual || !c.opponent ? null : c.n <= 2 ? "guess" : c.belief.confidence === "high" ? "sure" : "likely";

// Prépare tout ce que les règles lisent. Un champion inconnu du moteur reste à part, sans chiffre inventé.
function coachingContext(input) {
  const raw = input.enemies || [];
  const enemies = raw.filter(coachKnown);
  const fixed = input.fixedRoles || {};
  const manual = input.manualOpponent && enemies.includes(input.manualOpponent) ? input.manualOpponent : null;
  const belief = opponentBelief(enemies, fixed);
  const opponent = manual || identifiedOpponent(enemies, fixed);
  const scenarios = manual ? null : beliefScenarios(belief);
  const hasFront = input.hasFront !== false;
  const result = analyze(enemies, opponent, hasFront, enemies.length ? scenarios : null);
  const ranking = result.ranking;
  const entry = input.pick ? ranking.find(p => p.pick === pickId(input.pick)) || ranking[0] : ranking[0];
  const rival = entry ? ranking.find(p => p !== entry) || null : null;
  const pick = entry ? label(entry.pick) : null;
  return {
    enemies, unknown:raw.filter(n => !coachKnown(n)), n:enemies.length, compo:result.counts, belief, opponent, manual, scenarios, hasFront,
    ranking, entry, rival, pick, role:myRole(), opp:oppName(),
    crit: key => entry ? entry.criteria.find(c => c.key === key) || null : null,
    personal: input.personal || null
  };
}

// Conseils dont le contenu se déduit d'une donnée ou d'un calcul, et qu'un test peut vérifier.
// Tous les autres portent un avis sur le jeu : un joueur doit les relire avant de les considérer comme fiables.
const COACH_CHECKED = new Set(["draft.tie", "draft.why", "draft.close", "draft.conflict", "draft.incomplete", "matchup.measured", "matchup.written", "matchup.unknown", "limits.opponent-assumed", "limits.unknown-champion", "limits.rules-only", "personal.experience", "personal.results", "personal.too-few", "champ.difficulty"]);

// ---------- Règles ----------
// when(ctx) dit si le conseil s'applique, en renvoyant ce dont build a besoin. text : [débutant, intermédiaire, avancé].
const COACH_RULES = [
  // ----- Décision -----
  { id:"draft.tie", category:"decision", priority:96, basis:"heuristic",
    when: c => {
      if (!c.n || !c.entry || !c.entry.tiedWith || !c.entry.tiedWith.length) return null;
      const others = c.entry.tiedWith.map(label);
      const sameSheet = others.every(n => CHAMPION_TRAITS[n] !== undefined && CHAMPION_TRAITS[n] === CHAMPION_TRAITS[c.pick]);
      return { others, sameSheet, cause:tieCause(c.ranking, [c.entry.pick].concat(c.entry.tiedWith)) };
    },
    build: (c, d) => { const names = coachList([c.pick].concat(d.others)); return { title:I18N.t("coach.draft.tie.title", { champions: names }),
      text:[I18N.t("coach.draft.tie.l1"),
        I18N.t(d.sameSheet ? "coach.draft.tie.l2same" : "coach.draft.tie.l2diff"),
        I18N.t(d.sameSheet ? "coach.draft.tie.l3same" : "coach.draft.tie.l3", { score: coachNum(c.entry.score), cause: tieCauseText(d.cause, c.entry.pick) })],
      limits:I18N.t("coach.draft.tie.limits"), evidence:[{ kind:"tie", picks:[c.pick].concat(d.others), score:+c.entry.score.toFixed(2), cause:d.cause ? d.cause.kind : null }] }; } },
  { id:"draft.why", category:"decision", priority:82, basis:"heuristic",
    when: c => {
      if (!c.n || !c.entry || (c.entry.tiedWith && c.entry.tiedWith.length) || (c.rival && c.entry.score - c.rival.score < HOLD_GAP)) return null;
      const live = c.opponent && liveMatchup(c.entry.pick, c.opponent);
      const best = c.entry.criteria.filter(x => x.impact > 0 && x.text && !(live && x.key === "matchup")).sort((a, b) => b.impact - a.impact)[0];
      return best ? { best, reason:signedText(best, 1) } : null;
    },
    build: (c, d) => ({ title:I18N.t("coach.draft.why.title", { champion: c.pick }),
      text:[I18N.t("coach.draft.why.l1", { reason: coachReason(d.reason) }),
        I18N.t("coach.draft.why.l2", { criterion: d.best.label, reason: coachReason(d.reason) }),
        I18N.t(c.rival ? "coach.draft.why.l3rival" : "coach.draft.why.l3", { criterion: d.best.label, impact: coachNum(d.best.impact), gap: c.rival ? coachNum(c.entry.score - c.rival.score) : "", rival: c.rival ? label(c.rival.pick) : "" })],
      evidence:[{ kind:"criterion", key:d.best.key, value:+d.best.impact.toFixed(2) }], stance:{ key:d.best.key, sign:1 } }) },
  // Choix serré. Le pick affiché peut être 2e : pendant la draft, le nom ne change que si un autre mène d'au moins HOLD_GAP points.
  // « passe devant » n'est alors dit que du vrai 1er.
  { id:"draft.close", category:"decision", priority:84, basis:"heuristic",
    when: c => {
      if (!c.rival || !c.n || c.entry.score - c.rival.score >= HOLD_GAP || (c.entry.tiedWith && c.entry.tiedWith.length)) return null;
      const held = c.rival.score > c.entry.score;
      const lead = held ? c.rival : c.entry, other = held ? c.entry : c.rival;
      const row = comparePicks(lead, other).decisive.find(r => r.diff > 0);
      if (!row) return null;
      const crit = lead.criteria.find(x => x.key === row.key);
      return { row, held, lead:label(lead.pick), other:label(other.pick), gap:lead.score - other.score, reason:crit && crit.impact > 0 ? signedText(crit, 1) : "" };
    },
    build: (c, d) => {
      const why = coachReason(d.reason, I18N.t("coach.draft.close.whyFallback", { criterion: d.row.label })), stays = I18N.t("coach.draft.close.stays", { champion: c.pick, threshold: HOLD_GAP });
      return { title:d.held ? I18N.t("coach.draft.close.titleHeld", { champion: d.lead }) : I18N.t("coach.draft.close.title", { champion: c.pick, other: d.other }),
        text:d.held ? [I18N.t("coach.draft.close.held.l1", { champion: d.lead, reason: why, stays }),
          I18N.t(d.reason ? "coach.draft.close.held.l2reason" : "coach.draft.close.held.l2", { champion: d.lead, criterion: d.row.label, reason: d.reason ? coachReason(d.reason) : "", stays }),
          I18N.t("coach.draft.close.held.l3", { champion: d.lead, gap: coachNum(d.gap), threshold: HOLD_GAP, criterion: d.row.label })]
        : [I18N.t("coach.draft.close.l1", { champion: c.pick, reason: why }),
          I18N.t(d.reason ? "coach.draft.close.l2reason" : "coach.draft.close.l2", { champion: c.pick, criterion: d.row.label, reason: d.reason ? coachReason(d.reason) : "" }),
          I18N.t("coach.draft.close.l3", { gap: coachNum(d.gap), threshold: HOLD_GAP, criterion: d.row.label })],
        evidence:[{ kind:"criterion", key:d.row.key, value:+d.row.diff.toFixed(2), held:d.held }], stance:{ key:d.row.key, sign:d.held ? -1 : 1, relative:true } };
    } },
  { id:"draft.conflict", category:"decision", priority:90, basis:"heuristic",
    when: c => {
      if (!c.entry) return null;
      const worst = c.entry.criteria.filter(x => x.impact <= -2).sort((a, b) => a.impact - b.impact)[0];
      if (!worst || c.entry.score <= 0) return null;
      // Rang réel du pick : devant, à égalité, ou affiché alors qu'un autre mène de peu.
      const ahead = c.rival ? tieKey(c.entry.score) - tieKey(c.rival.score) : 1;
      return { worst, reason:signedText(worst, -1) || worst.text, rank:ahead > 0 ? "first" : ahead === 0 ? "tied" : "held" };
    },
    build: (c, d) => {
      const risk = coachReason(d.reason), other = c.rival ? label(c.rival.pick) : "";
      const where = d.rank === "first" ? I18N.t("coach.draft.conflict.first", { champion: c.pick })
        : d.rank === "tied" ? I18N.t("coach.draft.conflict.tied", { champion: c.pick, other })
        : I18N.t("coach.draft.conflict.held", { champion: c.pick, other });
      return { title:d.rank === "held" ? I18N.t("coach.draft.conflict.titleHeld", { champion: c.pick }) : I18N.t("coach.draft.conflict.title", { champion: c.pick }),
        text:[I18N.t("coach.draft.conflict.l1", { risk, where }),
          d.rank === "first" ? I18N.t("coach.draft.conflict.l2first", { criterion: d.worst.label, risk })
            : I18N.t("coach.draft.conflict.l2", { criterion: d.worst.label, risk, where }),
          I18N.t("coach.draft.conflict.l3", { criterion: d.worst.label, impact: coachNum(d.worst.impact) })],
        evidence:[{ kind:"criterion", key:d.worst.key, value:+d.worst.impact.toFixed(2) }], stance:{ key:d.worst.key, sign:-1 } };
    } },
  { id:"draft.incomplete", category:"decision", priority:40, basis:"general",
    when: c => c.n > 0 && c.n < 5 ? {} : null,
    build: c => ({ title:I18N.tp("coach.draft.incomplete.title", 5 - c.n),
      text:[I18N.t("coach.draft.incomplete.l1"),
        I18N.tp("coach.draft.incomplete.l2", c.n),
        I18N.tp("coach.draft.incomplete.l3", 5 - c.n)] }) },

  // ----- Matchups -----
  { id:"matchup.measured", category:"matchup", priority:88, basis:"measured",
    when: c => { if (!c.opponent || !c.entry) return null; const live = liveMatchup(c.entry.pick, c.opponent); return live ? { live } : null; },
    build: (c, d) => {
      const small = d.live.effective < 100, good = d.live.gap >= 0;
      return { basis:small ? "smallSample" : "measured", title:I18N.t("coach.matchup.measured.title", { champion: c.pick, opponent: c.opponent }),
        text:[I18N.t(good ? "coach.matchup.measured.l1good" : "coach.matchup.measured.l1bad", { games: d.live.games, champion: c.pick }),
          I18N.t("coach.matchup.measured.l2", { sign: d.live.gap >= 0 ? "+" : "", gap: Math.round(d.live.gap * 100), champion: c.pick, games: d.live.games }),
          I18N.t(d.live.recent ? "coach.matchup.measured.l3" : "coach.matchup.measured.l3old", { sign: d.live.gap >= 0 ? "+" : "", gap: Math.round(d.live.gap * 100), games: d.live.games })],
        limits:small ? I18N.t("coach.matchup.measured.limitSmall") : I18N.t("coach.matchup.measured.limit"),
        evidence:[{ kind:"games", value:d.live.games }], stance:{ key:"matchup", sign:Math.sign(d.live.gap) } };
    } },
  { id:"matchup.written", category:"matchup", priority:76, basis:"heuristic",
    when: c => {
      if (!c.opponent || !c.entry || liveMatchup(c.entry.pick, c.opponent) || !matchupOf(c.opponent)) return null;
      const cr = c.crit("matchup");
      return cr && cr.impact !== 0 ? { cr } : null;
    },
    build: (c, d) => { const good = d.cr.impact > 0, level = coachOpponentLevel(c); return { title:I18N.t(level && level !== "sure" ? (good ? "coach.matchup.written.titleGoodLevel" : "coach.matchup.written.titleBadLevel") : (good ? "coach.matchup.written.titleGood" : "coach.matchup.written.titleBad"), { champion: c.pick, opponent: c.opponent, level: level ? COACH_OPPONENT_WORD[level] : "" }),
      text:[good ? I18N.t("coach.matchup.written.l1good", { champion: c.pick, opponent: c.opponent }) : I18N.t("coach.matchup.written.l1bad", { champion: c.pick, opponent: c.opponent }),
        I18N.t("coach.matchup.written.l2", { text: d.cr.text }),
        I18N.t("coach.matchup.written.l3", { text: d.cr.text, impact: coachNum(d.cr.impact), min: MATCHUP_MIN_GAMES })],
      limits:I18N.t("coach.limit.writtenOpinion"), evidence:[{ kind:"criterion", key:"matchup", value:+d.cr.impact.toFixed(2) }], stance:{ key:"matchup", sign:Math.sign(d.cr.impact) } }; } },
  { id:"matchup.range", category:"matchup", priority:78, basis:"official",
    when: coachRangeDuel,
    build: (c, d) => ({ title:d.longer ? I18N.t("coach.matchup.range.titleLonger", { opponent: c.opponent }) : I18N.t("coach.matchup.range.titleShorter", { opponent: c.opponent }),
      terms:["portée", "échange", "farm"],
      text:d.longer ? [
        I18N.t("coach.matchup.range.longer.l1", { opponent: c.opponent, theirs: d.theirs, mine: d.mine }),
        I18N.t("coach.matchup.range.longer.l2", { gap: d.theirs - d.mine, mine: d.mine, theirs: d.theirs, opponent: c.opponent }),
        I18N.t("coach.matchup.range.longer.l3", { mine: d.mine, theirs: d.theirs })
      ] : [
        I18N.t("coach.matchup.range.shorter.l1", { mine: d.mine, opponent: c.opponent, theirs: d.theirs }),
        I18N.t("coach.matchup.range.shorter.l2", { gap: d.mine - d.theirs, opponent: c.opponent }),
        I18N.t("coach.matchup.range.shorter.l3", { mine: d.mine, theirs: d.theirs, opponent: c.opponent })
      ], evidence:[{ kind:"range", mine:d.mine, theirs:d.theirs }], limits:I18N.t("coach.matchup.range.limits") }) },
  { id:"matchup.tank", category:"matchup", priority:66, basis:"heuristic",
    when: c => c.opponent && c.pick && /t/.test(ROSTER[c.opponent] || "") ? {} : null,
    build: c => { const killer = hasTrait(c.entry.pick, "k"); return { title:I18N.t("coach.matchup.tank.title", { opponent: c.opponent }), terms:["frontline", "roam"],
      text:killer ? [I18N.t("coach.matchup.tank.killer.l1", { opponent: c.opponent, champion: c.pick }),
        I18N.t("coach.matchup.tank.killer.l2", { champion: c.pick }),
        I18N.t("coach.matchup.tank.killer.l3", { champion: c.pick })]
      : !["top", "mid", "adc"].includes(c.role) ? [I18N.t("coach.matchup.tank.roamer.l1", { opponent: c.opponent }),
        I18N.t("coach.matchup.tank.roamer.l2"),
        I18N.t("coach.matchup.tank.roamer.l3")]
      : [I18N.t("coach.matchup.tank.lane.l1", { opponent: c.opponent }),
        I18N.t("coach.matchup.tank.lane.l2"),
        I18N.t("coach.matchup.tank.lane.l3")],
      limits:I18N.t("coach.limit.generalNotMeasured") }; } },
  { id:"matchup.assassin", category:"matchup", priority:70, basis:"heuristic",
    when: c => c.opponent && c.pick && /a/.test(ROSTER[c.opponent] || "") && !/t/.test(ROSTER[c.opponent] || "") && !(c.compo.divers.length >= 2 && c.compo.divers.includes(c.opponent)) ? {} : null,
    build: c => ({ title:I18N.t("coach.matchup.assassin.title", { opponent: c.opponent }), terms:["assassin"],
      text:[I18N.t("coach.matchup.assassin.l1", { opponent: c.opponent }),
        I18N.t("coach.matchup.assassin.l2"),
        I18N.t("coach.matchup.assassin.l3")],
      limits:I18N.t("coach.limit.generalNotMeasured") }) },
  { id:"matchup.unknown", category:"matchup", priority:72, basis:"estimate",
    when: c => {
      if (c.opponent || !c.pick || !c.n) return null;
      // Candidats cités : ceux du texte d'hypothèses (p ≥ 0,2), sinon ceux que le calcul pèse vraiment (hypothèses gardées).
      let names = c.belief.candidates.filter(x => x.known && x.p >= .2).map(x => x.name).slice(0, 2);
      if (!names.length && c.scenarios) names = c.scenarios.filter(s => s.mid).map(s => s.mid).slice(0, 2);
      const weighted = !!(c.scenarios && c.scenarios.length > 1 && c.entry && c.entry.byScenario);
      return { names, hidden:c.belief.unrevealed >= .2, weighted, matchup:!!c.crit("matchup"), blind:!!c.crit("blind") };
    },
    build: (c, d) => ({ title:d.names.length === 2 ? I18N.t("coach.matchup.unknown.titleTwo", { role: c.opp, champions: I18N.t("coach.or", { a: d.names[0], b: d.names[1] }) }) : d.names.length ? I18N.t("coach.matchup.unknown.titleOne", { role: c.opp, champion: d.names[0] }) : I18N.t(d.hidden ? "coach.matchup.unknown.titleNotYet" : "coach.matchup.unknown.titleNone", { role: c.opp }), terms:["portée"],
      text: d.names.length ? [
        I18N.t("coach.matchup.unknown.l1", { champions: d.names.length === 2 ? I18N.t("coach.or", { a: d.names[0], b: d.names[1] }) : d.names.join(""), ranges: coachRangeWords(d.names) }),
        I18N.t("coach.matchup.unknown.l2", { champions: d.names.join(I18N.t("list.comma")), why: I18N.t(d.weighted && d.matchup ? "coach.matchup.unknown.weighted" : d.blind ? "coach.matchup.unknown.blind" : "coach.matchup.unknown.comp"), ranges: coachRangeWords(d.names) }),
        I18N.t(!d.weighted ? "coach.matchup.unknown.l3notWeighted" : d.hidden ? (d.matchup ? "coach.matchup.unknown.l3weightedHidden" : "coach.matchup.unknown.l3weightedHiddenNoMatchup") : (d.matchup ? "coach.matchup.unknown.l3weighted" : "coach.matchup.unknown.l3weightedNoMatchup"), { champions: d.names.join(I18N.t("list.comma")) })
      ] : [
        I18N.t("coach.matchup.unknown.none.l1"),
        I18N.t(d.blind ? "coach.matchup.unknown.blind" : "coach.matchup.unknown.comp"),
        I18N.t(d.blind ? "coach.matchup.unknown.none.l3blind" : "coach.matchup.unknown.comp")
      ], limits:I18N.t("coach.limit.roleDeduction") }) },

  // ----- Composition -----
  { id:"comp.assassins", category:"composition", priority:80, basis:"heuristic",
    when: c => c.compo.divers.length >= 2 && c.pick ? {} : null,
    build: c => {
      const names = coachNames(c.compo.divers), cr = c.crit("dive"), risky = cr && cr.impact < 0, solid = cr && cr.impact > 0;
      return { title:risky ? I18N.t("coach.comp.assassins.titleRisky", { champions: names, champion: c.pick }) : I18N.t("coach.comp.assassins.title", { count: c.compo.divers.length }), terms:["assassin"],
        text:risky ? [I18N.t("coach.comp.assassins.risky.l1", { champions: names, champion: c.pick }),
          I18N.t("coach.comp.assassins.risky.l2", { champion: c.pick, champions: names }),
          I18N.t("coach.comp.assassins.risky.l3", { criterion: cr.label })]
        : solid ? [I18N.t("coach.comp.assassins.solid.l1", { champions: names, champion: c.pick }),
          I18N.t("coach.comp.assassins.solid.l2", { champions: names }),
          I18N.t("coach.comp.assassins.solid.l3", { criterion: cr.label, count: c.compo.divers.length })]
        : [I18N.t("coach.comp.assassins.other.l1", { champions: names }),
          I18N.t("coach.comp.assassins.other.l2"),
          I18N.t("coach.comp.assassins.other.l3", { count: c.compo.divers.length })],
        stance:cr ? { key:"dive", sign:Math.sign(cr.impact) } : null, evidence:[{ kind:"champions", value:c.compo.divers }] };
    } },
  { id:"comp.frontline", category:"composition", priority:62, basis:"heuristic",
    when: c => { const solid = c.compo.front.filter(coachIsSolid); return solid.length >= 2 && c.pick ? { solid } : null; },
    build: (c, d) => { const names = coachNames(d.solid), cr = c.crit("front"), good = cr && cr.impact > 0, bad = cr && cr.impact < 0; return { title:I18N.t("coach.comp.frontline.title", { champions: names }), terms:["frontline", "carry"],
      text:good ? [I18N.t("coach.comp.frontline.good.l1", { champions: names, champion: c.pick }),
        I18N.t("coach.comp.frontline.good.l2", { count: d.solid.length, champion: c.pick }),
        I18N.t("coach.comp.frontline.good.l3", { criterion: cr.label, impact: coachNum(cr.impact), count: c.compo.front.length })]
      : bad ? [I18N.t("coach.comp.frontline.bad.l1", { champions: names }),
        I18N.t("coach.comp.frontline.bad.l2", { champion: c.pick }),
        I18N.t("coach.comp.frontline.bad.l3", { criterion: cr.label, text: cr.text })]
      : [I18N.t("coach.comp.frontline.other.l1", { champions: names }),
        I18N.t("coach.comp.frontline.other.l2", { count: d.solid.length }),
        I18N.t("coach.comp.frontline.other.l3")],
      stance:cr ? { key:"front", sign:Math.sign(cr.impact) } : null, evidence:[{ kind:"champions", value:d.solid }] }; } },
  { id:"comp.no-frontline", category:"composition", priority:64, basis:"heuristic",
    when: c => c.n >= 4 && !c.compo.front.length ? {} : null,
    build: c => ({ title:I18N.t("coach.comp.noFrontline.title"), terms:["frontline", "carry"],
      text:[I18N.t("coach.comp.noFrontline.l1", { count: c.n }),
        I18N.t("coach.comp.noFrontline.l2"),
        I18N.t("coach.comp.noFrontline.l3", { count: c.n })],
      evidence:[{ kind:"count", key:"frontline", value:0 }] }) },
  { id:"comp.control", category:"composition", priority:60, basis:"heuristic",
    when: c => c.compo.cc.length >= 2 && c.pick ? {} : null,
    build: c => { const names = coachNames(c.compo.cc), cr = c.crit("control"), bad = cr && cr.impact < 0, good = cr && cr.impact > 0;
      // Malus venu des engages (champion peu mobile), pas des CC ciblés : même règle, autre branche (draft-engine.js, critère control).
      const engage = bad && !(hasTrait(c.entry.pick, "h") && hasTrait(c.entry.pick, "d") && c.compo.cc.length >= 2);
      return { title:I18N.t("coach.comp.control.title", { champions: names }), terms:["contrôle"],
      text:engage ? [I18N.t("coach.comp.control.engage.l1", { champions: coachNames(c.compo.engage), champion: c.pick }),
        I18N.t("coach.comp.control.engage.l2", { text: cr.text }),
        I18N.t("coach.comp.control.engage.l3", { criterion: cr.label, count: c.compo.engage.length })]
      : bad ? [I18N.t("coach.comp.control.bad.l1"),
        I18N.t("coach.comp.control.bad.l2", { champion: c.pick, text: cr.text }),
        I18N.t("coach.comp.control.bad.l3", { criterion: cr.label, count: c.compo.cc.length })]
      : good ? [I18N.t("coach.comp.control.good.l1", { champion: c.pick }),
        I18N.t("coach.comp.control.good.l2", { text: cr.text }),
        I18N.t("coach.comp.control.good.l3", { criterion: cr.label })]
      : [I18N.t("coach.comp.control.other.l1"),
        I18N.t("coach.comp.control.other.l2"),
        I18N.t("coach.comp.control.other.l3", { count: c.compo.cc.length })],
      stance:cr ? { key:"control", sign:Math.sign(cr.impact) } : null, evidence:[{ kind:"champions", value:c.compo.cc }] }; } },
  { id:"comp.engage", category:"composition", priority:56, basis:"heuristic",
    when: c => c.compo.engage.length >= 3 ? {} : null,
    build: c => { const names = coachNames(c.compo.engage); return { title:I18N.t("coach.comp.engage.title", { champions: names }), terms:["engage", "objectif"],
      text:[I18N.t("coach.comp.engage.l1"),
        I18N.t("coach.comp.engage.l2"),
        I18N.t("coach.comp.engage.l3", { count: c.compo.engage.length })],
      evidence:[{ kind:"champions", value:c.compo.engage }] }; } },
  { id:"comp.poke", category:"composition", priority:54, basis:"heuristic",
    when: c => c.compo.poke.length >= 2 ? {} : null,
    build: c => { const names = coachNames(c.compo.poke), cr = c.crit("range"); return { title:I18N.t("coach.comp.poke.title", { champions: names }), terms:["poke", "portée"],
      text:[I18N.t("coach.comp.poke.l1"),
        I18N.t("coach.comp.poke.l2"),
        cr ? I18N.t("coach.comp.poke.l3", { champion: c.pick, criterion: cr.label, text: cr.text }) : I18N.t("coach.comp.poke.l3none")],
      stance:cr ? { key:"range", sign:Math.sign(cr.impact) } : null, evidence:[{ kind:"champions", value:c.compo.poke }] }; } },
  { id:"comp.damage", category:"composition", priority:50, basis:"estimate",
    when: c => {
      if (c.n < 4) return null;
      const kinds = c.enemies.map(coachDamage).filter(k => k && k !== "low");
      if (kinds.length < 3) return null;
      const phys = kinds.filter(k => k === "physical").length, mag = kinds.filter(k => k === "magic").length;
      if (phys === kinds.length) return { type:"physiques", other:"magiques", kinds };
      if (mag === kinds.length) return { type:"magiques", other:"physiques", kinds };
      return null;
    },
    build: (c, d) => ({ title:I18N.t(d.type === "magiques" ? "coach.comp.damage.titleAP" : "coach.comp.damage.titleAD"), terms:[],
      text:[I18N.t(d.type === "magiques" ? "coach.comp.damage.l1magic" : "coach.comp.damage.l1physical"),
        I18N.t("coach.comp.damage.l2", { count: d.kinds.length }),
        I18N.t("coach.comp.damage.l3")],
      limits:I18N.t("coach.comp.damage.limits"), evidence:[{ kind:"damage", value:d.type, champions:d.kinds.length }] }) },

  // ----- Champion -----
  { id:"champ.profile", category:"champion", priority:46, basis:"official",
    when: c => c.pick && coachOfficial(c.pick) && CHAMPION_TRAITS[c.pick] !== undefined ? {} : null,
    build: c => {
      const o = coachOfficial(c.pick), traits = CHAMPION_TRAITS[c.pick], word = coachRangeWord(o.range);
      const assets = [], risks = [];
      if (traits.includes("l")) assets.push(I18N.t("coach.champ.asset.poke"));
      if (traits.includes("k")) assets.push(I18N.t("coach.champ.asset.tanks"));
      if (traits.includes("h")) assets.push(I18N.t("coach.champ.asset.carry"));
      if (traits.includes("t")) assets.push(I18N.t("coach.champ.asset.solid"));
      if (traits.includes("c")) assets.push(I18N.t("coach.champ.asset.protect"));
      if (traits.includes("E")) assets.push(I18N.t("coach.champ.asset.engage"));
      if (traits.includes("g")) assets.push(I18N.t("coach.champ.asset.map"));
      if (traits.includes("i")) risks.push(I18N.t("coach.champ.risk.immobile"));
      if (traits.includes("p")) risks.push(I18N.t("coach.champ.risk.allies"));
      if (traits.includes("d") && !traits.includes("t")) risks.push(I18N.t("coach.champ.risk.expose"));
      if (traits.includes("w")) risks.push(I18N.t("coach.champ.risk.tanks"));
      const main = assets.length ? coachList(assets.slice(0, 2)) : null;
      const risk = risks.length ? I18N.t("coach.champ.attention", { risk: risks[0] }) : "";
      const changes = COACH_RANGE_CHANGES.includes(c.pick), how = changes ? I18N.t("coach.champ.howChanges", { champion: c.pick }) : I18N.t("coach.champ.how", { champion: c.pick, range: word, value: o.range });
      const points = main ? I18N.t("coach.champ.points", { assets: main }) : I18N.t("coach.champ.noPoints"), rangeText = changes ? I18N.t("coach.champ.rangeVariable") : I18N.t("coach.champ.rangeValue", { value: o.range });
      return { title:I18N.t("coach.champ.title", { champion: c.pick }), terms:["portée"],
        text:[coachSentences(how, main ? points : "", risk),
          coachSentences(I18N.t("coach.champ.classes", { classes: o.tags.map(coachRiotClass).join(I18N.t("list.comma")) }), I18N.t("coach.sentence", { text: I18N.upperFirst(rangeText) }), points, risk),
          coachSentences(I18N.t("coach.champ.l3", { classes: o.tags.map(coachRiotClass).join(I18N.t("list.comma")), range: rangeText, difficulty: o.difficulty }), points, risk)],
        limits:I18N.t("coach.champ.limits"), evidence:[{ kind:"official", tags:o.tags, range:o.range, difficulty:o.difficulty }] };
    } },
  { id:"champ.difficulty", category:"champion", priority:44, basis:"official", maxLevel:1,
    when: c => { const o = c.pick ? coachOfficial(c.pick) : null; const known = c.personal && c.personal.byChampion[c.pick]; return o && o.difficulty >= 8 && !(known && known.games >= PERSONAL_MIN_EXPERIENCE) ? { value:o.difficulty } : null; },
    build: (c, d) => ({ title:I18N.t("coach.champ.difficulty.title", { champion: c.pick }),
      text:[I18N.t("coach.champ.difficulty.l1", { note: d.value })],
      limits:I18N.t("coach.champ.difficulty.limits"),
      evidence:[{ kind:"official", difficulty:d.value }] }) },
  { id:"champ.alternative", category:"champion", priority:48, basis:"heuristic", minLevel:2,
    when: c => {
      if (!c.rival || !c.n || c.rival.score > c.entry.score) return null;
      const row = comparePicks(c.rival, c.entry).decisive.find(r => r.diff > 0);
      return row && row.textA ? { row } : null;
    },
    build: (c, d) => ({ title:I18N.t("coach.champ.alternative.title", { champion: label(c.rival.pick) }),
      text:[I18N.t("coach.champ.alternative.l1", { champion: label(c.rival.pick), text: d.row.textA }),
        I18N.t("coach.champ.alternative.l2", { champion: label(c.rival.pick), criterion: d.row.label, text: d.row.textA }),
        I18N.t("coach.champ.alternative.l3", { champion: label(c.rival.pick), gap: coachNum(d.row.diff), criterion: d.row.label, when: when2(c) })],
      evidence:[{ kind:"criterion", key:d.row.key, value:+d.row.diff.toFixed(2) }] }) },

  // ----- Données et limites -----
  { id:"limits.opponent-assumed", category:"limits", priority:58, basis:"estimate",
    when: c => !c.manual && c.opponent && coachOpponentLevel(c) !== "sure" ? {} : null,
    build: c => { const thin = coachOpponentLevel(c) === "guess"; return { title:thin ? I18N.t("coach.limits.opponentAssumed.titleThin", { role: c.opp, champion: c.opponent }) : I18N.t("coach.limits.opponentAssumed.title", { role: c.opp, champion: c.opponent }),
      text:[I18N.t("coach.limits.opponentAssumed.l1", { champion: c.opponent }),
        thin ? I18N.tp("coach.limits.opponentAssumed.l2thin", c.n) : I18N.t("coach.limits.opponentAssumed.l2"),
        thin ? I18N.tp("coach.limits.opponentAssumed.l3thin", c.n) : I18N.t("coach.limits.opponentAssumed.l3")] }; } },
  { id:"limits.unknown-champion", category:"limits", priority:92, basis:"general",
    when: c => c.unknown.length ? {} : null,
    build: c => ({ title:I18N.t("coach.limits.unknownChampion.title", { champions: coachList(c.unknown) }),
      text:[I18N.t("coach.limits.unknownChampion.l1")],
      limits:I18N.t("coach.limits.unknownChampion.limits") }) },
  { id:"limits.rules-only", category:"limits", priority:34, basis:"general", minLevel:3,
    when: c => c.entry && !c.entry.criteria.some(x => x.key === "matchup" && x.live) ? {} : null,
    build: () => ({ title:I18N.t("coach.limits.rulesOnly.title"),
      text:[I18N.t("coach.limits.rulesOnly.l1"), I18N.t("coach.limits.rulesOnly.l2"),
        I18N.t("coach.limits.rulesOnly.l3")] }) },

  // ----- Parties personnelles : jamais dans le classement, et seulement ce que l'échantillon permet -----
  { id:"personal.experience", category:"personal", priority:74, basis:"measured",
    when: c => {
      if (!c.personal || !c.rival) return null;
      const a = c.personal.byChampion[c.pick], b = c.personal.byChampion[label(c.rival.pick)];
      const na = a ? a.games : 0, nb = b ? b.games : 0;
      if (Math.max(na, nb) < PERSONAL_MIN_EXPERIENCE || Math.max(na, nb) < 3 * Math.max(1, Math.min(na, nb))) return null;
      return { na, nb };
    },
    build: (c, d) => { const more = d.na > d.nb ? c.pick : label(c.rival.pick), less = d.na > d.nb ? label(c.rival.pick) : c.pick; return { title:I18N.t("coach.personal.experience.title", { champion: more }),
      text:[I18N.t("coach.personal.experience.l1", { games: d.na, champion: c.pick, rivalGames: d.nb, rival: label(c.rival.pick) }),
        I18N.t("coach.personal.experience.l2", { champion: more, other: less }),
        I18N.t("coach.personal.experience.l3", { games: d.na, rivalGames: d.nb })],
      limits:I18N.t("coach.limit.gamesNotSkill"), evidence:[{ kind:"games", pick:d.na, rival:d.nb }] }; } },
  { id:"personal.results", category:"personal", priority:68, basis:"measured",
    when: c => {
      const s = c.personal && c.personal.byChampion[c.pick];
      return s && s.clearGap ? { s } : null;
    },
    build: (c, d) => ({ title:I18N.t("coach.personal.results.title", { champion: c.pick }),
      text:[I18N.t(d.s.clearGap > 0 ? "coach.personal.results.l1more" : "coach.personal.results.l1less", { games: d.s.games, champion: c.pick }),
        I18N.t(d.s.clearGap > 0 ? "coach.personal.results.l2good" : "coach.personal.results.l2bad", { wins: d.s.wins, games: d.s.games }),
        I18N.t("coach.personal.results.l3", { low: Math.round(d.s.low * 100), high: Math.round(d.s.high * 100), games: d.s.games, overall: Math.round(c.personal.overall * 100) })],
      limits:I18N.t("coach.personal.results.limits"), evidence:[{ kind:"games", value:d.s.games, wins:d.s.wins }] }) },
  { id:"personal.too-few", category:"personal", priority:30, basis:"general", minLevel:2,
    when: c => { if (!c.personal || !c.pick) return null; const s = c.personal.byChampion[c.pick]; return (s ? s.games : 0) < PERSONAL_MIN_RESULT ? { games:s ? s.games : 0 } : null; },
    build: (c, d) => ({ title:I18N.t("coach.personal.tooFew.title", { champion: c.pick }),
      text:[I18N.tp("coach.personal.tooFew.l1", d.games), I18N.tp("coach.personal.tooFew.l2", d.games, { min: PERSONAL_MIN_RESULT }), I18N.t("coach.personal.tooFew.l3", { min: PERSONAL_MIN_RESULT, games: d.games })] }) }
];

function when2(c) {
  if (c.scenarios && c.scenarios.length > 1) return I18N.t("coach.when.hypotheses", { champion: label(c.rival.pick) });
  if (!c.hasFront) return I18N.t("coach.when.noFront");
  const found = whenBetter(c.rival.pick, c.entry.pick, c.enemies, c.opponent);
  return found.length ? I18N.t("coach.when.found", { cases: found.length > 1 ? I18N.t("coach.orComma", { a: found[0], b: found[1] }) : found[0] }) : I18N.t("coach.when.none");
}

// ---------- Principes pour débuter : choisis d'après la draft, jamais au hasard ----------
const COACH_FUNDAMENTALS = [
  { id:"fund.damage-types", term:null, trigger:c => c.n >= 3, roles:null, priority:20, title:I18N.t("coach.fund.damageTypes.title"),
    text:I18N.t("coach.fund.damageTypes.text") },
  { id:"fund.cc", trigger:c => c.compo.cc.length >= 1, roles:null, priority:27, title:I18N.t("coach.fund.cc.title"), text:I18N.t("coach.fund.cc.text") },
  { id:"fund.positioning", trigger:c => c.compo.divers.length >= 1 || c.compo.engage.length >= 2, roles:null, priority:26, title:I18N.t("coach.fund.positioning.title"), text:I18N.t("coach.fund.positioning.text") },
  { id:"fund.range", trigger:c => !!coachRangeDuel(c), roles:["top", "mid", "adc"], priority:28, title:I18N.t("coach.fund.range.title"), text:I18N.t("coach.fund.range.text") },
  { id:"fund.trades", trigger:c => !!c.opponent, roles:["top", "mid", "adc"], priority:18, title:I18N.t("coach.fund.trades.title"), text:I18N.t("coach.fund.trades.text") },
  { id:"fund.farm", trigger:() => true, roles:["top", "mid", "adc"], priority:12, title:I18N.t("coach.fund.farm.title"), text:I18N.t("coach.fund.farm.text") },
  { id:"fund.waves", trigger:c => !!c.pick && hasTrait(pickId(c.pick), "r") || !!c.pick && hasTrait(pickId(c.pick), "g"), roles:["top", "mid"], priority:22, title:I18N.t("coach.fund.waves.title"), text:I18N.t("coach.fund.waves.text") },
  { id:"fund.objectives", trigger:c => c.n >= 4, roles:null, priority:14, title:I18N.t("coach.fund.objectives.title"), text:I18N.t("coach.fund.objectives.text") },
  { id:"fund.vision", trigger:c => c.compo.engage.length >= 2 || c.compo.divers.length >= 2, roles:null, priority:16, title:I18N.t("coach.fund.vision.title"), text:I18N.t("coach.fund.vision.text") },
  { id:"fund.role-top", trigger:() => true, roles:["top"], priority:24, title:I18N.t("coach.fund.roleTop.title"), text:I18N.t("coach.fund.roleTop.text") },
  { id:"fund.role-jungle", trigger:() => true, roles:["jungle"], priority:24, title:I18N.t("coach.fund.roleJungle.title"), text:I18N.t("coach.fund.roleJungle.text") },
  { id:"fund.role-mid", trigger:() => true, roles:["mid"], priority:24, title:I18N.t("coach.fund.roleMid.title"), text:I18N.t("coach.fund.roleMid.text") },
  { id:"fund.role-adc", trigger:() => true, roles:["adc"], priority:24, title:I18N.t("coach.fund.roleAdc.title"), text:I18N.t("coach.fund.roleAdc.text") },
  { id:"fund.role-support", trigger:() => true, roles:["support"], priority:24, title:I18N.t("coach.fund.roleSupport.title"), text:I18N.t("coach.fund.roleSupport.text") }
];

// ---------- Parties personnelles ----------
const PERSONAL_MIN_EXPERIENCE = 20, PERSONAL_MIN_RESULT = 30;
function coachWilson(k, n) { const z = 1.96, p = k / n, d = 1 + z * z / n, c = (p + z * z / (2 * n)) / d, m = z * Math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / d; return [c - m, c + m]; }

// rows : lignes de l'import Riot existant (champ, pos, win, time, acct). Rien n'est lu ailleurs, rien n'entre dans le score.
function personalTrends(rows, options = {}) {
  let list = rows || [];
  if (options.account) list = list.filter(r => r.acct === options.account);
  if (options.position) list = list.filter(r => r.pos === options.position);
  if (!list.length) return null;
  const wins = list.filter(r => r.win).length, overall = wins / list.length, byChampion = {};
  // L'import écrit les noms de l'API Riot (KogMaw, Chogath...) : le conseil lit les noms de DraftMid.
  const riotNames = {}; NAMES.forEach(n => { riotNames[riotKey(n)] = n; riotNames[norm(n)] = label(n); });
  list.forEach(r => { const shown = riotNames[norm(String(r.champ))] ? label(riotNames[norm(String(r.champ))]) : r.champ; const s = byChampion[shown] || (byChampion[shown] = { games:0, wins:0, last:0, kills:0, deaths:0, assists:0, kdaGames:0 }); s.games++; if (r.win) s.wins++; s.last = Math.max(s.last, r.time || 0); if (typeof r.k === "number") { s.kills += r.k; s.deaths += r.d; s.assists += r.a; s.kdaGames++; } });
  Object.keys(byChampion).forEach(n => {
    const s = byChampion[n];
    s.clearGap = 0;
    if (s.games >= PERSONAL_MIN_RESULT) {
      const [low, high] = coachWilson(s.wins, s.games);
      s.low = low; s.high = high;
      // Écart seulement si l'intervalle exclut la moyenne du reste du compte.
      const others = list.length > s.games ? (wins - s.wins) / (list.length - s.games) : overall;
      if (low > others) s.clearGap = 1;
      if (high < others) s.clearGap = -1;
    }
  });
  return { games:list.length, overall, byChampion };
}

// ---------- Point d'entrée pour l'interface ----------
const COACH_LIMIT_SHORT = 3;
// Vue détaillée : assez pour apprendre, pas au point de noyer la draft. Limites et résultats personnels restent toujours visibles.
const COACH_MAX_ITEMS = { 1:7, 2:6, 3:7 };
function coachingAdvice(input) {
  const level = COACH_LEVELS[input.level] || COACH_LEVELS.beginner;
  const c = coachingContext(input);
  const items = [];
  COACH_RULES.forEach(rule => {
    if ((rule.minLevel && level < rule.minLevel) || (rule.maxLevel && level > rule.maxLevel)) return;
    const data = rule.when(c);
    if (!data) return;
    const built = rule.build(c, data), text = built.text[Math.min(level, built.text.length) - 1];
    const basis = built.basis || rule.basis;
    items.push({ id:rule.id, category:rule.category, level:input.level || "beginner", title:built.title, text, priority:rule.priority,
      basis, basisLabel:COACH_BASIS[basis], limits:built.limits || null, terms:(built.terms || []).filter(t => COACH_TERMS[t]), evidence:built.evidence || [],
      stance:built.stance || null, reviewed:COACH_REVIEWED, verification:COACH_CHECKED.has(rule.id) ? "rule" : "human" });
  });
  // Bases du jeu, pour débuter seulement : un ou deux, ceux que cette draft rend utiles.
  if (level === 1 && input.fundamentals !== false) {
    COACH_FUNDAMENTALS.filter(f => (!f.roles || f.roles.includes(c.role)) && f.trigger(c)).sort((a, b) => b.priority - a.priority).slice(0, 2).forEach(f => {
      items.push({ id:f.id, category:"fundamentals", level:"beginner", title:f.title, text:f.text, priority:f.priority, basis:"general", basisLabel:COACH_BASIS.general, limits:null, terms:[], evidence:[], stance:null, reviewed:COACH_REVIEWED, verification:"human" });
    });
  }
  items.sort((a, b) => b.priority - a.priority || (a.id < b.id ? -1 : 1));
  const cap = input.maxItems || COACH_MAX_ITEMS[level];
  for (let i = items.length - 1; i >= 0 && items.length > cap; i--) if (items[i].category !== "limits" && items[i].category !== "personal") items.splice(i, 1);
  // Vue courte : les plus importants, sans trois conseils de la même famille.
  const short = [], perCategory = {};
  items.forEach(i => { if (short.length < (input.limit || COACH_LIMIT_SHORT) && (perCategory[i.category] || 0) < 2 && i.category !== "fundamentals") { short.push(i.id); perCategory[i.category] = (perCategory[i.category] || 0) + 1; } });
  return { version:1, level:input.level || "beginner", role:c.role, pick:c.pick, rival:c.rival ? label(c.rival.pick) : null, opponent:c.opponent, confidence:c.manual ? "manuel" : c.belief.confidence,
    complete:c.n >= 5, unknown:c.unknown, items, short, offline:true };
}

// Résumé pour l'interface : une ligne, une explication facultative, un risque, un conseil pour apprendre, et leur provenance.
function coachingSummary(input) {
  const a = coachingAdvice(Object.assign({}, input, { fundamentals:true }));
  const risky = i => i.category !== "personal" && ((i.stance && !i.stance.relative && i.stance.sign < 0) || i.id === "matchup.range" && i.evidence[0].theirs > i.evidence[0].mine || i.id === "matchup.assassin");
  const tie = a.items.find(i => i.id === "draft.tie");
  const risk = a.items.find(risky) || null;
  const main = a.items.find(i => i.category === "decision" && i.id !== "draft.incomplete" && i !== risk) || a.items.find(i => i.category !== "fundamentals" && i !== risk) || null;
  const lessonOrder = (COACH_LEVELS[input.level] || 1) === 1 ? ["fundamentals", "matchup", "composition"] : ["matchup", "composition", "champion"];
  let lesson = null;
  lessonOrder.some(cat => { lesson = a.items.find(i => i.category === cat && i !== main && i !== risk) || null; return !!lesson; });
  const shorten = i => i ? { id:i.id, title:i.title, text:i.text, source:i.basisLabel, basis:i.basis, limits:i.limits, verification:i.verification } : null;
  return {
    version:1, level:a.level, pick:a.pick, rival:a.rival, opponent:a.opponent, confidence:a.confidence,
    headline: !a.pick ? null : tie ? tie.title : a.rival ? I18N.t("coach.summary.headline", { champion: a.pick, rival: a.rival }) : a.pick,
    tied: tie ? tie.evidence[0].picks : [],
    explanation:shorten(main), risk:shorten(risk), lesson:shorten(lesson)
  };
}

// Définition courte d'un terme, pour une infobulle.
const coachingTerm = term => COACH_TERMS[term] || null;
