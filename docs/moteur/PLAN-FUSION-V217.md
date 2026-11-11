# Plan de fusion V2.17 (11 novembre 2026)

**Préalable :** la candidate 2.16.2 (#13 = `main` + #12 + #11) est fusionnée et publiée, avec ton accord. Les PR #12 et #11 se ferment alors d'elles-mêmes : leurs commits sont dans `main`. **Aucune fusion ne se fait sans ton accord.**

## Ordre et dépendances

| Étape | PR | Base | Dépend de | Fichiers sensibles | Avant de fusionner |
|---|---|---|---|---|---|
| 1 | **#8** banc d'évaluation | `main` | — | `.github/workflows/build.yml` (étape du banc), `docs/technique/VALIDATION.md` | Mettre à jour la branche sur `main` (2.16.2) ; CI verte, avec l'étape « Banc d'évaluation du moteur » à « Banc : OK » |
| 2 | **#7** mesure des rôles | `main` | #8 (conflit de documentation) | `docs/technique/VALIDATION.md`, `tools/run-accuracy-tests.ps1` | Rebaser sur `main`. Dans le conflit `VALIDATION.md`, garder la ligne `run-accuracy-tests` de #7 et toute la section « Banc » de #8. Puis `run-role-tests.ps1` (75/0) et `run-accuracy-tests.ps1` |
| 3 | **#6** protocole de drafts réelles | `main` | — | `tools/validation-report.ps1`, docs | Mettre à jour sur `main`, CI verte |
| 4 | **#9** audit vocabulaire | `main` | — | `tools/extract-ui-messages.ps1`, `web/draft-messages-tests.html` | Mettre à jour sur `main`. **Ajouter au catalogue** deux textes de la 2.16.2 : le bandeau « Simulation : ta draft en cours n'est pas modifiée » et le menu « Diagnostic de connexion LoL... ». Relevé sans texte hors inventaire |
| 5 | **#10** explications (PR D) | `v217/vocabulaire-lol` → à repointer sur `main` | #9 | `web/index.html`, `draft-coaching.js`, `draft-engine.js` (textes), `draft-mastery.js` | Après #9 : changer la base de #10 en `main`, puis la mettre à jour (le diff ne doit plus montrer que les 9 fichiers de la PR D). Lancer tous les tests (ci-dessous) |
| — | **#5** validation 2.16.1 | `main` | — | `docs/security/VALIDATION-2161.md` | Indépendante, à tout moment |

**Pourquoi cet ordre :**
- **#8 d'abord :** le banc devient un garde-fou de CI avant toute PR qui touche `web/`.
- **#7 juste après #8 :** pour résoudre une seule fois le conflit de documentation.
- **#9 avant #10 :** la #10 est empilée sur la #9 et utilise son outil de relevé.
- **#6 :** indépendante, elle ne touche que des docs et un script de lecture.

## Tests à relancer après chaque fusion qui touche `web/` ou `src/` (#10 surtout)
- `build.ps1`, puis `--selftest` : tout doit être bon, y compris signature, mise à jour, CSP, LCU et diagnostic.
- `--e2e`, 2 passages : P0 à 0 perte sur 50, réglages 6 bis / 6 ter / 7 bis, sécurité de la page.
- `run-coaching-tests.ps1` (conseils, égalités, maîtrise, moteur 190/0), `run-v215-tests.ps1`, `run-layout-tests.ps1`.
- `run-engine-eval.ps1 -AvecEvaluation` : **0 différence** (aucune PR V2.17 ne change le classement).
- `run-role-tests.ps1`.
- `extract-ui-messages.ps1` : les constats corrigés par la #10 restent à 0 ; il ne reste que B-05, B-06 et B-09, pour la PR E.

**Déjà vérifié le 11 novembre**, sur un arbre local `main` + #12 + #11 + #8 + #7 + #6 + #9 + #10 :
- un seul conflit, celui de documentation entre #8 et #7 ;
- toutes les suites ci-dessus au vert ;
- `--selftest` 274 OK, `--e2e` 88 OK (2 sur 2).

## Ce que la fusion ne doit pas importer
- **#10 :** ne fusionner qu'après #9. Sinon, la PR emporte l'audit avec elle, ce qui reste correct mais sans revue séparée.
- **Branches d'étude :** aucune n'est à fusionner (`etude/moteur-v217`, `etude/ux-i18n`, `essai/integration-v217` en local).
- **Correctifs de sécurité 2.16.1 :** la CSP, `DataScripts`, `IsAppPage` et les redirections ne sont touchées par aucune de ces PR. Le `--selftest` et le e2e de sécurité les vérifient à chaque étape.

## Après la V2.17
1. **PR E :** rôles affichés tirés du calcul, colonne `roles_predits`, constats B-05, B-06 et B-09. Base : `main` après #10.
2. **PR C-2 :** vocabulaire, selon le glossaire. Elle intègre aussi les textes de la 2.16.2.
3. **Option B :**
   - UX-0, repères de test ;
   - UX-1, comportements : bulle « DRAFT reste actif », retour sur la mise à jour ;
   - UX-2 : le simulateur est déjà isolé par la 2.16.2, il reste à le déplacer dans sa section ;
   - I18N-1, extraction des textes français ;
   - UX-3 à UX-8, nouvelle navigation ;
   - I18N-2 à I18N-4, version anglaise.
