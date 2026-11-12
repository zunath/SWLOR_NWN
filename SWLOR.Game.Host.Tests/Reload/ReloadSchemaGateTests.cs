using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Host.Reload;
using SWLOR.Game.Host.Tests.Support;

namespace SWLOR.Game.Host.Tests.Reload;

/// <summary>
/// The reload's schema gate: server migrations run only at boot, so a candidate whose newest server migration is newer than
/// the database's <c>MigrationVersion</c> is rejected with a detail saying a restart is required; one at or below it passes.
/// </summary>
[TestFixture]
public class ReloadSchemaGateTests
{
    private static ReloadSchemaGate Gate(int? database) => new(_ => ValueTask.FromResult(database));

    [Test]
    public async Task A_candidate_at_the_databases_version_passes()
    {
        var result = await Gate(31).CheckAsync(31, CancellationToken.None);

        result.Compatible.Should().BeTrue();
        result.Detail.Should().Be("The candidate's newest server migration (31) is at or below the database's (31).");
    }

    [Test]
    public async Task A_candidate_below_the_databases_version_passes_as_a_boot_would()
    {
        var result = await Gate(33).CheckAsync(31, CancellationToken.None);

        result.Compatible.Should().BeTrue("a boot runs no migration for a database that is ahead of the build either");
    }

    [Test]
    public async Task A_candidate_that_needs_a_newer_migration_is_rejected_and_told_to_restart()
    {
        var result = await Gate(31).CheckAsync(32, CancellationToken.None);

        result.Compatible.Should().BeFalse();
        result.Detail.Should().Be("The candidate needs server migration 32, but the database is at 31. " +
                                  "Server migrations run only at boot: a restart is required to load this build.");
    }

    [Test]
    public async Task A_database_without_a_version_or_that_cannot_be_read_rejects_the_candidate()
    {
        var missing = await Gate(null).CheckAsync(31, CancellationToken.None);
        var unreadable = await new ReloadSchemaGate(_ => throw new InvalidOperationException("redis down"))
            .CheckAsync(31, CancellationToken.None);

        missing.Compatible.Should().BeFalse();
        missing.Detail.Should().Contain("Redis holds no MigrationVersion under ServerConfiguration:SWLOR_CONFIG").And.Contain("Restart the server");
        unreadable.Compatible.Should().BeFalse();
        unreadable.Detail.Should().Contain("InvalidOperationException: redis down").And.Contain("Restart the server");
    }

    [Test]
    public async Task A_cancelled_check_is_not_taken_for_a_rejection()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var gate = new ReloadSchemaGate(token =>
        {
            token.ThrowIfCancellationRequested();
            return ValueTask.FromResult<int?>(31);
        });

        var check = async () => await gate.CheckAsync(31, cancelled.Token);

        await check.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestCase("{\"Id\":\"SWLOR_CONFIG\",\"MigrationVersion\":31,\"CharacterFileVersion\":4}", 31)]
    [TestCase("[{\"MigrationVersion\":30}]", 30)]
    [TestCase("{\"Id\":\"SWLOR_CONFIG\"}", null)]
    [TestCase("{\"MigrationVersion\":\"31\"}", null)]
    [TestCase("not json", null)]
    [TestCase("", null)]
    public void The_stored_configuration_is_read_for_its_migration_version(string json, int? expected)
    {
        ReloadSchemaGate.ParseMigrationVersion(json).Should().Be(expected);
    }

    [Test]
    public async Task The_redis_reader_uses_the_hosts_connection()
    {
        var source = new FakeRedisConnectionSource();

        var read = async () => await ReloadSchemaGate.ReadMigrationVersionAsync(source, CancellationToken.None);

        await read.Should().ThrowAsync<InvalidOperationException>();
        source.Opened.Should().Be(1);
    }
}
