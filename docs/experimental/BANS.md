# Bans adverses et déduction des rôles : étude

**État : aucun bénéfice démontré, le moteur ne les utilise pas.** Aucune mesure sur de vraies données n'a encore été faite.

## Ce qui est établi, et ce qui ne l'est pas

| Question | Réponse | Fiabilité |
|---|---|---|
| Bans par équipe dans la session LCU | `bans.myTeamBans`, `bans.theirTeamBans`, `numBans` | Exemples communautaires, non documentés par Riot |
| Liste `actions` dans la session | Elle existe | Exemples communautaires |
| Ban adverse relié à un joueur adverse | **Inconnu** | Aucune source trouvée, à vérifier sur un vrai client |
| Bans dans match-v5 | `championId` et `pickTurn` par équipe, sans lien écrit vers un joueur | Modèles de bibliothèques tierces. Page officielle non lisible par mon outil |
| Sens de `pickTurn` | « Le tour pendant lequel le champion a été banni » | Documentation officielle spectator-v5, cité via une recherche |
| Méthode d'OP.GG | Non publiée à ma connaissance. Son aide dit seulement estimer la position d'après diverses données de la partie, sans détail | Extrait de recherche : la page refuse la lecture automatique |

## Limite décisive

Si seuls les bans de l'équipe sont connus, ils ne peuvent presque pas départager deux flex de la même équipe. Échanger Yasuo et Yone entre mid et top ne change pas qui, dans l'équipe, a pu bannir quoi. **Le signal n'a de valeur que si un ban peut être relié à un joueur précis.** Et si le client expose ce lien sans l'afficher, l'utiliser pourrait contredire la règle « aucune information volontairement cachée ». Il faudra alors une décision explicite.

## Signaux possibles, à vérifier

Ce sont des hypothèses. Le lien « on bannit sa propre lane » vient de guides, pas d'une étude.

- Un joueur banni plutôt des champions de sa lane.
- Bruit fort : les bans suivent aussi la méta et le patch, sans rapport avec la lane.
- Le lien change donc avec le patch : il doit se mesurer par patch.

## Modèle testé

Le poids d'une répartition des rôles reste le produit des parts de chaque champion. Si un ban est relié à un joueur, un facteur s'ajoute :

`exp(λ × log( P(rôle du champion banni | rôle du joueur) / 0,2 ))`

- `P` s'apprend seulement sur la partie d'entraînement.
- `λ` se choisit seulement sur l'entraînement, parmi 0 ; 0,25 ; 0,5 ; 1.
- `λ = 0` est le comportement actuel, et reste la valeur tant que rien n'est démontré.

## Règle de décision, fixée avant toute mesure

Sur les mêmes équipes, avec et sans bans, il faut **toutes** ces conditions :

- au moins 500 équipes de test, jamais utilisées pour le modèle ;
- p < 0,01 sur les parties qui changent de résultat (test exact de McNemar) ;
- au moins 1 point de gain ;
- plus de parties gagnées que perdues ;
- des probabilités mieux calibrées.

Même alors, le résultat serait « à revoir », pas une activation automatique.

## Vérification de la règle sur données synthétiques

**Ces chiffres ne décrivent pas League of Legends.** Ils montrent seulement que la règle sait dire oui et non.

| Données fabriquées | Sans bans | Avec bans | Décision |
|---|---|---|---|
| Bans informatifs, 900 équipes de test | 82,3 % | 93,3 % | Bénéfice mesuré |
| Bans sans information, 900 équipes | 80,3 % | 80,3 % | Non démontré |
| Bans informatifs, 90 équipes seulement | 80 % | 90 % | Non démontré |

## Expérience isolée

Désactivée par défaut, sans effet sur les conseils. La case est dans **Réglages > Avancé**.

- `bans-structure.csv` : à chaque sélection, des nombres seulement. Combien de bans adverses sont visibles, combien d'actions de ban adverses, combien sont reliées à un joueur. Aucun champion, aucun joueur, aucun lien n'est conservé.
- `HoldoutBans` : compositions et bans des parties réservées, lors du calcul des rôles, sans joueur.
- `tools/run-ban-experiment.ps1` : applique la règle. Sans données, il le dit et s'arrête.

**Limite de la mesure :** rien n'indique que l'ordre de `pickTurn` correspond à l'ordre des joueurs dans la partie. Un résultat négatif ne prouverait donc pas que les bans sont inutiles, seulement que ce lien-là n'informe pas.

## Sources

- [Exemple de session LCU](https://gist.github.com/xadamxk/8cb5d21d24bb78d63c5241e97087bb23)
- [Overwolf : données du client](https://dev.overwolf.com/ow-electron/reference/live-game-data-gep/supported-games/league-of-legends-launcher)
- [Riot : spectator-v5](https://developer.riotgames.com/api-details/spectator-v5)
- [Modèle match-v5 d'une bibliothèque tierce](https://docs.rs/crate/riot-api/latest/source/src/models/league/lol_match/team.rs)
- [Message de Riot sur les informations en sélection](https://devtrackers.gg/leagueoflegends/p/749a443a-apps-that-reveal-player-names-or-stats-in-champ-select-need-to-remove-these-informations-starting-patch-12-22)
- [Aide OP.GG sur les positions](https://help.op.gg/hc/en-us/articles/30994139075737-My-position-is-shown-differently-from-what-I-actually-played)
