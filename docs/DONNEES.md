# Données de DraftMid

D'où vient chaque chiffre, à quel point s'y fier, et ce que l'application en fait. La priorité est la fiabilité de l'interprétation, pas la quantité.

## Audit des sources

Mesures faites le 4 novembre 2026 sur les fichiers réels.

| Source | Type | Provenance | Patch et date | Observations | Limites | Réutilisation | Impact |
|---|---|---|---|---|---|---|---|
| Noms, portraits, patch | Officiel | Riot, Data Dragon | Dernier patch connu | Sans objet | Hors ligne, le patch peut être en retard | Données publiques de Riot | Affichage, âge des données |
| Rôles livrés | Estimation externe | Meraki Analytics | Patch 16.3, soit 19 patchs d'écart avec le 16.22 | Inconnues : ce sont des taux, sans nombre de parties | Beaucoup de champions n'ont qu'un rôle : Teemo y est 100 % top, 65 % dans les parties observées | **Licence non confirmée** | Référence pour les rôles |
| Rôles calculés | Observé | API Riot, clé personnelle | Calcul du 4 novembre, patch et région non enregistrés par l'ancien format | 1 000 parties, 9 999 lignes. Médiane de 50 par champion, 13 champions sous 20, 86 sous 50 | Un seul calcul, Silver à Émeraude, classé solo/duo | Sur le PC du joueur, sans nom de joueur | Rôles adverses |
| Bilans de matchups | Observé | API Riot, clé personnelle | Jamais calculés avec une vraie clé | 0 | Écart à la moyenne des joueurs suivis, pas un winrate général | Sur le PC du joueur, sans nom de joueur | Critère le plus lourd du classement |
| Tableau de matchups | Écrit à la main | Projet | Aucun | Aucune | Mid et 4 champions seulement | Propre au projet | Critère le plus lourd pour ce profil |
| Fiches de champions | Écrit à la main | Projet | Aucun | Aucune | Avis général, à revoir si un champion est refait | Propre au projet | Règles du classement |
| Statistiques importées | Observé | API Riot, comptes du joueur | Selon l'import | Selon l'import | Peu de parties par matchup, centrées sur le mid | Privées, jamais publiées | Affichage seulement |
| Cache de l'application | Copie | Fichiers ci-dessus | Celle de la source | Celles de la source | Le calcul précédent écrasait le résultat | Local | Aucun en lui-même |

**Problèmes constatés :**
- Meraki n'a aucun nombre de parties, donc aucune taille d'échantillon. Ses « 100 % » rendaient des rôles presque impossibles.
- L'ancien seuil basculait d'un coup entre deux sources à 20 parties.
- L'ancien calcul de rôles n'enregistrait ni patch, ni région, et un nouveau calcul remplaçait le précédent.
- Une mauvaise supposition du mid faisait porter tout le matchup sur un seul adversaire.
- Quand deux champions avaient les mêmes chiffres, l'ordre de parcours décidait seul : avec Sett, Galio et Ahri, c'était Ahri.

## Règles de gestion

**Types.** Observé, estimation externe et écrit à la main restent distincts partout. Une valeur inconnue reste inconnue : aucun patch, aucune région ni aucun nombre de parties n'est inventé.

**Fraîcheur.** Le patch actuel compte en entier, le précédent moitié moins, et ainsi de suite jusqu'à 2 %. Sans patch connu, l'âge est estimé d'après la date et compte comme tel. Une source est dite ancienne à partir de 3 patchs d'écart. Rien n'est supprimé.

**Rôles d'un champion.** Les parties observées s'ajoutent à une référence pesant 10 parties. Cette référence est mêlée à un peu d'uniforme, davantage si elle est ancienne : un rôle rare reste possible. Peu de parties ne peuvent donc pas renverser la référence.

**Changement de méta.** Il faut au moins 30 parties sur le patch actuel et 30 avant, un écart d'au moins 20 points, et un écart statistiquement net (z ≥ 3). Alors, les anciennes parties comptent dix fois moins, sans être effacées. Un écart à la seule référence Meraki reste « à confirmer ». Aucune modification de code n'est nécessaire à chaque patch.

**Répartition des rôles.** Toutes les répartitions compatibles sont pesées, soit 120 au plus pour 5 champions. Les rôles non pris restent pour les champions non révélés. Les corrections manuelles l'emportent, et deux corrections contradictoires sont ignorées plutôt que de bloquer.

**Confiance affichée.** Élevée, moyenne ou faible, jamais en pourcentage. Elle est plafonnée à moyenne si les données du candidat sont anciennes, trop rares ou limitées à la référence.

**Adversaire affiché.** Avec une confiance faible, personne n'est présenté comme l'adversaire : l'interface écrit « Mid non identifié », cite les hypothèses, et le journal n'enregistre aucun nom. Le classement continue de les peser.

