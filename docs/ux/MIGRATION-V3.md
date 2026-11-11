# Plan de migration vers l'interface 3.0 (option B)

**Maquette non validée.** Ce plan dit où va chaque fonctionnalité existante, dans quel ordre on déplace, quels fichiers chaque PR touche, et où les nouveaux écrans rencontrent l'internationalisation. Référence de code : `integration/v2.17` (commit `a6fe325`, 2.16.2 incluse). **Aucune fonctionnalité ne disparaît** : l'inventaire du § 2 a été fait dans le code (`web/index.html`, `src/DraftMid.cs`), pas dans la documentation.

## 1. Correspondance des écrans

| Écran actuel | Devient |
|---|---|
| Fenêtre principale « DRAFT » (« Analyse détaillée ») | **Simulateur** (source Simulation ou Draft en cours) |
| Barre du haut de la fenêtre (rôle, patch, Réglages, Draft aléatoire, Nouvelle draft) | Pied de navigation (rôle joué), en-tête et bandeau du Simulateur |
| Onglets Plan, Analyse (Critères, Comparer, Règles), Conseils, Duel | Onglets du Simulateur à un seul niveau : Plan, Critères, Comparer, Conseils, Duel ; Règles devient l'aide « Comment DRAFT classe tes picks » |
| Onglet Parties (journal + statistiques importées) | **Mes statistiques** |
| Modale Réglages (12 sections) | **Mon pool** (profils, Mes picks) + page **Réglages** (6 rubriques) |
| Aucun écran d'accueil | **Accueil** (avec premier lancement) |
| Panneaux gauche, droit, unique | Overlay compact ([OVERLAY-V3.md](OVERLAY-V3.md)) |
| Menu de l'icône (16 entrées) | Menu de 8 entrées, un sous-menu |
| Dialogues de clé (historique, rôles, matchups) | Un modèle de dialogue natif, harmonisé |

## 2. Inventaire des fonctionnalités et destination

Légende PR : voir § 3. « = » : inchangé, seulement déplacé ou restylé.

### Fenêtre principale et simulation

