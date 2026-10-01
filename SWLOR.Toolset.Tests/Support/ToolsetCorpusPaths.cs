namespace SWLOR.Toolset.Tests.Support;

/// <summary>Selects an explicit local corpus for isolated-worktree qualification.</summary>
internal static class ToolsetCorpusPaths
{
    private const string RepositoryVariable = "SWLOR_TEST_REPOSITORY_ROOT";
    private const string HaksVariable = "SWLOR_TEST_HAKS_ROOT";

    public static string? RepositoryRoot => Select(RepositoryVariable);
    public static string? HaksRoot => Select(HaksVariable) ?? (RepositoryRoot is { } root ? Path.Combine(root, "SWLOR_Haks") : null);

    private static string? Select(string variable)
    {
        var selected = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(selected)) return null;
        var path = Path.GetFullPath(selected);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"The explicit {variable} corpus does not exist: {path}");
        return path;
    }
}
