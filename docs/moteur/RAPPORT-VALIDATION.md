# Protocole d'évaluation du moteur stratégique (volet 3)

Étude sur la branche `etude/moteur-v217`, copie de `main` 78519fb. Le moteur porte `ENGINE_VERSION = "2.2"`.

> **Toutes les données de ce dossier sont SYNTHÉTIQUES.** Les drafts sont inventées. Les annotations sont écrites à la main par l'auteur du protocole, sans validation d'experts. Les chiffres plus bas sont **mesurés sur ces scénarios synthétiques**. Ils ne disent rien de la qualité du moteur sur de vraies parties.

Aucun fichier existant n'a été modifié. Aucun appel réseau. Les exécutions ont eu lieu sous `%TEMP%\draft-etude-agent3`.

## 1. Ce qui est évalué

Le banc suit le même chemin que `computeVerdict` dans `web/index.html`, sans correction manuelle et avec la frontline alliée supposée (réglage par défaut) :

1. `opponentBelief` et `roleBelief` : répartition probable des rôles adverses.
2. `beliefScenarios` et `identifiedOpponent` : adversaire direct nommé ou non, avec un niveau de confiance.
3. `analyze` : classement du pool, scores et égalités.

Les données sont celles livrées dans le dépôt : rôles écrits à la main (`draft-role-sheet.js`), fiches (`draft-champions.js`), tableau de matchups mid. Sans fichier `draft-role-rates-live.js`, la confiance ne dépasse jamais « moyenne ». C'est voulu dans le moteur, et cela limite ce que mesure le banc.

## 2. Scénarios

**360 scénarios synthétiques**, générés par `evaluation/generer-scenarios.js` avec la graine 217. Le format est décrit dans `evaluation/schema-scenarios.json`. Chaque fichier et chaque scénario porte `"synthetique": true`. Le banc refuse un jeu sans ce champ.

| Catégorie | Nombre | Contenu |
|---|---|---|
| `complet_evident` | 90 | 5 adversaires, un champion évident par rôle |
| `complet_flex` | 60 | 40 avec un flex résolu par élimination, 20 avec deux flex (certains ambigus) |
| `atypique_hors_role` | 40 | Un choix rare (Vayne top, Kog'Maw mid…) dont le rôle habituel est déjà pris |
| `atypique_inconnu` | 10 | Locke ou Zaahen, sans avis de rôle, dans le seul rôle libre |
| `incomplet` | 120 | 1, 2, 3 ou 4 adversaires (30 de chaque), un tiers avec un flex |
| `egalite_attendue` | 30 | Deux picks du pool ont exactement la même fiche |
| `pick_pris` | 10 | Un champion du pool est pris par l'adversaire |

Les 5 rôles du joueur sont couverts à parts égales. Les pools ont de 2 à 5 champions. Environ un tiers des pools mid sont le pool d'origine (Twisted Fate, Fizz, Kog'Maw, Xerath), pour faire jouer le tableau de matchups. Le générateur vérifie que chaque nom existe dans `web/draft-champions.js`.

### D'où viennent les vérités de référence

Elles ne viennent **jamais** du score ou des sorties du moteur.

| Annotation | Source | Limite |
|---|---|---|
| Rôle `evident` | Tableaux `ARCHETYPES` et `EVIDENTS_HORS_ARCHETYPE`, écrits d'après une connaissance générale du jeu | Avis d'une seule personne. Proche, par nature, de `draft-role-sheet.js` : leur accord ne prouve rien sur de vraies parties |
| Rôle par `elimination` | Seul rôle libre compatible avec les rôles plausibles du tableau `FLEX` | Dépend des listes `FLEX`, discutables |
| `hors_role_par_construction` | Le scénario déclare le rôle joué | Vérité par définition du scénario, pas par observation |
| Adversaire direct | Déduit des rôles annotés. `non_revele` si aucun adversaire révélé ne peut jouer ce rôle | Aucune attente dans les cas ambigus |
| Égalité attendue | Propriété structurelle : deux picks à fiche identique, hors tableau de matchups, doivent avoir le même score | Ne dit pas lequel est le meilleur |
| Pick absent | Un champion pris par l'adversaire ne doit pas être conseillé | Aucune |
| `jugement_expert` | **Vide**. À collecter | Voir section 6 |

Deux annotations se contredisent déjà : Morgana est notée support évident, mais `HORS_ROLE` la déclare aussi jouable jungle. Le banc le fait ressortir (scénario CF-026). C'est la raison principale de faire relire les tableaux par plusieurs joueurs.

## 3. Jeu de développement et jeu d'évaluation gelé

