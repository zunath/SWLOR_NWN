using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Tests.Perks;

public class AbilityChainSequenceTests
{
    [Test]
    public void DamageRiders_KeepIndependentOncePerCastBudgetsAcrossTargetsAndPulses()
    {
        var sequence = new AbilityImpactSequence();
        for (var pulse = 0; pulse < 5; pulse++)
        for (var target = 0; target < 8; target++)
        {
            sequence.TryTriggerDamageRider("cluster").Should().Be(pulse == 0 && target == 0);
            sequence.TryTriggerDamageRider("ricochet").Should().Be(pulse == 0 && target == 0);
        }
        sequence.TryTriggerAreaPulse().Should().BeTrue();
        new AbilityImpactSequence().TryTriggerDamageRider("ricochet").Should().BeTrue();
    }

    [Test]
    public void SparsePrimaryHits_ShareRemainingArcBudgetWithoutRepeatingTargets()
    {
        var sequence = new AbilityImpactSequence();
        sequence.HasRemainingChainArcs(0).Should().BeFalse();
        sequence.HasRemainingChainArcs(2).Should().BeTrue();
        // An isolated primary hit has no neighbors and consumes no arcs.
        // The next primary has one neighbor; a later primary can spend the remaining arc.
        sequence.TryConsumeChainArc(100, 2).Should().BeTrue();
        sequence.TryConsumeChainArc(100, 2).Should().BeFalse();
        sequence.HasRemainingChainArcs(2).Should().BeTrue();
        sequence.TryConsumeChainArc(200, 2).Should().BeTrue();
        sequence.HasRemainingChainArcs(2).Should().BeFalse();
        sequence.TryConsumeChainArc(300, 2).Should().BeFalse();
        new AbilityImpactSequence().TryConsumeChainArc(100, 2).Should().BeTrue();
    }
}
