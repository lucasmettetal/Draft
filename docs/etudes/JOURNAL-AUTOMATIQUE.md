# Étude : journal automatique au verrouillage, sans « Je joue » en draft réelle

**Statut :** étude seulement, rien n'est implémenté. La beta.1 publiée (`v3.0.0-beta.1`, main `4fb621f`) et le moteur ne sont pas modifiés.

## 1. Ce que DRAFT fait aujourd'hui (audit du code, main 4fb621f)

**Lecture du client (LCU).** GET seulement, sur deux adresses autorisées (`LcuEndpoints.IsAllowed`) :
- `/lol-gameflow/v1/gameflow-phase` : la phase ;
- `/lol-champ-select/v1/session` : la sélection.

Dans la session, le programme lit **seulement** :
- les champions adverses (`theirTeam[].championId`) ;
- le poste du joueur (`myTeam[cellId == localPlayerCellId].assignedPosition`).

Par choix, il ne lit ni nom, ni identifiant de joueur, ni **intention de pick**, ni sort (commentaire de `LcuParser`).

**Phases.** `MapPhase` :
- `ChampSelect` → Draft ;
- `GameStart`, `InProgress`, `Reconnect` → InGame ;
- `Lobby`, `Matchmaking`, `ReadyCheck`, `None` et les phases de fin de partie → hors draft.

**Journal.** Une entrée n'est créée que par « Je joue X » (`choosePick`). Champs : `suggested`, `picked`, `tied`, `tiedWith`, `recommended`, `origin: "draft"`, `app`, `result` (saisi à la main).

**Défaut actuel : une draft abandonnée peut être comptée.**
- Le programme envoie le même message `draftEnd` dès que la phase quitte la draft (`DraftMid.cs`, transition `before == Phase.Draft`). Il le fait **que la partie démarre ou que la draft soit annulée** (dodge, retour au lobby).
- Une entrée notée avec « Je joue » puis abandonnée reste `result: null`. La page la propose ensuite comme « Dernière partie » à compléter.
- Rien ne la distingue d'une partie réellement lancée.

## 2. Données accessibles sans nouvelle adresse LCU

Tout ce qui suit est dans la session déjà lue (`/lol-champ-select/v1/session`). Ces champs ne sont pas documentés par Riot : leur forme est à confirmer sur un vrai client avant implémentation.

| Besoin | Champ | Fiabilité | Remarque |
|---|---|---|---|
| Champion survolé pendant mon tour | `actions[][]` : action `type: "pick"`, `actorCellId == localPlayerCellId`, `isInProgress: true`, `completed: false`, `championId` | Bonne | Change à chaque survol |
| Présélection avant mon tour | `myTeam[local].championPickIntent` | Moyenne | Absente dans certains modes |
| Verrouillage confirmé | même action, `completed: true` | Bonne | Le signal clé |
| Champion final après échange | `myTeam[local].championId` en fin de sélection (`timer.phase` = `FINALIZATION` puis `GAME_STARTING`) | Bonne | Couvre les échanges acceptés |
| Échange proposé ou reçu | `trades[]` (`state` : `AVAILABLE`, `SENT`, `RECEIVED`, `BUSY`) | Moyenne | Inutile au journal : seul le champion final compte |
| Partie réellement lancée | phase `GameStart` / `InProgress` juste après `ChampSelect` | Très bonne | Distingue une partie d'une draft abandonnée |
| Draft abandonnée | phase revenant à `Lobby`, `Matchmaking`, `ReadyCheck` ou `None` sans passer par `InProgress` | Très bonne | Dodge, file relancée |
| Identifiant de la partie | `gameId` de la session, si présent | À vérifier | Permettrait plus tard de relier l'entrée à l'historique Riot importé |
| Résultat (victoire, défaite) | non disponible sur ces deux adresses | — | Reste manuel, ou rapprochement ultérieur avec l'historique Riot importé |

**Règle de confidentialité proposée.** On lit **uniquement la case du joueur local** : son action de pick, son intention, son champion. Jamais les intentions, noms ou identifiants des coéquipiers. Toujours GET, mêmes deux adresses, aucune action dans League.

## 3. Comportement proposé

### 3.1 Deux sources indépendantes
- **Consulté dans DRAFT** : clic dans les panneaux ou la fenêtre. Ça ne change ni League ni le journal.
- **Dans League** : survolé, verrouillé, final. Lu dans le client, affiché à part.

Les deux ne se confondent jamais. Exemple de panneau : « Dans League : Vex, verrouillé ✓ », sous le conseil, et la carte « Consulté : Zed » reste indépendante.

