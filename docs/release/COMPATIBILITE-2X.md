# Compatibilité des données entre la 3.0 et les 2.x

Mesuré le 13/11/2026, avec des données **fictives** uniquement. Aucune donnée réelle n'a été lue.

**Mise à jour du 16/11/2026 :** l'incompatibilité de la 2.14 décrite plus bas est corrigée par la #34, fusionnée dans main et dans la préversion. Règle complète : [REPRISE-POOLS-2X.md](../technique/REPRISE-POOLS-2X.md).

## Méthode

- Exécutables compilés depuis les sources : 3.0 stable = `main` 6218022 (édition stable, comme `DRAFT-3.0-essai.exe`), `v2.14.0`, `v2.16.2`, préversion 3.0.0-beta.1.
- Tout tourne sur un bureau Windows caché. Chaque version a sa **copie** du dossier (`--data`), et une clé de démarrage de test.
- La 3.0 stable écrit elle-même un dossier « écrit par la 3.0 », par sa page et son programme :
  - pools sur trois rôles (mid 4, support 3, top 2), avec exclusions et favori pour mid et support ;
  - journal de 3 drafts, dont 2 avec un résultat ;
  - niveau « avancé », rôle attribué en « Changer de profil », guide de l'Accueil commencé ;
  - réglages : langue anglaise, « Toujours au premier plan ».
- L'historique importé (6 parties) et les rôles calculés (4 champions) sont des fichiers fictifs au format exact de la 3.0. Les produire pour de vrai demande une clé Riot.
- Chaque version relit sa copie dans sa propre page : rôle, pools, exclusions, favori, journal, réglages, clés du stockage.
- Les empreintes SHA-256 de tous les fichiers sont relevées avant et après chaque passage.

## Ce que chaque 2.x voit

| Donnée écrite par la 3.0 | 2.14 | 2.16.2 |
|---|---|---|
| Rôle joué et son pool (mid, 4 champions) | Lu | Lu |
| Pools des autres rôles (support, top) | **Ignorés**, car la 2.14 n'a qu'un profil. Ils restent dans le stockage, intacts | Lus, avec leurs exclusions et leur favori |
| Exclusions et favori du rôle joué | Lus | Lus |
| Journal (3 drafts, 2 résultats) | Lu | Lu |
| Historique importé (6 parties) | **Ignoré** : fonction absente de la 2.14. Le fichier reste intact | Lu |
| Rôles calculés (format 2) | Lus (4 champions) | Lus |
| Niveau, rôle attribué automatique | Conservés dans les préférences | Conservés |
| Réglage « Toujours au premier plan » | Lu | Lu |
| Langue (`Language` dans `settings.json`) | **Effacée** à la réécriture de `settings.json` | **Effacée** |

## Ce que chaque 2.x réécrit

Comparaison des empreintes avant et après un lancement de la 2.x, panneaux ouverts :

- `settings.json` est réécrit, et seul le champ `Language` disparaît. Les autres champs sont gardés.
- Le stockage de la page change (journal `000003.log` de LevelDB), mais les valeurs relues ensuite par la 3.0 sont identiques.
- Les fichiers de cache de WebView2 changent : 58 fichiers pour la 2.14, 62 pour la 2.16.2.
- L'interface extraite `app\` est remplacée par celle de la version lancée. C'est un cache, recréé par la 3.0 à son prochain lancement.
- Aucun autre fichier de données ne change : profils, historique, rôles calculés et relevés restent identiques.

## Retour en 3.0 après une 2.x

**Sans modification dans la 2.x.** Tout revient à l'identique : trois rôles, exclusions, favoris, journal, historique, rôles calculés, réglages. Une seule perte : la langue revient à « automatique », c'est-à-dire la langue de Windows (défaut m2).

**Avec des modifications dans la 2.x.** Dans le test, la 2.x change le pool mid, enlève l'exclusion, change le favori et note une partie.

| Modification faite dans la 2.x | Après la 2.14, 3.0 avant la #34 | Après la 2.14, 3.0 avec la #34 | Après la 2.16.2 |
|---|---|---|---|
| Pool du rôle joué | **Non** : la 3.0 garde son ancien pool | Oui, ancien pool gardé, notice | Oui |
| Exclusions et favori | **Non** | Oui | Oui |
| Partie notée au journal | Oui | Oui | Oui |

