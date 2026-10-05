## Polaris

A mod management and diagnostics tool for *Alice in Cradle*.

Once installed, a “Polaris” mod management page will appear in the title menu. It provides third-party mod management with a near-native UI
as well as basic game diagnostics: it identifies which mod is causing an issue and logs the information.

[English](README.en.md)

## What It Does

**One-click installation.** Double-click the installer, select the game, and click “Install.” BepInEx and Polaris will be installed together—no need to manually copy files.

**Mod management.** On the “Polaris” page of the game’s title screen, check the boxes to enable or disable mods; changes take effect after restarting the game.

**Tiered error diagnosis.** Minor errors are logged in the background; if an error is severe enough to affect gameplay, the game will exit:

| Level | When | What you’ll see |
|---|---|---|
| Minor | First error from a mod | A small提示 in the top-right corner; click to view details |
| Persistent | The same error occurs repeatedly, or the same mod produces multiple types of errors | A dialog box allowing you to disable the mod with one click (changes take effect after restarting) |
| Severe | Errors persist for too long, multiple mods fail simultaneously, the game freezes for an extended period, Polaris’s own patch fails, or memory is exhausted | A full-screen alert; the game automatically exits after a countdown |

Mods that have caused persistent or severe issues will be flagged on the Mod Management page; if the mod’s files are updated later, the flag will change to a “Possibly Fixed” notification.

**Crash Window.** When the game exits unexpectedly, a window pops up listing the possible causes: exit code, system-logged faulty modules, errors prior to the crash, and the most likely culprit mod.

**Settings. ** In the game’s “Settings” menu (last tab), you can adjust: the version line on the title screen, the error notification page for the previous game, in-game error prompts (on/off, minimum severity level, display duration, and which corner of the screen they appear in), automatic update checks, and whether to automatically exit the game in case of a critical error.

**Automatic Updates.** After the game launches, Polaris checks for new versions. If available, the game will prompt you; clicking “Update” will initiate the download; The files will be automatically replaced after you exit the game, without interrupting your current game session.

## Installation

Requires Windows and a copy of *Alice in Cradle*.

1. Go to [Releases](https://github.com/AAAA9731/Polaris/releases) to download `PolarisInstaller.exe` (a single file; no separate .NET installation required).
2. Run it, click “Browse…”, and select the game’s `AliceInCradle.exe`. You can also drag `AliceInCradle.exe` or the game folder directly into the window; if you run the installer from within the game folder, it will be automatically detected.
3. Click “Install.”

The installer will first back up the original files to be replaced; an already installed BepInEx will not be modified by default.

## Updates

Generally, no action is required: simply click “Update” when prompted in the game. You can also rerun the latest version of the installer at any time.

## Uninstall

Run the installer, select the game directory, and click “Uninstall.” Only Polaris’s own files will be deleted, and the backed-up original files will be restored; your other mods will remain untouched. If you’re certain you no longer need BepInEx, you can check the box labeled “Uninstall BepInEx as well.”

## Frequently Asked Questions

**Windows says “Your computer is protected”?** The installer is not code-signed, so Windows may display a SmartScreen warning. Click “More info” and then select “Run anyway.” The checksum is included in every release (`PolarisInstaller.exe.sha256`); you can verify it yourself.

**Where are the reports and logs?** Error reports are located in the game directory at `BepInEx/Polaris/reports/`. There is one file per game session, and the system retains up to the 20 most recent reports; Bep

Translated with DeepL.com (free version)