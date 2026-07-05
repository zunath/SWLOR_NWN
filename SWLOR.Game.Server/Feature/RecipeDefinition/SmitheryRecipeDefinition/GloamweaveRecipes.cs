using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class GloamweaveRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Gloamweave Harness
            _builder.Create(RecipeType.GloamweaveHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("ss_harness")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2)
                .Component("ss_silk", 1);

            // Gloamweave Wraps
            _builder.Create(RecipeType.GloamweaveWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("ss_wraps")
                .Level(13)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1);

            // Gloamweave Treads
            _builder.Create(RecipeType.GloamweaveTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("ss_treads")
                .Level(12)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1);

            // Gloamweave Sash
            _builder.Create(RecipeType.GloamweaveSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ss_sash")
                .Level(13)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1);

            // Gloamweave Mantle
            _builder.Create(RecipeType.GloamweaveMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ss_mantle")
                .Level(16)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1);

            // Gloamweave Gorget
            _builder.Create(RecipeType.GloamweaveGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ss_gorget")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1);

            // Gloamweave Band
            _builder.Create(RecipeType.GloamweaveBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ss_band")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1);

            // Gloamweave Guard
            _builder.Create(RecipeType.GloamweaveGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("ss_guard")
                .Level(17)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1);

            // Gloamweave Visor
            _builder.Create(RecipeType.GloamweaveVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("ss_visor")
                .Level(16)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("ss_silk", 1);

            // Gloamweave Charm
            _builder.Create(RecipeType.GloamweaveCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ss_charm")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1);
        }
    }
}
