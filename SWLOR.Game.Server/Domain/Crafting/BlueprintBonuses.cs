using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Crafting
{
    internal class BlueprintBonuses
    {
        private readonly Dictionary<RecipeEnhancementType, Dictionary<bool, Dictionary<int, List<BlueprintBonus>>>> _bonusesByEnhancementType = new();

        public BlueprintBonuses()
        {
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon] = new Dictionary<bool, Dictionary<int, List<BlueprintBonus>>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][false] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][false][1] = BlueprintWeaponBonusTables.Tier1WeaponCombatBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][false][2] = BlueprintWeaponBonusTables.Tier2WeaponCombatBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][false][3] = BlueprintWeaponBonusTables.Tier3WeaponCombatBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][true] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][true][1] = BlueprintWeaponBonusTables.Tier1WeaponCraftingBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][true][2] = BlueprintWeaponBonusTables.Tier2WeaponCraftingBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Weapon][true][3] = BlueprintWeaponBonusTables.Tier3WeaponCraftingBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Armor] = new Dictionary<bool, Dictionary<int, List<BlueprintBonus>>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][false] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][false][1] = BlueprintArmorBonusTables.Tier1ArmorCombatBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][false][2] = BlueprintArmorBonusTables.Tier2ArmorCombatBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][false][3] = BlueprintArmorBonusTables.Tier3ArmorCombatBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Armor][true] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][true][1] = BlueprintArmorBonusTables.Tier1ArmorCraftingBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][true][2] = BlueprintArmorBonusTables.Tier2ArmorCraftingBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Armor][true][3] = BlueprintArmorBonusTables.Tier3ArmorCraftingBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Food] = new Dictionary<bool, Dictionary<int, List<BlueprintBonus>>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][false] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][false][1] = BlueprintFoodBonusTables.Tier1FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][false][2] = BlueprintFoodBonusTables.Tier2FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][false][3] = BlueprintFoodBonusTables.Tier3FoodBonuses();

            _bonusesByEnhancementType[RecipeEnhancementType.Food][true] = new Dictionary<int, List<BlueprintBonus>>();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][true][1] = BlueprintFoodBonusTables.Tier1FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][true][2] = BlueprintFoodBonusTables.Tier2FoodBonuses();
            _bonusesByEnhancementType[RecipeEnhancementType.Food][true][3] = BlueprintFoodBonusTables.Tier3FoodBonuses();
        }

        public BlueprintBonus PickBonus(RecipeEnhancementType enhancementType, int tier, bool isCraftingItem)
        {
            if (!_bonusesByEnhancementType.ContainsKey(enhancementType) ||
                !_bonusesByEnhancementType[enhancementType].ContainsKey(isCraftingItem) ||
                !_bonusesByEnhancementType[enhancementType][isCraftingItem].ContainsKey(tier))
                return null;

            var set = _bonusesByEnhancementType[enhancementType][isCraftingItem][tier];
            var weights = set.Select(x => x.Weight).ToArray();
            var index = Domain.Foundation.Random.GetRandomWeightedIndex(weights);

            return set[index];
        }
    }
}
