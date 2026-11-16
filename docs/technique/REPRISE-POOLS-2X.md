# Reprise des pools changés dans une 2.x

Corrige l'incompatibilité décrite dans `docs/release/COMPATIBILITE-2X.md` (branche de la préversion) : des pools changés dans une 2.14 étaient ignorés par la 3.0, puis écrasés. Code : `web/draft-profiles.js` (`takeLegacyChanges`, `persistProfiles`, `loadProfiles`) et `web/index.html` (`savePrefs`, notice `LegacyNotice`).

## Le problème

- Une 2.14 a un seul profil. Elle l'écrit dans `draftProfile` (rôle, pool), et ses exclusions et son favori dans `draftPrefs`.
- La 3.0 garde un profil par rôle dans `draftProfiles`, et le lit en priorité.
- Elle tient `draftProfile` à jour pour les anciennes versions, mais ne le relit pas. Ce qu'on changeait dans une 2.14 était donc ignoré, puis écrasé à la sauvegarde suivante de la 3.0.
- La 2.16 n'a pas ce défaut : elle écrit `draftProfiles`, comme la 3.0.

## La règle

1. **Empreinte.** À chaque écriture de `draftProfile` ou de `draftPrefs`, la 3.0 enregistre une empreinte de leur contenu dans `draftLegacyMark`. L'empreinte porte sur leur sens : rôle et pool d'un côté, exclusions et favori de l'autre. Ni l'ordre des champs, ni les autres préférences n'y entrent, et une 2.x qui réécrit le même contenu ne déclenche rien.
2. **Au chargement**, si l'empreinte ne correspond plus, une 2.x a écrit depuis la dernière écriture de la 3.0 :
   - le rôle concerné est celui de `draftProfile`. Si c'est un autre rôle que le rôle joué, ce dernier ne change pas ;
   - seul ce qui a changé est repris : le pool si `draftProfile` a changé, les exclusions et le favori si `draftPrefs` a changé. Le reste du rôle et les autres rôles sont gardés ;
   - **avant**, l'ancien profil de ce rôle est copié dans `draftLegacyBackups` : liste datée, la plus récente en tête, 10 entrées au plus. Une entrée ne remplace jamais la précédente. Sans copie possible, rien n'est repris ;
   - si le contenu repris est identique au profil de la 3.0, rien ne change, sans notice ;
   - une notice non bloquante s'affiche sur l'Accueil et dans Mon pool, jusqu'à « Compris » : « Ton pool {rôle} a été modifié dans DRAFT 2.x : DRAFT reprend ces changements. L'ancien pool est conservé. »
3. **Première fois, sans empreinte.** C'est le cas des données écrites par une 3.0 sans cette correction, comme `DRAFT-3.0-essai.exe`, puis peut-être par une 2.14. On ne peut pas savoir qui a écrit en dernier :
   - **rien n'est repris**, et les profils de la 3.0 font référence ;
   - une copie de ce que contiennent `draftProfile` et `draftPrefs` est gardée dans `draftLegacyBackups` (raison `first-mark`, valeurs brutes et profil lu) ;
   - si le pool de l'ancienne clé diffère de celui du même rôle, une notice le dit : « Ton pool {rôle} dans DRAFT 2.x diffère de celui-ci : DRAFT garde le sien, sans rien reprendre. Une copie du pool 2.x est conservée. » ;
   - l'empreinte est posée. Les changements suivants d'une 2.x seront repris.

   **Justification.** Reprendre sans empreinte pourrait écraser un pool de la 3.0 avec un contenu périmé. `draftProfile` diffère aussi de `draftProfiles` sans aucune 2.x, par exemple quand le rôle joué n'a pas de champion. La copie évite toute perte. La notice évite tout écrasement silencieux.
4. **Données abîmées ou partielles.**
   - `draftProfile` illisible, sans pool, ou avec un rôle inconnu : rien n'est repris, son contenu brut est gardé (raison `unusable`), et les profils restent tels quels.
   - `draftPrefs` illisible : seul le pool est repris, et les exclusions et le favori de la 3.0 restent s'ils sont encore dans le pool.
   - Empreinte illisible : même règle que la première fois.
   - `draftProfiles` abîmé : la dernière sauvegarde est reprise, comme avant, puis les changements d'une 2.x passent par-dessus.
5. **Profil cohérent pour la 2.x.** Quand la 3.0 écrit `draftProfile` (rôle joué), elle écrit aussi les exclusions et le favori de ce rôle dans `draftPrefs`, sans toucher aux autres préférences. Avant, `draftPrefs` pouvait garder ceux d'un autre rôle.
6. **Rien n'est supprimé.** Les clés d'origine restent, et la notice lue est seulement marquée `seen`.

## Ce qui ne change pas

- Le moteur, ses poids et ses seuils ; la CSP ; la lecture LCU ; la mise à jour.
- La migration du profil unique (2.14 sans `draftProfiles`), la récupération après corruption, l'export et l'import.
- Une 3.0 seule n'est pas touchée : aucune reprise, aucune notice.
- La 2.16 n'est pas touchée : ses changements sont déjà dans `draftProfiles`, rien n'est repris et aucune notice n'apparaît.

## Préversion 3.0.0-beta.1

Sa copie initiale emporte `draftProfile`, `draftPrefs` et `draftLegacyMark` avec le reste du stockage :
- si une 2.x a écrit après la dernière écriture d'une 3.0 corrigée, la préversion reprend ce changement à son premier lancement ;
- si l'empreinte manque (données d'une 3.0 non corrigée), la règle de la première fois s'applique ;
- ensuite, aucune 2.x n'écrit dans le dossier de la préversion, donc plus aucune reprise n'a lieu.

## Tests

- `web/draft-profiles-tests.html` : 29 tests nouveaux. Sur `main`, les 18 qui demandent la correction échouent (65/18), et la branche passe 83/0. Ils couvrent la reprise, la 3.0 seule, la réécriture sans changement, un autre rôle, trois allers-retours, les sauvegardes bornées, la première empreinte, les données abîmées ou partielles, la 2.16, et l'absence de suppression.
- `web/draft-ux4-tests.html` : la vraie page, avec reprise, ancien pool gardé, notice dans Mon pool et sur l'Accueil, puis « Compris ». Sur `main`, 5 échecs ; la branche passe 33/0.
- Scénarios avec les exécutables 2.14 et 2.16.2 sur le bureau caché, données fictives : voir la description de la PR.
