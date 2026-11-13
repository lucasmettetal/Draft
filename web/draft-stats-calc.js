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

  // Origine d'une entrée du journal. Marque de la #33 : origin "draft". Choix du joueur pour une entrée sans marque : userOrigin.
  // Un champ absent est inconnu : rien n'est déduit, rien n'est supprimé.
  function originOf(e) {
    if (e.origin === "draft") return "draft";
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

  // Journal DRAFT : statistiques sur les entrées réelles ou confirmées réelles seulement ; « conseil suivi » sur les parties terminées.
  function journal(entries) {
    const all = (Array.isArray(entries) ? entries : []).filter(e => e && typeof e === "object");
    const finished = e => e.result === "win" || e.result === "loss", won = e => e.result === "win";
    const counted = all.filter(e => { const o = originOf(e); return o === "draft" || o === "real"; });
    const done = counted.filter(finished);
    const dates = counted.map(e => Date.parse(e.date)).filter(isFinite).sort((a, b) => a - b);
    const time = e => Date.parse(e.date) || e.id || 0;
    return {
      total: all.length, drafts: counted.length,
      unknown: all.filter(e => originOf(e) === UNKNOWN).length, simulations: all.filter(e => originOf(e) === "simulation").length,
      finished: record(done, won),
      pending: counted.filter(e => e.result === null || e.result === undefined).length,
      skipped: counted.filter(e => e.result === "skip").length,
      followed: record(done.filter(followedOf), won), other: record(done.filter(e => !followedOf(e)), won),
      ties: counted.filter(e => e.tied === true).length,
      period: dates.length ? { from: dates[0], to: dates[dates.length - 1] } : null,
      entries: all.slice().sort((a, b) => time(b) - time(a)).map(e => {
        const origin = originOf(e);
        return { entry: e, origin, counted: origin === "draft" || origin === "real", followed: followedOf(e), tied: e.tied === true, finished: finished(e) };
      })
    };
  }

  // Progression vers les seuils de l'étude (COACHING-ARCHITECTURE.md). Aucune conclusion, même seuil atteint.
  const THRESHOLDS = Object.freeze({ champion: 30, trend: 40, opponent: 10, advice: MIN_GAMES });
  function progress(games, journalSummary) {
    const champs = byChampion(games), main = mainRole(games), mids = matchups(games, { role: "mid" }).opponents;
    const j = journalSummary || journal([]);
    return {
      champion: { have: champs.length ? champs[0].games : 0, need: THRESHOLDS.champion, subject: champs.length ? champs[0].champ : null },
      trend: { have: main ? games.filter(g => g.role === main).length : 0, need: THRESHOLDS.trend, subject: main },
      opponent: { have: mids.length ? mids[0].games : 0, need: THRESHOLDS.opponent, subject: mids.length ? mids[0].name : null },
      advice: { have: Math.min(j.followed.games, j.other.games), need: THRESHOLDS.advice, followed: j.followed.games, other: j.other.games }
    };
  }

  root.STATS_CALC = Object.freeze({ MIN_GAMES, SLICE, MS_FROM, REMAKE_SEC, POSITIONS, ROLE_OF, ROLES, UNKNOWN,
    THRESHOLDS, pickSource, readHistory, record, kda, summary, byRole, byChampion, timeline, matchups, journal, originOf, followedOf, progress, mainRole });
})(typeof window !== "undefined" ? window : this);
