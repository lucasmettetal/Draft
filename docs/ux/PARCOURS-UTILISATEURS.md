# Parcours utilisateurs (DRAFT 2.16.1)

Huit parcours, de l'installation à la mise à jour. Pour chacun : point de départ, actions, écrans, ambiguïtés, clics, risque d'erreur, améliorations.

**Convention de comptage.** Une action = un clic, ou une saisie validée par Entrée. « Observé » : mesuré dans la vraie WebView2 lancée pour l'audit. « Estimé » : déduit du code, faute de client LoL, de clé Riot ou de vraie mise à jour. Les captures sont dans [captures/](captures/).

## Synthèse

| Parcours | Actions | Nature | Risque principal |
|---|---|---|---|
| A. Installer et comprendre | 3 à 6 avant le premier écran | Estimé (installation), observé (premier écran) | Ne pas comprendre que DRAFT vit près de l'horloge |
| B. Choisir Mid et ajouter Ahri, Fizz, Kog'Maw | 5 au mieux | Observé | Saisie perdue si le rôle n'est pas choisi d'abord |
| C. Modifier les champions d'un autre rôle | 4 + 1 par champion, + 1 pour revenir | Observé | Le rôle actif change sans le vouloir |
| D. Importer et consulter l'historique | 10 à 15 dans DRAFT, plus la clé | Estimé après le dialogue | Chercher les résultats au mauvais endroit |
| E. Simuler une draft | 3 à 7 | Observé | Écraser la draft réelle |
| F. Vraie sélection | 1 à 7 | Estimé (e2e simulé) | Perdre la draft en ouvrant l'analyse |
| G. Régler Windows, LCU, affichage | 3 à 6 | Observé (écran), estimé (menu) | Ne pas trouver le réglage, ou le chercher au mauvais endroit |
| H. Vérifier et installer une mise à jour | 2 à 3 | Estimé (code) | Aucun retour quand on lance depuis le menu |

## A. Installer et comprendre

**Départ.** Page de téléchargement GitHub, fichier `DRAFT-Setup.exe`.

**Actions et écrans (estimé, sauf mention).**

1. Lancer `DRAFT-Setup.exe`. Hypothèse : exe non signé par un éditeur reconnu, donc avertissement SmartScreen (« Informations complémentaires », puis « Exécuter quand même ») : 2 clics.
2. Sans le composant WebView2 : boîte « DRAFT a besoin du composant gratuit Microsoft Edge WebView2 » (`src/DraftMid.cs:2390`). La personne doit installer un composant puis relancer.
3. Installation silencieuse dans `%LOCALAPPDATA%\Programs\DraftMid`, puis « DRAFT est installé. Tu le retrouves dans le menu Démarrer. » : 1 clic (`src/DraftMid.cs:2488`).
4. DRAFT démarre. Environ une seconde après, la fenêtre principale s'ouvre **sur les réglages** (observé : [01](captures/01-premier-lancement-reglages.png)).

**Clics.** 1 à 3 avant le premier écran (estimé), plus 1 pour lancer le fichier.

**Ambiguïtés.**

- Le premier écran dit « DRAFT 2.16.1 bêta : ton assistant stratégique… Non approuvé par Riot Games. », puis « Réglages ». Rien n'explique ce que DRAFT fera : des panneaux apparaîtront à côté du client pendant la sélection.
- Derrière la modale, la fenêtre « Analyse détaillée » affiche « Aucun pick ». Deux messages se superposent.
- Rien ne dit que, fenêtre fermée, DRAFT reste actif près de l'horloge.
- Aux lancements suivants, **aucune fenêtre ne s'ouvre** (observé : aucune page WebView2 créée). Hypothèse : l'icône est rangée dans la zone de dépassement de la barre des tâches, donc DRAFT semble absent.

**Risque d'erreur.** Élevé. Fermer les réglages sans choisir de rôle. Croire que DRAFT ne s'est pas lancé. Le relancer donne « DRAFT est déjà ouvert : utilise son icône près de l'horloge. » : c'est le seul endroit où l'icône est expliquée.

**Améliorations.**

