# Test réel de Ctrl+Alt+D (préversion 3.0.0-beta.1)

À exécuter par l'utilisateur. Statut : **non exécuté**. But : prouver le vrai Ctrl+Alt+D, pressé au clavier, **sans arrêter ni modifier l'installation principale** (`%USERPROFILE%\Desktop\DraftMid.exe`, 2.14).

## Pourquoi ce test

- Sur le PC de développement, une DRAFT de l'utilisateur est toujours ouverte (l'installation principale, ou une autre version qu'il a lancée lui-même) et tient Ctrl+Alt+D. Une autre copie de DRAFT reçoit l'erreur Windows 1409 (raccourci déjà pris).
- La préversion ne tente même pas Ctrl+Alt+D quand une autre DRAFT tourne dans la session, ou quand la 2.x démarre avec Windows : elle le **laisse** à la 2.x. L'e2e le montre : « laissé volontairement à la 2.x (raison « instance ») ; aucune tentative, donc pas de conflit 1409 ».
- Prouvé par les tests automatiques : le mécanisme sur une autre combinaison (Ctrl+Alt+Maj+F11 : enregistrement, réception, bascule, conflit 1409 signalé), et l'activation explicite depuis Réglages (vraie tentative, conflit 1409 affiché).
- Reste à prouver : Ctrl+Alt+D lui-même, au clavier, dans une session où rien d'autre ne le tient.

Les raccourcis Windows sont propres à chaque session. Une seconde session Windows (autre compte) a donc son propre Ctrl+Alt+D, libre, même si l'installation principale tourne dans la tienne.

## Préparation commune

- [ ] Fichier : la candidate `DRAFT-Portable.exe` (dossier `Downloads\DRAFT-3.0.0-beta.1-candidate`), empreinte vérifiée avec `SHA256.txt`.
- [ ] Noter : version de Windows, échelle d'affichage (Paramètres > Affichage), clavier AZERTY ou QWERTY.

## Option A (recommandée) : une seconde session Windows

L'installation principale reste ouverte dans ta session. Rien n'y est touché.

