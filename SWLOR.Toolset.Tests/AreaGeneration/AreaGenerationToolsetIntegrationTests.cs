using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.AreaGeneration.Atmosphere;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Definitions;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;
using System.Numerics;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public class AreaGenerationToolsetIntegrationTests
{
    private static string RepoRoot
    {
        get
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")))
                    return current.FullName;
                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the repository root.");
        }
    }

    [Test]
    public void TilesetAdapter_PreservesCanonicalSetData()
    {
        var path = Path.Combine(RepoRoot, "SWLOR_Haks", "sw_t_minecave", "tdt01.set");
        var definition = SetFileParser.ParseFile(path);

        var model = TilesetSetParser.FromDefinition("tdt01", definition);

        model.Resref.Should().Be("tdt01");
        model.Name.Should().Be(definition.Name);
        model.FloorTerrain.Should().Be(definition.Floor);
        model.DefaultTerrain.Should().Be(definition.Default);
        model.Tiles.Should().HaveCount(definition.Tiles.Count);
        model.Groups.Should().HaveCount(definition.Groups.Count);
        model.Tiles[7].Model.Should().Be(definition.Tiles[7].Model);
        model.Tiles[7].Corners.Should().Equal("Wall", "Wall", "Wall", "Floor");
        model.Groups[6].TileIds.Should().Equal(73, 74, 71, 72);
    }

    [Test]
    public void AuthoringService_AndPreview_AreDeterministicForTheSameSeed()
    {
        var (service, _) = CreateAuthoringService();
        var settings = CreateSettings(service, seed: 77231);

        var first = service.Generate(settings);
        var second = service.Generate(settings);

        first.Result.Success.Should().BeTrue(first.Result.FailureReason);
        second.Result.Success.Should().BeTrue(second.Result.FailureReason);
        first.Result.AttemptSeed.Should().Be(second.Result.AttemptSeed);
        second.Result.Resolved!.Tiles.Select(tile => (tile.TileId, tile.Orientation, tile.Height))
            .Should().Equal(first.Result.Resolved!.Tiles.Select(tile => (tile.TileId, tile.Orientation, tile.Height)));

        var preview = new AreaGenerationPreviewRenderer(resources: null).Render(
            first,
            AreaPreviewMode.Schematic,
            showRoomOverlay: true,
            showTransitions: true,
            showDecorations: true);
        preview.Width.Should().Be(first.Result.Resolved.Width * 24);
        preview.Height.Should().Be(first.Result.Resolved.Height * 24);
        preview.Pixels.Should().HaveCount(preview.Width * preview.Height * 4);
        preview.MissingTileGraphics.Should().Be(0, "schematic mode never requests tile artwork");
    }

    [Test]
    public void AuthoringService_RejectsDimensionsBelowTheSelectedStyleFloor()
    {
        var (service, _) = CreateAuthoringService();
        var settings = CreateSettings(service, seed: 77231);
        var undersized = settings with
        {
            LayoutProfileKey = StandardLayoutProfiles.Halls,
            Width = 8,
            Height = 8,
            Overrides = settings.Overrides! with
            {
                Style = DungeonLayoutStyle.RoomsAndCorridors
            }
        };

        var action = () => service.Generate(undersized);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*requires width and height of at least 11*");
    }

    [Test]
    public void TileResolver_RejectsPlateauCandidatesThatWouldProduceNegativeTileHeight()
    {
        var corners = new CornerTerrainGrid(2, 1, "Floor");
        corners.Heights[2, 0] = 1;
        corners.Heights[2, 1] = 1;
        var layout = new MacroLayout(corners)
        {
            DoorTransitions = false,
            OpenTerrain = "Floor"
        };
        var tileset = new TilesetModel
        {
            Resref = "height_test",
            Tiles =
            [
                new TileRecord
                {
                    TileId = 0,
                    Corners = ["Floor", "Floor", "Floor", "Floor"],
                    CornerHeights = [0, 0, 0, 0],
                    PathNode = "B"
                },
                new TileRecord
                {
                    TileId = 1,
                    Corners = ["Floor", "Floor", "Floor", "Floor"],
                    CornerHeights = [1, 1, 1, 1],
                    PathNode = "A"
                },
                new TileRecord
                {
                    TileId = 2,
                    Corners = ["Floor", "Floor", "Floor", "Floor"],
                    CornerHeights = [0, 1, 1, 0],
                    PathNode = "A"
                }
            ]
        };

        TileResolver.TryResolve(tileset, layout, new Random(42), out var resolved, out var failure)
            .Should().BeTrue(failure);

        resolved.Tiles.Should().OnlyContain(tile => tile.Height >= 0);
        resolved.GetTile(0, 0).TileId.Should().Be(0,
            "the fully-pathable plateau candidate would require an invalid negative height");
        resolved.GetTile(1, 0).TileId.Should().Be(2);
    }

    [Test]
    public void GeneratedAreaWriter_CreatesNormalAreaTripletInOpenModule()
    {
        var moduleRoot = CreateFixtureModule();
        try
        {
            var (service, tilesets) = CreateAuthoringService();
            var draft = service.Generate(CreateSettings(service, seed: 93117));
            draft.Result.Success.Should().BeTrue(draft.Result.FailureReason);

            var workspace = new ModuleWorkspace(moduleRoot);
            GeneratedAreaWriter.TryCreate(
                    workspace,
                    tilesets,
                    draft,
                    "procgen_test",
                    "Generated Test Area",
                    out var error)
                .Should().BeTrue(error);

            var (are, git, gic) = workspace.LoadArea("procgen_test");
            are.Width.Should().Be(draft.Result.Resolved!.Width);
            are.Height.Should().Be(draft.Result.Resolved.Height);
            are.Tileset.Should().Be("tdt01");
            are.Name.Text.Should().Be("Generated Test Area");
            are.Tiles.Should().HaveCount(draft.Result.Resolved.Width * draft.Result.Resolved.Height);
            git.Fields.GetListOrEmpty("WaypointList").Should().HaveCount(draft.Result.Resolved.Transitions.Count);
            gic.Fields.GetListOrEmpty("WaypointList").Should().HaveCount(draft.Result.Resolved.Transitions.Count);
            IfoDocument.Load(Path.Combine(moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain("procgen_test");
        }
        finally
        {
            if (Directory.Exists(moduleRoot))
                Directory.Delete(moduleRoot, recursive: true);
        }
    }

    [Test]
    public void DocumentPopulator_WritesTilesAtmosphereTransitionsDoorsAndDecorations()
    {
        var moduleRoot = CorpusLocator.ModuleDirectory;
        var workspace = new ModuleWorkspace(moduleRoot);
        var are = AreDocument.Load(Path.Combine(moduleRoot, "are", "area_template.are.json"));
        var git = GitDocument.Load(Path.Combine(moduleRoot, "git", "area_template.git.json"));
        var gic = GicDocument.Load(Path.Combine(moduleRoot, "gic", "area_template.gic.json"));
        var tiles = Enumerable.Range(0, 4)
            .Select(index => new ResolvedTile { TileId = 20 + index, Orientation = index % 4, Height = index })
            .ToArray();
        var resolved = new ResolvedLayout
        {
            TilesetResref = "tdt01",
            Width = 2,
            Height = 2,
            Tiles = tiles,
            Transitions =
            [
                new TransitionPoint
                {
                    Kind = TransitionKind.Entrance,
                    Tile = (0, 0),
                    Style = TransitionStyle.Placeable
                },
                new TransitionPoint
                {
                    Kind = TransitionKind.Exit,
                    Tile = (1, 1),
                    Style = TransitionStyle.Door,
                    DoorX = 10f,
                    DoorY = 15f,
                    DoorZ = 1f,
                    DoorOrientation = 90f
                }
            ]
        };
        var tilesetProfile = new DungeonTilesetProfile
        {
            Key = "test",
            TilesetResref = "tdt01",
            Lighting = new DungeonTileLighting
            {
                MainLight1 = 2,
                MainLight2 = 3,
                SourceLight1 = 4,
                SourceLight2 = 5
            },
            Atmosphere = new DungeonAreaAtmosphere
            {
                SkyBox = 7,
                DayNightCycle = false,
                IsNight = true,
                ChanceRain = 33,
                FogClipDist = 88f
            }
        };
        var composition = new DungeonComposition
        {
            Content = new DungeonDetail
            {
                ExitDoorResref = "_mdrn_dt_rough",
                ExitPlaceableResref = "structure_rubble"
            },
            Tileset = tilesetProfile,
            Layout = new DungeonLayoutProfile()
        };
        var result = new GenerationResult
        {
            Success = true,
            Resolved = resolved,
            PlannedDecorations =
            [
                new PlannedDecoration
                {
                    Resref = "structure_rubble",
                    Position = new Vector3(6f, 7f, 0.5f),
                    GroundZ = 1f,
                    Facing = 180f,
                    VisualScale = 1.25f
                }
            ]
        };
        var draft = new AreaGenerationDraft(
            new AreaGenerationSettings { ThemeKey = "test", Width = 2, Height = 2 },
            composition,
            new TilesetModel { Resref = "tdt01" },
            result);

        using (EditScope.EnterConstruction())
        {
            AreaTemplateFactory.PopulateNewArea(are, "test", "Test", "tdt01", 2, 2, 0, 0);
            GeneratedAreaDocumentPopulator.Populate(draft, workspace, are, git, gic);
        }

        AreaTiles.At(are, 1, 1)!.Value.TileId.Should().Be(23);
        AreaTiles.At(are, 1, 1)!.Value.Orientation.Should().Be(3);
        AreaTiles.HeightLevelOf(are, 1, 1).Should().Be(3);
        are.SkyBox.Should().Be(7);
        are.IsNight.Should().BeTrue();
        are.ChanceRain.Should().Be(33);
        are.FogClipDist.Should().Be(88f);
        git.Fields.GetListOrEmpty("WaypointList").Should().HaveCount(2);
        git.Fields.GetListOrEmpty("Door List").Should().ContainSingle()
            .Which.GetStringOrNull("Tag").Should().Be("PG_DOOR_EXIT_1");
        git.Fields.GetListOrEmpty("Placeable List").Should().HaveCount(2);
        git.Fields.GetListOrEmpty("Placeable List")
            .Select(instance => instance.GetStringOrNull("Tag"))
            .Should().Equal("PG_TRANS_ENT_1", "PG_DEC_1");
        gic.Fields.GetListOrEmpty("WaypointList").Should().HaveCount(2);
        gic.Fields.GetListOrEmpty("Door List").Should().ContainSingle();
        gic.Fields.GetListOrEmpty("Placeable List").Should().HaveCount(2);
    }

    private static (AreaGenerationAuthoringService Service, TilesetCatalog Tilesets) CreateAuthoringService()
    {
        var index = new ResourceIndex(
            baseLayer: null,
            new[]
            {
                new ResourceIndex.HakLayer(
                    "minecave-test",
                    Path.Combine(RepoRoot, "SWLOR_Haks", "sw_t_minecave"))
            });
        index.EnsureInitialized();
        var tilesets = new TilesetCatalog(index);
        return (new AreaGenerationAuthoringService(tilesets), tilesets);
    }

    private static AreaGenerationSettings CreateSettings(AreaGenerationAuthoringService service, int seed)
    {
        var theme = service.Definitions.Themes.Single(theme =>
            theme.ThemeKey.Equals(MineCaveDungeonDefinition.ThemeKey, StringComparison.OrdinalIgnoreCase));
        var tileset = service.Definitions.TilesetProfiles[StandardTilesetProfiles.Cavern];
        var layout = service.Definitions.LayoutProfiles[StandardLayoutProfiles.Organic];
        var defaults = new DungeonComposition
        {
            Content = theme,
            Tileset = tileset,
            Layout = layout
        }.BuildLayoutParameters();

        return new AreaGenerationSettings
        {
            ThemeKey = theme.ThemeKey,
            TilesetProfileKey = tileset.Key,
            LayoutProfileKey = layout.Key,
            Width = 16,
            Height = 16,
            Seed = seed,
            Overrides = new LayoutKnobOverrides
            {
                Style = defaults.Style,
                MinRooms = defaults.MinRooms,
                MaxRooms = defaults.MaxRooms,
                MinRoomCornerSize = defaults.MinRoomCornerSize,
                MaxRoomCornerSize = defaults.MaxRoomCornerSize,
                CorridorWidth = defaults.CorridorWidth,
                LoopFactorPercent = (int)Math.Round(defaults.LoopFactor * 100),
                OpenFillTargetPercent = (int)Math.Round(defaults.OpenFillTarget * 100),
                EntranceCount = defaults.EntranceCount,
                ExitCount = defaults.ExitCount,
                DoorTransitions = false,
                AccentEnabled = defaults.AccentDensity > 0,
                AccentDensityPercent = (int)Math.Round(defaults.AccentDensity * 100),
                FeatureDensityPercent = (int)Math.Round(defaults.FeatureDensity * 100),
                ElevationRegions = defaults.ElevationRegions,
                EnableDecorations = false,
                DecorationDensityPercent = 0
            }
        };
    }

    private static string CreateFixtureModule()
    {
        var moduleRoot = Path.Combine(Path.GetTempPath(), "swlor_procgen_" + Guid.NewGuid().ToString("N"));
        var source = CorpusLocator.ModuleDirectory;
        foreach (var folder in new[] { "are", "git", "gic", "ifo", "utc", "utp" })
            Directory.CreateDirectory(Path.Combine(moduleRoot, folder));

        File.Copy(Path.Combine(source, "are", "area_template.are.json"),
            Path.Combine(moduleRoot, "are", "area_template.are.json"));
        File.Copy(Path.Combine(source, "git", "area_template.git.json"),
            Path.Combine(moduleRoot, "git", "area_template.git.json"));
        File.Copy(Path.Combine(source, "gic", "area_template.gic.json"),
            Path.Combine(moduleRoot, "gic", "area_template.gic.json"));
        File.Copy(Path.Combine(source, "ifo", "module.ifo.json"),
            Path.Combine(moduleRoot, "ifo", "module.ifo.json"));
        File.Copy(Path.Combine(source, "utp", "_mdrn_placedoord.utp.json"),
            Path.Combine(moduleRoot, "utp", "_mdrn_placedoord.utp.json"));
        return moduleRoot;
    }
}
