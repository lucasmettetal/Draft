# Audit des textes de l'interface

Constat : l'interface dit beaucoup trop de choses, et les mêmes rassurances reviennent partout.
Ce document mesure les textes, puis donne chaque réécriture avec son gain.

Branche `ui/audit-texte`. Catalogues `web/i18n/fr.js` (référence) et `web/i18n/en.js`.

## Méthode

- Un mot = une suite de caractères qui contient une lettre ou un chiffre. « : » et « » » ne comptent pas. Une variable `{x}` compte pour un mot.
- Pluriels : on compte la forme `other`.
- Écran : d'après le composant de `web/index.html` qui appelle la clé. Sinon, d'après le préfixe (`coach.*`, `mastery.*` : onglet Conseils des panneaux et du Simulateur ; `engine.*` : explications du moteur dans les panneaux ; `native.*` : menu et dialogues Windows).
- Une clé citée par deux écrans est comptée une fois, sur la ligne « A + B ».
- Hors périmètre : `home.*` et `nav.*` (maquette de l'Accueil, autre agent), et les 55 clés de `tools/i18n/securite.json` (traduites mot pour mot).

## Mots par écran

| Écran | Clés | Mots avant | Mots après | Gain |
|---|---:|---:|---:|---:|
| Accueil | 117 | 776 | 776 | 0 |
| Accueil + Panneaux | 2 | 9 | 9 | 0 |
| Accueil + Réglages | 5 | 61 | 47 | -14 (-23 %) |
| Mes statistiques | 172 | 1111 | 1080 | -31 (-3 %) |
| Mon pool | 88 | 571 | 544 | -27 (-5 %) |
| Mon pool + Panneaux | 1 | 1 | 1 | 0 |
| Mon pool + Simulateur | 1 | 9 | 9 | 0 |
| Natif (menu, dialogues) | 179 | 1533 | 1533 | 0 |
| Panneaux | 256 | 1046 | 1026 | -20 (-2 %) |
| Panneaux (explications du moteur) | 169 | 1390 | 1385 | -5 (-0 %) |
| Panneaux + Simulateur | 24 | 83 | 80 | -3 (-4 %) |
| Panneaux + Simulateur (onglet Conseils) | 407 | 4807 | 4556 | -251 (-5 %) |
| Réglages | 144 | 995 | 950 | -45 (-5 %) |
| Réglages + Simulateur | 1 | 5 | 5 | 0 |
| Simulateur | 181 | 1136 | 1039 | -97 (-9 %) |
| **Total** | 1747 | **13533** | **13040** | **-493** |

## Les 25 blocs les plus longs (avant)

Hors `home.*` et `nav.*`. « Sécurité » : texte de `securite.json`, laissé tel quel.

| # | Clé | Mots | Écran | Après |
|---:|---|---:|---|---:|
| 1 | `rules.reliability.text` | 81 | Simulateur | 44 |
| 2 | `rules.weights.text` | 42 | Simulateur | 27 |
| 3 | `coach.limits.rulesOnly.l3` | 37 | Panneaux + Simulateur (onglet Conseils) | 22 |
| 4 | `settings.league.autoRoleHint` | 37 | Réglages | 20 |
| 5 | `coach.matchup.unknown.l3weightedHiddenNoMatchup` | 34 | Panneaux + Simulateur (onglet Conseils) | 21 |
| 6 | `settings.league.lcuHint` | 34 | Réglages | sécurité, inchangé |
| 7 | `coach.draft.close.held.l3` | 33 | Panneaux + Simulateur (onglet Conseils) | 18 |
| 8 | `native.riot.dialog.intro` | 33 | Natif (menu, dialogues) | sécurité, inchangé |
| 9 | `prerelease.otherDraft` | 33 | Accueil + Réglages | 19 |
| 10 | `coach.draft.close.l3` | 32 | Panneaux + Simulateur (onglet Conseils) | 15 |
| 11 | `coach.fund.range.text` | 32 | Panneaux + Simulateur (onglet Conseils) | 20 |
| 12 | `rules.how.text` | 32 | Simulateur | 23 |
| 13 | `coach.matchup.range.longer.l3` | 31 | Panneaux + Simulateur (onglet Conseils) | 19 |
| 14 | `pool.table.help` | 31 | Mon pool | 20 |
| 15 | `coach.fund.farm.text` | 30 | Panneaux + Simulateur (onglet Conseils) | 19 |
| 16 | `stats.old.empty` | 30 | Simulateur | 22 |
| 17 | `coach.comp.damage.l3` | 29 | Panneaux + Simulateur (onglet Conseils) | 20 |
| 18 | `coach.fund.roleAdc.text` | 29 | Panneaux + Simulateur (onglet Conseils) | 21 |
| 19 | `mastery.compromise.gap.l3` | 29 | Panneaux + Simulateur (onglet Conseils) | 24 |
| 20 | `coach.fund.damageTypes.text` | 28 | Panneaux + Simulateur (onglet Conseils) | 18 |
| 21 | `coach.fund.positioning.text` | 28 | Panneaux + Simulateur (onglet Conseils) | 22 |
| 22 | `coach.matchup.unknown.l3weightedNoMatchup` | 28 | Panneaux + Simulateur (onglet Conseils) | 15 |
| 23 | `coach.fund.objectives.text` | 27 | Panneaux + Simulateur (onglet Conseils) | 16 |
| 24 | `engine.criterion.matchup.source` | 27 | Panneaux (explications du moteur) | 22 |
| 25 | `settings.display.hotkeyTaken` | 27 | Réglages | 23 |

Sur les 23 blocs réécrits de ce top 25 : 767 mots avant, 490 après (-36 %).

Pour mémoire, les plus longs blocs de l'Accueil (`home.*`, réservés à la maquette) :

- `home.guide.lcu.text` : 33 mots (sécurité)
- `home.hero.failed.lead` : 31 mots
- `home.guide.lead` : 25 mots
- `home.league.failed.text` : 22 mots
- `home.league.connected.text` : 20 mots

## Rassurances : un seul endroit, Réglages

Elles restent dans **Réglages** :

- **Réglages > League** : « Lecture seule, rien n'est modifié dans le client » (`settings.league.lcuHint`, sécurité) et l'avertissement de risque (`home.lcu.risk`, sécurité).
- **Réglages > Données** : « Rien ne quitte ton PC sans ton action » (`settings.data.sub`) et « ta clé n'est pas enregistrée » (`settings.data.history.note`, sécurité).
- **Réglages > Mises à jour** : « DRAFT ne contacte GitHub que lorsque tu cliques » (`settings.updates.version`, sécurité).

Retirées ailleurs :

- panneau droit, sources : « DRAFT n'écrit rien dans League » (`panel.sources.pick`, `panel.sources.league`) ;
- Mes statistiques, état vide : « ta clé, qui n'est jamais enregistrée » (`stats.empty.riot`).

Restent en double, mais intouchables ici : `advice.cta.help` et `stats.history.*` (sécurité), et `home.*` (maquette de l'Accueil). À trancher avec l'agent de l'Accueil : un voyant et un lien vers Réglages suffiraient sur l'Accueil.

## Réécritures

Règles suivies : une phrase par bloc quand c'est possible, le « tu », le vocabulaire du jeu, les variables `{x}` et les pluriels gardés (vérifiés par le script d'application). FR puis EN équivalent.

| Écran | Blocs réécrits | Mots avant | Mots après | Gain |
|---|---:|---:|---:|---:|
| Accueil + Réglages | 1 | 33 | 19 | -14 (-42 %) |
| Mes statistiques | 7 | 146 | 115 | -31 (-21 %) |
| Mon pool | 4 | 90 | 63 | -27 (-30 %) |
| Panneaux | 5 | 74 | 54 | -20 (-27 %) |
| Panneaux (explications du moteur) | 1 | 27 | 22 | -5 (-19 %) |
| Panneaux + Simulateur | 1 | 18 | 15 | -3 (-17 %) |
| Panneaux + Simulateur (onglet Conseils) | 30 | 816 | 565 | -251 (-31 %) |
| Réglages | 7 | 162 | 117 | -45 (-28 %) |
| Simulateur | 10 | 293 | 196 | -97 (-33 %) |
| **Total** | 66 | **1659** | **1166** | **-493 (-30 %)** |

### Accueil + Réglages

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `prerelease.otherDraft` | Une autre DRAFT est ouverte ({names}) : ses panneaux peuvent s'afficher avec ceux de la préversion pendant une draft. Masque les panneaux de l'une des deux (menu de son icône), ou ferme l'autre toi-même. | Une autre DRAFT est ouverte ({names}) : ses panneaux peuvent s'ajouter aux tiens. Masque-les (menu de son icône) ou ferme-la. | 33 → 19 (-14) |

### Mes statistiques

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `stats.journal.compare.warning` | Ce n'est pas une preuve que DRAFT t'aide : tu ne choisis pas au hasard, et il faut des centaines de parties pour un écart fiable. | Pas une preuve que DRAFT t'aide : tu ne choisis pas au hasard, et un écart fiable demande des centaines de parties. | 25 → 21 (-4) |
| `stats.slices.note` | Une tranche de 20 parties bouge beaucoup par hasard : aucune tendance n'est tirée ici (il faudrait {min} parties dans un rôle et un écart net). | Sur 20 parties, le hasard pèse lourd : aucune tendance tirée ici (il faut {min} parties dans un rôle et un écart net). | 25 → 22 (-3) |
| `stats.journal.unknown` | {count} entrées d'origine inconnue : gardées, mais hors des statistiques de performance tant qu'elles ne sont pas classées. Les anciennes versions notaient aussi le Simulateur. | {count} entrées d'origine inconnue, peut-être du Simulateur : gardées, mais hors statistiques tant que tu ne les classes pas. | 24 → 18 (-6) |
| `stats.overview.emptyJournal` | Aucune partie notée pour l'instant. Pendant une draft, clique sur « Je joue », puis sur le résultat : tes bilans apparaîtront ici. | Aucune partie notée. Clique sur « Je joue » en draft, puis sur le résultat : tes bilans s'afficheront ici. | 20 → 17 (-3) |
| `stats.sources.separate` | Historique Riot et journal DRAFT ne s'additionnent pas : ce ne sont pas les mêmes parties, et rien ne les relie. | Historique Riot et journal DRAFT ne s'additionnent pas : ce ne sont pas les mêmes parties. | 20 → 15 (-5) |
| `stats.empty.journal` | Note tes drafts : clique sur « Je joue » pendant une draft, puis note le résultat après la partie. | Clique sur « Je joue » pendant une draft, puis note le résultat. | 17 → 11 (-6) |
| `stats.empty.riot` | Importe ton historique Riot : tes parties classées, lues avec ta clé, qui n'est jamais enregistrée. | Importe ton historique Riot : tes parties classées, lues avec ta clé. | 15 → 11 (-4) |

### Mon pool

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `pool.table.help` | Le favori ne départage que des champions à égalité exacte. Un champion exclu reste dans le pool : il n'est simplement pas conseillé pour l'instant. Ces choix valent pour ce rôle seulement. | Le favori départage les égalités exactes. Un champion exclu reste dans le pool, sans être conseillé. Pour ce rôle seulement. | 31 → 20 (-11) |
| `profiles.legacy.kept` | Ton pool {role} dans DRAFT 2.x diffère de celui-ci : DRAFT garde le sien, sans rien reprendre. Une copie du pool 2.x est conservée. | Ton pool {role} de DRAFT 2.x diffère : celui-ci est gardé, et une copie du pool 2.x aussi. | 23 → 17 (-6) |
| `pool.backup.help` | Chaque rôle garde son propre pool. Ils sont enregistrés automatiquement. Modifier un pool ne change pas ton rôle joué. | Un pool par rôle, enregistré automatiquement. Le modifier ne change pas ton rôle joué. | 19 → 14 (-5) |
| `pool.backup.rulesNote` | Sans statistiques calculées, le conseil repose sur des fiches écrites à la main : c'est un avis général. | Sans statistiques calculées, le conseil vient des fiches écrites à la main. | 17 → 12 (-5) |

### Panneaux

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `advice.opponent.help` | Le client ne donne pas les rôles adverses : cet adversaire est déduit, et se corrige dans la composition. | Rôle déduit, le client ne le donne pas : corrige-le dans la composition. | 18 → 12 (-6) |
| `advice.familiar.help` | Nombre de parties importées dans ce rôle. Cela ne dit pas si tu joues bien ce champion. | Nombre de parties importées dans ce rôle : il ne dit pas si tu joues bien ce champion. | 17 → 17 (0) |
| `panel.sources.league` | « Dans League » : ton propre pick, lu dans ton client. DRAFT n'écrit rien dans League. | « Dans League » : ton propre pick, lu dans ton client. | 14 → 9 (-5) |
| `advice.cta.simHelp` | Simulation : ce choix sert à t'entraîner. Il n'est pas noté dans ton journal. | Simulation : ce choix n'est pas noté dans ton journal. | 13 → 9 (-4) |
| `panel.sources.pick` | Ton pick n'est noté qu'avec « Je joue ». DRAFT n'écrit rien dans League. | Ton pick n'est noté qu'avec « Je joue ». | 12 → 7 (-5) |

### Panneaux (explications du moteur)

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `engine.criterion.matchup.source` | Bilans calculés sur de vraies parties dès 30 parties, sinon tableau écrit à la main. Inclut la pression de lane, pour ne pas la compter deux fois. | Bilans de vraies parties dès 30 parties, sinon tableau écrit à la main. Inclut la pression de lane, pas comptée deux fois. | 27 → 22 (-5) |

### Panneaux + Simulateur

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `viewed.help` | Consulte ce champion : ses conseils et sa comparaison avec le conseil s'affichent. Rien n'est noté : « Je joue » confirme. | Affiche ses conseils et sa comparaison avec le conseil. Rien n'est noté sans « Je joue ». | 18 → 15 (-3) |

### Panneaux + Simulateur (onglet Conseils)

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `coach.limits.rulesOnly.l3` | Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle. | Le score additionne des règles écrites à la main, sans statistiques ni méta. Il compare tes picks : ce n'est pas un winrate. | 37 → 22 (-15) |
| `coach.matchup.unknown.l3weightedHiddenNoMatchup` | Classement pondéré entre les hypothèses : {champions}, et un adversaire pas encore choisi. Le critère matchup n'intervient pas pour ce pick. Si tu connais le vrai rôle, corrige-le : le calcul redevient exact pour ce matchup. | Moyenne pondérée des hypothèses : {champions}, et un adversaire pas encore choisi. Matchup non compté. Corrige le rôle si tu le connais. | 34 → 21 (-13) |
| `coach.draft.close.held.l3` | {champion} mène de {gap} point, sous le seuil de {threshold} utilisé pour changer le nom affiché. Le critère décisif est « {criterion} ». Ces points servent à comparer, ce n'est pas une probabilité de victoire. | {champion} mène de {gap} point, sous le seuil de {threshold} pour changer le nom affiché. Critère décisif : « {criterion} ». | 33 → 18 (-15) |
| `coach.draft.close.l3` | Écart de {gap} point : sous le seuil de {threshold} utilisé pour parler de choix net. Le critère décisif est « {criterion} ». Ces points servent à comparer, ce n'est pas une probabilité de victoire. | Écart de {gap} point, sous le seuil de {threshold} d'un choix net. Critère décisif : « {criterion} ». | 32 → 15 (-17) |
| `coach.fund.range.text` | Le plus court doit s'approcher pour toucher : il ne doit le faire que lorsqu'il y gagne quelque chose. Le plus long doit le punir à chaque fois qu'il avance pour un sbire. | Le plus court ne s'approche que s'il y gagne. Le plus long le punit à chaque pas vers un sbire. | 32 → 20 (-12) |
| `coach.matchup.range.longer.l3` | Écart de portée d'attaque de base : {mine} contre {theirs}. Il pèse surtout tôt dans la partie, et ne dit rien de la portée des sorts ni des outils pour le combler. | Portée d'attaque de base : {mine} contre {theirs}. Compte surtout en début de partie, sans dire la portée des sorts. | 31 → 19 (-12) |
| `coach.fund.farm.text` | Chaque sbire que tu achèves te donne de l'or. Une quinzaine de sbires rapporte à peu près autant qu'un kill : ne prends pas de gros risques pour un échange inutile. | Une quinzaine de sbires valent à peu près un kill : ne risque pas ton farm pour un échange inutile. | 30 → 19 (-11) |
| `coach.fund.roleAdc.text` | Tu fais beaucoup de dégâts mais tu es fragile. Ton but en teamfight : rester en vie et frapper ce qui est à portée, pas aller chercher une cible lointaine. | Gros dégâts, peu de vie. En teamfight, reste en vie et frappe ce qui est à portée, pas une cible lointaine. | 29 → 21 (-8) |
| `coach.comp.damage.l3` | Estimation d'après les notes de Riot et une table écrite à la main. Le vrai partage dépend des objets et de chaque partie : à confirmer une fois en jeu. | Estimé d'après les notes de Riot et une table écrite à la main. Les objets changent ce partage en jeu. | 29 → 20 (-9) |
| `mastery.compromise.gap.l3` | Écart de score de {gap} points en faveur de {champions} (seuil de {threshold} pour un choix net). Aucune conversion entre parties jouées et points : c'est à toi de trancher. | Écart de {gap} points en faveur de {champions} (choix net dès {threshold}). Parties jouées et points ne se comparent pas : à toi de trancher. | 29 → 24 (-5) |
| `coach.fund.positioning.text` | Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière ta frontline : le champion touché en premier a peu de chances de finir le combat. | Avant un combat, regarde qui peut t'atteindre. Si tu es fragile, reste derrière ta frontline : le premier touché finit rarement le combat. | 28 → 22 (-6) |
| `coach.fund.damageTypes.text` | L'armure réduit les dégâts physiques (AD), la résistance magique réduit les dégâts magiques (AP). Regarde ce que font surtout tes adversaires avant de choisir un objet de protection. | L'armure réduit les dégâts physiques (AD), la résistance magique les dégâts magiques (AP). Protège-toi selon les dégâts adverses. | 28 → 18 (-10) |
| `coach.matchup.unknown.l3weightedNoMatchup` | Classement pondéré entre les hypothèses : {champions}. Le critère matchup n'intervient pas pour ce pick. Si tu connais le vrai rôle, corrige-le : le calcul redevient exact pour ce matchup. | Moyenne pondérée des hypothèses : {champions}. Matchup non compté. Corrige le rôle si tu le connais. | 28 → 15 (-13) |
| `coach.fund.objectives.text` | Un kill rapporte davantage s'il mène à quelque chose : une tour, un dragon, un baron. Après un combat gagné, regarde d'abord ce que ton équipe peut prendre. | Après un combat gagné, cherche un objectif : tour, dragon ou baron. Un kill seul rapporte moins. | 27 → 16 (-11) |
| `coach.matchup.unknown.l3weightedHidden` | Classement pondéré entre les hypothèses : {champions}, et un adversaire pas encore choisi. Si tu connais le vrai rôle, corrige-le : le calcul redevient exact pour ce matchup. | Moyenne pondérée des hypothèses : {champions}, et un adversaire pas encore choisi. Corrige le rôle si tu le connais : le calcul redevient exact. | 26 → 22 (-4) |
| `coach.fund.cc.text` | Un champion immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son CC, c'est souvent le bon moment pour agir. | Un champion immobilisé ne peut ni fuir ni attaquer. Agis quand un adversaire vient de rater ou d'utiliser son CC. | 26 → 20 (-6) |
| `coach.matchup.range.shorter.l3` | Avantage de portée d'attaque de base : {mine} contre {theirs}. Il ne compte que si {opponent} n'a pas de quoi le combler : à vérifier dans ses sorts. | Avantage de portée d'attaque de base : {mine} contre {theirs}, si {opponent} n'a pas de sort pour le combler. | 26 → 18 (-8) |
| `coach.comp.frontline.good.l3` | Bonus du critère « {criterion} » (+{impact}). Attention : il compte aussi les bruisers ({count} ici), et se déclenche dès 2 : ne le lis pas comme un avantage rare. | Bonus « {criterion} » (+{impact}). Il compte aussi les bruisers ({count} ici) et se déclenche dès 2 : ce n'est pas un avantage rare. | 26 → 21 (-5) |
| `coach.matchup.range.shorter.l2` | Écart de portée de {gap} en ta faveur : {opponent} doit s'approcher pour farmer, ce qui t'offre des occasions de frapper. Garde un œil sur ses sorts. | {gap} de portée en ta faveur : {opponent} doit s'approcher pour farmer, frappe-le à ce moment. Surveille ses sorts. | 26 → 18 (-8) |
| `coach.matchup.assassin.l3` | Menace d'accès rapide : la zone dangereuse dépend de ses sorts de déplacement, pas de sa portée d'attaque. Le niveau d'objets de chacun change vite ce rapport. | Menace d'accès rapide : la zone dangereuse dépend de ses sorts de déplacement, pas de sa portée. Les objets changent vite ce rapport. | 26 → 22 (-4) |
| `mastery.theory.demanding.l1` | Riot note sa difficulté {note}/10 : il faut parfois plusieurs parties pour en tirer le meilleur. Si tu as envie de l'apprendre, c'est un choix valable. | Difficulté Riot : {note}/10. Il faut parfois plusieurs parties pour le maîtriser ; si tu veux l'apprendre, c'est un choix valable. | 25 → 19 (-6) |
| `coach.comp.noFrontline.l3` | Absence de tank ou de bruiser sur {count} champions visibles. Leur force dépend alors de tuer avant d'être atteint : le premier engage réussi pèse lourd. | Aucun tank ni bruiser sur {count} champions visibles : ils doivent tuer avant d'être atteints, et le premier engage réussi pèse lourd. | 25 → 21 (-4) |
| `coach.comp.frontline.good.l1` | {champions} encaissent beaucoup, et {champion} fait de bons dégâts à ce type de cible. Si l'un d'eux te barre la route, tes coups restent utiles. | {champions} encaissent beaucoup, et {champion} fait de bons dégâts à ce type de cible : tes coups restent utiles. | 25 → 18 (-7) |
| `coach.fund.trades.text` | Attaque quand un sort important de ton adversaire vient de partir, puis recule. Tu lui prends de la vie sans qu'il puisse répondre aussi fort. | Attaque quand un sort important de l'adversaire vient de partir, puis recule : il ne peut pas répondre aussi fort. | 25 → 19 (-6) |
| `coach.matchup.range.shorter.l1` | Tu attaques à {mine}, {opponent} à {theirs}. Ses attaques de base ne t'atteignent pas de loin, mais ses sorts peuvent changer ça : regarde-les avant d'avancer. | Tu attaques à {mine}, {opponent} à {theirs} : ses attaques de base ne t'atteignent pas de loin. Regarde ses sorts avant d'avancer. | 25 → 21 (-4) |
| `coach.matchup.tank.killer.l3` | Face à un tank, l'intérêt de {champion} grandit avec le temps. C'est une règle générale de sa fiche, pas un bilan mesuré de ce matchup. | Face à un tank, {champion} gagne en intérêt avec le temps. Règle générale de sa fiche, pas un bilan mesuré. | 25 → 20 (-5) |
| `coach.draft.conflict.l3` | Critère négatif « {criterion} » ({impact}), compensé par les autres. Un classement par somme peut masquer ce type de faiblesse : décide si elle est acceptable pour toi. | Critère négatif « {criterion} » ({impact}), compensé par les autres. Une somme peut cacher cette faiblesse : à toi de juger. | 25 → 18 (-7) |
| `coachTab.history.importedOn` | Historique importé le {date} : {count} parties classées solo/duo. Les autres modes et les parties non lues ne sont pas comptés. | Historique importé le {date} : {count} parties classées solo/duo, autres modes non comptés. | 20 → 12 (-8) |
| `coach.matchup.unknown.l3weighted` | Classement pondéré entre les hypothèses : {champions}. Si tu connais le vrai rôle, corrige-le : le calcul redevient exact pour ce matchup. | Moyenne pondérée des hypothèses : {champions}. Corrige le rôle si tu le connais : le calcul redevient exact. | 20 → 16 (-4) |
| `coachTab.history.imported` | Historique importé : {count} parties classées solo/duo. Les autres modes et les parties non lues ne sont pas comptés. | Historique importé : {count} parties classées solo/duo, autres modes non comptés. | 18 → 10 (-8) |

### Réglages

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `settings.league.autoRoleHint` | Par défaut, DRAFT charge le pool de ton poste, et te le dit avec un retour possible. Seulement si la lecture du client est active et que LoL indique ton poste. Rien n'est déduit de ton champion. | Charge le pool du poste indiqué par LoL, avec retour possible. Lecture du client requise ; jamais déduit de ton champion. | 37 → 20 (-17) |
| `settings.display.hotkeyTaken` | Ctrl+Alt+D est déjà utilisé par un autre programme, souvent une autre version de DRAFT ouverte. Utilise le menu de l'icône, ou ferme l'autre programme puis relance DRAFT. | Ctrl+Alt+D est pris par un autre programme, souvent une autre DRAFT. Utilise le menu de l'icône, ou ferme ce programme et relance DRAFT. | 27 → 23 (-4) |
| `settings.display.hotkeyLeftAutostart` | Ctrl+Alt+D est laissé à DRAFT 2.x, qui démarre avec Windows sur ce PC. Les panneaux restent accessibles par le menu de l'icône. | Ctrl+Alt+D est laissé à DRAFT 2.x, lancé avec Windows. Les panneaux restent dans le menu de l'icône. | 22 → 17 (-5) |
| `data.riotRoles.noPatch` | Calcul fait avant le suivi par patch : le patch et la région n'ont pas été enregistrés, l'âge est estimé d'après la date. | Calcul antérieur au suivi par patch : patch et région inconnus, âge estimé d'après la date. | 22 → 15 (-7) |
| `settings.display.hotkeyLeftInstance` | Ctrl+Alt+D est laissé à l'autre version de DRAFT ouverte sur ce PC. Les panneaux restent accessibles par le menu de l'icône. | Ctrl+Alt+D est laissé à l'autre DRAFT ouverte. Les panneaux restent dans le menu de l'icône. | 21 → 15 (-6) |
| `settings.diag.lcuHint` | Teste la lecture du client et copie le rapport dans le presse-papiers, pour le joindre à un signalement. | Teste la lecture du client et copie un rapport à joindre à un signalement. | 18 → 14 (-4) |
| `settings.language.hint` | La fenêtre change tout de suite. Les panneaux suivent dès qu'aucune draft n'est en cours. | La fenêtre change tout de suite ; les panneaux, après la draft en cours. | 15 → 13 (-2) |

### Simulateur

| Clé | Avant | Après | Mots |
|---|---|---|---:|
| `rules.reliability.text` | Pendant la draft, le nom affiché ne change que si un autre pick mène d'au moins {gap} points. Les autres rôles servent à l'affichage. Le ban conseillé vient de tes bilans calculés s'il y en a, sinon du tableau de matchups (mid), sinon des fiches de champions. La plupart des conseils sont un avis général, sans statistiques actuelles ni vérification du patch en cours. Seuls les bilans « Tes parties » viennent de tes comptes, et ils portent souvent sur peu de parties. | Le nom affiché ne change que si un autre pick mène d'au moins {gap} points. Le ban conseillé vient de tes bilans, sinon du tableau de matchups (mid), sinon des fiches. Hors « Tes parties », c'est un avis général, sans statistiques du patch en cours. | 81 → 44 (-37) |
| `rules.weights.text` | Chaque critère donne une note de -2 à +2, multipliée par son poids. Le total sert seulement à comparer tes picks : ce n'est ni un winrate ni une probabilité. Ces poids sont des choix de conception, jamais mesurés sur de vraies parties. | Chaque critère note de -2 à +2, multiplié par un poids choisi à la main, jamais mesuré. Le total compare tes picks : ce n'est pas un winrate. | 42 → 27 (-15) |
| `rules.how.text` | Chaque champion a une fiche écrite à la main : chasseur de carrys fragiles, anti-tank, peu mobile, protecteur, etc. Le moteur compare ces fiches à leur comp. C'est un avis général, sans statistiques. | Le moteur compare la fiche écrite à la main de chaque champion (anti-tank, protecteur, peu mobile…) à leur comp. Avis général, sans statistiques. | 32 → 23 (-9) |
| `stats.old.empty` | Pas encore de statistiques importées ici. Ton historique Riot s'importe dans les réglages et s'affiche dans l'onglet Conseils. Un ancien fichier draft-stats.js s'importe dans l'onglet Historique Riot, avec l'application DRAFT. | Aucune statistique importée. Ton historique Riot s'importe dans les réglages et s'affiche dans l'onglet Conseils ; un ancien draft-stats.js, dans l'onglet Historique Riot. | 30 → 22 (-8) |
| `duel.noData` | Pas encore de bilans pour ce profil : le conseil repose sur la composition adverse et les fiches de champions. Lance {action} dans les réglages. | Pas encore de bilans : le conseil repose sur leur comp et les fiches de champions. Lance {action} dans les réglages. | 24 → 20 (-4) |
| `coachTab.history.note` | Le nombre de parties ne dit pas si tu joues bien un champion, et il ne change pas le classement. | Ton expérience, pas ton niveau : ce nombre ne change pas le classement. | 20 → 12 (-8) |
| `plan.basis.many` | Avis général basé sur les {count} champions adverses visibles, sans tenir compte des cooldowns ni des builds. | Avis général sur les {count} champions adverses visibles, sans cooldowns ni builds. | 17 → 12 (-5) |
| `plan.basis.none` | Avis général basé sur aucun champion adverse pour l'instant, sans tenir compte des cooldowns ni des builds. | Avis général sans champion adverse pour l'instant, ni cooldowns ni builds. | 17 → 11 (-6) |
| `plan.basis.one` | Avis général basé sur 1 champion adverse visible, sans tenir compte des cooldowns ni des builds. | Avis général sur 1 champion adverse visible, sans cooldowns ni builds. | 16 → 11 (-5) |
| `sim.band.text` | {simulation} : ta draft en cours n'est pas modifiée. Rien n'est noté dans ton journal. | {simulation} : rien n'est noté dans ton journal, ta draft en cours ne change pas. | 14 → 14 (0) |

## Tests mis à jour

- `web/draft-i18n-tests.html` : texte attendu de `mastery.compromise.gap.l3` (le contrôle des pluriels décimaux reste).
- `web/draft-ux2-tests.html` : bandeau du Simulateur.
- `web/draft-messages-tests.html` : gabarits de l'inventaire des messages pour les textes réécrits (le relevé doit toujours tout rattacher).
- `src/DraftMid.cs` cite `prerelease.otherDraft` et `settings.display.hotkey*` par leur clé : clés gardées, texte seul changé.

