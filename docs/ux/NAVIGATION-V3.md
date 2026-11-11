# Navigation et parcours de DRAFT 3.0 bêta (option B)

**Maquette non validée.** Ce document refait les parcours A à H de [PARCOURS-UTILISATEURS.md](PARCOURS-UTILISATEURS.md) dans la nouvelle architecture. Il part du code de référence `integration/v2.17` (2.16.2 incluse : bandeau de simulation, diagnostic de connexion). Les maquettes sont dans [maquettes/v3/](maquettes/v3/index.html), les captures dans [maquettes/v3/captures/](maquettes/v3/captures/).

**Convention de comptage** (inchangée) : une action = un clic, ou une saisie validée par Entrée. « Avant » reprend les chiffres de l'audit 2.16.1. « Après » est estimé sur les maquettes, pas mesuré : la mesure se fera avec les critères de [CRITERES-VALIDATION-V3.md](CRITERES-VALIDATION-V3.md).

## 1. Carte cible

```
Menu Démarrer, double-clic sur l'icône ─► Fenêtre principale DRAFT
┌──────────────────┬───────────────────────────────────────────────┐
│ DRAFT  bêta      │                                               │
│ Accueil          │  Contenu de la section                        │
│ Mon pool         │  (une seule section visible, titre h1)        │
│ Simulateur       │                                               │
│ Mes statistiques │                                               │
│                  │                                               │
│ Réglages         │                                               │
│ ┌──────────────┐ │                                               │
│ │● League  ok  │ │  Pied de navigation : état League             │
│ │  Rôle joué   │ │  et rôle joué, toujours visibles              │
│ └──────────────┘ │                                               │
└──────────────────┴───────────────────────────────────────────────┘
Sous 1 100 px de large (1366 px à 125 %, 1920 px à 175 %) :
la navigation devient un rail de 92 px, icône et libellé empilés. Aucun libellé ne disparaît.

Overlay de draft (pendant la sélection seulement)
┌ DRAFT        [1|2] [⚙] [×] ┐   ┌ DRAFT        [1|2] [⚙] [×] ┐
│ Composition adverse         │   │ Rôle joué  ▣▣▣▣▣             │
│ recherche, 5 lignes, profil │   │ conseil, alternative, danger │
│                             │   │ [Je joue Fizz]  [Détails]    │
└─────────────────────────────┘   └──────────────────────────────┘

Menu de l'icône (8 entrées, état compris)
  ● DRAFT : connecté, aucune draft      (grisé)
  Ouvrir DRAFT                          (gras : action du double-clic)
  Afficher les panneaux       Ctrl+Alt+D
  Simuler une draft
  Détection de la draft  ▸  C'est une draft : retenir cet écran
                            Ce n'est pas une draft : retenir cet écran
                            Copier le diagnostic de connexion
  ─────
  Réglages
  Rechercher une mise à jour
  ─────
  Quitter
```

### Sections et sous-sections

| Section | Rôle | Sous-niveaux | Maquette |
|---|---|---|---|
| Accueil | Savoir en 3 secondes si DRAFT est prêt ; premier lancement guidé | aucun ; variantes « premier lancement » et « draft en cours » | [accueil](maquettes/v3/accueil.html), [premier lancement](maquettes/v3/accueil-premier-lancement.html), [draft en cours](maquettes/v3/accueil-draft-en-cours.html) |
| Mon pool | Pools des 5 rôles, rôle joué, exclusions, favori, sauvegarde | liste des 5 rôles (choix du pool à modifier) | [mon-pool](maquettes/v3/mon-pool.html) |
| Simulateur | Préparer une draft ; voir le détail de la draft en cours | bascule de source (Simulation, Draft en cours) ; onglets Plan, Critères, Comparer, Conseils, Duel | [simulateur](maquettes/v3/simulateur.html) |
| Mes statistiques | Journal des drafts, historique Riot, anciens imports | onglets Vue d'ensemble, Journal des drafts, Historique Riot | [statistiques](maquettes/v3/statistiques.html) |
| Réglages | Tous les réglages, un seul endroit | Général, Connexion League, Affichage et fenêtres, Données et confidentialité, Mises à jour, Diagnostic et informations | [reglages](maquettes/v3/reglages.html) |

