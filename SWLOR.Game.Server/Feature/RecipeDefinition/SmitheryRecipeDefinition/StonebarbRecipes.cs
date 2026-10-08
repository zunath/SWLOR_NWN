using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class StonebarbRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Stonebarb Harness
            _builder.Create(RecipeType.StonebarbHarness, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Tunic)
                .Resref("gs_harness")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("gs_spine", 1);

            // Stonebarb Wraps
            _builder.Create(RecipeType.StonebarbWraps, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Glove)
                .Resref("gs_wraps")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1);

            // Stonebarb Treads
            _builder.Create(RecipeType.StonebarbTreads, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Boots)
                .Resref("gs_treads")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1);

            // Stonebarb Sash
            _builder.Create(RecipeType.StonebarbSash, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Belt)
                .Resref("gs_sash")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1);

            // Stonebarb Mantle
            _builder.Create(RecipeType.StonebarbMantle, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Cloak)
                .Resref("gs_mantle")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1);

            // Stonebarb Gorget
            _builder.Create(RecipeType.StonebarbGorget, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gs_gorget")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1);

            // Stonebarb Band
            _builder.Create(RecipeType.StonebarbBand, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Ring)
                .Resref("gs_band")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1);

            // Stonebarb Guard
            _builder.Create(RecipeType.StonebarbGuard, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Bracer)
                .Resref("gs_guard")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1);

            // Stonebarb Visor
            _builder.Create(RecipeType.StonebarbVisor, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Cap)
                .Resref("gs_visor")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("gs_spine", 1);

            // Stonebarb Charm
            _builder.Create(RecipeType.StonebarbCharm, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gs_charm")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1);
        }
    }
}
