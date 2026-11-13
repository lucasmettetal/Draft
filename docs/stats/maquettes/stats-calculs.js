// Calculs des maquettes de « Mes statistiques » : fonctions pures, sans DOM ni texte.
// Préfigure le module proposé dans docs/stats/COACHING-ARCHITECTURE.md (calcul séparé du rendu).
// Chaque conclusion porte sa preuve : mesure, référence, règle et échantillon. Sous le seuil : « insuffisant », jamais une conclusion.
(function () {
  "use strict";
  // Seuils : ceux de l'application quand ils existent (MIN_GAMES, EVOLUTION_STEP, RESULT_MIN_GAMES, TREND_MIN_GAMES, KDA_MIN_GAMES).
  const SEUILS = Object.freeze({ pourcentage: 10, tranche: 20, champion: 30, reference: 30, tendance: 40, tendanceZ: 2, duel: 10, revu: 3, dureeMax: 20000 });
  const POSTES = ["TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY"];
  const ROLE = { TOP: "top", JUNGLE: "jungle", MIDDLE: "mid", BOTTOM: "adc", UTILITY: "support", "": "none" };

  function wilson(w, n, z) {
    z = z || 1.96;
    if (!n) return [0, 1];
    const p = w / n, d = 1 + z * z / n, c = p + z * z / (2 * n), m = z * Math.sqrt((p * (1 - p) + z * z / (4 * n)) / n);
    return [Math.max(0, (c - m) / d), Math.min(1, (c + m) / d)];
  }
  function bilan(rows, win) {
    win = win || (r => r.win);
    const parties = rows.length, victoires = rows.filter(win).length;
    return { parties, victoires, defaites: parties - victoires, taux: parties >= SEUILS.pourcentage ? victoires / parties : null, brut: parties ? victoires / parties : null };
  }
  function kda(rows) {
    const list = rows.filter(r => typeof r.k === "number" && typeof r.d === "number" && typeof r.a === "number");
    if (!list.length) return null;
    const k = list.reduce((s, r) => s + r.k, 0), d = list.reduce((s, r) => s + r.d, 0), a = list.reduce((s, r) => s + r.a, 0);
    return { parties: list.length, k: k / list.length, d: d / list.length, a: a / list.length, ratio: (k + a) / Math.max(1, d) };
  }
  const patchKey = p => p.split(".").map(Number).reduce((s, x, i) => s + x * (i ? 1 : 1000), 0);
  function echantillon(rows) {
    if (!rows || !rows.length) return null;
    const times = rows.map(r => r.time).filter(t => t > 0).sort((a, b) => a - b);
    const patchs = [...new Set(rows.map(r => r.patch).filter(Boolean))].sort((a, b) => patchKey(a) - patchKey(b));
    return { parties: rows.length, debut: times[0], fin: times[times.length - 1], jours: Math.round((times[times.length - 1] - times[0]) / 86400000),
      patchs, comptes: [...new Set(rows.map(r => r.acct))] };
  }
  function grouper(rows, cle) {
    const map = new Map();
    rows.forEach(r => { const k = cle(r); if (!map.has(k)) map.set(k, []); map.get(k).push(r); });
    return [...map].map(([nom, list]) => ({ nom, rows: list }));
  }
  function parPoste(rows) {
    const g = grouper(rows, r => r.pos || "");
    return POSTES.concat([""]).map(p => { const x = g.find(y => y.nom === p); return { poste: p, role: ROLE[p], ...bilan(x ? x.rows : []) }; }).filter(x => x.parties || x.poste !== "");
  }
  function parChampion(rows) {
    return grouper(rows, r => r.champ).map(({ nom, rows: list }) => {
      const postes = grouper(list, r => r.pos || "").sort((a, b) => b.rows.length - a.rows.length);
      return { champ: nom, poste: postes[0].nom, role: ROLE[postes[0].nom], ...bilan(list), kda: list.length >= SEUILS.pourcentage ? kda(list) : null,
        derniere: Math.max(...list.map(r => r.time || 0)), intervalle: wilson(list.filter(r => r.win).length, list.length) };
    }).sort((a, b) => b.parties - a.parties || a.champ.localeCompare(b.champ));
  }
  function parPatch(rows) {
    return grouper(rows, r => r.patch).map(({ nom, rows: list }) => ({ patch: nom, ...bilan(list) })).sort((a, b) => patchKey(a.patch) - patchKey(b.patch));
  }
  // Toutes les tranches, y compris la dernière, incomplète (l'application l'omet aujourd'hui).
  function tranches(rows, pas) {
    pas = pas || SEUILS.tranche;
    const sorted = rows.slice().sort((a, b) => a.time - b.time), out = [];
    for (let i = 0; i < sorted.length; i += pas) {
      const part = sorted.slice(i, i + pas);
      out.push({ de: i + 1, a: i + part.length, complete: part.length === pas, debut: part[0].time, fin: part[part.length - 1].time, ...bilan(part) });
    }
    return out;
  }
  function duree(rows) {
    const ok = rows.filter(r => r.sec > 0 && r.sec <= SEUILS.dureeMax);
    return ok.length ? { moyenne: ok.reduce((s, r) => s + r.sec, 0) / ok.length, parties: ok.length, exclues: rows.length - ok.length } : null;
  }
  // Vis-à-vis : seulement les parties jouées en mid, où « mid » est bien l'adversaire de voie.
  function vsMid(rows) {
    const list = rows.filter(r => r.pos === "MIDDLE" && r.mid);
    const g = grouper(list, r => r.mid).map(({ nom, rows: l }) => ({ adversaire: nom, ...bilan(l) })).sort((a, b) => b.parties - a.parties || a.adversaire.localeCompare(b.adversaire));
    return { parties: list.length, adversaires: g.length, revus: g.filter(x => x.parties >= SEUILS.revu).length, liste: g, global: bilan(list) };
  }
  // Journal : une seule définition de « conseil joué » (picked === suggested), comptée sur les parties terminées.
  // « simulation » : champ proposé (absent aujourd'hui). Une entrée sans ce champ est d'origine inconnue.
  function journal(entries) {
    const all = entries || [];
    const reelles = all.filter(e => e.simulation !== true);
    const finie = e => e.result === "win" || e.result === "loss", gagne = e => e.result === "win";
    const terminees = reelles.filter(finie);
    const dates = reelles.map(e => Date.parse(e.date)).sort((a, b) => a - b);
    return {
      drafts: reelles.length, simulees: all.length - reelles.length, origineInconnue: reelles.filter(e => e.simulation === undefined).length,
      debut: dates[0] || null, fin: dates[dates.length - 1] || null,
      terminees: bilan(terminees, gagne), enAttente: reelles.filter(e => e.result === null || e.result === undefined).length,
      ignorees: reelles.filter(e => e.result === "skip").length, serrees: reelles.filter(e => e.close).length,
      conseilJoueToutes: reelles.filter(e => e.picked === e.suggested).length,
      suivies: bilan(terminees.filter(e => e.picked === e.suggested), gagne), autres: bilan(terminees.filter(e => e.picked !== e.suggested), gagne),
      dernieres: reelles.slice().sort((a, b) => b.id - a.id)
    };
  }
  // Même règle que performanceTrend (draft-mastery.js) : deux moitiés, au moins 40 parties, |z| >= 2.
  function tendance(rows) {
    if (rows.length < SEUILS.tendance) return { statut: "insuffisant", parties: rows.length };
    const sorted = rows.slice().sort((a, b) => a.time - b.time), half = Math.floor(sorted.length / 2);
    const older = sorted.slice(0, half), newer = sorted.slice(half);
    const p1 = older.filter(r => r.win).length / older.length, p2 = newer.filter(r => r.win).length / newer.length;
    const p = sorted.filter(r => r.win).length / sorted.length, se = Math.sqrt(p * (1 - p) * (1 / older.length + 1 / newer.length));
    const z = se ? (p2 - p1) / se : 0;
    return { statut: Math.abs(z) >= SEUILS.tendanceZ ? "etabli" : "non-concluant", sens: z > 0 ? "hausse" : "baisse", z, avant: { parties: older.length, taux: p1 }, apres: { parties: newer.length, taux: p2 }, parties: rows.length };
  }

  // ---------- Conclusions : chaque élément dit son statut, sa mesure, sa référence, sa règle ----------
  function conclusions(rows, entries) {
    const out = [];
    rows = rows || [];
    const ech = echantillon(rows);
    // 1. Points forts et faibles : champion contre ses autres champions du même poste.
    let candidats = 0;
    POSTES.forEach(pos => {
      const roleRows = rows.filter(r => r.pos === pos);
      parChampion(roleRows).forEach(c => {
        if (c.parties < SEUILS.champion) return;
        candidats++;
        const ref = roleRows.filter(r => r.champ !== c.champ);
        const base = { type: "champion", sujet: c.champ, role: ROLE[pos], mesure: { victoires: c.victoires, parties: c.parties, taux: c.victoires / c.parties, intervalle: c.intervalle },
          regle: { min: SEUILS.champion, z: 1.96, niveau: 95 }, echantillon: ech };
        if (ref.length < SEUILS.reference) { out.push({ ...base, statut: "insuffisant", manque: "reference", reference: { parties: ref.length } }); return; }
        const refRate = ref.filter(r => r.win).length / ref.length;
        base.reference = { taux: refRate, parties: ref.length };
        if (c.intervalle[0] > refRate) out.push({ ...base, statut: "etabli", sens: "fort" });
        else if (c.intervalle[1] < refRate) out.push({ ...base, statut: "etabli", sens: "faible" });
        else out.push({ ...base, statut: "non-concluant" });
      });
    });
    if (!candidats) {
      const top = parChampion(rows)[0];
      out.push({ type: "champion", statut: "insuffisant", sujet: top ? top.champ : null, mesure: { parties: top ? top.parties : 0 }, regle: { min: SEUILS.champion } });
    }
    // 2. Tendance dans le poste le plus joué.
    const postes = parPoste(rows).filter(p => p.poste).sort((a, b) => b.parties - a.parties);
    if (postes.length) {
      const main = postes[0], tr = tendance(rows.filter(r => r.pos === main.poste));
      out.push({ type: "tendance", sujet: main.role, role: main.role, ...tr, regle: { min: SEUILS.tendance, z: SEUILS.tendanceZ }, echantillon: ech });
    } else out.push({ type: "tendance", statut: "insuffisant", parties: 0, regle: { min: SEUILS.tendance } });
    // 3. Duels en mid : plusieurs adversaires testés, donc intervalle à 99 %.
    const vs = vsMid(rows);
    const testes = vs.liste.filter(x => x.parties >= SEUILS.duel);
    if (!testes.length) {
      const top = vs.liste[0];
      out.push({ type: "duel", statut: "insuffisant", sujet: top ? top.adversaire : null, mesure: { parties: top ? top.parties : 0 }, regle: { min: SEUILS.duel } });
    }
    const zDuel = testes.length > 1 ? 2.576 : 1.96, refMid = vs.global.brut;
    testes.forEach(x => {
      const iv = wilson(x.victoires, x.parties, zDuel);
      const base = { type: "duel", sujet: x.adversaire, mesure: { victoires: x.victoires, parties: x.parties, taux: x.victoires / x.parties, intervalle: iv },
        reference: { taux: refMid, parties: vs.parties }, regle: { min: SEUILS.duel, z: zDuel, niveau: zDuel > 2 ? 99 : 95, testes: testes.length }, echantillon: ech };
      if (iv[1] < refMid) out.push({ ...base, statut: "etabli", sens: "difficile" });
      else if (iv[0] > refMid) out.push({ ...base, statut: "etabli", sens: "favorable" });
      else out.push({ ...base, statut: "non-concluant" });
    });
    // 4. Conseil joué contre autre choix : descriptif, jamais une preuve d'efficacité.
    const j = journal(entries);
    if (j.suivies.parties >= SEUILS.pourcentage && j.autres.parties >= SEUILS.pourcentage) {
      const a = wilson(j.suivies.victoires, j.suivies.parties), b = wilson(j.autres.victoires, j.autres.parties);
      out.push({ type: "conseil", statut: a[0] > b[1] || b[0] > a[1] ? "etabli" : "non-concluant", suivies: { ...j.suivies, intervalle: a }, autres: { ...j.autres, intervalle: b },
        regle: { min: SEUILS.pourcentage, z: 1.96 } });
    } else out.push({ type: "conseil", statut: "insuffisant", suivies: j.suivies, autres: j.autres, regle: { min: SEUILS.pourcentage } });
    return out;
  }

  window.MAQ_CALCULS = Object.freeze({ SEUILS, POSTES, ROLE, wilson, bilan, kda, echantillon, parPoste, parChampion, parPatch, tranches, duree, vsMid, journal, tendance, conclusions });
})();
