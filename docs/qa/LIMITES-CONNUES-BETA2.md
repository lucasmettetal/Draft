# Limites connues : candidate beta.2

Elles concernent la branche `integration/beta2-candidate` et la candidate de test 3.0.0-beta.2-rc.1 (intégration 05a3e90), puis la candidate 3.0.0-beta.2-rc.2 (intégration f6705e5 : + Mes statistiques #35, écran de fin de partie). Elles restent documentées jusqu'à la publication. Une limite corrigée est marquée comme telle, jamais effacée.

## Tests

| Limite | Constat | Statut |
|---|---|---|
| `web/draft-tests.html` ne rend aucun résultat (« erreur dans la page ou délai dépassé ») | Même échec sur `main` (c1d7623), sur la branche V2 et sur la #40 : **préexistant**, pas une régression de la beta.2 | Non traité. Test historique, à réparer ou à retirer avec l'accord de l'utilisateur |
| `web/draft-v215-tests.html` : 6 assertions sur 145 échouent | Elles décrivent l'ancien panneau (badge `.p-status`, `.coach-line` au premier niveau, `openDetail("compare")`). Passent sur `main`, échouent depuis la V2 (#41). En V2, l'égalité reste affichée au premier niveau (`TieGroup`) | **Corrigé** (6046e41) : assertions mises à jour pour la V2, mêmes vérifications aux nouveaux emplacements. 145/145 sur l'intégration 525c408 |
| e2e : « Ctrl+Alt+D non enregistré » | Le raccourci est laissé volontairement à une autre DRAFT ouverte (raison « instance ») | Comportement voulu |
| Relevé des messages FR : plantait (« clés avec une casse différente ») | Le relevé contient « Origine inconnue » (#35) et « origine inconnue » ; la lecture JSON de PowerShell les confondait | **Corrigé** (outil seulement) : lecture sensible à la casse. FR : 0 clé absente, 0 constat |
| Inventaire des messages (QA) : gabarits hors inventaire | FR : 141 en rc.2 (38 en rc.1) : nouveaux textes de Mes statistiques, de l'écran de fin de partie et des bans conseillés. EN : 1 988 (1 901 en rc.1), car l'inventaire est rédigé en français | À compléter (web/draft-messages-tests.html) ; pas un défaut visible |
| Pseudo-langue (textes allongés artificiellement) : 421 débordements | Banc de contrainte, pas un défaut réel. FR et EN : tout est bon. V2 seule : 427 | Informatif |

## Fonctions

| Limite | Détail | Statut |
|---|---|---|
| Bans et picks alliés | Format vérifié seulement en simulation, sur une session au format réel reconstitué | À confirmer en vraie draft (essai de l'utilisateur) |
| Bans conseillés | Peuvent citer un champion déjà banni ou pris | **Corrigé** (a90b2d1) : un ban déjà fait ou un champion pris cède la place au suivant ; sans lecture du client, bans identiques à avant |
| Panneau compact fermé seul | Revient au bout de 2 s | **Corrigé** (821ca2d) : fermé à la main, il reste fermé pendant la session ; « Afficher les panneaux » le rouvre. Sans fermeture volontaire, une nouvelle draft l'affiche |
| Mes statistiques (#35) | Les entrées `origin: "lcu"` et `abandoned` n'étaient pas prises en compte | **Corrigé** (96500d2) et **intégré en rc.2** (4bf2f63), statut visible par entrée |
| Marquage manuel des indisponibles | N'existe pas : sans lecture du client, rien n'est marqué | Prévu plus tard |
| Explication d'égalité entre styles différents | Reste générique (Cho'Gath, Master Yi, Viego) | À reprendre, hors beta.2 |
| Résultat de la partie | Saisi par le joueur. La lecture du résultat (PR C) et le rapprochement avec l'historique Riot (PR D) viendront plus tard | Prévu |
