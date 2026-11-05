# Moteur de conseils

Explique une draft à partir de règles explicites et de données locales. Déterministe et hors ligne, sans clé Riot ni IA générative. Il lit le moteur stratégique et **ne change jamais le classement**.

**Statut :** intégré sur `integration/v2.15-coaching`, non publié. Pendant la draft, un seul conseil, issu d'une donnée ou d'un calcul. Les avis à relire restent dans l'onglet Conseils, sous « À confirmer ».

## Intégration

Dans `index.html`, après `draft-data.js` :

```html
<script src="draft-champion-official.js"></script>
<script src="draft-coaching.js"></script>
```

```js
const advice = coachingAdvice({
  enemies: state.enemies,            // noms affichés
  level: "beginner",                 // "beginner", "intermediate" ou "advanced"
  fixedRoles: state.roleOf,          // corrections de rôles
  manualOpponent: state.manualMid,   // adversaire corrigé par le joueur
  hasFront: PREFS.assumeFront,
  pick: V.shown,                     // facultatif : champion à expliquer
  personal: personalTrends(rows, { account, position }), // facultatif
  limit: 3,                          // taille de la vue courte
  fundamentals: true                 // bases du jeu pour les débutants
});
```

Réponse :

```js
{
  version: 1, level, role, pick, rival, opponent,
  confidence: "élevée" | "moyenne" | "faible" | "manuel",
  complete,       // 5 adversaires visibles
  unknown: [],    // champions sans fiche, exclus du conseil
  short: [],      // identifiants pour la vue courte
  offline: true,
  items: [{
    id,           // stable, ex. "comp.assassins"
    category,     // decision, matchup, composition, champion, limits, personal, fundamentals
    level, title, text,
    priority,     // plus haut = plus important
    basis,        // official, measured, smallSample, estimate, heuristic, general
    basisLabel,   // texte à afficher
    limits,       // limite à afficher, ou null
    terms: [],    // mots à définir avec coachingTerm(mot)
    evidence: [], // ce qui justifie le conseil
    stance,       // { key, sign } : lien avec un critère du moteur, ou null
    reviewed      // date de relecture
  }]
}
```

**Vue courte :** afficher les éléments dont l'`id` est dans `short`. **Vue détaillée :** tous les `items`, déjà triés. Afficher `basisLabel` et `limits` pour ne jamais faire passer une règle pour une statistique.

La vue détaillée compte au plus 7 conseils en Débutant et Avancé, 6 en Intermédiaire. Les limites et les résultats personnels ne sont jamais retirés, donc ils peuvent s'y ajouter. `maxItems` change ce plafond.

Chaque conseil porte aussi `verification` : `"rule"` si son contenu se déduit d'une donnée ou d'un calcul, `"human"` s'il exprime un avis sur le jeu qu'un joueur doit relire.

## Résumé pour la draft

Pour ne pas surcharger l'écran, `coachingSummary` renvoie le strict nécessaire :

```js
const s = coachingSummary({ enemies, level, fixedRoles, manualOpponent, hasFront, pick, personal });

{
  version: 1, level, pick, rival, opponent, confidence,
  headline: "Fizz, sinon Xerath",   // ou "Égalité : Master Yi et Viego", ou null sans pick
  tied: [],                         // champions à égalité, vide sinon
  explanation: { id, title, text, source, basis, limits },  // facultative, à ouvrir
  risk:        { ... },             // risque prioritaire, ou null
  lesson:      { ... }              // conseil adapté au niveau, ou null
}
```

`source` donne la provenance à afficher. L'explication et le risque ne répètent jamais le même conseil.

## Afficher une égalité

`analyze` renvoie maintenant `ties`, et `tiedWith` sur chaque pick. **Quand `tied` n'est pas vide, l'interface doit présenter tous les champions du groupe au même niveau**, sans en mettre un en avant. Sinon, l'ordre alphabétique avantage certains noms : Cho'Gath apparaît en tête dans 71,5 % des drafts du pool audité, mais n'obtient que 43,4 % en partageant les égalités.

`holdPick` reste valable pour éviter qu'un nom change sans raison pendant la draft. Il ne remplace pas l'affichage de l'égalité : il faut lire `tiedWith` à côté.

## Règles de gestion

