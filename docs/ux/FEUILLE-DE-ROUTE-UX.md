# Feuille de route UX et i18n (après V2.17)

Option retenue : **B, refonte modérée avec vraie navigation** ([OPTIONS-REFONTE.md](OPTIONS-REFONTE.md)). Rien ne démarre avant la fusion des PR D, E et C-2 de V2.17. Efforts estimés en jours de développement, tests compris.

## Vue d'ensemble

| Ordre | PR | Sujet | Effort | Dépend de |
|---|---|---|---|---|
| 0 | UX-0 | Repères de test stables (`data-test`) | 1 j | V2.17 fusionnée |
| 1 | UX-1 | Corrections de comportement (P0, P1) | 2 à 3 j | UX-0 |
| 2 | UX-2 | Simulateur isolé de la draft réelle | 2 à 3 j | UX-1 |
| 3 | I18N-1 | Catalogue français, sans changer un mot | 4 à 6 j | UX-2 |
| 4 | UX-3 | Navigation de la fenêtre principale, Réglages en page | 3 à 4 j | I18N-1 |
| 5 | UX-4 | Mon pool | 2 à 3 j | UX-3 |
| 6 | UX-5 | Mes statistiques et clé Riot unique | 2 à 3 j | UX-3 |
| 7 | UX-6 | Overlay compact et menu de l'icône | 1 à 2 j | UX-3 |
| 8 | UX-7 | Système visuel | 2 à 3 j | UX-4, UX-5, UX-6 |
| 9 | UX-8 | Accueil et premier lancement | 2 j | UX-7 |
| 10 | I18N-2 | Catalogue anglais | 3 à 4 j | UX-8 |
| 11 | I18N-3 | Sélecteur de langue et textes natifs | 2 à 3 j | I18N-2 |
| 12 | I18N-4 | Parcours FR et EN, guide | 2 j | I18N-3 |

Total : environ 28 à 39 jours. UX-4, UX-5 et UX-6 peuvent avancer en parallèle (fichiers distincts une fois UX-3 fusionnée).

## UX-0 : repères de test stables

- **But.** Les tests ne doivent plus chercher les boutons par leur texte ni par une structure qui va bouger.
- **Fichiers.** `web/index.html` (attributs `data-test` sur réglages, pool, boutons de fermeture, import, mise à jour), `src/DraftMid.cs` (`TestFullWindow`, `TestProfilesAndRole`, `TestHistoryWindow`, sélecteurs `:1320-1321`, `:1379`, `:1467`, `:1479`, `:1526-1533`).
- **Tests.** `--selftest`, `--e2e`, `run-layout-tests.ps1` inchangés et verts.
- **Critère.** 0 sélecteur par texte dans les tests natifs ; aucune différence visible (captures identiques).

## UX-1 : corrections de comportement

- **Contenu.**
  1. Synchronisation : une page qui s'ouvre ne diffuse aucun état avant d'avoir reçu celui des autres pages, ou un court délai sans réponse (`web/index.html:896-905`, `:943-950`, `:2354`, `:2371`).
  2. Saisie sans rôle : champ désactivé avec la consigne « Choisis d'abord ton rôle » (`:2158-2191`).
  3. Modifier le pool d'un rôle ne change plus le rôle joué (`:2100-2112`, `:2165`). Bouton explicite « Jouer ce rôle ».
  4. Échap ne vide que le champ ; « Nouvelle draft » propose « Annuler » 5 secondes (`:2381-2391`).
  5. Retour des actions du menu : bulle pour chaque résultat de mise à jour lancée depuis le menu (`src/DraftMid.cs:1777-1822`).
  6. Première fermeture de la fenêtre principale : bulle « DRAFT reste actif près de l'horloge » (une fois, mémorisée dans `settings.json`).
