using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// Crafting: the recipe, category and enhancement caches, recipe requirement checks, blueprint details and the research cost
    /// formulas. The enhancement item properties are <see cref="CraftEnhancementPropertyService"/>'s, the devices and the property and
    /// login handlers <see cref="CraftDeviceService"/>'s. The logic that used to be a static class, moved here unchanged: the
    /// caches, and the objects the crafting minigames keep (the session and quick craft worlds, the quick craft batches and the
    /// route cache), are this instance's state. The event handlers run on the container's instance; static callers reach it through <c>GameServices.Craft</c>.
    /// </summary>
    public class CraftService : ICraftService
    {
        public const int MaxResearchLevel = 10;
        public const int RecipeSkillUnlockOffset = 3;

        private readonly Dictionary<RecipeType, RecipeDetail> _recipes = new();
        private readonly Dictionary<RecipeCategoryType, RecipeCategoryAttribute> _allCategories = new();
        private readonly Dictionary<RecipeCategoryType, RecipeCategoryAttribute> _activeCategories = new();
        private readonly Dictionary<SkillType, Dictionary<RecipeType, RecipeDetail>> _recipesBySkill = new();
        private readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, Dictionary<RecipeType, RecipeDetail>>> _recipesBySkillAndCategory = new();
        private readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, RecipeCategoryAttribute>> _categoriesBySkill = new();
        private readonly Dictionary<EnhancementSubType, EnhancementSubTypeAttribute> _enhancementSubTypes = new();

        private readonly Dictionary<RecipeType, RecipeDetail> _researchableRecipes = new();
        private readonly Dictionary<SkillType, Dictionary<RecipeType, RecipeDetail>> _researchableRecipesBySkill = new();
        private readonly Dictionary<SkillType, Dictionary<RecipeCategoryType, Dictionary<RecipeType, RecipeDetail>>> _researchableRecipesBySkillAndCategory = new();

        private readonly RecipeLevelChart _levelChart = new();
        private readonly HashSet<string> _componentResrefs = new();

        private readonly ICacheService _cache;
        private readonly IDBService _db;
        private readonly IItemService _item;
        private readonly ICraftEnhancementPropertyService _enhancement;
        private readonly ISkillService _skill;

        public CraftService(
            ICacheService cache,
            IDBService db,
            IItemService item,
            ICraftEnhancementPropertyService enhancement,
            ISkillService skill)
        {
            _cache = cache;
            _db = db;
            _item = item;
            _enhancement = enhancement;
            _skill = skill;
        }

        /// <inheritdoc />
        public NwnCraftSessionWorld SessionWorld { get; } = new();

        /// <inheritdoc />
        public NwnQuickCraftWorld QuickCraftWorld { get; } = new();

        /// <summary>
        /// Puts the craft file sequence the character file loaded with back on the entering player, after the engine has copied the
        /// locals of an earlier login over it. The login checks that read it call <see cref="NwnCraftSessionWorld.LoadedSequence"/>, which
        /// does the same, so they need not run after this.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "Restores the file's craft sequence over a login's copied locals; a player who stays online keeps theirs.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ApplyLoadedCraftSequence()
        {
            SessionWorld.ApplyLoadedSequence(GetEnteringObject());
        }

        /// <summary>
        /// When the skill cache has finished loading, recipe and category data is cached.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorSkillCache)]
        public void CacheData()
        {
            CacheCategories();
            CacheRecipes();
            CacheEnhancementSubTypes();
        }

        /// <summary>
        /// When the module loads, all recipe categories are loaded into the cache.
        /// </summary>
        private void CacheCategories()
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
        private void CacheRecipes()
        {
            void UpdateCraftingStatus(RecipeDetail recipe)
            {
                var tempContainer = GetObjectByTag("TEMP_ITEM_STORAGE");
                var item = CreateItemOnObject(recipe.Resref, tempContainer);

                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    var type = GetItemPropertyType(ip);
                    if (type == ItemPropertyType.Craftsmanship || type == ItemPropertyType.Control)
                    {
                        recipe.IsItemIntendedForCrafting = true;
                    }
                }

                DestroyObject(item);
            }

            var types = GameAssemblies.GetAssemblies()
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

                    var isResearchable = IsResearchableRecipe(recipe);

                    _recipes[recipeType] = recipe;
                    if (isResearchable)
                        _researchableRecipes[recipeType] = recipe;

                    UpdateCraftingStatus(recipe);

                    // Organize recipes by skill.
                    if (!_recipesBySkill.ContainsKey(recipe.Skill))
                        _recipesBySkill[recipe.Skill] = new Dictionary<RecipeType, RecipeDetail>();
                    _recipesBySkill[recipe.Skill][recipeType] = recipe;

                    if (isResearchable)
                    {
                        if (!_researchableRecipesBySkill.ContainsKey(recipe.Skill))
                            _researchableRecipesBySkill[recipe.Skill] = new Dictionary<RecipeType, RecipeDetail>();
                        _researchableRecipesBySkill[recipe.Skill][recipeType] = recipe;
                    }

                    // Organize recipe by skill and category.
                    if(!_recipesBySkillAndCategory.ContainsKey(recipe.Skill))
                        _recipesBySkillAndCategory[recipe.Skill] = new Dictionary<RecipeCategoryType, Dictionary<RecipeType, RecipeDetail>>();

                    if(!_recipesBySkillAndCategory[recipe.Skill].ContainsKey(recipe.Category))
                        _recipesBySkillAndCategory[recipe.Skill][recipe.Category] = new Dictionary<RecipeType, RecipeDetail>();

                    _recipesBySkillAndCategory[recipe.Skill][recipe.Category][recipeType] = recipe;

                    if (isResearchable)
                    {
                        if(!_researchableRecipesBySkillAndCategory.ContainsKey(recipe.Skill))
                            _researchableRecipesBySkillAndCategory[recipe.Skill] = new Dictionary<RecipeCategoryType, Dictionary<RecipeType, RecipeDetail>>();

                        if (!_researchableRecipesBySkillAndCategory[recipe.Skill].ContainsKey(recipe.Category))
                            _researchableRecipesBySkillAndCategory[recipe.Skill][recipe.Category] = new Dictionary<RecipeType, RecipeDetail>();

                        _researchableRecipesBySkillAndCategory[recipe.Skill][recipe.Category][recipeType] = recipe;
                    }


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

        private bool IsResearchableRecipe(RecipeDetail recipe)
        {
            return recipe.EnhancementType == RecipeEnhancementType.Weapon ||
                   recipe.EnhancementType == RecipeEnhancementType.Armor ||
                   recipe.EnhancementType == RecipeEnhancementType.Food;
        }

        private void CacheEnhancementSubTypes()
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
        public RecipeDetail GetRecipe(RecipeType recipeType)
        {
            return _recipes[recipeType];
        }

        /// <summary>
        /// Returns true if a recipe has been registered for this type.
        /// </summary>
        /// <param name="recipeType">The type of recipe to look for.</param>
        /// <returns>true if recipe has been registered, false otherwise.</returns>
        public bool RecipeExists(RecipeType recipeType)
        {
            return _recipes.ContainsKey(recipeType);
        }

        /// <summary>
        /// Retrieves the details about an enhancement subtype.
        /// If the enhancement subtype has not been registered, an exception will be raised.
        /// </summary>
        /// <param name="subType">The subtype of the enhancement.</param>
        /// <returns>The enhancement subtype detail.</returns>
        public EnhancementSubTypeAttribute GetEnhancementSubType(EnhancementSubType subType)
        {
            return _enhancementSubTypes[subType];
        }

        /// <summary>
        /// Retrieves all of the registered recipe categories.
        /// </summary>
        /// <returns>A dictionary containing all registered categories.</returns>
        public Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetAllCategories()
        {
            return _activeCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Determines if an item is a recipe.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a recipe, false otherwise</returns>
        public bool IsItemRecipe(uint item)
        {
            var tag = GetTag(item).ToLower();

            return tag == "recipe";
        }

        /// <summary>
        /// Determines if an item is a crafting component.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a crafting component, false otherwise</returns>
        public bool IsItemComponent(uint item)
        {
            var resref = GetResRef(item);
            return _componentResrefs.Contains(resref);
        }

        /// <summary>
        /// Determines if an item is an enhancement used in crafting.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is an enhancement, false otherwise</returns>
        public bool IsItemEnhancement(uint item)
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

        public Dictionary<RecipeType, RecipeDetail> GetAllRecipes()
        {
            return _recipes;
        }

        public Dictionary<RecipeType, RecipeDetail> GetAllResearchableRecipes()
        {
            return _researchableRecipes;
        }

        public Dictionary<RecipeType, RecipeDetail> GetAllRecipesBySkill(SkillType skill)
        {
            return _recipesBySkill[skill];
        }

        public Dictionary<RecipeType, RecipeDetail> GetAllResearchableRecipesBySkill(SkillType skill)
        {
            return _researchableRecipesBySkill[skill];
        }

        /// <summary>
        /// Retrieves all of the recipes associated with a skill and category.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of recipes under a given skill and category.</returns>
        public Dictionary<RecipeType, RecipeDetail> GetRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category)
        {
            if(!_recipesBySkillAndCategory.ContainsKey(skill))
                return new Dictionary<RecipeType, RecipeDetail>();

            if(!_recipesBySkillAndCategory[skill].ContainsKey(category))
                return new Dictionary<RecipeType, RecipeDetail>();

            return _recipesBySkillAndCategory[skill][category].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Calculates the skill rank required to craft a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to check.</param>
        /// <returns>The required skill rank.</returns>
        public int GetRequiredSkillRankForRecipe(RecipeDetail recipe)
        {
            var requiredRank = recipe.Level - RecipeSkillUnlockOffset;
            return Math.Max(requiredRank, 0);
        }

        public int GetBaseRecipeXP(RecipeDetail recipe, int skillRank)
        {
            return recipe.PracticeRankLimit.HasValue
                ? _skill.GetPracticeXP(recipe.Level, skillRank, recipe.PracticeRankLimit.Value)
                : _skill.GetDeltaXP(recipe.Level - skillRank);
        }

        private string CheckRecipeSkillRequirement(uint player, RecipeDetail recipe)
        {
            var requiredRank = GetRequiredSkillRankForRecipe(recipe);
            if (requiredRank <= 0)
                return string.Empty;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var skillRank = dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill)
                ? skill.Rank
                : 0;

            return skillRank >= requiredRank
                ? string.Empty
                : $"{_skill.GetSkillDetails(recipe.Skill).Name} must be level {requiredRank}.";
        }

        /// <summary>
        /// Retrieves all of the researchable recipes associated with a skill and category.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns></returns>
        public Dictionary<RecipeType, RecipeDetail> GetResearchableRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category)
        {
            if (!_researchableRecipesBySkillAndCategory.ContainsKey(skill))
                return new Dictionary<RecipeType, RecipeDetail>();

            if (!_researchableRecipesBySkillAndCategory[skill].ContainsKey(category))
                return new Dictionary<RecipeType, RecipeDetail>();

            return _researchableRecipesBySkillAndCategory[skill][category].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves all of the categories listed under a specific skill.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <returns>A list of recipe categories associated with a skill.</returns>
        public Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetRecipeCategoriesBySkill(SkillType skill)
        {
            if(!_categoriesBySkill.ContainsKey(skill))
                return new Dictionary<RecipeCategoryType, RecipeCategoryAttribute>();

            return _categoriesBySkill[skill].ToDictionary(x => x.Key, y => y.Value);
        }


        /// <summary>
        /// Builds a recipe's detail for use within the NUI window.
        /// </summary>
        /// <param name="player">The player to build for.</param>
        /// <param name="recipe">The recipe to build.</param>
        /// <param name="blueprint">The blueprint details. null if not a blueprint</param>
        public (GuiBindingList<string>, GuiBindingList<GuiColor>) BuildRecipeDetail(uint player, RecipeType recipe, BlueprintDetail blueprint)
        {
            var detail = GetRecipe(recipe);
            var recipeDetails = new GuiBindingList<string>();
            var recipeDetailColors = new GuiBindingList<GuiColor>();

            recipeDetails.Add("[COMPONENTS]");
            recipeDetailColors.Add(GuiColor.Cyan);
            foreach (var (resref, quantity) in detail.Components)
            {
                var componentName = _cache.GetItemNameByResref(resref);
                recipeDetails.Add($"{quantity}x {componentName}");
                recipeDetailColors.Add(GuiColor.White);
            }

            recipeDetails.Add(string.Empty);
            recipeDetailColors.Add(GuiColor.Green);

            recipeDetails.Add("[REQUIREMENTS]");
            recipeDetailColors.Add(GuiColor.Cyan);

            var requiredSkillRank = GetRequiredSkillRankForRecipe(detail);
            recipeDetails.Add($"{_skill.GetSkillDetails(detail.Skill).Name} rank {requiredSkillRank}");
            recipeDetailColors.Add(string.IsNullOrWhiteSpace(CheckRecipeSkillRequirement(player, detail))
                ? GuiColor.Green
                : GuiColor.Red);

            if (detail.PracticeRankLimit.HasValue)
            {
                recipeDetails.Add($"Trains {_skill.GetSkillDetails(detail.Skill).Name} up to rank {detail.PracticeRankLimit.Value}");
                recipeDetailColors.Add(GuiColor.White);
            }

            foreach (var req in detail.Requirements)
            {
                recipeDetails.Add(req.RequirementText);
                recipeDetailColors.Add(string.IsNullOrWhiteSpace(req.CheckRequirements(player))
                    ? GuiColor.Green
                    : GuiColor.Red);
            }

            recipeDetails.Add(string.Empty);
            recipeDetailColors.Add(GuiColor.Green);

            if (detail.EffectLines.Count > 0)
            {
                recipeDetails.Add("[EFFECT]");
                recipeDetailColors.Add(GuiColor.Cyan);

                foreach (var line in detail.EffectLines)
                {
                    recipeDetails.Add(line);
                    recipeDetailColors.Add(GuiColor.White);
                }

                recipeDetails.Add(string.Empty);
                recipeDetailColors.Add(GuiColor.White);
            }

            recipeDetails.Add("[PROPERTIES]");
            recipeDetailColors.Add(GuiColor.Cyan);
            var tempStorage = GetObjectByTag("TEMP_ITEM_STORAGE");
            var item = CreateItemOnObject(detail.Resref, tempStorage);

            foreach (var ip in _item.BuildItemPropertyList(item))
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
        public bool CanPlayerCraftRecipe(uint player, RecipeType recipeType)
        {
            var recipe = GetRecipe(recipeType);
            if (!string.IsNullOrWhiteSpace(CheckRecipeSkillRequirement(player, recipe)))
                return false;

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
        /// Determines whether a player can research a specific recipe.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="recipeType">The recipe to check</param>
        /// <returns>true if the player can research the recipe, false otherwise</returns>
        public bool CanPlayerResearchRecipe(uint player, RecipeType recipeType)
        {
            var recipe = GetRecipe(recipeType);
            var tier = recipe.Level / 10 + 1;
            if (tier > 5)
                tier = 5;

            return CraftRankMilestones.GetResearchTier(player) >= tier;
        }

        /// <summary>
        /// Retrieves a recipe's level detail by the given level number.
        /// </summary>
        /// <param name="level">The level to search by.</param>
        /// <returns>A recipe level detail.</returns>
        public RecipeLevelDetail GetRecipeLevelDetail(int level)
        {
            return _levelChart.GetByLevel(level);
        }

        /// <summary>
        /// Retrieves a blueprint detail object about an item.
        /// If item is not a blueprint, resulting recipe type will be Invalid.
        /// </summary>
        /// <param name="blueprint">The blueprint item</param>
        /// <returns>A blueprint detail object</returns>
        public BlueprintDetail GetBlueprintDetails(uint blueprint)
        {
            var blueprintDetail = new BlueprintDetail();
            var recipeId = GetLocalInt(blueprint, "BLUEPRINT_RECIPE_ID");
            blueprintDetail.Recipe = (RecipeType)recipeId;
            blueprintDetail.RandomEnhancementSlotGranted = GetLocalBool(blueprint, "BLUEPRINT_RANDOM_ENHANCEMENT_SLOT_GRANTED");

            var blueprintProperties = new List<(ItemProperty Property, ItemPropertyType Type, int SubType, int CostValue)>();
            for (var ip = GetFirstItemProperty(blueprint); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(blueprint))
            {
                blueprintProperties.Add((
                    ip,
                    GetItemPropertyType(ip),
                    GetItemPropertySubType(ip),
                    GetItemPropertyCostTableValue(ip)));
            }

            for (var index = 0; index < blueprintProperties.Count; index++)
            {
                var property = blueprintProperties[index];
                var type = property.Type;
                var subType = property.SubType;
                var costValue = property.CostValue;

                if (type == ItemPropertyType.Blueprint)
                {
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
                else if (type == ItemPropertyType.ArmorEnhancement ||
                         type == ItemPropertyType.WeaponEnhancement ||
                         type == ItemPropertyType.StructureEnhancement ||
                         type == ItemPropertyType.FoodEnhancement ||
                         type == ItemPropertyType.StarshipEnhancement ||
                         type == ItemPropertyType.ModuleEnhancement ||
                         type == ItemPropertyType.DroidEnhancement)
                {
                    var damageType = CombatDamageType.Invalid;
                    var enhancementSubType = (EnhancementSubType)subType;
                    if (type == ItemPropertyType.WeaponEnhancement &&
                        _enhancement.IsWeaponDamageEnhancement(enhancementSubType) &&
                        index + 1 < blueprintProperties.Count &&
                        blueprintProperties[index + 1].Type == ItemPropertyType.WeaponDamageType &&
                        Enum.IsDefined(typeof(CombatDamageType), blueprintProperties[index + 1].SubType))
                    {
                        damageType = (CombatDamageType)blueprintProperties[index + 1].SubType;
                        index++;
                    }

                    blueprintDetail.GuaranteedBonuses.AddRange(_enhancement.BuildItemPropertiesForEnhancement(enhancementSubType, costValue, damageType));
                }

            }

            return blueprintDetail;
        }

        /// <summary>
        /// Sets details about a blueprint onto the item.
        /// These are a combination of item properties and local variables set onto the item.
        /// </summary>
        /// <param name="blueprint">The blueprint to modify.</param>
        /// <param name="blueprintDetail">The details about the blueprint.</param>
        public void SetBlueprintDetails(uint blueprint, BlueprintDetail blueprintDetail)
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

        private int CalculateResearchCost(RecipeType recipe, int blueprintLevel, int baseConstant, float reductionBonus)
        {
            var recipeDetail = GetRecipe(recipe);
            var perkLevel = recipeDetail.Level / 10 + 1;
            if (perkLevel > 5)
                perkLevel = 5;

            var price = baseConstant * (Math.Pow(perkLevel, 2.2f) * Math.Pow(blueprintLevel, 2f));
            price += price * recipeDetail.ResearchCostModifier;
            price -= reductionBonus * price;

            return (int)price;
        }

        /// <summary>
        /// Calculates the credit cost to craft a blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to craft.</param>
        /// <returns>The number of credits to charge the player to craft the item.</returns>
        public int CalculateBlueprintCraftCreditCost(uint blueprint)
        {
            var blueprintDetail = GetBlueprintDetails(blueprint);
            var reductionBonus = blueprintDetail.CreditReduction * 0.01f;
            return CalculateResearchCost(blueprintDetail.Recipe, blueprintDetail.Level, 80, reductionBonus);
        }

        /// <summary>
        /// Calculates the credit cost to research a blueprint.
        /// </summary>
        /// <param name="recipe">The recipe to research</param>
        /// <param name="blueprintLevel">The level of the blueprint</param>
        /// <param name="reductionBonus">The % reduction towards credit cost to research</param>
        /// <returns>The number of credits to charge the player to research the blueprint.</returns>
        public int CalculateBlueprintResearchCreditCost(RecipeType recipe, int blueprintLevel, int reductionBonus)
        {
            return CalculateResearchCost(recipe, blueprintLevel, 200, reductionBonus * 0.01f);
        }

        /// <summary>
        /// Calculates the number of seconds it takes to research a blueprint.
        /// </summary>
        /// <param name="recipe">The recipe to research</param>
        /// <param name="blueprintLevel">The level of the blueprint</param>
        /// <param name="reductionBonus">The % reduction towards credit cost to research</param>
        /// <returns>The number of seconds to wait before the blueprint is researched to the next level.</returns>
        public int CalculateBlueprintResearchSeconds(RecipeType recipe, int blueprintLevel, int reductionBonus)
        {
            return CalculateResearchCost(recipe, blueprintLevel, 200, reductionBonus * 0.01f);
        }
    }
}
