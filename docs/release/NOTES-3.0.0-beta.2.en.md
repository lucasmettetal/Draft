# DRAFT 3.0.0-beta.2: test pre-release

**Pre-release for a small group of testers.** It has **no automatic update**: download new versions by hand. Your installed 2.x is not touched and never offers this pre-release.

DRAFT is not endorsed or supported by Riot Games. It never acts in the client: you always pick, ban and lock in yourself.

Version française : [NOTES-3.0.0-beta.2.md](https://github.com/lucasmettetal/Draft/blob/v3.0.0-beta.2/docs/release/NOTES-3.0.0-beta.2.md).

## What changed since beta.1

- **The panels become the main screen.**
  - Everything can be decided without opening the large window.
  - Immediate reading: role, advice, threat, viewed champion and action.
  - Deeper reading in collapsible sections: alternative, pool, game plan, threats, suggested bans and criteria.
  - A panel you close by hand (left, right or compact) stays closed for the session. To reopen it: DRAFT icon > "Show panels".
  - Left panel closed: the enemy team composition stays available in the right panel, in a collapsible section.
  - In compact mode, the 5 enemies collapse with one click.
- **"In League": your pick read from the client.** With client reading on, the panel shows "In League: X (hovered)", then "locked ✓", swaps included. The champion you view in DRAFT does not depend on it.
- **Automatic journal.**
  - With client reading on, in normal or ranked draft, your game is recorded **when it starts**, with your final champion. There is no "I'm playing" to click anymore.
  - Without reading, or in another mode, "I'm playing" stays.
  - One entry per game. No made-up result.
- **Cancelled drafts** (dodge, back to lobby): they stay visible with the label "Cancelled", but never count as a game. The next draft is recorded correctly.
- **Unavailable champions.**
  - Champions banned (by either team) and champions locked by a teammate are marked in your pool: "Banned", "Taken by an ally", "Taken by the enemy". They cannot be confirmed.
  - If the advice becomes unavailable, a sentence names the next one.
  - Suggested bans no longer name a champion already banned or taken.
  - The calculation does not change: only unavailable candidates are removed.
- **End-of-game screen.** When the game ends, the panel shows your champion in large, with two big buttons, Win and Loss. If you missed it, the "Last game" card on Home offers the same entry.
- **My statistics redone.**
  - Three tabs: overview, DRAFT journal and Riot history.
  - The status of every game is visible: "Pending", "Unconfirmed", "Cancelled" or the result.
  - No percentage below 10 games: a gauge shows what is missing.
  - Automatically recorded games count as real games.

## Client reading: what is read

It stays read-only, only on your PC, on the same two addresses as beta.1. In champ select, DRAFT reads only:
- your position;
- your own pick;
- the IDs of banned or locked champions.

It **never** reads a player's name, tag or ID, nor a teammate's hover or intent. Details: `docs/technique/LCU.md`.

## About this pre-release

- **Same data as beta.1** (`%LOCALAPPDATA%\DRAFT-beta`): you keep your pool and journal. Without beta.1, the first launch copies the 2.x data, without ever changing the 2.x.
- **Portable only.** A single file, `DRAFT-Portable.exe`.
- **Next to a 2.x:** both can run. If another DRAFT is running, the pre-release leaves Ctrl+Alt+D to it, and you see two sets of panels: quit one of them.

## Known limits

- **The game result is still entered by hand**: end-of-game screen or Home. Reading it automatically from the client is planned for a later version.
- **End-of-game screen:** it depends on client reading. Without it, enter the result from Home.
- **Bans and teammate picks:** format checked on rebuilt sessions in the real format. Report any wrong marking.
- **Multiple screens:** not tested.
- **Real Ctrl+Alt+D:** not proven on a session without another DRAFT.
- **First panel display:** about 5 s (WebView2 creation), then instant.
- **Tie between champions of different styles:** the explanation stays generic.

## Files

- `DRAFT-Portable.exe`: portable pre-release, built and tested by GitHub Actions.
- `DRAFT-Portable.exe.sig`: DRAFT signature, attached separately after the build.
