using SWLOR.Component.Crafting.Enums;
using SWLOR.Component.Crafting.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Crafting.Contracts
{
    public interface ICraftService
    {
        /// <summary>
        /// When the skill cache has finished loading, recipe and category data is cached.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Retrieves the details about a recipe.
        /// If recipe type has not been registered, an exception will be raised.
        /// </summary>
        /// <param name="recipeType">The type of recipe to retrieve.</param>
        /// <returns>The recipe detail.</returns>
        RecipeDetail GetRecipe(RecipeType recipeType);

        /// <summary>
        /// Returns true if a recipe has been registered for this type.
        /// </summary>
        /// <param name="recipeType">The type of recipe to look for.</param>
        /// <returns>true if recipe has been registered, false otherwise.</returns>
        bool RecipeExists(RecipeType recipeType);

        /// <summary>
        /// Retrieves the details about an enhancement subtype.
        /// If the enhancement subtype has not been registered, an exception will be raised.
        /// </summary>
        /// <param name="subType">The subtype of the enhancement.</param>
        /// <returns>The enhancement subtype detail.</returns>
        EnhancementSubTypeAttribute GetEnhancementSubType(EnhancementSubType subType);

        /// <summary>
        /// Retrieves all of the registered recipe categories.
        /// </summary>
        /// <returns>A dictionary containing all registered categories.</returns>
        Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetAllCategories();

        /// <summary>
        /// Determines if an item is a recipe.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a recipe, false otherwise</returns>
        bool IsItemRecipe(uint item);

        /// <summary>
        /// Determines if an item is a crafting component.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a crafting component, false otherwise</returns>
        bool IsItemComponent(uint item);

        /// <summary>
        /// Determines if an item is an enhancement used in crafting.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is an enhancement, false otherwise</returns>
        bool IsItemEnhancement(uint item);

        Dictionary<RecipeType, RecipeDetail> GetAllRecipes();
        Dictionary<RecipeType, RecipeDetail> GetAllResearchableRecipes();
        Dictionary<RecipeType, RecipeDetail> GetAllRecipesBySkill(SkillType skill);
        Dictionary<RecipeType, RecipeDetail> GetAllResearchableRecipesBySkill(SkillType skill);

        /// <summary>
        /// Retrieves all of the recipes associated with a skill and category.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of recipes under a given skill and category.</returns>
        Dictionary<RecipeType, RecipeDetail> GetRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category);

        /// <summary>
        /// Retrieves all of the researchable recipes associated with a skill and category.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns></returns>
        Dictionary<RecipeType, RecipeDetail> GetResearchableRecipesBySkillAndCategory(SkillType skill, RecipeCategoryType category);

        /// <summary>
        /// Retrieves all of the categories listed under a specific skill.
        /// </summary>
        /// <param name="skill">The skill to search by.</param>
        /// <returns>A list of recipe categories associated with a skill.</returns>
        Dictionary<RecipeCategoryType, RecipeCategoryAttribute> GetRecipeCategoriesBySkill(SkillType skill);

        /// <summary>
        /// When a crafting device is used, display the recipe menu.
        /// </summary>
        void UseCraftingDevice();

        /// <summary>
        /// Builds a recipe's detail for use within the NUI window.
        /// </summary>
        /// <param name="player">The player to build for.</param>
        /// <param name="recipe">The recipe to build.</param>
        /// <param name="blueprint">The blueprint details. null if not a blueprint</param>
        (GuiBindingList<string>, GuiBindingList<GuiColor>) BuildRecipeDetail(uint player, RecipeType recipe, BlueprintDetail blueprint);

        /// <summary>
        /// Determines whether a player can craft a specific recipe.
        /// This does not account for whether the player actually has the required items in their inventory.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="recipeType">The recipe to check</param>
        /// <returns>true if the player can craft the recipe, false otherwise</returns>
        bool CanPlayerCraftRecipe(uint player, RecipeType recipeType);

        /// <summary>
        /// Determines whether a player can research a specific recipe.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="recipeType">The recipe to check</param>
        /// <returns>true if the player can research the recipe, false otherwise</returns>
        bool CanPlayerResearchRecipe(uint player, RecipeType recipeType);

        /// <summary>
        /// Retrieves a recipe's level detail by the given level number.
        /// </summary>
        /// <param name="level">The level to search by.</param>
        /// <returns>A recipe level detail.</returns>
        RecipeLevelDetail GetRecipeLevelDetail(int level);

        /// <summary>
        /// Builds an item property for a given enhancement type.
        /// </summary>
        /// <param name="subTypeId">The sub type of the enhancement</param>
        /// <param name="amount">The amount to apply.</param>
        /// <returns></returns>
        ItemProperty BuildItemPropertyForEnhancement(EnhancementSubType subTypeId, int amount);

        void UseRefinery();
        void UseResearchTerminal();

        /// <summary>
        /// Retrieves a blueprint detail object about an item.
        /// If item is not a blueprint, resulting recipe type will be Invalid.
        /// </summary>
        /// <param name="blueprint">The blueprint item</param>
        /// <returns>A blueprint detail object</returns>
        BlueprintDetail GetBlueprintDetails(uint blueprint);

        /// <summary>
        /// Sets details about a blueprint onto the item.
        /// These are a combination of item properties and local variables set onto the item.
        /// </summary>
        /// <param name="blueprint">The blueprint to modify.</param>
        /// <param name="blueprintDetail">The details about the blueprint.</param>
        void SetBlueprintDetails(uint blueprint, BlueprintDetail blueprintDetail);

        /// <summary>
        /// Calculates the credit cost to craft a blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to craft.</param>
        /// <returns>The number of credits to charge the player to craft the item.</returns>
        int CalculateBlueprintCraftCreditCost(uint blueprint);

        /// <summary>
        /// Calculates the credit cost to research a blueprint.
        /// </summary>
        /// <param name="recipe">The recipe to research</param>
        /// <param name="blueprintLevel">The level of the blueprint</param>
        /// <param name="reductionBonus">The % reduction towards credit cost to research</param>
        /// <returns>The number of credits to charge the player to research the blueprint.</returns>
        int CalculateBlueprintResearchCreditCost(RecipeType recipe, int blueprintLevel, int reductionBonus);

        /// <summary>
        /// Calculates the number of seconds it takes to research a blueprint.
        /// </summary>
        /// <param name="recipe">The recipe to research</param>
        /// <param name="blueprintLevel">The level of the blueprint</param>
        /// <param name="reductionBonus">The % reduction towards credit cost to research</param>
        /// <returns>The number of seconds to wait before the blueprint is researched to the next level.</returns>
        int CalculateBlueprintResearchSeconds(RecipeType recipe, int blueprintLevel, int reductionBonus);

        /// <summary>
        /// When a property is removed, also remove any associated research jobs.
        /// </summary>
        void OnRemoveProperty();
    }
}