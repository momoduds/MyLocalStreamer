namespace MyLocalStreamer.Services;

public sealed class VideoItem
{
    /// <summary>Stable id derived from the file name (survives rescans and restarts).</summary>
    public required string Id { get; init; }
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public long FileSize { get; init; }
    public DateTime DateAdded { get; init; }
    public bool IsWatched { get; set; }

    /// <summary>Browser-facing URL. The server resolves the id to a path; clients never send paths.</summary>
    public string StreamUrl => $"/api/stream/{Id}";
}
