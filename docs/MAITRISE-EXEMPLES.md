# Exemples du classement personnalisé

Généré par `tools/run-mastery-compare.ps1`. **Tous les historiques sont fictifs.** Ces chiffres décrivent le comportement du prototype sur des drafts simulées : ils ne mesurent aucune victoire.

- **A :** moteur actuel, sans personnalisation.
- **B :** A + expérience + résultats.
- **C :** B + complexité, seulement là où l'expérience manque.
- **D :** C + statistiques de matchups. **Aucune source autorisée n'est branchée, donc D donne le même résultat que C.**

## Comportement sur 2 000 drafts

Part en tête, égalités partagées. « Changé » compte les drafts où le premier diffère de A sans être déjà à égalité avec lui.

### Mid, débutant sans historique

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Fizz 52.5, Twisted Fate 25.2, Kog'Maw 15.5, Xerath 6.9 | 2 % | 0 % |
| B | Fizz 52.5, Twisted Fate 25.2, Kog'Maw 15.5, Xerath 6.9 | 2 % | 0 % |
| C | Fizz 54.5, Twisted Fate 22.1, Kog'Maw 17.5, Xerath 6 | 0.3 % | 3.2 % |
| D | Fizz 54.5, Twisted Fate 22.1, Kog'Maw 17.5, Xerath 6 | 0.3 % | 3.2 % |

### Mid, joueur TF expérimenté

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Fizz 52.5, Twisted Fate 25.2, Kog'Maw 15.5, Xerath 6.9 | 2 % | 0 % |
| B | Fizz 50.7, Twisted Fate 34.4, Kog'Maw 10.3, Xerath 4.6 | 0 % | 9.7 % |
| C | Fizz 51.5, Twisted Fate 34.5, Kog'Maw 10.3, Xerath 3.8 | 0 % | 10.6 % |
| D | Fizz 51.5, Twisted Fate 34.5, Kog'Maw 10.3, Xerath 3.8 | 0 % | 10.6 % |

### Mid, 5 parties Kog'Maw à 80 %, 150 Fizz à 55 %

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Fizz 52.5, Twisted Fate 25.2, Kog'Maw 15.5, Xerath 6.9 | 2 % | 0 % |
| B | Fizz 63.6, Twisted Fate 17.3, Kog'Maw 15.7, Xerath 3.4 | 0.1 % | 11.6 % |
| C | Fizz 64.5, Kog'Maw 18, Twisted Fate 14.2, Xerath 3.4 | 0 % | 14.9 % |
| D | Fizz 64.5, Kog'Maw 18, Twisted Fate 14.2, Xerath 3.4 | 0 % | 14.9 % |

### Mid, Xerath en apprentissage

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Fizz 52.5, Twisted Fate 25.2, Kog'Maw 15.5, Xerath 6.9 | 2 % | 0 % |
| B | Fizz 56.6, Twisted Fate 29.7, Kog'Maw 11, Xerath 2.8 | 0 % | 8.7 % |
| C | Fizz 57.5, Twisted Fate 28.6, Kog'Maw 11.1, Xerath 2.9 | 0 % | 9.5 % |
| D | Fizz 57.5, Twisted Fate 28.6, Kog'Maw 11.1, Xerath 2.9 | 0 % | 9.5 % |

### Jungle, sans historique

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Cho'Gath 43.1, Lee Sin 34.1, Master Yi 11.3, Viego 11.2, Briar 0.3 | 37.5 % | 0 % |
| B | Cho'Gath 43.1, Lee Sin 34.1, Master Yi 11.3, Viego 11.2, Briar 0.3 | 37.5 % | 0 % |
| C | Cho'Gath 43.1, Lee Sin 34.1, Master Yi 11.3, Viego 11.2, Briar 0.3 | 37.5 % | 0 % |
| D | Cho'Gath 43.1, Lee Sin 34.1, Master Yi 11.3, Viego 11.2, Briar 0.3 | 37.5 % | 0 % |

### Jungle, joueur Lee Sin ancien

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Cho'Gath 43.1, Lee Sin 34.1, Master Yi 11.3, Viego 11.2, Briar 0.3 | 37.5 % | 0 % |
| B | Lee Sin 52, Cho'Gath 29.1, Viego 18.8, Master Yi 0.2, Briar 0 | 0.3 % | 4.2 % |
| C | Lee Sin 52, Cho'Gath 29.1, Viego 18.8, Master Yi 0.2, Briar 0 | 0.3 % | 4.2 % |
| D | Lee Sin 52, Cho'Gath 29.1, Viego 18.8, Master Yi 0.2, Briar 0 | 0.3 % | 4.2 % |

