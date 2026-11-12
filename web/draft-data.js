// Couche de données : d'où vient chaque chiffre, à quel point s'y fier, et ce qu'on en déduit sur les rôles.
// Aucune mesure n'est fabriquée : une donnée absente reste absente et fait baisser la confiance.
// Ajouter une source : une entrée dans DATA_PROVIDERS, sans toucher au moteur stratégique.

const PATCHES_PER_SEASON = 24, PATCH_DAYS = 14;
// Le patch précédent compte moitié moins, et ainsi de suite. Rien n'est supprimé.
const MIN_AGE_WEIGHT = .02;
// Poids de la référence face aux parties observées, en équivalent parties.
const PRIOR_STRENGTH = 10, NO_PRIOR_STRENGTH = 5;
const SOLID_GAMES = 30, STALE_PATCHES = 3;
// Changement de méta : il faut assez de parties des deux côtés, un gros écart, et un écart qui ne ressemble pas au hasard.
const SHIFT_MIN_GAMES = 30, SHIFT_MIN_GAP = .2, SHIFT_MIN_Z = 3, SHIFT_OLD_WEIGHT = .1;
const REFERENCE_GAP = .3, REFERENCE_MIN_GAMES = 50;
const SCENARIO_MIN = .1, SCENARIO_CERTAIN = .9, SCENARIO_MAX = 4;

const emptyRoles = () => [0, 0, 0, 0, 0];
const rolesSum = list => list ? list.reduce((a, b) => a + b, 0) : 0;
const rateId = name => { const key = norm(name); return RATE_NAMES[key] || key; };

// ---------- Patchs et fraîcheur ----------
const patchIndex = patch => { const m = /^(\d+)\.(\d+)/.exec(patch || ""); return m ? +m[1] * PATCHES_PER_SEASON + +m[2] : null; };
const shortPatch = version => { const m = /^(\d+)\.(\d+)/.exec(version || ""); return m ? `${m[1]}.${m[2]}` : null; };
let knownPatch = null;
try { knownPatch = localStorage.getItem("draftPatch"); } catch (e) {}

function setKnownPatch(version) {
  const patch = shortPatch(version);
  if (!patch) return;
  knownPatch = patch;
  try { localStorage.setItem("draftPatch", patch); } catch (e) {}
}

// Le dernier patch connu : celui de Riot, ou le plus récent vu dans les parties collectées. Hors ligne, il peut être en retard.
function currentPatch() {
  let best = knownPatch;
  const live = window.ROLE_RATES_LIVE;
  if (live && live.patches) Object.keys(live.patches).forEach(p => { if ((patchIndex(p) || 0) > (patchIndex(best) || 0)) best = p; });
  return best;
}

// Âge en patchs. Sans patch connu, on l'estime d'après la date, et on le dit.
function ageOf(patch, collected) {
  const now = patchIndex(currentPatch()), then = patchIndex(patch);
  if (now !== null && then !== null) return { age: Math.max(0, now - then), estimated: false };
  const time = Date.parse(collected || "");
  if (isNaN(time)) return { age: null, estimated: true };
  return { age: Math.max(0, Math.floor((Date.now() - time) / 864e5 / PATCH_DAYS)), estimated: true };
}

const ageWeight = age => age === null ? .25 : Math.max(MIN_AGE_WEIGHT, Math.pow(.5, age));

// ---------- Sources ----------
function riotRoleBuckets() {
  const live = window.ROLE_RATES_LIVE;
  if (!live) return [];
  const buckets = [];
  if (live.patches) Object.keys(live.patches).forEach(p => {
    const b = live.patches[p];
    buckets.push({ patch: p === "?" ? null : p, collected: b.collected, games: b.games, champions: b.champions });
  });
  const old = live.patches ? live.legacy : live;
  if (old && old.champions) buckets.push({ patch: null, collected: old.updated, games: old.games, champions: old.champions });
  return buckets;
}

