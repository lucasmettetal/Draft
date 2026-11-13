# Compte rendu QA-1 à QA-6, DRAFT 3.0.0-beta.1

Établi le 13/11/2026 à partir des résultats déjà enregistrés. Aucun test n'a été relancé pour ce document.

**Sources :**
- [RAPPORT-QA-300.md](RAPPORT-QA-300.md) ;
- [BACKLOG-QA-300.md](BACKLOG-QA-300.md) ;
- captures de [captures/](captures/) ;
- PR #31, branche `qa/3.0` en `175beb6`, partie de `main` = `38f228e`.

## Légende

- **Validé** : le critère de sortie est mesuré et atteint sur `qa/3.0`.
- **Échec** : le résultat mesuré ne satisfait pas le critère.
- **Non testé** : impossible à mesurer sur ce PC. Le document dit pourquoi et ce qui remplace la mesure.

Les échecs mesurés sur `main` sont indiqués à part : ils montrent que chaque test détecte bien le défaut avant correction.

## Synthèse

| N° | Point | Sur `main` (avant) | Sur `qa/3.0` (après) | Verdict | Non testé |
|---|---|---|---|---|---|
| QA-1 | Barres sous 50 % | 2 échecs : barre verte à 33 % | 0 échec, FR et EN | **Validé** | — |
| QA-2 | Menace du compact à 125 % | 4 échecs : menace sous le pli dans 4 compositions sur 4 | 0 échec : 4 compositions sur 4 visibles, FR et EN | **Validé** | Draft réelle avec le vrai client LoL |
| QA-3 | Boutons de l'overlay | 18 échecs : 28 px, 2 px d'écart, pas de contour | 0 échec | **Validé à 100 % et 125 %** | Vraie mise à l'échelle Windows à 150 % (tailles seulement calculées) |
| QA-4 | Multi-écrans | Position gardée s'il reste 1 px visible | Partie simulable corrigée : 3 tests OK, FR et EN | **Non testé (essai réel)** ; partie simulable validée | Deux écrans réels, échelles différentes, écran débranché |
| QA-5 | Tableaux Historique Riot | 8 échecs : pourcentages sous 10 parties, aucune période | 0 échec, FR et EN | **Validé** | — |
| QA-6 | Ctrl+Alt+D | e2e en échec, sans cause | e2e **toujours en échec**, cause affichée ; test isolé 4/4 | **Échec (conflit 1409, pas un défaut de la 3.0)** ; mécanisme validé | Vrai Ctrl+Alt+D au clavier, sur une session sans autre DRAFT |

## Détail

### QA-1 : couleur des barres de statistiques sous 50 %
- **Critère :** sous 50 %, la barre n'utilise plus le vert des réussites. Sous 10 parties, aucun pourcentage.
- **Sur `main` :** barre à 33 % en `rgb(130, 207, 168)`, le vert des réussites. 2 échecs.
- **Correction :** sous 50 %, la barre passe en teinte neutre `--muted`. Le repère des 50 % et le pourcentage écrit sont conservés. La jauge de crans sous 10 parties est inchangée.
- **Sur `qa/3.0` :** `draft-qa300-tests.html`, partie QA-1, FR et EN, 0 échec. Barre à 33 % en `rgb(181, 171, 163)` ; à 75 %, toujours verte.
- **Preuve visuelle :** `captures/avant-stats-barres-fr.png`, `apres-stats-barres-fr.png`, `apres-stats-barres-en.png`. Statistiques fictives, marquées dans l'image.
- **Verdict : validé.**

### QA-2 : menace dans le panneau compact à 125 %
- **Critère :** la menace est visible sans défilement dans le compact à 125 %, sans masquer « Je joue » ni « Détails ».
- **Sur `main` :** menace sous le pli dans 4 compositions sur 4. Dans WebView2 en 252 × 576, menace à 547..615 px, actions à 482 px.
- **Correction :** dans le compact seulement, la menace passe juste après le conseil. Les panneaux gauche et droit ne changent pas.
- **Sur `qa/3.0` :**
  - 4 compositions sur 4 visibles, en 3 tailles et 2 langues ;
  - dans WebView2 réelle, menace à 360..428 px en 252 × 576 et en 282 × 576 ;
  - « Je joue » et « Détails » restent visibles.
- **Preuve visuelle :** `captures/avant-panneau-compact-125pc-252-fr.png` → `apres-…-fr.png` et `-en.png`, de même en 282 px.
- **Non testé :** une draft réelle avec le vrai client LoL. La lecture d'un vrai compte est interdite pendant la QA, donc la draft a été simulée.
- **Verdict : validé.**

### QA-3 : confort des boutons de l'overlay
- **Critère :** cibles d'au moins 32 px, espacement, survol et focus, à 100 %, 125 % et 150 %.
- **Sur `main` :** outils de 28 px CSS (28, 35 et 42 px physiques à 100, 125 et 150 %), 2 px d'écart, pas de contour au survol. 18 échecs.
- **Correction :** outils de 32 px (32, 40 et 48 px physiques), 4 px d'écart, contour au survol. Le focus ivoire de 2 px est inchangé.
- **Sur `qa/3.0` :**
  - 0 échec sur les panneaux gauche, droit et compact, en FR et en EN ;
  - le banc de mise en page FR et EN ne trouve aucun défaut.
