using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Combat
    {
        private static readonly Dictionary<int, float> _dmgValues = new Dictionary<int, float>();
        
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

                if (float.TryParse(label, out var dmgValue))
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
        public static float GetDMGValueFromItemPropertyCostTableValue(int costTableValue)
        {
            if (!_dmgValues.ContainsKey(costTableValue))
                return 0.5f;

            return _dmgValues[costTableValue];
        }

        /// <summary>
        /// Calculates damage based on the damage formula:
        /// 20 * (DMG * 5 / defense (min 5)) * (attackAttribute+5)/(defenseAttribute+5)
        /// 
        /// Basic stuff - 10 defense, 1-2 dmg, 0-2 attributes, 40-80 hp + 0-100 from gear
        /// Top tier stuff - 100-150 defense, 23 damage (two handed = +1 damage), 0-10 attributes, 40-440 + 0-410 from gear
        /// 
        /// approximate damage scenarios: 
        /// mynock hits lightly armoured PC.  20 * (2.5 / 5) * (2 / 4) = 5.
        /// mynock hits defensive PC.  20 * (2.5 / 5) * (2 / 6) = 3
        /// PC shoots mynock.  20 * (7.5 / 5) * (4 / 4) = 30
        /// top tier heavy blaster shoots forcie.  20 * (115/85) * (9 / 7) =~ 30
        /// top tier heavy blaster shoots tank.  20 * 115/150 * (9/12) =~ 10
        /// top tier 2h vibroblade hits forcie.   20 * (115/85) * (19 / 7) =~ 75
        /// top tier 2h vibroblade hits tank.  20 * (115/150) * (19/12) =~ 22.5
        /// 
        /// max damage vs 100 force defense.  20 * (250/100) * (14 / 6) =~ 100
        /// 
        /// Base damage is 75%-100% of the number shown.  On a critical, max damage is increased by 25% per point of multiplier 
        /// and min damage is set to 100%. 
        /// 
        /// </summary>
        /// <param name="dmg">Base DMG value. Values should be between 0.5 and 50.0</param>
        /// <param name="attackAttribute">Attack attribute of the attacker. Typically Might/Perception for melee/ranged or Willpower for ether</param>
        /// <param name="defense">The total defense value of the target</param>
        /// <param name="defenseAttribute">The defense attribute of the target. Typically Vitality for melee/ranged or Willpower for ether</param>
        /// <param name="isCritical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <returns>A damage value to apply to the target.</returns>
        public static int CalculateDamage(
            float dmg,
            float attackAttribute,
            float defense,
            float defenseAttribute,
            int critical)
        {
            // Formula: 20 * (DMG * 5 / defense (min 5)) * (attackAttribute+5)/(defenseAttribute+5)
            if (defense < 5) defense = 5;
            var maxDamage = 20 * (dmg * 5) / defense * (attackAttribute + 5) / (defenseAttribute + 5);
            var minDamage = maxDamage * 0.75f;

            // Criticals - 25% bonus to damage range per multiplier point.
            if (critical > 0)
            {
                minDamage = maxDamage;
                switch(critical)
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
        /// Retrieves the level of an NPC. This is determined by an item property located on the NPC's skin.
        /// If no skin is equipped or the item property does not exist, the NPC's level will be returned as zero.
        /// </summary>
        /// <returns>The level of the NPC, or zero if not found.</returns>
        public static int GetNPCLevel(uint npc)
        {
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, npc);
            if (!GetIsObjectValid(skin))
                return 0;

            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.NPCLevel)
                {
                    return GetItemPropertyCostTableValue(ip);
                }
            }

            return 0;
        }

    }
}
