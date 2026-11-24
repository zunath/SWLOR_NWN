using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability records part of the combat recorder: casts, refusals, interruptions, queued abilities, impacts, the cost of an
    /// activation and the note that a toggled stance was turned off. It writes through the session core and reads the attribution
    /// stack and the feat codes of <see cref="ICombatRecorderService"/>.
    /// </summary>
    public class CombatRecordAbilityService : ICombatRecordAbilityService
    {
        private uint _toggleDeactivatedBy = OBJECT_INVALID;
        private ulong _toggleDeactivatedFrame;

        private readonly ICombatRecordSession _session;
        private readonly ICombatRecorderService _recorder;
        private readonly IStatService _stat;

        public CombatRecordAbilityService(ICombatRecordSession session, ICombatRecorderService recorder, IStatService stat)
        {
            _session = session;
            _recorder = recorder;
            _stat = stat;
        }

        public void RecordAbilityCast(
            uint activator, uint target, Location targetLocation, AbilityDetail ability, FeatType feat, float activationDelay,
            bool isToggleOff = false)
        {
            if (ability == null || !_session.IsRecorded(activator, target))
                return;

            var recordEvent = CombatRecordEvent.AbilityCast(
                _session.GetParticipantId(activator),
                _session.GetParticipantId(target),
                ability.Name,
                (int)feat,
                ability.AbilityLevel,
                (int)ability.ActivationType,
                (int)Math.Round(activationDelay * 1000f),
                isToggleOff);
            _session.Enqueue(_session.WithTargetLocation(recordEvent, target, targetLocation));
        }

        /// <summary>
        /// Records an attempted activation the ability's rules refused, with the refusal shown.
        /// </summary>
        /// <param name="atCompletion">True when the cast was refused as it completed.</param>
        public void RecordAbilityDenied(
            uint activator, uint target, AbilityDetail ability, FeatType feat, string reason, bool atCompletion)
        {
            if (ability == null || !_session.IsRecorded(activator, target))
                return;

            _session.Enqueue(CombatRecordEvent.AbilityDenied(
                _session.GetParticipantId(activator),
                _session.GetParticipantId(target),
                ability.Name,
                (int)feat,
                ability.AbilityLevel,
                reason,
                atCompletion));
        }

        /// <summary>
        /// Records a cast or channel stopped before it completed. A damage interruption names the
        /// creature that dealt the damage; an <see cref="CombatRecordInterruptCause.Other"/> stop made
        /// inside another creature's scope is that creature's crowd control.
        /// </summary>
        public void RecordAbilityInterrupted(
            uint activator, AbilityDetail ability, CombatRecordInterruptCause cause, uint damagedBy = OBJECT_INVALID)
        {
            if (ability == null || !_session.IsInRecordedArea(activator))
                return;

            var interrupter = default(CombatRecordAttribution);
            if (cause == CombatRecordInterruptCause.Damage)
            {
                if (GetIsObjectValid(damagedBy) && damagedBy != activator)
                    interrupter = _recorder.ResolveOwnAttribution(damagedBy);
            }
            else if (cause == CombatRecordInterruptCause.Other &&
                     _recorder.TryGetInnermostAttribution(out var innermost) &&
                     innermost.Actor != activator)
            {
                interrupter = innermost;
                cause = CombatRecordInterruptCause.CrowdControl;
            }

            _session.Enqueue(CombatRecordEvent.AbilityInterrupted(
                _session.GetParticipantId(activator),
                ability.Name,
                _recorder.GetFeatCode(ability, FeatType.Invalid),
                ability.AbilityLevel,
                cause,
                _session.GetParticipantId(interrupter.Actor),
                interrupter.Kind,
                interrupter.Name,
                interrupter.Code));
        }

        public void RecordAbilityQueued(uint activator, uint target, AbilityDetail ability, FeatType feat)
        {
            if (ability == null || !_session.IsRecorded(activator, target))
                return;

            _session.Enqueue(CombatRecordEvent.AbilityQueued(
                _session.GetParticipantId(activator),
                _session.GetParticipantId(target),
                ability.Name,
                (int)feat,
                ability.AbilityLevel));
        }

        public void RecordAbilityImpact(
            uint activator, uint target, Location targetLocation, AbilityDetail ability, FeatType feat, AbilityImpactSummary summary)
        {
            if (ability == null || (!_session.IsRecorded(activator, target) && !_session.AnyInRecordedArea(summary?.ImpactedTargets)))
                return;

            var recordEvent = CombatRecordEvent.AbilityImpact(
                _session.GetParticipantId(activator),
                _session.GetParticipantId(target),
                ability.Name,
                (int)feat,
                ability.AbilityLevel,
                summary?.ImpactedTargetCount ?? 0,
                summary?.CriticalHitCount ?? 0,
                summary?.AttributedDamage ?? 0);
            _session.Enqueue(_session.WithTargetLocation(recordEvent, target, targetLocation));
        }

        /// <summary>
        /// Notes that an activation turned a toggled stance off, which is a use of the ability
        /// even though its activation action reports no impact.
        /// </summary>
        public void NoteToggleDeactivated(uint activator)
        {
            _toggleDeactivatedBy = activator;
            _toggleDeactivatedFrame = _session.CurrentFrame();
        }

        /// <summary>
        /// Whether the activation that just ran turned <paramref name="activator"/>'s stance off.
        /// </summary>
        public bool TakeToggleDeactivation(uint activator)
        {
            var deactivated = _toggleDeactivatedBy == activator && _toggleDeactivatedFrame == _session.CurrentFrame();
            _toggleDeactivatedBy = OBJECT_INVALID;
            return deactivated;
        }

        /// <summary>
        /// Wraps an ability's activation requirements: measures the FP and STM they take and
        /// attributes resource changes inside the scope (including consumed items) to the
        /// ability's cost. Hit points an ability spends are self-damage during its impact and
        /// are recorded as damage attributed to the cost.
        /// </summary>
        public CombatRecordAbilityCostScope BeginAbilityCost(uint activator, AbilityDetail ability, FeatType feat = FeatType.Invalid)
        {
            if (!_session.IsRecording || ability == null || !_session.IsInRecordedArea(activator))
                return default;

            var attribution = _recorder.BeginAttribution(activator, CombatRecordSourceKind.AbilityCost, ability.Name, (int)feat, ability.AbilityLevel);
            return new CombatRecordAbilityCostScope(
                activator,
                ability,
                (int)feat,
                GetCurrentFP(activator),
                GetCurrentStamina(activator),
                attribution);
        }

        public void CompleteAbilityCost(in CombatRecordAbilityCostScope cost)
        {
            cost.Attribution.Dispose();
            if (!_session.IsRecording || !GetIsObjectValid(cost.Activator))
                return;

            _session.Enqueue(CombatRecordEvent.AbilityCost(
                _session.GetParticipantId(cost.Activator),
                cost.Ability.Name,
                cost.Feat,
                cost.Ability.AbilityLevel,
                cost.FPBefore - GetCurrentFP(cost.Activator),
                cost.StaminaBefore - GetCurrentStamina(cost.Activator)));
        }

        #region Calls into code that is not converted yet

        // Each wrapper's body is the call the recorder made; a test answers it by overriding the method.

        protected virtual int GetCurrentFP(uint creature, Player dbPlayer = null) => _stat.GetCurrentFP(creature, dbPlayer);

        protected virtual int GetCurrentStamina(uint creature, Player dbPlayer = null) => _stat.GetCurrentStamina(creature, dbPlayer);

        #endregion
    }
}
