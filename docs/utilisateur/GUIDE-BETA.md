# DRAFT : guide de la bêta privée

*Ton assistant stratégique pour League of Legends.*

Merci de tester DRAFT. Cette version est une **bêta privée** : elle peut contenir des erreurs, et tes retours servent à les trouver.

**DRAFT n'est ni approuvé ni soutenu par Riot Games.** Il n'agit jamais dans le client : tu choisis, bannis et verrouilles toujours toi-même.

## Lancer DRAFT

1. Copie `DRAFT-Portable.exe` dans un dossier de ton choix.
2. Lance-le. Windows peut afficher un avertissement, car l'exécutable n'est pas signé.
3. Aucune fenêtre ne s'ouvre : une icône **DRAFT** apparaît près de l'horloge. Clic droit dessus pour le menu.

Il faut Windows 10 ou 11, avec Microsoft Edge WebView2 (déjà présent sur la plupart des PC). S'il manque, DRAFT le dit au lancement.

**Aucune clé Riot, aucun compte et aucun historique ne sont nécessaires.**

## Configurer tes champions

1. Menu de l'icône, puis **Réglages**.
2. Dans **Mes profils**, choisis un rôle, puis ajoute tes champions avec la recherche.
3. Recommence pour chaque rôle que tu joues. Chaque rôle garde son propre pool, enregistré automatiquement.

## Changer de rôle

En haut de chaque panneau, et en haut de la fenêtre principale, cinq icônes de rôle. **Un clic suffit.** Le rôle actif est écrit à gauche.

Ta composition adverse reste en place quand tu changes de rôle. Seul le conseil change.

## Sauvegarder tes profils

Dans **Réglages, Mes profils** :

- **Exporter mes profils :** télécharge `draft-profils.json`, avec tes rôles, tes champions et quelques préférences. Ni clé, ni historique de parties, ni journal.
- **Importer :** relit un fichier exporté. Un fichier abîmé ou incomplet est refusé sans rien changer, et l'état précédent est sauvegardé avant tout remplacement.
- **Annuler le dernier import :** revient à l'état d'avant.

## Essayer sans partie : les simulations

Dans la fenêtre principale (menu de l'icône) :

- **Draft aléatoire** remplit une composition adverse au hasard.
- Tu peux aussi ajouter des adversaires à la main avec la recherche, puis corriger le rôle supposé d'un adversaire d'un clic.
- Un champion choisi dans une simulation n'est pas noté dans ton journal : tes statistiques ne comptent que les vraies drafts.

## Mettre à jour DRAFT

Dans **Réglages**, ou dans le menu de l'icône : **Rechercher une mise à jour**. Si une version plus récente existe, DRAFT te demande confirmation, la télécharge, la vérifie, puis redémarre. Tes profils, ton historique et tes réglages sont conservés.

DRAFT ne cherche jamais de mise à jour tout seul, et refuse pendant une draft, une partie, un import ou un calcul. Si quelque chose échoue, ta version actuelle reste en place.

## Ce qui est expérimental

| Fonction | État | Activée par défaut |
|---|---|---|
| Lecture du client LoL | Lecture seule de la phase, des adversaires verrouillés et de ton poste. **Riot n'a pas confirmé que c'est autorisé** | **Non** |
| Changement de profil selon le poste attribué par LoL | Dépend de la lecture du client. Lit le champ `assignedPosition`, non documenté par Riot | Non (proposition seulement, si la lecture est active) |
| Import de ton historique Riot | Avec ta propre clé, saisie par toi, jamais enregistrée | Non : seulement à ta demande |
| Conseils pédagogiques | Écrits à la main, pas encore validés par des joueurs. Ceux à confirmer sont signalés | Oui, en lecture seule |
| Le plus joué, égalités | Affichage seulement : le classement ne change pas | Oui |

## Limites connues

- Les conseils comparent tes champions selon des règles écrites à la main. Ce n'est ni un winrate, ni une probabilité de victoire.
- Le client ne donne pas les rôles adverses : ton adversaire direct est une supposition, à corriger d'un clic.
- La lecture de ton poste n'a été testée qu'avec des réponses simulées.
- Les clés Riot de développement expirent après 24 heures.

## Signaler un bug

Envoie à la personne qui t'a donné DRAFT :

1. Ce que tu faisais, ce que tu attendais, et ce qui s'est passé.
2. Une capture d'écran si possible.
3. La version, affichée en haut des réglages.
4. Si DRAFT ne démarre pas : le fichier `%LOCALAPPDATA%\DraftMid\draftmid.log` (ce nom vient de l'ancienne version).

**N'envoie jamais ta clé Riot**, ni ton fichier d'historique, ni tes identifiants.

## Tes données

Tout reste sur ton PC, dans `%LOCALAPPDATA%\DraftMid`. Ce nom de dossier vient de l'ancienne version et reste le même pour ne rien perdre. Aucune donnée n'est envoyée automatiquement. Seules les illustrations des champions sont chargées depuis le service officiel de Riot (Data Dragon). GitHub n'est contacté que si tu cliques sur **Rechercher une mise à jour**.
