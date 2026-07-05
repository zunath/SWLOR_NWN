using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class GhostkeyRelayRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Ghostkey Relay
            _builder.Create(RecipeType.GhostkeyRelay, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("nv_relay")
                .Level(12)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("elec_ruined", 3)
                .Component("ref_veldite", 2)
                .Component("nv_pin", 1);
        }
    }
}