function describeRiotRoles() {
  const live = window.ROLE_RATES_LIVE, buckets = riotRoleBuckets();
  if (!buckets.length) return { available:false, note:I18N.t("data.riotRoles.none") };
  const dated = buckets.map(b => Object.assign(ageOf(b.patch, b.collected), b)).sort((x, y) => (x.age === null ? 1e9 : x.age) - (y.age === null ? 1e9 : y.age));
  const newest = dated[0];
  const noPatch = buckets.every(b => !b.patch);
  return {
    available:true, patch: noPatch ? null : newest.patch, collected: live.updated, games: buckets.reduce((sum, b) => sum + (b.games || 0), 0), unit:I18N.t("data.unit.games"),
    region: live.regions && live.regions.length ? live.regions.join(", ") : null, rank:I18N.t("data.rank.silverEmerald"), age: newest.age,
    uncertainty: I18N.t(newest.age === null || newest.age >= STALE_PATCHES ? "data.uncertainty.high" : "data.uncertainty.medium"),
    note: noPatch ? I18N.t("data.riotRoles.noPatch") : I18N.t("data.riotRoles.patches", { count: buckets.length })
  };
}

function riotMatchupBuckets() {
  const live = window.MATCHUPS_LIVE;
  if (!live) return [];
  if (live.patches) return Object.keys(live.patches).map(p => ({ patch: p === "?" ? null : p, collected: live.updated, pool: live.patches[p] }));
  return [{ patch: null, collected: live.updated, pool: live.pool }];
}

let sheetById = null;

const DATA_PROVIDERS = [
  {
    id:"data-dragon", label:I18N.t("data.dragon.label"), kind:"official", provides:["champions", "patch"], source:I18N.t("data.dragon.source"),
    terms:I18N.t("data.dragon.terms"),
    describe() {
      const patch = currentPatch();
      return { available: !!patch, patch, note: patch ? I18N.t("data.dragon.patch") : I18N.t("data.dragon.noPatch") };
    }
  },
  {
    id:"riot-roles", label:I18N.t("data.riotRoles.label"), kind:"observed", provides:["roles"], source:I18N.t("data.source.riotKey"),
    terms:I18N.t("data.terms.riotKey"),
    roleBuckets: riotRoleBuckets, describe: describeRiotRoles
  },
  {
    id:"meraki-roles", label:I18N.t("data.meraki.label"), kind:"estimate", provides:["roles"], source:"Meraki Analytics",
    terms:I18N.t("data.meraki.terms"),
    rolePrior(id) { return typeof ROLE_RATES !== "undefined" && ROLE_RATES.champions[id] ? { row: ROLE_RATES.champions[id], patch: ROLE_RATES.patch } : null; },
    describe() {
      if (typeof ROLE_RATES === "undefined") return { available:false, note:I18N.t("data.meraki.absent") };
      const age = ageOf(ROLE_RATES.patch).age;
      return { available:true, patch:ROLE_RATES.patch, age, games:null, uncertainty:I18N.t("data.uncertainty.high"),
        note:I18N.t("data.meraki.note") };
    }
  },
  {
    // Sans licence à vérifier : c'est lui qui prend le relais si la référence Meraki est retirée.
    id:"written-roles", label:I18N.t("data.writtenRoles.label"), kind:"heuristic", provides:["roles"], source:I18N.t("data.kind.heuristic"),
    terms:I18N.t("data.terms.project"),
    rolePrior(id) {
      if (typeof ROLE_SHEET === "undefined") return null;
      if (!sheetById) { sheetById = {}; Object.keys(ROLE_SHEET).forEach(n => { sheetById[rateId(n)] = ROLE_SHEET[n]; }); }
      const roles = sheetById[id];
      if (!roles) return null;
      const row = emptyRoles(), weights = [1, .35, .15];
      [...roles].forEach((letter, i) => { row["tjmas".indexOf(letter)] = weights[i] || .1; });
      return { row };
    },
    describe() {
      if (typeof ROLE_SHEET === "undefined") return { available:false, note:I18N.t("data.writtenRoles.absent") };
      return { available:true, uncertainty:I18N.t("data.uncertainty.high"), note:I18N.t("data.writtenRoles.note", { count: Object.keys(ROLE_SHEET).length }) };
    }
  },
  {
    id:"riot-matchups", label:I18N.t("data.riotMatchups.label"), kind:"observed", provides:["matchups"], source:I18N.t("data.source.riotKey"),
    terms:I18N.t("data.terms.riotKey"),
    matchupBuckets: riotMatchupBuckets,
    describe() {
      const live = window.MATCHUPS_LIVE;
      if (!live) return { available:false, note:I18N.t("data.riotMatchups.none") };
      const buckets = riotMatchupBuckets();
      const dated = buckets.map(b => ageOf(b.patch, b.collected)).filter(x => x.age !== null).map(x => x.age);
      const age = dated.length ? Math.min(...dated) : null;
      return { available:true, patch: Object.keys(live.patches || {}).sort((a, b) => (patchIndex(b) || 0) - (patchIndex(a) || 0))[0] || null, collected:live.updated, games:live.games, unit:I18N.t("data.unit.games"), age,
        region: live.regions && live.regions.length ? live.regions.join(", ") : null, rank:I18N.t("data.rank.silverEmerald"), uncertainty: I18N.t(age === null || age >= STALE_PATCHES ? "data.uncertainty.high" : "data.uncertainty.medium"),
        note:I18N.t("data.riotMatchups.note", { role: ROLE_NAMES[live.role || "mid"], min: MATCHUP_MIN_GAMES }) };
    }
  },
  {
    id:"written-matchups", label:I18N.t("data.writtenMatchups.label"), kind:"heuristic", provides:["matchups"], source:I18N.t("data.kind.heuristic"),
    terms:I18N.t("data.terms.project"),
    describe() { return { available:true, games:null, uncertainty:I18N.t("data.uncertainty.high"), note:I18N.t("data.writtenMatchups.note", { count: MATCHUPS.length }) }; }
  },
  {
    id:"champion-sheets", label:I18N.t("data.sheets.label"), kind:"heuristic", provides:["profiles"], source:I18N.t("data.kind.heuristic"),
    terms:I18N.t("data.terms.project"),
    describe() { return { available:true, games:null, uncertainty:I18N.t("data.uncertainty.high"), note:I18N.t("data.sheets.note", { count: Object.keys(CHAMPION_TRAITS).length }) }; }
  },
  {
    id:"personal-stats", label:I18N.t("data.personal.label"), kind:"observed", provides:["history"], source:I18N.t("data.personal.source"),
    terms:I18N.t("data.personal.terms"),
    describe() {
      const stats = window.DRAFT_HISTORY || window.DRAFT_STATS;
      if (!stats || !Array.isArray(stats.rows)) return { available:false, note:I18N.t("data.personal.none") };
      return { available:true, collected: stats.generated, games: stats.rows.length, unit:I18N.t("data.unit.games"), uncertainty:I18N.t("data.uncertainty.high"), note:I18N.t("data.personal.note") };
    }
  }
];

