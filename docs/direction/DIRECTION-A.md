# Direction A — « Analyste »

> Un instrument de lecture de draft. L'ardoise reste calme ; la couleur ne sert qu'à dire quelque chose.

Feuille d'étude : [`direction-a.css`](direction-a.css) (≈ 230 lignes, chargée après la feuille de l'application).
Captures : [`captures/direction-a/`](captures/direction-a/). Comparaisons : [`captures/planches/`](captures/planches/).

## Idée

DRAFT sert pendant 30 secondes de sélection. A traite l'écran comme un tableau d'analyse :
- les actions sont **ivoire**, sans couleur, pour que la couleur reste libre pour le sens ;
- **chaque teinte a un seul sens**, écrit dans la légende ci-dessous ;
- les informations se rangent en **listes et bandeaux** sur des surfaces en paliers, plus en cartes bordées.

Ce qui la rend reconnaissable : le bouton ivoire, les chiffres en Bahnschrift condensée grasse, le pied de couleur de rôle sous chaque portrait.

## Palette et sens

| Rôle | Valeur | Sens, et seulement ce sens |
|---|---|---|
| Fond | `#0f151c` | Ardoise (bleu-gris, pas noir pur). |
| Surface | `#151d26` | Zone groupée (liste, colonne). |
| Surface haute | `#1c2631` | Ligne active, piste de barre. |
| Filet | `#26323f` | Séparateur décoratif. |
| Contour de contrôle | `#6b7c94` | Champs, boutons secondaires (≥ 3:1). |
| Texte | `#e8eef4` | Texte principal. |
| Texte secondaire | `#9dabbb` | Libellés, aides. |
| Signal ivoire | `#eef2f6` | Action principale, sélection, focus. Jamais un résultat. |
| Avantage / victoire | `#3fd0a0` | Bon pour toi : pick recommandé, cible, victoire, connexion active. |
| Menace / défaite | `#ff7b74` | Mauvais pour toi : menace, défaite, faiblesse. |
| Risque | `#f0b24a` | À surveiller : choix serré, client en attente, donnée ancienne. |
| Hypothèse | `#97b6ff` | Déduit, pas certain : rôle probable, mid supposé. Toujours avec un contour **pointillé**. |
| Simulation | `#c9a3ff` | Rien de réel : bandeau **hachuré** du mode simulation. |
| Rôle Top | `#cfa77c` | Uniquement avec l'icône Top. |
| Rôle Jungle | `#9cc07a` | Uniquement avec l'icône Jungle. |
| Rôle Mid | `#82bfdc` | Uniquement avec l'icône Mid. |
| Rôle ADC | `#dc98b8` | Uniquement avec l'icône ADC. |
| Rôle Support | `#b1aae4` | Uniquement avec l'icône Support. |

Règles :
- Les couleurs de rôle sont **douces** (peu saturées) ; les couleurs de sens sont **vives**. On ne confond pas « Jungle » et « victoire » : l'une est un liseré pâle sous un portrait, l'autre un texte ou une barre vive.
- Une couleur de sens n'est jamais seule : il y a toujours un mot (« Menace », « Victoire ») ou un signe (barré, pointillé, hachure). Lisible en daltonisme rouge-vert.
- Les bans : portrait grisé et barré d'un trait corail, pas une couleur de plus.

