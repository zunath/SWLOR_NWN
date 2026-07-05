using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class StonewakeRelayRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Stonewake Relay
            _builder.Create(RecipeType.StonewakeRelay, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("vs_relay")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("obsidian", 3)
                .Component("elec_good", 3)
                .Component("vs_mask", 1);
        }
    }
}
