# Validation de DRAFT 2.16.1

*Branche `release/2.16.1`, partie de `main` (`db60272`, fusion de la PR #3). Le commit final est indiqué dans la PR. Rapport du 10 novembre 2026. Il ne contient aucun secret. DRAFT n'est pas « 100 % sécurisé » : ce document dit ce qui a été vérifié et ce qui reste ouvert.*

**État :** candidate prête. **Ni tag ni release `v2.16.1` n'ont été créés.** Les scénarios qui exigent une release publique sont **en attente de ton autorisation** (§7).

## 1. Modifications apportées

| Fichier | Modification |
|---|---|
| `src/DraftMid.cs` | Version 2.16.1 (`AssemblyVersion`, `AssemblyFileVersion`, version affichée dans la désinstallation). Nouveau test `TestPageSecurity`, ajouté au scénario complet |
| `web/index.html` | `APP_VERSION = "2.16.1"`. Aucun autre changement : design et moteur intacts |
| `src/SecurityTests.cs` (nouveau) | Tests natifs : CSP stricte embarquée, aucune page de test embarquée, clé Riot jamais transmise lors d'une redirection |
| `tools/test-update.ps1` | Version lue dans les sources, option `-Base` pour partir d'un exécutable officiel, signature réelle vérifiée par l'ancienne et la nouvelle version |
| `web/draft-v215-tests.html` | Le test de version lit `APP_VERSION` au lieu d'attendre « 2.16.0 » |
| `README.md`, `docs/technique/ANCIENNES-RELEASES.md` | Version 2.16.1 |

**Non modifiés :**
- moteur stratégique (`web/draft-engine.js` et données : aucune différence avec `v2.16.0`) ;
- bibliothèques embarquées ;
- clé publique embarquée (identique à la `v2.16.0`) ;
- releases et tags existants.

## 2. Vulnérabilités corrigées (depuis la 2.16.0)

Elles viennent de la PR #3. Détail dans [AUDIT-SECURITE-216.md](AUDIT-SECURITE-216.md).

| Id | Sévérité | Correctif |
|---|---|---|
| V1 | Moyenne | Fichiers de données (`draft-stats.js`, historique, rôles, matchups) relus comme du JSON strict et réécrits avant la page ; origine des messages natifs vérifiée ; liens limités ; cadres refusés ; CSP |
| V2 | Faible | Clé Riot et mot de passe LCU : redirections non suivies |
| V3 | Faible | Cellules CSV commençant par `= + - @` neutralisées |
| V4 | Faible | CI en lecture seule hors publication, actions épinglées, CodeQL |
| V5 | Faible | Réponses de mise à jour plafonnées (2 Mo et 16 Ko) |

## 3. Tests exécutés sur la 2.16.1 (build local `dist-beta\r2161`)

| Suite | Résultat |
|---|---|
| Tests natifs (`--selftest`), dont 35 sur la mise à jour, 12 sur les fichiers de données et 6 de sécurité | **263 réussis, 0 échec** |
| Scénario complet dans la vraie WebView2 (`--e2e`) | **89 réussis, 0 échec** (`TOUT EST BON`), import Riot simulé compris |
| Protections de la page dans la vraie WebView2 (nouveau, dans le scénario complet) | **Réussi** (détail §4) |
| Vraie page (`draft-v215-tests.html`) | **111 réussis, 0 échec** |
| Cinq profils, export, import, migration (`draft-profiles-tests.html`) | **54 réussis, 0 échec** |
| Conseils / égalités / maîtrise | **133 / 18 / 91 réussis, 0 échec** |
| Moteur stratégique (`draft-tests.html`) | **190 réussis, 0 échec** |
| Rendu (31 états, jusqu'à 29 tailles) | **Tout passe** |
| Migration sur des **copies** de l'essai V2.15 et de ton dossier actuel | **96 parties et 3 entrées de journal relues, profil Mid relu, originaux intacts** |
| Installation puis désinstallation (dossier et clé de registre de test) | **`DRAFT.exe` 2.16.1 et raccourci installés, puis rien ne reste.** L'entrée de désinstallation réelle n'a pas été touchée |
| Mise à jour depuis **l'exécutable 2.16.0 officiel** (`tools/test-update.ps1 -Base`) | **23 vérifications réussies, 0 échec** (détail §5) |
| Tests natifs de la **2.16.0 officielle** | **244 réussis**, dont ses refus de fausse signature, d'empreinte, de version et de rejeu |

**Tests ignorés ou impossibles :**
- **Les deux tests du double écran :** un seul écran est branché ; ils sont sautés par le scénario quand c'est le cas.
- **Pas d'essai en vraie partie :** déjà fait par toi sur la 2.16.0, à ta demande. La 2.16.1 ne change ni le moteur ni l'interface.
- **La redirection du mot de passe LCU** n'est vérifiée que par lecture du code : un vrai serveur HTTPS local avec un certificat de test serait nécessaire pour l'éprouver.

## 4. Vérifications de sécurité demandées

| Exigence | Comment c'est vérifié | Résultat |
|---|---|---|
| Aucun fichier JavaScript importé n'exécute de code | Tests natifs (code avant ou après les données, expressions, fonctions) et démonstration rejouée lors de l'audit | **Vérifié** |
| Données validées avant chargement par WebView2 | Import, démarrage, après un import d'historique et après un calcul : tout passe par `DataScripts.Publish`. Test « fichier piégé ni copié, ni laissé » | **Vérifié** |
| Commandes natives seulement depuis l'interface autorisée | `IsAppPage` testé (origine exacte, faux hôtes refusés) ; navigation et fenêtres vers un site inconnu refusées dans la vraie WebView2 | **Vérifié** |
| La CSP fonctionne réellement | Dans la vraie WebView2, un `draft-stats.js` piégé posé directement dans le dossier de la page, comme si la relecture avait été contournée : `eval` **bloqué**, requête, image et cadre vers un site inconnu **bloqués** (événements de violation reçus) | **Vérifié** |
| Clé Riot et secrets LCU sans redirection | Riot : vrai serveur local qui renvoie 302, aucune requête n'atteint le second serveur. LCU : `AllowAutoRedirect = false`, lu dans le code | **Vérifié** pour Riot ; **lecture seule** pour la LCU |
| CSV sans formules exécutables | Test `Relevé CSV : formules neutralisées` | **Vérifié** |
| Réponses réseau plafonnées | Test `Mise à jour : réponse réseau plafonnée` | **Vérifié** (mise à jour). Les réponses Riot et LCU restent sans plafond (risque R6) |
| Aucune protection affaiblie pour réussir les tests | Test natif : l'exécutable contient la CSP stricte, **sans `unsafe-eval`**, et aucune page de test. `unsafe-eval` n'existe que dans les copies de travail des bancs Chrome, jamais dans `web/` ni dans l'exécutable | **Vérifié** |

**Point d'attention trouvé pendant ces tests :** le code injecté par `ExecuteScriptAsync` (outils de test) échappe à la CSP de la page. Ce n'est pas une faiblesse de DRAFT, car seul le code natif peut l'utiliser, mais un test de CSP fondé sur `ExecuteScriptAsync` donnerait un faux échec. Le test final fait donc charger le code par la page elle-même.

## 5. Mise à jour : ce qui a réellement été testé

**Depuis l'exécutable 2.16.0 publié** (empreinte égale au `digest` GitHub), vers des candidats construits localement. La source locale (`--test-update`) remplace GitHub et signe avec une clé jetable.

| Scénario | Résultat |
|---|---|
| 2.16.0 vers 2.16.1 : remplacement, redémarrage, données relues, `.ancien` et fichiers temporaires supprimés | **Réussi** |
| Même version | **Rien n'est remplacé**, « à jour » |
| Version plus ancienne (2.15.9) | **Aucun retour en arrière** |
| **D. Retour en arrière :** la candidate plante à l'ouverture | **2.16.0 remise en place et relancée**, aucun reste |
| **D. Retour en arrière :** la candidate se fige à l'ouverture | **2.16.0 remise en place et relancée** |
| Clé réelle : signature acceptée par la clé embarquée de la 2.16.0 **et** de la 2.16.1 | **Réussi** |
| Clé réelle : signature refusée pour un autre tag, et pour un autre fichier (avec la 2.16.0 et la 2.16.1) | **Réussi** |
| Lecture réelle de GitHub : dernière release v2.16.0, signée, téléchargée et vérifiée | **Réussi** |
| **C. Mise à jour corrompue** (fausse signature, empreinte, version, rejeu, autre clé) | **Réussi dans les tests natifs de la 2.16.0 officielle.** Pas encore avec une vraie release publique : voir §7 |

**Non encore réalisé (exige une release publique, donc ton accord) :** scénarios **A**, **B** et **E**, et **C** avec une vraie release.

## 6. Scanners

| Outil | Résultat sur `release/2.16.1` |
|---|---|
| Claude Security | **Indisponible** : l'outil Workflow manque toujours dans cette session. Aucune analyse n'a été simulée |
| `/security-review` (différence avec `main`) | Voir la PR (lancée après le dernier commit) |
| CodeQL (GitHub Actions, `security-extended`) | Voir la PR. Sur `main` : C# 0 alerte, JavaScript 1 alerte dans une page de test non livrée |
| Semgrep 1.185.0 | 38 alertes `unsafe-path-combine`, toutes déjà classées faux positifs (tests et noms fixes). Aucune nouvelle |
| Gitleaks 8.30.1 | Historique (toutes branches) et fichiers : **aucune fuite** |

## 7. Protocole de publication et test réel (en attente de ton accord)

Tout se fait sur une **copie** de la 2.16.0 officielle, avec un dossier de données de test : ton installation et tes données ne sont pas touchées.

**Préparation, par moi après accord :**
1. Copier la 2.16.0 officielle dans `C:\Users\lucas\Downloads\DRAFT-beta\dist-beta\essai-reel\app\DRAFT-Portable.exe`, avec ses données dans `...\essai-reel\data`.
2. La lancer avec `--data ...\essai-reel\data --no-hotkey --no-auto-panels --run-key Software\DRAFT-EssaiReel\Run`, puis y créer un profil de test et quelques réglages.

**Publication, par toi (Claude Code bloque la création de tags) :**
1. Fusionner la PR.
2. Lancer `git push origin <commit de fusion>:refs/tags/v2.16.1`. GitHub compile, teste et publie la release.

**Scénario A, avant la signature :** dans la 2.16.0 d'essai, « Rechercher une mise à jour » doit proposer la 2.16.1, puis la refuser avec « Cette version n'est pas encore signée », sans rien remplacer.

**Scénario C en réel, optionnel, par toi :**
1. Joindre une signature volontairement fausse avec `gh release upload v2.16.1 <faux .sig> --clobber`.
2. Vérifier le refus : « La signature de cette version n'est pas valide ». Le fichier téléchargé doit être supprimé sans avoir été lancé.

**Signature, par toi :** `.\tools\sign-release.ps1 -Tag v2.16.1`. Elle remplace une éventuelle fausse signature.

**Scénario B :**
1. Dans la 2.16.0 d'essai, cliquer sur « Rechercher une mise à jour » : la 2.16.1 est proposée, téléchargée, vérifiée, testée, puis DRAFT redémarre.
2. Contrôler :
   - le message « Mise à jour installée : version 2.16.1 » ;
   - le fichier `.ancien` a disparu ;
   - l'empreinte de l'exécutable est égale au `digest` de la release ;
   - les réglages et le profil de test sont identiques.

**Scénario E :** dans la 2.16.1, une nouvelle recherche doit répondre « DRAFT est à jour (version 2.16.1) ».

**Ensuite :** consigner les résultats ici et dans `docs/technique/VALIDATION-REELLE.md`, puis mettre à jour le site (§8 du rapport d'audit).

## 8. Protection de la clé de signature (étude, rien n'a été modifié)

La clé est un fichier XML en clair dans `%APPDATA%\DRAFT-signature`, accessible à ton seul compte, avec une copie sur un autre disque. **Ni la clé ni son format n'ont été touchés.**

| Option (gratuite) | Contre quoi elle protège | Simplicité | Récupération après panne | Compatibilité |
|---|---|---|---|---|
| **Chiffrement par phrase de passe** (AES-256-GCM, dérivation PBKDF2-SHA256 à 600 000 itérations, sel aléatoire) | Vol du fichier ou d'une copie (autre disque, sauvegarde, PC volé). Pas contre un logiciel espion actif au moment où tu signes | Une phrase de passe à chaque signature | Bonne : le fichier chiffré se recopie partout, il suffit de la phrase de passe. **Phrase oubliée = clé perdue** | Totale : `sign-release.ps1` déchiffre en mémoire et signe avec la même clé, donc l'application ne change pas |
| **DPAPI** (compte Windows) | Vol du disque ou du fichier hors de ta session | Transparent | **Mauvaise** : liée à ton profil Windows. Une réinstallation ou un autre PC rendent la clé illisible. Une autre copie de secours reste indispensable | Totale |
| **Support externe hors ligne** (clé USB branchée seulement pour signer) | Logiciel malveillant persistant sur le PC, la majorité du temps | Brancher le support à chaque release | Il faut **deux** supports, car un support perdu ou abîmé fait perdre la clé | Totale |
| **Droits NTFS** (déjà en place) et **BitLocker** | PC ou disque volé et éteint (BitLocker). Les droits NTFS n'arrêtent pas un logiciel qui tourne sous ton compte | Une activation, puis transparent. Vérifie que ton édition de Windows le propose | Clé de récupération BitLocker à garder à part | Totale |

**Recommandation :**
1. Garder une seule source de vérité : la clé **chiffrée par phrase de passe**, et supprimer les versions en clair, y compris sur l'autre disque.
2. Garder **deux copies chiffrées** : une sur l'autre disque, une sur un support hors ligne ou dans un gestionnaire de mots de passe.
3. Activer BitLocker sur le disque système si ton édition de Windows le permet.
4. Ranger la phrase de passe dans un gestionnaire de mots de passe.

Ça demande d'adapter `tools/sign-release.ps1` et d'ajouter un outil de chiffrement. **Je ne le ferai qu'avec ton accord**, et sans jamais afficher la clé.

## 9. Risques encore ouverts

- **R1 — Clé de signature en clair :** voir §8.
- **R2 — Téléchargement manuel non authentifié :** pas de certificat Authenticode, et le compte GitHub est critique. À faire : double authentification, protection des tags `v*`, releases immuables.
- **R3 — Options de test dans l'exécutable** (`--test-update`, `--run-key`, etc.) : il faut déjà tes droits pour les utiliser.
- **R4 — Arrêt brutal pendant la fenêtre de confirmation** d'une mise à jour : il n'y a alors pas de retour automatique, mais les données ne sont pas touchées.
- **R5 — Certificat non vérifié pour la LCU** sur `127.0.0.1`.
- **R6 — Réponses Riot et LCU non plafonnées :** saturation de la mémoire seulement.
- **R7 — CSP avec `'unsafe-inline'`** pour les scripts, car le code de l'interface est dans `index.html`.
- **R8 — Conformité de la LCU et de la vignette aux règles Riot :** non confirmée.
- **Nouveau, documenté :** du code injecté par le code natif via `ExecuteScriptAsync` n'est pas soumis à la CSP. Seuls les tests l'utilisent.

## 10. Actions qui nécessitent ton intervention

1. **Autoriser la publication :** fusionner la PR, pousser le tag `v2.16.1`, puis signer (§7).
2. **Me laisser faire les scénarios A, B et E** dans le dossier d'essai, et éventuellement C en réel.
3. **Décider pour la clé** (§8).
4. **Sécuriser le compte GitHub** (R2).
