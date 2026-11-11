# Plan d'exécution I18N-1, I18N-2, I18N-3

Ordre et durées repris de `DRAFT-ux/docs/ux/FEUILLE-DE-ROUTE-UX.md` et `PLAN-I18N-FR-EN.md` (non refaits). Ce plan ajoute le découpage en commits, les fichiers touchés et les garde-fous outillés par cette étude.

## 1. Place dans la feuille de route

| Ordre | PR | Dépend de | Lien avec l'i18n |
|---|---|---|---|
| — | PR D, PR E, **PR C-2** (vocabulaire) | — | Changent des centaines de textes : fusionnées **avant** I18N-1. C-2 corrige aussi les « (s) » et unifie les libellés A-30 (menu et page). |
| 0 | UX-0 | V2.17 | Attributs `data-test` : les tests ne cherchent plus de texte. **Préalable obligatoire.** |
| 1-2 | UX-1, UX-2 | UX-0 | Corrections de comportement, simulateur. |
| 3 | **I18N-1** | UX-2 | Extraction du français, 0 mot changé. |
| 4-9 | UX-3 à UX-8 | I18N-1 | Les nouveaux écrans écrivent leurs textes directement dans `web/i18n/fr.js`. |
| 10 | **I18N-2** | UX-8 | Catalogue anglais : une seule traduction, après la nouvelle navigation. |
| 11 | **I18N-3** | I18N-2 | Sélecteur, `Language`, textes natifs. |
| 12 | I18N-4 | I18N-3 | Parcours FR et EN, guide. |

Version cible : DRAFT 3.0.0 bêta après I18N-4.

## 2. I18N-1 : catalogue français, sans changer un mot (4 à 6 jours)

