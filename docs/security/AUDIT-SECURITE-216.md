# Audit de sécurité de DRAFT 2.16.0

*Rapport rédigé le 10 novembre 2026. Il ne contient aucun secret. DRAFT n'est pas, et ne sera jamais, « 100 % sécurisé » : ce rapport décrit ce qui a été vérifié, avec quelles preuves, et ce qui reste ouvert.*

## 1. Résumé exécutif

- **Une vulnérabilité confirmée, de sévérité MOYENNE (V1), corrigée.** Un fichier `draft-stats.js` importé par le menu « Importer mes statistiques » s'exécutait comme du JavaScript dans la page de confiance de DRAFT. Ce code pouvait :
  - envoyer des commandes au code natif ;
  - lire les données de la page (profils, historique importé, journal) et les faire sortir par le réseau ;
  - afficher une fausse interface ;
  - se relancer à chaque démarrage.
  
  Le scénario a été démontré sur l'exécutable publié v2.16.0, dans un dossier isolé. Aucune exécution de code Windows n'a été trouvée par ce chemin.
- **Quatre faiblesses de sévérité FAIBLE corrigées :**
  - V2 : redirection de la clé Riot ou du mot de passe LCU ;
  - V3 : formules dans le relevé CSV ;
  - V4 : chaîne GitHub Actions ;
  - V5 : réponses de mise à jour non plafonnées.
- **Défense en profondeur ajoutée :**
  - origine des messages WebView2 vérifiée ;
  - liens externes limités à une liste ;
  - iframes refusées ;
  - CSP sur l'interface ;
  - analyse CodeQL continue.
- **Aucun secret** dans l'historique Git (49 commits) ni dans les fichiers suivis. La clé privée de signature est hors du dépôt.
- **Aucune dépendance embarquée ne fait l'objet d'un avis de sécurité connu.**
- **Points bloquants avant une distribution plus large :** aucun critique. Restent la protection de la clé de signature (R1) et la sécurité du compte GitHub (R2), à traiter par toi.

## 2. Commit et version audités

| | |
|---|---|
| Commit audité | `8c6d909` (`main`, le 10/11/2026) |
| Code de la release | identique au tag `v2.16.0` (`d08801c`) : seuls `README.md` et une note de documentation diffèrent |
| Exécutable publié | `DRAFT-Portable.exe` 2.16.0.0, empreinte égale au `digest` GitHub, signature DRAFT valide |
| Corrections | branche `security/audit-216`, à partir de `8c6d909` |

## 3. Outils et compétences réellement utilisés

