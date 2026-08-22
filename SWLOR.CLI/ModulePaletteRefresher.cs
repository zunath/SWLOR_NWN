using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SWLOR.CLI
{
    /// <summary>
    /// Rebuilds the entity descriptors in NWN custom blueprint palettes from the module's blueprints.
    /// </summary>
    /// <remarks>
    /// Aurora's "Refresh Palette" operation treats the ITP as the category definition and the blueprint
    /// category byte (<c>PaletteID</c>, or <c>ID</c> for stores) as membership. It preserves the category
    /// tree, refreshes descriptors that still exist, removes stale descriptors, and appends newly found
    /// blueprints to the terminal category with the matching ID. Packing performs the same operation on
    /// temporary JSON copies so a build cannot dirty the unpacked module source tree.
    /// </remarks>
    public static class ModulePaletteRefresher
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);
        private static readonly Encoding NwnText = CreateNwnTextEncoding();

        private static readonly PaletteDefinition[] Definitions =
        {
            new("creaturepalcus", "utc", "PaletteID", "FirstName", IsCreature: true),
            new("doorpalcus", "utd", "PaletteID", "LocName"),
            new("encounterpalcus", "ute", "PaletteID", "LocalizedName"),
            new("itempalcus", "uti", "PaletteID", "LocalizedName"),
            new("placeablepalcus", "utp", "PaletteID", "LocName"),
            new("soundpalcus", "uts", "PaletteID", "LocName"),
            new("storepalcus", "utm", "ID", "LocName"),
            new("triggerpalcus", "utt", "PaletteID", "LocalizedName"),
            new("waypointpalcus", "utw", "PaletteID", "LocalizedName")
        };

        /// <summary>
        /// Writes refreshed palette JSON files beneath <paramref name="outputDirectory"/>.
        /// The returned map is keyed by the full path of the source ITP JSON and points at its temporary
        /// refreshed replacement.
        /// </summary>
        public static ModulePaletteRefresh Refresh(string moduleRoot, string outputDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

            var fullModuleRoot = Path.GetFullPath(moduleRoot);
            var fullOutputDirectory = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(fullOutputDirectory);

            var factionNames = LoadFactionNames(fullModuleRoot);
            var replacements = new Dictionary<string, string>(PathComparer);
            var results = new List<PaletteRefreshResult>();

            foreach (var definition in Definitions)
            {
                var palettePath = Path.Combine(
                    fullModuleRoot,
                    "itp",
                    definition.PaletteName + ".itp.json");
                if (!File.Exists(palettePath))
                    continue;

                var palette = LoadJsonObject(palettePath);
                var blueprints = LoadBlueprints(fullModuleRoot, definition, factionNames);
                var result = RefreshPalette(palette, definition, blueprints);
                var outputPath = Path.Combine(
                    fullOutputDirectory,
                    definition.PaletteName + ".itp.json");

                File.WriteAllText(outputPath, palette.ToString(Formatting.Indented));
                replacements[Path.GetFullPath(palettePath)] = outputPath;
                results.Add(result);
            }

            return new ModulePaletteRefresh(replacements, results);
        }

        private static PaletteRefreshResult RefreshPalette(
            JObject palette,
            PaletteDefinition definition,
            IReadOnlyList<BlueprintDescriptor> blueprints)
        {
            var terminals = new Dictionary<int, JObject>();
            CollectTerminalCategories(
                RequireListValue(palette, "MAIN", definition.PaletteName),
                definition.PaletteName,
                terminals);

            var desiredByCategory = blueprints
                .Where(blueprint => terminals.ContainsKey(blueprint.CategoryId))
                .GroupBy(blueprint => blueprint.CategoryId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(
                        blueprint => blueprint.ResRef,
                        blueprint => blueprint,
                        StringComparer.OrdinalIgnoreCase));

            var added = 0;
            var removed = 0;
            var updated = 0;
            var included = 0;

            foreach (var (categoryId, terminal) in terminals)
            {
                var existingListField = terminal["LIST"] as JObject;
                var current = existingListField == null
                    ? new JArray()
                    : RequireListValue(
                        terminal,
                        "LIST",
                        $"{definition.PaletteName} category {categoryId}");
                desiredByCategory.TryGetValue(categoryId, out var desired);
                desired ??= new Dictionary<string, BlueprintDescriptor>(StringComparer.OrdinalIgnoreCase);

                var refreshed = new List<JToken>();
                var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var token in current)
                {
                    if (token is not JObject node)
                        throw new InvalidDataException(
                            $"Palette '{definition.PaletteName}' category {categoryId} contains a non-struct entry.");

                    var existingResRef = ReadFieldString(node, "RESREF");
                    if (string.IsNullOrWhiteSpace(existingResRef))
                    {
                        // Defensive preservation for a non-standard terminal. BioWare's format says
                        // terminal lists contain only descriptors, but dropping an unknown node would
                        // be more destructive than Aurora's refresh behavior. Keep the original node:
                        // it can itself be an ID-bearing child category held by the terminals lookup.
                        refreshed.Add(node);
                        continue;
                    }

                    if (!desired.TryGetValue(existingResRef, out var blueprint) ||
                        !emitted.Add(blueprint.ResRef))
                    {
                        removed++;
                        continue;
                    }

                    var descriptor = blueprint.PaletteNode;
                    if (!JToken.DeepEquals(node, descriptor))
                        updated++;

                    refreshed.Add(descriptor.DeepClone());
                    included++;
                }

                foreach (var blueprint in desired.Values
                             .Where(blueprint => !emitted.Contains(blueprint.ResRef))
                             .OrderBy(blueprint => blueprint.ResRef, StringComparer.OrdinalIgnoreCase))
                {
                    refreshed.Add(blueprint.PaletteNode.DeepClone());
                    emitted.Add(blueprint.ResRef);
                    added++;
                    included++;
                }

                if (existingListField != null)
                {
                    // Rebuild the existing array rather than replacing it with clones. Clearing first
                    // detaches every retained category node so adding it back preserves object identity;
                    // later terminal refreshes therefore mutate the live output tree, even when that
                    // terminal is nested beneath another ID-bearing category.
                    current.RemoveAll();
                    foreach (var token in refreshed)
                        current.Add(token);
                }
                else if (refreshed.Count > 0)
                {
                    // Aurora permits an unused terminal category to omit LIST entirely. It materializes
                    // the list only when a blueprint is assigned to that ID.
                    var listProperty = new JProperty("LIST", NewField("list", new JArray(refreshed)));
                    var nextProperty = terminal.Properties()
                        .FirstOrDefault(property =>
                            string.Compare(
                                property.Name,
                                "LIST",
                                StringComparison.OrdinalIgnoreCase) > 0);
                    if (nextProperty == null)
                        terminal.Add(listProperty);
                    else
                        nextProperty.AddBeforeSelf(listProperty);
                }
            }

            return new PaletteRefreshResult(
                definition.PaletteName,
                included,
                added,
                removed,
                updated,
                blueprints.Count(blueprint => !terminals.ContainsKey(blueprint.CategoryId)));
        }

        private static IReadOnlyList<BlueprintDescriptor> LoadBlueprints(
            string moduleRoot,
            PaletteDefinition definition,
            IReadOnlyDictionary<int, string> factionNames)
        {
            var directory = Path.Combine(moduleRoot, definition.ResourceExtension);
            if (!Directory.Exists(directory))
                return Array.Empty<BlueprintDescriptor>();

            var pattern = $"*.{definition.ResourceExtension}.json";
            return Directory.EnumerateFiles(directory, pattern)
                .Where(IsPackableResource)
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .Select(path => LoadBlueprint(path, definition, factionNames))
                .ToList();
        }

        private static BlueprintDescriptor LoadBlueprint(
            string path,
            PaletteDefinition definition,
            IReadOnlyDictionary<int, string> factionNames)
        {
            var blueprint = LoadJsonObject(path);
            var categoryId = ReadRequiredFieldInt(
                blueprint,
                definition.CategoryField,
                path);
            var resRef = Path.GetFileName(path)[..^($".{definition.ResourceExtension}.json".Length)];
            var paletteNode = BuildPaletteNode(
                blueprint,
                definition,
                factionNames,
                resRef,
                path);

            return new BlueprintDescriptor(resRef, categoryId, paletteNode);
        }

        private static JObject BuildPaletteNode(
            JObject blueprint,
            PaletteDefinition definition,
            IReadOnlyDictionary<int, string> factionNames,
            string resRef,
            string path)
        {
            var node = new JObject
            {
                ["__struct_id"] = 0
            };

            if (definition.IsCreature)
            {
                var challengeRating = RequireFieldValue(blueprint, "ChallengeRating", path);
                var factionId = ReadRequiredFieldInt(blueprint, "FactionID", path);
                if (!factionNames.TryGetValue(factionId, out var factionName))
                    throw new InvalidDataException(
                        $"Creature blueprint '{path}' refers to missing faction ID {factionId}.");

                node["CR"] = NewField("float", challengeRating.DeepClone());
                node["FACTION"] = NewField("cexostring", factionName);
            }

            var localizedName = blueprint[definition.NameField] as JObject
                                ?? throw new InvalidDataException(
                                    $"Blueprint '{path}' is missing {definition.NameField}.");
            var nameOverride = ReadLocalizedOverride(localizedName);
            var stringRef = ReadOptionalUInt(localizedName, "id");

            if (!string.IsNullOrWhiteSpace(nameOverride))
            {
                node["NAME"] = NewField("cexostring", nameOverride);
                node["RESREF"] = NewField("resref", resRef);
            }
            else if (stringRef.HasValue)
            {
                // nwn_gff emits fields in label order. Keeping RESREF before STRREF produces the same
                // JSON shape as palettes unpacked after an Aurora Toolset refresh.
                node["RESREF"] = NewField("resref", resRef);
                node["STRREF"] = NewField("dword", stringRef.Value);
            }
            else
            {
                // A malformed empty LocString is still more usable under its resref than as a blank
                // palette row, and mirrors the Toolset's visible fallback for unnamed resources.
                node["NAME"] = NewField("cexostring", resRef);
                node["RESREF"] = NewField("resref", resRef);
            }

            return node;
        }

        private static IReadOnlyDictionary<int, string> LoadFactionNames(string moduleRoot)
        {
            var path = Path.Combine(moduleRoot, "fac", "repute.fac.json");
            if (!File.Exists(path))
                return new Dictionary<int, string>();

            var factionFile = LoadJsonObject(path);
            var factionList = RequireListValue(factionFile, "FactionList", path);
            var results = new Dictionary<int, string>();

            foreach (var token in factionList)
            {
                if (token is not JObject faction)
                    throw new InvalidDataException($"Faction list '{path}' contains a non-struct entry.");

                var id = faction.Value<int?>("__struct_id")
                         ?? throw new InvalidDataException($"Faction list '{path}' contains an entry with no ID.");
                var name = ReadFieldString(faction, "FactionName")
                           ?? throw new InvalidDataException(
                               $"Faction {id} in '{path}' has no FactionName.");
                results[id] = name;
            }

            return results;
        }

        private static void CollectTerminalCategories(
            JArray nodes,
            string paletteName,
            IDictionary<int, JObject> terminals)
        {
            foreach (var token in nodes)
            {
                if (token is not JObject node)
                    throw new InvalidDataException($"Palette '{paletteName}' contains a non-struct node.");

                var id = ReadOptionalFieldInt(node, "ID");
                if (id.HasValue)
                {
                    if (!terminals.TryAdd(id.Value, node))
                        throw new InvalidDataException(
                            $"Palette '{paletteName}' defines category ID {id.Value} more than once.");
                }

                if (node["LIST"] is JObject listField && listField["value"] is JArray children)
                    CollectTerminalCategories(children, paletteName, terminals);
            }
        }

        private static JObject LoadJsonObject(string path)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                var content = bytes.AsSpan();
                if (content.StartsWith(Encoding.UTF8.Preamble))
                    content = content[Encoding.UTF8.Preamble.Length..];

                string json;
                try
                {
                    json = StrictUtf8.GetString(content);
                }
                catch (DecoderFallbackException)
                {
                    // nwn_gff writes text fields as raw Windows-1252, while hand-edited resources
                    // may be real UTF-8. Match the formats library's strict-UTF-8-first convention
                    // so valid UTF-8 is preserved and legacy NWN bytes are decoded without damage.
                    json = NwnText.GetString(content);
                }

                return JObject.Parse(json);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                throw new InvalidDataException($"Could not read NWN JSON resource '{path}'.", ex);
            }
        }

        private static Encoding CreateNwnTextEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }

        private static JArray RequireListValue(JObject owner, string fieldName, string context)
        {
            if (owner[fieldName] is JObject field &&
                string.Equals(field.Value<string>("type"), "list", StringComparison.OrdinalIgnoreCase) &&
                field["value"] is JArray value)
            {
                return value;
            }

            throw new InvalidDataException($"{context} is missing list field {fieldName}.");
        }

        private static JToken RequireFieldValue(JObject owner, string fieldName, string context)
        {
            if (owner[fieldName] is JObject field && field["value"] is { } value)
                return value;

            throw new InvalidDataException($"{context} is missing field {fieldName}.");
        }

        private static int ReadRequiredFieldInt(JObject owner, string fieldName, string context)
        {
            var value = ReadOptionalFieldInt(owner, fieldName);
            return value ?? throw new InvalidDataException($"{context} is missing integer field {fieldName}.");
        }

        private static int? ReadOptionalFieldInt(JObject owner, string fieldName)
        {
            return owner[fieldName] is JObject field
                ? field.Value<int?>("value")
                : null;
        }

        private static string ReadFieldString(JObject owner, string fieldName)
        {
            return owner[fieldName] is JObject field
                ? field.Value<string>("value")
                : null;
        }

        private static uint? ReadOptionalUInt(JObject owner, string fieldName)
        {
            return owner[fieldName]?.Type == JTokenType.Integer
                ? owner.Value<uint?>(fieldName)
                : null;
        }

        private static string ReadLocalizedOverride(JObject localizedString)
        {
            if (localizedString["value"] is not JObject values)
                return null;

            // Aurora running in the project's default English locale chooses language/gender 0.
            // Imported resources occasionally carry only another language, for which the Toolset
            // falls back to the first available override.
            var english = values.Value<string>("0");
            if (!string.IsNullOrWhiteSpace(english))
                return english;

            return values.Properties()
                .Select(property => property.Value.Value<string>())
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        private static JObject NewField(string type, object value)
        {
            return new JObject
            {
                ["type"] = type,
                ["value"] = value is JToken token ? token : JToken.FromObject(value)
            };
        }

        private static bool IsPackableResource(string path)
        {
            return !path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) &&
                   !path.EndsWith(".save-backup", StringComparison.OrdinalIgnoreCase);
        }

        private static StringComparer PathComparer =>
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private sealed record PaletteDefinition(
            string PaletteName,
            string ResourceExtension,
            string CategoryField,
            string NameField,
            bool IsCreature = false);

        private sealed record BlueprintDescriptor(string ResRef, int CategoryId, JObject PaletteNode);
    }

    public sealed record ModulePaletteRefresh(
        IReadOnlyDictionary<string, string> Replacements,
        IReadOnlyList<PaletteRefreshResult> Results);

    public sealed record PaletteRefreshResult(
        string PaletteName,
        int Included,
        int Added,
        int Removed,
        int Updated,
        int MissingCategory);
}
