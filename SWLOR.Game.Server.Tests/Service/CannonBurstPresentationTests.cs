using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class CannonBurstPresentationTests
{
    [TestCase((byte)0, true)]
    [TestCase((byte)5, true)]
    [TestCase((byte)6, false)]
    [TestCase((byte)7, false)]
    public void OnlyWeaponProjectilesBurst(byte type, bool expected) =>
        CannonBurstPresentation.IsWeaponProjectile(type).Should().Be(expected);

    [TestCase(BaseItem.Cannon, true)]
    [TestCase(BaseItem.Rifle, false)]
    [TestCase(BaseItem.HeavyRifle, false)]
    [TestCase(BaseItem.Pistol, false)]
    public void OnlyCannonsBurst(BaseItem item, bool expected) =>
        CannonBurstPresentation.IsBurstWeapon(item).Should().Be(expected);

    [Test]
    public void NothingEquippedDoesNotBurst() =>
        CannonBurstPresentation.IsBurstWeapon(null).Should().BeFalse();

    [Test]
    public void ABurstHasTwoTrailingProjectilesAtOneHundredMillisecondIntervals()
    {
        CannonBurstPresentation.FollowUpDelays.Should().Equal(
            TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200));
    }
}
