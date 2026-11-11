# Mesure de l'attribution des rôles adverses

Ce document explique comment mesurer la fiabilité de l'estimation des rôles adverses, pour les cinq rôles.
La mesure ne change ni l'algorithme ni les données : elle copie le moteur tel quel dans un dossier de travail et l'interroge.
Aucun appel réseau, aucune clé Riot.

## Outils

| Fichier | Rôle |
|---|---|
| `tools/run-accuracy-tests.ps1` | Mesure complète. Lance d'abord les tests de non-régression. |
| `tools/run-role-tests.ps1` | Tests de non-régression seuls (propriétés + instantané). Code de sortie 0 ou 1. |
| `tests/roles/cas-atypiques.js` | Cas atypiques SYNTHÉTIQUES, rôles attendus annotés à la main. |
| `tests/roles/instantane-roles.js` | Comportement actuel du moteur sur ces cas. Généré, pas écrit à la main. |
| `tests/roles/mesure-roles.js` | Calcul des métriques, dans Chrome sans interface. |
| `tests/roles/generer-drafts.js` | Préparation des drafts figées (graine fixe). |
| `tests/roles/roles-tests.js` | Tests de non-régression. |
| `tests/roles/fixtures/` | Fichiers de TEST SYNTHÉTIQUES (réserve et rôles live inventés), pour vérifier l'outil. |

Tout tourne dans Chrome sans interface, comme les autres tests du projet. Le dossier de travail est
`%TEMP%\draft-role-accuracy` par défaut (`-Work` pour le changer). Le script refuse un dossier de travail placé dans
`%LOCALAPPDATA%\DraftMid`.

## Mode d'emploi

```powershell
# Drafts synthétiques tirées de la fiche écrite. Marche sur tout PC, sans aucune donnée.
pwsh -File tools/run-accuracy-tests.ps1

# Vraies compositions mises de côté sur ce PC (réserve), avec les rôles calculés sur ce PC.
pwsh -File tools/run-accuracy-tests.ps1 -LocalState -LiveFile "$env:LOCALAPPDATA\DraftMid\draft-role-rates-live.js"

# Même chose avec un fichier d'état précis.
pwsh -File tools/run-accuracy-tests.ps1 -StatePath D:\copie\draft-role-rates-state.json

# Comparer le moteur actuel et celui d'un commit (même drafts, mêmes métriques).
pwsh -File tools/run-accuracy-tests.ps1 -CompareRef main

# Tests de non-régression seuls, et mise à jour voulue de l'instantané.
pwsh -File tools/run-role-tests.ps1
pwsh -File tools/run-role-tests.ps1 -UpdateSnapshot
```

Autres paramètres : `-Teams` (équipes synthétiques, 2000 par défaut), `-MaxHoldout` (4000), `-Seed` (20261110),
`-OutJson` (agrégats en JSON, sans aucune composition), `-SkipTests`.

`-CompareRef` lit `web/draft-engine.js`, `web/draft-data.js`, `web/draft-role-sheet.js` et `web/draft-champions.js`
au commit donné. Il faut une version 2.12 ou plus récente : avant, `roleBelief` n'existait pas.

### Sources

**Données des rôles (le modèle).**
- Fiche écrite seule (`web/draft-role-sheet.js`) : toujours mesurée.
- Fiche + calcul live : avec `-LiveFile`, le fichier `draft-role-rates-live.js` est copié dans le dossier de travail et chargé.
- L'ancienne référence Meraki (`web/draft-role-rates.js`) n'est plus utilisée par l'outil. Elle n'est ni chargée ni demandée.

**Drafts mesurées.**
- Par défaut : équipes SYNTHÉTIQUES. Chaque rôle est tiré parmi les champions de la fiche qui le jouent, avec les poids
  1 / 0,35 / 0,15 de la fiche. Le générateur est le modèle lui-même : les chiffres sont une **borne haute**, pas la réalité.
- Avec `-StatePath` ou `-LocalState` : la réserve du calcul des rôles (voir plus bas). C'est la seule base indépendante.

Pour chaque équipe, un ordre de révélation est tiré au hasard (graine fixe), puis l'équipe est mesurée avec 1, 2, 3, 4 et
5 champions révélés. Toutes les variantes (fiche, live, commit de comparaison) voient exactement les mêmes drafts.

## La réserve (holdout)

Vérifié dans le code :

- Emplacement : `%LOCALAPPDATA%\DraftMid\draft-role-rates-state.json` (`src/DraftMid.cs:841`, `StatePath`).
- Format : JSON écrit par `JavaScriptSerializer`, classe `RoleState` (`src/RoleRatesUpdater.cs:290-298`) :
  `Version`, `Regions`, `Seen` (numéros de partie déjà lus), `Patches`, `Legacy`, et `Holdout`, une liste d'équipes.
