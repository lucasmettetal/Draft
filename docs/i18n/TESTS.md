# Tests de l'internationalisation

Ce qui doit être vérifié, avec quel outil, à quelle étape. Les prototypes sont dans `tools/i18n/` et tournent sans toucher à `web/` ni à `src/` (copie sous `%TEMP%\draft-i18n-prep`).
Base reprise : `DRAFT-ux/docs/ux/PLAN-I18N-FR-EN.md` § 6 à 8.

## 1. Outils livrés par cette étude

| Outil | Rôle | État |
|---|---|---|
| `tools/i18n/detect-hardcoded.ps1` | Chaînes visibles écrites en dur dans `web/*.js`, `web/index.html`, `src/*.cs` ; `-Max 0` pour la CI ; `-Baseline` pour n'échouer que sur les nouvelles ; `-SelfTest` | Fonctionne sur le code actuel (§ 2) ; autotest 23 cas verts |
| `tools/i18n/exclusions.json` | Exclusions du détecteur, chacune avec sa raison | Revue obligatoire à chaque ajout |
| `tools/i18n/check-catalogs.ps1` | Forme du fichier, doublons, noms de clés, HTML interdit, clés et variables identiques, pluriels requis, longueurs maximales | Vert sur les catalogues d'exemple (1 alerte voulue : clé de repli) |
| `tools/i18n/longueurs.json` | Longueurs maximales par emplacement (overlay étroit, badges, menu, dialogues) | Valeurs de départ à confirmer (§ 5) |
| `tools/i18n/pseudo-localize.ps1` | Pseudo-langue `qps` : accents, +35 %, crochets | Fonctionne |
| `tools/i18n/i18n-runtime.js` | Prototype de `t`, `tp`, `tx`, `fmtDate`, `fmtNumber`, repli, signalement | 19 contrôles verts dans Chrome |
| `tools/i18n/Strings.cs` | Prototype natif (même parseur que les données) | 23 contrôles verts, compilé par le csc de Windows |
| `tools/i18n/run-i18n-tests.ps1` | Lance tout ce qui précède | « Banc i18n : tout est correct. » |
| `tools/i18n/build-inventory.ps1` | Régénère `docs/i18n/inventaire.json` et `INVENTAIRE.md` | Fonctionne |

Commande unique : `pwsh tools\i18n\run-i18n-tests.ps1` (environ 20 s). Les scripts marchent aussi sous Windows PowerShell 5.1 (`-ExecutionPolicy Bypass` si les scripts sont bloqués).

## 2. Détecteur de chaînes en dur : résultat sur le code actuel

Base : `integration/v2.17` (`a6fe325`). Une « chaîne » est un littéral ou un morceau de gabarit ; une phrase assemblée en plusieurs morceaux compte plusieurs fois.

| Fichier | En dur | Exclues | Principales exclusions |
|---|---|---|---|
| `web/index.html` | 415 | 18 | noms de champions 10, marques 4, classes CSS 2 |
| `web/draft-coaching.js` | 267 | 2 | noms de champions |
| `web/draft-engine.js` | 155 | 14 | noms de champions 13, table codée 1 |
| `web/draft-mastery.js` | 118 | 1 | erreur interne |
| `web/draft-data.js` | 53 | 0 | |
| `web/draft-profiles.js` | 21 | 0 | |
| `web/draft-providers.js` | 0 | 2 | erreurs internes jamais affichées |
| `src/DraftMid.cs` | 92 | 313 | tests e2e et autotests 306, journaux 4 |
| `src/LcuConnector.cs` | 45 | 64 | autotests |
| `src/RoleRatesUpdater.cs` | 20 | 48 | autotests 45 |
| `src/AppUpdater.cs` | 18 | 38 | autotests |
| `src/HistoryImporter.cs` | 16 | 43 | autotests et contrôle de mise en page |
| `src/DraftDetection.cs` | 6 | 0 | |
| `src/DataScripts.cs`, `src/SecurityTests.cs` | 0 | 20 | autotests |
| **Total** | **1226** (page 1029, programme 197) | 563 | |

Fichiers ignorés : `draft-champions.js`, `draft-champion-ids.js`, `draft-champion-official.js`, `draft-role-sheet.js` (données), `draft-vendor/`, pages `*-tests.html`.

Qualité mesurée :
- **Rappel** : les 26 groupes natifs de l'audit (N-01 à N-26) sont tous retrouvés. Côté page, 932 des 1029 chaînes trouvées correspondent à un gabarit du catalogue de la PR #9 ; les 97 autres sont surtout des morceaux de phrases assemblées (`draft-mastery.js`) et des définitions du lexique (`draft-coaching.js:29-39`) : liste dans `inventaire.json` (`candidatsPageHorsCatalogue`), à vérifier en I18N-1.
- **Faux positifs** : relus à la main sur les 197 chaînes du programme : quelques fragments techniques restent (`Statistiques DRAFT|draft-stats.js|…`, filtre de fichier dont « Tous les fichiers » est pourtant visible). Côté page, aucun faux positif relevé dans les chaînes courtes.
- **Limite connue** : un mot seul, sans accent, hors d'un appel d'affichage (`"Tank"`, `"Victoire"` dans un tableau) n'est pas vu. La pseudo-langue (§ 4) les attrape à l'écran.

