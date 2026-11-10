# Architecture de navigation

## 1. Carte actuelle (2.16.1)

Quatre surfaces, sans lien visible entre elles.

```
Icône près de l'horloge (menu de 15 commandes)
├── [état, grisé] Client LoL fermé / Draft en cours (estimation) / ...
├── Afficher les panneaux (Ctrl+Alt+D)
├── C'est une draft : retenir cet écran
├── Ce n'est pas une draft : retenir cet écran
├── Activation automatique pendant la draft      (doublon des réglages, autre nom)
├── Positionnement automatique                   (doublon des réglages, autre nom)
├── Panneau unique                               (doublon des réglages)
├── Replacer les panneaux
├── Analyse détaillée  ──────────────► Fenêtre principale
├── Réglages  ───────────────────────► Fenêtre principale + modale Réglages
├── Importer mes statistiques...  ───► Dialogue fichier Windows
├── Mettre à jour les rôles...  ─────► Dialogue clé Riot (40 min)
├── Mettre à jour les matchups...  ──► Dialogue clé Riot (65 min)
├── Démarrer avec Windows                        (doublon des réglages)
├── Rechercher une mise à jour...                (doublon des réglages)
└── Quitter

Fenêtre principale « DRAFT » (= simulateur + copie de la draft réelle)
├── Barre : rôle actif (5 icônes), patch, Réglages, Draft aléatoire, Nouvelle draft
├── Colonne gauche : composition adverse, profil de l'équipe
├── Centre : conseil, « Ton pool » (4 meilleurs)
└── Colonne droite : bloc « Plan de jeu », puis onglets
    ├── Plan ─── Lane / Mid game / Teamfights
    ├── Analyse ─ Critères / Comparer / Règles
    ├── Conseils (niveau des conseils en double, historique Riot)
    ├── Duel
    └── Parties ─ journal DRAFT + statistiques importées

Modale « Réglages » (12 sections, 45 contrôles, 2,2 écrans de haut)
├── Mes profils : rôle, pool, export/import de profils
├── Niveau des conseils
├── Mise à jour
├── Mon historique Riot ──► Dialogue Windows (clé, comptes, consentement)
├── Détection de la draft : lecture du client, rôle attribué, affichage auto
├── Fenêtres : coller, panneau unique, premier plan, démarrer avec Windows
├── Mes picks : exclusions, favori, frontline supposée (rôle actif seulement)
├── Rôles des champions / Matchups ──► Dialogues clé Riot
├── Journal : import par bouton fichier natif
├── Sources de données (repliable)
└── Avancé (repliable) : diagnostic

Overlay de draft (2 panneaux ou 1)
├── Barre : ▭/⧉ (mode), ⚙ (tous les réglages), × (masquer)
├── Rôle actif (5 icônes)
├── Gauche : saisie et composition adverse
└── Droite : conseil, alternative ► Comparer, Analyse détaillée ► fenêtre principale
```

Problèmes de structure :

1. **Pas de point d'entrée.** La fenêtre principale ne s'ouvre qu'au premier lancement ou depuis le menu.
2. **Le pool vit dans les réglages**, entre la mise à jour et la lecture du client.
3. **La fenêtre principale a deux rôles incompatibles** : simuler et refléter la draft réelle. Elles partagent le même état.
4. **Doublons** : cinq réglages existent dans le menu et dans la page, parfois sous un autre nom.
5. **Profondeur cachée** : trois niveaux d'onglets dans une colonne de 370 px.
6. **Trois historiques** dans trois endroits.

## 2. Architecture cible

Principe : **une application principale avec une vraie navigation, et un overlay de draft séparé, compact, qui ne fait que la draft.**

```
Menu Démarrer / icône ──► Fenêtre principale DRAFT
                          ┌────────────┬──────────────────────────────────┐
                          │ Accueil    │                                  │
                          │ Mon pool   │        Contenu de la section     │
                          │ Simulateur │                                  │
                          │ Mes stats  │                                  │
                          │ ────────── │                                  │
                          │ Réglages   │                                  │
                          └────────────┴──────────────────────────────────┘

Overlay de draft (pendant la sélection seulement)
  Draft adverse · Conseil · Je joue X · [Détails] [Panneaux ⚙] [Masquer]
```

### Accueil

But : savoir en trois secondes si DRAFT est prêt.

