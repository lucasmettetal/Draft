# Overlay de draft et menu de l'icône (DRAFT 3.0 bêta)

**Maquette non validée.** L'overlay est la partie la plus solide de DRAFT 2.x : placement testé sur plusieurs écrans (`Layout.Compute`, `--selftest`), apparition en 11 ms, aucun vol de focus, e2e « TOUT EST BON ». La refonte **retire et corrige**, elle ne réinvente pas. Maquettes : [deux panneaux](maquettes/v3/overlay-deux-panneaux.html), [panneau unique](maquettes/v3/overlay-panneau-unique.html), [états](maquettes/v3/overlay-etats.html), [menu de l'icône](maquettes/v3/menu-icone.html).

Captures à taille réelle (pixels logiques) :

- [overlay-deux-panneaux-340x720.png](maquettes/v3/captures/overlay-deux-panneaux-340x720.png) : 2 × 340 px, client 1280 × 720.
- [overlay-panneau-unique-374x720.png](maquettes/v3/captures/overlay-panneau-unique-374x720.png) : 374 px (largeur idéale × 1,1).
- [overlay-deux-panneaux-125pc.png](maquettes/v3/captures/overlay-deux-panneaux-125pc.png) et [overlay-panneau-unique-175pc.png](maquettes/v3/captures/overlay-panneau-unique-175pc.png) : mêmes pages à 125 % et 175 %.
- [overlay-etats.png](maquettes/v3/captures/overlay-etats.png) : sans profil, draft estimée, fin de draft, masquage.

## 1. Compact : ce que l'overlay contient, et rien d'autre

| Panneau | Contenu, par ordre de priorité | Retiré |
|---|---|---|
| Gauche | Composition adverse (« 4 sur 5 »), recherche, 5 lignes de rôle (adversaire, traits, « Mid probable », placer au mid, retirer), profil de l'équipe (5 jauges) | Rien |
| Droit | Rôle joué (5 icônes, proposition de League), état (« Choix net », « 4/5 adversaires », source : « Lecture du client » ou « Draft estimée »), conseil (illustration, « Recommandé », nom), alternative (→ Comparer), 2 raisons, danger, ligne de conseil, « Plan de jeu et ban » repliable, **« Je joue Fizz »**, « Détails », résultat de la partie | Le mot « Analyse détaillée » (devient « Détails ») |
| Unique (374 px) | Recherche, adversaires en puces, puis le contenu du panneau droit | Jauges du profil (visibles dans « Détails ») |

**Barre de titre : trois outils au plus**, en SVG au lieu de « ▭ ⧉ ⚙ × » :

| Outil | Effet | Avant |
|---|---|---|
| Un panneau / deux panneaux | Bascule le mode (raccourci de Réglages > Affichage) | identique |
| Réglages d'affichage | Ouvre la fenêtre principale sur **Réglages > Affichage et fenêtres** | ouvrait toute la modale de réglages, seule à l'écran (`&solo=1`) |
| Masquer | Masque les panneaux ; la première fois, bulle « Panneaux masqués. Ctrl+Alt+D pour les rappeler. » | masquait sans rien dire |

Pas de formulaire, pas de page, pas de modale dans l'overlay. « Détails » ouvre le Simulateur sur la source **Draft en cours**, sans recharger la page et sans diffuser d'état vide (correction UX-1, voir [MIGRATION-V3.md](MIGRATION-V3.md)).

**Hauteur.** Le panneau suit la hauteur du client (minimum 520 px logiques). À 520 px, restent visibles sans défilement : barre de titre, rôle joué, état, conseil, alternative, raisons, et la barre d'actions (collée en bas). Le danger et le plan passent dans la zone qui défile. Le bouton « Je joue » n'est jamais hors écran.

## 2. Discret

