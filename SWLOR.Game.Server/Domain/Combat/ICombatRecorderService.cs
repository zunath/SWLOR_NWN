using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// DM-toggled recorder that writes every combat event in one area to a JSON Lines file plus a per-participant
    /// summary. The static <c>CombatRecorder</c> facade forwards here; the hooks in the game code check
    /// <see cref="IsRecording"/> first, so nothing else runs while no recording does.
    /// </summary>
    public interface ICombatRecorderService
    {
        /// <summary>
        /// True while a recording runs. Hooks check this before doing any work.
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// The writer of the current or most recent recording. Its Completion task finishes once
        /// the summary has been written.
        /// </summary>
        CombatRecordWriter LastWriter { get; }

        uint RecordedArea { get; }

        /// <summary>
        /// Starts recording every creature in an area.
        /// </summary>
        /// <param name="area">The area to record.</param>
        /// <param name="label">Optional label for the file name. Defaults to the area resref.</param>
        /// <param name="message">A message describing the result for the DM.</param>
        bool Start(uint area, string label, out string message);

        /// <summary>
        /// Stops the current recording. The writer finishes the files in the background.
        /// </summary>
        bool Stop(string reason, out string message);

        string GetStatus();

        void StopOnShutdown();

        void RestoreAttributionDepth(int depth);

        /// <summary>
        /// Attributes changes made inside the returned scope to an actor and source.
        /// </summary>
        CombatRecordAttributionScope BeginAttribution(
            uint actor,
            CombatRecordSourceKind kind,
            string name = null,
            int code = 0,
            int rank = 0);

        CombatRecordAttributionScope BeginAbilityAttribution(
            uint activator,
            AbilityDetail ability,
            FeatType feat = FeatType.Invalid);

        CombatRecordAttributionScope BeginStatusEffectAttribution(uint source, uint creature, IStatusEffect statusEffect);

        CombatRecordAttributionScope BeginItemAttribution(uint user, uint item);

        CombatRecordAttributionScope BeginStatTriggerAttribution(uint creature, StatType stat);

        /// <summary>
        /// Starts a rider scope (lifesteal, damage share) that keeps the enclosing ability's name.
        /// </summary>
        CombatRecordAttributionScope BeginRiderAttribution(uint actor, CombatRecordSourceKind kind);

        /// <summary>
        /// Attributes a delayed effect to the ability that scheduled it, named by its feat. Unlike
        /// a captured scope this also covers pulses scheduled before the recording started.
        /// </summary>
        CombatRecordAttributionScope BeginAbilityFeatAttribution(uint activator, FeatType feat);

        /// <summary>
        /// Attributes a trap going off to whatever placed it (the ability or kit captured with
        /// <see cref="CaptureScope"/>), or to a trap of its owner when it was placed before this
        /// recording started, including during an earlier recording.
        /// </summary>
        CombatRecordAttributionScope BeginTrapAttribution(uint owner, in CombatRecordAttribution placedBy);

        /// <summary>
        /// Marks a companion's setup heal (its deploy health correction), which is not combat
        /// healing and is not recorded.
        /// </summary>
        CombatRecordAttributionScope BeginSetupAttribution(uint creature);

        /// <summary>
        /// Starts the reactions to a heal on <paramref name="creature"/> that is applied right after
        /// them (a stamina restore on healing received). Their events are written after that heal.
        /// </summary>
        CombatRecordHealingReactionScope BeginHealingReaction(uint creature);

        void EndHealingReaction();

        /// <summary>
        /// Attributes a respawn's own recovery, so it is not counted as anyone's healing.
        /// </summary>
        CombatRecordAttributionScope BeginRespawnAttribution();

        /// <summary>
        /// The innermost open scope, or an inactive value when none is open, so queued work can
        /// restore exactly that scope with <see cref="BeginAttribution(in CombatRecordAttribution)"/>.
        /// </summary>
        CombatRecordAttribution CaptureScope();

        /// <summary>
        /// Attribution for damage split off a hit and applied later: the attacker's own open scope
        /// (the ability that hit), or a weapon swing when no scope is open.
        /// </summary>
        CombatRecordAttribution CaptureAttackAttribution(uint attacker);

        /// <summary>
        /// Restores a captured attribution. One captured during an earlier recording is ignored, since
        /// its source belongs to that recording.
        /// </summary>
        CombatRecordAttributionScope BeginAttribution(in CombatRecordAttribution attribution);

        void RecordAbilityCast(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability,
            FeatType feat,
            float activationDelay,
            bool isToggleOff = false);

        /// <summary>
        /// Records an attempted activation the ability's rules refused, with the refusal shown.
        /// </summary>
        /// <param name="atCompletion">True when the cast was refused as it completed.</param>
        void RecordAbilityDenied(
            uint activator,
            uint target,
            AbilityDetail ability,
            FeatType feat,
            string reason,
            bool atCompletion);

        /// <summary>
        /// Records a cast or channel stopped before it completed. A damage interruption names the
        /// creature that dealt the damage; an <see cref="CombatRecordInterruptCause.Other"/> stop made
        /// inside another creature's scope is that creature's crowd control.
        /// </summary>
        void RecordAbilityInterrupted(
            uint activator,
            AbilityDetail ability,
            CombatRecordInterruptCause cause,
            uint damagedBy = OBJECT_INVALID);

        void RecordAbilityQueued(uint activator, uint target, AbilityDetail ability, FeatType feat);

        void RecordAbilityImpact(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability,
            FeatType feat,
            AbilityImpactSummary summary);

        /// <summary>
        /// Notes that an activation turned a toggled stance off, which is a use of the ability
        /// even though its activation action reports no impact.
        /// </summary>
        void NoteToggleDeactivated(uint activator);

        /// <summary>
        /// Whether the activation that just ran turned <paramref name="activator"/>'s stance off.
        /// </summary>
        bool TakeToggleDeactivation(uint activator);

        /// <summary>
        /// Records a later pulse of a cast, such as a damage field's tick or an emitter's pulse.
        /// </summary>
        void RecordRecurringAbilityImpact(uint activator, AbilityDetail ability, AbilityImpactSummary summary);

        /// <summary>
        /// Wraps an ability's activation requirements: measures the FP and STM they take and
        /// attributes resource changes inside the scope (including consumed items) to the
        /// ability's cost. Hit points an ability spends are self-damage during its impact and
        /// are recorded as damage attributed to the cost.
        /// </summary>
        CombatRecordAbilityCostScope BeginAbilityCost(uint activator, AbilityDetail ability, FeatType feat = FeatType.Invalid);

        void CompleteAbilityCost(in CombatRecordAbilityCostScope cost);

        /// <param name="attackResult">The native attack result: 1/7 hit, 2 deflect, 3 critical, 4 miss.</param>
        void RecordAttack(uint attacker, uint defender, int attackResult, int hitChance, string abilityName);

        /// <summary>
        /// Registers damage that a deferred AssignCommand will apply, so the native damage hook
        /// can attribute it to the scope that queued it (a damage-over-time tick, a damage share).
        /// Direct damage normally lands inside its ability's scope or through the weapon hook;
        /// it is registered only when a status effect computes it, because status ticks and
        /// reactions (Burn, Poison and others) dispatch Direct damage through AssignCommand too.
        /// </summary>
        void ExpectDeferredDamage(uint source, uint target, int amount, CombatDamageDeliveryType deliveryType);

        /// <summary>
        /// Whether a registration is waiting for damage from <paramref name="source"/> to
        /// <paramref name="target"/>. Observability seam for the engine tests.
        /// </summary>
        bool HasPendingDamage(uint source, uint target);

        /// <summary>
        /// Registers hit points an ability spends by damaging its own caster.
        /// </summary>
        void ExpectHitPointCost(uint activator, int amount);

        /// <summary>
        /// Registers a landed weapon swing's rolled damage, applied when the attack animation lands.
        /// </summary>
        void ExpectWeaponDamage(uint attacker, uint defender, int amount, SkillType weaponSkill);

        /// <summary>
        /// Called by the native damage hook before the engine applies a damage effect. Lethal
        /// damage runs the death scripts inside the engine call, so the source is known to them
        /// and their death event waits for this damage event. Returns the application's index
        /// for <see cref="RecordDamageApplied"/> and <see cref="EndDamageApplication"/>.
        /// </summary>
        int BeginDamageApplication(uint source, uint target, int amount, int hitPointsBefore);

        /// <summary>
        /// Releases a damage application, and writes the held events once the outermost one ends.
        /// </summary>
        void EndDamageApplication(int applicationIndex);

        /// <summary>
        /// Called by the native damage hook after the engine applied a damage effect.
        /// Hit points exclude temporary HP, which is reported separately.
        /// </summary>
        /// <param name="isEngineApplied">True when no script or closure applied the effect, as for weapon hits.</param>
        void RecordDamageApplied(
            int applicationIndex,
            bool isEngineApplied,
            uint source,
            uint target,
            in CombatRecordDamageBreakdown damage,
            int hitPointsBefore,
            int hitPointsAfter,
            int maxHitPoints,
            CombatRecordTemporaryHitPointPool[] poolsBefore,
            CombatRecordTemporaryHitPointPool[] poolsAfter);

        /// <summary>
        /// Notes how much the PvP healing reduction took off the heal now being applied to
        /// <paramref name="target"/>, so the heal is recorded with the cut instead of as overheal.
        /// </summary>
        void RecordPvpHealingReduced(uint target, int requested, int reduced);

        /// <summary>Opens the frame for a heal the native hook is about to apply to <paramref name="target"/>.</summary>
        void BeginHealApplication(uint target);

        /// <summary>Closes the innermost frame for <paramref name="target"/> and returns its PvP reduction.</summary>
        int EndHealApplication(uint target);

        /// <summary>
        /// Called by the native heal hook after the engine applied a heal effect.
        /// </summary>
        void RecordHealingApplied(
            uint creator,
            uint target,
            int requested,
            int hitPointsBefore,
            int hitPointsAfter,
            int maxHitPoints,
            int pvpReduced = 0);

        /// <summary>
        /// Called before a keyed temporary-HP pool is removed or replaced. During a hit on that
        /// creature, what each pool had left is kept, so the hit's absorption is measured against
        /// that remainder instead of treating the removed pool as fully absorbed.
        /// </summary>
        void RecordTemporaryHitPointPoolRemoving(uint creature);

        /// <summary>
        /// Records a pool cut back to fit the temporary HP cap, credited to whoever granted it.
        /// </summary>
        void RecordTemporaryHitPointsTrimmed(uint target, string effectKey, int trimmed, int remaining);

        void RecordTemporaryHitPointsGranted(uint source, uint target, string effectKey, int amount, float durationSeconds);

        /// <summary>
        /// Records an FP or STM change that already happened.
        /// </summary>
        void RecordResourceChange(uint creature, CombatRecordResource resource, int delta, int valueAfter);

        /// <summary>
        /// Records an FP or STM reduction that is about to clamp the pool at zero.
        /// </summary>
        void RecordResourceReduction(uint creature, CombatRecordResource resource, int reduceBy, int currentValue);

        /// <summary>
        /// Records a stack reduction made by combat activity (an item use, an ability's cost).
        /// Reductions outside any scope, such as quest turn-ins, are not combat events.
        /// </summary>
        void RecordItemStackReduced(uint item, int quantity);

        /// <summary>
        /// Records a charge an activated item spent, attributed to that item.
        /// </summary>
        void RecordItemChargeUsed(uint user, uint item);

        void RecordItemConsumed(uint creature, string itemResref, int quantity);

        void RecordStatusApplied(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            bool isPermanent,
            bool isRefresh);

        void RecordStatusRemoved(uint creature, IStatusEffect statusEffect, bool isReplacement);

        void RecordCreatureDeath();

        void RecordPlayerDeath();

        /// <summary>
        /// Records a respawn. Called by the respawn handler after the resurrection and before it
        /// sends the player home, which may be outside the recorded area.
        /// </summary>
        void RecordRespawn(uint player);

        /// <summary>
        /// Keeps the HP a lethal hit left when a handler inside the damage call restores the
        /// creature (a fatal-damage save), so the damage event reports the hit, not the recovery.
        /// </summary>
        void KeepLethalHitPoints(uint creature);

        /// <summary>
        /// Records a resurrection, attributed to the innermost scope (for example the reviving
        /// ability's impact) or to <paramref name="kindWithoutScope"/>.
        /// </summary>
        void RecordResurrection(uint target, CombatRecordSourceKind kindWithoutScope = CombatRecordSourceKind.Unattributed);

        /// <summary>
        /// Records a subdual and its resurrection. Call before the subdued player is resurrected:
        /// inside the lethal damage call both events wait for that damage and death, and the HP
        /// the hit left is kept for the damage event.
        /// </summary>
        void RecordSubdual(uint subdued, uint subduedBy);
    }
}
