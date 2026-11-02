using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The election voter reward: 10 RP points, once per CD key every 28 days across all cities, written to the ledger
/// with the first vote.
/// </summary>
public class VoterRewardTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Reward_IsTenRpPointsOncePerTwentyEightDays()
    {
        VoterRewardRules.RewardPoints.Should().Be(10);
        VoterRewardRules.IntervalDays.Should().Be(28);
    }

    [Test]
    public void Ledger_IsKeyedByTheCdKey()
    {
        new VoterRewardLedger("ABCD1234").Id.Should().Be("ABCD1234");
    }

    [Test]
    public void FirstVote_PaysAndWritesTheLedgerEntry()
    {
        var entry = VoterRewardRules.Claim(null, "KEY", Now);

        entry.Should().NotBeNull();
        entry.Id.Should().Be("KEY");
        entry.LastRewardUtc.Should().Be(Now);
    }

    [Test]
    public void ChangingOrWithdrawingAVoteAndVotingAgainNeverPaysAgain()
    {
        var ledger = VoterRewardRules.Claim(null, "KEY", Now);

        // The same player changes the vote an hour later, then withdraws it and votes again on day 6.
        VoterRewardRules.Claim(ledger, "KEY", Now.AddHours(1)).Should().BeNull();
        VoterRewardRules.Claim(ledger, "KEY", Now.AddDays(6)).Should().BeNull();
        ledger.LastRewardUtc.Should().Be(Now);
    }

    [Test]
    public void VotingInAnotherCityWithinTwentyEightDaysPaysNothing()
    {
        var ledger = VoterRewardRules.Claim(null, "KEY", Now);

        VoterRewardRules.Claim(ledger, "KEY", Now.AddDays(10)).Should().BeNull();
        VoterRewardRules.Claim(ledger, "KEY", Now.AddDays(28).AddSeconds(-1)).Should().BeNull();
    }

    [Test]
    public void AfterTwentyEightDaysTheNextVotePaysAndRestartsTheClock()
    {
        var ledger = VoterRewardRules.Claim(null, "KEY", Now);

        var second = VoterRewardRules.Claim(ledger, "KEY", Now.AddDays(28));

        second.Should().NotBeNull();
        second.LastRewardUtc.Should().Be(Now.AddDays(28));
        VoterRewardRules.Claim(second, "KEY", Now.AddDays(30)).Should().BeNull();
    }

    [Test]
    public void EachCdKeyIsPaidOnItsOwn()
    {
        var first = VoterRewardRules.Claim(null, "ONE", Now);
        var second = VoterRewardRules.Claim(null, "TWO", Now);

        first.Id.Should().Be("ONE");
        second.Id.Should().Be("TWO");
    }

    [Test]
    public void IsDue_WithNoEntryOrAnOldOne()
    {
        VoterRewardRules.IsDue(null, Now).Should().BeTrue();
        VoterRewardRules.IsDue(Now.AddDays(-28), Now).Should().BeTrue();
        VoterRewardRules.IsDue(Now.AddDays(-27), Now).Should().BeFalse();
    }
}
