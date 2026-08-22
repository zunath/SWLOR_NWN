using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Runs the palette's preview resolution over the whole module against the real 2DAs and the real
    /// layered resource index, so a naming rule that stops matching the shipped artwork fails here rather
    /// than showing up as a grid of empty tiles.
    /// </summary>
    /// <remarks>
    /// Door coverage remains a measured regression floor because generic tileset doors legitimately
    /// have no standalone model. Module-owned creature and placeable appearances are held to a stricter
    /// standard: every requested model part must exist in the active base-game + HAK resource stack.
    /// <para>
    /// Note that a blueprint's appearance indexes placeables.2da by row <i>position</i>, not by the
    /// value printed in its first column - the two disagree in this corpus, where the labels run to
    /// 32189 across 32,090 rows. <see cref="Domain.GameData.TwoDa.TwoDaTable"/> reads positionally and
    /// so does the game; anything that reads the label column instead lands on the wrong artwork.
    /// </para>
    /// </remarks>
    [TestFixture]
    [Category("Corpus")]
    public class BlueprintIconCoverageTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                        return current.FullName;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        private static readonly Lazy<Fixture> Shared = new(Fixture.Build, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// The game-data stack these tests share. Built once: scanning 113 hak layers and the base-game
        /// KEY/BIF is several seconds, and every test here wants the same view of it.
        /// </summary>
        private sealed class Fixture
        {
            public required ResourceIndex Index { get; init; }
            public required ModuleWorkspace Workspace { get; init; }
            public required TwoDaService TwoDa { get; init; }
            public required BaseItemIconService BaseItems { get; init; }
            public required PortraitService Portraits { get; init; }
            public required AppearanceService Appearances { get; init; }
            public required PlaceableAppearanceService Placeables { get; init; }
            public required DoorTypeService Doors { get; init; }

            public static Fixture Build()
            {
                var twoDa = new TwoDaService(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_2da"));
                var tlk = TlkService.Load(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json"));

                KeyBifCatalog? baseLayer = null;
                var install = NwnInstallLocator.Locate(null);
                if (install != null)
                    baseLayer = KeyBifCatalog.Load(Path.Combine(install, "data"));

                var index = ResourceIndex.FromHakBuilderConfig(
                    Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                    Path.Combine(RepoRoot, "SWLOR_Haks"),
                    baseLayer);
                index.EnsureInitialized();

                return new Fixture
                {
                    Index = index,
                    Workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory, index),
                    TwoDa = twoDa,
                    BaseItems = new BaseItemIconService(twoDa),
                    Portraits = new PortraitService(twoDa),
                    Appearances = new AppearanceService(twoDa, tlk),
                    Placeables = new PlaceableAppearanceService(twoDa, tlk),
                    Doors = new DoorTypeService(twoDa, tlk)
                };
            }

            public bool HasTexture(string? resRef) =>
                !string.IsNullOrWhiteSpace(resRef) &&
                new[] { "tga", "dds", "plt" }.Any(extension =>
                    Index.TryLookup(
                        new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension(extension)), out _));

            public bool HasModel(string? resRef) =>
                !string.IsNullOrWhiteSpace(resRef) &&
                Index.TryLookup(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("mdl")), out _);
        }

        private static Fixture Data => Shared.Value;

        [Test]
        public void Every_Item_Blueprint_Resolves_An_Inventory_Icon()
        {
            var unresolved = new List<string>();
            var resRefs = Data.Workspace.EnumerateResRefs(ResourceType.Uti);

            foreach (var resRef in resRefs)
            {
                var root = Data.Workspace.LoadBlueprint(ResourceType.Uti, resRef).Fields;
                var resolved = ItemIconResolver
                    .Resolve(root, Data.BaseItems.GetOrNull)
                    .Any(stack => stack.Layers.Any(Data.HasTexture));

                if (!resolved)
                    unresolved.Add(resRef);
            }

            resRefs.Should().NotBeEmpty(because: "the module corpus should have item blueprints");
            unresolved.Should().BeEmpty(
                because: "every item's icon resolves through ItemClass or the base item's DefaultIcon; " +
                         $"unresolved: {string.Join(", ", unresolved.Take(10))}");
        }

        [Test]
        public void Every_Creature_Blueprint_Resolves_All_Requested_Model_Parts()
        {
            var unresolved = new List<string>();

            foreach (var resRef in Data.Workspace.EnumerateResRefs(ResourceType.Utc))
            {
                var root = Data.Workspace.LoadBlueprint(ResourceType.Utc, resRef).Fields;
                if (UsesIntentionalNullCreatureAppearance(root))
                    continue;

                var failure = ModelFailure(ResourceType.Utc, root, LoadItemBlueprint);
                if (failure != null)
                    unresolved.Add($"{resRef}: {failure}");
            }

            unresolved.Should().BeEmpty(
                because: "a portrait must not conceal a broken creature appearance; " +
                         $"unresolved: {string.Join(", ", unresolved.Take(10))}");
        }

        private static bool UsesIntentionalNullCreatureAppearance(Domain.Gff.JsonGffStruct root)
        {
            var appearanceId = root.GetIntOrNull("Appearance_Type");
            if (appearanceId is not >= 0)
                return false;

            var table = Data.TwoDa.GetTable("appearance");
            var label = table.GetString(appearanceId.Value, "LABEL");
            var model = table.GetString(appearanceId.Value, "RACE");
            return !TwoDaChoicePolicy.IsSelectableLabel(label) &&
                   string.Equals(model, "c_invsguy", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Every module-owned placeable must be renderable. Placeholder rows and references to absent
        /// MDLs produce invisible palette entries or area objects and are not valid appearances.
        /// </summary>
        /// <remarks>
        /// The appearance is indexed by physical 2DA row and the row's model must also resolve through
        /// the configured resource layers; a nonblank sentinel such as USER is therefore rejected.
        /// </remarks>
        [Test]
        public void Every_Placeable_Blueprint_Resolves_Its_Model()
        {
            var resRefs = Data.Workspace.EnumerateResRefs(ResourceType.Utp).ToList();
            var unresolved = resRefs
                .Select(resRef => (ResRef: resRef, Failure: ModelFailure(
                    ResourceType.Utp, Data.Workspace.LoadBlueprint(ResourceType.Utp, resRef).Fields)))
                .Where(result => result.Failure != null)
                .Select(result => $"{result.ResRef}: {result.Failure}")
                .ToList();

            resRefs.Should().NotBeEmpty();
            unresolved.Should().BeEmpty(
                because: $"every module placeable must render; unresolved: {string.Join(", ", unresolved.Take(10))}");
        }

        [Test]
        public void Placed_Creatures_Placeables_And_Items_Resolve_Their_Appearances()
        {
            var failures = new List<string>();

            foreach (var path in Directory.EnumerateFiles(
                         Path.Combine(CorpusLocator.ModuleDirectory, "git"), "*.git.json"))
            {
                var root = JsonGffDocument.Load(path).Root;
                var fileName = Path.GetFileName(path);

                CheckPlacedModels(root, "Creature List", ResourceType.Utc, fileName, failures);
                CheckPlacedModels(root, "Placeable List", ResourceType.Utp, fileName, failures);

                foreach (var item in EnumerateStructs(root).Where(candidate =>
                             candidate.Contains("BaseItem") && candidate.Contains("TemplateResRef")))
                {
                    var failure = ItemFailure(item);
                    if (failure != null)
                        failures.Add($"{fileName} item {item.GetStringOrNull("TemplateResRef")}: {failure}");
                }
            }

            failures.Should().BeEmpty(
                because: $"placed and embedded module assets must render; failures: {string.Join(", ", failures.Take(20))}");
        }

        [Test]
        public void Module_Visual_References_Use_Valid_2Da_Rows_And_Artwork()
        {
            var failures = new List<string>();
            var tailModels = Data.TwoDa.GetTable("tailmodel");
            var wingModels = Data.TwoDa.GetTable("wingmodel");
            var loadScreens = Data.TwoDa.GetTable("loadscreens");

            foreach (var directoryName in new[] { "are", "git", "utc", "utp" })
            {
                var directory = Path.Combine(CorpusLocator.ModuleDirectory, directoryName);
                foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
                {
                    var fileName = Path.GetFileName(path);
                    foreach (var item in EnumerateStructs(JsonGffDocument.Load(path).Root))
                    {
                        var portraitId = item.GetIntOrNull("PortraitId") ?? 0;
                        if (portraitId > 0 && !ResolvesPortrait(item))
                            failures.Add($"{fileName}: portrait {portraitId} has no artwork");

                        CheckOptionalModelRow(item, "Tail_New", tailModels, fileName, failures);
                        CheckOptionalModelRow(item, "Wings_New", wingModels, fileName, failures);

                        var loadScreenId = item.GetIntOrNull("LoadScreenID") ?? 0;
                        if (loadScreenId > 1)
                        {
                            var bitmap = loadScreens.GetString(loadScreenId, "BMPResRef");
                            if (bitmap == null || !Data.HasTexture(bitmap))
                                failures.Add($"{fileName}: load screen {loadScreenId} has no artwork");
                        }
                    }
                }
            }

            failures.Should().BeEmpty(
                because: $"optional visual references must resolve; failures: {string.Join(", ", failures.Take(20))}");
        }

        [Test]
        public void Composite_Item_Blueprints_Resolve_Every_Model_Part()
        {
            var failures = new List<string>();

            foreach (var resRef in Data.Workspace.EnumerateResRefs(ResourceType.Uti))
            {
                var failure = ItemFailure(Data.Workspace.LoadBlueprint(ResourceType.Uti, resRef).Fields);
                if (failure != null)
                    failures.Add($"{resRef}: {failure}");
            }

            failures.Should().BeEmpty(
                because: $"item base types and composite model parts must resolve; failures: {string.Join(", ", failures.Take(20))}");
        }

        [Test]
        public void Door_Model_Coverage_Does_Not_Regress()
        {
            var resolved = Data.Workspace.EnumerateResRefs(ResourceType.Utd)
                .Count(resRef => ResolvesModel(
                    ResourceType.Utd, Data.Workspace.LoadBlueprint(ResourceType.Utd, resRef).Fields));

            resolved.Should().BeGreaterThanOrEqualTo(115,
                because: "121 of the 129 doors resolved a model when this was measured");
        }

        [Test]
        public void Base_Items_Are_Read_With_Their_Icon_Naming_Columns()
        {
            // Row 21 is the belt: a simple (ModelType 0) item whose icons are iit_belt_###.
            var belt = Data.BaseItems.GetOrNull(21);

            belt.Should().NotBeNull();
            belt!.ModelType.Should().Be(0);
            belt.ItemClass.Should().Be("it_belt");
        }

        [Test]
        public void A_Reserved_Base_Item_Row_Resolves_To_Nothing()
        {
            Data.BaseItems.GetOrNull(int.MaxValue).Should().BeNull();
        }

        private static bool ResolvesPortrait(Domain.Gff.JsonGffStruct root)
        {
            if (!root.TryGet("PortraitId", out var field))
                return false;

            var portraitId = (int)field.GetInteger();
            if (portraitId is <= 0 or >= ushort.MaxValue)
                return false;

            var row = Data.Portraits.GetAll().FirstOrDefault(candidate => candidate.Id == portraitId);
            if (row == null)
                return false;

            // Plot portraits are official campaign/NPC assets. The engine resolves them by row even
            // when a particular local EE installation does not expose its campaign archive through
            // the data KEY/BIF stack scanned by the toolset. Custom non-plot portraits still have to
            // ship accessible artwork in the active HAK layers.
            if (row.IsPlot)
                return true;

            var variants = PortraitService.GetTgaVariants(row.BaseResRef);
            return Data.HasTexture(variants.Medium) || Data.HasTexture(variants.Large) ||
                   Data.HasTexture(variants.Small) || Data.HasTexture(variants.Huge);
        }

        private static void CheckPlacedModels(
            JsonGffStruct root,
            string listName,
            ResourceType type,
            string fileName,
            ICollection<string> failures)
        {
            var elements = root.GetOrNull(listName)?.Elements;
            if (elements == null)
                return;

            for (var index = 0; index < elements.Count; index++)
            {
                var element = elements[index];
                JsonGffStruct? LoadPlacedItem(string resRef) =>
                    element.GetOrNull("Equip_ItemList")?.Elements?
                        .FirstOrDefault(item => string.Equals(
                            item.GetStringOrNull("TemplateResRef"), resRef,
                            StringComparison.OrdinalIgnoreCase))
                    ?? LoadItemBlueprint(resRef);

                var failure = ModelFailure(
                    type, element, type == ResourceType.Utc ? LoadPlacedItem : null);
                if (failure != null)
                {
                    failures.Add(
                        $"{fileName} {listName}[{index}] ({element.GetStringOrNull("TemplateResRef")}): {failure}");
                }
            }
        }

        private static string? ModelFailure(
            ResourceType type,
            JsonGffStruct root,
            Func<string, JsonGffStruct?>? itemBlueprintLoader = null)
        {
            var reference = BlueprintModelResolver.Resolve(
                type,
                root,
                Data.Appearances,
                Data.Placeables,
                Data.Doors,
                itemBlueprintLoader,
                baseItems: Data.BaseItems.GetOrNull);

            if (reference.Kind == BlueprintModelKind.None &&
                type == ResourceType.Utc &&
                reference.Status.EndsWith("segmented creature has no body parts.", StringComparison.Ordinal))
            {
                return null;
            }

            if (reference.Kind == BlueprintModelKind.None)
                return reference.Status;

            if (reference.Kind == BlueprintModelKind.Simple)
            {
                return Data.HasModel(reference.ModelResRef)
                    ? null
                    : $"missing model {reference.ModelResRef}.mdl";
            }

            if (reference.Kind == BlueprintModelKind.Segmented &&
                !Data.HasModel(reference.SkeletonResRef))
            {
                return $"missing skeleton {reference.SkeletonResRef}.mdl";
            }

            var missing = reference.Parts
                .Where(part => !Data.HasModel(part.ModelResRef))
                .Select(part => $"{part.PartType}={part.ModelResRef}.mdl")
                .ToList();
            return missing.Count == 0 ? null : $"missing {string.Join(", ", missing)}";
        }

        private static string? ItemFailure(JsonGffStruct item)
        {
            var baseItem = item.GetIntOrNull("BaseItem") ?? -1;
            var row = baseItem < 0 ? null : Data.BaseItems.GetOrNull(baseItem);
            if (row == null)
                return $"base item {baseItem} is reserved or missing";
            if (row.ModelType != 2)
                return null;

            var reference = BlueprintModelResolver.Resolve(
                ResourceType.Uti,
                item,
                Data.Appearances,
                Data.Placeables,
                Data.Doors,
                baseItems: Data.BaseItems.GetOrNull);
            if (!reference.Parts.Any(part => Data.HasModel(part.ModelResRef)))
                return null;

            var missing = reference.Parts
                .Where(part => !Data.HasModel(part.ModelResRef))
                .Select(part => part.ModelResRef + ".mdl")
                .ToList();
            return missing.Count == 0 ? null : $"missing {string.Join(", ", missing)}";
        }

        private static JsonGffStruct? LoadItemBlueprint(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return null;

            try
            {
                return Data.Workspace.LoadBlueprint(ResourceType.Uti, resRef).Fields;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private static IEnumerable<JsonGffStruct> EnumerateStructs(JsonGffStruct root)
        {
            yield return root;

            foreach (var (_, field) in root.Entries)
            {
                if (field.Struct != null)
                {
                    foreach (var nested in EnumerateStructs(field.Struct))
                        yield return nested;
                }

                if (field.Elements == null)
                    continue;

                foreach (var element in field.Elements)
                foreach (var nested in EnumerateStructs(element))
                    yield return nested;
            }
        }

        private static void CheckOptionalModelRow(
            JsonGffStruct item,
            string fieldName,
            TwoDaTable table,
            string fileName,
            ICollection<string> failures)
        {
            var rowId = item.GetIntOrNull(fieldName) ?? 0;
            if (rowId <= 0)
                return;

            var model = table.GetString(rowId, "MODEL");
            if (model == null || !Data.HasModel(model))
                failures.Add($"{fileName}: {fieldName} row {rowId} has no model");
        }

        /// <summary>
        /// Resrefs of one type that exist only in the base game or a hak - what the palette's Standard
        /// group lists, and what the module's own enumeration never sees.
        /// </summary>
        private static IReadOnlyList<string> IndexOnlyResRefs(ResourceType type)
        {
            var inModule = Data.Workspace.EnumerateResRefs(type).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return Data.Index
                .EnumerateResources(ResourceIdentity.TypeFromExtension(type.Extension()))
                .Select(identity => identity.ResRef)
                .Where(resRef => !inModule.Contains(resRef))
                .ToList();
        }

        /// <summary>
        /// The base game's doors declare their type in the byte-sized <c>GenericType</c>; only blueprints
        /// the module authored use the wider <c>GenericType_New</c>. Reading just the latter resolved a
        /// model for none of the 86, so every Standard door in the palette showed the door type-symbol.
        /// </summary>
        /// <remarks>
        /// The floor is 40, not 86: 44 of them point at a doortypes.2da row whose Model is blank - a
        /// "Generic" door takes its tileset's own door at placement time and has no model of its own -
        /// and 2 name a row the table does not have. Those legitimately preview as the type symbol.
        /// </remarks>
        [Test]
        public void Base_Game_Doors_Resolve_Their_Model()
        {
            var indexOnly = IndexOnlyResRefs(ResourceType.Utd);
            indexOnly.Should().NotBeEmpty(because: "the base game ships doors the module does not override");

            var resolved = indexOnly.Count(resRef => ResolvesModel(
                ResourceType.Utd, Data.Workspace.LoadBlueprint(ResourceType.Utd, resRef).Fields));

            resolved.Should().BeGreaterThanOrEqualTo(40,
                because: "40 of the 86 base-game doors name a doortypes.2da row that has a model");
        }

        /// <summary>
        /// Creatures and placeables outside the module have to preview too - they are most of what the
        /// Standard palette lists.
        /// </summary>
        [TestCase(ResourceType.Utc, 1500)]
        [TestCase(ResourceType.Utp, 990)]
        public void Base_Game_Blueprints_Resolve_Their_Model(ResourceType type, int floor)
        {
            var resolved = IndexOnlyResRefs(type)
                .Count(resRef => ResolvesModel(type, Data.Workspace.LoadBlueprint(type, resRef).Fields));

            resolved.Should().BeGreaterThanOrEqualTo(floor,
                because: $"measured {type} coverage outside the module was above this when written");
        }

        private static bool ResolvesModel(ResourceType type, Domain.Gff.JsonGffStruct root)
        {
            var reference = BlueprintModelResolver.Resolve(
                type, root, Data.Appearances, Data.Placeables, Data.Doors);

            return reference.Kind switch
            {
                BlueprintModelKind.Simple => Data.HasModel(reference.ModelResRef),
                BlueprintModelKind.Segmented => reference.Parts.Any(part => Data.HasModel(part.ModelResRef)),
                _ => false
            };
        }
    }
}
