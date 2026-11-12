# DRAFT 3.0 · Audit visuel final B+

Branche `da/b-plus` (empilée sur `i18n/1-extraction-fr`, PR #27). Plan : [`PLAN-B-PLUS.md`](PLAN-B-PLUS.md). Brief : [`BRIEF-B-PLUS.pdf`](BRIEF-B-PLUS.pdf).
Cet audit juge le **rendu réel** dans WebView2. Les tests verts ne prouvent pas que l'écran est bon : la liste des défauts restants est en fin de document.

## Méthode des captures

- **Avant** : exe construit depuis la base `5d859a8` (`build.ps1`, dossier temporaire). **Après** : exe construit depuis cette branche.
- Lancement sur un **bureau Windows caché** (aucune fenêtre sur l'écran), page capturée par le protocole de débogage de la WebView2, taille fixée par `Emulation.setDeviceMetricsOverride` avec la session gardée ouverte.
- Tailles : 1366 × 768 et 1920 × 1080 à l'échelle 1. Contrôle à **125 %** (échelle réelle du PC, 120 dpi) : page de 1093 × 614 à l'échelle 1,25, panneaux de 256 × 608 à l'échelle 1,25 (320 px physiques).
- Données **neutres** : copie du profil d'essai (rôle Mid, pool Fizz, Twisted Fate, Kog'Maw), sans `draft-history.js*`, sans journal, sans relevé de validation. Lecture du client désactivée dans cette copie (aucun client League lu).
- Premier lancement : dossier de données vide.
- Simulateur : draft saisie par `draftApi` (Garen, Lee Sin, Syndra, Jinx, Thresh). Panneaux : démonstration de l'application (« Essayer les panneaux » : Sett, Vi, Galio, Samira, Janna).
- Réseau : tout est bloqué sauf `ddragon.leagueoflegends.com` (portraits et illustrations officiels, que l'application utilise normalement). Aucune autre connexion.
- **Données de test** : pour montrer Mes statistiques avec un petit échantillon, un journal **fictif** de 16 drafts (15 terminées, dont 12 sur un même champion) a été écrit dans la copie temporaire, capturé, puis effacé. Les fichiers concernés portent `donnees-de-test` dans leur nom. **Ces chiffres ne sont pas réels.**

## Captures et planches

| Écran | Planches (avant à gauche, après à droite) |
|---|---|
| Accueil configuré | [1366](captures-bplus/planches/accueil-1366.png) · [1920](captures-bplus/planches/accueil-1920.png) · [125 %](captures-bplus/planches/accueil-125pc.png) |
| Accueil, premier lancement | [1366](captures-bplus/planches/accueil-premier-lancement-1366.png) · [1920](captures-bplus/planches/accueil-premier-lancement-1920.png) · [125 %](captures-bplus/planches/accueil-premier-lancement-125pc.png) |
| Mon pool | [1366](captures-bplus/planches/pool-1366.png) · [1920](captures-bplus/planches/pool-1920.png) · [125 %](captures-bplus/planches/pool-125pc.png) |
| Simulateur, draft en cours | [1366](captures-bplus/planches/simulateur-1366.png) · [1920](captures-bplus/planches/simulateur-1920.png) · [125 %](captures-bplus/planches/simulateur-125pc.png) |
| Mes statistiques (état réel : vide) | [1366](captures-bplus/planches/stats-1366.png) · [1920](captures-bplus/planches/stats-1920.png) · [125 %](captures-bplus/planches/stats-125pc.png) |
| Mes statistiques, **données de test fictives** | [1366](captures-bplus/planches/stats-donnees-de-test-1366.png) · [1920](captures-bplus/planches/stats-donnees-de-test-1920.png) |
| Réglages, Général | [1366](captures-bplus/planches/reglages-general-1366.png) · [1920](captures-bplus/planches/reglages-general-1920.png) · [125 %](captures-bplus/planches/reglages-general-125pc.png) |
| Réglages, Connexion League | [1366](captures-bplus/planches/reglages-connexion-1366.png) · [1920](captures-bplus/planches/reglages-connexion-1920.png) · [125 %](captures-bplus/planches/reglages-connexion-125pc.png) |
| Panneaux gauche, droit, compact (340 × 760) | [panneaux](captures-bplus/planches/panneaux.png) |
| Panneaux à 125 % (256 px d'interface) | [panneaux-125pc](captures-bplus/planches/panneaux-125pc.png) |
| Planche globale | [avant](captures-bplus/planches/globale-avant.png) · [après](captures-bplus/planches/globale-apres.png) |

Captures brutes : [`captures-bplus/avant/`](captures-bplus/avant/) et [`captures-bplus/apres/`](captures-bplus/apres/) (30 chacune, mêmes noms).

![Planche globale, après](captures-bplus/planches/globale-apres.png)

## Ce qui a changé, écran par écran

### Toute l'application
- Fond graphite à nuance prune (`#17141D`), surfaces `#1E1A25` et `#26202E`. Le turquoise unique a disparu : chaque couleur a un seul sens (tableau du plan, § 2).
- Marque DRAFT et titres de page en Sitka. Le reste en Bahnschrift.
- Navigation active, onglets, sous-navigation, cases cochées : ivoire, plus aucune couleur de sens.
- Boîtes sans contour ; étiquettes en pilule ; rayon des boîtes 8 px.
- Focus : contour ivoire 2 px partout.

### Accueil
- Bannière d'état : titre en Sitka 30 px, filet vert quand tout est prêt, **un seul bouton or** (« Simuler une draft »).
- La chaîne League / pool / panneaux n'est plus trois cartes identiques : un bandeau à trois colonnes séparées par des filets, la colonne du pool plus large.
- Pool montré par les visages (48 px, 64 px sur grand écran), nom dessous. Le champion conseillé en blind porte un cadre or et son nom en or ; la phrase « En blind, DRAFT te conseille… » a un filet or.
- Connexion League en cyan (pointillé en attente), distincte du vert « prêt » et de l'or du conseil.
- Mise à jour et Données : sans boîte, sous un filet.
- Premier lancement : guide encadré d'or (étapes à faire), « Recommandée, facultative » en or, l'avertissement sur l'autorisation de Riot en lavande (sécurité).

### Mon pool
- Rôle joué : pilule ivoire pleine dans la barre, filet ivoire dans la liste des rôles, étiquette « Joué » en contour. Icônes de rôle à leur couleur.
- Tableau : portraits 40 px, noms en condensé 18 px ; ligne **favorite** : cadre or ; ligne **exclue** : portrait gris à 60 % et nom atténué (le mot « Exclu pour l'instant » reste en tête de colonne).
- Suggestions en puces rondes.

### Simulateur
- Grande illustration gardée. Nom du pick en Sitka (42 à 72 px). « Choix net » en pilule or pleine ; « Choix serré » en or pointillé ; bouton « Je joue … » en or.
- Draft adverse : cadre de la couleur du rôle autour de chaque visage, libellé du rôle écrit et coloré ; le Mid probable en cadre pointillé avec la mention en pervenche.
- Classement : tuile conseillée en or (contour, nom, barre, « Recommandé ») ; « Alternative » en contour ; « Moins adapté » en corail.
- Plan de jeu : « Gagne si » sous un filet or ; Menaces en corail ; Bans en gris.
- Mode Simulation : bandeau en pointillé pervenche. **Correction** : le bandeau prenait la ligne flexible de la grille (300 px vides en 1920) ; les lignes adverses ne s'étirent plus à 130 px sur grand écran.

### Panneaux
- Cadre or du conseil (2 px), « Recommandé » en pilule or, « Choix serré » en pointillé, second choix en contour neutre, menace en corail avec le mot « Menace », bouton « Je joue » en or.
- Draft adverse avec cadres de rôle (gauche et compact) ; libellés de rôle gardés.
- Marque en Sitka ; tout le reste en Bahnschrift, mêmes tailles qu'avant (lecture rapide).

### Mes statistiques
- Bandeau de chiffres entre deux filets, chiffres en Sitka 40 px à chiffres alignés.
- **Sous le bandeau, toujours** : la période (« Période : du 02/11/2026 au 11/11/2026 »), le nombre de parties terminées et la règle (« Moins de 10 parties terminées : pas encore de pourcentage… » ou « Pourcentage affiché à partir de 10 parties terminées ; il reste indicatif… »).
- Sous 10 parties : jauge de 10 crans sous « Victoires » ; au-dessus : « 60 % de victoires ».
- Barres par champion et par type de mid : sous 10 parties, **jauge de crans au lieu d'une barre de victoires** ; au-dessus, barre verte avec repère à 50 %.
- Journal vide : les zéros sont atténués et la ligne dit ce qui apparaîtra.

### Réglages
- Inchangés dans leur structure : rubrique active en ivoire, cases ivoire, état de la lecture du client en cyan quand elle est active. Aucun ornement.

## Critères d'acceptation du brief (§ 9)

| Critère | Verdict | Preuve |
|---|---|---|
| DRAFT immédiatement identifiable, pas un dashboard générique | **Atteint en grande partie.** Marque et titres en Sitka, prune, or réservé, cadres de rôle, visages : la planche globale ne ressemble plus au kit turquoise. Les Réglages restent volontairement neutres. | [globale-avant](captures-bplus/planches/globale-avant.png) / [globale-apres](captures-bplus/planches/globale-apres.png) |
| Recommandations et risques distingués par hiérarchie **et** couleur | **Atteint.** Conseil : place (scène, première tuile, bandeau du panneau), taille, étiquette « Recommandé / Choix net / serré » et or. Risque : mot « Menace(s) », filet gauche, corail. Hypothèse : pointillé et pervenche. | [simulateur-1366](captures-bplus/planches/simulateur-1366.png), [panneaux](captures-bplus/planches/panneaux.png) |
| Statistiques : chiffres utiles, période, taille de l'échantillon | **Atteint** pour la vue d'ensemble. Le seuil de 10 parties s'applique au bandeau et aux barres. L'onglet « Historique Riot » garde ses anciens tableaux (pourcentages par groupe, sans période propre). | [stats-donnees-de-test-1366](captures-bplus/planches/stats-donnees-de-test-1366.png) (données fictives) |
| Réglages sobres et rapides ; panneaux compacts et lisibles | **Atteint.** Réglages sans ajout ; panneaux aux mêmes tailles de texte, contrôlés à 340 px et à 256 px (125 %). | [reglages-connexion-1366](captures-bplus/planches/reglages-connexion-1366.png), [panneaux-125pc](captures-bplus/planches/panneaux-125pc.png) |
| Rendu réel validé dans WebView2 aux résolutions ciblées ; aucune fonction disparue | **Atteint.** 60 captures réelles ; bancs de pages, e2e, selftest et migration (ci-dessous). | § Tests |
| Audit final listant les défauts restants | Ce document, § Défauts restants. | — |

## Contrastes mesurés

Calcul WCAG 2.x (luminance relative), sur les valeurs exactes des jetons. Seuils : texte 4,5:1, éléments d'interface 3:1.

| Usage | Avant-plan | Fond | Rapport | Seuil | Verdict |
|---|---|---|---|---|---|
| Texte principal | `#F2ECE4` | `#17141D` | 15,50:1 | 4,5:1 | conforme |
| Texte principal sur surface haute | `#F2ECE4` | `#26202E` | 13,47:1 | 4,5:1 | conforme |
| Texte secondaire | `#B5ABA3` | `#17141D` | 8,07:1 | 4,5:1 | conforme |
| Texte secondaire sur surface | `#B5ABA3` | `#1E1A25` | 7,58:1 | 4,5:1 | conforme |
| Texte secondaire sur surface haute | `#B5ABA3` | `#26202E` | 7,01:1 | 4,5:1 | conforme |
| Or en texte (Choix serré, Gagne si, nom conseillé) | `#E5B55A` | `#1E1A25` | 9,02:1 | 4,5:1 | conforme |
| Or sur surface haute | `#E5B55A` | `#26202E` | 8,35:1 | 4,5:1 | conforme |
| Action principale (encre sur or) | `#241806` | `#E5B55A` | 9,18:1 | 4,5:1 | conforme |
| Vert en texte (victoire, force) | `#82CFA8` | `#1E1A25` | 9,30:1 | 4,5:1 | conforme |
| Encre sur vert (bouton Victoire actif) | `#0B2618` | `#82CFA8` | 8,77:1 | 4,5:1 | conforme |
| Corail en texte (menace, défaite) | `#F1847A` | `#1E1A25` | 6,77:1 | 4,5:1 | conforme |
| Corail sur surface haute | `#F1847A` | `#26202E` | 6,26:1 | 4,5:1 | conforme |
| Encre sur corail (bouton Défaite actif) | `#2B0D0A` | `#F1847A` | 7,15:1 | 4,5:1 | conforme |
| Connexion (texte) | `#79C6DA` | `#1E1A25` | 8,87:1 | 4,5:1 | conforme |
| Sécurité (texte) | `#C9A7E8` | `#1E1A25` | 8,28:1 | 4,5:1 | conforme |
| Hypothèse, incertitude (texte) | `#A3B4EC` | `#1E1A25` | 8,37:1 | 4,5:1 | conforme |
| Rôle Top (libellé, cadre) | `#E39B62` | `#17141D` | 7,91:1 | 4,5:1 | conforme |
| Rôle Jungle | `#9DBF6E` | `#17141D` | 8,76:1 | 4,5:1 | conforme |
| Rôle Mid | `#6FB0EC` | `#17141D` | 7,88:1 | 4,5:1 | conforme |
| Rôle ADC | `#EC8AAE` | `#17141D` | 7,64:1 | 4,5:1 | conforme |
| Rôle Support | `#B69AEE` | `#17141D` | 7,66:1 | 4,5:1 | conforme |
| Contour de champ, bouton secondaire | `#82778F` | `#17141D` | 4,32:1 | 3:1 | conforme |
| Contour sur surface | `#82778F` | `#1E1A25` | 4,05:1 | 3:1 | conforme |
| Contour sur surface haute (le plus faible) | `#82778F` | `#26202E` | 3,75:1 | 3:1 | conforme |
| Cadre or du conseil | `#E5B55A` | `#17141D` | 9,61:1 | 3:1 | conforme |
| Focus (ivoire) | `#F2ECE4` | `#17141D` | 15,50:1 | 3:1 | conforme |
| Cran de jauge rempli | `#B5ABA3` | `#1E1A25` | 7,58:1 | 3:1 | conforme |
| Cran de jauge vide (contour) | `#82778F` | `#1E1A25` | 4,05:1 | 3:1 | conforme |
| Filet de séparation | `#362F40` | `#17141D` | 1,42:1 | aucun | décoratif |

Non mesurables par calcul : textes posés sur l'illustration du Simulateur et du panneau (voile dégradé). À l'œil, lisibles sur les captures ; c'est le même voile qu'avant, teinté prune.

## Fonctions vérifiées présentes

Aucune commande ni aucun texte n'a été retiré. Changements du DOM : des ajouts (classes `is-fav`, `is-out`, `is-advice`, `home-blind`, `is-empty`, bloc d'échantillon, jauge, enveloppe `kpis-wrap`) et un seul remplacement : sous 10 parties, la barre de victoires d'un groupe devient une jauge de crans (le bilan écrit « 1V 2D » reste à côté). Vérifié par :
- **Captures** : chaque bouton des écrans capturés est présent avant et après (Simuler une draft, Activer la lecture du client, Réglages de connexion, Modifier mon pool, Réglages d'affichage, Rechercher une mise à jour, Mes statistiques, Gérer les données ; rôle joué et rôles, recherche, favori, exclusion, retrait, suggestions, export et import des profils ; Draft en cours / Simulation, Draft aléatoire, Nouvelle simulation, Reprendre, Quitter, Je joue, choix du classement, onglets Plan / Critères / Comparer / Conseils / Duel, Lane / Mid game / Teamfights ; onglets de Mes statistiques, Importer mon historique Riot, Voir tout le journal ; six rubriques des Réglages ; outils des panneaux, Comparer, Détails, Plan de jeu et ban).
- **Bancs de pages** (UX-1, 2, 4, 5, 8, i18n, profils, V2.15) : tous verts, voir ci-dessous.
- **e2e** dans la vraie WebView2 : mêmes résultats que la base (voir ci-dessous).
- **Moteur** : `run-engine-eval -AvecEvaluation`, 0 différence.
- CSP de `web/index.html` identique (la ligne n'est pas dans le diff) ; aucun script en ligne ajouté ; messages natifs, `IsAppPage`, lecture seule LCU non touchés (aucun fichier C# modifié).

## Tests

| Test | Résultat |
|---|---|
| `run-engine-eval -AvecEvaluation` | Banc OK. Développement : 312 comparés, 0 changement ; évaluation gelée : 108 comparés, 0 changement ; INATTENDUS 0. |
| `extract-ui-messages -Scenarios tests/moteur/scenarios-dev.json` | 812 drafts, 3168 étapes ; inventaire 634 entrées ; **0 gabarit hors inventaire** (nouvelle entrée UI-309 pour les textes d'échantillon). |
| `run-role-tests` | 85 réussis, 0 échec. |
| `run-coaching-tests -Work` | Conseils 162/0, égalités 18/0, maîtrise 91/0, moteur stratégique 190/0. |
| `run-v215-tests -Work` | 145 réussis, 0 échec. |
| Pages UX-1 / UX-2 / UX-4 / UX-5 / UX-8 | 28/0, 12/0, 25/0, 19/0, 92/0. |
| Pages i18n / profils | 229/0, 54/0. |
| `run-layout-tests -Work` | 51 passes, TOUT EST BON. |
| `run-layout-tests -Lang qps` | 140 débordements listés, **identiques à la base** (seul écart : un dépassement de la navigation passe de 3 à 2 px) ; 10 textes hors crochets (noms fictifs du banc, comme la base) ; 0 clé absente. |
| `check-catalogs` | 0 erreur (2 alertes déjà présentes dans la base, sur `mastery.*`). |
| `detect-hardcoded -Only web/ -Max 0` | 0 texte en dur. Exclusions : 53 (base : 51) ; les 2 nouvelles sont des noms de classes CSS (`is-empty`, `stats-few`), aucun texte visible. |
| `build.ps1 -Out <tmp>` | OK. |
| `--selftest` (bureau caché) | 284 OK, 0 échec, TOUT EST BON (deux passages). |
| `--e2e` × 2 (bureau caché) | 95 OK, 1 échec à chaque passage : « Raccourci Ctrl+Alt+D enregistré ». **Même résultat sur la base** (exe 5d859a8 : 95 OK, même échec) : le DRAFT ouvert sur ce PC tient déjà le raccourci. |
| `test-migration` (copie de `draft-essai-2162\data`) | Lu : rôle Mid, 3 champions, 96 parties d'historique, profil enregistré ; original non modifié. |

## Défauts restants (même avec des tests verts)

1. **Accueil en 1920 × 1080** : il reste environ un quart de hauteur vide sous le contenu. Les portraits passent à 64 px, mais la page n'a pas assez de contenu utile pour remplir l'écran, et le brief interdit de remplir pour remplir.
2. **Mon pool en 1920 × 1080** : la moitié basse reste vide (même limite qu'avant ; le tableau ne fait que trois lignes).
3. **Accueil : trois niveaux de titre** avant le premier contenu (« Accueil », phrase, bannière). Non retirés pour ne pas toucher la structure ni les textes.
4. **Répétition de textes** : la bannière et la colonne « Pendant la draft » disent toutes deux quand les panneaux s'ouvrent. Corriger demande de réécrire des textes (hors de cette passe visuelle).
5. **Cases et boutons radio non cochés** : rendu natif de Chromium en thème sombre (carré gris foncé), contour peu marqué. Déjà le cas avant ; non mesuré par le calcul ci-dessus.
6. **Focus au démarrage** : le lien « Accueil » de la navigation a parfois le focus visible dès l'ouverture (déjà le cas avant). En ivoire, il se voit davantage.
7. **Barre de victoires au-dessus du seuil** : toujours verte, même sous 50 %. Le repère à 50 % et le pourcentage écrit corrigent la lecture, mais une barre corail sous 50 % serait plus directe (et risquerait de suggérer une conclusion).
8. **« Leurs failles » en vert** (profil adverse) : une faiblesse adverse est une bonne nouvelle, d'où le vert ; mais le brief associe le corail aux faiblesses. À trancher.
9. **Rôle Top (orange) et or du conseil** sont voisins. Ils ne sont jamais au même endroit (cadre de portrait contre étiquette), mais un portrait Top encadré peut rappeler la tuile conseillée.
10. **« Aucun »** dans le bandeau des statistiques s'affiche en Sitka 40 px comme un chiffre.
11. **Onglet Historique Riot** : les anciens tableaux (`StatsTab`) affichent toujours des pourcentages par groupe, sans période propre ni jauge ; seul le seuil des barres s'y applique.
12. **Panneau compact** : le bouton de retrait « × » couvre le coin du portrait (déjà le cas avant) ; le cadre de rôle le rend plus visible.
13. **Pseudo-langue** : 140 débordements listés, comme la base. Ils concernent les futures langues et ne sont pas traités ici.
14. **Sitka non vérifiée hors de ce PC** : sur une installation sans Sitka, repli Georgia (non vu en capture). Segoe UI Variable n'est pas utilisée.
15. **Textes sur l'illustration** : contraste non calculable (voile dégradé), jugé à l'œil seulement.