- **Aucun vol de focus** : l'overlay ne prend le focus que sur un clic. Règle existante, gardée par l'e2e.
- **Aucun son, aucun clignotement.** Un seul mouvement : le nom conseillé change en fondu de 150 ms. Le reste est statique.
- **Sobre en couleur** : turquoise pour le conseil et l'action, rouge pour un seul danger, or seulement pour « Choix serré ». La bordure du panneau est `--accent-line` (sombre), pas un turquoise vif.
- **Typographie lisible d'un coup d'œil** : nom conseillé en condensée 24 px, raisons 15 px, rien sous 12 px (aujourd'hui 10,5 px pour les libellés de rôle).
- **Toujours au premier plan** reste un réglage (actif par défaut, comme aujourd'hui).

## 3. Multi-écrans

Le calcul existant (`src/DraftMid.cs`, `Layout.Compute`, `Layout.Narrowest`) est conservé tel quel. Ordre de décision, à l'échelle de chaque écran (`Monitor.Scale`) :

| Situation | Résultat | Message (Réglages > Diagnostic, et infobulle de l'icône) |
|---|---|---|
| ≥ 230 px libres de chaque côté du client | Deux panneaux de 340 px (moins si la place manque) | « Panneaux placés de chaque côté du client. » |
| Place d'un seul côté, ou « Un seul panneau » choisi | Un panneau de 374 px, du côté le plus large | « Panneau unique à côté du client. » |
| Aucune place autour, autre écran disponible | Deux panneaux (ou un) sur l'autre écran, contre le bord le plus proche du client | « Pas de place autour du client : panneaux sur l'autre écran. » |
| Aucune place nulle part | Panneau étroit (230 px) au bord de l'écran du client | « …panneau étroit au bord de l'écran, il en couvre une partie. Déplace-le si besoin. » |
| Panneaux déplacés à la main | Le collage se coupe ; « Replacer les panneaux » (Réglages > Affichage) le rétablit | — |

Ajouts de la refonte :

- Le mode effectif (« deux panneaux », « panneau unique », « autre écran ») s'affiche sur l'Accueil, ligne « Panneaux », et dans Réglages > Diagnostic.
- Écrans d'échelles différentes : le panneau prend l'échelle de **son** écran. Critère : aucune troncature à 100, 125, 150 et 175 % (tests de rendu Chrome à 230, 340, 374 et 400 px de large, voir [CRITERES-VALIDATION-V3.md](CRITERES-VALIDATION-V3.md)).

## 4. Disparition et retour du client

Comportement existant, gardé et documenté pour la personne :

| Événement | Panneaux | État de la draft |
|---|---|---|
| Client réduit (`IsIconic`) ou fermé | Masqués | Conservée en mémoire |
| Client rétabli pendant la sélection | Réaffichés au même endroit, recalculés si le client a bougé | Inchangée, sans nouvelle diffusion |
| Lancement de la partie | Masqués | Le pick noté reste ; boutons de résultat prêts pour la fin |
| Fin de partie, retour au salon | Masqués ; le résultat se note dans le panneau à la prochaine ouverture, ou dans Mes statistiques | Journal mis à jour |
| Nouvelle sélection | Réaffichés ; draft remise à zéro (`session-start`) | La simulation n'est pas touchée |
| Ctrl+Alt+D pendant la sélection | Bascule manuelle ; l'affichage automatique reprend à la sélection suivante | Inchangée |

Correction attendue (UX-1) : une page de panneau qui s'ouvre (retour du client, rechargement) **ne diffuse rien** avant d'avoir reçu l'état des autres pages ou un court délai. C'est la cause de la perte observée en ouvrant l'analyse depuis un panneau.

## 5. Menu de l'icône : 8 entrées au plus

| # | Entrée | Avant (2.16.2) |
|---|---|---|
| 1 | État grisé : « DRAFT : connecté, aucune draft » | identique (phase) |
| 2 | **Ouvrir DRAFT** (gras, action du double-clic) | n'existait pas (« Analyse détaillée ») |
| 3 | Afficher les panneaux, Ctrl+Alt+D | identique |
| 4 | Simuler une draft | « Analyse détaillée » |
| 5 | Détection de la draft ▸ C'est une draft : retenir cet écran ; Ce n'est pas une draft : retenir cet écran ; Copier le diagnostic de connexion | trois entrées séparées |
| 6 | Réglages | ouvrait la modale |
| 7 | Rechercher une mise à jour | identique, **avec bulle de résultat** |
| 8 | Quitter | identique |

Sortent du menu (et où les trouver) : « Activation automatique pendant la draft », « Positionnement automatique », « Panneau unique », « Replacer les panneaux » (Réglages > Affichage et fenêtres) ; « Démarrer avec Windows » (Réglages > Général) ; « Importer mes statistiques... » (Mes statistiques > Importer un ancien fichier) ; « Mettre à jour les rôles... », « Mettre à jour les matchups... » (Réglages > Données et confidentialité).

Le double-clic sur l'icône ouvre DRAFT (aujourd'hui : bascule les panneaux). **À valider** : les personnes habituées au double-clic gardent Ctrl+Alt+D et l'entrée 3.

L'infobulle de l'icône garde la phase et le placement (63 caractères au plus, limite Windows).
