using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native;

/// <summary>
/// Presents timed melee rolls as consecutive native attack bursts. Only the synchronous
/// client serializer sees the projected fields; damage, charges and round state stay intact.
/// </summary>
public static unsafe class WeaponAttackAnimation
{
    private const int Attack = 9;
    private const int Ready = 1;
    private const uint AnimationUpdate = 4;
    private static FunctionHook* _computeHook;
    private static FunctionHook* _writeHook;
    private static readonly Dictionary<uint, Playback> _playbacks = new();

    private sealed class Playback
    {
        public long Started;
        public int Duration;
        public byte EndAttack;
        public byte Group;
        public uint Target;
        public Roll[] Rolls;
        // A client may not receive every server tick. Mark a frame only after writing it.
        public readonly Dictionary<uint, int> SentFrames = new();
    }

    public sealed record Roll(byte Group, ushort Length, uint Target, ushort ReactionLength,
        ushort Reaction, byte Result, ushort Type, int Ranged, int KillingBlow, byte Weapon, short[] Damage)
    {
        public static Roll Read(CNWSCombatAttackData data) => new(data.m_nAttackGroup,
            data.m_nAnimationLength, data.m_oidReactObject, data.m_nReactionAnimationLength,
            data.m_nReactionAnimation, data.m_nAttackResult, data.m_nAttackType,
            data.m_bRangedAttack, data.m_bKillingBlow, data.m_nWeaponAttackType,
            Enumerable.Range(0, 32).Select(i => data.m_nDamage[i]).ToArray());

        public void Write(CNWSCombatAttackData data)
        {
            data.m_nAttackGroup = Group;
            data.m_nAnimationLength = Length;
            data.m_oidReactObject = Target;
            data.m_nReactionAnimationLength = ReactionLength;
            data.m_nReactionAnimation = Reaction;
            data.m_nAttackResult = Result;
            data.m_nAttackType = Type;
            data.m_bRangedAttack = Ranged;
            data.m_bKillingBlow = KillingBlow;
            data.m_nWeaponAttackType = Weapon;
            for (var i = 0; i < Damage.Length; i++) data.m_nDamage[i] = Damage[i];
        }
    }

    // Leave a short ready pose before the next cycle. The gameplay delay is not changed.
    public static int PlaybackDuration(int nativeDuration) =>
        Math.Clamp(nativeDuration, 1, Combat.BaseAttackDelayMilliseconds - 100);

    public static int FrameAt(long elapsed, int duration, int count) =>
        (int)Math.Clamp(elapsed * count / duration, 0, count);

    public static int RemainingFrameDuration(long elapsed, int duration, int count)
    {
        var frame = FrameAt(elapsed, duration, count);
        if (frame == count) return 0;
        // Round the boundary up to match FrameAt. Late network ticks use the remaining
        // time rather than extending this animation into the following hand's slot.
        return (int)((frame + 1L) * duration + count - 1) / count - (int)Math.Max(0, elapsed);
    }

    public static Roll[] OrderForPlayback(IEnumerable<Roll> rolls)
    {
        var pending = rolls.ToList();
        var ordered = new List<Roll>(pending.Count);
        // Native resolution groups main-hand rolls before off-hand rolls. Presentation
        // alternates them, including an odd extra main-hand roll from a limited charge.
        while (pending.Count > 0)
        {
            foreach (var hand in new byte[] { 1, 2 })
            {
                var index = pending.FindIndex(roll => roll.Weapon == hand);
                if (index < 0) continue;
                ordered.Add(pending[index]);
                pending.RemoveAt(index);
            }
            if (pending.All(roll => roll.Weapon != 1 && roll.Weapon != 2))
            {
                ordered.AddRange(pending);
                break;
            }
        }
        return ordered.ToArray();
    }

    public static void Capture(CNWSCreature creature, int firstAttack, int nativeDuration)
    {
        var round = creature.m_pcCombatRound;
        var count = round.m_nCurrentAttack - firstAttack;
        if (creature.GetRangeWeaponEquipped() != 0 || count <= 0 || count > Combat.MaxAttacksPerSwing * 2)
        {
            _playbacks.Remove(creature.m_idSelf);
            return;
        }
        var rolls = Enumerable.Range(firstAttack, count).Select(i => Roll.Read(round.GetAttack(i))).ToArray();
        _playbacks[creature.m_idSelf] = new Playback
        {
            Started = Environment.TickCount64,
            Duration = PlaybackDuration(nativeDuration),
            EndAttack = round.m_nCurrentAttack,
            Group = rolls[^1].Group,
            Target = rolls[^1].Target,
            Rolls = OrderForPlayback(rolls)
        };
    }