1. [ ] **Créer un compte local de test, toi-même** : Paramètres > Comptes > Famille et autres utilisateurs > Ajouter un autre utilisateur > « Je ne dispose pas des informations de connexion de cette personne » > « Ajouter un utilisateur sans compte Microsoft ». Nom : `Test-DRAFT`.
2. [ ] **Changer d'utilisateur sans fermer ta session** : Ctrl+Alt+Suppr > Changer d'utilisateur > `Test-DRAFT`.
3. [ ] Dans la session `Test-DRAFT`, copier `DRAFT-Portable.exe` dans `C:\Users\Test-DRAFT\DRAFT-beta\`.
4. [ ] Gestionnaire des tâches > Détails : aucun `DRAFT*.exe` ni `DraftMid*.exe` **pour l'utilisateur `Test-DRAFT`** (ceux de ton compte ne comptent pas).
5. [ ] Lancer `DRAFT-Portable.exe`. Attendu : icône DRAFT près de l'horloge ; l'Accueil s'ouvre ; carte « Préversion de test » : « Dossier de données séparé » (ce compte n'a pas de 2.x).

| N° | Action | Résultat attendu | Noté |
|---|---|---|---|
| A1 | Clic droit sur l'icône DRAFT | Entrée « Afficher les panneaux (Ctrl+Alt+D) » | |
| A2 | Réglages > Affichage et fenêtres | « Ctrl+Alt+D affiche ou masque les panneaux à tout moment. » ; ni « laissé à… », ni « déjà utilisé » ; pas de case « Utiliser Ctrl+Alt+D » | |
| A3 | Ctrl+Alt+D, LoL fermé | Les panneaux apparaissent | |
| A4 | Ctrl+Alt+D de nouveau | Les panneaux disparaissent ; bulle « Panneaux masqués » la première fois | |
| A5 | Maintenir Ctrl+Alt+D deux secondes | Une seule bascule (pas de répétition) | |
| A6 | Réglages > Général > Langue > English, puis A1 et A2 | « Show panels (Ctrl+Alt+D) » ; « Ctrl+Alt+D shows or hides the panels at any time. » | |
| A7 | Seconde copie : `DRAFT-Portable.exe --data C:\Users\Test-DRAFT\DRAFT-beta\donnees2` | La seconde copie laisse le raccourci à la première : Réglages > Affichage et fenêtres dit « Ctrl+Alt+D est laissé à l'autre version de DRAFT ouverte sur ce PC » ; la première garde Ctrl+Alt+D (A3 fonctionne toujours) | |
| A8 | Dans la seconde copie, cocher « Utiliser Ctrl+Alt+D dans cette préversion » | Message « Ctrl+Alt+D est déjà utilisé par un autre programme… » (conflit 1409) ; la première copie garde Ctrl+Alt+D | |
| A9 | Quitter les deux copies (menu > Quitter) | Plus aucun `DRAFT-Portable.exe` pour `Test-DRAFT` | |

Si LoL est installé et que tu veux tester en sélection : se connecter au client dans la session `Test-DRAFT` et faire A3 et A4 pendant la sélection (les panneaux ne prennent pas le focus du client).

Nettoyage : se déconnecter de `Test-DRAFT`, revenir dans ta session (rien n'y a changé), puis, si tu veux, supprimer le compte (Paramètres > Comptes > Famille et autres utilisateurs > Supprimer).

## Option B : le PC d'un testeur

À faire par le testeur, avec la préversion téléchargée.

| Cas | Préparation | Attendu |
|---|---|---|
| B1. Pas de 2.x | Aucune 2.x installée ni ouverte | Comme A1 à A6 |
| B2. 2.x ouverte | La 2.x tourne | Menu : « Afficher les panneaux » sans raccourci ; Réglages > Affichage et fenêtres : « Ctrl+Alt+D est laissé à l'autre version de DRAFT ouverte sur ce PC » ; la 2.x garde Ctrl+Alt+D (il fonctionne chez elle) |
| B3. 2.x ouverte, case cochée | Dans la préversion, cocher « Utiliser Ctrl+Alt+D dans cette préversion » | « Ctrl+Alt+D est déjà utilisé par un autre programme… » ; la 2.x garde Ctrl+Alt+D |
| B4. 2.x fermée, mais elle démarre avec Windows | Quitter la 2.x (menu > Quitter), relancer la préversion | « Ctrl+Alt+D est laissé à DRAFT 2.x, qui démarre avec Windows sur ce PC » ; cocher la case : la préversion prend Ctrl+Alt+D, A3 et A4 fonctionnent |
| B5. Limite connue | Après B4 (préversion avec Ctrl+Alt+D), lancer la 2.x à la main | La 2.x n'a pas Ctrl+Alt+D tant que la préversion tourne. Quitter la préversion, puis relancer la 2.x : elle le reprend |

## Option C : sur ce PC, en fermant toi-même l'installation principale

**Seulement avec ton accord explicite, et par toi.** Aucun agent ne ferme l'installation principale.

1. [ ] Quitter l'installation principale : clic droit sur son icône > Quitter. Vérifier dans le Gestionnaire des tâches qu'aucun `DraftMid.exe` ne tourne.
2. [ ] Lancer la candidate. Si l'installation principale démarre avec Windows, Réglages > Affichage et fenêtres dit « laissé à DRAFT 2.x, qui démarre avec Windows » : cocher « Utiliser Ctrl+Alt+D dans cette préversion ». Sinon, la préversion prend Ctrl+Alt+D toute seule.
3. [ ] Faire A1 à A6.
4. [ ] Décocher la case (si cochée), puis quitter la candidate (menu > Quitter).
5. [ ] Relancer `DraftMid.exe` depuis le Bureau. Vérifier qu'il a de nouveau Ctrl+Alt+D (ses panneaux s'affichent au raccourci).

## À noter pour chaque cas

- OK ou défaut, étape par étape.
- Le texte exact de Réglages > Affichage et fenêtres sous « Afficher les panneaux automatiquement ».
- Le libellé de l'entrée du menu (« Afficher les panneaux » avec ou sans « (Ctrl+Alt+D) »).
- En cas d'échec : Réglages > Diagnostic et informations > version, et `%LOCALAPPDATA%\DRAFT-beta\draftmid.log` (session de test).
- Pas de capture avec un pseudo ou une partie en cours visible.
