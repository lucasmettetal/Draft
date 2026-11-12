# Diagnostic : pourquoi DRAFT fait « dashboard générique »

Étude de direction artistique, branche `etude/direction-artistique` (refonte UX-0 à UX-8 complète).
Toutes les preuves sont des captures de l'application réelle, prises sur un bureau Windows caché.
Elles sont dans [`captures/actuel/`](captures/actuel/).

## En une phrase

L'organisation est bonne. L'habillage, lui, est le kit par défaut d'un tableau de bord : des boîtes identiques, une seule couleur pour tout, des portraits trop petits et des chiffres sans relief.

## Sept constats

### 1. Une boîte par information, toutes pareilles

Accueil : cinq boîtes au même rayon (10 px), au même filet de 1 px, au même fond. Trois d'entre elles ont exactement la même largeur et la même structure (petit titre, état, texte, bouton).
Mes statistiques : quatre boîtes. Mon pool : quatre boîtes. Réglages : une boîte presque vide.
L'œil ne sait pas où commencer, car tout pèse pareil.

Preuves : [`accueil-1366`](captures/actuel/accueil-1366.png), [`stats-1366`](captures/actuel/stats-1366.png), [`pool-1920`](captures/actuel/pool-1920.png).

### 2. Une seule couleur pour dix sens différents

Le turquoise `#3fd8c6` sert à la fois :
la marque, le lien de navigation actif, le bouton principal, l'onglet actif, le badge « Choix net », l'étiquette « Recommandé », la barre de force du premier pick, le rôle mid, les barres du profil adverse, les puces des conseils, le titre « Gagne si », une victoire, un état « connecté », un réglage activé.
Une couleur qui veut tout dire ne dit plus rien. C'est le signe le plus net d'une interface générée : fond presque noir et un seul accent vif.

L'or et le rouge ont un sens (prudence, menace), mais l'or sert aussi aux bans et au « choix serré », deux idées différentes.

Preuves : [`simulateur-1366`](captures/actuel/simulateur-1366.png), [`panneaux`](captures/planches/panneaux.png).

### 3. Des filets partout

Boîtes, boutons secondaires, puces de pool, onglets, étiquettes, champs : tout a un contour de 1 px.
Le contour ne sépare plus rien, il fait du bruit. Sur l'Accueil, on compte une quinzaine de rectangles tracés ou remplis à l'écran.

### 4. Une hiérarchie plate

Sur l'Accueil, trois niveaux de titre s'empilent avant le premier contenu utile (« Accueil », sous-titre, « Tout est prêt… »).
L'information la plus précieuse, le conseil en blind (« DRAFT te conseille Twisted Fate »), est une ligne de 13 px au milieu d'une carte.
Dans Mes statistiques, « 0 sur 0 partie terminée » a le même poids qu'un vrai chiffre.

### 5. Des portraits minuscules, ou géants

Les visages de champions sont le repère le plus rapide d'un joueur de League. Ici, ils font 24 px (Accueil, tableau de pool), 26 px (plan de jeu), 28 px (classement).
À l'inverse, l'illustration du Simulateur occupe 830 × 890 px en 1920 × 1080 pour trois lignes de texte.
Il n'y a pas de taille intermédiaire, pas de recadrage (le liseré des icônes Riot reste visible), pas de lien visuel entre un portrait et son rôle.

Preuves : [`accueil-1920`](captures/actuel/accueil-1920.png), [`simulateur-1920`](captures/actuel/simulateur-1920.png).

### 6. L'espace se perd sur grand écran

En 1920 × 1080 :
- Accueil : environ 25 % de la hauteur vide sous les cartes ;
- Mon pool : la moitié basse de la page vide ;
- Simulateur : les cinq lignes adverses s'étirent à 130 px de haut pour 44 px de contenu ;
- Réglages : un formulaire de trois lignes dans une boîte pleine largeur.

### 7. Des chiffres sans relief

Les chiffres sont en 24 px, comme un titre de carte. Aucun chiffre ne domine. L'échantillon (« sur combien de parties ») est écrit en petit gris, à côté, alors que c'est lui qui dit si le chiffre vaut quelque chose.
La règle « moins de 10 parties : pas de pourcentage » existe dans les barres de victoire, mais pas dans le bandeau de chiffres du haut.

## Ce qui marche déjà, à garder

- Bahnschrift condensée pour les titres et les noms : c'est la seule vraie personnalité actuelle.
- La structure en trois colonnes du Simulateur et l'ordre des rôles.
- Les icônes de rôle en forme de minicarte.
- Les contrastes : tous conformes (voir le tableau en fin de [COMPARATIF](COMPARATIF.md)).
- Le menu à 8 entrées, l'overlay compact, les cinq sections : rien à changer dans l'organisation.

## Ce qu'une direction doit donc régler

1. Donner un sens à chaque couleur, et réserver une couleur au conseil.
2. Remplacer la moitié des boîtes par des listes, des bandeaux et des tableaux.
3. Une échelle de portraits (32, 48, 64 px) recadrée, avec la couleur du rôle.
4. Des chiffres héros, mais seulement quand l'échantillon le permet.
5. Des hauteurs fixes là où le contenu ne grandit pas, pour ne plus étirer le vide.
