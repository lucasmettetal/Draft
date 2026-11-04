# Détection de la draft et suivi du client

La détection de la fenêtre et celle de la phase sont indépendantes.

| Classe | Rôle |
|---|---|
| `ClientWindowTracker` | Trouve le client par processus et classe de fenêtre, suit ses changements par événements Windows |
| `DraftPhaseDetector` | Estime la phase à partir d'une image très réduite |
| `PhaseMachine` | Exige plusieurs lectures concordantes, pour éviter le clignotement |
| `PanelVisibilityController` | Décide de l'affichage |
| `Layout` | Calcule la position des panneaux |
| `UserPreferences` | Enregistre les préférences et ce qui a été appris |
| `LcuReader` | Lecture LCU, verrouillée : voir [LCU.md](LCU.md) |

## Fiabilité de chaque signal

| État | Source | Fiabilité |
|---|---|---|
| Client fermé | Fenêtre Windows | Certain |
| Client réduit | Fenêtre Windows | Certain |
| Partie en cours | Processus `League of Legends` | Certain |
| Draft ou hors draft | Reconnaissance visuelle locale | **Estimation, calibrée sur une seule partie personnalisée** |

## Reconnaissance visuelle

**Mesuré le 4 novembre 2026, sur 133 images d'une partie personnalisée :** le salon et la sélection ont presque le même haut et le même bas d'écran. Ce qui les sépare est le centre : vide dans le salon, chargé en portraits dans la sélection. L'accueil garde sa barre de navigation et n'a pas de centre chargé. Je suppose que le salon était bien l'écran sans portraits : le journal ne contient que des chiffres, pas de nom d'écran.

| Écran | Centre chargé | Conclusion |
|---|---|---|
| Accueil, salon, chargement | Non | Hors draft |
| Écran corrigé « pas une draft » | Oui | Hors draft |
| Écran proche d'une draft retenue | Oui | Draft |
| Barre de navigation présente | Oui | Hors draft |
| Barre absente | Oui | Draft |
| Aucune référence ni correction | Oui | Incertain, aucun panneau |

- Une fois par seconde, si le client est réellement visible, une image de 96x54 pixels est lue en mémoire, puis remplacée à la lecture suivante.
- Si une autre fenêtre le recouvre, aucune lecture n'est faite et aucune conclusion n'est tirée.
- La barre de navigation est apprise seule, seulement sur un écran sans portraits au centre.
- `Ctrl+Alt+D` affiche ou masque les panneaux. L'affichage retient l'écran s'il contient des portraits, le masquage n'apprend rien. Le menu de l'icône permet de marquer un écran comme draft ou non.
- Une signature de 48 valeurs est enregistrée dans `settings.json`, jamais une image. Rien n'est transmis.

**Risques connus :**

- Le classé et les autres modes n'ont pas encore été mesurés.
- Une référence apprise dans un salon empêche de reconnaître une sélection tant qu'aucune draft n'a été retenue. Un affichage avec `Ctrl+Alt+D` en sélection corrige ce cas.
- Une fin de sélection sans portraits au centre masquerait les panneaux après 3 lectures.
- Client recouvert : aucune détection tant qu'il le reste. Une draft de plus de 8 minutes redevient incertaine, et la référence est oubliée.
- Une capture d'écran à chaque seconde coûte environ 17 ms.

**Diagnostic :** l'option « Journal de diagnostic » écrit des chiffres dans `%LOCALAPPDATA%\DraftMid\diagnostic.log`, sans image ni nom. Le fichier repart de zéro au-delà de 2 Mo.

## Anti-clignotement

- Entrée en draft : 2 lectures concordantes.
- Sortie de draft : 3 lectures concordantes. Une lecture incertaine maintient l'état.
- Client fermé, réduit ou partie lancée : masquage immédiat.

## Suivi

Le déplacement est suivi par `SetWinEventHook`, sans injection, avec au plus une mise à jour toutes les 16 ms. Les deux panneaux bougent ensemble via `DeferWindowPos`, sans prendre le focus. Le passage entre deux panneaux et panneau unique attend 350 ms de stabilité. Une recherche lente, toutes les 2 secondes, ne sert qu'à détecter l'ouverture du client. DraftMid ne déplace ni ne redimensionne jamais le client.

WebView2 n'est chargé que lorsque le client est ouvert, et libéré à sa fermeture.
