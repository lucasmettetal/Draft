# Glossaire LoL et guide d'écriture

Guide éditorial pour tous les textes de DRAFT : la page (`web/index.html`), les raisons du moteur (`web/draft-engine.js`), les conseils (`web/draft-coaching.js`, `web/draft-mastery.js`), les réglages et les textes natifs (`src/`).
Il sert à écrire de nouveaux textes et à relire les PR. La liste des textes à corriger est dans [`../moteur/MESSAGES-A-CORRIGER.md`](../moteur/MESSAGES-A-CORRIGER.md).

## 1. Principes

1. **Parler comme un joueur expérimenté.** Garder les mots anglais que les joueurs utilisent vraiment : dive, engage, peel, poke, CC, frontline, roam, teamfight, comp, matchup, lane. Ne pas les traduire mot à mot (« plongeur », « contrôle », « combat groupé »).
2. **Rester lisible pour un joueur intermédiaire.** Un terme technique par phrase au plus. Pas de jargon d'outil dans l'interface : éviter « heuristique », « verdict », « moteur », « score » sans explication.
3. **Tutoyer.** Jamais de « vous ». Pour l'équipe : « ton équipe », « ta comp ».
4. **Phrases courtes.** Une idée par phrase, 15 mots environ. Pas de deux-points en chaîne (« X passe devant : mid inconnu : peu de… »).
5. **Une seule personne par phrase.** Ne pas passer de « le protéger » à « te protéger » dans la même liste de raisons.
6. **Dire d'où vient l'information.** Fait mesuré, avis écrit à la main, hypothèse ou réglage (§ 3).
7. **Ne jamais habiller un écart du calcul.** Si l'outil ne sait pas, il le dit. Si un nom est affiché par stabilité et pas parce qu'il est 1er, il le dit.
8. **Accorder vraiment.** Pas de « (s) » : écrire « 1 partie », « 3 parties ».
9. **Garder les noms officiels** : champions, sorts (« carte dorée »), rangs, rôles.
10. **Un même concept, un même mot**, partout dans l'application (§ 6).

## 2. Les natures d'argument : ne jamais les mélanger

Chaque raison affichée appartient à une seule de ces familles. Le mot employé doit dire laquelle.

| Nature | Ce que DRAFT sait | Comment le dire | Exemples | Interdit |
|---|---|---|---|---|
| **Matchup direct** | Critère « Matchup direct » : tableau écrit à la main (mid) ou bilans calculés contre ton vis-à-vis | « matchup », « contre {vis-à-vis} » | « Bon matchup contre Ahri. » · « Matchup difficile contre Yasuo. » · « +3 % contre Ahri sur 120 parties. » | Utiliser « matchup » pour autre chose que le vis-à-vis. |
| **Sûr en blind** | Critère « sans adversaire connu » (mid, pool du tableau) | « sûr en blind », « peu de matchups difficiles » | « Sûr en blind : mauvais dans 1 matchup sur 41 du tableau. » | Le dire hors du mid ou hors du tableau. |
| **Réponse à la comp adverse** | Critères carrys, frontline, dive, CC et engage, poke : on compte les cinq adversaires, sans leur rôle | « en face », « leur comp », « contre leur dive / poke » | « Beaucoup d'AP en face. » · « Bon peel contre les champions qui dive. » · « 2 carrys fragiles à punir : Jinx, Lux. » | « Bon matchup » pour un avantage contre la comp. |
| **Counter d'un champion adverse** (pas le vis-à-vis) | Seulement quand une règle lie le pick à ce champion (menaces, cibles) | « bon contre {champion} », « tient le dive de {champion} » | « Tient bien le dive de Zed. » · « Fais fondre Ornn. » | « counter » sans nommer le mécanisme. |
| **Synergie avec ta comp** | Presque rien : DRAFT ne lit pas les picks alliés. Seul le réglage « frontline dans mon équipe » existe | Dire que c'est un réglage ou une condition | « Si ta comp a une frontline, Kog'Maw devient plus sûr. » | Affirmer « Ta comp manque d'engage. » sans données. Cette phrase est réservée au jour où les picks alliés seront lus. |
| **Atout propre au champion** | Critère « Pression sur la map » : ne dépend pas de la draft | Le dire quand c'est lui qui départage | « Grosse pression sur la map (atout du champion, pas de la draft). » | Le présenter comme une réponse à la draft. |
| **Préférence du joueur** | Favori, exclusions, nombre de parties importées | « ton favori », « le plus joué », « tu as plus de parties sur » | « Tu as plus de parties sur Fizz (120 contre 12). » | « meilleur », « tu maîtrises » ; le mélanger aux raisons de la draft. |

