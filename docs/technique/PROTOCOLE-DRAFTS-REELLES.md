# Protocole de collecte de vraies drafts (V2.17)

**Statut :** protocole prêt. **Aucune collecte n'a encore été faite.** Rien n'est automatique : la collecte se lance seulement si tu l'actives, et rien ne quitte ton PC.

Ce protocole complète [VALIDATION-REELLE.md](VALIDATION-REELLE.md), qui décrit le relevé existant, et le banc synthétique de la V2.17. Le banc mesure le **comportement** du moteur sur des drafts inventées. Ce protocole mesure ses **erreurs sur de vraies parties**.

## 1. Questions auxquelles la collecte répond

| Question | Mesure | Colonnes utilisées |
|---|---|---|
| L'adversaire direct est-il bien reconnu ? | Nommé juste, nommé à tort, vrai adversaire cité parmi les hypothèses, par confiance et par rôle | `mid_affiche`, `hypotheses`, `confiance`, `role`, `mid_reel` |
| Quels champions flex sont mal reconnus ? | Champions vus sur plusieurs rôles ; erreurs « affiché → réel » les plus fréquentes | `roles_reels`, `mid_affiche`, `mid_reel` |
| Que conseille DRAFT dans de vraies situations ? | Conseil, alternative, « provisoire », corrections manuelles | `recommande`, `alternative`, `provisoire`, `corrections` |
| Où les prédictions s'écartent-elles des observations ? | Rôles prédits pour les 5 adversaires comparés aux rôles réels | `roles_predits` (**à ajouter**, §5), `roles_reels` |

Ce que la collecte **ne mesure pas** : si un autre pick aurait gagné. Une victoire dépend de trop d'autres choses ; aucun taux de victoire ne sera tiré de ce relevé.

## 2. Minimisation des données

Enregistré, et seulement sur ton PC (`%LOCALAPPDATA%\DraftMid\validation-drafts.csv`) :
- date, version de DRAFT, ton rôle, ton pool ;
- les **noms de champions** adverses visibles pendant la sélection ;
- ce que DRAFT a affiché (adversaire direct, confiance, hypothèses, conseil) ;
- tes corrections manuelles et le champion que tu as noté dans le journal ;
- la phase suivante (`InGame` si la partie a démarré).

Jamais enregistré :
- aucun nom de joueur, Riot ID, PUUID ou identifiant de partie ;
- aucune clé Riot, aucun mot de passe ni port du client LoL ;
- aucun ban, chat, rang ou donnée d'un autre joueur que les champions visibles par tous.

**Durée de conservation :** le fichier reste tant que tu le gardes ; le supprimer arrête tout. **Partage :** aucun envoi automatique. Si tu choisis un jour de partager un relevé (avec un testeur ou pour un rapport), c'est un fichier que tu envoies toi-même, après l'avoir relu.

## 3. Règles Riot applicables

- La collecte n'utilise **que** ce que DRAFT lit déjà : la phase de sélection et les champions verrouillés, via le client local en lecture seule (voir [LCU.md](LCU.md)). Aucun appel supplémentaire à l'API Riot, aucune clé.
- Aucun profilage de joueurs : seuls des noms de champions sont gardés, sans lien avec une personne.
- **Non tranché, à vérifier avant toute collecte entre plusieurs utilisateurs :** regrouper des relevés de plusieurs personnes, ou les publier, sort de l'usage local. Cela demanderait leur accord écrit et une lecture des [règles développeur de Riot](https://developer.riotgames.com/policies/general) dans le cadre de l'application Production (voir [INTEGRATION-RIOT.md](INTEGRATION-RIOT.md)). Ce protocole ne couvre que **ta propre** collecte locale.

## 4. Déroulé

### Avant la première partie
1. **Réglages > Avancé > Diagnostic** : cocher (c'est ce qui active le relevé ; décocher l'arrête).
2. Garder la même version de DRAFT et le même profil pendant toute une série ; sinon, l'écrire dans `remarques`.
3. Jouer normalement. Rien à faire pendant la sélection.

### Après chaque partie (30 secondes)
Remplir, sur la dernière ligne du fichier :
- `mid_reel` : **l'adversaire direct réel**, c'est-à-dire le champion d'en face sur ton rôle (le nom de colonne date de l'époque où DRAFT ne gérait que le mid) ;
- `roles_reels` (si tu as le temps) : `top=…,jungle=…,mid=…,adc=…,support=…`, d'après l'écran de fin ou l'historique ;
- en cas de doute (échange de lanes, rôle incertain) : écrire `?`. La partie n'est alors pas comptée.

### Lire les résultats
```powershell
.\tools\validation-report.ps1
```
Le script ne modifie pas le fichier. Il donne l'adversaire direct par confiance et par rôle, les erreurs « affiché → réel » les plus fréquentes, et les champions vus sur plusieurs rôles.

## 5. Ce qu'il faut ajouter au relevé (PR E, avec l'affichage des rôles)

Aujourd'hui le relevé ne garde pas les rôles prédits pour les 5 adversaires : on ne peut comparer que l'adversaire direct. Ajout proposé, à faire **dans la PR E** (cohérence des rôles affichés), car il touche `web/index.html` (`sendValidationRow`) et l'en-tête de `src/DraftMid.cs` (`ValidationHeader`) :
- colonne `roles_predits` : rôle le plus probable de chaque adversaire et sa probabilité, par exemple `Garen=top:0.92,Kog'Maw=mid:0.41` ;
- ajoutée **en fin de ligne**, pour que les anciens fichiers restent lisibles ;
- le script de rapport calculera alors la précision par rôle, la calibration et les flex mal reconnus, comme le banc synthétique.

## 6. Seuils de décision

| Nombre de drafts renseignées | Ce qu'on peut en faire |
|---|---|
| Moins de 50 | Rien de chiffré. Repérer des cas surprenants à ajouter au jeu de **développement** du banc. |
| 50 à 100 | Tendances, avec leur intervalle (par exemple 90 % sur 100 drafts : entre 83 et 94 % environ). |
| 100 et plus, sur un même patch | Comparer deux versions du moteur. Prérequis pour toute PR V2.18 qui modifie un poids ou un seuil. |

Garde-fous :
- les drafts réelles ne servent **jamais** à régler le moteur et à le juger en même temps : une partie des drafts (une sur cinq, choisie par la date) est mise de côté pour l'évaluation ;
- tes drafts ne représentent que ton rang, ton serveur et tes rôles ;
- un résultat sur données réelles s'écrit toujours avec n et l'intervalle, et ne se mélange jamais aux chiffres synthétiques du banc.

## 7. Relecture des conseils à l'aveugle (facultatif)

Pour juger le **conseil** et pas seulement les rôles, sans se fier au score du moteur :
1. Choisir 30 drafts du relevé.
2. Pour chacune, présenter la draft et deux champions du pool, **dans un ordre tiré au hasard**, sans dire lequel DRAFT conseillait.
3. Une personne qui connaît bien le rôle choisit, ou répond « équivalent ».
4. Compter l'accord avec DRAFT, et surtout les cas de désaccord net, qui deviennent des cas du jeu de développement.

Les avis sont des jugements, pas des statistiques réelles : ils s'écrivent comme tels, avec le nombre de relecteurs.

## 8. Ce qui n'est pas prévu

- Pas de télémétrie, pas d'envoi automatique, pas de serveur de collecte. Toute évolution dans ce sens demanderait ton accord explicite.
- Pas de récupération automatique des rôles réels par l'API Riot (MATCH-V5) : ce serait possible avec ta propre clé, mais cela ajoute un appel réseau et une donnée de partie. À décider séparément.
