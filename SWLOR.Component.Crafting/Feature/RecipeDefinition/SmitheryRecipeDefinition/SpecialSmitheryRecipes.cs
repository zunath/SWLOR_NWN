using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Enums;
using SWLOR.Component.Crafting.Model;
using SWLOR.Component.Crafting.Service;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Crafting.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class SpecialSmitheryRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

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
