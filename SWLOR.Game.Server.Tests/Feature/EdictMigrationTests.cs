using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>Server migration 32's Edict step: the Manage Edicts key on every city permission record.</summary>
public class EdictMigrationTests
{
    private const string Mayor = "mayor";
    private const string Delegate = "delegate";

    private static WorldPropertyPermission LegacyRecord(string playerId)
    {
        var record = new WorldPropertyPermission { PlayerId = playerId, PropertyId = "city" };
        foreach (var permission in new[]
                 {
                     PropertyPermissionType.EditTaxes,
                     PropertyPermissionType.AccessTreasury,
                     PropertyPermissionType.ManageUpgrades,
                     PropertyPermissionType.ManageUpkeep
                 })
        {
            var isMayor = playerId == Mayor;
            record.Permissions[permission] = isMayor;
            record.GrantPermissions[permission] = isMayor;
        }

        return record;
    }

    [Test]
    public void TheMayorsRecord_GetsManageEdictsAndItsGrantRight()
    {
        var record = LegacyRecord(Mayor);

        EdictMigration.MigratePermission(record, Mayor).Should().BeTrue();

        record.Permissions[PropertyPermissionType.ManageEdicts].Should().BeTrue();
        record.GrantPermissions[PropertyPermissionType.ManageEdicts].Should().BeTrue();
    }

    [Test]
    public void ADelegatesRecord_GetsTheKeyOffSoNothingIsGivenAway()
    {
        var record = LegacyRecord(Delegate);
        record.Permissions[PropertyPermissionType.EditTaxes] = true;

        EdictMigration.MigratePermission(record, Mayor).Should().BeTrue();

        record.Permissions[PropertyPermissionType.ManageEdicts].Should().BeFalse();
        record.GrantPermissions[PropertyPermissionType.ManageEdicts].Should().BeFalse();
        record.Permissions[PropertyPermissionType.EditTaxes].Should().BeTrue("the other permissions are untouched");
    }

    [Test]
    public void RunningTheMigrationAgain_ChangesNothing()
    {
        var record = LegacyRecord(Mayor);
        EdictMigration.MigratePermission(record, Mayor);

        EdictMigration.MigratePermission(record, Mayor).Should().BeFalse();
        record.Permissions[PropertyPermissionType.ManageEdicts].Should().BeTrue();
    }

    [Test]
    public void AKeyThatAlreadyExists_IsLeftAlone()
    {
        var record = LegacyRecord(Delegate);
        record.Permissions[PropertyPermissionType.ManageEdicts] = true;
        record.GrantPermissions[PropertyPermissionType.ManageEdicts] = false;

        EdictMigration.MigratePermission(record, Mayor).Should().BeFalse();

        record.Permissions[PropertyPermissionType.ManageEdicts].Should().BeTrue("the mayor delegated it before the migration ran again");
        record.GrantPermissions[PropertyPermissionType.ManageEdicts].Should().BeFalse();
    }

    [Test]
    public void ACityWithNoMayorRecorded_GivesNobodyTheKey()
    {
        var record = LegacyRecord(Mayor);

        EdictMigration.MigratePermission(record, string.Empty).Should().BeTrue();

        record.Permissions[PropertyPermissionType.ManageEdicts].Should().BeFalse();
    }

    [Test]
    public void AnOlderCityRecord_LoadsWithNoEdictsNoRestrictedFundsAndNoEscrow()
    {
        var city = new WorldProperty();

        city.Edicts.Should().BeEmpty();
        city.RestrictedTreasury.Should().Be(0);
        city.Escrow.Should().BeEmpty();
        city.MayorRecord.Promised.Should().Be(0);
        city.MayorRecord.Broken.Should().Be(0);
    }

    [Test]
    public void AnOlderCityRecord_DeserializesWithTheNewFieldsEmpty()
    {
        const string legacyJson = "{\"Treasury\":5000,\"Upkeep\":0,\"PropertyType\":1}";

        var city = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldProperty>(legacyJson);

        city.Treasury.Should().Be(5000);
        city.Edicts.Should().NotBeNull().And.BeEmpty();
        city.Escrow.Should().NotBeNull().And.BeEmpty();
        city.MayorRecord.Should().NotBeNull();
    }

    [Test]
    public void HeldEdictsAndEscrow_SurviveASaveAndReload()
    {
        var city = new WorldProperty { RestrictedTreasury = 777 };
        city.Edicts[SWLOR.Game.Server.Service.EdictService.EdictType.Alliance] = new CityEdict
        {
            State = SWLOR.Game.Server.Service.EdictService.EdictState.Pending,
            EnactedUtc = new System.DateTime(2026, 11, 2, 12, 0, 0, System.DateTimeKind.Utc),
            TargetCityId = "hutlar",
            FromPlatform = true,
            IsPausedByCityLevel = true,
            SituationPauseEndsUtc = new System.DateTime(2026, 11, 5, 12, 0, 0, System.DateTimeKind.Utc)
        };
        city.Escrow["bounty"] = new CityEscrowEntry { Restricted = 10, Unrestricted = 20 };
        city.Escrow["bounty"].PlayerFees["alice"] = 30;
        city.MayorRecord.Promised = 2;
        city.MayorRecord.Broken = 1;

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(city);
        var reloaded = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldProperty>(json);

        json.Should().Contain("\"Alliance\"", "enum dictionary keys are saved by name");
        reloaded.RestrictedTreasury.Should().Be(777);
        var alliance = reloaded.Edicts[SWLOR.Game.Server.Service.EdictService.EdictType.Alliance];
        alliance.State.Should().Be(SWLOR.Game.Server.Service.EdictService.EdictState.Pending);
        alliance.LockEndsUtc.Should().BeNull();
        alliance.TargetCityId.Should().Be("hutlar");
        alliance.FromPlatform.Should().BeTrue();
        alliance.IsPausedByCityLevel.Should().BeTrue();
        alliance.SituationPauseEndsUtc.Should().Be(new System.DateTime(2026, 11, 5, 12, 0, 0, System.DateTimeKind.Utc));
        reloaded.Escrow["bounty"].Restricted.Should().Be(10);
        reloaded.Escrow["bounty"].Unrestricted.Should().Be(20);
        reloaded.Escrow["bounty"].PlayerFees["alice"].Should().Be(30);
        reloaded.MayorRecord.Promised.Should().Be(2);
        reloaded.MayorRecord.Broken.Should().Be(1);
    }
}
