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
    /// Handles the two shapes actually used across every quest/spawn definition file in this repo:
    ///   - a string literal passed directly, e.g. <c>builder.Create("selan_request", ...)</c>
    ///     (~75 quest calls, ~191 spawn table calls use this form directly);
    ///   - a same-file <c>const string</c> field resolved by name, e.g.
    ///     <c>_builder.Create(PrimalOverrunFoundationQuestId, ...)</c> where the file also declares
    ///     <c>private const string PrimalOverrunFoundationQuestId = "primal_overrun_foundation";</c>
    ///     (this is the dominant pattern in the ability capstone quest chains - ~196 const
    ///     declarations across 19 files).
    ///
    /// Deliberately NOT resolved: IDs threaded through a further level of indirection, i.e. a
    /// private helper method that takes the ID as a plain <c>string</c> parameter and is itself
    /// invoked with a literal elsewhere (e.g. <c>AgricultureGuildQuestDefinition.BuildItemTask</c>,
    /// <c>FishingSpawnPointDefinition.CreateFishingPoint</c>). Resolving that would mean matching
    /// call-site arguments to method parameters positionally across the file, which needs real
    /// parsing rather than a well-anchored regex. It affects a small minority of definitions (mostly
    /// per-item guild task generators and fishing points) and does not change validation behavior in
    /// a meaningful way - IDs from these generators are internal to their own generator and not
    /// referenced by hand-authored content that would need validating against this index.
    /// </summary>
    internal static class SourceIdScanner
    {
        private static readonly Regex ConstStringRegex = new(
            @"const\s+string\s+(?<name>[A-Za-z_]\w*)\s*=\s*""(?<value>(?:[^""\\]|\\.)*)""\s*;",
            RegexOptions.Compiled);

        private static readonly Regex CreateCallRegex = new(
            @"(?:builder|_builder)\.Create\(\s*(?:""(?<literal>(?:[^""\\]|\\.)*)""|(?<identifier>[A-Za-z_]\w*))",
            RegexOptions.Compiled);

        /// <summary>
        /// Scans every *.cs file under <paramref name="directoryPath"/> for builder.Create() IDs.
        /// Returns an empty set (never throws) if the directory is missing or unreadable.
        /// </summary>
        public static HashSet<string> ScanBuilderCreateIds(string directoryPath)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return ids;
            }

            foreach (var file in files)
            {
                ScanFile(file, ids);
            }

            return ids;
        }

        private static void ScanFile(string filePath, HashSet<string> ids)
        {
            string text;
            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return;
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
        }
    }
}
