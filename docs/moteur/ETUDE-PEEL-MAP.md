# Décisions produit à préparer pour V2.18 : règle `peel` et critère `map`

*10 novembre 2026, moteur 2.2 (`main` 78519fb, DRAFT 2.16.1). Aucune formule livrée n'a été modifiée : chaque variante est calculée **en mémoire** dans le banc (`tests/moteur/banc-etudes.js`), puis la valeur d'origine est remise. `web/draft-engine.js` n'est jamais écrit.*

> **Tous les chiffres viennent du jeu de développement SYNTHÉTIQUE (312 scénarios).** Les drafts sont inventées, les menaces ajoutées sont choisies par l'annotateur. Ces chiffres montrent l'effet de chaque variante sur le classement. Ils ne disent pas quelle variante donne de meilleurs conseils en vraie partie. Le jeu d'évaluation gelé n'a **pas** servi.

Reproduire :

```powershell
.\tools\run-engine-eval.ps1 -Etudes
```

Les résultats complets sont dans la clé `etudes` du fichier `resultats-*.json` du dossier de travail. Un contrôle vérifie que la copie de la règle `peel` utilisée par le banc (variante A0) redonne exactement la règle livrée sur les 312 scénarios.

## A. Bonus « Peu de plongeurs et de mobilité » (règle `peel`)

### Règle actuelle

`web/draft-engine.js`, `RULES.peel`, poids 1,5. Pour un pick dont la fiche porte `q` (craint la mobilité, par exemple Xerath ou Teemo), avec un frontline allié supposé :

- au moins 3 adversaires connus, **au plus 1 plongeur** et au plus 1 champion mobile → note +4/3, soit **+2 points** ;
- 3 mobiles ou plus → -4/3 ; sans frontline et 2 mobiles → -2.

Le 1er plongeur ne retire pas le bonus. S'il est le 3e adversaire connu, il le **déclenche** : c'est le cas IN-041.

### Comportement mesuré (variante A0 = règle livrée)

*Scénarios SYNTHÉTIQUES.*

