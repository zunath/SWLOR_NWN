using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    /// <summary>
    /// The outdoor structures of a city's farms and duels. Each is built at the level and from the parts of the existing structure
    /// recipe it resembles, without the power unit where the structure has no electrics.
    /// </summary>
    public class CityStructureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            // Garden Plot: the level of Small House Style 1 (const_parts 2, pow_supp_unit 1), with one construction part and no power unit
            _builder.Create(RecipeType.GardenPlot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5100")
                .Level(26)
                .Quantity(1)
                .Component("const_parts", 1)
                .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 20, frameQualityPercent: 21, shellQualityPercent: 23, wearRate: 5);

            // Dueling Ring: the level of Medium House Style 3 (r_const_parts 2, r_pow_supp_unit 1), with half its construction parts for a console and a painted ring
            _builder.Create(RecipeType.DuelingRing, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5101")
                .Level(35)
                .Quantity(1)
                .Component("r_const_parts", 1)
                .Component("r_pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 29, frameQualityPercent: 26, shellQualityPercent: 30, wearRate: 6);

            // Corral: the level and construction parts of Small House Style 4 (const_parts 2, pow_supp_unit 1), without the power unit
            _builder.Create(RecipeType.Corral, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5104")
                .Level(30)
                .Quantity(1)
                .Component("const_parts", 2)
                .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 29, frameQualityPercent: 26, shellQualityPercent: 25, wearRate: 6);

            return _builder.Build();
        }
    }
}
