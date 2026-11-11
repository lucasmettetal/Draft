// Prépare les drafts figés de la mesure des rôles, lancé par tools/run-accuracy-tests.ps1. Graine fixe : mêmes drafts à chaque passage.
// mode "fiche" : équipes SYNTHÉTIQUES tirées depuis la fiche écrite (générateur = modèle : borne haute, pas la réalité).
// mode "reserve" : vraies équipes mises de côté par le calcul des rôles, rangées par teamPosition. Seul l'ordre de révélation est tiré.
(function () {
  const G = window.ROLE_GEN;
  function mulberry(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
  const rng = mulberry(G.seed);
  const shuffle = () => { const o = [0, 1, 2, 3, 4]; for (let j = 4; j > 0; j--) { const k = Math.floor(rng() * (j + 1)); [o[j], o[k]] = [o[k], o[j]]; } return o; };
  const teams = [];
  let skipped = 0, unknown = 0;
  if (G.mode === "reserve") {
    // Les noms de Riot (championName) deviennent des clés minuscules sans ponctuation, comme riotKey().
    const byKey = {};
    NAMES.forEach(n => { byKey[riotKey(n)] = n; });
    G.holdout.forEach(team => {
      if (!Array.isArray(team) || team.length !== 5 || team.some(k => typeof k !== "string" || !k)) { skipped++; return; }
      const t = team.map(k => byKey[k]);
      if (t.some(n => !n)) { skipped++; unknown++; return; }
      if (new Set(t).size !== 5) { skipped++; return; }
      teams.push({ t, o:shuffle() });
    });
    // Au-delà du maximum demandé, on garde un échantillon tiré avec la même graine.
    while (G.max && teams.length > G.max) teams.splice(Math.floor(rng() * teams.length), 1);
  } else {
    const W = [1, .35, .15];
    const byRole = ROLE_ORDER.map((_, r) => Object.keys(ROLE_SHEET).filter(n => NAMES.includes(n))
      .map(n => { const i = ROLE_SHEET[n].indexOf("tjmas"[r]); return { n, w: i >= 0 ? (W[i] || .1) : 0 }; }).filter(x => x.w > 0));
    const draw = r => { const l = byRole[r], tot = l.reduce((s, x) => s + x.w, 0); let x = rng() * tot; for (const c of l) { x -= c.w; if (x < 0) return c.n; } return l[l.length - 1].n; };
    while (teams.length < G.count) { const t = [0, 1, 2, 3, 4].map(draw); if (new Set(t).size === 5) teams.push({ t, o:shuffle() }); }
  }
  document.getElementById("out").textContent = JSON.stringify({ source:G.mode, synthetique:G.mode !== "reserve" || !!G.synthetique, teams, skipped, unknown });
})();
