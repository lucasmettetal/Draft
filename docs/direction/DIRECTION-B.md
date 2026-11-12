# Direction B — « Salle de stratégie »

> Une table de travail du soir, avant la partie. Les champions se reconnaissent à leur visage ; le laiton montre ce que DRAFT conseille.

Feuille d'étude : [`direction-b.css`](direction-b.css) (≈ 230 lignes, chargée après la feuille de l'application).
Captures : [`captures/direction-b/`](captures/direction-b/). Comparaisons : [`captures/planches/`](captures/planches/).

## Idée

B parie sur la lecture par les visages et sur le ton éditorial :
- **portraits plus grands**, encadrés de la couleur de leur rôle : on lit la draft adverse d'un coup d'œil ;
- **une couleur chaude réservée au conseil** : le laiton. Plein, c'est « DRAFT te conseille » ; en pointillé, « conseil serré » ;
- **titres et noms en serif** (Sitka, livrée avec Windows), grands, avec peu de cadres autour.

Ce qui la rend reconnaissable : le nom du champion en Sitka, le laiton, les cadres de rôle colorés.

## Palette et sens

| Rôle | Valeur | Sens, et seulement ce sens |
|---|---|---|
| Fond | `#16131b` | Encre prune, plus chaude qu'un gris. |
| Surface | `#1e1a24` | Zone groupée, ligne active. |
| Surface haute | `#29242f` | Piste de barre, portrait absent. |
| Filet | `#383142` | Séparateur décoratif. |
| Contour de contrôle | `#7d7290` | Champs, boutons secondaires (≥ 3:1). |
| Texte | `#f3ede5` | Ivoire chaud. |
| Texte secondaire | `#b9afa6` | Libellés, aides. |
| Laiton | `#dcab5c` | **Le conseil** : bouton « Je joue », « Recommandé », premier du classement, onglet actif. Pointillé : choix serré. |
| Avantage / victoire | `#80d59c` | Bon pour toi, victoire. |
| Menace / défaite | `#ff8f7b` | Menace, défaite, faiblesse adverse dangereuse. |
| Hypothèse | `#a3b8ec` | Déduit : rôle probable. Italique + cadre pointillé. |
| Simulation | `#6ccbd8` | Bandeau du mode simulation. |
| Rôle Top | `#e49b5d` | Cadre du portrait et icône. |
| Rôle Jungle | `#7cc47c` | Idem. |
| Rôle Mid | `#66b0f2` | Idem. |
| Rôle ADC | `#f07ea3` | Idem. |
| Rôle Support | `#b897f3` | Idem. |

Règles :
- Les couleurs de rôle sont **vives**, mais elles ne touchent que les **cadres de portraits** et les **icônes de rôle**. Les couleurs de sens touchent le **texte et les barres**. Jungle et victoire sont proches : on les sépare par l'endroit, pas par la teinte. C'est le point faible de B.
- Le « risque » n'a pas de couleur à part : il se dit par le laiton pointillé (conseil incertain) ou le corail en contour (menace modérée).
- Bans : portrait en niveaux de gris, cadre neutre.

