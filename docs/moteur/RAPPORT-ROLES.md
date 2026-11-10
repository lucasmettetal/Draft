# Étude du moteur — volet 1 : attribution des rôles adverses

Agent 1 de l'étude en trois volets. Branche `etude/moteur-v217`, copie de `main` 78519fb (DRAFT 2.16.1).
Date : 10 novembre 2026. Lecture seule du code : seuls ce rapport et le dossier `docs/moteur/roles/` ont été écrits.

**Conventions.**
- « Vérifié » : lu dans le code, avec la référence `fichier:ligne`, ou mesuré en exécutant le moteur tel quel.
- « Hypothèse » : déduction non mesurée.
- Les mesures de ce rapport ne portent **que sur les rôles écrits à la main** (`web/draft-role-sheet.js`). Aucune donnée live, aucune donnée Riot réelle et aucun appel réseau n'ont servi. Tous les jeux de drafts utilisés sont **SYNTHÉTIQUES**.
- Pour rejouer les mesures : `pwsh -File docs/moteur/roles/run-etude-roles.ps1`. Le script copie le moteur dans `%TEMP%\draft-etude-agent1` et lance Chrome sans interface. Il ne modifie rien dans le dépôt.

---

## 1. Le système actuel

### 1.1 Ce qui compte vraiment dans le calcul

Une seule information sur les rôles change le classement : **qui est l'adversaire direct du joueur**, c'est-à-dire le champion qui joue le même rôle que lui. Le nom `guessMid` vient de la première version, mais il vaut pour tous les rôles (`web/draft-engine.js:352-360`).

Les autres rôles ne servent qu'à l'affichage. Le code le dit : « Les rôles servent à l'affichage ; seul le mid influence le calcul » (`web/index.html:797`). Les critères de composition (tanks, plongeurs, contrôles, portée…) comptent les cinq adversaires sans regarder leur rôle (`web/draft-engine.js:310-318`, `compoOf`).

Conséquence : une erreur entre Top et Jungle ne coûte rien au joueur mid. Elle compte pour un joueur top ou jungle.

### 1.2 Sources de données, par ordre de priorité

Toutes les sources sont déclarées dans `DATA_PROVIDERS` (`web/draft-data.js:89-166`). `roleEvidence` les lit (`web/draft-data.js:201-235`).

| Source | Code | Rôle dans le calcul |
|---|---|---|
| Rôles calculés par l'utilisateur (API Riot, `ROLE_RATES_LIVE`) | `riotRoleBuckets` `web/draft-data.js:53-64` | Observations : nombre de parties par champion et par rôle, rangées par patch. Calcul facultatif. |
| Référence Meraki (`ROLE_RATES`) | `web/draft-data.js:103-113` | **Absente du dépôt** : le fichier `web/draft-role-rates.js` est exclu par `.gitignore:9`. Le code la gère encore si un fichier local existe. |
| Rôles écrits à la main (`ROLE_SHEET`) | `web/draft-data.js:114-131`, `web/draft-role-sheet.js:4-22` | Repère par défaut. Pour beaucoup d'utilisateurs, c'est la **seule** source, car le calcul live est facultatif. |

**Collecte live (vérifié).** `src/RoleRatesUpdater.cs` compte le `teamPosition` de chaque participant (`:396`). Il ne prend que les parties classées solo/duo (file 420) de plus de 5 minutes, de Silver à Émeraude (`:95-96`). Une partie sur cinq, choisie d'après son identifiant, est mise de côté. Elle n'est jamais comptée dans les rôles : c'est le « holdout » (`:353`, `:355-381`, `:449`). Cette réserve contient au plus 4 000 compositions d'équipe complètes, rangées par rôle (top, jungle, mid, adc, support). C'est une vraie base de test indépendante (voir section 6).

