using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP6.1 <see cref="AreaWalkmesh.RaycastGround"/> ground-height raycast:
    /// synthetic hand-built scenes for the ray-triangle math and preferWalkable filtering, plus a
    /// builder-integration check that <see cref="AreaSceneBuilder.Build"/> actually attaches real
    /// walkmesh data when a <see cref="TileWalkmeshCache"/> is supplied (and leaves it null when
    /// one isn't, preserving prior behavior exactly).
    /// </summary>
    public class AreaWalkmeshTests
    {
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
        private static string ModuleDirectory => CorpusLocator.ModuleDirectory;

        private static ResourceIndex BuildHakOnlyIndex() =>
            ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);

        private static TilePlacement MakeTilePlacement(Matrix4x4 transform, WalkMesh? walkmesh, bool isFallback = false) =>
            new()
            {
                TileIndex = 0,
                Column = 0,
                Row = 0,
                TileId = 0,
                Orientation = 0,
                HeightLevel = 0,
                CenterX = 5f,
                CenterY = 5f,
                HeightOffset = 0f,
                Transform = transform,
                IsFallback = isFallback,
                Walkmesh = walkmesh
            };

        private static AreaScene MakeScene(params TilePlacement[] tiles) =>
            new()
            {
                Tileset = "test_tileset",
                Width = 1,
                Height = 1,
                Tiles = tiles,
                Instances = Array.Empty<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics()
            };

        /// <summary>A flat 10x10 quad (two triangles) at the given Z, spanning local X/Y [0,10], all faces walkable.</summary>
        private static WalkMesh FlatQuadAt(float z, int material = 1)
        {
            var vertices = new[]
            {
                new Vector3(0f, 0f, z),
                new Vector3(10f, 0f, z),
                new Vector3(10f, 10f, z),
                new Vector3(0f, 10f, z)
            };

            var faces = new[]
            {
                new WalkFace { A = 0, B = 1, C = 2, Material = material, Walkable = true },
                new WalkFace { A = 0, B = 2, C = 3, Material = material, Walkable = true }
            };

            return new WalkMesh { Vertices = vertices, Faces = faces };
        }

        // ------------------------------------------------------------------------------------
        // Synthetic scene raycast
        // ------------------------------------------------------------------------------------

        [Test]
        public void RaycastGround_DownwardRayOverFlatQuad_HitsExpectedPoint()
        {
            var walkmesh = FlatQuadAt(z: 2f);
            var tile = MakeTilePlacement(Matrix4x4.Identity, walkmesh);
            var scene = MakeScene(tile);

            var ray = new PickRay(new Vector3(5f, 5f, 50f), new Vector3(0f, 0f, -1f));

            var hit = AreaWalkmesh.RaycastGround(ray, scene);

            hit.Should().NotBeNull();
            hit!.Value.X.Should().BeApproximately(5f, 0.0001f);
            hit.Value.Y.Should().BeApproximately(5f, 0.0001f);
            hit.Value.Z.Should().BeApproximately(2f, 0.0001f);
        }

        [Test]
        public void RaycastGround_RayMissingTheFootprint_ReturnsNull()
        {
            var walkmesh = FlatQuadAt(z: 2f);
            var tile = MakeTilePlacement(Matrix4x4.Identity, walkmesh);
            var scene = MakeScene(tile);

            // Origin far outside the quad's [0,10]x[0,10] footprint, still pointing straight down.
            var ray = new PickRay(new Vector3(500f, 500f, 50f), new Vector3(0f, 0f, -1f));

            var hit = AreaWalkmesh.RaycastGround(ray, scene);

            hit.Should().BeNull();
        }

        [Test]
        public void RaycastGround_TileTransformIsApplied_NotJustIdentity()
        {
            // A tile translated to a different world cell - confirms the walkmesh is transformed
            // by the tile's Transform (not raycast in raw local space).
            var walkmesh = FlatQuadAt(z: 3f);
            var transform = Matrix4x4.CreateTranslation(100f, 200f, 0f);
            var tile = MakeTilePlacement(transform, walkmesh);
            var scene = MakeScene(tile);

            var ray = new PickRay(new Vector3(105f, 205f, 50f), new Vector3(0f, 0f, -1f));

            var hit = AreaWalkmesh.RaycastGround(ray, scene);

            hit.Should().NotBeNull();
            hit!.Value.X.Should().BeApproximately(105f, 0.0001f);
            hit.Value.Y.Should().BeApproximately(205f, 0.0001f);
            hit.Value.Z.Should().BeApproximately(3f, 0.0001f);
        }

        [Test]
        public void RaycastGround_NullScene_ReturnsNullWithoutThrowing()
        {
            var ray = new PickRay(Vector3.Zero, new Vector3(0f, 0f, -1f));

            Action act = () => AreaWalkmesh.RaycastGround(ray, null!).Should().BeNull();

            act.Should().NotThrow();
        }

        [Test]
        public void RaycastGround_TileWithNoWalkmesh_IsSkippedNotTreatedAsMiss()
        {
            var withWalkmesh = MakeTilePlacement(Matrix4x4.Identity, FlatQuadAt(z: 1f));
            var withoutWalkmesh = new TilePlacement
            {
                TileIndex = 1,
                Column = 1,
                Row = 0,
                TileId = 0,
                Orientation = 0,
                HeightLevel = 0,
                CenterX = 15f,
                CenterY = 5f,
                HeightOffset = 0f,
                Transform = Matrix4x4.CreateTranslation(10f, 0f, 0f),
                IsFallback = false,
                Walkmesh = null
            };
            var scene = MakeScene(withWalkmesh, withoutWalkmesh);

            var ray = new PickRay(new Vector3(5f, 5f, 50f), new Vector3(0f, 0f, -1f));

            var hit = AreaWalkmesh.RaycastGround(ray, scene);

            hit.Should().NotBeNull("the tile with a real walkmesh should still be hit even though a sibling tile has none");
            hit!.Value.Z.Should().BeApproximately(1f, 0.0001f);
        }

        // ------------------------------------------------------------------------------------
        // preferWalkable filtering
        // ------------------------------------------------------------------------------------

        [Test]
        public void RaycastGround_PreferWalkableTrue_ReturnsWalkableHitOverCloserNonWalkable()
        {
            // Two overlapping quads at different heights over the same footprint: a non-walkable
            // one higher up, a walkable one lower down. A downward ray hits the higher one first.
            var higherNonWalkable = new WalkFace { A = 0, B = 1, C = 2, Material = 7, Walkable = false };
            var higherNonWalkable2 = new WalkFace { A = 0, B = 2, C = 3, Material = 7, Walkable = false };
            var lowerWalkable = new WalkFace { A = 4, B = 5, C = 6, Material = 1, Walkable = true };
            var lowerWalkable2 = new WalkFace { A = 4, B = 6, C = 7, Material = 1, Walkable = true };

            var vertices = new[]
            {
                new Vector3(0f, 0f, 8f), new Vector3(10f, 0f, 8f), new Vector3(10f, 10f, 8f), new Vector3(0f, 10f, 8f),
                new Vector3(0f, 0f, 2f), new Vector3(10f, 0f, 2f), new Vector3(10f, 10f, 2f), new Vector3(0f, 10f, 2f)
            };
            var walkmesh = new WalkMesh
            {
                Vertices = vertices,
                Faces = new[] { higherNonWalkable, higherNonWalkable2, lowerWalkable, lowerWalkable2 }
            };
            var tile = MakeTilePlacement(Matrix4x4.Identity, walkmesh);
            var scene = MakeScene(tile);

            var ray = new PickRay(new Vector3(5f, 5f, 50f), new Vector3(0f, 0f, -1f));

            var preferWalkableHit = AreaWalkmesh.RaycastGround(ray, scene, preferWalkable: true);
            var closestHit = AreaWalkmesh.RaycastGround(ray, scene, preferWalkable: false);

            preferWalkableHit.Should().NotBeNull();
            preferWalkableHit!.Value.Z.Should().BeApproximately(2f, 0.0001f, "preferWalkable=true should skip the closer non-walkable ceiling and land on the walkable floor");

            closestHit.Should().NotBeNull();
            closestHit!.Value.Z.Should().BeApproximately(8f, 0.0001f, "preferWalkable=false should return the geometrically closest hit regardless of walkability");
        }

        [Test]
        public void RaycastGround_PreferWalkableTrue_FallsBackToNonWalkableWhenNoWalkableFaceIsHit()
        {
            var nonWalkable1 = new WalkFace { A = 0, B = 1, C = 2, Material = 7, Walkable = false };
            var nonWalkable2 = new WalkFace { A = 0, B = 2, C = 3, Material = 7, Walkable = false };
            var walkmesh = new WalkMesh
            {
                Vertices = new[]
                {
                    new Vector3(0f, 0f, 4f), new Vector3(10f, 0f, 4f), new Vector3(10f, 10f, 4f), new Vector3(0f, 10f, 4f)
                },
                Faces = new[] { nonWalkable1, nonWalkable2 }
            };
            var tile = MakeTilePlacement(Matrix4x4.Identity, walkmesh);
            var scene = MakeScene(tile);

            var ray = new PickRay(new Vector3(5f, 5f, 50f), new Vector3(0f, 0f, -1f));

            var hit = AreaWalkmesh.RaycastGround(ray, scene, preferWalkable: true);

            hit.Should().NotBeNull("no walkable face exists at all, so the closest non-walkable hit should be returned instead of null");
            hit!.Value.Z.Should().BeApproximately(4f, 0.0001f);
        }

        // ------------------------------------------------------------------------------------
        // GroundHeightAt (trigger-outline draping)
        // ------------------------------------------------------------------------------------

        [Test]
        public void GroundHeightAt_PointOverFlatQuad_ReturnsFloorHeight()
        {
            var tile = MakeTilePlacement(Matrix4x4.Identity, FlatQuadAt(z: 2f));

            var height = AreaWalkmesh.GroundHeightAt(new[] { tile }, 5f, 5f);

            height.Should().NotBeNull();
            height!.Value.Should().BeApproximately(2f, 0.0001f);
        }

        [Test]
        public void GroundHeightAt_PointOutsideEveryTileCell_ReturnsNull()
        {
            var tile = MakeTilePlacement(Matrix4x4.Identity, FlatQuadAt(z: 2f));

            AreaWalkmesh.GroundHeightAt(new[] { tile }, 25f, 5f).Should().BeNull(
                "the point is more than half a tile away from the only tile's centre, so no floor covers it");
        }

        [Test]
        public void GroundHeightAt_PrefersTopmostWalkableOverHigherNonWalkable()
        {
            // A non-walkable wall top at z=8 above a walkable floor at z=2: draping must pick the
            // floor, not the wall top, or a trigger vertex near a wall climbs onto it.
            var walkmesh = new WalkMesh
            {
                Vertices = new[]
                {
                    new Vector3(0f, 0f, 8f), new Vector3(10f, 0f, 8f), new Vector3(10f, 10f, 8f), new Vector3(0f, 10f, 8f),
                    new Vector3(0f, 0f, 2f), new Vector3(10f, 0f, 2f), new Vector3(10f, 10f, 2f), new Vector3(0f, 10f, 2f)
                },
                Faces = new[]
                {
                    new WalkFace { A = 0, B = 1, C = 2, Material = 7, Walkable = false },
                    new WalkFace { A = 0, B = 2, C = 3, Material = 7, Walkable = false },
                    new WalkFace { A = 4, B = 5, C = 6, Material = 1, Walkable = true },
                    new WalkFace { A = 4, B = 6, C = 7, Material = 1, Walkable = true }
                }
            };
            var tile = MakeTilePlacement(Matrix4x4.Identity, walkmesh);

            var height = AreaWalkmesh.GroundHeightAt(new[] { tile }, 5f, 5f);

            height.Should().NotBeNull();
            height!.Value.Should().BeApproximately(2f, 0.0001f);
        }

        [Test]
        public void GroundHeightAt_OnlyNonWalkableFaces_FallsBackToTheirTopmost()
        {
            var walkmesh = new WalkMesh
            {
                Vertices = new[]
                {
                    new Vector3(0f, 0f, 4f), new Vector3(10f, 0f, 4f), new Vector3(10f, 10f, 4f), new Vector3(0f, 10f, 4f)
                },
                Faces = new[]
                {
                    new WalkFace { A = 0, B = 1, C = 2, Material = 7, Walkable = false },
                    new WalkFace { A = 0, B = 2, C = 3, Material = 7, Walkable = false }
                }
            };
            var tile = MakeTilePlacement(Matrix4x4.Identity, walkmesh);

            var height = AreaWalkmesh.GroundHeightAt(new[] { tile }, 5f, 5f);

            height.Should().NotBeNull();
            height!.Value.Should().BeApproximately(4f, 0.0001f);
        }

        [Test]
        public void GroundHeightAt_TwoWalkableStoreys_ReturnsTheTopmost()
        {
            var walkmesh = new WalkMesh
            {
                Vertices = new[]
                {
                    new Vector3(0f, 0f, 8f), new Vector3(10f, 0f, 8f), new Vector3(10f, 10f, 8f), new Vector3(0f, 10f, 8f),
                    new Vector3(0f, 0f, 2f), new Vector3(10f, 0f, 2f), new Vector3(10f, 10f, 2f), new Vector3(0f, 10f, 2f)
                },
                Faces = new[]
                {
                    new WalkFace { A = 0, B = 1, C = 2, Material = 1, Walkable = true },
                    new WalkFace { A = 0, B = 2, C = 3, Material = 1, Walkable = true },
                    new WalkFace { A = 4, B = 5, C = 6, Material = 1, Walkable = true },
                    new WalkFace { A = 4, B = 6, C = 7, Material = 1, Walkable = true }
                }
            };
            var tile = MakeTilePlacement(Matrix4x4.Identity, walkmesh);

            var height = AreaWalkmesh.GroundHeightAt(new[] { tile }, 5f, 5f);

            height.Should().NotBeNull();
            height!.Value.Should().BeApproximately(8f, 0.0001f);
        }

        // ------------------------------------------------------------------------------------
        // Builder integration
        // ------------------------------------------------------------------------------------

        [Test]
        public void Build_WithTileWalkmeshCache_AttachesRealWalkmeshData()
        {
            var (are, git) = LoadArea("bank");
            var index = BuildHakOnlyIndex();
            var tilesetCatalog = new TilesetCatalog(index);
            var modelCache = new TileModelCache(index);
            var walkmeshCache = new TileWalkmeshCache(index, isWalkable: _ => true);

            var scene = AreaSceneBuilder.Build(are, git, tilesetCatalog, modelCache, walkmeshes: walkmeshCache);

            // bank.are's tile #5 (tfb01_p05_01) has a real loose .wok in this repo's hak sources
            // (confirmed while building this coverage); other tiles in the bank tileset may not.
            scene.Tiles.Should().Contain(t => t.Walkmesh != null && t.Walkmesh.Faces.Count > 0,
                "at least one bank tile's model resref should resolve a real .wok through the hak-only index");
        }

        [Test]
        public void Build_WithoutTileWalkmeshCache_EveryTileWalkmeshStaysNull()
        {
            var (are, git) = LoadArea("bank");
            var index = BuildHakOnlyIndex();
            var tilesetCatalog = new TilesetCatalog(index);
            var modelCache = new TileModelCache(index);

            var scene = AreaSceneBuilder.Build(are, git, tilesetCatalog, modelCache);

            scene.Tiles.Should().OnlyContain(t => t.Walkmesh == null,
                "omitting the TileWalkmeshCache parameter must preserve prior behavior exactly");
        }

        private static (AreDocument Are, GitDocument Git) LoadArea(string resRef)
        {
            var workspace = new ModuleWorkspace(ModuleDirectory);
            var (are, git, _) = workspace.LoadArea(resRef);
            return (are, git);
        }
    }
}
