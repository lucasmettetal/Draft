# Connecteur LCU : lecture seule

**État : disponible en option depuis le 4 novembre 2026, par décision du propriétaire du projet. Riot n'a pas confirmé que cette lecture est autorisée.**

Il faut à la fois `LcuGate.ReadingEnabled = true` dans `src/LcuConnector.cs` et l'option « Lecture du client Riot » cochée dans les réglages, décochée par défaut.

**Publication :** le code et l'exécutable sont publics sur GitHub, sans attendre la réponse de Riot. Si Riot refuse cet usage, repasser `ReadingEnabled` à `false` et republier.

## Politiques vérifiées le 3 novembre 2026, relues le 4 novembre pour les trois pages principales

| Source | Ce qu'elle dit |
|---|---|
| [Documentation League](https://developer.riotgames.com/docs/lol) | L'API du client n'est pas prise en charge pour les applications tierces. Riot demande quels endpoints sont utilisés et comment. |
| [Politique générale](https://developer.riotgames.com/policies/general) | Tout produit doit être enregistré et audité, et un produit qui utilise l'API du client doit lire sa documentation et s'enregistrer. Toute nouvelle fonction ou tout changement doit aussi être audité. Il ne doit pas retirer les décisions du joueur, ni désanonymiser des joueurs. Aucune exception n'est écrite pour un outil privé. |
| [Changements de la politique LCU](https://www.riotgames.com/en/DevRel/changes-to-the-lcu-api-policy) (2019) | Seuls les endpoints d'une liste approuvée, non publiée dans ce billet, sont autorisés. Il faut contacter Riot avant de publier une application. Son usage était interdit aux joueurs en Corée à cette date. |
| [Applications tierces](https://support.riotgames.com/en-us/riot/events/third-party-applications) | Interdit d'exposer une information masquée, d'agir à la place du joueur, ou de tirer des conclusions pour lui pendant le jeu. En cas de doute, Riot conseille de ne pas utiliser l'application. |
| [Accès mémoire](https://support.riotgames.com/en-us/riot/performance/game-memory-access-removed-for-third-party-apps) (29 septembre 2026) | Présente l'API officielle comme « le seul mécanisme autorisé » pour récupérer les données de jeu. Le billet ne cite pas le client ni le LCU : sa portée sur ce connecteur reste à clarifier. |

Aucune de ces pages ne dit si `gameflow-phase` et `champ-select/session` figurent parmi les endpoints approuvés. Ce connecteur ne doit donc pas être présenté comme conforme.

## Endpoints

Liste fermée, contrôlée par `LcuEndpoints.IsAllowed`.

| Endpoint | Méthode | Usage | Données conservées |
|---|---|---|---|
| `/lol-gameflow/v1/gameflow-phase` | GET | Phase de jeu, environ une fois par seconde | Nom de la phase |
| `/lol-champ-select/v1/session` | GET | Seulement pendant `ChampSelect` | `theirTeam[].championId` supérieur à 0 ; ton poste ; ton pick (beta.2) ; champions bannis et champions verrouillés par tes alliés (beta.2) |

Depuis la V2.16 bêta, la même réponse de session fournit aussi **ton propre poste** (`assignedPosition` de ta case dans `myTeam`), pour proposer le bon profil. Champ non documenté par Riot, vérifié seulement en simulation : voir [PROFILS.md](PROFILS.md).

Depuis la 3.0 beta.2 (journal automatique), la même réponse fournit aussi **ton propre pick** : ton action de pick (survolée, verrouillée), ta présélection (`championPickIntent` de ta case), ton champion final (`championId` de ta case, après échange), `timer.phase` et `gameId`. Seule ta case est ouverte. Voir [JOURNAL.md](JOURNAL.md).

Depuis la 3.0 beta.2 (disponibilité), la même réponse fournit aussi les **champions qui ne sont plus disponibles** : bannis, pris par un allié, pris en face. Seuls des identifiants de champions sont lus. Voir [Disponibilité des champions](#disponibilité-des-champions-beta2).

Sont volontairement ignorés : noms, tags et identifiants de joueurs (`gameName`, `tagLine`, `summonerId`, `puuid` et leurs versions masquées), sorts, skins, intentions et survols des alliés, postes des alliés, actions en cours ou à venir.

## Disponibilité des champions (beta.2)

But : DRAFT ne conseille pas un champion qui n'est plus disponible. Lecture dans la session déjà demandée, sans nouvelle adresse ni autre verbe que GET (`LcuParser.ParseUnavailable`, `src/LcuConnector.cs`).

**Champs lus, et seulement ceux-là :**

| Raison | Champs | Condition |
|---|---|---|
| Banni | `actions[][]` : `type`, `completed`, `championId` | `type` = `"ban"`, `completed` = `true`, `championId` > 0. Les deux équipes |
| Banni | `bans.myTeamBans[]`, `bans.theirTeamBans[]` | Identifiant > 0. Lus en plus des actions : l'un ou l'autre suffit |
| Pris par un allié | `localPlayerCellId` ; `myTeam[].cellId` ; `actions[][]` : `type`, `completed`, `championId`, `actorCellId` | `type` = `"pick"`, `completed` = `true`, `championId` > 0, `actorCellId` = une case de `myTeam` autre que la tienne |
| Pris par un allié | `myTeam[case de cet allié].championId` | Seulement quand son action de pick est terminée : champion final, à jour après un échange. Sinon, celui de l'action |
| Pris en face | `theirTeam[].championId` | > 0, comme pour la composition adverse |

**Jamais lu :** nom, tag, `summonerId`, `puuid` (et versions masquées) d'un joueur ; `championPickIntent` d'un allié (présélection) ; un survol (action en cours, `isInProgress`) ; un ban à venir ; le `championId` d'une case alliée dont le pick n'est pas terminé ; poste, sorts, skin d'un allié. Ton propre pick n'est jamais indisponible pour toi. Sans `localPlayerCellId`, aucun pick allié n'est déduit.

**Ce qui est gardé :** une liste d'identifiants de champions avec leur raison (`ban`, `ally`, `enemy`), en mémoire, versionnée (`AvailabilityTracker`) et envoyée à chaque page dans l'état du programme (`unavailable { v, key, items }`). Rien n'est écrit sur le disque. Le journal de diagnostic ajoute seulement des nombres : `bans=N allies=M`.

**Effet sur le conseil :** un champion banni ou pris par un allié sort de la liste candidate AVANT le classement, comme un pick adverse (`UNAVAILABLE`, `web/draft-engine.js`). Le moteur, ses poids et ses scores ne changent pas. Un pick adverse sort, comme avant, par la composition (`state.enemies`) : une correction à la main de la composition reste respectée.

**Robustesse :** session tronquée ou abîmée, la dernière liste connue reste. Lecture perdue en pleine sélection, la liste reste aussi : un ban ou un pick déjà vu ne revient pas. Nouvelle sélection : liste vide.

**Sans lecture du client** (option décochée, client fermé, navigateur, Simulateur) : rien ne change, rien n'est déduit. Un marquage manuel (« banni », « pris ») pourrait venir plus tard ; il n'existe pas encore.

**Tests :** selftest au format réel (bans faits, en cours et à venir ; bans dans les actions ou dans `bans` ; pick allié verrouillé ; survol et intentions d'alliés ignorés ; échange ; ton pick ; sans ta case ; réponse abîmée ; test négatif : nom, tag, puuid, summonerId, survol et intention d'un allié changent, la lecture non). Page `web/draft-dispo-tests.html` (moteur et liste candidate). Banc moteur à 0 différence.

## Garanties techniques

- **Lecture seule :** `ILcuTransport` n'expose que `GetAsync`, et le seul transport réel fixe `Method = "GET"`. Aucun abonnement WebSocket : la lecture périodique suffit et reste plus simple.
- **Local :** l'adresse est fixée à `127.0.0.1`, et le certificat auto-signé n'est accepté que pour cette requête.
- **Client figé :** une requête est abandonnée après 1,5 seconde, une lecture complète après 3,5 secondes, et l'application revient à l'estimation visuelle et au mode manuel.
- **Reconnexion :** `LcuSession` oublie les identifiants dès qu'une lecture échoue ou que le client se ferme. Elle relit le lockfile au plus toutes les 3 secondes, donc un client redémarré est retrouvé sans action.
- **Réponse incomplète :** une session tronquée, absente ou expirée ne vide pas la composition. La dernière connue reste affichée.
- **Identifiants temporaires :** le port et le mot de passe restent en mémoire. Ils ne sont ni écrits, ni journalisés, ni ajoutés aux préférences.
- **Aucune action :** rien sur les picks, les bans ou les sorts. Pas d'injection, de lecture mémoire ni d'interaction avec Vanguard.
- **Corrections conservées :** une nouvelle lecture ajoute seulement les nouveaux champions et retire ceux qui ont disparu de la lecture. Un champion ajouté ou retiré à la main le reste. Le mid reste une supposition, car le rôle des adversaires n'est pas lu.

## Comportement

| Phase LCU | Phase DRAFT |
|---|---|
| `ChampSelect` | Draft : panneaux affichés |
| `GameStart`, `InProgress`, `Reconnect` | Partie en cours : panneaux masqués |
| `None`, `Lobby`, `Matchmaking`, `ReadyCheck`, `WaitingForStats`, `PreEndOfGame`, `EndOfGame`, `TerminatedInError`, `CheckedIntoTournament` | Hors draft : panneaux masqués |
| Phase inconnue | Incertaine : panneaux masqués, sauf draft déjà en cours |
| Client injoignable | Retour à l'estimation visuelle et au raccourci manuel |

## Validation

Le scénario de `--e2e` déroule une draft complète sur `SimulatedLcuTransport` : accueil, lobby, début de sélection, adversaires ajoutés et changés, réponses abîmées, requête bloquée, déconnexion, redémarrage, annulation, chargement, partie, fermeture. Ces données simulées suivent des sources communautaires : elles ne prouvent pas le format réel du client.

**Validé sur une vraie partie personnalisée le 4 novembre 2026 :** phases `None`, `Lobby`, `ReadyCheck`, `ChampSelect`, et adversaires lus de 0 à 5. Le journal ne contient pas les noms : l'exactitude des champions se vérifie à l'écran. `GameStart` et `InProgress` n'ont pas encore été observés.

Le journal de diagnostic indique l'état réel de la lecture avec `lcu=phase/nombre d'adversaires`, ou `lcu=absent(erreur)`, sans identifiant ni nom.

## Note pour Riot

Une demande de précisions a été envoyée à Riot par le propriétaire du projet, sans réponse connue au 4 novembre 2026. Cette note résume ce qu'il faut clarifier, notamment pour un enregistrement sur le portail développeur.

> DRAFT est une application Windows qui conseille un pick mid à partir de la composition adverse visible. Nous souhaitons lire localement, en GET uniquement, `/lol-gameflow/v1/gameflow-phase` et `/lol-champ-select/v1/session`, pour afficher nos fenêtres pendant la sélection et récupérer les champions adverses déjà verrouillés et visibles, ainsi que les champions déjà bannis ou verrouillés par les alliés du joueur, pour ne pas conseiller un champion indisponible. Nous n'effectuons aucune écriture ni action automatique, et nous ne lisons ni noms, ni identifiants de joueurs, ni intentions de pick. Le joueur garde chaque décision.
>
> Ces deux endpoints sont-ils autorisés pour cet usage, y compris pour une distribution publique ? Un conseil de pick pendant la sélection des champions est-il acceptable au regard de vos règles sur les « conclusions pendant le jeu » ?
