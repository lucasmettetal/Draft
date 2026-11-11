# Résultats de mesure des rôles adverses — 10 novembre 2026

Moteur mesuré : branche `v217/mesure-roles`, moteur identique à `main` 78519fb (DRAFT 2.16.1).
Commande : `pwsh -File tools/run-accuracy-tests.ps1` (graine 20261110, 2000 équipes). Mode d'emploi et définitions :
`docs/technique/MESURE-ROLES.md`.

## Ce qui a été mesuré, et ce qui ne l'a pas été

| Source | Mesurée ? | Pourquoi |
|---|---|---|
| Fiche écrite seule, drafts **SYNTHÉTIQUES** tirées de la fiche | Oui | Fonctionne sur tout PC. |
| Réserve réelle (holdout) | **Non** | Aucun fichier `draft-role-rates-state.json` sur le PC de mesure. Le script l'a dit et s'est arrêté (code 2). |
| Fiche + calcul live réel | Non | Sans réserve, aucune vérité indépendante. Un fichier live local existe, mais il n'a pas été lu. |
| Chemins « réserve » et « live » de l'outil | Vérifiés seulement | Avec `tests/roles/fixtures/` : 14 équipes et des chiffres **inventés**. Ces sorties ne sont pas des résultats et ne sont pas reprises ici. |

**Aucun chiffre de ce document ne vient de vraies parties.** Le générateur des drafts suit la fiche écrite, donc le modèle
lui-même : ces chiffres sont une **borne haute**. Sur de vraies parties, la précision sera plus basse, surtout pour les
champions joués hors de leur rôle habituel, absents de ces drafts.

`-CompareRef b4fdeba` (moteur publié avec la 2.14.0, plus ancien) donne les mêmes chiffres sur ces drafts, à la décimale près :
les changements du moteur entre ces deux versions ne touchent pas l'attribution des rôles. Cela montre aussi que la
comparaison entre commits fonctionne.

## 1. Rôle par rôle (fiche écrite, drafts SYNTHÉTIQUES, n = 2000 drafts par taille)

Précision du 1er candidat (champion révélé ou « pas encore choisi »), intervalle de Wilson à 95 %.

| Révélés | Top | Jungle | Mid | ADC | Support | Brier | Log-loss |
|---|---|---|---|---|---|---|---|
| 1 | 94,8 % [93,7-95,6] | 94,8 % [93,7-95,7] | 92,2 % [90,9-93,2] | 98,3 % [97,6-98,8] | 94,1 % [93,0-95,1] | 0,093 | 0,178 |
| 2 | 90,5 % [89,1-91,7] | 91,3 % [90,0-92,5] | 87,0 % [85,4-88,4] | 97,2 % [96,4-97,8] | 90,9 % [89,6-92,1] | 0,152 | 0,303 |
| 3 | 88,6 % [87,1-89,9] | 90,7 % [89,4-91,9] | 86,2 % [84,7-87,7] | 97,0 % [96,1-97,6] | 90,2 % [88,8-91,5] | 0,174 | 0,367 |
| 4 | 89,3 % [87,9-90,6] | 93,4 % [92,2-94,4] | 89,1 % [87,7-90,4] | 97,7 % [96,9-98,3] | 93,5 % [92,3-94,5] | 0,150 | 0,339 |
| 5 | 95,4 % [94,4-96,2] | 97,6 % [96,8-98,2] | 95,1 % [94,0-95,9] | 99,8 % [99,4-99,9] | 97,4 % [96,6-98,0] | 0,058 | 0,151 |

Avec 1 champion révélé, la précision est haute surtout parce que « pas encore choisi » est presque toujours vrai.

**Assignation complète juste** (5 révélés, meilleure des 120 répartitions) : **94,1 % [92,9-95,0]**, dont 121 drafts
sur 2000 avec une égalité exacte entre plusieurs répartitions. Par rôle : Top 95,9 %, Jungle 97,8 %, Mid 95,7 %,
ADC 99,6 %, Support 97,9 %.

## 2. Calibration (toutes tailles, toutes options)

