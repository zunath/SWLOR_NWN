using System.Text.RegularExpressions;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// A resref-keyed index of every place a player can obtain an item, built once per workspace so
    /// the item editor's Source tab can answer "where can a player obtain this item" without
    /// per-item file IO.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This index must never disagree with the game's own obtainability contract:
    /// <c>SWLOR.Game.Server.Tests.Feature.EconomyObtainabilityCoverageTests</c>
    /// (<c>ReadObtainableResrefs</c>). Every regex below is copied verbatim from that method (cited
    /// per group) so the two stay in lockstep; if that test ever grows a new extraction shape, this
    /// index needs the matching addition or a real item will read as "no player source" here while
    /// the game considers it obtainable.
    /// </para>
    /// <para>
    /// Deliberately skipped: area .git placeable/creature instance inventories.
    /// <c>ReadObtainableResrefs</c> does not count them either - a .git instance inherits its
    /// blueprint's default inventory, and an instance-only inventory edit is not an acquisition
    /// shape the contract recognizes - so skipping them here keeps the two in agreement rather
    /// than opening a gap.
    /// </para>
    /// </remarks>
    public sealed class ItemObtainabilityIndex
    {
        private readonly Dictionary<string, List<ItemSourceEntry>> _sourcesByResRef;

        private ItemObtainabilityIndex(Dictionary<string, List<ItemSourceEntry>> sourcesByResRef)
        {
            _sourcesByResRef = sourcesByResRef;
        }

        /// <summary>Number of distinct resrefs with at least one known source.</summary>
        public int ItemsWithSources => _sourcesByResRef.Count;

        /// <summary>Every known source for a resref, case-insensitive. Empty when none are known.</summary>
        public IReadOnlyList<ItemSourceEntry> SourcesFor(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return Array.Empty<ItemSourceEntry>();

            return _sourcesByResRef.TryGetValue(resRef, out var list)
                ? list
                : Array.Empty<ItemSourceEntry>();
        }

        /// <summary>True when at least one source is known for this resref.</summary>
        public bool IsObtainable(string resRef) => SourcesFor(resRef).Count > 0;

        /// <summary>
        /// Builds the index synchronously - callers decide whether to run this on a background
        /// thread. One pass over the module's .utm stores, and (if <paramref name="gameSourceRoot"/>
        /// is a real directory) one pass over every *.cs file under it.
        /// </summary>
        /// <param name="gameSourceRoot">
        /// The SWLOR.Game.Server project directory (the one containing a "Feature" subfolder), or
        /// null/missing to build store-only data.
        /// </param>
        public static ItemObtainabilityIndex Build(
            ModuleWorkspace workspace,
            string? gameSourceRoot,
            IEnumerable<(ResourceType Type, string ResRef, string SourcePath)>? blueprintOverrides = null,
            IEnumerable<string>? additionalLiteralSourcePaths = null)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            var sources = new Dictionary<string, List<ItemSourceEntry>>(StringComparer.OrdinalIgnoreCase);
            var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (blueprintOverrides != null)
            {
                foreach (var (type, resRef, sourcePath) in blueprintOverrides)
                {
                    if (!string.IsNullOrWhiteSpace(resRef) && !string.IsNullOrWhiteSpace(sourcePath))
                        overrides[BlueprintKey(type, resRef)] = sourcePath;
                }
            }

            IndexStores(workspace, sources, overrides);
            IndexCreatureDroppables(workspace, sources, overrides);
            IndexPlacedContainers(workspace, sources, overrides);

            if (!string.IsNullOrWhiteSpace(gameSourceRoot) && Directory.Exists(gameSourceRoot))
                IndexGameCode(gameSourceRoot, sources);

            if (additionalLiteralSourcePaths != null)
            {
                foreach (var sourcePath in additionalLiteralSourcePaths)
                {
                    if (string.IsNullOrWhiteSpace(sourcePath))
                        continue;

                    try
                    {
                        IndexCreateOrCopyLiterals(
                            Path.GetFileName(sourcePath),
                            File.ReadAllText(sourcePath),
                            sources);
                    }
                    catch (Exception)
                    {
                        // A malformed or inaccessible staged source must not hide the other
                        // obtainability sources. Its import validation reports the file error.
                    }
                }
            }

            return new ItemObtainabilityIndex(sources);
        }

        private static void Add(
            Dictionary<string, List<ItemSourceEntry>> sources, string? resRef, ItemSourceEntry entry)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return;

            if (!sources.TryGetValue(resRef, out var list))
            {
                list = new List<ItemSourceEntry>();
                sources[resRef] = list;
            }

            // A resref can be found by more than one scan of the same file (e.g. a recipe file that
            // is also one of the LiteralRegistries below) - records compare structurally, so this
            // keeps the list from publishing the identical fact twice.
            if (!list.Contains(entry))
                list.Add(entry);
        }

        // ---------------------------------------------------------------------------------------
        // (a) Vendor stores - Module\utm\*.utm.json, StoreList -> ItemList -> InventoryRes.
        // Mirrors ReadObtainableResrefs's "Vendor stores (utm)" block, but keeps the owning store's
        // name/resref instead of only recording that *some* store sells the item.
        // ---------------------------------------------------------------------------------------

        private static void IndexStores(
            ModuleWorkspace workspace,
            Dictionary<string, List<ItemSourceEntry>> sources,
            IReadOnlyDictionary<string, string> overrides)
        {
            foreach (var (resRef, sourcePath) in EnumerateBlueprintSources(
                         workspace,
                         ResourceType.Utm,
                         overrides))
            {
                UtmDocument store;
                try
                {
                    store = UtmDocument.Load(sourcePath);
                }
                catch (Exception)
                {
                    // A malformed store must not cost every other store its item index.
                    continue;
                }

                var display = store.LocName.Text;
                if (string.IsNullOrWhiteSpace(display))
                    display = store.ResRef ?? resRef;

                var entry = new ItemSourceEntry(ItemSourceKind.Store, display!, resRef, resRef);

                foreach (var page in store.StoreList)
                {
                    foreach (var item in page.GetListOrEmpty("ItemList"))
                        Add(sources, item.GetStringOrNull("InventoryRes"), entry);
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // (a2) NPC droppables and placed containers. Mirrors ReadObtainableResrefs's "NPC carried
        // droppable items" (.utc ItemList entries with Dropable=1) and "placed treasure containers"
        // (.utp ItemList entries, no Dropable requirement) blocks, parsed with the same
        // System.Text.Json shapes so the two extractions cannot drift.
        // ---------------------------------------------------------------------------------------

        private static void IndexCreatureDroppables(
            ModuleWorkspace workspace,
            Dictionary<string, List<ItemSourceEntry>> sources,
            IReadOnlyDictionary<string, string> overrides)
        {
            ScanInventoryBlueprints(workspace, ResourceType.Utc, (root, resRef) =>
            {
                var display = LocalizedFirst(root, "FirstName") ?? resRef;
                var entry = new ItemSourceEntry(ItemSourceKind.Npc, display, resRef, resRef);

                if (!TryGetArray(root, "ItemList", out var items))
                    return Enumerable.Empty<(string?, ItemSourceEntry)>();

                return items.EnumerateArray()
                    .Where(item => GetInt(item, "Dropable") == 1)
                    .Select(item => (GetString(item, "InventoryRes"), entry));
            }, sources, overrides);
        }

        private static void IndexPlacedContainers(
            ModuleWorkspace workspace,
            Dictionary<string, List<ItemSourceEntry>> sources,
            IReadOnlyDictionary<string, string> overrides)
        {
            ScanInventoryBlueprints(workspace, ResourceType.Utp, (root, resRef) =>
            {
                var display = LocalizedFirst(root, "LocName") ?? resRef;
                var entry = new ItemSourceEntry(ItemSourceKind.Container, display, resRef, resRef);

                if (!TryGetArray(root, "ItemList", out var items))
                    return Enumerable.Empty<(string?, ItemSourceEntry)>();

                return items.EnumerateArray()
                    .Select(item => (GetString(item, "InventoryRes"), entry));
            }, sources, overrides);
        }

        private static void ScanInventoryBlueprints(
            ModuleWorkspace workspace,
            ResourceType type,
            Func<System.Text.Json.JsonElement, string, IEnumerable<(string? ResRef, ItemSourceEntry Entry)>> extract,
            Dictionary<string, List<ItemSourceEntry>> sources,
            IReadOnlyDictionary<string, string> overrides)
        {
            foreach (var (resRef, sourcePath) in EnumerateBlueprintSources(workspace, type, overrides))
            {
                string text;
                try
                {
                    text = File.ReadAllText(sourcePath);
                }
                catch (Exception)
                {
                    continue;
                }

                // Same cheap pre-filter as the coverage test: most blueprints carry no inventory
                // at all, and skipping them before the JSON parse is what keeps a full-corpus
                // sweep affordable.
                if (!text.Contains("InventoryRes"))
                    continue;

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(text);
                    foreach (var (itemResRef, entry) in extract(doc.RootElement, resRef))
                        Add(sources, itemResRef, entry);
                }
                catch (Exception)
                {
                    // A malformed blueprint must not cost every other one its item index.
                }
            }
        }

        private static IEnumerable<(string ResRef, string SourcePath)> EnumerateBlueprintSources(
            ModuleWorkspace workspace,
            ResourceType type,
            IReadOnlyDictionary<string, string> overrides)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var resRef in workspace.EnumerateResRefs(type))
            {
                seen.Add(resRef);
                var key = BlueprintKey(type, resRef);
                yield return (
                    resRef,
                    overrides.TryGetValue(key, out var overridePath)
                        ? overridePath
                        : workspace.GetResourcePath(type, resRef));
            }

            var prefix = ((int)type).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":";
            foreach (var (key, sourcePath) in overrides)
            {
                if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var resRef = key[prefix.Length..];
                if (seen.Add(resRef))
                    yield return (resRef, sourcePath);
            }
        }

        private static string BlueprintKey(ResourceType type, string resRef) =>
            ((int)type).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + resRef;

        private static bool TryGetArray(
            System.Text.Json.JsonElement element, string property, out System.Text.Json.JsonElement array)
        {
            array = default;
            return element.TryGetProperty(property, out var wrapper) &&
                   wrapper.TryGetProperty("value", out array) &&
                   array.ValueKind == System.Text.Json.JsonValueKind.Array;
        }

        private static string? GetString(System.Text.Json.JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var wrapper) &&
                wrapper.TryGetProperty("value", out var value) &&
                value.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }

        private static int GetInt(System.Text.Json.JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var wrapper) &&
                wrapper.TryGetProperty("value", out var value) &&
                value.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                return value.GetInt32();
            }

            return -1;
        }

        /// <summary>The English ("0") entry of a cexolocstring field, or null when absent/blank.</summary>
        private static string? LocalizedFirst(System.Text.Json.JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var wrapper) &&
                wrapper.TryGetProperty("value", out var value) &&
                value.ValueKind == System.Text.Json.JsonValueKind.Object &&
                value.TryGetProperty("0", out var first) &&
                first.ValueKind == System.Text.Json.JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(first.GetString()))
            {
                return first.GetString();
            }

            return null;
        }

        // ---------------------------------------------------------------------------------------
        // (b) SWLOR.Game.Server C# source. Every regex here is the literal pattern from
        // EconomyObtainabilityCoverageTests.ReadObtainableResrefs (SWLOR.Game.Server.Tests\Feature\
        // EconomyObtainabilityCoverageTests.cs), grouped by which acquisition shape it recognizes.
        // ---------------------------------------------------------------------------------------

        // Recipe outputs/components: RecipeBuilder's fluent .Resref(...)/.Component(...) calls,
        // confined (verified against the current tree) to Feature/RecipeDefinition/**. The owning
        // recipe is the nearest preceding "_builder.Create(RecipeType.X, ...)" - these definition
        // files never nest one recipe's Create call inside another's.
        private static readonly Regex RecipeCreateRegex = new(
            @"_builder\.Create\(\s*RecipeType\.(?<recipe>\w+)", RegexOptions.Compiled);

        private static readonly Regex ResrefOrComponentRegex = new(
            @"\.(?:Resref|Component)\(\s*""([^""]+)""", RegexOptions.Compiled);

        // Loot table items: LootTableBuilder's fluent .AddItem(...), confined to
        // Feature/LootTableDefinition/**. Same "nearest preceding Create" ownership rule.
        private static readonly Regex AddItemRegex = new(
            @"\.AddItem\(\s*""([^""]+)""", RegexOptions.Compiled);

        // Quest rewards: QuestBuilder's fluent .AddItemReward(...), confined to
        // Feature/QuestDefinition/**. Quest ids are either a string literal or a same-file const
        // identifier - same shape SourceIdScanner.CreateCallRegex/ConstStringRegex already resolve
        // for quest/spawn-table ids (SWLOR.Toolset.Domain\GameData\GameCode\SourceIdScanner.cs).
        private static readonly Regex CreateIdRegex = new(
            @"(?:builder|_builder)\.Create\(\s*(?:""(?<literal>(?:[^""\\]|\\.)*)""|(?<identifier>[A-Za-z_]\w*))",
            RegexOptions.Compiled);

        private static readonly Regex ConstStringRegex = new(
            @"const\s+string\s+(?<name>[A-Za-z_]\w*)\s*=\s*""(?<value>(?:[^""\\]|\\.)*)""\s*;",
            RegexOptions.Compiled);

        private static readonly Regex AddItemRewardRegex = new(
            @"\.AddItemReward\(\s*""([^""]+)""", RegexOptions.Compiled);

        // Dead-but-mirrored shapes: present in ReadObtainableResrefs's literal-pattern list but,
        // as of this writing, matching nothing in the current tree. Kept so a future addition using
        // one of these call shapes is picked up automatically instead of silently disagreeing with
        // the coverage test.
        private static readonly Regex GenericItemRewardRegex = new(
            @"new\s+ItemReward\(\s*""([^""]+)""|\.RewardItem\(\s*""([^""]+)""", RegexOptions.Compiled);

        private static readonly Regex RefinedItemResrefRegex = new(
            @"RefinedItemResref\s*=\s*""([^""]+)""", RegexOptions.Compiled);

        private static readonly Regex TerminalItemRegex = new(
            @"new\s+TerminalItem\([^,]+,\s*""([^""]+)""", RegexOptions.Compiled);

        // Slicing lockbox/terminal rewards: SlicingRewardCatalog.cs's Named/Schematic/Note/Tool
        // calls. A real, populated source (not dead) - the fourth argument, when present, is the
        // reward's own display name.
        private static readonly Regex SlicingRewardRegex = new(
            @"(?:Named|Schematic|Note|Tool)\(\s*SlicingSourceType\.(?<source>\w+)\s*,\s*\d+\s*,\s*" +
            @"""(?<resref>[^""]+)""(?:\s*,\s*""(?<name>(?:[^""\\]|\\.)*)"")?",
            RegexOptions.Compiled);

        // Direct item grants: CreateItemOnObject/CopyItemAndModify string literals, anywhere in the
        // tree. Per the coverage test these are simply "obtainable" with no further classification -
        // here they map to Other, named after their source file.
        private static readonly Regex CreateOrCopyLiteralRegex = new(
            @"CreateItemOnObject\(\s*""([^""]+)""|CopyItemAndModify\(\s*""([^""]+)""", RegexOptions.Compiled);

        // Files whose every bare 2-16 char lowercase alnum/underscore string literal is an item
        // resref (attribute-decorated registries) - same file list and same bare-literal regex as
        // ReadObtainableResrefs's "literalRegistries" block.
        private static readonly Regex BareLiteralRegex = new(@"""([a-z0-9_]{2,16})""", RegexOptions.Compiled);

        private static readonly (string FileName, ItemSourceKind Kind)[] LiteralRegistries =
        {
            ("FishType.cs", ItemSourceKind.Loot),
            ("FishingRodType.cs", ItemSourceKind.Loot),
            ("FishingBaitType.cs", ItemSourceKind.Loot),
            ("SlicingCacheSmitheryRecipes.cs", ItemSourceKind.Recipe),
            ("SlicingCacheCookingRecipes.cs", ItemSourceKind.Recipe),
            ("TraceFuseRecipes.cs", ItemSourceKind.Recipe),
            ("SlicingTerminalFurnitureRecipes.cs", ItemSourceKind.Recipe),
            ("ConcentratedVenomRecipes.cs", ItemSourceKind.Recipe),
        };

        // Fixed resref constants that are granted directly, same list as ReadObtainableResrefs.
        private static readonly string[] FixedGrantedResRefs =
        {
            "beast_dna", "beast_egg", "blueprint", "survival_knife",
            "fresh_bread", "dlarproto", "travelers_clothes",
            "ls_custom", "ss_custom",
        };

        private static readonly ItemSourceEntry FixedGrantEntry =
            new(ItemSourceKind.Other, "Granted directly (fixed system item)", null, null);

        private static void IndexGameCode(
            string gameSourceRoot, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            foreach (var resRef in FixedGrantedResRefs)
                Add(sources, resRef, FixedGrantEntry);

            List<string> files;
            try
            {
                files = Directory.EnumerateFiles(gameSourceRoot, "*.cs", SearchOption.AllDirectories).ToList();
            }
            catch (Exception)
            {
                return;
            }

            foreach (var file in files)
            {
                var normalized = file.Replace('\\', '/');

                // Skip a nested worktree copy found *within* the scanned tree - e.g. another
                // worktree's SWLOR.Game.Server accidentally checked out under this one. Computed
                // relative to gameSourceRoot (not the absolute path) so this guard does not skip
                // every file when gameSourceRoot itself lives under ".claude/worktrees/...", which
                // is the common case for a worktree-hosted build (mirrors
                // EconomyObtainabilityCoverageTests's identical relative-path guard).
                var relativeToRoot = Path.GetRelativePath(gameSourceRoot, file).Replace('\\', '/');
                if (relativeToRoot.Contains(".claude/worktrees/"))
                    continue;

                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception)
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);

                if (normalized.Contains("/Feature/RecipeDefinition/"))
                    IndexRecipeFile(fileName, text, sources);
                else if (normalized.Contains("/Feature/QuestDefinition/"))
                    IndexQuestFile(fileName, text, sources);
                else if (normalized.Contains("/Feature/LootTableDefinition/"))
                    IndexLootFile(fileName, text, sources);

                IndexCreateOrCopyLiterals(fileName, text, sources);
                IndexGenericItemRewardLiterals(fileName, text, sources);
                IndexRefinedItemResref(fileName, text, sources);
                IndexTerminalItem(fileName, text, sources);

                if (fileName.Equals("SlicingRewardCatalog.cs", StringComparison.Ordinal))
                    IndexSlicingRewards(fileName, text, sources);

                foreach (var registry in LiteralRegistries)
                {
                    if (fileName.Equals(registry.FileName, StringComparison.Ordinal))
                        IndexLiteralRegistry(fileName, text, registry.Kind, sources);
                }
            }
        }

        private static void IndexRecipeFile(
            string fileName, string text, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            var blocks = OwnerBlocks(RecipeCreateRegex.Matches(text), m => m.Groups["recipe"].Value);
            if (blocks.Count == 0)
                return;

            foreach (Match match in ResrefOrComponentRegex.Matches(text))
            {
                var recipe = FindOwner(blocks, match.Index);
                if (recipe == null)
                    continue;

                Add(sources, match.Groups[1].Value,
                    new ItemSourceEntry(ItemSourceKind.Recipe, recipe, fileName, null));
            }
        }

        private static void IndexQuestFile(
            string fileName, string text, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            var constants = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match constMatch in ConstStringRegex.Matches(text))
                constants[constMatch.Groups["name"].Value] = constMatch.Groups["value"].Value;

            var blocks = new List<(int Index, string Id)>();
            foreach (Match match in CreateIdRegex.Matches(text))
            {
                var literal = match.Groups["literal"];
                if (literal.Success)
                {
                    blocks.Add((match.Index, literal.Value));
                    continue;
                }

                var identifier = match.Groups["identifier"];
                if (identifier.Success && constants.TryGetValue(identifier.Value, out var resolved))
                    blocks.Add((match.Index, resolved));
            }

            if (blocks.Count == 0)
                return;

            foreach (Match match in AddItemRewardRegex.Matches(text))
            {
                var questId = FindOwner(blocks, match.Index);
                if (questId == null)
                    continue;

                Add(sources, match.Groups[1].Value,
                    new ItemSourceEntry(ItemSourceKind.Quest, questId, fileName, null));
            }
        }

        private static void IndexLootFile(
            string fileName, string text, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            // Loot table ids are always string literals in this codebase - no const-resolution needed.
            var blocks = new List<(int Index, string Id)>();
            foreach (Match match in CreateIdRegex.Matches(text))
            {
                var literal = match.Groups["literal"];
                if (literal.Success)
                    blocks.Add((match.Index, literal.Value));
            }

            if (blocks.Count == 0)
                return;

            foreach (Match match in AddItemRegex.Matches(text))
            {
                var lootTableId = FindOwner(blocks, match.Index);
                if (lootTableId == null)
                    continue;

                Add(sources, match.Groups[1].Value,
                    new ItemSourceEntry(ItemSourceKind.Loot, lootTableId, fileName, null));
            }
        }

        private static void IndexSlicingRewards(
            string fileName, string text, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            foreach (Match match in SlicingRewardRegex.Matches(text))
            {
                var source = match.Groups["source"].Value;
                var name = match.Groups["name"].Success ? match.Groups["name"].Value : null;

                Add(sources, match.Groups["resref"].Value,
                    new ItemSourceEntry(ItemSourceKind.Loot, $"Slicing reward ({source})", name ?? fileName, null));
            }
        }

        private static void IndexCreateOrCopyLiterals(
            string fileName, string text, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            foreach (Match match in CreateOrCopyLiteralRegex.Matches(text))
            {
                var group = match.Groups[1].Success ? match.Groups[1] : match.Groups[2];
                if (!group.Success)
                    continue;

                Add(sources, group.Value, new ItemSourceEntry(ItemSourceKind.Other, fileName, null, null));
            }
        }

        private static void IndexGenericItemRewardLiterals(
            string fileName, string text, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            foreach (Match match in GenericItemRewardRegex.Matches(text))
            {
                var group = match.Groups[1].Success ? match.Groups[1] : match.Groups[2];
                if (!group.Success)
                    continue;

                Add(sources, group.Value, new ItemSourceEntry(ItemSourceKind.Quest, fileName, null, null));
            }
        }

        private static void IndexRefinedItemResref(
            string fileName, string text, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            foreach (Match match in RefinedItemResrefRegex.Matches(text))
                Add(sources, match.Groups[1].Value,
                    new ItemSourceEntry(ItemSourceKind.Recipe, "Refining", fileName, null));
        }

        private static void IndexTerminalItem(
            string fileName, string text, Dictionary<string, List<ItemSourceEntry>> sources)
        {
            foreach (Match match in TerminalItemRegex.Matches(text))
                Add(sources, match.Groups[1].Value,
                    new ItemSourceEntry(ItemSourceKind.Store, "Training Store", fileName, null));
        }

        private static void IndexLiteralRegistry(
            string fileName,
            string text,
            ItemSourceKind kind,
            Dictionary<string, List<ItemSourceEntry>> sources)
        {
            var display = Path.GetFileNameWithoutExtension(fileName);
            foreach (Match match in BareLiteralRegex.Matches(text))
                Add(sources, match.Groups[1].Value, new ItemSourceEntry(kind, display, fileName, null));
        }

        private static List<(int Index, string Id)> OwnerBlocks(MatchCollection matches, Func<Match, string> idSelector)
        {
            var blocks = new List<(int Index, string Id)>(matches.Count);
            foreach (Match match in matches)
                blocks.Add((match.Index, idSelector(match)));
            return blocks;
        }

        /// <summary>
        /// The id of the last block starting at or before <paramref name="position"/> - i.e. the
        /// Create(...) call that textually owns this line, since these definition files never nest
        /// one Create block inside another.
        /// </summary>
        private static string? FindOwner(List<(int Index, string Id)> blocks, int position)
        {
            string? owner = null;
            foreach (var (index, id) in blocks)
            {
                if (index > position)
                    break;

                owner = id;
            }

            return owner;
        }
    }
}