Contrastes mesurés : tableau complet dans [COMPARATIF](COMPARATIF.md#contrastes-mesurés). Tout texte ≥ 6,5:1, contours ≥ 3,99:1.

## Typographie (polices Windows, aucune police externe)

| Usage | Police | Taille |
|---|---|---|
| Chiffres héros, nom du pick, titres de page | Bahnschrift Bold Condensed | 30 / 42 / 46 à 72 px |
| Titres de section, noms de champions | Bahnschrift SemiBold SemiCondensed | 17 à 19 px |
| Libellés, onglets, boutons | Bahnschrift SemiCondensed | 13 à 15 px |
| Texte courant (phrases) | Segoe UI Variable Text, sinon Segoe UI | 14 px, interligne 1,45 |

Échelle : 12, 13, 14, 15, 17, 19, 30, 42, 72. Pas de capitales. Chiffres tabulaires partout.
Pourquoi Segoe UI pour les phrases : Bahnschrift en 13 px fatigue sur trois lignes ; elle reste pour tout ce qui est court.

## Portraits

| Contexte | Taille | Traitement |
|---|---|---|
| Ligne adverse (Simulateur) | 48 px | Recadré à 112 % (le liseré Riot disparaît), pied de 3 px de la couleur du rôle. |
| Panneau gauche | 44 px | Idem. |
| Panneau compact | 40 px | Idem. |
| Classement du pool | 40 px | Pas de couleur de rôle (c'est ton rôle). |
| Plan de jeu | 28 px | Menace : anneau corail. Ban : grisé et barré. |
| Accueil, tableau de pool | 32 et 40 px | Sans cadre. |
| Rôle supposé | — | Contour pointillé pervenche à 2 px du portrait. |
| Portrait absent | même taille | Initiales en Bahnschrift sur la surface haute, pied de rôle conservé. |

## Composants

- **Accueil** : la chaîne de préparation devient **une liste à colonnes fixes** (sujet, état, détail, action), avec le liseré d'état à gauche. Plus trois cartes identiques. Mise à jour et données passent en texte simple sous un filet.
- **Simulateur** : lignes adverses à hauteur fixe (64 px), séparées par un filet, sans fond ; seule la ligne de ton vis-à-vis a une surface. Le profil adverse est gris, seul le danger est corail. L'illustration est désaturée et voilée, le nom du pick passe en 46 à 72 px.
- **Classement** : tuiles sans contour, le premier porte un liseré menthe.
- **Mon pool** : rôles en liste avec icône colorée, tableau aéré (portraits 40 px).
- **Mes statistiques** : bandeau de chiffres héros sans boîte ; 42 px de chiffre, libellé au-dessus, échantillon dessous.
- **Réglages** : groupes marqués par un liseré gauche, plus de cadre.
- **Overlay** : mêmes règles, bouton ivoire, « Recommandé » menthe, « Choix serré » ambre, menace corail.

## Statistiques et petits échantillons

Maquette : [`maquettes/statistiques-maquette.html`](maquettes/statistiques-maquette.html), capture [`planches/maquette-statistiques.png`](captures/planches/maquette-statistiques.png). **Données d'illustration**, inventées.

- À partir de 10 parties : chiffre héros (pourcentage), couleur de sens, barre avec repère à 50 %, et l'échantillon écrit dessous (« 14 V, 10 D »).
- Sous 10 parties : **ni pourcentage ni couleur**. Le chiffre affiché est le nombre de parties ; une jauge de 10 crans montre ce qui manque ; texte : « Trop peu de parties pour conclure : 6 sur 10. »
- À zéro : pas de « 0 » géant. État vide écrit (ce que montre la vraie capture, avec la boîte d'aide actuelle).

## 1366 × 768 et 1920 × 1080

- 1366 : tout tient sans défilement sur Accueil, Simulateur, Statistiques. Les lignes à hauteur fixe libèrent la place du profil adverse.
- 1920 : les lignes adverses ne s'étirent plus. Limite de l'étude CSS : la place libérée reste vide sous le profil adverse et sous Mon pool. Il faut un petit changement HTML (voir plus bas).

## Changements de structure HTML suggérés (non faits)

1. **Accueil** : mettre le conseil en blind dans le bandeau du haut, avec le portrait 64 px du champion conseillé. C'est l'information la plus utile de la page.
2. **Mes statistiques** : le bandeau de chiffres doit appliquer la règle des 10 parties (aujourd'hui « 0 sur 0 » s'affiche en grand).
3. **Simulateur 1920** : sous le profil adverse, afficher le plan de jeu complet quand la colonne de droite est pleine, ou passer la colonne gauche en deux rangées.
4. **Pool** : donner `data-slot` au tableau et aux tuiles de classement, pour porter la couleur de rôle partout de la même façon.

## Effort et risques

- Effort : une PR de 2 à 3 jours. Jetons de couleur, ≈ 250 lignes de CSS qui remplacent des règles existantes, les 4 changements HTML ci-dessus, mise à jour des tests de contraste et des captures.
- Risques : l'ivoire du bouton principal peut sembler « neutre » au premier regard (c'est voulu) ; il faut veiller à ne pas réintroduire de couleur « décorative » plus tard. Sept teintes vives + cinq douces demandent une légende dans l'aide.
