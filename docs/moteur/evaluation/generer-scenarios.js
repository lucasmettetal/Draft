// Générateur de scénarios SYNTHÉTIQUES pour le banc d'évaluation du moteur DRAFT.
// Aucune donnée réelle : les drafts sont inventées, les annotations viennent des tableaux écrits ci-dessous.
// Les annotations ne sont JAMAIS calculées par le moteur. Elles décrivent ce qu'un joueur attend d'une draft évidente.
// Seule dépendance au dépôt : CHAMPION_TRAITS (web/draft-champions.js), pour vérifier que chaque nom existe
// et pour repérer les fiches identiques (propriété structurelle : deux fiches identiques doivent être ex æquo).
// Graine fixe : relancer le générateur redonne exactement les mêmes fichiers.

const GEN_VERSION = "1.0";
const GRAINE = 217;
const PART_EVALUATION = 0.30;
const ROLES = ["top", "jungle", "mid", "adc", "support"];
// Picks couverts par le tableau de matchups écrit à la main : leurs scores dépendent du mid adverse,
// donc deux fiches identiques ne sont pas forcément ex æquo s'ils sont dans le pool.
const PICKS_TABLEAU = ["Twisted Fate", "Fizz", "Kog'Maw", "Xerath"];

// ---------- Annotations écrites à la main par l'auteur du protocole ----------
// Rôle « évident » : champion joué très majoritairement à ce rôle en partie classée.
// Écrit d'après une connaissance générale du jeu, sans consulter draft-role-sheet.js. À faire relire par des joueurs.
// Les groupes servent aussi d'« archétype » pour le test de remplacement par un champion comparable.
const ARCHETYPES = {
  "top-tank": { role:"top", noms:["Ornn", "Sion", "K'Sante", "Dr. Mundo"] },
  "top-combattant": { role:"top", noms:["Darius", "Garen", "Renekton", "Aatrox", "Riven", "Fiora", "Camille", "Illaoi", "Kled", "Mordekaiser"] },
  "jungle-tank": { role:"jungle", noms:["Sejuani", "Zac", "Amumu", "Rammus", "Nunu", "Skarner"] },
  "jungle-assassin": { role:"jungle", noms:["Kha'Zix", "Rengar", "Evelynn", "Shaco", "Kayn"] },
  "jungle-combattant": { role:"jungle", noms:["Vi", "Xin Zhao", "Jarvan IV", "Hecarim", "Warwick", "Viego", "Bel'Veth", "Master Yi", "Lee Sin", "Udyr", "Briar", "Rek'Sai"] },
  "mid-mage": { role:"mid", noms:["Orianna", "Syndra", "Viktor", "Azir", "Anivia", "Lissandra", "Malzahar", "Vex", "Cassiopeia"] },
  "mid-assassin": { role:"mid", noms:["Zed", "Talon", "Katarina", "Fizz", "LeBlanc", "Qiyana", "Naafiri", "Kassadin"] },
  "adc-tireur": { role:"adc", noms:["Jinx", "Caitlyn", "Jhin", "Aphelios", "Kai'Sa", "Ezreal", "Draven", "Xayah", "Zeri", "Miss Fortune", "Sivir", "Varus"] },
  "support-engage": { role:"support", noms:["Leona", "Nautilus", "Alistar", "Rell", "Thresh", "Blitzcrank", "Rakan"] },
  "support-enchanteur": { role:"support", noms:["Janna", "Lulu", "Nami", "Soraka", "Sona", "Milio", "Yuumi", "Renata Glasc"] }
};
const EVIDENTS_HORS_ARCHETYPE = {
  top: ["Gnar", "Nasus", "Teemo", "Tryndamere", "Urgot", "Yorick", "Gangplank", "Singed", "Quinn"],
  jungle: ["Elise", "Graves", "Ivern", "Kindred", "Nidalee", "Shyvana", "Nocturne"],
  mid: ["Ahri", "Aurelion Sol", "Ryze", "Zoe", "Mel", "Twisted Fate", "Veigar"],
  adc: ["Kalista", "Kog'Maw", "Samira", "Tristana", "Twitch", "Vayne", "Smolder", "Nilah", "Yunara"],
  support: ["Bard", "Braum", "Morgana", "Pyke", "Senna", "Taric", "Zilean"]
};
// Champions flex : rôles plausibles, du plus courant au moins courant (jugement de l'annotateur).
const FLEX = {
  "Akali":["mid", "top"], "Akshan":["mid", "top"], "Ambessa":["top", "jungle"], "Aurora":["mid", "top"], "Brand":["support", "mid", "jungle"],
  "Cho'Gath":["top", "mid"], "Corki":["mid", "adc"], "Diana":["jungle", "mid"], "Ekko":["jungle", "mid"], "Galio":["mid", "support"],
  "Gragas":["jungle", "top", "support"], "Gwen":["top", "jungle"], "Irelia":["top", "mid"], "Jax":["top", "jungle"], "Jayce":["top", "mid"],
  "Karma":["support", "mid"], "Karthus":["jungle", "mid"], "Kennen":["top", "mid"], "Lucian":["adc", "mid"], "Lux":["support", "mid"],
  "Malphite":["top", "support"], "Maokai":["support", "jungle"], "Neeko":["mid", "support"], "Olaf":["top", "jungle"], "Pantheon":["support", "mid", "top", "jungle"],
  "Poppy":["support", "jungle", "top"], "Rumble":["top", "mid", "jungle"], "Sett":["top", "support"], "Shen":["top", "support"], "Swain":["support", "mid"],
  "Sylas":["mid", "jungle"], "Taliyah":["jungle", "mid"], "Tahm Kench":["support", "top"], "Trundle":["jungle", "top"], "Vladimir":["mid", "top"],
  "Volibear":["top", "jungle"], "Wukong":["jungle", "top"], "Yasuo":["mid", "top"], "Yone":["mid", "top"], "Zyra":["support", "jungle"],
  "Heimerdinger":["mid", "support"], "Hwei":["mid", "support"], "Seraphine":["support", "adc"], "Ziggs":["mid", "adc"], "Vel'Koz":["support", "mid"],
  "Lillia":["jungle", "top"], "Kayle":["top", "mid"], "Ashe":["adc", "support"], "Fiddlesticks":["jungle", "support"], "Annie":["mid", "support"]
};
// Choix hors rôle connus mais rares : le scénario déclare le rôle réellement joué (vérité par construction du scénario).
const HORS_ROLE = {
  "Vayne":"top", "Teemo":"support", "Kog'Maw":"mid", "Senna":"adc", "Garen":"mid", "Nunu":"mid",
  "Pyke":"mid", "Taric":"jungle", "Shaco":"support", "Morgana":"jungle", "Tristana":"mid", "Zilean":"mid"
};
// Champions trop récents pour un avis : aucune annotation de rôle a priori.
const INCONNUS = ["Locke", "Zaahen"];

