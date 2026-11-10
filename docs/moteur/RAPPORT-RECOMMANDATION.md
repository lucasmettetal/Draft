# Moteur de recommandation : état, fragilités, pistes

**Volet 2 de l'étude du moteur.** Branche `etude/moteur-v217`, copie de `main` `78519fb` (DRAFT 2.16.1). Rapport du 10 novembre 2026.

**Ce rapport ne change rien au moteur.** Aucun poids, aucune règle, aucun texte n'a été modifié. Les variantes de poids testées ici existent seulement dans une page d'étude, chargée sur une copie des fichiers.

## Légende

- **[Code]** : vérifié dans le code, avec `fichier:ligne`.
- **[Mesuré]** : mesuré sur des drafts simulées. Ces chiffres décrivent le comportement du moteur. Ils ne disent pas si un conseil est bon : aucune victoire n'est mesurée.
- **[SYNTHÉTIQUE]** : composition ou bilan inventé pour isoler une règle. Ce ne sont pas des données réelles.
- **[Hypothèse]** : déduction plausible, non vérifiée par une mesure.

## Méthode et reproduction

- Pages d'étude : `docs/moteur/recommandation/etude-sensibilite.html` et `etude-maintien.html`.
- Lancement : `docs/moteur/recommandation/run-etude.ps1` (paramètre `-Page`). Tout est copié dans `%TEMP%\draft-etude-agent2`. Le calcul local des rôles est lu sans être modifié.
- Résultats bruts : `resultats-sensibilite.json` et `resultats-maintien.json`, dans le même dossier.
- **Drafts :** 1 500 drafts complètes par pool, tirées rôle par rôle selon les fréquences des 1 000 parties du calcul local des rôles. C'est la même méthode que `docs/AUDIT-MOTEUR.md`. Graines fixes. Pour le maintien du nom affiché : 800 drafts saisies une à une, de 1 à 4 adversaires.
- **Pools :** les 8 pools du tableau E1 ci-dessous. Cinq viennent de l'audit existant. Trois sont ajoutés pour le mid : les 4 champions d'origine, un pool mixte et un pool sans aucun champion du tableau.
- **Non-régression :** les suites existantes passent sur la copie de travail. Moteur 190/190, conseils 133/133, égalités 18/18, maîtrise 91/91.
- **Limite :** il n'existe aucun bilan de matchups calculé sur ce PC (pas de `draft-matchups-live.js`). Le relevé de vraies drafts (`validation-drafts.csv`) contient une seule ligne. Toute la partie « bilans calculés » est donc lue dans le code, ou testée sur un cas synthétique.

---

## 1. Le scoring actuel

### 1.1 Chaîne de calcul [Code]

1. `computeVerdict` (`web/index.html:827-877`) lit les adversaires saisis.
2. `opponentBelief` et `beliefScenarios` (`web/draft-data.js:289-325`) estiment l'adversaire direct et gardent jusqu'à 4 hypothèses.
3. `analyze` (`web/draft-engine.js:310-320`) évalue chaque pick disponible avec `evaluatePick` (`:278-290`) ou `evaluateScenarios` (`:293-308`).
4. Chaque critère est une règle de `RULES` (`:195-276`). Elle renvoie une note et un texte, ou `null` si le critère ne s'applique pas.
5. Le tri (`:318`) place les picks, `markTies` (`:326-335`) marque les égalités et `holdPick` (`:363-368`) stabilise le nom affiché.
6. Le badge « Choix net » ou « Choix serré » est calculé dans `index.html:855-856`.

### 1.2 Critères et pondérations [Code]

Score d'un pick = somme de (note × poids). Il n'y a **aucune normalisation** : un critère absent vaut 0, et le score ne tient compte ni de la taille du pool ni du nombre de critères actifs. Les poids sont définis dans `draft-engine.js:121-131`.

