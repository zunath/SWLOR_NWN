using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class WildlandsCliffCollisionEngineTests
    {
        [EngineTest("Wildlands northwest cliff blocks entry above its foundation", Category = "WildlandsCliffCollision", TimeoutSeconds = 50f)]
        public static async Task RaisedTerrainCannotEnterCliffBody(EngineTestContext ctx)
        {
            ctx.Log("Creating the Wildlands instance.");
            var areaId = ctx.CreateInstancedArea("viscarawildlands");
            await ctx.WaitFrameAsync();

            ctx.Log("Loading the cliff collision mesh.");
            using var mesh = new global::NWN.Native.API.CNWPlaceableSurfaceMesh();
            using var model = new global::NWN.Native.API.CResRef("sw_cliff_end2");
            ctx.Assert(mesh.LoadWalkMesh(model) != 0, "The cliff collision mesh loads.");
            ctx.AssertEqual(74, mesh.m_nVertices, "The solid cliff collision vertex count.");
            ctx.AssertEqual(144, mesh.m_nTriangles, "The solid cliff collision triangle count.");

            // Keep the crossing inside the tile rows instead of exactly on their Y=300 seam.
            var outside = new Vector3(13f, 301f, 2.366707f);
            var inside = new Vector3(3f, 301f, 2.241563f);
            var south = new Vector3(13f, 298f, 1.725255f);
            var north = new Vector3(13f, 304f, 3.385944f);
            var mover = CreateObject(ObjectType.Creature, "civilian", Location(areaId, outside, 0f));
            ctx.Assert(GetIsObjectValid(mover), "The exterior-route civilian is created.");
            ctx.Track(mover);
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(areaId, GetArea(mover), "The civilian belongs to the Wildlands instance.");
            ctx.Assert(System.Numerics.Vector3.Distance(GetPosition(mover), outside) < 0.2f,
                "The civilian starts at the exterior approach.");
            ctx.Log("Attempting to walk into the cliff.");
            var blockedDestination = Location(areaId, inside, 0f);
            await ctx.ExecuteInCreatureContextAsync(mover, () =>
            {
                ClearAllActions();
                ActionMoveToLocation(blockedDestination, true);
            });
            await ctx.DelaySecondsAsync(4f);
            ctx.Assert(GetDistanceBetweenLocations(GetLocation(mover), blockedDestination) > 5f,
                "The cliff stops the civilian outside its solid body.");

            ctx.Log("Walking the exterior route north and south.");
            await WalkToAsync(ctx, mover, areaId, north);
            await WalkToAsync(ctx, mover, areaId, south);

            var cliff = FindReportedCliff(areaId);
            ctx.Assert(GetIsObjectValid(cliff), "The reported northwest cliff is present in the instance.");
            // Remove the baked static mesh explicitly so the control tests the underlying terrain.
            var server = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp;
            var nativeArea = server.GetGameObject(areaId).AsNWSArea();
            var nativeCliff = server.GetGameObject(cliff).AsNWSPlaceable();
            var staticIndex = nativeCliff.m_nStaticObjectPosition;
            ctx.Assert(ObjectPlugin.GetPlaceableIsStatic(cliff) &&
                       staticIndex >= 0 && staticIndex < nativeArea.m_nStaticObjectsFilled,
                "The reported cliff has a baked static collision mesh.");
            nativeArea.RemoveStaticObject(staticIndex);
            nativeArea.RemoveStaticBoundingBox(cliff);
            nativeCliff.m_nStaticObjectPosition = -1;
            ObjectPlugin.SetPlaceableIsStatic(cliff, false);
            DestroyObject(cliff);
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(cliff), 3f, "the control cliff to be removed");
            ctx.Log("Walking the same crossing after removing the cliff.");
            await WalkToAsync(ctx, mover, areaId, outside);
            await WalkToAsync(ctx, mover, areaId, inside);
            ctx.SetResultDetail("The engine loads the 74-vertex, 144-triangle solid cliff PWK. A civilian cannot walk into the northwest cliff on 2m terrain, walks the exterior route north and south, and walks the original blocked crossing after the cliff is removed.");
        }

        private static uint FindReportedCliff(uint areaId)
        {
            for (var obj = GetFirstObjectInArea(areaId); GetIsObjectValid(obj); obj = GetNextObjectInArea(areaId))
            {
                if (GetObjectType(obj) != ObjectType.Placeable || ObjectPlugin.GetAppearance(obj) != 32090)
                    continue;
                var position = GetPosition(obj);
                if (System.Math.Abs(position.X - 2.79f) < 0.01f && System.Math.Abs(position.Y - 311.38f) < 0.01f)
                    return obj;
            }
            return OBJECT_INVALID;
        }

        private static async Task WalkToAsync(EngineTestContext ctx, uint mover, uint areaId, Vector3 position)
        {
            var destination = Location(areaId, position, 0f);
            await ctx.ExecuteInCreatureContextAsync(mover, () =>
            {
                ClearAllActions();
                ActionMoveToLocation(destination, true);
            });
            try
            {
                await ctx.WaitUntilAsync(
                    () => GetArea(mover) == areaId && GetDistanceBetweenLocations(GetLocation(mover), destination) < 0.5f,
                    10f, $"the civilian to walk to {position}");
            }
            finally
            {
                ctx.Log($"Walking to {position}: civilian at {GetPosition(mover)}, distance {GetDistanceBetweenLocations(GetLocation(mover), destination):F3}m.");
            }
        }
    }
}
