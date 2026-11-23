# Panneaux primaires : proposition

**Statut :** proposition à valider. Branche `feat/panneaux-primaires`, partie de `main` 4fb621f (3.0.0-beta.1 publiée). Ni PR, ni fusion, ni intégration à la beta.2.

**Mise à jour :** l'utilisateur a retenu **V2**. Ce qui a changé depuis : section 8, « Ajustements V2 ».

**Décision produit :** les panneaux sont l'interface principale. Le joueur choisit son champion, comprend les conseils et lit l'essentiel sans ouvrir la grande fenêtre.

**Direction retenue : V1.** Les tailles du placement ne changent pas (`Layout` inchangé). Le contenu des panneaux est réorganisé : en haut, la lecture immédiate, visible sans défiler ; dessous, la lecture approfondie, en sections dépliables, dans le panneau lui-même. V2 (panneaux agrandis) et V3 (vue « Détails » élargie) restent des options de test, mesurées mais non activées.

Documents repris : [PANNEAUX-PRIMAIRES.md](PANNEAUX-PRIMAIRES.md) (inventaire) et [PROPOSITION-TAILLES-PANNEAUX.md](PROPOSITION-TAILLES-PANNEAUX.md) (tailles, 272 px, risques de recouvrement). Le chiffre de 272 px n'a pas servi de contrainte.

## 1. Ce qui change dans les panneaux

### Panneau droit : l'assistant de décision

Lecture immédiate, de haut en bas :
- rôle joué et pool actif sur une ligne : « Rôle détecté · pool de 4 ». La raison complète (lecture désactivée, client fermé…) reste au survol et dans la note de provenance ;
- carte du conseil (or) : étiquette « Conseil », état (« Choix net », « Choix serré »), adversaires connus (« 3/5 adversaires »), nom ;
- « Pourquoi : » (vert) et « Risque : » (corail), une phrase chacun ;
- menace principale ; avant tout adversaire, le ban conseillé à sa place ;
- champion consulté, avec sa comparaison au conseil, s'il diffère du conseil ;
- alternative : un clic la consulte ; « Comparer » ouvre la comparaison dans le panneau ;
- barre d'actions collée en bas : « Je joue X », marqué « À la main », « Détails », « Revenir au conseil ».

Lecture approfondie (« Détails » y mène, « Conseil » ramène en haut, défilement instantané) :
- **Ton pool** : tout le pool classé, mot de rang et barre de force, un clic consulte ;
- **Pourquoi X** (X = champion consulté, sinon choisi, sinon le conseil) : position dans le classement, « Pourquoi DRAFT préfère Y », avantages, inconvénients, matchup, incertitude sur le vis-à-vis, picks à éviter ;
- **Plan de jeu avec X** : condition de victoire, conseil essentiel, lane, milieu de partie, combats ;
- **Menaces et cibles** ;
- **Bans conseillés**, avec leur raison ;
- **Comparer X et Y** : barres par critère ;
- **Critères du classement** pour X (+ et −, avec le texte du critère) ;
- **Profil adverse** (compact seulement : forces, failles, jauges) ;
- **D'où viennent ces informations** : source des adversaires, état du rôle, règle du pick ;
- « Ouvrir le Simulateur » en dernier recours.

Un seul niveau de sections, pas d'onglets ni de menus imbriqués, aucune animation longue. Le contenu d'une section n'est calculé qu'une fois ouverte. Consulter un champion (alternative, liste du pool, autre fenêtre) ramène en haut du panneau : sa carte et « Je joue X » se lisent sans défiler.

### Compact

Ordre : rôle et pool, adversaires (5 postes, recherche masquée quand les 5 sont saisis), conseil, information stratégique principale (menace, ou ban conseillé avant tout adversaire), nom du champion consulté, barre (« Je joue X » ou « Dans League »), puis la lecture approfondie.

Jusqu'à 700 px de haut, « Pourquoi », la comparaison du consulté et le changement de conseil passent dans les détails. Sous 600 px, la menace tient sur une ligne (texte complet au survol et dans « Menaces et cibles ») et les ex æquo se rangent côte à côte.

### Panneau gauche : la composition adverse

