using System.Text;
using System.Text.RegularExpressions;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// The checked-in text form of a debt measure: <c>#</c> header lines, then one <c>&lt;key&gt; | &lt;count&gt;</c> line per entry,
/// sorted by key. Unlike the cross-domain baseline the count is compared: it may not grow, and when it shrinks the
/// baseline is regenerated in the same pull request.
/// </summary>
internal static class DebtBaselineFile
{
    private static readonly Regex EntryLine = new(@"^(?<key>\S.*?) \| (?<count>\d+)$", RegexOptions.Compiled);

    public static string Render(IEnumerable<DebtEntry> entries, IEnumerable<string> headerLines)
    {
        var text = new StringBuilder();
        foreach (var line in headerLines)
            text.Append(line.Length == 0 ? "#" : "# " + line).Append('\n');

        text.Append('\n');
        foreach (var entry in entries.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            text.Append(entry.Key).Append(" | ").Append(entry.Count).Append('\n');

        return text.ToString();
    }

    /// <summary>The entries listed in the file; comments and blank lines are ignored.</summary>
    public static IReadOnlyDictionary<string, int> Parse(string text)
    {
        var entries = new Dictionary<string, int>(StringComparer.Ordinal);
        var lineNumber = 0;
        foreach (var rawLine in Normalize(text).Split('\n'))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var match = EntryLine.Match(line);
            if (!match.Success)
                throw new FormatException($"Baseline line {lineNumber} is not '<key> | <count>': {line}");

            var key = match.Groups["key"].Value;
            if (!entries.TryAdd(key, int.Parse(match.Groups["count"].Value)))
                throw new FormatException($"Baseline line {lineNumber} lists '{key}' a second time.");
        }

        return entries;
    }

    public static string Normalize(string text) => text.Replace("\r\n", "\n");
}
