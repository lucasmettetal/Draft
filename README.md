# DraftMid

Application Windows qui aide à choisir son champion pendant la sélection de League of Legends, à n'importe quel rôle et parmi les champions que tu choisis. Elle affiche une recommandation et ses raisons, et le joueur garde chaque décision.

**[Télécharger DraftMid-Portable.exe](https://github.com/lucasmettetal/Draft/releases/latest/download/DraftMid-Portable.exe)**

DraftMid n'est pas approuvé par Riot Games. Les portraits et illustrations appartiennent à Riot Games. L'exécutable n'est pas signé, donc Windows peut afficher un avertissement.

## Démarrage

Prérequis : Windows 10 ou 11 avec Microsoft Edge WebView2, généralement déjà présent.

1. Lance `DraftMid-Portable.exe`, sans installation. Aucune fenêtre ne s'ouvre : seule une icône apparaît près de l'horloge.
2. Pour une détection fiable, ouvre **Réglages** depuis le menu de l'icône et choisis ton rôle et tes champions, puis coche **Lire la draft depuis le client LoL**.
3. Lance une partie : les panneaux apparaissent autour du client pendant la sélection, puis disparaissent à la fin.

`DraftMid-Setup.exe` est la variante à installer, sans droits administrateur, avec un raccourci dans le menu Démarrer.

## Utilisation

- **`Ctrl+Alt+D`** affiche ou masque les panneaux.
- **Panneau gauche :** les champions adverses, remplis automatiquement avec la lecture du client, sinon à la main.
- **Panneau droit :** le verdict, ses raisons et le ban conseillé.
- **Adversaire direct supposé :** le client ne donne pas le rôle des adversaires. Ton adversaire direct reste une supposition, à corriger d'un clic si besoin.
- **Conseil approximatif hors du profil d'origine :** le tableau de matchups écrit à la main ne couvre que le mid avec Twisted Fate, Fizz, Kog'Maw et Xerath. Pour les autres, le conseil repose sur des fiches de champions écrites à la main, sans statistiques.
- **Calcul des matchups :** **Mettre à jour les matchups** ne calcule pour l'instant que ces quatre champions au mid.

Le menu de l'icône permet aussi de choisir le panneau unique, l'activation automatique, le positionnement automatique et le démarrage avec Windows.

## Lecture du client Riot

L'option est décochée par défaut. Elle lit localement la phase de jeu et les champions adverses verrouillés, en lecture seule, sans rien modifier dans le client et sans rien injecter.

**Riot n'a pas confirmé que cet usage est autorisé, et son activation se fait à tes risques.** Sans elle, tu saisis les adversaires à la main, et l'application utilise une estimation visuelle peu fiable, qui exige un client visible. Détails dans [LCU.md](docs/LCU.md) et [DETECTION.md](docs/DETECTION.md).

## Données facultatives

Ces deux fonctions demandent une [clé Riot](https://developer.riotgames.com). Elle reste masquée et n'est pas enregistrée. Une clé de développement ou personnelle est réservée à un usage privé.

**Mettre à jour les rôles :** dans le menu de l'icône ou les réglages. L'application compte les rôles joués sur 1 000 parties récentes, pour deviner le mid adverse. Compte environ 40 minutes en arrière-plan, à refaire à chaque patch. Sinon, elle utilise des statistiques livrées, qui datent du patch 16.3.

**Mettre à jour les matchups :** au même endroit. L'application calcule les bilans de ton pool contre chaque mid adverse, sur les 30 derniers jours. Compte environ 65 minutes en arrière-plan. Un matchup sous 30 parties garde l'avis écrit à la main.

**Bilans personnels :** télécharge `tools/draft-import-stats.bat` et `tools/draft-import-stats.ps1` dans un même dossier, puis lance le `.bat`. Il demande tes comptes une fois et les garde dans un fichier ignoré par Git. Importe ensuite le résultat avec le menu de l'icône > **Importer mes statistiques...**

## Limites connues

- **Testé en réel :** une partie personnalisée, de la sélection à la lecture des 5 adversaires. Le classé, le chargement et la partie restent à valider.
- **Deux écrans :** testé en réel à 100 % et 125 %, avec une fausse fenêtre LoL.
- **Testé seulement en simulation :** changement de résolution, reconnexion, requête bloquée et calcul des matchups avec Riot.
- **Recommandations :** les poids et les règles de composition sont des choix de conception. Les matchups viennent de vrais bilans une fois calculés, sinon d'un tableau écrit à la main. Voir [MOTEUR.md](docs/MOTEUR.md).

Tes préférences, ton journal et tes données restent sur ton PC, dans `%LOCALAPPDATA%\DraftMid`. L'application n'a ni compte, ni télémétrie.

## Développement

```powershell
.\build.ps1
```

Les exécutables sont créés dans `dist\`. Seul le compilateur C# fourni avec Windows est utilisé.

GitHub Actions compile et teste à chaque push. Pour publier une version, pousse un tag `v*` : le lien de téléchargement ci-dessus pointe alors vers cette version.

| Élément | Emplacement |
|---|---|
| Moteur stratégique | `web/draft-engine.js` |
| Connecteurs de draft | `web/draft-providers.js` |
| Interface | `web/index.html` (React et Motion, sans compilation) |
| Statistiques de rôles livrées | `web/draft-role-rates.js`, mises à jour avec `tools/update-role-rates.ps1` |
| Fenêtre du client et estimation visuelle | `src/DraftDetection.cs` |
| Lecture du client | `src/LcuConnector.cs` |
| Fenêtres, icône, installateur | `src/DraftMid.cs` |
| Persistance | `localStorage` pour le journal, `settings.json` pour les fenêtres |

**Tests :**
- Moteur : ouvrir `web/draft-tests.html` dans un navigateur.
- Rendu : `tools\run-layout-tests.ps1`, ou `-Shots` pour écrire aussi des captures. Il mesure chaque état et onglet sur 29 tailles au plus, dont 230 et 256 pixels d'interface, soit les panneaux réels à 125 %. Il ne remplace pas un contrôle à l'œil.
- Rendu : `toolsun-layout-tests.ps1`, ou `-Shots` pour écrire aussi des captures. Il mesure chaque état et onglet sur 29 tailles, dont 230 et 256 pixels d'interface, soit les panneaux réels à 125 %. Ce test ne remplace pas un contrôle à l'œil.
- Tests natifs : `DraftMid-Portable.exe --selftest --out resultat.txt`.
- Scénario complet, avec une fausse fenêtre et un faux client : `DraftMid-Portable.exe --e2e --data dossier --out resultat.txt`. Le raccourci y échoue si DraftMid tourne déjà.

Voir aussi [l'intégration Riot](docs/INTEGRATION-RIOT.md).

## Choix technique

Tauri 2 aurait été le premier choix, mais il exige Rust et Node pour compiler. DraftMid utilise un hôte C# natif avec WebView2, le même moteur d'affichage que Tauri sous Windows. L'exécutable pèse 1,4 Mo et se compile sans rien installer. L'interface reste en HTML et JavaScript, donc une migration vers Tauri resterait possible.
