# Format du catalogue de traduction

Étude préparatoire pour DRAFT 3.0.0 bêta (français et anglais). Aucun fichier de production n'est modifié ici.
Base reprise sans la refaire : `DRAFT-ux/docs/ux/PLAN-I18N-FR-EN.md` § 3. Ce document fixe les détails qu'il laissait ouverts.
Exemples exécutables : `tools/i18n/exemples/fr.js`, `tools/i18n/exemples/en.js`, vérifiés par `tools/i18n/run-i18n-tests.ps1`.

## 1. Décisions

| Question | Décision | Pourquoi |
|---|---|---|
| Un ou deux catalogues (page et programme) | **Un seul par langue**, partagé. Les textes du programme ont des clés `native.*` | Une seule source de vérité. Les mêmes notions (« Clé Riot », « Région », « Annuler ») ont le même texte partout. Plusieurs textes natifs s'affichent déjà dans la page (états d'import, de calcul, de mise à jour, phase, placement : 60 entrées, voir INVENTAIRE.md). Le build n'utilise que `csc` : pas de `.resx`. |
| Emplacement | `web/i18n/fr.js` et `web/i18n/en.js` | `build.ps1` intègre déjà tout `web/` dans l'exe. Le programme lit la ressource intégrée, la page lit le fichier extrait. |
| Forme | `window.DRAFT_I18N_FR = { … };` : un objet JSON strict | Se charge par une balise `<script src>` (CSP actuelle) et se lit côté programme par `DataScripts.Canonical`, déjà testé. |
| Clés | Plates, hiérarchiques par points : `zone.sousZone.libellé` | Faciles à chercher (`grep`), à comparer entre langues et à fusionner. Pas d'objets imbriqués : un pluriel est le seul objet autorisé comme valeur. |
| Langue de référence | Le français | L'anglais dérive du français, jamais l'inverse. |
| Repli | Langue choisie, puis français, puis `[clé]` | Une traduction manquante n'efface jamais un texte. |

## 2. Structure du fichier

```js
// Catalogue français : référence. Données seulement. Format : docs/i18n/FORMAT-CATALOGUE.md.
window.DRAFT_I18N_FR = {
"_meta": {"lang": "fr", "name": "Français", "version": 1},
"advice.card.playButton": "Je joue {champion}",
"advice.tie.title": {"one": "{count} pick à égalité de score", "other": "{count} picks à égalité de score"},
"settings.riot.keyNotSaved": "Tu saisis ta clé toi-même : elle n'est pas enregistrée.",
"native.menu.quit": "Quitter"
};
```

Règles de forme, vérifiées par `tools/i18n/check-catalogs.ps1` :

1. Avant `window.` : seulement des lignes vides ou des commentaires `//` (même règle que `DataScripts.Canonical`).
2. Première ligne de code : `window.DRAFT_I18N_XX = {`, avec `XX` le code de langue en majuscules (`FR`, `EN`). Le nom de variable est fixé par la langue : un fichier `en.js` qui définirait `DRAFT_I18N_FR` est refusé.
3. **Une clé par ligne**, triées par zone. Une clé en double est une erreur (JSON l'accepterait et garderait la dernière sans bruit).
4. Après l'objet : `};` et rien d'autre. Pas d'expression, pas d'appel, pas de fonction.
5. Encodage UTF-8 sans BOM, fins de ligne LF.
6. `_meta` est la seule clé sans point : langue, nom de la langue écrit dans cette langue (pour le sélecteur), version du format.

## 3. Clés

- Forme : `^[a-z][a-zA-Z0-9]*(\.[a-zA-Z][a-zA-Z0-9]*)+$`. ASCII, `camelCase` par segment.
- Zones : voir [INVENTAIRE.md](INVENTAIRE.md) § « Règles de nommage ». Page : `nav`, `draft`, `enemy`, `role`, `advice`, `pool`, `plan`, `analysis`, `compare`, `duel`, `coachTab`, `stats`, `journal`, `rules`, `panel`, `settings`, `error`, `legal`, `brand`. Moteur : `engine`, `coach`, `mastery`, `data`, `profiles`. Programme : `native.*`.
- Le dernier segment décrit le sens, pas le texte. Il ne change pas quand le texte change.
- Une clé n'est jamais réutilisée pour un autre sens. Une clé supprimée l'est dans toutes les langues dans la même PR.
- Une clé par phrase complète. Jamais de phrase assemblée de morceaux de clés (« Calcul des » + « rôles » + « : démarrage... »), car l'ordre des mots change d'une langue à l'autre. Le code natif actuel assemble souvent des morceaux (NA-056, NA-065 à NA-069 de l'inventaire) : I18N-3 les remplace par une clé par phrase.

