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
  if (!buckets.length) return { available:false, note:"Pas encore calculés : lance « Mettre à jour les rôles » avec ta clé Riot." };
  const dated = buckets.map(b => Object.assign(ageOf(b.patch, b.collected), b)).sort((x, y) => (x.age === null ? 1e9 : x.age) - (y.age === null ? 1e9 : y.age));
  const newest = dated[0];
  const noPatch = buckets.every(b => !b.patch);
  return {
    available:true, patch: noPatch ? null : newest.patch, collected: live.updated, games: buckets.reduce((sum, b) => sum + (b.games || 0), 0), unit:"parties",
    region: live.regions && live.regions.length ? live.regions.join(", ") : null, rank:"Silver à Émeraude, classé solo/duo", age: newest.age,
    uncertainty: newest.age === null || newest.age >= STALE_PATCHES ? "élevée" : "moyenne",
    note: noPatch ? "Calcul fait avant le suivi par patch : le patch et la région n'ont pas été enregistrés, l'âge est estimé d'après la date." : `${buckets.length} patch(s) conservé(s). Les plus anciens comptent moins, sans être supprimés.`
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
    id:"data-dragon", label:"Noms, portraits et patch", kind:"official", provides:["champions", "patch"], source:"Riot Games, Data Dragon",
    terms:"Données publiques de Riot.",
    describe() {
      const patch = currentPatch();
      return { available: !!patch, patch, note: patch ? "Dernier patch connu. Hors ligne, il peut être en retard." : "Patch inconnu : l'âge des données est estimé d'après leur date." };
    }
  },
  {
    id:"riot-roles", label:"Rôles joués, parties récentes", kind:"observed", provides:["roles"], source:"API Riot, avec ta clé personnelle",
    terms:"Calculés sur ton PC avec ta clé. Aucun nom de joueur n'est gardé.",
    roleBuckets: riotRoleBuckets, describe: describeRiotRoles
  },
  {
    id:"meraki-roles", label:"Rôles, référence livrée", kind:"estimate", provides:["roles"], source:"Meraki Analytics",
    terms:"Licence non confirmée : à vérifier avant de la présenter comme une source autorisée.",
    rolePrior(id) { return typeof ROLE_RATES !== "undefined" && ROLE_RATES.champions[id] ? { row: ROLE_RATES.champions[id], patch: ROLE_RATES.patch } : null; },
    describe() {
      if (typeof ROLE_RATES === "undefined") return { available:false, note:"Non livrée dans cette version : les rôles habituels écrits à la main prennent le relais." };
      const age = ageOf(ROLE_RATES.patch).age;
      return { available:true, patch:ROLE_RATES.patch, age, games:null, uncertainty:"élevée",
        note:"Des taux, sans nombre de parties, et beaucoup de champions n'ont qu'un seul rôle. Sert seulement de référence, jamais d'observation actuelle." };
    }
  },
  {
    // Sans licence à vérifier : c'est lui qui prend le relais si la référence Meraki est retirée.
    id:"written-roles", label:"Rôles habituels", kind:"heuristic", provides:["roles"], source:"Écrit à la main",
    terms:"Propre au projet.",
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
      if (typeof ROLE_SHEET === "undefined") return { available:false, note:"Fichier absent." };
      return { available:true, uncertainty:"élevée", note:`${Object.keys(ROLE_SHEET).length} champions. Avis général, utilisé seulement là où aucune autre référence n'existe.` };
    }
  },
  {
    id:"riot-matchups", label:"Bilans de matchups", kind:"observed", provides:["matchups"], source:"API Riot, avec ta clé personnelle",
    terms:"Calculés sur ton PC avec ta clé. Aucun nom de joueur n'est gardé.",
    matchupBuckets: riotMatchupBuckets,
    describe() {
      const live = window.MATCHUPS_LIVE;
      if (!live) return { available:false, note:"Pas encore calculés : lance « Mettre à jour les matchups » avec ta clé Riot." };
      const buckets = riotMatchupBuckets();
      const dated = buckets.map(b => ageOf(b.patch, b.collected)).filter(x => x.age !== null).map(x => x.age);
      const age = dated.length ? Math.min(...dated) : null;
      return { available:true, patch: Object.keys(live.patches || {}).sort((a, b) => (patchIndex(b) || 0) - (patchIndex(a) || 0))[0] || null, collected:live.updated, games:live.games, unit:"parties", age,
        region: live.regions && live.regions.length ? live.regions.join(", ") : null, rank:"Silver à Émeraude, classé solo/duo", uncertainty: age === null || age >= STALE_PATCHES ? "élevée" : "moyenne",
        note:`Pour le rôle ${ROLE_NAMES[live.role || "mid"]}. Sous ${MATCHUP_MIN_GAMES} parties utiles, un bilan n'est pas utilisé.` };
    }
  },
  {
    id:"written-matchups", label:"Tableau de matchups", kind:"heuristic", provides:["matchups"], source:"Écrit à la main",
    terms:"Propre au projet.",
    describe() { return { available:true, games:null, uncertainty:"élevée", note:`${MATCHUPS.length} adversaires au mid, pour Twisted Fate, Fizz, Kog'Maw et Xerath seulement. Sans statistiques ni patch.` }; }
  },
  {
    id:"champion-sheets", label:"Fiches de champions", kind:"heuristic", provides:["profiles"], source:"Écrit à la main",
    terms:"Propre au projet.",
    describe() { return { available:true, games:null, uncertainty:"élevée", note:`${Object.keys(CHAMPION_TRAITS).length} fiches d'avis général, sans statistiques ni patch. À revoir si un champion est refait.` }; }
  },
  {
    id:"personal-stats", label:"Tes statistiques importées", kind:"observed", provides:["history"], source:"API Riot, tes comptes",
    terms:"Privées : le fichier n'est jamais publié.",
    describe() {
      const stats = window.DRAFT_STATS;
      if (!stats || !Array.isArray(stats.rows)) return { available:false, note:"Pas d'import sur ce PC. Facultatif." };
      return { available:true, collected: stats.generated, games: stats.rows.length, unit:"parties", uncertainty:"élevée", note:"Affichées seulement : elles ne changent pas le classement." };
    }
  }
];

const KIND_LABELS = { official:"Officiel", observed:"Observé", estimate:"Estimation externe", heuristic:"Écrit à la main" };

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

function beliefConfidence(candidates, unrevealed) {
  const top = candidates[0];
  if (!top || !top.known) return "faible";
  const rival = Math.max(candidates[1] ? candidates[1].p : 0, unrevealed);
  let level = top.p >= .75 && top.p - rival >= .5 ? 2 : top.p >= .5 && top.p - rival >= .15 ? 1 : 0;
  // Avec des données fragiles, la répartition peut sembler nette sans l'être vraiment.
  if (top.quality !== "observed") level = Math.min(level, 1);
  return ["faible", "moyenne", "élevée"][level];
}

// Ce que l'interface a le droit de présenter comme l'adversaire. Sans confiance suffisante, personne n'est désigné :
// les hypothèses restent pesées dans le classement, mais aucune n'est affichée comme un fait.
function identifiedOpponent(team, fixed) {
  const belief = opponentBelief(team, fixed);
  return belief.confidence === "faible" ? null : guessMid(team, fixed);
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

// Texte des hypothèses, sans pourcentage : la précision des données ne le justifierait pas.
function beliefHypothesis(belief, mid) {
  const others = belief.candidates.filter(c => c.known && c.name !== mid && c.p >= .2).map(c => c.name).slice(0, 2);
  if (belief.unrevealed >= .2) others.push("pas encore choisi");
  return { main:mid, others };
}
