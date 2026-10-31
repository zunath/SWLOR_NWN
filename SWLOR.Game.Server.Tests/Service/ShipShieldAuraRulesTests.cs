using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Overlapping Bulwark Shield Generator auras: the strongest active one counts, and shield is granted only for max
/// shield an aura newly adds, so two generators cycling on one ally never become a repeatable heal.
/// </summary>
public class ShipShieldAuraRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    // Mk IX and Mk X generators: 15% and 20%, a 4 second cycle and an aura that lasts the cycle plus half a second.
    private const int MkIx = 15;
    private const int MkX = 20;
    private const double Cycle = 4.0;
    private const double AuraSeconds = Cycle + 0.5;

    [Test]
    public void TheFirstAura_GrantsTheMaxShieldItAdds()
    {
        var status = new ShipStatus { MaxShield = 200 };

        ShipShieldAuraRules.Refresh(status, MkX, Now.AddSeconds(AuraSeconds), Now).Should().Be(40);
        status.GetEffectiveMaxShield(Now).Should().Be(240);
    }

    [Test]
    public void OverlappingAuras_TheStrongestActiveOneCounts()
    {
        var status = new ShipStatus { MaxShield = 200 };

        ShipShieldAuraRules.Refresh(status, MkX, Now.AddSeconds(AuraSeconds), Now);
        ShipShieldAuraRules.Refresh(status, MkIx, Now.AddSeconds(AuraSeconds), Now).Should().Be(0);

        ShipShieldAuraRules.ActivePercent(status.ShieldAuras, Now).Should().Be(MkX);
        status.GetEffectiveMaxShield(Now).Should().Be(240);
    }

    [Test]
    public void TwoGeneratorsCyclingOnOneAlly_GrantShieldOnlyOnce()
    {
        var status = new ShipStatus { MaxShield = 200 };
        var granted = 0;

        for (var cycle = 0; cycle < 10; cycle++)
        {
            var now = Now.AddSeconds(cycle * Cycle);
            granted += ShipShieldAuraRules.Refresh(status, MkX, now.AddSeconds(AuraSeconds), now);
            granted += ShipShieldAuraRules.Refresh(status, MkIx, now.AddSeconds(AuraSeconds), now);
        }

        granted.Should().Be(40, "only the Mk X aura's first application adds max shield");
    }

    [Test]
    public void AStrongerAuraTakingOver_GrantsOnlyTheDifference()
    {
        var status = new ShipStatus { MaxShield = 200 };

        ShipShieldAuraRules.Refresh(status, MkIx, Now.AddSeconds(AuraSeconds), Now).Should().Be(30);
        ShipShieldAuraRules.Refresh(status, MkX, Now.AddSeconds(AuraSeconds), Now).Should().Be(10);
        status.GetEffectiveMaxShield(Now).Should().Be(240);
    }

    [Test]
    public void WhenTheStrongerAuraLapses_TheWeakerOneCarriesOnWithoutGrantingShield()
    {
        var status = new ShipStatus { MaxShield = 200 };
        ShipShieldAuraRules.Refresh(status, MkX, Now.AddSeconds(AuraSeconds), Now);
        ShipShieldAuraRules.Refresh(status, MkIx, Now.AddSeconds(AuraSeconds), Now);

        // The Mk X stops cycling; the Mk IX keeps going.
        var later = Now.AddSeconds(Cycle);
        ShipShieldAuraRules.Refresh(status, MkIx, later.AddSeconds(AuraSeconds), later).Should().Be(0);
        var afterLapse = Now.AddSeconds(AuraSeconds + 0.1);
        status.GetEffectiveMaxShield(afterLapse).Should().Be(230);

        var next = Now.AddSeconds(2 * Cycle);
        ShipShieldAuraRules.Refresh(status, MkIx, next.AddSeconds(AuraSeconds), next).Should().Be(0);
        status.ShieldAuras.Should().ContainKey(MkIx).And.NotContainKey(MkX, "a lapsed aura is dropped");
    }

    [Test]
    public void AnAuraThatLapsed_GrantsAgainWhenItReturns()
    {
        var status = new ShipStatus { MaxShield = 200 };
        ShipShieldAuraRules.Refresh(status, MkIx, Now.AddSeconds(AuraSeconds), Now);

        var later = Now.AddSeconds(10);
        status.GetEffectiveMaxShield(later).Should().Be(200);
        ShipShieldAuraRules.Refresh(status, MkIx, later.AddSeconds(AuraSeconds), later).Should().Be(30);
    }

    [Test]
    public void ARefresh_NeverShortensAnAura()
    {
        var status = new ShipStatus { MaxShield = 200 };
        ShipShieldAuraRules.Refresh(status, MkIx, Now.AddSeconds(10), Now);
        ShipShieldAuraRules.Refresh(status, MkIx, Now.AddSeconds(AuraSeconds), Now);

        status.ShieldAuras[MkIx].Should().Be(Now.AddSeconds(10));
    }

    [Test]
    public void SavedDataWithoutAuras_ReadsAsNone()
    {
        var status = new ShipStatus { MaxShield = 200, ShieldAuras = null };

        status.GetEffectiveMaxShield(Now).Should().Be(200);
        ShipShieldAuraRules.Refresh(status, MkIx, Now.AddSeconds(AuraSeconds), Now).Should().Be(30);
    }
}
