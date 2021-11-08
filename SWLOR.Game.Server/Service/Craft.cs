using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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

        private static readonly Dictionary<SkillType, Tuple<AbilityType, AbilityType>> _craftSkillToAbility = new();

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
            CacheCraftSkillToAbilities();

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
                        Console.WriteLine($"ERROR: Duplicate recipe detected: {recipeType}");
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
        /// Maps craft skills to the primary/secondary abilities they use during crafting.
        /// </summary>
        private static void CacheCraftSkillToAbilities()
        {
            _craftSkillToAbility[SkillType.Smithery] = new Tuple<AbilityType, AbilityType>(AbilityType.Perception, AbilityType.Might);
            _craftSkillToAbility[SkillType.Fabrication] = new Tuple<AbilityType, AbilityType>(AbilityType.Might, AbilityType.Vitality);
            _craftSkillToAbility[SkillType.FirstAid] = new Tuple<AbilityType, AbilityType>(AbilityType.Vitality, AbilityType.Perception);
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
        /// Retrieves a recipe category's details by a given type.
        /// If the type has not been registered, an exception will be thrown.
        /// </summary>
        /// <param name="categoryType">The type of category to retrieve.</param>
        /// <returns>A recipe category's details.</returns>
        public static RecipeCategoryAttribute GetCategoryDetail(RecipeCategoryType categoryType)
        {
            return _allCategories[categoryType];
        }

        /// <summary>
        /// Calculates a player's chance to craft a specific recipe.
        /// </summary>
        /// <param name="player">The player to calculate for</param>
        /// <param name="recipeType">The type of recipe to calculate for</param>
        /// <returns>A value between 0 and 95 representing the chance to craft an item.</returns>
        public static float CalculateChanceToCraft(uint player, RecipeType recipeType)
        {
            var chance = 60f;
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var recipe = GetRecipe(recipeType);
            var playerLevel = dbPlayer.Skills[recipe.Skill].Rank;
            var (primary, secondary) = _craftSkillToAbility[recipe.Skill];
            var levelDelta = playerLevel - recipe.Level;

            var attributeAdjustment = GetAbilityModifier(primary) * 2.0f + GetAbilityModifier(secondary) * 1.5f;
            var levelAdjustment = levelDelta * 10f;

            chance += levelAdjustment + attributeAdjustment;

            if (chance < 0)
                chance = 0;
            else if (chance >= 95)
                chance = 95;

            return chance;
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
        /// When a player uses an item tagged with "RECIPE", the list of recipes associated with the item
        /// are added to their collection. The item is then destroyed.
        /// </summary>
        [NWNEventHandler("recipe")]
        public static void LearnRecipes()
        {
            var user = OBJECT_SELF;

            if (!GetIsPC(user) || GetIsDM(user))
            {
                SendMessageToPC(user, "Only players may use this item.");
                return;
            }

            var item = StringToObject(EventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            var playerId = GetObjectUUID(user);
            var dbPlayer = DB.Get<Player>(playerId);
            var recipeList = GetLocalString(item, "RECIPES");
            var recipeIds = recipeList.Split(',');
            var recipesLearned = 0;

            foreach (var recipeId in recipeIds)
            {
                // If it fails to parse, exit early.
                if (!int.TryParse(recipeId, out var convertedId))
                {
                    SendMessageToPC(user, "This recipe book has a configuration problem. Please inform a DM.");
                    return;
                }

                // Id number is zero or negative. Skip as those aren't valid.
                if (convertedId <= 0)
                {
                    SendMessageToPC(user, "This recipe book has a configuration problem. Please inform a DM.");
                    return;
                }

                var recipeType = (RecipeType)convertedId;

                // Ensure this type of recipe has been registered.
                if (!RecipeExists(recipeType))
                {
                    SendMessageToPC(user, "This recipe has not been registered. Please inform a DM.");
                    return;
                }

                // Player already knows this recipe. Move to the next one.
                if (dbPlayer.UnlockedRecipes.ContainsKey(recipeType))
                    continue;

                recipesLearned++;
                dbPlayer.UnlockedRecipes[recipeType] = DateTime.UtcNow;

                var recipeDetail = GetRecipe(recipeType);
                var skillDetail = Skill.GetSkillDetails(recipeDetail.Skill);
                var itemName = Cache.GetItemNameByResref(recipeDetail.Resref);
                SendMessageToPC(user, $"You learn the {skillDetail.Name} recipe: {itemName}.");
            }

            // Player didn't learn any recipes. Let them know but don't destroy the item.
            if (recipesLearned <= 0)
            {
                SendMessageToPC(user, "You have already learned all of the recipes contained in this book.");
                return;
            }

            DB.Set(playerId, dbPlayer);
            DestroyObject(item);
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
        /// Determines whether an item is an enhancement of the specified type.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <param name="type">The type to expect.</param>
        /// <returns>true if item is an enhancement of the specified type. false otherwise.</returns>
        public static bool IsItemEnhancement(uint item, RecipeEnhancementType type)
        {
            // todo: check item properties
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
                case 14: // Control
                    return ItemPropertyCustom(ItemPropertyType.Control, -1, amount);
                case 15: // Craftsmanship
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, -1, amount);
                
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
                case 26: // Attack Bonus
                    return ItemPropertyAttackBonus(amount);
            }

            throw new Exception("Unsupported enhancement type.");
        }

    }
}
