using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Round-6 city-quality acceptance suite: the urban placement grammar (see
/// DungeonTilesetProfile.UrbanDressing) and the standard-vs-ruined decoration-profile split (see
/// DungeonTilesetProfile.DecorationProfiles).
///
/// Evidence baseline (July 2026 city review pass, all 24 hand-built fcx01 areas, 10477 decorative
/// placeables): 73.5% cardinal-aligned bearings (within 7.5 degrees of 0/90/180/270 -- objects face
/// the walls/roads/facades they belong to), same-resref groups share a dominant 15-degree
/// orientation bin (median ~50% share), streets keep a clear walkway with only lamp-family fixtures
/// standing on the lane surface, and cargo stacks against structure bases in aligned rows. The
/// round-5 generated areas measured 29% cardinal (chance), kiosks standing ON the carved road
/// ribbon, and wreckage piles free-floating mid-plaza -- the reported "still feels like a
/// scattering of different objects randomly placed" and "destruction needs separate profiles"
/// feedback this suite guards against regressing.
/// </summary>
public class UrbanDressingCompositionTests
{
    private const int SeedBase = 7001;
    private const int SeedCount = 10;
    private const int Size = 20;

    private static (DungeonDetail Detail, DungeonTilesetProfile Tileset, DungeonLayoutProfile Layout, TilesetModel Model)
        Composition(string themeKey, string tilesetKey, string layoutKey)
    {
        var themes = new Dictionary<string, DungeonDetail>();
        foreach (var definition in new IDungeonListDefinition[]
                 { new MineCaveDungeonDefinition(), new AlienRuinDungeonDefinition() })
        foreach (var (k, v) in definition.BuildDungeons())
            themes[k] = v;

        var tilesets = new BaseGameTilesetProfiles().BuildTilesetProfiles();
        foreach (var (k, v) in new StandardTilesetProfiles().BuildTilesetProfiles())
            tilesets.TryAdd(k, v);
        DungeonTilesetPaletteInheritance.Apply(tilesets);
        var layouts = new StandardLayoutProfiles().BuildLayoutProfiles();

        var tileset = tilesets[tilesetKey];
        return (themes[themeKey], tileset, layouts[layoutKey], TilesetTestSource.LoadTileset(tileset.TilesetResref));
    }

    private static (ResolvedLayout Layout, List<PlannedDecoration> Plan) PlanFor(
        (DungeonDetail Detail, DungeonTilesetProfile Tileset, DungeonLayoutProfile Layout, TilesetModel Model) c,
        int seed, string decorationProfile = null)
    {
        var composition = new DungeonComposition { Content = c.Detail, Tileset = c.Tileset, Layout = c.Layout };
        var parameters = composition.BuildLayoutParameters();
        parameters.EntranceCount = 1;
        parameters.ExitCount = 1;
        parameters.DoorTransitions = true;

        var result = LayoutSolver.Solve(parameters, c.Model, Size, Size, seed, c.Tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue($"seed {seed} must solve: {result.FailureReason}");

        return (result.Resolved, DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, c.Detail, 100, decorationProfile));
    }

    private static (int X, int Y) TileOf(PlannedDecoration p) =>
        ((int)MathF.Floor(p.Position.X / 10f), (int)MathF.Floor(p.Position.Y / 10f));

    private static bool IsCardinal(float facingDegrees)
    {
        var remainder = ((facingDegrees % 90f) + 90f) % 90f;
        return remainder <= 7.5f || remainder >= 82.5f;
    }

    private static HashSet<(int X, int Y)> RoadTiles(ResolvedLayout layout)
    {
        var tiles = new HashSet<(int X, int Y)>();
        for (var y = 0; y < layout.Height; y++)
        for (var x = 0; x < layout.Width; x++)
        for (var slot = 0; slot < 4; slot++)
        {
            if (string.Equals(layout.Crossers.GetEdge(x, y, slot), "Routes", StringComparison.OrdinalIgnoreCase))
                tiles.Add((x, y));
        }

        return tiles;
    }

    /// <summary>True when the tile hugs its owning room's boundary (a cardinal neighbor lies
    /// outside the room's own tile set -- the planner's own wall-anchoring notion), so a placement
    /// there is wall/corner-anchored by construction.</summary>
    private static bool IsWallAnchored((int X, int Y) tile, ResolvedLayout layout)
    {
        foreach (var room in layout.Rooms)
        {
            if (room.IsSetPiece || !room.Tiles.Contains(tile))
                continue;

            var tileSet = new HashSet<(int X, int Y)>(room.Tiles);
            return !tileSet.Contains((tile.X + 1, tile.Y)) || !tileSet.Contains((tile.X - 1, tile.Y)) ||
                   !tileSet.Contains((tile.X, tile.Y + 1)) || !tileSet.Contains((tile.X, tile.Y - 1));
        }

        return false;
    }

    private static bool IsNearRoad((int X, int Y) tile, HashSet<(int X, int Y)> roadTiles)
    {
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            if (roadTiles.Contains((tile.X + dx, tile.Y + dy)))
                return true;
        }

        return false;
    }

    private static HashSet<string> RoadAllowedResrefs(DungeonTilesetProfile tileset, string profileName = null)
    {
        var entries = profileName != null && tileset.DecorationProfiles.TryGetValue(profileName, out var named)
            ? named.Decorations
            : tileset.Decorations;
        return entries.Where(e => e.AllowOnRoadSurface).Select(e => e.Resref).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Resrefs curated ONLY by the ruined profile (the destruction content) -- the set the
    /// standard plan must never emit.</summary>
    private static HashSet<string> RuinedOnlyResrefs(DungeonTilesetProfile tileset)
    {
        tileset.DecorationProfiles.Should().ContainKey("ruined", "fcx01 declares the ruined destruction profile");
        var standard = tileset.Decorations.Select(d => d.Resref).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tileset.DecorationProfiles["ruined"].Decorations
            .Select(d => d.Resref)
            .Where(r => !standard.Contains(r))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // ============================================================
    // Bearing alignment: cardinal fraction meets the hand-built band.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls)]
    public void FutCityStandard_CardinalAlignedFraction_MeetsHandBuiltBand(string themeKey, string layoutKey)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var total = 0;
        var cardinal = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i);
            total += plan.Count;
            cardinal += plan.Count(p => IsCardinal(p.Facing));
        }

        total.Should().BeGreaterThan(200, "the city palette should dress densely across 10 seeds");
        var fraction = (double)cardinal / total;
        TestContext.WriteLine($"{themeKey}/{layoutKey}: cardinal-aligned {cardinal}/{total} ({fraction:F3})");
        fraction.Should().BeGreaterOrEqualTo(0.65,
            $"hand-built fcx01 dressing is 73.5% cardinal-aligned; random spin measures ~0.29 (got {fraction:F3})");
    }

    // ============================================================
    // Road integrity: the carved ribbon stays a clear walkway.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, null)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, null)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, "ruined")]
    public void FutCity_RoadCarryingTiles_OnlyHostLampFamilyFixtures(string themeKey, string layoutKey, string profile)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var roadAllowed = RoadAllowedResrefs(c.Tileset, profile);
        roadAllowed.Should().NotBeEmpty("the lamp family is flagged AllowOnRoadSurface");
        var violations = new List<string>();
        var placementsOnRoad = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i, profile);
            var roadTiles = RoadTiles(layout);
            if (roadTiles.Count == 0)
                continue;

            foreach (var placement in plan)
            {
                if (!roadTiles.Contains(TileOf(placement)))
                    continue;

                placementsOnRoad++;
                if (!roadAllowed.Contains(placement.Resref))
                    violations.Add($"seed {SeedBase + i}: '{placement.Resref}' ({placement.Context}) stands on road tile {TileOf(placement)}");
            }
        }

        TestContext.WriteLine($"{themeKey}/{layoutKey}/{profile ?? "standard"}: {placementsOnRoad} on-road placements, {violations.Count} violations");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations.Take(20)));
    }

    // ============================================================
    // Zone discipline: no unanchored singles.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls)]
    public void FutCityStandard_EveryLonePlacement_IsAnchored(string themeKey, string layoutKey)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var violations = new List<string>();
        var singlesChecked = 0;

        // Interior contexts are intentional set pieces (courtyards/centerpieces), never scatter.
        var intentionalContexts = new[]
        {
            DecorationContext.RoomCenter, DecorationContext.CourtyardCenter, DecorationContext.Courtyard
        };

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i);
            var roadTiles = RoadTiles(layout);

            foreach (var placement in plan)
            {
                var hasNeighbor = plan.Any(other =>
                    !ReferenceEquals(other, placement) &&
                    Vector2.Distance(
                        new Vector2(placement.Position.X, placement.Position.Y),
                        new Vector2(other.Position.X, other.Position.Y)) <= 3.0f);
                if (hasNeighbor)
                    continue; // row/pile/vignette membership is itself the anchor

                singlesChecked++;
                if (intentionalContexts.Contains(placement.Context))
                    continue;

                var tile = TileOf(placement);
                var anchored = IsWallAnchored(tile, layout) ||
                               IsNearRoad(tile, roadTiles) ||
                               DungeonDecorationPlanner.IsStructureAdjacent(tile, layout);
                if (!anchored)
                    violations.Add($"seed {SeedBase + i}: lone '{placement.Resref}' ({placement.Context}) at {tile} has no wall/road/structure anchor");
            }
        }

        TestContext.WriteLine($"{themeKey}/{layoutKey}: {singlesChecked} lone placements checked");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations.Take(20)));
    }

    // ============================================================
    // Destruction split: the standard city is clean.
    // ============================================================

    [Test]
    public void FutCityStandardPalette_CuratesNoDestructionContent()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);

        // Static palette check: no wreckage/rubble/debris/dirt resref families in the standard list.
        var destructionFragments = new[] { "rubb", "debri", "dirtyg", "jkpl", "pape", "wallblk", "barrim2" };
        var offenders = c.Tileset.Decorations
            .Select(d => d.Resref)
            .Where(r => destructionFragments.Any(f => r.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .ToList();
        offenders.Should().BeEmpty("destruction content lives exclusively in the ruined profile");
    }

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls)]
    public void FutCityStandardPlan_EmitsZeroRuinedResrefs(string themeKey, string layoutKey)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var ruinedOnly = RuinedOnlyResrefs(c.Tileset);
        ruinedOnly.Should().NotBeEmpty();

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i);
            plan.Where(p => ruinedOnly.Contains(p.Resref)).Should().BeEmpty(
                $"seed {SeedBase + i}: the standard clean-city plan must never draw destruction content");
        }
    }

    // ============================================================
    // The ruined profile: selected explicitly, composed not scattered.
    // ============================================================

    [Test]
    public void FutCityRuinedPlan_DrawsDestructionContent_AndStaysComposed()
    {
        var c = Composition(AlienRuinDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Halls);
        var ruinedOnly = RuinedOnlyResrefs(c.Tileset);
        var destructionSeen = 0;
        var violations = new List<string>();

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i, "ruined");
            destructionSeen += plan.Count(p => ruinedOnly.Contains(p.Resref));

            // Wrecks/rubble anchor against walls, structure bases, and corners -- never
            // free-floating in plaza centers (pile zone discipline applies to this profile too).
            // Courtyard decal toppings are the sanctioned interior exception: PlanCourtyard layers
            // 1-2 items ON a decal centerpiece (emitted under ClutterPile) as part of the composed
            // courtyard arrangement -- exclude anything within topping range of a CourtyardCenter.
            var courtyardCenters = plan
                .Where(p => p.Context == DecorationContext.CourtyardCenter)
                .Select(p => new Vector2(p.Position.X, p.Position.Y))
                .ToList();

            foreach (var placement in plan.Where(p =>
                         p.Context is DecorationContext.ClutterPile or DecorationContext.GroundDecal))
            {
                var position = new Vector2(placement.Position.X, placement.Position.Y);
                if (courtyardCenters.Any(center => Vector2.Distance(center, position) <= 2.0f))
                    continue;

                var tile = TileOf(placement);
                if (!IsWallAnchored(tile, layout) && !DungeonDecorationPlanner.IsStructureAdjacent(tile, layout))
                    violations.Add($"seed {SeedBase + i}: ruined '{placement.Resref}' ({placement.Context}) free-floats at {tile}");
            }
        }

        destructionSeen.Should().BeGreaterThan(50, "the ruined profile's whole point is destruction dressing");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations.Take(20)));
    }

    [Test]
    public void UnknownProfileName_FallsBackToStandardPlan()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var (_, standard) = PlanFor(c, SeedBase);
        var (_, fallback) = PlanFor(c, SeedBase, "no_such_profile");

        fallback.Count.Should().Be(standard.Count);
        for (var i = 0; i < standard.Count; i++)
        {
            fallback[i].Resref.Should().Be(standard[i].Resref);
            fallback[i].Position.Should().Be(standard[i].Position);
            fallback[i].Facing.Should().Be(standard[i].Facing);
            fallback[i].Context.Should().Be(standard[i].Context);
        }
    }

    [Test]
    public void RuinedPlan_SameSeed_IsDeterministic()
    {
        var c = Composition(AlienRuinDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Halls);
        var (_, a) = PlanFor(c, SeedBase, "ruined");
        var (_, b) = PlanFor(c, SeedBase, "ruined");

        a.Count.Should().Be(b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            b[i].Resref.Should().Be(a[i].Resref);
            b[i].Position.Should().Be(a[i].Position);
            b[i].Facing.Should().Be(a[i].Facing);
            b[i].Context.Should().Be(a[i].Context);
        }
    }

    [Test]
    public void ThemeDeclaredDecorationProfile_SelectsNamedPalette()
    {
        var c = Composition(AlienRuinDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Halls);
        var ruinedOnly = RuinedOnlyResrefs(c.Tileset);

        // A theme may DECLARE the named profile (DungeonDetail.DecorationProfile) instead of every
        // request passing it explicitly.
        var declaringDetail = new DungeonDetail
        {
            ThemeKey = c.Detail.ThemeKey,
            TilesetProfileKey = c.Detail.TilesetProfileKey,
            LayoutProfileKey = c.Detail.LayoutProfileKey,
            DecorationBaseDensity = c.Detail.DecorationBaseDensity,
            DecorationProfile = "ruined"
        };

        var composition = new DungeonComposition { Content = c.Detail, Tileset = c.Tileset, Layout = c.Layout };
        var parameters = composition.BuildLayoutParameters();
        parameters.EntranceCount = 1;
        parameters.ExitCount = 1;
        parameters.DoorTransitions = true;
        var result = LayoutSolver.Solve(parameters, c.Model, Size, Size, SeedBase, c.Tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue();

        var plan = DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, declaringDetail, 100);
        plan.Count(p => ruinedOnly.Contains(p.Resref)).Should().BeGreaterThan(0,
            "the theme-declared profile selects the ruined palette without a per-request override");
    }

    // ============================================================
    // Variant inheritance: the Plaza district offers the same profiles/grammar.
    // ============================================================

    [Test]
    public void FutCityPlazaVariant_InheritsUrbanGrammarAndNamedProfiles()
    {
        var tilesets = new BaseGameTilesetProfiles().BuildTilesetProfiles();
        DungeonTilesetPaletteInheritance.Apply(tilesets);

        var plaza = tilesets[BaseGameTilesetProfiles.FutCityPlaza];
        plaza.UrbanDressing.Should().BeTrue("the Cobble2 variant dresses under the family's urban grammar");
        plaza.DecorationProfiles.Should().ContainKey("ruined", "named profiles travel with the standard palette");
    }
}
