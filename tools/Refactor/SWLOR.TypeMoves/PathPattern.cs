using System.Text;
using System.Text.RegularExpressions;

namespace SWLOR.TypeMoves;

/// <summary>Matches repository paths (forward slashes) against patterns: <c>*</c> within one segment, <c>**</c> across segments.</summary>
internal static class PathPattern
{
    public static bool Matches(string pattern, string path) =>
        Regex.IsMatch(path.Replace('\\', '/'), ToRegex(pattern.Replace('\\', '/')), RegexOptions.CultureInvariant);

    private static string ToRegex(string pattern)
    {
        var builder = new StringBuilder("^");
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '*' && i + 1 < pattern.Length && pattern[i + 1] == '*')
            {
                builder.Append(".*");
                i++;
            }
            else if (c == '*')
            {
                builder.Append("[^/]*");
            }
            else if (c == '?')
            {
                builder.Append("[^/]");
            }
            else
            {
                builder.Append(Regex.Escape(c.ToString()));
            }
        }

        return builder.Append('$').ToString();
    }
}
