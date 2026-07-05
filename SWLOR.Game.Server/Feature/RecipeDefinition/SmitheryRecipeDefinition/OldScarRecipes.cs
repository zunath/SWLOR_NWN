using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class OldScarRecipes: IRecipeListDefinition
    {
        private const int RecipeLevel = 8;
        private const string Trophy = "oldscar_troph";

        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            CreateRecipe(RecipeType.OldScarHideVest, RecipeCategoryType.Tunic, "os_hidevest", 4, 2);
            CreateRecipe(RecipeType.OldScarWraps, RecipeCategoryType.Glove, "os_scarwraps", 2, 1);
            CreateRecipe(RecipeType.OldScarTreads, RecipeCategoryType.Boots, "os_treads", 2, 1);
            CreateRecipe(RecipeType.OldScarSash, RecipeCategoryType.Belt, "os_sash", 2, 1);
            CreateRecipe(RecipeType.OldScarMantle, RecipeCategoryType.Cloak, "os_mantle", 2, 1);
            CreateRecipe(RecipeType.OldScarCollar, RecipeCategoryType.Necklace, "os_collar", 2, 1);
            CreateRecipe(RecipeType.OldScarBand, RecipeCategoryType.Ring, "os_band", 2, 1);
            CreateRecipe(RecipeType.OldScarGuard, RecipeCategoryType.Bracer, "os_guard", 2, 1);
            CreateRecipe(RecipeType.OldScarVisor, RecipeCategoryType.Cap, "os_visor", 3, 2);
            CreateRecipe(RecipeType.OldScarCharm, RecipeCategoryType.Necklace, "os_charm", 2, 1);
            CreateRecipe(RecipeType.OldScarTrophyBand, RecipeCategoryType.Ring, "os_trophy", 2, 1);
            CreateRecipe(RecipeType.OldScarHideband, RecipeCategoryType.Belt, "os_hideband", 2, 1);
        }

        private void CreateRecipe(
            RecipeType recipe,
            RecipeCategoryType category,
            string resref,
            int leather,
            int fiberplast)
        {
            _builder.Create(recipe, SkillType.Smithery)
                .Category(category)
                .Resref(resref)
                .Level(RecipeLevel)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", leather)
                .Component("fiberp_ruined", fiberplast)
                .Component(Trophy, 1);
        }
    }
}
