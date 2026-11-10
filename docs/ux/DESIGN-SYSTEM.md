# Système visuel de DRAFT : état et règles proposées

État relevé sur la 2.16.1 (feuille de style `web/index.html:8-672`), mesures dans la vraie WebView2 à 125 %. Objectif : resserrer, pas changer l'identité. La palette sombre, le turquoise et la police Bahnschrift restent.

**Ce qui fait « pas pro » ici n'est pas la palette.** Ce sont les écarts : trop de tailles, trop de styles de boutons, trois façons de montrer un élément actif, des fenêtres Windows claires au milieu, des icônes de deux familles. Chacun est petit ; ensemble, ils donnent une impression d'assemblage.

## 1. Typographie

**État.** Deux familles : `--condensed` (Bahnschrift SemiBold Condensed) pour les titres et noms de champions, `--body` (Bahnschrift) pour le texte. Bon choix : proche de l'univers du jeu, présent sur tout Windows 10 et 11, aucune police à charger.

**Écart.** 21 tailles différentes : 10,5 ; 11 ; 11,5 ; 12 ; 12,5 ; 13 ; 13,5 ; 14 ; 14,5 ; 15 ; 16 ; 17 ; 18 ; 19 ; 20 ; 21 ; 24 ; 26 ; 34 ; 36 ; 50 px. Les plus utilisées : 13 px (30 fois), 12 et 12,5 px (20 fois chacune).

**Règle proposée : 7 tailles.**

| Jeton | Taille | Usage | Remplace |
|---|---|---|---|
| `--fs-xs` | 12 px | Étiquettes, métadonnées | 10,5 ; 11 ; 11,5 ; 12 |
| `--fs-sm` | 13 px | Texte secondaire, aides | 12,5 ; 13 ; 13,5 |
| `--fs-md` | 15 px | Texte courant, boutons | 14 ; 14,5 ; 15 ; 16 |
| `--fs-lg` | 18 px | Titres de section | 17 ; 18 ; 19 ; 20 ; 21 |
| `--fs-xl` | 24 px | Titres de page, nom dans le panneau | 24 ; 26 |
| `--fs-2xl` | 34 px | Nom conseillé (fenêtre) | 34 ; 36 |
| `--fs-hero` | 50 px | Nom conseillé, grand format | 50 |

Le minimum passe à 12 px : aujourd'hui, les libellés de rôle des panneaux sont à 10,5 px (mesure CDP, classe `role-icon`). Les tests de rendu existants (`TEXTE-TROP-PETIT` sous 10,5 px) devront suivre ce seuil.

## 2. Couleurs et contrastes (mesurés)

Jetons actuels (`:root`) : `--bg #0a0f16`, `--surface #111822`, `--raised #18212e`, `--line #263242`, `--text #e8eef5`, `--muted #93a3b6`, `--accent #3fd8c6`, `--accent-ink #04211e`, `--gold #d4b36a`, `--threat #f2777a`.

| Texte | sur `--bg` | sur `--surface` | sur `--raised` | sur `--line` |
|---|---|---|---|---|
| `--text` | 16,45 | 15,27 | 13,87 | 11,12 |
| `--muted` | 7,46 | 6,93 | 6,29 | 5,04 |
| `--accent` | 10,84 | 10,06 | 9,14 | 7,33 |
| `--threat` | 7,04 | 6,53 | 5,94 | 4,76 |
| `--gold` | 9,57 | 8,88 | 8,07 | 6,46 |
| `--accent-ink` sur `--accent` | 9,54 | | | |

Tous les textes dépassent 4,5:1. Relevé dans les quatre pages réelles (fenêtre, panneaux gauche, droit, unique) : **aucun texte sous le seuil** (script [outils/mesure-a11y.js](outils/mesure-a11y.js), fond opaque le plus proche, images ignorées).

**Écarts.**

- **Bordures** : `--line` sur `--bg` = **1,48:1**, sur `--surface` = 1,37:1. Pour la limite d'un champ de saisie ou d'une case, WCAG 1.4.11 demande 3:1. Proposition : `--line-strong` (environ `#4a5a70`, à vérifier à 3:1) pour les champs et cases ; `--line` reste pour les séparateurs décoratifs.
- **Couleurs hors jetons** : 3 hex (`#1f4a4a`, `#8fbbe8`, `#2a0708`) et 21 `rgba(...)`. Les remplacer par des jetons nommés (`--accent-soft`, `--info`, `--threat-ink`).
- **Opacités** pour atténuer (0,4 ; 0,45 ; 0,5 ; 0,55 ; 0,78) : elles baissent le contraste réel. Utiliser `--muted` à la place pour le texte.

## 3. Espacement et alignement

