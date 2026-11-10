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
