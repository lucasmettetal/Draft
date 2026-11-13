# DRAFT 3.0.0-beta.1 : préversion de test

**Préversion pour un petit groupe de testeurs.** Elle travaille sur une **copie** de tes données 2.x et n'a **pas de mise à jour automatique** : télécharge les nouvelles versions à la main. Ta version 2.x installée n'est pas modifiée.

DRAFT n'est ni approuvé ni soutenu par Riot Games. Il n'agit jamais dans le client : tu choisis, bannis et verrouilles toujours toi-même.

English version: [NOTES-3.0.0-beta.1.en.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.1/docs/release/NOTES-3.0.0-beta.1.en.md).

## Ce qui change depuis la 2.16.2

- **Interface refaite.** Navigation en cinq vues (Accueil, Mon pool, Simulateur, Mes statistiques, Réglages). Réglages rangés en six rubriques. Accueil qui guide le premier lancement en trois étapes au plus.
- **Nouvelle direction artistique (B+).** Couleurs, typographie, icônes de rôle et panneaux harmonisés. Barre de titre sombre quand Windows le permet.
- **Anglais complet.** Toute l'interface, les panneaux, le menu de l'icône, les bulles et les messages du programme.
- **Choix de la langue.** Réglages > Général > Langue : automatique (langue de Windows), français ou anglais. Le changement s'applique sans redémarrer. Pendant une draft, les panneaux gardent leur langue jusqu'à la fin.
- **Rôles affichés cohérents avec le calcul.** Une seule échelle pour ton vis-à-vis. Le rôle attribué par League (si la lecture du client est active) choisit le pool du rôle ; il n'est jamais déduit du champion.
- **Explications fidèles au calcul.** Les raisons affichées viennent du calcul lui-même. Aucun score ni seuil du moteur n'a changé.
- **Vocabulaire LoL harmonisé** dans toute l'interface.
- **QA finale.** Barre de winrate grise sous 50 %, panneau unique lisible à 125 %, outils des panneaux plus grands (32 px), historique Riot avec période et seuil de 10 parties, cause affichée quand Ctrl+Alt+D est déjà pris, position d'un panneau reprise seulement s'il reste visible.

## Particularités de cette préversion

- **Données sur une copie.** Au premier lancement, si son dossier est vide, la préversion copie les données de la 2.x (réglages, profils, pools, journal, historique) depuis `%LOCALAPPDATA%\DraftMid` vers `%LOCALAPPDATA%\DRAFT-beta`. Elle lit la 2.x sans jamais y écrire. Ensuite, chacune garde ses propres données : ce que tu changes dans la préversion ne revient pas dans la 2.x.
- **Portable seulement.** Un seul fichier, `DRAFT-Portable.exe`. Pas d'installateur, pas de désinstallation.
- **Pas de mise à jour automatique.** Réglages > Mises à jour dit « Préversion de test : télécharge les nouvelles versions à la main ». La 2.x installée ne propose jamais cette préversion.
- **À côté d'une 2.x.** Les deux peuvent tourner en même temps. Démarrage avec Windows séparé (valeur `DRAFT-beta`).
- **Ctrl+Alt+D.** Si une autre version de DRAFT tourne, ou si la 2.x démarre avec Windows, la préversion lui laisse le raccourci. Les panneaux restent dans le menu de l'icône. Réglages > Affichage et fenêtres explique pourquoi et permet de prendre le raccourci.

## Limites connues

- **Ctrl+Alt+D réel non prouvé** sur une session sans autre DRAFT (M2). Mécanisme vérifié par un test isolé. Une 2.x lancée à la main **après** la préversion, sans démarrage automatique, n'aura pas Ctrl+Alt+D si la préversion l'a pris.
- **Plusieurs écrans non testés** (m1) : un seul écran sur le PC de test.
- **m2 : langue et retour en 2.x.** La préversion ne partage plus son dossier avec la 2.x : la 2.x ne réécrit plus les réglages de la préversion. Mais tes changements faits dans la préversion ne reviennent pas dans la 2.x. Pour garder tes pools : Mon pool > Exporter mes profils, puis Importer dans la 2.x (même format de fichier). Pour le journal : Mes statistiques > Télécharger le journal, puis Importer un journal dans la 2.x.
- **Pas de passage automatique à la 3.0 finale** : elle aussi se télécharge à la main.
- **2.14 et 3.0 sur les mêmes données** : la 2.14 relit le rôle joué, son pool et le journal, et efface la langue choisie. Les changements de pool faits ensuite dans la 2.14 ne sont pas repris par la 3.0. Détails : [COMPATIBILITE-2X.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.1/docs/release/COMPATIBILITE-2X.md).
- **m3 : premier affichage des panneaux lent** : 4,5 à 5,1 s après la détection au premier affichage (création de WebView2), puis quelques millisecondes.
- **Si la 2.x est ouverte au premier lancement**, un fichier qu'elle tient en exclusivité n'est pas copié. L'Accueil le dit et compte ces fichiers.
- Si une autre DRAFT tourne pendant une draft, tu verras deux jeux de panneaux. La préversion le signale (Accueil, Réglages > Affichage et fenêtres, bulle) : masque les panneaux de l'une des deux, ou quitte l'autre toi-même.

## Fichiers

- `DRAFT-Portable.exe` : préversion portable, compilée et testée par GitHub Actions.
- `DRAFT-Portable.exe.sig` : signature DRAFT, jointe séparément après la compilation.

Guide du testeur : [GUIDE-TESTEUR-3.0.0-beta.1.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.1/docs/release/GUIDE-TESTEUR-3.0.0-beta.1.md).
