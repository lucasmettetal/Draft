# Test d'une vraie draft (préversion 3.0.0-beta.1)

À exécuter par l'utilisateur ou un testeur, sur une vraie sélection des champions. Statut : **non exécuté**. Les tests automatiques utilisent un client LoL simulé : seul un vrai client prouve la détection, le rôle attribué et les panneaux en situation.

## Avant de commencer

- [ ] **Panneaux en double.** Si une 2.x tourne, ses panneaux apparaissent aussi pendant la draft. Pour l'éviter **sans la modifier** : clic droit sur son icône > **Quitter** avant le test, et la relancer après. Ne change pas ses réglages.
- [ ] Lancer la préversion. Réglages > Diagnostic et informations : « DRAFT 3.0.0-beta.1, préversion de test ».
- [ ] Mon pool : au moins deux rôles avec des champions (par exemple mid et support).
- [ ] Choisir le mode de détection à tester :
  - **Lecture du client** : Réglages > Connexion League > cocher « Lire la draft depuis le client LoL » (lecture seule ; Riot n'a pas confirmé que c'est autorisé). « Quand LoL t'attribue un autre rôle » : d'abord « Me proposer de changer » (par défaut), puis refaire avec « Changer de profil ».
  - **Écran** : lecture du client décochée. DRAFT reconnaît la sélection à l'image.
- [ ] Noter : version de Windows, échelle d'affichage, résolution, taille de la fenêtre du client, file (normale, classée).

## Déroulé

| N° | Moment | Ce qu'il faut faire | Résultat attendu | Noté |
|---|---|---|---|---|
| 1 | Client ouvert, accueil ou lobby | Rien | Aucun panneau. Accueil de DRAFT : « Connexion League » connectée (lecture du client) ou en attente (écran) | |
| 2 | Entrée en sélection | Rien | Les panneaux apparaissent, collés au client, sans le couvrir. Lecture du client : en 1 à 2 s. Écran : en quelques secondes. Premier affichage de la session : jusqu'à environ 5 s (m3) | |
| 3 | Rôle attribué par League | Regarder l'en-tête des panneaux | Lecture du client : le rôle attribué est affiché ; avec « Changer de profil », le pool de ce rôle est utilisé ; avec « Me proposer de changer », DRAFT le propose sans changer. Écran : ton rôle choisi reste | |
| 4 | Bascule de rôle | Cliquer une autre icône de rôle | Le conseil change pour le pool de ce rôle ; les adversaires restent | |
| 5 | Adversaires | Lecture du client : attendre leurs verrouillages. Écran : les saisir avec la recherche | Chaque adversaire verrouillé apparaît à gauche ; le conseil se met à jour ; aucune frappe perdue dans la recherche | |
| 6 | Recommandation | Lire le conseil et ses raisons | Un champion de ton pool, des raisons qui citent la composition adverse, ton vis-à-vis supposé corrigeable d'un clic | |
| 7 | « Je joue » | Cliquer « Je joue {champion} » | Le bouton passe à l'état noté | |
| 8 | Lancement de la partie | Laisser la partie démarrer | Les panneaux disparaissent dès le chargement | |
| 9 | Fin de partie | Revenir au client | Mes statistiques > journal : une ligne pour cette draft (champion joué, conseil) | |
| 10 | Dodge | Sur une autre sélection, quitter (ou un joueur quitte) | Les panneaux disparaissent dès le retour au lobby | |
| 11 | Draft suivante | Nouvelle sélection | Panneaux propres : seulement les adversaires de la nouvelle draft ; ton choix manuel de vis-à-vis et le rôle de la draft précédente sont effacés | |
| 12 | Raccourci et menu | Ctrl+Alt+D, ou menu de l'icône > Afficher les panneaux | Les panneaux se masquent et se réaffichent. Si une 2.x tourne ou démarre avec Windows, Ctrl+Alt+D lui est laissé : utiliser le menu | |

## Deux langues

- [ ] Faire le déroulé en **français**.
- [ ] Réglages > Général > Langue > **English**. Refaire au moins 2, 3, 5, 6, 7, 8 et 11. Tous les textes des panneaux, du menu et des bulles sont en anglais.
- [ ] Changer de langue **pendant** une draft : les panneaux gardent leur langue jusqu'à la fin, la draft suivante est dans la nouvelle langue.

## En cas de problème

- Lecture du client : Réglages > Diagnostic et informations > **Diagnostic de connexion LoL**, copier le texte (ni mot de passe, ni port, ni chemin, ni pseudo).
- Détection par l'écran : cocher « Journal de diagnostic de la détection (chiffres seulement) », refaire une draft, envoyer `%LOCALAPPDATA%\DRAFT-beta\diagnostic.log`.
- Plantage : `%LOCALAPPDATA%\DRAFT-beta\draftmid.log`.
- Captures : flouter pseudos et partie en cours.

## Résultat à rapporter

Pour chaque ligne : OK ou défaut, la langue, le mode (client ou écran), et ce qui a été vu. Relancer la 2.x après le test si elle a été quittée.
