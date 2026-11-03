using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    /// <summary>
    /// The two outdoor structures that hold a city's stores and travelers: the Market Stall and the Traveler's Lodge. Like the
    /// other city structures (<see cref="CityStructureRecipes"/>), each is built at the level and from the parts of the existing
    /// structure recipe it resembles, without the power unit, since a stall and a tent have no electrics.
    ///
    /// PROPOSAL - NEEDS THE OWNER'S APPROVAL: the levels and bills below.
    /// </summary>
    public class CityStallAndLodgeRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            // Market Stall: the level and construction parts of Small House Style 2 (const_parts 2, pow_supp_unit 1), without the power unit
            _builder.Create(RecipeType.MarketStall, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5102")
                .Level(27)
                .Quantity(1)
                .Component("const_parts", 2)
                .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 20, frameQualityPercent: 30, shellQualityPercent: 23, wearRate: 4);

            // Traveler's Lodge: the level and construction parts of Medium House Style 1 (r_const_parts 2, r_pow_supp_unit 1), without the power unit
            _builder.Create(RecipeType.TravelersLodge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5103")
                .Level(33)
                .Quantity(1)
                .Component("r_const_parts", 2)
                .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 28, frameQualityPercent: 22, shellQualityPercent: 22, wearRate: 8);

            return _builder.Build();
        }
    }
}
