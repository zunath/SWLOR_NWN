using System.Text.RegularExpressions;

namespace SWLOR.TypeMoves;

/// <summary>
/// Keeps <c>tools/Refactor/HandlerRenames.txt</c> (<c>&lt;master name&gt; &lt;branch name&gt;</c> per line) in step with the handler
/// manifest: each manifest line the text rewrite renamed (a handler of a moved type) gets a line, or the line whose branch
/// name it was gets the new branch name, so master's name stays on the left.
/// </summary>
internal static class HandlerRenames
{
    private static readonly Regex HandlerLine = new(@"^\s*(action|conditional)\s+(?<name>\S+)\s*$", RegexOptions.CultureInvariant);

    /// <summary>The handler names a rewrite of the manifest changed, as (old, new) pairs in manifest order.</summary>
    public static List<(string Old, string New)> Pairs(string before, string after)
    {
        var oldLines = before.Split('\n');
        var newLines = after.Split('\n');
        var pairs = new List<(string, string)>();
        if (oldLines.Length != newLines.Length)
            throw new TypeMoveException("The rewrite changed the number of lines of the handler manifest.");

        for (var i = 0; i < oldLines.Length; i++)
        {
            var oldLine = oldLines[i].TrimEnd('\r');
            var newLine = newLines[i].TrimEnd('\r');
            if (oldLine == newLine)
                continue;

            var oldMatch = HandlerLine.Match(StripComment(oldLine));
            var newMatch = HandlerLine.Match(StripComment(newLine));
            if (oldMatch.Success && newMatch.Success)
                pairs.Add((oldMatch.Groups["name"].Value, newMatch.Groups["name"].Value));
        }

        return pairs;
    }

    /// <summary>The renames file with each pair recorded.</summary>
    public static string Update(string text, IReadOnlyList<(string Old, string New)> pairs)
    {
        var eol = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = text.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        foreach (var (old, @new) in pairs.Distinct())
        {
            var updated = false;
            for (var i = 0; i < lines.Count; i++)
            {
                var parts = lines[i].Split(' ');
                if (!lines[i].StartsWith('#') && parts.Length == 2 && parts[1] == old)
                {
                    lines[i] = parts[0] + " " + @new;
                    updated = true;
                    break;
                }
            }

            if (!updated && !lines.Contains(old + " " + @new))
                lines.Add(old + " " + @new);
        }

        return string.Join(eol, lines) + eol;
    }

    private static string StripComment(string line)
    {
        var comment = line.IndexOf("  #", StringComparison.Ordinal);
        return comment >= 0 ? line[..comment] : line;
    }
}
