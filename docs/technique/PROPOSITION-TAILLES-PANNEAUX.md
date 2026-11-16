# Proposition : des panneaux plus confortables

**Statut :** proposition, rien n'est changé dans l'application. L'utilisateur trouve le panneau de 222 px étroit. Il préfère la lisibilité et l'accès rapide à la compacité.

Tailles en pixels CSS (logiques). Les mesures viennent de Chrome sans interface, à 125 %, sur `web/draft-tailles-tests.html`, avec des données fictives et la lecture du client désactivée. Dans cette page de test seulement, la taille du panneau est une option (`w`, `h`, `seuil`).

## Tailles actuelles (placement `Layout`)

Les valeurs viennent de `Layout.Compute` et de `Layout.Narrowest`, appelés dans l'exe compilé (`tools/layout-table.ps1`, sans fenêtre).

Hypothèses :
- client LoL centré ;
- barre des tâches de 40 px en bas.

Abréviations du tableau :
- « libre » : place de chaque côté du client ;
- « étroit » : faute de place, panneau unique de 230 px au bord de l'écran, qui couvre une partie du client.

La zone utile mesurée dans la WebView2 (e2e) est de 4 à 8 px plus étroite et 9 px moins haute (230 × 520 donne 222 × 511 ; 256 × 576 donne 252 × 567).

**Client à la taille choisie dans LoL (pixels physiques : 1024 × 576, 1280 × 720, 1600 × 900).** C'est le cas mesuré dans l'e2e.

| Écran | Échelle | Client | Libre | Panneaux |
|---|---|---|---|---|
| 1366 × 768 | 100 % | 1024 × 576 | 171 | étroit 230 × 576, couvre 59 du client |
| 1366 × 768 | 100 % | 1280 × 720 | 43 | étroit 230 × 720, couvre 187 |
| 1366 × 768 | 125 % | 1024 × 576 | 137 | **étroit 230 × 520, couvre 93** (zone utile 222 × 511) |
| 1366 × 768 | 150 % | 1024 × 576 | 114 | étroit 230 × 472, couvre 116 |
| 1600 × 900 | 100 % | 1024 × 576 | 288 | deux 288 × 576 |
| 1600 × 900 | 100 % | 1280 × 720 | 160 | étroit 230 × 720, couvre 70 |
| 1600 × 900 | 125 % | 1024 × 576 | 230 | deux 230 × 520 |
| 1600 × 900 | 125 % | 1280 × 720 | 128 | étroit 230 × 576, couvre 102 |
| 1600 × 900 | 150 % | 1024 × 576 | 192 | étroit 230 × 520, couvre 38 |
| 1600 × 900 | 150 % | 1280 × 720 | 107 | étroit 230 × 520, couvre 123 |
| 1920 × 1080 | 100 % | 1280 × 720 | 320 | deux 320 × 720 |
| 1920 × 1080 | 100 % | 1600 × 900 | 160 | étroit 230 × 900, couvre 70 |
| 1920 × 1080 | 125 % | 1280 × 720 | 256 | **deux 256 × 576** (zone utile 252 × 567) |
| 1920 × 1080 | 125 % | 1600 × 900 | 128 | étroit 230 × 720, couvre 102 |
| 1920 × 1080 | 150 % | 1280 × 720 | 213 | étroit 230 × 520, couvre 17 |
| 1920 × 1080 | 150 % | 1600 × 900 | 107 | étroit 230 × 600, couvre 123 |
| 2560 × 1440 | 100 % | 1280 × 720 | 640 | deux 340 × 720 |
| 2560 × 1440 | 125 % | 1600 × 900 | 384 | deux 340 × 720 |
| 2560 × 1440 | 150 % | 1600 × 900 | 320 | deux 320 × 600 |

Les cas absents ne tiennent pas sur l'écran : à 1366 × 768, 1280 × 720 ne tient pas à 125 et 150 %, et 1600 × 900 à aucune échelle. Les autres cas de 2560 × 1440 donnent deux panneaux de 340 de large.

