using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class InkguardRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Inkguard Harness
            _builder.Create(RecipeType.InkguardHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("ig_harness")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2)
                .Component("midink_sac", 1);

            // Inkguard Wraps
            _builder.Create(RecipeType.InkguardWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("ig_wraps")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1);

            // Inkguard Treads
            _builder.Create(RecipeType.InkguardTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("ig_treads")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1);

            // Inkguard Sash
            _builder.Create(RecipeType.InkguardSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ig_sash")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1);

            // Inkguard Mantle
            _builder.Create(RecipeType.InkguardMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ig_mantle")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1);

            // Inkguard Gorget
            _builder.Create(RecipeType.InkguardGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ig_gorget")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1);

            // Inkguard Band
            _builder.Create(RecipeType.InkguardBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ig_band")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1);

            // Inkguard Guard
            _builder.Create(RecipeType.InkguardGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("ig_guard")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1);

            // Inkguard Visor
            _builder.Create(RecipeType.InkguardVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("ig_visor")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2)
                .Component("midink_sac", 1);

            // Inkguard Charm
            _builder.Create(RecipeType.InkguardCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ig_charm")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1);
        }
    }
}
