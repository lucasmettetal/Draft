# Audit : historique Riot et journal DRAFT

Étude, aucune modification de l'application. Base : `main` 6218022.
Lu : `src/HistoryImporter.cs`, `web/index.html` (Mes statistiques, journal, Simulateur), `web/draft-mastery.js`, `web/draft-coaching.js`, `web/draft-data.js`, `tools/draft-import-stats.ps1`.
Aucune donnée réelle lue. Les chiffres d'exemple sont fictifs ou calculés.

## 1. En bref

- DRAFT a **trois** sources personnelles, pas deux : l'historique Riot (import de l'application), le journal DRAFT (saisi à la main) et l'ancien import `draft-stats.js` (script).
- L'historique Riot dit **comment tu joues**. Le journal dit **ce que DRAFT a conseillé et ce que tu en as fait**. Ce sont deux questions différentes.
- Les deux ne partagent aucune clé. On ne peut pas les additionner, ni déduire l'un de l'autre.
- La page actuelle mélange les deux dans le même bandeau de chiffres. Elle compte « Conseil suivi » de deux façons différentes selon l'endroit.
- Les vues riches (par champion, évolution, type de mid) ne lisent que l'**ancien** format. L'import de l'application n'affiche que le total, le top 5 du rôle actif et une table par rôle.

## 2. Historique Riot : ce qui est stocké

Fichier `draft-history.js` dans le dossier de données, sous la forme `window.DRAFT_HISTORY = {…};` (`HistoryImporter.cs`, l. 31-33).

### Clés du fichier (`Save`, l. 139-154)

| Clé | Contenu |
|---|---|
| `generated` | Date de l'écriture (ISO, UTC). |
| `accounts` | Comptes importés (`Nom#TAG`). Plusieurs comptes possibles. |
| `skipped` | Parties lues puis écartées (`compte|matchId`) : remakes, autre file. Pour ne pas les redemander. |
| `rows` | Une ligne par partie gardée, de la plus récente à la plus ancienne. |

### Champs d'une ligne (`ConvertMatch`, l. 180-209)

| Champ | Source match-v5 | Remarque |
|---|---|---|
| `id` | `metadata.matchId` | Identifiant Riot de la partie. |
| `acct` | compte saisi | Pas de puuid, pas de nom d'un autre joueur (test l. 330). |
| `time` | `info.gameCreation` | Millisecondes UTC. Création de la partie, pas sa fin. |
| `patch` | `info.gameVersion` | Deux premiers nombres : « 16.22 ». |
| `sec` | `info.gameDuration` | Voir la limite des durées plus bas. |
| `champ` | `championName` du joueur | Nom de l'API : `KogMaw`, `MonkeyKing`. Converti à l'affichage (`toName`, `masteryName`). |
| `pos` | `teamPosition` du joueur | `TOP`, `JUNGLE`, `MIDDLE`, `BOTTOM`, `UTILITY`, ou vide si Riot n'en donne pas. |
| `win` | `win` du joueur | Booléen. |
| `k`, `d`, `a` | `kills`, `deaths`, `assists` | Du joueur seulement. |
| `mid` | `championName` du premier adversaire en `MIDDLE` | Vide si aucun. C'est l'adversaire de voie **seulement si `pos` = `MIDDLE`**. |
| `enemies` | `championName` des 5 adversaires | Dans l'ordre des participants, **sans poste**. |

### Filtres de l'import

- File solo/duo seulement (`queueId` 420, l. 184 et l. 227). Flex, normales et ARAM ne sont jamais lues.
- Partie de moins de 300 s : remake, écartée (l. 185).
- Depuis la date choisie (`startTime`), au plus 100 parties par import (`MaxGamesPerRun`, l. 53). L'import suivant va plus loin dans le passé.

### Ce qui n'est PAS stocké

Farm (CS), or, dégâts, vision, objets, runes, sorts d'invocateur, alliés, poste des adversaires (sauf le mid), file (seule la 420 entre), rang, LP, MMR, objectifs (dragons, barons, tours), chronologie (minute par minute), abandon anticipé, coéquipier en duo, maîtrise officielle Riot, phase de bans, ordre des picks.

