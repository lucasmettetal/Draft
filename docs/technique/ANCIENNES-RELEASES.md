# Anciennes releases : état et recommandation

**Aucune release n'a été supprimée ni modifiée.** Ce document prépare une décision, qui appartient au propriétaire du dépôt.

## État

| Élément | Constat |
|---|---|
| Releases publiques | V2.6.0 à V2.10.0, puis V2.14.0 (sans Meraki) et V2.16.0, la dernière (« Latest ») |
| Référence Meraki | Vérifiée dans l'exécutable de la V2.10.0. Le fichier est aussi dans les sources de la V2.6.0, donc les releases intermédiaires la contiennent probablement. Exécutables intermédiaires non inspectés |
| Droits de redistribution | **À clarifier.** La licence MIT du dépôt de Meraki couvre son code, et rien ne couvre `championrates.json` |
| Bilans personnels | Retirés du code en V2.13, donc présents dans ces versions plus anciennes |

**Ces téléchargements sont anciens. Ce ne sont pas les versions recommandées.** La version stable de référence est la V2.14, à compiler depuis `main`.

## Limite du retrait de Meraki

Depuis `main` à jour, le fichier est absent des fichiers courants et de la compilation par défaut. **Cela ne l'efface pas du passé :** il reste lisible dans l'historique Git depuis la V2.5, et dans les anciennes releases et leurs tags. Seule une réécriture de l'historique pourrait l'en retirer, et elle n'a pas été faite.

## Options

| Option | Effet | Inconvénient |
|---|---|---|
| A. Tout garder | Rien à faire | Le lien « Latest » mène à des données aux droits incertains |
| B. Avertir dans les notes des releases | Prévient les visiteurs | Le fichier reste distribué |
| C. Publier une V2.14 ou V2.15 sans Meraki | « Latest » pointe enfin vers une version propre | Demande une version validée |
| D. Retirer les exécutables anciens | Arrête leur distribution par ce biais | Destructif pour les liens existants. Les tags et l'historique restent |
| E. Réécrire l'historique | Seule façon de retirer le fichier du passé Git | Casse les clones et les liens. Ne supprime pas les copies déjà faites |
| F. Obtenir l'accord écrit de Meraki | Peut régler la question entièrement | Dépend de leur réponse |

## Recommandation

1. **Maintenant :** rien de destructif. Le README avertit (fait).
2. **À la prochaine release validée :** option C, avec l'option B sur les anciennes, pour que le lien de téléchargement mène à une version propre.
3. **Ensuite :** décider de D selon la réponse à F, si tu choisis de contacter Meraki.
4. **E seulement en dernier recours :** elle n'a de sens que si un ayant droit ou la confidentialité l'exige.
