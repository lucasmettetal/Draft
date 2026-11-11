# Référence chiffrée du moteur stratégique : DRAFT 2.16.1

Moteur `ENGINE_VERSION = "2.2"`, dépôt `main` en `78519fb`. Mesures du 10 novembre 2026 avec `tools/run-engine-eval.ps1 -AvecEvaluation -Generer` (banc 1.1).

> **Tous les chiffres de cette page viennent de scénarios SYNTHÉTIQUES.** Les drafts sont inventées et les réponses attendues sont écrites à la main par un seul annotateur, sans avis d'experts. Ces chiffres décrivent le comportement du moteur sur ces scénarios. Ils ne disent rien de la qualité des conseils en vraie partie.

Conditions : données livrées dans le dépôt seulement (pas de `draft-role-rates-live.js`, pas de bilans de matchups calculés, pas de dossier `%LOCALAPPDATA%\DraftMid`), frontline alliée supposée, aucune correction manuelle. Dans ces conditions, la confiance ne dépasse jamais « moyenne ».

À quoi sert cette page : une PR qui touche le moteur compare ses chiffres à ceux-ci, et joint le rapport de différences du banc (voir [VALIDATION.md](VALIDATION.md#banc-dévaluation-du-moteur)).

## 1. Jeux de scénarios

| Jeu | Scénarios | Empreinte SHA-256 (texte LF) | Usage |
|---|---|---|---|
| Développement | 312 (252 d'origine + 60 « flex révélé seul ») | `9054133C…D01E6F` | Toute mise au point |
| Évaluation gelé | 108 | `14B66E79…5844D5` | Jalons de version seulement |

*Scénarios SYNTHÉTIQUES.* Séparation contrôlée à chaque exécution : 0 identifiant commun, 0 draft identique (mêmes adversaires, même pool, même rôle). Le générateur, lancé sans aucun fichier du moteur, reproduit le jeu gelé à l'identique.

## 2. Rôles adverses et adversaire direct

*Scénarios SYNTHÉTIQUES.*

| Mesure | Développement (312) | Évaluation gelé (108) |
|---|---|---|
| Rôle juste, argmax / MAP | 97,4 % / 98,4 % (1 026 champions) | 96,2 % / 97,0 % |
| Niveau `evident` | 99,5 % (936) | 99,2 % (360) |
| Niveau `elimination` | 91,9 % (62) | 85,2 % (27) |
| Niveau `hors_role_par_construction` | 39,3 % (28) | 33,3 % (12) |
| Affectation complète juste | 94,9 % (157 drafts) | 90,9 % (66 drafts) |
| Brier / ECE | 0,0453 / 0,075 | 0,0677 / 0,0769 |
| Adversaire direct : juste / faux / abstention | 96 % / 0 % / 4 % (201) | 95,1 % / 1,2 % / 3,7 % (81) |
| Personne de nommé quand l'adversaire n'est pas révélé | 73 / 73 | 22 / 22 |
| Confiance « élevée » là où elle est interdite | 0 / 89 | 0 / 13 |

Précision par rôle annoté, développement : top 99,5 %, jungle 97 %, mid 94,7 %, adc 99,1 %, support 96,5 %.

### Champion flex révélé seul (ajout V2.17, développement seulement)

*Scénarios SYNTHÉTIQUES.* 12 champions (Sylas, Lux, Kog'Maw, Pantheon, Karma, Galio, Gragas, Swain, Lucian, Seraphine, Taliyah, Ziggs) × 5 rôles du joueur = 60 scénarios. Un seul adversaire est connu.

| Cas | Résultat |
|---|---|
| Le rôle du joueur fait partie des rôles plausibles (27) | Nommé comme adversaire direct dans **10 cas**, toujours en confiance « moyenne » ; 17 cas en confiance « faible », sans nom |
| Le rôle du joueur n'est pas plausible (33) | **0 nommé** à tort |

Exemples nommés : Sylas pour un joueur mid, Lux et Karma pour un joueur support, Kog'Maw et Lucian pour un joueur adc, Ziggs pour un joueur adc (alors que l'annotateur le place d'abord mid). La probabilité affichée vaut alors 0,61 (0,80 pour Kog'Maw adc). Aucune attente n'est fixée dans ces cas : c'est une mesure pour régler le seuil prévu en V2.18 (PR 18-1), pas un échec.

## 3. Stabilité du classement

*Scénarios SYNTHÉTIQUES.*

| Indicateur | Développement | Évaluation gelé |
|---|---|---|
| Déterminisme (même entrée, deux fois) | 312 / 312 | 108 / 108 |
| Déterminisme global (deux passages complets) | 0 écart | — |
| Ordre du pool (4 permutations) | 1 248 / 1 248 | 432 / 432 |
| Ordre des adversaires (4 permutations) : classement / rôles | 972 / 972 et 972 / 972 | 348 / 348 et 348 / 348 |
| Exclure un champion hors du pool | 312 / 312 | 108 / 108 |
| Favori sur un pick sans égalité | 235 / 235 | 78 / 78 |
| Égalités attendues (fiches identiques) | 21 / 21 | 9 / 9 |
| Pick pris par l'adversaire bien absent | 7 / 7 | 3 / 3 |
| Monotonie, plongeur ajouté (pick `i`) : mid fixé / chemin complet | 1 / 124 et 0 / 124 | 0 / 26 et 0 / 26 |
| Monotonie, tank ajouté (pick `k`) | 0 / 127 et 0 / 127 | 0 / 27 et 0 / 27 |
| Monotonie, cible fragile ajoutée (pick `h`) | 0 / 76 et 1 / 76 | 0 / 22 et 0 / 22 |
| Remplacement par un champion de même archétype : 1er choix changé | 10 / 156 (6,4 %), Kendall 0,049 | 4 / 68 (5,9 %), Kendall 0,04 |

Les deux violations de monotonie connues : IN-041 (Xerath -0,69 → 1,31 quand on ajoute Zed, à mid fixé : bonus de la règle `peel`, voir [ETUDE-PEEL-MAP.md](../moteur/ETUDE-PEEL-MAP.md)) et IN-085 (Fizz 0,18 → -0,19 quand on ajoute Jinx, chemin complet : effet du mélange d'hypothèses). Elles ne font pas échouer le banc : ce sont des effets de conception à arbitrer.

### Sensibilité aux poids (±25 % par critère, en mémoire)

*Scénarios SYNTHÉTIQUES, jeu de développement (312).* Changement de tête = le 1er choix (avec ses ex æquo) n'est plus le même.

| Critère | Poids | +25 % | -25 % | Rôle le plus touché |
|---|---|---|---|---|
| `matchup` | 3 | 0 (0 %) | 0 (0 %) | — |
| `blind` | 1 | 1 (0,3 %) | 1 (0,3 %) | mid 1,5 % |
| `targets` | 1,5 | 6 (1,9 %) | 7 (2,2 %) | mid 4,6 % / 6,2 % |
| `front` | 2 | 5 (1,6 %) | 6 (1,9 %) | adc 5,3 % à -25 % |
| `dive` | 2 | 3 (1 %) | 3 (1 %) | adc 3,5 % |
| `control` | 2 | 4 (1,3 %) | 4 (1,3 %) | mid 3,1 % à -25 % |
| `range` | 1 | 0 (0 %) | 3 (1 %) | support 4,8 % à -25 % |
| `peel` | 1,5 | 1 (0,3 %) | 1 (0,3 %) | — |
| `map` | 0,5 | 2 (0,6 %) | 2 (0,6 %) | — |

Ces taux sont plus bas que ceux de l'étude de recommandation (8 à 20 %) : les pools du banc sont plus petits (2 à 5 champions tirés au hasard) et ses drafts ne suivent pas les taux de jeu réels. Les deux mesures ne se comparent pas directement.

### Rapport de différences (démonstration)

*Scénarios SYNTHÉTIQUES.* Variante fictive en mémoire : poids de `front` de 2 à 1,5. Résultat : 1er choix changé dans 5 scénarios sur 312, ordre dans 9, scores dans 76. Déclarée « front », les 76 différences sont attendues. Sans déclaration, les 76 sont signalées INATTENDUES et le banc échoue.

## 4. Instantané de référence

`tests/moteur/instantanes/instantane-moteur-2.2-main-78519fb.json` : pour chacun des 420 scénarios, l'adversaire direct, la confiance, les hypothèses, les probabilités de rôle, le classement, les scores, l'impact de chaque critère et les égalités.

- Les 360 scénarios d'origine donnent **0 différence** avec l'instantané de l'étude (`etude/moteur-v217`, `b097c5d`).
- Les 60 scénarios « flex révélé seul » ont été ajoutés avec le même moteur.
- `tools/run-engine-eval.ps1 -AvecEvaluation` contre cet instantané : **0 différence** sur 312 + 108 scénarios.

Ce n'est pas une vérité : il dit seulement ce qui change d'une version à l'autre.
