using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The model numbers that name the inventory icons of astromech modules, read from MODULE_ICONS in
/// tools/GenerateAstromechModules.py, which puts them on the blueprints. A number has an icon and no model.
/// </summary>
internal static class AstromechModuleIconNumbers
{
    /// <summary>Base item -> the model numbers of its module kind's icons.</summary>
    public static IReadOnlyDictionary<int, HashSet<int>> ByBaseItem { get; } = Read();

    /// <summary>The generator's source text, for the other constants a test reads from it.</summary>
    public static string Generator => File.ReadAllText(Path.Combine(FindRepositoryRoot(), "tools", "GenerateAstromechModules.py"));

    /// <summary>
    /// Whether an item is an astromech module showing one of its kind's icons. Such an item has nothing to restyle:
    /// the number is no appearance a wearer can pick, and no appearance catalog lists it.
    /// </summary>
    public static bool IsModuleIcon(string resref, int baseItem, int model) =>
        resref.StartsWith("am_") && ByBaseItem.TryGetValue(baseItem, out var icons) && icons.Contains(model);

    private static IReadOnlyDictionary<int, HashSet<int>> Read()
    {
        var generator = Generator;
        var table = Regex.Match(generator, @"^MODULE_ICONS = \{(.*?)^\}", RegexOptions.Singleline | RegexOptions.Multiline);
        var variants = int.Parse(Regex.Match(generator, @"^ICON_VARIANTS = (\d+)", RegexOptions.Multiline).Groups[1].Value);
        return Regex.Matches(table.Groups[1].Value, @"(\d+): \(""\w+"", (\d+)\)")
            .ToDictionary(row => int.Parse(row.Groups[1].Value),
                row => Enumerable.Range(int.Parse(row.Groups[2].Value), variants).ToHashSet());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
