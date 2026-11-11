# Validation de DRAFT

Ce qui est vérifié automatiquement, ce qui ne l'est pas, et comment le vérifier à la main.

## Lancer les tests

```powershell
.\build.ps1 -Out dist-test
dist-test\DRAFT-Portable.exe --selftest --data <dossier> --out selftest.txt
dist-test\DRAFT-Portable.exe --e2e --data <dossier> --out e2e.txt
dist-test\DRAFT-Portable.exe --soak 15 --data <dossier> --out soak.txt
.\tools\run-layout-tests.ps1
.\tools\run-accuracy-tests.ps1
.\tools\run-coaching-tests.ps1
.\tools\run-engine-eval.ps1
```

Résultats, branches et statut de publication : voir [Branches et publication](BRANCHES.md). Le test `web/draft-tests.html` s'ouvre dans un navigateur. Dans `--e2e`, « Raccourci Ctrl+Alt+D enregistré » échoue si un autre DRAFT tourne : il occupe le raccourci.

## Ce que chaque test prouve, et ce qu'il ne prouve pas

| Test | Prouve | Ne prouve pas |
|---|---|---|
| `--selftest` | Calculs de placement sur 5 résolutions et 4 échelles, reprise, lecture LCU | Le rendu sur de vrais écrans |
| `--e2e` | Vraies fenêtres et vrais WebView2, sur une fausse fenêtre LoL et un faux LCU | Le comportement du vrai client, l'antitriche, le ressenti |
| `--soak` | Stabilité sur plusieurs cycles | Une longue session réelle |
| `run-layout-tests` | Rendu Chrome de chaque état, à chaque taille | L'écran réel, les échelles fractionnaires |
| `run-accuracy-tests` | Estimation des rôles adverses (5 rôles), sur la réserve de vraies compositions si elle existe, sinon sur des drafts synthétiques tirées de la fiche | La réserve n'existe qu'après un calcul des rôles ; les drafts synthétiques ne donnent qu'une borne haute |
| `run-engine-eval` | Sur 312 scénarios SYNTHÉTIQUES : invariances (ordre, déterminisme), égalités attendues, picks pris, et aucun changement non déclaré par rapport à l'instantané de référence | La qualité des conseils : les réponses attendues sont écrites à la main, sans experts ni vraies parties |

## Banc d'évaluation du moteur

Mesure reproductible du moteur stratégique (`web/draft-engine.js`, `draft-data.js`, `draft-champions.js`, `draft-role-sheet.js`), indépendante du moteur lui-même. **Tous les scénarios sont SYNTHÉTIQUES** : ils montrent ce qui change d'une version à l'autre, pas si les conseils sont bons en vraie partie. Chiffres de départ : [Référence chiffrée 2.16.1](REFERENCE-MOTEUR-2161.md).

### Fichiers (`tests/moteur/`)

