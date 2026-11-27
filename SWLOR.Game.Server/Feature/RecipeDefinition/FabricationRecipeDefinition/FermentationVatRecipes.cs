using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    /// <summary>
    /// The Fermentation Vat of the Player City Revamp ("Farming for other crafts"), a Fabrication structure like the Cookpot: it is
    /// the other crafting station of Agriculture, and the cultures it makes are Agriculture level 20 work, so it is built at the
    /// Cookpot's level with the Cookpot's bill. The enzyme cultures themselves are not recipes: a culture is a 2 day job, picked
    /// at the vat, and its inputs and XP are <c>EnzymeCultureRules</c>.
    /// </summary>
    public class FermentationVatRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            // Fermentation Vat: the Cookpot's level and bill
            _builder.Create(RecipeType.FermentationVat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_5105")
                .Level(25)
                .Quantity(1)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3)
                .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 33, frameQualityPercent: 31, shellQualityPercent: 36, wearRate: 7);

            return _builder.Build();
        }
    }
}
