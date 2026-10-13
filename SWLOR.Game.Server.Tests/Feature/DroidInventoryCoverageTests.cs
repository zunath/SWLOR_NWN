using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.EngineTests.Definitions;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class DroidInventoryCoverageTests
{
    [TestCase(nameof(DroidInventoryEngineTests.LegacyInventoryIdentity))]
    [TestCase(nameof(DroidInventoryEngineTests.EquipmentRoundTrip))]
    public void EquipmentPersistenceScenariosKeepBothDiscoveryLabels(string scenario)
    {
        var attribute = typeof(DroidInventoryEngineTests)
            .GetMethod(scenario, BindingFlags.Public | BindingFlags.Static)!
            .GetCustomAttribute<EngineTestAttribute>()!;

        attribute.Name.Should().StartWith("Migration ");
        attribute.Category.Should().Be("DroidInventory");
    }
}
