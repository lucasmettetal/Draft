# Messages à corriger (V2.17)

Liste exhaustive des corrections issues de [`AUDIT-VOCABULAIRE.md`](AUDIT-VOCABULAIRE.md), état `main` 78519fb.
Catégorie **A** : vocabulaire. Catégorie **B** : l'explication ne correspond pas au calcul.
Priorité **P1** : casse la confiance du joueur. **P2** : faux ou maladroit, mais secondaire. **P3** : rare, peu visible ou mineur.
Les formulations suivent [`../technique/GLOSSAIRE-LOL.md`](../technique/GLOSSAIRE-LOL.md). Les identifiants entre parenthèses (UI-…, EN-…, CO-…) renvoient à l'inventaire.

**Effet sur les scores** : « aucun » veut dire que seuls des textes, ou le choix du texte, changent. Le classement doit rester identique (instantané du banc à 0 différence). « moteur » veut dire que la correction touche un calcul et part en V2.18.

## 1. Tableau des corrections

| # | Emplacement | Texte actuel | Texte proposé | Cat. | Prio | Effet sur les scores | PR cible |
|---|---|---|---|---|---|---|---|
| B-01 | draft-coaching.js:143-164 (draft.close, draft.conflict) ; index.html:1438, 1445 (CO-013, CO-014, CO-015, CO-017, CO-018) | Les deux peuvent convenir. {affiché} passe devant : {raison}. Prends celui avec lequel tu es le plus à l'aise. / Écart de -1,0 point : sous le seuil de 2… | Si le nom affiché est 1er : « Les deux se valent presque. {1er} passe devant : {raison}. Prends celui que tu maîtrises le mieux. » S'il ne l'est pas : titre « {1er} passe devant de peu » ; texte « {1er} mène de {écart} point : {raison}. {affiché} reste affiché tant que l'écart reste sous 2 points, pour que le conseil ne change pas à chaque pick. » draft.conflict : « reste devant » seulement si le pick est 1er. | B | P1 | aucun | PR D |
| B-02 | draft-mastery.js:286-308, 316-325 ; index.html:1440, 1503 (MA-002, MA-010, MA-011, MA-012) | Meilleur choix pour la draft · La draft penche légèrement vers {affiché}. | « Conseil affiché » ; si le nom affiché n'est pas 1er : « Conseil affiché ({1er} mène de peu) ». Compromis : « La draft penche légèrement vers {1er}. » | B | P1 | aucun | PR D |
| B-03 | index.html:2047 (puce du panneau), 1583 et 860 (V.order met le nom affiché en tête) (UI-205, UI-092) | Recommandé | « Recommandé » seulement si le nom affiché est 1er ; sinon « Presque à égalité » (comme la carte du pool, qui dit déjà « Très proche »). | B | P1 | aucun | PR D |
| B-04 | index.html:1406, 1417, 1691 ; draft-coaching.js:129 (UI-056, UI-121, CO-006, UI-054) | L'ordre d'affichage n'a pas de sens. / Le nom affiché en premier ne vient que de l'ordre alphabétique, ou de ton favori. / {N} choix équivalents | Selon la vraie cause : « Même score. {X} passe en premier : son plus gros point faible pèse moins ({critère}). » / « Même score : {X} passe en premier car c'est ton favori. » / « Même score et même risque : l'ordre est alphabétique, il ne veut rien dire. » Titre : « {N} picks à égalité de score ». | B | P1 | aucun | PR D |
| B-05 | index.html:1266, 1308 ; draft-data.js:299-314 ; draft-engine.js:344, 354-360 (UI-026, UI-027) | Mid probable | Avec moins de 3 champions révélés : « Mid ? » et, sous le nom, « hypothèse ({n} champion vu) ». Le seuil pour nommer l'adversaire relève du moteur (V2.18, PR 18-1). | B | P1 | aucun pour le texte ; moteur pour le seuil | PR E (texte) puis V2.18 |
| B-06 | index.html:790-811 (roleHints, deriveSlots), 1288-1317 (RoleRow) (UI-028, UI-033, rôles des lignes adverses) | (ligne Jungle) Yasuo · Mid non identifié + champion | Placer chaque champion selon la répartition la plus probable de roleBelief ; laisser la ligne du joueur vide (« Mid d'en face : pas identifié ») si personne n'est identifié ; marquer « ? » un rôle incertain. | B | P1 | aucun (les rôles affichés n'entrent pas dans le score, index.html:797) | PR E |
| B-07 | index.html:1695 (UI-125) | Frontline allié supposé. | Option cochée : « Frontline alliée supposée (réglage). » Option décochée : « Pas de frontline alliée supposée (réglage). » | B | P2 | aucun | PR D |
| B-08 | draft-engine.js:296-300 (evaluateScenarios, champ text), 428-430 (positiveReasons) (EN-045, EN-012 (dans les raisons positives)) | 1er choix contre Yasuo · À éviter contre Yone | « Si c'est Yasuo : 1er choix. Si c'est Yone : à éviter. » Dans les raisons positives, ne garder que les hypothèses positives (et l'inverse pour le danger). | B | P2 | aucun (texte seul) | PR D |
| B-09 | index.html:1689 (UI-119) | Mid inconnu : le matchup direct n'est pas évalué. | Avec hypothèses : « Mid pas identifié : le matchup est une moyenne des hypothèses. » Sans hypothèse utile : « Mid pas identifié : le matchup ne compte pas encore. » | B | P2 | aucun | PR E |
| B-10 | index.html:859, 866, 869, 1759, 1777 ; draft-engine.js:475-496 (UI-088, UI-144, UI-145, EN-051, EN-052, EN-053, CO-128, CO-129) | si ton équipe n'a pas de frontline · Sans frontline : {C} | Tout de suite (texte) : ne pas proposer « si ton équipe n'a pas de frontline » quand l'option est décochée. Ensuite : passer les hypothèses et le réglage à whenBetter, midSwapEffect et au calcul « sans frontline ». | B | P2 | aucun pour le texte ; calcul annexe (classement inchangé) pour la suite | PR D (texte) ; calcul annexe en PR D si l'instantané reste à 0 différence, sinon V2.18 |
| A-01 | draft-engine.js:122-130 (EN-001) | Matchup direct · Sécurité sans adversaire connu · Cibles exploitables · Tanks et bruisers · Survie aux plongeurs · Engages et contrôles · Poke et longue portée · Protection nécessaire · Pression sur la carte | Matchup direct · Sûr en blind · Carrys à punir · Frontline adverse · Résistance au dive · Engage et CC adverses · Poke adverse · Besoin de peel · Pression sur la map | A | P2 | aucun | PR C |
| A-02 | index.html:1361-1365 (UI-040, UI-041) | Frontline · Carrys fragiles · Plongeurs · Contrôles ciblés · Engage | Frontline · Carrys fragiles · Dive · CC ciblés · Engage | A | P2 | aucun | PR C |
| A-03 | index.html:1194-1205 (UI-039) | Carry · Plongeur · Contrôle · Soutien (champion sans trait, souvent un enchanteur) | Carry fragile · Dive · CC · Utilitaire | A | P2 | aucun | PR C |
| A-04 | draft-engine.js:519-527 (EN-059, UI-043, UI-044) | Leurs forces : frontline, contrôles ciblés, plongeurs · Leurs failles : aucun contrôle ciblé | Leurs forces : frontline, CC ciblés, dive · Leurs failles : aucun CC ciblé | A | P2 | aucun | PR C |
| A-05 | draft-engine.js:245-251 (EN-028, EN-029, EN-030, EN-032) | 2 plongeurs contre un champion immobile : Zed, Kha'Zix · Encaisse leurs 2 plongeurs · Peut protéger ses alliés de leurs 2 plongeurs · 2 contrôles ciblés punissent ses plongeons : … | 2 champions de dive contre un champion peu mobile : Zed, Kha'Zix · Tient bien le dive de Zed et Kha'Zix · Bon peel contre leurs 2 champions de dive · 2 CC ciblés punissent ses all-in : … | A | P2 | aucun | PR C |
| A-06 | draft-engine.js:221-226, 258-259 (EN-018, EN-021, EN-022, EN-035, EN-036) | 2 cibles fragiles : … · 2 cibles à abîmer de loin : … · Peut rendre les coups à distance : … · Se fait toucher avant de pouvoir frapper : … | 2 carrys fragiles à punir : … · 2 carrys à poke : … · Peut répondre à leur poke : … · Subit leur poke avant de pouvoir agir : … | A | P2 | aucun | PR C |
| A-07 | draft-engine.js:263-267 (EN-038, EN-039, EN-041) | Ton équipe n'a pas de frontline pour le protéger · Personne pour te protéger de leur mobilité · Peu de plongeurs et de mobilité, et un frontline devant toi | Pas de frontline alliée pour le protéger · Pas de peel contre leur mobilité · Peu de dive en face, et une frontline devant toi (une seule personne grammaticale par phrase) | A | P2 | aucun | PR C |
| A-08 | draft-engine.js:252, 271-273, 566, 602 (EN-033, EN-042, EN-043, EN-044, EN-074, EN-091) | …revenir avec son ultime · Son ultime couvre toute la carte · Peut peser sur toute la carte · Peu d'impact hors des combats groupés | …revenir avec son ult · Son ult couvre toute la map · Grosse pression sur la map · Peu d'impact hors teamfight | A | P2 | aucun | PR C |
| A-09 | draft-coaching.js:66-73, 139-152 (CO-013 (et toute raison insérée par coachReason)) | Twisted Fate passe devant : mid inconnu : peu de mauvais matchups (1 sur 41 dans le tableau). | Twisted Fate passe devant. Raison : sûr en blind (mauvais dans 1 matchup sur 41 du tableau). Une raison du moteur ne se coupe pas en minuscule au milieu d'une phrase à deux-points. | A | P2 | aucun | PR C |
| A-10 | index.html:1266-1308, 1687 ; draft-coaching.js:234, 373 ; draft-mastery.js:270-271 (UI-026 à UI-031, UI-117, CO-130, MA-016, MA-019, EN-087, CO-059) | Mid probable · Mid très probable · Mid non identifié · Mid supposé : X · Mid pas encore visible · Mid supposé, à vérifier · Adversaire supposé : X · Adversaire direct inconnu · Confiance moyenne | Une seule échelle : « Mid d'en face : Ahri (probable) » · « (presque sûr) » · « Mid d'en face : pas identifié (Ahri ou Zed) » · « Mid d'en face : pas encore choisi ». « Ton vis-à-vis » dans les phrases. | A | P2 | aucun | PR E |
| A-11 | draft-coaching.js:348-349 (CO-118) | Classes Riot : Fighter, Tank | Classes Riot : Combattant, Tank (Fighter → Combattant, Marksman → Tireur, Mage, Assassin, Tank, Support) | A | P2 | aucun | PR C |
| A-12 | index.html:1910-1917, 1935, 2287 ; draft-engine.js:129, 263, 267, 567, 604, 613 (UI-257, UI-170, UI-172, EN-009, EN-037, EN-041, EN-075, EN-093, EN-100, UI-125, UI-186) | un frontline · ton frontline · Frontline allié supposé · pas le frontline (et ailleurs « une frontline alliée ») | Féminin partout : une frontline, ta frontline, frontline alliée, pas la frontline | A | P2 | aucun | PR C |
| A-13 | draft-coaching.js:264, 277, 284, 297, 304, 319 (CO-078, CO-088, CO-092, CO-102, CO-106, CO-111) | Des joueurs solides en face : … · Personne pour encaisser en face · … peuvent t'immobiliser · … peuvent lancer le combat · … touchent de très loin · Dégâts surtout magiques en face | Grosse frontline en face : … · Pas de frontline en face · Beaucoup de CC ciblés : … · Beaucoup d'engage en face : … · Beaucoup de poke en face : … · Beaucoup d'AP en face (ou d'AD) | A | P2 | aucun | PR C |
| B-11 | index.html:1676 (UI-115) | Avis général basé sur les 5 champions adverses, sans cooldowns ni builds vérifiés. | Avis général basé sur les {n} champions adverses visibles, sans tenir compte des cooldowns ni des builds. | B | P3 | aucun | PR D |
| B-12 | draft-coaching.js:241-242 (CO-064, CO-065) | …seul le critère « sans adversaire connu » intervient pour ce rôle. | Hors mid ou hors tableau : « Le matchup ne compte pas encore : le conseil repose sur la comp adverse. » | B | P3 | aucun | PR E |
| B-13 | draft-coaching.js:253 (CO-071) | Il baisse avec une frontline alliée qui protège : à vérifier selon les picks de ton équipe. | En jeu, une frontline alliée qui peel réduit ce risque. Le calcul n'en tient pas compte. | B | P3 | aucun | PR D |
| B-14 | draft-coaching.js:228-238 (CO-058, CO-060, CO-061) | Mid pas encore identifié : prépare les deux cas | Titre selon le nombre : « Mid pas identifié : Ahri ou Zed » / « Mid pas identifié : peut-être Ahri ». Texte : « Le conseil tient compte de ces hypothèses. » | B | P3 | aucun | PR E |
| B-15 | draft-coaching.js:282-294 (CO-092, CO-093, CO-094, CO-095) | Si tu fonces et qu'ils t'immobilisent, tu ne peux plus rien faire. | Texte selon la branche : « 3 engages contre un champion peu mobile : place-toi derrière ta frontline. » | B | P3 | aucun | PR D |
| B-16 | index.html:813-825 (UI-011) | {action} : {C} passe devant {C} — {raison} | Raison = texte du critère décisif (leadCriterion(nouveau, ancien)). | B | P3 | aucun | PR D |
| B-17 | index.html:1585 (UI-095) | Peu adapté | « Moins adapté » (selon le rang), sans seuil sur le score. | B | P3 | aucun | PR D |
| B-18 | index.html:1935 (UI-186, UI-187) | L'outil suppose que ton équipe a un frontline et le signale si ça changerait le verdict. … Le ban conseillé vient des tableaux de cet outil. … patch 16.21. | Texte selon le réglage et la source réelle du ban ; patch lu dans les données. | B | P3 | aucun | PR D |
| B-19 | index.html:1908-1921 (UI-170 à UI-175) | Selon la composition adverse (tableau Situation / Pick) | Titre « Repères pour ton pool (résumé, pas le calcul exact) ». | B | P3 | aucun | PR D |
| B-20 | index.html:1807 (UI-154) | Pas dans le tableau. Par défaut, prends Twisted Fate. | Pas dans le tableau. Ajoute ce champion à ta draft pour voir le classement. | B | P3 | aucun | PR D |
| B-21 | index.html:2291 (UI-258) | Ils servent à deviner le mid adverse. | Ils servent à deviner ton vis-à-vis. | B | P3 | aucun | PR E |
| B-22 | draft-coaching.js:192 (CO-032) | Ce critère est le plus lourd du classement. | C'est le critère au plus gros poids du calcul. | B | P3 | aucun | PR D |
| B-23 | draft-coaching.js:358-369, 419-422 (CO-124, CO-126, CO-127, CO-129) | Aucun changement simple de la draft ne le fait passer devant. | Si X est déjà 1er : « {X} est déjà devant de peu. » | B | P3 | aucun | PR D |
| B-24 | index.html:855-856 (UI-081) | Choix net | Mesurer l'écart contre le 2e du classement. | B | P3 | aucun (choix du rival affiché, pas du score) | PR D |
| A-14 | draft-coaching.js:217-219, 431-437 (CO-050 à CO-052, CO-159, CO-160, CO-164, CO-165) | Tuer un tank en voie… · l'objectif de voie · Avant de quitter ta voie · Tu n'as pas de voie · Ta voie est courte | lane partout : « Tuer un tank en lane… », « Avant de quitter ta lane… » | A | P3 | aucun | PR C |
| A-15 | draft-coaching.js:219, 431-439 ; index.html:1920 (CO-052, CO-159, CO-161, CO-162, CO-163, CO-167, UI-175) | le meurtre · une mise à mort · Une balise · Une téléportation · tes mates · tes ultimes | le kill · un kill · Une ward · Un TP · tes alliés · tes ults | A | P3 | aucun | PR C |
| A-16 | draft-coaching.js:306 (CO-108) | Équipe de poke : si vous arrivez affaiblis, le combat démarre mal. | Comp poke : si ton équipe arrive affaiblie, le teamfight démarre mal. Engage vite, ou reste hors de portée. | A | P3 | aucun | PR C |
| A-17 | draft-engine.js:124-129 ; index.html:1931 (EN-004 à EN-009, UI-184) | Règle heuristique : … · Heuristique (écrit à la main) : | Règle écrite à la main : … · Écrit à la main : | A | P3 | aucun | PR C |
| A-18 | index.html:1693, 2203, 2226 (UI-123, UI-238, UI-242) | 2 adversaire(s) sur 5 · 4 absente(s) · 1 partie(s) ajoutée(s) | Accord selon le nombre : « 2 adversaires sur 5 », « 4 absentes », « 1 partie ajoutée » | A | P3 | aucun | PR C |
| A-19 | draft-profiles.js:131-158 (PR-002, PR-003) | Champion inconnu dans top : … · Favori ignoré pour top · laissé vide : support | Noms affichés des rôles : « dans Top », « pour Top », « laissé vide : Support » | A | P3 | aucun | PR C |
| A-20 | draft-data.js:74, 143 (DA-014) | Silver à Émeraude, classé solo/duo | Silver à Emerald, classé solo/duo (une seule langue pour les rangs) | A | P3 | aucun | PR C |
| A-21 | index.html:1562, 2064 (UI-084, UI-212) | Danger {pire critère} (fenêtre) · Danger : {champion} (panneau, autre calcul) | « Risque : {pire critère} » dans la fenêtre, « Menace : {champion} » dans le panneau | A | P3 | aucun | PR C |
| A-22 | index.html:1977 (UI-192) | Je prends | Je joue (comme le bouton principal) | A | P3 | aucun | PR C |
| A-23 | draft-engine.js:439-454 (EN-049, EN-050) | Mage de carte : pousse, contrôle une cible… · Tueur de tanks : fait fondre la première ligne. · Tank : encaisse en première ligne. | Mage de map : push, stun une cible… · Anti-tank : fait fondre la frontline. · Tank : encaisse en frontline. | A | P3 | aucun | PR C |
| A-24 | draft-engine.js:553-554, 606-621 (EN-067, EN-068, EN-095, EN-096, EN-108) | Lance le combat sur X… · Repère X sur la carte… sans visuel, recule · Pose une ward de contrôle… · invisibles sur la carte | Engage sur X… · Repère X sur la map… sans vision, recule · Pose une control ward… · absents de la map | A | P3 | aucun | PR C |
| A-25 | draft-coaching.js:59, 347 (CO-067, CO-117) | corps à corps | mêlée | A | P3 | aucun | PR C |
| A-26 | index.html:2281, 2287 (UI-256, UI-257) | À égalité exacte, préférer · Supposer un frontline dans mon équipe | En cas d'égalité, mettre en premier · Compter sur une frontline dans mon équipe | A | P3 | aucun | PR C |
| A-27 | index.html:1359, 1694 ; draft-mastery.js:307 (UI-049, UI-124, MA-012) | Profil de l'équipe · Profil provisoire : … · Écart moteur de 1,20 points | Profil de leur comp · Fiche provisoire : … · Écart de score de 1,20 point | A | P3 | aucun | PR C (MA-012 dans PR D, même fonction) |
| A-28 | draft-coaching.js:425-426 (CO-154) | Dégâts physiques et magiques | Dégâts AD et AP (« L'armure réduit les dégâts physiques (AD)… ») | A | P3 | aucun | PR C |
| A-29 | index.html:823 (UI-010) | Il menait déjà de peu : le nom attendait la fin de la draft pour changer. | Il menait déjà de peu : le conseil change maintenant que la draft est complète. | A | P3 | aucun | PR D |
| A-30 | src/DraftMid.cs:406-430 (Menu natif (src)) | Activation automatique pendant la draft · Positionnement automatique · Importer mes statistiques... | Mêmes mots que la page : « Afficher les panneaux pendant la draft », « Coller les panneaux au client LoL », « Importer mes statistiques (ancien format)... » | A | P3 | aucun | PR C (natif, à part) |

Total : 24 entrées B (6 P1, 4 P2, 14 P3) et 30 entrées A (13 P2, 17 P3). Seules B-05 (seuil) et, si besoin, la deuxième partie de B-10 touchent un calcul.

## 2. Tests existants qui vérifient ces textes

À mettre à jour dans la même PR que le texte. Recherche faite dans `web/*-tests.html`, `tools/*.ps1` et les tests intégrés de `src/DraftMid.cs`.

| Test | Ligne | Ce qui est vérifié | Touché par |
|---|---|---|---|
| web/draft-v215-tests.html | 23 | `/ordre d'affichage/` dans `.tie-group` | PR D (B-04) |
| web/draft-v215-tests.html | 24 | `/ne les départage pas/` dans la ligne de conseil (draft.tie, débutant) | PR D (B-04 : garder la phrase débutant ou adapter) |
| web/draft-v215-tests.html | 53 | `/Meilleur choix pour la draft/` et `/Le plus joué dans ce rôle/` dans l'onglet Conseils | PR D (B-02) |
| web/draft-v215-tests.html | 51 | `/Le plus joué/` dans la ligne d'expérience | — (inchangé) |
| web/draft-v215-tests.html | tableau des onglets | `Plan,Analyse,Conseils,Duel,Parties` | — (« Duel » est gardé) |
| web/draft-coaching-tests.html | 41 | `/serré/` dans le titre de draft.close | PR D (B-01 : le titre « {1er} passe devant de peu » ne contient plus « serré » quand le nom est maintenu ; garder « serré » dans le cas normal) |
| web/draft-coaching-tests.html | 42 | `/alphab/` et `/identiques/` dans draft.tie avancé | PR D (B-04 : reste vrai seulement si la cause est l'alphabet ; prévoir un cas « risque » et un cas « favori ») |
| web/draft-coaching-tests.html | 75 | `/malgré/` dans le titre de draft.conflict | PR D (B-01) |
| web/draft-coaching-tests.html | 79 | `/difficile/` dans le titre de matchup.written, `/écrit à la main/` dans sa limite | PR C (si le titre change) |
| web/draft-coaching-tests.html | 37 | `/D[ée]duction/` dans la limite de matchup.unknown | PR E (garder « Déduction ») |
| web/draft-coaching-tests.html | 55, 63 | `/supposé/` dans le titre de matchup.written quand la confiance n'est pas élevée ; « adversaire présenté comme supposé » | PR E (A-10 : le mot « supposé » peut devenir « probable » : adapter l'expression) |
| web/draft-coaching-tests.html | 52 | `/corps à corps/` absent du profil de Jayce | PR C (A-25 : remplacer par `/mêlée/`) |
| web/draft-coaching-tests.html | 53 | `/4 cibles fragiles : [^.]*Syndra\./` | PR C (A-06 : nouveau libellé « carrys fragiles à punir ») |
| web/draft-coaching-tests.html | 71 | `/te touche de plus loin/` dans le titre de matchup.range | PR C (si le titre change) |
| web/draft-coaching-tests.html | 77 | `/solides.*Yasuo/` absent du titre de comp.frontline | PR C (A-13 : adapter à « Grosse frontline en face ») |
| web/draft-coaching-tests.html | 50 | `/vague/` absent pour un support | PR C (vérifier après A-14) |
| web/draft-tests.html | 485 | aucune raison positive ne commence par `À éviter`, `Aucune cible`, `Ton équipe n'a pas` | PR D (B-08 : ajouter le format « Si c'est X : à éviter ») et PR C (A-07 : « Pas de frontline alliée… ») |
| web/draft-mastery-tests.html | 61 | `familiar.label === "Le plus joué"` | — |
| web/draft-mastery-tests.html | 76 | `/proche/` dans le compromis | PR D (B-02, à garder) |
| web/draft-mastery-tests.html | 78 | aucune incertitude ne dit `/est supposé/` | PR E (A-10) |
| web/draft-profiles-tests.html | 48 | `/absent/` et `/Favori ignoré/` dans les avertissements d'import | PR C (A-19 : garder ces mots) |
| src/DraftMid.cs | 2000 | `hypothesisText().includes('non identifié')` (avec Meraki seulement) | PR E (A-10) |
| src/DraftMid.cs | 1586 | le texte d'adversaire d'un support contient « Support » et pas « Mid » | PR E |
| src/DraftMid.cs | 1582 | le bouton principal contient « Je joue » | — (gardé) |
| src/DraftMid.cs | 1479 | un bouton « Rechercher une mise à jour » | — |
| tools/run-coaching-review.ps1 | 30 | libellé « non identifié » dans le rapport de relecture | PR E (alignement, pas une assertion) |
| tools/run-mastery-compare.ps1 | 17 | libellé « Le plus joué » dans le rapport | — |
| web/draft-layout-tests.html | — | débordements et textes coupés | PR C, D, E : relancer `tools/run-layout-tests.ps1` (longueur des libellés) |
| web/draft-messages-tests.html | catalogue | gabarits de toutes les entrées | PR C, D, E : mettre à jour les lignes du catalogue touchées ; les constats corrigés doivent tomber à 0 |

## 3. Découpage proposé

Ordre recommandé : **PR D, puis PR E, puis PR C**. D et E corrigent les problèmes P1 de confiance. C est un balayage de textes : il se rebase facilement en dernier, et D et E écrivent déjà leurs nouveaux textes avec le vocabulaire du glossaire. Une seule PR à la fois sur `web/index.html` (règle de la synthèse).

Chaque PR : toutes les suites existantes, `tools/extract-ui-messages.ps1` (constats visés à 0, aucun texte hors inventaire), `tools/run-layout-tests.ps1`, et le banc d'évaluation des autres agents avec **0 différence de classement**.

### PR D — explications inexactes (17-4)

Entrées : B-01, B-02, B-03, B-04, B-07, B-08, B-10 (partie texte), B-11, B-13, B-15, B-16, B-17, B-18, B-19, B-20, B-22, B-23, B-24, A-29, et MA-012 (A-27).

| Fichier | Lignes | Changement |
|---|---|---|
| web/draft-coaching.js | 118-130 | draft.tie : dire la vraie cause de l'ordre (risque, favori ou alphabet) |
| web/draft-coaching.js | 143-164 | draft.close et draft.conflict : texte selon le rang réel du nom affiché |
| web/draft-coaching.js | 192, 253, 282-294 | matchup.written (poids), comp.assassins (frontline alliée), comp.control (branche engage) |
| web/draft-coaching.js | 358-369, 419-422 | champ.alternative quand le rival est déjà devant |
| web/draft-engine.js | 296-300 | texte des hypothèses : « Si c'est X : … » (champ `text` seulement, notes inchangées) |
| web/draft-engine.js | 326-335 (à côté) | nouvelle fonction pure `tieCause` (lit le tri, ne change rien) |
| web/draft-engine.js | 428-434 | positiveReasons et mainRisk : pas de texte d'hypothèse de signe contraire |
| web/draft-mastery.js | 286-308, 316-325 | « Conseil affiché » ; compromis sur le vrai 1er |
| web/index.html | 813-825 | raison du changement = critère décisif ; texte A-29 |
| web/index.html | 855-856 | rival de « Choix net » = 2e du classement (B-24) |
| web/index.html | 1406, 1417 | égalité : titre et cause réelle |
| web/index.html | 1580-1586 | « Moins adapté » au lieu de « Peu adapté » |
| web/index.html | 1676 | nombre réel de champions visibles |
| web/index.html | 1691-1695 | égalité dans l'analyse ; frontline selon le réglage |
| web/index.html | 1759, 1777 | « passerait devant » : pas de condition déjà vraie (texte) |
| web/index.html | 1807, 1907-1921, 1935 | Duel, Règles (repères, fiabilité) |
| web/index.html | 2047 | puce « Recommandé » seulement pour le 1er |
| Calcul annexe (facultatif) | draft-engine.js:475-496, index.html:859, 866, 869 | passer hypothèses et réglage frontline à whenBetter, midSwapEffect, « sans frontline » et « provisoire » ; à garder dans D seulement si le banc reste à 0 différence, sinon V2.18 |

### PR E — rôles affichés et adversaire direct (17-5)

Entrées : B-05 (texte), B-06, B-09, B-12, B-14, B-21, A-10.

| Fichier | Lignes | Changement |
|---|---|---|
| web/draft-data.js | 258-286 (à côté) | nouvelle fonction pure `likelyAssignment(team, fixed)` : la répartition la plus probable, tirée de `roleBelief` |
| web/draft-data.js | 327-332 | beliefHypothesis : nombre de champions vus, pour le texte |
| web/index.html | 790-811 | deriveSlots à partir de `likelyAssignment` ; ligne du joueur vide si personne n'est identifié ; « ? » si incertain |
| web/index.html | 1264-1286 | aide, échelle de confiance, hypothesisText, ligne adversaire (« Mid d'en face : … (probable) », « hypothèse » sous 3 champions) |
| web/index.html | 1288-1317, 1353 | étiquette de la ligne du joueur ; indice de la colonne gauche |
| web/index.html | 1687-1689 | incertitudes de l'analyse : confiance, matchup moyen des hypothèses |
| web/index.html | 2291 | « ton vis-à-vis » |
| web/draft-coaching.js | 228-244, 371-376 | matchup.unknown (titre selon le nombre de noms, critère blind hors mid), limits.opponent-assumed |
| web/draft-mastery.js | 270-271 | mêmes mots pour l'adversaire supposé ou inconnu |

Le seuil pour nommer l'adversaire (B-05) ne change pas ici : il part en V2.18 (PR 18-1), mesuré avec le banc.

### PR C — vocabulaire (textes seulement)

Entrées : A-01 à A-09, A-11 à A-28 (sauf MA-012), A-30.

| Fichier | Lignes | Changement |
|---|---|---|
| web/draft-engine.js | 122-130 | libellés et sources des critères |
| web/draft-engine.js | 201-273 | textes des raisons (chaînes seulement, notes inchangées) |
| web/draft-engine.js | 439-454, 519-527, 542-568, 587-621 | styles, lecture de comp, menaces, conditions de victoire, plan de jeu |
| web/draft-coaching.js | 59, 66-73 | portées (« mêlée ») ; coachReason sans coupure en minuscule |
| web/draft-coaching.js | 197-226, 246-325 | matchup.range, tank, assassin ; titres et textes de composition (après D) |
| web/draft-coaching.js | 326-349, 425-439 | classes Riot traduites ; principes (lane, kill, ward, TP, AD/AP) |
| web/index.html | 1194-1205, 1359-1365 | profils des adversaires, jauges |
| web/index.html | 1562, 2064 | « Risque » et « Menace » |
| web/index.html | 1693-1694, 2203, 2226 | pluriels, « Fiche provisoire » |
| web/index.html | 1910-1932, 1977, 2281, 2287 | Règles (frontline, alliés, écrit à la main), « Je joue », réglages |
| web/draft-data.js | 74, 143 | rangs dans une seule langue |
| web/draft-profiles.js | 131-158 | noms de rôles affichés dans les messages d'import |
| src/DraftMid.cs | 406-423 | menu de l'icône aligné sur la page (peut être une petite PR à part) |

### Conflits attendus

| Fichier | Où | Entre | Comment l'éviter |
|---|---|---|---|
| web/index.html | 1687-1695 (incertitudes de l'analyse) | D (1691-1695) et E (1687-1689) | lignes voisines : fusionner D avant d'ouvrir E |
| web/index.html | 1353-1365 | E (indice) et C (jauges) | hunks voisins : C après E |
| web/index.html | 1907-1935 (Règles) | D (repères, fiabilité) et C (frontline, alliés) | C après D, en reprenant les textes de D |
| web/draft-coaching.js | 246-294 (comp.assassins, comp.control) | D (textes niveau 3 et branche engage) et C (titres) | C après D |
| web/draft-coaching.js | 228-244 | E seulement (matchup.unknown confié à E en entier) | — |
| web/draft-mastery.js | 286-325 | D (compromis, libellé) ; MA-012 traité dans D | C ne touche pas ce bloc |
| web/draft-engine.js | 296-300, 428-434 (D) ; 122-130, 201-273, 439-621 (C) | D et C | hunks séparés, sans conflit attendu |
| web/draft-messages-tests.html | bloc `catalog` | C, D, E | chaque PR ne modifie que les lignes de ses entrées |

Les autres agents travaillent sur `tests/moteur/`, `tools/run-engine-eval.ps1` et `tools/run-accuracy-tests.ps1` : aucune de ces PR n'y touche. Si l'instantané du banc compare aussi les textes des raisons, la PR C les changera volontairement : le dire dans la PR.

## 4. Ce qui relève du moteur (V2.18)

| Sujet | Entrée | Pourquoi ce n'est pas du texte |
|---|---|---|
| Seuil pour nommer l'adversaire direct selon le nombre de champions révélés | B-05 | `currentMid` sert au classement quand une seule hypothèse reste, et au plan de jeu. PR 18-1 de la synthèse. |
| Hypothèses dans les calculs annexes (whenBetter, midSwapEffect, « sans frontline », « provisoire ») | B-10 (suite) | Classement principal inchangé, mais les résultats de ces calculs changent. À garder en V2.17 seulement si le banc reste à 0 différence. |
| Même seuil de 2 points pour « choix net » et pour garder le nom affiché | B-01, B-03 | Les textes deviennent justes en V2.17 ; séparer les deux seuils est un choix de calcul. |
| Critère « Pression sur la map » souvent décisif alors qu'il ne dépend pas de la draft | (étude, E7) | L'explication est fidèle ; c'est le poids (0,5) qui est en cause. |
| Raisons d'hypothèses avec leur poids | B-08 | Le texte « Si c'est X » suffit en V2.17 ; afficher un poids demanderait de valider la qualité des probabilités. |
