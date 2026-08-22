using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>
    /// Reads the author-facing names paired with spawn table IDs in the server definitions.
    /// The existing ID scanner remains authoritative for which tables exist; this reader only
    /// supplies labels for editor pickers and falls back to a humanized ID when a definition
    /// omits its optional name.
    /// </summary>
    internal static class SpawnTableSourceReader
    {
        private static readonly Regex ConstStringRegex = new(
            @"const\s+string\s+(?<name>[A-Za-z_]\w*)\s*=\s*""(?<value>(?:[^""\\]|\\.)*)""\s*;",
            RegexOptions.Compiled);

        private static readonly Regex CreateCallRegex = new(
            @"(?:builder|_builder)\.Create\(\s*" +
            @"(?:""(?<literal>(?:[^""\\]|\\.)*)""|(?<identifier>[A-Za-z_]\w*))\s*" +
            @"(?:,\s*""(?<display>(?:[^""\\]|\\.)*)"")?",
            RegexOptions.Compiled);

        private static readonly Regex FishingPointRegex = new(
            @"CreateFishingPoint\(\s*""(?<id>(?:[^""\\]|\\.)*)""\s*,\s*" +
            @"FishingLocationType\.(?<location>[A-Za-z_]\w*)",
            RegexOptions.Compiled);

        private static readonly IReadOnlyDictionary<string, string> TokenNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CZ220"] = "CZ-220",
                ["DAN"] = "Dantooine",
                ["DANTOOINE"] = "Dantooine",
                ["DATH"] = "Dathomir",
                ["FP"] = string.Empty,
                ["HUTL"] = "Hutlar",
                ["MONC"] = "Mon Cala",
                ["NAR"] = "Nar Shaddaa",
                ["NPC"] = "NPC",
                ["TAT"] = "Tatooine",
                ["VISC"] = "Viscara"
            };

        public static IReadOnlyList<SpawnTableInfo> Read(
            string? directoryPath,
            IEnumerable<string> ids)
        {
            var idSet = new HashSet<string>(ids, StringComparer.Ordinal);
            var names = idSet.ToDictionary(id => id, HumanizeId, StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                return ToList(names);

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(
                    directoryPath, "*.cs", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return ToList(names);
            }

            foreach (var file in files)
            {
                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                var constants = ConstStringRegex.Matches(text)
                    .ToDictionary(
                        match => match.Groups["name"].Value,
                        match => Decode(match.Groups["value"].Value),
                        StringComparer.Ordinal);

                foreach (Match match in CreateCallRegex.Matches(text))
                {
                    var id = match.Groups["literal"].Success
                        ? Decode(match.Groups["literal"].Value)
                        : constants.GetValueOrDefault(match.Groups["identifier"].Value);
                    if (id == null || !idSet.Contains(id))
                        continue;

                    var display = match.Groups["display"];
                    if (display.Success && !string.IsNullOrWhiteSpace(display.Value))
                        names[id] = Decode(display.Value);
                }

                // Fishing definitions name their location through an enum rather than the optional
                // SpawnTableBuilder display-name argument. That enum member is the friendly label.
                foreach (Match match in FishingPointRegex.Matches(text))
                {
                    var id = Decode(match.Groups["id"].Value);
                    if (idSet.Contains(id))
                        names[id] = HumanizePascalCase(match.Groups["location"].Value);
                }
            }

            return ToList(names);
        }

        private static IReadOnlyList<SpawnTableInfo> ToList(
            IReadOnlyDictionary<string, string> names) =>
            names
                .Select(pair => new SpawnTableInfo(pair.Key, pair.Value))
                .OrderBy(table => table.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(table => table.Id, StringComparer.Ordinal)
                .ToList();

        private static string HumanizeId(string id)
        {
            var words = id
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => TokenNames.TryGetValue(token, out var name)
                    ? name
                    : token.Length <= 3
                        ? token
                        : char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant())
                .Where(word => word.Length > 0);

            return string.Join(" ", words);
        }

        private static string HumanizePascalCase(string value) =>
            Regex.Replace(
                value,
                @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
                " ");

        private static string Decode(string value) =>
            value.Replace("\\\"", "\"", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal);
    }
}
