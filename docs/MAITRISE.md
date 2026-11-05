# Complexité, maîtrise et classement personnalisé

**Statut : prototype, désactivé.** `web/draft-mastery.js` n'est chargé par aucune page. Le classement par défaut reste celui du moteur (modèle A), et rien ne montre encore que la personnalisation améliore les conseils.

- Exemples et comportement chiffré : `docs/MAITRISE-EXEMPLES.md`
- Sources de matchups : `docs/SOURCES-MATCHUPS.md`
- Tests : `.\tools\run-coaching-tests.ps1`
- Comparaison : `.\tools\run-mastery-compare.ps1`

## Données de complexité réellement disponibles

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

**Conséquence : la note de Data Dragon est un indice faible.** Le terme de complexité du modèle C baisse Twisted Fate et Xerath sur cette seule base, alors que le client les considère bien plus simples. Le modèle C ne devrait donc pas passer en production sans une meilleure mesure.

## Modèle de familiarité

`familiarityProfile(rows, { now, account, changes, officialMastery })` travaille **par champion et par rôle**. Une partie sans rôle n'est pas comptée, et un champion inconnu est signalé.

| Élément | Traitement |
|---|---|
| Nombre de parties | Paliers : découverte (<5), apprentissage (<20), familier (<60), expérimenté |
| Ancienneté | Demi-vie de 120 jours, avec un plancher de 25 % : l'expérience baisse sans disparaître |
| Fréquence récente | Parties des 30 derniers jours |
| Inactivité | Signalée à partir de 20 parties et 180 jours |
| Résultats | Intervalle à 95 %, comparé à ses autres champions du même rôle, jamais à 50 %. Pris en compte dès 30 parties, et ramené vers sa moyenne |
| KDA | Comparé seulement à soi, sur ce champion et ce rôle, dès 20 parties. **Hors du classement** |
| Évolution | Deux moitiés comparées, dès 40 parties. Signalée seulement si elle ressemble peu au hasard (z ≥ 2). **Hors du classement** |
| Champion modifié | Expérience limitée aux parties récentes, plus 25 % du passé. Liste fournie à la main : Data Dragon n'a pas cette information |
| Maîtrise officielle | Sans effet : 1 000 000 de points ne changent rien, test à l'appui |
| Champions en apprentissage | Choix manuel respecté, sans baisse liée à la complexité |

Aucun niveau global n'est calculé, et le joueur n'est comparé à personne. Le pool reste celui choisi manuellement : aucun champion n'en sort.

**Limite de l'import :** il commence par défaut au 1er janvier 2026 et ne lit que le solo/duo. Une expérience plus ancienne, ou en Flex, reste invisible. Une absence de partie dans l'import ne prouve donc pas une absence d'expérience.

## Modèles

| Modèle | Formule |
|---|---|
| **A** | Score du moteur, identique au champion près |
| **B** | A + expérience + résultats |
| **C** | B + complexité |
| **D** | C, avec le matchup remplacé par une statistique autorisée. **Sans source, identique à C** |

| Terme | Calcul | Plafond |
|---|---|---|
| Expérience | `1,5 × (1 - exp(-expérience utile / 25))` | +1,5 |
| Résultats | Écart ramené vers sa moyenne, `× 0,75 / 10 points` | ±0,75 |
| Complexité | Note au-dessus de 7, et seulement si moins de 40 parties utiles | -0,75 |

**Ces réglages sont des choix écrits à la main, sans validation.** Pour comparaison, le moteur parle de choix net à partir de 2 points d'écart.

### Ce qui n'est pas compté deux fois

Chaque point est vérifié par un test :

- **Expérience et résultats :** l'expérience ne dépend jamais des victoires, et les résultats ne comptent pas sous 30 parties.
- **Difficile ou simple :** le même historique donne le même effet. Au-delà de 40 parties utiles, le terme de complexité vaut 0 sur tous les champions.
- **Pas de bonus de difficulté :** ce terme ne peut être que négatif ou nul. Sous 8/10, il vaut 0, donc les champions simples ne reçoivent aucune faveur directe. Ils peuvent passer devant quand un autre baisse : c'est un effet à surveiller.
- **Absence d'historique :** ce n'est pas un manque d'expérience. Sans import, la complexité ne joue que si le joueur se déclare débutant.
- **Matchups :** une statistique remplace le critère du moteur, sans s'y ajouter.

### Égalités

