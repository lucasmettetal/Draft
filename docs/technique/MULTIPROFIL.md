# Dépendances personnelles et future version multiprofil

État au 4 novembre 2026. Pas de compte, de serveur ni de service en ligne prévu à ce stade.

## Dépendances identifiées

| Élément | Où | État |
|---|---|---|
| Champions d'origine | `FIRST_PROFILE`, `DefaultPool` | Repris seulement par une installation d'avant les profils |
| Tableau de matchups | `MATCHUPS` dans `web/draft-engine.js` | Mid et 4 champions seulement. Ignoré pour les autres rôles |
| Bilans personnels | Retirés du code en V2.13 | **Encore présents dans l'historique Git et les releases déjà publiées** |
| Plans et conditions de victoire | `pickThreats`, `winCondition`, `buildPlan` | Textes propres aux 4 champions, fiches générales pour les autres |
| Règles affichées | `RulesTab` dans `web/index.html` | Texte d'origine sur ce profil, texte général sinon |
| Comptes Riot | `draft-accounts.txt` | Hors du dépôt (`.gitignore`) |
| Statistiques importées | `draft-stats.js` | Hors du dépôt (`.gitignore`). Import centré sur le mid (`MIDDLE`) |
| Chemins | `%LOCALAPPDATA%\DraftMid` | Aucun chemin propre à un PC dans le code ou l'exécutable |
| Lien de téléchargement | `README.md` | Dépôt du projet, sans donnée personnelle |
| Nom « DraftMid » | Partout | Trompeur pour les autres rôles |

## Séparation actuelle

| Domaine | Emplacement | Portée |
|---|---|---|
| Profil et pool | `draftProfile` dans le stockage local, copie dans `settings.json` | Ce PC |
| Préférences | `draftPrefs`, `settings.json` | Ce PC |
| Journal | `draftJournal` | Ce PC |
| Statistiques personnelles | `draft-stats.js` | Ce PC, jamais publiées |
| Données des champions | `draft-champions.js`, `draft-role-sheet.js` | Livrées |
| Calculs locaux | `draft-*-live.js`, `draft-*-state*.json` | Ce PC |
| Moteur | `draft-engine.js`, `draft-data.js` | Livré, sans donnée personnelle |

## Migration nécessaire

1. **À décider :** effacer les anciens bilans de l'historique public, ce qui réécrit les branches, les tags et les releases.
2. Faire passer l'import de statistiques par le rôle du profil.
3. Remplacer les textes propres aux 4 champions par des fiches, à qualité égale.
4. Mesurer les fiches sur de vraies parties, par rôle, avant de les présenter comme fiables.
5. Plusieurs profils sur un même PC : préfixer `draftProfile`, `draftPrefs` et `draftJournal` par un identifiant, puis ajouter un sélecteur.
6. Choisir un nom sans « Mid ».

## À ne pas généraliser pour l'instant

Le conseil hors du profil d'origine repose sur des fiches écrites à la main. Il reste un avis général tant que de vrais bilans par rôle n'existent pas.
