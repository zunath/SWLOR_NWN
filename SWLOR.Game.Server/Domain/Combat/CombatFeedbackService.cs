using NWN.Native.API;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The combat log lines and floating text of <c>Combat</c>: attack and ability results, guarded hits, Critical Ward and
    /// temporary HP absorbing damage. It keeps no state.
    /// </summary>
    public class CombatFeedbackService : ICombatFeedbackService
    {
        private readonly IPlayerNameService _playerName;
        private readonly IMessagingService _messaging;
        private readonly IPlayerFeedbackService _playerFeedback;

        public CombatFeedbackService(IPlayerNameService playerName, IMessagingService messaging, IPlayerFeedbackService playerFeedback)
        {
            _playerName = playerName;
            _messaging = messaging;
            _playerFeedback = playerFeedback;
        }
        public void SendGuardedHitFeedback(uint defender, uint attacker, int preventedDamage)
        {
            if (GetIsPC(defender))
            {
                var feedback = BuildGuardedHitFeedback(defender, defender, attacker, preventedDamage);
                SendMessageToPC(defender, feedback);
                _playerFeedback.ShowDiagnosticFloatingText(ColorToken.Combat($"Guard (-{preventedDamage})"), defender, false);
            }

            if (GetIsPC(attacker))
            {
                var feedback = BuildGuardedHitFeedback(attacker, defender, attacker, preventedDamage);
                SendMessageToPC(attacker, feedback);
            }
        }

        private string BuildGuardedHitFeedback(uint observer, uint defender, uint attacker, int preventedDamage)
        {
            var defenderName = _playerName.GetColoredDisplayName(observer, defender);
            var attackerName = _playerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName} guards against {attackerName}'s attack, preventing {preventedDamage} damage.");
        }

        public void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(defender))
                return;

            if (GetIsPC(defender))
            {
                var feedback = BuildIncomingCriticalHitDowngradeCombatLogMessage(defender, attacker, defender);
                SendMessageToPC(defender, feedback);
                _playerFeedback.ShowDiagnosticFloatingText(ColorToken.Combat("Critical Ward"), defender, false);
            }

            if (GetIsObjectValid(attacker) &&
                attacker != defender &&
                GetIsPC(attacker))
            {
                var feedback = BuildIncomingCriticalHitDowngradeCombatLogMessage(attacker, attacker, defender);
                SendMessageToPC(attacker, feedback);
            }
        }

        private string BuildIncomingCriticalHitDowngradeCombatLogMessage(uint observer, uint attacker, uint defender)
        {
            var defenderName = _playerName.GetColoredDisplayName(observer, defender);

            if (!GetIsObjectValid(attacker) || attacker == defender)
                return ColorToken.Combat($"{defenderName}'s Critical Ward negates the critical hit.");

            var attackerName = _playerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName}'s Critical Ward negates {attackerName}'s critical hit.");
        }

        /// <summary>
        /// Builds a combat log message based on the provided information.
        /// </summary>
        /// <param name="attacker">The id of the attacker</param>
        /// <param name="defender">The id of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        public string BuildCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            int attackResultType,
            int chanceToHit)
        {
            var type = string.Empty;

            switch (attackResultType)
            {
                case 1:
                case 7:
                    type = ": *hit*";
                    break;
                case 3:
                    type = ": *critical*";
                    break;
                case 4:
                    type = ": *miss*";
                    break;
            }

            var attackerName = _playerName.GetColoredDisplayName(observer, attacker);
            var defenderName = _playerName.GetColoredDisplayName(observer, defender);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        public string BuildAbilityCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            string abilityName,
            int attackResultType,
            int chanceToHit)
        {
            var type = string.Empty;

            switch (attackResultType)
            {
                case 1:
                case 7:
                    type = ": *hit*";
                    break;
                case 3:
                    type = ": *critical*";
                    break;
                case 4:
                    type = ": *miss*";
                    break;
            }

            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "an ability";

            var attackerName = _playerName.GetColoredDisplayName(observer, attacker);
            var defenderName = _playerName.GetColoredDisplayName(observer, defender);

            return ColorToken.Combat($"{attackerName} uses {abilityName} on {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        public string BuildAbilityNoTargetCombatLogMessage(
            uint observer,
            uint attacker,
            string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "an ability";

            var attackerName = _playerName.GetColoredDisplayName(observer, attacker);

            return ColorToken.Combat($"{attackerName} uses {abilityName}, but it hits no targets.");
        }

        public void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            _messaging.SendMessageNearbyToPlayers(
                defender,
                observer => BuildAbilityCriticalHitCombatLogMessage(
                    observer,
                    attacker,
                    defender,
                    abilityName),
                60f);
        }

        public string BuildAbilityCriticalHitCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "Ability";

            var attackerName = _playerName.GetColoredDisplayName(observer, attacker);
            var defenderName = _playerName.GetColoredDisplayName(observer, defender);
            return ColorToken.Combat($"{attackerName}'s {abilityName} critically hits {defenderName}.");
        }

        public void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage)
        {
            if (damage <= 0 ||
                !GetIsObjectValid(defender) ||
                !HasTemporaryHitPoints(defender))
            {
                return;
            }

            _messaging.SendMessageNearbyToPlayers(
                defender,
                receiver => BuildTemporaryHitPointDamageCombatLogMessage(receiver, attacker, defender, damage),
                60f);
        }

        private bool HasTemporaryHitPoints(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints)
                    return true;
            }

            return false;
        }

        private string BuildTemporaryHitPointDamageCombatLogMessage(uint observer, uint attacker, uint defender, int damage)
        {
            var defenderName = _playerName.GetColoredDisplayName(observer, defender);

            if (!GetIsObjectValid(attacker) || attacker == defender)
                return ColorToken.Combat($"{defenderName}'s temporary HP absorbs {damage} damage.");

            var attackerName = _playerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{attackerName} deals {damage} damage against {defenderName}'s temporary HP.");
        }

        /// <summary>
        /// Builds a combat log message based on the provided information, for native contexts.
        /// </summary>
        /// <param name="attacker">The CNWSCreature of the attacker</param>
        /// <param name="defender">The CNWSCreature of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        public string BuildCombatLogMessageNative(
            uint observer,
            CNWSCreature attacker,
            CNWSCreature defender,
            int attackResultType,
            int chanceToHit,
            DeflectionSource deflectionSource = DeflectionSource.None)
        {
            var type = string.Empty;

            switch (attackResultType)
            {
                case 1:
                case 7:
                    type = ": *hit*";
                    break;
                case 3:
                    type = ": *critical*";
                    break;
                case 4:
                    type = ": *miss*";
                    break;
                case 2:
                    type = $": *{GetDeflectionResultName(deflectionSource)}*";
                    break;
            }

            var attackerName = _playerName.GetColoredDisplayName(observer, attacker.m_idSelf);
            var defenderName = _playerName.GetColoredDisplayName(observer, defender.m_idSelf);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }
    }
}
