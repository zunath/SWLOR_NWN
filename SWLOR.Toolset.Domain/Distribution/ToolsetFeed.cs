namespace SWLOR.Toolset.Domain.Distribution;

public sealed class ToolsetFeed
{
    public const string DefaultUrl = "https://github.com/zunath/SWLOR_NWN/releases/latest/download/toolset-feed.json";
    public int SchemaVersion { get; set; } = 1;
    public string ContentRevision { get; set; } = string.Empty;
    public string AssetRevision { get; set; } = string.Empty;
    public DownloadArchive Workspace { get; set; } = new();
    public DownloadArchive Assets { get; set; } = new();
}
