using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class RecipeBuilder
    {
        private readonly Dictionary<RecipeType, RecipeDetail> _recipes = new Dictionary<RecipeType, RecipeDetail>();
        private RecipeDetail _activeRecipe;
        private RecipeType _activeType;

        /// <summary>
        /// Creates a new recipe.
        /// </summary>
        /// <param name="type">The type of recipe to create.</param>
        /// <param name="skill">The skill associated with this recipe.</param>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder Create(RecipeType type, SkillType skill)
        {
            _activeRecipe = new RecipeDetail
            {
                Skill = skill
            };
            _activeType = type;
            _recipes.Add(type, _activeRecipe);

            return this;
        }

        /// <summary>
        /// Sets the category of the recipe. If no category is set,
        /// the item will be displayed in the "Uncategorized" category in the menu.
        /// It's recommended all recipes have a category set.
        /// </summary>
        /// <param name="category">The category to put the recipe under.</param>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder Category(RecipeCategoryType category)
        {
            _activeRecipe.Category = category;
            return this;
        }

        /// <summary>
        /// Sets the level of the recipe which is used for success calculation.
        /// </summary>
        /// <param name="level">The level of the recipe.</param>
        /// <returns>A recipe builder with the configured options.</returns>
        public RecipeBuilder Level(int level)
        {
            _activeRecipe.Level = level;
            return this;
        }

        /// <summary>
        /// Sets the quantity of items the player receives when crafting this recipe.
        /// Quantity is automatically set to 1 by default so this is only necessary if
        /// you need a different number.
        /// </summary>
        /// <param name="quantity">The quantity of items to create.</param>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder Quantity(int quantity)
        {
            if (quantity < 1)
                quantity = 1;

            _activeRecipe.Quantity = quantity;
            return this;
        }

        /// <summary>
        /// Sets the resref of the item created when a player crafts this recipe.
        /// </summary>
        /// <param name="resref">The resref of the item to create.</param>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder Resref(string resref)
        {
            _activeRecipe.Resref = resref;
            return this;
        }

        /// <summary>
        /// Sets the number of enhancement slots available to a recipe.
        /// If called twice, the latest one will replace the previous one.
        /// </summary>
        /// <param name="type">The type of enhancement.</param>
        /// <param name="slots">The number of slots</param>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder EnhancementSlots(RecipeEnhancementType type, int slots)
        {
            _activeRecipe.EnhancementType = type;
            _activeRecipe.EnhancementSlots = slots;

            return this;
        }

        /// <summary>
        /// Adjusts the cost of researching this blueprint by a certain percentage.
        /// Positive numbers increase the cost. Negative numbers decrease it.
        /// </summary>
        /// <param name="modifier">The modifier to apply to researching the blueprint.</param>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder ResearchCostModifier(float modifier)
        {
            _activeRecipe.ResearchCostModifier = modifier;

            return this;
        }
        
        /// <summary>
        /// Deactivates the recipe which will prevent players from learning and crafting the item.
        /// </summary>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder Inactive()
        {
            _activeRecipe.IsActive = false;
            return this;
        }

        /// <summary>
        /// Adds a perk requirement for this recipe.
        /// </summary>
        /// <param name="perk">The perk which is required.</param>
        /// <param name="requiredLevel">The level required.</param>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder RequirementPerk(PerkType perk, int requiredLevel)
        {
            var requirement = new RecipePerkRequirement(perk, requiredLevel);
            _activeRecipe.Requirements.Add(requirement);
            return this;
        }

        /// <summary>
        /// Adds an unlock requirement for this recipe.
        /// </summary>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder RequirementUnlocked()
        {
            var requirement = new RecipeUnlockRequirement(_activeType);
            _activeRecipe.Requirements.Add(requirement);
            return this;
        }

        /// <summary>
        /// Adds a component requirement to craft this recipe.
        /// </summary>
        /// <param name="resref">The item resref required.</param>
        /// <param name="quantity">The number of this component required.</param>
        /// <returns>A recipe builder with the configured options</returns>
        public RecipeBuilder Component(string resref, int quantity)
        {
            _activeRecipe.Components[resref] = quantity;
            return this;
        }

        /// <summary>
        /// Returns a built list of recipes.
        /// </summary>
        /// <returns>A list of built recipes.</returns>
        public Dictionary<RecipeType, RecipeDetail> Build()
        {
            return _recipes;
        }
    }
}
