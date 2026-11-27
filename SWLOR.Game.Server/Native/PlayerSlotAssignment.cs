using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Gives each new connection the slot <see cref="PlayerSlotRotation"/> picks instead of the lowest free one.
    /// A connection's slot is its player ID, which clients store with every chat line to start portrait tells,
    /// so a slot handed straight to the next login sent tells meant for a departed player to the newcomer.
    /// </summary>
    public static unsafe class PlayerSlotAssignment
    {
        /// <summary>CNetLayer::MAX_PLAYERS: the engine's sliding windows, one per connection slot.</summary>
        public const int SlotCount = 255;

        // CNetLayerInternal keeps its CNetLayerWindow array at +0x68. SetSlidingWindow scans it for the lowest
        // window that is free (m_bWindowInUse == 0) and makes that window's index the connection's player ID.
        internal const int WindowArrayOffset = 0x68;
        internal const int WindowSize = 0x960;
        internal const int WindowNetLayerOffset = 0x08;
        internal const int WindowInUseOffset = 0x10;
        internal const int WindowConnectionIdOffset = 0x18;

        // CNetLayer: m_nInstance, then m_pcNetLayerInternal.
        internal const int NetLayerInternalOffset = 0x08;

        // SetSlidingWindow passes over a window whose flag is neither 0 (free) nor 1 (in use).
        internal const int HeldBack = 2;

        /// <summary>Set once the hook has turned itself off (written by <see cref="PlayerSlotAssignmentSink"/>).</summary>
        internal static bool HookDisabled;

        /// <summary>True once the hook is in place and has not turned itself off.</summary>
        public static bool IsActive => NativeHookAccess.Hooks.IsEnabled(NativeHookId.PlayerSlotAssignment) && !HookDisabled;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.PlayerSlotAssignment);
        }

        /// <summary>The server's CNetLayerInternal, which owns the connection slots.</summary>
        public static IntPtr ServerNetLayer()
        {
            var netLayer = NWNXLib.g_pAppManager.m_pServerExoApp.GetNetLayer();
            return *(IntPtr*)((byte*)netLayer.Pointer + NetLayerInternalOffset);
        }

        /// <summary>
        /// True when every window in the expected array points back at this net layer, so the offsets above
        /// still match the engine build.
        /// </summary>
        public static bool LayoutMatches(IntPtr netLayer)
        {
            if (netLayer == IntPtr.Zero)
                return false;

            for (var slot = 0; slot < SlotCount; slot++)
            {
                if (*(void**)(Window((void*)netLayer, slot) + WindowNetLayerOffset) != (void*)netLayer)
                    return false;
            }

            return true;
        }

        /// <summary>Whether the engine has this slot in use.</summary>
        public static bool IsSlotInUse(IntPtr netLayer, int slot) => InUseFlag((void*)netLayer, slot) != 0;

        internal static int InUseFlag(void* netLayer, int slot) => *(int*)(Window(netLayer, slot) + WindowInUseOffset);

        internal static byte* Window(void* netLayer, int slot)
        {
            if ((uint)slot >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "No such connection slot.");

            return (byte*)netLayer + WindowArrayOffset + slot * WindowSize;
        }
    }
}
