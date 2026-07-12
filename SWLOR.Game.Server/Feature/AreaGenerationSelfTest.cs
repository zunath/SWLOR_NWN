using System;
using System.Diagnostics;
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
    /// AREA_GENERATION_SELF_TEST=1 in the server environment. Exercises everything a
    /// client cannot see from the outside — tileset override, CreateArea, per-tile
    /// readback, path validation, teardown, and override reuse — and reports PASS/FAIL
    /// on the server console so a headless docker run verifies the whole path.
    /// </summary>
    public static class AreaGenerationSelfTest
    {
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleSelfTest()
        {
            if (Environment.GetEnvironmentVariable("AREA_GENERATION_SELF_TEST") != "1")
                return;

            // Delay past module load so the scheduler and all boot caches are live.
            Scheduler.Schedule(Run, TimeSpan.FromSeconds(10));
        }

        private static void Run()
        {
            Report("Starting area generation self-test.");
            RunPassChain(12345, 1, () =>
                RunPassChain(99999, 2, () =>
                    Report("PASS - both generation passes succeeded: tiles, content, and teardown verified.")));
        }

        /// <summary>
        /// Generates and geometry-checks a pass synchronously, then defers the content phase one
        /// tick — CreateArea's initialization only completes after the creating script returns, so
        /// content placement (matching the facade's deferred-callback contract) must wait.
        /// </summary>
        private static void RunPassChain(int seed, int pass, Action onSuccess)
        {
            AreaGenerationResult result;
            RuntimeAreaInstance instance;
            try
            {
                (result, instance) = RunGeometryPhase(seed, pass);
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
                    RunContentPhase(result, instance, pass, onSuccess);
                }
                catch (Exception ex)
                {
                    Report($"FAIL - {ex.Message}");
                }
            }, TimeSpan.FromSeconds(2));
        }

        private static (AreaGenerationResult, RuntimeAreaInstance) RunGeometryPhase(int seed, int pass)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = AreaGeneration.Generate(new AreaGenerationRequest
            {
                Width = 16,
                Height = 16,
                Seed = seed,
                DisplayName = $"SelfTest Area {pass}",
                Tag = "GEN_SELFTEST"
            });
            stopwatch.Stop();

            if (!result.Success)
                throw new InvalidOperationException($"pass {pass}: generation failed: {result.FailureReason}");

            var area = result.Area;
            var layout = result.Layout;
            Report($"pass {pass}: generated {result.InstanceId} in {stopwatch.ElapsedMilliseconds}ms " +
                   $"(seed {result.SeedUsed}, {result.AttemptsUsed} attempt(s), {layout.Rooms.Count} rooms).");

            var width = GetAreaSize(Dimension.Width, area);
            var height = GetAreaSize(Dimension.Height, area);
            if (width != layout.Width || height != layout.Height)
                throw new InvalidOperationException(
                    $"pass {pass}: area size is {width}x{height}, expected {layout.Width}x{layout.Height} — override did not define the grid.");

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
                    $"pass {pass}: tile readback mismatches — {idMismatches} IDs, {orientationMismatches} orientations of {layout.Tiles.Length} tiles.");

            Report($"pass {pass}: all {layout.Tiles.Length} tiles read back correctly.");

            if (!RuntimeAreaRegistry.TryGetByArea(area, out var instance) || instance.WalkablePoints.Count == 0)
                throw new InvalidOperationException($"pass {pass}: no walkable points registered.");

            var sample = instance.WalkablePoints[0];
            Report($"pass {pass}: {instance.WalkablePoints.Count} walkable points; sample ground height z={sample.Z:F2}.");

            return (result, instance);
        }

        private static void RunContentPhase(AreaGenerationResult result, RuntimeAreaInstance instance, int pass, Action onSuccess)
        {
            var population = DungeonContentPlacer.Populate(instance, DungeonDefinition.MineCaveDungeonDefinition.ThemeKey, 1);
            if (population.CreaturesSpawned == 0)
                throw new InvalidOperationException($"pass {pass}: content placement spawned no creatures.");
            if (!population.BossSpawned)
                throw new InvalidOperationException($"pass {pass}: boss did not spawn.");
            if (!population.TreasurePlaced)
                throw new InvalidOperationException($"pass {pass}: treasure container was not placed.");
            if (!population.ExitPlaced)
                throw new InvalidOperationException($"pass {pass}: exit placeable did not spawn.");

            Report($"pass {pass}: content placed — {population.CreaturesSpawned} creatures in {population.RoomsPopulated} rooms, " +
                   $"boss '{population.BossResref}', treasure container present, exit present.");

            // The treasure fill happens on a later tick (placeable inventories reject items in
            // their creation script context), so its assertion and teardown defer once more.
            Scheduler.Schedule(() =>
            {
                try
                {
                    if (population.TreasureItemsSpawned == 0)
                        throw new InvalidOperationException($"pass {pass}: treasure fill produced no items.");

                    Report($"pass {pass}: treasure filled with {population.TreasureItemsSpawned} item(s).");

                    if (!AreaGeneration.DestroyGeneratedArea(result.InstanceId, out var destroyFailure))
                        throw new InvalidOperationException($"pass {pass}: teardown failed: {destroyFailure}");

                    Report($"pass {pass}: teardown clean.");
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
