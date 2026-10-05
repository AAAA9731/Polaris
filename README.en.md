# Polaris

A mod management and diagnostics tool for Alice in Cradle.

Once installed, it keeps an eye on the game for you: which mod went wrong and how badly, right inside the game; if the game crashes, a popup tells you why; when a new version is out, it lets you know and helps you update.

[简体中文](README.md)

## What it can do

**One-click install.** Double-click the installer, pick your game, and click "Install" — BepInEx and Polaris are installed together, with no manual file copying.

**Mod management.** Tick mods to enable or disable them on the "Polaris" page of the title screen; changes take effect after restarting the game.

**Tiered error diagnostics.** Minor errors are quietly logged in the background; you're only interrupted when they're serious enough to affect the game:

| Level | When | What you'll see |
|---|---|---|
| Minor | A mod errors for the first time | A small toast in the top-right corner; click to see details |
| Persistent | The same error keeps recurring, or one mod produces many different errors | A dialog that lets you disable that mod with one click (takes effect after restart) |
| Severe | An error lasts too long, several mods fail at once, the game freezes too long, Polaris's own patches fail, or memory runs out | A full-screen notice, then the game exits automatically after a countdown |

The vanilla game's own errors won't bother you. Mods that have had persistent or severe issues are flagged on the mod management page; if that mod's files are updated later, the flag turns into a one-time "possibly fixed" notice.

**Crash window.** When the game exits unexpectedly, a window pops up explaining the likely cause: the exit code, the faulting module recorded by the system, errors before the crash, and the most suspicious mod.

**Automatic updates.** After the game starts, Polaris checks for a new version. If there is one, it asks you in-game, and the download only starts once you click "Update"; files are replaced automatically after you quit the game, so your current session isn't interrupted.

## Installation

Requires Windows and a copy of Alice in Cradle.

1. Go to [Releases](https://github.com/AAAA9731/Polaris/releases) and download `PolarisInstaller.exe` (a single file, no separate .NET install needed).
2. Run it, click "Browse…", and select the game's `AliceInCradle.exe`. You can also drag `AliceInCradle.exe` or the game folder straight into the window; if you run the installer from inside the game folder, it will detect it automatically.
3. Click "Install".

The installer backs up the original files it replaces first, and by default it leaves an existing BepInEx installation alone.

## Updating

Usually there's nothing to do: just click "Update" when prompted in-game. You can also rerun the latest installer at any time.

## Uninstalling

Run the installer, select the game folder, and click "Uninstall". It only removes Polaris's own files and restores the backed-up originals; your other mods are left untouched. If you're sure you no longer need BepInEx, tick "Also uninstall BepInEx".

## FAQ

**Windows says "Windows protected your PC"?** The installer isn't code-signed, so Windows may show a SmartScreen warning. Click "More info" and then "Run anyway". A checksum is included with every Release (`PolarisInstaller.exe.sha256`) if you'd like to verify it yourself.

**Where are the reports and logs?** Error reports are in `BepInEx/Polaris/reports/` in the game folder — one file per game session, keeping the 20 most recent; the BepInEx log is at `BepInEx/LogOutput.log`.

**How do I turn off update checks?** Edit `BepInEx/config/Polaris/_polaris_update.cfg` and change `CheckForUpdates` to `false`.

**Polaris stopped working after a game update?** Polaris is compiled for a specific game version (currently ver030i). After a major game update, you may need to wait for a new Polaris release; if its core patches stop working, it will tell you and exit rather than keep running in a broken state.

**Don't want a particular popup?** The thresholds for all notices can be adjusted in `BepInEx/config/Polaris/_polaris_diagnostics.cfg`, and you can set `ToastEnabled` under `[Severity]` to `false` to turn off in-game popups (error reports are still written).

## For developers

See [doc/DEVELOPING.md](doc/DEVELOPING.md) for the build, CI, release process, and directory structure.

Polaris used to ship a full game API library, which has since been split out into a separate project; the complete old code is preserved in the `legacy` branch and the `pre-slim-v2.0.0` tag.

## License

LGPL-2.1, see [LICENSE.txt](LICENSE.txt). For third-party components, see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
