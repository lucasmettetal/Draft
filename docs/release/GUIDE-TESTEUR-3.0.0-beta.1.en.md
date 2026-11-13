# Tester guide: DRAFT 3.0.0-beta.1

Thanks for testing this pre-release. It is meant to find errors before 3.0. Version française : [GUIDE-TESTEUR-3.0.0-beta.1.md](GUIDE-TESTEUR-3.0.0-beta.1.md).

**DRAFT is not endorsed or supported by Riot Games.** It never acts in the client: you always pick, ban and lock in yourself.

## In short

- This is a **test pre-release**: it may contain errors.
- It works on a **copy** of your 2.x data. Your 2.x is not changed.
- **No automatic update**: you download each new pre-release manually.
- A single **portable** file: nothing to install.

## 1. Install the pre-release

1. Download `DRAFT-Portable.exe` from the `v3.0.0-beta.1` pre-release page on GitHub (marked "Pre-release").
2. Check its hash, if you were given one. In PowerShell: `Get-FileHash .\DRAFT-Portable.exe -Algorithm SHA256`. Both values must match.
3. Put it in a separate folder, for example `C:\DRAFT-beta\`. **Do not replace** your 2.x file.
4. Run it. Windows may show a warning (SmartScreen), because the executable has no Microsoft certificate: "More info", then "Run anyway".
5. A **DRAFT** icon appears near the clock. On first launch, Home opens and a balloon says DRAFT works on a copy of your 2.x data.

You need Windows 10 or 11 with Microsoft Edge WebView2 (already on most PCs).

## 2. Living next to your 2.x

| Point | What happens |
|---|---|
| Data | On first launch, the pre-release copies your settings, profiles, pools, journal and history from `%LOCALAPPDATA%\DraftMid` to `%LOCALAPPDATA%\DRAFT-beta`. It reads 2.x without writing to it. After that, each version has its own data. |
| Running | The pre-release and 2.x can run at the same time. Neither closes the other. |
| Start with Windows | Separate. Ticking "Start with Windows" in the pre-release does not touch your 2.x one. |
| Ctrl+Alt+D | If your 2.x is running, or if it starts with Windows, the pre-release **leaves** it Ctrl+Alt+D. The panels stay in the tray icon menu ("Show panels"). Settings > Display and windows explains why, and the "Use Ctrl+Alt+D in this pre-release" box lets you take it. |
| Duplicate panels | If your 2.x runs during a draft, **its panels show up too**. To avoid it without changing it: quit it before the draft (right-click its icon > Quit), and start it again after the test. |
| Updates | The pre-release does not check for any. Your 2.x will never offer the pre-release. |

Good to know: if your 2.x is **open** on the pre-release's first launch, a file it holds exclusively is not copied. Home says so ("Files not copied because they were open elsewhere"). For a complete copy, quit your 2.x before the first launch.

## 3. What to test

For each point, note: OK or problem, and what you saw.

- [ ] **First launch**: Home opens, the "Test pre-release" card says your data comes from a copy, with the date.
- [ ] **Your data**: My pool shows your champions per role; My stats shows your journal and your history, as in your 2.x.
- [ ] **Language**: Settings > General > Language. Switch to English, then back to French. The window changes right away.
- [ ] **Real draft**: client reading (if you turned it on) or screen detection; the role assigned by League; that role's pool; the advice; "I'm playing"; the journal. Detailed protocol: [TEST-DRAFT-REELLE.md](TEST-DRAFT-REELLE.md) (French).
- [ ] **Panels**: they show during champion select, stick to the client, disappear when the game starts and after a dodge.
- [ ] **Ctrl+Alt+D**: depending on your case (see § 2). If you have a second Windows account, protocol: [TEST-CTRL-ALT-D-REEL.md](TEST-CTRL-ALT-D-REEL.md) (French).
- [ ] **Several screens** (if you have them): [PROCEDURE-MULTI-ECRANS.md](../qa/PROCEDURE-MULTI-ECRANS.md) (French).
- [ ] **Settings > Updates**: "Test pre-release: download new versions manually.", without a button.

## 4. Report a bug

Send to the person who gave you the pre-release:

1. What you were doing, what you expected, what happened.
2. The version: Settings > Diagnostics and info ("DRAFT 3.0.0-beta.1, test pre-release").
3. A screenshot, **without your Riot ID** or a visible game in progress (blur them).
4. If client reading misbehaves: Settings > Diagnostics and info > **League connection diagnostic**. The text has no password, port, path or player name: copy it as is.
5. If DRAFT does not start or crashes: the file `%LOCALAPPDATA%\DRAFT-beta\draftmid.log`.
6. If draft detection is wrong: tick Settings > Diagnostics and info > "Detection diagnostic log (numbers only)", do another draft, then send `%LOCALAPPDATA%\DRAFT-beta\diagnostic.log`.

**Never send** your Riot key, your history file or your credentials.

## 5. Go back to 2.x

1. Quit the pre-release: right-click its icon > **Quit**.
2. If you ticked "Start with Windows" in the pre-release, untick it **before** quitting (Settings > General).
3. Start your 2.x as usual. Its data has not changed.

To remove the pre-release completely: delete `DRAFT-Portable.exe` and the `%LOCALAPPDATA%\DRAFT-beta` folder.

## 6. Keep your changes

What you change in the pre-release (pools, exclusions, favourites) **does not go back** to your 2.x on its own.

- **Pools and preferences**: in the pre-release, My pool > **Export my profiles** (file `draft-profils.json`). In your 2.16.x: Réglages > Mes profils > **Importer**. Same format (`draft-config`, version 1).
- **Journal**: in the pre-release, My stats > **Download the journal** (file `draft-journal.json`). In your 2.16.x: **Importer un journal**. Games are added, nothing is deleted (same file check in both versions).
- **Imported Riot history**: it stays in the pre-release (`%LOCALAPPDATA%\DRAFT-beta`). Your 2.x keeps its own.
- **Language**: 2.x is French only.

## 7. New pre-release

Download it manually and replace `DRAFT-Portable.exe` in your `C:\DRAFT-beta\` folder. It keeps the `%LOCALAPPDATA%\DRAFT-beta` folder: the copy from 2.x is made only once, when that folder is empty.
