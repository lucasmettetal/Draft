# DRAFT

*Ton assistant stratégique pour League of Legends.* Anciennement DraftMid : le projet couvre maintenant les cinq rôles.

Application Windows qui aide à choisir son champion pendant la sélection de League of Legends. Elle compare les champions que tu as choisis pour ton rôle, affiche un conseil avec ses raisons, et **le joueur décide toujours lui-même**.

**Pour qui :** les joueurs qui veulent préparer leur choix en quelques secondes, à n'importe quel rôle, sans quitter le client.

**Statut :** projet personnel, non officiel. DRAFT n'est ni approuvé ni soutenu par Riot Games. Les portraits et illustrations appartiennent à Riot Games. L'exécutable n'est pas signé : Windows peut afficher un avertissement.

## Versions

| Version | Où | État |
|---|---|---|
| **V2.14** | Branche `main`, à compiler | **Stable de référence.** Pas encore publiée en release |
| V2.6 à V2.10 | Anciennes releases publiques | **Pas recommandées.** Voir ci-dessous |
| V2.15 | Branche `integration/v2.15-coaching` | **Candidate, non stable** |
| V2.16 bêta (DRAFT) | Branche `feature/draft-profiles-beta` | **Bêta privée, non publiée** : profils par rôle, rôle attribué, export des profils |

Ce document décrit la V2.14. Le détail est dans [l'index de la documentation](docs/README.md).

**Anciennes releases :** GitHub affiche encore la V2.10 comme dernière release, mais aucune V2.11 ou plus récente n'a été publiée. Ces versions anciennes peuvent contenir des données Meraki, dont les droits de redistribution restent à clarifier, et des bilans personnels retirés depuis. La référence Meraki est retirée des fichiers courants, mais reste dans l'historique Git et ces releases : voir [leur statut](docs/technique/ANCIENNES-RELEASES.md).

## Fonctionnalités de la V2.14

- **Profil :** ton rôle et ton pool, parmi tous les champions.
- **Conseil :** un champion, une alternative et leurs raisons, selon la composition adverse.
- **Panneaux autour du client :** composition adverse à gauche, verdict à droite, ou panneau unique.
- **Lecture du client, en option :** les adversaires verrouillés se remplissent seuls.
- **Saisie manuelle :** toujours disponible, et une draft aléatoire pour tester.
- **Analyse :** comparaison, plan de jeu, bans conseillés, règles appliquées.
- **Journal :** tes choix et tes résultats, sur ce PC.

## Fonctionnement avec League of Legends

**Aucune action dans le client :** ni verrouillage, ni modification, ni injection, ni lecture de la mémoire, ni information volontairement masquée.

**La lecture du client est décochée par défaut.** Elle lit localement la phase de jeu et les champions adverses verrouillés. **Riot n'a pas confirmé que cet usage est autorisé, et son activation se fait à tes risques.** Sans elle, tu saisis les adversaires à la main, et une estimation visuelle peu fiable sert de secours.

## Limites

- **Le score compare tes champions :** ce n'est ni un winrate, ni une probabilité. Les règles et les poids sont écrits à la main, et n'ont jamais été validés sur de vraies parties.
- **Adversaire direct :** le client ne donne pas les rôles adverses. C'est une supposition, à corriger d'un clic.
- **Tableau de matchups :** écrit à la main, seulement pour le mid avec Twisted Fate, Fizz, Kog'Maw et Xerath. Ailleurs, le conseil repose sur des fiches générales.
- **Validation réelle :** une partie personnalisée a été testée de bout en bout. Le classé reste à valider.

## Installation

Prérequis : Windows 10 ou 11, avec Microsoft Edge WebView2 (généralement déjà présent).

**Compiler la V2.14**, sans Visual Studio ni Node :

```powershell
git clone https://github.com/lucasmettetal/Draft.git
cd Draft
.\build.ps1
```

Les exécutables sont dans `dist\` : `DRAFT-Portable.exe` sans installation, `DRAFT-Setup.exe` à installer sans droits administrateur.

Au lancement, aucune fenêtre ne s'ouvre : une icône apparaît près de l'horloge. La suite est dans le [guide d'utilisation](docs/utilisateur/GUIDE.md).

## Tes données

Tout reste sur ton PC, dans `%LOCALAPPDATA%\DraftMid`. Il n'y a ni compte, ni télémétrie. Une clé Riot est facultative, saisie par toi, masquée et jamais enregistrée.

## Fonctionnalités expérimentales

**Aucune n'est disponible dans la V2.14.** Coaching pédagogique, affichage des égalités, import Riot dans l'application, profil OP.GG, bans adverses : voir [leur statut](docs/experimental/README.md).

## Développement

- [Architecture](docs/technique/ARCHITECTURE.md)
- [Branches et publication](docs/technique/BRANCHES.md)
- [Validation et tests](docs/technique/VALIDATION.md)
- [Feuille de route V2.15](docs/ROADMAP-V2.15.md)

**Attention :** pousser un tag `v*` publie automatiquement une release. Voir [Branches et publication](docs/technique/BRANCHES.md).

Aucun fichier de licence n'accompagne le projet pour l'instant : le code reste public sans licence de réutilisation accordée.
