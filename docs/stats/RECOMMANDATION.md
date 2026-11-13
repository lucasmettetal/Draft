# Mes statistiques : recommandation

Étude, à valider sur les captures avant toute implémentation. Base : `main` 6218022.
Documents liés : [`AUDIT-HISTORIQUE-ET-JOURNAL.md`](AUDIT-HISTORIQUE-ET-JOURNAL.md), [`COACHING-ARCHITECTURE.md`](COACHING-ARCHITECTURE.md), [`CLES-PROPOSEES.md`](CLES-PROPOSEES.md).
Maquettes : [`maquettes/index.html`](maquettes/index.html). Captures : [`captures/`](captures/).

## 1. En bref

- **Recommandation : A (tableau de bord), avec trois éléments repris de B.**
- A garde le journal DRAFT visible dès l'ouverture, à côté de l'historique Riot, et jamais additionné avec lui.
- B est plus pédagogique, mais il enterre le journal tout en bas (chapitre 5) et ne montre qu'un chapitre au premier écran à 1366.
- Repris de B : la colonne « Pas encore » avec sa jauge vers le seuil, la phrase d'état du journal (« Aucun résultat noté : DRAFT ne peut encore rien mesurer. »), et la frise des parties, en option.

## 2. Ce que les deux propositions corrigent

Constats de l'audit, visibles sur [`captures/planches/planche-riot93-1366-pleine-page.png`](captures/planches/planche-riot93-1366-pleine-page.png) :

| Page actuelle (main) | A et B |
|---|---|
| « Drafts notées 5 », « Victoires 0 », « Conseil suivi 0 sur 5 » et « Historique Riot 93 » dans le même bandeau. | Deux blocs séparés, chacun avec sa source écrite. La phrase « Ces deux sources ne s'additionnent pas » est dans la page. |
| « Conseil suivi » compté sur toutes les drafts dans le bandeau, sur les parties terminées dans l'onglet Journal. | Une seule définition : « Conseil joué », sur les parties terminées. Sans partie terminée : un tiret, pas « 0 sur 5 ». |
| L'historique de l'application ne montre que le total, le top 5 du rôle actif et une table par rôle. | KDA, durée, patchs, tranches, champions et vis-à-vis mid, avec l'échantillon (période, parties, patchs, comptes). |
| La dernière tranche de 20 parties disparaît (93 → 80 montrées). | La tranche en cours est montrée, marquée « En cours ». |
| Le résultat ne se saisit que dans le plan de jeu ou le panneau, pour la dernière draft en attente ; jamais dans Mes statistiques. | Boutons « Victoire » et « Défaite » sur chaque draft en attente. |
| Les simulations écrites avant le correctif comptent comme des drafts, sans marque. | Après `fix/choix-champion`, le Simulateur n'écrit plus rien. Pour les journaux existants, une ligne le dit (« certaines peuvent être des simulations ») ; pour les nouveaux : « Drafts réelles seulement ». |
| Aucune conclusion, mais aucune explication de ce qui manque. | « Ce qu'on peut conclure » et « Pas encore », chaque ligne avec sa preuve ou ce qui manque. |

Gardé de main : le seuil de 10 parties (aucun pourcentage dessous), la jauge de crans, la barre verte avec repère à 50 % (neutre sous 50 %), la ligne d'échantillon.

## 3. Les deux propositions

### A. Tableau de bord ([captures/a/](captures/a/))

1. Deux colonnes, une par source : **Historique Riot** à gauche (large), **Journal DRAFT** à droite (boîte étroite).
2. Bandeau héros Riot dans la grammaire actuelle : parties, victoires (% à partir de 10), KDA moyen, durée ; puis la ligne d'échantillon.
3. Cartes de champions (portrait 52 px cadré du rôle, parties en Sitka, barre ou jauge, KDA), badge seulement si une conclusion est établie.
4. Graphiques utiles : par poste, par tranches de 20, par patch (victoires et défaites), puis vis-à-vis mid et « Ce qu'on peut conclure ».
5. Journal : drafts notées, parties terminées, conseil joué ; dernières drafts avec le conseil (cadre or) et le pick joué ; comparaison seulement à 10 parties par groupe.

### B. Récit ([captures/b/](captures/b/))

1. Une colonne de lecture en chapitres numérotés, avec sommaire (puces à 1366, colonne fixe à droite à 1920).
2. « Ton échantillon » : phrase avec le chiffre héros, frise des parties (victoires au-dessus, défaites au-dessous), répartition par poste, faits clés.
3. « Tes champions » : une ligne par champion, volume, barre ou jauge, KDA ; « Tes vis-à-vis » en mid.
4. « Ce qu'on peut dire » : deux colonnes, établi et pas encore, avec jauge de progression vers le seuil.
5. « Tes drafts DRAFT » : chapitre à part sur fond distinct, phrase d'état, liste des drafts, comparaison du conseil.

## 4. Comparaison

