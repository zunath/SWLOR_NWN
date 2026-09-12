using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public class DecorationPlacementSafetyTests
{
    private static ResolvedLayout Room() => new()
    {
        Width = 5, Height = 5,
        Rooms = [new LayoutRoom
        {
            Id = 1, Role = RoomRole.Standard, CenterTile = (2, 2),
            Tiles = (from y in Enumerable.Range(1, 3) from x in Enumerable.Range(1, 3) select (x, y)).ToList()
        }]
    };

    private static PlannedDecoration Prop(float x, float y, float radius = 1) => new()
    {
        Resref = "crate", Position = new Vector3(x, y, 0), FootprintRadius = radius
    };

    private static DecorationPlacementReport Apply(List<PlannedDecoration> plan, ResolvedLayout? layout = null,
        DecorationPlacementStyle style = DecorationPlacementStyle.Spacious, DungeonTilesetProfile? profile = null) =>
        DecorationPlacementSafety.Apply(plan, layout ?? Room(), profile ?? new(), new(), style);

    [TestCase(DecorationPlacementStyle.Spacious)]
    [TestCase(DecorationPlacementStyle.Compact)]
    public void OverlappingFootprints_AreOmittedInBothModes(DecorationPlacementStyle style)
    {
        var first = Prop(15, 15, 1.5f);
        var second = Prop(17, 15, 1.5f);
        var plan = new List<PlannedDecoration> { first, second };
        var report = Apply(plan, style: style);
        plan.Should().Equal(first);
        report.OverlapCount.Should().Be(1);
    }

    [Test]
    public void CompactMode_LeavesNarrowerRoutesAndCloserSpacing()
    {
        var layout = Room();
        layout.Transitions.Add(new() { RoomId = 1, Tile = (3, 2), Style = TransitionStyle.Door, DoorX = 40, DoorY = 25 });
        var nearRoute = Prop(32, 27);
        var spacious = new List<PlannedDecoration> { nearRoute };
        var compact = new List<PlannedDecoration> { nearRoute };
        Apply(spacious, layout).RouteConflictCount.Should().Be(1);
        Apply(compact, layout, DecorationPlacementStyle.Compact).PlacedCount.Should().Be(1);
        spacious.Should().BeEmpty();
        compact.Should().Equal(nearRoute);

        var closePair = new List<PlannedDecoration> { Prop(15, 15), Prop(17.2f, 15) };
        Apply(closePair, style: DecorationPlacementStyle.Compact).PlacedCount.Should().Be(2);
        Apply(closePair).PlacedCount.Should().Be(1);
    }

    [Test]
    public void Routes_FollowConcaveRoomTilesAndIncludeTunnelMouths()
    {
        var layout = Room();
        layout.Rooms[0].Tiles = [(1, 1), (2, 1), (3, 1), (3, 2), (3, 3)];
        layout.Rooms[0].CenterTile = (1, 1);
        layout.Crossers = new(5, 5);
        layout.Crossers.SetEdge(3, 3, EdgeSlot.Top, "CustomTunnel");
        var routes = DecorationPlacementSafety.BuildRoutes(layout, DecorationPlacementSafety.BuildOpenSurface(layout), "");
        routes.Should().Contain((new Vector2(35, 35), new Vector2(35, 40)));
        routes.Should().OnlyContain(route => route.Start.X == route.End.X || route.Start.Y == route.End.Y);
        var plan = new List<PlannedDecoration> { Prop(35, 30) };
        Apply(plan, layout).RouteConflictCount.Should().Be(1);
    }

    [Test]
    public void FootprintSupport_AccountsForScaleWallsAndStructureCells()
    {
        var layout = Room();
        layout.StampedStructureTiles.Add((3, 1));
        var scaled = Prop(12, 15, 1.5f);
        scaled.VisualScale = 2;
        var plan = new List<PlannedDecoration> { scaled, Prop(29, 15, 2), Prop(15, 38, 3) };
        Apply(plan, layout).UnsupportedCount.Should().Be(3);
        plan.Should().BeEmpty();
    }

    [Test]
    public void ChasmCheck_RejectsFootprintOverhangEvenWhenItsCenterIsSupported()
    {
        var layout = Room();
        layout.CornerTerrains = new(5, 5, "Floor");
        layout.CornerTerrains.Labels[2, 2] = "Hole";
        var plan = new List<PlannedDecoration> { Prop(14, 20, 2) };
        Apply(plan, layout, profile: new() { ChasmTerrains = ["Hole"] }).UnsupportedCount.Should().Be(1);
    }

    [Test]
    public void Arrangement_IsOmittedTogetherWhenOneMemberCannotFit()
    {
        var first = Prop(15, 15);
        var second = Prop(39.5f, 15);
        first.ArrangementId = second.ArrangementId = 1;
        var plan = new List<PlannedDecoration> { first, second };
        Apply(plan).UnsupportedCount.Should().Be(2);
        plan.Should().BeEmpty();
    }

    [Test]
    public void Stacks_KeepSupportedTiersAndDropTiersWhoseBaseWasRejected()
    {
        var supported = Prop(15, 15);
        var unsupported = Prop(9, 15);
        var tier = Prop(15, 15);
        tier.Position += Vector3.UnitZ;
        tier.SupportDecoration = supported;
        var orphan = Prop(9, 15);
        orphan.Position += Vector3.UnitZ;
        orphan.SupportDecoration = unsupported;
        var plan = new List<PlannedDecoration> { supported, unsupported, tier, orphan };
        Apply(plan);
        plan.Should().Equal(supported, tier);
    }

    [Test]
    public void FloorPaint_DoesNotBlockPropsRoutesOrCreatureClearance()
    {
        var paint = Prop(15, 15, 2);
        paint.BlocksMovement = false;
        paint.Context = DecorationContext.RoadMarking;
        var crate = Prop(15, 15);
        var plan = new List<PlannedDecoration> { paint, crate };
        Apply(plan).PlacedCount.Should().Be(2);
        var draft = new AreaGenerationDraft(new() { ThemeKey = "test" }, new() { Content = new() }, new(),
            new() { Resolved = Room(), PlannedDecorations = plan });
        GeneratedAreaDocumentPopulator.CreatureOccupiedAnchors(draft).Should().ContainSingle();
    }

    [Test]
    public void InvalidNumbers_CannotReachTheAreaDocuments()
    {
        var plan = new List<PlannedDecoration> { Prop(float.NaN, 15), Prop(15, 15, float.PositiveInfinity) };
        Apply(plan).UnsupportedCount.Should().Be(2);
        plan.Should().BeEmpty();
    }

    [Test]
    public void RigidProps_RejectSteepSlopesAndRaisedTileSeams()
    {
        var layout = Room();
        layout.HeightTransition = 5;
        layout.Tiles = Enumerable.Range(0, 25).Select(_ => new ResolvedTile()).ToArray();
        var tileset = new TilesetModel { Tiles = [new TileRecord { CornerHeights = [0, 1, 1, 0] }] };
        DecorationPlacementSafety.HasLevelSupport(new(15, 15), 1.5f, layout, tileset).Should().BeFalse();
        tileset.Tiles[0].CornerHeights = [0, 0, 0, 0];
        layout.Tiles[1 * 5 + 2].Height = 1;
        DecorationPlacementSafety.HasLevelSupport(new(19.5f, 15), 1, layout, tileset).Should().BeFalse();
        DecorationPlacementSafety.HasLevelSupport(new(15, 15), 1, layout, tileset).Should().BeTrue();
    }

    [Test]
    public void CuratedLawnSurface_AcceptsProps_ButAnUnknownFeatureDoesNot()
    {
        var layout = Room();
        layout.FeatureTileCells.Add((1, 1), "Grass");
        var plan = new List<PlannedDecoration> { Prop(15, 15) };
        Apply(plan, layout).UnsupportedCount.Should().Be(1);
        plan.Add(Prop(15, 15));
        Apply(plan, layout, profile: new() { FeatureTileDressings = new() { ["Grass"] = FeatureZoneDressing.Lawn } })
            .PlacedCount.Should().Be(1);
    }

    [Test]
    public void StandardPalette_OverridesTheThemesNamedPalette_AndSmallFootprintsStaySmall()
    {
        var profile = new DungeonTilesetProfile
        {
            Decorations = [new() { Resref = "standard", Size = DecorationSize.Small }],
            DecorationProfiles = new() { ["ruined"] = new() { Decorations = [new() { Resref = "ruin" }] } }
        };
        var detail = new DungeonDetail { DecorationProfile = "ruined", DecorationBaseDensity = 10 };
        var standard = DungeonDecorationPlanner.Plan(Room(), profile, detail, 100, "");
        standard.Should().NotBeEmpty();
        standard.Should().OnlyContain(prop => prop.Resref == "standard" && prop.FootprintRadius == 0.6f);
        DungeonDecorationPlanner.Plan(Room(), profile, detail, 100).Should().OnlyContain(prop => prop.Resref == "ruin");
    }

    [Test]
    public void ZeroDensity_ClearsStaleFrontageOccupancy()
    {
        var layout = Room();
        layout.PlaceableStructureCells.Add((3, 1));
        DungeonDecorationPlanner.Plan(layout, new(), new(), 0).Should().BeEmpty();
        layout.PlaceableStructureCells.Should().BeEmpty();
    }

    [Test]
    public void MissingPlaceables_AreReportedBeforePreviewIsCreatable()
    {
        var draft = new AreaGenerationDraft(new() { ThemeKey = "test" }, new() { Content = new() }, new(),
            new() { Resolved = Room(), PlannedDecorations = [new() { Resref = "missing_prop" }] });
        var action = () => AreaGenerationAuthoringService.ValidatePlaceableBlueprints(draft, new(CorpusLocator.ModuleDirectory));
        action.Should().Throw<InvalidOperationException>().WithMessage("*missing_prop.utp*");
    }

    [Test]
    public void LongBuildings_ReserveTheirMeasuredBoundsWithoutBlockingTheEntireStreet()
    {
        var building = Prop(25, 11, 16);
        building.Context = DecorationContext.BuildingFrontage;
        building.FootprintBounds = new(10, 10, 40, 12);
        var beside = Prop(15, 14);
        var intersecting = Prop(20, 12.5f);
        var plan = new List<PlannedDecoration> { building, beside, intersecting };
        Apply(plan).OverlapCount.Should().Be(1);
        plan.Should().Equal(building, beside);
        var bounds = new DecorationBounds(10, 10, 40, 15);
        var anchors = GeneratedAreaDocumentPopulator.SelectCreatureAnchors(Room(), Room().Rooms[0],
            Enumerable.Repeat(0.5f, 5).ToList(), new List<(float, float, float)>(), new Random(1), [bounds]);
        anchors.Should().OnlyContain(point => !bounds.IntersectsCircle(point.X, point.Y, 0.5f));
    }

    [Test]
    public void Schematic_ShowsOpenExteriorFloorEvenWhenItIsAlsoTheTilesetDefault()
    {
        var layout = new ResolvedLayout
        {
            Width = 1, Height = 1, OpenTerrain = "Cobble", Tiles = [new()],
            Rooms = [new() { Role = RoomRole.Standard, Tiles = [(0, 0)] }]
        };
        var tileset = new TilesetModel
        {
            DefaultTerrain = "Cobble", FloorTerrain = "Cobble",
            Tiles = [new() { Corners = ["Cobble", "Cobble", "Cobble", "Cobble"] }]
        };
        var draft = new AreaGenerationDraft(new() { ThemeKey = "test" }, new(), tileset,
            new() { Success = true, Resolved = layout });
        var preview = new AreaGenerationPreviewRenderer(null).Render(draft, AreaPreviewMode.Schematic, false);
        preview.Pixels.Skip((5 * preview.Width + 5) * 4).Take(3).Should().Equal(70, 105, 140);
    }
}
