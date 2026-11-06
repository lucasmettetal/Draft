# Exemples de conseils à relire

Généré par `tools/run-coaching-review.ps1`, avec les données de rôles locales. Situations choisies pour couvrir les cas, pas pour être représentatives.

- **Règle :** le contenu se déduit d'une donnée ou d'un calcul, et un test peut le vérifier.
- **À relire :** c'est un avis sur le jeu. Il faut un joueur pour dire s'il est juste.

## Niveau Débutant

### Débutant 1 : Mid, draft complète, mid connu

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Malzahar, Ornn, Rengar, Samira, Pyke
- **Conseil du moteur :** Twisted Fate, sinon Fizz ; scores : Twisted Fate 3.50, Fizz -0.75, Kog'Maw -1.25, Xerath -3.25
- **Adversaire direct :** Malzahar, confiance élevée
- **Raisons du moteur :** Matchup direct 1.5 ; Engages et contrôles 1.5 ; Pression sur la carte 0.5
- **Ligne courte :** Twisted Fate, sinon Fizz

1. **Pourquoi Twisted Fate** (Règle, Règle écrite à la main, vue courte). La raison principale : 2e choix contre Malzahar. Déclencheur : `[{"kind":"criterion","key":"matchup","value":1.5}]`.
1. **3 adversaires sautent sur leurs cibles** (À relire, Règle écrite à la main, vue courte). Rengar, Samira et Pyke cherchent des cibles fragiles. Avant d'avancer, vérifie où ils sont sur la carte. Déclencheur : `[{"kind":"champions","value":["Rengar","Samira","Pyke"]}]`.
1. **Twisted Fate contre Malzahar : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). Le tableau de DRAFT place Twisted Fate parmi les bons choix contre Malzahar. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":1.5}]`.
1. **Comprendre Twisted Fate** (À relire, Donnée officielle de Riot). Twisted Fate se joue à longue portée (525). Points forts : peut peser sur toute la carte. Attention : a peu de prise sur les joueurs solides. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Marksman"],"range":525,"difficulty":9}]`.
1. **Twisted Fate peut demander de la pratique** (Règle, Donnée officielle de Riot). Riot note sa difficulté 9/10. Si tu découvres ce champion, prévois un temps de prise en main. Le choix reste le tien. *Limite : Note de Riot non documentée et parfois contredite par celle du client : une indication, pas une mesure.* Déclencheur : `[{"kind":"official","difficulty":9}]`.
1. **Pourquoi les contrôles comptent** (À relire, Principe général du jeu). Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir.
1. **Se placer avant le combat** (À relire, Principe général du jeu). Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière un allié solide : le joueur touché en premier a peu de chances de finir le combat.

### Débutant 2 : Mid, matchup à longue portée

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Syndra, Ornn, Lee Sin, Jinx, Thresh
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 4.25, Kog'Maw 2.75, Twisted Fate 2.00
- **Adversaire direct :** Syndra, confiance élevée
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Pourquoi Fizz** (Règle, Règle écrite à la main, vue courte). La raison principale : 1er choix contre Syndra. Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Syndra te touche de plus loin** (À relire, Donnée officielle de Riot, vue courte). Syndra attaque à 550, toi à 175. Pour farmer, tu dois t'approcher et risques d'être touché. Avance quand ses sorts viennent d'être utilisés. *Limite : Portée d'attaque de base seulement : les sorts ne sont pas comptés.* Déclencheur : `[{"kind":"range","mine":175,"theirs":550}]`.
1. **Fizz contre Syndra : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). Le tableau de DRAFT place Fizz parmi les bons choix contre Syndra. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Comprendre Fizz** (À relire, Donnée officielle de Riot). Fizz se joue à corps à corps (175). Points forts : peut tuer une cible fragile. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Assassin","Fighter"],"range":175,"difficulty":6}]`.
1. **Jouer avec la portée** (À relire, Principe général du jeu). Le plus court doit s'approcher pour toucher : il ne doit le faire que lorsqu'il y gagne quelque chose. Le plus long doit le punir à chaque fois qu'il avance pour un sbire.
1. **Pourquoi les contrôles comptent** (À relire, Principe général du jeu). Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir.

### Débutant 3 : Mid, adversaire incertain

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Yasuo, Yone, Lee Sin, Jinx, Thresh
- **Conseil du moteur :** Kog'Maw, sinon Fizz ; scores : Kog'Maw 1.79, Fizz 0.47, Twisted Fate -2.01, Xerath -6.50
- **Adversaire direct :** Yasuo, confiance moyenne
- **Raisons du moteur :** Matchup direct 2.0 ; Tanks et bruisers 4.0 ; Survie aux plongeurs -4.0 ; Pression sur la carte -0.3
- **Ligne courte :** Kog'Maw, sinon Fizz

1. **Kog'Maw ressort malgré un vrai risque** (Règle, Règle écrite à la main, vue courte). Le risque : 3 plongeurs contre un champion immobile : Yasuo, Yone, Lee Sin. Kog'Maw reste devant car le reste de la draft compense, par rapport à tes autres champions. Déclencheur : `[{"kind":"criterion","key":"dive","value":-4}]`.
1. **Kog'Maw ou Fizz : le choix est serré** (Règle, Règle écrite à la main, vue courte). Les deux peuvent convenir. Kog'Maw passe devant : 3 tanks ou bruisers : ses dégâts en % des PV font la différence. Prends celui avec lequel tu es le plus à l'aise. Déclencheur : `[{"kind":"criterion","key":"front","value":5}]`.
1. **Yasuo, Yone et Lee Sin peuvent atteindre Kog'Maw** (À relire, Règle écrite à la main, vue courte). Yasuo, Yone et Lee Sin peuvent venir sur toi d'un coup, et Kog'Maw s'échappe mal. Joue près d'un allié qui peut te protéger. Déclencheur : `[{"kind":"champions","value":["Yasuo","Yone","Lee Sin"]}]`.
1. **Kog'Maw contre Yasuo (supposé) : plutôt un bon choix** (Règle, Règle écrite à la main). Le tableau de DRAFT place Kog'Maw parmi les bons choix contre Yasuo. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":2.04}]`.
1. **Mid supposé, à vérifier** (Règle, Estimation). DRAFT pense que c'est Yasuo, sans certitude. Si tu sais que c'est un autre champion, corrige-le.
1. **Dégâts surtout physiques en face** (À relire, Estimation). Leurs principaux dégâts semblent physiques. Un objet qui protège des dégâts physiques peut donc être plus utile que d'habitude : regarde lesquels existent. *Limite : Estimation, pas une mesure : les dégâts réels dépendent des objets.* Déclencheur : `[{"kind":"damage","value":"physiques","champions":4}]`.
1. **Comprendre Kog'Maw** (À relire, Donnée officielle de Riot). Kog'Maw se joue à longue portée (500). Points forts : fait de bons dégâts aux joueurs solides. Attention : se déplace peu, donc un adversaire mobile peut l'atteindre. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Marksman","Mage"],"range":500,"difficulty":6}]`.

### Débutant 4 : Mid, beaucoup de plongeurs, champion immobile

- **Rôle et pool :** mid, Xerath, Kog'Maw
- **Adversaires visibles :** Zed, Rengar, Katarina, Jinx, Thresh
- **Conseil du moteur :** Xerath, sinon Kog'Maw ; scores : Xerath -8.00, Kog'Maw -8.25
- **Adversaire direct :** Katarina, confiance élevée
- **Raisons du moteur :** Matchup direct -4.0 ; Survie aux plongeurs -4.0
- **Ligne courte :** Xerath, sinon Kog'Maw

1. **Xerath ou Kog'Maw : le choix est serré** (Règle, Règle écrite à la main, vue courte). Les deux peuvent convenir. Xerath passe devant : avantage sur « Pression sur la carte ». Prends celui avec lequel tu es le plus à l'aise. Déclencheur : `[{"kind":"criterion","key":"map","value":0.25}]`.
1. **Zed, Rengar et Katarina peuvent atteindre Xerath** (À relire, Règle écrite à la main, vue courte). Zed, Rengar et Katarina peuvent venir sur toi d'un coup, et Xerath s'échappe mal. Joue près d'un allié qui peut te protéger. Déclencheur : `[{"kind":"champions","value":["Zed","Rengar","Katarina"]}]`.
1. **Xerath contre Katarina : un matchup difficile** (Règle, Règle écrite à la main, vue courte). Le tableau de DRAFT déconseille Xerath contre Katarina. Si ce choix reste devant, c'est pour le reste de la draft. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":-4}]`.
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main). Aucun des 5 adversaires n'encaisse vraiment : si ton équipe les atteint, le combat peut basculer vite. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Comprendre Xerath** (À relire, Donnée officielle de Riot). Xerath se joue à longue portée (525). Points forts : touche de très loin. Attention : se déplace peu, donc un adversaire mobile peut l'atteindre. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Support"],"range":525,"difficulty":8}]`.
1. **Xerath peut demander de la pratique** (Règle, Donnée officielle de Riot). Riot note sa difficulté 8/10. Si tu découvres ce champion, prévois un temps de prise en main. Le choix reste le tien. *Limite : Note de Riot non documentée et parfois contredite par celle du client : une indication, pas une mesure.* Déclencheur : `[{"kind":"official","difficulty":8}]`.
1. **Se placer avant le combat** (À relire, Principe général du jeu). Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière un allié solide : le joueur touché en premier a peu de chances de finir le combat.

