using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>Covers the split of waiting Search finds into what a return delivered and what has no room, which the engine side relies on.</summary>
public class ScavengeReturnTests
{
    private static readonly Dictionary<string, int> Wanted = new() { ["fiberp_ruined"] = 3, ["ruby"] = 1 };

    [Test]
    public void WhenEverythingFits_EverythingIsDeliveredAndNothingIsLeft()
    {
        var split = ScavengeReturn.Divide(Wanted, (_, quantity) => quantity);

        split.Delivered.Should().BeEquivalentTo(Wanted);
        split.Remainder.Should().BeEmpty();
    }

    [Test]
    public void WhenPartOfAStackFits_TheRestStaysWaiting()
    {
        var split = ScavengeReturn.Divide(Wanted, (resref, quantity) => resref == "fiberp_ruined" ? 2 : quantity);

        split.Delivered.Should().BeEquivalentTo(new Dictionary<string, int> { ["fiberp_ruined"] = 2, ["ruby"] = 1 });
        split.Remainder.Should().BeEquivalentTo(new Dictionary<string, int> { ["fiberp_ruined"] = 1 });
    }

    [Test]
    public void WhenNothingFits_EverythingStaysWaiting_AndNothingIsDelivered()
    {
        var split = ScavengeReturn.Divide(Wanted, (_, _) => 0);

        split.Delivered.Should().BeEmpty();
        split.Remainder.Should().BeEquivalentTo(Wanted);
    }

    [Test]
    public void ADeliveryReportBeyondTheRange_IsClamped_SoNothingIsMadeOrLost()
    {
        var split = ScavengeReturn.Divide(Wanted, (resref, quantity) => resref == "ruby" ? -5 : quantity + 10);

        split.Delivered.Should().BeEquivalentTo(new Dictionary<string, int> { ["fiberp_ruined"] = 3 });
        split.Remainder.Should().BeEquivalentTo(new Dictionary<string, int> { ["ruby"] = 1 });
    }

    [Test]
    public void DeliveredAndRemainderAlwaysAddUpToWhatWasWanted()
    {
        for (var fit = 0; fit <= 4; fit++)
        {
            var split = ScavengeReturn.Divide(Wanted, (_, quantity) => Math.Min(quantity, fit));

            var total = ScavengeReturn.Merge(split.Delivered, split.Remainder);

            total.Should().BeEquivalentTo(Wanted, $"{fit} fit per resref");
        }
    }

    [Test]
    public void MergeAddsTheSameResrefOfSeveralListsAndSkipsEmptyOnes()
    {
        var merged = ScavengeReturn.Merge(
            new Dictionary<string, int> { ["ruby"] = 1, ["gold"] = 0 },
            null,
            new Dictionary<string, int> { ["ruby"] = 2, ["fiberp_ruined"] = 4 });

        merged.Should().BeEquivalentTo(new Dictionary<string, int> { ["ruby"] = 3, ["fiberp_ruined"] = 4 });
    }

    [Test]
    public void DescribeUsesTheItemNamesWhereKnown()
    {
        var text = ScavengeReturn.Describe(
            new Dictionary<string, int> { ["ruby"] = 1, ["fiberp_ruined"] = 3 },
            new Dictionary<string, string> { ["ruby"] = "Ruby" });

        text.Should().Be("1x Ruby, 3x fiberp_ruined");
    }
}