    private static bool TryGetPlayback(CNWSObject obj, out Playback playback, out int frame, out int remaining)
    {
        frame = 0;
        remaining = 0;
        if (!_playbacks.TryGetValue(obj.m_idSelf, out playback) || obj.m_nAnimation != Attack)
            return false;
        var creature = obj.AsNWSCreature();
        var round = creature?.m_pcCombatRound;
        if (round == null || creature.m_oidAttackTarget != playback.Target ||
            round.m_nCurrentAttack != playback.EndAttack ||
            round.GetAttack(playback.EndAttack - 1).m_nAttackGroup != playback.Group)
            return false;
        var elapsed = Environment.TickCount64 - playback.Started;
        frame = FrameAt(elapsed, playback.Duration, playback.Rolls.Length);
        remaining = RemainingFrameDuration(elapsed, playback.Duration, playback.Rolls.Length);
        return true;
    }

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHooks()
    {
        if (_computeHook != null) return;
        var library = NativeLibrary.GetMainProgramHandle();
        delegate* unmanaged<void*, void*, void*, void*, int, uint> compute = &ComputeUpdate;
        delegate* unmanaged<void*, void*, void*, void*, uint, uint, void> write = &WriteUpdate;
        _computeHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN11CNWSMessage21ComputeUpdateRequiredEP10CNWSPlayerP10CNWSObjectP17CLastUpdateObjecti"),
            (IntPtr)compute, HookOrder.Late);
        _writeHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN11CNWSMessage31WriteGameObjUpdate_UpdateObjectEP10CNWSPlayerP10CNWSObjectP17CLastUpdateObjectjj"),
            (IntPtr)write, HookOrder.Late);
    }

    [UnmanagedCallersOnly]
    private static uint ComputeUpdate(void* message, void* player, void* obj, void* last, int playerObject)
    {
        var original = (delegate* unmanaged<void*, void*, void*, void*, int, uint>)_computeHook->m_trampoline;
        var updates = original(message, player, obj, last, playerObject);
        try
        {
            if (NeedsAnimationUpdate(CNWSObject.FromPointer(obj), CNWSPlayer.FromPointer(player).m_nPlayerID))
                updates |= AnimationUpdate;
        }
        catch (Exception ex) { Log.WriteError(ex, "Could not refresh a weapon attack animation"); }
        return updates;
    }

    public static bool NeedsAnimationUpdate(CNWSObject obj, uint playerId) =>
        TryGetPlayback(obj, out var playback, out var frame, out _) &&
        (!playback.SentFrames.TryGetValue(playerId, out var sent) || sent != frame);

    [UnmanagedCallersOnly]
    private static void WriteUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance)
    {
        var original = (delegate* unmanaged<void*, void*, void*, void*, uint, uint, void>)_writeHook->m_trampoline;
        // Keep exceptions inside managed code; the native call must run exactly once.
        Playback playback = null;
        var frame = 0;
        var duration = 0;
        try
        {
            if ((updates & AnimationUpdate) != 0 &&
                !TryGetPlayback(CNWSObject.FromPointer(obj), out playback, out frame, out duration))
                playback = null;
        }
        catch (Exception ex)
        {
            playback = null;
            Log.WriteError(ex, "Could not prepare a weapon attack animation");
        }
        if (playback == null)
        {
            original(message, player, obj, last, updates, appearance);
            return;
        }
        var wrote = false;
        try
        {
            var creature = CNWSObject.FromPointer(obj).AsNWSCreature();
            var roll = frame < playback.Rolls.Length ? playback.Rolls[frame] : null;
            WithProjectedAttack(creature, roll, duration, () =>
            {
                wrote = true;
                original(message, player, obj, last, updates, appearance);
            });
            playback.SentFrames[CNWSPlayer.FromPointer(player).m_nPlayerID] = frame;
        }
        catch (Exception ex)
        {
            Log.WriteError(ex, "Could not write a weapon attack animation");
            if (!wrote) original(message, player, obj, last, updates, appearance);
        }
    }

    /// <summary>Projects one packet, restoring every touched field even if serialization fails.</summary>
    public static void WithProjectedAttack(CNWSCreature creature, Roll roll, int duration, Action serialize)
    {
        var animation = creature.m_nAnimation;
        var round = creature.m_pcCombatRound;
        var lastIndex = round.m_nCurrentAttack - 1;
        if (roll == null)
        {
            try { creature.m_nAnimation = Ready; serialize(); }
            finally { creature.m_nAnimation = animation; }
            return;
        }
        var lastAttack = round.GetAttack(lastIndex);
        var saved = Roll.Read(lastAttack);
        var firstIndex = Math.Max(0, lastIndex - 2);
        var groups = Enumerable.Range(firstIndex, lastIndex - firstIndex)
            .Select(i => round.GetAttack(i).m_nAttackGroup).ToArray();
        try
        {
            // NWN transmits only the final group among its last three attack slots.
            // Isolate this visual roll without clearing or reordering any real attack data.
            for (var i = firstIndex; i < lastIndex; i++) round.GetAttack(i).m_nAttackGroup = 255;
            (roll with { Group = 0, Length = (ushort)duration, ReactionLength = (ushort)duration }).Write(lastAttack);
            creature.m_nAnimation = Attack;
            serialize();
        }
        finally
        {
            saved.Write(lastAttack);
            for (var i = firstIndex; i < lastIndex; i++) round.GetAttack(i).m_nAttackGroup = groups[i - firstIndex];
            creature.m_nAnimation = animation;
        }
    }

    [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
    public static void ClearInactive()
    {
        foreach (var pair in _playbacks.ToArray())
            if (Environment.TickCount64 - pair.Value.Started > 30000 || !GetIsObjectValid(pair.Key))
                _playbacks.Remove(pair.Key);
    }
}
