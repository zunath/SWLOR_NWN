using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using CNWSArea = NWN.Native.API.CNWSArea;
using NWNXLib = NWN.Native.API.NWNXLib;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    public static class Walkmesh
    {
        private static readonly Dictionary<uint, List<uint>> _noSpawnZoneTriggers = new();
        private static Dictionary<string, List<Vector3>> _walkmeshesByArea = new();
        private static readonly Dictionary<ushort, bool> _placeableHasWalkmesh = new();
        private static readonly (float X, float Y)[] _stepDirections =
        {
            (1f, 0f), (0.7071f, 0.7071f), (0f, 1f), (-0.7071f, 0.7071f),
            (-1f, 0f), (-0.7071f, -0.7071f), (0f, -1f), (0.7071f, -0.7071f),
        };
        private const int AreaBakeStep = 2;
        private static bool _bakingRan;

        /// <summary>
        /// When the module content changes, rerun the baking process.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleContentChange)]
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
        [NWNEventHandler(ScriptName.OnModuleLoad)]
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
            var nativeArea = NWNXLib.g_pAppManager.m_pServerExoApp.GetAreaByGameObjectID(area);
            var objectClearance = GetObjectClearanceGrid(area, MinDistance);

            for (var x = 0; x < arraySizeX; x++)
            {
                for (var y = 0; y < arraySizeY; y++)
                {
                    var checkPosition = Vector3(x * AreaBakeStep, y * AreaBakeStep);
                    var checkLocation = Location(area, checkPosition, 0.0f);
                    var material = GetSurfaceMaterial(checkLocation);
                    if (Convert.ToInt32(Get2DAString("surfacemat", "Walk", material)) != 1)
                        continue;

                    var groundHeight = GetGroundHeight(checkLocation);
                    var isWalkable = true;

                    // Location is not walkable if another object exists nearby. Distances are measured from the ground,
                    // not from Z 0, so raised terrain keeps the full radius.
                    if (objectClearance.HasObjectWithin(new Vector3(checkPosition.X, checkPosition.Y, groundHeight)))
                    {
                        isWalkable = false;
                    }

                    // Location is not walkable if a creature there has no room to stand, e.g. inside or against a
                    // placeable's walkmesh, which the tile surface material does not know about.
                    if (isWalkable && LacksStandingRoom(nativeArea, checkPosition.X, checkPosition.Y, groundHeight))
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
                            groundHeight);
                        _walkmeshesByArea[resref].Add(position);
                    }
                }
            }
        }

        // Placeables without a walkmesh (grass, flowers, dirt decals) block nothing, so they do not push spawns away.
        private static SpawnClearanceGrid GetObjectClearanceGrid(uint area, float radius)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var meshManager = server.m_pcExoAppInternal.m_pPlaceMeshManager;
            var positions = new List<Vector3>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var type = GetObjectType(obj);
                if (type == ObjectType.Placeable)
                {
                    var appearance = server.GetPlaceableByGameObjectID(obj).m_nAppearance;
                    if (!_placeableHasWalkmesh.TryGetValue(appearance, out var hasWalkmesh))
                    {
                        var mesh = meshManager.GetWalkMesh(appearance);
                        _placeableHasWalkmesh[appearance] = hasWalkmesh = mesh != null && mesh.m_nTriangles > 0;
                    }

                    if (hasWalkmesh)
                        positions.Add(GetPosition(obj));
                }
                else if (type == ObjectType.Creature || type == ObjectType.Door || type == ObjectType.Trigger)
                {
                    positions.Add(GetPosition(obj));
                }
            }

            return new SpawnClearanceGrid(positions, radius);
        }

        /// <summary>
        /// Asks the engine's collision test whether a creature at the position has room to stand: a clear step in
        /// every direction. The tile surface material ignores placeable walkmeshes, and a creature created inside or
        /// against one is nudged into it and never walks out.
        /// </summary>
        private static bool LacksStandingRoom(CNWSArea nativeArea, float x, float y, float groundHeight)
        {
            const float StepLength = 0.5f;
            const float BelowGround = 1.0f;
            const float AboveGround = 2.0f;
            if (nativeArea == null)
                return false;

            foreach (var (dx, dy) in _stepDirections)
            {
                if (nativeArea.NoNonWalkPolys(x, y, x + dx * StepLength, y + dy * StepLength,
                        groundHeight - BelowGround, groundHeight + AboveGround, 0.0f) == 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a random location from the walkmeshes in an area.
        /// </summary>
        /// <param name="area">The area to retrieve a random location for.</param>
        /// <returns>A random location within an area.</returns>
        public static Location GetRandomLocation(uint area)
        {
            return GetRandomLocation(area, GetResRef(area));
        }

        /// <summary>
        /// A random walkable location in <paramref name="area"/> from the walkmesh baked for <paramref name="templateResref"/>.
        /// An instance made from a template (Area.CreateInstance) gets a generated resref such as "nw_3" that was never
        /// baked, so callers placing things in an instance pass the template's resref (W11r).
        /// </summary>
        public static Location GetRandomLocation(uint area, string templateResref)
        {
            var resref = templateResref;
            if (string.IsNullOrEmpty(resref) || !_walkmeshesByArea.ContainsKey(resref))
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
