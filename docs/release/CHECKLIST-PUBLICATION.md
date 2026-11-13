# Checklist de publication : DRAFT 3.0.0-beta.1 (préversion)

À suivre dans l'ordre, par l'utilisateur. **Chaque étape demande son accord explicite** avant d'être faite. Rien de cette liste n'a été exécuté par la préparation de la branche : ni merge, ni tag, ni release, ni signature.

Repères : dépôt `lucasmettetal/Draft`, branche `release/3.0.0-beta.1`, tag prévu `v3.0.0-beta.1`. Dernière release stable : `v2.16.2`.

## 0. Avant de commencer

- [ ] La PR `release/3.0.0-beta.1` est relue. La CI de la branche est verte (compilation, selftest, catalogues, textes en dur, banc du moteur).
- [ ] Les tests manuels voulus avant publication sont faits ou explicitement reportés : [TEST-CTRL-ALT-D-REEL.md](TEST-CTRL-ALT-D-REEL.md), [TEST-DRAFT-REELLE.md](TEST-DRAFT-REELLE.md), [PROCEDURE-MULTI-ECRANS.md](../qa/PROCEDURE-MULTI-ECRANS.md).
- [ ] `src/DraftMid.cs` : `AssemblyInformationalVersion("3.0.0-beta.1")`. `web/index.html` : `APP_VERSION = "3.0.0-beta.1"`. Le selftest vérifie qu'ils sont égaux.
- [ ] `docs/release/NOTES-3.0.0-beta.1.md` existe : le workflow le lit pour les notes de la release.

## 1. Merge dans main

- [ ] Accord de l'utilisateur pour fusionner : ______
- [ ] Fusionner la PR (bouton GitHub, sans forcer).
- [ ] CI de `main` verte après le merge.

## 2. Tag

- [ ] Accord de l'utilisateur pour créer et pousser le tag : ______
- [ ] Sur le commit de merge de `main` :
  ```powershell
  git fetch origin
  git tag -a v3.0.0-beta.1 origin/main -m "DRAFT 3.0.0-beta.1, préversion de test"
  git push origin v3.0.0-beta.1
  ```
- [ ] Le tag contient un « - » : le workflow choisit la branche « préversion ».

## 3. Vérification de la CI au tag

- [ ] Actions > Build > job `release` : étapes « Compiler », « Tests natifs » (selftest), « Publier l'exécutable » vertes.
- [ ] Le journal de « Publier l'exécutable » montre `--prerelease --latest=false` et un seul fichier, `dist\DRAFT-Portable.exe`.
- [ ] Si le job échoue sur « Version de l'exe (…) différente du tag » : rien n'est publié. Corriger la version, supprimer le tag (avec accord), recommencer à l'étape 2.

## 4. Release marquée préversion et « non latest »

- [ ] Accord de l'utilisateur sur l'état de la release : ______
- [ ] Vérifier :
  ```powershell
  gh release view v3.0.0-beta.1 -R lucasmettetal/Draft --json isPrerelease,isDraft,name,assets --jq "{pre: .isPrerelease, draft: .isDraft, name: .name, assets: [.assets[].name]}"
  ```
  Attendu : `pre: true`, `draft: false`, `assets: ["DRAFT-Portable.exe"]` (pas de `DRAFT-Setup.exe`).
- [ ] La page de la release porte l'étiquette « Pre-release » et les notes de `NOTES-3.0.0-beta.1.md`, accents compris (lues depuis le fichier). Pour mémoire, les notes en ligne des releases stables arrivent abîmées (« approuvÃ© » sur v2.16.2) : défaut connu de la commande stable, laissée telle quelle.
- [ ] Noter le SHA-256 de `DRAFT-Portable.exe` publié (`gh api repos/lucasmettetal/Draft/releases/tags/v3.0.0-beta.1 --jq ".assets[] | {name, digest}"`). Il diffère de celui de la candidate locale : c'est une autre compilation du même code.

## 5. /releases/latest reste v2.16.2

- [ ] Vérifier :
  ```powershell
  gh api repos/lucasmettetal/Draft/releases/latest --jq .tag_name
  ```
  Attendu : `v2.16.2`. Documentation GitHub : « The latest release is the most recent non-prerelease, non-draft release ». Les 2.16.x interrogent cette adresse : elles restent « à jour ».

## 6. Signature par la clé réelle

La signature porte sur l'exe **compilé par la CI au tag**, téléchargé depuis la release. Jamais sur un exe compilé localement (même source, mais octets différents : horodatage et identifiant de module changent à chaque compilation).

- [ ] Accord de l'utilisateur pour signer avec la clé réelle : ______
- [ ] Sur le PC qui détient la clé, depuis le dépôt à jour :
  ```powershell
  .\tools\sign-release.ps1 -Tag v3.0.0-beta.1
  ```
  Le script télécharge `DRAFT-Portable.exe` de la release, calcule son SHA-256, signe « DRAFT-update, tag, empreinte », fait vérifier la signature par l'exe lui-même avec la clé publique qu'il embarque (`--test-signature`), puis seulement envoie `DRAFT-Portable.exe.sig`.
- [ ] Le lancement `--test-signature` de la préversion crée au plus un dossier vide `%LOCALAPPDATA%\DRAFT-beta` : sans effet (la copie initiale se fera au premier vrai lancement, tant qu'il est vide).

## 7. Envoi du .sig

- [ ] Accord de l'utilisateur pour joindre la signature : ______ (fait par le script de l'étape 6 : `gh release upload … --clobber`)
- [ ] Vérifier : `gh release view v3.0.0-beta.1 -R lucasmettetal/Draft --json assets --jq "[.assets[].name]"` → `["DRAFT-Portable.exe","DRAFT-Portable.exe.sig"]`.

## 8. Contrôle final de /releases/latest

- [ ] Refaire l'étape 5 après la signature : toujours `v2.16.2`.
- [ ] Facultatif, sur un PC avec une 2.16.2 : menu de l'icône > Rechercher une mise à jour → « DRAFT est à jour (version 2.16.2). ».

## 9. Essai de téléchargement par un testeur

- [ ] Accord de l'utilisateur pour envoyer le lien : ______
- [ ] Un testeur télécharge `DRAFT-Portable.exe` depuis la page de la préversion, compare son SHA-256 (`Get-FileHash -Algorithm SHA256`) avec l'empreinte affichée par GitHub pour ce fichier, puis suit [GUIDE-TESTEUR-3.0.0-beta.1.md](GUIDE-TESTEUR-3.0.0-beta.1.md).
- [ ] Le testeur confirme : copie de ses données 2.x annoncée à l'Accueil, sa 2.x intacte, Réglages > Mises à jour sans bouton.

## En cas de problème après publication

Toujours avec l'accord de l'utilisateur :
- passer la release en brouillon (`gh release edit v3.0.0-beta.1 --draft`) : les testeurs ne la voient plus, `/releases/latest` reste inchangé ;
- corriger sur une branche, puis publier `v3.0.0-beta.2` (même procédure). Une release publiée n'est jamais remplacée.
