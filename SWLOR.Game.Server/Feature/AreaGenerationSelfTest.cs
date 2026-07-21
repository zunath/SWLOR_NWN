using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.Game.Server.Service.AreaGenerationService.Decoration;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Boot-time self-test for the area generation engine path, enabled by setting
    /// AREA_GENERATION_SELF_TEST=1 in the server environment. Runs one full pass per
    /// registered dungeon theme — tileset override, CreateArea, per-tile readback, content
    /// placement, treasure fill, and teardown — and reports PASS/FAIL on the server console
    /// so a headless docker run verifies the whole path for every theme.
    /// </summary>
    public static class AreaGenerationSelfTest
    {
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleSelfTest()
        {
            Report($"env: SELF_TEST='{Environment.GetEnvironmentVariable("AREA_GENERATION_SELF_TEST")}', " +
                   $"CROSS_TEST='{Environment.GetEnvironmentVariable("AREA_GENERATION_CROSS_TEST")}'");

            if (Environment.GetEnvironmentVariable("AREA_GENERATION_SELF_TEST") == "1")
            {
                // Delay past module load so the scheduler and all boot caches are live.
                Scheduler.Schedule(Run, TimeSpan.FromSeconds(10));
            }

            if (Environment.GetEnvironmentVariable("AREA_GENERATION_CROSS_TEST") == "1")
            {
                Report("scheduling cross-tileset test.");
                Scheduler.Schedule(RunCrossTilesetTest, TimeSpan.FromSeconds(10));
            }
        }

        /// <summary>
        /// Architecture probe: can an override re-tileset a placeholder onto a DIFFERENT tileset?
        /// Generates on the tdt01 cave placeholder with the tds01 sewer tileset. If this passes,
        /// one generic placeholder can serve every supported tileset.
        /// </summary>
        private static void RunCrossTilesetTest()
        {
            try
            {
                var result = AreaGeneration.Generate(new AreaGenerationRequest
                {
                    TilesetResref = "tds01",
                    PlaceholderResref = "gen_placeholder1",
                    Width = 16,
                    Height = 16,
                    Seed = 777,
                    DisplayName = "CrossTileset Test",
                    Tag = "GEN_CROSSTEST"
                });

                if (!result.Success)
                {
                    Report($"CROSS FAIL - generation failed: {result.FailureReason}");
                    return;
                }

                var layout = result.Layout;
                var tilesetOnArea = GetTilesetResRef(result.Area);
                var mismatches = 0;
                for (var y = 0; y < layout.Height; y++)
                for (var x = 0; x < layout.Width; x++)
                {
                    var tileLocation = Location(result.Area, new Vector3(x * 10f + 5f, y * 10f + 5f, 0f), 0f);
                    if (GetTileID(tileLocation) != layout.GetTile(x, y).TileId)
                        mismatches++;
                }

                Report($"CROSS result: area tileset='{tilesetOnArea}' (wanted tds01), tile mismatches={mismatches}/{layout.Tiles.Length}.");
                AreaGeneration.DestroyGeneratedArea(result.InstanceId, out _);
                Report(mismatches == 0 && tilesetOnArea == "tds01"
                    ? "CROSS PASS - overrides can re-tileset a placeholder."
                    : "CROSS FAIL - placeholder tileset was not fully overridden.");
            }
            catch (Exception ex)
            {
                Report($"CROSS FAIL - {ex.Message}");
            }
        }

        private static void Run()
        {
            Report("Starting area generation self-test.");
            var themes = DungeonContentPlacer.GetAllDungeonThemes().Keys.OrderBy(k => k).ToList();
            if (themes.Count == 0)
            {
                Report("FAIL - no dungeon themes registered.");
                return;
            }

            RunThemeChain(themes, 0);
        }

        private static void RunThemeChain(List<string> themes, int index)
        {
            if (index >= themes.Count)
            {
                // Final pass: the city frontage composition, asserting the live per-instance
                // scale-transform path (see RunCityFrontagePass) before reporting overall PASS.
                RunCityFrontagePass(themes[0], () =>
                    Report($"PASS - all {themes.Count} theme passes succeeded (tiles, content, teardown) " +
                           "and the city frontage pass verified live per-instance scale transforms."));
                return;
            }

            RunPassChain(themes[index], 12345 + index, () => RunThemeChain(themes, index + 1));
        }

        /// <summary>
        /// City-composition pass: generates the frontage-declaring FutCity tileset (Packed layout,
        /// fixed seed) and asserts the live decoration path applied every planned per-instance
        /// visual scale (frontage scale jitter -- see DungeonTilesetProfile.FrontageScaleJitter and
        /// DungeonPopulationResult.ScaleTransformsPlanned/Applied): the offline review module
        /// persists the same scales as .git VisualTransform structs, and this keeps the two paths
        /// from silently diverging.
        /// </summary>
        private static void RunCityFrontagePass(string themeKey, Action onSuccess)
        {
            AreaGenerationResult result;
            RuntimeAreaInstance instance;
            try
            {
                var composition = DungeonContentPlacer.GetComposition(
                    themeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
                var request = new AreaGenerationRequest
                {
                    TilesetResref = composition.Tileset.TilesetResref,
                    TilesetProfileKey = composition.Tileset.Key,
                    PlaceholderResref = composition.Tileset.PlaceholderResref,
                    OpenTerrainOverride = composition.Tileset.PrimaryOpenTerrain,
                    Lighting = composition.Tileset.Lighting,
                    Atmosphere = composition.Tileset.ResolveAtmosphere(composition.Content.AtmosphereProfile),
                    Layout = composition.BuildLayoutParameters(),
                    Width = 16,
                    Height = 16,
                    Seed = 24242,
                    DisplayName = "SelfTest city frontage",
                    Tag = "GEN_SELFTEST_CITY"
                };

                result = AreaGeneration.Generate(request);
                if (!result.Success)
                    throw new InvalidOperationException($"city frontage pass: generation failed: {result.FailureReason}");
                if (!RuntimeAreaRegistry.TryGetByArea(result.Area, out instance))
                    throw new InvalidOperationException("city frontage pass: generated area is not registered.");
            }
            catch (Exception ex)
            {
                Report($"FAIL - {ex.Message}");
                return;
            }

            Scheduler.Schedule(() =>
            {
                try
                {
                    var population = DungeonContentPlacer.Populate(instance, themeKey, 1);
                    Scheduler.Schedule(() =>
                    {
                        try
                        {
                            if (!population.DecorationsSpawnComplete)
                                throw new InvalidOperationException(
                                    "city frontage pass: batched decoration spawning did not complete " +
                                    $"({population.DecorationsPlaced}/{population.DecorationsPlanned} spawned).");
                            if (population.ScaleTransformsPlanned == 0)
                                throw new InvalidOperationException(
                                    "city frontage pass: the plan carried zero per-instance scale transforms — " +
                                    "FrontageScaleJitter did not reach the live plan.");
                            if (population.ScaleTransformsApplied != population.ScaleTransformsPlanned)
                                throw new InvalidOperationException(
                                    $"city frontage pass: only {population.ScaleTransformsApplied}/" +
                                    $"{population.ScaleTransformsPlanned} live scale transforms verified.");

                            // Support-anchor grounding parity (see PlannedDecoration.GroundAnchor):
                            // every frontage building's live GetGroundHeight sample at its anchor
                            // must agree with the plan's own GroundZ, so the live path grounds
                            // buildings on the platform exactly where the offline review module
                            // puts them -- a divergence here means a building would sink to the
                            // chasm floor (or float) live while looking correct in the toolset.
                            if (population.GroundAnchorsPlanned == 0)
                                throw new InvalidOperationException(
                                    "city frontage pass: the plan carried zero support anchors — " +
                                    "frontage grounding anchors did not reach the live plan.");
                            if (population.GroundAnchorsVerified != population.GroundAnchorsPlanned)
                                throw new InvalidOperationException(
                                    $"city frontage pass: only {population.GroundAnchorsVerified}/" +
                                    $"{population.GroundAnchorsPlanned} support-anchor ground heights matched the plan.");

                            Report($"city frontage pass: {population.ScaleTransformsApplied}/" +
                                   $"{population.ScaleTransformsPlanned} per-instance scale transforms verified live; " +
                                   $"{population.GroundAnchorsVerified}/{population.GroundAnchorsPlanned} " +
                                   "support-anchor ground heights verified against the plan.");

                            if (!AreaGeneration.DestroyGeneratedArea(result.InstanceId, out var destroyFailure))
                                throw new InvalidOperationException($"city frontage pass: teardown failed: {destroyFailure}");

                            onSuccess();
                        }
                        catch (Exception ex)
                        {
                            Report($"FAIL - {ex.Message}");
                        }
                    }, TimeSpan.FromSeconds(3));
                }
                catch (Exception ex)
                {
                    Report($"FAIL - {ex.Message}");
                }
            }, TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Generates and geometry-checks a theme synchronously, then defers the content phase one
        /// tick — CreateArea's initialization only completes after the creating script returns, so
        /// content placement (matching the facade's deferred-callback contract) must wait.
        /// </summary>
        private static void RunPassChain(string themeKey, int seed, Action onSuccess)
        {
            AreaGenerationResult result;
            RuntimeAreaInstance instance;
            try
            {
                (result, instance) = RunGeometryPhase(themeKey, seed);
            }
            catch (Exception ex)
            {
                Report($"FAIL - {ex.Message}");
                return;
            }

            Scheduler.Schedule(() =>
            {
                try
                {
                    RunContentPhase(result, instance, themeKey, onSuccess);
                }
                catch (Exception ex)
                {
                    Report($"FAIL - {ex.Message}");
                }
            }, TimeSpan.FromSeconds(2));
        }

        private static (AreaGenerationResult, RuntimeAreaInstance) RunGeometryPhase(string themeKey, int seed)
        {
            var composition = DungeonContentPlacer.GetComposition(themeKey);
            var stopwatch = Stopwatch.StartNew();
            var result = AreaGeneration.Generate(new AreaGenerationRequest
            {
                TilesetResref = composition.Tileset.TilesetResref,
                TilesetProfileKey = composition.Tileset.Key,
                PlaceholderResref = composition.Tileset.PlaceholderResref,
                OpenTerrainOverride = composition.Tileset.PrimaryOpenTerrain,
                Lighting = composition.Tileset.Lighting,
                Atmosphere = composition.Tileset.ResolveAtmosphere(composition.Content.AtmosphereProfile),
                Layout = composition.BuildLayoutParameters(),
                Width = 16,
                Height = 16,
                Seed = seed,
                DisplayName = $"SelfTest {themeKey}",
                Tag = "GEN_SELFTEST"
            });
            stopwatch.Stop();

            if (!result.Success)
                throw new InvalidOperationException($"{themeKey}: generation failed: {result.FailureReason}");

            var area = result.Area;
            var layout = result.Layout;
            Report($"{themeKey}: generated {result.InstanceId} in {stopwatch.ElapsedMilliseconds}ms " +
                   $"(seed {result.SeedUsed}, {result.AttemptsUsed} attempt(s), {layout.Rooms.Count} rooms).");

            var width = GetAreaSize(Dimension.Width, area);
            var height = GetAreaSize(Dimension.Height, area);
            if (width != layout.Width || height != layout.Height)
                throw new InvalidOperationException(
                    $"{themeKey}: area size is {width}x{height}, expected {layout.Width}x{layout.Height} — override did not define the grid.");

            var idMismatches = 0;
            var orientationMismatches = 0;
            for (var y = 0; y < layout.Height; y++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var expected = layout.GetTile(x, y);
                    var tileLocation = Location(area, new Vector3(x * 10f + 5f, y * 10f + 5f, 0f), 0f);

                    if (GetTileID(tileLocation) != expected.TileId)
                        idMismatches++;
                    if (GetTileOrientation(tileLocation) != expected.Orientation)
                        orientationMismatches++;
                }
            }

            if (idMismatches > 0 || orientationMismatches > 0)
                throw new InvalidOperationException(
                    $"{themeKey}: tile readback mismatches — {idMismatches} IDs, {orientationMismatches} orientations of {layout.Tiles.Length} tiles.");

            Report($"{themeKey}: all {layout.Tiles.Length} tiles read back correctly.");

            VerifyAtmosphere(area, themeKey, composition);

            if (!RuntimeAreaRegistry.TryGetByArea(area, out var instance) || instance.WalkablePoints.Count == 0)
                throw new InvalidOperationException($"{themeKey}: no walkable points registered.");

            var sample = instance.WalkablePoints[0];
            Report($"{themeKey}: {instance.WalkablePoints.Count} walkable points; sample ground height z={sample.Z:F2}.");

            return (result, instance);
        }

        /// <summary>
        /// Live assertion that the runtime-settable atmosphere subset actually landed on the
        /// generated instance: reads every field AreaSynthesizer.ApplyAtmosphere writes back
        /// through the corresponding engine/NWNX getter and compares against the composed
        /// atmosphere. Themes composing onto a family without a declared atmosphere skip with a
        /// note (there is nothing to verify -- the clone deliberately keeps placeholder values).
        /// SunShadows/MoonShadows, LightingScheme, and LoadScreenID have no runtime setter OR
        /// getter and are .are-emission-only; they are deliberately absent here.
        /// </summary>
        private static void VerifyAtmosphere(uint area, string themeKey, DungeonComposition composition)
        {
            var atmosphere = composition.Tileset.ResolveAtmosphere(composition.Content.AtmosphereProfile);
            if (atmosphere == null)
            {
                Report($"{themeKey}: no family atmosphere declared for '{composition.Tileset.Key}' -- placeholder area properties retained (expected).");
                return;
            }

            var failures = new List<string>();
            void Check(string field, int expected, int actual)
            {
                if (expected != actual)
                    failures.Add($"{field} expected {expected} got {actual}");
            }

            Check("SkyBox", atmosphere.SkyBox, (int)GetSkyBox(area));
            var expectedCycle = atmosphere.DayNightCycle
                ? SWLOR.NWN.API.NWScript.Enum.Area.DayNightCycle.CycleDayNight
                : atmosphere.IsNight
                    ? SWLOR.NWN.API.NWScript.Enum.Area.DayNightCycle.AlwaysDark
                    : SWLOR.NWN.API.NWScript.Enum.Area.DayNightCycle.AlwaysBright;
            Check("DayNightCycle", (int)expectedCycle, (int)SWLOR.NWN.API.NWNX.AreaPlugin.GetDayNightCycle(area));
            // Color readback asymmetry (see AreaSynthesizer.ApplyAtmosphere's conversion note):
            // NWNX GetSunMoonColors returns the raw NATIVE (BGR) dword, which is exactly the .are
            // encoding the atmosphere carries -- compare directly. Base GetFogColor mirrors its own
            // RGB-encoded Set, so the fog readbacks compare against the RGB-swapped value.
            Check("SunAmbientColor", atmosphere.SunAmbientColor,
                SWLOR.NWN.API.NWNX.AreaPlugin.GetSunMoonColors(area, AreaLightColorType.SunAmbient));
            Check("SunDiffuseColor", atmosphere.SunDiffuseColor,
                SWLOR.NWN.API.NWNX.AreaPlugin.GetSunMoonColors(area, AreaLightColorType.SunDiffuse));
            Check("MoonAmbientColor", atmosphere.MoonAmbientColor,
                SWLOR.NWN.API.NWNX.AreaPlugin.GetSunMoonColors(area, AreaLightColorType.MoonAmbient));
            Check("MoonDiffuseColor", atmosphere.MoonDiffuseColor,
                SWLOR.NWN.API.NWNX.AreaPlugin.GetSunMoonColors(area, AreaLightColorType.MoonDiffuse));
            Check("SunFogColor", AreaSynthesizer.SwapRedBlue(atmosphere.SunFogColor), (int)GetFogColor(FogType.Sun, area));
            Check("MoonFogColor", AreaSynthesizer.SwapRedBlue(atmosphere.MoonFogColor), (int)GetFogColor(FogType.Moon, area));
            Check("SunFogAmount", atmosphere.SunFogAmount, GetFogAmount(FogType.Sun, area));
            Check("MoonFogAmount", atmosphere.MoonFogAmount, GetFogAmount(FogType.Moon, area));
            Check("WindPower", atmosphere.WindPower, SWLOR.NWN.API.NWNX.AreaPlugin.GetWindPower(area));
            Check("ShadowOpacity", atmosphere.ShadowOpacity, SWLOR.NWN.API.NWNX.AreaPlugin.GetShadowOpacity(area));
            Check("ChanceRain", atmosphere.ChanceRain,
                SWLOR.NWN.API.NWNX.AreaPlugin.GetWeatherChance(area, WeatherEffectType.Rain));
            Check("ChanceSnow", atmosphere.ChanceSnow,
                SWLOR.NWN.API.NWNX.AreaPlugin.GetWeatherChance(area, WeatherEffectType.Snow));
            Check("ChanceLightning", atmosphere.ChanceLightning,
                SWLOR.NWN.API.NWNX.AreaPlugin.GetWeatherChance(area, WeatherEffectType.Lightning));

            var fogClip = SWLOR.NWN.API.NWNX.AreaPlugin.GetFogClipDistance(area);
            if (Math.Abs(fogClip - atmosphere.FogClipDist) > 0.01f)
                failures.Add($"FogClipDist expected {atmosphere.FogClipDist} got {fogClip}");

            if (failures.Count > 0)
                throw new InvalidOperationException(
                    $"{themeKey}: atmosphere readback mismatches -- {string.Join("; ", failures)}.");

            Report($"{themeKey}: atmosphere verified on the live instance " +
                   $"(skybox {atmosphere.SkyBox}, {(atmosphere.DayNightCycle ? "day/night cycle" : atmosphere.IsNight ? "always night" : "always day")}, " +
                   $"wind {atmosphere.WindPower}, fog clip {atmosphere.FogClipDist}).");
        }

        private static void RunContentPhase(AreaGenerationResult result, RuntimeAreaInstance instance, string themeKey, Action onSuccess)
        {
            var population = DungeonContentPlacer.Populate(instance, themeKey, 1);
            if (population.CreaturesSpawned == 0)
                throw new InvalidOperationException($"{themeKey}: content placement spawned no creatures.");
            if (!population.BossSpawned)
                throw new InvalidOperationException($"{themeKey}: boss did not spawn.");
            if (!population.TreasurePlaced)
                throw new InvalidOperationException($"{themeKey}: treasure container was not placed.");
            if (!population.ExitPlaced)
                throw new InvalidOperationException($"{themeKey}: exit placeable did not spawn.");

            // Door-style and GroupExit-style transitions must have produced real door objects;
            // anything the planner marked Door/GroupExit that failed door creation falls back to a
            // placeable and logs an error, so a shortfall here means UtilPlugin.CreateDoor rejected
            // the theme's blueprint.
            var doorTransitions = instance.Layout.Transitions.Count(t => t.Style is TransitionStyle.Door or TransitionStyle.GroupExit);
            if (population.DoorsCreated < doorTransitions)
                throw new InvalidOperationException(
                    $"{themeKey}: {doorTransitions} door-style transition(s) but only {population.DoorsCreated} door(s) created.");

            // Decorations default on (AreaGenerationRequest.EnableDecorations = true). A fixed test
            // seed's per-tile RNG rolls (see DungeonDecorationPlanner.Plan) can legitimately produce
            // zero decorations at low base densities even though the theme curates a real palette, so
            // asserting DecorationsPlaced > 0 outright would be a false-failure risk, not a real
            // invariant. What IS a real, RNG-independent invariant is that whatever the planner
            // decided gets spawned exactly — the deterministic plan count is recorded on the result
            // (DecorationsPlanned) and the batched spawn (see DungeonContentPlacer.PlaceDecorations,
            // which chains large city-density plans across scheduler ticks) must converge on it 1:1;
            // any shortfall means CreateObject/spawn tracking broke, not that RNG rolled unluckily.
            // The count assertion therefore runs in the deferred block below, after the batches have
            // had time to drain.
            var detail = DungeonContentPlacer.GetDungeonDetail(themeKey);
            if (population.DecorationsPlanned == 0 && detail.Decorations.Count > 0 && detail.DecorationBaseDensity > 0)
                Report($"{themeKey}: decoration pass planned zero placeables this run (RNG); palette and density are both non-empty, so this is not treated as a failure.");

            Report($"{themeKey}: content placed — {population.CreaturesSpawned} creatures in {population.RoomsPopulated} rooms, " +
                   $"boss '{population.BossResref}', treasure container present, exit present, " +
                   $"{population.DoorsCreated}/{doorTransitions} transition doors created, " +
                   $"{population.DecorationsPlaced}/{population.DecorationsPlanned} decoration(s) placed so far.");

            // The treasure fill happens on a later tick (placeable inventories reject items in
            // their creation script context), so its assertion and teardown defer once more — as
            // does the decoration spawn-count assertion (batched across ticks, see above).
            Scheduler.Schedule(() =>
            {
                try
                {
                    if (population.TreasureItemsSpawned == 0)
                        throw new InvalidOperationException($"{themeKey}: treasure fill produced no items.");

                    Report($"{themeKey}: treasure filled with {population.TreasureItemsSpawned} item(s).");

                    if (!population.DecorationsSpawnComplete)
                        throw new InvalidOperationException(
                            $"{themeKey}: batched decoration spawning did not complete within the deferred window " +
                            $"({population.DecorationsPlaced}/{population.DecorationsPlanned} spawned).");
                    if (population.DecorationsPlaced != population.DecorationsPlanned)
                        throw new InvalidOperationException(
                            $"{themeKey}: decoration spawn count {population.DecorationsPlaced} does not match the " +
                            $"deterministic plan count {population.DecorationsPlanned} — spawning likely failed for some planned decoration(s).");

                    if (!AreaGeneration.DestroyGeneratedArea(result.InstanceId, out var destroyFailure))
                        throw new InvalidOperationException($"{themeKey}: teardown failed: {destroyFailure}");

                    Report($"{themeKey}: teardown clean.");
                    onSuccess();
                }
                catch (Exception ex)
                {
                    Report($"FAIL - {ex.Message}");
                }
            }, TimeSpan.FromSeconds(3));
        }

        private static void Report(string message)
        {
            Console.WriteLine($"[AreaGenSelfTest] {message}");
            Log.Write(LogGroup.Server, $"[AreaGenSelfTest] {message}", true);
        }
    }
}