**État.** 22 valeurs différentes de marge, de retrait ou d'écart, de 1 à 84 px. Les plus fréquentes : 6 px (55 fois), 8 px (51), 2 px (33), 10 px (30), 4 px (27).

**Règle proposée.** Échelle de 4 : 4, 8, 12, 16, 24, 32 px, plus 2 px pour les ajustements fins. Les panneaux gardent 12 px de marge latérale ; la fenêtre principale 16 à 24 px.

## 4. Hiérarchie

- **Panneaux** : bonne hiérarchie. Un nom, une raison, un danger, un bouton. À garder.
- **Fenêtre principale** : trois colonnes de poids égal. La colonne droite empile un bloc fixe et trois niveaux d'onglets. La navigation proposée (option B) retire deux niveaux.
- **Réglages** : douze titres `h3` de même taille, 45 contrôles. Rien ne dit ce qui est important. La page à sous-sections règle ce point mieux qu'un changement de style.

## 5. Composants

### Boutons

**État.** Une douzaine de styles : `cta`, `ghost-btn`, `p-btn`, `link-btn`, `seg-tab`, `tab`, `role-btn`, `pool-chip`, `rank`, `cmp-btn`, `p-alt`, plus le bouton de fichier natif du navigateur (gris clair, observé dans les réglages, [captures/06](captures/06-reglages-bas.png)).

**Règle proposée : trois variantes et un bouton d'icône.**

| Variante | Usage | Exemple |
|---|---|---|
| Principal (`cta`) | Une seule action principale par écran | « Je joue Fizz » |
| Secondaire (`ghost-btn`) | Actions courantes | « Analyse détaillée », « Exporter » |
| Lien (`link-btn`) | Actions dans une phrase | « Voir les conseils » |
| Icône (`p-btn`) | Outils, toujours avec `aria-label` | Masquer, Panneaux |

