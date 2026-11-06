using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// DM-toggled recorder that writes every combat event in one area to a JSON Lines file plus
    /// a per-participant summary. Off by default: while no recording runs, every hook in the
    /// game code is a single <see cref="IsRecording"/> check. While recording, hooks only build
    /// small event structs; serialization and file I/O happen on <see cref="CombatRecordWriter"/>'s
    /// background thread. The logic lives in <see cref="ICombatRecorderService"/>; the event handlers stay here and
    /// every member forwards to the container's instance.
    /// </summary>
    public static class CombatRecorder
    {
        private static ICombatRecorderService Instance => FacadeAccessor.Resolve<ICombatRecorderService>();

        /// <summary>
        /// True while a recording runs. Hooks check this before doing any work.
        /// </summary>
        public static bool IsRecording => Instance.IsRecording;

        /// <summary>
        /// The writer of the current or most recent recording. Its Completion task finishes once
        /// the summary has been written.
        /// </summary>
        public static CombatRecordWriter LastWriter => Instance.LastWriter;

        public static uint RecordedArea => Instance.RecordedArea;

        /// <summary>
        /// Starts recording every creature in an area.
        /// </summary>
        /// <param name="area">The area to record.</param>
        /// <param name="label">Optional label for the file name. Defaults to the area resref.</param>
        /// <param name="message">A message describing the result for the DM.</param>
        public static bool Start(uint area, string label, out string message) => Instance.Start(area, label, out message);

        /// <summary>
        /// Stops the current recording. The writer finishes the files in the background.
        /// </summary>
        public static bool Stop(string reason, out string message) => Instance.Stop(reason, out message);

        public static string GetStatus() => Instance.GetStatus();

        [NWNEventHandler(ScriptName.OnSWLORApplicationShutdown)]
        public static void StopOnShutdown() => Instance.StopOnShutdown();

        internal static void RestoreAttributionDepth(int depth) => Instance.RestoreAttributionDepth(depth);

        /// <summary>
        /// Attributes changes made inside the returned scope to an actor and source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAttribution(
            uint actor,
            CombatRecordSourceKind kind,
            string name = null,
            int code = 0,
            int rank = 0)
            => Instance.BeginAttribution(actor, kind, name, code, rank);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAbilityAttribution(
            uint activator,
            AbilityDetail ability,
            FeatType feat = FeatType.Invalid)
            => Instance.BeginAbilityAttribution(activator, ability, feat);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginStatusEffectAttribution(
            uint source,
            uint creature,
            IStatusEffect statusEffect)
            => Instance.BeginStatusEffectAttribution(source, creature, statusEffect);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginItemAttribution(
            uint user,
            uint item)
            => Instance.BeginItemAttribution(user, item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginStatTriggerAttribution(
            uint creature,
            StatType stat)
            => Instance.BeginStatTriggerAttribution(creature, stat);

        /// <summary>
        /// Starts a rider scope (lifesteal, damage share) that keeps the enclosing ability's name.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginRiderAttribution(
            uint actor,
            CombatRecordSourceKind kind)
            => Instance.BeginRiderAttribution(actor, kind);

        /// <summary>
        /// Attributes a delayed effect to the ability that scheduled it, named by its feat. Unlike
        /// a captured scope this also covers pulses scheduled before the recording started.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAbilityFeatAttribution(
            uint activator,
            FeatType feat)
            => Instance.BeginAbilityFeatAttribution(activator, feat);

        /// <summary>
        /// Attributes a trap going off to whatever placed it (the ability or kit captured with
        /// <see cref="CaptureScope"/>), or to a trap of its owner when it was placed before this
        /// recording started, including during an earlier recording.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginTrapAttribution(
            uint owner,
            in CombatRecordAttribution placedBy)
            => Instance.BeginTrapAttribution(owner, in placedBy);

        /// <summary>
        /// Marks a companion's setup heal (its deploy health correction), which is not combat
        /// healing and is not recorded.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginSetupAttribution(
            uint creature)
            => Instance.BeginSetupAttribution(creature);

        /// <summary>
        /// Starts the reactions to a heal on <paramref name="creature"/> that is applied right after
        /// them (a stamina restore on healing received). Their events are written after that heal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordHealingReactionScope BeginHealingReaction(
            uint creature)
            => Instance.BeginHealingReaction(creature);

        internal static void EndHealingReaction() => Instance.EndHealingReaction();

        /// <summary>
        /// Attributes a respawn's own recovery, so it is not counted as anyone's healing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginRespawnAttribution() => Instance.BeginRespawnAttribution();

        /// <summary>
        /// The innermost open scope, or an inactive value when none is open, so queued work can
        /// restore exactly that scope with <see cref="BeginAttribution(in CombatRecordAttribution)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttribution CaptureScope() => Instance.CaptureScope();

        /// <summary>
        /// Attribution for damage split off a hit and applied later: the attacker's own open scope
        /// (the ability that hit), or a weapon swing when no scope is open.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttribution CaptureAttackAttribution(
            uint attacker)
            => Instance.CaptureAttackAttribution(attacker);

        /// <summary>
        /// Restores a captured attribution. One captured during an earlier recording is ignored, since
        /// its source belongs to that recording.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAttribution(
            in CombatRecordAttribution attribution)
            => Instance.BeginAttribution(in attribution);

        public static void RecordAbilityCast(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability,
            FeatType feat,
            float activationDelay,
            bool isToggleOff = false)
            => Instance.RecordAbilityCast(activator, target, targetLocation, ability, feat, activationDelay, isToggleOff);

        /// <summary>
        /// Records an attempted activation the ability's rules refused, with the refusal shown.
        /// </summary>
        /// <param name="atCompletion">True when the cast was refused as it completed.</param>
        public static void RecordAbilityDenied(
            uint activator,
            uint target,
            AbilityDetail ability,
            FeatType feat,
            string reason,
            bool atCompletion)
            => Instance.RecordAbilityDenied(activator, target, ability, feat, reason, atCompletion);

        /// <summary>
        /// Records a cast or channel stopped before it completed. A damage interruption names the
        /// creature that dealt the damage; an <see cref="CombatRecordInterruptCause.Other"/> stop made
        /// inside another creature's scope is that creature's crowd control.
        /// </summary>
        public static void RecordAbilityInterrupted(
            uint activator,
            AbilityDetail ability,
            CombatRecordInterruptCause cause,
            uint damagedBy = OBJECT_INVALID)
            => Instance.RecordAbilityInterrupted(activator, ability, cause, damagedBy);

        public static void RecordAbilityQueued(
            uint activator,
            uint target,
            AbilityDetail ability,
            FeatType feat)
            => Instance.RecordAbilityQueued(activator, target, ability, feat);

        public static void RecordAbilityImpact(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability,
            FeatType feat,
            AbilityImpactSummary summary)
            => Instance.RecordAbilityImpact(activator, target, targetLocation, ability, feat, summary);

        /// <summary>
        /// Notes that an activation turned a toggled stance off, which is a use of the ability
        /// even though its activation action reports no impact.
        /// </summary>
        public static void NoteToggleDeactivated(uint activator) => Instance.NoteToggleDeactivated(activator);

        /// <summary>
        /// Whether the activation that just ran turned <paramref name="activator"/>'s stance off.
        /// </summary>
        public static bool TakeToggleDeactivation(uint activator) => Instance.TakeToggleDeactivation(activator);

        /// <summary>
        /// Records a later pulse of a cast, such as a damage field's tick or an emitter's pulse.
        /// </summary>
        public static void RecordRecurringAbilityImpact(
            uint activator,
            AbilityDetail ability,
            AbilityImpactSummary summary)
            => Instance.RecordRecurringAbilityImpact(activator, ability, summary);

        /// <summary>
        /// Wraps an ability's activation requirements: measures the FP and STM they take and
        /// attributes resource changes inside the scope (including consumed items) to the
        /// ability's cost. Hit points an ability spends are self-damage during its impact and
        /// are recorded as damage attributed to the cost.
        /// </summary>
        public static CombatRecordAbilityCostScope BeginAbilityCost(
            uint activator,
            AbilityDetail ability,
            FeatType feat = FeatType.Invalid)
            => Instance.BeginAbilityCost(activator, ability, feat);

        internal static void CompleteAbilityCost(in CombatRecordAbilityCostScope cost) => Instance.CompleteAbilityCost(in cost);

        /// <param name="attackResult">The native attack result: 1/7 hit, 2 deflect, 3 critical, 4 miss.</param>
        public static void RecordAttack(
            uint attacker,
            uint defender,
            int attackResult,
            int hitChance,
            string abilityName)
            => Instance.RecordAttack(attacker, defender, attackResult, hitChance, abilityName);

        /// <summary>
        /// Registers damage that a deferred AssignCommand will apply, so the native damage hook
        /// can attribute it to the scope that queued it (a damage-over-time tick, a damage share).
        /// Direct damage normally lands inside its ability's scope or through the weapon hook;
        /// it is registered only when a status effect computes it, because status ticks and
        /// reactions (Burn, Poison and others) dispatch Direct damage through AssignCommand too.
        /// </summary>
        public static void ExpectDeferredDamage(
            uint source,
            uint target,
            int amount,
            CombatDamageDeliveryType deliveryType)
            => Instance.ExpectDeferredDamage(source, target, amount, deliveryType);

        /// <summary>
        /// Whether a registration is waiting for damage from <paramref name="source"/> to
        /// <paramref name="target"/>. Observability seam for the engine tests.
        /// </summary>
        public static bool HasPendingDamage(uint source, uint target) => Instance.HasPendingDamage(source, target);

        /// <summary>
        /// Registers hit points an ability spends by damaging its own caster.
        /// </summary>
        public static void ExpectHitPointCost(uint activator, int amount) => Instance.ExpectHitPointCost(activator, amount);

        /// <summary>
        /// Registers a landed weapon swing's rolled damage, applied when the attack animation lands.
        /// </summary>
        public static void ExpectWeaponDamage(
            uint attacker,
            uint defender,
            int amount,
            SkillType weaponSkill)
            => Instance.ExpectWeaponDamage(attacker, defender, amount, weaponSkill);

        /// <summary>
        /// Called by the native damage hook before the engine applies a damage effect. Lethal
        /// damage runs the death scripts inside the engine call, so the source is known to them
        /// and their death event waits for this damage event. Returns the application's index
        /// for <see cref="RecordDamageApplied"/> and <see cref="EndDamageApplication"/>.
        /// </summary>
        internal static int BeginDamageApplication(
            uint source,
            uint target,
            int amount,
            int hitPointsBefore)
            => Instance.BeginDamageApplication(source, target, amount, hitPointsBefore);

        /// <summary>
        /// Releases a damage application, and writes the held events once the outermost one ends.
        /// </summary>
        internal static void EndDamageApplication(int applicationIndex) => Instance.EndDamageApplication(applicationIndex);

        /// <summary>
        /// Called by the native damage hook after the engine applied a damage effect.
        /// Hit points exclude temporary HP, which is reported separately.
        /// </summary>
        /// <param name="isEngineApplied">True when no script or closure applied the effect, as for weapon hits.</param>
        internal static void RecordDamageApplied(
            int applicationIndex,
            bool isEngineApplied,
            uint source,
            uint target,
            in CombatRecordDamageBreakdown damage,
            int hitPointsBefore,
            int hitPointsAfter,
            int maxHitPoints,
            CombatRecordTemporaryHitPointPool[] poolsBefore,
            CombatRecordTemporaryHitPointPool[] poolsAfter)
            => Instance.RecordDamageApplied(applicationIndex, isEngineApplied, source, target, in damage, hitPointsBefore, hitPointsAfter, maxHitPoints, poolsBefore, poolsAfter);

        /// <summary>
        /// Notes how much the PvP healing reduction took off the heal now being applied to
        /// <paramref name="target"/>, so the heal is recorded with the cut instead of as overheal.
        /// </summary>
        public static void RecordPvpHealingReduced(
            uint target,
            int requested,
            int reduced)
            => Instance.RecordPvpHealingReduced(target, requested, reduced);

        /// <summary>Opens the frame for a heal the native hook is about to apply to <paramref name="target"/>.</summary>
        public static void BeginHealApplication(uint target) => Instance.BeginHealApplication(target);

        /// <summary>Closes the innermost frame for <paramref name="target"/> and returns its PvP reduction.</summary>
        public static int EndHealApplication(uint target) => Instance.EndHealApplication(target);

        /// <summary>
        /// Called by the native heal hook after the engine applied a heal effect.
        /// </summary>
        internal static void RecordHealingApplied(
            uint creator,
            uint target,
            int requested,
            int hitPointsBefore,
            int hitPointsAfter,
            int maxHitPoints,
            int pvpReduced = 0)
            => Instance.RecordHealingApplied(creator, target, requested, hitPointsBefore, hitPointsAfter, maxHitPoints, pvpReduced);

        /// <summary>
        /// Called before a keyed temporary-HP pool is removed or replaced. During a hit on that
        /// creature, what each pool had left is kept, so the hit's absorption is measured against
        /// that remainder instead of treating the removed pool as fully absorbed.
        /// </summary>
        public static void RecordTemporaryHitPointPoolRemoving(
            uint creature)
            => Instance.RecordTemporaryHitPointPoolRemoving(creature);

        /// <summary>
        /// Records a pool cut back to fit the temporary HP cap, credited to whoever granted it.
        /// </summary>
        public static void RecordTemporaryHitPointsTrimmed(
            uint target,
            string effectKey,
            int trimmed,
            int remaining)
            => Instance.RecordTemporaryHitPointsTrimmed(target, effectKey, trimmed, remaining);

        public static void RecordTemporaryHitPointsGranted(
            uint source,
            uint target,
            string effectKey,
            int amount,
            float durationSeconds)
            => Instance.RecordTemporaryHitPointsGranted(source, target, effectKey, amount, durationSeconds);

        /// <summary>
        /// Records an FP or STM change that already happened.
        /// </summary>
        public static void RecordResourceChange(
            uint creature,
            CombatRecordResource resource,
            int delta,
            int valueAfter)
            => Instance.RecordResourceChange(creature, resource, delta, valueAfter);

        /// <summary>
        /// Records an FP or STM reduction that is about to clamp the pool at zero.
        /// </summary>
        public static void RecordResourceReduction(
            uint creature,
            CombatRecordResource resource,
            int reduceBy,
            int currentValue)
            => Instance.RecordResourceReduction(creature, resource, reduceBy, currentValue);

        /// <summary>
        /// Records a stack reduction made by combat activity (an item use, an ability's cost).
        /// Reductions outside any scope, such as quest turn-ins, are not combat events.
        /// </summary>
        public static void RecordItemStackReduced(uint item, int quantity) => Instance.RecordItemStackReduced(item, quantity);

        /// <summary>
        /// Records a charge an activated item spent, attributed to that item.
        /// </summary>
        public static void RecordItemChargeUsed(uint user, uint item) => Instance.RecordItemChargeUsed(user, item);

        public static void RecordItemConsumed(
            uint creature,
            string itemResref,
            int quantity)
            => Instance.RecordItemConsumed(creature, itemResref, quantity);

        public static void RecordStatusApplied(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            bool isPermanent,
            bool isRefresh)
            => Instance.RecordStatusApplied(source, creature, statusEffect, durationSeconds, isPermanent, isRefresh);

        public static void RecordStatusRemoved(
            uint creature,
            IStatusEffect statusEffect,
            bool isReplacement)
            => Instance.RecordStatusRemoved(creature, statusEffect, isReplacement);

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void RecordCreatureDeath() => Instance.RecordCreatureDeath();

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void RecordPlayerDeath() => Instance.RecordPlayerDeath();

        /// <summary>
        /// Records a respawn. Called by the respawn handler after the resurrection and before it
        /// sends the player home, which may be outside the recorded area.
        /// </summary>
        public static void RecordRespawn(uint player) => Instance.RecordRespawn(player);

        /// <summary>
        /// Keeps the HP a lethal hit left when a handler inside the damage call restores the
        /// creature (a fatal-damage save), so the damage event reports the hit, not the recovery.
        /// </summary>
        public static void KeepLethalHitPoints(uint creature) => Instance.KeepLethalHitPoints(creature);

        /// <summary>
        /// Records a resurrection, attributed to the innermost scope (for example the reviving
        /// ability's impact) or to <paramref name="kindWithoutScope"/>.
        /// </summary>
        public static void RecordResurrection(
            uint target,
            CombatRecordSourceKind kindWithoutScope = CombatRecordSourceKind.Unattributed)
            => Instance.RecordResurrection(target, kindWithoutScope);

        /// <summary>
        /// Records a subdual and its resurrection. Call before the subdued player is resurrected:
        /// inside the lethal damage call both events wait for that damage and death, and the HP
        /// the hit left is kept for the damage event.
        /// </summary>
        public static void RecordSubdual(uint subdued, uint subduedBy) => Instance.RecordSubdual(subdued, subduedBy);
    }
}
