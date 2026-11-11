using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Native;
using static SWLOR.Game.Server.Native.WeaponAttackAnimation;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Presents timed melee rolls as consecutive native attack bursts. Only the synchronous client serializer sees the
/// projected fields; damage, charges and round state stay intact. The playbacks and the pure rules live in
/// <see cref="WeaponAttackAnimation"/>; both per-object update hooks end here and run per object per frame.
/// </summary>
public sealed unsafe class WeaponAttackAnimationSink : IWeaponAttackAnimationSink
{
    public uint ComputeUpdate(void* message, void* player, void* obj, void* last, int playerObject, nint original)
    {
        var updates = ((delegate* unmanaged<void*, void*, void*, void*, int, uint>)original)(message, player, obj, last, playerObject);
        var profile = FrameProfiler.EnterManaged();
        try
        {
            var nativeObject = CNWSObject.FromPointer(obj);
            var playerId = CNWSPlayer.FromPointer(player).m_nPlayerID;
            if (NeedsAnimationUpdate(nativeObject, playerId))
                updates |= AnimationUpdate;
            if (_playbacks.TryGetValue(nativeObject.m_idSelf, out var playback) &&
                playback.RemappedObservers.Contains(playerId) && !TryGetPlayback(nativeObject, out _))
                updates |= AnimationReplacementUpdate;
        }
        catch (Exception ex) { Log.WriteError(ex, "Could not refresh a weapon attack animation"); }
        finally { FrameProfiler.ExitManaged(FrameMetric.ObjectUpdateHook, profile); }
        return updates;
    }

    public void WriteUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance, nint original)
    {
        var profile = FrameProfiler.EnterManaged();
        try { WriteObjectUpdate(message, player, obj, last, updates, appearance, original); }
        finally { FrameProfiler.ExitManaged(FrameMetric.ObjectUpdateHook, profile); }
    }

    private static void WriteObjectUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance, nint originalFunction)
    {
        var trampoline = (delegate* unmanaged<void*, void*, void*, void*, uint, uint, void>)originalFunction;
        // Packet serialization is engine work; keep it out of managed time.
        void original(void* message, void* player, void* obj, void* last, uint updates, uint appearance)
        {
            var native = FrameProfiler.Start();
            trampoline(message, player, obj, last, updates, appearance);
            FrameProfiler.ExcludeFromManaged(native);
        }

        // Keep exceptions inside managed code; the native call must run exactly once.
        Playback playback = null;
        var stage = 0;
        var playerId = CNWSPlayer.FromPointer(player).m_nPlayerID;
        var nativeObject = CNWSObject.FromPointer(obj);
        try
        {
            if ((updates & (AnimationUpdate | AnimationReplacementUpdate)) != 0 &&
                TryGetPlayback(nativeObject, out var active))
            {
                playback = active;
                stage = ObserverStage(playback, playerId);
            }
        }
        catch (Exception ex)
        {
            playback = null;
            Log.WriteError(ex, "Could not prepare a weapon attack animation");
        }
        if (playback == null)
        {
            if (_playbacks.TryGetValue(nativeObject.m_idSelf, out var previous) &&
                previous.RemappedObservers.Contains(playerId) && !TryGetPlayback(nativeObject, out _))
                updates |= AnimationReplacementUpdate;
            original(message, player, obj, last, updates, appearance);
            if ((updates & AnimationReplacementUpdate) != 0) previous?.RemappedObservers.Remove(playerId);
            return;
        }
        // A native ready-pose update must not restart or shorten a swing already
        // playing on the client. Only emit an animation when its visual stage changes.
        if (playback.SentStages.TryGetValue(playerId, out var sentStage) && sentStage == stage)
            updates &= ~AnimationUpdate;
        if ((updates & (AnimationUpdate | AnimationReplacementUpdate)) == 0)
        {
            original(message, player, obj, last, updates, appearance);
            return;
        }
        var wrote = false;
        try
        {
            var creature = CNWSObject.FromPointer(obj).AsNWSCreature();
            var roll = stage % 2 == 0 ? playback.Rolls[stage / 2] : null;
            WithSwingVariant(creature, roll == null ? -1 : playback.Variant, () =>
            {
                WithProjectedAttack(creature, roll, playback.SwingLength, () =>
                {
                    wrote = true;
                    // Replacement names are serialized before the attack in this packet.
                    original(message, player, obj, last, updates | AnimationReplacementUpdate, appearance);
                });
            });
            if ((updates & AnimationUpdate) != 0)
            {
                playback.SentAt[playerId] = Clock.TickCount64;
                playback.SentStages[playerId] = stage;
            }
            if (roll == null) playback.RemappedObservers.Remove(playerId);
            else playback.RemappedObservers.Add(playerId);
        }
        catch (Exception ex)
        {
            Log.WriteError(ex, "Could not write a weapon attack animation");
            if (!wrote) original(message, player, obj, last, updates, appearance);
        }
    }
}
