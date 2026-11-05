# Branches et publication

## Stratégie

| Branche | Contenu | Règle |
|---|---|---|
| `main` | Dernière version stable validée, actuellement V2.14 | Avance seulement par fusion rapide |
| `feature/strategic-engine-coaching` | Moteur de conseils, égalités, familiarité | Jamais fusionnée directement dans `main` |
| `integration/v2.15-coaching` | V2.14 + moteur, pour les tests | Devient stable seulement après validation |
| `v2.3-detection` | Ancienne branche de travail, jusqu'à la V2.14 | Conservée, plus de nouveau travail |
| `docs/consolidation-v2.14` | Préparation de `main` | Temporaire |

Pas de force-push ni de réécriture de l'historique public sans décision explicite.

## Worktrees

Un worktree par chantier, chacun avec ses dossiers `dist-*` (ignorés par Git).

| Dossier | Branche |
|---|---|
| `draftmid-app` | `v2.3-detection`. Contient aussi des fichiers non suivis : OP.GG et bans |
| `DraftMid-coaching` | `feature/strategic-engine-coaching` |
| `DraftMid-v215` | `integration/v2.15-coaching` |
| `DraftMid-docs` | `docs/consolidation-v2.14` |

Le stash partagé contient l'expérience sur les bans : ne jamais lancer `git stash pop` sans référence.

## Workflow GitHub Actions

`.github/workflows/build.yml` :

- **Chaque push, sur toute branche :** compile avec `.\build.ps1`, puis lance les tests natifs. Rien n'est publié.
- **Tag `v*` :** ajoute `gh release create`, qui **publie une release publique avec les exécutables**.

Donc : ne jamais utiliser `git push --tags` ni pousser un tag sans décision. Le workflow compile tout ce que contient `web/` : une donnée ajoutée au dépôt finit dans une release.

## Contrôles avant un push

1. Lister exactement les commits envoyés : `git log origin/<branche>..<branche>`.
2. Chercher clés, jetons et chemins personnels dans le patch de ces commits, pas seulement dans le dernier état.
3. Vérifier qu'aucun fichier privé n'est suivi : `draft-stats.js`, `draft-history.js`, `draft-accounts.txt`, journaux, sauvegardes, exécutables.
4. Vérifier que `web/draft-role-rates.js` n'est pas revenu.
5. Compiler et lancer les tests natifs.
6. Pousser une branche à la fois, sans tag.

## Ce que l'historique public contient déjà

Une suppression dans un nouveau commit n'efface rien du passé. Sans réécriture, restent publics :

- **Référence Meraki** (`web/draft-role-rates.js`) : dans l'historique depuis la V2.5, et dans l'exécutable des releases publiques. Vérifié sur la V2.10.
- **Bilans personnels :** retirés du code en V2.13, présents dans l'historique et les releases plus anciennes.

Réécrire l'historique ou retirer d'anciennes releases reste une décision à prendre par le propriétaire du dépôt.
