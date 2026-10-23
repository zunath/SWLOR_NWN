using System.Collections.Immutable;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using static SWLOR.Game.Server.Tests.Service.Prospecting.ProspectingTestData;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>Covers what a node holds between sessions: how a fresh one is made, how it is stored and what taking units from it leaves.</summary>
public class ProspectingNodeTests
{
    [Test]
    public void ANodeNobodyHasWorkedHoldsToday_sD4PoolFullStabilityAndHiddenSeams()
    {
        // The pool is Next(1, 5); then Next(1, 101) picks the seam count; then a d100 for each seam picks its kind.
        var node = ProspectingNode.Create(new ScriptedMinigameRandom(3, 10, 100), hasGems: true);

        node.Pool.Should().Be(3);
        node.Stability.Should().Be(ProspectingRules.MaxStability);
        node.Seams.Should().ContainSingle().Which.Should().Be(new ProspectingSeam(ProspectingSeamKind.Rich, false));
    }

    [TestCase(1, 1)]
    [TestCase(ProspectingRules.OneSeamWeight, 1)]
    [TestCase(ProspectingRules.OneSeamWeight + 1, 2)]
    [TestCase(ProspectingRules.OneSeamWeight + ProspectingRules.TwoSeamsWeight, 2)]
    [TestCase(ProspectingRules.OneSeamWeight + ProspectingRules.TwoSeamsWeight + 1, 3)]
    [TestCase(100, 3)]
    public void TheSeamCountFollowsTheWeights_OneToThree(int seamRoll, int expectedSeams)
    {
        var node = ProspectingNode.Create(new ScriptedMinigameRandom(4, seamRoll, 100, 100, 100), hasGems: false);

        node.Seams.Should().HaveCount(expectedSeams);
        node.Seams.Should().OnlyContain(seam => !seam.Revealed);
    }

    [Test]
    public void EachSeamSealsAUnit_SoANodeHoldsNoMoreSeamsThanUnits()
    {
        var node = ProspectingNode.Create(new ScriptedMinigameRandom(1, 100, 100, 100, 100), hasGems: false);

        node.Pool.Should().Be(1);
        node.Seams.Should().HaveCount(1, "three seams were rolled but one unit can seal only one");
        node.LooseUnits.Should().Be(0);

        var act = () => new ProspectingNode(1, 100, Node(2, "rr").Seams);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void AGemPocketNeedsGemEntriesInTheTable()
    {
        const int Pocket = ProspectingRules.GemPocketPercent;
        const int Rich = ProspectingRules.GemPocketPercent + 1;

        ProspectingNode.Create(new ScriptedMinigameRandom(4, 100, Pocket, Pocket, Pocket), hasGems: true)
            .Seams.Should().OnlyContain(seam => seam.Kind == ProspectingSeamKind.GemPocket);
        ProspectingNode.Create(new ScriptedMinigameRandom(4, 100, Rich, Rich, Rich), hasGems: true)
            .Seams.Should().OnlyContain(seam => seam.Kind == ProspectingSeamKind.Rich);
        ProspectingNode.Create(new ScriptedMinigameRandom(4, 100, Pocket, Pocket, Pocket), hasGems: false)
            .Seams.Should().OnlyContain(seam => seam.Kind == ProspectingSeamKind.Rich, "an arkoxit node has no gem entries to fill a pocket from");
    }

    [Test]
    public void ThePoolIsAFourSidedDieLikeTodaysHarvests()
    {
        ProspectingRules.PoolDie.Should().Be(4);
        for (var roll = 1; roll <= 4; roll++)
            ProspectingNode.Create(new ScriptedMinigameRandom(roll, 1, 100), hasGems: false).Pool.Should().Be(roll);

        ProspectingQuickHarvest.ExpectedUnits.Should().Be(2.5);
    }

    [TestCase(3, "", 100, "P3S100:")]
    [TestCase(4, "rG", 85, "P4S85:rG")]
    [TestCase(4, "gRG", 0, "P4S0:gRG")]
    public void ANodeIsStoredAsText_AndReadBackExactly(int pool, string seams, int stability, string expected)
    {
        var node = Node(pool, seams, stability);

        node.Serialize().Should().Be(expected);
        ProspectingNode.TryParse(expected, out var read).Should().BeTrue();
        read.Should().Be(node);
        read.Serialize().Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("garbage")]
    [TestCase("P3")]
    [TestCase("P3S50")]
    [TestCase("P3S101:")]
    [TestCase("P3S50:x")]
    [TestCase("P-3S50:")]
    [TestCase("P2S50:rrr")]
    [TestCase("P9S50:rrrr")]
    public void TextThatIsNotAStoredNodeIsRejected_SoAnUnworkedNodeIsMadeFresh(string text)
    {
        ProspectingNode.TryParse(text, out var node).Should().BeFalse();
        node.Should().BeNull();
    }

    [Test]
    public void TakingUnitsLeavesTheRestOfThePool_AndNoMoreSeamsThanUnits()
    {
        var node = Node(4, "rGg");

        var after = node.Take(2);

        after.Pool.Should().Be(2);
        after.Stability.Should().Be(100);
        after.Seams.Should().HaveCount(2, "the hidden seam went first, as a strike breaks hidden seams before revealed ones");
        after.Seams.Should().OnlyContain(seam => seam.Kind == ProspectingSeamKind.GemPocket || seam.Revealed);
        node.Take(4).IsSpent.Should().BeTrue();
        node.Take(5).Pool.Should().Be(0);
    }

    [Test]
    public void ALooseUnitIsOneNotSealedInASeam()
    {
        Node(4, "rG").LooseUnits.Should().Be(2);
        Node(2, "rg").LooseUnits.Should().Be(0);
    }

    [Test]
    public void StabilityMustBeInsideItsRange()
    {
        var act = () => new ProspectingNode(2, ProspectingRules.MaxStability + 1, ImmutableArray<ProspectingSeam>.Empty);
        act.Should().Throw<ArgumentOutOfRangeException>();

        var negative = () => new ProspectingNode(2, -1, ImmutableArray<ProspectingSeam>.Empty);
        negative.Should().Throw<ArgumentOutOfRangeException>();
    }
}
