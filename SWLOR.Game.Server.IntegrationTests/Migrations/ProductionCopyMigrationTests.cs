using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.IntegrationTests.Migrations;

/// <summary>
/// Runs the pending server migrations on a copy of a production database. Skipped unless <c>SWLOR_INTEGRATION_DUMP</c> names
/// a <c>dump.rdb</c>. The file is only read: it is copied into a container of the test's own (never mounted), which has
/// persistence switched off, publishes its port on 127.0.0.1 only and is removed when the test ends. Nothing the dump holds
/// is written to the test output; the assertions count and compare, they do not print records.
/// </summary>
[TestFixture]
public class ProductionCopyMigrationTests
{
    public const string DumpVariable = "SWLOR_INTEGRATION_DUMP";
    private const string Image = "redislabs/redismod:latest";
    private static readonly TimeSpan MigrationBudget = TimeSpan.FromMinutes(30);

    private string? _container;

    [TearDown]
    public void RemoveContainer()
    {
        if (_container != null)
            Docker("rm", "-f", _container);
        _container = null;
    }

    [Test]
    [CancelAfter(3600000)]
    public void PendingMigrations_RunOnTheDumpAndANewBootChangesNothing()
    {
        var dump = Environment.GetEnvironmentVariable(DumpVariable);
        if (string.IsNullOrWhiteSpace(dump))
            Assert.Ignore($"{DumpVariable} is not set.");
        if (!File.Exists(dump))
            Assert.Fail($"{DumpVariable} names a file that does not exist.");
        if (!string.Equals(Path.GetFileName(dump), "dump.rdb", StringComparison.OrdinalIgnoreCase))
            Assert.Fail($"{DumpVariable} must name a dump.rdb.");

        // No webhook or token of the environment may be reached by the migrations.
        foreach (var variable in new[]
                 {
                     "SWLOR_BUG_DISCORD_WEBHOOK_URL", "SWLOR_HOLONET_WEBHOOK_URL", "SWLOR_DM_SHOUT_WEBHOOK_URL",
                     "SWLOR_PROPERTY_BROADCAST_WEBHOOK_URL", "SWLOR_SERVER_NOTIFICATION_WEBHOOK_URL", "SWLOR_BUG_GITHUB_TOKEN",
                 })
            Environment.SetEnvironmentVariable(variable, string.Empty);

        var endpoint = StartContainerWithDump(dump);
        using var redis = RedisEnvironment.Connect(endpoint);
        var database = redis.GetDatabase();
        var keysBefore = redis.GetServer(redis.GetEndPoints()[0]).DatabaseSize();

        var db = new IntegrationDBService(endpoint);
        db.Load();

        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        services.Replace(ServiceDescriptor.Singleton<IDBService>(db));
        services.Replace(ServiceDescriptor.Singleton<IMessagingService, UnconcealedMessagingService>());
        using var provider = ContainerHost.Create(services);
        using var container = CompositionRoot.InstallForTests(provider);
        using var engine = new FakeEngineSet();

        var stored = db.Get<ServerConfiguration>("SWLOR_CONFIG");
        stored.Should().NotBeNull("a production database has its server configuration");
        var before = stored.MigrationVersion;
        db.EndFrame();
        if (before < MigrationService.RetiredServerMigrationVersion)
            Assert.Inconclusive($"The dump is at server migration {before}; this build only upgrades databases at " +
                                $"{MigrationService.RetiredServerMigrationVersion} or later (upgrade the copy with an older build first).");

        var clock = Stopwatch.StartNew();
        var service = new IntegrationMigrationService(db);
        service.AfterDatabaseLoaded();
        service.AfterCacheLoaded();
        clock.Stop();
        TestContext.Out.WriteLine($"Migrations from version {before}: {clock.Elapsed.TotalSeconds:0} s.");

        clock.Elapsed.Should().BeLessThan(MigrationBudget, "a migration pass that long needs its own look before it runs in a gate");
        engine.Administration.CountOf("ShutdownServer").Should().Be(0, "the migrations did not fail");

        var newest = typeof(IServerMigration).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IServerMigration).IsAssignableFrom(type))
            .Select(type => ((IServerMigration)Activator.CreateInstance(type)!).Version)
            .DefaultIfEmpty(0)
            .Max();
        db.EndFrame();
        var after = db.Get<ServerConfiguration>("SWLOR_CONFIG").MigrationVersion;
        after.Should().BeGreaterThanOrEqualTo(Math.Max(before, newest));
        redis.GetServer(redis.GetEndPoints()[0]).DatabaseSize().Should().Be(keysBefore,
            "server migrations change records, they do not add or remove any");

        // A second boot finds nothing pending.
        var configuration = db.GetRawJson<ServerConfiguration>("SWLOR_CONFIG");
        var again = new IntegrationMigrationService(db);
        again.AfterDatabaseLoaded();
        again.AfterCacheLoaded();
        db.GetRawJson<ServerConfiguration>("SWLOR_CONFIG").Should().Be(configuration);
        database.Ping();
    }

    /// <summary>Creates a throwaway Redis container, copies the dump into it, starts it and waits until it has loaded the data.</summary>
    private string StartContainerWithDump(string dump)
    {
        var runId = Environment.GetEnvironmentVariable("SWLOR_INTEGRATION_RUN_ID");
        var name = $"swlor-it-dump-{Environment.ProcessId}-{Guid.NewGuid():N}".Substring(0, 40);
        var arguments = new List<string>
        {
            "create", "--name", name, "--label", "swlor-integration-run=" + (runId ?? "standalone"), "-p", "127.0.0.1::6379", Image,
            "--save", string.Empty, "--appendonly", "no",
            "--loadmodule", "/usr/lib/redis/modules/redisai.so",
            "--loadmodule", "/usr/lib/redis/modules/redisearch.so", "FORK_GC_CLEAN_THRESHOLD", "100000000",
            "--loadmodule", "/usr/lib/redis/modules/redisgraph.so",
            "--loadmodule", "/usr/lib/redis/modules/redistimeseries.so",
            "--loadmodule", "/usr/lib/redis/modules/rejson.so",
            "--loadmodule", "/usr/lib/redis/modules/redisbloom.so",
            "--loadmodule", "/usr/lib/redis/modules/redisgears.so", "Plugin", "/var/opt/redislabs/modules/rg/plugin/gears_python.so",
        };
        Docker(arguments.ToArray()).ExitCode.Should().Be(0, "the container is created");
        _container = name;
        Docker("cp", dump, $"{name}:/data/dump.rdb").ExitCode.Should().Be(0, "the dump is copied into the container");
        Docker("start", name).ExitCode.Should().Be(0);

        // While Redis loads the file it answers LOADING instead of PONG.
        var deadline = DateTime.UtcNow.AddMinutes(20);
        while (true)
        {
            if (Docker("exec", name, "redis-cli", "ping").Output.Trim() == "PONG")
                break;
            if (DateTime.UtcNow > deadline)
                Assert.Fail("Redis did not finish loading the dump in 20 minutes.");
            Thread.Sleep(2000);
        }

        var port = Docker("port", name, "6379/tcp").Output.Split('\n')[0].Trim();
        port.Should().StartWith("127.0.0.1:", "the port is published on the loopback address only");
        return port;
    }

    private static (int ExitCode, string Output) Docker(params string[] arguments)
    {
        var info = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        info.Environment["MSYS_NO_PATHCONV"] = "1";
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }
}
