# Backlog de QA, DRAFT 3.0.0-beta.1

Points à vérifier ou corriger pendant la QA finale. La direction artistique B+ est figée : seules des corrections ciblées, sans refonte.

| N° | Point | Origine | Critère de sortie |
|---|---|---|---|
| QA-1 | Couleur des barres statistiques sous 50 % (la barre de victoires reste verte) | Audit B+ | Sous 50 %, la barre n'utilise plus le vert des réussites ; sous le seuil de 10 parties, toujours aucun pourcentage |
| QA-2 | Panneau compact à 125 % : la menace passe sous le pli | Finition B+ | La menace est visible sans défilement dans le panneau compact à 125 %, ou accessible en un geste évident |
| QA-3 | Confort des boutons de l'overlay | Utilisateur | Cibles d'au moins 32 px (déjà testé) ; vérifier l'espacement, le survol et le focus, et les tester à 100 %, 125 % et 150 % |
| QA-4 | Validation multi-écrans | e2e « NON TESTE : deux écrans, un seul présent » | Test sur un vrai PC à deux écrans (placement, retour du client, échelles différentes), ou procédure manuelle documentée et réalisée |
| QA-5 | Ancien affichage des tableaux de l'onglet Historique Riot | Audit B+ | Mêmes règles que Mes statistiques : période, échantillon, pas de pourcentage sous le seuil |
| QA-6 | Raccourci Ctrl+Alt+D (`--e2e` : « Raccourci Ctrl+Alt+D enregistré » en échec) | QA | Voir ci-dessous : toujours rapporté, jamais masqué |

## QA-6 : distinguer un conflit d'un vrai défaut du raccourci
- **Constat du 12/11/2026, sur le PC de développement :** `RegisterHotKey(Ctrl+Alt+D)` échoue avec l'erreur Windows **1409** (`ERROR_HOTKEY_ALREADY_REGISTERED`). Le seul processus DRAFT présent est l'installation principale de l'utilisateur, `DraftMid.exe` (2.14), qui tient le raccourci. **C'est un conflit avec une autre instance, pas un défaut de DRAFT 3.0.**
- **Ce qui montre le bon fonctionnement :** avec `--no-hotkey`, l'e2e vérifie qu'aucune tentative n'est faite (« Raccourci laissé à l'autre version ») et passe.
- **Ce qui reste à prouver :** l'enregistrement réel sur une session sans autre DRAFT, par exemple un compte Windows de test, une machine virtuelle ou le PC de l'ami, ou après avoir quitté l'installation principale avec l'accord de l'utilisateur.
- **Règle des rapports :** l'échec est **toujours affiché**, avec sa cause mesurée (code 1409 et processus détenteur). Il n'est jamais retiré des résultats.
