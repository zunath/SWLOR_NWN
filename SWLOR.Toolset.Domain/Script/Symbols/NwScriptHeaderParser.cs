using System.Text;
using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Domain.Script.Symbols
{
    /// <summary>
    /// Reads nwscript.nss — the engine header — into functions and constants with their
    /// documentation attached.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The header that ships in this repo (<c>SWLOR.NWN.API/NWN/nwscript-8193.37.nss</c>) is version
    /// matched to the NWN.Core packages the server runs against, so completion is accurate without
    /// requiring an NWN install anywhere.
    /// </para>
    /// <para>
    /// Its comment convention is machine readable and consistent, which is what makes rich tooltips
    /// and parameter-aware completion fall out for free:
    /// <code>
    /// // Get the nNth creature nearest to oTarget.
    /// // - nFirstCriteriaType: CREATURE_TYPE_*
    /// // * Return value on error: OBJECT_INVALID
    /// object GetNearestCreature(int nFirstCriteriaType, ...);
    /// </code>
    /// A <c>// - name:</c> line documents one parameter, and a <c>FOO_*</c> mention inside it names
    /// the constant family that parameter accepts.
    /// </para>
    /// </remarks>
    public static class NwScriptHeaderParser
    {
        // A declaration ends in ');' on one logical line. Return type and name are plain identifiers.
        private static readonly Regex FunctionPattern = new(
            @"^(?<ret>[A-Za-z_][A-Za-z0-9_]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\((?<args>[^;]*)\)\s*;",
            RegexOptions.Compiled);

        private static readonly Regex ConstantPattern = new(
            @"^(?<type>int|float|string|object|effect|itemproperty|vector|location|talent|json|sqlquery|cassowary)\s+(?<name>[A-Z_][A-Z0-9_]*)\s*=\s*(?<value>[^;]+?)\s*;",
            RegexOptions.Compiled);

        // "CREATURE_TYPE_*" or "ABILITY_*" inside a doc line. The trailing "_*" is required, so a
        // bare word or a multiplication in prose cannot be mistaken for a family. An earlier version
        // demanded two underscore-separated segments and silently missed every single-segment family
        // (ABILITY_*, ACTION_*, ANIMATION_*), losing a third of the parameter hints.
        private static readonly Regex FamilyPattern = new(@"\b([A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*_)\*", RegexOptions.Compiled);

        private static readonly Regex ParamDocPattern = new(@"^-\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<text>.*)$", RegexOptions.Compiled);

        /// <summary>Everything parsed out of one header file.</summary>
        public sealed record Result(
            IReadOnlyList<ScriptFunction> Functions,
            IReadOnlyList<ScriptConstant> Constants,
            IReadOnlyList<string> ConstantFamilies);

        /// <summary>Parses a header from disk.</summary>
        public static Result ParseFile(string path, IReadOnlyDictionary<string, string>? categories = null) =>
            Parse(File.ReadAllText(path), categories);

        /// <summary>Parses header source.</summary>
        public static Result Parse(string source, IReadOnlyDictionary<string, string>? categories = null)
        {
            var functions = new List<ScriptFunction>();
            var constants = new List<ScriptConstant>();
            var constantFamilies = FamilyPattern.Matches(source)
                .Select(m => m.Groups[1].Value + "*")
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(FamilyPrefixLength)
                .ThenBy(f => f, StringComparer.Ordinal)
                .ToList();

            // Comment lines accumulate until a declaration consumes them; anything else clears them,
            // so a stray comment far above a function never gets attached to it.
            var comment = new List<string>();
            var lines = source.Split('\n');

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r').Trim();

                if (line.StartsWith("//", StringComparison.Ordinal))
                {
                    comment.Add(line[2..].Trim());
                    continue;
                }

                if (line.Length == 0)
                {
                    // A blank line inside a comment block is a paragraph break, not a reset: several
                    // declarations in this header separate their prose that way. Only a non-comment,
                    // non-blank line that is not a declaration clears the buffer.
                    continue;
                }

                var fn = FunctionPattern.Match(line);
                if (fn.Success && !IsConstantLine(line))
                {
                    functions.Add(BuildFunction(fn, comment, categories));
                    comment.Clear();
                    continue;
                }

                var cn = ConstantPattern.Match(line);
                if (cn.Success)
                {
                    constants.Add(new ScriptConstant(
                        cn.Groups["name"].Value,
                        cn.Groups["type"].Value,
                        cn.Groups["value"].Value.Trim()));
                    comment.Clear();
                    continue;
                }

                comment.Clear();
            }

            return new Result(functions, constants, constantFamilies);
        }

        private static int FamilyPrefixLength(string family) =>
            family.EndsWith("*", StringComparison.Ordinal) ? family.Length - 1 : family.Length;

        // "int ITEM_PROPERTY_X = 1 ;" would otherwise match the function pattern's shape if it
        // contained parentheses; keep the two disjoint explicitly.
        private static bool IsConstantLine(string line) => line.Contains('=') && !line.Contains('(');

        private static ScriptFunction BuildFunction(
            Match match, List<string> comment, IReadOnlyDictionary<string, string>? categories)
        {
            var name = match.Groups["name"].Value;
            var docs = ParseComment(comment);
            var parameters = ParseParameters(match.Groups["args"].Value, docs.Parameters);

            var resolved = categories != null && categories.TryGetValue(name, out var c) ? c : "Uncategorized";

            return new ScriptFunction(name, match.Groups["ret"].Value, parameters, docs.Summary, docs.ReturnsOnError, resolved);
        }

        private sealed record CommentDocs(string? Summary, string? ReturnsOnError, Dictionary<string, string> Parameters);

        private static CommentDocs ParseComment(List<string> comment)
        {
            var summary = new StringBuilder();
            var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            string? returnsOnError = null;
            string? currentParam = null;

            foreach (var line in comment)
            {
                if (line.StartsWith('*'))
                {
                    var text = line[1..].Trim();
                    if (text.StartsWith("Return", StringComparison.OrdinalIgnoreCase))
                        returnsOnError = text;

                    currentParam = null;
                    continue;
                }

                if (line.StartsWith("Return value on error", StringComparison.OrdinalIgnoreCase))
                {
                    returnsOnError = line;
                    currentParam = null;
                    continue;
                }

                var pm = ParamDocPattern.Match(line);
                if (pm.Success)
                {
                    currentParam = pm.Groups["name"].Value;
                    parameters[currentParam] = pm.Groups["text"].Value.Trim();
                    continue;
                }

                // A continuation line belongs to whichever parameter is open; parameter docs in this
                // header routinely wrap across several lines.
                if (currentParam != null)
                {
                    parameters[currentParam] = (parameters[currentParam] + " " + line).Trim();
                    continue;
                }

                if (line.Length > 0)
                    summary.Append(summary.Length > 0 ? " " : "").Append(line);
            }

            return new CommentDocs(
                summary.Length > 0 ? summary.ToString() : null,
                returnsOnError,
                parameters);
        }

        private static List<ScriptParameter> ParseParameters(string args, Dictionary<string, string> docs)
        {
            var result = new List<ScriptParameter>();
            if (string.IsNullOrWhiteSpace(args))
                return result;

            foreach (var part in SplitParameters(args))
            {
                var text = part.Trim();
                if (text.Length == 0)
                    continue;

                string? defaultValue = null;
                var eq = text.IndexOf('=');
                if (eq >= 0)
                {
                    defaultValue = text[(eq + 1)..].Trim();
                    text = text[..eq].Trim();
                }

                var space = text.LastIndexOf(' ');
                if (space < 0)
                    continue;

                var type = text[..space].Trim();
                var name = text[(space + 1)..].Trim();

                docs.TryGetValue(name, out var doc);
                result.Add(new ScriptParameter(type, name, defaultValue, doc, ExtractFamily(doc)));
            }

            return result;
        }

        /// <summary>Splits on commas that are not nested inside parentheses (default values can call).</summary>
        private static IEnumerable<string> SplitParameters(string args)
        {
            var depth = 0;
            var start = 0;
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth--;
                        break;
                    case ',' when depth == 0:
                        yield return args[start..i];
                        start = i + 1;
                        break;
                }
            }

            yield return args[start..];
        }

        private static string? ExtractFamily(string? doc)
        {
            if (string.IsNullOrEmpty(doc))
                return null;

            var m = FamilyPattern.Match(doc);
            return m.Success ? m.Groups[1].Value + "*" : null;
        }
    }
}
