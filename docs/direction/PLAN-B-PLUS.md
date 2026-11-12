# DRAFT 3.0 · Plan de mise en œuvre B+

Source : [`BRIEF-B-PLUS.pdf`](BRIEF-B-PLUS.pdf), décision de l'utilisateur.
Base : B « Salle de stratégie » pour l'identité, A « Analyste » pour la lecture des chiffres.
Études de départ (non refaites) : branche `etude/direction-artistique`, `docs/direction/` (DIAGNOSTIC, DIRECTION-A, DIRECTION-B, COMPARATIF).
Branche : `da/b-plus`, empilée sur `i18n/1-extraction-fr` (PR #27). Audit du résultat : [`AUDIT-B-PLUS.md`](AUDIT-B-PLUS.md).

## 1. Principes

1. **Une couleur, un sens.** L'or ne sert qu'au conseil. Il ne marque ni la navigation, ni les onglets, ni les cases cochées.
2. **Hiérarchie et couleur ensemble.** Un conseil se reconnaît à sa place, sa taille et son étiquette écrite, pas seulement à l'or. Une menace porte toujours le mot « Menace » ou « Menaces ».
3. **Serif rare.** Sitka pour la marque, les titres de page, le nom du pick conseillé dans le Simulateur et les chiffres héros. Tout le reste reste en Bahnschrift : lecture, panneaux, commandes, réglages, petits pourcentages.
4. **Moins de boîtes.** Les cartes identiques deviennent des colonnes séparées par des filets, ou des bandeaux.
5. **Pas de chiffre trompeur.** Sous le seuil d'échantillon, on montre le nombre de parties et une jauge, jamais un pourcentage.
6. **Pas de décor.** Ni néon, ni lueur, ni animation nouvelle. Les animations existantes (Motion) sont gardées telles quelles.

## 2. Jetons

Tous les jetons sont dans le premier `:root` de `web/index.html`. Les anciens noms (`--accent`, `--gold`) restent comme alias, mais plus aucune règle de la feuille ne s'en sert.

### Couleurs avec leur sens

| Jeton | Valeur | Sens, et seulement ce sens |
|---|---|---|
| `--bg` | `#17141D` | Fond : graphite très sombre, nuance prune discrète. |
| `--surface` | `#1E1A25` | Zone groupée : bandeau, colonne, boîte de page. |
| `--raised` | `#26202E` | Ligne active, survol, piste de barre. |
| `--hover` | `#2C2535` | Survol de navigation. |
| `--line` | `#362F40` | Filet décoratif. |
| `--line-strong` | `#82778F` | Contour de champ et de bouton secondaire (≥ 3:1). |
| `--text` | `#F2ECE4` | Ivoire chaud. Aussi : sélection, focus, onglet actif. |
| `--muted` | `#B5ABA3` | Libellés, aides. |
| `--prio` | `#E5B55A` | **Or stratégique** : pick conseillé, « Recommandé », « Choix net / serré », action principale, favori du pool. |
| `--good` | `#82CFA8` | **Vert fonctionnel** : victoire, force, réglage réussi, panneaux prêts. |
| `--threat` | `#F1847A` | **Corail** : menace, défaite, faiblesse, donnée périmée. |
| `--conn` | `#79C6DA` | **Connexion League** : état de la lecture du client (point, filet). |
| `--secure` | `#C9A7E8` | **Sécurité** : avertissement sur l'autorisation de Riot, lecture seule. |
| `--info` | `#A3B4EC` | Hypothèse et incertitude : rôle probable, petit échantillon, mode Simulation. |
| `--r-top` … `--r-support` | `#E39B62` `#9DBF6E` `#6FB0EC` `#EC8AAE` `#B69AEE` | Rôles : cadre de portrait et icône de rôle seulement. |

Encres sur aplat : `--prio-ink #241806`, `--good-ink #0B2618`, `--threat-ink #2B0D0A`.

Règle de séparation : les couleurs de rôle ne touchent que les cadres de portraits et les icônes ; les couleurs de sens touchent le texte, les barres et les filets d'état. Le Mid (bleu) et la Connexion (cyan) sont voisins : ils ne sont jamais au même endroit.

### Typographie (polices de Windows, aucune police externe)

| Usage | Police | Taille |
|---|---|---|
| Marque DRAFT | Sitka Banner semi-gras | 26 px (fenêtre), 20 px (panneaux) |
| Titres de page | Sitka Banner semi-gras | 34 px |
| Bannière de l'Accueil | Sitka Banner semi-gras | 30 px (24 px sous 700 px de haut) |
| Nom du pick conseillé (Simulateur) | Sitka Banner semi-gras | 42 à 72 px selon la largeur |
| Chiffres héros (Mes statistiques) | Sitka Banner, `lining-nums tabular-nums` | 40 px |
| Titres de section, noms de champions, panneaux | Bahnschrift SemiBold Condensed | 15 à 19 px |
| Texte courant, boutons, réglages, pourcentages | Bahnschrift | 12 à 15 px |

Tous les nombres : `font-variant-numeric: lining-nums tabular-nums` (jeton `--figures`). Sitka dessine sinon des chiffres elzéviriens, illisibles en colonne.

### Échelle, espacements, rayons

- Texte : 12, 13, 15, 18, 24, 34 px (inchangé), plus 30 et 40 px pour la bannière et les chiffres héros.
- Espacements : 4, 8, 12, 16, 24, 32 px (inchangés).
- Rayons : étiquette en pilule (999 px), contrôle 6 px, boîte 8 px (au lieu de 10).

## 3. Composants revus

| Composant | Avant | B+ |
|---|---|---|
| Bouton principal `.cta` | Turquoise | Or, encre sombre, 18 px semi-gras. Un seul par écran. |
| Boutons secondaires | Contour gris-bleu | Contour `--line-strong`. Inchangés dans leur forme. |
| Navigation active | Turquoise | Fond `--raised`, filet ivoire, texte semi-gras. |
| Onglets, sous-navigation | Turquoise | Filet ivoire. |
| Étiquette « Recommandé », « Choix net » | Turquoise | Pilule or pleine. |
| « Choix serré », égalités | Or plein | Or en pointillé. |
| « Alternative » | Contour gris | Pilule contour `--line-strong`. |
| « Moins adapté », menaces | Corail | Corail (inchangé) avec le mot écrit. |
| Bans | Or | Portraits en gris, libellé neutre. |
| Profil adverse (pips) | Turquoise | Ivoire doux ; la catégorie dangereuse en corail. |
| Barres de victoire | Turquoise | Vert, repère à 50 %. Sous le seuil : jauge de crans. |
| Bandeau Simulation | Or | Pointillé pervenche (`--info`) : c'est une hypothèse, pas un conseil. |
| Cases, boutons radio | Turquoise | Ivoire ; le favori du pool en or. |
| Boîtes `.box` | Contour 1 px | Sans contour : l'aplat suffit. |

## 4. Changements HTML ciblés (sans toucher la navigation)

| Écran | Changement | Fichier |
|---|---|---|
| Accueil | Portraits du pool en 48 px avec le nom dessous ; le champion conseillé en blind porte un cadre or ; la phrase du conseil en blind a un filet or. | `HomePool` |
| Accueil | La chaîne League, pool, panneaux devient un bandeau à trois colonnes séparées par des filets (CSS) ; la colonne du pool est plus large. | CSS |
| Mon pool | Lignes du tableau marquées `is-fav` (portrait cadré d'or) et `is-out` (portrait gris, nom atténué) ; portraits 40 px. | `PoolPage` |
| Simulateur | Portraits du classement 44 px, plan de jeu 28 px. Cadre de rôle par CSS (`data-slot` existait déjà). | `Ranking`, `SidePanel` |
| Simulateur | Correction : en mode Simulation, le bandeau prenait la ligne flexible de la grille (vide de 300 px en 1920). | CSS |
| Mes statistiques | Sous le bandeau de chiffres : période (première et dernière draft notée), nombre de parties terminées et règle d'échantillon. Jauge de crans sous le seuil, pourcentage au-dessus. | `StatsKpis`, `SampleGauge` |
| Mes statistiques | Barres par champion et par type de mid : sous le seuil, jauge de crans au lieu d'une barre de victoires. Au-dessus : barre verte avec repère à 50 %. | `WinBars` |
| Panneaux | Portrait de la menace 36 px. Cadre or du conseil 2 px. | `VerdictPanel`, CSS |

Nouveaux textes, tous dans `web/i18n/fr.js` : `stats.kpi.rate`, `stats.sample.period`, `stats.sample.periodOne`, `stats.sample.played`, `stats.sample.few`, `stats.sample.enough`, `stats.sample.none`, `stats.gauge.aria`.

## 5. Portraits

| Contexte | Taille | Traitement |
|---|---|---|
| Draft adverse (Simulateur) | 44 à 76 px (selon la hauteur) | Recadré à 110 %, cadre 2 px de la couleur du rôle à 2 px du visage. Libellé du rôle écrit à côté. |
| Panneau gauche | 44 px (38 px à 125 %) | Idem, cadre à 1 px. |
| Panneau compact | 34 px (30 px sous 600 px de haut) | Idem, cadre à 1 px. Le libellé du rôle reste au-dessus. |
| Rôle supposé (Mid probable) | — | Cadre en pointillé + mention écrite en pervenche. |
| Classement du pool | 44 px | Tuile conseillée : contour et nom en or. |
| Plan de jeu | 28 px | Menace : anneau corail. Ban : gris. |
| Accueil (pool) | 48 px, nom dessous | Conseil en blind : cadre or et nom en or. |
| Mon pool (tableau) | 40 px | Favori : cadre or. Exclu : gris à 60 %. |
| Puces de suggestion | 28 px, rondes | — |
| Portrait absent (réseau coupé, nom inconnu) | Même taille | Deux initiales en Sitka sur fond `--raised`, cadre pointillé. La place ne bouge pas. |

## 6. Statistiques : seuil et règles

**Seuil retenu : 10 parties terminées** (constante `MIN_GAMES` déjà utilisée par l'application).

Pourquoi 10, pour DRAFT :
- C'est déjà la règle des barres de victoires et des bilans de matchup (« trop peu de parties »). Un second seuil sur la même page créerait deux règles contradictoires.
- Le journal se remplit à la main (« Je joue » puis le résultat) : quelques parties par soirée. Dix parties, c'est environ une semaine de jeu régulier sur un rôle. Le seuil est atteignable, il ne bloque pas l'écran pendant des mois.
- Sous 10 parties, une partie déplace le pourcentage de plus de 10 points. À 3 victoires sur 6, l'intervalle de Wilson à 95 % va d'environ 19 % à 81 % : le chiffre ne dit rien.
- À 10 parties, l'intervalle reste large (5 sur 10 : environ 24 % à 76 %). C'est pourquoi, même au-dessus du seuil, la ligne d'échantillon dit « il reste indicatif sur un petit nombre » et le nombre de parties reste écrit à côté du pourcentage.

Ce seuil est un choix d'usage pour l'application, pas une preuve statistique.

Règles d'affichage :
- **0 partie** : aucun chiffre héros inventé ; le bandeau affiche 0 et la ligne « Aucune draft notée : la période et le nombre de parties s'afficheront ici. »
- **1 à 9 parties** : le grand chiffre reste le nombre de victoires « sur N parties terminées » ; une jauge de 10 crans montre l'échantillon ; texte en pervenche « Moins de 10 parties terminées : pas encore de pourcentage, l'écart peut venir du hasard. » Les barres par champion deviennent des jauges.
- **10 parties et plus** : « 60 % de victoires » sous le chiffre, toujours avec « sur N parties terminées ». Barre verte avec repère à 50 %.
- **Période** toujours écrite : « Période : du 02/11/2026 au 11/11/2026 » (dates de la première et de la dernière draft notée).
- Aucune statistique fictive en production. Les captures avec journal portent « Données de test » dans leur nom.

## 7. Accessibilité

- Contrastes : mesurés dans [`AUDIT-B-PLUS.md`](AUDIT-B-PLUS.md#contrastes-mesurés). Texte ≥ 4,5:1, éléments d'interface ≥ 3:1. Le plus faible : contour de contrôle sur surface haute, 3,75:1.
- Focus : contour ivoire 2 px partout (`--focus`), y compris sur l'or du bouton principal. Les règles d'offset existantes (focus intérieur pour les boutons serrés) sont gardées.
- Survol : fond `--raised` ou contour `--muted`, jamais une couleur de sens.
- Troncatures : noms de champions sur deux lignes au lieu d'être coupés (Accueil, panneau compact) ; vérifiées par `run-layout-tests` et la pseudo-langue.
- Sens jamais porté par la couleur seule : chaque état coloré a un mot (« Recommandé », « Menace », « Victoire », « Exclu pour l'instant », « Simulation ») ou une forme (pointillé pour l'incertain, gris pour les bans, crans pour l'échantillon).
- 1366 × 768 et 125 % (1093 × 614 et panneaux de 256 px) : vérifiés en capture réelle.

## 8. Découpage en PR

Une seule PR (`da/b-plus`), empilée sur #27, en commits séparés : plan, jetons et règles, statistiques, captures et audit. Si la revue préfère, elle peut se scinder ainsi :
1. **DA-1 jetons et couleurs de sens** (feuille seule, aucun texte).
2. **DA-2 portraits et statistiques** (tailles, cadres, seuil, jauges, nouveaux textes).
3. **DA-3 captures et audit**.

## 9. Risques

- **Sitka** est présente sur Windows 10 et 11 ; repli Georgia. Sur une installation sans Sitka, les titres passent en Georgia (lisible, moins typé).
- **Or proche de l'or du client Riot** : il reste réservé au conseil et n'habille pas la fenêtre.
- **Jungle (vert olive) et victoire (vert menthe)** : teintes distinctes, et jamais au même endroit (cadre de portrait contre texte).
- **Support (lilas) et sécurité (lavande)** : idem, séparés par l'endroit.
- **Tests visuels** : les bancs vérifient les débordements, pas le goût. L'audit liste ce qui reste à reprendre.
- **Pas de changement du moteur** : vérifié par `run-engine-eval -AvecEvaluation` (0 différence).
