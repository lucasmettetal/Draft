# Moteur de recommandation

Le score sert uniquement à comparer les picks. Ce n'est ni un winrate ni une probabilité. Les règles et les poids sont des choix de conception, jamais mesurés sur de vraies parties.

## Calcul

Chaque critère donne une note de -2 à +2, multipliée par son poids. Le score, les raisons, le classement et la comparaison viennent des mêmes lignes.

| Critère | Poids | Source |
|---|---|---|
| Matchup mid | 3 | Tableau écrit à la main, inclut la pression de lane |
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

## Données

| Type | Contenu |
|---|---|
| Officiel (Data Dragon) | Noms, portraits, illustrations, patch, classes de Locke et Zaahen |
| Rôles récents (API Riot) | Calculés par `tools/draft-update-roles.bat` sur 1 000 parties des 14 derniers jours, Silver à Emeraude. Stockés dans `%LOCALAPPDATA%\DraftMid\draft-role-rates-live.js`, sans nom de joueur. Clé personnelle : usage privé seulement |
| Rôles de secours (Meraki Analytics) | Taux de jeu du patch 16.3, utilisés sous 20 parties récentes. Licence non confirmée : à vérifier avant toute distribution |
| Heuristique | Matchups, profils, règles, plans de jeu |
| Personnel | Journal et import Riot, affichés sans effet sur le classement |