**Rôles écrits à la main (vérifié).** Chaque champion reçoit une chaîne de lettres (t, j, m, a, s), du rôle le plus courant au moins courant. Les poids sont 1, puis 0,35, puis 0,15 (`web/draft-data.js:123-124`). La fiche compte 171 champions, dont **99 avec un seul rôle** (mesuré). Locke et Zaahen n'y figurent pas (`web/draft-role-sheet.js:3`).

### 1.3 Probabilités par champion (`roleEvidence`)

Vérifié dans `web/draft-data.js:201-235` :

1. Les parties observées sont pondérées par leur âge en patchs : poids 0,5^âge, avec un minimum de 0,02 (`:50`, `:220`). Si un changement de méta est détecté (`detectShift`, `:185-199`), les anciens patchs comptent dix fois moins.
2. La référence est la première source disponible : Meraki, sinon la fiche écrite (`:210-213`). Elle est mêlée à une part uniforme qui grandit avec son âge : `uniform = min(0,25 ; 0,05 + 0,01 × âge)` (`:227-230`).
   - La fiche écrite n'a pas de patch. Son âge vaut donc `null`, puis 20 par défaut, et la part uniforme atteint le maximum de 0,25.
   - Résultat mesuré : un champion à un seul rôle reçoit 0,80 pour ce rôle et **0,05 pour chacun des quatre autres**. Un champion à deux rôles reçoit 0,61 et 0,24.
3. La référence compte comme 10 parties (`PRIOR_STRENGTH`). Sans référence, la base est uniforme et compte comme 5 parties (`:9`, `:231-232`).
4. La qualité vaut `observed`, `old`, `thin`, `reference` ou `none` (`:233`).

### 1.4 Combinaison des cinq adversaires (`roleBelief`)

Vérifié dans `web/draft-data.js:258-286`. Le moteur parcourt toutes les façons de placer les champions révélés sur des rôles distincts : 120 au plus pour 5 champions. Le poids d'une répartition est le produit des parts de chaque champion pour le rôle qu'il y occupe. Il en tire deux résultats :

- `marginals[i][r]` : la probabilité que le champion i joue le rôle r ;
- `vacant[r]` : la probabilité que le rôle r ne soit pris par aucun champion révélé. C'est le cas « adversaire pas encore choisi ».

Remarque (hypothèse argumentée) : ce produit est la bonne vraisemblance bayésienne si les choix de champions sont indépendants d'un rôle à l'autre. En effet, P(champion | rôle) est proportionnel à P(rôle | champion), puisque chaque rôle apparaît exactement une fois par équipe. Le modèle est donc sain dans son principe. Ses défauts viennent des paramètres (plancher de 0,05, poids 1/0,35/0,15) et de l'hypothèse d'indépendance : il ne connaît pas les duos bot ni les compositions types.

### 1.5 L'adversaire direct (le « mid supposé »)

- `opponentBelief` (`web/draft-data.js:289-297`) prend la colonne du rôle du joueur (`myRole()`, `web/draft-engine.js:93`). Il trie les candidats par probabilité p et donne `unrevealed = vacant[rôle]` tant que l'équipe a moins de 5 champions.
- `guessMid` (`web/draft-engine.js:354-360`) renvoie le premier candidat connu. Si l'équipe est incomplète et que la part propre du champion pour ce rôle (`share`, pas la probabilité finale) est sous `MIN_MID_SHARE = 0,2` (`:344`), il ne renvoie personne.
- `identifiedOpponent` (`web/draft-data.js:311-314`) renvoie `guessMid` seulement si la confiance n'est pas « faible ».
- `currentMid` (`web/index.html:712-715`) : la correction manuelle `state.manualMid` passe avant tout.

### 1.6 Correction manuelle

Vérifié :

