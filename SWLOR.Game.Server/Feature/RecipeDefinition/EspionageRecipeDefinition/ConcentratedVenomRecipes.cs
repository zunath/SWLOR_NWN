using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EspionageRecipeDefinition
{
    public class ConcentratedVenomRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Add(RecipeType.WhisperthornConcentrate, "conc_poison_1", 1, 6, "kath_blood", "herb_v");
            Add(RecipeType.GlassfangConcentrate, "conc_poison_2", 2, 16, "raivor_blood", "herb_m");
            Add(RecipeType.TombsporeConcentrate, "conc_poison_3", 3, 26, "byysk_meat", "herb_c");
            Add(RecipeType.RimevenomConcentrate, "conc_poison_4", 4, 36, "sanddemon_meat", "herb_t");
            Add(RecipeType.NightrootConcentrate, "conc_poison_5", 5, 46, "wild_innards", "herb_x");
            return _builder.Build();
        }

        private void Add(RecipeType type, string resref, int tier, int level, string creatureMaterial, string herb)
        {
            _builder.Create(type, SkillType.Espionage)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.Poisoncraft, tier, "Poisoncraft")
                .Category(RecipeCategoryType.Poison)
                .Resref(resref)
                .Level(level)
                .Quantity(1)
                .Component(creatureMaterial, 1)
                .Component(herb, 2);
        }
    }
}
