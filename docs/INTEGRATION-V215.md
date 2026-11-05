# Préparation de l'intégration V2.15

Rien ici n'est fusionné, installé ni publié. La V2.14 est sur une autre branche.

## Contenu de la branche

| Élément | Fichier | État |
|---|---|---|
| Égalités explicites | `web/draft-engine.js` | Dans le moteur, scores inchangés |
| Conseils pédagogiques | `web/draft-coaching.js` | Non chargé par l'interface |
| Données officielles | `web/draft-champion-official.js` | Non chargé par l'interface |
| Familiarité et compromis | `web/draft-mastery.js` | Non chargé par l'interface |
| Import de l'historique | `src/HistoryImporter.cs` | Accessible par `--import-history`, sans bouton |

## Ordre conseillé

1. **Fusionner la V2.14 dans une branche d'essai,** puis relancer tous les tests. Une simulation de fusion ne signale aucun conflit de fichiers, mais les comportements combinés n'ont pas été testés.
2. **Afficher les égalités** avec `tiedWith`. Sans cela, l'ordre alphabétique reste un biais visible.
3. **Ajouter le bouton d'import** et le chargement de `draft-history.js`.
4. **Afficher les deux lectures** de `playerChoices`, sans changer le classement.
5. **Relire les conseils** avec un joueur.
6. **Décider du modèle T** après observation sur de vraies drafts.

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

**Commande d'essai :** `DraftMid-Portable.exe --import-history`, depuis `dist-coaching\build`. Elle n'exige pas de fermer DraftMid, et n'installe rien.

| Garantie | Mise en œuvre | Testé |
|---|---|---|
| Clé non conservée | Saisie masquée, gardée en mémoire par le client HTTP. L'importeur n'y a pas accès, et elle n'est écrite nulle part | Oui, avec une fausse clé |
| Clé hors des adresses | En-tête `X-Riot-Token` | Oui |
| Données existantes intactes | Écrit seulement `draft-history.js` et sa copie `.precedent`. `draft-stats.js`, son cache, `draft-accounts.txt` et `settings.json` ne sont pas touchés | Oui, par empreinte |
| Aucun autre joueur | Champions adverses seulement, ni identifiant ni nom | Oui |
| Pas de perte | Écriture atomique, sauvegarde toutes les 25 parties, reprise sans doublon, fichier abîmé mis de côté | Oui |
| Refus de Riot | Clé refusée, limite de requêtes, panne, compte introuvable, réponse illisible | Oui, sur un serveur simulé |
| Rythme | 1 requête toutes les 1,3 seconde, soit environ 22 minutes pour 1 000 parties | Réglage repris de l'existant |

**Non testé :** un import avec une vraie clé, et la fenêtre affichée à l'écran. Elle a seulement été créée sans affichage par les tests natifs.

**Le fichier contient le `Nom#TAG`** de tes comptes, comme l'ancien import. Il reste sur ce PC, dans `%LOCALAPPDATA%\DraftMid`, et `draft-history.js*` est ignoré par Git.

**À faire pour le bouton de la V2.15 :**

1. Appeler `HistoryImportDialog.RunWindow(dataDir)`, sans l'exécuter pendant une draft.
2. Copier `draft-history.js` vers le dossier web, comme `draft-stats.js`, et ajouter sa balise de script.
3. Recharger les panneaux hors draft, comme pour les rôles.
4. Ajouter le fichier à la procédure de sauvegarde et de restauration.

## Conflits à surveiller

- **`src/DraftMid.cs` :** 4 petites lignes (option, appel, test natif).
- **`web/draft-engine.js` :** tri d'`analyze` et nouveaux champs. À revérifier avec `run-ties-compare.ps1` après fusion.
- **`web/draft-tests.html` :** modifié des deux côtés, sans conflit à la simulation. La tolérance d'un millionième doit rester.
- **`.gitignore` :** une ligne.

## Conditions avant d'aller plus loin

- **Riot :** un nouveau traitement de l'historique doit être décrit dans la fiche du produit, et rien ne doit être présenté comme approuvé.
- **Victoires :** aucune annonce sans échantillon suffisant et indépendant.
- **Modèle T :** à activer seulement après relecture sur de vraies drafts, avec les règles de comparaison écrites à l'avance.
- **Matchups :** le calcul local reste la seule source utilisable. Il n'alimente pas de nouveau terme de score.
