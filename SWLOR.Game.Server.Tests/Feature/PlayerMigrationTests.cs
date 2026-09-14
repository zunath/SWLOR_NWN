#nullable enable
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerMigrationTests
{
    [TestCase(0, false, true)]
    [TestCase(0, true, false)]
    [TestCase(-1, false, false)]
    [TestCase(15, false, false)]
    public void MissingRecordsRequireValidatedCreationEvenWithoutCharacterProgress(
        int version, bool pending, bool requiresRecord)
    {
        var player = new Player("audit-player") { Version = version, CharacterInitializationPending = pending };
        var guard = typeof(Server.Feature.PlayerInitialization).GetMethod("RequiresExistingPlayerRecord", BindingFlags.Static | BindingFlags.NonPublic)!;
        guard.Invoke(null, new object[] { player }).Should().Be(requiresRecord);
    }

    [TestCase(0u, false, true)]
    [TestCase(110633u, false, false)]
    [TestCase(0u, true, false)]
    public void OnlyAcceptedNewCharacterCreationRecordsDurableInitializationIntent(uint result, bool exists, bool expectedSave)
    {
        Player? saved = exists ? new Player("creation-test") { Version = 15 } : null;
        var original = saved;
        var saves = 0;
        var fileSaved = false;
        var record = typeof(Server.Native.PlayerCreation).GetMethod("RecordValidatedCreation", BindingFlags.Static | BindingFlags.NonPublic)!;
        void Run() => record.Invoke(null, new object[] { result, "creation-test", (Func<Player?>)(() => saved),
            (Action)(() => fileSaved = true),
            (Action<Player>)(player => { fileSaved.Should().BeTrue(); saved = player; saves++; }) });
        Run();
        Run();
        saves.Should().Be(expectedSave ? 1 : 0, "validation must not reset an existing record on repeated calls");
        if (expectedSave)
        {
            saved!.CharacterInitializationPending.Should().BeTrue();
            saved.Version.Should().Be(0);
        }
        else saved.Should().BeSameAs(original);
    }

    [Test]
    public void FailedCreationIdentitySaveCannotAuthorizeDestructiveInitialization()
    {
        var record = typeof(Server.Native.PlayerCreation).GetMethod("RecordValidatedCreation", BindingFlags.Static | BindingFlags.NonPublic)!;
        Action run = () => record.Invoke(null, new object[] { 0u, "creation-test", (Func<Player?>)(() => null),
            (Action)(() => throw new InvalidOperationException("Injected identity export failure")),
            (Action<Player>)(_ => Assert.Fail("No initialization intent may be saved after an export failure")) });
        run.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();
    }

    [Test]
    public void BonusTokenAndPlayerVersionAreSavedTogetherAfterRefreshingLiveChanges()
    {
        var saved = PlayerJson(14);
        var saves = 0;
        var migration = TokenMigration(() => saved["UnallocatedSP"] = 37);

        Apply(migration, () => saved.ToObject<Player>()!, player =>
        {
            player.Version.Should().Be(15);
            player.Currencies[CurrencyType.RebuildToken].Should().Be(4);
            player.UnallocatedSP.Should().Be(37);
            saved = JObject.FromObject(player);
            saves++;
        });

        Apply(migration, () => saved.ToObject<Player>()!, _ => saves++);
        saves.Should().Be(1);
    }

    [TestCase(15)]
    [TestCase(16)]
    public void CurrentPlayersDoNotReceiveAnotherStartingOrMigrationToken(int version)
    {
        var saved = PlayerJson(version);
        var executed = false;
        Apply(TokenMigration(() => executed = true), () => saved.ToObject<Player>()!, _ => Assert.Fail("Unexpected save"));
        executed.Should().BeFalse();
    }

    [Test]
    public void FailedLiveMigrationDoesNotGrantTokenOrAdvancePlayerVersion()
    {
        var saved = PlayerJson(14);
        Action run = () => Apply(TokenMigration(() => throw new InvalidOperationException("item failure")),
            () => saved.ToObject<Player>()!, _ => Assert.Fail("Unexpected save"));
        run.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();
        saved["Version"]!.Value<int>().Should().Be(14);
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(3);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RetryingFailedSaveGrantsTokenOnceEvenIfTheWriteSucceededBeforeTheError(bool persistedBeforeError)
    {
        var saved = PlayerJson(14);
        var migration = TokenMigration(() => { });
        Action fail = () => Apply(migration, () => saved.ToObject<Player>()!, player =>
        {
            if (persistedBeforeError)
                saved = JObject.FromObject(player);
            throw new InvalidOperationException("lost save acknowledgement");
        });
        fail.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();

        Apply(migration, () => saved.ToObject<Player>()!, player => saved = JObject.FromObject(player));
        saved["Version"]!.Value<int>().Should().Be(15);
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(4);
    }

    [Test]
    public void AFailedLaterMigrationKeepsTheEarlierPlayerCheckpoint()
    {
        var saved = PlayerJson(13);
        var firstRuns = 0;
        var first = new TestMigration(14, () => firstRuns++);
        void Save(Player player) => saved = JObject.FromObject(player);
        Player Load() => saved.ToObject<Player>()!;

        Apply(first, Load, Save);
        Action fail = () => Apply(TokenMigration(() => throw new InvalidOperationException("item failure")), Load, Save);
        fail.Should().Throw<TargetInvocationException>();

        Apply(first, Load, Save);
        firstRuns.Should().Be(1);
        saved["Version"]!.Value<int>().Should().Be(14);
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(3);
    }

    [TestCase(1)]
    [TestCase(2)]
    public void FailedPlayerLoadDoesNotSaveOrGrantTokenAndCanBeRetried(int failingLoad)
    {
        var saved = PlayerJson(14);
        var loads = 0;
        var liveRuns = 0;
        var migration = TokenMigration(() => liveRuns++);
        Action fail = () => Apply(migration, () =>
        {
            if (++loads == failingLoad)
                throw new InvalidOperationException("Player record unavailable");
            return saved.ToObject<Player>()!;
        }, _ => Assert.Fail("A failed load must prevent saving"));

        fail.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();
        liveRuns.Should().Be(failingLoad - 1);
        saved.Should().BeEquivalentTo(PlayerJson(14));

        Apply(migration, () => saved.ToObject<Player>()!, player => saved = JObject.FromObject(player));
        saved["Version"]!.Value<int>().Should().Be(15);
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(4);
    }

    /// <summary>
    /// Verifies a failed data hook cannot leak staged state or duplicate a rebuild token on retry.
    /// </summary>
    [Test]
    public void FailedDataHookDiscardsUnsavedChangesAndRetryGrantsOnlyOneToken()
    {
        var saved = PlayerJson(14);
        var failHook = true;
        var migration = new TestMigration(15, () => { }, player =>
        {
            new _15_RemoveObsoleteCombatInstructionDiscs().MigratePlayerData(player);
            if (failHook)
                throw new InvalidOperationException("Data update failed");
        });
        Action fail = () => Apply(migration, () => saved.ToObject<Player>()!,
            _ => Assert.Fail("A failed data hook must prevent saving"));
        fail.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();
        saved.Should().BeEquivalentTo(PlayerJson(14));

        failHook = false;
        Apply(migration, () => saved.ToObject<Player>()!, player => saved = JObject.FromObject(player));
        saved["Version"]!.Value<int>().Should().Be(15);
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(4);
    }

    /// <summary>
    /// Verifies failure leaves the shared cached player at its last durable checkpoint.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void FailedMigrationDoesNotAdvanceTheSharedCachedPlayer(bool failInDataHook)
    {
        var persisted = PlayerJson(14);
        var cached = persisted.ToObject<Player>()!;
        var fail = true;
        var migration = new TestMigration(15, () => { }, player =>
        {
            new _15_RemoveObsoleteCombatInstructionDiscs().MigratePlayerData(player);
            if (fail && failInDataHook)
                throw new InvalidOperationException("Data hook failed");
        });
        void Save(Player player)
        {
            if (fail)
                throw new InvalidOperationException("Save failed before persistence");
            persisted = JObject.FromObject(player);
            cached = player;
        }

        Action first = () => Apply(migration, () => cached, Save);
        first.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();
        cached.Version.Should().Be(14, "DB.Get returns the shared cached instance");
        cached.Currencies[CurrencyType.RebuildToken].Should().Be(3, "an unsaved token must not leak into the cache");
        persisted.Should().BeEquivalentTo(PlayerJson(14));

        fail = false;
        Apply(migration, () => cached, Save);
        cached.Version.Should().Be(15);
        cached.Currencies[CurrencyType.RebuildToken].Should().Be(4);
        persisted["Version"]!.Value<int>().Should().Be(15);
        persisted["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(4);
    }

    [Test]
    public void BonusTokenIsAddedWhenTheCurrencyDictionaryHasNoTokenEntry()
    {
        var saved = PlayerJson(14);
        saved["Currencies"] = new JObject();
        Apply(TokenMigration(() => { }), () => saved.ToObject<Player>()!, player => saved = JObject.FromObject(player));
        saved["Version"]!.Value<int>().Should().Be(15);
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(1);
    }

    [Test]
    public void LiveOnlyMigrationSavesRefreshedDataAndAdvancesTheExistingVersion()
    {
        var saved = PlayerJson(12);
        var migration = new TestMigration(13, () => saved["UnallocatedSP"] = 42);
        Apply(migration, () => saved.ToObject<Player>()!, player => saved = JObject.FromObject(player));
        saved["Version"]!.Value<int>().Should().Be(13);
        saved["UnallocatedSP"]!.Value<int>().Should().Be(42);
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(3);
    }

    private static JObject PlayerJson(int version) => new()
    {
        ["Version"] = version,
        ["Currencies"] = new JObject { ["RebuildToken"] = 3 }
    };

    private static IPlayerMigration TokenMigration(Action migrate)
    {
        IPlayerMigration migration = new _15_RemoveObsoleteCombatInstructionDiscs();
        return new TestMigration(migration.Version, migrate, migration.MigratePlayerData);
    }

    private static void Apply(IPlayerMigration migration, Func<Player> load, Action<Player> save)
    {
        var fileVersion = 0;
        Player Load()
        {
            var player = load();
            fileVersion = player.Version;
            return player;
        }
        ApplyWithFile(migration, Load, save, () => fileVersion, _ => { });
    }

    private static void ApplyWithFile(IPlayerMigration migration, Func<Player> load, Action<Player> save,
        Func<int> loadFileVersion, Action<int> saveFileVersion) =>
        typeof(Migration).GetMethod("ApplyPlayerMigration", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { migration, 0u, load, save, loadFileVersion, saveFileVersion });

    /// <summary>
    /// Replays file changes after an old BIC is restored without granting a second database reward.
    /// </summary>
    [Test]
    public void RestoredOlderCharacterFileIsMigratedEvenWhenDatabaseIsCurrent()
    {
        var saved = PlayerJson(15);
        var fileVersion = 14;
        var liveRuns = 0;
        ApplyWithFile(TokenMigration(() => liveRuns++), () => saved.ToObject<Player>()!,
            _ => Assert.Fail("The completed database reward must not run again"),
            () => fileVersion, version => fileVersion = version);
        liveRuns.Should().Be(1);
        fileVersion.Should().Be(15);
        saved.Should().BeEquivalentTo(PlayerJson(15));
    }

    /// <summary>
    /// A saved file is authoritative for native changes when the database checkpoint write must be retried.
    /// </summary>
    [Test]
    public void DatabaseSaveRetryDoesNotRepeatASuccessfullySavedLiveMigration()
    {
        var saved = PlayerJson(14);
        var fileVersion = 14;
        var liveRuns = 0;
        var migration = TokenMigration(() => liveRuns++);
        Action first = () => ApplyWithFile(migration, () => saved.ToObject<Player>()!,
            _ => throw new InvalidOperationException("Database unavailable"),
            () => fileVersion, version => fileVersion = version);
        first.Should().Throw<TargetInvocationException>();
        fileVersion.Should().Be(15);
        saved["Version"]!.Value<int>().Should().Be(14);

        ApplyWithFile(migration, () => saved.ToObject<Player>()!, player => saved = JObject.FromObject(player),
            () => fileVersion, version => fileVersion = version);
        liveRuns.Should().Be(1);
        saved["Version"]!.Value<int>().Should().Be(15);
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(4);
    }

    /// <summary>
    /// A character save failure must prevent recording migration completion or its reward in the database.
    /// </summary>
    [Test]
    public void FailedCharacterSaveDoesNotAdvanceTheDatabaseOrGrantTokens()
    {
        var saved = PlayerJson(14);
        Action run = () => ApplyWithFile(TokenMigration(() => { }), () => saved.ToObject<Player>()!,
            _ => Assert.Fail("Character saving must succeed first"),
            () => 14, _ => throw new InvalidOperationException("Character file write failed"));
        run.Should().Throw<TargetInvocationException>();
        saved.Should().BeEquivalentTo(PlayerJson(14));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NewCharacterRetriesRestoreStarterStateWithoutDuplicatingRewards(bool failFinalDatabaseWrite)
    {
        var saved = JObject.FromObject(new Player("new-character"));
        var fileVersion = 0;
        var starterStateWritten = false;
        var initializeRuns = 0;
        var fail = true;
        void Initialize(Player player)
        {
            initializeRuns++;
            if (player.Version == 0)
                player.Currencies[CurrencyType.RebuildToken] = 1;
            player.Version = 15;
        }
        void Save(Player player)
        {
            if (fail && failFinalDatabaseWrite && !player.CharacterInitializationPending)
                throw new InvalidOperationException("Final record write failed");
            saved = JObject.FromObject(player);
        }
        void SaveFile(int version)
        {
            if (fail && !failFinalDatabaseWrite)
                throw new InvalidOperationException("First character export failed");
            starterStateWritten = true;
            fileVersion = version;
        }
        void Run() => typeof(Migration).GetMethod("RunPlayerInitialization", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { saved.ToObject<Player>()!, (Action<Player>)Initialize,
                (Action<Player>)Save, (Func<int>)(() => fileVersion), (Action<int>)SaveFile });

        ((Action)Run).Should().Throw<TargetInvocationException>();
        saved["CharacterInitializationPending"]!.Value<bool>().Should().BeTrue();
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(1);
        fail = false;
        Run();
        starterStateWritten.Should().BeTrue();
        fileVersion.Should().Be(15);
        initializeRuns.Should().Be(failFinalDatabaseWrite ? 1 : 2);
        saved["CharacterInitializationPending"]!.Value<bool>().Should().BeFalse();
        saved["Currencies"]!["RebuildToken"]!.Value<int>().Should().Be(1);
    }

    [Test]
    public void NewCharacterIntentMustBeDurableBeforeChangingTheCharacter()
    {
        var initialized = false;
        Action run = () => typeof(Migration).GetMethod("RunPlayerInitialization", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { new Player("new-character"), (Action<Player>)(_ => initialized = true),
                (Action<Player>)(_ => throw new InvalidOperationException("Database unavailable")),
                (Func<int>)(() => 0), (Action<int>)(_ => Assert.Fail("File save must not be reached")) });
        run.Should().Throw<TargetInvocationException>();
        initialized.Should().BeFalse();
    }

    private sealed class TestMigration(int version, Action migrate, Action<Player>? updateRecord = null) : IPlayerMigration
    {
        public int Version => version;
        public void Migrate(uint player) => migrate();
        public void MigratePlayerData(Player player) => updateRecord?.Invoke(player);
    }
}
