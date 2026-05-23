using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.CraftService
{
    internal class BlueprintBonuses
    {
        private readonly Dictionary<RecipeEnhancementType, Dictionary<bool, Dictionary<int, List<BlueprintBonus>>>> _bonusesByEnhancementType = new();

        public BlueprintBonuses()
        {
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon] = new Dictionary<bool, Dictionary<int, List<BlueprintBonus>>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][false] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][false][1] = Tier1WeaponCombatBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][false][2] = Tier2WeaponCombatBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][false][3] = Tier3WeaponCombatBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][true] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][true][1] = Tier1WeaponCraftingBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][true][2] = Tier2WeaponCraftingBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][true][3] = Tier3WeaponCraftingBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Armor] = new Dictionary<bool, Dictionary<int, List<BlueprintBonus>>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][false] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][false][1] = Tier1ArmorCombatBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][false][2] = Tier2ArmorCombatBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][false][3] = Tier3ArmorCombatBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Armor][true] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][true][1] = Tier1ArmorCraftingBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][true][2] = Tier2ArmorCraftingBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][true][3] = Tier3ArmorCraftingBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Food] = new Dictionary<bool, Dictionary<int, List<BlueprintBonus>>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][false] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][false][1] = Tier1FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][false][2] = Tier2FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][false][3] = Tier3FoodBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Food][true] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][true][1] = Tier1FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][true][2] = Tier2FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][true][3] = Tier3FoodBonuses();
        }

        public BlueprintBonus PickBonus(RecipeEnhancementType enhancementType, int tier, bool isCraftingItem)
        {
            if (!_bonusesByEnhancementType.ContainsKey(enhancementType) ||
                !_bonusesByEnhancementType[enhancementType].ContainsKey(isCraftingItem) ||
                !_bonusesByEnhancementType[enhancementType][isCraftingItem].ContainsKey(tier))
                return null;

            var set = _bonusesByEnhancementType[enhancementType][isCraftingItem][tier];
            var weights = set.Select(x => x.Weight).ToArray();
            var index = Random.GetRandomWeightedIndex(weights);

            return set[index];
        }

        private List<BlueprintBonus> Tier1WeaponCombatBonuses()
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
                new(3, EnhancementSubType.DMG, 1, CombatDamageType.Force),
                new(1, EnhancementSubType.DMG, 2, CombatDamageType.Force),
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

        private List<BlueprintBonus> Tier2WeaponCombatBonuses()
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
                new(5, EnhancementSubType.DMG, 1, CombatDamageType.Force),
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Force),
                new(1, EnhancementSubType.DMG, 3, CombatDamageType.Force),
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

        private List<BlueprintBonus> Tier3WeaponCombatBonuses()
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
                new(3, EnhancementSubType.DMG, 2, CombatDamageType.Force),
                new(2, EnhancementSubType.DMG, 3, CombatDamageType.Force),
                new(1, EnhancementSubType.DMG, 4, CombatDamageType.Force),
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

        private List<BlueprintBonus> Tier1WeaponCraftingBonuses()
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

        private List<BlueprintBonus> Tier2WeaponCraftingBonuses()
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

        private List<BlueprintBonus> Tier3WeaponCraftingBonuses()
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
        private List<BlueprintBonus> Tier1ArmorCombatBonuses()
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

        private List<BlueprintBonus> Tier2ArmorCombatBonuses()
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

        private List<BlueprintBonus> Tier3ArmorCombatBonuses()
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

        private List<BlueprintBonus> Tier1ArmorCraftingBonuses()
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

        private List<BlueprintBonus> Tier2ArmorCraftingBonuses()
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

        private List<BlueprintBonus> Tier3ArmorCraftingBonuses()
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

        private List<BlueprintBonus> Tier1FoodBonuses()
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

        private List<BlueprintBonus> Tier2FoodBonuses()
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

        private List<BlueprintBonus> Tier3FoodBonuses()
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
