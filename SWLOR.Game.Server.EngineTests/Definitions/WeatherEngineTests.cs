using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.WeatherService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;
using WeatherRuntime = SWLOR.Game.Server.Service.Weather;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class WeatherEngineTests
    {
        [EngineTest("Settlement maps and copied city areas retain weather hazard protection", Category = "Weather")]
        public static async Task SettlementWeatherProtection(EngineTestContext ctx)
        {
            var predicate = typeof(WeatherRuntime).GetMethod("IsHazardousWeatherArea",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            var protectedAreas = 0;
            var wildernessAreas = 0;
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (GetLocalBool(area, "WEATHER_HAZARD_SAFE"))
                {
                    ctx.Assert(!(bool)predicate.Invoke(null, new object[] { area })!,
                        $"Authored settlement {GetResRef(area)} is safe from weather hazards");
                    protectedAreas++;
                }
                else if (GetResRef(area) == "viscarawildlands" || GetResRef(area) == "hutlar_qion")
                {
                    ctx.Assert((bool)predicate.Invoke(null, new object[] { area })!,
                        $"Wilderness {GetResRef(area)} retains weather hazards");
                    wildernessAreas++;
                }
            }
            ctx.AssertEqual(27, protectedAreas, "All authored outdoor settlement markers survive module packing");
            ctx.AssertEqual(2, wildernessAreas, "Both wilderness controls were loaded");

            var city = ctx.CreateInstancedArea("tat_tocheemain");
            ctx.Assert(GetLocalBool(city, "WEATHER_HAZARD_SAFE"), "City instances inherit the authored marker");
            WeatherRuntime.SetWeather(city);
            ctx.Assert(WeatherRuntime.GetWeather(city) != Precipitation.Invalid, "The protected city retains weather visuals");
            ctx.Assert(!(bool)predicate.Invoke(null, new object[] { city })!, "The new city instance is hazard safe");

            var target = ctx.SpawnCreature("nw_bandit001");
            ObjectPlugin.AddToArea(target, city, GetPositionFromLocation(ctx.GetArenaLocation()));
            await ctx.WaitFrameAsync();
            SetPlotFlag(target, false);
            Stat.SetNPCMaxHitPoints(target, 200, true);
            var hp = GetCurrentHitPoints(target);
            var knockdowns = CountKnockdowns(target);
            var bolt = typeof(WeatherRuntime).GetMethod("Thunderstorm", BindingFlags.Static | BindingFlags.NonPublic)!;
            await ctx.ExecuteInCreatureContextAsync(city,
                () => bolt.Invoke(null, new object[] { GetLocation(target), 40 }));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(hp, GetCurrentHitPoints(target), "Settlement lightning causes no damage");
            ctx.AssertEqual(knockdowns, CountKnockdowns(target), "Settlement lightning causes no knockdown");

            // Only this private copy is changed; the authored world remains safe.
            DeleteLocalInt(city, "WEATHER_HAZARD_SAFE");
            await ctx.ExecuteInCreatureContextAsync(city,
                () => bolt.Invoke(null, new object[] { GetLocation(target), 40 }));
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) < hp, 3f,
                "the unprotected lightning control to deal damage");
            ctx.SetResultDetail($"Verified {protectedAreas} packed settlement markers, two harmful wilderness controls, copied city weather visuals, and actual lightning immunity. Removing protection in the private instance restored lightning damage.");
        }

        private static int CountKnockdowns(uint target)
        {
            var count = 0;
            var creature = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSCreature();
            foreach (var effect in creature.m_appliedEffects)
                if ((EffectTypeEngine)effect.m_nType == EffectTypeEngine.Knockdown) count++;
            return count;
        }

        [EngineTest("Outdoor weather stays consistent across Viscara maps and a new instance", Category = "Weather")]
        public static async Task SharedOutdoorWeather(EngineTestContext ctx)
        {
            // Use a warm, wet, windy front so independently rolled storms would
            // split the planet's maps between ordinary rain and thunderstorms.
            WeatherRuntime.LoadData();
            try
            {
                var pattern = (WeatherPattern)typeof(WeatherRuntime)
                    .GetField("_pattern", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
                pattern.TryAdvance(DateTime.UtcNow, 6, false, max => max - 1);
                var areas = new List<uint>();
                for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
                {
                    if (Planet.GetPlanetType(area) != PlanetType.Viscara || GetIsAreaInterior(area) ||
                        !GetIsAreaAboveGround(area) || GetIsAreaNatural(area) == 0 ||
                        GetLocalBool(area, "SPACE") || !string.IsNullOrWhiteSpace(GetLocalString(area, "VAR_WEATHER_CLIMATE")) ||
                        GetLocalInt(area, "VAR_WEATHER_HEAT") != 0 || GetLocalInt(area, "VAR_WEATHER_HUMIDITY") != 0 ||
                        GetLocalInt(area, "VAR_WEATHER_WIND") != 0) continue;
                    areas.Add(area);
                }
                ctx.Assert(areas.Count >= 10, "Need several comparable outdoor maps to exercise shared storm selection.");
                var instance = ctx.CreateInstancedArea(GetResRef(areas[0]));
                areas.Add(instance);

                int? storm = null;
                foreach (var area in areas)
                {
                    WeatherRuntime.SetWeather(area);
                    ctx.AssertEqual(10, WeatherRuntime.GetHeatIndex(area), $"Heat in {GetResRef(area)}");
                    ctx.AssertEqual(10, WeatherRuntime.GetHumidity(area), $"Humidity in {GetResRef(area)}");
                    ctx.AssertEqual(10, WeatherRuntime.GetWindStrength(area), $"Wind in {GetResRef(area)}");
                    ctx.AssertEqual(Precipitation.Rain, WeatherRuntime.GetWeather(area), $"Rain in {GetResRef(area)}");
                    storm ??= GetLocalInt(area, "GS_AM_SKY_OVERRIDE");
                    ctx.AssertEqual(storm.Value, GetLocalInt(area, "GS_AM_SKY_OVERRIDE"), $"Storm in {GetResRef(area)}");
                    WeatherRuntime.SetWeather(area);
                    ctx.AssertEqual(storm.Value, GetLocalInt(area, "GS_AM_SKY_OVERRIDE"), "Repeated entry retains the shared storm");
                }
                ctx.SetResultDetail($"Verified equal heat, humidity, wind, native rain and storm sky flags across {areas.Count - 1} outdoor maps plus a fresh instance. Repeated requests retained the same storm.");
                await ctx.WaitFrameAsync();
            }
            finally
            {
                WeatherRuntime.LoadData();
                WeatherRuntime.InitializeWeather();
            }
        }
    }
}
