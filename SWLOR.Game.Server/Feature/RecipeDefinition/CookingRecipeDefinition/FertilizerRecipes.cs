using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    /// <summary>
    /// Fertilizer I to V, the Agriculture recipes that feed the Farmer line. Each tier is made from Distilled Water and either
    /// one fish of the tier's level (Denizanasi 5, Greedie 15, Cheval Salmon 25, Tiger Cod 35, Blowfish 45) or one manure of the
    /// tier. A recipe takes one set of components, so each tier has a recipe for each.
    /// </summary>
    public class FertilizerRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            FishFertilizers();
            ManureFertilizers();

            return _builder.Build();
        }

        private void FishFertilizers()
        {
            _builder.Create(RecipeType.FertilizerFish1, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer I (Fish)")
                .Resref("fertilizer_1")
                .Level(5)
                .Quantity(1)
                .Component("denizanasi", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 22, targetSweet: 8, targetSpice: 12, innerRadius: 11, outerRadius: 21, freshness: 86);

            _builder.Create(RecipeType.FertilizerFish2, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer II (Fish)")
                .Resref("fertilizer_2")
                .Level(15)
                .Quantity(1)
                .Component("greedie", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 29, targetSweet: 1, targetSpice: 8, innerRadius: 10, outerRadius: 23, freshness: 86);

            _builder.Create(RecipeType.FertilizerFish3, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer III (Fish)")
                .Resref("fertilizer_3")
                .Level(25)
                .Quantity(1)
                .Component("cheval_salmon", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 27, targetSweet: 4, targetSpice: 7, innerRadius: 6, outerRadius: 23, freshness: 90);

            _builder.Create(RecipeType.FertilizerFish4, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer IV (Fish)")
                .Resref("fertilizer_4")
                .Level(35)
                .Quantity(1)
                .Component("tiger_cod", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 25, targetSweet: 7, targetSpice: 4, innerRadius: 5, outerRadius: 18, freshness: 93);

            _builder.Create(RecipeType.FertilizerFish5, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer V (Fish)")
                .Resref("fertilizer_5")
                .Level(45)
                .Quantity(1)
                .Component("blowfish", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 26, targetSweet: 1, targetSpice: 6, innerRadius: 4, outerRadius: 18, freshness: 91);
        }

        private void ManureFertilizers()
        {
            _builder.Create(RecipeType.FertilizerManure1, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer I (Manure)")
                .Resref("fertilizer_1")
                .Level(5)
                .Quantity(1)
                .Component("manure_t1", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 17, targetSweet: 0, targetSpice: 12, innerRadius: 7, outerRadius: 18, freshness: 98);

            _builder.Create(RecipeType.FertilizerManure2, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer II (Manure)")
                .Resref("fertilizer_2")
                .Level(15)
                .Quantity(1)
                .Component("manure_t2", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 20, targetSweet: 4, targetSpice: 4, innerRadius: 7, outerRadius: 18, freshness: 98);

            _builder.Create(RecipeType.FertilizerManure3, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer III (Manure)")
                .Resref("fertilizer_3")
                .Level(25)
                .Quantity(1)
                .Component("manure_t3", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 28, targetSweet: 4, targetSpice: 12, innerRadius: 7, outerRadius: 23, freshness: 93);

            _builder.Create(RecipeType.FertilizerManure4, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer IV (Manure)")
                .Resref("fertilizer_4")
                .Level(35)
                .Quantity(1)
                .Component("manure_t4", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 27, targetSweet: 0, targetSpice: 12, innerRadius: 7, outerRadius: 25, freshness: 91);

            _builder.Create(RecipeType.FertilizerManure5, SkillType.Agriculture)
                .Category(RecipeCategoryType.Fertilizer)
                .Name("Fertilizer V (Manure)")
                .Resref("fertilizer_5")
                .Level(45)
                .Quantity(1)
                .Component("manure_t5", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 27, targetSweet: 5, targetSpice: 15, innerRadius: 5, outerRadius: 18, freshness: 97);
        }
    }
}
