# Audit du moteur stratégique

Audit du 5 novembre 2026, sur le code de la V2.13.0 (`95164df`). Depuis, **seul le traitement des égalités a changé** (voir plus bas). La frontline n'a pas été modifiée : ses variantes n'existent que dans les pages d'audit.

Le tableau « Cho'Gath » et les sections suivantes décrivent le moteur **avant** cette correction.

Reproduire : `.\tools\run-engine-audit.ps1`. Graines fixes, résultat identique d'un passage à l'autre.

**Limite de ces chiffres :** ils décrivent le comportement du moteur sur des drafts simulées, pas la qualité réelle de ses conseils. Les drafts « observées » sont tirées rôle par rôle des fréquences de 1 000 parties, sans reproduire les vraies compositions. Aucune victoire n'est mesurée.

## Ce que calcule le moteur

Le score est une somme de 9 critères notés de -2 à +2 et multipliés par un poids. Il sert à comparer les picks d'un pool : ce n'est ni un winrate ni une probabilité.

| Critère | Poids | Source | Actif pour |
|---|---|---|---|
| Matchup direct | 3 | Bilans calculés, sinon tableau à la main | Mid et 4 champions (tableau). Tout rôle si des bilans existent |
| Sans adversaire connu | 1 | Tableau à la main | Mid et 4 champions |
| Cibles, frontline, plongeurs, contrôles, portée | 1,5 / 2 / 2 / 2 / 1 | Fiches à la main | Tous |
| Protection, carte | 1,5 / 0,5 | Fiches à la main | Tous |

**Conséquence :** hors du profil d'origine et sans bilans calculés, le critère le plus lourd ne s'applique pas. Seules les règles sur les fiches comptent, et rien ne dépend de la méta.

## Cho'Gath : réponse chiffrée

Pool : Cho'Gath, Viego, Master Yi, Briar, Lee Sin, en jungle. 2 000 drafts complètes.

| Situation | Cho'Gath | Lee Sin | Viego | Master Yi | Égalité exacte en tête |
|---|---|---|---|---|---|
| Bouton Draft aléatoire | 67,9 % | 32,1 % | 0 | 0 | 45,4 % |
| Drafts observées | **72,1 %** | 27,9 % | 0 | 0 | 41 % |
| **Mêmes champions, ordre de liste inversé** | **31,3 %** | 48,8 % | 0 | 19,3 % | 41 % |
| Sans Cho'Gath | - | 32,9 % | 67,2 % | 0 | 67,2 % |
| Sans Cho'Gath, ordre inversé | - | 61,2 % | 0 | 37,5 % | 67,2 % |

Score moyen par critère (drafts observées) :

| Champion | Fiche | Tanks | Plongeurs | Cibles | Contrôles | Total |
|---|---|---|---|---|---|---|
| Cho'Gath | tank, tueur de tanks | +2,21 | +0,83 | | | 3,04 |
| Viego, Master Yi | plongeur, tueur de tanks | +2,21 | 0 | | | 2,21 |
| Lee Sin | chasseur, plongeur, engage | -0,24 | 0 | +2,41 | -0,37 | 1,80 |
| Briar | plongeur | | 0 | | | 0 |

### Causes, de la plus lourde à la plus légère

**1. Le moteur, pas le tirage.** Les drafts observées et le bouton donnent presque le même taux de frontlines (64,1 % et 65,3 %). Le générateur n'explique donc pas l'écart.

**2. Une définition trop large de la frontline.** 69 champions sur 173 y sont rangés, dont 16 plongeurs et 9 très mobiles (Yasuo, Yone, Irelia, Fiora, Riven, Lee Sin, Sylas...). Le bonus se déclenche dès 2 :

| Condition, sur 2 000 drafts observées | Drafts |
|---|---|
| Au moins 2 tanks ou bruisers | 64,1 % |
| Au moins 2 vrais tanks | **8,9 %** |

**3. Un bonus pensé pour une exception.** Ce bonus a été conçu pour Kog'Maw, qui paie ensuite son immobilité. Sur les autres champions du trait « tueur de tanks » (44 sur 173), il n'a souvent aucune contrepartie.

| Variante, dans la page d'audit uniquement | Cho'Gath | Lee Sin |
|---|---|---|
| Actuel | 72,1 % | 27,9 % |
| Bonus réduit de moitié hors immobiles | 50,9 % | 49,2 % |
| Sans ce bonus hors immobiles | 33,8 % | 66,3 % |
| Seuls les vrais tanks comptent | 32,9 % | 67,2 % |

**4. Les égalités décidées par l'ordre de la liste.** 41 % des drafts ont une égalité exacte en tête, et le tri garde alors l'ordre du pool. Viego et Master Yi ont la même fiche : celui qui est placé devant gagne toujours, à 0 % pour l'autre. Ce n'est pas une information sur le jeu.