## 4. Valeurs

### Texte et variables

- Texte brut. **Aucun HTML** (`<b>`, `<br>`) : refusé par le contrôle.
- Variables nommées entre accolades : `{champion}`, `{count}`, `{role}`, `{query}`. Jamais `{0}`. Nom en `camelCase` ASCII.
- Mêmes variables dans toutes les langues pour une clé (contrôlé).
- Accolade littérale : `{{` et `}}`.
- Retour à la ligne : `\n` (boîtes de message natives). Dans la page, préférer deux clés à un `\n`.
- Une variable absente au rendu reste visible (`{champion}`) et est signalée en mode test. Elle n'est jamais remplacée par une chaîne vide.

### Mise en valeur (gras d'un nom)

Le catalogue ne contient pas de balise. La fonction `tx(key, vars)` rend un tableau de morceaux pour htm/React ; une variable peut recevoir un élément :

```js
tx("advice.change.overtakes", { champion: html`<b>${label(p)}</b>`, other: label(q) })
// FR « {champion} passe devant {other} » → [<b>Ahri</b>, " passe devant Zed"]
```

Le choix de ce qui est en gras reste dans le code. Le traducteur déplace `{champion}` librement dans la phrase.

### Pluriels

- Valeur objet dont les noms sont les catégories d'`Intl.PluralRules` : `zero`, `one`, `two`, `few`, `many`, `other`.
- Exigées : `one` et `other` en français et en anglais. `other` est toujours obligatoire. `many` (français, 1 000 000) est facultatif et retombe sur `other`.
- Français : `one` couvre 0 et 1 (et 1,5) : « 0 partie ajoutée » est correct. Anglais : `one` seulement pour 1 : « 0 games ».
- La variable `{count}` est fournie par `tp(key, count, vars)` et doit apparaître dans `other`.
- Côté programme, `Strings.PluralCategory` applique la même règle. Le banc compare les deux sur 15 nombres et 2 langues (0 différence, voir TESTS.md).
- Les bricolages actuels (`partie(s)`, `> 1 ? "s" : ""`) sont corrigés par la PR C-2 avant l'extraction ; l'extraction (I18N-1) reprend le texte tel qu'il est alors, sans le changer.

### Nombres et dates