### Jungle, joueur Master Yi régulier

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Cho'Gath 43.1, Lee Sin 34.1, Master Yi 11.3, Viego 11.2, Briar 0.3 | 37.5 % | 0 % |
| B | Master Yi 37.4, Cho'Gath 33.1, Lee Sin 29.2, Viego 0.4, Briar 0 | 0 % | 0.3 % |
| C | Master Yi 37.4, Cho'Gath 33.1, Lee Sin 29.2, Viego 0.4, Briar 0 | 0 % | 0.3 % |
| D | Master Yi 37.4, Cho'Gath 33.1, Lee Sin 29.2, Viego 0.4, Briar 0 | 0 % | 0.3 % |

### Top, joueur simple (Darius, Garen)

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Ornn 72.3, Malphite 17.6, Camille 8.6, Garen 1.4, Darius 0 | 34.8 % | 0 % |
| B | Darius 50.3, Ornn 37.7, Malphite 7.5, Garen 4, Camille 0.6 | 14.5 % | 50.3 % |
| C | Darius 50.3, Ornn 37.7, Malphite 7.5, Garen 4, Camille 0.6 | 14.5 % | 50.3 % |
| D | Darius 50.3, Ornn 37.7, Malphite 7.5, Garen 4, Camille 0.6 | 14.5 % | 50.3 % |

### ADC, joueur Vayne, note Riot élevée

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Vayne 62.1, Ezreal 30, Caitlyn 7.1, Kog'Maw 0.9, Jinx 0 | 15.3 % | 0 % |
| B | Vayne 84.5, Ezreal 14.2, Caitlyn 0.7, Kog'Maw 0.7, Jinx 0 | 0 % | 21 % |
| C | Vayne 84.5, Ezreal 14.2, Caitlyn 0.7, Kog'Maw 0.7, Jinx 0 | 0 % | 21 % |
| D | Vayne 84.5, Ezreal 14.2, Caitlyn 0.7, Kog'Maw 0.7, Jinx 0 | 0 % | 21 % |

### ADC, débutant déclaré sans historique

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Vayne 62.1, Ezreal 30, Caitlyn 7.1, Kog'Maw 0.9, Jinx 0 | 15.3 % | 0 % |
| B | Vayne 62.1, Ezreal 30, Caitlyn 7.1, Kog'Maw 0.9, Jinx 0 | 15.3 % | 0 % |
| C | Vayne 48.2, Ezreal 31.6, Kog'Maw 13.2, Caitlyn 7.1, Jinx 0 | 36.7 % | 0 % |
| D | Vayne 48.2, Ezreal 31.6, Kog'Maw 13.2, Caitlyn 7.1, Jinx 0 | 36.7 % | 0 % |

### Support, historique égal partout

| Modèle | Répartition | Égalité en tête | Changé |
|---|---|---|---|
| A | Leona 32.5, Thresh 31.7, Soraka 26.4, Lux 9.3, Lulu 0.3 | 58.6 % | 0 % |
| B | Leona 32.5, Thresh 31.7, Soraka 26.4, Lux 9.3, Lulu 0.3 | 58.6 % | 0 % |
| C | Leona 32.5, Thresh 31.7, Soraka 26.4, Lux 9.3, Lulu 0.3 | 58.6 % | 0 % |
| D | Leona 32.5, Thresh 31.7, Soraka 26.4, Lux 9.3, Lulu 0.3 | 58.6 % | 0 % |

## Exemples détaillés

### Mid, débutant sans historique

- **Adversaires visibles :** Malzahar, Ornn, Rengar, Samira, Pyke
- **En tête :** A Twisted Fate, B Twisted Fate, C Twisted Fate, D Twisted Fate

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Twisted Fate | 9/10 | 0 | 0 | 3.5 | 0 | 0 | -0.5 | **3** |
| Fizz | 6/10 | 0 | 0 | -0.75 | 0 | 0 | 0 | **-0.75** |
| Kog'Maw | 6/10 | 0 | 0 | -1.25 | 0 | 0 | 0 | **-1.25** |
| Xerath | 8/10 | 0 | 0 | -3.25 | 0 | 0 | -0.25 | **-3.5** |