Le modèle A garde l'égalité stratégique. Les autres peuvent la départager par l'expérience, mais `strategicTies` reste exposé et le score du moteur ne change pas. Sans historique, l'égalité reste une égalité.

## Ce que montre la comparaison

**Sur des historiques fictifs : cela décrit un comportement, pas une qualité.**

- **Mid (tes 4 champions), sans historique ni niveau déclaré :** classement identique à A sur les 2 000 drafts.
- **Mid, débutant déclaré, modèle C :** premier changé dans 3,2 % des drafts, hors égalités.
- **Mid, joueur Twisted Fate à 150 parties :** 10,6 % avec le modèle C. Twisted Fate passe de 25,2 % à 34,5 % en tête, sans écraser Fizz (51,5 %).
- **Mid, 5 parties de Kog'Maw à 80 % et 150 de Fizz à 55 % :** Fizz monte de 52,5 % à 64,5 %, et Kog'Maw reste à 18 %. Les 5 parties n'apportent qu'un petit effet d'expérience.
- **Top, joueur Darius à 100 parties :** premier changé dans **50,3 %** des drafts.

**Le cas Top est le signal d'alerte.** Darius passe de 0 % à 50,3 % en tête alors que le joueur n'obtient que 52 % de victoires, simplement grâce au volume de parties. C'est peut-être souhaitable pour un joueur qui connaît son champion, mais le poids de 1,5 point n'est pas démontré. Il peut aussi enfermer un joueur sur ce qu'il joue déjà.

## Mode débutant

`masteryAdvice` explique chaque compromis sans rien interdire. `pick` permet de faire expliquer n'importe quel champion, et aucun n'est retiré. Chaque conseil suit le schéma de `docs/COACHING.md`, avec sa source et sa limite.

Les 16 règles couvrent : compromis draft et joueur, égalité, champion accessible ou exigeant, expérience sur un champion difficile, apprentissage, alternative plus jouée, petit échantillon, résultats, inactivité, champion modifié, évolution, adversaire méconnu, matchup favorable à exploiter, absence d'historique, absence de données.

Tous les textes restent à relire par un joueur.

## Cas où les données sont insuffisantes

| Situation | Comportement |
|---|---|
| Pas d'import | Personnalisation sans effet, et dit clairement |
| Moins de 30 parties sur un champion dans ce rôle | Résultats ignorés |
| Moins de 30 parties sur ses autres champions du rôle | Pas de référence, résultats ignorés |
| Moins de 20 parties avec KDA | Aucune lecture du KDA |
| Moins de 40 parties | Aucune évolution annoncée |
| Note de Riot à 0 | Difficulté inconnue, aucune baisse |
| Pas de liste des changements | Rien n'est affirmé sur une modification |
| Couple sans bilan, tableau ni source | Le matchup n'entre pas dans le conseil |
| Courbe de maîtrise, complexité mécanique | Inconnues |
| Efficacité réelle de B, C ou D | **Inconnue** : aucune vraie partie évaluée |

## Recommandations pour la V2.15

1. **Importer d'abord.** Rien ne peut être jugé sans un historique réel. Ajouter ensuite le rôle dans le suivi des drafts réelles, si ce n'est pas déjà fait.
2. **Afficher avant de classer.** Montrer les informations de `masteryAdvice` à côté du classement A, sans le modifier : expérience, petit échantillon, compromis. C'est le gain le plus sûr.
3. **Modèle B, en option seulement,** désactivé par défaut. Comparer plusieurs poids d'expérience sur le relevé réel avant d'en retenir un.
4. **Modèle C, pas pour l'instant.** Il repose sur une note contestée. Il faut d'abord décider si la note du client peut être livrée, puis mesurer.
5. **Lancer le calcul des matchups** par rôle avec une clé personnelle : c'est la voie D la plus claire.
6. **Décider avant de regarder les résultats :** règles de comparaison écrites à l'avance, et aucune annonce sur le taux de victoire sans échantillon suffisant et indépendant.
7. **Réglages à prévoir :** activation de la personnalisation, niveau déclaré, champions en apprentissage.

Intégration technique, après `draft-coaching.js` :

```html
<script src="draft-mastery.js"></script>
```

```js
const profile = familiarityProfile(window.DRAFT_STATS.rows, { account });
const result = personalizedRanking({ enemies, model:"A", profile, learning, level });
const advice = masteryAdvice({ enemies, model:"B", profile, learning, level, pick });
// result.draftBest et result.playerBest donnent les deux lectures à présenter.
```
