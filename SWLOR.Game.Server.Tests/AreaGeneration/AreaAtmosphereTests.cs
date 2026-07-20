using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Acceptance gates for the family AREA atmosphere system (DungeonAreaAtmosphere): generated city
/// areas were shipping with the placeholder .are's daylight-white test-grid properties while the
/// hand-built flagship (pw_ar_narpromena) is a skybox-78 neon night city. These tests pin
/// (a) the exact mined per-family declarations -- fcx01 carries the flagship tuple verbatim, and
/// the five families with unambiguous hand-built module evidence (>= 3 exemplar areas agreeing on
/// the full core tuple) carry their mined values,
/// (b) palette-variant inheritance (a variant profile shares its family basis's atmosphere),
/// (c) that families WITHOUT unambiguous evidence declare nothing (both output paths then keep
/// their existing defaults), and
/// (d) the .are patching path (AreaAtmosphereAreWriter) that SWLOR.ProcgenReview's EmitArea runs,
/// against the real gen_placeholder1 module JSON.
/// </summary>
public class AreaAtmosphereTests
{
    private static Dictionary<string, DungeonTilesetProfile> BuildAllProfiles()
    {
        var profiles = new Dictionary<string, DungeonTilesetProfile>();
        foreach (var (key, profile) in new StandardTilesetProfiles().BuildTilesetProfiles())
            profiles[key] = profile;
        foreach (var (key, profile) in new BaseGameTilesetProfiles().BuildTilesetProfiles())
            profiles[key] = profile;
        DungeonTilesetPaletteInheritance.Apply(profiles);
        return profiles;
    }

    /// <summary>
    /// The hand-built flagship's own .are values (pw_ar_narpromena, verified directly against the
    /// module JSON): the atmosphere every fcx01 city composition must carry.
    /// </summary>
    private static void AssertFlagshipAtmosphere(DungeonAreaAtmosphere a)
    {
        a.Should().NotBeNull();
        a.SkyBox.Should().Be(78);
        a.DayNightCycle.Should().BeFalse();
        a.IsNight.Should().BeTrue();
        a.SunAmbientColor.Should().Be(6566450);
        a.SunDiffuseColor.Should().Be(16777215);
        a.MoonAmbientColor.Should().Be(5987195);
        a.MoonDiffuseColor.Should().Be(5987248);
        a.SunFogAmount.Should().Be(0);
        a.SunFogColor.Should().Be(9535080);
        a.MoonFogAmount.Should().Be(0);
        a.MoonFogColor.Should().Be(2368329);
        a.SunShadows.Should().BeTrue();
        a.MoonShadows.Should().BeTrue();
        a.ShadowOpacity.Should().Be(50);
        a.WindPower.Should().Be(0);
        a.ChanceRain.Should().Be(0);
        a.ChanceSnow.Should().Be(0);
        a.ChanceLightning.Should().Be(0);
        a.LightingScheme.Should().Be(0);
        a.FogClipDist.Should().Be(130f);
        a.LoadScreenId.Should().BeNull();
    }

    [TestCase(BaseGameTilesetProfiles.FutCity)]
    [TestCase(BaseGameTilesetProfiles.FutCityPlaza)]
    public void FutCityFamily_CarriesTheFlagshipNightCityAtmosphere(string profileKey)
    {
        var profiles = BuildAllProfiles();
        AssertFlagshipAtmosphere(profiles[profileKey].Atmosphere);
    }

    /// <summary>
    /// The SWLOR standard windowless-interior tuple three families' hand-built exemplars share
    /// (tdc01: 3/5 areas, tdm01: 4/13, zsf01: 3/9 -- see the profile declarations' citations).
    /// </summary>
    [TestCase(BaseGameTilesetProfiles.Crypt)]
    [TestCase(BaseGameTilesetProfiles.CryptGrey)]
    [TestCase(BaseGameTilesetProfiles.CryptDwarven)]
    [TestCase(BaseGameTilesetProfiles.MinesAndCaverns)]
    [TestCase(BaseGameTilesetProfiles.MinesAndCavernsDesert)]
    [TestCase(BaseGameTilesetProfiles.MinesAndCavernsOrganic)]
    [TestCase(BaseGameTilesetProfiles.MinesAndCavernsCity)]
    [TestCase(BaseGameTilesetProfiles.MinesAndCavernsTracks)]
    [TestCase(BaseGameTilesetProfiles.MinesAndCavernsDesertTracks)]
    [TestCase(BaseGameTilesetProfiles.MinesAndCavernsOrganicTracks)]
    [TestCase(StandardTilesetProfiles.Facility)]
    public void DarkInteriorFamilies_CarryTheStandardInteriorAtmosphere(string profileKey)
    {
        var profiles = BuildAllProfiles();
        var a = profiles[profileKey].Atmosphere;
        a.Should().NotBeNull();
        a.SkyBox.Should().Be(0);
        a.DayNightCycle.Should().BeFalse();
        a.IsNight.Should().BeTrue();
        a.SunAmbientColor.Should().Be(0);
        a.SunDiffuseColor.Should().Be(0);
        a.MoonAmbientColor.Should().Be(2960685);
        a.MoonDiffuseColor.Should().Be(6457991);
        a.MoonFogAmount.Should().Be(5);
        a.SunShadows.Should().BeFalse();
        a.MoonShadows.Should().BeFalse();
        a.ShadowOpacity.Should().Be(60);
        a.WindPower.Should().Be(0);
        a.LightingScheme.Should().Be(13);
        a.FogClipDist.Should().Be(45f);
        a.LoadScreenId.Should().BeNull();
    }

