# Opération finalisation : état initial et plan d'exécution (11 novembre 2026)

## État initial (phase 0)
| Élément | État |
|---|---|
| `origin/main` | `915d2eb` = tag `v2.16.2`. CI au vert (build et CodeQL) |
| Release v2.16.2 | Publiée, « Latest », signée (`DRAFT-Portable.exe`, `.sig`, `DRAFT-Setup.exe`) |
| PR #11, #12, #13 | Fusionnées |
| PR #6 à #10 | Ouvertes, **inchangées** depuis les tests (`a78abda`, `fed6f7f`, `a5e2cb2`, `599d9df`, `08cfd1f`) |
| PR #5 | Ouverte (résultats de la validation 2.16.1, documentation seulement) |
| Agents | Aucun en cours |
| Worktrees | Tous propres, sauf l'ancien `draftmid-app` (6 modifications, hors projet actuel, non touché) |
| Sécurité 2.16.1 | CSP, `DataScripts`, `IsAppPage`, liens limités, pas de redirection pour les identifiants, signature RSA des mises à jour : toutes vérifiées par `--selftest` et `--e2e` |

Rapports de base, à ne pas refaire :
- **Branche `etude/moteur-v217` :** les rapports du moteur, la synthèse et `PLAN-FUSION-V217.md`.
- **Branche `etude/ux-i18n` :** `docs/ux/*`, avec 21 captures de la vraie WebView2.
- **PR #9 :** `GLOSSAIRE-LOL.md`, `AUDIT-VOCABULAIRE.md`, `MESSAGES-A-CORRIGER.md`.
- **Sécurité :** `docs/security/AUDIT-SECURITE-216.md`.