Les puces de pool, les onglets et les segments restent des composants à part, mais partagent les mêmes états (repos, survol, actif, focus, désactivé). Le bouton de fichier est caché derrière un `ghost-btn` (déjà fait pour l'import de profils, `web/index.html:2152`, pas pour le journal, `:2304`).

### État actif de la navigation

**État.** Au moins cinq façons de montrer « actif » : texte clair plus trait animé (`tab`), fond plus clair (`seg-tab`), bordure et texte turquoise (`role-btn`, `role-pick`), bordure turquoise seule (`cmp-btn`), bordure vert sombre (`pool-chip`, `set-card`).

**Règle proposée.**

- Navigation principale et onglets : trait turquoise à gauche (navigation) ou dessous (onglets), texte `--text`.
- Choix dans un groupe (rôle, segment) : bordure et texte `--accent`.
- Élément sélectionné dans une liste : fond `--raised`.

### Icônes

**État.** Rôles en SVG dessinés à la main, cohérents. Outils du panneau en caractères Unicode : « ⧉ », « ▭ », « ⚙ », « × » (`web/index.html:2017-2019`). Leur rendu dépend de la police : épaisseur et alignement varient.

**Règle.** Toutes les icônes en SVG en ligne, trait de 2 px, 16 ou 20 px, `currentColor`. Pas de bibliothèque externe (CSP). Huit icônes suffisent : masquer, réglages, un ou deux panneaux, détails, accueil, pool, simulateur, statistiques.

### Modales

**État.** Une seule modale, « Réglages », qui contient tout. En pleine draft, elle s'ouvre seule dans la fenêtre (`&solo=1`).

**Règle.** Une modale ne sert qu'à confirmer ou à un choix court. Les réglages deviennent une page. Les confirmations utilisent une modale à deux boutons au plus, avec l'action nommée (« Effacer la draft », pas « OK »).

### Retours (feedback)

**État.** Trois canaux : texte d'état dans la page (`role="status"`), bulles Windows, boîtes de message Windows. Le choix dépend de l'endroit d'où l'action est lancée, d'où des actions sans réponse (mise à jour depuis le menu).

**Règle.**

- Action lancée depuis la page : réponse dans la page, à côté du bouton.
- Action lancée depuis le menu de l'icône : bulle courte, et la même information sur l'Accueil.
- Action destructrice : confirmation, ou annulation pendant 5 secondes (« Draft effacée. Annuler »).
- Erreur : dire ce qui s'est passé, ce qui est conservé, quoi faire (le modèle existe déjà : « Ce fichier n'est pas un import DRAFT valide : rien n'a été modifié. »).

### États vides

| Endroit | Texte actuel | Problème | Proposition |
|---|---|---|---|
| Onglet Parties | « Lance draft-import-stats.bat, placé dans le même dossier que cette page, puis recharge-la. » | Consigne de développeur, dossier inaccessible | « Aucune partie importée. » + bouton « Importer mon historique Riot » |
| Fenêtre sans pool | « Aucun pick » + consigne | Bon fond, mais le bouton manque dans la fenêtre | Ajouter « Choisir mes champions » |
| Panneau sans profil | « Choisis ton rôle et tes champions dans les réglages. » + bouton | Bon | Pointer vers « Mon pool » |
| Sources de données au premier lancement | « Sources de données : 4 absente(s) » | Alarme sans raison | « Données de base. Calcule les matchups pour des conseils plus précis. » |

### Erreurs

Les messages d'erreur existants sont bons sur le fond (ce qui est conservé est toujours dit). Les écarts sont de forme : jargon (« Heuristique », « Meraki », « Licence non confirmée »), pluriels « (s) », erreurs écrites loin de l'action. Voir l'inventaire de la PR #9 (`MESSAGES-A-CORRIGER.md`).

### Dialogues Windows

**État.** Trois dialogues WinForms (historique, rôles, matchups) au thème clair de Windows, police système, au milieu d'une application sombre ([captures/18](captures/18-dialogue-import-historique.png), [19](captures/19-dialogue-cle-matchups.png)). Mise à l'échelle correcte à 125 % (`AutoScaleMode.Dpi`, `LayoutProblem()` testé).

**Règle (option B).** Garder des dialogues natifs : la clé ne doit jamais passer par la page. Les harmoniser : un seul dialogue de clé, même largeur, même ordre (explication, lien, clé, région, ce qui sera lancé et sa durée, boutons). Barre de titre sombre possible avec `DwmSetWindowAttribute` (attribut 20) ; fond et contrôles sombres en option C seulement.

## 6. Accessibilité (mesurée)

| Point | Résultat | Source |
|---|---|---|
| Contraste du texte | Tous ≥ 5:1 | Calcul des jetons, relevé CDP des 4 pages |
| Contraste des limites de champ | 1,48:1 (échec 1.4.11) | `--line` sur `--bg` |
| Focus visible | Oui, contour turquoise de 2 px (`:focus-visible`) | `web/index.html:22` |
| Champ de recherche | Contour retiré, remplacé par une bordure turquoise au focus | `web/index.html:48` |
| Cibles < 24 × 24 px | Aucune dans les 4 pages | Relevé CDP |
| Texte < 12 px | Libellés de rôle 10,5 px, « Ajouter » 11 px (panneau unique) | Relevé CDP |
| Clavier, réglages | Échap ferme, focus initial sur « Fermer », tabulation reste dans la modale (observé) | CDP, touches réelles |
| Clavier, draft | Échap efface toute la draft (observé), Retour arrière retire le dernier adversaire | `web/index.html:2381-2391` |
| Mouvement réduit | Respecté (`MotionConfig reducedMotion="user"`, règle CSS) | `web/index.html:602` |
| Repères de page | Aucun `h1` ; `header`, `main`, `aside` présents | Relevé CDP |
| Annonces | Conseil annoncé (`aria-live="polite"`) | `web/index.html` (`speakVerdict`) |

Échap a trois sens selon l'endroit : fermer les réglages, vider un champ, effacer la draft. Le troisième est dangereux. Règle : Échap ferme ou vide, n'efface jamais une donnée.

## 7. Petites résolutions et échelle Windows

- **Tests de rendu** (`tools/run-layout-tests.ps1`, Chrome headless) : 31 états, panneaux de 230 à 400 px, fenêtre de 800 × 600 à 1920 × 1080. **Aucun débordement, aucun texte coupé.** Résultat obtenu pendant l'audit.
- **Observé à 125 %** : panneaux de 336 px de page (420 physiques), fenêtre 1280 × 792 logiques ; dialogues natifs lisibles, non coupés.
- **Non observé** : 150 % et 175 %. Le placement utilise l'échelle de chaque écran (`m.Scale`) ; les tests e2e couvrent le passage en panneau unique quand la place manque. Hypothèse : à 175 % sur un écran de 1920 px, la fenêtre principale tombe sous 1100 px logiques et passe à sa mise en page étroite (`@media (max-width:1100px)`), déjà testée.
- Avec une navigation latérale (option B), prévoir une barre réduite à des icônes sous 1100 px.

## 8. Ce qu'il ne faut pas faire

- Pas de nouvelle palette ni de nouvelle police : l'identité actuelle fonctionne.
- Pas de ressource externe (police web, bibliothèque d'icônes) : la CSP interdit `font-src` et `script-src` externes, et l'exe doit rester léger.
- Pas de nouveau script en ligne : les nouveaux composants vivent dans le script principal existant ou dans des fichiers servis par `draftmid.local`.
- Pas d'animation de plus : le mouvement actuel est déjà discret et respecte la préférence de mouvement réduit.
