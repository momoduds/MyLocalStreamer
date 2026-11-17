# MyLocalStreamer

Local video streamer built with ASP.NET Core Blazor Web App (Interactive Server), .NET 10.

## Run
1. Open `MyLocalStreamer.sln` in Visual Studio 2026 (or VS 2022 with the .NET 10 SDK installed).
2. Press F5. On first start a `LocalMedia` folder is created next to the project file
   (`Selected/` and `Screenshots/` are created inside it). Drop videos into `LocalMedia`, then press **⟳ Rescan**.

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
