using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Tunnel corridor mode: rooms joined by Corridor edge-crosser chains through solid cells with
/// Doorway junction ports, instead of open-terrain lanes. Runs the full pipeline against every
/// generation tileset's real .set data, on the open terrain its tileset profile actually uses
/// (zsf01 rooms live on 'floor', vmr01 on 'Plaza').
/// </summary>
public class TunnelCorridorTests
{
    private static readonly Dictionary<string, string> TilesetHakDirectories = new()
    {
        ["tdt01"] = "sw_t_minecave",
        ["zsf01"] = "sw_t_scifibase",
        ["tds01"] = "sw_t_sewer",
        ["vmr01"] = "sw_t_alienruin",
    };

    private static TilesetModel LoadTileset(string tilesetResref)
    {
        var root = FindRepositoryRoot();
        var hakDirectory = TilesetHakDirectories[tilesetResref];
        var contents = File.ReadAllText(Path.Combine(root.FullName, "SWLOR_Haks", hakDirectory, $"{tilesetResref}.set"));
        return TilesetSetParser.Parse(tilesetResref, contents);
    }

    private static MacroLayoutParameters TunnelParameters(TilesetModel model, string openTerrainOverride, int seedWidth = 20)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel,
            MinRooms = 6,
            MaxRooms = 9,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            LoopFactor = 0.3,
            Width = seedWidth,
            Height = seedWidth,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = string.IsNullOrEmpty(openTerrainOverride) ? model.FloorTerrain : openTerrainOverride,
        };
    }

    [TestCase("tdt01", "")]
    [TestCase("tds01", "")]
    [TestCase("zsf01", "floor")]
    [TestCase("vmr01", "Plaza")]
    public void TunnelMode_FullPipelineSucceedsAcrossManySeeds(string tilesetResref, string openTerrainOverride)
    {
        var model = LoadTileset(tilesetResref);
        var failures = new List<string>();
        var tunneledLayouts = 0;

        for (var seed = 5000; seed < 5030; seed++)
        {
            var rng = new Random(seed);
            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(TunnelParameters(model, openTerrainOverride), rng);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;
            if (macro.TunnelLinks.Count > 0)
                tunneledLayouts++;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            // A boss room must exist even though rooms are disconnected in the open-corner graph —
            // role assignment traverses tunnel links.
            macro.Rooms.Should().Contain(r => r.Role == RoomRole.Boss, $"seed {seed} must assign a boss room across tunnels");

            AssertEdgeAgreement(model, macro, resolved, seed, failures);
        }

        failures.Should().BeEmpty();
        tunneledLayouts.Should().BeGreaterThan(25,
            "tunnel carving should succeed for nearly every layout, not constantly fall back to open lanes");
    }

    /// <summary>
    /// Global edge-agreement proof: every resolved tile's oriented edges must match the crosser plan
    /// on all four sides, and adjacent tiles must agree on their shared edge. This is the invariant
    /// that makes the seams render and path correctly in the engine.
    /// </summary>
    private static void AssertEdgeAgreement(TilesetModel model, MacroLayout macro, ResolvedLayout resolved, int seed, List<string> failures)
    {
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

        // TileDoorPlanner substitutes transition-door tiles after resolution; those two cells carry a
        // Doorway edge (paired with each other) that the crosser plan never knew about. They're
        // covered by TileDoorPlannerTests — exclude them from plan agreement here.
        var doorCells = new HashSet<(int X, int Y)>();
        foreach (var transition in resolved.Transitions.Where(t => t.Style == TransitionStyle.Door))
        {
            doorCells.Add(transition.Tile);
            doorCells.Add(transition.DoorCell);
            doorCells.Add(transition.DoorwayCell);
        }

        for (var y = 0; y < resolved.Height; y++)
        {
            for (var x = 0; x < resolved.Width; x++)
            {
                if (doorCells.Contains((x, y))) continue;

                var tile = resolved.GetTile(x, y);
                var record = tilesById[tile.TileId];

                for (var slot = 0; slot < 4; slot++)
                {
                    var actual = record.GetEdgeAt(tile.Orientation, slot) ?? string.Empty;
                    var expected = macro.Crossers.GetEdge(x, y, slot) ?? string.Empty;
                    if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        failures.Add($"seed {seed}: cell ({x},{y}) slot {slot}: planned '{expected}' but resolved TILE{tile.TileId} o={tile.Orientation} has '{actual}'");
                    }
                }
            }
        }
    }

    [Test]
    public void TunnelMode_CrosserPlanUsesOnlyCorridorAndDoorway()
    {
        var model = LoadTileset("zsf01");
        var rng = new Random(6001);
        var macro = MacroLayoutGenerator.Generate(TunnelParameters(model, "floor"), rng);

        macro.TunnelLinks.Should().NotBeEmpty();

        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var y = 0; y < 20; y++)
        for (var x = 0; x < 20; x++)
        for (var slot = 0; slot < 4; slot++)
        {
            var edge = macro.Crossers.GetEdge(x, y, slot);
            if (edge.Length != 0) labels.Add(edge);
        }

        labels.Should().NotBeEmpty();
        labels.Should().BeSubsetOf(new[] { "Corridor", "Doorway" });
        labels.Should().Contain("Doorway", "every tunnel enters rooms through doorway ports");
    }

    [Test]
    public void TunnelMode_IsDeterministicPerSeed()
    {
        var model = LoadTileset("tds01");

        MacroLayout Generate()
        {
            var rng = new Random(6002);
            return MacroLayoutGenerator.Generate(TunnelParameters(model, ""), rng);
        }

        var first = Generate();
        var second = Generate();

        first.TunnelLinks.Count.Should().Be(second.TunnelLinks.Count);
        for (var i = 0; i < first.TunnelLinks.Count; i++)
        {
            first.TunnelLinks[i].CornerA.Should().Be(second.TunnelLinks[i].CornerA);
            first.TunnelLinks[i].CornerB.Should().Be(second.TunnelLinks[i].CornerB);
            first.TunnelLinks[i].Length.Should().Be(second.TunnelLinks[i].Length);
        }

        for (var y = 0; y < 20; y++)
        for (var x = 0; x < 20; x++)
        for (var slot = 0; slot < 4; slot++)
        {
            first.Crossers.GetEdge(x, y, slot).Should().Be(second.Crossers.GetEdge(x, y, slot),
                $"cell ({x},{y}) slot {slot}");
        }
    }

    [TestCase("tdt01", "")]
    [TestCase("tds01", "")]
    public void TunnelMode_TransitionDoorsNeverLandOnTunnelCells(string tilesetResref, string openTerrainOverride)
    {
        var model = LoadTileset(tilesetResref);

        for (var seed = 7000; seed < 7015; seed++)
        {
            var rng = new Random(seed);
            var parameters = TunnelParameters(model, openTerrainOverride);
            parameters.ExitCount = 3;
            parameters.EntranceCount = 2;
            var macro = MacroLayoutGenerator.Generate(parameters, rng);

            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);

            foreach (var transition in resolved.Transitions.Where(t => t.Style == TransitionStyle.Door))
            {
                foreach (var cell in new[] { transition.Tile, transition.DoorCell })
                {
                    for (var slot = 0; slot < 4; slot++)
                    {
                        macro.Crossers.GetEdge(cell.X, cell.Y, slot).Should().BeEmpty(
                            $"seed {seed}: transition door at ({cell.X},{cell.Y}) must not claim a tunnel cell");
                    }
                }
            }
        }
    }

    [Test]
    public void TilesetProfiles_DeclareVerifiedPrimaryOpenTerrains()
    {
        var profiles = new SWLOR.Game.Server.Feature.DungeonDefinition.StandardTilesetProfiles().BuildTilesetProfiles();

        profiles["facility"].PrimaryOpenTerrain.Should().Be("floor",
            "zsf01's declared floor has one fully-open tile; the hand-built room vocabulary lives on 'floor'");
        profiles["ancientruin"].PrimaryOpenTerrain.Should().Be("Plaza",
            "vmr01's Plaza carries 11 fully-open variants vs 4 on Floor");
        profiles["cavern"].PrimaryOpenTerrain.Should().BeEmpty();
        profiles["sewers"].PrimaryOpenTerrain.Should().BeEmpty();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SWLOR.Game.Server.sln")))
                return current;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (SWLOR.Game.Server.sln).");
    }
}
