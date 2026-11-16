using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Domain.Foundation;

/// <summary>The ship status is saved with the ship item, so it must start empty and survive a save and load unchanged.</summary>
[TestFixture]
public class ShipStatusTests
{
    [Test]
    public void ANewStatusHasNoModulesAndNothingActive()
    {
        var status = new ShipStatus();

        status.HighPowerModules.Should().BeEmpty();
        status.LowPowerModules.Should().BeEmpty();
        status.ConfigurationModules.Should().BeEmpty();
        status.ActiveModules.Should().BeEmpty();
        status.CapitalShip.Should().BeFalse();
    }

    [Test]
    public void AStatusSurvivesASaveAndLoad()
    {
        var recast = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var status = new ShipStatus
        {
            ItemTag = "ship_x",
            Shield = 10, Hull = 20, Capacitor = 30,
            MaxShield = 11, MaxHull = 21, MaxCapacitor = 31,
            ShieldCycle = 4, ShieldRechargeRate = 5,
            EMDamage = 6, ThermalDamage = 7, ExplosiveDamage = 8,
            Accuracy = 9, Evasion = 12,
            ThermalDefense = 13, ExplosiveDefense = 14, EMDefense = 15,
            Industrial = 16, CapitalShip = true, GlobalRecast = recast
        };
        status.HighPowerModules[1] = new ShipStatus.ShipStatusModule
        {
            ItemInstanceId = "i1", ItemTag = "laser", SerializedItem = "blob",
            RecastTime = recast, RecastStartedAt = recast.AddSeconds(-6), ModuleBonus = 3
        };
        status.LowPowerModules[2] = new ShipStatus.ShipStatusModule { ItemTag = "armor" };
        status.ConfigurationModules[3] = new ShipStatus.ShipStatusModule { ItemTag = "config" };
        status.ActiveModules.Add(1);

        var restored = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status))!;

        restored.Should().BeEquivalentTo(status);
        restored.HighPowerModules[1].ModuleBonus.Should().Be(3);
        restored.ActiveModules.Should().Contain(1);
    }
}
