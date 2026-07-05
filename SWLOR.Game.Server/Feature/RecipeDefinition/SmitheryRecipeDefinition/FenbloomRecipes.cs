using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class FenbloomRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Fenbloom Harness
            _builder.Create(RecipeType.FenbloomHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("mv_harness")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("mv_core", 1);

            // Fenbloom Wraps
            _builder.Create(RecipeType.FenbloomWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("mv_wraps")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1);

            // Fenbloom Treads
            _builder.Create(RecipeType.FenbloomTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("mv_treads")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1);

            // Fenbloom Sash
            _builder.Create(RecipeType.FenbloomSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("mv_sash")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1);

            // Fenbloom Mantle
            _builder.Create(RecipeType.FenbloomMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("mv_mantle")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1);

            // Fenbloom Gorget
            _builder.Create(RecipeType.FenbloomGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mv_gorget")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1);

            // Fenbloom Band
            _builder.Create(RecipeType.FenbloomBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("mv_band")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1);

            // Fenbloom Guard
            _builder.Create(RecipeType.FenbloomGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("mv_guard")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1);

            // Fenbloom Visor
            _builder.Create(RecipeType.FenbloomVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("mv_visor")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("mv_core", 1);

            // Fenbloom Charm
            _builder.Create(RecipeType.FenbloomCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mv_charm")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1);
        }
    }
}
