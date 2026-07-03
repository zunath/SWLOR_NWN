using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class CombatLog
    {
        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public static void ClearCombatState()
        {
            uint player = OBJECT_SELF;

            // Clear combat state.
            if (!GetIsInCombat(player))
            {
                DeleteLocalFloat(player, "ATTACK_ORIENTATION_X");
                DeleteLocalFloat(player, "ATTACK_ORIENTATION_Y");
            }
        }

        /// <summary>
        /// Builds a combat log message based on the provided information.
        /// </summary>
        /// <param name="attacker">The id of the attacker</param>
        /// <param name="defender">The id of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        public static string BuildCombatLogMessage(
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

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        public static string BuildAbilityCombatLogMessage(
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

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            return ColorToken.Combat($"{attackerName} uses {abilityName} on {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        public static string BuildAbilityNoTargetCombatLogMessage(
            uint observer,
            uint attacker,
            string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "an ability";

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);

            return ColorToken.Combat($"{attackerName} uses {abilityName}, but it hits no targets.");
        }

        public static void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage)
        {
            if (damage <= 0 ||
                !GetIsObjectValid(defender) ||
                !CombatLog.HasTemporaryHitPoints(defender))
            {
                return;
            }

            Messaging.SendMessageNearbyToPlayers(
                defender,
                receiver => CombatLog.BuildTemporaryHitPointDamageCombatLogMessage(receiver, attacker, defender, damage),
                60f);
        }

        internal static bool HasTemporaryHitPoints(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints)
                    return true;
            }

            return false;
        }

        internal static string BuildTemporaryHitPointDamageCombatLogMessage(uint observer, uint attacker, uint defender, int damage)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            if (!GetIsObjectValid(attacker) || attacker == defender)
                return ColorToken.Combat($"{defenderName}'s temporary HP absorbs {damage} damage.");

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
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
        public static string BuildCombatLogMessageNative(
            uint observer,
            CNWSCreature attacker,
            CNWSCreature defender,
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
                case 2:
                    type = ": *deflect*";
                    break;
            }

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker.m_idSelf);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender.m_idSelf);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }
    }
}
