# Composants réutilisables de DRAFT 3.0 bêta

**Maquette non validée.** Ce document décrit les jetons, les composants, leurs états et les règles d'accessibilité de la refonte option B. Il prolonge [DESIGN-SYSTEM.md](DESIGN-SYSTEM.md) sans changer l'identité : fond sombre, turquoise, Bahnschrift. La feuille de référence est [maquettes/v3/v3.css](maquettes/v3/v3.css) ; la planche visuelle est [maquettes/v3/composants.html](maquettes/v3/composants.html) ([capture](maquettes/v3/captures/composants.png)). L'overlay a son propre document : [OVERLAY-V3.md](OVERLAY-V3.md).

## 1. Direction visuelle

**Une console de draft, pas un site.** DRAFT sert pendant 90 secondes de sélection, souvent à côté d'un client plein écran. La refonte garde ce qui marche et met de l'ordre :

- **Une seule chose forte par écran.** Dans le Simulateur et l'overlay, c'est le nom du champion conseillé (grande condensée). Sur l'Accueil, c'est la phrase d'état « Tout est prêt pour ta prochaine draft ». Le reste est calme.
- **Les couleurs ont un sens fixe.** Turquoise : réel et action (draft en cours, conseil, bouton principal, élément actif). Or : hypothèse (simulation, choix serré, étape à faire, mention « Exemple »). Rouge : danger et erreur. Bleu clair (`--info`) : déduction de DRAFT (« Mid probable »). Aucune couleur décorative.
- **La structure porte l'information.** Les bordures séparent des groupes réels ; les cartes ne sont pas toutes identiques : boîte (rayon 10) pour une zone, contrôle (rayon 6) pour ce qui se clique, étiquette (rayon 4) pour ce qui qualifie.
- **Pas d'animation gratuite.** Restent seulement les mouvements qui répondent à une action ou à un changement de conseil (voir § 6).
- **Écarts volontaires avec les tics d'interface générée** : pas de libellés en capitales au-dessus des titres, pas de flèche ajoutée aux boutons, pas de dégradé décoratif (le seul motif hachuré marque l'emplacement d'une illustration dans les maquettes), pas d'ombre sous les cartes posées.

## 2. Jetons

### Couleurs

| Jeton | Valeur | Usage | Nouveau |
|---|---|---|---|
| `--bg` | `#0a0f16` | Fond de page, fond des champs | |
| `--surface` | `#111822` | Boîtes, navigation, panneaux | |
| `--raised` | `#18212e` | Élément sélectionné, section active | |
| `--hover` | `#1e2938` | Survol d'un élément cliquable | oui |
| `--line` | `#263242` | Séparateurs décoratifs seulement | |
| `--line-strong` | `#5f7088` | Limites de champ, case, bouton secondaire (≥ 3:1) | oui (remplace la proposition `#4a5a70`, mesurée à 2,73:1) |
| `--text` | `#e8eef5` | Texte | |
| `--muted` | `#93a3b6` | Texte secondaire, aides | |
| `--accent` | `#3fd8c6` | Action, actif, réel | |
| `--accent-ink` | `#04211e` | Texte sur `--accent` | |
| `--accent-soft` | `#0e2a2a` | Fond « actif », bandeau Draft en cours | oui (remplace des `rgba`) |
| `--accent-line` | `#1f4a4a` | Bordure d'un élément actif ou de l'overlay | oui (ex-`#1f4a4a` en dur) |
| `--gold` | `#d4b36a` | Hypothèse, simulation, choix serré | |
| `--gold-soft`, `--gold-line` | `#2a2312`, `#5a4a22` | Fond et bordure du bandeau Simulation | oui |
| `--threat` | `#f2777a` | Danger, erreur, défaite | |
| `--threat-soft`, `--threat-line` | `#2d1416`, `#5e2a2d` | Fond et bordure d'un danger, d'une erreur | oui (ex-`#2a0708`) |
| `--info` | `#8fbbe8` | Déduction (« Mid probable ») | oui (ex-`#8fbbe8` en dur) |

Règle : **0 couleur hors jetons** dans la feuille de style, 0 opacité pour atténuer un texte (utiliser `--muted`). Une seule exception, le voile de modale `rgba(5,8,12,.72)`, à nommer `--scrim`.

### Typographie : 7 tailles au lieu de 21