**Explications, niveau Débutant :**

1. **Twisted Fate demande souvent de la pratique** Riot note sa difficulté 9/10 : il faut parfois plusieurs parties pour en tirer le meilleur. Si tu as envie de l'apprendre, c'est un choix valable. *Limite : Note parfois contredite par celle du client, et aucune courbe de maîtrise mesurée : une indication, pas une prédiction.*
1. **Aucun historique importé** Sans historique, DraftMid ne sait pas ce que tu connais déjà. Le conseil ne porte que sur la draft.

**Niveau Avancé, les trois premières :**

1. **Aucun historique importé** Pas de données personnelles : l'absence d'historique n'est jamais lue comme un manque d'expérience, sauf si tu te déclares débutant.

### Mid, joueur TF expérimenté

- **Adversaires visibles :** Malzahar, Ornn, Rengar, Samira, Pyke
- **En tête :** A Twisted Fate, B Twisted Fate, C Twisted Fate, D Twisted Fate

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Twisted Fate | 9/10 | 150 | 107.2 | 3.5 | 1.48 | 0.19 | 0 | **5.17** |
| Fizz | 6/10 | 20 | 15.1 | -0.75 | 0.68 | 0 | 0 | **-0.07** |
| Kog'Maw | 6/10 | 0 | 0 | -1.25 | 0 | 0 | 0 | **-1.25** |
| Xerath | 8/10 | 8 | 6.1 | -3.25 | 0.32 | 0 | -0.21 | **-3.14** |

**Explications, niveau Débutant :**

1. **Twisted Fate : ton expérience compte plus que sa difficulté** 150 parties dans ce rôle : la difficulté du champion ne pèse pas dans ce conseil.
1. **Twisted Fate : pas d'écart net avec tes autres champions** 80 victoires sur 150, contre 50 % sur tes autres champions de ce rôle. L'écart peut venir du hasard. *Limite : Corrélation sur tes parties : ce n'est pas une preuve de cause à effet.*

**Niveau Avancé, les trois premières :**

1. **Twisted Fate : ton expérience compte plus que sa difficulté** Terme de complexité nul au-delà de 40 parties utiles. Aucun bonus n'est donné parce que le champion est difficile.
1. **Twisted Fate : pas d'écart net avec tes autres champions** Écart ramené vers ta moyenne : +2,5 points de pourcentage. Il ne contrôle ni la date, ni le matchup, ni le niveau des parties.

### Mid, 5 parties Kog'Maw à 80 %, 150 Fizz à 55 %

- **Adversaires visibles :** Ornn, Sion, Maokai, Jinx, Thresh
- **En tête :** A Kog'Maw, B Kog'Maw, C Kog'Maw, D Kog'Maw

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Kog'Maw | 6/10 | 5 | 4.8 | 5.35 | 0.26 | 0 | 0 | **5.61** |
| Xerath | 8/10 | 0 | 0 | 1.8 | 0 | 0 | -0.25 | **1.55** |
| Twisted Fate | 9/10 | 0 | 0 | 1.5 | 0 | 0 | -0.5 | **1** |
| Fizz | 6/10 | 150 | 107.2 | -3.47 | 1.48 | 0.23 | 0 | **-1.75** |

**Explications, niveau Débutant :**

1. **5 parties sur Kog'Maw : trop peu pour conclure** 4 victoires sur 5 : trop peu pour savoir si ce champion te réussit.

**Niveau Avancé, les trois premières :**

1. **5 parties sur Kog'Maw : trop peu pour conclure** Intervalle à 95 % : 38 à 96 %. Seul le volume compte ici ; le résultat ne compte qu'à partir de 30 parties.

### Mid, Xerath en apprentissage

- **Adversaires visibles :** Sett, Vi
- **En tête :** A Twisted Fate, B Twisted Fate, C Twisted Fate, D Twisted Fate

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Twisted Fate | 9/10 | 40 | 31.1 | 2.85 | 1.07 | 0.01 | -0.11 | **3.81** |
| Kog'Maw | 6/10 | 0 | 0 | 2.21 | 0 | 0 | 0 | **2.21** |
| Fizz | 6/10 | 60 | 42.5 | -0.75 | 1.23 | 0.01 | 0 | **0.49** |
| Xerath | 8/10 | 3 | 2.9 | -0.69 | 0.17 | 0 | 0 | **-0.53** |