### Débutant 5 : Mid, draft incomplète

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Sett, Vi
- **Conseil du moteur :** Twisted Fate, sinon Kog'Maw ; scores : Twisted Fate 2.85, Kog'Maw 2.21, Xerath -0.69, Fizz -0.75
- **Adversaire direct :** non identifié, confiance faible
- **Raisons du moteur :** Sécurité sans adversaire connu 0.8 ; Engages et contrôles 1.5 ; Pression sur la carte 0.5
- **Ligne courte :** Twisted Fate, sinon Kog'Maw

1. **Twisted Fate ou Kog'Maw : le choix est serré** (Règle, Règle écrite à la main, vue courte). Les deux peuvent convenir. Twisted Fate passe devant : peut jouer loin de leur engage et revenir avec son ultime. Prends celui avec lequel tu es le plus à l'aise. Déclencheur : `[{"kind":"criterion","key":"control","value":1.5}]`.
1. **Mid pas encore visible** (Règle, Estimation, vue courte). Tant qu'il n'est pas visible, le conseil ne porte pas sur ton duel. Si tu peux, choisis après l'avoir vu. *Limite : Déduction d'après les rôles habituels : ce n'est pas une information du client.*
1. **Comprendre Twisted Fate** (À relire, Donnée officielle de Riot, vue courte). Twisted Fate se joue à longue portée (525). Points forts : peut peser sur toute la carte. Attention : a peu de prise sur les joueurs solides. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Marksman"],"range":525,"difficulty":9}]`.
1. **Twisted Fate peut demander de la pratique** (Règle, Donnée officielle de Riot). Riot note sa difficulté 9/10. Si tu découvres ce champion, prévois un temps de prise en main. Le choix reste le tien. *Limite : Note de Riot non documentée et parfois contredite par celle du client : une indication, pas une mesure.* Déclencheur : `[{"kind":"official","difficulty":9}]`.
1. **Il reste 3 adversaires à voir** (Règle, Principe général du jeu). Ce conseil peut changer. Attends si tu peux, avant de te décider.
1. **Pourquoi les contrôles comptent** (À relire, Principe général du jeu). Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir.
1. **Se placer avant le combat** (À relire, Principe général du jeu). Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière un allié solide : le joueur touché en premier a peu de chances de finir le combat.

### Débutant 6 : Mid, équipe sans frontline

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Lux, Jinx, Syndra, Caitlyn, Soraka
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 5.25, Twisted Fate 0.50, Kog'Maw -1.25
- **Adversaire direct :** Syndra, confiance moyenne
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Fizz ou Xerath : le choix est serré** (Règle, Règle écrite à la main, vue courte). Les deux peuvent convenir. Fizz passe devant : 4 cibles fragiles : Lux, Jinx, Syndra et 1 autre. Prends celui avec lequel tu es le plus à l'aise. Déclencheur : `[{"kind":"criterion","key":"targets","value":2.25}]`.
1. **Syndra te touche de plus loin** (À relire, Donnée officielle de Riot, vue courte). Syndra attaque à 550, toi à 175. Pour farmer, tu dois t'approcher et risques d'être touché. Avance quand ses sorts viennent d'être utilisés. *Limite : Portée d'attaque de base seulement : les sorts ne sont pas comptés.* Déclencheur : `[{"kind":"range","mine":175,"theirs":550}]`.
1. **Fizz contre Syndra (supposé) : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). Le tableau de DRAFT place Fizz parmi les bons choix contre Syndra. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main). Aucun des 5 adversaires n'encaisse vraiment : si ton équipe les atteint, le combat peut basculer vite. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Mid supposé, à vérifier** (Règle, Estimation). DRAFT pense que c'est Syndra, sans certitude. Si tu sais que c'est un autre champion, corrige-le.
1. **Lux, Syndra et Caitlyn touchent de très loin** (À relire, Règle écrite à la main). Ils t'abîment de loin avant le combat. Ne reste pas à découvert sans raison : ta vie baisse pour rien. Déclencheur : `[{"kind":"champions","value":["Lux","Syndra","Caitlyn"]}]`.
1. **Comprendre Fizz** (À relire, Donnée officielle de Riot). Fizz se joue à corps à corps (175). Points forts : peut tuer une cible fragile. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Assassin","Fighter"],"range":175,"difficulty":6}]`.

### Débutant 7 : Mid, dégâts surtout magiques

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Syndra, Ahri, Brand, Veigar, Thresh
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 3.46, Twisted Fate 1.29, Kog'Maw -2.34
- **Adversaire direct :** non identifié, confiance faible
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Pourquoi Fizz** (Règle, Règle écrite à la main, vue courte). La raison principale : 1er choix contre Ahri · 1er choix contre Syndra. Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Mid pas encore identifié : prépare les deux cas** (Règle, Estimation, vue courte). Ce peut être Ahri ou Syndra. Les deux : longue portée. Regarde les sorts d'invocateur et le placement au début de la partie. *Limite : Déduction d'après les rôles habituels : ce n'est pas une information du client.*
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main, vue courte). Aucun des 5 adversaires n'encaisse vraiment : si ton équipe les atteint, le combat peut basculer vite. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Dégâts surtout magiques en face** (À relire, Estimation). Leurs principaux dégâts semblent magiques. Un objet qui protège des dégâts magiques peut donc être plus utile que d'habitude : regarde lesquels existent. *Limite : Estimation, pas une mesure : les dégâts réels dépendent des objets.* Déclencheur : `[{"kind":"damage","value":"magiques","champions":4}]`.
1. **Comprendre Fizz** (À relire, Donnée officielle de Riot). Fizz se joue à corps à corps (175). Points forts : peut tuer une cible fragile. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Assassin","Fighter"],"range":175,"difficulty":6}]`.
1. **Pourquoi les contrôles comptent** (À relire, Principe général du jeu). Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir.
1. **Le rôle du mid** (À relire, Principe général du jeu). Ta voie est courte et centrale : tu peux aider les deux côtés. Pousse ta vague d'abord, puis regarde où ton aide changerait quelque chose.

### Débutant 8 : Jungle, égalité entre fiches identiques

- **Rôle et pool :** jungle, Viego, Master Yi, Briar, Lee Sin
- **Adversaires visibles :** Ornn, Sion, Maokai, Jinx, Thresh
- **Conseil du moteur :** Master Yi, sinon Viego ; scores : Master Yi 4.00, Viego 4.00, Briar 0.00, Lee Sin -0.25
- **Adversaire direct :** Maokai, confiance moyenne
- **Raisons du moteur :** Tanks et bruisers 4.0
- **Ligne courte :** Égalité : Master Yi et Viego

1. **Égalité : Master Yi et Viego** (Règle, Règle écrite à la main, vue courte). Le classement ne les départage pas. Choisis celui que tu connais le mieux. *Limite : Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu.* Déclencheur : `[{"kind":"tie","picks":["Master Yi","Viego"],"score":4}]`.
1. **Maokai encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Maokai est solide, mais Master Yi fait de bons dégâts aux joueurs solides. Tuer Maokai prend du temps : n'en fais pas ta priorité tout de suite. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Ornn, Sion et Maokai** (À relire, Règle écrite à la main, vue courte). Ornn, Sion et Maokai encaissent beaucoup, et Master Yi fait de bons dégâts à ce type de cible. Si l'un d'eux te barre la route, tes coups restent utiles. Déclencheur : `[{"kind":"champions","value":["Ornn","Sion","Maokai"]}]`.
1. **Jungle supposé, à vérifier** (Règle, Estimation). DRAFT pense que c'est Maokai, sans certitude. Si tu sais que c'est un autre champion, corrige-le.
1. **Ornn, Sion, Maokai et 1 autre peuvent lancer le combat** (À relire, Règle écrite à la main). Ils peuvent démarrer un combat d'un coup. Évite de rester seul dans une zone que tu ne vois pas, surtout près d'un objectif. Déclencheur : `[{"kind":"champions","value":["Ornn","Sion","Maokai","Thresh"]}]`.
1. **Comprendre Master Yi** (À relire, Donnée officielle de Riot). Master Yi se joue à corps à corps (175). Points forts : fait de bons dégâts aux joueurs solides. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Fighter","Assassin"],"range":175,"difficulty":4}]`.
1. **Pourquoi les contrôles comptent** (À relire, Principe général du jeu). Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir.

### Débutant 9 : Jungle, historique personnel fictif

- **Rôle et pool :** jungle, Viego, Cho'Gath
- **Adversaires visibles :** Ornn, Vi, Ahri, Jinx, Thresh
- **Conseil du moteur :** Cho'Gath, sinon Viego ; scores : Cho'Gath 3.00, Viego 3.00
- **Adversaire direct :** Vi, confiance élevée
- **Raisons du moteur :** Tanks et bruisers 3.0
- **Ligne courte :** Égalité : Cho'Gath et Viego

