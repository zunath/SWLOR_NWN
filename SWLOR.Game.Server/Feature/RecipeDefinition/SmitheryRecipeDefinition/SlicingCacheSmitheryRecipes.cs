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
            Add(RecipeType.StitchplateLockGloves, RecipeCategoryType.Glove, "slc_stitchglv", 5, "fiberp_ruined", "lth_ruined", parameters: new ForgeParameters(rangeCenter: 36, rangeWidth: 60, coolingRate: 2, startingThermal: 60, shapeShare: 54));
            Add(RecipeType.FalseFaceFieldVisor, RecipeCategoryType.Helmet, "slc_falsevisor", 15, "fiberp_flawed", "lth_flawed", parameters: new ForgeParameters(rangeCenter: 34, rangeWidth: 60, coolingRate: 2, startingThermal: 54, shapeShare: 57));
            Add(RecipeType.QuietstepReinforcedBoots, RecipeCategoryType.Boots, "slc_quietboots", 25, "fiberp_imperfect", "lth_imperfect", parameters: new ForgeParameters(rangeCenter: 42, rangeWidth: 52, coolingRate: 3, startingThermal: 54, shapeShare: 54));
            Add(RecipeType.DeadDropArmoredCloak, RecipeCategoryType.Cloak, "slc_dropcloak", 35, "fiberp_high", "lth_high", parameters: new ForgeParameters(rangeCenter: 39, rangeWidth: 47, coolingRate: 3, startingThermal: 47, shapeShare: 50));
            Add(RecipeType.BlacksiteBreachHarness, RecipeCategoryType.Breastplate, "slc_breachhar", 45, "fiberp_perfect", "lth_perfect", parameters: new ForgeParameters(rangeCenter: 39, rangeWidth: 47, coolingRate: 4, startingThermal: 42, shapeShare: 61));
            return _builder.Build();
        }

        private void Add(RecipeType type, RecipeCategoryType category, string resref, int level, string fiber, string leather, RecipeMinigameParameters parameters = null)
        {
            _builder.Create(type, SkillType.Smithery)
                .RequirementUnlocked()
                .Category(category)
                .Resref(resref)
                .Level(level)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component(fiber, 3)
                .Component(leather, 2)
                .MinigameParameters(parameters);
        }
    }
}
