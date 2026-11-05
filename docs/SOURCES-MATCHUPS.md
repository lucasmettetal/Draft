# Sources de matchups : ce qui est utilisable

Vérifié le 5 novembre 2026. Ce n'est pas un avis juridique. **Aucune donnée tierce n'a été collectée ni intégrée.**

Règle appliquée : sans conditions lisibles qui autorisent la réutilisation, une source est considérée comme non autorisée. Un code sous licence MIT n'accorde aucun droit sur les données qu'il sert.

## Conclusion

**Aucune source tierce de statistiques de matchups n'est utilisable aujourd'hui.** La seule voie claire est le calcul déjà présent dans DraftMid, à partir de l'API Riot. Il n'a encore jamais tourné avec une vraie clé.

## Sources

| Source | Matchups en classé | Patch, rang, région, rôle | Conditions constatées | Verdict |
|---|---|---|---|---|
| **API Riot, Match-V5, calcul local** | Oui, à calculer | Oui, tout est calculable | Les statistiques agrégées sans joueur précis figurent parmi les usages approuvés. Clé personnelle pour un usage privé | **Utilisable, sur le PC du joueur** |
| **Même calcul, livré à tous** | Oui | Oui | Demande une clé de production et un produit enregistré. Riot interdit aussi de servir d'intermédiaire de données vers un tiers | Pas sans décision de Riot |
| Data Dragon | Non | - | Données officielles | Utilisable, mais pas pour les matchups |
| CommunityDragon | Non | - | Se décrit comme une zone grise, reconnue par Riot | À éviter pour livrer des données sans décision |
| Meraki `lolstaticdata` | Non | - | Code MIT, crédit demandé. Sa licence sur les données reste à confirmer | Sans intérêt pour les matchups |
| OP.GG (site) | Oui | Oui | Conditions non lisibles par mes outils. Collecte automatique déjà exclue par tes consignes | **Non** |
| OP.GG (serveur MCP `opgg-mcp`) | Oui, selon son dépôt | À vérifier | Code MIT, publié par `opgginc`, mais aucune condition sur les données ou les limites | **Non, sans autorisation écrite** |
| Lolalytics | Oui | Oui | Pas d'API publique trouvée, page de conditions introuvable | **Non** |
| U.GG | Oui | Oui | Accès à ses conditions refusé à mes outils | **Non** |
| Mobalytics | Oui | Oui | Aucune API publique officielle trouvée | **Non** |
| METAsrc | Oui | Oui | Page LoL non lue. Sa page TFT limite l'usage commercial sans autorisation, et ne donne aucun droit de réutilisation | **Non** |
| Leaguepedia | Non, parties professionnelles | Non applicable | Page d'API inaccessible à mes outils | Hors sujet : pas le classé |
| Oracle's Elixir | Non, parties professionnelles | Non applicable | Conditions non trouvées | Hors sujet : pas le classé |
| Jeux de données publics (Kaggle...) | Parfois | Souvent anciens | Licence variable selon le jeu | Au cas par cas, patch trop ancien en général |

« Non lisible » signifie que je n'ai pas pu lire la page : cela ne prouve ni interdiction, ni autorisation.

## Règles de Riot à respecter

Résumé des pages développeur :

- **Décision :** une application peut souligner une décision et proposer des options, sans la dicter ni la retirer. D'où le modèle à plusieurs choix, et les explications sous forme de compromis.
- **Classement :** pas d'alternative aux classements officiels, ni de calculateur de MMR ou d'ELO. La familiarité reste donc propre au joueur et à chaque champion, sans niveau global ni comparaison entre joueurs.
- **Winrates :** l'interdiction explicite vise les Augments et objets d'Arena. Les pages lues ne traitent pas directement les outils de draft : **aucune fonction nouvelle ne doit être présentée comme autorisée par Riot.**
- **Enregistrement :** les produits doivent être enregistrés et revus, et chaque changement important doit l'être aussi. L'avertissement de non-approbation reste obligatoire.

## Fournisseur interchangeable

`registerMatchupProvider` (dans `web/draft-mastery.js`) accepte une source seulement si elle déclare `id`, `license`, `terms` et `lookup`. Aucune n'est enregistrée.

```js
registerMatchupProvider({
  id: "source",
  license: "...",
  terms: "...",
  lookup: (pick, opponent, { role }) => ({
    games, wins, baseline,   // obligatoires. baseline : winrate de référence du champion dans ce rôle
    role,                    // obligatoire, et égal au rôle demandé
    patch, rank, region      // conservés pour l'affichage
  })
});
```

- **Ligne ignorée :** mauvais rôle, moins de 30 parties, ou référence absente. Aucune valeur n'est estimée à sa place.
- **Calcul :** écart à la référence, ramené vers zéro selon l'échantillon, comme les bilans actuels.
- **Aucun effet sur le classement :** la valeur n'est qu'affichée, avec sa source. Le jour où une source sera validée, elle devra remplacer le critère de matchup du moteur, sans s'y ajouter.

## Sources consultées

- [Politiques générales de Riot](https://developer.riotgames.com/policies/general)
- [Documentation League of Legends de Riot](https://developer.riotgames.com/docs/lol)
- [Meraki `lolstaticdata`](https://github.com/meraki-analytics/lolstaticdata)
- [CommunityDragon](https://communitydragon.org/documentation)
- [OP.GG `opgg-mcp`](https://github.com/opgginc/opgg-mcp)
- [METAsrc, conditions TFT](https://www.metasrc.com/tft/17.6/terms)
