using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using NativeVector = NWN.Native.API.Vector;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class StaleDriveAfterJumpEngineTests
    {
        private const string ActorResref = "nw_bandit001";
        private const float JumpDistanceMeters = 6f;
        private const ushort DriveGroupId = 0;
        private const int DriveBearing = 900;
        private const int DriveClientPathNumber = 1;
        private const int DriveFlags = 3;
        private const int DriveWayPoints = 2;

        [EngineTest("A drive path sent from before a teleport starts at the landing point", Category = "Movement", TimeoutSeconds = 40f)]
        public static async Task StaleDriveStartAfterJumpUsesTheLandingPoint(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var creature = arena.Spawn(ActorResref, 6f, 90f);
            await ctx.WaitFrameAsync();

            var origin = GetPosition(creature);
            var landing = new System.Numerics.Vector3(origin.X, origin.Y + JumpDistanceMeters, origin.Z);
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                ClearAllActions(true);
                JumpToLocation(Location(GetArea(creature), landing, 90f));
            });
            await ctx.WaitUntilAsync(
                () => System.Numerics.Vector3.Distance(GetPosition(creature), landing) < 0.5f,
                5f, "the creature to land after its jump");

            var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(creature);
            var server = GetPosition(creature);

            // Keyboard movement sent before the client saw the jump still starts at the old position.
            var stale = new NativeVector(origin.X, origin.Y, origin.Z);
            native.AddDriveAction(DriveGroupId, stale, DriveBearing, DriveClientPathNumber, DriveFlags, DriveWayPoints);
            ctx.Assert(System.Numerics.Vector3.Distance(new System.Numerics.Vector3(stale.x, stale.y, stale.z), server) < 0.01f,
                $"A drive path sent from the pre-jump position must start at the landing point {server}, not ({stale.x:0.00}, {stale.y:0.00})");

            // A client that has not caught up keeps driving from its old track.
            var stillStale = new NativeVector(origin.X, origin.Y + 1f, origin.Z);
            native.AddDriveAction(DriveGroupId, stillStale, DriveBearing, DriveClientPathNumber + 1, DriveFlags, DriveWayPoints);
            ctx.Assert(System.Numerics.Vector3.Distance(new System.Numerics.Vector3(stillStale.x, stillStale.y, stillStale.z), GetPosition(creature)) < 0.01f,
                $"A later path from the client's old track must also start at the server position, got ({stillStale.x:0.00}, {stillStale.y:0.00})");

            // A new path that starts at the creature's own position is left as the client sent it.
            server = GetPosition(creature);
            var current = new NativeVector(server.X + 0.5f, server.Y, server.Z);
            native.AddDriveAction(DriveGroupId, current, DriveBearing, DriveClientPathNumber + 2, DriveFlags, DriveWayPoints);
            ctx.Assert(System.Math.Abs(current.x - (server.X + 0.5f)) < 0.001f && System.Math.Abs(current.y - server.Y) < 0.001f,
                $"A drive path that starts near the creature must not change, got ({current.x:0.00}, {current.y:0.00})");

            // Once the client has caught up, a far start is the engine's to handle again.
            var server2 = GetPosition(creature);
            var late = new NativeVector(origin.X, origin.Y, origin.Z);
            native.AddDriveAction(DriveGroupId, late, DriveBearing, DriveClientPathNumber + 3, DriveFlags, DriveWayPoints);
            ctx.Assert(System.Math.Abs(late.y - origin.Y) < 0.001f,
                $"A far drive start long after the jump must not be rewritten (server {server2}, got ({late.x:0.00}, {late.y:0.00}))");
            await ctx.ExecuteInCreatureContextAsync(creature, () => ClearAllActions(true));
        }
    }
}
