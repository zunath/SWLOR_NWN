using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>Covers the component grades that set a recipe's overload line.</summary>
public class CircuitComponentGradesTests
{
    [TestCase("elec_ruined", 1)]
    [TestCase("elec_flawed", 2)]
    [TestCase("elec_good", 3)]
    [TestCase("elec_imperfect", 4)]
    [TestCase("elec_high", 5)]
    [TestCase("elec_perfect", 5)]
    [TestCase("lth_ruined", 1)]
    [TestCase("lth_high", 5)]
    [TestCase("fiberp_good", 3)]
    [TestCase("c_unit1", 1)]
    [TestCase("discharge_unit4", 4)]
    [TestCase("d_chassis5", 5)]
    [TestCase("diag_circuit2", 2)]
    [TestCase("dmotive_sys3", 3)]
    [TestCase("jade", 1)]
    [TestCase("ruby", 4)]
    [TestCase("emerald", 5)]
    [TestCase("quadrenium", 1)]
    [TestCase("zinsiam", 5)]
    [TestCase("ref_veldite", 1)]
    [TestCase("ref_currian", 2)]
    [TestCase("ref_barinium", 4)]
    [TestCase("ref_arda", 5)]
    [TestCase("capc_power", 6)]
    [TestCase("chiro_shard", 6)]
    [TestCase("sr_token", 0)]
    [TestCase("elec_unheard", 0)]
    [TestCase("c_unit6", 0)]
    [TestCase("", 0)]
    public void GradeOf_ReadsTheComponentsFamilyAndTier(string resref, int expected)
    {
        CircuitComponentGrades.GradeOf(resref).Should().Be(expected);
    }

    [TestCase(1, 1)]
    [TestCase(10, 1)]
    [TestCase(11, 2)]
    [TestCase(30, 3)]
    [TestCase(31, 4)]
    [TestCase(50, 5)]
    [TestCase(51, 6)]
    [TestCase(53, 6)]
    [TestCase(0, 1)]
    public void TierOfLevel_StepsEveryTenLevels_AndSixIsTheCapitalTier(int level, int tier)
    {
        CircuitComponentGrades.TierOfLevel(level).Should().Be(tier);
    }

    [Test]
    public void ARecipesGradeIsItsBestComponents_AndFallsBackToItsLevelsTierWhenNoneIsGraded()
    {
        CircuitComponentGrades.ForRecipe(30, new[] { "elec_flawed", "ref_idailia", "sr_token" }).Should().Be(3);
        CircuitComponentGrades.ForRecipe(44, new[] { "sr_token", "nv_pin" }).Should().Be(5);
        CircuitComponentGrades.ForRecipe(20, Array.Empty<string>()).Should().Be(2);
    }
}
