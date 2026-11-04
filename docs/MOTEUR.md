# Moteur de recommandation

Le score sert uniquement à comparer les picks. Ce n'est ni un winrate ni une probabilité. Les poids et les règles de composition sont des choix de conception, jamais mesurés sur de vraies parties.

## Calcul

Chaque critère donne une note de -2 à +2, multipliée par son poids. Le score, les raisons, le classement et la comparaison viennent des mêmes lignes.

| Critère | Poids | Source |
|---|---|---|
| Matchup mid | 3 | Bilans réels dès 30 parties, sinon tableau écrit à la main. Inclut la pression de lane |
| Tanks et bruisers | 2 | Règle heuristique |
| Survie aux plongeurs | 2 | Règle heuristique |
| Engages et contrôles | 2 | Règle heuristique |
| Cibles exploitables | 1,5 | Règle heuristique |
| Protection nécessaire | 1,5 | Règle heuristique |
| Poke et longue portée | 1 | Règle heuristique |
| Sécurité sans mid connu | 1 | Calculé depuis le tableau, seulement si le mid est inconnu |
| Pression sur la carte | 0,5 | Atout propre au champion |

## Stabilité du verdict

`HOLD_GAP` vaut 2 points.

- **Draft incomplète :** le nom affiché ne change que si un autre pick mène d'au moins 2 points. Un vrai changement stratégique reste donc visible, sans clignotement à chaque ajout.
- **Draft complète :** le classement s'applique directement, quel que soit l'ordre de saisie.
- **Écart inférieur à 2 points :** l'interface affiche « Choix serré » et nomme les deux champions.

## Changements de la V2.2

Ces changements peuvent modifier un résultat. Le calcul des scores, lui, ne change pas à préférences par défaut.

- **Mid probable :** un champion typé jungle, ADC ou support passe après les autres candidats. Le mid supposé peut donc changer pour certaines drafts, et le verdict avec lui. Il reste affiché comme une supposition, corrigeable en un clic.
- **Exclusions :** un pick exclu disparaît du classement et des raisons de ban.
- **Favori :** il ne départage qu'une égalité exacte et ne modifie aucun score.
- **Frontline :** sa supposition devient une préférence, activée par défaut.

## Changements de la V2.5

- **Mid probable :** les listes écrites à la main sont remplacées par les taux de jeu par rôle. Le moteur cherche la répartition la plus probable des adversaires connus, puis lit qui se retrouve au mid.
- **Mid inconnu :** si aucun adversaire connu n'a de chance sérieuse d'être mid, aucun mid n'est inventé, et le matchup direct n'est pas évalué.

## Changements de la V2.6

- **Rôles récents :** s'ils ont été calculés, ils passent avant les statistiques livrées, pour chaque champion joué au moins 20 fois dans cet échantillon. Ces valeurs viennent de la commande « Mettre à jour les rôles » de l'application.
- **Draft complète :** avec 5 adversaires, un mid est toujours supposé. Sur une draft incomplète, aucun mid n'est retenu si sa part est inférieure à 5 %.

## Bilans de matchups (V2.9)

L'API de Riot ne permet pas de chercher les parties d'un champion, et Kog'Maw mid est trop rare dans des parties prises au hasard. Le calcul part donc de joueurs au hasard, de Silver à Emeraude, puis suit ceux qui jouent un champion du pool mid : leur historique en contient bien plus. Il lit les 30 derniers jours, dans la limite de 800 parties par champion et de 3 000 requêtes, soit environ 65 minutes.

- **Biais de l'échantillon :** ces joueurs connaissent souvent bien leur champion et gagnent plus que la moyenne. Le moteur utilise donc l'écart au winrate moyen du pick dans le même échantillon, jamais le winrate brut. Cet écart ne compare pas les picks entre eux en valeur absolue.
- **Petits échantillons :** un matchup sous 30 parties garde l'avis écrit à la main. Au-delà, le bilan est ramené vers la moyenne du pick avec 50 parties fictives.
- **Note :** 5 points d'écart donnent la note d'un 1er choix (+1, soit +3 au score). La note est limitée à ±1,33. Un écart de -3 points ou moins classe le pick « à éviter ».
- **Limites :** ces seuils sont des choix de conception. Un bilan de 100 parties garde environ ±10 points d'incertitude, et les joueurs d'un même champion ne sont pas un échantillon neutre de ton elo.

## Données

| Type | Contenu |
|---|---|
| Officiel (Data Dragon) | Noms, portraits, illustrations, patch, classes de Locke et Zaahen |
| Rôles récents (API Riot) | Calculés par « Mettre à jour les rôles » (`src/RoleRatesUpdater.cs`) sur 1 000 parties des 14 derniers jours, Silver à Emeraude. Stockés dans `%LOCALAPPDATA%\DraftMid\draft-role-rates-live.js`, sans nom de joueur. Résultat local, jamais livré avec l'exécutable. Clé personnelle : usage privé seulement |
| Rôles de secours (Meraki Analytics) | Taux de jeu du patch 16.3, utilisés sous 20 parties récentes et livrés avec l'exécutable. Licence non confirmée : point ouvert, voir [INTEGRATION-RIOT.md](INTEGRATION-RIOT.md) |
| Bilans de matchups (API Riot) | Calculés par « Mettre à jour les matchups » (`src/RoleRatesUpdater.cs`). Stockés dans `%LOCALAPPDATA%DraftMiddraft-matchups-live.js`, sans nom de joueur. Clé personnelle : usage privé seulement |
| Heuristique | Matchups sans assez de parties, profils, règles, plans de jeu |
| Personnel | Journal et import Riot, affichés sans effet sur le classement |

## Adversaire incertain

Les règles et les poids ne changent pas. Quand plusieurs adversaires sont plausibles, chaque pick est évalué contre chacun, et son score est la moyenne pondérée. Une seule hypothèse donne exactement le calcul habituel. Voir [DONNEES.md](DONNEES.md).
