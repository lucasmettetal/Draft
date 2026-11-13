# Guide du testeur : DRAFT 3.0.0-beta.1

Merci de tester cette préversion. Elle sert à trouver les erreurs avant la 3.0. English version: [GUIDE-TESTEUR-3.0.0-beta.1.en.md](GUIDE-TESTEUR-3.0.0-beta.1.en.md).

**DRAFT n'est ni approuvé ni soutenu par Riot Games.** Il n'agit jamais dans le client : tu choisis, bannis et verrouilles toujours toi-même.

## En bref

- C'est une **préversion de test** : elle peut contenir des erreurs.
- Elle travaille sur une **copie** de tes données 2.x. Ta 2.x n'est pas modifiée.
- **Pas de mise à jour automatique** : tu télécharges chaque nouvelle préversion à la main.
- Un seul fichier, **portable** : rien à installer.

## 1. Installer la préversion

1. Télécharge `DRAFT-Portable.exe` depuis la page de la préversion `v3.0.0-beta.1` sur GitHub (marquée « Pre-release »).
2. Vérifie son empreinte, si on te l'a donnée. Dans PowerShell : `Get-FileHash .\DRAFT-Portable.exe -Algorithm SHA256`. Les deux valeurs doivent être identiques.
3. Range-le dans un dossier à part, par exemple `C:\DRAFT-beta\`. **Ne remplace pas** le fichier de ta 2.x.
4. Lance-le. Windows peut afficher un avertissement (SmartScreen), car l'exécutable n'a pas de certificat Microsoft : « Informations complémentaires », puis « Exécuter quand même ».
5. Une icône **DRAFT** apparaît près de l'horloge. Au premier lancement, l'Accueil s'ouvre et une bulle dit que DRAFT travaille sur une copie de tes données 2.x.

Il faut Windows 10 ou 11 avec Microsoft Edge WebView2 (présent sur la plupart des PC).

## 2. Cohabiter avec ta 2.x

| Point | Ce qui se passe |
|---|---|
| Données | Au premier lancement, la préversion copie tes réglages, profils, pools, journal et historique de `%LOCALAPPDATA%\DraftMid` vers `%LOCALAPPDATA%\DRAFT-beta`. Elle lit la 2.x sans y écrire. Ensuite, chacune a ses données. |
| Lancement | La préversion et la 2.x peuvent tourner en même temps. Aucune ne ferme l'autre. |
| Démarrage avec Windows | Séparé. Cocher « Démarrer avec Windows » dans la préversion ne touche pas celui de ta 2.x. |
| Ctrl+Alt+D | Si ta 2.x tourne, ou si elle démarre avec Windows, la préversion lui **laisse** Ctrl+Alt+D. Les panneaux restent dans le menu de l'icône (« Afficher les panneaux »). Réglages > Affichage et fenêtres explique pourquoi, et la case « Utiliser Ctrl+Alt+D dans cette préversion » te permet de le prendre. |
| Panneaux en double | Si ta 2.x tourne pendant une draft, **ses panneaux apparaissent aussi**. La préversion le signale : notice sur l'Accueil et dans Réglages > Affichage et fenêtres, avec le nom de l'autre programme, et une bulle au premier passage en draft. Pour l'éviter sans la modifier : masque les panneaux de l'une des deux (menu de son icône), ou quitte la 2.x avant la draft (clic droit sur son icône > Quitter) et relance-la après le test. |
| Mises à jour | La préversion n'en cherche pas. Ta 2.x ne te proposera jamais la préversion. |

Bon à savoir : si ta 2.x est **ouverte** au premier lancement de la préversion, un fichier qu'elle tient en exclusivité n'est pas copié. L'Accueil l'indique (« Fichiers non copiés car ouverts ailleurs »). Pour une copie complète, quitte ta 2.x avant le premier lancement.

## 3. Ce qu'il faut tester

Note pour chaque point : OK ou problème, et ce que tu as vu.

- [ ] **Premier lancement** : l'Accueil s'ouvre, la carte « Préversion de test » dit que tes données viennent d'une copie, avec la date.
- [ ] **Tes données** : Mon pool montre tes champions par rôle ; Mes statistiques montre ton journal et ton historique, comme dans ta 2.x.
- [ ] **Langue** : Réglages > Général > Langue. Passe en English, puis reviens en français. La fenêtre change tout de suite.
- [ ] **Vraie draft** : lecture du client (si tu l'as activée) ou détection par l'écran ; ton rôle attribué par League ; le pool du rôle ; le conseil ; « Je joue » ; le journal. Protocole détaillé : [TEST-DRAFT-REELLE.md](TEST-DRAFT-REELLE.md).
- [ ] **Panneaux** : ils apparaissent pendant la sélection, se collent au client, disparaissent au lancement de la partie et après un dodge.
- [ ] **Ctrl+Alt+D** : selon ton cas (voir § 2). Si tu as un second compte Windows, protocole : [TEST-CTRL-ALT-D-REEL.md](TEST-CTRL-ALT-D-REEL.md).
- [ ] **Plusieurs écrans** (si tu en as) : [PROCEDURE-MULTI-ECRANS.md](../qa/PROCEDURE-MULTI-ECRANS.md).
- [ ] **Réglages > Mises à jour** : « Préversion de test : télécharge les nouvelles versions à la main. », sans bouton.

## 4. Signaler un bug

Envoie à la personne qui t'a donné la préversion :

1. Ce que tu faisais, ce que tu attendais, ce qui s'est passé.
2. La version : Réglages > Diagnostic et informations (« DRAFT 3.0.0-beta.1, préversion de test »).
3. Une capture d'écran, **sans pseudo** ni partie en cours visible (floute-les).
4. Si la lecture du client pose problème : Réglages > Diagnostic et informations > **Diagnostic de connexion LoL**. Le texte ne contient ni mot de passe, ni port, ni chemin, ni nom de joueur : copie-le tel quel.
5. Si DRAFT ne démarre pas ou plante : le fichier `%LOCALAPPDATA%\DRAFT-beta\draftmid.log`.
6. Si la détection de la draft se trompe : coche Réglages > Diagnostic et informations > « Journal de diagnostic de la détection (chiffres seulement) », refais une draft, puis envoie `%LOCALAPPDATA%\DRAFT-beta\diagnostic.log`.

**N'envoie jamais** ta clé Riot, ton fichier d'historique, ni tes identifiants.

## 5. Revenir à la 2.x

1. Quitte la préversion : clic droit sur son icône > **Quitter**.
2. Si tu avais coché « Démarrer avec Windows » dans la préversion, décoche-le **avant** de quitter (Réglages > Général).
3. Relance ta 2.x comme d'habitude. Ses données n'ont pas changé.

Pour retirer complètement la préversion : supprime `DRAFT-Portable.exe` et le dossier `%LOCALAPPDATA%\DRAFT-beta`.

## 6. Garder tes changements

Ce que tu changes dans la préversion (pools, exclusions, favoris) **ne revient pas** tout seul dans ta 2.x.

- **Pools et préférences** : dans la préversion, Mon pool > **Exporter mes profils** (fichier `draft-profils.json`). Dans ta 2.16.x : Réglages > Mes profils > **Importer**. Le format est le même (`draft-config`, version 1).
- **Journal** : dans la préversion, Mes statistiques > **Télécharger le journal** (fichier `draft-journal.json`). Dans ta 2.16.x : **Importer un journal**. Les parties sont ajoutées, rien n'est supprimé (même contrôle du fichier dans les deux versions).
- **Historique Riot importé** : il reste dans la préversion (`%LOCALAPPDATA%\DRAFT-beta`). Ta 2.x garde le sien.
- **Langue** : la 2.x n'a que le français.
- **Attention avec la 2.14** : ce que tu changes dans une 2.14 (pool, exclusions, favori) n'est pas repris par une 3.0 ni par la préversion, qui lisent d'abord les profils par rôle. Le journal, lui, est partagé. Détails : [COMPATIBILITE-2X.md](COMPATIBILITE-2X.md).

## 7. Nouvelle préversion

Télécharge-la à la main et remplace `DRAFT-Portable.exe` dans ton dossier `C:\DRAFT-beta\`. Elle garde le dossier `%LOCALAPPDATA%\DRAFT-beta` : la copie depuis la 2.x n'est faite qu'une fois, quand ce dossier est vide.