| Critère (clé) | Poids | Notes possibles (règle) | Contribution min / max | Condition de déclenchement |
|---|---|---|---|---|
| Matchup direct (`matchup`) | 3 | Tableau : 1er choix +1, 2e choix +0,5, sans avis 0, à éviter -4/3 (`:203-208`). Bilan calculé : écart / 5 points, borné à ±4/3 (`:200`) | -4 / +3 (tableau), -4 / +4 (bilan) | Mid et 4 champions seulement pour le tableau (`:101-103`, `:204`). Tout rôle si un bilan existe (`:197`) |
| Sécurité sans adversaire connu (`blind`) | 1 | 1 - 2 × (mauvais matchups / maximum du pool) (`:211-216`) | -1 / +1 | Mid inconnu, rôle mid, champion du tableau |
| Cibles exploitables (`targets`) | 1,5 | Trait h : +2 dès 2 carrys, +0,5 pour 1, **-2 si 0 carry et au moins 4 adversaires connus**. E : +1 dès 2. l : +0,5 dès 2 (`:218-228`) | -3 / +3 | Selon la fiche |
| Tanks et bruisers (`front`) | 2 | k : +1,5 pour 2, +2 dès 3. h+d : -0,5 pour 3, -1 dès 4. w : -0,25 dès 3 (`:229-241`) | -2 / +4 | Frontline = profil `t` ou `b` dans `ROSTER` (`:188`) |
| Survie aux plongeurs (`dive`) | 2 | Dès 2 plongeurs : i -1,5 (-2 dès 3), t +1, c +1, sinon 0 (`:242-249`) | -4 / +2 | |
| Engages et contrôles (`control`) | 2 | h+d face à 2 contrôles ou plus : -1,5. e face à 2 contrôles ou 2 engages : +0,75. i face à 3 engages ou plus : -0,5 (`:250-255`) | -3 / +1,5 | |
| Poke et longue portée (`range`) | 1 | Dès 2 champions de poke : l +1, o -1 (`:256-261`) | -1 / +1 | Liste `POKE` (`:119`) |
| Protection nécessaire (`peel`) | 1,5 | p : 0 si un frontline allié est supposé, sinon -2. q : -2, -4/3 ou +4/3 selon la mobilité adverse (`:262-269`) | -3 / +2 | `hasFront` vient d'une préférence (`index.html:891`), pas de la draft |
| Pression sur la carte (`map`) | 0,5 | g +1, r +0,5, n -0,5 (`:270-275`) | -0,5 / +0,5 | **Ne dépend jamais de la draft** |

**Deux alphabets de profils coexistent** [Code]. `ROSTER` décrit les adversaires (`draft-engine.js:46-47`). `CHAMPION_TRAITS` décrit les picks (`draft-champions.js:1-5`). La lettre `c` y a deux sens : « contrôle ciblé » dans le premier, « protège ses alliés » dans le second. Ce n'est pas un bug, mais c'est un piège pour qui modifie les fiches.

### 1.3 Matchups calculés [Code]

Constantes définies dans `draft-engine.js:137` et `draft-data.js:7,50`.

- Un bilan n'est utilisé qu'à partir de **30 parties pondérées**. Il est ramené vers le winrate moyen du pick avec **50 parties fictives**.
- Le patch actuel compte plein. Chaque patch plus ancien compte deux fois moins, avec un plancher à 0,02.
- Un écart de **-3 points ou moins** classe le pick « à éviter ». Un écart de **+5 points** donne la note pleine.
- Le moteur compare l'**écart au winrate moyen du pick** (`:143-170`). `docs/technique/MOTEUR.md` précise que cet écart « ne compare pas les picks entre eux en valeur absolue ». Le moteur s'en sert pourtant pour classer les picks entre eux.

### 1.4 Scénarios sur l'adversaire direct [Code]

- Constantes (`draft-data.js:14`) : une hypothèse doit atteindre 10 % pour être gardée, et on en garde 4 au plus. À partir de 90 %, seule l'hypothèse principale est gardée.
- Une hypothèse « pas encore choisi » est ajoutée si sa probabilité atteint 10 % (`:319`).
- Chaque critère reçoit la moyenne pondérée de ses notes (`draft-engine.js:298`). Comme le calcul est linéaire, cela revient à pondérer les scores.
- Les **textes** des hypothèses sont seulement juxtaposés : deux au plus, séparés par « · », sans probabilité (`:299-300`).

### 1.5 Seuils de décision [Code]

| Situation | Règle | Où |
|---|---|---|
| Égalité | Scores identiques au millionième près (`tieKey`) | `draft-engine.js:323` |
| Ordre dans une égalité | 1. le pick le moins pénalisé (son pire critère), 2. le favori du joueur, 3. l'ordre alphabétique | `draft-engine.js:316-318` |
| Choix serré | Écart de moins de 2 points (`HOLD_GAP`) avec le rival. Le rival est le premier pick qui n'est pas « à éviter » | `draft-engine.js:52`, `index.html:855-856` |
| Choix net | Écart d'au moins 2 points | idem |
| Nom affiché pendant la draft | Change seulement si un autre pick mène d'au moins 2 points. À 5 adversaires, le 1er du classement s'affiche toujours | `draft-engine.js:363-368`, `index.html:844` |

