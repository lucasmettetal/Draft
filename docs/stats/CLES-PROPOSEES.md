# Clés proposées pour « Mes statistiques »

Textes des maquettes A et B, au format du catalogue ([docs/i18n/FORMAT-CATALOGUE.md](../i18n/FORMAT-CATALOGUE.md)) : clés plates, variables nommées, pluriels `{one, other}`, aucun HTML, une phrase complète par clé.
Source : [maquettes/textes-maquettes.js](maquettes/textes-maquettes.js). Liste générée depuis ce fichier.

- **115 clés nouvelles**, en français et en anglais, mêmes variables dans les deux langues (contrôlé).
- Aucune collision avec `web/i18n/fr.js` (contrôlé ; `stats.journal.title` existait déjà avec un autre texte : la maquette utilise `stats.journal.heading`).
- Les clés `maquette.*` servent au bandeau des maquettes : elles n'entrent pas dans l'application.
- Clés **existantes** reprises telles quelles (ne pas recréer) : `nav.home`, `nav.pool`, `nav.sim`, `nav.stats`, `nav.settings`, `nav.beta`, `nav.state.league`, `nav.state.role`, `nav.state.lcu.off`, `role.name.top`, `role.name.jungle`, `role.name.mid`, `role.name.adc`, `role.name.support`, `stats.history.title`, `stats.history.none`, `stats.history.collect`, `stats.history.import`, `stats.history.more`, `stats.sample.period`, `stats.sample.periodOne`, `stats.record`, `stats.percent`, `stats.gauge.aria`, `stats.range`, `stats.kpi.drafts`, `stats.recent.title`, `stats.overview.emptyJournal`, `journal.result.win`, `journal.result.loss`, `journal.status.pending`, `journal.status.ignored`, `journal.download`.
- Les clés `stats.story.*` ne servent qu'à la proposition B. Les autres servent aux deux.
- Toutes ne seront pas gardées : la liste suit la proposition retenue (voir [RECOMMANDATION.md](RECOMMANDATION.md)).

### `stats.page.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.page.sub` | Deux sources, jamais additionnées : tes parties classées lues chez Riot, et les drafts que tu as notées dans DRAFT. | Two sources, never added together: your ranked games read from Riot, and the drafts you logged in DRAFT. |

### `stats.sources.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.sources.separate` | Ces deux sources ne s'additionnent pas : ce ne sont pas les mêmes parties, et rien ne les relie. | These two sources don't add up: they aren't the same games, and nothing links them. |

### `stats.riot.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.riot.source` | Mesuré par Riot : parties classées solo/duo. | Measured by Riot: ranked solo/duo games. |
| `stats.riot.importedOn` | Import du {date} | Imported {date} |
| `stats.riot.sample.games` | one : {count} partie classée<br>other : {count} parties classées | one : {count} ranked game<br>other : {count} ranked games |
| `stats.riot.sample.patches` | one : {count} patch : {first}<br>other : {count} patchs, de {first} à {last} | one : {count} patch: {first}<br>other : {count} patches, {first} to {last} |
| `stats.riot.sample.accounts` | one : {count} compte<br>other : {count} comptes | one : {count} account<br>other : {count} accounts |
| `stats.riot.sample.queue` | Solo/duo seulement | Solo/duo only |
| `stats.riot.kpi.games` | Parties | Games |
| `stats.riot.kpi.rate` | Victoires | Win rate |
| `stats.riot.kpi.rateOf` | one : sur {count} partie<br>other : sur {count} parties | one : over {count} game<br>other : over {count} games |
| `stats.riot.kpi.kda` | KDA moyen | Average KDA |
| `stats.riot.kpi.kdaHow` | (K + A) / D, toutes parties | (K + A) / D, all games |
| `stats.riot.kpi.duration` | Durée moyenne | Average length |
| `stats.riot.kpi.durationHow` | Remakes exclus | Remakes excluded |
| `stats.riot.kpi.noPercent` | Pas de pourcentage sous {min} parties | No percentage under {min} games |

### `stats.minutes.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.minutes` | {count} min | {count} min |

### `stats.champions.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.champions.title` | Tes champions | Your champions |
| `stats.champions.sub` | Classés au nombre de parties : le volume dit ton expérience, pas ton niveau. | Sorted by games played: volume shows experience, not skill. |
| `stats.champions.games` | one : {count} partie<br>other : {count} parties | one : {count} game<br>other : {count} games |
| `stats.champions.kda` | KDA {value} | KDA {value} |
| `stats.champions.gamesUnit` | one : partie<br>other : parties | one : game<br>other : games |
| `stats.champions.badgeStrength` | Au-dessus de ta moyenne | Above your average |
| `stats.champions.badgeWeakness` | En dessous de ta moyenne | Below your average |
| `stats.champions.others` | one : Et {count} autre champion : {games}<br>other : Et {count} autres champions : {games} | one : And {count} other champion: {games}<br>other : And {count} other champions: {games} |

