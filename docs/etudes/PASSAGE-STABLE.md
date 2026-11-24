# Étude : passage de la 3.0 en stable (3.0.0)

Étude seulement. Aucun code modifié. Base : `main` 4101ad6 (3.0.0-beta.2), tags `v2.14.0`, `v2.16.0` à `v2.16.2`, `v3.0.0-beta.1`, `v3.0.0-beta.2`.

Repères :
- **[V]** vérifié dans le code ou dans un document mesuré (fichier et ligne cités) ;
- **[S]** supposé : déduit de la lecture, pas mesuré.

## 0. En bref

1. Une 2.16.x **peut** passer en 3.0.0 toute seule, mais **seulement si le joueur clique** sur « Rechercher une mise à jour ». Aucune vérification n'a lieu au démarrage. Sans communication, la plupart resteront en 2.16.2. [V]
2. La 3.0.0 stable reprend exactement les noms de la 2.x : `%LOCALAPPDATA%\DraftMid`, Mutex `DraftMid.Instance`, valeur Run `DraftMid`. Elle démarre sur les données 2.x sans copie ni conversion de fichier. [V]
3. **Risque n° 1 :** au tag stable, le workflow ne compare pas la version de l'exe au tag. Si `AssemblyInformationalVersion` reste à `3.0.0-beta.3`, le selftest passe, et une **édition préversion** part en release stable. Les 2.16.x l'installeraient : dossier `DRAFT-beta`, plus de mise à jour. [V]
4. La compatibilité des données 2.x ↔ 3.0 a été mesurée le 13/11, sur `6218022`. Les formats de la beta.2 sont venus après : journal automatique, drafts annulées, `origin: "lcu"`. Elle est donc **à remesurer**. [V]
5. La 3.0.0 stable **n'a aucun lien** avec le dossier `DRAFT-beta`. Les données des testeurs n'y passent pas d'elles-mêmes. Recommandation : reprise manuelle guidée (§ 2). [V]

## 1. Mise à jour automatique 2.16.x → 3.0.0

### 1.1 La 2.16.2 verra-t-elle la 3.0.0 ?

Oui, si la release remplit **toutes** ces conditions (`ParseRelease`, `src/AppUpdater.cs`, inchangé en substance depuis la 2.16.0) [V] :

| Condition | Source | Release 3.0.0 par le workflow |
|---|---|---|
| Servie par `/releases/latest` : ni brouillon, ni préversion | `LatestUrl`, `ParseRelease` | Oui : le chemin stable n'a ni `--prerelease` ni `--latest=false` |
| Tag `vX.Y.Z` strict, sans suffixe | `ParseTag` | `v3.0.0` : oui |
| Fichier `*-Portable.exe` avec `digest` SHA-256, 300 Ko à 60 Mo, sous `/releases/download/v3.0.0/` | `ParseRelease` | `DRAFT-Portable.exe` : oui |
| `DRAFT-Portable.exe.sig` présent | `SignatureAsync` | Après `tools/sign-release.ps1`. Avant : « pas encore signée » |
| Plus récente que la version de l'exe | `IsNewer(Current, …)`, `AssemblyVersion` | 3.0.0 > 2.16.2 |
| Signature RSA de « DRAFT-update, tag, empreinte » | `ValidSignature` | Même clé publique de la 2.16.0 à la 3.0 (module `rrTfauqIObcE…`) |

Déclenchement [V] :
- seulement par un clic : menu de l'icône, ou bouton de Réglages (2.16.2 : `CheckForUpdate` appelé par `miUpdate` et par `checkUpdate` de la page) ;
- refusé pendant une draft ou une partie (`UpdateBlocked`) ;
- la boîte de confirmation de la 2.16.2 est figée : « version X, actuelle Y ». Elle ne dit pas que c'est une version majeure.

Les 2.16.0 et 2.16.1 ont la même logique de choix et la même clé. Seul change le plafond de la réponse réseau, ajouté en 2.16.1. Elles passent donc aussi en 3.0.0. [V]

