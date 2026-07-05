using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class LucidSpliceRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Lucid Splice
            _builder.Create(RecipeType.LucidSplice, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("mg_splice")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("crystal", 4)
                .Component("elec_imperfect", 3)
                .Component("mg_totem", 1);
        }
    }
}
