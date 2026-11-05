# Feuille de route V2.15

Point de départ : `integration/v2.15-coaching`, qui contient `main` et le moteur. État au 5 novembre 2026.

| Élément | État | Reste à faire |
|---|---|---|
| Intégration du coaching | **Déjà implémenté** sur la candidate | Relecture |
| Affichage des ex æquo | **Déjà implémenté** sur la candidate | Essai réel |
| Import Riot depuis l'interface | **Déjà implémenté** sur la candidate, testé en simulation | Premier import avec une vraie clé, par le joueur |
| Le plus joué et limites de l'historique | **Déjà implémenté** sur la candidate | Essai avec un vrai historique |
| Trois niveaux de conseils | **Déjà implémenté** sur la candidate | Relecture |
| Navigation en 5 onglets | **Déjà implémenté** sur la candidate | Essai réel |
| Retrait de Meraki des sources | **Déjà implémenté** sur toutes les branches actives | L'historique et les anciennes releases restent : voir `docs/technique/ANCIENNES-RELEASES.md` |
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

## Ordre conseillé

1. Essai isolé de la candidate, avec l'import : `tools/try-v215.ps1`.
2. Relecture des conseils et relevé sur de vraies drafts.
3. Fusion dans `main` et release, seulement sur décision.
