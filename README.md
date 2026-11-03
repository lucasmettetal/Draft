# DraftMid

Application Windows compagnon pour choisir son champion mid pendant la sélection de League of Legends, parmi un pool de Twisted Fate, Fizz, Kog'Maw et Xerath. Elle explique chaque recommandation.

DraftMid est un outil personnel non approuvé par Riot Games. Les portraits et illustrations appartiennent à Riot Games. Il ne modifie pas le client, ne s'y connecte pas et n'injecte rien.

## Utilisation

- `DraftMid-Portable.exe` : se lance directement, sans installation.
- `DraftMid-Setup.exe` : installe l'application pour l'utilisateur courant, sans droits administrateur, avec un raccourci dans le menu Démarrer.

Prérequis : Windows 10 ou 11 avec Microsoft Edge WebView2, généralement déjà présent. Node.js n'est pas nécessaire.

L'application s'ouvre en deux panneaux et ajoute une icône près de l'horloge. Saisis les champions adverses dans le panneau gauche : le verdict s'affiche dans le panneau droit. Si le client est visible et qu'il reste assez de place, les panneaux se placent de chaque côté.

Les préférences, le journal et le cache d'images sont dans `%LOCALAPPDATA%\DraftMid`.

## Compilation

```powershell
.\build.ps1
```

Les exécutables sont créés dans `dist\`. Seul le compilateur C# fourni avec Windows est utilisé. Le workflow GitHub Actions produit les mêmes fichiers à chaque push.

## Structure

| Élément | Emplacement |
|---|---|
| Moteur stratégique | `web/draft-engine.js` |
| Connecteurs de draft | `web/draft-providers.js` |
| Interface | `web/index.html` (React et Motion, sans compilation) |
| Fenêtres, icône, installateur | `src/DraftMid.cs` |
| Persistance | `localStorage` pour le journal, `settings.json` pour les fenêtres |
| Import des statistiques | `tools/draft-import-stats.ps1` |
| Tests du moteur | `web/draft-tests.html` |
| Tests natifs | `DraftMid-Portable.exe --selftest --out resultat.txt` |

Voir aussi [le moteur](docs/MOTEUR.md) et [l'intégration Riot](docs/INTEGRATION-RIOT.md).

## Choix technique

Tauri 2 aurait été le premier choix, mais il exige Rust et Node pour compiler. DraftMid utilise un hôte C# natif avec WebView2, le même moteur d'affichage que Tauri sous Windows. L'exécutable pèse 1,3 Mo et se compile sans rien installer. L'interface reste en HTML et JavaScript, donc une migration vers Tauri resterait possible.
