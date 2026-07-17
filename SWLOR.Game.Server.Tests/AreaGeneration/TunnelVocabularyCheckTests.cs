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
        // The exterior wave: ttd01/ttf01/ttf02 declare NO canonical "Corridor"/"Doorway" crosser at
        // all, and under the composed INVERTED solid (SolidTerrainOverride("Cliff") -- see the wave
        // comment in BaseGameTilesetProfiles) no crosser family of any name crosses the cliff mass
        // either (see ExteriorTilesets_CrosserFamiliesOnlyCrossTheWalkableGround below), so Complex
        // genuinely downgrades to OpenLane for all three -- the Barrows/Crypt-Dwarven fallback.
        BaseGameTilesetProfiles.Desert,
        BaseGameTilesetProfiles.Forest,
        BaseGameTilesetProfiles.ForestFacelift,
        // Forest (Platform): the same "no crosser family crosses the composed solid" gap, this time
        // against SolidTerrainOverride("Pit") -- none of Wall/Road/Stream/etc. cross Pit either. See
        // BaseGameTilesetProfiles.ForestPlatform's own doc comment.
        BaseGameTilesetProfiles.ForestPlatform,
        // Forest (Rural): same SolidTerrainOverride("Cliff") as the base Forest profile above (only
        // AccentTerrain/ReliefBlendTerrain differ) -- identical "no crosser family crosses Cliff" gap.
        // See BaseGameTilesetProfiles.ForestRural's own doc comment.
        BaseGameTilesetProfiles.ForestRural,
        // Round-3 exterior-tail-closure PaletteVariant profiles: each declares the SAME
        // SolidTerrainOverride("Cliff") + walkable Desert/Forest open terrain as Desert/Forest above,
        // and only declares a RampCrosser (not a TunnelCrossers pair) -- identical "no crosser family
        // crosses Cliff" gap. See BaseGameTilesetProfiles.DesertRoad/ForestCityWall/ForestMossWall/
        // ForestRuralWallOne/ForestRuralWallTwo/ForestRuralStream/ForestRoad/ForestStoneBridge's own
        // doc comments.
        BaseGameTilesetProfiles.DesertRoad,
        BaseGameTilesetProfiles.ForestCityWall,
        BaseGameTilesetProfiles.ForestMossWall,
        BaseGameTilesetProfiles.ForestRuralWallOne,
        BaseGameTilesetProfiles.ForestRuralWallTwo,
        BaseGameTilesetProfiles.ForestRuralStream,
        BaseGameTilesetProfiles.ForestRoad,
        BaseGameTilesetProfiles.ForestStoneBridge,
        // Forest (Good Castle) / Forest (Evil Castle): the same "no crosser family crosses the
        // composed solid" gap, this time against SolidTerrainOverride("GoodCastle")/("EvilCastle") --
        // none of Wall/Road/Stream/etc. cross either castle terrain (verified directly: each blends
        // only with Forest, 2/16 against every other terrain). See
        // BaseGameTilesetProfiles.ForestGoodCastle/ForestEvilCastle's own doc comment.
        BaseGameTilesetProfiles.ForestGoodCastle,
        BaseGameTilesetProfiles.ForestEvilCastle,
        // Forest (Marsh): same SolidTerrainOverride("Cliff") as the base Forest profile above (only
        // AccentTerrain differs) -- identical "no crosser family crosses Cliff" gap. See
        // BaseGameTilesetProfiles.ForestMarsh's own doc comment.
        BaseGameTilesetProfiles.ForestMarsh,
        // D20 Futuristic City SW (fcx01): only "pont"/"Routes"/"murs" crossers exist, none paired as a
        // Corridor/Doorway body+port set -- see BaseGameTilesetProfiles.FutCity's own doc comment.
        BaseGameTilesetProfiles.FutCity,
        BaseGameTilesetProfiles.FutCityPlaza,
        // Wave-5: D20 Modern Facility (tbx78) declares "doorway1/2/3", none literally "Doorway", and
        // D20 Office Interiors UDP (udp2) declares "Door" -- neither pairs as a canonical Corridor/
        // Doorway body+port set. See BaseGameTilesetProfiles.ModernFacility/OfficeInteriors' own doc comments.
        // D20 Secret Base (tjsb0) and Complex laps storage (tqq01) are DELIBERATELY NOT here: both
        // declare case-insensitive-canonical "corridor"/"Corridor" + "doorway"/"Doorway" and pass this
        // verdict check as TRUE (verified directly) -- the first onboarded profiles since fcx01's wave
        // to keep real Tunnel-mode support rather than downgrading.
        BaseGameTilesetProfiles.ModernFacility,
        BaseGameTilesetProfiles.OfficeInteriors,
        // udp2's six remaining district palettes: same "Door" crosser, no TunnelCrossers declared --
        // identical OpenLane downgrade as the base OfficeInteriors profile above. See
        // BaseGameTilesetProfiles.OfficeInteriorsService's own doc comment.
        BaseGameTilesetProfiles.OfficeInteriorsService,
        BaseGameTilesetProfiles.OfficeInteriorsTiled,
        BaseGameTilesetProfiles.OfficeInteriorsOfficeWood,
        BaseGameTilesetProfiles.OfficeInteriorsOfficeAlum,
        BaseGameTilesetProfiles.OfficeInteriorsFoyerL,
        BaseGameTilesetProfiles.OfficeInteriorsFoyerU,
        // [CEP] City Interior 1 (Elven/Sigil): each district's own hallway crosser (ElvenHallway/
        // SigilHallway) is a renamed BOTH-halves family with no resolvable boundary shape against the
        // canonical Corridor/Doorway probe this test runs (verified directly) -- neither profile
        // declares TunnelCrossers (only DoorSlotCrossers, which this check doesn't consult), so Complex
        // downgrades to OpenLane for both, the same Crypt-Dwarven/Barrows-style fallback. The base
        // [CEP] City Interior 1 profile is DELIBERATELY NOT here: it keeps the tileset's own canonical
        // "Corridor"/"Doorway" pair (Stairs - Up/Down, Exit - Corridor) and passes this verdict as TRUE.
        BaseGameTilesetProfiles.CepCityInteriorElven,
        BaseGameTilesetProfiles.CepCityInteriorSigil,
        // Rural Grass (ttr01): declares NO canonical "Corridor"/"Doorway" crosser at all (verified
        // directly), the same shape as ttd01/ttf01/jac01's own exterior wave -- Complex downgrades to
        // OpenLane for all four profiles (base + the three PaletteVariants) regardless of which
        // terrain each composes as Solid. See BaseGameTilesetProfiles.RuralGrass's own doc comment.
        BaseGameTilesetProfiles.RuralGrass,
        BaseGameTilesetProfiles.RuralGrassGoodCastle,
        BaseGameTilesetProfiles.RuralGrassEvilCastle,
        BaseGameTilesetProfiles.RuralGrassWater,
        // Rural Winter* (tts01): declares NO canonical "Corridor"/"Doorway" crosser at all (verified
        // directly), the same shape as RuralGrass's own exterior wave -- Complex downgrades to OpenLane
        // for all four profiles (base + the three PaletteVariants) regardless of which terrain each
        // composes as Solid. See BaseGameTilesetProfiles.RuralWinter's own doc comment.
        BaseGameTilesetProfiles.RuralWinter,
        BaseGameTilesetProfiles.RuralWinterGoodCastle,
        BaseGameTilesetProfiles.RuralWinterEvilCastle,
        BaseGameTilesetProfiles.RuralWinterWater,
        // Castle Exterior, Rural* (tno01): declares NO canonical "Corridor"/"Doorway" crosser at all
        // (verified directly -- its nine crossers are road/ridge/bridge/smallwall/sandbank/stonewall/
        // river/lists/listssmall), the same shape as the ttd01/ttf01/jac01/ttr01/tts01 exterior wave --
        // Complex downgrades to OpenLane for all six profiles (base + the five PaletteVariant
        // districts) regardless of which terrain each composes as Solid. See
        // BaseGameTilesetProfiles.CastleExteriorRural's own doc comment.
        BaseGameTilesetProfiles.CastleExteriorRural,
        BaseGameTilesetProfiles.CastleExteriorRuralVillage,
        BaseGameTilesetProfiles.CastleExteriorRuralCastleWall,
        BaseGameTilesetProfiles.CastleExteriorRuralKeep,
        BaseGameTilesetProfiles.CastleExteriorRuralWater,
        BaseGameTilesetProfiles.CastleExteriorRuralHarbor,
        // City Exterior* (tcn01): declares NO canonical "Corridor"/"Doorway" crosser at all (its ten
        // crossers are Wall/Stream/Dock/Bridge/Alley/SigilRoad/FieldDock/FieldBridge/GothicDock/
        // GothicBridge) -- the same shape as the tno01/ttd01/ttf01/jac01/ttr01/tts01/fcx01 exterior
        // wave. Complex downgrades to OpenLane under the canonical probe this test runs regardless of
        // which terrain each district composes as Solid; each profile's own TunnelCrossers("Dock"/
        // "FieldDock"/"GothicDock", ...) declaration is what gives it REAL Custom-mode Tunnel support
        // instead (verified via ProbeTool's direct TunnelVocabularyCheck.SupportsTunnels(Custom, ...)
        // call -- see BaseGameTilesetProfiles.CityExterior's own doc comment), which this canonical-only
        // check doesn't exercise.
        BaseGameTilesetProfiles.CityExterior,
        BaseGameTilesetProfiles.CityExteriorFieldstone,
        BaseGameTilesetProfiles.CityExteriorGothic,
        BaseGameTilesetProfiles.CityExteriorSigil,
        // Frozen Wastes* (tti01): declares ZERO crossers at all (verified directly -- Count=0 in the
        // .set data, more extreme than every prior exterior wave, which at least has SOME crosser
        // family even when none is the canonical Corridor/Doorway pair). Complex downgrades to
        // OpenLane for both profiles (base + the EvilCastle PaletteVariant) regardless of which
        // terrain each composes as Solid. See BaseGameTilesetProfiles.FrozenWastes' own doc comment.
        BaseGameTilesetProfiles.FrozenWastes,
        BaseGameTilesetProfiles.FrozenWastesEvilCastle,
        // Tropical* (ttz01): declares NO canonical "Corridor"/"Doorway" crosser at all (its 4 crossers
        // are stream/wall1/wall2/road), the same shape as ttr01/tts01/tno01/tcn01's own exterior wave --
        // Complex downgrades to OpenLane for all four profiles (base + Sand/Water/SandWater
        // PaletteVariants) regardless of which terrain each composes as Solid. See
        // BaseGameTilesetProfiles.Tropical's own doc comment.
        BaseGameTilesetProfiles.Tropical,
        BaseGameTilesetProfiles.TropicalSand,
        BaseGameTilesetProfiles.TropicalWater,
        BaseGameTilesetProfiles.TropicalSandWater,
        // Underdark* (ttu01): declares 5 crossers (Wall/Stream/Bridge/RuinWall/Slope), none a canonical
        // or near-canonical "Corridor"/"Doorway" pair -- Complex downgrades to OpenLane. See
        // BaseGameTilesetProfiles.Underdark's own doc comment.
        BaseGameTilesetProfiles.Underdark,
        // Early Winter 2 (trs02): declares 4 crossers (Stream/Wall/Ridge/Street), none a canonical or
        // near-canonical "Corridor"/"Doorway" pair -- Complex downgrades to OpenLane for both profiles
        // (base + Mountain inversion). See BaseGameTilesetProfiles.EarlyWinter's own doc comment.
        BaseGameTilesetProfiles.EarlyWinter,
        BaseGameTilesetProfiles.EarlyWinterMountain,
        // Medieval Rural 2 (trm02): declares 6 crossers (Road/Stream/Wall/Bridge/Ridge/Street), none a
        // canonical or near-canonical "Corridor"/"Doorway" pair -- Complex downgrades to OpenLane for
        // both profiles (base + Mountain inversion). See BaseGameTilesetProfiles.MedievalRural's own
        // doc comment.
        BaseGameTilesetProfiles.MedievalRural,
        BaseGameTilesetProfiles.MedievalRuralMountain,
        // Sea Ships (tss13): "gangplank" is this tileset's only declared crosser, and it is not a
        // canonical or near-canonical Corridor/Doorway pair -- Complex downgrades to OpenLane for the
        // base profile and all three PaletteVariants. See BaseGameTilesetProfiles.SeaShips' own doc
        // comment (including the CorridorStubChain hypothesis test confirming this stays correct even
        // if gangplank were declared as a Tunnel body crosser).
        BaseGameTilesetProfiles.SeaShips,
        BaseGameTilesetProfiles.SeaShipsCity,
        BaseGameTilesetProfiles.SeaShipsRural,
        BaseGameTilesetProfiles.SeaShipsTropical,
    };

    [TestCaseSource(typeof(OnboardedTilesetPipelineTests), nameof(OnboardedTilesetPipelineTests.OnboardedTilesetKeys))]
    public void OnboardedTilesets_SupportsTunnelsVerdictMatchesKnownGaps(string tilesetKey)
    {
        var profile = OnboardedProfiles[tilesetKey];
        var model = LoadTileset(profile.TilesetResref);
        var openTerrain = OpenTerrainFor(profile, model);
        var solidTerrain = string.IsNullOrEmpty(profile.SolidTerrainOverride) ? model.DefaultTerrain : profile.SolidTerrainOverride;

        var supports = TunnelVocabularyCheck.SupportsTunnels(
            model, openTerrain, profile.SecondaryOpenTerrain, solidTerrain, CorridorCrosserType.Corridor);

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

    // ============================================================
    // Custom crosser vocabulary: district-scoped alternate body/port families a tileset profile can
    // declare (see MacroLayoutParameters.TunnelBodyCrosser/DungeonTilesetProfile.TunnelBodyCrosser).
    // Empirically probed directly against the SWLOR_Haks-resolved .set data for every district family
    // this generalization pass investigated -- see BaseGameTilesetProfiles.CryptGrey/CryptDwarven/
    // MinesAndCavernsDesert/MinesAndCavernsOrganic for the wiring these verdicts justify.
    // ============================================================

    [Test]
    public void CryptGrey_SupportsCustomTunnels_GreyCorridorDoorway()
    {
        var model = LoadTileset("tdc01");
        TunnelVocabularyCheck.SupportsTunnels(
                model, "GreyFloor", string.Empty, model.DefaultTerrain, CorridorCrosserType.Custom, "GreyCorridor", "Doorway")
            .Should().BeTrue("tdc01's [Grey] district renames only its Tunnel body crosser (\"GreyCorridor\") and keeps the canonical \"Doorway\" port -- TILE515 and the GreyFloor|Wall boundary tiles carry it directly");
    }

    [Test]
    public void CryptDwarven_DoesNotSupportCustomTunnels_DwarvenCorridorDwarvenDoorway()
    {
        var model = LoadTileset("tdc01");
        TunnelVocabularyCheck.SupportsTunnels(
                model, "DwarvenFloor", string.Empty, model.DefaultTerrain, CorridorCrosserType.Custom, "DwarvenCorridor", "DwarvenDoorway")
            .Should().BeFalse("tdc01's [Dwarven] district renames BOTH halves of the pair and has no resolvable DwarvenFloor|Wall boundary tile carrying a lone \"DwarvenDoorway\" edge -- a genuine gap, not merely an unwired name (matches ExpectedUnsupported above)");
    }

    [Test]
    public void MinesAndCavernsDesert_SupportsCustomTunnels_DesertCorridorDoorway()
    {
        var model = LoadTileset("tdm01");
        TunnelVocabularyCheck.SupportsTunnels(
                model, "Desert", string.Empty, model.DefaultTerrain, CorridorCrosserType.Custom, "DesertCorridor", "Doorway")
            .Should().BeTrue("tdm01's [Desert] district renames only its Tunnel body crosser (\"DesertCorridor\") and keeps the canonical \"Doorway\" port, mirroring Crypt Grey's own shape");
    }

    [Test]
    public void MinesAndCavernsOrganic_SupportsCustomTunnels_OrganicCorridorDoorway()
    {
        var model = LoadTileset("tdm01");
        TunnelVocabularyCheck.SupportsTunnels(
                model, "Organic", string.Empty, model.DefaultTerrain, CorridorCrosserType.Custom, "OrganicCorridor", "Doorway")
            .Should().BeTrue("tdm01's [Organic] district renames only its Tunnel body crosser (\"OrganicCorridor\") and keeps the canonical \"Doorway\" port, mirroring Desert's own shape");
    }

    /// <summary>
    /// tdm01 ships a SECOND, independent alternate body family per district ("Tracks"/"DesertTracks"/
    /// "OrganicTracks") that is ALSO fully shape-verified -- confirmed here so the "stays unwired"
    /// documentation in BaseGameTilesetProfiles/TileCoverageCensusTests is a real, checked capability
    /// gap (one Tunnel body/port slot per profile) rather than an unverified assumption.
    /// </summary>
    [Test]
    public void MinesAndCavernsDesert_TracksFamilyAlsoSupportsCustomTunnels_ButIsNotWired()
    {
        var model = LoadTileset("tdm01");
        TunnelVocabularyCheck.SupportsTunnels(
                model, "Desert", string.Empty, model.DefaultTerrain, CorridorCrosserType.Custom, "DesertTracks", "Doorway")
            .Should().BeTrue("DesertTracks is a second, fully shape-verified alternate body family alongside DesertCorridor -- a tileset profile carries only one Tunnel body/port slot, so this stays unwired (see BaseGameTilesetProfiles.MinesAndCavernsDesert)");
    }

    /// <summary>
    /// The exterior wave's crosser families (Road/Wall/Trench/Stream, each a same-name body/port
    /// pair) all run across the WALKABLE Desert/Forest ground and never cross the Cliff mass:
    /// SupportsTunnels resolves the full shape inventory when the walkable terrain plays the solid
    /// role (a data fact about where the crossers live -- the .set's degenerate Default==walkable
    /// declaration), and fails under the composition's ACTUAL solid (SolidTerrainOverride("Cliff") --
    /// see the wave comment in BaseGameTilesetProfiles), which is why no exterior profile declares
    /// TunnelCrossers and Complex downgrades to OpenLane for all three (ExpectedUnsupported above).
    /// </summary>
    [TestCase("ttd01", "Desert", "Road")]
    [TestCase("ttd01", "Desert", "Wall")]
    [TestCase("ttd01", "Desert", "Trench")]
    [TestCase("ttf01", "Forest", "Road")]
    [TestCase("ttf01", "Forest", "Wall")]
    [TestCase("ttf01", "Forest", "Stream")]
    [TestCase("ttf02", "Forest", "Road")]
    [TestCase("ttf02", "Forest", "Wall")]
    [TestCase("ttf02", "Forest", "Stream")]
    public void ExteriorTilesets_CrosserFamiliesOnlyCrossTheWalkableGround(string tilesetResref, string walkableTerrain, string family)
    {
        var model = LoadTileset(tilesetResref);

        TunnelVocabularyCheck.SupportsTunnels(
                model, "Cliff", string.Empty, walkableTerrain, CorridorCrosserType.Custom, family, family)
            .Should().BeTrue($"{tilesetResref}'s {family}/{family} family fully resolves when the walkable {walkableTerrain} plays the solid role -- the crossers all run across the ground");

        TunnelVocabularyCheck.SupportsTunnels(
                model, walkableTerrain, string.Empty, "Cliff", CorridorCrosserType.Custom, family, family)
            .Should().BeFalse($"no {family} shape crosses the Cliff mass, the composition's actual solid -- this is why the exterior profiles declare no TunnelCrossers");
    }
}
