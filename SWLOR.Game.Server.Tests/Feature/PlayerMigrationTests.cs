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

    private static void Apply(IPlayerMigration migration, Func<Player> load, Action<Player> save) =>
        typeof(Migration).GetMethod("ApplyPlayerMigration", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { migration, 0u, load, save });

    private sealed class TestMigration(int version, Action migrate, Action<Player>? updateRecord = null) : IPlayerMigration
    {
        public int Version => version;
        public void Migrate(uint player) => migrate();
        public void MigratePlayerData(Player player) => updateRecord?.Invoke(player);
    }
}
