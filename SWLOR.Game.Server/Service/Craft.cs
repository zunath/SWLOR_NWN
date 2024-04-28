using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
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
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    public static class Craft
    {
        private static readonly Dictionary<RecipeType, RecipeDetail> _recipes = new();
        private static readonly Dictionary<RecipeCategoryType, RecipeCategoryAttribute> _allCategories = new();
        private static readonly Dictionary<RecipeCategoryType, RecipeCategoryAttribute> _activeCategories = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeType, RecipeDetail>> _recipesBySkill = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, Dictionary<RecipeType, RecipeDetail>>> _recipesBySkillAndCategory = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, RecipeCategoryAttribute>> _categoriesBySkill = new();
        private static readonly Dictionary<EnhancementSubType, EnhancementSubTypeAttribute> _enhancementSubTypes = new();
        
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
            CacheEnhancementSubTypes();
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
            
            Console.WriteLine($"Loaded {_allCategories.Count} recipe category types.");
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
            
            Console.WriteLine($"Loaded {_recipes.Count} recipes.");
        }

        private static void CacheEnhancementSubTypes()
        {
            var subTypes = Enum.GetValues(typeof(EnhancementSubType)).Cast<EnhancementSubType>();
            foreach (var type in subTypes)
            {
                var detail = type.GetAttribute<EnhancementSubType, EnhancementSubTypeAttribute>();
                _enhancementSubTypes[type] = detail;
            }
            
            Console.WriteLine($"Loaded {_enhancementSubTypes.Count} enhancement sub types.");
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
        /// Retrieves the details about an enhancement subtype.
        /// If the enhancement subtype has not been registered, an exception will be raised.
        /// </summary>
        /// <param name="subType">The subtype of the enhancement.</param>
        /// <returns>The enhancement subtype detail.</returns>
        public static EnhancementSubTypeAttribute GetEnhancementSubType(EnhancementSubType subType)
        {
            return _enhancementSubTypes[subType];
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
        /// <param name="blueprint">The blueprint details. null if not a blueprint</param>
        public static (GuiBindingList<string>, GuiBindingList<GuiColor>) BuildRecipeDetail(uint player, RecipeType recipe, BlueprintDetail blueprint)
        {
            var detail = GetRecipe(recipe);
            var recipeDetails = new GuiBindingList<string>();
            var recipeDetailColors = new GuiBindingList<GuiColor>();

            recipeDetails.Add("[COMPONENTS]");
            recipeDetailColors.Add(GuiColor.Cyan);
            foreach (var (resref, quantity) in detail.Components)
            {
                var componentName = Cache.GetItemNameByResref(resref);
                recipeDetails.Add($"{quantity}x {componentName}");
                recipeDetailColors.Add(GuiColor.White);
            }

            recipeDetails.Add(string.Empty);
            recipeDetailColors.Add(GuiColor.Green);

            recipeDetails.Add("[REQUIREMENTS]");
            recipeDetailColors.Add(GuiColor.Cyan);
            foreach (var req in detail.Requirements)
            {
                recipeDetails.Add(req.RequirementText);
                recipeDetailColors.Add(string.IsNullOrWhiteSpace(req.CheckRequirements(player))
                    ? GuiColor.Green
                    : GuiColor.Red);
            }

            recipeDetails.Add(string.Empty);
            recipeDetailColors.Add(GuiColor.Green);

            recipeDetails.Add("[PROPERTIES]");
            recipeDetailColors.Add(GuiColor.Cyan);
            var tempStorage = GetObjectByTag("TEMP_ITEM_STORAGE");
            var item = CreateItemOnObject(detail.Resref, tempStorage);
            
            foreach (var ip in Item.BuildItemPropertyList(item))
            {
                recipeDetails.Add(ip);
                recipeDetailColors.Add(GuiColor.White);
            }
            
            DestroyObject(item);

            recipeDetails.Add(string.Empty);
            recipeDetailColors.Add(GuiColor.White);
            
            if (blueprint != null && blueprint.Recipe != RecipeType.Invalid)
            {
                recipeDetails.Add("[BLUEPRINT]");
                recipeDetailColors.Add(GuiColor.Cyan);

                if (blueprint.Level == 0)
                {
                    recipeDetails.Add("No Bonuses");
                    recipeDetailColors.Add(GuiColor.White);
                }
                else
                {
                    if (blueprint.LicensedRuns > 0)
                    {
                        recipeDetails.Add($"Licensed Runs x{blueprint.LicensedRuns}");
                        recipeDetailColors.Add(GuiColor.White);
                    }
                    if (blueprint.ItemBonuses > 0)
                    {
                        recipeDetails.Add($"Stat Bonus x{blueprint.ItemBonuses}");
                        recipeDetailColors.Add(GuiColor.White);
                    }

                    if (blueprint.CreditReduction > 0)
                    {
                        recipeDetails.Add($"Price Reduced -{blueprint.CreditReduction}%");
                        recipeDetailColors.Add(GuiColor.White);
                    }

                    if (blueprint.EnhancementSlots > 0)
                    {
                        recipeDetails.Add($"Bonus Enhancements x{blueprint.EnhancementSlots}");
                        recipeDetailColors.Add(GuiColor.White);
                    }
                }
                
            }
            
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
        public static ItemProperty BuildItemPropertyForEnhancement(EnhancementSubType subTypeId, int amount)
        {
            switch (subTypeId)
            {
                case EnhancementSubType.DefensePhysical: // Defense - Physical
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Physical, amount);
                case EnhancementSubType.DefenseForce: // Defense - Force
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Force, amount);
                case EnhancementSubType.DefenseFire: // Defense - Fire
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Fire, amount);
                case EnhancementSubType.DefensePoison: // Defense - Poison
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Poison, amount);
                case EnhancementSubType.DefenseElectrical: // Defense - Electrical
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Electrical, amount);
                case EnhancementSubType.DefenseIce: // Defense - Ice
                    return ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Ice, amount);
                case EnhancementSubType.Evasion: // Evasion
                    return ItemPropertyCustom(ItemPropertyType.Evasion, -1, amount);
                case EnhancementSubType.HP: // HP
                    return ItemPropertyCustom(ItemPropertyType.HPBonus, -1, amount);
                case EnhancementSubType.FP: // FP
                    return ItemPropertyCustom(ItemPropertyType.FP, -1, amount);
                case EnhancementSubType.Stamina: // Stamina
                    return ItemPropertyCustom(ItemPropertyType.Stamina, -1, amount);
                case EnhancementSubType.Vitality: // Vitality
                    return ItemPropertyAbilityBonus(AbilityType.Vitality, amount);
                case EnhancementSubType.Social: // Social
                    return ItemPropertyAbilityBonus(AbilityType.Social, amount);
                case EnhancementSubType.Willpower: // Willpower
                    return ItemPropertyAbilityBonus(AbilityType.Willpower, amount);
                case EnhancementSubType.ControlSmithery: // Control - Smithery
                    return ItemPropertyCustom(ItemPropertyType.Control, 1, amount);
                case EnhancementSubType.CraftsmanshipSmithery: // Craftsmanship - Smithery
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, 1, amount);
                
                // 16 and 17 are applied within the view model, as they are not actually item properties.
                
                case EnhancementSubType.DMGPhysical: // DMG - Physical
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, amount);
                case EnhancementSubType.DMGForce: // DMG - Force
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Force, amount);
                case EnhancementSubType.DMGFire: // DMG - Fire
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Fire, amount);
                case EnhancementSubType.DMGPoison: // DMG - Poison
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Poison, amount);
                case EnhancementSubType.DMGElectrical: // DMG - Electrical
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Electrical, amount);
                case EnhancementSubType.DMGIce: // DMG - Ice
                    return ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Ice, amount);
                case EnhancementSubType.Might: // Might
                    return ItemPropertyAbilityBonus(AbilityType.Might, amount);
                case EnhancementSubType.Perception: // Perception
                    return ItemPropertyAbilityBonus(AbilityType.Perception, amount);
                case EnhancementSubType.Accuracy: // Accuracy
                    return ItemPropertyAttackBonus(amount);
                case EnhancementSubType.RecastReduction: // Recast Reduction
                    return ItemPropertyCustom(ItemPropertyType.AbilityRecastReduction, -1, amount);
                case EnhancementSubType.StructureBonus: // Structure Bonus
                    return ItemPropertyCustom(ItemPropertyType.StructureBonus, -1, amount);
                case EnhancementSubType.FoodBonusHPRegen: // Food Bonus - HP Regen
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.HPRegen, amount);
                case EnhancementSubType.FoodBonusFPRegen: // Food Bonus - FP Regen
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.FPRegen, amount);
                case EnhancementSubType.FoodBonusSTMRegen: // Food Bonus - STM Regen
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.STMRegen, amount);
                case EnhancementSubType.FoodBonusRestRegen: // Food Bonus - Rest Regen
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.RestRegen, amount);
                case EnhancementSubType.FoodBonusXPBonus: // Food Bonus - XP Bonus
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.XPBonus, amount);
                case EnhancementSubType.FoodBonusRecastReduction: // Food Bonus - Recast Reduction
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.RecastReduction, amount);
                case EnhancementSubType.FoodBonusDuration: // Food Bonus - Duration
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Duration, amount);
                case EnhancementSubType.FoodBonusHP: // Food Bonus - HP
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.HP, amount);
                case EnhancementSubType.FoodBonusFP: // Food Bonus - FP
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.FP, amount);
                case EnhancementSubType.FoodBonusSTM: // Food Bonus - STM
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.STM, amount);
                case EnhancementSubType.ControlEngineering: // Control - Engineering
                    return ItemPropertyCustom(ItemPropertyType.Control, 2, amount);
                case EnhancementSubType.CraftsmanshipEngineering: // Craftsmanship - Engineering
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, 2, amount);
                case EnhancementSubType.ControlFabrication: // Control - Fabrication
                    return ItemPropertyCustom(ItemPropertyType.Control, 3, amount);
                case EnhancementSubType.CraftsmanshipFabrication: // Craftsmanship - Fabrication
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, 3, amount);
                case EnhancementSubType.ControlAgriculture: // Control - Agriculture
                    return ItemPropertyCustom(ItemPropertyType.Control, 4, amount);
                case EnhancementSubType.CraftsmanshipAgriculture: // Craftsmanship - Agriculture
                    return ItemPropertyCustom(ItemPropertyType.Craftsmanship, 4, amount);
                case EnhancementSubType.ModuleBonus: // Module Bonus
                    return ItemPropertyCustom(ItemPropertyType.ModuleBonus, -1, amount);
                case EnhancementSubType.StarshipHull: // Starship Hull
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 46, amount);
                case EnhancementSubType.StarshipCapacitor: // Starship Capacitor
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 47, amount);
                case EnhancementSubType.StarshipShield: // Starship Shield
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 48, amount);
                case EnhancementSubType.StarshipShieldRechargeRate: // Starship Shield Recharge Rate
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 49, amount);
                case EnhancementSubType.StarshipEMDamage: // Starship EM Damage
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 50, amount);
                case EnhancementSubType.StarshipThermalDamage: // Starship Thermal Damage
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 51, amount);
                case EnhancementSubType.StarshipExplosiveDamage: // Starship Explosive Damage
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 52, amount);
                case EnhancementSubType.StarshipAccuracy: // Starship Accuracy
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 53, amount);
                case EnhancementSubType.StarshipEvasion: // Starship Evasion
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 54, amount);
                case EnhancementSubType.StarshipThermalDefense: // Starship Thermal Defense
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 55, amount);
                case EnhancementSubType.StarshipExplosiveDefense: // Starship Explosive Defense
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 56, amount);
                case EnhancementSubType.StarshipEMDefense: // Starship EM Defense
                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 57, amount);
                case EnhancementSubType.Agility: // Agility
                    return ItemPropertyAbilityBonus(AbilityType.Agility, amount);
                
                // 59 is free
                
                case EnhancementSubType.FoodBonusAttack: // Food Bonus - Attack
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Attack, amount);
                case EnhancementSubType.FoodBonusAccuracy: // Food Bonus - Accuracy
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Accuracy, amount);
                case EnhancementSubType.FoodBonusPhysicalDefense: // Food Bonus - Physical Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefensePhysical, amount);
                case EnhancementSubType.FoodBonusForceDefense: // Food Bonus - Force Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseForce, amount);
                case EnhancementSubType.FoodBonusPoisonDefense: // Food Bonus - Poison Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefensePoison, amount);
                case EnhancementSubType.FoodBonusFireDefense: // Food Bonus - Fire Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseFire, amount);
                case EnhancementSubType.FoodBonusIceDefense: // Food Bonus - Ice Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseIce, amount);
                case EnhancementSubType.FoodBonusElectricalDefense: // Food Bonus - Electrical Defense
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseElectrical, amount);
                case EnhancementSubType.FoodBonusEvasion: // Food Bonus - Evasion
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Evasion, amount);
                case EnhancementSubType.FoodBonusControlSmithery: // Food Bonus - Control Smithery
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlSmithery, amount);
                case EnhancementSubType.FoodBonusCraftsmanshipSmithery: // Food Bonus - Craftsmanship Smithery
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipSmithery, amount);
                case EnhancementSubType.FoodBonusControlFabrication: // Food Bonus - Control Fabrication
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlFabrication, amount);
                case EnhancementSubType.FoodBonusCraftsmanshipFabrication: // Food Bonus - Craftsmanship Fabrication
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipFabrication, amount);
                case EnhancementSubType.FoodBonusControlEngineering: // Food Bonus - Control Engineering
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlEngineering, amount);
                case EnhancementSubType.FoodBonusCraftsmanshipEngineering: // Food Bonus - Craftsmanship Engineering
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipEngineering, amount);
                case EnhancementSubType.FoodBonusControlAgriculture: // Food Bonus - Control Agriculture
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlAgriculture, amount);
                case EnhancementSubType.FoodBonusCraftsmanshipAgriculture: // Food Bonus - Craftsmanship Agriculture
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipAgriculture, amount);
                case EnhancementSubType.FoodBonusMight: // Food Bonus - Might
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Might, amount);
                case EnhancementSubType.FoodBonusPerception: // Food Bonus - Perception
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Perception, amount);
                case EnhancementSubType.FoodBonusVitality: // Food Bonus - Vitality
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Vitality, amount);
                case EnhancementSubType.FoodBonusWillpower: // Food Bonus - Willpower
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Willpower, amount);
                case EnhancementSubType.FoodBonusAgility: // Food Bonus - Agility
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Agility, amount);
                case EnhancementSubType.FoodBonusSocial: // Food Bonus - Social
                    return ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Social, amount);
                case EnhancementSubType.Attack: // Attack
                    return ItemPropertyCustom(ItemPropertyType.Attack, -1, amount);
                case EnhancementSubType.ForceAttack: // Force Attack
                    return ItemPropertyCustom(ItemPropertyType.ForceAttack, -1, amount);

                // 83-101 are free

                case EnhancementSubType.DroidAISlot: // Droid: AI Slot
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 3, amount);
                case EnhancementSubType.DroidHP: // Droid: HP
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 4, amount);
                case EnhancementSubType.DroidSTM: // Droid: STM
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 5, amount);
                case EnhancementSubType.DroidMGT: // Droid: MGT
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 6, amount);
                case EnhancementSubType.DroidPER: // Droid: PER
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 7, amount);
                case EnhancementSubType.DroidVIT: // Droid: VIT
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 8, amount);
                case EnhancementSubType.DroidWIL: // Droid: WIL
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 9, amount);
                case EnhancementSubType.DroidAGI: // Droid: AGI
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 10, amount);
                case EnhancementSubType.DroidSOC: // Droid: SOC
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 11, amount);
                case EnhancementSubType.Droid1Handed: // Droid: 1-Handed
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 12, amount);
                case EnhancementSubType.Droid2Handed: // Droid: 2-Handed
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 13, amount);
                case EnhancementSubType.DroidMartialArts: // Droid: Martial Arts
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 14, amount);
                case EnhancementSubType.DroidRanged: // Droid: Ranged
                    return ItemPropertyCustom(ItemPropertyType.DroidStat, 15, amount);
            }

            throw new Exception("Unsupported enhancement type.");
        }

        [NWNEventHandler("refinery_used")]
        public static void UseRefinery()
        {
            var player = GetLastUsedBy();
            Gui.TogglePlayerWindow(player, GuiWindowType.Refinery, null, OBJECT_SELF);
        }

        /// <summary>
        /// Retrieves a blueprint detail object about an item.
        /// If item is not a blueprint, resulting recipe type will be Invalid.
        /// </summary>
        /// <param name="blueprint">The blueprint item</param>
        /// <returns>A blueprint detail object</returns>
        public static BlueprintDetail GetBlueprintDetails(uint blueprint)
        {
            var blueprintDetail = new BlueprintDetail();
            var recipeId = GetLocalInt(blueprint, "BLUEPRINT_RECIPE_ID");
            blueprintDetail.Recipe = (RecipeType)recipeId;
            blueprintDetail.RandomEnhancementSlotGranted = GetLocalBool(blueprint, "BLUEPRINT_RANDOM_ENHANCEMENT_SLOT_GRANTED");

            for (var ip = GetFirstItemProperty(blueprint); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(blueprint))
            {
                var type = GetItemPropertyType(ip);

                if (type == ItemPropertyType.Blueprint)
                {
                    var subType = GetItemPropertySubType(ip);
                    var costValue = GetItemPropertyCostTableValue(ip);
                    
                    if (subType == (int)BlueprintSubType.Level)
                    {
                        blueprintDetail.Level = costValue;
                    }
                    else if (subType == (int)BlueprintSubType.LicensedRuns)
                    {
                        blueprintDetail.LicensedRuns = costValue;
                    }
                    else if (subType == (int)BlueprintSubType.ItemBonuses)
                    {
                        blueprintDetail.ItemBonuses = costValue;
                    }
                    else if (subType == (int)BlueprintSubType.CreditReduction)
                    {
                        blueprintDetail.CreditReduction = costValue;
                    }
                    else if (subType == (int)BlueprintSubType.TimeReduction)
                    {
                        blueprintDetail.TimeReduction = costValue;
                    }
                    else if (subType == (int)BlueprintSubType.EnhancementSlots)
                    {
                        blueprintDetail.EnhancementSlots = costValue;
                    }
                }
            }

            return blueprintDetail;
        }

        public static void SetBlueprintDetails(uint blueprint, BlueprintDetail blueprintDetail)
        {
            if (blueprintDetail.LicensedRuns <= 0)
            {
                DestroyObject(blueprint);
                return;
            }
            
            SetLocalInt(blueprint, "BLUEPRINT_RECIPE_ID", (int)blueprintDetail.Recipe);
            SetLocalBool(blueprint, "BLUEPRINT_RANDOM_ENHANCEMENT_SLOT_GRANTED", blueprintDetail.RandomEnhancementSlotGranted);
            
            BiowareXP2.IPSafeAddItemProperty(blueprint, ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.Level, blueprintDetail.Level), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(blueprint, ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.LicensedRuns, blueprintDetail.LicensedRuns), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(blueprint, ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.ItemBonuses, blueprintDetail.ItemBonuses), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(blueprint, ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.CreditReduction, blueprintDetail.CreditReduction), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(blueprint, ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.TimeReduction, blueprintDetail.TimeReduction), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(blueprint, ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.EnhancementSlots, blueprintDetail.EnhancementSlots), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            
        }
        
        public static int CalculateBlueprintCraftCreditCost(uint blueprint)
        {
            var blueprintDetail = GetBlueprintDetails(blueprint);
            var recipeDetail = GetRecipe(blueprintDetail.Recipe);
            var creditReduction = blueprintDetail.CreditReduction * 0.01f;
            var perkLevel = recipeDetail.Level / 10;
            if (perkLevel <= 0)
                perkLevel = 1;
            
            var blueprintLevel = blueprintDetail.Level;
            
            const int BaseConstant = 120;
            var price = BaseConstant * (Math.Pow(perkLevel, 2.2f) * Math.Pow(blueprintLevel, 2f));
            price += price * recipeDetail.ResearchCostModifier;
            price -= creditReduction * price;
            
            return (int)price;  
        }
    }
}