### Limites à connaître

- **Poste vide.** `teamPosition` peut être vide. La partie compte dans le total, pas dans les tables par poste.
- **Durées anciennes.** D'après la documentation de match-v5, `gameDuration` est en millisecondes avant le patch 11.20 (octobre 2021), en secondes après. L'import accepte les parties depuis le 16/06/2021 (`OldestSupported`, l. 30). Pour ces quelques mois, `sec` serait 1000 fois trop grand, et le filtre des remakes ne s'appliquerait pas. À filtrer à l'affichage (`sec` > 20 000) tant que l'import ne le corrige pas.
- **Plusieurs comptes.** Les lignes portent `acct`. Toute statistique doit dire si elle mélange les comptes.

## 3. Journal DRAFT : ce qui est stocké

`localStorage`, clé `draftJournal` (`index.html`, l. 1165-1173). Il vit dans le profil WebView2 du dossier de données (`data\webview`). Seul « Télécharger le journal » en fait une copie (`exportJournal`, l. 1610).

### Champs d'une entrée (`choosePick`, l. 1592-1603)

| Champ | Contenu | Remarque |
|---|---|---|
| `id` | `Date.now()` au clic | Sert de clé à la fusion d'un journal importé (`JournalImport`, l. 3154). |
| `date` | ISO au clic sur « Je joue » | Pendant la sélection, avant la partie. |
| `suggested` | Pick affiché par DRAFT (`state.shown`) | Le conseil vu à ce moment. |
| `close` | `!state.clear` | Vrai si le choix était serré. |
| `picked` | Pick cliqué | Mis à jour si le joueur change d'avis dans la même draft. |
| `enemies` | Adversaires connus | Mis à jour pendant la draft (`syncCurrentEntry`, l. 1181). |
| `mid` | `currentMid()` | **Le vis-à-vis du rôle joué**, pas forcément un mid. |
| `result` | `null`, `"win"`, `"loss"` ou `"skip"` | Saisi à la main (`ResultButtons`, l. 2174 ; `setResult`, l. 1605). |
| `engine` | `ENGINE_VERSION` | Version du moteur (« 2.2 »). |

### Ce qui n'est PAS stocké

Rôle joué, origine (draft réelle ou Simulateur), compte, `matchId` Riot, classement complet, scores, raisons du conseil, réglages, pool du moment, durée, KDA, alliés.

### Limites à connaître

- **Simulateur.** `choosePick` ne regarde pas `simulating` : une draft simulée s'écrit dans le vrai journal. Correction en cours ailleurs (`fix/choix-champion`). Les entrées déjà écrites n'ont aucune marque : leur origine est **invérifiable**.
- **Rôle.** Sans rôle enregistré, le journal ne peut pas être découpé par rôle. `mid` d'une draft en jungle est le jungler adverse.
- **Résultat.** La saisie n'apparaît que dans le plan de jeu, pour la draft en cours ou la dernière en attente (`PlanBlock`, l. 2182). Une draft plus ancienne ne se complète qu'après les plus récentes.
- **Partie terminée.** = `result` vaut `win` ou `loss`. `skip` (« Ignorée ») et `null` (« En attente ») n'entrent dans aucun bilan.

## 4. Ce que la page affiche aujourd'hui

| Bloc (fonction, ligne) | Source | Ce qui s'affiche |
|---|---|---|
| Bandeau de chiffres (`StatsKpis`, l. 3185) | Journal + historique | Drafts notées, victoires du journal, « Conseil suivi », nombre de parties Riot. Dans la même rangée. |
| « Conseil suivi » du bandeau (l. 3187) | Journal | `picked === suggested` sur **toutes** les drafts, résultat ou non. |
| « Conseil suivi » de l'onglet Journal (`JournalTab`, l. 2446) | Journal | `picked === suggested` sur les **parties terminées** seulement. |
| Victoires par champion (`StatsOverview`, l. 3250) | Journal | Barres ou jauges, seuil 10. |
| Historique Riot (`HistoryImportBox`, l. 3214) | Historique | Total, top 5 du rôle actif, et dans l'onglet : période, table par rôle (parties, V/D, sans %). |
| Statistiques importées (`StatsTab`, l. 2418) | **Ancien format seulement** (`STAT_ROWS`) | Par champion, par type de mid, évolution par 20, pool / hors pool. |

