# Coaching à partir des statistiques : architecture (étude)

Priorité 5. Étude seulement : rien n'est à implémenter avant validation.
Base : `main` 6218022, et l'audit [`AUDIT-HISTORIQUE-ET-JOURNAL.md`](AUDIT-HISTORIQUE-ET-JOURNAL.md).
Prototype de calcul : [`maquettes/stats-calculs.js`](maquettes/stats-calculs.js) (fonctions pures) et [`maquettes/commun.js`](maquettes/commun.js) (`describe`, mise en texte). Les exemples ci-dessous sont **calculés** par ce prototype sur les données **fictives** des maquettes.

## 1. Règle explicite

**Aucune conclusion inventée.**

1. Une conclusion = une règle écrite + les données qu'elle lit + l'échantillon + l'intervalle. Les quatre s'affichent avec elle.
2. Sous le seuil, la page dit « pas encore » et ce qui manque (« 22 parties sur 30 »). Elle ne dit jamais « tendance », « plutôt », « semble ».
3. Un intervalle qui contient la référence donne « pas d'écart net ». Ce n'est pas un échec : c'est une information.
4. Le texte ne dit jamais plus que la règle. « Au-dessus de ta moyenne en Mid » ; jamais « tu es fort sur Orianna ».
5. Aucune comparaison à d'autres joueurs, aucun niveau global, aucun rang (principes de `MAITRISE.md`).
6. Aucune cause. Les parties ne sont pas tirées au hasard : on décrit, on n'explique pas.
7. Le coaching ne change jamais le score du moteur.

## 2. Ce que le coaching voudrait dire, et ce qu'il faut pour le dire

| Conclusion | Question | Données nécessaires | Disponible aujourd'hui | Règle proposée |
|---|---|---|---|---|
| Point fort | Ce champion fait-il mieux que mes autres champions du même poste ? | `champ`, `pos`, `win` | Oui | 30 parties sur le champion **et** 30 sur les autres du poste ; Wilson 95 % au-dessus de la référence. Même règle que `personal.results` et `familiarityProfile`. |
| Point faible | … moins bien ? | idem | Oui | Wilson 95 % en dessous de la référence. |
| Champions principaux | Lesquels je joue le plus ? | `champ`, `time` | Oui | Volume seulement, jamais les victoires. « Le plus familier » à partir de 5 parties et 5 d'écart (`FAMILIAR_MIN_GAMES`, `FAMILIAR_CLEAR_GAP`). |
| Meilleur champion | Lequel me réussit le mieux ? | idem | Oui, mais rarement concluant | N'existe que comme **point fort établi**. Jamais un classement des taux bruts. |
| Duel difficile (mid) | Contre qui je perds plus que ma moyenne ? | `mid` quand `pos` = `MIDDLE` | Mid seulement | 10 parties contre le même adversaire ; Wilson **99 %** sous la moyenne du mid (plusieurs duels testés). |
| Duel difficile (autres postes) | idem en top, jungle, bot, support | poste des adversaires | **Non** | Après l'import de `enemyPos` (audit § 10). |
| Tendance | Est-ce que je progresse dans mon poste ? | `time`, `win`, `pos` | Oui | 40 parties dans le poste, deux moitiés, \|z\| ≥ 2 (`performanceTrend`). |
| KDA dans le temps | Mon KDA change-t-il sur ce champion ? | `k`, `d`, `a` | Oui | 20 parties sur le même champion et poste (`KDA_MIN_GAMES`). Jamais entre rôles. |
| Conseil joué ou non | Les parties où j'ai joué le conseil se passent-elles mieux ? | journal : `suggested`, `picked`, `result` | Oui, saisi à la main | 10 parties terminées dans chaque groupe pour un taux ; **jamais** de conclusion causale. À égalité, l'ex æquo choisi compte comme conseil joué (règle de `fix/choix-champion`, à valider). |
| Farm, vision, objets | — | non importés | **Non** | Hors périmètre tant que l'import ne les garde pas. |

