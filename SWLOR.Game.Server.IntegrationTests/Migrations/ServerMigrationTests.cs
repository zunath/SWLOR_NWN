using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;

namespace SWLOR.Game.Server.IntegrationTests.Migrations;

/// <summary>
/// The real migration runner over records in Redis: older-shape records go in, the server's migrations run in order at the
/// two boot steps, and the versions, the records and a second boot are checked in the database.
/// </summary>
[TestFixture]
public class ServerMigrationTests : IntegrationTestBase
{
    private const int Retired = MigrationService.RetiredServerMigrationVersion;

    private void SeedConfiguration(int version)
    {
        Db.Set(new ServerConfiguration { Id = "SWLOR_CONFIG", MigrationVersion = version });
        Db.EndFrame();
    }

    private int StoredVersion()
    {
        Db.EndFrame();
        return Db.Get<ServerConfiguration>("SWLOR_CONFIG").MigrationVersion;
    }

    private IntegrationMigrationService Boot(params IServerMigration[] migrations) => BootWith(migrations);

    private IntegrationMigrationService BootWith(IServerMigration[]? migrations)
    {
        var service = new IntegrationMigrationService(Db, migrations);
        service.AfterDatabaseLoaded();
        service.AfterCacheLoaded();
        return service;
    }

    [Test]
    public void ANewDatabase_StartsAtTheRetiredVersionAndRunsNothingBelowIt()
    {
        var ran = new List<int>();

        Boot(new Stand(Retired - 5, MigrationExecutionType.PostCacheLoad, ran));

        ran.Should().BeEmpty("a migration production already ran is never run on a new database");
        StoredVersion().Should().Be(Retired);
    }

    [Test]
    public void PendingMigrations_RunInVersionOrderPerPhaseAndTheVersionAdvancesToTheLast()
    {
        SeedConfiguration(Retired);
        var ran = new List<string>();

        Boot(
            new Stand(Retired + 3, MigrationExecutionType.PostCacheLoad, ran),
            new Stand(Retired + 1, MigrationExecutionType.PostDatabaseLoad, ran),
            new Stand(Retired + 2, MigrationExecutionType.PostCacheLoad, ran),
            new Stand(Retired + 4, MigrationExecutionType.PostDatabaseLoad, ran));

        ran.Should().Equal($"database {Retired + 1}", $"database {Retired + 4}", $"cache {Retired + 2}", $"cache {Retired + 3}");
        StoredVersion().Should().Be(Retired + 4);
    }

    [Test]
    public void ASecondBoot_RunsNothingAndKeepsTheVersion()
    {
        SeedConfiguration(Retired);
        var first = new List<int>();
        Boot(new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, first), new Stand(Retired + 2, MigrationExecutionType.PostDatabaseLoad, first));
        first.Should().HaveCount(2);
        var stored = Db.GetRawJson<ServerConfiguration>("SWLOR_CONFIG");

        var second = new List<int>();
        Boot(new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, second), new Stand(Retired + 2, MigrationExecutionType.PostDatabaseLoad, second));

        second.Should().BeEmpty();
        Db.GetRawJson<ServerConfiguration>("SWLOR_CONFIG").Should().Be(stored);
    }

    [Test]
    public void AFailingMigration_StopsTheServerKeepsTheVersionAndTheNextBootRunsItAgain()
    {
        SeedConfiguration(Retired);
        var ran = new List<int>();

        Boot(
            new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, ran),
            new Stand(Retired + 2, MigrationExecutionType.PostCacheLoad, ran) { Fails = true },
            new Stand(Retired + 3, MigrationExecutionType.PostCacheLoad, ran));

        ran.Should().Equal(Retired + 1, Retired + 2);
        Fakes.Administration.CountOf("ShutdownServer").Should().Be(1);
        StoredVersion().Should().Be(Retired, "the version advances only after every pending migration succeeded");

        var rerun = new List<int>();
        Boot(
            new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, rerun),
            new Stand(Retired + 2, MigrationExecutionType.PostCacheLoad, rerun),
            new Stand(Retired + 3, MigrationExecutionType.PostCacheLoad, rerun));

        rerun.Should().Equal(Retired + 1, Retired + 2, Retired + 3);
        StoredVersion().Should().Be(Retired + 3);
    }

    [Test]
    public void ADatabaseOlderThanTheRetiredVersion_IsRefusedAndNothingRuns()
    {
        SeedConfiguration(Retired - 1);
        var ran = new List<int>();

        Boot(new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, ran));

        ran.Should().BeEmpty();
        Fakes.Administration.CountOf("ShutdownServer").Should().Be(1);
        StoredVersion().Should().Be(Retired - 1);
    }

    private sealed class Stand : IServerMigration
    {
        private readonly List<string>? _phased;
        private readonly List<int>? _versions;

        public Stand(int version, MigrationExecutionType executionType, List<string> ran)
        {
            Version = version;
            ExecutionType = executionType;
            _phased = ran;
        }

        public Stand(int version, MigrationExecutionType executionType, List<int> ran)
        {
            Version = version;
            ExecutionType = executionType;
            _versions = ran;
        }

        public bool Fails { get; init; }

        public int Version { get; }

        public MigrationExecutionType ExecutionType { get; }

        public void Migrate()
        {
            _versions?.Add(Version);
            _phased?.Add($"{(ExecutionType == MigrationExecutionType.PostDatabaseLoad ? "database" : "cache")} {Version}");
            if (Fails)
                throw new InvalidOperationException("migration failed on purpose");
        }
    }
}
