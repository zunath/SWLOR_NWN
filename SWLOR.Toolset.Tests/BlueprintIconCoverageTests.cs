using FluentAssertions;
using NUnit.Framework;
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
    /// The floors asserted for placeables and doors are the measured coverage at the time this was
    /// written, rounded down. They are regression guards, not targets: several thousand of the module's
    /// placeables point at appearance rows that are blank in placeables.2da and legitimately have no
    /// model to preview, so the interesting failure is coverage dropping, not coverage being short of
    /// 100%.
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
        public void Every_Creature_Blueprint_Resolves_A_Portrait_Or_A_Model()
        {
            var unresolved = new List<string>();

            foreach (var resRef in Data.Workspace.EnumerateResRefs(ResourceType.Utc))
            {
                var root = Data.Workspace.LoadBlueprint(ResourceType.Utc, resRef).Fields;

                if (ResolvesPortrait(root) || ResolvesModel(ResourceType.Utc, root))
                    continue;

                unresolved.Add(resRef);
            }

            unresolved.Should().BeEmpty(
                because: "creatures preview as their portrait, falling back to their model; " +
                         $"unresolved: {string.Join(", ", unresolved.Take(10))}");
        }

        [Test]
        public void Placeable_Model_Coverage_Does_Not_Regress()
        {
            var resolved = Data.Workspace.EnumerateResRefs(ResourceType.Utp)
                .Count(resRef => ResolvesModel(
                    ResourceType.Utp, Data.Workspace.LoadBlueprint(ResourceType.Utp, resRef).Fields));

            resolved.Should().BeGreaterThanOrEqualTo(5000,
                because: "5,357 placeables resolved a model when this was measured; the rest point at " +
                         "blank placeables.2da rows and fall back to the type symbol");
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

            var variants = PortraitService.GetTgaVariants(row.BaseResRef);
            return Data.HasTexture(variants.Medium) || Data.HasTexture(variants.Large) ||
                   Data.HasTexture(variants.Small) || Data.HasTexture(variants.Huge);
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