// ---------- Outils ----------
function mulberry32(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let x = Math.imul(a ^ a >>> 15, 1 | a); x = x + Math.imul(x ^ x >>> 7, 61 | x) ^ x; return ((x ^ x >>> 14) >>> 0) / 4294967296; }; }
function fnv1a(texte) { let h = 0x811c9dc5; for (let i = 0; i < texte.length; i++) { h ^= texte.charCodeAt(i); h = Math.imul(h, 0x01000193) >>> 0; } return h >>> 0; }

function genererScenarios(traits) {
  const rng = mulberry32(GRAINE);
  const pick = l => l[Math.floor(rng() * l.length)];
  const melange = l => { const c = l.slice(); for (let i = c.length - 1; i > 0; i--) { const k = Math.floor(rng() * (i + 1)); [c[i], c[k]] = [c[k], c[i]]; } return c; };
  const existe = n => Object.prototype.hasOwnProperty.call(traits, n);

  const EVIDENT = {}; ROLES.forEach(r => { EVIDENT[r] = []; });
  Object.values(ARCHETYPES).forEach(g => g.noms.forEach(n => EVIDENT[g.role].push(n)));
  ROLES.forEach(r => EVIDENT[r].push(...EVIDENTS_HORS_ARCHETYPE[r]));
  const roleEvident = {}; ROLES.forEach(r => EVIDENT[r].forEach(n => { roleEvident[n] = r; }));
  const tous = [].concat(...ROLES.map(r => EVIDENT[r]), Object.keys(FLEX), Object.keys(HORS_ROLE), INCONNUS);
  const absents = [...new Set(tous)].filter(n => !existe(n));
  if (absents.length) throw new Error("Noms absents de draft-champions.js : " + absents.join(", "));
  Object.keys(FLEX).forEach(n => { if (roleEvident[n]) throw new Error(n + " est à la fois flex et évident"); });

  // Pool du joueur : champions évidents ou flex de son rôle.
  const candidatsPool = r => EVIDENT[r].concat(Object.keys(FLEX).filter(n => FLEX[n].includes(r)));
  const tirerPool = (role, interdits, taille) => {
    if (role === "mid" && rng() < 0.35) return PICKS_TABLEAU.filter(n => !interdits.includes(n)).slice(0, Math.max(2, taille));
    return melange(candidatsPool(role).filter(n => !interdits.includes(n))).slice(0, taille);
  };
  const tailleSuivante = () => 2 + Math.floor(rng() * 4);

  const scenarios = [];
  const compteur = {};
  const ajouter = (prefixe, categorie, description, role, pool, adversaires, ann) => {
    compteur[prefixe] = (compteur[prefixe] || 0) + 1;
    const id = `${prefixe}-${String(compteur[prefixe]).padStart(3, "0")}`;
    const annotations = Object.assign({
      roles_attendus: {}, roles_possibles: {}, adversaire_direct_attendu: null,
      egalites_attendues: [], absents_attendus: [], confiance_elevee_interdite: false, jugement_expert: null
    }, ann);
    scenarios.push({ id, synthetique: true, categorie, description, joueur: { role, pool }, adversaires, annotations });
  };
  const directDepuis = (role, roles) => {
    const nom = Object.keys(roles).find(n => roles[n].role === role);
    return nom ? { nom, niveau: roles[nom].niveau } : null;
  };
  // Équipe évidente complète, sans les champions interdits.
  const equipeEvidente = interdits => ROLES.map(r => pick(EVIDENT[r].filter(n => !interdits.includes(n))));

  // 1. Drafts complètes, rôles évidents (90).
  for (let i = 0; i < 90; i++) {
    const role = ROLES[i % 5];
    const pool = tirerPool(role, [], tailleSuivante());
    const equipe = equipeEvidente(pool);
    const roles = {}; equipe.forEach((n, k) => { roles[n] = { role: ROLES[k], niveau: "evident" }; });
    ajouter("CE", "complet_evident", "5 adversaires, un champion évident par rôle.", role, pool, melange(equipe),
      { roles_attendus: roles, adversaire_direct_attendu: directDepuis(role, roles) });
  }
  // 2a. Un champion flex, résolu par élimination (40).
  const flexPar = r => Object.keys(FLEX).filter(n => FLEX[n].includes(r));
  for (let i = 0; i < 40; i++) {
    const role = ROLES[i % 5], libre = ROLES[(i * 3 + 1) % 5];
    const flex = pick(flexPar(libre));
    const pool = tirerPool(role, [flex], tailleSuivante());
    const equipe = equipeEvidente(pool.concat(flex));
    const roles = {};
    equipe.forEach((n, k) => { if (ROLES[k] !== libre) roles[n] = { role: ROLES[k], niveau: "evident" }; });
    roles[flex] = { role: libre, niveau: "elimination" };
    const adv = equipe.map((n, k) => ROLES[k] === libre ? flex : n);
    ajouter("CF", "complet_flex", `Flex ${flex} : seul rôle libre ${libre}.`, role, pool, melange(adv),
      { roles_attendus: roles, roles_possibles: { [flex]: FLEX[flex] }, adversaire_direct_attendu: directDepuis(role, roles) });
  }
  // 2b. Deux champions flex : résolus si un seul rôle libre convient à chacun, sinon ambigus (20).
  for (let i = 0; i < 20; i++) {
    const role = ROLES[i % 5];
    const libres = melange(ROLES).slice(0, 2);
    const f1 = pick(flexPar(libres[0])), f2 = pick(flexPar(libres[1]).filter(n => n !== f1));
    const pool = tirerPool(role, [f1, f2], tailleSuivante());
    const equipe = equipeEvidente(pool.concat(f1, f2));
    const roles = {}, possibles = { [f1]: FLEX[f1], [f2]: FLEX[f2] };
    equipe.forEach((n, k) => { if (!libres.includes(ROLES[k])) roles[n] = { role: ROLES[k], niveau: "evident" }; });
    const ok1 = libres.filter(r => FLEX[f1].includes(r)), ok2 = libres.filter(r => FLEX[f2].includes(r));
    const ambigu = ok1.length === 2 && ok2.length === 2;
    if (!ambigu) {
      const r1 = ok1.length === 1 ? ok1[0] : libres.find(r => r !== ok2[0]);
      roles[f1] = { role: r1, niveau: "elimination" };
      roles[f2] = { role: libres.find(r => r !== r1), niveau: "elimination" };
    }
    const adv = equipe.map((n, k) => ROLES[k] === libres[0] ? f1 : ROLES[k] === libres[1] ? f2 : n);
    let direct = directDepuis(role, roles);
    if (!direct && ambigu && libres.includes(role)) direct = null;
    ajouter("CF", "complet_flex", `Deux flex ${f1} et ${f2} sur ${libres.join(" et ")}${ambigu ? ", répartition ambiguë" : ""}.`, role, pool, melange(adv),
      { roles_attendus: roles, roles_possibles: possibles, adversaire_direct_attendu: direct, confiance_elevee_interdite: ambigu && libres.includes(role) });
  }
  // 3a. Choix hors rôle : le rôle habituel du champion est déjà pris par un champion évident (40).
  const horsRole = Object.keys(HORS_ROLE);
  for (let i = 0; i < 40; i++) {
    const role = ROLES[i % 5];
    const o = horsRole[i % horsRole.length], cible = HORS_ROLE[o];
    const pool = tirerPool(role, [o], tailleSuivante());
    const equipe = equipeEvidente(pool.concat(o));
    const roles = {};
    equipe.forEach((n, k) => { if (ROLES[k] !== cible) roles[n] = { role: ROLES[k], niveau: "evident" }; });
    roles[o] = { role: cible, niveau: "hors_role_par_construction" };
    const adv = equipe.map((n, k) => ROLES[k] === cible ? o : n);
    ajouter("HR", "atypique_hors_role", `${o} joué ${cible} ; son rôle habituel (${roleEvident[o]}) est occupé.`, role, pool, melange(adv),
      { roles_attendus: roles, adversaire_direct_attendu: directDepuis(role, roles), confiance_elevee_interdite: true });
  }
  // 3b. Champion sans avis (trop récent), dans le seul rôle libre (10).
  for (let i = 0; i < 10; i++) {
    const role = ROLES[i % 5], libre = ROLES[(i * 2 + 1) % 5], u = INCONNUS[i % 2];
    const pool = tirerPool(role, [u], tailleSuivante());
    const equipe = equipeEvidente(pool);
    const roles = {};
    equipe.forEach((n, k) => { if (ROLES[k] !== libre) roles[n] = { role: ROLES[k], niveau: "evident" }; });
    roles[u] = { role: libre, niveau: "elimination" };
    const adv = equipe.map((n, k) => ROLES[k] === libre ? u : n);
    ajouter("NV", "atypique_inconnu", `${u}, sans avis de rôle, dans le seul rôle libre (${libre}).`, role, pool, melange(adv),
      { roles_attendus: roles, adversaire_direct_attendu: directDepuis(role, roles), confiance_elevee_interdite: libre === role });
  }
  // 4. Drafts incomplètes, de 1 à 4 adversaires (120). Un tiers contient un flex.
  for (let taille = 1; taille <= 4; taille++) {
    for (let i = 0; i < 30; i++) {
      const role = ROLES[(i + taille) % 5];
      const pool = tirerPool(role, [], tailleSuivante());
      const rolesTires = melange(ROLES).slice(0, taille);
      const roles = {}, possibles = {};
      const adv = rolesTires.map((r, k) => {
        if (k === 0 && i % 3 === 2) {
          const f = pick(flexPar(r).filter(n => !pool.includes(n)));
          possibles[f] = FLEX[f];
          return f;
        }
        const n = pick(EVIDENT[r].filter(x => !pool.includes(x)));
        roles[n] = { role: r, niveau: "evident" };
        return n;
      });
      const flexNoms = Object.keys(possibles);
      let direct = directDepuis(role, roles);
      if (!direct) {
        const flexPeutEtre = flexNoms.some(f => FLEX[f].includes(role));
        // Personne de révélé ne joue ce rôle : l'adversaire direct n'est pas encore choisi.
        direct = flexPeutEtre ? null : { nom: null, niveau: "non_revele" };
      }
      ajouter("IN", "incomplet", `${taille} adversaire(s) révélé(s)${flexNoms.length ? `, dont le flex ${flexNoms[0]}` : ""}.`, role, pool, adv,
        { roles_attendus: roles, roles_possibles: possibles, adversaire_direct_attendu: direct });
    }
  }
  // 5. Égalités attendues : deux picks du pool ont exactement la même fiche (30).
  const parFiche = {};
  Object.keys(traits).forEach(n => { if (!PICKS_TABLEAU.includes(n) && !INCONNUS.includes(n)) (parFiche[traits[n]] = parFiche[traits[n]] || []).push(n); });
  const groupes = Object.keys(parFiche).sort().map(k => parFiche[k].sort()).filter(g => g.length >= 2);
  for (let i = 0; i < 30; i++) {
    const g = groupes[i % groupes.length];
    const paire = melange(g).slice(0, 2);
    const role = roleEvident[paire[0]] || (FLEX[paire[0]] && FLEX[paire[0]][0]) || ROLES[i % 5];
    const autres = melange(candidatsPool(role).filter(n => !paire.includes(n) && !PICKS_TABLEAU.includes(n))).slice(0, i % 3);
    const pool = melange(paire.concat(autres));
    const complet = i % 2 === 0;
    const equipe = equipeEvidente(pool);
    const garder = complet ? ROLES : melange(ROLES).slice(0, 1 + (i % 4));
    const roles = {};
    const adv = equipe.filter((n, k) => garder.includes(ROLES[k]));
    equipe.forEach((n, k) => { if (garder.includes(ROLES[k])) roles[n] = { role: ROLES[k], niveau: "evident" }; });
    let direct = directDepuis(role, roles);
    if (!direct) direct = { nom: null, niveau: "non_revele" };
    ajouter("EG", "egalite_attendue", `${paire.join(" et ")} ont la même fiche (« ${traits[paire[0]]} ») : ils doivent être ex æquo.`, role, pool, melange(adv),
      { roles_attendus: roles, adversaire_direct_attendu: direct, egalites_attendues: [paire.slice().sort()] });
  }
  // 6. Pick déjà pris par l'adversaire (10).
  for (let i = 0; i < 10; i++) {
    const role = ROLES[i % 5];
    const pool = tirerPool(role, [], 3 + (i % 3));
    const pris = pool[0];
    const roleDuPris = roleEvident[pris] || (FLEX[pris] ? FLEX[pris][0] : role);
    const equipe = equipeEvidente(pool).map((n, k) => ROLES[k] === roleDuPris ? pris : n);
    const roles = {};
    equipe.forEach((n, k) => { if (n !== pris || roleEvident[pris]) roles[n] = { role: ROLES[k], niveau: roleEvident[n] ? "evident" : "elimination" }; });
    if (!roles[pris]) roles[pris] = { role: roleDuPris, niveau: "elimination" };
    ajouter("PP", "pick_pris", `${pris}, dans le pool, est pris par l'adversaire.`, role, pool, melange(equipe),
      { roles_attendus: roles, adversaire_direct_attendu: directDepuis(role, roles), absents_attendus: [pris] });
  }

  // Séparation reproductible : dans chaque catégorie, les 30 % au plus petit hachage (graine + id) forment le jeu d'évaluation.
  const parCategorie = {};
  scenarios.forEach(s => { (parCategorie[s.categorie] = parCategorie[s.categorie] || []).push(s); });
  const evaluation = new Set();
  Object.keys(parCategorie).sort().forEach(c => {
    const l = parCategorie[c].slice().sort((a, b) => fnv1a(`${GRAINE}:${a.id}`) - fnv1a(`${GRAINE}:${b.id}`) || a.id.localeCompare(b.id));
    l.slice(0, Math.round(l.length * PART_EVALUATION)).forEach(s => evaluation.add(s.id));
  });
  const entete = jeu => ({
    synthetique: true, version_schema: GEN_VERSION, graine: GRAINE, jeu,
    avertissement: "Données SYNTHÉTIQUES inventées pour tester le moteur. Aucune statistique réelle. Annotations écrites à la main, non validées par des experts.",
    source_annotations: "Tableaux ARCHETYPES, EVIDENTS_HORS_ARCHETYPE, FLEX, HORS_ROLE et INCONNUS de generer-scenarios.js. Égalités : fiches identiques dans web/draft-champions.js."
  });
  const dev = scenarios.filter(s => !evaluation.has(s.id));
  const ev = scenarios.filter(s => evaluation.has(s.id));
  return {
    dev: Object.assign(entete("developpement"), { scenarios: dev }),
    evaluation: Object.assign(entete("evaluation_gele"), { scenarios: ev }),
    resume: { total: scenarios.length, developpement: dev.length, evaluation: ev.length, parCategorie: Object.fromEntries(Object.keys(parCategorie).map(c => [c, parCategorie[c].length])) }
  };
}
