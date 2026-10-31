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

        status.Hardeners[15].Should().Be(Now.AddSeconds(4.5));
        SpaceCombatMath.HardenedResistance(20, status.GetHardenedAmount(Now.AddSeconds(4.4))).Should().Be(35);
        SpaceCombatMath.HardenedResistance(20, status.GetHardenedAmount(Now.AddSeconds(4.6))).Should().Be(20);
        SpaceCombatMath.HardenedResistance(20, 0).Should().Be(20, "no hardener, no bonus");
        new ShipStatus { Hardeners = null }.GetHardenedAmount(Now).Should().Be(0, "saved data without hardeners reads as none");
    }

    [Test]
    public void ShieldHardener_RunningTwiceKeepsTheStrongerBonus_AndAWeakerOneNeverExtendsIt()
    {
        var status = new ShipStatus();

        status.Harden(19, 4.0, Now);
        status.Harden(11, 2.0, Now.AddSeconds(1));
        status.GetHardenedAmount(Now.AddSeconds(2)).Should().Be(19);

        // The weaker hardener keeps cycling every 2 s; the stronger one stopped after its cycle at 0 s.
        status.Harden(11, 2.0, Now.AddSeconds(3));
        status.GetHardenedAmount(Now.AddSeconds(4)).Should().Be(19, "the stronger cycle still runs until 4.5 s");
        status.Hardeners[19].Should().Be(Now.AddSeconds(4.5), "the weaker hardener did not extend it");

        status.Harden(11, 2.0, Now.AddSeconds(5));
        status.GetHardenedAmount(Now.AddSeconds(5)).Should().Be(11, "a lapsed hardener no longer holds the old bonus");
        status.GetHardenedAmount(Now.AddSeconds(8)).Should().Be(0);
    }

    [Test]
    public void EWarPenalty_LowersAccuracyOnlyWhileItLasts()
    {
        var status = new ShipStatus { AccuracyPenalties = new() { [-12] = Now.AddSeconds(4.5) } };

        status.GetAccuracyPenalty(Now).Should().Be(-12);
        status.GetAccuracyPenalty(Now.AddSeconds(5)).Should().Be(0);
        new ShipStatus().GetAccuracyPenalty(Now).Should().Be(0);
    }

    [Test]
    public void BulwarkAura_RaisesMaxShieldOnlyWhileItLasts()
    {
        var status = new ShipStatus { MaxShield = 200, ShieldAuras = new() { [15] = Now.AddSeconds(4.5) } };

        status.GetEffectiveMaxShield(Now).Should().Be(230);
        status.GetEffectiveMaxShield(Now.AddSeconds(5)).Should().Be(200);
        new ShipStatus { MaxShield = 200 }.GetEffectiveMaxShield(Now).Should().Be(200);
    }
}