- Placer un champion dans le rôle du joueur passe par `setMid` → `state.manualMid` (`web/index.html:1069-1080`). Dans ce cas, aucune croyance n'est calculée (`belief = null`, `:834`) : le classement n'utilise que cet adversaire.
- Placer un champion dans un autre rôle passe par `state.roleOf[nom] = rôle` (`setRole` `:1001-1008`, `clickRole` `:1082-1095`, `addEnemy` avec un rôle visé `:1037-1042`). Ces rôles imposés vont à `roleBelief` sous le nom `fixed`, qui écarte les répartitions incompatibles (`web/draft-data.js:272-274`). Deux corrections contradictoires sont ignorées toutes les deux (`:282-283`).
- Mesuré : dans la draft Garen / Vi / Kog'Maw / Jinx / Thresh, corriger « Jinx = ADC » fait passer Kog'Maw de p = 0,41 (confiance faible, personne de nommé) à p = 0,83 (confiance moyenne, Kog'Maw nommé).
- La lecture du client ne fournit **aucun rôle adverse**. `diffDetected` n'émet que des ajouts et des retraits (`web/draft-providers.js:54-60`). `docs/technique/LCU.md` le confirme : « Le mid reste une supposition, car le rôle des adversaires n'est pas lu ». Seuls la démo et la saisie manuelle émettent `role-changed`.
- Les corrections sont écrites dans le journal de diagnostic (`web/index.html:1253`).

### 1.7 Affichage des autres rôles (`deriveSlots`)

`web/index.html:798-811`. L'adversaire direct est placé d'abord. Les autres champions sont placés un par un, de façon gloutonne, d'après leurs parts propres (`roleHints`, `:790-796`). Ils ne sont **pas** placés d'après les probabilités jointes de `roleBelief`. Les lignes affichées peuvent donc contredire la répartition la plus probable. Cela reste sans effet sur le score.

---

## 2. Confusions probables et cas atypiques

Toutes les valeurs ci-dessous sont **mesurées** avec les seuls rôles écrits à la main (`cas-roles.html`). Les drafts sont **SYNTHÉTIQUES** : elles montrent le comportement du moteur, pas la fréquence réelle de ces situations.

| Cas | Ce que fait le moteur | Évaluation |
|---|---|---|
| **Kog'Maw mid** + un vrai ADC (Garen, Vi, Kog'Maw, Jinx, Thresh) | Kog'Maw et Jinx sont tous deux « a ». Égalité parfaite : p(mid) = 0,41 pour chacun, confiance faible, personne de nommé, deux scénarios à 50 %. | Prudent et correct sur le fond. Mais le moteur ne peut jamais trancher seul. Une correction (« Jinx = ADC ») suffit. |
| Kog'Maw + Lucian (« am ») | Lucian est pris pour le mid (p = 0,81), Kog'Maw pour l'ADC. | Probablement faux si c'est Kog'Maw qui va au mid (hypothèse). La fiche ne donne aucun rôle mid à Kog'Maw. |
| Twitch + Jinx | Même égalité que Kog'Maw : 0,41 et 0,41, confiance faible. | Twitch est noté « a » seul, alors qu'on le voit aussi jungle et support (hypothèse : à mesurer). |
| **Quinn jungle** (Quinn, Darius, Ahri, Jinx, Thresh) | Quinn et Darius sont tous deux « t » : top 0,49 / jungle 0,41 chacun. Profil jungle ou top : confiance faible. Profil mid : Ahri est nommé, l'erreur ne coûte rien. | Le coût dépend du rôle du joueur. Pour un jungler, l'adversaire direct reste inconnu sans correction. |
| Flex Top/Mid (Sylas + Yasuo) | 0,49 / 0,49, confiance faible, deux scénarios. | Correct. Le test unitaire « 4 et 12 » le couvre déjà (`web/draft-tests.html:195-196`). |
| Mages supports (Xerath + Lux) | Les deux sont « sm » : 0,48 mid / 0,49 support chacun. Faible pour un profil mid comme pour un profil support. | Correct sur le fond. La paire est fréquente : beaucoup de drafts resteront ambiguës sans correction. |
| ADC/Mid (Tristana + Smolder) | Égalité, confiance faible. | Idem. |
| Seraphine + Senna (« sa ») pour un ADC | Égalité 0,48 / 0,49. | Idem. |
| Galio + Karma + Pantheon | Galio 0,78 au mid, Karma 0,67 au support, Pantheon 0,73 au top. Galio nommé, confiance moyenne. | Raisonnable. |
| Taliyah + Zed | Zed mid 0,84, Taliyah jungle 0,84. | Raisonnable. |
| Gragas, Sett, Vi, Jinx, Thresh (aucun mid évident) | Le mid revient à Gragas (0,40), puis Vi (0,29) et Sett (0,18). Confiance faible, 4 scénarios. | Le moteur répartit la place vide entre des champions qui n'y jouent presque jamais. C'est cohérent, mais le plancher de 0,05 pèse beaucoup : le résultat dépend surtout de qui a le moins d'autres options. |
| Malphite + Sion | Sion au mid (0,75), Malphite au top. | Plausible. Malphite mid existe pourtant (hypothèse). |
| **Champion sans fiche** (Locke) dans une équipe complète | Parts uniformes à 0,2. Le moteur lui donne p(mid) = 0,77, mais l'écarte de l'adversaire direct, car il est « inconnu ». Scénario unique « pas encore choisi », p = 1, alors que les 5 champions sont révélés. | Prudent : aucun matchup n'est inventé. Mais l'étiquette interne `mid:null` mélange « inconnu » et « pas encore choisi ». Le texte affiché semble juste, car `unrevealed = 0` (`web/draft-data.js:295`, `:330`). Cas à surveiller. |
| Un seul champion révélé : Sylas | p = 0,61, adversaire « pas encore choisi » à 0,39. Écart 0,22 ≥ 0,15 : confiance **moyenne**, Sylas est affiché comme « Mid probable ». | Discutable : il y a 39 % de chances que le vrai mid ne soit pas encore choisi. Voir section 4. |
| Un seul champion révélé : Lux | p(mid) = 0,24 : faible, personne de nommé. | Correct. |

