#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using PlayerRecord = SWLOR.Game.Server.Entity.Player;
using SWLOR.Game.Server.Tests.Feature;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the migration service: which server migrations run and in which order, the version numbers read and
/// written, what a failure does, the boot order of the phases and the character upgrade, and the login guard. The
/// database is an in-memory fake, the engine a recording fake, and the reads of the game's static services sit behind
/// the seams <see cref="BenchMigrationService"/> records.
/// </summary>
[TestFixture]
public class MigrationServiceTests
{
    private const uint PlayerObject = 1001u;
    private const string ConfigId = "SWLOR_CONFIG";

    private InMemoryDBService _db = null!;
    private PlayerEngine _world = null!;
    private FakeAdministrationPlugin _admin = null!;
    private IDisposable _adminScope = null!;
    private BenchMigrationService _service = null!;
    private LocalVariables _locals = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new InMemoryDBService();
        _world = new PlayerEngine();
        _admin = new FakeAdministrationPlugin();
        _adminScope = AdministrationPluginRuntime.Override(_admin);
        _locals = new LocalVariables(_world.Engine);
        _world.Engine.SetReturn("GetEnteringObject", PlayerObject);
        _service = new BenchMigrationService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _adminScope.Dispose();
        _world.Dispose();
    }

    private int StoredVersion => _db.Get<ServerConfiguration>(ConfigId)!.MigrationVersion;

    private StandInMigration Server(int version, MigrationExecutionType phase)
    {
        var migration = new StandInMigration(version, phase, _service.Steps);
        _service.Definitions.Add(migration);
        return migration;
    }

    private static int Retired => MigrationService.RetiredServerMigrationVersion;

    // ---- The boot phases -------------------------------------------------------------------------------------------

    [Test]
    public void A_new_database_starts_at_the_retired_version_and_runs_every_newer_migration_in_version_order()
    {
        Server(Retired + 3, MigrationExecutionType.PostDatabaseLoad);
        Server(Retired + 1, MigrationExecutionType.PostDatabaseLoad);
        Server(Retired + 2, MigrationExecutionType.PostCacheLoad);
        Server(Retired - 1, MigrationExecutionType.PostDatabaseLoad);

        _service.AfterDatabaseLoaded();
        _db.Saved.Should().ContainSingle().Which.Should().BeOfType<ServerConfiguration>()
            .Which.MigrationVersion.Should().Be(Retired, "a new database has nothing for the retired migrations to change");
        _service.Steps.Should().Equal($"database {Retired + 1}", $"database {Retired + 3}");

        _service.AfterCacheLoaded();

        _service.Steps.Should().Equal($"database {Retired + 1}", $"database {Retired + 3}", $"cache {Retired + 2}", "vault");
        StoredVersion.Should().Be(Retired + 3, "the highest version either phase completed");
    }

    [Test]
    public void Migrations_at_or_below_the_stored_version_do_not_run()
    {
        _db.With(new ServerConfiguration { MigrationVersion = Retired + 2 });
        Server(Retired + 1, MigrationExecutionType.PostDatabaseLoad);
        Server(Retired + 2, MigrationExecutionType.PostCacheLoad);
        Server(Retired + 3, MigrationExecutionType.PostCacheLoad);

        _service.AfterDatabaseLoaded();
        _service.AfterCacheLoaded();

        _service.Steps.Should().Equal($"cache {Retired + 3}", "vault");
        _db.Saved.OfType<ServerConfiguration>().Should().ContainSingle().Which.MigrationVersion.Should().Be(Retired + 3);
    }

    [Test]
    public void The_stored_version_stays_when_no_migration_is_pending()
    {
        _db.With(new ServerConfiguration { MigrationVersion = Retired + 4 });

        _service.AfterDatabaseLoaded();
        _service.AfterCacheLoaded();

        _db.Saved.Should().BeEmpty("nothing completed above the stored version, so nothing is written");
        _service.Steps.Should().Equal("vault");
    }

    [Test]
    public void The_version_is_stored_after_both_phases_and_before_the_characters_are_upgraded()
    {
        _db.With(new ServerConfiguration { MigrationVersion = Retired });
        Server(Retired + 1, MigrationExecutionType.PostDatabaseLoad);
        Server(Retired + 1, MigrationExecutionType.PostCacheLoad);
        var versionDuringUpgrade = -1;
        _service.OnVaultUpgrade = () => versionDuringUpgrade = StoredVersion;

        _service.AfterDatabaseLoaded();
        StoredVersion.Should().Be(Retired, "the version advances only after both phases succeed");
        _service.AfterCacheLoaded();

        versionDuringUpgrade.Should().Be(Retired + 1);
        _service.Steps.Should().Equal($"database {Retired + 1}", $"cache {Retired + 1}", "vault");
    }

    [Test]
    public void A_failing_database_phase_migration_stops_the_run_and_shuts_the_server_down()
    {
        _db.With(new ServerConfiguration { MigrationVersion = Retired });
        Server(Retired + 1, MigrationExecutionType.PostDatabaseLoad);
        Server(Retired + 2, MigrationExecutionType.PostDatabaseLoad).FailWith = new InvalidOperationException("broken");
        Server(Retired + 3, MigrationExecutionType.PostDatabaseLoad);
        Server(Retired + 4, MigrationExecutionType.PostCacheLoad);

        _service.AfterDatabaseLoaded();
        _service.AfterCacheLoaded();

        _admin.CountOf("ShutdownServer").Should().Be(1);
        _service.Steps.Should().Equal($"database {Retired + 1}", $"database {Retired + 2}");
        _db.Saved.Should().BeEmpty("a failed run never advances the stored version");
        StoredVersion.Should().Be(Retired);
    }

    [Test]
    public void A_failing_cache_phase_migration_keeps_the_version_and_skips_the_character_upgrade()
    {
        _db.With(new ServerConfiguration { MigrationVersion = Retired });
        Server(Retired + 1, MigrationExecutionType.PostDatabaseLoad);
        Server(Retired + 2, MigrationExecutionType.PostCacheLoad).FailWith = new InvalidOperationException("broken");

        _service.AfterDatabaseLoaded();
        _service.AfterCacheLoaded();
        _service.RunServerMigrationsPostCache();

        _admin.CountOf("ShutdownServer").Should().Be(1);
        _service.Steps.Should().Equal($"database {Retired + 1}", $"cache {Retired + 2}");
        _db.Saved.Should().BeEmpty();
        StoredVersion.Should().Be(Retired);
    }

    [Test]
    public void A_database_below_the_retired_version_runs_nothing_and_shuts_the_server_down()
    {
        _db.With(new ServerConfiguration { MigrationVersion = Retired - 1 });
        Server(Retired + 1, MigrationExecutionType.PostDatabaseLoad);
        Server(Retired + 1, MigrationExecutionType.PostCacheLoad);

        _service.AfterDatabaseLoaded();
        _service.AfterCacheLoaded();

        _admin.CountOf("ShutdownServer").Should().Be(1);
        _service.Steps.Should().BeEmpty("no migration, no version write and no character upgrade");
        _db.Saved.Should().BeEmpty();
        StoredVersion.Should().Be(Retired - 1);
    }

    [Test]
    public void A_character_upgrade_that_throws_is_contained()
    {
        _db.With(new ServerConfiguration { MigrationVersion = Retired });
        Server(Retired + 1, MigrationExecutionType.PostCacheLoad);
        _service.VaultFailure = new InvalidOperationException("vault unreadable");

        var act = () =>
        {
            _service.AfterDatabaseLoaded();
            _service.AfterCacheLoaded();
        };

        act.Should().NotThrow("characters the upgrade did not reach are refused at login instead");
        StoredVersion.Should().Be(Retired + 1, "the server migrations completed");
        _admin.CountOf("ShutdownServer").Should().Be(0);
    }

    [Test]
    public void The_default_character_upgrade_with_no_server_vault_folder_is_a_success()
    {
        var previous = Environment.GetEnvironmentVariable("SWLOR_SERVERVAULT_DIRECTORY");
        Environment.SetEnvironmentVariable("SWLOR_SERVERVAULT_DIRECTORY", Path.Combine(Path.GetTempPath(), "mr-p2-c6-missing-vault-" + Guid.NewGuid()));
        try
        {
            new MigrationService(_db, new OverflowStorageService(_db), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatService>()).UpgradeServerVault().Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("SWLOR_SERVERVAULT_DIRECTORY", previous);
        }
    }

    [Test]
    public void The_default_character_upgrade_is_skipped_once_every_character_is_current()
    {
        _db.With(new ServerConfiguration { CharacterFileVersion = CharacterMigrations.LatestVersion });

        new MigrationService(_db, new OverflowStorageService(_db), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatService>()).UpgradeServerVault().Should().BeTrue();

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void The_character_upgrade_is_run_on_request()
    {
        _service.UpgradeServerVault().Should().BeTrue();
        _service.VaultResult = false;
        _service.UpgradeServerVault().Should().BeFalse("a character that could not be upgraded is reported");

        _service.Steps.Should().Equal("vault", "vault");
    }

    // ---- Version bookkeeping ----------------------------------------------------------------------------------------

    [Test]
    public void ResolveConfiguration_starts_new_databases_at_the_retired_version_and_rejects_older_ones()
    {
        _service.ResolveConfiguration(null!).MigrationVersion.Should().Be(Retired);
        _service.ResolveConfiguration(new ServerConfiguration { MigrationVersion = Retired - 1 }).Should().BeNull();
        var current = new ServerConfiguration { MigrationVersion = Retired };
        _service.ResolveConfiguration(current).Should().BeSameAs(current);
        var newer = new ServerConfiguration { MigrationVersion = Retired + 7 };
        _service.ResolveConfiguration(newer).Should().BeSameAs(newer);
    }

    [Test]
    public void The_retired_version_and_the_file_version_variable_keep_their_values()
    {
        Retired.Should().Be(30);
        MigrationService.PlayerFileVersionVariable.Should().Be("PLAYER_MIGRATION_VERSION");

        // Native code and tests read both constants here: the facade that repeated them was removed (P2-ZI).
        typeof(MigrationService).GetField(nameof(MigrationService.RetiredServerMigrationVersion))!.IsLiteral.Should().BeTrue();
        typeof(MigrationService).GetField(nameof(MigrationService.PlayerFileVersionVariable))!.IsLiteral.Should().BeTrue();
    }

    [Test]
    public void The_latest_player_version_is_the_newest_character_migration()
    {
        _service.GetLatestPlayerVersion().Should().Be(CharacterMigrations.LatestVersion).And.BeGreaterThan(0);
    }

    [Test]
    public void RequestVaultRescan_clears_the_vault_version_only_when_it_is_set()
    {
        _service.RequestVaultRescan();
        _db.Saved.Should().BeEmpty("there is no configuration to reset");

        _db.With(new ServerConfiguration { CharacterFileVersion = 0, MigrationVersion = Retired });
        _service.RequestVaultRescan();
        _db.Saved.Should().BeEmpty("the vault is already due a full read");

        _db.With(new ServerConfiguration { CharacterFileVersion = 12, MigrationVersion = Retired + 2 });
        _service.RequestVaultRescan();
        var saved = _db.Saved.Should().ContainSingle().Subject.Should().BeOfType<ServerConfiguration>().Subject;
        saved.CharacterFileVersion.Should().Be(0);
        saved.MigrationVersion.Should().Be(Retired + 2, "only the vault version changes");
    }

    // ---- Login ------------------------------------------------------------------------------------------------------

    private void SetCheckpoint(int version) => NWScript.SetLocalInt(PlayerObject, MigrationService.PlayerFileVersionVariable, version);

    [Test]
    public void Non_players_and_DMs_are_left_alone()
    {
        _world.Engine.SetReturn("GetIsPC", false);
        _service.AfterCharacterInitialization();
        _world.Engine.SetReturn("GetIsPC", true);
        _world.Engine.SetReturn("GetIsDM", true);
        _service.AfterCharacterInitialization();

        _db.Reads.Should().BeEmpty();
        _service.Steps.Should().BeEmpty();
        _world.Engine.CallsTo("BootPC").Should().BeEmpty();
    }

    [Test]
    public void A_character_without_a_record_is_refused()
    {
        SetCheckpoint(5);

        _service.AfterCharacterInitialization();

        _world.Engine.CallsTo("BootPC").Should().ContainSingle().Which.Arguments[0].Should().Be(PlayerObject);
        _service.Steps.Should().BeEmpty("none of the login restoration runs for a refused character");
    }

    [Test]
    public void A_character_behind_the_latest_version_is_refused_and_the_next_boot_reads_the_whole_vault()
    {
        var latest = CharacterMigrations.LatestVersion;
        _db.With(new ServerConfiguration { CharacterFileVersion = latest, MigrationVersion = Retired });
        _db.With(new PlayerRecord(PlayerEngine.PlayerId) { Version = latest - 1 });
        SetCheckpoint(latest);

        _service.AfterCharacterInitialization();

        _world.Engine.CallsTo("BootPC").Should().ContainSingle();
        _service.Steps.Should().BeEmpty();
        _db.Saved.OfType<ServerConfiguration>().Should().ContainSingle().Which.CharacterFileVersion.Should().Be(0);
    }

    [Test]
    public void A_character_file_behind_the_latest_version_is_refused_even_when_its_record_is_current()
    {
        var latest = CharacterMigrations.LatestVersion;
        _db.With(new PlayerRecord(PlayerEngine.PlayerId) { Version = latest });
        SetCheckpoint(latest - 1);

        _service.AfterCharacterInitialization();

        _world.Engine.CallsTo("BootPC").Should().ContainSingle();
        _service.Steps.Should().BeEmpty();
    }

    [Test]
    public void A_current_character_gets_its_notices_and_the_login_restoration_in_order()
    {
        var latest = CharacterMigrations.LatestVersion;
        var record = new PlayerRecord(PlayerEngine.PlayerId) { Version = latest };
        record.UpgradeNotices.Add("First notice");
        record.UpgradeNotices.Add("Second notice");
        _db.With(record);
        _db.With(new OverflowItem { Id = "overflow-1", PlayerId = PlayerEngine.PlayerId, Name = "Blaster" });
        SetCheckpoint(latest);

        _service.AfterCharacterInitialization();

        _world.Engine.CallsTo("BootPC").Should().BeEmpty();
        _world.Messages().Take(2).Should().Equal("First notice", "Second notice");
        _world.Messages().Should().HaveCount(3).And.Contain(message => message.Contains("overflow storage"),
            "the overflow storage reminder follows the notices");
        record.UpgradeNotices.Should().BeEmpty("a notice is shown once");
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(record);
        _service.Steps.Should().Equal("attributes", "feats", "readiness", "maxhp", "equipment");
    }

    [Test]
    public void A_current_character_without_notices_saves_nothing()
    {
        var latest = CharacterMigrations.LatestVersion;
        _db.With(new PlayerRecord(PlayerEngine.PlayerId) { Version = latest });
        SetCheckpoint(latest);

        _service.AfterCharacterInitialization();

        _db.Saved.Should().BeEmpty();
        _world.Messages().Should().BeEmpty();
        _service.Steps.Should().Equal("attributes", "feats", "readiness", "maxhp", "equipment");
    }

    // ---- The character file checkpoint ------------------------------------------------------------------------------

    [Test]
    public void A_saved_checkpoint_keeps_the_new_version_on_the_character()
    {
        SetCheckpoint(3);

        _service.SavePlayerFileCheckpoint(PlayerObject, 9);

        _service.Steps.Should().Equal("save");
        _locals.Int(PlayerObject, MigrationService.PlayerFileVersionVariable).Should().Be(9);
    }

    [TestCase(0)]
    [TestCase(4)]
    public void A_checkpoint_that_cannot_be_saved_restores_the_previous_version_and_throws(int previous)
    {
        SetCheckpoint(previous);
        _service.CanSave = false;

        var act = () => _service.SavePlayerFileCheckpoint(PlayerObject, 9);

        act.Should().Throw<InvalidOperationException>().WithMessage("The migrated character file could not be saved.");
        _locals.Int(PlayerObject, MigrationService.PlayerFileVersionVariable).Should().Be(previous);
        _world.Engine.CallsTo("DeleteLocalInt").Count.Should().Be(previous == 0 ? 1 : 0);
    }

    [Test]
    public void A_checkpoint_save_that_faults_restores_the_previous_version_and_rethrows()
    {
        SetCheckpoint(2);
        _service.SaveFailure = new ApplicationException("client gone");

        var act = () => _service.SavePlayerFileCheckpoint(PlayerObject, 9);

        act.Should().Throw<ApplicationException>().WithMessage("client gone");
        _locals.Int(PlayerObject, MigrationService.PlayerFileVersionVariable).Should().Be(2);
    }

    /// <summary>A server migration that records when it runs and can be told to fail.</summary>
    private sealed class StandInMigration : IServerMigration
    {
        private readonly List<string> _steps;

        public StandInMigration(int version, MigrationExecutionType executionType, List<string> steps)
        {
            Version = version;
            ExecutionType = executionType;
            _steps = steps;
        }

        public int Version { get; }
        public MigrationExecutionType ExecutionType { get; }
        public Exception? FailWith { get; set; }

        public void Migrate()
        {
            _steps.Add($"{(ExecutionType == MigrationExecutionType.PostDatabaseLoad ? "database" : "cache")} {Version}");
            if (FailWith != null)
                throw FailWith;
        }
    }
}
