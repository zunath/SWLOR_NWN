using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.WeatherService;
using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public class WeatherAreaConfigurationTests
{
    // GENERAL/Interior metadata from the shipped .set resources. These tilesets
    // provide roofs; the three reviewed open-air layouts below are deliberate exceptions.
    private static readonly HashSet<string> InteriorTilesets = new(StringComparer.OrdinalIgnoreCase)
    {
        "fifi", "flow_pa", "net01", "shp02", "sjm01", "tbw01", "tbx78", "tcdh0", "tdc01", "tdm01",
        "tdr01", "tds01", "tdt01", "tfb01", "tib01", "tii01", "tin01", "tjsb0", "tmi", "tni02",
        "tqq01", "tsw01", "udp2", "vmr01", "zdc01", "zde01", "zdm01", "zib01", "zid01", "zin01", "zsf01"
    };
    private static readonly HashSet<string> OutdoorLayouts = new()
    {
        "pw_sc_canyonpit", // Tatooine capstone arena, open canyon.
        "valkorrdung1b", // Korriban fortress courtyard, skybox 10.
        "vrotrkorracadrft" // Academy rooftop, authored rain and visible sky.
    };

    private sealed record Area(string Resource, string Name, string Tileset, int Flags, PlanetType Planet,
        string Override, bool Space, int HeatModifier, int HumidityModifier, int WindModifier);

    [Test]
    public void ModuleCorpus_InteriorTilesetsProvideShelter_WhileReviewedOutdoorLayoutsRemainOpen()
    {
        var areas = ReadAreas();
        var exposedInteriors = areas.Where(a => InteriorTilesets.Contains(a.Tileset) && (a.Flags & 3) == 0)
            .Select(a => a.Resource);
        exposedInteriors.Should().BeEquivalentTo(OutdoorLayouts);
        (areas.Single(a => a.Resource == "pwsc_eshstarport").Flags & 1).Should().Be(1,
            "Eshan's starport is enclosed and must shelter players from weather");
        (areas.Single(a => a.Resource == "korr_cavern").Flags & 2).Should().Be(2);
    }

    [Test]
    public void ModuleCorpus_AllLiveOutdoorPlanetAreasHaveAnExplicitClimate()
    {
        var planets = WeatherPlanetDefinitions.GetPlanetClimates();
        var named = WeatherPlanetDefinitions.GetNamedClimates(planets);
        var areas = ReadAreas();
        foreach (var area in areas)
        {
            if (!string.IsNullOrWhiteSpace(area.Override))
                named.Should().ContainKey(area.Override, $"{area.Resource} must not silently lose its weather to a typo");
            if ((area.Flags & 3) != 0 || area.Space) continue;
            var climate = WeatherPlanetDefinitions.ResolveClimate(area.Planet, area.Override, planets, named);
            if (area.Name.StartsWith("[") || area.Name.StartsWith("*") || area.Resource == "area_template") continue;
            climate.Should().NotBeNull($"{area.Name} ({area.Resource}) needs an authored weather profile");
            climate!.IsSheltered.Should().BeFalse($"{area.Name} is an outdoor planet area");
        }
    }

    [Test]
    public void ModuleCorpus_AreasWithTheSameClimateAndModifiersShareOneWeatherSnapshotPerFront()
    {
        var planets = WeatherPlanetDefinitions.GetPlanetClimates();
        var named = WeatherPlanetDefinitions.GetNamedClimates(planets);
        var areas = ReadAreas().Where(area => (area.Flags & 3) == 0 && !area.Space &&
            !area.Name.StartsWith("[") && !area.Name.StartsWith("*") && area.Resource != "area_template").ToArray();
        var pattern = new WeatherPattern();
        pattern.TryAdvance(new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc), 6, false, _ => 0)
            .Should().BeTrue();
        var regions = new WeatherRegionCache();
        var snapshots = new List<(WeatherClimate Climate, int Heat, int Humidity, int Wind, WeatherConditions Conditions)>();

        foreach (var area in areas)
        {
            var climate = WeatherPlanetDefinitions.ResolveClimate(area.Planet, area.Override, planets, named);
            climate.Should().NotBeNull($"{area.Name} ({area.Resource}) must resolve before entering a weather region");
            var conditions = regions.GetConditions(pattern, climate!, area.HeatModifier, area.HumidityModifier,
                area.WindModifier, max => max - 1);
            var existing = snapshots.FirstOrDefault(snapshot => ReferenceEquals(snapshot.Climate, climate) &&
                snapshot.Heat == area.HeatModifier && snapshot.Humidity == area.HumidityModifier &&
                snapshot.Wind == area.WindModifier);
            if (existing.Conditions != null)
            {
                regions.GetConditions(pattern, climate!, area.HeatModifier, area.HumidityModifier,
                    area.WindModifier, _ => throw new AssertionException(
                    $"{area.Name} ({area.Resource}) rerolled a region which should already exist"));
                conditions.Should().BeSameAs(existing.Conditions,
                    $"{area.Name} ({area.Resource}) must join its existing climate region without rerolling");
            }
            else
                snapshots.Add((climate!, area.HeatModifier, area.HumidityModifier, area.WindModifier, conditions));
        }

        // A map first entered after the weather front changes must use the new shared
        // snapshot and must not independently roll weather on map entry.
        pattern.TryAdvance(pattern.NextChangeUtc, 6, false, _ => 0).Should().BeTrue();
        var updatedByRegion = new List<(WeatherClimate Climate, int Heat, int Humidity, int Wind, WeatherConditions Conditions)>();
        foreach (var area in areas)
        {
            var climate = WeatherPlanetDefinitions.ResolveClimate(area.Planet, area.Override, planets, named)!;
            var conditions = regions.GetConditions(pattern, climate, area.HeatModifier, area.HumidityModifier,
                area.WindModifier, max => max - 1);
            var existing = updatedByRegion.FirstOrDefault(snapshot => ReferenceEquals(snapshot.Climate, climate) &&
                snapshot.Heat == area.HeatModifier && snapshot.Humidity == area.HumidityModifier &&
                snapshot.Wind == area.WindModifier);
            if (existing.Conditions != null)
            {
                regions.GetConditions(pattern, climate, area.HeatModifier, area.HumidityModifier,
                    area.WindModifier, _ => throw new AssertionException(
                    $"{area.Name} ({area.Resource}) rerolled a region after the front changed"));
                conditions.Should().BeSameAs(existing.Conditions,
                    $"{area.Name} ({area.Resource}) must join the updated shared climate region");
            }
            else
                updatedByRegion.Add((climate, area.HeatModifier, area.HumidityModifier, area.WindModifier, conditions));
        }
        foreach (var previous in snapshots)
            updatedByRegion.Should().Contain(snapshot => ReferenceEquals(snapshot.Climate, previous.Climate) &&
                snapshot.Heat == previous.Heat && snapshot.Humidity == previous.Humidity &&
                snapshot.Wind == previous.Wind && !ReferenceEquals(snapshot.Conditions, previous.Conditions),
                "each shared region should replace its weather snapshot exactly once per front");
    }

    [Test]
    public void StationsAreAlwaysSheltered_AndEveryRegisteredPlanetHasAWeatherPolicy()
    {
        var planets = WeatherPlanetDefinitions.GetPlanetClimates();
        var areas = ReadAreas();
        foreach (var planet in Planet.GetAllPlanets().Keys) planets.Should().ContainKey(planet);
        foreach (var area in areas.Where(a => a.Planet is PlanetType.CZ220 or PlanetType.SmugglersMoonStation))
        {
            planets[area.Planet].IsSheltered.Should().BeTrue();
            (area.Flags & 3).Should().NotBe(0, $"{area.Name} is inside a sealed station");
        }
    }

    [Test]
    public void FrozenWastes_PreserveAuthoredPermanentSnow_InEveryWeatherFront()
    {
        var area = ReadAreas().Single(a => a.Resource == "hutlar_frozen_wa");
        var climate = WeatherPlanetDefinitions.GetPlanetClimates()[area.Planet];
        for (var heat = -1; heat <= 12; heat++)
        for (var humidity = 1; humidity <= 10; humidity++)
            WeatherConditions.Create(heat, humidity, 5, climate, 0, area.HumidityModifier, 0, false,
                WeatherStorm.None, max => max - 1).Precipitation.Should().Be(Precipitation.Snow);
    }

    private static List<Area> ReadAreas()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Module"))) root = root.Parent;
        root.Should().NotBeNull();
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        typeof(Planet).GetMethod("CachePlanets", flags)!.Invoke(null, null);
        var resolveName = typeof(Planet).GetMethod("ResolvePlanetTypeByAreaName", flags)!;
        var areas = new List<Area>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root!.FullName, "Module", "are"), "*.are.json"))
        {
            var resource = Path.GetFileName(file).Replace(".are.json", "");
            using var are = JsonDocument.Parse(File.ReadAllText(file));
            using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "git", resource + ".git.json")));
            var data = are.RootElement;
            var locals = new Dictionary<string, JsonElement>();
            foreach (var source in new[] { data, git.RootElement.GetProperty("AreaProperties").GetProperty("value"), git.RootElement })
                if (source.TryGetProperty("VarTable", out var table))
                    foreach (var variable in table.GetProperty("value").EnumerateArray())
                        locals[variable.GetProperty("Name").GetProperty("value").GetString()!] = variable.GetProperty("Value").GetProperty("value");
            var name = data.GetProperty("Name").GetProperty("value").GetProperty("0").GetString()!;
            var space = name.StartsWith("Space -") || locals.TryGetValue("SPACE", out var spaceFlag) && spaceFlag.GetInt32() != 0;
            var planet = (PlanetType)resolveName.Invoke(null, new object[] { name })!;
            if (planet == PlanetType.Invalid && !space) planet = Planet.GetPlanetTypeByAreaResref(resource);
            if (planet == PlanetType.Invalid && locals.TryGetValue("PLANET_TYPE_ID", out var id)) planet = (PlanetType)id.GetInt32();
            areas.Add(new Area(resource, name, data.GetProperty("Tileset").GetProperty("value").GetString()!,
                data.GetProperty("Flags").GetProperty("value").GetInt32(), planet,
                locals.TryGetValue("VAR_WEATHER_CLIMATE", out var custom) ? custom.GetString()! : "", space,
                locals.TryGetValue("VAR_WEATHER_HEAT", out var heat) ? heat.GetInt32() : 0,
                locals.TryGetValue("VAR_WEATHER_HUMIDITY", out var humidity) ? humidity.GetInt32() : 0,
                locals.TryGetValue("VAR_WEATHER_WIND", out var wind) ? wind.GetInt32() : 0));
        }
        areas.Should().HaveCountGreaterThan(450, "the audit must cover the complete module, including prefabs");
        return areas;
    }
}
