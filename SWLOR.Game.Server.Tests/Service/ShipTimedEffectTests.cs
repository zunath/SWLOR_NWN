using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The timed bonuses a ship carries: Shield Hardener resistance, E-War Accuracy loss and Bulwark shield.
/// </summary>
public class ShipTimedEffectTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ShieldHardener_AddsItsBonusUntilTheCycleEndsPlusHalfASecond()
    {
        var status = new ShipStatus();

        status.Harden(15, 4.0, Now);

        status.HardenedUntil.Should().Be(Now.AddSeconds(4.5));
        SpaceCombatMath.HardenedResistance(20, status.HardenedAmount, status.HardenedUntil, Now.AddSeconds(4.4)).Should().Be(35);
        SpaceCombatMath.HardenedResistance(20, status.HardenedAmount, status.HardenedUntil, Now.AddSeconds(4.6)).Should().Be(20);
        SpaceCombatMath.HardenedResistance(20, 0, Now.AddSeconds(10), Now).Should().Be(20, "no hardener, no bonus");
    }

    [Test]
    public void ShieldHardener_RunningTwiceKeepsTheStrongerBonusAndTheLaterEnd()
    {
        var status = new ShipStatus();

        status.Harden(19, 4.0, Now);
        status.Harden(11, 2.0, Now.AddSeconds(1));

        status.HardenedAmount.Should().Be(19);
        status.HardenedUntil.Should().Be(Now.AddSeconds(4.5));

        status.Harden(11, 2.0, Now.AddSeconds(5));
        status.HardenedAmount.Should().Be(11, "a lapsed hardener no longer holds the old bonus");
        status.HardenedUntil.Should().Be(Now.AddSeconds(7.5));
    }

    [Test]
    public void EWarPenalty_LowersAccuracyOnlyWhileItLasts()
    {
        var status = new ShipStatus { AccuracyPenalty = -12, AccuracyPenaltyUntil = Now.AddSeconds(4.5) };

        status.GetAccuracyPenalty(Now).Should().Be(-12);
        status.GetAccuracyPenalty(Now.AddSeconds(5)).Should().Be(0);
        new ShipStatus().GetAccuracyPenalty(Now).Should().Be(0);
    }

    [Test]
    public void BulwarkAura_RaisesMaxShieldOnlyWhileItLasts()
    {
        var status = new ShipStatus { MaxShield = 200, ShieldAuraPercent = 15, ShieldAuraUntil = Now.AddSeconds(4.5) };

        status.GetEffectiveMaxShield(Now).Should().Be(230);
        status.GetEffectiveMaxShield(Now.AddSeconds(5)).Should().Be(200);
        new ShipStatus { MaxShield = 200 }.GetEffectiveMaxShield(Now).Should().Be(200);
    }
}
