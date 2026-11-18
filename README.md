# 🎬 My Local Streamer - Tubely

A self-hosted, YouTube-style video streamer for your local media collection. Point it at a folder, press F5, and browse your videos in a modern web UI, with no cloud hosting or third-party media server.

Built with **ASP.NET Core Blazor Web App (Interactive Server)** on **.NET 10**.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Blazor](https://img.shields.io/badge/Blazor-Interactive%20Server-5C2D91)
![Platform](https://img.shields.io/badge/runs-locally-brightgreen)

---

## ✨ Features

### Dashboard and UI
- **Thumbnail sidebar**: lists every discovered video (`.mp4`, `.mkv`, `.webm`, `.mov`) with title and file size.
- **Hover previews**: hover a thumbnail to see a muted, looping mini-preview.
- **Theater mode** and **fullscreen** toggles.
- **Dark / Light theme**, remembered in browser `localStorage`.

### Player
- **HTTP Range streaming** via a custom `StreamController`, so you can scrub through multi-gigabyte files instantly with low memory use.
- **Keyboard shortcuts** (see [below](#-keyboard-shortcuts)), smart-filtered so typing in the search box never triggers them.

### Watch history and filtering
- **Watched badge**: a video is marked `✓ WATCHED` (and dimmed) once it plays past a configurable threshold (default 15 s) or finishes.
- **Filter pills**: All, Unwatched, Watched.
- **Instant search**: filter by filename as you type.
- History is saved to a local `watch-history.json`.

### ⭐ Select & Next
A one-click workflow for curating your favourites. It:
1. Loads a random unwatched video into the player first (avoids file locks).
2. Moves the previous video into `LocalMedia/Selected`.

### 📷 Screenshot capture
Grabs the current frame through an HTML5 canvas and saves it as a `.png` in `LocalMedia/Screenshots`, with a brief toast confirming the save.

---

## 🚀 Getting Started

**Prerequisites:** Visual Studio 2026 (or VS 2022 with the .NET 10 SDK).

1. Open `MyLocalStreamer.sln`.
2. Press **F5**.
3. On first run, a `LocalMedia` folder is created in the project folder, along with `Selected/` and `Screenshots/` inside it.
4. Drop your videos into `LocalMedia` and click **⟳ Rescan**.

---

## ⚙️ Configuration

Settings live in `appsettings.json` under the `Media` section:

| Key | Default | Description |
|---|---|---|
| `LibraryPath` | `LocalMedia` | Folder to scan. Relative to the project, or absolute (e.g. `D:\Videos`). |
| `SelectedFolderName` | `Selected` | Sub-folder that **Select & Next** moves files into. |
| `ScreenshotsFolderName` | `Screenshots` | Sub-folder where PNG screenshots are saved. |
| `WatchedAfterSeconds` | `15` | Playback time after which a video counts as watched. |

Example:

```json
{
  "Media": {
    "LibraryPath": "D:\\Videos",
    "SelectedFolderName": "Selected",
    "ScreenshotsFolderName": "Screenshots",
    "WatchedAfterSeconds": 15
  }
}
```

---

## ⌨️ Keyboard Shortcuts

| Key | Action |
|---|---|
| `Space` | Play / Pause |
| `←` / `→` | Seek back / forward 5 s |
| `F` | Fullscreen |
| `M` | Mute |
| `N` | Random next video |
| `T` | Theater mode |
| `S` | Screenshot |

---

## 📱 Watching From Other Devices

1. Launch with the **LAN (other devices)** profile, which listens on `http://0.0.0.0:5080`.
2. Allow port **5080** through Windows Firewall.
3. Open `http://<your-pc-ip>:5080` from another device on the same network.

> ⚠️ **There is no authentication.** Only use this on a network you trust.

---

## 🧱 How It Works

- **Single-project architecture**: Blazor Interactive Server gives components direct access to the file system through `System.IO`.
- **`VideoService.cs`** scans the library on startup, persists watch history to `watch-history.json`, and handles async file moves and random selection.
- **`StreamController`** serves video files with HTTP Range support.
- **`app.js`** is the JS interop bridge. It forwards keyboard shortcuts, video dimensions and canvas frame captures to Blazor components through `[JSInvokable]` methods.

---

## 📝 Notes

- Browsers reliably play **MP4 (H.264/AAC)** and **WebM**. `.mkv` and `.mov` files only play if their internal codecs are browser-supported.
- Moved files and screenshots stay on your machine; nothing is uploaded anywhere.
