# Préparation de l'intégration V2.15

Branche `integration/v2.15-coaching`, partie de `7690f79` (V2.14). Rien n'est fusionné dans la version stable, installé ni publié.

**Intégré sur cette branche :** égalités à l'écran, import dans les réglages, le plus joué, conseils sur 3 niveaux, onglet Conseils. Le classement reste le modèle A, et le modèle T reste désactivé.

## Contenu de la branche

| Élément | Fichier | État |
|---|---|---|
| Égalités explicites | `web/draft-engine.js`, `web/index.html` | Scores inchangés, ex æquo présentés au même niveau |
| Conseils pédagogiques | `web/draft-coaching.js`, onglet Conseils | Au plus un conseil pendant la draft, issu d'une donnée ou d'un calcul |
| Données officielles | `web/draft-champion-official.js` | Chargées |
| Familiarité et compromis | `web/draft-mastery.js` | Affichage seulement |
| Import de l'historique | `src/HistoryImporter.cs`, réglages | Bouton « Importer mon historique Riot » |

## Reste à faire avant une version stable

1. Tester l'import avec une vraie clé, et voir sa fenêtre à l'écran.
2. Essayer la compilation sur de vraies drafts.
3. Faire relire les conseils par un joueur.

## Chargement dans `index.html`

Après `draft-data.js` :

```html
<script src="draft-champion-official.js"></script>
<script src="draft-coaching.js"></script>
<script src="draft-mastery.js"></script>
```

Les noms globaux de ces fichiers ne recoupent pas ceux de `index.html` et `draft-data.js`, y compris dans la V2.14 (vérifié par analyse du texte). `pct` et `decimal` ont été renommés pour cela.

## Présentation

```js
const history = importedHistory();   // import de l'application, sinon ancien import. Lecture seule
const profile = history ? familiarityProfile(history.rows, { account }) : null;
const c = playerChoices({ enemies, level, profile, learning, fixedRoles, manualOpponent, hasFront, pick });
```

| Champ | Affichage |
|---|---|
| `c.strategic` | **Toujours visible.** `champions` (plusieurs si égalité), `label`, `source` |
| `c.familiar` | Sur une ligne distincte : `label` (« Le plus joué »), `champion`, `games`. Absent si rien de fiable. `isPerformance` vaut toujours `false` |
| `c.compromise.text` | Une phrase, facultative |
| `c.theory`, `c.experience`, `c.uncertainty` | À ouvrir. Chaque conseil a `title`, `text`, `basisLabel`, `limits` |
| `c.ranking` | Classement du moteur, inchangé |

```
Pour la draft : Camille
Le plus joué : Darius, 100 parties
La draft penche légèrement vers Camille. Darius est le champion que tu as le plus joué.
> Théorie   > Expérience   > Incertitude
```

**À éviter à l'écran :** une médaille, une couleur de réussite ou le mot « maîtrise » sur la ligne du plus joué, car elle ne dit rien de la performance. Ne pas non plus afficher le plus joué comme une recommandation.

## Import de l'historique

**Parcours :** Réglages, « Importer mon historique Riot », puis fenêtre dédiée (consentement, comptes, clé masquée, région, nombre de parties). Le premier import lit 100 parties par défaut. Le bouton devient ensuite « Importer plus de parties ». Aucun appel n'a lieu sans ce clic, et l'import est refusé pendant une draft ou un autre calcul Riot.

| Garantie | Mise en œuvre | Testé |
|---|---|---|
| Clé non conservée | Saisie masquée, gardée en mémoire par le client HTTP. L'importeur n'y a pas accès, et elle n'est écrite nulle part | Oui, avec une fausse clé |
| Clé hors des adresses | En-tête `X-Riot-Token` | Oui |
| Données existantes intactes | Écrit seulement `draft-history.js`, sa copie `.precedent` et une copie datée dans `sauvegardes-historique` (les 5 dernières). `draft-stats.js`, son cache, `draft-accounts.txt` et `settings.json` ne sont pas touchés | Oui, par empreinte |
| Consentement | Case obligatoire : le bouton reste bloqué sans elle | Oui |
| Aucun autre joueur | Champions adverses seulement, ni identifiant ni nom | Oui |
| Pas de perte | Écriture atomique, sauvegarde toutes les 25 parties, reprise sans doublon, fichier abîmé mis de côté | Oui |
| Refus de Riot | Clé refusée, limite de requêtes, panne, compte introuvable, réponse illisible | Oui, sur un serveur simulé |
| Rythme | 1 requête toutes les 1,3 seconde, soit environ 22 minutes pour 1 000 parties | Réglage repris de l'existant |

**Limites :**

- Testé sur un serveur simulé seulement : jamais avec une vraie clé, et la fenêtre de saisie n'a jamais été affichée à l'écran.
- Solo/duo uniquement, depuis juin 2021 au mieux.
- Une clé de développement expire après 24 heures et reste limitée à 100 requêtes par 2 minutes, soit environ 1 minute 30 par 60 parties.
- Les régions proposées sont celles de la fenêtre existante : EUW, EUNE, TR, NA et KR.
- L'onglet Parties et les cartes du pool lisent toujours l'ancien import, pas ce fichier.

**Le fichier contient le `Nom#TAG`** de tes comptes, comme l'ancien import. Il reste sur ce PC, dans `%LOCALAPPDATA%\DraftMid`, et `draft-history.js*` est ignoré par Git.

## Conditions avant d'aller plus loin

- **Riot :** un nouveau traitement de l'historique doit être décrit dans la fiche du produit, et rien ne doit être présenté comme approuvé.
- **Victoires :** aucune annonce sans échantillon suffisant et indépendant.
- **Modèle T :** à activer seulement après relecture sur de vraies drafts, avec les règles de comparaison écrites à l'avance.
- **Matchups :** le calcul local reste la seule source utilisable. Il n'alimente pas de nouveau terme de score.
