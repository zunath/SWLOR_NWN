namespace SWLOR.Toolset.Tests;

/// <summary>Frozen inputs for the legacy DLG importer/editor, never sources for live conversations.</summary>
internal static class LegacyConversationFixtures
{
    public static string DirectoryPath => Path.Combine(
        CorpusLocator.RepositoryRoot, "SWLOR.Toolset.Tests", "Fixtures", "LegacyConversations");

    public static string PathFor(string id) => id == "dmfi_universal"
        ? Path.Combine(CorpusLocator.ModuleDirectory, "dlg", id + ".dlg.json")
        : Path.Combine(DirectoryPath, id + ".dlg.json");

    public static IEnumerable<string> AllPaths() => Directory.EnumerateFiles(DirectoryPath, "*.dlg.json")
        .Append(PathFor("dmfi_universal"))
        .OrderBy(path => path, StringComparer.Ordinal);
}