| Fichier | Rôle |
|---|---|
| `generer-scenarios.js` | Réponses attendues écrites à la main (tableaux `ARCHETYPES`, `FLEX`, `HORS_ROLE`, `FLEX_REVELE_SEUL`…), générateur à graine fixe, séparation développement / évaluation |
| `generer.html` | Lance le générateur **sans aucun fichier du moteur** : seule la liste des fiches (`draft-champions.js`) est chargée |
| `scenarios-dev.json` | 312 scénarios de développement (252 d'origine + 60 « flex révélé seul ») |
| `scenarios-eval.json` | 108 scénarios d'évaluation, **gelés** le 2026-11-10 |
| `schema-scenarios.json` | Format des scénarios |
| `manifeste.json` | Graines, comptes, empreintes SHA-256, instantané de référence, lancements du jeu gelé |
| `banc-evaluation.js`, `banc.html` | Métriques, invariances, monotonie, sensibilité aux poids, comparaison d'instantanés |
| `banc-etudes.js` | Études de décision (règle `peel`, critère `map`), variantes calculées en mémoire |
| `instantanes/instantane-moteur-2.2-main-78519fb.json` | Sorties du moteur 2.16.1 sur les 420 scénarios (pas une vérité) |

`tools/run-engine-eval.ps1` copie ces fichiers et ceux du moteur dans un dossier de travail (par défaut `dist-engine-eval`, ignoré par git), puis lance Chrome headless. Aucun appel réseau, aucune clé, et aucune lecture de `%LOCALAPPDATA%\DraftMid` : seules les données livrées dans `web/` servent.

### Ce que le script vérifie

1. **Sans navigateur** : empreintes SHA-256 des deux jeux conformes au manifeste (texte en fins de ligne LF, pour ne pas dépendre de `core.autocrlf`), champ `synthetique` partout, aucun identifiant commun et aucune draft identique entre les deux jeux (mêmes adversaires, même pool, même rôle), et aucun appel du générateur au moteur (`analyze`, `roleBelief`, `opponentBelief`, `RULES`, `CRITERIA`, `ROSTER`…).
2. **Avec `-Generer`** : le générateur, lancé sans le moteur, reproduit le jeu gelé à l'identique.
3. **Banc** : déterminisme, invariance à l'ordre du pool et des adversaires, exclusion d'un champion hors pool, favori hors égalité, égalités attendues, picks pris absents, monotonie, remplacement par un champion de même archétype, sensibilité de chaque poids à ±25 % (en mémoire), rapport de différences avec l'instantané.

**Code de sortie 1** si une empreinte ou la séparation des jeux n'est pas conforme, si une invariance casse, si une égalité attendue ou un pick pris n'est pas respecté, ou si un changement **INATTENDU** apparaît par rapport à l'instantané. Les violations de monotonie et le remplacement même archétype sont des mesures : ils ne font pas échouer le banc.

### Commandes

```powershell
.\tools\run-engine-eval.ps1                          # développement contre l'instantané de référence
.\tools\run-engine-eval.ps1 -Criteres front,map      # la PR déclare modifier front et map
.\tools\run-engine-eval.ps1 -Generer                 # en plus : régénération sans le moteur
.\tools\run-engine-eval.ps1 -Etudes                  # en plus : études peel et map (docs/moteur/ETUDE-PEEL-MAP.md)
.\tools\run-engine-eval.ps1 -AvecEvaluation          # jalon de version seulement : ajoute le jeu gelé
.\tools\run-engine-eval.ps1 -NouvelInstantane tests\moteur\instantanes\instantane-moteur-<version>-<commit>.json
```

La sortie donne un résumé lisible. Le détail complet est écrit en JSON dans le dossier de travail (`resultats-*.json`, `instantane-*.json`).

### Règles d'usage

- Tout réglage du moteur (poids, seuils, fiches, rôles) se justifie **uniquement** avec le jeu de développement.
- Le jeu d'évaluation gelé se lance seulement à un jalon de version, avec `-AvecEvaluation`. Chaque lancement est noté dans `manifeste.json` (`evaluation_gele.lancements`). Si un de ses chiffres sert à décider un réglage, il est « usé » : on en génère un nouveau avec une autre graine, et on le note.
- On n'ajoute jamais un scénario d'évaluation au jeu de développement. Les nouveaux cas vont **seulement** dans le développement, avec un générateur à part, comme `flex_revele_seul` (graine 2172). Ensuite, on met à jour le compte et l'empreinte dans le manifeste ; le script affiche l'empreinte trouvée.
- Les réponses attendues sont écrites à la main ou viennent de règles structurelles (deux fiches identiques sont ex æquo, un pick pris est absent). Elles ne viennent jamais d'une sortie du moteur.

### Pour une PR qui touche le moteur

Toute PR qui modifie `web/draft-engine.js`, `draft-data.js`, `draft-champions.js` ou `draft-role-sheet.js` :

1. **Déclare ses critères modifiés** dans la description, avec les clés de `CRITERIA` : `matchup`, `blind`, `targets`, `front`, `dive`, `control`, `range`, `peel`, `map`. Une PR qui ne doit rien changer au classement (textes, explications, interface) n'en déclare aucun.
2. **Lance le banc** avec cette déclaration : `.\tools\run-engine-eval.ps1 -Criteres <clés>`.
3. **Joint le rapport de différences** (ligne « Contre l'instantané » et ses exemples). Elle explique chaque changement du 1er choix. Un changement est « attendu » si seuls les critères déclarés ont bougé, sans changement de rôles ni d'adversaire direct. Tout autre changement est « INATTENDU » et fait échouer le banc.
4. **Si les changements sont acceptés**, elle enregistre un nouvel instantané sous un nouveau nom (`-NouvelInstantane`), sans écraser l'ancien, et met à jour `instantane_reference` dans le manifeste. Le banc (et la CI) repasse alors au vert contre ce nouvel instantané. Le rapport joint reste celui contre l'ancien.
5. **Si elle touche aux rôles** (fiche ou calcul), aucun critère ne peut rendre les changements « attendus » : elle les liste un par un dans la description, puis passe par l'étape 4.

Modèle à copier dans la description de la PR :

```
Critères modifiés : <aucun | front, map…>
Banc (développement, scénarios SYNTHÉTIQUES) : <N> comparés, 1er choix changé <a>, ordre <b>, scores <c>, adversaire <d>, rôles <e> ; attendus <f>, INATTENDUS 0
Changements du 1er choix : <id : ancien -> nouveau, raison>
Nouvel instantané : <fichier, ou « aucun »>
```

### CI

Le job `windows` de `.github/workflows/build.yml` lance `run-engine-eval.ps1 -Generer` sur le jeu de développement, contre l'instantané de référence. Le jeu gelé n'y est jamais lancé. Le banc a besoin de Chrome. Chrome fait partie de l'image `windows-latest` de GitHub, mais rien ne garantit qu'il y restera : s'il manque, l'étape échoue avec « Chrome introuvable ».

## Vérifications manuelles

Ne rien fermer pendant une partie. Noter ce qui diffère de l'attendu.

**A. Parcours complet, client à 1600×900**
1. Lancer DRAFT, puis le client : rien ne doit apparaître. Le réglage doit afficher « Connecté au client LoL ».
2. Lobby, file, acceptation : rien ne doit apparaître.
3. Sélection : les panneaux apparaissent, sans prendre le focus. Taper au clavier doit toujours aller dans LoL.
4. Déplacer le client pendant la sélection : observer la traînée, sans et avec le second écran.
5. Attendre que les 5 adversaires soient visibles : composition juste, mid cohérent, ou « Mid non identifié » si c'est ambigu.
6. Vérifier qu'aucun bouton de LoL n'est couvert, et que les clics fonctionnent.
7. Lancer la partie : les panneaux disparaissent. Pendant la partie : rien.
8. Fin de partie, retour au client, nouvelle file : rien jusqu'à la sélection suivante.

**B. Draft annulée.** Quitter la sélection ou laisser quelqu'un esquiver : les panneaux disparaissent, sans clignoter, et reviennent à la sélection suivante, composition vide.

**C. Autre application.** Pendant une sélection, ouvrir un navigateur en plein écran, puis revenir sur LoL par Alt+Tab : les panneaux doivent repasser devant le navigateur, et jamais devant lui tant qu'il est actif.

**D. Sans place autour du client.** Sur un seul écran 1080p, client en 1600×900 : un seul panneau étroit à droite, qui couvre une partie du bord du client. **À juger : couvre-t-il quelque chose d'utile ?**

**E. Échelles.** Pour 100 %, 125 %, 150 % et 175 % : relancer DRAFT, ouvrir la sélection, vérifier textes et portraits.

**F. Deux écrans à échelles différentes.** Faire passer le client d'un écran à l'autre : les panneaux suivent et prennent la bonne taille.

**G. Raccourci.** Ctrl+Alt+D affiche et masque les panneaux, y compris hors sélection. L'icône près de l'horloge donne le même résultat.

**H. PC sans environnement de développement.** Télécharger seulement l'exécutable sur l'autre PC, le lancer : les réglages s'ouvrent au premier lancement, le profil se choisit, et reste après relance.
