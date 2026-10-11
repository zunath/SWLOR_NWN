namespace SWLOR.Toolset.Services;

/// <summary>The completed delete and any transaction backups that could not be tidied.</summary>
public readonly record struct ModuleResourceDeletionResult(
    IReadOnlyList<string> DeletedPaths,
    IReadOnlyList<string> CleanupWarnings);
