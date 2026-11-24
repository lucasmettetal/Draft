# DRAFT 3.0.0-beta.3: test pre-release

**Pre-release for a small group of testers.** It has **no automatic update**: download new versions by hand. Your installed 2.x is not touched and never offers this pre-release.

DRAFT is not endorsed or supported by Riot Games. It never acts in the client: you always pick, ban and lock in yourself.

Version française : [NOTES-3.0.0-beta.3.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.3/docs/release/NOTES-3.0.0-beta.3.md).

## What changed since beta.2

- **Result recorded automatically.**
  - With client reading on, at the end of a normal or ranked game, DRAFT reads whether your team won.
  - The end-of-game screen confirms it: your champion in large and "Win logged automatically ✓".
  - A small "Correct" link shows Win, Loss and Ignore again.
  - Remake, missing summary, or DRAFT closed before the end: nothing is made up. The two buttons stay, or the "Last game" card on Home.
  - A result you entered yourself is never replaced. My statistics shows "read from the client".
- **Matching with your Riot history.**
  - After importing your history (My statistics > Riot history), a recorded game is linked to the Riot game with the **same ID**. Nothing is guessed from the time or the champion.
  - It shows "Confirmed by Riot", with K/D/A, duration and patch.
  - **Riot is the reference** in the statistics. If your recorded result differs, a "Mismatch" label is shown. Nothing is deleted.

## Client reading: what is read

It stays read-only, only on your PC. It now uses **three** addresses: the two from beta.2, plus the **end-of-game summary**.

From that summary, DRAFT reads only:
- the game ID;
- your team;
- the winning team;
- the remake flag.

It **never** reads other players' names or statistics, nor your own statistics.

If you had already turned client reading on, you are not asked again. The texts in Settings > League say so. Details: `docs/technique/LCU.md`.

## About this pre-release

- **Same data as beta.1 and beta.2** (`%LOCALAPPDATA%\DRAFT-beta`): you keep your pool and journal.
- **Portable only.** A single file, `DRAFT-Portable.exe`.
- **Next to a 2.x:** both can run. If another DRAFT is running, the pre-release leaves Ctrl+Alt+D to it and you see two sets of panels: quit one of them.

## Known limits

- **End-of-game summary:** its format is not documented by Riot. If DRAFT cannot read it, it falls back to the buttons. Please report it: the connection diagnostic shows `bilan=lu|absent|illisible|remake` (read, missing, unreadable, remake), with no names.
- **Riot history:** only ranked solo/duo games are imported, and only games recorded since beta.2 carry the needed ID. The import stays manual, with your Riot key.
- **Multiple screens:** not tested.
- **Real Ctrl+Alt+D:** not proven on a session without another DRAFT.
- **First panel display:** about 5 s (WebView2 creation), then instant.

## Files

- `DRAFT-Portable.exe`: portable pre-release, built and tested by GitHub Actions.
- `DRAFT-Portable.exe.sig`: DRAFT signature, attached separately after the build.
