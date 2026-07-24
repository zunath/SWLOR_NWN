using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class SilkweaveRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Silkweave Cloak
            _builder.Create(RecipeType.SilkweaveCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("silkweave_cloak")
                .Level(45)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("silkvine_fiber", 5)
                .Component("lth_imperfect", 3)
                .Component("ref_jasioclase", 2);

            // Silkweave Belt
            _builder.Create(RecipeType.SilkweaveBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("silkweave_belt")
                .Level(47)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("silkvine_fiber", 5)
                .Component("lth_imperfect", 3);
        }
    }
}
