using Microsoft.Net.Http.Headers;
using MyLocalStreamer.Services;

namespace MyLocalStreamer.Endpoints;

public static class MediaEndpoints
{
    private const long MaxScreenshotBytes = 50 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp4"] = "video/mp4",
        [".m4v"] = "video/mp4",
        [".webm"] = "video/webm",
        [".mkv"] = "video/x-matroska",
        [".mov"] = "video/quicktime",
    };

    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        // GET /api/stream/{id}  - clients reference videos by id, never by file path.
        api.MapGet("/stream/{id}", StreamVideo);

        // POST /api/screenshots/{id}  - raw PNG body (too big for the SignalR circuit).
        api.MapPost("/screenshots/{id}", SaveScreenshot).DisableAntiforgery();

        return app;
    }

    private static IResult StreamVideo(string id, IVideoLibrary library)
    {
        var video = library.Find(id);
        if (video is null || !File.Exists(video.FilePath))
            return Results.NotFound();

        var info = new FileInfo(video.FilePath);
        var contentType = ContentTypes.GetValueOrDefault(info.Extension, "application/octet-stream");

        // FileShare.Delete lets "Select & Next" move the file even while it is still being streamed.
        var stream = new FileStream(
            info.FullName, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var lastModified = new DateTimeOffset(info.LastWriteTimeUtc);
        var etag = new EntityTagHeaderValue($"\"{info.Length:x}-{info.LastWriteTimeUtc.Ticks:x}\"");

        return Results.File(stream, contentType,
            lastModified: lastModified, entityTag: etag, enableRangeProcessing: true);
    }

    private static async Task<IResult> SaveScreenshot(
        string id, HttpRequest request, IVideoLibrary library, CancellationToken ct)
    {
        if (request.ContentLength is > MaxScreenshotBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        var fileName = await library.SaveScreenshotAsync(id, request.Body, ct);
        return fileName is null ? Results.NotFound() : Results.Ok(new { fileName });
    }
}