- Nombres : `fmtNumber` donne la virgule décimale en français (`String(n).replace(".", ",")`, comme aujourd'hui) et le point en anglais. Pas de séparateur de milliers ajouté : il changerait le texte français.
- Dates : en français, `toLocaleDateString("fr-FR")` inchangé ; en anglais, `day: "numeric", month: "short", year: "numeric"` (« Nov 11, 2026 »), sans l'ambiguïté jour/mois du format américain.
- Pourcentages : le texte garde son espace (« +3 % ») en français ; l'anglais écrit « +3% ». C'est une affaire de catalogue, pas de code.

### Échappement

| Où | Ce qui protège | Remarque |
|---|---|---|
| Fichier `.js` | JSON strict : guillemets `\"`, barre `\\`, `\n`, ` ` | Le contrôle refuse tout ce qui n'est pas JSON. |
| Page | React échappe le texte inséré par htm | Jamais `innerHTML` avec une traduction. La seule exception actuelle (`index.html:697`, erreur de démarrage avant React) garde un texte fixe, hors catalogue. |
| Programme | `Label.Text`, `MessageBox`, menus : texte brut | Pas d'interprétation. |
| CSV de validation | Hors catalogue | Colonnes et valeurs restent des codes techniques (STRATEGIE.md § 6). |

Espaces insécables : le français actuel utilise des espaces simples avant « : ; ? ! » et dans « « » ». I18N-1 les garde tels quels (0 différence). Les remplacer par des espaces insécables est une décision de style à part, avec son propre relevé.

## 5. Chargement compatible avec la CSP

CSP actuelle de `web/index.html` : `default-src 'none'; script-src 'self' 'unsafe-inline' file:; … connect-src https://ddragon.leagueoflegends.com`. Le script principal est déjà en ligne ; on n'en ajoute aucun.

- Deux balises statiques, avant `draft-engine.js` (le moteur en a besoin pour ses messages) :
  `<script src="i18n/fr.js"></script>` puis `<script src="i18n/en.js"></script>`.
- Les deux langues sont toujours chargées : le repli sur le français est immédiat, sans chargement asynchrone avant le premier rendu. Coût estimé : 60 à 100 Ko par langue, lus depuis le disque.
- Pas de `fetch` (`connect-src` ne vise que Data Dragon), pas d'`eval`, pas de `new Function`.
- **Rien d'exécutable** : trois barrières.
  1. CI : `check-catalogs.ps1` refuse tout fichier qui n'a pas exactement la forme du § 2.
  2. Démarrage (I18N-3) : `Extract()` écrit `web/i18n/*.js` via `DataScripts.Canonical` (relu comme JSON, réécrit par le programme, chevrons neutralisés, `__proto__` retiré), comme les fichiers de données. Un catalogue modifié ne peut donc pas porter de code jusqu'à la page.
  3. `--selftest` charge le catalogue intégré avec le même parseur : un catalogue invalide fait échouer l'autotest avant toute publication.
- Les catalogues sont livrés dans l'exe. Ils ne sont jamais écrits par l'utilisateur ni téléchargés.

## 6. Fonctions de la page

| Fonction | Rôle |
|---|---|
| `t(key, vars)` | Texte. Cherche dans la langue active, puis en français, puis rend `[key]`. |
| `tp(key, count, vars)` | Pluriel par `Intl.PluralRules(lang).select(count)`. |
| `tx(key, vars)` | Morceaux pour htm/React (variables-éléments). |
| `fmtNumber(n)`, `fmtDate(d)` | Nombres et dates selon la langue (§ 4). |
| `I18N.lang` | Langue active (`fr` ou `en`) ; `document.documentElement.lang` suit. |

Prototype : `tools/i18n/i18n-runtime.js` (non chargé par DRAFT).

## 7. Repli et signalement

- Clé absente dans la langue active : texte français. Absente aussi en français : `[clé]` visible.
- Mode test (paramètre `i18ncheck=1` des pages de test, `Strings.TestMode` en `--selftest`) : chaque clé ou variable absente est notée une fois (`console.warn("[i18n] …")`, liste `I18N.missing()`), et le banc échoue avant une version.
- En usage normal : aucun message, aucun journal ; le repli est silencieux.
- Une clé `native.*` absente côté programme suit la même règle (`Strings.T`). Un catalogue anglais refusé (forme invalide) fait revenir tout le programme au français.

## 8. État après I18N-1 (extraction du français)

- Tous les textes visibles de la page sont dans `web/i18n/fr.js` : `index.html` appelle `t`, `tp`, `tx` ; le moteur (`draft-engine.js`, `draft-coaching.js`, `draft-mastery.js`, `draft-data.js`, `draft-profiles.js`) appelle `I18N.t` et `I18N.tp` et rend toujours des chaînes : aucun calcul, tri ni égalité ne dépend du texte.
- `draft-i18n.js` et le catalogue se chargent **avant** le moteur (page et bancs de test). Les gabarits sont découpés une seule fois.
- Restent volontairement en français, hors catalogue : les valeurs du relevé de validation (CSV), les identifiants de données (termes du lexique, types de mid du tableau, codes `physiques`/`magiques`, `manuel`), le message affiché avant React quand les fichiers manquent. Chaque cas est une exclusion justifiée du détecteur (`tools/i18n/exclusions.json`).
- Contrôles : `tools/i18n/check-catalogs.ps1 -Reference web\i18n\fr.js -Code . -Security tools\i18n\securite.json` (clés citées = clés du catalogue, variables fournies, pluriels par `tp`, aucune orpheline, messages de sécurité présents) ; `tools/i18n/detect-hardcoded.ps1 -Only web/ -Max 0`. Les deux tournent dans la CI.
- Une clé n'est jamais construite (`"nav." + v`) : on écrit une table de clés complètes, pour que le contrôle les retrouve.
- Pseudo-langue : `tools/run-layout-tests.ps1 -Lang qps` (copie de test seulement, jamais livrée ni proposée au joueur).
- Assemblages gardés tels quels pour ne changer aucun mot, à revoir en I18N-2 : minuscule forcée en début de raison (`coachReason`, `scenarioText`), retrait de « contre {champion} » dans une raison du moteur, liste « {first} et {last} », séparateur « ou », « {count} parties » sans pluriel là où le texte actuel n'en a pas.