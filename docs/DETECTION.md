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
| Draft ou hors draft | Reconnaissance visuelle locale | **Estimation, jamais validée sur une vraie draft** |

## Reconnaissance visuelle

**Hypothèse à confirmer :** la barre de navigation du client disparaît pendant la sélection, et le centre de l'écran se remplit de portraits.

- Une fois par seconde, si le client est réellement visible, une image de 96x54 pixels est lue en mémoire, puis remplacée à la lecture suivante.
- Si une autre fenêtre le recouvre, aucune lecture n'est faite et aucune conclusion n'est tirée.
- La barre de navigation est apprise seule, quand elle reste stable plusieurs secondes.
- `Ctrl+Alt+D` et le menu de l'icône corrigent l'affichage. Une correction enregistre une signature de 48 valeurs dans `settings.json`, jamais une image, et elle passe avant les règles.
- Rien n'est transmis.

**Risques connus :**

- Lancé en pleine draft, DraftMid peut apprendre une mauvaise référence. Une draft de plus de 8 minutes redevient incertaine, et la référence est oubliée.
- D'autres écrans sans barre de navigation peuvent déclencher une fausse détection.
- Une capture d'écran à chaque seconde coûte environ 20 ms.

**Diagnostic :** l'option « Journal de diagnostic » écrit des chiffres dans `%LOCALAPPDATA%\DraftMid\diagnostic.log`, sans image ni nom. C'est ce qu'il faut pour calibrer les seuils après une vraie draft.

## Anti-clignotement

- Entrée en draft : 2 lectures concordantes.
- Sortie de draft : 3 lectures concordantes. Une lecture incertaine maintient l'état.
- Client fermé, réduit ou partie lancée : masquage immédiat.

## Suivi

Le déplacement est suivi par `SetWinEventHook`, sans injection, avec au plus une mise à jour toutes les 16 ms. Les deux panneaux bougent ensemble via `DeferWindowPos`, sans prendre le focus. Le passage entre deux panneaux et panneau unique attend 350 ms de stabilité. Une recherche lente, toutes les 2 secondes, ne sert qu'à détecter l'ouverture du client. DraftMid ne déplace ni ne redimensionne jamais le client.

WebView2 n'est chargé que lorsque le client est ouvert, et libéré à sa fermeture.
