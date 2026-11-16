# Architecture

Un hôte C# natif affiche une interface web dans WebView2. Tauri aurait été le premier choix, mais il exige Rust et Node pour compiler, alors que le compilateur C# est fourni avec Windows.

## Vue d'ensemble

| Partie | Rôle | Fichiers |
|---|---|---|
| Hôte natif | Icône, fenêtres, placement, installation, tests natifs | `src/DraftMid.cs` |
| Détection | Fenêtre du client, phase, estimation visuelle | `src/DraftDetection.cs` |
| Lecture du client | Deux endpoints en lecture seule | `src/LcuConnector.cs` |
| Calculs Riot | Rôles et matchups avec la clé du joueur | `src/RoleRatesUpdater.cs` |
| Interface | React et Motion, sans étape de compilation | `web/index.html`, `web/draft-vendor/` |
| Moteur | Score, classement, plan de jeu | `web/draft-engine.js` |
| Données et rôles | Sources, déduction probabiliste | `web/draft-data.js`, `web/draft-role-sheet.js` |
| Fiches | Traits écrits à la main | `web/draft-champions.js` |
| Connecteurs | Fournisseurs de draft | `web/draft-providers.js` |

La page et l'hôte échangent des messages JSON par `chrome.webview` : commandes `hostSend({ cmd })` d'un côté, état `HostJson()` de l'autre. Les fichiers web sont embarqués dans l'exécutable et extraits dans `%LOCALAPPDATA%\DraftMid\app`.

## Persistance

| Donnée | Emplacement |
|---|---|
| Profil, préférences, journal | `localStorage` du WebView2, avec une copie partielle dans `settings.json`. Format du journal : [JOURNAL.md](JOURNAL.md) |
| Fenêtres et apprentissage | `settings.json` |
| Calculs Riot | `draft-*-live.js`, `draft-*-state*.json` |
| Statistiques importées | `draft-stats.js` |
| Relevé de validation | `validation-drafts.csv`, si le diagnostic est activé |

## Moteur et rôles

Le score additionne 9 critères pondérés, sans effet de la méta hors des rôles et des bilans calculés : voir [moteur](MOTEUR.md). Le client ne donnant pas les rôles adverses, `draft-data.js` calcule une probabilité par rôle et plusieurs hypothèses pondérées : voir [données](DONNEES.md).

## Collecte Riot et statistiques personnelles

- **Rôles et matchups :** calculés sur le PC du joueur, avec sa clé, à son rythme (1,3 s entre deux requêtes), sans nom de joueur conservé.
- **Statistiques personnelles :** `tools/draft-import-stats.ps1` produit une ligne par partie (champion, rôle, résultat, KDA, champions adverses). Elles sont affichées, sans effet sur le classement.
- **Limites :** une clé personnelle est réservée à un usage privé, et rien de calculé n'est redistribué. Le calcul des matchups n'a jamais tourné avec une vraie clé.

## Compilation

```powershell
.\build.ps1 [-Out dossier] [-Prototypes]
```

- **Compilateur :** `csc` v4.0.30319, fourni avec Windows, donc C# 5 uniquement.
- **Embarqué :** tout `web/`, sauf `*-tests.html`, et les composants WebView2 de `lib/`.
- **Prototypes :** `src/Opgg*.cs` n'est compilé qu'avec `-Prototypes`. Ces fichiers ne sont pas suivis par Git.
- **Installateur :** même programme que le portable, qui s'installe quand son nom contient « Setup ».

## Tests

Voir [validation](VALIDATION.md).

| Commande | Portée |
|---|---|
| `web/draft-tests.html` | Moteur, dans un navigateur |
| `tools/run-layout-tests.ps1` | Rendu, jusqu'à 29 tailles par état |
| `tools/run-accuracy-tests.ps1` | Mesure de l'estimation des rôles adverses, pour les cinq rôles (voir MESURE-ROLES.md) |
| `tools/run-ux-shots.ps1` | Captures comparables |
| `DRAFT-Portable.exe --selftest` | Tests natifs |
| `DRAFT-Portable.exe --e2e` | Vraies fenêtres, faux client |
| `DRAFT-Portable.exe --soak N` | Stabilité sur plusieurs cycles |

## Dépendances tierces

| Élément | Licence | Note |
|---|---|---|
| React, ReactDOM | MIT, indiquée dans le fichier | `web/draft-vendor/` |
| Framer Motion | MIT selon son projet, non vérifié dans le fichier | `web/draft-vendor/` |
| WebView2 SDK | Conditions de Microsoft | `lib/`, redistribution à vérifier |
| Portraits et illustrations | Riot Games | Chargés depuis Data Dragon, non embarqués |
