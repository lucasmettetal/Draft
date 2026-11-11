# Tester la 2.16.2 sur un autre PC (15 minutes)

But : vérifier sur le PC où les bugs sont apparus que :
- DRAFT se connecte au client LoL ;
- le diagnostic s'explique clairement ;
- fermer les réglages ne fait plus disparaître DRAFT.

**Rien à installer d'autre, aucun mot de passe ni clé à donner.**

## Avant de commencer
- La personne garde ses données : la mise à jour les conserve.
- Si DRAFT est déjà installé : clic droit sur l'icône DRAFT près de l'horloge → **Rechercher une mise à jour...** → **Oui**. Sinon, télécharger `DRAFT-Portable.exe` depuis la page de la release 2.16.2.
- Vérifier la version : **Réglages**, en haut : « DRAFT 2.16.2 bêta ».
- Dans **Réglages**, cocher **Lire la draft depuis le client LoL**.

## Test 1 : connexion au client LoL
1. Lancer le client LoL **normalement**, attendre l'écran d'accueil.
2. Ouvrir **Réglages** dans DRAFT et regarder l'état de la lecture du client.

| Ce qu'on voit | À noter |
|---|---|
| « Connecté au client LoL » | ✅ Test réussi |
| « Client ouvert, mais connexion impossible » | ❌ Faire le test 2 |

3. Refaire avec le client lancé **en tant qu'administrateur** (clic droit sur le raccourci de LoL → Exécuter en tant qu'administrateur), DRAFT restant lancé normalement. Noter le résultat de la même façon.

## Test 2 : diagnostic (à faire dans tous les cas)
1. Client LoL ouvert, sur l'accueil.
2. Clic droit sur l'icône DRAFT près de l'horloge → **Diagnostic de connexion LoL...**
3. Une fenêtre s'affiche, et le texte est **déjà copié**. Il suffit de le coller dans un message (Discord, mail) et de l'envoyer.

Le texte ne contient ni mot de passe, ni port, ni chemin, ni pseudo. Il se termine par une ligne « Conclusion : ».

## Test 3 : fermer les réglages
Après chaque essai, noter si **une fenêtre DRAFT reste visible** et si **l'icône DRAFT est toujours près de l'horloge**.

| Essai | Résultat attendu |
|---|---|
| Ouvrir la fenêtre principale (clic droit sur l'icône → Analyse détaillée), puis Réglages, puis **Fermer** | La fenêtre principale reste ouverte |
| Même chose, mais fermer les réglages avec **Échap** | Pareil |
| Pendant une sélection de champions, ouvrir les réglages avec ⚙ sur un panneau, puis **Fermer** | Les réglages disparaissent, les panneaux restent |
| Ouvrir les réglages pendant la sélection, attendre que la partie démarre, puis **Fermer** | La fenêtre principale reste visible (avant : tout disparaissait) |
| Fermer la fenêtre principale avec la **croix Windows** | La fenêtre se masque, **l'icône reste près de l'horloge** (comportement voulu ; un message l'expliquera dans une prochaine version) |

Si DRAFT semble avoir disparu : regarder l'icône près de l'horloge (éventuellement dans la flèche « ^ »). Si elle n'y est plus du tout, noter ce qui a été fait juste avant. Envoyer aussi le fichier `%LOCALAPPDATA%\DraftMid\draftmid.log`, qui ne contient que des erreurs techniques, sans secret.

## Test 4 : la draft n'est plus effacée
1. Pendant une sélection (ou avec les panneaux affichés via le menu de l'icône), saisir 2 ou 3 champions adverses.
2. Cliquer **Analyse détaillée**, fermer la fenêtre, recommencer 5 fois. Faire de même avec **Comparer**.
3. Les champions saisis doivent toujours être là.
4. Dans la fenêtre principale, cliquer **Draft aléatoire** :
   - un bandeau « Simulation » apparaît ;
   - les panneaux gardent la vraie draft ;
   - **Revenir à la draft en cours** la retrouve.

## Ce qu'il faut renvoyer
- Les résultats des tests 1, 3 et 4 (✅ / ❌).
- Le texte du diagnostic (test 2).
- En cas de problème : la version de Windows et le fichier `draftmid.log`.
