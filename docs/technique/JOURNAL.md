# Journal des drafts

Le journal garde les drafts réelles où le joueur a dit quel champion il jouait, et leur résultat. Il sert à Mes statistiques (« Conseil suivi », victoires par champion).

## Où et quand

- **Stockage :** `localStorage`, clé `draftJournal`, du navigateur intégré (hôte `draftmid.local`). C'est un tableau JSON d'entrées. Export : « Télécharger le journal ». Import : Mes statistiques, sans rien supprimer.
- **Création :** à la première **confirmation** d'une vraie draft : « Je joue X » (voir ci-dessous). Consulter un champion n'écrit rien.
- **Mise à jour :** à chaque nouvelle confirmation dans la même draft, à chaque action sur la composition (`enemies`, `mid`), puis au résultat.
- **Issue de la sélection** (beta.2) : `gameStarted` si la partie a démarré, `abandoned` si la draft a été annulée. Voir plus bas.
- **Journal automatique** (beta.2) : avec la lecture du client en draft classique, ton pick est lu dans League et noté au lancement de la partie, sans « Je joue ». Voir plus bas.
- **Simulation :** rien n'est écrit. Une draft simulée n'a pas de résultat.
- **Aucune déduction :** le champion choisi vient d'un clic du joueur (« Je joue ») ou de son verrouillage lu dans le client (journal automatique), jamais de l'historique Riot.

## Consulter et confirmer

Depuis le correctif de la 3.0.0-beta.1 (test réel de la candidate 4), **consulter** et **confirmer** sont deux actions distinctes. C'est un changement par rapport à la #33, où un clic dans « Ton pool » ou sur une égalité notait directement le champion.

| Action | Avant (#33) | Après (correctif beta.1) |
|---|---|---|
| Clic sur une carte de « Ton pool » (fenêtre) | Note le champion (`picked`), coche | **Consulte** : contour pointillé, carte « Consulté » avec ses raisons, son risque, son matchup et sa comparaison avec le conseil ; le plan de jeu le suit. Rien n'est noté |
| Clic sur une case d'égalité (fenêtre, panneaux) | Note le champion | **Consulte**, puis « Je joue X » apparaît |
| Clic sur l'alternative du panneau (« Alternative Vex ») | Ouvrait Comparer, ne choisissait rien | **Consulte** Vex |
| Ligne de la liste du pool, dans les panneaux (nouvelle, repliée) | — | **Consulte** |
| « Comparer » (panneau) | Ouvre la comparaison | Ouvre la comparaison, **ne choisit jamais** |
| Vignettes du haut de la vue Comparer | Changent la paire comparée | Inchangé : ne choisissent rien |
| « Je joue X » (bouton principal, fenêtre, barre du bas, panneaux) | Note le conseil | **Confirme** le champion consulté, sinon le conseil. **Seule écriture de `picked`** ; jamais rien dans League |
| « Je joue X » de la vue Comparer (nouveau) | — | **Confirme** ce champion |
| « Revenir au conseil » | Note le conseil à la place du choix | Pendant une consultation : l'**annule** (rien n'était noté). Après la confirmation d'un autre champion : **confirme** le conseil à la place |
| « X noté » (bouton, après confirmation) | Aucun effet | Inchangé |
| Victoire, Défaite, Ignorer | Écrivent `result` | Inchangé |

Garanties inchangées : le classement, le conseil et les scores ne bougent pas, ni pendant une consultation ni après une confirmation. Le conseil reste identifiable : « Conseil : Twisted Fate » dans la fenêtre et sous « Je joue X » ; dans un panneau de moins de 540 px de haut, l'illustration du conseil, juste au-dessus, en tient lieu et la barre ne garde que « Revenir au conseil ». Le bouton dit toujours quel champion il confirme, il ne remplace donc jamais un choix sans le nommer. En simulation, rien n'est écrit. Consultation et confirmation sont partagées entre la fenêtre et les panneaux, dans les deux sens, et ne survivent pas à la draft suivante.

