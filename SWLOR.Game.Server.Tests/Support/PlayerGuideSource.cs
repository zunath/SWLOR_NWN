namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The text of the player guide wherever it lives: <c>PlayerGuideViewModel.cs</c>, the guide topic types beside it and, once the
/// split of P5-16h moves the topics into data types, every file that holds the guide's code or text. A test that guards a rule by
/// reading the guide uses <see cref="Read"/>, so a <c>NotContain</c> check cannot pass because the text left one file.
/// </summary>
/// <remarks>
/// The files are every <c>.cs</c> file under <c>SWLOR.Game.Server/Feature/GuiDefinition</c> named <c>PlayerGuide*.cs</c> or
/// <c>*GuideTopics.cs</c>, plus <c>ArticleBlock.cs</c> and <c>QuestionAnswer.cs</c>, read in path order.
/// </remarks>
public static class PlayerGuideSource
{
    private static readonly Lazy<IReadOnlyList<string>> Files = new(Find);

    /// <summary>The full paths of the guide's files, in the order <see cref="Read"/> combines them.</summary>
    public static IReadOnlyList<string> Paths() => Files.Value;

    /// <summary>The combined text of every guide file, line endings as LF.</summary>
    public static string Read() =>
        string.Join("\n", Files.Value.Select(path => File.ReadAllText(path).Replace("\r\n", "\n")));

    private static IReadOnlyList<string> Find()
    {
        var root = FindRepositoryRoot();
        var folder = Path.Combine(root, "SWLOR.Game.Server", "Feature", "GuiDefinition");
        return Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                return name.StartsWith("PlayerGuide", StringComparison.Ordinal) ||
                       name.EndsWith("GuideTopics.cs", StringComparison.Ordinal) ||
                       name is "ArticleBlock.cs" or "QuestionAnswer.cs";
            })
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }
}
