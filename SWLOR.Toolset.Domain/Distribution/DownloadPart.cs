namespace SWLOR.Toolset.Domain.Distribution;

public sealed record DownloadPart(string Url, string Sha256, long Length);
