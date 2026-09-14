using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class TideguardRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Tideguard Harness
            _builder.Create(RecipeType.TideguardHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("tg_harness")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 4)
                .Component("fiberp_good", 2)
                .Component("tide_scale", 1);

            // Tideguard Wraps
            _builder.Create(RecipeType.TideguardWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("tg_wraps")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1);

            // Tideguard Treads
            _builder.Create(RecipeType.TideguardTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("tg_treads")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1);

            // Tideguard Sash
            _builder.Create(RecipeType.TideguardSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("tg_sash")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1);

            // Tideguard Mantle
            _builder.Create(RecipeType.TideguardMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("tg_mantle")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1);

            // Tideguard Gorget
            _builder.Create(RecipeType.TideguardGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tg_gorget")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1);

            // Tideguard Band
            _builder.Create(RecipeType.TideguardBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("tg_band")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1);

            // Tideguard Guard
            _builder.Create(RecipeType.TideguardGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("tg_guard")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1);

            // Tideguard Visor
            _builder.Create(RecipeType.TideguardVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("tg_visor")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .Component("tide_scale", 1);

            // Tideguard Charm
            _builder.Create(RecipeType.TideguardCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tg_charm")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1);
        }
    }
}
