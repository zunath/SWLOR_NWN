using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class PrismhideRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Prismhide Harness
            _builder.Create(RecipeType.PrismhideHarness, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Tunic)
                .Resref("se_harness")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("se_eye", 1);

            // Prismhide Wraps
            _builder.Create(RecipeType.PrismhideWraps, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Glove)
                .Resref("se_wraps")
                .Level(19)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1);

            // Prismhide Treads
            _builder.Create(RecipeType.PrismhideTreads, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Boots)
                .Resref("se_treads")
                .Level(18)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1);

            // Prismhide Sash
            _builder.Create(RecipeType.PrismhideSash, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Belt)
                .Resref("se_sash")
                .Level(19)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1);

            // Prismhide Mantle
            _builder.Create(RecipeType.PrismhideMantle, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Cloak)
                .Resref("se_mantle")
                .Level(22)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1);

            // Prismhide Gorget
            _builder.Create(RecipeType.PrismhideGorget, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Necklace)
                .Resref("se_gorget")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1);

            // Prismhide Band
            _builder.Create(RecipeType.PrismhideBand, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Ring)
                .Resref("se_band")
                .Level(21)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1);

            // Prismhide Guard
            _builder.Create(RecipeType.PrismhideGuard, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Bracer)
                .Resref("se_guard")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1);

            // Prismhide Visor
            _builder.Create(RecipeType.PrismhideVisor, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Cap)
                .Resref("se_visor")
                .Level(22)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("se_eye", 1);

            // Prismhide Charm
            _builder.Create(RecipeType.PrismhideCharm, SkillType.Smithery)
                .CraftingProfile(CraftProfile.Sturdy, CraftTechnique.None, pilot: false)
                .Category(RecipeCategoryType.Necklace)
                .Resref("se_charm")
                .Level(21)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1);
        }
    }
}