| # | Fonctionnalité (code) | Où aujourd'hui | Où en 3.0 | PR |
|---|---|---|---|---|
| 1 | Changement de rôle actif en un clic (`RoleSwitch`) | Barre du haut, panneaux | Segment « Rôle joué » (Mon pool, overlay) ; pied de navigation en lecture | UX-3, UX-4 |
| 2 | Proposition de League : « LoL t'attribue X », Passer, Ignorer (`roleSuggestion`) | Sous le sélecteur de rôle | Overlay (sous le rôle joué) et bandeau de l'Accueil | UX-6 |
| 3 | Version du patch (Data Dragon) | Barre du haut | En-tête du Simulateur | UX-3 |
| 4 | Draft aléatoire (`startRandomDraft`) | Barre du haut | Bandeau Simulation | UX-2 |
| 5 | Nouvelle draft (`resetDraft`) | Barre du haut | « Nouvelle simulation » + « Annuler » 5 s | UX-1, UX-2 |
| 6 | Bandeau « Simulation : ta draft en cours n'est pas modifiée » + « Revenir à la draft en cours » (2.16.2) | Fenêtre | Bandeau or permanent + bascule de source « Simulation / Draft en cours » | UX-2 |
| 7 | Recherche d'adversaire : suggestions, flèches, Entrée, surnoms (`SearchBox`) | Colonne gauche, panneau gauche | = | — |
| 8 | Lignes de rôle : viser un rôle, échanger, placer au mid, retirer (`RoleRow`, `clickRole`, `setMid`) | idem | = | — |
| 9 | Hypothèse de mid (« Mid probable », `hypothesisText`) | idem | = (couleur `--info`) | UX-7 |
| 10 | Profil de l'équipe : 5 jauges, forces et failles (`Meter`, `DraftEffects`) | idem | = | — |
| 11 | Conseil : étiquettes Choix net / serré, n/5, alternative, raisons, danger, lignes de conseil, « Plus de détails » (à éviter, tes parties, sans frontline), annonce « X passe devant Y » (`Stage`) | Centre | Centre du Simulateur | — |
| 12 | Égalité (`TieGroup`) | Centre, panneau | = | — |
| 13 | « Je joue X », « X noté » (`choosePick`) | Centre, panneau | = (bouton principal unique) | — |
| 14 | Ton pool : 4 meilleurs, mot de rang, barre de force, bilan (`Ranking`) | Centre | = | — |
| 15 | Plan de jeu : Gagne si, menaces, cibles, bans (`PlanBlock`) | Colonne droite | En-tête de la colonne de détails | — |
| 16 | Plan par phase Lane / Mid game / Teamfights (`PlanTab`) | Onglet Plan | Onglet Plan | UX-3 |
| 17 | Critères par champion, bans conseillés (`CriteriaView`) | Analyse > Critères | Onglet Critères | UX-3 |
| 18 | Comparer deux champions (`CompareTab`) | Analyse > Comparer | Onglet Comparer (cible de l'alternative de l'overlay) | UX-3 |
| 19 | Règles, provenance, fiabilité (`RulesTab`) | Analyse > Règles | Aide « Comment DRAFT classe tes picks » (lien) | UX-3 |
| 20 | Conseils : le plus joué, expérience, vérifiés, à confirmer, incertain (`CoachTab`) | Onglet Conseils | Onglet Conseils | UX-3 |
| 21 | Niveau des conseils (`CoachLevel`, en double) | Réglages + Conseils | Réglages > Général ; lien depuis l'onglet Conseils | UX-3 |
| 22 | Duel : bilans contre un adversaire, sans toucher la draft (`MatchupsTab`) | Onglet Duel | Onglet Duel | — |
| 23 | Résultat Victoire / Défaite / Ignorer, dernière partie en attente (`ResultButtons`) | Panneau, fenêtre | Overlay + journal de Mes statistiques | UX-5 |
| 24 | Barre mobile (fenêtre étroite) (`MobileBar`) | Fenêtre étroite | = | — |
| 25 | Raccourcis : Échap, Retour arrière, lettre → recherche | Fenêtre, panneau | Échap ne vide que le champ ; Retour arrière avec « Annuler » | UX-1 |
| 26 | Annonce du conseil (`aria-live`) | Fenêtre, panneau | = | — |
| 27 | Mode démonstration (`runDemo`, message `demo`) | Tests | + étape « Essayer les panneaux » du premier lancement | UX-8 |

### Profils et réglages (modale actuelle)

