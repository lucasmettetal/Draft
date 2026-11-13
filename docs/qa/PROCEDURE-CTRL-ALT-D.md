# Procédure manuelle : raccourci Ctrl+Alt+D (QA-6)

Statut au 13/11/2026 : **non prouvé sur le PC de développement**. Cette procédure n'a pas été exécutée.

## Pourquoi un essai manuel

- Sur le PC de développement, `RegisterHotKey(Ctrl+Alt+D)` échoue avec l'erreur Windows **1409** (`ERROR_HOTKEY_ALREADY_REGISTERED`).
- La seule autre instance DRAFT de la session est l'installation principale de l'utilisateur, `%USERPROFILE%\Desktop\DraftMid.exe` (2.14). Elle tient le raccourci. L'e2e l'écrit dans sa ligne d'échec : « Win32 1409 : déjà pris par un autre programme ; autres instances DRAFT de la session : DraftMid.exe pid … ».
- Les raccourcis sont propres à la station Windows : un bureau caché ne contourne pas le conflit. Le même échec existe sur main.
- Ce qui est prouvé ici, par un test isolé (`--e2e`, ligne « QA-6. Mécanisme du raccourci ») : enregistrement réel d'une autre combinaison (Ctrl+Alt+Maj+F11), réception de `WM_HOTKEY`, bascule des panneaux, détection du conflit 1409 sans plantage, et message à l'utilisateur (menu sans raccourci annoncé, consigne dans Réglages > Affichage).
- Ce qui reste à prouver : le vrai Ctrl+Alt+D, pressé au clavier, sur une session sans autre DRAFT.

## Où le faire

Au choix, sans fermer l'installation principale sans l'accord de l'utilisateur :
1. un autre compte Windows (session séparée, sans DRAFT lancé) ;
2. une machine virtuelle Windows 10 ou 11 ;
3. le PC de l'ami testeur ;
4. le PC de développement, seulement après que l'utilisateur a quitté lui-même son installation (menu de l'icône > Quitter).

## Préparation

1. Copier `DRAFT-Portable.exe` de la bêta dans un dossier neuf, par exemple `C:\Essai-DRAFT\`.
2. Vérifier qu'aucun autre DRAFT ne tourne : Gestionnaire des tâches > Détails, aucun `DRAFT*.exe` ni `DraftMid*.exe`.
3. Lancer avec un dossier de données d'essai : `DRAFT-Portable.exe --data C:\Essai-DRAFT\donnees --run-key Software\DRAFT-Test-Raccourci\Run`.

## Étapes et résultats attendus

| N° | Action | Attendu |
|---|---|---|
| 1 | Clic droit sur l'icône DRAFT | L'entrée s'appelle « Afficher les panneaux (Ctrl+Alt+D) » |
| 2 | Réglages > Affichage et fenêtres | La consigne dit « Ctrl+Alt+D affiche ou masque les panneaux à tout moment. » ; aucune consigne « déjà utilisé » |
| 3 | Appuyer sur Ctrl+Alt+D, LoL fermé | Les panneaux apparaissent (affichage manuel) |
| 4 | Appuyer de nouveau sur Ctrl+Alt+D | Les panneaux disparaissent ; une bulle « Panneaux masqués » au premier masquage de la session |
| 5 | Maintenir Ctrl+Alt+D deux secondes | Une seule bascule (pas de répétition automatique) |
| 6 | Avec LoL ouvert, en sélection des champions | Ctrl+Alt+D masque puis réaffiche les panneaux, sans voler le focus du client |
| 7 | Lancer une seconde copie avec un autre dossier (`--data C:\Essai-DRAFT\donnees2`) | La seconde copie n'a pas le raccourci : son menu affiche « Afficher les panneaux » sans raccourci, et Réglages > Affichage explique que Ctrl+Alt+D est déjà utilisé par un autre programme |
| 8 | Quitter les deux copies | Plus aucun `DRAFT-Portable.exe` dans le Gestionnaire des tâches |

Passer en anglais (Réglages > Général > Langue > English) et refaire les étapes 1, 2 et 7 : textes « Show panels (Ctrl+Alt+D) » et « Ctrl+Alt+D is already used by another program… ».

## Nettoyage

- Supprimer `C:\Essai-DRAFT\`.
- Supprimer la clé `HKEY_CURRENT_USER\Software\DRAFT-Test-Raccourci` si elle existe.

## Résultat à rapporter

Pour chaque étape : OK ou défaut, avec la version de Windows, l'échelle d'affichage et la disposition du clavier (AZERTY ou QWERTY). En cas d'échec à l'étape 3, noter le texte de Réglages > Affichage (il indique si le raccourci est pris).
