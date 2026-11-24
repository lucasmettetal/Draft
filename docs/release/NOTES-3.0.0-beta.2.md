# DRAFT 3.0.0-beta.2 : préversion de test

**Préversion pour un petit groupe de testeurs.** Elle n'a **pas de mise à jour automatique** : télécharge les nouvelles versions à la main. Ta version 2.x installée n'est pas touchée et ne propose jamais cette préversion.

DRAFT n'est ni approuvé ni soutenu par Riot Games. Il n'agit jamais dans le client : tu choisis, bannis et verrouilles toujours toi-même.

English version: [NOTES-3.0.0-beta.2.en.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.2/docs/release/NOTES-3.0.0-beta.2.en.md).

## Ce qui change depuis la beta.1

- **Les panneaux deviennent l'écran principal.**
  - Tout se décide sans ouvrir la grande fenêtre.
  - Lecture immédiate : rôle, conseil, menace, champion consulté et action.
  - Lecture approfondie dans des sections dépliables : alternative, pool, plan de jeu, menaces, bans conseillés et critères.
  - Panneau fermé à la main (gauche, droit ou compact) : il reste fermé pendant la session. Pour le rouvrir : icône DRAFT > « Afficher les panneaux ».
  - Panneau gauche fermé : la composition adverse reste accessible dans le panneau droit, dans une section repliable.
  - En compact, les 5 adversaires se replient d'un clic.
- **« Dans League » : ton pick lu dans le client.** Avec la lecture du client, le panneau affiche « League : X (survolé) », puis « verrouillé ✓ », échange compris. Le champion que tu consultes dans DRAFT n'en dépend pas.
- **Journal automatique.**
  - Avec la lecture du client, en draft normale ou classée, ta partie est notée **au lancement**, avec ton champion final. Tu n'as plus de « Je joue » à cliquer.
  - Sans lecture, ou dans un autre mode, « Je joue » reste là.
  - Une seule entrée par partie. Aucun résultat inventé.
- **Drafts annulées** (dodge, retour au salon) : elles restent visibles avec l'étiquette « Annulée », mais ne comptent jamais comme une partie. La draft suivante est bien notée.
- **Champions indisponibles.**
  - Les champions bannis (des deux équipes) et ceux verrouillés par un allié sont marqués dans ton pool : « Banni », « Pris par un allié », « Pris en face ». Ils ne peuvent pas être confirmés.
  - Si le conseil devient indisponible, une phrase te nomme le suivant.
  - Les bans conseillés ne citent plus un champion déjà banni ou pris.
  - Le calcul ne change pas : seuls les candidats indisponibles sont retirés.
- **Écran de fin de partie.** À la fin de la partie, le panneau affiche ton champion en grand, avec deux gros boutons, Victoire et Défaite. Si tu l'as raté, la carte « Dernière partie » de l'Accueil propose la même saisie.
- **Mes statistiques refaites.**
  - Trois onglets : vue d'ensemble, journal DRAFT et historique Riot.
  - Le statut de chaque partie est visible : « En attente », « À confirmer », « Annulée » ou le résultat.
  - Pas de pourcentage tant qu'il y a moins de 10 parties : une jauge montre ce qui manque.
  - Les parties notées automatiquement comptent comme de vraies parties.

## Lecture du client : ce qui est lu

Elle reste en lecture seule, uniquement sur ton PC, sur les deux mêmes adresses que la beta.1. Dans la sélection, DRAFT lit seulement :
- ton poste ;
- ton propre pick ;
- les identifiants des champions bannis ou verrouillés.

Il ne lit **jamais** le nom, le tag ou l'identifiant d'un joueur, ni le survol ou l'intention d'un allié. Détail : `docs/technique/LCU.md`.

## Particularités de cette préversion

- **Mêmes données que la beta.1** (`%LOCALAPPDATA%\DRAFT-beta`) : tu retrouves ton pool et ton journal. Sans beta.1, le premier lancement copie les données de la 2.x, sans jamais modifier la 2.x.
- **Portable seulement.** Un seul fichier, `DRAFT-Portable.exe`.
- **À côté d'une 2.x :** les deux peuvent tourner. Si une autre DRAFT tourne, la préversion lui laisse Ctrl+Alt+D, et tu vois deux jeux de panneaux : quitte l'une des deux.

## Limites connues

- **Le résultat de la partie se saisit encore à la main** : écran de fin de partie ou Accueil. Sa lecture automatique dans le client est prévue pour une version suivante.
- **Écran de fin de partie :** il dépend de la lecture du client. Sans elle, saisis le résultat depuis l'Accueil.
- **Bans et picks alliés :** format vérifié sur des sessions au format réel reconstitué. Signale tout marquage incorrect.
- **Plusieurs écrans :** pas testé.
- **Ctrl+Alt+D réel :** pas prouvé sur une session sans autre DRAFT.
- **Premier affichage des panneaux :** environ 5 s (création de WebView2), puis instantané.
- **Égalité entre champions de styles différents :** l'explication reste générique.

## Fichiers

- `DRAFT-Portable.exe` : préversion portable, compilée et testée par GitHub Actions.
- `DRAFT-Portable.exe.sig` : signature DRAFT, jointe séparément après la compilation.