### 3.2 Avec une lecture du client opérationnelle (draft réelle)
- **Pas de bouton « Je joue ».** À sa place, une ligne d'état : « Ton pick sera noté automatiquement au verrouillage. »
- **Survol ou présélection dans League :** « Dans League : Vex (survolé) ». Le classement ne change pas, et rien n'est noté.
- **Verrouillage :** « Dans League : Vex, verrouillé ✓ ». Le conseil du moment est mémorisé, en mémoire seulement : `suggested`, `tied`, `tiedWith`, `recommended`.
- **Échange accepté :** le champion final remplace le champion verrouillé, et on garde « échangé : oui ».
- **Partie lancée** (`GameStart` / `InProgress`) : **une seule** entrée de journal est écrite :
  - `picked` = champion final ;
  - `origin: "lcu"` ;
  - `lockedFirst` = premier verrouillage, s'il diffère ;
  - `traded` ;
  - `gameId`, si disponible ;
  - `result: null`.
- **Draft abandonnée** (retour au lobby sans `InProgress`) : **rien n'est écrit**. Le panneau affiche « Draft annulée : rien n'est noté ».
- **Clé anti-doublon :** une entrée par sélection (`gameId`, sinon l'horodatage du début de sélection), mise à jour au lieu d'être recréée.

### 3.3 Repli sur « Je joue »
« Je joue » réapparaît, avec la règle actuelle `origin: "draft"`, dans ces cas :
- lecture du client désactivée, client fermé ou injoignable ;
- mode sans action de pick lisible ;
- **perte de connexion pendant la draft** : le panneau dit « Lecture du client perdue : confirme ton pick avec Je joue » ;
- Simulateur, où rien n'est jamais écrit au journal (règle de la #33).

Si la lecture revient et voit un verrouillage, l'entrée « Je joue » de la même sélection est **mise à jour**, pas doublée.

### 3.4 Cas particuliers
| Cas | Comportement proposé |
|---|---|
| DRAFT lancé après le verrouillage | Le verrouillage est lu dans la session : même suite |
| DRAFT fermé avant le lancement de la partie | Rien n'est écrit, car il ne voit pas `InProgress`. C'est acceptable : pas de faux positif |
| Draft relancée (dodge puis nouvelle file) | Nouvelle sélection, nouvelle clé ; l'ancienne n'a rien écrit |
| ARAM et modes à banc | Pas d'action de pick : aucune écriture auto (option : ne journaliser que les modes à postes) |
| Partie personnalisée ou entraînement | À exclure si `isCustomGame` est présent (à vérifier) |
| Résultat | Manuel, comme aujourd'hui ; plus tard, rapprochement facultatif avec l'historique Riot importé (même champion, `gameId` ou heure proche), toujours confirmé par le joueur |

### 3.5 Conséquences sur le journal et Mes statistiques
- **Nouvelle origine** `origin: "lcu"` : partie confirmée par la lecture du client. Elle compte comme « Partie confirmée », sans classement à faire.
- **Plus de draft abandonnée** comptée comme jouée.
- **« Conseil suivi » :** `picked` final === `suggested` au verrouillage (ou dans le groupe d'égalité). Si un échange a modifié le champion, on l'indique et on pourra l'exclure des comparaisons.
- **Entrées existantes :** rien n'est migré ni réécrit.
- **Compatibilité 2.16.2 :** les champs en plus sont ignorés, comme pour `origin` et `tied`.

## 4. Tests proposés (avant implémentation)

**Analyse de la session** (selftest, sessions au format réel, valeurs fictives) :
- survol pendant le tour ;
- intention avant le tour ;
- verrouillage ;
- échange accepté ;
- banc (ARAM) ;
- joueur absent de son équipe ;
- réponse tronquée ;
- **aucune donnée de coéquipier lue** (test négatif).

**Cycle de vie** (machine d'état, séquences simulées) :
1. verrouillage → `InProgress` : **1** entrée, `origin: "lcu"` ;
2. verrouillage → `Lobby` (dodge) : **0** entrée, « Draft annulée » ;
3. verrouillage → échange → `InProgress` : champion échangé, `traded: true` ;
4. DRAFT démarré après le verrouillage → `InProgress` : 1 entrée ;
5. lecture perdue avant verrouillage → « Je joue » affiché → reconnexion → verrouillage : **1** entrée, mise à jour et non doublée ;
6. deux drafts de suite : 2 entrées distinctes ;
7. lectures en double ou dans le désordre : aucune entrée en double ;
8. Simulateur : 0 entrée.

**Interface** (pages et vrais panneaux WebView2, FR et EN, 1366, 1920 et 125 %) :
- pas de « Je joue » quand la lecture fonctionne ;
- « Je joue » présent en repli et dans le Simulateur ;
- consultation indépendante du survol League ;
- ligne « Dans League » visible sans défilement.

**Non-régression** :
- banc du moteur à 0 différence ;
- mêmes deux adresses LCU, GET seulement ;
- CSP inchangée ;
- relecture du journal par la 2.16.2.

**Essai réel indispensable :** une vraie draft (survol, verrouillage, échange si possible, dodge volontaire en partie normale) pour confirmer la forme réelle des champs avant de fusionner.

## 5. Risques et décisions à prendre
- **Champs non documentés.** Riot peut les changer : on garde le repli « Je joue » et un diagnostic qui écrit l'état lu (survolé, verrouillé, final), sans nom.
- **Politique de confidentialité :** lire l'intention et le pick **du joueur local seulement** change une règle écrite du code. À valider.
- **Écriture au lancement de la partie seulement.** C'est plus sûr (aucune draft abandonnée), mais rien n'est noté si DRAFT est fermé à ce moment. À valider.
- **Modes à exclure :** ARAM, personnalisées, entraînement. À valider.
- **Résultat automatique :** hors périmètre ; plus tard, éventuellement, par rapprochement avec l'historique Riot importé.

## 6. Découpage d'implémentation (après validation)
1. Lecture du pick du joueur local, plus diagnostic, plus tests d'analyse.
2. Machine de cycle : verrouillage, échange, lancement, abandon. Écriture unique `origin: "lcu"`, tests de cycle.
3. Panneaux : ligne « Dans League », « Je joue » masqué quand la lecture fonctionne, repli, tests UI et captures.
4. Correctif indépendant, utile dès maintenant : ne plus proposer comme « Dernière partie » une entrée dont la draft a été abandonnée (il faut distinguer `draftEnd` vers la partie et vers le lobby).

## 7. Suite validée : résultat de la partie (après les PR #39 et #40 et leur essai réel)

**Décision de l'utilisateur (19/11/2026) :** « fais ce qui te paraît cohérent ». La clé de production Riot, avec son serveur, viendra **après la validation du projet par Riot**, comme chantier séparé.

### 7.1 Résultat lu dans le client, à la fin de la partie (PR C, empilée sur #40)

**Fait (beta.3, branche `feat/resultat-auto`).** Implémenté comme décrit ci-dessous, avec deux précisions : l'indicateur de remake lu est `gameEndedInEarlySurrender` (et `invalid`) ; **ton propre K/D/A n'est pas lu** (seul le résultat compte ici ; les détails viendront de l'historique Riot, 7.2). `resultSource : "manual"` est posé quand le joueur clique. Détails : `docs/technique/LCU.md` (« Troisième adresse ») et `docs/technique/JOURNAL.md` (« Résultat lu dans le client »).
- **Une troisième adresse LCU**, en lecture seule (GET) : le bilan de fin de partie (`/lol-end-of-game/v1/eog-stats-block`). C'est un **changement de la règle « deux adresses »**, à signaler explicitement dans la PR.
- **On lit seulement :**
  - l'identifiant de partie ;
  - **l'équipe du joueur local** et l'équipe gagnante (`localPlayer.teamId`, `teams[].teamId` et `isWinningTeam`) ;
  - éventuellement **son propre** K/D/A.
- **Jamais** la liste ni les statistiques des autres joueurs, qui ne sont ni lues dans le code, ni stockées, ni journalisées. Un test le prouve.
- **Écriture :** `result: "win" | "loss"` sur l'entrée **de la même partie** (même `gameId`, sinon même clé de sélection), avec `resultSource: "lcu"`.
  - Un résultat déjà saisi à la main n'est jamais écrasé.
  - Le joueur peut toujours corriger.
- **Pas de bilan lu** (DRAFT fermé à la fin, partie interrompue, remake, champs absents) : le résultat reste « en attente ». **Aucun résultat inventé.**
- Les champs ne sont pas documentés par Riot. On les lit au format réel, on garde un repli manuel, et le diagnostic indique `bilan=lu|absent|illisible`, sans nom.

### 7.2 Rapprochement avec l'historique Riot importé (PR D, sans nouvelle adresse LCU)
- **Lien :** entrée du journal (`gameId` lu dans le client) ↔ partie de l'historique (`id` = `<plateforme>_<gameId>`, par exemple `EUW1_…`).
- **Après un import :** l'entrée reçoit les détails officiels (K/D/A, durée, patch, adversaire de voie quand il est connu) et `resultSource: "riot"`.
- **En cas de désaccord** entre le client et Riot, **Riot fait foi**. Le désaccord est signalé, jamais masqué.
- **Sans `gameId`** (anciennes entrées), aucun rapprochement approximatif par heure ou champion sans confirmation du joueur.

### 7.3 Plus tard : clé de production
Elle sera conservée **sur un serveur**, jamais dans l'application distribuée. Le rapprochement 7.2 pourra alors devenir automatique.

### 7.4 Tests prévus
- **Bilan au format réel :** victoire, défaite, remake, bilan absent ou tronqué ; aucune donnée des autres joueurs lue.
- **Écriture du résultat :**
  - une seule fois ;
  - jamais sur une autre partie ;
  - jamais par-dessus une saisie manuelle ;
  - DRAFT fermé avant la fin : rien ;
  - désordre et doublons.
- **Rapprochement :** `gameId` présent ou absent ; désaccord client / Riot ; plusieurs comptes.
