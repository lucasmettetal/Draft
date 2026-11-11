# Bugs signalés : connexion LCU et fermeture des réglages (10 novembre 2026)

## Bug 1 : « Client ouvert, mais connexion impossible »

### Ce que fait DRAFT
1. Il repère la fenêtre du client (classe `RCLIENT`, processus `LeagueClientUx`). Le nom du processus se lit quels que soient les droits : la fenêtre est trouvée, d'où « Client ouvert ».
2. Il lit le dossier du client pour ouvrir le `lockfile`, qui contient le port et le mot de passe temporaires.
3. Il interroge `https://127.0.0.1:<port>` en lecture seule (phase et session de sélection).

L'état « connexion impossible » veut dire : fenêtre trouvée (1), mais l'étape 2 ou 3 échoue.

### Cause confirmée
- À l'étape 2, DRAFT utilisait `Process.MainModule`, qui demande un accès complet au processus du client.
- Cet accès est **refusé quand le client tourne avec plus de droits que DRAFT**, par exemple un client lancé « en tant qu'administrateur », ou un raccourci réglé ainsi.
- L'exception était avalée sans trace. Le statut restait donc « connexion impossible », sans diagnostic.
- **Preuve sur ce PC :** 8 processus du même compte lancés avec plus de droits refusent `MainModule`, alors que la requête limitée (`QueryFullProcessImageName` avec `PROCESS_QUERY_LIMITED_INFORMATION`) donne bien leur chemin. Le test natif rejoue ce cas sur un tel processus quand il en existe un.
- **Ce qui reste incertain :** que le PC de la personne qui a signalé le bug soit dans ce cas n'est pas prouvé. Le nouveau diagnostic le dira.

### Correctif
- Le dossier du client se lit par requête limitée (`ClientProcess.FolderOf`), sans `MainModule`.
- **Diagnostic sûr :** chaque échec d'ouverture est noté dans `diagnostic.log`, quand le diagnostic est activé. Le motif est l'un de :
  - `processus du client inaccessible (Win32 n)`
  - `chemin du client illisible`
  - `lockfile absent`
  - `lockfile invalide`
  - `lockfile refusé`
  - `lockfile verrouillé`
  - sinon l'erreur HTTP (`http 401`, `Timeout`, `TrustFailure`, etc.).

  Il ne contient **ni chemin, ni port, ni mot de passe** ; un test le vérifie.

### Autres pistes vérifiées, non confirmées (à trancher avec le nouveau diagnostic)
| Piste | État |
|---|---|
| Port ou format du lockfile différent | Format `nom:pid:port:motdepasse:https` contrôlé ; un lockfile invalide est maintenant signalé |
| Lockfile absent (client en cours de démarrage, installation déplacée) | Nouvel essai toutes les 3 s ; maintenant signalé |
| Délai de 1,5 s trop court sur un PC lent | Hypothèse : apparaîtrait comme `Timeout` ou `RequestCanceled` |
| Proxy ou antivirus qui intercepte `127.0.0.1` | Hypothèse : apparaîtrait comme `ConnectFailure`, `TrustFailure` ou `SecureChannelFailure` |
| Architecture 32/64 bits | Écartée : DRAFT est compilé en x64 |

### Diagnostic pour une personne non technique
**Clic droit sur l'icône DRAFT près de l'horloge, puis « Diagnostic de connexion LoL... ».** Le résultat s'affiche dans une fenêtre et se copie tout seul : il suffit de le coller dans un message. Il vérifie dans l'ordre :
1. l'option de lecture du client. Si elle est décochée, le lockfile n'est pas lu, et le diagnostic s'arrête là ;
2. la fenêtre du client ;
3. l'accès au processus du client et la différence de droits avec DRAFT ;
4. le dossier du client et le lockfile (présence, format) ;
5. le proxy Windows pour l'adresse locale ;
6. la connexion TCP locale ;
7. la lecture de la phase avec le délai de DRAFT (1,5 s), puis avec un délai long (6 s), pour repérer un client lent.

Il termine par une conclusion en une phrase. Il **n'affiche jamais** le mot de passe, le port, le chemin du client ni un nom de joueur ; des tests natifs le vérifient. Il ne fait qu'une lecture de la phase, en local, comme la lecture normale.

À défaut, avec le diagnostic activé (**Réglages > Avancé**), la ligne `lcu=absent(...)` de `%LOCALAPPDATA%\DraftMid\diagnostic.log` donne l'étape en échec, sans secret.