**Explications, niveau Débutant :**

1. **Twisted Fate : pas d'écart net avec tes autres champions** 20 victoires sur 40, contre 50 % sur tes autres champions de ce rôle. L'écart peut venir du hasard. *Limite : Corrélation sur tes parties : ce n'est pas une preuve de cause à effet.*

**Niveau Avancé, les trois premières :**

1. **Twisted Fate : pas d'écart net avec tes autres champions** Écart ramené vers ta moyenne : +0,1 points de pourcentage. Il ne contrôle ni la date, ni le matchup, ni le niveau des parties.

### Jungle, sans historique

- **Adversaires visibles :** Malzahar, Ornn, Rengar, Samira, Pyke
- **En tête :** A Lee Sin, B Lee Sin, C Lee Sin, D Lee Sin

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Lee Sin | 6/10 | 0 | 0 | 3 | 0 | 0 | 0 | **3** |
| Cho'Gath | 5/10 | 0 | 0 | 2 | 0 | 0 | 0 | **2** |
| Briar | 3/10 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |
| Master Yi | 4/10 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |
| Viego | 5/10 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |

**Explications, niveau Débutant :**

1. **Aucun historique importé** Sans historique, DraftMid ne sait pas ce que tu connais déjà. Le conseil ne porte que sur la draft.

**Niveau Avancé, les trois premières :**

1. **Aucun historique importé** Pas de données personnelles : l'absence d'historique n'est jamais lue comme un manque d'expérience, sauf si tu te déclares débutant.
1. **Lee Sin contre Rengar : aucune donnée fiable** Critère de matchup absent : aucune valeur n'est estimée à sa place.

### Jungle, joueur Lee Sin ancien

- **Adversaires visibles :** Darius, Syndra, Jinx, Thresh, Vi
- **En tête :** A Cho'Gath, B Viego, C Viego, D Viego

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Viego | 5/10 | 15 | 13.1 | 3 | 0.61 | 0 | 0 | **3.61** |
| Cho'Gath | 5/10 | 0 | 0 | 3 | 0 | 0 | 0 | **3** |
| Master Yi | 4/10 | 0 | 0 | 3 | 0 | 0 | 0 | **3** |
| Lee Sin | 6/10 | 200 | 50 | 0 | 1.3 | 0.22 | 0 | **1.51** |
| Briar | 3/10 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |

**Explications, niveau Débutant :**

1. **Cho'Gath pour la draft, Viego pour ton historique** Le choix t'appartient : Cho'Gath répond mieux à la draft, et ton historique penche vers Viego. *Limite : Le poids donné à l'expérience est un choix, pas une mesure.*
1. **Égalité stratégique : ton expérience peut trancher** Le classement ne sépare pas Viego et Cho'Gath. Tu as davantage d'expérience sur Viego : c'est un argument, si ce champion te convient. *Limite : L'expérience ne dit pas à elle seule si tu joues bien ce champion.*
1. **15 parties sur Viego : trop peu pour conclure** 8 victoires sur 15 : trop peu pour savoir si ce champion te réussit.

**Niveau Avancé, les trois premières :**

1. **Cho'Gath pour la draft, Viego pour ton historique** Viego passe devant avec les réglages de personnalisation, qui ne sont pas validés sur de vraies parties. Cho'Gath reste premier du moteur.
1. **Égalité stratégique : ton expérience peut trancher** Scores du moteur identiques pour Viego et Cho'Gath : l'écart ne vient que de l'expérience récente, qui penche vers Viego.
1. **15 parties sur Viego : trop peu pour conclure** Intervalle à 95 % : 30 à 75 %. Seul le volume compte ici ; le résultat ne compte qu'à partir de 30 parties.

### Jungle, joueur Master Yi régulier

- **Adversaires visibles :** Ornn, Sion, Maokai, Jinx, Thresh
- **En tête :** A Cho'Gath, B Master Yi, C Master Yi, D Master Yi

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Master Yi | 4/10 | 120 | 85.3 | 4 | 1.45 | 0.09 | 0 | **5.54** |
| Viego | 5/10 | 30 | 24.3 | 4 | 0.93 | -0.02 | 0 | **4.91** |
| Cho'Gath | 5/10 | 6 | 5.2 | 4 | 0.28 | 0 | 0 | **4.28** |
| Briar | 3/10 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |
| Lee Sin | 6/10 | 0 | 0 | -0.25 | 0 | 0 | 0 | **-0.25** |

