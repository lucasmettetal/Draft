# Audit UX de DRAFT 2.16.1

Branche `etude/ux-i18n`, partie de `origin/main` 78519fb (DRAFT 2.16.1). Étude seule : aucun fichier de production n'est modifié.

Documents de l'étude :

| Document | Contenu |
|---|---|
| [AUDIT-UX.md](AUDIT-UX.md) | Synthèse, méthode, les dix problèmes principaux, le reste |
| [PARCOURS-UTILISATEURS.md](PARCOURS-UTILISATEURS.md) | Parcours A à H : écrans, clics, ambiguïtés, risques |
| [ARCHITECTURE-NAVIGATION.md](ARCHITECTURE-NAVIGATION.md) | Carte actuelle et architecture cible |
| [OPTIONS-REFONTE.md](OPTIONS-REFONTE.md) | Options A, B, C et recommandation |
| [DESIGN-SYSTEM.md](DESIGN-SYSTEM.md) | Typographie, couleurs, composants, accessibilité mesurée |
| [PLAN-I18N-FR-EN.md](PLAN-I18N-FR-EN.md) | Plan d'internationalisation français / anglais |
| [FEUILLE-DE-ROUTE-UX.md](FEUILLE-DE-ROUTE-UX.md) | PR après V2.17 : ordre, fichiers, tests, critères |
| [captures/](captures/) | Captures de la vraie WebView2, données de test vides |
| [outils/](outils/) | Scripts d'observation (CDP, fenêtres, mesures, reproduction) |
| [maquettes/](maquettes/) | Maquette de navigation, **non validée** |

## 1. Synthèse

Le ressenti « pas professionnel » ne vient pas d'abord du graphisme. Les panneaux de draft sont propres, lisibles et bien testés : 29 tailles sans débordement, aucun vol de focus, contrastes de texte supérieurs à 5:1. Le problème est l'**organisation**.

DRAFT n'a pas de « maison ». Au lancement, aucune fenêtre ne s'ouvre. Tout passe par l'icône près de l'horloge et un menu de quinze commandes. La fenêtre principale s'appelle « Analyse détaillée ». Elle sert à la fois de simulateur et de copie de la draft réelle. Les réglages sont une modale de douze sections. On y trouve le pool, le rôle, la lecture du client, les fenêtres, la mise à jour, l'import Riot, les calculs de 65 minutes et les sources de données.

Ce que l'on ressent sans pouvoir l'expliquer :

- **« Je ne sais jamais où je suis. »** Il n'existe pas de navigation principale. Les fonctions sont réparties entre le menu de l'icône, une modale, deux niveaux d'onglets dans une colonne et trois dialogues Windows.
- **« Ça a fait quelque chose que je n'ai pas demandé. »** Modifier le pool d'un autre rôle change le rôle actif. Une simulation écrase la draft réelle. Ouvrir l'analyse depuis un panneau vide ce panneau.
- **« Ça n'a rien fait. »** Un champion saisi avant de choisir un rôle disparaît sans message. Une recherche de mise à jour lancée depuis le menu ne donne aucune réponse visible.
- **« Ça ressemble à un outil de développeur. »** Messages internes visibles (« Licence non confirmée », « 4 absente(s) »), consignes obsolètes (« Lance draft-import-stats.bat »), dialogues Windows clairs au milieu d'une application sombre.

La bonne réponse est une **réorganisation des parcours**, pas une refonte graphique. La recommandation est l'option B : une vraie navigation dans la fenêtre principale (Accueil, Mon pool, Simulateur, Mes statistiques, Réglages), un overlay de draft limité à la draft, et quelques corrections de comportement à faire en premier.

## 2. Méthode

### Ce qui a été réellement observé

- **Vraie WebView2.** La copie de travail a été compilée (`build.ps1 -Out %TEMP%\draft-ux\build`). L'exe a été lancé avec un dossier de données temporaire et vide (`--data %TEMP%\draft-ux\data --no-hotkey --run-key Software\DRAFT-UX-Audit\Run`). Un port CDP local (127.0.0.1:9339) a permis de lire l'état des pages, de simuler des saisies et de capturer chaque page. Scripts : [outils/](outils/).
- **Captures.** Uniquement les pages et fenêtres de ce processus : CDP `Page.captureScreenshot` pour les pages, `PrintWindow` sur les fenêtres du pid pour les dialogues Windows. Jamais l'écran entier. Aucune donnée personnelle : profil de test Mid (Ahri, Fizz, Kog'Maw).
- **Écran de test.** 1920 × 1080 physiques, échelle Windows 125 %. Fenêtre principale 1280 × 792 pixels logiques. Panneaux 336 px de page (420 physiques).
- **Mode de bout en bout existant.** `--e2e` (faux client LoL, LCU simulé) : 100 % des vérifications passent (« TOUT EST BON »). Il sert de preuve pour le parcours F.
- **Tests de rendu existants.** `tools/run-layout-tests.ps1` dans Chrome headless, dossier temporaire redirigé sous `%TEMP%\draft-ux` : 31 états, « TOUT EST BON ».
- **Code.** Lecture ciblée de `web/index.html` (2 429 lignes), `src/DraftMid.cs`, `src/HistoryImporter.cs`, `src/RoleRatesUpdater.cs`, `src/AppUpdater.cs`.

