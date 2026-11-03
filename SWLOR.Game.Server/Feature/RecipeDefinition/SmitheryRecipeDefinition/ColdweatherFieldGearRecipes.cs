using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ColdweatherFieldGearRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            // Alternate material patterns retain the original recipes' levels, costs and outputs.
            _builder.Create(RecipeType.SilverpeltHideCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("hide_cuirass")
                .Level(42)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("esh_wolf_pelt", 1)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2);

            _builder.Create(RecipeType.FrostFangGauntlet, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("fang_gaunt")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("esh_frost_fang", 1)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2);

            _builder.Create(RecipeType.RidgedHideMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("windcall_mantle")
                .Level(44)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("esh_gorak_hide", 1)
                .Component("fiberp_high", 3)
                .Component("elec_high", 1);

            _builder.Create(RecipeType.ReclaimedScopewrightCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("scopewright_cap")
                .Level(43)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("esh_mando_salv", 1)
                .Component("elec_high", 3)
                .Component("fiberp_high", 2);

            _builder.Create(RecipeType.ReclaimedWarbandHelm, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("warband_helm")
                .Level(45)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("esh_sun_insignia", 1)
                .Component("lth_high", 3)
                .Component("elec_high", 2);

            return _builder.Build();
        }
    }
}