- **Preuve visuelle :** `captures/avant-panneau-droit-125pc-fr.png` → `apres-…`.
- **Non testé :** la vraie mise à l'échelle Windows à 150 %. Le PC est fixé à 125 %, donc les tailles à 150 % sont calculées, pas observées.
- **Verdict : validé à 100 % et 125 %.**

### QA-4 : validation multi-écrans
- **Critère :** test sur un vrai PC à deux écrans, ou procédure manuelle documentée **et réalisée**.
- **Sur `main` :** une position de panneau enregistrée était gardée tant qu'il restait 1 pixel visible.
- **Correction de la partie simulable :** la position n'est reprise que si la moitié de la largeur et la barre de titre sont visibles sur un écran présent.
- **Sur `qa/3.0` :** `--selftest`, 3 tests QA-4, OK en FR et en EN.
- **Non testé :**
  - deux écrans réels ;
  - échelles différentes ;
  - écran débranché pendant l'utilisation ;
  - retour du client sur l'autre écran.

  Le PC n'a qu'un écran. La procédure [PROCEDURE-MULTI-ECRANS.md](PROCEDURE-MULTI-ECRANS.md) est écrite, mais **pas réalisée**.
- **Verdict : non testé pour l'essai réel.** Le critère de sortie n'est pas atteint tant que la procédure n'a pas été réalisée.

### QA-5 : tableaux de l'onglet Historique Riot
- **Critère :** mêmes règles que Mes statistiques : période, échantillon, pas de pourcentage sous le seuil.
- **Sur `main` :** pourcentages affichés sous 10 parties, sur 4 lignes de 9 et 3 parties, sans période. 8 échecs.
- **Correction :**
  - sous le seuil, « Sous 10 parties » / « Under 10 games » remplace le pourcentage ;
  - une ligne indique la période, le nombre de parties et la règle du seuil.
- **Sur `qa/3.0` :** 0 échec, FR et EN.
- **Preuve visuelle :** `captures/avant-stats-historique-riot-fr.png` → `apres-…-fr.png` et `-en.png`. Historique fictif, marqué dans l'image.
- **Verdict : validé.**

### QA-6 : raccourci Ctrl+Alt+D
- **Critère :** l'échec est toujours rapporté avec sa cause, sans jamais être masqué. Le conflit avec une autre instance doit être distingué d'un vrai défaut.
- **Résultat e2e :** **toujours en échec**, 4 fois sur 4 sur `main` comme sur `qa/3.0`, en FR et en EN.
  - Message sur `qa/3.0` : « Win32 1409 : déjà pris par un autre programme ; autres instances DRAFT de la session : DraftMid.exe ».
  - Le détenteur est l'installation principale de l'utilisateur (`%USERPROFILE%\Desktop\DraftMid.exe`, 2.14). Elle n'a été ni fermée ni touchée.
- **Diagnostic :** conflit avec une autre instance, **pas un défaut de la 3.0**.
- **Test isolé sur Ctrl+Alt+Maj+F11 :** 4/4.
  - enregistrement réel du raccourci ;
  - 2 `WM_HOTKEY` reçus, et un 3e, d'un autre identifiant, ignoré ;
  - panneaux masqués → affichés → masqués ;
  - second enregistrement de la même combinaison : erreur 1409, sans plantage.
- **Ce que voit l'utilisateur :** le menu n'annonce plus un raccourci indisponible. Réglages > Affichage explique que Ctrl+Alt+D est déjà pris (`captures/apres-reglages-affichage-fr.png` et `-en.png`).
- **Non testé :** le vrai Ctrl+Alt+D au clavier, sur une session Windows sans autre DRAFT. Procédure : [PROCEDURE-CTRL-ALT-D.md](PROCEDURE-CTRL-ALT-D.md), écrite mais **non réalisée**.
- **Verdict : l'e2e reste en échec (conflit 1409) ; le mécanisme est validé par le test isolé.**

## Contrôles communs (déjà mesurés, non relancés)

- **Moteur :** banc avec évaluation à 0 différence sur 312 + 108 scénarios. Relancé par le coordinateur sur `175beb6`.
- **CSP :** identique à `main`.
- **Lecture du client (LCU) :** GET seulement, aucun autre verbe HTTP dans `src/`.
- **Mises à jour :** `src/AppUpdater.cs` inchangé par la branche.
- **CI de la PR #31 :** verte (windows, CodeQL C# et JavaScript).
- **Autotest :** FR 311/0, EN 311/0.
- **e2e :** FR ×2 et EN ×2, 101 OK et 1 échec chacun. L'échec est Ctrl+Alt+D, voir QA-6.

## Reste à faire avant la bêta

1. Réaliser [PROCEDURE-CTRL-ALT-D.md](PROCEDURE-CTRL-ALT-D.md) sur une session sans autre DRAFT (QA-6).
2. Réaliser [PROCEDURE-MULTI-ECRANS.md](PROCEDURE-MULTI-ECRANS.md) sur un PC à deux écrans (QA-4), avec si possible un écran à 150 % (QA-3).
3. Faire une draft réelle avec le vrai client LoL (QA-2 en conditions réelles).
4. Décider de la diffusion de la bêta, voir RAPPORT-QA-300.md, défaut M1 :
   - le numéro de version est toujours 2.16.2 ;
   - la mise à jour automatique ignore les préversions.