### Ce qui est estimé

- Le client LoL n'était pas lancé. La draft réelle est décrite d'après le mode `--e2e` et le code.
- Aucune clé Riot n'a été utilisée. L'import et les calculs sont décrits jusqu'au dialogue de saisie, puis d'après le code.
- La mise à jour n'a pas été lancée (elle contacte GitHub). Le parcours vient du code.
- Le menu de l'icône n'a pas été ouvert : il est dans la zone de notification de la personne. Son contenu vient du code (`src/DraftMid.cs:405-432`).
- Échelles 150 % et 175 % : non observées. L'émulation CDP de taille ne s'applique pas dans cette WebView2 ; les petites tailles viennent des tests de rendu Chrome.

### Outils de méthode

- **Méthode Impeccable (critique et audit) appliquée à la main à partir de ses fiches, outil non installé.** Grilles utilisées : heuristiques de Nielsen notées de 0 à 4, liste de charge cognitive, priorités P0 à P3, audit technique en cinq dimensions. Le détecteur automatique d'Impeccable n'a pas tourné. Les fiches demandent deux évaluations isolées par des sous-agents : elles ont été faites dans un seul contexte. **Mode dégradé : un seul contexte, détecteur absent.**
- **Improve UI appliqué à la main à partir de sa fiche, outil non installé.** Contre-expertise : chaque constat doit avoir une preuve de règle, une preuve d'exécution et une seule correction.
- **Fiches Impeccable clarify et onboard, Baseline UI** : consultées pour les messages, le premier lancement et les actions destructrices.
- **Skill frontend-design** : non invoqué. La réorganisation des parcours répond mieux au problème qu'une nouvelle direction graphique.
- Études reprises sans les refaire : `GLOSSAIRE-LOL.md`, `AUDIT-VOCABULAIRE.md`, `MESSAGES-A-CORRIGER.md` (branche `v217/vocabulaire-lol`, PR #9).

### Artefact de mesure écarté

Une fermeture de la fenêtre principale par `WM_CLOSE` brut détruit la fenêtre (WinForms y voit `CloseReason.None`). « Analyse détaillée » lève alors une `NullReferenceException` (`draftmid.log`). Un clic sur la croix envoie `SC_CLOSE` et masque seulement la fenêtre. Ce cas vient du banc, pas d'un parcours normal : il est classé P3 (robustesse) et n'entre pas dans les dix problèmes.

## 3. Notes globales

### Heuristiques de Nielsen (méthode Impeccable, à la main)

| # | Heuristique | Note | Constat principal |
|---|---|---|---|
| 1 | Visibilité de l'état | 2 | Phase dans l'infobulle de l'icône ; résultat de mise à jour invisible hors réglages |
| 2 | Langage du monde réel | 3 | Vocabulaire LoL juste ; jargon interne (« Heuristique », « Meraki », « Licence non confirmée ») |
| 3 | Contrôle et liberté | 1 | Échap efface la draft sans annulation ; la simulation écrase la draft réelle |
| 4 | Cohérence | 2 | Trois styles d'état actif, deux familles d'icônes, dialogues clairs dans une app sombre |
| 5 | Prévention des erreurs | 1 | Saisie perdue sans rôle ; changement de rôle implicite |
| 6 | Reconnaître plutôt que se souvenir | 2 | Fonctions cachées dans le menu de l'icône ; Ctrl+Alt+D à retenir |
| 7 | Souplesse et efficacité | 3 | Saisie clavier rapide, alias (« tf », « kata »), 320 frappes sans perte (e2e) |
| 8 | Esthétique et minimalisme | 2 | Panneaux sobres ; réglages en mur de 45 contrôles |
| 9 | Récupération après erreur | 2 | Imports sûrs (« rien n'a été modifié ») ; aucune annulation de draft |
| 10 | Aide et documentation | 2 | Aides en ligne nombreuses ; guide utilisateur resté en V2.14 |
| **Total** | | **20/40** | **Acceptable, travail important nécessaire** |

Charge cognitive : 5 critères sur 8 échouent (focus unique, regroupement, choix limités, mémoire de travail, divulgation progressive des réglages). Charge **élevée**.

### Audit technique (méthode Impeccable, à la main)

| # | Dimension | Note | Constat |
|---|---|---|---|
| 1 | Accessibilité | 3 | Texte ≥ 5:1 partout ; bordures de champs à 1,48:1 ; libellés de rôle à 10,5 px |
| 2 | Performance | 3 | WebView2 libérée sans client ; panneaux visibles 11 ms après détection (e2e) |
| 3 | Thème | 2 | Jetons de couleur présents ; 21 tailles de police ; 3 hex et 21 rgba hors jetons |
| 4 | Adaptation aux tailles | 3 | 31 états × 5 à 29 tailles sans défaut (tests de rendu) |
| 5 | Intégrité | 2 | Dialogues natifs hors système ; icônes Unicode et SVG mêlées |
| **Total** | | **13/20** | **Acceptable** |

## 4. Les dix problèmes principaux

Classement : gravité d'abord, puis portée, puis effort. Effort : S (moins d'une journée), M (2 à 4 jours), L (une semaine ou plus). Estimations.

### 1. Ouvrir l'analyse depuis un panneau vide la draft du panneau — P0

- **Preuve observée.** Panneau rempli, clic sur « Analyse détaillée » ou sur l'alternative (« Comparer ») : le panneau repasse à 0 adversaire, la fenêtre principale garde la draft. **16 pertes sur 18 essais** (10 sur 10 avec `outils/repro-desynchro.ps1`). Même page (marqueur conservé) : l'état est écrasé, pas rechargé. Capture : [21-panneau-vide-apres-analyse.png](captures/21-panneau-vide-apres-analyse.png).
- **Cause (hypothèse forte).** La page rechargée envoie `hello` (`web/index.html:2354`), puis reçoit l'état du programme et appelle `handleAssignedRole` (`:2371`, `:943`), qui diffuse son état encore vide avant d'avoir reçu les réponses. Le journal des messages relevé dans le panneau montre : `hello`, trois états à 3 adversaires, puis un état à 0.
- **Pourquoi c'est grave.** Avec la lecture du client, les adversaires ne sont renvoyés qu'à leur prochain changement (`src/DraftMid.cs:604-614`) : le panneau reste faux jusqu'au pick suivant. Sans lecture du client, la saisie est perdue en pleine sélection.
- **Correction.** Une page qui vient de s'ouvrir ne diffuse rien avant d'avoir reçu un état ou un délai court sans réponse. Test e2e : 0 perte sur 50 ouvertures.
- **Effort** : S.

### 2. Le simulateur et la draft réelle partagent le même état — P1

- **Preuve observée.** « Draft aléatoire » dans la fenêtre principale : les panneaux reçoivent la même composition aléatoire. Captures [13](captures/13-draft-aleatoire-fenetre.png) et [14](captures/14-draft-aleatoire-repercutee-panneau.png). « Nouvelle draft » et toute saisie suivent le même chemin (`BroadcastChannel("draftmid")`, `web/index.html:896-905`).
- **Ressenti.** « J'ai testé une idée et ma vraie draft a disparu. » C'est l'erreur d'action « dans le mauvais écran ».
- **Correction.** Deux sources distinctes : « Draft en cours » (partagée avec l'overlay) et « Simulation » (locale à la fenêtre). Bandeau visible quand on simule.
- **Effort** : M.

### 3. Le pool et le rôle sont rangés dans les réglages techniques — P1

- **Preuve observée.** La modale « Réglages » fait 1 598 px de haut pour 722 visibles (2 337 avec les détails ouverts). Elle contient 45 contrôles et douze sections : profils et pool, niveau des conseils, mise à jour, historique Riot, détection, fenêtres, exclusions, rôles des champions, matchups, journal, sources de données, avancé. Captures [01](captures/01-premier-lancement-reglages.png), [05](captures/05-reglages-milieu.png), [06](captures/06-reglages-bas.png). Code : `web/index.html:2214-2316`.
- **Ressenti.** C'est exactement le constat de départ : « trop de fonctionnalités au même endroit ». Les exclusions du pool (« Mes picks ») sont sous « Fenêtres », loin du pool.
- **Correction.** Page « Mon pool » dédiée (cinq rôles, exclusions, favori, export). Réglages réduits au technique, en sous-sections.
- **Effort** : M (avec la navigation, voir option B).

### 4. Modifier le pool d'un autre rôle change le rôle actif — P1

- **Preuve observée.** Profil Mid actif. Clic sur « Top » dans « Mes profils », ajout de Darius : le rôle actif devient Top, la recommandation devient Darius. Capture [04](captures/04-autre-role-top-devient-actif.png). Code : `web/index.html:2165` (`pickRole` appelle `switchRole`) et `:2106` (`saveMyProfile` bascule aussi).
- **Ressenti.** « Je voulais juste préparer mon jungle, et la draft suivante m'a conseillé un jungler au mid. »
- **Correction.** Séparer « voir ou modifier ce rôle » de « jouer ce rôle ». Un bouton explicite « Jouer ce rôle ».
- **Effort** : S.

### 5. Premier lancement : la première saisie disparaît sans message — P1

- **Preuve observée.** Premier lancement, aucun rôle choisi. Saisie « ahri », Entrée : le champ se vide, rien n'est ajouté, aucun message. Capture [02](captures/02-ajout-sans-role.png). Code : `web/index.html:2166` (`if (!r) return`).
- **Ressenti.** « Ça ne marche pas. » C'est le premier geste du premier lancement.
- **Correction.** Désactiver le champ tant qu'aucun rôle n'est choisi, avec la consigne « Choisis d'abord ton rôle », ou proposer le rôle en même temps.
- **Effort** : S.

### 6. Pas de fenêtre d'accueil : tout passe par l'icône et un menu de quinze commandes — P1

- **Preuve observée.** Second lancement : aucune fenêtre, aucune page WebView2 (port CDP fermé). Le menu de l'icône compte quinze commandes (`src/DraftMid.cs:405-432`) : usage quotidien, apprentissage de la détection (« C'est une draft : retenir cet écran »), réglages en double, calculs de 40 à 65 minutes, import de fichier, démarrage avec Windows, mise à jour.
- **Hypothèse.** Sous Windows 10, une icône récente est souvent rangée dans la zone de dépassement. DRAFT peut alors sembler ne pas s'être lancé.
- **Correction.** Le raccourci du menu Démarrer ouvre la fenêtre principale sur « Accueil ». Le menu de l'icône descend à huit commandes au plus.
- **Effort** : M.

### 7. Des actions sans retour visible — P1

- **Preuve (code).** « Rechercher une mise à jour... » depuis le menu : le résultat (« DRAFT est à jour », erreur, blocage pendant une draft) n'est écrit que dans l'état de la page des réglages (`src/DraftMid.cs:1777-1822`, `:1209-1225`). Sans réglages ouverts, rien ne s'affiche. Seuls les échecs de redémarrage et la réussite ont une bulle.
- **Observé.** Fermer la fenêtre principale la masque sans le dire ; le programme continue près de l'horloge (`src/DraftMid.cs:318-322`).
- **Correction.** Toute action lancée depuis le menu répond au même endroit : bulle Windows courte, et carte d'état sur l'Accueil.
- **Effort** : S.

### 8. Trois historiques, trois endroits, une consigne obsolète — P2

- **Preuve observée.** Onglet « Parties » vide : « Lance draft-import-stats.bat, placé dans le même dossier que cette page, puis recharge-la. » Capture [16](captures/16-onglet-parties-vide.png), `web/index.html:1860`. Dans une application installée, ce dossier n'existe pas pour la personne.
- **Constat (code et captures).** Journal DRAFT (« Je joue ») dans « Parties » ; statistiques importées par un script `.bat` puis par « Importer mes statistiques... » du menu ; historique Riot importé depuis les réglages mais affiché dans « Conseils », pas dans « Parties ». La clé Riot se ressaisit dans trois dialogues distincts ([18](captures/18-dialogue-import-historique.png), [19](captures/19-dialogue-cle-matchups.png)).
- **Correction.** Une page « Mes statistiques » : journal, historique Riot, ancien import en option. Un seul point d'entrée pour la clé.
- **Effort** : M.

### 9. Actions destructrices sans confirmation ni annulation — P2

- **Preuve observée.** Quatre adversaires saisis, Échap dans le champ vide : la draft entière est effacée, sans annulation. Code : `web/index.html:2388-2389`. La consigne « Entrée ajoute, Échap efface » ne dit pas que toute la draft part.
- **Aussi.** « Draft aléatoire » remplace la draft sans prévenir (`:1016`).
- **Correction.** Échap n'efface que la saisie. « Nouvelle draft » propose « Annuler » quelques secondes.
- **Effort** : S.

### 10. Signaux d'amateurisme accumulés — P2

Chacun est mineur. Ensemble, ils expliquent l'impression « pas pro ».

- **Dialogues Windows clairs** dans une application sombre ([18](captures/18-dialogue-import-historique.png), [19](captures/19-dialogue-cle-matchups.png)). Bouton fichier natif gris (« Choisir un fichier ») dans les réglages ([06](captures/06-reglages-bas.png), `web/index.html:2304`).
- **Icônes mêlées** : caractères Unicode « ⧉ ▭ ⚙ × » pour les outils du panneau (`:2017-2019`), SVG pour les rôles.
- **21 tailles de police** et une douzaine de styles de boutons (`ghost-btn`, `cta`, `p-btn`, `link-btn`, `seg-tab`, `tab`, `role-btn`, `pool-chip`, `rank`, `cmp-btn`, `p-alt`, bouton fichier natif) dans une seule feuille de style.
- **Messages internes visibles** : « Licence non confirmée : à vérifier avant de la présenter comme une source autorisée » (`web/draft-data.js:105`), « Sources de données : 4 absente(s) » dès le premier lancement.
- **Signaux contradictoires** sur un même panneau : « Choix serré », « Recommandé », « Presque à égalité » ([12](captures/12-panneau-droit-draft.png)). « Je joue » et « Je prends » (`:1977`). « Avis général basé sur les 5 champions adverses » avec 0 adversaire ([07](captures/07-fenetre-principale-vide.png), `:1676`). Ces textes relèvent des PR D, E et C-2 en cours.
- **Correction.** Voir [DESIGN-SYSTEM.md](DESIGN-SYSTEM.md). Effort : M, en plusieurs passes.

## 5. Autres constats

| Prio | Constat | Preuve |
|---|---|---|
| P2 | Deux niveaux d'onglets dans la colonne de droite (5 onglets, puis 3 vues dans « Analyse », puis 3 phases dans « Plan ») | Capture 15, `web/index.html:1943-1967` |
| P2 | Bloc « Plan de jeu » au-dessus des onglets et onglet « Plan » : même contenu deux fois | Capture 07 |
| P2 | « Niveau des conseils » réglable à deux endroits (Réglages et onglet Conseils) | `web/index.html:1501`, `:2242` |
| P2 | La fenêtre principale s'appelle « DRAFT » dans Windows, « Analyse détaillée » dans le menu ; elle sert surtout de simulateur | `src/DraftMid.cs:279`, `:421` |
| P2 | La roue dentée du panneau ouvre tous les réglages, pas ceux des panneaux | `web/index.html:2018` |
| P2 | Lecture du client « Recommandé » mais désactivée par défaut, et « à tes risques » juste en dessous | Capture 05, `src/DraftMid.cs:176-177` |
| P2 | Le dialogue des matchups ne dit ni le rôle ni le pool calculés, pour 65 minutes de calcul | Capture 19, `src/DraftMid.cs:825-827` |
| P2 | Sans client, les panneaux se placent aux bords de l'écran et chevauchent la fenêtre principale | Rectangles relevés : panneaux 0 et 1495, fenêtre 40 à 1640 (physiques) |
| P3 | Bordures de champs et de cartes à 1,48:1 sur le fond (WCAG 1.4.11 demande 3:1 pour la limite d'un champ) | `--line:#263242` sur `--bg:#0a0f16` |
| P3 | Libellés de rôle à 10,5 px dans les panneaux | Mesure CDP, `role-icon` |
| P3 | « Kog'Maw : Peu adapté » affiché avec 0 adversaire | Capture 07 |
| P3 | Les panneaux s'appellent « DRAFT : left », « DRAFT : right » (identifiants internes) ; visibles dans Alt+Tab (hypothèse) | `src/DraftMid.cs:279` |
| P3 | Fenêtre principale détruite hors fermeture normale : « Analyse détaillée » ne la rouvre plus avant redémarrage | Artefact du banc, `draftmid.log` |
| P3 | Guide utilisateur resté en V2.14 (import par script `.bat`) | `docs/utilisateur/GUIDE.md:57` |

## 6. Ce qui fonctionne et doit rester

- **Panneaux de draft.** Hiérarchie claire : nom conseillé en grand, raisons courtes, danger, action principale « Je joue X ». Aucun débordement sur 29 tailles.
- **Comportement de l'overlay.** Apparition 11 ms après la détection, aucun vol de focus, suivi du client sans retard, masquage en partie (e2e, 100 % des vérifications).
- **Saisie rapide.** Alias, flèches, Tab, Retour arrière : 320 frappes sans perte (e2e).
- **Honnêteté des données.** Les sources, la confiance et les limites sont dites. À garder, mais dans un langage de produit.
- **Sécurité.** CSP stricte, clé Riot jamais enregistrée ni visible par la page, liens externes filtrés. Toute proposition de cette étude la respecte.