## Phase 1 : intégration V2.17 (préparée, en attente de validation)
- **Branche :** `integration/v2.17` (poussée, sans PR).
- **Construction :** `main` puis #8, #7, #6, #9 et #10, fusionnées dans cet ordre, plus un commit qui ajoute au catalogue des messages les textes de la 2.16.2.
- **Conflit :** un seul, documentaire (`VALIDATION.md`, entre #8 et #7), résolu.
- **Résultats :** voir le rapport de validation, dans la réponse et en commentaire des PR.

**Fusion sur `main` après ton accord, PR par PR**, pour que chacune se ferme proprement. Pour #7, reporter la même résolution de conflit. Pour #10, repointer sa base sur `main` après #9. Le résultat doit être identique à `integration/v2.17` (contrôle par empreinte de l'arbre).

## Phases 2 à 6 : organisation

### Règle de partage des fichiers
Les fichiers d'interface `web/index.html`, `draft-coaching.js`, `draft-mastery.js`, `draft-data.js` et `src/DraftMid.cs` sont touchés par presque tout. **Une seule PR de production à la fois** les modifie. Les autres agents ne produisent que des rapports, outils, tests isolés ou maquettes, en attendant leur tour.

### Séquence des PR de production (une branche par PR, chacune basée sur la précédente une fois validée)
| # | PR | Contenu | Fichiers principaux | Garde-fous |
|---|---|---|---|---|
| 1 | **E**, rôles affichés | Rôles tirés de `roleBelief` (répartition la plus probable), seuil d'affirmation selon les champions révélés, flex, colonne `roles_predits`, constats B-05, B-06, B-09 | `index.html` (`deriveSlots`, textes), `draft-data.js` (nouvelle fonction pure, sans changer les probabilités), `draft-coaching.js`, `DraftMid.cs` (en-tête du relevé) | Banc à 0 différence de classement ; relevé : B-05, B-06 et B-09 à 0 ; probabilités inchangées |
| 2 | **C-2**, vocabulaire | Entrées A-01 à A-30 du glossaire, menu natif | Textes de `index.html`, `draft-engine.js`, `draft-coaching.js`, `draft-profiles.js`, `DraftMid.cs` | Banc à 0 ; relevé avant et après ; aucun texte hors inventaire |
| 3 | **UX-0** | Repères `data-test` indépendants de la langue ; tests e2e sans texte | `index.html`, `DraftMid.cs` (tests) | Tous les tests verts, sans changement visible |
| 4 | **UX-1** | Comportements : bulle « DRAFT reste actif », retour sur la mise à jour, Échap avec annulation, rôle préservé quand on modifie un pool, champ de recherche dans la bonne fenêtre | `index.html`, `DraftMid.cs` | Nouveaux tests e2e par constat |
| 5 | **UX-2** | Simulateur dans son propre espace (déjà isolé depuis la 2.16.2) | `index.html` | Test P0 maintenu (0 perte sur 50) |
| 6 | **I18N-1** | Catalogue `web/i18n/fr.js`, chargé comme données JSON strictes et compatible CSP ; textes **inchangés** ; détecteur de textes en dur | Tous les textes ; `DataScripts` pour le catalogue | Relevé : **0 différence** de texte avant et après ; banc à 0 |
| 7 | **UX-3** | Navigation en 5 sections (Accueil, Mon pool, Simulateur, Mes statistiques, Réglages) | `index.html`, `DraftMid.cs` (`openFull` et onglets) | Anciennes adresses `?tab=` valables comme alias |
| 8 | **UX-4, UX-5, UX-6** | Mon pool ; Mes statistiques avec une seule clé Riot ; overlay et menu (8 entrées au plus) | Sections séparées | Une à la fois, chacune avec ses tests |
| 9 | **UX-7** | Harmonisation visuelle : échelle typographique, espacements, composants, états, contraste, DPI | CSS de `index.html` | Tests de mise en page sur 29 tailles ; captures avant et après |
| 10 | **UX-8** | Accueil et premier lancement | `index.html` | Parcours A et B mesurés |
| 11 | **I18N-2 et I18N-3** | `en.js`, sélecteur de langue, langue de Windows au premier lancement, préférence dans `settings.json`, textes natifs (menu, boîtes de dialogue, diagnostic) | Catalogues, `DraftMid.cs`, `LcuConnector.cs` | Clés identiques en FR et EN, repli sur le français, pseudo-langue pour les débordements |
| 12 | **QA et bêta** | Parcours A à H en FR et en EN ; 1366×768, 1920×1080, 2560×1440 ; DPI 100 à 175 % ; sécurité ; packaging ; mise à jour depuis la 2.16.2 | — | Rapport de candidate |

### Agents (au plus 3 en même temps, avec des périmètres disjoints)
- **Agent moteur :** PR E, puis C-2 (production, l'une après l'autre).
- **Agent UX, en parallèle de E et C-2, sans toucher la production :** maquettes haute fidélité des 5 sections et de l'overlay dans `docs/ux/maquettes/` (méthode frontend-design), et spécification des composants (UX-7). Ensuite, PR UX-0 à UX-8 dans l'ordre, une seule active à la fois.
- **Agent i18n, en parallèle, sans toucher la production :**
  - format du catalogue et chargement compatible CSP ;
  - détecteur de textes en dur (`tools/`) ;
  - glossaire anglais (termes LoL : dive, peel, poke, engage, matchup…) ;
  - plan des textes natifs.

  Ensuite I18N-1, à sa place dans la séquence, puis I18N-2 et I18N-3.
- **Agent QA et sécurité :** harnais d'écrans et de DPI dans `tools/`, liste de contrôle de la candidate, puis validation finale.

**Coût :** chaque agent consomme 300 000 à 600 000 tokens. La limite de 5 heures a déjà été atteinte plusieurs fois. Les phases seront découpées en sessions, et les agents reprendront là où ils se sont arrêtés.

## Numéro de version proposé
Une seule version publique pour tout le périmètre. Je propose la **3.0.0 bêta** : nouvelle navigation, interface bilingue et changements de vocabulaire visibles partout. À défaut, la **2.17.0 bêta**. La mise à jour depuis la 2.16.2 reste automatique dans les deux cas. **À toi de décider.**
