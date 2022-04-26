using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Combat
    {
        private static readonly Dictionary<int, int> _dmgValues = new Dictionary<int, int>();
        
        /// <summary>
        /// When the module loads, cache the DMG values found in iprp_dmg.2da into memory.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            var rowCount = UtilPlugin.Get2DARowCount("iprp_dmg");

            for (var row = 0; row < rowCount; row++)
            {
                var label = Get2DAString("iprp_dmg", "Label", row);

                if (int.TryParse(label, out var dmgValue))
                {
                    _dmgValues[row] = dmgValue;
                }
            }
        }

        /// <summary>
        /// Retrieves the DMG value by a defense item property's cost table value.
        /// </summary>
        /// <param name="costTableValue">The cost table value of the defense item property.</param>
        /// <returns>The DMG value</returns>
        public static int GetDMGValueFromItemPropertyCostTableValue(int costTableValue)
        {
            if (!_dmgValues.ContainsKey(costTableValue))
                return 1;

            return _dmgValues[costTableValue];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attackerAttack"></param>
        /// <param name="attackerDMG"></param>
        /// <param name="attackerStat"></param>
        /// <param name="defenderDefense"></param>
        /// <param name="defenderStat"></param>
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
    }
}
