using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// DM-toggled recorder that writes every combat event in one area to a JSON Lines file plus a per-participant
    /// summary. Static callers reach the container's instance through <c>GameServices.CombatRecorder</c>; the hooks in the game code check
    /// <see cref="IsRecording"/> first, so nothing else runs while no recording does.
    /// </summary>
    public interface ICombatRecorderService : ICharacterRecording, IShutdownStep
    {
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

        CombatRecordAttributionScope BeginAbilityAttribution(
            uint activator,
            AbilityDetail ability,
            FeatType feat = FeatType.Invalid);

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

        /// <summary>
        /// The innermost open scope as it is, or false when none is open. The ability records read it to name the creature whose
        /// scope stopped a cast.
        /// </summary>
        bool TryGetInnermostAttribution(out CombatRecordAttribution attribution);

        /// <summary>
        /// Resolves who caused a change: the innermost attribution scope, then the actor's active ability impact, then the
        /// actor alone. The damage records read it to name who healed or granted temporary hit points.
        /// </summary>
        CombatRecordAttribution ResolveAttribution(uint fallbackActor);

        /// <summary>
        /// The actor's active ability impact as an attribution, or the actor alone when it has none.
        /// </summary>
        CombatRecordAttribution ResolveActiveAbilityAttribution(uint actor);

        /// <summary>
        /// Whether an open scope belongs to <paramref name="actor"/>, as its actor or its owner.
        /// </summary>
        bool IsOwnedBy(in CombatRecordAttribution scope, uint actor);

        /// <summary>
        /// Registers who is told when a recording starts and stops. Registering the same listener twice keeps one entry.
        /// </summary>
        void RegisterSessionListener(ICombatRecordingSessionListener listener);

        /// <summary>
        /// The innermost scope when it belongs to <paramref name="actor"/>; otherwise the actor's active ability impact, then the
        /// actor alone. Used where another creature's scope must not lend its source to the actor.
        /// </summary>
        CombatRecordAttribution ResolveOwnAttribution(uint actor);

        /// <summary>
        /// The activated feat when known, else the feat the ability is registered under.
        /// </summary>
        int GetFeatCode(AbilityDetail ability, FeatType feat);

        /// <summary>
        /// Records a later pulse of a cast, such as a damage field's tick or an emitter's pulse.
        /// </summary>
        void RecordRecurringAbilityImpact(uint activator, AbilityDetail ability, AbilityImpactSummary summary);

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
        /// Notes how much the PvP healing reduction took off the heal now being applied to
        /// <paramref name="target"/>, so the heal is recorded with the cut instead of as overheal.
        /// </summary>
        void RecordPvpHealingReduced(uint target, int requested, int reduced);

        void RecordItemConsumed(uint creature, string itemResref, int quantity);
    }
}