**Incompatibilité (2.14 seulement), corrigée par la #34.**
- **Où :** la 2.14 enregistre son profil dans les clés `draftProfile` et `draftPrefs`. La 3.0 lit d'abord `draftProfiles` (fonction `loadProfiles` de `web/draft-profiles.js`), que la 2.14 ne connaît pas.
- **Effet :** ce qu'on change dans la 2.14 (pool, exclusions, favori) est ignoré par la 3.0 et par la préversion. À sa première sauvegarde, la 3.0 réécrit `draftProfile` avec son propre pool : la modification faite en 2.14 est alors perdue.
- **Non touchés :** le journal, l'historique et les réglages.
- **La 2.16.2 n'a pas ce défaut :** elle écrit le même format que la 3.0.
- **Correction (#34) :** la 3.0 garde une empreinte de ce qu'elle écrit dans draftProfile et draftPrefs. Si une 2.x y a écrit depuis, ses changements sont repris dans le rôle concerné, après une copie datée de l'ancien profil (draftLegacyBackups), avec une notice. **Première fois sans empreinte** (données d'une 3.0 plus ancienne, comme l'essai) : rien n'est repris, les pools 3.0 restent, une copie de l'ancienne clé est gardée, une notice le dit si le pool diffère, puis l'empreinte est posée. Détails : [REPRISE-POOLS-2X.md](../technique/REPRISE-POOLS-2X.md).

## Réponse : une 2.14 relancée sur des données écrites par la 3.0 est-elle sûre ?

**Oui pour la lecture.** Elle démarre et retrouve le rôle joué, son pool, ses exclusions, son favori, le journal et les réglages. Elle ne supprime rien : les autres rôles et l'historique restent sur le disque, intacts.

**Ce qui change :**
1. La langue choisie dans la 3.0 est effacée de `settings.json`.
2. Les autres rôles et l'historique importé ne sont pas visibles dans la 2.14.
3. Les changements de pool, d'exclusions ou de favori faits dans la 2.14 sont **repris** par une 3.0 ou une préversion qui contient la #34, l'ancien pool étant gardé. Exception : la première fois sur des données écrites sans empreinte, rien n'est repris (copie gardée, notice). Le journal est partagé.

Avant la #34, le conseil était de ne pas modifier les pools dans la 2.14. Ce n'est plus nécessaire avec une 3.0 à jour ; pour des données écrites par l'essai 3.0 (sans empreinte), un pool changé dans la 2.14 avant le premier lancement d'une 3.0 à jour n'est pas repris : refaire ce changement dans la 3.0, la copie gardée permet de le retrouver.

## Import par la préversion

La préversion copie les données avec `--legacy-data` (au premier lancement, le dossier 2.x par défaut). La source est toujours restée identique octet pour octet après la copie.

| Source | Fichiers copiés | Ce que la préversion relit |
|---|---|---|
| Dossier écrit par la 3.0 stable | 9, 0 sauté | Tout, identique à la vue de la 3.0 stable : 3 rôles, exclusions, favoris, journal 3, historique 6, rôles 4, niveau, rôle attribué. La langue anglaise s'applique dès le premier lancement (corrigé ici : avant, seulement au lancement suivant) |
| Même dossier, après des modifications dans la 2.14 (préversion avant la #34) | 10, 0 sauté | Pool, exclusions et favori **de la 3.0**, les modifications de la 2.14 sont ignorées (incompatibilité ci-dessus) ; journal 4, avec la partie notée en 2.14 ; langue automatique |
| 3.0 avec la #34, puis la 2.14 change Mid (préversion avec la #34, 16/11) | 8, 0 sauté | Changement de la 2.14 **repris** au premier lancement (Ahri+Zed, favori Zed), ancien pool gardé, notice Mid ; journal copié intact (2 entrées) ; rien de plus à la relance ; source identique (248 fichiers) |
| 3.0 sans empreinte (main avant la #34), puis la 2.14 change Mid (préversion avec la #34, 16/11) | 8, 0 sauté | Première fois : pools 3.0 **gardés**, copie du pool 2.x (Ahri+Zed), notice « gardé sans reprise » ; journal copié intact ; source identique (250 fichiers) |
| Même dossier, après des modifications dans la 2.16.2 | 10, 0 sauté | Modifications de la 2.16.2 reprises ; journal 4 ; langue automatique |
| Données 2.16.2 (copie d'essai, mesure précédente) | 8, 0 sauté | 6 champs communs identiques à la 2.16.2 |
| Données écrites par la 2.14 (fictives, mesure précédente) | 5, 0 sauté | Profil unique repris en mid : pool 3, 1 exclusion, 1 favori, journal 3 |

Dans les deux derniers cas, une nouvelle entrée notée dans la préversion porte `origin: "draft"` et `app: "3.0.0-beta.1"`, et le bouton « Je joue » suit le choix (« Zed noté », conseil Ahri affiché à part).

Formats acceptés : le profil unique de la 2.14 (`draftProfile` et `draftPrefs`, sans `draftProfiles`), les profils par rôle de la 2.16.2 et de la 3.0 (`draftProfiles`, version 1), le journal (`draftJournal`), l'historique (`draft-history.js`), les rôles calculés (format 2) et les réglages, avec ou sans `Language`.
