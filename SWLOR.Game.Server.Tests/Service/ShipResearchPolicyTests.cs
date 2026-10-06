using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Tests.Service;
public class ShipResearchPolicyTests
{
    [Test] public void DurationAndSuccess_UsePublishedBounds()
    {
        ShipResearchPolicy.Duration(0,0).Should().Be(300);
        ShipResearchPolicy.Duration(50,0).Should().Be(1800);
        ShipResearchPolicy.Duration(20,10).Should().Be(810);
        ShipResearchPolicy.SuccessChance().Should().Be(.8);
        ShipResearchPolicy.SuccessChance(1).Should().Be(.95);
        ShipResearchPolicy.SuccessChance(-1).Should().Be(.1);
    }
    [Test] public void ResearchFailure_RetainsLevelAndExistingRunsWithoutExtraTuning()
    {
        var bp = new BlueprintDetail { Level=4,LicensedRuns=3,EnhancementSlots=5,ItemBonuses=3 };
        ShipResearchPolicy.Upgrade(bp,false,5,0,10);
        bp.Level.Should().Be(4);bp.LicensedRuns.Should().Be(3);
        bp.EnhancementSlots.Should().Be(0);bp.ItemBonuses.Should().Be(0);
    }
    [Test] public void SuccessfulLevels_ImproveEconomyWithoutMultiplyingRefinementSlots()
    {
        var bp = new BlueprintDetail();
        for(var i=0;i<10;i++) ShipResearchPolicy.Upgrade(bp,true,5,i%2,10);
        bp.Level.Should().Be(10);bp.LicensedRuns.Should().Be(10);
        (bp.CreditReduction+bp.TimeReduction).Should().Be(40);
        bp.EnhancementSlots.Should().Be(0);bp.GuaranteedBonuses.Should().BeEmpty();
    }
}
