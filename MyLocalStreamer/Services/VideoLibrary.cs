using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MyLocalStreamer.Services;

public sealed class VideoLibrary : IVideoLibrary
{
    private sealed record Snapshot(IReadOnlyList<VideoItem> List, Dictionary<string, VideoItem> ById);

    private readonly object _gate = new();
    private readonly ILogger<VideoLibrary> _logger;
    private readonly HashSet<string> _extensions;
    private readonly HashSet<string> _watched = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _libraryPath;
    private readonly string _selectedPath;
    private readonly string _screenshotsPath;
    private readonly string _historyPath;

    private volatile Snapshot _snapshot = new([], new());

    public VideoLibrary(IWebHostEnvironment env, IOptions<MediaOptions> options, ILogger<VideoLibrary> logger)
    {
        _logger = logger;
        var o = options.Value;

        _libraryPath = Path.GetFullPath(o.LibraryPath, env.ContentRootPath);
        _selectedPath = Path.Combine(_libraryPath, o.SelectedFolderName);
        _screenshotsPath = Path.Combine(_libraryPath, o.ScreenshotsFolderName);
        _historyPath = Path.Combine(_libraryPath, "watch-history.json");
        _extensions = new HashSet<string>(o.Extensions, StringComparer.OrdinalIgnoreCase);

        Directory.CreateDirectory(_libraryPath);
        Directory.CreateDirectory(_selectedPath);
        Directory.CreateDirectory(_screenshotsPath);

        LoadHistory();
        Rescan();
    }

    public IReadOnlyList<VideoItem> GetVideos(bool rescan = false)
    {
        if (rescan) Rescan();
        return _snapshot.List;
    }

    public VideoItem? Find(string id) =>
        _snapshot.ById.TryGetValue(id, out var item) ? item : null;

    public VideoItem? GetRandom(string? excludeId = null)
    {
        var all = _snapshot.List;
        var pool = all.Where(v => v.Id != excludeId && !v.IsWatched).ToList();
        if (pool.Count == 0)
            pool = all.Where(v => v.Id != excludeId).ToList();

        return pool.Count == 0 ? null : pool[Random.Shared.Next(pool.Count)];
    }

    public Task MarkWatchedAsync(string id)
    {
        var item = Find(id);
        if (item is null) return Task.CompletedTask;

        lock (_gate)
        {
            item.IsWatched = true;
            if (_watched.Add(item.FileName))
                SaveHistoryLocked();
        }
        return Task.CompletedTask;
    }

    public async Task<bool> MoveToSelectedAsync(string id)
    {
        var item = Find(id);
        if (item is null || !File.Exists(item.FilePath)) return false;

        var destination = Path.Combine(_selectedPath, item.FileName);
        if (File.Exists(destination))
        {
            var name = Path.GetFileNameWithoutExtension(item.FileName);
            var ext = Path.GetExtension(item.FileName);
            destination = Path.Combine(_selectedPath, $"{name}_{Guid.NewGuid().ToString("N")[..6]}{ext}");
        }

        // The browser may still hold the file open for a moment, so retry briefly.
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(item.FilePath, destination);
                break;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                await Task.Delay(200);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not move {File} to {Destination}", item.FilePath, destination);
                return false;
            }
        }

        lock (_gate)
        {
            Publish(_snapshot.List.Where(v => v.Id != id).ToList());
        }
        return true;
    }

    public async Task<string?> SaveScreenshotAsync(string videoId, Stream image, CancellationToken cancellationToken = default)
    {
        var item = Find(videoId);
        if (item is null) return null;

        var invalid = Path.GetInvalidFileNameChars();
        var clean = string.Concat(Path.GetFileNameWithoutExtension(item.FileName).Where(c => !invalid.Contains(c)));
        var fileName = $"{clean}_screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var fullPath = Path.Combine(_screenshotsPath, fileName);

        await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await image.CopyToAsync(fs, cancellationToken);
        return fileName;
    }

    // ---------------------------------------------------------------- internals

    private void Rescan()
    {
        lock (_gate)
        {
            var items = new DirectoryInfo(_libraryPath)
                .EnumerateFiles()
                .Where(f => _extensions.Contains(f.Extension))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Select(f => new VideoItem
                {
                    Id = MakeId(f.Name),
                    FileName = f.Name,
                    FilePath = f.FullName,
                    FileSize = f.Length,
                    DateAdded = f.CreationTime,
                    IsWatched = _watched.Contains(f.Name)
                })
                .ToList();

            Publish(items);
        }
    }

    private void Publish(List<VideoItem> items) =>
        _snapshot = new Snapshot(items, items.ToDictionary(v => v.Id));

    private static string MakeId(string fileName) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fileName.ToLowerInvariant())))[..16];

    private void LoadHistory()
    {
        if (!File.Exists(_historyPath)) return;
        try
        {
            var names = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_historyPath));
            if (names is not null)
                foreach (var n in names) _watched.Add(n);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ignoring unreadable watch history at {Path}", _historyPath);
        }
    }

    private void SaveHistoryLocked()
    {
        try
        {
            // Write to a temp file first so a crash can never leave a half-written history.
            var tmp = _historyPath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_watched.Order(StringComparer.OrdinalIgnoreCase)));
            File.Move(tmp, _historyPath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save watch history");
        }
    }
}
