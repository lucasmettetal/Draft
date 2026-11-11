# Critères de validation de l'interface 3.0 (option B)

**Maquette non validée.** Chaque critère est mesurable, a un seuil, et dit avec quel outil existant on le vérifie. Une PR UX n'est fusionnée que si ses critères (colonne « PR ») sont verts. Les seuils de clics viennent de [NAVIGATION-V3.md](NAVIGATION-V3.md).

## 1. Outils

| Outil | Ce qu'il mesure | Lancement |
|---|---|---|
| `tools/run-layout-tests.ps1` + `web/draft-layout-tests.html` | Rendu réel dans Chrome headless, hors ligne : débordement horizontal (`PAGE-X`, `DEFILE-X`), texte coupé ou superposé (`TEXTE-`, `TEXTES-`, `COUPE-X`), boîtes (`BOITE-`), cibles trop petites (`CIBLE`), boutons (`BOUTON-`) ; 31 états aujourd'hui | `powershell -File tools/run-layout-tests.ps1 -Shots` (copie de `web/` sous `%TEMP%`, profil Chrome dédié) |
| `DraftMid.exe --e2e` | Faux client League, LCU simulé, vrais panneaux : apparition, placement, focus, masquage ; « TOUT EST BON » | Copie compilée sous `%TEMP%`, `--data` sur un dossier vide |
| `DraftMid.exe --selftest` | Calculs purs (placement multi-écrans, migrations, signature), `LayoutProblem()` des dialogues | idem |
| `docs/ux/outils/repro-desynchro.ps1` | Perte de draft à l'ouverture de la fenêtre principale depuis un panneau (CDP) | `-Runs 50` sur une instance de test (`lancer-audit.ps1`) |
| `docs/ux/outils/mesure-a11y.js` | Contraste du texte sur le fond opaque le plus proche, tailles de texte, cibles | Injecté par `cdp.ps1` dans chaque page |
| `tools/run-ux-shots.ps1` | Captures des écrans réels pour comparaison avant/après | Après avoir rendu son dossier de données paramétrable |
| `tools/extract-ui-messages.ps1` | Relevé des messages visibles (PR #9) | Avant et après chaque PR UX |

Toute mesure se fait sur une **copie** compilée sous `%TEMP%`, avec `--data` sur un dossier vide et `--no-hotkey --run-key Software\DRAFT-UX-Test\Run`. Jamais sur l'installation principale ni sur `%LOCALAPPDATA%\DraftMid`.

## 2. Critères fonctionnels

| # | Critère | Seuil | Méthode | PR |
|---|---|---|---|---|
| F1 | Draft conservée à l'ouverture de « Détails » depuis un panneau rempli | **0 perte sur 50** (aujourd'hui 16 sur 18) | `repro-desynchro.ps1 -Runs 50` ; nouveau test e2e équivalent | UX-1 |
| F2 | Draft conservée après réduction puis rétablissement du client | 0 perte sur 20 | e2e : réduire le faux client, le rétablir, comparer `state.enemies` | UX-1 |
| F3 | Simulation isolée | 0 changement des panneaux sur 20 actions de simulation (aléatoire, ajouts, nouvelle simulation) | e2e : état des panneaux avant et après | UX-2 |
| F4 | « Reprendre la draft en cours » copie, sans lier | Modifier la simulation ensuite ne change pas les panneaux (0 sur 10) | e2e | UX-2 |
| F5 | Rôle joué stable | Modifier le pool Top avec Mid joué laisse Mid joué (10 sur 10) | `TestProfilesAndRole` étendu | UX-1, UX-4 |
| F6 | Aucun échec silencieux de saisie | 0 saisie perdue sans message : champ désactivé sans rôle, « déjà dans le pool », « Aucun champion trouvé » | `draft-profiles-tests.html` + test de page | UX-1, UX-4 |
| F7 | Échap n'efface aucune donnée | Échap dans un champ vide : 0 adversaire retiré | Test clavier CDP | UX-1 |
| F8 | Annulation | « Nouvelle simulation », retrait d'un adversaire au clavier, retrait d'un champion du pool : « Annuler » rétablit l'état exact | Test de page | UX-1, UX-4 |
| F9 | Chaque action du menu de l'icône répond | Mise à jour : 4 issues (à jour, disponible, bloquée, erreur) → 4 bulles + carte Accueil | `--test-update` (local), e2e | UX-1 |
| F10 | Anciennes adresses | `?tab=settings`, `analyse`, `compare`, `rules`, `coach`, `matchups`, `parties` mènent à la bonne vue | Banc de rendu (paramètre `tab`) | UX-3 |
| F11 | Toute fonction de l'ancienne modale | Atteignable en **≤ 2 clics** depuis l'Accueil | Lignes 28 à 48 de [MIGRATION-V3.md](MIGRATION-V3.md) § 2, cochée à la main | UX-3 |
| F12 | Aucune fonctionnalité perdue | 72 sur 72 lignes de l'inventaire présentes | Revue de la liste à chaque PR | toutes |
| F13 | Overlay inchangé là où il marche | e2e « TOUT EST BON » ; panneaux visibles **< 50 ms** après la détection ; 0 vol de focus | `--e2e` | UX-6 |
| F14 | Menu de l'icône | ≤ 8 entrées au premier niveau ; 0 case à cocher | `--selftest` (compte des `menu.Items`) | UX-6 |
| F15 | Premier lancement | Une fenêtre s'ouvre sur l'Accueil ; lancement par le menu Démarrer : fenêtre ouverte 10 fois sur 10 | e2e sur dossier vide ; lancement d'une seconde instance | UX-8 |
| F16 | Clé Riot | Jamais dans la page ni sur le disque : 0 occurrence dans le dossier de données et dans le DOM après un import | Recherche de la clé de test après `TestHistoryImport` | UX-5 |
| F17 | Classement inchangé | 0 différence de classement | Banc moteur (PR #8) avant et après | toutes |

## 3. Parcours (nombre d'actions)

Mesure : une action = un clic ou une saisie validée par Entrée, comptée par un script CDP qui rejoue le parcours, ou à la main sur la copie de test.

| Parcours | Seuil 3.0 | Avant | PR |
|---|---|---|---|
| A. Premier conseil après le premier écran | **≤ 6** | 5, avec modale et risque de perte | UX-8 |
| B. Mid + 3 champions depuis l'Accueil | **≤ 5**, 0 échec | 5, échec silencieux possible | UX-4 |
| C. Ajouter un champion au Top, Mid joué, fenêtre ouverte | **≤ 3**, rôle joué inchangé | 5, rôle changé | UX-4 |
| D. Import de l'historique, résultats visibles | **≤ 10** (hors obtention de la clé), résultats sur la même page | 10 à 15, résultats ailleurs | UX-5 |
| E. Simulation aléatoire depuis l'icône | **≤ 3** | 3 | UX-2 |
| F. Noter son pick pendant la sélection | **1** clic ; « Détails » sans perte | 1 | UX-6 |
| G. Changer un réglage d'affichage, fenêtre ouverte | **≤ 3** | 3 à 6 | UX-3 |
| H. Rechercher une mise à jour depuis l'icône | **≤ 3**, réponse visible 4 issues sur 4 | 2 à 3, réponse invisible | UX-1 |

## 4. Critères visuels

| # | Critère | Seuil | Méthode | PR |
|---|---|---|---|---|
| V1 | Contraste du texte | **≥ 4,5:1** partout (gros texte ≥ 3:1) ; aujourd'hui ≥ 5:1 | `mesure-a11y.js` sur les 5 sections, les 3 panneaux, les 6 rubriques | UX-7 |
| V2 | Limites de champ et de case | **≥ 3:1** (`--line-strong` : 3,81 sur `--bg`) | `mesure-a11y.js` (bordures) | UX-7 |
| V3 | Taille de texte minimale | **0 texte < 12 px** | Banc de rendu : seuil `TEXTE-TROP-PETIT` relevé de 10,5 à 12 px | UX-7 |
| V4 | Échelle typographique | **≤ 7 tailles** dans la feuille de style | Relevé des `font-size` (script de l'audit) | UX-7 |
| V5 | Couleurs | **0 couleur hors jetons** (hors `--scrim`) | Relevé des hex et `rgba` dans `web/app.css` et `index.html` | UX-7 |
| V6 | Troncature | **0 texte coupé ni débordement** à 1366 × 768 (100 % et 125 %), 1920 × 1080 (100 % et 175 %), 2560 × 1440 ; panneaux à 230, 256, 340, 374, 400 px | `run-layout-tests.ps1` étendu aux sections (voir § 5) | UX-3 à UX-8 |
| V7 | Cibles | **0 cible < 24 × 24 px** | Règle `CIBLE` du banc | UX-7 |
| V8 | Focus visible | 100 % des éléments atteints par Tab ont un contour visible | Parcours Tab scripté (CDP), capture | UX-7 |
| V9 | Clavier | Toutes les actions des 5 sections faisables sans souris | Parcours à la main, liste cochée | UX-3 à UX-8 |
| V10 | État actif | Une seule règle par famille (nav, onglet, segment, liste) | Revue de la planche [composants.html](maquettes/v3/composants.html) | UX-7 |
| V11 | Mouvement | 0 animation > 200 ms ; 0 animation avec mouvement réduit | Revue du code (`motion`) ; `--force-prefers-reduced-motion` | UX-7 |
| V12 | Pseudo-langue | 0 texte sans crochets, 0 troncature avec +35 % | `run-layout-tests.ps1` avec `lang=qps` | I18N-1, puis chaque PR UX |
| V13 | Fidélité aux maquettes | Écart revu capture contre capture, validé par l'utilisateur | `run-ux-shots.ps1` contre `maquettes/v3/captures/` | chaque PR |

## 5. Extensions de banc à prévoir (dans les PR, pas ici)

- `web/draft-layout-tests.html` : paramètre `view=home|pool|sim|stats|settings` et `section=` pour les rubriques ; états `firstrun`, `live`, `poolEditOther`.
- `tools/run-layout-tests.ps1` : ajouter les tailles de fenêtre 1029 × 582 (1366 à 125 %), 1051 × 591 (1920 à 175 %), 1366 × 768, 1920 × 1080, 2560 × 1440, en plus des 5 tailles actuelles ; facteur d'échelle par `--force-device-scale-factor` (méthode utilisée pour les captures des maquettes).
- Seuil `TEXTE-TROP-PETIT` à 12 px.
- e2e : test « Détails × 50 », test « simulation × 20 », test premier lancement, compte des entrées du menu.

## 6. Validation des maquettes elles-mêmes (fait pour cette étude)

| Vérification | Résultat |
|---|---|
| Aucun `<script>` dans `maquettes/v3/*.html` | 0 (relevé `grep`) |
| Aucune URL externe | 0 (relevé `grep http`) |
| Tailles de police en dur | 0 (toutes par jeton) |
| Mention « Maquette non validée » | Sur chaque page (bandeau fixe ou ligne au-dessus des panneaux) |
| Données | Marquées « Exemple » ; compte fictif `Exemple#EUW` ; aucune donnée personnelle |
| Captures | 53 PNG, Chrome headless hors ligne (`--host-resolver-rules`), profil dédié supprimé des processus à la fin |
| Contrastes des jetons | Tous ≥ 4,5:1 pour le texte ; `--line-strong` ≥ 3:1 sur les trois fonds |

Limites : les captures sont des maquettes statiques dans Chrome, pas la WebView2 réelle ; Bahnschrift est la police système de la machine de capture. Le contraste a été calculé sur les jetons, pas relevé sur des pages réelles.
