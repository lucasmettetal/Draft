# Profils, migration et rôle attribué

## Nom de l'application

Le nom affiché devient **DRAFT** : fenêtres, panneaux, menu de l'icône, dialogues, import Riot, informations de version et exécutables (`DRAFT-Portable.exe`, `DRAFT-Setup.exe`).

**Restent volontairement inchangés,** pour ne rien perdre et ne pas créer deux applications :

| Identifiant | Pourquoi |
|---|---|
| Dossier `%LOCALAPPDATA%\DraftMid` | Profils, historique, rôles calculés, réglages et sauvegardes restent en place |
| Hôte `draftmid.local` | Le stockage du navigateur intégré (profils, journal) en dépend : le changer le viderait |
| Valeur `DraftMid` du démarrage automatique | Une seule entrée : DRAFT remplace l'ancienne au lieu d'en ajouter une |
| Verrou `DraftMid.Instance` | DRAFT et DraftMid ne peuvent pas tourner ensemble sur les mêmes données |
| Clé de désinstallation `DraftMid`, dossier `Programs\DraftMid` | Mise à jour au même endroit |
| `draftmid.log`, clés du stockage, `src/DraftMid.cs`, dépôt GitHub | Internes |

**Installation par-dessus DraftMid :** `DRAFT-Setup.exe` copie `DRAFT.exe`, supprime l'ancien `DraftMid.exe` et l'ancien raccourci, et repointe le démarrage automatique s'il visait l'ancien fichier. Testé dans un dossier et une clé de test.

## Profils par rôle

Stockés dans `localStorage`, clé `draftProfiles` :

```json
{ "version": 1, "active": "mid",
  "roles": { "mid": { "pool": ["TF", "Fizz"], "excluded": [], "favorite": null }, "jungle": {...}, ... } }
```

- Cinq rôles indépendants, chacun avec son pool, ses exclusions et son favori. Un pool vide est permis.
- Chaque changement est enregistré aussitôt, avec une copie de secours dans `draft-profiles.json` (dossier de données). Si le stockage du navigateur est vidé, la copie le remplit au prochain lancement.
- **Compatibilité arrière :** l'ancienne clé `draftProfile` reste à jour avec le rôle actif. Une V2.14 ou V2.15 relancée retrouve son profil.
- **Migration :** au premier lancement, l'ancien profil unique et ses exclusions et favori deviennent le profil de son rôle. Les autres rôles restent vides. L'ancienne clé n'est pas effacée.
- **Corruption :** un contenu illisible est mis de côté (`draftProfilesCorrupt`), puis la dernière sauvegarde reprend, sinon l'ancien profil.

Code : `web/draft-profiles.js`, testé par `web/draft-profiles-tests.html`.

## Export et import

Format `draft-config`, version 1. L'export contient les rôles et trois préférences (`assumeFront`, `coachLevel`, `autoRole`). **Liste fermée :** ni clé, ni jeton, ni identifiant, ni historique, ni journal.

L'import refuse sans rien écrire :

- un fichier vide, trop gros (256 Ko), ou qui n'est pas du JSON ;
- un autre format, une version absente ou plus récente ;
- un rôle inconnu, un pool mal formé ou un champion inconnu.

L'ancien format `{ role, pool }` est accepté et ne remplace que son rôle. Un rôle absent du fichier est laissé vide, avec un avertissement. Avant d'écrire, la configuration actuelle est sauvegardée (trois sauvegardes gardées). Après l'écriture, elle est relue, et tout est remis en place au moindre échec.

## Rôle attribué par le client

**Source :** dans la réponse de `/lol-champ-select/v1/session`, déjà lue pendant la sélection, le champ `assignedPosition` de l'entrée de `myTeam` dont `cellId` vaut `localPlayerCellId`. **Aucun nouvel endpoint**, et aucun autre joueur n'est lu.

| Valeur | Rôle |
|---|---|
| `top`, `jungle`, `middle`, `bottom`, `utility` | Top, Jungle, Mid, ADC, Support |
| Vide, absente, autre | Inconnu : rien n'est déduit, en particulier pas du champion choisi |

**Ce champ n'est pas documenté par Riot**, et il n'a été vérifié qu'avec des réponses simulées. Il est souvent vide en sélection aveugle et dans les modes sans poste.

**Préférence `autoRole` :**

| Valeur | Effet |
|---|---|
| `suggest` (par défaut) | Indication discrète « LoL t'attribue Jungle », avec un bouton pour changer |
| `auto` | Change de profil une fois par draft |
| `off` | Rien |

Une correction manuelle pendant la draft arrête tout changement automatique jusqu'à la draft suivante. Une indication ignorée ne revient pas. Hors draft, en cas de déconnexion ou de lecture désactivée, le rôle attribué redevient inconnu.

## Vérifier une migration

```powershell
.\tools\test-migration.ps1 -Source "$env:LOCALAPPDATA\DraftMid-essai-v215"
```

Le script copie le dossier, lance la candidate sur la copie avec `--report-state`, puis vérifie que l'original n'a pas changé. Le rapport ne contient que des nombres.
