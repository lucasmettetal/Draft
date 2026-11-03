# Intégration Riot : état et conditions

La lecture du client est implémentée côté application, mais verrouillée : voir [LCU.md](LCU.md). DraftMid fonctionne entièrement sans elle.

Politiques consultées le 3 novembre 2026. Elles peuvent évoluer : à relire avant toute activation.

## Ce que disent les sources officielles

- **[Documentation League of Legends](https://developer.riotgames.com/docs/lol) :** l'API du client n'est « pas officiellement prise en charge » pour les applications tierces, sans garantie de documentation, de disponibilité ni d'annonce des changements. Riot demande aussi de déclarer les endpoints utilisés et leur usage.
- **[Politique générale](https://developer.riotgames.com/policies/general) :** tout produit doit être enregistré et audité via le portail développeur. Aucune exception n'est écrite pour un outil privé. Un produit ne doit pas retirer les décisions du joueur, mais il peut les mettre en évidence. Il ne doit pas désanonymiser des joueurs.
- **[Applications tierces](https://support.riotgames.com/en-us/riot/events/third-party-applications) :** sont interdits l'exposition d'informations volontairement masquées, les actions à la place du joueur et le fait de tirer des conclusions pour lui pendant le jeu. La page ne mentionne pas la sélection des champions, et conseille de ne pas utiliser une application en cas de doute.

## Méthodes possibles

| Méthode | Statut | Décision |
|---|---|---|
| Saisie manuelle | Sans contact avec le jeu | Active |
| Simulation | Sans contact avec le jeu | Active, pour les tests et la démonstration |
| Position de la fenêtre via l'API Windows | Lecture du titre, du processus et du rectangle, sans lien avec le contenu du jeu | Active, désactivable |
| Data Dragon | Service officiel de données statiques | Active |
| API officielle Riot pour l'historique | Clé personnelle, prévue pour ses propres statistiques | Script manuel, aucune clé dans l'exécutable |
| API du client (lockfile) | Non prise en charge, statut à clarifier | Lecture seule, verrouillée, jamais connectée |
| Lecture mémoire, injection, contournement de Vanguard | Hors de question | Jamais |
| Reconnaissance visuelle locale du client | Sans contact avec le client, non validée par Riot | Active, comme estimation : voir [DETECTION.md](DETECTION.md) |
| Noms de joueurs masqués en sélection | Information volontairement cachée | Jamais |

## Avant d'ouvrir le verrou

1. Enregistrer le projet sur le portail développeur en décrivant l'usage : lecture seule des champions et bans visibles.
2. Obtenir une réponse écrite de Riot, en demandant explicitement si un conseil de pick en sélection reste acceptable.
3. Limiter la lecture aux champions, aux bans et à la position du joueur, sans action sur le client ni donnée de joueur.
4. Conserver la saisie manuelle en secours.

## Risque résiduel

Riot n'a pas validé DraftMid. Le conseil de pick n'interagit pas avec le client, mais la clause sur les « conclusions pendant le jeu » ne précise pas si elle couvre la sélection des champions. Enregistrer le projet et poser la question reste la démarche la plus sûre.