| p annoncée | n | p moyenne | Fréquence observée |
|---|---|---|---|
| [0,0 ; 0,1[ | 123 445 | 0,028 | 0,000 |
| [0,1 ; 0,2[ | 13 203 | 0,135 | 0,022 |
| [0,2 ; 0,3[ | 8 843 | 0,245 | 0,184 |
| [0,3 ; 0,4[ | 3 888 | 0,355 | 0,301 |
| [0,4 ; 0,5[ | 2 186 | 0,452 | 0,517 |
| [0,5 ; 0,6[ | 2 273 | 0,560 | 0,753 |
| [0,6 ; 0,7[ | 6 117 | 0,642 | 0,779 |
| [0,7 ; 0,8[ | 7 021 | 0,746 | 0,901 |
| [0,8 ; 0,9[ | 16 528 | 0,856 | 0,996 |
| [0,9 ; 1,0] | 16 496 | 0,946 | 1,000 |

Face à son propre générateur, le modèle est **sous-confiant** au-dessus de 0,4 et un peu sur-confiant entre 0,1 et 0,4.
C'est attendu : le plancher de 0,05 par rôle prévoit des choix atypiques que ce générateur ne produit jamais.
Sur de vraies parties, la calibration reste **inconnue**.

## 3. Probable, possible, écarté (seuils de mesure 0,6 et 0,2)

Paires champion révélé × rôle, toutes tailles :

| Classe | Paires | Part | Rôle réel |
|---|---|---|---|
| probable (p ≥ 0,6) | 27 600 | 18,4 % | 95,6 % [95,3-95,8] |
| possible (0,2 ≤ p < 0,6) | 9 223 | 6,1 % | 36,9 % [35,9-37,9] |
| écarté (p < 0,2) | 113 177 | 75,5 % | 0,2 % [0,2-0,2] |

Champions révélés au rôle « probable » (sinon « indéterminé ») :

| Révélés | Rôle probable | Justes si probable | Justes si indéterminé |
|---|---|---|---|
| 1 | 96,3 % | 87,7 % [86,2-89,1] | 70,3 % [59,1-79,5] |
| 2 | 90,9 % | 91,1 % [90,2-92,0] | 64,2 % [59,1-68,9] |
| 3 | 88,2 % | 94,0 % [93,4-94,6] | 70,0 % [66,6-73,3] |
| 4 | 91,4 % | 96,4 % [95,9-96,8] | 67,9 % [64,3-71,3] |
| 5 | 94,3 % | 99,1 % [98,9-99,3] | 64,4 % [60,3-68,2] |

Les seuils séparent bien les classes sur ces drafts : « probable » est juste bien plus de 60 % du temps, « écarté »
presque jamais. À revérifier sur la réserve.

## 4. Adversaire direct, par rôle du joueur (toutes tailles, n = 10 000 drafts par profil)

| Profil | 1er candidat juste | 5 révélés | Confiance faible | Nommé | Nom juste | Nommé à tort (non révélé) | Brier | Log-loss |
|---|---|---|---|---|---|---|---|---|
| Top | 91,7 % | 95,4 % | 44,9 % | 55,2 % | 93,8 % | 6,6 % | 0,149 | 0,303 |
| Jungle | 93,6 % | 97,6 % | 41,3 % | 58,7 % | 94,9 % | 6,4 % | 0,123 | 0,263 |
| Mid | 89,9 % | 95,1 % | 42,9 % | 57,1 % | 92,0 % | 9,4 % | 0,172 | 0,336 |
| ADC | 98,0 % | 99,8 % | 40,9 % | 59,2 % | 98,5 % | 2,1 % | 0,061 | 0,173 |
| Support | 93,2 % | 97,4 % | 43,0 % | 57,0 % | 95,0 % | 5,6 % | 0,123 | 0,264 |

Détail par taille (profil Mid) :

| Révélés | Confiance faible | Nommé | Nom juste | Mid pas révélé → nommé à tort |
|---|---|---|---|---|
| 1 | 80,3 % [78,5-82,0] | 19,7 % | 81,2 % [77,1-84,8] | 4,6 % [3,7-5,8] (n = 1593) |
| 2 | 59,8 % [57,6-61,9] | 40,2 % | 84,5 % [81,8-86,8] | 10,4 % [8,7-12,3] (n = 1158) |
| 3 | 42,9 % [40,7-45,1] | 57,1 % | 89,2 % [87,3-90,9] | 13,6 % [11,4-16,2] (n = 770) |
| 4 | 25,1 % [23,2-27,0] | 75,0 % | 93,5 % [92,2-94,7] | 17,4 % [14,0-21,6] (n = 384) |
| 5 | 6,3 % [5,3-7,5] | 93,7 % | 98,0 % [97,2-98,5] | — |

Constats (sur ces drafts SYNTHÉTIQUES) :
- La confiance « élevée » n'apparaît jamais : sans calcul live, la qualité reste `reference`, plafonnée à « moyenne ».
- Le taux « nommé à tort » **augmente** avec le nombre de champions révélés quand l'adversaire manque encore
  (mid : 4,6 % avec 1 révélé, 17,4 % avec 4). Plus la draft avance, plus le moteur désigne quelqu'un hors rôle.
- Le vrai cas reste dans les hypothèses gardées dans 99,8 % à 100 % des drafts, pour tous les profils.

## 5. Champions flex

**Fiche écrite** : 171 champions, 99 avec un seul rôle, 72 avec plusieurs :
Akali (mt), Akshan (mt), Ambessa (tj), Amumu (js), Annie (ms), Ashe (as), Aurora (mt), Brand (sj), Cassiopeia (mt),
Cho'Gath (tm), Corki (ma), Diana (jm), Dr. Mundo (tj), Ekko (jm), Fiddlesticks (js), Galio (ms), Gragas (jts), Gwen (tj),
Heimerdinger (mts), Hwei (ms), Irelia (tm), Jax (tj), Jayce (tm), Karma (sm), Karthus (jm), Kayle (tm), Kennen (tm),
Lillia (jt), Lucian (am), Lux (sm), Malphite (ts), Maokai (sj), Mel (ms), Mordekaiser (tj), Morgana (sj), Naafiri (mj),
Neeko (ms), Olaf (tj), Pantheon (stm), Poppy (jts), Qiyana (mj), Rengar (jt), Rumble (tm), Ryze (mt), Senna (sa),
Seraphine (sa), Sett (ts), Shaco (js), Shen (ts), Sion (tm), Skarner (jt), Smolder (am), Swain (sm), Sylas (mt),
Tahm Kench (ts), Taliyah (jm), Talon (mj), Tristana (am), Trundle (jt), Udyr (jt), Vayne (at), Veigar (ms), Vel'Koz (sm),
Vladimir (mt), Volibear (jt), Warwick (jt), Wukong (jt), Xerath (sm), Yasuo (mt), Yone (mt), Zed (mj), Ziggs (am).
Sans fiche : Locke, Zaahen (parts uniformes à 0,2).

**Modèle, fiche seule** (au moins deux rôles à p ≥ 0,2) : les 72 ci-dessus, plus Locke et Zaahen (74). Pour Heimerdinger,
Pantheon, Gragas et Poppy, la 3ᵉ lettre reste sous 0,2. Le calcul live, quand il existe, peut ajouter ou retirer des
champions de cette liste : l'outil la recalcule avec `-LiveFile`.

**Égalités exactes** (parts identiques avec la fiche seule) :

| Parts T/J/M/A/S | Champions |
|---|---|
| 0,05/0,05/0,05/0,80/0,05 | 18 ADC, dont **Kog'Maw, Jinx, Twitch** |
| 0,80/0,05/0,05/0,05/0,05 | 20 top, dont **Quinn, Darius** |
| 0,05/0,05/0,24/0,05/0,61 | **Karma, Lux, Swain, Vel'Koz, Xerath** |
| 0,05/0,05/0,05/0,24/0,61 | **Senna, Seraphine** |
| 0,05/0,05/0,24/0,61/0,05 | Lucian, Smolder, Tristana, Ziggs |
| 0,24/0,05/0,61/0,05/0,05 | Akali, Akshan, Aurora, Cassiopeia, Ryze, Sylas, Vladimir, Yasuo, Yone |
| 0,61/0,05/0,24/0,05/0,05 | Cho'Gath, Irelia, Jayce, Kayle, Kennen, Rumble, Sion |
| 0,24/0,61/0,05/0,05/0,05 | Lillia, Rengar, Skarner, Trundle, Udyr, Volibear, Warwick, Wukong |
| 0,61/0,24/0,05/0,05/0,05 | Ambessa, Dr. Mundo, Gwen, Jax, Mordekaiser, Olaf |
| 0,05/0,05/0,61/0,05/0,24 | Annie, Galio, Hwei, Mel, Neeko, Veigar |
| 0,05/0,61/0,24/0,05/0,05 | Diana, Ekko, Karthus, Taliyah |
| 0,61/0,05/0,05/0,05/0,24 | Malphite, Sett, Shen, Tahm Kench |
| 0,05/0,24/0,61/0,05/0,05 | Naafiri, Qiyana, Talon, Zed |
| 0,05/0,61/0,05/0,05/0,24 | Amumu, Fiddlesticks, Shaco |
| 0,05/0,24/0,05/0,05/0,61 | Brand, Maokai, Morgana |
| 0,23/0,55/0,05/0,05/0,13 | Gragas, Poppy |
| 0,20/0,20/0,20/0,20/0,20 | Locke, Zaahen |
| 0,05/0,80/0,05/0,05/0,05 | 24 junglers |
| 0,05/0,05/0,05/0,05/0,80 | 21 supports |
| 0,05/0,05/0,80/0,05/0,05 | 16 mids |

**Champions suivis, par rôle réel** (drafts SYNTHÉTIQUES, toutes tailles) :

| Champion | Joué | n | Rôle juste | p moyenne du vrai rôle |
|---|---|---|---|---|
| Kog'Maw | ADC | 210 | 100,0 % | 0,89 |
| Xerath | Mid | 45 | 48,9 % [35,0-63,0] | 0,43 |
| Xerath | Support | 173 | 100,0 % | 0,75 |
| Lux | Mid | 58 | 50,0 % [37,5-62,5] | 0,45 |
| Lux | Support | 164 | 98,8 % | 0,77 |
| Quinn | Top | 178 | 100,0 % | 0,88 |
| Seraphine | ADC | 65 | 58,5 % [46,3-69,6] | 0,50 |
| Senna | ADC | 80 | 43,8 % [33,4-54,7] | 0,45 |
| Pantheon | Mid | 42 | 38,1 % [25,0-53,2] | 0,30 |
| Sylas | Top | 35 | 45,7 % [30,5-61,8] | 0,45 |

Kog'Maw mid et Quinn jungle n'apparaissent **jamais** dans ces drafts : la fiche ne leur donne qu'un rôle, donc le
générateur ne les y place pas. Leur évaluation sur plusieurs rôles demande la réserve réelle. L'outil la fait déjà
(tableau par champion et par rôle réel) dès qu'un fichier d'état est fourni ; avec le fichier de test synthétique et des
rôles live inventés, il affiche bien Kog'Maw mid / ADC et Quinn top / jungle séparément.

Couples champion × rôle les moins bien attribués (n ≥ 30, toutes tailles) : Cassiopeia top 22,6 %, Akshan top 23,8 %,
Heimerdinger top 23,9 %, Warwick top 32,6 %, Aurora top 33,3 %, Irelia mid 35,9 %, Trundle top 36,4 %, Ryze top 37,0 %,
Akali top 37,8 %, Pantheon mid 38,1 %, Jayce mid 39,4 %, Rengar top 39,5 %. Ce sont tous des rôles secondaires de la fiche.

## 6. Cas atypiques SYNTHÉTIQUES (24 cas, rôles annotés à la main)

- Rôles annotés retrouvés en premier : **81 sur 95** (crédit partagé en cas d'égalité).
- Adversaire affiché conforme à l'annotation (ou personne quand il n'est pas révélé) : **10 sur 24**.
  Dans les 14 autres, le moteur n'affiche personne (confiance faible) dans 12 cas, et nomme un autre champion dans 2.
- **Adversaire nommé à tort : 2 cas.**
  - `kog-mid-lucian` (Garen, Vi, Kog'Maw, Lucian, Nami ; Kog'Maw mid) : Lucian est affiché comme mid, confiance moyenne.
  - `malphite-mid` (Sion, Vi, Malphite, Jinx, Thresh ; Malphite mid) : Sion est affiché comme mid, confiance moyenne.
- Égalités exactes confirmées, confiance faible et personne de nommé : Kog'Maw/Jinx, Quinn/Darius, Seraphine/Senna
  (les deux sens), Xerath/Lux (les deux sens), Twitch/Jinx.
- Une correction suffit : avec « Jinx = ADC », Kog'Maw passe de p = 0,41 à 0,83 et il est nommé.
- `sylas-seul-mid` : Sylas seul est nommé (p = 0,61, confiance moyenne). `sylas-seul-top` : rien n'est nommé (p = 0,24).
- `pantheon-support-partiel` (Pantheon et Jinx révélés) : Pantheon est nommé au support avec p = 0,57, classe « possible ».
- `sans-fiche-par-elimination` : Locke a p(mid) = 0,77 mais n'est pas nommé, car il n'a aucune donnée. Prudent.

## 7. Tests

- `tools/run-role-tests.ps1` : 75 réussis, 0 échec (propriétés et instantané de 24 cas).
- `tools/run-coaching-tests.ps1` : conseils 133/0, égalités 18/0, maîtrise 91/0, moteur stratégique 190/0.
- Build et `--selftest` : « TOUT EST BON ».

## À faire pour une mesure réelle

Sur un PC où « Mettre à jour les rôles » a tourné avec la version 2.12 ou plus récente :

```powershell
pwsh -File tools/run-accuracy-tests.ps1 -LocalState -LiveFile "$env:LOCALAPPDATA\DraftMid\draft-role-rates-live.js" -OutJson "$env:TEMP\roles-reel.json"
```

Ne publier que des agrégats, avec l'étiquette « données locales réelles, n = … ». Priorités à vérifier : la calibration
réelle, le taux « nommé à tort » avec 3 ou 4 champions révélés, et les couples flex (Kog'Maw mid, Xerath/Lux, Quinn jungle).
