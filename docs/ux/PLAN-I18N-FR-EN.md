# Plan d'internationalisation français / anglais

Aucune chaîne de production n'est traduite par cette étude. Le français reste la référence : l'anglais en dérive, jamais l'inverse.

## 1. Ordre imposé

1. Finir les PR D (explications fidèles), E (mid adverse) et C-2 (vocabulaire). Elles changent des centaines de textes ; extraire avant serait à refaire.
2. Extraire le français dans un catalogue **sans changer un mot**.
3. Vérifier que les sorties visibles sont identiques avant et après (relevé automatique).
4. Écrire le catalogue anglais.
5. Ajouter le sélecteur de langue (détection de Windows, choix manuel, persistance).
6. Tester tous les parcours en français et en anglais.

Les PR de la feuille de route suivent cet ordre : I18N-1 (étapes 2 et 3), I18N-2 (étape 4), I18N-3 (étape 5), I18N-4 (étape 6). Voir [FEUILLE-DE-ROUTE-UX.md](FEUILLE-DE-ROUTE-UX.md).

## 2. Où sont les textes aujourd'hui

Ordres de grandeur (chaînes contenant au moins deux mots, tests compris pour le C#). L'inventaire de référence reste celui de la PR #9 : **591 messages** avec fichier et ligne (`docs/moteur/AUDIT-VOCABULAIRE.md`, `tools/extract-ui-messages.ps1`). Il n'est pas refait ici.

| Fichier | Chaînes (approx.) | Nature |
|---|---|---|
| `web/index.html` | 317 | Navigation, réglages, panneaux, états vides, aides |
| `web/draft-coaching.js` | 222 | Conseils, titres, explications |
| `web/draft-engine.js` | 146 | Raisons, critères, plans de jeu, dangers |
| `web/draft-mastery.js` | 95 | Maîtrise, compromis, expérience |
| `web/draft-data.js` | 43 | Sources de données, notes de fiabilité |
| `web/draft-profiles.js` | 22 | Erreurs et avertissements d'import |
| `web/draft-providers.js` | 2 | États du fournisseur |
| `src/DraftMid.cs` | 182 (dont tests) | Menu, boîtes de message, bulles, états, installation |
| `src/AppUpdater.cs` | 23 | Erreurs de mise à jour |
| `src/RoleRatesUpdater.cs` | 19 | Dialogue de clé, états, erreurs |
| `src/HistoryImporter.cs` | 16 | Dialogue d'import, états |
| `src/DraftDetection.cs` | 5 | Phases (« Draft en cours (estimation) ») |
| `src/LcuConnector.cs` | 4 | États de connexion |

Points techniques relevés :

- Dates : `toLocaleDateString("fr-FR")` écrit 7 fois dans `web/index.html`.
- Nombres : `String(c.weight).replace(".", ",")` (`web/index.html:1925`).
- Pluriels bricolés : « partie(s) », « absente(s) », « ancienne(s) », « ajoutée(s) », « adversaire(s) » dans la page, « patch(s) », « conservé(s) » dans `draft-data.js`, et des `> 1 ? "s" : ""` dans `draft-coaching.js` (5) et `draft-mastery.js` (4).
- Langue de la page fixée : `<html lang="fr">`.
- Noms de champions : identifiants et noms anglais de Riot (« Master Yi », « Kog'Maw »), aussi en français aujourd'hui.
- Textes générés par le moteur : phrases complètes construites dans `draft-engine.js`, `draft-coaching.js`, `draft-mastery.js`, avec les noms insérés par concaténation.
- Tests qui lisent des textes français : `draft-coaching-tests.html` (6 repères), `draft-v215-tests.html` (5), et surtout les tests e2e natifs qui cherchent des boutons par leur texte (`src/DraftMid.cs:1379` « historique Riot », `:1479` « Rechercher une mise à jour »).

## 3. Catalogues

### Format

Un fichier par langue, dans `web/i18n/` :

```js
// web/i18n/fr.js : référence. Généré ou écrit à la main, jamais modifié par l'anglais.
window.DRAFT_I18N_FR = {
  "nav.home": "Accueil",
  "pool.add.placeholder": "Ajouter un champion : ahri, tf, kata...",
  "journal.import.added": { "one": "{count} partie ajoutée à ton journal.", "other": "{count} parties ajoutées à ton journal." },
  "native.menu.quit": "Quitter"
};
```

Règles :

- **Objet JSON strict** après `window.NOM =` : le même fichier se lit côté page (script) et côté programme (parseur `DataScripts.Canonical`, déjà écrit et testé, `src/DataScripts.cs`).
- **Clés plates à points**, par zone : `nav.`, `home.`, `pool.`, `sim.`, `stats.`, `settings.`, `overlay.`, `engine.`, `coach.`, `mastery.`, `data.`, `native.`.
- **Variables nommées** entre accolades : `{champion}`, `{count}`, `{role}`. Jamais de position (`{0}`), jamais de concaténation de morceaux de phrase.
- **Pluriels** : objet par catégorie de `Intl.PluralRules`. Français : `one`, `many`, `other` (0 et 1 sont `one` : « 0 partie ajoutée » est correct). Anglais : `one`, `other`.
- **Pas de HTML** dans les valeurs. Une mise en valeur (gras d'un nom) se fait par un marqueur reconnu par la fonction de rendu, par exemple `{champion:b}`.

### Chargement compatible WebView2 et CSP

- Deux balises statiques avant le script principal : `<script src="i18n/fr.js"></script>` et `<script src="i18n/en.js"></script>`. La CSP actuelle (`script-src 'self'`) les autorise ; **aucun script en ligne nouveau**, aucune ressource externe.
- Pas de `fetch` du JSON : `connect-src` n'autorise que Data Dragon.
- `build.ps1` embarque déjà tout `web/` récursivement : `web/i18n/` est inclus sans changement.
- Ces fichiers sont livrés avec l'exe, jamais écrits par l'utilisateur : ils ne passent pas par `DataScripts.Publish`.
- Poids estimé : 60 à 100 Ko par langue, chargés une fois par page.

### Fonction de traduction

Dans le script principal :

- `t(key, vars)` : cherche dans la langue choisie, puis en français, puis rend la clé entre crochets. En mode test, une clé absente est journalisée.
- `tp(key, count, vars)` : choisit la forme avec `new Intl.PluralRules(lang).select(count)`.
- `fmtDate(d)`, `fmtNumber(n)` : `Intl.DateTimeFormat(lang)` et `Intl.NumberFormat(lang)`, à la place des `"fr-FR"` et `replace(".", ",")`.
- `document.documentElement.lang` mis à la langue active.

### Textes du moteur

Le moteur ne doit plus produire de phrases, mais des **messages structurés** : `{ key: "engine.reason.targets", vars: { count: 2, champions: ["Syndra", "Jinx"] } }`. L'interface les rend avec `t()`. Cela sépare le calcul de sa formulation, comme le demande déjà la PR D (« l'explication doit correspondre au calcul »).

Condition : le classement ne change pas. Le banc de la PR #8 (instantané moteur) doit rester à 0 différence, et le relevé de messages (PR #9) identique en français.

### Messages natifs (C#)

Deux choix étudiés :

| Option | Pour | Contre |
|---|---|---|
| Ressources `.resx` et assemblys satellites | Standard .NET | `build.ps1` n'utilise que `csc.exe` de Windows : pas de `resgen` sans SDK ; deux sources de vérité (page et programme) |
| **Catalogue partagé `web/i18n/*.js`, clés `native.*`** | Une seule source ; parseur strict déjà présent ; aucun outil de plus ; cohérence page et dialogues garantie | Lecture à faire au démarrage (quelques ms) |

Recommandation : **catalogue partagé**. Une classe `Strings` lit la ressource intégrée `web/i18n/{lang}.js` avec `DataScripts.Canonical`, garde un `Dictionary<string, object>`, et expose `Strings.T("native.menu.quit")` et `Strings.Plural(...)`. Repli sur le français, puis sur la clé.

Zones natives à couvrir : menu de l'icône, infobulle de l'icône, bulles, `MessageBox` (installation, désinstallation, WebView2 absent, déjà ouvert, mise à jour, dossier non inscriptible), dialogues de clé et d'import, états d'import et de calcul, erreurs de mise à jour (`AppUpdater.FriendlyError`), phases (`PhaseText`), états LCU, titres de fenêtres.

## 4. Choix de la langue

- **Détection** : au premier lancement, `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName`. `fr` donne le français ; toute autre langue donne l'anglais.
- **Choix manuel** : Réglages > Général > Langue : « Automatique (Windows) », « Français », « English ». Chaque langue écrite dans sa propre langue.
- **Persistance** : `settings.json`, champ `Language` (`"auto"`, `"fr"`, `"en"`) dans `UserPreferences` (`src/DraftMid.cs:168`). Pas dans le stockage du navigateur : le programme en a besoin avant toute page, et ce stockage peut être vidé.
- **Transmission à la page** : le programme ajoute `&lang=fr` ou `&lang=en` à chaque adresse qu'il ouvre (`Ensure`, `OpenFull`), et l'envoie dans l'état (`HostJson`). La page lit `lang` avant le premier rendu.
- **Changement à chaud** : la page de la fenêtre principale se recharge ; les panneaux se rechargent seulement hors draft (même règle que `ReloadRolesIfIdle`, `src/DraftMid.cs:870-875`). Le menu de l'icône se reconstruit.

## 5. Termes LoL en anglais

L'anglais suit les mots de la communauté, plus simples encore qu'en français. Base : `docs/technique/GLOSSAIRE-LOL.md` (PR #9).

| Français (référence) | Anglais proposé |
|---|---|
| Frontline (une frontline) | Frontline |
| Carry fragile | Squishy carry |
| Dive, champion de dive | Dive, diver |
| CC ciblé | Targeted CC |
| Engage | Engage |
| Poke | Poke |
| Peel | Peel |
| Sûr en blind | Blind-safe |
| Matchup direct | Lane matchup |
| Mid d'en face, ton vis-à-vis | Enemy mid, lane opponent |
| Choix net / Choix serré | Clear pick / Close call |
| Je joue {champion} | Play {champion} |
| Ban conseillé | Suggested ban |
| Plan de jeu, Gagne si | Game plan, Win condition |
| Menaces, Cibles | Threats, Targets |
| Teamfight, lane, roam, gank, ward, TP, ult | Identiques |
| Top, Jungle, Mid, ADC, Support | Top, Jungle, Mid, ADC (Bot), Support |
| Silver à Emerald | Silver to Emerald |
| Mes statistiques, Mon pool | My stats, My pool |

Noms de champions : garder les noms anglais de Riot dans les deux langues (comportement actuel). Les alias de recherche restent communs ; on peut ajouter les noms français du client (« maitre yi ») plus tard, sans changer l'affichage.

## 6. Textes longs et débordements

- **Pseudo-langue `qps`** générée par un outil de test (jamais livrée) : accents ajoutés, texte allongé de 35 %, crochets autour de chaque chaîne (`[Ãççüéîl ·····]`).
- Lancer `tools/run-layout-tests.ps1` avec `lang=qps` : les débordements et textes coupés apparaissent avec les mêmes mesures qu'aujourd'hui (29 tailles de panneaux, 5 de fenêtre).
- Toute chaîne visible **sans crochets** en pseudo-langue est une chaîne restée en dur.
- L'anglais est en général plus court que le français ; les risques réels sont les mots longs (« Recommended », « Close call ») dans les panneaux de 230 px et les boutons des dialogues natifs (`LayoutProblem()` à lancer en anglais).

## 7. Traductions manquantes

- Repli : langue choisie, puis français, puis `[clé]`.
- En développement : liste des clés absentes en anglais écrite par le test, sans bloquer.
- Avant une version : le test échoue si une clé française n'a pas d'équivalent anglais, ou si les variables diffèrent.

## 8. Tests automatisés

| Test | Ce qu'il vérifie | Quand |
|---|---|---|
| Clés identiques | Mêmes clés en `fr` et `en` | I18N-2 |
| Variables identiques | Même ensemble de `{variables}` par clé | I18N-2 |
| Pluriels complets | Catégories requises par `Intl.PluralRules` de chaque langue | I18N-2 |
| Aucune chaîne en dur (page) | Pseudo-langue : aucun texte visible sans crochets, sur tous les états du banc de rendu | I18N-1 |
| Aucune chaîne en dur (code) | Recherche de lettres françaises dans les gabarits `html\`` hors `t()` | I18N-1 |
| Relevé avant/après | `tools/extract-ui-messages.ps1` (PR #9) sur les mêmes drafts, avant et après extraction : **0 différence** en français | I18N-1 |
| Catalogue natif | Toutes les clés `native.*` utilisées existent ; dialogues sans texte coupé (`LayoutProblem()`) en FR et EN | I18N-3 |
| Parcours e2e | `--e2e` en FR et EN ; les tests cherchent des attributs `data-test`, plus des textes | I18N-4 |
| Moteur | Instantané du banc (PR #8) à 0 différence de classement | I18N-1 et à chaque PR |

Préalable : remplacer les sélecteurs par texte des tests natifs (`src/DraftMid.cs:1379`, `:1479`) et des pages de test par des attributs `data-test`, **avant** l'extraction. Sinon les tests casseront en anglais.

## 9. Cohérence Web et dialogues Windows

- Même catalogue, mêmes clés pour les mêmes notions (« Clé Riot », « Région », « Annuler »).
- Les bulles et boîtes natives reprennent le vocabulaire de la page (aujourd'hui, le menu dit « Positionnement automatique » et la page « Coller les panneaux au client LoL » : un seul libellé par réglage avant de traduire).
- Les dialogues de clé ont une largeur fixe de 340 à 360 px : vérifier l'anglais avec `LayoutProblem()`.

## 10. Fichiers à modifier plus tard

| Fichier | Étape | Changement |
|---|---|---|
| `web/i18n/fr.js` (nouveau) | I18N-1 | Catalogue de référence |
| `web/i18n/en.js` (nouveau) | I18N-2 | Catalogue anglais |
| `web/index.html` | I18N-1, I18N-3 | `t()`, dates, nombres, `lang`, deux balises de script, sélecteur |
| `web/draft-engine.js` | I18N-1 | Messages structurés (clé et variables) |
| `web/draft-coaching.js` | I18N-1 | Idem |
| `web/draft-mastery.js` | I18N-1 | Idem |
| `web/draft-data.js` | I18N-1 | Libellés des sources |
| `web/draft-profiles.js` | I18N-1 | Erreurs et avertissements |
| `web/draft-providers.js` | I18N-1 | États |
| `src/DataScripts.cs` | I18N-3 | Lecture du catalogue intégré |
| `src/DraftMid.cs` | I18N-3 | `Strings`, `UserPreferences.Language`, `lang=` dans les adresses, menu, boîtes, bulles |
| `src/DraftDetection.cs`, `src/LcuConnector.cs` | I18N-3 | Phases et états |
| `src/HistoryImporter.cs`, `src/RoleRatesUpdater.cs`, `src/AppUpdater.cs` | I18N-3 | Dialogues, états, erreurs |
| `tools/extract-ui-messages.ps1`, `web/draft-messages-tests.html` (PR #9) | I18N-1 | Rattacher les messages aux clés, plus au texte littéral |
| `tools/run-layout-tests.ps1`, `web/draft-layout-tests.html` | I18N-1 | Paramètre `lang` (dont `qps`) |
| `tools/run-ux-shots.ps1` | I18N-4 | Captures FR et EN |
| Pages `web/*-tests.html` | I18N-1 | Langue forcée à `fr`, ou assertions sur les clés |
| `docs/utilisateur/GUIDE.md` | I18N-4 | Mise à jour, puis version anglaise |

## 11. Dépendances avec les PR V2.17

| PR | Branche | Fichiers | Lien avec l'i18n |
|---|---|---|---|
| #6 F | `v217/protocole-drafts-reelles` | `tools/validation-report.ps1`, docs | Le relevé CSV (`ValidationHeader`, `src/DraftMid.cs`) garde des colonnes techniques : à ne pas traduire |
| #7 B | `v217/mesure-roles` | `tests/roles/*`, `tools/run-accuracy-tests.ps1`, `tools/run-role-tests.ps1` | Aucun texte d'interface ; pas de conflit attendu |
| #8 A | `v217/banc-evaluation` | `tests/moteur/*`, `tools/run-engine-eval.ps1`, CI | L'instantané moteur sert de garde : 0 différence de classement à l'extraction |
| #9 C | `v217/vocabulaire-lol` | Glossaire, inventaire, `tools/extract-ui-messages.ps1`, `web/draft-messages-tests.html` | Outil de relevé avant/après ; son catalogue littéral devra passer aux clés |
| D | `v217/explications-fideles` (en cours) | `web/index.html`, `draft-coaching.js`, `draft-mastery.js` | Change les textes et leur logique : à fusionner **avant** I18N-1 |
| E | (en cours) | `web/index.html` (lignes adverses), `draft-coaching.js`, `draft-data.js` | Idem |
| C-2 | (en cours) | `draft-engine.js`, `index.html`, `draft-coaching.js`, `draft-profiles.js`, `draft-data.js` | Applique le vocabulaire : à fusionner **avant** I18N-1 |

## 12. Risques de conflit

- **I18N-1 touche presque chaque ligne de texte** de `web/index.html` et des trois fichiers du moteur. Toute branche ouverte en même temps sur ces fichiers entrera en conflit. Règle : aucune autre PR sur ces fichiers pendant I18N-1 ; I18N-1 courte (quelques jours), fusionnée vite.
- **Refonte UX et extraction** : la feuille de route place les corrections de comportement (UX-1, UX-2) avant I18N-1, et la nouvelle navigation (UX-3 et suivantes) après. Les nouveaux écrans écrivent alors leurs textes directement dans `fr.js`. L'anglais (I18N-2) vient après la navigation pour ne traduire qu'une fois.
- **Tests par texte** : à convertir en `data-test` dans une PR préalable courte, sinon I18N-3 casse le e2e.
- **Main locale en retard** (`b4fdeba`) : toujours comparer et partir de `origin/main`.