**Le même seuil de 2 points sert à deux choses** : stabiliser le nom affiché et décider qu'un choix est net. Le conseil « choix serré » s'en sert aussi (`draft-coaching.js:133,145`).

### 1.6 Tableau écrit à la main [Mesuré, sur le tableau lui-même]

41 adversaires au mid, pour 4 picks seulement (`draft-engine.js:2-44`).

| Pick | 1er choix | 2e choix | À éviter | Sans avis | Points moyens au critère | Note « sans adversaire connu » |
|---|---|---|---|---|---|---|
| Twisted Fate | 14 | 13 | 1 | 12 | +1,44 | +0,85 |
| Fizz | 18 | 6 | 13 | 3 | +0,28 | -1 |
| Kog'Maw | 8 | 4 | 10 | 19 | -0,25 | -0,54 |
| Xerath | 1 | 18 | 11 | 10 | -0,35 | -0,69 |

La note « sans adversaire connu » compte seulement les matchups « à éviter ». Elle ne tient pas compte de la fréquence de chaque adversaire au mid.

---

## 2. Étude par thème

### 2.1 Synergies et counters

- **[Code] Aucune synergie n'est calculée.**
  - `analyze(enemies, mid, hasFront, scenarios)` ne reçoit aucun pick allié (`draft-engine.js:310`).
  - `compoOf` ne lit que les adversaires (`:183-192`).
  - Le seul signal sur les alliés est `hasFront`. C'est une préférence fixe, cochée par défaut (`index.html:891,2287`), jamais déduite de la draft.
  - Le moteur ne fait donc que des **counters contre une composition adverse**, à partir de profils à lettres.
  - Le module de conseils le reconnaît : « que l'outil ne connaît pas » (`draft-coaching.js:273`).
- **[Code] Les counters sont des règles à seuils.** Les adversaires sont comptés par catégorie (carrys, frontline, plongeurs, contrôles, engages, mobiles, poke). Une règle se déclenche à 2 ou à 3 adversaires d'une catégorie. Aucune paire de champions n'est évaluée, à part le tableau de matchups du mid.

### 2.2 Frontline, dégâts AP/AD, engage, contrôle

- **Frontline [Mesuré + Code].**
  - 69 champions sur 173 comptent comme frontline (profils `t` ou `b`), dont des bruisers très mobiles.
  - Sur un cas SYNTHÉTIQUE, Darius et Yasuo suffisent à donner +3 à Kog'Maw (+1,5 × 2), qui passe 1er. Avec Darius seul, Kog'Maw est dernier (-0,79).
  - Ce défaut est déjà documenté dans `docs/AUDIT-MOTEUR.md`, qui propose la variante « Sans mobiles ». Elle n'est pas appliquée.
- **Dégâts AP/AD [Code].** Ils sont absents du score. Seul un conseil en parle (`comp.damage`, `draft-coaching.js:309-323`), présenté comme une estimation. Comme les alliés ne sont pas connus, le moteur ne peut pas juger l'équilibre des dégâts de l'équipe.
- **Engage et contrôle [Code + Mesuré].**
  - Dès 2 contrôles ciblés en face, Fizz perd 3 points et Twisted Fate en gagne 1,5. L'écart entre eux bouge donc de 4,5 points.
  - C'est la **première cause des changements de nom en tête** pour le pool d'origine : 33 % des cas (§ 3.1).

### 2.3 Matchups directs

- **Couverture [Code].**
  - Le tableau ne couvre que le rôle mid et Twisted Fate, Fizz, Kog'Maw et Xerath (`draft-engine.js:101-103`).
  - Hors du mid et sans bilans calculés, le critère le plus lourd (poids 3) et la sécurité sans adversaire connu ne s'appliquent jamais.
  - **Dans ces rôles, l'identité de l'adversaire direct ne change donc aucun score.** Le moteur l'estime, l'affiche et la pèse dans les scénarios, sans aucun effet sur le classement.
- **Biais des pools mixtes [Mesuré].**
  - Un champion hors tableau reçoit `null`, donc 0, au critère de matchup.
  - Pool testé : Twisted Fate, Fizz, Ahri, Syndra, Orianna. Le matchup décide de l'écart entre le 1er et le 2e dans **66,6 %** des drafts.
  - Syndra et Orianna ne sont **jamais** 1ers sur 1 500 drafts.
  - Cas SYNTHÉTIQUE, même profil d'adversaire (`s`, poke) :
    - Viktor au mid (dans le tableau) : Fizz 5,25, Kog'Maw 2,75, Xerath 2,75, Twisted Fate 1,5.
    - Heimerdinger au mid (hors tableau) : Kog'Maw 2,75, Fizz 2,25, Twisted Fate 1,5, Xerath 1,25.
    - Le 1er change seulement parce qu'un adversaire est présent dans le tableau.
