namespace MyLocalStreamer.Services;

public sealed class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>Folder scanned for videos. Relative paths are resolved against the content root.</summary>
    public string LibraryPath { get; set; } = "LocalMedia";

    public string SelectedFolderName { get; set; } = "Selected";

    public string ScreenshotsFolderName { get; set; } = "Screenshots";

    /// <summary>Seconds of playback after which a video counts as "watched".</summary>
    public int WatchedAfterSeconds { get; set; } = 15;

    public string[] Extensions { get; set; } = [".mp4", ".webm", ".mkv", ".mov", ".m4v"];
}
