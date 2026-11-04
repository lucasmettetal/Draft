# Validation sur de vraies drafts

But : mesurer DraftMid en situation réelle. **Aucun de ces tests n'a encore été réalisé.** Rien n'est à noter pendant la sélection : l'application enregistre, et le reste se fait après la partie.

## Avant de commencer

1. Ouvrir **Réglages > Avancé** et cocher le diagnostic.
2. Laisser le fichier `validation-drafts.csv` fermé pendant que tu joues.
3. Pour l'ensemble du test, ne pas changer de version ni de profil, ou le noter dans `remarques`.

Fichier : `%LOCALAPPDATA%\DraftMid\validation-drafts.csv`. Il reste sur ce PC, sans nom de joueur. S'il était ouvert, les lignes vont dans `validation-drafts-attente.csv`.

## Enregistré automatiquement à la fin de chaque sélection

| Colonne | Contenu |
|---|---|
| `adversaires` | Champions adverses visibles |
| `mid_affiche` | Mid présenté, vide si « non identifié » |
| `confiance` | élevée, moyenne, faible, ou manuel |
| `hypotheses` | Autres candidats gardés |
| `recommande`, `alternative` | Conseil affiché |
| `provisoire` | Conseil marqué provisoire |
| `corrections` | Tes corrections manuelles |
| `joue` | Champion cliqué, s'il l'a été |
| `phase_suivante` | `InGame` si la partie a démarré |

## À compléter après la partie (30 secondes)

Dans l'historique de la partie, remplir sur la ligne correspondante :

| Colonne | Contenu | Exemple |
|---|---|---|
| `mid_reel` | Le vrai mid adverse, écrit comme dans `adversaires` | `Ahri` |
| `roles_reels` | Les 5 rôles, seulement si tu as le temps | `top=Sett,jungle=Vi,mid=Ahri,adc=Jinx,support=Janna` |
| `remarques` | Ce qui t'a surpris | `conseil absurde contre 4 tanks` |

Le rôle réel est celui joué en partie : s'il y a un doute (échange de lanes), écrire `?` et ne pas compter la partie.

## Panneaux et traînée (une fois par session)

Copier ce tableau dans `remarques.txt` ou une note. À remplir une fois, puis seulement si quelque chose change.

| Date | Écrans et échelle | Taille du client | Disposition (2 panneaux, 1, étroit) | Texte coupé ou portrait déformé | Couvre un bouton de LoL | Clignotement | Focus perdu | Traînée (0 à 3) |
|---|---|---|---|---|---|---|---|---|
| | | | | | | | | |

Échelle de traînée, en déplaçant le client d'un geste normal pendant 3 secondes, **de préférence hors file** :
- **0 :** aucune différence visible.
- **1 :** léger décalage, seulement si on le cherche.
- **2 :** décalage visible, sans gêne une fois immobile.
- **3 :** gênant, ou les panneaux restent décalés à l'arrêt.

Ne jamais déplacer le client ni fermer quoi que ce soit si cela peut gêner une partie.

## Lire les résultats

```powershell
.\tools\validation-report.ps1
```

- Compter seulement les parties jouées, dans la file classée, et sur le même patch si possible.
- **Sous 50 drafts renseignées, aucune conclusion chiffrée.** Le script le rappelle.
- Avec 100 drafts, un taux de 90 % reste incertain, entre 83 % et 94 % environ.
- Tes drafts ne représentent que ton rang et ton serveur.
- La qualité du conseil ne se juge pas sur une victoire : une partie dépend de trop d'autres choses.

## Ce que ce relevé ne mesure pas

Il ne dit pas si un autre pick aurait gagné, ni si tu aurais choisi autrement sans l'outil.