| # | Fonctionnalité | Où aujourd'hui | Où en 3.0 | PR |
|---|---|---|---|---|
| 28 | Choix du rôle et pool par rôle, suggestions par rôle, enregistrement auto (`ProfileCard`) | Réglages > Mes profils | **Mon pool** (liste des 5 rôles, tableau, recherche) | UX-4 |
| 29 | Export, import, « Annuler le dernier import » (`ProfileBackup`) | Mes profils | Mon pool > Sauvegarde des profils | UX-4 |
| 30 | Exclure pour l'instant (`PREFS.excluded`, rôle actif seulement) | Mes picks | Mon pool, colonne par rôle | UX-4 |
| 31 | Favori à égalité exacte (`PREFS.favorite`) | Mes picks | Mon pool, colonne par rôle | UX-4 |
| 32 | Supposer une frontline (`assumeFront`) | Mes picks | Réglages > Général | UX-3 |
| 33 | Mise à jour : version, état, bouton (`checkUpdate`) | Réglages | Réglages > Mises à jour + carte Accueil | UX-3, UX-8 |
| 34 | Historique Riot : nombre, date, Importer / Arrêter / Importer plus (`importHistory`) | Réglages | Mes statistiques > Historique Riot | UX-5 |
| 35 | Lecture du client (`setLcu`, verrouillée tant que `lcuConfirmed` est faux), état LCU | Réglages > Détection | Réglages > Connexion League + Accueil | UX-3 |
| 36 | Rôle attribué par League : proposer, changer, rien (`autoRole`) | Réglages > Détection | Réglages > Connexion League | UX-3 |
| 37 | Affichage automatique (`setAutoDraft`) | Réglages + menu | Réglages > Affichage et fenêtres | UX-3, UX-6 |
| 38 | Coller au client (`setFollow`) | Réglages + menu | Réglages > Affichage et fenêtres | UX-3, UX-6 |
| 39 | Un seul panneau (`toggleCompact`) | Réglages + menu + overlay | Réglages > Affichage + overlay | UX-3, UX-6 |
| 40 | Toujours au premier plan (`setTopmost`) | Réglages | Réglages > Affichage | UX-3 |
| 41 | Démarrer avec Windows (`setAutostart`) | Réglages + menu | Réglages > Général | UX-3, UX-6 |
| 42 | Rôles des champions : date, calcul, arrêt (`updateRoles`) | Réglages + menu | Réglages > Données et confidentialité | UX-3, UX-5 |
| 43 | Matchups : rôle, nombre par champion, calcul (`updateMatchups`) | Réglages + menu | Réglages > Données et confidentialité | UX-3, UX-5 |
| 44 | Import d'un journal (fichier JSON) | Réglages > Journal | Mes statistiques > Importer un ancien fichier (bouton stylé) | UX-5 |
| 45 | Sources de données, rôles à surveiller (`DataSources`) | Réglages, repliable | Réglages > Données, repliable | UX-3 |
| 46 | Journal de diagnostic (`setDiagnostics`), phase et placement | Réglages > Avancé | Réglages > Diagnostic et informations | UX-3 |
| 47 | Réglages seuls pendant une draft (`&solo=1`, `closeFull`) | Modale | « Réglages d'affichage » de l'overlay ouvre la rubrique ; la fenêtre se referme à la fermeture si elle était cachée | UX-3, UX-6 |
| 48 | Mention « Non approuvé par Riot Games », version bêta | En-tête des réglages | Pied de navigation + Diagnostic et informations | UX-3 |

### Panneaux (overlay)

| # | Fonctionnalité | Où en 3.0 | PR |
|---|---|---|---|
| 49 | Barre de titre : mode, réglages, masquer (`TitleBar`) | Trois icônes SVG ; réglages → rubrique Affichage ; bulle au premier masquage | UX-6 |
| 50 | Panneau sans profil : message + « Choisir mon profil » | État vide → « Ouvrir Mon pool » | UX-6 |
| 51 | Alternative → Comparer ; « Analyse détaillée » | Alternative → onglet Comparer ; « Détails » (source Draft en cours) | UX-2, UX-6 |
| 52 | « Plan de jeu et ban » repliable | = | — |
| 53 | Panneau unique (`CompactEnemies`) | = (puces d'adversaires) | UX-6 |
| 54 | Ouverture auto des réglages si aucun profil (`draftProfileAsked`) | Ouvre Mon pool | UX-4 |

### Programme natif

| # | Fonctionnalité | Où en 3.0 | PR |
|---|---|---|---|
| 55 | Icône, infobulle de phase | = | — |
| 56 | Double-clic : bascule des panneaux | Ouvre DRAFT (à valider) ; bascule par Ctrl+Alt+D et menu | UX-6 |
| 57 | Raccourci global Ctrl+Alt+D | = | — |
| 58 | Apprendre l'écran (C'est / Ce n'est pas une draft, `Teach`) | Sous-menu « Détection de la draft » | UX-6 |
| 59 | Diagnostic de connexion LoL (2.16.2, copié dans le presse-papiers) | Sous-menu + Réglages > Diagnostic | UX-3, UX-6 |
| 60 | Importer mes statistiques (`draft-stats.js`, `ImportStats`) | Mes statistiques > Importer un ancien fichier (commande hôte `importStats`) | UX-5 |
| 61 | Dialogues de clé : historique, rôles, matchups | Un modèle commun, barre de titre sombre, case d'accord gardée | UX-5, UX-7 |
| 62 | Mise à jour : signature, `--selftest`, remplacement, retour arrière, blocage pendant une draft | = ; bulle pour chaque résultat lancé du menu | UX-1 |
| 63 | Bulles (historique, rôles, apprentissage, mise à jour) | = + bulle de première fermeture + bulle de masquage | UX-1, UX-6 |
| 64 | Premier lancement : ouvre les réglages (`firstRun`) | Ouvre l'Accueil, premier lancement | UX-8 |
| 65 | Instance unique (« DRAFT est déjà ouvert ») | Ouvre la fenêtre de l'instance existante (message conservé en repli) | UX-8 |
| 66 | Installation, désinstallation, WebView2 absent | = (textes vers le catalogue `native.*`) | I18N-3 |
| 67 | Copie des profils dans le dossier de données (`saveProfiles`, `savedProfiles`) et adoption de l'ancien profil (`legacyProfile`) | = | — |
| 68 | Envoi du profil au calcul des matchups (`setProfile`) | = | — |
| 69 | Relevé de validation CSV (`logDraft`, si diagnostic actif) | = ; mentionné dans Données et confidentialité | UX-3 |
| 70 | Détection : LCU, estimation par l'image, rôle attribué | = ; source affichée dans l'overlay | UX-6 |
| 71 | Placement multi-écrans (`Layout.Compute`, `Narrowest`) | = ; mode affiché sur l'Accueil | UX-8 |
| 72 | Modes de test (`--selftest`, `--e2e`, `--soak`, `--capture`, `--report-state`…) | = ; sélecteurs adaptés (UX-0) | UX-0 |

Seule fonctionnalité **nouvelle** et non indispensable : l'export du journal (mentionné dans l'architecture). Elle n'est pas dans les maquettes et reste hors périmètre de la 3.0.

