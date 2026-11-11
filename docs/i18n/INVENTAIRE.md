# Inventaire des chaînes visibles (FR → clés)

Fichier généré par `tools/i18n/build-inventory.ps1` (base `a6fe325`). Ne pas éditer : corriger les règles du script, puis le relancer. Version machine : [`inventaire.json`](inventaire.json).

L'audit n'est pas refait. La page reprend les 594 entrées du catalogue de la PR #9 (`web/draft-messages-tests.html`, bloc `catalog`). Le natif complète les groupes N-01 à N-26 de `docs/moteur/AUDIT-VOCABULAIRE.md` § 8 par une lecture outillée de `src/*.cs` (détecteur), une entrée par ligne de code.

## Résumé

| Mesure | Valeur |
|---|---|
| Entrées de la page (catalogue PR #9) | **594** |
| Entrées natives (src/*.cs, une par ligne de code) | **156** |
| dont rattachées à un groupe N-xx de l'audit | 60 (sur 26 groupes) |
| dont affichées dans la page (état transmis par le programme) | 60 |
| Entrées marquées « sécurité » (sens à garder mot pour mot) | 42 |
| Entrées de la page sans ligne retrouvée dans le code actuel | 18 |
| Identifiants en double dans le catalogue | UI-269 (le second reçoit le suffixe -bis) |
| Chaînes de la page vues par le détecteur | 1029 |
| dont sans gabarit correspondant dans le catalogue | 97 (à vérifier en I18N-1, liste dans le JSON) |

| Catégorie | Entrées |
|---|---|
| erreurs | 11 |
| navigation | 38 |
| conseil/moteur | 447 |
| rôles | 33 |
| sécurité | 13 |
| réglages | 52 |
| natif | 156 |

| Fichier | Entrées |
|---|---|
| `web/index.html` | 270 |
| `web/draft-engine.js` | 109 |
| `web/draft-coaching.js` | 170 |
| `web/draft-mastery.js` | 25 |
| `web/draft-data.js` | 15 |
| `web/draft-profiles.js` | 5 |
| `src/AppUpdater.cs` | 18 |
| `src/DraftDetection.cs` | 6 |
| `src/DraftMid.cs` | 73 |
| `src/HistoryImporter.cs` | 12 |
| `src/LcuConnector.cs` | 28 |
| `src/RoleRatesUpdater.cs` | 19 |

## Règles de nommage des clés

- Forme : `zone.sousZone.libellé`, en ASCII, `camelCase` pour chaque segment. Le dernier segment vient des premiers mots du texte français de référence (sans accents). Il identifie le texte ; il ne le traduit pas.
- Zones de la page : `nav`, `brand`, `legal`, `draft` (saisie, notices, simulateur), `enemy` (lignes et profil adverses), `role`, `advice` (conseil affiché, égalités, carte), `pool`, `plan`, `analysis`, `compare`, `duel`, `coachTab`, `stats`, `journal`, `rules`, `panel`, `settings`, `error`. Moteur : `engine`, `coach` (identifiant de règle repris de `draft-coaching.js`, puis `title`, `level1`, `level2`, `level3`, `limit`, `risk`), `mastery`, `data`, `profiles`.
- Natif : `native.<zone>.libellé` ; zones `menu`, `balloon`, `dialog`, `app`, `install`, `phase`, `layout`, `detection`, `lcu`, `lcu.status`, `lcu.reason`, `lcuDiag`, `history`, `history.dialog`, `history.status`, `riot`, `riot.dialog`, `riot.region`, `riot.status`, `update`, `update.error`.
- Une entrée du catalogue qui regroupe plusieurs libellés (alternance `^(A|B|C)$`) reçoit une clé de groupe et des sous-clés (colonne « sous-clés » du JSON).
- Les clés proposées ici sont stables dès leur livraison en I18N-1 : un texte qui change garde sa clé ; une clé ne se réutilise jamais pour un autre sens.

## Catégories

| Catégorie | Contenu |
|---|---|
| navigation | Structure et saisie : onglets, panneaux, barre du haut, saisie de la draft, notices, journal, statistiques |
| conseil/moteur | Recommandation, égalités, plan, analyse, comparaison, duel, règles, raisons du moteur, conseils, maîtrise |
| rôles | Lignes adverses, adversaire direct, noms de rôles, rôle attribué |
| réglages | Réglages, sources de données, profils |
| erreurs | Refus, échecs, fichiers illisibles (texte qui contient un mot d'erreur) |
| natif | Tout texte écrit par le programme Windows (src/*.cs) ; les drapeaux « sécurité » et « erreur » restent dans le JSON |
| sécurité | Clé jamais enregistrée, rien n'est envoyé, rien n'a été modifié, signature et empreinte, consentement, mention Riot : sens à garder mot pour mot (voir STRATEGIE.md § 5) |

## Textes natifs (complément de l'audit § 8)

| ID | Fichier:ligne | Classe.méthode | Audit | Affichage | Clé proposée | Texte (fragments séparés par {…}) |
|---|---|---|---|---|---|---|
| NA-001 (S,E) | `src/AppUpdater.cs:106` | AppUpdater.CheckFile |  | page | `native.update.error.telechargementIncompletRien` | Le téléchargement est incomplet. Rien n'a été changé. |
| NA-002 (S,E) | `src/AppUpdater.cs:108` | AppUpdater.CheckFile |  | page | `native.update.error.fichierRecuExecutable` | Le fichier reçu n'est pas un exécutable. Rien n'a été changé. |
| NA-003 (S) | `src/AppUpdater.cs:109` | AppUpdater.CheckFile |  | page | `native.update.error.empreinteFichierRecu` | L'empreinte du fichier reçu ne correspond pas à celle de GitHub. Rien n'a été changé. |
| NA-004 (S) | `src/AppUpdater.cs:113` | AppUpdater.CheckFile | N-15 | page | `native.update.error.versionFichierRecu` | La version du fichier reçu ne correspond pas à celle annoncée. Rien n'a été changé. |
| NA-005 (S) | `src/AppUpdater.cs:148` | AppUpdater.SelfCheck |  | page | `native.update.error.nouvelleVersionFini` | La nouvelle version n'a pas fini ses tests à temps. Rien n'a été changé. |
| NA-006 (S,E) | `src/AppUpdater.cs:151` | AppUpdater.SelfCheck |  | page | `native.update.error.nouvelleVersionEchoue` | La nouvelle version a échoué à ses tests sur ce PC. Rien n'a été changé. |
| NA-007 (S) | `src/AppUpdater.cs:166` | AppUpdater.Prepare |  | page | `native.update.error.signatureCetteVersion` | La signature de cette version n'est pas valide. Rien n'a été changé. |
| NA-008 (E) | `src/AppUpdater.cs:271` | AppUpdater.FriendlyError |  | page | `native.update.error.draftPeutEcrire` | DRAFT ne peut pas écrire dans son dossier : télécharge la mise à jour depuis GitHub. |
| NA-009 (E) | `src/AppUpdater.cs:272` | AppUpdater.FriendlyError |  | page | `native.update.error.fichierDraftBloque` | Un fichier de DRAFT est bloqué. Réessaie dans un instant. |
| NA-010 (E) | `src/AppUpdater.cs:273` | AppUpdater.FriendlyError |  | page | `native.update.error.githubRepondVerifie` | GitHub ne répond pas. Vérifie ta connexion, puis réessaie. |
| NA-011 (S,E) | `src/AppUpdater.cs:274` | AppUpdater.FriendlyError |  | page | `native.update.error.miseJourEchoue` | La mise à jour a échoué. Rien n'a été changé. |
| NA-012 (S) | `src/AppUpdater.cs:296` | GitHubUpdateSource.ReadLimited |  | page | `native.update.error.reponseGithubAnormalement` | La réponse de GitHub est anormalement grosse. Rien n'a été changé. |
| NA-013 (S) | `src/AppUpdater.cs:305` | GitHubUpdateSource.SignatureAsync |  | page | `native.update.error.cetteVersionEncore` | Cette version n'est pas encore signée. Réessaie dans un moment. |
| NA-014 | `src/AppUpdater.cs:328` | GitHubUpdateSource.LatestAsync |  | page | `native.update.error.derniereVersionPubliee` | La dernière version publiée n'est pas utilisable par la mise à jour automatique. |
| NA-015 | `src/AppUpdater.cs:336` | GitHubUpdateSource.LatestAsync |  | page | `native.update.error.aucuneVersionPubliee` | Aucune version publiée pour le moment. |
| NA-016 | `src/AppUpdater.cs:337` | GitHubUpdateSource.LatestAsync |  | page | `native.update.error.githubLimiteRequetes` | GitHub limite les requêtes pour le moment. Réessaie dans une heure. |
| NA-017 (S) | `src/AppUpdater.cs:352` | GitHubUpdateSource.DownloadAsync |  | page | `native.update.error.telechargementEteRedirige` | Le téléchargement a été redirigé vers un site inconnu. Rien n'a été changé. |
| NA-018 (S) | `src/AppUpdater.cs:362` | GitHubUpdateSource.DownloadAsync |  | page | `native.update.error.fichierRecuGros` | Le fichier reçu est plus gros qu'annoncé. Rien n'a été changé. |
| NA-019 | `src/DraftDetection.cs:21` | PhaseText.Of | N-01 | page | `native.phase.clientLolFerme` | Client LoL fermé |
| NA-020 | `src/DraftDetection.cs:22` | PhaseText.Of | N-01 | page | `native.phase.clientOuvertHors` | Client ouvert, hors draft (estimation) |
| NA-021 | `src/DraftDetection.cs:23` | PhaseText.Of | N-01 | page | `native.phase.draftCoursEstimation` | Draft en cours (estimation) |
| NA-022 | `src/DraftDetection.cs:24` | PhaseText.Of | N-01 | page | `native.phase.partieCours` | Partie en cours |
| NA-023 | `src/DraftDetection.cs:25` | PhaseText.Of | N-01 | page | `native.phase.phaseIncertaine` | Phase incertaine |
| NA-024 | `src/DraftDetection.cs:370` | PanelVisibilityController |  | page | `native.detection.visiblesSeulementPendant` | Visibles seulement pendant une draft, avec le client restauré. Le raccourci manuel décide jusqu'au prochain changement de phase. |
| NA-025 (S) | `src/DraftMid.cs:23` |  |  | Windows | `native.app.draftAssistantStrategique` | DRAFT : ton assistant stratégique pour League of Legends. Non approuvé par Riot Games. |
| NA-026 | `src/DraftMid.cs:101` | Layout.Compute |  | page | `native.layout.panneauxPlacesChaque` | Panneaux placés de chaque côté du client. |
| NA-027 | `src/DraftMid.cs:111` | Layout.Compute | N-05 | page | `native.layout.panneauUniqueCote` | Panneau unique à côté du client. {…} Pas assez de place pour deux panneaux : panneau unique. |
| NA-028 | `src/DraftMid.cs:125` | Layout.Compute |  | page | `native.layout.placeAutourClient` | Pas de place autour du client : panneaux sur l'autre écran. |
| NA-029 | `src/DraftMid.cs:131` | Layout.Compute |  | page | `native.layout.placeAutourClient2` | Pas de place autour du client : panneau sur l'autre écran. |
| NA-030 | `src/DraftMid.cs:134` | Layout.Compute |  | page | `native.layout.placeAutourClient3` | Pas de place autour du client : place les panneaux à la main pour ne rien couvrir. |
| NA-031 | `src/DraftMid.cs:147` | Layout.Narrowest |  | page | `native.layout.placeAutourClient4` | Pas de place autour du client : panneau étroit au bord de l'écran, il en couvre une partie. Déplace-le si besoin. |
| NA-032 | `src/DraftMid.cs:374` | CompanionApp |  | Windows | `native.app.clientLolDetecte` | Client LoL non détecté. |
| NA-033 | `src/DraftMid.cs:406` | CompanionApp.CompanionApp |  | Windows | `native.menu.afficherPanneauxCtrl` | Afficher les panneaux (Ctrl+Alt+D) |
| NA-034 | `src/DraftMid.cs:407` | CompanionApp.CompanionApp | N-03 | Windows | `native.menu.activationAutomatiquePendant` | Activation automatique pendant la draft |
| NA-035 | `src/DraftMid.cs:408` | CompanionApp.CompanionApp | N-04 | Windows | `native.menu.positionnementAutomatique` | Positionnement automatique |
| NA-036 | `src/DraftMid.cs:409` | CompanionApp.CompanionApp | N-05 | Windows | `native.menu.panneauUnique` | Panneau unique |
| NA-037 | `src/DraftMid.cs:410` | CompanionApp.CompanionApp | N-05 | Windows | `native.menu.demarrerWindows` | Démarrer avec Windows |
| NA-038 | `src/DraftMid.cs:413` | CompanionApp.CompanionApp | N-06 | Windows | `native.menu.draftRetenirCet` | C'est une draft : retenir cet écran |
| NA-039 (E) | `src/DraftMid.cs:414` | CompanionApp.CompanionApp | N-06 | Windows | `native.menu.draftRetenirCet2` | Ce n'est pas une draft : retenir cet écran |
| NA-040 | `src/DraftMid.cs:419` | CompanionApp.CompanionApp | N-07 | Windows | `native.menu.replacerPanneaux` | Replacer les panneaux |
| NA-041 | `src/DraftMid.cs:421` | CompanionApp.CompanionApp | N-07 | Windows | `native.menu.analyseDetaillee` | Analyse détaillée |
| NA-042 | `src/DraftMid.cs:422` | CompanionApp.CompanionApp | N-07 | Windows | `native.menu.reglages` | Réglages |
| NA-043 | `src/DraftMid.cs:423` | CompanionApp.CompanionApp | N-25 | Windows | `native.menu.diagnosticConnexionLol` | Diagnostic de connexion LoL... |
| NA-044 | `src/DraftMid.cs:424` | CompanionApp.CompanionApp | N-07 | Windows | `native.menu.importerMesStatistiques` | Importer mes statistiques... |
| NA-045 | `src/DraftMid.cs:425` | CompanionApp.CompanionApp | N-08 | Windows | `native.menu.mettreJourRoles` | Mettre à jour les rôles... |
| NA-046 | `src/DraftMid.cs:426` | CompanionApp.CompanionApp | N-08 | Windows | `native.menu.mettreJourMatchups` | Mettre à jour les matchups... |
| NA-047 | `src/DraftMid.cs:431` | CompanionApp.CompanionApp | N-09 | Windows | `native.menu.rechercherMiseJour` | Rechercher une mise à jour... |
| NA-048 | `src/DraftMid.cs:433` | CompanionApp.CompanionApp | N-10 | Windows | `native.menu.quitter` | Quitter |
| NA-049 (E) | `src/DraftMid.cs:586` | CompanionApp.ShowLcuDiagnostic |  | Windows | `native.lcuDiag.diagnosticInterrompu` | Diagnostic interrompu : |
| NA-050 | `src/DraftMid.cs:589` | CompanionApp.ShowLcuDiagnostic | N-26 | Windows | `native.lcuDiag.texteCopieColle` | Ce texte est copié : colle-le dans ton message. {…} DRAFT : diagnostic de connexion LoL |
| NA-051 | `src/DraftMid.cs:765` | CompanionApp.ToggleHistoryImport |  | page | `native.history.status.arretImportParties` | Arrêt de l'import : les parties déjà lues sont conservées... |
| NA-052 | `src/DraftMid.cs:769` | CompanionApp.ToggleHistoryImport | N-12 | page | `native.history.status.draftCoursLance` | Une draft est en cours : lance l'import après. |
| NA-053 (S) | `src/DraftMid.cs:770` | CompanionApp.ToggleHistoryImport |  | page | `native.history.status.calculRiotDeja` | Un calcul Riot est déjà en cours : attends sa fin pour ne pas dépasser les limites de ta clé. |
| NA-054 | `src/DraftMid.cs:793` | CompanionApp.RunHistoryImport | N-12 | page | `native.history.status.importHistoriqueDemarrage` | Import de l'historique : démarrage... |
| NA-055 | `src/DraftMid.cs:801` | CompanionApp.RunHistoryImport | N-12 | page | `native.history.status.importHistorique` | Import de l'historique : |
| NA-056 | `src/DraftMid.cs:816` | CompanionApp.RunHistoryImport |  | page | `native.history.status.historiqueEnregistreEncore` | Historique enregistré, mais pas encore visible : relance DRAFT. |
| NA-057 | `src/DraftMid.cs:819` | CompanionApp.RunHistoryImport |  | page | `native.history.status.partiesAjouteesPartie` | parties ajoutées, {…} partie ajoutée, {…} au total. |
| NA-058 | `src/DraftMid.cs:820` | CompanionApp.RunHistoryImport | N-12 | page | `native.history.status.importArreteImport` | Import arrêté : {…} Import terminé : |
| NA-059 (E) | `src/DraftMid.cs:821` | CompanionApp.RunHistoryImport |  | page | `native.history.status.compteIntrouvable` | Compte introuvable : |
| NA-060 | `src/DraftMid.cs:829` | CompanionApp.ToggleRiotUpdate |  | page | `native.riot.status.importHistoriqueCours` | Un import de l'historique est en cours : attends sa fin. |
| NA-061 | `src/DraftMid.cs:833` | CompanionApp.ToggleRiotUpdate |  | page | `native.riot.status.arretCalculCours` | Arrêt du calcul en cours... |
| NA-062 | `src/DraftMid.cs:839` | CompanionApp.ToggleRiotUpdate |  | page | `native.riot.status.roles` | rôles |
| NA-063 | `src/DraftMid.cs:849` | CompanionApp.ToggleRiotUpdate |  | page | `native.riot.status.calculeBilansPool` | Calcule les bilans de ton pool contre chaque adversaire direct, à ton rôle, sur des parties des 30 derniers jours. Plus ton pool est grand, … |
| NA-064 | `src/DraftMid.cs:850` | CompanionApp.ToggleRiotUpdate |  | page | `native.riot.status.compteRolesJoues` | Compte les rôles joués sur 1 000 parties récentes, pour mieux deviner le mid adverse. Le calcul prend environ 40 minutes et tourne en arrièr… |
| NA-065 | `src/DraftMid.cs:851` | CompanionApp.ToggleRiotUpdate |  | page | `native.riot.status.draftMettreJour` | DRAFT : mettre à jour les |
| NA-066 | `src/DraftMid.cs:859` | CompanionApp.ToggleRiotUpdate | N-12 | page | `native.riot.status.calculDemarrage` | Calcul des {…} : démarrage... |
| NA-067 | `src/DraftMid.cs:870` | CompanionApp.ToggleRiotUpdate | N-13 | page | `native.riot.status.calcul` | Calcul des |
| NA-068 | `src/DraftMid.cs:883` | CompanionApp.ToggleRiotUpdate | N-13 | page | `native.riot.status.matchupsRolesDeja` | Matchups {…} Rôles {…} déjà à jour : aucune requête dépensée. |
| NA-069 | `src/DraftMid.cs:884` | CompanionApp.ToggleRiotUpdate |  | page | `native.riot.status.matchupsMisJour` | Matchups mis à jour : {…} Rôles mis à jour : |
| NA-070 | `src/DraftMid.cs:886` | CompanionApp.ToggleRiotUpdate |  | page | `native.riot.status.calculArreteParties` | Calcul arrêté : {…} parties, trop peu pour remplacer les données actuelles. |
| NA-071 (E) | `src/DraftMid.cs:939` | CompanionApp.CreateEnv | N-16 | Windows | `native.app.draftDemarrerDetail` | DRAFT n'a pas pu démarrer. Détail dans |
| NA-072 | `src/DraftMid.cs:1186` | CompanionApp.PanelMovedByUser |  | Windows | `native.app.positionManuelleConservee` | Position manuelle conservée. Réactive le positionnement automatique dans les réglages. |
| NA-073 | `src/DraftMid.cs:1207` | CompanionApp.Teach | N-11 | Windows | `native.balloon.clientDoitEtre` | Le client doit être visible à l'écran pour retenir cette vue. |
| NA-074 | `src/DraftMid.cs:1208` | CompanionApp.Teach | N-11 | Windows | `native.balloon.aucunPortraitCentre` | Aucun portrait au centre : cet écran n'a pas été retenu comme une draft. |
| NA-075 | `src/DraftMid.cs:1237` | CompanionApp.RefreshUi | N-09 | Windows | `native.menu.miseJourCours` | Mise à jour en cours... {…} Rechercher une mise à jour... |
| NA-076 | `src/DraftMid.cs:1238` | CompanionApp.RefreshUi | N-08 | Windows | `native.menu.arreterCalculCours` | Arrêter le calcul en cours {…} Mettre à jour les rôles... |
| NA-077 | `src/DraftMid.cs:1240` | CompanionApp.RefreshUi | N-02 | Windows | `native.menu.masquerAfficherPanneaux` | Masquer {…} Afficher {…} les panneaux |
| NA-078 | `src/DraftMid.cs:1350` | CompanionApp |  | Windows | `native.app.mesure` | non mesuré |
| NA-079 | `src/DraftMid.cs:1543` | CompanionApp |  | Windows | `native.app.mesure2` | non mesuré |
| NA-080 | `src/DraftMid.cs:1804` | CompanionApp.ImportStats |  | Windows | `native.dialog.choisirDraftStats` | Choisir draft-stats.js {…} Statistiques DRAFT\|draft-stats.js\|Tous les fichiers\|*.* |
| NA-081 (S,E) | `src/DraftMid.cs:1809` | CompanionApp.ImportStats | N-17 | Windows | `native.dialog.fichierImportDraft` | Ce fichier n'est pas un import DRAFT valide : rien n'a été modifié. |
| NA-082 | `src/DraftMid.cs:1913` | CompanionApp.UpdateBlocked |  | page | `native.update.draftPartieCours` | Une draft ou une partie est en cours : relance la mise à jour après. |
| NA-083 | `src/DraftMid.cs:1914` | CompanionApp.UpdateBlocked |  | page | `native.update.importCalculCours` | Un import ou un calcul est en cours : attends sa fin. |
| NA-084 | `src/DraftMid.cs:1920` | CompanionApp.CheckForUpdate | N-15 | page | `native.update.versionDisponibleDraft` | La version {…} est disponible (tu as la {…} ). DRAFT va la télécharger, la vérifier, puis redémarrer. Tes profils, ton historique et tes rég… |
| NA-085 | `src/DraftMid.cs:1948` | CompanionApp.RunUpdate | N-14 | page | `native.update.rechercheMiseJour` | Recherche d'une mise à jour... |
| NA-086 | `src/DraftMid.cs:1950` | CompanionApp.RunUpdate |  | page | `native.update.draftJourVersion` | DRAFT est à jour (version |
| NA-087 | `src/DraftMid.cs:1951` | CompanionApp.RunUpdate | N-15 | page | `native.update.versionDisponible` | La version {…} est disponible. |
| NA-088 | `src/DraftMid.cs:1956` | CompanionApp.RunUpdate | N-14 | page | `native.update.telechargement` | Téléchargement : |
| NA-089 (S) | `src/DraftMid.cs:1958` | CompanionApp.RunUpdate | N-14 | page | `native.update.verificationNouvelleVersion` | Vérification de la nouvelle version... |
| NA-090 (E) | `src/DraftMid.cs:1990` | CompanionApp.AnnounceRolledBack | N-14 | page | `native.update.nouvelleVersionDemarre` | La nouvelle version n'a pas démarré : la précédente a été remise en place. |
| NA-091 | `src/DraftMid.cs:1997` | CompanionApp.AnnounceUpdated | N-14 | page | `native.update.miseJourInstallee` | Mise à jour installée : version |
| NA-092 | `src/DraftMid.cs:2553` | RuntimeHelp.Explain | N-18 | Windows | `native.app.draftBesoinComposant` | DRAFT a besoin du composant gratuit Microsoft Edge WebView2, absent de ce PC. Ouvrir la page de Microsoft pour l'installer ? Choisis « Everg… |
| NA-093 | `src/DraftMid.cs:2651` | Installer.Install | N-19 | Windows | `native.install.draftInstalleRetrouves` | DRAFT est installé. Tu le retrouves dans le menu Démarrer. |
| NA-094 | `src/DraftMid.cs:2658` | Installer.Uninstall | N-19 | Windows | `native.install.desinstallerDraftJournal` | Désinstaller DRAFT ? Ton journal et tes préférences sont conservés. |
| NA-095 (S,E) | `src/DraftMid.cs:3010` | Program.Run | N-16 | Windows | `native.app.draftPeutEcrire` | DRAFT ne peut pas écrire dans ce dossier : {…} Vérifie ses droits, ou que le disque n'est pas plein. |
| NA-096 | `src/DraftMid.cs:3020` | Program.Run | N-20 | Windows | `native.app.prototypeIndiqueDossier` | Prototype OP.GG : indique un dossier de test avec --data. |
| NA-097 | `src/DraftMid.cs:3039` | Program.Run | N-19 | Windows | `native.app.draftDejaOuvert` | DRAFT est déjà ouvert : utilise son icône près de l'horloge. |
| NA-098 (S,E) | `src/HistoryImporter.cs:163` | HistoryImporter.Get |  | Windows | `native.history.cleRiotRefusee` | Clé Riot refusée ou expirée. Crée une nouvelle clé, puis relance : les parties déjà lues sont conservées. |
| NA-099 | `src/HistoryImporter.cs:167` | HistoryImporter.Get | N-22 | Windows | `native.history.limiteRequetesRiot` | Limite de requêtes de Riot atteinte : reprise dans {…} secondes. Rien n'est perdu. |
| NA-100 (E) | `src/HistoryImporter.cs:171` | HistoryImporter.Get |  | Windows | `native.history.riotRepondMoment` | Riot ne répond pas pour le moment. Relance plus tard : les parties déjà lues sont conservées. |
| NA-101 (E) | `src/HistoryImporter.cs:252` | HistoryImporter.Run | N-22 | Windows | `native.history.compteInvalideFormat` | Compte invalide : le format attendu est Nom#TAG. |
| NA-102 (E) | `src/HistoryImporter.cs:260` | HistoryImporter.Run | N-22 | Windows | `native.history.reponseRiotIllisible` | Réponse de Riot illisible : les parties déjà lues sont conservées. |
| NA-103 (S) | `src/HistoryImporter.cs:276` | HistoryImportDialog | N-21 | Windows | `native.history.dialog.accepteCetteLecture` | J'accepte cette lecture |
| NA-104 | `src/HistoryImporter.cs:287` | HistoryImportDialog.HistoryImportDialog | N-21 | Windows | `native.history.dialog.draftImporterMon` | DRAFT : importer mon historique Riot |
| NA-105 (S) | `src/HistoryImporter.cs:304` | HistoryImportDialog.HistoryImportDialog |  | Windows | `native.history.dialog.draftLiraParties` | DRAFT lira tes parties classées solo/duo avec ta clé et les gardera sur ce PC. |
| NA-106 | `src/HistoryImporter.cs:305` | HistoryImportDialog.HistoryImportDialog |  | Windows | `native.history.dialog.litPartiesRecentes` | Lit tes parties, des plus récentes aux plus anciennes. Un import suivant ira plus loin, sans doublon. Seuls tes champions, résultats et les … |
| NA-107 | `src/HistoryImporter.cs:310` | HistoryImportDialog.HistoryImportDialog | N-07 | Windows | `native.history.dialog.importer` | Importer |
| NA-108 | `src/HistoryImporter.cs:311` | HistoryImportDialog.HistoryImportDialog |  | Windows | `native.history.dialog.annuler` | Annuler |
| NA-109 (S) | `src/HistoryImporter.cs:319` | HistoryImportDialog.HistoryImportDialog | N-23 | Windows | `native.history.dialog.comptesNomTag` | Comptes (Nom#TAG, séparés par une virgule) : {…} Clé Riot (masquée, jamais enregistrée) : {…} Région : {…} Parties à lire : |
| NA-110 | `src/LcuConnector.cs:58` | LockfileInfo.ToString |  | Windows | `native.lcu.motPasseMasque` | , mot de passe masqué |
| NA-111 (S) | `src/LcuConnector.cs:78` | LocalHttpsTransport.GetAsync |  | Windows | `native.lcu.endpointAutorise` | Endpoint non autorisé |
| NA-112 | `src/LcuConnector.cs:127` | ClientProcess.FolderOf |  | Windows | `native.lcu.reason.processusClientInaccessible` | processus du client inaccessible (Win32 |
| NA-113 (E) | `src/LcuConnector.cs:132` | ClientProcess.FolderOf |  | Windows | `native.lcu.reason.cheminClientIllisible` | chemin du client illisible (Win32 |
| NA-114 (E) | `src/LcuConnector.cs:155` | LcuFactory.CreateReal |  | Windows | `native.lcu.reason.lockfileInvalide` | lockfile invalide |
| NA-115 | `src/LcuConnector.cs:159` | LcuFactory.CreateReal |  | Windows | `native.lcu.reason.lockfileAbsent` | lockfile absent |
| NA-116 | `src/LcuConnector.cs:160` | LcuFactory.CreateReal |  | Windows | `native.lcu.reason.lockfileAbsent2` | lockfile absent |
| NA-117 (E) | `src/LcuConnector.cs:161` | LcuFactory.CreateReal |  | Windows | `native.lcu.reason.lockfileRefuse` | lockfile refusé |
| NA-118 | `src/LcuConnector.cs:162` | LcuFactory.CreateReal |  | Windows | `native.lcu.reason.lockfileVerrouille` | lockfile verrouillé |
| NA-119 | `src/LcuConnector.cs:182` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.diagnosticConnexionClient` | Diagnostic de connexion au client LoL (DRAFT |
| NA-120 (S) | `src/LcuConnector.cs:183` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.aucunMotPasse` | Aucun mot de passe, port, chemin ni nom de joueur n'apparaît ici. |
| NA-121 (S) | `src/LcuConnector.cs:184` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.draftLanceAdministrateur` | DRAFT lancé en administrateur : |
| NA-122 | `src/LcuConnector.cs:185` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.lectureClientDesactivee` | Lecture du client : désactivée dans les réglages. Rien d'autre n'est vérifié. {…} Conclusion : coche « Lire la draft depuis le client LoL » … |
| NA-123 (E) | `src/LcuConnector.cs:189` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.fenetreClientIntrouvable` | Fenêtre du client : introuvable. {…} Conclusion : ouvre le client LoL, attends qu'il soit complètement chargé, puis relance ce diagnostic. |
| NA-124 | `src/LcuConnector.cs:190` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.fenetreClientTrouvee` | Fenêtre du client : trouvée. |
| NA-125 (S) | `src/LcuConnector.cs:193` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.accesCompletProcessus` | Accès complet au processus du client : {…} non : le client tourne probablement avec plus de droits que DRAFT (lancé en administrateur) |
| NA-126 (S,E) | `src/LcuConnector.cs:196` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.dossierClientIntrouvable` | Dossier du client : introuvable ( {…} Conclusion : lance DRAFT et le client LoL avec les mêmes droits (tous deux normalement, ou tous deux e… |
| NA-127 | `src/LcuConnector.cs:197` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.dossierClientTrouve` | Dossier du client : trouvé. |
| NA-128 | `src/LcuConnector.cs:200` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.conclusionClientDemarre` | Conclusion : le client démarre encore, ou ce n'est pas le client de League of Legends. Réessaie une fois l'accueil du client affiché. |
| NA-129 (S,E) | `src/LcuConnector.cs:203` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.lockfilePresentIllisible` | Lockfile : présent mais illisible ( {…} Conclusion : un antivirus ou des droits Windows bloquent la lecture. |
| NA-130 (E) | `src/LcuConnector.cs:204` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.lockfileFormatInattendu` | Lockfile : format inattendu. {…} Conclusion : format du client inconnu de DRAFT. Envoie ce diagnostic. |
| NA-131 | `src/LcuConnector.cs:205` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.lockfileLisibleFormat` | Lockfile : lisible, format valide. |
| NA-132 | `src/LcuConnector.cs:209` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.proxyWindowsAdresse` | Proxy Windows pour l'adresse locale : {…} UTILISÉ (peut empêcher la connexion) {…} non utilisé |
| NA-133 (E) | `src/LcuConnector.cs:217` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.reponseRefusee` | pas de réponse en 2 s {…} refusée |
| NA-134 | `src/LcuConnector.cs:221` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.connexionLocaleClient` | Connexion locale au client : |
| NA-135 | `src/LcuConnector.cs:228` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.lecturePhaseDelai` | Lecture de la phase (délai {…} ms) : réussie en {…} Conclusion : la connexion fonctionne maintenant. Si le message revient, envoie ce diagno… |
| NA-136 (E) | `src/LcuConnector.cs:229` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.lecturePhaseDelai2` | Lecture de la phase (délai {…} ms) : échec ( {…} réponse vide ou 404 {…} ) après |
| NA-137 (E) | `src/LcuConnector.cs:231` | LcuDiagnostic.RunAsync |  | Windows | `native.lcuDiag.conclusionProxyWindows` | Conclusion : le proxy Windows est le suspect principal. {…} Conclusion : le client refuse ou ne comprend pas la requête (voir l'erreur ci-de… |
| NA-138 | `src/RoleRatesUpdater.cs:77` | RiotRegion |  | Windows | `native.riot.region.europeOuestEuw` | Europe Ouest (EUW) |
| NA-139 | `src/RoleRatesUpdater.cs:78` | RiotRegion |  | Windows | `native.riot.region.europeNordEune` | Europe Nord et Est (EUNE) |
| NA-140 | `src/RoleRatesUpdater.cs:79` | RiotRegion |  | Windows | `native.riot.region.turquie` | Turquie (TR) |
| NA-141 | `src/RoleRatesUpdater.cs:80` | RiotRegion |  | Windows | `native.riot.region.ameriqueNord` | Amérique du Nord (NA) |
| NA-142 | `src/RoleRatesUpdater.cs:81` | RiotRegion |  | Windows | `native.riot.region.coree` | Corée (KR) |
| NA-143 | `src/RoleRatesUpdater.cs:141` | RiotCrawler.LoadPlayers |  | Windows | `native.riot.listeJoueurs` | liste de joueurs |
| NA-144 | `src/RoleRatesUpdater.cs:150` | RiotCrawler.LoadPlayers |  | Windows | `native.riot.aucunJoueurFormat` | Aucun joueur lu : le format de réponse de Riot a peut-être changé. |
| NA-145 (E) | `src/RoleRatesUpdater.cs:252` | RiotCrawler.Run | N-22 | Windows | `native.riot.reponseRiotIllisible` | Réponse de Riot illisible. |
| NA-146 (E) | `src/RoleRatesUpdater.cs:261` | RiotCrawler.Run | N-24 | Windows | `native.riot.ecritureFichierImpossible` | Écriture du fichier impossible. |
| NA-147 (S,E) | `src/RoleRatesUpdater.cs:275` | RiotCrawler.Get |  | Windows | `native.riot.cleRiotRefusee` | Clé Riot refusée ou expirée (code |
| NA-148 (E) | `src/RoleRatesUpdater.cs:277` | RiotCrawler.Get |  | Windows | `native.riot.riotRefuseRequetes` | Riot a refusé {…} requêtes de suite ( |
| NA-149 | `src/RoleRatesUpdater.cs:439` | RoleRatesUpdater.Crawl |  | Windows | `native.riot.historiqueJoueur` | historique d'un joueur |
| NA-150 | `src/RoleRatesUpdater.cs:446` | RoleRatesUpdater.Crawl |  | Windows | `native.riot.detailPartie` | détail d'une partie |
| NA-151 | `src/RoleRatesUpdater.cs:636` | MatchupStatsUpdater.ReadHistory |  | Windows | `native.riot.historiqueJoueur2` | historique d'un joueur |
| NA-152 | `src/RoleRatesUpdater.cs:643` | MatchupStatsUpdater.ReadHistory |  | Windows | `native.riot.detailPartie2` | détail d'une partie |
| NA-153 (S) | `src/RoleRatesUpdater.cs:705` | RiotKeyDialog.RiotKeyDialog | N-23 | Windows | `native.riot.dialog.cleRiotMasquee` | Clé Riot (masquée, jamais enregistrée) : |
| NA-154 | `src/RoleRatesUpdater.cs:707` | RiotKeyDialog.RiotKeyDialog |  | Windows | `native.riot.dialog.region` | Région : |
| NA-155 | `src/RoleRatesUpdater.cs:710` | RiotKeyDialog.RiotKeyDialog |  | Windows | `native.riot.dialog.lancer` | Lancer |
| NA-156 | `src/RoleRatesUpdater.cs:711` | RiotKeyDialog.RiotKeyDialog |  | Windows | `native.riot.dialog.annuler` | Annuler |

S : sécurité, E : erreur. « page » : le texte est envoyé à la page (statut d'import, de calcul, de mise à jour, phase, placement) et doit donc exister dans les deux langues du même catalogue.

## Page (catalogue de la PR #9)

Ligne : ligne actuelle retrouvée dans le code (le catalogue date de `78519fb`, DRAFT 2.16.1).

| ID | Ligne | Catégorie | Clé proposée | Gabarit |
|---|---|---|---|---|
| UI-001 | `index.html:697 (non retrouvée)` | erreurs | `error.startup.draftPeutDemarrer` | DRAFT ne peut pas démarrer : le dossier draft-vendor ou le fichier draft-engine.js est introuvable. Replace-le… |
| UI-002 | `index.html:744` | navigation | `stats.personal.assezParties` | re:pas assez de parties |
| UI-003 | `index.html:746` | navigation | `stats.personal.preference` | re:pas de préférence |
| UI-004 | `index.html:747 (non retrouvée)` | navigation | `stats.personal.avantageLegerAvantage` | {x} avantage \| re:léger (dés)?avantage |
| UI-005 | `index.html:754` | navigation | `stats.personal.aucunePartie` | aucune partie sur {C} |
| UI-006 | `index.html:758` | navigation | `stats.personal.bilanJamaisContre` | {bilan}, jamais contre {C} |
| UI-007 | `index.html:759` | navigation | `stats.personal.bilanContreBilan` | {bilan}, contre {C} {bilan} : {mot} |
| UI-008 | `index.html:735,1838 (non retrouvée)` | navigation | `stats.record.bilanFmtrecordCartes` | re:^\d+V \d+D(, \d+ %)?$ |
| UI-009 | `index.html:823` | conseil/moteur | `advice.change.prisFace` | {C} est pris en face |
| UI-010 | `index.html:819` | conseil/moteur | `advice.change.menaitDejaPeu` | Il menait déjà de peu : le conseil change maintenant que la draft est complète. |
| UI-011 | `index.html:1256` | conseil/moteur | `advice.change.passeDevantAvantage` | re:( passe devant \|^avantage sur « .+ »$\|^(Draft complète\|Ajout de .+\|Retrait de .+\|.+ remplacé par .+\|.… |
| UI-012 | `index.html:1056` | navigation | `draft.notice.ajout` | Ajout de {C} |
| UI-013 | `index.html:1070` | navigation | `draft.notice.retrait` | Retrait de {C} |
| UI-014 | `index.html:1007` | navigation | `draft.notice.remplace` | {C} remplacé par {C} |
| UI-015 | `index.html:1076 (non retrouvée)` | navigation | `draft.notice.topJungleMid` | re:^.+ (au top\|à la jungle\|au mid\|à l'ADC\|au support)$ |
| UI-016 | `index.html:1176` | rôles | `draft.notice.topJungleMid2` | re:^.+ : (Top\|Jungle\|Mid\|ADC\|Support)$ |
| UI-017 | `index.html:993` | navigation | `draft.notice.selectionCoursTerminee` | re:^Sélection (en cours\|terminée)$ |
| UI-018 | `index.html:1252` | rôles | `draft.search.prochainChampionRole` | Prochain champion : {Rôle} |
| UI-019 | `index.html:1255` | navigation | `draft.search.ajouterChampionAdverse` | Ajouter un champion adverse |
| UI-020 | `index.html:1256` | navigation | `draft.search.draftComplete` | Draft complète |
| UI-021 | `index.html:1256` | navigation | `draft.search.ajouterAdversaireAhri` | Ajouter un adversaire : ahri, tf, kata... |
| UI-022 | `index.html:1264` | navigation | `draft.search.aucunChampionCorrespond` | Aucun champion ne correspond à « {q} ». |
| UI-023 | `index.html:1282` | sécurité | `advice.help.noteChoixJournal` | Note ce choix dans ton journal. Rien n'est envoyé au client LoL : tu verrouilles toi-même ton champion. |
| UI-024 | `index.html:1283` | conseil/moteur | `advice.help.deuxChampionsOnt` | Deux de tes champions ont des scores proches : les deux se défendent. |
| UI-025 | `index.html:1284` | rôles | `enemy.slot.clientDonneRoles` | Le client ne donne pas les rôles adverses : cet adversaire est déduit, et se corrige dans la composition. |
| UI-026 | `index.html:1286` | rôles | `enemy.slot.topJungleMid` | re:^(Top\|Jungle\|Mid\|ADC\|Support) très probable( \|$) |
| UI-027 | `index.html:1286` | rôles | `enemy.slot.topJungleMid2` | re:^(Top\|Jungle\|Mid\|ADC\|Support) probable( \|$) |
| UI-028 | `index.html:1216` | rôles | `enemy.slot.topJungleMid3` | re:^(Top\|Jungle\|Mid\|ADC\|Support) non identifié( (Tank\|Bruiser\|Carry\|Plongeur\|Contrôle\|Engage\|Soutien… |
| UI-029 | `index.html:1294` | rôles | `enemy.slot.topJungleMid4` | re:^(Top\|Jungle\|Mid\|ADC\|Support) pas encore visible\.$ |
| UI-030 | `index.html:1295` | rôles | `enemy.slot.topJungleMid5` | re:^(Top\|Jungle\|Mid\|ADC\|Support) non identifié : |
| UI-031 | `index.html:1296` | rôles | `enemy.slot.topJungleMid6` | re:(Top\|Jungle\|Mid\|ADC\|Support) supposé : |
| UI-032 | `index.html:1304` | rôles | `enemy.slot.ligneAdversaireMidswapeffect` | Si c'est {C} : {C}. |
| UI-033 | `index.html:1318` | rôles | `enemy.slot.topJungleMid7` | re:^(Top\|Jungle\|Mid\|ADC\|Support) : .+\. (Échanger avec .+\|Sélectionner pour changer de rôle)$ |
| UI-034 | `index.html:1319` | rôles | `enemy.slot.topJungleMid8` | re:^(Top\|Jungle\|Mid\|ADC\|Support) vide\. (Y placer .+\|Choisir ce rôle pour le prochain champion)$ |
| UI-035 | `index.html:1312 (non retrouvée)` | rôles | `enemy.slot.placerTopJungle` | re:^Placer .+ (au top\|à la jungle\|au mid\|à l'ADC\|au support)$ |
| UI-036 | `index.html:1333` | rôles | `enemy.slot.retirer` | re:^Retirer [^ ].*$ |
| UI-037 | `index.html:1335` | rôles | `enemy.slot.tapeNom` | Tape son nom |
| UI-038 | `index.html:1335` | rôles | `enemy.slot.ajouterAdcTop` | re:^(Ajouter )?(l'ADC\|le (Top\|Jungle\|Mid\|Support))$ |
| UI-039 | `index.html:1216` | rôles | `enemy.profile.profilInconnuSoutien` | re:^(Profil inconnu\|Soutien\|(Tank\|Bruiser\|Carry\|Plongeur\|Contrôle\|Engage)(, (Tank\|Bruiser\|Carry\|Plon… |
| UI-040 | `index.html:1384` | rôles | `enemy.profile.frontlineCarrysFragiles` | re:^(Frontline\|Carrys fragiles\|Plongeurs\|Contrôles ciblés\|Engage) : \d sur 5$ |
| UI-041 | `index.html:1384` | rôles | `enemy.profile.frontlineCarrysFragiles2` | re:^(Frontline\|Carrys fragiles\|Plongeurs\|Contrôles ciblés\|Engage)$ |
| UI-042 | `index.html:1356` | rôles | `enemy.profile.profilRemplitChaque` | Le profil se remplit à chaque adversaire ajouté. |
| UI-043 | `index.html:1360` | rôles | `enemy.profile.forcesListe` | Leurs forces : {liste} |
| UI-044 | `index.html:1361` | rôles | `enemy.profile.faillesListe` | Leurs failles : {liste} |
| UI-045 | `index.html:1362` | rôles | `enemy.profile.profilMarqueInstant` | Pas de profil marqué pour l'instant. |
| UI-046 | `index.html:1370` | rôles | `enemy.profile.compositionAdverse` | re:^(Composition adverse( \d sur 5)?\|\d sur 5)$ |
| UI-047 | `index.html:1373` | rôles | `enemy.profile.selectionneCliqueAutre` | {C} sélectionné : clique sur un autre rôle. |
| UI-048 | `index.html:1373` | rôles | `enemy.profile.entreeAjouteEchap` | Entrée ajoute, Échap efface. |
| UI-049 | `index.html:1379` | rôles | `enemy.profile.profilEquipe` | Profil de l'équipe |
| UI-050 | `index.html:1414` | conseil/moteur | `advice.art.illustrationIndisponibleVerifie` | Illustration indisponible : vérifie ta connexion. |
| UI-051 | `index.html:1420` | conseil/moteur | `advice.tie.egaliteEntreListe` | Égalité entre {liste} |
| UI-052 | `index.html:1400 (non retrouvée)` | conseil/moteur | `advice.tie.conseil` | re:^Conseil : .+ |
| UI-053 | `index.html:1425` | conseil/moteur | `advice.tie.championsEgalite` | Champions à égalité |
| UI-054 | `index.html:1426` | conseil/moteur | `advice.tie.picksEgaliteScore` | re:^\d+ picks à égalité de score( sur cette draft)?$ |
| UI-055 | `index.html:1432` | conseil/moteur | `advice.tie.favori` | Ton favori |
| UI-056 | `index.html:1417, draft-coaching.js tieCauseText (non retrouvée)` | conseil/moteur | `advice.tie.passePremierPire` | re:(passe en premier : son pire critère pèse moins\|passe en premier : aucun critère ne le pénalise\|passe en … |
| UI-057 | `index.html:1464` | conseil/moteur | `advice.coach.conseil` | Conseil |
| UI-058 | `index.html:1487` | conseil/moteur | `advice.coach.voirConseils` | Voir les conseils |
| UI-059 | `index.html:1491` | conseil/moteur | `coachTab.nombrePartiesImportees` | Nombre de parties importées dans ce rôle. Cela ne dit pas si tu joues bien ce champion. |
| UI-060 | `index.html:1495` | conseil/moteur | `coachTab.joueParties` | re:^Le plus joué( .+ · \d+ parties)?$ |
| UI-061 | `index.html:1507` | conseil/moteur | `coachTab.niveauConseilsDebutant` | re:^(Niveau des conseils\|Débutant\|Intermédiaire\|Avancé)$ |
| UI-062 | `index.html:1515` | conseil/moteur | `coachTab.aucunConseilDisponible` | Aucun conseil disponible pour l'instant. |
| UI-063 | `index.html:1522` | conseil/moteur | `coachTab.choix` | Ton choix |
| UI-064 | `index.html:1524` | conseil/moteur | `coachTab.joueRole` | Le plus joué dans ce rôle |
| UI-065 | `index.html:1524` | conseil/moteur | `coachTab.donneesInsuffisantes` | Données insuffisantes |
| UI-066 | `index.html:1527` | conseil/moteur | `coachTab.experienceRole` | Expérience dans ce rôle |
| UI-067 | `index.html:1528` | conseil/moteur | `coachTab.partiesDontMois` | re:^.+ \d+ parties(, dont \d+ ce mois-ci\|, dernière il y a \d+ jours)?$ |
| UI-068 | `index.html:1529` | conseil/moteur | `coachTab.aucunePartieImportee` | Aucune partie importée sur {liste}. |
| UI-069 | `index.html:1530` | conseil/moteur | `coachTab.historiqueImporteParties` | Historique importé{x} parties classées solo/duo. Les autres modes et les parties non lues ne sont pas comptés. |
| UI-070 | `index.html:1530` | conseil/moteur | `coachTab.aucunHistoriqueImporte` | Aucun historique importé : tu peux le faire dans les réglages. |
| UI-071 | `index.html:1530` | conseil/moteur | `coachTab.nombrePartiesDit` | Le nombre de parties ne dit pas si tu joues bien un champion, et il ne change pas le classement. |
| UI-072 | `index.html:1531` | conseil/moteur | `coachTab.conseilsIssusDonnee` | Conseils issus d'une donnée ou d'un calcul |
| UI-073 | `index.html:1532` | conseil/moteur | `coachTab.confirmer` | À confirmer |
| UI-074 | `index.html:1533` | conseil/moteur | `coachTab.avisEcritsMain` | Avis écrits à la main, pas encore validés par un joueur. Garde ton jugement. |
| UI-075 | `index.html:1535` | conseil/moteur | `coachTab.resteIncertain` | Ce qui reste incertain |
| UI-076 | `index.html:1542` | conseil/moteur | `advice.card.recommandation` | Recommandation |
| UI-077 | `index.html:1543` | conseil/moteur | `advice.card.aucunPick` | Aucun pick |
| UI-078 | `index.html:1554` | conseil/moteur | `advice.card.egalite` | Égalité |
| UI-079 | `index.html:1554` | conseil/moteur | `advice.card.adversaires` | re:^\d/5 adversaires$ |
| UI-080 | `index.html:1556` | conseil/moteur | `advice.card.sinon` | re:^(Sinon\|Ou)( .+\|[A-Z].*)?$ |
| UI-081 | `index.html:1571` | conseil/moteur | `advice.card.choixNet` | Choix net |
| UI-082 | `index.html:1571` | conseil/moteur | `advice.card.choixSerre` | Choix serré |
| UI-083 | `index.html:1579` | conseil/moteur | `advice.card.aucunArgumentParticulier` | Aucun argument particulier dans cette draft |
| UI-084 | `index.html:1582` | conseil/moteur | `advice.card.danger` | re:^Danger( .+)?$ |
| UI-085 | `index.html:1587` | conseil/moteur | `advice.card.details` | Plus de détails |
| UI-086 | `index.html:1588` | conseil/moteur | `advice.card.eviterListe` | À éviter : {liste} |
| UI-087 | `index.html:1589` | conseil/moteur | `advice.card.parties` | Tes parties : {x} |
| UI-088 | `index.html:1590` | conseil/moteur | `advice.card.frontline` | Sans frontline : {C} |
| UI-089 | `index.html:1574,2077 (non retrouvée)` | conseil/moteur | `advice.card.joueNote` | re:^(Je joue .+\|.+ noté)$ |
| UI-090 | `index.html:1601` | conseil/moteur | `pool.egalite` | À égalité |
| UI-091 | `index.html:1588` | conseil/moteur | `pool.eviter` | À éviter |
| UI-092 | `index.html:1603` | conseil/moteur | `pool.recommande` | Recommandé |
| UI-093 | `index.html:1603` | conseil/moteur | `pool.proche` | Très proche |
| UI-094 | `index.html:1604` | conseil/moteur | `pool.alternative` | re:^Alternative |
| UI-095 | `index.html:1606` | conseil/moteur | `pool.moinsAdapte` | Moins adapté |
| UI-096 | `index.html:1606` | conseil/moteur | `pool.possible` | Possible |
| UI-097 | `index.html:1618` | conseil/moteur | `pool.poolMeilleurs` | re:^(Ton pool\|Les \d+ meilleurs sur \d+) |
| UI-098 | `index.html:1618` | conseil/moteur | `pool.cliqueChampionJoues` | Clique sur le champion que tu joues |
| UI-099 | `index.html:1629` | conseil/moteur | `pool.championChoisiChoisi` | re:^(Champion choisi\|Choisi)$ |
| UI-100 | `index.html:1924` | navigation | `journal.result.victoireDefaiteIgnorer` | re:^(Victoire\|Défaite\|Ignorer\|Ignorée\|En attente)$ |
| UI-101 | `index.html:1668` | conseil/moteur | `plan.planJeu` | re:^(Plan de jeu( avec .+)?\|avec .+)$ |
| UI-102 | `index.html:1671` | conseil/moteur | `plan.gagne` | re:^Gagne si |
| UI-103 | `index.html:1671` | conseil/moteur | `plan.choisisChampion` | re:Choisis un champion\.$ |
| UI-104 | `index.html:1673` | conseil/moteur | `plan.menaces` | re:^Menaces$ |
| UI-105 | `index.html:1673` | conseil/moteur | `plan.rienParticulier` | Rien de particulier |
| UI-106 | `index.html:1674` | conseil/moteur | `plan.cibles` | re:^Cibles$ |
| UI-107 | `index.html:1674` | conseil/moteur | `plan.carryExpose` | Le carry le plus exposé |
| UI-108 | `index.html:1654 (non retrouvée)` | conseil/moteur | `plan.bans` | re:^Bans$ |
| UI-109 | `index.html:1675` | conseil/moteur | `plan.aucun` | Aucun |
| UI-110 | `index.html:1676` | conseil/moteur | `plan.dernierePartie` | Dernière partie : {C} |
| UI-111 | `index.html:1681` | conseil/moteur | `plan.laneMidGame` | re:^(Lane\|Mid game\|Teamfights)$ |
| UI-112 | `index.html:1687` | conseil/moteur | `plan.choisisChampionsEgalite` | Choisis un des champions à égalité pour voir son plan. |
| UI-113 | `index.html:1687` | conseil/moteur | `plan.aucunPickDisponible` | Aucun pick disponible. |
| UI-114 | `index.html:1691` | conseil/moteur | `plan.phaseJeu` | Phase de jeu |
| UI-115 | `index.html:1697` | conseil/moteur | `plan.avisGeneralBase` | re:^Avis général basé sur (les \d champions adverses visibles\|1 champion adverse visible\|aucun champion adve… |
| UI-116 | `index.html:1702` | conseil/moteur | `analysis.avantageDesavantageNeutre` | re:: (avantage\|désavantage\|neutre)$ |
| UI-117 | `index.html:1708` | conseil/moteur | `analysis.confianceFaibleMoyenne` | re:Confiance (faible\|moyenne\|élevée)\. |
| UI-118 | `index.html:1709` | conseil/moteur | `analysis.classementPondereEntre` | Classement pondéré entre ces hypothèses, avec les mêmes règles. |
| UI-119 | `index.html:1710` | conseil/moteur | `analysis.topJungleMid` | re:(Top\|Jungle\|Mid\|ADC\|Support) inconnu : le matchup direct n'est pas évalué\. |
| UI-120 | `index.html:1711` | conseil/moteur | `analysis.donneesMatchupContre` | Pas de données de matchup contre {C} : ce critère n'est pas évalué. |
| UI-121 | `index.html:1712` | conseil/moteur | `analysis.ontMemeScore` | re:ont le même score\. |
| UI-122 | `index.html:1713` | conseil/moteur | `analysis.resteAfficheMene` | reste affiché : {C} mène de trop peu pour changer de nom en cours de draft. |
| UI-123 | `index.html:1714` | conseil/moteur | `analysis.draftIncompleteAdversaire` | Draft incomplète : {N} adversaire(s) sur 5. |
| UI-124 | `index.html:1715` | conseil/moteur | `analysis.profilProvisoireListe` | Profil provisoire : {liste}. |
| UI-125 | `index.html:1716` | conseil/moteur | `analysis.frontlineAllieeSupposee` | re:(Pas de f\|F)rontline alliée supposée \(réglage\)\. |
| UI-126 | `index.html:1719` | conseil/moteur | `analysis.critereCriteresChampion` | re:^(Critère\|Critères par champion)$ |
| UI-127 | `index.html:1726` | conseil/moteur | `analysis.concerne` | Non concerné |
| UI-128 | `index.html:1741` | conseil/moteur | `analysis.aucunCritereDistingue` | Aucun critère ne le distingue dans cette draft. |
| UI-129 | `index.html:1742` | conseil/moteur | `analysis.menacesListe` | Menaces : {liste} |
| UI-130 | `index.html:1743` | conseil/moteur | `analysis.opportunitesListe` | Opportunités : {liste} |
| UI-131 | `index.html:1744` | conseil/moteur | `analysis.conditionVictoire` | Condition de victoire : {x} |
| UI-132 | `index.html:1745` | conseil/moteur | `analysis.verifierMatchupLolalytics` | Vérifier ce matchup sur LoLalytics |
| UI-133 | `index.html:1748` | conseil/moteur | `analysis.bansConseilles` | Bans conseillés |
| UI-134 | `index.html:1758` | conseil/moteur | `compare.premierSecond` | pour le premier, {x} pour le second |
| UI-135 | `index.html:1765` | conseil/moteur | `compare.fautMoinsDeux` | Il faut au moins deux picks disponibles pour comparer. |
| UI-136 | `index.html:1776` | conseil/moteur | `compare.premierSecondChampion` | re:^(Premier\|Second) champion$ |
| UI-137 | `index.html:1761-1762 (non retrouvée)` | conseil/moteur | `compare.contre` | re:^(VS\|.+ contre .+)$ |
| UI-138 | `index.html:1786` | conseil/moteur | `compare.egaliteTotalCriteres` | À égalité sur le total des critères. |
| UI-139 | `index.html:1786` | conseil/moteur | `compare.serreLegerAvantage` | Serré, léger avantage à {C}. |
| UI-140 | `index.html:1786` | conseil/moteur | `compare.avantageNet` | Avantage net à {C}. |
| UI-141 | `index.html:1786` | conseil/moteur | `compare.gagneSurtoutCritere` | gagne surtout sur « {critère} » |
| UI-142 | `index.html:1779` | conseil/moteur | `compare.moinsPenaliseCritere` | Moins pénalisé : {critère} |
| UI-143 | `index.html:1788` | conseil/moteur | `compare.aucunCritereSepare` | Aucun critère ne les sépare pour l'instant. |
| UI-144 | `index.html:1799` | conseil/moteur | `compare.passeraitDevant` | {C} passerait devant |
| UI-145 | `index.html:1800` | conseil/moteur | `compare.aucunCasSimples` | re:(Dans aucun des cas simples testés : autre vis-à-vis, absence de frontline ou nouvel adversaire type\.\|Pas… |
| UI-146 | `index.html:1785 (non retrouvée)` | conseil/moteur | `duel.encoreBilansProfil` | Pas encore de bilans pour ce profil : le conseil repose sur la composition adverse et les fiches de champions.… |
| UI-147 | `index.html:1812` | conseil/moteur | `duel.regardeBilansContre` | Regarde tes bilans contre un adversaire, sans toucher à ta draft. |
| UI-148 | `index.html:1813` | conseil/moteur | `duel.chercherAdversaireAdversaire` | re:^(Chercher un adversaire\|Adversaire : ahri, tf, kata\.\.\.)$ |
| UI-149 | `index.html:1820` | conseil/moteur | `duel.bilans` | re:^Bilans : |
| UI-150 | `index.html:1820` | conseil/moteur | `duel.moinsPartiesBilan` | Moins de {N} parties : pas de bilan. |
| UI-151 | `index.html:1827` | conseil/moteur | `duel.regardeQuoiPrendre` | Regarde quoi prendre contre un mid, sans toucher à ta draft. |
| UI-152 | `index.html:1828` | conseil/moteur | `duel.chercherMidAdverse` | re:^(Chercher un mid adverse\|Mid adverse : ahri, tf, kata\.\.\.)$ |
| UI-153 | `index.html:1830` | conseil/moteur | `duel.tableau` | re:^Pas dans le tableau\. |
| UI-154 | `index.html:1830` | conseil/moteur | `duel.ajouteChampionDraft` | Ajoute ce champion à ta draft pour voir le classement. |
| UI-155 | `index.html:1838` | conseil/moteur | `duel.prendsSinon` | Prends {C}, sinon {C} |
| UI-156 | `index.html:1839` | conseil/moteur | `duel.eviter` | re:^À éviter : |
| UI-157 | `index.html:1842` | conseil/moteur | `duel.avisEcritMain` | Avis écrit à la main, pas encore de bilan. |
| UI-158 | `index.html:1865` | navigation | `stats.pourcentageMoinsParties` | Sans pourcentage : moins de {N} parties, trop peu pour conclure. |
| UI-159 | `index.html:1883` | navigation | `stats.encoreStatistiquesLance` | re:(Pas encore de statistiques\. Lance draft-import-stats\.bat, placé dans le même dossier que cette page, pui… |
| UI-160 | `index.html:1885` | navigation | `stats.cesBilansDecrivent` | Ces bilans décrivent le passé et ne changent pas le verdict. |
| UI-161 | `index.html:1890` | navigation | `stats.victoiresChampionSelon` | re:^(Victoires par champion, .+\|Selon le type de mid adverse\|Évolution par tranche de \d+ parties\|Total\|Mo… |
| UI-162 | `index.html:1906` | navigation | `journal.journalVideClique` | Ton journal est vide : clique sur « Je joue » pendant une draft pour commencer. |
| UI-163 | `index.html:1916` | navigation | `journal.dernieresPartiesVictoires` | re:^(Dernières parties\|Victoires par champion joué\|Compositions récentes)$ |
| UI-164 | `index.html:1913` | navigation | `journal.victoiresPartiesNotees` | re:^(\d+ victoires sur \d+ parties notées\|.+ : (victoire\|défaite))$ |
| UI-165 | `index.html:1920` | navigation | `journal.conseilSuiviAutre` | Conseil suivi : {x}. Autre choix : {x}. Ces chiffres ne prouvent pas que l'outil aide : tu ne choisis pas au h… |
| UI-166 | `index.html:1921` | navigation | `journal.datePickMid` | re:^(Date\|Pick\|Mid\|Conseil\|Résultat)$ |
| UI-167 | `index.html:1925` | navigation | `journal.telechargerJournal` | Télécharger le journal |
| UI-168 | `index.html:1930` | conseil/moteur | `rules.commentPicksSont` | Comment tes picks sont classés |
| UI-169 | `index.html:1931` | conseil/moteur | `rules.chaqueChampionFiche` | Chaque champion a une fiche écrite à la main : chasseur de cibles fragiles, tueur de tanks, immobile, protecte… |
| UI-170 | `index.html:1935` | conseil/moteur | `rules.reperesPoolResume` | re:^(Repères pour ton pool \(résumé, pas le calcul exact\)\|Situation\|Au moins 2 tanks ou bruisers, et tu as … |
| UI-171 | `index.html:1938` | conseil/moteur | `rules.quandEviterChaque` | Quand éviter chaque pick |
| UI-172 | `index.html:1940` | conseil/moteur | `rules.equipeFrontlineEquipe` | ton équipe n'a pas de frontline, ou l'équipe adverse a au moins 2 assassins ou plongeurs. |
| UI-173 | `index.html:1941` | conseil/moteur | `rules.aucuneCibleFragile` | aucune cible fragile accessible, ou beaucoup de contrôles ciblés en face. |
| UI-174 | `index.html:1942` | erreurs | `rules.plusieursChampionsMobiles` | plusieurs champions très mobiles en face, et ton équipe ne peut pas te protéger. |
| UI-175 | `index.html:1943` | conseil/moteur | `rules.rarementMauvaisMoins` | rarement mauvais, mais moins utile si tes mates ne suivent jamais tes ultimes. |
| UI-176 | `index.html:1945` | conseil/moteur | `rules.criteresPoidsMoteur` | Critères et poids du moteur |
| UI-177 | `index.html:1946` | conseil/moteur | `rules.chaqueCritereDonne` | Chaque critère donne une note de -2 à +2, multipliée par son poids. Le total sert seulement à comparer tes pic… |
| UI-178 | `index.html:1947` | conseil/moteur | `rules.poidsVient` | re:^(Poids\|D'où ça vient)$ |
| UI-179 | `index.html:1950` | conseil/moteur | `rules.provenanceDonnees` | Provenance des données |
| UI-180 | `index.html:1952` | conseil/moteur | `rules.nomsPortraitsIllustrations` | noms, portraits, illustrations, numéro de patch, classes de Locke et Zaahen. |
| UI-181 | `index.html:1953` | conseil/moteur | `rules.rolesBilansMatchups` | rôles et bilans de matchups, si tu les as calculés avec l'API Riot. Le lien LoLalytics ouvre simplement leur p… |
| UI-182 | `index.html:1954` | conseil/moteur | `rules.matchupsAssezParties` | matchups sans assez de parties, profils de champions, règles, plan de jeu. |
| UI-183 | `index.html:1955` | conseil/moteur | `rules.journalImportRiot` | ton journal et ton import Riot. Affichés, mais sans effet sur le classement. |
| UI-184 | `index.html:1953` | conseil/moteur | `rules.officielRiotData` | re:^(Officiel \(Riot, Data Dragon\) :\|Statistiques observées :\|Heuristique \(écrit à la main\) :\|Personnel … |
| UI-185 | `index.html:1957` | conseil/moteur | `rules.fiabilite` | Fiabilité |
| UI-186 | `index.html:1958` | conseil/moteur | `rules.outilCompteFrontline` | re:(L'outil compte sur une frontline dans ton équipe \(réglage\)\|Réglage actuel : pas de frontline alliée sup… |
| UI-187 | `index.html:1958` | conseil/moteur | `rules.banConseilleVient` | Le ban conseillé vient de tes bilans calculés s'il y en a, sinon du tableau de matchups (mid), sinon des fiche… |
| UI-188 | `index.html:1959` | sécurité | `legal.outilPersonnelApprouve` | Outil personnel non approuvé par Riot Games. Les portraits et illustrations appartiennent à Riot Games. |
| UI-189 | `index.html:1970` | navigation | `nav.analysisView.criteresComparerRegles` | re:^(Critères\|Comparer\|Règles\|Vue de l'analyse)$ |
| UI-190 | `index.html:1983` | navigation | `nav.tab.planAnalyseConseils` | re:^(Plan\|Analyse\|Conseils\|Duel\|Parties\|Détails et statistiques)$ |
| UI-191 | `index.html:1997` | navigation | `nav.mobile.egaliteListe` | Égalité : {liste} |
| UI-192 | `index.html:2000` | navigation | `nav.mobile.enregistrePrends` | re:^(Enregistré\|Je prends)$ |
| UI-193 | `index.html:2013` | conseil/moteur | `advice.empty.choisisRoleChampions` | Choisis ton rôle et tes champions dans les réglages. |
| UI-194 | `index.html:2014` | conseil/moteur | `advice.empty.aucunChampionRole` | Aucun champion pour le rôle {Rôle}. Ajoute-les dans les réglages, ou change de rôle. |
| UI-195 | `index.html:2015` | conseil/moteur | `advice.empty.tousPicksSont` | Tous tes picks sont exclus ou déjà pris en face. Change tes exclusions dans les réglages. |
| UI-196 | `index.html:2022` | rôles | `role.switch.roleActifRole` | re:^(Rôle actif\|Rôle \?)$ |
| UI-197 | `index.html:2026` | rôles | `role.switch.topJungleMid` | re:^(Top\|Jungle\|Mid\|ADC\|Support)( : \d+ champions?\| : aucun champion)( \(attribué par LoL\))?$ |
| UI-198 | `index.html:2027` | rôles | `role.switch.passerProfilTop` | re:^Passer au profil (Top\|Jungle\|Mid\|ADC\|Support)$ |
| UI-199 | `index.html:2029` | rôles | `role.switch.lolAttribueTop` | re:^LoL t'attribue (Top\|Jungle\|Mid\|ADC\|Support)\. |
| UI-200 | `index.html:2030` | rôles | `role.switch.passerTopJungle` | re:^(Passer à (Top\|Jungle\|Mid\|ADC\|Support)\|Ignorer)$ |
| UI-201 | `index.html:2037` | navigation | `brand.assistantStrategiqueLeague` | Ton assistant stratégique pour League of Legends |
| UI-202 | `index.html:2040` | navigation | `nav.panels.deuxPanneauxPanneau` | re:^(Deux panneaux\|Panneau unique\|Passer à deux panneaux\|Passer au panneau unique\|Ouvrir les réglages\|Mas… |
| UI-203 | `index.html:2053` | conseil/moteur | `panel.choisirMonProfil` | Choisir mon profil |
| UI-204 | `index.html:2069` | conseil/moteur | `panel.championConseille` | Champion conseillé |
| UI-205 | `index.html:2073` | conseil/moteur | `panel.recommandePresqueEgalite` | re:^(Recommandé\|Presque à égalité)$ |
| UI-206 | `index.html:2078` | conseil/moteur | `panel.comparerCesDeux` | Comparer ces deux champions |
| UI-207 | `index.html:2073` | conseil/moteur | `panel.presqueEgaliteDevant` | re:^(Presque à égalité\|Devant de peu) |
| UI-208 | `index.html:2084` | conseil/moteur | `panel.pourquoiChoix` | Pourquoi ce choix ? |
| UI-209 | `index.html:2086` | conseil/moteur | `panel.raisonsDifferentCompare` | Leurs raisons diffèrent : compare-les dans Analyse. |
| UI-210 | `index.html:2086` | conseil/moteur | `panel.aucunArgumentMarque` | Aucun argument marqué pour l'instant : ajoute des adversaires. |
| UI-211 | `index.html:2088` | conseil/moteur | `panel.dangerPrincipal` | Danger principal |
| UI-212 | `index.html:2090` | conseil/moteur | `panel.danger` | Danger : {C} |
| UI-213 | `index.html:2090` | conseil/moteur | `panel.communCesChampions` | Commun à ces champions. |
| UI-214 | `index.html:2095` | conseil/moteur | `panel.planJeuBan` | re:^(Plan de jeu et ban\|Ban conseillé)$ |
| UI-215 | `index.html:2071 (non retrouvée)` | conseil/moteur | `panel.panneauDroitAutres` | re:^ou .+ |
| UI-216 | `index.html:2072 (non retrouvée)` | conseil/moteur | `panel.mauvaisBilanGene` | re:^.+ (mauvais bilan de\|gêne) .+\.$ |
| UI-217 | `index.html:2100` | conseil/moteur | `panel.choisisChampionVoir` | Choisis un champion pour voir son plan. |
| UI-218 | `index.html:898` | conseil/moteur | `panel.analyseAnalyseDetaillee` | re:^(Analyse\|Analyse détaillée\|détaillée)$ |
| UI-219 | `index.html:2114` | réglages | `settings.lcu.connecteClientLol` | Connecté au client LoL |
| UI-220 | `index.html:2115` | réglages | `settings.lcu.attenteLanceClient` | En attente : lance le client LoL pour vérifier |
| UI-221 | `index.html:2116` | erreurs | `settings.lcu.clientOuvertConnexion` | Client ouvert, mais connexion impossible |
| UI-222 | `index.html:2117` | réglages | `settings.lcu.desactiveeDetectionEstimee` | Désactivée : la détection est estimée et peu fiable |
| UI-223 | `index.html:2148` | sécurité | `settings.profiles.profilsExportesRoles` | Profils exportés : rôles, champions et préférences, sans historique ni clé. |
| UI-224 | `index.html:2156` | sécurité | `settings.profiles.rienEteModifie` | re:Rien n'a été modifié\.$ |
| UI-225 | `index.html:2166` | réglages | `settings.profiles.profilsImportesEtat` | Profils importés.{x}L'état précédent est sauvegardé. |
| UI-226 | `index.html:2168` | sécurité | `settings.profiles.fichierIllisibleRien` | Fichier illisible : rien n'a été modifié. |
| UI-227 | `index.html:2174` | réglages | `settings.profiles.profilsPrecedentsRestaures` | Profils précédents restaurés. |
| UI-228 | `index.html:2179` | réglages | `settings.profiles.exporterMesProfils` | re:^(Exporter mes profils\|Importer\|Importer des profils\|Annuler le dernier import)$ |
| UI-229 | `index.html:2196` | réglages | `settings.mesProfils` | Mes profils |
| UI-230 | `index.html:2198` | réglages | `settings.choisisRoleChampions` | Choisis ton rôle et tes champions pour obtenir un conseil. |
| UI-231 | `index.html:2202` | réglages | `settings.monRoleMes` | re:^(Mon rôle\|Mes champions)$ |
| UI-232 | `index.html:2204` | réglages | `settings.aucunChampionInstant` | Aucun champion pour l'instant. |
| UI-233 | `index.html:2206` | réglages | `settings.ajouterChampionAjouter` | re:^(Ajouter un champion\|Ajouter un champion : ahri, tf, kata\.\.\.)$ |
| UI-234 | `index.html:2210` | réglages | `settings.aucunChampionTrouve` | Aucun champion trouvé. |
| UI-235 | `index.html:2212` | réglages | `settings.aucunChampionRole` | Aucun champion pour ce rôle : ajoute-en pour obtenir un conseil. |
| UI-236 | `index.html:2213` | réglages | `settings.chaqueRoleGarde` | Chaque rôle garde son propre pool. Ils sont enregistrés automatiquement. |
| UI-237 | `index.html:2215` | réglages | `settings.statistiquesCalculeesConseil` | Sans statistiques calculées, le conseil repose sur des fiches écrites à la main : c'est un avis général. |
| UI-238 | `index.html:2229` | réglages | `settings.sourcesDonnees` | re:^Sources de données |
| UI-239 | `index.html:2231` | réglages | `settings.ancienneAbsente` | re:^(Ancienne\|Absente)$ |
| UI-240 | `index.html:2232` | réglages | `settings.patchObservationsIncertitude` | re:(Patch : \|Observations : \|Incertitude (élevée\|moyenne)\.) |
| UI-241 | `index.html:2235` | réglages | `settings.rolesSurveillerSouvent` | re:(Rôles à surveiller\|plus souvent (Top\|Jungle\|Mid\|ADC\|Support) que dans) |
| UI-242 | `index.html:2252` | réglages | `settings.partieAjouteeJournal` | {N} partie(s) ajoutée(s) à ton journal. |
| UI-243 | `index.html:2254` | sécurité | `settings.fichierJournalDraft` | Ce fichier n'est pas un journal DRAFT : rien n'a été modifié. |
| UI-244 | `index.html:2264` | réglages | `settings.reglagesFermerReglages` | re:^(Réglages\|Fermer\|Réglages Fermer)$ |
| UI-245 | `index.html:2265` | sécurité | `settings.betaAssistantStrategique` | bêta : ton assistant stratégique pour League of Legends. Non approuvé par Riot Games. |
| UI-246 | `index.html:2271` | réglages | `settings.miseJourMise` | re:^(Mise à jour\|Mise à jour en cours\.\.\.\|Rechercher une mise à jour\|Version .+\. DRAFT ne contacte GitHu… |
| UI-247 | `index.html:2277` | sécurité | `settings.monHistoriqueRiot` | re:(Mon historique Riot\|Rien d'importé pour l'instant\.\|Sert à afficher ton expérience, jamais à changer le … |
| UI-248 | `index.html:2286` | réglages | `settings.detectionDraftLire` | re:^(Détection de la draft\|Lire la draft depuis le client LoL)$ |
| UI-249 | `index.html:2287` | réglages | `settings.recommandePanneauxOuvrent` | Recommandé : les panneaux s'ouvrent au bon moment et les adversaires se remplissent seuls. Lecture seule, rien… |
| UI-250 | `index.html:2289` | sécurité | `settings.riotConfirmeAutorise` | Riot n'a pas confirmé que c'est autorisé : tu l'actives à tes risques. |
| UI-251 | `index.html:2290` | réglages | `settings.quandLolAttribue` | re:^(Quand LoL t'attribue un autre rôle\|Me proposer de changer\|Changer de profil\|Ne rien faire)$ |
| UI-252 | `index.html:2294` | réglages | `settings.seulementLectureClient` | Seulement si la lecture du client est active et que LoL indique ton poste. Rien n'est déduit de ton champion. |
| UI-253 | `index.html:2296` | réglages | `settings.afficherPanneauxAutomatiquement` | re:^(Afficher les panneaux automatiquement pendant la draft\|Fenêtres\|Coller les panneaux au client LoL\|Un s… |
| UI-254 | `index.html:2297` | réglages | `settings.afficheMasquePanneaux` | affiche ou masque les panneaux à tout moment. |
| UI-255 | `index.html:2305` | réglages | `settings.mesPicksExclure` | re:^(Mes picks\|Exclure pour l'instant :)$ |
| UI-256 | `index.html:2307` | réglages | `settings.egaliteExactePreferer` | À égalité exacte, préférer |
| UI-257 | `index.html:2313` | réglages | `settings.supposerFrontlineMon` | Supposer un frontline dans mon équipe |
| UI-258 | `index.html:2317` | réglages | `settings.rolesChampionsIls` | re:(Rôles des champions\|Ils servent à deviner le mid adverse\.) |
| UI-259 | `index.html:2317` | réglages | `settings.encoreCalculesRoles` | Pas encore calculés : les rôles habituels écrits à la main servent de repère. |
| UI-260 | `index.html:2319` | réglages | `settings.matchupsSousParties` | re:(^Matchups$\|Sous \d+ parties, le matchup n'utilise pas le bilan\.\|relance le calcul pour ton rôle actuel\… |
| UI-261 | `index.html:2325` | réglages | `settings.arreterCalculMettre` | re:^(Arrêter le calcul\|Mettre à jour les rôles\|Mettre à jour les matchups)$ |
| UI-262 | `index.html:2329` | réglages | `settings.journalRecupererAncien` | re:^(Journal\|Pour récupérer un ancien journal, importe son fichier ici\. Rien n'est supprimé\.\|Importer un j… |
| UI-263 | `index.html:2336` | réglages | `settings.avanceJournalDiagnostic` | re:^(Avancé\|Journal de diagnostic de la détection \(chiffres seulement\))$ |
| UI-264 | `index.html:2437` | navigation | `nav.top.patchDraftAleatoire` | re:^(Patch \d+\.\d+\|Draft aléatoire\|Nouvelle draft)$ |
| UI-265 | `index.html:788` | rôles | `role.name.topJungleMid` | re:^(Top\|Jungle\|Mid\|ADC\|Support)( \d+)?$ |
| UI-266 | `index.html:2037` | navigation | `brand.draft` | DRAFT |
| UI-267 | `index.html:1835 (non retrouvée)` | navigation | `stats.barreVictoires` | re:^.+ : \d+V \d+D$ |
| UI-269 | `index.html:1866,1869 (non retrouvée)` | navigation | `stats.statistiquesNomCompte` | re:^[^ ]+#[^ ]+$ |
| UI-268 | `index.html:2311 (non retrouvée)` | réglages | `settings.advanced.clientLolFerme` | re:^(Client LoL fermé\|Client ouvert, hors draft \(estimation\)\|Draft en cours \(estimation\)\|Partie en cour… |
| UI-269-bis | `index.html:2440` | navigation | `draft.simulator.simulationDraftCours` | re:^(Simulation : ta draft en cours n'est pas modifiée\.( Revenir à la draft en cours)?\|Revenir à la draft en… |
| EN-001 | `draft-engine.js:123` | conseil/moteur | `engine.libellesCriteres.comparerRegles.matchupDirectSecurite` | re:^(Matchup direct\|Sécurité sans adversaire connu\|Cibles exploitables\|Tanks et bruisers\|Survie aux plonge… |
| EN-002 | `draft-engine.js:122` | conseil/moteur | `engine.criteria.source.sourceCritere.bilansCalculesVraies` | Bilans calculés sur de vraies parties dès 30 parties, sinon tableau écrit à la main. Inclut la pression de lan… |
| EN-003 | `draft-engine.js:123` | conseil/moteur | `engine.criteria.source.sourceBlind.calculePartMids` | Calculé : part des mids du tableau où le pick est à éviter. Ne compte que si le mid est inconnu. |
| EN-004 | `draft-engine.js:124` | conseil/moteur | `engine.criteria.source.sourceTargets.regleHeuristiqueNombre` | Règle heuristique : nombre de carrys fragiles adverses. |
| EN-005 | `draft-engine.js:125` | conseil/moteur | `engine.criteria.source.sourceFront.regleHeuristiqueNombre` | Règle heuristique : nombre de frontlines adverses. |
| EN-006 | `draft-engine.js:126` | conseil/moteur | `engine.criteria.source.sourceDive.regleHeuristiqueNombre` | Règle heuristique : nombre d'assassins et de plongeurs adverses. |
| EN-007 | `draft-engine.js:127` | conseil/moteur | `engine.criteria.source.sourceControl.regleHeuristiqueControles` | Règle heuristique : contrôles ciblés et engages adverses, comptés une seule fois par pick. |
| EN-008 | `draft-engine.js:128` | conseil/moteur | `engine.criteria.source.sourceRange.regleHeuristiqueNombre` | Règle heuristique : nombre de champions à longue portée adverses. |
| EN-009 | `draft-engine.js:129` | conseil/moteur | `engine.criteria.source.sourcePeel.regleHeuristiqueFrontline` | Règle heuristique : frontline allié supposé, mobilité adverse. |
| EN-010 | `draft-engine.js:130` | conseil/moteur | `engine.criteria.source.sourceMap.atoutPropreChampion` | Atout propre au champion. Poids faible, car il ne dépend pas de la draft. |
| EN-011 | `draft-engine.js:201` | conseil/moteur | `engine.reason.bilanCalcule.contrePartiesEcart` | re:(% contre .+ sur \d+ parties\|Sans écart net contre .+ sur \d+ parties) |
| EN-012 | `draft-engine.js:205` | conseil/moteur | `engine.reason.tableau.eviterContre` | À éviter contre {C} |
| EN-013 | `draft-engine.js:206` | conseil/moteur | `engine.reason.tableau.choixContre` | 1er choix contre {C} |
| EN-014 | `draft-engine.js:207` | conseil/moteur | `engine.reason.tableau.choixContre2` | 2e choix contre {C} |
| EN-015 | `draft-engine.js:208` | conseil/moteur | `engine.reason.tableauNote.avisMarqueContre` | Pas d'avis marqué contre {C} |
| EN-016 | `draft-engine.js:216` | conseil/moteur | `engine.reason.positive.midInconnuPeu` | Mid inconnu : peu de mauvais matchups ({N} sur {N} dans le tableau) |
| EN-017 | `draft-engine.js:216` | conseil/moteur | `engine.reason.negative.midInconnuRisque` | Mid inconnu : risqué, mauvais dans {N} matchups sur {N} du tableau |
| EN-018 | `draft-engine.js:221` | conseil/moteur | `engine.reason.ciblesFragiles` | re:\d cibles fragiles : |
| EN-019 | `draft-engine.js:222` | conseil/moteur | `engine.reason.cibleFragile` | Une cible fragile : {C} |
| EN-020 | `draft-engine.js:223` | conseil/moteur | `engine.reason.aucune.aucuneCibleFragile` | Aucune cible fragile à éliminer |
| EN-021 | `draft-engine.js:225` | conseil/moteur | `engine.reason.ciblesFragilesAttraper` | cibles fragiles à attraper : {liste} |
| EN-022 | `draft-engine.js:226` | conseil/moteur | `engine.reason.ciblesAbimerLoin` | cibles à abîmer de loin : {liste} |
| EN-023 | `draft-engine.js:232` | conseil/moteur | `engine.reason.tanksBruisersFont` | tanks ou bruisers : {x} font la différence |
| EN-024 | `draft-engine.js:233` | conseil/moteur | `engine.reason.tanksBruisersSont` | 2 tanks ou bruisers : {x} sont utiles |
| EN-025 | `draft-engine.js:236` | conseil/moteur | `engine.reason.frontlinesTraverser` | re:\d frontlines à traverser |
| EN-026 | `draft-engine.js:237` | conseil/moteur | `engine.reason.beaucoupFrontlineTraverser` | Beaucoup de frontline à traverser |
| EN-027 | `draft-engine.js:239` | conseil/moteur | `engine.reason.frontlinesPeuPrise` | frontlines : peu de prise sur les tanks |
| EN-028 | `draft-engine.js:245` | conseil/moteur | `engine.reason.plongeursContreChampion` | plongeurs contre un champion immobile : {liste} |
| EN-029 | `draft-engine.js:246` | conseil/moteur | `engine.reason.encaissePlongeurs` | Encaisse leurs {N} plongeurs |
| EN-030 | `draft-engine.js:247` | conseil/moteur | `engine.reason.peutProtegerAllies` | Peut protéger ses alliés de leurs {N} plongeurs |
| EN-031 | `draft-engine.js:248` | conseil/moteur | `engine.reason.noteMatrice.luiLaisseSortie` | re:^(Son E lui laisse une sortie contre les plongeurs\|Son étourdissement l'aide à se défendre contre les plon… |
| EN-032 | `draft-engine.js:251` | conseil/moteur | `engine.reason.controlesCiblesPunissent` | contrôles ciblés punissent ses plongeons : {liste} |
| EN-033 | `draft-engine.js:252` | conseil/moteur | `engine.reason.peutJouerLoin` | re:Peut jouer loin de leur engage (et revenir avec son ultime\|et de leurs contrôles) |
| EN-034 | `draft-engine.js:253` | conseil/moteur | `engine.reason.engagesContreChampion` | engages contre un champion lent : {liste} |
| EN-035 | `draft-engine.js:258` | conseil/moteur | `engine.reason.peutRendreCoups` | Peut rendre les coups à distance : {liste} |
| EN-036 | `draft-engine.js:259` | conseil/moteur | `engine.reason.faitToucherAvant` | Se fait toucher avant de pouvoir frapper : {liste} |
| EN-037 | `draft-engine.js:263` | conseil/moteur | `engine.reason.noteMatrice.supposeFrontlineAllie` | Suppose un frontline allié pour le protéger |
| EN-038 | `draft-engine.js:263` | conseil/moteur | `engine.reason.frontline.equipeFrontlineProteger` | Ton équipe n'a pas de frontline pour le protéger |
| EN-039 | `draft-engine.js:265` | conseil/moteur | `engine.reason.frontline.personneProtegerMobilite` | Personne pour te protéger de leur mobilité |
| EN-040 | `draft-engine.js:266` | conseil/moteur | `engine.reason.championsMobilesListe` | champions très mobiles : {liste} |
| EN-041 | `draft-engine.js:267` | conseil/moteur | `engine.reason.positive.peuPlongeursMobilite` | Peu de plongeurs et de mobilité, et un frontline devant toi |
| EN-042 | `draft-engine.js:271` | conseil/moteur | `engine.reason.ultimeCouvreToute` | re:(Son ultime couvre toute la carte\|Peut peser sur toute la carte) |
| EN-043 | `draft-engine.js:272` | conseil/moteur | `engine.reason.peutRoamerFois` | Peut roamer une fois sa vague poussée |
| EN-044 | `draft-engine.js:273` | conseil/moteur | `engine.reason.peuImpactHors` | Peu d'impact hors des combats groupés |
| EN-045 | `draft-engine.js:295` | conseil/moteur | `engine.reason.chacuneNommee.encoreChoisi` | re:^(?:[+−] )?(Si c'est .+\|S'il n'est pas encore choisi) : |
| EN-046 | `draft-engine.js:428` | conseil/moteur | `engine.ban reason.bilans.mauvaisBilanListe` | mauvais bilan de {liste} sur de vraies parties |
| EN-047 | `draft-engine.js:431` | conseil/moteur | `engine.ban reason.tableau.geneListeTableau` | gêne {liste} dans le tableau de matchups |
| EN-048 | `draft-engine.js:433` | conseil/moteur | `engine.ban reason.fiches.geneListeApres` | gêne {liste} d'après leurs profils |
| EN-049 | `draft-engine.js:466` | conseil/moteur | `engine.stylePicks.assassinMeleeEntre` | re:^(Assassin de mêlée : entre, tue une cible et ressort\.\|Mage de carte : pousse, contrôle une cible et rejo… |
| EN-050 | `draft-engine.js:477` | conseil/moteur | `engine.stylePick.protecteurGardeAllies` | re:^(Protecteur : garde ses alliés en vie\.\|Tank d'engage : lance les combats et encaisse\.\|Tank : encaisse … |
| EN-051 | `draft-engine.js:509` | conseil/moteur | `engine.whenBetter.frontline.equipeFrontline` | si ton équipe n'a pas de frontline |
| EN-052 | `draft-engine.js:510` | conseil/moteur | `engine.whenBetter.autreAdversaire.adversaireTopJungle` | re:si ton adversaire (au top\|à la jungle\|au mid\|à l'ADC\|au support) est |
| EN-053 | `draft-engine.js:496` | conseil/moteur | `engine.whenBetter.adversaireType.tankPlongeurControle` | re:avec un (tank\|plongeur\|contrôle ciblé\|carry fragile\|poke) de plus |
| EN-054 | `draft-engine.js:534` | conseil/moteur | `engine.position.position.seulPickDisponible` | Seul pick disponible. |
| EN-055 | `draft-engine.js:536` | conseil/moteur | `engine.position.position.passeDevantSurtout` | 1er : passe devant {C} surtout grâce au critère « {critère} ». |
| EN-056 | `draft-engine.js:536` | conseil/moteur | `engine.position.positionEgalite.egalite` | 1er, à égalité avec {C}. |
| EN-057 | `draft-engine.js:539` | conseil/moteur | `engine.position.position.derriereSurtoutCause` | Derrière {C} surtout à cause du critère « {critère} ». |
| EN-058 | `draft-engine.js:513 (non retrouvée)` | conseil/moteur | `engine.position.positionEgalite.egalite2` | re:^À égalité avec .+\.$ |
| EN-059 | `draft-engine.js:550` | conseil/moteur | `engine.motsComporeading.frontlineEngageControles` | re:(frontline\|engage\|contrôles ciblés\|plongeurs\|poke\|plusieurs cibles fragiles\|pas de frontline\|peu d'e… |
| EN-060 | `draft-engine.js:568` | conseil/moteur | `engine.danger.controleCiblePeut` | Son contrôle ciblé peut stopper ton plongeon. |
| EN-061 | `draft-engine.js:569` | conseil/moteur | `engine.danger.peutRattraperQuand` | Peut te rattraper quand tu ressors d'un combat. |
| EN-062 | `draft-engine.js:569` | conseil/moteur | `engine.danger.peutSauterDessus` | Peut te sauter dessus si tu es mal placé. |
| EN-063 | `draft-engine.js:570` | conseil/moteur | `engine.danger.mobiliteLuiPermet` | Sa mobilité lui permet d'éviter tes sorts à distance. |
| EN-064 | `draft-engine.js:571` | conseil/moteur | `engine.danger.peutDemarrerCombat` | Peut démarrer le combat sur toi. |
| EN-065 | `draft-engine.js:572` | conseil/moteur | `engine.danger.surveillerPendantPartie` | À surveiller pendant la partie. |
| EN-066 | `draft-engine.js:578` | erreurs | `engine.winCondition.protegeMeilleurCarry` | Protège ton meilleur carry, et fais échouer les engages sur lui. |
| EN-067 | `draft-engine.js:579` | conseil/moteur | `engine.winCondition.lanceCombatEquipe` | Lance le combat sur {C} dès que ton équipe peut suivre. |
| EN-068 | `draft-engine.js:580` | conseil/moteur | `engine.winCondition.encaissePremiereLigne` | Encaisse en première ligne, et prends l'espace autour des objectifs. |
| EN-069 | `draft-engine.js:581` | conseil/moteur | `engine.winCondition.prendsAvantagePuis` | Prends ton avantage, puis élimine {C} avant l'objectif. |
| EN-070 | `draft-engine.js:582` | conseil/moteur | `engine.winCondition.faisFondreReste` | re:(Fais fondre .+, et reste en vie jusqu'à la fin du combat\.\|Frappe la cible la plus proche, et reste en vi… |
| EN-071 | `draft-engine.js:583` | conseil/moteur | `engine.winCondition.affaiblisDistanceAvant` | re:Affaiblis-les à distance avant l'objectif, (sans te faire attraper\|loin de leurs plongeurs)\. |
| EN-072 | `draft-engine.js:584` | conseil/moteur | `engine.winCondition.prendsAvantageLane` | Prends ton avantage en lane, puis joue chaque objectif avec ton équipe. |
| EN-073 | `draft-engine.js:591` | conseil/moteur | `engine.winCondition.pousseVagueRoam` | Pousse ta vague, roam, et tue {C} avant l'objectif. |
| EN-074 | `draft-engine.js:592` | conseil/moteur | `engine.winCondition.pousseCarteBleue` | Pousse à la carte bleue, puis crée un surnombre avec ton ultime. |
| EN-075 | `draft-engine.js:593` | conseil/moteur | `engine.winCondition.resteDerriereFrontline` | re:Reste derrière ton frontline et (fais fondre .+\|frappe) autour des objectifs\. |
| EN-076 | `draft-engine.js:613` | conseil/moteur | `engine.plan.adversairePlongeur.peutSauterDessus` | peut te sauter dessus : garde tes distances et ton sort de défense. |
| EN-077 | `draft-engine.js:614` | conseil/moteur | `engine.plan.adversaireControle.controleCibleAttends` | a un contrôle ciblé : attends de le voir partir avant d'engager. |
| EN-078 | `draft-engine.js:615` | conseil/moteur | `engine.plan.contreObserveSorts` | Contre {C}, observe ses sorts importants avant d'engager un échange. |
| EN-079 | `draft-engine.js:616` | conseil/moteur | `engine.plan.matchupDifficile.matchupDifficileFarme` | est un matchup difficile : farme prudemment et n'échange qu'après avoir vu partir un de ses sorts importants. |
| EN-080 | `draft-engine.js:617` | conseil/moteur | `engine.plan.controleCibleAttends` | a un contrôle ciblé : attends de le voir partir avant de lancer ton all-in. |
| EN-081 | `draft-engine.js:618` | conseil/moteur | `engine.plan.contreChercheEchanges` | Contre {C}, cherche des échanges courts, puis ressors avec ton E. |
| EN-082 | `draft-engine.js:619` | conseil/moteur | `engine.plan.pickImmobile.peutSauterDessus` | peut te sauter dessus : garde tes distances et ne gaspille pas ton sort de contrôle ou de ralentissement. |
| EN-083 | `draft-engine.js:620` | conseil/moteur | `engine.plan.toucheLoinVient` | Touche {C} de loin dès qu'il vient au farm, sans dépasser la moitié de la lane. |
| EN-084 | `draft-engine.js:621` | conseil/moteur | `engine.plan.contreFarmePortee` | Contre {C}, farme à portée maximale et échange seulement quand ta vague te couvre. |
| EN-085 | `draft-engine.js:622` | conseil/moteur | `engine.plan.contreGardeCarte` | Contre {C}, garde ta carte dorée pour répondre à un engage ou préparer un gank. |
| EN-086 | `draft-engine.js:623` | conseil/moteur | `engine.plan.jungler.peutVenirGank` | peut venir en gank : place une ward avant de pousser ta vague. |
| EN-087 | `draft-engine.js:624` | conseil/moteur | `engine.plan.adversaireInconnu.adversaireTopJungle` | re:^Ton adversaire (au top\|à la jungle\|au mid\|à l'ADC\|au support) n'est pas connu : joue d'abord la sécuri… |
| EN-088 | `draft-engine.js:625` | conseil/moteur | `engine.plan.prepareCheminVers` | Prépare ton chemin vers la lane qui peut le mieux profiter de ton aide. |
| EN-089 | `draft-engine.js:625` | conseil/moteur | `engine.plan.gardeVisionCote` | Garde la vision de ton côté avant de chercher un engage. |
| EN-090 | `draft-engine.js:625` | conseil/moteur | `engine.plan.midAdc.pousseVagueAvant` | Pousse ta vague avant de bouger, pour ne pas perdre de farm. |
| EN-091 | `draft-engine.js:628` | conseil/moteur | `engine.plan.utiliseUltimeLane` | Utilise ton ultime dès qu'une lane peut punir {C}. |
| EN-092 | `draft-engine.js:629` | conseil/moteur | `engine.plan.chercheIsoleCote` | Cherche {C} isolé sur un côté, puis disparais avant la riposte. |
| EN-093 | `draft-engine.js:630` | conseil/moteur | `engine.plan.resteGroupeFrontline` | Reste groupé avec ton frontline : seul sur un côté, tu n'as presque aucune échappatoire. |
| EN-094 | `draft-engine.js:631` | conseil/moteur | `engine.plan.affaiblisAvantChaque` | Affaiblis {C} avant chaque objectif, depuis un endroit couvert par une ward. |
| EN-095 | `draft-engine.js:632` | conseil/moteur | `engine.plan.plongeur.repereCarteAvant` | Repère {C} sur la carte avant de te placer : sans visuel, recule. |
| EN-096 | `draft-engine.js:633` | conseil/moteur | `engine.plan.engage.poseWardControle` | Pose une ward de contrôle sur le chemin de {C} avant les objectifs. |
| EN-097 | `draft-engine.js:634` | conseil/moteur | `engine.plan.tank.perdsTempsTour` | Ne perds pas de temps sur {C} si une tour ou un objectif est disponible. |
| EN-098 | `draft-engine.js:635` | conseil/moteur | `engine.plan.defaut.apresCombatGagne` | Après un combat gagné, prends tout de suite une tour ou un objectif. |
| EN-099 | `draft-engine.js:638` | conseil/moteur | `engine.plan.entreSeulementApres` | Entre seulement après avoir vu partir le contrôle de {C}. |
| EN-100 | `draft-engine.js:639` | conseil/moteur | `engine.plan.ciblePrioritaireFrontline` | Ta cible prioritaire est {C}, pas le frontline. |
| EN-101 | `draft-engine.js:640` | conseil/moteur | `engine.plan.cibleFragileAccessible` | Sans cible fragile accessible, ne plonge pas : va chercher un objectif ailleurs. |
| EN-102 | `draft-engine.js:641` | conseil/moteur | `engine.plan.gardeCarteDoree` | Garde ta carte dorée pour {C} ou pour le premier qui plonge. |
| EN-103 | `draft-engine.js:642` | conseil/moteur | `engine.plan.temporiseTantEngage` | Temporise tant que {C} n'a pas engagé, puis rejoins le combat. |
| EN-104 | `draft-engine.js:643` | conseil/moteur | `engine.plan.pickImmobile.gardeToiSortie` | Garde-toi une sortie contre {C} : ne te place jamais en première ligne. |
| EN-105 | `draft-engine.js:644` | conseil/moteur | `engine.plan.frappeProcheTraverse` | Frappe {C} s'il est le plus proche : ne traverse pas le combat pour une cible lointaine. |
| EN-106 | `draft-engine.js:645` | conseil/moteur | `engine.plan.lanceSortsDepuis` | Lance tes sorts sur {C} depuis l'arrière, et recule dès qu'un adversaire s'approche. |
| EN-107 | `draft-engine.js:646` | conseil/moteur | `engine.plan.engage.eviteCombatsVision` | Évite les combats sans vision : {C} peut démarrer l'engage sur toi. |
| EN-108 | `draft-engine.js:647` | conseil/moteur | `engine.plan.defaut.eviteOuvrirCombat` | Évite d'ouvrir un combat quand plusieurs adversaires sont invisibles sur la carte. |
| EN-109 | `draft-engine.js:35` | conseil/moteur | `engine.typesMid.journal.mageAssassinCombattant` | re:^(Mage\|Assassin\|Combattant\|Tank\|Support)$ |
| CO-001 | `draft-coaching.js:11` | conseil/moteur | `coach.provenanceConseil.donneeOfficielleRiot` | re:^(Donnée officielle de Riot\|Mesuré sur de vraies parties\|Mesuré sur peu de parties\|Estimation\|Règle écr… |
| CO-002 | `draft-coaching.js:137` | conseil/moteur | `coach.draft.tie.title.egaliteListe` | Égalité : {liste} |
| CO-003 | `draft-coaching.js:138` | conseil/moteur | `coach.draft.tie.level1.classementDepartage` | Le classement ne les départage pas. Choisis celui que tu connais le mieux. |
| CO-004 | `draft-coaching.js:139` | erreurs | `coach.draft.tie.level2.fichesSont` | re:^Leurs fiches sont identiques : l'outil ne peut pas les distinguer dans cette draft\. |
| CO-005 | `draft-coaching.js:140` | conseil/moteur | `coach.draft.tie.level2.memeScore` | re:^Même score pour des raisons différentes\. |
| CO-006 | `draft-coaching.js:141` | conseil/moteur | `coach.draft.tie.level3.scoreIdentique` | re:^Score identique \(-?\d+,\d\)\. |
| CO-007 | `draft-coaching.js:142` | erreurs | `coach.draft.tie.limit.reglesDistinguent` | Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu. |
| CO-008 | `draft-coaching.js:138 (non retrouvée)` | conseil/moteur | `coach.draft.why.title.pourquoi` | re:^Pourquoi .+[^?]$ |
| CO-009 | `draft-coaching.js:151` | conseil/moteur | `coach.draft.why.level1.raisonPrincipale` | La raison principale : {raison}. |
| CO-010 | `draft-coaching.js:152` | conseil/moteur | `coach.draft.why.level2.pese` | re:^« .+ » pèse le plus : |
| CO-011 | `draft-coaching.js:153` | conseil/moteur | `coach.draft.why.level3.critereLourd` | Critère le plus lourd : « {critère} » (+{N}) |
| CO-012 | `draft-coaching.js:169` | conseil/moteur | `coach.draft.close.title.choixSerre` | re:^(.+ ou .+ : le choix est serré\|Choix serré : .+ passe devant de peu)$ |
| CO-013 | `draft-coaching.js:173` | conseil/moteur | `coach.draft.close.level1.deuxValent` | re:^(Les deux se valent presque\.\|.+ passe devant de peu : ) |
| CO-014 | `draft-coaching.js:171` | conseil/moteur | `coach.draft.close.level2.meneSurtout` | re: mène surtout sur « |
| CO-015 | `draft-coaching.js:172` | conseil/moteur | `coach.draft.close.level3.ecartPoint` | re:(^Écart de \d+,\d point : sous le seuil\|mène de \d+,\d point, sous le seuil de \d utilisé pour changer le … |
| CO-016 | `draft-coaching.js:192` | conseil/moteur | `coach.draft.conflict.title.ressortReste` | re:^.+ (ressort\|reste affiché) malgré un vrai risque$ |
| CO-017 | `draft-coaching.js:189` | conseil/moteur | `coach.draft.conflict.level1.risqueReste` | re:^Le risque : .+(reste devant car le reste de la draft compense\|reste au même score que .+\|reste affiché, … |
| CO-018 | `draft-coaching.js:194` | conseil/moteur | `coach.draft.conflict.level2.scoreReste` | re:(Le score reste le meilleur, mais « \|» pèse contre ce choix : .+Vérifie que tu peux jouer avec cette contr… |
| CO-019 | `draft-coaching.js:196` | conseil/moteur | `coach.draft.conflict.level3.critereNegatif` | re:^Critère négatif « .+ » \(-?\d+,\d\), compensé par les autres\. |
| CO-020 | `draft-coaching.js:284` | conseil/moteur | `coach.draft.incomplete.title.resteAdversaires` | re:^Il reste \d adversaires? à voir$ |
| CO-021 | `draft-coaching.js:202` | conseil/moteur | `coach.draft.incomplete.level1.conseilPeut` | Ce conseil peut changer. Attends si tu peux, avant de te décider. |
| CO-022 | `draft-coaching.js:203` | conseil/moteur | `coach.draft.incomplete.level2.seulementComptes` | seulement : les comptes de frontline et de contrôles peuvent encore bouger. |
| CO-023 | `draft-coaching.js:204` | conseil/moteur | `coach.draft.incomplete.level3.criteresSeuil` | Les critères à seuil ne se déclenchent pas encore tous |
| CO-024 | `draft-coaching.js:211` | conseil/moteur | `coach.matchup.measured.title.contreBilan` | {C} contre {C} : bilan mesuré |
| CO-025 | `draft-coaching.js:212` | conseil/moteur | `coach.matchup.measured.level1.partiesJoueurs` | re:^Sur \d+ parties de joueurs habitués à .+, ce matchup se passe plutôt (mieux\|moins bien) que leur moyenne\… |
| CO-026 | `draft-coaching.js:213` | conseil/moteur | `coach.matchup.measured.level2.pointsVictoires` | points de victoires par rapport à leur moyenne sur {C}, sur {N} parties. |
| CO-027 | `draft-coaching.js:214` | conseil/moteur | `coach.matchup.measured.level3.ecartRamene` | Écart ramené vers zéro selon l'échantillon |
| CO-028 | `draft-coaching.js:215` | conseil/moteur | `coach.matchup.measured.limit.peuParties` | re:(Peu de parties : à prendre comme une tendance\.\|Ces joueurs jouent souvent ce champion : pas forcément to… |
| CO-029 | `draft-coaching.js:224` | conseil/moteur | `coach.matchup.written.title.contrePlutot` | re:^.+ contre .+ : (plutôt un bon choix\|un matchup difficile)$ |
| CO-030 | `draft-coaching.js:225` | conseil/moteur | `coach.matchup.written.level1.tableauDraft` | Le tableau de DRAFT place {C} parmi les bons choix contre {C}. |
| CO-031 | `draft-coaching.js:225` | conseil/moteur | `coach.matchup.written.level1.tableauDraft2` | Le tableau de DRAFT déconseille {C} contre {C}. Si ce choix reste devant, c'est pour le reste de la draft. |
| CO-032 | `draft-coaching.js:226` | conseil/moteur | `coach.matchup.written.level2.selonTableau` | , selon un tableau écrit à la main. C'est le critère au plus gros poids du calcul. |
| CO-033 | `draft-coaching.js:227` | conseil/moteur | `coach.matchup.written.level3.avisEcrit` | avis écrit à la main ({N} points), sans statistiques ni patch. Un bilan calculé le remplacera dès {N} parties … |
| CO-034 | `draft-coaching.js:228` | conseil/moteur | `coach.matchup.written.limit.avisEcrit` | Avis écrit à la main, pas une statistique. |
| CO-035 | `draft-coaching.js:231` | conseil/moteur | `coach.matchup.range.title.toucheLoin` | re:^(.+ te touche de plus loin\|Tu touches .+ de plus loin)$ |
| CO-036 | `draft-coaching.js:234` | conseil/moteur | `coach.matchup.range.level1.farmerDois` | Pour farmer, tu dois t'approcher et risques d'être touché. Avance quand ses sorts viennent d'être utilisés. |
| CO-037 | `draft-coaching.js:235` | conseil/moteur | `coach.matchup.range.level2.choisisEchanges` | Choisis tes échanges : engage juste après un sort important de {C}, puis ressors. |
| CO-038 | `draft-coaching.js:236` | conseil/moteur | `coach.matchup.range.level3.peseSurtout` | Il pèse surtout tôt dans la partie, et ne dit rien de la portée des sorts ni des outils pour le combler. |
| CO-039 | `draft-coaching.js:238` | conseil/moteur | `coach.matchup.range.level1.attaquesBase` | Ses attaques de base ne t'atteignent pas de loin, mais ses sorts peuvent changer ça : regarde-les avant d'avan… |
| CO-040 | `draft-coaching.js:239` | conseil/moteur | `coach.matchup.range.level2.doitApprocher` | doit s'approcher pour farmer, ce qui t'offre des occasions de frapper. Garde un œil sur ses sorts. |
| CO-041 | `draft-coaching.js:240` | conseil/moteur | `coach.matchup.range.level3.compteQuoi` | Il ne compte que si {C} n'a pas de quoi le combler : à vérifier dans ses sorts. |
| CO-042 | `draft-coaching.js:241` | conseil/moteur | `coach.matchup.range.limit.porteeAttaque` | Portée d'attaque de base seulement : les sorts ne sont pas comptés. |
| CO-043 | `draft-coaching.js:244` | conseil/moteur | `coach.matchup.tank.title.encaisseBeaucoup` | {C} encaisse beaucoup |
| CO-044 | `draft-coaching.js:245` | conseil/moteur | `coach.matchup.tank.level1.faitBons` | fait de bons dégâts aux joueurs solides. Tuer {C} prend du temps : n'en fais pas ta priorité tout de suite. |
| CO-045 | `draft-coaching.js:246` | conseil/moteur | `coach.matchup.tank.level2.tankFace` | Un tank en face tue rarement vite : joue la durée. |
| CO-046 | `draft-coaching.js:247` | conseil/moteur | `coach.matchup.tank.level3.interetGrandit` | l'intérêt de {C} grandit avec le temps. C'est une règle générale de sa fiche, pas un bilan mesuré de ce matchu… |
| CO-047 | `draft-coaching.js:248` | conseil/moteur | `coach.matchup.tank.level1.acharneRegarde` | Ne t'acharne pas : regarde s'il y a une cible plus fragile à ta portée. |
| CO-048 | `draft-coaching.js:249` | conseil/moteur | `coach.matchup.tank.level2.tuerTank` | Tuer un tank est long : garde tes sorts pour une cible plus fragile, sauf si tes alliés se concentrent sur lui… |
| CO-049 | `draft-coaching.js:250` | conseil/moteur | `coach.matchup.tank.level3.contreTank` | Contre un tank, ton impact passe souvent par les cibles fragiles et le timing. À nuancer selon ce que ton équi… |
| CO-050 | `draft-coaching.js:251` | conseil/moteur | `coach.matchup.tank.level1.acharnePlutot` | Ne t'acharne pas : va plutôt aider ailleurs quand ta vague est poussée. |
| CO-051 | `draft-coaching.js:252` | conseil/moteur | `coach.matchup.tank.level2.tuerTank2` | Tuer un tank en voie est long et rarement rentable tôt : pousse ta vague et regarde où ton aide pèserait davan… |
| CO-052 | `draft-coaching.js:253` | conseil/moteur | `coach.matchup.tank.level3.contreTank2` | Contre un tank, l'objectif de voie est souvent le tempo plutôt que le meurtre. À nuancer selon ta capacité à l… |
| CO-053 | `draft-coaching.js:254` | conseil/moteur | `coach.matchup.tank/assassin.limit.avisGeneral` | Avis général, pas un bilan de ce matchup. |
| CO-054 | `draft-coaching.js:257` | conseil/moteur | `coach.matchup.assassin.title.peutSauter` | {C} peut te sauter dessus |
| CO-055 | `draft-coaching.js:258` | conseil/moteur | `coach.matchup.assassin.level1.chercheSurprendre` | cherche à te surprendre et à te tuer vite. Garde de quoi te défendre tant que ses sorts pour s'approcher sont … |
| CO-056 | `draft-coaching.js:259` | conseil/moteur | `coach.matchup.assassin.level2.menaceDepend` | Sa menace dépend de sa capacité à arriver sur toi. Quand ses sorts de déplacement sont utilisés, c'est ta fenê… |
| CO-057 | `draft-coaching.js:260` | conseil/moteur | `coach.matchup.assassin.level3.menaceAcces` | Menace d'accès rapide : la zone dangereuse dépend de ses sorts de déplacement, pas de sa portée d'attaque. Le … |
| CO-058 | `draft-coaching.js:268` | conseil/moteur | `coach.matchup.unknown.title.topJungle` | re:^(Top\|Jungle\|Mid\|ADC\|Support) pas encore identifié : prépare les deux cas$ |
| CO-059 | `draft-coaching.js:268` | conseil/moteur | `coach.matchup.unknown.title.topJungle2` | re:^(Top\|Jungle\|Mid\|ADC\|Support) pas encore visible$ |
| CO-060 | `draft-coaching.js:270` | conseil/moteur | `coach.matchup.unknown.level1.regardeSorts` | Regarde les sorts d'invocateur et le placement au début de la partie. |
| CO-061 | `draft-coaching.js:271` | conseil/moteur | `coach.matchup.unknown.level2.conseilTient` | Le conseil tient compte des deux. |
| CO-062 | `draft-coaching.js:272` | conseil/moteur | `coach.matchup.unknown.level3.connaisVrai` | Si tu connais le vrai rôle, corrige-le : le calcul redevient exact pour ce matchup. |
| CO-063 | `draft-coaching.js:274` | conseil/moteur | `coach.matchup.unknown.level1.tantVisible` | Tant qu'il n'est pas visible, le conseil ne porte pas sur ton duel. Si tu peux, choisis après l'avoir vu. |
| CO-064 | `draft-coaching.js:275` | conseil/moteur | `coach.matchup.unknown.level2.critereMatchup` | Le critère de matchup ne s'applique pas encore : le conseil repose sur le reste de la composition et sur la sé… |
| CO-065 | `draft-coaching.js:276` | conseil/moteur | `coach.matchup.unknown.level3.candidatPlausible` | Sans candidat plausible, seul le critère « sans adversaire connu » intervient pour ce rôle. |
| CO-066 | `draft-coaching.js:277` | erreurs | `coach.matchup.unknown.limit.deductionApres` | Déduction d'après les rôles habituels : ce n'est pas une information du client. |
| CO-067 | `draft-coaching.js:84` | conseil/moteur | `coach.porteesCoachrangewords.deuxLonguePortee` | re:(Les deux : \|: )(longue portée\|portée moyenne\|corps à corps\|portée inconnue) |
| CO-068 | `draft-coaching.js:284` | conseil/moteur | `coach.comp.assassins.title.peuventAtteindre` | re:^(.+ peuvent atteindre .+\|\d adversaires sautent sur leurs cibles)$ |
| CO-069 | `draft-coaching.js:285` | conseil/moteur | `coach.comp.assassins.risk.peuventVenir` | peuvent venir sur toi d'un coup, et {C} s'échappe mal. Joue près d'un allié qui peut te protéger. |
| CO-070 | `draft-coaching.js:286` | conseil/moteur | `coach.comp.assassins.risk.resteZone` | Reste dans la zone de protection de ton équipe, et repère-les avant d'avancer. |
| CO-071 | `draft-coaching.js:287` | conseil/moteur | `coach.comp.assassins.risk.jeuFrontline` | En jeu, une frontline alliée qui peel le réduit, mais le calcul n'en tient pas compte. |
| CO-072 | `draft-coaching.js:288` | conseil/moteur | `coach.comp.assassins.level1.encaisseProtege` | encaisse ou protège bien : place-toi près de ton joueur le plus fragile. |
| CO-073 | `draft-coaching.js:289` | conseil/moteur | `coach.comp.assassins.level2.soliditeAide` | ta solidité aide ton équipe : garde ton carry à portée pour répondre à leur saut. |
| CO-074 | `draft-coaching.js:290` | conseil/moteur | `coach.comp.assassins.level3.profilSolide` | profil solide ou protecteur face à {N} plongeurs. |
| CO-075 | `draft-coaching.js:291` | conseil/moteur | `coach.comp.assassins.level1.cherchentCibles` | cherchent des cibles fragiles. Avant d'avancer, vérifie où ils sont sur la carte. |
| CO-076 | `draft-coaching.js:292` | conseil/moteur | `coach.comp.assassins.level2.plongeursPunissent` | Leurs plongeurs punissent un mauvais placement : quand ils ne sont pas visibles, considère qu'ils peuvent arri… |
| CO-077 | `draft-coaching.js:293` | conseil/moteur | `coach.comp.assassins.level3.plongeursEquipe` | plongeurs : l'équipe est forte sur une cible isolée, plus faible si elle doit traverser une frontline bien pla… |
| CO-078 | `draft-coaching.js:298` | conseil/moteur | `coach.comp.frontline.title.joueursSolides` | Des joueurs solides en face : {liste} |
| CO-079 | `draft-coaching.js:299` | conseil/moteur | `coach.comp.frontline.level1.faitBons` | fait de bons dégâts à ce type de cible. Si l'un d'eux te barre la route, tes coups restent utiles. |
| CO-080 | `draft-coaching.js:300` | conseil/moteur | `coach.comp.frontline.level2.joueursSolides` | joueurs très solides : le profil de {C} est valorisé ici. Reste en vie assez longtemps pour que tes dégâts com… |
| CO-081 | `draft-coaching.js:301` | conseil/moteur | `coach.comp.frontline.level3.attentionCompte` | Attention : il compte aussi les bruisers |
| CO-082 | `draft-coaching.js:302` | conseil/moteur | `coach.comp.frontline.level1.mettentDevant` | se mettent devant leurs joueurs fragiles. Cherche à les contourner : foncer dans le tas ne marchera pas. |
| CO-083 | `draft-coaching.js:303` | conseil/moteur | `coach.comp.frontline.level2.tropFrontline` | Trop de frontline à traverser pour {C} : attends que leurs sorts soient utilisés, ou joue ailleurs sur la cart… |
| CO-084 | `draft-coaching.js:304` | conseil/moteur | `coach.comp.frontline.level3.moteurCompte` | Le moteur compte un malus (« {critère} ») : {raison}. |
| CO-085 | `draft-coaching.js:305` | conseil/moteur | `coach.comp.frontline.level1.encaissentBeaucoup` | encaissent beaucoup. Un combat long les avantage si ton équipe manque de dégâts. |
| CO-086 | `draft-coaching.js:306` | conseil/moteur | `coach.comp.frontline.level2.joueursSolides2` | joueurs très solides : ils tiennent longtemps en combat groupé. Pense à leurs joueurs fragiles derrière. |
| CO-087 | `draft-coaching.js:307` | conseil/moteur | `coach.comp.frontline.level3.compositionResistante` | Composition résistante : à comparer aux dégâts soutenus de ton équipe, que l'outil ne connaît pas. |
| CO-088 | `draft-coaching.js:311` | conseil/moteur | `coach.comp.noFrontline.title.personneEncaisser` | Personne pour encaisser en face |
| CO-089 | `draft-coaching.js:312` | conseil/moteur | `coach.comp.noFrontline.level1.adversairesEncaisse` | adversaires n'encaisse vraiment : si ton équipe les atteint, le combat peut basculer vite. |
| CO-090 | `draft-coaching.js:313` | conseil/moteur | `coach.comp.noFrontline.level2.frontlineJoueurs` | Sans frontline, leurs joueurs fragiles sont exposés dès que quelqu'un les atteint. À l'inverse, ils peuvent te… |
| CO-091 | `draft-coaching.js:314` | conseil/moteur | `coach.comp.noFrontline.level3.forceDepend` | Leur force dépend alors de tuer avant d'être atteint : le premier engagement réussi pèse lourd. |
| CO-092 | `draft-coaching.js:321` | conseil/moteur | `coach.comp.control.title.listePeuvent` | {liste} peuvent t'immobiliser |
| CO-093 | `draft-coaching.js:325` | conseil/moteur | `coach.comp.control.level1.foncesIls` | Si tu fonces et qu'ils t'immobilisent, tu ne peux plus rien faire. Mieux vaut entrer après avoir vu leurs sort… |
| CO-094 | `draft-coaching.js:326` | conseil/moteur | `coach.comp.control.level2.regardeQuel` | Regarde quel sort vient de partir avant d'entrer. |
| CO-095 | `draft-coaching.js:324` | conseil/moteur | `coach.comp.control.level3.controlesCibles` | re:(contrôles ciblés visibles\.\|engages contre un champion peu mobile, pas les CC ciblés\.) |
| CO-096 | `draft-coaching.js:328` | conseil/moteur | `coach.comp.control.level1.peutRester` | peut rester loin de leurs contrôles, puis revenir au bon moment. Ne te place pas devant ton équipe. |
| CO-097 | `draft-coaching.js:329` | conseil/moteur | `coach.comp.control.level2.gardeCette` | Garde cette distance : c'est ce qui rend ce pick intéressant ici. |
| CO-098 | `draft-coaching.js:330` | conseil/moteur | `coach.comp.control.level3.profilCapable` | profil capable de jouer loin de l'engage et des contrôles. |
| CO-099 | `draft-coaching.js:331` | conseil/moteur | `coach.comp.control.level1.seulSorts` | Un seul de leurs sorts peut t'empêcher d'agir : évite d'être le premier à portée quand tu ne sais pas où ils s… |
| CO-100 | `draft-coaching.js:332` | conseil/moteur | `coach.comp.control.level2.plusieursControles` | Plusieurs contrôles ciblés : joue autour du moment où ils sont utilisés. |
| CO-101 | `draft-coaching.js:333` | conseil/moteur | `coach.comp.control.level3.controlesCibles2` | contrôles ciblés : à lire avec les délais de récupération, que l'outil ne connaît pas. |
| CO-102 | `draft-coaching.js:337` | conseil/moteur | `coach.comp.engage.title.listePeuvent` | {liste} peuvent lancer le combat |
| CO-103 | `draft-coaching.js:338` | conseil/moteur | `coach.comp.engage.level1.ilsPeuvent` | Ils peuvent démarrer un combat d'un coup. Évite de rester seul dans une zone que tu ne vois pas, surtout près … |
| CO-104 | `draft-coaching.js:339` | conseil/moteur | `coach.comp.engage.level2.equipeChoisit` | Équipe qui choisit quand le combat commence : place-toi avant l'apparition des objectifs, pas au dernier momen… |
| CO-105 | `draft-coaching.js:340` | conseil/moteur | `coach.comp.engage.level3.sourcesEngage` | sources d'engage : leur force est de forcer le combat. La vision et l'ordre d'arrivée sur les objectifs devien… |
| CO-106 | `draft-coaching.js:344` | conseil/moteur | `coach.comp.poke.title.listeTouchent` | {liste} touchent de très loin |
| CO-107 | `draft-coaching.js:345` | conseil/moteur | `coach.comp.poke.level1.ilsAbiment` | Ils t'abîment de loin avant le combat. Ne reste pas à découvert sans raison : ta vie baisse pour rien. |
| CO-108 | `draft-coaching.js:346` | conseil/moteur | `coach.comp.poke.level2.equipePoke` | Équipe de poke : si vous arrivez affaiblis, le combat démarre mal. Cherche à engager vite, ou à ne pas rester … |
| CO-109 | `draft-coaching.js:347` | conseil/moteur | `coach.comp.poke.level3.moteurTient` | Le moteur en tient compte pour {C} (« {critère} » : {raison}). |
| CO-110 | `draft-coaching.js:347` | conseil/moteur | `coach.comp.poke.level3.pokeGagne` | Le poke gagne le temps : il faut soit le raccourcir, soit soigner. Ton équipe n'est pas connue de l'outil. |
| CO-111 | `draft-coaching.js:359` | conseil/moteur | `coach.comp.damage.title.degatsSurtout` | re:^Dégâts surtout (physiques\|magiques) en face$ |
| CO-112 | `draft-coaching.js:360` | conseil/moteur | `coach.comp.damage.level1.objetProtege` | Un objet qui protège des dégâts {x} peut donc être plus utile que d'habitude : regarde lesquels existent. |
| CO-113 | `draft-coaching.js:361` | conseil/moteur | `coach.comp.damage.level2.resistanceAdaptee` | une résistance adaptée a plus de valeur. Vérifie en partie, car les objets adverses changent le partage. |
| CO-114 | `draft-coaching.js:362` | conseil/moteur | `coach.comp.damage.level3.estimationApres` | Estimation d'après les notes de Riot et une table écrite à la main. Le vrai partage dépend des objets et de ch… |
| CO-115 | `draft-coaching.js:363` | conseil/moteur | `coach.comp.damage.limit.estimationMesure` | Estimation, pas une mesure : les dégâts réels dépendent des objets. |
| CO-116 | `draft-coaching.js:386` | conseil/moteur | `coach.champ.profile.title.comprendre` | Comprendre {C} |
| CO-117 | `draft-coaching.js:384` | conseil/moteur | `coach.champ.profile.level1.joueLongue` | re:^.+ (se joue à (longue portée\|portée moyenne\|corps à corps) \(\d+\)\|change de portée selon sa forme ou s… |
| CO-118 | `draft-coaching.js:388` | conseil/moteur | `coach.champ.profile.level2.classesRiot` | re:^Classes Riot : |
| CO-119 | `draft-coaching.js:385` | conseil/moteur | `coach.champ.profile.risk.pointsForts` | re:(Points forts : \|Attention : \|Sa fiche ne lui donne pas de point fort particulier\.) |
| CO-120 | `draft-coaching.js:390` | conseil/moteur | `coach.champ.profile.limit.porteeClasses` | Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main. |
| CO-121 | `draft-coaching.js:394` | conseil/moteur | `coach.champ.difficulty.title.peutDemander` | {C} peut demander de la pratique |
| CO-122 | `draft-coaching.js:395` | conseil/moteur | `coach.champ.difficulty.level1.decouvresChampion` | Si tu découvres ce champion, prévois un temps de prise en main. Le choix reste le tien. |
| CO-123 | `draft-coaching.js:396` | conseil/moteur | `coach.champ.difficulty.limit.noteRiot` | Note de Riot non documentée et parfois contredite par celle du client : une indication, pas une mesure. |
| CO-124 | `draft-coaching.js:404` | conseil/moteur | `coach.champ.alternative.title.quandPreferer` | Quand préférer {C} ? |
| CO-125 | `draft-coaching.js:405` | conseil/moteur | `coach.champ.alternative.level1.faitMieux` | fait mieux sur un point : {raison}. |
| CO-126 | `draft-coaching.js:406` | conseil/moteur | `coach.champ.alternative.level2.pointCompte` | Si ce point compte le plus pour ta partie, ce choix devient intéressant. |
| CO-127 | `draft-coaching.js:406` | conseil/moteur | `coach.champ.alternative.level3.avantage` | re:: avantage de -?\d+,\d sur « |
| CO-128 | `draft-coaching.js:462` | conseil/moteur | `coach.champ.alternative.level3.choixPasserait` | Ce choix passerait devant {x}. |
| CO-129 | `draft-coaching.js:460` | conseil/moteur | `coach.champ.alternative.level3.aucunChangement` | re:(Aucun changement simple de la draft ne le fait passer devant\.\|Pas de calcul des cas où .+ passerait deva… |
| CO-130 | `draft-coaching.js:413` | conseil/moteur | `coach.limits.opponentAssumed.title.topJungle` | re:^(Top\|Jungle\|Mid\|ADC\|Support) supposé, à vérifier$ |
| CO-131 | `draft-coaching.js:414` | conseil/moteur | `coach.limits.opponentAssumed.level1.draftPense` | DRAFT pense que c'est {C}, sans certitude. Si tu sais que c'est un autre champion, corrige-le. |
| CO-132 | `draft-coaching.js:415` | conseil/moteur | `coach.limits.opponentAssumed.level2.deduitRoles` | déduit des rôles habituels. Une correction change le matchup pris en compte. |
| CO-133 | `draft-coaching.js:416` | conseil/moteur | `coach.limits.opponentAssumed.level3.deductionProbabiliste` | Déduction probabiliste sur les rôles habituels (confiance {x}), jamais une donnée du client. Sa qualité dépend… |
| CO-134 | `draft-coaching.js:419` | conseil/moteur | `coach.limits.unknownChampion.title.championInconnu` | {C} : champion inconnu de DRAFT |
| CO-135 | `draft-coaching.js:420` | conseil/moteur | `coach.limits.unknownChampion.text.aucuneFiche` | Aucune fiche pour ce champion : il n'entre pas dans ce conseil. Regarde ses sorts en jeu. |
| CO-136 | `draft-coaching.js:421` | conseil/moteur | `coach.limits.unknownChampion.limit.rienInvente` | Rien n'est inventé pour un champion sans fiche. |
| CO-137 | `draft-coaching.js:424` | conseil/moteur | `coach.limits.rulesOnly.title.classementRepose` | Ce classement repose sur des règles, pas sur des statistiques |
| CO-138 | `draft-coaching.js:425` | conseil/moteur | `coach.limits.rulesOnly.level2.aucunBilan` | re:^(Aucun bilan de vraies parties n'intervient ici\.\|Le score vient de règles et de fiches écrites à la main… |
| CO-139 | `draft-coaching.js:426` | conseil/moteur | `coach.limits.rulesOnly.level3.scoreAdditionne` | Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes pic… |
| CO-140 | `draft-coaching.js:437` | conseil/moteur | `coach.personal.experience.title.experience` | Tu as plus d'expérience sur {C} |
| CO-141 | `draft-coaching.js:438` | conseil/moteur | `coach.personal.experience.level1.connaitreChampion` | Connaître son champion compte : à toi de voir si cela change ton choix. |
| CO-142 | `draft-coaching.js:439` | conseil/moteur | `coach.personal.experience.level2.nettementParties` | nettement plus de parties que {C}. Cela ne change pas le classement, mais c'est un argument à peser. |
| CO-143 | `draft-coaching.js:440` | conseil/moteur | `coach.personal.experience.level3.horsScore` | hors du score. Le lien avec tes résultats n'est pas établi par ce seul chiffre. |
| CO-144 | `draft-coaching.js:441` | conseil/moteur | `coach.personal.experience.limit.nombreParties` | Le nombre de parties ne dit pas si tu joues bien ce champion. |
| CO-145 | `draft-coaching.js:447` | conseil/moteur | `coach.personal.results.title.resultatsDistinguent` | Tes résultats sur {C} se distinguent |
| CO-146 | `draft-coaching.js:448` | conseil/moteur | `coach.personal.results.level1.partiesGagnes` | re:^Sur \d+ parties, tu gagnes (plus\|moins) souvent avec .+ qu'avec tes autres champions\.$ |
| CO-147 | `draft-coaching.js:449` | conseil/moteur | `coach.personal.results.level2.ecartNet` | écart net avec le reste de ton compte, dans le sens |
| CO-148 | `draft-coaching.js:450` | conseil/moteur | `coach.personal.results.level3.lienCause` | Sans lien de cause établi : adversaires et niveau changent. |
| CO-149 | `draft-coaching.js:451` | conseil/moteur | `coach.personal.results.limit.passeCompte` | Passé sur ton compte : ni une prévision, ni un jugement sur ton niveau. |
| CO-150 | `draft-coaching.js:454` | conseil/moteur | `coach.personal.tooFew.title.tropPeu` | Trop peu de parties pour juger sur {C} |
| CO-151 | `draft-coaching.js:455` | conseil/moteur | `coach.personal.tooFew.level1.tropPeu` | trop peu pour dire comment ce champion te réussit. |
| CO-152 | `draft-coaching.js:455` | conseil/moteur | `coach.personal.tooFew.level2.aucuneTendance` | aucune tendance personnelle n'est tirée sous {N} parties. |
| CO-153 | `draft-coaching.js:455` | conseil/moteur | `coach.personal.tooFew.level3.aucuneConclusion` | aucune conclusion sur tes résultats. |
| CO-154 | `draft-coaching.js:468` | conseil/moteur | `coach.principle.degats.degatsPhysiques` | re:^(Dégâts physiques et magiques\|L'armure réduit les dégâts physiques, la résistance magique réduit les dégâ… |
| CO-155 | `draft-coaching.js:469` | conseil/moteur | `coach.principle.controles.pourquoiControles` | re:^(Pourquoi les contrôles comptent\|Un joueur immobilisé ne peut ni fuir ni attaquer\..+)$ |
| CO-156 | `draft-coaching.js:470` | conseil/moteur | `coach.principle.placement.placerAvant` | re:^(Se placer avant le combat\|Avant un combat, demande-toi qui peut t'atteindre\..+)$ |
| CO-157 | `draft-coaching.js:471` | conseil/moteur | `coach.principle.portee.jouerPortee` | re:^(Jouer avec la portée\|Le plus court doit s'approcher pour toucher.+)$ |
| CO-158 | `draft-coaching.js:472` | conseil/moteur | `coach.principle.echanges.bonEchange` | re:^(Un bon échange\|Attaque quand un sort important de ton adversaire vient de partir, puis recule\..+)$ |
| CO-159 | `draft-coaching.js:473` | conseil/moteur | `coach.principle.farm.vientAbord` | re:^(L'or vient d'abord des sbires\|Chaque sbire que tu achèves te donne de l'or\..+)$ |
| CO-160 | `draft-coaching.js:474` | conseil/moteur | `coach.principle.vagues.pousserAvant` | re:^(Pousser avant de bouger\|Avant de quitter ta voie, pousse ta vague contre la tour adverse\..+)$ |
| CO-161 | `draft-coaching.js:475` | conseil/moteur | `coach.principle.objectifs.objectifsGagnent` | re:^(Les objectifs gagnent les parties\|Une mise à mort rapporte davantage si elle mène à quelque chose.+)$ |
| CO-162 | `draft-coaching.js:476` | conseil/moteur | `coach.principle.vision.voirAvant` | re:^(Voir avant d'avancer\|Une balise montre qui arrive\..+)$ |
| CO-163 | `draft-coaching.js:477` | conseil/moteur | `coach.principle.top.roleTop` | re:^(Le rôle du top\|Tu joues souvent seul et loin de ton équipe\..+)$ |
| CO-164 | `draft-coaching.js:478` | conseil/moteur | `coach.principle.jungle.roleJungler` | re:^(Le rôle du jungler\|Tu n'as pas de voie : tu choisis où aider\..+)$ |
| CO-165 | `draft-coaching.js:479` | conseil/moteur | `coach.principle.mid.roleMid` | re:^(Le rôle du mid\|Ta voie est courte et centrale : tu peux aider les deux côtés\..+)$ |
| CO-166 | `draft-coaching.js:480` | conseil/moteur | `coach.principle.adc.roleAdc` | re:^(Le rôle de l'ADC\|Tu fais beaucoup de dégâts mais tu es fragile\..+)$ |
| CO-167 | `draft-coaching.js:481` | conseil/moteur | `coach.principle.support.roleSupport` | re:^(Le rôle du support\|Ta valeur ne se lit pas dans les mises à mort\..+)$ |
| CO-168 | `draft-coaching.js:28` | conseil/moteur | `coach.definitionsCoach.joueursSolidesPlacent` | re:^Les joueurs solides qui se placent devant et encaissent les coups\.$ |
| CO-169 | `draft-coaching.js:168` | conseil/moteur | `coach.draft.close.text.resteAffiche` | re:reste affiché tant que l'écart reste sous \d points, pour que le conseil ne change pas à chaque pick\. |
| CO-170 | `draft-coaching.js:322` | conseil/moteur | `coach.comp.control.text.peuventEngager` | re:(peuvent engager sur toi, et .+ est peu mobile\. Reste derrière ta frontline et attends leur engage\.\|Plac… |
| MA-001 | `draft-mastery.js:329` | conseil/moteur | `mastery.blocChoix.egaliteDraft` | Égalité sur la draft |
| MA-002 | `draft-mastery.js:329` | conseil/moteur | `mastery.blocChoix.meilleurChoixDraft` | re:^(Meilleur choix pour la draft\|Conseil affiché \(.+ mène de peu\))$ |
| MA-003 | `draft-mastery.js:213` | conseil/moteur | `mastery.provenanceIncertitudes.mesurePartiesPetit` | re:^(Mesuré sur tes parties\|Petit échantillon de tes parties\|Principe général)(\. .+)?$ |
| MA-004 | `draft-mastery.js:291` | conseil/moteur | `mastery.compromis.aucunChampionDisponible` | Aucun champion disponible dans ton pool. |
| MA-005 | `draft-mastery.js:292` | conseil/moteur | `mastery.compromis.draftMetPlutot` | La draft met plutôt en avant {liste} : ton choix reste possible, voici ce qu'il implique. |
| MA-006 | `draft-mastery.js:293` | conseil/moteur | `mastery.compromis.historiqueSeuleLecture` | Sans historique, seule la lecture de la draft est disponible. |
| MA-007 | `draft-mastery.js:294` | conseil/moteur | `mastery.compromis.partiesRoleSeule` | parties dans ce rôle : seule la lecture de la draft est disponible. |
| MA-008 | `draft-mastery.js:298` | conseil/moteur | `mastery.compromis.ressortDraftAussi` | ressort sur la draft, et c'est aussi ton champion le plus joué. |
| MA-009 | `draft-mastery.js:302` | conseil/moteur | `mastery.compromis.egaliteDraftEntre` | re:^Égalité sur la draft entre .+(Rien ne les sépare : à toi de voir\.\|c'est un argument, à toi de voir\.) |
| MA-010 | `draft-mastery.js:307` | conseil/moteur | `mastery.compromis.draftPencheNettement` | re:^La draft penche (nettement\|légèrement) vers |
| MA-011 | `draft-mastery.js:308` | conseil/moteur | `mastery.compromis.ecartNetFaible` | re:^Écart (net\|faible) sur la draft en faveur de |
| MA-012 | `draft-mastery.js:308` | conseil/moteur | `mastery.compromis.ecartScorePoints` | re:^Écart de score de -?\d+,\d points? en faveur de |
| MA-013 | `draft-mastery.js:295` | conseil/moteur | `mastery.compromis.presqueMemeNiveau` | presque au même niveau d'expérience. |
| MA-014 | `draft-mastery.js:268` | conseil/moteur | `mastery.incertitude.aucunHistoriqueImporte` | re:^(Aucun historique importé\|DRAFT ne sait pas ce que tu connais déjà : ce conseil ne porte que sur la draft… |
| MA-015 | `draft-mastery.js:269` | conseil/moteur | `mastery.incertitude.conseilPeutEncore` | Ce conseil peut encore changer. |
| MA-016 | `draft-mastery.js:270` | conseil/moteur | `mastery.incertitude.adversaireSuppose` | Adversaire supposé : {C} |
| MA-017 | `draft-mastery.js:270` | conseil/moteur | `mastery.incertitude.draftDeduitRoles` | DRAFT le déduit des rôles habituels. Si tu sais que c'est un autre champion, corrige-le. |
| MA-018 | `draft-mastery.js:270` | erreurs | `mastery.incertitude.informationClient` | Ce n'est pas une information du client. |
| MA-019 | `draft-mastery.js:271` | erreurs | `mastery.incertitude.adversaireDirectInconnu` | re:^(Adversaire direct inconnu\|Le conseil ne peut pas encore porter sur un duel précis\.)$ |
| MA-020 | `draft-mastery.js:272` | conseil/moteur | `mastery.incertitude.contreAucuneDonnee` | re:^(.+ contre .+ : aucune donnée\|Ni statistique, ni tableau pour ce couple : le matchup n'entre pas dans ce … |
| MA-021 | `draft-mastery.js:275` | conseil/moteur | `mastery.incertitude.tropPeuConclure` | re:(: trop peu pour conclure$\|trop peu pour savoir si ce champion te réussit\.\|^Intervalle à 95 % : ) |
| MA-022 | `draft-mastery.js:278` | conseil/moteur | `mastery.incertitude.partieRecenteDerniere` | re:^(Pas de partie récente sur .+\|Ta dernière partie importée dans ce rôle date de \d+ jours\. Le jeu a pu ch… |
| MA-023 | `draft-mastery.js:279` | conseil/moteur | `mastery.incertitude.changeSeulementDepuis` | re:( a changé$\|seulement depuis\. Ton expérience passée n'est peut-être plus aussi utile\.\|La liste des chan… |
| MA-024 | `draft-mastery.js:284` | conseil/moteur | `mastery.incertitude.peutSurprendreMatchup` | re:( peut surprendre$\|Ce matchup peut être difficile si tu ne connais pas ses compétences\.\|rencontres? avec… |
| MA-025 | `draft-mastery.js:226` | conseil/moteur | `mastery.sectionsTheorie.pourquoiTheorie` | re:^Pourquoi .+ en théorie$ |
| DA-001 | `draft-data.js:99` | réglages | `data.nomSources.nomsPortraitsPatch` | re:^(Noms, portraits et patch\|Rôles joués, parties récentes\|Rôles, référence livrée\|Rôles habituels\|Bilans… |
| DA-002 | `draft-data.js:168` | réglages | `data.typeSource.officielObserveEstimation` | re:^(Officiel\|Observé\|Estimation externe\|Écrit à la main)$ |
| DA-003 | `draft-data.js:99` | sécurité | `data.origineSource.riotGamesData` | re:^(Riot Games, Data Dragon\|API Riot, avec ta clé personnelle\|Meraki Analytics\|Écrit à la main\|API Riot, … |
| DA-004 | `draft-data.js:159` | sécurité | `data.conditionsSource.donneesPubliquesRiot` | re:(Données publiques de Riot\.\|Calculés sur ton PC avec ta clé\. Aucun nom de joueur n'est gardé\.\|Licence … |
| DA-005 | `draft-data.js:68` | sécurité | `data.noteBilans.encoreCalculesLance` | re:Pas encore calculés : lance « Mettre à jour les (rôles\|matchups) » avec ta clé Riot\. |
| DA-006 | `draft-data.js:76` | réglages | `data.noteRoles.calculFaitAvant` | re:(Calcul fait avant le suivi par patch\|patch\(s\) conservé\(s\)\. Les plus anciens comptent moins, sans êtr… |
| DA-007 | `draft-data.js:95` | réglages | `data.notePatch.dernierPatchConnu` | re:(Dernier patch connu\. Hors ligne, il peut être en retard\.\|Patch inconnu : l'âge des données est estimé d… |
| DA-008 | `draft-data.js:108` | réglages | `data.noteMeraki.livreeCetteVersion` | re:(Non livrée dans cette version : les rôles habituels écrits à la main prennent le relais\.\|Des taux, sans … |
| DA-009 | `draft-data.js:129` | réglages | `data.noteRoles.fichierAbsentChampions` | re:(Fichier absent\.\|champions\. Avis général, utilisé seulement là où aucune autre référence n'existe\.) |
| DA-010 | `draft-data.js:144` | réglages | `data.noteBilans.roleTopJungle` | re:Pour le rôle (Top\|Jungle\|Mid\|ADC\|Support)\. Sous \d+ parties utiles, un bilan n'est pas utilisé\. |
| DA-011 | `draft-data.js:150` | réglages | `data.noteTableau.adversairesMidTwisted` | adversaires au mid, pour Twisted Fate, Fizz, Kog'Maw et Xerath seulement. Sans statistiques ni patch. |
| DA-012 | `draft-data.js:155` | réglages | `data.noteFiches.fichesAvisGeneral` | fiches d'avis général, sans statistiques ni patch. À revoir si un champion est refait. |
| DA-013 | `draft-data.js:163` | réglages | `data.noteStatistiques.importFacultatifAffichees` | re:(Pas d'import sur ce PC\. Facultatif\.\|Affichées seulement : elles ne changent pas le classement\.) |
| DA-014 | `draft-data.js:74` | réglages | `data.rangBilans.silverEmeraudeClasse` | Silver à Émeraude, classé solo/duo |
| DA-015 | `draft-data.js:330` | réglages | `data.hypotheseEncore.encoreChoisi` | re:pas encore choisi |
| PR-001 | `draft-profiles.js:124` | erreurs | `profiles.importRefuse.fichierVideFichier` | re:^(Le fichier est vide\.\|Le fichier est trop volumineux pour une configuration\.\|Ce fichier n'est pas un J… |
| PR-002 | `draft-profiles.js:132` | erreurs | `profiles.importRefuse.roleMalForme` | re:(Le rôle .+ est mal formé\.\|Le rôle .+ n'a pas de liste de champions\.\|Le rôle .+ contient trop de champi… |
| PR-003 | `draft-profiles.js:148` | réglages | `profiles.import.favoriIgnorePool` | re:(Favori ignoré pour .+ : il n'est pas dans le pool\.\|Ancien format : seul ce rôle est remplacé\.\|Rôle abs… |
| PR-004 | `draft-profiles.js:153` | réglages | `profiles.importRefuse.roleCetAncien` | re:(Le rôle de cet ancien profil est inconnu\.\|La version du format est absente\.\|Ce fichier vient d'une ver… |
| PR-005 | `draft-profiles.js:189` | sécurité | `profiles.import.configurationInvalideRien` | re:(Configuration invalide : rien n'a été modifié\.\|L'enregistrement a échoué : ta configuration précédente e… |
