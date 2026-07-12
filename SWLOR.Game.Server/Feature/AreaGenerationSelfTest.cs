using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;

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
                Report($"PASS - all {themes.Count} theme passes succeeded: tiles, content, and teardown verified.");
                return;
            }

            RunPassChain(themes[index], 12345 + index, () => RunThemeChain(themes, index + 1));
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
            var detail = DungeonContentPlacer.GetDungeonDetail(themeKey);
            var stopwatch = Stopwatch.StartNew();
            var result = AreaGeneration.Generate(new AreaGenerationRequest
            {
                TilesetResref = detail.TilesetResref,
                PlaceholderResref = detail.PlaceholderResref,
                Lighting = detail.Lighting,
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

            if (!RuntimeAreaRegistry.TryGetByArea(area, out var instance) || instance.WalkablePoints.Count == 0)
                throw new InvalidOperationException($"{themeKey}: no walkable points registered.");

            var sample = instance.WalkablePoints[0];
            Report($"{themeKey}: {instance.WalkablePoints.Count} walkable points; sample ground height z={sample.Z:F2}.");

            return (result, instance);
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

            Report($"{themeKey}: content placed — {population.CreaturesSpawned} creatures in {population.RoomsPopulated} rooms, " +
                   $"boss '{population.BossResref}', treasure container present, exit present.");

            // The treasure fill happens on a later tick (placeable inventories reject items in
            // their creation script context), so its assertion and teardown defer once more.
            Scheduler.Schedule(() =>
            {
                try
                {
                    if (population.TreasureItemsSpawned == 0)
                        throw new InvalidOperationException($"{themeKey}: treasure fill produced no items.");

                    Report($"{themeKey}: treasure filled with {population.TreasureItemsSpawned} item(s).");

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