**Explications, niveau Débutant :**

1. **Cho'Gath pour la draft, Master Yi pour ton historique** Le choix t'appartient : Cho'Gath répond mieux à la draft, et ton historique penche vers Master Yi. *Limite : Le poids donné à l'expérience est un choix, pas une mesure.*
1. **Égalité stratégique : ton expérience peut trancher** Le classement ne sépare pas Master Yi et Viego. Tu as davantage d'expérience sur Master Yi : c'est un argument, si ce champion te convient. *Limite : L'expérience ne dit pas à elle seule si tu joues bien ce champion.*
1. **Master Yi : pas d'écart net avec tes autres champions** 62 victoires sur 120, contre 50 % sur tes autres champions de ce rôle. L'écart peut venir du hasard. *Limite : Corrélation sur tes parties : ce n'est pas une preuve de cause à effet.*

**Niveau Avancé, les trois premières :**

1. **Cho'Gath pour la draft, Master Yi pour ton historique** Master Yi passe devant avec les réglages de personnalisation, qui ne sont pas validés sur de vraies parties. Cho'Gath reste premier du moteur.
1. **Égalité stratégique : ton expérience peut trancher** Scores du moteur identiques pour Master Yi et Viego : l'écart ne vient que de l'expérience récente, qui penche vers Master Yi.
1. **Master Yi : pas d'écart net avec tes autres champions** Écart ramené vers ta moyenne : +1,2 points de pourcentage. Il ne contrôle ni la date, ni le matchup, ni le niveau des parties.

### Top, joueur simple (Darius, Garen)

- **Adversaires visibles :** Sett, Vi
- **En tête :** A Camille, B Darius, C Darius, D Darius

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Darius | 2/10 | 100 | 71.1 | 3 | 1.41 | 0.1 | 0 | **4.51** |
| Camille | 4/10 | 0 | 0 | 3.5 | 0 | 0 | 0 | **3.5** |
| Ornn | 5/10 | 0 | 0 | 3 | 0 | 0 | 0 | **3** |
| Garen | 5/10 | 50 | 36.3 | 0 | 1.15 | -0.03 | 0 | **1.11** |
| Malphite | 2/10 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |

**Explications, niveau Débutant :**

1. **Camille pour la draft, Darius pour ton historique** Le choix t'appartient : Camille répond mieux à la draft, et ton historique penche vers Darius. *Limite : Le poids donné à l'expérience est un choix, pas une mesure.*
1. **Égalité stratégique : ton expérience peut trancher** Le classement ne sépare pas Darius et Ornn. Tu as davantage d'expérience sur Darius : c'est un argument, si ce champion te convient. *Limite : L'expérience ne dit pas à elle seule si tu joues bien ce champion.*
1. **Darius : pas d'écart net avec tes autres champions** 52 victoires sur 100, contre 50 % sur tes autres champions de ce rôle. L'écart peut venir du hasard. *Limite : Corrélation sur tes parties : ce n'est pas une preuve de cause à effet.*

**Niveau Avancé, les trois premières :**

1. **Camille pour la draft, Darius pour ton historique** Darius passe devant avec les réglages de personnalisation, qui ne sont pas validés sur de vraies parties. Camille reste premier du moteur.
1. **Égalité stratégique : ton expérience peut trancher** Scores du moteur identiques pour Darius et Ornn : l'écart ne vient que de l'expérience récente, qui penche vers Darius.
1. **Darius : pas d'écart net avec tes autres champions** Écart ramené vers ta moyenne : +1,3 points de pourcentage. Il ne contrôle ni la date, ni le matchup, ni le niveau des parties.

### ADC, joueur Vayne, note Riot élevée

- **Adversaires visibles :** Ornn, Lee Sin, Ahri, Caitlyn, Thresh
- **En tête :** A Vayne, B Vayne, C Vayne, D Vayne

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Vayne | 8/10 | 180 | 128.6 | 3 | 1.49 | 0.18 | 0 | **4.68** |
| Ezreal | 7/10 | 15 | 12.8 | 2.25 | 0.6 | 0 | 0 | **2.85** |
| Kog'Maw | 6/10 | 0 | 0 | 2.75 | 0 | 0 | 0 | **2.75** |
| Jinx | 6/10 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |

**Explications, niveau Débutant :**