| Jeton | Taille | Famille | Usage |
|---|---|---|---|
| `--fs-xs` | 12 px | Bahnschrift | Étiquettes, métadonnées, libellés de rôle (minimum absolu) |
| `--fs-sm` | 13 px | Bahnschrift | Texte secondaire, aides, tableaux |
| `--fs-md` | 15 px | Bahnschrift | Texte courant, boutons, champs |
| `--fs-lg` | 18 px | Condensée | Titres de boîte, bouton principal |
| `--fs-xl` | 24 px | Condensée | Titre de page, nom conseillé dans l'overlay |
| `--fs-2xl` | 34 px | Condensée | Phrase d'état de l'Accueil, nom conseillé de l'Accueil |
| `--fs-hero` | 50 px | Condensée | Nom conseillé dans le Simulateur |

- Familles : `--condensed` (Bahnschrift SemiBold Condensed) pour titres, noms de champions et bouton principal ; `--body` (Bahnschrift) pour le reste. Présentes sur Windows 10 et 11 : aucune police chargée.
- Interlignage : 1,45 pour le texte, 1,15 pour les titres, 1 pour les noms héros.
- Chiffres en colonnes : `font-variant-numeric: tabular-nums` (classe `.num`).
- Pas de capitales forcées, pas d'espacement de lettres modifié.
- Contrôle : les maquettes n'utilisent aucune taille en dur (relevé `grep` : 0).

### Espacements, rayons, ombres, couches

| Famille | Valeurs |
|---|---|
| Espacement | `--s1` 4, `--s2` 8, `--s3` 12, `--s4` 16, `--s5` 24, `--s6` 32, `--s7` 48 px ; 2 px pour un ajustement fin seulement |
| Marges | Fenêtre : 24 à 32 px ; sous 1 100 px : 16 à 24 px. Panneaux : 12 px |
| Rayons | `--r-tag` 4 px (étiquette, puce), `--r-ctl` 6 px (bouton, champ, ligne), `--r-box` 10 px (boîte, bandeau, toast, modale) ; panneau d'overlay 8 px (bord de fenêtre) |
| Ombre | Une seule, `--shadow-float: 0 12px 32px rgba(0,0,0,.5)`, pour ce qui flotte : menu, toast, modale |
| Couches (`z-index`) | 0 contenu, 10 barre collante, 20 modale, 30 toast, 50 mention de maquette |

## 3. Composants

Chaque composant a les mêmes états, rendus de la même façon :

| État | Rendu commun |
|---|---|
| Repos | Défini par la variante |
| Survol | Fond `--hover` (ou teinte claire pour le principal) ; jamais de déplacement |
| Focus clavier | Contour turquoise 2 px, décalé de 2 px (`:focus-visible`) ; champs : bordure et halo de 1 px turquoise |
| Actif (sélectionné) | Voir « état actif » ci-dessous |
| Désactivé | Bordure `--line`, texte `--muted`, curseur interdit, **raison écrite à côté ou dans l'infobulle** |

### État actif : une règle par famille

| Famille | Rendu |
|---|---|
| Navigation principale, sous-rubriques, liste des rôles de Mon pool | Trait turquoise de 3 px à gauche, fond `--raised`, texte `--text`, `aria-current` |
| Onglets | Trait turquoise de 2 px dessous, texte `--text`, `aria-selected` |
| Segment (rôle joué, phase, source) | Bordure et texte `--accent`, fond `--accent-soft`, `aria-pressed` ; variante or pour la source « Simulation » |
| Élément choisi dans une liste (« Je joue ») | Bordure `--accent`, fond `--accent-soft` |

### Catalogue

