using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService
{
    internal class BlueprintBonusSet
    {
        private readonly Dictionary<RecipeEnhancementType, Dictionary<int, List<BlueprintBonus>>> _bonusesByEnhancementType = new();
        
        public BlueprintBonusSet()
        {
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][1] = Tier1WeaponBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][2] = Tier2WeaponBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][3] = Tier3WeaponBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Armor] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][1] = Tier1ArmorBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][2] = Tier2ArmorBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][3] = Tier3ArmorBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Food] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][1] = Tier1FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][2] = Tier2FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][3] = Tier3FoodBonuses();
        }

        public BlueprintBonus PickBonus(RecipeEnhancementType enhancementType, int tier)
        {
            if (!_bonusesByEnhancementType.ContainsKey(enhancementType) ||
                !_bonusesByEnhancementType[enhancementType].ContainsKey(tier))
                return null;

            var set = _bonusesByEnhancementType[enhancementType][tier];
            var weights = set.Select(x => x.Weight).ToArray();
            var index = Random.GetRandomWeightedIndex(weights);

            return set[index];
        }
        
        private List<BlueprintBonus> Tier1WeaponBonuses()
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

                // DMG
                new(3, EnhancementSubType.DMGPhysical, 1),
                new(1, EnhancementSubType.DMGPhysical, 2),
                new(3, EnhancementSubType.DMGForce, 1),
                new(1, EnhancementSubType.DMGForce, 2),
                new(3, EnhancementSubType.DMGElectrical, 1),
                new(1, EnhancementSubType.DMGElectrical, 2),
                new(3, EnhancementSubType.DMGFire, 1),
                new(1, EnhancementSubType.DMGFire, 2),
                new(3, EnhancementSubType.DMGIce, 1),
                new(1, EnhancementSubType.DMGIce, 2),
                new(3, EnhancementSubType.DMGPoison, 1),
                new(1, EnhancementSubType.DMGPoison, 2),

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

        private List<BlueprintBonus> Tier2WeaponBonuses()
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

                // DMG
                new(5, EnhancementSubType.DMGPhysical, 1),
                new(3, EnhancementSubType.DMGPhysical, 2),
                new(1, EnhancementSubType.DMGPhysical, 3),
                new(5, EnhancementSubType.DMGForce, 1),
                new(3, EnhancementSubType.DMGForce, 2),
                new(1, EnhancementSubType.DMGForce, 3),
                new(5, EnhancementSubType.DMGElectrical, 1),
                new(3, EnhancementSubType.DMGElectrical, 2),
                new(1, EnhancementSubType.DMGElectrical, 3),
                new(5, EnhancementSubType.DMGFire, 1),
                new(3, EnhancementSubType.DMGFire, 2),
                new(1, EnhancementSubType.DMGFire, 3),
                new(5, EnhancementSubType.DMGIce, 1),
                new(3, EnhancementSubType.DMGIce, 2),
                new(1, EnhancementSubType.DMGIce, 3),
                new(5, EnhancementSubType.DMGPoison, 1),
                new(3, EnhancementSubType.DMGPoison, 2),
                new(1, EnhancementSubType.DMGPoison, 3),

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

        private List<BlueprintBonus> Tier3WeaponBonuses()
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

                // DMG
                new(3, EnhancementSubType.DMGPhysical, 2),
                new(2, EnhancementSubType.DMGPhysical, 3),
                new(1, EnhancementSubType.DMGPhysical, 4),
                new(3, EnhancementSubType.DMGForce, 2),
                new(2, EnhancementSubType.DMGForce, 3),
                new(1, EnhancementSubType.DMGForce, 4),
                new(3, EnhancementSubType.DMGElectrical, 2),
                new(2, EnhancementSubType.DMGElectrical, 3),
                new(1, EnhancementSubType.DMGElectrical, 4),
                new(3, EnhancementSubType.DMGFire, 2),
                new(2, EnhancementSubType.DMGFire, 3),
                new(1, EnhancementSubType.DMGFire, 4),
                new(3, EnhancementSubType.DMGIce, 2),
                new(2, EnhancementSubType.DMGIce, 3),
                new(1, EnhancementSubType.DMGIce, 4),
                new(3, EnhancementSubType.DMGPoison, 2),
                new(2, EnhancementSubType.DMGPoison, 3),
                new(1, EnhancementSubType.DMGPoison, 4),

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

        private List<BlueprintBonus> Tier1ArmorBonuses()
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
            
                // Defense
                new(10, EnhancementSubType.DefensePhysical, 1),
                new(5, EnhancementSubType.DefensePhysical, 2),
                new(10, EnhancementSubType.DefenseForce, 1),
                new(5, EnhancementSubType.DefenseForce, 2),
                new(10, EnhancementSubType.DefenseElectrical, 2),
                new(5, EnhancementSubType.DefenseElectrical, 3),
                new(10, EnhancementSubType.DefenseFire, 2),
                new(5, EnhancementSubType.DefenseFire, 3),
                new(10, EnhancementSubType.DefenseIce, 2),
                new(5, EnhancementSubType.DefenseIce, 3),
                new(10, EnhancementSubType.DefensePoison, 2),
                new(5, EnhancementSubType.DefensePoison, 3),
            
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
            
                // Recast Reduction
                new(5, EnhancementSubType.RecastReduction, 1),
                new(1, EnhancementSubType.RecastReduction, 2),
            
                // STM
                new(15, EnhancementSubType.Stamina, 1),
                new(10, EnhancementSubType.Stamina, 2),
                new(5, EnhancementSubType.Stamina, 3),
                new(2, EnhancementSubType.Stamina, 4),
            };

            
            

            return list;
        }

        private List<BlueprintBonus> Tier2ArmorBonuses()
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
            
                // Defense
                new(15, EnhancementSubType.DefensePhysical, 1),
                new(10, EnhancementSubType.DefensePhysical, 2),
                new(5, EnhancementSubType.DefensePhysical, 3),
                new(15, EnhancementSubType.DefenseForce, 1),
                new(10, EnhancementSubType.DefenseForce, 2),
                new(5, EnhancementSubType.DefenseForce, 3),
                new(15, EnhancementSubType.DefenseElectrical, 2),
                new(10, EnhancementSubType.DefenseElectrical, 3),
                new(5, EnhancementSubType.DefenseElectrical, 4),
                new(15, EnhancementSubType.DefenseFire, 2),
                new(10, EnhancementSubType.DefenseFire, 3),
                new(5, EnhancementSubType.DefenseFire, 4),
                new(15, EnhancementSubType.DefenseIce, 2),
                new(10, EnhancementSubType.DefenseIce, 3),
                new(5, EnhancementSubType.DefenseIce, 4),
                new(15, EnhancementSubType.DefensePoison, 2),
                new(10, EnhancementSubType.DefensePoison, 3),
                new(5, EnhancementSubType.DefensePoison, 4),
            
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
            
                // Recast Reduction
                new(5, EnhancementSubType.RecastReduction, 2),
                new(1, EnhancementSubType.RecastReduction, 3),
            
                // STM
                new(15, EnhancementSubType.Stamina, 2),
                new(10, EnhancementSubType.Stamina, 3),
                new(5, EnhancementSubType.Stamina, 4),
                new(2, EnhancementSubType.Stamina, 5),
            };


            return list;
        }

        private List<BlueprintBonus> Tier3ArmorBonuses()
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
            
                // Defense
                new(15, EnhancementSubType.DefensePhysical, 2),
                new(10, EnhancementSubType.DefensePhysical, 3),
                new(5, EnhancementSubType.DefensePhysical, 4),
                new(15, EnhancementSubType.DefenseForce, 2),
                new(10, EnhancementSubType.DefenseForce, 3),
                new(5, EnhancementSubType.DefenseForce, 4),
                new(15, EnhancementSubType.DefenseElectrical, 3),
                new(10, EnhancementSubType.DefenseElectrical, 4),
                new(5, EnhancementSubType.DefenseElectrical, 5),
                new(15, EnhancementSubType.DefenseFire, 3),
                new(10, EnhancementSubType.DefenseFire, 4),
                new(5, EnhancementSubType.DefenseFire, 5),
                new(15, EnhancementSubType.DefenseIce, 3),
                new(10, EnhancementSubType.DefenseIce, 4),
                new(5, EnhancementSubType.DefenseIce, 5),
                new(15, EnhancementSubType.DefensePoison, 3),
                new(10, EnhancementSubType.DefensePoison, 4),
                new(5, EnhancementSubType.DefensePoison, 5),
            
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
            
                // Recast Reduction
                new(5, EnhancementSubType.RecastReduction, 3),
                new(1, EnhancementSubType.RecastReduction, 4),
            
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

                // Recast Reduction
                new(10, EnhancementSubType.FoodBonusRecastReduction, 3),
                new(5, EnhancementSubType.FoodBonusRecastReduction, 4),

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

                // Recast Reduction
                new(15, EnhancementSubType.FoodBonusRecastReduction, 3),
                new(10, EnhancementSubType.FoodBonusRecastReduction, 4),
                new(5, EnhancementSubType.FoodBonusRecastReduction, 5),

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

                // Recast Reduction
                new(15, EnhancementSubType.FoodBonusRecastReduction, 4),
                new(10, EnhancementSubType.FoodBonusRecastReduction, 5),
                new(5, EnhancementSubType.FoodBonusRecastReduction, 6),

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
            };

            return list;
        }
    }
}
