# Options de refonte

Trois options, du plus prudent au plus ambitieux. Toutes partent après la stabilisation de V2.17 (PR D, E, C-2 fusionnées). Efforts estimés en jours de développement, tests compris.

Les corrections de comportement P0 et P1 (synchronisation des panneaux, saisie sans rôle, rôle changé en modifiant un pool, Échap, retour des actions du menu) sont **communes aux trois options**. Elles forment la PR UX-1 et passent en premier.

## Option A : conservatrice

**Idée.** Garder la page unique et la modale. Ranger sans déplacer.

**Contenu.**

- UX-1 (corrections de comportement).
- Simulateur isolé : un indicateur « Simulation » dans la fenêtre principale, état local quand il est actif.
- Modale « Réglages » découpée en onglets : Profil et pool, Draft, Panneaux, Données, Mise à jour, Avancé. Exclusions et favori déplacés à côté du pool.
- Menu de l'icône réduit à huit commandes.
- Nettoyage des messages internes (« Licence non confirmée », « .bat »), bouton fichier stylé, icônes SVG dans le panneau.

**Bénéfices.** Rapide. Peu de risque. Corrige les erreurs qui coûtent une draft.

**Limites.** Le pool reste « dans les réglages », même dans un onglet : le constat de départ n'est résolu qu'à moitié. Toujours pas de fenêtre d'accueil. La fenêtre principale reste un simulateur nommé « Analyse détaillée ».

**Risques de régression.**

