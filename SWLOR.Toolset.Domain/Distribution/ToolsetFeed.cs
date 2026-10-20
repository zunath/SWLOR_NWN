using System.Text.Json;
using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Domain.Distribution;

public sealed class ToolsetFeed
{
    public const string DefaultUrl = "https://github.com/zunath/SWLOR_NWN/releases/latest/download/toolset-feed.json";
    public const string ReleaseFileName = "toolset-release.json";
    public int SchemaVersion { get; set; } = 1;
    public string ContentRevision { get; set; } = string.Empty;
    public string AssetRevision { get; set; } = string.Empty;
    public DownloadArchive Workspace { get; set; } = new();
    public DownloadArchive Assets { get; set; } = new();

    public static string UrlForVersion(string version)
    {
        if (!Regex.IsMatch(version ?? "", @"^\d+\.\d+\.\d+(-[A-Za-z0-9.-]+)?$"))
            throw new InvalidDataException("The installed toolset release version is invalid.");
        return $"https://github.com/zunath/SWLOR_NWN/releases/download/toolset-v{version}/toolset-feed.json";
    }

    public static string? ReadInstalledVersion(string applicationDirectory)
    {
        var path = Path.Combine(applicationDirectory, ReleaseFileName);
        if (!File.Exists(path)) return null; // Source builds use the latest published content.
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var version = document.RootElement.GetProperty("Version").GetString();
        _ = UrlForVersion(version!);
        return version;
    }
}
