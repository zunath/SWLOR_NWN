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
    [TestCase(nameof(DroidInventoryEngineTests.LootRevisionSavedController))]
    [TestCase(nameof(DroidInventoryEngineTests.LootRevisionSummonedDroid))]
    [TestCase(nameof(DroidInventoryEngineTests.LootRevisionPlayerDroid))]
    [TestCase(nameof(DroidInventoryEngineTests.HeadgearSavedSlotFirst))]
    [TestCase(nameof(DroidInventoryEngineTests.HeadgearSavedStatsFirst))]
    [TestCase(nameof(DroidInventoryEngineTests.HeadgearSavedFreeHead))]
    [TestCase(nameof(DroidInventoryEngineTests.HeadgearSavedDeniedHead))]
    [TestCase(nameof(DroidInventoryEngineTests.HeadgearSavedPendingChest))]
    [TestCase(nameof(DroidInventoryEngineTests.HeadgearSummonedDroid))]
    [TestCase(nameof(DroidInventoryEngineTests.HeadgearSummonedDroidOccupiedHead))]
    [TestCase(nameof(DroidInventoryEngineTests.AppearanceSavedController))]
    [TestCase(nameof(DroidInventoryEngineTests.WeaponSkillsPlayerController))]
    [TestCase(nameof(DroidInventoryEngineTests.WeaponSkillsStoredController))]
    public void EquipmentPersistenceScenariosKeepBothDiscoveryLabels(string scenario)
    {
        var attribute = typeof(DroidInventoryEngineTests)
            .GetMethod(scenario, BindingFlags.Public | BindingFlags.Static)!
            .GetCustomAttribute<EngineTestAttribute>()!;

        attribute.Name.Should().StartWith("Migration ");
        attribute.Category.Should().Be("DroidInventory");
    }
}
