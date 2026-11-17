namespace MyLocalStreamer.Services;

public interface IVideoLibrary
{
    /// <summary>Returns a snapshot of the library. Pass <c>rescan: true</c> to re-read the folder first.</summary>
    IReadOnlyList<VideoItem> GetVideos(bool rescan = false);

    VideoItem? Find(string id);

    /// <summary>Random pick, preferring unwatched videos and skipping <paramref name="excludeId"/>.</summary>
    VideoItem? GetRandom(string? excludeId = null);

    Task MarkWatchedAsync(string id);

    /// <summary>Moves the file into the "Selected" folder. Returns false if it could not be moved.</summary>
    Task<bool> MoveToSelectedAsync(string id);

    /// <summary>Saves a PNG for the given video. Returns the new file name, or null if the video is unknown.</summary>
    Task<string?> SaveScreenshotAsync(string videoId, Stream image, CancellationToken cancellationToken = default);
}