- Chaque équipe de `Holdout` est un tableau de 5 clés de champion, **rangé par rôle** : top, jungle, mid, adc, support.
- Choix des parties : une partie sur cinq, d'après la somme des caractères de son numéro (`IsHoldout`, `:353`).
  Le choix ne dépend ni du résultat ni des champions. Ces parties ne sont jamais comptées dans les rôles (`:449`).
- Filtres : classé solo/duo (file 420), plus de 5 minutes, joueurs de Silver à Émeraude (`:95-96`). Au plus 4000 équipes (`:308`).
  Les deux équipes d'une partie sont gardées si leurs 5 postes sont remplis.
- La réserve n'existe que si le calcul a été fait avec la version 2.12 ou plus récente (commit 1571d02).

**Ce qui sert de vérité.** `AddHoldout` (`:355-377`) range chaque participant d'après son champ `teamPosition`
dans la réponse match-v5 : `TOP`, `JUNGLE`, `MIDDLE`, `BOTTOM`, `UTILITY`. L'ordre des participants n'est **pas** utilisé.
La vérité est donc le poste que Riot attribue au joueur pour la partie. Limites :
- `teamPosition` est déduit par Riot. Il peut se tromper dans les rares parties avec échange de lignes.
- L'ordre de pick n'est pas gardé : l'ordre de révélation de la mesure est tiré au hasard, pas l'ordre réel.
- La clé de champion vient de `championName`, en minuscules sans ponctuation (`ChampionKey`). L'outil la relie aux noms
  du moteur avec `riotKey` (Wukong = `monkeyking`, Renata Glasc = `renata`). Une équipe avec un champion inconnu est
  écartée et comptée.

**Lecture seule.** Le script copie le fichier d'état dans le dossier de travail et ne lit que la copie. Il n'écrit jamais
dans `%LOCALAPPDATA%\DraftMid`. Le rapport ne contient que des agrégats : aucune composition n'est affichée.
Si le fichier n'existe pas, ou si sa réserve est vide, le script le dit et s'arrête avec le code 2, sans rien mesurer.

**Confidentialité.** Les compositions ne contiennent aucun joueur, mais elles viennent du PC de l'utilisateur.
Ne jamais versionner le fichier d'état ni sa copie. Un résultat publié doit porter l'étiquette
« données locales réelles, n = … » et ne donner que des agrégats.

## Métriques

Toutes les proportions sont données avec un intervalle de Wilson à 95 %.
Une égalité exacte (deux options de même probabilité) donne un crédit partagé : 1/k si la vérité est parmi les k ex aequo.

**Rôle par rôle (indépendant du profil du joueur).** Pour chaque rôle, les options sont les champions révélés et
« pas encore choisi » (la place vide, `vacant`). Les probabilités viennent de `roleBelief`.
- *Précision du 1er candidat* : l'option la plus probable est-elle la bonne ?
- *Brier* : somme sur les options de (p − y)², moyenne par draft et par rôle. 0 est parfait.
- *Log-loss* : −ln p(vraie option), moyenne. 0 est parfait.
- *Tableau de fiabilité* : toutes les options rangées par tranches de 0,1. Une option annoncée à p doit être juste
  dans une proportion proche de p.
- *Assignation complète juste* (5 révélés) : la répartition de plus grand poids parmi les 120 est-elle entièrement juste ?

**Adversaire direct (pour chacun des 5 rôles du joueur).** Calculé avec `opponentBelief`, `identifiedOpponent` et
`beliefScenarios`, profil appliqué avec `applyProfile`.
- *1er candidat juste* (« pas encore choisi » compris).
- *Confiance faible* : taux de drafts où `beliefConfidence` répond « faible ». Aussi « moyenne » et « élevée ».
- *Nommé* : taux de drafts où l'interface désignerait un adversaire, et précision de ce nom.
- *Nommé à tort* : parmi les drafts où le vrai adversaire n'est pas encore révélé, part où quelqu'un est nommé quand même.
- *Vrai cas gardé* : la vérité (un champion, ou « pas encore choisi ») fait-elle partie des hypothèses gardées ?

**Par champion et par rôle réel.** Pour chaque champion révélé : son rôle le plus probable est-il le bon ?
Moyenne de p(rôle réel). Toutes tailles, et 5 révélés à part. Le rapport détaille Kog'Maw, Xerath, Lux, Quinn,
Seraphine, Senna, Twitch, Jinx, Darius, Pantheon et Sylas, puis les 12 couples champion × rôle les moins bien attribués.
Un champion joué hors de son rôle habituel n'apparaît que si les drafts le contiennent : la réserve réelle le permet,
les drafts synthétiques tirées de la fiche ne le permettent pas pour un champion à un seul rôle.

