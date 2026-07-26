using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Domain.Script.Symbols
{
    /// <summary>
    /// Every engine function and constant, indexed for completion, signature help and the
    /// Script Reference browser.
    /// </summary>
    /// <remarks>
    /// Categories are derived from the filenames in <c>SWLOR.NWN.API/NWScript/</c> — that folder
    /// groups the C# wrappers the same way nwscript.nss groups its declarations
    /// (<c>CoreFunctions.cs</c>, <c>ActionFunctions.cs</c>, <c>EffectFunctions.cs</c>, …), so mapping
    /// function name → declaring file reproduces Aurora's category tree and keeps it current as that
    /// API evolves. Scanning C# source for metadata has precedent here in
    /// <c>GameData/GameCode/SourceIdScanner</c>.
    /// </remarks>
    public sealed class EngineSymbolDatabase
    {
        private static readonly Regex MethodPattern = new(
            @"public\s+static\s+[A-Za-z_][A-Za-z0-9_<>,\.\[\]\?\s]*?\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
            RegexOptions.Compiled);

        private readonly Dictionary<string, ScriptFunction> _functionsByName;
        private readonly Dictionary<string, ScriptConstant> _constantsByName;

        private EngineSymbolDatabase(IReadOnlyList<ScriptFunction> functions, IReadOnlyList<ScriptConstant> constants)
        {
            Functions = functions;
            Constants = constants;
            _functionsByName = functions
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            _constantsByName = constants
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<ScriptFunction> Functions { get; }

        public IReadOnlyList<ScriptConstant> Constants { get; }

        /// <summary>An empty database, for when the header cannot be found.</summary>
        public static EngineSymbolDatabase Empty { get; } =
            new(Array.Empty<ScriptFunction>(), Array.Empty<ScriptConstant>());

        /// <summary>
        /// Builds from the engine header, optionally taking categories from the
        /// <c>SWLOR.NWN.API/NWScript/</c> folder.
        /// </summary>
        public static EngineSymbolDatabase Load(string headerPath, string? nwscriptApiDirectory = null)
        {
            var categories = nwscriptApiDirectory != null && Directory.Exists(nwscriptApiDirectory)
                ? BuildCategoryMap(nwscriptApiDirectory)
                : null;

            var parsed = NwScriptHeaderParser.ParseFile(headerPath, categories);
            return new EngineSymbolDatabase(parsed.Functions, parsed.Constants);
        }

        public ScriptFunction? FindFunction(string name) =>
            _functionsByName.TryGetValue(name, out var f) ? f : null;

        public ScriptConstant? FindConstant(string name) =>
            _constantsByName.TryGetValue(name, out var c) ? c : null;

        /// <summary>Constants in a <c>FOO_*</c> family, in declared order.</summary>
        public IReadOnlyList<ScriptConstant> ConstantsInFamily(string family) =>
            Constants.Where(c => c.IsInFamily(family)).ToList();

        /// <summary>Function categories with their counts, for the reference browser's tree.</summary>
        public IReadOnlyList<(string Category, int Count)> CategoryCounts() =>
            Functions.GroupBy(f => f.Category)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => (g.Key, g.Count()))
                .ToList();

        /// <summary>
        /// Maps function name → category by finding which <c>*Functions.cs</c> declares it.
        /// "CreatureFunctions.cs" becomes "Creature".
        /// </summary>
        private static Dictionary<string, string> BuildCategoryMap(string directory)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var category = Path.GetFileNameWithoutExtension(file);
                if (category.EndsWith("Functions", StringComparison.Ordinal))
                    category = category[..^"Functions".Length];

                if (category.Length == 0)
                    continue;

                foreach (Match m in MethodPattern.Matches(File.ReadAllText(file)))
                {
                    var name = m.Groups[1].Value;

                    // First file wins: a name declared in two partials belongs to whichever the
                    // enumeration reached first, which is stable for a fixed folder.
                    map.TryAdd(name, Humanize(category));
                }
            }

            return map;
        }

        /// <summary>"LocalVariable" → "Local Variable", so the tree reads as labels not identifiers.</summary>
        private static string Humanize(string pascal)
        {
            var spaced = Regex.Replace(pascal, "(?<=[a-z0-9])(?=[A-Z])", " ");
            return spaced;
        }
    }
}