1. **Égalité : Cho'Gath et Viego** (Règle, Règle écrite à la main, vue courte). Le classement ne les départage pas. Choisis celui que tu connais le mieux. *Limite : Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu.* Déclencheur : `[{"kind":"tie","picks":["Cho'Gath","Viego"],"score":3}]`.
1. **Tu as plus d'expérience sur Viego** (Règle, Mesuré sur de vraies parties, vue courte). 5 parties sur Cho'Gath, 150 sur Viego. Connaître son champion compte : à toi de voir si cela change ton choix. *Limite : Le nombre de parties ne dit pas si tu joues bien ce champion.* Déclencheur : `[{"kind":"games","pick":5,"rival":150}]`.
1. **Vi peut te sauter dessus** (À relire, Règle écrite à la main, vue courte). Vi cherche à te surprendre et à te tuer vite. Garde de quoi te défendre tant que ses sorts pour s'approcher sont disponibles. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Ornn, Vi et Thresh peuvent lancer le combat** (À relire, Règle écrite à la main). Ils peuvent démarrer un combat d'un coup. Évite de rester seul dans une zone que tu ne vois pas, surtout près d'un objectif. Déclencheur : `[{"kind":"champions","value":["Ornn","Vi","Thresh"]}]`.
1. **Comprendre Cho'Gath** (À relire, Donnée officielle de Riot). Cho'Gath se joue à corps à corps (125). Points forts : fait de bons dégâts aux joueurs solides et encaisse bien. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Tank","Mage"],"range":125,"difficulty":5}]`.
1. **Pourquoi les contrôles comptent** (À relire, Principe général du jeu). Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir.
1. **Se placer avant le combat** (À relire, Principe général du jeu). Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière un allié solide : le joueur touché en premier a peu de chances de finir le combat.

### Débutant 10 : Top, beaucoup de contrôles

- **Rôle et pool :** top, Darius, Malphite, Camille
- **Adversaires visibles :** Sion, Lee Sin, Syndra, Jinx, Leona
- **Conseil du moteur :** Camille, sinon Darius ; scores : Camille 4.50, Darius 4.00, Malphite 1.50
- **Adversaire direct :** Sion, confiance élevée
- **Raisons du moteur :** Tanks et bruisers 4.0 ; Pression sur la carte 0.5
- **Ligne courte :** Camille, sinon Darius

1. **Camille ou Darius : le choix est serré** (Règle, Règle écrite à la main, vue courte). Les deux peuvent convenir. Camille passe devant : peut peser sur toute la carte. Prends celui avec lequel tu es le plus à l'aise. Déclencheur : `[{"kind":"criterion","key":"map","value":0.5}]`.
1. **Sion encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Sion est solide, mais Camille fait de bons dégâts aux joueurs solides. Tuer Sion prend du temps : n'en fais pas ta priorité tout de suite. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Sion et Leona** (À relire, Règle écrite à la main, vue courte). Sion et Leona encaissent beaucoup, et Camille fait de bons dégâts à ce type de cible. Si l'un d'eux te barre la route, tes coups restent utiles. Déclencheur : `[{"kind":"champions","value":["Sion","Leona"]}]`.
1. **Syndra et Leona peuvent t'immobiliser** (À relire, Règle écrite à la main). Un seul de leurs sorts peut t'empêcher d'agir : évite d'être le premier à portée quand tu ne sais pas où ils sont. Déclencheur : `[{"kind":"champions","value":["Syndra","Leona"]}]`.
1. **Comprendre Camille** (À relire, Donnée officielle de Riot). Camille se joue à corps à corps (125). Points forts : fait de bons dégâts aux joueurs solides et peut peser sur toute la carte. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Fighter","Assassin"],"range":125,"difficulty":4}]`.
1. **Pourquoi les contrôles comptent** (À relire, Principe général du jeu). Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir.
1. **Se placer avant le combat** (À relire, Principe général du jeu). Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière un allié solide : le joueur touché en premier a peu de chances de finir le combat.

### Débutant 11 : ADC, adversaire direct connu

- **Rôle et pool :** adc, Jinx, Ezreal
- **Adversaires visibles :** Ornn, Lee Sin, Ahri, Caitlyn, Thresh
- **Conseil du moteur :** Ezreal, sinon Jinx ; scores : Ezreal 2.25, Jinx 0.00
- **Adversaire direct :** Caitlyn, confiance élevée
- **Raisons du moteur :** Cibles exploitables 0.8 ; Engages et contrôles 1.5
- **Ligne courte :** Ezreal, sinon Jinx

1. **Pourquoi Ezreal** (Règle, Règle écrite à la main, vue courte). La raison principale : peut jouer loin de leur engage et de leurs contrôles. Déclencheur : `[{"kind":"criterion","key":"control","value":1.5}]`.
1. **Comprendre Ezreal** (À relire, Donnée officielle de Riot, vue courte). Ezreal se joue à longue portée (550). Points forts : touche de très loin. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Marksman","Mage"],"range":550,"difficulty":7}]`.
1. **Se placer avant le combat** (À relire, Principe général du jeu). Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière un allié solide : le joueur touché en premier a peu de chances de finir le combat.
1. **Le rôle de l'ADC** (À relire, Principe général du jeu). Tu fais beaucoup de dégâts mais tu es fragile. Ton but en combat : rester en vie et frapper ce qui est à portée, pas aller chercher une cible lointaine.

### Débutant 12 : Support, tank adverse direct

- **Rôle et pool :** support, Soraka, Leona, Lux
- **Adversaires visibles :** Ornn, Lee Sin, Ahri, Jinx, Alistar
- **Conseil du moteur :** Leona, sinon Lux ; scores : Leona 1.50, Lux 0.75, Soraka 0.50
- **Adversaire direct :** Alistar, confiance manuel
- **Raisons du moteur :** Cibles exploitables 1.5
- **Ligne courte :** Leona, sinon Lux

1. **Leona ou Lux : le choix est serré** (Règle, Règle écrite à la main, vue courte). Les deux peuvent convenir. Leona passe devant : 2 cibles fragiles à attraper : Ahri, Jinx. Prends celui avec lequel tu es le plus à l'aise. Déclencheur : `[{"kind":"criterion","key":"targets","value":0.75}]`.
1. **Alistar encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Tu mettras du temps à tuer Alistar. Ne t'acharne pas : regarde s'il y a une cible plus fragile à ta portée. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Ornn et Alistar** (À relire, Règle écrite à la main, vue courte). Ornn et Alistar encaissent beaucoup. Un combat long les avantage si ton équipe manque de dégâts. Déclencheur : `[{"kind":"champions","value":["Ornn","Alistar"]}]`.
1. **Comprendre Leona** (À relire, Donnée officielle de Riot). Leona se joue à corps à corps (125). Points forts : encaisse bien et peut lancer les combats. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Tank","Support"],"range":125,"difficulty":4}]`.
1. **Pourquoi les contrôles comptent** (À relire, Principe général du jeu). Un joueur immobilisé ne peut ni fuir ni attaquer. Quand un adversaire vient de rater ou d'utiliser son contrôle, c'est souvent le bon moment pour agir.
1. **Se placer avant le combat** (À relire, Principe général du jeu). Avant un combat, demande-toi qui peut t'atteindre. Si tu es fragile, reste derrière un allié solide : le joueur touché en premier a peu de chances de finir le combat.

## Niveau Intermédiaire

### Intermédiaire 1 : Mid, draft complète, mid connu

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Malzahar, Ornn, Rengar, Samira, Pyke
- **Conseil du moteur :** Twisted Fate, sinon Fizz ; scores : Twisted Fate 3.50, Fizz -0.75, Kog'Maw -1.25, Xerath -3.25
- **Adversaire direct :** Malzahar, confiance élevée
- **Raisons du moteur :** Matchup direct 1.5 ; Engages et contrôles 1.5 ; Pression sur la carte 0.5
- **Ligne courte :** Twisted Fate, sinon Fizz

1. **Pourquoi Twisted Fate** (Règle, Règle écrite à la main, vue courte). « Matchup direct » pèse le plus : 2e choix contre Malzahar. Déclencheur : `[{"kind":"criterion","key":"matchup","value":1.5}]`.
1. **3 adversaires sautent sur leurs cibles** (À relire, Règle écrite à la main, vue courte). Leurs plongeurs punissent un mauvais placement : quand ils ne sont pas visibles, considère qu'ils peuvent arriver. Déclencheur : `[{"kind":"champions","value":["Rengar","Samira","Pyke"]}]`.
1. **Twisted Fate contre Malzahar : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). 2e choix contre Malzahar, selon un tableau écrit à la main. Ce critère est le plus lourd du classement. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":1.5}]`.
1. **Quand préférer Fizz ?** (À relire, Règle écrite à la main). Fizz reste meilleur sur « Cibles exploitables » : 2 cibles fragiles : Malzahar, Samira. Si ce point compte le plus pour ta partie, ce choix devient intéressant. Déclencheur : `[{"kind":"criterion","key":"targets","value":3}]`.
1. **Comprendre Twisted Fate** (À relire, Donnée officielle de Riot). Classes Riot : Mage, Marksman. Portée 525. Points forts : peut peser sur toute la carte. Attention : a peu de prise sur les joueurs solides. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Marksman"],"range":525,"difficulty":9}]`.

