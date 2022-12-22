using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    public static class Craft
    {
        private static readonly GuiColor _white = new GuiColor(255, 255, 255);
        private static readonly GuiColor _green = new GuiColor(0, 255, 0);
        private static readonly GuiColor _red = new GuiColor(255, 0, 0);
        private static readonly GuiColor _cyan = new GuiColor(0, 255, 255);

        private static readonly Dictionary<RecipeType, RecipeDetail> _recipes = new();
        private static readonly Dictionary<RecipeCategoryType, RecipeCategoryAttribute> _allCategories = new();
        private static readonly Dictionary<RecipeCategoryType, RecipeCategoryAttribute> _activeCategories = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeType, RecipeDetail>> _recipesBySkill = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, Dictionary<RecipeType, RecipeDetail>>> _recipesBySkillAndCategory = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, RecipeCategoryAttribute>> _categoriesBySkill = new();

        private static readonly RecipeLevelChart _levelChart = new();
        private static readonly HashSet<string> _componentResrefs = new();

        /// <summary>
        /// When the skill cache has finished loading, recipe and category data is cached.
        /// </summary>
        [NWNEventHandler("swlor_skl_cache")]
        public static void CacheData()
        {
            CacheCategories();
            CacheRecipes();

            Console.WriteLine($"Loaded {_recipes.Count} recipes.");
        }

        /// <summary>
        /// When the module loads, all recipe categories are loaded into the cache.
        /// </summary>
        private static void CacheCategories()
        {
            var categories = Enum.GetValues(typeof(RecipeCategoryType)).Cast<RecipeCategoryType>();
            foreach (var category in categories)
            {
                var categoryDetail = category.GetAttribute<RecipeCategoryType, RecipeCategoryAttribute>();
                _allCategories[category] = categoryDetail;

                if (categoryDetail.IsActive)
                {
                    _activeCategories[category] = categoryDetail;
                }
            }
        }

        /// <summary>
        /// When the module loads, all recipes are loaded into the cache.
        /// </summary>
        private static void CacheRecipes()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IRecipeListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IRecipeListDefinition)Activator.CreateInstance(type);
                var recipes = instance.BuildRecipes();

                foreach (var (recipeType, recipe) in recipes)
                {
                    if (_recipes.ContainsKey(recipeType))
                    {
                        Log.Write(LogGroup.Error, $"ERROR: Duplicate recipe detected: {recipeType}", true);
                        continue;
                    }

                    _recipes[recipeType] = recipe;

                    // Organize recipes by skill.
                    if (!_recipesBySkill.ContainsKey(recipe.Skill))
                        _recipesBySkill[recipe.Skill] = new Dictionary<RecipeType, RecipeDetail>();
                    _recipesBySkill[recipe.Skill][recipeType] = recipe;

                    // Organize recipe by skill and category.
                    if(!_recipesBySkillAndCategory.ContainsKey(recipe.Skill))
                        _recipesBySkillAndCategory[recipe.Skill] = new Dictionary<RecipeCategoryType, Dictionary<RecipeType, RecipeDetail>>();

                    if(!_recipesBySkillAndCategory[recipe.Skill].ContainsKey(recipe.Category))
                        _recipesBySkillAndCategory[recipe.Skill][recipe.Category] = new Dictionary<RecipeType, RecipeDetail>();

                    _recipesBySkillAndCategory[recipe.Skill][recipe.Category][recipeType] = recipe;

                    // Organize categories by skill based on whether there are any recipes under that category.
                    if (recipe.IsActive)
                    {
                        if (!_categoriesBySkill.ContainsKey(recipe.Skill))
                            _categoriesBySkill[recipe.Skill] = new Dictionary<RecipeCategoryType, RecipeCategoryAttribute>();

                        if (!_categoriesBySkill[recipe.Skill].ContainsKey(recipe.Category))
                            _categoriesBySkill[recipe.Skill][recipe.Category] = _allCategories[recipe.Category];

                        // Cache the resrefs into a hashset for later use in determining if an item is a component
                        foreach (var (resref, _) in recipe.Components)
                        {
                            if (!_componentResrefs.Contains(resref))
                                _componentResrefs.Add(resref);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the details about a recipe.
        /// If recipe type has not been registered, an exception will be raised.
        /// </summary>
        /// <param name="recipeType">The type of recipe to retrieve.</param>
        /// <returns>The recipe detail.</returns>
        public static RecipeDetail GetRecipe(RecipeType recipeType)
        {
            return _recipes[recipeType];
        }

        /// <summary>
        /// Returns true if a recipe has been registered for this type.
        /// </summary>
        /// <param name="recipeType">The type of recipe to look for.</param>
        /// <returns>true if recipe has been registered, false otherwise.</returns>
        public static bool RecipeExists(RecipeType recipeType)
        {
            return _recipes.ContainsKey(recipeType);
        }

        /// <summary>
        /// Retrieves all of the registered recipe categories.
        /// </summary>
        /// <returns>A dictionary containing all registered categories.</returns>
        public static Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetAllCategories()
        {
            return _activeCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Determines if an item is a recipe.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a recipe, false otherwise</returns>
        public static bool IsItemRecipe(uint item)
        {
            var tag = GetTag(item).ToLower();

            return tag == "recipe";
        }

        /// <summary>
        /// Determines if an item is a crafting component.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a crafting component, false otherwise</returns>
        public static bool IsItemComponent(uint item)
        {
            var resref = GetResRef(item);
            return _componentResrefs.Contains(resref);
        }

        /// <summary>
        /// Determines if an item is an enhancement used in crafting.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is an enhancement, false otherwise</returns>
        public static bool IsItemEnhancement(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.ArmorEnhancement ||
                    type == ItemPropertyType.WeaponEnhancement ||
                    type == ItemPropertyType.StructureEnhancement ||
                    type == ItemPropertyType.FoodEnhancement ||
                    type == ItemPropertyType.StarshipEnhancement ||
                    type == ItemPropertyType.ModuleEnhancement)
                {
                    return true;
                }
            }

            return false;
        }

        public static Dictionary<RecipeType, RecipeDetail> GetAllRecipes()
        {
            return _recipes;
        }

        public static Dictionary<RecipeType, RecipeDetail> GetAllRecipesBySkill(SkillType skill)
        {
            return _recipesBySkill[skill];
        }

        /// <summary>
        /// Retrieves all of the recipes associated with a skill and category.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of recipes under a given skill and category.</returns>
        public static Dictionary<RecipeType, RecipeDetail> GetRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category)
        {
            if(!_recipesBySkillAndCategory.ContainsKey(skill))
                return new Dictionary<RecipeType, RecipeDetail>();

            if(!_recipesBySkillAndCategory[skill].ContainsKey(category))
                return new Dictionary<RecipeType, RecipeDetail>();

            return _recipesBySkillAndCategory[skill][category].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves all of the categories listed under a specific skill.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <returns>A list of recipe categories associated with a skill.</returns>
        public static Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetRecipeCategoriesBySkill(SkillType skill)
        {
            if(!_categoriesBySkill.ContainsKey(skill))
                return new Dictionary<RecipeCategoryType, RecipeCategoryAttribute>();

            return _categoriesBySkill[skill].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// When a crafting device is used, display the recipe menu.
        /// </summary>
        [NWNEventHandler("craft_on_used")]
        public static void UseCraftingDevice()
        {
            var player = GetLastUsedBy();
            var skillType = (SkillType)GetLocalInt(OBJECT_SELF, "CRAFTING_SKILL_TYPE_ID");
            var payload = new RecipesPayload(skillType);
            Gui.TogglePlayerWindow(player, GuiWindowType.Recipes, payload, OBJECT_SELF);
        }
        
        /// <summary>
        /// Builds a recipe's detail for use within the NUI window.
        /// </summary>
        /// <param name="player">The player to build for.</param>
        /// <param name="recipe">The recipe to build.</param>
        public static (GuiBindingList<string>, GuiBindingList<GuiColor>) BuildRecipeDetail(uint player, RecipeType recipe)
        {
            var detail = GetRecipe(recipe);
            var recipeDetails = new GuiBindingList<string>();
            var recipeDetailColors = new GuiBindingList<GuiColor>();

            recipeDetails.Add("[COMPONENTS]");
            recipeDetailColors.Add(_cyan);
            foreach (var (resref, quantity) in detail.Components)
            {
                var componentName = Cache.GetItemNameByResref(resref);
                recipeDetails.Add($"{quantity}x {componentName}");
                recipeDetailColors.Add(_white);
            }

            recipeDetails.Add(string.Empty);
            recipeDetailColors.Add(_green);

            recipeDetails.Add("[REQUIREMENTS]");
            recipeDetailColors.Add(_cyan);
            foreach (var req in detail.Requirements)
            {
                recipeDetails.Add(req.RequirementText);
                recipeDetailColors.Add(string.IsNullOrWhiteSpace(req.CheckRequirements(player))
                    ? _green
                    : _red);
            }

            recipeDetails.Add(string.Empty);
            recipeDetailColors.Add(_green);

            recipeDetails.Add("[PROPERTIES]");
            recipeDetailColors.Add(_cyan);
            var tempStorage = GetObjectByTag("TEMP_ITEM_STORAGE");
            var item = CreateItemOnObject(detail.Resref, tempStorage);
            
            foreach (var ip in Item.BuildItemPropertyList(item))
            {
                recipeDetails.Add(ip);
                recipeDetailColors.Add(_white);
            }
            
            DestroyObject(item);

            return (recipeDetails, recipeDetailColors);
        }

        /// <summary>
        /// Determines whether a player can craft a specific recipe.
        /// This does not account for whether the player actually has the required items in their inventory.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="recipeType">The recipe to check</param>
        /// <returns>true if the player can craft the recipe, false otherwise</returns>
        public static bool CanPlayerCraftRecipe(uint player, RecipeType recipeType)
        {
            var recipe = GetRecipe(recipeType);
            if (recipe.Requirements.Count <= 0) return true;

            foreach (var requirement in recipe.Requirements)
            {
                if (!string.IsNullOrWhiteSpace(requirement.CheckRequirements(player)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Retrieves a recipe's level detail by the given level number.
        /// </summary>
        /// <param name="level">The level to search by.</param>
        /// <returns>A recipe level detail.</returns>
        public static RecipeLevelDetail GetRecipeLevelDetail(int level)
        {
            return _levelChart.GetByLevel(level);
        }

        /// <summary>
        /// Builds an item property for a given enhancement type.
        /// </summary>
        /// <param name="subTypeId">The sub type of the enhancement</param>
        /// <param name="amount">The amount to apply.</param>
        /// <returns></returns>
        public static ItemProperty BuildItemPropertyForEnhancement(int subTypeId, int amount)
        {
            switch (subTypeId)
            {
                case 1: // Defense - Physical
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Physical, amount);
                case 2: // Defense - Force
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Force, amount);
                case 3: // Defense - Fire
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Fire, amount);
                case 4: // Defense - Poison
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Poison, amount);
                case 5: // Defense - Electrical
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Electrical, amount);
                case 6: // Defense - Ice
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Ice, amount);
                case 7: // Evasion
                    return ItemPropertyCustom(ItemPropertyType.Evasion, -1, amount);
                case 8: // HP
                    return ItemPropertyCustom(ItemPropertyType.HPBonus, -1, amount);
                case 9: // FP
                    return ItemPropertyCustom(ItemPropertyType.FPBonus, -1, amount);
                case 10: // Stamina
                    return ItemPropertyCustom(ItemPropertyType.STMBonus, -1, amount);
                case 11: // Vitality
                    return ItemPropertyAbilityBonus(AbilityType.Vitality, amount);
                case 12: // Social
                    return ItemPropertyAbilityBonus(AbilityType.Social, amount);
                case 13: // Willpower
                    return ItemPropertyAbilityBonus(AbilityType.Willpower, amount);
                case 14: // Control - Smithery
                    return ItemPropertyCustom(ItemPropertyType.Control, 1, amount);
                case 15: // Craftsmanship - Smithery
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, 1, amount);
                
                // 16 and 17 are applied within the view model, as they are not actually item properties.
                
                case 18: // DMG - Physical
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, amount);
                case 19: // DMG - Force
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Force, amount);
                case 20: // DMG - Fire
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Fire, amount);
                case 21: // DMG - Poison
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Poison, amount);
                case 22: // DMG - Electrical
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Electrical, amount);
                case 23: // DMG - Ice
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Ice, amount);
                case 24: // Might
                    return ItemPropertyAbilityBonus(AbilityType.Might, amount);
                case 25: // Perception
                    return ItemPropertyAbilityBonus(AbilityType.Perception, amount);
                case 26: // Accuracy
                    return ItemPropertyAttackBonus(amount);
                case 27: // Recast Reduction
                    return ItemPropertyCustom(ItemPropertyType.AbilityRecastReduction, -1, amount);
                case 28: // Structure Bonus
                    return ItemPropertyCustom(ItemPropertyType.StructureBonus, -1, amount);
                case 29: // Food Bonus - HP Regen
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.HPRegen, amount);
                case 30: // Food Bonus - FP Regen
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.FPRegen, amount);
                case 31: // Food Bonus - STM Regen
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.STMRegen, amount);
                case 32: // Food Bonus - Rest Regen
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.RestRegen, amount);
                case 33: // Food Bonus - XP Bonus
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.XPBonus, amount);
                case 34: // Food Bonus - Recast Reduction
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.RecastReduction, amount);
                case 35: // Food Bonus - Duration
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Duration, amount);
                case 36: // Food Bonus - HP
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.HP, amount);
                case 37: // Food Bonus - FP
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.FP, amount);
                case 38: // Food Bonus - STM
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.STM, amount);
                case 39: // Control - Engineering
                    return ItemPropertyCustom(ItemPropertyType.Control, 2, amount);
                case 40: // Craftsmanship - Engineering
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, 2, amount);
                case 41: // Control - Fabrication
                    return ItemPropertyCustom(ItemPropertyType.Control, 3, amount);
                case 42: // Craftsmanship - Fabrication
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, 3, amount);
                case 43: // Control - Agriculture
                    return ItemPropertyCustom(ItemPropertyType.Control, 4, amount);
                case 44: // Craftsmanship - Agriculture
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, 4, amount);
                case 45: // Module Bonus
                    return ItemPropertyCustom(ItemPropertyType.ModuleBonus, -1, amount);
                case 46: // Starship Hull
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 46, amount);
                case 47: // Starship Capacitor
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 47, amount);
                case 48: // Starship Shield
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 48, amount);
                case 49: // Starship Shield Recharge Rate
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 49, amount);
                case 50: // Starship EM Damage
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 50, amount);
                case 51: // Starship Thermal Damage
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 51, amount);
                case 52: // Starship Explosive Damage
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 52, amount);
                case 53: // Starship Accuracy
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 53, amount);
                case 54: // Starship Evasion
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 54, amount);
                case 55: // Starship Thermal Defense
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 55, amount);
                case 56: // Starship Explosive Defense
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 56, amount);
                case 57: // Starship EM Defense
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 57, amount);
                case 58: // Agility
                    return ItemPropertyAbilityBonus(AbilityType.Agility, amount);
                case 59: // Attack
                    return ItemPropertyCustom(ItemPropertyType.Attack, -1, amount);
                case 60: // Food Bonus - Attack
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Attack, amount);
                case 61: // Food Bonus - Accuracy
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Accuracy, amount);
                case 62: // Food Bonus - Physical Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefensePhysical, amount);
                case 63: // Food Bonus - Force Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseForce, amount);
                case 64: // Food Bonus - Poison Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefensePoison, amount);
                case 65: // Food Bonus - Fire Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseFire, amount);
                case 66: // Food Bonus - Ice Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseIce, amount);
                case 67: // Food Bonus - Electrical Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseElectrical, amount);
                case 68: // Food Bonus - Evasion
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Evasion, amount);
                case 69: // Food Bonus - Control Smithery
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlSmithery, amount);
                case 70: // Food Bonus - Craftsmanship Smithery
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipSmithery, amount);
                case 71: // Food Bonus - Control Fabrication
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlFabrication, amount);
                case 72: // Food Bonus - Craftsmanship Fabrication
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipFabrication, amount);
                case 73: // Food Bonus - Control Engineering
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlEngineering, amount);
                case 74: // Food Bonus - Craftsmanship Engineering
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipEngineering, amount);
                case 75: // Food Bonus - Control Agriculture
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlAgriculture, amount);
                case 76: // Food Bonus - Craftsmanship Agriculture
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipAgriculture, amount);
                case 77: // Food Bonus - Might
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Might, amount);
                case 78: // Food Bonus - Perception
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Perception, amount);
                case 79: // Food Bonus - Vitality
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Vitality, amount);
                case 80: // Food Bonus - Willpower
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Willpower, amount);
                case 81: // Food Bonus - Agility
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Agility, amount);
                case 82: // Food Bonus - Social
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Social, amount);

            }

            throw new Exception("Unsupported enhancement type.");
        }

        [NWNEventHandler("refinery_used")]
        public static void UseRefinery()
        {
            var player = GetLastUsedBy();
            Gui.TogglePlayerWindow(player, GuiWindowType.Refinery, null, OBJECT_SELF);
        }

    }
}
