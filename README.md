# Strinova Replay Manager

A Windows desktop app for managing **Strinova** match replays. It keeps your in-game replay slots organized while storing the actual replay files in a separate library you can import, export, and remap between slots.

---

## What it does (plain English)

Strinova saves replays into a **Demos** folder on your PC. Each replay slot in the game corresponds to a `.replay` file in that folder.

This app helps you:

- **Keep replays safe** — When new replays appear, the app moves the real files into a hidden `.original` library folder and leaves lightweight shortcuts (symlinks) in the Demos folder so the game still finds them.
- **Swap which replay a slot plays** — Pick an in-game slot and point it at any replay in your library, without copying large files around.
- **Import replays from elsewhere** — Add `.replay` files you received from friends or backups into your library, then map them to a slot.
- **Share replays** — Export a replay to another folder, or copy it to the clipboard for Discord, email, etc.
- **Clean up** — Delete an in-game slot (the library copy stays), or permanently remove a replay from your library.
- **Download from server** — Re-fetch a recent replay from Strinova’s CDN into your Originals library (when still online).
- **Recover replay** — Restore a replay you recently deleted from the library, if it is still on the server.

On first launch, the app checks whether Windows allows it to create symlinks. If not, you can enable **Developer Mode**, restart as administrator, or continue in **view-only** mode (browse and export, but no remapping or import).

---

## Features

| Feature | Full mode | View-only |
|--------|-----------|-----------|
| Browse mapped slots and originals library | Yes | Yes |
| Auto-organize new replays (ingest) | Yes | No |
| Live refresh when Demos folder changes | Yes | No |
| Remap slot → different original | Yes | No |
| Import `.replay` into library | Yes | No |
| Download from server / Recover replay | Yes | No |
| Save as / copy replay | Yes | Yes |
| Delete slot or library item | Yes | Yes |

Additional behavior:

- Parses replay filenames to show match ID, game version, user ID, and timestamp when available.
- Warns if you remap while Strinova is running (`Strinova-Win64-Shipping`).
- Marks imported files so they are not auto-linked back into Demos until you remap them manually.
- Optional “always request administrator on startup” if symlinks require elevation on your system.
- CDN download uses local vs server filename mapping and HEAD checks; see [docs/replay-cdn-retention.md](docs/replay-cdn-retention.md) for retention research and tuning.

---

## How it works

```
%LocalAppData%\Strinova\Saved\Demos\
├── SomeSlot_replay.replay          ← symlink (what the game reads)
├── AnotherSlot_replay.replay       ← symlink
└── .original\                      ← your replay library
    ├── SomeSlot_replay.replay      ← actual file (moved here by ingest)
    └── imported_match.replay       ← imported or duplicate originals
```

1. **Ingest** — On refresh (and when the Demos folder changes in full mode), new plain `.replay` files are moved into `.original` and replaced with symlinks pointing back to the library copy.
2. **Remap** — Replaces a slot’s symlink so it points at a different file in `.original`.
3. **Import** — Copies an external `.replay` into `.original` and tags it as import-only (no automatic symlink).
4. **Catalog** — Lists symlinks as “mapped replays” and `.original` contents as the “originals library,” sorted newest first.

Settings are stored under `HKCU\Software\StrinovaReplayManager`. Import markers use an NTFS alternate data stream on each file.

---

## Technical overview

### Stack

| Layer | Choice |
|-------|--------|
| UI | WinUI 3 (Windows App SDK **2.0.1**) |
| Runtime | **.NET 10** (`net10.0-windows10.0.19041.0`) |
| Pattern | MVVM ([CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) **8.4.2**) |
| Packaging | Self-contained publish; optional Inno Setup installer |

### Architecture

- **`AppServices`** — Composition root wiring path, filesystem, ingest, catalog, remap, import/export, clipboard, file watcher, and view models.
- **`DemosPathService`** — Resolves `%LocalAppData%\Strinova\Saved\Demos` and `.original` paths.
- **`ReplayIngestService`** — Moves new replays into the library and creates symlinks.
- **`ReplayRemapService`** — Updates symlink targets for a slot.
- **`ReplayCatalogService`** — Builds mapped/originals lists; parses filenames via `ReplayFilenameParser`.
- **`SymlinkCapabilityService`** — Probes symlink permission, opens Developer settings, optional UAC relaunch.
- **`ReplayFileWatcher`** — Debounced `FileSystemWatcher` on the Demos folder (full mode only).

UI pages: `SetupPage` → `MainPage` → `RemapPage` (navigation frame in `MainWindow`).

Replay filenames follow Strinova’s convention, roughly:

`{userId}_{gameVersion}_{matchId}_{unixTime}_{randomSeq}.replay`

(with optional variations when the timestamp segment is omitted).

### Project layout

