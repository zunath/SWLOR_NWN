using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the door anchors <see cref="AreaSceneBuilder"/> resolves out of each placed
    /// tile's <c>[TILEnDOORd]</c> blocks - the only positions the area editor will hang a door at.
    /// </summary>
    public class TileDoorAnchorTests
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
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the repository root from the test context.");
            }
        }

        private static (TilesetCatalog Catalog, TileModelCache Models, ModuleWorkspace Workspace) BuildFixture()
        {
            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"), Path.Combine(RepoRoot, "SWLOR_Haks"));

            return (new TilesetCatalog(index), new TileModelCache(index), new ModuleWorkspace(CorpusLocator.ModuleDirectory));
        }

        /// <summary>
        /// Wherever a tile declares doorways, the door standing on that tile is in one of them.
        /// </summary>
        /// <remarks>
        /// The strongest check available, because those doors were placed by Aurora itself: if the
        /// anchors were computed in the wrong space, off by a half tile, or turned the wrong way with
        /// their tile, the existing doors would not line up with them. A quarter-metre tolerance is far
        /// tighter than any of the failure modes - a mis-rotated anchor lands metres away, a mis-centred
        /// one a full five.
        /// <para>
        /// Scoped to the door's own tile, because the corpus contains doors standing on tiles that
        /// declare no doorway at all: coolship is laid in the custom "fifi" tileset, whose tile 43
        /// carries eight doors and no door nodes, and whose tile 42 has a corrupt node
        /// (<c>X==-4.75</c>). Those doors predate or bypass the rule and say nothing about whether the
        /// arithmetic here is right, so this asserts the case that does - a tile that declares a
        /// doorway must have its door in it.
        /// </para>
        /// </remarks>
        [Test]
        public void DoorAnchors_MatchTheDoorsAuroraAlreadyPlaced()
        {
            var (catalog, models, workspace) = BuildFixture();

            var matched = 0;
            var checkedAreas = 0;
            var offenders = new List<string>();

            foreach (var resRef in workspace.EnumerateAreaResRefs())
            {
                var (are, git, _) = workspace.LoadArea(resRef);
                if (git.Doors.Count == 0)
                    continue;

                var scene = AreaSceneBuilder.Build(are, git, catalog, models);
                if (scene.DoorAnchors.Count == 0)
                    continue;

                checkedAreas++;
                var anchorsByTile = scene.DoorAnchors.ToLookup(anchor => anchor.TileIndex);

                foreach (var door in scene.Instances.Where(i => i.Kind == InstanceMarkerKind.Door))
                {
                    var column = (int)(door.Position.X / AreaSceneBuilder.TileSize);
                    var row = (int)(door.Position.Y / AreaSceneBuilder.TileSize);
                    var candidates = anchorsByTile[row * scene.Width + column].ToList();
                    if (candidates.Count == 0)
                        continue;

                    var nearest = candidates.Min(anchor =>
                        Vector2.Distance(
                            new Vector2(anchor.Position.X, anchor.Position.Y),
                            new Vector2(door.Position.X, door.Position.Y)));

                    if (nearest <= 0.25f)
                        matched++;
                    else if (offenders.Count < 10)
                        offenders.Add(
                            $"{resRef}: door '{door.Tag}' at ({door.Position.X:0.00},{door.Position.Y:0.00}) " +
                            $"is {nearest:0.00}m from its own tile's nearest doorway");
                }

                if (checkedAreas >= 40)
                    break;
            }

            checkedAreas.Should().BeGreaterThan(0, "the corpus should contain areas with both doors and door nodes");
            matched.Should().BeGreaterThan(20, "many of the corpus's placed doors should land on the resolved doorways");
            offenders.Should().BeEmpty();
        }

        /// <summary>
        /// An anchor belongs to the cell of the tile that declared it. Door nodes sit on a tile's edge,
        /// so they land on the cell boundary at furthest - never in a neighbouring cell, which is what
        /// rotating about the wrong origin would produce.
        /// </summary>
        [Test]
        public void DoorAnchors_StayWithinTheDeclaringTilesCell()
        {
            var (catalog, models, workspace) = BuildFixture();
            const float half = AreaSceneBuilder.TileSize / 2f;
            const float epsilon = 0.01f;

            var offenders = new List<string>();
            var checkedAnchors = 0;

            foreach (var resRef in workspace.EnumerateAreaResRefs().Take(40))
            {
                var (are, git, _) = workspace.LoadArea(resRef);
                var scene = AreaSceneBuilder.Build(are, git, catalog, models);

                foreach (var anchor in scene.DoorAnchors)
                {
                    var tile = scene.Tiles[anchor.TileIndex];
                    checkedAnchors++;

                    if (MathF.Abs(anchor.Position.X - tile.CenterX) > half + epsilon ||
                        MathF.Abs(anchor.Position.Y - tile.CenterY) > half + epsilon)
                    {
                        if (offenders.Count < 10)
                        {
                            offenders.Add(
                                $"{resRef}: anchor {anchor.DoorIndex} of tile {anchor.TileIndex} " +
                                $"at ({anchor.Position.X:0.0},{anchor.Position.Y:0.0}) is outside cell " +
                                $"({tile.CenterX:0.0},{tile.CenterY:0.0})");
                        }
                    }
                }
            }

            checkedAnchors.Should().BeGreaterThan(0);
            offenders.Should().BeEmpty();
        }

        /// <summary>
        /// A doorway turns with its tile: a tile rotated one quarter turn presents its doorway one
        /// quarter turn round, on both position and heading. Recomputed here from the .set values
        /// rather than reusing the builder's own arithmetic.
        /// </summary>
        [Test]
        public void DoorAnchors_TurnWithTheirTile()
        {
            var (catalog, models, workspace) = BuildFixture();

            var checkedAnchors = 0;
            var orientationsSeen = new HashSet<int>();
            var offenders = new List<string>();

            foreach (var resRef in workspace.EnumerateAreaResRefs().Take(40))
            {
                var (are, git, _) = workspace.LoadArea(resRef);
                if (!catalog.TryGetTileset(are.Tileset ?? string.Empty, out var tileset))
                    continue;

                var scene = AreaSceneBuilder.Build(are, git, catalog, models);

                foreach (var anchor in scene.DoorAnchors)
                {
                    var tile = scene.Tiles[anchor.TileIndex];
                    var door = tileset.Tiles[tile.TileId].Doors[anchor.DoorIndex];
                    var quarterTurns = tile.Orientation * MathF.PI / 2f;

                    orientationsSeen.Add(tile.Orientation);
                    checkedAnchors++;

                    var cos = MathF.Cos(quarterTurns);
                    var sin = MathF.Sin(quarterTurns);
                    var expectedX = tile.CenterX + ((float)door.X * cos - (float)door.Y * sin);
                    var expectedY = tile.CenterY + ((float)door.X * sin + (float)door.Y * cos);

                    var expectedHeading = (float)(door.Orientation * Math.PI / 180.0) + quarterTurns;
                    var expected = new Vector2(MathF.Cos(expectedHeading), MathF.Sin(expectedHeading));

                    if (MathF.Abs(anchor.Position.X - expectedX) > 0.01f ||
                        MathF.Abs(anchor.Position.Y - expectedY) > 0.01f ||
                        Vector2.Distance(anchor.Orientation, expected) > 0.001f)
                    {
                        if (offenders.Count < 10)
                        {
                            offenders.Add(
                                $"{resRef}: tile {anchor.TileIndex} (orientation {tile.Orientation}) door {anchor.DoorIndex}: " +
                                $"got ({anchor.Position.X:0.00},{anchor.Position.Y:0.00}) facing {anchor.Orientation}, " +
                                $"expected ({expectedX:0.00},{expectedY:0.00}) facing {expected}");
                        }
                    }
                }
            }

            checkedAnchors.Should().BeGreaterThan(0);
            orientationsSeen.Should().HaveCountGreaterThan(1, "the sample must include rotated tiles for this to prove anything");
            offenders.Should().BeEmpty();
        }
    }
}