## 3. Données disponibles et ce qu'elles permettent

| Échantillon (fictif) | Ce qui est établi | Ce qui ne l'est pas |
|---|---|---|
| 0 partie | Rien. | Tout. La page invite à importer ou à noter. |
| 5 drafts, 0 résultat | Rien sur le conseil. | « Conseil joué ou autre choix : il faut 10 parties terminées dans chaque groupe. Tu en as 0 et 0. » |
| 93 parties (71 mid, 17 champions, 39 adversaires mid) | « Pas d'évolution nette en Mid » : 40 % sur 35 parties, puis 50 % sur 36 (z = 0,85). | Points forts : Orianna, le plus joué, a 22 parties sur 30. Duels : Yasuo, le plus croisé, a 5 parties sur 10. |
| 400 parties, 64 drafts | Orianna au-dessus de la moyenne en Mid ; Syndra en dessous ; progrès en Mid ; duel difficile contre LeBlanc. | Ahri, Viktor, Lee Sin : pas d'écart net. 4 autres duels : pas d'écart net. Conseil joué ou non : pas d'écart net. |

Lecture : avec un premier import d'une centaine de parties, le coaching honnête ne peut presque rien affirmer. Il doit le dire, et montrer ce qui manque.

## 4. Limites statistiques

### Tailles minimales et écarts détectables

Pour qu'un champion soit « au-dessus » d'une référence à 50 % (Wilson 95 %, calculé) :

| Parties sur le champion | Taux de victoire nécessaire |
|---|---|
| 30 | 70 % (21 V) |
| 50 | 64 % |
| 100 | 60 % |
| 200 | 57 % |
| 400 | 55 % |

Pour « en dessous » : 30 % à 30 parties, 40 % à 100 parties. Pour un duel difficile à 99 % : au plus 1 victoire sur 12, 2 sur 15, 4 sur 20.

Conséquence : seuls les écarts **forts** sont visibles avant plusieurs centaines de parties. C'est voulu. Un faux point faible ferait abandonner un bon champion.

### Comparaisons multiples

Avec 17 champions et 39 adversaires, une règle à 95 % testée partout trouverait des écarts par hasard. Garde-fous :

- on ne teste que ce qui passe le seuil (30 parties, 10 parties de duel) ;
- les duels, nombreux, utilisent un intervalle à 99 % ; le texte de la règle le dit (« car 5 duels sont testés ») ;
- la tendance est testée dans **un** poste, le plus joué.

### Biais connus, écrits dans la page

- **Choix non aléatoire.** On joue un champion quand il convient. Son taux mélange champion et contexte.
- **Patchs.** Un champion modifié n'est plus le même. La familiarité le prévoit déjà (`changed`) ; les conclusions devront le faire avant d'aller plus loin.
- **Comptes multiples.** Une conclusion porte sur un compte, ou dit qu'elle les mélange.
- **Journal déclaratif.** On note plus volontiers certaines parties. Les simulations anciennes ne sont pas distinguables.
- **Durées anciennes.** `sec` en millisecondes avant 11.20 : à écarter (audit § 2).

## 5. Architecture proposée

```
 sources (lecture seule)      normalisation            calcul pur              conclusions               rendu
 ─────────────────────        ──────────────           ──────────              ───────────               ─────
 DRAFT_HISTORY  ─┐            noms Riot → noms         bilan, kda, duree,      règles déclarées :         Mes statistiques
 DRAFT_STATS    ─┼──────────▶ postes, comptes,  ─────▶ parChampion,     ─────▶ { id, besoin, seuil,  ───▶ (A ou B)
 draftJournal   ─┘            durées en ms,            parPoste, parPatch,     évaluer() }               Conseils du
                              simulations écartées     tranches, vsMid,        → établi | non concluant   Simulateur
                                                       journal, tendance,      | insuffisant              (même vérité)
                                                       wilson (une seule)      + preuve + échantillon
```

