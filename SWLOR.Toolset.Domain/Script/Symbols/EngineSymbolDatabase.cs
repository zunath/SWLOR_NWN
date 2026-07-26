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
        private readonly Dictionary<string, string> _constantFamiliesByName;

        private EngineSymbolDatabase(
            IReadOnlyList<ScriptFunction> functions,
            IReadOnlyList<ScriptConstant> constants,
            IReadOnlyList<string> documentedConstantFamilies)
        {
            Functions = functions;
            Constants = constants;
            _functionsByName = functions
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            _constantsByName = constants
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            _constantFamiliesByName = BuildConstantFamilyMap(documentedConstantFamilies, constants);
        }

        public IReadOnlyList<ScriptFunction> Functions { get; }

        public IReadOnlyList<ScriptConstant> Constants { get; }

        /// <summary>An empty database, for when the header cannot be found.</summary>
        public static EngineSymbolDatabase Empty { get; } =
            new(Array.Empty<ScriptFunction>(), Array.Empty<ScriptConstant>(), Array.Empty<string>());

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
            return new EngineSymbolDatabase(parsed.Functions, parsed.Constants, parsed.ConstantFamilies);
        }

        public ScriptFunction? FindFunction(string name) =>
            _functionsByName.TryGetValue(name, out var f) ? f : null;

        public ScriptConstant? FindConstant(string name) =>
            _constantsByName.TryGetValue(name, out var c) ? c : null;

        /// <summary>Constants in a <c>FOO_*</c> family, in declared order.</summary>
        public IReadOnlyList<ScriptConstant> ConstantsInFamily(string family) =>
            Constants.Where(c => c.IsInFamily(family)).ToList();

        /// <summary>
        /// Display family for the Constants reference tree. Header-documented families win over
        /// structural guesses, so <c>ABILITY_CHARISMA</c> sits under <c>ABILITY_*</c>, not the
        /// useless singleton <c>ABILITY_CHARISMA_*</c>.
        /// </summary>
        public string ConstantFamilyOf(ScriptConstant constant) =>
            _constantFamiliesByName.TryGetValue(constant.Name, out var family) ? family : constant.Name;

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

        private static Dictionary<string, string> BuildConstantFamilyMap(
            IReadOnlyList<string> documentedFamilies,
            IReadOnlyList<ScriptConstant> constants)
        {
            var structuralFamilies = BuildStructuralFamilyMap(constants);

            var initialFamilies = constants.ToDictionary(
                c => c.Name,
                c => SelectDisplayFamily(c, documentedFamilies, structuralFamilies),
                StringComparer.OrdinalIgnoreCase);

            return CollapseSingletonWildcardFamilies(initialFamilies);
        }

        private static string SelectDisplayFamily(
            ScriptConstant constant,
            IReadOnlyList<string> documentedFamilies,
            IReadOnlyDictionary<string, string> structuralFamilies)
        {
            structuralFamilies.TryGetValue(constant.Name, out var structural);
            structural ??= constant.Name;

            var documented = documentedFamilies.FirstOrDefault(constant.IsInFamily);
            if (documented == null)
                return structural;

            return FamilyPrefixLength(documented) >= FamilyPrefixLength(structural)
                ? documented
                : structural;
        }

        private static int FamilyPrefixLength(string family) =>
            family.EndsWith("*", StringComparison.Ordinal) ? family.Length - 1 : family.Length;

        private static Dictionary<string, string> BuildStructuralFamilyMap(IReadOnlyList<ScriptConstant> constants)
        {
            var prefixCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var constant in constants)
            {
                foreach (var prefix in FamilyPrefixes(constant.Name))
                    prefixCounts[prefix] = prefixCounts.TryGetValue(prefix, out var count) ? count + 1 : 1;
            }

            return constants.ToDictionary(
                c => c.Name,
                c => FamilyPrefixes(c.Name)
                    .Where(p => prefixCounts[p] > 1)
                    .OrderByDescending(FamilyPrefixLength)
                    .ThenBy(p => p, StringComparer.Ordinal)
                    .FirstOrDefault() ?? c.Name,
                StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> CollapseSingletonWildcardFamilies(Dictionary<string, string> initialFamilies)
        {
            var result = new Dictionary<string, string>(initialFamilies, StringComparer.OrdinalIgnoreCase);
            var changed = true;

            while (changed)
            {
                changed = false;
                var groups = result
                    .GroupBy(pair => pair.Value, StringComparer.Ordinal)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(pair => pair.Key).ToList(),
                        StringComparer.Ordinal);

                foreach (var group in groups)
                {
                    if (!IsWildcardFamily(group.Key) || group.Value.Count != 1)
                        continue;

                    var constantName = group.Value[0];
                    var replacement = ParentFamilyOf(group.Key) ?? constantName;
                    if (replacement == group.Key)
                        continue;

                    result[constantName] = replacement;
                    changed = true;
                }
            }

            return result;
        }

        private static IEnumerable<string> FamilyPrefixes(string name)
        {
            for (var i = 0; i < name.Length; i++)
            {
                if (name[i] == '_')
                    yield return name[..(i + 1)] + "*";
            }
        }

        private static bool IsWildcardFamily(string family) =>
            family.EndsWith("*", StringComparison.Ordinal);

        private static string? ParentFamilyOf(string family)
        {
            if (!IsWildcardFamily(family))
                return null;

            var prefix = family[..^1];
            if (prefix.EndsWith("_", StringComparison.Ordinal))
                prefix = prefix[..^1];

            var lastUnderscore = prefix.LastIndexOf('_');
            return lastUnderscore < 0
                ? null
                : prefix[..(lastUnderscore + 1)] + "*";
        }
    }
}