- **Asymétrie de l'échelle [Code].** Dans le tableau, « à éviter » vaut -4 points et « 1er choix » +3.
- **Bilans calculés au-delà de 30 parties [Code + SYNTHÉTIQUE].**
  - Ce chemin n'a jamais tourné sur ce PC.
  - Cas SYNTHÉTIQUE : Twisted Fate a 18 victoires sur 40 parties contre Ahri, pour un winrate moyen de 52 %. Le moteur affiche « -3 % contre Ahri sur 40 parties » et classe Twisted Fate « à éviter ». Sa contribution est de -1,86, alors qu'un « à éviter » du tableau vaut -4.
  - Dans la même draft, Fizz reste noté par le tableau (+3). **Deux sources d'échelles différentes se mélangent dans un même classement** (`draft-engine.js:197-204`), et le libellé « à éviter » n'a pas le même poids selon la source.

### 2.4 Gestion des égalités

- **Ordre du pool [Mesuré].** Inverser le pool ne change le classement dans **aucune** des drafts testées. La correction de la V2.14 tient.
- **Taux d'égalité en tête [Mesuré].**

| Pool | Égalité en tête |
|---|---|
| Support | 61,1 % |
| Jungle (Cho'Gath) | 38,9 % |
| Top | 37,1 % |
| ADC | 17,3 % |
| Mid mixte | 10,5 % |
| Mid hors tableau | 2,5 % |
| Mid d'origine | 1,8 % |
| Jungle classique | 0 % |

- **Ordre affiché dans une égalité [Code + Mesuré].**
  - Le tri départage d'abord par le risque le plus faible (`draft-engine.js:316`).
  - L'écran dit pourtant « l'ordre d'affichage n'a pas de sens » (`index.html:1691`).
  - Le conseil dit « ne vient que de l'ordre alphabétique, ou de ton favori » (`draft-coaching.js:129`).
  - Pour le pool d'origine, **89,5 % des égalités en tête** sont en fait départagées par le risque. Les deux textes sont alors inexacts. Dans les autres pools testés, les risques sont identiques et les textes sont justes.

### 2.5 Fidélité des explications

**Point fort [Code].** Le score, les raisons, la matrice et la comparaison viennent des mêmes lignes de critères (`draft-engine.js:278-290`, `:457-468`). Une raison affichée correspond toujours à un critère réellement compté.

**Écarts relevés :**

| # | Écart | Preuve | Fréquence |
|---|---|---|---|
| E1 | Pendant la draft, le nom affiché peut être moins bien noté que le 1er (maintien à 2 points). Le module de conseils prend alors ce pick, et le rival est le 1er du classement (`draft-coaching.js:99-100`). Le conseil `draft.close` peut dire « X passe devant » et « Écart de -1,2 point » (`:150-152`) | Code + Mesuré | Condition remplie dans **20,4 %** des étapes de draft incomplète (mid d'origine), 32,8 % (Top), 23,2 % (Support), 14,8 % (Jungle classique), 1,4 % (Jungle Cho'Gath), 0,4 % (ADC) |
| E2 | Même cas pour `draft.conflict` : « reste devant » (`draft-coaching.js:160`) | Code | Non mesurée [Hypothèse : rare, car il faut aussi un critère à -2 ou moins] |
| E3 | Avec plusieurs adversaires possibles, le texte du matchup juxtapose deux hypothèses sans probabilité (`draft-engine.js:299-300`). Une raison « positive » peut alors contenir « À éviter contre X » (`:428-430`) | Code + Mesuré | Textes mélangés pour 15,3 % des picks en tête (mid d'origine). « À éviter » parmi les raisons positives : 1,5 % |
| E4 | « Frontline allié supposé. » est toujours affiché, même si la préférence est décochée (`index.html:1695`) | Code | Toujours, si la préférence est décochée |
| E5 | `whenBetter` (`draft-engine.js:475-487`), `V.without` (`index.html:859`), `midSwapEffect` (`draft-engine.js:489-496`) et l'indicateur « provisoire » (`index.html:869`) recalculent sans les scénarios. `whenBetter` force aussi le frontline allié. Ils peuvent contredire le classement affiché | Code | Non mesurée |
| E6 | Messages d'égalité inexacts (§ 2.4) | Code + Mesuré | 89,5 % des égalités en tête, mid d'origine |
| E7 | Le critère « décisif » peut être la pression sur la carte, qui ne dépend pas de la draft : 58,5 % des écarts en Jungle classique, 70,5 % en Support, 27,4 % au mid hors tableau. L'explication suit bien le calcul, mais elle montre que le choix repose alors sur un atout fixe du champion | Mesuré | Voir à gauche |
| E8 | Le badge « Choix net » se mesure par rapport au premier rival non « à éviter ». Il peut cacher un 2e pick à moins de 2 points | Code | 0 cas sur les drafts testées |

---

## 3. Classements incohérents ou fragiles

### 3.1 Un seul adversaire remplacé [Mesuré]

Draft complète. Un adversaire est remplacé par un autre champion du même rôle. Dans la colonne 3, l'adversaire direct estimé ne change pas.

| Pool | Nom en tête changé | Dont adversaire direct inchangé | Dont saut d'au moins 2 points | Cause principale |
|---|---|---|---|---|
| Mid d'origine | 25,5 % | 16,7 % | 67 % | contrôles 33 %, plongeurs 21 % |
| Mid mixte | 22,9 % | 13 % | 46,9 % | cibles 33 % |
| Mid hors tableau | 22,4 % | 18,4 % | 64,5 % | plongeurs 35 % |
| Jungle (Cho'Gath) | 31,3 % | 27,3 % | 47,9 % | frontline 37 % |
| Jungle classique | 21 % | 20 % | 88,7 % | plongeurs 73 % |
| Top | 19,5 % | 19,4 % | 82,8 % | frontline 72 % |
| ADC | 22,1 % | 26,1 % | 77,7 % | frontline 70 % |
| Support | 19,7 % | 18,1 % | 21,4 % | cibles 58 % |

**Lecture.** Un changement d'avis après une substitution n'est pas une erreur en soi. Mais la plupart de ces changements viennent d'un **seuil franchi** : un 2e plongeur, un 2e contrôle, un 2e bruiser. Ils ne viennent pas d'un glissement progressif.

### 3.2 Seuils en marche d'escalier [Code + SYNTHÉTIQUE]

Scores des 4 picks d'origine, adversaire direct inconnu.

| Composition SYNTHÉTIQUE | Résultat | Règle |
|---|---|---|
| Ornn, Sejuani, Leona → + Braum | Fizz passe de -1,75 à -5,75 | 4e adversaire connu : -3 aux cibles (`:223`), et -1 pour 3 frontlines |
| Zed → Zed, Talon | Xerath passe de 1,31 à -3,69, Kog'Maw de -0,79 à -3,79 | 2e plongeur : -3 (`:245`), et le bonus de protection (+2) disparaît |
| Ornn, Ahri, Janna → Ezreal à la place de Janna → + Vayne | Xerath passe de 1,31 à 0,06, puis à -1,94 | Protection pour le trait q : de +2 à 0, puis à -2 (`:265-267`) |
| Darius → Darius, Yasuo | Kog'Maw passe de -0,79 à 2,21 et devient 1er | 2e « bruiser », même mobile (`:233`) |

**Cas du 4e adversaire.** La pénalité « aucune cible fragile » ne s'applique qu'à partir de 4 adversaires connus (`:223`). Elle n'est donc pas évaluée tant que la draft compte 3 adversaires ou moins, puis tombe d'un coup.

### 3.3 Critères qui dominent [Mesuré]

Trois mesures : la part du poids absolu dans les scores, le changement du nom en tête quand un critère est retiré (poids 0), et le changement quand son poids varie de ±25 %.

| Pool | Critère le plus lourd | Retiré : nom en tête changé | ±25 % : pire cas |
|---|---|---|---|
| Mid d'origine | matchup 35,6 % (puis plongeurs, cibles, frontline, environ 15 % chacun) | cibles 39 %, matchup 38,1 % | frontline +25 % : 8,1 % |
| Mid mixte | cibles 43,7 % | matchup 71,1 % | matchup -25 % : 9,1 % |
| Mid hors tableau | cibles 45,3 % | cibles 53 % | cibles -25 % : 20 % |
| Jungle (Cho'Gath) | frontline 65,6 % | cibles 50,2 %, frontline 48,5 % | cibles ±25 % : 20,1 % |
| Jungle classique | cibles 76,4 % | carte 58,5 % | cibles +25 % : 18,1 % |
| Top | frontline 53,2 % | cibles 59,8 % | **0 % pour tous les critères** |
| ADC | plongeurs 41,8 % | frontline 63 % | frontline -25 % : 10,1 % |
| Support | plongeurs 60,2 % | cibles 61,1 % | portée -25 % : 11,2 % |

- **Hypothèse sur le Top.** Le classement ne réagit à aucun poids. Les fiches imposent un ordre fixe : celle d'Ornn (`tEk`) contient celle de Malphite (`tE`). Ornn est donc toujours au moins à égalité avec Malphite.
- **Poids non mesurés.** Les poids ne viennent d'aucune mesure (`MOTEUR.md`). Une variation de 25 %, invisible pour un joueur, change le nom en tête dans 8 à 20 % des drafts pour plusieurs pools.

### 3.4 Champions jamais conseillés [Mesuré]

| Pool | Jamais en tête sur 1 500 drafts |
|---|---|
| Mid mixte | Syndra, Orianna (environ 0) |
| Mid hors tableau | Syndra, Viktor |
| Jungle classique | Vi, Graves |
| ADC | Jinx, Kog'Maw |
| Support | Lulu |
| Jungle (Cho'Gath) | Briar (0,2 %) |

**Fiches sans aucune règle active**, hors pression sur la carte : Aatrox, Briar, Illaoi, Udyr, Zaahen (trait `d` seul), Rek'Sai, Rumble et Taliyah. Leur score vaut au plus ±0,5 dans toutes les drafts.

### 3.5 Ordre de saisie [Mesuré]

Avec les **mêmes 4 adversaires** saisis dans l'ordre inverse, le nom affiché diffère dans 10,6 % des cas (mid d'origine), 48,4 % (mid hors tableau) et 51 % (Jungle classique).

C'est l'effet voulu du maintien à 2 points (`holdPick`), et le badge « Choix serré » apparaît dans ces cas. Mais dans les pools où les écarts sont presque toujours sous 2 points, **le nom affiché pendant la draft dépend surtout de l'ordre de saisie**. À 5 adversaires, l'effet disparaît (`index.html:844`).

### 3.6 Surajustement au profil d'origine

**[Code] Ce qui est écrit pour les 4 champions d'origine :**

- le tableau des matchups et la sécurité sans adversaire connu ;
- des textes écrits pour eux dans les règles (`draft-engine.js:232,248,252,271`), `PICK_STYLES` (`:438-443`), `pickThreats` (`:533-538`), `winCondition` (`:561-570`) et `buildPlan` (`:572-625`).

**Traits rares, taillés pour eux** (comptés sur les fiches) :

- `q` : 2 champions, Xerath et Teemo ;
- `o` : 3 champions ;
- `w` : 5 champions ;
- `k` : 44 champions. Ce bonus a été pensé pour Kog'Maw, selon l'audit existant.

**[Mesuré] Le pool d'origine est le seul où les « choix nets » sont majoritaires :**

| Pool | Choix net (hors égalités) |
|---|---|
| Mid d'origine | 51,1 % |
| Jungle (Cho'Gath) | 76,2 % |
| Top | 19,5 % |
| Mid hors tableau | 4,1 % |
| Jungle classique | 0 % |
| Support | 0 % |

Le pool Jungle (Cho'Gath) fait exception. Ses choix nets viennent du bonus frontline, déjà mis en cause par l'audit.

**[Hypothèse] Les autres rôles sont mal couverts.** Hors du mid, le moteur départage surtout avec des fiches pauvres et un atout fixe. Il le dit honnêtement (« Choix serré », « Égalité »), mais il aide peu à choisir.

---

## 4. Améliorations proposées (aucune appliquée)

Impact et complexité sont des estimations. **Aucune proposition ne touche à la difficulté des champions ni à l'historique personnel. Le modèle A reste le seul actif.** Les propositions marquées (*) changent des scores ou le classement. Elles demandent une décision et une relecture à l'aveugle sur de vraies drafts.

| # | Proposition | Impact | Complexité | Risque de régression | Comment le mesurer |
|---|---|---|---|---|---|
| P1 | Conseils : quand le pick affiché n'est pas le 1er, prendre le 1er comme rival et dire « maintenu en attendant la suite de la draft ». Ne plus écrire « passe devant » (`draft.close`, `draft.conflict`) | Élevé (fidélité, 20 % des étapes au mid) | Faible | Fréquence des conseils modifiée. Tests de conseils à adapter | Nouveau test : pick maintenu derrière le 1er, aucun texte « passe devant ». Rejouer `etude-maintien.html` : 0 % attendu |
| P2 | Égalités : dire « départagés par le risque le plus faible » quand c'est le cas (`index.html:1691`, `draft-coaching.js:129`) | Moyen | Faible | Aucun sur les scores | Test : égalité dont les risques diffèrent, le texte nomme le risque |
| P3 | Afficher « Frontline allié supposé » seulement si la préférence est cochée (`index.html:1695`) | Faible | Très faible | Aucun | Test d'interface, préférence décochée |
| P4 | Recalculer `whenBetter`, `V.without`, `midSwapEffect` et l'indicateur « provisoire » avec les mêmes scénarios et la préférence de frontline réelle | Moyen | Moyenne | Textes « passerait devant » modifiés. Coût de calcul plus élevé | Drafts figées : compter les textes qui contredisent le classement, avant et après |
| P5 | Textes des scénarios : séparer les hypothèses favorables et défavorables. Ne jamais placer une note négative parmi les raisons positives. Nommer l'hypothèse principale | Moyen | Faible | Textes seulement | Rejouer l'étude : 1,5 % → 0 % de « À éviter » parmi les raisons positives |
| P6 | Pools mixtes : signaler à l'écran « matchup évalué seulement pour X, Y ». Sans changer le score | Moyen | Faible | Aucun sur les scores | Test : pool mixte, message présent |
| P7 (*) | Pools mixtes : neutraliser le biais du tableau. Par exemple, centrer la note de matchup sur la moyenne du pick, ou ne la compter que si tous les picks comparés en ont une | Élevé pour les pools mixtes | Moyenne | Change le 1er dans environ 70 % des drafts d'un pool mixte (borne : retirer le critère change 71 %). Le pool d'origine est aussi touché | Étude de sensibilité, puis relecture à l'aveugle sur le relevé |
| P8 (*) | Une seule échelle pour les matchups : même note pour « à éviter » selon la source, et pas de mélange tableau / bilan entre picks d'une même draft | Moyen, quand des bilans existeront | Moyenne | Change les drafts avec bilans partiels | Cas SYNTHÉTIQUES dans les tests, puis bilans réels |
| P9 (*) | Adoucir les seuils : note progressive (1 plongeur = moitié de 2), plus de marche au 4e adversaire connu | Moyen à élevé sur la stabilité | Moyenne | Beaucoup de tests du moteur écrits sur les seuils actuels. Nombreux changements de nom | Taux de changement après une substitution (§ 3.1), sauts d'au moins 2 points, tests du moteur |
| P10 (*) | Frontline « sans mobiles », déjà proposée par l'audit | Moyen | Faible | 6,8 % de changements au mid (audit) | Protocole déjà décrit dans `AUDIT-MOTEUR.md` |
| P11 | Pression sur la carte : la présenter comme un « atout constant », et non comme une raison liée à la draft. En option (*), ne s'en servir que pour départager | Moyen (explications), élevé si le classement change | Faible / moyenne | Option (*) : 58 % de changements en Jungle classique | Retirer le critère dans l'étude (§ 3.3) |
| P12 | Compléter les 8 fiches sans règle active et relire les fiches pauvres (liste de l'audit). Relecture par un joueur, sans statistique inventée | Moyen pour les rôles hors mid | Moyenne (contenu) | Classements de ces champions modifiés | Part en tête par pool, « jamais conseillé » (§ 3.4) |
| P13 | Bilans de matchups par rôle : lancer le calcul local pour un rôle hors mid, puis relire | Élevé (le critère le plus lourd devient actif) | Faible côté code, temps de calcul | Changements nombreux et légitimes | Comparer avant et après sur drafts figées. Relecture à l'aveugle |
| P14 (*) | Picks alliés : entrée « alliés » dans `analyze`, avec frontline allié déduit au lieu d'une préférence. Plus tard, synergies simples. À vérifier contre les règles de Riot : toujours présenter plusieurs options | Élevé | Élevée (lecture, interface, règles) | Toute la chaîne. Risque de sur-dicter le choix | Relevé réel, relecture à l'aveugle, taux d'égalité et de choix serré |
| P15 | Ajouter l'étude de sensibilité à la suite d'outils, en rapport et non en test bloquant : substitution, critère retiré, ±25 %, maintien du nom | Moyen (détection des régressions) | Faible | Aucun | Comparer les chiffres d'une version à l'autre |
| P16 | Séparer le seuil « choix net » du seuil de maintien du nom (`HOLD_GAP`), pour pouvoir régler l'un sans l'autre. Même valeur au départ | Faible | Faible | Aucun à valeur égale | Tests existants inchangés |
| P17 | Relevé de vraies drafts : en collecter assez (une seule ligne aujourd'hui) avant toute proposition (*) | Préalable à P7-P14 | Faible (code), long (collecte) | Aucun | Nombre de drafts relevées, relecture à l'aveugle |

**Ordre conseillé :**

1. Corrections de fidélité sans effet sur les scores : P1, P2, P3, P5, P6, P4.
2. Outils de mesure : P15, P17.
3. Contenu et données : P12, P13.
4. Seulement ensuite, les changements de calcul (*) : P10, P7, P8, P9, P11, P14.

## 5. Fichiers que ces propositions toucheraient

| Fichier | Propositions |
|---|---|
| `web/draft-coaching.js` | P1, P2, P5 (textes repris), P11 |
| `web/draft-coaching-tests.html` | P1, P2 |
| `web/index.html` (`computeVerdict`, onglets Analyse et Comparer, égalités) | P2, P3, P4, P6, P11, P14, P16 |
| `web/draft-engine.js` (`RULES`, `evaluateScenarios`, `whenBetter`, `midSwapEffect`, `positiveReasons`, `HOLD_GAP`, `CRITERIA`) | P4, P5, P7, P8, P9, P10, P11, P14, P16 |
| `web/draft-tests.html`, `web/draft-ties-tests.html` | P5, P7-P11, P16 |
| `web/draft-champions.js` | P10 (selon la variante), P12 |
| `web/draft-data.js` | P8 (bilans), P13 (description des sources) |
| `web/draft-providers.js` et code C# de lecture (`src/`) | P14 |
| `tools/` (nouveau script ou reprise de `docs/moteur/recommandation/run-etude.ps1`), `tools/experiments/` | P15 |
| `docs/technique/MOTEUR.md`, `docs/AUDIT-MOTEUR.md`, `docs/COACHING.md` | P1, P2, P7-P11 |

**Conflits probables :**

- `web/index.html` et `web/draft-coaching.js` sont partagés avec un chantier « interface » ou « conseils ».
- `web/draft-engine.js` est partagé avec tout chantier sur les poids ou les données.
- `web/draft-mastery.js` n'est touché par **aucune** proposition.

## 6. Limites de cette étude

- Les drafts simulées reproduisent les fréquences par rôle, pas les vraies compositions. Aucune victoire n'est mesurée.
- Les chiffres disent ce qui bouge, pas ce qui est juste.
- Les pools hors mid sont ceux de l'audit, pas des pools réels de joueurs.
- La sécurité sans adversaire connu n'est pas mesurée : les drafts complètes ont toujours un adversaire direct supposé.
- Le chemin « bilans calculés » n'est testé que sur un cas SYNTHÉTIQUE.

## Résumé

1. Le score additionne 9 critères à seuils (poids 3 / 2 / 2 / 2 / 1,5 / 1,5 / 1 / 1 / 0,5), sans normalisation. Le seuil de 2 points sert à la fois au « choix net » et au maintien du nom affiché.
2. Le moteur ne connaît pas les alliés : aucune synergie, et le frontline allié est une simple préférence. Les dégâts AP/AD n'entrent pas dans le score.
3. Le matchup (poids 3) n'existe que pour le mid et 4 champions. Ailleurs, l'adversaire direct ne change aucun score, et les pools mixtes sont biaisés : Syndra et Orianna ne sont jamais conseillées.
4. Les classements sont fragiles. Remplacer un seul adversaire change le 1er dans 13 à 27 % des drafts, surtout par des seuils franchis. ±25 % sur un poids change le 1er dans jusqu'à 20 % des drafts.
5. L'ordre du pool n'a plus aucun effet. L'ordre de saisie décide du nom affiché dans environ la moitié des drafts incomplètes pour certains pools, comme prévu par le maintien.
6. Explications : le conseil « choix serré » peut dire « passe devant » pour un pick classé derrière (20 % des étapes au mid). Les messages d'égalité taisent le départage par le risque. Les textes de scénarios se mélangent.
7. Priorité : des corrections de fidélité sans effet sur les scores (P1-P6), puis des mesures (P15, P17). Les changements de calcul viennent ensuite, après relecture sur de vraies drafts.
