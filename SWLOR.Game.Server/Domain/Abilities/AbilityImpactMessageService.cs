using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The combat log lines of an ability impact (part 15 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>AbilityService</c> unchanged. The text comes from the combat feedback service behind
    /// <c>protected virtual</c> methods whose bodies are the original calls, so a unit test can answer them.
    /// </summary>
    public class AbilityImpactMessageService : IAbilityImpactMessageService
    {
        private readonly IMessagingService _messaging;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ICombatFeedbackService _feedback;

        public AbilityImpactMessageService(
            IMessagingService messaging,
            ICombatRecorderService combatRecorder,
            ICombatFeedbackService feedback)
        {
            _messaging = messaging;
            _combatRecorder = combatRecorder;
            _feedback = feedback;
        }

        public void SendCombatImpactResultMessage(
            uint activator,
            uint target,
            AbilityDetail ability,
            int attackResultType,
            int hitRate)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || ability == null)
                return;

            if (_combatRecorder.IsRecording)
                _combatRecorder.RecordAttack(activator, target, attackResultType, hitRate, ability.Name);

            _messaging.SendMessageNearbyToPlayers(
                target,
                receiver => BuildAbilityCombatLogMessage(
                    receiver,
                    activator,
                    target,
                    ability.Name,
                    attackResultType,
                    hitRate),
                60f,
                activator);
        }

        public void SendCombatImpactNoTargetsMessage(
            uint activator,
            AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            _messaging.SendMessageNearbyToPlayers(
                activator,
                receiver => BuildAbilityNoTargetCombatLogMessage(
                    receiver,
                    activator,
                    ability.Name),
                60f);
        }

        protected virtual string BuildAbilityCombatLogMessage(uint observer, uint attacker, uint defender, string abilityName, int attackResultType, int chanceToHit) =>
            _feedback.BuildAbilityCombatLogMessage(observer, attacker, defender, abilityName, attackResultType, chanceToHit);

        protected virtual string BuildAbilityNoTargetCombatLogMessage(uint observer, uint attacker, string abilityName) => _feedback.BuildAbilityNoTargetCombatLogMessage(observer, attacker, abilityName);
    }
}
