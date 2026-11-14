using System.Collections.Generic;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition
{
    /// <summary>
    /// The ship component recipes (plan section 14, Appendix D) from <see cref="RevampComponentCatalog"/>: Smithery's
    /// metal components, Engineering's electronic ones and Agriculture's life support, each with its crafted variants.
    /// A variant makes the same item and stamps its trait on it (ShipComponents.ApplyCraftedTraits).
    /// </summary>
    public class ShipComponentRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            foreach (var row in RevampComponentCatalog.Recipes)
            {
                var recipe = _builder.Create(row.RecipeType, row.Skill)
                    .Category(RecipeCategoryType.ShipComponent)
                    .Resref(row.Resref)
                    .Level(row.RecipeLevel)
                    .Quantity(1)
                    .MinigameParameters(RecipeParameterTable.For(row.RecipeType));

                var trait = ShipTraitRules.Get(row.Trait);
                if (trait != null)
                {
                    recipe.Name(row.Name)
                        .EffectDescription(new[] { $"Trait: {trait.Name} ({trait.Description})" });
                }

                foreach (var (resref, quantity) in row.Components)
                {
                    recipe.Component(resref, quantity);
                }
            }

            // The capital parts (W11o): the top band Capital hulls take, filed with the other capital construction parts.
            foreach (var row in RevampComponentCatalog.CapitalPartRecipes)
            {
                var recipe = _builder.Create(row.RecipeType, row.Skill)
                    .Category(RecipeCategoryType.CapitalConstruction)
                    .Resref(row.Resref)
                    .Level(row.RecipeLevel)
                    .Quantity(1)
                    .MinigameParameters(RecipeParameterTable.For(row.RecipeType));

                foreach (var (resref, quantity) in row.Components)
                {
                    recipe.Component(resref, quantity);
                }
            }

            return _builder.Build();
        }
    }
}