1. **Vayne : ton expérience compte plus que sa difficulté** 180 parties dans ce rôle : la difficulté du champion ne pèse pas dans ce conseil.
1. **Vayne : pas d'écart net avec tes autres champions** 95 victoires sur 180, contre 50 % sur tes autres champions de ce rôle. L'écart peut venir du hasard. *Limite : Corrélation sur tes parties : ce n'est pas une preuve de cause à effet.*

**Niveau Avancé, les trois premières :**

1. **Vayne : ton expérience compte plus que sa difficulté** Terme de complexité nul au-delà de 40 parties utiles. Aucun bonus n'est donné parce que le champion est difficile.
1. **Vayne : pas d'écart net avec tes autres champions** Écart ramené vers ta moyenne : +2,5 points de pourcentage. Il ne contrôle ni la date, ni le matchup, ni le niveau des parties.
1. **Vayne contre Caitlyn : aucune donnée fiable** Critère de matchup absent : aucune valeur n'est estimée à sa place.

### ADC, débutant déclaré sans historique

- **Adversaires visibles :** Yasuo, Yone, Lee Sin, Jinx, Thresh
- **En tête :** A Vayne, B Vayne, C Vayne, D Vayne

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Vayne | 8/10 | 0 | 0 | 4 | 0 | 0 | -0.25 | **3.75** |
| Ezreal | 7/10 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |
| Kog'Maw | 6/10 | 0 | 0 | -0.25 | 0 | 0 | 0 | **-0.25** |
| Caitlyn | 6/10 | 0 | 0 | -4 | 0 | 0 | 0 | **-4** |

**Explications, niveau Débutant :**

1. **Vayne demande souvent de la pratique** Riot note sa difficulté 8/10 : il faut parfois plusieurs parties pour en tirer le meilleur. Si tu as envie de l'apprendre, c'est un choix valable. *Limite : Note parfois contredite par celle du client, et aucune courbe de maîtrise mesurée : une indication, pas une prédiction.*
1. **Aucun historique importé** Sans historique, DraftMid ne sait pas ce que tu connais déjà. Le conseil ne porte que sur la draft.

**Niveau Avancé, les trois premières :**

1. **Aucun historique importé** Pas de données personnelles : l'absence d'historique n'est jamais lue comme un manque d'expérience, sauf si tu te déclares débutant.
1. **Vayne contre Jinx : aucune donnée fiable** Critère de matchup absent : aucune valeur n'est estimée à sa place.

### Support, historique égal partout

- **Adversaires visibles :** Ornn, Sion, Maokai, Jinx, Thresh
- **En tête :** A Soraka, B Soraka, C Soraka, D Soraka

| Champion | Note Riot | Parties | Expérience utile | Moteur | Expérience | Résultats | Complexité | Total C |
|---|---|---|---|---|---|---|---|---|
| Soraka | 3/10 | 40 | 31.5 | 0.5 | 1.07 | 0 | 0 | **1.57** |
| Leona | 4/10 | 40 | 31.5 | 0 | 1.07 | 0 | 0 | **1.07** |
| Lulu | 5/10 | 40 | 31.5 | 0 | 1.07 | 0 | 0 | **1.07** |
| Lux | 5/10 | 40 | 31.5 | -1 | 1.07 | 0 | 0 | **0.07** |

**Explications, niveau Débutant :**

1. **Soraka : pas d'écart net avec tes autres champions** 20 victoires sur 40, contre 50 % sur tes autres champions de ce rôle. L'écart peut venir du hasard. *Limite : Corrélation sur tes parties : ce n'est pas une preuve de cause à effet.*
1. **Thresh peut surprendre** Ce matchup peut être difficile si tu ne connais pas les compétences de Thresh. Aucune rencontre avec Soraka dans ton historique. *Limite : Ta connaissance réelle n'est pas mesurée : tu as pu le jouer ou le rencontrer ailleurs.*

**Niveau Avancé, les trois premières :**

1. **Soraka : pas d'écart net avec tes autres champions** Écart ramené vers ta moyenne : +0,0 points de pourcentage. Il ne contrôle ni la date, ni le matchup, ni le niveau des parties.
1. **Thresh peut surprendre** Aucune rencontre sur ce couple : aucune tendance personnelle n'est tirée.
1. **Soraka contre Thresh : aucune donnée fiable** Critère de matchup absent : aucune valeur n'est estimée à sa place.