### 1.2 Remplacement de l'exe

Même mécanisme en portable et en installé : `exePath` est le fichier lancé (`Application.ExecutablePath`). [V]

1. Téléchargement à côté de l'exe : `<nom>.maj.exe`.
2. Contrôles : taille, en-tête MZ, SHA-256, version interne (`FileVersion` = 3.0.0), signature.
3. **Selftest de la candidate** : `--selftest --data <DraftMid>\maj-test\donnees`, **180 s au plus** (`SelfCheckMs`, figé dans la 2.16.x).
4. Échange : `<exe>` devient `<exe>.ancien`, la candidate prend son nom (`Swap`).
5. Relance avec les mêmes arguments plus `--after-update <pid>`. La 3.0.0 doit se dire prête en moins de 60 s : elle le fait 5 s après son démarrage. Sinon, l'ancienne revient (`LaunchAndWatch`, `Rollback`).
6. Une fois la 3.0.0 confirmée, `.ancien` est supprimé (`CleanUp`). **Il ne reste aucune copie locale de la 2.16.2.**

Différences entre portable et installé :
- **Portable :** le nom du fichier ne change pas, même renommé par le joueur. [V]
- **Installé :** `%LOCALAPPDATA%\Programs\DraftMid\DRAFT.exe` est remplacé en place. La clé de désinstallation garde `DisplayVersion = 2.16.2`, car seul l'installateur l'écrit. C'est cosmétique. [V]
- Un exe placé dans un dossier protégé (Program Files) échoue : « DRAFT ne peut pas écrire dans son dossier ». Défaut déjà présent en 2.x. [V]

### 1.3 Premier lancement de la 3.0.0 sur les données 2.x

