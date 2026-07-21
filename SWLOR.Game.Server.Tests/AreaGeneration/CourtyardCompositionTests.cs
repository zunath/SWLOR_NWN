using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Decoration;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Regression coverage for composed courtyard arrangements (DungeonDecorationPlanner.PlanCourtyard):
/// the fix for reported empty plaza interiors on generated city areas. Evidence baseline (July 2026
/// city-density pass, hand-built fcx01 interior items &gt;2 tiles from walls/roads): interior
/// dressing clusters as a centerpiece + 4-13-member ring at radius ~4-9m with a mixed (2-10 distinct
/// resref) composition -- not uniform scatter. Generated measurement after the mechanism (20 seeds,
/// 32x32): futcity/packed 1.8 courtyards/area, futcity_plaza/complex 0.35/area (its rooms are
/// road-threaded and building-consumed, so eligible 3x3 interiors are rare -- see
/// CourtyardInteriorClearance's doc comment).
///
/// Note on the wall-run ring detector (DungeonDecorationCoherenceTests.IsClosedRing): courtyard
/// rings are a real hand-built pattern (a ring of light poles around a floor light is literally
/// present in narshadaar_promi) and are emitted under the dedicated Courtyard/CourtyardCenter
/// contexts, which that detector deliberately does not scan -- it exists to catch same-resref
/// wall-hugging PERIMETER rings, a different artifact. No detector refinement was needed: the two
/// context families are disjoint by construction.
/// </summary>
public class CourtyardCompositionTests
{
    private const int Size = 32;
    private const int SeedBase = 5001;
    private const int SeedCount = 12;

    private static (DungeonDetail Detail, DungeonTilesetProfile Tileset, DungeonLayoutProfile Layout, TilesetModel Model)
        Composition(string tilesetKey, string layoutKey)
    {
        var themes = new MineCaveDungeonDefinition().BuildDungeons();
        var tilesets = new BaseGameTilesetProfiles().BuildTilesetProfiles();
        foreach (var (k, v) in new StandardTilesetProfiles().BuildTilesetProfiles())
            tilesets.TryAdd(k, v);
        DungeonTilesetPaletteInheritance.Apply(tilesets);
        var layouts = new StandardLayoutProfiles().BuildLayoutProfiles();

        var tileset = tilesets[tilesetKey];
        return (themes.Values.First(), tileset, layouts[layoutKey], TilesetTestSource.LoadTileset(tileset.TilesetResref));
    }

    private static List<PlannedDecoration> PlanFor(
        (DungeonDetail Detail, DungeonTilesetProfile Tileset, DungeonLayoutProfile Layout, TilesetModel Model) c, int seed)
    {
        var composition = new DungeonComposition { Content = c.Detail, Tileset = c.Tileset, Layout = c.Layout };
        var result = LayoutSolver.Solve(
            composition.BuildLayoutParameters(), c.Model, Size, Size, seed, c.Tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue($"seed {seed} must solve: {result.FailureReason}");

        return DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, c.Detail, 100);
    }

    [Test]
    public void FutCityPacked_At32_ComposesCourtyardsMatchingHandBuiltShape()
    {
        var c = Composition(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var courtyardsSeen = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var plan = PlanFor(c, SeedBase + i);

            var centers = plan.Where(p => p.Context == DecorationContext.CourtyardCenter).ToList();
            var members = plan.Where(p => p.Context == DecorationContext.Courtyard).ToList();
            courtyardsSeen += centers.Count;

            if (centers.Count == 0)
            {
                members.Should().BeEmpty("ring members only exist as part of a committed courtyard");
                continue;
            }

            // Every ring member belongs to exactly one courtyard: its nearest centerpiece, within
            // the generator's radius band (5.0-6.5 base +-0.5 member jitter; assert with margin).
            foreach (var member in members)
            {
                var nearest = centers.Min(ctr => Vector2.Distance(
                    new Vector2(ctr.Position.X, ctr.Position.Y), new Vector2(member.Position.X, member.Position.Y)));
                nearest.Should().BeInRange(3.5f, 8.5f,
                    $"a courtyard ring member must sit in the measured hand-built radius band around its centerpiece (got {nearest:F1})");
            }

            // Per-courtyard shape: at least 3 committed members, mixed resrefs on full-size rings --
            // the hand-built clusters' 2-10 distinct-resref composition.
            foreach (var center in centers)
            {
                var ring = members.Where(m => Vector2.Distance(
                    new Vector2(center.Position.X, center.Position.Y), new Vector2(m.Position.X, m.Position.Y)) <= 8.5f).ToList();
                ring.Count.Should().BeGreaterOrEqualTo(3, "PlanCourtyard commits only with 3+ ring members");
                if (ring.Count >= 4)
                    ring.Select(m => m.Resref).Distinct().Count().Should().BeGreaterOrEqualTo(2,
                        "full-size courtyard rings cycle a mixed 2-3-resref motif");

                // Members face back at the centerpiece (within quantization tolerance).
                foreach (var member in ring)
                {
                    var expected = Math.Atan2(center.Position.Y - member.Position.Y, center.Position.X - member.Position.X) * (180.0 / Math.PI);
                    var delta = Math.Abs(((member.Facing - expected + 540.0) % 360.0) - 180.0);
                    delta.Should().BeLessThan(25.0, "ring members orient into the arrangement they surround");
                }
            }
        }

        // Measured 1.8 courtyards/area over 20 seeds; require a clearly nonzero floor so the
        // "empty plaza interior" regression cannot silently return.
        courtyardsSeen.Should().BeGreaterOrEqualTo(SeedCount,
            $"expected at least one courtyard per area on average across {SeedCount} seeds (got {courtyardsSeen})");
    }

    [Test]
    public void FutCityPacked_SameSeed_ProducesIdenticalCourtyardPlan()
    {
        var c = Composition(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var a = PlanFor(c, SeedBase);
        var b = PlanFor(c, SeedBase);

        a.Count.Should().Be(b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            a[i].Resref.Should().Be(b[i].Resref);
            a[i].Context.Should().Be(b[i].Context);
            a[i].Position.Should().Be(b[i].Position);
            a[i].Facing.Should().Be(b[i].Facing);
        }
    }

    [Test]
    public void NonCuratedTileset_NeverEmitsCourtyardContexts()
    {
        var c = Composition(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls);

        for (var i = 0; i < 4; i++)
        {
            var plan = PlanFor(c, SeedBase + i);
            plan.Should().NotContain(p => p.Context == DecorationContext.CourtyardCenter || p.Context == DecorationContext.Courtyard,
                "courtyards exist only for palettes that curate CourtyardCenter/Courtyard buckets");
        }
    }
}