    [TestCase(BaseGameTilesetProfiles.Desert)]
    [TestCase(BaseGameTilesetProfiles.DesertRoad)]
    public void DesertFamily_CarriesTheTatooineDaylightAtmosphere(string profileKey)
    {
        var profiles = BuildAllProfiles();
        var a = profiles[profileKey].Atmosphere;
        a.Should().NotBeNull();
        a.SkyBox.Should().Be(77);
        a.DayNightCycle.Should().BeTrue();
        a.IsNight.Should().BeFalse();
        a.SunAmbientColor.Should().Be(3952475);
        a.SunDiffuseColor.Should().Be(7325921);
        a.MoonDiffuseColor.Should().Be(132358);
        a.SunFogAmount.Should().Be(10);
        a.SunFogColor.Should().Be(4890809);
        a.MoonFogAmount.Should().Be(10);
        a.MoonFogColor.Should().Be(2178364);
        a.SunShadows.Should().BeTrue();
        a.MoonShadows.Should().BeTrue();
        a.WindPower.Should().Be(2);
        a.LightingScheme.Should().Be(6);
        a.FogClipDist.Should().Be(70f);
        a.LoadScreenId.Should().Be(69);
    }

    [TestCase(BaseGameTilesetProfiles.RuralGrass)]
    [TestCase(BaseGameTilesetProfiles.RuralGrassGoodCastle)]
    [TestCase(BaseGameTilesetProfiles.RuralGrassEvilCastle)]
    [TestCase(BaseGameTilesetProfiles.RuralGrassWater)]
    public void RuralGrassFamily_CarriesTheGrasslandDaylightAtmosphere(string profileKey)
    {
        var profiles = BuildAllProfiles();
        var a = profiles[profileKey].Atmosphere;
        a.Should().NotBeNull();
        a.SkyBox.Should().Be(0);
        a.DayNightCycle.Should().BeTrue();
        a.IsNight.Should().BeFalse();
        a.SunAmbientColor.Should().Be(6566450);
        a.SunDiffuseColor.Should().Be(16777215);
        a.MoonDiffuseColor.Should().Be(13132900);
        a.SunFogAmount.Should().Be(0);
        a.SunFogColor.Should().Be(9535080);
        a.MoonFogColor.Should().Be(6566450);
        a.SunShadows.Should().BeTrue();
        a.MoonShadows.Should().BeTrue();
        a.WindPower.Should().Be(0);
        a.FogClipDist.Should().Be(45f);
    }

    /// <summary>
    /// The atmosphere is declared ONLY where the hand-built module evidence is unambiguous
    /// (>= 3 exemplar areas agreeing on the full core tuple, no dead tie). Every other family --
    /// including the recorded ties (udp2 4-vs-4, tjsb0 3-vs-3) and every family with fewer than 3
    /// module exemplars -- must stay undeclared so both output paths keep their existing defaults.
    /// </summary>
    [Test]
    public void FamiliesWithoutUnambiguousEvidence_DeclareNoAtmosphere()
    {
        var declaredTilesets = new[] { "fcx01", "tdc01", "tdm01", "zsf01", "ttd01", "ttr01" };
        var profiles = BuildAllProfiles();

        foreach (var profile in profiles.Values)
        {
            if (declaredTilesets.Contains(profile.TilesetResref))
                profile.Atmosphere.Should().NotBeNull(
                    $"'{profile.Key}' belongs to evidence-backed family '{profile.TilesetResref}'");
            else
                profile.Atmosphere.Should().BeNull(
                    $"'{profile.Key}' ({profile.TilesetResref}) has no unambiguous hand-built atmosphere evidence");
        }
    }

    [Test]
    public void ResolveAtmosphere_MirrorsDecorationProfileResolution()
    {
        var atmosphere = new DungeonAreaAtmosphere { SkyBox = 1 };
        var named = new DungeonAreaAtmosphere { SkyBox = 2 };
        var profile = new DungeonTilesetProfile { Atmosphere = atmosphere };
        profile.AtmosphereProfiles["stormy"] = named;

        // Standard fallback.
        profile.ResolveAtmosphere(null).Should().BeSameAs(atmosphere);
        profile.ResolveAtmosphere(string.Empty).Should().BeSameAs(atmosphere);
        // Theme declaration selects a named profile (case-insensitive, like decoration profiles).
        profile.ResolveAtmosphere("STORMY").Should().BeSameAs(named);
        // A request override wins over the theme declaration.
        profile.ResolveAtmosphere("stormy", "unknown-name").Should().BeSameAs(atmosphere,
            "an undeclared name falls back to the standard atmosphere, mirroring decoration profiles");
        profile.ResolveAtmosphere(null, "stormy").Should().BeSameAs(named);
        // No declarations at all resolves to null -- callers change nothing.
        new DungeonTilesetProfile().ResolveAtmosphere("stormy").Should().BeNull();
    }