Règles associées :
- **R1.** Une raison de comp commence par la comp adverse (« Beaucoup de poke en face »), une raison de matchup par le vis-à-vis.
- **R2.** Un texte d'hypothèse nomme l'hypothèse : « Si c'est Yasuo : 1er choix. » Jamais « 1er choix contre Yasuo · À éviter contre Yone » sans explication.
- **R3.** Une raison positive ne contient jamais « à éviter ».
- **R4.** Ne jamais dire « bon matchup » pour un avantage contre la composition.
- **R5.** « Recommandé », « meilleur choix » et « passe devant » sont réservés au 1er du classement. Un nom maintenu pendant la draft se dit « reste affiché » ou « presque à égalité ».

## 3. Fait, hypothèse, réglage : les mots de certitude

| Situation dans le calcul | Mot retenu | À éviter | Exemple |
|---|---|---|---|
| Bilan calculé sur de vraies parties | « sur {n} parties » | « statistiques » sans nombre | « +3 % contre Ahri sur 120 parties. » |
| Moins de parties que le seuil | « peu de parties » | « tendance » seule | « Peu de parties : à prendre comme une tendance. » |
| Avis écrit à la main (tableau, fiches, règles) | « écrit à la main », « avis général » | « heuristique », « règle heuristique » | « Avis écrit à la main, pas une statistique. » |
| Donnée de Riot | « donnée de Riot », « classes Riot » | — | « Classes Riot : Mage, Assassin. » |
| Adversaire direct, confiance moyenne | « probable » | « supposé » (réservé aux réglages), « deviné » | « Mid d'en face : Ahri (probable). » |
| Adversaire direct, confiance élevée | « presque sûr » | « très probable » si on garde « probable » ailleurs, « certain » | « Mid d'en face : Ahri (presque sûr). » |
| Peu de champions révélés (1 ou 2) | « hypothèse » | « probable » | « Mid ? Sylas, hypothèse (1 champion vu). » |
| Plusieurs candidats, aucun nommé | « pas identifié » | « non identifié », « inconnu » | « Mid d'en face : pas identifié (Ahri ou Zed). » |
| Rôle pas encore pris dans la draft | « pas encore choisi » | « pas encore visible », « pas encore pick » | « Mid d'en face : pas encore choisi. » |
| Valeur venant d'un réglage du joueur | « supposé(e) (réglage) » | — | « Frontline alliée supposée (réglage). » |
| Nombre de parties du joueur | « parties importées » | « expérience », « maîtrise » | « 120 parties importées sur Fizz. » |

## 4. Termes

Genre : celui que les joueurs francophones utilisent. Les mots anglais restent invariables sauf indication.

