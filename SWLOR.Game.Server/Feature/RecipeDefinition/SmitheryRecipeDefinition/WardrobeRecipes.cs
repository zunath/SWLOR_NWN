using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    /// <summary>
    /// Pattern loom parts Vessa Thrane asks for in her wardrobe expansion quests, one per expansion.
    /// Each uses the standard fiberplast, leather and refined metal of its level.
    /// </summary>
    public class WardrobeRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Pattern Thread Spool
            _builder.Create(RecipeType.PatternThreadSpool, SkillType.Smithery)
                .Category(RecipeCategoryType.Crafting)
                .Resref("wd_thread_spool")
                .Level(5)
                .Quantity(1)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 1)
                .Component("ref_veldite", 1);

            // Pattern Loom Heddle
            _builder.Create(RecipeType.PatternLoomHeddle, SkillType.Smithery)
                .Category(RecipeCategoryType.Crafting)
                .Resref("wd_loom_heddle")
                .Level(15)
                .Quantity(1)
                .Component("ref_scordspar", 2)
                .Component("fiberp_flawed", 2)
                .Component("lth_flawed", 1);

            // Pattern Template Plates
            _builder.Create(RecipeType.PatternTemplatePlates, SkillType.Smithery)
                .Category(RecipeCategoryType.Crafting)
                .Resref("wd_tmpl_plates")
                .Level(25)
                .Quantity(1)
                .Component("ref_plagionite", 2)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1);

            // Loom Tension Weights
            _builder.Create(RecipeType.LoomTensionWeights, SkillType.Smithery)
                .Category(RecipeCategoryType.Crafting)
                .Resref("wd_tension_wts")
                .Level(35)
                .Quantity(1)
                .Component("ref_keromber", 2)
                .Component("fiberp_imperfect", 2)
                .Component("lth_imperfect", 1);

            // Pattern Index Tags
            _builder.Create(RecipeType.PatternIndexTags, SkillType.Smithery)
                .Category(RecipeCategoryType.Crafting)
                .Resref("wd_index_tags")
                .Level(45)
                .Quantity(1)
                .Component("ref_jasioclase", 2)
                .Component("fiberp_high", 2)
                .Component("lth_high", 1);
        }
    }
}
