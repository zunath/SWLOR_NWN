using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class SlicingCacheSmitheryRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Add(RecipeType.StitchplateLockGloves, RecipeCategoryType.Glove, "slc_stitchglv", 5, "fiberp_ruined", "lth_ruined");
            Add(RecipeType.FalseFaceFieldVisor, RecipeCategoryType.Helmet, "slc_falsevisor", 15, "fiberp_flawed", "lth_flawed");
            Add(RecipeType.QuietstepReinforcedBoots, RecipeCategoryType.Boots, "slc_quietboots", 25, "fiberp_imperfect", "lth_imperfect");
            Add(RecipeType.DeadDropArmoredCloak, RecipeCategoryType.Cloak, "slc_dropcloak", 35, "fiberp_high", "lth_high");
            Add(RecipeType.BlacksiteBreachHarness, RecipeCategoryType.Breastplate, "slc_breachhar", 45, "fiberp_perfect", "lth_perfect");
            return _builder.Build();
        }

        private void Add(RecipeType type, RecipeCategoryType category, string resref, int level, string fiber, string leather)
        {
            _builder.Create(type, SkillType.Smithery)
                .RequirementUnlocked()
                .Category(category)
                .Resref(resref)
                .Level(level)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component(fiber, 3)
                .Component(leather, 2);
        }
    }
}