**Client de 1280 × 720 ou 1600 × 900 logiques** (pixels physiques = logiques × échelle) :

| Écran | 100 % | 125 % | 150 % |
|---|---|---|---|
| 1366 × 768 | 1280 : étroit 230 × 720, couvre 187 | rien ne tient | rien ne tient |
| 1600 × 900 | 1280 : étroit 230 × 720, couvre 70 | ne tient pas | ne tient pas |
| 1920 × 1080 | 1280 : deux 320 × 720 ; 1600 : étroit 230 × 900, couvre 70 | 1280 : étroit 230 × 720, couvre 102 | ne tient pas |
| 2560 × 1440 | 1280 : deux 340 × 720 ; 1600 : deux 340 × 900 | 1280 : deux 340 × 720 ; 1600 : étroit 230 × 900, couvre 6 | 1280 : étroit 230 × 720, couvre 17 ; 1600 : couvre 177 |

**Ce qui gêne :** 230 px (222 utiles) dès que la place manque, et une hauteur de 520 px quand le client est petit. Le cas de l'utilisateur, 1366 × 768 à 125 %, cumule les deux.

## Proposition

1. **Largeur minimale confortable de 272 px** (264 utiles) pour le panneau qui porte le conseil :
   - deux panneaux : inchangé (230 au moins, 340 au plus) ;
   - panneau unique : au moins 272 quand un côté a cette place, jusqu'à 374 ;
   - faute de place des deux côtés : panneau étroit de 272 au lieu de 230.
2. **Hauteur :** le panneau unique et le panneau étroit prennent toute la hauteur utile de l'écran. À 1366 × 768 à 125 %, cela fait 574 au lieu de 520. Les deux panneaux restent à leur hauteur (gain mesuré faible, voir plus bas).
3. **Resserrement :** les règles actuelles sous 540 px de haut (illustration du conseil 68 px, écarts 5 px, barre réduite, recherche masquée quand 5 adversaires sont saisis) s'appliqueraient jusqu'à 600 px.
4. **Quand la place manque :**

   | Cas (client centré) | Aujourd'hui | Proposé, client centré | Proposé, client calé à gauche |
   |---|---|---|---|
   | 1366 à 125 %, 1024 × 576 | 230, couvre 93 | 272, couvre 135 (+42) | 272, ne couvre rien (274 libres) |
   | 1366 à 100 %, 1024 × 576 | 230, couvre 59 | 272, couvre 101 | 272, rien (342 libres) |
   | 1366 à 150 %, 1024 × 576 | 230, couvre 116 | 272, couvre 158 | 272, couvre 44 (228 libres) |
   | 1600 à 125 %, 1280 × 720 | 230, couvre 102 | 272, couvre 144 | 272, couvre 16 (256 libres) |
   | 1920 à 150 %, 1280 × 720 | 230, couvre 17 | 272, couvre 59 | 272, rien (427 libres) |
   | 1600 à 125 %, 1024 × 576 | deux de 230 | inchangé, ou un seul de 272 qui couvre 42 | un seul de 272, rien (461 libres) |

   Trois réponses possibles :
   - **chevauchement léger :** couvrir un peu plus le bord droit du client ;
   - **panneau unique :** garder un seul panneau, plus large, plutôt que deux étroits ;
   - **bascule en compact :** déjà automatique quand un seul côté a de la place.

   **Risque.** Le panneau étroit est posé au bord droit de l'écran. Il couvre donc le bord droit du client, où la sélection affiche d'habitude l'équipe adverse, ses choix et ses noms. À 1366 à 125 %, 135 px couvrent 16 % de la largeur du client ; à vérifier sur un vrai client. Sans lecture du client, le joueur doit voir ces choix pour les saisir.

   **Recommandation :**
   - aller jusqu'à 272 seulement si le chevauchement ajouté reste sous 48 px. C'est le cas de 1366 à 125 % (+42) ; sinon, garder 230 ;
   - ajouter une aide, une seule fois : « Place le client LoL contre le bord gauche de l'écran : le panneau aura 272 px sans rien couvrir ». DRAFT ne déplace jamais le client.