Profondeur maximale : **section, puis un niveau** (sous-rubrique ou onglet). Aujourd'hui : trois niveaux d'onglets dans une colonne de 370 px.

### Règles de navigation

1. **La section active est toujours visible** : trait turquoise à gauche, fond `--raised`, `aria-current="page"`.
2. **Le rôle joué est toujours visible** : pied de navigation, overlay, et bandeau de Mon pool. Il ne change que par un choix explicite (bouton « Rôle joué » ou proposition de League acceptée).
3. **Le mode Simulation se voit** : bandeau or permanent dans le Simulateur. L'or est réservé à « hypothèse » (simulation, choix serré, étape à faire). Le turquoise signifie « réel ou action » (draft en cours, conseil, action principale).
4. **Aucune modale de navigation.** Les modales servent à confirmer (deux boutons au plus, action nommée).
5. **Un réglage existe à un seul endroit.** Le menu de l'icône ne contient plus de case à cocher.
6. **Échap ferme ou vide, n'efface jamais une donnée.** « Nouvelle simulation » propose « Annuler » pendant 5 secondes.
7. **Une action lancée hors de la fenêtre** (menu de l'icône) répond par une bulle, et le même résultat s'affiche sur l'Accueil.

### Adresses (compatibilité)

| Adresse actuelle | Adresse 3.0 | Alias gardé |
|---|---|---|
| `?panel=full` | `?panel=full&view=home` | oui, une version |
| `?panel=full&tab=settings` (et `&solo=1`) | `?panel=full&view=settings&section=general` | oui |
| `?panel=full&tab=plan`, `analyse`, `criteria`, `compare`, `rules`, `coach`, `matchups` | `?panel=full&view=sim&source=live&tab=…` (`rules` devient l'aide « Comment DRAFT classe tes picks ») | oui |
| `?panel=full&tab=parties` | `?panel=full&view=stats` | oui |
| Message `openSettings` | `openView` `{ view, section }` | `openSettings` traduit en `openView` pendant une version |
| Commande `openFull` `{ tab }` | `openFull` `{ view, section, tab, source }` | `tab` seul accepté |
| `?panel=left`, `right`, `compact` | inchangées | — |

## 2. Parcours refaits

### Synthèse

| Parcours | Avant (2.16.1) | Après (3.0, estimé) | Gain principal |
|---|---|---|---|
| A. Installer et comprendre | 3 à 6 avant le premier écran, puis 5 pour un conseil ; aucune fenêtre aux lancements suivants | Installation inchangée ; premier conseil en **6** actions depuis le premier écran ; le menu Démarrer ouvre toujours l'Accueil | DRAFT a une « maison » et explique les panneaux |
| B. Mid + Ahri, Fizz, Kog'Maw | 5 au mieux, saisie perdue sans rôle | **5** depuis l'Accueil, **4** depuis Mon pool ; 0 échec silencieux | Champ désactivé tant qu'aucun rôle n'est choisi, confirmation visible |
| C. Pool d'un autre rôle | 5 fenêtre ouverte (+2 depuis l'icône), rôle actif changé | **3** fenêtre ouverte, **5** depuis l'icône ; rôle joué inchangé | Plus de retour à faire, plus d'erreur de rôle |
| D. Importer et consulter l'historique | 10 à 15, plus la clé ; résultats ailleurs | **9 à 10**, plus la clé ; résultats sur la même page | Un seul endroit, une seule saisie de clé |
| E. Simuler | 3 à 7, écrase la draft réelle | **3 à 7** ; draft réelle intacte | Isolation garantie, mot « Simulateur » visible |
| F. Vraie sélection | 1 à 7 ; « Analyse détaillée » vide le panneau | **1 à 7** ; « Détails » n'efface rien | 0 perte de draft |
| G. Régler Windows, LCU, affichage | 3 à 6 ; doublons sous deux noms | **3** fenêtre ouverte, **4** depuis l'icône ; un seul nom par réglage | Plus de doublon ; +2 clics pour les cases autrefois dans le menu |
| H. Mise à jour | 2 à 3 ; aucun retour depuis le menu | **2 à 3** ; bulle + carte Accueil | Chaque clic a une réponse |

### A. Installer et comprendre

**Départ.** Page de téléchargement, `DRAFT-Setup.exe`.

**Écrans.** Installation inchangée (1 à 3 actions estimées, plus le lancement). Puis la fenêtre principale s'ouvre sur l'**Accueil, premier lancement** ([capture](maquettes/v3/captures/accueil-premier-lancement-1366x768.png)) :

1. « Ton rôle et tes champions » : bouton « Choisir » vers Mon pool (1).
2. Mon pool : clic « Mid » dans la liste des rôles (1). Sans rôle joué existant, ce premier rôle devient le rôle joué, et la page le dit.
3. Saisie « ahri », « fizz », « kog » + Entrée (3). Chaque ajout affiche « Enregistré : Ahri ajouté au pool Mid ».
4. Retour à l'Accueil (1) : « Ton conseil sans adversaire : Ahri ».

**Clics.** 6 depuis le premier écran (avant : 5 dans une modale qui cachait le résultat, avec risque de saisie perdue). Les étapes 2 et 3 de l'Accueil (lecture du client, essai des panneaux) sont facultatives et ne bloquent pas le conseil.

**Ambiguïtés levées.**

- Le premier écran dit ce que fait DRAFT : « Des panneaux s'ouvrent à côté du client League, puis se masquent au lancement de la partie. »
- Il dit où DRAFT reste : « Fenêtre fermée, DRAFT reste actif près de l'horloge. » Une bulle le redit à la première fermeture (une fois).
- Aux lancements suivants, le menu Démarrer ouvre l'Accueil. Le démarrage avec Windows reste silencieux (choix explicite dans Réglages > Général).
- La lecture du client est présentée une fois, avec son risque, et c'est un choix (« Choisir »), pas une recommandation contredite.

**Risque restant.** Faible. SmartScreen et WebView2 absent restent hors de la page (voir [MIGRATION-V3.md](MIGRATION-V3.md), risques).

### B. Choisir Mid et ajouter Ahri, Fizz, Kog'Maw

**Départ.** Accueil, premier lancement.

**Actions.** « Mon pool » dans la navigation (1), « Mid » (1), trois saisies + Entrée (3). **5 actions**, ou 4 si Mon pool est déjà ouvert. Aucun bouton « Fermer » : l'enregistrement est automatique et visible à chaque ajout.

**Ambiguïtés levées.**

- Tant qu'aucun rôle n'est choisi, le champ est désactivé et dit « Choisis d'abord un rôle ». Plus d'échec silencieux.
- La recherche reconnaît les surnoms (« tf », « kata », « kog ») et l'indique sous le champ. Un nom déjà présent est signalé (« déjà dans le pool ») au lieu d'être ignoré.
- Le résultat (conseil sans adversaire) est visible sur l'Accueil, pas caché par une modale.
- Un ajout se défait : « Annuler » à côté de la confirmation.

### C. Modifier les champions d'un autre rôle

**Départ.** Rôle joué Mid. Fenêtre ouverte, ou DRAFT près de l'horloge.

**Actions.** Fenêtre ouverte : « Mon pool » (1), « Top » dans la liste (1), « darius » + Entrée (1). **3 actions.** Depuis l'icône : clic droit, « Ouvrir DRAFT » (2), puis les 3 : **5 actions**. Rien à rétablir ensuite ([capture](maquettes/v3/captures/mon-pool-1366x768.png)).

**Ambiguïtés levées.**

- Deux contrôles distincts : « Rôle joué » (segment en haut, utilisé pour les conseils) et la liste « Pool à modifier ». Sous le titre du pool : « Tu modifies le pool Top. Ton rôle joué reste Mid. »
- Exclusions et favori sont rangés **par rôle**, dans le tableau du pool, et non plus dans « Mes picks », qui ne valait que pour le rôle actif.

**Risque restant.** Nul pour le rôle joué. Le changement par League (« Quand League t'attribue un autre rôle ») reste réglable et visible dans l'overlay.

### D. Importer et consulter l'historique

**Départ.** DRAFT près de l'horloge, aucun import.

**Actions.** « Ouvrir DRAFT » (2), « Mes statistiques » (1), « Importer mon historique Riot » (1). Dialogue de clé harmonisé ([capture](maquettes/v3/captures/dialogue-cle-riot.png)) : comptes, clé, région, nombre de parties, case d'accord, « Importer » (environ 5). **9 à 10 actions**, plus l'obtention de la clé hors de DRAFT (inchangée : compte Riot, clé de développement de 24 heures).

**Pendant l'import.** Progression dans Mes statistiques (« 46 sur 100 parties », bouton « Arrêter l'import »), bulle à la fin si la fenêtre est fermée.

**Consulter.** Les parties apparaissent **sur la même page** : chiffres clés, « Le plus joué en Mid », victoires par champion. 0 action de plus (avant : ouvrir la fenêtre, onglet Conseils, chercher).

**Ambiguïtés levées.**

- L'onglet « Parties » et sa consigne `.bat` disparaissent. L'ancien import par fichier (`draft-stats.js`, journal) est replié sous « Importer un ancien fichier », sans consigne de script.
- Un seul modèle de dialogue de clé pour l'historique, les rôles et les matchups. Il dit ce qui sera lancé et combien de temps.
- La clé ne passe jamais par la page (dialogue natif), elle n'est jamais enregistrée : c'est écrit dans le dialogue et dans Réglages > Données et confidentialité.

### E. Simuler une draft

**Départ.** DRAFT près de l'horloge.

**Actions.** Menu de l'icône, « Simuler une draft » (2). Puis « Draft aléatoire » (1) ou 5 saisies. **3 à 7 actions**, comme avant ([capture](maquettes/v3/captures/simulateur-1366x768.png)).

**Ce qui change.**

- Le mot « Simulateur » est partout : navigation, titre, menu.
- Bandeau or permanent : « Simulation : ta draft en cours n'est pas modifiée. » (texte de la 2.16.2 conservé).
- « Draft aléatoire » et « Nouvelle simulation » n'agissent que sur la simulation. « Nouvelle simulation » propose « Annuler » 5 secondes.
- Pendant une vraie sélection, « Reprendre la draft en cours » **copie** la draft réelle dans la simulation (pas de lien).
- Échap dans le champ vide ne fait rien d'autre que vider le champ.

### F. Vraie sélection

**Départ.** Client League ouvert, file acceptée.

**Actions.** Avec la lecture du client : 0 action pour les adversaires, 1 clic « Je joue Fizz », 1 clic plus tard sur le résultat. Sans lecture : 5 saisies dans le panneau gauche, puis 1 clic. **1 à 7 actions**, inchangé ([capture](maquettes/v3/captures/overlay-deux-panneaux-340x720.png)).

**Ce qui change.**

- « Détails » (ancien « Analyse détaillée ») ouvre le Simulateur sur la source **Draft en cours**, sans recharger la page et sans diffuser d'état vide. Objectif mesuré : 0 perte sur 50 ouvertures (aujourd'hui 16 pertes sur 18).
- Barre d'outils de l'overlay : trois icônes SVG au lieu de caractères Unicode. « Panneau unique / deux panneaux », « Réglages d'affichage » (ouvre Réglages > Affichage et fenêtres, et non plus toute la page), « Masquer ». Au premier masquage, une bulle dit « Panneaux masqués. Ctrl+Alt+D pour les rappeler. »
- Une étiquette « Lecture du client » ou « Draft estimée » dit d'où viennent les adversaires.