- **Tests.** Nouveau test e2e : 50 ouvertures de l'analyse depuis un panneau rempli. Test du profil : modifier Top avec Mid actif. Test clavier : Échap dans le champ vide.
- **Critères.** 0 perte sur 50 ouvertures (`docs/ux/outils/repro-desynchro.ps1 -Runs 50`, aujourd'hui 10 sur 10 perdues). Rôle joué inchangé après modification d'un autre pool. Aucune saisie perdue sans message.

## UX-2 : simulateur isolé

- **Contenu.** Deux sources dans la fenêtre principale : « Draft en cours » (partagée avec l'overlay) et « Simulation » (locale). Bandeau en simulation. « Draft aléatoire » et « Nouvelle draft » n'agissent que sur la simulation. Bouton « Reprendre la draft en cours ».
- **Fichiers.** `web/index.html` (`publish`, canal, `App`, `startRandomDraft`, `resetDraft`), `src/DraftMid.cs` (`OpenFull` : paramètre `source`).
- **Tests.** e2e : une draft aléatoire en simulation laisse les panneaux inchangés ; « Détails » depuis l'overlay ouvre la source « Draft en cours ».
- **Critère.** 0 changement des panneaux sur 20 actions de simulation.

## I18N-1 : catalogue français, sans changer un mot

- **Contenu.** `web/i18n/fr.js`, fonctions `t`, `tp`, `fmtDate`, `fmtNumber`. Moteur : messages structurés. Pluriels « (s) » remplacés par de vrais pluriels **seulement si la PR C-2 les a déjà validés** ; sinon, gardés tels quels.
- **Fichiers.** Voir [PLAN-I18N-FR-EN.md](PLAN-I18N-FR-EN.md), section 10.
- **Tests.** `extract-ui-messages.ps1` avant et après sur les drafts du banc ; banc moteur (PR #8) ; pseudo-langue sur le banc de rendu.
- **Critères.** 0 différence de message en français. 0 différence de classement. 0 texte visible sans crochets en pseudo-langue.

## UX-3 : navigation de la fenêtre principale

- **Contenu.** Barre de navigation : Accueil (squelette), Mon pool (contenu actuel de « Mes profils »), Simulateur (vue actuelle), Mes statistiques (onglet Parties actuel), Réglages (page à sous-sections, plus de modale). Adresses `?view=` ; alias des anciennes `?tab=` pendant une version.
- **Fichiers.** `web/index.html` (`App`, `Settings`, `SidePanel`), `src/DraftMid.cs` (`OpenFull`, `openSettings` devient `openView`, `OnMessage`), `tools/run-ux-shots.ps1`, `web/draft-layout-tests.html`, `tools/run-layout-tests.ps1`.
- **Tests.** Banc de rendu sur chaque section ; e2e « fenêtre principale ».
- **Critères.** Section active toujours visible. Toute fonction de l'ancienne modale accessible en 2 clics au plus depuis l'Accueil. Les anciennes adresses mènent au bon endroit.

## UX-4 : Mon pool

- **Contenu.** Cinq rôles, rôle joué explicite, exclusions et favori par rôle (sortis de « Mes picks »), export et import des profils.
- **Fichiers.** `web/index.html` (`ProfileCard`, `ProfileBackup`, `savePrefs`), `web/draft-profiles.js` si le format bouge (à éviter).
- **Tests.** `draft-profiles-tests.html`, `TestProfilesAndRole`.
- **Critères.** Parcours C en 3 actions sans changer le rôle joué. Parcours B en 5 actions, sans échec silencieux.

## UX-5 : Mes statistiques et clé Riot unique

- **Contenu.** Journal et historique Riot réunis ; ancien import de fichier replié ; plus de consigne `.bat`. Un dialogue de clé unique qui dit ce qui sera lancé (historique, rôles, matchups), pour quel rôle et quel pool, et combien de temps.
- **Fichiers.** `web/index.html` (`StatsTab`, `JournalTab`, `CoachTab`), `src/HistoryImporter.cs`, `src/RoleRatesUpdater.cs` (dialogues), `src/DraftMid.cs` (`ToggleHistoryImport`, `ToggleRiotUpdate`).
- **Tests.** `TestHistoryImport`, `TestHistoryWindow`, `LayoutProblem()` des dialogues.
- **Critères.** Parcours D : les parties importées visibles là où on les cherche (« Mes statistiques »). Clé saisie dans un seul modèle de dialogue.

## UX-6 : overlay compact et menu de l'icône

- **Contenu.** Outils de l'overlay : Détails, Panneaux (ouvre Réglages > Panneaux), Masquer (rappelle le raccourci une fois). Icônes SVG. Menu de l'icône à huit commandes ; le double-clic ouvre DRAFT ; le raccourci du menu Démarrer ouvre l'Accueil.
- **Fichiers.** `web/index.html` (`TitleBar`, `PanelApp`), `src/DraftMid.cs` (menu, `tray.DoubleClick`, démarrage sans fenêtre).
- **Tests.** e2e complet (placement, focus, apparition) ; banc de rendu des panneaux.
- **Critères.** e2e « TOUT EST BON ». Panneaux toujours visibles en moins de 50 ms après détection. Aucun vol de focus.

## UX-7 : système visuel

- **Contenu.** Échelle de 7 tailles, échelle d'espacement de 4, jetons pour les couleurs hors jetons, bordures de champ à 3:1, trois variantes de boutons, un seul style d'état actif par famille, bouton de fichier stylé, dialogues natifs harmonisés ([DESIGN-SYSTEM.md](DESIGN-SYSTEM.md)).
- **Fichiers.** `web/index.html` (feuille de style), `src/HistoryImporter.cs`, `src/RoleRatesUpdater.cs`.
- **Tests.** Banc de rendu (seuil de texte minimal relevé à 12 px), mesures [outils/mesure-a11y.js](outils/mesure-a11y.js).
- **Critères.** 7 tailles de police au plus. 0 couleur hors jetons. Limites de champ ≥ 3:1. 0 texte sous 12 px.

## UX-8 : Accueil et premier lancement

- **Contenu.** Accueil complet (état, profil joué, deux actions, cartes Mise à jour et Données). Premier lancement en trois étapes dans l'Accueil.
- **Fichiers.** `web/index.html`, `src/DraftMid.cs` (`firstRun`, `HostJson` : date de dernière vérification).
- **Tests.** e2e premier lancement sur dossier vide.
- **Critères.** Parcours A : premier conseil en 6 actions au plus après l'installation. Aucun lancement sans fenêtre quand on passe par le menu Démarrer.

## I18N-2, I18N-3, I18N-4

Voir [PLAN-I18N-FR-EN.md](PLAN-I18N-FR-EN.md).

- **I18N-2** : `web/i18n/en.js`. Critères : clés et variables identiques, pluriels complets.
- **I18N-3** : `Strings` natif, `Language` dans `settings.json`, détection de Windows, sélecteur, `lang=` dans les adresses. Critère : aucun texte français visible en anglais (pseudo-langue et relevé), dialogues sans texte coupé.
- **I18N-4** : parcours A à H en français et en anglais (e2e, captures), guide mis à jour. Critère : e2e vert dans les deux langues.

## Conflits avec les branches actuelles

| Branche | Fichiers communs avec cette feuille de route | Risque | Conduite |
|---|---|---|---|
| `v217/explications-fideles` (PR D, en cours) | `web/index.html`, `web/draft-coaching.js`, `web/draft-mastery.js` | Fort | Fusionner avant UX-0 |
| PR E (mid adverse, en cours) | `web/index.html` (lignes adverses), `web/draft-coaching.js`, `web/draft-data.js` | Fort | Fusionner avant UX-0 |
| PR C-2 (vocabulaire, en cours) | `web/draft-engine.js`, `web/index.html`, `web/draft-coaching.js`, `web/draft-profiles.js`, `web/draft-data.js` | Fort | Fusionner avant UX-0 ; ses textes deviennent la référence de `fr.js` |
| `v217/vocabulaire-lol` (PR #9) | `tools/extract-ui-messages.ps1`, `web/draft-messages-tests.html` | Moyen | Outil réutilisé tel quel en I18N-1, puis adapté aux clés |
| `v217/banc-evaluation` (PR #8) | `.github/workflows/build.yml`, `tests/moteur/*` | Faible | Garde de non-régression du classement |
| `v217/mesure-roles` (PR #7) | `docs/technique/ARCHITECTURE.md` (une ligne), `tools/run-accuracy-tests.ps1` | Faible | Aucun fichier d'interface |
| `v217/protocole-drafts-reelles` (PR #6) | `tools/validation-report.ps1` | Faible | Ne pas traduire les colonnes du relevé |
| `etude/moteur-v217` | Documentation moteur | Faible | Aucun |
| `docs/validation-2161-resultats` (PR #5) | Documentation | Nul | Aucun |

Points d'attention :

- Les textes de l'audit qui relèvent du vocabulaire ou de la fidélité des explications (« Recommandé » et « Choix serré » ensemble, « Je prends », « Avis général basé sur les 5 champions ») **vont changer** dans les PR D, E et C-2. Cette étude ne les corrige pas ; la feuille de route part de leur version fusionnée.
- `main` locale est en retard (`b4fdeba`) : toute PR part de `origin/main`.
- `tools/run-ux-shots.ps1` lit `%LOCALAPPDATA%\DraftMid\draft-role-rates-live.js` s'il existe : à rendre optionnel (paramètre de dossier) avant de s'en servir comme banc de captures partagé.
