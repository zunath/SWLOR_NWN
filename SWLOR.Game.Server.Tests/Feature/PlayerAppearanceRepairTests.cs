using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerAppearanceRepairTests
{
    [TestCase(22, 5, 0, 0)]
    [TestCase(22, 0, 0, 0)]
    [TestCase(22, 1, 0, 1)]
    [TestCase(0, 4, 0, 0)]
    [TestCase(2, 5, 2, 0)]
    [TestCase(110, 6, 110, 0)]
    [TestCase(0, 0, 0, 0)]
    [TestCase(2, 1, 2, 1)]
    [TestCase(110, 1, 110, 1)]
    public void NormalizesRetiredRidingIdsWithoutChangingOtherAppearances(
        int phenotype, int tail, int expectedPhenotype, int expectedTail)
    {
        var normalized = PlayerAppearanceRepair.Normalize((PhenoType)phenotype, (TailType)tail);

        normalized.Should().Be(((PhenoType)expectedPhenotype, (TailType)expectedTail));
        PlayerAppearanceRepair.Normalize(normalized.Phenotype, normalized.Tail).Should().Be(normalized);
    }
}
