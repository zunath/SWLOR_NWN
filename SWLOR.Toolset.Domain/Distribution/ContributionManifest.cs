namespace SWLOR.Toolset.Domain.Distribution;

public sealed class ContributionManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string ContentRevision { get; set; } = string.Empty;
    public string AssetRevision { get; set; } = string.Empty;
    public List<ContributionChange> Changes { get; set; } = new();
}
