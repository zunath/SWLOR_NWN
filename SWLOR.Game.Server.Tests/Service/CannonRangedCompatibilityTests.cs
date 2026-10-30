using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class CannonRangedCompatibilityTests
{
    [TestCase(BaseItem.Cannon, true)]
    [TestCase(BaseItem.Rifle, false)]
    [TestCase(BaseItem.HeavyRifle, false)]
    [TestCase(BaseItem.Shuriken, false)]
    public void OnlyCannonsKeepTheirAmmunition(BaseItem weapon, bool expected) =>
        CannonRangedCompatibility.UsesUnlimitedLauncherAmmunition(weapon).Should().Be(expected);

    [Test]
    public void CannonShotsReachClientsAsRifleBolts() =>
        CannonRangedCompatibility.ClientBaseItem(unchecked((byte)BaseItem.Cannon), BaseItem.Cannon)
            .Should().Be((byte)BaseItem.Rifle);

    [Test]
    public void OtherShotsKeepTheirBaseItem()
    {
        CannonRangedCompatibility.ClientBaseItem((byte)BaseItem.Rifle, BaseItem.Rifle).Should().Be((byte)BaseItem.Rifle);
        // Objects without a right-hand weapon send 0xff.
        CannonRangedCompatibility.ClientBaseItem(0xff, null).Should().Be(0xff);
    }
}