const KIND_LABELS = { official:I18N.t("data.kind.official"), observed:I18N.t("data.kind.observed"), estimate:I18N.t("data.kind.estimate"), heuristic:I18N.t("data.kind.heuristic") };

// État commun, lu par les réglages. Une valeur absente reste null : l'interface affiche « inconnu ».
function dataReport() {
  return DATA_PROVIDERS.map(p => {
    const d = p.describe();
    return {
      id:p.id, label:p.label, kind:p.kind, kindLabel:KIND_LABELS[p.kind], provides:p.provides, source:p.source, terms:p.terms,
      available:!!d.available, patch:d.patch || null, collected:d.collected || null, region:d.region || null, rank:d.rank || null,
      games: typeof d.games === "number" ? d.games : null, unit:d.unit || "", uncertainty:d.uncertainty || null, note:d.note || "",
      stale: typeof d.age === "number" && d.age >= STALE_PATCHES, age: typeof d.age === "number" ? d.age : null
    };
  });
}

// ---------- Rôles d'un champion ----------
// Un écart n'est retenu que s'il ressemble peu au hasard : quelques parties isolées ne suffisent jamais.
function detectShift(rows) {
  const current = rows.filter(r => r.age === 0 && !r.estimated);
  const older = rows.filter(r => !(r.age === 0 && !r.estimated));
  const sumRows = list => list.reduce((acc, r) => acc.map((v, i) => v + r.row[i]), emptyRoles());
  const a = sumRows(current), b = sumRows(older), na = rolesSum(a), nb = rolesSum(b);
  if (na < SHIFT_MIN_GAMES || nb < SHIFT_MIN_GAMES) return null;
  let found = null;
  a.forEach((count, role) => {
    const pa = count / na, pb = b[role] / nb, pool = (count + b[role]) / (na + nb);
    const se = Math.sqrt(pool * (1 - pool) * (1 / na + 1 / nb));
    const z = se > 0 ? Math.abs(pa - pb) / se : 0;
    if (pa - pb >= SHIFT_MIN_GAP && z >= SHIFT_MIN_Z && (!found || pa - pb > found.to - found.from)) found = { role, from:pb, to:pa, games:na, baseline:nb };
  });
  return found;
}