## Champs

| Champ | Depuis | Sens |
|---|---|---|
| `id` | 2.x | Horodatage de création (ms), unique |
| `date` | 2.x | Date de création, ISO 8601 |
| `picked` | 2.x | Champion **confirmé** par le joueur avec « Je joue X » (identifiant de pick, par exemple `TF`) |
| `suggested` | 2.x | Conseil au moment de ce choix. À égalité, l'ex æquo choisi, s'il fait partie du groupe |
| `close` | 2.x | Conseil serré au moment du choix |
| `enemies`, `mid` | 2.x | Composition adverse et vis-à-vis supposé, tenus à jour pendant la draft |
| `result` | 2.x | `null` (en attente), `"win"`, `"loss"` ou `"skip"` (ignorée) |
| `engine` | 2.x | Version du moteur |
| `tied` | 3.0 | `true` si le conseil était une égalité au moment du choix |
| `tiedWith` | 3.0 | Groupe complet des ex æquo à ce moment (vide sans égalité) |
| `recommended` | 3.0 | Premier du classement du moteur. Il peut différer du nom affiché, maintenu pendant la draft pour ne pas clignoter |
| `origin` | 3.0 | `"draft"` : vraie draft, détectée ou saisie à la main hors simulation, choix par « Je joue ». `"lcu"` (beta.2) : voir plus bas |
| `app` | 3.0 | Version de DRAFT qui a créé l'entrée (`APP_VERSION`) |
| `selection` | 3.0 beta.2 | Clé de la sélection donnée par le programme : horodatage de son début (ms). Absente hors sélection détectée |
| `gameStarted` | 3.0 beta.2 | `true` : la partie a démarré après cette sélection. Le résultat reste à saisir |
| `abandoned` | 3.0 beta.2 | `true` : la draft a été annulée (lobby, file, client fermé). Gardée, jamais comptée |
| `origin: "lcu"` | 3.0 beta.2 | Pick lu dans le client et partie lancée : entrée du journal automatique |
| `lockedFirst` | 3.0 beta.2 | Premier champion verrouillé, seulement s'il diffère de `picked` (échange) |
| `traded` | 3.0 beta.2 | `true` si un échange a changé le champion final |
| `gameId` | 3.0 beta.2 | Identifiant de la partie lu dans la session, s'il est présent. Clé anti-doublon avec `selection` |

`suggested`, `close`, `tied`, `tiedWith` et `recommended` décrivent le moment du choix : ils changent avec un nouveau choix dans la même draft, pas quand le conseil bouge seul. `origin` et `app` sont posés à la création.

**Conseil suivi :** `picked === suggested`. À égalité, choisir un ex æquo du groupe compte comme conseil suivi ; un champion hors du groupe compte comme autre choix.

## Issue de la sélection (beta.2)

**Défaut corrigé.** Jusqu'à la beta.1, le programme envoyait le même `draftEnd` pour une partie lancée et pour une draft annulée (dodge, retour au lobby). Une entrée notée avec « Je joue » puis annulée restait `result: null`, et la page la proposait comme « Dernière partie ».

**Programme** (`DraftOutcomeTracker`, `src/DraftDetection.cs`) :
- À la sortie de draft, l'issue est en attente : `{ kind:"draftEnd", draft, outcome:"pending" }`. Rien n'est écrit.
- Elle devient `"game"` quand la partie est observée : lecture du client (`GameStart`, `InProgress`, `Reconnect`) ou processus de jeu.
- Elle devient `"abandoned"` quand le client revient hors draft sans partie :
  - lobby, file, `ReadyCheck` ou `None` lus dans le client : tout de suite ;
  - image hors draft ou client fermé : après 20 s sans partie (`ConfirmSeconds`), le temps que la partie démarre.
