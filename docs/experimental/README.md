# Travaux expérimentaux

**Rien ici n'est disponible dans la version stable.** Aucune de ces pistes n'a été validée sur de vraies parties.

| Sujet | État | Où | Document |
|---|---|---|---|
| OP.GG | Prototype isolé, non distribué. Ses conditions interdisent l'extraction automatique | Fichiers locaux non suivis (`src/Opgg*.cs`) | [OPGG.md](OPGG.md) |
| Bans adverses | Aucun bénéfice démontré, moteur non modifié | Stash local et fichiers non suivis | [BANS.md](BANS.md) |
| Égalités à l'écran | Candidate, non stable | `integration/v2.15-coaching` | `docs/INTEGRATION-V215.md`, sur cette branche |
| Coaching pédagogique | Conseils à relire par un joueur | `feature/strategic-engine-coaching`, `integration/v2.15-coaching` | `docs/COACHING.md`, sur ces branches |
| Difficulté et maîtrise | Conseils seulement, jamais dans le score | Mêmes branches | `docs/MAITRISE.md`, sur ces branches |
| Import Riot dans l'application | Testé sur un serveur simulé seulement | `integration/v2.15-coaching` | `docs/INTEGRATION-V215.md`, sur cette branche |
| Variantes de frontline | Audit, rien d'appliqué | `feature/strategic-engine-coaching` | `docs/AUDIT-MOTEUR.md`, sur cette branche |
| Sources de matchups | Aucune source tierce clairement autorisée | `feature/strategic-engine-coaching` | `docs/SOURCES-MATCHUPS.md`, sur cette branche |

## Modèles de classement

| Modèle | Principe | Décision |
|---|---|---|
| **A** | Le moteur, inchangé | **Classement par défaut** |
| **T** | A, et l'expérience départage seulement les vraies égalités | Prototype, désactivé |
| **B** | A + poids d'expérience et de résultats | Audit seulement. Sur un profil fictif, il plaçait Darius en tête de 0 à 50,3 % des drafts |
| **C** | B + malus de difficulté | Supprimé : la note de Riot est contestée, et elle ne sert qu'aux conseils |
| **D** | Statistiques de matchups dans le score | Non retenu. Le fournisseur existe, sans source branchée |

Ces chiffres viennent d'historiques fictifs et de drafts simulées. Ils décrivent un comportement, pas une qualité. Aucune amélioration du taux de victoire n'est démontrée.
