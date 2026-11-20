#nullable enable
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class MigrationDataTests
{
    // Production has run player migration 26 and server migration 31. The next release adds player migration
    // NextPlayerVersion and server migration NextServerVersion. Neither is registered until it holds real work, because
    // booting a build records the highest registered version and an empty one would skip work later folded into it.
    // Update these at each post-release cleanup.
    private const int ReleasedPlayerVersion = 26;
    private const int ReleasedServerVersion = 31;
    private const int NextPlayerVersion = ReleasedPlayerVersion + 1;
    private const int NextServerVersion = ReleasedServerVersion + 1;
    private static readonly Assembly ServerAssembly = typeof(SWLOR.Game.Server.Service.Migration).Assembly;

    [Test]
    public void CharacterMigrationsHaveUniqueVersionsAndAtMostOneNextPass()
    {
        var migrations = CharacterMigrations.ImplementationTypes
            .Select(type => Activator.CreateInstance(type)!).ToArray();
        migrations.Select(CharacterMigrations.VersionOf).Should().OnlyHaveUniqueItems();
        migrations.Select(CharacterMigrations.VersionOf).Where(version => version > NextPlayerVersion).Should().BeEmpty(
            $"the next release receives at most one player pass, as migration {NextPlayerVersion}");
        CharacterMigrations.LatestVersion.Should().BeInRange(ReleasedPlayerVersion, NextPlayerVersion);
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
        remaining.Where(server => server.Version <= baseline).Should().BeEmpty("retired migrations are removed, not kept beside the baseline");
        remaining.Where(server => server.Version != NextServerVersion).Should().BeEmpty(
            $"the next release adds at most one combined stored-item pass, as server migration {NextServerVersion}");
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