| Composant | Classe (maquette) | Anatomie | Règles d'usage |
|---|---|---|---|
| Bouton principal | `.btn.btn-primary` | Fond turquoise, texte condensé 18 px, 36 px de haut | **Un seul par écran** (« Je joue Fizz », « Simuler une draft ») |
| Bouton secondaire | `.btn.btn-secondary` | Bordure `--line-strong`, texte 15 px | Actions courantes (« Exporter mes profils », « Détails ») |
| Bouton destructeur | `.btn.btn-danger` | Bordure et texte rouges | Seulement dans une confirmation ; l'action est nommée (« Retirer Fizz ») |
| Bouton lien | `.btn.btn-link` | Texte turquoise souligné | Action dans une phrase (« Comment DRAFT classe tes picks ») |
| Bouton d'icône | `.btn.btn-icon` | 32 × 32 px (28 dans l'overlay), SVG 18 px | Toujours `aria-label` et `title` |
| Petit bouton | `.btn-sm` | 28 px de haut | Dans une ligne de liste ou un bandeau |
| Bouton « fait » | `.btn.is-done` | Fond `--accent-soft`, coche | Retour après « Je joue » : « Fizz noté » |
| Champ | `.input` | 40 px, fond `--bg`, bordure `--line-strong` | Libellé visible ou `aria-label` ; aide dessous, en `--muted` |
| Recherche de champion | `.search` | Loupe, champ, touche « Entrée » | Accepte les surnoms ; Entrée ajoute le premier résultat ; Échap vide le champ seulement ; résultats en puces |
| Liste déroulante | `.select` | Même gabarit que le champ | Options courtes ; valeur courante lisible |
| Interrupteur | `.toggle` | Case à cocher native stylée + titre + aide | Effet immédiat, enregistré tout de suite ; l'aide dit l'effet, une fois |
| Segment | `.seg` | Groupe de 2 à 5 boutons | Choix exclusif visible (rôle joué, phase de jeu, source) |
| Onglets | `.tabs` | Libellés, trait dessous | Un seul niveau sous une section |
| Navigation principale | `.nav`, `.nav-link` | Logo, 4 sections, Réglages en bas, pied d'état | 220 px ; rail de 92 px sous 1 100 px, libellés conservés |
| Boîte | `.box` | Surface, bordure `--line`, rayon 10 | Une zone de sens ; titre `h2` condensé 18 px |
| Liste de lignes | `.rows` | Lignes séparées, méta à droite | État d'une donnée (date, nombre) |
| Bandeau d'état | `.band-live`, `.band-sim`, `.band-warn`, `.band-info` | Icône ou voyant, mot fort, phrase, actions | Un bandeau par écran au plus ; `role="status"` |
| Voyant | `.dot.good`, `.wait`, `.bad` | Point de 8 px | Toujours accompagné d'un texte (jamais la couleur seule) |
| Étiquette | `.tag`, `.tag.net`, `.close`, `.bad`, `.solid` | Texte 12 px, bordure | Qualifie (« Choix net », « 4/5 adversaires », « Recommandé ») |
| Puce de champion | `.chip` (+ `.sel`, `.add`) | Portrait 24 px, nom, croix | Pool, suggestions, adversaires du panneau unique ; puce pointillée = à ajouter |
| Portrait | `.mono` (maquette) | Image Data Dragon 24, 32, 40 ou 44 px | En maquette, un monogramme remplace l'image (aucune ressource externe) |
| Ligne de classement | `.rank` (+ `.first`, `.picked`) | Portrait, nom, barre de force, étiquette, bilan | Cliquable (« Je joue ») ; la barre est relative au pool affiché |
| Ligne d'adversaire | `.slot`, `.p-slot` (+ `.mine`) | Rôle (icône + libellé), portrait, nom, traits, retirer | La ligne du rôle joué porte le trait turquoise ; « Mid probable » en `--info` |
| Jauge du profil adverse | `.meter` (+ `.hot`) | Libellé, 5 segments | Rouge seulement pour plongeurs et contrôles ciblés ; `role="img"` avec « 2 sur 5 » |
| Barre de victoires | `.winbar` | Libellé, barre victoires/défaites, « 6 V, 3 D » | Le chiffre est toujours écrit |
| Bloc danger | `.danger` | Portrait, « Danger : Syndra », phrase | Un seul danger visible |
| Toast | `.toast` | Icône, phrase, action éventuelle (« Annuler ») | Retour d'une action ; 5 s pour une annulation, sinon reste jusqu'à l'action suivante ; `role="status"` (`alert` pour une erreur) |
| Confirmation en ligne | `.saved` | Coche + « Enregistré : … » | À côté de ce qui vient d'être modifié |
| Modale | `.scrim` + `.modal` | Titre-question, phrase, 2 boutons | Confirmer une perte ; bouton d'action nommé ; focus sur le bouton sûr ; Échap = annuler |
| État vide | `.empty` | Titre, phrase, une action | Dit quoi faire, jamais de consigne technique |
| Erreur | `.error` | Ce qui s'est passé, ce qui est conservé, quoi faire, action | Placée à côté de l'action qui a échoué |
| Chargement | `.progress`, `.skel` | Barre déterminée avec « 46 sur 100 », ou squelette | Une tâche longue a toujours un bouton « Arrêter » |
| Raccourci | `kbd` | Touche dessinée | « Ctrl Alt D » |
| Menu de l'icône | natif (`ContextMenuStrip`) | 8 entrées, un sous-menu | Pas de case à cocher ; l'entrée du double-clic en gras |
| Dialogue de clé | natif (WinForms) | Titre, ce qui sera lancé et durée, comptes, clé, région, nombre, accord, 2 boutons | Un seul modèle pour historique, rôles, matchups ; barre de titre sombre (`DwmSetWindowAttribute` 20) |

