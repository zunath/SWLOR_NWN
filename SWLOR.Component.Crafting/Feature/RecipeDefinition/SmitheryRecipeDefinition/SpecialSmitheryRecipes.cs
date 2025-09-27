using SWLOR.Component.Crafting.Contracts;

using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Crafting.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Crafting.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class SpecialSmitheryRecipes: IRecipeListDefinition
    {
                private readonly IRecipeBuilder _builder;

        public SpecialSmitheryRecipes(IRecipeBuilder builder)
        {
            _builder = builder;
        }

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            SpecialRecipes();

            return _builder.Build();
        }

        private void SpecialRecipes()
        {
            // Weapon Submission Token (Smithery)
            _builder.Create(RecipeType.WeaponSubmissionTokenSmithery, SkillType.Smithery)
                .Category(RecipeCategoryType.SpecialSubmissionItems)
                .Resref("wpn_sub_token")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("chiro_shard", 5);

            // Armor Submission Token (Smithery)
            _builder.Create(RecipeType.ArmorSubmissionTokenSmithery, SkillType.Smithery)
                .Category(RecipeCategoryType.SpecialSubmissionItems)
                .Resref("arm_sub_token")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("chiro_shard", 5);
        }
    }
}