Constats :

1. **Deux définitions de « Conseil suivi »** sur la même page. Avec 5 drafts sans résultat, le bandeau peut afficher « 3 sur 5 » alors que rien n'est mesuré.
2. **Sources mélangées.** « Drafts notées 5 » et « Historique Riot 93 » sont côte à côte, avec la même taille. Rien ne dit qu'ils ne s'additionnent pas.
3. **L'historique de l'application est sous-exploité.** Par champion, évolution et type de mid ne lisent que `draft-stats.js`. Le KDA, la durée, le patch et les vis-à-vis ne s'affichent nulle part.
4. **Évolution tronquée.** `evolutionGroups` (l. 2396) ne garde que les tranches complètes de 20. Avec 93 parties, les 13 plus récentes disparaissent.
5. **Le seuil est bien tenu.** Aucun pourcentage sous 10 parties (`MIN_GAMES`, l. 1131), jauge de crans à la place. À garder.

Autres usages de l'historique, hors de la page :

- Conseils du Simulateur : `personalTrends` (`draft-coaching.js`, l. 504), résultat personnel seulement à partir de 30 parties et si l'intervalle de Wilson exclut la moyenne du reste.
- Familiarité : `familiarityProfile` (`draft-mastery.js`, l. 87), au volume de parties, jamais aux victoires.
- Classement : `personalLine` (l. 1152) ne lit que l'**ancien** format.

## 5. Deux questions, deux sources

| | A. Performances mesurées par les parties Riot | B. Recommandations DRAFT et leurs résultats |
|---|---|---|
| Question | Comment je joue, sur quels champions, avec quels résultats ? | Que vaut ce que DRAFT m'a conseillé ? |
| Source | `DRAFT_HISTORY` (import Riot) | `draftJournal` (clics « Je joue », résultat saisi) |
| Unité | Une partie classée solo/duo jouée | Une draft notée dans DRAFT |
| Qui l'écrit | Riot, lu par l'import | Le joueur, à la main |
| Fiabilité du résultat | Exacte (donnée Riot) | Déclarative, souvent manquante |
| Contient le conseil | Non, jamais | Oui (`suggested`) |
| Périmètre | Toutes les parties de la file 420, DRAFT ouvert ou non | Les drafts où DRAFT était ouvert et « Je joue » cliqué |

## 6. Pourquoi les 93 parties ne s'ajoutent pas aux 5 drafts

Situation de référence : 5 drafts enregistrées, 0 partie terminée, 0 conseil suivi, 93 parties Riot importées.

1. **Pas la même unité.** Une draft notée peut ne jamais devenir une partie (esquive, simulation). Une partie peut n'avoir aucune draft notée (DRAFT fermé, « Je joue » oublié).
2. **Aucune clé commune.** Le journal ne garde pas le `matchId`. La ligne Riot ne garde aucun identifiant DRAFT.
3. **Recouvrement inconnu.** Les 5 drafts peuvent être 5 des 93 parties. Additionner compterait alors deux fois les mêmes. Elles peuvent aussi être des simulations, qui ne sont pas des parties.
4. **Périodes différentes.** Le journal commence au premier « Je joue ». L'historique commence à la date choisie à l'import.

Conséquence : la page doit montrer deux blocs séparés, chacun avec son échantillon. Jamais un total commun.

## 7. Pourquoi une partie Riot ne dit pas si le conseil a été suivi

