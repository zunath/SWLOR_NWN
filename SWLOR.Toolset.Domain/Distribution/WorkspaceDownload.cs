using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace SWLOR.Toolset.Domain.Distribution;

/// <summary>Downloads checked snapshots into new directories and never updates an editable workspace in place.</summary>
public sealed class WorkspaceDownload
{
    private readonly HttpClient _client;
    private readonly string _feedUrl;
    public WorkspaceDownload(HttpClient client, string? installedVersion = null)
    {
        _client = client;
        _feedUrl = installedVersion == null ? ToolsetFeed.DefaultUrl : ToolsetFeed.UrlForVersion(installedVersion);
    }
    public static string AssetCacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SWLOR.Toolset", "Assets");

    public async Task<ToolsetFeed> ReadFeedAsync(CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync(_feedUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 2 * 1024 * 1024)
            throw new InvalidDataException("The download feed is too large.");
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > 2 * 1024 * 1024) throw new InvalidDataException("The download feed is too large.");
        var feed = JsonSerializer.Deserialize<ToolsetFeed>(bytes) ?? throw new InvalidDataException("Empty download feed.");
        if (feed.SchemaVersion != 1 || !ContentPaths.IsRevision(feed.ContentRevision) || !ContentPaths.IsRevision(feed.AssetRevision))
            throw new InvalidDataException("Unsupported download feed.");
        return feed;
    }

    public async Task CreateAsync(ToolsetFeed feed, string destination, string cacheRoot, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (feed.SchemaVersion != 1 || !ContentPaths.IsRevision(feed.ContentRevision) || !ContentPaths.IsRevision(feed.AssetRevision))
            throw new InvalidDataException("Unsupported download feed.");
        if (Directory.Exists(destination) || File.Exists(destination))
            throw new IOException("Choose a new workspace folder. Existing workspaces are never replaced.");
        var assets = Path.Combine(cacheRoot, feed.AssetRevision);
        Directory.CreateDirectory(cacheRoot);
        progress?.Report("Verifying shared SWLOR assets...");
        var cacheValid = await Task.Run(() => IsAssetCacheValid(assets, feed.AssetRevision), cancellationToken);
        if (!cacheValid)
        {
            progress?.Report("Downloading SWLOR assets. This first download can take a while...");
            await InstallAsync(feed.Assets, assets, true, feed, cancellationToken);
        }
        progress?.Report("Downloading the editable workspace...");
        await InstallAsync(feed.Workspace, destination, false, feed, cancellationToken);
        var workspace = WorkspaceManifest.Load(destination);
        if (workspace.ContentRevision != feed.ContentRevision || workspace.AssetRevision != feed.AssetRevision)
            throw new InvalidDataException("The workspace does not match the selected release.");
    }

    private async Task InstallAsync(DownloadArchive descriptor, string destination, bool assets, ToolsetFeed feed, CancellationToken token)
    {
        ValidateDescriptor(descriptor);
        var parent = Path.GetDirectoryName(Path.GetFullPath(destination))!;
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, ".swlor-download-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        var archivePath = Path.Combine(staging, "download.zip");
        var extracted = Path.Combine(staging, "content");
        try
        {
            await using (var output = File.Create(archivePath))
            {
                using var combined = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[1024 * 1024];
                foreach (var part in descriptor.Parts)
                {
                    using var response = await _client.GetAsync(part.Url, HttpCompletionOption.ResponseHeadersRead, token);
                    response.EnsureSuccessStatusCode();
                    await using var input = await response.Content.ReadAsStreamAsync(token);
                    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    long length = 0;
                    int read;
                    while ((read = await input.ReadAsync(buffer, token)) != 0)
                    {
                        length += read;
                        if (length > part.Length) throw new InvalidDataException("A download exceeds its declared size.");
                        hash.AppendData(buffer, 0, read);
                        combined.AppendData(buffer, 0, read);
                        await output.WriteAsync(buffer.AsMemory(0, read), token);
                    }
                    if (length != part.Length || !Convert.ToHexString(hash.GetHashAndReset()).Equals(part.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("A download failed its checksum. Retry the download.");
                }
                if (!Convert.ToHexString(combined.GetHashAndReset()).Equals(descriptor.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The complete download failed its checksum.");
            }
            Extract(archivePath, extracted, assets);
            if (assets)
            {
                AssetManifest.Load(extracted, feed.AssetRevision, verifyFiles: true);
            }
            else
            {
                var manifest = WorkspaceManifest.Load(extracted);
                if (manifest.ContentRevision != feed.ContentRevision || manifest.AssetRevision != feed.AssetRevision)
                    throw new InvalidDataException("The workspace does not match the selected release.");
                foreach (var (path, hash) in manifest.Baseline)
                    if (!ContentPaths.HashFile(ContentPaths.Resolve(extracted, path)).Equals(hash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException($"Workspace checksum failed: {path}");
                manifest.ValidateMetadata(extracted);
                var current = ContentPaths.Capture(extracted);
                if (current.Count != manifest.Baseline.Count) throw new InvalidDataException("The workspace baseline is incomplete.");
            }
            if (assets) PublishAssets(extracted, destination, feed.AssetRevision);
            else Directory.Move(extracted, destination);
        }
        finally
        {
            // Staging is a freshly generated, task-owned sibling, never the destination.
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    private static bool IsAssetCacheValid(string directory, string revision)
    {
        try
        {
            AssetManifest.Load(directory, revision, verifyFiles: true);
            return true;
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or JsonException)
        {
            return false;
        }
    }

    private static void PublishAssets(string extracted, string destination, string revision)
    {
        string? previous = null;
        try
        {
            if (Directory.Exists(destination))
            {
                if (IsAssetCacheValid(destination, revision)) return;
                previous = destination + ".corrupt-" + Guid.NewGuid().ToString("N");
                try { Directory.Move(destination, previous); }
                catch (IOException) when (!Directory.Exists(destination)) { previous = null; }
            }
            try { Directory.Move(extracted, destination); }
            catch (IOException) when (Directory.Exists(destination))
            {
                // Another installer may have published the same immutable revision first.
                AssetManifest.Load(destination, revision, verifyFiles: true);
            }
        }
        catch
        {
            if (previous != null && Directory.Exists(previous) && !Directory.Exists(destination))
                Directory.Move(previous, destination);
            throw;
        }
        finally
        {
            // Discard the displaced cache only after a complete replacement exists.
            if (previous != null && Directory.Exists(previous) && Directory.Exists(destination))
                Directory.Delete(previous, recursive: true);
        }
    }

    public static void ValidateDescriptor(DownloadArchive descriptor)
    {
        if (descriptor == null || !ContentPaths.IsHash(descriptor.Sha256) || descriptor.Parts == null || descriptor.Parts.Count is < 1 or > 100)
            throw new InvalidDataException("Invalid download description.");
        foreach (var part in descriptor.Parts)
            if (!ContentPaths.IsHash(part.Sha256) || part.Length is <= 0 or > 2L * 1024 * 1024 * 1024 ||
                !Uri.TryCreate(part.Url, UriKind.Absolute, out var url) || url.Scheme != "https" ||
                url.Host != "github.com" || !url.AbsolutePath.StartsWith("/zunath/SWLOR_NWN/releases/download/", StringComparison.Ordinal))
                throw new InvalidDataException("A download is not a supported SWLOR release asset.");
    }

    private static void Extract(string archive, string destination, bool assets)
    {
        using var zip = ZipFile.OpenRead(archive);
        if (zip.Entries.Count > 100000 || zip.Entries.Sum(entry => entry.Length) > 32L * 1024 * 1024 * 1024)
            throw new InvalidDataException("The download archive is too large.");
        Directory.CreateDirectory(destination);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
        {
            if (!names.Add(entry.FullName)) throw new InvalidDataException("Duplicate archive entry.");
            if (assets)
            {
                if (entry.FullName != AssetManifest.FileName) AssetManifest.RequireAssetPath(entry.FullName);
            }
            else if (entry.FullName != WorkspaceManifest.FileName && entry.FullName != "Metadata/game-code.json")
                ContentPaths.RequireContentPath(entry.FullName);
            var path = ContentPaths.Resolve(destination, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var input = entry.Open();
            using var output = new FileStream(path, FileMode.CreateNew);
            input.CopyTo(output);
        }
    }
}