### Intermédiaire 2 : Mid, matchup à longue portée

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Syndra, Ornn, Lee Sin, Jinx, Thresh
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 4.25, Kog'Maw 2.75, Twisted Fate 2.00
- **Adversaire direct :** Syndra, confiance élevée
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Pourquoi Fizz** (Règle, Règle écrite à la main, vue courte). « Matchup direct » pèse le plus : 1er choix contre Syndra. Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Syndra te touche de plus loin** (À relire, Donnée officielle de Riot, vue courte). Écart de portée de 375 (175 contre 550). Choisis tes échanges : engage juste après un sort important de Syndra, puis ressors. *Limite : Portée d'attaque de base seulement : les sorts ne sont pas comptés.* Déclencheur : `[{"kind":"range","mine":175,"theirs":550}]`.
1. **Fizz contre Syndra : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). 1er choix contre Syndra, selon un tableau écrit à la main. Ce critère est le plus lourd du classement. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Quand préférer Xerath ?** (À relire, Règle écrite à la main). Xerath reste meilleur sur « Protection nécessaire » : Peu de plongeurs et de mobilité, et un frontline devant toi. Si ce point compte le plus pour ta partie, ce choix devient intéressant. Déclencheur : `[{"kind":"criterion","key":"peel","value":2}]`.
1. **Comprendre Fizz** (À relire, Donnée officielle de Riot). Classes Riot : Assassin, Fighter. Portée 175. Points forts : peut tuer une cible fragile. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Assassin","Fighter"],"range":175,"difficulty":6}]`.

### Intermédiaire 3 : Mid, adversaire incertain

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Yasuo, Yone, Lee Sin, Jinx, Thresh
- **Conseil du moteur :** Kog'Maw, sinon Fizz ; scores : Kog'Maw 1.79, Fizz 0.47, Twisted Fate -2.01, Xerath -6.50
- **Adversaire direct :** Yasuo, confiance moyenne
- **Raisons du moteur :** Matchup direct 2.0 ; Tanks et bruisers 4.0 ; Survie aux plongeurs -4.0 ; Pression sur la carte -0.3
- **Ligne courte :** Kog'Maw, sinon Fizz

1. **Kog'Maw ressort malgré un vrai risque** (Règle, Règle écrite à la main, vue courte). Le score reste le meilleur, mais « Survie aux plongeurs » pèse contre ce choix : 3 plongeurs contre un champion immobile : Yasuo, Yone, Lee Sin. Vérifie que tu peux jouer avec cette contrainte. Déclencheur : `[{"kind":"criterion","key":"dive","value":-4}]`.
1. **Kog'Maw ou Fizz : le choix est serré** (Règle, Règle écrite à la main, vue courte). Kog'Maw mène surtout sur « Tanks et bruisers » : 3 tanks ou bruisers : ses dégâts en % des PV font la différence. L'écart reste faible : la maîtrise et ton équipe peuvent décider. Déclencheur : `[{"kind":"criterion","key":"front","value":5}]`.
1. **Yasuo, Yone et Lee Sin peuvent atteindre Kog'Maw** (À relire, Règle écrite à la main, vue courte). Kog'Maw a peu de sorties contre Yasuo, Yone et Lee Sin. Reste dans la zone de protection de ton équipe, et repère-les avant d'avancer. Déclencheur : `[{"kind":"champions","value":["Yasuo","Yone","Lee Sin"]}]`.
1. **Kog'Maw contre Yasuo (supposé) : plutôt un bon choix** (Règle, Règle écrite à la main). 2e choix contre Yasuo · 1er choix contre Yone, selon un tableau écrit à la main. Ce critère est le plus lourd du classement. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":2.04}]`.
1. **Mid supposé, à vérifier** (Règle, Estimation). Confiance moyenne : déduit des rôles habituels. Une correction change le matchup pris en compte.
1. **Dégâts surtout physiques en face** (À relire, Estimation). Répartition déséquilibrée sur 4 champions : une résistance adaptée a plus de valeur. Vérifie en partie, car les objets adverses changent le partage. *Limite : Estimation, pas une mesure : les dégâts réels dépendent des objets.* Déclencheur : `[{"kind":"damage","value":"physiques","champions":4}]`.

### Intermédiaire 4 : Mid, beaucoup de plongeurs, champion immobile

- **Rôle et pool :** mid, Xerath, Kog'Maw
- **Adversaires visibles :** Zed, Rengar, Katarina, Jinx, Thresh
- **Conseil du moteur :** Xerath, sinon Kog'Maw ; scores : Xerath -8.00, Kog'Maw -8.25
- **Adversaire direct :** Katarina, confiance élevée
- **Raisons du moteur :** Matchup direct -4.0 ; Survie aux plongeurs -4.0
- **Ligne courte :** Xerath, sinon Kog'Maw

1. **Xerath ou Kog'Maw : le choix est serré** (Règle, Règle écrite à la main, vue courte). Xerath mène surtout sur « Pression sur la carte ». L'écart reste faible : la maîtrise et ton équipe peuvent décider. Déclencheur : `[{"kind":"criterion","key":"map","value":0.25}]`.
1. **Zed, Rengar et Katarina peuvent atteindre Xerath** (À relire, Règle écrite à la main, vue courte). Xerath a peu de sorties contre Zed, Rengar et Katarina. Reste dans la zone de protection de ton équipe, et repère-les avant d'avancer. Déclencheur : `[{"kind":"champions","value":["Zed","Rengar","Katarina"]}]`.
1. **Xerath contre Katarina : un matchup difficile** (Règle, Règle écrite à la main, vue courte). À éviter contre Katarina, selon un tableau écrit à la main. Ce critère est le plus lourd du classement. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":-4}]`.
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main). Sans frontline, leurs joueurs fragiles sont exposés dès que quelqu'un les atteint. À l'inverse, ils peuvent te tuer vite si tu arrives seul. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Comprendre Xerath** (À relire, Donnée officielle de Riot). Classes Riot : Mage, Support. Portée 525. Points forts : touche de très loin. Attention : se déplace peu, donc un adversaire mobile peut l'atteindre. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Support"],"range":525,"difficulty":8}]`.

### Intermédiaire 5 : Mid, draft incomplète

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Sett, Vi
- **Conseil du moteur :** Twisted Fate, sinon Kog'Maw ; scores : Twisted Fate 2.85, Kog'Maw 2.21, Xerath -0.69, Fizz -0.75
- **Adversaire direct :** non identifié, confiance faible
- **Raisons du moteur :** Sécurité sans adversaire connu 0.8 ; Engages et contrôles 1.5 ; Pression sur la carte 0.5
- **Ligne courte :** Twisted Fate, sinon Kog'Maw

1. **Twisted Fate ou Kog'Maw : le choix est serré** (Règle, Règle écrite à la main, vue courte). Twisted Fate mène surtout sur « Engages et contrôles » : peut jouer loin de leur engage et revenir avec son ultime. L'écart reste faible : la maîtrise et ton équipe peuvent décider. Déclencheur : `[{"kind":"criterion","key":"control","value":1.5}]`.
1. **Mid pas encore visible** (Règle, Estimation, vue courte). Le critère de matchup ne s'applique pas encore : le conseil repose sur le reste de la composition et sur la sécurité générale du pick. *Limite : Déduction d'après les rôles habituels : ce n'est pas une information du client.*
1. **Quand préférer Kog'Maw ?** (À relire, Règle écrite à la main, vue courte). Kog'Maw reste meilleur sur « Tanks et bruisers » : 2 tanks ou bruisers : ses dégâts en % des PV sont utiles. Si ce point compte le plus pour ta partie, ce choix devient intéressant. Déclencheur : `[{"kind":"criterion","key":"front","value":3}]`.
1. **Comprendre Twisted Fate** (À relire, Donnée officielle de Riot). Classes Riot : Mage, Marksman. Portée 525. Points forts : peut peser sur toute la carte. Attention : a peu de prise sur les joueurs solides. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Marksman"],"range":525,"difficulty":9}]`.
1. **Il reste 3 adversaires à voir** (Règle, Principe général du jeu). La lecture de la composition se fait sur 2 champions seulement : les comptes de frontline et de contrôles peuvent encore bouger.

### Intermédiaire 6 : Mid, équipe sans frontline

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Lux, Jinx, Syndra, Caitlyn, Soraka
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 5.25, Twisted Fate 0.50, Kog'Maw -1.25
- **Adversaire direct :** Syndra, confiance moyenne
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Fizz ou Xerath : le choix est serré** (Règle, Règle écrite à la main, vue courte). Fizz mène surtout sur « Cibles exploitables » : 4 cibles fragiles : Lux, Jinx, Syndra et 1 autre. L'écart reste faible : la maîtrise et ton équipe peuvent décider. Déclencheur : `[{"kind":"criterion","key":"targets","value":2.25}]`.
1. **Syndra te touche de plus loin** (À relire, Donnée officielle de Riot, vue courte). Écart de portée de 375 (175 contre 550). Choisis tes échanges : engage juste après un sort important de Syndra, puis ressors. *Limite : Portée d'attaque de base seulement : les sorts ne sont pas comptés.* Déclencheur : `[{"kind":"range","mine":175,"theirs":550}]`.
1. **Fizz contre Syndra (supposé) : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). 1er choix contre Syndra · 1er choix contre Lux, selon un tableau écrit à la main. Ce critère est le plus lourd du classement. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main). Sans frontline, leurs joueurs fragiles sont exposés dès que quelqu'un les atteint. À l'inverse, ils peuvent te tuer vite si tu arrives seul. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Mid supposé, à vérifier** (Règle, Estimation). Confiance moyenne : déduit des rôles habituels. Une correction change le matchup pris en compte.
1. **Lux, Syndra et Caitlyn touchent de très loin** (À relire, Règle écrite à la main). Équipe de poke : si vous arrivez affaiblis, le combat démarre mal. Cherche à engager vite, ou à ne pas rester à portée. Déclencheur : `[{"kind":"champions","value":["Lux","Syndra","Caitlyn"]}]`.

