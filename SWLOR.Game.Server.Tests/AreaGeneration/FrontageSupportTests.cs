using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Decoration;
using SWLOR.Game.Server.Service.AreaGenerationService.Frontage;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Footprint-support acceptance suite for frontage buildings on chasm-bearing tilesets (see
/// FrontageSupportRule and DungeonTilesetProfile.ChasmTerrains).
///
/// ENVELOPE (USER OVERRIDE, street-coherence review round): total in-grid chasm share (interior
/// abyss AND map-edge moat) at most 0.05 and in-grid overhang at most 2m -- buildings are
/// effectively fully platform-supported, with only fully-off-grid space exempt. The previously
/// mined hand-built envelope (r16_mine_support.py, 476 building placeables over 19 hand-built
/// fcx01 areas: interior share <= 0.36, overhang <= 9m, map-edge moat free to a 0.50 total)
/// was statistically hand-faithful, but the delivered rim towers it admitted still read as
/// "buildings hanging off the side" in user review -- design authority replaced the mined
/// tolerances; the mined numbers stay recorded in FrontageSupportRule's doc comment. Enclosure
/// is preserved by LayoutPlatformApronPainter's ApronDepth-deep paved band (platform under the
/// frontage footprints by construction).
///
/// Also pins the SUPPORT-ANCHOR grounding contract: every frontage placement carries a ground
/// anchor just inside its fronted open (platform) cell with a plan-time GroundZ matching that
/// tile's height, so the live path (GetGroundHeight at the anchor) and the offline review module
/// (GroundZ + offset) ground buildings identically -- never at the chasm floor under the
/// footprint's center.
/// </summary>
public class FrontageSupportTests
{
    private const int SeedBase = 7001;
    private const int SeedCount = 10;

    /// <summary>The exact composition/seed of the user-reviewed floating-building showcase.</summary>
    private const int ReviewedSeed = 1091305452;

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
        int seed, int size)
    {
        var composition = new DungeonComposition { Content = c.Detail, Tileset = c.Tileset, Layout = c.Layout };
        var parameters = composition.BuildLayoutParameters();
        parameters.EntranceCount = 1;
        parameters.ExitCount = 1;
        parameters.DoorTransitions = true;

        var result = LayoutSolver.Solve(parameters, c.Model, size, size, seed, c.Tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue($"seed {seed} must solve: {result.FailureReason}");

        return (result.Resolved, DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, c.Detail, 100));
    }

    private static (int Dx, int Dy) OutwardOf(PlannedDecoration p) => p.Facing switch
    {
        0f => (1, 0),
        90f => (0, 1),
        180f => (-1, 0),
        _ => (0, -1)
    };

    private static (float MinX, float MinY, float MaxX, float MaxY) FootprintOf(
        PlannedDecoration p, Dictionary<string, BuildingFrontageEntry> entries)
    {
        var entry = entries[p.Resref];
        var outward = OutwardOf(p);
        var width = entry.FaceWidth * p.VisualScale;
        var depth = entry.Depth * p.VisualScale;
        var halfX = outward.Dx != 0 ? depth / 2f : width / 2f;
        var halfY = outward.Dx != 0 ? width / 2f : depth / 2f;
        return (p.Position.X - halfX, p.Position.Y - halfY, p.Position.X + halfX, p.Position.Y + halfY);
    }

    private static IEnumerable<(int Seed, ResolvedLayout Layout, List<PlannedDecoration> Frontage,
            Dictionary<string, BuildingFrontageEntry> Entries)>
        CityPlans(string themeKey, string layoutKey, int size, IEnumerable<int> seeds)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var entries = c.Tileset.FrontageBuildings.ToDictionary(e => e.Resref, StringComparer.OrdinalIgnoreCase);
        foreach (var seed in seeds)
        {
            var (layout, plan) = PlanFor(c, seed, size);
            var frontage = plan.Where(p => p.Context == DecorationContext.BuildingFrontage).ToList();
            yield return (seed, layout, frontage, entries);
        }
    }

    private static IEnumerable<int> SweepSeeds => Enumerable.Range(0, SeedCount).Select(i => SeedBase + i);

    // ============================================================
    // The zero-overhang support envelope (user override) holds on every placement:
    // TOTAL in-grid chasm share -- interior abyss and map-edge moat alike, no moat
    // exemption -- and the near-zero overhang ceiling.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 12)]
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 20)]
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 24)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, 20)]
    public void FrontageBuildings_SatisfySupportEnvelope(string themeKey, string layoutKey, int size)
    {
        var violations = new List<string>();
        var checkedCount = 0;

        foreach (var (seed, layout, frontage, entries) in CityPlans(themeKey, layoutKey, size, SweepSeeds))
        {
            layout.CornerTerrains.Should().NotBeNull("the resolver must carry the corner plan through");

            foreach (var p in frontage)
            {
                checkedCount++;
                var (_, totalShare, overhang) = FrontageSupportRule.Evaluate(
                    FootprintOf(p, entries), layout, new[] { "holes" });
                if (totalShare > FrontageSupportRule.MaxChasmShare + 0.001f)
                    violations.Add($"seed {seed}: '{p.Resref}' at ({p.Position.X:F0},{p.Position.Y:F0}) " +
                                   $"total chasm share {totalShare:F3} > {FrontageSupportRule.MaxChasmShare}");
                if (overhang > FrontageSupportRule.MaxChasmOverhang + 0.1f)
                    violations.Add($"seed {seed}: '{p.Resref}' at ({p.Position.X:F0},{p.Position.Y:F0}) " +
                                   $"chasm overhang {overhang:F1}m > {FrontageSupportRule.MaxChasmOverhang}m");
            }
        }

        checkedCount.Should().BeGreaterThan(0, "the frontage mechanism must still place buildings");
        violations.Should().BeEmpty();
    }

    /// <summary>The exact user-reviewed floating-building composition must conform after the fix.</summary>
    [Test]
    public void ReviewedShowcaseSeed_CarriesNoFloatingBuildings()
    {
        var violations = new List<string>();
        var total = 0;

        foreach (var (seed, layout, frontage, entries) in CityPlans(
                     AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, 20, new[] { ReviewedSeed }))
        {
            frontage.Should().NotBeEmpty($"seed {seed} must still erect canyon walls");
            foreach (var p in frontage)
            {
                total++;
                if (!FrontageSupportRule.IsSupported(FootprintOf(p, entries), layout, new[] { "holes" }))
                {
                    var (interiorShare, totalShare, overhang) = FrontageSupportRule.Evaluate(
                        FootprintOf(p, entries), layout, new[] { "holes" });
                    violations.Add(
                        $"'{p.Resref}' interior {interiorShare:F3} total {totalShare:F3} overhang {overhang:F1}m");
                }
            }
        }

        total.Should().BeGreaterThan(0);
        violations.Should().BeEmpty();
    }

    // ============================================================
    // Support-anchor grounding: anchors sit on the fronted platform cell,
    // GroundZ matches that tile's plan-time height.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 20)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, 20)]
    public void FrontageBuildings_CarryPlatformGroundAnchors(string themeKey, string layoutKey, int size)
    {
        var violations = new List<string>();
        var checkedCount = 0;

        foreach (var (seed, layout, frontage, _) in CityPlans(themeKey, layoutKey, size, SweepSeeds))
        {
            var open = layout.Rooms.Where(r => !r.IsSetPiece).SelectMany(r => r.Tiles).ToHashSet();

            foreach (var p in frontage)
            {
                checkedCount++;
                if (p.GroundAnchor == null)
                {
                    violations.Add($"seed {seed}: '{p.Resref}' carries no ground anchor");
                    continue;
                }

                var anchor = p.GroundAnchor.Value;
                var cell = ((int)MathF.Floor(anchor.X / 10f), (int)MathF.Floor(anchor.Y / 10f));
                if (!open.Contains(cell))
                    violations.Add($"seed {seed}: '{p.Resref}' anchor {anchor} lands on non-open cell {cell}");
                else
                {
                    var expected = layout.GetTile(cell.Item1, cell.Item2).Height * layout.HeightTransition;
                    if (Math.Abs(p.GroundZ - expected) > 0.001f)
                        violations.Add($"seed {seed}: '{p.Resref}' GroundZ {p.GroundZ} != tile height {expected}");
                }
            }
        }

        checkedCount.Should().BeGreaterThan(0);
        violations.Should().BeEmpty();
    }

    // ============================================================
    // Rule scoping: inactive without chasm semantics.
    // ============================================================

    [Test]
    public void SupportRule_IsInactive_WithoutChasmTerrainsOrCornerPlan()
    {
        var layout = new ResolvedLayout { Width = 4, Height = 4 };
        var box = (MinX: 0f, MinY: 0f, MaxX: 40f, MaxY: 40f);

        // No corner plan carried (legacy layouts) -> always supported.
        FrontageSupportRule.IsSupported(box, layout, new[] { "holes" }).Should().BeTrue();

        // No declared chasm terrain -> always supported, corner plan or not.
        layout.CornerTerrains = new CornerTerrainGrid(4, 4, "holes");
        FrontageSupportRule.IsSupported(box, layout, Array.Empty<string>()).Should().BeTrue();

        // Sanity: with both present, an all-chasm grid rejects the footprint.
        FrontageSupportRule.IsSupported(box, layout, new[] { "holes" }).Should().BeFalse();
    }

    /// <summary>Only the chasm-declaring fcx01 family activates the rule; every other family's
    /// profile carries no chasm terrains, so plans stay untouched by this pass.</summary>
    [Test]
    public void OnlyTheCityFamily_DeclaresChasmTerrains()
    {
        var tilesets = new BaseGameTilesetProfiles().BuildTilesetProfiles();
        foreach (var (k, v) in new StandardTilesetProfiles().BuildTilesetProfiles())
            tilesets.TryAdd(k, v);
        DungeonTilesetPaletteInheritance.Apply(tilesets);

        var declaring = tilesets.Values.Where(t => t.ChasmTerrains.Count > 0).ToList();
        declaring.Should().NotBeEmpty();
        declaring.Should().OnlyContain(t => t.TilesetResref == "fcx01",
            "chasm semantics are mined fcx01 evidence; declaring them for another family needs its own evidence pass");
    }
}
