using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Service;

public class CharacterUpgradeCheckpointTests
{
    private static readonly Type Upgrade = typeof(Player).Assembly.GetType("SWLOR.Game.Server.Service.MigrationService.CharacterFileUpgrade")!;

    private static int LatestVersion => (int)Upgrade.GetProperty("LatestVersion", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private static int ResolveFileVersion(int checkpoint, int recordVersion) =>
        (int)Upgrade.GetMethod("ResolveFileVersion", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { checkpoint, recordVersion })!;

    private static bool IsCurrent(int checkpoint, int recordVersion) =>
        (bool)Upgrade.GetMethod("IsCurrent", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { checkpoint, new Player { Version = recordVersion } })!;

    [Test]
    public void FilesWithoutACheckpointUseTheRecordVersionUpToTheLastVersionBeforeCheckpoints()
    {
        ResolveFileVersion(0, -1).Should().Be(-1, "a legacy record's file has had no migration");
        ResolveFileVersion(0, 6).Should().Be(6);
        ResolveFileVersion(0, 12).Should().Be(12);
        ResolveFileVersion(0, 18).Should().Be(12, "files saved after version 12 carry their own checkpoint");
        ResolveFileVersion(15, 18).Should().Be(15, "the file's checkpoint wins over its record");
    }

    [Test]
    public void LoginRequiresBothTheFileAndTheRecordAtTheLatestVersion()
    {
        var latest = LatestVersion;
        IsCurrent(latest, latest).Should().BeTrue();
        IsCurrent(0, latest).Should().BeFalse("a file without a checkpoint predates the upgrade");
        IsCurrent(latest - 1, latest).Should().BeFalse("the file is behind");
        IsCurrent(latest, latest - 1).Should().BeFalse("the record is behind");
        IsCurrent(0, -1).Should().BeFalse("legacy characters are upgraded at boot");
    }

    [Test]
    public void CharacterMigrationsHaveUniqueAscendingVersions()
    {
        var migrations = ((System.Collections.IEnumerable)Upgrade.GetProperty("Migrations", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!)
            .Cast<object>()
            .Select(migration => (int)migration.GetType().GetProperty("Version")!.GetValue(migration)!)
            .ToArray();

        migrations.Should().NotBeEmpty();
        migrations.Should().BeInAscendingOrder();
        migrations.Should().OnlyHaveUniqueItems();
        migrations.First().Should().Be(1, "legacy characters start at the first migration");
        migrations.Last().Should().Be(LatestVersion);
    }
}
