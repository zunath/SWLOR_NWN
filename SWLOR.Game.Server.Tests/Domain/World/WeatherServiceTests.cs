using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.WeatherService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;
using WeatherFacade = SWLOR.Game.Server.Service.Weather;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the weather instance against the scripted world: the engine rolls, areas, players and strike targets
/// come from <see cref="WeatherWorld"/>, the database is in memory, and the damage pipeline of the static combat
/// services is a table. The wall clock is the service's own (<c>DateTime.UtcNow</c>), so the tests rely on what holds
/// within one test: a front lasts an hour, a damage pulse six seconds and a lightning round a minute.
/// </summary>
[TestFixture]
public class WeatherServiceTests
{
    private const uint Meadow = 100;
    private const uint Dunes = 101;
    private const uint Glacier = 102;
    private const uint Hall = 103;
    private const uint Player = 5001;
    private const uint OtherPlayer = 5002;
    private const uint Staff = 5003;
    private const uint Dm = 5004;
    private const uint Possessed = 5005;
    private const uint Monster = 5006;
    private const uint Door = 5007;

    private WeatherWorld _world = null!;

    private WeatherWorld.WeatherServiceUnderTest Service => _world.Service;

    [SetUp]
    public void SetUp()
    {
        _world = new WeatherWorld();
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    private static WeatherClimate Climate(PlanetType planet) => WeatherPlanetDefinitions.GetPlanetClimates()[planet];

    /// <summary>The rolls that give a storm: the front (heat 12, humidity 10, wind 10), then a storm roll of 0.</summary>
    private void StormyFront() => _world.Rolls(4, 9, 9, 9, 0);

    private void EnterAs(uint creature, uint area)
    {
        _world.Engine.SetReturn("GetEnteringObject", creature);
        _world.Engine.SetReturn("OBJECT_SELF", area);
        Service.OnAreaEnter();
    }

    private int Rolled => _world.Engine.CountOf("Random");

    private int NativeSends => _world.Engine.CountOf("SetWeather");

    // ---- construction and the facade

    [Test]
    public void Building_the_instance_touches_neither_the_engine_nor_the_database()
    {
        _world.ClearCalls();
        var db = new InMemoryDBService();

        _ = new WeatherService(db, new WeatherWorld.StubPlanets(), new DMPresenceService());

        _world.Engine.Calls.Should().BeEmpty();
        db.Reads.Should().BeEmpty();
    }

    [Test]
    public void The_facade_keeps_the_handlers_of_the_old_static_class()
    {
        static string[] Scripts(string method) => typeof(WeatherFacade).GetMethod(method)!
            .GetCustomAttributes(typeof(NWNEventHandler), false)
            .Cast<NWNEventHandler>().Select(handler => handler.Script).ToArray();

        Scripts(nameof(WeatherFacade.LoadData)).Should().Equal(ScriptName.OnModuleCacheBefore);
        Scripts(nameof(WeatherFacade.InitializeWeather)).Should().Equal(ScriptName.OnModuleLoad);
        Scripts(nameof(WeatherFacade.OnAreaEnter)).Should().Equal(ScriptName.OnAreaEnter);
        Scripts(nameof(WeatherFacade.OnModuleHeartbeat)).Should().Equal(ScriptName.OnSwlorHeartbeat);
    }

    [Test]
    public void Loading_the_data_forgets_the_front_and_every_area()
    {
        _world.AddArea(Meadow);
        Service.SetWeather(Meadow);
        Rolled.Should().Be(5);

        Service.LoadData();
        Service.SetWeather(Meadow);

        Rolled.Should().Be(10, "the forgotten front is rolled again");
        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain, WeatherType.Rain);
    }

    // ---- which areas have weather

    [Test]
    public void The_module_and_objects_that_are_not_valid_have_no_weather()
    {
        Service.GetWeather(WeatherWorld.Module).Should().Be(Precipitation.Invalid);
        Service.GetWeather(WeatherWorld.Invalid).Should().Be(Precipitation.Invalid);
        Service.GetWeather(4242).Should().Be(Precipitation.Invalid);

        Rolled.Should().Be(0);
        NativeSends.Should().Be(0);
    }

    [Test]
    public void Interior_underground_and_space_areas_have_no_weather()
    {
        _world.AddArea(Meadow);
        _world.SetInterior(Meadow);
        _world.AddArea(Dunes);
        _world.SetUnderground(Dunes);
        _world.AddArea(Glacier);
        _world.SetInt(Glacier, "SPACE", 1);
        _world.AddArea(Hall);
        _world.Cast.Name(Hall, "space - Orbit");

        foreach (var area in new[] { Meadow, Dunes, Glacier, Hall })
            Service.GetWeather(area).Should().Be(Precipitation.Invalid);

        NativeSends.Should().Be(0);
    }

    [Test]
    public void An_area_whose_planet_has_no_climate_or_a_sheltered_one_has_no_weather()
    {
        _world.AddArea(Meadow, PlanetType.Invalid);
        _world.AddArea(Dunes, PlanetType.CZ220);
        _world.AddArea(Glacier);
        _world.SetString(Glacier, "VAR_WEATHER_CLIMATE", " none ");

        foreach (var area in new[] { Meadow, Dunes, Glacier })
            Service.GetWeather(area).Should().Be(Precipitation.Invalid);
    }