- **État** : client LoL (fermé, ouvert, sélection en cours), lecture du client (active ou non, avec un lien vers le réglage), panneaux (automatiques ou manuels, raccourci).
- **Profil joué** : rôle et pool résumé (« Mid : Ahri, Fizz, Kog'Maw »), lien « Modifier » vers Mon pool.
- **Actions principales** : « Simuler une draft », « Voir les panneaux ».
- **Cartes d'état** : dernière vérification de mise à jour et son résultat ; données (rôles, matchups, historique) avec leur date.
- **Premier lancement** : la même page affiche trois étapes à cocher (rôle et pool, lecture du client, essai des panneaux), puis redevient l'Accueil normal.

### Mon pool

- Cinq rôles visibles (onglets ou colonnes). Modifier un rôle **ne change pas** le rôle joué.
- Pour chaque rôle : champions, exclusions temporaires, favori à égalité.
- **Rôle joué** : un choix explicite, aussi présent dans l'overlay.
- Export et import des profils, avec « Annuler le dernier import ».

Ce qui sort des réglages : « Mes profils », « Mes picks » (exclusions, favori). « Supposer une frontline dans mon équipe » reste un réglage de calcul : il va dans Réglages, section Draft.

### Simulateur

- La fenêtre principale actuelle (composition, conseil, pool classé, onglets d'analyse), mais **avec son propre état**.
- Bandeau permanent : « Simulation : ta draft en cours n'est pas modifiée. »
- Pendant une vraie sélection : bouton « Reprendre la draft en cours » (copie, pas lien).
- Onglets aplatis : Plan, Analyse (critères), Comparer, Conseils, Duel. « Règles » va dans une aide « Comment DRAFT classe tes picks ».
- « Draft aléatoire » et « Nouvelle draft » n'agissent que sur la simulation.

**Analyse de la draft réelle.** « Détails » dans l'overlay ouvre la même vue, mais sur la source « Draft en cours », sans rechargement de la page et sans diffusion d'état vide. La bascule entre les deux sources est visible en haut de la vue.

### Mes statistiques

- **Journal DRAFT** : picks notés avec « Je joue », résultats, conseil suivi ou non.
- **Historique Riot** : état de l'import, nombre de parties, date, bouton « Importer » ou « Importer plus ». Le dialogue de clé s'ouvre d'ici.
- **Ancien import par fichier** (`draft-stats.js`) : replié sous « Importer un ancien fichier », sans consigne de script.
- Export du journal.

### Réglages

Sous-navigation à gauche ou en haut, une section visible à la fois :

| Section | Contenu |
|---|---|
| Général | Démarrer avec Windows, langue (futur), niveau des conseils |
| Draft et client LoL | Lecture du client (avec risque dit une fois), rôle attribué par LoL, affichage automatique, frontline supposée |
| Panneaux | Coller au client, panneau unique, premier plan, replacer, raccourci |
| Données | Rôles des champions, matchups (avec rôle et pool calculés, durée), sources de données en langage de produit |
| Mise à jour | Version, recherche, dernier résultat |
| Avancé | Journal de diagnostic, apprentissage de l'écran (« C'est une draft »), import de journal |

### Overlay de draft

- Ne contient que la draft : saisie ou lecture des adversaires, conseil, alternative, danger, « Je joue X ».
- Trois outils au plus : « Détails » (ouvre le Simulateur sur la draft en cours), « Panneaux » (ouvre Réglages > Panneaux), « Masquer » (rappelle une fois le raccourci).
- Le changement de rôle reste, car il sert en pleine sélection.
- Aucune autre page, aucun formulaire.

### Menu de l'icône (8 commandes au plus)

```
DRAFT : Draft en cours                (état, grisé)
Ouvrir DRAFT
Afficher les panneaux (Ctrl+Alt+D)
────────────
Simuler une draft
Mon pool
Réglages
────────────
Rechercher une mise à jour
Quitter
```

Les commandes de détection (« C'est une draft… »), les calculs Riot et les cases à cocher passent dans la page. Le double-clic sur l'icône ouvre DRAFT (aujourd'hui, il bascule les panneaux).

## 3. Pages inutiles à ne pas créer

- Pas de page « Analyse » séparée du Simulateur : c'est la même vue sur une autre source.
- Pas de page « Données Riot » : rôles et matchups sont des réglages de données ; l'historique est une statistique personnelle.
- Pas de page « À propos » : version et mentions légales en pied de la navigation.
- Pas de tableau de bord décoratif sur l'Accueil : état, profil, deux actions, deux cartes.

## 4. Adresses et compatibilité

Les adresses actuelles doivent rester valides pendant la transition, car le code natif et les tests les utilisent.

| Actuel | Cible |
|---|---|
| `?panel=full` | `?panel=full&view=home` |
| `?panel=full&tab=settings` (et `&solo=1`) | `?panel=full&view=settings` |
| `?panel=full&tab=analyse`, `compare`, `rules`, `coach`, `parties`… | `?panel=full&view=sim&source=live&tab=…` |
| Message `openSettings` | `openView` avec `view` et `section` |
| Commande `openFull` avec `tab` | `openFull` avec `view`, `tab`, `source` |

Fichiers touchés : `web/index.html` (App, SidePanel, Settings, PanelApp, TitleBar), `src/DraftMid.cs` (`OpenFull`, menu de l'icône, `OnMessage`), `tools/run-ux-shots.ps1`, `web/draft-layout-tests.html`, tests e2e de la fenêtre principale (`TestFullWindow`).

## 5. Maquette

[maquettes/navigation.html](maquettes/navigation.html) et son rendu [navigation-maquette-non-validee.png](maquettes/navigation-maquette-non-validee.png) : maquette statique de l'Accueil avec la navigation. **Maquette non validée.** Elle reprend les jetons de couleur actuels et ne charge aucune ressource externe.
