using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Accent-terrain crosser channels: a one-cell-wide band of accent terrain (Water/Pit/Chasm)
/// carved through open space and spanned by exactly one real Bridge edge-crosser chain
/// (LayoutAccentChannelCarver). Runs the full pipeline against every relevant tileset's real .set
/// data, mirroring TunnelCorridorTests' structure for the Bridge crosser instead of Corridor/Doorway.
/// </summary>
public class BridgeChannelTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static MacroLayoutParameters ChannelParameters(
        TilesetModel model, string accentTerrain, int accentChannels, DungeonLayoutStyle style, int width = 24)
    {
        return new MacroLayoutParameters
        {
            Style = style,
            MinRooms = 4,
            MaxRooms = 7,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 6,
            LoopFactor = 0.3,
            Width = width,
            Height = width,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = model.FloorTerrain,
            AccentTerrain = accentTerrain,
            AccentChannels = accentChannels,
        };
    }

    private static IEnumerable<string> AllCrosserLabels(MacroLayout macro, int width, int height)
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        for (var slot = 0; slot < 4; slot++)
        {
            var edge = macro.Crossers.GetEdge(x, y, slot);
            if (edge.Length != 0) yield return edge;
        }
    }

    // Warren is deliberately excluded here: WarrenLayout hard-caps chamber size at 5 corners, and a
    // valid channel window always overlaps the chamber's protected center-tile corners at that size
    // (see AccentChannels_WarrenChambersCannotHostAChannelWithoutOverlappingCenterTile). Organic's
    // blobbier, larger open regions have real room for a crossing.
    [TestCase("tdt01", "Water", DungeonLayoutStyle.OrganicCave)]
    [TestCase("tds01", "Pit", DungeonLayoutStyle.OrganicCave)]
    public void AccentChannels_FullPipelineSucceedsAcrossManySeeds(string tilesetResref, string accentTerrain, DungeonLayoutStyle style)
    {
        var model = LoadTileset(tilesetResref);
        var failures = new List<string>();
        var seedsWithBridge = 0;
        const int seedCount = 15;

        for (var seed = 9000; seed < 9000 + seedCount; seed++)
        {
            var rng = new Random(seed);
            var parameters = ChannelParameters(model, accentTerrain, accentChannels: 2, style);

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;

            var hasBridge = AllCrosserLabels(macro, parameters.Width, parameters.Height)
                .Any(e => string.Equals(e, "Bridge", StringComparison.OrdinalIgnoreCase));
            if (hasBridge) seedsWithBridge++;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            AssertEdgeAgreement(model, macro, resolved, seed, failures);
        }

        failures.Should().BeEmpty();
        seedsWithBridge.Should().BeGreaterThan(0,
            $"{tilesetResref}/{style} should place at least one Bridge crossing across {seedCount} seeds");
        TestContext.WriteLine($"{tilesetResref}/{style}: {seedsWithBridge}/{seedCount} seeds produced a Bridge edge, 0 resolution failures");
    }

    /// <summary>
    /// Global edge-agreement proof for Bridge crossings specifically: every resolved tile carrying a
    /// planned Bridge edge must resolve to a real tile whose oriented edge is Bridge on both sides of
    /// the shared boundary. Mirrors TunnelCorridorTests.AssertEdgeAgreement.
    /// </summary>
    private static void AssertEdgeAgreement(TilesetModel model, MacroLayout macro, ResolvedLayout resolved, int seed, List<string> failures)
    {
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

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

    [TestCase("tdt01", "Water")]
    [TestCase("tds01", "Pit")]
    public void AccentChannels_ConnectivityInvariantHoldsAndLinksAreRecorded(string tilesetResref, string accentTerrain)
    {
        var model = LoadTileset(tilesetResref);
        var channelsWithLinks = 0;

        for (var seed = 9100; seed < 9130; seed++)
        {
            var rng = new Random(seed);
            var parameters = ChannelParameters(model, accentTerrain, accentChannels: 2, DungeonLayoutStyle.OrganicCave);

            // MacroLayoutGenerator.Generate throws InvalidOperationException if connectivity (with
            // links) is ever violated -- reaching this line for every seed already proves the
            // invariant; here we additionally confirm links were actually recorded when a channel
            // was placed (the mechanism actually engaged, not just harmlessly absent).
            var macro = MacroLayoutGenerator.Generate(parameters, rng);

            var hasBridge = AllCrosserLabels(macro, parameters.Width, parameters.Height)
                .Any(e => string.Equals(e, "Bridge", StringComparison.OrdinalIgnoreCase));

            if (hasBridge)
            {
                macro.TunnelLinks.Should().NotBeEmpty($"seed {seed}: a Bridge crossing must record a TunnelLink");
                channelsWithLinks++;
            }
        }

        channelsWithLinks.Should().BeGreaterThan(0, $"{tilesetResref} should exercise the link-recording path at least once across 30 seeds");
    }

    [TestCase("tds01", "Pit")]
    public void AccentChannels_IsDeterministicPerSeed(string tilesetResref, string accentTerrain)
    {
        var model = LoadTileset(tilesetResref);

        MacroLayout Generate()
        {
            var rng = new Random(9200);
            return MacroLayoutGenerator.Generate(ChannelParameters(model, accentTerrain, accentChannels: 2, DungeonLayoutStyle.OrganicCave), rng);
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

        for (var y = 0; y < 24; y++)
        for (var x = 0; x < 24; x++)
        {
            first.Corners.Labels[x, y].Should().Be(second.Corners.Labels[x, y], $"corner ({x},{y})");
            for (var slot = 0; slot < 4; slot++)
            {
                first.Crossers.GetEdge(x, y, slot).Should().Be(second.Crossers.GetEdge(x, y, slot), $"cell ({x},{y}) slot {slot}");
            }
        }
    }

    /// <summary>
    /// Back-compat: AccentChannels=0 (the default) must never emit a Bridge edge and must not
    /// otherwise perturb generation -- same style/params/seed with channels explicitly zeroed
    /// reproduces byte-for-byte identical corners/crossers to a plain accent-only layout.
    /// </summary>
    [TestCase("tdt01", "Water")]
    [TestCase("tds01", "Pit")]
    public void AccentChannels_Zero_ProducesNoBridgeEdgesAndMatchesAccentOnlyBaseline(string tilesetResref, string accentTerrain)
    {
        var model = LoadTileset(tilesetResref);

        for (var seed = 9300; seed < 9315; seed++)
        {
            var withZeroChannels = ChannelParameters(model, accentTerrain, accentChannels: 0, DungeonLayoutStyle.OrganicCave);
            withZeroChannels.AccentDensity = 0.06;
            var macroA = MacroLayoutGenerator.Generate(withZeroChannels, new Random(seed));

            AllCrosserLabels(macroA, withZeroChannels.Width, withZeroChannels.Height)
                .Should().NotContain(e => string.Equals(e, "Bridge", StringComparison.OrdinalIgnoreCase),
                    $"seed {seed}: AccentChannels=0 must never place a Bridge edge");

            // Identical to a hand-built MacroLayoutParameters that never sets AccentChannels at all
            // (the property's own default), proving the new field is a pure opt-in.
            var withDefaultField = ChannelParameters(model, accentTerrain, accentChannels: 0, DungeonLayoutStyle.OrganicCave);
            withDefaultField.AccentDensity = 0.06;
            withDefaultField.AccentChannels.Should().Be(0);
            var macroB = MacroLayoutGenerator.Generate(withDefaultField, new Random(seed));

            for (var y = 0; y <= macroA.Corners.Height; y++)
            for (var x = 0; x <= macroA.Corners.Width; x++)
                macroA.Corners.Labels[x, y].Should().Be(macroB.Corners.Labels[x, y], $"seed {seed} corner ({x},{y})");
        }
    }

    /// <summary>
    /// vmr01's Chasm carries the same bank/span Bridge vocabulary as tdt01/tds01 (verified offline:
    /// TILE47-52/121-124/131-132 span variants, TILE52/179 Plaza-side banks), so the raw carving
    /// mechanism is exercised directly here even though it is NOT wired into the shipped
    /// AncientRuin tileset profile (see AccentChannels_AncientRuinProfileKeepsChasmChannelsDisabled) —
    /// that profile's single AccentTerrain field also gates AccentDensity blob painting, which was
    /// separately verified insufficient for Chasm's corner coverage, so enabling it there would
    /// silently turn on unverified blob patches too.
    /// </summary>
    [Test]
    public void AccentChannels_Vmr01ChasmVocabularyResolvesDirectly()
    {
        var model = LoadTileset("vmr01");
        var failures = new List<string>();
        var seedsWithBridge = 0;
        const int seedCount = 15;

        for (var seed = 9400; seed < 9400 + seedCount; seed++)
        {
            var rng = new Random(seed);
            var parameters = ChannelParameters(model, "Chasm", accentChannels: 2, DungeonLayoutStyle.OrganicCave);
            parameters.OpenTerrain = "Plaza"; // vmr01's richest fully-open terrain (matches AncientRuin's PrimaryOpenTerrain)

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;
            var hasBridge = AllCrosserLabels(macro, parameters.Width, parameters.Height)
                .Any(e => string.Equals(e, "Bridge", StringComparison.OrdinalIgnoreCase));
            if (hasBridge) seedsWithBridge++;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            AssertEdgeAgreement(model, macro, resolved, seed, failures);
        }

        failures.Should().BeEmpty();
        seedsWithBridge.Should().BeGreaterThan(0, "vmr01 Chasm has full bank/span Bridge vocabulary and should place at least one crossing");
    }

    [Test]
    public void AccentChannels_AncientRuinProfileKeepsChasmBlobPaintingDisabledButEnablesChannelsViaChannelTerrain()
    {
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();

        // AncientRuin's shipped default pairing (see AlienRuinDungeonDefinition).
        var composition = new DungeonComposition
        {
            Tileset = tilesetProfiles[StandardTilesetProfiles.AncientRuin],
            Layout = layoutProfiles[StandardLayoutProfiles.Halls]
        };

        var parameters = composition.BuildLayoutParameters();
        parameters.AccentTerrain.Should().BeEmpty("vmr01/Chasm has no verified blob-patch coverage; the shared AccentTerrain field must stay off");
        parameters.ChannelTerrain.Should().Be("Chasm", "vmr01/Chasm has verified bank/span coverage against Plaza, so ChannelTerrain (independent of AccentTerrain) should be populated");

        parameters.Width = 24;
        parameters.Height = 24;
        parameters.SolidTerrain = "Wall";
        parameters.OpenTerrain = "Plaza";

        var model = LoadTileset("vmr01");
        var failures = new List<string>();
        var seedsWithBridge = 0;

        for (var seed = 9500; seed < 9515; seed++)
        {
            var rng = new Random(seed);
            var macro = MacroLayoutGenerator.Generate(parameters, rng);
            macro.Seed = seed;

            var hasBridge = AllCrosserLabels(macro, parameters.Width, parameters.Height)
                .Any(e => string.Equals(e, "Bridge", StringComparison.OrdinalIgnoreCase));
            if (hasBridge) seedsWithBridge++;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            AssertEdgeAgreement(model, macro, resolved, seed, failures);
        }

        failures.Should().BeEmpty();
        seedsWithBridge.Should().BeGreaterThan(0, "AncientRuin's shipped Halls pairing now carves Chasm channels via ChannelTerrain");
    }

    [TestCase(StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Organic)]
    [TestCase(StandardTilesetProfiles.Sewers, StandardLayoutProfiles.Organic)]
    public void AccentChannels_ShippedProfileCompositionPlacesBridgeCrossings(string tilesetKey, string layoutKey)
    {
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var tilesetProfile = tilesetProfiles[tilesetKey];

        var composition = new DungeonComposition
        {
            Tileset = tilesetProfile,
            Layout = layoutProfiles[layoutKey]
        };

        var model = LoadTileset(tilesetProfile.TilesetResref);
        var parameters = composition.BuildLayoutParameters();
        parameters.AccentTerrain.Should().NotBeEmpty();
        parameters.AccentChannels.Should().BeGreaterThan(0);

        parameters.Width = 24;
        parameters.Height = 24;
        parameters.SolidTerrain = model.DefaultTerrain;
        parameters.OpenTerrain = model.FloorTerrain;

        var seedsWithBridge = 0;
        const int seedCount = 15;
        for (var seed = 9600; seed < 9600 + seedCount; seed++)
        {
            var rng = new Random(seed);
            var macro = MacroLayoutGenerator.Generate(parameters, rng);
            TileResolver.TryResolve(model, macro, rng, out _, out var reason).Should().BeTrue(reason);

            if (AllCrosserLabels(macro, parameters.Width, parameters.Height).Any(e => string.Equals(e, "Bridge", StringComparison.OrdinalIgnoreCase)))
                seedsWithBridge++;
        }

        seedsWithBridge.Should().BeGreaterThan(0, $"{tilesetKey}/{layoutKey} composition should place at least one Bridge crossing across {seedCount} seeds");
    }

    /// <summary>
    /// Documents why Warren is not wired for AccentChannels: WarrenLayout.CarveChambers hard-caps
    /// chamber size at 5 corners (Math.Min(parameters.MaxRoomCornerSize, 5)) regardless of what the
    /// layout profile requests. A channel needs 4 consecutive open corner rows (or columns) that
    /// avoid the chamber's own protected center-tile corners; in a chamber only 5 corners tall, the
    /// only two possible 4-row windows are rows [R+1..R+4] and [R..R+3] relative to the chamber's top
    /// row R, and the center tile sits at R+2 (dead center of a 5-row span) — inside both windows.
    /// So no valid, center-avoiding window can ever exist purely within a Warren chamber. This test
    /// exhaustively confirms zero surviving windows across several seeds (mirroring the offline
    /// probe), independent of RNG luck.
    /// </summary>
    [Test]
    public void AccentChannels_WarrenChambersCannotHostAChannelWithoutOverlappingCenterTile()
    {
        var model = LoadTileset("tds01");

        for (var seed = 9700; seed < 9705; seed++)
        {
            var rng = new Random(seed);
            var parameters = new MacroLayoutParameters
            {
                Style = DungeonLayoutStyle.Warren,
                MinRooms = 3,
                MaxRooms = 5,
                MaxRoomCornerSize = 5,
                LoopFactor = 0.3,
                Width = 24,
                Height = 24,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = model.FloorTerrain,
            };

            var layout = MacroLayoutGenerator.Generate(parameters, rng);
            var corners = layout.Corners;
            var open = parameters.OpenTerrain;

            var forbidden = new HashSet<(int X, int Y)>();
            foreach (var room in layout.Rooms)
            {
                var (cx, cy) = room.CenterTile;
                forbidden.Add((cx, cy));
                forbidden.Add((cx + 1, cy));
                forbidden.Add((cx, cy + 1));
                forbidden.Add((cx + 1, cy + 1));
            }

            var survivingWindows = 0;
            for (var length = 3; length <= 6; length++)
            {
                for (var cross = 2; cross <= corners.Height - 3; cross++)
                {
                    for (var cx0 = 1; cx0 <= corners.Width - 2 - length; cx0++)
                    {
                        var cx1 = cx0 + length - 1;
                        var allOpen = true;
                        for (var a = cx0; a <= cx1 + 1 && allOpen; a++)
                        for (var c = cross - 1; c <= cross + 2 && allOpen; c++)
                        {
                            if (corners.Labels[a, c] != open) allOpen = false;
                        }
                        if (!allOpen) continue;

                        var blocked = false;
                        for (var a = cx0; a <= cx1 + 1 && !blocked; a++)
                        for (var c = cross; c <= cross + 1 && !blocked; c++)
                        {
                            if (forbidden.Contains((a, c))) blocked = true;
                        }
                        if (!blocked) survivingWindows++;
                    }
                }
            }

            survivingWindows.Should().Be(0, $"seed {seed}: Warren's 5-corner chamber cap should leave no center-avoiding channel window");
        }
    }

    /// <summary>
    /// The production Sewers dungeon (SewerDungeonDefinition) still defaults to the Warren layout
    /// profile, which does not enable AccentChannels (see above) — confirms that pairing stays
    /// bridge-free rather than silently no-op-ing in a way nobody would notice.
    /// </summary>
    [Test]
    public void AccentChannels_ProductionSewersWarrenPairingStaysBridgeFree()
    {
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var tilesetProfile = tilesetProfiles[StandardTilesetProfiles.Sewers];

        var composition = new DungeonComposition
        {
            Tileset = tilesetProfile,
            Layout = layoutProfiles[StandardLayoutProfiles.Warren]
        };

        var model = LoadTileset(tilesetProfile.TilesetResref);
        var parameters = composition.BuildLayoutParameters();
        parameters.AccentChannels.Should().Be(0, "Warren does not opt into AccentChannels");

        parameters.Width = 24;
        parameters.Height = 24;
        parameters.SolidTerrain = model.DefaultTerrain;
        parameters.OpenTerrain = model.FloorTerrain;

        for (var seed = 9800; seed < 9810; seed++)
        {
            var macro = MacroLayoutGenerator.Generate(parameters, new Random(seed));
            AllCrosserLabels(macro, parameters.Width, parameters.Height)
                .Should().NotContain(e => string.Equals(e, "Bridge", StringComparison.OrdinalIgnoreCase),
                    $"seed {seed}: production Sewers/Warren pairing must not place Bridge edges yet");
        }
    }

}
