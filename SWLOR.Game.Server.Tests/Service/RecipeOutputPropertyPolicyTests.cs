using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class RecipeOutputPropertyPolicyTests
{
    [Test]
    public void OrdinaryRecipes_RetainUnrestrictedEnhancements()
    {
        var recipe = new RecipeDetail();
        foreach (var type in Enum.GetValues<EnhancementSubType>()) recipe.AllowsEnhancement(type).Should().BeTrue();
        foreach (var type in Enum.GetValues<ItemPropertyType>()) recipe.AllowsOutputProperty(type).Should().BeTrue();
    }

    [Test]
    public void ProfileRestrictedRecipe_RejectsNativeAndGlobalBonusKits()
    {
        var builder = new RecipeBuilder();
        builder.Create(RecipeType.BasicPistol, SkillType.Smithery)
            .AllowedEnhancements(EnhancementSubType.DMG, EnhancementSubType.Accuracy)
            .AllowedOutputProperties(ItemPropertyType.DMG, ItemPropertyType.WeaponDamageType, ItemPropertyType.Accuracy);
        var recipe = builder.Build()[RecipeType.BasicPistol];
        recipe.AllowsEnhancement(EnhancementSubType.DMG).Should().BeTrue();
        recipe.AllowsEnhancement(EnhancementSubType.Accuracy).Should().BeTrue();
        recipe.AllowsEnhancement(EnhancementSubType.Perception).Should().BeFalse();
        recipe.AllowsEnhancement(EnhancementSubType.Attack).Should().BeFalse();
        recipe.AllowsOutputProperty(ItemPropertyType.AbilityBonus).Should().BeFalse();
        recipe.AllowsOutputProperty(ItemPropertyType.Attack).Should().BeFalse();
        recipe.AllowsOutputProperty(ItemPropertyType.WeaponDamageType).Should().BeTrue();
    }

    [Test]
    public void Restrictions_ArePerRecipeAndCannotLeakToFollowingRecipes()
    {
        var builder = new RecipeBuilder();
        builder.Create(RecipeType.BasicPistol, SkillType.Smithery).AllowedEnhancements(EnhancementSubType.DMG);
        builder.Create(RecipeType.TitanPistol, SkillType.Smithery);
        builder.Build()[RecipeType.TitanPistol].AllowsEnhancement(EnhancementSubType.Perception).Should().BeTrue();
    }
}
