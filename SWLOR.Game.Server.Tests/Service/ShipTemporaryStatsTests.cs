using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipTemporaryStatsTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void RepeatedControl_UsesFullHalfImmuneAndResetsAfterTwentySeconds()
    {
        var target = new ShipStatus();
        ShipTemporaryStats.ControlDuration(target, "engine", 4, false, Now).Should().Be(4);
        ShipTemporaryStats.ControlDuration(target, "engine", 4, false, Now.AddSeconds(1)).Should().Be(2);
        ShipTemporaryStats.ControlDuration(target, "engine", 4, false, Now.AddSeconds(2)).Should().Be(0);
        ShipTemporaryStats.ControlDuration(target, "engine", 4, false, Now.AddSeconds(19)).Should().Be(0);
        target = Newtonsoft.Json.JsonConvert.DeserializeObject<ShipStatus>(Newtonsoft.Json.JsonConvert.SerializeObject(target))!;
        ShipTemporaryStats.ControlDuration(target, "engine", 4, false, Now.AddSeconds(20)).Should().Be(4);
        ShipTemporaryStats.ControlDuration(target, "hard", 9, true, Now).Should().Be(3);
    }

    [Test]
    public void RepeatedFamilyEffects_DoNotMultiplyControlAndAllDistinctDownsidesRemain()
    {
        var target = new ShipStatus();
        ShipTemporaryStats.Add(target, StatType.ShipAccuracy, -.12, 5, "interference", Now);
        ShipTemporaryStats.Add(target, StatType.ShipAccuracy, -.08, 5, "interference", Now);
        ShipTemporaryStats.Add(target, StatType.ShipAccuracy, -.05, 5, "tradeoff", Now);
        ShipTemporaryStats.Add(target, StatType.ShipSpeed, .2, 5, "maneuver", Now);
        ShipTemporaryStats.Add(target, StatType.ShipSpeed, .3, 5, "thrust", Now);
        var current = ShipTemporaryStats.Current(target, Now.AddSeconds(1));
        current[StatType.ShipAccuracy].Should().BeApproximately(-.17, 1e-9);
        current[StatType.ShipSpeed].Should().Be(.3);
        ShipTemporaryStats.Current(target, Now.AddSeconds(5)).Should().BeEmpty();
    }

    [Test]
    public void ExternalRecovery_CapsEachPoolSeparatelyOverARollingThirtySeconds()
    {
        var target = new ShipStatus();
        target.ExternalRecoveryReceipts.Add(new(ShipResource.Hull, 30, Now));
        target.ExternalRecoveryReceipts.Add(new(ShipResource.Shield, 20, Now));
        ShipTemporaryStats.ExternalRecoveryAllowance(target, ShipResource.Hull, 100, 50, Now.AddSeconds(29)).Should().Be(10);
        ShipTemporaryStats.ExternalRecoveryAllowance(target, ShipResource.Shield, 100, 50, Now.AddSeconds(29)).Should().Be(20);
        ShipTemporaryStats.ExternalRecoveryAllowance(target, ShipResource.Hull, 100, 50, Now.AddSeconds(30)).Should().Be(40);
        Action capacitor = () => ShipTemporaryStats.ExternalRecoveryAllowance(target, ShipResource.Capacitor, 100, 50, Now);
        capacitor.Should().Throw<ArgumentException>();
    }
}
