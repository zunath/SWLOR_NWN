using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using static SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetFormat;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// The stats table rows for damage dealt, resource cost, haste and the single weapon bonus.
    /// </summary>
    public class CharacterSheetDamageRows
    {
        private ICombatAttackTimingService CombatAttackTiming { get; }
        private ICombatResourcesService CombatResources { get; }
        private IStatService Stat { get; }

        public CharacterSheetDamageRows(ICombatAttackTimingService combatAttackTiming, ICombatResourcesService combatResources, IStatService stat)
        {
            CombatAttackTiming = combatAttackTiming;
            CombatResources = combatResources;
            Stat = stat;
        }

        public void AddRows(ICharacterSheetHost host, Action<string, string, string> addStat)
        {
            addStat("Damage Dealt", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.DamageDealtPercentAdjustment)), "Adjusts all outgoing damage.");
            addStat("Weapon/Force Damage", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.WeaponAndForceDamageDealtPercentAdjustment)), "Adjusts outgoing weapon and Force damage. Stacks with Damage Dealt.");
            AddHighResourceAbilityDamageStats(host, addStat);
            addStat("Healing Received", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.HealingReceivedPercentAdjustment)), "Adjusts the amount of healing you receive from all sources.");
            addStat("Enmity", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.EnmityPercentAdjustment)), "Increases or decreases the rate at which enmity is acquired.");
            addStat("FP Cost", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.FPCostPercentAdjustment)), "Adjusts the FP cost of abilities. Lower is better.");
            addStat("STM Cost", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.AbilityStaminaCostPercentAdjustment)), "Adjusts the Stamina cost of abilities. Lower is better.");
            addStat("Haste", FormatPercent(CombatAttackTiming.CalculateAttackDelayReduction(host.Target)), "Increases attack speed. Negative values slow attacks.");
            addStat("Off-Hand Haste", FormatPercent(CombatAttackTiming.CalculateOffhandAttackDelayReduction(host.Target)), "Increases off-hand attack speed. Only applies while dual wielding.");
            addStat("Single Weapon DMG", FormatPercent(EquipmentPredicates.HasSingleWeapon(host.Target) ? WeaponDamage.GetSingleWeaponPercent(host.Target) : 0),
                "Bonus to the item's weapon DMG with one eligible one-handed melee, pistol, or throwing weapon and an empty off hand. Includes the natural bonus and Doublehand; already included in equipped DMG.");
        }

        private void AddHighResourceAbilityDamageStats(ICharacterSheetHost host, Action<string, string, string> addStat)
        {
            AddConditionalDamage(StatType.HighFPAndStaminaAbilityDamageBonus,
                StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, " DMG");
            AddConditionalDamage(StatType.HighFPAndStaminaAbilityDamagePercentAdjustment,
                StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent, "% DMG");

            void AddConditionalDamage(StatType payload, StatType thresholdStat, string units)
            {
                foreach (var group in Stat.GetStatSources(host.Target, payload)
                             .Where(source => source[payload] > 0 && source[thresholdStat] > 0)
                             .GroupBy(source => source[thresholdStat]).OrderBy(group => group.Key))
                {
                    var threshold = group.Key;
                    var bonus = group.Sum(source => source[payload]);
                    var active = CombatResources.IsCurrentFPAndStaminaAbovePercent(host.Target, threshold);
                    addStat($"Ability DMG above {threshold}% FP/STM",
                        active ? $"Active (+{bonus}{units})" : $"Inactive ({threshold}% required)",
                        $"Hostile combat abilities gain +{bonus}{units} while FP and STM are both above {threshold}%.");
                }
            }

        }
    }
}
