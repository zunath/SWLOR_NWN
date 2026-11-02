using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>Covers how hard each Fabrication component is on the heads, which sets a recipe's wear rate.</summary>
public class FabricatorMaterialsTests
{
    [TestCase("wood", 1)]
    [TestCase("fine_wood", 2)]
    [TestCase("aracia_wood", 3)]
    [TestCase("hyphae_wood", 4)]
    [TestCase("ancient_wood", 5)]
    [TestCase("lth_ruined", 1)]
    [TestCase("lth_high", 5)]
    [TestCase("fiberp_good", 3)]
    [TestCase("fiberp_perfect", 5)]
    [TestCase("elec_imperfect", 4)]
    [TestCase("ref_veldite", 2)]
    [TestCase("ref_currian", 3)]
    [TestCase("ref_keromber", 5)]
    [TestCase("ref_arkoxit", 6)]
    [TestCase("emerald", 6)]
    [TestCase("pow_supp_unit", 3)]
    [TestCase("r_const_parts", 4)]
    [TestCase("sr_token", 2)]
    [TestCase("", 2)]
    public void HardnessOf_ReadsTheComponentsFamilyAndGrade(string resref, double expected)
    {
        FabricatorMaterials.HardnessOf(resref).Should().Be(expected);
    }

    [Test]
    public void ARecipesHardness_IsTheMeanOfItsComponentsByUnit()
    {
        var components = new Dictionary<string, int> { ["wood"] = 3, ["ref_keromber"] = 1 };

        FabricatorMaterials.ForRecipe(components).Should().BeApproximately((3 * 1 + 5) / 4.0, 1e-9);
        FabricatorMaterials.ForRecipe(new Dictionary<string, int>()).Should().Be(FabricatorMaterials.DefaultHardness);
    }

    [Test]
    public void HarderMaterialsWearTheHeadsFaster_InsideTheWearBounds()
    {
        FabricatorParameterProfile.WearForHardness(1).Should().BeLessThan(FabricatorParameterProfile.WearForHardness(3));
        FabricatorParameterProfile.WearForHardness(3).Should().BeLessThan(FabricatorParameterProfile.WearForHardness(6));
        FabricatorParameterProfile.WearForHardness(FabricatorMaterials.SoftestHardness)
            .Should().BeInRange(FabricatorParameterProfile.MinimumWear, FabricatorParameterProfile.MaximumWear);
        FabricatorParameterProfile.WearForHardness(FabricatorMaterials.HardestHardness)
            .Should().BeInRange(FabricatorParameterProfile.MinimumWear, FabricatorParameterProfile.MaximumWear);
    }
}