Contrastes mesurés : tableau complet dans [COMPARATIF](COMPARATIF.md#contrastes-mesurés). Tout texte ≥ 7:1, contours ≥ 3,81:1.

## Typographie (polices Windows, aucune police externe)

| Usage | Police | Taille |
|---|---|---|
| Titres de page, nom du pick, marque | Sitka Banner (semi-gras) | 40 / 46 / 48 à 80 px |
| Titres de section, noms de champions | Sitka Subheading (semi-gras) | 17 à 22 px |
| Texte courant, libellés, boutons | Segoe UI Variable Text, sinon Segoe UI | 13,5 à 16 px, interligne 1,5 |
| Chiffres | Sitka Banner en chiffres alignés (`lining-nums tabular-nums`) | 44 px |

Attention : Sitka dessine par défaut des **chiffres elzéviriens** (le 4 et le 2 descendent). L'étude les force en chiffres alignés ; sans ce réglage, un tableau de chiffres devient illisible (voir la maquette).
Sitka est présente sur Windows 10 et 11. Repli : Georgia.

## Portraits

| Contexte | Taille | Traitement |
|---|---|---|
| Ligne adverse (Simulateur) | 60 px (50 px sous 800 px de haut) | Recadré à 110 %, coins 6 px, **cadre 2 px de la couleur du rôle**, à 2 px du portrait. |
| Panneau gauche | 46 px | Idem. |
| Panneau compact | 36 px | Cadre 2 px collé. |
| Classement du pool | 52 px | Premier : fond et contour laiton. |
| Plan de jeu | 32 px, **ronds** | Menace : anneau corail. Ban : gris. |
| Accueil, pool | 56 px, nom dessous | Le pool se montre en visages. |
| Mon pool, tableau | 48 px | Nom en Sitka 19 px. |
| Rôle supposé | — | Cadre de rôle en pointillé, mention en italique pervenche. |
| Portrait absent | même taille | Initiales en Sitka sur fond teinté de la couleur du rôle. |

## Composants

- **Accueil** : le bandeau d'état devient un **titre de une** (Sitka 46 px), sans boîte ni icône. La chaîne de préparation garde trois colonnes, mais **sans cadres** : un filet vertical les sépare, des filets haut et bas les tiennent. Le pool se montre par ses visages.
- **Simulateur** : lignes adverses sans fond, cadre de rôle autour du visage ; l'illustration reste forte, voile chaud ; nom du pick en Sitka jusqu'à 80 px. « Gagne si » devient une citation (filet laiton, Sitka 17 px).
- **Classement** : tuiles contour fin ; la première en laiton.
- **Mon pool** : tableau éditorial (noms en Sitka), puces rondes pour les suggestions.
- **Mes statistiques** : bandeau de chiffres entre deux filets, chiffre en Sitka 44 px.
- **Réglages** : onglet actif marqué au laiton.
- **Overlay** : cadre de panneau laiton sombre, « Recommandé » plein laiton, « Choix serré » laiton pointillé, nom du pick en Sitka.

## Statistiques et petits échantillons

Maquette : [`maquettes/statistiques-maquette.html`](maquettes/statistiques-maquette.html), capture [`planches/maquette-statistiques.png`](captures/planches/maquette-statistiques.png). **Données d'illustration**, inventées.

- À partir de 10 parties : pourcentage en grand, bilan en toutes lettres (« 14 victoires, 10 défaites »), barre arrondie avec repère à 50 %. Le chiffre mis en avant par DRAFT (taux de victoire global) est en laiton.
- Sous 10 parties : le grand chiffre est **le nombre de parties**, jamais un pourcentage ; jauge de 10 crans ; texte « Trop peu de parties : 6 sur 10. Pas encore de bilan. »
- À zéro : état vide écrit, en italique, dans un cadre pointillé.

## 1366 × 768 et 1920 × 1080

- 1366 : les portraits passent à 50 px sous 800 px de haut pour que la colonne adverse tienne. Le titre de l'Accueil descend à 38 px. Tout tient, mais avec moins de marge que A.
- 1920 : les grands portraits et le grand titre occupent mieux la place que A. Le vide sous Mon pool reste (même limite CSS que A).
- Overlay compact : en 36 px avec cadre, les noms longs se tronquent (« Sa… » pour Samira) et le libellé de rôle touche le cadre. Il faudrait retirer le libellé de rôle au-dessus des portraits (la couleur du cadre le remplace) : changement HTML.

## Changements de structure HTML suggérés (non faits)

1. **Accueil** : titre de une avec le portrait du conseil en blind (96 px) à droite, à la place du bouton seul.
2. **Overlay compact** : retirer le libellé de rôle au-dessus du portrait ; la couleur du cadre et l'icône suffisent.
3. **Simulateur** : utiliser l'illustration « chargement » verticale de Data Dragon (portrait en pied) dans les tuiles de classement, au lieu de l'icône carrée. Demande une nouvelle URL d'image et un cache.
4. **Statistiques** : même règle des 10 parties que A dans le bandeau de chiffres.

## Effort et risques

- Effort : une PR de 4 à 5 jours. Même base CSS que A, plus : réglages typographiques Sitka (chiffres alignés partout, tailles minimales), réglages de hauteur en 1366, refonte de l'overlay compact, nouvelle image de chargement si retenue.
- Risques : la serif peut sembler « magazine » plutôt que « performance » ; Sitka est moins lisible que Bahnschrift sous 14 px (à garder hors des libellés) ; jungle et victoire proches ; laiton et or de League of Legends voisins : à garder sobre pour ne pas imiter le client Riot.
