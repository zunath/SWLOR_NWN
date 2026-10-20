namespace SWLOR.Toolset.Domain.Distribution;

public sealed class DownloadArchive
{
    public string Sha256 { get; set; } = string.Empty;
    public List<DownloadPart> Parts { get; set; } = new();
}
