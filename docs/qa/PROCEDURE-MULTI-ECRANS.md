# Procédure manuelle : plusieurs écrans (QA-4)

Statut au 13/11/2026 : **non exécutée**. Le PC de développement n'a qu'un écran (1920 × 1080 à 125 %, zone de travail 1920 × 1030).

## Ce qui est déjà vérifié sans second écran

- `--selftest`, calcul de placement pur : deux écrans à 100 % et 150 %, à 100 % et 200 %, client plein écran sur l'un et panneaux sur l'autre à sa propre échelle, client à cheval (l'écran le plus couvert compte), 90 combinaisons résolution × échelle × taille du client sur un écran (dont 30 sans place libre).
- `--selftest`, QA-4 (nouveau) : une position enregistrée n'est reprise que si au moins la moitié du panneau et toute sa barre de titre restent sur un écran présent. Écran retiré, panneau presque hors écran (10 px), barre de titre au-dessus de l'écran : place par défaut. Avant, un seul pixel commun suffisait.
- `--e2e` : la ligne « NON TESTE : deux écrans, un seul présent. » reste affichée tant qu'il n'y a qu'un écran.

## Matériel

Un PC avec deux écrans, idéalement d'échelles différentes (par exemple 100 % et 125 %, ou 125 % et 150 %), et le client LoL. Données d'essai : `--data C:\Essai-DRAFT\donnees --no-hotkey` si une autre version de DRAFT est ouverte.

## Étapes et résultats attendus

| N° | Situation | Attendu |
|---|---|---|
| 1 | Client LoL sur l'écran principal, sélection des champions | Deux panneaux collés au client, sur le même écran, sans le couvrir |
| 2 | Déplacer le client sur le second écran pendant la sélection | Les panneaux suivent sur le second écran, à son échelle (textes nets, même taille apparente) |
| 3 | Client à cheval sur les deux écrans | Les panneaux vont sur l'écran qui contient la plus grande part du client |
| 4 | Client en plein écran sur un écran | Panneau unique ou panneaux placés sur l'autre écran, jamais sur le client |
| 5 | Déplacer un panneau à la main sur le second écran, puis fermer et relancer DRAFT | Le panneau revient à la position choisie |
| 6 | Débrancher le second écran (ou le désactiver dans Paramètres > Affichage), puis lancer une sélection | Les panneaux réapparaissent sur l'écran restant, entiers, barre de titre visible ; aucune fenêtre invisible |
| 7 | Rebrancher le second écran | Rien ne disparaît ; Réglages > Affichage > « Replacer les panneaux » remet la place par défaut |
| 8 | Changer l'échelle d'un écran pendant que DRAFT est ouvert | Les panneaux se replacent sans couvrir le client |
| 9 | Placer l'écran secondaire au-dessus ou à gauche du principal (coordonnées négatives) et refaire 2 et 6 | Même comportement |

Refaire les étapes 1, 2 et 6 avec DRAFT en anglais.

## Résultat à rapporter

Pour chaque étape : OK ou défaut, avec les résolutions, échelles et positions relatives des écrans, et une capture si un panneau est mal placé. Ne pas joindre de capture contenant un pseudo ou une partie en cours sans flouter.
