# Feuille de route V2.15

Point de départ : `integration/v2.15-coaching`, partie de la V2.14 (`7690f79`). État au 5 novembre 2026.

| Élément | État | Reste à faire |
|---|---|---|
| Intégration du coaching | **Déjà implémenté** sur la candidate | Relecture |
| Affichage des ex æquo | **Déjà implémenté** sur la candidate | Essai réel |
| Import Riot depuis l'interface | **Déjà implémenté** sur la candidate, testé en simulation | Premier import avec une vraie clé, par le joueur |
| Le plus joué et limites de l'historique | **Déjà implémenté** sur la candidate | Essai avec un vrai historique |
| Trois niveaux de conseils | **Déjà implémenté** sur la candidate | Relecture |
| Simplification des onglets | **À développer** | Décider entre les options ci-dessous |
| Retrait de Meraki dans la candidate | **Prêt à intégrer** | Reprendre le commit de retrait de `main` |
| Tests de non-régression | **Prêt à intégrer** | Relancer après chaque fusion, vérifier en GitHub Actions |
| `coachingSummary` à l'écran | **Prêt à intégrer** | Décider s'il remplace le filtre actuel |
| Onglet Parties lisant le nouvel import | **À développer** | Avec mise en garde sur les petits échantillons |
| Conseils pour débutants | **Expérimental** | 121 conseils d'exemple sur 213 attendent un joueur |
| Modèle T | **Expérimental** | Comparaison écrite à l'avance, sur de vraies drafts |
| Variante de frontline | **Expérimental** | Comparaison sur de vraies drafts |
| Validation sur de vraies drafts | **Bloqué par un manque de données** | Relevé à remplir |
| Validation des conseils | **Bloqué par un manque de données** | Relecture par un joueur, et relevé |
| Bilans de matchups | **Bloqué par un manque de données** | Calcul local avec une vraie clé |
| Effet sur le taux de victoire | **Bloqué par un manque de données** | Échantillon suffisant et indépendant |

## Onglets

| Largeur | Sept onglets (358 px) | Six, sans Règles (308 px) | Cinq, sans Règles ni Comparer (240 px) |
|---|---|---|---|
| 1280 x 720 (297 px) | 2 lignes | 2 lignes | 1 ligne |
| 1366 x 768 (315 px) | 2 lignes | 1 ligne | 1 ligne |
| 1920 x 1080 (427 px) | 1 ligne | 1 ligne | 1 ligne |

**Proposition, à valider :** cinq onglets : Plan, Analyse, Conseils, Duel, Parties.

- **Règles :** en bas de Conseils, sous « Comment DraftMid décide ». Le contenu reste, et il répond à la même question.
- **Comparer :** en section d'Analyse, qui montre déjà les critères de chaque champion. Le bouton de l'alternative y mène directement.

C'est un déplacement, sans nouvelle direction graphique.

## Ordre conseillé

1. Essai isolé de la candidate, avec l'import.
2. Reprise du retrait de Meraki, puis simplification des onglets.
3. Relecture des conseils et relevé sur de vraies drafts.
4. Fusion dans `main` et release, seulement sur décision.