| Fichier | Zone | Tests touchés |
|---|---|---|
| `web/index.html` | `Settings`, `ProfileCard`, `App` (canal d'état), `TitleBar` | `draft-layout-tests.html` (états `reglages`), `TestFullWindow`, `TestProfilesAndRole`, `TestHistoryWindow` (sélecteurs `.settings`) |
| `src/DraftMid.cs` | Menu de l'icône, `RunUpdate` (bulles) | `--selftest`, `--e2e` |

**Effort.** 5 à 7 jours.

## Option B : refonte modérée avec vraie navigation (recommandée)

**Idée.** Une application principale avec cinq sections, un overlay limité à la draft. Le graphisme actuel est conservé et resserré.

**Contenu.**

- UX-1, puis isolation du simulateur (UX-2).
- Navigation de la fenêtre principale : Accueil, Mon pool, Simulateur, Mes statistiques, Réglages ([ARCHITECTURE-NAVIGATION.md](ARCHITECTURE-NAVIGATION.md)).
- Réglages devient une page à sous-sections ; la modale disparaît.
- « Mon pool » : cinq rôles, rôle joué explicite, exclusions et favori par rôle.
- « Mes statistiques » : journal et historique Riot réunis.
- Overlay réduit : Détails, Panneaux, Masquer.
- Menu de l'icône à huit commandes ; le raccourci du menu Démarrer ouvre l'Accueil.
- Passe de système visuel ([DESIGN-SYSTEM.md](DESIGN-SYSTEM.md)) : échelle typographique, trois variantes de boutons, icônes SVG, bordures à 3:1, dialogues natifs harmonisés.
- Premier lancement guidé dans l'Accueil.

**Bénéfices.**

- Répond au constat de départ : le pool n'est plus un réglage, chaque chose a sa place.
- On sait toujours où l'on est (section active dans la navigation).
- Le simulateur ne peut plus abîmer une vraie draft.
- Même pile technique (une page, React, htm, CSP inchangée), même exe léger.
- Prépare l'internationalisation : chaque nouvelle chaîne entre directement dans le catalogue.

**Limites.**

- La page reste un seul fichier `index.html` de plus de 2 400 lignes : la lisibilité du code ne s'améliore pas d'elle-même.
- L'état reste diffusé entre pages par `BroadcastChannel` : corrigé, mais pas remplacé.
- Les dialogues de clé restent des fenêtres WinForms (choix de sécurité : la clé ne passe jamais par la page). Ils sont harmonisés, pas intégrés.

**Risques de régression.**

| Fichier | Zone | Tests touchés |
|---|---|---|
| `web/index.html` | `App`, `Settings` (devient page), `ProfileCard` (devient Mon pool), `SidePanel`, `PanelApp`, `TitleBar`, canal d'état, adresses `?tab=` | `draft-layout-tests.html` (paramètres `tab`, `tabclick`), `run-layout-tests.ps1`, `run-ux-shots.ps1`, `draft-tests.html`, `draft-profiles-tests.html` |
| `src/DraftMid.cs` | `OpenFull`, message `openSettings`, menu de l'icône, `OnMessage`, tests `TestFullWindow`, `TestProfilesAndRole`, `TestHistoryWindow` | `--selftest`, `--e2e` (sélecteurs `.settings`, `.pool-search`, textes de boutons) |
| `src/HistoryImporter.cs`, `src/RoleRatesUpdater.cs` | Dialogues de clé (présentation) | `LayoutProblem()` des dialogues |
| `docs/utilisateur/GUIDE.md` | À réécrire | aucun |

Moyen d'atténuation : garder les anciennes adresses (`?tab=settings`, `?tab=analyse`) comme alias pendant une version ; ajouter des attributs `data-test` stables avant de déplacer les éléments ; captures avant/après avec `run-ux-shots.ps1` et les scripts de [outils/](outils/).

**Effort.** 15 à 22 jours, en huit PR (voir [FEUILLE-DE-ROUTE-UX.md](FEUILLE-DE-ROUTE-UX.md)).

## Option C : ambitieuse

**Idée.** Reconstruire la couche d'interface.

**Contenu.**

- Tout ce que contient B.
- Deux pages distinctes : `app.html` (application) et `overlay.html` (panneaux), modules séparés au lieu d'un fichier unique.
- État de la draft tenu par le programme natif (source unique), diffusé aux pages par messages WebView2 ; plus de `BroadcastChannel`.
- Saisie de la clé dans un contrôle natif intégré à la fenêtre (pas dans la page), avec thème sombre.
- Thème sombre des fenêtres natives (barre de titre via `DwmSetWindowAttribute`), dialogues refaits.
- Internationalisation intégrée dès la conception.

**Bénéfices.** Base saine pour plusieurs années. Plus aucune course entre pages. Code plus lisible.

**Limites.** Gros changement pendant que le moteur évolue encore (V2.17, puis V2.18). Peu de bénéfice visible en plus de B pour la personne qui joue.

**Risques de régression.** Élevés. Touche toutes les zones de B, plus `build.ps1` (ressources, deux pages), la CSP (deux documents), le chargement des scripts de données (`DataScripts`), tout le banc e2e (`EndToEnd`, `RunTestScript` qui cherche `panels["right"]`), les outils `run-*-tests.ps1` qui ouvrent `index.html`, `extract-ui-messages.ps1` (PR #9), le banc moteur (PR #8) s'il charge la page. Conflits garantis avec toute PR qui touche `index.html`.

**Effort.** 35 à 50 jours.

## Comparaison

| Critère | A | B | C |
|---|---|---|---|
| Le pool n'est plus un réglage | Partiel | Oui | Oui |
| Fenêtre d'accueil | Non | Oui | Oui |
| Simulateur isolé | Oui (indicateur) | Oui (page) | Oui (état natif) |
| Overlay compact distinct | Partiel | Oui | Oui |
| Risque de régression | Faible | Moyen | Élevé |
| Conflits avec les branches en cours | Faibles | Moyens, évitables par l'ordre | Forts |
| Effort | 5 à 7 j | 15 à 22 j | 35 à 50 j |
| Logiciel léger (taille, mémoire) | Inchangé | Inchangé | Inchangé ou mieux |

## Recommandation : option B

1. **Elle traite la cause.** Le ressenti vient de l'organisation : pas d'accueil, pool dans les réglages, simulateur confondu avec la draft réelle. B règle ces trois points ; A en laisse deux.
2. **Elle garde ce qui marche.** Les panneaux, leur placement et leur vitesse sont solides et testés (e2e « TOUT EST BON », 31 états de rendu sans défaut). B n'y touche que pour retirer et corriger.
3. **Le risque se découpe.** Huit PR courtes, chacune avec ses tests et un critère mesurable. Les adresses actuelles restent valides le temps de la transition.
4. **Elle prépare l'anglais.** Les écrans réorganisés reçoivent leurs textes directement dans le catalogue, au lieu de traduire deux fois.
5. **C viendra peut-être, mais pas maintenant.** Un état tenu par le programme natif est la bonne cible à long terme. Il se justifie quand le moteur sera stable (après V2.18). B le rend plus facile, car les écrans seront déjà séparés.

Ce que B ne doit pas faire : changer la palette, la police ou l'identité. La correction est structurelle.