### Modules (fichiers proposés, à créer seulement après validation)

1. **`web/draft-stats-sources.js`** : lit les trois sources, normalise (`masteryName`, postes, comptes), écarte les durées aberrantes et les entrées de simulation marquées. Renvoie `{ rows, entries, meta }`. Aucune écriture.
2. **`web/draft-stats-calc.js`** : fonctions pures du prototype (`stats-calculs.js`). Une **seule** fonction de Wilson pour toute l'application : aujourd'hui `wilsonInterval` (`draft-mastery.js`, l. 60) et `coachWilson` (`draft-coaching.js`, l. 501) font la même chose.
3. **`web/draft-stats-findings.js`** : une liste de règles déclarées. Chaque règle donne :
   - `id`, `version` ;
   - `besoin` : champs requis (`["pos", "win"]`, plus tard `["enemyPos"]`) ;
   - `seuil` : `{ parties, reference, z }`, tirés d'une table unique `STATS_RULES` ;
   - `evaluer(donnees)` → `{ statut: "etabli" | "non-concluant" | "insuffisant", sens, mesure, reference, regle, echantillon, manque }`.
   Une règle dont un champ manque renvoie `insuffisant` avec `manque: "donnée non importée"`, jamais une valeur par défaut.
4. **Rendu** (`index.html`) : lit les conclusions, ne calcule rien. La mise en texte passe par des clés `stats.findings.*` et `stats.withheld.*` (une phrase complète par clé). Prototype : `describe()` dans `maquettes/commun.js`.
5. **Conseils du Simulateur** : `personal.results` et `personal.too-few` (`draft-coaching.js`) consomment les mêmes conclusions. Une seule vérité, deux affichages.

### Forme d'une conclusion (exemple calculé, données fictives)

```js
{ type: "champion", statut: "etabli", sens: "fort", sujet: "Orianna", role: "mid",
  mesure:    { victoires: 60, parties: 95, taux: 0.632, intervalle: [0.531, 0.722] },
  reference: { taux: 0.483, parties: 205 },            // ses autres champions en mid
  regle:     { min: 30, z: 1.96, niveau: 95 },
  echantillon: { parties: 400, debut: "17/09/2025", fin: "10/11/2026", patchs: 29, comptes: 1 } }
```

Rendu : « Orianna : au-dessus de ta moyenne en Mid. 60V 35D sur 95 parties : 63 %, entre 53 % et 72 %. Tes autres champions en Mid : 48 % sur 205 parties. Règle : 30 parties sur le champion et sur tes autres champions du poste ; intervalle à 95 % qui exclut ta moyenne. »

### Tests (à écrire avec l'implémentation)

- **Pas de conclusion sous le seuil** : pour tout jeu de données sous les seuils, aucune conclusion `etabli`.
- **Faux positifs** : 1 000 historiques simulés à 50 % partout ; taux de conclusions `etabli` mesuré et borné.
- **Référence** : les cas du § 3 (93 et 400 parties fictives) donnent exactement les statuts listés.
- **Texte** : chaque conclusion affichée contient ses nombres (parties, intervalle, référence).
- **Champs absents** : une règle `enemyPos` sur un historique sans ce champ renvoie `insuffisant`.

### Évolution

- Nouveau champ d'import → nouvelle règle qui le déclare dans `besoin`. Les anciennes lignes restent lisibles.
- Seuils dans une table unique, cités par le texte (`{min}`), jamais recopiés à la main.
- Versions des règles dans la conclusion : une capture ou un export dit quelle règle a parlé.

## 6. Ce que le coaching ne fera pas

- Conseiller un build, des runes, une façon de farmer : données absentes.
- Dire « DRAFT t'a fait gagner » : impossible sans choix au hasard (audit § 8).
- Classer les champions par taux de victoire brut.
- Changer le classement du moteur à partir de ces conclusions.
