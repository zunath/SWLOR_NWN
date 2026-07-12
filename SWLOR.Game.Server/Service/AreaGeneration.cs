using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Facade for procedural area generation. Consumers queue an AreaGenerationRequest and
    /// receive an AreaGenerationResult via callback; generation runs one request per scheduler
    /// tick so bursts cannot spike the main loop. All geometry work completes before the
    /// callback fires, so callers may admit players immediately on success.
    /// </summary>
    public static class AreaGeneration
    {
        private static readonly Dictionary<string, TilesetModel> _tilesetModels = new();
        private static readonly Queue<(AreaGenerationRequest Request, Action<AreaGenerationResult> Callback)> _queue = new();
        private static bool _processingScheduled;
        private static int _instanceCounter;

        /// <summary>
        /// Loads and caches the tileset model from the tileset's .set resource.
        /// Throws when the resource is missing or unparseable — callers treat that as a failed request.
        /// </summary>
        public static TilesetModel GetTilesetModel(string tilesetResref)
        {
            if (_tilesetModels.TryGetValue(tilesetResref, out var cached))
                return cached;

            var contents = ResManGetFileContents(tilesetResref, (int)ResType.SET);
            if (string.IsNullOrEmpty(contents))
                throw new InvalidOperationException($"Tileset '{tilesetResref}' has no readable .set resource.");

            var model = TilesetSetParser.Parse(tilesetResref, contents);
            _tilesetModels[tilesetResref] = model;
            return model;
        }

        public static void QueueGeneration(AreaGenerationRequest request, Action<AreaGenerationResult> callback)
        {
            _queue.Enqueue((request, callback));
            ScheduleProcessing();
        }

        private static void ScheduleProcessing()
        {
            if (_processingScheduled || _queue.Count == 0)
                return;

            _processingScheduled = true;
            Scheduler.Schedule(ProcessNext, TimeSpan.FromMilliseconds(1));
        }

        private static void ProcessNext()
        {
            _processingScheduled = false;
            if (_queue.Count == 0)
                return;

            var (request, callback) = _queue.Dequeue();
            AreaGenerationResult result;
            try
            {
                result = Generate(request);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Area generation failed with exception: {ex}");
                result = new AreaGenerationResult
                {
                    Success = false,
                    FailureReason = ex.Message
                };
            }

            // CreateArea's initialization only runs after the creating script returns, and object
            // interactions inside the new area (e.g. filling container inventories) fail until then.
            // Deliver the callback on a later tick so consumers can safely populate content.
            Scheduler.Schedule(() => callback?.Invoke(result), TimeSpan.FromSeconds(1));
            ScheduleProcessing();
        }

        /// <summary>
        /// Generates an area synchronously: solve layout, realize via tileset override,
        /// validate pathability, register. Retries with fresh seeds up to MaxAttempts,
        /// then fails cleanly. Prefer QueueGeneration from gameplay code.
        /// </summary>
        public static AreaGenerationResult Generate(AreaGenerationRequest request)
        {
            var model = GetTilesetModel(request.TilesetResref);
            var lastFailure = "No attempts were made.";

            for (var attempt = 0; attempt < request.MaxAttempts; attempt++)
            {
                // A caller-fixed seed must stay deterministic across retries, so attempt
                // seeds derive from it; otherwise every attempt rolls fresh.
                var seed = request.Seed.HasValue
                    ? request.Seed.Value + attempt
                    : Random.Next(int.MaxValue);

                var rng = new System.Random(seed);
                MacroLayout macro;
                try
                {
                    macro = MacroLayoutGenerator.Generate(new MacroLayoutParameters
                    {
                        Width = request.Width,
                        Height = request.Height,
                        SolidTerrain = model.DefaultTerrain,
                        OpenTerrain = model.FloorTerrain,
                        MinRooms = request.MinRooms,
                        MaxRooms = request.MaxRooms
                    }, rng);
                }
                catch (InvalidOperationException ex)
                {
                    lastFailure = $"Macro layout failed (seed {seed}): {ex.Message}";
                    Log.Write(LogGroup.Server, lastFailure);
                    continue;
                }

                macro.Seed = seed;

                if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var resolveFailure))
                {
                    lastFailure = $"Tile resolution failed (seed {seed}): {resolveFailure}";
                    Log.Write(LogGroup.Server, lastFailure);
                    continue;
                }

                var instanceId = $"genarea_{++_instanceCounter}";
                var area = AreaSynthesizer.Realize(resolved, request.PlaceholderResref, instanceId, request.Tag, request.DisplayName, request.Lighting);

                if (!GetIsObjectValid(area))
                {
                    SWLOR.NWN.API.NWNX.TilesetPlugin.DeleteTileOverride(instanceId);
                    lastFailure = $"CreateArea returned an invalid object for placeholder '{request.PlaceholderResref}' (seed {seed}).";
                    Log.Write(LogGroup.Error, lastFailure);
                    continue;
                }

                if (!AreaSynthesizer.ValidatePaths(area, resolved, out var pathFailure))
                {
                    DestroyRealizedArea(area, instanceId);
                    lastFailure = $"Path validation failed (seed {seed}): {pathFailure}";
                    Log.Write(LogGroup.Server, lastFailure);
                    continue;
                }

                var instance = new RuntimeAreaInstance
                {
                    InstanceId = instanceId,
                    Area = area,
                    OverrideName = instanceId,
                    Layout = resolved,
                    Request = request
                };
                AreaSynthesizer.ComputeWalkablePoints(area, resolved, instance);
                RuntimeAreaRegistry.Register(instance);

                return new AreaGenerationResult
                {
                    Success = true,
                    InstanceId = instanceId,
                    Area = area,
                    Layout = resolved,
                    SeedUsed = seed,
                    AttemptsUsed = attempt + 1
                };
            }

            Log.Write(LogGroup.Error, $"Area generation aborted after {request.MaxAttempts} attempts. Last failure: {lastFailure}");
            return new AreaGenerationResult
            {
                Success = false,
                FailureReason = lastFailure,
                AttemptsUsed = request.MaxAttempts
            };
        }

        /// <summary>
        /// Destroys a generated instance. Refuses while players are inside — callers evacuate
        /// first (RuntimeAreaInstance.ExitLocation) and retry, mirroring the shuttle teardown pattern.
        /// </summary>
        public static bool DestroyGeneratedArea(string instanceId, out string failureReason)
        {
            failureReason = string.Empty;

            if (!RuntimeAreaRegistry.TryGetById(instanceId, out var instance))
            {
                failureReason = $"'{instanceId}' is not a registered generated area.";
                return false;
            }

            var result = DestroyArea(instance.Area);
            if (result != 1)
            {
                failureReason = result == -2
                    ? "Players are still inside the area."
                    : $"DestroyArea returned {result}.";
                return false;
            }

            SWLOR.NWN.API.NWNX.TilesetPlugin.DeleteTileOverride(instance.OverrideName);
            RuntimeAreaRegistry.Unregister(instanceId);
            return true;
        }

        private static void DestroyRealizedArea(uint area, string overrideName)
        {
            DestroyArea(area);
            SWLOR.NWN.API.NWNX.TilesetPlugin.DeleteTileOverride(overrideName);
        }
    }
}