function roleEvidence(name) {
  const id = rateId(name);
  const rows = [];
  let prior = null;
  DATA_PROVIDERS.forEach(p => {
    if (p.roleBuckets) p.roleBuckets().forEach(b => {
      const row = b.champions && b.champions[id];
      if (row && rolesSum(row) > 0) rows.push(Object.assign({ row }, ageOf(b.patch, b.collected)));
    });
    if (p.rolePrior && !prior) {
      const found = p.rolePrior(id);
      if (found && rolesSum(found.row) > 0) prior = Object.assign({}, found, ageOf(found.patch));
    }
  });
  const shift = detectShift(rows);
  const counts = emptyRoles();
  let effective = 0, games = 0, currentGames = 0;
  rows.forEach(r => {
    const current = r.age === 0 && !r.estimated;
    const weight = ageWeight(r.age) * (shift && !current ? SHIFT_OLD_WEIGHT : 1);
    r.row.forEach((v, i) => { counts[i] += v * weight; });
    effective += rolesSum(r.row) * weight;
    games += rolesSum(r.row);
    if (current) currentGames += rolesSum(r.row);
  });
  // Une référence ancienne pèse moins et n'exclut jamais un rôle : un rôle rare reste possible.
  const age = prior && prior.age !== null ? prior.age : 20;
  const uniform = Math.min(.25, .05 + .01 * age);
  const priorTotal = prior ? rolesSum(prior.row) : 0;
  const base = emptyRoles().map((_, i) => prior ? (prior.row[i] / priorTotal) * (1 - uniform) + uniform / 5 : .2);
  const strength = prior ? PRIOR_STRENGTH : NO_PRIOR_STRENGTH;
  const shares = counts.map((v, i) => (v + strength * base[i]) / (effective + strength));
  const quality = effective >= SOLID_GAMES ? (rows.some(r => r.age !== null && r.age < STALE_PATCHES) ? "observed" : "old") : games > 0 ? "thin" : prior ? "reference" : "none";
  return { shares, known: rows.length > 0 || !!prior, games, effective, currentGames, quality, shift };
}

// Champions dont le rôle semble bouger. Sans base observée, l'écart à la référence reste « à confirmer ».
function roleChanges() {
  const list = [];
  NAMES.forEach(name => {
    const e = roleEvidence(name);
    if (e.shift) { list.push({ name, role:ROLE_ORDER[e.shift.role], from:e.shift.from, to:e.shift.to, games:e.shift.games, confirmed:true }); return; }
    const found = DATA_PROVIDERS.reduce((acc, p) => acc || (p.rolePrior ? p.rolePrior(rateId(name)) : null), null);
    if (!found || e.games < REFERENCE_MIN_GAMES) return;
    const total = rolesSum(found.row);
    let best = null;
    e.shares.forEach((share, role) => {
      const gap = share - found.row[role] / total;
      if (gap >= REFERENCE_GAP && (!best || gap > best.gap)) best = { role, gap };
    });
    if (best) list.push({ name, role:ROLE_ORDER[best.role], from:found.row[best.role] / total, to:e.shares[best.role], games:e.games, confirmed:false });
  });
  return list.sort((a, b) => (b.to - b.from) - (a.to - a.from));
}

// ---------- Répartition des rôles ----------
// Toutes les répartitions compatibles sont pesées. Les rôles non pris restent pour les champions non révélés.
function roleBelief(team, fixed) {
  const list = team.slice(0, ROLE_ORDER.length);
  const evidence = list.map(roleEvidence);
  const forced = fixed || {};
  const marginals = list.map(emptyRoles), vacant = emptyRoles();
  const walk = (lock) => {
    let total = 0;
    const place = (i, used, weight, chosen) => {
      if (i === list.length) {
        total += weight;
        chosen.forEach((role, k) => { marginals[k][role] += weight; });
        ROLE_ORDER.forEach((_, role) => { if (!(used & (1 << role))) vacant[role] += weight; });
        return;
      }
      const only = lock && forced[list[i]] ? ROLE_ORDER.indexOf(forced[list[i]]) : -1;
      ROLE_ORDER.forEach((_, role) => {
        if ((used & (1 << role)) || (only >= 0 && role !== only)) return;
        place(i + 1, used | (1 << role), weight * evidence[i].shares[role], chosen.concat(role));
      });
    };
    place(0, 0, 1, []);
    return total;
  };
  let total = walk(true);
  // Deux corrections contradictoires : on repart sans elles plutôt que de ne rien répondre.
  if (!total) { marginals.forEach(m => m.fill(0)); vacant.fill(0); total = walk(false); }
  const divide = v => total ? v / total : 0;
  return { team:list, evidence, marginals:marginals.map(m => m.map(divide)), vacant:vacant.map(divide) };
}