### G. Régler Windows, la connexion League, l'affichage

**Départ.** DRAFT près de l'horloge.

**Actions.** Menu de l'icône, « Réglages » (2), rubrique (1), réglage (1) : **4 actions**. Fenêtre ouverte : 3. Les rubriques tiennent sur un écran de 768 px de haut ; il n'y a plus 700 px de défilement ([capture](maquettes/v3/captures/reglages-affichage-1366x768.png)).

| Réglage | Avant | Après |
|---|---|---|
| Lire la draft depuis le client | Réglages, milieu de page | Réglages > Connexion League |
| Rôle attribué par League | Réglages | Réglages > Connexion League |
| Affichage automatique des panneaux | Réglages + menu (« Activation automatique pendant la draft ») | Réglages > Affichage et fenêtres (un seul nom) |
| Coller les panneaux au client | Réglages + menu (« Positionnement automatique ») | Réglages > Affichage et fenêtres |
| Panneau unique | Réglages + menu + bouton de l'overlay | Réglages > Affichage et fenêtres + bouton de l'overlay |
| Toujours au premier plan | Réglages | Réglages > Affichage et fenêtres |
| Replacer les panneaux | Menu seulement | Réglages > Affichage et fenêtres |
| Démarrer avec Windows | Réglages + menu | Réglages > Général |
| Niveau des conseils | Réglages + onglet Conseils | Réglages > Général (lien depuis l'onglet Conseils) |
| Supposer une frontline | Réglages > Mes picks | Réglages > Général |
| Journal de diagnostic | Réglages > Avancé | Réglages > Diagnostic et informations |

**Compromis assumé.** Les cases qui étaient dans le menu de l'icône (2 clics) demandent maintenant 4 clics. Elles changent rarement ; le menu garde les actions de draft. Si un test utilisateur montre un besoin, « Panneau unique » reste à un clic dans l'overlay.

### H. Vérifier et installer une mise à jour

**Départ.** DRAFT près de l'horloge.

**Actions.** Menu de l'icône, « Rechercher une mise à jour » (2). Si une version existe, la boîte « Installer maintenant ? » (1). **2 à 3 actions**, inchangé.

**Ce qui change.** Chaque résultat lancé depuis le menu donne une bulle : « DRAFT est à jour », « Version X disponible », « Une draft est en cours : relance après », ou l'erreur réseau en clair. Le même résultat, avec l'heure, s'affiche sur l'Accueil (carte « Mise à jour ») et dans Réglages > Mises à jour.

### Parcours ajouté : I. Les panneaux ne viennent pas

**Départ.** Sélection en cours, lecture du client désactivée, panneaux absents.

| Action | Avant | Après |
|---|---|---|
| Afficher les panneaux | Ctrl+Alt+D, ou menu (2) | identique |
| Apprendre l'écran | menu (2) | menu, « Détection de la draft », puis l'entrée (3) |
| Diagnostic de connexion | menu (2), boîte copiée | menu, sous-menu (3), ou Réglages > Diagnostic |

Le sous-menu coûte un clic de plus, en échange d'un menu de 8 entrées au lieu de 16.

## 3. Ce qui n'est pas créé

- Pas de page « Analyse » : c'est le Simulateur sur la source « Draft en cours ».
- Pas de page « Données Riot » : rôles et matchups sont dans Réglages > Données ; l'historique est une statistique.
- Pas de page « À propos » : Réglages > Diagnostic et informations, et pied de navigation.
- Pas de tableau de bord décoratif : l'Accueil montre l'état, le profil, deux actions et deux cartes.
