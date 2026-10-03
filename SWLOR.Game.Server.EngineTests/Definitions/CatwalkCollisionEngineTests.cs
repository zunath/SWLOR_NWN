using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CatwalkCollisionEngineTests
    {
        private static readonly Vector3 WestPosition = new(41.9f, 65.13f, 0f);
        private static readonly Vector3 EastPosition = new(44.8f, 65.13f, 0f);

        [EngineTest("Nar Shaddaa catwalk doorway remains traversable", Category = "CatwalkCollision", TimeoutSeconds = 30f)]
        public static async Task CatwalkDoorwayPassabilityAndLineOfSight(EngineTestContext ctx)
        {
            var areaId = ctx.CreateInstancedArea("pw_ar_narcatwalk");
            await ctx.WaitFrameAsync();

            var server = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp;
            var area = server.GetGameObject(areaId).AsNWSArea();
            VerifyTowerWalkMesh(ctx);
            var mover = CreateAreaCivilian(ctx, areaId, WestPosition, "catwalk_doorway_mover");
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(areaId, GetArea(mover), "The doorway mover belongs to the Catwalks instance.");
            ctx.Assert(System.Numerics.Vector3.Distance(GetPosition(mover), WestPosition) < 0.2f,
                "The mover starts at the west-side test position.");

            ctx.AssertEqual(1, WalkingLine(area, WestPosition, EastPosition),
                "The direct walking line is clear from west to east through the doorway.");
            ctx.AssertEqual(1, WalkingLine(area, EastPosition, WestPosition),
                "The direct walking line is clear from east to west through the doorway.");

            await WalkBetweenAsync(ctx, mover, areaId, EastPosition, "east side of the doorway");
            await WalkBetweenAsync(ctx, mover, areaId, WestPosition, "west side of the doorway");

            var witness = CreateAreaCivilian(ctx, areaId, EastPosition, "catwalk_doorway_los_witness");
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(areaId, GetArea(witness), "The LOS witness belongs to the Catwalks instance.");
            ctx.Assert(System.Numerics.Vector3.Distance(GetPosition(witness), EastPosition) < 0.2f,
                "The LOS witness remains at the east-side test position.");
            await ctx.ExecuteInCreatureContextAsync(mover, () =>
            {
                ctx.Assert(LineOfSightObject(mover, witness), "The west-side creature can see the east-side creature.");
                ctx.Assert(LineOfSightObject(witness, mover), "The east-side creature can see the west-side creature.");

                foreach (var height in new[] { 1f, 1.6f })
                {
                    var west = new Vector3(WestPosition.X, WestPosition.Y, height);
                    var east = new Vector3(EastPosition.X, EastPosition.Y, height);
                    ctx.Assert(LineOfSightVector(west, east), $"The area-aware LOS vector is clear west to east at height {height}.");
                    ctx.Assert(LineOfSightVector(east, west), $"The area-aware LOS vector is clear east to west at height {height}.");
                }

                var towerStart = new Vector3(20f, 75.1f, 1f);
                var towerEnd = new Vector3(50f, 75.1f, 1f);
                ctx.Assert(!LineOfSightVector(towerStart, towerEnd), "The tower remains an area-aware LOS blocker in the negative control.");
                ctx.Assert(WalkingLine(area, towerStart, towerEnd) != 1,
                    "The tower remains a direct walking-line blocker in the negative control.");
            });

            ctx.SetResultDetail("On the placed pw_ar_narcatwalk instance, native walking lines pass through the open frame in both directions, a civilian physically walks across and back, and creatures plus area-aware vectors retain LOS at heights 1m and 1.6m. The neighboring tower blocks the negative-control crossing; its loaded daf_sw011 walkmesh has 64 vertices and 94 triangles.");
        }

        private static uint CreateAreaCivilian(EngineTestContext ctx, uint areaId, Vector3 position, string tag)
        {
            var creature = CreateObject(ObjectType.Creature, "civilian", Location(areaId, position, 0f));
            ctx.Assert(GetIsObjectValid(creature), $"Creature '{tag}' is created.");
            SetTag(creature, tag);
            ctx.Track(creature);
            return creature;
        }

        private static async Task WalkBetweenAsync(
            EngineTestContext ctx,
            uint creature,
            uint areaId,
            Vector3 destinationPosition,
            string destinationName)
        {
            var destination = Location(areaId, destinationPosition, 0f);
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                ClearAllActions();
                ActionMoveToLocation(destination, true);
            });
            await ctx.WaitUntilAsync(
                () => GetArea(creature) == areaId &&
                      GetDistanceBetweenLocations(GetLocation(creature), destination) < 0.2f,
                10f,
                $"the civilian to walk to the {destinationName}");
        }

        private static int WalkingLine(
            global::NWN.Native.API.CNWSArea area,
            Vector3 start,
            Vector3 end) => area.TestDirectLine(
                start.X,
                start.Y,
                end.X,
                end.Y,
                0.5f,
                1.6f,
                1);

        private static void VerifyTowerWalkMesh(EngineTestContext ctx)
        {
            using var mesh = new global::NWN.Native.API.CNWPlaceableSurfaceMesh();
            using var model = new global::NWN.Native.API.CResRef("daf_sw011");
            ctx.Assert(mesh.LoadWalkMesh(model) != 0, "The engine loads the tower's daf_sw011 walkmesh.");
            ctx.AssertEqual(64, mesh.m_nVertices, "The revised cylindrical tower walkmesh vertex count.");
            ctx.AssertEqual(94, mesh.m_nTriangles, "The revised cylindrical tower walkmesh triangle count.");
        }
    }
}
