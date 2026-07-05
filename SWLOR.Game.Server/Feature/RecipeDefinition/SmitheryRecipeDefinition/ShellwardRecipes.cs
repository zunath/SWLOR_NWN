using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ShellwardRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Shellward Harness
            _builder.Create(RecipeType.ShellwardHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("mb_harness")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("mb_shell", 1);

            // Shellward Wraps
            _builder.Create(RecipeType.ShellwardWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("mb_wraps")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1);

            // Shellward Treads
            _builder.Create(RecipeType.ShellwardTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("mb_treads")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1);

            // Shellward Sash
            _builder.Create(RecipeType.ShellwardSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("mb_sash")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1);

            // Shellward Mantle
            _builder.Create(RecipeType.ShellwardMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("mb_mantle")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1);

            // Shellward Gorget
            _builder.Create(RecipeType.ShellwardGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mb_gorget")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1);

            // Shellward Band
            _builder.Create(RecipeType.ShellwardBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("mb_band")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1);

            // Shellward Guard
            _builder.Create(RecipeType.ShellwardGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("mb_guard")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1);

            // Shellward Visor
            _builder.Create(RecipeType.ShellwardVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("mb_visor")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("mb_shell", 1);

            // Shellward Charm
            _builder.Create(RecipeType.ShellwardCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mb_charm")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1);
        }
    }
}
