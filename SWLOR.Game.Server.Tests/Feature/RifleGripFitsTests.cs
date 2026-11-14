using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

namespace SWLOR.Game.Server.Tests.Feature;

public class RifleGripFitsTests
{
    private static readonly Type Fits = typeof(WeaponFit).Assembly.GetType(
        "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.RifleGripFits")!;

    [TestCase(false)]
    [TestCase(true)]
    public void ReferenceRifleRetainsItsAuthoredPlacement(bool ready)
    {
        var fit = Placement(101, "pmh0_handr001", "pmh0_handl001", ready);
        fit.Translation.Length().Should().BeLessThan(.000001f);
        fit.Rotation.Length().Should().BeLessThan(.0001f);
        fit.Scale.Should().Be(1);
    }

    [Test]
    public void ProstheticUsesItsActualTipCentreInEachHold()
    {
        var idle = Placement(101, "pmo0_handr30153", "pmo0_handl153", false);
        var ready = Placement(101, "pmo0_handr30153", "pmo0_handl153", true);
        idle.Translation.X.Should().BeApproximately(-.10091517f, .000002f);
        idle.Translation.Y.Should().BeApproximately(.01336412f, .000002f);
        idle.Translation.Z.Should().BeApproximately(-.12003522f, .000002f);
        ready.Translation.X.Should().BeApproximately(-.13350788f, .000002f);
        ready.Translation.Y.Should().BeApproximately(-.04168537f, .000002f);
        ready.Translation.Z.Should().BeApproximately(-.06223492f, .000002f);
        ready.Scale.Should().Be(1);
        ready.Rotation.Length().Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RenderedHandIdentifiersAreCaseInsensitive(bool ready) =>
        Placement(101, "PMO0_HANDR30153", "pmo0_HANDL153", ready)
            .Should().Be(Placement(101, "pmo0_handr30153", "pmo0_handl153", ready));

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(9, true)]
    [TestCase(4, false)]
    [TestCase(157, false)]
    public void OnlyAimingAndShootingUseTheReadyHold(int animation, bool ready) =>
        ((bool)Fits.GetMethod("IsReadyAnimation", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { animation })!).Should().Be(ready);

    [TestCase("pmh0_handr001", "pfh0_handl001")]
    [TestCase("pmh0_handl001", "pmh0_handr001")]
    [TestCase("pmh0_handr001", "")]
    public void InvalidSupportSelectionDoesNotInventAFit(string hand, string support)
    {
        var arguments = new object[] { 101, hand, support, false, null! };
        ((bool)Fits.GetMethod("TryGetPosePlacement", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, arguments)!).Should().BeFalse();
        arguments[4].Should().Be(WeaponGripFit.Identity);
    }

    private static WeaponGripFit Placement(int model, string hand, string support, bool ready)
    {
        var arguments = new object[] { model, hand, support, ready, null! };
        ((bool)Fits.GetMethod("TryGetPosePlacement", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, arguments)!).Should().BeTrue();
        return (WeaponGripFit)arguments[4];
    }
}
