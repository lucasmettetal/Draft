// Données FICTIVES des maquettes de « Mes statistiques ». Champions, dates, comptes et résultats sont inventés.
// Même forme que draft-history.js (window.DRAFT_HISTORY) et que le journal (localStorage draftJournal).
// Déterministe : même graine, mêmes données. Formes visées : voir docs/stats/RECOMMANDATION.md, § Données fictives.
(function () {
  "use strict";
  const DAY = 86400000;

  // Générateur pseudo-aléatoire à graine (mulberry32).
  function rng(seed) {
    return function () {
      seed = (seed + 0x6D2B79F5) | 0;
      let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }
  const between = (r, lo, hi) => lo + Math.floor(r() * (hi - lo + 1));
  function shuffle(list, r) {
    const a = list.slice();
    for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(r() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; }
    return a;
  }

  // Calendrier fictif des patchs : 16.1 le 8 janvier 2026, puis un patch toutes les deux semaines.
  function patchStart(patch) {
    const [major, minor] = patch.split(".").map(Number);
    if (major === 16) return Date.UTC(2026, 0, 8) + (minor - 1) * 14 * DAY;
    return Date.UTC(2025, 11, 10) - (24 - minor) * 14 * DAY;
  }

  // Adversaires possibles par poste (noms de l'API Riot, comme dans l'import).
  const POOLS = {
    TOP: ["Darius", "Aatrox", "Ornn", "Jax", "Fiora", "Camille", "Renekton", "Gwen", "KSante", "Mordekaiser", "Shen", "Teemo", "Gragas", "Riven"],
    JUNGLE: ["Viego", "Kindred", "JarvanIV", "XinZhao", "Hecarim", "Briar", "Elise", "Nocturne", "Warwick", "Khazix", "Rengar", "Nidalee"],
    BOTTOM: ["Jinx", "Kaisa", "Ezreal", "Caitlyn", "Jhin", "Ashe", "MissFortune", "Varus", "Lucian", "Xayah", "Draven", "Vayne", "Sivir"],
    UTILITY: ["Thresh", "Nautilus", "Leona", "Nami", "Rakan", "Karma", "Milio", "Renata", "Braum", "Pyke", "Bard", "Rell", "Soraka"],
    MIDDLE: ["Yasuo", "Akali", "Zed", "Sylas", "Leblanc", "Katarina", "Yone", "Hwei", "Aurora", "Galio", "Diana", "Kassadin", "Talon", "Malzahar",
      "Cassiopeia", "Veigar", "Ekko", "Qiyana", "Zoe", "Anivia", "Naafiri", "Akshan", "Azir", "Brand", "Corki", "Irelia", "Lissandra", "Mel", "Neeko",
      "Pantheon", "Ryze", "Smolder", "Taliyah", "Tristana", "Vladimir", "Ziggs", "AurelionSol", "Kennen", "Lux", "Annie", "Velkoz", "Swain", "Seraphine"]
  };
  const KDA = {
    MIDDLE: [[4, 12, 1, 6, 4, 12], [1, 8, 4, 10, 2, 9]], JUNGLE: [[4, 11, 2, 6, 6, 14], [1, 7, 4, 9, 3, 10]],
    TOP: [[4, 10, 2, 6, 3, 9], [1, 6, 4, 9, 1, 7]], BOTTOM: [[6, 13, 1, 5, 4, 10], [2, 8, 4, 9, 2, 7]],
    UTILITY: [[0, 4, 1, 5, 12, 24], [0, 3, 4, 9, 5, 14]]
  };

  // plan : { seed, account, champions:[[clé, poste, parties, victoires]], sessions:[[patch, parties, début?, fin?]],
  //          duels:[[adversaire, parties, victoires]], tail:[[adversaire, parties]], trendBias, meanSec }
  function riotHistory(plan) {
    const r = rng(plan.seed);
    let slots = [];
    plan.champions.forEach(([champ, pos, games, wins]) => {
      for (let i = 0; i < games; i++) slots.push({ champ, pos, win: i < wins });
    });
    // Ordre chronologique : au hasard. trendBias : part des victoires en mid tirées avec une densité croissante (plus fréquentes vers la fin).
    slots = slots.map(s => ({ s, key: plan.trendBias && s.pos === "MIDDLE" && s.win && r() < plan.trendBias ? Math.sqrt(r()) : r() }))
      .sort((a, b) => a.key - b.key).map(x => x.s);
    const total = slots.length;
    const times = [], patches = [];
    plan.sessions.forEach(([patch, count, from, to]) => {
      const start = from ? Date.parse(from) : patchStart(patch), end = to ? Date.parse(to) : start + 12 * DAY;
      const days = Math.max(1, Math.round((end - start) / DAY));
      const list = [];
      for (let i = 0; i < count; i++) {
        const day = Math.floor((i * (days + 1)) / count);
        list.push(start + day * DAY + (17 + between(r, 0, 5)) * 3600000 + between(r, 0, 59) * 60000);
      }
      list.sort((a, b) => a - b).forEach(t => { times.push(t); patches.push(patch); });
    });
    if (times.length !== total) throw new Error("sessions : " + times.length + " parties pour " + total + " lignes");
    // Durées : tirées puis ajustées pour la moyenne visée (en secondes).
    const secs = slots.map(() => between(r, 1380, 2640));
    if (plan.meanSec) {
      let diff = plan.meanSec * total - secs.reduce((a, b) => a + b, 0);
      for (let i = 0; diff !== 0; i = (i + 1) % total) {
        const step = Math.sign(diff) * Math.min(Math.abs(diff), 37);
        const next = secs[i] + step;
        if (next >= 1260 && next <= 2820) { secs[i] = next; diff -= step; }
      }
    }
    // Vis-à-vis en mid : d'abord les duels imposés (victoires et défaites choisies), puis la longue traîne.
    const mids = slots.map((s, i) => (s.pos === "MIDDLE" ? i : -1)).filter(i => i >= 0);
    const opponent = {};
    const free = shuffle(mids, r);
    (plan.duels || []).forEach(([name, games, wins]) => {
      let w = wins, l = games - wins;
      free.slice().forEach(i => {
        if (opponent[i] !== undefined || slots[i].champ === name) return;
        if (slots[i].win && w > 0) { opponent[i] = name; w--; } else if (!slots[i].win && l > 0) { opponent[i] = name; l--; }
      });
      if (w || l) throw new Error("duel impossible : " + name);
    });
    const tail = [];
    (plan.tail || []).forEach(([name, games]) => { for (let i = 0; i < games; i++) tail.push(name); });
    const left = free.filter(i => opponent[i] === undefined);
    if (tail.length !== left.length) throw new Error("traîne : " + tail.length + " adversaires pour " + left.length + " parties en mid");
    shuffle(tail, r).forEach((name, k) => { opponent[left[k]] = name; });
    left.forEach(i => { // Un adversaire ne peut pas être le champion joué : échange avec une autre partie.
      if (opponent[i] !== slots[i].champ) return;
      const j = left.find(x => opponent[x] !== slots[i].champ && opponent[i] !== slots[x].champ);
      [opponent[i], opponent[j]] = [opponent[j], opponent[i]];
    });
    const rows = slots.map((s, i) => {
      const taken = new Set([s.champ]);
      const pick = (pool, fixed) => {
        if (fixed && !taken.has(fixed)) { taken.add(fixed); return fixed; }
        let n; do { n = pool[Math.floor(r() * pool.length)]; } while (taken.has(n));
        taken.add(n); return n;
      };
      const mid = pick(POOLS.MIDDLE, opponent[i]), top = pick(POOLS.TOP), jungle = pick(POOLS.JUNGLE), bot = pick(POOLS.BOTTOM), sup = pick(POOLS.UTILITY);
      const k = KDA[s.pos][s.win ? 0 : 1];
      return {
        id: "FICTIF-" + plan.tag + "-" + String(i + 1).padStart(4, "0"), acct: plan.account, time: times[i], patch: patches[i], sec: secs[i],
        champ: s.champ, pos: s.pos, win: s.win, k: between(r, k[0], k[1]), d: between(r, k[2], k[3]), a: between(r, k[4], k[5]),
        mid, enemies: [top, jungle, mid, bot, sup]
      };
    });
    return { generated: plan.generated, accounts: [plan.account], skipped: [], rows: rows.sort((a, b) => b.time - a.time) };
  }

  // Journal : entrées dans le format de choosePick. Après fix/choix-champion, le Simulateur n'écrit plus rien dans le journal.
  // « app » : marque PROPOSÉE (version qui a écrit l'entrée), absente aujourd'hui. Sans elle, une entrée ancienne peut être une simulation.
  function journal(plan) {
    const r = rng(plan.seed);
    const list = [];
    plan.entries.forEach((e, n) => {
      const time = Date.parse(e.date);
      const enemies = [POOLS.TOP[n % 14], POOLS.JUNGLE[(n * 5) % 12], e.mid, POOLS.BOTTOM[(n * 3) % 13], POOLS.UTILITY[(n * 7) % 13]];
      list.push({ id: time, date: new Date(time).toISOString(), suggested: e.suggested, close: !!e.close, picked: e.picked,
        enemies, mid: e.mid, result: e.result === undefined ? null : e.result, engine: "2.2" });
      if (plan.app) list[list.length - 1].app = plan.app;
    });
    r();
    return list.sort((a, b) => a.id - b.id);
  }

  // ---------- Profil de référence : 93 parties, un compte, environ 7,5 mois, 6 patchs ----------
  const REF = riotHistory({
    seed: 2026, tag: "A", account: "Joueur#FICTIF", generated: "2026-11-09T10:00:00.000Z", meanSec: 1920,
    champions: [
      ["Orianna", "MIDDLE", 22, 11], ["Ahri", "MIDDLE", 16, 6], ["Viktor", "MIDDLE", 13, 7], ["Syndra", "MIDDLE", 11, 4],
      ["Vex", "MIDDLE", 6, 3], ["Lux", "MIDDLE", 2, 1], ["Annie", "MIDDLE", 1, 0],
      ["LeeSin", "JUNGLE", 7, 3], ["Vi", "JUNGLE", 3, 1], ["Amumu", "JUNGLE", 3, 2], ["Graves", "JUNGLE", 2, 0], ["Kayn", "JUNGLE", 1, 1], ["Nunu", "JUNGLE", 1, 0],
      ["Malphite", "TOP", 2, 1], ["Garen", "TOP", 1, 1], ["Sett", "TOP", 1, 0], ["Lulu", "UTILITY", 1, 1]
    ],
    sessions: [["16.6", 14, "2026-03-25", "2026-04-01"], ["16.7", 18], ["16.11", 12], ["16.15", 9], ["16.20", 22], ["16.22", 18, "2026-10-29", "2026-11-08"]],
    tail: [["Yasuo", 5], ["Akali", 4], ["Zed", 4], ["Sylas", 3], ["Leblanc", 3], ["Katarina", 3], ["Yone", 3],
      ...["Hwei", "Aurora", "Galio", "Diana", "Kassadin", "Talon", "Malzahar", "Cassiopeia", "Veigar", "Ekko", "Qiyana", "Zoe", "Anivia", "Naafiri"].map(n => [n, 2]),
      ...["Akshan", "Azir", "Brand", "Corki", "Irelia", "Lissandra", "Mel", "Neeko", "Pantheon", "Ryze", "Smolder", "Taliyah", "Tristana", "Vladimir", "Ziggs", "AurelionSol", "Kennen", "Velkoz"].map(n => [n, 1])]
  });

  // ---------- Historique riche : 400 parties sur 29 patchs ----------
  const richSessions = [];
  ["15.18", "15.19", "15.20", "15.21", "15.22", "15.23", "15.24"].concat(Array.from({ length: 22 }, (_, i) => "16." + (i + 1))).forEach((p, i) => {
    richSessions.push(p === "16.22" ? [p, 0, "2026-10-29", "2026-11-10"] : [p, 0]);
  });
  const richCounts = [9, 11, 14, 12, 16, 10, 8, 12, 15, 17, 13, 14, 16, 12, 10, 9, 14, 18, 15, 13, 11, 12, 16, 17, 14, 15, 13, 17, 16];
  for (let i = richCounts.length - 1, missing = 400 - richCounts.reduce((a, b) => a + b, 0); missing > 0; i--, missing--) richCounts[i]++;
  richSessions.forEach((s, i) => { s[1] = richCounts[i]; });
  // Longue traîne : 238 parties en mid sur 36 adversaires, au plus 9 contre le même (aucun n'atteint 10).
  const tailNames = POOLS.MIDDLE.filter(n => !["Yasuo", "Akali", "Leblanc", "Zed", "Sylas", "Lux", "Annie"].includes(n));
  const tailCounts = tailNames.map(() => 1);
  let rest = 238 - tailNames.length;
  while (rest > 0) tailNames.forEach((_, i) => { if (rest > 0 && tailCounts[i] < 9 - Math.floor(i / 7)) { tailCounts[i]++; rest--; } });
  const richTail = tailNames.map((n, i) => [n, tailCounts[i]]);
  const RICH = riotHistory({
    seed: 4242, tag: "B", account: "Joueur#FICTIF", generated: "2026-11-11T09:00:00.000Z", meanSec: 1890, trendBias: 0.3,
    champions: [
      ["Orianna", "MIDDLE", 95, 60], ["Ahri", "MIDDLE", 70, 36], ["Viktor", "MIDDLE", 55, 29], ["Syndra", "MIDDLE", 40, 13],
      ["Vex", "MIDDLE", 25, 13], ["Lux", "MIDDLE", 8, 4], ["Annie", "MIDDLE", 7, 4],
      ["LeeSin", "JUNGLE", 30, 15], ["Vi", "JUNGLE", 15, 8], ["Amumu", "JUNGLE", 10, 5], ["Graves", "JUNGLE", 5, 2],
      ["Malphite", "TOP", 15, 8], ["Garen", "TOP", 6, 3], ["Sett", "TOP", 4, 2], ["Jinx", "BOTTOM", 5, 2], ["Lulu", "UTILITY", 10, 4]
    ],
    sessions: richSessions,
    duels: [["Yasuo", 16, 6], ["Akali", 13, 5], ["Leblanc", 12, 1], ["Zed", 11, 7], ["Sylas", 10, 6]],
    tail: richTail
  });

  // ---------- Journaux ----------
  // Cinq drafts sans résultat, aucune où le pick joué est le conseil (situation de référence).
  const FIVE = journal({ seed: 5, entries: [
    { date: "2026-11-05T19:12:00Z", suggested: "Orianna", picked: "Ahri", mid: "Yasuo", close: true },
    { date: "2026-11-07T20:40:00Z", suggested: "Viktor", picked: "Orianna", mid: "Akali" },
    { date: "2026-11-09T18:05:00Z", suggested: "Ahri", picked: "Syndra", mid: "LeBlanc", close: true },
    { date: "2026-11-11T21:22:00Z", suggested: "Orianna", picked: "Viktor", mid: "Zed" },
    { date: "2026-11-12T19:48:00Z", suggested: "Syndra", picked: "Ahri", mid: "Hwei" }
  ] });
  // Journal riche : 64 drafts, 60 avec résultat (44 conseils joués : 25 V ; 16 autres choix : 7 V), 2 ignorées, 2 en attente.
  const richEntries = [];
  const jr = rng(77), pool = ["Orianna", "Ahri", "Viktor", "Syndra"], opps = ["Yasuo", "Akali", "LeBlanc", "Zed", "Sylas", "Hwei", "Aurora", "Galio", "Diana", "Katarina", "Yone", "Talon", "Malzahar", "Veigar"];
  const kinds = [];
  for (let i = 0; i < 44; i++) kinds.push({ followed: true, result: i < 25 ? "win" : "loss" });
  for (let i = 0; i < 16; i++) kinds.push({ followed: false, result: i < 7 ? "win" : "loss" });
  kinds.push({ followed: true, result: "skip" }, { followed: false, result: "skip" }, { followed: true, result: null }, { followed: true, result: null });
  const order = shuffle(kinds.slice(0, 62), jr).concat(kinds.slice(62));
  const t0 = Date.parse("2026-08-12T18:00:00Z");
  order.forEach((k, i) => {
    const suggested = pool[Math.floor(jr() * (jr() < 0.45 ? 1 : 4))];
    const picked = k.followed ? suggested : pool.filter(p => p !== suggested)[Math.floor(jr() * 3)];
    const time = t0 + Math.round(i * (92 / 64) * DAY) + Math.floor(jr() * 4) * 3600000;
    richEntries.push({ date: new Date(time).toISOString(), suggested, picked, mid: opps[Math.floor(jr() * opps.length)], close: jr() < 0.28, result: k.result });
  });
  const RICH_JOURNAL = journal({ seed: 64, app: "3.0.0", entries: richEntries });

  window.MAQ_DONNEES = {
    pool: ["Orianna", "Ahri", "Viktor", "Syndra"],
    etats: {
      vide: { history: null, journal: [] },
      drafts5: { history: null, journal: FIVE },
      riot93: { history: REF, journal: FIVE },
      riche: { history: RICH, journal: RICH_JOURNAL }
    }
  };
})();
