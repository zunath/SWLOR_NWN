#nullable enable
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class CraftingPerkConversionMigrationTests
{
    private const string MigrationName = "_26_ConvertCraftingPerksToRanks";
    private const int StartingSP = 5;
    private const int StartingTokens = 2;

    private static readonly Type SavedCharacterType = typeof(Migration).Assembly
        .GetType("SWLOR.Game.Server.Service.MigrationService.SavedCharacter")!;

    // The refund table of the crafting revamp plan: SP paid per rank, read from the combat-rebuild prices.
    private static readonly PerkPrices[] Table =
    {
        new(PerkType.Harvesting, "Harvesting", new[] { 1, 1, 2, 3, 4 }, 11, FeatType.Harvesting1, FeatType.Harvesting2, FeatType.Harvesting3, FeatType.Harvesting4, FeatType.Harvesting5),
        new(PerkType.Refining, "Refining", new[] { 1, 1, 2, 3, 4 }, 11, FeatType.Refining1, FeatType.Refining2, FeatType.Refining3, FeatType.Refining4, FeatType.Refining5),
        new(PerkType.RefineryManagement, "Refinery Management", new[] { 1, 1, 2, 2, 3, 4 }, 13,
            FeatType.RefineryManagement1, FeatType.RefineryManagement2, FeatType.RefineryManagement3, FeatType.RefineryManagement4, FeatType.RefineryManagement5, FeatType.RefineryManagement6),
        new(PerkType.Scavenging, "Scavenging", new[] { 1, 1, 2, 3, 4 }, 11, FeatType.Scavenging1, FeatType.Scavenging2, FeatType.Scavenging3, FeatType.Scavenging4, FeatType.Scavenging5),
        new(PerkType.HardLook, "Hard Look", new[] { 1, 1, 2, 3, 3 }, 10, FeatType.HardLook1, FeatType.HardLook2, FeatType.HardLook3, FeatType.HardLook4, FeatType.HardLook5),
        new(PerkType.TreasureHunter, "Treasure Hunter", new[] { 3, 3, 4 }, 10, FeatType.TreasureHunterTrait),
        new(PerkType.CreditFinder, "Creditfinder", new[] { 3, 3, 4 }, 10, FeatType.CreditFinderTrait),
        new(PerkType.Research, "Research", new[] { 2, 2, 3, 3, 4 }, 14, FeatType.ResearchTrait),
        new(PerkType.ScientificNetworking, "Scientific Networking", new[] { 3, 4 }, 7, FeatType.ScientificNetworkingTrait),
        new(PerkType.ResearchProjects, "Research Projects", new[] { 2, 3 }, 5, FeatType.ResearchProjectsTrait),
        new(PerkType.DroidAssembly, "Droid Assembly", new[] { 1, 1, 2, 3, 4 }, 11, FeatType.DroidAssemblyTrait),
    };

    private static int PreviousVersion => CharacterMigrations.VersionOf(MigrationName) - 1;

    public static IEnumerable<TestCaseData> EveryPerkAtEveryRank() =>
        Table.SelectMany(perk => Enumerable.Range(1, perk.Prices.Length)
            .Select(rank => new TestCaseData(perk.Perk, rank, perk.Prices.Take(rank).Sum())
                .SetName($"{perk.Perk} rank {rank} refunds {perk.Prices.Take(rank).Sum()} SP")));

    private static Player Record(int version, int tokens = StartingTokens)
    {
        var player = new Player("conversion-test") { Version = version, UnallocatedSP = StartingSP };
        player.Currencies[CurrencyType.RebuildToken] = tokens;
        return player;
    }

    /// <summary>
    /// A plain record upgraded by the character migrations after this one only (the space revamp's 26 follows it). Every upgrade below
    /// runs them too, so their SP, tokens and notices are the baseline this migration's effects are measured against.
    /// </summary>
    private static Player LaterOnly()
    {
        var record = Record(PreviousVersion + 1);
        return CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion + 1), record)?.Record ?? record;
    }

    private static int LaterSP => LaterOnly().UnallocatedSP - StartingSP;

    private static int LaterTokens => LaterOnly().Currencies[CurrencyType.RebuildToken] - StartingTokens;

    /// <summary>The notices this migration added: the record's notices without the later migrations' own.</summary>
    private static List<string> OwnNotices(Player player)
    {
        var later = LaterOnly().UpgradeNotices;
        return player.UpgradeNotices.Where(notice => !later.Contains(notice)).ToList();
    }

    [Test]
    public void TheFullRefundTotalsMatchThePlan()
    {
        Table.ToDictionary(perk => perk.Perk, perk => perk.Prices.Sum()).Should().Equal(new Dictionary<PerkType, int>
        {
            [PerkType.Harvesting] = 11,
            [PerkType.Refining] = 11,
            [PerkType.RefineryManagement] = 13,
            [PerkType.Scavenging] = 11,
            [PerkType.HardLook] = 10,
            [PerkType.TreasureHunter] = 10,
            [PerkType.CreditFinder] = 10,
            [PerkType.Research] = 14,
            [PerkType.ScientificNetworking] = 7,
            [PerkType.ResearchProjects] = 5,
            [PerkType.DroidAssembly] = 11,
        });
        Table.Select(perk => perk.FullRefund).Should().Equal(Table.Select(perk => perk.Prices.Sum()));
    }

    [TestCaseSource(nameof(EveryPerkAtEveryRank))]
    public void EachOwnedRankIsRefundedAndTheTokenIsGrantedOnce(PerkType perk, int rank, int expectedRefund)
    {
        var record = Record(PreviousVersion);
        record.Perks[perk] = rank;
        record.UnlockedPerks[perk] = DateTime.UtcNow;
        var name = Table.Single(row => row.Perk == perk).Name;

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record);

        upgraded.Should().NotBeNull();
        var result = upgraded!.Record;
        result.Version.Should().Be(CharacterMigrations.LatestVersion);
        result.UnallocatedSP.Should().Be(StartingSP + expectedRefund + LaterSP);
        result.Perks.Should().NotContainKey(perk);
        result.UnlockedPerks.Should().NotContainKey(perk);
        result.Currencies[CurrencyType.RebuildToken].Should().Be(StartingTokens + 1 + LaterTokens);
        result.RebuildComplete.Should().BeFalse("rebuilding stays optional");
        OwnNotices(result).Should().HaveCount(2);
        OwnNotices(result).Should().ContainSingle(notice =>
            notice.Contains($"'{name}'") && notice.Contains($"You reclaimed {expectedRefund} SP."));
        OwnNotices(result).Should().ContainSingle(notice =>
            notice.Contains("1 Rebuild Token") && notice.Contains("optional"));
    }

    [Test]
    public void ACharacterOwningEveryPerkAtMaximumRankIsRefundedInFullWithOneToken()
    {
        var record = Record(PreviousVersion);
        foreach (var perk in Table)
        {
            record.Perks[perk.Perk] = perk.Prices.Length;
            record.UnlockedPerks[perk.Perk] = DateTime.UtcNow;
        }

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!.Record;

        upgraded.UnallocatedSP.Should().Be(StartingSP + 113 + LaterSP);
        upgraded.Perks.Should().BeEmpty();
        upgraded.UnlockedPerks.Should().BeEmpty();
        upgraded.Currencies[CurrencyType.RebuildToken].Should().Be(StartingTokens + 1 + LaterTokens);
        OwnNotices(upgraded).Should().HaveCount(Table.Length + 1, "one notice per refunded perk and one for the token");
    }

    [Test]
    public void ACharacterOwningNoneOfThePerksStillReceivesTheTokenAndItsNotice()
    {
        var record = Record(PreviousVersion);

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!.Record;

        upgraded.UnallocatedSP.Should().Be(StartingSP + LaterSP);
        upgraded.Currencies[CurrencyType.RebuildToken].Should().Be(StartingTokens + 1 + LaterTokens);
        OwnNotices(upgraded).Should().ContainSingle().Which.Should().Contain("1 Rebuild Token");
    }

    [Test]
    public void ACharacterWithoutATokenEntryGainsExactlyOne()
    {
        var record = Record(PreviousVersion);
        record.Currencies.Clear();

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!.Record;

        upgraded.Currencies[CurrencyType.RebuildToken].Should().Be(1 + LaterTokens);
    }

    [Test]
    public void OtherPerksAndSkillPointTotalsAreLeftAlone()
    {
        var record = Record(PreviousVersion);
        record.TotalSPAcquired = 120;
        record.Perks[PerkType.Alertness] = 2;
        record.Perks[PerkType.Harvesting] = 5;

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!.Record;

        upgraded.Perks.Should().Equal(new Dictionary<PerkType, int> { [PerkType.Alertness] = 2 });
        upgraded.TotalSPAcquired.Should().Be(120);
    }

    [Test]
    public void RetiredEntriesWithoutRanksAreRemovedWithoutARefundOrNotice()
    {
        var record = Record(PreviousVersion);
        record.Perks[PerkType.Research] = 0;

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!.Record;

        upgraded.Perks.Should().NotContainKey(PerkType.Research);
        upgraded.UnallocatedSP.Should().Be(StartingSP + LaterSP);
        OwnNotices(upgraded).Should().ContainSingle().Which.Should().Contain("Rebuild Token");
    }

    [Test]
    public void ARankBeyondTheOldPerkIsRefundedOnlyForTheRanksThatExisted()
    {
        var record = Record(PreviousVersion);
        record.Perks[PerkType.ResearchProjects] = 9;

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!.Record;

        upgraded.UnallocatedSP.Should().Be(StartingSP + 5 + LaterSP);
    }

    [Test]
    public void ASecondRunChangesNothing()
    {
        var record = Record(PreviousVersion);
        foreach (var perk in Table)
            record.Perks[perk.Perk] = perk.Prices.Length;
        var first = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!;
        var snapshot = JsonConvert.SerializeObject(first.Record);

        CharacterMigrations.Upgrade(first.File, first.Record).Should().BeNull("the character is current");

        JsonConvert.SerializeObject(first.Record).Should().Be(snapshot);
    }

    [Test]
    public void ARetryOfTheFileDoesNotRefundTheRecordAgain()
    {
        var original = SavedCharacterFile.Create(PreviousVersion);
        var record = Record(PreviousVersion);
        record.Perks[PerkType.Harvesting] = 5;
        var first = CharacterMigrations.Upgrade(original, record)!;

        var retried = CharacterMigrations.Upgrade(original, first.Record);

        retried.Should().NotBeNull("the unchanged file is still behind");
        retried!.Record.UnallocatedSP.Should().Be(StartingSP + 11 + LaterSP, "the record was already refunded");
        retried.Record.Currencies[CurrencyType.RebuildToken].Should().Be(StartingTokens + 1 + LaterTokens);
        OwnNotices(retried.Record).Should().HaveCount(2);
    }

    [Test]
    public void FeatsGrantedByTheRefundedPerksLeaveTheCharacterFile()
    {
        var retiredFeats = Table.SelectMany(perk => perk.Feats).ToArray();
        var file = WithFeats(SavedCharacterFile.Create(PreviousVersion), retiredFeats.Append(FeatType.AlertnessTrait).ToArray());
        FeatsOf(file).Should().Contain(retiredFeats);

        var upgraded = CharacterMigrations.Upgrade(file, Record(PreviousVersion))!;

        FeatsOf(upgraded.File).Should().Equal(FeatType.AlertnessTrait);
    }

    [Test]
    public void ARestoredOlderFileLosesTheFeatsWithoutChangingTheRecord()
    {
        var retiredFeats = Table.SelectMany(perk => perk.Feats).ToArray();
        var file = WithFeats(SavedCharacterFile.Create(PreviousVersion), retiredFeats);
        var record = Record(PreviousVersion + 1);
        record.Perks[PerkType.Alertness] = 1;

        var upgraded = CharacterMigrations.Upgrade(file, record)!;

        FeatsOf(upgraded.File).Should().BeEmpty();
        upgraded.Record.UnallocatedSP.Should().Be(StartingSP + LaterSP);
        upgraded.Record.Perks.Should().ContainKey(PerkType.Alertness);
        upgraded.Record.Currencies[CurrencyType.RebuildToken].Should().Be(StartingTokens + LaterTokens, "only the later migrations' tokens");
        OwnNotices(upgraded.Record).Should().BeEmpty();
    }

    [Test]
    public void TheRefundTableIsIndependentOfThePerkDefinitions()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server",
            "Feature", "MigrationDefinition", "CharacterMigration", MigrationName + ".cs"));

        source.Should().NotContain("Perk.GetPerkDetails", "the retired definitions no longer exist");
        source.Should().NotContain("LegacyPerkRefundMigration", "its prices predate what players paid after the rebuild");
    }

    private static byte[] WithFeats(byte[] file, params FeatType[] feats)
    {
        var character = LoadCharacter(file);
        var addFeat = SavedCharacterType.GetMethod("AddFeat")!;
        foreach (var feat in feats)
            addFeat.Invoke(character, new object[] { feat });
        return (byte[])SavedCharacterType.GetMethod("Serialize")!.Invoke(character, null)!;
    }

    private static IReadOnlyList<FeatType> FeatsOf(byte[] file) =>
        (IReadOnlyList<FeatType>)SavedCharacterType.GetProperty("Feats")!.GetValue(LoadCharacter(file))!;

    private static object LoadCharacter(byte[] file) =>
        Activator.CreateInstance(SavedCharacterType,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object?[] { file, null }, null)!;

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private sealed record PerkPrices(PerkType Perk, string Name, int[] Prices, int FullRefund, params FeatType[] Feats);
}