1. **Riot ignore DRAFT.** Aucune donnée de match ne dit si DRAFT était ouvert, ni ce qu'il affichait.
2. **Le conseil dépend de l'instant.** Il dépend des adversaires connus au moment du clic, de leur ordre, du rôle supposé de chacun, du pool, des exclusions, des réglages et de la version du moteur. La ligne Riot n'a ni l'ordre des picks, ni le poste des adversaires, ni le pool de l'époque.
3. **Recalculer donne le conseil d'aujourd'hui.** Rejouer la composition finale dans le moteur actuel dirait ce que DRAFT conseillerait maintenant, pas ce qu'il a montré. Et la composition finale n'est pas celle du moment du choix.
4. **Suivi suppose un choix.** Même si le pick joué coïncide avec un conseil recalculé, rien ne prouve que le joueur l'a vu.

Ce qui reste possible, plus tard : **rapprocher** une entrée du journal de sa partie Riot (même compte, même champion, `gameCreation` peu après la `date` du clic) pour remplir le **résultat**. Dans ce sens seulement : du journal vers Riot, jamais de Riot vers un « conseil suivi ». Voir § 10.

## 8. Indicateurs calculables et fiables

Règle commune : un nombre de parties est toujours écrit à côté. Aucun pourcentage sous 10 parties (`MIN_GAMES`). Aucune conclusion sans intervalle (voir `COACHING-ARCHITECTURE.md`).

Repères (intervalle de Wilson à 95 %, calculé) :

| Bilan | Taux | Intervalle |
|---|---|---|
| 3 V sur 6 | 50 % | 19 % à 81 % |
| 5 V sur 10 | 50 % | 24 % à 76 % |
| 11 V sur 22 | 50 % | 31 % à 69 % |
| 42 V sur 93 | 45 % | 35 % à 55 % |
| 150 V sur 300 | 50 % | 44 % à 56 % |

### A. Historique Riot

| Indicateur | Champs | Règle d'affichage | Limite d'échantillon |
|---|---|---|---|
| Parties, V/D | `win` | Toujours. | Aucune : ce sont des comptes. |
| Taux de victoire | `win` | À partir de 10 parties, avec N. | Large : ± 10 points à 93 parties. |
| KDA moyen | `k`, `d`, `a` | (K + A) / max(1, D), sur la somme. Global toujours ; par champion à partir de 10 parties. | Dépend du rôle : ne jamais comparer un support et un mid. Comparaison dans le temps à partir de 20 parties (`KDA_MIN_GAMES`). |
| Champions principaux | `champ` | Classés au **nombre de parties**. | Le volume dit l'expérience, pas le niveau (principe de `MAITRISE.md`). |
| Répartition par poste | `pos` | Comptes ; « sans poste » si vide. | Taux par poste à partir de 10. |
| Par patch | `patch` | Comptes ; taux à partir de 10 par patch. | Un patch n'est pas une durée fixe. Écart entre patchs presque toujours dans le bruit. |
| Tendance par tranches | `time`, `win` | Tranches de 20 (`EVOLUTION_STEP`), dernière tranche incomplète montrée avec une jauge. | Deux tranches de 20 à 40 % et 60 % ont des intervalles qui se recouvrent (22-61 % et 39-78 %). Conclusion seulement avec la règle de tendance (40 parties, z ≥ 2). |
| Durée moyenne | `sec` | Moyenne en minutes, remakes déjà exclus. | Écarter `sec` > 20 000 (durées anciennes en ms). Les abandons à 15 min sont inclus. |
| Vis-à-vis mid | `mid` si `pos` = `MIDDLE` | Comptes par adversaire ; taux à partir de 10 contre le même. | Avec 71 parties en mid et 39 adversaires différents, dont 7 vus au moins 3 fois, aucun duel n'atteint 10 : comptes seulement. |
| Adversaires de la partie | `enemies` | Présence d'un champion dans l'équipe adverse. | Sans poste : « contre une équipe avec X », jamais « contre X en voie ». |

### B. Journal DRAFT

