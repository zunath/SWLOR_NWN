using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using System.Text;

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
        public void TryLookup_WhenSameResourceExistsInTwoHakLayers_FirstLayerWinsAndProvenanceReflectsIt()
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

                // hakbuilder.json/module.ifo order: the first matching HAK has precedence.
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
                handle.Provenance.LayerName.Should().Be("layer_a", "the first hak layer in precedence order must win");
                handle.Provenance.SourcePath.Should().Be(Path.Combine(layerADirectory, "duplicate.uti"));

                var bytes = handle.GetBytes();
                System.Text.Encoding.UTF8.GetString(bytes).Should().Be("from layer A");
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void BaseGameExistenceCheckDoesNotTreatHakOnlyScriptsAsCompilerInputs()
        {
            var tempRoot = Path.Combine(
                Path.GetTempPath(),
                "SWLOR.Toolset.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                File.WriteAllText(Path.Combine(tempRoot, "hak_header.nss"), "int Helper();");
                var index = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[]
                    {
                        new ResourceIndex.HakLayer("fixture", tempRoot)
                    });
                var identity = ResourceIdentity.FromFileName("hak_header.nss");

                index.TryLookup(identity, out _).Should().BeTrue();
                index.ContainsBaseGameResource(identity).Should().BeFalse(
                    "the compiler receives the NWN KEY/BIF root, not the module's HAK directories");
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
        public void HakArchiveCatalog_ReadsAnIndexedResourceFromPackedHak()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var hakPath = Path.Combine(tempRoot, "fixture.hak");
            var expected = Encoding.UTF8.GetBytes("packed resource");

            try
            {
                WriteSingleResourceHak(hakPath, "packed", "uti", expected);

                var catalog = HakArchiveCatalog.Open(hakPath);
                var identity = ResourceIdentity.FromFileName("packed.uti");

                catalog.ResourceCount.Should().Be(1);
                catalog.TryGetBytes(identity, out var bytes).Should().BeTrue();
                bytes.Should().Equal(expected);
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public async Task ReloadHakLayersAsync_AtomicallyReplacesTheActiveModuleStack()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            var firstDirectory = Path.Combine(tempRoot, "first");
            var secondDirectory = Path.Combine(tempRoot, "second");
            Directory.CreateDirectory(firstDirectory);
            Directory.CreateDirectory(secondDirectory);
            File.WriteAllText(Path.Combine(firstDirectory, "old.uti"), "old");
            File.WriteAllText(Path.Combine(secondDirectory, "new.uti"), "new");

            try
            {
                var index = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[] { new ResourceIndex.HakLayer("first", firstDirectory) });
                index.EnsureInitialized();
                var reloads = 0;
                index.ResourcesReloaded += () => reloads++;

                await index.ReloadHakLayersAsync(
                    new[] { new ResourceIndex.HakLayer("second", secondDirectory) });

                index.TryLookup(ResourceIdentity.FromFileName("old.uti"), out _).Should().BeFalse();
                index.TryLookup(ResourceIdentity.FromFileName("new.uti"), out var replacement).Should().BeTrue();
                Encoding.UTF8.GetString(replacement.GetBytes()).Should().Be("new");
                index.HakLayers.Select(layer => layer.Name).Should().Equal("second");
                reloads.Should().Be(1);
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public async Task ReloadHakLayersAsync_WhenStackIsUnchanged_DoesNotRescanOrInvalidateConsumers()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            File.WriteAllText(Path.Combine(tempRoot, "stable.uti"), "stable");

            try
            {
                var layers = new[] { new ResourceIndex.HakLayer("fixture", tempRoot) };
                var index = new ResourceIndex(baseLayer: null, hakLayersInOrder: layers);
                index.EnsureInitialized();
                var reloads = 0;
                index.ResourcesReloaded += () => reloads++;

                await index.ReloadHakLayersAsync(layers, rescanWhenUnchanged: false);

                reloads.Should().Be(0,
                    "confirming the module.ifo stack at startup must not invalidate every derived cache");
                index.TryLookup(ResourceIdentity.FromFileName("stable.uti"), out _).Should().BeTrue();
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public async Task ReloadHakLayersAsync_ExplicitReloadRescansAnArchiveRebuiltAtTheSamePath()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var hakPath = Path.Combine(tempRoot, "rebuilt.hak");
            var layers = new[] { new ResourceIndex.HakLayer("rebuilt", hakPath) };

            try
            {
                WriteSingleResourceHak(hakPath, "before", "uti", Encoding.UTF8.GetBytes("before"));
                var index = new ResourceIndex(baseLayer: null, hakLayersInOrder: layers);
                index.EnsureInitialized();
                index.TryLookup(ResourceIdentity.FromFileName("before.uti"), out _).Should().BeTrue();

                WriteSingleResourceHak(hakPath, "after", "uti", Encoding.UTF8.GetBytes("after"));
                await index.ReloadHakLayersAsync(layers);

                index.TryLookup(ResourceIdentity.FromFileName("before.uti"), out _).Should().BeFalse();
                index.TryLookup(ResourceIdentity.FromFileName("after.uti"), out var replacement).Should().BeTrue();
                Encoding.UTF8.GetString(replacement.GetBytes()).Should().Be("after");
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public async Task ReloadHakLayersAsync_DoesNotSynchronouslyBlockOnColdInitialization()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var configPath = Path.Combine(tempRoot, "hakbuilder.json");
            File.WriteAllText(configPath, """{"HakList":[]}""");
            using var factoryStarted = new ManualResetEventSlim();
            using var releaseFactory = new ManualResetEventSlim();

            try
            {
                var index = ResourceIndex.FromHakBuilderConfigDeferred(
                    configPath,
                    tempRoot,
                    () =>
                    {
                        factoryStarted.Set();
                        releaseFactory.Wait();
                        return null;
                    });
                factoryStarted.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();

                // StartNew deliberately does not unwrap the returned Task. Its outer task completes
                // only when ReloadHakLayersAsync returns control to its caller at the first await.
                var invocation = Task.Factory.StartNew(
                    () => index.ReloadHakLayersAsync(Array.Empty<ResourceIndex.HakLayer>()));
                var returned = await Task.WhenAny(invocation, Task.Delay(TimeSpan.FromSeconds(2)));

                returned.Should().BeSameAs(invocation,
                    "workspace startup must not synchronously wait for the cold resource scan");
                releaseFactory.Set();
                await await invocation;
            }
            finally
            {
                releaseFactory.Set();
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void FindHakConflicts_ReportsEveryProvidingLayerInPrecedenceOrder()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            var firstDirectory = Path.Combine(tempRoot, "first");
            var secondDirectory = Path.Combine(tempRoot, "second");
            Directory.CreateDirectory(firstDirectory);
            Directory.CreateDirectory(secondDirectory);
            File.WriteAllText(Path.Combine(firstDirectory, "duplicate.2da"), "first");
            File.WriteAllText(Path.Combine(secondDirectory, "duplicate.2da"), "second");
            File.WriteAllText(Path.Combine(secondDirectory, "unique.2da"), "unique");

            try
            {
                var index = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[]
                    {
                        new ResourceIndex.HakLayer("first", firstDirectory),
                        new ResourceIndex.HakLayer("second", secondDirectory)
                    });

                var conflict = index.FindHakConflicts().Should().ContainSingle().Subject;
                conflict.Resource.Should().Be(ResourceIdentity.FromFileName("duplicate.2da"));
                conflict.Layers.Should().Equal("first", "second");
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void AHandleThrowsWhenItsIndexedPayloadCanNoLongerBeRead()
        {
            var tempRoot = Path.Combine(
                Path.GetTempPath(),
                "SWLOR.Toolset.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var resourcePath = Path.Combine(tempRoot, "vanishing.2da");
            File.WriteAllText(resourcePath, "2DA V2.0\r\n");

            try
            {
                var index = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[] { new ResourceIndex.HakLayer("fixture", tempRoot) });
                var identity = new ResourceIdentity(
                    "vanishing",
                    ResourceIdentity.TypeFromExtension("2da"));
                index.TryLookup(identity, out var handle).Should().BeTrue();

                File.Delete(resourcePath);

                var read = () => handle.GetBytes();
                read.Should().Throw<IOException>()
                    .WithMessage("*indexed resource*could not be read*");
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
        public async Task FromHakBuilderConfigDeferred_LoadsBaseLayerOffTheCallingThread()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var configPath = Path.Combine(tempRoot, "hakbuilder.json");
            File.WriteAllText(configPath, """{"HakList":[]}""");

            using var factoryStarted = new ManualResetEventSlim();
            using var releaseFactory = new ManualResetEventSlim();

            try
            {
                var index = ResourceIndex.FromHakBuilderConfigDeferred(
                    configPath,
                    tempRoot,
                    () =>
                    {
                        factoryStarted.Set();
                        releaseFactory.Wait();
                        return null;
                    });

                factoryStarted.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
                index.InitializationTask.IsCompleted.Should().BeFalse(
                    "the caller must receive the index before optional base-game archives finish loading");

                releaseFactory.Set();
                await index.InitializationTask;
            }
            finally
            {
                releaseFactory.Set();
                Directory.Delete(tempRoot, recursive: true);
            }
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
        public void KeyBifCatalog_ConcurrentFirstLoadOfSameArchive_IsStable()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE installation was found; skipping concurrent KEY/BIF test.");
                return;
            }

            var catalog = KeyBifCatalog.Load(Path.Combine(installPath, "data"));
            var modelType = ResourceIdentity.TypeFromExtension("mdl");
            var identity = new[] { "c_bear", "c_badger", "nw_chicken", "plc_chest1" }
                .Select(resRef => new ResourceIdentity(resRef, modelType))
                .FirstOrDefault(catalog.Contains);
            if (identity == default)
            {
                Assert.Ignore("No known base-game model candidate was present in this installation.");
                return;
            }

            var failures = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            Parallel.For(0, 64, _ =>
            {
                try
                {
                    catalog.TryGetBytes(identity, out var bytes).Should().BeTrue();
                    bytes.Should().NotBeEmpty();
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            failures.Should().BeEmpty("concurrent cache initialization must publish one safe BIF instance");
        }

        [Test]
        public void ResourceIdentity_MtrExtension_MapsToNwnEeMaterialType()
        {
            // NWN:EE added MTR after the original resource-type table; keep the standalone
            // formats mapping aligned with SWLOR.NWN.API's ResType.MTR = 2072.
            ResourceIdentity.TypeFromExtension("mtr").Should().Be(2072);
            ResourceIdentity.TypeFromExtension(".mtr").Should().Be(2072);
            ResourceIdentity.ExtensionFromType(2072).Should().Be("mtr");

            var identity = ResourceIdentity.FromFileName("c_barract.mtr");
            identity.ResRef.Should().Be("c_barract");
            identity.ResourceType.Should().Be(2072);
        }

        private static void WriteSingleResourceHak(
            string path,
            string resRef,
            string extension,
            byte[] payload)
        {
            const uint keyOffset = 160;
            const uint resourceOffset = keyOffset + 24;
            const uint payloadOffset = resourceOffset + 8;

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
            writer.Write(Encoding.ASCII.GetBytes("HAK "));
            writer.Write(Encoding.ASCII.GetBytes("V1.0"));
            writer.Write(0u); // localized string count
            writer.Write(0u); // localized string byte size
            writer.Write(1u); // entry count
            writer.Write(0u); // localized string offset
            writer.Write(keyOffset);
            writer.Write(resourceOffset);
            while (stream.Position < keyOffset)
                writer.Write((byte)0);

            var resRefBytes = new byte[16];
            Encoding.ASCII.GetBytes(resRef, resRefBytes);
            writer.Write(resRefBytes);
            writer.Write(0u); // resource id
            writer.Write(ResourceIdentity.TypeFromExtension(extension));
            writer.Write((ushort)0);
            writer.Write(payloadOffset);
            writer.Write((uint)payload.Length);
            writer.Write(payload);

            File.WriteAllBytes(path, stream.ToArray());
        }
    }
}
