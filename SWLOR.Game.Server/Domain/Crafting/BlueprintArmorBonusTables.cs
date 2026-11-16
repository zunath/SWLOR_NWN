using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>The armor blueprint bonus tables (combat and crafting, tiers 1 to 3), moved out of <see cref="BlueprintBonuses"/> unchanged.</summary>
    internal static class BlueprintArmorBonusTables
    {
        internal static List<BlueprintBonus> Tier1ArmorCombatBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Resistance
                new(12, EnhancementSubType.DefensePhysical, 1),
                new(6, EnhancementSubType.DefensePhysical, 2),
                new(12, EnhancementSubType.DefenseForce, 1),
                new(6, EnhancementSubType.DefenseForce, 2),
                new(14, EnhancementSubType.ResistanceElectrical, 3),
                new(7, EnhancementSubType.ResistanceElectrical, 4),
                new(14, EnhancementSubType.ResistanceFire, 3),
                new(7, EnhancementSubType.ResistanceFire, 4),
                new(14, EnhancementSubType.ResistanceIce, 3),
                new(7, EnhancementSubType.ResistanceIce, 4),
                new(14, EnhancementSubType.ResistancePoison, 3),
                new(7, EnhancementSubType.ResistancePoison, 4),
                new(10, EnhancementSubType.ResistanceMind, 3),
                new(5, EnhancementSubType.ResistanceMind, 4),
                new(10, EnhancementSubType.ResistanceMobility, 3),
                new(5, EnhancementSubType.ResistanceMobility, 4),
                new(10, EnhancementSubType.ResistanceTrauma, 3),
                new(5, EnhancementSubType.ResistanceTrauma, 4),
                new(10, EnhancementSubType.ResistanceDisruption, 3),
                new(5, EnhancementSubType.ResistanceDisruption, 4),

                // Evasion
                new(5, EnhancementSubType.Evasion, 1),
                new(1, EnhancementSubType.Evasion, 2),

                // FP
                new(15, EnhancementSubType.FP, 1),
                new(10, EnhancementSubType.FP, 2),
                new(5, EnhancementSubType.FP, 3),
                new(2, EnhancementSubType.FP, 4),

                // HP
                new(15, EnhancementSubType.HP, 5),
                new(10, EnhancementSubType.HP, 7),
                new(5, EnhancementSubType.HP, 9),
                new(2, EnhancementSubType.HP, 11),

                // Combat Readiness
                new(5, EnhancementSubType.CombatReadiness, 1),
                new(1, EnhancementSubType.CombatReadiness, 2),

                // STM
                new(15, EnhancementSubType.Stamina, 1),
                new(10, EnhancementSubType.Stamina, 2),
                new(5, EnhancementSubType.Stamina, 3),
                new(2, EnhancementSubType.Stamina, 4),
            };

            return list;
        }

        internal static List<BlueprintBonus> Tier2ArmorCombatBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Resistance
                new(14, EnhancementSubType.DefensePhysical, 2),
                new(9, EnhancementSubType.DefensePhysical, 3),
                new(3, EnhancementSubType.DefensePhysical, 4),
                new(14, EnhancementSubType.DefenseForce, 2),
                new(9, EnhancementSubType.DefenseForce, 3),
                new(3, EnhancementSubType.DefenseForce, 4),
                new(16, EnhancementSubType.ResistanceElectrical, 5),
                new(10, EnhancementSubType.ResistanceElectrical, 6),
                new(4, EnhancementSubType.ResistanceElectrical, 7),
                new(16, EnhancementSubType.ResistanceFire, 5),
                new(10, EnhancementSubType.ResistanceFire, 6),
                new(4, EnhancementSubType.ResistanceFire, 7),
                new(16, EnhancementSubType.ResistanceIce, 5),
                new(10, EnhancementSubType.ResistanceIce, 6),
                new(4, EnhancementSubType.ResistanceIce, 7),
                new(16, EnhancementSubType.ResistancePoison, 5),
                new(10, EnhancementSubType.ResistancePoison, 6),
                new(4, EnhancementSubType.ResistancePoison, 7),
                new(12, EnhancementSubType.ResistanceMind, 5),
                new(7, EnhancementSubType.ResistanceMind, 6),
                new(2, EnhancementSubType.ResistanceMind, 7),
                new(12, EnhancementSubType.ResistanceMobility, 5),
                new(7, EnhancementSubType.ResistanceMobility, 6),
                new(2, EnhancementSubType.ResistanceMobility, 7),
                new(12, EnhancementSubType.ResistanceTrauma, 5),
                new(7, EnhancementSubType.ResistanceTrauma, 6),
                new(2, EnhancementSubType.ResistanceTrauma, 7),
                new(12, EnhancementSubType.ResistanceDisruption, 5),
                new(7, EnhancementSubType.ResistanceDisruption, 6),
                new(2, EnhancementSubType.ResistanceDisruption, 7),

                // Evasion
                new(10, EnhancementSubType.Evasion, 1),
                new(5, EnhancementSubType.Evasion, 2),
                new(1, EnhancementSubType.Evasion, 3),

                // FP
                new(15, EnhancementSubType.FP, 2),
                new(10, EnhancementSubType.FP, 3),
                new(5, EnhancementSubType.FP, 4),
                new(2, EnhancementSubType.FP, 5),

                // HP
                new(15, EnhancementSubType.HP, 7),
                new(10, EnhancementSubType.HP, 9),
                new(5, EnhancementSubType.HP, 11),
                new(2, EnhancementSubType.HP, 13),

                // Combat Readiness
                new(5, EnhancementSubType.CombatReadiness, 2),
                new(1, EnhancementSubType.CombatReadiness, 3),

                // STM
                new(15, EnhancementSubType.Stamina, 2),
                new(10, EnhancementSubType.Stamina, 3),
                new(5, EnhancementSubType.Stamina, 4),
                new(2, EnhancementSubType.Stamina, 5),
            };


            return list;
        }

        internal static List<BlueprintBonus> Tier3ArmorCombatBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Resistance
                new(14, EnhancementSubType.DefensePhysical, 3),
                new(9, EnhancementSubType.DefensePhysical, 4),
                new(3, EnhancementSubType.DefensePhysical, 5),
                new(14, EnhancementSubType.DefenseForce, 3),
                new(9, EnhancementSubType.DefenseForce, 4),
                new(3, EnhancementSubType.DefenseForce, 5),
                new(16, EnhancementSubType.ResistanceElectrical, 7),
                new(10, EnhancementSubType.ResistanceElectrical, 8),
                new(4, EnhancementSubType.ResistanceElectrical, 9),
                new(16, EnhancementSubType.ResistanceFire, 7),
                new(10, EnhancementSubType.ResistanceFire, 8),
                new(4, EnhancementSubType.ResistanceFire, 9),
                new(16, EnhancementSubType.ResistanceIce, 7),
                new(10, EnhancementSubType.ResistanceIce, 8),
                new(4, EnhancementSubType.ResistanceIce, 9),
                new(16, EnhancementSubType.ResistancePoison, 7),
                new(10, EnhancementSubType.ResistancePoison, 8),
                new(4, EnhancementSubType.ResistancePoison, 9),
                new(12, EnhancementSubType.ResistanceMind, 7),
                new(7, EnhancementSubType.ResistanceMind, 8),
                new(2, EnhancementSubType.ResistanceMind, 9),
                new(12, EnhancementSubType.ResistanceMobility, 7),
                new(7, EnhancementSubType.ResistanceMobility, 8),
                new(2, EnhancementSubType.ResistanceMobility, 9),
                new(12, EnhancementSubType.ResistanceTrauma, 7),
                new(7, EnhancementSubType.ResistanceTrauma, 8),
                new(2, EnhancementSubType.ResistanceTrauma, 9),
                new(12, EnhancementSubType.ResistanceDisruption, 7),
                new(7, EnhancementSubType.ResistanceDisruption, 8),
                new(2, EnhancementSubType.ResistanceDisruption, 9),

                // Evasion
                new(10, EnhancementSubType.Evasion, 2),
                new(5, EnhancementSubType.Evasion, 3),
                new(1, EnhancementSubType.Evasion, 4),

                // FP
                new(15, EnhancementSubType.FP, 3),
                new(10, EnhancementSubType.FP, 4),
                new(5, EnhancementSubType.FP, 5),
                new(2, EnhancementSubType.FP, 6),

                // HP
                new(15, EnhancementSubType.HP, 9),
                new(10, EnhancementSubType.HP, 11),
                new(5, EnhancementSubType.HP, 13),
                new(2, EnhancementSubType.HP, 15),

                // Combat Readiness
                new(5, EnhancementSubType.CombatReadiness, 3),
                new(1, EnhancementSubType.CombatReadiness, 4),

                // STM
                new(15, EnhancementSubType.Stamina, 3),
                new(10, EnhancementSubType.Stamina, 4),
                new(5, EnhancementSubType.Stamina, 5),
                new(2, EnhancementSubType.Stamina, 6),
            };

            return list;
        }

        internal static List<BlueprintBonus> Tier1ArmorCraftingBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Control
                new(10, EnhancementSubType.ControlAgriculture, 1),
                new(5, EnhancementSubType.ControlAgriculture, 2),
                new(10, EnhancementSubType.ControlEngineering, 1),
                new(5, EnhancementSubType.ControlEngineering, 2),
                new(10, EnhancementSubType.ControlFabrication, 1),
                new(5, EnhancementSubType.ControlFabrication, 2),
                new(10, EnhancementSubType.ControlSmithery, 1),
                new(5, EnhancementSubType.ControlSmithery, 2),

                // Craftsmanship
                new(10, EnhancementSubType.CraftsmanshipAgriculture, 1),
                new(5, EnhancementSubType.CraftsmanshipAgriculture, 2),
                new(10, EnhancementSubType.CraftsmanshipEngineering, 1),
                new(5, EnhancementSubType.CraftsmanshipEngineering, 2),
                new(10, EnhancementSubType.CraftsmanshipFabrication, 1),
                new(5, EnhancementSubType.CraftsmanshipFabrication, 2),
                new(10, EnhancementSubType.CraftsmanshipSmithery, 1),
                new(5, EnhancementSubType.CraftsmanshipSmithery, 2),

                // Evasion
                new(5, EnhancementSubType.Evasion, 1),
                new(1, EnhancementSubType.Evasion, 2),

                // FP
                new(15, EnhancementSubType.FP, 1),
                new(10, EnhancementSubType.FP, 2),
                new(5, EnhancementSubType.FP, 3),
                new(2, EnhancementSubType.FP, 4),

                // HP
                new(15, EnhancementSubType.HP, 5),
                new(10, EnhancementSubType.HP, 7),
                new(5, EnhancementSubType.HP, 9),
                new(2, EnhancementSubType.HP, 11),

                // STM
                new(15, EnhancementSubType.Stamina, 1),
                new(10, EnhancementSubType.Stamina, 2),
                new(5, EnhancementSubType.Stamina, 3),
                new(2, EnhancementSubType.Stamina, 4),
            };




            return list;
        }

        internal static List<BlueprintBonus> Tier2ArmorCraftingBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Control
                new(15, EnhancementSubType.ControlAgriculture, 1),
                new(10, EnhancementSubType.ControlAgriculture, 2),
                new(5, EnhancementSubType.ControlAgriculture, 3),
                new(15, EnhancementSubType.ControlEngineering, 1),
                new(10, EnhancementSubType.ControlEngineering, 2),
                new(5, EnhancementSubType.ControlEngineering, 3),
                new(15, EnhancementSubType.ControlFabrication, 1),
                new(10, EnhancementSubType.ControlFabrication, 2),
                new(5, EnhancementSubType.ControlFabrication, 3),
                new(15, EnhancementSubType.ControlSmithery, 1),
                new(10, EnhancementSubType.ControlSmithery, 2),
                new(5, EnhancementSubType.ControlSmithery, 3),

                // Craftsmanship
                new(15, EnhancementSubType.CraftsmanshipAgriculture, 1),
                new(10, EnhancementSubType.CraftsmanshipAgriculture, 2),
                new(5, EnhancementSubType.CraftsmanshipAgriculture, 3),
                new(15, EnhancementSubType.CraftsmanshipEngineering, 1),
                new(10, EnhancementSubType.CraftsmanshipEngineering, 2),
                new(5, EnhancementSubType.CraftsmanshipEngineering, 3),
                new(15, EnhancementSubType.CraftsmanshipFabrication, 1),
                new(10, EnhancementSubType.CraftsmanshipFabrication, 2),
                new(5, EnhancementSubType.CraftsmanshipFabrication, 3),
                new(15, EnhancementSubType.CraftsmanshipSmithery, 1),
                new(10, EnhancementSubType.CraftsmanshipSmithery, 2),
                new(5, EnhancementSubType.CraftsmanshipSmithery, 3),

                // Evasion
                new(10, EnhancementSubType.Evasion, 1),
                new(5, EnhancementSubType.Evasion, 2),
                new(1, EnhancementSubType.Evasion, 3),

                // FP
                new(15, EnhancementSubType.FP, 2),
                new(10, EnhancementSubType.FP, 3),
                new(5, EnhancementSubType.FP, 4),
                new(2, EnhancementSubType.FP, 5),

                // HP
                new(15, EnhancementSubType.HP, 7),
                new(10, EnhancementSubType.HP, 9),
                new(5, EnhancementSubType.HP, 11),
                new(2, EnhancementSubType.HP, 13),

                // STM
                new(15, EnhancementSubType.Stamina, 2),
                new(10, EnhancementSubType.Stamina, 3),
                new(5, EnhancementSubType.Stamina, 4),
                new(2, EnhancementSubType.Stamina, 5),
            };


            return list;
        }

        internal static List<BlueprintBonus> Tier3ArmorCraftingBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Control
                new(15, EnhancementSubType.ControlAgriculture, 2),
                new(10, EnhancementSubType.ControlAgriculture, 3),
                new(5, EnhancementSubType.ControlAgriculture, 4),
                new(15, EnhancementSubType.ControlEngineering, 2),
                new(10, EnhancementSubType.ControlEngineering, 3),
                new(5, EnhancementSubType.ControlEngineering, 4),
                new(15, EnhancementSubType.ControlFabrication, 2),
                new(10, EnhancementSubType.ControlFabrication, 3),
                new(5, EnhancementSubType.ControlFabrication, 4),
                new(15, EnhancementSubType.ControlSmithery, 2),
                new(10, EnhancementSubType.ControlSmithery, 3),
                new(5, EnhancementSubType.ControlSmithery, 4),

                // Craftsmanship
                new(15, EnhancementSubType.CraftsmanshipAgriculture, 2),
                new(10, EnhancementSubType.CraftsmanshipAgriculture, 3),
                new(5, EnhancementSubType.CraftsmanshipAgriculture, 4),
                new(15, EnhancementSubType.CraftsmanshipEngineering, 2),
                new(10, EnhancementSubType.CraftsmanshipEngineering, 3),
                new(5, EnhancementSubType.CraftsmanshipEngineering, 4),
                new(15, EnhancementSubType.CraftsmanshipFabrication, 2),
                new(10, EnhancementSubType.CraftsmanshipFabrication, 3),
                new(5, EnhancementSubType.CraftsmanshipFabrication, 4),
                new(15, EnhancementSubType.CraftsmanshipSmithery, 2),
                new(10, EnhancementSubType.CraftsmanshipSmithery, 3),
                new(5, EnhancementSubType.CraftsmanshipSmithery, 4),

                // Evasion
                new(10, EnhancementSubType.Evasion, 2),
                new(5, EnhancementSubType.Evasion, 3),
                new(1, EnhancementSubType.Evasion, 4),

                // FP
                new(15, EnhancementSubType.FP, 3),
                new(10, EnhancementSubType.FP, 4),
                new(5, EnhancementSubType.FP, 5),
                new(2, EnhancementSubType.FP, 6),

                // HP
                new(15, EnhancementSubType.HP, 9),
                new(10, EnhancementSubType.HP, 11),
                new(5, EnhancementSubType.HP, 13),
                new(2, EnhancementSubType.HP, 15),

                // STM
                new(15, EnhancementSubType.Stamina, 3),
                new(10, EnhancementSubType.Stamina, 4),
                new(5, EnhancementSubType.Stamina, 5),
                new(2, EnhancementSubType.Stamina, 6),
            };

            return list;
        }
    }
}
