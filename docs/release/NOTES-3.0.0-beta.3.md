# DRAFT 3.0.0-beta.3 : préversion de test

**Préversion pour un petit groupe de testeurs.** Elle n'a **pas de mise à jour automatique** : télécharge les nouvelles versions à la main. Ta version 2.x installée n'est pas touchée et ne propose jamais cette préversion.

DRAFT n'est ni approuvé ni soutenu par Riot Games. Il n'agit jamais dans le client : tu choisis, bannis et verrouilles toujours toi-même.

English version: [NOTES-3.0.0-beta.3.en.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.3/docs/release/NOTES-3.0.0-beta.3.en.md).

## Ce qui change depuis la beta.2

- **Résultat noté automatiquement.**
  - Avec la lecture du client, à la fin d'une partie normale ou classée, DRAFT lit si ton équipe a gagné.
  - L'écran de fin de partie confirme : ton champion en grand et « Victoire notée automatiquement ✓ ».
  - Un petit lien « Corriger » réaffiche Victoire, Défaite et Ignorer.
  - Remake, bilan absent ou DRAFT fermé avant la fin : rien n'est inventé. Les deux boutons restent, ou la carte « Dernière partie » de l'Accueil.
  - Un résultat que tu as saisi toi-même n'est jamais remplacé. Mes statistiques indiquent « lu dans le client ».
- **Rapprochement avec ton historique Riot.**
  - Après un import de ton historique (Mes statistiques > Historique Riot), une partie notée est reliée à la partie Riot qui a le **même identifiant**. Rien n'est deviné par l'heure ou le champion.
  - Elle affiche « Confirmé par Riot », avec K/D/A, durée et patch.
  - **Riot fait foi** dans les statistiques. Si ton résultat noté diffère, un « Désaccord » s'affiche. Rien n'est effacé.

## Lecture du client : ce qui est lu

Elle reste en lecture seule, uniquement sur ton PC. Elle passe désormais par **trois** adresses : les deux de la beta.2, et le **bilan de fin de partie**.

Dans ce bilan, DRAFT lit seulement :
- l'identifiant de la partie ;
- ton équipe ;
- l'équipe gagnante ;
- l'indication de remake.

Il ne lit **jamais** les noms ni les statistiques des autres joueurs, ni tes propres statistiques.

Si tu avais déjà activé la lecture du client, rien ne t'est redemandé. Les textes de Réglages > League le précisent. Détail : `docs/technique/LCU.md`.

## Particularités de cette préversion

- **Mêmes données que la beta.1 et la beta.2** (`%LOCALAPPDATA%\DRAFT-beta`) : tu retrouves ton pool et ton journal.
- **Portable seulement.** Un seul fichier, `DRAFT-Portable.exe`.
- **À côté d'une 2.x :** les deux peuvent tourner. Si une autre DRAFT tourne, la préversion lui laisse Ctrl+Alt+D et tu vois deux jeux de panneaux : quitte l'une des deux.

## Limites connues

- **Bilan de fin de partie :** son format n'est pas documenté par Riot. Si DRAFT ne sait pas le lire, il revient aux boutons. Signale-le : le diagnostic de connexion indique `bilan=lu|absent|illisible|remake`, sans nom.
- **Historique Riot :** seules les parties classées solo/duo sont importées, et seules les parties notées depuis la beta.2 portent l'identifiant nécessaire. L'import reste manuel, avec ta clé Riot.
- **Plusieurs écrans :** pas testé.
- **Ctrl+Alt+D réel :** pas prouvé sur une session sans autre DRAFT.
- **Premier affichage des panneaux :** environ 5 s (création de WebView2), puis instantané.

## Fichiers

- `DRAFT-Portable.exe` : préversion portable, compilée et testée par GitHub Actions.
- `DRAFT-Portable.exe.sig` : signature DRAFT, jointe séparément après la compilation.