| Outil | Utilisé | Résultat |
|---|---|---|
| Plugin **claude-security** | **Non lancé** | Il exige l'outil Workflow, absent de cette session. Aucune analyse n'a été simulée à sa place. |
| Plugin **security-guidance** | Non | Pas installé dans cette session |
| **/security-review** (revue de la différence avec un agent indépendant) | Oui, sur les corrections | Aucune faille introduite. A signalé deux copies non relues, corrigées depuis (voir V1) |
| **CodeQL** 2.27.4 (GitHub Actions, `security-extended`, C# sans compilation, JavaScript) | Oui | C# : 0 alerte (63 règles). JavaScript : 1 alerte dans une page de test non livrée (faux positif F2) |
| **Gitleaks** 8.30.1 (release officielle, empreinte SHA-256 vérifiée) | Oui | Historique : 0 fuite. Fichiers suivis : 0. Alertes seulement dans `dist-beta/`, ignoré par Git (F3) |
| **Semgrep** 1.185.0 (PyPI, environnement isolé, `p/csharp`, `p/javascript`, `p/secrets`, `p/github-actions`) | Oui | 39 alertes : 1 réelle (V4), 38 faux positifs (F1) |
| GitHub Advisory Database | Oui | Aucun avis pour React 18.3.1, ReactDOM 18.3.1, Framer Motion 11.18.2, htm, SDK WebView2 1.0.4314.35 |
| Lecture manuelle du code, démonstrations non destructives | Oui | Base de ce rapport |

## 4. Périmètre et limites

**Dans le périmètre :**
- `src/` : WebView2, imports, mise à jour, API Riot, LCU, registre, installation ;
- `web/` : page, scripts, bibliothèques embarquées ;
- `tools/` : signature, clé, tests ;
- `.github/workflows/` ;
- l'historique Git ;
- l'exécutable publié.

**Limites :**
- **Analyses manquantes :** pas d'analyse Claude Security (outil absent), et CodeQL a analysé le C# sans le compiler, ce qui réduit le nombre de règles.
- **Pas de test d'intrusion dynamique** sur un vrai client League of Legends, ni d'analyse du binaire WebView2 Runtime, qui est mis à jour par Windows.
- **Mise à jour :** jamais testée de bout en bout entre deux releases publiées (voir le plan au §12).
- **Sources externes :** l'état de la LCU et des règles Riot est celui de la documentation publique ; les politiques peuvent évoluer.
- **Ce que l'audit ne prouve pas :** l'absence de toute autre vulnérabilité.

## 5. Modèle de menaces

**Actifs :**
- clé privée de signature (`%APPDATA%\DRAFT-signature`) ;
- clé Riot de développement (en mémoire, le temps d'une opération) ;
- mot de passe LCU (lu dans le `lockfile`, en mémoire) ;
- données locales `%LOCALAPPDATA%\DraftMid` (profils, Riot ID, historique, journal) ;
- intégrité de l'exécutable et de la valeur `Run` ;
- compte GitHub et releases.

**Processus et privilèges :** DRAFT tourne avec les droits de l'utilisateur, jamais en administrateur. L'installation se fait dans `%LOCALAPPDATA%\Programs` et ne touche qu'à `HKCU`. Les processus de WebView2 sont isolés par Edge.

**Frontières de confiance :**
1. **Page WebView2 et code natif :** `postMessage`, 19 commandes. La page de confiance est `https://draftmid.local/`, servie depuis le dossier `app`.
2. **Fichiers de données et page :** scripts `draft-*.js` chargés par `<script>`.
3. **Réseau et application :** API Riot, Data Dragon, API GitHub, téléchargement de release.
4. **Processus du client LoL et DRAFT :** `lockfile`, puis HTTPS local sur `127.0.0.1`.
5. **GitHub Actions et release :** compilation, puis publication. La signature est faite hors CI.
6. **Utilisateur et fichiers importés :** `draft-stats.js` et profils JSON.

**Profils d'attaquants :**

| | Attaquant | Conclusion |
|---|---|---|
| A | Fournit un fichier d'import piégé | **Exploitable avant correction (V1).** L'import de profils JSON était déjà strict |
| B | Contrôle une réponse réseau | Demande de casser TLS. Mise à jour : signature exigée. Clé Riot : redirection fermée (V2) |
| C | Site ouvert depuis la WebView | Navigation hors de DRAFT déjà annulée. Désormais liste de sites, iframes refusées, origine des messages vérifiée |
| D | Logiciel malveillant aux droits limités | S'il a les droits de l'utilisateur, il peut déjà tout modifier, ce qui n'est pas une vulnérabilité de DRAFT. Exception : la clé privée en clair (R1) |
| E | Compte GitHub compromis | Ne peut pas faire accepter une mise à jour : la clé n'est pas sur GitHub. Peut piéger le téléchargement manuel (R2) |
| F | Contrôle un fichier local lu par DRAFT | Avec les droits de l'utilisateur, privilège équivalent. Les fichiers de données sont désormais relus strictement |
| G | Corrompt une sauvegarde, une migration ou une mise à jour | Profils : sauvegardes et relecture. Mise à jour : retour à l'ancienne version, testé. Arrêt brutal pendant la confirmation : R4 |

## 6. Vulnérabilités confirmées

### V1 — Exécution de JavaScript dans la page de confiance via `draft-stats.js` — MOYENNE, confiance élevée — **corrigée**

**Fichiers :**
- `src/DraftMid.cs:1574-1585` : `ImportStats` ne vérifie que le début du fichier (`StartsWith("window.DRAFT_STATS")`), puis le copie tel quel.
- `src/DraftMid.cs:2708-2715` : `Extract` le recopie à chaque démarrage.
- `web/index.html:679` : `<script src="draft-stats.js">`.
- `src/DraftMid.cs:957` : aucune vérification de l'origine des messages.

**Conditions :** l'utilisateur importe un fichier fourni par un tiers (par exemple « mes stats ») avec « Importer mes statistiques… ».

**Démonstration (non destructive, dossier isolé, exécutable publié v2.16.0) :**
- le fichier contient `window.DRAFT_STATS = {"rows":[]}; window.chrome.webview.postMessage({cmd:"setDiagnostics", value:true});` ;
- résultat : `settings.json` passe à `Diagnostics = true`, et le fichier reste dans les données, donc il se relance à chaque démarrage.

**Impact réel :** du JavaScript arbitraire s'exécute dans la page de confiance.
- **Il peut :** envoyer les commandes natives (démarrage automatique de DRAFT, lecture du client, réglages, ouverture de la fenêtre d'import, copie des profils) ; lire et faire sortir par le réseau les profils, le Riot ID et l'historique importé ; afficher une fausse demande de clé ; ouvrir un site `https://` dans le navigateur.
- **Il ne peut pas :** exécuter un programme, écrire un fichier arbitraire, lire la clé Riot (saisie dans une fenêtre Windows) ni le mot de passe LCU.

**Correctif :** nouveau fichier `src/DataScripts.cs`.
- Chaque fichier de données chargé par la page est relu comme un objet JSON strict (`window.NOM = {…};`, commentaires `//` permis, 64 Mo au plus), sans clé `__proto__`, puis **réécrit par DRAFT**. Seules des données atteignent la page.
- Un fichier refusé n'est ni copié ni laissé dans le dossier de la page, et il est journalisé. L'original reste dans les données.
- C'est appliqué à l'import, au démarrage, après un import d'historique et après un calcul de rôles ou de matchups.
- En complément : origine des messages vérifiée, liste de sites pour les liens, iframes refusées, CSP.

**Risques de régression :** un fichier de statistiques non standard est refusé. Les vrais fichiers ont été testés sur des copies : 96 parties de l'essai V2.15 relues, rôles calculés du dossier actuel publiés.

**Non-régression :**
- 9 tests de `DataScriptsTests` : code ajouté avant ou après les données, expressions, `__proto__`, imbrication, chevrons, fichier piégé ni copié ni effacé ;
- 3 tests d'origine, de liens et de CSV ;
- démonstration rejouée sur la version corrigée : `Diagnostics = false`, fichier refusé et journalisé, et un fichier valide toujours publié.

### V2 — La clé Riot et le mot de passe LCU pouvaient suivre une redirection HTTP — FAIBLE, confiance moyenne — **corrigée**

**Fichiers :** `src/RoleRatesUpdater.cs:48` (en-tête `X-Riot-Token`) et `src/LcuConnector.cs:82-84` (`Authorization`).

**Condition :** une réponse 3xx de `*.api.riotgames.com`, ce qui suppose un serveur Riot ou TLS compromis. Sur `127.0.0.1`, cela suppose un processus local qui se fait passer pour le client.

**Impact :** l'en-tête personnalisé pouvait être renvoyé vers un autre hôte.

**Correctif :** `AllowAutoRedirect = false` sur les deux requêtes. Une redirection devient un échec, géré comme une erreur.

**Régression :** aucune, puisque l'API Riot ne redirige pas. Import simulé validé dans le scénario complet.

### V3 — Formules dans le relevé CSV de diagnostic — FAIBLE — **corrigée**

**Fichier :** `src/DraftMid.cs:667`.

**Condition :** mode diagnostic actif, plus une valeur venant de la page qui commence par `=`, `+`, `-` ou `@`.

**Impact :** une formule s'exécute à l'ouverture du fichier dans Excel.

**Correctif :** `CsvCell` préfixe ces cellules par `'`.

**Test :** `Relevé CSV : formules neutralisées`.

### V4 — Chaîne GitHub Actions — FAIBLE — **corrigée**

**Fichier :** `.github/workflows/build.yml:8` (`contents: write` pour tous les push) et `:13` (`actions/checkout@v4`, étiquette modifiable).

**Condition :** une étiquette d'action déplacée par un tiers compromis.

**Impact :** du code tiers tournait avec un jeton capable d'écrire dans le dépôt et les releases.

**Correctif :**
- lecture seule par défaut ;
- écriture réservée au job `release` (tags `v*`) ;
- actions épinglées sur un commit ;
- `persist-credentials: false` ;
- `gh release create` échoue si la release existe, donc pas de remplacement par la CI.

**Test :** le build sur la branche a réussi. Le job `release` n'a pas été exécuté, puisqu'aucun tag n'a été créé.

### V5 — Réponses de mise à jour lues sans limite — FAIBLE — **corrigée**

**Fichier :** `src/AppUpdater.cs:298` et `:317` (`ReadToEndAsync`).

**Impact :** une saturation de la mémoire seulement, depuis GitHub.

**Correctif :** `ReadLimited`, qui plafonne à 2 Mo pour la release et 16 Ko pour la signature.

**Test :** `Mise à jour : réponse réseau plafonnée`.

## 7. Risques plausibles non confirmés

| Id | Risque | Pourquoi non confirmé | Recommandation |
|---|---|---|---|
| R1 | **Clé privée de signature en clair** dans `%APPDATA%\DRAFT-signature` (droits limités à ton compte), et une copie sur un autre disque | Exige déjà un code malveillant avec tes droits, mais l'impact serait maximal : des mises à jour signées frauduleuses, à condition de pouvoir aussi publier sur GitHub | Chiffrer la clé avec une phrase de passe, ou la garder sur un support retiré du PC ; chiffrer le disque (BitLocker) ; ne jamais la synchroniser dans un cloud |
| R2 | **Téléchargement manuel non authentifié** : sans certificat Authenticode, un compte GitHub compromis pourrait remplacer les liens du README ou publier une release piégée | La mise à jour intégrée refuserait cette version, mais pas un nouvel utilisateur | Double authentification GitHub ; règles de protection des tags `v*` ; releases immuables ; plus tard, un certificat de signature de code |
| R3 | **Options de test dans l'exécutable** (`--test-update <fichier>` installe un fichier local sans signature, `--run-key`, `--install-dir`, `--data`) | Il faut lancer DRAFT avec ces arguments, donc avoir déjà tes droits | Plus tard, les exclure des builds de release (`#if`) |
| R4 | **Arrêt brutal** entre l'échange des fichiers et la confirmation de démarrage : au lancement suivant, la nouvelle version démarre sans surveillance de retour | Les données ne sont jamais touchées. Une version cassée resterait installée jusqu'à une réinstallation manuelle | Acceptable pour une bêta ; documenté |
| R5 | **Certificat non vérifié pour la LCU** (`LcuConnector.cs:84`), limité à la requête vers `127.0.0.1` | Pour tromper DRAFT, un processus local devrait se faire passer pour le client, donc avoir tes droits | Garder cette exception strictement locale (c'est le cas) |
| R6 | **Réponses Riot et LCU lues sans limite de taille** | Saturation de la mémoire seulement, depuis des serveurs authentifiés ou locaux | Plafonner comme V5 à l'occasion |
| R7 | **CSP avec `'unsafe-inline'`** pour les scripts (le code de l'interface est dans `index.html`) | La CSP n'est qu'une défense en profondeur ; V1 est fermé par la relecture stricte | Plus tard, sortir le script principal dans un fichier pour retirer `'unsafe-inline'` |
| R8 | **LCU et vignette de détection** : conformité aux règles de Riot non confirmée | Question de règles, pas de sécurité | Voir `COMPLIANCE-QUESTIONS.md` du site |

## 8. Faux positifs écartés

- **F1** — Semgrep `unsafe-path-combine` (38) : `Path.Combine` dans les classes de test ou avec des noms de fichiers fixes, sans aucune entrée contrôlée par un tiers.
- **F2** — CodeQL `js/unvalidated-dynamic-method-call` dans `web/draft-layout-tests.html:44` : page de test, exclue de l'exécutable (`*-tests.html`).
- **F3** — Gitleaks sur le dossier de travail : 3 377 alertes « clé générique » dans les profils de navigateur créés par les tests sous `dist-beta/`, ignoré par Git et jamais publié. Les fichiers suivis : 0.
- **F4** — Contournement de `NavigationStarting` par `https://draftmid.local@site` : impossible, le préfixe exige le `/` final (vérifié par test).
- **F5** — Hôte de redirection du téléchargement accepté par suffixe `.githubusercontent.com` : seul GitHub contrôle ces sous-domaines, et le fichier est de toute façon vérifié par empreinte et par signature.
- **F6** — `MATCHUPS_LIVE` assemblé par concaténation (`RoleRatesUpdater.cs:619-625`) : le rôle est validé, les noms viennent de l'API Riot sur HTTPS, et le fichier passe désormais par la relecture stricte avant la page.

## 9. Ce qui était déjà correctement protégé

- **Mise à jour intégrée :**
  - jamais de recherche sans clic ;
  - releases brouillon ou préversion ignorées ;
  - adresse limitée aux releases du dépôt ;
  - vérification de la taille, de l'en-tête PE, du SHA-256 annoncé par GitHub, de la version interne égale au tag, puis d'une signature RSA 3072 liée au tag et à l'empreinte (pas de rejeu d'un ancien fichier, pas de retour à une version plus ancienne) ;
  - fichier mal signé supprimé sans être lancé ;
  - autotest en dossier isolé ;
  - échange réversible et retour automatique si la nouvelle version plante ou se fige (testé).
- **Clé de signature :** jamais dans le dépôt ni dans la CI, seule la partie publique est embarquée (vérifié par test).
- **Clé Riot :** masquée, jamais enregistrée ni écrite dans un journal, envoyée en en-tête et jamais dans l'adresse ; pauses de 1,3 s, `Retry-After` respecté sur 429, arrêt sur 401 ou 403.
- **LCU :** désactivée par défaut, liste blanche de deux adresses en lecture seule imposée par le code (une interface qui n'expose qu'une lecture), mot de passe masqué dans les journaux.
- **Import de profils JSON :** validation stricte, sauvegarde avant remplacement, retour en arrière ; aucune clé, aucun historique ni aucun journal dans les exports.
- **WebView2 :** outils de développement désactivés hors tests, aucun objet hôte exposé, navigation hors de DRAFT annulée, liens externes seulement en `https://`.
- **Registre :** `HKCU` uniquement ; valeur `Run` entre guillemets, ce qui évite le chemin non cité.
- **Interface :** React échappe les textes ; pas de `dangerouslySetInnerHTML` ; la seule écriture `innerHTML` est un message statique ; pas d'`eval` dans le code ni dans les bibliothèques.
- **Données :** migrations testées sur des copies, originaux jamais modifiés.

## 10. Plan de remédiation priorisé

| Priorité | Action | État |
|---|---|---|
| 1 | V1 : relecture stricte des fichiers de données et défenses de la WebView | **Fait** (branche `security/audit-216`) |
| 2 | V2 à V5 | **Fait** |
| 3 | R2 : double authentification GitHub, protection des tags `v*`, releases immuables | **À faire par toi** (réglages GitHub) |
| 4 | R1 : clé de signature chiffrée ou hors ligne, disque chiffré | **À faire par toi** |
| 5 | Publier une 2.16.1 avec ces corrections, signée, et tester la mise à jour réelle (§12) | Après fusion, avec ton accord |
| 6 | R3 : options de test exclues des builds de release | Plus tard |
| 7 | R7 : script principal hors de `index.html`, CSP sans `'unsafe-inline'` | Plus tard |
| 8 | Certificat Authenticode | À l'ouverture publique |

## 11. Tests effectués et résultats

| Test | Avant (v2.16.0) | Après correction |
|---|---|---|
| Tests natifs (`--selftest`) | 244 | **260 réussis, 0 échec** (+16 de sécurité) |
| Scénario complet dans la vraie WebView2, avec la CSP | 86 à 88 | **88 réussis, 0 échec** |
| Vraie page | 111 | **111 réussis** |
| Profils, export, import, migration | 54 | **54 réussis** |
| Conseils, égalités, maîtrise, moteur stratégique | 133 / 18 / 91 / 190 | **133 / 18 / 91 / 190 réussis** ; fichiers du moteur inchangés depuis v2.16.0 |
| Rendu (31 états, jusqu'à 29 tailles) | Tout passe | **Tout passe** |
| Mise à jour réelle (remplacement, plantage, blocage, signature réelle, GitHub) | 20 | **20 réussis** |
| Migration sur copies (essai V2.15 : 96 parties ; dossier actuel) | Relues | **Relues**, originaux intacts |
| Démonstration V1 | Commande native exécutée | **Bloquée et journalisée** |
| CodeQL, Semgrep, Gitleaks | — | Voir §3 |

Les bancs de test des pages interrogent la page avec `eval`. La CSP l'interdit, donc `'unsafe-eval'` est ajouté **uniquement dans leurs copies de travail**, jamais dans l'application.

## 12. Risques résiduels et plan de test de la mise à jour 2.16.0 → 2.16.1

**Risques résiduels :** R1 à R8 ci-dessus. Le risque principal est humain et organisationnel : la clé de signature et le compte GitHub.

**Test réel de mise à jour (à faire avec ton accord, rien n'est publié automatiquement) :**

1. **Préparer :**
   - fusionner la PR de sécurité dans `main` ;
   - passer `AssemblyVersion` et `AssemblyFileVersion` à `2.16.1.0`, et `APP_VERSION` à `2.16.1` ;
   - vérifier que le build GitHub passe.
2. **Préparer le poste :** installer la 2.16.0 publiée dans un dossier d'essai, par exemple `C:\DRAFT-essai\DRAFT-Portable.exe`, puis la lancer avec `--data C:\DRAFT-essai\data` pour ne pas toucher à tes données. Noter son empreinte.
3. **Publier :** pousser le tag `v2.16.1` (par toi), attendre la release, puis lancer `.\tools\sign-release.ps1 -Tag v2.16.1` (par toi).
4. **Avant de signer, vérifier le refus :** dans la 2.16.0 d'essai, « Rechercher une mise à jour » doit répondre « pas encore signée ».
5. **Après signature, vérifier la mise à jour :**
   - le bouton propose la 2.16.1, télécharge, vérifie et redémarre ;
   - après 5 secondes, le message « Mise à jour installée : version 2.16.1 » apparaît ;
   - le fichier `.ancien` a disparu ;
   - l'empreinte de l'exécutable est égale au `digest` de la release ;
   - les profils et les réglages du dossier d'essai sont identiques.
6. **Vérifier les contrôles :**
   - relancer la recherche donne « DRAFT est à jour » ;
   - pendant une vraie sélection (client LoL ouvert), la recherche doit refuser avec « Une draft ou une partie est en cours ».
7. **Garder une trace :** noter les résultats dans `docs/technique/VALIDATION-REELLE.md`.
