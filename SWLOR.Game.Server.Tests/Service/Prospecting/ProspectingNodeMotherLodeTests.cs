using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using static SWLOR.Game.Server.Tests.Service.Prospecting.ProspectingTestData;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>Mother Lode quickens the first Strike or Heavy Strike of a node, not of a session, so the node remembers it was spent.</summary>
public class ProspectingNodeMotherLodeTests
{
    private static readonly ProspectingEffects MotherLode = new(MotherLode: true);

    [Test]
    public void ANodeRemembersThatMotherLodeWasSpentOnIt_ThroughItsStoredText()
    {
        var node = new ProspectingNode(3, 80, Node(3, "rG").Seams, motherLodeSpent: true);

        var text = node.Serialize();

        text.Should().Be("P3S80:rG*");
        ProspectingNode.TryParse(text, out var parsed).Should().BeTrue();
        parsed.MotherLodeSpent.Should().BeTrue();
        parsed.Should().Be(node);
        parsed.Equals(node with { }).Should().BeTrue();
    }

    [Test]
    public void ANodeStoredBeforeMotherLodeExistedReadsAsOneItWasNeverSpentOn()
    {
        ProspectingNode.TryParse("P3S85:rG", out var parsed).Should().BeTrue();

        parsed.MotherLodeSpent.Should().BeFalse();
        parsed.Serialize().Should().Be("P3S85:rG");
    }

    [Test]
    public void ANodeWorkedByMotherLodeIsNotQuickAgainWhenItIsReopened()
    {
        var node = Node(4, "");
        var first = new ProspectingEngine(new ScriptedMinigameRandom(1, 1), ProspectingSetup.ForNode(node, 20, 0, 1, MotherLode));
        first.SecondsFor(ProspectingActionIds.Strike).Should().BeLessThan(ProspectingRules.StrikeSeconds);

        first.Perform(ProspectingActionIds.Strike);

        var left = first.ToNode();
        left.MotherLodeSpent.Should().BeTrue();

        var again = new ProspectingEngine(new ScriptedMinigameRandom(1), ProspectingSetup.ForNode(left, 20, 0, 1, MotherLode));
        again.MotherLodeSpent.Should().BeTrue();
        again.SecondsFor(ProspectingActionIds.Strike).Should().Be(ProspectingRules.StrikeSeconds);
        again.SecondsFor(ProspectingActionIds.HeavyStrike).Should().Be(ProspectingRules.HeavyStrikeSeconds);
    }

    [Test]
    public void TakingUnitsWithoutASessionKeepsTheFlag()
    {
        var node = new ProspectingNode(4, 100, Node(4, "r").Seams, motherLodeSpent: true);

        node.Take(1).MotherLodeSpent.Should().BeTrue();
    }
}