- les 5 postes ; rôle **supposé** en pointillé, rôle **placé à la main** en trait plein, case vide = pas encore choisi ; légende « Pointillé : rôle supposé. » ;
- provenance : « Lecture du client » ou « Saisie à la main » ;
- forces et failles juste après les postes, puis les jauges ;
- saisie rapide inchangée (Entrée, Tab, retour arrière, Échap) ;
- après chaque ajout ou retrait, l'annonce rappelle le conseil à jour : « Ajout de Vi · Conseil : Twisted Fate ». Aucune autre recommandation à gauche.

### « Dans League » (compatibilité avec les PR #39 et #40)

Aucun code des PR #39 et #40 n'est repris. Un emplacement est prêt dans la barre d'actions :
- **draft connectée** : « Dans League : Fizz (survolé) » ou « Dans League : Twisted Fate verrouillé ✓ », en bleu « connexion », à part du champion consulté ; dessous, « Noté tout seul au lancement. », sans bouton « Je joue » ;
- **mode manuel ou lecture indisponible** : « Je joue X », marqué « À la main » (au survol : « Rien n'est lu dans League : « Je joue » note ton pick à la main. »).

Sur cette branche, l'emplacement est alimenté seulement par une **donnée de test** : `leagueSlot = { champion, state: "hover" | "locked" }`, `null` dans l'application. Branchement prévu à l'intégration, une ligne : `leagueSlot` déduit de l'état « Dans League » envoyé par le programme dans la PR #40 (survolé → `hover` ; verrouillé ou final → `locked` ; lecture perdue ou mode non pris en charge → `null`, et « Je joue » revient).

## 2. Informations accessibles depuis les panneaux

| Information | Avant (4fb621f) | Après |
|---|---|---|
| Rôle et pool actifs | rôle ; pool dans une liste repliée | rôle et « pool de N » sur une ligne |
| Conseil et pourquoi | nom ; raisons après l'alternative, sous le pli pendant une consultation | nom, état, « Pourquoi », « Risque » sans défiler |
| Principal risque | menace seulement | menace et risque du conseil |
| Champion consulté | carte ; nom caché sous la barre à 222 x 511 | nom visible à 222 x 511 |
| Autres champions du pool | liste repliée | « Ton pool » : rang, barre de force |
| Avantages et inconvénients du consulté | « Matchup, raisons et risque » (2 raisons, 1 risque) | toutes les raisons + et − |
| Pourquoi DRAFT en préfère un autre | résumé d'une phrase | « Pourquoi DRAFT préfère Y » et comparaison par critère |
| Plan de jeu du champion consulté | condition de victoire (repliée) | condition, lane, milieu, combats |
| Menaces | une | toutes (2 au plus), avec leur effet |
| Cibles prioritaires | non | oui |
| Bans conseillés | le 1er et « ou … » | ban clé au début ; section avec raisons |
| Matchups et comparaison | grande fenêtre | dans le panneau (barres par critère) |
| Critères du classement | grande fenêtre | dans le panneau, pour le champion lu |
| Profil adverse dans le compact | non | section « Profil adverse » |
| Rôle supposé ou placé (gauche) | mid seulement | les 5 postes |
| Emplacement « Dans League » | non | oui (donnée de test) |

### Ce qui manque dans les données (rien n'est inventé)

| Information | État | D'où elle pourrait venir |
|---|---|---|
| Champion survolé ou verrouillé dans League | pas lu sur cette branche | PR #40 (état « Dans League »), emplacement prêt |
| Bans réellement faits pendant la draft | pas lus : un ban conseillé peut déjà être banni | actions « ban » de la session de sélection, même adresse LCU, lecture seule ; à étudier |
| Picks alliés (synergies, vraie frontline alliée) | pas lus ; frontline alliée supposée (réglage) | équipe alliée de la session ; décision de confidentialité à prendre |
| Rôles adverses réels | non fournis en classée ; rôles supposés | aucune source fiable connue |
| Survol adverse | pas lu | intention de pick adverse de la session, à vérifier |
| Minuteur de la phase | pas lu sur cette branche | `timer.phase` (lu par la PR #40) |
| Bilans de matchup mesurés | seulement avec un import Riot, ou le tableau écrit du mid | import existant |
| Bilan personnel | seulement avec l'historique importé | import existant |
| Runes, objets, sorts | absents | hors périmètre |

Les bans conseillés, eux, existent : ils sont calculés (bilans mesurés, tableau écrit, ou profils). Le moteur n'a pas été touché.

## 3. Variantes V1, V2, V3

- **V1** : tailles actuelles (`Layout.Compute` et `Narrowest` de l'exe), contenu réorganisé. **Implémentée.**
- **V2** : panneau étroit de 256 px au lieu de 230 ; hauteur = écran utile pour l'étroit, 1,25 x client pour les panneaux à côté du client. Option de test (taille imposée), non activée.
- **V3** : V1, plus une vue « Détails » élargie à 360 px pendant la lecture approfondie (lecture immédiate masquée, sections ouvertes). Option de test (style injecté), non activée.

Tailles : `tools/layout-table.ps1 -Exe <exe> -Variantes` (Windows PowerShell 5.1). Zone utile = panneau moins 4 (gauche, droit) ou 8 (unique, étroit) en largeur, moins 9 en hauteur, comme mesuré dans la WebView2 de l'e2e. « Couvre » = largeur du client masquée, en pixels CSS et en part de sa largeur ; client LoL centré, à la taille choisie dans LoL (1024 x 576 à 1366, 1280 x 720 à 1920).

Mesures : `tools/run-panneaux-tests.ps1` (Chrome sans interface, `web/draft-panneaux-tests.html`, 9 scénarios fictifs pour la lecture immédiate, sections toutes ouvertes pour la lecture approfondie). « Essentiel » = éléments de la lecture immédiate entiers et au-dessus de la barre d'actions, sans défiler. « Fenêtre » = hauteur visible de la lecture approfondie après « Détails » ; « part » = fenêtre / hauteur totale, sections ouvertes, champion consulté.

| Écran (client) | Variante | Panneau (zone utile) | Couvre le client | Essentiel sans défiler | Approfondie : fenêtre / total (part) | « Je joue Renata Glasc » (EN) |
|---|---|---|---|---|---|---|
| 1366 x 768, 125 % (1024 x 576) | **V1** | unique étroit 230 x 520 (222 x 511) | **93 px, 11 %** | **67/67** | 342 / 2 625 (13 %) | 2 lignes |
| | V2 | 256 x 574 (248 x 565) | 119 px, 15 % | 67/67 | 395 / 2 464 (16 %) | 2 lignes |
| | V3 | 360 x 520 pendant « Détails » | 223 px, 27 % | comme V1 | 342 / 2 133 (16 %) | — |
| 1366 x 768, 100 % | V1 | 230 x 576 (222 x 567) | 59 px, 6 % | 67/67 | 386 / 2 665 (14 %) | 2 lignes |
| | V2 | 256 x 728 (248 x 719) | 85 px, 8 % | 67/67 | 535 / 2 474 (22 %) | 2 lignes |
| | V3 | 360 x 576 | 189 px, 18 % | comme V1 | 396 / 2 143 (18 %) | — |
| 1366 x 768, 150 % | V1 | 230 x 472 (222 x 463) | 116 px, 17 % | 65/67 | 294 / 2 626 (11 %) | 2 lignes |
| | V2 | 256 x 472 (248 x 463) | 142 px, 21 % | 65/67 | 294 / 2 448 (12 %) | 2 lignes |
| | V3 | 360 x 472 | 246 px, 36 % | comme V1 | 294 / 2 133 (14 %) | — |
| 1920 x 1080, 125 % (1280 x 720) | **V1** | deux 256 x 576 (droit 252 x 567) | **0** | **67/67** | 391 / 2 225 (18 %) | 2 lignes |
| | V2 | deux 256 x 720 (252 x 711) | 0 | 67/67 | 529 / 2 225 (24 %) | 2 lignes |
| | V3 | droit 360 x 576 pendant « Détails » | 104 px, 10 % | comme V1 | 391 / 1 929 (20 %) | — |
| 1920 x 1080, 100 % | V1 | deux 320 x 720 (316 x 711) | 0 | 67/67 | 527 / 1 973 (27 %) | 1 ligne |
| | V2 | deux 320 x 900 (316 x 891) | 0 | 67/67 | 707 / 1 973 (36 %) | 1 ligne |
| | V3 | droit 360 x 720 | 40 px, 3 % | comme V1 | 527 / 1 939 (27 %) | — |
| 1920 x 1080, 150 % | V1 | unique étroit 230 x 520 (222 x 511) | 17 px, 2 % | 67/67 | 342 / 2 626 (13 %) | 2 lignes |
| | V2 | 256 x 680 (248 x 671) | 43 px, 5 % | 67/67 | 489 / 2 448 (20 %) | 2 lignes |
| | V3 | 360 x 520 | 147 px, 17 % | comme V1 | 342 / 2 133 (16 %) | — |
| 2 écrans : 1366 à 125 % (client) + 1920 à 100 % | V1 = V2 | deux 340 x 900 sur le second écran (336 x 891) | 0 | 67/67 | 707 / 1 956 (36 %) | 1 ligne |

Autres relevés, les mêmes pour V1, V2 et V3 :
- textes : 13 px au moins (12 px pour les libellés de rôle du compact) ; conseil 24 px ; phrases 13 px ; bouton 15 px ;
- débordement horizontal : 0 px partout ;
- le panneau droit à 1920 x 1080 à 125 % garde les 5 postes et le profil sans défiler à gauche (8/8) ;
- à 1366 x 768 à 150 %, la zone utile n'a que 463 px de haut : 2 cas sur 67 restent sous la barre (rôle à la main avec proposition et recherche visible : la menace ; nom anglais long : le nom du consulté, qui reste dans le bouton « I'm playing Renata Glasc »). V2 n'y change rien : la hauteur est déjà celle de l'écran.

**Effet de 125 et 150 %.** À 1366 x 768, chaque palier d'échelle rétrécit le panneau en hauteur (567, 511, 463 px utiles) et élargit la part du client couverte (6, 11, 17 %). À 1920 x 1080, 150 % fait passer de deux panneaux à un panneau étroit (2 % du client couvert).

**Deux écrans.** Le placement envoie les panneaux sur l'autre écran quand celui du client n'a pas de place : deux 340 x 900, rien de couvert. Cas calculé par `Layout` ; pas essayé avec deux vrais écrans.

## 4. Compromis de lisibilité et de placement

- **Le panneau étroit couvre le bord droit du client**, là où la sélection montre l'équipe adverse. V2 ajoute 26 px de recouvrement à 1366 x 768 (93 → 119 px à 125 %). V3 en ajoute 130 pendant la lecture approfondie (223 px, 27 % du client). Sans lecture du client, le joueur doit voir les picks adverses pour les saisir.
- **La hauteur coûte peu.** Un panneau plus haut que le client ne couvre que le bureau. V2 agrandit la fenêtre de la lecture approfondie (1920 à 125 % : 391 → 529 px, +35 %), sans rien changer à la lecture immédiate, déjà entière en V1.
- **La largeur aide peu.** À 360 px, la lecture approfondie raccourcit de 13 à 20 % quand le panneau fait moins de 260 px de large, de 2 % à 316 px (moins de retours à la ligne). Le gain en part visible reste faible (13 → 16 % à 1366 x 768 à 125 %) pour 27 % du client masqué.
- **Ce que V1 resserre pour tenir à 222 x 511 :** « Pourquoi » et la comparaison du consulté passent dans les détails (compact jusqu'à 700 px de haut) ; menace sur une ligne sous 600 px ; « Je joue Twisted Fate » sur deux lignes à 222 et 252 px de large (le libellé « Détails » est gardé).
- **Panneau gauche :** la recherche se masque quand les 5 adversaires sont saisis ; forces et failles avant les jauges, pour rester visibles à 567 px.

## 5. Captures

`docs/qa/captures-panneaux-primaires/` : **vraies fenêtres WebView2** de l'exe compilé (avant : `main` 4fb621f ; après : e8ae3ed), lancées sur un bureau caché (rien à l'écran), dossier de données neuf, clé `Software\DRAFT-Test-Panneaux\Run` retirée à la fin. Pools et drafts fictifs.

- La **zone utile** et l'échelle sont imposées par le protocole de débogage (`Emulation.setDeviceMetricsOverride`) : la fenêtre, elle, garde la place que lui donne le programme sans client LoL.
- L'**état du programme** (lecture du client, poste attribué) est injecté dans chaque page (fixture) : aucun vrai client.
- **« Dans League »** (scénario 9 et « verrouillé ») est une **donnée de test**, annoncée en bandeau bleu sur l'image.
- Chaque image porte un bandeau « Données fictives ». Les manifestes (`manifeste-*.csv`) donnent, pour chaque image, l'essentiel visible relevé dans la vraie page.

Scénarios : 1 début (composition inconnue, rôle détecté), 2 partielle, 3 complète, 4 conseil différent du consulté, 5 égalité, 6 pool de 9, 7 rôle changé à la main, 8 lecture du client indisponible, 9 compact avec « Dans League », 10 textes longs (FR et EN), plus « Dans League verrouillé ».

Essentiel visible sans défiler, relevé dans les vraies pages :

| Panneau, taille | Avant | Après |
|---|---|---|
| Unique 222 x 511, 125 % (1366), FR, 11 scénarios | 73/80 : menace (2), consulté (4, 9, 10), conseil (5), conseil et menace (7) cachés | **81/81** |
| Droit 252 x 567, 125 % (1920), FR, 10 scénarios | 61/61 | 72/72 |
| Gauche 252 x 567, 125 %, FR, 5 scénarios | 5/10 : profil adverse sous le pli | **10/10** |
| Unique 222 x 463, 150 % (1366), scénario 4 | 7/9 | 9/9 |
| Unique 222 x 567, 100 % et 222 x 511, 150 % (1920) | 8/9 chacun | 9/9 chacun |
| Anglais, scénarios 4, 9, 10, 11 | compact 16/18 | compact 34/34, droit 25/25 |

Planches (`planches/`) :
- actuel / proposé, 1366 x 768 à 125 % : `planche-1366-125-avant-apres-fr.png`, `planche-1366-125-avant-apres-fr-suite.png` ;
- actuel / proposé, 1920 x 1080 à 125 % : `planche-1920-125-droit-avant-apres-fr.png`, `planche-1920-125-droit-avant-apres-fr-suite.png`, `planche-1920-125-gauche-avant-apres-fr.png` ;
- V1 / V2 / V3 et lecture approfondie : `planche-variantes-v1-v2-v3.png` ;
- 100, 125 et 150 % : `planche-echelles-100-125-150.png` ;
- FR / EN : `planche-fr-en-apres.png` ;
- « Dans League » ou « À la main » : `planche-dans-league-ou-a-la-main.png`.

**Pas testé avec un vrai client LoL :** placement réel à côté du client, lecture réelle (rôle, adversaires), recouvrement réel de la colonne adverse, deux vrais écrans, échelles 100 et 150 % sur un vrai écran (seule l'échelle de la page est simulée), « Dans League » réel.

## 6. Tests exécutés

En série, un seul lancement lourd à la fois. Exe compilé depuis la branche (`54aa5fb`, sans la référence Meraki absente du dépôt), bureau caché, dossiers de données neufs, clé `Software\DRAFT-Test-Panneaux\Run` retirée à la fin.

| Test | Résultat |
|---|---|
| Banc de mise en page FR (`tools/run-layout-tests.ps1`), avec les nouveaux relevés « essentiel visible sans défilement » (conseil, rôle et pool, pourquoi, ban, consulté, adversaires, « Dans League ») et 4 relevés de plus | **TOUT EST BON**, 61 relevés (coche et nom compris) |
| Banc de mise en page EN | **TOUT EST BON**, 61 relevés ; français visible 0, clés absentes 0. Une première passe avait signalé un défilement de 6 px du panneau gauche 400 x 720 (animation des portraits en cours) : non reproduit sur 3 essais ciblés, puis passe complète propre |
| Banc de mise en page, pseudo-langue (textes allongés, ne fait pas échouer) | 151 débordements listés contre 128 sur `main` ; clés absentes 0. En plus : à 222 x 511, la menace, le consulté, le conseil ou le ban passent sous la barre dans 13 relevés (textes 30 à 40 % plus longs) ; 4 px de défilement horizontal du compact 222 x 511 (4 relevés) ; onglets de la fenêtre (défaut déjà présent sur `main`) dans les relevés ajoutés. Textes hors crochets : 9 contre 8, le seul en plus est un nom fictif (« Le Maître Des Cartes Du Destin ») repris dans l'infobulle du pool |
| `draft-choix-tests.html` | **220 réussis, 0 échec** |
| `draft-beta1-reel-tests.html` | **182 réussis, 0 échec**. Une première passe donnait 176/6 : après « Comparer » (ouvert dans le panneau), la consultation restait sous le pli. Corrigé (consulter ramène en haut du panneau) |
| Mesures des variantes (`tools/run-panneaux-tests.ps1`) | tableau de la section 3 |
| Captures WebView2 réelles | 93 images, essentiel relevé dans chaque page (section 5) |
| selftest FR / EN | **330 / 0** et **330 / 0** |
| e2e FR | **103 réussis, 1 échec** : Ctrl+Alt+D laissé à la 2.x, raison « instance » (DRAFT de l'utilisateur, pid 32132, non touché). Dans l'e2e, les vraies fenêtres des panneaux mises aux tailles du placement (1366 à 125 % : 222 x 511 ; 1920 à 100 et 125 %) montrent « Je joue », « Détails », « Revenir au conseil », le rôle, le consulté et la menace sans défiler |
| e2e EN | **101 réussis, 3 échecs**, deux fois (2 lancements) : Ctrl+Alt+D (« instance », comme en FR) ; « panneau collé au client à chaque pas » (11 écarts) ; « Rafales : les panneaux rattrapent la dernière position » (moyenne 7,1 puis 10,1 ms). **Comparaison avec `main`** (même machine, 2 lancements EN) : « collé au client » échoue aussi (11 et 9 écarts), défaut déjà présent ; « Rafales » réussit (6,4 et 6,7 ms). Ce second écart n'est pas expliqué : ni le placement ni le code natif ne changent, mais un panneau plus chargé peut ralentir le suivi des fenêtres. À revérifier avant toute intégration |
| Catalogues (`check-catalogs.ps1`, FR / EN, longueurs, sécurité ; `-Code`) | 0 erreur ; 0 clé orpheline (alertes de pluriel déjà présentes) |
| Détecteur de textes en dur (`detect-hardcoded.ps1 -Max 0`) | 0 chaîne |
| Banc moteur `run-engine-eval.ps1 -AvecEvaluation` | **Banc : OK**, 0 changement contre les deux instantanés (312 et 108 scénarios), 0 inattendu |
| Fichiers protégés | aucun changement dans le moteur, les données, `src/LcuConnector.cs`, `src/AppUpdater.cs`, `src/DraftMid.cs` (placement compris), la CSP |

Les captures ont été faites avec l'exe `e8ae3ed`. `54aa5fb` n'y ajoute que le retour en haut du panneau après une consultation : aucun rendu statique ne change.

## 7. Recommandation

**Retenir V1.** Raisons, chiffrées :
- c'est la seule variante qui ne masque pas davantage le client : 93 px à 1366 x 768 à 125 %, comme aujourd'hui ;
- la lecture immédiate y est entière à toutes les tailles réelles de 500 px de haut et plus (67/67 dans Chrome ; 81/81 et 72/72 dans les vraies fenêtres à 125 %), alors que l'actuel cache le consulté, la menace ou le conseil dans 6 cas sur 11 à 222 x 511 ;
- toute la lecture approfondie de la grande fenêtre utile en draft est dans le panneau, sans changer de fenêtre.

Ce qu'il faut valider ensuite, dans cet ordre :
1. **la hauteur de V2 pour les panneaux à côté du client** (deux panneaux, panneau unique) : aucun recouvrement, +35 % de lecture approfondie visible à 1920 à 125 %. À essayer sur un vrai client : un panneau plus haut que le client (720 contre 576 px) reste-t-il discret ?
2. **ne pas élargir le panneau étroit** (V2 à 256 px) : +26 px sur la colonne adverse, pour un gain mesuré faible ;
3. **ne pas activer V3** : 27 % du client masqué pendant la lecture, pour 3 points de lecture visible en plus ;
4. revérifier le suivi des fenêtres en anglais (« Rafales », section 6) : 2 échecs sur 2 sur la branche, 0 sur 2 sur `main` ;
5. brancher « Dans League » sur la PR #40 (une ligne, section 1), puis remesurer la barre d'actions : en mode connecté, elle est plus haute d'environ 20 px ; le nom du consulté reste visible à 222 x 511 (relevé dans la vraie page, scénario 9), avec peu de marge.

## 8. Ajustements V2

L'utilisateur a retenu **V2** : panneau étroit de 256 x 574 à 1366 x 768 à 125 %. Les sections 1 à 7 décrivent l'état d'avant ce choix.

| Commit | Ajustement |
|---|---|
| 9af52ae | V2 réelle dans `Layout` : panneau étroit de 256 px, hauteur utile de l'écran (900 px au plus) ; panneaux à côté du client à 1,25 fois sa hauteur. « Je joue X » seul sur sa rangée. Ordre de la lecture immédiate (ci-dessous). |
| c7214f0 | Aucun mot coupé, bouton principal sur une ligne. Relevés MOT-COUPE et BOUTON-DEUX-LIGNES dans le banc. |
| e6d7aeb | Pas de défilement horizontal dans les panneaux. |
| 95d4640 | Rôle de chaque panneau ; relevés PORTRAITS-ADVERSES et PLACE-JE-JOUE. |
| captures V2 | 6 défauts vus sur les captures des vraies fenêtres ; 5 relevés de plus au banc (section « Captures V2 » ci-dessous). |

### Point 3 : un panneau pour la composition, un pour la décision

**Déjà fait dans 9af52ae, pas refait :**
- ordre de la lecture immédiate : conseil et « Pourquoi », puis risque principal (menace ; sinon risque du conseil ; avant tout adversaire, ban conseillé), puis champion consulté (verdict d'une phrase), puis changement de conseil et alternative. Le détail des comparaisons est dans les sections dépliables ;
- mode connecté (« Dans League », `leagueSlot`) : la ligne League prend la place de « Je joue ». Rien n'est réservé.

**Composition adverse.** Le panneau gauche la porte. Le panneau droit ne montre ni les 5 postes adverses ni le profil adverse. C'était déjà le cas : aucun code ne les y affichait. Le compact (panneau unique ou étroit, sans panneau gauche) montre les 5 postes.

**Rappel minimal dans le panneau droit : rien d'ajouté.** Deux éléments existants suffisent :
- « 3/5 adversaires » sur la carte du conseil, tant qu'il en manque : il dit sur quoi repose le conseil ;
- le portrait de la menace principale : c'est le risque principal, pas la composition.

Une rangée de 5 portraits ajouterait une ligne à la lecture immédiate, pour une information affichée juste à côté, à la même hauteur, et dans le client.

**Compact.** Les 5 postes restent au-dessus du conseil : c'est là qu'on saisit les adversaires quand le client n'est pas lu. Dans la partie conseil, l'ordre est celui du panneau droit.

### Comment la page sait que le panneau gauche est affiché

- **Par son adresse.** Le panneau droit est chargé avec `panel=right` (`PANEL` dans la page). Le programme ne le crée et ne l'affiche qu'en disposition « two », toujours avec le gauche : `Place` (placement suivi), `Place2` (placement manuel) et `TranslatePanels` (déplacement) montrent et déplacent les deux ensemble. En compact, `panel=compact` est seul.
- **Ce que l'état du programme transmet** (`HostState`) : `compact` (réglage), `panelsShown` (tous les panneaux), `placement` (une note). Pas la visibilité de chaque panneau.
- **Ce qui manque.** Le panneau gauche peut être masqué seul : Alt+F4 sur lui (`OnFormClosing`) le cache et laisse le droit, jusqu'au placement suivant (déplacement du client). Un panneau gauche déplacé à la main derrière une autre fenêtre n'est pas su non plus. Rien n'est supposé : dans ces cas, le panneau droit ne montre pas la composition. Pour le faire, le programme devrait envoyer la visibilité de chaque panneau dans son état. Non fait.

### Relevés ajoutés au banc

Dans `web/draft-layout-tests.html`. Ils font échouer `tools/run-layout-tests.ps1`.
- **PORTRAITS-ADVERSES** (panneau droit) : postes adverses présents ; plus d'un portrait adverse dans la lecture immédiate ; les 5 adversaires visibles, sections ouvertes comprises (menaces et cibles : 2 + 2 au plus).
- **PLACE-JE-JOUE** (mode « Dans League », donnée de test) : bouton « Je joue » présent, ou espace au-dessus de la ligne League.

Les portraits portent le nom du champion (`data-champ`) pour ce relevé. Contrôle : sur une copie volontairement fautive (postes adverses et place réservée ajoutés au panneau droit), les deux relevés échouent aux 9 tailles.

### Tests

Tests légers seulement, en série, un seul Chrome sans tête à la fois. Ni e2e ni capture WebView2 : un autre agent mesurait le suivi des fenêtres.

| Test | Résultat |
|---|---|
| Banc de mise en page FR, avec les relevés précédents (essentiel sans défilement, mots coupés, bouton sur une ligne) et les deux nouveaux | **TOUT EST BON**, 61 relevés |
| Banc de mise en page EN | **TOUT EST BON**, 61 relevés ; français visible 0, clés absentes 0 |
| `draft-choix-tests.html` | **220 réussis, 0 échec** |
| `draft-beta1-reel-tests.html` | **182 réussis, 0 échec** |
| Catalogues (`check-catalogs.ps1 -Code`) | 0 erreur, 0 clé orpheline (20 alertes de pluriel déjà présentes) |
| Détecteur de textes en dur (`detect-hardcoded.ps1 -Only web/ -Max 0`) | 0 chaîne |

### Captures V2 : 6 défauts corrigés

Repérés sur les captures des vraies fenêtres V2 : 248 x 565 (compact 1366 à 125 %), 252 x 711 (gauche et droit 1920 à 125 %), 248 x 711 (unique 1920).

| Défaut | Cause | Correction |
|---|---|---|
| 1. « ^ Conseil » à la place de « Détails », sans défilement ; clic sans effet | Le bouton basculait dès que la lecture approfondie commençait dans la moitié haute de la zone qui défile. À 711 px de haut, c'est vrai au repos ; le clic remettait `scrollTop` à 0, déjà à 0 | « Conseil » seulement après un vrai défilement (`scrollTop >= 1`). Au repos, « Détails » reste l'entrée |
| 2. Menace coupée au milieu d'un mot (compact 1366) | Une ligne au plus sous 600 px de haut | Deux lignes au plus : la phrase entière. EN : « Their targeted CC can stop your all-in. » (trois lignes à 222 px avec « point-and-click ») |
| 3. « AjouterAjouterAjouter » | « Ajouter » remplissait ses 45 px de colonne ; aucun espace entre colonnes | Case vide marquée d'un « + ». « Ajouter le Top » au survol ; l'étiquette de la case reste lue par les lecteurs d'écran |
| 4. Jauge « Engage » sous le pli (gauche EN, 252 x 711) | « Point-and-click CC » et « Squishy carries » (91 px pour 90) sur deux lignes | « Targeted CC » (même sens que « CC ciblés ») ; colonne des libellés de 100 px ; 4 px entre les jauges |
| 5. « Dans League : Twisted Fate verrouillé ✓ » sur deux lignes | 39 caractères pour 214 px | « League : {champion} verrouillé ✓ » (et « (survolé) ») ; si la place manque encore, seul le nom se raccourcit, nom complet au survol. À 248 et 252 px, « Twisted Fate » tient en entier, FR et EN |
| 6. « Presque à ég… » ; « Malp… » | Étiquette de l'alternative : 101 px pour 90. Nom adverse : colonne de 45 px | Étiquette sur deux lignes au plus, coupée entre deux mots. Noms adverses : toujours tronqués (un seul mot plus large que la colonne), nom complet au survol de la case (le titre du nom était sous la zone cliquable) |

### Relevés ajoutés au banc (captures V2)

Dans `web/draft-layout-tests.html` ; ils font échouer `tools/run-layout-tests.ps1`. Taille 248 x 711 ajoutée au compact. Temps virtuel porté à 180 s : avec cette taille, l'état « extreme » dépassait 120 s (aucune mesure).
- **DETAILS-ABSENT** : zone qui défile en haut, et le bouton propose « Conseil ».
- **JAUGE-SOUS-LE-PLI** : panneau gauche de 700 px de haut et plus, une des 5 jauges sous le pli.
- **LEAGUE-DEUX-LIGNES** : ligne « Dans League » sur deux lignes.
- **TEXTES-COLLES** : deux textes de colonnes voisines (postes adverses du compact, ex æquo) à moins de 3 px sur la même ligne. Un texte en animation figée (`--dump-dom` ne produit aucune image) est ignoré.
- **MENACE-COUPEE** : phrase de la menace tronquée (une ligne masquée).

| Banc | Code de 95d4640 | Après |
|---|---|---|
| FR | 55 défauts : DETAILS-ABSENT, TEXTES-COLLES « Ajouter », MENACE-COUPEE, LEAGUE-DEUX-LIGNES | **TOUT EST BON** |
| EN | 90 défauts : les mêmes, plus JAUGE-SOUS-LE-PLI (jusqu'à 36 px sous le pli à 252 x 711, 11 px à 280 x 720) | **TOUT EST BON** ; français visible 0, clés absentes 0 |
| qps (ne fait pas échouer) | | 488 débordements listés, dont 152 TEXTES-COLLES et 123 JAUGE-SOUS-LE-PLI (nouveaux relevés, textes allongés) et 111 DEFILE-X des onglets de la fenêtre (déjà présents) ; hors crochets 9 ; clés absentes 0 |