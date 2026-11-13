# Mes statistiques : règles de calcul

Toutes les règles ci-dessous sont dans un seul module pur, [`web/draft-stats-calc.js`](../../web/draft-stats-calc.js) : aucun DOM, aucun texte affiché, aucune écriture. Tests unitaires : [`web/draft-stats-tests.html`](../../web/draft-stats-tests.html).
Le rendu (`web/draft-stats-view.js`) lit les résultats du module et ne recalcule rien.
Chaîne prévue pour le futur coaching : **sources → calcul → règles → rendu**. Cette PR s'arrête au calcul et au rendu : aucune règle de conclusion automatique n'est branchée.

Étude de référence (branche `etude/stats-bplus`) : `docs/stats/AUDIT-HISTORIQUE-ET-JOURNAL.md`, `COACHING-ARCHITECTURE.md`.

## 1. Sources

| Source | Lue depuis | Écrite par | Ce module |
|---|---|---|---|
| Historique Riot | `window.DRAFT_HISTORY` (fichier `draft-history.js` du dossier de données) | l'import de l'application (`src/HistoryImporter.cs`) | lecture seule |
| Ancien import | `window.DRAFT_STATS` (`draft-stats.js`) | l'ancien script `tools/draft-import-stats.ps1` | lecture seule, seulement si `DRAFT_HISTORY` est absent |
| Journal DRAFT | `localStorage.draftJournal` (`loadJournal()`) | le joueur (« Je joue », puis le résultat) | lecture seule |

- Les deux formats d'historique ont la même ligne (vérifié dans `HistoryImporter.ConvertMatch` et `Convert-Match` du script) : `id`, `acct`, `time`, `patch`, `sec`, `champ`, `pos`, `win`, `k`, `d`, `a`, `mid`, `enemies`.
- Historique et journal ne sont **jamais** additionnés, ni rapprochés : aucune clé ne les relie (le journal ne garde pas le `matchId`).
- Aucun fichier n'est modifié. Les corrections ci-dessous (doublons, millisecondes, lignes incomplètes) n'existent que dans le calcul.

## 2. Lecture d'une ligne d'historique

Chaque champ est vérifié séparément. Une valeur invalide n'écarte la ligne que des mesures qui en ont besoin, et chaque écart est compté pour être affiché.