## 3. Découpage en PR

Ordre : après la fusion de V2.17. I18N-1 (catalogue français sans changer un mot) passe entre UX-2 et UX-3. UX-4, UX-5 et UX-6 peuvent avancer en parallèle une fois UX-3 fusionnée. Chaque PR garde les anciennes adresses comme alias.

| PR | Sujet | Fichiers touchés | Tests et preuves | Effort |
|---|---|---|---|---|
| **UX-0** | Repères de test stables (`data-test`), 0 changement visible | `web/index.html` (attributs), `src/DraftMid.cs` (`TestFullWindow`, `TestProfilesAndRole`, `TestHistoryWindow`), `web/draft-layout-tests.html` (sélecteurs) | `--selftest`, `--e2e`, `run-layout-tests.ps1` verts ; captures identiques | 1 j |
| **UX-1** | Corrections de comportement : synchronisation des pages, champ désactivé sans rôle, modifier un pool ne change pas le rôle joué, Échap, « Annuler » 5 s, bulles des actions du menu, bulle de première fermeture | `web/index.html` (canal `hello`, `ProfileCard`, `saveMyProfile`, `onKey`, `resetDraft`), `src/DraftMid.cs` (`RunUpdate`, fermeture de la fenêtre, `settings.json` : drapeau « bulle vue »), `docs/ux/outils/repro-desynchro.ps1` | Nouveau test e2e « 50 ouvertures de Détails » ; test profil Top avec Mid joué ; test clavier Échap | 2 à 3 j |
| **UX-2** | Simulateur isolé (termine la 2.16.2) : état local, bascule de source, « Reprendre la draft en cours » (copie), « Détails » ouvre la source live sans réinitialiser | `web/index.html` (`simulating`, `leaveSimulation`, `publish`, `startRandomDraft`, `App`), `src/DraftMid.cs` (`OpenFull` : `source`) | e2e : 20 actions de simulation, 0 changement des panneaux | 2 j |
| **UX-3** | Coquille de navigation (5 sections, rail sous 1 100 px), Réglages en page à 6 rubriques, onglets du Simulateur à un niveau, Mes statistiques vide (squelette), adresses `?view=`, message `openView` ; jetons CSS ajoutés sans changement visible ; `web/app.css` et `web/ui/*.js` créés | `web/index.html` (`App`, `Settings` → `SettingsPage`, `SidePanel`), `web/app.css`, `web/ui/nav.js`, `web/ui/settings.js`, `src/DraftMid.cs` (`OpenFull`, `OnMessage`, `openSettings`), `tools/run-layout-tests.ps1`, `web/draft-layout-tests.html`, `tools/run-ux-shots.ps1` | Banc de rendu : chaque section × 5 tailles ; e2e fenêtre principale ; alias d'adresses | 3 à 4 j |
| **UX-4** | Mon pool : liste des rôles, rôle joué explicite, recherche, exclusions et favori **par rôle**, confirmation et annulation, sauvegarde | `web/index.html` (`ProfileCard` → `PoolPage`, `ProfileBackup`, `savePrefs`), `web/draft-profiles.js` (exclusions et favori par rôle : migration douce des `PREFS` existantes), `web/draft-profiles-tests.html` | `draft-profiles-tests.html` (migration des préférences), `TestProfilesAndRole` | 2 à 3 j |
| **UX-5** | Mes statistiques (journal + historique Riot + anciens imports) ; dialogue de clé unique | `web/index.html` (`JournalTab`, `StatsTab`, `CoachTab` en partie), `src/HistoryImporter.cs`, `src/RoleRatesUpdater.cs` (dialogue commun), `src/DraftMid.cs` (`ToggleHistoryImport`, `ToggleRiotUpdate`, `ImportStats` → commande `importStats`) | `TestHistoryImport`, `TestHistoryWindow`, `LayoutProblem()` des dialogues | 2 à 3 j |
| **UX-6** | Overlay compact (icônes SVG, outils, source affichée, état vide vers Mon pool) ; menu de l'icône à 8 entrées ; double-clic | `web/index.html` (`TitleBar`, `PanelApp`, `VerdictPanel`, `RoleSwitch`), `src/DraftMid.cs` (menu, `tray.DoubleClick`, bulle de masquage) | e2e complet (placement, focus, apparition < 50 ms) ; banc de rendu des panneaux à 230, 340, 374, 400 px | 1 à 2 j |
| **UX-7** | Système visuel : 7 tailles, échelle de 4, jetons de couleur, `--line-strong`, boutons à 3 variantes, un état actif par famille, mouvements retirés, dialogues natifs sombres | `web/app.css`, `web/index.html` (classes), `src/HistoryImporter.cs`, `src/RoleRatesUpdater.cs`, `src/DraftMid.cs` (`DwmSetWindowAttribute`) | Banc de rendu (seuil 12 px), [outils/mesure-a11y.js](outils/mesure-a11y.js), relevé des tailles et couleurs | 2 à 3 j |
| **UX-8** | Accueil complet et premier lancement (3 étapes, essai des panneaux), menu Démarrer → Accueil, instance unique → ouvre la fenêtre | `web/index.html` (`HomePage`), `src/DraftMid.cs` (`firstRun`, `HostJson` : dernière vérification, mode de placement ; deuxième instance), `docs/utilisateur/GUIDE.md` | e2e premier lancement sur dossier vide ; parcours A mesuré | 2 j |

