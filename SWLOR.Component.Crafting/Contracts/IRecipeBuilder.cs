using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Crafting.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Crafting.Contracts
{
    public interface IRecipeBuilder
    {
        /// <summary>
        /// Creates a new recipe.
        /// </summary>
        /// <param name="type">The type of recipe to create.</param>
        /// <param name="skill">The skill associated with this recipe.</param>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder Create(RecipeType type, SkillType skill);

        /// <summary>
        /// Sets the category of the recipe. If no category is set,
        /// the item will be displayed in the "Uncategorized" category in the menu.
        /// It's recommended all recipes have a category set.
        /// </summary>
        /// <param name="category">The category to put the recipe under.</param>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder Category(RecipeCategoryType category);

        /// <summary>
        /// Sets the level of the recipe which is used for success calculation.
        /// </summary>
        /// <param name="level">The level of the recipe.</param>
        /// <returns>A recipe builder with the configured options.</returns>
        IRecipeBuilder Level(int level);

        /// <summary>
        /// Sets the quantity of items the player receives when crafting this recipe.
        /// Quantity is automatically set to 1 by default so this is only necessary if
        /// you need a different number.
        /// </summary>
        /// <param name="quantity">The quantity of items to create.</param>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder Quantity(int quantity);

        /// <summary>
        /// Sets the resref of the item created when a player crafts this recipe.
        /// </summary>
        /// <param name="resref">The resref of the item to create.</param>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder Resref(string resref);

        /// <summary>
        /// Sets the number of enhancement slots available to a recipe.
        /// If called twice, the latest one will replace the previous one.
        /// </summary>
        /// <param name="type">The type of enhancement.</param>
        /// <param name="slots">The number of slots</param>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder EnhancementSlots(RecipeEnhancementType type, int slots);

        /// <summary>
        /// Adjusts the cost of researching this blueprint by a certain percentage.
        /// Positive numbers increase the cost. Negative numbers decrease it.
        /// </summary>
        /// <param name="modifier">The modifier to apply to researching the blueprint.</param>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder ResearchCostModifier(float modifier);
        
        /// <summary>
        /// Deactivates the recipe which will prevent players from learning and crafting the item.
        /// </summary>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder Inactive();

        /// <summary>
        /// Adds a perk requirement for this recipe.
        /// </summary>
        /// <param name="perk">The perk which is required.</param>
        /// <param name="requiredLevel">The level required.</param>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder RequirementPerk(PerkType perk, int requiredLevel);

        /// <summary>
        /// Adds an unlock requirement for this recipe.
        /// </summary>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder RequirementUnlocked();

        /// <summary>
        /// Adds a component requirement to craft this recipe.
        /// </summary>
        /// <param name="resref">The item resref required.</param>
        /// <param name="quantity">The number of this component required.</param>
        /// <returns>A recipe builder with the configured options</returns>
        IRecipeBuilder Component(string resref, int quantity);

        /// <summary>
        /// Returns a built list of recipes.
        /// </summary>
        /// <returns>A list of built recipes.</returns>
        Dictionary<RecipeType, RecipeDetail> Build();
    }
}
