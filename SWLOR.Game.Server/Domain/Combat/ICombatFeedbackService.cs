using NWN.Native.API;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>The combat log lines and floating text that attacks, abilities, guarded hits and Critical Ward produce.</summary>
    public interface ICombatFeedbackService
    {        void SendGuardedHitFeedback(uint defender, uint attacker, int preventedDamage);

        void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender);

        /// <summary>
        /// Builds a combat log message based on the provided information.
        /// </summary>
        /// <param name="attacker">The id of the attacker</param>
        /// <param name="defender">The id of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        string BuildCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            int attackResultType,
            int chanceToHit);

        string BuildAbilityCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            string abilityName,
            int attackResultType,
            int chanceToHit);

        string BuildAbilityNoTargetCombatLogMessage(
            uint observer,
            uint attacker,
            string abilityName);

        void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName);

        string BuildAbilityCriticalHitCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            string abilityName);

        void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage);

        /// <summary>
        /// Builds a combat log message based on the provided information, for native contexts.
        /// </summary>
        /// <param name="attacker">The CNWSCreature of the attacker</param>
        /// <param name="defender">The CNWSCreature of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        string BuildCombatLogMessageNative(
            uint observer,
            CNWSCreature attacker,
            CNWSCreature defender,
            int attackResultType,
            int chanceToHit,
            DeflectionSource deflectionSource = DeflectionSource.None);
    }
}
