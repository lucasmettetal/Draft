# Panneaux primaires : inventaire et propositions

Étude, sans implémentation au-delà du correctif de la 3.0.0-beta.1. Demande de l'utilisateur : « les panneaux sont primaires et la grande app est secondaire. Je devrais avoir toutes les infos sur les panneaux. »

Ce document inventorie ce que la fenêtre principale et le Simulateur offrent **pendant une draft**. Pour chaque élément : où il est disponible, son importance en draft, et comment l'amener dans les panneaux.

**Panneaux mesurés.**
- Gauche (G) : adversaires.
- Droit (D) : conseil.
- Compact (C) : adversaires et conseil dans un seul panneau.

Tailles réelles, calculées par le placement du programme (`Layout.Compute`, client LoL centré), en pixels CSS :

| Écran | Client LoL | Panneaux |
|---|---|---|
| 1366 × 768 à 125 % | 1024 × 576 | Pas de place pour deux : panneau unique étroit, **230 × 520** (zone utile **222 × 511**, mesurée dans la WebView2 de l'e2e) |
| 1920 × 1080 à 125 % | 1280 × 720 | Deux panneaux de **256 × 576** ; compact choisi : 256 × 576 |
| 1920 × 1080 à 100 % | 1280 × 720 | Deux panneaux de **320 × 720** ; compact choisi : 320 × 720 |

Mesures dans Chrome, sur la branche `fix/beta1-alternative-role`, à ces tailles.

## Ce que le correctif beta.1 apporte déjà aux panneaux

- **Choix :** consulter n'importe quel champion du pool (alternative, liste repliée « Choisir dans ton pool »), carte « Consulté » avec la comparaison au conseil, confirmation par « Je joue X ».
- **Rôle :** état du rôle (détecté, à la main, inconnu), notice de bascule avec « Revenir en … », proposition encadrée.
- **Menace :** juste après le conseil, dans le panneau droit comme dans le compact.

## Inventaire

Importance en draft : **I** = indispensable, **U** = utile, **S** = secondaire.

| Élément (fenêtre ou Simulateur) | G | D | C | Imp. | Proposition pour les panneaux (emplacement, taille, coût, risque) |
|---|---|---|---|---|---|
| Saisie des adversaires (recherche, ajout, retrait, Annuler) | oui | non | oui | I | Déjà en G et C. Rien à faire |
| Rôles adverses supposés, correction d'un clic | oui | non | oui | I | Déjà en G et C |
| Vis-à-vis et certitude (« Mid d'en face (probable) ») | oui | oui | oui | I | Déjà présent |
| Rôle joué, changement, état (détecté, à la main, inconnu) | non | oui | oui | I | Fait dans ce correctif |
| Conseil (nom, net ou serré, n/5, illustration) | non | oui | oui | I | Déjà présent |
| « Je joue X », « X noté », « Revenir au conseil » | non | oui | oui | I | Fait. Barre d'actions collée en bas, toujours visible |
| Menace principale | non | oui | oui | I | Fait : juste après le conseil |
| Alternative | non | oui | oui | I | Fait : se consulte d'un clic, « Comparer » à côté |
| Égalité | non | oui | oui | I | Déjà présent ; chaque ex æquo se consulte puis se confirme |
| Champion consulté et comparaison résumée | non | oui | oui | I | Fait. Matchup, raisons et risque repliés |
| Raisons du conseil (2) | non | oui | oui | U | Présentes, juste au-dessus de la barre d'actions (droit 256 × 576 : 424..479, barre à 482) |
| Risque principal du conseil (« Risque : … ») | non | non | non | U | À ajouter sous les raisons : 1 ligne (≈ 18 px). Coût faible. Risque : allonger le défilement |
| Changement de conseil (« X passe devant Y ») | non | oui | oui | U | Déjà présent |
| Classement complet du pool (mots de rang) | non | oui (replié) | oui (replié) | U | Fait : liste repliée. Les barres de force et le bilan personnel restent dans la fenêtre : + 8 px par ligne si on les ajoutait. Coût faible |
| Ban conseillé et sa raison | non | oui (replié) | oui (replié) | U | Dans « Plan et ban ». Ouvert d'office tant qu'aucun adversaire n'est connu |
| Condition de victoire | non | oui (replié) | oui (replié) | U | Dans « Plan et ban » ; suit le champion consulté |
| Cibles prioritaires | non | non | non | U | À ajouter dans « Plan et ban » : une rangée de portraits (≈ 34 px). Coût faible |
| Plan par phase (lane, milieu de partie, combats) | non | non | non | U | Vue étendue (Détails) plutôt que le panneau : 3 listes de 2 à 4 conseils (≈ 150 à 250 px) |
| Conseil pédagogique essentiel | non | oui | oui | U | Une ligne et « Voir les conseils » (fenêtre) |
| Liste complète des conseils (vérifiés, à confirmer) | non | non | non | S | Rester dans Détails : 6 à 12 conseils par draft |
| Le plus joué dans le rôle | non | oui | oui | S | Déjà présent |
| Profil de la composition adverse (jauges) et forces ou failles | oui | non | non | U | Dans le compact : une ligne « Forces : … · Failles : … » (≈ 36 px) dans un repli. Coût moyen (place à 125 %) |
| Comparaison détaillée (barres par critère, « passerait devant ») | non | non | non | S | Détails ; le résumé est déjà dans la carte « Consulté » |
| Critères (matrice) et hypothèses sur le vis-à-vis | non | non | non | S | Détails |
| Duel (tableau de matchups, recherche) | non | non | non | S | Détails |
| « Plus de détails » (picks à éviter, bilan contre le mid, sans frontline) | non | non | non | S | À ajouter au repli « Plan et ban » : 1 à 3 lignes. Coût faible |
| Résultat (Victoire, Défaite, Ignorer) et dernière partie en attente | non | oui | oui | U | Déjà présent après une confirmation |
| Source des adversaires (lecture du client, saisie) | non | oui | oui | S | Déjà en D et C ; masquée sous 240 px de large (place) |
| Pool, exclusions, favori (édition) | non | non | non | S | Mon pool, hors draft ; « Ouvrir Mon pool » si le pool est vide |
| Simulation, draft aléatoire, règles | non | non | non | S | Fenêtre seulement : pas utile pendant une vraie draft |

## Mesures

Pool de 4 champions, 5 adversaires, alternative consultée. Positions de haut en bas, en pixels CSS. La barre d'actions (« Je joue X », Détails, « Revenir au conseil ») est collée en bas.

| Panneau | Taille | Draft complète | + consultation | + liste du pool ouverte | Positions pendant la consultation |
|---|---|---|---|---|---|
| Compact, 1366 à 125 % | 222 × 511 | 805 | 1 075 | 1 255 | Rôle 35..72, conseil 229..297, menace 302..366, carte « Consulté » 379..570 (tête 388..438), barre 426..510 |
| Droit, 1920 à 125 % | 256 × 576 | 734 | 970 | 1 159 | Conseil 110..182, menace 189..257, consulté 272..448, barre 472..575 |
| Compact, 1920 à 125 % | 256 × 576 | 905 | 1 141 | 1 322 | Conseil 281..353, menace 360..428, carte « Consulté » à partir de 443, barre 472..575 |
| Droit, 1920 à 100 % | 320 × 720 | 720 | 962 | 1 151 | Conseil 120..220, menace 228..295, consulté 311..469, barre 634..719 |
| Compact, 1920 à 100 % | 320 × 720 | 890 | 1 137 | 1 318 | Conseil 295..396, menace 404..470, consulté 486..644, barre 634..719 |

Dans les panneaux, le matchup, les raisons et le risque du champion consulté sont repliés (« Matchup, raisons et risque ») ; la comparaison avec le conseil reste visible.

**Lecture.**
- Rôle, conseil, menace et barre d'actions tiennent sans défiler à toutes ces tailles.
- À 222 × 511, avant ce correctif, la menace (350..414) passait sous la barre : 407 en français, 369 en anglais, où « I'm playing Vex » et « Details » prenaient deux lignes. Avec la lecture du client désactivée, la ligne d'état du rôle (2 lignes) la poussait à 390..455. Ce qui a été resserré :
  - sous 540 px de haut : illustration du conseil 68 px, écarts 5 px ;
  - la barre ne garde que « Revenir au conseil », le conseil étant affiché juste au-dessus ;
  - la recherche, désactivée quand les 5 adversaires sont saisis, est masquée (42 px) ;
  - sous 240 px de large : « Détails » passe en icône, « Je joue X » passe sur deux lignes dans le bouton, les cases vides n'affichent plus « Ajouter » (qui débordait sur la voisine), et la source des adversaires (« Lecture du client ») est masquée : avec « Choix net », elle passait sur une seconde ligne (+30 px dans la vraie WebView2).
- Champion consulté : la carte entière est visible dans le panneau droit et dans le compact à 100 %. Dans le compact à 125 %, seule sa tête (portrait, nom) dépasse au-dessus de la barre ; le nom reste dans « Je joue Vex », et la carte suit en défilant.
- La liste du pool ouverte ajoute environ 180 à 190 px pour 4 champions (45 à 47 px par ligne).
- Fenêtre : la notice, la proposition et l'état du rôle sont sur une bande sous l'en-tête, qui garde ses 52 px.

## Proposition de tailles

- **Pour la beta.1 : aucun agrandissement.** Le banc de mise en page vérifie, sans défiler et au-dessus de la barre d'actions, les rôles, l'état du rôle, la notice et son retour, la proposition et la menace ; « Revenir au conseil » dans la barre. Toutes les tailles réelles sont couvertes (hauteur 520 px et plus, dont 230 × 520). L'e2e refait cette vérification dans les vrais panneaux WebView2, aux tailles du tableau. La taille d'essai extrême 320 × 420 défile.
- **Pour une version suivante (3.1), si l'on ajoute risque, cibles et forces ou failles** (≈ 90 px) :
  - panneau droit, 1920 à 125 % : 576 → 700 px CSS de haut, soit 875 px physiques (place disponible : 1 030 px physiques de haut, barre des tâches déduite) ;
  - compact : sans changement de hauteur. Les ajouts vont dans les replis.
  - Gain mesuré : à 700 px de haut, la barre commence vers 607 ; raisons (424..479) et liste du pool (389..417) restent au-dessus, en draft complète.
  - Risque : le panneau dépasse la hauteur du client (720) ; le placement le centre sur le client et le garde sur l'écran (`--selftest`, 5 résolutions et 4 échelles).
- **Ce qui ne doit pas entrer dans les panneaux :** plan par phase, liste complète des conseils, matrice des critères, duel. Ils restent dans Détails, à un clic de « Détails » ou de « Voir les conseils ».