- Le bonus se déclenche pour 7 picks, dans 7 scénarios sur 312.
- Expérience de monotonie : on ajoute **chacun** des 7 plongeurs évidents de l'annotateur (Zed, Kha'Zix, Talon, Rengar, Evelynn, Qiyana, Naafiri) à chaque draft incomplète. On vérifie les picks `i` ou `q` : 876 vérifications.
- À adversaire direct fixé : **7 violations**, toutes dans un seul scénario, IN-041 (Yunara, Sejuani ; pool mid d'origine). Xerath passe de -0,69 à +1,31, quel que soit le plongeur ajouté.
- Sur le chemin complet (comme l'interface) : 18 violations, dont **3 hausses d'au moins 1 point**, toutes dues au bonus.

### Matchup direct ou menace globale

C'est la distinction utile. Sur les 7 violations d'IN-041 :

| Plongeur ajouté | Devient l'adversaire direct ? | Xerath, chemin complet | Lecture |
|---|---|---|---|
| Zed, Talon, Qiyana, Naafiri (4) | Oui (mid) | Zed : -0,69 → -1,45 | Le tableau de matchups compense : pas de hausse visible |
| Kha'Zix, Rengar, Evelynn (3) | Non (jungle) | -0,69 → **+1,55**, +1,47, +1,55 | **Rien ne compense** : un jungler plongeur fait monter Xerath de plus de 2 points |

Le paradoxe vient donc surtout de la **menace globale** (un plongeur hors de la lane), pas du matchup direct. Le cas IN-041 publié (Zed) est le moins grave : sur le chemin complet, Zed devient le mid probable et le matchup corrige.

Sans le bonus, les 18 violations du chemin complet restent, mais **toutes sous 1 point** : elles viennent du critère `blind` et du mélange d'hypothèses (même famille qu'IN-085). Par exemple Kog'Maw -0,79 → -0,60, ou Xerath -0,69 → -0,45 avec Kha'Zix (variante A4).

### Alternatives comparées (en mémoire)

| Variante | Règle du bonus |
|---|---|
| A0 actuel | +4/3 si au moins 3 connus, au plus 1 plongeur, au plus 1 mobile |
| A1 zéro plongeur | +4/3 seulement s'il n'y a **aucun** plongeur connu (reste inchangé) |
| A2 progressif | 4/3 × min(1, adversaires calmes / 4), divisé par 2 avec 1 plongeur, nul au-delà. Plus de seuil à 3 connus |
| A3 menace globale | Plongeurs et mobiles comptés **hors adversaire direct** (le matchup s'en charge). Bonus si 0 plongeur hors lane |
| A4 sans bonus | Le bonus disparaît, les pénalités restent |

*Scénarios SYNTHÉTIQUES, jeu de développement (312).*

| Variante | Bonus déclenchés | 1er choix changé vs actuel | Ordre changé | Violations à mid fixé | Violations chemin complet (hausses ≥ 1 pt) | Hausses ≥ 2 pts à l'ajout d'un plongeur | IN-041, Xerath + Zed : mid fixé / chemin complet |
|---|---|---|---|---|---|---|---|
| A0 actuel | 7 | — | — | **7** (1 scénario) | 18 (**3**) | 344 | -0,69 → 1,31 / -0,69 → -1,45 |
| A1 zéro plongeur | 3 | 1 (0,3 %) | 3 | 0 | 18 (0) | 341 | -0,69 → -0,69 / -0,69 → -3,45 |
| A2 progressif | 11 | 2 (0,6 %) | 3 | 0 | 12 (0) | 341 | 0,31 → -0,19 / 0,31 → -2,95 |
| A3 menace globale | 4 | 1 (0,3 %) | 3 | 0 | 18 (0) | 341 | -0,69 → -0,69 / -0,69 → -1,78 |
| A4 sans bonus | 0 | 1 (0,3 %) | 5 | 0 | 18 (0) | 341 | -0,69 → -0,69 / -0,69 → -3,45 |

« Hausses ≥ 2 pts » compte tous les picks et tous les critères (3 600 ajouts). La différence entre A0 (344) et les autres (341) correspond aux **3 sauts de +2 points** dus au bonus. Les sauts dans les deux sens (≥ 2 points en valeur absolue) vont de 588 (A4) à 602 (A1). Ils viennent surtout de la règle `dive` (2e plongeur) et ne dépendent pas de cette décision.

Le seul 1er choix qui change (A1, A3, A4) : **IN-102**, joueur top, adversaires Taliyah, Jhin, Nasus, Zed. Teemo est 1er aujourd'hui grâce au bonus, alors que Zed est présent. Sans le bonus, Gnar et Gragas passent devant, ex æquo. A2 change aussi FS-036 (Swain seul, joueur top : Sett et Teemo ex æquo → Teemo seul).

### Recommandation pour V2.18 (à décider)

1. **A1, zéro plongeur.** C'est le plus petit changement qui supprime toutes les violations liées au bonus : 1 changement de 1er choix sur 312, aucune hausse d'au moins 1 point. Et le texte affiché (« Peu de plongeurs ») devient exact.
2. A3 (menace globale) répond bien à la question posée, mais il donne les mêmes chiffres qu'A1 ici. Il garde le bonus quand le plongeur est l'adversaire direct, en comptant sur le matchup. Or le tableau de matchups ne couvre que le mid et 4 picks : dans les autres rôles, un plongeur en face ne serait plus compté du tout. **À écarter tant que les matchups par rôle (PR 18-4) n'existent pas.**
3. A2 (progressif) supprime aussi le seuil de 3 adversaires connus. Mais il modifie plus de scores (11 bonus au lieu de 7) et il est plus dur à expliquer. À garder si tu veux traiter tous les effets de seuil en une seule fois (P7 à P11).
4. A4 est l'option prudente si le bonus n'a pas de justification de jeu claire.

Toute PR qui l'applique doit déclarer `-Criteres peel` et lister IN-102 comme changement attendu.

## B. Critère « Pression sur la carte » (`map`)

### Définition (lue dans `web/draft-engine.js`)

- `CRITERIA` : `{ key:"map", label:"Pression sur la carte", weight:.5, source:"Atout propre au champion. Poids faible, car il ne dépend pas de la draft." }`
- `RULES.map` : trait `g` +1 (« Peut peser sur toute la carte »), `r` +0,5 (« Peut roamer une fois sa vague poussée »), `n` -0,5 (« Peu d'impact hors des combats groupés »). Contribution entre -0,25 et +0,5 point.
- Il ne lit **jamais** les adversaires : il départage des picks sur une qualité fixe du champion. Entre deux picks, il pèse au plus 0,75 point (`g` contre `n`).

### Impact par rôle

*Scénarios SYNTHÉTIQUES, jeu de développement (312).*

| Rôle | Scénarios | Pool avec `map` actif | `map` touche l'écart 1er / 2e | 1er choix changé si `map` est retiré | `map` dans le score du 1er (part moyenne) |
|---|---|---|---|---|---|
| Top | 62 | 50 % | 33,9 % | 12 (19,4 %) | 18,3 % |
| Jungle | 66 | 45,5 % | 27,3 % | 11 (16,7 %) | 11,2 % |
| Mid | 65 | 89,2 % | 73,8 % | 12 (18,5 %) | 21,3 % |
| ADC | 57 | 10,5 % | 8,8 % | 2 (3,5 %) | 0,1 % |
| Support | 62 | 54,8 % | 40,3 % | 16 (25,8 %) | 20,9 % |
| **Total** | 312 | | | **53 (17 %)** | |

### Quand décide-t-il ?

Sur les 53 drafts où retirer `map` change le 1er choix, on mesure l'avance que l'autre pick avait sur les critères qui dépendent de la draft :

| Avance hors carte renversée par `map` | Drafts |
|---|---|
| 0 : égalité parfaite hors carte | **51** |
| Entre 0 et 0,25 point | 0 |
| Entre 0,25 et 0,5 point | 2 |
| 0,5 point ou plus | 0 |

Les 2 cas où la carte passe devant un avantage de draft :
- CE-069, joueur ADC, adversaires Rell, Anivia, Nasus, Rek'Sai, Samira : Corki (carte 0) passe devant Kog'Maw (carte -0,25), qui avait 0,25 point d'avance.
- FS-028, joueur mid, Galio seul : Twisted Fate (carte +0,5) passe devant Kog'Maw (carte -0,25), qui avait 0,36 point d'avance.

**En Jungle et en Support, les 27 changements sont tous des égalités parfaites hors carte.** Dans ce jeu, la carte n'y écrase aucun avantage de draft. Son influence est forte en fréquence (16,7 % et 25,8 % des 1ers choix), mais elle ne joue que le rôle d'un **bris d'égalité caché dans le score**.

Le chiffre de la synthèse (« 58 à 70 % des drafts en Jungle et en Support ») vient d'une autre étude : pools fixes et drafts tirées selon les taux de jeu locaux. Ici, `map` touche l'écart entre le 1er et le 2e dans 27,3 % (Jungle) et 40,3 % (Support) des drafts. Les deux mesures montrent la même chose : la carte départage souvent, parce que les critères de draft sont souvent à égalité. Aujourd'hui, 115 drafts sur 312 ont déjà une égalité en tête ; sans `map`, il y en aurait 156.

### Variantes de poids (en mémoire)

*Scénarios SYNTHÉTIQUES, jeu de développement (312).*

| Variante | 1er choix changé vs actuel | Ordre changé | Égalités en tête (115 aujourd'hui) | Par rôle |
|---|---|---|---|---|
| Retiré (poids 0) | 53 (17 %) | 58 | 156 | top 19,4 %, jungle 16,7 %, mid 18,5 %, adc 3,5 %, support 25,8 % |
| Poids 0,25 | 2 (0,6 %) | 6 | 114 | mid 1, adc 1 |
| Poids 0,375 (-25 %) | 2 (0,6 %) | 2 | 114 | mid 1, adc 1 |
| Poids 0,625 (+25 %) | 2 (0,6 %) | 2 | 113 | mid 1, adc 1 |
| Poids 1 | 4 (1,3 %) | 15 | 112 | mid 2, adc 1, support 1 |
| Bris d'égalité seulement (poids 0,001) | 3 (1 %) | 7 | 114 | mid 2, adc 1 |

« Bris d'égalité seulement » est simulé avec un poids de 0,001 : la carte ne peut plus renverser aucun écart de draft, mais elle sépare encore les égalités. Dans une vraie PR, il faudrait l'ajouter au tri de `analyze`, à côté du « moins pénalisé » et du favori. Les picks resteraient alors marqués ex æquo, et l'explication dirait « départagé par la pression sur la carte ».

### Recommandation chiffrée pour V2.18 (à décider)

1. **Recommandé : bris d'égalité seulement.** 51 des 53 décisions actuelles (96 %) sont déjà des bris d'égalité. Le changement ne touche que 3 1ers choix sur 312 (1 %), dont les 2 cas où la carte passe devant un avantage de draft. L'explication devient fidèle : le pick n'est pas « meilleur » de 0,25 point, il est à égalité et départagé. Il faut choisir sa place dans l'ordre des départages (avant ou après « le moins pénalisé ») et l'afficher.
2. **Variante minimale : poids 0,25.** 2 changements sur 312 (0,6 %). La carte reste dans le score, mais elle ne peut plus renverser plus de 0,375 point. C'est plus simple, mais l'explication reste « un écart » là où il n'y en a pas.
3. **Inchangé (0,5) : acceptable.** Aucun cas ne renverse un avantage d'au moins 0,5 point. Le poids actuel n'est donc pas « excessif » au sens du score. Il l'est seulement dans la présentation, puisqu'il transforme des égalités en écarts.
4. **À écarter : retirer `map`.** Le 1er choix change dans 17 % des drafts, et les égalités en tête passent de 115 à 156, sans aucune information de draft en plus.

Toute PR qui l'applique doit déclarer `-Criteres map` et joindre le rapport de différences du banc.

## Limites

- Données SYNTHÉTIQUES, un seul annotateur. Les menaces ajoutées et les traits concernés (`i`, `q`) sont choisis par l'annotateur. Les picks touchés sont repérés par leurs traits du moteur.
- Le bonus `peel` ne concerne que les picks `q` : 7 déclenchements sur 312 scénarios. Les écarts entre variantes portent donc sur très peu de cas.
- Sans `draft-role-rates-live.js`, la confiance ne dépasse jamais « moyenne ». Les hypothèses d'adversaire direct sont plus étalées qu'avec de vraies données de rôles.
- Le bon réglage ne se décide pas ici : il faut de vraies drafts (PR 17-6) et des avis d'experts (`jugement_expert`, encore vide).