**Confusions systématiques à prévoir** (hypothèses à mesurer sur la réserve) :

- **Mid ↔ ADC** : Kog'Maw, Twitch, Ziggs, Lucian, Tristana, Smolder, Corki, Seraphine.
- **Mid ↔ Support** : Xerath, Lux, Vel'Koz, Karma, Swain, Galio, Hwei, Neeko, Seraphine, Pantheon.
- **Mid ↔ Top** : Yasuo, Yone, Sylas, Akali, Vladimir, Cassiopeia, Ryze, Irelia, Jayce, Kennen, Gragas.
- **Mid ↔ Jungle** : Diana, Ekko, Taliyah, Naafiri, Qiyana, Talon, Zed, Karthus, Lillia.
- **Top ↔ Jungle** : Quinn (fiche « t » seul), Jax, Olaf, Mordekaiser, Volibear, Wukong, Warwick, Sett.

La fiche écrite ne liste qu'un seul rôle pour 99 champions. Pour eux, tout rôle secondaire réel tombe au plancher de 0,05, identique pour les quatre autres rôles.

**Quand les données live existent**, la plupart de ces égalités disparaissent si l'échantillon est assez grand. La référence pèse 10 parties, et 300 parties la rendent négligeable (`web/draft-data.js:232`). Mais `docs/technique/DONNEES.md:13` note qu'un calcul typique donne une médiane d'environ 50 parties par champion, et moins de 20 parties pour 13 champions. Pour les champions rares, la fiche écrite reste donc décisive.

---

## 3. Les bans

**Vérifié :**

- Le moteur de rôles n'utilise aucun ban. `roleBelief`, `opponentBelief` et `beliefScenarios` ne reçoivent que les champions adverses et les corrections.
- Le mot « ban » n'apparaît dans le moteur que pour **conseiller** un ban au joueur (`suggestBans`, `web/draft-engine.js:370-416`). Ce conseil se sert des parts de rôle pour viser les adversaires directs courants (`bansFromTraits`, `:407-416`).
- La lecture du client ignore volontairement les bans (`docs/technique/LCU.md`, section Endpoints : « Sont volontairement ignorés : … bans »).

