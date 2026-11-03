// DraftMid : données et moteur de recommandation. Fichier chargé par la page et par draft-tests.html.
const MATCHUPS = [
  { n:"Ahri", t:"Mage", first:"Fizz", second:"TF", avoid:"Kog'Maw", rec:"Kog'Maw : 2V 6D" },
  { n:"Anivia", t:"Mage", first:"Fizz", second:"Xerath", avoid:"Kog'Maw" },
  { n:"Annie", t:"Mage", first:"TF", second:"Xerath", avoid:"Fizz" },
  { n:"Aurelion Sol", t:"Mage", a:["asol"], first:"Fizz", second:"TF", avoid:"Kog'Maw" },
  { n:"Azir", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Cassiopeia", t:"Mage", a:["cass","cassio"], first:"Xerath", second:"TF", avoid:"Fizz" },
  { n:"Hwei", t:"Mage", first:"Fizz", second:"TF" },
  { n:"Lissandra", t:"Mage", a:["liss"], first:"TF", second:"Xerath", avoid:"Fizz" },
  { n:"Lux", t:"Mage", first:"Fizz", second:"Xerath", rec:"Xerath : 1V 0D" },
  { n:"Malzahar", t:"Mage", a:["malz"], first:"Kog'Maw", second:"TF", avoid:"Fizz", rec:"Kog'Maw : 10V 2D" },
  { n:"Mel", t:"Mage", first:"Fizz", second:"TF", avoid:"Xerath" },
  { n:"Orianna", t:"Mage", a:["ori"], first:"Fizz", second:"Xerath", rec:"Xerath : 1V 0D" },
  { n:"Swain", t:"Mage", first:"Kog'Maw", second:"Xerath", avoid:"Fizz" },
  { n:"Syndra", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Twisted Fate", t:"Mage", a:["tf"], first:"Fizz", second:"Xerath" },
  { n:"Veigar", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Vel'Koz", t:"Mage", a:["velkoz","vel"], first:"Fizz", second:"TF" },
  { n:"Vex", t:"Mage", first:"TF", second:"Xerath", avoid:"Fizz" },
  { n:"Viktor", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Xerath", t:"Mage", first:"Fizz", second:"TF" },
  { n:"Ziggs", t:"Mage", first:"Fizz", second:"TF" },
  { n:"Zoe", t:"Mage", first:"Fizz", second:"Xerath", rec:"Xerath : 1V 0D" },
  { n:"Akali", t:"Assassin", first:"TF", second:"Fizz", avoid:"Kog'Maw, Xerath" },
  { n:"Fizz", t:"Assassin", first:"TF", second:"Kog'Maw", avoid:"Xerath" },
  { n:"Kassadin", t:"Assassin", a:["kass","kassa"], first:"TF", second:"Xerath", avoid:"Fizz" },
  { n:"Katarina", t:"Assassin", a:["kata"], first:"TF", second:"Fizz", avoid:"Kog'Maw, Xerath" },
  { n:"LeBlanc", t:"Assassin", a:["lb"], first:"TF", second:"Fizz", avoid:"Kog'Maw, Xerath" },
  { n:"Naafiri", t:"Assassin", first:"TF", second:"Fizz", avoid:"Xerath" },
  { n:"Qiyana", t:"Assassin", first:"TF", second:"Fizz", avoid:"Kog'Maw, Xerath" },
  { n:"Talon", t:"Assassin", first:"TF", second:"Fizz", avoid:"Kog'Maw, Xerath" },
  { n:"Zed", t:"Assassin", first:"Fizz", second:"TF", avoid:"Kog'Maw, Xerath" },
  { n:"Cho'Gath", t:"Tank", a:["chogath","cho"], first:"Kog'Maw", second:"Xerath", avoid:"Fizz" },
  { n:"Diana", t:"Combattant", first:"Kog'Maw", second:"TF", avoid:"Xerath", rec:"Kog'Maw : 4V 3D" },
  { n:"Galio", t:"Tank", first:"Kog'Maw", second:"TF", avoid:"Fizz" },
  { n:"Irelia", t:"Combattant", first:"TF", second:"Kog'Maw", avoid:"Xerath" },
  { n:"Malphite", t:"Tank", a:["malph"], first:"Kog'Maw", second:"Xerath", avoid:"Fizz" },
  { n:"Pantheon", t:"Combattant", a:["panth"], first:"TF", second:"Xerath", avoid:"Kog'Maw" },
  { n:"Sion", t:"Tank", first:"Kog'Maw", second:"Xerath", avoid:"Fizz" },
  { n:"Sylas", t:"Combattant", first:"TF", second:"Kog'Maw", avoid:"Fizz" },
  { n:"Yasuo", t:"Combattant", a:["yas"], first:"Fizz", second:"Kog'Maw", avoid:"TF" },
  { n:"Yone", t:"Combattant", first:"Kog'Maw", second:"TF", avoid:"Fizz", rec:"Kog'Maw : 5V 2D" }
].sort((x, y) => x.n.localeCompare(y.n, "fr"));

// Profils : t tank, b bruiser, s carry fragile, a assassin/plongeur, c contrôle ciblé, e engage, m très mobile, ? inconnu
const ROSTER = Object.fromEntries("Aatrox:b|Ahri:sm|Akali:am|Akshan:sm|Alistar:tce|Ambessa:bam|Amumu:te|Anivia:s|Annie:sc|Aphelios:s|Ashe:se|Aurelion Sol:s|Aurora:sm|Azir:s|Bard:|Bel'Veth:bam|Blitzcrank:e|Brand:s|Braum:t|Briar:ba|Caitlyn:s|Camille:bace|Cassiopeia:s|Cho'Gath:t|Corki:s|Darius:b|Diana:bae|Dr. Mundo:t|Draven:s|Ekko:am|Elise:a|Evelynn:a|Ezreal:sm|Fiddlesticks:e|Fiora:bm|Fizz:am|Galio:te|Gangplank:b|Garen:b|Gnar:be|Gragas:be|Graves:s|Gwen:b|Hecarim:bae|Heimerdinger:s|Hwei:s|Illaoi:b|Irelia:bam|Ivern:|Janna:|Jarvan IV:be|Jax:ba|Jayce:b|Jhin:s|Jinx:s|K'Sante:te|Kai'Sa:sa|Kalista:s|Karma:|Karthus:s|Kassadin:am|Katarina:am|Kayle:s|Kayn:a|Kennen:se|Kha'Zix:a|Kindred:s|Kled:be|Kog'Maw:s|LeBlanc:am|Lee Sin:bam|Leona:tce|Lillia:sm|Lissandra:sce|Locke:?|Lucian:sm|Lulu:c|Lux:s|Malphite:te|Malzahar:sc|Maokai:tce|Master Yi:am|Mel:s|Milio:|Miss Fortune:s|Mordekaiser:bc|Morgana:|Naafiri:a|Nami:|Nasus:b|Nautilus:tce|Neeko:se|Nidalee:am|Nilah:se|Nocturne:ace|Nunu:te|Olaf:b|Orianna:s|Ornn:te|Pantheon:bac|Poppy:tce|Pyke:ae|Qiyana:am|Quinn:s|Rakan:em|Rammus:tce|Rek'Sai:ba|Rell:te|Renata Glasc:|Renekton:bce|Rengar:a|Riven:bm|Rumble:b|Ryze:sc|Samira:sa|Sejuani:te|Senna:s|Seraphine:s|Sett:be|Shaco:a|Shen:te|Shyvana:b|Singed:b|Sion:te|Sivir:s|Skarner:tce|Smolder:s|Sona:|Soraka:|Swain:s|Sylas:bam|Syndra:sc|Tahm Kench:tc|Taliyah:s|Talon:am|Taric:t|Teemo:s|Thresh:e|Tristana:sm|Trundle:b|Tryndamere:b|Twisted Fate:s|Twitch:s|Udyr:b|Urgot:b|Varus:s|Vayne:sm|Veigar:s|Vel'Koz:s|Vex:s|Vi:bace|Viego:am|Viktor:s|Vladimir:s|Volibear:be|Warwick:bce|Wukong:be|Xayah:s|Xerath:s|Xin Zhao:bace|Yasuo:bam|Yone:bam|Yorick:b|Yunara:s|Yuumi:|Zaahen:?|Zac:te|Zed:am|Zeri:sm|Ziggs:s|Zilean:|Zoe:s|Zyra:s".split("|").map(x => { const i = x.indexOf(":"); return [x.slice(0, i), x.slice(i + 1)]; }));
const ALIASES = { "Twisted Fate":["tf"], "Miss Fortune":["mf"], "Jarvan IV":["j4","jarvan"], "Xin Zhao":["xin"], "Kog'Maw":["kog"], "Aurelion Sol":["asol"], "Master Yi":["yi"], "Dr. Mundo":["mundo"], "Gangplank":["gp"], "LeBlanc":["lb"], "Lee Sin":["lee"], "Tahm Kench":["tahm"], "Katarina":["kata"], "Cassiopeia":["cass","cassio"], "Orianna":["ori"], "Malzahar":["malz"], "Malphite":["malph"], "Blitzcrank":["blitz"], "Warwick":["ww"], "Renata Glasc":["renata"], "Heimerdinger":["heim","donger"], "Fiddlesticks":["fiddle"], "Kassadin":["kassa"], "Lissandra":["liss"], "Pantheon":["panth"], "Yasuo":["yas"], "Tristana":["trist"], "Nautilus":["naut"], "Seraphine":["sera"], "Mordekaiser":["morde"], "Hecarim":["hec"] };

const FULL = { "TF":"Twisted Fate" };
const PICKS = ["TF", "Fizz", "Kog'Maw", "Xerath"];
const MID_ORDER = { Mage:0, Assassin:1, Combattant:2, Tank:3 };
const HOLD_GAP = 2;
const label = p => FULL[p] || p;
const norm = s => s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().replace(/[^a-z0-9]/g, "");
const esc = s => s.replace(/[&<>"]/g, c => ({ "&":"&amp;", "<":"&lt;", ">":"&gt;", '"':"&quot;" }[c]));
const NAMES = Object.keys(ROSTER).sort((x, y) => x.localeCompare(y, "fr"));
const champKeys = n => [n].concat(ALIASES[n] || []).map(norm);
const matchupOf = name => MATCHUPS.find(m => m.n === name) || null;


// ---------- Moteur V2.1 ----------
// Un seul calcul : chaque critère donne une note (v) de -2 à +2, multipliée par son poids.
// Le score, les raisons, le classement et la comparaison viennent tous de ces mêmes lignes.
// Le score sert à comparer les picks : ce n'est ni un winrate ni une probabilité.
const ENGINE_VERSION = "2.2";
// Préférences enregistrées une fois. Elles ne changent le calcul que pour exclure un pick ou départager une égalité exacte.
const ENGINE_PREFS = { excluded: [], favorite: null };

// Classes publiées par Riot sur Data Dragon 16.21 : profil provisoire, à compléter à la main.
const PROVISIONAL = ["Locke", "Zaahen"];
ROSTER.Locke = "as";
ROSTER.Zaahen = "b";
// Avis général, comme le reste des profils.
const POKE = new Set("Ashe,Caitlyn,Corki,Ezreal,Heimerdinger,Hwei,Jayce,Jhin,Karma,Kog'Maw,Lux,Mel,Nidalee,Orianna,Seraphine,Syndra,Varus,Vel'Koz,Viktor,Xerath,Ziggs,Zoe".split(","));

const CRITERIA = [
  { key:"matchup", label:"Matchup mid", weight:3, source:"Tableau de matchups écrit à la main. Inclut la pression de lane, pour ne pas la compter deux fois." },
  { key:"blind", label:"Sécurité sans mid connu", weight:1, source:"Calculé : part des mids du tableau où le pick est à éviter. Ne compte que si le mid est inconnu." },
  { key:"targets", label:"Cibles exploitables", weight:1.5, source:"Règle heuristique : nombre de carrys fragiles adverses." },
  { key:"front", label:"Tanks et bruisers", weight:2, source:"Règle heuristique : nombre de frontlines adverses." },
  { key:"dive", label:"Survie aux plongeurs", weight:2, source:"Règle heuristique : nombre d'assassins et de plongeurs adverses." },
  { key:"control", label:"Engages et contrôles", weight:2, source:"Règle heuristique : contrôles ciblés et engages adverses, comptés une seule fois par pick." },
  { key:"range", label:"Poke et longue portée", weight:1, source:"Règle heuristique : nombre de champions à longue portée adverses." },
  { key:"peel", label:"Protection nécessaire", weight:1.5, source:"Règle heuristique : frontline allié supposé, mobilité adverse." },
  { key:"map", label:"Pression sur la carte", weight:.5, source:"Atout propre au champion. Poids faible, car il ne dépend pas de la draft." }
];
const MID_COUNT = MATCHUPS.length;
const avoidCount = pick => MATCHUPS.filter(m => (m.avoid || "").split(", ").includes(pick)).length;
const MAX_AVOID = Math.max(...PICKS.map(avoidCount));
const namesOf = list => list.slice(0, 3).join(", ");

function compoOf(enemies) {
  const known = enemies.filter(n => ROSTER[n] !== "?");
  const tag = t => known.filter(n => ROSTER[n].includes(t));
  return {
    known,
    front: known.filter(n => /[tb]/.test(ROSTER[n])),
    carries: tag("s"), divers: tag("a"), cc: tag("c"), engage: tag("e"), mobile: tag("m"),
    poke: known.filter(n => POKE.has(n))
  };
}

// Renvoie [note, explication], ou null si le critère n'a rien à dire pour ce pick.
const RULES = {
  matchup(p, c, x) {
    const m = matchupOf(x.mid);
    if (!m) return null;
    if ((m.avoid || "").split(", ").includes(p)) return [-4 / 3, `À éviter contre ${x.mid}`];
    if (m.first === p) return [1, `1er choix contre ${x.mid}`];
    if (m.second === p) return [.5, `2e choix contre ${x.mid}`];
    return [0, `Pas d'avis marqué contre ${x.mid}`];
  },
  blind(p, c, x) {
    if (x.mid) return null;
    const bad = avoidCount(p);
    const v = 1 - 2 * bad / MAX_AVOID;
    return [v, v > 0 ? `Mid inconnu : peu de mauvais matchups (${bad} sur ${MID_COUNT} dans le tableau)` : `Mid inconnu : risqué, mauvais dans ${bad} matchups sur ${MID_COUNT} du tableau`];
  },
  targets(p, c) {
    const n = c.carries.length;
    if (p === "Fizz") {
      if (n >= 2) return [2, `${n} cibles fragiles : ${namesOf(c.carries)}`];
      if (n === 1) return [.5, `Une cible fragile : ${c.carries[0]}`];
      if (c.known.length >= 4) return [-2, "Aucune cible fragile à éliminer"];
    }
    if (p === "Xerath" && n >= 2) return [.5, `${n} cibles à abîmer de loin : ${namesOf(c.carries)}`];
    return null;
  },
  front(p, c) {
    const n = c.front.length;
    if (p === "Kog'Maw") {
      if (n >= 3) return [2, `${n} tanks ou bruisers : ses dégâts en % des PV font la différence`];
      if (n === 2) return [1.5, "2 tanks ou bruisers : ses dégâts en % des PV sont utiles"];
    }
    if (p === "Fizz") {
      if (n >= 4) return [-1, `${n} frontlines à traverser`];
      if (n === 3) return [-.5, "Beaucoup de frontline à traverser"];
    }
    if ((p === "TF" || p === "Xerath") && n >= 3) return [-.25, `${n} frontlines : peu de prise sur les tanks`];
    return null;
  },
  dive(p, c) {
    const n = c.divers.length;
    if (n < 2) return null;
    if (p === "Kog'Maw" || p === "Xerath") return [n >= 3 ? -2 : -1.5, `${n} plongeurs contre un champion immobile : ${namesOf(c.divers)}`];
    if (p === "Fizz") return [0, "Son E lui laisse une sortie contre les plongeurs"];
    return [0, "Son étourdissement l'aide à se défendre contre les plongeurs"];
  },
  control(p, c) {
    if (p === "Fizz" && c.cc.length >= 2) return [-1.5, `${c.cc.length} contrôles ciblés punissent ses plongeons : ${namesOf(c.cc)}`];
    if (p === "TF" && (c.cc.length >= 2 || c.engage.length >= 2)) return [.75, "Peut jouer loin de leur engage et revenir avec son ultime"];
    if ((p === "Kog'Maw" || p === "Xerath") && c.engage.length >= 3) return [-.5, `${c.engage.length} engages contre un champion lent : ${namesOf(c.engage)}`];
    return null;
  },
  range(p, c) {
    if (c.poke.length < 2) return null;
    if (p === "Xerath") return [1, `Peut rendre les coups à distance : ${namesOf(c.poke)}`];
    if (p === "Kog'Maw") return [-1, `Se fait toucher avant de pouvoir frapper : ${namesOf(c.poke)}`];
    return null;
  },
  peel(p, c, x) {
    if (p === "Kog'Maw") return x.hasFront ? [0, "Suppose un frontline allié pour le protéger"] : [-2, "Ton équipe n'a pas de frontline pour le protéger"];
    if (p !== "Xerath") return null;
    if (!x.hasFront && c.mobile.length >= 2) return [-2, "Personne pour te protéger de leur mobilité"];
    if (c.mobile.length >= 3) return [-4 / 3, `${c.mobile.length} champions très mobiles : ${namesOf(c.mobile)}`];
    if (x.hasFront && c.divers.length <= 1 && c.mobile.length <= 1 && c.known.length >= 3) return [4 / 3, "Peu de plongeurs et de mobilité, et un frontline devant toi"];
    return null;
  },
  map(p) {
    return { "TF":[1, "Son ultime couvre toute la carte"], "Fizz":[.5, "Peut roamer une fois sa vague poussée"], "Kog'Maw":[-.5, "Peu d'impact hors des combats groupés"] }[p] || null;
  }
};

function evaluatePick(pick, compo, ctx) {
  const criteria = CRITERIA.map(def => {
    const rated = RULES[def.key](pick, compo, ctx);
    if (!rated) return null;
    return { key:def.key, label:def.label, v:rated[0], impact:rated[0] * def.weight, text:rated[1] };
  }).filter(Boolean);
  return {
    pick,
    score: criteria.reduce((sum, c) => sum + c.impact, 0),
    criteria,
    why: criteria.filter(c => c.impact !== 0).map(c => ({ key:c.key, v:c.impact, text:c.text }))
  };
}

function analyze(enemies, mid, hasFront) {
  const compo = compoOf(enemies);
  const ctx = { mid, hasFront };
  // Égalité : on préfère le pick le moins pénalisé. L'ordre ne donne aucun avantage à un champion précis.
  const worst = p => Math.min(0, ...p.why.map(w => w.v));
  const ranking = availablePicks(enemies).map(p => evaluatePick(p, compo, ctx))
    .sort((a, b) => b.score - a.score || worst(b) - worst(a) || (b.pick === ENGINE_PREFS.favorite) - (a.pick === ENGINE_PREFS.favorite));
  return { ranking, unknown: enemies.filter(n => PROVISIONAL.includes(n)), counts: compo };
}

function availablePicks(enemies) {
  return PICKS.filter(p => !enemies.includes(label(p)) && !ENGINE_PREFS.excluded.includes(p));
}

// ---------- Mid probable ----------
// Listes indicatives, écrites à la main. Le résultat reste une supposition, jamais un rôle confirmé.
const MARKSMEN = new Set("Aphelios,Ashe,Caitlyn,Corki,Draven,Ezreal,Graves,Jhin,Jinx,Kai'Sa,Kalista,Kindred,Kog'Maw,Lucian,Miss Fortune,Nilah,Samira,Senna,Sivir,Smolder,Tristana,Twitch,Varus,Vayne,Xayah,Yunara,Zeri".split(","));
const SUPPORTS = new Set("Alistar,Bard,Blitzcrank,Braum,Janna,Karma,Leona,Lulu,Milio,Morgana,Nami,Nautilus,Pyke,Rakan,Rell,Renata Glasc,Seraphine,Sona,Soraka,Tahm Kench,Taric,Thresh,Yuumi,Zilean,Zyra".split(","));
const JUNGLERS = new Set("Amumu,Bel'Veth,Briar,Diana,Ekko,Elise,Evelynn,Fiddlesticks,Gragas,Hecarim,Ivern,Jarvan IV,Karthus,Kayn,Kha'Zix,Lee Sin,Lillia,Maokai,Master Yi,Nidalee,Nocturne,Nunu,Rammus,Rek'Sai,Rengar,Sejuani,Shaco,Shyvana,Skarner,Trundle,Udyr,Vi,Viego,Volibear,Warwick,Wukong,Xin Zhao,Zac".split(","));

function guessMid(enemies) {
  const rank = n => {
    const offRole = MARKSMEN.has(n) || SUPPORTS.has(n) || JUNGLERS.has(n) ? 10 : 0;
    return offRole + MID_ORDER[matchupOf(n).t];
  };
  const known = enemies.filter(matchupOf);
  if (!known.length) return null;
  return known.reduce((best, n) => rank(n) < rank(best) ? n : best);
}

// Le nom affiché ne change que si un autre pick mène d'au moins HOLD_GAP points.
function holdPick(ranking, held) {
  if (!ranking.length) return null;
  const heldEntry = ranking.find(p => p.pick === held);
  if (!heldEntry) return ranking[0].pick;
  return ranking[0].score - heldEntry.score >= HOLD_GAP ? ranking[0].pick : held;
}

function suggestBans() {
  const own = PICKS.map(label);
  const active = p => !ENGINE_PREFS.excluded.includes(p);
  return MATCHUPS
    .filter(m => !own.includes(m.n))
    .map(m => ({
      n: m.n,
      harm: (m.avoid || "").split(", ").filter(p => p && active(p)).length,
      threat: (ROSTER[m.n].match(/[ace]/g) || []).length
    }))
    .filter(x => x.harm > 0)
    .sort((x, y) => y.harm - x.harm || y.threat - x.threat)
    .slice(0, 3)
    .map(x => x.n);
}

const banReason = name => {
  const m = matchupOf(name);
  const hit = m && m.avoid ? m.avoid.split(", ").filter(p => !ENGINE_PREFS.excluded.includes(p)) : [];
  return hit.length ? `gêne ${hit.map(label).join(", ")} dans le tableau de matchups` : "";
};

function positiveReasons(entry) {
  return entry.why.filter(w => w.v > 0).sort((x, y) => y.v - x.v).slice(0, 2).map(w => w.text);
}

function mainRisk(entry) {
  const bad = entry.why.filter(w => w.v < 0).sort((x, y) => x.v - y.v)[0];
  return bad ? bad.text : "";
}

// ---------- Comparaison ----------
const PICK_STYLES = {
  "Fizz": "Assassin de mêlée : entre, tue une cible et ressort.",
  "TF": "Mage de carte : pousse, contrôle une cible et rejoint les autres lanes.",
  "Kog'Maw": "Artillerie immobile : fait de gros dégâts s'il est protégé.",
  "Xerath": "Artillerie à très longue portée : affaiblit avant le combat."
};

function comparePicks(a, b) {
  const byKey = (entry, key) => entry.criteria.find(c => c.key === key);
  const rows = CRITERIA.map(def => {
    const ca = byKey(a, def.key);
    const cb = byKey(b, def.key);
    const ia = ca ? ca.impact : 0;
    const ib = cb ? cb.impact : 0;
    return { key:def.key, label:def.label, a:ia, b:ib, diff:ia - ib, textA:ca ? ca.text : "", textB:cb ? cb.text : "" };
  }).filter(r => r.a !== 0 || r.b !== 0);
  const decisive = rows.filter(r => r.diff !== 0).sort((x, y) => Math.abs(y.diff) - Math.abs(x.diff));
  return { rows, decisive, gap: a.score - b.score };
}

const WHAT_IF_ADDS = [["un tank de plus", "Ornn"], ["un plongeur de plus", "Zed"], ["un contrôle ciblé de plus", "Leona"], ["un carry fragile de plus", "Jinx"], ["un poke de plus", "Ziggs"]];

// Recalcule la draft avec le même moteur pour trouver quand l'autre pick passerait devant.
function whenBetter(target, other, enemies, mid) {
  const beats = (list, m, front) => {
    const r = analyze(list, m, front).ranking;
    const t = r.find(p => p.pick === target);
    const o = r.find(p => p.pick === other);
    return t && o && t.score > o.score;
  };
  const found = [];
  if (beats(enemies, mid, false)) found.push("si ton équipe n'a pas de frontline");
  enemies.filter(n => matchupOf(n) && n !== mid).forEach(n => { if (beats(enemies, n, true)) found.push(`si le mid adverse est ${n}`); });
  if (enemies.length < 5) WHAT_IF_ADDS.forEach(([text, champ]) => { if (!enemies.includes(champ) && beats(enemies.concat(champ), mid, true)) found.push(`avec ${text}`); });
  return found.slice(0, 3);
}

function midSwapEffect(enemies, mid, shown, hasFront = true) {
  for (const n of enemies) {
    if (n === mid || !matchupOf(n)) continue;
    const top = analyze(enemies, n, hasFront).ranking[0];
    if (top && top.pick !== shown) return { mid:n, pick:top.pick };
  }
  return null;
}

// Le critère qui avantage réellement a par rapport à b, jamais un simple gros écart.
function leadCriterion(a, b) {
  if (a.score - b.score < 1e-9) return null;
  return comparePicks(a, b).decisive.find(r => r.diff > 0) || null;
}

const positionText = (ranking, entry) => {
  const i = ranking.indexOf(entry);
  if (i === 0) {
    const next = ranking[1];
    if (!next) return "Seul pick disponible.";
    const lead = leadCriterion(entry, next);
    return lead ? `1er : passe devant ${label(next.pick)} surtout grâce au critère « ${lead.label} ».` : `1er, à égalité avec ${label(next.pick)}.`;
  }
  const lead = leadCriterion(ranking[0], entry);
  return lead ? `Derrière ${label(ranking[0].pick)} surtout à cause du critère « ${lead.label} ».` : `À égalité avec ${label(ranking[0].pick)}.`;
};

function compoReading(c) {
  const strong = [];
  const weak = [];
  if (c.front.length >= 2) strong.push("frontline");
  if (c.engage.length >= 2) strong.push("engage");
  if (c.cc.length >= 2) strong.push("contrôles ciblés");
  if (c.divers.length >= 2) strong.push("plongeurs");
  if (c.poke.length >= 2) strong.push("poke");
  if (c.carries.length >= 2) weak.push("plusieurs cibles fragiles");
  if (c.known.length >= 4 && !c.front.length) weak.push("pas de frontline");
  if (c.known.length >= 4 && !c.engage.length) weak.push("peu d'engage");
  if (c.known.length >= 4 && !c.cc.length) weak.push("aucun contrôle ciblé");
  return { strong, weak };
}

// ---------- Plan de jeu contextuel ----------
// Règles écrites à la main. Aucun cooldown, aucune statistique, aucun build supposé.
const pickThreats = {
  "Fizz": c => c.cc.concat(c.divers),
  "TF": c => c.divers.concat(c.engage),
  "Kog'Maw": c => c.divers.concat(c.engage),
  "Xerath": c => c.divers.concat(c.mobile, c.engage)
};
const threatsFor = (pick, c) => [...new Set(pickThreats[pick](c))].slice(0, 2);
function threatText(pick, name, c) {
  if (c.cc.includes(name) && pick === "Fizz") return "Son contrôle ciblé peut stopper ton plongeon.";
  if (c.divers.includes(name)) return pick === "Fizz" ? "Peut te rattraper quand tu ressors d'un combat." : "Peut te sauter dessus si tu es mal placé.";
  if (c.mobile.includes(name)) return "Sa mobilité lui permet d'éviter tes sorts à distance.";
  if (c.engage.includes(name)) return "Peut démarrer le combat sur toi.";
  return "À surveiller pendant la partie.";
}
const targetsFor = (pick, c) => (pick === "Kog'Maw" ? c.front.concat(c.carries) : c.carries).filter((n, i, l) => l.indexOf(n) === i).slice(0, 2);

function winCondition(pick, c) {
  const carry = c.carries[0];
  return {
    "Fizz": carry ? `Pousse ta vague, roam, et tue ${carry} avant l'objectif.` : "Pousse ta vague, roam, et tue leur carry avant l'objectif.",
    "TF": "Pousse à la carte bleue, puis crée un surnombre avec ton ultime.",
    "Kog'Maw": c.front.length >= 2 ? `Reste derrière ton frontline et fais fondre ${c.front[0]} autour des objectifs.` : "Reste derrière ton frontline et frappe autour des objectifs.",
    "Xerath": "Affaiblis-les à distance avant l'objectif, loin de leurs plongeurs."
  }[pick];
}

function buildPlan(pick, compo, mid, jungler) {
  const m = matchupOf(mid);
  const midBad = m && (m.avoid || "").split(", ").includes(pick);
  const midCc = mid && compo.cc.includes(mid);
  const midDive = mid && compo.divers.includes(mid);
  const diver = compo.divers.filter(n => n !== mid)[0];
  const cc = compo.cc[0];
  const engager = compo.engage[0];
  const carry = compo.carries.filter(n => n !== mid)[0] || compo.carries[0];
  const tank = compo.front[0];
  const ranged = pick === "Xerath" || pick === "Kog'Maw";

  const lane = [
    midBad && `${mid} est un matchup difficile : farme prudemment et n'échange qu'après avoir vu partir un de ses sorts importants.`,
    pick === "Fizz" && midCc && `${mid} a un contrôle ciblé : attends de le voir partir avant de lancer ton all-in.`,
    pick === "Fizz" && !midBad && mid && `Contre ${mid}, cherche des échanges courts, puis ressors avec ton E.`,
    ranged && midDive && `${mid} peut te sauter dessus : garde tes distances et ne gaspille pas ton sort de contrôle ou de ralentissement.`,
    pick === "Xerath" && !midDive && mid && `Touche ${mid} de loin dès qu'il vient au farm, sans dépasser la moitié de la lane.`,
    pick === "Kog'Maw" && !midDive && mid && `Contre ${mid}, farme à portée maximale et échange seulement quand ta vague te couvre.`,
    pick === "TF" && mid && `Contre ${mid}, garde ta carte dorée pour répondre à un engage ou préparer un gank.`,
    jungler && `${jungler} peut venir en gank : place une ward avant de pousser ta vague.`,
    !mid && "Le mid adverse n'est pas connu : joue d'abord la sécurité et observe son style.",
    "Pousse ta vague avant de bouger, pour ne pas perdre de farm."
  ];
  const midGame = [
    pick === "TF" && carry && `Utilise ton ultime dès qu'une lane peut punir ${carry}.`,
    pick === "Fizz" && carry && `Cherche ${carry} isolé sur un côté, puis disparais avant la riposte.`,
    pick === "Kog'Maw" && "Reste groupé avec ton frontline : seul sur un côté, tu n'as presque aucune échappatoire.",
    pick === "Xerath" && carry && `Affaiblis ${carry} avant chaque objectif, depuis un endroit couvert par une ward.`,
    diver && `Repère ${diver} sur la carte avant de te placer : sans visuel, recule.`,
    engager && `Pose une ward de contrôle sur le chemin de ${engager} avant les objectifs.`,
    tank && pick !== "Kog'Maw" && `Ne perds pas de temps sur ${tank} si une tour ou un objectif est disponible.`,
    "Après un combat gagné, prends tout de suite une tour ou un objectif."
  ];
  const fight = [
    pick === "Fizz" && cc && `Entre seulement après avoir vu partir le contrôle de ${cc}.`,
    pick === "Fizz" && carry && `Ta cible prioritaire est ${carry}, pas le frontline.`,
    pick === "Fizz" && !carry && "Sans cible fragile accessible, ne plonge pas : va chercher un objectif ailleurs.",
    pick === "TF" && carry && `Garde ta carte dorée pour ${carry} ou pour le premier qui plonge.`,
    pick === "TF" && engager && `Temporise tant que ${engager} n'a pas engagé, puis rejoins le combat.`,
    ranged && diver && `Garde-toi une sortie contre ${diver} : ne te place jamais en première ligne.`,
    pick === "Kog'Maw" && tank && `Frappe ${tank} s'il est le plus proche : ne traverse pas le combat pour une cible lointaine.`,
    pick === "Xerath" && carry && `Lance tes sorts sur ${carry} depuis l'arrière, et recule dès qu'un adversaire s'approche.`,
    ranged && engager && `Évite les combats sans vision : ${engager} peut démarrer l'engage sur toi.`,
    "Évite d'ouvrir un combat quand plusieurs adversaires sont invisibles sur la carte."
  ];
  const three = list => [...new Set(list.filter(Boolean))].slice(0, 3);
  return { lane: three(lane), mid: three(midGame), fight: three(fight) };
}

const matchupLink = (pick, mid) => `https://lolalytics.com/lol/${norm(label(pick))}/vs/${norm(mid)}/build/`;
