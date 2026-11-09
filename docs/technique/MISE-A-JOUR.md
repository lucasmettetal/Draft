# Mise à jour depuis l'application

Code : `src/AppUpdater.cs`. Déclenchée seulement par un clic (réglages ou menu de l'icône).

1. Lecture de `api.github.com/repos/lucasmettetal/Draft/releases/latest`. Brouillons, préversions et tags autres que `vX.Y.Z` sont ignorés.
2. Si la version est strictement plus récente, l'utilisateur confirme. Jamais de retour en arrière.
3. Téléchargement de l'asset `*-Portable.exe`, à côté de l'exécutable (`<nom>.maj.exe`). L'adresse doit être celle des releases du dépôt, et la redirection doit rester sur GitHub.
4. Vérifications : taille, en-tête d'exécutable, SHA-256 fourni par GitHub (`digest`), version interne égale au tag, puis **signature** RSA (`<asset>.sig`) sur le tag et l'empreinte, contrôlée avec la clé publique embarquée. Un fichier mal signé est supprimé sans jamais être lancé.
5. Le candidat lance `--selftest` sur un dossier de données temporaire. Il doit réussir.
6. Refus si une draft, une partie, un import ou un calcul a commencé entre-temps.
7. L'exécutable courant est renommé `<nom>.exe.ancien`, le candidat prend sa place. L'application se ferme, puis son processus lance la nouvelle version avec `--after-update <pid>` et surveille son démarrage.
8. La nouvelle version confirme après 5 secondes sans plantage (ou à une fermeture normale), puis supprime `.ancien`. Sans confirmation sous 60 secondes, ou si elle s'arrête avant, elle est arrêtée, l'ancienne version est remise en place et relancée.

Le fichier garde son nom et son emplacement : le démarrage automatique et les raccourcis restent valables. Les données ne sont pas touchées.

**Clé de signature.** Créée une seule fois par `tools/new-update-key.ps1`, dans `%APPDATA%\DRAFT-signature`, jamais dans le dépôt ni dans les secrets GitHub. **Sa perte interdit toute mise à jour automatique** des versions déjà distribuées : en garder une copie à part.

**Limites.** Cette signature protège le bouton de mise à jour, mais ne supprime pas l'avertissement SmartScreen : il faudrait un certificat de signature de code. Un défaut qui apparaît après la confirmation de démarrage ne déclenche pas de retour en arrière. Une version sans ce module (2.14 et avant) doit être remplacée une fois à la main.

**Pour publier :** un tag `vX.Y.Z` supérieur à la version installée, avec `AssemblyFileVersion` égale au tag. Le workflow joint les exécutables. Ensuite : `.\tools\sign-release.ps1 -Tag vX.Y.Z`, sur le PC qui détient la clé. Tant que la signature n'est pas jointe, DRAFT refuse cette version.

**Tester :** `.\tools\test-update.ps1` (dossiers isolés, une vraie mise à jour et une lecture réelle de GitHub, sans rien remplacer d'installé).