- **Séparation reproductible.** Dans chaque catégorie, les identifiants sont triés par hachage FNV-1a de `"217:<id>"`. Les 30 % au plus petit hachage forment le jeu d'évaluation. Résultat : 252 scénarios de développement, 108 d'évaluation.
- **Gel.** Le fichier `scenarios-eval.json` est gelé au 2026-11-10. Son SHA-256 est inscrit dans `evaluation/manifeste.json`. Un hachage différent signale une modification.
- **Règle d'usage.**
  1. Tout réglage du moteur (poids, seuils, fiches, rôles) se justifie **uniquement** avec le jeu de développement.
  2. Le jeu d'évaluation se lance seulement à un jalon (fin de version), avec `-AvecEvaluation`. Chaque lancement est noté : date, version, résultat.
  3. Si un chiffre du jeu d'évaluation sert à décider un réglage, ce jeu est « usé ». On en génère un nouveau avec une autre graine, et on le note.
  4. On n'ajoute jamais un scénario d'évaluation au jeu de développement pour corriger un échec.

## 4. Métriques

### Attribution des rôles
- **Précision par champion annotée**, par rôle et par niveau d'annotation. Prédiction = rôle de plus forte probabilité marginale (`argmax`). Également mesurée avec l'affectation conjointe la plus probable (`MAP`).
- **Affectation complète juste** : sur les drafts de 5 adversaires entièrement annotées, les 5 rôles sont justes en même temps.
- **Adversaire direct** : nommé juste, nommé faux, ou abstention. Aussi : le moteur ne doit nommer personne si l'adversaire direct n'est pas encore révélé.

### Calibration
- **Score de Brier** sur les 5 probabilités de rôle de chaque champion annoté.
- **Erreur de calibration attendue (ECE)** sur 10 tranches de probabilité, avec le tableau de fiabilité.
- **Précision par niveau de confiance** affiché (faible, moyenne, élevée).
- Limite : les annotations portent surtout sur des cas évidents. La calibration mesurée ici est biaisée vers les cas faciles.

### Cohérence et stabilité du classement
- **Déterminisme** : deux exécutions identiques.
- **Invariance à l'ordre du pool** : 4 permutations par scénario.
- **Invariance à l'ordre des adversaires** : 4 permutations. Classement, adversaire direct, confiance et probabilités de rôle comparés.
- **Changement non pertinent** : exclure un champion hors du pool, ou nommer favori un pick sans égalité. Le classement ne doit pas bouger.
- **Monotonie** : sur les drafts incomplètes, on ajoute une menace évidente choisie par l'annotateur. Un plongeur ne doit pas faire monter un pick immobile (`i`). Un tank ne doit pas faire baisser un tueur de tanks (`k`). Une cible fragile ne doit pas faire baisser un assassin (`h`). Mesurée à adversaire direct fixé, puis sur le chemin complet.
- **Sensibilité à un remplacement comparable** : un adversaire est remplacé par un champion du même rôle et du même archétype annoté. Un changement du 1er choix n'est pas une erreur en soi. Un taux élevé signale un classement fragile.

### Changements entre deux versions
- **Taux de changement du 1er choix**, de l'ordre complet, des scores et de l'adversaire direct.
- **Taux de changements inattendus** : un changement d'ordre est « attendu » si seuls les critères que la PR déclare modifier ont bougé, sans changement de rôles ni d'adversaire. Sinon il est « inattendu ».

## 5. Non-régression par instantané

- `evaluation/instantanes/instantane-moteur-2.2-main-78519fb.json` contient les sorties actuelles pour les 360 scénarios : adversaire direct, confiance, hypothèses, probabilités de rôle, classement, scores, impact de chaque critère, égalités.
- **Ce n'est pas une vérité.** Il dit seulement ce qui change d'une version à l'autre. Un changement peut être une amélioration.
- À chaque PR qui touche `web/draft-engine.js`, `draft-data.js`, `draft-champions.js` ou `draft-role-sheet.js` :

```powershell
.\docs\moteur\evaluation\executer-banc.ps1 -Instantane .\docs\moteur\evaluation\instantanes\instantane-moteur-2.2-main-78519fb.json -Criteres front
```

  Le rapport donne les comptes de différences et jusqu'à 15 exemples, chacun avec les critères modifiés et la mention « attendu » ou « INATTENDU ». La PR explique chaque changement inattendu. Si elle est acceptée, on enregistre un nouvel instantané sous un nouveau nom, sans écraser l'ancien.
- Vérification faite : la version actuelle comparée à son instantané donne **0 différence sur 360 scénarios**.

