using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Gives each new connection the slot <see cref="PlayerSlotRotation"/> picks instead of the lowest free one.
    /// A connection's slot is its player ID, which clients store with every chat line to start portrait tells,
    /// so a slot handed straight to the next login sent tells meant for a departed player to the newcomer.
    /// </summary>
    public static unsafe class PlayerSlotAssignment
    {
        private const string SetSlidingWindowSymbol = "_ZN17CNetLayerInternal16SetSlidingWindowE16CNetConnectionIdPj";

        /// <summary>CNetLayer::MAX_PLAYERS: the engine's sliding windows, one per connection slot.</summary>
        public const int SlotCount = 255;

        // CNetLayerInternal keeps its CNetLayerWindow array at +0x68. SetSlidingWindow scans it for the lowest
        // window that is free (m_bWindowInUse == 0) and makes that window's index the connection's player ID.
        private const int WindowArrayOffset = 0x68;
        private const int WindowSize = 0x960;
        private const int WindowNetLayerOffset = 0x08;
        private const int WindowInUseOffset = 0x10;
        private const int WindowConnectionIdOffset = 0x18;

        // CNetLayer: m_nInstance, then m_pcNetLayerInternal.
        private const int NetLayerInternalOffset = 0x08;

        // SetSlidingWindow passes over a window whose flag is neither 0 (free) nor 1 (in use).
        private const int HeldBack = 2;

        private static readonly PlayerSlotRotation Rotation = new(SlotCount);
        private static FunctionHook* _hook;
        private static void* _checkedNetLayer;
        private static bool _disabled;

        /// <summary>True once the hook is in place and has not turned itself off.</summary>
        public static bool IsActive => _hook != null && !_disabled;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_hook != null)
                return;

            delegate* unmanaged<void*, uint, uint*, int> setSlidingWindow = &SetSlidingWindow;
            _hook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), SetSlidingWindowSymbol),
                (IntPtr)setSlidingWindow, HookOrder.Earliest);
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

        [UnmanagedCallersOnly]
        private static int SetSlidingWindow(void* netLayer, uint connectionId, uint* windowId)
        {
            var original = (delegate* unmanaged<void*, uint, uint*, int>)_hook->m_trampoline;
            Span<bool> heldBack = stackalloc bool[SlotCount];
            var preferred = -1;

            try
            {
                preferred = HoldBackSlotsBefore(netLayer, connectionId, heldBack);
            }
            catch (Exception exception)
            {
                Release(netLayer, heldBack);
                preferred = -1;
                Disable(exception, "Could not pick a connection slot");
            }

            int result;
            try
            {
                result = original(netLayer, connectionId, windowId);
            }
            finally
            {
                Release(netLayer, heldBack);
            }

            if (preferred < 0 || result == 0)
                return result;

            try
            {
                var slot = (int)*windowId;
                Rotation.MarkInUse(slot);
                if (slot != preferred)
                    Disable(null, $"The engine gave connection slot {slot} instead of {preferred}");
            }
            catch (Exception exception)
            {
                Disable(exception, "Could not record a connection slot");
            }

            return result;
        }

        /// <summary>
        /// Marks every free slot below the rotation's pick as held back, so the engine's lowest-free scan lands
        /// on the pick. Returns the pick, or -1 when the engine should choose on its own.
        /// </summary>
        private static int HoldBackSlotsBefore(void* netLayer, uint connectionId, Span<bool> heldBack)
        {
            if (_disabled)
                return -1;

            if (netLayer != _checkedNetLayer)
            {
                if (!LayoutMatches((IntPtr)netLayer))
                {
                    Disable(null, "The engine's connection slot layout does not match this build");
                    return -1;
                }

                _checkedNetLayer = netLayer;
            }

            Span<bool> inUse = stackalloc bool[SlotCount];
            var reconnecting = false;
            for (var slot = 0; slot < SlotCount; slot++)
            {
                var flag = InUseFlag(netLayer, slot);
                inUse[slot] = flag != 0;
                if (flag == 1 && *(uint*)(Window(netLayer, slot) + WindowConnectionIdOffset) == connectionId)
                    reconnecting = true;
            }

            Rotation.Observe(inUse);

            // The engine hands an existing connection its own window back.
            if (reconnecting)
                return -1;

            var preferred = Rotation.ChooseSlot();
            for (var slot = 0; slot < preferred; slot++)
            {
                if (inUse[slot])
                    continue;

                *(int*)(Window(netLayer, slot) + WindowInUseOffset) = HeldBack;
                heldBack[slot] = true;
            }

            return preferred;
        }

        private static void Release(void* netLayer, Span<bool> heldBack)
        {
            for (var slot = 0; slot < heldBack.Length; slot++)
            {
                if (!heldBack[slot])
                    continue;

                *(int*)(Window(netLayer, slot) + WindowInUseOffset) = 0;
                heldBack[slot] = false;
            }
        }

        private static void Disable(Exception exception, string reason)
        {
            _disabled = true;
            const string Fallback = "; new connections take the lowest free slot until the server restarts";
            if (exception == null)
                Log.Write(LogGroup.Error, reason + Fallback, true);
            else
                Log.WriteError(exception, reason + Fallback);
        }

        private static int InUseFlag(void* netLayer, int slot) => *(int*)(Window(netLayer, slot) + WindowInUseOffset);

        private static byte* Window(void* netLayer, int slot)
        {
            if ((uint)slot >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "No such connection slot.");

            return (byte*)netLayer + WindowArrayOffset + slot * WindowSize;
        }
    }
}