### Intermédiaire 7 : Mid, dégâts surtout magiques

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Syndra, Ahri, Brand, Veigar, Thresh
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 3.46, Twisted Fate 1.29, Kog'Maw -2.34
- **Adversaire direct :** non identifié, confiance faible
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Pourquoi Fizz** (Règle, Règle écrite à la main, vue courte). « Matchup direct » pèse le plus : 1er choix contre Ahri · 1er choix contre Syndra. Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Mid pas encore identifié : prépare les deux cas** (Règle, Estimation, vue courte). Hypothèses : Ahri, Syndra. Le conseil tient compte des deux. Les deux : longue portée. *Limite : Déduction d'après les rôles habituels : ce n'est pas une information du client.*
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main, vue courte). Sans frontline, leurs joueurs fragiles sont exposés dès que quelqu'un les atteint. À l'inverse, ils peuvent te tuer vite si tu arrives seul. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Dégâts surtout magiques en face** (À relire, Estimation). Répartition déséquilibrée sur 4 champions : une résistance adaptée a plus de valeur. Vérifie en partie, car les objets adverses changent le partage. *Limite : Estimation, pas une mesure : les dégâts réels dépendent des objets.* Déclencheur : `[{"kind":"damage","value":"magiques","champions":4}]`.
1. **Quand préférer Xerath ?** (À relire, Règle écrite à la main). Xerath reste meilleur sur « Protection nécessaire » : Peu de plongeurs et de mobilité, et un frontline devant toi. Si ce point compte le plus pour ta partie, ce choix devient intéressant. Déclencheur : `[{"kind":"criterion","key":"peel","value":2}]`.
1. **Comprendre Fizz** (À relire, Donnée officielle de Riot). Classes Riot : Assassin, Fighter. Portée 175. Points forts : peut tuer une cible fragile. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Assassin","Fighter"],"range":175,"difficulty":6}]`.

### Intermédiaire 8 : Jungle, égalité entre fiches identiques

- **Rôle et pool :** jungle, Viego, Master Yi, Briar, Lee Sin
- **Adversaires visibles :** Ornn, Sion, Maokai, Jinx, Thresh
- **Conseil du moteur :** Master Yi, sinon Viego ; scores : Master Yi 4.00, Viego 4.00, Briar 0.00, Lee Sin -0.25
- **Adversaire direct :** Maokai, confiance moyenne
- **Raisons du moteur :** Tanks et bruisers 4.0
- **Ligne courte :** Égalité : Master Yi et Viego

1. **Égalité : Master Yi et Viego** (Règle, Règle écrite à la main, vue courte). Leurs fiches sont identiques : l'outil ne peut pas les distinguer dans cette draft. Ta maîtrise et ton envie comptent davantage que le classement. *Limite : Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu.* Déclencheur : `[{"kind":"tie","picks":["Master Yi","Viego"],"score":4}]`.
1. **Maokai encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Un tank en face tue rarement vite : joue la durée. Master Yi a ce qu'il faut contre une grosse barre de vie. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Ornn, Sion et Maokai** (À relire, Règle écrite à la main, vue courte). 3 joueurs très solides : le profil de Master Yi est valorisé ici. Reste en vie assez longtemps pour que tes dégâts comptent. Déclencheur : `[{"kind":"champions","value":["Ornn","Sion","Maokai"]}]`.
1. **Jungle supposé, à vérifier** (Règle, Estimation). Confiance moyenne : déduit des rôles habituels. Une correction change le matchup pris en compte.
1. **Ornn, Sion, Maokai et 1 autre peuvent lancer le combat** (À relire, Règle écrite à la main). Équipe qui choisit quand le combat commence : place-toi avant l'apparition des objectifs, pas au dernier moment. Déclencheur : `[{"kind":"champions","value":["Ornn","Sion","Maokai","Thresh"]}]`.
1. **Comprendre Master Yi** (À relire, Donnée officielle de Riot). Classes Riot : Fighter, Assassin. Portée 175. Points forts : fait de bons dégâts aux joueurs solides. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Fighter","Assassin"],"range":175,"difficulty":4}]`.

### Intermédiaire 9 : Jungle, historique personnel fictif

- **Rôle et pool :** jungle, Viego, Cho'Gath
- **Adversaires visibles :** Ornn, Vi, Ahri, Jinx, Thresh
- **Conseil du moteur :** Cho'Gath, sinon Viego ; scores : Cho'Gath 3.00, Viego 3.00
- **Adversaire direct :** Vi, confiance élevée
- **Raisons du moteur :** Tanks et bruisers 3.0
- **Ligne courte :** Égalité : Cho'Gath et Viego

1. **Égalité : Cho'Gath et Viego** (Règle, Règle écrite à la main, vue courte). Même score pour des raisons différentes. Regarde ce que chacun apporte dans Analyse, vue Comparer, et choisis selon ta maîtrise. *Limite : Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu.* Déclencheur : `[{"kind":"tie","picks":["Cho'Gath","Viego"],"score":3}]`.
1. **Tu as plus d'expérience sur Viego** (Règle, Mesuré sur de vraies parties, vue courte). Viego : nettement plus de parties que Cho'Gath. Cela ne change pas le classement, mais c'est un argument à peser. *Limite : Le nombre de parties ne dit pas si tu joues bien ce champion.* Déclencheur : `[{"kind":"games","pick":5,"rival":150}]`.
1. **Vi peut te sauter dessus** (À relire, Règle écrite à la main, vue courte). Sa menace dépend de sa capacité à arriver sur toi. Quand ses sorts de déplacement sont utilisés, c'est ta fenêtre pour échanger. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Ornn, Vi et Thresh peuvent lancer le combat** (À relire, Règle écrite à la main). Équipe qui choisit quand le combat commence : place-toi avant l'apparition des objectifs, pas au dernier moment. Déclencheur : `[{"kind":"champions","value":["Ornn","Vi","Thresh"]}]`.
1. **Comprendre Cho'Gath** (À relire, Donnée officielle de Riot). Classes Riot : Tank, Mage. Portée 125. Points forts : fait de bons dégâts aux joueurs solides et encaisse bien. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Tank","Mage"],"range":125,"difficulty":5}]`.
1. **Trop peu de parties pour juger sur Cho'Gath** (Règle, Principe général du jeu). 5 parties : aucune tendance personnelle n'est tirée sous 30 parties.

### Intermédiaire 10 : Top, beaucoup de contrôles

- **Rôle et pool :** top, Darius, Malphite, Camille
- **Adversaires visibles :** Sion, Lee Sin, Syndra, Jinx, Leona
- **Conseil du moteur :** Camille, sinon Darius ; scores : Camille 4.50, Darius 4.00, Malphite 1.50
- **Adversaire direct :** Sion, confiance élevée
- **Raisons du moteur :** Tanks et bruisers 4.0 ; Pression sur la carte 0.5
- **Ligne courte :** Camille, sinon Darius

1. **Camille ou Darius : le choix est serré** (Règle, Règle écrite à la main, vue courte). Camille mène surtout sur « Pression sur la carte » : peut peser sur toute la carte. L'écart reste faible : la maîtrise et ton équipe peuvent décider. Déclencheur : `[{"kind":"criterion","key":"map","value":0.5}]`.
1. **Sion encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Un tank en face tue rarement vite : joue la durée. Camille a ce qu'il faut contre une grosse barre de vie. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Sion et Leona** (À relire, Règle écrite à la main, vue courte). 2 joueurs très solides : le profil de Camille est valorisé ici. Reste en vie assez longtemps pour que tes dégâts comptent. Déclencheur : `[{"kind":"champions","value":["Sion","Leona"]}]`.
1. **Syndra et Leona peuvent t'immobiliser** (À relire, Règle écrite à la main). Plusieurs contrôles ciblés : joue autour du moment où ils sont utilisés. Déclencheur : `[{"kind":"champions","value":["Syndra","Leona"]}]`.
1. **Comprendre Camille** (À relire, Donnée officielle de Riot). Classes Riot : Fighter, Assassin. Portée 125. Points forts : fait de bons dégâts aux joueurs solides et peut peser sur toute la carte. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Fighter","Assassin"],"range":125,"difficulty":4}]`.

### Intermédiaire 11 : ADC, adversaire direct connu

