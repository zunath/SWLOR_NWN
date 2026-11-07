using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static unsafe class PlayerSlotAssignmentEngineTests
    {
        private const string SetSlidingWindowSymbol = "_ZN17CNetLayerInternal16SetSlidingWindowE16CNetConnectionIdPj";
        private const int WindowArrayOffset = 0x68;
        private const int WindowSize = 0x960;
        private const int WindowInUseOffset = 0x10;

        // Connection IDs no real client holds in the test server.
        private const uint FirstConnection = 0xFFFFFF01;
        private const uint SecondConnection = 0xFFFFFF02;

        [EngineTest("A connection slot freed by a logout does not go to the next login", Category = "PlayerName", TimeoutSeconds = 30f)]
        public static Task FreedSlotWaitsBehindUnusedSlots(EngineTestContext ctx)
        {
            var netLayer = PlayerSlotAssignment.ServerNetLayer();
            ctx.Assert(PlayerSlotAssignment.IsActive, "the slot rotation hook is active");
            ctx.Assert(PlayerSlotAssignment.LayoutMatches(netLayer), "the engine's slot layout matches the hook");

            var setSlidingWindow = (delegate* unmanaged<void*, uint, uint*, int>)NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), SetSlidingWindowSymbol);
            uint first = uint.MaxValue, second = uint.MaxValue;
            try
            {
                // Both connections are opened and freed inside this call, before the engine's network loop runs.
                ctx.AssertEqual(1, setSlidingWindow((void*)netLayer, FirstConnection, &first), "the first connection gets a slot");
                Free(netLayer, first);
                ctx.AssertEqual(1, setSlidingWindow((void*)netLayer, SecondConnection, &second), "the second connection gets a slot");

                ctx.Assert(second != first, $"the second connection takes a different slot than the freed {first}, got {second}");
                ctx.Assert(PlayerSlotAssignment.IsActive, "the engine gave the slot the rotation picked");
                for (var slot = 0; slot < PlayerSlotAssignment.SlotCount; slot++)
                    ctx.Assert(*(int*)Window(netLayer, slot) is 0 or 1, $"slot {slot} was released after the pick");
            }
            finally
            {
                Free(netLayer, first);
                Free(netLayer, second);
            }

            return Task.CompletedTask;
        }

        private static void Free(IntPtr netLayer, uint slot)
        {
            if (slot < PlayerSlotAssignment.SlotCount)
                *(int*)Window(netLayer, (int)slot) = 0;
        }

        private static byte* Window(IntPtr netLayer, int slot) =>
            (byte*)netLayer + WindowArrayOffset + slot * WindowSize + WindowInUseOffset;
    }
}
