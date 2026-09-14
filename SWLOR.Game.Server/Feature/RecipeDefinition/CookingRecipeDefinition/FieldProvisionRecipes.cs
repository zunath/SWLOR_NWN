using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class FieldProvisionRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Brineleaf Chowder
            _builder.Create(RecipeType.BrineleafChowder, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("bl_chowder")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("viper_meat", 3)
                .Component("herb_m", 2)
                .Component("tide_scale", 1);

            // Field Ration Stew
            _builder.Create(RecipeType.FieldRationStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("fr_stew")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("herb_c", 3)
                .Component("distilled_water", 1)
                .Component("field_chip", 1);

            // Deepwatch Broth
            _builder.Create(RecipeType.DeepwatchBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dw_broth")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("c_butter", 2)
                .Component("distilled_water", 1)
                .Component("command_key", 1);

            // Midnight Ink Noodles
            _builder.Create(RecipeType.MidnightInkNoodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mi_noodles")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("mtench_tentacle", 3)
                .Component("distilled_water", 1)
                .Component("midink_sac", 1);

            // Shatterfin Curry
            _builder.Create(RecipeType.ShatterfinCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sf_curry")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("scorch_tail", 2)
                .Component("herb_m", 2)
                .Component("sh_chitin", 1);
        }
    }
}
