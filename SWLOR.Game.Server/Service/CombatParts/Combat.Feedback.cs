using NWN.Native.API;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service
{
    // The combat log lines live in Domain/Combat/CombatFeedbackService.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
        private static ICombatFeedbackService Feedback => FacadeAccessor.Resolve<ICombatFeedbackService>();
        private static void SendGuardedHitFeedback(uint defender, uint attacker, int preventedDamage) =>
            Feedback.SendGuardedHitFeedback(defender, attacker, preventedDamage);

        public static void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender) =>
            Feedback.SendIncomingCriticalHitDowngradeFeedback(attacker, defender);

        /// <inheritdoc cref="ICombatFeedbackService.BuildCombatLogMessage"/>
        public static string BuildCombatLogMessage(uint observer, uint attacker, uint defender, int attackResultType, int chanceToHit) =>
            Feedback.BuildCombatLogMessage(observer, attacker, defender, attackResultType, chanceToHit);

        public static string BuildAbilityCombatLogMessage(uint observer, uint attacker, uint defender, string abilityName, int attackResultType, int chanceToHit) =>
            Feedback.BuildAbilityCombatLogMessage(observer, attacker, defender, abilityName, attackResultType, chanceToHit);

        public static string BuildAbilityNoTargetCombatLogMessage(uint observer, uint attacker, string abilityName) =>
            Feedback.BuildAbilityNoTargetCombatLogMessage(observer, attacker, abilityName);

        public static void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName) =>
            Feedback.SendAbilityCriticalHitFeedback(attacker, defender, abilityName);

        public static string BuildAbilityCriticalHitCombatLogMessage(uint observer, uint attacker, uint defender, string abilityName) =>
            Feedback.BuildAbilityCriticalHitCombatLogMessage(observer, attacker, defender, abilityName);

        public static void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage) =>
            Feedback.SendTemporaryHitPointDamageFeedback(attacker, defender, damage);

        /// <inheritdoc cref="ICombatFeedbackService.BuildCombatLogMessageNative"/>
        public static string BuildCombatLogMessageNative(uint observer, CNWSCreature attacker, CNWSCreature defender, int attackResultType, int chanceToHit, DeflectionSource deflectionSource = DeflectionSource.None) =>
            Feedback.BuildCombatLogMessageNative(observer, attacker, defender, attackResultType, chanceToHit, deflectionSource);
    }
}