Garde-fou de CI (à activer en fin d'I18N-3) : `detect-hardcoded.ps1 -Max 0`. Entre I18N-1 et I18N-3, utiliser `-Baseline` avec le rapport de la PR précédente : aucune chaîne en dur **nouvelle**.

## 3. Catalogues

| Contrôle | Outil | Bloquant à partir de |
|---|---|---|
| Forme stricte (`window.DRAFT_I18N_XX = {…};`, commentaires `//` seulement avant) | `check-catalogs.ps1` et `Strings.Parse` (même règle que `DataScripts.Canonical`) | I18N-1 |
| Une clé par ligne, aucun doublon, noms de clés | `check-catalogs.ps1` | I18N-1 |
| Pas de HTML, pas de valeur vide, accolades équilibrées | `check-catalogs.ps1` | I18N-1 |
| Clés identiques FR/EN | `check-catalogs.ps1` (`-AllowMissing` en développement, sans pendant une version) | I18N-2 |
| Variables identiques par clé | `check-catalogs.ps1` | I18N-2 |
| Pluriels complets (`one` et `other` en fr et en) | `check-catalogs.ps1` | I18N-2 |
| Chaque clé utilisée existe | recherche des appels `t("…")`, `tp`, `tx`, `Strings.T` dans le code, comparée au catalogue (à écrire en I18N-1, même analyseur que le détecteur) | I18N-1 |
| Aucune clé inutilisée | même outil, en alerte | I18N-2 |

## 4. Pseudo-localisation

1. `pseudo-localize.ps1 -Source web\i18n\fr.js -Out <copie>\web\i18n\qps.js`.
2. Ouvrir la page avec `lang=qps&i18ncheck=1` (copie de travail seulement ; le programme n'accepte jamais `qps`).
3. Relevé du DOM avec le banc de la PR #9 (`tools/extract-ui-messages.ps1`, adapté) : **tout texte visible sans crochets est une chaîne restée en dur** ; un texte sans crochet fermant est coupé.
4. `tools/run-layout-tests.ps1` avec `lang=qps` : mêmes 29 tailles de panneaux et 5 de fenêtre qu'aujourd'hui ; aucun débordement nouveau.
5. Programme : `--selftest` avec `Strings` chargé en `qps` pour lancer `LayoutProblem()` des dialogues de clé et d'import (largeur fixe 340 à 360 px).

Le texte anglais est en général plus court que le français ; +35 % couvre l'allemand ou l'espagnol si d'autres langues viennent, et les mots anglais longs dans les badges de 230 px.

## 5. Longueurs maximales par emplacement

`tools/i18n/longueurs.json` fixe des maximums de départ : badges 16 caractères, bouton « Je joue » 28, onglets 14, menu de l'icône 48, étiquettes du dialogue de clé 60, bulle 200. Sur les catalogues d'exemple, la pseudo-langue dépasse deux limites (`advice.card.playButton`, `advice.card.badgeClose`) : c'est voulu, l'outil montre les emplacements serrés. Les maximums définitifs viennent de `run-layout-tests.ps1` en pseudo-langue (I18N-1), puis s'appliquent aux catalogues réels (I18N-2).

## 6. Relevé avant/après l'extraction (I18N-1)

- Avant toute modification : `tools/extract-ui-messages.ps1 -Scenarios …\scenarios-dev.json -All -Out avant.json` (tous les textes relevés, 252 drafts + 21 intégrées).
- Après l'extraction : même commande, `-Out apres.json`.
- Comparaison des textes observés (ensembles et nombres d'occurrences) : **0 différence en français**. Une seule différence (espace, apostrophe, point) bloque la PR.
- Le banc moteur de la PR #8 : 0 différence de classement.
- Les gabarits du catalogue de la PR #9 restent valables pendant I18N-1 (le texte ne change pas). Ensuite, le catalogue de test passe aux clés (colonne « clé » en plus du gabarit).

## 7. Tests natifs (`--selftest`)

À ajouter en I18N-3, sur le modèle de `tools/i18n/StringsHarness.cs` :

| Test | Attendu |
|---|---|
| Chargement du français intégré | Réussi ; sinon l'autotest échoue |
| Chargement de l'anglais | Réussi ; `Strings.Lang == "en"` |
| Clé absente en anglais | Texte français, signalé en mode test |
| Clé absente partout | `[clé]` |
| Catalogue piégé (code après l'objet, autre nom de variable, `__proto__`) | Refusé ou nettoyé, repli sur le français |
| Pluriels | Identiques à `Intl.PluralRules` (table relevée dans Chrome, 15 nombres, 2 langues : 0 différence aujourd'hui) |
| Détection | `fr-FR`, `fr-CA`, `fr-BE` → fr ; `en-US`, `de-DE`, culture invariante → en |
| Réglage | Absent ou `auto` → détection ; `fr`/`en` → prioritaire ; valeur inconnue → détection |
| Clés `native.*` utilisées | Toutes présentes en fr et en |
| Dialogues | `LayoutProblem()` vide en fr, en et qps |
| e2e | `--e2e` vert en fr et en (les tests cherchent `data-test`, plus des textes) |

## 8. Pages de test existantes

Les pages `web/*-tests.html` lisent des textes français (6 repères dans `draft-coaching-tests.html`, 5 dans `draft-v215-tests.html`, tout le catalogue de `draft-messages-tests.html`). Elles forcent `lang=fr` à partir d'I18N-1, ou passent à des assertions sur les clés. Les tests e2e natifs qui cherchent un bouton par son texte (`src/DraftMid.cs:1482`, `:1587`) passent aux attributs `data-test` dans UX-0, avant toute extraction.