Mesures sur les maquettes, fenêtre de 1366 de large (positions en pixels CSS depuis le haut de la zone de contenu ; à 1366 × 768, 738 px sont visibles sous le bandeau). Plus petit texte : 12 px dans les deux.

| Critère | A. Tableau de bord | B. Récit |
|---|---|---|
| Premier écran à 1366 (93 parties) | Bandeau héros, échantillon, 1re rangée de champions (dès 423 px) ; à droite, chiffres du journal et première draft. | Chapitre 1 (phrase, frise, postes, faits), puis le titre « Tes champions » (549 px) ; leurs lignes sont sous le pli. |
| Journal visible sans défiler | **Oui** : la colonne commence à 126 px, la première draft à 457 px (zone visible : 738 px). | **Non** : le chapitre commence vers 1 681 px (93 parties), 2 372 px (400 parties). |
| Hauteur de page à 1366 (93 / 400 parties) | 2 047 / 2 526 px | 2 359 / 3 181 px |
| Densité | Forte : tout est à un coup d'œil, au prix d'une colonne journal étroite (≈ 390 px à 1366). | Faible à moyenne : aéré, phrases complètes. |
| Lisibilité à 1366 | Bonne ; dans la colonne étroite, chaque draft prend trois lignes courtes. Texte de 12 px au minimum. | Très bonne ; lignes longues, mais beaucoup de défilement. Texte de 12 px au minimum. |
| Fidélité à B+ | Forte : réutilise le bandeau de chiffres, les barres, les jauges, les filets et les boîtes existants. | Bonne, mais ajoute des composants neufs : numéros de chapitre, frise, sommaire. |
| Cohérence avec le Simulateur | Même hiérarchie : un héros, puis des panneaux. | Hiérarchie de document, différente du reste de l'application. |
| Coût d'implémentation | Moyen : surtout du HTML et du CSS sur des motifs existants, trois petits graphiques. | Moyen : moins de graphiques, mais une frise SVG, un sommaire collant et plus de phrases plurielles (clés `stats.story.*`). |
| Risque de lecture trompeuse | Moyen : un tableau de bord invite à comparer des chiffres voisins. Atténué par les colonnes, les sources écrites, l'absence de % sous 10 et la neutralité des barres sous 50 %. Les graphiques par patch et par tranche peuvent faire voir du hasard : une note le dit sous chacun. | Faible : chaque chiffre est dans une phrase qui porte sa limite. La frise peut faire voir des « séries » ; aucune conclusion n'y est attachée. |
| Situation de référence (93 parties, 5 drafts sans résultat) | Les 93 parties remplissent la page ; les 5 drafts en attente et leurs boutons sont visibles à droite. | Les 93 parties racontent l'échantillon ; les 5 drafts arrivent après 1 915 px de lecture. |
| État vide | Deux colonnes vides côte à côte, chacune avec son action. | Une page d'accueil en deux étapes, plus claire. |

## 5. Recommandation argumentée

**A**, pour trois raisons :

1. **Le journal est la donnée propre à DRAFT.** Il doit rester visible sans défiler, surtout tant qu'il attend des résultats. B le place en dernier.
2. **Cohérence.** A reprend la hiérarchie et les composants de B+ déjà en place (bandeau de chiffres, barres, jauges). Moins de nouveaux motifs, moins de risque visuel, moins de code.
3. **Lecture rapide entre deux parties.** On ouvre la page entre deux files : un coup d'œil doit suffire.

À reprendre de B dans A :

- la présentation « Pas encore » avec jauge de progression (`22 / 30`), plus parlante qu'une phrase seule ;
- l'état vide en deux étapes (Historique Riot, Journal DRAFT) ;
- la frise des parties dans le bandeau d'échantillon, en option, si elle passe la revue (elle montre bien les semaines sans jeu).

Garder les onglets actuels : « Vue d'ensemble » devient le tableau de bord A ; « Journal des drafts » garde la table complète, l'import et l'export ; « Historique Riot » garde l'import et l'ancien format. La refonte touche donc une seule vue.

Point d'attention de A : la colonne du journal fait environ 390 px à 1366. La maquette range chaque draft sur trois lignes nettes (qui contre qui ; date et étiquettes ; « Noter le résultat » et ses deux boutons). À garder tel quel à l'implémentation.

## 6. Découpage en PR (après validation seulement)

Chaque PR est petite, testable seule, sans changement du moteur, de la CSP, de la LCU ni de la mise à jour.

