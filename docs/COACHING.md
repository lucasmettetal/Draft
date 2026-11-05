# Moteur de conseils

Explique une draft à partir de règles explicites et de données locales. Déterministe et hors ligne, sans clé Riot ni IA générative. Il lit le moteur stratégique et **ne change jamais le classement**.

**Statut : prototype, sans interface.** `web/draft-coaching.js` n'est chargé par aucune page de l'application.

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

`.\tools\run-coaching-tests.ps1 -Samples` : 100 contrôles sur les 15 cas demandés, puis sur 6 000 conseils calculés à travers 5 profils et 3 niveaux. Ils vérifient : pas de texte cassé, pas de contradiction avec le moteur, pas de phrase creuse, résultat identique à chaque appel, classement inchangé, aucun appel réseau.

**Ils ne prouvent pas que les conseils sont bons.** Une relecture par un joueur reste indispensable.

## Ce qui reste à faire

- Interface, à relier après l'intégration du travail UX.
- Choix du niveau et de l'activation du personnel, à stocker dans les préférences.
- Relecture humaine de tous les textes et des tables de dégâts.
- Évaluation réelle : utile, compris, redondant ?
- Conseils après partie, à partir du journal.
