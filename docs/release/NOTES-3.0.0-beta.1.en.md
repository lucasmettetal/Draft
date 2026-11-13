# DRAFT 3.0.0-beta.1: test pre-release

**Pre-release for a small group of testers.** It works on a **copy** of your 2.x data and has **no automatic update**: download new versions manually. Your installed 2.x version is not changed.

DRAFT is not endorsed or supported by Riot Games. It never acts in the client: you always pick, ban and lock in yourself.

Version française : [NOTES-3.0.0-beta.1.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.1/docs/release/NOTES-3.0.0-beta.1.md).

## What changed since 2.16.2

- **Redesigned interface.** Five views (Home, My pool, Simulator, My stats, Settings). Settings grouped in six sections. A Home page that guides the first launch in three steps at most.
- **New art direction (B+).** Consistent colours, typography, role icons and panels. Dark title bar when Windows allows it.
- **Full English.** The whole interface, the panels, the tray icon menu, the balloons and the program messages.
- **Language choice.** Settings > General > Language: automatic (Windows language), French or English. The change applies without a restart. During a draft, the panels keep their language until the end.
- **Displayed roles consistent with the calculation.** A single scale for your lane opponent. The role assigned by League (if client reading is on) picks that role's pool; it is never guessed from the champion.
- **Explanations faithful to the calculation.** The reasons shown come from the calculation itself. No engine score or threshold changed.
- **League vocabulary harmonised** across the interface.
- **Final QA.** Grey win-rate bar under 50%, single panel readable at 125%, larger panel tools (32 px), Riot history with its period and the 10-game threshold, cause shown when Ctrl+Alt+D is already taken, a panel position is reused only if the panel stays visible.

## What is special about this pre-release

- **Data on a copy.** On first launch, if its folder is empty, the pre-release copies the 2.x data (settings, profiles, pools, journal, history) from `%LOCALAPPDATA%\DraftMid` to `%LOCALAPPDATA%\DRAFT-beta`. It reads the 2.x folder and never writes to it. After that, each version keeps its own data: what you change in the pre-release does not go back to 2.x.
- **Portable only.** A single file, `DRAFT-Portable.exe`. No installer, no uninstall.
- **No automatic update.** Settings > Updates says "Test pre-release: download new versions manually". The installed 2.x never offers this pre-release.
- **Next to a 2.x.** Both can run at the same time. Separate start with Windows (value `DRAFT-beta`).
- **Ctrl+Alt+D.** If another DRAFT is running, or if 2.x starts with Windows, the pre-release leaves it the shortcut. The panels stay in the tray icon menu. Settings > Display and windows explains why and lets you take the shortcut.

## Known limits

- **Real Ctrl+Alt+D not proven** on a session without another DRAFT (M2). Mechanism checked by an isolated test. A 2.x started by hand **after** the pre-release, without start with Windows, won't get Ctrl+Alt+D if the pre-release took it.
- **Multiple screens not tested** (m1): a single screen on the test PC.
- **m2: language and going back to 2.x.** The pre-release no longer shares its folder with 2.x: 2.x no longer rewrites the pre-release settings. But changes made in the pre-release don't go back to 2.x. To keep your pools: My pool > Export my profiles, then Import in 2.x (same file format). For the journal: My stats > Download the journal, then "Importer un journal" in 2.x.
- **No automatic move to the final 3.0**: it is downloaded manually too.
- **m3: slow first display of the panels**: 4.5 to 5.1 s after detection the first time (WebView2 start-up), then a few milliseconds.
- **If 2.x is open on first launch**, a file it holds exclusively is not copied. Home says so and counts these files.
- If another DRAFT runs during a draft, you will see two sets of panels. The pre-release says so (Home, Settings > Display and windows, balloon): hide the panels of one of them, or quit the other one yourself.

## Files

- `DRAFT-Portable.exe`: portable pre-release, built and tested by GitHub Actions.
- `DRAFT-Portable.exe.sig`: DRAFT signature, attached separately after the build.

Tester guide: [GUIDE-TESTEUR-3.0.0-beta.1.en.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.1/docs/release/GUIDE-TESTEUR-3.0.0-beta.1.en.md).
