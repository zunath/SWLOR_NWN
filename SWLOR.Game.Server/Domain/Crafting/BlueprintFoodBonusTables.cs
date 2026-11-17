using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>The food blueprint bonus tables (tiers 1 to 3), moved out of <see cref="BlueprintBonuses"/> unchanged.</summary>
    internal static class BlueprintFoodBonusTables
    {
        internal static List<BlueprintBonus> Tier1FoodBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Duration
                new(10, EnhancementSubType.FoodBonusDuration, 1),
                new(5, EnhancementSubType.FoodBonusDuration, 2),

                // FP
                new(10, EnhancementSubType.FoodBonusFP, 2),
                new(5, EnhancementSubType.FoodBonusFP, 3),

                // FP Regen
                new(10, EnhancementSubType.FoodBonusFPRegen, 1),
                new(5, EnhancementSubType.FoodBonusFPRegen, 2),

                // HP
                new(10, EnhancementSubType.FoodBonusHP, 3),
                new(5, EnhancementSubType.FoodBonusHP, 4),

                // HP Regen
                new(5, EnhancementSubType.FoodBonusHPRegen, 2),
                new(3, EnhancementSubType.FoodBonusHPRegen, 3),

                // Combat Readiness
                new(10, EnhancementSubType.FoodBonusCombatReadiness, 3),
                new(5, EnhancementSubType.FoodBonusCombatReadiness, 4),

                // Rest Regen
                new(10, EnhancementSubType.FoodBonusRestRegen, 4),
                new(5, EnhancementSubType.FoodBonusRestRegen, 5),

                // STM
                new(10, EnhancementSubType.FoodBonusSTM, 2),
                new(5, EnhancementSubType.FoodBonusSTM, 3),

                // STM Regen
                new(10, EnhancementSubType.FoodBonusSTMRegen, 1),
                new(5, EnhancementSubType.FoodBonusSTMRegen, 2),

                // XP Bonus
                new(10, EnhancementSubType.FoodBonusXPBonus, 3),
                new(5, EnhancementSubType.FoodBonusXPBonus, 4),

                // Resistance
                new(4, EnhancementSubType.FoodBonusElectricalResistance, 3),
                new(2, EnhancementSubType.FoodBonusElectricalResistance, 4),
                new(4, EnhancementSubType.FoodBonusFireResistance, 3),
                new(2, EnhancementSubType.FoodBonusFireResistance, 4),
                new(4, EnhancementSubType.FoodBonusIceResistance, 3),
                new(2, EnhancementSubType.FoodBonusIceResistance, 4),
                new(4, EnhancementSubType.FoodBonusPoisonResistance, 3),
                new(2, EnhancementSubType.FoodBonusPoisonResistance, 4),
                new(3, EnhancementSubType.FoodBonusMindResistance, 3),
                new(1, EnhancementSubType.FoodBonusMindResistance, 4),
                new(3, EnhancementSubType.FoodBonusMobilityResistance, 3),
                new(1, EnhancementSubType.FoodBonusMobilityResistance, 4),
                new(3, EnhancementSubType.FoodBonusTraumaResistance, 3),
                new(1, EnhancementSubType.FoodBonusTraumaResistance, 4),
                new(3, EnhancementSubType.FoodBonusDisruptionResistance, 3),
                new(1, EnhancementSubType.FoodBonusDisruptionResistance, 4),
            };

            return list;
        }

        internal static List<BlueprintBonus> Tier2FoodBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Duration
                new(15, EnhancementSubType.FoodBonusDuration, 2),
                new(10, EnhancementSubType.FoodBonusDuration, 3),
                new(5, EnhancementSubType.FoodBonusDuration, 4),

                // FP
                new(15, EnhancementSubType.FoodBonusFP, 2),
                new(10, EnhancementSubType.FoodBonusFP, 3),
                new(5, EnhancementSubType.FoodBonusFP, 4),

                // FP Regen
                new(15, EnhancementSubType.FoodBonusFPRegen, 2),
                new(10, EnhancementSubType.FoodBonusFPRegen, 3),
                new(5, EnhancementSubType.FoodBonusFPRegen, 4),

                // HP
                new(15, EnhancementSubType.FoodBonusHP, 3),
                new(10, EnhancementSubType.FoodBonusHP, 4),
                new(5, EnhancementSubType.FoodBonusHP, 5),

                // HP Regen
                new(8, EnhancementSubType.FoodBonusHPRegen, 2),
                new(5, EnhancementSubType.FoodBonusHPRegen, 3),
                new(3, EnhancementSubType.FoodBonusHPRegen, 4),

                // Combat Readiness
                new(15, EnhancementSubType.FoodBonusCombatReadiness, 3),
                new(10, EnhancementSubType.FoodBonusCombatReadiness, 4),
                new(5, EnhancementSubType.FoodBonusCombatReadiness, 5),

                // Rest Regen
                new(15, EnhancementSubType.FoodBonusRestRegen, 4),
                new(10, EnhancementSubType.FoodBonusRestRegen, 5),
                new(5, EnhancementSubType.FoodBonusRestRegen, 6),

                // STM
                new(15, EnhancementSubType.FoodBonusSTM, 2),
                new(10, EnhancementSubType.FoodBonusSTM, 3),
                new(5, EnhancementSubType.FoodBonusSTM, 4),

                // STM Regen
                new(15, EnhancementSubType.FoodBonusSTMRegen, 1),
                new(10, EnhancementSubType.FoodBonusSTMRegen, 2),
                new(5, EnhancementSubType.FoodBonusSTMRegen, 3),

                // XP Bonus
                new(10, EnhancementSubType.FoodBonusXPBonus, 6),
                new(10, EnhancementSubType.FoodBonusXPBonus, 9),
                new(5, EnhancementSubType.FoodBonusXPBonus, 12),

                // Resistance
                new(5, EnhancementSubType.FoodBonusElectricalResistance, 5),
                new(3, EnhancementSubType.FoodBonusElectricalResistance, 6),
                new(1, EnhancementSubType.FoodBonusElectricalResistance, 7),
                new(5, EnhancementSubType.FoodBonusFireResistance, 5),
                new(3, EnhancementSubType.FoodBonusFireResistance, 6),
                new(1, EnhancementSubType.FoodBonusFireResistance, 7),
                new(5, EnhancementSubType.FoodBonusIceResistance, 5),
                new(3, EnhancementSubType.FoodBonusIceResistance, 6),
                new(1, EnhancementSubType.FoodBonusIceResistance, 7),
                new(5, EnhancementSubType.FoodBonusPoisonResistance, 5),
                new(3, EnhancementSubType.FoodBonusPoisonResistance, 6),
                new(1, EnhancementSubType.FoodBonusPoisonResistance, 7),
                new(4, EnhancementSubType.FoodBonusMindResistance, 5),
                new(2, EnhancementSubType.FoodBonusMindResistance, 6),
                new(1, EnhancementSubType.FoodBonusMindResistance, 7),
                new(4, EnhancementSubType.FoodBonusMobilityResistance, 5),
                new(2, EnhancementSubType.FoodBonusMobilityResistance, 6),
                new(1, EnhancementSubType.FoodBonusMobilityResistance, 7),
                new(4, EnhancementSubType.FoodBonusTraumaResistance, 5),
                new(2, EnhancementSubType.FoodBonusTraumaResistance, 6),
                new(1, EnhancementSubType.FoodBonusTraumaResistance, 7),
                new(4, EnhancementSubType.FoodBonusDisruptionResistance, 5),
                new(2, EnhancementSubType.FoodBonusDisruptionResistance, 6),
                new(1, EnhancementSubType.FoodBonusDisruptionResistance, 7),
            };

            return list;
        }

        internal static List<BlueprintBonus> Tier3FoodBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Duration
                new(15, EnhancementSubType.FoodBonusDuration, 3),
                new(10, EnhancementSubType.FoodBonusDuration, 4),
                new(5, EnhancementSubType.FoodBonusDuration, 5),

                // FP
                new(15, EnhancementSubType.FoodBonusFP, 3),
                new(10, EnhancementSubType.FoodBonusFP, 4),
                new(5, EnhancementSubType.FoodBonusFP, 5),

                // FP Regen
                new(15, EnhancementSubType.FoodBonusFPRegen, 3),
                new(10, EnhancementSubType.FoodBonusFPRegen, 4),
                new(5, EnhancementSubType.FoodBonusFPRegen, 5),

                // HP
                new(15, EnhancementSubType.FoodBonusHP, 4),
                new(10, EnhancementSubType.FoodBonusHP, 5),
                new(5, EnhancementSubType.FoodBonusHP, 6),

                // HP Regen
                new(8, EnhancementSubType.FoodBonusHPRegen, 3),
                new(5, EnhancementSubType.FoodBonusHPRegen, 4),
                new(3, EnhancementSubType.FoodBonusHPRegen, 5),

                // Combat Readiness
                new(15, EnhancementSubType.FoodBonusCombatReadiness, 4),
                new(10, EnhancementSubType.FoodBonusCombatReadiness, 5),
                new(5, EnhancementSubType.FoodBonusCombatReadiness, 6),

                // Rest Regen
                new(15, EnhancementSubType.FoodBonusRestRegen, 5),
                new(10, EnhancementSubType.FoodBonusRestRegen, 6),
                new(5, EnhancementSubType.FoodBonusRestRegen, 7),

                // STM
                new(15, EnhancementSubType.FoodBonusSTM, 3),
                new(10, EnhancementSubType.FoodBonusSTM, 4),
                new(5, EnhancementSubType.FoodBonusSTM, 5),

                // STM Regen
                new(15, EnhancementSubType.FoodBonusSTMRegen, 2),
                new(10, EnhancementSubType.FoodBonusSTMRegen, 3),
                new(5, EnhancementSubType.FoodBonusSTMRegen, 4),

                // XP Bonus
                new(10, EnhancementSubType.FoodBonusXPBonus, 9),
                new(10, EnhancementSubType.FoodBonusXPBonus, 12),
                new(5, EnhancementSubType.FoodBonusXPBonus, 15),

                // Resistance
                new(5, EnhancementSubType.FoodBonusElectricalResistance, 7),
                new(3, EnhancementSubType.FoodBonusElectricalResistance, 8),
                new(1, EnhancementSubType.FoodBonusElectricalResistance, 9),
                new(5, EnhancementSubType.FoodBonusFireResistance, 7),
                new(3, EnhancementSubType.FoodBonusFireResistance, 8),
                new(1, EnhancementSubType.FoodBonusFireResistance, 9),
                new(5, EnhancementSubType.FoodBonusIceResistance, 7),
                new(3, EnhancementSubType.FoodBonusIceResistance, 8),
                new(1, EnhancementSubType.FoodBonusIceResistance, 9),
                new(5, EnhancementSubType.FoodBonusPoisonResistance, 7),
                new(3, EnhancementSubType.FoodBonusPoisonResistance, 8),
                new(1, EnhancementSubType.FoodBonusPoisonResistance, 9),
                new(4, EnhancementSubType.FoodBonusMindResistance, 7),
                new(2, EnhancementSubType.FoodBonusMindResistance, 8),
                new(1, EnhancementSubType.FoodBonusMindResistance, 9),
                new(4, EnhancementSubType.FoodBonusMobilityResistance, 7),
                new(2, EnhancementSubType.FoodBonusMobilityResistance, 8),
                new(1, EnhancementSubType.FoodBonusMobilityResistance, 9),
                new(4, EnhancementSubType.FoodBonusTraumaResistance, 7),
                new(2, EnhancementSubType.FoodBonusTraumaResistance, 8),
                new(1, EnhancementSubType.FoodBonusTraumaResistance, 9),
                new(4, EnhancementSubType.FoodBonusDisruptionResistance, 7),
                new(2, EnhancementSubType.FoodBonusDisruptionResistance, 8),
                new(1, EnhancementSubType.FoodBonusDisruptionResistance, 9),
            };

            return list;
        }
    }
}
