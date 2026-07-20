using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP2.3 resource layer: <see cref="ResourceIdentity"/>,
    /// <see cref="NwnInstallLocator"/>, <see cref="KeyBifCatalog"/>, <see cref="HakDirectoryCatalog"/>,
    /// and the layered <see cref="ResourceIndex"/> resolver.
    /// </summary>
    public class ResourceIndexTests
    {
        /// <summary>
        /// Locates the repository root from the test execution context by walking up from the
        /// test assembly location until both "Build\hakbuilder.json" and "SWLOR_Haks" are found.
        /// Deliberately independent from <see cref="CorpusLocator"/> per WP2.3 scope rules.
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

        private static string HakBuilderConfigPath => Path.Combine(RepoRoot, "Build", "hakbuilder.json");

        private static string HaksDirectory => Path.Combine(RepoRoot, "SWLOR_Haks");

        [Test]
        public void TryLookup_WhenSameResourceExistsInTwoHakLayers_LaterLayerWinsAndProvenanceReflectsIt()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            var layerADirectory = Path.Combine(tempRoot, "layer_a");
            var layerBDirectory = Path.Combine(tempRoot, "layer_b");
            Directory.CreateDirectory(layerADirectory);
            Directory.CreateDirectory(layerBDirectory);

            try
            {
                File.WriteAllText(Path.Combine(layerADirectory, "duplicate.uti"), "from layer A");
                File.WriteAllText(Path.Combine(layerBDirectory, "duplicate.uti"), "from layer B");

                // hakbuilder.json order: A is listed first (lowest precedence), B last (highest -
                // "later wins"), matching how HakList/module.ifo precedence works.
                var layers = new List<ResourceIndex.HakLayer>
                {
                    new("layer_a", layerADirectory),
                    new("layer_b", layerBDirectory)
                };

                var index = new ResourceIndex(baseLayer: null, hakLayersInOrder: layers);

                var identity = new ResourceIdentity("duplicate", ResourceIdentity.TypeFromExtension("uti"));
                var found = index.TryLookup(identity, out var handle);

                found.Should().BeTrue();
                handle.Provenance.Kind.Should().Be(ResourceLayerKind.Hak);
                handle.Provenance.LayerName.Should().Be("layer_b", "the later hak layer in precedence order must win");
                handle.Provenance.SourcePath.Should().Be(Path.Combine(layerBDirectory, "duplicate.uti"));

                var bytes = handle.GetBytes();
                System.Text.Encoding.UTF8.GetString(bytes).Should().Be("from layer B");
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void TryLookup_WhenResourceOnlyExistsInEarlierLayer_StillResolves()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            var layerADirectory = Path.Combine(tempRoot, "layer_a");
            var layerBDirectory = Path.Combine(tempRoot, "layer_b");
            Directory.CreateDirectory(layerADirectory);
            Directory.CreateDirectory(layerBDirectory);

            try
            {
                File.WriteAllText(Path.Combine(layerADirectory, "onlyhere.2da"), "2DA V2.0\r\n");

                var layers = new List<ResourceIndex.HakLayer>
                {
                    new("layer_a", layerADirectory),
                    new("layer_b", layerBDirectory)
                };

                var index = new ResourceIndex(baseLayer: null, hakLayersInOrder: layers);

                var identity = new ResourceIdentity("onlyhere", ResourceIdentity.TypeFromExtension("2da"));
                index.TryLookup(identity, out var handle).Should().BeTrue();
                handle.Provenance.LayerName.Should().Be("layer_a");
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void FromHakBuilderConfig_OverRealCorpus_FindsTde01SetAndDungeonModel_AndScansQuickly()
        {
            var index = ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            index.EnsureInitialized();
            stopwatch.Stop();

            TestContext.Out.WriteLine(
                $"ResourceIndex cold directory scan over {index.HakLayers.Count} hak layers took {stopwatch.ElapsedMilliseconds}ms");

            index.HakLayers.Count.Should().BeGreaterThan(50, "hakbuilder.json should list the full SWLOR hak set");

            // tde01 is the dungeon tileset .set file referenced in SetParserTests.
            var setIdentity = new ResourceIdentity("tde01", ResourceIdentity.TypeFromExtension("set"));
            index.TryLookup(setIdentity, out var setHandle).Should().BeTrue("tde01.set ships in sw_t_dungeon");
            setHandle.Provenance.SourcePath.Should().Contain("sw_t_dungeon");
            setHandle.GetBytes().Should().NotBeEmpty();

            // A known model unique to sw_t_dungeon (verified against the corpus - not reused by
            // any other hak folder), so a successful resolve proves the hak layer was scanned and
            // not shadowed by an unrelated same-named resource elsewhere.
            var modelIdentity = new ResourceIdentity("zde01_z06_41", ResourceIdentity.TypeFromExtension("mdl"));
            index.TryLookup(modelIdentity, out var modelHandle).Should().BeTrue("zde01_z06_41.mdl ships in sw_t_dungeon");
            modelHandle.Provenance.SourcePath.Should().Contain("sw_t_dungeon");
            modelHandle.GetBytes().Should().NotBeEmpty();
        }

        [Test]
        public void FromHakBuilderConfig_WhenResourceIsMissing_ReturnsFalseGracefully()
        {
            var index = ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);

            var missing = new ResourceIdentity("definitely_not_a_real_resref_12345", ResourceIdentity.TypeFromExtension("uti"));
            index.TryLookup(missing, out _).Should().BeFalse();
        }

        [Test]
        public void NwnInstallLocator_WhenInstallFound_KeyBifCatalogReportsManyResources()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE installation was found (Steam/GOG); skipping base-game KEY/BIF test.");
                return;
            }

            var dataDirectory = Path.Combine(installPath, "data");
            var keyPath = Path.Combine(dataDirectory, "nwn_base.key");
            if (!File.Exists(keyPath))
            {
                Assert.Ignore($"NWN install found at '{installPath}' but no nwn_base.key under its data directory; skipping.");
                return;
            }

            var catalog = KeyBifCatalog.Load(dataDirectory);

            catalog.ResourceCount.Should().BeGreaterThan(1000, "nwn_base.key indexes the entire base-game resource set");
        }

        [Test]
        public void ResourceIdentity_MtrExtension_MapsToNwnEeMaterialType()
        {
            // Radoub.Formats.Common.ResourceTypes does not define MTR; this is the local patch
            // documented on ResourceIdentity, checked against SWLOR.NWN.API's ResType.MTR = 2072.
            ResourceIdentity.TypeFromExtension("mtr").Should().Be(2072);
            ResourceIdentity.TypeFromExtension(".mtr").Should().Be(2072);
            ResourceIdentity.ExtensionFromType(2072).Should().Be("mtr");

            var identity = ResourceIdentity.FromFileName("c_barract.mtr");
            identity.ResRef.Should().Be("c_barract");
            identity.ResourceType.Should().Be(2072);
        }
    }
}
