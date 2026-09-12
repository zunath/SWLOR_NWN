using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.WeatherService;
using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;
using WeatherRuntime = SWLOR.Game.Server.Service.Weather;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public class WeatherTests
{
    private static readonly DateTime Start = new(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public void FirstUpdate_InitializesEvenAtMidnight_AndWaitsOneRealHour()
    {
        var pattern = new WeatherPattern();
        pattern.TryAdvance(Start, 6, false, Rolls(4, 9, 9, 9)).Should().BeTrue();
        pattern.Heat.Should().Be(12);
        pattern.Humidity.Should().Be(10);
        pattern.Wind.Should().Be(10);
        pattern.Revision.Should().Be(1);
        pattern.NextChangeUtc.Should().Be(Start.AddHours(1));

        for (var second = 0; second < 3600; second += 6)
        {
            pattern.TryAdvance(Start.AddSeconds(second), 12, true, NoRoll).Should().BeFalse();
            pattern.Heat.Should().Be(12, "dusk and calendar changes cannot bypass the real-time interval");
            pattern.Humidity.Should().Be(10);
        }

        pattern.TryAdvance(Start.AddHours(1), 12, true, Rolls(0, 0, 0, 0)).Should().BeTrue();
        pattern.Heat.Should().Be(11, "temperature moves by at most one point, including at dusk");
        pattern.Humidity.Should().Be(9, "wind must not cause a humidity jump");
    }

    [Test]
    public void MissedUpdate_AdvancesOnce_AndSchedulesFromTheActualUpdateTime()
    {
        var pattern = new WeatherPattern();
        pattern.TryAdvance(Start, 6, false, Rolls(4, 9, 9, 9));
        pattern.TryAdvance(Start.AddDays(3), 12, true, Rolls(0, 0, 0, 0)).Should().BeTrue();
        pattern.Revision.Should().Be(2);
        pattern.Heat.Should().Be(11);
        pattern.NextChangeUtc.Should().Be(Start.AddDays(3).AddHours(1));
        pattern.TryAdvance(Start.AddDays(3), 6, false, NoRoll).Should().BeFalse();
    }

    [Test]
    public void ClockMovingBackwards_DoesNotCauseAnEarlyUpdate()
    {
        var pattern = new WeatherPattern();
        pattern.TryAdvance(Start, 6, false, Rolls(0, 0, 0, 0));
        pattern.TryAdvance(Start.AddHours(-1), 6, false, NoRoll).Should().BeFalse();
    }

    [Test]
    public void EveryWindRoll_StaysInTheAuthoredRange()
    {
        for (var first = 0; first < 10; first++)
        for (var second = 0; second < 10; second++)
        {
            var pattern = new WeatherPattern();
            pattern.TryAdvance(Start, 6, false, Rolls(0, 0, first, second));
            pattern.Wind.Should().BeInRange(1, 10);
            pattern.NextChangeUtc.Should().Be(Start.AddHours(1));
        }
    }

    [Test]
    public void ExtendedSimulation_PreservesBoundsAndGradualTemperatureAndHumidity()
    {
        var random = new System.Random(1234);
        var pattern = new WeatherPattern();
        for (var hour = 0; hour < 10000; hour++)
        {
            var previousHeat = pattern.Heat;
            var previousHumidity = pattern.Humidity;
            pattern.TryAdvance(Start.AddHours(hour), hour % 12 + 1, hour % 2 == 0, random.Next).Should().BeTrue();
            pattern.Heat.Should().BeInRange(-1, 12);
            pattern.Humidity.Should().BeInRange(1, 10);
            pattern.Wind.Should().BeInRange(1, 10);
            if (hour == 0) continue;
            Math.Abs(pattern.Heat - previousHeat).Should().BeLessThanOrEqualTo(1);
            Math.Abs(pattern.Humidity - previousHumidity).Should().BeLessThanOrEqualTo(1);
        }
    }

    [TestCase(3, 8, 2, Precipitation.Snow)]
    [TestCase(4, 8, 2, Precipitation.Foggy)]
    [TestCase(5, 8, 2, Precipitation.Foggy)]
    [TestCase(5, 8, 3, Precipitation.Rain)]
    [TestCase(6, 8, 2, Precipitation.Rain)]
    [TestCase(1, 7, 10, Precipitation.Clear)]
    [TestCase(10, 7, 10, Precipitation.Clear)]
    public void Precipitation_UsesTheAuthoredThresholds(int heat, int humidity, int wind, Precipitation expected)
    {
        Conditions(heat, humidity, wind).Precipitation.Should().Be(expected);
    }

    [TestCase(PlanetType.Viscara, 4, 10, 5)]
    [TestCase(PlanetType.Tatooine, 10, 1, 5)]
    [TestCase(PlanetType.MonCala, 7, 8, 6)]
    [TestCase(PlanetType.Hutlar, 1, 1, 5)]
    [TestCase(PlanetType.Korriban, 9, 3, 5)]
    public void PlanetClimates_AreAppliedOnce(PlanetType planet, int heat, int humidity, int wind)
    {
        var climate = WeatherPlanetDefinitions.GetPlanetClimates()[planet];
        var result = WeatherConditions.Create(6, 8, 5, climate, 0, 0, 0, true, WeatherStorm.None, max => max - 1);
        result.Heat.Should().Be(heat);
        result.Humidity.Should().Be(humidity);
        result.Wind.Should().Be(wind);
    }

    [Test]
    public void LocalModifiers_AndCityShelter_CombineWithPlanetClimateBeforeClamping()
    {
        var climate = WeatherPlanetDefinitions.GetPlanetClimates()[PlanetType.MonCala];
        var result = WeatherConditions.Create(6, 8, 5, climate, -2, -3, 2, false, WeatherStorm.None, NoRoll);
        result.Heat.Should().Be(5);
        result.Humidity.Should().Be(5);
        result.Wind.Should().Be(7);
        result.Precipitation.Should().Be(Precipitation.Clear);
    }

    [Test]
    public void ClimateLookup_UsesTheSharedPlanetService_ForSpacedAndHyphenatedNames()
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        typeof(Planet).GetMethod("CachePlanets", flags)!.Invoke(null, null);
        var resolve = typeof(Planet).GetMethod("ResolvePlanetTypeByAreaName", flags)!;
        resolve.Invoke(null, new object[] { "Mon Cala - Coral Isles - Inner" }).Should().Be(PlanetType.MonCala);
        resolve.Invoke(null, new object[] { "CZ-220 - Main Deck" }).Should().Be(PlanetType.CZ220);
        resolve.Invoke(null, new object[] { "Smuggler's Moon Station - Corridors" }).Should().Be(PlanetType.SmugglersMoonStation);
        Planet.GetPlanetTypeByAreaResref("canyon_001").Should().Be(PlanetType.Tatooine);
        Method("GetAreaClimate").Should().Contain("Planet.GetPlanetType(area)").And.Contain("TryGetValue");
    }

    [Test]
    public void Thunderstorm_StartProbabilityIsWindInTwenty_AndContinuationIsOneInThree()
    {
        for (var wind = 1; wind <= 10; wind++)
        {
            var starts = Enumerable.Range(0, 20).Count(roll =>
                Conditions(6, 8, wind, random: _ => roll).Storm == WeatherStorm.Thunder);
            starts.Should().Be(wind);
            var continuations = Enumerable.Range(0, 3).Count(roll =>
                Conditions(6, 8, wind, WeatherStorm.Thunder, _ => roll).Storm == WeatherStorm.Thunder);
            continuations.Should().Be(1, "an existing storm has a two-in-three chance of clearing");
        }
    }

    [TestCase(3, 10, 10)]
    [TestCase(5, 10, 2)]
    [TestCase(8, 7, 10)]
    public void Thunderstorm_ClearsWhenRainConditionsEnd(int heat, int humidity, int wind)
    {
        Conditions(heat, humidity, wind, WeatherStorm.Thunder, NoRoll).Storm.Should().Be(WeatherStorm.None);
    }

    [TestCase(PlanetType.Tatooine, WeatherStorm.Sand, WeatherHazard.Sand)]
    [TestCase(PlanetType.Hutlar, WeatherStorm.Snow, WeatherHazard.Snow)]
    public void RegionalStorms_StartInStrongWind_AndClearWhenWindDrops(PlanetType planet, WeatherStorm storm, WeatherHazard hazard)
    {
        var climate = WeatherPlanetDefinitions.GetPlanetClimates()[planet];
        var started = WeatherConditions.Create(6, 8, 9, climate, 0, 0, 0, true, WeatherStorm.None, _ => 0);
        started.Storm.Should().Be(storm);
        started.GetHazard(false).Should().Be(hazard);
        var cleared = WeatherConditions.Create(6, 8, 8, climate, 0, 0, 0, true, storm, NoRoll);
        cleared.Storm.Should().Be(WeatherStorm.None);
        cleared.GetHazard(false).Should().Be(WeatherHazard.None);
    }

    [Test]
    public void RepeatedAreaEntries_DoNotRerollStormsOrChangePrecipitation()
    {
        var pattern = new WeatherPattern();
        pattern.TryAdvance(Start, 6, false, Rolls(0, 9, 9, 9));
        var area = new WeatherAreaState();
        area.TryUpdate(pattern, new WeatherClimate(), 0, 0, 0, true, _ => 0).Should().BeTrue();
        var initial = area.Conditions;
        initial.Storm.Should().Be(WeatherStorm.Thunder);
        for (var entry = 0; entry < 1000; entry++)
        {
            area.TryUpdate(pattern, new WeatherClimate(), 0, 0, 0, true, NoRoll).Should().BeFalse();
            area.Conditions.Should().BeSameAs(initial);
        }
        pattern.TryAdvance(Start.AddHours(1), 6, false, Rolls(0, 10, 9, 9));
        area.TryUpdate(pattern, new WeatherClimate(), 0, 0, 0, true, _ => 2).Should().BeTrue();
        area.Conditions.Storm.Should().Be(WeatherStorm.None);
    }

    [Test]
    public void ExplicitAreaModifierEdit_InvalidatesOnlyThatArea()
    {
        var pattern = new WeatherPattern();
        pattern.TryAdvance(Start, 6, false, Rolls(0, 0, 0, 0));
        var changed = new WeatherAreaState();
        var other = new WeatherAreaState();
        changed.TryUpdate(pattern, new WeatherClimate(), 0, 0, 0, true, NoRoll);
        other.TryUpdate(pattern, new WeatherClimate(), 0, 0, 0, true, NoRoll);
        changed.Invalidate();
        changed.TryUpdate(pattern, new WeatherClimate(), -20, 0, 0, true, NoRoll).Should().BeTrue();
        other.TryUpdate(pattern, new WeatherClimate(), 0, 0, 0, true, NoRoll).Should().BeFalse();
        changed.Conditions.Heat.Should().Be(1);
        other.Conditions.Heat.Should().Be(8);
    }

    [Test]
    public void HazardDamage_ContinuesEverySixSeconds_WithoutStackingOnEntryOrHeartbeat()
    {
        var exposure = new WeatherExposure();
        exposure.GetDamageDice(Start, WeatherHazard.Acid).Should().Be(2);
        for (var second = 0; second < 6; second++)
            exposure.GetDamageDice(Start.AddSeconds(second), WeatherHazard.Acid).Should().Be(0);
        exposure.GetDamageDice(Start.AddSeconds(6), WeatherHazard.Acid).Should().Be(1);
        exposure.GetDamageDice(Start.AddSeconds(12), WeatherHazard.Acid).Should().Be(1);
        exposure.GetDamageDice(Start.AddSeconds(18), WeatherHazard.Snow).Should().Be(2);
        exposure.GetDamageDice(Start.AddSeconds(24), WeatherHazard.Sand).Should().Be(2);
    }

    [Test]
    public void ShelterAndClearingWeather_StopDamage_AndReentryCannotBypassThePulseTimer()
    {
        var exposure = new WeatherExposure();
        exposure.GetDamageDice(Start, WeatherHazard.Sand).Should().Be(2);
        exposure.GetDamageDice(Start.AddSeconds(1), WeatherHazard.None).Should().Be(0);
        exposure.GetDamageDice(Start.AddSeconds(2), WeatherHazard.Sand).Should().Be(0);
        exposure.GetDamageDice(Start.AddSeconds(6), WeatherHazard.None).Should().Be(0);
        exposure.GetDamageDice(Start.AddHours(1), WeatherHazard.None).Should().Be(0);
    }

    [Test]
    public void AcidHazard_RequiresActualRain_AndWarningsMatchTheSelectedHazard()
    {
        var climate = new WeatherClimate();
        Conditions(6, 8, 3).GetHazard(true).Should().Be(WeatherHazard.Acid);
        Conditions(6, 8, 3).GetFeedback(climate, false, true).Should().Be(WeatherFeedbackText.AcidRain);
        Conditions(5, 8, 2).GetHazard(true).Should().Be(WeatherHazard.None);
        Conditions(3, 8, 3).GetHazard(true).Should().Be(WeatherHazard.None);
        Conditions(6, 7, 3).GetHazard(true).Should().Be(WeatherHazard.None);
        (Conditions(6, 7, 9) with { Storm = WeatherStorm.Sand }).GetFeedback(climate, false, false)
            .Should().Be(WeatherFeedbackText.SandStorm);
        (Conditions(1, 7, 9) with { Storm = WeatherStorm.Snow }).GetFeedback(climate, false, false)
            .Should().Be(WeatherFeedbackText.SnowStorm);
    }

    [TestCase(11, 3f, 0)]
    [TestCase(30, 3f, 0)]
    [TestCase(40, 3f, 10)]
    [TestCase(110, 0f, 110)]
    public void LightningFalloff_NeverProducesNegativeDamage(int power, float distance, int expected)
    {
        WeatherConditions.GetLightningDamage(power, distance).Should().Be(expected);
    }

    [Test]
    public void NativeWeatherOverride_StopsAcidWhenCleared_AndEnablesItWhenRainIsForced()
    {
        Conditions(6, 8, 3).GetHazard(true, Precipitation.Clear).Should().Be(WeatherHazard.None);
        Conditions(6, 7, 3).GetHazard(true, Precipitation.Rain).Should().Be(WeatherHazard.Acid);
        Conditions(6, 7, 3).GetFeedback(new WeatherClimate(), false, true, Precipitation.Rain)
            .Should().Be(WeatherFeedbackText.AcidRain);
        Method("ApplyWeatherDamage").Should().Contain("NWScript.GetWeather(area)");
    }

    [Test]
    public void Runtime_InitializesAllAreas_AndUsesOneHeartbeatForExposureAndDistinctOccupiedAreas()
    {
        typeof(WeatherRuntime).GetMethod(nameof(WeatherRuntime.InitializeWeather))!
            .GetCustomAttributes<NWNEventHandler>().Select(x => x.Script).Should().Contain(ScriptName.OnModuleLoad);
        Method("AdjustWeather").Should().Contain("GetFirstArea()").And.Contain("GetNextArea()")
            .And.NotContain("GetFirstPC()");
        var heartbeat = Method("OnModuleHeartbeat");
        heartbeat.Should().Contain("ApplyWeatherDamage(player, now)").And.Contain("new HashSet<uint>()")
            .And.Contain("now.AddMinutes(1)").And.Contain("_exposures.Remove(player)");
        Source().Should().NotContain("GetTimeHour()").And.NotContain("DelayCommand(")
            .And.NotContain("GetMaster(");
        Method("IsWeatherArea").Should().Contain("!GetIsAreaInterior(area)")
            .And.Contain("GetIsAreaAboveGround(area)").And.Contain("\"SPACE\"");
    }

    [Test]
    public void Runtime_UpdatesFogWhenPrecipitationChanges_AndRestoresAllAuthoredVisualSettings()
    {
        var update = Method("SetWeather", 1);
        update.Should().Contain("state.Revision == _pattern.Revision")
            .And.Contain("previous?.Precipitation != conditions.Precipitation")
            .And.Contain("ClearMist(state)").And.NotContain("EnsureMist(");
        foreach (var variable in new[] { "VAR_SKYBOX", "VAR_FOG_SUN", "VAR_FOG_MOON", "VAR_FOG_C_SUN", "VAR_FOG_C_MOON" })
            update.Should().Contain($"GetLocalInt(area, {variable})");
        Method("ClearMist").Should().Contain("DestroyObject(placeable)")
            .And.Contain("state.MistPlaceables.Clear()");
        Method("EnsureMist").Should().Contain("index = 0").And.Contain("state.MistInitialized = true");
        Method("OnAreaEnter").Should().Contain("GetIsPC(creature)").And.Contain("EnsureMist(area, state)");
        Method("OnModuleHeartbeat").Should().Contain("EnsureMist(area, state)");
        Method("TryGetGroundLocation").Should().Contain("GetGroundHeight(location)");
    }

    [Test]
    public void Runtime_HazardsExcludeDeadAndStaffPlayers_AndColdDamageUsesColdFeedback()
    {
        var damage = Method("ApplyWeatherDamage");
        damage.Should().Contain("GetIsPC(creature)").And.Contain("!GetIsDM(creature)")
            .And.Contain("!GetIsDMPossessed(creature)").And.Contain("!GetIsDead(creature)")
            .And.Contain("conditions.GetHazard(").And.Contain("WeatherHazard.None")
            .And.Contain("WeatherHazard.Snow => DamageType.Cold")
            .And.Contain("VisualEffect.Vfx_Imp_Frost_S").And.Contain("AssignCommand(area,");
        Method("Thunderstorm", 1).Should().Contain("state.Conditions.Storm != WeatherStorm.Thunder");
    }

    private static WeatherConditions Conditions(int heat, int humidity, int wind,
        WeatherStorm previous = WeatherStorm.None, Func<int, int> random = null) =>
        WeatherConditions.Create(heat, humidity, wind, new WeatherClimate(), 0, 0, 0, true,
            previous, random ?? (max => max - 1));

    private static Func<int, int> Rolls(params int[] values)
    {
        var rolls = new Queue<int>(values);
        return max =>
        {
            rolls.Should().NotBeEmpty();
            var value = rolls.Dequeue();
            value.Should().BeInRange(0, max - 1);
            return value;
        };
    }

    private static int NoRoll(int max) => throw new AssertionException("Weather must not reroll here.");

    private static string Source()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server"))) root = root.Parent;
        return File.ReadAllText(Path.Combine(root!.FullName, "SWLOR.Game.Server", "Service", "Weather.cs"));
    }

    private static string Method(string name, int? parameterCount = null)
    {
        return CSharpSyntaxTree.ParseText(Source()).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(method => method.Identifier.ValueText == name &&
                             (parameterCount == null || method.ParameterList.Parameters.Count == parameterCount)).ToString();
    }
}
