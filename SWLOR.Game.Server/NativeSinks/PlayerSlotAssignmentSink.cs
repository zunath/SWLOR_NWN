using System.Runtime.InteropServices;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Native;
using static SWLOR.Game.Server.Native.PlayerSlotAssignment;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Gives each new connection the slot <see cref="PlayerSlotRotation"/> picks instead of the lowest free one.
/// A connection's slot is its player ID, which clients store with every chat line to start portrait tells,
/// so a slot handed straight to the next login sent tells meant for a departed player to the newcomer.
/// <see cref="PlayerSlotAssignment"/> installs the hook and knows the engine's slot layout.
/// </summary>
public sealed unsafe class PlayerSlotAssignmentSink : IPlayerSlotAssignmentSink
{
    private readonly PlayerSlotRotation _rotation = new(SlotCount);
    private void* _checkedNetLayer;

    public int SetSlidingWindow(void* netLayer, uint connectionId, uint* windowId, nint original)
    {
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
            result = ((delegate* unmanaged<void*, uint, uint*, int>)original)(netLayer, connectionId, windowId);
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
            _rotation.MarkInUse(slot);
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
    private int HoldBackSlotsBefore(void* netLayer, uint connectionId, Span<bool> heldBack)
    {
        if (HookDisabled)
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

        _rotation.Observe(inUse);

        // The engine hands an existing connection its own window back.
        if (reconnecting)
            return -1;

        var preferred = _rotation.ChooseSlot();
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
        HookDisabled = true;
        const string Fallback = "; new connections take the lowest free slot until the server restarts";
        if (exception == null)
            Log.Write(LogGroup.Error, reason + Fallback, true);
        else
            Log.WriteError(exception, reason + Fallback);
    }
}