    [Test]
    public void An_authored_climate_name_replaces_the_planets_climate_and_a_typo_gives_no_weather()
    {
        _world.AddArea(Meadow, PlanetType.Tatooine);
        _world.AddArea(Dunes, PlanetType.Invalid);
        _world.AddArea(Glacier);
        _world.SetString(Meadow, "VAR_WEATHER_CLIMATE", "Hutlar");
        _world.SetString(Dunes, "VAR_WEATHER_CLIMATE", "kashyyyk");
        _world.SetString(Glacier, "VAR_WEATHER_CLIMATE", "typo");

        Service.GetWeather(Meadow).Should().Be(Precipitation.Snow, "the climate of the name is Hutlar's, whatever planet the area is on");
        Service.GetWeather(Dunes).Should().Be(Precipitation.Rain, "Kashyyyk is not a planet of the map and is found by name");
        Service.GetWeather(Glacier).Should().Be(Precipitation.Invalid);
    }

    [Test]
    public void The_climate_comes_from_the_planet_service()
    {
        _world.AddArea(Meadow, PlanetType.Tatooine);
        _world.AddArea(Dunes, PlanetType.Viscara);

        Service.GetHumidity(Meadow).Should().Be(2, "the desert is dry");
        Service.GetHumidity(Dunes).Should().Be(10);
    }

    // ---- an area's weather

