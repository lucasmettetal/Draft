# Audit du moteur stratégique

Audit du 5 novembre 2026, sur le code de la V2.13.0 (`95164df`). **Le moteur n'a pas été modifié.** Les variantes citées n'existent que dans la page d'audit.

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

## Recommandations, à décider

Rien n'est appliqué. Chacune change les conseils.

1. **Égalités.** Les signaler au lieu de les trancher par l'ordre, sans toucher aux scores. Risque faible : c'est mon premier conseil.
2. **Frontline.** Réserver le bonus aux tanks, ou séparer tanks et bruisers. Change les scores du profil d'origine, donc à comparer sur le relevé réel.
3. **Fiches à 0.** Compléter Briar, Udyr, Aatrox et Illaoi.
4. **Champion inconnu.** Le signaler dans l'interface.
5. **Bilans mesurés.** Lancer le calcul des matchups par rôle : c'est ce qui remplacerait réellement ces suppositions.