// Affichage seulement : deux rôles sont « proches » quand l'un pèse au moins 80 % de l'autre. N'entre dans aucun calcul.
const ROLE_CLOSE_RATIO = .8, ROLE_SURE_SHARE = .5;

// Répartition la plus probable, tirée de roleBelief sans rien y changer : même produit des parts, mêmes corrections.
// Fonction pure : elle ne modifie aucune probabilité et ne sert qu'à l'affichage et au relevé.
// À égalité exacte, l'ordre alphabétique des noms tranche, pour que l'ordre de saisie n'ait aucun effet.
// Pour chaque champion : rôle affiché, sa probabilité, rôle le plus probable pris seul, rôles proches, et s'il est incertain.
function likelyAssignment(team, fixed) {
  const belief = roleBelief(team, fixed);
  const forced = fixed || {};
  const order = belief.team.map((name, i) => i).sort((a, b) => belief.team[a] < belief.team[b] ? -1 : belief.team[a] > belief.team[b] ? 1 : a - b);
  const search = lock => {
    let best = null;
    const place = (k, used, weight, chosen) => {
      if (k === order.length) { if (weight > 0 && (!best || weight > best.weight * (1 + 1e-9))) best = { weight, chosen:chosen.slice() }; return; }
      const i = order[k];
      const only = lock && forced[belief.team[i]] ? ROLE_ORDER.indexOf(forced[belief.team[i]]) : -1;
      ROLE_ORDER.forEach((_, role) => {
        if ((used & (1 << role)) || (only >= 0 && role !== only)) return;
        chosen[i] = role;
        place(k + 1, used | (1 << role), weight * belief.evidence[i].shares[role], chosen);
      });
    };
    place(0, 0, 1, []);
    return best;
  };
  // Deux corrections contradictoires : comme roleBelief, on repart sans elles.
  const best = search(true) || search(false);
  const slots = {}, byName = {};
  belief.team.forEach((name, i) => {
    const m = belief.marginals[i];
    const role = best ? best.chosen[i] : m.indexOf(Math.max(...m));
    const top = m.indexOf(Math.max(...m));
    const close = ROLE_ORDER.map((_, r) => r).filter(r => r !== role && (r === top || m[r] >= ROLE_CLOSE_RATIO * m[role]) && m[r] > 0)
      .sort((a, b) => m[b] - m[a] || a - b).map(r => ROLE_ORDER[r]);
    const corrected = !!forced[name] && forced[name] === ROLE_ORDER[role];
    slots[ROLE_ORDER[role]] = name;
    byName[name] = { role:ROLE_ORDER[role], p:m[role], top:ROLE_ORDER[top], topP:m[top], close:corrected ? [] : close, uncertain:!corrected && (close.length > 0 || m[role] < ROLE_SURE_SHARE), corrected };
  });
  return { slots, byName, team:belief.team };
}

// Relevé de validation : rôle le plus probable de chaque adversaire et sa probabilité, « Garen=top:0.92,Kog'Maw=mid:0.41 ».
function predictedRolesText(team, fixed) {
  const belief = roleBelief(team, fixed);
  return belief.team.map((name, i) => {
    const m = belief.marginals[i], top = m.indexOf(Math.max(...m));
    return `${name}=${ROLE_ORDER[top]}:${m[top].toFixed(2)}`;
  }).join(",");
}

// Qui peut être en face du joueur, et le risque qu'il ne soit pas encore révélé.
function opponentBelief(team, fixed) {
  const index = ROLE_ORDER.indexOf(myRole());
  const belief = roleBelief(team, fixed);
  const candidates = belief.team.map((name, i) => ({
    name, p:belief.marginals[i][index], share:belief.evidence[i].shares[index], known:belief.evidence[i].known, quality:belief.evidence[i].quality
  })).sort((a, b) => b.p - a.p);
  const unrevealed = belief.team.length < ROLE_ORDER.length ? belief.vacant[index] : 0;
  return { candidates, unrevealed, confidence:beliefConfidence(candidates, unrevealed) };
}

