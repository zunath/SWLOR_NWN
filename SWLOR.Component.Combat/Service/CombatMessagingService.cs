using SWLOR.Component.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Combat.Service
{
    internal class CombatMessagingService : ICombatMessagingService
    {
        private readonly IMessagingService _messaging;

        public CombatMessagingService(
            IMessagingService messaging)
        {
            _messaging = messaging;
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

            var attackerName = GetIsPC(attacker) ? ColorToken.GetNamePCColor(attacker) : ColorToken.GetNameNPCColor(attacker);
            var defenderName = GetIsPC(defender) ? ColorToken.GetNamePCColor(defender) : ColorToken.GetNameNPCColor(defender);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        public void SendParalysisMessage(uint creature)
        {
            var name = GetName(creature);
            _messaging.SendMessageNearbyToPlayers(creature, $"{name} is paralyzed and cannot act!");
        }
    }
}
