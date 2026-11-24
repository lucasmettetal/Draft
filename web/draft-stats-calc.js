// Mes statistiques : calculs purs. Aucun DOM, aucun texte affiché, aucune écriture. Règles : docs/stats/CALCULS.md.
// Chaîne prévue : sources -> calcul (ce fichier) -> règles (futur coaching, non branché) -> rendu (draft-stats-view.js).
// Les corrections (doublons, millisecondes, lignes incomplètes) n'existent que dans le calcul : aucun fichier n'est modifié.
(function (root) {
  const MIN_GAMES = 10;          // aucun pourcentage en dessous (MIN_GAMES de l'application)
  const SLICE = 20;              // tranches de la chronologie (EVOLUTION_STEP)
  const MS_FROM = 20000;         // durée >= 20 000 : millisecondes (avant le patch 11.20)
  const REMAKE_SEC = 300;        // même seuil que l'import (MinDurationSec)
  const TIME_MIN = 1e11;         // date en millisecondes
  const POSITIONS = ["TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY"];
  const ROLE_OF = { TOP: "top", JUNGLE: "jungle", MIDDLE: "mid", BOTTOM: "adc", UTILITY: "support" };
  const ROLES = ["top", "jungle", "mid", "adc", "support"];
  const UNKNOWN = "unknown";

  const isNum = v => typeof v === "number" && isFinite(v);
  const text = v => typeof v === "string" ? v.trim() : "";

  // Choix de la source : l'import de l'application, sinon l'ancien script. Jamais les deux additionnés.
  function pickSource(history, legacy) {
    if (history && Array.isArray(history.rows)) return { source: "application", data: history };
    if (legacy && Array.isArray(legacy.rows)) return { source: "script", data: legacy };
    return null;
  }

  // Lecture et contrôle de chaque ligne. resolveName : nom de l'API Riot -> nom affiché (toName de la page).
  function readHistory(history, legacy, resolveName) {
    const picked = pickSource(history, legacy);
    const quality = { read: 0, unreadable: 0, duplicates: 0, noId: 0, noResult: 0, remakes: 0, msConverted: 0, noDuration: 0, noTime: 0, noPatch: 0, noKda: 0, unknownRole: 0, unknownChampion: 0 };
    if (!picked) return { source: null, generated: null, accounts: [], games: [], quality };
    const name = typeof resolveName === "function" ? resolveName : (x => x);
    const seen = new Set(), games = [];
    picked.data.rows.forEach(row => {
      quality.read++;
      if (!row || typeof row !== "object" || Array.isArray(row)) { quality.unreadable++; return; }
      const id = text(row.id), acct = text(row.acct);
      if (id) {
        const key = acct + "|" + id;
        if (seen.has(key)) { quality.duplicates++; return; }
        seen.add(key);
      } else quality.noId++;
      if (row.win !== true && row.win !== false) { quality.noResult++; return; }
      let sec = isNum(row.sec) && row.sec > 0 ? row.sec : null;
      if (sec !== null && sec >= MS_FROM) { sec = sec / 1000; quality.msConverted++; }
      if (sec !== null && sec < REMAKE_SEC) { quality.remakes++; return; }
      if (sec === null) quality.noDuration++;
      const time = isNum(row.time) && row.time > TIME_MIN ? row.time : null;
      if (time === null) quality.noTime++;
      const patch = /^\d+\.\d+$/.test(text(row.patch)) ? text(row.patch) : null;
      if (patch === null) quality.noPatch++;
      const role = ROLE_OF[text(row.pos).toUpperCase()] || UNKNOWN;
      if (role === UNKNOWN) quality.unknownRole++;
      const raw = text(row.champ), champ = raw ? (name(raw) || raw) : "";
      if (!champ) quality.unknownChampion++;
      const kdaOk = [row.k, row.d, row.a].every(v => isNum(v) && v >= 0);
      if (!kdaOk) quality.noKda++;
      const mid = text(row.mid) ? (name(text(row.mid)) || text(row.mid)) : null;
      games.push({ id: id || null, acct, time, patch, sec, champ, role, win: row.win, k: kdaOk ? row.k : null, d: kdaOk ? row.d : null, a: kdaOk ? row.a : null, mid });
    });
    const accounts = [...new Set(games.map(g => g.acct))];
    return { source: picked.source, generated: picked.data.generated || null, accounts, games, quality };
  }

  function record(list, isWin) {
    const win = isWin || (g => g.win === true);
    const games = list.length, wins = list.filter(win).length;
    return { games, wins, losses: games - wins, rate: games >= MIN_GAMES ? wins / games : null };
  }

  // KDA sur la somme : (K + A) / max(1, D). D = 0 sur l'ensemble : « sans mort ».
  function kda(list) {
    const ok = list.filter(g => g.k !== null);
    if (!ok.length) return null;
    const k = ok.reduce((s, g) => s + g.k, 0), d = ok.reduce((s, g) => s + g.d, 0), a = ok.reduce((s, g) => s + g.a, 0);
    return { games: ok.length, k: k / ok.length, d: d / ok.length, a: a / ok.length, ratio: (k + a) / Math.max(1, d), deathless: d === 0 };
  }

  const patchValue = p => { const [x, y] = p.split(".").map(Number); return x * 1000 + y; };
  function rolesOf(list) {
    const counts = {};
    list.forEach(g => { counts[g.role] = (counts[g.role] || 0) + 1; });
    return counts;
  }
  // Rôle connu le plus joué ; égalité : ordre Top, Jungle, Mid, ADC, Support. Jamais le rôle inconnu.
  function mainRole(list) {
    const counts = rolesOf(list);
    let best = null;
    ROLES.forEach(r => { if ((counts[r] || 0) > (best ? counts[best] : 0)) best = r; });
    return best;
  }

  function summary(read) {
    const games = read.games;
    const dated = games.filter(g => g.time !== null).map(g => g.time).sort((a, b) => a - b);
    const patches = [...new Set(games.map(g => g.patch).filter(Boolean))].sort((a, b) => patchValue(a) - patchValue(b));
    const timed = games.filter(g => g.sec !== null);
    return {
      record: record(games), kda: kda(games),
      duration: timed.length ? { meanSec: timed.reduce((s, g) => s + g.sec, 0) / timed.length, games: timed.length } : null,
      period: dated.length ? { from: dated[0], to: dated[dated.length - 1] } : null,
      patches: { count: patches.length, first: patches[0] || null, last: patches[patches.length - 1] || null, list: patches },
      accounts: read.accounts.length, mainRole: mainRole(games)
    };
  }

  function byRole(games) {
    return ROLES.concat([UNKNOWN]).map(role => {
      const list = games.filter(g => g.role === role);
      return Object.assign({ role }, record(list));
    }).filter(r => r.games || r.role !== UNKNOWN);
  }

  function byChampion(games) {
    const groups = new Map();
    games.forEach(g => { const k = g.champ || ""; if (!groups.has(k)) groups.set(k, []); groups.get(k).push(g); });
    return [...groups].map(([champ, list]) => Object.assign({ champ, mainRole: mainRole(list), roles: rolesOf(list), kda: kda(list),
      last: Math.max(0, ...list.map(g => g.time || 0)) || null }, record(list)))
      .sort((a, b) => b.games - a.games || (a.champ || "~").localeCompare(b.champ || "~"));
  }

  // Suite des parties datées, et tranches de 20 dont la dernière, incomplète, est gardée.
  function timeline(games, step) {
    const size = step || SLICE;
    const dated = games.filter(g => g.time !== null).sort((a, b) => a.time - b.time);
    const slices = [];
    for (let i = 0; i < dated.length; i += size) {
      const part = dated.slice(i, i + size);
      slices.push(Object.assign({ from: i + 1, to: i + part.length, complete: part.length === size, start: part[0].time, end: part[part.length - 1].time }, record(part)));
    }
    return { points: dated.map(g => ({ time: g.time, win: g.win })), slices, undated: games.length - dated.length };
  }

  // Vis-à-vis : connu seulement en mid (champ « mid », premier adversaire placé MIDDLE par Riot).
  function matchups(games, filter) {
    const f = filter || {};
    const role = f.role || "mid";
    const inRole = games.filter(g => g.role === role && (!f.champ || g.champ === f.champ));
    if (role !== "mid") return { role, available: false, games: inRole.length, opponents: [], withoutOpponent: inRole.length, played: [] };
    const known = inRole.filter(g => g.mid);
    const groups = new Map();
    known.forEach(g => { if (!groups.has(g.mid)) groups.set(g.mid, []); groups.get(g.mid).push(g); });
    const opponents = [...groups].map(([name, list]) => {
      const champs = {};
      list.forEach(g => { const c = g.champ || ""; champs[c] = (champs[c] || 0) + 1; });
      return Object.assign({ name, champs: Object.entries(champs).map(([n, c]) => ({ name: n, games: c })).sort((a, b) => b.games - a.games || a.name.localeCompare(b.name)) }, record(list));
    }).sort((a, b) => b.games - a.games || a.name.localeCompare(b.name));
    // Champions joués en mid, pour le filtre (avant le filtre de champion).
    const played = byChampion(games.filter(g => g.role === "mid")).map(c => ({ name: c.champ, games: c.games }));
    return { role, available: true, games: inRole.length, opponents, withoutOpponent: inRole.length - known.length, played };
  }

  // Origine d'une entrée du journal. Marque de la #33 : origin "draft". Journal automatique (beta.2, #40) : origin "lcu", pick lu
  // dans le client et partie lancée : une vraie draft, comptée comme "draft". Choix du joueur pour une entrée sans marque : userOrigin.
  // Un champ absent est inconnu : rien n'est déduit, rien n'est supprimé.
  function originOf(e) {
    if (e.origin === "draft" || e.origin === "lcu") return "draft";
    if (e.userOrigin === "real") return "real";
    if (e.userOrigin === "simulation") return "simulation";
    return UNKNOWN;
  }
  // Conseil suivi : picked === suggested, ou un ex æquo quand l'entrée porte tied (#33). Aucune égalité reconstruite sinon.
  function followedOf(e) {
    if (!e.picked) return false;
    if (e.picked === e.suggested) return true;
    return e.tied === true && Array.isArray(e.tiedWith) && e.tiedWith.includes(e.picked);
  }

  // Draft annulée (beta.2, #39) : gardée, jamais comptée comme partie (docs/technique/JOURNAL.md).
  const abandonedOf = e => e.abandoned === true;
  const countedOf = e => { const o = originOf(e); return (o === "draft" || o === "real") && !abandonedOf(e); };

  // ---------- Rapprochement avec l'historique Riot importé (beta.3, docs/technique/JOURNAL.md, « Champ riot ») ----------
  // Lien unique : le gameId lu dans le client (entrée) = la partie numérique de l'id Riot « <plateforme>_<gameId> » (EUW1_7012345678).
  // Sans gameId, rien : aucun rapprochement approximatif par heure ou par champion. Calculé à l'affichage, jamais écrit dans le journal.
  const GAME_ID = /^[1-9]\d{0,18}$/;
  function gameIdOf(e) {
    const v = typeof e.gameId === "number" ? (Number.isSafeInteger(e.gameId) ? String(e.gameId) : "") : text(e.gameId);
    return GAME_ID.test(v) ? v : null;
  }
  const riotGameId = id => { const m = /^[A-Za-z0-9]+_([1-9]\d{0,18})$/.exec(text(id)); return m ? m[1] : null; };
  // Détails officiels, seulement ceux de la ligne Riot : un champ absent reste null. Adversaire de voie : connu seulement en mid
  // (champ « mid » de l'import, premier adversaire placé MIDDLE), jamais déduit ailleurs.
  function riotDetails(g) {
    const o = { id: g.id, result: g.win ? "win" : "loss", k: g.k, d: g.d, a: g.a, durationSec: g.sec, patch: g.patch };
    if (g.role === "mid" && g.mid) o.opponent = g.mid;
    return o;
  }
  const sameGame = (x, y) => x.id === y.id && x.win === y.win && x.k === y.k && x.d === y.d && x.a === y.a && x.sec === y.sec;
  // Parties Riot par gameId. Même gameId sur deux plateformes, ou deux lignes différentes : ambigu, aucun rapprochement.
  function riotIndex(games) {
    const map = new Map();
    (Array.isArray(games) ? games : []).forEach(g => {
      const n = g && riotGameId(g.id);
      if (!n) return;
      if (!map.has(n)) map.set(n, g);
      else if (map.get(n) && !sameGame(map.get(n), g)) map.set(n, null);
    });
    return map;
  }
  // Entrées comptées (hors drafts annulées) -> détails Riot. Une partie Riot ne confirme qu'UNE entrée : deux entrées du même
  // gameId restent sans rapprochement (ambigu). Aucune entrée n'est modifiée.
  function riotLinks(all, games) {
    const index = riotIndex(games), links = new Map(), byId = new Map();
    const stats = { matched: 0, disagreements: 0, ambiguous: 0, notFound: 0, withoutGameId: 0 };
    all.filter(countedOf).forEach(e => {
      const n = gameIdOf(e);
      if (!n) { stats.withoutGameId++; return; }
      if (!byId.has(n)) byId.set(n, []);
      byId.get(n).push(e);
    });
    byId.forEach((list, n) => {
      if (!index.has(n)) { stats.notFound += list.length; return; }
      const g = index.get(n);
      if (!g || list.length > 1) { stats.ambiguous += list.length; return; }
      const riot = riotDetails(g), e = list[0];
      links.set(e, riot);
      stats.matched++;
      if ((e.result === "win" || e.result === "loss") && e.result !== riot.result) stats.disagreements++;
    });
    return { links, stats };
  }

  // Journal DRAFT. Trois compteurs distincts : enregistrées (toutes, rien n'est supprimé), confirmées (origin "draft" ou "lcu", ou
  // userOrigin "real", hors drafts annulées), à classer (origine inconnue). Les statistiques de performance (V/D, conseil suivi) ne
  // lisent que les confirmées. games (facultatif) : parties lues par readHistory. Riot fait foi : le résultat d'une entrée rapprochée
  // est celui de Riot, même si le résultat stocké (saisi ou lu dans le client) diffère ; le désaccord est rendu, jamais masqué.
  function journal(entries, games) {
    const all = (Array.isArray(entries) ? entries : []).filter(e => e && typeof e === "object");
    const riot = riotLinks(all, games);
    const stored = e => e.result === "win" || e.result === "loss" ? e.result : null;
    const official = e => riot.links.has(e) ? riot.links.get(e).result : stored(e);
    const finished = e => official(e) !== null, won = e => official(e) === "win";
    const confirmed = all.filter(countedOf);
    const done = confirmed.filter(finished);
    const dates = all.map(e => Date.parse(e.date)).filter(isFinite).sort((a, b) => a - b);
    const time = e => Date.parse(e.date) || e.id || 0;
    return {
      recorded: all.length, confirmed: confirmed.length,
      toSort: all.filter(e => originOf(e) === UNKNOWN).length, simulations: all.filter(e => originOf(e) === "simulation").length,
      abandoned: all.filter(abandonedOf).length,
      finished: record(done, won),
      pending: confirmed.filter(e => !riot.links.has(e) && (e.result === null || e.result === undefined)).length,
      skipped: confirmed.filter(e => !riot.links.has(e) && e.result === "skip").length,
      followed: record(done.filter(followedOf), won), other: record(done.filter(e => !followedOf(e)), won),
      ties: confirmed.filter(e => e.tied === true).length,
      period: dates.length ? { from: dates[0], to: dates[dates.length - 1] } : null,
      riot: riot.stats,
      entries: all.slice().sort((a, b) => time(b) - time(a)).map(e => {
        const origin = originOf(e);
        const abandoned = abandonedOf(e);
        const r = riot.links.get(e) || null, s = stored(e);
        return { entry: e, origin, abandoned, counted: countedOf(e), followed: followedOf(e), tied: e.tied === true, finished: finished(e),
          result: official(e), riot: r, disagree: !!r && s !== null && s !== r.result,
          storedSource: s === null ? null : e.resultSource === "lcu" ? "lcu" : "manual" };
      })
    };
  }

  // ---------- Seuils de l'étude (COACHING-ARCHITECTURE.md) : progression, puis observations factuelles ----------
  // Atteindre un seuil n'est pas une preuve : l'observation dépend d'un test (intervalle de Wilson, écart des deux moitiés).
  const THRESHOLDS = Object.freeze({ champion: 30, reference: 30, trend: 40, trendZ: 2, opponent: 10, advice: MIN_GAMES });
  function wilson(wins, games, z) {
    const q = z || 1.96;
    if (!games) return [0, 1];
    const p = wins / games, d = 1 + q * q / games, c = p + q * q / (2 * games), m = q * Math.sqrt((p * (1 - p) + q * q / (4 * games)) / games);
    return [Math.max(0, (c - m) / d), Math.min(1, (c + m) / d)];
  }
  // Deux moitiés dans l'ordre du temps ; z = écart des taux / erreur type. Même règle que performanceTrend (draft-mastery.js).
  function halves(list) {
    const sorted = list.filter(g => g.time !== null).sort((a, b) => a.time - b.time), half = Math.floor(sorted.length / 2);
    const older = sorted.slice(0, half), newer = sorted.slice(half);
    if (!older.length || !newer.length) return null;
    const p1 = older.filter(g => g.win).length / older.length, p2 = newer.filter(g => g.win).length / newer.length;
    const p = sorted.filter(g => g.win).length / sorted.length, se = Math.sqrt(p * (1 - p) * (1 / older.length + 1 / newer.length));
    return { older: { games: older.length, rate: p1 }, newer: { games: newer.length, rate: p2 }, z: se ? (p2 - p1) / se : 0 };
  }

  // Progression : seulement ce qui n'a pas encore atteint son seuil.
  function progress(games, journalSummary) {
    const champs = byChampion(games), main = mainRole(games), mids = matchups(games, { role: "mid" }).opponents;
    const j = journalSummary || journal([]);
    const all = {
      champion: { have: champs.length ? champs[0].games : 0, need: THRESHOLDS.champion, subject: champs.length ? champs[0].champ : null },
      trend: { have: main ? games.filter(g => g.role === main).length : 0, need: THRESHOLDS.trend, subject: main },
      opponent: { have: mids.length ? mids[0].games : 0, need: THRESHOLDS.opponent, subject: mids.length ? mids[0].name : null },
      advice: { have: Math.min(j.followed.games, j.other.games), need: THRESHOLDS.advice, followed: j.followed.games, other: j.other.games }
    };
    Object.keys(all).forEach(k => { all[k].reached = all[k].have >= all[k].need; });
    return all;
  }

  // Observations factuelles, une fois le seuil atteint. Chaque observation porte sa mesure, sa référence et sa règle.
  // statut : "above" / "below" (intervalle qui exclut la référence), "up" / "down" (|z| >= 2), "flat" (critère non franchi),
  // "noReference" (seuil atteint, référence trop petite pour comparer).
  function observations(games, journalSummary) {
    const out = [];
    // Champions : 30 parties sur le champion, 30 sur ses autres champions du même rôle ; Wilson 95 %.
    C_ROLES.forEach(role => {
      const inRole = games.filter(g => g.role === role);
      byChampion(inRole).filter(c => c.games >= THRESHOLDS.champion).forEach(c => {
        const ref = inRole.filter(g => g.champ !== c.champ), iv = wilson(c.wins, c.games);
        const base = { kind: "champion", subject: c.champ, role, measure: { wins: c.wins, games: c.games, rate: c.wins / c.games, interval: iv }, rule: { min: THRESHOLDS.champion, ref: THRESHOLDS.reference, level: 95 } };
        if (ref.length < THRESHOLDS.reference) { out.push(Object.assign(base, { status: "noReference", reference: { games: ref.length } })); return; }
        const refRate = ref.filter(g => g.win).length / ref.length;
        out.push(Object.assign(base, { reference: { games: ref.length, rate: refRate }, status: iv[0] > refRate ? "above" : iv[1] < refRate ? "below" : "flat" }));
      });
    });
    // Tendance : rôle principal, 40 parties, |z| >= 2 entre les deux moitiés.
    const main = mainRole(games), inMain = main ? games.filter(g => g.role === main) : [];
    if (inMain.length >= THRESHOLDS.trend) {
      const h = halves(inMain);
      if (h) out.push({ kind: "trend", subject: main, role: main, measure: h, rule: { min: THRESHOLDS.trend, z: THRESHOLDS.trendZ },
        status: Math.abs(h.z) >= THRESHOLDS.trendZ ? (h.z > 0 ? "up" : "down") : "flat" });
    }
    // Vis-à-vis mid : 10 parties contre le même adversaire ; Wilson 99 % dès que plusieurs adversaires sont testés.
    const m = matchups(games, { role: "mid" }), tested = m.opponents.filter(o => o.games >= THRESHOLDS.opponent);
    const midAll = games.filter(g => g.role === "mid" && g.mid), refMid = midAll.length ? midAll.filter(g => g.win).length / midAll.length : null;
    const z = tested.length > 1 ? 2.576 : 1.96;
    tested.forEach(o => {
      const iv = wilson(o.wins, o.games, z);
      out.push({ kind: "opponent", subject: o.name, role: "mid", measure: { wins: o.wins, games: o.games, rate: o.wins / o.games, interval: iv },
        reference: { games: midAll.length, rate: refMid }, rule: { min: THRESHOLDS.opponent, level: z > 2 ? 99 : 95, tested: tested.length },
        status: iv[0] > refMid ? "above" : iv[1] < refMid ? "below" : "flat" });
    });
    // Conseil suivi ou autre choix : 10 parties terminées dans chaque groupe ; intervalles qui se recouvrent ou non. Jamais une cause.
    const j = journalSummary || journal([]), f = j.followed, o = j.other;
    if (f.games >= THRESHOLDS.advice && o.games >= THRESHOLDS.advice) {
      const a = wilson(f.wins, f.games), b = wilson(o.wins, o.games);
      out.push({ kind: "advice", subject: null, measure: { followed: Object.assign({ interval: a }, f), other: Object.assign({ interval: b }, o) }, rule: { min: THRESHOLDS.advice, level: 95 },
        status: a[0] > b[1] ? "above" : a[1] < b[0] ? "below" : "flat" });
    }
    return out;
  }
  const C_ROLES = ROLES;
  root.STATS_CALC = Object.freeze({ MIN_GAMES, SLICE, MS_FROM, REMAKE_SEC, POSITIONS, ROLE_OF, ROLES, UNKNOWN,
    THRESHOLDS, pickSource, readHistory, record, kda, summary, byRole, byChampion, timeline, matchups, journal, originOf, followedOf, gameIdOf, riotGameId, progress, observations, wilson, halves, mainRole });
})(typeof window !== "undefined" ? window : this);
