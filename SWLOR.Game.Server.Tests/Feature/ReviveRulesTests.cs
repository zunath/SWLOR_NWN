using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class ReviveRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 20, 0, 0, DateTimeKind.Utc);

    [Test]
    public void PlayerNeverRevivedMidFight_IsNotLockedOut()
    {
        ReviveRules.IsWithinMidFightLockout(null, Now).Should().BeFalse();
    }

    [TestCase(0)]
    [TestCase(299)]
    public void MidFightReviveWithinFiveMinutes_IsLockedOut(int secondsAgo)
    {
        ReviveRules.IsWithinMidFightLockout(Now.AddSeconds(-secondsAgo), Now).Should().BeTrue();
    }

    [TestCase(300)]
    [TestCase(900)]
    public void MidFightReviveFiveMinutesAgoOrMore_IsNotLockedOut(int secondsAgo)
    {
        ReviveRules.IsWithinMidFightLockout(Now.AddSeconds(-secondsAgo), Now).Should().BeFalse();
    }

    [Test]
    public void NoPlayerKill_StartsNoPartyDamageWait()
    {
        ReviveRules.IsWithinPartyDamageWindow(null, Now).Should().BeFalse();
    }

    [TestCase(0)]
    [TestCase(14)]
    public void PlayerKillWithinFifteenSeconds_CountsAsRecentPartyDamage(int secondsAgo)
    {
        ReviveRules.IsWithinPartyDamageWindow(Now.AddSeconds(-secondsAgo), Now).Should().BeTrue();
    }

    [TestCase(15)]
    [TestCase(60)]
    public void PlayerKillFifteenSecondsAgoOrMore_NoLongerCounts(int secondsAgo)
    {
        ReviveRules.IsWithinPartyDamageWindow(Now.AddSeconds(-secondsAgo), Now).Should().BeFalse();
    }
}