- Premier lancement guidé en trois étapes : ton rôle et tes champions, la lecture du client (avec le risque dit simplement), un essai des panneaux.
- Le raccourci du menu Démarrer ouvre toujours la fenêtre principale, sur « Accueil ».
- Une bulle unique à la première fermeture : « DRAFT reste actif près de l'horloge. Les panneaux s'ouvriront pendant ta prochaine sélection. »

## B. Choisir Mid et ajouter Ahri, Fizz, Kog'Maw

**Départ.** Premier lancement, réglages ouverts (observé).

**Actions observées.**

1. Clic « Mid » dans « Mes profils » : le bouton devient « Mid 3 » après ajouts ; douze suggestions de mid apparaissent.
2. Saisie « ahri » + Entrée.
3. Saisie « fizz » + Entrée.
4. Saisie « kog » + Entrée (alias reconnu : Kog'Maw).
5. Clic « Fermer ».

Résultat : [03](captures/03-pool-mid-rempli.png), puis fenêtre principale [07](captures/07-fenetre-principale-vide.png) avec Ahri conseillée.

**Clics.** 5 (observé), si le rôle est choisi en premier.

**Ambiguïtés.**

- **Saisie avant le rôle.** Saisie « ahri » + Entrée sans rôle : le champ se vide, rien n'est ajouté, aucun message (observé : [02](captures/02-ajout-sans-role.png)). Le champ est pourtant actif et propose « Ahri ».
- Aucun bouton « Terminé » : « Fermer » enregistre, mais rien ne le dit, sauf une phrase en bas (« Ils sont enregistrés automatiquement »).
- Le résultat (le conseil) est caché par la modale pendant toute la saisie.

**Risque d'erreur.** Moyen à élevé au premier lancement (saisie perdue). Faible ensuite.

**Améliorations.** Champ désactivé tant qu'aucun rôle n'est choisi, avec la consigne. Page « Mon pool » plutôt qu'une modale, avec un aperçu « Ton conseil sans adversaire : Ahri ».

## C. Modifier les champions d'un autre rôle

**Départ.** Profil Mid actif, réglages ouverts.

**Actions observées.**

1. Clic « Top » dans « Mes profils ».
2. Saisie « darius » + Entrée.

**Observation.** Après l'étape 1, le rôle actif passe à Top (en haut de la fenêtre : « Top »). La recommandation devient Darius ([04](captures/04-autre-role-top-devient-actif.png)). Pour revenir : clic « Mid ».

**Clics.** 2 + 1 par champion + 1 pour revenir à Mid + 1 pour fermer (observé). Depuis la fenêtre principale fermée : + 2 (menu de l'icône, « Réglages »), estimé.

**Ambiguïtés.**

- Le même bouton sert à « voir ce rôle » et à « jouer ce rôle ». Rien ne le signale.
- Les exclusions et le favori (« Mes picks », plus bas) ne valent que pour le rôle actif.

**Risque d'erreur.** Élevé. Oublier de revenir à Mid, puis recevoir des conseils de top à la sélection suivante. La lecture du client corrige le rôle si LoL l'attribue et si l'option le permet, sinon non.

**Améliorations.** Page « Mon pool » à cinq colonnes ou cinq onglets. Le rôle actif est un réglage distinct (« Rôle joué : Mid »). Modifier un pool ne change jamais le rôle joué.

## D. Importer et consulter l'historique

**Départ.** DRAFT près de l'horloge. Aucune clé Riot utilisée pour l'audit.

**Actions.**

1. Clic droit sur l'icône, « Réglages » : 2 clics (estimé, code `src/DraftMid.cs:422`).
2. Faire défiler jusqu'à « Mon historique Riot », clic « Importer mon historique Riot » : 1 clic (observé).
3. Dialogue Windows « DRAFT : importer mon historique Riot » (observé : [18](captures/18-dialogue-import-historique.png)) : comptes `Nom#TAG`, clé, région, nombre de parties, case « J'accepte cette lecture », « Importer ». Environ 6 actions.
4. Hors de DRAFT : obtenir une clé sur developer.riotgames.com. Estimé à plus de 5 clics, compte Riot requis. Une clé de développement expire après 24 heures : à refaire à chaque import (fait connu des clés de développement Riot, non vérifié ici).
5. Attente : 100 parties, environ 3 minutes. Progression dans les réglages et bulle à la fin (code `src/DraftMid.cs:766-799`).
6. Consulter : fenêtre principale, onglet « Conseils » (« Le plus joué dans ce rôle », « Expérience dans ce rôle »).

**Clics.** 10 à 15 dans DRAFT (estimé), plus l'obtention de la clé.

**Ambiguïtés.**

- **Où sont mes parties ?** L'onglet qui s'appelle « Parties » n'affiche pas l'historique Riot. Il affiche le journal DRAFT et des statistiques d'un autre import. Vide, il dit : « Lance draft-import-stats.bat, placé dans le même dossier que cette page » (observé : [16](captures/16-onglet-parties-vide.png)).
- Trois chemins d'import coexistent : l'historique Riot (réglages), les statistiques par fichier (menu de l'icône, « Importer mes statistiques... »), le journal (réglages, « Choisir un fichier »).
- La clé est demandée dans trois dialogues distincts : historique, rôles, matchups ([19](captures/19-dialogue-cle-matchups.png)).

**Risque d'erreur.** Élevé : croire que l'import a échoué parce que « Parties » reste vide.

**Améliorations.** Page « Mes statistiques » qui réunit journal et historique Riot, avec l'état de l'import. Retirer la consigne du `.bat`. Un seul dialogue de clé, qui dit ce qui sera lancé.

## E. Simuler une draft

**Départ.** DRAFT près de l'horloge.

**Actions observées.**

1. Menu de l'icône, « Analyse détaillée » : 2 clics (estimé).
2. Saisir des adversaires (« syndra », « ornn », « lee », « jinx » + Entrée) : 4 actions. Ou « Draft aléatoire » : 1 clic.
3. Lire le conseil, les onglets Plan, Analyse, Conseils, Duel.

Résultat : [08](captures/08-simulation-4-adversaires.png), [13](captures/13-draft-aleatoire-fenetre.png).

**Clics.** 3 (aléatoire) à 7 (cinq saisies), observé pour la saisie.

**Ambiguïtés.**

- Le mot « simulateur » n'apparaît nulle part. La fenêtre s'appelle « Analyse détaillée » dans le menu, « DRAFT » dans Windows.
- La simulation est **partagée avec les panneaux** : « Draft aléatoire » remplit aussi les panneaux de draft ([14](captures/14-draft-aleatoire-repercutee-panneau.png)).
- Échap dans le champ vide efface toute la draft, sans annulation (observé).

**Risque d'erreur.** Élevé pendant une vraie sélection.

**Améliorations.** Page « Simulateur » à état propre, avec bandeau « Simulation : ta draft en cours n'est pas modifiée ». Bouton « Reprendre la draft en cours » pour copier la vraie draft dans le simulateur.

## F. Vraie sélection

**Départ.** Client LoL ouvert, file d'attente acceptée. Le client n'était pas lancé pour l'audit.

**Preuve utilisée.** Mode `--e2e` : faux client, LCU simulé, état réel des panneaux. Résultat : « TOUT EST BON ». Panneaux visibles 11 ms après la détection, aucun vol de focus, panneaux masqués au lancement de la partie.

**Actions (estimé).**

- **Avec la lecture du client** (case à cocher dans les réglages, désactivée par défaut) : 0 action pour les adversaires. 1 clic sur « Je joue X ». 1 clic plus tard sur le résultat.
- **Sans la lecture du client** : détection par l'image, qualifiée d'« estimation » (« Draft en cours (estimation) », `src/DraftDetection.cs:23`). 5 saisies dans le panneau gauche, puis 1 clic « Je joue X ».
- **Panneaux qui ne viennent pas** : Ctrl+Alt+D, ou menu « Afficher les panneaux », ou « C'est une draft : retenir cet écran ».

**Clics.** 1 à 7 (estimé).

**Ambiguïtés.**

- Sur le panneau droit, trois signaux se contredisent : « Choix serré », « Recommandé », « Presque à égalité » (observé : [12](captures/12-panneau-droit-draft.png)). Textes revus par la PR D.
- La roue dentée ouvre tous les réglages ; la croix masque les panneaux sans dire comment les rappeler.

**Risque d'erreur.** Élevé : **ouvrir « Analyse détaillée » depuis le panneau vide la draft du panneau** (observé 16 fois sur 18, [21](captures/21-panneau-vide-apres-analyse.png)). Avec la lecture du client, la draft ne revient qu'au prochain pick.

**Améliorations.** Corriger la synchronisation en premier (voir FEUILLE-DE-ROUTE-UX, UX-1). Panneau réduit à la draft. La croix affiche une fois « Ctrl+Alt+D pour les rappeler ».

## G. Régler Windows, LCU, affichage

**Départ.** DRAFT près de l'horloge.

**Actions.**

- **Depuis le menu de l'icône** (estimé) : « Activation automatique pendant la draft », « Positionnement automatique », « Panneau unique », « Replacer les panneaux », « Démarrer avec Windows » : 2 clics chacun.
- **Depuis les réglages** (observé : [05](captures/05-reglages-milieu.png)) : 2 clics pour ouvrir, défilement d'environ 700 px, puis 1 clic par case : « Lire la draft depuis le client LoL », « Quand LoL t'attribue un autre rôle », « Afficher les panneaux automatiquement », « Coller les panneaux au client LoL », « Un seul panneau », « Toujours au premier plan », « Démarrer avec Windows ».

**Clics.** 3 à 6 (observé dans la page, estimé pour le menu).

**Ambiguïtés.**

- Les mêmes réglages existent à deux endroits, sous deux noms : « Positionnement automatique » (menu) et « Coller les panneaux au client LoL » (page) ; « Activation automatique pendant la draft » et « Afficher les panneaux automatiquement pendant la draft ».
- La lecture du client est dite « Recommandé », mais elle est désactivée par défaut et suivie de « tu l'actives à tes risques ».
- « Toujours au premier plan » n'existe que dans la page ; « Replacer les panneaux » que dans le menu.

**Risque d'erreur.** Moyen : régler au mauvais endroit, ou ne pas trouver.

**Améliorations.** Réglages en sous-sections (Général, Draft et client LoL, Panneaux, Données, Mise à jour, Avancé). Menu de l'icône limité aux actions immédiates.

## H. Vérifier et installer une mise à jour

**Départ.** DRAFT près de l'horloge. Aucune vraie mise à jour n'a été lancée.

**Actions (d'après le code, `src/DraftMid.cs:1757-1822` et `src/AppUpdater.cs`).**

1. Menu de l'icône, « Rechercher une mise à jour... » : 2 clics. Ou réglages, section « Mise à jour », bouton « Rechercher une mise à jour » : 3 clics.
2. Bloqué pendant une draft, une partie, un import ou un calcul : message d'état (« Une draft ou une partie est en cours : relance la mise à jour après. »).
3. Si une version existe : boîte « La version X est disponible (tu as la 2.16.1)… Installer maintenant ? » : 1 clic.
4. Téléchargement, vérification de signature, essai de la nouvelle version (`--selftest`), remplacement, redémarrage. Bulle « Mise à jour installée : version X. » Retour automatique à l'ancienne version si la nouvelle ne démarre pas, avec une bulle d'avertissement.

**Clics.** 2 à 3 (estimé).

**Ambiguïtés.**

- **Depuis le menu, le résultat n'est visible que dans les réglages.** « DRAFT est à jour », une erreur réseau ou un blocage pendant une draft s'écrivent dans l'état de la page. Si les réglages sont fermés, rien ne s'affiche. Le menu montre seulement « Mise à jour en cours... » pendant la recherche.
- La seule mention de la confidentialité (« DRAFT ne contacte GitHub que lorsque tu cliques ») est dans les réglages.

**Risque d'erreur.** Faible pour les données (conservées, retour arrière automatique). Élevé pour la confiance : un clic sans réponse.

**Améliorations.** Bulle courte pour chaque résultat lancé depuis le menu. Carte « Mise à jour » sur l'Accueil : version, date de dernière vérification, résultat.
