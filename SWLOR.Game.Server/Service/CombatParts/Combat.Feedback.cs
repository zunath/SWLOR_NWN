using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        private static void SendGuardedHitFeedback(uint defender, uint attacker, int preventedDamage)
        {
            if (GetIsPC(defender))
            {
                var feedback = BuildGuardedHitFeedback(defender, defender, attacker, preventedDamage);
                SendMessageToPC(defender, feedback);
                PlayerFeedback.ShowDiagnosticFloatingText(ColorToken.Combat($"Guard (-{preventedDamage})"), defender, false);
            }

            if (GetIsPC(attacker))
            {
                var feedback = BuildGuardedHitFeedback(attacker, defender, attacker, preventedDamage);
                SendMessageToPC(attacker, feedback);
            }
        }

        private static string BuildGuardedHitFeedback(uint observer, uint defender, uint attacker, int preventedDamage)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);
            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName} guards against {attackerName}'s attack, preventing {preventedDamage} damage.");
        }

        public static void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(defender))
                return;

            if (GetIsPC(defender))
            {
                var feedback = BuildIncomingCriticalHitDowngradeCombatLogMessage(defender, attacker, defender);
                SendMessageToPC(defender, feedback);
                PlayerFeedback.ShowDiagnosticFloatingText(ColorToken.Combat("Critical Ward"), defender, false);
            }

            if (GetIsObjectValid(attacker) &&
                attacker != defender &&
                GetIsPC(attacker))
            {
                var feedback = BuildIncomingCriticalHitDowngradeCombatLogMessage(attacker, attacker, defender);
                SendMessageToPC(attacker, feedback);
            }
        }

        private static string BuildIncomingCriticalHitDowngradeCombatLogMessage(uint observer, uint attacker, uint defender)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            if (!GetIsObjectValid(attacker) || attacker == defender)
                return ColorToken.Combat($"{defenderName}'s Critical Ward negates the critical hit.");

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
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

        public static void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            Messaging.SendMessageNearbyToPlayers(
                defender,
                observer => BuildAbilityCriticalHitCombatLogMessage(
                    observer,
                    attacker,
                    defender,
                    abilityName),
                60f);
        }

        public static string BuildAbilityCriticalHitCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "Ability";

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);
            return ColorToken.Combat($"{attackerName}'s {abilityName} critically hits {defenderName}.");
        }

        public static void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage)
        {
            if (damage <= 0 ||
                !GetIsObjectValid(defender) ||
                !HasTemporaryHitPoints(defender))
            {
                return;
            }

            Messaging.SendMessageNearbyToPlayers(
                defender,
                receiver => BuildTemporaryHitPointDamageCombatLogMessage(receiver, attacker, defender, damage),
                60f);
        }

        private static bool HasTemporaryHitPoints(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints)
                    return true;
            }

            return false;
        }

        private static string BuildTemporaryHitPointDamageCombatLogMessage(uint observer, uint attacker, uint defender, int damage)
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

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker.m_idSelf);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender.m_idSelf);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }
    }
}