**5. Des fiches trop pauvres.** Briar, Udyr, Aatrox et Illaoi n'ont qu'un trait sans règle associée : leur score vaut 0 dans toutes les drafts. Ils ne peuvent être conseillés que si tout le pool tombe sous 0.

**Cette fréquence est-elle justifiée ?** Rien ne le montre : aucune statistique n'indique que Cho'Gath est le meilleur jungler dans 7 drafts sur 10. Elle vient de règles écrites à la main. Baisser son taux ne prouverait pas non plus un meilleur conseil : les variantes ci-dessus décrivent un comportement, pas une qualité.

## Autres biais

**Échelle par rôle (score moyen).** Top : K'Sante 4,19, Ornn 4,17, très au-dessus d'Aatrox et Illaoi à 0. Support : Rakan 2,23, contre Lux -0,85. Certains profils sont structurellement favorisés.

| Trait de fiche | Avec | Sans |
|---|---|---|
| Tueur de tanks | 2,28 | 1,00 |
| Chasseur de cibles | 2,19 | 1,15 |
| Tank | 2,02 | 1,18 |
| Longue portée | 0,27 | 1,52 |
| Besoin de protection | 0,05 | 1,42 |
| Immobile | -0,23 | 1,71 |

Mages et tireurs à longue portée sont donc pénalisés en moyenne.

**Profil d'origine.** Fizz 55,3 %, Twisted Fate 23 %, Kog'Maw 15,1 %, Xerath 6,7 %, avec 2,5 % d'égalités seulement. Les règles et le tableau y sont bien plus discriminants qu'ailleurs. Cette répartition reste une conséquence de règles écrites à la main, pas une mesure.

**Couverture du matchup, mid uniquement.** 41 adversaires dans le tableau. L'adversaire déduit y figure dans 78 % des drafts observées.

**Incertitude sur l'adversaire.** Plusieurs hypothèses dans 31,4 % des drafts, et un conseil différent dans 3,2 %. L'effet existe, mais reste limité.

**Méta.** Seuls les rôles et les bilans calculés dépendent du patch. Les fiches et le tableau n'en tiennent pas compte, et doivent être revus quand un champion est refait.

**Champion trop récent.** La lecture du client écarte sans message un champion absent de `ROSTER` (`applyDetected`). Le conseil se fait alors sur moins d'adversaires, sans le dire.

## État des données

| Nature | Données | Limites |
|---|---|---|
| Officielles | Noms, images, classes, portée, vitesse, notes de Riot (`draft-champion-official.js`, Data Dragon 16.22.1) | Les notes de 0 à 10 sont des appréciations de Riot. 4 champions sont à 0/0 en attaque et magie |
| Mesurées | Rôles (1 000 parties, patch non enregistré) | Médiane de 50 observations par champion |
| Mesurées, à venir | Bilans de matchups, statistiques personnelles | Rien sur ce PC aujourd'hui. Clé Riot personnelle nécessaire |
| Petits échantillons | Tout bilan sous 100 parties utiles | Marqué comme tel dans les conseils |
| Estimations | Adversaire direct, répartition des dégâts | Déduites, jamais présentées comme un fait |
| Règles écrites à la main | Fiches, tableau de matchups, critères, poids | Aucune validation statistique |
| Informations générales | Bases du jeu, définitions | Stables, à relire si le jeu change |

**Manquent le plus :** de vrais bilans par rôle, des fiches révisées (ou mesurées), l'historique réel du joueur, et une évaluation sur de vraies drafts (protocole déjà prêt).

## Correction des égalités (appliquée)

**Règle de tri :** score, puis risque le plus faible (inchangé), puis favori choisi par le joueur (inchangé), puis nom. Le nom sert seulement à rendre l'ordre indépendant du pool. Aucun score ne change, et aucun bonus n'est ajouté.

**Nouveautés dans le résultat de `analyze` :**

- `ties` : les groupes ex æquo ;
- `tiedWith` sur chaque pick : les champions à égalité avec lui.

`holdPick` reste distinct : il stabilise le nom affiché pendant la draft, sans rien dire sur l'égalité stratégique. Deux scores qui diffèrent de moins d'un millionième sont à égalité : ce sont des arrondis de calcul.

Tests : 4 000 pools mélangés sur 1 000 drafts, dont 777 avec égalité, sans aucun écart de score, d'ordre ou de groupe.

### Différences de classement

1 500 drafts de 1 à 5 adversaires, comparées à `95164df` (`.\tools\run-ties-compare.ps1`). **Aucun score ne diffère sur aucune draft.**

