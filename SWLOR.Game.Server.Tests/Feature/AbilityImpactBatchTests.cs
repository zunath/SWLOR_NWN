using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityImpactBatchTests
{
    [TestCase(3u, 1u, 2u, 3)]
    [TestCase(3u, 1u, uint.MaxValue, 2)]
    [TestCase(3u, 3u, 1u, 2)]
    [TestCase(uint.MaxValue, uint.MaxValue, uint.MaxValue, 0)]
    public void Arrivals_CompleteOneSummaryWithAllDistinctHits(uint first, uint second, uint third, int expectedTargets)
    {
        var impactType = typeof(Ability).GetNestedType("TrackedAbilityImpact", BindingFlags.NonPublic)!;
        var impact = impactType.GetConstructors().Single().Invoke(new object[]
        {
            new AbilityDetail { IsAreaAbility = true }, 0, 0, 0, 0, 0, false, 0,
            Array.Empty<TelegraphGeometry>(), new AbilityImpactSequence()
        });
        var summary = (AbilityImpactSummary)impactType.GetProperty("Summary")!.GetValue(impact)!;
        var completedSummaries = new List<AbilityImpactSummary>();
        var arrivals = new List<uint>();
        var batch = new AbilityImpactBatch<uint>(3, (target, isFinal) =>
        {
            arrivals.Add(target);
            if (target != uint.MaxValue)
                impactType.GetMethod("RecordTarget")!.Invoke(impact, new object[] { target });
            if (isFinal)
                completedSummaries.Add(summary);
        });

        batch.Apply(first);
        arrivals.Should().Equal(first);
        completedSummaries.Should().BeEmpty();
        batch.Apply(second);
        arrivals.Should().Equal(first, second);
        completedSummaries.Should().BeEmpty();
        batch.Apply(third);

        arrivals.Should().Equal(first, second, third);
        completedSummaries.Should().ContainSingle().Which.Should().BeSameAs(summary);
        summary.ImpactedTargetCount.Should().Be(expectedTargets);
        summary.IsAreaAbility.Should().BeTrue();
        summary.IsSingleTargetAbility.Should().BeFalse();
    }

    [Test]
    public void OverlappingPulses_CompleteIndependently()
    {
        var completions = new List<string>();
        var first = new AbilityImpactBatch<int>(2, (_, final) => { if (final) completions.Add("first"); });
        var second = new AbilityImpactBatch<int>(2, (_, final) => { if (final) completions.Add("second"); });
        first.Apply(1);
        second.Apply(1);
        second.Apply(2);
        completions.Should().Equal("second");
        first.Apply(2);
        completions.Should().Equal("second", "first");
    }

    [Test]
    public void FailedArrival_CancelsRemainingImpacts()
    {
        var calls = 0;
        var batch = new AbilityImpactBatch<int>(2, (_, _) =>
        {
            calls++;
            throw new InvalidOperationException("Impact failed");
        });
        var apply = () => batch.Apply(1);
        apply.Should().Throw<InvalidOperationException>();
        batch.Apply(2);
        calls.Should().Be(1);
    }

    [Test]
    public void CompletedBatch_CannotAwardAgain()
    {
        var completions = 0;
        var batch = new AbilityImpactBatch<int>(1, (_, final) => { if (final) completions++; });
        batch.Apply(1);
        var repeat = () => batch.Apply(1);
        repeat.Should().Throw<InvalidOperationException>();
        completions.Should().Be(1);
    }
}
