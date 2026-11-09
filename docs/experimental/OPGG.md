# Profil personnel et OP.GG : faisabilité

Étude et prototype du 5 novembre 2026. **Rien n'est activé, et le moteur n'a pas changé.** L'objectif est d'ajouter la maîtrise personnelle au classement. La recherche du profil n'est qu'un moyen.

## Conditions d'OP.GG (mises à jour le 14 septembre 2026)

Lues sur [op.gg/policies/agreement](https://op.gg/policies/agreement). Ce résumé n'est pas un avis juridique.

| Sujet | Texte | Conséquence |
|---|---|---|
| Extraction | « scraping or data mining » interdit, « use automated scripts to collect information from or otherwise interact with the Services » interdit | **Pas de lecture de la page, même sur le profil du joueur** |
| Reprise | « No Site Content may be modified, copied, distributed, framed, reproduced » sans permission écrite | Copie interdite, et l'intégration dans l'application reste à clarifier à cause de « framed » |
| Usage | « for your personal, non-commercial use only » | À vérifier avant toute distribution |
| Lien vers une page | Non abordé | Ouvrir le site dans le navigateur semble le plus sûr |

`robots.txt` autorise les robots en général, mais ne remplace pas ces conditions. `X-Frame-Options: SAMEORIGIN` empêche un cadre dans une page, sans gêner une fenêtre WebView2 dédiée.

## Sources possibles de statistiques

| Source | Clé à saisir | Données | État |
|---|---|---|---|
| Page OP.GG | Non | Tout ce qu'elle affiche | **Interdit par ses conditions** |
| Serveur MCP d'OP.GG | Non indiqué | Profil, pool, historique, détail d'une partie | Code sous licence MIT. **Aucune condition sur les données** : permission écrite nécessaire |
| API Riot, clé personnelle | Oui | Historique complet par partie | **Déjà possible** avec `tools/draft-import-stats.ps1`. Prévue pour les petits projets personnels |
| API Riot, clé de production et RSO | Non pour le joueur | Identité vérifiée et historique | Produit validé par Riot. La clé ne peut pas être dans l'exécutable, donc il faudrait un petit serveur |
| Client LoL (LCU), ton propre compte | Non | Historique récent, maîtrise de champions : **à vérifier** | Non testé. Élargirait la liste des deux adresses lues aujourd'hui, et l'usage du LCU n'est pas confirmé par Riot |

Les données de l'API Riot ne doivent concerner que le joueur lui-même : les règles de Riot interdisent de désanonymiser des joueurs.

## Ce que DRAFT sait déjà faire

Une ligne par partie : champion, rôle, victoire, date, patch, durée, mid adverse, équipe adverse. On peut donc en tirer le nombre de parties, le winrate, le rôle, la forme récente, l'évolution, l'expérience et les matchups personnels. Une moyenne avec régression existe déjà pour éviter de croire aux petits échantillons.

**Manquants :** le KDA (3 champs à ajouter à l'import) ; le lancement depuis l'application ; l'adversaire direct des autres rôles (seul le mid est gardé) ; le lien entre ces chiffres et le score. Aucune statistique n'existe sur ce PC aujourd'hui.

## Prototype isolé

`src/OpggBrowser.cs`, lancé seulement par `--opgg-prototype --data <dossier>` : aucune entrée dans les réglages, aucun effet sur le moteur.

- Fenêtre et dossier à part, session privée.
- Aucun accès aux cookies, aucun script injecté, aucune lecture de la page, aucun pont avec l'application.
- Les liens hors d'OP.GG partent dans le navigateur, seulement après un clic.
- Seule l'adresse de la page sert à reconnaître un profil, et paramètres ou jetons sont écartés.
- Enregistre seulement région, nom, tag et adresse, après clic sur « Utiliser ce profil ».

**Vérifié (47 tests) :** adresses valides et pièges, y compris un site imitant op.gg ; changement d'adresse sans rechargement ; sortie du site, fenêtres, permissions et téléchargements bloqués ; rien n'est gardé entre deux ouvertures. Sur le vrai site : l'accueil se charge, et une adresse de profil inventée est reconnue.

**Pas vérifié :** la vraie recherche et un vrai profil, à essayer à la main.

**Problème constaté :** OP.GG affiche son bandeau de consentement (1 751 partenaires) à l'ouverture. En session privée, il reviendra à chaque fois. Garder le choix demanderait une session durable, donc des cookies stockés.

**Limite de fond :** ce prototype identifie un joueur, il **n'importe aucune statistique**. Il donne le même résultat qu'un champ « Nom#TAG ».

## Proposition

### 1. Identifier le joueur
Par ordre de préférence : le compte du client ouvert, s'il est validé ; un champ `Nom#TAG` et une région ; OP.GG intégré, seulement avec une permission écrite. Un bouton « Voir sur OP.GG » peut ouvrir le navigateur.

### 2. Importer, après accord
Bouton dans les réglages, avec aperçu avant validation. Aucune synchronisation automatique, clé jamais enregistrée. Résultat dans un fichier local, avec reprise, patch et date. Sans ce fichier, l'application marche comme aujourd'hui, hors ligne compris.

### 3. Trois dimensions séparées
- **A. Théorie :** les règles actuelles, à corriger d'abord (voir Cho'Gath et Viego).
- **B. Méta et matchups :** bilans calculés.
- **C. Maîtrise :** un nouveau bloc, affiché à part, jamais fondu dans les autres.

Pour C, seulement à partir de parties antérieures :
- écart de winrate au reste du compte, ramené vers 0 s'il y a peu de parties ;
- expérience, croissante puis plafonnée, sans pénaliser un joueur pour un champion à apprendre ;
- forme récente, avec plus de poids aux parties récentes.

`Score final = A + B + β × écart + γ × expérience`, avec **β = γ = 0 par défaut**, donc aucun changement tant que rien n'est démontré. Option pour couper l'influence.

Statistiques retenues : parties, winrate, rôle, date. Le KDA n'entre que si un test montre qu'il apporte quelque chose. Les matchups personnels restent affichés sans compter, faute de parties suffisantes.

### 4. Pool
L'import **propose** des champions à cocher et ne retire jamais rien. Ajout et retrait manuels existent déjà ; les favoris existent seulement pour départager une égalité ; « à apprendre » et l'option d'influence sont à ajouter.

## Tests nécessaires

Le vrai problème : on ne sait jamais ce qu'un autre pick aurait donné. Une série de parties ne prouve donc pas qu'un conseil était meilleur.

1. **Non-régression :** avec β = γ = 0 ou sans fichier, scores identiques à aujourd'hui.
2. **Pas d'information du futur :** chaque partie ne voit que les précédentes.
3. **Scénarios :** Cho'Gath à 5 parties contre Viego à 150. L'écart et la raison changent, sans première place automatique.
4. **Prédiction sur ton historique :** le début pour apprendre, la fin pour tester, sur au moins 300 parties. La maîtrise prédit-elle mieux la victoire que la seule composition ? Attention : tu joues plus souvent ce qui te réussit, et ton niveau évolue.
5. **Mode observation :** dans le relevé réel, enregistrer le conseil avec et sans maîtrise, sans l'afficher, et regarder quand ils divergent.
6. **Répartition :** le conseil se concentre-t-il moins sur un seul champion ?

**Règle fixée à l'avance :** activation proposée seulement si le test 4 montre un gain net sur des parties non utilisées pour l'apprentissage, si les tests 1 à 3 passent, et si aucun comportement absurde n'apparaît au test 5. Le gain en victoires ne pourra pas être prouvé sur un seul joueur.

## Sources

- [Conditions d'OP.GG](https://op.gg/policies/agreement)
- [Serveur MCP d'OP.GG](https://github.com/opgginc/opgg-mcp)
- [Règles générales de Riot](https://developer.riotgames.com/policies/general)
- [Documentation LoL de Riot : clés et RSO](https://developer.riotgames.com/docs/lol)