### Icônes

SVG en ligne, trait de 2 px, 18 px (16 dans les petits boutons), `currentColor`, définies comme constantes dans le script (pas de bibliothèque, CSP). Jeu de base : accueil, pool, simulateur, statistiques, réglages, coche, croix, un panneau, deux panneaux, recherche, détails, étoile, actualiser, exporter, importer, info, alerte, bouclier, prise (connexion), moniteur, clé, annuler, chevron, horloge, œil, pouls (diagnostic). Les cinq icônes de rôle actuelles (`ROLE_PATHS`) sont gardées telles quelles.

## 4. Accessibilité

### Contrastes mesurés

Calcul WCAG 2.1 (luminance relative), script `contrast.py` de l'étude (hors dépôt), sur les valeurs des jetons :

| Paire | Rapport | Seuil |
|---|---|---|
| `--text` sur `--bg` / `--surface` / `--raised` / `--hover` | 16,45 / 15,27 / 13,87 / 12,58 | 4,5 |
| `--muted` sur `--bg` / `--surface` / `--raised` / `--hover` | 7,46 / 6,93 / 6,29 / 5,70 | 4,5 |
| `--accent` sur `--bg` / `--surface` / `--raised` / `--accent-soft` | 10,84 / 10,06 / 9,14 / 8,57 | 4,5 |
| `--accent-ink` sur `--accent` / survol `#5fe3d3` | 9,54 / 10,81 | 4,5 |
| `--gold` sur `--surface` / `--gold-soft` | 8,88 / 7,76 | 4,5 |
| `--text` sur `--gold-soft` / `--threat-soft` | 13,34 / 14,70 | 4,5 |
| `--threat` sur `--surface` / `--threat-soft` | 6,53 / 6,29 | 4,5 |
| `--info` sur `--bg` / `--surface` | 9,55 / 8,87 | 4,5 |
| `--line-strong` sur `--bg` / `--surface` / `--raised` (limites de champ, case) | **3,81 / 3,53 / 3,21** | 3 (WCAG 1.4.11) |
| `--line` sur `--bg` (séparateur décoratif seulement) | 1,48 | sans objet |

Le texte le plus faible (`--muted` sur `--hover`) reste à 5,70:1. L'ancienne limite de champ (1,48:1) passe à 3,81:1. À vérifier dans la vraie page avec [outils/mesure-a11y.js](outils/mesure-a11y.js) (fond opaque le plus proche).

### Clavier

| Touche | Effet | Où |
|---|---|---|
| Tab / Maj+Tab | Ordre visuel : navigation, en-tête de page, contenu | Partout |
| Entrée | Ajoute le premier résultat de recherche ; active un bouton | Recherche, boutons |
| Flèches haut/bas | Parcourent les résultats | Recherche d'adversaire |
| Flèches gauche/droite | Changent d'onglet ou de segment (motif `tablist`, `radiogroup`) | Onglets, segments |
| Échap | Vide le champ ; ferme une modale ou un menu ; **n'efface jamais une donnée** | Partout |
| Retour arrière (hors champ) | Retire le dernier adversaire, avec « Annuler » | Simulateur, panneau gauche |
| Lettre (hors champ) | Place le focus dans la recherche d'adversaire | Simulateur, panneau gauche |
| Ctrl+Alt+D | Affiche ou masque les panneaux | Windows entier |
| Alt+1 à Alt+5 (proposé) | Sections de la fenêtre principale | Fenêtre principale |

- Focus visible partout (contour 2 px turquoise). Le contour n'est jamais retiré, y compris dans la recherche.
- Au changement de section, le focus va au titre `h1` de la section (`tabindex="-1"`). Une page = un `h1`.
- Les panneaux d'overlay ne prennent jamais le focus d'eux-mêmes (règle existante, `focusAdd` vérifie `document.hasFocus()`).

### Cibles et lecture

