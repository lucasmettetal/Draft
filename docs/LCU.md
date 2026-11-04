# Connecteur LCU : lecture seule et verrouillé

**État : implémenté, testé sur des données simulées, jamais connecté à un vrai client.**

Le connecteur est fermé par `LcuGate.RiotConfirmed = false` dans `src/LcuConnector.cs`. Tant que cette valeur n'est pas modifiée et l'application recompilée, cocher l'option ne provoque aucune lecture du lockfile ni aucune requête.

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
| `/lol-champ-select/v1/session` | GET | Seulement pendant `ChampSelect` | `theirTeam[].championId` supérieur à 0 |

Sont volontairement ignorés : noms et identifiants de joueurs, `championPickIntent`, sorts, équipe alliée, bans.

## Garanties techniques

- **Lecture seule :** `ILcuTransport` n'expose que `GetAsync`, et le seul transport réel fixe `Method = "GET"`. Aucun abonnement WebSocket : la lecture périodique suffit et reste plus simple.
- **Local :** l'adresse est fixée à `127.0.0.1`, et le certificat auto-signé n'est accepté que pour cette requête.
- **Client figé :** une requête est abandonnée après 800 ms, une lecture complète après 2 secondes, et l'application revient à l'estimation visuelle et au mode manuel.
- **Reconnexion :** `LcuSession` oublie les identifiants dès qu'une lecture échoue ou que le client se ferme. Elle relit le lockfile au plus toutes les 3 secondes, donc un client redémarré est retrouvé sans action.
- **Réponse incomplète :** une session tronquée, absente ou expirée ne vide pas la composition. La dernière connue reste affichée.
- **Identifiants temporaires :** le port et le mot de passe restent en mémoire. Ils ne sont ni écrits, ni journalisés, ni ajoutés aux préférences.
- **Aucune action :** rien sur les picks, les bans ou les sorts. Pas d'injection, de lecture mémoire ni d'interaction avec Vanguard.
- **Corrections conservées :** une nouvelle lecture ajoute seulement les nouveaux champions et retire ceux qui ont disparu de la lecture. Un champion ajouté ou retiré à la main le reste. Le mid reste une supposition, car le rôle des adversaires n'est pas lu.

## Comportement

| Phase LCU | Phase DraftMid |
|---|---|
| `ChampSelect` | Draft : panneaux affichés |
| `GameStart`, `InProgress`, `Reconnect` | Partie en cours : panneaux masqués |
| `None`, `Lobby`, `Matchmaking`, `ReadyCheck`, `WaitingForStats`, `PreEndOfGame`, `EndOfGame`, `TerminatedInError`, `CheckedIntoTournament` | Hors draft : panneaux masqués |
| Phase inconnue | Incertaine : panneaux masqués, sauf draft déjà en cours |
| Client injoignable | Retour à l'estimation visuelle et au raccourci manuel |

## Validation

Le scénario de `--e2e` déroule une draft complète sur `SimulatedLcuTransport` : accueil, lobby, début de sélection, adversaires ajoutés et changés, réponses abîmées, requête bloquée, déconnexion, redémarrage, annulation, chargement, partie, fermeture. Ces données simulées suivent des sources communautaires : elles ne prouvent pas le format réel du client.

## Pour l'activer

1. Enregistrer le projet sur le [portail développeur](https://developer.riotgames.com/) et y laisser la note ci-dessous.
2. Obtenir une réponse écrite de Riot.
3. Mettre `LcuGate.RiotConfirmed` à `true`, adapter le test du verrou, recompiler.
4. Valider sur une vraie draft : les noms de phases et le format de session viennent de sources communautaires, jamais confirmés sur un vrai client.
5. Cocher l'option dans les réglages. Elle reste désactivée par défaut.

## Note proposée pour Riot

> DraftMid est une application Windows qui conseille un pick mid à partir de la composition adverse visible. Nous souhaitons lire localement, en GET uniquement, `/lol-gameflow/v1/gameflow-phase` et `/lol-champ-select/v1/session`, pour afficher nos fenêtres pendant la sélection et récupérer les champions adverses déjà verrouillés et visibles. Nous n'effectuons aucune écriture ni action automatique, et nous ne lisons ni noms, ni identifiants de joueurs, ni intentions de pick. Le joueur garde chaque décision.
>
> Ces deux endpoints sont-ils autorisés pour cet usage, y compris pour une distribution publique ? Un conseil de pick pendant la sélection des champions est-il acceptable au regard de vos règles sur les « conclusions pendant le jeu » ?
