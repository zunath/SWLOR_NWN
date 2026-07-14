using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.ContentBuilder.Models;
using SWLOR.ContentBuilder.Services;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Round-trips SWLOR.ContentBuilder's .swproj save/load/validate stack (ProjectFileService +
/// ContentBuilderProjectFile), specifically the decoration toggle/density fields added on top of
/// the existing v1 project file format. ProjectFileService/DefinitionCatalog/AreaSettingsBounds/
/// LayoutSupportRules/TilesetModelCache/RepoPaths are linked directly into this test project (see
/// SWLOR.Game.Server.Tests.csproj) rather than referenced via ProjectReference: the enclosing
/// SWLOR.ContentBuilder.csproj is net8.0-windows/UseWPF (for MainWindow etc.), which a plain net8.0
/// test project cannot ProjectReference (NU1201), but none of these specific files touch WPF.
/// </summary>
public class ContentBuilderProjectFileServiceTests
{
    private static DefinitionCatalog _catalog;

    [OneTimeSetUp]
    public void SetUpCatalog()
    {
        // Real discovery over the production theme/tileset/layout definitions (same convention
        // DungeonContentPlacer/SWLOR.ProcgenReview use) -- guarantees the theme/tileset/layout keys
        // used below are ones ProjectFileService's validation will actually accept.
        _catalog = new DefinitionCatalog();
    }

    private static DungeonDetail FirstTheme()
    {
        _catalog.Themes.Should().NotBeEmpty("at least one dungeon theme must be registered for this test to be meaningful");
        return _catalog.Themes[0];
    }

    /// <summary>
    /// Builds the minimal valid v1 project-file JSON (mirroring MainWindow's own default slider
    /// values) for the given theme's own default tileset/layout profiles, WITHOUT the
    /// "decorationsEnabled"/"decorationDensityPercent" fields present at all -- exactly what a
    /// project file saved before decorations existed looks like on disk.
    /// </summary>
    private static string BuildV1JsonWithoutDecorationFields(DungeonDetail theme)
    {
        return $$"""
        {
          "Version": 1,
          "AreaSettings": {
            "ThemeKey": "{{theme.ThemeKey}}",
            "TilesetProfileKey": "{{theme.TilesetProfileKey}}",
            "LayoutProfileKey": "{{theme.LayoutProfileKey}}",
            "Width": 16,
            "Height": 16,
            "Style": "RoomsAndCorridors",
            "MinRooms": 4,
            "MaxRooms": 8,
            "MinRoomSize": 3,
            "MaxRoomSize": 7,
            "CorridorWidth": 1,
            "LoopFactorPercent": 25,
            "OrganicFillPercent": 45,
            "AccentEnabled": false,
            "AccentDensityPercent": 5,
            "FeatureDensityPercent": 5,
            "ElevationRegions": 0,
            "Entrances": 1,
            "Exits": 1,
            "DoorTransitions": true,
            "Seed": 4242,
            "PreviewMode": "schematic",
            "RoomOverlay": false
          },
          "Batch": []
        }
        """;
    }

    [Test]
    public void ValidateJson_V1FileWithoutDecorationFields_DefaultsToEnabledAtFullDensity()
    {
        var theme = FirstTheme();
        var json = BuildV1JsonWithoutDecorationFields(theme);

        var result = ProjectFileService.ValidateJson(json, _catalog);

        result.Success.Should().BeTrue(result.Error);
        result.File.Version.Should().Be(1);
        result.File.AreaSettings.DecorationsEnabled.Should().BeTrue(
            "a v1 file saved before decorations existed has no decorationsEnabled key and must default on");
        result.File.AreaSettings.DecorationDensityPercent.Should().Be(100,
            "a v1 file saved before decorations existed has no decorationDensityPercent key and must default to 100%");
    }

    [Test]
    public void SaveAndValidate_RoundTripsExplicitDecorationSettings()
    {
        var theme = FirstTheme();
        var file = new ContentBuilderProjectFile
        {
            Version = ProjectFileService.CurrentVersion,
            AreaSettings = new AreaSettingsFile
            {
                ThemeKey = theme.ThemeKey,
                TilesetProfileKey = theme.TilesetProfileKey,
                LayoutProfileKey = theme.LayoutProfileKey,
                Width = 16,
                Height = 16,
                Style = "RoomsAndCorridors",
                MinRooms = 4,
                MaxRooms = 8,
                MinRoomSize = 3,
                MaxRoomSize = 7,
                CorridorWidth = 1,
                LoopFactorPercent = 25,
                OrganicFillPercent = 45,
                AccentEnabled = false,
                AccentDensityPercent = 5,
                FeatureDensityPercent = 5,
                ElevationRegions = 0,
                Entrances = 1,
                Exits = 1,
                DoorTransitions = true,
                DecorationsEnabled = false,
                DecorationDensityPercent = 175,
                Seed = 4242,
                PreviewMode = "schematic",
                RoomOverlay = false
            },
            Batch = new List<AreaBatchFileEntry>
            {
                new()
                {
                    ThemeKey = theme.ThemeKey,
                    Seed = 777,
                    Size = 16,
                    EnableDecorations = false,
                    DecorationDensityPercent = 60,
                    Parameters = new DungeonComposition
                    {
                        Content = theme,
                        Tileset = _catalog.TilesetProfiles[theme.TilesetProfileKey],
                        Layout = _catalog.LayoutProfiles[theme.LayoutProfileKey]
                    }.BuildLayoutParameters()
                }
            }
        };

        var json = ProjectFileService.Serialize(file);
        var result = ProjectFileService.ValidateJson(json, _catalog);

        result.Success.Should().BeTrue(result.Error);
        result.File.AreaSettings.DecorationsEnabled.Should().BeFalse();
        result.File.AreaSettings.DecorationDensityPercent.Should().Be(175);
        result.File.Batch.Should().HaveCount(1);
        result.File.Batch[0].EnableDecorations.Should().BeFalse();
        result.File.Batch[0].DecorationDensityPercent.Should().Be(60);
    }

    [Test]
    public void ValidateJson_NegativeDecorationDensity_Fails()
    {
        var theme = FirstTheme();
        var json = BuildV1JsonWithoutDecorationFields(theme)
            .Replace("\"DoorTransitions\": true,", "\"DoorTransitions\": true,\n            \"DecorationDensityPercent\": -5,");

        var result = ProjectFileService.ValidateJson(json, _catalog);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Decoration Density");
    }

    [Test]
    public void ValidateJson_DecorationDensityAboveMax_ClampsTo200()
    {
        var theme = FirstTheme();
        var json = BuildV1JsonWithoutDecorationFields(theme)
            .Replace("\"DoorTransitions\": true,", "\"DoorTransitions\": true,\n            \"DecorationDensityPercent\": 9000,");

        var result = ProjectFileService.ValidateJson(json, _catalog);

        result.Success.Should().BeTrue(result.Error);
        result.File.AreaSettings.DecorationDensityPercent.Should().Be(AreaSettingsBounds.DecorationDensityPercentMax);
    }
}
