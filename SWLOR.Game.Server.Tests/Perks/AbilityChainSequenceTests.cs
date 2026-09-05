using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Tests.Perks;

public class AbilityChainSequenceTests
{
    [Test]
    public void SparsePrimaryHits_ShareRemainingArcBudgetWithoutRepeatingTargets()
    {
        var sequence = new AbilityImpactSequence();
        // An isolated primary hit has no neighbors and consumes no arcs.
        // The next primary has one neighbor; a later primary can spend the remaining arc.
        sequence.TryConsumeChainArc(100, 2).Should().BeTrue();
        sequence.TryConsumeChainArc(100, 2).Should().BeFalse();
        sequence.TryConsumeChainArc(200, 2).Should().BeTrue();
        sequence.TryConsumeChainArc(300, 2).Should().BeFalse();
        new AbilityImpactSequence().TryConsumeChainArc(100, 2).Should().BeTrue();
    }
}