- **Niveau :** il change les textes et les éléments affichés, jamais les scores.
- **Sources :** chaque conseil déclare la sienne. Une règle écrite à la main n'est jamais présentée comme une statistique.
- **Cohérence :** les règles lisent les critères du moteur, au lieu de les recalculer. `stance` permet de le vérifier.
- **Absence de données :** une donnée manquante supprime le conseil concerné, sans chiffre inventé.
- **Adversaire incertain :** les hypothèses sont citées, sans matchup précis.
- **Personnel :** hors du classement. Expérience à partir de 20 parties, résultats à partir de 30, et seulement si l'intervalle à 95 % exclut le reste du compte. Aucun jugement sur le joueur.
- **Intégrité :** préparation et compréhension. Aucune action dans le client, aucune information cachée, aucune consigne en temps réel pendant une partie.

## Ajouter un conseil

Une entrée dans `COACH_RULES` : `id`, `category`, `priority`, `basis`, `when(ctx)`, `build(ctx, data)`, avec 3 textes (débutant, intermédiaire, avancé). Évite les phrases qui valent pour toutes les drafts : le test en interdit plusieurs.

## Données

- `web/draft-champion-official.js` : généré par `tools/update-champion-official.ps1` depuis Data Dragon. À relancer après un patch.
- `DAMAGE_OVERRIDES` et `LOW_DAMAGE` : corrections écrites à la main, car les notes de Riot manquent ou trompent pour certains champions.
- `tools/draft-import-stats.ps1` ajoute maintenant `k`, `d` et `a` à chaque partie. Les anciennes lignes restent lisibles.

## Tests

`.\tools\run-coaching-tests.ps1 -Samples` : 133 contrôles de conseils, 18 sur les égalités et 177 sur le moteur. Ils vérifient : pas de texte cassé, pas de contradiction avec le moteur, pas de phrase creuse, résultat identique à chaque appel, classement inchangé, aucun appel réseau.

**Ils ne prouvent pas que les conseils sont bons.** Une relecture par un joueur reste indispensable.

## Relecture

`.\tools\run-coaching-review.ps1` génère `docs/COACHING-EXEMPLES.md` : 36 situations, soit 12 par niveau, avec le contexte exact et le déclencheur de chaque conseil.

Défauts trouvés à la relecture, tous corrigés et couverts par un test :

| Type | Défaut | Correction |
|---|---|---|
| Phrase cassée | « Xerath passe devant : . » quand le moteur ne donne pas de raison | Nom du critère à la place |
| Champion mal désigné | Katarina, Leona et Camille appelées « il » | Tournures sans pronom |
| Trompeur | « Tu peux le toucher sans qu'il te réponde » contre Yasuo ou Katarina | Plus d'avantage de portée annoncé contre un plongeur ou un champion très mobile ; ailleurs, rappel que ses sorts comptent |
| Trompeur | « Frappe le plus proche » pour Master Yi et Viego, qui cherchent souvent les cibles fragiles | Consigne retirée |
| Inadapté au rôle | « Pousse ta vague » pour un support, comparaison de portée pour un jungler | Texte propre au rôle, ou conseil retiré |
| Classification | « Corps à corps » et « touche de très loin » pour Jayce | Champions à forme variable traités à part |
| Chiffre trompeur | « 4 cibles fragiles : Lux, Jinx, Syndra » | « ... et 1 autre » |
| Trop catégorique | « Le combat est perdu d'avance », « Le farm passe avant tout » | Reformulés |
| Répétition | Même danger dit deux fois, jusqu'à 11 conseils en Débutant | Doublon retiré, plafond |
| Trop vague | Choix net sans sa raison principale | Conseil « Pourquoi ce champion » |
| Incertitude | Matchup présenté sans dire que l'adversaire est supposé | « (supposé) » dans le titre |

**Restent à juger par un joueur :** 121 conseils sur 213 dans les exemples (`À relire`). Je ne peux pas garantir leur justesse stratégique. Les points les plus discutables :

- les conseils de placement généraux ;
- « Un tank en face tue rarement vite » ;
- les tables de dégâts écrites à la main ;
- les fiches listées dans `docs/AUDIT-MOTEUR.md`.

Les bases « Se placer » et « Contrôles » reviennent aussi souvent, ce qui peut lasser.

En cas d'égalité, les conseils décrivent le premier champion du groupe, choisi par ordre alphabétique. L'interface devrait laisser choisir lequel expliquer avec `pick`.

## Ce qui reste à faire

- Interface, à relier après l'intégration du travail UX.
- Choix du niveau et de l'activation du personnel, à stocker dans les préférences.
- Relecture humaine de tous les textes et des tables de dégâts.
- Évaluation réelle : utile, compris, redondant ?
- Conseils après partie, à partir du journal.

La difficulté, l'expérience et le classement personnalisé sont traités à part : `docs/MAITRISE.md`.