- **Rôle et pool :** adc, Jinx, Ezreal
- **Adversaires visibles :** Ornn, Lee Sin, Ahri, Caitlyn, Thresh
- **Conseil du moteur :** Ezreal, sinon Jinx ; scores : Ezreal 2.25, Jinx 0.00
- **Adversaire direct :** Caitlyn, confiance élevée
- **Raisons du moteur :** Cibles exploitables 0.8 ; Engages et contrôles 1.5
- **Ligne courte :** Ezreal, sinon Jinx

1. **Pourquoi Ezreal** (Règle, Règle écrite à la main, vue courte). « Engages et contrôles » pèse le plus : peut jouer loin de leur engage et de leurs contrôles. Déclencheur : `[{"kind":"criterion","key":"control","value":1.5}]`.
1. **Comprendre Ezreal** (À relire, Donnée officielle de Riot, vue courte). Classes Riot : Marksman, Mage. Portée 550. Points forts : touche de très loin. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Marksman","Mage"],"range":550,"difficulty":7}]`.

### Intermédiaire 12 : Support, tank adverse direct

- **Rôle et pool :** support, Soraka, Leona, Lux
- **Adversaires visibles :** Ornn, Lee Sin, Ahri, Jinx, Alistar
- **Conseil du moteur :** Leona, sinon Lux ; scores : Leona 1.50, Lux 0.75, Soraka 0.50
- **Adversaire direct :** Alistar, confiance manuel
- **Raisons du moteur :** Cibles exploitables 1.5
- **Ligne courte :** Leona, sinon Lux

1. **Leona ou Lux : le choix est serré** (Règle, Règle écrite à la main, vue courte). Leona mène surtout sur « Cibles exploitables » : 2 cibles fragiles à attraper : Ahri, Jinx. L'écart reste faible : la maîtrise et ton équipe peuvent décider. Déclencheur : `[{"kind":"criterion","key":"targets","value":0.75}]`.
1. **Alistar encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Tuer un tank est long : garde tes sorts pour une cible plus fragile, sauf si tes alliés se concentrent sur lui. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Ornn et Alistar** (À relire, Règle écrite à la main, vue courte). 2 joueurs très solides : ils tiennent longtemps en combat groupé. Pense à leurs joueurs fragiles derrière. Déclencheur : `[{"kind":"champions","value":["Ornn","Alistar"]}]`.
1. **Comprendre Leona** (À relire, Donnée officielle de Riot). Classes Riot : Tank, Support. Portée 125. Points forts : encaisse bien et peut lancer les combats. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Tank","Support"],"range":125,"difficulty":4}]`.

## Niveau Avancé

### Avancé 1 : Mid, draft complète, mid connu

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Malzahar, Ornn, Rengar, Samira, Pyke
- **Conseil du moteur :** Twisted Fate, sinon Fizz ; scores : Twisted Fate 3.50, Fizz -0.75, Kog'Maw -1.25, Xerath -3.25
- **Adversaire direct :** Malzahar, confiance élevée
- **Raisons du moteur :** Matchup direct 1.5 ; Engages et contrôles 1.5 ; Pression sur la carte 0.5
- **Ligne courte :** Twisted Fate, sinon Fizz

1. **Pourquoi Twisted Fate** (Règle, Règle écrite à la main, vue courte). Critère le plus lourd : « Matchup direct » (+1,5), pour 4,3 points d'écart avec Fizz. Déclencheur : `[{"kind":"criterion","key":"matchup","value":1.5}]`.
1. **3 adversaires sautent sur leurs cibles** (À relire, Règle écrite à la main, vue courte). 3 plongeurs : l'équipe est forte sur une cible isolée, plus faible si elle doit traverser une frontline bien placée. Déclencheur : `[{"kind":"champions","value":["Rengar","Samira","Pyke"]}]`.
1. **Twisted Fate contre Malzahar : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). 2e choix contre Malzahar : avis écrit à la main (1,5 points), sans statistiques ni patch. Un bilan calculé le remplacera dès 30 parties utiles. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":1.5}]`.
1. **Quand préférer Fizz ?** (À relire, Règle écrite à la main). Fizz : avantage de 3,0 sur « Cibles exploitables ». Aucun changement simple de la draft ne le fait passer devant. Déclencheur : `[{"kind":"criterion","key":"targets","value":3}]`.
1. **Comprendre Twisted Fate** (À relire, Donnée officielle de Riot). Classes Riot : Mage, Marksman, portée 525, difficulté notée 9/10 par Riot. Points forts : peut peser sur toute la carte. Attention : a peu de prise sur les joueurs solides. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Marksman"],"range":525,"difficulty":9}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 2 : Mid, matchup à longue portée

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Syndra, Ornn, Lee Sin, Jinx, Thresh
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 4.25, Kog'Maw 2.75, Twisted Fate 2.00
- **Adversaire direct :** Syndra, confiance élevée
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Pourquoi Fizz** (Règle, Règle écrite à la main, vue courte). Critère le plus lourd : « Matchup direct » (+3,0), pour 2,0 points d'écart avec Xerath. Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Syndra te touche de plus loin** (À relire, Donnée officielle de Riot, vue courte). Écart de portée d'attaque de base : 175 contre 550. Il pèse surtout tôt dans la partie, et ne dit rien de la portée des sorts ni des outils pour le combler. *Limite : Portée d'attaque de base seulement : les sorts ne sont pas comptés.* Déclencheur : `[{"kind":"range","mine":175,"theirs":550}]`.
1. **Fizz contre Syndra : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). 1er choix contre Syndra : avis écrit à la main (3,0 points), sans statistiques ni patch. Un bilan calculé le remplacera dès 30 parties utiles. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Quand préférer Xerath ?** (À relire, Règle écrite à la main). Xerath : avantage de 2,0 sur « Protection nécessaire ». Aucun changement simple de la draft ne le fait passer devant. Déclencheur : `[{"kind":"criterion","key":"peel","value":2}]`.
1. **Comprendre Fizz** (À relire, Donnée officielle de Riot). Classes Riot : Assassin, Fighter, portée 175, difficulté notée 6/10 par Riot. Points forts : peut tuer une cible fragile. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Assassin","Fighter"],"range":175,"difficulty":6}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 3 : Mid, adversaire incertain

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Yasuo, Yone, Lee Sin, Jinx, Thresh
- **Conseil du moteur :** Kog'Maw, sinon Fizz ; scores : Kog'Maw 1.79, Fizz 0.47, Twisted Fate -2.01, Xerath -6.50
- **Adversaire direct :** Yasuo, confiance moyenne
- **Raisons du moteur :** Matchup direct 2.0 ; Tanks et bruisers 4.0 ; Survie aux plongeurs -4.0 ; Pression sur la carte -0.3
- **Ligne courte :** Kog'Maw, sinon Fizz

1. **Kog'Maw ressort malgré un vrai risque** (Règle, Règle écrite à la main, vue courte). Critère négatif « Survie aux plongeurs » (-4,0), compensé par les autres. Un classement par somme peut masquer ce type de faiblesse : décide si elle est acceptable pour toi. Déclencheur : `[{"kind":"criterion","key":"dive","value":-4}]`.
1. **Kog'Maw ou Fizz : le choix est serré** (Règle, Règle écrite à la main, vue courte). Écart de 1,3 point : sous le seuil de 2 utilisé pour parler de choix net. Le critère décisif est « Tanks et bruisers ». Ces points servent à comparer, ce n'est pas une probabilité de victoire. Déclencheur : `[{"kind":"criterion","key":"front","value":5}]`.
1. **Yasuo, Yone et Lee Sin peuvent atteindre Kog'Maw** (À relire, Règle écrite à la main, vue courte). Risque compté par le moteur (« Survie aux plongeurs »). Il baisse avec une frontline alliée qui protège : à vérifier selon les picks de ton équipe. Déclencheur : `[{"kind":"champions","value":["Yasuo","Yone","Lee Sin"]}]`.
1. **Kog'Maw contre Yasuo (supposé) : plutôt un bon choix** (Règle, Règle écrite à la main). 2e choix contre Yasuo · 1er choix contre Yone : avis écrit à la main (2,0 points), sans statistiques ni patch. Un bilan calculé le remplacera dès 30 parties utiles. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":2.04}]`.
1. **Mid supposé, à vérifier** (Règle, Estimation). Déduction probabiliste sur les rôles habituels (confiance moyenne), jamais une donnée du client. Sa qualité dépend de l'âge des données de rôles.
1. **Dégâts surtout physiques en face** (À relire, Estimation). Estimation d'après les notes de Riot et une table écrite à la main. Le vrai partage dépend des objets et de chaque partie : à confirmer une fois en jeu. *Limite : Estimation, pas une mesure : les dégâts réels dépendent des objets.* Déclencheur : `[{"kind":"damage","value":"physiques","champions":4}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 4 : Mid, beaucoup de plongeurs, champion immobile

- **Rôle et pool :** mid, Xerath, Kog'Maw
- **Adversaires visibles :** Zed, Rengar, Katarina, Jinx, Thresh
- **Conseil du moteur :** Xerath, sinon Kog'Maw ; scores : Xerath -8.00, Kog'Maw -8.25
- **Adversaire direct :** Katarina, confiance élevée
- **Raisons du moteur :** Matchup direct -4.0 ; Survie aux plongeurs -4.0
- **Ligne courte :** Xerath, sinon Kog'Maw

