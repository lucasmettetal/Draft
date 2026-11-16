# Documentation de DRAFT

## Où se trouve quoi

| Fonctionnalité | V2.14 stable (`main`) | Moteur (`feature/strategic-engine-coaching`) | Candidate (`integration/v2.15-coaching`) |
|---|---|---|---|
| Panneaux, profils, lecture du client | Oui | Base V2.13 | Oui |
| Navigation en 5 onglets | Non (6 onglets) | Non | Oui |
| Finition de l'interface, correctifs des réglages | Oui | Non | Oui |
| Égalités sans effet de l'ordre du pool | Non | Dans le moteur | Moteur et écran |
| Moteur de conseils | Non | Non branché | Onglet Conseils |
| Le plus joué dans le rôle | Non | Non branché | Oui |
| Import Riot dans l'application | Non (script seulement) | Sans bouton | Dans les réglages |
| Modèle T (départage par l'expérience) | Non | Prototype | Désactivé |
| Profil OP.GG | Non | Non | Non : prototype local non suivi |
| Bans adverses | Non | Non | Non : expérience locale non suivie |

## Pour utiliser DRAFT

[Guide de la bêta privée](utilisateur/GUIDE-BETA.md), pour les testeurs.

[Guide d'utilisation](utilisateur/GUIDE.md) : installation, configuration, profils, détection, recommandations, import, sauvegarde, dépannage.

## Pour comprendre et modifier

| Document | Sujet |
|---|---|
| [Profils, migration, rôle attribué](technique/PROFILS.md) | Renommage, stockage, export, compatibilité, `assignedPosition` |
| [Architecture](technique/ARCHITECTURE.md) | C# et WebView2, frontend, compilation, carte des fichiers |
| [Moteur](technique/MOTEUR.md) | Calcul du conseil, stabilité, historique des changements |
| [Données](technique/DONNEES.md) | Sources, rôles probabilistes, fiabilité, licences |
| [Journal des drafts](technique/JOURNAL.md) | Format d'une entrée, égalités, origine, anciennes entrées, compatibilité 2.16.2 |
| [Détection](technique/DETECTION.md) | Fenêtre, phase, reconnaissance visuelle |
| [Lecture du client](technique/LCU.md) | Endpoints, politique de Riot, garanties |
| [Intégration Riot](technique/INTEGRATION-RIOT.md) | Méthodes, conditions, risque résiduel |
| [Multiprofil](technique/MULTIPROFIL.md) | Dépendances personnelles restantes |
| [Validation](technique/VALIDATION.md) | Tests automatiques et manuels |
| [Validation réelle](technique/VALIDATION-REELLE.md) | Protocole sur de vraies drafts, pas encore réalisé |
| [Branches et publication](technique/BRANCHES.md) | Stratégie, worktrees, workflow, contrôles avant push |
| [Anciennes releases](technique/ANCIENNES-RELEASES.md) | Statut, limite du retrait de Meraki, recommandation |

## Travaux non livrés

- [Statut des expériences](experimental/README.md)
- [OP.GG](experimental/OPGG.md)
- [Bans adverses](experimental/BANS.md)
- [Feuille de route V2.15](ROADMAP-V2.15.md)

**Règle de rédaction :** un document dit ce qui fonctionne dans quelle version, et distingue ce qui est mesuré de ce qui est supposé. Un prototype n'est jamais présenté comme disponible.