**Pertinence pour les rôles :** faible.

- Un champion banni ne joue pas. Il ne change donc pas le rôle des champions déjà révélés.
- Il pourrait seulement affiner le cas « adversaire pas encore choisi ». Mais le moteur ne modélise pas ce cas champion par champion : le scénario `mid:null` déclenche le critère « Sécurité sans adversaire connu » (`RULES.blind`, `web/draft-engine.js:210-217`), qui se calcule sur le tableau écrit. Retirer les bannis de ce tableau changerait peu de choses (hypothèse).
- Les bans sont visibles par tous à l'écran : ce n'est pas une information cachée. Mais les lire demanderait d'élargir la lecture du client, contre la politique écrite du projet, ou d'ajouter une saisie manuelle pénible.

**Recommandation : ne rien faire pour les rôles.** Si un jour le critère « adversaire inconnu » est revu, on pourra reconsidérer une saisie manuelle facultative des bans.

À écarter aussi : les postes adverses, les sorts adverses et les intentions de pick. Ces informations sont cachées ou explicitement exclues (`docs/technique/LCU.md`). L'ordre de pick adverse est visible, mais aucune donnée autorisée ne permet de mesurer son lien avec le rôle : `match-v5` ne contient pas l'ordre de pick. Il ne faut donc pas l'utiliser.

---

## 4. Gestion de l'incertitude

### 4.1 Seuils (vérifié)

| Élément | Valeur | Code |
|---|---|---|
| Confiance **élevée** | p ≥ 0,75 **et** écart ≥ 0,5 avec le rival | `web/draft-data.js:303` |
| Confiance **moyenne** | p ≥ 0,5 **et** écart ≥ 0,15 | `:303` |
| Rival | le plus grand entre le 2ᵉ candidat et « pas encore choisi » | `:302` |
| Plafond | moyenne au plus si la qualité du 1ᵉʳ candidat n'est pas `observed` | `:305` |
| Champion inconnu en tête | toujours faible | `:301` |
| Scénarios gardés | p ≥ 0,10, 4 au plus ; « pas encore choisi » s'il pèse ≥ 0,10 | `:14`, `:317-325` |
| Scénario unique | si le 1ᵉʳ pèse ≥ 0,90 | `:322` |
| Autres hypothèses affichées | p ≥ 0,20, sans pourcentage | `:328-332` |
| Supposition sur une équipe incomplète | part propre ≥ 0,20 | `web/draft-engine.js:344`, `:358` |

**Conséquence vérifiée :** sans calcul live, la qualité vaut toujours `reference`, et la confiance **ne dépasse jamais « moyenne »**. Sur 20 000 drafts SYNTHÉTIQUES, aucune n'a atteint « élevée ».

### 4.2 Affichage (vérifié)

- Mots affichés : `élevée` → « très probable », `moyenne` → « probable », `faible` → « non identifié » (`web/index.html:1266`). Ils apparaissent à côté du nom (`:1308`).
- Si la confiance est faible, personne n'est nommé. Le texte cite les hypothèses (`docs/technique/DONNEES.md:41`).
- Le conseil est marqué « provisoire » si la confiance est faible, ou si les scénarios mènent à des picks différents (`web/index.html:869`).
- `midSwapEffect` signale l'adversaire plausible qui changerait le pick (`web/draft-engine.js:489-496`, appelé en `web/index.html:866`).

### 4.3 Propagation au classement (vérifié)

- `analyze(enemies, mid, hasFront, scenarios)` : avec plusieurs scénarios, chaque critère vaut la moyenne de ses notes, pondérée par la probabilité de chaque scénario (`evaluateScenarios`, `web/draft-engine.js:293-308`).
- Avec un seul scénario, le calcul est identique à l'ancien (`:317`).
- La coaching et la maîtrise relisent la même croyance (`web/draft-coaching.js:91-95`, `web/draft-mastery.js:169-173`).
- Le scénario « pas encore choisi » n'active le critère « Sécurité sans adversaire connu » que pour un joueur mid avec les 4 picks du tableau écrit (`web/draft-engine.js:211`). Pour les autres rôles et pools, ce scénario ne fait que diluer le critère « Matchup direct ».