- `Uncertain` ne tranche rien, et ce temps ne compte pas.
- Une nouvelle sélection sans partie entre les deux clôt la précédente : annulée.
- Message : `{ kind:"draftOutcome", draft, outcome }`, une seule fois. Une issue définitive ne change plus.
- La clé et l'issue sont aussi dans l'état envoyé aux pages (`draftKey`, `draftOutcome`) : une page ouverte après la décision la retrouve.
- **En mémoire seulement.** DRAFT fermé ou redémarré avant la décision : rien n'est déduit, l'issue reste inconnue.

**Page** (`applyDraftOutcome`, `web/index.html`) :
- « Je joue » pendant la sélection, ou juste après sa fin, pose `selection` sur l'entrée.
- « Je joue » ne modifie que l'entrée de la sélection affichée (même `selection`, ou aucune clé hors sélection du programme). Une entrée d'une autre sélection n'est jamais réutilisée : une page restée sur la draft d'avant crée une nouvelle entrée (défaut réel de la candidate 4, voir SYNCHRONISATION.md).
- L'issue n'est notée que dans l'entrée de cette clé, une seule fois : `gameStarted: true` ou `abandoned: true`. Rien n'est créé.
- Plusieurs pages (fenêtre, panneaux) reçoivent l'issue : elles écrivent la même chose. Messages en double, en retard ou contradictoires : sans effet.
- `result` reste `null` tant que le joueur ne l'a pas saisi. Aucun résultat n'est inventé.

| Entrée | Statut affiché | « Dernière partie » | Mes statistiques |
|---|---|---|---|
| `abandoned: true` | Annulée ; dans la draft : « Draft annulée : pas de partie à noter. », sans boutons de résultat | Jamais | Pas comptée (drafts, victoires, conseil suivi) |
| `gameStarted: true`, sans résultat | En attente | Oui | Comptée comme draft, pas encore comme partie terminée |
| Ni l'un ni l'autre, sans résultat | À confirmer (issue inconnue) | Jamais | Comptée comme draft, jamais comme partie jouée d'office |

Les anciennes entrées n'ont ni `selection` ni issue : elles sont « À confirmer » et ne sont pas réécrites. Leur résultat se saisit encore tant que la draft est affichée (entrée en cours).

**Tests :** selftest (`DraftOutcomeSelfTest` : partie, lobby, client fermé, image puis partie, Uncertain, redémarrage, deux sélections, lectures répétées), étape e2e « Beta.2. Issue de la sélection » (LCU simulé, vraie fenêtre WebView2), page `web/draft-abandon-tests.html` (FR et EN).

**Limites :**
- Sans aucune détection de draft (affichage manuel seul), l'entrée n'a pas de clé : son issue reste inconnue.
- « Je joue » après une issue définitive : l'entrée n'a pas de clé, son issue reste inconnue.
- Une draft de plus de 8 minutes en détection par l'image redevient incertaine ; si elle est détectée de nouveau, c'est une nouvelle sélection et la précédente est annulée. La page repartait déjà de zéro dans ce cas.

## Journal automatique (beta.2)

**Lecture** (`LcuParser.ParseLocalPick`, `src/LcuConnector.cs`), dans la session déjà lue, GET, mêmes deux adresses :
- ton action de pick (`actorCellId == localPlayerCellId`, `type: "pick"`) : survolée si `isInProgress` et pas `completed`, verrouillée si `completed` ;
- ta case de `myTeam` : `championPickIntent` (présélection), `championId` (final, après échange), `assignedPosition` ;
- `timer.phase` (`FINALIZATION`, `GAME_STARTING` : final) et `gameId`.

Les autres cases de `myTeam` ne sont pas ouvertes : seule leur `cellId` est comparée. Aucune donnée de coéquipier, aucun nom, aucun identifiant de joueur (test négatif dans le selftest).

**Mode pris en charge :** draft classique, c'est-à-dire une action de pick pour toi ET un poste attribué. Les autres modes (ARAM, sélection aveugle, sans action de pick) gardent « Je joue ». Pour en ajouter un, il suffit d'élargir `Supported`.

