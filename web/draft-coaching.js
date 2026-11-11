// Moteur de conseils : explique une draft sans changer le classement. Déterministe, hors ligne, sans réseau.
// Lit le moteur stratégique (analyze, compoOf, opponentBelief...) et n'écrit jamais dedans.
// Conseils pour préparer et comprendre une draft : aucune consigne pendant une partie, aucune action dans le client.
// Interface et types : docs/COACHING.md.

const COACH_REVIEWED = "2026-11";
const COACH_LEVELS = { beginner:1, intermediate:2, advanced:3 };
// Nature de ce qui fonde un conseil. Une règle n'est jamais présentée comme une statistique.
const COACH_BASIS = {
  official:"Donnée officielle de Riot",
  measured:"Mesuré sur de vraies parties",
  smallSample:"Mesuré sur peu de parties",
  estimate:"Estimation",
  heuristic:"Règle écrite à la main",
  general:"Principe général du jeu"
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
  frontline:"Les joueurs solides qui se placent devant et encaissent les coups.",
  carry:"Le joueur qui fait le plus de dégâts de son équipe, souvent fragile.",
  engage:"Démarrer un combat en sautant sur l'adversaire.",
  poke:"Toucher l'adversaire de loin, sans entrer vraiment dans le combat.",
  "contrôle":"Un effet qui empêche de bouger ou d'agir : étourdissement, immobilisation...",
  assassin:"Un champion qui saute sur une cible fragile pour la tuer très vite.",
  roam:"Quitter ta voie pour aller aider une autre voie.",
  vague:"Le groupe de sbires qui avance sur une voie.",
  farm:"Donner le dernier coup aux sbires pour gagner de l'or.",
  "échange":"Une courte attaque sur l'adversaire, suivie d'un recul.",
  portée:"La distance à laquelle un champion peut toucher sa cible.",
  objectif:"Une cible importante de la carte : tour, dragon, baron."
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
const coachNum = v => v.toFixed(1).replace(".", ",");
const coachList = names => names.length < 2 ? names.join("") : `${names.slice(0, -1).join(", ")} et ${names[names.length - 1]}`;
const coachRangeWord = range => range === null ? null : range >= 500 ? "longue portée" : range >= 300 ? "portée moyenne" : "corps à corps";

// Trois noms au plus, sans cacher qu'il y en a d'autres.
const coachNames = names => names.length <= 3 ? coachList(names) : `${names.slice(0, 3).join(", ")} et ${names.length - 3} autre${names.length > 4 ? "s" : ""}`;
// Raison écrite par le moteur, à placer au milieu d'une phrase : sans majuscule isolée, et sans laisser croire qu'une liste coupée est complète.
function coachReason(text, fallback) {
  if (!text) return fallback;
  const m = text.match(/^(\d+) ([^:]+) : ([^:]+)$/);
  let out = text;
  if (m && m[3].split(", ").every(n => ROSTER[n] !== undefined)) { const shown = m[3].split(", ").length, total = +m[1]; if (shown < total) out = `${text} et ${total - shown} autre${total - shown > 1 ? "s" : ""}`; }
  return NAMES.some(n => out.startsWith(n)) ? out : out.charAt(0).toLowerCase() + out.slice(1);
}
// Pourquoi un pick est affiché en premier à score égal (tieCause, draft-engine.js). shown : le nom affiché en premier.
function tieCauseText(cause, shown) {
  if (!cause) return "";
  const first = label(cause.first), second = label(cause.second);
  if (shown && shown !== cause.first) return `${label(shown)} reste en premier car c'était déjà le conseil affiché : à score égal, le nom ne change pas en cours de draft.`;
  if (cause.kind === "risk") return cause.firstRisk
    ? `${first} passe en premier : son pire critère pèse moins (« ${cause.firstRisk} », contre « ${cause.secondRisk} » pour ${second}).`
    : `${first} passe en premier : aucun critère ne le pénalise, alors que ${second} a « ${cause.secondRisk} » contre lui.`;
  if (cause.kind === "favorite") return `${first} passe en premier car c'est ton favori : ce n'est pas un avantage dans la draft.`;
  if (cause.kind === "mixed") return "L'ordre suit d'abord le risque (le pire critère le moins lourd passe devant), puis ton favori, puis l'alphabet.";
  return "Même risque : l'ordre est alphabétique, il ne veut rien dire.";
}
const coachRangeWords = names => { const w = names.map(n => coachRangeWord(coachRange(n)) || "portée inconnue"); return names.length === 2 && w[0] === w[1] ? `Les deux : ${w[0]}` : names.map((n, i) => `${n} : ${w[i]}`).join(", "); };
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
    build: (c, d) => { const names = coachList([c.pick].concat(d.others)); return { title:`Égalité : ${names}`,
      text:[`Le classement ne les départage pas. Choisis celui que tu connais le mieux.`,
        d.sameSheet ? `Leurs fiches sont identiques : l'outil ne peut pas les distinguer dans cette draft. Ta maîtrise et ton envie comptent davantage que le classement.`
          : `Même score pour des raisons différentes. Regarde ce que chacun apporte dans Analyse, vue Comparer, et choisis selon ta maîtrise.`,
        `Score identique (${coachNum(c.entry.score)}). ${tieCauseText(d.cause, c.entry.pick)}${d.sameSheet ? " Fiches identiques : aucune règle ne peut les séparer." : ""}`],
      limits:"Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu.", evidence:[{ kind:"tie", picks:[c.pick].concat(d.others), score:+c.entry.score.toFixed(2), cause:d.cause ? d.cause.kind : null }] }; } },
  { id:"draft.why", category:"decision", priority:82, basis:"heuristic",
    when: c => {
      if (!c.n || !c.entry || (c.entry.tiedWith && c.entry.tiedWith.length) || (c.rival && c.entry.score - c.rival.score < HOLD_GAP)) return null;
      const live = c.opponent && liveMatchup(c.entry.pick, c.opponent);
      const best = c.entry.criteria.filter(x => x.impact > 0 && x.text && !(live && x.key === "matchup")).sort((a, b) => b.impact - a.impact)[0];
      return best ? { best, reason:signedText(best, 1) } : null;
    },
    build: (c, d) => ({ title:`Pourquoi ${c.pick}`,
      text:[`La raison principale : ${coachReason(d.reason)}.`,
        `« ${d.best.label} » pèse le plus : ${coachReason(d.reason)}.`,
        `Critère le plus lourd : « ${d.best.label} » (+${coachNum(d.best.impact)})${c.rival ? `, pour ${coachNum(c.entry.score - c.rival.score)} points d'écart avec ${label(c.rival.pick)}` : ""}.`],
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
      const why = coachReason(d.reason, `avantage sur « ${d.row.label} »`), stays = `${c.pick} reste affiché tant que l'écart reste sous ${HOLD_GAP} points, pour que le conseil ne change pas à chaque pick.`;
      return { title:d.held ? `Choix serré : ${d.lead} passe devant de peu` : `${c.pick} ou ${d.other} : le choix est serré`,
        text:d.held ? [`${d.lead} passe devant de peu : ${why}. ${stays}`,
          `${d.lead} mène surtout sur « ${d.row.label} »${d.reason ? ` : ${coachReason(d.reason)}` : ""}. ${stays}`,
          `${d.lead} mène de ${coachNum(d.gap)} point, sous le seuil de ${HOLD_GAP} utilisé pour changer le nom affiché. Le critère décisif est « ${d.row.label} ». Ces points servent à comparer, ce n'est pas une probabilité de victoire.`]
        : [`Les deux se valent presque. ${c.pick} passe devant : ${why}. Prends celui que tu maîtrises le mieux.`,
          `${c.pick} mène surtout sur « ${d.row.label} »${d.reason ? ` : ${coachReason(d.reason)}` : ""}. L'écart reste faible : ta maîtrise et ton équipe peuvent décider.`,
          `Écart de ${coachNum(d.gap)} point : sous le seuil de ${HOLD_GAP} utilisé pour parler de choix net. Le critère décisif est « ${d.row.label} ». Ces points servent à comparer, ce n'est pas une probabilité de victoire.`],
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
      const where = d.rank === "first" ? `${c.pick} reste devant car le reste de la draft compense, par rapport à tes autres champions.`
        : d.rank === "tied" ? `${c.pick} reste au même score que ${other}, car le reste de la draft compense.`
        : `${c.pick} reste affiché, mais ${other} mène de peu.`;
      return { title:d.rank === "held" ? `${c.pick} reste affiché malgré un vrai risque` : `${c.pick} ressort malgré un vrai risque`,
        text:[`Le risque : ${risk}. ${where}`,
          d.rank === "first" ? `Le score reste le meilleur, mais « ${d.worst.label} » pèse contre ce choix : ${risk}. Vérifie que tu peux jouer avec cette contrainte.`
            : `« ${d.worst.label} » pèse contre ce choix : ${risk}. ${where} Vérifie que tu peux jouer avec cette contrainte.`,
          `Critère négatif « ${d.worst.label} » (${coachNum(d.worst.impact)}), compensé par les autres. Un classement par somme peut masquer ce type de faiblesse : décide si elle est acceptable pour toi.`],
        evidence:[{ kind:"criterion", key:d.worst.key, value:+d.worst.impact.toFixed(2) }], stance:{ key:d.worst.key, sign:-1 } };
    } },
  { id:"draft.incomplete", category:"decision", priority:40, basis:"general",
    when: c => c.n > 0 && c.n < 5 ? {} : null,
    build: c => ({ title:`Il reste ${5 - c.n} adversaire${5 - c.n > 1 ? "s" : ""} à voir`,
      text:[`Ce conseil peut changer. Attends si tu peux, avant de te décider.`,
        `La lecture de la composition se fait sur ${c.n} champion${c.n > 1 ? "s" : ""} seulement : les comptes de frontline et de contrôles peuvent encore bouger.`,
        `Les critères à seuil ne se déclenchent pas encore tous, et ${5 - c.n} rôle${5 - c.n > 1 ? "s restent" : " reste"} ouvert${5 - c.n > 1 ? "s" : ""} dans le calcul des hypothèses.`] }) },

  // ----- Matchups -----
  { id:"matchup.measured", category:"matchup", priority:88, basis:"measured",
    when: c => { if (!c.opponent || !c.entry) return null; const live = liveMatchup(c.entry.pick, c.opponent); return live ? { live } : null; },
    build: (c, d) => {
      const small = d.live.effective < 100, good = d.live.gap >= 0;
      return { basis:small ? "smallSample" : "measured", title:`${c.pick} contre ${c.opponent} : bilan mesuré`,
        text:[`Sur ${d.live.games} parties de joueurs habitués à ${c.pick}, ce matchup se passe ${good ? "plutôt mieux" : "plutôt moins bien"} que leur moyenne.`,
          `Écart de ${d.live.gap >= 0 ? "+" : ""}${Math.round(d.live.gap * 100)} points de victoires par rapport à leur moyenne sur ${c.pick}, sur ${d.live.games} parties.`,
          `Écart ramené vers zéro selon l'échantillon : ${d.live.gap >= 0 ? "+" : ""}${Math.round(d.live.gap * 100)} points, sur ${d.live.games} parties${d.live.recent ? "" : " de patchs précédents"}. Il compare à leur moyenne sur ce champion, pas à 50 %.`],
        limits:small ? "Peu de parties : à prendre comme une tendance." : "Ces joueurs jouent souvent ce champion : pas forcément ton cas.",
        evidence:[{ kind:"games", value:d.live.games }], stance:{ key:"matchup", sign:Math.sign(d.live.gap) } };
    } },
  { id:"matchup.written", category:"matchup", priority:76, basis:"heuristic",
    when: c => {
      if (!c.opponent || !c.entry || liveMatchup(c.entry.pick, c.opponent) || !matchupOf(c.opponent)) return null;
      const cr = c.crit("matchup");
      return cr && cr.impact !== 0 ? { cr } : null;
    },
    build: (c, d) => { const good = d.cr.impact > 0; return { title:`${c.pick} contre ${c.opponent}${!c.manual && c.belief.confidence !== "élevée" ? " (supposé)" : ""} : ${good ? "plutôt un bon choix" : "un matchup difficile"}`,
      text:[good ? `Le tableau de DRAFT place ${c.pick} parmi les bons choix contre ${c.opponent}.` : `Le tableau de DRAFT déconseille ${c.pick} contre ${c.opponent}. Si ce choix reste devant, c'est pour le reste de la draft.`,
        `${d.cr.text}, selon un tableau écrit à la main. C'est le critère au plus gros poids du calcul.`,
        `${d.cr.text} : avis écrit à la main (${coachNum(d.cr.impact)} points), sans statistiques ni patch. Un bilan calculé le remplacera dès ${MATCHUP_MIN_GAMES} parties utiles.`],
      limits:"Avis écrit à la main, pas une statistique.", evidence:[{ kind:"criterion", key:"matchup", value:+d.cr.impact.toFixed(2) }], stance:{ key:"matchup", sign:Math.sign(d.cr.impact) } }; } },
  { id:"matchup.range", category:"matchup", priority:78, basis:"official",
    when: coachRangeDuel,
    build: (c, d) => ({ title:d.longer ? `${c.opponent} te touche de plus loin` : `Tu touches ${c.opponent} de plus loin`,
      terms:["portée", "échange", "farm"],
      text:d.longer ? [
        `${c.opponent} attaque à ${d.theirs}, toi à ${d.mine}. Pour farmer, tu dois t'approcher et risques d'être touché. Avance quand ses sorts viennent d'être utilisés.`,
        `Écart de portée de ${d.theirs - d.mine} (${d.mine} contre ${d.theirs}). Choisis tes échanges : engage juste après un sort important de ${c.opponent}, puis ressors.`,
        `Écart de portée d'attaque de base : ${d.mine} contre ${d.theirs}. Il pèse surtout tôt dans la partie, et ne dit rien de la portée des sorts ni des outils pour le combler.`
      ] : [
        `Tu attaques à ${d.mine}, ${c.opponent} à ${d.theirs}. Ses attaques de base ne t'atteignent pas de loin, mais ses sorts peuvent changer ça : regarde-les avant d'avancer.`,
        `Écart de portée de ${d.mine - d.theirs} en ta faveur : ${c.opponent} doit s'approcher pour farmer, ce qui t'offre des occasions de frapper. Garde un œil sur ses sorts.`,
        `Avantage de portée d'attaque de base : ${d.mine} contre ${d.theirs}. Il ne compte que si ${c.opponent} n'a pas de quoi le combler : à vérifier dans ses sorts.`
      ], evidence:[{ kind:"range", mine:d.mine, theirs:d.theirs }], limits:"Portée d'attaque de base seulement : les sorts ne sont pas comptés." }) },
  { id:"matchup.tank", category:"matchup", priority:66, basis:"heuristic",
    when: c => c.opponent && c.pick && /t/.test(ROSTER[c.opponent] || "") ? {} : null,
    build: c => { const killer = hasTrait(c.entry.pick, "k"); return { title:`${c.opponent} encaisse beaucoup`, terms:["frontline", "roam"],
      text:killer ? [`${c.opponent} est solide, mais ${c.pick} fait de bons dégâts aux joueurs solides. Tuer ${c.opponent} prend du temps : n'en fais pas ta priorité tout de suite.`,
        `Un tank en face tue rarement vite : joue la durée. ${c.pick} a ce qu'il faut contre une grosse barre de vie.`,
        `Face à un tank, l'intérêt de ${c.pick} grandit avec le temps. C'est une règle générale de sa fiche, pas un bilan mesuré de ce matchup.`]
      : !["top", "mid", "adc"].includes(c.role) ? [`Tu mettras du temps à tuer ${c.opponent}. Ne t'acharne pas : regarde s'il y a une cible plus fragile à ta portée.`,
        `Tuer un tank est long : garde tes sorts pour une cible plus fragile, sauf si tes alliés se concentrent sur lui.`,
        `Contre un tank, ton impact passe souvent par les cibles fragiles et le timing. À nuancer selon ce que ton équipe peut tuer.`]
      : [`Tu mettras du temps à tuer ${c.opponent}. Ne t'acharne pas : va plutôt aider ailleurs quand ta vague est poussée.`,
        `Tuer un tank en voie est long et rarement rentable tôt : pousse ta vague et regarde où ton aide pèserait davantage.`,
        `Contre un tank, l'objectif de voie est souvent le tempo plutôt que le meurtre. À nuancer selon ta capacité à le punir.`],
      limits:"Avis général, pas un bilan de ce matchup." }; } },
  { id:"matchup.assassin", category:"matchup", priority:70, basis:"heuristic",
    when: c => c.opponent && c.pick && /a/.test(ROSTER[c.opponent] || "") && !/t/.test(ROSTER[c.opponent] || "") && !(c.compo.divers.length >= 2 && c.compo.divers.includes(c.opponent)) ? {} : null,
    build: c => ({ title:`${c.opponent} peut te sauter dessus`, terms:["assassin"],
      text:[`${c.opponent} cherche à te surprendre et à te tuer vite. Garde de quoi te défendre tant que ses sorts pour s'approcher sont disponibles.`,
        `Sa menace dépend de sa capacité à arriver sur toi. Quand ses sorts de déplacement sont utilisés, c'est ta fenêtre pour échanger.`,
        `Menace d'accès rapide : la zone dangereuse dépend de ses sorts de déplacement, pas de sa portée d'attaque. Le niveau d'objets de chacun change vite ce rapport.`],
      limits:"Avis général, pas un bilan de ce matchup." }) },
  { id:"matchup.unknown", category:"matchup", priority:72, basis:"estimate",
    when: c => {
      if (c.opponent || !c.pick || !c.n) return null;
      const names = c.belief.candidates.filter(x => x.known && x.p >= .2).map(x => x.name).slice(0, 2);
      return { names, hidden:c.belief.unrevealed >= .2 };
    },
    build: (c, d) => ({ title:d.names.length ? `${c.opp} pas encore identifié : prépare les deux cas` : `${c.opp} pas encore visible`, terms:["portée"],
      text: d.names.length ? [
        `Ce peut être ${d.names.join(" ou ")}. ${coachRangeWords(d.names)}. Regarde les sorts d'invocateur et le placement au début de la partie.`,
        `Hypothèses : ${d.names.join(", ")}. Le conseil tient compte des deux. ${coachRangeWords(d.names)}.`,
        `Classement pondéré entre ${d.names.join(" et ")}${d.hidden ? ", et un adversaire pas encore révélé" : ""}. Si tu connais le vrai rôle, corrige-le : le calcul redevient exact pour ce matchup.`
      ] : [
        `Tant qu'il n'est pas visible, le conseil ne porte pas sur ton duel. Si tu peux, choisis après l'avoir vu.`,
        `Le critère de matchup ne s'applique pas encore : le conseil repose sur le reste de la composition et sur la sécurité générale du pick.`,
        `Sans candidat plausible, seul le critère « sans adversaire connu » intervient pour ce rôle.`
      ], limits:"Déduction d'après les rôles habituels : ce n'est pas une information du client." }) },

  // ----- Composition -----
  { id:"comp.assassins", category:"composition", priority:80, basis:"heuristic",
    when: c => c.compo.divers.length >= 2 && c.pick ? {} : null,
    build: c => {
      const names = coachNames(c.compo.divers), cr = c.crit("dive"), risky = cr && cr.impact < 0, solid = cr && cr.impact > 0;
      return { title:risky ? `${names} peuvent atteindre ${c.pick}` : `${c.compo.divers.length} adversaires sautent sur leurs cibles`, terms:["assassin"],
        text:risky ? [`${names} peuvent venir sur toi d'un coup, et ${c.pick} s'échappe mal. Joue près d'un allié qui peut te protéger.`,
          `${c.pick} a peu de sorties contre ${names}. Reste dans la zone de protection de ton équipe, et repère-les avant d'avancer.`,
          `Risque compté par le moteur (« ${cr.label} »). En jeu, une frontline alliée qui peel le réduit, mais le calcul n'en tient pas compte.`]
        : solid ? [`${names} cherchent des cibles fragiles. ${c.pick} encaisse ou protège bien : place-toi près de ton joueur le plus fragile.`,
          `Contre ${names}, ta solidité aide ton équipe : garde ton carry à portée pour répondre à leur saut.`,
          `Le moteur compte un avantage sur « ${cr.label} » : profil solide ou protecteur face à ${c.compo.divers.length} plongeurs.`]
        : [`${names} cherchent des cibles fragiles. Avant d'avancer, vérifie où ils sont sur la carte.`,
          `Leurs plongeurs punissent un mauvais placement : quand ils ne sont pas visibles, considère qu'ils peuvent arriver.`,
          `${c.compo.divers.length} plongeurs : l'équipe est forte sur une cible isolée, plus faible si elle doit traverser une frontline bien placée.`],
        stance:cr ? { key:"dive", sign:Math.sign(cr.impact) } : null, evidence:[{ kind:"champions", value:c.compo.divers }] };
    } },
  { id:"comp.frontline", category:"composition", priority:62, basis:"heuristic",
    when: c => { const solid = c.compo.front.filter(coachIsSolid); return solid.length >= 2 && c.pick ? { solid } : null; },
    build: (c, d) => { const names = coachNames(d.solid), cr = c.crit("front"), good = cr && cr.impact > 0, bad = cr && cr.impact < 0; return { title:`Des joueurs solides en face : ${names}`, terms:["frontline", "carry"],
      text:good ? [`${names} encaissent beaucoup, et ${c.pick} fait de bons dégâts à ce type de cible. Si l'un d'eux te barre la route, tes coups restent utiles.`,
        `${d.solid.length} joueurs très solides : le profil de ${c.pick} est valorisé ici. Reste en vie assez longtemps pour que tes dégâts comptent.`,
        `Bonus du critère « ${cr.label} » (+${coachNum(cr.impact)}). Attention : il compte aussi les bruisers (${c.compo.front.length} ici), et se déclenche dès 2 : ne le lis pas comme un avantage rare.`]
      : bad ? [`${names} se mettent devant leurs joueurs fragiles. Cherche à les contourner : foncer dans le tas ne marchera pas.`,
        `Trop de frontline à traverser pour ${c.pick} : attends que leurs sorts soient utilisés, ou joue ailleurs sur la carte.`,
        `Le moteur compte un malus (« ${cr.label} ») : ${cr.text}.`]
      : [`${names} encaissent beaucoup. Un combat long les avantage si ton équipe manque de dégâts.`,
        `${d.solid.length} joueurs très solides : ils tiennent longtemps en combat groupé. Pense à leurs joueurs fragiles derrière.`,
        `Composition résistante : à comparer aux dégâts soutenus de ton équipe, que l'outil ne connaît pas.`],
      stance:cr ? { key:"front", sign:Math.sign(cr.impact) } : null, evidence:[{ kind:"champions", value:d.solid }] }; } },
  { id:"comp.no-frontline", category:"composition", priority:64, basis:"heuristic",
    when: c => c.n >= 4 && !c.compo.front.length ? {} : null,
    build: c => ({ title:"Personne pour encaisser en face", terms:["frontline", "carry"],
      text:[`Aucun des ${c.n} adversaires n'encaisse vraiment : si ton équipe les atteint, le combat peut basculer vite.`,
        `Sans frontline, leurs joueurs fragiles sont exposés dès que quelqu'un les atteint. À l'inverse, ils peuvent te tuer vite si tu arrives seul.`,
        `Absence de tank ou de bruiser sur ${c.n} champions visibles. Leur force dépend alors de tuer avant d'être atteint : le premier engagement réussi pèse lourd.`],
      evidence:[{ kind:"count", key:"frontline", value:0 }] }) },
  { id:"comp.control", category:"composition", priority:60, basis:"heuristic",
    when: c => c.compo.cc.length >= 2 && c.pick ? {} : null,
    build: c => { const names = coachNames(c.compo.cc), cr = c.crit("control"), bad = cr && cr.impact < 0, good = cr && cr.impact > 0;
      // Malus venu des engages (champion peu mobile), pas des CC ciblés : même règle, autre branche (draft-engine.js, critère control).
      const engage = bad && !(hasTrait(c.entry.pick, "h") && hasTrait(c.entry.pick, "d") && c.compo.cc.length >= 2);
      return { title:`${names} peuvent t'immobiliser`, terms:["contrôle"],
      text:engage ? [`${coachNames(c.compo.engage)} peuvent engager sur toi, et ${c.pick} est peu mobile. Reste derrière ta frontline et attends leur engage.`,
        `${cr.text}. Place-toi derrière ta frontline avant les objectifs.`,
        `Malus du critère « ${cr.label} » : ${c.compo.engage.length} engages contre un champion peu mobile, pas les CC ciblés.`]
      : bad ? [`Si tu fonces et qu'ils t'immobilisent, tu ne peux plus rien faire. Mieux vaut entrer après avoir vu leurs sorts partir.`,
        `Leurs contrôles punissent ${c.pick} : ${cr.text}. Regarde quel sort vient de partir avant d'entrer.`,
        `Malus du critère « ${cr.label} » : ${c.compo.cc.length} contrôles ciblés visibles.`]
      : good ? [`${c.pick} peut rester loin de leurs contrôles, puis revenir au bon moment. Ne te place pas devant ton équipe.`,
        `${cr.text}. Garde cette distance : c'est ce qui rend ce pick intéressant ici.`,
        `Bonus du critère « ${cr.label} » : profil capable de jouer loin de l'engage et des contrôles.`]
      : [`Un seul de leurs sorts peut t'empêcher d'agir : évite d'être le premier à portée quand tu ne sais pas où ils sont.`,
        `Plusieurs contrôles ciblés : joue autour du moment où ils sont utilisés.`,
        `${c.compo.cc.length} contrôles ciblés : à lire avec les délais de récupération, que l'outil ne connaît pas.`],
      stance:cr ? { key:"control", sign:Math.sign(cr.impact) } : null, evidence:[{ kind:"champions", value:c.compo.cc }] }; } },
  { id:"comp.engage", category:"composition", priority:56, basis:"heuristic",
    when: c => c.compo.engage.length >= 3 ? {} : null,
    build: c => { const names = coachNames(c.compo.engage); return { title:`${names} peuvent lancer le combat`, terms:["engage", "objectif"],
      text:[`Ils peuvent démarrer un combat d'un coup. Évite de rester seul dans une zone que tu ne vois pas, surtout près d'un objectif.`,
        `Équipe qui choisit quand le combat commence : place-toi avant l'apparition des objectifs, pas au dernier moment.`,
        `${c.compo.engage.length} sources d'engage : leur force est de forcer le combat. La vision et l'ordre d'arrivée sur les objectifs deviennent décisifs.`],
      evidence:[{ kind:"champions", value:c.compo.engage }] }; } },
  { id:"comp.poke", category:"composition", priority:54, basis:"heuristic",
    when: c => c.compo.poke.length >= 2 ? {} : null,
    build: c => { const names = coachNames(c.compo.poke), cr = c.crit("range"); return { title:`${names} touchent de très loin`, terms:["poke", "portée"],
      text:[`Ils t'abîment de loin avant le combat. Ne reste pas à découvert sans raison : ta vie baisse pour rien.`,
        `Équipe de poke : si vous arrivez affaiblis, le combat démarre mal. Cherche à engager vite, ou à ne pas rester à portée.`,
        cr ? `Le moteur en tient compte pour ${c.pick} (« ${cr.label} » : ${cr.text}).` : `Le poke gagne le temps : il faut soit le raccourcir, soit soigner. Ton équipe n'est pas connue de l'outil.`],
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
    build: (c, d) => ({ title:`Dégâts surtout ${d.type} en face`, terms:[],
      text:[`Leurs principaux dégâts semblent ${d.type}. Un objet qui protège des dégâts ${d.type} peut donc être plus utile que d'habitude : regarde lesquels existent.`,
        `Répartition déséquilibrée sur ${d.kinds.length} champions : une résistance adaptée a plus de valeur. Vérifie en partie, car les objets adverses changent le partage.`,
        `Estimation d'après les notes de Riot et une table écrite à la main. Le vrai partage dépend des objets et de chaque partie : à confirmer une fois en jeu.`],
      limits:"Estimation, pas une mesure : les dégâts réels dépendent des objets.", evidence:[{ kind:"damage", value:d.type, champions:d.kinds.length }] }) },

  // ----- Champion -----
  { id:"champ.profile", category:"champion", priority:46, basis:"official",
    when: c => c.pick && coachOfficial(c.pick) && CHAMPION_TRAITS[c.pick] !== undefined ? {} : null,
    build: c => {
      const o = coachOfficial(c.pick), traits = CHAMPION_TRAITS[c.pick], word = coachRangeWord(o.range);
      const assets = [], risks = [];
      if (traits.includes("l")) assets.push("touche de très loin");
      if (traits.includes("k")) assets.push("fait de bons dégâts aux joueurs solides");
      if (traits.includes("h")) assets.push("peut tuer une cible fragile");
      if (traits.includes("t")) assets.push("encaisse bien");
      if (traits.includes("c")) assets.push("protège ses alliés");
      if (traits.includes("E")) assets.push("peut lancer les combats");
      if (traits.includes("g")) assets.push("peut peser sur toute la carte");
      if (traits.includes("i")) risks.push("se déplace peu, donc un adversaire mobile peut l'atteindre");
      if (traits.includes("p")) risks.push("a besoin d'alliés autour");
      if (traits.includes("d") && !traits.includes("t")) risks.push("doit entrer dans le combat, donc s'exposer");
      if (traits.includes("w")) risks.push("a peu de prise sur les joueurs solides");
      const main = assets.length ? assets.slice(0, 2).join(" et ") : null;
      const risk = risks.length ? ` Attention : ${risks[0]}.` : "";
      const changes = COACH_RANGE_CHANGES.includes(c.pick), how = changes ? `${c.pick} change de portée selon sa forme ou sa transformation` : `${c.pick} se joue à ${word} (${o.range})`;
      const points = main ? ` Points forts : ${main}.` : " Sa fiche ne lui donne pas de point fort particulier.", rangeText = changes ? "portée variable" : `portée ${o.range}`;
      return { title:`Comprendre ${c.pick}`, terms:["portée"],
        text:[`${how}.${main ? ` Points forts : ${main}.` : ""}${risk}`,
          `Classes Riot : ${o.tags.join(", ")}. ${rangeText.charAt(0).toUpperCase() + rangeText.slice(1)}.${points}${risk}`,
          `Classes Riot : ${o.tags.join(", ")}, ${rangeText}, difficulté notée ${o.difficulty}/10 par Riot.${points}${risk}`],
        limits:"Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.", evidence:[{ kind:"official", tags:o.tags, range:o.range, difficulty:o.difficulty }] };
    } },
  { id:"champ.difficulty", category:"champion", priority:44, basis:"official", maxLevel:1,
    when: c => { const o = c.pick ? coachOfficial(c.pick) : null; const known = c.personal && c.personal.byChampion[c.pick]; return o && o.difficulty >= 8 && !(known && known.games >= PERSONAL_MIN_EXPERIENCE) ? { value:o.difficulty } : null; },
    build: (c, d) => ({ title:`${c.pick} peut demander de la pratique`,
      text:[`Riot note sa difficulté ${d.value}/10. Si tu découvres ce champion, prévois un temps de prise en main. Le choix reste le tien.`],
      limits:"Note de Riot non documentée et parfois contredite par celle du client : une indication, pas une mesure.",
      evidence:[{ kind:"official", difficulty:d.value }] }) },
  { id:"champ.alternative", category:"champion", priority:48, basis:"heuristic", minLevel:2,
    when: c => {
      if (!c.rival || !c.n || c.rival.score > c.entry.score) return null;
      const row = comparePicks(c.rival, c.entry).decisive.find(r => r.diff > 0);
      return row && row.textA ? { row } : null;
    },
    build: (c, d) => ({ title:`Quand préférer ${label(c.rival.pick)} ?`,
      text:[`${label(c.rival.pick)} fait mieux sur un point : ${d.row.textA}.`,
        `${label(c.rival.pick)} reste meilleur sur « ${d.row.label} » : ${d.row.textA}. Si ce point compte le plus pour ta partie, ce choix devient intéressant.`,
        `${label(c.rival.pick)} : avantage de ${coachNum(d.row.diff)} sur « ${d.row.label} ». ${when2(c)}`],
      evidence:[{ kind:"criterion", key:d.row.key, value:+d.row.diff.toFixed(2) }] }) },

  // ----- Données et limites -----
  { id:"limits.opponent-assumed", category:"limits", priority:58, basis:"estimate",
    when: c => !c.manual && c.opponent && c.belief.confidence !== "élevée" ? {} : null,
    build: c => ({ title:`${c.opp} supposé, à vérifier`,
      text:[`DRAFT pense que c'est ${c.opponent}, sans certitude. Si tu sais que c'est un autre champion, corrige-le.`,
        `Confiance ${c.belief.confidence} : déduit des rôles habituels. Une correction change le matchup pris en compte.`,
        `Déduction probabiliste sur les rôles habituels (confiance ${c.belief.confidence}), jamais une donnée du client. Sa qualité dépend de l'âge des données de rôles.`] }) },
  { id:"limits.unknown-champion", category:"limits", priority:92, basis:"general",
    when: c => c.unknown.length ? {} : null,
    build: c => ({ title:`${coachList(c.unknown)} : champion inconnu de DRAFT`,
      text:[`Aucune fiche pour ce champion : il n'entre pas dans ce conseil. Regarde ses sorts en jeu.`],
      limits:"Rien n'est inventé pour un champion sans fiche." }) },
  { id:"limits.rules-only", category:"limits", priority:34, basis:"general", minLevel:3,
    when: c => c.entry && !c.entry.criteria.some(x => x.key === "matchup" && /parties/.test(x.text)) ? {} : null,
    build: () => ({ title:"Ce classement repose sur des règles, pas sur des statistiques",
      text:[`Aucun bilan de vraies parties n'intervient ici.`, `Le score vient de règles et de fiches écrites à la main, sans mesure.`,
        `Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.`] }) },

  // ----- Parties personnelles : jamais dans le classement, et seulement ce que l'échantillon permet -----
  { id:"personal.experience", category:"personal", priority:74, basis:"measured",
    when: c => {
      if (!c.personal || !c.rival) return null;
      const a = c.personal.byChampion[c.pick], b = c.personal.byChampion[label(c.rival.pick)];
      const na = a ? a.games : 0, nb = b ? b.games : 0;
      if (Math.max(na, nb) < PERSONAL_MIN_EXPERIENCE || Math.max(na, nb) < 3 * Math.max(1, Math.min(na, nb))) return null;
      return { na, nb };
    },
    build: (c, d) => { const more = d.na > d.nb ? c.pick : label(c.rival.pick), less = d.na > d.nb ? label(c.rival.pick) : c.pick; return { title:`Tu as plus d'expérience sur ${more}`,
      text:[`${d.na} parties sur ${c.pick}, ${d.nb} sur ${label(c.rival.pick)}. Connaître son champion compte : à toi de voir si cela change ton choix.`,
        `${more} : nettement plus de parties que ${less}. Cela ne change pas le classement, mais c'est un argument à peser.`,
        `Écart d'expérience (${d.na} contre ${d.nb}), hors du score. Le lien avec tes résultats n'est pas établi par ce seul chiffre.`],
      limits:"Le nombre de parties ne dit pas si tu joues bien ce champion.", evidence:[{ kind:"games", pick:d.na, rival:d.nb }] }; } },
  { id:"personal.results", category:"personal", priority:68, basis:"measured",
    when: c => {
      const s = c.personal && c.personal.byChampion[c.pick];
      return s && s.clearGap ? { s } : null;
    },
    build: (c, d) => ({ title:`Tes résultats sur ${c.pick} se distinguent`,
      text:[`Sur ${d.s.games} parties, tu gagnes ${d.s.clearGap > 0 ? "plus" : "moins"} souvent avec ${c.pick} qu'avec tes autres champions.`,
        `${d.s.wins} victoires en ${d.s.games} parties : écart net avec le reste de ton compte, dans le sens ${d.s.clearGap > 0 ? "favorable" : "défavorable"}.`,
        `Intervalle à 95 % de ${Math.round(d.s.low * 100)} à ${Math.round(d.s.high * 100)} sur ${d.s.games} parties, contre ${Math.round(c.personal.overall * 100)} sur le compte. Sans lien de cause établi : adversaires et niveau changent.`],
      limits:"Passé sur ton compte : ni une prévision, ni un jugement sur ton niveau.", evidence:[{ kind:"games", value:d.s.games, wins:d.s.wins }] }) },
  { id:"personal.too-few", category:"personal", priority:30, basis:"general", minLevel:2,
    when: c => { if (!c.personal || !c.pick) return null; const s = c.personal.byChampion[c.pick]; return (s ? s.games : 0) < PERSONAL_MIN_RESULT ? { games:s ? s.games : 0 } : null; },
    build: (c, d) => ({ title:`Trop peu de parties pour juger sur ${c.pick}`,
      text:[`${d.games} partie${d.games > 1 ? "s" : ""} sur ce champion : trop peu pour dire comment ce champion te réussit.`, `${d.games} partie${d.games > 1 ? "s" : ""} : aucune tendance personnelle n'est tirée sous ${PERSONAL_MIN_RESULT} parties.`, `Moins de ${PERSONAL_MIN_RESULT} parties (${d.games}) : aucune conclusion sur tes résultats.`] }) }
];

function when2(c) {
  if (c.scenarios && c.scenarios.length > 1) return `Pas de calcul des cas où ${label(c.rival.pick)} passerait devant tant que ton vis-à-vis n'est pas identifié.`;
  if (!c.hasFront) return "Ces cas ne sont calculés qu'avec une frontline alliée, pas avec ton réglage actuel.";
  const found = whenBetter(c.rival.pick, c.entry.pick, c.enemies, c.opponent);
  return found.length ? `Ce choix passerait devant ${found.slice(0, 2).join(", ou ")}.` : "Aucun changement simple de la draft ne le fait passer devant.";
}

// ---------- Principes pour débuter : choisis d'après la draft, jamais au hasard ----------
const COACH_FUNDAMENTALS = [
  { id:"fund.damage-types", term:null, trigger:c => c.n >= 3, roles:null, priority:20, title:"Dégâts physiques et magiques",
    text:"L'armure réduit les dégâts physiques, la résistance magique réduit les dégâts magiques. Regarde ce que font surtout tes adversaires avant de choisir un objet de protection." },
  { id:"fund.cc", trigger:c => c.compo.cc.length >= 1, roles:null, priority:27, title:"Pourquoi les contrôles comptent", text:"Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir." },
  { id:"fund.positioning", trigger:c => c.compo.divers.length >= 1 || c.compo.engage.length >= 2, roles:null, priority:26, title:"Se placer avant le combat", text:"Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière un allié solide : le joueur touché en premier a peu de chances de finir le combat." },
  { id:"fund.range", trigger:c => !!coachRangeDuel(c), roles:["top", "mid", "adc"], priority:28, title:"Jouer avec la portée", text:"Le plus court doit s'approcher pour toucher : il ne doit le faire que lorsqu'il y gagne quelque chose. Le plus long doit le punir à chaque fois qu'il avance pour un sbire." },
  { id:"fund.trades", trigger:c => !!c.opponent, roles:["top", "mid", "adc"], priority:18, title:"Un bon échange", text:"Attaque quand un sort important de ton adversaire vient de partir, puis recule. Tu lui prends de la vie sans qu'il puisse répondre aussi fort." },
  { id:"fund.farm", trigger:() => true, roles:["top", "mid", "adc"], priority:12, title:"L'or vient d'abord des sbires", text:"Chaque sbire que tu achèves te donne de l'or. Une quinzaine de sbires rapporte à peu près autant qu'une mise à mort : ne prends pas de gros risques pour un échange inutile." },
  { id:"fund.waves", trigger:c => !!c.pick && hasTrait(pickId(c.pick), "r") || !!c.pick && hasTrait(pickId(c.pick), "g"), roles:["top", "mid"], priority:22, title:"Pousser avant de bouger", text:"Avant de quitter ta voie, pousse ta vague contre la tour adverse. Tes sbires occupent l'ennemi, et tu perds moins d'or pendant ton absence." },
  { id:"fund.objectives", trigger:c => c.n >= 4, roles:null, priority:14, title:"Les objectifs gagnent les parties", text:"Une mise à mort rapporte davantage si elle mène à quelque chose : une tour, un dragon, un baron. Après un combat gagné, regarde d'abord ce que ton équipe peut prendre." },
  { id:"fund.vision", trigger:c => c.compo.engage.length >= 2 || c.compo.divers.length >= 2, roles:null, priority:16, title:"Voir avant d'avancer", text:"Une balise montre qui arrive. Sans elle, considère que l'adversaire absent de la carte peut se trouver près de toi." },
  { id:"fund.role-top", trigger:() => true, roles:["top"], priority:24, title:"Le rôle du top", text:"Tu joues souvent seul et loin de ton équipe. Une téléportation ou une tour prise peut peser plus qu'un duel gagné." },
  { id:"fund.role-jungle", trigger:() => true, roles:["jungle"], priority:24, title:"Le rôle du jungler", text:"Tu n'as pas de voie : tu choisis où aider. Va là où un allié peut suivre, et regarde les objectifs qui apparaissent bientôt." },
  { id:"fund.role-mid", trigger:() => true, roles:["mid"], priority:24, title:"Le rôle du mid", text:"Ta voie est courte et centrale : tu peux aider les deux côtés. Pousse ta vague d'abord, puis regarde où ton aide changerait quelque chose." },
  { id:"fund.role-adc", trigger:() => true, roles:["adc"], priority:24, title:"Le rôle de l'ADC", text:"Tu fais beaucoup de dégâts mais tu es fragile. Ton but en combat : rester en vie et frapper ce qui est à portée, pas aller chercher une cible lointaine." },
  { id:"fund.role-support", trigger:() => true, roles:["support"], priority:24, title:"Le rôle du support", text:"Ta valeur ne se lit pas dans les mises à mort. Protéger ton ADC, poser de la vision et lancer le bon combat rapporte plus." }
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
  const risky = i => i.category !== "personal" && ((i.stance && !i.stance.relative && i.stance.sign < 0) || i.id === "matchup.range" && /te touche/.test(i.title) || i.id === "matchup.assassin");
  const tie = a.items.find(i => i.id === "draft.tie");
  const risk = a.items.find(risky) || null;
  const main = a.items.find(i => i.category === "decision" && i.id !== "draft.incomplete" && i !== risk) || a.items.find(i => i.category !== "fundamentals" && i !== risk) || null;
  const lessonOrder = (COACH_LEVELS[input.level] || 1) === 1 ? ["fundamentals", "matchup", "composition"] : ["matchup", "composition", "champion"];
  let lesson = null;
  lessonOrder.some(cat => { lesson = a.items.find(i => i.category === cat && i !== main && i !== risk) || null; return !!lesson; });
  const shorten = i => i ? { id:i.id, title:i.title, text:i.text, source:i.basisLabel, basis:i.basis, limits:i.limits, verification:i.verification } : null;
  return {
    version:1, level:a.level, pick:a.pick, rival:a.rival, opponent:a.opponent, confidence:a.confidence,
    headline: !a.pick ? null : tie ? tie.title : a.rival ? `${a.pick}, sinon ${a.rival}` : a.pick,
    tied: tie ? tie.evidence[0].picks : [],
    explanation:shorten(main), risk:shorten(risk), lesson:shorten(lesson)
  };
}

// Définition courte d'un terme, pour une infobulle.
const coachingTerm = term => COACH_TERMS[term] || null;
