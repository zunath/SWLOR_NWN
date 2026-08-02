using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP2.4 editor lookup/dropdown services in
    /// SWLOR.Toolset.Domain.GameData.Lookups, built over the real repo data (sw_2da, the custom
    /// TLK, and the hakbuilder.json-driven <see cref="ResourceIndex"/>) rather than fixtures, so a
    /// broken column-name assumption shows up as a real test failure.
    /// </summary>
    public class LookupServiceTests
    {
        /// <summary>
        /// Locates the repository root from the test execution context by walking up from the
        /// test assembly location until both "Build\hakbuilder.json" and "SWLOR_Haks" are found.
        /// Deliberately duplicated locally (matching the existing ResourceIndexTests pattern)
        /// rather than added to CorpusLocator, which is out of scope for this work package.
        /// </summary>
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var hakBuilderConfig = Path.Combine(current.FullName, "Build", "hakbuilder.json");
                    var haksDirectory = Path.Combine(current.FullName, "SWLOR_Haks");
                    if (File.Exists(hakBuilderConfig) && Directory.Exists(haksDirectory))
                        return current.FullName;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        private static string HaksDirectory => Path.Combine(RepoRoot, "SWLOR_Haks");
        private static string Sw2DaDirectory => Path.Combine(HaksDirectory, "sw_2da");
        private static string SwTlkJsonPath => Path.Combine(HaksDirectory, "sw_tlk", "sw_tlk.tlk.json");
        private static string HakBuilderConfigPath => Path.Combine(RepoRoot, "Build", "hakbuilder.json");

        private static TwoDaService CreateTwoDaService() => new(Sw2DaDirectory);
        private static TlkService CreateTlkService() => TlkService.Load(SwTlkJsonPath);

        [Test]
        public void AppearanceService_KnownRow_ResolvesLabelAndModelInfo()
        {
            var service = new AppearanceService(CreateTwoDaService(), CreateTlkService());

            // Row 8 in SWLOR_Haks/sw_2da/appearance.2da: Badger, STRING_REF ****, MODELTYPE S
            // (simple model), RACE c_badger (the literal model ResRef for a simple-model row),
            // PORTRAIT po_Badger.
            var badger = service.Get(8);

            badger.Label.Should().Be("Badger");
            badger.DisplayName.Should().Be("Badger", "STRING_REF is **** for this row so DisplayName falls back to LABEL");
            badger.ModelType.Should().Be("S");
            badger.Race.Should().Be("c_badger");
            badger.Portrait.Should().Be("po_Badger");

            var monCalamariCruiser = service.Get(2170);
            monCalamariCruiser.Label.Should().Be("[SWLOR] Mon Calamari Cruiser");
            monCalamariCruiser.DisplayName.Should().Be("[SWLOR] Mon Calamari Cruiser");
            monCalamariCruiser.ModelType.Should().Be("s");
            monCalamariCruiser.Race.Should().Be("C_MON1");
        }

        [Test]
        public void AppearanceService_GetAll_SkipsReservedRowsWithEmptyLabel()
        {
            var service = new AppearanceService(CreateTwoDaService(), CreateTlkService());

            var all = service.GetAll();

            all.Should().NotBeEmpty();
            all.Should().OnlyContain(row => !string.IsNullOrEmpty(row.Label));
            all.Should().Contain(row => row.Id == 8 && row.Label == "Badger");
        }

        [Test]
        public void AppearanceService_GetAll_ReturnsNoChoicesWhenTableIsUnavailable()
        {
            var emptyTwoDaDirectory = Path.Combine(
                Path.GetTempPath(), $"swlor_missing_appearance_{Guid.NewGuid():N}");
            Directory.CreateDirectory(emptyTwoDaDirectory);
            try
            {
                var service = new AppearanceService(
                    new TwoDaService(emptyTwoDaDirectory),
                    CreateTlkService());

                service.GetAll().Should().BeEmpty(
                    "a missing appearance.2da must disable the picker rather than crash the editor");
            }
            finally
            {
                Directory.Delete(emptyTwoDaDirectory, recursive: true);
            }
        }

        [Test]
        public void AppearanceService_UnknownId_Throws()
        {
            var service = new AppearanceService(CreateTwoDaService(), CreateTlkService());

            var act = () => service.Get(int.MaxValue);

            act.Should().Throw<KeyNotFoundException>();
        }

        [Test]
        public void PortraitService_KnownRow_ResolvesBaseResRefAndSexRace()
        {
            var service = new PortraitService(CreateTwoDaService());

            // Row 572 in SWLOR_Haks/sw_2da/portraits.2da: hu_f_sf81_  1  6  ****  ****  ****
            var portrait = service.Get(572);

            portrait.BaseResRef.Should().Be("hu_f_sf81_");
            portrait.DisplayName.Should().Be("hu_f_sf81_", "portraits.2da has no strref column, so DisplayName is always BaseResRef");
            portrait.Sex.Should().Be(1);
            portrait.Race.Should().Be(6);
        }

        [Test]
        public void PortraitService_GetTgaVariants_MatchesRealPortraitResourcesInSwPortraitHak()
        {
            var index = ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);
            var variants = PortraitService.GetTgaVariants("hu_f_sf81_");

            variants.Tiny.Should().Be("po_hu_f_sf81_t");
            variants.Small.Should().Be("po_hu_f_sf81_s");
            variants.Medium.Should().Be("po_hu_f_sf81_m");
            variants.Large.Should().Be("po_hu_f_sf81_l");
            variants.Huge.Should().Be("po_hu_f_sf81_h");

            // All five variants ship as loose .tga files in sw_t_portrait's hak (sw_portrait),
            // confirming the naming convention actually matches real shipped resources.
            var tgaType = ResourceIdentity.TypeFromExtension("tga");
            foreach (var resref in new[] { variants.Tiny, variants.Small, variants.Medium, variants.Large, variants.Huge })
            {
                index.TryLookup(new ResourceIdentity(resref, tgaType), out var handle)
                    .Should().BeTrue($"{resref}.tga should ship in the sw_portrait hak");
                handle.GetBytes().Should().NotBeEmpty();
            }
        }

        [Test]
        public void PlaceableAppearanceService_KnownRow_ResolvesLabelAndModelName()
        {
            var service = new PlaceableAppearanceService(CreateTwoDaService(), CreateTlkService());

            // Row 0 in SWLOR_Haks/sw_2da/placeables.2da: "Armoire 1"  5645  PLC_A01
            var armoire = service.Get(0);

            armoire.Label.Should().Be("Armoire 1");
            armoire.ModelName.Should().Be("PLC_A01");
            armoire.DisplayName.Should().Be("Armoire 1",
                "StrRef 5645 is a base-game dialog.tlk strref and no base TLK is available in this test environment, " +
                "so DisplayName falls back to the already human-readable Label");
        }

        [Test]
        public void SoundService_KnownRow_ResolvesResourceAndCustomTlkDisplayName()
        {
            var service = new SoundService(CreateTwoDaService(), CreateTlkService());

            // Row 201 in SWLOR_Haks/sw_2da/ambientsound.2da: Description 16777490 (a custom strref,
            // 16777216 + 274), Resource mdrnmod_cityday. sw_tlk.tlk.json entry 274 -> "Modern City Day".
            var sound = service.Get(201);

            sound.Resource.Should().Be("mdrnmod_cityday");
            sound.DisplayName.Should().Be("Modern City Day");
        }

        [Test]
        public void SoundService_GetAll_SkipsReservedRowsWithEmptyResource()
        {
            var service = new SoundService(CreateTwoDaService(), CreateTlkService());

            var all = service.GetAll();

            all.Should().NotBeEmpty();
            all.Should().OnlyContain(row => !string.IsNullOrEmpty(row.Resource));
            all.Should().Contain(row => row.Id == 201 && row.Resource == "mdrnmod_cityday");
        }

        [Test]
        public void DoorTypeService_KnownRow_ResolvesLabelAndModel()
        {
            var service = new DoorTypeService(CreateTwoDaService(), CreateTlkService());

            // Row 1 in SWLOR_Haks/sw_2da/doortypes.2da: Wall1Door  TTR_UDoor_01  ...  StringRefGame 63491
            var doorType = service.Get(1);

            doorType.Label.Should().Be("Wall1Door");
            doorType.Model.Should().Be("TTR_UDoor_01");
            doorType.DisplayName.Should().Be("Wall1Door",
                "StringRefGame 63491 is a base-game dialog.tlk strref and no base TLK is available in this test environment");
        }

        [Test]
        public void TilesetCatalog_Tde01_ResolvesAndParsesWithTiles()
        {
            var index = ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);
            var catalog = new TilesetCatalog(index);

            var found = catalog.TryGetTileset("tde01", out var tileset);

            found.Should().BeTrue();
            tileset.Name.Should().Be("TDE01");
            tileset.TileCount.Should().BeGreaterThan(0);
        }

        [Test]
        public void TilesetCatalog_GetTilesetNames_CoversTheFullSwTCorpus()
        {
            var index = ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);
            var catalog = new TilesetCatalog(index);

            var names = catalog.GetTilesetNames();

            names.Count.Should().BeGreaterThan(50, "the sw_t_* tileset corpus should be present");
            names.Should().Contain(name => name.Equals("tde01", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void TilesetCatalog_WithBaseGameLayer_DiscoversBaseGameTilesets()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null || !File.Exists(Path.Combine(installPath, "data", "nwn_base.key")))
            {
                Assert.Ignore("No local NWN:EE base-game KEY/BIF layer was found.");
                return;
            }

            var baseLayer = KeyBifCatalog.Load(Path.Combine(installPath, "data"));
            var index = ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory, baseLayer);
            var catalog = new TilesetCatalog(index);

            catalog.GetTilesetNames().Should().Contain("tcm02",
                "tcm02 is used by the module but ships only in the base-game resource layer");
            catalog.TryGetTileset("tcm02", out var tileset).Should().BeTrue();
            tileset.TileCount.Should().BeGreaterThan(0);
        }

        [Test]
        public void TilesetCatalog_UnknownResref_ReturnsFalse()
        {
            var index = ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);
            var catalog = new TilesetCatalog(index);

            var found = catalog.TryGetTileset("not_a_real_tileset_xyz", out var tileset);

            found.Should().BeFalse();
            tileset.Should().BeNull();
        }
    }
}