// Confiance sur l'adversaire direct : un code technique (low, medium, high), jamais un mot affiché.
// Le relevé de validation (CSV) et les instantanés des bancs gardent l'ancien mot français, par cette table.
const CONFIDENCE_WORD = { low:"faible", medium:"moyenne", high:"élevée" };
const confidenceWord = code => CONFIDENCE_WORD[code] || code;

function beliefConfidence(candidates, unrevealed) {
  const top = candidates[0];
  if (!top || !top.known) return "low";
  const rival = Math.max(candidates[1] ? candidates[1].p : 0, unrevealed);
  let level = top.p >= .75 && top.p - rival >= .5 ? 2 : top.p >= .5 && top.p - rival >= .15 ? 1 : 0;
  // Avec des données fragiles, la répartition peut sembler nette sans l'être vraiment.
  if (top.quality !== "observed") level = Math.min(level, 1);
  return ["low", "medium", "high"][level];
}

// Ce que l'interface a le droit de présenter comme l'adversaire. Sans confiance suffisante, personne n'est désigné :
// les hypothèses restent pesées dans le classement, mais aucune n'est affichée comme un fait.
function identifiedOpponent(team, fixed) {
  const belief = opponentBelief(team, fixed);
  return belief.confidence === "low" ? null : guessMid(team, fixed);
}

// Hypothèses à évaluer : les adversaires plausibles, et « pas encore choisi » s'il reste une vraie chance.
function beliefScenarios(belief) {
  const list = belief.candidates.filter(c => c.known && c.p >= SCENARIO_MIN).map(c => ({ mid:c.name, p:c.p }));
  if (belief.unrevealed >= SCENARIO_MIN) list.push({ mid:null, p:belief.unrevealed });
  if (!list.length) return [{ mid:null, p:1 }];
  const kept = list.sort((a, b) => b.p - a.p).slice(0, SCENARIO_MAX);
  if (kept[0].p >= SCENARIO_CERTAIN) return [{ mid:kept[0].mid, p:1 }];
  const sum = kept.reduce((acc, s) => acc + s.p, 0);
  return kept.map(s => ({ mid:s.mid, p:s.p / sum }));
}

// Hypothèses, sans pourcentage : la précision des données ne le justifierait pas.
// others : des noms de champions seulement. notYet : il reste une vraie chance que l'adversaire ne soit pas encore choisi.
function beliefHypothesis(belief, mid) {
  const others = belief.candidates.filter(c => c.known && c.name !== mid && c.p >= .2).map(c => c.name).slice(0, 2);
  return { main:mid, others, notYet:belief.unrevealed >= .2 };
}

// ---------- Simulation ----------
// Une équipe adverse au hasard : un champion réellement joué à chaque rôle, hors du pool du joueur pour pouvoir comparer tous ses picks.
// Renvoie les noms dans un ordre de révélation mélangé, ou null si les données ne le permettent pas.
const RANDOM_ROLE_MIN_SHARE = .25;
function randomDraft(random = Math.random) {
  const own = PICKS.map(label);
  const byRole = ROLE_ORDER.map((_, role) => NAMES.filter(n => {
    if (own.includes(n) || PROVISIONAL.includes(n) || !roleEvidence(n).known) return false;
    return roleEvidence(n).shares[role] >= RANDOM_ROLE_MIN_SHARE;
  }));
  if (byRole.some(list => !list.length)) return null;
  const team = [];
  // Les rôles les moins fournis d'abord : un champion flex ne doit pas leur prendre leur seul candidat.
  byRole.map((list, role) => ({ list, role })).sort((a, b) => a.list.length - b.list.length).forEach(({ list, role }) => {
    const free = list.filter(n => !team.some(t => t.name === n));
    if (free.length) team.push({ name:free[Math.floor(random() * free.length)], role });
  });
  if (team.length < ROLE_ORDER.length) return null;
  const names = team.map(t => t.name);
  for (let i = names.length - 1; i > 0; i--) { const k = Math.floor(random() * (i + 1)); [names[i], names[k]] = [names[k], names[i]]; }
  return names;
}