## Mesures avant et après

Pools Mid fictifs (Twisted Fate, Vex, Fizz, Xerath). Adversaires : Zed, Malphite, Vi, Caitlyn, Leona. « Consultation » : Vex consulté.

L'essentiel est compté visible quand il est entier et au-dessus de la barre d'actions collée en bas. Sept éléments sont suivis : rôle, état du rôle, conseil, menace, champion consulté, alternative, raisons.

| Cas | Zone utile | État | Visibles sans défiler | Barre d'actions | Contenu total |
|---|---|---|---|---|---|
| 1366 à 125 %, actuel | 222 × 511 | draft | 4/6 : rôle, état, conseil, menace (alternative 412..474 sous la barre à 456) | 456..511 | 583 |
| | | consultation | 4/7 : la carte « Consulté » (428..464) passe sous la barre (426) | 426..511 | 809 |
| 1366 à 125 %, proposé (272 × 574, seuil 600) | 264 × 565 | draft | 5/6 : + alternative (412..465) | 509..565 | 558 |
| | | consultation | 5/7 : + champion consulté (428..464, barre à 479) | 479..565 | 774 |
| 1920 à 125 %, actuel | 252 × 567 | draft | 6/6 | 474..567 | 515 |
| | | consultation | 5/7 | 463..567 | 714 |
| 1920 à 125 %, proposé (hauteur 640) | 252 × 631 | draft | 6/6 | 576..631 | 512 |
| | | consultation | 5/7 (l'alternative reste sous la barre : l'illustration du conseil grandit avec la hauteur) | 527..631 | 742 |

En anglais, les mêmes cas donnent le même nombre d'éléments visibles (4/6, 4/7 et 5/6, 5/7 à 1366 ; 6/6 et 5/7 à 1920).

**Texte et actions :**
- Les tailles de police ne changent pas : conseil 24 px (26 à 1920, panneau de 631 px), menace 13, bouton 15, état du rôle 13, raisons 13.
- À 222 px, « Détails » n'est qu'une icône (32 px), et « I'm playing Twisted Fate » passe sur deux lignes dans le bouton.
- À 264 px, « Détails » retrouve son libellé (86 px), et le bouton tient sur une ligne en français comme en anglais.
- Le rappel « Conseil : X » reste masqué dans la barre sous le seuil de resserrement.

**Lecture :**
- À 1366 à 125 %, la proposition rend visibles l'alternative (en draft) et le champion consulté (en consultation), sans défiler. Elle rend aussi son libellé à « Détails ».
- À 1920 à 125 %, la hauteur seule n'apporte presque rien : mêmes éléments visibles. Elle n'est pas recommandée.

## Captures

Dans `docs/qa/captures-tailles-panneaux/` : rendu Chrome à 125 % de la page de test, en consultation (Vex), en français.

| Fichier | Taille utile |
|---|---|
| `panneau-1366-125-actuel-fr.png` | 222 × 511, panneau unique actuel |
| `panneau-1366-125-propose-fr.png` | 264 × 565, proposé, seuil 600 |
| `panneau-1920-125-actuel-fr.png` | 252 × 567, panneau droit actuel |
| `panneau-1920-125-propose-fr.png` | 252 × 631, hauteur 640 |

## À décider

- La largeur de 272 et sa limite de chevauchement (+48 px).
- L'aide « caler le client à gauche ».
- Le seuil de resserrement à 600.
- La hauteur pleine pour le panneau unique.

Rien de cela n'est activé. La page de test, `tools/run-tailles-panneaux.ps1` (mesures et captures) et `tools/layout-table.ps1` permettent de remesurer chaque variante.
