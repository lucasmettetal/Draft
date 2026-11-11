// Études de décision produit (V2.17), sur le jeu de DÉVELOPPEMENT SYNTHÉTIQUE seulement.
// Aucune formule livrée n'est modifiée : chaque variante remplace RULES.peel ou un poids de CRITERIA EN MÉMOIRE,
// le temps du calcul, puis remet la valeur d'origine. web/draft-engine.js n'est jamais écrit.
// Se charge après banc-evaluation.js (executer, tete, pct, arrondi, MENACES).

// ---------- A. Règle « peel » (Protection nécessaire) ----------
// Copie fidèle de la règle livrée (web/draft-engine.js, RULES.peel), avec le seul bonus rendu paramétrable.
function peelAvecBonus(bonus) {
  return function (p, c, x) {
    if (hasTrait(p, "p")) return x.hasFront ? [0, "Suppose un frontline allié pour le protéger"] : [-2, "Ton équipe n'a pas de frontline pour le protéger"];
    if (!hasTrait(p, "q")) return null;
    if (!x.hasFront && c.mobile.length >= 2) return [-2, "Personne pour te protéger de leur mobilité"];
    if (c.mobile.length >= 3) return [-4 / 3, `${c.mobile.length} champions très mobiles : ${namesOf(c.mobile)}`];
    const v = x.hasFront ? bonus(c, x) : 0;
    return v ? [v, "Peu de plongeurs et de mobilité, et un frontline devant toi"] : null;
  };
}
const calmes = c => c.known.filter(n => !c.divers.includes(n) && !c.mobile.includes(n)).length;
const VARIANTES_PEEL = {
  A0_actuel: { texte: "Règle livrée : +4/3 (soit +2 points) dès 3 adversaires connus, avec au plus 1 plongeur et au plus 1 champion mobile.",
    bonus: c => c.divers.length <= 1 && c.mobile.length <= 1 && c.known.length >= 3 ? 4 / 3 : 0 },
  A1_zero_plongeur: { texte: "Bonus seulement sans aucun plongeur connu (au plus 1 mobile, au moins 3 connus).",
    bonus: c => c.divers.length === 0 && c.mobile.length <= 1 && c.known.length >= 3 ? 4 / 3 : 0 },
  A2_progressif: { texte: "Bonus progressif : 4/3 × min(1, adversaires calmes / 4), divisé par 2 avec 1 plongeur, nul au-delà. Plus de seuil à 3 connus.",
    bonus: c => c.divers.length <= 1 && c.mobile.length <= 1 ? 4 / 3 * Math.min(1, calmes(c) / 4) * (c.divers.length ? .5 : 1) : 0 },
  A3_menace_globale: { texte: "Menace globale seulement : plongeurs et mobiles comptés HORS adversaire direct (le matchup direct le traite déjà). Bonus si 0 plongeur hors lane, au plus 1 mobile hors lane, au moins 3 connus.",
    bonus: (c, x) => { const hors = l => l.filter(n => n !== x.mid); return hors(c.divers).length === 0 && hors(c.mobile).length <= 1 && c.known.length >= 3 ? 4 / 3 : 0; } },
  A4_sans_bonus: { texte: "Plus aucun bonus : la règle ne garde que ses pénalités (extrême prudent).",
    bonus: () => 0 }
};

function avecPeel(nom, fn) {
  const origine = RULES.peel;
  RULES.peel = peelAvecBonus(VARIANTES_PEEL[nom].bonus);
  try { return fn(); } finally { RULES.peel = origine; }
}