**Champions flex.**
- Fiche écrite : champions avec au moins deux lettres.
- Modèle : champions avec au moins deux rôles à p ≥ 0,2 dans `roleEvidence` (fiche, et live s'il est chargé).
- Égalités exactes : groupes de champions aux parts identiques. Deux champions d'un même groupe, révélés ensemble,
  ne peuvent pas être départagés sans autre information.

**Cas atypiques SYNTHÉTIQUES.** Pour chaque cas de `tests/roles/cas-atypiques.js` : p du rôle annoté, rôle retrouvé en
premier ou non, adversaire affiché comparé à l'adversaire annoté.

## Seuils de mesure : probable, possible, écarté

Ces seuils servent à **mesurer**. Ils ne changent ni l'affichage ni le calcul.

| Classe | Seuil | Raison |
|---|---|---|
| probable | p ≥ 0,6 | Le rôle pèse au moins 1,5 fois tous les autres réunis. Plus strict que la confiance « moyenne » du moteur (p ≥ 0,5), pour qu'un rôle « probable » ne soit pas un pile ou face. Un champion à deux rôles dans la fiche part de 0,61 : il n'est probable que si la draft ne le contredit pas. |
| possible | 0,2 ≤ p < 0,6 | 0,2 = une chance sur cinq, le niveau du hasard sur cinq rôles. C'est aussi le seuil déjà utilisé par le moteur pour citer une autre hypothèse (`beliefHypothesis`, p ≥ 0,2) et pour supposer un adversaire dans une équipe incomplète (`MIN_MID_SHARE`). |
| écarté | p < 0,2 | Moins probable que le hasard. |

Pour un champion révélé : rôle **probable** s'il a un rôle à p ≥ 0,6, sinon **indéterminé** (deux rôles ou plus restent
possibles). Comme les cinq probabilités d'un champion somment à 1, il a toujours au moins un rôle à p ≥ 0,2.

Les seuils se vérifient eux-mêmes : le rapport donne la part de rôles justes dans chaque classe.
Une classe « probable » juste nettement moins de 60 % du temps sur la réserve signalerait un modèle trop sûr de lui.

## Tests de non-régression

`tools/run-role-tests.ps1` lance `tests/roles/roles-tests.js` dans Chrome sans interface. Deux familles :

1. **Propriétés** (vraies quel que soit le réglage) : sur 240 équipes tirées au hasard (graine fixe, 0 à 5 champions,
   champions sans fiche compris), les cas atypiques et un champion inconnu :
   - les probabilités de rôle de chaque champion somment à 1 ; pour chaque rôle, champions + place vide somment à 1 ;
   - le nombre de places vides vaut 5 moins le nombre de champions révélés ;
   - aucune probabilité hors de [0 ; 1] ;
   - adversaire direct : candidats + « pas encore choisi » somment à 1, pour les cinq profils ; hypothèses aussi ;
   - aucun adversaire inventé : seul un champion révélé et connu peut être nommé ou évalué ;
   - rien de révélé : personne n'est nommé, une seule hypothèse « pas encore choisi » ;
   - correction manuelle : le champion corrigé a son rôle à p = 1 et personne d'autre ne l'occupe ;
     au rôle du joueur, il devient l'adversaire direct ; deux corrections contradictoires ne bloquent rien ;
   - même draft, même réponse.
2. **Instantané** : le comportement actuel sur chaque cas atypique (probabilités arrondies à 10⁻⁴, places vides,
   confiance, nom affiché, hypothèses), comparé à `tests/roles/instantane-roles.js` avec une tolérance de 5 × 10⁻⁴.
   **Ce n'est pas une vérité** : un écart signale un changement de comportement. S'il est voulu, relancer avec
   `-UpdateSnapshot` et relire le diff du fichier, une ligne par cas.

La page n'est pas sous `web/` : elle n'est donc jamais embarquée dans l'application. Elle n'a pas été ajoutée à
`tools/run-coaching-tests.ps1`, pour ne pas toucher ce script partagé : `run-accuracy-tests.ps1` la lance d'abord
(`-SkipTests` pour l'éviter).

## Limites

- **Drafts synthétiques** : le générateur suit la fiche écrite, donc le modèle. Les chiffres sont une borne haute.
  Ils ne disent rien de la fréquence réelle des cas atypiques. Les champions à un seul rôle n'y sont jamais joués ailleurs.
- **Réserve** : vraie, mais propre à un PC, une région, une période et les rangs Silver à Émeraude.
  Le rang et la région ne sont pas enregistrés équipe par équipe.
- **Vérité** : `teamPosition` de Riot, pas le rôle annoncé en sélection.
- **Ordre de révélation** : tiré au hasard. L'ordre réel des picks (par exemple, le support souvent choisi tôt)
  n'est pas connu, car match-v5 ne le donne pas.
- **Corrections manuelles** : la mesure ne simule aucune correction, sauf dans les cas atypiques.
  Côté interface, une correction au rôle du joueur passe par `state.manualMid` et contourne la croyance
  (`web/index.html`) : cette partie n'est pas couverte, seul le moteur l'est.
- **Regret** : l'ancien outil mesurait la perte de score du pick conseillé. Cette métrique dépend du pool et des règles,
  pas des rôles : elle n'est plus calculée ici.
- **Taille des échantillons** : sous une trentaine d'apparitions, un résultat par champion n'est qu'un indice ;
  l'intervalle de Wilson le montre.
