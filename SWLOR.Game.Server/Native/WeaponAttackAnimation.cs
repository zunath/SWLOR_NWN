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
    private const int AttackAction = 12;
    private const uint AnimationUpdate = 4;
    private const uint AnimationReplacementUpdate = 0x1000000;
    // The installed a_ba sword clips (slashl, slashr, stab and slasho) are one
    // second long. The 1,750 ms gameplay cycle floor is not a clip duration.
    public const int SwingDuration = 1000;
    public const int TransitionDuration = 100;
    private static FunctionHook* _computeHook;
    private static FunctionHook* _writeHook;
    private static readonly Dictionary<uint, Playback> _playbacks = new();

    private sealed class Playback
    {
        public long Started;
        public int Duration;
        public int SwingLength;
        public byte EndAttack;
        public byte Group;
        public uint Target;
        public Roll[] Rolls;
        public int Variant;
        public bool Cancelled;
        // Even stages are swings; odd stages are ready transitions. A client must
        // receive the transition before the next swing, even if updates arrive late.
        public readonly Dictionary<uint, int> SentStages = new();
        public readonly Dictionary<uint, long> SentAt = new();
        public readonly HashSet<uint> RemappedObservers = new();
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

    public static int NextVariant(int previous, int randomChoice) =>
        previous < 0 ? randomChoice % 3 : (previous + 1 + randomChoice % 2) % 3;

    // Always show both hands. Extra haste rolls share their hand's presentation;
    // they do not remove the off hand or add a backlog of accelerated animations.
    public static Roll[] SelectVisualRolls(IEnumerable<Roll> rolls) =>
        OrderForPlayback(rolls).DistinctBy(roll => roll.Weapon).ToArray();

    public static int CalculateSwingDuration(int cycleDuration, int handCount) =>
        Math.Min(SwingDuration,
            Math.Max(Combat.BaseAttackDelayMilliseconds, cycleDuration) / Math.Max(1, handCount) - TransitionDuration);

    public static int AdvanceStage(int sent, long elapsed, int swingDuration, int handCount) =>
        elapsed >= (sent % 2 == 0 ? swingDuration : TransitionDuration)
            ? Math.Min(sent + 1, handCount * 2 - 1) : sent;

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

    public static void Capture(CNWSCreature creature, int firstAttack, int cycleDuration, bool pendingOffHand = false)
    {
        var round = creature.m_pcCombatRound;
        var count = round.m_nCurrentAttack - firstAttack;
        if (creature.GetRangeWeaponEquipped() != 0 || count <= 0 || count > Combat.MaxAttacksPerSwing * 2)
        {
            if (_playbacks.TryGetValue(creature.m_idSelf, out var inactive)) inactive.Cancelled = true;
            return;
        }
        var rolls = Enumerable.Range(firstAttack, count).Select(i => Roll.Read(round.GetAttack(i))).ToArray();
        _playbacks.TryGetValue(creature.m_idSelf, out var previous);
        var visualRolls = SelectVisualRolls(rolls);
        if (pendingOffHand) visualRolls = new[] { visualRolls[0], null };
        var swingLength = CalculateSwingDuration(cycleDuration, visualRolls.Length);
        var playback = new Playback
        {
            Started = Environment.TickCount64,
            Duration = visualRolls.Length * (swingLength + TransitionDuration),
            SwingLength = swingLength,
            EndAttack = round.m_nCurrentAttack,
            Group = rolls[^1].Group,
            Target = rolls[^1].Target,
            Rolls = visualRolls,
            Variant = NextVariant(previous?.Variant ?? -1, System.Random.Shared.Next(previous == null ? 3 : 2))
        };
        if (previous != null) playback.RemappedObservers.UnionWith(previous.RemappedObservers);
        _playbacks[creature.m_idSelf] = playback;
    }

    public static void Cancel(uint creature)
    {
        if (_playbacks.TryGetValue(creature, out var playback)) playback.Cancelled = true;
    }

    public static void CompleteOffHand(CNWSCreature creature, int firstAttack)
    {
        if (!_playbacks.TryGetValue(creature.m_idSelf, out var playback) || playback.Cancelled) return;
        var round = creature.m_pcCombatRound;
        if (round.m_nCurrentAttack <= firstAttack) { playback.Cancelled = true; return; }
        playback.Rolls[1] = Roll.Read(round.GetAttack(firstAttack));
        playback.EndAttack = round.m_nCurrentAttack;
        playback.Group = round.GetAttack(round.m_nCurrentAttack - 1).m_nAttackGroup;
    }

    public static bool IsPlaying(CNWSObject obj) =>
        TryGetPlayback(obj, out var playback) &&
        (Environment.TickCount64 - playback.Started < playback.Duration ||
         playback.SentStages.Any(sent => sent.Value < playback.Rolls.Length * 2 - 1));

    private static bool TryGetPlayback(CNWSObject obj, out Playback playback)
    {
        if (!_playbacks.TryGetValue(obj.m_idSelf, out playback) || playback.Cancelled)
            return false;
        // The engine returns to ready when its damage phase ends, before our second
        // visual hand. Permit that pose only while the original attack action remains
        // active. Movement, casting, cancellation and target changes still interrupt.
        if (obj.m_nAnimation != Attack &&
            !(obj.m_nAnimation is Ready or 0 && HasQueuedAttack(obj, playback.Target))) return false;
        var creature = obj.AsNWSCreature();
        var round = creature?.m_pcCombatRound;
        if (round == null || creature.m_oidAttackTarget != playback.Target ||
            round.m_nCurrentAttack != playback.EndAttack ||
            round.GetAttack(playback.EndAttack - 1).m_nAttackGroup != playback.Group)
            return false;
        return true;
    }

    public static bool HasQueuedAttack(CNWSObject obj, uint target)
    {
        // m_nCurrentAction is 0xffff between AI executions, including during an
        // ongoing attack. The queue head retains the action while networking runs.
        if (obj.m_lQueuedActions.IsEmpty() != 0) return false;
        var action = obj.m_lQueuedActions.GetHead();
        return action.m_nActionId == AttackAction && (uint)action.m_pParameter[0] == target;
    }

    private static int ObserverStage(Playback playback, uint playerId)
    {
        var now = Environment.TickCount64;
        if (!playback.SentStages.TryGetValue(playerId, out var sent))
            return now - playback.Started < playback.Duration ? 0 : playback.Rolls.Length * 2 - 1;
        var next = AdvanceStage(sent, now - playback.SentAt[playerId], playback.SwingLength, playback.Rolls.Length);
        // A second swing is only publishable once that hand has actually resolved.
        return next == 2 && playback.Rolls[1] == null ? sent : next;
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
            var nativeObject = CNWSObject.FromPointer(obj);
            var playerId = CNWSPlayer.FromPointer(player).m_nPlayerID;
            if (NeedsAnimationUpdate(nativeObject, playerId))
                updates |= AnimationUpdate;
            if (_playbacks.TryGetValue(nativeObject.m_idSelf, out var playback) &&
                playback.RemappedObservers.Contains(playerId) && !TryGetPlayback(nativeObject, out _))
                updates |= AnimationReplacementUpdate;
        }
        catch (Exception ex) { Log.WriteError(ex, "Could not refresh a weapon attack animation"); }
        return updates;
    }

    public static bool NeedsAnimationUpdate(CNWSObject obj, uint playerId) =>
        TryGetPlayback(obj, out var playback) &&
        (!playback.SentStages.TryGetValue(playerId, out var sent) || sent != ObserverStage(playback, playerId));

    [UnmanagedCallersOnly]
    private static void WriteUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance)
    {
        var original = (delegate* unmanaged<void*, void*, void*, void*, uint, uint, void>)_writeHook->m_trampoline;
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
                playback.SentAt[playerId] = Environment.TickCount64;
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

    public static Dictionary<string, string> ReadAnimationReplacements(CNWSObject obj) =>
        obj.m_lAnimationReplaceInfo.ToDictionary(
            entry => ReadName(entry.m_sOldName), entry => ReadName(entry.m_sNewName));

    private static string ReadName(NativeArray<byte> bytes) =>
        System.Text.Encoding.ASCII.GetString(Enumerable.Range(0, 17)
            .Select(i => bytes[i]).TakeWhile(value => value != 0).ToArray());

    public static string VariantReplacement(string source, string existing, int variant)
    {
        var clip = string.IsNullOrEmpty(existing) ? source : existing;
        var families = new[] { "1h", "2h", "2w", "pl", "nw" };
        var suffixes = new[] { "slashl", "slashr", "stab", "closeh", "closel", "reach" };
        // Keep custom ability clips and the distinct off-hand slash. Equipment mappings
        // such as katar -> unarmed retain their destination animation family.
        if (!families.Any(prefix => clip.StartsWith(prefix, StringComparison.Ordinal)) ||
            !suffixes.Contains(clip[2..])) return existing;
        return clip[..2] + new[] { "slashl", "slashr", "stab" }[variant];
    }

    public static void WithSwingVariant(CNWSObject obj, int variant, Action serialize)
    {
        if (variant < 0) { serialize(); return; }
        var saved = ReadAnimationReplacements(obj);
        var changes = new Dictionary<string, string>();
        foreach (var prefix in new[] { "1h", "2h", "2w", "pl", "nw" })
        foreach (var suffix in new[] { "slashl", "slashr", "stab", "closeh", "closel", "reach" })
        {
            var source = prefix + suffix;
            saved.TryGetValue(source, out var existing);
            var replacement = VariantReplacement(source, existing, variant);
            if (replacement != existing) changes[source] = replacement;
        }
        void Replace(string source, string replacement)
        {
            using var oldName = new CExoString(source);
            using var newName = new CExoString(replacement ?? "");
            obj.SetAnimationReplace(oldName, newName);
        }
        try
        {
            foreach (var change in changes) Replace(change.Key, change.Value);
            serialize();
        }
        finally
        {
            foreach (var change in changes) Replace(change.Key, saved.GetValueOrDefault(change.Key));
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
