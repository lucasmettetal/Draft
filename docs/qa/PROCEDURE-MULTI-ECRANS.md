# Procédure manuelle : plusieurs écrans et échelles (QA-4, m1)

Statut au 13/11/2026 : **non exécutée**. Le PC de développement n'a qu'un écran (1920 × 1080 à 125 %, zone de travail 1920 × 1030). Mise à jour pour la préversion 3.0.0-beta.1 : échelles 100, 125 et 150 %, échelles différentes par écran, écran débranché, client LoL sur l'écran secondaire.

## Ce qui est déjà vérifié sans second écran

- `--selftest`, calcul de placement pur : deux écrans à 100 % et 150 %, à 100 % et 200 %, client plein écran sur l'un et panneaux sur l'autre à sa propre échelle, client à cheval (l'écran le plus couvert compte), 90 combinaisons résolution × échelle (100, 125, 150, 175 %) × taille du client sur un écran (dont 30 sans place libre).
- `--selftest`, QA-4 : une position enregistrée n'est reprise que si au moins la moitié du panneau et toute sa barre de titre restent sur un écran présent. Écran retiré, panneau presque hors écran (10 px), barre de titre au-dessus de l'écran : place par défaut.
- `--e2e` : la ligne « NON TESTE : deux écrans, un seul présent. » reste affichée tant qu'il n'y a qu'un écran.
- Ce qui n'est **pas** vérifié : le rendu réel à 100 % et 150 % (le PC est à 125 %), le changement d'échelle à chaud, le passage d'un écran à l'autre avec un vrai client, un vrai débranchement.

## Matériel et préparation

- [ ] Un PC avec deux écrans. Idéal : échelles différentes (par exemple 100 % et 150 %).
- [ ] Le client LoL. Une file où la sélection dure (partie personnalisée avec sélection, ou normale).
- [ ] La préversion. Si une 2.x tourne : la quitter (menu de son icône > Quitter) pour ne pas voir deux jeux de panneaux, sans changer ses réglages. Ou lancer la préversion avec un dossier d'essai : `DRAFT-Portable.exe --data C:\Essai-DRAFT\donnees`.
- [ ] Noter pour chaque écran : résolution, échelle (Paramètres > Système > Affichage), position relative (gauche, droite, au-dessus), écran principal ou non.

## A. Une seule échelle, puis chaque échelle

Sur l'écran principal, refaire A1 à A3 à **100 %**, **125 %** et **150 %** (Paramètres > Affichage > Échelle ; se déconnecter si Windows le demande).

| N° | Situation | Attendu | 100 % | 125 % | 150 % |
|---|---|---|---|---|---|
| A1 | Client LoL fenêtré 1280 × 720, sélection des champions | Deux panneaux collés au client, sans le couvrir, textes nets | | | |
| A2 | Réglages > Affichage et fenêtres > « Un seul panneau » | Panneau unique collé au client ; menace, « Je joue » et « Détails » visibles sans défiler | | | |
| A3 | Client 1600 × 900 ou plein écran | Panneau unique ou placement manuel, jamais sur le client | | | |

## B. Deux écrans

| N° | Situation | Attendu | Noté |
|---|---|---|---|
| B1 | Client sur l'écran principal, sélection | Deux panneaux collés au client, sur le même écran | |
| B2 | **Client sur l'écran secondaire** dès le lancement, sélection | Les panneaux apparaissent sur l'écran secondaire, à côté du client, à l'échelle de cet écran | |
| B3 | Pendant la sélection, déplacer le client d'un écran à l'autre | **Les panneaux suivent** sur l'autre écran, à son échelle (même taille apparente, textes nets) | |
| B4 | Client à cheval sur les deux écrans | Les panneaux vont sur l'écran qui contient la plus grande part du client | |
| B5 | Client en plein écran sur un écran | Panneau unique ou panneaux sur l'autre écran, jamais sur le client | |
| B6 | Déplacer un panneau à la main sur le second écran, fermer puis relancer DRAFT | Le panneau revient à la position choisie | |
| B7 | Écran secondaire placé au-dessus ou à gauche (coordonnées négatives), refaire B2 et B3 | Même comportement | |

## C. Échelles différentes par écran

Par exemple écran principal à 100 % et secondaire à 150 % (puis l'inverse).

| N° | Situation | Attendu | Noté |
|---|---|---|---|
| C1 | Client sur l'écran à 100 %, sélection | Panneaux à l'échelle 100 % (largeur environ 230 à 400 px) | |
| C2 | Déplacer le client sur l'écran à 150 % | Les panneaux suivent et prennent l'échelle 150 % : même taille apparente, pas de flou | |
| C3 | Client plein écran sur l'écran à 100 % | Panneaux sur l'écran à 150 %, à sa propre échelle | |
| C4 | Changer l'échelle d'un écran pendant que DRAFT est ouvert | Les panneaux se replacent sans couvrir le client | |
| C5 | Fenêtre principale de DRAFT glissée d'un écran à l'autre | Elle change d'échelle proprement, barre de titre sombre conservée | |

## D. Écran débranché

| N° | Situation | Attendu | Noté |
|---|---|---|---|
| D1 | Panneau déplacé à la main sur l'écran secondaire, puis débrancher cet écran (ou le désactiver dans Paramètres > Affichage) | Rien ne reste invisible | |
| D2 | Lancer une sélection | Les panneaux réapparaissent sur l'écran restant, entiers, barre de titre visible | |
| D3 | Client LoL resté sur l'écran débranché (Windows le ramène) | Les panneaux suivent le client là où Windows le place | |
| D4 | Rebrancher l'écran | Rien ne disparaît ; Réglages > Affichage et fenêtres > « Replacer les panneaux » remet la place par défaut | |

## E. Langue

Refaire B2, B3, C2 et D2 avec DRAFT en anglais (Réglages > Général > Langue > English).

## Résultat à rapporter

Pour chaque ligne : OK ou défaut, avec les résolutions, échelles et positions relatives des écrans, et une capture si un panneau est mal placé. Ne pas joindre de capture contenant un pseudo ou une partie en cours sans flouter.
