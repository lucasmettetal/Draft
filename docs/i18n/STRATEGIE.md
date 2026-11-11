# Stratégie : programme C#, page WebView2, moteur, langue

Étude préparatoire pour DRAFT 3.0.0 bêta. Aucun fichier de production n'est modifié ici.
Reprend `DRAFT-ux/docs/ux/PLAN-I18N-FR-EN.md` § 3, 4 et 9 sans les refaire ; ce document précise le « comment ».
Format du catalogue : [FORMAT-CATALOGUE.md](FORMAT-CATALOGUE.md). Inventaire : [INVENTAIRE.md](INVENTAIRE.md).

## 1. Vue d'ensemble

```
settings.json ─ Language ("auto" | "fr" | "en", absent = auto)
      │
      ▼
Programme (src/*.cs) ── Strings.Load(lang) ── ressource intégrée web/i18n/{fr,en}.js ── DataScripts.Canonical
      │  menus, bulles, MessageBox, dialogues : Strings.T("native.…")
      │  états envoyés à la page (HostJson) : clé + variables
      ▼
Page (WebView2) ── index.html?…&lang=en ── <script src="i18n/fr.js">, <script src="i18n/en.js"> ── t(), tp(), tx()
      │
      ▼
Moteur (draft-engine.js, draft-coaching.js, draft-mastery.js) : calculs inchangés, messages { key, vars }
```

## 2. Le programme lit le même catalogue