| Pool | Classement identique | Seul l'ordre des ex æquo change | Dont nom en tête différent |
|---|---|---|---|
| Mid (tes 4) | **1 500** | 0 | 0 |
| Jungle (Cho'Gath, Viego, Master Yi, Briar, Lee Sin) | 14 | 1 486 | 268 |
| Jungle (Vi, Amumu, Graves, Kha'Zix, Hecarim) | 287 | 1 213 | 261 |
| Top (Darius, Garen, Malphite, Ornn, Camille) | 1 465 | 35 | 0 |
| ADC (Jinx, Caitlyn, Kog'Maw, Ezreal, Vayne) | 577 | 923 | 577 |
| Support (Thresh, Leona, Soraka, Lulu, Lux) | 63 | 1 437 | 431 |

Ces écarts ne concernent que des champions à score identique : ils ne disent pas qu'un conseil est meilleur.

| Pool Jungle, drafts observées | Cho'Gath en tête | Lee Sin en tête | Égalité en tête |
|---|---|---|---|
| Avant, ordre de saisie | 72,1 % | 27,9 % | 41 % |
| Avant, ordre inversé | 31,3 % | 48,8 % | 41 % |
| **Après, ordre de saisie** | **71,5 %** | **27,8 %** | 41 % |
| **Après, ordre inversé** | **71,5 %** | **27,8 %** | 41 % |

**Limite importante.** L'ordre alphabétique est neutre vis-à-vis du pool, mais pas vis-à-vis des noms : Cho'Gath passe avant Master Yi et Viego. C'est pourquoi il reste à 71,5 % en tête. En partageant chaque égalité à parts égales, sa part tombe à 43,4 %, sur un autre tirage de 2 500 drafts. **Tant que l'écran n'affiche pas les égalités, le biais change de forme sans disparaître.** Cet affichage appartient à l'intégration de la V2.14.

## Audit de la frontline (non appliqué)

Mesuré par `.\tools\run-engine-audit.ps1 -Page frontline-audit.html`, sur 2 500 drafts observées. Rien de ce qui suit n'est dans le moteur.

**Catégories, déduites des fiches existantes :**

| Catégorie | Champions | Défense moyenne notée par Riot |
|---|---|---|
| Tank principal | 23 | 8,00 |
| Frontline situationnelle (bruiser ni plongeur ni très mobile) | 28 | 5,57 |
| Bruiser plongeur ou très mobile | 18 | 4,56 |
| Toutes les autres fiches sans frontline fiable (bruisers exclus compris) | 122 | 3,76 |

Les notes de Riot n'ont pas servi à bâtir ces catégories, mais ce sont des appréciations, pas des mesures. L'écart reste nuancé : 4,56 contre 5,57.

**Variantes :**

- **Ancien :** tous les tanks et bruisers comptent.
- **Sans mobiles :** on retire seulement les bruisers plongeurs ou très mobiles.
- **Tanks seuls :** seuls les tanks principaux comptent.
- **Catégories :** un tank compte pour 1, un situationnel pour 1/2. Ce poids est un choix, pas une mesure.

| Seuil de 2 atteint | Ancien | Sans mobiles | Tanks seuls | Catégories |
|---|---|---|---|---|
| Part des drafts | 63,9 % | 35,8 % | 8,8 % | 11,5 % |

Part en tête, avec les égalités partagées :

| Pool | Variante | Répartition | Égalité en tête | Nom en tête changé |
|---|---|---|---|---|
| **Mid (tes 4)** | Ancien | Fizz 52,5, TF 25,2, Kog'Maw 15,8, Xerath 6,5 | 1,9 % | - |
| | Sans mobiles | Fizz 54,1, TF 29, Kog'Maw 9,7, Xerath 7,3 | 1,6 % | 6,8 % |
| | Tanks seuls | Fizz 55,5, TF 31,8, Kog'Maw 3,4, Xerath 9,2 | 1,8 % | 13,5 % |
| | Catégories | Fizz 55,5, TF 31,4, Kog'Maw 4,3, Xerath 8,8 | 1,5 % | 12,6 % |
| **Jungle Cho'Gath** | Ancien | Cho'Gath 43,4, Lee Sin 34, Viego 11,1, Yi 11,1, Briar 0,3 | 37,1 % | - |
| | Sans mobiles | Lee Sin 48,3, Cho'Gath 32,9, Viego 9,1, Yi 9,1, Briar 0,7 | 31 % | 16,2 % |
| | Tanks seuls | Lee Sin 65,3, Cho'Gath 26,4, Yi 3,5, Viego 3,4, Briar 1,4 | 13 % | 38,2 % |
| | Catégories | Lee Sin 63,5, Cho'Gath 27,1, Viego 4,1, Yi 4,1, Briar 1,2 | 15 % | 35,7 % |
| **Top** | Ancien | Ornn 72,5, Malphite 17,4, Camille 8,7, Garen 1,4 | 34,2 % | - |
| | Sans mobiles | Ornn 57,6, Malphite 29,4, Camille 8,5, Garen 4,6 | 61,2 % | 27,3 % |
| | Tanks seuls | Ornn 46,8, Malphite 39,1, Camille 8,2, Garen 5,8 | 81,4 % | 47,8 % |
| **ADC** | Ancien | Vayne 62,3, Ezreal 29,6, Caitlyn 7, Kog'Maw 1,1 | 14,9 % | - |
| | Sans mobiles | Ezreal 50,4, Vayne 39,4, Caitlyn 9,3, Kog'Maw 0,9 | 24,7 % | 26,2 % |
| | Tanks seuls | Ezreal 68,4, Vayne 15,9, Caitlyn 14,7, Jinx 0,8 | 36,9 % | 52,2 % |
| **Jungle classique** | Ancien | Amumu 46, Kha'Zix 41,5, Hecarim 11,8, Graves 0,7 | 0,1 % | - |
| | Sans mobiles | Amumu 45,9, Kha'Zix 43,8, Hecarim 9,6, Graves 0,7 | 0 % | 2,3 % |
| **Support** | Toutes | Leona 32,4, Thresh 31,2, Soraka 26,7, Lux 9,5, Lulu 0,3 | 58,2 % | 0 % |

**Une répartition plus équilibrée ne prouve rien.** Ces chiffres montrent ce qui bouge, pas ce qui gagne.

**Effets observés :**

- **Moins de décisions.** Réduire la frontline crée beaucoup d'égalités au Top (34 % à 81 % avec les tanks seuls) et à l'ADC. Le critère départageait ces pools.
- **Kog'Maw.** Le bonus lui a été donné pour ses dégâts en % des PV, qui valent aussi contre un bruiser à grosse barre de vie. Avec les tanks seuls, il tombe à 3,4 % : ce choix retire un cas pour lequel la règle a été écrite.

**Cas écrits à la main, à titre de contrôle :**

| Adversaires | Attendu | Ancien | Sans mobiles | Tanks seuls | Catégories |
|---|---|---|---|---|---|
| Ornn, Sion, Maokai | bonus | oui | oui | oui | oui |
| Ornn, Malphite | bonus | oui | oui | oui | oui |
| Yasuo, Irelia, Fiora | pas de bonus | **oui** | non | non | non |
| Darius, Garen | bonus | oui | oui | **non** | **non** |
| Darius, Garen, Sett, Olaf | bonus | oui | oui | **non** | oui |
| Zed, Talon, Akali | pas de bonus | non | non | non | non |

**Ces attentes sont mon avis, pas des données.** J'ai notamment changé celle de « Darius, Garen » après une première lecture des résultats, en passant de « pas de bonus » à « bonus », pour la raison Kog'Maw ci-dessus. Ce tableau ne peut donc pas départager les variantes à lui seul.

### Proposition

**Correction candidate : « Sans mobiles ».** C'est la seule qui retire le défaut évident (Yasuo, Irelia et Fiora comptés comme de la frontline) sans supprimer les bruisers lents, et c'est la plus petite modification.

**Elle n'est pas appliquée**, faute de preuve qu'elle améliore les conseils. Elle changerait le nom en tête dans 6,8 % de tes drafts Mid. Pour décider :

1. rejouer les vraies drafts du relevé avec les deux versions ;
2. relire chaque draft où le conseil change, sans savoir quelle version l'a produit ;
3. ne regarder les victoires que sur un échantillon suffisant et indépendant.

Le bonus sans contrepartie hors Kog'Maw reste un second sujet, à mesurer séparément.

## Fiches à vérifier

Contrôle croisé avec les classes de Riot. Ce sont des questions, pas des erreurs établies.

| Constat | Champions |
|---|---|
| « Tueur de tanks » sur un champion lui-même tank | Cho'Gath, Dr. Mundo, K'Sante, Ornn |
| Classe Tank chez Riot, mais pas tank dans la fiche | Blitzcrank, Singed |
| « Longue portée » avec une portée de base de 125 | Jayce (change de forme) |
| Fiche très pauvre, un trait ou moins | Aatrox, Briar, Corki, Illaoi, Jayce, Lulu, Milio, Morgana, Nami, Neeko, Renata Glasc, Sona, Swain, Udyr, Yuumi |

Le trait « tueur de tanks » de Cho'Gath pèse directement sur sa fréquence : c'est la première fiche à faire relire.

## Recommandations restantes, à décider

Chacune change les conseils.

1. **Afficher les égalités** dans la draft : nécessaire pour que la correction atteigne le joueur.
2. **Frontline** : comparer « Sans mobiles » sur le relevé réel.
3. **Fiches** : relire celles du tableau ci-dessus.
4. **Champion inconnu** : le signaler dans l'interface.
5. **Bilans mesurés** : lancer le calcul des matchups par rôle, ce qui remplacerait réellement ces suppositions.