| Indicateur | Règle d'affichage | Limite |
|---|---|---|
| Drafts notées | Toujours. | Peut inclure des simulations (avant la correction). |
| Parties terminées | `result` win ou loss. | Saisie manuelle : souvent incomplète. |
| Taux de victoire du journal | À partir de 10 parties terminées. | Biais : on note plus volontiers certaines parties. |
| Conseil joué | `picked === suggested`, sur les **parties terminées**. Une seule définition. | Sur les drafts sans résultat : seulement « conseil joué dans 3 drafts sur 5 », sans bilan. |
| Conseil suivi contre autre choix | Les deux groupes à 10 parties terminées au moins. | Ne prouve pas que l'outil aide : le choix n'est pas tiré au hasard. Il faut plusieurs centaines de parties pour un écart fiable (texte `journal.followed` existant). |
| Choix serrés | `close`. | Comptes seulement. |

## 9. Indicateurs NON calculables

- Farm, or, dégâts, vision, objets, runes, sorts : non importés.
- Adversaire de voie hors mid, poste des adversaires, synergies avec les alliés.
- Rang, LP, MMR, progression de classement.
- Objectifs, début de partie (first blood, avance à 15 min), chronologie.
- Autres files (flex, normales, ARAM).
- « Conseil suivi » à partir d'une partie Riot (§ 7).
- Effet de DRAFT sur les résultats : il faudrait un choix au hasard, et des centaines de parties.
- Origine des anciennes entrées du journal (réelle ou simulée).
- Journal par rôle (rôle non enregistré).

## 10. Améliorations possibles, pour plus tard (non faites)

Coût : faible = quelques lignes et un test ; moyen = nouvelle saisie ou nouvelle donnée de référence ; élevé = nouvel écran ou nouvelle source.

Ré-import : l'import ne relit jamais une partie déjà connue (`known`, l. 229). Ajouter un champ demande un mode « relire les parties » : une requête par partie, avec la clé du joueur. À 1,3 s par requête, 93 parties ≈ 2 min, 400 parties ≈ 9 min. Les anciennes lignes restent valides : un champ absent vaut « inconnu », jamais 0.

| Amélioration | Champs Riot | Coût | Ré-import | Intérêt |
|---|---|---|---|---|
| Adversaire de **même poste** pour chaque rôle (`opp`) | `teamPosition` des adversaires | Faible | Oui, pour l'existant | **Élevé** : vis-à-vis en jungle, top, bot, support. Débloque les duels hors mid. |
| Poste de chaque adversaire (`enemyPos`, aligné sur `enemies`) | idem | Faible | Oui | Élevé : contient le précédent, et permet « contre une compo avec un tank top ». |
| Farm par minute | `totalMinionsKilled`, `neutralMinionsKilled` | Faible | Oui | Moyen : utile en mid et top, peu en support. Sans comparaison aux autres joueurs. |
| Correction des durées | présence de `gameEndTimestamp` | Faible | Non (correction à la lecture) | Moyen : durées justes avant 11.20. |
| Abandon anticipé | `gameEndedInEarlySurrender` | Faible | Oui | Faible à moyen : écarter les parties à 15 min des durées. |
| Alliés | `championName`, `teamPosition` des alliés | Faible | Oui | Moyen : synergies, mais il faut de très gros échantillons. |
| Or, dégâts, vision | champs du participant | Faible | Oui | Faible : chiffres sans contexte, tentation de comparer. |
| Objets, runes, sorts | identifiants + tables Data Dragon | Moyen | Oui | Faible pour un outil de draft. |
| Flex en plus de solo/duo | `queueId` 440, choix dans la fenêtre | Moyen | Non (nouvelles parties) | Moyen ; risque de mélanger deux files. Garder la file dans la ligne. |
| Rang, LP | `league-v4` | Moyen | Non | Faible : rang actuel seulement, pas d'historique. |
| `role`, `matchId` et origine dans le **journal** | — (journal) | Faible | Non | **Élevé** : journal par rôle, simulations écartées, lien sûr avec Riot. |
| Rapprochement journal → partie Riot | `time`, `champ`, `acct` | Moyen | Non | **Élevé** pour B : résultat rempli sans saisie. À confirmer par le joueur. Risques : esquive, deux parties proches, autre file. |

Ordre conseillé : origine et rôle dans le journal (avec la correction du Simulateur), puis `enemyPos`, puis le rapprochement. Le reste attend un besoin réel.
