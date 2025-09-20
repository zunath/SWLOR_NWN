using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.Shared.Core.Event;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    public static class Walkmesh
    {
        private static readonly Dictionary<uint, List<uint>> _noSpawnZoneTriggers = new();
        private static Dictionary<string, List<Vector3>> _walkmeshesByArea = new();
        private const int AreaBakeStep = 2;
        private static bool _bakingRan;

        /// <summary>
        /// When the module content changes, rerun the baking process.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleContentChange)]
        public static void LoadWalkmeshes()
        {
            StoreNoSpawnZoneTriggers();

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                BakeArea(area);
            }
            
            var serverConfig = DB.Get<ModuleCache>("SWLOR_CACHE") ?? new ModuleCache{ Id = "SWLOR_CACHE" };
            serverConfig.WalkmeshesByArea = _walkmeshesByArea;
            DB.Set(serverConfig);

            _bakingRan = true;
            Console.WriteLine($"Baked {_walkmeshesByArea.Count} areas.");
        }

        /// <summary>
        /// When the module loads, find all of the "no spawn zone" triggers that have been hand placed by a builder.
        /// These indicate that walkmesh locations within the trigger are not valid and will be excluded from the list.
        /// </summary>
        private static void StoreNoSpawnZoneTriggers()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    var resref = GetResRef(obj);

                    if (resref != "anti_spawn_trigg")
                        continue;

                    if (!_noSpawnZoneTriggers.ContainsKey(area))
                        _noSpawnZoneTriggers[area] = new List<uint>();

                    _noSpawnZoneTriggers[area].Add(obj);
                }
            }
        }

        /// <summary>
        /// When the module loads, retrieve the list of walkable locations from the database.
        /// These locations can be used to spawn objects randomly throughout an area.
        /// This only runs if the module content has NOT changed since the last run.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleLoad)]
        public static void RetrieveWalkmeshes()
        {
            if (_bakingRan)
                return;

            var serverConfig = DB.Get<ModuleCache>("SWLOR_CACHE");
            _walkmeshesByArea = serverConfig.WalkmeshesByArea;
            Console.WriteLine($"Loaded {_walkmeshesByArea.Count} area walkmeshes.");
        }

        // Area baking process
        // Run through and look for valid locations for later use by the spawn system.
        // Each tile is 10x10 meters. The "step" value in the config table determines how many meters we progress before checking for a valid location.
        private static void BakeArea(uint area)
        {
            var resref = GetResRef(area);
            _walkmeshesByArea[resref] = new List<Vector3>();

            const float MinDistance = 6.0f;
            var width = GetAreaSize(Dimension.Width, area);
            var height = GetAreaSize(Dimension.Height, area);

            var arraySizeX = width * (10 / AreaBakeStep);
            var arraySizeY = height * (10 / AreaBakeStep);

            for (var x = 0; x < arraySizeX; x++)
            {
                for (var y = 0; y < arraySizeY; y++)
                {
                    var checkPosition = Vector3(x * AreaBakeStep, y * AreaBakeStep);
                    var checkLocation = Location(area, checkPosition, 0.0f);
                    var material = GetSurfaceMaterial(checkLocation);
                    var isWalkable = Convert.ToInt32(Get2DAString("surfacemat", "Walk", material)) == 1;

                    // Location is not walkable if another object exists nearby.
                    var nearest = GetNearestObjectToLocation(checkLocation, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable | ObjectType.Trigger);
                    var distance = GetDistanceBetweenLocations(checkLocation, GetLocation(nearest));
                    if (GetIsObjectValid(nearest) && distance <= MinDistance)
                    {
                        isWalkable = false;
                    }

                    // Location is not walkable if it's contained within any "no spawn zone" triggers.
                    if (_noSpawnZoneTriggers.ContainsKey(area))
                    {
                        foreach (var trigger in _noSpawnZoneTriggers[area])
                        {
                            if (ObjectPlugin.GetPositionIsInTrigger(trigger, checkPosition))
                            {
                                isWalkable = false;
                                break;
                            }
                        }
                    }

                    if (isWalkable)
                    {
                        var position = new Vector3(x * AreaBakeStep,
                            y * AreaBakeStep,
                            GetGroundHeight(checkLocation));
                        _walkmeshesByArea[resref].Add(position);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a random location from the walkmeshes in an area.
        /// </summary>
        /// <param name="area">The area to retrieve a random location for.</param>
        /// <returns>A random location within an area.</returns>
        public static Location GetRandomLocation(uint area)
        {
            var resref = GetResRef(area);
            if (!_walkmeshesByArea.ContainsKey(resref)) 
                return Location(area, Vector3.Zero, 0.0f);

            var count = _walkmeshesByArea[resref].Count;
            if (count <= 0) 
                return Location(area, Vector3.Zero, 0.0f);

            var index = Random.Next(count);
            var position = _walkmeshesByArea[resref][index];
            return Location(area, position, 0.0f);
        }

    }
}