1. **Xerath ou Kog'Maw : le choix est serré** (Règle, Règle écrite à la main, vue courte). Écart de 0,3 point : sous le seuil de 2 utilisé pour parler de choix net. Le critère décisif est « Pression sur la carte ». Ces points servent à comparer, ce n'est pas une probabilité de victoire. Déclencheur : `[{"kind":"criterion","key":"map","value":0.25}]`.
1. **Zed, Rengar et Katarina peuvent atteindre Xerath** (À relire, Règle écrite à la main, vue courte). Risque compté par le moteur (« Survie aux plongeurs »). Il baisse avec une frontline alliée qui protège : à vérifier selon les picks de ton équipe. Déclencheur : `[{"kind":"champions","value":["Zed","Rengar","Katarina"]}]`.
1. **Xerath contre Katarina : un matchup difficile** (Règle, Règle écrite à la main, vue courte). À éviter contre Katarina : avis écrit à la main (-4,0 points), sans statistiques ni patch. Un bilan calculé le remplacera dès 30 parties utiles. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":-4}]`.
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main). Absence de tank ou de bruiser sur 5 champions visibles. Leur force dépend alors de tuer avant d'être atteint : le premier engagement réussi pèse lourd. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Comprendre Xerath** (À relire, Donnée officielle de Riot). Classes Riot : Mage, Support, portée 525, difficulté notée 8/10 par Riot. Points forts : touche de très loin. Attention : se déplace peu, donc un adversaire mobile peut l'atteindre. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Support"],"range":525,"difficulty":8}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 5 : Mid, draft incomplète

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Sett, Vi
- **Conseil du moteur :** Twisted Fate, sinon Kog'Maw ; scores : Twisted Fate 2.85, Kog'Maw 2.21, Xerath -0.69, Fizz -0.75
- **Adversaire direct :** non identifié, confiance faible
- **Raisons du moteur :** Sécurité sans adversaire connu 0.8 ; Engages et contrôles 1.5 ; Pression sur la carte 0.5
- **Ligne courte :** Twisted Fate, sinon Kog'Maw