- Cibles : 24 × 24 px minimum (WCAG 2.2, 2.5.8) ; 32 px nominal ; 36 à 40 px pour les actions principales et les champs. Petites croix de l'overlay : 28 × 28 px.
- Texte minimum : 12 px. Les tests de rendu (`TEXTE-TROP-PETIT`) passent de 10,5 à 12 px.
- Toute couleur porte aussi un texte ou une forme : voyant + phrase, barre de victoires + « 6 V, 3 D », danger + titre.
- Annonces : conseil (`aria-live="polite"`, existant), toasts et confirmations (`role="status"`), erreurs (`role="alert"`).
- Mouvement réduit respecté (`prefers-reduced-motion`, `MotionConfig reducedMotion="user"`).

## 5. Échelle Windows (DPI) de 100 à 175 %

La page travaille en pixels logiques. Les maquettes ont été rendues avec un facteur d'échelle réel (`--force-device-scale-factor`), pas seulement une fenêtre réduite.

| Écran et échelle | Fenêtre principale (logique) | Disposition | Capture |
|---|---|---|---|
| 1366 × 768, 100 % | ~1 280 × 728 | Navigation 220 px, 3 colonnes au Simulateur | `*-1366x768.png` |
| 1366 × 768, 125 % | ~1 029 × 582 | Rail 92 px, 2 colonnes au Simulateur, détails dessous | `*-1366x768-125pc.png` |
| 1920 × 1080, 100 % | 1 280 × 820 (ouverture) à 1 920 (agrandie) | Navigation 220 px, contenu centré (max 1 180, Simulateur 1 560) | `*-1920x1080.png` |
| 1920 × 1080, 175 % | ~1 051 × 591 | Rail 92 px, Réglages en 180 + contenu | `*-1920x1080-175pc.png` |
| 2560 × 1440, 100 % | jusqu'à 2 560 | Contenu centré, largeur de lecture tenue | `*-2560x1440.png` |

Règles :

- Point de rupture unique de la fenêtre : **1 100 px** (déjà utilisé par la 2.x). Au-dessous : rail de navigation, grilles à 1 ou 2 colonnes.
- Hauteur utile minimale visée : 580 px logiques. Rien d'essentiel sous la ligne de flottaison de l'Accueil à 1366 × 768 ; les pages longues défilent dans la zone de contenu, jamais la navigation.
- Pas de texte coupé par `ellipsis` pour un nom de champion ou un libellé de bouton. Les traits secondaires d'un adversaire peuvent passer sur deux lignes au plus.
- Les dialogues natifs gardent `AutoScaleMode.Dpi` et le test `LayoutProblem()`.

## 6. Mouvement

| Garder | Retirer |
|---|---|
| Changement de conseil : fondu du nom (150 ms, opacité seulement) | Lueur du premier rang (`rank-glow`) |
| Apparition d'un adversaire dans sa ligne (150 ms) | Rebond des boutons de résultat |
| Ouverture d'un détail (réponse à un clic) | Décalage des raisons ligne par ligne, chiffres `+1` flottants des jauges |
| Toast qui apparaît (opacité) | Trait d'onglet animé (`layoutId`), soulèvement au survol des lignes |

Aucune animation n'excède 200 ms. Seules `transform` et `opacity` sont animées.

## 7. Mise en œuvre compatible avec la CSP actuelle

CSP de `web/index.html` : `script-src 'self' 'unsafe-inline' file:` ; `style-src 'self' 'unsafe-inline' file:` ; aucune source externe hors Data Dragon (images et `connect-src`).

- **Aucun script en ligne nouveau.** Les composants sont des fonctions du script principal existant, ou de nouveaux fichiers servis par `draftmid.local` et chargés par `<script src="ui/…js">` (par exemple `web/ui/nav.js`, `web/ui/settings.js`). `build.ps1` embarque déjà `web/` récursivement. À terme, cela permettra de retirer `'unsafe-inline'` de `script-src`.
- **Styles** : les jetons et composants vont dans `web/app.css` (chargé par `<link rel="stylesheet">`, autorisé par `style-src 'self'`). Les attributs `style="…"` des maquettes sont une commodité de maquette : en production, des classes.
- **Maquettes** : 0 balise `<script>`, 0 URL externe (vérifié par `grep`). Navigation par liens ; sous-rubriques des Réglages par `:target` (en production : état React et `aria-current`).
- **Icônes** : constantes SVG dans le script ; aucune police d'icônes.
- **Textes** : jamais en dur dans les nouveaux composants ; `t("clé")` sur le catalogue (voir [MIGRATION-V3.md](MIGRATION-V3.md), § 5).
