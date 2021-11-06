﻿using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Craft
    {
        private const string CraftItemResref = "auto_craft_item";
        private const string LoadComponentsResref = "load_components";
        private const string SelectRecipeResref = "select_recipe";

        private static readonly string[] _commandResrefs =
        {
            CraftItemResref,
            LoadComponentsResref,
            SelectRecipeResref
        };

        private static readonly Dictionary<RecipeType, RecipeDetail> _recipes = new();
        private static readonly Dictionary<RecipeCategoryType, RecipeCategoryAttribute> _allCategories = new();
        private static readonly Dictionary<RecipeCategoryType, RecipeCategoryAttribute> _activeCategories = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeType, RecipeDetail>> _recipesBySkill = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, Dictionary<RecipeType, RecipeDetail>>> _recipesBySkillAndCategory = new();
        private static readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, RecipeCategoryAttribute>> _categoriesBySkill = new();

        private static readonly Dictionary<SkillType, Tuple<AbilityType, AbilityType>> _craftSkillToAbility = new();

        private static readonly Dictionary<uint, PlayerCraftingState> _playerCraftingStates = new();
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
        /// Retrieves a player's crafting state.
        /// If no state is found, a new one will be created and returned.
        /// </summary>
        /// <param name="player">The player state to retrieve.</param>
        /// <returns>A player's crafting state</returns>
        public static PlayerCraftingState GetPlayerCraftingState(uint player)
        {
            if(!_playerCraftingStates.ContainsKey(player))
                _playerCraftingStates[player] = new PlayerCraftingState();

            return _playerCraftingStates[player];
        }

        /// <summary>
        /// Removes a player's crafting state from the cache.
        /// Be sure this is called when the player leaves the server or stops crafting.
        /// </summary>
        /// <param name="player">The player to remove state from.</param>
        public static void ClearPlayerCraftingState(uint player)
        {
            if (!_playerCraftingStates.ContainsKey(player))
                return;

            _playerCraftingStates.Remove(player);
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
        /// Handles the crafting procedure.
        /// Success is determined by a player's stats.
        /// An item is created on a successful attempt.
        /// </summary>
        /// <param name="player">The player performing the crafting.</param>
        private static void CraftItem(uint player)
        {
            var state = GetPlayerCraftingState(player);
            var device = OBJECT_SELF;

            float CalculateAutoCraftingDelay()
            {
                return 20f;
            }

            void CraftItem(bool isSuccessful)
            {
                var recipe = GetRecipe(state.SelectedRecipe);

                var playerComponents = GetComponents(player, device);
                var remainingComponents = recipe.Components.ToDictionary(x => x.Key, y => y.Value);

                for (var index = playerComponents.Count - 1; index >= 0; index--)
                {
                    var component = playerComponents[index];
                    var resref = GetResRef(component);

                    // Item does not need any more of this component type.
                    if (!remainingComponents.ContainsKey(resref))
                        continue;

                    var quantity = GetItemStackSize(component);

                    // Player's component stack size is greater than the amount required.
                    if (quantity > remainingComponents[resref])
                    {
                        SetItemStackSize(component, quantity - remainingComponents[resref]);
                        remainingComponents[resref] = 0;
                    }
                    // Player's component stack size is less than or equal to the amount required.
                    else if (quantity <= remainingComponents[resref])
                    {
                        remainingComponents[resref] -= quantity;
                        DestroyObject(component);
                    }

                    if (remainingComponents[resref] <= 0)
                        remainingComponents.Remove(resref);
                }

                if (isSuccessful)
                {
                    CreateItemOnObject(recipe.Resref, player, recipe.Quantity);
                    ExecuteScript("craft_success", player);
                }
            }

            if (!HasAllComponents(player, device))
            {
                SendMessageToPC(player, ColorToken.Red("You are missing some necessary components..."));
                return;
            }

            var craftingDelay = CalculateAutoCraftingDelay();

            state.IsAutoCrafting = true;
            PlayerPlugin.StartGuiTimingBar(player, craftingDelay);
            AssignCommand(player, () => ActionPlayAnimation(Animation.LoopingGetMid, 1f, craftingDelay));
            DelayCommand(craftingDelay, () =>
            {
                // Player logged out.
                if (!GetIsObjectValid(player))
                {
                    ClearPlayerCraftingState(player);
                    return;
                }

                var chanceToCraft = CalculateChanceToCraft(player, state.SelectedRecipe);
                var roll = Random.NextFloat(0f, 100f);

                if (roll <= chanceToCraft)
                {
                    CraftItem(true);
                }
                else
                {
                    CraftItem(false);
                    SendMessageToPC(player, ColorToken.Red("You failed to craft the item..."));
                }

                state.IsAutoCrafting = false;
            });
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), player, craftingDelay);
        }

        /// <summary>
        /// Determines if the player has all of the necessary components for this recipe.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="device">The crafting device</param>
        /// <returns>true if player has all components, false otherwise</returns>
        private static bool HasAllComponents(uint player, uint device)
        {
            var state = GetPlayerCraftingState(player);
            var recipe = GetRecipe(state.SelectedRecipe);
            var remainingComponents = recipe.Components.ToDictionary(x => x.Key, y => y.Value);
            var components = GetComponents(player, device);

            for (var index = components.Count - 1; index >= 0; index--)
            {
                var component = components[index];
                var resref = GetResRef(component);

                // Item does not need any more of this component type.
                if (!remainingComponents.ContainsKey(resref))
                    continue;

                var quantity = GetItemStackSize(component);

                // Player's component stack size is greater than the amount required.
                if (quantity > remainingComponents[resref])
                {
                    remainingComponents[resref] = 0;
                }
                // Player's component stack size is less than or equal to the amount required.
                else if (quantity <= remainingComponents[resref])
                {
                    remainingComponents[resref] -= quantity;
                }

                if (remainingComponents[resref] <= 0)
                    remainingComponents.Remove(resref);
            }

            return remainingComponents.Count <= 0;
        }

        /// <summary>
        /// Retrieves all of the items found on a crafting device which match a recipe's component list.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="device">The crafting device</param>
        /// <returns>A list of item object Ids </returns>
        private static List<uint> GetComponents(uint player, uint device)
        {
            var playerComponents = new List<uint>();
            var model = GetPlayerCraftingState(player);
            var recipe = GetRecipe(model.SelectedRecipe);

            for (var item = GetFirstItemInInventory(device); GetIsObjectValid(item); item = GetNextItemInInventory(device))
            {
                var resref = GetResRef(item);
                if (recipe.Components.ContainsKey(resref))
                    playerComponents.Add(item);
            }

            return playerComponents;
        }

        /// <summary>
        /// Searches a player's inventory for components matching this recipe's requirements.
        /// </summary>
        /// <param name="player">The player to search.</param>
        private static void LoadComponents(uint player)
        {
            var device = OBJECT_SELF;
            var state = GetPlayerCraftingState(player);
            var recipe = GetRecipe(state.SelectedRecipe);

            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                var resref = GetResRef(item);

                if (recipe.Components.ContainsKey(resref))
                {
                    Item.ReturnItem(device, item);
                }
            }
        }

        /// <summary>
        /// Opens the recipe menu so that a player can select a different recipe to create.
        /// </summary>
        /// <param name="player">The player object</param>
        private static void SelectRecipe(uint player)
        {
            var device = OBJECT_SELF;
            var state = GetPlayerCraftingState(player);
            state.IsOpeningMenu = true;

            Dialog.StartConversation(player, device, nameof(RecipeDialog));
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
    }
}
