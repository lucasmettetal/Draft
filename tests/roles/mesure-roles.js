// Mesure de l'attribution des rôles adverses, lancée par tools/run-accuracy-tests.ps1.
// Lit les drafts figés (ROLE_DRAFTS) et les cas atypiques (ROLE_CASES). Appelle le moteur tel quel : aucun réglage n'est changé.
// Vérité : pour chaque équipe, t[r] est le champion du rôle r (top, jungle, mid, adc, support).
// Sur la réserve, ce rôle est le teamPosition donné par Riot pour la partie (src/RoleRatesUpdater.cs, AddHoldout).
(function () {
  const D = window.ROLE_DRAFTS;
  const R = ROLE_ORDER;
  // Seuils de MESURE, pas d'affichage : probable si p ≥ 0,6, possible si 0,2 ≤ p < 0,6, écarté sous 0,2.
  const PROBABLE = .6, POSSIBLE = .2, EPS = 1e-9;
  const classOf = p => p >= PROBABLE ? "probable" : p >= POSSIBLE ? "possible" : "ecarte";
  const perms = [];
  (function walk(a, rest) { if (!rest.length) { perms.push(a); return; } rest.forEach((x, i) => walk(a.concat(x), rest.filter((_, k) => k !== i))); })([], [0, 1, 2, 3, 4]);
  const bins = () => Array.from({ length:10 }, () => ({ n:0, p:0, y:0 }));
  const addBin = (b, p, y) => { const k = Math.min(9, Math.floor(p * 10)); b[k].n++; b[k].p += p; b[k].y += y; };
  const classes = () => ({ probable:{ n:0, y:0 }, possible:{ n:0, y:0 }, ecarte:{ n:0, y:0 } });
  // Crédit partagé en cas d'égalité exacte : le moteur ne peut pas trancher, on compte la chance d'un tirage au sort.
  const credit = (probs, truth) => {
    const best = Math.max(...probs), tied = probs.map((p, i) => i).filter(i => best - probs[i] <= EPS);
    return { ok: tied.includes(truth) ? 1 / tied.length : 0, tie: tied.length > 1 };
  };

  const out = { source:D.source, synthetique:!!D.synthetique, equipes:D.teams.length, ecartees:D.skipped || 0, seuils:{ probable:PROBABLE, possible:POSSIBLE } };
  const sizes = {}, profiles = {}, champs = {};
  for (let s = 1; s <= 5; s++) {
    sizes[s] = { n:0, role:R.map(() => ({ n:0, ok:0, ties:0 })), brier:0, logloss:0, calib:bins(), pairs:classes(),
      status:{ probable:{ n:0, ok:0 }, indetermine:{ n:0, ok:0 } }, complete:{ n:0, ok:0, ties:0 }, completeRole:R.map(() => 0) };
  }
  R.forEach(role => {
    profiles[role] = {};
    for (let s = 1; s <= 5; s++) profiles[role][s] = { n:0, revealed:0, conf:{ "faible":{ n:0, ok:0 }, "moyenne":{ n:0, ok:0 }, "élevée":{ n:0, ok:0 } },
      named:0, namedOk:0, hidden:0, hiddenNamed:0, covered:0, top1:0, slot:classes(), brier:0, logloss:0, calib:bins() };
  });

  D.teams.forEach(d => {
    for (let s = 1; s <= 5; s++) {
      const idx = d.o.slice(0, s), shown = idx.map(r => d.t[r]);
      const b = roleBelief(shown);
      const S = sizes[s];
      S.n++;
      // 1. Rôle par rôle : le 1er candidat (ou « pas encore choisi ») est-il le vrai titulaire ?
      R.forEach((_, r) => {
        const probs = shown.map((_, i) => b.marginals[i][r]).concat(b.vacant[r]);
        const truth = idx.indexOf(r) >= 0 ? idx.indexOf(r) : shown.length;
        const c = credit(probs, truth);
        S.role[r].n++; S.role[r].ok += c.ok; if (c.tie) S.role[r].ties++;
        let brier = 0;
        probs.forEach((p, o) => { const y = o === truth ? 1 : 0; brier += (p - y) * (p - y); addBin(S.calib, p, y); });
        S.brier += brier; S.logloss += -Math.log(Math.max(probs[truth], 1e-9));
      });
      // 2. Champion par champion : statut de son rôle et justesse.
      shown.forEach((name, i) => {
        const row = b.marginals[i], truth = idx[i], c = credit(row, truth);
        const st = Math.max(...row) >= PROBABLE ? "probable" : "indetermine";
        S.status[st].n++; S.status[st].ok += c.ok;
        row.forEach((p, r) => { const k = classOf(p); S.pairs[k].n++; S.pairs[k].y += r === truth ? 1 : 0; });
        const key = name + "|" + R[truth];
        const ch = champs[key] || (champs[key] = { name, role:R[truth], n:0, ok:0, p:0, n5:0, ok5:0, p5:0, top:R.map(() => 0) });
        ch.n++; ch.ok += c.ok; ch.p += row[truth];
        if (s === 5) { ch.n5++; ch.ok5 += c.ok; ch.p5 += row[truth]; }
        const best = Math.max(...row); row.forEach((p, r) => { if (best - p <= EPS) ch.top[r]++; });
      });
      // 3. Équipe complète : la répartition la plus probable des 120 est-elle entièrement juste ?
      if (s === 5) {
        const w = perms.map(p => p.reduce((m, r, k) => m * b.evidence[k].shares[r], 1));
        const best = Math.max(...w), tied = perms.filter((_, k) => best - w[k] <= best * 1e-9);
        const right = tied.some(p => p.every((r, k) => r === idx[k]));
        S.complete.n++; S.complete.ok += right ? 1 / tied.length : 0; if (tied.length > 1) S.complete.ties++;
        R.forEach((_, r) => { S.completeRole[r] += tied.reduce((acc, p) => acc + (p[idx.indexOf(r)] === r ? 1 : 0), 0) / tied.length; });
      }
      // 4. Adversaire direct, pour chacun des cinq rôles du joueur.
      R.forEach((role, ri) => {
        applyProfile({ role, pool:["Garen"] });
        const P = profiles[role][s];
        const ob = opponentBelief(shown), id = identifiedOpponent(shown), sc = beliefScenarios(ob);
        const at = idx.indexOf(ri), truth = at >= 0 ? shown[at] : null;
        P.n++; if (truth) P.revealed++; else P.hidden++;
        const C = P.conf[ob.confidence]; C.n++; if (id && id === truth) C.ok++;
        if (id) { P.named++; if (id === truth) P.namedOk++; if (!truth) P.hiddenNamed++; }
        if (sc.some(x => x.mid === truth)) P.covered++;
        const opts = ob.candidates.map(c => ({ name:c.name, p:c.p })).concat({ name:null, p:ob.unrevealed });
        const probs = opts.map(o => o.p), tIndex = opts.findIndex(o => o.name === truth);
        P.top1 += credit(probs, tIndex).ok;
        const top = ob.candidates[0];
        if (top) { const k = classOf(top.p); P.slot[k].n++; P.slot[k].y += top.name === truth ? 1 : 0; }
        let brier = 0;
        probs.forEach((p, o) => { const y = o === tIndex ? 1 : 0; brier += (p - y) * (p - y); addBin(P.calib, p, y); });
        P.brier += brier; P.logloss += -Math.log(Math.max(probs[tIndex], 1e-9));
      });
    }
  });
  out.tailles = sizes; out.profils = profiles;
  out.champions = Object.values(champs).sort((a, b) => a.name.localeCompare(b.name, "fr") || R.indexOf(a.role) - R.indexOf(b.role));

  // 5. Champions flex : selon les parts du modèle (fiche écrite, et calcul live s'il est chargé).
  applyProfile({ role:"mid", pool:["Garen"] });
  const shares = {};
  NAMES.forEach(n => { const e = roleEvidence(n); shares[n] = { s:e.shares.map(v => +v.toFixed(4)), q:e.quality }; });
  out.flexModele = NAMES.filter(n => shares[n].s.filter(v => v >= POSSIBLE).length >= 2)
    .map(n => ({ name:n, roles:R.filter((_, r) => shares[n].s[r] >= POSSIBLE), parts:shares[n].s }));
  out.flexFiche = Object.keys(ROLE_SHEET).filter(n => ROLE_SHEET[n].length >= 2).map(n => ({ name:n, code:ROLE_SHEET[n] }));
  out.ficheUnRole = Object.keys(ROLE_SHEET).filter(n => ROLE_SHEET[n].length === 1).length;
  out.ficheTotal = Object.keys(ROLE_SHEET).length;
  out.sansFiche = NAMES.filter(n => !ROLE_SHEET[n]);
  // Égalités exactes : parts identiques, donc impossibles à départager sans autre information.
  const groups = {};
  NAMES.forEach(n => { const k = shares[n].s.join(","); (groups[k] = groups[k] || []).push(n); });
  out.egalites = Object.keys(groups).filter(k => groups[k].length >= 2)
    .map(k => ({ parts:k.split(",").map(Number), champions:groups[k] })).sort((a, b) => b.champions.length - a.champions.length);
  const FOCUS = ["Kog'Maw", "Xerath", "Lux", "Quinn", "Seraphine", "Senna", "Twitch", "Jinx", "Darius", "Pantheon", "Sylas"];
  out.focus = FOCUS.map(n => ({ name:n, parts:shares[n] ? shares[n].s : null, qualite:shares[n] ? shares[n].q : null }));
  out.live = window.ROLE_RATES_LIVE ? { charge:true, synthetique:!!window.ROLE_RATES_LIVE.synthetique, parties:window.ROLE_RATES_LIVE.games || null } : { charge:false };

  // 6. Cas atypiques SYNTHÉTIQUES : accord avec les rôles annotés à la main.
  out.cas = (typeof ROLE_CASES !== "undefined" ? ROLE_CASES : []).map(c => {
    applyProfile({ role:c.profil, pool:["Garen"] });
    const b = roleBelief(c.equipe, c.corrections);
    const ob = opponentBelief(c.equipe, c.corrections), id = identifiedOpponent(c.equipe, c.corrections);
    const expectedOpp = c.equipe.find(n => c.attendu[n] === c.profil) || null;
    const rows = c.equipe.map((n, i) => {
      const row = b.marginals[i], want = c.attendu[n] ? R.indexOf(c.attendu[n]) : -1;
      return { name:n, attendu:c.attendu[n] || null, pAttendu: want >= 0 ? +row[want].toFixed(3) : null,
        ok: want >= 0 ? credit(row, want).ok : null, classe: want >= 0 ? classOf(row[want]) : null };
    });
    return { id:c.id, profil:c.profil, synthetique:c.synthetique === true, taille:c.equipe.length, corrections:c.corrections || null,
      lignes:rows, confiance:ob.confidence, identifie:id, adversaireAttendu:expectedOpp,
      adversaireJuste: id === expectedOpp, nommeATort: !!id && id !== expectedOpp };
  });
  document.getElementById("out").textContent = JSON.stringify(out);
})();