**Recommandation.** Sont gardés les adversaires à au moins 10 %, et « pas encore choisi » à au moins 10 %. Le score est la moyenne pondérée des hypothèses, avec les mêmes règles stratégiques. À 90 % ou plus, une seule hypothèse reste et le calcul est identique à l'ancien. La stabilité du verdict n'a pas changé. Le conseil est dit provisoire si la confiance est faible ou si les hypothèses mènent à des picks différents.

## Collecte

- Compteurs par patch, lu dans la partie elle-même, avec date et région.
- Reprise après interruption, avec un enregistrement toutes les 25 parties.
- Les parties déjà lues ne sont jamais redemandées. Une partie n'est marquée lue qu'après son téléchargement.
- Déjà assez de parties lues récemment : aucune requête n'est envoyée.
- L'ancien fichier est sauvegardé en `.bak`, et l'ancien calcul reste une référence distincte.
- Fichier de reprise abîmé : mis de côté, puis le comptage repart de zéro pour ne rien compter deux fois.
- Limites de Riot : 1 300 ms entre les requêtes, attente sur le code 429, arrêt après 5 refus de suite.
- Fichiers de reprise dans `%LOCALAPPDATA%\DraftMid`, avec des numéros de partie et des compteurs, sans joueur ni clé. Jamais publiés.

Aucune collecte automatique depuis OP.GG ou une autre plateforme.

## Retirer Meraki

`build.ps1 -SansMeraki` compile sans ce fichier. Les rôles habituels écrits à la main (`web/draft-role-sheet.js`) prennent alors le relais, et le moteur fonctionne sans autre changement. Retirer le fichier du dépôt ne l'efface pas de l'historique Git.

## Ajouter une source

Ajouter une entrée à `DATA_PROVIDERS` dans `web/draft-data.js`, avec ce qu'elle fournit, sa provenance, ses conditions d'utilisation et `describe()`. Selon le cas : `roleBuckets()`, `rolePrior(id)` ou `matchupBuckets()`. Le moteur stratégique et les réglages n'ont pas à changer. Une source sans autorisation claire ne doit pas être ajoutée.

## Vérification sur 3 000 drafts simulées

Les drafts sont tirées des fréquences de rôles des 1 000 parties calculées, avec l'adversaire réel connu. Le tirage est indépendant d'un rôle à l'autre : les compositions réelles ne sont pas reproduites. **Ce n'est pas une mesure sur de vraies drafts.** Le test « avec les parties » utilise les mêmes données que le tirage, donc il est optimiste. Le test « référence seule » est plus indépendant.

| 5 champions, 1 500 drafts | Juste | Faux | Aucune supposition |
|---|---|---|---|
| Avec les parties, ancien | 1 343 | 157 | 0 |
| Avec les parties, nouveau | 1 342 | 158 | 0 |
| Référence seule, ancien | 1 162 | 305 | 33 |
| Référence seule, nouveau | 1 174 | 292 | 34 |

La supposition n'est donc pas plus juste à 5 champions. Le gain porte sur l'incertitude :

| Confiance affichée, avec les parties | Drafts | Supposition juste | Vrai cas parmi les hypothèses |
|---|---|---|---|
| Élevée | 1 443 | 93 % | 97 % |
| Moyenne | 722 | 79 % | 97 % |
| Faible | 835 | 11 % | 97 % |

Avec la référence seule, la confiance ne dépasse jamais moyenne : 85 % de suppositions justes, contre 18 % en faible.

Probabilités annoncées contre réalité, à 5 champions : 44 / 48, 63 / 69, 83 / 84 et 98 / 97 avec les parties. Avec la référence seule : 41 / 43, 63 / 55, 84 / 79 et 95 / 96.

Point négatif : quand le vrai adversaire n'est pas encore révélé, le nouveau calcul nomme à tort un champion plus souvent (155 fois contre 114 sur 746). L'hypothèse « pas encore choisi » reste alors pesée dans le classement si elle atteint 10 %.

## Limites restantes

- Le calcul complet n'a jamais tourné sur l'API Riot : seulement sur des réponses simulées.
- Aucune évaluation sur de vraies drafts, ni sur la qualité finale des conseils.
- Les seuils (poids de la référence, 30 parties, z ≥ 3) sont des choix prudents, non calibrés sur plusieurs patchs réels.
- La licence Meraki n'est pas confirmée, alors que le fichier est livré dans le dépôt public.
- 1 000 parties laissent la moitié des champions sous 50 observations.
- Fiches et tableau restent des avis écrits à la main.

## Besoins en données officielles

- Clé de production, pour calculer rôles et matchups à l'avance et les fournir sans clé personnelle.
- Confirmation écrite sur l'usage du LCU pendant la sélection.
- Pour remplacer Meraki : statistiques propres, par patch et par rang.

Une fois la réponse de Riot reçue : une tâche planifiée publierait un fichier par patch, ajouté comme une source de plus, puis les fiches et le tableau pourraient être remplacés champion par champion par des mesures.
