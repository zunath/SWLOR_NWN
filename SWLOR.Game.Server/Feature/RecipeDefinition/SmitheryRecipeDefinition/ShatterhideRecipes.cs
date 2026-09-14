using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ShatterhideRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Shatterhide Harness
            _builder.Create(RecipeType.ShatterhideHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sh_harness")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 4)
                .Component("fiberp_good", 2)
                .Component("sh_chitin", 1);

            // Shatterhide Wraps
            _builder.Create(RecipeType.ShatterhideWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sh_wraps")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1);

            // Shatterhide Treads
            _builder.Create(RecipeType.ShatterhideTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sh_treads")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1);

            // Shatterhide Sash
            _builder.Create(RecipeType.ShatterhideSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sh_sash")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1);

            // Shatterhide Mantle
            _builder.Create(RecipeType.ShatterhideMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sh_mantle")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1);

            // Shatterhide Gorget
            _builder.Create(RecipeType.ShatterhideGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sh_gorget")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1);

            // Shatterhide Band
            _builder.Create(RecipeType.ShatterhideBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sh_band")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1);

            // Shatterhide Guard
            _builder.Create(RecipeType.ShatterhideGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("sh_guard")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1);

            // Shatterhide Visor
            _builder.Create(RecipeType.ShatterhideVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sh_visor")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 3)
                .Component("fiberp_good", 2)
                .Component("sh_chitin", 1);

            // Shatterhide Charm
            _builder.Create(RecipeType.ShatterhideCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sh_charm")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1);
        }
    }
}
