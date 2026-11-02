using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>Server migration 32's garden step: the Tend Gardens permission on existing House and City permission records.</summary>
public class GardenPermissionMigrationTests
{
    private static WorldPropertyPermission Record(string playerId) => new()
    {
        PropertyId = "house",
        PlayerId = playerId,
        Permissions = new Dictionary<PropertyPermissionType, bool>
        {
            [PropertyPermissionType.EditStructures] = true,
            [PropertyPermissionType.EnterProperty] = true
        },
        GrantPermissions = new Dictionary<PropertyPermissionType, bool>
        {
            [PropertyPermissionType.EditStructures] = true,
            [PropertyPermissionType.EnterProperty] = true
        }
    };

    [Test]
    public void TheOwnerOrMayorGetsTheKeyTrueForUseAndForGranting()
    {
        var record = Record("owner");

        GardenPermissionMigration.Migrate(record, "owner").Should().BeTrue();

        record.Permissions[PropertyPermissionType.TendGardens].Should().BeTrue();
        record.GrantPermissions[PropertyPermissionType.TendGardens].Should().BeTrue();
    }

    [Test]
    public void EveryoneElseGetsTheKeyFalse()
    {
        var record = Record("guest");

        GardenPermissionMigration.Migrate(record, "owner").Should().BeTrue();

        record.Permissions[PropertyPermissionType.TendGardens].Should().BeFalse();
        record.GrantPermissions[PropertyPermissionType.TendGardens].Should().BeFalse();
    }

    [Test]
    public void ARecordIsLeftAloneOnceItHasTheKey_SoRunningAgainChangesNothing()
    {
        var record = Record("guest");
        record.Permissions[PropertyPermissionType.TendGardens] = true;
        record.GrantPermissions[PropertyPermissionType.TendGardens] = false;

        GardenPermissionMigration.Migrate(record, "owner").Should().BeFalse("a permission already granted or withheld stays as it is");

        record.Permissions[PropertyPermissionType.TendGardens].Should().BeTrue();
        record.GrantPermissions[PropertyPermissionType.TendGardens].Should().BeFalse();

        var fresh = Record("owner");
        GardenPermissionMigration.Migrate(fresh, "owner").Should().BeTrue();
        GardenPermissionMigration.Migrate(fresh, "owner").Should().BeFalse();
    }

    [Test]
    public void ARecordWithOnlyThePermissionKeyStillGetsTheGrantKey()
    {
        var record = Record("owner");
        record.Permissions[PropertyPermissionType.TendGardens] = true;

        GardenPermissionMigration.Migrate(record, "owner").Should().BeTrue();

        record.GrantPermissions[PropertyPermissionType.TendGardens].Should().BeTrue();
    }

    [Test]
    public void ThePermissionsTheRecordAlreadyHadAreNotTouched()
    {
        var record = Record("guest");
        record.Permissions[PropertyPermissionType.EditStructures] = false;

        GardenPermissionMigration.Migrate(record, "owner");

        record.Permissions[PropertyPermissionType.EditStructures].Should().BeFalse();
        record.Permissions[PropertyPermissionType.EnterProperty].Should().BeTrue();
        record.Permissions.Should().HaveCount(3);
    }

    [Test]
    public void ARecordOfAPropertyWithNoOwnerGetsTheKeyFalse()
    {
        var record = Record("");

        GardenPermissionMigration.Migrate(record, "").Should().BeTrue();

        record.Permissions[PropertyPermissionType.TendGardens].Should().BeFalse("an empty owner id is nobody");
    }

    [Test]
    public void Migration32RunsTheGardenStepBesideTheElectionStep()
    {
        var folder = Path.Combine(FindRoot(), "SWLOR.Game.Server", "Feature", "MigrationDefinition", "ServerMigration");
        var source = File.ReadAllText(Path.Combine(folder, "_32_PlayerCityRevamp.cs"));

        source.Should().Contain("ElectionMigration.Run(Version)").And.Contain("GardenPermissionMigration.Run(Version)");
        Directory.GetFiles(folder, "_32_*.cs").Should().ContainSingle("a release has one migration class per version");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