### 4.4 Ce que montre la simulation SYNTHÉTIQUE

Fichier `synth-roles.html`. Pour chacun des 5 rôles, 4 000 drafts sont générées : des équipes tirées **depuis la fiche écrite elle-même**, et des tailles de 1 à 5 champions révélés. Le générateur est le modèle lui-même : ces chiffres sont une **borne haute**, pas la réalité.

- Assignation complète juste (5 révélés, répartition la plus probable) : 93 à 95 % selon le rôle.
- Profil mid, 5 révélés : confiance faible dans 6 % des drafts (46 sur 800). Avec 2 révélés : 60 %.
- Profil mid, 2 révélés : 41 drafts sur 454 où le vrai adversaire n'était pas encore choisi ont tout de même nommé quelqu'un (cas « Sylas seul »).
- Calibration : le modèle est **sous-confiant** face à son propre générateur. Annoncer 0,75 correspond à 0,95 observé. C'est attendu, car le plancher de 0,05 prévoit des choix atypiques que ce générateur ne produit pas. Sur de vraies parties, la calibration reste **inconnue** : il faut la mesurer sur la réserve (section 6).

---

## 5. Améliorations possibles, sans données interdites

Aucune ne demande d'information cachée, ni de profiler des joueurs : seulement les champions visibles, les corrections du joueur et des statistiques agrégées par champion.