    [Test]
    public void The_first_request_rolls_the_front_and_gives_a_viscara_area_warm_rain()
    {
        _world.AddArea(Meadow);

        Service.SetWeather(Meadow);

        Rolled.Should().Be(5, "four rolls for the front and one for the storm");
        Service.GetHeatIndex(Meadow).Should().Be(10);
        Service.GetHumidity(Meadow).Should().Be(10);
        Service.GetWindStrength(Meadow).Should().Be(10);
        Service.GetWeather(Meadow).Should().Be(Precipitation.Rain);
        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain);
        _world.Engine.CountOf("SetSkyBox").Should().Be(1);
    }

    [Test]
    public void The_authored_sky_and_fog_are_saved_once_and_put_back_with_the_weather()
    {
        _world.AddArea(Meadow);
        _world.SetSkyAndFog(Meadow, skybox: 4, sunAmount: 20, moonAmount: 30, sunColor: 5, moonColor: 6);

        Service.SetWeather(Meadow);

        _world.Locals.Int(Meadow, "VAR_WH_INITIALIZED").Should().Be(1);
        _world.Locals.Int(Meadow, "VAR_WH_SKYBOX").Should().Be(4);
        _world.Locals.Int(Meadow, "VAR_WH_FOG_SUN").Should().Be(20);
        _world.Locals.Int(Meadow, "VAR_WH_FOG_MOON").Should().Be(30);
        _world.Locals.Int(Meadow, "VAR_WH_FOG_C_SUN").Should().Be(5);
        _world.Locals.Int(Meadow, "VAR_WH_FOG_C_MOON").Should().Be(6);
        _world.Engine.CallsTo("SetSkyBox").Single().Arguments.Should().Equal((Skybox)4, Meadow);
        _world.Engine.CallsTo("SetFogColor").Select(call => (call.Arguments[0], call.Arguments[1])).Should().Equal(
            ((object?)FogType.Sun, (object?)(FogColor)5), (FogType.Moon, (FogColor)6));
        _world.Engine.CallsTo("SetFogAmount").Select(call => (call.Arguments[0], call.Arguments[1])).Should().Equal(
            ((object?)FogType.Sun, (object?)20), (FogType.Moon, 30));
    }

    [Test]
    public void The_authored_sky_is_not_saved_again_over_what_the_weather_painted()
    {
        _world.AddArea(Meadow);
        _world.SetSkyAndFog(Meadow, 4, 20, 30, 5, 6);
        Service.SetWeather(Meadow);
        _world.SetSkyAndFog(Meadow, 9, 90, 90, 9, 9);

        Service.SetAreaWindModifier(Meadow, 0);

        _world.Locals.Int(Meadow, "VAR_WH_SKYBOX").Should().Be(4);
        _world.Locals.Int(Meadow, "VAR_WH_FOG_SUN").Should().Be(20);
    }

    [Test]
    public void The_same_front_is_not_applied_twice_to_an_area()
    {
        _world.AddArea(Meadow);
        Service.SetWeather(Meadow);
        int Writes() => _world.Engine.Calls.Count(call => call.Member is "SetSkyBox" or "SetFogAmount" or "SetFogColor" or "SetWeather");
        var writes = Writes();
        var rolls = Rolled;

        Service.SetWeather(Meadow);
        Service.GetWeather(Meadow);
        Service.GetHeatIndex(Meadow);

        Writes().Should().Be(writes);
        Rolled.Should().Be(rolls, "entering an area again never rerolls the weather");
    }

    [Test]
    public void Areas_with_the_same_climate_and_modifiers_share_one_storm_roll()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes);
        StormyFront();
        Service.SetWeather(Meadow);
        var rolls = Rolled;

        Service.SetWeather(Dunes);

        Rolled.Should().Be(rolls, "the second area joins the storm that the first one rolled");
        _world.Locals.Int(Dunes, "GS_AM_SKY_OVERRIDE").Should().Be(1);
    }

    [Test]
    public void The_calling_area_is_the_default_target_of_the_weather_requests()
    {
        _world.AddArea(Meadow);
        _world.Engine.SetReturn("OBJECT_SELF", Meadow);

        Service.SetWeather();

        Service.GetWeather().Should().Be(Precipitation.Rain);
        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain);
    }

    [Test]
    public void A_thunderstorm_darkens_the_sky_and_marks_the_area()
    {
        _world.AddArea(Meadow);
        StormyFront();

        Service.SetWeather(Meadow);

        _world.Engine.CallsTo("SetSkyBox").Last().Arguments.Should().Equal(Skybox.GrassStorm, Meadow);
        _world.Locals.Int(Meadow, "GS_AM_SKY_OVERRIDE").Should().Be(1);
        Service.GetWeather(Meadow).Should().Be(Precipitation.Rain);
    }

    [Test]
    public void Without_a_storm_the_marks_of_the_last_one_are_removed()
    {
        _world.AddArea(Meadow);
        _world.SetInt(Meadow, "SAND_STORM", 1);
        _world.SetInt(Meadow, "GS_AM_SKY_OVERRIDE", 1);

        Service.SetWeather(Meadow);

        _world.Locals.Has(Meadow, "SAND_STORM").Should().BeFalse();
        _world.Locals.Has(Meadow, "GS_AM_SKY_OVERRIDE").Should().BeFalse();
        _world.Engine.CallsTo("DeleteLocalInt").Select(call => (string)call.Arguments[1]!)
            .Should().BeEquivalentTo("GS_AM_SKY_OVERRIDE", "DUST_STORM", "SAND_STORM", "SNOW_STORM");
    }

    [Test]
    public void A_sand_storm_orange_fogs_the_desert()
    {
        _world.AddArea(Dunes, PlanetType.Tatooine);
        StormyFront();

        Service.SetWeather(Dunes);

        _world.Locals.Int(Dunes, "SAND_STORM").Should().Be(1);
        _world.Locals.Int(Dunes, "SNOW_STORM").Should().Be(0);
        _world.Engine.CallsTo("SetFogColor").TakeLast(2).Select(call => call.Arguments[1]).Should().Equal(FogColor.OrangeDark, FogColor.OrangeDark);
        _world.Engine.CallsTo("SetFogAmount").TakeLast(2).Select(call => call.Arguments[1]).Should().Equal(80, 80);
        Service.GetWeather(Dunes).Should().Be(Precipitation.Clear);
    }

    [Test]
    public void A_blizzard_whitens_the_ice_fields()
    {
        _world.AddArea(Glacier, PlanetType.Hutlar);
        StormyFront();

        Service.SetWeather(Glacier);

        _world.Locals.Int(Glacier, "SNOW_STORM").Should().Be(1);
        _world.Locals.Int(Glacier, "SAND_STORM").Should().Be(0);
        _world.Engine.CallsTo("SetFogColor").TakeLast(2).Select(call => call.Arguments[1]).Should().Equal(FogColor.White, FogColor.White);
        _world.NativeWeatherSent(Glacier).Should().Equal(WeatherType.Snow);
    }

    [Test]
    public void An_area_that_is_not_natural_has_a_gentler_wind()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes);
        _world.SetUnnatural(Dunes);

        Service.GetWindStrength(Meadow).Should().Be(10);
        Service.GetWindStrength(Dunes).Should().Be(9);
    }

    [Test]
    public void An_area_without_weather_answers_with_the_shared_front_kept_between_one_and_ten()
    {
        _world.AddArea(Hall);
        _world.SetInterior(Hall);

        Service.GetHeatIndex(Hall).Should().Be(1, "no front has been rolled yet");
        Service.GetHumidity(Hall).Should().Be(1);
        Service.GetWindStrength(Hall).Should().Be(1);

        _world.AddArea(Meadow);
        Service.SetWeather(Meadow);

        Service.GetHeatIndex(Hall).Should().Be(10, "the front is heat 12 and the index never goes past ten");
        Service.GetHumidity(Hall).Should().Be(10);
        Service.GetWindStrength(Hall).Should().Be(10);
    }

    // ---- area modifiers

    [Test]
    public void A_heat_modifier_changes_only_that_area_and_sends_the_new_precipitation()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes);
        Service.SetWeather(Meadow);
        Service.SetWeather(Dunes);

        Service.SetAreaHeatModifier(Meadow, -20);

        Service.GetHeatIndex(Meadow).Should().Be(1);
        Service.GetWeather(Meadow).Should().Be(Precipitation.Snow);
        Service.GetHeatIndex(Dunes).Should().Be(10);
        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain, WeatherType.Snow);
        _world.NativeWeatherSent(Dunes).Should().Equal(WeatherType.Rain);
        _world.Locals.Int(Meadow, "VAR_WEATHER_HEAT").Should().Be(-20);
    }

    [Test]
    public void A_humidity_modifier_dries_the_area()
    {
        _world.AddArea(Meadow);
        Service.SetWeather(Meadow);

        Service.SetAreaHumidityModifier(Meadow, -20);

        Service.GetHumidity(Meadow).Should().Be(1);
        Service.GetWeather(Meadow).Should().Be(Precipitation.Clear);
        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain, WeatherType.Clear);
        _world.Locals.Int(Meadow, "VAR_WEATHER_HUMIDITY").Should().Be(-20);
    }

    [Test]
    public void A_wind_modifier_calms_the_area_and_the_same_native_rain_is_not_sent_again()
    {
        _world.AddArea(Meadow);
        Service.SetWeather(Meadow);

        Service.SetAreaWindModifier(Meadow, -3);

        Service.GetWindStrength(Meadow).Should().Be(7);
        Service.GetWeather(Meadow).Should().Be(Precipitation.Rain);
        _world.NativeWeatherSent(Meadow).Should().Equal(new[] { WeatherType.Rain },
            "clients restart rain they are already drawing when it is sent again");
        _world.Locals.Int(Meadow, "VAR_WEATHER_WIND").Should().Be(-3);
    }

    [Test]
    public void Native_weather_that_a_script_changed_is_put_back()
    {
        _world.AddArea(Meadow);
        Service.SetWeather(Meadow);
        _world.ForceNativeWeather(Meadow, Precipitation.Clear);

        Service.SetAreaWindModifier(Meadow, 0);

        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain, WeatherType.Rain);
    }

    [Test]
    public void The_acid_rain_flag_is_only_stored()
    {
        _world.AddArea(Meadow);
        Service.SetWeather(Meadow);
        _world.ClearCalls();

        Service.SetAreaAcidRain(Meadow, 1);

        _world.Locals.Int(Meadow, "VAR_WEATHER_ACID_RAIN").Should().Be(1);
        _world.Engine.Calls.Select(call => call.Member).Should().OnlyContain(member => member == "SetLocalInt" || member.StartsWith("GetLocal"));
    }

    // ---- fronts for the whole module

    [Test]
    public void Adjusting_the_weather_rolls_the_front_and_sets_every_area_even_without_players()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes, PlanetType.Tatooine);
        _world.AddArea(Hall);
        _world.SetInterior(Hall);

        Service.AdjustWeather().Should().BeTrue();

        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain);
        _world.NativeWeatherSent(Dunes).Should().Equal(WeatherType.Clear);
        _world.NativeWeatherSent(Hall).Should().BeEmpty();
        _world.Engine.CountOf("GetFirstPC").Should().Be(0, "the weather belongs to the areas, not to whoever is online");
    }

    [Test]
    public void Adjusting_the_weather_again_before_the_front_changes_does_nothing()
    {
        _world.AddArea(Meadow);
        Service.AdjustWeather();
        var sends = NativeSends;

        Service.AdjustWeather().Should().BeFalse();

        NativeSends.Should().Be(sends);
        _world.Engine.CountOf("GetFirstArea").Should().Be(1);
    }

    [Test]
    public void Initializing_the_weather_rolls_the_first_front_for_all_areas()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes);

        Service.InitializeWeather();

        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain);
        _world.NativeWeatherSent(Dunes).Should().Equal(WeatherType.Rain);
    }

    // ---- mist

    /// <summary>A cool, humid and calm front over an area: heat 5, humidity 10, wind 2, which is mist.</summary>
    private void MistyFront(params uint[] areas)
    {
        _world.Rolls(0, 9, 5, 5);
        foreach (var area in areas)
            _world.SetInt(area, "VAR_WEATHER_HEAT", -1);
    }

    [Test]
    public void Mist_is_reported_over_clear_native_weather()
    {
        _world.AddArea(Meadow);
        MistyFront(Meadow);
        Service.SetWeather(Meadow);

        Service.GetWeather(Meadow).Should().Be(Precipitation.Foggy);
        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Clear);

        _world.ForceNativeWeather(Meadow, Precipitation.Rain);
        Service.GetWeather(Meadow).Should().Be(Precipitation.Rain);
    }

    [Test]
    public void A_player_entering_a_misty_area_raises_the_mist_once()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        MistyFront(Meadow);

        EnterAs(Player, Meadow);
        EnterAs(Player, Meadow);

        var mist = _world.Engine.CallsTo("CreateObject").ToList();
        mist.Should().HaveCount(8, "one puff of mist for every eight tiles of an 8 by 8 area");
        mist.Should().OnlyContain(call => (string)call.Arguments[1]! == "x3_plc_mist");
        _world.Created.Should().OnlyContain(created => created.Area == Meadow);
        _world.Engine.CountOf("SetObjectVisualTransform").Should().Be(8);
        _world.MessagesTo(Player).Should().Equal(Climate(PlanetType.Viscara).MistText, Climate(PlanetType.Viscara).MistText);
    }

    [Test]
    public void A_creature_that_is_not_a_player_does_not_raise_the_mist_or_hear_the_weather()
    {
        _world.AddArea(Meadow);
        _world.AddCreature(Monster, Meadow);
        MistyFront(Meadow);

        EnterAs(Monster, Meadow);

        _world.Engine.CountOf("CreateObject").Should().Be(0);
        _world.Engine.CountOf("SendMessageToPC").Should().Be(0);
        NativeSends.Should().Be(1, "the area still gets its weather");
    }

    [Test]
    public void Mist_is_only_placed_on_ground_the_engine_reports()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        MistyFront(Meadow);
        _world.Engine.SetReturn("GetSurfaceMaterial", 0);

        EnterAs(Player, Meadow);

        _world.Engine.CountOf("CreateObject").Should().Be(0, "every tile tried was off the walkmesh");
        _world.Engine.CountOf("GetSurfaceMaterial").Should().Be(8 * 5, "each puff tries five tiles before giving up");
    }

    [Test]
    public void Mist_skips_tiles_without_a_ground_height()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        MistyFront(Meadow);
        _world.Engine.SetReturn("GetGroundHeight", -6f);

        EnterAs(Player, Meadow);

        _world.Engine.CountOf("CreateObject").Should().Be(0);
    }

    [Test]
    public void Mist_that_the_engine_refuses_to_create_is_not_remembered()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        MistyFront(Meadow);
        _world.CreatesObjects = false;

        EnterAs(Player, Meadow);

        _world.Engine.CountOf("CreateObject").Should().Be(8);
        _world.Engine.CountOf("SetObjectVisualTransform").Should().Be(0);
    }

    [Test]
    public void Mist_is_taken_down_when_the_precipitation_changes()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        MistyFront(Meadow);
        EnterAs(Player, Meadow);

        Service.SetAreaHeatModifier(Meadow, 0);

        Service.GetWeather(Meadow).Should().Be(Precipitation.Rain);
        _world.Engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!)
            .Should().BeEquivalentTo(_world.Created.Select(created => created.Object));
        _world.Created.Should().HaveCount(8, "rain needs no mist");
    }

    [Test]
    public void The_heartbeat_raises_the_mist_in_the_occupied_areas()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes);
        _world.AddPlayer(Player, Meadow);
        _world.AddPlayer(OtherPlayer, Meadow);
        MistyFront(Meadow);
        Service.SetWeather(Meadow);
        Service.SetWeather(Dunes);

        Service.OnModuleHeartbeat();

        _world.Created.Should().HaveCount(8).And.OnlyContain(created => created.Area == Meadow,
            "two players in one area raise its mist once, and the empty area gets none");
    }

    // ---- what a player is told

    [Test]
    public void A_player_is_told_the_weather_of_the_area_they_enter()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);

        EnterAs(Player, Meadow);

        _world.MessagesTo(Player).Should().Equal(Climate(PlanetType.Viscara).RainWarmText);
        _world.NativeWeatherSent(Meadow).Should().Equal(WeatherType.Rain);
    }

    [Test]
    public void Weather_effects_need_a_valid_player_in_an_area_with_weather()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Hall);
        _world.SetInterior(Hall);
        _world.AddPlayer(Player, Hall);
        _world.AddCreature(Monster, Meadow);

        Service.DoWeatherEffects(WeatherWorld.Invalid);
        Service.DoWeatherEffects(Monster);
        Service.DoWeatherEffects(Player);

        _world.Engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void The_description_follows_the_climate()
    {
        _world.AddArea(Dunes, PlanetType.Tatooine);
        _world.AddPlayer(Player, Dunes);

        Service.DoWeatherEffects(Player);

        _world.MessagesTo(Player).Should().Equal(Climate(PlanetType.Tatooine).ScorchingText);
    }

    [Test]
    public void Acid_rain_warns_the_player_and_hurts_them_with_poison()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        Service.SetAreaAcidRain(Meadow, 1);
        Service.SetWeather(Meadow);

        Service.DoWeatherEffects(Player);
        _world.RunAssigned(Meadow);

        _world.MessagesTo(Player).Should().Equal(WeatherFeedbackText.AcidRain);
        Service.ProtectedHits.Should().Equal((Player, 6, CombatDamageType.Poison));
        _world.Hits().Should().Equal((Player, 6, DamageType.Acid));
        _world.ObjectVisuals().Should().Equal((Player, VisualEffect.Vfx_Imp_Acid_S));
    }

    [TestCase(PlanetType.Tatooine, CombatDamageType.Physical, DamageType.Bludgeoning)]
    [TestCase(PlanetType.Hutlar, CombatDamageType.Ice, DamageType.Cold)]
    public void Sand_and_snow_storms_hurt_with_their_own_damage_type(PlanetType planet, CombatDamageType expected, DamageType native)
    {
        _world.AddArea(Dunes, planet);
        _world.AddPlayer(Player, Dunes);
        StormyFront();
        Service.SetWeather(Dunes);

        Service.DoWeatherEffects(Player);
        _world.RunAssigned(Dunes);

        Service.ProtectedHits.Should().Equal((Player, 6, expected));
        _world.Hits().Should().Equal((Player, 6, native));
        _world.ObjectVisuals().Select(visual => visual.Visual).Should().Equal(planet == PlanetType.Hutlar
            ? new[] { VisualEffect.Vfx_Imp_Frost_S }
            : Array.Empty<VisualEffect>());
        _world.MessagesTo(Player).Should().Equal(planet == PlanetType.Hutlar ? WeatherFeedbackText.SnowStorm : WeatherFeedbackText.SandStorm);
    }

    [Test]
    public void A_pulse_that_the_players_protections_reduce_to_nothing_applies_no_effect()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        Service.SetAreaAcidRain(Meadow, 1);
        Service.SetWeather(Meadow);
        Service.Protection = (_, _, _) => 0;

        Service.DoWeatherEffects(Player);
        _world.RunAssigned(Meadow);

        Service.ProtectedHits.Should().HaveCount(1);
        _world.Hits().Should().BeEmpty();
        _world.ObjectVisuals().Should().BeEmpty();
    }

    [Test]
    public void Entering_again_and_the_heartbeat_do_not_stack_a_second_pulse_on_the_first()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        Service.SetAreaAcidRain(Meadow, 1);
        Service.SetWeather(Meadow);

        Service.DoWeatherEffects(Player);
        Service.DoWeatherEffects(Player);
        EnterAs(Player, Meadow);
        Service.OnModuleHeartbeat();

        _world.AssignedTo(Meadow).Should().Be(1);
    }

    [Test]
    public void The_native_weather_decides_whether_acid_rain_falls()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        Service.SetAreaAcidRain(Meadow, 1);
        Service.SetWeather(Meadow);
        _world.ForceNativeWeather(Meadow, Precipitation.Clear);

        Service.DoWeatherEffects(Player);

        _world.AssignedTo(Meadow).Should().Be(0, "a DM cleared the sky");
        _world.MessagesTo(Player).Should().NotContain(WeatherFeedbackText.AcidRain);

        _world.ForceNativeWeather(Meadow, Precipitation.Rain);
        Service.DoWeatherEffects(Player);

        _world.AssignedTo(Meadow).Should().Be(1);
    }

    [Test]
    public void Dead_players_staff_and_possessed_creatures_take_no_weather_damage()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        _world.AddPlayer(Staff, Meadow, isDM: true);
        _world.AddPlayer(Dm, Meadow, isDM: true);
        _world.AddCreature(Possessed, Meadow);
        _world.Possess(Dm, Possessed, Meadow);
        _world.SetDead(Player);
        Service.SetAreaAcidRain(Meadow, 1);
        Service.SetWeather(Meadow);

        Service.DoWeatherEffects(Player);
        Service.DoWeatherEffects(Staff);
        Service.DoWeatherEffects(Possessed);

        _world.AssignedTo(Meadow).Should().Be(0);
        _world.MessagesTo(Player).Should().Equal(WeatherFeedbackText.AcidRain);
        _world.MessagesTo(Possessed).Should().Equal(WeatherFeedbackText.AcidRain);
    }

    // ---- shelter

    [Test]
    public void A_settlement_marked_safe_describes_the_weather_without_hurting()
    {
        _world.AddArea(Dunes, PlanetType.Tatooine);
        _world.AddPlayer(Player, Dunes);
        _world.SetInt(Dunes, "WEATHER_HAZARD_SAFE", 1);
        StormyFront();
        Service.SetWeather(Dunes);

        Service.DoWeatherEffects(Player);

        _world.MessagesTo(Player).Should().Equal(WeatherFeedbackText.ShelteredSandStorm);
        _world.AssignedTo(Dunes).Should().Be(0);
    }

    [TestCase(PlanetType.Tatooine, WeatherFeedbackText.ShelteredSandStorm)]
    [TestCase(PlanetType.Hutlar, WeatherFeedbackText.ShelteredSnowStorm)]
    public void Wilderness_next_to_a_safe_settlement_still_hurts(PlanetType planet, string sheltered)
    {
        _world.AddArea(Dunes, planet);
        _world.AddArea(Glacier, planet);
        _world.AddPlayer(Player, Dunes);
        _world.AddPlayer(OtherPlayer, Glacier);
        _world.SetInt(Dunes, "WEATHER_HAZARD_SAFE", 1);
        StormyFront();
        Service.SetWeather(Dunes);
        Service.SetWeather(Glacier);

        Service.DoWeatherEffects(Player);
        Service.DoWeatherEffects(OtherPlayer);
        _world.RunAssigned(Dunes);
        _world.RunAssigned(Glacier);

        _world.Hits().Select(hit => hit.Target).Should().Equal(OtherPlayer);
        _world.MessagesTo(Player).Should().Equal(sheltered);
    }

    [Test]
    public void A_player_city_protects_its_area_only_while_the_city_exists()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        Service.SetAreaAcidRain(Meadow, 1);
        Service.SetWeather(Meadow);
        _world.SetString(Meadow, "PROPERTY_ID", "city-1");
        _world.Db.With(new WorldProperty { Id = "city-1", PropertyType = PropertyType.City });

        Service.DoWeatherEffects(Player);

        _world.AssignedTo(Meadow).Should().Be(0);
        _world.MessagesTo(Player).Should().Equal(WeatherFeedbackText.ShelteredAcidRain);

        _world.Db.Delete<WorldProperty>("city-1");
        Service.DoWeatherEffects(Player);

        _world.AssignedTo(Meadow).Should().Be(1, "protection follows the city's life");
    }

    [Test]
    public void A_property_that_is_not_a_city_does_not_protect_its_area()
    {
        _world.AddArea(Meadow);
        _world.AddPlayer(Player, Meadow);
        Service.SetAreaAcidRain(Meadow, 1);
        Service.SetWeather(Meadow);
        _world.SetString(Meadow, "PROPERTY_ID", "house-1");
        _world.Db.With(new WorldProperty { Id = "house-1", PropertyType = PropertyType.House });

        Service.DoWeatherEffects(Player);

        _world.AssignedTo(Meadow).Should().Be(1);
        _world.Db.Reads.Should().Contain("house-1");
    }

    [Test]
    public void Crossing_a_safe_area_does_not_restart_the_damage_pulse()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes);
        _world.AddPlayer(Player, Meadow);
        _world.SetInt(Dunes, "WEATHER_HAZARD_SAFE", 1);
        Service.SetAreaAcidRain(Meadow, 1);
        Service.SetAreaAcidRain(Dunes, 1);
        Service.SetWeather(Meadow);
        Service.SetWeather(Dunes);
        Service.DoWeatherEffects(Player);

        _world.Move(Player, Dunes);
        Service.DoWeatherEffects(Player);
        _world.Move(Player, Meadow);
        Service.DoWeatherEffects(Player);

        _world.AssignedTo(Meadow).Should().Be(1, "the pulse clock stays anchored at the first hit");
        _world.AssignedTo(Dunes).Should().Be(0);
    }

    // ---- the heartbeat

    /// <summary>
    /// A player in the meadow, a DM in the dunes, and a DM whose avatar waits in the dunes while they possess a creature
    /// in the meadow.
    /// </summary>
    private void FullCast()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes);
        _world.AddPlayer(Player, Meadow);
        _world.AddPlayer(Staff, Dunes, isDM: true);
        _world.AddPlayer(Dm, Dunes, isDM: true);
        _world.AddCreature(Possessed, Meadow);
        _world.Possess(Dm, Possessed, Meadow);
    }

    [Test]
    public void A_new_front_reaches_a_possessing_dm_where_their_creature_is()
    {
        FullCast();
        MistyFront(Meadow, Dunes);

        Service.OnModuleHeartbeat();

        _world.MessageRecipients().Should().Equal(new[] { Player, Staff, Possessed },
            "each viewer is told once, and a possessing DM through the creature they control");
        _world.Created.Select(created => created.Area).Distinct().Should().BeEquivalentTo(new[] { Meadow, Dunes },
            "the mist goes up in every area with someone in it");
    }

    [Test]
    public void Weather_exposure_follows_the_possessed_creature_and_is_forgotten_when_the_player_leaves()
    {
        FullCast();
        Service.SetAreaAcidRain(Meadow, 1);
        Service.AdjustWeather();

        Service.OnModuleHeartbeat();
        _world.AssignedTo(Meadow).Should().Be(1, "the player takes the pulse and the possessed creature does not");

        _world.Leave(Player);
        Service.OnModuleHeartbeat();
        _world.AssignedTo(Meadow).Should().Be(1);

        _world.Rejoin(Player);
        Service.OnModuleHeartbeat();
        _world.AssignedTo(Meadow).Should().Be(2, "a player who was forgotten takes a new pulse the moment they are back");
    }

    [Test]
    public void The_heartbeat_strikes_lightning_once_a_minute_in_each_occupied_thunderstorm()
    {
        FullCast();
        _world.AddPlayer(OtherPlayer, Meadow);
        _world.AddArea(Glacier);
        StormyFront();
        Service.SetWeather(Meadow);
        Service.SetWeather(Glacier);

        Service.OnModuleHeartbeat();
        Service.OnModuleHeartbeat();

        _world.AssignedTo(Meadow).Should().Be(1, "three people in the area share one strike, and the next one is a minute away");
        _world.AssignedTo(Dunes).Should().Be(1, "the dunes share the climate and so the storm, and a staff member stands there");
        _world.AssignedTo(Glacier).Should().Be(0, "nobody stands under that storm");
    }

    [Test]
    public void No_lightning_falls_without_a_thunderstorm_a_lucky_roll_or_solid_ground()
    {
        _world.AddArea(Meadow);
        _world.AddArea(Dunes);
        _world.AddArea(Glacier, width: 0, height: 0);
        StormyFront();
        Service.SetWeather(Meadow);
        Service.SetWeather(Glacier);

        _world.D3 = 2;
        Service.Thunderstorm(Meadow);
        _world.D3 = 1;
        Service.Thunderstorm(Dunes);
        Service.Thunderstorm(Glacier);
        Service.Thunderstorm(Hall);
        _world.Engine.SetReturn("GetSurfaceMaterial", 0);
        Service.Thunderstorm(Meadow);

        _world.Engine.CountOf("AssignCommand").Should().Be(0);
    }

    // ---- lightning

    private void ThunderstormOver(uint area)
    {
        _world.AddArea(area);
        StormyFront();
        Service.SetWeather(area);
    }

    private void Strike(uint area)
    {
        Service.Thunderstorm(area);
        _world.AssignedTo(area).Should().Be(1, "the bolt is assigned to the area");
        _world.RunAssigned(area);
    }

    [Test]
    public void Lightning_in_a_safe_area_shows_the_flash_without_looking_for_targets()
    {
        _world.AddArea(Meadow);
        _world.SetInt(Meadow, "WEATHER_HAZARD_SAFE", 1);
        StormyFront();
        Service.SetWeather(Meadow);
        _world.AddPlayer(Player, Meadow);
        _world.StrikeReaches(Meadow, Player);

        Strike(Meadow);

        _world.LocationVisuals().Should().Equal(VisualEffect.Vfx_Imp_Lightning_M);
        _world.SearchesStarted.Should().Be(0);
        _world.Hits().Should().BeEmpty();
        _world.Knockdowns().Should().BeEmpty();
    }

    [Test]
    public void Lightning_in_the_wilderness_hurts_and_knocks_down_the_players_it_reaches()
    {
        ThunderstormOver(Meadow);
        _world.AddPlayer(Player, Meadow);
        _world.StrikeReaches(Meadow, Player);

        Strike(Meadow);

        _world.LocationVisuals().Should().Equal(VisualEffect.Vfx_Imp_Lightning_M);
        _world.Hits().Should().Equal((Player, 100, DamageType.Electrical));
        _world.Knockdowns().Should().Equal(Player);
        _world.MessagesTo(Player).Should().Equal(WeatherFeedbackText.Lightning);
        _world.Engine.CountOf("PlayVoiceChat").Should().Be(1);
        Service.ProtectedHits.Should().Equal((Player, 100, CombatDamageType.Electrical));
        _world.Engine.CallsTo("ApplyEffectToObject").Last().Arguments[0].Should().Be(DurationType.Temporary);
    }

    [Test]
    public void Lightning_spares_staff_and_possessed_creatures_and_does_not_knock_down_the_dead()
    {
        ThunderstormOver(Meadow);
        _world.AddPlayer(Player, Meadow);
        _world.AddPlayer(Staff, Meadow, isDM: true);
        _world.AddPlayer(Dm, Meadow, isDM: true);
        _world.AddCreature(Possessed, Meadow);
        _world.Possess(Dm, Possessed, Meadow);
        _world.AddCreature(Monster, Meadow);
        _world.SetDead(Monster);
        _world.StrikeReaches(Meadow, Staff, Possessed, Monster, Player);

        Strike(Meadow);

        _world.Hits().Select(hit => hit.Target).Should().Equal(Monster, Player);
        _world.Knockdowns().Should().Equal(Player);
        _world.MessagesTo(Monster).Should().BeEmpty();
    }

    [Test]
    public void Lightning_that_is_absorbed_leaves_the_player_standing_and_so_does_knockdown_immunity()
    {
        ThunderstormOver(Meadow);
        _world.AddPlayer(Player, Meadow);
        _world.AddPlayer(OtherPlayer, Meadow);
        _world.AddCreature(Monster, Meadow);
        _world.StrikeReaches(Meadow, Monster, Player, OtherPlayer);
        Service.Protection = (target, damage, _) => target == Monster ? 0 : damage;
        Service.KnockdownImmunity = target => target == Player ? 5 : 0;
        Service.KnockdownTicks = (target, ticks) => target == OtherPlayer ? 0 : ticks;

        Strike(Meadow);

        _world.Hits().Select(hit => hit.Target).Should().Equal(Player, OtherPlayer);
        _world.Knockdowns().Should().BeEmpty("one has immunity and the other resists all of it");
        _world.MessagesTo(Monster).Should().BeEmpty();
    }

    [Test]
    public void Lightning_hurts_fixtures_but_only_creatures_react()
    {
        ThunderstormOver(Meadow);
        _world.AddFixture(Door, Meadow);
        _world.AddCreature(Monster, Meadow);
        _world.StrikeReaches(Meadow, Door, Monster);

        Strike(Meadow);

        _world.Hits().Select(hit => hit.Target).Should().Equal(Door, Monster);
        _world.Knockdowns().Should().Equal(Monster);
        _world.Engine.CountOf("PlayVoiceChat").Should().Be(1);
    }

    [Test]
    public void Lightning_damage_falls_off_with_distance_and_a_weak_bolt_hurts_nobody()
    {
        ThunderstormOver(Meadow);
        _world.AddPlayer(Player, Meadow);
        _world.StrikeReaches(Meadow, Player);
        _world.Engine.SetReturn("GetDistanceBetweenLocations", 3f);
        _world.D100 = 20;

        Strike(Meadow);

        _world.Hits().Should().BeEmpty("a bolt of 30 loses 30 to three metres");
        _world.Knockdowns().Should().BeEmpty();
        _world.SearchesStarted.Should().Be(1);
    }

    [Test]
    public void Targets_are_captured_before_the_first_one_is_hit()
    {
        ThunderstormOver(Meadow);
        _world.AddPlayer(Player, Meadow);
        _world.AddPlayer(OtherPlayer, Meadow);
        _world.StrikeReaches(Meadow, Player, OtherPlayer);
        // A script run by the damage searches again; bound it so a regression fails without hanging the test runner.
        var searches = 3;
        _world.OnDamage = () =>
        {
            if (searches-- > 0)
                _world.SearchAgain();
        };

        Strike(Meadow);

        _world.Hits().Select(hit => hit.Target).Should().Equal(Player, OtherPlayer);
        _world.SearchesStarted.Should().Be(3, "the strike's own search and the one the script starts for each of the two hits");
    }

    [Test]
    public void Lightning_skips_a_target_that_is_gone_by_the_time_it_is_reached()
    {
        ThunderstormOver(Meadow);
        _world.AddPlayer(Player, Meadow);
        _world.AddCreature(Monster, Meadow);
        _world.StrikeReaches(Meadow, Player, Monster);
        _world.OnDamage = () => _world.Invalidate(Monster);

        Strike(Meadow);

        _world.Hits().Select(hit => hit.Target).Should().Equal(Player);
    }

    // ---- the damage pipeline of the original class

    [Test]
    public void Doors_and_placeables_take_the_whole_bolt_and_never_a_negative_amount()
    {
        Service.UnprotectedDamage(Door, 45, CombatDamageType.Electrical).Should().Be(45);
        Service.UnprotectedDamage(Door, 0, CombatDamageType.Electrical).Should().Be(0);
        Service.UnprotectedDamage(Door, -5, CombatDamageType.Electrical).Should().Be(0);
    }
}
