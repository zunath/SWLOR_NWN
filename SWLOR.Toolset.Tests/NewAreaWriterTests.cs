using FluentAssertions;
using NUnit.Framework;
using System.Security.Cryptography;
using System.Text.Json;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// End-to-end coverage for the WP7.3 new-area wizard's write path: it must produce an area
    /// triplet that loads back as a real, solid-filled area and is registered in module.ifo. Runs
    /// against a THROWAWAY module fixture (the repo's real template files copied into a temp
    /// directory), so the repository module is never written to.
    /// </summary>
    public class NewAreaWriterTests
    {
        private string _moduleRoot = string.Empty;

        private static string RepoRoot
        {
            get
            {
                var c = new DirectoryInfo(AppContext.BaseDirectory);
                while (c != null)
                {
                    if (File.Exists(Path.Combine(c.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(c.FullName, "SWLOR_Haks")))
                        return c.FullName;
                    c = c.Parent;
                }
                throw new DirectoryNotFoundException("repo root not found");
            }
        }

        [SetUp]
        public void CreateFixtureModule()
        {
            var source = CorpusLocator.ModuleDirectory;
            _moduleRoot = Path.Combine(Path.GetTempPath(), "swlor_wp73_" + Guid.NewGuid().ToString("N"));

            foreach (var folder in new[] { "are", "git", "gic", "ifo", "utc" })
                Directory.CreateDirectory(Path.Combine(_moduleRoot, folder));

            File.Copy(Path.Combine(source, "are", "area_template.are.json"),
                Path.Combine(_moduleRoot, "are", "area_template.are.json"));
            File.Copy(Path.Combine(source, "git", "area_template.git.json"),
                Path.Combine(_moduleRoot, "git", "area_template.git.json"));
            File.Copy(Path.Combine(source, "gic", "area_template.gic.json"),
                Path.Combine(_moduleRoot, "gic", "area_template.gic.json"));
            File.Copy(Path.Combine(source, "ifo", "module.ifo.json"),
                Path.Combine(_moduleRoot, "ifo", "module.ifo.json"));
        }

        [TearDown]
        public void RemoveFixtureModule()
        {
            try
            {
                if (Directory.Exists(_moduleRoot))
                    Directory.Delete(_moduleRoot, recursive: true);
            }
            catch
            {
                // A leftover temp directory must never fail the run.
            }
        }

        /// <summary>Resolves real tilesets, so the fill tile comes from genuine .set corner rules.</summary>
        private static NewAreaWriter.TilesetResolver? RealTilesets()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
                return null;

            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installPath, "data")));
            var catalog = new TilesetCatalog(index);
            return (string resRef, out TilesetDefinition tileset) => catalog.TryGetTileset(resRef, out tileset);
        }

        [Test]
        public void TryCreate_WritesALoadableSolidArea_AndRegistersIt()
        {
            var resolver = RealTilesets();
            if (resolver == null)
            {
                Assert.Ignore("No local NWN:EE install; the fill tile needs real tileset data.");
                return;
            }

            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(workspace, resolver, "wp73_new", "WP73 New", "tms01", 3, 2, out var error)
                .Should().BeTrue(error);

            // The triplet exists and loads back as a real area.
            var (are, _, _) = workspace.LoadArea("wp73_new");
            are.Width.Should().Be(3);
            are.Height.Should().Be(2);
            are.Tileset.Should().Be("tms01");
            are.Tag.Should().Be("wp73_new");
            are.Name.Text.Should().Be("WP73 New");
            are.Tiles.Should().HaveCount(6, "the grid is filled to width*height");

            // Every cell carries the same solid fill tile, that tile really is uniform terrain (a
            // plain walkable floor, not an arbitrary legal tile), and the terrain is the one the
            // TILESET declares - not something a caller chose. Terrain is not an area-level property.
            resolver("tms01", out var tileset).Should().BeTrue();
            tileset.Floor.Should().NotBeNullOrWhiteSpace("the .set declares the blank-area terrain in [GENERAL]");

            var first = AreaTiles.At(are, 0, 0)!.Value;
            var corners = new[]
            {
                TileCorner.NorthWest, TileCorner.NorthEast, TileCorner.SouthWest, TileCorner.SouthEast
            }.Select(c => TileAdjacency.WorldCornerTerrain(tileset.Tiles[first.TileId], first.Orientation, c)).ToList();
            corners.Distinct(StringComparer.OrdinalIgnoreCase).Should().ContainSingle("the fill tile is a single uniform terrain")
                .Which.Should().BeEquivalentTo(tileset.Floor, "the fill comes from the tileset's own declared floor terrain");

            for (var row = 0; row < 2; row++)
            for (var col = 0; col < 3; col++)
                AreaTiles.At(are, col, row).Should().Be(first, "every cell gets the same fill");

            // And the module now lists it.
            IfoDocument.Load(Path.Combine(_moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain("wp73_new");
        }

        [Test]
        public void TryCreate_WithAnotherDocumentSessionOpen_UsesItsOwnTransactions()
        {
            var templatePath = Path.Combine(_moduleRoot, "are", "area_template.are.json");
            using var unrelatedEditorSession = DocumentSession.Open(templatePath);

            var tileset = new TilesetDefinition
            {
                Floor = "Grass",
                Terrains = new[] { new TerrainDefinition("Grass", null, null) },
                Tiles = new[]
                {
                    new TileDefinition
                    {
                        TopLeft = "Grass",
                        TopRight = "Grass",
                        BottomLeft = "Grass",
                        BottomRight = "Grass",
                        PathNode = "A"
                    }
                }
            };
            NewAreaWriter.TilesetResolver resolver =
                (string _, out TilesetDefinition resolved) =>
                {
                    resolved = tileset;
                    return true;
                };

            var workspace = new ModuleWorkspace(_moduleRoot);
            NewAreaWriter.TryCreate(
                    workspace, resolver, "guarded_new", "Guarded New", "synthetic", 2, 2, out var error)
                .Should().BeTrue(error);

            workspace.LoadArea("guarded_new").Are.Name.Text.Should().Be("Guarded New");
            IfoDocument.Load(Path.Combine(_moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain("guarded_new");
        }

        [Test]
        public void TryCreate_WithPopulator_PersistsCustomizationBeforeAtomicWrite()
        {
            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(
                    workspace,
                    SyntheticTilesets(),
                    "customized",
                    "Customized",
                    "synthetic",
                    2,
                    2,
                    (are, _, _) => are.FogClipDist = 73.5f,
                    out var error)
                .Should().BeTrue(error);

            workspace.LoadArea("customized").Are.FogClipDist.Should().Be(73.5f);
            IfoDocument.Load(Path.Combine(_moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain("customized");
        }

        [Test]
        public void TryCreate_WhenPopulatorThrows_RollsBackTripletAndModuleRegistration()
        {
            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(
                    workspace,
                    SyntheticTilesets(),
                    "broken_custom",
                    "Broken Customization",
                    "synthetic",
                    2,
                    2,
                    (_, _, _) => throw new FileNotFoundException("missing generated blueprint"),
                    out var error)
                .Should().BeFalse();

            error.Should().Contain("missing generated blueprint");
            File.Exists(workspace.GetResourcePath(ResourceType.Area, "broken_custom")).Should().BeFalse();
            File.Exists(Path.Combine(_moduleRoot, "git", "broken_custom.git.json")).Should().BeFalse();
            File.Exists(Path.Combine(_moduleRoot, "gic", "broken_custom.gic.json")).Should().BeFalse();
            File.Exists(Path.Combine(
                _moduleRoot,
                ".swlor-toolset-new-area-broken_custom.pending")).Should().BeFalse();
            IfoDocument.Load(Path.Combine(_moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().NotContain("broken_custom");
        }

        /// <summary>
        /// Gives the "fills from the tileset's floor" rule real teeth. tms01 declares a single
        /// terrain, so there it cannot tell "used Floor" apart from "used the only terrain". ztd01
        /// can: it declares Floor=Desert while its terrain list STARTS with Cliff, so an
        /// implementation that grabbed the first terrain (or let a caller choose) would pick Cliff.
        /// </summary>
        [Test]
        public void TryCreate_FillsFromTheTilesetsDeclaredFloor_NotItsFirstTerrain()
        {
            var resolver = RealTilesets();
            if (resolver == null || !resolver("ztd01", out var tileset))
            {
                Assert.Ignore("ztd01 could not be resolved; this gate needs a tileset whose Floor differs from its first terrain.");
                return;
            }

            // Precondition that makes this test discriminating - if it ever stops holding, the test
            // is no longer proving anything and should be pointed at a different tileset.
            tileset.Floor.Should().NotBeEquivalentTo(tileset.Terrains[0].Name,
                "this tileset was chosen because its declared Floor is NOT its first terrain");

            var workspace = new ModuleWorkspace(_moduleRoot);
            NewAreaWriter.TryCreate(workspace, resolver, "wp73_floor", "Floor probe", "ztd01", 2, 2, out var error)
                .Should().BeTrue(error);

            var (are, _, _) = workspace.LoadArea("wp73_floor");
            var fill = AreaTiles.At(are, 0, 0)!.Value;
            TileAdjacency.WorldCornerTerrain(tileset.Tiles[fill.TileId], fill.Orientation, TileCorner.NorthWest)
                .Should().BeEquivalentTo(tileset.Floor, "the blank area is made of the terrain the tileset declares as its floor");
        }

        [Test]
        public void TryCreate_RejectsDuplicateResRef_WithoutTouchingTheExistingArea()
        {
            var resolver = RealTilesets();
            if (resolver == null)
            {
                Assert.Ignore("No local NWN:EE install; the fill tile needs real tileset data.");
                return;
            }

            var workspace = new ModuleWorkspace(_moduleRoot);
            NewAreaWriter.TryCreate(workspace, resolver, "wp73_new", "First", "tms01", 2, 2, out _)
                .Should().BeTrue();
            var bytesAfterFirst = File.ReadAllBytes(workspace.GetResourcePath(ResourceType.Area, "wp73_new"));

            NewAreaWriter.TryCreate(workspace, resolver, "wp73_new", "Second", "tms01", 8, 8, out var error)
                .Should().BeFalse("the resref is taken");
            error.Should().Contain("already exists");

            File.ReadAllBytes(workspace.GetResourcePath(ResourceType.Area, "wp73_new"))
                .Should().Equal(bytesAfterFirst, "a rejected create must not modify the existing area");
        }

        [Test]
        public void TryCreate_PreflightsTheWholeTripletBeforeWritingAre()
        {
            var orphanGit = Path.Combine(_moduleRoot, "git", "orphaned.git.json");
            File.Copy(
                Path.Combine(_moduleRoot, "git", "area_template.git.json"),
                orphanGit);
            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(
                    workspace, null, "orphaned", "Orphaned", "unused", 2, 2, out var error)
                .Should().BeFalse();

            error.Should().Contain("orphaned.git.json");
            File.Exists(workspace.GetResourcePath(ResourceType.Area, "orphaned")).Should().BeFalse(
                "an existing .git destination must be detected before the .are is written");
            File.Exists(Path.Combine(_moduleRoot, "gic", "orphaned.gic.json")).Should().BeFalse();
        }

        [Test]
        public void TryCreate_RecoversTripletLeftByInterruptedCreate()
        {
            var tileset = new TilesetDefinition
            {
                Floor = "Grass",
                Terrains = new[] { new TerrainDefinition("Grass", null, null) },
                Tiles = new[]
                {
                    new TileDefinition
                    {
                        TopLeft = "Grass",
                        TopRight = "Grass",
                        BottomLeft = "Grass",
                        BottomRight = "Grass",
                        PathNode = "A"
                    }
                }
            };
            NewAreaWriter.TilesetResolver resolver =
                (string _, out TilesetDefinition resolved) =>
                {
                    resolved = tileset;
                    return true;
                };

            var workspace = new ModuleWorkspace(_moduleRoot);
            var partialAre = workspace.GetResourcePath(ResourceType.Area, "interrupted");
            File.Copy(
                workspace.GetResourcePath(ResourceType.Area, NewAreaWriter.TemplateResRef),
                partialAre);
            var marker = Path.Combine(
                _moduleRoot,
                ".swlor-toolset-new-area-interrupted.pending");
            File.WriteAllText(marker, JsonSerializer.Serialize(new
            {
                ResRef = "interrupted",
                Are = Fingerprint(File.ReadAllBytes(partialAre)),
                Git = Fingerprint(File.ReadAllBytes(Path.Combine(
                    _moduleRoot, "git", "area_template.git.json"))),
                Gic = Fingerprint(File.ReadAllBytes(Path.Combine(
                    _moduleRoot, "gic", "area_template.gic.json")))
            }));

            NewAreaWriter.TryCreate(
                    workspace,
                    resolver,
                    "interrupted",
                    "Recovered",
                    "synthetic",
                    2,
                    2,
                    out var error)
                .Should().BeTrue(error);

            File.Exists(marker).Should().BeFalse();
            workspace.LoadArea("interrupted").Are.Name.Text.Should().Be("Recovered");
            IfoDocument.Load(Path.Combine(_moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain("interrupted");
        }

        [Test]
        public void TryCreate_PreservesAnIndependentlyRestoredPendingArea()
        {
            var workspace = new ModuleWorkspace(_moduleRoot);
            var partialAre = workspace.GetResourcePath(ResourceType.Area, "interrupted");
            File.Copy(
                workspace.GetResourcePath(ResourceType.Area, NewAreaWriter.TemplateResRef),
                partialAre);
            var marker = Path.Combine(
                _moduleRoot,
                ".swlor-toolset-new-area-interrupted.pending");
            File.WriteAllText(marker, JsonSerializer.Serialize(new
            {
                ResRef = "interrupted",
                Are = Fingerprint(File.ReadAllBytes(partialAre)),
                Git = Fingerprint(File.ReadAllBytes(Path.Combine(
                    _moduleRoot, "git", "area_template.git.json"))),
                Gic = Fingerprint(File.ReadAllBytes(Path.Combine(
                    _moduleRoot, "gic", "area_template.gic.json")))
            }));
            var restored = System.Text.Encoding.UTF8.GetBytes("independently restored generation");
            File.WriteAllBytes(partialAre, restored);

            NewAreaWriter.TryCreate(
                    workspace,
                    SyntheticTilesets(),
                    "interrupted",
                    "Recovered",
                    "synthetic",
                    2,
                    2,
                    out var error)
                .Should().BeFalse();

            error.Should().Contain("changed after the interrupted area creation");
            File.ReadAllBytes(partialAre).Should().Equal(restored);
            File.Exists(marker).Should().BeTrue("the marker remains as recovery evidence");
            IfoDocument.Load(Path.Combine(_moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().NotContain("interrupted");
        }

        [Test]
        public void TryCreate_NormalizesResRefCase()
        {
            var resolver = RealTilesets();
            if (resolver == null)
            {
                Assert.Ignore("No local NWN:EE install; the fill tile needs real tileset data.");
                return;
            }

            var workspace = new ModuleWorkspace(_moduleRoot);

            // NWN resrefs are case-insensitive and conventionally lowercase, so mixed-case input is
            // normalized rather than rejected - the files land under the lowercase name.
            NewAreaWriter.TryCreate(workspace, resolver, "WP73_Mixed", "Mixed", "tms01", 2, 2, out var error)
                .Should().BeTrue(error);

            File.Exists(workspace.GetResourcePath(ResourceType.Area, "wp73_mixed")).Should().BeTrue();
            workspace.EnumerateAreaResRefs().Should().Contain("wp73_mixed");
        }

        [TestCase("", "resref must be rejected when blank")]
        [TestCase("has spaces", "spaces are not allowed")]
        [TestCase("way_too_long_resref_name", "resrefs are capped at 16 characters")]
        [TestCase("bad-dash", "punctuation other than underscore is not allowed")]
        public void TryCreate_RejectsInvalidResRefs(string resRef, string why)
        {
            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(workspace, RealTilesets(), resRef, "Name", "tms01", 2, 2, out var error)
                .Should().BeFalse(why);
            error.Should().NotBeEmpty();
        }

        [Test]
        public void TryCreate_RejectsOutOfRangeDimensions()
        {
            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(workspace, RealTilesets(), "wp73_big", "Too big", "tms01", 33, 2, out var error)
                .Should().BeFalse();
            error.Should().Contain("between 1 and 32");

            NewAreaWriter.TryCreate(workspace, RealTilesets(), "wp73_zero", "Zero", "tms01", 0, 2, out _)
                .Should().BeFalse();
        }

        [Test]
        public async Task TryCreate_SerializesConcurrentModuleIfoUpdates()
        {
            var tileset = new TilesetDefinition
            {
                Floor = "Grass",
                Terrains = new[] { new TerrainDefinition("Grass", null, null) },
                Tiles = new[]
                {
                    new TileDefinition
                    {
                        TopLeft = "Grass",
                        TopRight = "Grass",
                        BottomLeft = "Grass",
                        BottomRight = "Grass",
                        PathNode = "A"
                    }
                }
            };
            NewAreaWriter.TilesetResolver resolver =
                (string _, out TilesetDefinition resolved) =>
                {
                    resolved = tileset;
                    return true;
                };

            var started = new CountdownEvent(2);
            Task<(bool Success, string Error)> StartCreate(string resRef)
            {
                return Task.Run(() =>
                {
                    started.Signal();
                    var success = NewAreaWriter.TryCreate(
                        new ModuleWorkspace(_moduleRoot),
                        resolver,
                        resRef,
                        resRef,
                        "synthetic",
                        2,
                        2,
                        out var error);
                    return (success, error);
                });
            }

            var heldLock = ModuleIfoUpdateLock.Acquire(_moduleRoot);
            Task<(bool Success, string Error)> first;
            Task<(bool Success, string Error)> second;
            try
            {
                first = StartCreate("concurrent_a");
                second = StartCreate("concurrent_b");
                started.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
                await Task.Delay(100);
                first.IsCompleted.Should().BeFalse("the external module.ifo lock is still held");
                second.IsCompleted.Should().BeFalse("the external module.ifo lock is still held");
            }
            finally
            {
                heldLock.Dispose();
            }

            var results = await Task.WhenAll(first!, second!);
            results.Should().OnlyContain(result => result.Success, string.Join("; ", results.Select(r => r.Error)));
            IfoDocument.Load(Path.Combine(_moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain(new[] { "concurrent_a", "concurrent_b" });
        }

        private static NewAreaWriter.TilesetResolver SyntheticTilesets()
        {
            var tileset = new TilesetDefinition
            {
                Floor = "Grass",
                Terrains = new[] { new TerrainDefinition("Grass", null, null) },
                Tiles = new[]
                {
                    new TileDefinition
                    {
                        TopLeft = "Grass",
                        TopRight = "Grass",
                        BottomLeft = "Grass",
                        BottomRight = "Grass",
                        PathNode = "A"
                    }
                }
            };
            return (string _, out TilesetDefinition resolved) =>
            {
                resolved = tileset;
                return true;
            };
        }

        private static object Fingerprint(byte[] content) => new
        {
            Length = content.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(content))
        };
    }
}
