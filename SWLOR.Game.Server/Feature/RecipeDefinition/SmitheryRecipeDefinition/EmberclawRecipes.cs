using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class EmberclawRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Emberclaw Harness
            _builder.Create(RecipeType.EmberclawHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("rk_harness")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("rk_claw", 1);

            // Emberclaw Wraps
            _builder.Create(RecipeType.EmberclawWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("rk_wraps")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1);

            // Emberclaw Treads
            _builder.Create(RecipeType.EmberclawTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("rk_treads")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1);

            // Emberclaw Sash
            _builder.Create(RecipeType.EmberclawSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("rk_sash")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1);

            // Emberclaw Mantle
            _builder.Create(RecipeType.EmberclawMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("rk_mantle")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1);

            // Emberclaw Gorget
            _builder.Create(RecipeType.EmberclawGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("rk_gorget")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1);

            // Emberclaw Band
            _builder.Create(RecipeType.EmberclawBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("rk_band")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1);

            // Emberclaw Guard
            _builder.Create(RecipeType.EmberclawGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("rk_guard")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1);

            // Emberclaw Visor
            _builder.Create(RecipeType.EmberclawVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("rk_visor")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("rk_claw", 1);

            // Emberclaw Charm
            _builder.Create(RecipeType.EmberclawCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("rk_charm")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1);
        }
    }
}
