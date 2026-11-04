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
const PICKS = [];
const HOLD_GAP = 2;
const label = p => FULL[p] || p;
const norm = s => s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().replace(/[^a-z0-9]/g, "");

// ---------- Profil du joueur : son rôle et ses champions ----------
const PROFILE_KEY = "draftProfile";
const ROLE_NAMES = { top:"Top", jungle:"Jungle", mid:"Mid", adc:"ADC", support:"Support" };
const ROLE_AT = { top:"au top", jungle:"à la jungle", mid:"au mid", adc:"à l'ADC", support:"au support" };
// Anciennes installations, créées avant les profils : elles gardent leurs réglages sans rien refaire.
const FIRST_PROFILE = { role:"mid", pool:["TF", "Fizz", "Kog'Maw", "Xerath"] };
const pickId = name => name === "Twisted Fate" ? "TF" : name;
let PROFILE = null;

function applyProfile(profile) {
  PROFILE = profile;
  PICKS.length = 0;
  if (profile) profile.pool.forEach(p => { if (!PICKS.includes(pickId(p))) PICKS.push(pickId(p)); });
}

function loadProfile() {
  let saved = null;
  try {
    saved = JSON.parse(localStorage.getItem(PROFILE_KEY));
  } catch (e) {}
  const valid = saved && ROLE_NAMES[saved.role] && Array.isArray(saved.pool) && saved.pool.length > 0;
  applyProfile(valid ? saved : null);
}

function saveProfile(profile) {
  applyProfile(profile);
  try { localStorage.setItem(PROFILE_KEY, JSON.stringify(profile)); } catch (e) {}
}

// Champions les plus joués à un rôle, pour proposer un pool sans chercher.
function championsForRole(role) {
  const index = ROLE_ORDER.indexOf(role);
  const source = window.ROLE_RATES_LIVE || (typeof ROLE_RATES !== "undefined" ? ROLE_RATES : null);
  const played = n => { const key = norm(n), row = source && source.champions[RATE_NAMES[key] || key]; return row ? row[index] : 0; };
  return NAMES.filter(n => ((roleShares(n) || [])[index] || 0) >= .25).sort((a, b) => played(b) - played(a));
}