function etudePeel(scenarios) {
  const plongeurs = MENACES.plongeur.noms;
  // Contrôle : la copie A0 doit redonner exactement la règle livrée.
  const livre = scenarios.map(s => executer(s));
  const copieA0 = avecPeel("A0_actuel", () => scenarios.map(s => executer(s)));
  const copieFidele = livre.every((r, i) => signature(r) === signature(copieA0[i]));
  const res = { avertissement: "Scénarios SYNTHÉTIQUES (jeu de développement). Variantes calculées en mémoire, formule livrée inchangée.", copieA0IdentiqueALaRegleLivree: copieFidele, variantes: {} };
  Object.keys(VARIANTES_PEEL).forEach(nom => avecPeel(nom, () => {
    const v = { description: VARIANTES_PEEL[nom].texte, bonusDeclenches: 0, scenariosAvecBonus: 0, changementsTeteVsActuel: 0, changementsOrdreVsActuel: 0, parRole: {},
      monotonie: { verifsMidFixe: 0, violationsMidFixe: 0, violationsMidFixePlongeurDevenuDirect: 0, violationsMidFixePlongeurHorsLane: 0, verifsComplet: 0, violationsComplet: 0, violationsCompletPlongeurDevenuDirect: 0, violationsCompletPlongeurHorsLane: 0, haussesCompletDe1PointOuPlus: 0, haussesMidFixeDe1PointOuPlus: 0, scenariosAvecViolationMidFixe: 0, exemples: [] },
      sauts: { ajouts: 0, sautsDe2PointsOuPlus: 0, haussesDe2PointsOuPlus: 0 }, in041: null, exemples: [] };
    const vusMidFixe = new Set();
    scenarios.forEach((s, i) => {
      const r = executer(s);
      const pr = v.parRole[s.joueur.role] = v.parRole[s.joueur.role] || [0, 0]; pr[1]++;
      const nb = r.classement.filter(p => (p.criteres.peel || 0) > 1e-9).length;
      v.bonusDeclenches += nb; if (nb) v.scenariosAvecBonus++;
      if (tete(r) !== tete(livre[i])) { v.changementsTeteVsActuel++; pr[0]++; if (v.exemples.length < 6) v.exemples.push(`${s.id} [${s.joueur.role}] ${s.adversaires.join(", ")} : ${tete(livre[i])} -> ${tete(r)}`); }
      if (r.classement.map(p => p.pick).join() !== livre[i].classement.map(p => p.pick).join()) v.changementsOrdreVsActuel++;
      // Monotonie et sauts : on ajoute chaque plongeur évident (liste de l'annotateur) à une draft incomplète.
      if (s.adversaires.length > 4) return;
      const concernes = s.joueur.pool.filter(p => /[iq]/.test(CHAMPION_TRAITS[p] || ""));
      const avantFixe = executer(s, { midFixe: r.mid });
      plongeurs.filter(n => !s.adversaires.includes(n) && !s.joueur.pool.includes(n)).forEach(nom2 => {
        const adv2 = s.adversaires.concat(nom2);
        const apresFixe = executer(s, { adversaires: adv2, midFixe: r.mid }), complet = executer(s, { adversaires: adv2 });
        r.classement.forEach(p0 => {
          const p1 = complet.classement.find(x => x.pick === p0.pick); if (!p1) return;
          v.sauts.ajouts++;
          const d = p1.score - p0.score;
          if (Math.abs(d) >= 2 - 1e-9) v.sauts.sautsDe2PointsOuPlus++;
          if (d >= 2 - 1e-9) v.sauts.haussesDe2PointsOuPlus++;
        });
        concernes.forEach(p => {
          const a0 = avantFixe.classement.find(x => x.pick === p), a1 = apresFixe.classement.find(x => x.pick === p);
          const c0 = r.classement.find(x => x.pick === p), c1 = complet.classement.find(x => x.pick === p);
          if (!a0 || !a1 || !c0 || !c1) return;
          v.monotonie.verifsMidFixe++;
          if (a1.score > a0.score + 1e-9) {
            v.monotonie.violationsMidFixe++; vusMidFixe.add(s.id);
            if (a1.score - a0.score >= 1 - 1e-9) v.monotonie.haussesMidFixeDe1PointOuPlus++;
            if (complet.mid === nom2) v.monotonie.violationsMidFixePlongeurDevenuDirect++; else v.monotonie.violationsMidFixePlongeurHorsLane++;
            if (v.monotonie.exemples.length < 20) v.monotonie.exemples.push(`${s.id} [${s.joueur.role}] ${s.adversaires.join(", ")} +${nom2} : ${p} ${arrondi(a0.score, 2)} -> ${arrondi(a1.score, 2)} à adversaire direct fixé (${r.mid || "aucun"}) ; ${nom2} ${complet.mid === nom2 ? "devient" : "ne devient pas"} l'adversaire direct`);
          }
          v.monotonie.verifsComplet++;
          if (c1.score > c0.score + 1e-9) {
            v.monotonie.violationsComplet++;
            if (c1.score - c0.score >= 1 - 1e-9) v.monotonie.haussesCompletDe1PointOuPlus++;
            if (complet.mid === nom2) v.monotonie.violationsCompletPlongeurDevenuDirect++; else v.monotonie.violationsCompletPlongeurHorsLane++;
            if (v.monotonie.exemples.length < 20) v.monotonie.exemples.push(`${s.id} [${s.joueur.role}] ${s.adversaires.join(", ")} +${nom2} : ${p} ${arrondi(c0.score, 2)} -> ${arrondi(c1.score, 2)} chemin complet (adversaire direct ${r.mid || "aucun"} -> ${complet.mid || "aucun"}) ; critères qui montent : ${Object.keys(c1.criteres).concat(Object.keys(c0.criteres)).filter((k, j, l) => l.indexOf(k) === j && (c1.criteres[k] || 0) > (c0.criteres[k] || 0) + 1e-9).join(", ")}`);
          }
        });
      });
      // Cas IN-041 du rapport de validation : Xerath, adversaires Yunara et Sejuani, on ajoute Zed.
      if (s.id === "IN-041") {
        const x = (res2, nm) => { const e = res2.classement.find(q => q.pick === nm); return e ? arrondi(e.score, 2) : null; };
        const adv2 = s.adversaires.concat("Zed");
        v.in041 = { adversaires: s.adversaires.join(", ") + " + Zed",
          xerathMidFixe: `${x(avantFixe, "Xerath")} -> ${x(executer(s, { adversaires: adv2, midFixe: r.mid }), "Xerath")}`,
          xerathComplet: `${x(r, "Xerath")} -> ${x(executer(s, { adversaires: adv2 }), "Xerath")}` };
      }
    });
    v.monotonie.scenariosAvecViolationMidFixe = vusMidFixe.size;
    v.tauxChangementTete = pct(v.changementsTeteVsActuel, scenarios.length);
    v.parRole = Object.fromEntries(Object.entries(v.parRole).map(([k, x]) => [k, `${x[0]}/${x[1]}`]));
    res.variantes[nom] = v;
  }));
  return res;
}

