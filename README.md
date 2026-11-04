# DraftMid

Application Windows compagnon pour choisir son champion mid pendant la sélection de League of Legends, parmi un pool de Twisted Fate, Fizz, Kog'Maw et Xerath. Elle explique chaque recommandation.

DraftMid est un outil personnel non approuvé par Riot Games. Les portraits et illustrations appartiennent à Riot Games. Il ne modifie pas le client, ne s'y connecte pas et n'injecte rien.

## Utilisation

- `DraftMid-Portable.exe` : se lance directement, sans installation.
- `DraftMid-Setup.exe` : installe l'application pour l'utilisateur courant, sans droits administrateur, avec un raccourci dans le menu Démarrer.

Prérequis : Windows 10 ou 11 avec Microsoft Edge WebView2, généralement déjà présent. Node.js n'est pas nécessaire.

L'application démarre en arrière-plan, avec une icône près de l'horloge et aucun panneau. Quand elle reconnaît une sélection, les panneaux apparaissent autour du client et le suivent. `Ctrl+Alt+D` les affiche ou les masque si elle se trompe. Saisis les champions adverses dans le panneau gauche : le verdict s'affiche dans le panneau droit.

La détection de la draft est une estimation visuelle locale, calibrée sur une seule partie personnalisée : voir [la détection](docs/DETECTION.md). La lecture du client est [implémentée mais verrouillée](docs/LCU.md), et testée uniquement sur un faux client.

Aucune distribution n'est prévue pour l'instant : DraftMid reste un outil personnel.

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
| Fenêtre du client et phase | `src/DraftDetection.cs` |
| Lecture du client, verrouillée | `src/LcuConnector.cs` |
| Interface | `web/index.html` (React et Motion, sans compilation) |
| Fenêtres, icône, installateur | `src/DraftMid.cs` |
| Persistance | `localStorage` pour le journal, `settings.json` pour les fenêtres |
| Import des statistiques | `tools/draft-import-stats.ps1` |
| Tests du moteur | `web/draft-tests.html` |
| Tests natifs | `DraftMid-Portable.exe --selftest --out resultat.txt`, et `--e2e --data dossier --out resultat.txt` pour le scénario avec une fausse fenêtre |

Voir aussi [le moteur](docs/MOTEUR.md) et [l'intégration Riot](docs/INTEGRATION-RIOT.md).

## Choix technique

Tauri 2 aurait été le premier choix, mais il exige Rust et Node pour compiler. DraftMid utilise un hôte C# natif avec WebView2, le même moteur d'affichage que Tauri sous Windows. L'exécutable pèse 1,3 Mo et se compile sans rien installer. L'interface reste en HTML et JavaScript, donc une migration vers Tauri resterait possible.
