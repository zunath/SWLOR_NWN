using System.Text.Json;

namespace SWLOR.ContentInjection;

/// <summary>
/// <c>tools/Refactor/ContentInjection.json</c>: the content the branch has converted, as repository-relative paths with forward
/// slashes. A path that ends in <c>/</c> is a folder: every <c>.cs</c> file below it is converted, so a file master adds there is
/// converted whole at the next sync. An empty list leaves the tool inactive: it changes nothing but the registrations it owns.
/// </summary>
internal sealed class ContentInjectionData
{
    private ContentInjectionData(IReadOnlyList<string> converted)
    {
        Converted = converted;
    }

    /// <summary>The converted files and folders.</summary>
    public IReadOnlyList<string> Converted { get; }

    public static ContentInjectionData Load(string path)
    {
        if (!File.Exists(path))
            throw new ContentInjectionException($"The data file {path} does not exist.");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("converted", out var list) || list.ValueKind != JsonValueKind.Array)
            throw new ContentInjectionException($"{path} has no \"converted\" list.");

        var converted = new List<string>();
        foreach (var entry in list.EnumerateArray())
        {
            var value = entry.GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Contains('\\') || Path.IsPathRooted(value))
                throw new ContentInjectionException($"{path}: \"{value}\" is not a repository-relative path with forward slashes.");
            converted.Add(value);
        }

        if (!converted.SequenceEqual(converted.OrderBy(value => value, StringComparer.Ordinal)) || converted.Distinct().Count() != converted.Count)
            throw new ContentInjectionException($"{path}: the \"converted\" list must be sorted and have no duplicates.");

        return new ContentInjectionData(converted);
    }

    public static ContentInjectionData Of(params string[] converted) => new(converted);

    /// <summary>True when the repository-relative <paramref name="path"/> is converted content.</summary>
    public bool IsConverted(string path) =>
        Converted.Any(entry => entry.EndsWith('/') ? path.StartsWith(entry, StringComparison.Ordinal) : path == entry);
}
