# Moteur stratégique : synthèse des trois études et feuille de route V2.17 / V2.18

*Préparation du 10 novembre 2026, sur `main` en `78519fb` (DRAFT 2.16.1). Aucun fichier de production n'a été modifié et aucun poids n'a changé. Tous les chiffres viennent de **drafts SYNTHÉTIQUES** : ils décrivent le comportement du moteur, pas la qualité de ses conseils en vraie partie.*

| Rapport | Sujet | Outils reproductibles |
|---|---|---|
| [RAPPORT-ROLES.md](RAPPORT-ROLES.md) | Estimation des rôles adverses | `roles/run-etude-roles.ps1` |
| [RAPPORT-RECOMMANDATION.md](RAPPORT-RECOMMANDATION.md) | Score, explications, fragilité (17 propositions P1 à P17) | `recommandation/run-etude.ps1` |
| [RAPPORT-VALIDATION.md](RAPPORT-VALIDATION.md) | Protocole d'évaluation, 360 scénarios, banc | `evaluation/executer-banc.ps1` |

**Vérifié par le coordinateur dans le code :**
- `tools/run-accuracy-tests.ps1:21` copie `web/draft-role-rates.js`, fichier absent depuis le retrait de Meraki : l'outil est cassé ;
- les 9 poids (`web/draft-engine.js:121-131`) ;
- une égalité est un écart de moins d'un millionième (`draft-engine.js:323`) ;
- « Frontline allié supposé. » est affiché sans condition (`web/index.html:1695`) ;
- aucun fichier hors de `docs/moteur/` n'a été modifié par les agents.

## 1. Points d'accord

1. **Sur la mesure.** Il n'existe aujourd'hui aucune mesure fiable. L'outil de précision des rôles est cassé et ne couvre que le mid (rapports 1 et 3), et aucune victoire n'est mesurée (rapports 2 et 3). **Toute modification du score doit attendre un banc de mesure intégré.**
2. **Sur le principe des rôles.** Peser toutes les répartitions est le bon principe (rapport 1). Les rôles évidents sont justes à environ 99 % sur les scénarios synthétiques (rapport 3).
3. **Les faiblesses viennent surtout du contenu et des seuils**, pas du calcul :
   - fiches à un seul rôle (99 champions sur 171) ;
   - tableau de matchups limité au mid et à 4 champions ;
   - 8 fiches qui ne déclenchent aucune règle ;
   - effets de seuil (2e plongeur, 2e contrôle…).
4. **Les explications doivent d'abord être rendues fidèles au calcul.** C'est sans risque pour le classement :
   - le pick affiché derrière le 1er ;
   - les égalités attribuées à l'alphabet alors que le risque les départage ;
   - le frontline allié affiché alors que l'option est décochée ;
   - les rôles affichés qui peuvent contredire le calcul probabiliste.
5. **Les bans et l'ordre des picks adverses ne doivent pas servir.** Il n'existe aucune donnée autorisée pour les exploiter, et la lecture du client ignore volontairement les bans.

## 2. Contradictions et nuances

| Sujet | Rapport 1 ou 2 | Rapport 3 | Explication et action |
|---|---|---|---|
| Nommer un adversaire trop tôt | Sylas est affiché « Mid probable » avec un seul champion révélé (rapport 1) | Aucun adversaire nommé quand le direct n'est pas révélé (62 sur 62) | Les scénarios du rapport 3 ne contiennent pas ce cas de champion flex révélé seul. **Ajouter ce cas au jeu de développement** avant de régler le seuil |
| Fragilité du 1er choix | 13 à 27 % de changements quand on remplace un adversaire quelconque (rapport 2) | Environ 6 % quand on le remplace par un champion comparable | Les définitions diffèrent. Les deux mesures sont utiles : la seconde sert à repérer une instabilité anormale |
| Précision des rôles | 99 champions à un seul rôle, égalités exactes sur les champions flex (rapport 1) | 99 % sur les rôles évidents, 33 à 39 % hors rôle | Les deux sont vrais. L'annotateur du rapport 3 est proche de la fiche écrite, donc le 99 % est en partie **circulaire**. Il faut une base indépendante : les compositions réelles mises de côté par le calcul des rôles (rapport 1, point 8), à mesurer en local |
| Monotonie | — | Xerath monte quand on ajoute Zed (IN-041), Fizz baisse quand on ajoute Jinx (IN-085) | Le premier cas est un effet de seuil voulu par la règle `peel`, le second un effet du mélange d'hypothèses. **Décision produit requise** pour IN-041 (§6) |

## 3. Dépendances entre les chantiers

```
Banc d'évaluation (rapport 3) ──┬──> toute modification des rôles (rapport 1 : C, D, E, G)
Outil de rôles réparé (A)    ───┤
Base indépendante (holdout)  ───┘
Corrections de fidélité (P1 à P6) : indépendantes, et classement inchangé (instantané à 0 différence)
Collecte de vraies drafts (P17) et avis d'experts ──> changements de calcul (P7 à P11, P14)
Fiches enrichies (P12) et matchups par rôle (P13) ──> mesurables seulement avec le banc
```

## 4. Fichiers partagés : risques de conflit

| Fichier | Rôles (rapport 1) | Recommandation (rapport 2) | Évaluation (rapport 3) | Risque |
|---|---|---|---|---|
| `web/index.html` | F, G (affichage des rôles) | P1 à P6 (explications) | — | **Élevé** : à séquencer |
| `web/draft-data.js` | B, D, E (calcul et seuils) | P12, P13 | — | **Élevé** |
| `web/draft-engine.js` | E | P7 à P11, P14 | — | Moyen |
| `web/draft-coaching.js` | — | P1 à P5 | — | Moyen |
| `web/draft-role-sheet.js`, `draft-champions.js` | C | P12 | — | Moyen (données) |
| `tools/` | A (outil de rôles) | P15 | nouveau `run-engine-eval.ps1` | Faible |
| `docs/technique/MOTEUR.md` | I | Doc | Doc | Faible |

