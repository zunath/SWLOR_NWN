namespace SWLOR.Toolset.Domain.Distribution;

public sealed record ContributionChange(string Path, string? BeforeHash, string? AfterHash);
