using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CityBountyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The words of the bounty system. The Gazette is public, so it names only the identity a stranger sees.
/// </summary>
public class BountyTextTests
{
    private static readonly DateTime Expires = new(2026, 11, 9, 12, 30, 0, DateTimeKind.Utc);

    private static IEnumerable<string> AllTexts()
    {
        yield return BountyText.BuildNoticeText();
        yield return BountyText.BuildPostedText("a hooded Zabrak", 25_000, Expires);
        yield return BountyText.BuildClaimedText("a hooded Zabrak", "a tall Twi'lek", 25_000);
        yield return BountyText.BuildExpiredText("a hooded Zabrak", 25_000);
        yield return BountyText.BuildCancelledText("a hooded Zabrak", 25_000);
        yield return BountyText.BuildTargetNotice("Ashford", 25_000, Expires);
        yield return BountyText.BuildClaimerNotice("a hooded Zabrak", 25_000);
        yield return BountyText.BuildPostedText(string.Empty, 1_000, Expires);
    }

    [Test]
    public void EveryText_IsPlainAscii()
    {
        foreach (var text in AllTexts())
            text.Should().MatchRegex("^[\\x20-\\x7E\\n]+$", text);
    }

    [Test]
    public void ThePostedLine_NamesTheIdentityTheRewardAndTheEnd()
    {
        BountyText.BuildPostedText("a hooded Zabrak", 25_000, Expires)
            .Should().Be("A bounty of 25,000 credits was posted on a hooded Zabrak. It runs until 2026-11-09 12:30 UTC.");
    }

    [Test]
    public void TheClaimedLine_NamesBothByHowStrangersSeeThem()
    {
        BountyText.BuildClaimedText("a hooded Zabrak", "a tall Twi'lek", 25_000)
            .Should().Be("A tall Twi'lek collected the bounty of 25,000 credits on a hooded Zabrak.");
    }

    [Test]
    public void AnUnnamedIdentity_ReadsAsSomeone()
    {
        BountyText.BuildPostedText(string.Empty, 1_000, Expires).Should().Contain("posted on someone.");
        BountyText.BuildClaimedText("a hooded Zabrak", " ", 1_000).Should().StartWith("Someone collected");
    }

    [Test]
    public void ExpiredAndCancelledLines_SayTheRewardReturned()
    {
        BountyText.BuildExpiredText("a hooded Zabrak", 5_000).Should().Contain("ran out unclaimed").And.Contain("returned to the treasury");
        BountyText.BuildCancelledText("a hooded Zabrak", 5_000).Should().Contain("withdrawn").And.Contain("returned to the treasury");
    }

    [Test]
    public void TheTargetsPrivateNotice_NamesTheCityTheRewardAndHowToOptOut()
    {
        var notice = BountyText.BuildTargetNotice("Ashford", 25_000, Expires);

        notice.Should().StartWith("Ashford posted a bounty of 25,000 credits on you.");
        notice.Should().Contain("2026-11-09 12:30 UTC");
        notice.Should().Contain("PvP flag");
    }

    [Test]
    public void TheNotice_ExplainsWhoCollects_WhatIsExcluded_AndThatItIsPublic()
    {
        var notice = BountyText.BuildNoticeText();

        notice.Should().Contain("never who is behind a disguise");
        notice.Should().Contain("never in a duel");
        notice.Should().Contain("CD key");
        notice.Should().Contain("once a week");
        notice.Should().Contain("Gazette");
        notice.Should().Contain("Until you accept, no city can post a bounty");
    }
}