| Terme retenu | À éviter | Genre, accord | Exemple |
|---|---|---|---|
| matchup | duel (sauf nom d'onglet), affrontement | un matchup, des matchups | « Matchup difficile contre Yasuo. » |
| vis-à-vis | adversaire direct, laner, « le mid supposé » | ton vis-à-vis | « Ton vis-à-vis n'est pas encore choisi. » |
| {rôle} d'en face | « Mid adverse » dans un titre | le mid d'en face, l'ADC d'en face | « Mid d'en face : Ahri (probable). » |
| comp | composition (sauf titres longs) | la comp, ta comp, leur comp | « Leur comp manque de frontline. » |
| pick | choix (sauf « choix serré ») | un pick ; verbe : pick (« pick Ahri ») | « Ton pool a 4 picks. » |
| pool | liste de champions | ton pool | « Le meilleur de ton pool. » |
| ban | bannissement | un ban | « Ban conseillé : Katarina. » |
| blind (pick) | « sans adversaire connu » | en blind ; sûr en blind | « Sûr en blind. » |
| counter | contre-choix | un counter ; « bon contre » | « Bon contre Zed. » |
| frontline | « joueurs solides », « première ligne » | **la** frontline, ta frontline, frontline alliée, pas de pluriel en « frontlines » sauf pour compter | « Pas de frontline alliée pour le protéger. » |
| backline | arrière-garde | la backline | « Reste dans ta backline. » |
| tank | — | un tank | « 3 tanks en face. » |
| bruiser | combattant (sauf classe Riot) | un bruiser | « 2 tanks ou bruisers. » |
| carry fragile | squishy, cible fragile | un carry, des carrys ; un carry fragile | « 2 carrys fragiles à punir : Jinx, Lux. » |
| dive | plongeur, plongeon | le dive ; un champion de dive ; verbe : dive (« va dive l'ADC ») | « Beaucoup de dive en face. » |
| engage | « lancer le combat », « démarrer le combat » | un engage ; verbe : engager | « Beaucoup d'engage en face. » · « Engage sur Jinx dès que ton équipe suit. » |
| disengage | désengagement | le disengage | « Peu de disengage dans ta comp. » |
| peel | protection | le peel ; verbe : peel (« peel ton ADC ») | « Bon peel contre les champions qui dive. » |
| poke | « toucher de loin », « abîmer de loin » | le poke ; verbe : poke | « Beaucoup de poke en face. » |
| burst | dégâts instantanés | le burst | « Gros burst sur une cible. » |
| CC | contrôle, effet de contrôle | un CC, des CC (invariable) ; CC ciblé | « 2 CC ciblés : Leona, Lissandra. » |
| all-in | plongeon | un all-in | « Attends son CC avant ton all-in. » |
| trade | — (« échange » accepté) | un trade, un échange | « Cherche des trades courts. » |
| teamfight | combat groupé | un teamfight | « Gros impact en teamfight. » |
| split push | poussée latérale | le split push | « Bon en split push. » |
| kite | — | le kite ; verbe : kiter | « Kite vers ta frontline. » |
| waveclear | nettoyage de vague | le waveclear | « Bon waveclear. » |
| vague | wave (accepté à l'oral) | la vague, ta vague | « Pousse ta vague avant de roam. » |
| sbires | minions | les sbires | « Une quinzaine de sbires valent un kill. » |
| farm | — | le farm ; verbe : farmer | « Farme à portée maximale. » |
| roam | — | le roam ; verbe : roam (« aller roam ») | « Peut roam une fois sa vague poussée. » |
| gank | — | un gank ; verbe : ganker | « Le jungler peut venir ganker. » |
| lane | voie | la lane, en lane | « Tuer un tank en lane est long. » |
| early, mid game, late | début, milieu, fin de partie | l'early, le mid game, le late | « Fort en early. » |
| scaling | montée en puissance | le scaling ; verbe : scale | « Scale bien en late. » |
| snowball | effet boule de neige | le snowball | « Snowball dès le premier kill. » |
| tempo | — | le tempo | « Le tempo plutôt que le kill. » |
| map | carte | la map | « Grosse pression sur la map. » |
| vision, ward | balise | la vision ; une ward ; une control ward | « Pose une control ward avant le dragon. » |
| kill | mise à mort, meurtre | un kill | « Un kill rapporte plus s'il mène à un objectif. » |
| ult | ultime, ultimate | l'ult, son ult | « Son ult couvre toute la map. » |
| cooldown | délai de récupération | un cooldown | « Sans tenir compte des cooldowns. » |
| sorts d'invocateur | summoners (accepté à l'oral) | les sorts d'invocateur ; Flash, TP | « Regarde ses sorts d'invocateur. » |
| TP | téléportation | un TP | « Un TP bien placé vaut plus qu'un duel gagné. » |
| AD, AP | « dégâts physiques / magiques » seuls | de l'AD, de l'AP | « Beaucoup d'AP en face. » |
| objet | item (accepté) | un objet | « Un objet de résistance magique. » |
| portée, mêlée, distance | corps à corps, range | la portée ; un champion de mêlée | « Champion de mêlée (175). » |
| mobilité | — | la mobilité | « Pas de peel contre leur mobilité. » |
| sustain | — | le sustain | « Peu de sustain en lane. » |
| objectifs : dragon, Nashor, Héraut, tour | baron (accepté) | le dragon, le Nashor, une tour | « Prends une tour après un teamfight gagné. » |
| enchanteur, utilitaire | « soutien » pour un profil (confusion avec le rôle) | un enchanteur | « Profil : utilitaire. » |
| win condition | — (« Gagne si » en étiquette) | la win condition | « Gagne si : tu tues Jinx avant l'objectif. » |
| rangs | mélange « Silver à Émeraude » | une seule langue : Silver, Gold, Platinum, Emerald | « Silver à Emerald, classé solo/duo. » |

## 5. Rôles

- Étiquettes : **Top, Jungle, Mid, ADC, Support** (majuscule, jamais « top » brut ni la clé interne « adc »).
- Dans une phrase : « au top », « à la jungle », « au mid », « à l'ADC », « au support » ; le joueur : « le jungler », « l'ADC », « le support ».
- Classes Riot traduites comme le client : Fighter → Combattant, Marksman → Tireur, Mage, Assassin, Tank, Support.

## 6. Libellés retenus dans l'interface

| Concept | Libellé retenu | Remplace |
|---|---|---|
| Critère matchup | Matchup direct | — |
| Critère blind | Sûr en blind | Sécurité sans adversaire connu |
| Critère cibles | Carrys à punir | Cibles exploitables |
| Critère frontline adverse | Frontline adverse | Tanks et bruisers |
| Critère dive | Résistance au dive | Survie aux plongeurs |
| Critère contrôle | Engage et CC adverses | Engages et contrôles |
| Critère poke | Poke adverse | Poke et longue portée |
| Critère protection | Besoin de peel | Protection nécessaire |
| Critère map | Pression sur la map | Pression sur la carte |
| Jauges | Frontline · Carrys fragiles · Dive · CC ciblés · Engage | Plongeurs · Contrôles ciblés |
| Profils adverses | Tank · Bruiser · Carry fragile · Dive · CC · Engage · Utilitaire | Carry · Plongeur · Contrôle · Soutien |
| Pire critère du pick | Risque | Danger (fenêtre) |
| Champion adverse le plus menaçant | Menace | Danger (panneau) |
| Badges | Choix net · Choix serré · Égalité | — |
| Nom affiché par stabilité | Presque à égalité · reste affiché | Recommandé |
| Bouton | Je joue {champion} | Je prends |
| Réglage | Compter sur une frontline dans mon équipe | Supposer un frontline dans mon équipe |

## 7. Phrases types

| Avant | Après | Pourquoi |
|---|---|---|
| 2 plongeurs contre un champion immobile : Zed, Kha'Zix | 2 champions de dive contre un champion peu mobile : Zed, Kha'Zix | Vocabulaire joueur |
| Peut protéger ses alliés de leurs 2 plongeurs | Bon peel contre leurs 2 champions de dive | Vocabulaire joueur |
| Dégâts surtout magiques en face | Beaucoup d'AP en face | Phrase courte |
| Des joueurs solides en face : Ornn, Sion | Grosse frontline en face : Ornn, Sion | Vocabulaire joueur |
| Peu d'impact hors des combats groupés | Peu d'impact hors teamfight | Vocabulaire joueur |
| Twisted Fate passe devant : mid inconnu : peu de mauvais matchups (1 sur 41 dans le tableau). | Twisted Fate passe devant. Raison : sûr en blind (mauvais dans 1 matchup sur 41 du tableau). | Pas de deux-points en chaîne |
| 1er choix contre Yasuo · À éviter contre Yone | Si c'est Yasuo : 1er choix. Si c'est Yone : à éviter. | R2 |
| Fizz passe devant (alors que Kog'Maw est 1er) | Kog'Maw passe devant de peu. Fizz reste affiché tant que l'écart reste sous 2 points. | R5, principe 7 |
| L'ordre d'affichage n'a pas de sens. (alors que le risque départage) | Même score. Warwick passe en premier : son plus gros point faible pèse moins. | Principe 7 |
| Mid probable (1 champion vu) | Mid ? Sylas, hypothèse (1 champion vu) | § 3 |
| Frontline allié supposé. (option décochée) | Pas de frontline alliée supposée (réglage). | Fidélité, genre |
| Équipe de poke : si vous arrivez affaiblis… | Comp poke : si ton équipe arrive affaiblie, le teamfight démarre mal. | Tutoiement |
| Ta comp manque d'engage. | (à n'écrire que lorsque les picks alliés seront lus) | § 2, synergie |
| Gros impact en teamfight. | (correct : atout du champion ou réponse à la comp, à dire comme tel) | § 2 |

## 8. Vérifier un texte

Avant de valider une PR qui touche un texte :
1. Le texte appartient-il à une seule nature d'argument (§ 2) ? Le mot employé le dit-il ?
2. Le calcul qui le produit dit-il vraiment cela, pour tous les cas où il s'affiche (nom maintenu, égalité, plusieurs hypothèses, réglage décoché, autre rôle que mid) ?
3. Le mot de certitude est-il juste (§ 3) ?
4. Tutoiement, une personne, une idée, accords sans « (s) » ?
5. Lancer `tools/extract-ui-messages.ps1` : aucun texte hors inventaire, constats B visés à 0, et mettre à jour le catalogue de `web/draft-messages-tests.html`.
6. Lancer `tools/run-layout-tests.ps1` si un libellé s'allonge.