### `stats.roles.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.roles.title` | Par poste | By role |
| `stats.roles.none` | Sans poste | No role |

### `stats.patches.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.patches.title` | Par patch | By patch |
| `stats.patches.note` | Un patch n'est pas une durée fixe : un écart entre deux patchs dit rarement quelque chose. | A patch isn't a fixed length of time: a gap between two patches rarely means anything. |

### `stats.slices.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.slices.title` | Par tranches de {count} parties | In blocks of {count} games |
| `stats.slices.note` | Une tranche de 20 parties bouge beaucoup par hasard. Une tendance n'est conclue qu'avec {min} parties dans un poste et un écart net. | A block of 20 games moves a lot by chance. A trend is only stated with {min} games in one role and a clear gap. |
| `stats.slices.current` | En cours | In progress |

### `stats.duels.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.duels.title` | Tes vis-à-vis en mid | Your mid lane opponents |
| `stats.duels.opponents` | one : {count} adversaire différent<br>other : {count} adversaires différents | one : {count} different opponent<br>other : {count} different opponents |
| `stats.duels.repeated` | one : {count} vu au moins 3 fois<br>other : {count} vus au moins 3 fois | one : {count} faced 3 times or more<br>other : {count} faced 3 times or more |
| `stats.duels.midOnly` | Mid seulement : l'import garde le mid adverse, pas l'adversaire des autres postes. | Mid only: the import keeps the enemy mid, not the opponent in other roles. |
| `stats.duels.noPercent` | Aucun pourcentage sous {min} parties contre le même adversaire. | No percentage under {min} games against the same opponent. |
| `stats.duels.noMid` | Aucune partie en mid : pas de vis-à-vis à montrer. | No mid games: no lane opponents to show. |
| `stats.duels.badgeHard` | Duel difficile | Tough matchup |
| `stats.duels.badgeEasy` | Duel favorable | Good matchup |

### `stats.journal.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.journal.heading` | Journal DRAFT | DRAFT journal |
| `stats.journal.source` | Noté par toi avec « Je joue », sur ce PC. | Logged by you with "I'm playing", on this PC. |
| `stats.journal.kpi.finished` | Parties terminées | Finished games |
| `stats.journal.kpi.finishedHow` | résultat noté | result logged |
| `stats.journal.kpi.followed` | Conseil joué | Advice played |
| `stats.journal.kpi.followedHow` | sur les parties terminées | over finished games |
| `stats.journal.followedDef` | Conseil joué : le champion cliqué est celui que DRAFT conseillait à ce moment. Compté seulement sur les parties terminées. | Advice played: the champion you clicked is the one DRAFT advised at that moment. Counted on finished games only. |
| `stats.journal.tieNote` | À égalité, choisir l'un des ex æquo compte comme conseil joué. | On a tie, picking one of the tied champions counts as advice played. |
| `stats.journal.pending` | one : {count} draft attend son résultat.<br>other : {count} drafts attendent leur résultat. | one : {count} draft is waiting for its result.<br>other : {count} drafts are waiting for their result. |
| `stats.journal.pendingHelp` | Sans résultat, DRAFT ne mesure rien. Note-le juste après la partie. | Without a result, DRAFT can't measure anything. Log it right after the game. |
| `stats.journal.originUnknown` | one : {count} draft notée avant que le Simulateur cesse d'écrire dans le journal : elle peut être une simulation.<br>other : {count} drafts notées avant que le Simulateur cesse d'écrire dans le journal : certaines peuvent être des simulations. | one : {count} draft logged before the Simulator stopped writing to the journal: it may be a simulation.<br>other : {count} drafts logged before the Simulator stopped writing to the journal: some may be simulations. |
| `stats.journal.realOnly` | Drafts réelles seulement : le Simulateur n'écrit rien dans le journal. | Real drafts only: the Simulator writes nothing to the journal. |
| `stats.journal.row.advised` | DRAFT conseillait {champion} | DRAFT advised {champion} |
| `stats.journal.row.played` | Tu as joué {champion} | You played {champion} |
| `stats.journal.row.vs` | contre {champion} | against {champion} |
| `stats.journal.row.close` | Choix serré | Close call |
| `stats.journal.tag.followed` | Conseil joué | Advice played |
| `stats.journal.tag.other` | Autre choix | Other pick |
| `stats.journal.compare.title` | Conseil joué ou autre choix | Advice played or other pick |
| `stats.journal.compare.warning` | Ce n'est pas une preuve que DRAFT t'aide : tu ne choisis pas au hasard, et il faut des centaines de parties pour un écart fiable. | This doesn't prove DRAFT helps: you don't pick at random, and a reliable gap takes hundreds of games. |
| `stats.journal.compare.notEnough` | Comparaison à partir de {min} parties terminées dans chaque groupe. Aujourd'hui : {followed} et {other}. | Comparison from {min} finished games in each group. Today: {followed} and {other}. |
| `stats.journal.logResult` | Noter le résultat | Log the result |

