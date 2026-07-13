using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// TunnelVocabularyCheck.SupportsTunnels verdicts, probe-verified directly against every real
/// generation and onboarded base-game tileset's .set data -- crosser SHAPE coverage, not just crosser
/// NAME presence. Two known gaps motivated this check (see TunnelVocabularyCheck's class doc for the
/// full shape inventory and why each shape is required):
///
/// - Illithid Interior (tii01) declares both "Doorway" and "Corridor" (a bare
///   Crossers.Contains("Doorway")/Contains("Corridor") check passes) but has no tile for a solid cell
///   carrying one Doorway edge together with two Corridor edges (a "T-with-port" junction) -- the ONE
///   shape (of the full inventory) it fails, confirmed by direct probing during development.
/// - Ruins (tdr01) declares "Alley" (a bare Crossers.Contains("Alley") check passes) but has no
///   side-open boundary tile carrying a lone Alley edge at all, so no Alley tunnel port can ever be
///   placed -- a hard, deterministic blocker, not an intermittent one.
/// </summary>
public class TunnelVocabularyCheckTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static readonly Dictionary<string, DungeonTilesetProfile> OnboardedProfiles =
        new BaseGameTilesetProfiles().BuildTilesetProfiles();

    private static string OpenTerrainFor(DungeonTilesetProfile profile, TilesetModel model)
    {
        return string.IsNullOrEmpty(profile.PrimaryOpenTerrain) ? model.FloorTerrain : profile.PrimaryOpenTerrain;
    }

    // ============================================================
    // The four original generation tilesets: full shape coverage, Corridor mode.
    // ============================================================

    [TestCase("tdt01", "floor")]
    [TestCase("tds01", "floor")]
    public void KnownGoodCorridorTilesets_SupportTunnels(string tilesetResref, string openTerrain)
    {
        var model = LoadTileset(tilesetResref);
        TunnelVocabularyCheck.SupportsTunnels(model, openTerrain, model.DefaultTerrain, CorridorCrosserType.Corridor)
            .Should().BeTrue($"{tilesetResref} is a verified-good Corridor tunnel tileset");
    }

    [Test]
    public void Zsf01_SupportsCorridorTunnels()
    {
        var model = LoadTileset("zsf01");
        TunnelVocabularyCheck.SupportsTunnels(model, "floor", model.DefaultTerrain, CorridorCrosserType.Corridor)
            .Should().BeTrue("zsf01/Facility is one of the four verified generation tilesets");
    }

    [Test]
    public void Vmr01_SupportsCorridorTunnels()
    {
        var model = LoadTileset("vmr01");
        TunnelVocabularyCheck.SupportsTunnels(model, "Plaza", model.DefaultTerrain, CorridorCrosserType.Corridor)
            .Should().BeTrue("vmr01/AncientRuin supports Corridor-mode tunnels on its Plaza terrain");
    }

    [Test]
    public void Vmr01_SupportsAlleyTunnels()
    {
        var model = LoadTileset("vmr01");
        TunnelVocabularyCheck.SupportsTunnels(model, "Plaza", model.DefaultTerrain, CorridorCrosserType.Alley)
            .Should().BeTrue("vmr01 is the only tileset with a verified Alley crosser SHAPE inventory, not just the crosser name");
    }

    // ============================================================
    // Known gaps: false verdicts.
    // ============================================================

    [Test]
    public void Tbw01Barrows_DoesNotSupportCorridorTunnels()
    {
        var model = LoadTileset("tbw01");
        TunnelVocabularyCheck.SupportsTunnels(model, "barrow", model.DefaultTerrain, CorridorCrosserType.Corridor)
            .Should().BeFalse("tbw01 declares a Corridor crosser but no Doorway crosser at all -- no port shape can ever exist");
    }

    [Test]
    public void Tii01Illithid_DoesNotSupportCorridorTunnels()
    {
        var model = LoadTileset("tii01");
        TunnelVocabularyCheck.SupportsTunnels(model, "Floor", model.DefaultTerrain, CorridorCrosserType.Corridor)
            .Should().BeFalse("tii01 is missing the T-with-port junction shape (Corridor+Corridor+Doorway) even though both crosser names are declared");
    }

    [Test]
    public void Tdr01Ruins_DoesNotSupportAlleyTunnels()
    {
        var model = LoadTileset("tdr01");
        TunnelVocabularyCheck.SupportsTunnels(model, "Floor", model.DefaultTerrain, CorridorCrosserType.Alley)
            .Should().BeFalse("tdr01 declares an Alley crosser but has no side-open boundary tile carrying a lone Alley edge");
    }

    [Test]
    public void Tdr01Ruins_SupportsCorridorTunnels()
    {
        var model = LoadTileset("tdr01");
        TunnelVocabularyCheck.SupportsTunnels(model, "Floor", model.DefaultTerrain, CorridorCrosserType.Corridor)
            .Should().BeTrue("tdr01's Corridor/Doorway vocabulary is complete even though its Alley vocabulary is not -- this is the downgrade destination");
    }

    // ============================================================
    // Every onboarded base-game tileset (Corridor mode): only Barrows and Illithid should fail.
    // ============================================================

    private static readonly string[] ExpectedUnsupported =
    {
        BaseGameTilesetProfiles.Barrows,
        BaseGameTilesetProfiles.IllithidInterior,
        // Crypt (Dwarven): tdc01's [Dwarven] palette keeps its door transitions on the non-canonical
        // "DwarvenDoorway" crosser (unlike [Grey]/[Tan], whose Door - Transition tiles use the
        // canonical "Doorway"), so no Doorway-port boundary shape exists against the variant's
        // DwarvenFloor open terrain -- Complex downgrades to OpenLane for this profile, the same
        // mechanism as Barrows' missing-Doorway gap. See BaseGameTilesetProfiles.CryptDwarven.
        BaseGameTilesetProfiles.CryptDwarven,
    };

    [TestCaseSource(typeof(OnboardedTilesetPipelineTests), nameof(OnboardedTilesetPipelineTests.OnboardedTilesetKeys))]
    public void OnboardedTilesets_SupportsTunnelsVerdictMatchesKnownGaps(string tilesetKey)
    {
        var profile = OnboardedProfiles[tilesetKey];
        var model = LoadTileset(profile.TilesetResref);
        var openTerrain = OpenTerrainFor(profile, model);

        var supports = TunnelVocabularyCheck.SupportsTunnels(
            model, openTerrain, profile.SecondaryOpenTerrain, model.DefaultTerrain, CorridorCrosserType.Corridor);

        var shouldSupport = System.Array.IndexOf(ExpectedUnsupported, tilesetKey) < 0;
        supports.Should().Be(shouldSupport,
            $"{tilesetKey} ({profile.TilesetResref}) SupportsTunnels verdict must match the known tile-inventory gaps");
    }

    // ============================================================
    // Multi-terrain districts: both open terrains must independently support the boundary shape.
    // ============================================================

    [Test]
    public void Zsf01Districts_SupportCorridorTunnelsOnBothTerrains()
    {
        var model = LoadTileset("zsf01");
        TunnelVocabularyCheck.SupportsTunnels(model, "floor", "Floor2", model.DefaultTerrain, CorridorCrosserType.Corridor)
            .Should().BeTrue("zsf01's Floor2 secondary district terrain has full boundary-shape coverage, same as its primary floor terrain");
    }

    [Test]
    public void Vmr01Districts_SupportCorridorTunnelsOnBothTerrains()
    {
        var model = LoadTileset("vmr01");
        TunnelVocabularyCheck.SupportsTunnels(model, "Plaza", "Floor", model.DefaultTerrain, CorridorCrosserType.Corridor)
            .Should().BeTrue("vmr01's Floor secondary district terrain has full boundary-shape coverage under Corridor mode");
    }

    /// <summary>
    /// vmr01's Floor secondary terrain does NOT actually have a resolvable Alley boundary shape, but
    /// this must never matter: RoomsAndCorridorsLayout's own useDistricts gate only activates a
    /// secondary-terrain room under CorridorCrosserType.Corridor (see
    /// MultiTerrainDistrictTests.AlleyCrosserType_NeverActivatesDistrictsEvenWithSecondaryOpenTerrainConfigured),
    /// so SupportsTunnels must skip the secondary-terrain probe entirely for Alley and judge purely on
    /// the primary terrain (which IS fully covered -- see Vmr01_SupportsAlleyTunnels above).
    /// </summary>
    [Test]
    public void Vmr01Districts_AlleySecondaryTerrainNeverGatesTheVerdict()
    {
        var model = LoadTileset("vmr01");
        TunnelVocabularyCheck.SupportsTunnels(model, "Plaza", "Floor", model.DefaultTerrain, CorridorCrosserType.Alley)
            .Should().BeTrue("Alley mode never activates districts, so an unsupported secondary-terrain boundary shape must not gate the verdict");
    }
}