```
StrinovaReplayManager/
├── Helpers/          File I/O, parsing, display, Win32 interop
├── Models/           ReplayEntry, MappedReplay, AppMode
├── Services/         Business logic
├── ViewModels/       MVVM bindings
├── Strings/          Localized UI strings (en-us, zh-cn, ja-jp)
├── Styles/           WinUI resource dictionaries
├── Setup/            Inno Setup script + build-setup.ps1
├── Assets/           Icons and logos
└── Properties/PublishProfiles/   win-x64, win-x86, win-arm64
```

---

## Requirements

### To run

- **Windows 10** version 19041 or later (Windows 11 supported)
- **Windows App Runtime** 2.x (bundled by the setup installer, or install separately)
- **Symlink permission** for full mode:
  - Windows **Developer Mode**, or
  - Run as **administrator** (optional “always elevate” setting)

### To build from source

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows SDK / WinUI workload (included with Visual Studio 2022 **“Windows application development”** workload, or build via CLI on Windows)
- **Inno Setup 6** — only if you want to build the `.exe` installer (`Setup/build-setup.ps1`)

---

## How to build

All commands assume the repository root (`StrinovaReplayManager/`).

### Debug build and run

```powershell
dotnet build
dotnet run
```

### Command-line: display language

Force the UI language for this session (unpackaged builds do not persist the choice). Set **before** the window opens — pass arguments to the app, not to `dotnet` itself.

| Switch | Value |
|--------|--------|
| `--lang`, `--language`, `-lang`, `/lang`, `/language` | BCP-47 tag or short alias |

**Supported languages:** `en-us`, `zh-cn`, `ja-jp` (resource folders under `Strings/`).

**Short aliases:** `en` → `en-us`, `zh` → `zh-cn`, `ja` → `ja-jp`.

Examples:

```powershell
# Published executable
.\StrinovaReplayManager.exe --lang zh-cn

# dotnet run (note the -- separator)
dotnet run -- --lang zh

# Inline value
dotnet run -- --lang=ja-jp
```

Visual Studio / Cursor: add `commandLineArgs` to the unpackaged profile in `Properties/launchSettings.json`, e.g. `"commandLineArgs": "--lang zh-cn"`.

If Windows is already set to a language the app supports, omit the flag to use the system preference.

### Release publish (self-contained folder)

```powershell
dotnet publish StrinovaReplayManager.csproj -c Release -r win-x64 --self-contained true
```

Output:

```
bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\StrinovaReplayManager.exe
```

Other runtimes: replace `win-x64` with `win-x86` or `win-arm64` (publish profiles exist under `Properties/PublishProfiles/`).

The project includes a post-publish MSBuild target (`CopyWinUIResourcesToPublish`) that copies XAML binary resources (`.xbf`), the `.pri` manifest, and `Assets/` into the publish folder — required for WinUI apps published with `dotnet publish`.

### Windows installer (optional)

```powershell
.\Setup\build-setup.ps1
```

This script:

1. Publishes a Release `win-x64` build
2. Downloads the Windows App Runtime installer into `Setup/Redist/` (if missing)
3. Compiles `Setup/StrinovaReplayManager.iss` with Inno Setup

The setup executable is written to `Setup/Output/Strinova Replay Manager-Setup.exe`.

---

## First run

1. Launch **Strinova Replay Manager**.
2. If symlink creation is blocked, the **Setup** screen offers:
   - Open **Developer settings** and enable Developer Mode
   - **Restart as administrator**
   - **Retry** after fixing permissions
   - **Continue view-only** to browse/export without remapping
3. In **full mode**, play Strinova as usual — new replays are organized automatically.
4. To use an imported replay in-game: **Import** it, then select a mapped slot and **Remap** it to the imported file.

Close Strinova before remapping when possible; the app warns if the game process is still running.

---

## Data locations

| What | Path |
|------|------|
| Game demos folder | `%LocalAppData%\Strinova\Saved\Demos` |
| Originals library | `%LocalAppData%\Strinova\Saved\Demos\.original` |
| App settings | `HKCU\Software\StrinovaReplayManager` |

---

## License

This project is **free software** under the [GNU General Public License v3.0 or later](LICENSE) (GPL-3.0-or-later).

If you **distribute** this program (or a modified version), you must:

- Provide the **complete corresponding source code** under the same license.
- **License your changes under GPLv3** so recipients keep the same freedoms.
- Preserve copyright and license notices.

You may charge no more than the cost of physically conveying a copy, or offer it gratis. See the license for full terms.

## Disclaimer

**Strinova Replay Manager** is an independent, community-maintained tool for managing local Strinova replay files on Windows. It is **not** affiliated with, endorsed by, sponsored by, or approved by **iDreamSky** (publisher and developer of *Strinova*) or any official Strinova partners. *Strinova* and related names, logos, and assets are trademarks of their respective owners.

This app is provided **as is**, without warranty. You use it at your own risk. The authors are not responsible for data loss, game issues, or violations of game terms of service.

**Replay availability:** Only **recent** replays may still exist on Strinova’s download servers or through the game client. Older matches often return 404 from the CDN. If you delete a replay from the **Originals library** and it is no longer on the server, recovery may be impossible — keep backups of matches you care about.
