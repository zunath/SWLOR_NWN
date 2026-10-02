namespace SWLOR.Toolset.Tests.Support;

/// <summary>Selects the shared source corresponding to the property-control package under test.</summary>
internal static class SharedToolsetSource
{
    private const string SourceVariable = "NWN_TOOLSET_TEST_SOURCE_ROOT";

    public static string ReadBehaviorFile(string file)
    {
        var configured = Environment.GetEnvironmentVariable(SourceVariable);
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException($"Set {SourceVariable} to the reviewed shared source for markup qualification.");
        var root = Path.GetFullPath(configured);
        var directory = Path.Combine(root, "src", "Nwn.Toolset.Avalonia", "Behaviors");
        var path = Path.GetFullPath(Path.Combine(directory, file));
        if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The shared source file must remain in its behavior directory.", nameof(file));
        return File.ReadAllText(path);
    }
}
