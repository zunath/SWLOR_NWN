using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>
    /// Extracts string IDs passed to <c>builder.Create("id", ...)</c> / <c>_builder.Create("id", ...)</c>
    /// calls in SWLOR.Game.Server's QuestDefinition and SpawnDefinition source folders. These IDs are
    /// declared inside method bodies as arguments to <c>QuestBuilder</c>/<c>SpawnTableBuilder</c>
    /// fluent calls, so they are invisible to reflection - the only way to recover them without
    /// executing game code is to scan the C# source text.
    ///
    /// Handles the shapes used across the quest/spawn definition files in this repo:
    ///   - a string literal passed directly, e.g. <c>builder.Create("selan_request", ...)</c>
    ///     (~75 quest calls, ~191 spawn table calls use this form directly);
    ///   - a same-file <c>const string</c> field resolved by name, e.g.
    ///     <c>_builder.Create(PrimalOverrunFoundationQuestId, ...)</c> where the file also declares
    ///     <c>private const string PrimalOverrunFoundationQuestId = "primal_overrun_foundation";</c>
    ///     (this is the dominant pattern in the ability capstone quest chains - ~196 const
    ///     declarations across 19 files).
    ///
    /// Also resolves the one-level helper shape used by fishing spawn tables: a private helper takes
    /// an ID as its first string parameter, passes that parameter directly to
    /// <c>builder.Create</c>, and is called with a literal in the same file.
    ///
    /// Finally, it expands simple inclusive integer loops whose Create ID interpolates the loop
    /// variable, such as <c>for (var tier = 1; tier &lt;= 5; tier++)</c> creating
    /// <c>$"SLICING_TERMINAL_T{tier}"</c>.
    /// </summary>
    internal static class SourceIdScanner
    {
        private static readonly Regex ConstStringRegex = new(
            @"const\s+string\s+(?<name>[A-Za-z_]\w*)\s*=\s*""(?<value>(?:[^""\\]|\\.)*)""\s*;",
            RegexOptions.Compiled);

        private static readonly Regex CreateCallRegex = new(
            @"(?:builder|_builder)\.Create\(\s*(?:""(?<literal>(?:[^""\\]|\\.)*)""|(?<identifier>[A-Za-z_]\w*))",
            RegexOptions.Compiled);

        private static readonly Regex HelperCreateRegex = new(
            @"(?:(?:private|public|protected|internal)\s+)?(?:static\s+)?(?:void|[A-Za-z_][\w<>,.? ]*)\s+" +
            @"(?<method>[A-Za-z_]\w*)\s*\(\s*string\s+(?<parameter>[A-Za-z_]\w*)[^)]*\)\s*\{" +
            @"(?:(?!\n\s*\}).)*?(?:builder|_builder)\.Create\(\s*\k<parameter>\b",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex IntegerRangeLoopRegex = new(
            @"for\s*\(\s*var\s+(?<variable>[A-Za-z_]\w*)\s*=\s*(?<start>\d+)\s*;\s*" +
            @"\k<variable>\s*<=\s*(?<end>\d+)\s*;\s*\k<variable>\+\+\s*\)\s*\{" +
            @"(?<body>(?:(?!\r?\n\s*\}).)*)\r?\n\s*\}",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Scans every *.cs file under <paramref name="directoryPath"/> for builder.Create() IDs.
        /// Returns an empty set (never throws) if the directory is missing or unreadable.
        /// </summary>
        public static HashSet<string> ScanBuilderCreateIds(string directoryPath) =>
            ScanBuilderCreateIds(directoryPath, out _);

        /// <summary>
        /// Scans a source tree for builder-created ids, reporting whether every file was actually read.
        /// </summary>
        /// <remarks>
        /// <paramref name="complete"/> matters because callers use the result to decide what is valid: a
        /// partial scan silently drops real ids, and validation then flags legitimate
        /// CREATURE_SPAWN_TABLE_ID values as unknown. A caller that cannot tell a partial scan from an
        /// empty tree reports those false errors with full confidence.
        /// </remarks>
        public static HashSet<string> ScanBuilderCreateIds(string directoryPath, out bool complete)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            complete = true;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                complete = false;
                return ids;
            }

            foreach (var file in files)
            {
                if (!ScanFile(file, ids))
                    complete = false;
            }

            return ids;
        }

        private static bool ScanFile(string filePath, HashSet<string> ids)
        {
            string text;
            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }

            var constants = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match constMatch in ConstStringRegex.Matches(text))
            {
                constants[constMatch.Groups["name"].Value] = constMatch.Groups["value"].Value;
            }

            foreach (Match createMatch in CreateCallRegex.Matches(text))
            {
                var literalGroup = createMatch.Groups["literal"];
                if (literalGroup.Success)
                {
                    ids.Add(literalGroup.Value);
                    continue;
                }

                var identifierGroup = createMatch.Groups["identifier"];
                if (identifierGroup.Success && constants.TryGetValue(identifierGroup.Value, out var resolved))
                {
                    ids.Add(resolved);
                }
            }

            foreach (Match helperMatch in HelperCreateRegex.Matches(text))
            {
                var methodName = Regex.Escape(helperMatch.Groups["method"].Value);
                var callRegex = new Regex(
                    $@"\b{methodName}\s*\(\s*""(?<literal>(?:[^""\\]|\\.)*)""",
                    RegexOptions.Compiled);

                foreach (Match callMatch in callRegex.Matches(text))
                    ids.Add(callMatch.Groups["literal"].Value);
            }

            foreach (Match loopMatch in IntegerRangeLoopRegex.Matches(text))
            {
                var start = int.Parse(loopMatch.Groups["start"].Value);
                var end = int.Parse(loopMatch.Groups["end"].Value);
                if (end < start || end - start > 10_000)
                    continue;

                var variable = Regex.Escape(loopMatch.Groups["variable"].Value);
                var interpolatedCreateRegex = new Regex(
                    @"(?:builder|_builder)\.Create\(\s*\$""(?<prefix>(?:[^""\\]|\\.)*)\{" +
                    variable +
                    @"\}(?<suffix>(?:[^""\\]|\\.)*)""",
                    RegexOptions.Compiled);

                foreach (Match createMatch in interpolatedCreateRegex.Matches(
                             loopMatch.Groups["body"].Value))
                {
                    var prefix = createMatch.Groups["prefix"].Value;
                    var suffix = createMatch.Groups["suffix"].Value;
                    for (var value = start; value <= end; value++)
                        ids.Add($"{prefix}{value}{suffix}");
                }
            }

            return true;
        }
    }
}
