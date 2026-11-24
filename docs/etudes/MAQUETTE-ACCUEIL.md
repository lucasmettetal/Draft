# Maquette de l'Accueil : moins de texte, plus visuel

Statut : **proposition à valider**. Seul l'Accueil change. Aucun autre écran n'est touché.
Branche `ui/maquette-accueil`, partie de `integration/beta3` (371b9cb).

## Le constat

« L'interface reste fade, il y a beaucoup de texte, c'est un peu indigeste. »
Sur l'ancien Accueil, chaque carte avait un titre, deux paragraphes et un bouton. Tout avait le même poids.
Le bon exemple existait déjà : l'écran de fin de partie des panneaux (grande image, deux mots, deux gros boutons).

## Ce qui change

1. **Un seul message en grand, selon l'état.**
   - Prêt : « Tout est prêt », une phrase, un gros bouton. L'image du champion conseillé en blind remplit la droite de la bannière.
   - Partie à compléter : la carte « Dernière partie » devient le message principal. Image du champion en grand, Victoire et Défaite en gros. La bannière se fait discrète.
   - Première configuration : le guide, avec trois grands numéros, un visuel et une action par étape.
2. **Voyants.** L'état de League et des panneaux tient en une pastille : un point de couleur et deux ou trois mots. Un clic ouvre la phrase d'explication et les actions (activer la lecture, diagnostic, réglages).
3. **Ton pool en portraits.** L'icône du rôle sert de voyant de rôle. Les champions sont des portraits, le nom au survol. Le conseil en blind est cerclé d'or.
4. **Tes 5 dernières parties.** Portraits cerclés de vert (victoire) ou de rouge (défaite), grisés si le résultat attend. Lues dans le journal existant (`loadJournal`), sans donnée inventée. Les victoires et défaites sont écrites en gros chiffres. Sans partie : cinq cercles vides et une phrase.
5. **Une ligne en bas.** Version (ou préversion) et « tes données » tiennent sur une ligne, avec un lien « Gérer les données » vers Réglages. Le détail (copie 2.x, rassurance GitHub, journal, historique, rôles, matchups) est dans un « ? ».
6. **Une phrase au plus par bloc.** Le reste est derrière un « ? » (élément `details` natif, fermé au clic ailleurs ou avec Échap) ou dans une aide au survol.

Toutes les fonctions restent : boutons d'action, liens vers les réglages, « Dernière partie », guide du premier lancement, état hors application, notices (autre DRAFT ouverte, mise à jour, erreur d'import), avertissement de sécurité sur la lecture du client.
L'avertissement « Riot n'a pas confirmé… » reste toujours à côté du bouton qui active la lecture.

## Mots visibles

Mesure : `innerText` de la zone principale de l'Accueil (`[data-test="home"]`), mots contenant une lettre ou un chiffre.
Un « ? » fermé n'est pas compté : il n'est pas à l'écran. Vraie fenêtre WebView2, en français, 1366 × 768.

| État | Avant | Après | Écart |
|---|---|---|---|
| 1. Prêt, 5 dernières parties | 216 | 84 | −61 % |
| 2. Partie à compléter | 226 | 88 | −61 % |
| 3. Première configuration | 254 | 106 | −58 % |

Les deux mesures comptent la notice « Une autre DRAFT est ouverte » (environ 40 mots) : une autre DRAFT tournait sur la machine pendant les captures. Sans elle, l'écart est encore plus net.

## Captures avant / après

Vraie fenêtre WebView2 (exe construit avec `build.ps1`), images des champions chargées depuis Data Dragon, bureau caché.
**Pool et journal fictifs**, créés pour la capture dans un dossier de données temporaire, supprimé ensuite. Aucune donnée réelle.
L'« avant » est construit depuis 371b9cb (worktree temporaire, retiré ensuite).

Dossier : `docs/etudes/captures-maquette-accueil/`.

| État | 1366 × 768 | 1920 × 1080 à 125 % |
|---|---|---|
| 1. Prêt, 5 dernières parties | `avant-1-pret-5-parties-1366x768.png` / `apres-1-pret-5-parties-1366x768.png` | `avant-1-pret-5-parties-1920x1080-125.png` / `apres-1-pret-5-parties-1920x1080-125.png` |
| 2. Partie à compléter | `avant-2-partie-a-completer-1366x768.png` / `apres-2-partie-a-completer-1366x768.png` | `avant-2-partie-a-completer-1920x1080-125.png` / `apres-2-partie-a-completer-1920x1080-125.png` |
| 3. Première configuration | `avant-3-premiere-configuration-1366x768.png` / `apres-3-premiere-configuration-1366x768.png` | `avant-3-premiere-configuration-1920x1080-125.png` / `apres-3-premiere-configuration-1920x1080-125.png` |

## Textes

Seules les clés `home.*` changent (FR et EN).
- Modifiées : `home.hero.ready` (« Tout est prêt »), `home.guide.lead` (une phrase).
- Ajoutées : `home.more`, `home.hero.blind`, `home.guide.more` (la 2e phrase de l'ancien lead, dans le « ? »), `home.recent.*`.

## Tests

- `draft-ux8-tests.html` (Accueil, premier lancement, mise en page à 5 tailles) : 110 réussis, 0 échec. Une assertion visait l'ancien titre : elle vérifie maintenant le titre « Tout est prêt » de la bannière.
- `draft-fin-partie-tests.html` (carte « Dernière partie ») : 116 réussis, 0 échec.
- `draft-ux1` et `draft-ux2` ne visent pas l'Accueil : non relancées.
- Catalogues (`check-catalogs.ps1`) : 0 erreur (22 alertes déjà présentes, aucune sur les clés `home.*`).
- Détecteur de textes en dur (`detect-hardcoded.ps1 -Max 0`) : 0.
- Inventaire des messages (`draft-messages-tests.html`) : les nouveaux textes de l'Accueil y sont ajoutés.
- Mise en page (`run-layout-tests.ps1`), français : 75 passes OK, 0 échec, dont les 10 passes de l'Accueil (vierge, hors application, LoL fermé, connecté, lecture désactivée, erreur, draft en cours, dernière partie à compléter).
- Mise en page, anglais (`-Lang en`) : 75 passes OK, 0 échec, dont les 10 passes de l'Accueil.

## À valider

- Le principe : un message en grand, des voyants, des portraits, le détail derrière un « ? ».
- L'image du conseil en blind dans la bannière (elle change avec le conseil).
- Le rôle montré par l'icône du pool plutôt que par un troisième voyant (la barre latérale l'affiche déjà).
- Les parties « ignorées » et « à confirmer » ne sont pas montrées dans les 5 dernières parties.
- Si c'est validé : appliquer la même méthode aux autres écrans, un par un.
