# My Local Streamer - MomoduDS

Local video streamer built with ASP.NET Core Blazor Web App (Interactive Server), .NET 10.

What The Application Is
​It is a self-hosted, YouTube-style media streaming web application built entirely using .NET (Interactive Server render mode). It is specifically designed to run locally on your machine, instantly scanning a designated local folder (LocalMedia) to transform your video collection into a polished, modern web streaming center without needing cloud hosting or third-party media servers.
​Key Features & Capabilities
​1. YouTube-Style Dashboard & Responsive UI
​Thumbnail Grid Sidebar: Displays all discovered videos (.mp4, .mkv, .webm, .mov) in a clean, scrollable sidebar complete with file sizes and title metadata.
​Inline Hover Previews: Hovering your mouse over any video thumbnail triggers a muted, looping mini-video preview card, letting you preview content instantly before clicking.
​Theater Mode Toggle: Expand the video viewer to occupy a larger screen area (Theater Mode) or toggle full screen instantly.
​Dual Theme Support: Includes a built-in Dark / Light Mode toggle that remembers your preference using browser storage (localStorage).
​2. Advanced Custom Video Player Controls
​Seamless HTTP Range Streaming: Powered by a custom backend API controller (StreamController) that supports HTTP Range requests. This allows you to scrub, drag, or skip through multi-gigabyte video timelines instantly without lagging or high memory usage.
​Global Keyboard Shortcuts:
​Spacebar: Play / Pause (smart-filtered so typing in search boxes doesn't trigger it).
​F: Toggle Fullscreen.
​Right / Left Arrows: Skip forward or backward by 5 seconds.
​3. Smart Watch History & Filtering
​Watched Stickers: Automatically tracks your viewing progress (stamping a video as "Watched" once it plays past 15 seconds or finishes). Watched items receive a clean ✓ WATCHED badge and dimmed overlay.
​Library Filters: Filter your sidebar instantly using YouTube-style pills: All, Unwatched, or Watched.
​Instant Search Bar: Filter videos in real-time by typing filenames.
​4. The "Select & Next" Quick-Action Workflow
​A prominent ⭐ Select & Next button sits right next to the player for sorting exceptional videos.
​Clicking it instantly triggers a safe background task that:
​1. Picks and loads a random unwatched video into the player first (preventing file locks).
​2. Safely relocates the old video file into a special subfolder called LocalMedia/Selected.
​5. Server-Side Screenshot Capture
​Clicking the 📷 Screenshot button captures the exact frame currently playing on screen, converts it via HTML5 canvas, and automatically saves a .png file directly into your server's LocalMedia/Screenshots folder.
​Displays a sleek, floating 1-second toast notification confirming the save action.
​How It Works Under the Hood
​1. Single-Project Server Architecture: Built on a straightforward single-project .NET setup using interactive server rendering, giving the application direct, high-speed access to your local machine's file system via standard C# libraries (System.IO).
​2. Service Layer (VideoService.cs):
​Scans your LocalMedia directory upon startup.
​Maintains persistent watch history records in a local JSON file (watch-history.json).
​Handles asynchronous file moves and random sequence selection.
​3. JS Interop Bridge (app.js): Connects browser-side events (keyboard shortcuts, video dimensions, canvas frame extractions) directly to your C# Blazor component methods using asynchronous invocation (JSInvokable).

## Run
1. Open `MyLocalStreamer.sln` in Visual Studio 2026 (or VS 2022 with the .NET 10 SDK installed).
2. Press F5. On first start, a `LocalMedia` folder is created in the project folder (if not already there) and other sub folders
   (`Selected/` and `Screenshots/`) are created inside it. Drop videos into `LocalMedia`, then press **⟳ Rescan**.

## Configuration (`appsettings.json` → `Media`)
| Key | Default | Meaning |
|---|---|---|
| `LibraryPath` | `LocalMedia` | Folder to scan. Relative to the project, or an absolute path such as `D:\Videos`. |
| `SelectedFolderName` | `Selected` | Sub-folder that "Select & Next" moves files into. |
| `ScreenshotsFolderName` | `Screenshots` | Sub-folder for PNG screenshots. |
| `WatchedAfterSeconds` | `15` | Playback time after which a video counts as watched. |

## Keyboard shortcuts
Space play/pause · ←/→ seek 5s · F fullscreen · M mute · N random next · T theater mode · S screenshot

## Watching from other devices
Use the **LAN (other devices)** launch profile (listens on `http://0.0.0.0:5080`) and allow port 5080 in Windows Firewall.
There is no authentication - only use it on a network you trust.

## Notes
- Browsers play MP4 (H.264/AAC) and WebM reliably. `.mkv` / `.mov` only play if the codecs inside are browser-supported.
