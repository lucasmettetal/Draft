// DRAFT : données et moteur de recommandation. Fichier chargé par la page et par draft-tests.html.
const MATCHUPS = [
  { n:"Ahri", t:"Mage", first:"Fizz", second:"TF", avoid:"Kog'Maw" },
  { n:"Anivia", t:"Mage", first:"Fizz", second:"Xerath", avoid:"Kog'Maw" },
  { n:"Annie", t:"Mage", first:"TF", second:"Xerath", avoid:"Fizz" },
  { n:"Aurelion Sol", t:"Mage", a:["asol"], first:"Fizz", second:"TF", avoid:"Kog'Maw" },
  { n:"Azir", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Cassiopeia", t:"Mage", a:["cass","cassio"], first:"Xerath", second:"TF", avoid:"Fizz" },
  { n:"Hwei", t:"Mage", first:"Fizz", second:"TF" },
  { n:"Lissandra", t:"Mage", a:["liss"], first:"TF", second:"Xerath", avoid:"Fizz" },
  { n:"Lux", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Malzahar", t:"Mage", a:["malz"], first:"Kog'Maw", second:"TF", avoid:"Fizz" },
  { n:"Mel", t:"Mage", first:"Fizz", second:"TF", avoid:"Xerath" },
  { n:"Orianna", t:"Mage", a:["ori"], first:"Fizz", second:"Xerath" },
  { n:"Swain", t:"Mage", first:"Kog'Maw", second:"Xerath", avoid:"Fizz" },
  { n:"Syndra", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Twisted Fate", t:"Mage", a:["tf"], first:"Fizz", second:"Xerath" },
  { n:"Veigar", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Vel'Koz", t:"Mage", a:["velkoz","vel"], first:"Fizz", second:"TF" },
  { n:"Vex", t:"Mage", first:"TF", second:"Xerath", avoid:"Fizz" },
  { n:"Viktor", t:"Mage", first:"Fizz", second:"Xerath" },
  { n:"Xerath", t:"Mage", first:"Fizz", second:"TF" },
  { n:"Ziggs", t:"Mage", first:"Fizz", second:"TF" },
  { n:"Zoe", t:"Mage", first:"Fizz", second:"Xerath" },
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
  { n:"Diana", t:"Combattant", first:"Kog'Maw", second:"TF", avoid:"Xerath" },
  { n:"Galio", t:"Tank", first:"Kog'Maw", second:"TF", avoid:"Fizz" },
  { n:"Irelia", t:"Combattant", first:"TF", second:"Kog'Maw", avoid:"Xerath" },
  { n:"Malphite", t:"Tank", a:["malph"], first:"Kog'Maw", second:"Xerath", avoid:"Fizz" },
  { n:"Pantheon", t:"Combattant", a:["panth"], first:"TF", second:"Xerath", avoid:"Kog'Maw" },
  { n:"Sion", t:"Tank", first:"Kog'Maw", second:"Xerath", avoid:"Fizz" },
  { n:"Sylas", t:"Combattant", first:"TF", second:"Kog'Maw", avoid:"Fizz" },
  { n:"Yasuo", t:"Combattant", a:["yas"], first:"Fizz", second:"Kog'Maw", avoid:"TF" },
  { n:"Yone", t:"Combattant", first:"Kog'Maw", second:"TF", avoid:"Fizz" }
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
const ROLE_NAMES = { top:I18N.t("role.name.top"), jungle:I18N.t("role.name.jungle"), mid:I18N.t("role.name.mid"), adc:I18N.t("role.name.adc"), support:I18N.t("role.name.support") };
const ROLE_AT = { top:I18N.t("role.at.top"), jungle:I18N.t("role.at.jungle"), mid:I18N.t("role.at.mid"), adc:I18N.t("role.at.adc"), support:I18N.t("role.at.support") };
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
  { key:"matchup", label:I18N.t("engine.criterion.matchup"), weight:3, source:I18N.t("engine.criterion.matchup.source") },
  { key:"blind", label:I18N.t("engine.criterion.blind"), weight:1, source:I18N.t("engine.criterion.blind.source") },
  { key:"targets", label:I18N.t("engine.criterion.targets"), weight:1.5, source:I18N.t("engine.criterion.targets.source") },
  { key:"front", label:I18N.t("engine.criterion.front"), weight:2, source:I18N.t("engine.criterion.front.source") },
  { key:"dive", label:I18N.t("engine.criterion.dive"), weight:2, source:I18N.t("engine.criterion.dive.source") },
  { key:"control", label:I18N.t("engine.criterion.control"), weight:2, source:I18N.t("engine.criterion.control.source") },
  { key:"range", label:I18N.t("engine.criterion.range"), weight:1, source:I18N.t("engine.criterion.range.source") },
  { key:"peel", label:I18N.t("engine.criterion.peel"), weight:1.5, source:I18N.t("engine.criterion.peel.source") },
  { key:"map", label:I18N.t("engine.criterion.map"), weight:.5, source:I18N.t("engine.criterion.map.source") }
];
const MID_COUNT = MATCHUPS.length;
// Noms que Riot écrit autrement dans ses données.
const RATE_NAMES = { wukong:"monkeyking", renataglasc:"renata" };

// Bilans calculés sur de vraies parties. Sous ce nombre de parties, on garde le tableau écrit à la main.
const MATCHUP_MIN_GAMES = 30, MATCHUP_SHRINK = 50, MATCHUP_AVOID_GAP = -.03, MATCHUP_FULL_GAP = .05;

const riotKey = name => { const key = norm(name); return RATE_NAMES[key] || key; };
// Un fichier sans rôle date d'avant les profils : il ne concerne que le mid.
const liveRoleOk = () => !!window.MATCHUPS_LIVE && (window.MATCHUPS_LIVE.role || "mid") === myRole();

// Écart au winrate moyen du pick, ramené vers zéro quand il y a peu de parties.
// Les joueurs suivis jouent souvent ce champion : seul l'écart à leur moyenne a un sens, pas le winrate brut.
function liveMatchup(pick, mid) {
  if (!liveRoleOk() || !mid) return null;
  const key = riotKey(label(pick)), opponent = riotKey(mid);
  // Le patch actuel compte plein, les précédents de moins en moins, et n'importe quelle source de bilans convient.
  let wins = 0, games = 0, rowWins = 0, rowGames = 0, seenWins = 0, seenGames = 0, recent = false;
  DATA_PROVIDERS.forEach(provider => {
    if (provider.matchupBuckets) provider.matchupBuckets().forEach(bucket => {
      const live = bucket.pool && bucket.pool[key];
      if (!live || !live.games) return;
      const age = ageOf(bucket.patch, bucket.collected).age;
      const weight = bucket.patch || bucket.collected ? ageWeight(age) : 1;
      wins += live.wins * weight;
      games += live.games * weight;
      const row = live.vs && live.vs[opponent];
      if (!row) return;
      rowWins += row[0] * weight;
      rowGames += row[1] * weight;
      seenWins += row[0];
      seenGames += row[1];
      if (age === 0 || age === null) recent = true;
    });
  });
  if (!games || rowGames < MATCHUP_MIN_GAMES) return null;
  const base = wins / games;
  return { wins:seenWins, games:seenGames, effective:rowGames, recent, gap:(rowWins + MATCHUP_SHRINK * base) / (rowGames + MATCHUP_SHRINK) - base };
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

// Renvoie [note, explication], ou null si le critère n'a rien à dire pour ce pick. Le matchup ajoute un troisième élément :
// live (bilan de vraies parties) et short (la même explication sans « contre {adversaire} », pour nommer une hypothèse).
const RULES = {
  matchup(p, c, x) {
    const live = liveMatchup(p, x.mid);
    if (live) {
      const percent = Math.round(live.gap * 100);
      const note = Math.max(-4 / 3, Math.min(4 / 3, live.gap / MATCHUP_FULL_GAP));
      const games = I18N.t(live.recent ? "engine.matchup.games" : "engine.matchup.gamesOld", { count: live.games });
      const gap = percent ? I18N.t("engine.matchup.gap", { gap: `${percent > 0 ? "+" : ""}${percent}` }) : I18N.t("engine.matchup.noGap");
      return [note, I18N.t("engine.matchup.live", { gap, champion: x.mid, games }), { live:true, short:I18N.t("engine.matchup.liveShort", { gap, games }) }];
    }
    const m = matchupOf(x.mid);
    if (!m || !WRITTEN_PICKS.includes(p)) return null;
    if (avoidedPicks(m).includes(p)) return [-4 / 3, I18N.t("engine.matchup.avoid", { champion: x.mid }), { short:I18N.t("engine.matchup.avoidShort") }];
    if (m.first === p) return [1, I18N.t("engine.matchup.first", { champion: x.mid }), { short:I18N.t("engine.matchup.firstShort") }];
    if (m.second === p) return [.5, I18N.t("engine.matchup.second", { champion: x.mid }), { short:I18N.t("engine.matchup.secondShort") }];
    return [0, I18N.t("engine.matchup.neutral", { champion: x.mid }), { short:I18N.t("engine.matchup.neutralShort") }];
  },
  blind(p, c, x) {
    if (x.mid || !WRITTEN_PICKS.includes(p) || myRole() !== "mid") return null;
    const maxAvoid = Math.max(...PICKS.filter(q => WRITTEN_PICKS.includes(q)).map(avoidCount), ...WRITTEN_PICKS.map(avoidCount));
    if (!maxAvoid) return null;
    const bad = avoidCount(p);
    const v = 1 - 2 * bad / maxAvoid;
    return [v, I18N.tp(v > 0 ? "engine.blind.safe" : "engine.blind.risky", bad, { total: MID_COUNT })];
  },
  targets(p, c) {
    const n = c.carries.length;
    if (hasTrait(p, "h")) {
      if (n >= 2) return [2, I18N.t("engine.targets.many", { count: n, champions: namesOf(c.carries) })];
      if (n === 1) return [.5, I18N.t("engine.targets.one", { champion: c.carries[0] })];
      if (c.known.length >= 4) return [-2, I18N.t("engine.targets.none")];
    }
    if (hasTrait(p, "E") && n >= 2) return [1, I18N.t("engine.targets.catch", { count: n, champions: namesOf(c.carries) })];
    if (hasTrait(p, "l") && n >= 2) return [.5, I18N.t("engine.targets.poke", { count: n, champions: namesOf(c.carries) })];
    return null;
  },
  front(p, c) {
    const n = c.front.length;
    if (hasTrait(p, "k")) {
      if (n >= 3) return [2, I18N.t(p === "Kog'Maw" ? "engine.front.killerManyKog" : "engine.front.killerMany", { count: n })];
      if (n === 2) return [1.5, I18N.t(p === "Kog'Maw" ? "engine.front.killerTwoKog" : "engine.front.killerTwo")];
    }
    if (hasTrait(p, "h") && hasTrait(p, "d")) {
      if (n >= 4) return [-1, I18N.t("engine.front.cross", { count: n })];
      if (n === 3) return [-.5, I18N.t("engine.front.crossMuch")];
    }
    if (hasTrait(p, "w") && n >= 3) return [-.25, I18N.t("engine.front.weak", { count: n })];
    return null;
  },
  dive(p, c) {
    const n = c.divers.length;
    if (n < 2) return null;
    if (hasTrait(p, "i")) return [n >= 3 ? -2 : -1.5, I18N.t("engine.dive.immobile", { count: n, champions: namesOf(c.divers) })];
    if (hasTrait(p, "t")) return [1, I18N.t("engine.dive.tank", { count: n })];
    if (hasTrait(p, "c")) return [1, I18N.t("engine.dive.peel", { count: n })];
    return [0, p === "Fizz" ? I18N.t("engine.dive.fizz") : p === "TF" ? I18N.t("engine.dive.tf") : I18N.t("engine.dive.survive")];
  },
  control(p, c) {
    if (hasTrait(p, "h") && hasTrait(p, "d") && c.cc.length >= 2) return [-1.5, I18N.t("engine.control.allIn", { count: c.cc.length, champions: namesOf(c.cc) })];
    if (hasTrait(p, "e") && (c.cc.length >= 2 || c.engage.length >= 2)) return [.75, p === "TF" ? I18N.t("engine.control.tf") : I18N.t("engine.control.escape")];
    if (hasTrait(p, "i") && c.engage.length >= 3) return [-.5, I18N.t("engine.control.immobile", { count: c.engage.length, champions: namesOf(c.engage) })];
    return null;
  },
  range(p, c) {
    if (c.poke.length < 2) return null;
    if (hasTrait(p, "l")) return [1, I18N.t("engine.range.answer", { champions: namesOf(c.poke) })];
    if (hasTrait(p, "o")) return [-1, I18N.t("engine.range.suffer", { champions: namesOf(c.poke) })];
    return null;
  },
  peel(p, c, x) {
    if (hasTrait(p, "p")) return x.hasFront ? [0, I18N.t("engine.peel.front")] : [-2, I18N.t("engine.peel.noFront")];
    if (!hasTrait(p, "q")) return null;
    if (!x.hasFront && c.mobile.length >= 2) return [-2, I18N.t("engine.peel.noFrontMobile")];
    if (c.mobile.length >= 3) return [-4 / 3, I18N.t("engine.peel.mobile", { count: c.mobile.length, champions: namesOf(c.mobile) })];
    if (x.hasFront && c.divers.length <= 1 && c.mobile.length <= 1 && c.known.length >= 3) return [4 / 3, I18N.t("engine.peel.safe")];
    return null;
  },
  map(p) {
    if (hasTrait(p, "g")) return [1, p === "TF" ? I18N.t("engine.map.tf") : I18N.t("engine.map.global")];
    if (hasTrait(p, "r")) return [.5, I18N.t("engine.map.roam")];
    if (hasTrait(p, "n")) return [-.5, I18N.t("engine.map.weak")];
    return null;
  }
};

function evaluatePick(pick, compo, ctx) {
  const criteria = CRITERIA.map(def => {
    const rated = RULES[def.key](pick, compo, ctx);
    if (!rated) return null;
    const criterion = { key:def.key, label:def.label, v:rated[0], impact:rated[0] * def.weight, text:rated[1] };
    if (rated[2] && rated[2].live) criterion.live = true;
    if (rated[2] && rated[2].short !== undefined) criterion.short = rated[2].short;
    return criterion;
  }).filter(Boolean);
  return {
    pick,
    score: criteria.reduce((sum, c) => sum + c.impact, 0),
    criteria,
    why: criteria.filter(c => c.impact !== 0).map(c => ({ key:c.key, v:c.impact, text:c.text }))
  };
}

// « Si c'est Yasuo : 1er choix ». Le nom de l'hypothèse remplace « contre Yasuo » : la règle fournit sa forme courte (short).
// Texte seulement : aucune note ne dépend de cette fonction.
function scenarioText(mid, c) {
  const text = c.text;
  const short = !mid ? text : c.short !== undefined ? c.short : text.replace(I18N.t("engine.scenario.against", { champion: mid }), "");
  return I18N.t(mid ? "engine.scenario.if" : "engine.scenario.notYet", { champion: mid, text: `${short.charAt(0).toLowerCase()}${short.slice(1)}` });
}
const joinScenarioTexts = parts => parts.map((x, i) => i ? x.text.charAt(0).toLowerCase() + x.text.slice(1) : x.text).join(" ; ");

// Plusieurs adversaires possibles : mêmes règles, pondérées par la probabilité de chaque hypothèse.
function evaluateScenarios(pick, compo, hasFront, scenarios) {
  const each = scenarios.map(s => ({ s, entry: evaluatePick(pick, compo, { mid:s.mid, hasFront }) }));
  const criteria = CRITERIA.map(def => {
    const found = each.map(x => ({ s:x.s, c:x.entry.criteria.find(c => c.key === def.key) })).filter(x => x.c);
    if (!found.length) return null;
    const v = found.reduce((sum, x) => sum + x.s.p * x.c.v, 0);
    // Textes seulement : quand les hypothèses ne disent pas la même chose, chacune est nommée, avec le signe de sa propre note.
    const distinct = found.filter((x, i) => found.findIndex(y => y.c.text === x.c.text) === i).slice(0, 2);
    const parts = distinct.map(x => ({ mid:x.s.mid, v:x.c.v, text:scenarioText(x.s.mid, x.c) }));
    const criterion = { key:def.key, label:def.label, v, impact:v * def.weight, text:parts.length > 1 ? joinScenarioTexts(parts) : found[0].c.text, parts:parts.length > 1 ? parts : null };
    // Bilan de vraies parties dans ce qui est affiché : les hypothèses nommées, ou la seule explication.
    if (parts.length > 1 ? distinct.some(x => x.c.live) : found[0].c.live) criterion.live = true;
    return criterion;
  }).filter(Boolean);
  return {
    pick, criteria,
    score: criteria.reduce((sum, c) => sum + c.impact, 0),
    why: criteria.filter(c => c.impact !== 0).map(c => ({ key:c.key, v:c.impact, text:c.text, parts:c.parts })),
    byScenario: each.map(x => ({ mid:x.s.mid, p:x.s.p, score:x.entry.score }))
  };
}

function analyze(enemies, mid, hasFront, scenarios) {
  const compo = compoOf(enemies);
  const ctx = { mid, hasFront };
  const several = scenarios && scenarios.length > 1;
  // Même score : le moins pénalisé passe devant, puis le favori choisi par le joueur. Restent ex æquo ceux que rien ne sépare :
  // ils sont marqués comme tels, et rangés par nom pour que l'ordre du pool n'ait aucun effet. L'alphabet n'est pas un avantage.
  const worst = p => tieKey(Math.min(0, ...p.why.map(w => w.v)));
  const ranking = availablePicks(enemies).map(p => several ? evaluateScenarios(p, compo, hasFront, scenarios) : evaluatePick(p, compo, ctx))
    .sort((a, b) => tieKey(b.score) - tieKey(a.score) || worst(b) - worst(a) || (b.pick === ENGINE_PREFS.favorite) - (a.pick === ENGINE_PREFS.favorite) || label(a.pick).localeCompare(label(b.pick), "fr"));
  return { ranking, ties: markTies(ranking), unknown: enemies.filter(n => PROVISIONAL.includes(n)), counts: compo };
}

// Égalité stratégique : même score. À ne pas confondre avec holdPick, qui ne fait que stabiliser le nom affiché pendant la draft.
const tieKey = v => Math.round(v * 1e6);

// Ajoute tiedWith à chaque pick et renvoie les groupes ex æquo, du meilleur au moins bon.
function markTies(ranking) {
  const groups = [];
  ranking.forEach(p => {
    const last = groups[groups.length - 1];
    if (last && last.key === tieKey(p.score)) last.picks.push(p.pick);
    else groups.push({ key:tieKey(p.score), score:p.score, picks:[p.pick] });
  });
  ranking.forEach(p => { p.tiedWith = groups.find(g => g.key === tieKey(p.score)).picks.filter(x => x !== p.pick); });
  return groups.filter(g => g.picks.length > 1).map(g => ({ score:g.score, picks:g.picks }));
}

function availablePicks(enemies) {
  return PICKS.filter(p => !enemies.includes(label(p)) && !ENGINE_PREFS.excluded.includes(p));
}

// ---------- Mid probable ----------
// Taux de jeu par rôle (draft-role-rates.js). Le résultat reste une supposition, jamais un rôle confirmé.
const ROLE_ORDER = ["top", "jungle", "mid", "adc", "support"];
const MIN_MID_SHARE = .2;

// Part de chaque rôle pour un champion, ou null s'il n'a aucune donnée (draft-data.js).
function roleShares(name) {
  const evidence = roleEvidence(name);
  return evidence.known ? evidence.shares : null;
}

// L'adversaire direct le plus probable. Le nom vient de la première version : quel que soit le rôle du joueur.
// Tant que la draft est incomplète, le vrai adversaire n'est peut-être pas encore choisi.
function guessMid(enemies, fixed) {
  const team = enemies.slice(0, ROLE_ORDER.length);
  const best = opponentBelief(team, fixed).candidates[0];
  if (!best || !best.known) return null;
  if (team.length < ROLE_ORDER.length && best.share < MIN_MID_SHARE) return null;
  return best.name;
}

// Le nom affiché ne change que si un autre pick mène d'au moins HOLD_GAP points.
function holdPick(ranking, held) {
  if (!ranking.length) return null;
  const heldEntry = ranking.find(p => p.pick === held);
  if (!heldEntry) return ranking[0].pick;
  return ranking[0].score - heldEntry.score >= HOLD_GAP ? ranking[0].pick : held;
}

function suggestBans() {
  const measured = bansFromLive();
  if (measured.length) return measured;
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

// Dès que de vrais bilans existent, ils désignent les adversaires qui gênent le plus le pool.
function bansFromLive() {
  if (!liveRoleOk()) return [];
  const own = PICKS.map(label);
  const active = PICKS.filter(p => !ENGINE_PREFS.excluded.includes(p));
  return NAMES.filter(n => !own.includes(n)).map(n => {
    const bad = active.map(p => liveMatchup(p, n)).filter(x => x && x.gap <= MATCHUP_AVOID_GAP);
    return { n, harm: bad.length, worst: Math.min(0, ...bad.map(x => x.gap)) };
  }).filter(x => x.harm > 0).sort((x, y) => y.harm - x.harm || x.worst - y.worst).slice(0, 3).map(x => x.n);
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
  const measured = PICKS.filter(p => !ENGINE_PREFS.excluded.includes(p) && (liveMatchup(p, name) || { gap:0 }).gap <= MATCHUP_AVOID_GAP);
  if (measured.length) return I18N.t("engine.ban.measured", { champions: measured.map(label).join(", ") });
  const m = matchupOf(name);
  const hit = m && m.avoid ? m.avoid.split(", ").filter(p => !ENGINE_PREFS.excluded.includes(p) && PICKS.includes(p)) : [];
  if (hit.length) return I18N.t("engine.ban.table", { champions: hit.map(label).join(", ") });
  const rough = harmFromTraits(name);
  return rough.length ? I18N.t("engine.ban.profiles", { champions: rough.join(", ") }) : "";
};

// Une raison pesée sur plusieurs hypothèses ne garde que celles du bon signe : jamais « à éviter » parmi les raisons positives.
const signedText = (w, sign) => w.parts ? joinScenarioTexts(w.parts.filter(x => Math.sign(x.v) === sign)) : w.text;

function positiveReasons(entry) {
  return entry.why.filter(w => w.v > 0).sort((x, y) => y.v - x.v).map(w => signedText(w, 1)).filter(Boolean).slice(0, 2);
}

function mainRisk(entry) {
  return entry.why.filter(w => w.v < 0).sort((x, y) => x.v - y.v).map(w => signedText(w, -1)).filter(Boolean)[0] || "";
}

// Pourquoi, à score égal, un pick est rangé avant l'autre. Lit le tri d'analyze (risque, puis favori, puis alphabet) sans rien changer.
function tieCause(ranking, picks) {
  const entries = ranking.filter(p => picks.includes(p.pick));
  if (entries.length < 2) return null;
  const [a, b] = entries;
  const worstOf = e => e.why.filter(w => w.v < 0).sort((x, y) => x.v - y.v)[0] || null;
  const risk = e => tieKey(worstOf(e) ? worstOf(e).v : 0);
  const wa = worstOf(a), wb = worstOf(b);
  const name = w => w ? (CRITERIA.find(c => c.key === w.key) || {}).label : null;
  if (risk(a) !== risk(b)) return { kind:"risk", first:a.pick, second:b.pick, firstRisk:name(wa), secondRisk:name(wb) };
  // La 1re paire ne se départage pas par le risque, mais la suite du groupe, si : l'ordre mêle plusieurs règles.
  const mixed = entries.some(e => risk(e) !== risk(a)) || (a.pick !== ENGINE_PREFS.favorite && entries.some(e => e.pick === ENGINE_PREFS.favorite));
  if (a.pick === ENGINE_PREFS.favorite) return { kind:"favorite", first:a.pick, second:b.pick, mixed };
  return { kind:mixed ? "mixed" : "alpha", first:a.pick, second:b.pick };
}

// ---------- Comparaison ----------
const PICK_STYLES = {
  "Fizz": I18N.t("engine.style.fizz"),
  "TF": I18N.t("engine.style.tf"),
  "Kog'Maw": I18N.t("engine.style.kogmaw"),
  "Xerath": I18N.t("engine.style.xerath")
};

function pickStyle(p) {
  if (PICK_STYLES[p]) return PICK_STYLES[p];
  if (hasTrait(p, "c")) return I18N.t("engine.style.protector");
  if (hasTrait(p, "t") && hasTrait(p, "E")) return I18N.t("engine.style.engageTank");
  if (hasTrait(p, "t")) return I18N.t("engine.style.tank");
  if (hasTrait(p, "h")) return I18N.t("engine.style.assassin");
  if (hasTrait(p, "k")) return I18N.t("engine.style.antiTank");
  if (hasTrait(p, "d")) return I18N.t("engine.style.fighter");
  if (hasTrait(p, "l")) return I18N.t("engine.style.longRange");
  return I18N.t("engine.style.flexible");
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

const WHAT_IF_ADDS = [["engine.whatIf.tank", "Ornn"], ["engine.whatIf.dive", "Zed"], ["engine.whatIf.cc", "Leona"], ["engine.whatIf.carry", "Jinx"], ["engine.whatIf.poke", "Ziggs"]];

// Recalcule la draft avec le même moteur pour trouver quand l'autre pick passerait devant.
const hasMatchupData = name => !!matchupOf(name) || PICKS.some(p => liveMatchup(p, name));

function whenBetter(target, other, enemies, mid) {
  const beats = (list, m, front) => {
    const r = analyze(list, m, front).ranking;
    const t = r.find(p => p.pick === target);
    const o = r.find(p => p.pick === other);
    return t && o && t.score > o.score;
  };
  const found = [];
  if (beats(enemies, mid, false)) found.push(I18N.t("engine.whenBetter.noFront"));
  enemies.filter(n => hasMatchupData(n) && n !== mid).forEach(n => { if (beats(enemies, n, true)) found.push(I18N.t("engine.whenBetter.opponent", { roleAt: oppAt(), champion: n })); });
  if (enemies.length < 5) WHAT_IF_ADDS.forEach(([key, champ]) => { if (!enemies.includes(champ) && beats(enemies.concat(champ), mid, true)) found.push(I18N.t("engine.whenBetter.with", { what: I18N.t(key) })); });
  return found.slice(0, 3);
}

function midSwapEffect(enemies, mid, shown, hasFront = true, plausible) {
  for (const n of plausible || enemies) {
    if (n === mid || !hasMatchupData(n)) continue;
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
    if (!next) return I18N.t("engine.position.alone");
    const lead = leadCriterion(entry, next);
    return lead ? I18N.t("engine.position.firstLead", { champion: label(next.pick), criterion: lead.label }) : I18N.t("engine.position.firstTie", { champion: label(next.pick) });
  }
  const lead = leadCriterion(ranking[0], entry);
  return lead ? I18N.t("engine.position.behind", { champion: label(ranking[0].pick), criterion: lead.label }) : I18N.t("engine.position.tie", { champion: label(ranking[0].pick) });
};

function compoReading(c) {
  const strong = [];
  const weak = [];
  if (c.front.length >= 2) strong.push(I18N.t("engine.compo.front"));
  if (c.engage.length >= 2) strong.push(I18N.t("engine.compo.engage"));
  if (c.cc.length >= 2) strong.push(I18N.t("engine.compo.cc"));
  if (c.divers.length >= 2) strong.push(I18N.t("engine.compo.dive"));
  if (c.poke.length >= 2) strong.push(I18N.t("engine.compo.poke"));
  if (c.carries.length >= 2) weak.push(I18N.t("engine.compo.carries"));
  if (c.known.length >= 4 && !c.front.length) weak.push(I18N.t("engine.compo.noFront"));
  if (c.known.length >= 4 && !c.engage.length) weak.push(I18N.t("engine.compo.noEngage"));
  if (c.known.length >= 4 && !c.cc.length) weak.push(I18N.t("engine.compo.noCc"));
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
  if (c.cc.includes(name) && hasTrait(pick, "d")) return I18N.t("engine.threat.ccAllIn");
  if (c.divers.includes(name)) return hasTrait(pick, "d") ? I18N.t("engine.threat.catchOut") : I18N.t("engine.threat.jump");
  if (c.mobile.includes(name)) return I18N.t("engine.threat.mobile");
  if (c.engage.includes(name)) return I18N.t("engine.threat.engage");
  return I18N.t("engine.threat.watch");
}
const targetsFor = (pick, c) => (hasTrait(pick, "k") ? c.front.concat(c.carries) : c.carries).filter((n, i, l) => l.indexOf(n) === i).slice(0, 2);

function winConditionFromTraits(pick, c) {
  const carry = c.carries[0] || I18N.t("engine.win.theirCarry");
  if (hasTrait(pick, "c")) return I18N.t("engine.win.protect");
  if (hasTrait(pick, "E")) return I18N.t("engine.win.engage", { champion: carry });
  if (hasTrait(pick, "t")) return I18N.t("engine.win.tank");
  if (hasTrait(pick, "h")) return I18N.t("engine.win.assassin", { champion: carry });
  if (hasTrait(pick, "k")) return c.front.length >= 2 ? I18N.t("engine.win.antiTank", { champion: c.front[0] }) : I18N.t("engine.win.antiTankNear");
  if (hasTrait(pick, "l")) return I18N.t("engine.win.longRange");
  return I18N.t("engine.win.default");
}

function winCondition(pick, c) {
  const carry = c.carries[0];
  if (!WRITTEN_PICKS.includes(pick)) return winConditionFromTraits(pick, c);
  return {
    "Fizz": carry ? I18N.t("engine.win.fizz", { champion: carry }) : I18N.t("engine.win.fizzNoCarry"),
    "TF": I18N.t("engine.win.tf"),
    "Kog'Maw": c.front.length >= 2 ? I18N.t("engine.win.kogmaw", { champion: c.front[0] }) : I18N.t("engine.win.kogmawNoTank"),
    "Xerath": I18N.t("engine.win.xerath")
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
    !WRITTEN_PICKS.includes(pick) && mid && midDive && I18N.t("engine.plan.lane.dive", { champion: mid }),
    !WRITTEN_PICKS.includes(pick) && mid && midCc && !midDive && I18N.t("engine.plan.lane.cc", { champion: mid }),
    !WRITTEN_PICKS.includes(pick) && mid && !midDive && !midCc && I18N.t("engine.plan.lane.observe", { champion: mid }),
    midBad && I18N.t("engine.plan.lane.hard", { champion: mid }),
    pick === "Fizz" && midCc && I18N.t("engine.plan.lane.fizzCc", { champion: mid }),
    pick === "Fizz" && !midBad && mid && I18N.t("engine.plan.lane.fizz", { champion: mid }),
    ranged && midDive && I18N.t("engine.plan.lane.rangedDive", { champion: mid }),
    pick === "Xerath" && !midDive && mid && I18N.t("engine.plan.lane.xerath", { champion: mid }),
    pick === "Kog'Maw" && !midDive && mid && I18N.t("engine.plan.lane.kogmaw", { champion: mid }),
    pick === "TF" && mid && I18N.t("engine.plan.lane.tf", { champion: mid }),
    !roamer && jungler && I18N.t("engine.plan.lane.gank", { champion: jungler }),
    !mid && I18N.t("engine.plan.lane.unknown", { roleAt: oppAt() }),
    role === "jungle" ? I18N.t("engine.plan.lane.jungle") : role === "support" ? I18N.t("engine.plan.lane.support") : I18N.t("engine.plan.lane.push")
  ];
  const midGame = [
    pick === "TF" && carry && I18N.t("engine.plan.mid.tf", { champion: carry }),
    pick === "Fizz" && carry && I18N.t("engine.plan.mid.fizz", { champion: carry }),
    pick === "Kog'Maw" && I18N.t("engine.plan.mid.kogmaw"),
    pick === "Xerath" && carry && I18N.t("engine.plan.mid.xerath", { champion: carry }),
    diver && I18N.t("engine.plan.mid.diver", { champion: diver }),
    engager && I18N.t("engine.plan.mid.engager", { champion: engager }),
    tank && pick !== "Kog'Maw" && I18N.t("engine.plan.mid.tank", { champion: tank }),
    I18N.t("engine.plan.mid.objective")
  ];
  const fight = [
    pick === "Fizz" && cc && I18N.t("engine.plan.fight.fizzCc", { champion: cc }),
    pick === "Fizz" && carry && I18N.t("engine.plan.fight.fizzTarget", { champion: carry }),
    pick === "Fizz" && !carry && I18N.t("engine.plan.fight.fizzNoCarry"),
    pick === "TF" && carry && I18N.t("engine.plan.fight.tfCarry", { champion: carry }),
    pick === "TF" && engager && I18N.t("engine.plan.fight.tfWait", { champion: engager }),
    ranged && diver && I18N.t("engine.plan.fight.rangedDive", { champion: diver }),
    pick === "Kog'Maw" && tank && I18N.t("engine.plan.fight.kogmaw", { champion: tank }),
    pick === "Xerath" && carry && I18N.t("engine.plan.fight.xerath", { champion: carry }),
    ranged && engager && I18N.t("engine.plan.fight.rangedEngage", { champion: engager }),
    I18N.t("engine.plan.fight.vision")
  ];
  const three = list => [...new Set(list.filter(Boolean))].slice(0, 3);
  return { lane: three(lane), mid: three(midGame), fight: three(fight) };
}

const matchupLink = (pick, mid) => `https://lolalytics.com/lol/${norm(label(pick))}/vs/${norm(mid)}/build/`;

// Chargé en dernier : le moteur a besoin de tous ses tableaux pour lire le profil.
loadProfile();
