using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using CNWSArea = NWN.Native.API.CNWSArea;
using NWNXLib = NWN.Native.API.NWNXLib;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Walkmesh: the logic and state that used to be static in the <c>Walkmesh</c> facade, moved here unchanged. The
    /// event handlers run on the container's instance; static callers reach it through <c>GameServices.Walkmesh</c>.
    /// </summary>
    public class WalkmeshService : IWalkmeshService
    {
        private readonly IDBService _db;
        // Working state of the boot-only bake (LoadWalkmeshes); nothing reads it after the bake.
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, List<uint>> _noSpawnZoneTriggers = new();
        private Dictionary<string, List<Vector3>> _walkmeshesByArea = new();
        // Filled at boot from the cache and, for an area without a spawn table, on its first placeable spawn.
        [ReloadState(ReloadStateKind.Runtime)]
        private Dictionary<string, List<Vector3>> _placeableSpawnPointsByArea = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<int, bool> _surfaceMaterialIsWalkable = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<ushort, bool> _placeableHasWalkmesh = new();
        private readonly (float X, float Y)[] _stepDirections =
        {
            (1f, 0f), (0.7071f, 0.7071f), (0f, 1f), (-0.7071f, 0.7071f),
            (-1f, 0f), (-0.7071f, -0.7071f), (0f, -1f), (0.7071f, -0.7071f),
        };
        private const int AreaBakeStep = 2;

        /// <summary>
        /// Raised whenever the bake rules change, so a cache written by an older build is baked again at startup even
        /// when the module itself did not change.
        /// </summary>
        public const int BakeVersion = 2;

        // Areas with these spawn tables get random placeable spawns, which need the stricter placeable points.
        private static readonly string[] _spawnTableVariables =
        {
            "RESOURCE_SPAWN_TABLE_ID", "SLICING_TERMINAL_SPAWN_TABLE_ID", "CREATURE_SPAWN_TABLE_ID",
        };
        [ReloadState(ReloadStateKind.Runtime)]
        private bool _bakingRan;

        public WalkmeshService(IDBService db)
        {
            _db = db;
        }

        [BootPhase(BootReload.BootOnly, NothingToAdopt = "The walkmeshes it stored stay in Redis; the rerun RetrieveWalkmeshes reads them.")]
        [NWNEventHandler(ScriptName.OnModuleContentChange)]
        public void LoadWalkmeshes()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            StoreNoSpawnZoneTriggers();
            _placeableSpawnPointsByArea = new Dictionary<string, List<Vector3>>();

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                BakeArea(area);
                if (HasSpawnTable(area))
                    _placeableSpawnPointsByArea[GetResRef(area)] = BakePlaceablePoints(area);
            }

            var serverConfig = _db.Get<ModuleCache>("SWLOR_CACHE") ?? new ModuleCache{ Id = "SWLOR_CACHE" };
            serverConfig.WalkmeshesByArea = _walkmeshesByArea;
            serverConfig.PlaceableSpawnPointsByArea = _placeableSpawnPointsByArea;
            serverConfig.WalkmeshBakeVersion = BakeVersion;
            _db.Set(serverConfig);

            _bakingRan = true;
            Console.WriteLine($"Baked {_walkmeshesByArea.Count} areas ({_placeableSpawnPointsByArea.Count} with placeable " +
                              $"spawn points) in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
        }

        /// <summary>
        /// When the module loads, find all of the "no spawn zone" triggers that have been hand placed by a builder.
        /// These indicate that walkmesh locations within the trigger are not valid and will be excluded from the list.
        /// </summary>
        private void StoreNoSpawnZoneTriggers()
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

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void RetrieveWalkmeshes()
        {
            if (_bakingRan)
                return;

            var serverConfig = _db.Get<ModuleCache>("SWLOR_CACHE");
            if (serverConfig?.WalkmeshesByArea == null ||
                serverConfig.PlaceableSpawnPointsByArea == null ||
                serverConfig.WalkmeshBakeVersion != BakeVersion)
            {
                Console.WriteLine("Cached walkmeshes come from an older bake. Baking them again.");
                LoadWalkmeshes();
                return;
            }

            _walkmeshesByArea = serverConfig.WalkmeshesByArea;
            _placeableSpawnPointsByArea = serverConfig.PlaceableSpawnPointsByArea;
            Console.WriteLine($"Loaded {_walkmeshesByArea.Count} area walkmeshes.");
        }

        // Area baking process
        // Run through and look for valid locations for later use by the spawn system.
        // Each tile is 10x10 meters. The "step" value in the config table determines how many meters we progress before checking for a valid location.
        private void BakeArea(uint area)
        {
            var resref = GetResRef(area);
            _walkmeshesByArea[resref] = new List<Vector3>();

            const float MinDistance = 6.0f;
            var width = GetAreaSize(Dimension.Width, area);
            var height = GetAreaSize(Dimension.Height, area);

            var arraySizeX = width * (10 / AreaBakeStep);
            var arraySizeY = height * (10 / AreaBakeStep);
            var nativeArea = GetNativeArea(area);
            var objectClearance = GetObjectClearanceGrid(area, MinDistance);

            // Column and row 0 lie on the area's west and south edges, where a creature stands half outside the map,
            // past the strip players can walk on. Starting one step in keeps every point at least one step from all
            // four edges, as the last column and row already are from the east and north edges.
            for (var x = 1; x < arraySizeX; x++)
            {
                for (var y = 1; y < arraySizeY; y++)
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
        private SpawnClearanceGrid GetObjectClearanceGrid(uint area, float radius)
        {
            var positions = new List<Vector3>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var type = GetObjectType(obj);
                if (type == ObjectType.Placeable)
                {
                    var appearance = GetPlaceableAppearance(obj);
                    if (!_placeableHasWalkmesh.TryGetValue(appearance, out var hasWalkmesh))
                    {
                        _placeableHasWalkmesh[appearance] = hasWalkmesh = AppearanceHasWalkmesh(appearance);
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
        private bool LacksStandingRoom(object nativeArea, float x, float y, float groundHeight)
        {
            const float StepLength = 0.5f;
            const float BelowGround = 1.0f;
            const float AboveGround = 2.0f;
            if (nativeArea == null)
                return false;

            foreach (var (dx, dy) in _stepDirections)
            {
                if (NoNonWalkPolys(nativeArea, x, y, x + dx * StepLength, y + dy * StepLength,
                        groundHeight - BelowGround, groundHeight + AboveGround) == 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a random location for a randomly placed placeable (resource node, terminal). These points also
        /// leave room to walk around the placeable, so it cannot block a passage. An area without such points falls
        /// back to the ordinary random spawn points.
        /// </summary>
        /// <param name="area">The area to retrieve a random location for.</param>
        /// <returns>A random location within an area.</returns>
        public Location GetRandomPlaceableLocation(uint area)
        {
            var points = GetPlaceableSpawnPoints(area);
            if (points.Count <= 0)
                return GetRandomLocation(area);

            var position = points[Random.Next(points.Count)];
            return Location(area, position, 0.0f);
        }

        /// <summary>
        /// Returns the baked points of an area where a placeable leaves room to be walked around. The startup bake works
        /// them out for every area with a spawn table; any other area works them out on its first placeable spawn.
        /// </summary>
        /// <param name="area">The area to retrieve the points for.</param>
        /// <returns>The subset of the area's random spawn points that suit placeables.</returns>
        public IReadOnlyList<Vector3> GetPlaceableSpawnPoints(uint area)
        {
            var resref = GetResRef(area);
            if (!_placeableSpawnPointsByArea.TryGetValue(resref, out var points))
                _placeableSpawnPointsByArea[resref] = points = BakePlaceablePoints(area);

            return points;
        }

        private static bool HasSpawnTable(uint area)
        {
            foreach (var variable in _spawnTableVariables)
            {
                if (!string.IsNullOrWhiteSpace(GetLocalString(area, variable)))
                    return true;
            }

            return false;
        }

        private List<Vector3> BakePlaceablePoints(uint area)
        {
            var points = new List<Vector3>();
            var nativeArea = GetNativeArea(area);
            if (nativeArea == null || !_walkmeshesByArea.TryGetValue(GetResRef(area), out var baked))
                return points;

            var grid = new WalkableGrid(
                GetAreaSize(Dimension.Width, area) * 10f,
                GetAreaSize(Dimension.Height, area) * 10f,
                (x, y) => SampleGround(area, x, y));
            foreach (var position in baked)
            {
                if (LeavesWalkingRoom(nativeArea, grid, position))
                    points.Add(position);
            }

            return points;
        }

        private (bool Walkable, float Height) SampleGround(uint area, float x, float y)
        {
            var location = Location(area, Vector3(x, y), 0.0f);
            var material = GetSurfaceMaterial(location);
            if (!_surfaceMaterialIsWalkable.TryGetValue(material, out var walkable))
                _surfaceMaterialIsWalkable[material] = walkable = Get2DAString("surfacemat", "Walk", material) == "1";

            return (walkable, GetGroundHeight(location));
        }

        /// <summary>
        /// Returns true when a node placed at the point leaves the ground around it connected (see
        /// <see cref="PlaceableSpawnRoom"/>). A line is walkable when the tile walkmesh carries it (cave walls and cliffs
        /// block it, see <see cref="WalkableGrid"/>) and it crosses no placeable walkmesh. NoNonWalkPolys alone sees
        /// only placeables: it reports a line through a cave wall as clear.
        /// </summary>
        private bool LeavesWalkingRoom(object nativeArea, WalkableGrid grid, Vector3 position)
        {
            const float BelowGround = 1.0f;
            const float AboveGround = 2.0f;
            var minZ = position.Z - BelowGround;
            var maxZ = position.Z + AboveGround;

            return PlaceableSpawnRoom.LeavesWalkingRoom(position.X, position.Y,
                (fromX, fromY, toX, toY) =>
                    grid.IsLineWalkable(fromX, fromY, toX, toY) &&
                    NoNonWalkPolys(nativeArea, fromX, fromY, toX, toY, minZ, maxZ) != 0);
        }

        /// <summary>
        /// Retrieves a random location from the walkmeshes in an area.
        /// </summary>
        /// <param name="area">The area to retrieve a random location for.</param>
        /// <returns>A random location within an area.</returns>
        public Location GetRandomLocation(uint area)
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

        /// <summary>
        /// The engine's native area object, or null when the engine has none. The server's native structures cannot be
        /// built without a running server, so this read is one overridable line whose body is the original call; the
        /// bake only passes the result on to <see cref="NoNonWalkPolys"/>.
        /// </summary>
        protected virtual object GetNativeArea(uint area)
        {
            return NWNXLib.g_pAppManager.m_pServerExoApp.GetAreaByGameObjectID(area);
        }

        /// <summary>
        /// The engine's collision test along one step of the area: how many non-walkable polygons lie between the two
        /// points within the height range. The body is the original native call.
        /// </summary>
        protected virtual int NoNonWalkPolys(object nativeArea, float fromX, float fromY, float toX, float toY,
            float lowZ, float highZ)
        {
            return ((CNWSArea)nativeArea).NoNonWalkPolys(fromX, fromY, toX, toY, lowZ, highZ, 0.0f);
        }

        /// <summary>
        /// The appearance row of a placeable, read from the server's native object. The body is the original native
        /// read.
        /// </summary>
        protected virtual ushort GetPlaceableAppearance(uint placeable)
        {
            return NWNXLib.g_pAppManager.m_pServerExoApp.GetPlaceableByGameObjectID(placeable).m_nAppearance;
        }

        /// <summary>
        /// Whether the placeable appearance has a walkmesh with triangles in the server's placeable mesh manager. The
        /// body is the original native read.
        /// </summary>
        protected virtual bool AppearanceHasWalkmesh(ushort appearance)
        {
            var meshManager = NWNXLib.g_pAppManager.m_pServerExoApp.m_pcExoAppInternal.m_pPlaceMeshManager;
            var mesh = meshManager.GetWalkMesh(appearance);
            return mesh != null && mesh.m_nTriangles > 0;
        }
    }
}
