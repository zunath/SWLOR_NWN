#nullable enable
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class MigrationDataTests
{
    // Release 2026-10-16 (b1406fec3) shipped player 22 and server 27.
    private const int ReleasedPlayerVersion = 22;
    private const int ReleasedServerVersion = 27;
    private static readonly Assembly ServerAssembly = typeof(_28_CorrectStoredEquipmentTypes).Assembly;

    [Test]
    public void PlayerMigrationsHaveUniqueVersionsAndOneCombinedItemUpdate()
    {
        var migrations = ServerAssembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IPlayerMigration).IsAssignableFrom(type))
            .Select(type => (IPlayerMigration)Activator.CreateInstance(type)!).ToArray();
        migrations.Select(migration => migration.Version).Should().OnlyHaveUniqueItems();
        new _17_RemoveWeaponStatOverrides().Version.Should().BeGreaterThan(new _16_RetireForceWeaponEnhancements().Version);
        migrations.Where(migration => migration.Version >= 22).Select(migration => migration.GetType())
            .Should().BeEquivalentTo(new[] { typeof(_22_ReviseLootStats), typeof(_23_CorrectEquipmentTypes) });
        migrations.Where(migration => migration.Version > ReleasedPlayerVersion).Select(migration => migration.GetType())
            .Should().BeEquivalentTo(new[] { typeof(_23_CorrectEquipmentTypes) }, "the last release must receive one combined player-item pass");
        new _23_CorrectEquipmentTypes().Version.Should().Be(ReleasedPlayerVersion + 1);
    }

    [TestCase(112, -12)]
    [TestCase(200, -100)]
    [TestCase(15, 15)]
    public void ResistanceRowsAreDecodedBeforeMigrationMerges(int encoded, int expected)
    {
        Invoke("SerializedItemResistanceMigration", "GetMigrationPropertyValue", ItemPropertyType.Resistance, encoded)
            .Should().Be(expected);
    }

    [Test]
    public void RecipeVariantIdsAreStableAndDistinct()
    {
        var first = Invoke("ServerMigration.StoredItemDataMigration", "GetVariantId", "source", 4800);
        first.Should().Be(Invoke("ServerMigration.StoredItemDataMigration", "GetVariantId", "source", 4800));
        first.Should().NotBe(Invoke("ServerMigration.StoredItemDataMigration", "GetVariantId", "source", 4801));
        first.Should().NotBe(Invoke("ServerMigration.StoredItemDataMigration", "GetVariantId", "other", 4800));
        first.Should().Be("source-recipe-4800");
    }

    [Test]
    public void AFailedLaterPhaseCannotCheckpointAnEarlierHigherVersion()
    {
        var type = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.ServerMigrationState")!;
        var state = Activator.CreateInstance(type, 20)!;
        var run = type.GetMethod("Run")!;
        run.Invoke(state, new object[] { new StandInMigration(22, MigrationExecutionType.PostDatabaseLoad), (Action<IServerMigration>)(_ => { }) });
        var later = new StandInMigration(21, MigrationExecutionType.PostCacheLoad);
        Action fail = () => run.Invoke(state, new object[]
        {
            later, (Action<IServerMigration>)(_ => throw new InvalidOperationException("failed item"))
        });
        fail.Should().Throw<TargetInvocationException>();
        type.GetProperty("Failed")!.GetValue(state).Should().Be(true);
        type.GetProperty("CompletedVersion")!.GetValue(state).Should().Be(20);

        var executed = false;
        Action retry = () => run.Invoke(state, new object[]
        {
            later, (Action<IServerMigration>)(_ => executed = true)
        });
        retry.Should().Throw<TargetInvocationException>();
        executed.Should().BeFalse();
    }

    [Test]
    public void SuccessfulPhasesKeepTheHighestCompletedVersion()
    {
        var type = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.ServerMigrationState")!;
        var state = Activator.CreateInstance(type, 20)!;
        foreach (var migration in new IServerMigration[]
                 {
                     new StandInMigration(22, MigrationExecutionType.PostDatabaseLoad), new StandInMigration(21, MigrationExecutionType.PostCacheLoad),
                     new StandInMigration(22, MigrationExecutionType.PostCacheLoad),
                 })
            type.GetMethod("Run")!.Invoke(state, new object[] { migration, (Action<IServerMigration>)(_ => { }) });
        type.GetProperty("CompletedVersion")!.GetValue(state).Should().Be(22);
    }

    [Test]
    public void RetiredServerMigrations_StartNewDatabasesAtTheBaselineAndRejectOlderOnes()
    {
        var migration = ServerAssembly.GetType("SWLOR.Game.Server.Service.Migration")!;
        var baseline = (int)migration.GetField("RetiredServerMigrationVersion", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var resolve = migration.GetMethod("ResolveConfiguration", BindingFlags.NonPublic | BindingFlags.Static)!;
        ServerConfiguration Resolve(ServerConfiguration stored) => (ServerConfiguration)resolve.Invoke(null, new object[] { stored! })!;

        baseline.Should().Be(ReleasedServerVersion, "every server migration production has run is retired");
        Resolve(null!).MigrationVersion.Should().Be(baseline, "a new database has nothing for the retired migrations to change");
        Resolve(new ServerConfiguration { MigrationVersion = baseline - 1 }).Should().BeNull("this build no longer has the migrations an older database needs");
        var current = new ServerConfiguration { MigrationVersion = baseline };
        Resolve(current).Should().BeSameAs(current);

        var remaining = ServerAssembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IServerMigration).IsAssignableFrom(type))
            .Select(type => (IServerMigration)Activator.CreateInstance(type)!).ToArray();
        remaining.Should().OnlyContain(server => server.Version > baseline, "retired migrations are removed, not kept beside the baseline");
    }

    private static object? Invoke(string type, string method, params object[] args) =>
        ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition." + type)!
            .GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);

    private sealed class StandInMigration : IServerMigration
    {
        public StandInMigration(int version, MigrationExecutionType executionType)
        {
            Version = version;
            ExecutionType = executionType;
        }

        public int Version { get; }
        public MigrationExecutionType ExecutionType { get; }
        public void Migrate() { }
    }
}
