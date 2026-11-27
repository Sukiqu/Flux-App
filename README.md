<p align="center">
  <img src="Assets/flux-symbol.png" alt="Flux logo" width="180">
</p>

<h1 align="center">Flux</h1>

<p align="center"><strong>Customizable Windows desktop flyouts.</strong><br>
Media, volume and keyboard lock notifications in one focused interface.</p>

<p align="center">Version 2.1.0 · Windows · Developed by <strong>sukiqu</strong></p>

---

Flux puts music controls, audio levels and keyboard lock indicators in
customizable desktop panels. Adjust the appearance, placement and timing to fit
your desktop.

## What you can do

- See the current song and album artwork, and control playback.
- Adjust your volume and see which audio device is in use.
- Check Caps Lock, Num Lock and Scroll Lock status.
- Choose a theme and use your Windows accent color or a custom color.
- Set each panel's screen, position, size, transparency and display duration.
- Preview changes and optionally start Flux when you sign in to Windows.

Music information and playback controls depend on what your player shares with
Windows. Some players may not provide every feature.

## Requirements

- Windows 10 version 1809 or later.
- .NET Framework 4.8.

## Install

Open the [Flux Releases page](https://github.com/Sukiqu/Flux-App/releases) and download
**Flux-2.1.0-windows.zip** from the release's **Assets** section.

1. Choose `Flux-2.1.0-windows.zip`. The **Source code** archives are not the application.
2. Right-click the ZIP and choose **Extract All**. Keep the extracted files together.
3. Open the extracted folder and run **Flux.exe**.

Flux is portable: there is no installer, and administrator access is not required.

## Get started

Open settings from the Flux icon in the Windows system tray, near the clock.
If the icon is hidden, expand the tray's hidden icons first.

- **Media panel:** choose how your music controls and track information appear.
- **Volume panel:** customize volume notifications and the device-name display.
- **Lock panel:** customize keyboard lock notifications.
- **Application:** manage the application's appearance and startup behavior.

Settings are saved automatically. Use the previews to check the result as you
adjust your panels. By default, Flux follows your Windows accent color.

Closing the settings window with **X** keeps Flux running in the system tray.
To stop it completely, choose **Exit application**.

## Update

1. Exit Flux completely.
2. Download and extract the new Windows package from the [Flux Releases page](https://github.com/Sukiqu/Flux-App/releases).
3. Replace the previous application files with the new ones, then run **Flux.exe**.

Your preferences are stored separately and are preserved when you update.

## Uninstall

1. If **Start with Windows** is enabled, turn it off in the application settings.
2. Choose **Exit application** to stop Flux.
3. Delete the folder containing the extracted application files.
4. To also remove saved preferences, press **Win + R**, enter
   `%LOCALAPPDATA%\AkisFlyout`, and delete that folder after Flux has exited.
   The folder keeps its original name for compatibility with earlier versions.

Keeping the preferences folder lets a future installation reuse your settings.

## Privacy

Flux works locally. It does not require an account, collect usage data or show
advertising. Your preferences stay on your computer. Read the [privacy
policy](PRIVACY.md) for details.

## More information

- [Changelog](CHANGELOG.md) — concise user-facing version summaries.
- [Security reporting](SECURITY.md)

Copyright © 2026 **sukiqu**. **All rights reserved.** See [LICENSE](LICENSE).
