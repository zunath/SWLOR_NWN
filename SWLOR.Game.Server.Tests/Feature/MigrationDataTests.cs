#nullable enable
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class MigrationDataTests
{
    // Production has run player migration 24 and server migration 29.
    private const int ReleasedPlayerVersion = 24;
    private const int ReleasedServerVersion = 29;
    private static readonly Assembly ServerAssembly = typeof(SWLOR.Game.Server.Service.Migration).Assembly;

    [Test]
    public void CharacterMigrationsHaveUniqueVersionsAndOneCombinedItemUpdate()
    {
        var migrations = CharacterMigrations.ImplementationTypes
            .Select(type => Activator.CreateInstance(type)!).ToArray();
        migrations.Select(CharacterMigrations.VersionOf).Should().OnlyHaveUniqueItems();
        CharacterMigrations.VersionOf("_17_RemoveWeaponStatOverrides").Should().BeGreaterThan(CharacterMigrations.VersionOf("_16_RetireForceWeaponEnhancements"));
        migrations.Where(migration => CharacterMigrations.VersionOf(migration) >= 22).Select(migration => migration.GetType())
            .Should().BeEquivalentTo(new[] { CharacterMigrations.Named("_22_ReviseLootStats"), CharacterMigrations.Named("_23_CorrectEquipmentTypes"),
                CharacterMigrations.Named("_24_CorrectCarapaceBracers"), CharacterMigrations.Named("_25_ConsolidatePelvisAppearance") });
        migrations.Where(migration => CharacterMigrations.VersionOf(migration) > ReleasedPlayerVersion).Select(migration => migration.GetType())
            .Should().BeEquivalentTo(new[] { CharacterMigrations.Named("_25_ConsolidatePelvisAppearance") }, "the next release must receive one combined player-item pass");
        CharacterMigrations.VersionOf("_25_ConsolidatePelvisAppearance").Should().Be(ReleasedPlayerVersion + 1);
    }

    [Test]
    public void TheCharacterUpgradeRunsEveryMigrationInVersionOrder()
    {
        var registered = CharacterMigrations.Registered.Select(CharacterMigrations.VersionOf).ToArray();
        registered.Should().OnlyHaveUniqueItems();
        registered.Should().BeInAscendingOrder("the upgrade applies migrations oldest first");
        registered.Should().BeEquivalentTo(
            CharacterMigrations.ImplementationTypes.Select(type => CharacterMigrations.VersionOf(Activator.CreateInstance(type)!)),
            "every migration class is registered");
        CharacterMigrations.LatestVersion.Should().Be(registered[^1]);
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
        remaining.Select(server => server.GetType()).Should().BeEquivalentTo(new[] { typeof(_30_ConsolidatePelvisAppearance) },
            "the next release must receive one combined stored-item pass");
        new _30_ConsolidatePelvisAppearance().ExecutionType.Should().Be(MigrationExecutionType.PostCacheLoad);
        new _30_ConsolidatePelvisAppearance().Version.Should().Be(ReleasedServerVersion + 1);
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
