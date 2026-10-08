using System.Collections.Generic;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EspionageRecipeDefinition
{
    public class VenomCoatingRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            // Venom Coating I
            _builder.Create(RecipeType.VenomCoating1, SkillType.Espionage)
                .RequirementPerk(PerkType.Poisoncraft, 1, "Poisoncraft")
                .Category(RecipeCategoryType.Poison)
                .Resref("poison_vial_1")
                .Level(3)
                .SkillUpToRank(Skill.GetEspionagePracticeRankLimit(PerkType.Poisoncraft, 1))
                .Quantity(5)
                .EffectDescription(VenomCoatingItemDefinition.BuildEffectSummary(1, false))
                .Component("kath_blood", 3)
                .Component("herb_v", 2);

            // Venom Coating II
            _builder.Create(RecipeType.VenomCoating2, SkillType.Espionage)
                .RequirementPerk(PerkType.Poisoncraft, 2, "Poisoncraft")
                .Category(RecipeCategoryType.Poison)
                .Resref("poison_vial_2")
                .Level(16)
                .SkillUpToRank(Skill.GetEspionagePracticeRankLimit(PerkType.Poisoncraft, 2))
                .Quantity(5)
                .EffectDescription(VenomCoatingItemDefinition.BuildEffectSummary(2, false))
                .Component("raivor_blood", 3)
                .Component("herb_m", 2);

            // Venom Coating III
            _builder.Create(RecipeType.VenomCoating3, SkillType.Espionage)
                .RequirementPerk(PerkType.Poisoncraft, 3, "Poisoncraft")
                .Category(RecipeCategoryType.Poison)
                .Resref("poison_vial_3")
                .Level(26)
                .SkillUpToRank(Skill.GetEspionagePracticeRankLimit(PerkType.Poisoncraft, 3))
                .Quantity(5)
                .EffectDescription(VenomCoatingItemDefinition.BuildEffectSummary(3, false))
                .Component("byysk_meat", 3)
                .Component("herb_c", 2);

            // Venom Coating IV
            _builder.Create(RecipeType.VenomCoating4, SkillType.Espionage)
                .RequirementPerk(PerkType.Poisoncraft, 4, "Poisoncraft")
                .Category(RecipeCategoryType.Poison)
                .Resref("poison_vial_4")
                .Level(36)
                .SkillUpToRank(Skill.GetEspionagePracticeRankLimit(PerkType.Poisoncraft, 4))
                .Quantity(5)
                .EffectDescription(VenomCoatingItemDefinition.BuildEffectSummary(4, false))
                .Component("sanddemon_meat", 3)
                .Component("herb_t", 2);

            // Venom Coating V
            _builder.Create(RecipeType.VenomCoating5, SkillType.Espionage)
                .RequirementPerk(PerkType.Poisoncraft, 5, "Poisoncraft")
                .Category(RecipeCategoryType.Poison)
                .Resref("poison_vial_5")
                .Level(46)
                .SkillUpToRank(Skill.GetEspionagePracticeRankLimit(PerkType.Poisoncraft, 5))
                .Quantity(5)
                .EffectDescription(VenomCoatingItemDefinition.BuildEffectSummary(5, false))
                .Component("wild_innards", 3)
                .Component("herb_x", 2);

            return _builder.Build();
        }
    }
}