const myRole = () => PROFILE ? PROFILE.role : "mid";
const oppName = () => ROLE_NAMES[myRole()];
const oppAt = () => ROLE_AT[myRole()];
const traitsOfPick = p => CHAMPION_TRAITS[label(p)] || "";
const hasTrait = (p, t) => traitsOfPick(p).includes(t);
const esc = s => s.replace(/[&<>"]/g, c => ({ "&":"&amp;", "<":"&lt;", ">":"&gt;", '"':"&quot;" }[c]));
const NAMES = Object.keys(ROSTER).sort((x, y) => x.localeCompare(y, "fr"));
const champKeys = n => [n].concat(ALIASES[n] || []).map(norm);
// Le tableau écrit à la main ne couvre que le mid et ces quatre champions.
const WRITTEN_PICKS = FIRST_PROFILE.pool;
const matchupOf = name => myRole() === "mid" ? MATCHUPS.find(m => m.n === name) || null : null;


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
  { key:"matchup", label:"Matchup direct", weight:3, source:"Bilans calculés sur de vraies parties dès 30 parties, sinon tableau écrit à la main. Inclut la pression de lane, pour ne pas la compter deux fois." },
  { key:"blind", label:"Sécurité sans adversaire connu", weight:1, source:"Calculé : part des mids du tableau où le pick est à éviter. Ne compte que si le mid est inconnu." },
  { key:"targets", label:"Cibles exploitables", weight:1.5, source:"Règle heuristique : nombre de carrys fragiles adverses." },
  { key:"front", label:"Tanks et bruisers", weight:2, source:"Règle heuristique : nombre de frontlines adverses." },
  { key:"dive", label:"Survie aux plongeurs", weight:2, source:"Règle heuristique : nombre d'assassins et de plongeurs adverses." },
  { key:"control", label:"Engages et contrôles", weight:2, source:"Règle heuristique : contrôles ciblés et engages adverses, comptés une seule fois par pick." },
  { key:"range", label:"Poke et longue portée", weight:1, source:"Règle heuristique : nombre de champions à longue portée adverses." },
  { key:"peel", label:"Protection nécessaire", weight:1.5, source:"Règle heuristique : frontline allié supposé, mobilité adverse." },
  { key:"map", label:"Pression sur la carte", weight:.5, source:"Atout propre au champion. Poids faible, car il ne dépend pas de la draft." }
];
const MID_COUNT = MATCHUPS.length;
// Noms que Riot écrit autrement dans ses données.
const RATE_NAMES = { wukong:"monkeyking", renataglasc:"renata" };

// Bilans calculés sur de vraies parties. Sous ce nombre de parties, on garde le tableau écrit à la main.
const MATCHUP_MIN_GAMES = 30, MATCHUP_SHRINK = 50, MATCHUP_AVOID_GAP = -.03, MATCHUP_FULL_GAP = .05;

// Écart au winrate moyen du pick, ramené vers zéro quand il y a peu de parties.
// Les joueurs suivis jouent souvent ce champion : seul l'écart à leur moyenne a un sens, pas le winrate brut.
function liveMatchup(pick, mid) {
  const live = window.MATCHUPS_LIVE && window.MATCHUPS_LIVE.pool[norm(label(pick))];
  if (!live || !live.games || !mid || myRole() !== "mid") return null;
  const key = norm(mid);
  const row = live.vs[RATE_NAMES[key] || key];
  if (!row || row[1] < MATCHUP_MIN_GAMES) return null;
  const base = live.wins / live.games;
  return { wins: row[0], games: row[1], gap: (row[0] + MATCHUP_SHRINK * base) / (row[1] + MATCHUP_SHRINK) - base };
}

function avoidedPicks(m) {
  const written = (m.avoid || "").split(", ").filter(Boolean);
  return PICKS.filter(p => {
    const live = liveMatchup(p, m.n);
    return live ? live.gap <= MATCHUP_AVOID_GAP : written.includes(p);
  });
}

const avoidCount = pick => MATCHUPS.filter(m => avoidedPicks(m).includes(pick)).length;
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
    const live = liveMatchup(p, x.mid);
    if (live) {
      const percent = Math.round(live.gap * 100);
      const note = Math.max(-4 / 3, Math.min(4 / 3, live.gap / MATCHUP_FULL_GAP));
      return [note, `${percent ? `${percent > 0 ? "+" : ""}${percent} % contre ${x.mid}` : `Sans écart net contre ${x.mid}`} sur ${live.games} parties`];
    }
    const m = matchupOf(x.mid);
    if (!m || !WRITTEN_PICKS.includes(p)) return null;
    if (avoidedPicks(m).includes(p)) return [-4 / 3, `À éviter contre ${x.mid}`];
    if (m.first === p) return [1, `1er choix contre ${x.mid}`];
    if (m.second === p) return [.5, `2e choix contre ${x.mid}`];
    return [0, `Pas d'avis marqué contre ${x.mid}`];
  },
  blind(p, c, x) {
    if (x.mid || !WRITTEN_PICKS.includes(p) || myRole() !== "mid") return null;
    const maxAvoid = Math.max(...PICKS.filter(q => WRITTEN_PICKS.includes(q)).map(avoidCount), ...WRITTEN_PICKS.map(avoidCount));
    if (!maxAvoid) return null;
    const bad = avoidCount(p);
    const v = 1 - 2 * bad / maxAvoid;
    return [v, v > 0 ? `Mid inconnu : peu de mauvais matchups (${bad} sur ${MID_COUNT} dans le tableau)` : `Mid inconnu : risqué, mauvais dans ${bad} matchups sur ${MID_COUNT} du tableau`];
  },
  targets(p, c) {
    const n = c.carries.length;
    if (hasTrait(p, "h")) {
      if (n >= 2) return [2, `${n} cibles fragiles : ${namesOf(c.carries)}`];
      if (n === 1) return [.5, `Une cible fragile : ${c.carries[0]}`];
      if (c.known.length >= 4) return [-2, "Aucune cible fragile à éliminer"];
    }
    if (hasTrait(p, "E") && n >= 2) return [1, `${n} cibles fragiles à attraper : ${namesOf(c.carries)}`];
    if (hasTrait(p, "l") && n >= 2) return [.5, `${n} cibles à abîmer de loin : ${namesOf(c.carries)}`];
    return null;
  },
  front(p, c) {
    const n = c.front.length;
    if (hasTrait(p, "k")) {
      if (n >= 3) return [2, `${n} tanks ou bruisers : ${p === "Kog'Maw" ? "ses dégâts en % des PV" : "ses dégâts contre les tanks"} font la différence`];
      if (n === 2) return [1.5, `2 tanks ou bruisers : ${p === "Kog'Maw" ? "ses dégâts en % des PV" : "ses dégâts contre les tanks"} sont utiles`];
    }
    if (hasTrait(p, "h") && hasTrait(p, "d")) {
      if (n >= 4) return [-1, `${n} frontlines à traverser`];
      if (n === 3) return [-.5, "Beaucoup de frontline à traverser"];
    }
    if (hasTrait(p, "w") && n >= 3) return [-.25, `${n} frontlines : peu de prise sur les tanks`];
    return null;
  },
  dive(p, c) {
    const n = c.divers.length;
    if (n < 2) return null;
    if (hasTrait(p, "i")) return [n >= 3 ? -2 : -1.5, `${n} plongeurs contre un champion immobile : ${namesOf(c.divers)}`];
    if (hasTrait(p, "t")) return [1, `Encaisse leurs ${n} plongeurs`];
    if (hasTrait(p, "c")) return [1, `Peut protéger ses alliés de leurs ${n} plongeurs`];
    return [0, p === "Fizz" ? "Son E lui laisse une sortie contre les plongeurs" : p === "TF" ? "Son étourdissement l'aide à se défendre contre les plongeurs" : "Peut survivre aux plongeurs"];
  },
  control(p, c) {
    if (hasTrait(p, "h") && hasTrait(p, "d") && c.cc.length >= 2) return [-1.5, `${c.cc.length} contrôles ciblés punissent ses plongeons : ${namesOf(c.cc)}`];
    if (hasTrait(p, "e") && (c.cc.length >= 2 || c.engage.length >= 2)) return [.75, p === "TF" ? "Peut jouer loin de leur engage et revenir avec son ultime" : "Peut jouer loin de leur engage et de leurs contrôles"];
    if (hasTrait(p, "i") && c.engage.length >= 3) return [-.5, `${c.engage.length} engages contre un champion lent : ${namesOf(c.engage)}`];
    return null;
  },
  range(p, c) {
    if (c.poke.length < 2) return null;
    if (hasTrait(p, "l")) return [1, `Peut rendre les coups à distance : ${namesOf(c.poke)}`];
    if (hasTrait(p, "o")) return [-1, `Se fait toucher avant de pouvoir frapper : ${namesOf(c.poke)}`];
    return null;
  },
  peel(p, c, x) {
    if (hasTrait(p, "p")) return x.hasFront ? [0, "Suppose un frontline allié pour le protéger"] : [-2, "Ton équipe n'a pas de frontline pour le protéger"];
    if (!hasTrait(p, "q")) return null;
    if (!x.hasFront && c.mobile.length >= 2) return [-2, "Personne pour te protéger de leur mobilité"];
    if (c.mobile.length >= 3) return [-4 / 3, `${c.mobile.length} champions très mobiles : ${namesOf(c.mobile)}`];
    if (x.hasFront && c.divers.length <= 1 && c.mobile.length <= 1 && c.known.length >= 3) return [4 / 3, "Peu de plongeurs et de mobilité, et un frontline devant toi"];
    return null;
  },
  map(p) {
    if (hasTrait(p, "g")) return [1, p === "TF" ? "Son ultime couvre toute la carte" : "Peut peser sur toute la carte"];
    if (hasTrait(p, "r")) return [.5, "Peut roamer une fois sa vague poussée"];
    if (hasTrait(p, "n")) return [-.5, "Peu d'impact hors des combats groupés"];
    return null;
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
// Taux de jeu par rôle (draft-role-rates.js). Le résultat reste une supposition, jamais un rôle confirmé.
const ROLE_ORDER = ["top", "jungle", "mid", "adc", "support"];
const UNKNOWN_SHARE = 0.2, SHARE_FLOOR = 0.02, MIN_MID_SHARE = 0.05;

const LIVE_MIN_GAMES = 20;
const sumOf = list => list ? list.reduce((a, b) => a + b, 0) : 0;

// Part de chaque rôle pour un champion, ou null s'il n'a pas de données.
// Les parties récentes passent d'abord. Les statistiques livrées servent de secours pour les champions rares.
function roleShares(name) {
  const key = norm(name);
  const id = RATE_NAMES[key] || key;
  const live = window.ROLE_RATES_LIVE && window.ROLE_RATES_LIVE.champions[id];
  const rates = sumOf(live) >= LIVE_MIN_GAMES ? live : typeof ROLE_RATES !== "undefined" ? ROLE_RATES.champions[id] : null;
  const total = sumOf(rates);
  return total > 0 ? rates.map(v => v / total) : null;
}

// Cherche la répartition des rôles la plus probable pour toute l'équipe, puis lit qui se retrouve en face du joueur.
// Le nom vient de la première version : « mid » désigne ici l'adversaire direct, quel que soit le rôle.
function guessMid(enemies) {
  const MID_INDEX = ROLE_ORDER.indexOf(myRole());
  const team = enemies.slice(0, ROLE_ORDER.length);
  const shares = team.map(roleShares);
  let bestScore = -Infinity, bestMid = -1;
  const place = (i, used, score, mid) => {
    if (i === team.length) {
      if (score > bestScore) { bestScore = score; bestMid = mid; }
      return;
    }
    ROLE_ORDER.forEach((_, role) => {
      if (used & (1 << role)) return;
      const share = shares[i] ? shares[i][role] : UNKNOWN_SHARE;
      place(i + 1, used | (1 << role), score + Math.log(share + SHARE_FLOOR), role === MID_INDEX ? i : mid);
    });
  };
  place(0, 0, 0, -1);
  if (bestMid < 0 || !shares[bestMid]) return null;
  // Tant que la draft est incomplète, le vrai mid n'est peut-être pas encore choisi. À 5, l'un d'eux y va forcément.
  if (team.length < ROLE_ORDER.length && shares[bestMid][MID_INDEX] < MIN_MID_SHARE) return null;
  return team[bestMid];
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
  const active = p => !ENGINE_PREFS.excluded.includes(p) && PICKS.includes(p);
  const written = myRole() === "mid" ? MATCHUPS
    .filter(m => !own.includes(m.n))
    .map(m => ({
      n: m.n,
      harm: (m.avoid || "").split(", ").filter(p => p && active(p)).length,
      threat: (ROSTER[m.n].match(/[ace]/g) || []).length
    }))
    .filter(x => x.harm > 0)
    .sort((x, y) => y.harm - x.harm || y.threat - x.threat)
    .slice(0, 3)
    .map(x => x.n) : [];
  return written.length ? written : bansFromTraits();
}

// Pool sans tableau écrit : on vise les adversaires directs courants qui gênent le plus ses champions.
function harmFromTraits(name) {
  const t = ROSTER[name] || "";
  return PICKS.filter(p => !ENGINE_PREFS.excluded.includes(p)).filter(p =>
    (hasTrait(p, "i") && t.includes("a")) || (hasTrait(p, "d") && t.includes("c")) || (hasTrait(p, "q") && t.includes("m"))).map(label);
}

function bansFromTraits() {
  const own = PICKS.map(label);
  const index = ROLE_ORDER.indexOf(myRole());
  return NAMES.filter(n => !own.includes(n) && (roleShares(n) || [])[index] >= .3)
    .map(n => ({ n, harm: harmFromTraits(n).length, share: roleShares(n)[index] }))
    .filter(x => x.harm > 0)
    .sort((x, y) => y.harm - x.harm || y.share - x.share)
    .slice(0, 3)
    .map(x => x.n);
}

const banReason = name => {
  const m = matchupOf(name);
  const hit = m && m.avoid ? m.avoid.split(", ").filter(p => !ENGINE_PREFS.excluded.includes(p) && PICKS.includes(p)) : [];
  if (hit.length) return `gêne ${hit.map(label).join(", ")} dans le tableau de matchups`;
  const rough = harmFromTraits(name);
  return rough.length ? `gêne ${rough.join(", ")} d'après leurs profils` : "";
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

function pickStyle(p) {
  if (PICK_STYLES[p]) return PICK_STYLES[p];
  if (hasTrait(p, "c")) return "Protecteur : garde ses alliés en vie.";
  if (hasTrait(p, "t") && hasTrait(p, "E")) return "Tank d'engage : lance les combats et encaisse.";
  if (hasTrait(p, "t")) return "Tank : encaisse en première ligne.";
  if (hasTrait(p, "h")) return "Assassin : cherche les cibles fragiles.";
  if (hasTrait(p, "k")) return "Tueur de tanks : fait fondre la première ligne.";
  if (hasTrait(p, "d")) return "Combattant : plonge dans le combat.";
  if (hasTrait(p, "l")) return "Longue portée : affaiblit avant le combat.";
  return "Polyvalent : s'adapte au déroulé de la partie.";
}

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
  enemies.filter(n => matchupOf(n) && n !== mid).forEach(n => { if (beats(enemies, n, true)) found.push(`si ton adversaire ${oppAt()} est ${n}`); });
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
const threatsFromTraits = (pick, c) => hasTrait(pick, "d") ? c.cc.concat(c.divers) : hasTrait(pick, "q") ? c.divers.concat(c.mobile, c.engage) : c.divers.concat(c.engage);
const threatsFor = (pick, c) => [...new Set((pickThreats[pick] || (x => threatsFromTraits(pick, x)))(c))].slice(0, 2);
function threatText(pick, name, c) {
  if (c.cc.includes(name) && hasTrait(pick, "d")) return "Son contrôle ciblé peut stopper ton plongeon.";
  if (c.divers.includes(name)) return hasTrait(pick, "d") ? "Peut te rattraper quand tu ressors d'un combat." : "Peut te sauter dessus si tu es mal placé.";
  if (c.mobile.includes(name)) return "Sa mobilité lui permet d'éviter tes sorts à distance.";
  if (c.engage.includes(name)) return "Peut démarrer le combat sur toi.";
  return "À surveiller pendant la partie.";
}
const targetsFor = (pick, c) => (hasTrait(pick, "k") ? c.front.concat(c.carries) : c.carries).filter((n, i, l) => l.indexOf(n) === i).slice(0, 2);

function winConditionFromTraits(pick, c) {
  const carry = c.carries[0] || "leur carry";
  if (hasTrait(pick, "c")) return `Protège ton meilleur carry, et fais échouer les engages sur lui.`;
  if (hasTrait(pick, "E")) return `Lance le combat sur ${carry} dès que ton équipe peut suivre.`;
  if (hasTrait(pick, "t")) return "Encaisse en première ligne, et prends l'espace autour des objectifs.";
  if (hasTrait(pick, "h")) return `Prends ton avantage, puis élimine ${carry} avant l'objectif.`;
  if (hasTrait(pick, "k")) return c.front.length >= 2 ? `Fais fondre ${c.front[0]}, et reste en vie jusqu'à la fin du combat.` : "Frappe la cible la plus proche, et reste en vie jusqu'à la fin du combat.";
  if (hasTrait(pick, "l")) return "Affaiblis-les à distance avant l'objectif, sans te faire attraper.";
  return "Prends ton avantage en lane, puis joue chaque objectif avec ton équipe.";
}

function winCondition(pick, c) {
  const carry = c.carries[0];
  if (!WRITTEN_PICKS.includes(pick)) return winConditionFromTraits(pick, c);
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
  const ranged = hasTrait(pick, "i");
  const role = myRole();
  const roamer = role === "jungle" || role === "support";

  const lane = [
    !WRITTEN_PICKS.includes(pick) && mid && midDive && `${mid} peut te sauter dessus : garde tes distances et ton sort de défense.`,
    !WRITTEN_PICKS.includes(pick) && mid && midCc && !midDive && `${mid} a un contrôle ciblé : attends de le voir partir avant d'engager.`,
    !WRITTEN_PICKS.includes(pick) && mid && !midDive && !midCc && `Contre ${mid}, observe ses sorts importants avant d'engager un échange.`,
    midBad && `${mid} est un matchup difficile : farme prudemment et n'échange qu'après avoir vu partir un de ses sorts importants.`,
    pick === "Fizz" && midCc && `${mid} a un contrôle ciblé : attends de le voir partir avant de lancer ton all-in.`,
    pick === "Fizz" && !midBad && mid && `Contre ${mid}, cherche des échanges courts, puis ressors avec ton E.`,
    ranged && midDive && `${mid} peut te sauter dessus : garde tes distances et ne gaspille pas ton sort de contrôle ou de ralentissement.`,
    pick === "Xerath" && !midDive && mid && `Touche ${mid} de loin dès qu'il vient au farm, sans dépasser la moitié de la lane.`,
    pick === "Kog'Maw" && !midDive && mid && `Contre ${mid}, farme à portée maximale et échange seulement quand ta vague te couvre.`,
    pick === "TF" && mid && `Contre ${mid}, garde ta carte dorée pour répondre à un engage ou préparer un gank.`,
    !roamer && jungler && `${jungler} peut venir en gank : place une ward avant de pousser ta vague.`,
    !mid && `Ton adversaire ${oppAt()} n'est pas connu : joue d'abord la sécurité et observe son style.`,
    role === "jungle" ? "Prépare ton chemin vers la lane qui peut le mieux profiter de ton aide." : role === "support" ? "Garde la vision de ton côté avant de chercher un engage." : "Pousse ta vague avant de bouger, pour ne pas perdre de farm."
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

// Chargé en dernier : le moteur a besoin de tous ses tableaux pour lire le profil.
loadProfile();
