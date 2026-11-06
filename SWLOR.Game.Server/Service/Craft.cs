using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service
{
    public static class Craft
    {
        public const int MaxResearchLevel = Domain.Crafting.CraftService.MaxResearchLevel;
        public const int RecipeSkillUnlockOffset = Domain.Crafting.CraftService.RecipeSkillUnlockOffset;

        private static ICraftService Instance => FacadeAccessor.Resolve<ICraftService>();

        /// <summary>The one engine world of the crafting sessions. It keeps the properties of the item being crafted.</summary>
        public static NwnCraftSessionWorld SessionWorld => Instance.SessionWorld;

        /// <summary>The one engine world of the quick craft batches. It keeps what the last craft created.</summary>
        public static NwnQuickCraftWorld QuickCraftWorld => Instance.QuickCraftWorld;

        /// <summary>
        /// When the skill cache has finished loading, recipe and category data is cached.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorSkillCache)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Retrieves the details about a recipe.
        /// If recipe type has not been registered, an exception will be raised.
        /// </summary>
        /// <param name="recipeType">The type of recipe to retrieve.</param>
        /// <returns>The recipe detail.</returns>
        public static RecipeDetail GetRecipe(RecipeType recipeType)
        {
            return Instance.GetRecipe(recipeType);
        }

        /// <summary>
        /// Returns true if a recipe has been registered for this type.
        /// </summary>
        /// <param name="recipeType">The type of recipe to look for.</param>
        /// <returns>true if recipe has been registered, false otherwise.</returns>
        public static bool RecipeExists(RecipeType recipeType)
        {
            return Instance.RecipeExists(recipeType);
        }

        /// <summary>
        /// Retrieves the details about an enhancement subtype.
        /// If the enhancement subtype has not been registered, an exception will be raised.
        /// </summary>
        /// <param name="subType">The subtype of the enhancement.</param>
        /// <returns>The enhancement subtype detail.</returns>
        public static EnhancementSubTypeAttribute GetEnhancementSubType(EnhancementSubType subType)
        {
            return Instance.GetEnhancementSubType(subType);
        }

        /// <summary>
        /// Retrieves all of the registered recipe categories.
        /// </summary>
        /// <returns>A dictionary containing all registered categories.</returns>
        public static Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetAllCategories()
        {
            return Instance.GetAllCategories();
        }

        /// <summary>
        /// Determines if an item is a recipe.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a recipe, false otherwise</returns>
        public static bool IsItemRecipe(uint item)
        {
            return Instance.IsItemRecipe(item);
        }

        /// <summary>
        /// Determines if an item is a crafting component.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a crafting component, false otherwise</returns>
        public static bool IsItemComponent(uint item)
        {
            return Instance.IsItemComponent(item);
        }

        /// <summary>
        /// Determines if an item is an enhancement used in crafting.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is an enhancement, false otherwise</returns>
        public static bool IsItemEnhancement(uint item)
        {
            return Instance.IsItemEnhancement(item);
        }

        public static Dictionary<RecipeType, RecipeDetail> GetAllRecipes()
        {
            return Instance.GetAllRecipes();
        }

        public static Dictionary<RecipeType, RecipeDetail> GetAllResearchableRecipes()
        {
            return Instance.GetAllResearchableRecipes();
        }

        public static Dictionary<RecipeType, RecipeDetail> GetAllRecipesBySkill(SkillType skill)
        {
            return Instance.GetAllRecipesBySkill(skill);
        }

        public static Dictionary<RecipeType, RecipeDetail> GetAllResearchableRecipesBySkill(SkillType skill)
        {
            return Instance.GetAllResearchableRecipesBySkill(skill);
        }

        /// <summary>
        /// Retrieves all of the recipes associated with a skill and category.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of recipes under a given skill and category.</returns>
        public static Dictionary<RecipeType, RecipeDetail> GetRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category)
        {
            return Instance.GetRecipesBySkillAndCategory(skill, category);
        }

        /// <summary>
        /// Calculates the skill rank required to craft a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to check.</param>
        /// <returns>The required skill rank.</returns>
        public static int GetRequiredSkillRankForRecipe(RecipeDetail recipe)
        {
            return Instance.GetRequiredSkillRankForRecipe(recipe);
        }

        public static int GetBaseRecipeXP(RecipeDetail recipe, int skillRank)
        {
            return Instance.GetBaseRecipeXP(recipe, skillRank);
        }

        /// <summary>
        /// Retrieves all of the researchable recipes associated with a skill and category.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns></returns>
        public static Dictionary<RecipeType, RecipeDetail> GetResearchableRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category)
        {
            return Instance.GetResearchableRecipesBySkillAndCategory(skill, category);
        }

        /// <summary>
        /// Retrieves all of the categories listed under a specific skill.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <returns>A list of recipe categories associated with a skill.</returns>
        public static Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetRecipeCategoriesBySkill(SkillType skill)
        {
            return Instance.GetRecipeCategoriesBySkill(skill);
        }

        /// <summary>
        /// When a crafting device is used, display the recipe menu.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCraftUsed)]
        public static void UseCraftingDevice()
        {
            Instance.UseCraftingDevice();
        }

        /// <summary>
        /// Builds a recipe's detail for use within the NUI window.
        /// </summary>
        /// <param name="player">The player to build for.</param>
        /// <param name="recipe">The recipe to build.</param>
        /// <param name="blueprint">The blueprint details. null if not a blueprint</param>
        public static (GuiBindingList<string>, GuiBindingList<GuiColor>) BuildRecipeDetail(uint player, RecipeType recipe, BlueprintDetail blueprint)
        {
            return Instance.BuildRecipeDetail(player, recipe, blueprint);
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
            return Instance.CanPlayerCraftRecipe(player, recipeType);
        }

        /// <summary>
        /// Determines whether a player can research a specific recipe.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="recipeType">The recipe to check</param>
        /// <returns>true if the player can research the recipe, false otherwise</returns>
        public static bool CanPlayerResearchRecipe(uint player, RecipeType recipeType)
        {
            return Instance.CanPlayerResearchRecipe(player, recipeType);
        }

        /// <summary>
        /// Retrieves a recipe's level detail by the given level number.
        /// </summary>
        /// <param name="level">The level to search by.</param>
        /// <returns>A recipe level detail.</returns>
        public static RecipeLevelDetail GetRecipeLevelDetail(int level)
        {
            return Instance.GetRecipeLevelDetail(level);
        }

        /// <summary>
        /// Builds an item property for a given enhancement type.
        /// </summary>
        /// <param name="subTypeId">The sub type of the enhancement</param>
        /// <param name="amount">The amount to apply.</param>
        /// <returns></returns>
        public static IEnumerable<ItemProperty> BuildItemPropertiesForEnhancement(EnhancementSubType subTypeId, int amount, CombatDamageType damageType = CombatDamageType.Invalid)
        {
            return Instance.BuildItemPropertiesForEnhancement(subTypeId, amount, damageType);
        }

        public static ItemProperty BuildItemPropertyForEnhancement(EnhancementSubType subTypeId, int amount)
        {
            return Instance.BuildItemPropertyForEnhancement(subTypeId, amount);
        }

        public static bool IsWeaponDamageEnhancement(EnhancementSubType subTypeId)
        {
            return Instance.IsWeaponDamageEnhancement(subTypeId);
        }

        public static bool TryGetWeaponDamageTypeForEnhancement(EnhancementSubType subTypeId, CombatDamageType explicitDamageType, out CombatDamageType damageType)
        {
            return Instance.TryGetWeaponDamageTypeForEnhancement(subTypeId, explicitDamageType, out damageType);
        }

        /// <summary>
        /// Applies an enhancement-provided item property onto a crafted item,
        /// merging with any existing property of the same type. DMG amounts are
        /// summed together; a conflicting WeaponDamageType is resolved by randomly
        /// keeping one of the two rather than always favoring the latest one applied.
        /// </summary>
        public static void ApplyCraftedItemProperty(uint item, ItemProperty ip)
        {
            Instance.ApplyCraftedItemProperty(item, ip);
        }

        /// <summary>
        /// Merges a crafted property into a list of properties by the rules <see cref="ApplyCraftedItemProperty"/> applies to an item:
        /// a property of the same type (and subtype, when the incoming one has one) is summed into one, a weapon keeps one damage type,
        /// and Force is never a weapon damage type. Working on a list lets a craft settle every property before any of them reaches
        /// the item, so the item never has a property removed and re-added: a removal is not committed to the item's saved property
        /// list until later, so a serialized form or copy taken in the same script would still carry the pre-merge property.
        /// </summary>
        public static void MergeCraftedItemProperty(List<ItemProperty> properties, ItemProperty ip)
        {
            Instance.MergeCraftedItemProperty(properties, ip);
        }

        [NWNEventHandler(ScriptName.OnRefineryUsed)]
        public static void UseRefinery()
        {
            Instance.UseRefinery();
        }

        [NWNEventHandler(ScriptName.OnResearchTerminal)]
        public static void UseResearchTerminal()
        {
            Instance.UseResearchTerminal();
        }

        /// <summary>
        /// Retrieves a blueprint detail object about an item.
        /// If item is not a blueprint, resulting recipe type will be Invalid.
        /// </summary>
        /// <param name="blueprint">The blueprint item</param>
        /// <returns>A blueprint detail object</returns>
        public static BlueprintDetail GetBlueprintDetails(uint blueprint)
        {
            return Instance.GetBlueprintDetails(blueprint);
        }

        /// <summary>
        /// Sets details about a blueprint onto the item.
        /// These are a combination of item properties and local variables set onto the item.
        /// </summary>
        /// <param name="blueprint">The blueprint to modify.</param>
        /// <param name="blueprintDetail">The details about the blueprint.</param>
        public static void SetBlueprintDetails(uint blueprint, BlueprintDetail blueprintDetail)
        {
            Instance.SetBlueprintDetails(blueprint, blueprintDetail);
        }

        /// <summary>
        /// Calculates the credit cost to craft a blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to craft.</param>
        /// <returns>The number of credits to charge the player to craft the item.</returns>
        public static int CalculateBlueprintCraftCreditCost(uint blueprint)
        {
            return Instance.CalculateBlueprintCraftCreditCost(blueprint);
        }

        /// <summary>
        /// Calculates the credit cost to research a blueprint.
        /// </summary>
        /// <param name="recipe">The recipe to research</param>
        /// <param name="blueprintLevel">The level of the blueprint</param>
        /// <param name="reductionBonus">The % reduction towards credit cost to research</param>
        /// <returns>The number of credits to charge the player to research the blueprint.</returns>
        public static int CalculateBlueprintResearchCreditCost(RecipeType recipe, int blueprintLevel, int reductionBonus)
        {
            return Instance.CalculateBlueprintResearchCreditCost(recipe, blueprintLevel, reductionBonus);
        }

        /// <summary>
        /// Calculates the number of seconds it takes to research a blueprint.
        /// </summary>
        /// <param name="recipe">The recipe to research</param>
        /// <param name="blueprintLevel">The level of the blueprint</param>
        /// <param name="reductionBonus">The % reduction towards credit cost to research</param>
        /// <returns>The number of seconds to wait before the blueprint is researched to the next level.</returns>
        public static int CalculateBlueprintResearchSeconds(RecipeType recipe, int blueprintLevel, int reductionBonus)
        {
            return Instance.CalculateBlueprintResearchSeconds(recipe, blueprintLevel, reductionBonus);
        }

        /// <summary>
        /// When a property is removed, also remove any associated research jobs.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public static void OnRemoveProperty()
        {
            Instance.OnRemoveProperty();
        }

        /// <summary>Applies the player's selected crafting styles again when they log in; the effects are not kept across a logout.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RestoreCraftingStylesOnEnter()
        {
            Instance.RestoreCraftingStylesOnEnter();
        }
    }
}