// ---------- B. Critère « Pression sur la carte » (map) ----------
function avecPoids(cle, poids, fn) {
  const def = CRITERIA.find(c => c.key === cle), origine = def.weight;
  def.weight = poids;
  try { return fn(); } finally { def.weight = origine; }
}

function etudeMap(scenarios) {
  const def = CRITERIA.find(c => c.key === "map");
  const livre = scenarios.map(s => executer(s));
  const sansMap = avecPoids("map", 0, () => scenarios.map(s => executer(s)));
  const res = {
    avertissement: "Scénarios SYNTHÉTIQUES (jeu de développement). Variantes de poids calculées en mémoire, formule livrée inchangée.",
    poidsActuel: def.weight, source: def.source,
    regle: "g +1, r +0,5, n -0,5 (traits de la fiche du pick, web/draft-champions.js). Ne dépend jamais des adversaires.",
    parRole: {}, variantes: {}, avanceRenversee: { "0 (égalité hors carte)": 0, "0 à 0,25": 0, "0,25 à 0,5": 0, "0,5 et plus": 0 }, exemplesRenversements: [], exemplesExcessifs: []
  };
  const roles = {};
  scenarios.forEach((s, i) => {
    const role = s.joueur.role, r = livre[i], r0 = sansMap[i];
    const st = roles[role] = roles[role] || { scenarios: 0, poolAvecMapActif: 0, ecart1er2eTouche: 0, teteChangeSansMap: 0, excessifs: 0, partMoyenneMap: 0, nPart: 0 };
    st.scenarios++;
    if (r.classement.some(p => p.criteres.map)) st.poolAvecMapActif++;
    if (r.classement.length > 1) {
      const a = r.classement[0], b = r.classement[1];
      if (Math.abs((a.criteres.map || 0) - (b.criteres.map || 0)) > 1e-9) st.ecart1er2eTouche++;
    }
    // Part du critère dans le score absolu du 1er.
    const p1 = r.classement[0];
    if (p1) { const tot = Object.values(p1.criteres).reduce((x, y) => x + Math.abs(y), 0); if (tot > 1e-9) { st.partMoyenneMap += Math.abs(p1.criteres.map || 0) / tot; st.nPart++; } }
    if (tete(r) !== tete(r0)) {
      st.teteChangeSansMap++;
      // Influence jugée excessive : le 1er ne gagne que par la carte, alors que le 1er sans la carte
      // a au moins 0,5 point d'avance sur les critères qui dépendent de la draft.
      const gagnant = r.classement[0].pick, autre = r0.classement[0].pick;
      const horsMap = nom => { const e = r.classement.find(p => p.pick === nom); return e ? e.score - (e.criteres.map || 0) : 0; };
      const avance = horsMap(autre) - horsMap(gagnant);
      const h = res.avanceRenversee;
      if (avance < 1e-6) h["0 (égalité hors carte)"]++; else if (avance < .25 - 1e-9) h["0 à 0,25"]++; else if (avance < .5 - 1e-9) h["0,25 à 0,5"]++; else h["0,5 et plus"]++;
      if (avance >= 1e-6 && res.exemplesRenversements.length < 12) res.exemplesRenversements.push(`${s.id} [${role}] ${s.adversaires.join(", ")} : ${gagnant} (carte ${arrondi(r.classement[0].criteres.map || 0, 2)}) passe devant ${autre} (carte ${arrondi((r.classement.find(p => p.pick === autre) || { criteres: {} }).criteres.map || 0, 2)}), qui avait ${arrondi(avance, 2)} point d'avance hors carte`);
      if (avance >= .5 - 1e-9) {
        st.excessifs++;
        if (res.exemplesExcessifs.length < 12) res.exemplesExcessifs.push(`${s.id} [${role}] ${s.adversaires.join(", ")} : ${gagnant} 1er grâce à la carte (${arrondi(r.classement[0].criteres.map || 0, 2)}), alors que ${autre} a ${arrondi(avance, 2)} point(s) d'avance sur la draft`);
      }
    }
  });
  res.parRole = Object.fromEntries(Object.entries(roles).map(([k, st]) => [k, {
    scenarios: st.scenarios,
    poolAvecMapActif: pct(st.poolAvecMapActif, st.scenarios),
    ecart1er2eTouchePar_map: pct(st.ecart1er2eTouche, st.scenarios),
    teteChangeSansMap: `${st.teteChangeSansMap}/${st.scenarios} (${pct(st.teteChangeSansMap, st.scenarios)} %)`,
    influenceExcessive: `${st.excessifs}/${st.scenarios} (${pct(st.excessifs, st.scenarios)} %)`,
    partMoyenneDuScoreDu1er: pct(st.partMoyenneMap, st.nPart)
  }]));
  const VARIANTES = [["poids 0 (retiré)", 0], ["poids 0,25", .25], ["poids 0,375 (-25 %)", .375], ["poids 0,625 (+25 %)", .625], ["poids 1", 1], ["bris d'égalité seulement (poids 0,001)", .001]];
  VARIANTES.forEach(([nom, w]) => {
    const parRole = {}; let ch = 0, ordre = 0, egalitesAvant = 0, egalitesApres = 0;
    const sorties = avecPoids("map", w, () => scenarios.map(s => executer(s)));
    scenarios.forEach((s, i) => {
      const pr = parRole[s.joueur.role] = parRole[s.joueur.role] || [0, 0]; pr[1]++;
      if (tete(sorties[i]) !== tete(livre[i])) { ch++; pr[0]++; }
      if (sorties[i].classement.map(p => p.pick).join() !== livre[i].classement.map(p => p.pick).join()) ordre++;
      if (livre[i].egalites.some(g => g.includes(livre[i].classement[0].pick))) egalitesAvant++;
      if (sorties[i].classement[0] && sorties[i].egalites.some(g => g.includes(sorties[i].classement[0].pick))) egalitesApres++;
    });
    res.variantes[nom] = { changementsTeteVsActuel: `${ch}/${scenarios.length} (${pct(ch, scenarios.length)} %)`, changementsOrdre: ordre,
      teteEnEgalite: `${egalitesAvant} -> ${egalitesApres}`,
      parRole: Object.fromEntries(Object.entries(parRole).map(([k, v]) => [k, `${v[0]}/${v[1]} (${pct(v[0], v[1])} %)`])) };
  });
  return res;
}

function lancerEtudes(scenarios) {
  return { peel: etudePeel(scenarios), map: etudeMap(scenarios) };
}
