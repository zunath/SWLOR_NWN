using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>The weapon blueprint bonus tables (combat and crafting, tiers 1 to 3), moved out of <see cref="BlueprintBonuses"/> unchanged.</summary>
    internal static class BlueprintWeaponBonusTables
    {
        internal static List<BlueprintBonus> Tier1WeaponCombatBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Attack
                new(10, EnhancementSubType.Attack, 1),
                new(5, EnhancementSubType.Attack, 2),

                // Accuracy
                new(10, EnhancementSubType.Accuracy, 1),
                new(5, EnhancementSubType.Accuracy, 2),

                // Force Attack
                new(10, EnhancementSubType.ForceAttack, 1),
                new(5, EnhancementSubType.ForceAttack, 2),

                // DMG
                new(3, EnhancementSubType.DMG, 2),
                new(1, EnhancementSubType.DMG, 3),
                new(3, EnhancementSubType.DMG, 1, CombatDamageType.Electrical),
                new(1, EnhancementSubType.DMG, 2, CombatDamageType.Electrical),
                new(3, EnhancementSubType.DMG, 1, CombatDamageType.Fire),
                new(1, EnhancementSubType.DMG, 2, CombatDamageType.Fire),
                new(3, EnhancementSubType.DMG, 1, CombatDamageType.Ice),
                new(1, EnhancementSubType.DMG, 2, CombatDamageType.Ice),
                new(3, EnhancementSubType.DMG, 1, CombatDamageType.Poison),
                new(1, EnhancementSubType.DMG, 2, CombatDamageType.Poison),

                // Evasion
                new(5, EnhancementSubType.Evasion, 1),
                new(1, EnhancementSubType.Evasion, 2),

                // FP
                new(10, EnhancementSubType.FP, 2),
                new(5, EnhancementSubType.FP, 3),
                new(2, EnhancementSubType.FP, 4),

                // HP
                new(10, EnhancementSubType.HP, 5),
                new(5, EnhancementSubType.HP, 7),
                new(2, EnhancementSubType.HP, 9),

                // STM
                new(10, EnhancementSubType.Stamina, 2),
                new(5, EnhancementSubType.Stamina, 3),
                new(2, EnhancementSubType.Stamina, 4),
            };


            return list;
        }

        internal static List<BlueprintBonus> Tier2WeaponCombatBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Attack
                new(15, EnhancementSubType.Attack, 1),
                new(10, EnhancementSubType.Attack, 2),
                new(5, EnhancementSubType.Attack, 3),

                // Accuracy
                new(15, EnhancementSubType.Accuracy, 1),
                new(10, EnhancementSubType.Accuracy, 2),
                new(5, EnhancementSubType.Accuracy, 3),

                // Force Attack
                new(15, EnhancementSubType.ForceAttack, 1),
                new(10, EnhancementSubType.ForceAttack, 2),
                new(5, EnhancementSubType.ForceAttack, 3),

                // DMG
                new(5, EnhancementSubType.DMG, 2),
                new(3, EnhancementSubType.DMG, 3),
                new(1, EnhancementSubType.DMG, 4),
                new(5, EnhancementSubType.DMG, 1, CombatDamageType.Electrical),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Electrical),
                new(1, EnhancementSubType.DMG, 3, CombatDamageType.Electrical),
                new(5, EnhancementSubType.DMG, 1, CombatDamageType.Fire),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Fire),
                new(1, EnhancementSubType.DMG, 3, CombatDamageType.Fire),
                new(5, EnhancementSubType.DMG, 1, CombatDamageType.Ice),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Ice),
                new(1, EnhancementSubType.DMG, 3, CombatDamageType.Ice),
                new(5, EnhancementSubType.DMG, 1, CombatDamageType.Poison),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Poison),
                new(1, EnhancementSubType.DMG, 3, CombatDamageType.Poison),

                // Evasion
                new(3, EnhancementSubType.Evasion, 1),
                new(2, EnhancementSubType.Evasion, 2),
                new(1, EnhancementSubType.Evasion, 3),

                // FP
                new(15, EnhancementSubType.FP, 2),
                new(10, EnhancementSubType.FP, 3),
                new(5, EnhancementSubType.FP, 4),
                new(2, EnhancementSubType.FP, 5),

                // HP
                new(15, EnhancementSubType.HP, 5),
                new(10, EnhancementSubType.HP, 7),
                new(5, EnhancementSubType.HP, 9),
                new(2, EnhancementSubType.HP, 11),

                // STM
                new(15, EnhancementSubType.Stamina, 2),
                new(10, EnhancementSubType.Stamina, 3),
                new(5, EnhancementSubType.Stamina, 4),
                new(2, EnhancementSubType.Stamina, 5),
            };


            return list;
        }

        internal static List<BlueprintBonus> Tier3WeaponCombatBonuses()
        {
            var list = new List<BlueprintBonus>
            {
                // Attack
                new(15, EnhancementSubType.Attack, 2),
                new(10, EnhancementSubType.Attack, 3),
                new(5, EnhancementSubType.Attack, 4),

                // Accuracy
                new(15, EnhancementSubType.Accuracy, 2),
                new(10, EnhancementSubType.Accuracy, 3),
                new(5, EnhancementSubType.Accuracy, 4),

                // Force Attack
                new(15, EnhancementSubType.ForceAttack, 2),
                new(10, EnhancementSubType.ForceAttack, 3),
                new(5, EnhancementSubType.ForceAttack, 4),

                // DMG
                new(3, EnhancementSubType.DMG, 3),
                new(2, EnhancementSubType.DMG, 4),
                new(1, EnhancementSubType.DMG, 5),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Electrical),
                new(2, EnhancementSubType.DMG, 3, CombatDamageType.Electrical),
                new(1, EnhancementSubType.DMG, 4, CombatDamageType.Electrical),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Fire),
                new(2, EnhancementSubType.DMG, 3, CombatDamageType.Fire),
                new(1, EnhancementSubType.DMG, 4, CombatDamageType.Fire),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Ice),
                new(2, EnhancementSubType.DMG, 3, CombatDamageType.Ice),
                new(1, EnhancementSubType.DMG, 4, CombatDamageType.Ice),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Poison),
                new(2, EnhancementSubType.DMG, 3, CombatDamageType.Poison),
                new(1, EnhancementSubType.DMG, 4, CombatDamageType.Poison),

                // Evasion
                new(3, EnhancementSubType.Evasion, 2),
                new(2, EnhancementSubType.Evasion, 3),
                new(1, EnhancementSubType.Evasion, 4),

                // FP
                new(10, EnhancementSubType.FP, 4),
                new(5, EnhancementSubType.FP, 5),
                new(2, EnhancementSubType.FP, 6),

                // HP
                new(10, EnhancementSubType.HP, 9),
                new(5, EnhancementSubType.HP, 11),
                new(2, EnhancementSubType.HP, 13),

                // STM
                new(10, EnhancementSubType.Stamina, 4),
                new(5, EnhancementSubType.Stamina, 5),
                new(2, EnhancementSubType.Stamina, 6),
            };


            return list;
        }

        internal static List<BlueprintBonus> Tier1WeaponCraftingBonuses()
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
                new(10, EnhancementSubType.FP, 2),
                new(5, EnhancementSubType.FP, 3),
                new(2, EnhancementSubType.FP, 4),

                // HP
                new(10, EnhancementSubType.HP, 5),
                new(5, EnhancementSubType.HP, 7),
                new(2, EnhancementSubType.HP, 9),

                // STM
                new(10, EnhancementSubType.Stamina, 2),
                new(5, EnhancementSubType.Stamina, 3),
                new(2, EnhancementSubType.Stamina, 4),
            };


            return list;
        }

        internal static List<BlueprintBonus> Tier2WeaponCraftingBonuses()
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
                new(3, EnhancementSubType.Evasion, 1),
                new(2, EnhancementSubType.Evasion, 2),
                new(1, EnhancementSubType.Evasion, 3),

                // FP
                new(15, EnhancementSubType.FP, 2),
                new(10, EnhancementSubType.FP, 3),
                new(5, EnhancementSubType.FP, 4),
                new(2, EnhancementSubType.FP, 5),

                // HP
                new(15, EnhancementSubType.HP, 5),
                new(10, EnhancementSubType.HP, 7),
                new(5, EnhancementSubType.HP, 9),
                new(2, EnhancementSubType.HP, 11),

                // STM
                new(15, EnhancementSubType.Stamina, 2),
                new(10, EnhancementSubType.Stamina, 3),
                new(5, EnhancementSubType.Stamina, 4),
                new(2, EnhancementSubType.Stamina, 5),
            };


            return list;
        }

        internal static List<BlueprintBonus> Tier3WeaponCraftingBonuses()
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
                new(3, EnhancementSubType.Evasion, 2),
                new(2, EnhancementSubType.Evasion, 3),
                new(1, EnhancementSubType.Evasion, 4),

                // FP
                new(10, EnhancementSubType.FP, 4),
                new(5, EnhancementSubType.FP, 5),
                new(2, EnhancementSubType.FP, 6),

                // HP
                new(10, EnhancementSubType.HP, 9),
                new(5, EnhancementSubType.HP, 11),
                new(2, EnhancementSubType.HP, 13),

                // STM
                new(10, EnhancementSubType.Stamina, 4),
                new(5, EnhancementSubType.Stamina, 5),
                new(2, EnhancementSubType.Stamina, 6),
            };


            return list;
        }
    }
}