### Reproduction sur un deuxième PC (à faire, non fait)
1. Installer la version corrigée.
2. Lancer le client LoL normalement, puis DRAFT : la connexion doit s'établir.
3. Quitter le client, le relancer **en tant qu'administrateur**, DRAFT restant normal :
   - avant le correctif : « connexion impossible » ;
   - après : connexion établie ;
   - le diagnostic indique « Accès complet au processus du client : non ».
4. Si le message persiste, lancer le diagnostic et envoyer son texte.

## Bug 2 : fermer les réglages semble fermer DRAFT

### Cycle de vie
- **La fenêtre principale** (`full`) est une vraie fenêtre Windows. Sa croix la **masque**, elle ne la ferme pas (`OnFormClosing`). DRAFT reste actif près de l'horloge. Seul « Quitter » arrête le processus (`Quit` puis `ExitThread`).
- **Les réglages** s'affichent par-dessus la page principale. Leur bouton « Fermer », Échap ou un clic à côté appellent `closeSettings`.
- **Réglages ouverts seuls pendant une draft** (`solo=1`, quand des panneaux sont affichés) : à la fermeture, la page envoie `closeFull` et la fenêtre est masquée, pour ne rien laisser devant le client.
- **`hideAll`** (croix des panneaux) ne fait qu'afficher ou masquer les panneaux.

### Causes confirmées (reproduites par le test complet, `--e2e`)
1. **La draft se termine pendant que les réglages sont ouverts.** Après « Fermer », la fenêtre principale était masquée alors qu'il n'y avait plus aucun panneau : plus aucune fenêtre DRAFT à l'écran, le processus toujours actif près de l'horloge.
   - **Correctif :** `closeFull` ne masque la fenêtre que si des panneaux sont encore affichés.
2. **La fenêtre principale est déjà ouverte pendant une draft, et la page se recharge** (après un import, par exemple) au moment où on ouvre les réglages. Le message d'ouverture est perdu. Au bout de 1,5 s, DRAFT rechargeait la page en `solo=1`, et « Fermer » faisait alors disparaître la fenêtre principale.
   - **Correctif :** ce rechargement de secours n'ajoute plus `solo=1`, puisque la fenêtre était déjà à l'écran.

### Vérifié sans défaut
- **Le processus ne s'arrête pas :**
  - la croix, Alt+F4 et un `WM_CLOSE` envoyé par un autre programme masquent seulement la fenêtre ;
  - seul « Quitter » arrête DRAFT ;
  - les exceptions de l'interface sont journalisées sans arrêter l'application.
- **Un `WM_CLOSE` brut ne détruit pas la fenêtre :** l'audit UX avait observé une destruction, mais le test 7 bis ne la reproduit pas. La fenêtre est masquée, puis les réglages se rouvrent. Aucun correctif n'est appliqué ; le test reste comme garde-fou.
- **Plusieurs panneaux :** `hideAll` (croix des panneaux) masque les panneaux, jamais la fenêtre principale.

### Non modifié, à traiter dans l'UX (audit UX, problème 7)
- La croix Windows de la fenêtre principale la masque **sans message**. Si la personne ferme la fenêtre pendant que les réglages sont ouverts, DRAFT semble fermé alors qu'il tourne toujours.
- C'est le comportement voulu, mais il n'est pas signalé. Une bulle « DRAFT reste actif près de l'horloge » à la première fermeture est proposée dans UX-1, hors de ce correctif.

## Tests
- **Diagnostic (`--selftest`) :** 4 tests :
  - option décochée ;
  - client fermé ;
  - client qui ne répond pas ;
  - absence de secret.
- **Complet (`--e2e`) :** scénario 7 bis (`WM_CLOSE` brut).
- **Natifs (`--selftest`) :** 8 nouveaux tests LCU :
  - requête limitée ;
  - processus introuvable ;
  - processus à droits supérieurs ;
  - lockfile absent, invalide, valide ;
  - absence de secret dans le diagnostic.
- **Complet (`--e2e`) :** scénarios 7 et 8 ajoutés. Le scénario 6 utilise maintenant de vrais panneaux affichés. Il forçait auparavant l'indicateur, que la boucle de visibilité remettait à faux.
  - L'ancien code échoue sur les scénarios 7 et 8.
  - Le code corrigé passe 3 fois sur 3.
- Aucune modification de `web/`, du moteur, du scoring ni du design.
