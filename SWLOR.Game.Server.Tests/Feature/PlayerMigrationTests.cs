#nullable enable
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerMigrationTests
{
    // The upgrade tests below run the server's character upgrade over a minimal saved character that holds no class,
    // items or quickbar, so the migrations they reach read and write the file without the game engine.
    private const int RebuildTokens = 3;

    // Migrations 15 (combat upgrade), 24 (Unarmed and Twin Pistol release) and 25 (crafting perk conversion) each grant one rebuild token.
    private const int UnarmedTwinPistolTokenVersion = 24;
    private const int PerkConversionTokenVersion = 25;

    private static int Latest => CharacterMigrations.LatestVersion;

    private static Player Record(int version, int tokens = RebuildTokens)
    {
        var player = new Player("upgrade-test") { Version = version };
        player.Currencies[CurrencyType.RebuildToken] = tokens;
        return player;
    }

    [TestCase(15)]
    [TestCase(17)]
    public void MigrationsAlreadyAppliedToTheRecordDoNotRepeatTheirRecordChanges(int checkpoint)
    {
        var record = Record(checkpoint);
        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(checkpoint), record);

        upgraded.Should().NotBeNull("the file and the record are behind the latest migration");
        upgraded!.Record.Version.Should().Be(Latest);
        upgraded.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens + 2, "only the tokens of migrations 24 and 25 are still ahead of the record");
        CharacterMigrations.ReadFileVersion(upgraded.File).Should().Be(Latest);
    }

    [Test]
    public void ACurrentCharacterIsNotUpgradedAgain()
    {
        var record = Record(Latest);
        var json = JsonConvert.SerializeObject(record);

        CharacterMigrations.Upgrade(SavedCharacterFile.Create(Latest), record).Should().BeNull();

        JsonConvert.SerializeObject(record).Should().Be(json, "a current character's record is not touched");
    }

    [Test]
    public void BonusTokenAndPlayerVersionAreAdvancedTogetherAndOnlyOnce()
    {
        var file = SavedCharacterFile.Create(Latest);
        var record = Record(14);

        var upgraded = CharacterMigrations.Upgrade(file, record);

        upgraded.Should().NotBeNull();
        upgraded!.Record.Version.Should().Be(Latest);
        upgraded.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens + 3, "migrations 15, 24 and 25 each grant one");
        upgraded.File.Should().Equal(file, "a file that is not behind any migration is not changed");
        CharacterMigrations.Upgrade(upgraded.File, upgraded.Record).Should().BeNull("a retry finds nothing left to upgrade");
        upgraded.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens + 3);
    }

    [TestCase(15)]
    [TestCase(16)]
    [TestCase(UnarmedTwinPistolTokenVersion - 1)]
    public void PlayersPastTheCombatUpgradeReceiveTheUnarmedTwinPistolAndPerkConversionTokens(int version)
    {
        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(Latest), Record(version));

        upgraded.Should().NotBeNull("the record is behind the latest migration");
        upgraded!.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens + 2);
        CharacterMigrations.Upgrade(upgraded.File, upgraded.Record).Should().BeNull("a retry finds nothing left to upgrade");
        upgraded.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens + 2, "each token is granted once");
    }

    [Test]
    public void PlayersAtTheUnarmedTwinPistolReleaseReceiveOnlyThePerkConversionToken()
    {
        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(Latest), Record(UnarmedTwinPistolTokenVersion));

        upgraded.Should().NotBeNull("the record is behind the latest migration");
        upgraded!.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens + 1);
    }

    [Test]
    public void PlayersAtThePerkConversionReleaseDoNotReceiveItsTokenAgain()
    {
        var record = Record(PerkConversionTokenVersion);

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(Latest), record);

        (upgraded?.Record ?? record).Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens);
    }

    [Test]
    public void BonusTokenIsAddedWhenTheCurrencyDictionaryHasNoTokenEntry()
    {
        var record = new Player("upgrade-test") { Version = 14 };
        record.Currencies.Clear();

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(Latest), record);

        upgraded!.Record.Version.Should().Be(Latest);
        upgraded.Record.Currencies[CurrencyType.RebuildToken].Should().Be(3);
    }

    /// <summary>
    /// A restored older character file is upgraded even when its record is current, without a second reward.
    /// </summary>
    [Test]
    public void RestoredOlderCharacterFileIsUpgradedEvenWhenTheRecordIsCurrent()
    {
        var record = Record(Latest);

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(Latest - 1), record);

        upgraded.Should().NotBeNull();
        CharacterMigrations.ReadFileVersion(upgraded!.File).Should().Be(Latest);
        upgraded.Record.Version.Should().Be(Latest);
        upgraded.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens);
    }

    /// <summary>
    /// The upgrade saves the record before the file. A crash between the two leaves a record ahead of its unchanged
    /// file, and the retry changes the file only.
    /// </summary>
    [Test]
    public void RetryAfterTheRecordWasSavedButTheFileWasNotUpgradesTheFileOnly()
    {
        var original = SavedCharacterFile.Create(17);
        var firstRecord = Record(14);
        var first = CharacterMigrations.Upgrade(original, firstRecord);
        first!.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens + 3);

        var retried = CharacterMigrations.Upgrade(original, first.Record);

        retried.Should().NotBeNull("the unchanged file is still behind");
        retried!.File.Should().Equal(first.File, "the retry reaches the same file");
        retried.Record.Currencies[CurrencyType.RebuildToken].Should().Be(RebuildTokens + 3, "the record reward is not repeated");
        retried.Record.Version.Should().Be(Latest);
    }

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
}
