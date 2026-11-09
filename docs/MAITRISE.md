# Familiarité, difficulté et compromis

**Statut :** affichage intégré sur `integration/v2.15-coaching`, non publié. Le classement reste celui du moteur (modèle A), et le modèle T reste désactivé.

- Exemples pour débutant et effet sur le classement : `docs/MAITRISE-EXEMPLES.md`
- Préparation de la V2.15 : `docs/INTEGRATION-V215.md`
- Sources de matchups : `docs/SOURCES-MATCHUPS.md`
- Tests : `.\tools\run-coaching-tests.ps1`
- Comparaison : `.\tools\run-mastery-compare.ps1`

## Principes retenus

1. **Le moteur décide du classement.** Aucune personnalisation ne modifie un score.
2. **Deux lectures séparées :** le meilleur choix pour la draft, et le champion le plus joué.
3. **La familiarité n'est pas la performance.** Le plus joué est désigné au volume de parties, jamais aux victoires.
4. **Pas de conversion** entre parties jouées et points de draft : 100 parties ne « valent » pas un meilleur matchup.
5. **La difficulté de Riot n'alimente que des conseils,** avec ses limites. Elle n'entre dans aucun score.

## Données de complexité disponibles

| Notion | Donnée | État |
|---|---|---|
| Difficulté officielle | `info.difficulty` de Data Dragon, de 1 à 10 | Disponible. **Non documentée par Riot.** 0 pour Akshan, Rell, Seraphine et Vex, lu comme inconnu |
| Complexité mécanique | Aucune | **Inconnue** |
| Difficulté de prise en main | Aucune mesure | Approchée par la note de Riot, et déclarée comme telle |
| Courbe de maîtrise | Aucune | **Non mesurée.** Il faudrait suivre beaucoup de joueurs champion par champion |
| Expérience personnelle | Import Riot du joueur | Disponible après import. Aucun import sur ce PC |
| Maîtrise officielle Riot | Non importée | Ignorée dans tous les calculs, même fournie |

### Les notes de Riot se contredisent

Le client affiche une autre note, de 1 à 3 (`tacticalInfo.difficulty`, lue via CommunityDragon). Je l'ai comparée à Data Dragon sur les 173 champions, sans rien conserver ni livrer :

- **Corrélation :** 0,52.
- **Accord sur 3 niveaux :** 50,3 %, sur 169 champions, avec des seuils choisis par moi (1-3, 4-7, 8-10).
- **Contradictions :** sur 41 champions notés 8 ou plus dans Data Dragon, 17 sont à 2 ou moins dans le client.

| Champion | Data Dragon | Client |
|---|---|---|
| Twisted Fate | 9/10 | 2/3 |
| Xerath | 8/10 | 1/3 |
| Vayne | 8/10 | 2/3 |
| Fizz | 6/10 | 2/3 |
| Kog'Maw | 6/10 | 2/3 |
| Lee Sin | 6/10 | 3/3 |
| Viego | 5/10 | 3/3 |
| Cho'Gath | 5/10 | 1/3 |
| Master Yi | 4/10 | 1/3 |
| Briar | 3/10 | 2/3 |
| Darius | 2/10 | 2/3 |

## Modèle de familiarité

`familiarityProfile(rows, { now, account, changes, officialMastery })` travaille **par champion et par rôle**. Une partie sans rôle n'est pas comptée, et un champion inconnu est signalé.

| Élément | Traitement |
|---|---|
| Nombre de parties | Paliers : découverte (<5), apprentissage (<20), familier (<60), expérimenté |
| Ancienneté | Demi-vie de 120 jours, avec un plancher de 25 % : l'expérience baisse sans disparaître |
| Fréquence récente | Parties des 30 derniers jours |
| Inactivité | Signalée à partir de 20 parties et 180 jours |
| Résultats | Affichés à part dès 30 parties, avec un intervalle à 95 %, comparés à ses autres champions du même rôle, jamais à 50 % |
| KDA | Comparé seulement à soi, sur ce champion et ce rôle, dès 20 parties. Pas encore affiché |
| Évolution | Deux moitiés comparées, dès 40 parties. Signalée seulement si elle ressemble peu au hasard (z ≥ 2) |
| Champion modifié | Expérience limitée aux parties récentes, plus 25 % du passé. Liste fournie à la main : Data Dragon n'a pas cette information |
| Maîtrise officielle | Sans effet : 1 000 000 de points ne changent rien, test à l'appui |
| Champions en apprentissage | Choix manuel respecté et expliqué |