    /// <summary>
    /// The offline emission path, against the REAL placeholder module JSON ProcgenReview clones:
    /// every atmosphere field lands with its exact value, non-atmosphere fields stay untouched,
    /// and the result is still valid JSON. LoadScreenID stays at the placeholder's value when the
    /// atmosphere declares none.
    /// </summary>
    [Test]
    public void AreWriter_PatchesTheFlagshipAtmosphereIntoThePlaceholderAre()
    {
        var root = TilesetTestSource.FindRepositoryRoot().FullName;
        var arePath = Path.Combine(root, "Module", "are", "gen_placeholder1.are.json");
        var original = File.ReadAllText(arePath);

        var profiles = BuildAllProfiles();
        var atmosphere = profiles[BaseGameTilesetProfiles.FutCity].ResolveAtmosphere(null);
        var patched = AreaAtmosphereAreWriter.Apply(original, atmosphere);

        var json = JsonNode.Parse(patched);
        int Value(string field) => json[field]["value"].GetValue<int>();

        Value("SkyBox").Should().Be(78);
        Value("DayNightCycle").Should().Be(0);
        Value("IsNight").Should().Be(1);
        Value("SunAmbientColor").Should().Be(6566450);
        Value("SunDiffuseColor").Should().Be(16777215);
        Value("MoonAmbientColor").Should().Be(5987195);
        Value("MoonDiffuseColor").Should().Be(5987248);
        Value("SunFogAmount").Should().Be(0);
        Value("SunFogColor").Should().Be(9535080);
        Value("MoonFogAmount").Should().Be(0);
        Value("MoonFogColor").Should().Be(2368329);
        Value("SunShadows").Should().Be(1);
        Value("MoonShadows").Should().Be(1);
        Value("ShadowOpacity").Should().Be(50);
        Value("WindPower").Should().Be(0);
        Value("ChanceRain").Should().Be(0);
        Value("ChanceSnow").Should().Be(0);
        Value("ChanceLightning").Should().Be(0);
        Value("LightingScheme").Should().Be(0);
        json["FogClipDist"]["value"].GetValue<float>().Should().Be(130f);

        // LoadScreenId is null on the flagship atmosphere -- the placeholder's value survives.
        var originalJson = JsonNode.Parse(original);
        Value("LoadScreenID").Should().Be(originalJson["LoadScreenID"]["value"].GetValue<int>());

        // Untouched fields survive byte-for-byte semantics: tileset, size, tile list untouched.
        json["Tileset"]["value"].GetValue<string>().Should()
            .Be(originalJson["Tileset"]["value"].GetValue<string>());
        json["Width"]["value"].GetValue<int>().Should()
            .Be(originalJson["Width"]["value"].GetValue<int>());

        // Null atmosphere is the documented no-op.
        AreaAtmosphereAreWriter.Apply(original, null).Should().Be(original);
    }

    [Test]
    public void AreWriter_PatchesLoadScreenOnlyWhenDeclared()
    {
        var root = TilesetTestSource.FindRepositoryRoot().FullName;
        var arePath = Path.Combine(root, "Module", "are", "gen_placeholder1.are.json");
        var original = File.ReadAllText(arePath);

        var profiles = BuildAllProfiles();
        var atmosphere = profiles[BaseGameTilesetProfiles.Desert].ResolveAtmosphere(null);
        var patched = AreaAtmosphereAreWriter.Apply(original, atmosphere);

        var json = JsonNode.Parse(patched);
        json["LoadScreenID"]["value"].GetValue<int>().Should().Be(69,
            "ttd01's evidence agrees on the Tatooine loadscreen (18/20 agreeing areas)");
        json["FogClipDist"]["value"].GetValue<float>().Should().Be(70f);
        json["WindPower"]["value"].GetValue<int>().Should().Be(2);
    }

    /// <summary>
    /// Variant inheritance must share the SAME atmosphere instance as the family basis (the
    /// palette-inheritance convention: values are read-only after build, so reference sharing is
    /// the documented behavior, not a copy).
    /// </summary>
    [Test]
    public void PaletteVariants_ShareTheFamilyBasisAtmosphereInstance()
    {
        var profiles = BuildAllProfiles();
        profiles[BaseGameTilesetProfiles.FutCityPlaza].Atmosphere.Should()
            .BeSameAs(profiles[BaseGameTilesetProfiles.FutCity].Atmosphere);
        profiles[BaseGameTilesetProfiles.CryptGrey].Atmosphere.Should()
            .BeSameAs(profiles[BaseGameTilesetProfiles.Crypt].Atmosphere);
        profiles[BaseGameTilesetProfiles.DesertRoad].Atmosphere.Should()
            .BeSameAs(profiles[BaseGameTilesetProfiles.Desert].Atmosphere);
    }
}
