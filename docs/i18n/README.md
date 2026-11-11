# Internationalisation (préparation)

Étude préparatoire pour DRAFT 3.0.0 bêta, bilingue français et anglais. Aucun fichier de production n'est modifié : l'extraction commence après la PR C-2 (vocabulaire).
Base reprise sans la refaire : `DRAFT-ux/docs/ux/PLAN-I18N-FR-EN.md`, `docs/moteur/AUDIT-VOCABULAIRE.md`, `docs/technique/GLOSSAIRE-LOL.md`.

| Document | Contenu |
|---|---|
| [FORMAT-CATALOGUE.md](FORMAT-CATALOGUE.md) | Un catalogue JSON strict par langue, partagé page et programme ; clés, variables, pluriels, échappement, chargement compatible CSP, repli |
| [INVENTAIRE.md](INVENTAIRE.md) et [inventaire.json](inventaire.json) | 594 entrées de la page et 156 entrées natives, avec clé et catégorie proposées (généré) |
| [STRATEGIE.md](STRATEGIE.md) | Programme C# et WebView2, messages de sécurité, messages structurés du moteur, CSV, détection et mémorisation de la langue |
| [GLOSSAIRE-FR-EN.md](GLOSSAIRE-FR-EN.md) | Termes LoL, rôles, ton, phrases types en français et en anglais |
| [TESTS.md](TESTS.md) | Contrôles, pseudo-langue, longueurs, relevé avant/après, autotests ; résultat du détecteur |
| [PLAN-EXECUTION.md](PLAN-EXECUTION.md) | I18N-1, I18N-2, I18N-3 : fichiers, ordre, risques |

Outils : `tools/i18n/` (voir TESTS.md § 1). Tout lancer : `pwsh tools\i18n\run-i18n-tests.ps1`.
