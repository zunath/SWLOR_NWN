using System.Text;
using System.Text.RegularExpressions;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// The checked-in text form of a list of cross-domain edges: one line per edge, <c>Combat -&gt; Stat | 12</c>
/// (from, to, number of distinct referencing members), grouped by domain pair and sorted, with <c>#</c> comment lines.
/// The member count is information for reviewers and is never compared.
/// </summary>
internal static class EdgeBaselineFile
{
    private static readonly Regex EdgeLine = new(@"^(?<from>\S+) -> (?<to>\S+) \| (?<members>\d+)$", RegexOptions.Compiled);

    public static string Render(IEnumerable<UnitEdge> edges, IEnumerable<string> headerLines)
    {
        var text = new StringBuilder();
        foreach (var line in headerLines)
            text.Append("# ").Append(line).Append('\n');

        var groups = edges
            .GroupBy(edge => (edge.FromDomain, edge.ToDomain))
            .OrderBy(group => group.Key.FromDomain, StringComparer.Ordinal)
            .ThenBy(group => group.Key.ToDomain, StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var ordered = group.OrderBy(edge => edge.Key, StringComparer.Ordinal).ToList();
            text.Append('\n');
            text.Append($"# {group.Key.FromDomain} -> {group.Key.ToDomain} ({ordered.Count} {(ordered.Count == 1 ? "edge" : "edges")})\n");
            foreach (var edge in ordered)
                text.Append($"{edge.Key} | {edge.MemberCount}\n");
        }

        return text.ToString();
    }

    /// <summary>The edge keys (<c>Combat -&gt; Stat</c>) listed in the file; comments, blank lines and member counts are ignored.</summary>
    public static IReadOnlySet<string> ParseKeys(string text)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var lineNumber = 0;
        foreach (var rawLine in Normalize(text).Split('\n'))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var match = EdgeLine.Match(line);
            if (!match.Success)
                throw new FormatException($"Baseline line {lineNumber} is not '<from> -> <to> | <members>': {line}");

            var key = $"{match.Groups["from"].Value} -> {match.Groups["to"].Value}";
            if (!keys.Add(key))
                throw new FormatException($"Baseline line {lineNumber} lists '{key}' a second time.");
        }

        return keys;
    }

    public static string Normalize(string text) => text.Replace("\r\n", "\n");
}
