using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Combat
    {
        /// <summary>
        /// Calculates the minimum and maximum damage possible with the provided stats.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <returns>A minimum and maximum damage range</returns>
        public static (int, int) CalculateDamageRange(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical)
        {

            var statDelta = attackerStat - defenderStat;
            var baseDamage = attackerDMG + statDelta;
            var ratio = (float)attackerAttack / (float)defenderDefense;
            var maxDamage = baseDamage * ratio;
            var minDamage = maxDamage * 0.70f;

            Log.Write(LogGroup.Attack, $"attackerAttack = {attackerAttack}, attackerDMG = {attackerDMG}, attackerStat = {attackerStat}, defenderDefense = {defenderDefense}, defenderStat = {defenderStat}, critical = {critical}");
            Log.Write(LogGroup.Attack, $"statDelta = {statDelta}, baseDamage = {baseDamage}, ratio = {ratio}, minDamage = {minDamage}, maxDamage = {maxDamage}");

            // Criticals - 25% bonus to damage range per multiplier point.
            if (critical > 0)
            {
                minDamage = maxDamage;
                switch (critical)
                {
                    case 2:
                        maxDamage *= 1.25f;
                        break;
                    case 3:
                        maxDamage *= 1.50f;
                        break;
                    case 4:
                        maxDamage *= 1.75f;
                        break;
                }
            }

            return ((int)minDamage, (int)maxDamage);
        }

        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <returns>A damage value to apply to the target.</returns>
        public static int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical)
        {
            var (minDamage, maxDamage) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical);

            return (int)Random.NextFloat(minDamage, maxDamage);
        }

        /// <summary>
        /// Return a damage bonus equal to 0.15 of the player's relevant skill.  This helps abilities 
        /// as the player progresses. 
        ///
        /// Global scaling on gear is closer to 0.25 DMG per player skill level so low tier abilities will still
        /// become less useful over time, and get replaced by higher tier ones.  But they will have some utility still.
        /// </summary>
        /// <returns> 0.15 * the player's rank in the specified skill, or 0 for NPCs. </returns>

        public static int GetAbilityDamageBonus(uint player, SkillType skill)
        {
            if (!GetIsPC(player)) return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var pcSkill = dbPlayer.Skills[skill];

            return (int)(0.30f * pcSkill.Rank);
        }

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        [NWNEventHandler("interval_pc_6s")]
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
        /// Sends a combat log message to both the attacker and defender.
        /// </summary>
        /// <param name="attacker">The creature attacking</param>
        /// <param name="defender">The creature defending</param>
        /// <param name="attackStat">The stat used for the attacker</param>
        /// <param name="defendStat">The stat used for the defender</param>
        /// <param name="attackRoll">The base attack roll</param>
        /// <param name="attackMod">The attack roll modifier</param>
        /// <param name="isHit">true if the attack hits, false otherwise</param>
        public static void SendCombatLog(
            uint attacker, 
            uint defender,
            AbilityType attackStat,
            AbilityType defendStat,
            int attackRoll,
            int attackMod,
            bool isHit)
        {
            string GetStatName(AbilityType type)
            {
                switch (type)
                {
                    case AbilityType.Might:
                        return "MGT";
                    case AbilityType.Perception:
                        return "PER";
                    case AbilityType.Vitality:
                        return "VIT";
                    case AbilityType.Willpower:
                        return "WIL";
                    case AbilityType.Social:
                        return "SOC";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            var total = attackRoll + attackMod;
            var operation = attackMod < 0 ? "-" : "+";
            var attackStatName = GetStatName(attackStat);
            var defendStatName = GetStatName(defendStat);
            var attackText = isHit ? "*hit*" : "*miss*";
            var coloredAttackerName = ColorToken.Custom(GetName(attacker), 153, 255, 255);
            var message = ColorToken.Combat($"{coloredAttackerName} attacks {GetName(defender)} {attackText} [{attackStatName} vs {defendStatName}] : ({attackRoll} {operation} {Math.Abs(attackMod)} = {total})");

            SendMessageToPC(attacker, message);
            SendMessageToPC(defender, message);
        }
    }
}
