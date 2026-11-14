# Journal des drafts

Le journal garde les drafts réelles où le joueur a dit quel champion il jouait, et leur résultat. Il sert à Mes statistiques (« Conseil suivi », victoires par champion).

## Où et quand

- **Stockage :** `localStorage`, clé `draftJournal`, du navigateur intégré (hôte `draftmid.local`). C'est un tableau JSON d'entrées. Export : « Télécharger le journal ». Import : Mes statistiques, sans rien supprimer.
- **Création :** au premier choix d'une vraie draft (« Je joue », une carte de « Ton pool », une case d'égalité).
- **Mise à jour :** à chaque nouveau choix de la même draft, à chaque action sur la composition (`enemies`, `mid`), puis au résultat.
- **Simulation :** rien n'est écrit. Une draft simulée n'a pas de résultat.
- **Aucune déduction :** le champion choisi vient toujours d'un clic du joueur, jamais de l'historique Riot.

## Champs

| Champ | Depuis | Sens |
|---|---|---|
| `id` | 2.x | Horodatage de création (ms), unique |
| `date` | 2.x | Date de création, ISO 8601 |
| `picked` | 2.x | Champion choisi par le joueur (identifiant de pick, par exemple `TF`) |
| `suggested` | 2.x | Conseil au moment de ce choix. À égalité, l'ex æquo choisi, s'il fait partie du groupe |
| `close` | 2.x | Conseil serré au moment du choix |
| `enemies`, `mid` | 2.x | Composition adverse et vis-à-vis supposé, tenus à jour pendant la draft |
| `result` | 2.x | `null` (en attente), `"win"`, `"loss"` ou `"skip"` (ignorée) |
| `engine` | 2.x | Version du moteur |
| `tied` | 3.0 | `true` si le conseil était une égalité au moment du choix |
| `tiedWith` | 3.0 | Groupe complet des ex æquo à ce moment (vide sans égalité) |
| `recommended` | 3.0 | Premier du classement du moteur. Il peut différer du nom affiché, maintenu pendant la draft pour ne pas clignoter |
| `origin` | 3.0 | `"draft"` : vraie draft, détectée ou saisie à la main hors simulation |
| `app` | 3.0 | Version de DRAFT qui a créé l'entrée (`APP_VERSION`) |

`suggested`, `close`, `tied`, `tiedWith` et `recommended` décrivent le moment du choix : ils changent avec un nouveau choix dans la même draft, pas quand le conseil bouge seul. `origin` et `app` sont posés à la création.

**Conseil suivi :** `picked === suggested`. À égalité, choisir un ex æquo du groupe compte comme conseil suivi ; un champion hors du groupe compte comme autre choix.

## Exemple

Entrée créée par la branche du correctif : DRAFT conseille Twisted Fate, le joueur choisit Vex. `app` vaut la version déclarée par l'application, encore 2.16.2 tant que le numéro de la 3.0 n'est pas monté.

```json
{
  "id": 1794567600000,
  "date": "2026-11-13T11:00:00.000Z",
  "suggested": "TF",
  "close": true,
  "tied": false,
  "tiedWith": [],
  "recommended": "TF",
  "picked": "Vex",
  "enemies": ["Ahri", "Ornn", "Vi", "Jinx", "Lulu"],
  "mid": "Ahri",
  "result": null,
  "engine": "2.2",
  "origin": "draft",
  "app": "2.16.2"
}
```

À égalité (Cho'Gath, Master Yi et Viego ex æquo, Master Yi choisi) : `"suggested": "Master Yi"`, `"tied": true`, `"tiedWith": ["Cho'Gath", "Master Yi", "Viego"]`, `"recommended": "Cho'Gath"`.

## Anciennes entrées

Une entrée sans `origin` vient d'une version antérieure : origine inconnue. Elle peut venir d'une vraie draft ou d'une simulation (avant la 3.0, la simulation écrivait dans le journal). Sans `tied`, on ne sait pas si le conseil était une égalité. Rien n'est reconstruit, rien n'est migré : ces entrées restent telles quelles.

## Compatibilité

La 2.16.2 ne lit que `id`, `date`, `picked`, `suggested`, `result`, `mid` et `enemies`. Elle ignore les autres champs et les garde quand elle enregistre un résultat (`Object.assign`, puis `JSON.stringify` de la liste). Vérifié sur la vraie page v2.16.2 : quatre entrées affichées, « Conseil suivi » et « Autre choix » comptés comme avant, résultat saisi sans perte de champ, aucune erreur.