### `stats.findings.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.findings.title` | Ce qu'on peut conclure | What we can conclude |
| `stats.findings.sub` | Seulement ce que l'échantillon permet. Chaque conclusion montre sa preuve. | Only what the sample allows. Each conclusion shows its proof. |
| `stats.findings.established` | Établi | Established |
| `stats.findings.inconclusive` | Pas d'écart net | No clear gap |
| `stats.findings.notYet` | Pas encore | Not yet |
| `stats.findings.none` | Rien d'établi pour l'instant : chaque conclusion demande un échantillon minimum. | Nothing established yet: each conclusion needs a minimum sample. |
| `stats.findings.strength` | {champion} : au-dessus de ta moyenne en {role} | {champion}: above your {role} average |
| `stats.findings.weakness` | {champion} : en dessous de ta moyenne en {role} | {champion}: below your {role} average |
| `stats.findings.championFlat` | {champion} : pas d'écart net avec tes autres champions en {role} | {champion}: no clear gap with your other {role} champions |
| `stats.findings.flatProof` | {record} : {rate}, entre {low} et {high} | {record}: {rate}, between {low} and {high} |
| `stats.findings.championProof` | {record} sur {games} parties : {rate}, entre {low} et {high}. Tes autres champions en {role} : {ref} sur {refGames} parties. | {record} over {games} games: {rate}, between {low} and {high}. Your other {role} champions: {ref} over {refGames} games. |
| `stats.findings.championRule` | Règle : {min} parties sur le champion et sur tes autres champions du poste ; intervalle à 95 % qui exclut ta moyenne. | Rule: {min} games on the champion and on your other champions in that role; 95% interval that excludes your average. |
| `stats.findings.trendUp` | En progrès en {role} | Improving in {role} |
| `stats.findings.trendDown` | En baisse en {role} | Dropping in {role} |
| `stats.findings.trendFlat` | Pas d'évolution nette en {role} | No clear change in {role} |
| `stats.findings.trendProof` | Première moitié : {older} sur {olderGames} parties. Seconde moitié : {newer} sur {newerGames} parties. | First half: {older} over {olderGames} games. Second half: {newer} over {newerGames} games. |
| `stats.findings.trendRule` | Règle : {min} parties dans le poste, et un écart d'au moins 2 écarts-types entre les deux moitiés. | Rule: {min} games in the role, and a gap of at least 2 standard errors between the two halves. |
| `stats.findings.duelHard` | Duel difficile : {champion} | Tough matchup: {champion} |
| `stats.findings.duelEasy` | Duel favorable : {champion} | Good matchup: {champion} |
| `stats.findings.duelProof` | {record} contre {champion} : {rate}, au plus {high}. Ta moyenne en mid : {ref} sur {refGames} parties. | {record} against {champion}: {rate}, at most {high}. Your mid average: {ref} over {refGames} games. |
| `stats.findings.duelRule` | Règle : {min} parties contre le même adversaire ; intervalle à 99 %, car {count} duels sont testés. | Rule: {min} games against the same opponent; 99% interval, because {count} matchups are tested. |
| `stats.findings.duelFlat` | one : {count} autre duel testé : pas d'écart net.<br>other : {count} autres duels testés : pas d'écart net. | one : {count} other matchup tested: no clear gap.<br>other : {count} other matchups tested: no clear gap. |
| `stats.findings.adviceFlat` | Conseil joué ou autre choix : pas d'écart net | Advice played or other pick: no clear gap |
| `stats.findings.adviceProof` | Conseil joué : {followed}. Autre choix : {other}. Les intervalles se recouvrent. | Advice played: {followed}. Other pick: {other}. The intervals overlap. |