| Champ | Valide si | Sinon |
|---|---|---|
| ligne | objet | ligne illisible, ignorée, comptée |
| `id` | chaîne non vide | ligne gardée, comptée « sans identifiant » (impossible de repérer un doublon) |
| `acct` | chaîne | compte vide `""` |
| `win` | booléen `true` ou `false` | **écartée de toutes les mesures** (sans résultat, une partie ne compte ni en V/D ni en winrate), comptée |
| `sec` | nombre > 0 | écartée de la durée moyenne, comptée |
| `time` | nombre > 10¹¹ (millisecondes) | écartée de la période et de la chronologie, comptée |
| `patch` | `M.m` (ex. « 16.22 ») | écartée des patchs, comptée |
| `champ` | chaîne non vide | regroupée sous « champion inconnu » |
| `pos` | `TOP`, `JUNGLE`, `MIDDLE`, `BOTTOM`, `UTILITY` | **rôle inconnu**, affiché comme tel |
| `k`, `d`, `a` | trois nombres ≥ 0 | écartée du KDA, comptée |
| `mid` | chaîne non vide | pas de vis-à-vis pour cette partie |
| `enemies` | tableau | ignoré (aucune mesure ne l'utilise ici) |

## 3. Doublons

- Clé : `acct + "|" + id`. Une partie présente deux fois n'est comptée qu'**une** fois : la première lue.
- Une ligne sans `id` ne peut pas être dédoublonnée : elle est gardée et comptée à part.
- Le nombre de doublons ignorés est affiché.

## 4. Durées et remakes

- D'après la documentation de match-v5, `gameDuration` est en **millisecondes** avant le patch 11.20 (octobre 2021) et en **secondes** ensuite. Le champ `gameEndTimestamp` permet de trancher, mais l'import ne le garde pas. La documentation n'a pas pu être relue ici (réseau limité à Data Dragon) : à revérifier en ligne.
- L'import accepte des parties depuis le 16/06/2021 (`OldestSupported`) : quelques mois peuvent donc avoir des durées en millisecondes.
- Règle du calcul : une durée **≥ 20 000** est lue comme des millisecondes et divisée par 1 000. 20 000 s = 5 h 33 : aucune partie réelle n'en approche, et une partie en millisecondes vaut au moins 300 000 (5 min). Le nombre de durées converties est affiché.
- Après conversion, une partie de **moins de 300 s** est un remake : elle est écartée de toutes les mesures, comme l'import le fait déjà pour les parties récentes (`MinDurationSec`). Comptée.
- Durée moyenne = moyenne des durées valides des parties comptées, en minutes arrondies.

## 5. Parties comptées et winrate

- **Partie comptée** = ligne lisible, non doublon, avec un `win` valide, et qui n'est pas un remake.
- Parties = nombre de parties comptées. V = `win === true`, D = `win === false`.
- **Winrate = V / parties comptées.** Le dénominateur est toujours écrit à côté (« sur 93 parties »).
- **Aucun pourcentage sous 10 parties** (`MIN_GAMES` de l'application) : on montre les V/D, le nombre de parties et une jauge de 10 crans. Ce seuil vaut pour le total, chaque champion, chaque rôle, chaque tranche et chaque adversaire.
- Aucune étiquette « favorable », « défavorable », « point fort » ou « en progrès » : ces conclusions demandent les seuils de l'étude (30 parties, intervalle de Wilson, 40 parties et z ≥ 2) et ne sont pas développées ici.

## 6. KDA

- KDA = (ΣK + ΣA) / **max(1, ΣD)**, sur la **somme** des parties (pas la moyenne des KDA par partie, qui exploserait sur une partie sans mort). Même règle que `contextKda` dans `draft-mastery.js`.
- **ΣD = 0** (aucune mort sur l'ensemble) : le ratio vaut ΣK + ΣA, et la valeur est marquée « sans mort » à l'affichage.
- Moyennes K, D, A par partie : sommes / parties avec KDA valide.
- Parties sans KDA valide : écartées du KDA seulement, comptées.

## 7. Période, comptes, filtres

- Période = première et dernière `time` valides des parties comptées.
- Tous les comptes sont additionnés ; leur nombre est affiché. Pas de filtre de date dans cette version.
- Filtres des vis-à-vis : champion joué et rôle (voir § 11). Ils ne changent que cette section.

## 8. Champions

- Nom affiché = nom de l'API Riot traduit par le résolveur de l'application (`toName` dans `index.html`, données `draft-champions.js`) : `KogMaw` → Kog'Maw, `MonkeyKing` → Wukong, `Renata` → Renata Glasc, `Leblanc` → LeBlanc. Testé sur les 173 identifiants de `draft-champion-ids.js`.
- Regroupement par nom affiché : `KogMaw` (import) et `Kog'Maw` (autre source) font un seul champion.
- Rôle principal d'un champion = rôle connu le plus joué avec lui (égalité : ordre Top, Jungle, Mid, ADC, Support).
- Tri : nombre de parties décroissant, puis nom. Le volume dit l'expérience, pas le niveau.

## 9. Rôles

- Cinq rôles : Top (`TOP`), Jungle (`JUNGLE`), Mid (`MIDDLE`), ADC (`BOTTOM`), Support (`UTILITY`), plus **rôle inconnu** (vide, absent ou autre valeur).
- Rôle principal du joueur = rôle connu le plus joué. Le rôle inconnu n'est jamais rôle principal.
- Chaque rôle : parties, V/D, winrate à partir de 10 parties.

## 10. Patchs

- Format `M.m`. Tri numérique (16.9 avant 16.10). Nombre de patchs distincts, premier et dernier.
- Un patch n'est pas une durée fixe : aucune comparaison entre patchs n'est tirée.

## 11. Chronologie

- Parties comptées avec une `time` valide, de la plus ancienne à la plus récente.
- Tranches de **20** parties (`EVOLUTION_STEP`), **dernière tranche incomplète incluse** et marquée « en cours » (l'ancien affichage la perdait : 80 parties montrées sur 93).
- Une tranche de moins de 10 parties n'a pas de pourcentage.
- Aucune phrase de progression : la règle de tendance de l'étude (40 parties dans un rôle, z ≥ 2) n'est pas branchée.

## 12. Vis-à-vis (matchups)

- L'adversaire de voie n'est connu que si `pos = MIDDLE` : `mid` est le premier adversaire placé `MIDDLE` par Riot.
- Pour les autres rôles, l'import n'enregistre pas l'adversaire de même poste (`enemies` n'a pas de poste). La page le dit. L'enrichissement de l'import fera l'objet d'une PR séparée.
- Par adversaire : confrontations, V/D, winrate à partir de 10, champions joués contre lui.

## 13. Journal DRAFT

Format du journal : [`docs/technique/JOURNAL.md`](../technique/JOURNAL.md) (PR #33, fusionnée). Les marques sont lues **sans dépendre de son code** : un champ absent est traité comme inconnu. `recommended` (premier du classement) n'entre dans aucune mesure ici.

| Champ | Écrit par | Sens |
|---|---|---|
| `picked` | « Je joue » | champion cliqué par le joueur |
| `suggested` | « Je joue » | conseil au moment du choix ; à égalité, l'ex æquo choisi |
| `tied`, `tiedWith` | #33, entrées nouvelles | le conseil était une égalité ; groupe complet des ex æquo |
| `origin: "draft"`, `app` | #33, entrées nouvelles | entrée écrite pendant une vraie draft, par cette version |
| `result` | le joueur | `null` (en attente), `win`, `loss` ou `skip` |
| `userOrigin` | **cette PR**, le joueur | `"real"` ou `"simulation"` : choix du joueur pour une entrée d'origine inconnue |

### Origine

- **Draft réelle** : `origin === "draft"`.
- **Confirmée réelle** : pas de marque, et le joueur a choisi « Partie réelle » (`userOrigin: "real"`).
- **Simulation** : pas de marque, et le joueur a choisi « Simulation » (`userOrigin: "simulation"`). L'entrée est gardée, jamais supprimée, et peut être reclassée.
- **Origine inconnue** : aucune marque, aucun choix. Les anciennes versions écrivaient aussi les drafts du Simulateur dans le journal : rien ne permet de les distinguer.
- **Seules les drafts réelles et confirmées réelles entrent dans les statistiques du journal.** Le nombre d'entrées d'origine inconnue et de simulations écartées est toujours affiché.
- Le résultat ne se saisit que pour une entrée réelle ou confirmée réelle : une simulation ne devient jamais une partie.

### Mesures (entrées comptées seulement)

| Mesure | Définition | Dénominateur |
|---|---|---|
| Drafts notées | entrées comptées | — |
| Parties terminées | `result` = `win` ou `loss` | drafts notées |
| En attente | `result` absent ou `null` | drafts notées |
| Ignorées | `result` = `skip` | drafts notées |
| **Conseil suivi** | `picked === suggested`, ou, si `tied === true`, `picked` dans `tiedWith` | **parties terminées**, partout (bandeau, vue d'ensemble, onglet Journal) |
| Victoires avec / sans le conseil | V parmi les parties terminées de chaque groupe | parties terminées du groupe ; % à partir de 10 |

- Égalités : une entrée sans `tied` n'en reconstruit aucune ; seul `picked === suggested` compte.
- L'ancien bandeau comptait « Conseil suivi » sur toutes les drafts (« 0 sur 5 » sans partie terminée), l'onglet Journal sur les terminées : une seule définition maintenant, et un tiret tant qu'aucune partie n'est terminée.
- Le journal n'est jamais assimilé à une partie Riot.

### Seuils de « Pas encore »

La colonne « Pas encore » montre la progression vers les seuils de l'étude (`COACHING-ARCHITECTURE.md`), **sans tirer de conclusion**, même seuil atteint :

| Ligne | Mesure | Seuil |
|---|---|---|
| Points forts et faibles | parties du champion le plus joué | 30 |
| Tendance | parties dans le rôle principal | 40 |
| Vis-à-vis | confrontations avec l'adversaire mid le plus croisé | 10 |
| Conseil suivi ou autre choix | parties terminées du plus petit des deux groupes | 10 |

## 14. Lignes incomplètes : ce qui est affiché

Sous l'échantillon, une ligne n'apparaît que si un compte est non nul : doublons ignorés, lignes illisibles, sans résultat, remakes écartés, durées converties, sans durée, sans date, sans patch, sans KDA, rôle inconnu, sans identifiant.
