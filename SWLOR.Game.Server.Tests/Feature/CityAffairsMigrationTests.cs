using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Server migration 32's City Affairs step, and the ids City Affairs reserved: the permission on every city permission
/// record, officers and counts starting empty.
/// </summary>
public class CityAffairsMigrationTests
{
    private const string Mayor = "mayor";

    private static WorldPropertyPermission ExistingRecord(string playerId, bool hasEverything)
    {
        var existing = new[]
        {
            PropertyPermissionType.EditStructures, PropertyPermissionType.EditTaxes, PropertyPermissionType.AccessTreasury,
            PropertyPermissionType.ManageUpgrades, PropertyPermissionType.ManageUpkeep
        };

        return new WorldPropertyPermission
        {
            PlayerId = playerId,
            PropertyId = "city",
            Permissions = existing.ToDictionary(type => type, _ => hasEverything),
            GrantPermissions = existing.ToDictionary(type => type, _ => hasEverything)
        };
    }

    [Test]
    public void TheMayorsRecord_GetsTheCityAffairsPermission()
    {
        var record = ExistingRecord(Mayor, true);

        CityAffairsMigration.MigratePermission(record, Mayor).Should().BeTrue();

        record.Permissions[PropertyPermissionType.HandleCityAffairs].Should().BeTrue();
        record.GrantPermissions[PropertyPermissionType.HandleCityAffairs].Should().BeTrue();
    }

    [Test]
    public void EveryoneElsesRecord_GetsItAsFalse()
    {
        var record = ExistingRecord("delegate", true);

        CityAffairsMigration.MigratePermission(record, Mayor).Should().BeTrue();

        record.Permissions[PropertyPermissionType.HandleCityAffairs].Should().BeFalse("only officers get it, and only by appointment");
        record.GrantPermissions[PropertyPermissionType.HandleCityAffairs].Should().BeFalse();
    }

    [Test]
    public void ThePermissionMigration_LeavesTheOtherPermissionsAlone()
    {
        var record = ExistingRecord("delegate", true);
        record.Permissions[PropertyPermissionType.AccessTreasury] = false;

        CityAffairsMigration.MigratePermission(record, Mayor);

        record.Permissions[PropertyPermissionType.EditTaxes].Should().BeTrue();
        record.Permissions[PropertyPermissionType.AccessTreasury].Should().BeFalse();
        record.Permissions.Should().HaveCount(6);
    }

    [Test]
    public void ARecordThatAlreadyHasThePermission_IsNotChanged_SoRunningAgainChangesNothing()
    {
        var record = ExistingRecord("officer", false);
        record.Permissions[PropertyPermissionType.HandleCityAffairs] = true;
        record.GrantPermissions[PropertyPermissionType.HandleCityAffairs] = false;

        CityAffairsMigration.MigratePermission(record, Mayor).Should().BeFalse();

        record.Permissions[PropertyPermissionType.HandleCityAffairs].Should().BeTrue("an appointed officer keeps the permission");

        var fresh = ExistingRecord("delegate", true);
        CityAffairsMigration.MigratePermission(fresh, Mayor).Should().BeTrue();
        CityAffairsMigration.MigratePermission(fresh, Mayor).Should().BeFalse();
    }

    [Test]
    public void ACityRecordWithNothingFromBefore_StartsWithNoOfficersModifiersOrCounts()
    {
        var city = new WorldProperty();

        CityAffairsMigration.MigrateCity(city).Should().BeFalse("a record saved before City Affairs loads with the defaults");

        city.Officers.Should().BeEmpty();
        city.Modifiers.Should().BeEmpty();
        city.NeglectCount.Should().Be(0);
        city.RecallStage.Should().Be(0);
    }

    [Test]
    public void ACityRecordWithMissingOrNegativeValues_IsRepaired()
    {
        var city = new WorldProperty { Officers = null, Modifiers = null, NeglectCount = -2, RecallStage = -1 };

        CityAffairsMigration.MigrateCity(city).Should().BeTrue();

        city.Officers.Should().NotBeNull().And.BeEmpty();
        city.Modifiers.Should().NotBeNull().And.BeEmpty();
        city.NeglectCount.Should().Be(0);
        city.RecallStage.Should().Be(0);
        CityAffairsMigration.MigrateCity(city).Should().BeFalse();
    }

    [Test]
    public void ACityWithOfficersAndCounts_KeepsThem()
    {
        var city = new WorldProperty { NeglectCount = 3, RecallStage = 1 };
        city.Officers["officer"] = new CityOfficer { Title = "Marshal" };
        city.Modifiers[CityModifierType.FestivalXpBonus] = new CityModifier { ExpiresUtc = DateTime.UtcNow.AddHours(5) };

        CityAffairsMigration.MigrateCity(city).Should().BeFalse();

        city.Officers.Should().ContainKey("officer");
        city.Modifiers.Should().ContainKey(CityModifierType.FestivalXpBonus);
        city.NeglectCount.Should().Be(3);
        city.RecallStage.Should().Be(1);
    }

    [Test]
    public void TheMigrationRunsInTheOneCityRevampVersion()
    {
        var migration = new _32_PlayerCityRevamp();

        migration.Version.Should().Be(32);
        typeof(_32_PlayerCityRevamp).Assembly.GetTypes()
            .Where(type => typeof(SWLOR.Game.Server.Service.MigrationService.IServerMigration).IsAssignableFrom(type) && type.Name.StartsWith("_32_"))
            .Should().ContainSingle("City Affairs adds a step to the existing version 32 class, never a second one");
    }

    [Test]
    public void TheReservedIdsAreUsedAsAssigned()
    {
        ((int)PropertyPermissionType.HandleCityAffairs).Should().Be(20);
        ((int)GuiWindowType.CityAffairs).Should().Be(111);
    }

    [Test]
    public void HandleCityAffairs_IsSetByAppointingAnOfficer_AndNeverShownInThePermissionWindow()
    {
        var attribute = (PropertyPermissionAttribute)Attribute.GetCustomAttribute(
            typeof(PropertyPermissionType).GetField(nameof(PropertyPermissionType.HandleCityAffairs)),
            typeof(PropertyPermissionAttribute));

        attribute.Should().NotBeNull();
        attribute.IsActive.Should().BeTrue();
        attribute.ShowInPermissionWindow.Should().BeFalse();

        var others = Enum.GetValues<PropertyPermissionType>().Where(type => type != PropertyPermissionType.HandleCityAffairs);
        foreach (var type in others)
        {
            var other = (PropertyPermissionAttribute)Attribute.GetCustomAttribute(
                typeof(PropertyPermissionType).GetField(type.ToString()),
                typeof(PropertyPermissionAttribute));
            other.ShowInPermissionWindow.Should().BeTrue(type.ToString());
        }
    }
}