1. **Twisted Fate ou Kog'Maw : le choix est serré** (Règle, Règle écrite à la main, vue courte). Écart de 0,6 point : sous le seuil de 2 utilisé pour parler de choix net. Le critère décisif est « Engages et contrôles ». Ces points servent à comparer, ce n'est pas une probabilité de victoire. Déclencheur : `[{"kind":"criterion","key":"control","value":1.5}]`.
1. **Mid pas encore visible** (Règle, Estimation, vue courte). Sans candidat plausible, seul le critère « sans adversaire connu » intervient pour ce rôle. *Limite : Déduction d'après les rôles habituels : ce n'est pas une information du client.*
1. **Quand préférer Kog'Maw ?** (À relire, Règle écrite à la main, vue courte). Kog'Maw : avantage de 3,0 sur « Tanks et bruisers ». Aucun changement simple de la draft ne le fait passer devant. Déclencheur : `[{"kind":"criterion","key":"front","value":3}]`.
1. **Comprendre Twisted Fate** (À relire, Donnée officielle de Riot). Classes Riot : Mage, Marksman, portée 525, difficulté notée 9/10 par Riot. Points forts : peut peser sur toute la carte. Attention : a peu de prise sur les joueurs solides. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Mage","Marksman"],"range":525,"difficulty":9}]`.
1. **Il reste 3 adversaires à voir** (Règle, Principe général du jeu). Les critères à seuil ne se déclenchent pas encore tous, et 3 rôles restent ouverts dans le calcul des hypothèses.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 6 : Mid, équipe sans frontline

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Lux, Jinx, Syndra, Caitlyn, Soraka
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 5.25, Twisted Fate 0.50, Kog'Maw -1.25
- **Adversaire direct :** Syndra, confiance moyenne
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Fizz ou Xerath : le choix est serré** (Règle, Règle écrite à la main, vue courte). Écart de 1,0 point : sous le seuil de 2 utilisé pour parler de choix net. Le critère décisif est « Cibles exploitables ». Ces points servent à comparer, ce n'est pas une probabilité de victoire. Déclencheur : `[{"kind":"criterion","key":"targets","value":2.25}]`.
1. **Syndra te touche de plus loin** (À relire, Donnée officielle de Riot, vue courte). Écart de portée d'attaque de base : 175 contre 550. Il pèse surtout tôt dans la partie, et ne dit rien de la portée des sorts ni des outils pour le combler. *Limite : Portée d'attaque de base seulement : les sorts ne sont pas comptés.* Déclencheur : `[{"kind":"range","mine":175,"theirs":550}]`.
1. **Fizz contre Syndra (supposé) : plutôt un bon choix** (Règle, Règle écrite à la main, vue courte). 1er choix contre Syndra · 1er choix contre Lux : avis écrit à la main (3,0 points), sans statistiques ni patch. Un bilan calculé le remplacera dès 30 parties utiles. *Limite : Avis écrit à la main, pas une statistique.* Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main). Absence de tank ou de bruiser sur 5 champions visibles. Leur force dépend alors de tuer avant d'être atteint : le premier engagement réussi pèse lourd. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Mid supposé, à vérifier** (Règle, Estimation). Déduction probabiliste sur les rôles habituels (confiance moyenne), jamais une donnée du client. Sa qualité dépend de l'âge des données de rôles.
1. **Lux, Syndra et Caitlyn touchent de très loin** (À relire, Règle écrite à la main). Le poke gagne le temps : il faut soit le raccourcir, soit soigner. Ton équipe n'est pas connue de l'outil. Déclencheur : `[{"kind":"champions","value":["Lux","Syndra","Caitlyn"]}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 7 : Mid, dégâts surtout magiques

- **Rôle et pool :** mid, TF, Fizz, Kog'Maw, Xerath
- **Adversaires visibles :** Syndra, Ahri, Brand, Veigar, Thresh
- **Conseil du moteur :** Fizz, sinon Xerath ; scores : Fizz 6.25, Xerath 3.46, Twisted Fate 1.29, Kog'Maw -2.34
- **Adversaire direct :** non identifié, confiance faible
- **Raisons du moteur :** Matchup direct 3.0 ; Cibles exploitables 3.0 ; Pression sur la carte 0.3
- **Ligne courte :** Fizz, sinon Xerath

1. **Pourquoi Fizz** (Règle, Règle écrite à la main, vue courte). Critère le plus lourd : « Matchup direct » (+3,0), pour 2,8 points d'écart avec Xerath. Déclencheur : `[{"kind":"criterion","key":"matchup","value":3}]`.
1. **Mid pas encore identifié : prépare les deux cas** (Règle, Estimation, vue courte). Classement pondéré entre Ahri et Syndra. Si tu connais le vrai rôle, corrige-le : le calcul redevient exact pour ce matchup. *Limite : Déduction d'après les rôles habituels : ce n'est pas une information du client.*
1. **Personne pour encaisser en face** (À relire, Règle écrite à la main, vue courte). Absence de tank ou de bruiser sur 5 champions visibles. Leur force dépend alors de tuer avant d'être atteint : le premier engagement réussi pèse lourd. Déclencheur : `[{"kind":"count","key":"frontline","value":0}]`.
1. **Dégâts surtout magiques en face** (À relire, Estimation). Estimation d'après les notes de Riot et une table écrite à la main. Le vrai partage dépend des objets et de chaque partie : à confirmer une fois en jeu. *Limite : Estimation, pas une mesure : les dégâts réels dépendent des objets.* Déclencheur : `[{"kind":"damage","value":"magiques","champions":4}]`.
1. **Quand préférer Xerath ?** (À relire, Règle écrite à la main). Xerath : avantage de 2,0 sur « Protection nécessaire ». Aucun changement simple de la draft ne le fait passer devant. Déclencheur : `[{"kind":"criterion","key":"peel","value":2}]`.
1. **Comprendre Fizz** (À relire, Donnée officielle de Riot). Classes Riot : Assassin, Fighter, portée 175, difficulté notée 6/10 par Riot. Points forts : peut tuer une cible fragile. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Assassin","Fighter"],"range":175,"difficulty":6}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 8 : Jungle, égalité entre fiches identiques

- **Rôle et pool :** jungle, Viego, Master Yi, Briar, Lee Sin
- **Adversaires visibles :** Ornn, Sion, Maokai, Jinx, Thresh
- **Conseil du moteur :** Master Yi, sinon Viego ; scores : Master Yi 4.00, Viego 4.00, Briar 0.00, Lee Sin -0.25
- **Adversaire direct :** Maokai, confiance moyenne
- **Raisons du moteur :** Tanks et bruisers 4.0
- **Ligne courte :** Égalité : Master Yi et Viego

1. **Égalité : Master Yi et Viego** (Règle, Règle écrite à la main, vue courte). Score identique (4,0). Le nom affiché en premier ne vient que de l'ordre alphabétique, ou de ton favori : ce n'est pas un avantage. Fiches identiques : aucune règle ne peut les séparer. *Limite : Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu.* Déclencheur : `[{"kind":"tie","picks":["Master Yi","Viego"],"score":4}]`.
1. **Maokai encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Face à un tank, l'intérêt de Master Yi grandit avec le temps. C'est une règle générale de sa fiche, pas un bilan mesuré de ce matchup. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Ornn, Sion et Maokai** (À relire, Règle écrite à la main, vue courte). Bonus du critère « Tanks et bruisers » (+4,0). Attention : il compte aussi les bruisers (3 ici), et se déclenche dès 2 : ne le lis pas comme un avantage rare. Déclencheur : `[{"kind":"champions","value":["Ornn","Sion","Maokai"]}]`.
1. **Jungle supposé, à vérifier** (Règle, Estimation). Déduction probabiliste sur les rôles habituels (confiance moyenne), jamais une donnée du client. Sa qualité dépend de l'âge des données de rôles.
1. **Ornn, Sion, Maokai et 1 autre peuvent lancer le combat** (À relire, Règle écrite à la main). 4 sources d'engage : leur force est de forcer le combat. La vision et l'ordre d'arrivée sur les objectifs deviennent décisifs. Déclencheur : `[{"kind":"champions","value":["Ornn","Sion","Maokai","Thresh"]}]`.
1. **Comprendre Master Yi** (À relire, Donnée officielle de Riot). Classes Riot : Fighter, Assassin, portée 175, difficulté notée 4/10 par Riot. Points forts : fait de bons dégâts aux joueurs solides. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Fighter","Assassin"],"range":175,"difficulty":4}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 9 : Jungle, historique personnel fictif

- **Rôle et pool :** jungle, Viego, Cho'Gath
- **Adversaires visibles :** Ornn, Vi, Ahri, Jinx, Thresh
- **Conseil du moteur :** Cho'Gath, sinon Viego ; scores : Cho'Gath 3.00, Viego 3.00
- **Adversaire direct :** Vi, confiance élevée
- **Raisons du moteur :** Tanks et bruisers 3.0
- **Ligne courte :** Égalité : Cho'Gath et Viego

1. **Égalité : Cho'Gath et Viego** (Règle, Règle écrite à la main, vue courte). Score identique (3,0). Le nom affiché en premier ne vient que de l'ordre alphabétique, ou de ton favori : ce n'est pas un avantage. *Limite : Les règles ne les distinguent pas ici : ce n'est pas une preuve qu'ils se valent en jeu.* Déclencheur : `[{"kind":"tie","picks":["Cho'Gath","Viego"],"score":3}]`.
1. **Tu as plus d'expérience sur Viego** (Règle, Mesuré sur de vraies parties, vue courte). Écart d'expérience (5 contre 150), hors du score. Le lien avec tes résultats n'est pas établi par ce seul chiffre. *Limite : Le nombre de parties ne dit pas si tu joues bien ce champion.* Déclencheur : `[{"kind":"games","pick":5,"rival":150}]`.
1. **Vi peut te sauter dessus** (À relire, Règle écrite à la main, vue courte). Menace d'accès rapide : la zone dangereuse dépend de ses sorts de déplacement, pas de sa portée d'attaque. Le niveau d'objets de chacun change vite ce rapport. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Ornn, Vi et Thresh peuvent lancer le combat** (À relire, Règle écrite à la main). 3 sources d'engage : leur force est de forcer le combat. La vision et l'ordre d'arrivée sur les objectifs deviennent décisifs. Déclencheur : `[{"kind":"champions","value":["Ornn","Vi","Thresh"]}]`.
1. **Comprendre Cho'Gath** (À relire, Donnée officielle de Riot). Classes Riot : Tank, Mage, portée 125, difficulté notée 5/10 par Riot. Points forts : fait de bons dégâts aux joueurs solides et encaisse bien. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Tank","Mage"],"range":125,"difficulty":5}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.
1. **Trop peu de parties pour juger sur Cho'Gath** (Règle, Principe général du jeu). Moins de 30 parties (5) : aucune conclusion sur tes résultats.

### Avancé 10 : Top, beaucoup de contrôles

- **Rôle et pool :** top, Darius, Malphite, Camille
- **Adversaires visibles :** Sion, Lee Sin, Syndra, Jinx, Leona
- **Conseil du moteur :** Camille, sinon Darius ; scores : Camille 4.50, Darius 4.00, Malphite 1.50
- **Adversaire direct :** Sion, confiance élevée
- **Raisons du moteur :** Tanks et bruisers 4.0 ; Pression sur la carte 0.5
- **Ligne courte :** Camille, sinon Darius

1. **Camille ou Darius : le choix est serré** (Règle, Règle écrite à la main, vue courte). Écart de 0,5 point : sous le seuil de 2 utilisé pour parler de choix net. Le critère décisif est « Pression sur la carte ». Ces points servent à comparer, ce n'est pas une probabilité de victoire. Déclencheur : `[{"kind":"criterion","key":"map","value":0.5}]`.
1. **Sion encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Face à un tank, l'intérêt de Camille grandit avec le temps. C'est une règle générale de sa fiche, pas un bilan mesuré de ce matchup. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Sion et Leona** (À relire, Règle écrite à la main, vue courte). Bonus du critère « Tanks et bruisers » (+4,0). Attention : il compte aussi les bruisers (3 ici), et se déclenche dès 2 : ne le lis pas comme un avantage rare. Déclencheur : `[{"kind":"champions","value":["Sion","Leona"]}]`.
1. **Syndra et Leona peuvent t'immobiliser** (À relire, Règle écrite à la main). 2 contrôles ciblés : à lire avec les délais de récupération, que l'outil ne connaît pas. Déclencheur : `[{"kind":"champions","value":["Syndra","Leona"]}]`.
1. **Comprendre Camille** (À relire, Donnée officielle de Riot). Classes Riot : Fighter, Assassin, portée 125, difficulté notée 4/10 par Riot. Points forts : fait de bons dégâts aux joueurs solides et peut peser sur toute la carte. Attention : doit entrer dans le combat, donc s'exposer. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Fighter","Assassin"],"range":125,"difficulty":4}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 11 : ADC, adversaire direct connu

- **Rôle et pool :** adc, Jinx, Ezreal
- **Adversaires visibles :** Ornn, Lee Sin, Ahri, Caitlyn, Thresh
- **Conseil du moteur :** Ezreal, sinon Jinx ; scores : Ezreal 2.25, Jinx 0.00
- **Adversaire direct :** Caitlyn, confiance élevée
- **Raisons du moteur :** Cibles exploitables 0.8 ; Engages et contrôles 1.5
- **Ligne courte :** Ezreal, sinon Jinx

1. **Pourquoi Ezreal** (Règle, Règle écrite à la main, vue courte). Critère le plus lourd : « Engages et contrôles » (+1,5), pour 2,3 points d'écart avec Jinx. Déclencheur : `[{"kind":"criterion","key":"control","value":1.5}]`.
1. **Comprendre Ezreal** (À relire, Donnée officielle de Riot, vue courte). Classes Riot : Marksman, Mage, portée 550, difficulté notée 7/10 par Riot. Points forts : touche de très loin. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Marksman","Mage"],"range":550,"difficulty":7}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu, vue courte). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

### Avancé 12 : Support, tank adverse direct

- **Rôle et pool :** support, Soraka, Leona, Lux
- **Adversaires visibles :** Ornn, Lee Sin, Ahri, Jinx, Alistar
- **Conseil du moteur :** Leona, sinon Lux ; scores : Leona 1.50, Lux 0.75, Soraka 0.50
- **Adversaire direct :** Alistar, confiance manuel
- **Raisons du moteur :** Cibles exploitables 1.5
- **Ligne courte :** Leona, sinon Lux

1. **Leona ou Lux : le choix est serré** (Règle, Règle écrite à la main, vue courte). Écart de 0,8 point : sous le seuil de 2 utilisé pour parler de choix net. Le critère décisif est « Cibles exploitables ». Ces points servent à comparer, ce n'est pas une probabilité de victoire. Déclencheur : `[{"kind":"criterion","key":"targets","value":0.75}]`.
1. **Alistar encaisse beaucoup** (À relire, Règle écrite à la main, vue courte). Contre un tank, ton impact passe souvent par les cibles fragiles et le timing. À nuancer selon ce que ton équipe peut tuer. *Limite : Avis général, pas un bilan de ce matchup.*
1. **Des joueurs solides en face : Ornn et Alistar** (À relire, Règle écrite à la main, vue courte). Composition résistante : à comparer aux dégâts soutenus de ton équipe, que l'outil ne connaît pas. Déclencheur : `[{"kind":"champions","value":["Ornn","Alistar"]}]`.
1. **Comprendre Leona** (À relire, Donnée officielle de Riot). Classes Riot : Tank, Support, portée 125, difficulté notée 4/10 par Riot. Points forts : encaisse bien et peut lancer les combats. *Limite : Portée et classes viennent de Riot. Forces et risques sont un avis écrit à la main.* Déclencheur : `[{"kind":"official","tags":["Tank","Support"],"range":125,"difficulty":4}]`.
1. **Ce classement repose sur des règles, pas sur des statistiques** (Règle, Principe général du jeu). Le score additionne des règles écrites à la main, sur des fiches sans statistiques. Il sert à comparer tes picks : ce n'est ni un winrate, ni une probabilité, et il ne dépend pas de la méta actuelle.