Aucun niveau global n'est calculé, et le joueur n'est comparé à personne. Le pool reste celui choisi manuellement.

**Limite de l'import :** il ne lit que le solo/duo, depuis la date choisie. Une absence de partie ne prouve donc pas une absence d'expérience, et l'interface le dit.

## Classement

| Modèle | Rôle | Statut |
|---|---|---|
| **A** | Le moteur, inchangé | **Par défaut** |
| **T** | A, et l'expérience seulement entre champions vraiment à égalité | Prototype, à activer sur décision |
| **B** | A + poids d'expérience et de résultats | **Audit seulement.** Sans `experimental:true`, donne A |
| **C** | B + malus de difficulté | **Supprimé** |
| **D** | Statistiques de matchups dans le score | **Non retenu.** Le fournisseur ne sert qu'à afficher |

### Règles du modèle T

Une égalité n'est départagée que si :

- le score est identique, **et** le risque aussi (même pire pénalité) ;
- le joueur n'a pas désigné de favori dans ce groupe ;
- le meneur a au moins 5 parties utiles d'avance. Sinon, l'égalité reste une égalité.

Les victoires n'interviennent pas. Le score ne change pas, et `strategicTies` reste exposé.

### Effet mesuré (historiques fictifs, 2 000 drafts simulées)

| Profil | Nom en tête changé par B | Dont hors égalité | Nom en tête changé par T | Dont hors égalité |
|---|---|---|---|---|
| Top, 100 parties de Darius à 52 % | **50,3 %** | **50,3 %** | **0 %** | **0 %** |
| Mid, 150 parties de Twisted Fate | 10,4 % | 9,7 % | 0,1 % | 0 % |
| ADC, 180 parties de Vayne | 27,7 % | 21 % | 15,3 % | 0 % |
| Jungle, 120 parties de Master Yi | 35,9 % | 0,3 % | 35,6 % | 0 % |

**T ne dépasse jamais un meilleur score**, test à l'appui avec 1 000 parties sur Darius. Il peut pourtant changer souvent le nom affiché quand le pool contient beaucoup d'égalités : c'est le cas de Master Yi, à égalité avec Cho'Gath et Viego dans 35,6 % de ces drafts. Ce chiffre vient des fiches pauvres relevées dans `docs/AUDIT-MOTEUR.md`.

Cela décrit un comportement. **Rien ne mesure si les conseils sont meilleurs.**

## Présentation pour le joueur

`playerChoices(input)` renvoie les deux lectures et trois sections séparées :

- **Théorie :** ce que les règles disent de la draft, et la difficulté avec ses limites.
- **Expérience :** les parties jouées, puis les résultats à part.
- **Incertitude :** ce qui manque ou peut changer.

Le joueur peut faire expliquer n'importe quel champion avec `pick` : il n'est jamais présenté comme le meilleur choix, et rien n'est interdit.

## Cas où les données sont insuffisantes

| Situation | Comportement |
|---|---|
| Pas d'import | Lecture de la draft seule, et dit clairement |
| Moins de 5 parties sur tous les champions du pool | Personne n'est présenté comme le plus joué |
| Moins de 30 parties sur un champion | Aucun résultat interprété |
| Moins de 30 parties sur ses autres champions du rôle | Pas de référence, aucun résultat interprété |
| Moins de 20 parties avec KDA | Aucune lecture du KDA |
| Moins de 40 parties | Aucune évolution annoncée |
| Note de Riot à 0 | Difficulté dite inconnue |
| Pas de liste des changements | Rien n'est affirmé sur une modification |
| Couple sans bilan, tableau ni source | Le matchup n'entre pas dans le conseil, et c'est dit |
| Courbe de maîtrise, complexité mécanique | Inconnues |
