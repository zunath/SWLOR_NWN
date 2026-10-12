using System.Collections.Generic;
using System.Text.Json;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// Reads a palette (.itp) in GFF JSON form into categories and blueprints, the way the native creator shows it.
    /// Category nodes name themselves with NAME or a TLK STRREF and hold their children in LIST.
    /// Blueprint nodes carry a RESREF, a NAME or STRREF and, for creatures, CR and FACTION.
    /// </summary>
    public static class DMPaletteReader
    {
        public const string PathSeparator = " / ";
        private const string UnnamedCategory = "(unnamed)";

        /// <summary>
        /// Reads a palette.
        /// </summary>
        /// <param name="paletteJson">The palette as GFF JSON, as TemplateToJson returns it.</param>
        /// <param name="category">The kind of blueprint the palette lists.</param>
        /// <param name="source">Which palette this is.</param>
        /// <param name="resolveStrRef">Looks up TLK text for a string reference.</param>
        public static DMPalette Read(string paletteJson, DMBlueprintCategory category, DMBlueprintSource source, Func<int, string> resolveStrRef)
        {
            var categoryPaths = new List<string>();
            var blueprints = new List<DMBlueprint>();
            if (!string.IsNullOrWhiteSpace(paletteJson))
            {
                using var document = JsonDocument.Parse(paletteJson);
                if (TryGetValue(document.RootElement, "MAIN", out var main) && main.ValueKind == JsonValueKind.Array)
                    ReadNodes(main, string.Empty, category, source, resolveStrRef, categoryPaths, blueprints);
            }

            return new DMPalette(categoryPaths, blueprints);
        }

        // Returns how many blueprints the nodes hold, including nested categories.
        private static int ReadNodes(
            JsonElement nodes,
            string parentPath,
            DMBlueprintCategory category,
            DMBlueprintSource source,
            Func<int, string> resolveStrRef,
            List<string> categoryPaths,
            List<DMBlueprint> blueprints)
        {
            var count = 0;
            foreach (var node in nodes.EnumerateArray())
            {
                var name = ReadName(node, resolveStrRef);
                if (TryGetValue(node, "RESREF", out var resrefValue))
                {
                    var resref = resrefValue.GetString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(resref))
                        continue;

                    float? challengeRating = TryGetValue(node, "CR", out var cr) && cr.ValueKind == JsonValueKind.Number
                        ? cr.GetSingle()
                        : null;
                    var faction = TryGetValue(node, "FACTION", out var factionValue) ? factionValue.GetString() : string.Empty;
                    blueprints.Add(new DMBlueprint(
                        resref,
                        string.IsNullOrWhiteSpace(name) ? resref : name,
                        parentPath,
                        category,
                        source,
                        challengeRating,
                        faction));
                    count++;
                    continue;
                }

                var path = string.IsNullOrWhiteSpace(name) ? UnnamedCategory : name;
                if (parentPath.Length > 0)
                    path = parentPath + PathSeparator + path;

                // Listed before its children so the combo reads in tree order; dropped again if it holds nothing.
                var pathIndex = categoryPaths.Count;
                categoryPaths.Add(path);
                var nested = TryGetValue(node, "LIST", out var children) && children.ValueKind == JsonValueKind.Array
                    ? ReadNodes(children, path, category, source, resolveStrRef, categoryPaths, blueprints)
                    : 0;
                if (nested == 0)
                    categoryPaths.RemoveAt(pathIndex);

                count += nested;
            }

            return count;
        }

        private static string ReadName(JsonElement node, Func<int, string> resolveStrRef)
        {
            if (TryGetValue(node, "NAME", out var name) && name.ValueKind == JsonValueKind.String)
            {
                var text = name.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    return text.Trim();
            }

            if (TryGetValue(node, "STRREF", out var strRef) && strRef.ValueKind == JsonValueKind.Number &&
                strRef.TryGetInt64(out var reference) && reference > 0 && reference <= int.MaxValue)
            {
                return (resolveStrRef((int)reference) ?? string.Empty).Trim();
            }

            return string.Empty;
        }

        // GFF JSON stores each field as {"type": ..., "value": ...}.
        private static bool TryGetValue(JsonElement node, string field, out JsonElement value)
        {
            value = default;
            return node.ValueKind == JsonValueKind.Object &&
                   node.TryGetProperty(field, out var wrapper) &&
                   wrapper.ValueKind == JsonValueKind.Object &&
                   wrapper.TryGetProperty("value", out value);
        }
    }
}
