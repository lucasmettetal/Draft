# Synchronisation entre la fenêtre et les panneaux

La fenêtre principale et les panneaux (gauche, droit, compact) sont des pages distinctes, parfois dans des processus WebView2 différents. Elles partagent la draft en cours par `BroadcastChannel("draftmid")`, dans `web/index.html`.

## Messages

| Message | Envoyé par | Effet |
|---|---|---|
| `{ kind:"hello" }` | Une page qui s'ouvre | Les pages déjà synchronisées répondent par leur état |
| `{ kind:"state", … }` | Une page après chaque changement (`publish`) | État partagé, profils, préférences, versions |

Une page qui s'ouvre ne diffuse rien avant d'avoir reçu un état, ou avant 1,5 s sans réponse (`SYNC_WAIT_MS`). Sinon, sa draft vide écraserait la vraie.

## Ordre : une version par groupe

Depuis le correctif de la 3.0.0-beta.1 (`fix/beta1-sync-panneaux`), une ancienne information ne remplace jamais une sélection plus récente. Avant, la règle était « dernier message reçu ». Un message en retard, livré deux fois ou rejoué pouvait alors ramener un ancien champion consulté, un ancien choix ou un ancien rôle.

Les champs synchronisés sont rangés en groupes. Chaque groupe porte une version `[compteur, page]` :

| Groupe | Contenu |
|---|---|
| `draft` | Adversaires, vis-à-vis, rôles adverses, sélection, notice, sélection en cours, clé de la sélection (`sessionKey`, beta.2) |
| `pick` | Champion confirmé et son entrée de journal (`current`, `picked`, `pickContext`) |
| `viewed` | Champion consulté |
| `role` | Rôle joué (`PROFILES.active`), rôle à la main, proposition ignorée, bascule et rôle d'avant |
| `pools` | Pools, exclusions et favori de chaque rôle |
| `prefs` | Préférences |

**Règles :**
- **Changement local :** à l'envoi, chaque groupe qui a changé depuis le dernier envoi ou la dernière réception reçoit `[compteur + 1, page]`. Le compteur dépasse tous ceux que la page a vus (horloge de Lamport).
- **Réception :** un groupe n'est pris que si sa version est plus récente. Plus récent veut dire un compteur plus grand ; à compteur égal, l'identifiant de page le plus grand, ce qui donne le même gagnant dans toutes les pages.
- **État de départ** (stockage, page qui s'ouvre) : version 0. Il ne remplace rien de ce qu'une autre page a changé.
- **Pools et rôle joué** ont chacun leur version. Un panneau qui change de rôle sans avoir encore reçu un pool modifié ailleurs n'impose pas son ancien pool.
- **Nouvelle sélection** (correctif beta.2) : le programme envoie `draftStart { draft }` à TOUTES les pages, fenêtre comprise, et la clé est aussi dans son état (`draftKey`). Chaque page remet la draft à zéro une seule fois par clé (`startSelection`) : consultation, choix, entrée en cours, rôle à la main. Une page déjà passée à cette clé, par elle-même ou par la synchronisation (`sessionKey`), ne recommence pas. Une page qui ne reçoit pas le message (créée plus tard, message perdu, panneau en pause) voit la nouvelle clé dans l'état du programme. Avant, le message n'allait qu'au premier panneau prêt et se perdait s'il n'y en avait aucun.
- **Poste attribué par League :** il vient toujours du programme, qui l'envoie à chaque page. L'état d'une autre page ne le remplace jamais.
- **« Dans League »** (beta.2, ton pick lu dans le client) : même règle. Le programme l'envoie à chaque page (`league`), avec sa propre version `v` : une page ignore un état plus ancien (clé de sélection plus petite, ou même clé et `v` plus petit). Il n'entre dans aucun groupe synchronisé. Le conseil mémorisé au verrouillage est gardé une fois par sélection (`draftLockMemo`).
- **Champions indisponibles** (beta.2 : bannis, pris par un allié, pris en face) : même règle. Le programme envoie à chaque page `unavailable { v, key, items: [{ id, reason }] }`, versionné comme `league`. Il n'entre dans aucun groupe synchronisé : chaque page le reçoit du programme et l'applique seulement à la sélection en cours (`draftKey`), hors simulation, avec la lecture du client activée. Voir [LCU.md](LCU.md).
- **Compatibilité :** un message sans `v` (page d'une version précédente) est appliqué entièrement, comme avant. Les champs du message ne changent pas ; `v` et `from` s'ajoutent.
- **Simulation :** la draft en cours suivie à part (`liveCopy`) garde, groupe par groupe, la version la plus récente. Au retour, elle remplace l'état de la simulation.

## Tests

`web/draft-sync-tests.html` ouvre trois vraies pages (fenêtre, panneau droit, compact) et perturbe la livraison dans la page de test seulement :
- messages reçus à l'envers ;
- double livraison ;
- rafale de 10 changements en 900 ms sur les trois pages, avec des retards variés ;
- changements simultanés ;
- page ouverte en retard, puis ancien message rejoué ;
- draft suivante ;
- pool modifié pendant un changement de rôle ;
- message sans version.

## Limites

- Dans un même groupe, deux changements simultanés de champs différents ne fusionnent pas : un seul l'emporte.
- La réception ne réécrit pas le stockage du navigateur. C'est la page qui fait un changement qui l'enregistre.
- Un changement fait dans une page avant sa synchronisation (1,5 s au plus après son ouverture) n'est pas diffusé.