- **Pas de `.resx`** : `build.ps1` compile avec le seul `csc.exe` de Windows ; `resgen` et les assemblys satellites demandent un SDK. Et deux sources (page et programme) divergeraient.
- Classe `Strings` (prototype : `tools/i18n/Strings.cs`, compilé et testé par `tools/i18n/run-i18n-tests.ps1` avec le vrai `src/DataScripts.cs`) :
  - lit la ressource intégrée `web/i18n/{code}.js` (pas le fichier extrait, que l'utilisateur pourrait modifier) ;
  - la valide avec `DataScripts.Canonical("DRAFT_I18N_" + CODE, texte)`, puis la désérialise avec `JavaScriptSerializer` ;
  - garde deux dictionnaires : français (référence) et langue active ;
  - `Strings.T(key, vars)`, `Strings.P(key, count, vars)`, `Strings.Number(n)` ; repli langue active → français → `[clé]`.
- Coût : une lecture de 60 à 100 Ko au démarrage, quelques millisecondes, avant la création du menu.
- Si le français intégré est illisible, `--selftest` échoue : la CI (`.github/workflows/build.yml`, qui lance déjà l'autotest) bloque la publication.
- `Extract()` écrit aussi `web/i18n/*.js` dans le dossier de la page **par `DataScripts.Canonical`**, comme les fichiers de données : la page ne reçoit jamais autre chose qu'un objet JSON réécrit par le programme.

## 3. La page reçoit la langue

- Le programme ajoute `&lang=fr` ou `&lang=en` à chaque adresse qu'il ouvre (`Ensure`, `OpenFull`, rechargements), et `"lang"` dans `HostJson`.
- La page lit `lang` avant le premier rendu, sur une liste blanche (`fr`, `en`) ; toute autre valeur donne `fr`. `qps` (pseudo-langue) n'est accepté qu'avec `i18ncheck=1`, dans les pages de test.
- Hors du programme (fichier ouvert dans un navigateur, bancs de test) : sans `lang`, la page reste en français, comme aujourd'hui. Les bancs existants ne changent donc pas.
- `document.documentElement.lang` suit la langue (lecteurs d'écran, césure).

## 4. Cohérence des termes entre programme et page

- Même catalogue, mêmes clés pour les mêmes notions. Exemple : la page et le dialogue de clé utilisent la même clé pour « Clé Riot (masquée, jamais enregistrée) : » (aujourd'hui écrite deux fois : `HistoryImporter.cs:319`, `RoleRatesUpdater.cs:705`).
- Un seul libellé par réglage **avant** de traduire (relevé A-30 de l'audit) :

| Réglage | Menu de l'icône aujourd'hui | Page aujourd'hui | Clé commune proposée |
|---|---|---|---|
| Ouverture automatique | Activation automatique pendant la draft | Afficher les panneaux automatiquement pendant la draft | `settings.windows.autoDraft` |
| Placement | Positionnement automatique | Coller les panneaux au client LoL | `settings.windows.follow` |
| Panneau unique | Panneau unique | Un seul panneau | `settings.windows.compact` |
| Import | Importer mes statistiques... | Importer mon historique Riot | (deux fonctions différentes : garder deux libellés distincts) |

  Le choix du libellé appartient à la PR C-2 (vocabulaire) ou à UX ; I18N-1 reprend le résultat.
- **États natifs affichés dans la page** (`historyStatus`, `rolesStatus`, `updateStatus`, `placement`, `phase`, `lcuStatus` : 60 entrées marquées « page » dans l'inventaire). Aujourd'hui, ce sont des phrases françaises envoyées par `HostJson`. Proposition I18N-3 : le programme envoie `{ "key": "native.history.status.progress", "vars": { "done": 3, "total": 120 } }` et la page les rend avec `t()`. La bulle Windows, elle, utilise `Strings.T` avec la même clé. Transition : pendant I18N-1, le programme continue d'envoyer le texte ; la page l'affiche tel quel.
- Erreurs de mise à jour (`AppUpdater`, `FriendlyError`) : des exceptions portent aujourd'hui la phrase (`throw new UpdateError("…")`). Proposition : l'exception porte une clé (`native.update.error.signatureInvalid`) ; `FriendlyError` la traduit au dernier moment. Le message technique pour le journal reste en anglais ou en français fixe, hors catalogue.

## 5. Messages de sécurité : le sens se garde mot pour mot

Ces textes protègent l'utilisateur ou disent ce que DRAFT ne fait pas. La traduction doit garder **chaque affirmation**, sans l'adoucir ni l'enrichir. Ils sont marqués `securite: true` dans `inventaire.json` (42 entrées) et relus par deux personnes en I18N-2.

| Idée à garder | Français actuel | Anglais proposé |
|---|---|---|
| La clé n'est jamais enregistrée | Clé Riot (masquée, jamais enregistrée) : | Riot key (hidden, never saved): |
| Tu saisis ta clé, elle n'est pas gardée | Tu saisis ta clé toi-même : elle n'est pas enregistrée. | You enter your key yourself: it is never saved. |
| DRAFT n'agit pas dans le client | Rien n'est envoyé au client LoL : tu verrouilles toi-même ton champion. | Nothing is sent to the League client: you lock in your champion yourself. |
| Riot n'a pas validé la lecture du client | Riot n'a pas confirmé que c'est autorisé : tu l'actives à tes risques. | Riot has not confirmed this is allowed: you turn it on at your own risk. |
| Pas d'affiliation | Outil personnel non approuvé par Riot Games. | Personal tool, not endorsed by Riot Games. |
| Données gardées localement | DRAFT lira tes parties classées solo/duo avec ta clé et les gardera sur ce PC. | DRAFT will read your ranked solo/duo games with your key and keep them on this PC. |
| Consentement explicite | J'accepte cette lecture | I agree to this |
| Échec sans effet | … Rien n'a été changé. / rien n'a été modifié. | … Nothing was changed. |
| Vérification de la mise à jour | La signature de cette version n'est pas valide. Rien n'a été changé. | This version's signature is not valid. Nothing was changed. |
| Diagnostic sans secret | Aucun mot de passe, port, chemin ni nom de joueur n'apparaît ici. | No password, port, path or player name appears here. |

Règles :
- Pas de « may », « should » là où le français affirme (« n'est pas enregistrée » → « is never saved », jamais « is not stored permanently »).
- « Not endorsed by Riot Games » reprend la formule usuelle des outils tiers ; ne pas écrire « approved » ni « affiliated » seul.
- La lecture du client reste désactivée par défaut dans les deux langues ; l'avertissement s'affiche dans les deux langues au même endroit.

## 6. Textes du moteur : messages structurés, calculs inchangés

Aujourd'hui, le moteur renvoie des phrases : `RULES.matchup` rend `[note, "À éviter contre " + mid]`, `draft-coaching.js` construit `title` et `body`, `draft-mastery.js` assemble des explications. Proposition :

```js
// Avant
return [-4 / 3, `À éviter contre ${x.mid}`];
// Après : même note, même ordre de calcul ; seul le texte devient une clé
return [-4 / 3, msg("engine.reason.matchup.avoid", { champion: x.mid })];
```

- `msg(key, vars)` crée `{ key, vars }`. Le rendu (`t()`, `tx()`) est fait par l'interface, au dernier moment.
- Les **notes, poids, seuils et classements ne changent pas**. Garde : instantané du banc moteur (PR #8) à 0 différence de classement, et relevé des messages (PR #9) à 0 différence de texte en français.
- Transition sûre : `msg()` peut d'abord rendre directement `t(key, vars)` (une chaîne), ce qui laisse tous les consommateurs inchangés ; le passage aux objets se fait ensuite fichier par fichier.
- **Logique qui lit du texte français** (à remplacer par des codes avant ou pendant I18N-1, sinon l'anglais casse le calcul) :

| Où | Ce qui est lu | Remplacement |
|---|---|---|
| `web/draft-data.js:301-313`, `draft-coaching.js:412`, `draft-mastery.js:270`, `index.html:869` | confiance `"faible"`, `"moyenne"`, `"élevée"` comparée comme valeur | codes `low`, `medium`, `high` ; libellé par `t("data.confidence.high")` |
| `web/draft-data.js:330`, `index.html:1292` | candidat `"pas encore choisi"` mis dans une liste de noms puis cherché | marqueur `{ unrevealed: true }` |
| `web/draft-coaching.js:423` | `/parties/.test(x.text)` pour savoir si le matchup vient d'un bilan | drapeau `live: true` dans le critère |
| `web/draft-coaching.js:550` | `/te touche/.test(i.title)` | sens porté par l'identifiant ou une variable (`longer: true`) |
| `src/DraftMid.cs:1482, 1587` (e2e) et pages `*-tests.html` | boutons cherchés par leur texte | attributs `data-test` (préalable, voir PLAN-EXECUTION.md) |

- Noms de champions : noms anglais de Riot dans les deux langues (comportement actuel). Ils sont des variables, jamais des clés.
- Rôles : la clé interne (`top`, `jungle`, `mid`, `adc`, `support`) ne change pas ; le libellé passe par `role.name.*` et les formes en phrase par `role.in.*` (« au mid » / « in mid lane »).

## 7. CSV de validation et fichiers de données

- `validation-drafts.csv` (`ValidationHeader`, `src/DraftMid.cs:682`) : **colonnes et valeurs non traduites**. Ce sont des codes lus par `tools/validation-report.ps1` (`date;version;role;pool;adversaires;mid_affiche;confiance;…`, valeurs `manuel`, `oui`, `non`, confiance `faible|moyenne|élevée`). Elles ne sont pas affichées dans l'interface.
- Si la confiance passe aux codes `low|medium|high` (§ 6), la ligne CSV garde l'ancien mot français par une table de correspondance, pour que les relevés anciens et nouveaux restent comparables.
- Journal (`draftJournal`), profils, `draft-stats.js`, `draft-history.js`, bilans calculés : données sans texte affiché ; rien à migrer. Les dates sont en ISO.
- Fichier exporté `draft-profils.json` et `draft-journal.json` : noms de fichier inchangés (compatibilité d'import).
- Journal de diagnostic (`draftmid.log`) : reste en français fixe, hors catalogue (il sert au développeur ; mélanger les langues gênerait la comparaison des rapports).
- Diagnostic de connexion LoL (`LcuDiagnostic`) : texte que l'utilisateur colle dans un message. Traduit (il le lit), mais chaque ligne garde un repère fixe non traduit (`[lockfile]`, `[proxy]`…) proposé en I18N-3, pour que le développeur lise un rapport anglais ou français de la même façon.

## 8. Détection et mémorisation de la langue

| Étape | Comportement |
|---|---|
| Premier lancement | `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName` : `fr` (fr-FR, fr-CA, fr-BE, fr-CH…) donne le français ; toute autre langue donne l'anglais. |
| Réglage | Réglages > Général > Langue : « Automatique (Windows) », « Français », « English », chaque langue écrite dans sa langue. |
| Mémorisation | `settings.json`, champ `Language` de `UserPreferences` (`src/DraftMid.cs:168`) : `"auto"`, `"fr"` ou `"en"`. |
| Rétrocompatibilité | Champ absent (installations 2.x) = `"auto"` : `JavaScriptSerializer` laisse `null`, `Strings.Resolve` détecte. Aucune migration écrite. Valeur inconnue (`"de"`, texte piégé) : ignorée, détection. Un retour à une version 2.x ignore le champ sans erreur. |
| Pas de stockage navigateur | La langue n'est pas dans `localStorage` : le programme en a besoin avant toute page, et ce stockage peut être vidé. |
| Envoi à la page | `&lang=` dans l'adresse et `lang` dans `HostJson` (§ 3). |
| Changement | Commande de page `setLanguage` (liste blanche de valeurs, origine vérifiée par `IsAppPage` comme les autres commandes). Le programme enregistre, reconstruit le menu de l'icône, recharge **tout de suite** la fenêtre complète (là où se trouve le réglage), et recharge les panneaux **seulement hors draft** (même règle que `ReloadRolesIfIdle`, `src/DraftMid.cs:893`) ; pendant une draft, ils changent à la fin de la sélection. |
| Boîtes et dialogues ouverts | Gardent leur langue jusqu'à leur fermeture. |

Recommandation : **changement à chaud hors draft**, plutôt qu'au redémarrage. Le redémarrage ferait perdre l'état d'un panneau ouvert et oblige à expliquer pourquoi rien ne change ; le rechargement existe déjà pour les rôles.

Anciennes données : aucune n'a de texte à traduire (§ 7). Le journal affiche « Victoire », « Défaite » à partir d'un code (`result`) : il s'affiche dans la nouvelle langue sans migration. Les exports de profils d'une version française s'importent dans une version anglaise.

## 9. Ce qui ne se traduit pas

Noms de champions, de sorts et d'objets de Riot (anglais dans les deux langues aujourd'hui), « DRAFT », « League of Legends », « Data Dragon », « LoLalytics », codes de région (EUW, NA), identifiants Riot `Nom#TAG`, noms de fichiers, colonnes CSV, journaux internes. Le détecteur les exclut (`tools/i18n/exclusions.json`).