| PR | Contenu | Tests |
|---|---|---|
| S-1 | Une seule définition de « Conseil joué » (parties terminées) dans le bandeau actuel. | Banc `draft-ux5-tests.html` : 5 drafts sans résultat → tiret, jamais « 0 sur 5 ». |
| S-2 | Module pur `draft-stats-calc.js` (bilan, KDA, durée, postes, patchs, tranches avec la dernière, vis-à-vis mid, journal). Une seule fonction de Wilson, anciennes fonctions gardées en alias. | Page de tests avec les historiques fictifs (93 et 400 parties) : totaux exacts, durées en ms écartées. |
| S-3 | Module `draft-stats-findings.js` (règles déclarées, statuts, preuves). | « Aucune conclusion sous le seuil » ; 1 000 historiques simulés à 50 % : taux de faux « établi » borné ; statuts du § 3 de l'architecture. |
| S-4 | Clés FR et EN de la proposition retenue (sous-ensemble de `CLES-PROPOSEES.md`). | `tools/i18n/check-catalogs.ps1` : format, variables, aucune collision. |
| S-5 | Vue d'ensemble en deux colonnes : bandeau héros Riot et échantillon, colonne Journal. | `run-layout-tests` à 1366, 1920 et 125 % ; captures avant et après. |
| S-6 | Cartes de champions, panneaux par poste, tranches, patchs, vis-à-vis mid. | Pas de % sous 10 (test DOM) ; aucun débordement horizontal. |
| S-7 | « Ce qu'on peut conclure » et « Pas encore » (côté Riot), comparaison du conseil (côté journal). | Chaque conclusion affichée contient parties, intervalle et référence. |
| S-8 | Résultat saisissable sur chaque draft en attente ; ligne d'origine inconnue pour les entrées d'avant le correctif. Demande une marque sur les nouvelles entrées (`app` ou date de mise à jour), à ajouter avec ou après `fix/choix-champion`. | Banc journal : saisie, export, import ; une entrée sans marque est comptée « origine inconnue », jamais retirée. |

Plus tard, sur décision séparée (audit § 10) : rôle et `matchId` dans le journal, `enemyPos` à l'import, correction des durées, rapprochement journal → partie Riot.

## 7. Données fictives

Générées par [`maquettes/donnees-fictives.js`](maquettes/donnees-fictives.js), déterministes (graine fixe). Aucune donnée réelle. Mêmes données pour A, B et la référence avant.

| État | Historique Riot | Journal DRAFT |
|---|---|---|
| Aucun historique | — | — |
| 5 drafts sans résultat | — | 5 drafts d'avant le correctif (sans marque de version), 0 terminée, aucune où le pick joué est le conseil |
| 93 parties et peu de drafts | 93 parties, 1 compte, 25/03/2026 au 08/11/2026, 6 patchs ; 71 mid, 17 jungle, 4 top, 1 support ; 17 champions (4 à 10 parties ou plus, 6 à 5 ou plus) ; 42 V ; 32 min en moyenne ; 39 adversaires mid, 7 vus au moins 3 fois | les mêmes 5 drafts |
| Historique riche | 400 parties, 29 patchs ; 16 champions ; 208 V ; duels imposés (LeBlanc 1 V sur 12) ; tendance en hausse en mid | 64 drafts écrites après le correctif (marque `app` proposée) : 60 terminées (32 V), 44 conseils joués (25 V), 2 ignorées, 2 en attente |

## 8. Captures

Toutes les images portent un bandeau « Maquette : données fictives / Mockup: fictitious data » (maquettes) ou « Données fictives / Fictitious data » (référence avant).

- Maquettes : `captures/a/` et `captures/b/`, quatre états en français et deux en anglais, à 1366 × 768, 1920 × 1080, 1920 × 1080 à 125 % (échelle 1,25 : page de 1536 × 864) et en pleine page à 1366. Chrome sans tête, réseau limité à Data Dragon.
- Référence avant : `captures/avant/`, la page actuelle de main rendue par l'application (WebView2) sur un bureau caché, mêmes états et tailles, plus les onglets Historique Riot et Journal.
- Planches : `captures/planches/`, avant, A et B côte à côte, par état et par taille ; tailles de A et B ; anglais.

Les plus parlantes :

1. [`planche-riot93-1366x768.png`](captures/planches/planche-riot93-1366x768.png) : la situation de référence au premier écran.
2. [`planche-riot93-1366-pleine-page.png`](captures/planches/planche-riot93-1366-pleine-page.png) : toute la page, avant, A et B.
3. [`planche-riche-1366-pleine-page.png`](captures/planches/planche-riche-1366-pleine-page.png) : les conclusions établies, avec leur preuve.
4. [`planche-drafts5-1366x768.png`](captures/planches/planche-drafts5-1366x768.png) : 5 drafts sans résultat, sans historique.
5. [`planche-en-riot93-pleine-page.png`](captures/planches/planche-en-riot93-pleine-page.png) : l'anglais.

## 9. À trancher par l'utilisateur

1. A, B, ou A avec les éléments de B (recommandé) ?
2. Garder les trois onglets, ou une seule page ?
3. Saisie du résultat depuis Mes statistiques pour les drafts anciennes : oui ou non ?
4. Frise des parties dans A : oui ou non ?
5. Origine des anciennes entrées du journal : afficher « origine inconnue » grâce à une marque de version (proposé), ou ne rien dire ?
6. Égalité : choisir un ex æquo compte comme « conseil joué » (règle de `fix/choix-champion`, à valider). Garder une marque `tied` pour pouvoir changer d'avis plus tard ?