**État « Dans League »** (`LeaguePickTracker`) : `{ v, key, supported, readable, lost, state, champion, lockedFirst, traded, gameId }`, dans l'état envoyé aux pages (`league`). `state` vaut `none`, `hover`, `locked` ou `final`. `v` augmente à chaque changement ; une page ignore un état plus ancien (clé plus petite, ou même clé et `v` plus petit). Comme le poste attribué, il vient toujours du programme, jamais d'une autre page.

**Affichage** (panneaux et fenêtre, à part du champion consulté) : « Dans League : Vex (survolé) », puis « Dans League : Vex verrouillé ✓ » (« …, après échange » si le champion final diffère). Consulter un champion dans DRAFT ne change pas cette ligne, et un survol dans League ne change ni le champion consulté, ni le classement.

**« Je joue »** :

| Cas | « Je joue » | Ligne |
|---|---|---|
| Lecture opérationnelle, draft classique | Absent | « Ton pick sera noté automatiquement au lancement de la partie. » |
| Lecture désactivée ou indisponible | Présent | Aucune |
| Lecture perdue en pleine draft | Présent | « Lecture du client perdue : confirme ton pick avec « Je joue ». » |
| Mode non pris en charge | Présent | « Ce mode n'est pas noté automatiquement… » |
| Simulateur | Présent, sans écriture | Aucune |

**Écriture** (`writeLeagueEntry`, `web/index.html`), seulement à l'issue `"game"` de la sélection, si un verrouillage a été lu, en draft classique, lecture non perdue :
- UNE entrée : `origin: "lcu"`, `picked` = champion final, `lockedFirst` s'il diffère, `traded`, `gameStarted: true`, `result: null`, `selection`, `gameId` s'il est présent ;
- `suggested`, `close`, `tied`, `tiedWith`, `recommended` : conseil mémorisé au premier verrouillage vu (`draftLockMemo`, une fois par sélection, jamais en simulation) ;
- une entrée « Je joue » de la même sélection (même `selection`, sinon même `gameId`) est COMPLÉTÉE, jamais doublée : même `id`, même date ;
- verrouillage puis draft annulée : rien n'est écrit, la page dit « Draft annulée : pas de partie à noter. » ;
- démarrage non observé (DRAFT fermé ou redémarré pendant le chargement) : rien n'est écrit, jamais joué d'office.

**Tests :** selftest (lecture : survol, présélection, verrouillage, échange, aucun, mode à banc, sans poste, sans action, joueur absent, réponse tronquée, test négatif des coéquipiers ; suivi « Dans League »), étape e2e « Beta.2. Journal automatique » (LCU simulé au format réel, vraies pages WebView2), page `web/draft-journal-auto-tests.html` (FR et EN). Captures WebView2 réelles, avant (PR A) et après : `docs/qa/captures-journal-auto/`, à 125 % (taille de la page en pixels CSS dans le nom du fichier).

**Limites :**
- Champs non documentés par Riot, vérifiés seulement sur des sessions simulées au format réel. Un essai sur un vrai client reste à faire (sans dodge volontaire).
- Lecture perdue au lancement de la partie : rien d'automatique ; seule une entrée « Je joue » suit la règle de l'issue.
- Un échange fait après une perte de lecture n'est pas vu.
- Plusieurs pages peuvent écrire en même temps : le journal étant une seule valeur, il ne peut pas y avoir deux entrées, mais une autre modification simultanée pourrait être perdue.

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

**Beta.2 (`selection`, `gameStarted`, `abandoned`) :** mêmes garanties. Vérifié dans le code de v2.16.2 (`updateEntry` : `Object.assign` puis `saveJournal` ; import : `concat` des entrées entières), puis sur sa vraie page : trois entrées lues, résultat saisi sans perte de champ, aucune erreur. Limite : la 2.16.2 ne connaît pas `abandoned`. Relue par elle, une entrée annulée et sans résultat peut encore être proposée comme « Dernière partie » (sa règle : `result === null`).
