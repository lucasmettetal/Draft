# Glossaire français / anglais (League of Legends)

Base : [`../technique/GLOSSAIRE-LOL.md`](../technique/GLOSSAIRE-LOL.md) (guide français, PR #9). Ce document donne, pour chaque terme, ce que disent vraiment les joueurs anglophones. Il complète le tableau de `DRAFT-ux/docs/ux/PLAN-I18N-FR-EN.md` § 5 sans le refaire.
Règle d'or : **pas de mot-à-mot**. On traduit l'idée comme un joueur Emerald l'écrirait dans un chat ou sur Reddit, en restant lisible pour un Silver.

## 1. Ton et principes

| Français | Anglais | Remarque |
|---|---|---|
| Tutoiement (« ton équipe », « tu verrouilles ») | « you », « your team » | Direct, sans « please ». Pas de « the user ». |
| Phrases courtes, une idée | Idem, souvent plus courtes | L'anglais tolère moins les deux-points : « X moves ahead. Reason: … » plutôt que « X : … : … ». |
| Impératif pour un conseil (« Pousse ta vague ») | Impératif (« Shove your wave ») | Pas de « You should ». |
| « DRAFT pense que » | « DRAFT thinks » / « DRAFT's best guess » | Assumer l'incertitude sans jargon. |
| Majuscule seulement en début de phrase | Sentence case partout (« Close call », pas « Close Call ») | Y compris titres et boutons. |
| Espace avant « : » et « % » | Pas d'espace (« Note: », « +3% ») | Affaire de catalogue. |
| Guillemets « … » | "…" (guillemets droits) | Dans le JSON, échapper `\"`. |
| Nombres « 1,5 » | « 1.5 » | `fmtNumber`. |

## 2. Rôles et positions

| Français (DRAFT) | Libellé anglais retenu | Nom officiel Riot (client) | Dans une phrase |
|---|---|---|---|
| Top | Top | Top | « in top lane », « the top laner » |
| Jungle | Jungle | Jungle | « in the jungle », « the jungler » |
| Mid | Mid | Middle | « in mid lane », « the mid laner » |
| ADC | ADC | Bottom | « the ADC », « bot lane » ; « Bot » accepté comme synonyme dans la recherche |
| Support | Support | Support (« Utility » dans l'API) | « the support » |
| le {rôle} d'en face | enemy {role} | — | « Enemy mid: Ahri (likely) » |
| ton vis-à-vis | your lane opponent | — | « Your lane opponent isn't locked in yet. » |
| jungler, laner | jungler, laner | — | identiques |

Classes Riot (Data Dragon) : on garde les noms anglais officiels en anglais : Fighter, Mage, Assassin, Tank, Marksman, Support (le français dit Combattant, Mage, Assassin, Tank, Tireur, Support).
Rangs : Iron, Bronze, Silver, Gold, Platinum, Emerald, Diamond, Master, Grandmaster, Challenger ; file : « Ranked Solo/Duo ».

## 3. Termes de jeu

| Français retenu | Anglais (usage joueur) | Remarque |
|---|---|---|
| matchup | matchup | « Good matchup into Ahri », « into » plus naturel que « against » chez les joueurs ; DRAFT écrit « against » dans les phrases longues, « into » dans les étiquettes courtes. |
| matchup direct (critère) | Lane matchup | |
| sûr en blind | Blind-safe / safe blind pick | « Blind-safe: bad in only 1 of 41 matchups in the table. » |
| blind pick | blind pick | « en blind » → « blind » (« good blind ») |
| counter, bon contre | counter, counters | « Counters Zed », « a Zed counter » ; jamais « counter-pick » comme nom vague. |
| comp | comp | « their comp », « your comp » |
| pick | pick | « Your pool has 4 picks. » |
| pool | pool, champion pool | |
| ban | ban | « Suggested ban: Katarina » |
| flex (pick) | flex pick | « can flex mid or top » |
| frontline | frontline | « No allied frontline to protect him. » |
| backline | backline | |
| tank, bruiser | tank, bruiser | |
| carry fragile | squishy carry | « 2 squishy carries to punish: Jinx, Lux » |
| carry | carry | |
| dive, champion de dive | dive, diver | « Lots of dive on their side », « 2 divers » |
| engage | engage | « Engage on Jinx once your team follows. » |
| disengage | disengage | |
| peel | peel | « Good peel against divers », « peel for your ADC » |
| poke | poke | « Heavy poke », « poke comp » |
| burst | burst | « High burst on one target » |
| CC, CC ciblé | CC, point-and-click CC | « targeted CC » compris aussi ; « 2 point-and-click CC: Leona, Lissandra » |
| all-in | all-in | |
| trade, échange | trade | « Look for short trades. » |
| teamfight | teamfight | « Big teamfight impact » |
| split push | split push | |
| kite | kite | « Kite back toward your frontline. » |
| waveclear | waveclear | « Good waveclear » |
| vague | wave | « Shove your wave before you roam. » |
| sbires | minions | « 15 minions are worth about a kill. » |
| farm, farmer | farm, CS | « Farm at max range. » |
| roam | roam | |
| gank | gank | |
| lane | lane | « in lane », « laning phase » |
| early, mid game, late | early game, mid game, late game | |
| scaling, scale | scaling, scales | « Scales well into late game. » |
| snowball | snowball | « Snowballs off the first kill. » |
| tempo | tempo | |
| map | map | « Strong map pressure » |
| vision, ward, control ward | vision, ward, control ward | |
| kill | kill | |
| ult | ult | « Save your ult for… » |
| cooldown | cooldown | |
| sorts d'invocateur | summoner spells, summs | Flash, Teleport (TP) |
| AD, AP | AD, AP | « Lots of AP on their side. » |
| objet | item | |
| portée, mêlée, distance | range, melee, ranged | « Melee champion (175 range) » |
| mobilité | mobility | |
| sustain | sustain | |
| objectifs : dragon, Nashor, Héraut, tour | objectives: dragon, Baron (Nashor), Herald, tower | « Baron » est l'usage joueur. |
| enchanteur, utilitaire | enchanter, utility | |
| win condition, « Gagne si » | win condition, « Win if » | Étiquette : « Win condition ». |
| plan de jeu | game plan | |
| menaces, cibles | threats, targets | |

## 4. Libellés de l'interface

| Concept | Français retenu | Anglais |
|---|---|---|
| Badge net / serré / égalité | Choix net · Choix serré · Égalité | Clear pick · Close call · Tie |
| Nom maintenu | Presque à égalité · reste affiché | Nearly tied · stays shown |
| 1er du classement | Recommandé | Recommended |
| Devant de peu | Devant de peu | Slightly ahead |
| Bouton principal | Je joue {champion} | Play {champion} |
| Bouton fait | {champion} noté | {champion} logged |
| Pire critère | Risque | Risk |
| Champion adverse le plus menaçant | Menace | Threat |
| Ban conseillé | Ban conseillé | Suggested ban |
| Onglets | Plan · Analyse · Conseils · Duel · Parties | Plan · Analysis · Tips · Matchup · Games |
| Critères | Matchup direct · Sûr en blind · Carrys à punir · Frontline adverse · Résistance au dive · Engage et CC adverses · Poke adverse · Besoin de peel · Pression sur la map | Lane matchup · Blind-safe · Carries to punish · Enemy frontline · Dive resistance · Enemy engage & CC · Enemy poke · Needs peel · Map pressure |
| Jauges | Frontline · Carrys fragiles · Dive · CC ciblés · Engage | Frontline · Squishy carries · Dive · Point-and-click CC · Engage |
| Profils adverses | Tank · Bruiser · Carry fragile · Dive · CC · Engage · Utilitaire | Tank · Bruiser · Squishy carry · Diver · CC · Engage · Utility |
| Réglage frontline | Compter sur une frontline dans mon équipe | Count on a frontline in my team |
| Mes statistiques, Mon pool, Journal | Mes statistiques, Mon pool, Journal | My stats, My pool, Journal |
| Réglages | Réglages | Settings |
| Langue (sélecteur) | Langue : Automatique (Windows) · Français · English | Language: Automatic (Windows) · Français · English |

## 5. Mots de certitude

| Situation | Français | Anglais |
|---|---|---|
| Bilan sur de vraies parties | sur {n} parties | over {n} games |
| Peu de parties | peu de parties | small sample |
| Avis écrit à la main | écrit à la main, avis général | hand-written, general advice |
| Donnée de Riot | donnée de Riot | Riot data |
| Confiance moyenne | probable | likely |
| Confiance élevée | presque sûr | almost certain |
| 1 ou 2 champions vus | hypothèse | guess |
| Plusieurs candidats | pas identifié | not identified |
| Rôle pas encore pris | pas encore choisi | not picked yet |
| Réglage du joueur | supposé(e) (réglage) | assumed (setting) |
| Parties importées | parties importées | imported games |

« Probable » et « likely », « presque sûr » et « almost certain » : un seul mot par niveau, partout.

## 6. Phrases types

| Type | Français | Anglais |
|---|---|---|
| Conseil net | Twisted Fate passe devant. Raison : sûr en blind (mauvais dans 1 matchup sur 41 du tableau). | Twisted Fate moves ahead. Reason: blind-safe (bad in only 1 of 41 matchups in the table). |
| Raison de comp | Beaucoup d'AP en face. | Lots of AP on their side. |
| Raison de comp | 2 champions de dive contre un champion peu mobile : Zed, Kha'Zix | 2 divers against an immobile champion: Zed, Kha'Zix |
| Peel | Bon peel contre leurs 2 champions de dive | Good peel against their 2 divers |
| Plan de lane | Contre Yasuo, cherche des trades courts, puis ressors avec ton E. | Against Yasuo, look for short trades, then get out with your E. |
| Plan de teamfight | Garde ta carte dorée pour Zed ou pour le premier qui plonge. | Save your Gold Card for Zed or whoever dives first. |
| Incertitude | Mid d'en face : Ahri (probable). | Enemy mid: Ahri (likely). |
| Hypothèse | Mid ? Sylas, hypothèse (1 champion vu) | Mid? Sylas, a guess (1 champion seen) |
| Deux hypothèses | Si c'est Yasuo : 1er choix. Si c'est Yone : à éviter. | If it's Yasuo: top pick. If it's Yone: avoid. |
| Choix serré | Kog'Maw passe devant de peu. Fizz reste affiché tant que l'écart reste sous 2 points. | Kog'Maw is slightly ahead. Fizz stays shown while the gap is under 2 points. |
| Égalité | Même score. Warwick passe en premier : son plus gros point faible pèse moins. | Same score. Warwick goes first: his biggest weakness weighs less. |
| Réglage | Pas de frontline alliée supposée (réglage). | No allied frontline assumed (setting). |
| Préférence | Tu as plus de parties sur Fizz (120 contre 12). | You have more games on Fizz (120 vs 12). |
| Draft incomplète | Ce conseil peut encore changer. | This advice can still change. |
| Erreur d'import | Fichier illisible : rien n'a été modifié. | Unreadable file: nothing was changed. |
| Erreur Riot | Limite de requêtes de Riot atteinte : reprise dans 30 secondes. Rien n'est perdu. | Riot rate limit reached: resuming in 30 seconds. Nothing is lost. |
| Mise à jour | La version 3.0.1 est disponible (tu as la 3.0.0). | Version 3.0.1 is available (you have 3.0.0). |
| Sécurité | Rien n'est envoyé au client LoL : tu verrouilles toi-même ton champion. | Nothing is sent to the League client: you lock in your champion yourself. |

## 7. Pièges

- « Mid adverse » ≠ « adverse mid » : écrire « enemy mid ».
- « Contrôle » (CC) ≠ « control » (« control mage » est un autre sens) : écrire « CC ».
- « Carte » (map) ≠ « card » ; mais « carte dorée » = « Gold Card » (sort de Twisted Fate, nom officiel).
- « Bilan » : « record » (V/D) ou « stats », pas « assessment ».
- « Journal » : garder « Journal » (pas « log », qui évoque un fichier technique).
- « Draft » : identique ; « draft aléatoire » → « random draft ».
- Pas de « counterpick » comme verbe vague ; pas de « OP », « broken » : DRAFT reste factuel.
