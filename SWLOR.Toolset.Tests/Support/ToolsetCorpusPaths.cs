namespace SWLOR.Toolset.Tests.Support;

/// <summary>
/// Locates the SWLOR checkout and HAK corpus the tests read. An explicit environment variable
/// selects an isolated copy; otherwise the checkout containing the test assembly is used.
/// </summary>
internal static class ToolsetCorpusPaths
{
    private const string RepositoryVariable = "SWLOR_TEST_REPOSITORY_ROOT";
    private const string HaksVariable = "SWLOR_TEST_HAKS_ROOT";
    private const string SolutionFile = "SWLOR.Game.Server.sln";

    public static string? RepositoryRoot => Select(RepositoryVariable) ?? DiscoverRepositoryRoot();
    public static string? HaksRoot => Select(HaksVariable) ?? (RepositoryRoot is { } root ? Path.Combine(root, "SWLOR_Haks") : null);

    private static string? Select(string variable)
    {
        var selected = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(selected)) return null;
        var path = Path.GetFullPath(selected);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"The explicit {variable} corpus does not exist: {path}");
        return path;
    }

    private static string? DiscoverRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFile)))
                return current.FullName;
        }

        return null;
    }
}