## 6. Jugements d'experts à collecter

Le banc ne dit pas si le 1er choix est **bon**. Pour cela, il faut des avis humains, jamais le score du moteur :

1. Tirer 60 scénarios du jeu de développement, et à part 30 du jeu d'évaluation.
2. Montrer à 3 joueurs au moins (de préférence Émeraude et plus) la draft, le rôle et le pool, **sans** le conseil du moteur.
3. Chacun note : meilleur pick, picks acceptables, picks à éviter.
4. Remplir `jugement_expert` (forme dans le schéma) avec l'accord entre experts.
5. Métriques : le 1er choix du moteur est-il dans les picks acceptables ? Est-il un pick à éviter ? Mesurer seulement là où les experts s'accordent.

Ces avis restent des opinions. Le relevé sur de vraies drafts (`docs/technique/VALIDATION-REELLE.md`) reste la seule mesure en situation réelle.

## 7. Résultats mesurés (données synthétiques, 2026-11-10)

Banc exécuté dans Chrome headless. Résultats complets : `evaluation/resultats/mesures-synthetiques-2026-11-10.json`.

| Mesure | Développement (252) | Évaluation gelé (108) |
|---|---|---|
| Rôle juste, argmax / MAP | 97,4 % / 98,4 % | 96,2 % / 97,0 % |
| Niveau `evident` | 99,5 % (936) | 99,2 % (360) |
| Niveau `elimination` | 91,9 % (62) | 85,2 % (27) |
| Niveau `hors_role_par_construction` | 39,3 % (28) | 33,3 % (12) |
| Affectation complète juste | 94,9 % (157 drafts) | 90,9 % (66 drafts) |
| Brier / ECE | 0,045 / 0,075 | 0,068 / 0,077 |
| Adversaire direct : juste / faux / abstention | 96 % / 0 % / 4 % | 95,1 % / 1,2 % / 3,7 % |
| Personne de nommé quand l'adversaire n'est pas révélé | 40 / 40 | 22 / 22 |
| Invariances (pool, adversaires, préférences, déterminisme) | 100 % | 100 % |
| Égalités attendues respectées | 21 / 21 | 9 / 9 |
| Pick pris absent | 7 / 7 | 3 / 3 |
| Monotonie, violations | 1 / 212 à mid fixé, 1 / 212 en complet | 0 / 75 |
| Remplacement comparable : 1er choix changé | 6,4 % (156) | 5,9 % (68) |

Ce qu'on peut en lire, avec prudence :

- **Cas évidents : quasi parfaits.** C'est attendu : les annotations et la fiche de rôles du moteur sont deux avis écrits à la main, proches l'un de l'autre.
- **Choix hors rôle : le moteur se trompe souvent (33 à 39 %).** Avec des rôles écrits à la main, le plancher de probabilité est le même pour tous les rôles rares. Le moteur hésite donc entre « Vayne top et Jinx adc » et « Jinx top et Vayne adc ». Il reste prudent : confiance jamais « élevée » (0 violation sur 42 cas interdits). Seul cas faux nommé : HR-003, Kog'Maw mid, où le moteur désigne Tristana mid avec une confiance « moyenne ».
- **Calibration : sous-confiance au milieu de l'échelle.** Entre 0,6 et 0,8 de probabilité, le rôle prédit est juste 93 à 95 % du temps. Entre 0,4 et 0,5, environ 50 %. Ces chiffres ne valent que pour ces cas synthétiques.
- **Invariances : aucun écart.** L'ordre de saisie et l'ordre du pool n'ont aucun effet, ce qui confirme les tests existants (`draft-ties-tests.html`) sur un plus grand ensemble.
- **Une violation de monotonie réelle, à mid fixé (IN-041).** Pool mid d'origine, adversaires Yunara et Sejuani, mid inconnu. On ajoute Zed : Xerath passe de -0,69 à +1,31. Cause : la règle `peel` donne +2 (4/3 × 1,5) à un pick qui craint la mobilité (`q`) quand au moins 3 adversaires sont connus, avec au plus un plongeur et au plus un champion mobile. Zed est le 3e adversaire : il franchit le seuil de 3 tout en restant le seul plongeur. Le **premier** plongeur ajouté donne donc un bonus « Peu de plongeurs et de mobilité ». Sur le chemin complet, Zed devient le mid probable et le tableau de matchups compense. C'est un effet de seuil de conception, à arbitrer, pas une erreur de calcul.
- **Autre écart (IN-085)** : ajouter Jinx fait baisser Fizz (0,18 → -0,19) sur le chemin complet, car les poids des hypothèses d'adversaire direct changent. Effet attendu du mélange d'hypothèses, à documenter.
- **Démonstration du rapport de différences.** Une variante fictive, en mémoire seulement (poids « Tanks et bruisers » 2 → 1,5), change le 1er choix dans 5 scénarios sur 252 (2 %) et l'ordre dans 9. Déclaré « front », les 9 changements sont attendus. Sans déclaration, ils sont tous signalés inattendus (3,6 %).

