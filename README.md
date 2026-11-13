# DRAFT

*Ton assistant stratégique pour League of Legends.*

Pendant la sélection, DRAFT classe les champions que tu joues face à la composition adverse, met en avant les plus adaptés et explique comment jouer chacun. **Il ne sélectionne, ne bannit et ne verrouille aucun champion : tu décides toujours.**

## ⬇️ Télécharger

**Version 2.16.2 bêta** · Windows 10 ou 11

| | Fichier | Pour qui |
|---|---|---|
| **Recommandé** | [**DRAFT-Portable.exe**](https://github.com/lucasmettetal/Draft/releases/latest/download/DRAFT-Portable.exe) | Double-clic, rien à installer |
| | [DRAFT-Setup.exe](https://github.com/lucasmettetal/Draft/releases/latest/download/DRAFT-Setup.exe) | Installe DRAFT, avec un raccourci et une désinstallation propre |

Toutes les versions : [page des releases](https://github.com/lucasmettetal/Draft/releases). Site : [draft-website-ashy.vercel.app](https://draft-website-ashy.vercel.app).

**Premier lancement :**

1. Windows peut afficher « Windows a protégé votre ordinateur », car l'exécutable n'est pas signé par un certificat : clique sur **Informations complémentaires**, puis **Exécuter quand même**.
2. La fenêtre DRAFT s'ouvre sur l'**Accueil**, avec trois étapes courtes : ton rôle et ton pool, la lecture du client LoL (recommandée, facultative), puis un essai des panneaux. Chacune se fait en un clic ou s'ignore.
3. Fenêtre fermée, DRAFT reste actif près de l'horloge : double-clic sur son icône pour revenir à l'Accueil.

Il faut Microsoft Edge WebView2, déjà présent sur la plupart des PC : DRAFT le signale au lancement s'il manque. Aucune clé Riot, aucun compte, aucun historique ne sont nécessaires.

**Mises à jour :** à partir de la 2.16, plus besoin de retélécharger. Dans les réglages ou le menu de l'icône, **Rechercher une mise à jour** : DRAFT télécharge la nouvelle version, vérifie sa signature, la teste, puis redémarre. Si elle ne démarre pas, l'ancienne revient toute seule. Depuis la 2.14 ou avant, télécharge une fois la 2.16 ci-dessus.

> DRAFT n'est ni approuvé ni soutenu par Riot Games. *DRAFT isn't endorsed by Riot Games and doesn't reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games, and all associated properties are trademarks or registered trademarks of Riot Games, Inc.*

## Fonctionnalités

| Fonction | État |
|---|---|
| Classement de ton pool face à la composition adverse, avec une alternative et les raisons | ✅ |
| Profils pour les cinq rôles, changement de rôle en un clic, export et import des profils | ✅ |
| Panneaux compacts à côté du client, ou fenêtre complète | ✅ |
| Cinq onglets : Plan de jeu, Analyse (critères, comparaison), Conseils, Duel, Parties | ✅ |
| Égalités signalées quand deux options sont très proches | ✅ |
| Saisie manuelle des adversaires, rôle supposé corrigeable d'un clic, draft aléatoire pour s'entraîner | ✅ |
| Mise à jour depuis l'application, signée et réversible | ✅ |
| Import de ton historique classé (avec ta propre clé Riot) : champions les plus joués, sans changer le classement | ✅ facultatif |
| Statistiques de rôles et de matchups calculées depuis l'API Riot (avec ta propre clé) | ✅ facultatif |
| Conseils pédagogiques (lane, milieu de partie, combats) | 🟡 écrits à la main, en relecture |
| Lecture du client LoL : adversaires verrouillés et poste attribué | 🧪 désactivée par défaut |
| Ouverture automatique des panneaux pendant la sélection | 🧪 estimation par une vignette du client |

## Avec League of Legends

**Aucune action dans le client :** ni sélection, ni bannissement, ni verrouillage, ni injection, ni lecture de la mémoire, ni information cachée.

- **Lecture du client, décochée par défaut :** des requêtes de lecture locales vers deux adresses du client (phase de jeu et sélection), pour remplir les adversaires verrouillés et proposer le profil de ton poste. **Riot n'a pas confirmé que cet usage est autorisé** : son activation se fait à tes risques. Détails : [LCU](docs/technique/LCU.md).
- **Ouverture automatique des panneaux :** DRAFT compare une vignette de 96 × 54 pixels de la fenêtre du client, gardée en mémoire et jamais enregistrée, pour estimer si une sélection commence. Désactivable dans le menu de l'icône.

## Limites

- **Le score compare tes champions entre eux :** ce n'est ni un winrate, ni une probabilité de victoire. Les règles et les poids sont écrits à la main.
- **Rôles adverses :** le client ne les donne pas. DRAFT les suppose, et tu les corriges d'un clic.
- **Matchups :** un tableau écrit à la main existe pour le mid (Twisted Fate, Fizz, Kog'Maw, Xerath). Ailleurs, le conseil repose sur des fiches générales, et sur des bilans réels si tu les calcules.
- **Validation :** la 2.16 passe ses tests automatiques, mais n'a pas encore été éprouvée par des joueurs en classé.
- **Interface en français** uniquement.

## Tes données

Tout reste sur ton PC, dans `%LOCALAPPDATA%\DraftMid` (le nom vient de l'ancienne version). Ni compte, ni télémétrie, ni serveur DRAFT.

- **Clé Riot :** facultative, saisie par toi au moment d'un import, masquée, jamais enregistrée.
- **Connexions :** illustrations depuis Data Dragon (Riot) ; API Riot seulement quand tu lances un import ou un calcul ; GitHub seulement quand tu cliques sur **Rechercher une mise à jour**.
- **Tout supprimer :** quitte DRAFT, puis supprime le dossier ci-dessus.

Politique complète : [confidentialité](https://draft-website-ashy.vercel.app/confidentialite.html).

## Versions

| Version | État |
|---|---|
| **2.16.2** (DRAFT) | **Dernière release.** Stabilité : la draft en cours n'est plus effacée par Analyse détaillée, Comparer ou le simulateur ; connexion au client LoL lancé en administrateur ; diagnostic de connexion LoL ; fermeture des réglages (voir [le détail](docs/technique/BUGS-LCU-REGLAGES.md)) |
| 2.16.1 (DRAFT) | Correctifs de sécurité (voir [l'audit](docs/security/AUDIT-SECURITE-216.md)). Se met à jour vers la 2.16.2 depuis l'application |
| 2.16.0 (DRAFT) | Profils par rôle, poste attribué, mise à jour intégrée. Se met à jour vers la dernière version depuis l'application |
| 2.14.0 (DraftMid) | Release précédente, sans Meraki ni mise à jour intégrée |
| 2.6 à 2.10 (DraftMid) | **Déconseillées** : elles peuvent contenir des données Meraki aux droits de redistribution non clarifiés. Voir [leur statut](docs/technique/ANCIENNES-RELEASES.md) |

## Compiler soi-même

Sans Visual Studio ni Node :

```powershell
git clone https://github.com/lucasmettetal/Draft.git
cd Draft
.\build.ps1
```

Les exécutables sont dans `dist\`. Les tests natifs se lancent avec `dist\DRAFT-Portable.exe --selftest --data <dossier> --out <fichier>`.

## Développement

- [Index de la documentation](docs/README.md) · [Architecture](docs/technique/ARCHITECTURE.md) · [Validation et tests](docs/technique/VALIDATION.md)
- [Profils et migration](docs/technique/PROFILS.md) · [Mise à jour intégrée](docs/technique/MISE-A-JOUR.md) · [Guide de la bêta](docs/utilisateur/GUIDE-BETA.md)

**Publier une version :** monter `AssemblyFileVersion`, pousser un tag `vX.Y.Z` (le workflow compile, teste et publie la release), puis lancer `.\tools\sign-release.ps1 -Tag vX.Y.Z` sur le PC qui détient la clé de signature. Sans signature, la mise à jour intégrée refuse la version. Voir [Branches et publication](docs/technique/BRANCHES.md).

**Publier une préversion :** `AssemblyInformationalVersion` avec un « - » (par exemple `3.0.0-beta.1`, égale à `APP_VERSION`) donne l'édition préversion : données dans `%LOCALAPPDATA%\DRAFT-beta` (copie de la 2.x au premier lancement), ni installation ni mise à jour automatique. Un tag `vX.Y.Z-suffixe` publie une release marquée préversion, jamais « latest », avec l'exe portable seul et les notes de `docs/release/NOTES-X.Y.Z-suffixe.md`. Étapes : [Checklist de publication](docs/release/CHECKLIST-PUBLICATION.md).

Aucun fichier de licence n'accompagne le projet pour l'instant : le code est public, mais sans licence de réutilisation accordée.