Préalables dans la même PR, en premiers commits :
1. Remplacer les **valeurs françaises utilisées comme codes** (STRATEGIE.md § 6) : confiance `faible|moyenne|élevée` → `low|medium|high` (le CSV garde l'ancien mot), `"pas encore choisi"` → marqueur, `/parties/.test(x.text)` et `/te touche/.test(i.title)` → drapeaux. Garde : banc moteur 0 différence, relevé 0 différence.
2. Relevé de référence : `tools/extract-ui-messages.ps1 -All -Out avant.json` ; rapport du détecteur `detect-hardcoded.ps1 -Json avant-detect.json`.

Extraction, un commit par fichier, dans cet ordre (du plus isolé au plus central) :

| Commit | Fichier | Entrées (inventaire) | Remarque |
|---|---|---|---|
| a | `web/i18n/fr.js` (nouveau), fonctions `t`, `tp`, `tx`, `fmtDate`, `fmtNumber` dans `index.html`, deux balises `<script src>` | — | Reprend `tools/i18n/i18n-runtime.js`. `lang` lu de l'adresse, `fr` par défaut. |
| b | `web/draft-profiles.js` | 5 | Erreurs d'import. |
| c | `web/draft-data.js` | 15 | Libellés des sources. |
| d | `web/draft-engine.js` | 109 | `msg(key, vars)` rend d'abord une chaîne (transition sans risque). |
| e | `web/draft-mastery.js` | 25 | Phrases assemblées : une clé par phrase entière. |
| f | `web/draft-coaching.js` | 170 | Clés `coach.<règle>.<niveau>`. |
| g | `web/index.html` | 270 | Le plus gros ; dates `fr-FR` et virgule décimale passent par `fmtDate`, `fmtNumber` sans changer le rendu. |
| h | Pages de test : `lang=fr` forcé ; catalogue de `draft-messages-tests.html` complété par une colonne « clé » | — | |

Critères de sortie :
- `extract-ui-messages.ps1` : **0 différence** de texte avec `avant.json`.
- Banc moteur (PR #8) : 0 différence de classement.
- `check-catalogs.ps1 -Reference web\i18n\fr.js` : 0 erreur.
- `detect-hardcoded.ps1` sur `web/` : 0 chaîne en dur hors exclusions (le natif reste pour I18N-3 : `-Baseline` sur `src/`).
- Pseudo-langue + `run-layout-tests.ps1` : aucun texte sans crochets, aucun débordement nouveau ; maximums de `longueurs.json` confirmés.
- `--selftest`, `--e2e` verts.

Fichiers touchés : `web/i18n/fr.js` (nouveau), `web/index.html`, `web/draft-engine.js`, `web/draft-coaching.js`, `web/draft-mastery.js`, `web/draft-data.js`, `web/draft-profiles.js`, `web/draft-messages-tests.html`, `web/*-tests.html`, `tools/extract-ui-messages.ps1`, `tools/run-layout-tests.ps1`, `tools/i18n/*` (outils passés de prototype à outil de CI), `.github/workflows/build.yml` (étape `check-catalogs` et détecteur). `build.ps1` : aucun changement (il intègre déjà `web/` récursivement).

## 3. I18N-2 : catalogue anglais (3 à 4 jours)

1. `web/i18n/en.js` : toutes les clés, à partir de [GLOSSAIRE-FR-EN.md](GLOSSAIRE-FR-EN.md).
2. Entrées `securite: true` (42 dans l'inventaire) relues par deux personnes ; sens gardé mot pour mot (STRATEGIE.md § 5).
3. `check-catalogs.ps1 -Reference fr.js -Target en.js` sans `-AllowMissing` : 0 erreur (clés, variables, pluriels, longueurs).
4. Captures en anglais forcé (`lang=en` dans une copie de travail) : overlay 230 px, badges, dialogues.
5. Pas encore de sélecteur : l'anglais n'est visible que par `lang=en` en test.

Fichiers touchés : `web/i18n/en.js` (nouveau), `tools/i18n/longueurs.json` si un maximum change, captures dans `docs/`. Aucun conflit avec le code.

## 4. I18N-3 : sélecteur de langue et textes natifs (2 à 3 jours)

| Commit | Fichiers | Contenu |
|---|---|---|
| a | `src/Strings.cs` (nouveau, repris de `tools/i18n/Strings.cs`), `build.ps1` (ajout du fichier à la compilation) | Lecture de la ressource intégrée, repli, pluriels ; tests ajoutés à `--selftest`. |
| b | `src/DraftMid.cs` | `UserPreferences.Language`, `Strings.Resolve` au démarrage, `&lang=` dans `Ensure`/`OpenFull`, `lang` dans `HostJson`, commande `setLanguage`, rechargement hors draft, menu reconstruit. `Extract()` publie `web/i18n/*.js` via `DataScripts.Canonical`. |
| c | `src/DraftMid.cs` (menu, bulles, `MessageBox`, installation), `src/DraftDetection.cs` (`PhaseText`), `src/LcuConnector.cs` (diagnostic), `src/HistoryImporter.cs`, `src/RoleRatesUpdater.cs`, `src/AppUpdater.cs` | 156 entrées natives de l'inventaire. Phrases assemblées remplacées par une clé par phrase. États envoyés à la page en `{ key, vars }`. |
| d | `web/index.html` | Réglages > Général > Langue ; rendu des états natifs par clé. |
| e | `web/i18n/fr.js`, `web/i18n/en.js` | Clés `native.*` et `settings.language.*`. |

Critères de sortie : `detect-hardcoded.ps1 -Max 0` sur tout le périmètre ; `--selftest` vert (chargement, repli, catalogue piégé, pluriels, `LayoutProblem()` en fr, en, qps) ; pseudo-langue : aucun texte sans crochets, page et dialogues ; première installation sur un Windows anglais → anglais ; `settings.json` d'une 2.x → détection, sans erreur.

## 5. Risques de conflit et parades

| Risque | Gravité | Parade |
|---|---|---|
| I18N-1 touche presque chaque ligne de texte de `index.html` et des trois fichiers du moteur | Forte | Aucune autre PR sur ces fichiers pendant I18N-1 ; PR courte, fusionnée vite ; un commit par fichier pour faciliter un rebase. |
| Travaux parallèles en cours (moteur sur `web/index.html`, maquettes UX) | Forte | I18N-1 part après fusion de D, E, C-2, UX-0 à UX-2. Les branches ouvertes ensuite se rebasent en remplaçant leurs textes par des clés (le détecteur avec `-Baseline` signale les oublis). |
| Logique qui compare du texte français (confiance, « pas encore choisi », regex sur `text`/`title`) | Forte | Codes techniques d'abord (§ 2, préalable 1) ; banc moteur et relevé à 0 différence. |
| Tests e2e qui cherchent des boutons par leur texte | Moyenne | UX-0 (`data-test`) avant I18N-1. |
| Phrases natives assemblées en morceaux (« Calcul des » + quoi + « : ») | Moyenne | Une clé par phrase en I18N-3 ; contrôle « aucun morceau de phrase » en revue. |
| Libellés différents entre menu et page (A-30) | Moyenne | Unifiés par C-2 ou UX-6 avant I18N-2. |
| Débordement en anglais dans l'overlay 230 px et les dialogues à largeur fixe | Moyenne | Pseudo-langue et `longueurs.json` dès I18N-1 ; `LayoutProblem()` en I18N-3. |
| Écart de règle de pluriel entre page et programme | Faible | Banc qui compare `Intl.PluralRules` et `Strings.PluralCategory` (0 différence aujourd'hui). |
| Catalogue modifié pour porter du code | Faible | Forme stricte vérifiée en CI, à l'autotest et réécrite par `DataScripts.Canonical` à l'extraction. |
| Inventaire daté (catalogue PR #9 sur 2.16.1) | Faible | 18 entrées sans ligne retrouvée aujourd'hui ; `build-inventory.ps1` se relance après C-2. |
| Main locale en retard | Faible | Toujours partir de `origin/main` ou de la branche d'intégration à jour. |