## 8. Limites

- Données synthétiques, un seul annotateur, pas de vraies parties.
- Les menaces de la monotonie sont choisies par l'annotateur, mais les picks concernés sont repérés par leurs traits du moteur (`i`, `k`, `h`).
- Sans rôles calculés sur de vraies parties, la confiance « élevée » n'apparaît jamais. Il faut relancer le banc avec un `draft-role-rates-live.js` pour tester ce niveau.
- Les drafts complètes sont toujours « 5 rôles distincts, un par rôle ». Les échanges de lane ne sont pas couverts.

## 9. Fichiers écrits par cette étude

| Fichier | Rôle |
|---|---|
| `docs/moteur/RAPPORT-VALIDATION.md` | Ce rapport |
| `docs/moteur/evaluation/generer-scenarios.js` | Annotations écrites à la main, générateur, séparation dev / évaluation |
| `docs/moteur/evaluation/schema-scenarios.json` | Schéma JSON des scénarios |
| `docs/moteur/evaluation/scenarios-dev.json` | 252 scénarios de développement |
| `docs/moteur/evaluation/scenarios-eval.json` | 108 scénarios d'évaluation, gelés |
| `docs/moteur/evaluation/manifeste.json` | Graine, méthode, comptes, SHA-256 |
| `docs/moteur/evaluation/banc-evaluation.js` | Métriques, invariances, monotonie, comparaison d'instantanés |
| `docs/moteur/evaluation/banc.html` | Page lancée en Chrome headless |
| `docs/moteur/evaluation/executer-banc.ps1` | Copie dans `%TEMP%`, lance, écrit les résultats dans `%TEMP%` |
| `docs/moteur/evaluation/instantanes/instantane-moteur-2.2-main-78519fb.json` | Instantané de référence (pas une vérité) |
| `docs/moteur/evaluation/resultats/mesures-synthetiques-2026-11-10.json` | Chiffres mesurés |

## 10. Fichiers touchés si le protocole était intégré

- `tools/run-engine-eval.ps1` (nouveau) ou une extension de `tools/run-coaching-tests.ps1` : appeler le banc, comparer à l'instantané, échouer si une invariance casse.
- `.github/workflows/*` (si une CI existe) : lancer le banc à chaque PR qui touche `web/draft-engine.js`, `web/draft-data.js`, `web/draft-champions.js` ou `web/draft-role-sheet.js`.
- `docs/technique/VALIDATION.md` : ajouter une ligne au tableau « Ce que chaque test prouve ».
- `docs/technique/MOTEUR.md` : documenter l'effet de seuil de la règle `peel` et l'effet du mélange d'hypothèses.
- Modèle de PR : un champ « critères modifiés » pour la déclaration utilisée par le rapport de différences.
- Aucun changement nécessaire dans `web/` : le banc n'utilise que des fonctions déjà exposées (`applyProfile`, `opponentBelief`, `roleBelief`, `beliefScenarios`, `identifiedOpponent`, `analyze`, `ENGINE_PREFS`, `CRITERIA`).

## Résumé

1. 360 scénarios synthétiques (252 de développement, 108 d'évaluation gelés), graine 217, séparation par hachage et par catégorie.
2. Vérités de référence écrites à la main ou structurelles, jamais tirées du moteur. Les avis d'experts restent à collecter.
3. Banc exécuté pour de vrai : rôles évidents justes à plus de 99 %, choix hors rôle justes à environ 35 % seulement.
4. Adversaire direct juste à 95-96 %, un seul faux nommé, et personne d'inventé quand l'adversaire n'est pas révélé.
5. Invariances (ordre du pool, ordre des adversaires, préférences, déterminisme) : 100 %. Égalités et picks pris : 100 %.
6. Une violation de monotonie à mid fixé : le seuil de la règle `peel` récompense Xerath quand on ajoute un premier plongeur.
7. Instantané v2.2 enregistré. La version actuelle donne 0 différence. Le rapport de différences sépare changements attendus et inattendus.
8. Tous ces chiffres viennent de données synthétiques et d'un seul annotateur. Ils ne mesurent pas la qualité sur de vraies parties.
