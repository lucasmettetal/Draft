# Guide d'utilisation (V2.14)

## Installation

Il faut Windows 10 ou 11, avec Microsoft Edge WebView2. S'il manque, DraftMid l'indique au lancement.

- **Portable :** lance `DraftMid-Portable.exe`, sans installation.
- **Installée :** lance `DraftMid-Setup.exe`, sans droits administrateur, avec un raccourci dans le menu Démarrer.

L'exécutable n'est pas signé : Windows peut afficher un avertissement. Pour obtenir la V2.14, [compile-la](../../README.md#installation). Les releases publiques sont plus anciennes et ne sont pas recommandées : voir [leur statut](../technique/ANCIENNES-RELEASES.md).

## Configuration

Au lancement, seule une icône apparaît près de l'horloge. Ouvre **Réglages** depuis son menu.

| Réglage | Effet |
|---|---|
| Lire la draft depuis le client LoL | Remplit les adversaires. Décoché par défaut, et Riot n'a pas confirmé que c'est autorisé |
| Afficher les panneaux automatiquement | Pendant la draft |
| Coller les panneaux au client | Suit la fenêtre de LoL |
| Un seul panneau | Pour les petits écrans |
| Toujours au premier plan | Au-dessus des autres fenêtres |
| Démarrer avec Windows | Décoché par défaut |

`Ctrl+Alt+D` affiche ou masque les panneaux à tout moment.

## Profils et pool

Dans **Réglages > Mon profil**, choisis ton rôle et tes champions. Tu peux en exclure un, ou choisir un favori : à score égal, il passe devant.

Le tableau de matchups écrit à la main ne couvre que le mid avec Twisted Fate, Fizz, Kog'Maw et Xerath. Ailleurs, le conseil repose sur des fiches générales, jusqu'à ce que tu calcules des bilans.

## Détection de la draft

- **Avec la lecture du client :** phase et adversaires verrouillés sont lus localement, sans rien modifier.
- **Sans elle :** tu saisis les adversaires dans le panneau gauche. Une estimation visuelle, peu fiable, gère l'affichage.

L'adversaire direct reste une supposition, car le client ne donne pas les rôles adverses. Corrige-le d'un clic dans la composition. Détails : [détection](../technique/DETECTION.md).

## Recommandations

- **Choix net :** le conseil mène d'au moins 2 points.
- **Choix serré :** les deux champions se défendent.
- **Pendant la draft :** le nom ne change que pour un vrai écart, pour éviter le clignotement.

Le score compare tes champions : ce n'est ni un winrate, ni une probabilité. Le clic sur « Je joue » note ton choix dans ton journal, sans rien envoyer au client. Tu verrouilles toujours toi-même. Détails : [moteur](../technique/MOTEUR.md).

## Données avec une clé Riot

Tout est facultatif. La [clé](https://developer.riotgames.com) est saisie par toi, masquée et jamais enregistrée. Elle est réservée à un usage privé.

| Action | Durée approximative | Résultat |
|---|---|---|
| **Mettre à jour les rôles** (réglages) | 40 minutes | Meilleure déduction de l'adversaire direct |
| **Mettre à jour les matchups** (réglages) | 65 minutes | Bilans de ton pool, à ton rôle, sur 30 jours |

**Importer tes parties :** dans la V2.14, cela passe par un script, sans PowerShell à écrire : télécharge `tools/draft-import-stats.bat` et `tools/draft-import-stats.ps1` dans un même dossier, lance le `.bat`, puis, dans le menu de l'icône, choisis le fichier `draft-stats.js` produit. Ces statistiques sont affichées, sans changer le classement.

L'import depuis les réglages n'existe que sur la [candidate V2.15](../experimental/README.md).

## Sauvegarde et restauration

Tes données sont dans `%LOCALAPPDATA%\DraftMid` : préférences (`settings.json`), navigateur intégré (profil, préférences et journal), calculs et statistiques.

- **Journal :** bouton **Télécharger le journal** dans l'onglet Parties, et import du fichier dans les réglages. Rien n'est supprimé à l'import.
- **Remplacer une version :** `tools/install-local.ps1` sauvegarde l'exécutable et les données dans `%LOCALAPPDATA%\DraftMid-sauvegardes`, et refuse d'agir si le client LoL est ouvert.
- **Revenir en arrière :** `tools/restore-local.ps1 -Sauvegarde <dossier>`.

Ne remplace jamais une version pendant une draft ou une partie.

## Dépannage

| Problème | À faire |
|---|---|
| Aucune fenêtre au lancement | C'est normal : regarde l'icône près de l'horloge, ou fais `Ctrl+Alt+D` |
| Message sur WebView2 | Installe Microsoft Edge WebView2 Runtime |
| Avertissement de Windows | L'exécutable n'est pas signé |
| Panneaux absents pendant la draft | Vérifie la lecture du client et l'affichage automatique, sinon `Ctrl+Alt+D` |
| Panneaux au mauvais endroit | Active « Coller les panneaux au client » |
| Mauvais adversaire direct | Corrige-le dans la composition |
| Clé Riot refusée | Une clé de développement expire après 24 heures : crée-en une nouvelle |
| Raccourci inactif | Une autre application, ou un autre DraftMid, l'occupe |
| Conseil surprenant | Ouvre l'onglet Analyse : chaque point y est expliqué |