| Élément | Effet | Statut |
|---|---|---|
| Dossier, Mutex, valeur Run | `DraftMid`, `DraftMid.Instance`, `DraftMid` : ceux de la 2.x | [V] `Edition`, selftest « Édition stable » |
| Copie initiale (`LegacyCopy`) | Jamais en stable : `legacySource = null` | [V] `Program.Run` |
| Profil WebView2 | Même dossier `DraftMid\webview` : le localStorage est retrouvé tel quel | [V] `CoreWebView2Environment.CreateAsync(null, dataDir\webview)` |
| `app\<id>\` | Nouvelle interface extraite ; celle de la 2.x est supprimée (c'est un cache) | [V] `Extract` |
| `draft-stats.js`, `draft-history.js`, rôles, matchups | Relus et publiés dans `app\`. Les originaux ne changent pas | [V] `Extract`, `DataScripts.Publish` |
| `settings.json` | Réécrit à la première sauvegarde ; seul ajout : `Language`. Absent, il vaut « auto » : un Windows anglais affichera l'anglais | [V] `UserPreferences` (seul champ en plus de la 2.16.2) |
| Profils (localStorage) | `draftLegacyMark` posé. Copie `first-mark` dans `draftLegacyBackups` si `draftProfile` diffère. `draftProfile` et `draftPrefs` réécrits pour rester cohérents. Pas de notice pour une 2.16 | [V] `REPRISE-POOLS-2X.md` § 3 et 5 |
| Journal (`draftJournal`) | Anciennes entrées **non réécrites**. Les nouvelles ont `origin`, `app`, `gameStarted` ou `abandoned` | [V] commentaire de `isAbandoned`, `web/index.html` |
| 2.14 (profil unique) | Migré vers les profils par rôle, dans le rôle de `draftProfile` | [V] `COMPATIBILITE-2X.md` |
| Lecture du client | `LcuEnabled` repris de la 2.x : un joueur qui l'avait cochée a tout de suite les lectures 3.0, sans nouvelle question | [V] `LcuGate`, `UserPreferences` |
| Registre | Rien au démarrage. La valeur Run n'est touchée que par le réglage | [V] |

### 1.4 Risques

| # | Risque | Statut | Parade |
|---|---|---|---|
| R1 | Exe préversion publié comme stable (version oubliée) | [V] le chemin stable du workflow ne compare pas la version au tag ; le selftest accepte `3.0.0-beta.3` avec `FileVersion` 3.0.0 | Ajouter au chemin stable le contrôle `ProductVersion == tag`, déjà présent au chemin préversion |
| R2 | Selftest de la 3.0.0 plus long que 180 s sur un PC lent ou avec antivirus : mise à jour refusée | [S] durée jamais mesurée hors CI | La mesurer sur un PC lent ; viser moins de 60 s |
| R3 | Personne ne met à jour : pas de vérification au démarrage | [V] | README, site, notes, message aux joueurs |
| R4 | Formats beta.2 et beta.3 mal lus par une 2.16.2 en cas de retour arrière | [S] pas remesuré depuis le 13/11 | Remesurer (§ 6, étape 4) |
| R5 | Fenêtre entre publication et `.sig` : « pas encore signée » | [V] | Signer juste après la CI, comme en 2.16.x |
| R6 | La boîte de la 2.16.2 n'annonce pas une version majeure | [V] texte figé | Notes de version claires ; on ne peut rien changer côté 2.16.2 |
| R7 | Nouvelle adresse LCU pour le résultat automatique (`/lol-end-of-game/v1/eog-stats-block`, étude `origin/etude/journal-automatique` § 7) activée sans question pour qui avait coché la lecture en 2.x | [S] dépend de la beta.3 | À décider (§ 7) |

## 2. Données des testeurs bêta (`DRAFT-beta`)

### 2.1 Ce qui existe

- `%LOCALAPPDATA%\DRAFT-beta` contient, à la racine : `settings.json`, `draft-history.js`, rôles, matchups, relevés, `draft-beta.json`. Il contient aussi `webview\EBWebView\Default\Local Storage\leveldb` : pools, préférences et **journal**. [V] `LegacyCopy`
- **Le journal n'existe que dans le localStorage.** Aucun code n'en écrit une copie fichier. `draftmid.log` est le journal technique, pas celui des parties. Seule copie possible : « Télécharger le journal » (`draft-journal.json`), faite par le joueur. [V] `exportJournal`, recherche dans `src/`
- Au premier lancement, la bêta a copié la 2.x. Les entrées copiées ont gardé leur `id`. [V]
- Moyens déjà présents en 2.16.x et en 3.0 [V] :
  - « Exporter / Importer mes profils » (`draft-profils.json`). L'import **remplace** les pools, avec une sauvegarde et « Annuler ».
  - « Importer un journal » : ajout sans suppression, dédoublonné par `id`. Les entrées sans `picked` sont ignorées.

### 2.2 Pourquoi la 3.0.0 ne peut pas « juste lire » la bêta

- Le localStorage est un LevelDB interne à Chromium, propre au profil WebView2 `DRAFT-beta\webview`. Il est verrouillé tant que la bêta tourne. [V] pour le dossier ; [S] pour le verrou, constaté par `test-migration.ps1`, qui saute les fichiers verrouillés.
- Copier ces fichiers **remplace** tout le localStorage de la cible : c'est du tout ou rien. C'est pour cela que `LegacyCopy` ne copie que vers un dossier vide. [V]
- Une fusion exige de lire le localStorage de la bêta dans un **second environnement WebView2**, même origine `draftmid.local`, puis de fusionner en JavaScript. C'est du code nouveau. [S]

### 2.3 Options

| Option | Ce que fait la 3.0.0 | Code | Risques |
|---|---|---|---|
| **A. Aucune reprise** | Rien. Le testeur retrouve ses données 2.x ; `DRAFT-beta` reste intact | Aucun | Perte apparente du journal et des pools de la bêta ; plaintes des testeurs les plus actifs |
| **B. Reprise manuelle guidée** (recommandée) | Rien de nouveau. Une procédure dans les notes beta.3 et 3.0.0 | Aucun (option : une notice qui dit seulement « dossier DRAFT-beta trouvé : voici comment reprendre ») | Doublons si une même partie a été notée dans la 2.x **et** dans la bêta (deux `id`). Une entrée copiée de la 2.x puis complétée en bêta (résultat) n'est pas reprise : l'`id` existe déjà. L'import de profils remplace tous les pools. L'historique Riot ne suit pas (copie de fichier ou nouvel import) |
| **C1. Import proposé au 1er lancement, par remplacement** | Si `DRAFT-beta` a `draft-beta.json` : proposer « reprendre la préversion ». `DraftMid` est sauvegardé, puis remplacé par une copie de `DRAFT-beta` (`LegacyCopy` inversée) | Moyen | Perd tout ce qui a été fait en 2.x depuis la copie bêta. La bêta ne doit pas tourner. Décision irréversible sans la sauvegarde. Ne doit pas toucher un joueur non testeur |
| **C2. Fusion des journaux au 1er lancement** | Second WebView2 sur `DRAFT-beta\webview`, lecture de `draftJournal` et `draftProfiles`, fusion par `id` (l'entrée la plus complète gagne), choix des pools | Important | Code neuf juste avant la stable. Verrou si la bêta tourne. Règles de conflit à inventer. Tests lourds. Doublons d'une même partie toujours possibles |

### 2.4 Recommandation

**Option B pour la 3.0.0.** Le groupe de testeurs est petit, les outils existent déjà des deux côtés, et rien ne change dans le code avant la stable. La beta.3 sert de répétition.

Procédure à écrire (notes beta.3, puis 3.0.0) :
1. Dans la bêta : « Exporter mes profils », puis « Télécharger le journal ».
2. Quitter la bêta. Couper son démarrage avec Windows (valeur Run `DRAFT-beta`). Sinon, deux DRAFT tournent et affichent leurs panneaux. La bêta décide au démarrage seulement : lancée seule, sans valeur Run `DraftMid`, elle garde Ctrl+Alt+D et la stable échoue (1409) [V] `HotkeyLeftReason`.
3. Passer la 2.x en 3.0.0 (mise à jour ou téléchargement).
4. Dans la 3.0.0 : « Importer » les profils, puis « Importer un journal ».
5. Facultatif, 3.0.0 fermée : copier `DRAFT-beta\draft-history.js` dans `DraftMid`. Il remplace l'historique de la 2.x.
6. Garder `DRAFT-beta` quelques semaines, puis le supprimer à la main.

Cas simple à signaler : un testeur qui n'a plus ouvert la 2.x depuis la copie peut remplacer `DraftMid` par `DRAFT-beta` (option C1 faite à la main, après une sauvegarde).

Option C1 en 3.0.1 si plusieurs testeurs le demandent. C2 : non.

## 3. Installateur `DRAFT-Setup.exe` sur une 2.x installée

Code de l'installateur identique à la 2.16.2, sauf les textes et `DisplayVersion` (`Installer`, comparé à `v2.16.2`). [V]

| Élément | 2.16.x installée | 2.14 installée | Statut |
|---|---|---|---|
| Exécutable | `Programs\DraftMid\DRAFT.exe` écrasé | `DRAFT.exe` créé ; `DraftMid.exe` supprimé | [V] |
| Menu Démarrer | `DRAFT.lnk` recréé ; `DraftMid.lnk` supprimé s'il reste | `DraftMid.lnk` supprimé, `DRAFT.lnk` créé | [V] |
| Valeur Run `DraftMid` | Inchangée (elle vise déjà `DRAFT.exe`) | Réorientée vers `DRAFT.exe` si elle visait `DraftMid.exe` (`Repoint`) | [V] |
| Valeur Run vers un **portable** ailleurs | Inchangée : le portable démarre avec Windows et prend le Mutex. La 3.0 installée dit alors « déjà ouvert » | idem | [V] `Repoint`, `Mutex` |
| Clé `Uninstall\DraftMid` | `DisplayName` DRAFT, `DisplayVersion` 3.0.0 | idem | [V] |
| Données | Non touchées | Non touchées | [V] |
| Fin | Message, puis lancement de la 3.0.0 | idem | [V] |
| 2.x ouverte pendant l'installation | `File.Copy` sur un exe en cours : exception non gérée, sans message clair. Défaut déjà présent en 2.x | idem | [S] à tester ; conseil : quitter DRAFT avant |

Désinstallation (`--uninstall`) [V] :
- supprime `DRAFT.lnk`, `DraftMid.lnk`, la clé `Uninstall\DraftMid`, la valeur Run `DraftMid` (même si elle vise un portable), puis l'exe ;
- garde `%LOCALAPPDATA%\DraftMid`, comme le message l'annonce ;
- ne touche jamais `DRAFT-beta`.

Le Setup d'une préversion refuse de s'installer (`RefusesInstall`). Seule la 3.0.0 stable aura un `DRAFT-Setup.exe`. [V]

## 4. Retour arrière vers la 2.16.2

**Comment :**
- rien dans l'application : `IsNewer` refuse toute version plus ancienne, et `.ancien` est supprimé après la mise à jour [V] ;
- le joueur télécharge `DRAFT-Portable.exe` (ou Setup) de la release `v2.16.2` et remplace le fichier ;
- même dossier `DraftMid` : rien à copier.

**Ce qu'il retrouve** (`COMPATIBILITE-2X.md`, « Ce que chaque 2.x voit », mesure du 13/11) [V] :
- rôle joué, pools des autres rôles, exclusions et favoris ;
- journal, historique importé, rôles calculés ;
- niveau, rôle attribué, « Toujours au premier plan ».

**Ce qu'il perd** [V] :
- « `Language` … **Effacée** à la réécriture de `settings.json` » : la 2.x n'a que le français ;
- les fonctions 3.0 (panneaux V2, lecture des bans et picks, journal automatique, écran de fin de partie, Mes statistiques).

**Sans perte au retour en 3.0 :** « Tout revient à l'identique […] Une seule perte : la langue revient à “automatique” ». Les changements faits dans la 2.16.2 sont repris. [V]

**Non mesuré** [S] : la lecture par la 2.16.2 des entrées écrites en beta.2 et beta.3 (`abandoned`, `gameStarted`, `origin: "lcu"`, résultat automatique). Hypothèse : elles s'affichent comme des entrées sans résultat. Une draft annulée pourrait alors compter comme une partie dans les statistiques de la 2.16.2. À remesurer avant la 3.0.0.

## 5. Utilisateur d'une 2.14 (sans mise à jour automatique)

La 2.14 n'a pas `AppUpdater` : aucune recherche possible. [V] `git ls-tree v2.14.0 src/`

| Situation | À faire |
|---|---|
| 2.14 installée (`Programs\DraftMid\DraftMid.exe`) | Télécharger et lancer `DRAFT-Setup.exe` 3.0.0. Il migre l'exe, le raccourci et la valeur Run (§ 3) |
| 2.14 portable (`DraftMid-Portable.exe` ou renommé) | Quitter la 2.14. Remplacer le fichier par `DRAFT-Portable.exe` au **même endroit**. Si le démarrage avec Windows était actif ailleurs : dans la 3.0, le décocher puis le recocher. Le réglage affiche « actif » dès que la valeur `DraftMid` existe, quelle que soit sa cible (`Autostart.IsOn`) [V] |
| Données | Même dossier. Profil unique repris par rôle, journal lu (`COMPATIBILITE-2X.md`) [V] |
| Ensuite | Mises à jour depuis l'application (3.0.x) |

Le README dit déjà « Depuis la 2.14 ou avant, télécharge une fois la 2.16 ». À changer en « la 3.0 ».

## 6. Protocole de test avant la 3.0.0

**Règles :**
- toujours `--data <copie>` et `--run-key <clé de test>` ;
- jamais `%LOCALAPPDATA%\DraftMid` en écriture, jamais la vraie valeur Run, jamais l'exe réel ;
- la 2.x de l'utilisateur fermée pendant les copies : sinon des fichiers verrouillés sont sautés.

Préalable : une candidate stable, compilée depuis une branche, **sans tag**. `AssemblyInformationalVersion("3.0.0")` et `APP_VERSION = "3.0.0"`. Le selftest vérifie qu'ils sont égaux.

| # | Étape | Outil existant | Où |
|---|---|---|---|
| 1 | Selftest et e2e de la candidate, en FR et en EN. **Mesurer la durée du selftest** (limite de 180 s côté 2.16.x) | `--selftest --data`, `--e2e`, lanceur `bureau-cache.ps1` | Bureau caché |
| 2 | Données réelles **copiées** : la candidate lit-elle tout ? Source jamais modifiée (empreintes) | `tools/test-migration.ps1 -Source %LOCALAPPDATA%\DraftMid -Exe <candidate>` (copie, `--report-state`, clé Run de test) | Bureau caché |
| 3 | Chaîne de mise à jour depuis la **vraie 2.16.2** : échange, relance, `.ancien` supprimé, plantage et blocage ramènent l'ancienne | `tools/test-update.ps1 -Base <DRAFT-Portable.exe v2.16.2>` (source locale signée par une clé jetable) | Bureau caché |
| 3b | Même chose avec un dossier pré-rempli par une copie de données 2.x | `test-update.ps1` **à adapter** : paramètre de données ; et `Build` doit aussi changer `AssemblyInformationalVersion`, sinon les candidates « plante » et « fige » ratent leur selftest pour une autre raison [S] | Bureau caché |
| 4 | Aller-retour 2.16.2 → 3.0.0 → 2.16.2 → 3.0.0 sur une copie, avec des entrées beta.2/beta.3 (annulée, automatique, résultat). Comparer les empreintes et `--report-state` | Méthode de `COMPATIBILITE-2X.md`, exes `v2.14.0` et `v2.16.2` | Bureau caché |
| 5 | Données bêta : beta.3 sur une copie de `DRAFT-beta`, export, puis import dans la candidate sur une copie de `DraftMid` ; compter les doublons | `--data`, boutons Exporter et Importer (CDP ou à la main) | Bureau caché ou session séparée |
| 6 | Installateur sans le vrai registre : dossier d'essai contenant un ancien `DraftMid.exe`, valeur Run de test | `--test-install --install-dir <essai> --run-key <clé> --data <essai>` (sans `--data`, une sonde d'écriture touche le vrai dossier) | Bureau caché |
| 7 | Vrai `DRAFT-Setup.exe` sur une 2.14 puis une 2.16.2 installées : menu Démarrer, valeur Run, Applications et fonctionnalités, désinstallation ; installation avec la 2.x ouverte | — | **Autre compte Windows** ou VM |
| 8 | Démarrage avec Windows réel, Ctrl+Alt+D réel sans autre DRAFT | `TEST-CTRL-ALT-D-REEL.md` | **Autre compte Windows** |
| 9 | Selftest sur un PC lent ou avec antivirus actif (R2) | `--selftest` chronométré | **Autre machine** |
| 10 | Après publication seulement : la 2.16.2 voit-elle `v3.0.0`, signée ? Rien n'est remplacé | `DRAFT-2.16.2.exe --data <tmp> --test-update-live --out r.txt` (option présente en 2.16.2) | Bureau caché |
| 11 | Après publication : vraie mise à jour d'une 2.16.2 par le menu | — | **Autre compte Windows** |

Limites :
- `LatestUrl` est écrit en dur : le vrai chemin GitHub ne se teste qu'**après** publication (étapes 10 et 11) [V] ;
- un autre compte Windows a son propre HKCU et son propre `%LOCALAPPDATA%` : c'est le moyen le plus simple de tester les vrais chemins sans toucher l'installation de l'utilisateur ;
- `bureau-cache.ps1` n'est pas versionné : il n'existe que dans un dossier temporaire de session. À ranger dans `tools/`.

## 7. Bloquant, à décider, non bloquant

### Bloquant

| # | Point | Pourquoi |
|---|---|---|
| B1 | Workflow, chemin stable : refuser si `ProductVersion` ≠ tag | R1 : sinon une préversion peut partir chez tous les 2.16.x |
| B2 | Remesurer la compatibilité 2.x ↔ 3.0 avec les formats beta.2/beta.3 (§ 6, étape 4) | `COMPATIBILITE-2X.md` date d'avant 109 commits ; le retour arrière n'est pas prouvé |
| B3 | Chaîne 2.16.2 → 3.0.0 testée avec la vraie 2.16.2, et durée du selftest mesurée (§ 6, étapes 1, 3, 9) | Seul chemin de mise à jour des joueurs ; 180 s figées dans la 2.16.x |
| B4 | Vrai Setup sur une 2.14 et une 2.16.2 installées, dans une autre session (§ 6, étape 7) | Jamais fait pour la 3.0 ; l'édition préversion n'avait pas de Setup |
| B5 | Checklist de publication **stable** 3.0.0 (l'actuelle est celle de la beta.1) : Setup présent, « Latest » = v3.0.0, `.sig`, contrôle `--test-update-live` | Le chemin stable diffère : deux fichiers, release « Latest » |
| B6 | Résultat automatique : fini et testé en beta.3, ou absent de la 3.0.0 | Pas de demi-fonction dans une stable |
| B7 | Procédure de reprise des données bêta écrite (option B) dans les notes beta.3 et 3.0.0 | Sinon, perte apparente des journaux des testeurs |

### À décider

| # | Point | Options |
|---|---|---|
| D1 | Consentement pour une 3e adresse LCU (résultat), quand `LcuEnabled` vient de la 2.x | **Décidé (24/11/2026, utilisateur) : pas de nouvelle demande.** La lecture déjà acceptée suffit. Les textes qui décrivent la lecture mentionnent le résultat de fin de partie, et les notes de version le disent |
| D2 | Notes de la release 3.0.0 | Chemin stable avec `--notes-file` (corrige aussi les accents abîmés) ; ou `gh release edit` après |
| D3 | Site web (`draft-website-ashy.vercel.app`) et README (« Version 2.16.2 bêta », phrase sur la 2.14) | Mise à jour le jour de la sortie ou avant. Les liens `releases/latest/download/DRAFT-*.exe` suivront seuls [S] |
| D4 | Annonce aux joueurs 2.16.x | Seul moyen pour qu'ils cliquent sur « Rechercher une mise à jour » (R3) |
| D5 | Dette de tests : `web/draft-tests.html` ne rend rien (préexistant) ; 141 gabarits FR hors inventaire | Réparer ou retirer `draft-tests.html` ; compléter l'inventaire |
| D6 | Notice « dossier DRAFT-beta trouvé » dans la 3.0.0 | Petite aide à l'option B ; ou rien |
| D7 | Ctrl+Alt+D réel et plusieurs écrans | Faire les procédures (session séparée) ou garder en limites connues |

### Non bloquant

- `DisplayVersion` reste 2.16.2 après une mise à jour par l'application (cosmétique, déjà le cas en 2.16.x).
- Installer avec une 2.x ouverte : erreur sans message clair (déjà en 2.x). Conseil dans les notes.
- `sign-release.ps1` lance `--test-signature` sans `--data` : simple sonde d'écriture dans le vrai dossier de l'édition.
- `bureau-cache.ps1` à versionner ; `test-update.ps1` à adapter à `AssemblyInformationalVersion`.
- La désinstallation retire la valeur Run `DraftMid` même si elle vise un portable.
- L'explication des égalités entre styles différents reste générique.
- Passage en « auto » de la langue pour les données 2.x : un Windows anglais affichera l'anglais.