| # | Amélioration | Impact | Complexité | Remarques |
|---|---|---|---|---|
| A | **Réparer et étendre la mesure** : `tools/run-accuracy-tests.ps1` copie `web/draft-role-rates.js` (ligne 20), qui n'existe plus. Le script s'arrête donc en erreur (vérifié par lecture, pas exécuté). Il ne mesure aussi que le mid (`truth: t[2]`, profil mid forcé). Il faut le rendre tolérant à l'absence de Meraki et mesurer les 5 rôles. | Élevé : rien d'autre ne peut se valider sans mesure | Faible | Préalable à tout le reste. |
| B | **Demander une correction ciblée** : quand le conseil est provisoire et que deux candidats sont presque à égalité (Kog'Maw/Jinx, Xerath/Lux, Quinn/Darius), proposer un seul bouton « Qui joue X ? » sur la paire qui change le pick. Le mécanisme de correction existe déjà. | Élevé sur les cas flex | Moyenne (interface) | Ne demander que si `provisional` est vrai : pas de bruit inutile. |
| C | **Enrichir la fiche écrite** pour les rôles secondaires connus : Kog'Maw « am », Twitch « ajs », Quinn « tj »… à vérifier. C'est le repère par défaut de beaucoup d'utilisateurs. | Moyen à élevé sans calcul live | Faible | Reste un « avis général ». Valider chaque ajout par la mesure A sur la réserve, sinon on remplace une erreur par une autre. |
| D | **Calibrer les paramètres** (plancher uniforme 0,25 pour la fiche, poids 1/0,35/0,15, `PRIOR_STRENGTH`) en minimisant la log-loss sur la réserve. Une petite grille suffit. | Moyen | Faible à moyenne | Seulement sur les données locales de l'utilisateur. Ne jamais livrer de chiffres réels dans le dépôt. |
| E | **Revoir la confiance quand peu de champions sont révélés** : exiger par exemple que « pas encore choisi » pèse moins de 0,3 pour dire « probable ». | Moyen (moins de noms affichés à tort) | Faible | Ajuster d'après le taux « nommé alors que non révélé » mesuré sur la réserve. |
| F | **Afficher les autres rôles d'après `roleBelief`** (la répartition la plus probable, ou les marges) plutôt que le placement glouton de `deriveSlots`. | Faible pour le score, moyen pour la crédibilité | Faible | Ce que voit l'utilisateur deviendrait cohérent avec le calcul. |
| G | **Séparer « inconnu » et « pas encore choisi »** dans `beliefScenarios` (cas Locke). | Faible | Faible | Clarifie le texte et les journaux. |
| H | **Statistiques de paires** (duos bot, mid + jungle) tirées des compositions déjà collectées. Données agrégées, sans joueur. | Faible à moyen | Élevée | Lève l'hypothèse d'indépendance. Demande un nouveau format de données. Pas prioritaire. |
| I | Mettre à jour `docs/technique/MOTEUR.md:40` et `:45` : ils décrivent un seuil de « 20 parties » et « la répartition la plus probable ». Le code mélange une référence de poids 10 et pèse toutes les répartitions. | Faible | Très faible | Évite des contresens pour les autres chantiers. |

À **rejeter** : les bans pour les rôles (section 3), l'ordre de pick (non mesurable avec des données autorisées), les postes et sorts adverses (cachés), l'historique des joueurs adverses (profilage).

Ordre conseillé : **A → I → B → E → C/D → F → G → H**.

---

## 6. Mesures et tests reproductibles

### 6.1 Métriques

Pour chaque rôle du joueur et chaque nombre de champions révélés (1 à 5) :

1. **Précision top-1 par rôle** : le candidat de tête, ou « pas encore choisi » si cette case pèse plus, est-il le vrai titulaire du rôle ?
2. **Précision du nom affiché** : quand `identifiedOpponent` nomme quelqu'un, est-ce le bon ? Donner aussi le taux de noms affichés. Intervalle de Wilson, déjà codé dans `tools/run-accuracy-tests.ps1`.
3. **Nommé à tort** : part des drafts où le vrai adversaire n'est pas encore choisi, mais où quelqu'un est nommé.
4. **Couverture** : le vrai cas figure-t-il parmi les scénarios gardés ?
5. **Assignation complète** (5 révélés) : la répartition la plus probable (produit des parts sur les 120 permutations) est-elle entièrement juste ? Précision par rôle sur les 5 lignes.
6. **Calibration** : regrouper les probabilités `marginals` par tranches de 0,1 et comparer à la fréquence observée. Ajouter le score de Brier et la log-loss sur la colonne du rôle du joueur.
7. **Taux de confiance faible**, et précision du nom selon chaque niveau de confiance.
8. **Regret** (déjà dans l'outil) : ce que perd le pick conseillé une fois le vrai adversaire connu, avec les mêmes règles.

### 6.2 Format des jeux de test

```json
{ "id": "flex-001", "origine": "SYNTHÉTIQUE", "equipe": ["Garen","Vi","Kog'Maw","Jinx","Thresh"],
  "ordre": [0,1,3,2,4], "corrections": { "Jinx": "adc" }, "attendu": { "mid": "Kog'Maw" } }
```

- `equipe` est rangée par rôle : top, jungle, mid, adc, support. C'est déjà le format de la réserve (`RoleState.Holdout`, `src/RoleRatesUpdater.cs:295-296`).
- `ordre` donne l'ordre de révélation, tiré au hasard avec une graine fixe (mulberry32, comme dans l'outil existant).
- Deux familles de jeux :
  - **Cas écrits SYNTHÉTIQUES** (versionnés dans le dépôt) : les cas de la section 2, avec un comportement attendu qualitatif (« confiance faible », « deux scénarios », « correction décisive »). Ils peuvent compléter le bloc « Déduction des rôles » de `web/draft-tests.html:183-224`.
  - **Réserve réelle** : lue sur le PC dans `%LOCALAPPDATA%\DraftMid\draft-role-rates-state.json`. Elle n'est **jamais** copiée dans le dépôt. Les résultats agrégés peuvent être notés dans `docs/technique/DONNEES.md`, avec la date et le nombre de parties.

### 6.3 Exécution avec l'outillage existant

- **Unitaires** : ouvrir `web/draft-tests.html` (lancé par les scripts `tools/run-*.ps1` en Chrome sans interface, comme les autres pages `*-tests.html`). Les cas synthétiques passent par `withRoles(...)` (`web/draft-tests.html:173-178`), qui injecte un faux `ROLE_RATES_LIVE` puis le restaure.
- **Comportement sans live** : `docs/moteur/roles/run-etude-roles.ps1` (ce rapport) produit les tableaux des sections 2 et 4.4.
- **Précision réelle** : `tools/run-accuracy-tests.ps1`, après la correction A. Il lit la réserve, fige les drafts (`fixed.js`), puis compare la version de référence (`v2.10.0`) et la version actuelle sur les mêmes drafts. À étendre :
  - paramètre `-Role` ou boucle sur les 5 rôles ;
  - métriques 5 à 7 ;
  - suppression des variantes Meraki si le fichier est absent.
- **Règle de non-régression** proposée : sur la réserve, aucune baisse de la précision du nom affiché ni de la couverture au-delà de l'intervalle de Wilson, et un taux « nommé à tort » qui n'augmente pas.

---

## 7. Fichiers touchés par ces améliorations

Pour repérer les conflits avec les autres volets :

| Fichier | Améliorations | Risque de conflit |
|---|---|---|
| `web/draft-data.js` (`roleEvidence`, `beliefConfidence`, `beliefScenarios`, constantes l. 9-14 et 123) | C (lecture de la fiche), D, E, G | **Élevé** : cœur du moteur |
| `web/draft-role-sheet.js` | C | Faible |
| `web/draft-engine.js` (`guessMid`, `MIN_MID_SHARE`, éventuellement `RULES.blind`) | E, G | Moyen |
| `web/index.html` (`deriveSlots`, `roleHints`, `computeVerdict`, `RoleRow`, nouvelle invite de correction) | B, F | **Élevé** : interface partagée |
| `web/draft-coaching.js`, `web/draft-mastery.js` | Lecture seule de la croyance. À vérifier si l'API de `beliefScenarios` change (G) | Faible |
| `web/draft-tests.html` (bloc « Déduction des rôles ») ou une nouvelle page `web/draft-roles-tests.html` | A, tests des cas | Moyen |
| `tools/run-accuracy-tests.ps1` (ou un nouveau `tools/run-role-tests.ps1`) | A | Faible |
| `docs/technique/MOTEUR.md`, `docs/technique/DONNEES.md` | I, résultats de mesure | Faible |
| `src/RoleRatesUpdater.cs` | H seulement (statistiques de paires) | Faible, non prioritaire |

---

## Résumé (10 lignes)

1. Seul l'adversaire direct (le même rôle que le joueur) influence le score. Les autres rôles ne servent qu'à l'affichage.
2. Le modèle pèse les 120 répartitions possibles avec le produit des parts par champion. C'est sain en principe, mais il suppose les choix indépendants.
3. Sans calcul live, la seule source est la fiche écrite (99 champions sur 171 n'y ont qu'un rôle). Chaque autre rôle reçoit un plancher de 0,05, et la confiance ne dépasse jamais « moyenne ».
4. Les flex et cas atypiques (Kog'Maw/Jinx, Xerath/Lux, Quinn/Darius) donnent des égalités exactes : confiance faible, deux scénarios, une correction manuelle suffit.
5. Les bans ne sont pas utilisés et ne sont pas utiles pour les rôles. Ne pas les ajouter.
6. Un seul champion révélé peut être nommé « probable » alors que l'adversaire a encore 39 % de chances de ne pas être choisi (cas Sylas).
7. `tools/run-accuracy-tests.ps1` dépend du fichier Meraki retiré et ne mesure que le mid : c'est à réparer en premier.
8. La réserve de compositions réelles (holdout, jusqu'à 4 000 équipes) permet de mesurer la précision par rôle et la calibration, en local.
9. Priorités : mesure (A), invite de correction ciblée (B), confiance pour les équipes incomplètes (E), puis fiche et paramètres validés par la mesure (C/D).
10. Fichiers les plus exposés aux conflits : `web/draft-data.js` et `web/index.html`.
