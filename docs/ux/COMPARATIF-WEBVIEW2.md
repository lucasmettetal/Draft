# Comparatif avant / après la refonte 3.0, dans la vraie application

Captures prises dans **DRAFT lui-même** (WinForms + WebView2 Edge 157.0.4341.46), pas dans Chrome sans écran.

- **Avant** : `main` 4dd76f9 (dernière version avant la refonte), compilé depuis un worktree temporaire.
- **Après** : `main` 87edef4 (navigation en 5 sections, Réglages en 6 rubriques).
- Images : [captures-webview2/avant](captures-webview2/avant), [captures-webview2/apres](captures-webview2/apres), planche [captures-webview2/apres/planche-sections.png](captures-webview2/apres/planche-sections.png).
- Mesures de chaque image (pid, tailles, débordements) : `manifeste-avant.csv` et `manifeste-apres.csv` dans ces dossiers.

## 1. Méthode

| Point | Ce qui a été fait |
|---|---|
| Outil | [`tools/run-webview-shots.ps1`](../../tools/run-webview-shots.ps1) (+ [`tools/shots/ShotsLib.ps1`](../../tools/shots/ShotsLib.ps1), [`tools/shots/planche.ps1`](../../tools/shots/planche.ps1)), sous Windows PowerShell 5.1 |
| Lancement | `DRAFT-Portable.exe --data <copie temporaire> --no-hotkey --run-key Software\DRAFT-Shots\Run`. Sans `settings.json`, DRAFT ouvre seul sa fenêtre principale (premier lancement) |
| Données | Copie neuve de `draft-essai-2162\data` (profil Mid : Fizz, Twisted Fate, Kog'Maw ; 96 parties). Le compte du journal est remplacé par `Joueur#TEST` dans la copie. `%LOCALAPPDATA%\DraftMid` n'est jamais lu |
| Pilotage | CDP sur `127.0.0.1` : variable `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS` donnée au seul processus lancé. Navigation par les fonctions de la page (`goView`, `openViewInPage`, clics sur les boutons d'avant) |
| Draft simulée | `draftApi` dans le panneau droit : `setActive(true)` puis Syndra, Ornn, Lee Sin, Jinx. Panneaux ouverts par la commande hôte `hideAll` (même effet que Ctrl+Alt+D), panneau unique par `toggleCompact` |
| Capture | **PrintWindow avec PW_RENDERFULLCONTENT (2)** sur les fenêtres du pid lancé. Le pid de chaque fenêtre est vérifié avant chaque image. Jamais l'écran entier. CDP ne sert pas à capturer |
| Taille | Fenêtre principale redimensionnée par sa **zone cliente** : 1366 x 768, puis 1920 x 1080 demandé |
| Contrôle | Chaque PNG : taille, luminosité moyenne, nombre de couleurs. 22 images avant, 33 après, 0 douteuse |
| Arrêt | Seul le pid lancé est arrêté, après vérification du chemin de l'exe. La clé `HKCU\Software\DRAFT-Shots` n'a pas été créée. Le DRAFT installé (pid 15044) n'a pas été touché |
| Réseau | `--host-resolver-rules="MAP * ~NOTFOUND"` : aucun appel sortant |

### Échelle Windows de ce PC

- Écran 1920 x 1080, **125 %** (120 ppp).
- Mesure : dans DRAFT, la page reste à `devicePixelRatio = 1` à 125 %. La page voit donc autant de pixels CSS que la zone cliente a de pixels physiques. Seule la barre de titre Windows suit les 125 %.
- Les captures « 1366 x 768 » montrent donc exactement la mise en page d'un écran 1366 x 768 à 100 %.
- Cela montre aussi qu'à 125 %, **le texte de DRAFT n'est pas agrandi** comme celui des autres applications. C'est un défaut à suivre (hors périmètre de la refonte, présent avant comme après).
- J'ai essayé une capture à 125 % « réels » avec `--force-device-scale-factor` : le rendu des panneaux devient faux (contenu réduit à 80 % dans la fenêtre). L'essai est abandonné. Je n'ai pas trouvé d'option propre pour forcer une échelle par processus sans modifier l'application.
- **1920 x 1080** : Windows limite la zone cliente à la taille de l'écran. Il donne **1920 x 1055** (fenêtre 1938 x 1102). Les fichiers gardent le nom `1920x1080`.

### Portraits

Aucun appel réseau. Les portraits sont ceux du cache de la copie (Fizz, Kog'Maw, Twisted Fate, Lissandra, Pantheon, Akali). Les autres champions affichent leurs initiales (« Or », « Sy »...). Le grand visuel central affiche « Illustration indisponible : vérifie ta connexion. ». C'est visible avant comme après.

## 2. Tableau avant / après par écran

| Écran | Avant (4dd76f9) | Après (87edef4) |
|---|---|---|
| Fenêtre principale | [1366](captures-webview2/avant/avant-fenetre-1366x768.png) · [1920](captures-webview2/avant/avant-fenetre-1920x1080.png) | Simulateur : [1366](captures-webview2/apres/apres-simulateur-1366x768.png) · [1920](captures-webview2/apres/apres-simulateur-1920x1080.png) |
| Accueil | n'existait pas | [1366](captures-webview2/apres/apres-accueil-1366x768.png) · [1920](captures-webview2/apres/apres-accueil-1920x1080.png) |
| Profils et pool | Modale Réglages, haut : [1366](captures-webview2/avant/avant-reglages-modale-1366x768.png) · [1920](captures-webview2/avant/avant-reglages-modale-1920x1080.png) | Mon pool : [1366](captures-webview2/apres/apres-mon-pool-1366x768.png) · [1920](captures-webview2/apres/apres-mon-pool-1920x1080.png) |
| Onglet Plan | dans la fenêtre principale (ci-dessus) | Simulateur, onglet Plan (ci-dessus) |
| Analyse > Critères | [1366](captures-webview2/avant/avant-onglet-analyse-1366x768.png) · [1920](captures-webview2/avant/avant-onglet-analyse-1920x1080.png) | [1366](captures-webview2/apres/apres-simulateur-criteres-1366x768.png) · [1920](captures-webview2/apres/apres-simulateur-criteres-1920x1080.png) |
| Analyse > Comparer | [1366](captures-webview2/avant/avant-onglet-analyse-comparer-1366x768.png) · [1920](captures-webview2/avant/avant-onglet-analyse-comparer-1920x1080.png) | [1366](captures-webview2/apres/apres-simulateur-comparer-1366x768.png) · [1920](captures-webview2/apres/apres-simulateur-comparer-1920x1080.png) |
| Analyse > Règles | [1366](captures-webview2/avant/avant-onglet-analyse-regles-1366x768.png) · [1920](captures-webview2/avant/avant-onglet-analyse-regles-1920x1080.png) | [1366](captures-webview2/apres/apres-simulateur-regles-1366x768.png) · [1920](captures-webview2/apres/apres-simulateur-regles-1920x1080.png) |
| Conseils | [1366](captures-webview2/avant/avant-onglet-conseils-1366x768.png) · [1920](captures-webview2/avant/avant-onglet-conseils-1920x1080.png) | [1366](captures-webview2/apres/apres-simulateur-conseils-1366x768.png) · [1920](captures-webview2/apres/apres-simulateur-conseils-1920x1080.png) |
| Duel | [1366](captures-webview2/avant/avant-onglet-duel-1366x768.png) · [1920](captures-webview2/avant/avant-onglet-duel-1920x1080.png) | [1366](captures-webview2/apres/apres-simulateur-duel-1366x768.png) · [1920](captures-webview2/apres/apres-simulateur-duel-1920x1080.png) |
| Parties | onglet : [1366](captures-webview2/avant/avant-onglet-parties-1366x768.png) · [1920](captures-webview2/avant/avant-onglet-parties-1920x1080.png) | Mes statistiques : [1366](captures-webview2/apres/apres-mes-statistiques-1366x768.png) · [1920](captures-webview2/apres/apres-mes-statistiques-1920x1080.png) |
| Réglages | modale à 12 sections, défilée : [1](captures-webview2/avant/avant-reglages-modale-1366x768.png) [2](captures-webview2/avant/avant-reglages-modale-2-1366x768.png) [3](captures-webview2/avant/avant-reglages-modale-3-1366x768.png) (1366) · [1](captures-webview2/avant/avant-reglages-modale-1920x1080.png) [2](captures-webview2/avant/avant-reglages-modale-2-1920x1080.png) (1920) | page à 6 rubriques, ci-dessous |
| Réglages > Général | — | [1366](captures-webview2/apres/apres-reglages-general-1366x768.png) · [1920](captures-webview2/apres/apres-reglages-general-1920x1080.png) |
| Réglages > Connexion League | — | [1366](captures-webview2/apres/apres-reglages-connexion-1366x768.png) · [1920](captures-webview2/apres/apres-reglages-connexion-1920x1080.png) |
| Réglages > Affichage et fenêtres | — | [1366](captures-webview2/apres/apres-reglages-affichage-1366x768.png) · [1920](captures-webview2/apres/apres-reglages-affichage-1920x1080.png) |
| Réglages > Données et confidentialité | — | [1366](captures-webview2/apres/apres-reglages-donnees-1366x768.png) · [1920](captures-webview2/apres/apres-reglages-donnees-1920x1080.png) |
| Réglages > Mises à jour | — | [1366](captures-webview2/apres/apres-reglages-mises-a-jour-1366x768.png) · [1920](captures-webview2/apres/apres-reglages-mises-a-jour-1920x1080.png) |
| Réglages > Diagnostic et informations | — | [1366](captures-webview2/apres/apres-reglages-diagnostic-1366x768.png) · [1920](captures-webview2/apres/apres-reglages-diagnostic-1920x1080.png) |
| Panneau gauche (draft simulée) | [image](captures-webview2/avant/avant-panneau-gauche.png) | [image](captures-webview2/apres/apres-panneau-gauche.png) |
| Panneau droit | [image](captures-webview2/avant/avant-panneau-droit.png) | [image](captures-webview2/apres/apres-panneau-droit.png) |
| Panneau unique | [image](captures-webview2/avant/avant-panneau-unique.png) | [image](captures-webview2/apres/apres-panneau-unique.png) |

Les panneaux font 425 x 1030 px (placement de DRAFT sans client LoL : bords gauche et droit de l'écran). Leur taille ne dépend pas de la fenêtre principale.

## 3. Observations factuelles

### Cohérence visuelle
- Même thème sombre, mêmes couleurs, mêmes cartes avant et après. Le Simulateur reprend la fenêtre d'avant presque à l'identique : seuls la barre latérale et l'en-tête changent.
- La barre de titre Windows reste **claire** (blanche) sur une application sombre, avant comme après.
- Icônes de la navigation des Réglages : plusieurs icônes en reprennent d'autres. « Mises à jour » utilise le triangle de « Simulateur ». « Données et confidentialité » utilise les barres de « Mes statistiques ». « Affichage et fenêtres » utilise la grille de « Mon pool ». « Connexion League » est une coche, qui se lit comme un état. « Diagnostic et informations » est un petit rond presque invisible.
- La page Réglages garde un bouton « Fermer » en haut à droite. Les autres sections n'en ont pas : c'est un reste de la modale.
- **Panneaux de l'overlay** : identiques à l'œil avant et après (mêmes blocs, mêmes icônes, mêmes textes). La refonte de l'overlay (UX-6) n'est pas encore là. Le panneau droit dit toujours « Analyse détaillée », alors que la fenêtre parle de « Simulateur ».
- Diagnostic et informations affiche « Client LoL fermé. Client LoL non détecté. » : deux phrases qui disent la même chose.

### Navigation
- Avant : 1 fenêtre, 5 onglets dans la colonne de droite (Plan, Analyse, Conseils, Duel, Parties), plus une modale Réglages à faire défiler (3 écrans à 1366 x 768).
- Après : 5 sections toujours visibles à gauche. Réglages est en bas, à part. Le pied de la barre indique l'état League et le rôle joué.
- Les onglets du Simulateur restent sur **deux niveaux** : Analyse contient encore Critères, Comparer et Règles. Le plan de migration prévoyait un seul niveau (Plan, Critères, Comparer, Conseils, Duel) et Règles en aide. Ce n'est pas fait.
- « Niveau des conseils » existe à deux endroits : Réglages > Général et onglet Conseils (comme avant).
- Historique Riot : la carte de Mes statistiques renvoie vers Réglages > Données (« Gérer l'import »). L'import ne se fait pas dans Mes statistiques.

### Actions principales
- Accueil : un seul bouton principal, « Simuler une draft ». Trois lignes d'état avec chacune un bouton vers la bonne rubrique.
- Simulateur : « Je joue Fizz » reste le seul bouton plein. En haut : « Draft en cours / Simulation », « Draft aléatoire », « Nouvelle draft » (devient « Nouvelle simulation » en simulation). En simulation apparaissent « Reprendre la draft en cours » et « Quitter la simulation » (vérifié par CDP).
- Après « Je joue » : Victoire, Défaite, Ignorer s'affichent (vérifié par CDP).

### Lisibilité à 1366 x 768
- **Aucun débordement horizontal** sur les 55 images (mesure `scrollWidth - innerWidth = 0`).
- Avec la barre latérale (225 px), la colonne « Composition adverse » du Simulateur est plus étroite qu'avant. Le texte d'aide de la recherche est coupé (« ahri, tf, ka » au lieu de « ahri, tf, kata... »). « Mid d'en face (probable)... » est coupé, avant comme après.
- Contenu sous le pli de la colonne de droite (défilement interne) : Critères 390 px, Comparer 169 px, Règles 1 582 px, Conseils 1 474 px (avant : 390, 169, 1 509, 1 401). Aucune section ni rubrique des Réglages ne défile à 1366 x 768. L'ancienne modale cachait 862 px de réglages.
- Les rubriques de Réglages sont courtes. Leurs noms longs passent sur deux lignes (« Données et / confidentialité », « Diagnostic et / informations »).

### Grand écran (1920 x 1055)
- Accueil, Mon pool, Mes statistiques et Réglages laissent une grande zone vide à droite et en bas. La carte de l'Accueil est centrée et étroite (environ 760 px).
- Le Simulateur occupe toute la largeur, comme avant.

## 4. Fonctionnalités d'avant et où les retrouver

Base : `docs/ux/MIGRATION-V3.md` § 2 de la branche `etude/ux-maquettes` (72 lignes). Vérifié dans l'application lancée : captures, et relevé par CDP des boutons et repères `data-test` de chaque vue. « Natif » : menu de l'icône près de l'horloge, non capturé (la capture ne vise que les fenêtres du processus), relu dans `src/DraftMid.cs` de 87edef4.

Légende : **OK** trouvée ; **Ailleurs** trouvée, mais pas à l'endroit prévu par le plan ; **Non vérifié** présente dans le code, pas déclenchable ici ; **Introuvable**.

### Fenêtre principale et simulation

| # | Fonctionnalité | Où dans 87edef4 | État |
|---|---|---|---|
| 1 | Changement de rôle en un clic | En-tête du Simulateur (5 icônes), panneaux ; pied de la barre en lecture | OK |
| 2 | « LoL t'attribue X », Passer, Ignorer | Sous le rôle (code `role-hint`) ; demande une lecture LCU | Non vérifié |
| 3 | Version du patch | En-tête du Simulateur (« Patch 16.21 ») | OK |
| 4 | Draft aléatoire | En-tête du Simulateur | OK |
| 5 | Nouvelle draft | En-tête du Simulateur (« Nouvelle simulation » en simulation) | OK |
| 6 | Bandeau Simulation, retour à la draft en cours | « Draft en cours / Simulation », « Reprendre la draft en cours », « Quitter la simulation » | OK |
| 7 | Recherche d'adversaire | Colonne gauche du Simulateur, panneau gauche | OK |
| 8 | Lignes de rôle | idem | OK |
| 9 | Mid probable | idem | OK |
| 10 | Profil de l'équipe (5 jauges, forces, failles) | idem | OK |
| 11 | Conseil (étiquettes, n/5, alternative, raisons, « Plus de détails ») | Centre du Simulateur | OK |
| 12 | Égalité | Centre (code `TieGroup`) ; pas d'égalité dans la draft testée | Non vérifié |
| 13 | « Je joue X », « X noté » | Centre du Simulateur, panneau droit | OK |
| 14 | Ton pool | Centre du Simulateur | OK |
| 15 | Plan de jeu (Gagne si, menaces, cibles, bans) | Colonne droite du Simulateur | OK |
| 16 | Plan par phase | Onglet Plan | OK |
| 17 | Critères | Onglet Analyse > Critères | OK (encore sous Analyse) |
| 18 | Comparer | Onglet Analyse > Comparer | OK (encore sous Analyse) |
| 19 | Règles | Onglet Analyse > Règles | Ailleurs (pas en aide « Comment DRAFT classe tes picks ») |
| 20 | Onglet Conseils | Onglet Conseils | OK |
| 21 | Niveau des conseils | Réglages > Général **et** onglet Conseils | OK (toujours en double) |
| 22 | Duel | Onglet Duel | OK |
| 23 | Victoire / Défaite / Ignorer | Simulateur après « Je joue », panneaux | OK |
| 24 | Barre mobile (fenêtre étroite) | Code `MobileBar` ; non capturée (fenêtre ≥ 1366 px) | Non vérifié |
| 25 | Raccourcis clavier | Simulateur seulement (code `onKey`) | Non vérifié |
| 26 | Annonce `aria-live` | Code présent | Non vérifié (invisible) |
| 27 | Mode démonstration | Message `demo` conservé ; pas d'étape de premier lancement | OK (tests) |

### Profils et réglages (ancienne modale)

| # | Fonctionnalité | Où dans 87edef4 | État |
|---|---|---|---|
| 28 | Rôles, pool par rôle, suggestions | Mon pool > Mes profils | OK |
| 29 | Export, import des profils | Mon pool (« Exporter mes profils », « Importer ») | OK |
| 29b | « Annuler le dernier import » | Mon pool, après un import (code `undo-profiles-import`) | Non vérifié |
| 30 | Exclure pour l'instant | Mon pool > carte « Rôle joué » > Mes picks | OK (rôle joué seulement, pas par rôle) |
| 31 | Favori à égalité | Mon pool > « En cas d'égalité, mettre en premier » | OK (rôle joué seulement) |
| 32 | Supposer une frontline | Réglages > Général | OK |
| 33 | Mise à jour (version, bouton) | Réglages > Mises à jour | OK (pas de carte sur l'Accueil) |
| 34 | Historique Riot (nombre, date, Importer) | Réglages > Données ; résumé et lien dans Mes statistiques | Ailleurs (plan : Mes statistiques) |
| 35 | Lecture du client LCU | Réglages > Connexion League + ligne de l'Accueil + pied de la barre | OK |
| 36 | Rôle attribué par League | Réglages > Connexion League | OK |
| 37 | Affichage automatique | Réglages > Affichage et fenêtres | OK |
| 38 | Coller au client | Réglages > Affichage et fenêtres | OK |
| 39 | Un seul panneau | Réglages > Affichage + icône du panneau | OK |
| 40 | Toujours au premier plan | Réglages > Affichage | OK |
| 41 | Démarrer avec Windows | Réglages > Général | OK |
| 42 | Rôles des champions | Réglages > Données | OK |
| 43 | Matchups | Réglages > Données | OK |
| 44 | Import d'un journal (JSON) | Mes statistiques > « Importer un ancien fichier » (repliable) | OK |
| 45 | Sources de données | Réglages > Données, repliable | OK |
| 46 | Journal de diagnostic, phase et placement | Réglages > Diagnostic et informations | OK |
| 47 | Réglages seuls pendant une draft | Code `solo=1`, `closeFull` | Non vérifié |
| 48 | Non approuvé par Riot Games, bêta | Pied de la barre + Diagnostic et informations | OK |

### Panneaux

| # | Fonctionnalité | Où dans 87edef4 | État |
|---|---|---|---|
| 49 | Barre de titre : mode, réglages, masquer | Trois icônes du panneau (capturées) | OK (inchangée) |
| 50 | Panneau sans profil | Profil présent dans les données d'essai | Non vérifié |
| 51 | Alternative, « Analyse détaillée » | Panneau droit | OK (libellé d'avant) |
| 52 | « Plan de jeu et ban » repliable | Panneau droit et unique | OK |
| 53 | Panneau unique | Capturé | OK |
| 54 | Ouverture auto si aucun profil | Code `draftProfileAsked` | Non vérifié |

### Programme natif (menu de l'icône, relu dans le code)

| # | Fonctionnalité | Où dans 87edef4 | État |
|---|---|---|---|
| 55-57 | Icône, double-clic, Ctrl+Alt+D | Inchangés (même menu qu'avant : UX-6 pas encore fusionnée) | OK |
| 58 | C'est / Ce n'est pas une draft | Menu de l'icône | OK |
| 59 | Diagnostic de connexion LoL | **Menu de l'icône seulement**. Aucun bouton dans Réglages > Diagnostic | Ailleurs (plan : aussi dans Réglages) |
| 60 | Importer `draft-stats.js` | **Menu de l'icône seulement**. Mes statistiques le dit en texte, sans bouton | Ailleurs (plan : bouton dans Mes statistiques) |
| 61-63 | Dialogues de clé, mise à jour, bulles | Inchangés | OK |
| 64 | Premier lancement | Ouvre **Réglages > Général** (constaté à chaque lancement) | Ailleurs (plan : Accueil) |
| 65-72 | Instance unique, installation, profils copiés, `setProfile`, CSV, détection, placement, modes de test | Inchangés dans le code ; `--data`, `--no-hotkey`, `--run-key` utilisés ici | OK |

**Aucune fonctionnalité n'est introuvable.** Cinq sont à un autre endroit que celui prévu (19, 34, 59, 60, 64). Dix ne sont pas vérifiables sans client LoL, sans import ou sans fenêtre étroite.

## 5. Limites

- 1920 x 1080 est en fait **1920 x 1055** : Windows ne laisse pas une zone cliente dépasser l'écran 1080p de ce PC.
- Pas de capture à 125 % « agrandie » : DRAFT garde la page à 100 % même à 125 %. Tout est capturé à 125 % Windows, c'est le vrai comportement.
- Pas de client LoL : état « Lecture du client désactivée », panneaux posés aux bords de l'écran.
- Pendant les captures, la fenêtre de test s'affiche en haut à gauche de l'écran (PrintWindow demande une fenêtre affichée). Si la souris la survole, un survol peut apparaître sur une image.
- Le menu de l'icône et les dialogues natifs ne sont pas capturés.

## 6. Refaire les captures

```powershell
# Après : compile la copie de travail puis capture.
powershell -NoProfile -ExecutionPolicy Bypass -File tools\run-webview-shots.ps1 -Version apres -Out docs\ux\captures-webview2\apres
# Avant : exe compilé depuis un worktree détaché de 4dd76f9 (build.ps1 -Out <tmp>).
powershell -NoProfile -ExecutionPolicy Bypass -File tools\run-webview-shots.ps1 -Version avant -Exe <tmp>\DRAFT-Portable.exe -Out docs\ux\captures-webview2\avant
powershell -NoProfile -ExecutionPolicy Bypass -File tools\shots\planche.ps1 -Dir docs\ux\captures-webview2\apres
```

Le script refuse de démarrer si un client LoL est ouvert. Il n'arrête que l'instance qu'il a lancée.
