# Intégration Riot : état et conditions

La lecture du client est disponible en option, décochée par défaut, sans confirmation de Riot : voir [LCU.md](LCU.md). DRAFT fonctionne entièrement sans elle.

Politiques consultées le 3 novembre 2026, et relues le 4 novembre pour ces trois pages. Elles peuvent évoluer : à relire avant toute activation.

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
| API officielle Riot | Clé personnelle, pour un usage privé | Scripts manuels pour les bilans et les rôles récents, aucune clé dans l'exécutable |
| Statistiques de rôles Meraki Analytics | Fichier public, droits non confirmés | Retirées du dépôt : voir [DONNEES.md](DONNEES.md) |
| API du client (lockfile) | Non prise en charge, statut à clarifier | Lecture seule, option à cocher |
| Lecture mémoire, injection, contournement de Vanguard | Hors de question | Jamais |
| Reconnaissance visuelle locale du client | Sans contact avec le client, non validée par Riot | Active, comme estimation : voir [DETECTION.md](DETECTION.md) |
| Noms de joueurs masqués en sélection | Information volontairement cachée | Jamais |

## Démarche auprès de Riot

Une demande de précisions a été envoyée par e-mail, sans réponse connue au 4 novembre 2026. L'enregistrement sur le portail reste la voie prévue par la politique de Riot.

1. Enregistrer le projet sur le portail développeur en décrivant l'usage : lecture seule de la phase et des champions adverses visibles.
2. Obtenir une réponse écrite de Riot, en demandant explicitement si un conseil de pick en sélection reste acceptable.
3. Limiter la lecture à la phase et aux champions adverses verrouillés, sans action sur le client ni donnée de joueur.
4. Conserver la saisie manuelle en secours.

## Distribution publique

Depuis le 4 novembre 2026, le code et l'exécutable sont publics sur GitHub, par choix du propriétaire du projet et sans réponse de Riot à ce jour. Le workflow publie l'exécutable à chaque tag `v*`. Les points ci-dessous restent à traiter :

- **Audit :** le produit et chacune de ses évolutions passent par le portail développeur.
- **Illustrations :** l'application affiche des portraits de Riot, et la page des applications tierces vise celles qui « allow the use of our assets for free ». Le cadre applicable reste à vérifier.
- **Données de rôles :** le fichier Meraki est retiré, faute de droits confirmés. Publier des rôles calculés demanderait une clé de production, et Riot doit l'autoriser.
- **Identifiants :** aucune clé Riot dans l'exécutable. Les scripts restent séparés.
- **Conseil de pick :** garder plusieurs options et les raisons affichées, car la politique permet de mettre une décision en évidence mais pas de la retirer au joueur.

## Risque résiduel

Riot n'a pas validé DraftMid. Le conseil de pick n'agit pas sur le client, mais la clause sur les « conclusions pendant le jeu » ne précise pas si elle couvre la sélection des champions. Enregistrer le projet et poser la question reste la démarche la plus sûre.
