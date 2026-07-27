using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Covers the palette's Standard (base-game) half: that the standard palettes are read from the base
    /// game rather than from a hak, that the binary GFF the game ships imports into a real category tree,
    /// that a base-game-only blueprint loads through the workspace, and that none of it can reach the
    /// category sidecar.
    /// </summary>
    /// <remarks>
    /// Needs an NWN:EE install (for the base KEY/BIF layer and base dialog.tlk) and the repository layout.
    /// SWLOR_Haks is optional here: without it the index is base-game-only, which is enough for everything
    /// asserted below except the hak-override check, which says so itself.
    /// </remarks>
    [TestFixture]
    [Category("Corpus")]
    public class StandardPaletteTests
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

        private static Fixture Data => Shared.Value;

        /// <summary>
        /// The layered resource stack, built once - scanning the base KEY/BIF plus the hak layers is
        /// several seconds and every test here wants the same view of it.
        /// </summary>
        private sealed class Fixture
        {
            public required ResourceIndex Index { get; init; }
            public required TlkService Tlk { get; init; }
            public required ModuleWorkspace Workspace { get; init; }

            /// <summary>True when SWLOR's palette hak is actually checked out, so hak precedence is live.</summary>
            public required bool HasPaletteHak { get; init; }

            public static Fixture Build()
            {
                var installPath = NwnInstallLocator.Locate(null)
                    ?? throw new DirectoryNotFoundException(
                        "No NWN:EE install was found; the standard palettes only exist in the base game.");

                var baseLayer = KeyBifCatalog.Load(Path.Combine(installPath, "data"));
                var index = ResourceIndex.FromHakBuilderConfig(
                    Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                    Path.Combine(RepoRoot, "SWLOR_Haks"),
                    baseLayer);
                index.EnsureInitialized();

                // The base-game category names are base dialog.tlk strrefs, which is the half that matters
                // here. The custom TLK is only present when the haks submodule is checked out, so an empty
                // one stands in for it rather than making these tests depend on the submodule.
                var swTlkPath = Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json");
                var baseTlkPath = Path.Combine(installPath, "lang", "en", "data", "dialog.tlk");
                var tlk = File.Exists(swTlkPath)
                    ? TlkService.Load(swTlkPath, baseTlkPath)
                    : new TlkService(
                        TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}"),
                        SWLOR.NWN.Formats.Tlk.TlkReader.Read(baseTlkPath));

                return new Fixture
                {
                    Index = index,
                    Tlk = tlk,
                    Workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory, index),
                    HasPaletteHak = Directory.Exists(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_palette"))
                };
            }

            public StandardPalette Load(ResourceType type) =>
                StandardPaletteLoader.Load(Index, type, Tlk.GetString);
        }

        /// <summary>
        /// The regression guard for the trap that motivates the "std" suffix: the bare names are overridden
        /// by SWLOR's own haks, so a standard palette read from one of those would show custom content.
        /// </summary>
        [Test]
        public void Every_Standard_Palette_Resolves_From_The_Base_Game()
        {
            foreach (var type in ModuleWorkspace.BlueprintTypes)
            {
                var resRef = StandardPaletteLoader.PaletteResRefFor(type);
                resRef.Should().NotBeNull(because: $"{type} is a blueprint type and has a standard palette");

                var identity = new ResourceIdentity(resRef!, ResourceIdentity.TypeFromExtension("itp"));
                Data.Index.TryLookup(identity, out var handle)
                    .Should().BeTrue(because: $"'{resRef}.itp' ships with the base game");

                handle.Provenance.Kind.Should().Be(
                    ResourceLayerKind.BaseGame,
                    because: $"'{resRef}.itp' must come from the base game, not from a hak");
            }
        }

        [Test]
        public void The_Bare_Palette_Names_Are_Overridden_By_A_Hak()
        {
            if (!Data.HasPaletteHak)
                Assert.Ignore("SWLOR_Haks/sw_palette is not checked out, so hak precedence cannot be observed.");

            var overridden = new List<string>();
            foreach (var stem in new[] { "creaturepal", "doorpal", "itempal", "placeablepal" })
            {
                if (Data.Index.TryLookup(
                        new ResourceIdentity(stem, ResourceIdentity.TypeFromExtension("itp")), out var handle) &&
                    handle.Provenance.Kind == ResourceLayerKind.Hak)
                    overridden.Add(stem);
            }

            overridden.Should().NotBeEmpty(
                because: "the bare palette names resolve to SWLOR hak content, which is why the loader " +
                         "must ask for the '*palstd' names instead");
        }

        /// <summary>
        /// Floors are the measured shape of the shipped palettes, rounded down: placeables 18 folders /
        /// 1,453 blueprints, doors 5 / 38, creatures 52 / 647. They are regression guards for the whole
        /// binary GFF -> JSON -> ItpDocument -> importer chain, not targets.
        /// </summary>
        [Test]
        [TestCase(ResourceType.Utp, 15, 1400)]
        [TestCase(ResourceType.Utd, 4, 30)]
        [TestCase(ResourceType.Utc, 45, 600)]
        public void A_Standard_Palette_Imports_A_Populated_Tree(
            ResourceType type, int minimumFolders, int minimumResRefs)
        {
            var palette = Data.Load(type);

            palette.IsEmpty.Should().BeFalse();
            palette.Section.AllFolders().Count().Should().BeGreaterThanOrEqualTo(minimumFolders);
            palette.ResRefs.Count.Should().BeGreaterThanOrEqualTo(minimumResRefs);

            // Every resref the palette offers must really resolve - that is the promise of the set.
            foreach (var resRef in palette.ResRefs)
            {
                Data.Index.TryLookup(
                        new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension(type.Extension())), out _)
                    .Should().BeTrue();
            }
        }

        [Test]
        public void Standard_Category_Names_Come_From_The_Base_Tlk()
        {
            var placeholders = new[] { ResourceType.Utp, ResourceType.Utd, ResourceType.Utc }
                .SelectMany(type => Data.Load(type).Section.AllFolders())
                .Where(folder => folder.Name.StartsWith("Category ", StringComparison.Ordinal))
                .Select(folder => folder.Name)
                .ToList();

            placeholders.Should().BeEmpty(
                because: "with the base dialog.tlk supplied every strref-named category resolves to real " +
                         $"text; unresolved: {string.Join(", ", placeholders.Take(5))}");

            // A concrete name, so "no placeholders" cannot be satisfied by an empty tree.
            Data.Load(ResourceType.Utd).Section.Folders
                .Select(folder => folder.Name)
                .Should().Contain("Universal");
        }

        [Test]
        public void Without_A_Resource_Index_A_Standard_Palette_Is_Empty_Rather_Than_Throwing()
        {
            StandardPaletteLoader.Load(null, ResourceType.Utp).IsEmpty.Should().BeTrue();
        }

        [Test]
        public void Types_The_Game_Ships_No_Palette_For_Have_No_Standard_Content()
        {
            foreach (var type in new[] { ResourceType.Area, ResourceType.Dlg, ResourceType.Nss })
            {
                StandardPaletteLoader.PaletteResRefFor(type).Should().BeNull();
                StandardPaletteLoader.Load(Data.Index, type).IsEmpty.Should().BeTrue();
            }
        }

        [Test]
        public void LoadBlueprint_Falls_Back_To_The_Resource_Index_For_A_Base_Game_Only_ResRef()
        {
            const string resRef = "nw_chicken";
            File.Exists(Data.Workspace.GetResourcePath(ResourceType.Utc, resRef))
                .Should().BeFalse(because: "this test is only meaningful for a resref the module does not have");

            var document = Data.Workspace.LoadBlueprint(ResourceType.Utc, resRef);

            document.Should().BeOfType<UtcDocument>();
            var creature = (UtcDocument)document;
            creature.TemplateResRef.Should().Be(resRef);
            creature.Fields.Entries.Count.Should().BeGreaterThanOrEqualTo(60,
                because: "the bridged binary blueprint carries its full field set (63 root fields observed)");
        }

        [Test]
        public void LoadBlueprint_Still_Prefers_The_Modules_Own_File()
        {
            var document = Data.Workspace.LoadBlueprint(ResourceType.Utc, "alask");

            ((UtcDocument)document).Tag.Should().Be("Alask");
        }

        [Test]
        public void LoadBlueprint_Naming_Both_Places_When_A_ResRef_Exists_In_Neither()
        {
            var act = () => Data.Workspace.LoadBlueprint(ResourceType.Utc, "no_such_creature_xyz");

            act.Should().Throw<FileNotFoundException>()
                .WithMessage("*module*")
                .WithMessage("*resource index*");
        }

        /// <summary>
        /// The standard section describes base-game content, so it must be structurally incapable of being
        /// persisted: it is not part of the catalog the sidecar is written from, and editing it - which the
        /// UI has no reason to do - still leaves the file untouched.
        /// </summary>
        [Test]
        public void Editing_The_Standard_Section_Cannot_Reach_The_Sidecar()
        {
            var palette = Data.Load(ResourceType.Utd);
            var catalog = CategoryCatalog.Load(Path.Combine(Path.GetTempPath(), "swlor-standard-palette-test.json"));

            palette.Section.AddFolder("Injected By A Bug").AddMember("nw_door_gate");
            palette.Section.Pin("Injected By A Bug");
            catalog.MarkDirty();

            var written = System.Text.Encoding.UTF8.GetString(catalog.ToJsonBytes());

            written.Should().NotContain("Injected By A Bug");
            written.Should().NotContain("nw_door_gate");
            catalog.SectionOrNull(ResourceType.Utd).Should().BeNull(
                because: "the standard palette's section is never registered with the catalog");
        }
    }
}
