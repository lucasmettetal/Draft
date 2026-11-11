# Audit des messages : vocabulaire LoL et fidélité au calcul

État audité : `main` 78519fb (DRAFT 2.16.1). Aucun fichier de production n'a été modifié.
Ce document fait l'inventaire des textes vus par le joueur, les relève dans la vraie page, et classe les problèmes.
Les corrections sont listées dans [`MESSAGES-A-CORRIGER.md`](MESSAGES-A-CORRIGER.md). Le guide de style est [`../technique/GLOSSAIRE-LOL.md`](../technique/GLOSSAIRE-LOL.md).

Études reprises sans les refaire : `RAPPORT-RECOMMANDATION.md` (§ 2.4, 2.5), `RAPPORT-ROLES.md` (§ 1.5, 1.7, 4.2), `SYNTHESE-FEUILLE-DE-ROUTE.md` (PR 17-4, 17-5).

## 1. Résumé chiffré

| Mesure | Valeur |
|---|---|
| Entrées d'inventaire (page web) | **591** (dont 5 jamais affichées par l'interface) |
| Entrées visibles vues réellement à l'écran | **568 sur 586** |
| Entrées visibles non déclenchées | **18** (liste et raison au § 7) |
| Textes distincts relevés dans le DOM | 13442 (avec noms de champions), environ 1 960 une fois noms et nombres remplacés |
| Textes relevés hors inventaire | 0 |
| Drafts jouées dans la page | 692 passages, 3048 étapes (5 rôles, 4 tailles de fenêtre) |
| Étapes vérifiées en fenêtre complète | 1593, dont 1326 avec une draft incomplète et 590 égalités affichées |
| Textes natifs (src/*.cs), classés à part | 24 groupes (§ 8) |
| Problèmes de fidélité (B) | **24** : 6 P1, 4 P2, 14 P3 |
| Problèmes de vocabulaire (A) | **30** : 0 P1, 13 P2, 17 P3 |

Une entrée regroupe parfois plusieurs libellés courts (par exemple les 9 noms de critères, EN-001). Les gabarits ont leurs parties variables entre accolades.

Par fichier :

| Fichier | Entrées | Visibles | Vues à l'écran |
|---|---|---|---|
| web/index.html | 269 | 269 | 261 |
| web/draft-engine.js | 109 | 109 | 108 |
| web/draft-coaching.js | 168 | 164 | 159 |
| web/draft-mastery.js | 25 | 24 | 21 |
| web/draft-data.js | 15 | 15 | 15 |
| web/draft-profiles.js | 5 | 5 | 4 |
| web/draft-providers.js | 0 | 0 | — (seul texte : une erreur interne jamais affichée, `RiotDraftProvider est désactivé…`) |
| web/draft-champions.js | 0 | 0 | — (fiches en lettres, aucun texte affiché) |

## 2. Méthode

1. **Inventaire par lecture du code.** Chaque texte visible a un identifiant (UI, EN, CO, MA, DA, PR), son emplacement `fichier:ligne`, le calcul qui le produit et son gabarit. L'inventaire vit dans `web/draft-messages-tests.html` (bloc `catalog`), pour que l'outil et ce document lisent la même liste.
2. **Relevé dans la vraie page.** `tools/extract-ui-messages.ps1` copie `web/` dans `%TEMP%\draft-v217-vocab`, ajoute `'unsafe-eval'` à la CSP **dans cette copie seulement** (comme `run-v215-tests.ps1`), puis ouvre `web/draft-messages-tests.html` dans Chrome sans écran et hors ligne. La page de test pilote `index.html` dans des cadres :
   - les 252 drafts de `scenarios-dev.json` (5 rôles, drafts complètes, incomplètes, flex, hors rôle, égalités) et 21 drafts écrites pour couvrir des cas précis ;
   - chaque draft est saisie champion par champion ; après chaque ajout, tout le texte du DOM est relevé (texte, `title`, `aria-label`, `placeholder`, replis ouverts) ;
   - fenêtre complète (onglets Plan, Analyse : Critères, Comparer, Règles ; Conseils aux 3 niveaux ; Duel ; Parties), panneau droit, panneau compact, panneau gauche ;
   - option « frontline » décochée sur 81 drafts ;
   - situations particulières : saisie et recherche, correction des rôles, lecture du client simulée, journal, favori, exclusions, sans profil, réglages avec les sections de l'application, imports de profils et de journal (fichiers fictifs), **données calculées SYNTHÉTIQUES** (bilans, rôles observés), historique fictif, statistiques importées fictives.
3. **Rattachement.** Chaque texte relevé est comparé aux gabarits. Aucun texte relevé n'est resté hors inventaire.
4. **Constats.** À chaque étape, la page refait le calcul et vérifie si l'explication affichée le contredit (nom affiché qui n'est pas 1er, égalité départagée par le risque, réglage frontline, rôles affichés contre `roleBelief`, etc.). Les numéros renvoient aux entrées B du § 4.

Reproduire : `tools\extract-ui-messages.ps1 -Scenarios <chemin>\scenarios-dev.json` (environ 2 minutes). Sans `-Scenarios`, seules les 21 drafts intégrées sont jouées. `-All` ajoute tous les textes relevés au fichier de résultat.

## 3. Ce que montre le relevé

Les comptes portent sur des **étapes** (un état de draft après un ajout). Ils servent à repérer et à mesurer l'ampleur, pas à estimer une fréquence en vraie partie : les drafts de test sont synthétiques.

| Constat | Étapes | Exemple relevé |
|---|---|---|
| B-01 « choix serré » : le nom affiché « passe devant » ou écart négatif | 90 | fenêtre MID-dive [2/5, mid] Les deux peuvent convenir. Fizz passe devant : son E lui laisse une sortie contre les plongeurs. Prends celui avec lequel tu es le plus à l'aise. |
| B-02 « Meilleur choix pour la draft » sur un pick qui n'est pas 1er | 128 | fenêtre MID-dive [2/5, mid] Fizz |
| B-03 étiquette « Recommandé » sur un nom qui n'est pas 1er | 118 | right MID-dive Fizz / Kog'Maw |
| B-04 égalité départagée par le risque, texte « alphabet / ordre sans sens » | 6 | fenêtre CE-012 [3/5, jungle] Warwick = Graves (risques 0.00 / -3.00) |
| B-05 adversaire direct étiqueté « probable » ou « très probable » avec 1 ou 2 champions révélés | 220 | « ADC probable » avec 1 champion(s) révélé(s) : 7<br>« ADC probable » avec 2 champion(s) révélé(s) : 32<br>« Jungle probable » avec 1 champion(s) révélé(s) : 8<br>« Jungle probable » avec 2 champion(s) révélé(s) : 27<br>« Mid probable » avec 1 champion(s) révélé(s) : 25<br>« Mid probable » avec 2 champion(s) révélé(s) : 32<br>« Mid très probable » avec 1 champion(s) révélé(s) : 2<br>« Mid très probable » avec 2 champion(s) révélé(s) : 1 |
| B-06 champion placé dans le rôle du joueur sous « non identifié » | 12 | fenêtre MID-dive [5/5, mid] Yasuo, Yone, Lee Sin, Jinx, Thresh |
| B-06 rôle affiché différent du rôle le plus probable du calcul | 162 | fenêtre MID-dive [2/5, mid] Yasuo affiché jungle, calcul mid (42 % contre 8 %) |
| B-07 « Frontline allié supposé. » affiché, option décochée | 405 | sans frontline MID-ref [1/5] |
| B-08 raison mélangeant deux hypothèses sans les nommer | 22 | fenêtre MID-dive [2/5, mid] 1er choix contre Yasuo · À éviter contre Yone |
| B-09 « inconnu : le matchup direct n'est pas évalué » alors que les hypothèses le comptent | 28 | fenêtre MID-dive [2/5, mid] 2e choix contre Yasuo · 1er choix contre Yone |
| B-10 classement sans hypothèses (Comparer, « passerait devant ») différent du classement affiché | 15 | fenêtre MID-dive [2/5, mid] affiché Kog'Maw, recalcul Twisted Fate |
| B-10 « Sans frontline : X » calculé sans les hypothèses, autre résultat avec | 11 | fenêtre MID-dive [2/5, mid] affiché Twisted Fate, avec hypothèses Fizz |
| B-10 « passerait devant si ton équipe n'a pas de frontline » alors que c'est déjà le réglage | 45 | sans frontline MID-dive [2/5, mid] Twisted Fate |
| B-11 « basé sur les 5 champions adverses » avec une draft incomplète | 1326 | fenêtre MID-ref [1/5, mid] |
| B-16 raison du changement de conseil différente du critère décisif | 5 | fenêtre CE-019 [4/5, adc] Caitlyn → Tristana : « 3 engages contre un champion lent : Zac, Nilah, Nautilus », décisif « Tanks et bruisers » |
| Contexte : nom affiché classé derrière le 1er | 128 | fenêtre MID-dive [2/5, mid] Fizz affiché, Kog'Maw 1er (écart 1.89) |

Repères pour lire ces chiffres :
- 1 326 étapes de draft incomplète ont été vérifiées ; le nom affiché n'était pas le 1er dans 128 d'entre elles (9,7 %).
- L'adversaire direct a été étiqueté 936 fois ; 220 fois avec 1 ou 2 champions révélés (23 %).
- 590 égalités affichées ; 6 départagées en réalité par le risque. Ce taux est faible ici car les pools de test ont souvent des fiches identiques ; l'étude mesure 89,5 % sur le pool mid d'origine.
- L'option frontline décochée : le texte faux s'affiche à chaque fois (405 sur 405).

Vérifié sans problème trouvé :
- **Bonus de composition présenté comme un matchup direct** : aucun cas. Les raisons de composition ne disent jamais « bon matchup ». Les conseils `matchup.tank` et `matchup.assassin` sont rangés dans la famille « matchup » mais disent eux-mêmes « Avis général, pas un bilan de ce matchup ». Le guide interdit désormais la confusion (règle R4).
- **« À éviter » parmi les raisons positives** : 0 cas dans nos drafts (l'étude en mesure 1,5 % sur le pool mid d'origine). Le défaut existe dans le code (B-08).
- **« reste devant » sur un pick à égalité**, **titre du matchup écrit contraire au tableau** : 0 cas.
- Une raison affichée correspond toujours à un critère réellement compté (comme l'étude l'avait vu).

## 4. Fidélité au calcul (catégorie B)

Règle suivie : aucune justification n'est inventée pour couvrir un écart. Quand le calcul ne permet pas de dire quelque chose, la correction propose de le dire simplement.

Priorités : **P1** casse la confiance du joueur sur l'écran principal ; **P2** faux mais secondaire ou dans l'analyse ; **P3** imprécis, rare ou peu visible.


#### B-01 (P1) — CO-013, CO-014, CO-015, CO-017, CO-018

- **Source** : `draft-coaching.js:143-164 (draft.close, draft.conflict) ; index.html:1438, 1445`
- **Calcul réel** : Le conseil reçoit le nom affiché (pick = V.shown, index.html:1438). Ce nom est maintenu par holdPick (draft-engine.js:363-368, index.html:844) tant que le 1er ne mène pas de 2 points. Le « rival » devient alors le vrai 1er (draft-coaching.js:99-100).
- **En quoi c'est faux** : Le texte dit « X passe devant », « X mène surtout sur… », « Écart de -1,0 point », ou « reste devant / le score reste le meilleur », alors que X est 2e. Vu 90 fois à l'écran (128 étapes où le nom affiché n'est pas 1er, sur 1 326 étapes de draft incomplète).
- **Texte actuel** : « Les deux peuvent convenir. {affiché} passe devant : {raison}. Prends celui avec lequel tu es le plus à l'aise. / Écart de -1,0 point : sous le seuil de 2… »
- **Correction proposée** : Si le nom affiché est 1er : « Les deux se valent presque. {1er} passe devant : {raison}. Prends celui que tu maîtrises le mieux. » S'il ne l'est pas : titre « {1er} passe devant de peu » ; texte « {1er} mène de {écart} point : {raison}. {affiché} reste affiché tant que l'écart reste sous 2 points, pour que le conseil ne change pas à chaque pick. » draft.conflict : « reste devant » seulement si le pick est 1er.
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-02 (P1) — MA-002, MA-010, MA-011, MA-012

- **Source** : `draft-mastery.js:286-308, 316-325 ; index.html:1440, 1503`
- **Calcul réel** : playerChoices prend best = le nom affiché (displayed: V.shown), pas rows[0].
- **En quoi c'est faux** : L'onglet Conseils écrit « Meilleur choix pour la draft » et « La draft penche vers {affiché} » pour un pick classé 2e. Vu 128 fois.
- **Texte actuel** : « Meilleur choix pour la draft · La draft penche légèrement vers {affiché}. »
- **Correction proposée** : « Conseil affiché » ; si le nom affiché n'est pas 1er : « Conseil affiché ({1er} mène de peu) ». Compromis : « La draft penche légèrement vers {1er}. »
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-03 (P1) — UI-205, UI-092

- **Source** : `index.html:2047 (puce du panneau), 1583 et 860 (V.order met le nom affiché en tête)`
- **Calcul réel** : La puce « Recommandé » est affichée sans condition sur V.shown.
- **En quoi c'est faux** : « Recommandé » sur un nom qui n'est pas 1er : 118 étapes dans le panneau droit.
- **Texte actuel** : « Recommandé »
- **Correction proposée** : « Recommandé » seulement si le nom affiché est 1er ; sinon « Presque à égalité » (comme la carte du pool, qui dit déjà « Très proche »).
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-04 (P1) — UI-056, UI-121, CO-006, UI-054

- **Source** : `index.html:1406, 1417, 1691 ; draft-coaching.js:129`
- **Calcul réel** : Tri à score égal : 1. le pire critère (risque) le moins lourd, 2. le favori, 3. l'alphabet (draft-engine.js:316-318). markTies groupe par score seul (:326-335).
- **En quoi c'est faux** : Les textes disent « l'ordre d'affichage n'a pas de sens » et « ne vient que de l'ordre alphabétique, ou de ton favori ». Faux quand le risque départage : 6 des 590 égalités affichées dans nos drafts, et 89,5 % des égalités en tête du pool mid d'origine selon l'étude. « Choix équivalents » est aussi trop fort : seul le score est égal.
- **Texte actuel** : « L'ordre d'affichage n'a pas de sens. / Le nom affiché en premier ne vient que de l'ordre alphabétique, ou de ton favori. / {N} choix équivalents »
- **Correction proposée** : Selon la vraie cause : « Même score. {X} passe en premier : son plus gros point faible pèse moins ({critère}). » / « Même score : {X} passe en premier car c'est ton favori. » / « Même score et même risque : l'ordre est alphabétique, il ne veut rien dire. » Titre : « {N} picks à égalité de score ».
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-05 (P1) — UI-026, UI-027

- **Source** : `index.html:1266, 1308 ; draft-data.js:299-314 ; draft-engine.js:344, 354-360`
- **Calcul réel** : L'étiquette vient de beliefConfidence (p ≥ 0,5 et écart ≥ 0,15 pour « probable »). guessMid accepte un champion dès que sa part propre atteint 0,2, même avec 1 seul champion révélé.
- **En quoi c'est faux** : « Mid probable » (ou Top, Jungle, ADC, Support) avec 1 ou 2 champions révélés : 220 des 936 étapes où l'étiquette apparaît (23 %). Exemple : Sylas seul → « Mid probable ». Le calcul pèse pourtant aussi « pas encore choisi ».
- **Texte actuel** : « Mid probable »
- **Correction proposée** : Avec moins de 3 champions révélés : « Mid ? » et, sous le nom, « hypothèse ({n} champion vu) ». Le seuil pour nommer l'adversaire relève du moteur (V2.18, PR 18-1).
- **Effet sur les scores** : aucun pour le texte ; moteur pour le seuil. **PR** : PR E (texte) puis V2.18.

#### B-06 (P1) — UI-028, UI-033, rôles des lignes adverses

- **Source** : `index.html:790-811 (roleHints, deriveSlots), 1288-1317 (RoleRow)`
- **Calcul réel** : deriveSlots place les champions un par un d'après leurs parts propres, sans le rôle du joueur, puis complète au hasard des rôles libres. roleBelief (draft-data.js:258-286) calcule la répartition jointe, mais n'est pas utilisé pour l'affichage.
- **En quoi c'est faux** : Un champion est affiché dans un rôle alors que le calcul en préfère nettement un autre : 162 étapes (exemple : Yasuo affiché Jungle, calcul mid à 42 % contre 8 %). 12 étapes avec un champion placé dans la ligne du joueur sous « Mid non identifié ».
- **Texte actuel** : « (ligne Jungle) Yasuo · Mid non identifié + champion »
- **Correction proposée** : Placer chaque champion selon la répartition la plus probable de roleBelief ; laisser la ligne du joueur vide (« Mid d'en face : pas identifié ») si personne n'est identifié ; marquer « ? » un rôle incertain.
- **Effet sur les scores** : aucun (les rôles affichés n'entrent pas dans le score, index.html:797). **PR** : PR E.

#### B-07 (P2) — UI-125

- **Source** : `index.html:1695`
- **Calcul réel** : Phrase ajoutée sans condition à la liste des incertitudes.
- **En quoi c'est faux** : « Frontline allié supposé. » s'affiche aussi quand l'option est décochée : 405 étapes sur 405.
- **Texte actuel** : « Frontline allié supposé. »
- **Correction proposée** : Option cochée : « Frontline alliée supposée (réglage). » Option décochée : « Pas de frontline alliée supposée (réglage). »
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-08 (P2) — EN-045, EN-012 (dans les raisons positives)

- **Source** : `draft-engine.js:296-300 (evaluateScenarios, champ text), 428-430 (positiveReasons)`
- **Calcul réel** : Avec plusieurs hypothèses, le texte du critère colle deux textes avec « · », sans dire de quelle hypothèse il s'agit ni son poids. Le signe vient de la moyenne.
- **En quoi c'est faux** : « 1er choix contre Yasuo · À éviter contre Yone » affiché comme raison ou comme danger : 22 étapes. L'étude mesure « À éviter » parmi les raisons positives dans 1,5 % des picks en tête (0 dans nos drafts).
- **Texte actuel** : « 1er choix contre Yasuo · À éviter contre Yone »
- **Correction proposée** : « Si c'est Yasuo : 1er choix. Si c'est Yone : à éviter. » Dans les raisons positives, ne garder que les hypothèses positives (et l'inverse pour le danger).
- **Effet sur les scores** : aucun (texte seul). **PR** : PR D.

#### B-09 (P2) — UI-119

- **Source** : `index.html:1689`
- **Calcul réel** : Avec plusieurs hypothèses, le critère Matchup direct est compté en moyenne pondérée (draft-engine.js:293-308).
- **En quoi c'est faux** : « Mid inconnu : le matchup direct n'est pas évalué. » alors que le matchup compte : 28 étapes.
- **Texte actuel** : « Mid inconnu : le matchup direct n'est pas évalué. »
- **Correction proposée** : Avec hypothèses : « Mid pas identifié : le matchup est une moyenne des hypothèses. » Sans hypothèse utile : « Mid pas identifié : le matchup ne compte pas encore. »
- **Effet sur les scores** : aucun. **PR** : PR E.

#### B-10 (P2) — UI-088, UI-144, UI-145, EN-051, EN-052, EN-053, CO-128, CO-129

- **Source** : `index.html:859, 866, 869, 1759, 1777 ; draft-engine.js:475-496`
- **Calcul réel** : « Sans frontline », « passerait devant », « Si c'est X » et « provisoire » sont recalculés sans les hypothèses. whenBetter force en plus la frontline alliée (beats(…, true)).
- **En quoi c'est faux** : « Sans frontline : X » différent du calcul avec hypothèses (11 étapes) ; base de la vue Comparer différente du classement affiché (15) ; « passerait devant si ton équipe n'a pas de frontline » alors que l'option est déjà décochée (45).
- **Texte actuel** : « si ton équipe n'a pas de frontline · Sans frontline : {C} »
- **Correction proposée** : Tout de suite (texte) : ne pas proposer « si ton équipe n'a pas de frontline » quand l'option est décochée. Ensuite : passer les hypothèses et le réglage à whenBetter, midSwapEffect et au calcul « sans frontline ».
- **Effet sur les scores** : aucun pour le texte ; calcul annexe (classement inchangé) pour la suite. **PR** : PR D (texte) ; calcul annexe en PR D si l'instantané reste à 0 différence, sinon V2.18.

#### B-11 (P3) — UI-115

- **Source** : `index.html:1676`
- **Calcul réel** : Texte fixe.
- **En quoi c'est faux** : « basé sur les 5 champions adverses » avec 1 à 4 adversaires : 1 326 étapes.
- **Texte actuel** : « Avis général basé sur les 5 champions adverses, sans cooldowns ni builds vérifiés. »
- **Correction proposée** : Avis général basé sur les {n} champions adverses visibles, sans tenir compte des cooldowns ni des builds.
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-12 (P3) — CO-064, CO-065

- **Source** : `draft-coaching.js:241-242`
- **Calcul réel** : Le critère « sans adversaire connu » n'existe que pour le mid avec un pick du tableau (draft-engine.js:211).
- **En quoi c'est faux** : Annonce la « sécurité générale du pick » et ce critère pour tous les rôles.
- **Texte actuel** : « …seul le critère « sans adversaire connu » intervient pour ce rôle. »
- **Correction proposée** : Hors mid ou hors tableau : « Le matchup ne compte pas encore : le conseil repose sur la comp adverse. »
- **Effet sur les scores** : aucun. **PR** : PR E.

#### B-13 (P3) — CO-071

- **Source** : `draft-coaching.js:253`
- **Calcul réel** : Le critère dive ne lit pas la frontline alliée (draft-engine.js:242-249).
- **En quoi c'est faux** : « Risque compté par le moteur… Il baisse avec une frontline alliée » laisse croire que le calcul en tient compte.
- **Texte actuel** : « Il baisse avec une frontline alliée qui protège : à vérifier selon les picks de ton équipe. »
- **Correction proposée** : En jeu, une frontline alliée qui peel réduit ce risque. Le calcul n'en tient pas compte.
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-14 (P3) — CO-058, CO-060, CO-061

- **Source** : `draft-coaching.js:228-238`
- **Calcul réel** : Jusqu'à 2 noms (p ≥ 0,2), parfois 1 seul ; le classement pèse jusqu'à 4 hypothèses et « pas encore choisi ».
- **En quoi c'est faux** : « prépare les deux cas » ou « Le conseil tient compte des deux » avec un seul nom ou plus de deux hypothèses.
- **Texte actuel** : « Mid pas encore identifié : prépare les deux cas »
- **Correction proposée** : Titre selon le nombre : « Mid pas identifié : Ahri ou Zed » / « Mid pas identifié : peut-être Ahri ». Texte : « Le conseil tient compte de ces hypothèses. »
- **Effet sur les scores** : aucun. **PR** : PR E.

#### B-15 (P3) — CO-092, CO-093, CO-094, CO-095

- **Source** : `draft-coaching.js:282-294`
- **Calcul réel** : Le malus du critère contrôle peut venir de 3 engages contre un champion peu mobile (draft-engine.js:253).
- **En quoi c'est faux** : Dans ce cas, le texte parle de CC ciblés et de « Si tu fonces ». Non vu dans nos drafts, possible par le code.
- **Texte actuel** : « Si tu fonces et qu'ils t'immobilisent, tu ne peux plus rien faire. »
- **Correction proposée** : Texte selon la branche : « 3 engages contre un champion peu mobile : place-toi derrière ta frontline. »
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-16 (P3) — UI-011

- **Source** : `index.html:813-825`
- **Calcul réel** : La raison est la première raison nouvelle (perte de l'ancien pick ou gain du nouveau), pas le critère qui fait la différence.
- **En quoi c'est faux** : Raison différente du critère décisif : 5 étapes (ex. « 3 engages contre un champion lent » alors que l'écart vient de « Tanks et bruisers »).
- **Texte actuel** : « {action} : {C} passe devant {C} — {raison} »
- **Correction proposée** : Raison = texte du critère décisif (leadCriterion(nouveau, ancien)).
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-17 (P3) — UI-095

- **Source** : `index.html:1585`
- **Calcul réel** : Score < 0. Le score n'a pas de sens absolu (somme sans normalisation, voir UI-177).
- **En quoi c'est faux** : « Peu adapté » juge un pick sur un seuil que l'outil dit lui-même sans valeur.
- **Texte actuel** : « Peu adapté »
- **Correction proposée** : « Moins adapté » (selon le rang), sans seuil sur le score.
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-18 (P3) — UI-186, UI-187

- **Source** : `index.html:1935`
- **Calcul réel** : Bans : bilans calculés, sinon tableau, sinon fiches (draft-engine.js:370-415). Signal « sans frontline » calculé sans hypothèses.
- **En quoi c'est faux** : « suppose que ton équipe a un frontline et le signale » (faux si décoché), « Le ban conseillé vient des tableaux », « patch 16.21 » écrit en dur.
- **Texte actuel** : « L'outil suppose que ton équipe a un frontline et le signale si ça changerait le verdict. … Le ban conseillé vient des tableaux de cet outil. … patch 16.21. »
- **Correction proposée** : Texte selon le réglage et la source réelle du ban ; patch lu dans les données.
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-19 (P3) — UI-170 à UI-175

- **Source** : `index.html:1908-1921`
- **Calcul réel** : Le classement est une somme pondérée de critères (CRITERIA), pas ce tableau de décision.
- **En quoi c'est faux** : Le tableau « Situation → Pick » est présenté comme la règle.
- **Texte actuel** : « Selon la composition adverse (tableau Situation / Pick) »
- **Correction proposée** : Titre « Repères pour ton pool (résumé, pas le calcul exact) ».
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-20 (P3) — UI-154

- **Source** : `index.html:1807`
- **Calcul réel** : Texte fixe, sans calcul.
- **En quoi c'est faux** : « Par défaut, prends Twisted Fate. » n'est pas un résultat du moteur.
- **Texte actuel** : « Pas dans le tableau. Par défaut, prends Twisted Fate. »
- **Correction proposée** : Pas dans le tableau. Ajoute ce champion à ta draft pour voir le classement.
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-21 (P3) — UI-258

- **Source** : `index.html:2291`
- **Calcul réel** : Les rôles servent à deviner l'adversaire direct, quel que soit le rôle du joueur.
- **En quoi c'est faux** : « Ils servent à deviner le mid adverse. » pour un joueur top, jungle, ADC ou support.
- **Texte actuel** : « Ils servent à deviner le mid adverse. »
- **Correction proposée** : Ils servent à deviner ton vis-à-vis.
- **Effet sur les scores** : aucun. **PR** : PR E.

#### B-22 (P3) — CO-032

- **Source** : `draft-coaching.js:192`
- **Calcul réel** : Le matchup a le plus gros poids (3), mais son impact n'est pas forcément le plus grand dans cette draft.
- **En quoi c'est faux** : « Ce critère est le plus lourd du classement. »
- **Texte actuel** : « Ce critère est le plus lourd du classement. »
- **Correction proposée** : C'est le critère au plus gros poids du calcul.
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-23 (P3) — CO-124, CO-126, CO-127, CO-129

- **Source** : `draft-coaching.js:358-369, 419-422`
- **Calcul réel** : Le rival est le vrai 1er quand le nom affiché est maintenu (même cause que B-01).
- **En quoi c'est faux** : « Quand préférer X ? » et « Aucun changement simple ne le fait passer devant » alors que X est déjà devant. Non vu dans nos drafts.
- **Texte actuel** : « Aucun changement simple de la draft ne le fait passer devant. »
- **Correction proposée** : Si X est déjà 1er : « {X} est déjà devant de peu. »
- **Effet sur les scores** : aucun. **PR** : PR D.

#### B-24 (P3) — UI-081

- **Source** : `index.html:855-856`
- **Calcul réel** : « Choix net » se mesure contre le premier rival qui n'est pas « à éviter » (étude, E8).
- **En quoi c'est faux** : Peut cacher un 2e pick à moins de 2 points. 0 cas dans nos drafts.
- **Texte actuel** : « Choix net »
- **Correction proposée** : Mesurer l'écart contre le 2e du classement.
- **Effet sur les scores** : aucun (choix du rival affiché, pas du score). **PR** : PR D.

## 5. Vocabulaire (catégorie A)

Textes justes mais peu naturels pour un joueur : traduction littérale, jargon d'outil, mélange de genres, phrases à rallonge. Les formulations suivent [`GLOSSAIRE-LOL.md`](../technique/GLOSSAIRE-LOL.md). Aucune entrée A n'est P1 : le sens reste compréhensible.

| ID | Prio | Messages | Emplacement | Texte actuel | Formulation proposée | PR |
|---|---|---|---|---|---|---|
| A-01 | P2 | EN-001 | draft-engine.js:122-130 | Matchup direct · Sécurité sans adversaire connu · Cibles exploitables · Tanks et bruisers · Survie aux plongeurs · Engages et contrôles · Poke et longue portée · Protection nécessaire · Pression sur la carte | Matchup direct · Sûr en blind · Carrys à punir · Frontline adverse · Résistance au dive · Engage et CC adverses · Poke adverse · Besoin de peel · Pression sur la map | PR C |
| A-02 | P2 | UI-040, UI-041 | index.html:1361-1365 | Frontline · Carrys fragiles · Plongeurs · Contrôles ciblés · Engage | Frontline · Carrys fragiles · Dive · CC ciblés · Engage | PR C |
| A-03 | P2 | UI-039 | index.html:1194-1205 | Carry · Plongeur · Contrôle · Soutien (champion sans trait, souvent un enchanteur) | Carry fragile · Dive · CC · Utilitaire | PR C |
| A-04 | P2 | EN-059, UI-043, UI-044 | draft-engine.js:519-527 | Leurs forces : frontline, contrôles ciblés, plongeurs · Leurs failles : aucun contrôle ciblé | Leurs forces : frontline, CC ciblés, dive · Leurs failles : aucun CC ciblé | PR C |
| A-05 | P2 | EN-028, EN-029, EN-030, EN-032 | draft-engine.js:245-251 | 2 plongeurs contre un champion immobile : Zed, Kha'Zix · Encaisse leurs 2 plongeurs · Peut protéger ses alliés de leurs 2 plongeurs · 2 contrôles ciblés punissent ses plongeons : … | 2 champions de dive contre un champion peu mobile : Zed, Kha'Zix · Tient bien le dive de Zed et Kha'Zix · Bon peel contre leurs 2 champions de dive · 2 CC ciblés punissent ses all-in : … | PR C |
| A-06 | P2 | EN-018, EN-021, EN-022, EN-035, EN-036 | draft-engine.js:221-226, 258-259 | 2 cibles fragiles : … · 2 cibles à abîmer de loin : … · Peut rendre les coups à distance : … · Se fait toucher avant de pouvoir frapper : … | 2 carrys fragiles à punir : … · 2 carrys à poke : … · Peut répondre à leur poke : … · Subit leur poke avant de pouvoir agir : … | PR C |
| A-07 | P2 | EN-038, EN-039, EN-041 | draft-engine.js:263-267 | Ton équipe n'a pas de frontline pour le protéger · Personne pour te protéger de leur mobilité · Peu de plongeurs et de mobilité, et un frontline devant toi | Pas de frontline alliée pour le protéger · Pas de peel contre leur mobilité · Peu de dive en face, et une frontline devant toi (une seule personne grammaticale par phrase) | PR C |
| A-08 | P2 | EN-033, EN-042, EN-043, EN-044, EN-074, EN-091 | draft-engine.js:252, 271-273, 566, 602 | …revenir avec son ultime · Son ultime couvre toute la carte · Peut peser sur toute la carte · Peu d'impact hors des combats groupés | …revenir avec son ult · Son ult couvre toute la map · Grosse pression sur la map · Peu d'impact hors teamfight | PR C |
| A-09 | P2 | CO-013 (et toute raison insérée par coachReason) | draft-coaching.js:66-73, 139-152 | Twisted Fate passe devant : mid inconnu : peu de mauvais matchups (1 sur 41 dans le tableau). | Twisted Fate passe devant. Raison : sûr en blind (mauvais dans 1 matchup sur 41 du tableau). Une raison du moteur ne se coupe pas en minuscule au milieu d'une phrase à deux-points. | PR C |
| A-10 | P2 | UI-026 à UI-031, UI-117, CO-130, MA-016, MA-019, EN-087, CO-059 | index.html:1266-1308, 1687 ; draft-coaching.js:234, 373 ; draft-mastery.js:270-271 | Mid probable · Mid très probable · Mid non identifié · Mid supposé : X · Mid pas encore visible · Mid supposé, à vérifier · Adversaire supposé : X · Adversaire direct inconnu · Confiance moyenne | Une seule échelle : « Mid d'en face : Ahri (probable) » · « (presque sûr) » · « Mid d'en face : pas identifié (Ahri ou Zed) » · « Mid d'en face : pas encore choisi ». « Ton vis-à-vis » dans les phrases. | PR E |
| A-11 | P2 | CO-118 | draft-coaching.js:348-349 | Classes Riot : Fighter, Tank | Classes Riot : Combattant, Tank (Fighter → Combattant, Marksman → Tireur, Mage, Assassin, Tank, Support) | PR C |
| A-12 | P2 | UI-257, UI-170, UI-172, EN-009, EN-037, EN-041, EN-075, EN-093, EN-100, UI-125, UI-186 | index.html:1910-1917, 1935, 2287 ; draft-engine.js:129, 263, 267, 567, 604, 613 | un frontline · ton frontline · Frontline allié supposé · pas le frontline (et ailleurs « une frontline alliée ») | Féminin partout : une frontline, ta frontline, frontline alliée, pas la frontline | PR C |
| A-13 | P2 | CO-078, CO-088, CO-092, CO-102, CO-106, CO-111 | draft-coaching.js:264, 277, 284, 297, 304, 319 | Des joueurs solides en face : … · Personne pour encaisser en face · … peuvent t'immobiliser · … peuvent lancer le combat · … touchent de très loin · Dégâts surtout magiques en face | Grosse frontline en face : … · Pas de frontline en face · Beaucoup de CC ciblés : … · Beaucoup d'engage en face : … · Beaucoup de poke en face : … · Beaucoup d'AP en face (ou d'AD) | PR C |
| A-14 | P3 | CO-050 à CO-052, CO-159, CO-160, CO-164, CO-165 | draft-coaching.js:217-219, 431-437 | Tuer un tank en voie… · l'objectif de voie · Avant de quitter ta voie · Tu n'as pas de voie · Ta voie est courte | lane partout : « Tuer un tank en lane… », « Avant de quitter ta lane… » | PR C |
| A-15 | P3 | CO-052, CO-159, CO-161, CO-162, CO-163, CO-167, UI-175 | draft-coaching.js:219, 431-439 ; index.html:1920 | le meurtre · une mise à mort · Une balise · Une téléportation · tes mates · tes ultimes | le kill · un kill · Une ward · Un TP · tes alliés · tes ults | PR C |
| A-16 | P3 | CO-108 | draft-coaching.js:306 | Équipe de poke : si vous arrivez affaiblis, le combat démarre mal. | Comp poke : si ton équipe arrive affaiblie, le teamfight démarre mal. Engage vite, ou reste hors de portée. | PR C |
| A-17 | P3 | EN-004 à EN-009, UI-184 | draft-engine.js:124-129 ; index.html:1931 | Règle heuristique : … · Heuristique (écrit à la main) : | Règle écrite à la main : … · Écrit à la main : | PR C |
| A-18 | P3 | UI-123, UI-238, UI-242 | index.html:1693, 2203, 2226 | 2 adversaire(s) sur 5 · 4 absente(s) · 1 partie(s) ajoutée(s) | Accord selon le nombre : « 2 adversaires sur 5 », « 4 absentes », « 1 partie ajoutée » | PR C |
| A-19 | P3 | PR-002, PR-003 | draft-profiles.js:131-158 | Champion inconnu dans top : … · Favori ignoré pour top · laissé vide : support | Noms affichés des rôles : « dans Top », « pour Top », « laissé vide : Support » | PR C |
| A-20 | P3 | DA-014 | draft-data.js:74, 143 | Silver à Émeraude, classé solo/duo | Silver à Emerald, classé solo/duo (une seule langue pour les rangs) | PR C |
| A-21 | P3 | UI-084, UI-212 | index.html:1562, 2064 | Danger {pire critère} (fenêtre) · Danger : {champion} (panneau, autre calcul) | « Risque : {pire critère} » dans la fenêtre, « Menace : {champion} » dans le panneau | PR C |
| A-22 | P3 | UI-192 | index.html:1977 | Je prends | Je joue (comme le bouton principal) | PR C |
| A-23 | P3 | EN-049, EN-050 | draft-engine.js:439-454 | Mage de carte : pousse, contrôle une cible… · Tueur de tanks : fait fondre la première ligne. · Tank : encaisse en première ligne. | Mage de map : push, stun une cible… · Anti-tank : fait fondre la frontline. · Tank : encaisse en frontline. | PR C |
| A-24 | P3 | EN-067, EN-068, EN-095, EN-096, EN-108 | draft-engine.js:553-554, 606-621 | Lance le combat sur X… · Repère X sur la carte… sans visuel, recule · Pose une ward de contrôle… · invisibles sur la carte | Engage sur X… · Repère X sur la map… sans vision, recule · Pose une control ward… · absents de la map | PR C |
| A-25 | P3 | CO-067, CO-117 | draft-coaching.js:59, 347 | corps à corps | mêlée | PR C |
| A-26 | P3 | UI-256, UI-257 | index.html:2281, 2287 | À égalité exacte, préférer · Supposer un frontline dans mon équipe | En cas d'égalité, mettre en premier · Compter sur une frontline dans mon équipe | PR C |
| A-27 | P3 | UI-049, UI-124, MA-012 | index.html:1359, 1694 ; draft-mastery.js:307 | Profil de l'équipe · Profil provisoire : … · Écart moteur de 1,20 points | Profil de leur comp · Fiche provisoire : … · Écart de score de 1,20 point | PR C (MA-012 dans PR D, même fonction) |
| A-28 | P3 | CO-154 | draft-coaching.js:425-426 | Dégâts physiques et magiques | Dégâts AD et AP (« L'armure réduit les dégâts physiques (AD)… ») | PR C |
| A-29 | P3 | UI-010 | index.html:823 | Il menait déjà de peu : le nom attendait la fin de la draft pour changer. | Il menait déjà de peu : le conseil change maintenant que la draft est complète. | PR D |
| A-30 | P3 | Menu natif (src) | src/DraftMid.cs:406-430 | Activation automatique pendant la draft · Positionnement automatique · Importer mes statistiques... | Mêmes mots que la page : « Afficher les panneaux pendant la draft », « Coller les panneaux au client LoL », « Importer mes statistiques (ancien format)... » | PR C (natif, à part) |

Points de style relevés sur l'ensemble (pas une entrée par texte) :
- **Tutoiement** : respecté partout. Une seule phrase avec « vous » (A-16), au sens « toi et ton équipe ».
- **« voie » et « lane »** coexistent (conseils contre moteur) : choisir « lane » (A-14).
- **Personnes grammaticales mélangées** dans les raisons : « le protéger » puis « te protéger » (A-07).
- **Coupures au milieu d'une phrase** : `coachReason` met en minuscule une raison du moteur et l'insère après un deux-points (A-09).
- **Mots d'outil** à éviter dans l'interface : « heuristique », « moteur », « score » sans contexte, « verdict » (A-17, A-27).

## 6. Messages présents dans le code mais jamais affichés

| ID | Pourquoi |
|---|---|
| CO-125, CO-138, CO-151 | Niveau débutant d'un conseil réservé aux niveaux 2 ou 3 (`minLevel`) : texte mort. |
| CO-168 | `COACH_TERMS` (définitions de frontline, carry, engage…) : jamais affichées. Elles pourraient servir d'infobulles, avec le vocabulaire du glossaire. |
| MA-025 | Sections « théorie » et « expérience » de `playerChoices` : calculées, jamais affichées. |
| — | `coachingSummary` (draft-coaching.js:506-524) : non utilisé par l'interface. |

## 7. Entrées visibles non déclenchées

| ID | Emplacement | Texte | Pourquoi |
|---|---|---|---|
| UI-001 | index.html:697 | DRAFT ne peut pas démarrer : le dossier draft-vendor ou le fichier draft-engine.js est introuvable. Replace-les à côté de ce fichier, puis recharge la page. | Erreur de démarrage (fichiers absents) : non provoquée. |
| UI-003 | index.html:746 | `pas de préférence` | Demande au moins 10 parties importées contre un mid, avec un écart sous 3 points : absent des statistiques fictives. |
| UI-005 | index.html:754 | aucune partie sur {C} | Le pick affiché avait toujours des parties dans les statistiques fictives. |
| UI-202 | index.html:2017-2019 | `^(Deux panneaux\|Panneau unique\|Passer à deux panneaux\|Passer au panneau unique\|Ouvrir les réglages\|Masquer\|Masquer les panneaux)$` | Boutons visibles seulement dans l'application (IS_HOST), pas dans un navigateur. |
| UI-223 | index.html:2122 | Profils exportés : rôles, champions et préférences, sans historique ni clé. | Non cliqué : l'export lance un téléchargement. |
| UI-226 | index.html:2142 | Fichier illisible : rien n'a été modifié. | Seulement si le navigateur ne peut pas lire le fichier. |
| UI-241 | index.html:2209-2210 | `(Rôles à surveiller\|plus souvent (Top\|Jungle\|Mid\|ADC\|Support) que dans)` | Demande un changement de rôle détecté sur plusieurs patchs : non simulé. |
| UI-242 | index.html:2226 | {N} partie(s) ajoutée(s) à ton journal. | Vu lors d'un passage précédent, pas lors du dernier : la note est remplacée très vite (relevé instable). |
| EN-065 | draft-engine.js:546 | À surveiller pendant la partie. | Inaccessible : threatsFor ne renvoie que des champions déjà couverts par les autres cas de threatText. |
| CO-134 | draft-coaching.js:379 | {C} : champion inconnu de DRAFT | Inaccessible : tous les champions proposés ont une fiche (Locke et Zaahen ont un profil provisoire). |
| CO-135 | draft-coaching.js:380 | Aucune fiche pour ce champion : il n'entre pas dans ce conseil. Regarde ses sorts en jeu. | Inaccessible (même raison que CO-134). |
| CO-136 | draft-coaching.js:381 | Rien n'est inventé pour un champion sans fiche. | Inaccessible (même raison que CO-134). |
| CO-161 | draft-coaching.js:433 | `^(Les objectifs gagnent les parties\|Une mise à mort rapporte davantage si elle mène à quelque chose.+)$` | Inaccessible : un principe plus prioritaire se déclenche toujours dans les mêmes conditions, et 2 principes seulement sont affichés. |
| CO-162 | draft-coaching.js:434 | `^(Voir avant d'avancer\|Une balise montre qui arrive\..+)$` | Inaccessible (même raison que CO-161 : « Se placer avant le combat » passe toujours devant). |
| MA-004 | draft-mastery.js:291 | Aucun champion disponible dans ton pool. | Inaccessible : sans pick, l'onglet s'arrête avant playerChoices. |
| MA-005 | draft-mastery.js:292 | La draft met plutôt en avant {liste} : ton choix reste possible, voici ce qu'il implique. | Inaccessible : l'interface ne passe jamais de pick choisi à playerChoices. |
| MA-023 | draft-mastery.js:279 | `( a changé$\|seulement depuis\. Ton expérience passée n'est peut-être plus aussi utile\.\|La liste des changements est fournie à la main\.)` | Demande une liste de changements de champions : non fournie. |
| PR-005 | draft-profiles.js:173,189 | `(Configuration invalide : rien n'a été modifié\.\|L'enregistrement a échoué : ta configuration précédente est conservée\.)` | Échec d'écriture du stockage : non simulé. |

## 8. Textes natifs (src/*.cs), classés à part

Ils ne sont pas dans l'inventaire de la page et n'ont pas été relevés à l'écran (menu de l'icône, bulles et boîtes Windows). Lecture du code seulement.

| # | Emplacement | Texte | Remarque |
|---|---|---|---|
| N-01 | DraftDetection.cs:21-25 (menu, bulle d'icône `DRAFT : {phase}`, Réglages avancés) | Client LoL fermé · Client ouvert, hors draft (estimation) · Draft en cours (estimation) · Partie en cours · Phase incertaine | Correct. |
| N-02 | DraftMid.cs:406, 1217 | Afficher / Masquer les panneaux (Ctrl+Alt+D) | Correct. |
| N-03 | DraftMid.cs:407 | Activation automatique pendant la draft | A-30 : la page dit « Afficher les panneaux automatiquement pendant la draft ». |
| N-04 | DraftMid.cs:408 | Positionnement automatique | A-30 : la page dit « Coller les panneaux au client LoL ». |
| N-05 | DraftMid.cs:409-410 | Panneau unique · Démarrer avec Windows | Correct. |
| N-06 | DraftMid.cs:413-414 | C'est une draft : retenir cet écran · Ce n'est pas une draft : retenir cet écran | Correct. |
| N-07 | DraftMid.cs:419-423 | Replacer les panneaux · Analyse détaillée · Réglages · Importer mes statistiques... | A-30 : « statistiques » (ancien import) se confond avec « Importer mon historique Riot ». |
| N-08 | DraftMid.cs:424-425, 1215 | Mettre à jour les rôles... · Mettre à jour les matchups... · Arrêter le calcul en cours | Correct. |
| N-09 | DraftMid.cs:430, 1214 | Rechercher une mise à jour... · Mise à jour en cours... | Correct. |
| N-10 | DraftMid.cs:432 | Quitter | Correct. |
| N-11 | DraftMid.cs:1184-1185 (bulles) | Le client doit être visible à l'écran pour retenir cette vue. · Aucun portrait au centre : cet écran n'a pas été retenu comme une draft. | Correct. |
| N-12 | DraftMid.cs:742-799 (Réglages + bulle) | Import de l'historique : démarrage... · Import de l'historique : {n} sur {total} parties · Une draft est en cours : lance l'import après. · Import terminé : … | Correct. |
| N-13 | DraftMid.cs:806-865 (Réglages + bulle) | Calcul des {rôles/matchups} : {n} sur {total} parties · Rôles déjà à jour : aucune requête dépensée. | Correct. |
| N-14 | DraftMid.cs:1787-1838 | Recherche d'une mise à jour... · Téléchargement : {n} % · Vérification de la nouvelle version... · Mise à jour installée : version {v}. · La nouvelle version n'a pas démarré : la précédente a été remise en place. | Correct. |
| N-15 | DraftMid.cs:1759 (boîte) | La version {v} est disponible (tu as la {v}). DRAFT va la télécharger, la vérifier, puis redémarrer… | Correct. |
| N-16 | DraftMid.cs:916, 2847 (boîtes d'erreur) | DRAFT n'a pas pu démarrer. Détail dans … · DRAFT ne peut pas écrire dans ce dossier : … | Correct. |
| N-17 | DraftMid.cs:1650 | Ce fichier n'est pas un import DRAFT valide : rien n'a été modifié. | Correct. |
| N-18 | DraftMid.cs:2390 | DRAFT a besoin du composant gratuit Microsoft Edge WebView2… | Correct. |
| N-19 | DraftMid.cs:2488, 2495, 2876 | DRAFT est installé… · Désinstaller DRAFT ? … · DRAFT est déjà ouvert : utilise son icône près de l'horloge. | Correct. |
| N-20 | DraftMid.cs:2857 | Prototype OP.GG : indique un dossier de test avec --data. | Outil de développement, pas pour le joueur. |
| N-21 | HistoryImporter.cs:276-311 | DRAFT : importer mon historique Riot · J'accepte cette lecture · Obtenir une clé sur developer.riotgames.com · Importer · Annuler | Correct. |
| N-22 | HistoryImporter.cs:167, 252, 260 | Limite de requêtes de Riot atteinte : reprise dans {n} secondes. Rien n'est perdu. · Compte invalide : le format attendu est Nom#TAG. · Réponse de Riot illisible : … | Correct. |
| N-23 | RoleRatesUpdater.cs:702-711 | Clé Riot (masquée, jamais enregistrée) : · Région : · Lancer · Annuler | Correct. |
| N-24 | RoleRatesUpdater.cs:252, 261 | Réponse de Riot illisible. · Écriture du fichier impossible. | Correct. |
| N-25 | DraftMid.cs (menu de l'icône, 2.16.2) | Diagnostic de connexion LoL... | Correct. Ajouté par la 2.16.2. |
| N-26 | LcuConnector.cs, LcuDiagnostic (2.16.2) | Fenêtre « DRAFT : diagnostic de connexion LoL » : étapes (Fenêtre du client, Lockfile, Proxy Windows, Connexion locale, Lecture de la phase) et « Conclusion : … » ; « Ce texte est copié : colle-le dans ton message. » | Correct. Textes de diagnostic volontairement factuels ; à traduire avec les textes natifs (I18N-3). |

## 9. Limites

- Les drafts et données « calculées » du relevé sont **synthétiques**. Les comptes mesurent l'ampleur des défauts sur ces drafts, pas leur fréquence en vraie partie.
- Les constats sont vérifiés au niveau débutant des conseils (le niveau par défaut). Les textes des niveaux 2 et 3 sont relevés, mais certains contrôles propres à ces niveaux (B-12, B-13) reposent sur la lecture du code.
- Les textes natifs n'ont pas été vus à l'écran.
- Le relevé n'évalue pas la mise en page : `tools/run-layout-tests.ps1` doit être relancé après chaque PR de texte.
- UI-242 a été vu dans un passage et pas dans un autre : la note d'import est remplacée très vite.

## 10. Inventaire complet

Colonne « Vu à l'écran » : nombre de relevés pendant le dernier passage. Colonne « Classement » : entrées A ou B qui le concernent.

### web/index.html (269 entrées, 261 vues à l'écran)

| ID | Emplacement | Contexte et calcul | Texte ou gabarit | Vu à l'écran | Classement |
|---|---|---|---|---|---|
| UI-001 | index.html:697 | Erreur de démarrage, fichiers absents | DRAFT ne peut pas démarrer : le dossier draft-vendor ou le fichier draft-engine.js est introuvable. Replace-les à côté de ce fichier, puis recharge la page. | non | — |
| UI-002 | index.html:744 | Tes parties (Plus de détails), matchupWord, moins de 10 parties | `pas assez de parties` | oui (31) | — |
| UI-003 | index.html:746 | Tes parties, matchupWord, écart < 3 points | `pas de préférence` | non | — |
| UI-004 | index.html:747 | Tes parties, matchupWord, écart ≥ 3 points | `léger (dés)?avantage` | oui (157) | — |
| UI-005 | index.html:754 | Tes parties, personalLine, aucune partie | aucune partie sur {C} | non | — |
| UI-006 | index.html:758 | Tes parties, personalLine | {bilan}, jamais contre {C} | oui (1) | — |
| UI-007 | index.html:759 | Tes parties, personalLine | {bilan}, contre {C} {bilan} : {mot} | oui (33) | — |
| UI-008 | index.html:735,1838 | Bilan V/D (fmtRecord), cartes du pool, statistiques | `^\d+V \d+D(, \d+ %)?$` | oui (264) | — |
| UI-009 | index.html:821 | Changement de conseil (describeChange), pick pris en face | {C} est pris en face | oui (6) | — |
| UI-010 | index.html:823 | Changement de conseil, draft complète sans nouvelle raison | Il menait déjà de peu : le nom attendait la fin de la draft pour changer. | oui (468) | A-29 |
| UI-011 | index.html:1546,2051 | Bandeau de changement (Stage, panneau droit) : action en gras, puis « X passe devant Y » | `( passe devant \|^(Draft complète\|Ajout de .+\|Retrait de .+\|.+ remplacé par .+\|.+ (au top\|à la jungle\|au mid\|à l'ADC\|au support)) :$)` | oui (6343) | B-16 |
| UI-012 | index.html:1036 | Action / notice après ajout (state.lastAction) | Ajout de {C} | oui (6184) | — |
| UI-013 | index.html:1050 | Action / notice après retrait | Retrait de {C} | oui (1) | — |
| UI-014 | index.html:997 | Notice, remplacement par la lecture du client | {C} remplacé par {C} | oui (1) | — |
| UI-015 | index.html:1076 | Notice après correction de l'adversaire direct (setMid) | `^.+ (au top\|à la jungle\|au mid\|à l'ADC\|au support)$` | oui (21591) | — |
| UI-016 | index.html:1006,1093 | Notice après correction d'un rôle | `^.+ : (Top\|Jungle\|Mid\|ADC\|Support)$` | oui (3) | — |
| UI-017 | index.html:983 | Notice début / fin de sélection (lecture du client) | `^Sélection (en cours\|terminée)$` | oui (2) | — |
| UI-018 | index.html:1232 | Champ de recherche, rôle visé | Prochain champion : {Rôle} | oui (2) | — |
| UI-019 | index.html:1235 | Champ de recherche (aria) | Ajouter un champion adverse | oui (7938) | — |
| UI-020 | index.html:1236 | Champ de recherche plein | Draft complète | oui (5914) | — |
| UI-021 | index.html:1236 | Champ de recherche (placeholder) | Ajouter un adversaire : ahri, tf, kata... | oui (9960) | — |
| UI-022 | index.html:1244 | Recherche sans résultat | Aucun champion ne correspond à « {q} ». | oui (1) | — |
| UI-023 | index.html:1262 | Infobulle « Je joue » (CTA_HELP) | Note ce choix dans ton journal. Rien n'est envoyé au client LoL : tu verrouilles toi-même ton champion. | oui (18379) | — |
| UI-024 | index.html:1263 | Infobulle « Choix serré » (CLOSE_HELP) | Deux de tes champions ont des scores proches : les deux se défendent. | oui (3920) | — |
| UI-025 | index.html:1264 | Infobulle ligne adversaire (OPPONENT_HELP) | Le client ne donne pas les rôles adverses : cet adversaire est déduit, et se corrige dans la composition. | oui (8967) | — |
| UI-026 | index.html:1266,1308 | Étiquette de l'adversaire direct, confiance élevée | `^(Top\|Jungle\|Mid\|ADC\|Support) très probable( \|$)` | oui (74) | B-05, A-10 |
| UI-027 | index.html:1266,1308 | Étiquette de l'adversaire direct, confiance moyenne | `^(Top\|Jungle\|Mid\|ADC\|Support) probable( \|$)` | oui (10564) | B-05, A-10 |
| UI-028 | index.html:1266,1308 | Étiquette de l'adversaire direct, confiance faible (champion placé par deriveSlots) | `^(Top\|Jungle\|Mid\|ADC\|Support) non identifié( (Tank\|Bruiser\|Carry\|Plongeur\|Contrôle\|Engage\|Soutien\|Profil inconnu).*)?$` | oui (298) | B-06, A-10 |
| UI-029 | index.html:1274 | Ligne adversaire (hypothesisText), personne de plausible | `^(Top\|Jungle\|Mid\|ADC\|Support) pas encore visible\.$` | oui (4333) | A-10 |
| UI-030 | index.html:1275 | Ligne adversaire, confiance faible | `^(Top\|Jungle\|Mid\|ADC\|Support) non identifié :` | oui (1312) | A-10 |
| UI-031 | index.html:1276 | Ligne adversaire, adversaire nommé | `(Top\|Jungle\|Mid\|ADC\|Support) supposé :` | oui (6825) | A-10 |
| UI-032 | index.html:1284 | Ligne adversaire, midSwapEffect | Si c'est {C} : {C}. | oui (268) | — |
| UI-033 | index.html:1298 | Rôle adverse rempli (aria) | `^(Top\|Jungle\|Mid\|ADC\|Support) : .+\. (Échanger avec .+\|Sélectionner pour changer de rôle)$` | oui (27066) | B-06 |
| UI-034 | index.html:1299 | Rôle adverse vide (aria) | `^(Top\|Jungle\|Mid\|ADC\|Support) vide\. (Y placer .+\|Choisir ce rôle pour le prochain champion)$` | oui (12624) | — |
| UI-035 | index.html:1312 | Bouton « placer au rôle du joueur » (aria) | `^Placer .+ (au top\|à la jungle\|au mid\|à l'ADC\|au support)$` | oui (21590) | — |
| UI-036 | index.html:1313,2177 | Bouton retirer (aria) | `^Retirer [^ ].*$` | oui (27190) | — |
| UI-037 | index.html:1315 | Rôle visé, en attente de saisie | Tape son nom | oui (1) | — |
| UI-038 | index.html:1315 | Rôle vide | `^(Ajouter )?(l'ADC\|le (Top\|Jungle\|Mid\|Support))$` | oui (25246) | — |
| UI-039 | index.html:1196-1204 | Profil d'un adversaire (traitsOf) | `^(Profil inconnu\|Soutien\|(Tank\|Bruiser\|Carry\|Plongeur\|Contrôle\|Engage)(, (Tank\|Bruiser\|Carry\|Plongeur\|Contrôle\|Engage))*)$` | oui (56297) | A-03 |
| UI-040 | index.html:1328 | Jauge (aria) | `^(Frontline\|Carrys fragiles\|Plongeurs\|Contrôles ciblés\|Engage) : \d sur 5$` | oui (38165) | A-02 |
| UI-041 | index.html:1361-1365 | Jauges du profil adverse | `^(Frontline\|Carrys fragiles\|Plongeurs\|Contrôles ciblés\|Engage)$` | oui (39287) | A-02 |
| UI-042 | index.html:1336 | Profil adverse vide | Le profil se remplit à chaque adversaire ajouté. | oui (36) | — |
| UI-043 | index.html:1340 | Profil adverse : forces (compoReading) | Leurs forces : {liste} | oui (3921) | A-04 |
| UI-044 | index.html:1341 | Profil adverse : failles (compoReading) | Leurs failles : {liste} | oui (3819) | A-04 |
| UI-045 | index.html:1342 | Profil adverse sans trait marqué | Pas de profil marqué pour l'instant. | oui (2324) | — |
| UI-046 | index.html:1350-1351 | Titre colonne gauche | `^(Composition adverse( \d sur 5)?\|\d sur 5)$` | oui (22899) | — |
| UI-047 | index.html:1353 | Aide après sélection d'un adversaire | {C} sélectionné : clique sur un autre rôle. | oui (1) | — |
| UI-048 | index.html:1353 | Aide clavier | Entrée ajoute, Échap efface. | oui (36) | — |
| UI-049 | index.html:1359 | Titre des jauges | Profil de l'équipe | oui (7633) | A-27 |
| UI-050 | index.html:1394 | Illustration absente | Illustration indisponible : vérifie ta connexion. | oui (6020) | — |
| UI-051 | index.html:1400 | Lecteur d'écran, égalité | Égalité entre {liste} | oui (2987) | — |
| UI-052 | index.html:1400 | Lecteur d'écran, conseil | `^Conseil : .+` | oui (6020) | — |
| UI-053 | index.html:1405 | Égalité (aria) | Champions à égalité | oui (2987) | — |
| UI-054 | index.html:1406 | Égalité, titre | `^\d+ choix équivalents( sur cette draft)?$` | oui (5974) | B-04 |
| UI-055 | index.html:1412 | Égalité, favori marqué | Ton favori | oui (5) | — |
| UI-056 | index.html:1417 | Égalité, note sous la liste | L'ordre d'affichage n'a pas de sens. | oui (2987) | B-04 |
| UI-057 | index.html:1464 | Conseil pendant la draft (aria) | Conseil | oui (6823) | — |
| UI-058 | index.html:1467 | Lien vers l'onglet Conseils | Voir les conseils | oui (6822) | — |
| UI-059 | index.html:1471 | Infobulle « Le plus joué » | Nombre de parties importées dans ce rôle. Cela ne dit pas si tu joues bien ce champion. | oui (2432) | — |
| UI-060 | index.html:1475 | Ligne « Le plus joué » | `^Le plus joué( .+ · \d+ parties)?$` | oui (234) | — |
| UI-061 | index.html:1487-1489 | Réglage du niveau | `^(Niveau des conseils\|Débutant\|Intermédiaire\|Avancé)$` | oui (9390) | — |
| UI-062 | index.html:1495 | Onglet Conseils vide | Aucun conseil disponible pour l'instant. | oui (3) | — |
| UI-063 | index.html:1502 | Onglet Conseils, bloc choix (aria) | Ton choix | oui (2315) | — |
| UI-064 | index.html:1504 | Onglet Conseils, le plus joué | Le plus joué dans ce rôle | oui (2315) | — |
| UI-065 | index.html:1504 | Onglet Conseils, pas de champion assez joué | Données insuffisantes | oui (2288) | — |
| UI-066 | index.html:1507 | Onglet Conseils, titre | Expérience dans ce rôle | oui (30) | — |
| UI-067 | index.html:1508 | Onglet Conseils, liste d'expérience | `^.+ \d+ parties(, dont \d+ ce mois-ci\|, dernière il y a \d+ jours)?$` | oui (317) | — |
| UI-068 | index.html:1509 | Onglet Conseils, champions sans partie | Aucune partie importée sur {liste}. | oui (12) | — |
| UI-069 | index.html:1510 | Onglet Conseils, historique importé | Historique importé{x} parties classées solo/duo. Les autres modes et les parties non lues ne sont pas comptés. | oui (30) | — |
| UI-070 | index.html:1510 | Onglet Conseils, sans historique | Aucun historique importé : tu peux le faire dans les réglages. | oui (2285) | — |
| UI-071 | index.html:1510 | Onglet Conseils, limite de l'historique | Le nombre de parties ne dit pas si tu joues bien un champion, et il ne change pas le classement. | oui (2315) | — |
| UI-072 | index.html:1511 | Onglet Conseils, titre | Conseils issus d'une donnée ou d'un calcul | oui (2315) | — |
| UI-073 | index.html:1512 | Onglet Conseils, titre | À confirmer | oui (2315) | — |
| UI-074 | index.html:1513 | Onglet Conseils, avertissement | Avis écrits à la main, pas encore validés par un joueur. Garde ton jugement. | oui (2315) | — |
| UI-075 | index.html:1515 | Onglet Conseils, titre | Ce qui reste incertain | oui (2315) | — |
| UI-076 | index.html:1522,1542 | Zone de recommandation (aria) | Recommandation | oui (7549) | — |
| UI-077 | index.html:1523 | Aucun pick disponible | Aucun pick | oui (15) | — |
| UI-078 | index.html:1534,2040 | Badge égalité | Égalité | oui (2987) | — |
| UI-079 | index.html:1534,1552,2041 | Badge draft incomplète | `^\d/5 adversaires$` | oui (5913) | — |
| UI-080 | index.html:1536,1556,2054 | Alternative affichée | `^(Sinon\|Ou)( .+\|[A-Z].*)?$` | oui (12706) | — |
| UI-081 | index.html:1551,2040 | Badge Choix net | Choix net | oui (2068) | B-24 |
| UI-082 | index.html:1551,2040 | Badge Choix serré | Choix serré | oui (3920) | — |
| UI-083 | index.html:1559 | Aucune raison positive | Aucun argument particulier dans cette draft | oui (244) | — |
| UI-084 | index.html:1562 | Risque principal (mainRisk) | `^Danger( .+)?$` | oui (3184) | A-21 |
| UI-085 | index.html:1567 | Repli | Plus de détails | oui (863) | — |
| UI-086 | index.html:1568 | Picks à éviter (tableau) | À éviter : {liste} | oui (413) | — |
| UI-087 | index.html:1569 | Bilan personnel | Tes parties : {x} | oui (49) | — |
| UI-088 | index.html:1570 | Pick sans frontline allié (V.without) | Sans frontline : {C} | oui (489) | B-10 |
| UI-089 | index.html:1574,2077 | Bouton principal | `^(Je joue .+\|.+ noté)$` | oui (6020) | — |
| UI-090 | index.html:1581 | Étiquette du pool | À égalité | oui (6230) | — |
| UI-091 | index.html:1582 | Étiquette du pool | À éviter | oui (461) | — |
| UI-092 | index.html:1583 | Étiquette du pool, 1er | Recommandé | oui (2727) | B-03 |
| UI-093 | index.html:1583-1584 | Étiquette du pool, choix serré | Très proche | oui (6557) | — |
| UI-094 | index.html:1584,2054 | Étiquette du pool ou du panneau, 2e | `^Alternative` | oui (2119) | — |
| UI-095 | index.html:1585 | Étiquette du pool, score négatif | Peu adapté | oui (1867) | B-17 |
| UI-096 | index.html:1585 | Étiquette du pool, autres | Possible | oui (5463) | — |
| UI-097 | index.html:1596-1597 | Titre du pool | `^(Ton pool\|Les \d+ meilleurs sur \d+)` | oui (16418) | — |
| UI-098 | index.html:1597 | Sous-titre du pool | Clique sur le champion que tu joues | oui (12458) | — |
| UI-099 | index.html:1608 | Pool, champion choisi | `^(Champion choisi\|Choisi)$` | oui (4) | — |
| UI-100 | index.html:1632,1901 | Résultat de partie | `^(Victoire\|Défaite\|Ignorer\|Ignorée\|En attente)$` | oui (22) | — |
| UI-101 | index.html:1647-1648 | Titre du plan | `^(Plan de jeu( avec .+)?\|avec .+)$` | oui (20249) | — |
| UI-102 | index.html:1650,2074 | Condition de victoire | `^Gagne si` | oui (15988) | — |
| UI-103 | index.html:1650 | Plan sans champion | `Choisis un champion\.$` | oui (2419) | — |
| UI-104 | index.html:1652 | Plan, ligne menaces | `^Menaces$` | oui (7549) | — |
| UI-105 | index.html:1652 | Plan, aucune menace | Rien de particulier | oui (3387) | — |
| UI-106 | index.html:1653 | Plan, ligne cibles | `^Cibles$` | oui (7549) | — |
| UI-107 | index.html:1653 | Plan, aucune cible | Le carry le plus exposé | oui (3071) | — |
| UI-108 | index.html:1654 | Plan, ligne bans | `^Bans$` | oui (7549) | — |
| UI-109 | index.html:1654 | Plan, aucun ban | Aucun | oui (760) | — |
| UI-110 | index.html:1655,2081 | Dernière partie à noter | Dernière partie : {C} | oui (4) | — |
| UI-111 | index.html:1660 | Phases du plan | `^(Lane\|Mid game\|Teamfights)$` | oui (4704) | — |
| UI-112 | index.html:1666 | Plan à égalité | Choisis un des champions à égalité pour voir son plan. | oui (592) | — |
| UI-113 | index.html:1666 | Plan sans pick | Aucun pick disponible. | oui (6) | — |
| UI-114 | index.html:1670 | Phases (aria) | Phase de jeu | oui (1568) | — |
| UI-115 | index.html:1676 | Plan, limite | Avis général basé sur les 5 champions adverses, sans cooldowns ni builds vérifiés. | oui (1568) | B-11 |
| UI-116 | index.html:1681,1705 | Matrice (aria) | `: (avantage\|désavantage\|neutre)$` | oui (10358) | — |
| UI-117 | index.html:1687 | Analyse, confiance | `Confiance (faible\|moyenne\|élevée)\.` | oui (1579) | A-10 |
| UI-118 | index.html:1688 | Analyse, scénarios | Classement pondéré entre ces hypothèses, avec les mêmes règles. | oui (616) | — |
| UI-119 | index.html:1689 | Analyse, adversaire inconnu | `(Top\|Jungle\|Mid\|ADC\|Support) inconnu : le matchup direct n'est pas évalué\.` | oui (663) | B-09 |
| UI-120 | index.html:1690 | Analyse, pas de données de matchup | Pas de données de matchup contre {C} : ce critère n'est pas évalué. | oui (712) | — |
| UI-121 | index.html:1691 | Analyse, égalité | ont le même score : l'ordre d'affichage n'a pas de sens. | oui (591) | B-04 |
| UI-122 | index.html:1692 | Analyse, nom maintenu (holdPick) | reste affiché : {C} mène de trop peu pour changer de nom en cours de draft. | oui (157) | — |
| UI-123 | index.html:1693 | Analyse, draft incomplète | Draft incomplète : {N} adversaire(s) sur 5. | oui (1309) | A-18 |
| UI-124 | index.html:1694 | Analyse, profil provisoire | Profil provisoire : {liste}. | oui (24) | A-27 |
| UI-125 | index.html:1695 | Analyse, frontline allié (toujours ajouté) | `Frontline allié supposé\.` | oui (1580) | B-07, A-12 |
| UI-126 | index.html:1698-1699 | Matrice, en-tête | `^(Critère\|Critères par champion)$` | oui (3529) | — |
| UI-127 | index.html:1705 | Matrice, case vide (title) | Non concerné | oui (4821) | — |
| UI-128 | index.html:1720 | Analyse, aucun critère | Aucun critère ne le distingue dans cette draft. | oui (2366) | — |
| UI-129 | index.html:1721 | Analyse, menaces | Menaces : {liste} | oui (3764) | — |
| UI-130 | index.html:1722 | Analyse, cibles | Opportunités : {liste} | oui (4209) | — |
| UI-131 | index.html:1723 | Analyse, condition de victoire | Condition de victoire : {x} | oui (5312) | — |
| UI-132 | index.html:1724 | Analyse, lien externe | Vérifier ce matchup sur LoLalytics | oui (642) | — |
| UI-133 | index.html:1727 | Analyse, titre bans | Bans conseillés | oui (1580) | — |
| UI-134 | index.html:1737 | Comparer (aria) | pour le premier, {x} pour le second | oui (717) | — |
| UI-135 | index.html:1744 | Comparer, un seul pick | Il faut au moins deux picks disponibles pour comparer. | oui (5) | — |
| UI-136 | index.html:1755 | Comparer (aria) | `^(Premier\|Second) champion$` | oui (728) | — |
| UI-137 | index.html:1761-1762 | Comparer, titre | `^(VS\|.+ contre .+)$` | oui (9360) | — |
| UI-138 | index.html:1763 | Comparer, égalité | À égalité sur le total des critères. | oui (105) | — |
| UI-139 | index.html:1763 | Comparer, écart faible | Serré, léger avantage à {C}. | oui (157) | — |
| UI-140 | index.html:1763 | Comparer, écart net | Avantage net à {C}. | oui (102) | — |
| UI-141 | index.html:1763 | Comparer, critère décisif | gagne surtout sur « {critère} » | oui (259) | — |
| UI-142 | index.html:1758 | Comparer, avantage sans texte | Moins pénalisé : {critère} | oui (32) | — |
| UI-143 | index.html:1765 | Comparer, aucun écart | Aucun critère ne les sépare pour l'instant. | oui (34) | — |
| UI-144 | index.html:1776 | Comparer, titre whenBetter | {C} passerait devant | oui (391) | B-10 |
| UI-145 | index.html:1777 | Comparer, whenBetter vide | Dans aucun des cas simples testés : changement de mid, absence de frontline ou nouvel adversaire type. | oui (326) | B-10 |
| UI-146 | index.html:1785 | Duel, aucun bilan hors tableau | Pas encore de bilans pour ce profil : le conseil repose sur la composition adverse et les fiches de champions. Lance Mettre à jour les matchups dans les réglages. | oui (311) | — |
| UI-147 | index.html:1789 | Duel, bilans hors tableau | Regarde tes bilans contre un adversaire, sans toucher à ta draft. | oui (4) | — |
| UI-148 | index.html:1790 | Duel, recherche (aria, placeholder) | `^(Chercher un adversaire\|Adversaire : ahri, tf, kata\.\.\.)$` | oui (12) | — |
| UI-149 | index.html:1797,1818 | Duel, bilans | `^Bilans :` | oui (8) | — |
| UI-150 | index.html:1797 | Duel, pas de bilan | Moins de {N} parties : pas de bilan. | oui (2) | — |
| UI-151 | index.html:1804 | Duel, tableau mid | Regarde quoi prendre contre un mid, sans toucher à ta draft. | oui (62) | — |
| UI-152 | index.html:1805 | Duel, recherche mid (aria, placeholder) | `^(Chercher un mid adverse\|Mid adverse : ahri, tf, kata\.\.\.)$` | oui (186) | — |
| UI-153 | index.html:1807 | Duel, absent du tableau | `^Pas dans le tableau\.` | oui (2) | — |
| UI-154 | index.html:1807 | Duel, défaut Twisted Fate | Par défaut, prends Twisted Fate. | oui (2) | B-20 |
| UI-155 | index.html:1815 | Duel, choix | Prends {C}, sinon {C} | oui (2) | — |
| UI-156 | index.html:1816 | Duel, à éviter | `^À éviter :` | oui (413) | — |
| UI-157 | index.html:1819 | Duel, source | Avis écrit à la main, pas encore de bilan. | oui (1) | — |
| UI-158 | index.html:1842 | Statistiques, limite | Sans pourcentage : moins de {N} parties, trop peu pour conclure. | oui (11) | — |
| UI-159 | index.html:1860 | Statistiques absentes | `(Pas encore de statistiques\. Lance draft-import-stats\.bat, placé dans le même dossier que cette page, puis recharge-la\.\|^draft-import-stats\.bat$)` | oui (734) | — |
| UI-160 | index.html:1862 | Statistiques, en-tête | Ces bilans décrivent le passé et ne changent pas le verdict. | oui (3) | — |
| UI-161 | index.html:1866-1875 | Statistiques, titres | `^(Victoires par champion, .+\|Selon le type de mid adverse\|Évolution par tranche de \d+ parties\|Total\|Mon pool\|Hors de ce pool\|Hors pool, par rôle\|Hors pool, par champion\|Parties\|Bilan\|Winrate\|\d+ à \d+\|Autre)$` | oui (7625) | — |
| UI-162 | index.html:1883 | Journal vide | Ton journal est vide : clique sur « Je joue » pendant une draft pour commencer. | oui (369) | — |
| UI-163 | index.html:1889-1895 | Journal, titres | `^(Dernières parties\|Victoires par champion joué\|Compositions récentes)$` | oui (3) | — |
| UI-164 | index.html:1890-1891 | Journal (aria, title) | `^(\d+ victoires sur \d+ parties notées\|.+ : (victoire\|défaite))$` | oui (2) | — |
| UI-165 | index.html:1897 | Journal, bilan suivi | Conseil suivi : {x}. Autre choix : {x}. Ces chiffres ne prouvent pas que l'outil aide : tu ne choisis pas au hasard, et il faut plusieurs centaines de parties pour voir un écart fiable. | oui (1) | — |
| UI-166 | index.html:1898 | Journal, en-têtes | `^(Date\|Pick\|Mid\|Conseil\|Résultat)$` | oui (23444) | — |
| UI-167 | index.html:1902 | Journal, export | Télécharger le journal | oui (1) | — |
| UI-168 | index.html:1907 | Règles, titre (pool hors tableau) | Comment tes picks sont classés | oui (320) | — |
| UI-169 | index.html:1908 | Règles, texte (pool hors tableau) | Chaque champion a une fiche écrite à la main : chasseur de cibles fragiles, tueur de tanks, immobile, protecteur, etc. Le moteur compare ces fiches à la composition adverse. C'est un avis général, sans statistiques. | oui (320) | — |
| UI-170 | index.html:1908-1913 | Règles mid, tableau | `^(Selon la composition adverse\|Situation\|Au moins 2 tanks ou bruisers, et tu as un frontline\|Au moins 2 carrys fragiles, et peu de contrôles ciblés\|Adversaires lents et peu de plongeurs, et tu as un frontline\|Tous les autres cas, ou draft inconnue)$` | oui (294) | B-19, A-12 |
| UI-171 | index.html:1915 | Règles mid, titre | Quand éviter chaque pick | oui (49) | B-19 |
| UI-172 | index.html:1917 | Règles mid | ton équipe n'a pas de frontline, ou l'équipe adverse a au moins 2 assassins ou plongeurs. | oui (49) | B-19, A-12 |
| UI-173 | index.html:1918 | Règles mid | aucune cible fragile accessible, ou beaucoup de contrôles ciblés en face. | oui (49) | B-19 |
| UI-174 | index.html:1919 | Règles mid | plusieurs champions très mobiles en face, et ton équipe ne peut pas te protéger. | oui (49) | B-19 |
| UI-175 | index.html:1920 | Règles mid | rarement mauvais, mais moins utile si tes mates ne suivent jamais tes ultimes. | oui (49) | B-19, A-15 |
| UI-176 | index.html:1922 | Règles, titre | Critères et poids du moteur | oui (369) | — |
| UI-177 | index.html:1923 | Règles, poids | Chaque critère donne une note de -2 à +2, multipliée par son poids. Le total sert seulement à comparer tes picks : ce n'est ni un winrate ni une probabilité. Ces poids sont des choix de conception, jamais mesurés sur de vraies parties. | oui (369) | — |
| UI-178 | index.html:1924 | Règles, en-têtes | `^(Poids\|D'où ça vient)$` | oui (738) | — |
| UI-179 | index.html:1927 | Règles, titre | Provenance des données | oui (369) | — |
| UI-180 | index.html:1929 | Règles, provenance | noms, portraits, illustrations, numéro de patch, classes de Locke et Zaahen. | oui (369) | — |
| UI-181 | index.html:1930 | Règles, provenance | rôles et bilans de matchups, si tu les as calculés avec l'API Riot. Le lien LoLalytics ouvre simplement leur page. | oui (369) | — |
| UI-182 | index.html:1931 | Règles, provenance | matchups sans assez de parties, profils de champions, règles, plan de jeu. | oui (369) | — |
| UI-183 | index.html:1932 | Règles, provenance | ton journal et ton import Riot. Affichés, mais sans effet sur le classement. | oui (369) | — |
| UI-184 | index.html:1929-1932 | Règles, étiquettes de provenance | `^(Officiel \(Riot, Data Dragon\) :\|Statistiques observées :\|Heuristique \(écrit à la main\) :\|Personnel :\|Kog'Maw :\|Fizz :\|Xerath :\|Twisted Fate :)$` | oui (1672) | A-17 |
| UI-185 | index.html:1934 | Règles, titre | Fiabilité | oui (369) | — |
| UI-186 | index.html:1935 | Règles, fiabilité | L'outil suppose que ton équipe a un frontline et le signale si ça changerait le verdict. Le nom affiché ne change que si un autre pick mène nettement. Les autres rôles servent à l'affichage. Le ban conseillé vient des tableaux de cet outil. | oui (369) | B-18, A-12 |
| UI-187 | index.html:1935 | Règles, fiabilité (suite) | La plupart des conseils sont un avis général, sans statistiques actuelles ni vérification du patch 16.21. Seuls les bilans « Tes parties » viennent de tes comptes, et ils portent souvent sur peu de parties. | oui (369) | B-18 |
| UI-188 | index.html:1936 | Mention légale | Outil personnel non approuvé par Riot Games. Les portraits et illustrations appartiennent à Riot Games. | oui (369) | — |
| UI-189 | index.html:1941,1947 | Vues d'Analyse | `^(Critères\|Comparer\|Règles\|Vue de l'analyse)$` | oui (9272) | — |
| UI-190 | index.html:1953,1960 | Onglets | `^(Plan\|Analyse\|Conseils\|Duel\|Parties\|Détails et statistiques)$` | oui (45303) | — |
| UI-191 | index.html:1974 | Barre mobile, égalité | Égalité : {liste} | oui (3205) | — |
| UI-192 | index.html:1977 | Barre mobile, bouton | `^(Enregistré\|Je prends)$` | oui (5130) | A-22 |
| UI-193 | index.html:1990 | Aucun profil | Choisis ton rôle et tes champions dans les réglages. | oui (3) | — |
| UI-194 | index.html:1991 | Rôle sans champion | Aucun champion pour le rôle {Rôle}. Ajoute-les dans les réglages, ou change de rôle. | oui (3) | — |
| UI-195 | index.html:1992 | Tous les picks exclus ou pris | Tous tes picks sont exclus ou déjà pris en face. Change tes exclusions dans les réglages. | oui (10) | — |
| UI-196 | index.html:1999-2000 | Sélecteur de rôle | `^(Rôle actif\|Rôle \?)$` | oui (9026) | — |
| UI-197 | index.html:2003 | Sélecteur de rôle (title) | `^(Top\|Jungle\|Mid\|ADC\|Support)( : \d+ champions?\| : aucun champion)( \(attribué par LoL\))?$` | oui (45115) | — |
| UI-198 | index.html:2004 | Sélecteur de rôle (aria) | `^Passer au profil (Top\|Jungle\|Mid\|ADC\|Support)$` | oui (45115) | — |
| UI-199 | index.html:2006 | Rôle attribué par le client | `^LoL t'attribue (Top\|Jungle\|Mid\|ADC\|Support)\.` | oui (1) | — |
| UI-200 | index.html:2007-2008 | Rôle attribué, actions | `^(Passer à (Top\|Jungle\|Mid\|ADC\|Support)\|Ignorer)$` | oui (9) | — |
| UI-201 | index.html:2014,2406 | Logo (title) | Ton assistant stratégique pour League of Legends | oui (9107) | — |
| UI-202 | index.html:2017-2019 | Barre des panneaux | `^(Deux panneaux\|Panneau unique\|Passer à deux panneaux\|Passer au panneau unique\|Ouvrir les réglages\|Masquer\|Masquer les panneaux)$` | non | — |
| UI-203 | index.html:2027 | Panneau sans profil | Choisir mon profil | oui (1) | — |
| UI-204 | index.html:2043 | Panneau droit (aria) | Champion conseillé | oui (890) | — |
| UI-205 | index.html:2047 | Panneau droit, étiquette du nom affiché | Recommandé | oui (2727) | B-03 |
| UI-206 | index.html:2052 | Panneau droit, alternative (title) | Comparer ces deux champions | oui (1096) | — |
| UI-207 | index.html:2054 | Panneau droit, alternative en choix serré | `^Presque à égalité` | oui (1254) | — |
| UI-208 | index.html:2058 | Panneau droit, titre caché | Pourquoi ce choix ? | oui (1473) | — |
| UI-209 | index.html:2060 | Panneau droit, égalité sans raison commune | Leurs raisons diffèrent : compare-les dans Analyse. | oui (405) | — |
| UI-210 | index.html:2060 | Panneau droit, aucune raison | Aucun argument marqué pour l'instant : ajoute des adversaires. | oui (39) | — |
| UI-211 | index.html:2062 | Panneau droit, danger (aria) | Danger principal | oui (996) | — |
| UI-212 | index.html:2064 | Panneau droit, danger | Danger : {C} | oui (996) | A-21 |
| UI-213 | index.html:2064 | Panneau droit, danger commun | Commun à ces champions. | oui (318) | — |
| UI-214 | index.html:2069 | Panneau droit, repli | `^(Plan de jeu et ban\|Ban conseillé)$` | oui (2807) | — |
| UI-215 | index.html:2071 | Panneau droit, autres bans | `^ou .+` | oui (1285) | — |
| UI-216 | index.html:2072 | Panneau droit, raison du ban | `^.+ (mauvais bilan de\|gêne) .+\.$` | oui (1334) | — |
| UI-217 | index.html:2074 | Panneau droit, plan sans champion | Choisis un champion pour voir son plan. | oui (583) | — |
| UI-218 | index.html:2078 | Bouton vers l'analyse | `^(Analyse\|Analyse détaillée\|détaillée)$` | oui (10495) | — |
| UI-219 | index.html:2088 | Lecture du client : connectée | Connecté au client LoL | oui (1) | — |
| UI-220 | index.html:2089 | Lecture du client : attente | En attente : lance le client LoL pour vérifier | oui (1) | — |
| UI-221 | index.html:2090 | Lecture du client : échec | Client ouvert, mais connexion impossible | oui (1) | — |
| UI-222 | index.html:2091 | Lecture du client : désactivée | Désactivée : la détection est estimée et peu fiable | oui (6) | — |
| UI-223 | index.html:2122 | Export des profils | Profils exportés : rôles, champions et préférences, sans historique ni clé. | non | — |
| UI-224 | index.html:2130 | Import refusé | `Rien n'a été modifié\.$` | oui (9) | — |
| UI-225 | index.html:2140 | Import réussi | Profils importés.{x}L'état précédent est sauvegardé. | oui (1) | — |
| UI-226 | index.html:2142 | Import illisible | Fichier illisible : rien n'a été modifié. | non | — |
| UI-227 | index.html:2148 | Annulation de l'import | Profils précédents restaurés. | oui (3) | — |
| UI-228 | index.html:2151-2153 | Boutons des profils | `^(Exporter mes profils\|Importer\|Importer des profils\|Annuler le dernier import)$` | oui (79) | — |
| UI-229 | index.html:2170 | Réglages, titre | Mes profils | oui (26) | — |
| UI-230 | index.html:2172 | Réglages, sans profil | Choisis ton rôle et tes champions pour obtenir un conseil. | oui (1) | — |
| UI-231 | index.html:2173,2176 | Réglages (aria) | `^(Mon rôle\|Mes champions)$` | oui (52) | — |
| UI-232 | index.html:2178 | Réglages, pool vide | Aucun champion pour l'instant. | oui (3) | — |
| UI-233 | index.html:2180 | Réglages, recherche de champion | `^(Ajouter un champion\|Ajouter un champion : ahri, tf, kata\.\.\.)$` | oui (78) | — |
| UI-234 | index.html:2184 | Réglages, recherche sans résultat | Aucun champion trouvé. | oui (1) | — |
| UI-235 | index.html:2186 | Réglages, rôle sans champion | Aucun champion pour ce rôle : ajoute-en pour obtenir un conseil. | oui (2) | — |
| UI-236 | index.html:2187 | Réglages, pools | Chaque rôle garde son propre pool. Ils sont enregistrés automatiquement. | oui (26) | — |
| UI-237 | index.html:2189 | Réglages, limite | Sans statistiques calculées, le conseil repose sur des fiches écrites à la main : c'est un avis général. | oui (26) | — |
| UI-238 | index.html:2203 | Réglages, sources | `^Sources de données` | oui (26) | A-18 |
| UI-239 | index.html:2205 | Réglages, sources, état | `^(Ancienne\|Absente)$` | oui (100) | — |
| UI-240 | index.html:2206 | Réglages, sources, détail | `(Patch : \|Observations : \|Incertitude (élevée\|moyenne)\.)` | oui (108) | — |
| UI-241 | index.html:2209-2210 | Réglages, rôles à surveiller | `(Rôles à surveiller\|plus souvent (Top\|Jungle\|Mid\|ADC\|Support) que dans)` | non | — |
| UI-242 | index.html:2226 | Réglages, import du journal | {N} partie(s) ajoutée(s) à ton journal. | non | A-18 |
| UI-243 | index.html:2228 | Réglages, journal invalide | Ce fichier n'est pas un journal DRAFT : rien n'a été modifié. | oui (2) | — |
| UI-244 | index.html:2238,2240 | Réglages, titre | `^(Réglages\|Fermer\|Réglages Fermer)$` | oui (7627) | — |
| UI-245 | index.html:2239 | Réglages, en-tête | bêta : ton assistant stratégique pour League of Legends. Non approuvé par Riot Games. | oui (26) | — |
| UI-246 | index.html:2244-2247 | Réglages, mise à jour | `^(Mise à jour\|Mise à jour en cours\.\.\.\|Rechercher une mise à jour\|Version .+\. DRAFT ne contacte GitHub que lorsque tu cliques\.)$` | oui (27) | — |
| UI-247 | index.html:2250-2254 | Réglages, historique | `(Mon historique Riot\|Rien d'importé pour l'instant\.\|Sert à afficher ton expérience, jamais à changer le classement\. Tu saisis ta clé toi-même : elle n'est pas enregistrée\.\|Arrêter l'import\|Importer plus de parties\|Importer mon historique Riot)` | oui (27) | — |
| UI-248 | index.html:2258-2260 | Réglages, détection | `^(Détection de la draft\|Lire la draft depuis le client LoL)$` | oui (18) | — |
| UI-249 | index.html:2261 | Réglages, lecture du client | Recommandé : les panneaux s'ouvrent au bon moment et les adversaires se remplissent seuls. Lecture seule, rien n'est modifié dans le client. | oui (9) | — |
| UI-250 | index.html:2263 | Réglages, avertissement Riot | Riot n'a pas confirmé que c'est autorisé : tu l'actives à tes risques. | oui (9) | — |
| UI-251 | index.html:2264-2266 | Réglages, rôle attribué | `^(Quand LoL t'attribue un autre rôle\|Me proposer de changer\|Changer de profil\|Ne rien faire)$` | oui (36) | — |
| UI-252 | index.html:2268 | Réglages, rôle attribué | Seulement si la lecture du client est active et que LoL indique ton poste. Rien n'est déduit de ton champion. | oui (9) | — |
| UI-253 | index.html:2270-2276 | Réglages, fenêtres | `^(Afficher les panneaux automatiquement pendant la draft\|Fenêtres\|Coller les panneaux au client LoL\|Un seul panneau\|Toujours au premier plan\|Démarrer avec Windows)$` | oui (54) | — |
| UI-254 | index.html:2271 | Réglages, raccourci | affiche ou masque les panneaux à tout moment. | oui (9) | — |
| UI-255 | index.html:2278-2279 | Réglages, picks | `^(Mes picks\|Exclure pour l'instant :)$` | oui (52) | — |
| UI-256 | index.html:2281 | Réglages, favori | À égalité exacte, préférer | oui (26) | A-26 |
| UI-257 | index.html:2287 | Réglages, frontline allié | Supposer un frontline dans mon équipe | oui (26) | A-12, A-26 |
| UI-258 | index.html:2290-2291 | Réglages, rôles | `(Rôles des champions\|Ils servent à deviner le mid adverse\.)` | oui (18) | B-21 |
| UI-259 | index.html:2291 | Réglages, rôles non calculés | Pas encore calculés : les rôles habituels écrits à la main servent de repère. | oui (7) | — |
| UI-260 | index.html:2292-2293 | Réglages, matchups | `(^Matchups$\|Sous \d+ parties, le matchup n'utilise pas le bilan\.\|relance le calcul pour ton rôle actuel\|Pas encore de bilans calculés\.)` | oui (18) | — |
| UI-261 | index.html:2297-2299 | Réglages, boutons Riot | `^(Arrêter le calcul\|Mettre à jour les rôles\|Mettre à jour les matchups)$` | oui (326) | — |
| UI-262 | index.html:2302-2304 | Réglages, journal | `^(Journal\|Pour récupérer un ancien journal, importe son fichier ici\. Rien n'est supprimé\.\|Importer un journal)$` | oui (78) | — |
| UI-263 | index.html:2308-2310 | Réglages, avancé | `^(Avancé\|Journal de diagnostic de la détection \(chiffres seulement\))$` | oui (2376) | — |
| UI-264 | index.html:2408-2412 | Barre du haut | `^(Patch \d+\.\d+\|Draft aléatoire\|Nouvelle draft)$` | oui (22647) | — |
| UI-265 | index.html:788,1303,2000,2174 | Noms des rôles (lignes adverses, sélecteur, réglages) | `^(Top\|Jungle\|Mid\|ADC\|Support)( \d+)?$` | oui (70437) | — |
| UI-266 | index.html:2014,2406 | Marque | DRAFT | oui (9107) | — |
| UI-267 | index.html:1835 | Barre de victoires (aria) | `^.+ : \d+V \d+D$` | oui (67) | — |
| UI-269 | index.html:1866,1869 | Statistiques : nom du compte importé (donnée) | `^[^ ]+#[^ ]+$` | oui (3) | — |
| UI-268 | index.html:2311 | Réglages avancés : phase et placement transmis par le programme (src/DraftDetection.cs PhaseText) | `^(Client LoL fermé\|Client ouvert, hors draft \(estimation\)\|Draft en cours \(estimation\)\|Partie en cours\|Phase incertaine)\.` | oui (7) | — |

### web/draft-engine.js (109 entrées, 108 vues à l'écran)

| ID | Emplacement | Contexte et calcul | Texte ou gabarit | Vu à l'écran | Classement |
|---|---|---|---|---|---|
| EN-001 | draft-engine.js:122-130 | Libellés des critères (matrice, comparer, règles, conseils) | `^(Matchup direct\|Sécurité sans adversaire connu\|Cibles exploitables\|Tanks et bruisers\|Survie aux plongeurs\|Engages et contrôles\|Poke et longue portée\|Protection nécessaire\|Pression sur la carte)$` | oui (7047) | A-01 |
| EN-002 | draft-engine.js:122 | Règles, source du critère matchup | Bilans calculés sur de vraies parties dès 30 parties, sinon tableau écrit à la main. Inclut la pression de lane, pour ne pas la compter deux fois. | oui (369) | — |
| EN-003 | draft-engine.js:123 | Règles, source blind | Calculé : part des mids du tableau où le pick est à éviter. Ne compte que si le mid est inconnu. | oui (369) | — |
| EN-004 | draft-engine.js:124 | Règles, source targets | Règle heuristique : nombre de carrys fragiles adverses. | oui (369) | A-17 |
| EN-005 | draft-engine.js:125 | Règles, source front | Règle heuristique : nombre de frontlines adverses. | oui (369) | A-17 |
| EN-006 | draft-engine.js:126 | Règles, source dive | Règle heuristique : nombre d'assassins et de plongeurs adverses. | oui (369) | A-17 |
| EN-007 | draft-engine.js:127 | Règles, source control | Règle heuristique : contrôles ciblés et engages adverses, comptés une seule fois par pick. | oui (369) | A-17 |
| EN-008 | draft-engine.js:128 | Règles, source range | Règle heuristique : nombre de champions à longue portée adverses. | oui (369) | A-17 |
| EN-009 | draft-engine.js:129 | Règles, source peel | Règle heuristique : frontline allié supposé, mobilité adverse. | oui (369) | A-12, A-17 |
| EN-010 | draft-engine.js:130 | Règles, source map | Atout propre au champion. Poids faible, car il ne dépend pas de la draft. | oui (369) | — |
| EN-011 | draft-engine.js:201 | Raison matchup, bilan calculé | `(% contre .+ sur \d+ parties\|Sans écart net contre .+ sur \d+ parties)` | oui (83) | — |
| EN-012 | draft-engine.js:205 | Raison matchup, tableau | À éviter contre {C} | oui (437) | B-08 |
| EN-013 | draft-engine.js:206 | Raison matchup, tableau | 1er choix contre {C} | oui (984) | — |
| EN-014 | draft-engine.js:207 | Raison matchup, tableau | 2e choix contre {C} | oui (541) | — |
| EN-015 | draft-engine.js:208 | Raison matchup, tableau (note 0, seulement en matrice) | Pas d'avis marqué contre {C} | oui (222) | — |
| EN-016 | draft-engine.js:216 | Raison blind, positive | Mid inconnu : peu de mauvais matchups ({N} sur {N} dans le tableau) | oui (483) | — |
| EN-017 | draft-engine.js:216 | Raison blind, négative | Mid inconnu : risqué, mauvais dans {N} matchups sur {N} du tableau | oui (740) | — |
| EN-018 | draft-engine.js:221 | Raison targets h | `\d cibles fragiles :` | oui (1880) | A-06 |
| EN-019 | draft-engine.js:222 | Raison targets h | Une cible fragile : {C} | oui (1300) | — |
| EN-020 | draft-engine.js:223 | Raison targets h, aucune | Aucune cible fragile à éliminer | oui (34) | — |
| EN-021 | draft-engine.js:225 | Raison targets E | cibles fragiles à attraper : {liste} | oui (2827) | A-06 |
| EN-022 | draft-engine.js:226 | Raison targets l | cibles à abîmer de loin : {liste} | oui (1059) | A-06 |
| EN-023 | draft-engine.js:232 | Raison front k, 3+ | tanks ou bruisers : {x} font la différence | oui (1145) | — |
| EN-024 | draft-engine.js:233 | Raison front k, 2 | 2 tanks ou bruisers : {x} sont utiles | oui (2399) | — |
| EN-025 | draft-engine.js:236 | Raison front h+d, 4+ | `\d frontlines à traverser` | oui (52) | — |
| EN-026 | draft-engine.js:237 | Raison front h+d, 3 | Beaucoup de frontline à traverser | oui (291) | — |
| EN-027 | draft-engine.js:239 | Raison front w | frontlines : peu de prise sur les tanks | oui (192) | — |
| EN-028 | draft-engine.js:245 | Raison dive i | plongeurs contre un champion immobile : {liste} | oui (635) | A-05 |
| EN-029 | draft-engine.js:246 | Raison dive t | Encaisse leurs {N} plongeurs | oui (634) | A-05 |
| EN-030 | draft-engine.js:247 | Raison dive c | Peut protéger ses alliés de leurs {N} plongeurs | oui (373) | A-05 |
| EN-031 | draft-engine.js:248 | Raison dive, note 0 (matrice) | `^(Son E lui laisse une sortie contre les plongeurs\|Son étourdissement l'aide à se défendre contre les plongeurs\|Peut survivre aux plongeurs)$` | oui (300) | — |
| EN-032 | draft-engine.js:251 | Raison control h+d | contrôles ciblés punissent ses plongeons : {liste} | oui (120) | A-05 |
| EN-033 | draft-engine.js:252 | Raison control e | `Peut jouer loin de leur engage (et revenir avec son ultime\|et de leurs contrôles)` | oui (871) | A-08 |
| EN-034 | draft-engine.js:253 | Raison control i | engages contre un champion lent : {liste} | oui (215) | — |
| EN-035 | draft-engine.js:258 | Raison range l | Peut rendre les coups à distance : {liste} | oui (289) | A-06 |
| EN-036 | draft-engine.js:259 | Raison range o | Se fait toucher avant de pouvoir frapper : {liste} | oui (38) | A-06 |
| EN-037 | draft-engine.js:263 | Raison peel p, note 0 (matrice) | Suppose un frontline allié pour le protéger | oui (425) | A-12 |
| EN-038 | draft-engine.js:263 | Raison peel p, sans frontline | Ton équipe n'a pas de frontline pour le protéger | oui (338) | A-07 |
| EN-039 | draft-engine.js:265 | Raison peel q, sans frontline | Personne pour te protéger de leur mobilité | oui (45) | A-07 |
| EN-040 | draft-engine.js:266 | Raison peel q | champions très mobiles : {liste} | oui (37) | — |
| EN-041 | draft-engine.js:267 | Raison peel q, positive | Peu de plongeurs et de mobilité, et un frontline devant toi | oui (94) | A-07, A-12 |
| EN-042 | draft-engine.js:271 | Raison map g | `(Son ultime couvre toute la carte\|Peut peser sur toute la carte)` | oui (3070) | A-08 |
| EN-043 | draft-engine.js:272 | Raison map r | Peut roamer une fois sa vague poussée | oui (2446) | A-08 |
| EN-044 | draft-engine.js:273 | Raison map n | Peu d'impact hors des combats groupés | oui (664) | A-08 |
| EN-045 | draft-engine.js:299-300 | Raison mélangée de plusieurs hypothèses (evaluateScenarios) | ` ·` | oui (439) | B-08 |
| EN-046 | draft-engine.js:420 | Raison du ban, bilans | mauvais bilan de {liste} sur de vraies parties | oui (12) | — |
| EN-047 | draft-engine.js:423 | Raison du ban, tableau | gêne {liste} dans le tableau de matchups | oui (897) | — |
| EN-048 | draft-engine.js:425 | Raison du ban, fiches | gêne {liste} d'après leurs profils | oui (4279) | — |
| EN-049 | draft-engine.js:439-442 | Style des picks du tableau (Comparer) | `^(Assassin de mêlée : entre, tue une cible et ressort\.\|Mage de carte : pousse, contrôle une cible et rejoint les autres lanes\.\|Artillerie immobile : fait de gros dégâts s'il est protégé\.\|Artillerie à très longue portée : affaiblit avant le combat\.)$` | oui (106) | A-23 |
| EN-050 | draft-engine.js:447-454 | Style d'un pick (Comparer) | `^(Protecteur : garde ses alliés en vie\.\|Tank d'engage : lance les combats et encaisse\.\|Tank : encaisse en première ligne\.\|Assassin : cherche les cibles fragiles\.\|Tueur de tanks : fait fondre la première ligne\.\|Combattant : plonge dans le combat\.\|Longue portée : affaiblit avant le combat\.\|Polyvalent : s'adapte au déroulé de la partie\.)$` | oui (622) | A-23 |
| EN-051 | draft-engine.js:483 | whenBetter, sans frontline | si ton équipe n'a pas de frontline | oui (30) | B-10 |
| EN-052 | draft-engine.js:484 | whenBetter, autre adversaire | `si ton adversaire (au top\|à la jungle\|au mid\|à l'ADC\|au support) est` | oui (14) | B-10 |
| EN-053 | draft-engine.js:470,485 | whenBetter, adversaire type ajouté | `avec un (tank\|plongeur\|contrôle ciblé\|carry fragile\|poke) de plus` | oui (43) | B-10 |
| EN-054 | draft-engine.js:508 | Analyse, position | Seul pick disponible. | oui (20) | — |
| EN-055 | draft-engine.js:510 | Analyse, position 1er | 1er : passe devant {C} surtout grâce au critère « {critère} ». | oui (977) | — |
| EN-056 | draft-engine.js:510 | Analyse, position 1er à égalité | 1er, à égalité avec {C}. | oui (582) | — |
| EN-057 | draft-engine.js:513 | Analyse, position | Derrière {C} surtout à cause du critère « {critère} ». | oui (2717) | — |
| EN-058 | draft-engine.js:513 | Analyse, position à égalité | `^À égalité avec .+\.$` | oui (1016) | — |
| EN-059 | draft-engine.js:519-527 | Mots de compoReading (Leurs forces / failles) | `(frontline\|engage\|contrôles ciblés\|plongeurs\|poke\|plusieurs cibles fragiles\|pas de frontline\|peu d'engage\|aucun contrôle ciblé)` | oui (19775) | A-04 |
| EN-060 | draft-engine.js:542 | Danger (panneau droit) | Son contrôle ciblé peut stopper ton plongeon. | oui (151) | — |
| EN-061 | draft-engine.js:543 | Danger | Peut te rattraper quand tu ressors d'un combat. | oui (83) | — |
| EN-062 | draft-engine.js:543 | Danger | Peut te sauter dessus si tu es mal placé. | oui (289) | — |
| EN-063 | draft-engine.js:544 | Danger | Sa mobilité lui permet d'éviter tes sorts à distance. | oui (10) | — |
| EN-064 | draft-engine.js:545 | Danger | Peut démarrer le combat sur toi. | oui (145) | — |
| EN-065 | draft-engine.js:546 | Danger | À surveiller pendant la partie. | non | — |
| EN-066 | draft-engine.js:552 | Condition de victoire c | Protège ton meilleur carry, et fais échouer les engages sur lui. | oui (1300) | — |
| EN-067 | draft-engine.js:553 | Condition de victoire E | Lance le combat sur {C} dès que ton équipe peut suivre. | oui (2223) | A-24 |
| EN-068 | draft-engine.js:554 | Condition de victoire t | Encaisse en première ligne, et prends l'espace autour des objectifs. | oui (252) | A-24 |
| EN-069 | draft-engine.js:555 | Condition de victoire h | Prends ton avantage, puis élimine {C} avant l'objectif. | oui (1470) | — |
| EN-070 | draft-engine.js:556 | Condition de victoire k | `(Fais fondre .+, et reste en vie jusqu'à la fin du combat\.\|Frappe la cible la plus proche, et reste en vie jusqu'à la fin du combat\.)` | oui (2189) | — |
| EN-071 | draft-engine.js:557,568 | Condition de victoire l / Xerath | `Affaiblis-les à distance avant l'objectif, (sans te faire attraper\|loin de leurs plongeurs)\.` | oui (1004) | — |
| EN-072 | draft-engine.js:558 | Condition de victoire par défaut | Prends ton avantage en lane, puis joue chaque objectif avec ton équipe. | oui (1046) | — |
| EN-073 | draft-engine.js:565 | Condition de victoire Fizz | Pousse ta vague, roam, et tue {C} avant l'objectif. | oui (675) | — |
| EN-074 | draft-engine.js:566 | Condition de victoire TF | Pousse à la carte bleue, puis crée un surnombre avec ton ultime. | oui (755) | A-08 |
| EN-075 | draft-engine.js:567 | Condition de victoire Kog'Maw | `Reste derrière ton frontline et (fais fondre .+\|frappe) autour des objectifs\.` | oui (418) | A-12 |
| EN-076 | draft-engine.js:587 | Plan lane, adversaire plongeur | peut te sauter dessus : garde tes distances et ton sort de défense. | oui (92) | — |
| EN-077 | draft-engine.js:588 | Plan lane, adversaire à contrôle | a un contrôle ciblé : attends de le voir partir avant d'engager. | oui (48) | — |
| EN-078 | draft-engine.js:589 | Plan lane | Contre {C}, observe ses sorts importants avant d'engager un échange. | oui (343) | — |
| EN-079 | draft-engine.js:590 | Plan lane, matchup difficile | est un matchup difficile : farme prudemment et n'échange qu'après avoir vu partir un de ses sorts importants. | oui (26) | — |
| EN-080 | draft-engine.js:591 | Plan lane Fizz | a un contrôle ciblé : attends de le voir partir avant de lancer ton all-in. | oui (13) | — |
| EN-081 | draft-engine.js:592 | Plan lane Fizz | Contre {C}, cherche des échanges courts, puis ressors avec ton E. | oui (55) | — |
| EN-082 | draft-engine.js:593 | Plan lane, pick immobile | peut te sauter dessus : garde tes distances et ne gaspille pas ton sort de contrôle ou de ralentissement. | oui (23) | — |
| EN-083 | draft-engine.js:594 | Plan lane Xerath | Touche {C} de loin dès qu'il vient au farm, sans dépasser la moitié de la lane. | oui (3) | — |
| EN-084 | draft-engine.js:595 | Plan lane Kog'Maw | Contre {C}, farme à portée maximale et échange seulement quand ta vague te couvre. | oui (15) | — |
| EN-085 | draft-engine.js:596 | Plan lane TF | Contre {C}, garde ta carte dorée pour répondre à un engage ou préparer un gank. | oui (52) | — |
| EN-086 | draft-engine.js:597 | Plan lane, jungler | peut venir en gank : place une ward avant de pousser ta vague. | oui (459) | — |
| EN-087 | draft-engine.js:598 | Plan lane, adversaire inconnu | `^Ton adversaire (au top\|à la jungle\|au mid\|à l'ADC\|au support) n'est pas connu : joue d'abord la sécurité et observe son style\.$` | oui (409) | A-10 |
| EN-088 | draft-engine.js:599 | Plan lane jungle | Prépare ton chemin vers la lane qui peut le mieux profiter de ton aide. | oui (166) | — |
| EN-089 | draft-engine.js:599 | Plan lane support | Garde la vision de ton côté avant de chercher un engage. | oui (167) | — |
| EN-090 | draft-engine.js:599 | Plan lane top, mid, ADC | Pousse ta vague avant de bouger, pour ne pas perdre de farm. | oui (693) | — |
| EN-091 | draft-engine.js:602 | Plan mid game TF | Utilise ton ultime dès qu'une lane peut punir {C}. | oui (15) | A-08 |
| EN-092 | draft-engine.js:603 | Plan mid game Fizz | Cherche {C} isolé sur un côté, puis disparais avant la riposte. | oui (22) | — |
| EN-093 | draft-engine.js:604 | Plan mid game Kog'Maw | Reste groupé avec ton frontline : seul sur un côté, tu n'as presque aucune échappatoire. | oui (11) | A-12 |
| EN-094 | draft-engine.js:605 | Plan mid game Xerath | Affaiblis {C} avant chaque objectif, depuis un endroit couvert par une ward. | oui (3) | — |
| EN-095 | draft-engine.js:606 | Plan mid game, plongeur | Repère {C} sur la carte avant de te placer : sans visuel, recule. | oui (155) | A-24 |
| EN-096 | draft-engine.js:607 | Plan mid game, engage | Pose une ward de contrôle sur le chemin de {C} avant les objectifs. | oui (204) | A-24 |
| EN-097 | draft-engine.js:608 | Plan mid game, tank | Ne perds pas de temps sur {C} si une tour ou un objectif est disponible. | oui (210) | — |
| EN-098 | draft-engine.js:609 | Plan mid game, défaut | Après un combat gagné, prends tout de suite une tour ou un objectif. | oui (129) | — |
| EN-099 | draft-engine.js:612 | Plan teamfight Fizz | Entre seulement après avoir vu partir le contrôle de {C}. | oui (11) | — |
| EN-100 | draft-engine.js:613 | Plan teamfight Fizz | Ta cible prioritaire est {C}, pas le frontline. | oui (22) | A-12 |
| EN-101 | draft-engine.js:614 | Plan teamfight Fizz | Sans cible fragile accessible, ne plonge pas : va chercher un objectif ailleurs. | oui (2) | — |
| EN-102 | draft-engine.js:615 | Plan teamfight TF | Garde ta carte dorée pour {C} ou pour le premier qui plonge. | oui (15) | — |
| EN-103 | draft-engine.js:616 | Plan teamfight TF | Temporise tant que {C} n'a pas engagé, puis rejoins le combat. | oui (16) | — |
| EN-104 | draft-engine.js:617 | Plan teamfight, pick immobile | Garde-toi une sortie contre {C} : ne te place jamais en première ligne. | oui (23) | — |
| EN-105 | draft-engine.js:618 | Plan teamfight Kog'Maw | Frappe {C} s'il est le plus proche : ne traverse pas le combat pour une cible lointaine. | oui (11) | — |
| EN-106 | draft-engine.js:619 | Plan teamfight Xerath | Lance tes sorts sur {C} depuis l'arrière, et recule dès qu'un adversaire s'approche. | oui (3) | — |
| EN-107 | draft-engine.js:620 | Plan teamfight, engage | Évite les combats sans vision : {C} peut démarrer l'engage sur toi. | oui (33) | — |
| EN-108 | draft-engine.js:621 | Plan teamfight, défaut | Évite d'ouvrir un combat quand plusieurs adversaires sont invisibles sur la carte. | oui (257) | A-24 |
| EN-109 | draft-engine.js:2-44 | Types de mid du tableau (Statistiques, Journal) | `^(Mage\|Assassin\|Combattant\|Tank\|Support)$` | oui (13807) | — |

### web/draft-coaching.js (168 entrées, 159 vues à l'écran)

| ID | Emplacement | Contexte et calcul | Texte ou gabarit | Vu à l'écran | Classement |
|---|---|---|---|---|---|
| CO-001 | draft-coaching.js:9-16 | Provenance d'un conseil (AdviceItem) | `^(Donnée officielle de Riot\|Mesuré sur de vraies parties\|Mesuré sur peu de parties\|Estimation\|Règle écrite à la main\|Principe général du jeu)(\. .+)?$` | oui (16835) | — |
| CO-002 | draft-coaching.js:125 | draft.tie, titre | Égalité : {liste} | oui (3205) | — |
| CO-003 | draft-coaching.js:126 | draft.tie, débutant | Le classement ne les départage pas. Choisis celui que tu connais le mieux. | oui (3368) | — |
| CO-004 | draft-coaching.js:127 | draft.tie, intermédiaire, fiches identiques | `^Leurs fiches sont identiques : l'outil ne peut pas les distinguer dans cette draft\.` | oui (70) | — |
| CO-005 | draft-coaching.js:128 | draft.tie, intermédiaire | `^Même score pour des raisons différentes\.` | oui (140) | — |
| CO-006 | draft-coaching.js:129 | draft.tie, avancé (alphabet ou favori) | `(^Score identique \(-?\d+,\d\)\.\|ne vient que de l'ordre alphabétique, ou de ton favori)` | oui (210) | B-04 |
| CO-007 | draft-coaching.js:130 | draft.tie, limite | Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu. | oui (801) | — |
| CO-008 | draft-coaching.js:138 | draft.why, titre | `^Pourquoi .+[^?]$` | oui (1066) | — |
| CO-009 | draft-coaching.js:139 | draft.why, débutant | La raison principale : {raison}. | oui (289) | — |
| CO-010 | draft-coaching.js:140 | draft.why, intermédiaire | `^« .+ » pèse le plus :` | oui (102) | — |
| CO-011 | draft-coaching.js:141 | draft.why, avancé | Critère le plus lourd : « {critère} » (+{N}) | oui (102) | — |
| CO-012 | draft-coaching.js:149 | draft.close, titre | {C} ou {C} : le choix est serré | oui (4676) | — |
| CO-013 | draft-coaching.js:150 | draft.close, débutant (« X passe devant ») | `^Les deux peuvent convenir\.` | oui (4058) | B-01, A-09 |
| CO-014 | draft-coaching.js:151 | draft.close, intermédiaire | ` mène surtout sur «` | oui (301) | B-01 |
| CO-015 | draft-coaching.js:152 | draft.close, avancé (écart chiffré) | `^Écart de -?\d+,\d point : sous le seuil` | oui (309) | B-01 |
| CO-016 | draft-coaching.js:160 | draft.conflict, titre | {C} ressort malgré un vrai risque | oui (90) | — |
| CO-017 | draft-coaching.js:161 | draft.conflict, débutant | Le risque : {raison}. {C} reste devant car le reste de la draft compense, par rapport à tes autres champions. | oui (7) | B-01 |
| CO-018 | draft-coaching.js:162 | draft.conflict, intermédiaire | Le score reste le meilleur, mais « {critère} » pèse contre ce choix : {raison}. Vérifie que tu peux jouer avec cette contrainte. | oui (5) | B-01 |
| CO-019 | draft-coaching.js:163 | draft.conflict, avancé | `^Critère négatif « .+ » \(-?\d+,\d\), compensé par les autres\.` | oui (10) | — |
| CO-020 | draft-coaching.js:167 | draft.incomplete, titre | `^Il reste \d adversaires? à voir$` | oui (3014) | — |
| CO-021 | draft-coaching.js:168 | draft.incomplete, débutant | Ce conseil peut changer. Attends si tu peux, avant de te décider. | oui (1309) | — |
| CO-022 | draft-coaching.js:169 | draft.incomplete, intermédiaire | seulement : les comptes de frontline et de contrôles peuvent encore bouger. | oui (99) | — |
| CO-023 | draft-coaching.js:170 | draft.incomplete, avancé | Les critères à seuil ne se déclenchent pas encore tous | oui (99) | — |
| CO-024 | draft-coaching.js:177 | matchup.measured, titre | {C} contre {C} : bilan mesuré | oui (45) | — |
| CO-025 | draft-coaching.js:178 | matchup.measured, débutant | `^Sur \d+ parties de joueurs habitués à .+, ce matchup se passe plutôt (mieux\|moins bien) que leur moyenne\.$` | oui (33) | — |
| CO-026 | draft-coaching.js:179 | matchup.measured, intermédiaire | points de victoires par rapport à leur moyenne sur {C}, sur {N} parties. | oui (6) | — |
| CO-027 | draft-coaching.js:180 | matchup.measured, avancé | Écart ramené vers zéro selon l'échantillon | oui (6) | — |
| CO-028 | draft-coaching.js:181 | matchup.measured, limites | `(Peu de parties : à prendre comme une tendance\.\|Ces joueurs jouent souvent ce champion : pas forcément ton cas\.)` | oui (9) | — |
| CO-029 | draft-coaching.js:190 | matchup.written, titre | `^.+ contre .+ : (plutôt un bon choix\|un matchup difficile)$` | oui (179) | — |
| CO-030 | draft-coaching.js:191 | matchup.written, débutant, bon | Le tableau de DRAFT place {C} parmi les bons choix contre {C}. | oui (83) | — |
| CO-031 | draft-coaching.js:191 | matchup.written, débutant, difficile | Le tableau de DRAFT déconseille {C} contre {C}. Si ce choix reste devant, c'est pour le reste de la draft. | oui (24) | — |
| CO-032 | draft-coaching.js:192 | matchup.written, intermédiaire | , selon un tableau écrit à la main. Ce critère est le plus lourd du classement. | oui (36) | B-22 |
| CO-033 | draft-coaching.js:193 | matchup.written, avancé | avis écrit à la main ({N} points), sans statistiques ni patch. Un bilan calculé le remplacera dès {N} parties utiles. | oui (36) | — |
| CO-034 | draft-coaching.js:194 | matchup.written, limite | Avis écrit à la main, pas une statistique. | oui (179) | — |
| CO-035 | draft-coaching.js:197 | matchup.range, titres | `^(.+ te touche de plus loin\|Tu touches .+ de plus loin)$` | oui (226) | — |
| CO-036 | draft-coaching.js:200 | matchup.range, débutant, plus court | Pour farmer, tu dois t'approcher et risques d'être touché. Avance quand ses sorts viennent d'être utilisés. | oui (90) | — |
| CO-037 | draft-coaching.js:201 | matchup.range, intermédiaire, plus court | Choisis tes échanges : engage juste après un sort important de {C}, puis ressors. | oui (31) | — |
| CO-038 | draft-coaching.js:202 | matchup.range, avancé, plus court | Il pèse surtout tôt dans la partie, et ne dit rien de la portée des sorts ni des outils pour le combler. | oui (31) | — |
| CO-039 | draft-coaching.js:204 | matchup.range, débutant, plus long | Ses attaques de base ne t'atteignent pas de loin, mais ses sorts peuvent changer ça : regarde-les avant d'avancer. | oui (50) | — |
| CO-040 | draft-coaching.js:205 | matchup.range, intermédiaire, plus long | doit s'approcher pour farmer, ce qui t'offre des occasions de frapper. Garde un œil sur ses sorts. | oui (12) | — |
| CO-041 | draft-coaching.js:206 | matchup.range, avancé, plus long | Il ne compte que si {C} n'a pas de quoi le combler : à vérifier dans ses sorts. | oui (12) | — |
| CO-042 | draft-coaching.js:207 | matchup.range, limite | Portée d'attaque de base seulement : les sorts ne sont pas comptés. | oui (226) | — |
| CO-043 | draft-coaching.js:210 | matchup.tank, titre | {C} encaisse beaucoup | oui (226) | — |
| CO-044 | draft-coaching.js:211 | matchup.tank, tueur de tanks, débutant | fait de bons dégâts aux joueurs solides. Tuer {C} prend du temps : n'en fais pas ta priorité tout de suite. | oui (32) | — |
| CO-045 | draft-coaching.js:212 | matchup.tank, tueur de tanks, intermédiaire | Un tank en face tue rarement vite : joue la durée. | oui (16) | — |
| CO-046 | draft-coaching.js:213 | matchup.tank, tueur de tanks, avancé | l'intérêt de {C} grandit avec le temps. C'est une règle générale de sa fiche, pas un bilan mesuré de ce matchup. | oui (16) | — |
| CO-047 | draft-coaching.js:214 | matchup.tank, jungle/support, débutant | Ne t'acharne pas : regarde s'il y a une cible plus fragile à ta portée. | oui (70) | — |
| CO-048 | draft-coaching.js:215 | matchup.tank, jungle/support, intermédiaire | Tuer un tank est long : garde tes sorts pour une cible plus fragile, sauf si tes alliés se concentrent sur lui. | oui (25) | — |
| CO-049 | draft-coaching.js:216 | matchup.tank, jungle/support, avancé | Contre un tank, ton impact passe souvent par les cibles fragiles et le timing. À nuancer selon ce que ton équipe peut tuer. | oui (25) | — |
| CO-050 | draft-coaching.js:217 | matchup.tank, voie, débutant | Ne t'acharne pas : va plutôt aider ailleurs quand ta vague est poussée. | oui (28) | A-14 |
| CO-051 | draft-coaching.js:218 | matchup.tank, voie, intermédiaire | Tuer un tank en voie est long et rarement rentable tôt : pousse ta vague et regarde où ton aide pèserait davantage. | oui (7) | A-14 |
| CO-052 | draft-coaching.js:219 | matchup.tank, voie, avancé | Contre un tank, l'objectif de voie est souvent le tempo plutôt que le meurtre. À nuancer selon ta capacité à le punir. | oui (7) | A-14, A-15 |
| CO-053 | draft-coaching.js:220,227 | matchup.tank/assassin, limite | Avis général, pas un bilan de ce matchup. | oui (366) | — |
| CO-054 | draft-coaching.js:223 | matchup.assassin, titre | {C} peut te sauter dessus | oui (255) | — |
| CO-055 | draft-coaching.js:224 | matchup.assassin, débutant | cherche à te surprendre et à te tuer vite. Garde de quoi te défendre tant que ses sorts pour s'approcher sont disponibles. | oui (98) | — |
| CO-056 | draft-coaching.js:225 | matchup.assassin, intermédiaire | Sa menace dépend de sa capacité à arriver sur toi. Quand ses sorts de déplacement sont utilisés, c'est ta fenêtre pour échanger. | oui (21) | — |
| CO-057 | draft-coaching.js:226 | matchup.assassin, avancé | Menace d'accès rapide : la zone dangereuse dépend de ses sorts de déplacement, pas de sa portée d'attaque. Le niveau d'objets de chacun change vite ce rapport. | oui (21) | — |
| CO-058 | draft-coaching.js:234 | matchup.unknown, titre, candidats | `^(Top\|Jungle\|Mid\|ADC\|Support) pas encore identifié : prépare les deux cas$` | oui (163) | B-14 |
| CO-059 | draft-coaching.js:234 | matchup.unknown, titre, aucun candidat | `^(Top\|Jungle\|Mid\|ADC\|Support) pas encore visible$` | oui (622) | A-10 |
| CO-060 | draft-coaching.js:236 | matchup.unknown, débutant | Regarde les sorts d'invocateur et le placement au début de la partie. | oui (119) | B-14 |
| CO-061 | draft-coaching.js:237 | matchup.unknown, intermédiaire | Le conseil tient compte des deux. | oui (22) | B-14 |
| CO-062 | draft-coaching.js:238 | matchup.unknown, avancé | Si tu connais le vrai rôle, corrige-le : le calcul redevient exact pour ce matchup. | oui (22) | — |
| CO-063 | draft-coaching.js:240 | matchup.unknown, débutant, aucun candidat | Tant qu'il n'est pas visible, le conseil ne porte pas sur ton duel. Si tu peux, choisis après l'avoir vu. | oui (544) | — |
| CO-064 | draft-coaching.js:241 | matchup.unknown, intermédiaire, aucun candidat | Le critère de matchup ne s'applique pas encore : le conseil repose sur le reste de la composition et sur la sécurité générale du pick. | oui (39) | B-12 |
| CO-065 | draft-coaching.js:242 | matchup.unknown, avancé, aucun candidat | Sans candidat plausible, seul le critère « sans adversaire connu » intervient pour ce rôle. | oui (39) | B-12 |
| CO-066 | draft-coaching.js:243 | matchup.unknown, limite | Déduction d'après les rôles habituels : ce n'est pas une information du client. | oui (785) | — |
| CO-067 | draft-coaching.js:61,64 | Portées (coachRangeWords) | `(Les deux : \|: )(longue portée\|portée moyenne\|corps à corps\|portée inconnue)` | oui (141) | A-25 |
| CO-068 | draft-coaching.js:250 | comp.assassins, titres | `^(.+ peuvent atteindre .+\|\d adversaires sautent sur leurs cibles)$` | oui (376) | — |
| CO-069 | draft-coaching.js:251 | comp.assassins, risque, débutant | peuvent venir sur toi d'un coup, et {C} s'échappe mal. Joue près d'un allié qui peut te protéger. | oui (14) | — |
| CO-070 | draft-coaching.js:252 | comp.assassins, risque, intermédiaire | Reste dans la zone de protection de ton équipe, et repère-les avant d'avancer. | oui (7) | — |
| CO-071 | draft-coaching.js:253 | comp.assassins, risque, avancé | Il baisse avec une frontline alliée qui protège : à vérifier selon les picks de ton équipe. | oui (7) | B-13 |
| CO-072 | draft-coaching.js:254 | comp.assassins, solide, débutant | encaisse ou protège bien : place-toi près de ton joueur le plus fragile. | oui (60) | — |
| CO-073 | draft-coaching.js:255 | comp.assassins, solide, intermédiaire | ta solidité aide ton équipe : garde ton carry à portée pour répondre à leur saut. | oui (32) | — |
| CO-074 | draft-coaching.js:256 | comp.assassins, solide, avancé | profil solide ou protecteur face à {N} plongeurs. | oui (32) | — |
| CO-075 | draft-coaching.js:257 | comp.assassins, neutre, débutant | cherchent des cibles fragiles. Avant d'avancer, vérifie où ils sont sur la carte. | oui (118) | — |
| CO-076 | draft-coaching.js:258 | comp.assassins, neutre, intermédiaire | Leurs plongeurs punissent un mauvais placement : quand ils ne sont pas visibles, considère qu'ils peuvent arriver. | oui (53) | — |
| CO-077 | draft-coaching.js:259 | comp.assassins, neutre, avancé | plongeurs : l'équipe est forte sur une cible isolée, plus faible si elle doit traverser une frontline bien placée. | oui (53) | — |
| CO-078 | draft-coaching.js:264 | comp.frontline, titre | Des joueurs solides en face : {liste} | oui (385) | A-13 |
| CO-079 | draft-coaching.js:265 | comp.frontline, bonus, débutant | fait de bons dégâts à ce type de cible. Si l'un d'eux te barre la route, tes coups restent utiles. | oui (105) | — |
| CO-080 | draft-coaching.js:266 | comp.frontline, bonus, intermédiaire | joueurs très solides : le profil de {C} est valorisé ici. Reste en vie assez longtemps pour que tes dégâts comptent. | oui (44) | — |
| CO-081 | draft-coaching.js:267 | comp.frontline, bonus, avancé | Attention : il compte aussi les bruisers | oui (44) | — |
| CO-082 | draft-coaching.js:268 | comp.frontline, malus, débutant | se mettent devant leurs joueurs fragiles. Cherche à les contourner : foncer dans le tas ne marchera pas. | oui (23) | — |
| CO-083 | draft-coaching.js:269 | comp.frontline, malus, intermédiaire | Trop de frontline à traverser pour {C} : attends que leurs sorts soient utilisés, ou joue ailleurs sur la carte. | oui (13) | — |
| CO-084 | draft-coaching.js:270 | comp.frontline, malus, avancé | Le moteur compte un malus (« {critère} ») : {raison}. | oui (13) | — |
| CO-085 | draft-coaching.js:271 | comp.frontline, neutre, débutant | encaissent beaucoup. Un combat long les avantage si ton équipe manque de dégâts. | oui (85) | — |
| CO-086 | draft-coaching.js:272 | comp.frontline, neutre, intermédiaire | joueurs très solides : ils tiennent longtemps en combat groupé. Pense à leurs joueurs fragiles derrière. | oui (29) | — |
| CO-087 | draft-coaching.js:273 | comp.frontline, neutre, avancé | Composition résistante : à comparer aux dégâts soutenus de ton équipe, que l'outil ne connaît pas. | oui (29) | — |
| CO-088 | draft-coaching.js:277 | comp.no-frontline, titre | Personne pour encaisser en face | oui (62) | A-13 |
| CO-089 | draft-coaching.js:278 | comp.no-frontline, débutant | adversaires n'encaisse vraiment : si ton équipe les atteint, le combat peut basculer vite. | oui (42) | — |
| CO-090 | draft-coaching.js:279 | comp.no-frontline, intermédiaire | Sans frontline, leurs joueurs fragiles sont exposés dès que quelqu'un les atteint. À l'inverse, ils peuvent te tuer vite si tu arrives seul. | oui (10) | — |
| CO-091 | draft-coaching.js:280 | comp.no-frontline, avancé | Leur force dépend alors de tuer avant d'être atteint : le premier engagement réussi pèse lourd. | oui (10) | — |
| CO-092 | draft-coaching.js:284 | comp.control, titre | {liste} peuvent t'immobiliser | oui (139) | B-15, A-13 |
| CO-093 | draft-coaching.js:285 | comp.control, malus, débutant | Si tu fonces et qu'ils t'immobilisent, tu ne peux plus rien faire. Mieux vaut entrer après avoir vu leurs sorts partir. | oui (8) | B-15 |
| CO-094 | draft-coaching.js:286 | comp.control, malus, intermédiaire | Regarde quel sort vient de partir avant d'entrer. | oui (2) | B-15 |
| CO-095 | draft-coaching.js:287 | comp.control, malus, avancé | contrôles ciblés visibles. | oui (2) | B-15 |
| CO-096 | draft-coaching.js:288 | comp.control, bonus, débutant | peut rester loin de leurs contrôles, puis revenir au bon moment. Ne te place pas devant ton équipe. | oui (12) | — |
| CO-097 | draft-coaching.js:289 | comp.control, bonus, intermédiaire | Garde cette distance : c'est ce qui rend ce pick intéressant ici. | oui (7) | — |
| CO-098 | draft-coaching.js:290 | comp.control, bonus, avancé | profil capable de jouer loin de l'engage et des contrôles. | oui (7) | — |
| CO-099 | draft-coaching.js:291 | comp.control, neutre, débutant | Un seul de leurs sorts peut t'empêcher d'agir : évite d'être le premier à portée quand tu ne sais pas où ils sont. | oui (57) | — |
| CO-100 | draft-coaching.js:292 | comp.control, neutre, intermédiaire | Plusieurs contrôles ciblés : joue autour du moment où ils sont utilisés. | oui (22) | — |
| CO-101 | draft-coaching.js:293 | comp.control, neutre, avancé | contrôles ciblés : à lire avec les délais de récupération, que l'outil ne connaît pas. | oui (22) | — |
| CO-102 | draft-coaching.js:297 | comp.engage, titre | {liste} peuvent lancer le combat | oui (187) | A-13 |
| CO-103 | draft-coaching.js:298 | comp.engage, débutant | Ils peuvent démarrer un combat d'un coup. Évite de rester seul dans une zone que tu ne vois pas, surtout près d'un objectif. | oui (93) | — |
| CO-104 | draft-coaching.js:299 | comp.engage, intermédiaire | Équipe qui choisit quand le combat commence : place-toi avant l'apparition des objectifs, pas au dernier moment. | oui (47) | — |
| CO-105 | draft-coaching.js:300 | comp.engage, avancé | sources d'engage : leur force est de forcer le combat. La vision et l'ordre d'arrivée sur les objectifs deviennent décisifs. | oui (47) | — |
| CO-106 | draft-coaching.js:304 | comp.poke, titre | {liste} touchent de très loin | oui (104) | A-13 |
| CO-107 | draft-coaching.js:305 | comp.poke, débutant | Ils t'abîment de loin avant le combat. Ne reste pas à découvert sans raison : ta vie baisse pour rien. | oui (52) | — |
| CO-108 | draft-coaching.js:306 | comp.poke, intermédiaire | Équipe de poke : si vous arrivez affaiblis, le combat démarre mal. Cherche à engager vite, ou à ne pas rester à portée. | oui (26) | A-16 |
| CO-109 | draft-coaching.js:307 | comp.poke, avancé, critère | Le moteur en tient compte pour {C} (« {critère} » : {raison}). | oui (9) | — |
| CO-110 | draft-coaching.js:307 | comp.poke, avancé, sans critère | Le poke gagne le temps : il faut soit le raccourcir, soit soigner. Ton équipe n'est pas connue de l'outil. | oui (17) | — |
| CO-111 | draft-coaching.js:319 | comp.damage, titre | `^Dégâts surtout (physiques\|magiques) en face$` | oui (91) | A-13 |
| CO-112 | draft-coaching.js:320 | comp.damage, débutant | Un objet qui protège des dégâts {x} peut donc être plus utile que d'habitude : regarde lesquels existent. | oui (49) | — |
| CO-113 | draft-coaching.js:321 | comp.damage, intermédiaire | une résistance adaptée a plus de valeur. Vérifie en partie, car les objets adverses changent le partage. | oui (21) | — |
| CO-114 | draft-coaching.js:322 | comp.damage, avancé | Estimation d'après les notes de Riot et une table écrite à la main. Le vrai partage dépend des objets et de chaque partie : à confirmer une fois en jeu. | oui (21) | — |
| CO-115 | draft-coaching.js:323 | comp.damage, limite | Estimation, pas une mesure : les dégâts réels dépendent des objets. | oui (91) | — |
| CO-116 | draft-coaching.js:346 | champ.profile, titre | Comprendre {C} | oui (2315) | — |
| CO-117 | draft-coaching.js:347 | champ.profile, débutant | `^.+ (se joue à (longue portée\|portée moyenne\|corps à corps) \(\d+\)\|change de portée selon sa forme ou sa transformation)\.` | oui (1579) | A-25 |
| CO-118 | draft-coaching.js:348-349 | champ.profile, intermédiaire et avancé | `^Classes Riot :` | oui (736) | A-11 |
| CO-119 | draft-coaching.js:332-342 | champ.profile, forces et risques | `(Points forts : \|Attention : \|Sa fiche ne lui donne pas de point fort particulier\.)` | oui (2327) | — |
| CO-120 | draft-coaching.js:350 | champ.profile, limite | Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main. | oui (2315) | — |
| CO-121 | draft-coaching.js:354 | champ.difficulty, titre | {C} peut demander de la pratique | oui (357) | — |
| CO-122 | draft-coaching.js:355 | champ.difficulty, débutant | Si tu découvres ce champion, prévois un temps de prise en main. Le choix reste le tien. | oui (357) | — |
| CO-123 | draft-coaching.js:356 | champ.difficulty, limite (aussi maîtrise) | Note de Riot non documentée et parfois contredite par celle du client : une indication, pas une mesure. | oui (357) | — |
| CO-124 | draft-coaching.js:364 | champ.alternative, titre | Quand préférer {C} ? | oui (252) | B-23 |
| CO-125 | draft-coaching.js:365 | champ.alternative, débutant [non affiché] (minLevel 2) | fait mieux sur un point : {raison}. | non affiché | — |
| CO-126 | draft-coaching.js:366 | champ.alternative, intermédiaire | Si ce point compte le plus pour ta partie, ce choix devient intéressant. | oui (126) | B-23 |
| CO-127 | draft-coaching.js:367 | champ.alternative, avancé | `: avantage de -?\d+,\d sur «` | oui (126) | B-23 |
| CO-128 | draft-coaching.js:421 | champ.alternative, avancé (when2) | Ce choix passerait devant {x}. | oui (27) | B-10 |
| CO-129 | draft-coaching.js:421 | champ.alternative, avancé (when2 vide) | Aucun changement simple de la draft ne le fait passer devant. | oui (99) | B-10, B-23 |
| CO-130 | draft-coaching.js:373 | limits.opponent-assumed, titre | `^(Top\|Jungle\|Mid\|ADC\|Support) supposé, à vérifier$` | oui (1521) | A-10 |
| CO-131 | draft-coaching.js:374 | limits.opponent-assumed, débutant | DRAFT pense que c'est {C}, sans certitude. Si tu sais que c'est un autre champion, corrige-le. | oui (913) | — |
| CO-132 | draft-coaching.js:375 | limits.opponent-assumed, intermédiaire | déduit des rôles habituels. Une correction change le matchup pris en compte. | oui (304) | — |
| CO-133 | draft-coaching.js:376 | limits.opponent-assumed, avancé | Déduction probabiliste sur les rôles habituels (confiance {x}), jamais une donnée du client. Sa qualité dépend de l'âge des données de rôles. | oui (304) | — |
| CO-134 | draft-coaching.js:379 | limits.unknown-champion, titre | {C} : champion inconnu de DRAFT | non | — |
| CO-135 | draft-coaching.js:380 | limits.unknown-champion | Aucune fiche pour ce champion : il n'entre pas dans ce conseil. Regarde ses sorts en jeu. | non | — |
| CO-136 | draft-coaching.js:381 | limits.unknown-champion, limite | Rien n'est inventé pour un champion sans fiche. | non | — |
| CO-137 | draft-coaching.js:384 | limits.rules-only, titre (avancé) | Ce classement repose sur des règles, pas sur des statistiques | oui (364) | — |
| CO-138 | draft-coaching.js:385 | limits.rules-only, débutant et intermédiaire [non affiché] (minLevel 3) | `^(Aucun bilan de vraies parties n'intervient ici\.\|Le score vient de règles et de fiches écrites à la main, sans mesure\.)$` | non affiché | — |
| CO-139 | draft-coaching.js:386 | limits.rules-only, avancé | Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle. | oui (364) | — |
| CO-140 | draft-coaching.js:397 | personal.experience, titre | Tu as plus d'expérience sur {C} | oui (9) | — |
| CO-141 | draft-coaching.js:398 | personal.experience, débutant | Connaître son champion compte : à toi de voir si cela change ton choix. | oui (3) | — |
| CO-142 | draft-coaching.js:399 | personal.experience, intermédiaire | nettement plus de parties que {C}. Cela ne change pas le classement, mais c'est un argument à peser. | oui (3) | — |
| CO-143 | draft-coaching.js:400 | personal.experience, avancé | hors du score. Le lien avec tes résultats n'est pas établi par ce seul chiffre. | oui (3) | — |
| CO-144 | draft-coaching.js:401 | personal.experience, limite (aussi maîtrise) | Le nombre de parties ne dit pas si tu joues bien ce champion. | oui (9) | — |
| CO-145 | draft-coaching.js:407 | personal.results, titre | Tes résultats sur {C} se distinguent | oui (6) | — |
| CO-146 | draft-coaching.js:408 | personal.results, débutant | `^Sur \d+ parties, tu gagnes (plus\|moins) souvent avec .+ qu'avec tes autres champions\.$` | oui (2) | — |
| CO-147 | draft-coaching.js:409 | personal.results, intermédiaire | écart net avec le reste de ton compte, dans le sens | oui (2) | — |
| CO-148 | draft-coaching.js:410 | personal.results, avancé | Sans lien de cause établi : adversaires et niveau changent. | oui (2) | — |
| CO-149 | draft-coaching.js:411 | personal.results, limite | Passé sur ton compte : ni une prévision, ni un jugement sur ton niveau. | oui (6) | — |
| CO-150 | draft-coaching.js:414 | personal.too-few, titre (intermédiaire et avancé) | Trop peu de parties pour juger sur {C} | oui (10) | — |
| CO-151 | draft-coaching.js:415 | personal.too-few, débutant [non affiché] (minLevel 2) | trop peu pour dire comment ce champion te réussit. | non affiché | — |
| CO-152 | draft-coaching.js:415 | personal.too-few, intermédiaire | aucune tendance personnelle n'est tirée sous {N} parties. | oui (5) | — |
| CO-153 | draft-coaching.js:415 | personal.too-few, avancé | aucune conclusion sur tes résultats. | oui (5) | — |
| CO-154 | draft-coaching.js:425-426 | Principe débutant : dégâts | `^(Dégâts physiques et magiques\|L'armure réduit les dégâts physiques, la résistance magique réduit les dégâts magiques\..+)$` | oui (188) | A-28 |
| CO-155 | draft-coaching.js:427 | Principe débutant : contrôles | `^(Pourquoi les contrôles comptent\|Un joueur immobilisé ne peut ni fuir ni attaquer\..+)$` | oui (1146) | — |
| CO-156 | draft-coaching.js:428 | Principe débutant : placement | `^(Se placer avant le combat\|Avant un combat, demande-toi qui peut t'atteindre\..+)$` | oui (1714) | — |
| CO-157 | draft-coaching.js:429 | Principe débutant : portée | `^(Jouer avec la portée\|Le plus court doit s'approcher pour toucher.+)$` | oui (280) | — |
| CO-158 | draft-coaching.js:430 | Principe débutant : échanges | `^(Un bon échange\|Attaque quand un sort important de ton adversaire vient de partir, puis recule\..+)$` | oui (56) | — |
| CO-159 | draft-coaching.js:431 | Principe débutant : farm | `^(L'or vient d'abord des sbires\|Chaque sbire que tu achèves te donne de l'or\..+)$` | oui (172) | A-14, A-15 |
| CO-160 | draft-coaching.js:432 | Principe débutant : vagues | `^(Pousser avant de bouger\|Avant de quitter ta voie, pousse ta vague contre la tour adverse\..+)$` | oui (230) | A-14 |
| CO-161 | draft-coaching.js:433 | Principe débutant : objectifs | `^(Les objectifs gagnent les parties\|Une mise à mort rapporte davantage si elle mène à quelque chose.+)$` | non | A-15 |
| CO-162 | draft-coaching.js:434 | Principe débutant : vision | `^(Voir avant d'avancer\|Une balise montre qui arrive\..+)$` | non | A-15 |
| CO-163 | draft-coaching.js:435 | Principe débutant : top | `^(Le rôle du top\|Tu joues souvent seul et loin de ton équipe\..+)$` | oui (432) | A-15 |
| CO-164 | draft-coaching.js:436 | Principe débutant : jungle | `^(Le rôle du jungler\|Tu n'as pas de voie : tu choisis où aider\..+)$` | oui (446) | A-14 |
| CO-165 | draft-coaching.js:437 | Principe débutant : mid | `^(Le rôle du mid\|Ta voie est courte et centrale : tu peux aider les deux côtés\..+)$` | oui (566) | A-14 |
| CO-166 | draft-coaching.js:438 | Principe débutant : ADC | `^(Le rôle de l'ADC\|Tu fais beaucoup de dégâts mais tu es fragile\..+)$` | oui (392) | — |
| CO-167 | draft-coaching.js:439 | Principe débutant : support | `^(Le rôle du support\|Ta valeur ne se lit pas dans les mises à mort\..+)$` | oui (408) | A-15 |
| CO-168 | draft-coaching.js:28-41 | Définitions COACH_TERMS [non affiché] | `^Les joueurs solides qui se placent devant et encaissent les coups\.$` | non affiché | — |

### web/draft-mastery.js (25 entrées, 21 vues à l'écran)

| ID | Emplacement | Contexte et calcul | Texte ou gabarit | Vu à l'écran | Classement |
|---|---|---|---|---|---|
| MA-001 | draft-mastery.js:325 | Bloc « Ton choix », égalité | Égalité sur la draft | oui (801) | — |
| MA-002 | draft-mastery.js:325 | Bloc « Ton choix », nom affiché (pas forcément le 1er) | Meilleur choix pour la draft | oui (1514) | B-02 |
| MA-003 | draft-mastery.js:213 | Provenance (incertitudes) | `^(Mesuré sur tes parties\|Petit échantillon de tes parties\|Principe général)(\. .+)?$` | oui (5003) | — |
| MA-004 | draft-mastery.js:291 | Compromis, aucun champion | Aucun champion disponible dans ton pool. | non | — |
| MA-005 | draft-mastery.js:292 | Compromis, champion choisi | La draft met plutôt en avant {liste} : ton choix reste possible, voici ce qu'il implique. | non | — |
| MA-006 | draft-mastery.js:293 | Compromis, sans historique | Sans historique, seule la lecture de la draft est disponible. | oui (2285) | — |
| MA-007 | draft-mastery.js:294 | Compromis, aucun champion assez joué | parties dans ce rôle : seule la lecture de la draft est disponible. | oui (3) | — |
| MA-008 | draft-mastery.js:296 | Compromis, même champion | ressort sur la draft, et c'est aussi ton champion le plus joué. | oui (12) | — |
| MA-009 | draft-mastery.js:300-301 | Compromis, égalité | `^Égalité sur la draft entre .+(Rien ne les sépare : à toi de voir\.\|c'est un argument, à toi de voir\.)` | oui (6) | — |
| MA-010 | draft-mastery.js:305 | Compromis, débutant | `^La draft penche (nettement\|légèrement) vers` | oui (3) | B-02 |
| MA-011 | draft-mastery.js:306 | Compromis, intermédiaire | `^Écart (net\|faible) sur la draft en faveur de` | oui (3) | B-02 |
| MA-012 | draft-mastery.js:307 | Compromis, avancé | `^Écart moteur de -?\d+,\d+ points en faveur de` | oui (3) | B-02, A-27 |
| MA-013 | draft-mastery.js:295 | Compromis, expériences proches | presque au même niveau d'expérience. | oui (12) | — |
| MA-014 | draft-mastery.js:267-268 | Incertitude, sans historique | `^(Aucun historique importé\|DRAFT ne sait pas ce que tu connais déjà : ce conseil ne porte que sur la draft\.\|Sans import, aucune expérience n'est connue\.\|L'absence d'historique n'est pas lue comme un manque d'expérience\.)$` | oui (4570) | — |
| MA-015 | draft-mastery.js:269 | Incertitude, draft incomplète | Ce conseil peut encore changer. | oui (1507) | — |
| MA-016 | draft-mastery.js:270 | Incertitude, adversaire supposé | Adversaire supposé : {C} | oui (1521) | A-10 |
| MA-017 | draft-mastery.js:270 | Incertitude, adversaire supposé, texte | DRAFT le déduit des rôles habituels. Si tu sais que c'est un autre champion, corrige-le. | oui (1521) | — |
| MA-018 | draft-mastery.js:270 | Incertitude, limite | Ce n'est pas une information du client. | oui (1521) | — |
| MA-019 | draft-mastery.js:271 | Incertitude, adversaire inconnu | `^(Adversaire direct inconnu\|Le conseil ne peut pas encore porter sur un duel précis\.)$` | oui (1570) | A-10 |
| MA-020 | draft-mastery.js:272 | Incertitude, matchup sans donnée | `^(.+ contre .+ : aucune donnée\|Ni statistique, ni tableau pour ce couple : le matchup n'entre pas dans ce conseil\. Rien n'est estimé à sa place\.)$` | oui (2374) | — |
| MA-021 | draft-mastery.js:274-275 | Incertitude, petit échantillon | `(: trop peu pour conclure$\|trop peu pour savoir si ce champion te réussit\.\|^Intervalle à 95 % : )` | oui (24) | — |
| MA-022 | draft-mastery.js:278 | Incertitude, pas de partie récente | `^(Pas de partie récente sur .+\|Ta dernière partie importée dans ce rôle date de \d+ jours\. Le jeu a pu changer entre-temps\.)$` | oui (24) | — |
| MA-023 | draft-mastery.js:279 | Incertitude, champion modifié | `( a changé$\|seulement depuis\. Ton expérience passée n'est peut-être plus aussi utile\.\|La liste des changements est fournie à la main\.)` | non | — |
| MA-024 | draft-mastery.js:283-284 | Incertitude, adversaire rarement rencontré | `( peut surprendre$\|Ce matchup peut être difficile si tu ne connais pas ses compétences\.\|rencontres? avec .+ dans ton historique\.\|rencontres? sur ce couple : aucune tendance personnelle n'est tirée\.\|Tu as pu jouer ou rencontrer ce champion ailleurs\.)` | oui (9) | — |
| MA-025 | draft-mastery.js:224-284 | Sections théorie et expérience [non affiché] | `^Pourquoi .+ en théorie$` | non affiché | — |

### web/draft-data.js (15 entrées, 15 vues à l'écran)

| ID | Emplacement | Contexte et calcul | Texte ou gabarit | Vu à l'écran | Classement |
|---|---|---|---|---|---|
| DA-001 | draft-data.js:91-153 | Nom des sources | `^(Noms, portraits et patch\|Rôles joués, parties récentes\|Rôles, référence livrée\|Rôles habituels\|Bilans de matchups\|Tableau de matchups\|Fiches de champions\|Tes statistiques importées)$` | oui (208) | — |
| DA-002 | draft-data.js:168 | Type de source | `^(Officiel\|Observé\|Estimation externe\|Écrit à la main)$` | oui (208) | — |
| DA-003 | draft-data.js:91-158 | Origine d'une source | `^(Riot Games, Data Dragon\|API Riot, avec ta clé personnelle\|Meraki Analytics\|Écrit à la main\|API Riot, tes comptes)\.` | oui (208) | — |
| DA-004 | draft-data.js:92-159 | Conditions d'une source | `(Données publiques de Riot\.\|Calculés sur ton PC avec ta clé\. Aucun nom de joueur n'est gardé\.\|Licence non confirmée\|Propre au projet\.\|Privées : le fichier n'est jamais publié\.)` | oui (208) | — |
| DA-005 | draft-data.js:68,138 | Note : bilans non calculés | `Pas encore calculés : lance « Mettre à jour les (rôles\|matchups) » avec ta clé Riot\.` | oui (48) | — |
| DA-006 | draft-data.js:76 | Note : rôles calculés | `(Calcul fait avant le suivi par patch\|patch\(s\) conservé\(s\)\. Les plus anciens comptent moins, sans être supprimés\.)` | oui (2) | — |
| DA-007 | draft-data.js:95 | Note : patch | `(Dernier patch connu\. Hors ligne, il peut être en retard\.\|Patch inconnu : l'âge des données est estimé d'après leur date\.)` | oui (26) | — |
| DA-008 | draft-data.js:108,111 | Note : Meraki | `(Non livrée dans cette version : les rôles habituels écrits à la main prennent le relais\.\|Des taux, sans nombre de parties)` | oui (26) | — |
| DA-009 | draft-data.js:128-129 | Note : rôles écrits | `(Fichier absent\.\|champions\. Avis général, utilisé seulement là où aucune autre référence n'existe\.)` | oui (26) | — |
| DA-010 | draft-data.js:144 | Note : bilans calculés | `Pour le rôle (Top\|Jungle\|Mid\|ADC\|Support)\. Sous \d+ parties utiles, un bilan n'est pas utilisé\.` | oui (2) | — |
| DA-011 | draft-data.js:150 | Note : tableau de matchups | adversaires au mid, pour Twisted Fate, Fizz, Kog'Maw et Xerath seulement. Sans statistiques ni patch. | oui (26) | — |
| DA-012 | draft-data.js:155 | Note : fiches | fiches d'avis général, sans statistiques ni patch. À revoir si un champion est refait. | oui (26) | — |
| DA-013 | draft-data.js:162-163 | Note : statistiques importées | `(Pas d'import sur ce PC\. Facultatif\.\|Affichées seulement : elles ne changent pas le classement\.)` | oui (26) | — |
| DA-014 | draft-data.js:74,143 | Rang des bilans | Silver à Émeraude, classé solo/duo | oui (4) | A-20 |
| DA-015 | draft-data.js:330 | Hypothèse « pas encore choisi » (ligne adversaire) | `pas encore choisi` | oui (1626) | — |

### web/draft-profiles.js (5 entrées, 4 vues à l'écran)

| ID | Emplacement | Contexte et calcul | Texte ou gabarit | Vu à l'écran | Classement |
|---|---|---|---|---|---|
| PR-001 | draft-profiles.js:123-127 | Import refusé | `^(Le fichier est vide\.\|Le fichier est trop volumineux pour une configuration\.\|Ce fichier n'est pas un JSON valide\.\|Ce fichier n'est pas une configuration DRAFT\.)` | oui (4) | — |
| PR-002 | draft-profiles.js:131-136 | Import refusé, rôle | `(Le rôle .+ est mal formé\.\|Le rôle .+ n'a pas de liste de champions\.\|Le rôle .+ contient trop de champions\.\|Champion inconnu dans .+ : \|Le rôle .+ a des exclusions mal formées\.)` | oui (2) | A-19 |
| PR-003 | draft-profiles.js:140-166 | Import, avertissements | `(Favori ignoré pour .+ : il n'est pas dans le pool\.\|Ancien format : seul ce rôle est remplacé\.\|Rôle absent du fichier, laissé vide : \|Préférence ignorée : )` | oui (1) | A-19 |
| PR-004 | draft-profiles.js:145-156 | Import refusé, format | `(Le rôle de cet ancien profil est inconnu\.\|La version du format est absente\.\|Ce fichier vient d'une version plus récente de DRAFT\.\|Les rôles sont absents de ce fichier\.\|Rôle inconnu : )` | oui (3) | — |
| PR-005 | draft-profiles.js:173,189 | Import, enregistrement | `(Configuration invalide : rien n'a été modifié\.\|L'enregistrement a échoué : ta configuration précédente est conservée\.)` | non | — |