### `stats.withheld.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.withheld.champion` | Points forts et faibles : il faut {min} parties sur un champion. {champion} en a {games}. | Strengths and weaknesses: a champion needs {min} games. {champion} has {games}. |
| `stats.withheld.trend` | Tendance : il faut {min} parties dans un poste. Tu en as {games} en {role}. | Trend: a role needs {min} games. You have {games} in {role}. |
| `stats.withheld.duel` | Duels : il faut {min} parties contre le même adversaire. Le plus croisé, {champion}, en compte {games}. | Matchups: {min} games against the same opponent are needed. The most frequent, {champion}, has {games}. |
| `stats.withheld.advice` | Conseil joué ou autre choix : il faut {min} parties terminées dans chaque groupe. Tu en as {followed} et {other}. | Advice played or other pick: each group needs {min} finished games. You have {followed} and {other}. |
| `stats.withheld.noData` | Rien à conclure sans données : importe ton historique ou note tes drafts. | Nothing to conclude without data: import your history or log your drafts. |
| `stats.withheld.otherLanes` | Vis-à-vis hors mid : l'import ne garde pas encore le poste des adversaires. | Lane opponents outside mid: the import doesn't keep enemy roles yet. |

### `stats.story.*`

| Clé | Français | Anglais |
|---|---|---|
| `stats.story.nav.sample` | Ton échantillon | Your sample |
| `stats.story.nav.champions` | Tes champions | Your champions |
| `stats.story.nav.duels` | Tes vis-à-vis | Your opponents |
| `stats.story.nav.findings` | Ce qu'on peut dire | What we can say |
| `stats.story.nav.journal` | Tes drafts DRAFT | Your DRAFT drafts |
| `stats.story.patches` | one : {count} patch<br>other : {count} patchs | one : {count} patch<br>other : {count} patches |
| `stats.story.sample.lead` | {games}, du {from} au {to}, sur {patches}. | {games}, from {from} to {to}, across {patches}. |
| `stats.story.sample.main` | Surtout en {role} : {count} parties sur {total}. | Mostly {role}: {count} games out of {total}. |
| `stats.story.sample.timeline` | Chaque trait est une partie : vert pour une victoire, corail pour une défaite. Les espaces sont des semaines sans partie classée. | Each mark is one game: green for a win, coral for a loss. Gaps are weeks without ranked games. |
| `stats.story.champions.count` | one : Tu as joué {count} champion.<br>other : Tu as joué {count} champions. | one : You played {count} champion.<br>other : You played {count} champions. |
| `stats.story.champions.enough` | one : {count} dépasse {min} parties : son pourcentage s'affiche.<br>other : {count} dépassent {min} parties : leur pourcentage s'affiche. | one : {count} is over {min} games: its percentage shows.<br>other : {count} are over {min} games: their percentage shows. |
| `stats.story.champions.noneEnough` | Aucun ne dépasse {min} parties : pas encore de pourcentage. | None is over {min} games: no percentage yet. |
| `stats.story.duels.lead` | En mid, {games} contre {opponents}. | In mid, {games} against {opponents}. |
| `stats.story.duels.repeated` | one : {count} revient au moins 3 fois.<br>other : {count} reviennent au moins 3 fois. | one : {count} comes back 3 times or more.<br>other : {count} come back 3 times or more. |
| `stats.story.drafts` | one : {count} draft notée<br>other : {count} drafts notées | one : {count} draft logged<br>other : {count} drafts logged |
| `stats.story.journal.leadNone` | Tu n'as encore noté aucune draft. | You haven't logged any draft yet. |
| `stats.story.journal.lead` | {drafts}, du {from} au {to}. | {drafts}, from {from} to {to}. |
| `stats.story.journal.noResult` | Aucun résultat noté : DRAFT ne peut encore rien mesurer. | No result logged: DRAFT can't measure anything yet. |
| `stats.story.journal.results` | one : {count} a un résultat.<br>other : {count} ont un résultat. | one : {count} has a result.<br>other : {count} have a result. |
| `stats.story.empty.title` | Rien à raconter pour l'instant | Nothing to tell yet |
| `stats.story.empty.lead` | DRAFT n'a ni historique Riot ni draft notée. Deux façons de commencer : | DRAFT has no Riot history and no logged draft. Two ways to start: |
| `stats.story.empty.riot` | Importer ton historique Riot : tes parties classées, lues avec ta clé, jamais enregistrée. | Import your Riot history: your ranked games, read with your key, never saved. |
| `stats.story.empty.journal` | Noter tes drafts : clique sur « Je joue » pendant une draft, puis note le résultat. | Log your drafts: click "I'm playing" during a draft, then log the result. |
