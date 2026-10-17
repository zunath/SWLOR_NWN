namespace SWLOR.ContributorSetup;

internal sealed record NativeToolReceipt(string Version, string Source, string ArchiveSha256, Dictionary<string, string> Files);