Règle : **une seule PR à la fois** sur `index.html` et `draft-data.js`. Les PR sur les données (fiches) restent séparées des PR sur le calcul.

## 5. Feuille de route

Chaque PR passe toutes les suites existantes (natifs, scénario complet, page, profils, conseils, égalités, maîtrise, moteur) et joint le **rapport de différences du banc** sur le jeu de développement. Le jeu d'évaluation n'est lancé qu'aux jalons de version.

### V2.17 — Mesurer et rendre fidèle (classement inchangé)

| PR | Contenu | Fichiers | Objectif mesurable |
|---|---|---|---|
| 17-1 | Intégrer le banc d'évaluation, les 360 scénarios et l'instantané ; script `tools/run-engine-eval.ps1` ; ajout des cas manquants (champion flex révélé seul, Sylas) | `tools/`, `docs/moteur/evaluation/` → dossier de tests | Le banc tourne en local et en CI ; **0 différence** avec l'instantané `78519fb` |
| 17-2 | Réparer la mesure des rôles (A), l'étendre aux 5 rôles et à la base de compositions mises de côté, en local | `tools/run-accuracy-tests.ps1` | Précision du 1er candidat, assignation complète, Brier, avec intervalles, pour les 5 rôles |
| 17-3 | Corriger la documentation du moteur (I) | `docs/technique/MOTEUR.md`, `DONNEES.md` | Plus aucune contradiction entre la documentation et le code |
| 17-4 | Fidélité des explications (P1 à P6) : pick affiché, cause des égalités, frontline allié, raisons d'hypothèses mélangées | `draft-coaching.js`, `index.html` | **0 différence de classement** ; 0 explication contradictoire sur les scénarios du banc (nouvelle assertion) |
| 17-5 | Rôles affichés tirés du calcul probabiliste (F), distinction entre « inconnu » et « pas encore choisi » (G) | `index.html`, `draft-data.js` | Classement inchangé ; rôles affichés égaux au calcul dans 100 % des scénarios |
| 17-6 | Collecte locale de vraies drafts (P17), avec ton accord et sans donnée d'autres joueurs | Relevé existant | Au moins 50 drafts réelles relevées avant toute PR V2.18 sur le calcul |

### V2.18 — Améliorer, mesure à l'appui

| PR | Contenu | Fichiers | Objectif mesurable (jeu de développement, puis évaluation au jalon) |
|---|---|---|---|
| 18-1 | Seuil pour nommer un adversaire selon le nombre de champions révélés (E) | `draft-data.js` | Moins d'adversaires nommés à tort, sans perte de plus de 2 points sur l'adversaire direct nommé juste |
| 18-2 | Question ciblée en cas d'ambiguïté, par exemple « Qui joue ADC ? » (B) | `index.html` | Au plus une question par draft ambiguë ; assignation complète juste après réponse |
| 18-3 | Fiches enrichies : rôles secondaires (C, D) et fiches pauvres (P12), validées sur la base mise de côté | `draft-role-sheet.js`, `draft-champions.js` | Hors rôle : de 33-39 % vers au moins 50 % ; rôles évidents à 99 % maintenus |
| 18-4 | Bilans de matchups par rôle (P13), au-delà du mid | `draft-data.js`, `draft-providers.js` | Au moins un rôle de plus couvert ; « à éviter » cohérent entre les sources (P9) |
| 18-5 | Changements de calcul décidés par toi : seuils `peel` et `front`, poids de `map`, lissage des effets de seuil (P7 à P11, P14) | `draft-engine.js` | Changements de 1er choix tous **déclarés** dans la PR ; aucun changement « inattendu » au banc ; relecture à l'aveugle positive sur les vraies drafts |

**Hors feuille de route :** synergies avec les picks alliés et équilibre AP/AD dans le score. Ils demanderaient de lire les picks alliés, à valider d'abord du point de vue des règles et de la stabilité.

## 6. Décisions qui te reviennent
1. **IN-041 :** faut-il garder le bonus `peel` qui se déclenche dès le premier plongeur parmi trois adversaires connus, ou le rendre progressif ?
2. **Poids de la « pression sur la carte » (0,5) :** il décide de l'écart entre le 1er et le 2e dans 58 à 70 % des drafts en Jungle et en Support, alors qu'il ne dépend pas de la draft.
3. **Collecte de vraies drafts (17-6)** et recherche d'avis d'experts pour les annotations.
4. **Ordre de priorité :** fidélité des explications (17-4) avant ou après l'affichage des rôles (17-5). Les deux touchent `index.html`.

## 7. Risques pour la stabilité
- **Raisonnement circulaire :** régler le moteur sur des annotations proches de la fiche écrite. Parades : une base indépendante, un jeu d'évaluation gelé et des avis d'experts.
- **Effets de seuil :** un réglage de ±25 % sur un poids change le 1er choix dans jusqu'à 20 % des drafts. Toute PR de calcul doit lister les changements attendus.
- **Conflits sur `index.html` et `draft-data.js` :** séquencer les PR (§4).
- **Données livrées :** sans calcul des rôles par l'utilisateur, la confiance ne dépasse jamais « moyenne ». Le niveau « élevé » n'est donc jamais testé avec les seules données livrées.