Total UX : 17 à 23 jours, comme estimé dans [FEUILLE-DE-ROUTE-UX.md](FEUILLE-DE-ROUTE-UX.md) (option B).

**Compatibilité des données.** Aucune PR ne change le format de `settings.json` sans valeur par défaut, ni celui des profils sans migration testée (UX-4 : `excluded` et `favorite` passent du profil actif à chaque rôle ; l'ancien champ est lu puis recopié dans le rôle actif).

## 4. Risques

| Risque | Effet | Parade |
|---|---|---|
| Conflits avec les PR moteur sur `web/index.html` | Rebases coûteux | UX-0 seulement après la fusion de V2.17 ; nouveaux écrans dans `web/ui/*.js` pour réduire la surface commune |
| Tests natifs qui cherchent du texte ou `.settings` | e2e rouge | UX-0 d'abord ; alias `openSettings` pendant une version |
| Régression de la synchronisation des panneaux | Perte de draft | UX-1 avant toute navigation ; critère 0 sur 50 |
| Double-clic changé | Habitude cassée | À valider avec l'utilisateur ; repli : garder la bascule et ajouter « Ouvrir DRAFT » en gras |
| Cases retirées du menu | +2 clics | « Panneau unique » reste dans l'overlay ; à revoir après test |
| Fenêtre trop petite à 175 % sur 1366 × 768 (~780 × 410 logiques) | Défilement | Rail de navigation ; contenu défilant ; critère « aucune troncature », défilement permis |
| Clé Riot | Fuite | Inchangé : dialogue natif, jamais dans la page, jamais enregistrée |
| Illustrations Data Dragon absentes (hors ligne) | Zone vide | Message existant « Illustration indisponible » ; le nom reste lisible sur fond uni |

## 5. Points de contact avec l'internationalisation

Le catalogue de langue est préparé par un autre agent (plan : [PLAN-I18N-FR-EN.md](PLAN-I18N-FR-EN.md), fichiers `web/i18n/fr.js` et `en.js`, objet JSON strict, clés plates à points, variables nommées, pluriels `Intl.PluralRules`). Règles pour la refonte :

1. **Aucun texte en dur dans un nouvel écran** à partir d'UX-3. Chaque texte passe par `t("clé", vars)` ou `tp("clé", n, vars)`. Si I18N-1 n'est pas fusionnée, UX-3 l'attend (ordre de la feuille de route).
2. **Espaces de noms** : `nav.`, `home.`, `pool.`, `sim.`, `stats.`, `settings.general.`, `settings.league.`, `settings.display.`, `settings.data.`, `settings.update.`, `settings.diag.`, `overlay.`, `native.menu.`, `native.key.`, `native.balloon.`.
3. **Textes français de référence** : ceux des maquettes, sauf s'ils touchent le vocabulaire du moteur (« Choix net », « Recommandé », raisons) : ceux-là viennent du moteur et de la PR C-2, pas de la refonte.
4. **Longueurs** : les libellés de navigation et d'onglets doivent tenir en anglais dans le rail de 92 px (deux lignes au plus). Pseudo-langue `qps` (+35 %) passée sur chaque section par `run-layout-tests.ps1`.
5. **Nombres et dates** : `fmtDate`, `fmtNumber` (« 08/11/2026 », « 4 820 ») ; plus de `toLocaleDateString("fr-FR")` ni de « (s) ».
6. **Natif** : menu de l'icône, bulles et dialogue de clé lisent `Strings.T("native.…")` (I18N-3). UX-6 et UX-5 écrivent leurs nouveaux libellés directement avec ces clés si `Strings` existe ; sinon, une seule constante par libellé, regroupée, pour la migration.
7. **Sélecteur de langue** : Réglages > Général > Langue (« Automatique (Windows) », « Français », « English ») ; présent dans la maquette, actif en I18N-3.

Exemples de clés pour les nouveaux écrans (français de référence) :

| Clé | Texte |
|---|---|
| `nav.home` / `nav.pool` / `nav.sim` / `nav.stats` / `nav.settings` | Accueil / Mon pool / Simulateur / Mes statistiques / Réglages |
| `home.ready.title` | Tout est prêt pour ta prochaine draft |
| `home.ready.idle` | Aucune draft en cours. Les panneaux s'ouvriront à côté du client dès la sélection des champions. |
| `home.check.league.ok` | Client League connecté |
| `home.check.pool` | Rôle joué : {role}, {count} champions (pluriel) |
| `home.first.title` | Prépare ta première draft |
| `home.first.trayNote` | Fenêtre fermée, DRAFT reste actif près de l'horloge. Pour revenir ici : menu Démarrer, ou clic sur son icône. |
| `pool.played.label` | Rôle joué |
| `pool.editing.note` | Tu modifies le pool {role}. Ton rôle joué reste {played}. |
| `pool.saved.added` | Enregistré : {champion} ajouté au pool {role}. |
| `pool.search.needRole` | Choisis d'abord un rôle |
| `sim.band.title` / `sim.band.text` | Simulation / Ta draft en cours n'est pas modifiée. |
| `sim.source.sim` / `sim.source.live` | Simulation / Draft en cours |
| `sim.resumeLive` | Reprendre la draft en cours |
| `stats.riot.imported` | {count} parties importées (pluriel) |
| `settings.display.autoShow` | Afficher les panneaux automatiquement pendant la draft |
| `overlay.tools.hide` | Masquer les panneaux |
| `native.menu.open` | Ouvrir DRAFT |
| `native.menu.detection` | Détection de la draft |
| `native.balloon.hidden` | Panneaux masqués. Ctrl+Alt+D pour les rappeler. |
