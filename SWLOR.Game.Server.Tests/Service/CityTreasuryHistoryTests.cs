#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;
using static SWLOR.Game.Server.Tests.Feature.SpaceItemFixtures;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The city treasury history shown in Manage City's Treasury tab: 30 days kept, a cap on lines, small frequent income kept
/// as one line per day, how lines read, and that every change to a treasury writes one.
/// </summary>
public class CityTreasuryHistoryTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 18, 30, 0, DateTimeKind.Utc);

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(RepositoryRoot(), "SWLOR.Game.Server"), Path.Combine)).Replace("\r\n", "\n");

    private static CityTreasuryHistoryEntry Line(CityTreasuryEntryType type, long amount, DateTime time, long balance = 0) =>
        new() { Type = type, Amount = amount, TimeUtc = time, Balance = balance };

    [Test]
    public void SmallIncome_IsOneLinePerKindPerDay()
    {
        var entries = new List<CityTreasuryHistoryEntry>();
        CityTreasuryHistoryRules.Add(entries, Line(CityTreasuryEntryType.ExchangeTariffs, 10, Now, 1010));
        CityTreasuryHistoryRules.Add(entries, Line(CityTreasuryEntryType.ExchangeTariffs, 15, Now.AddMinutes(5), 1025));
        CityTreasuryHistoryRules.Add(entries, Line(CityTreasuryEntryType.StoreSalesTax, 4, Now.AddMinutes(6), 1029));

        entries.Should().HaveCount(2, "a busy exchange must not add a line per trade");
        var tariffs = entries[0];
        tariffs.Amount.Should().Be(25);
        tariffs.Count.Should().Be(2);
        tariffs.Balance.Should().Be(1025);
        CityTreasuryHistoryRules.Describe(tariffs, string.Empty).Should().Be("Exchange tariffs (2 trades)");

        CityTreasuryHistoryRules.Add(entries, Line(CityTreasuryEntryType.ExchangeTariffs, 5, Now.AddDays(1), 1034));
        entries.Should().HaveCount(3, "the next day starts its own total");
    }

    [Test]
    public void OfficialsActions_AreLinesOfTheirOwn()
    {
        var entries = new List<CityTreasuryHistoryEntry>();
        CityTreasuryHistoryRules.Add(entries, Line(CityTreasuryEntryType.Withdrawal, -500, Now));
        CityTreasuryHistoryRules.Add(entries, Line(CityTreasuryEntryType.Withdrawal, -500, Now.AddMinutes(1)));
        entries.Should().HaveCount(2, "each withdrawal shows who made it");
    }

    [Test]
    public void LinesOlderThanThirtyDays_AreDropped_AndTheCountIsCapped()
    {
        var entries = new List<CityTreasuryHistoryEntry>
        {
            Line(CityTreasuryEntryType.Deposit, 100, Now.AddDays(-31)),
            Line(CityTreasuryEntryType.Deposit, 100, Now.AddDays(-29)),
        };
        CityTreasuryHistoryRules.Prune(entries, Now).Should().BeTrue();
        entries.Should().ContainSingle().Which.TimeUtc.Should().Be(Now.AddDays(-29));
        CityTreasuryHistoryRules.KeepDays.Should().Be(30);

        for (var i = 0; i < CityTreasuryHistoryRules.MaxEntries + 20; i++)
            CityTreasuryHistoryRules.Add(entries, Line(CityTreasuryEntryType.Deposit, 1, Now.AddSeconds(i)));
        entries.Should().HaveCount(CityTreasuryHistoryRules.MaxEntries, "a record never grows past the cap, whatever the activity");
        entries.Last().TimeUtc.Should().Be(Now.AddSeconds(CityTreasuryHistoryRules.MaxEntries + 19), "the oldest go first");
    }

    [Test]
    public void Lines_ReadPlainly()
    {
        CityTreasuryHistoryRules.FormatAmount(1200).Should().Be("+1,200");
        CityTreasuryHistoryRules.FormatAmount(-50_000).Should().Be("-50,000");
        CityTreasuryHistoryRules.FormatAmount(0).Should().Be("0");
        CityTreasuryHistoryRules.FormatTime(Now).Should().Be("Nov 14 18:30");

        CityTreasuryHistoryRules.Describe(new CityTreasuryHistoryEntry { Type = CityTreasuryEntryType.BountyPosted, Detail = "a hooded Zabrak" }, "Ana")
            .Should().Be("Bounty posted on a hooded Zabrak by Ana");
        CityTreasuryHistoryRules.Describe(new CityTreasuryHistoryEntry
            {
                Type = CityTreasuryEntryType.BountyCollected, EscrowCredits = 50_000, Detail = "a hooded Zabrak, by a tall Human",
            }, string.Empty)
            .Should().Be("Bounty of 50,000 collected: a hooded Zabrak, by a tall Human");
        CityTreasuryHistoryRules.Describe(new CityTreasuryHistoryEntry { Type = CityTreasuryEntryType.UpkeepPaid, Restricted = -300 }, "Ana")
            .Should().Be("Upkeep paid by Ana (300 restricted)");

        foreach (var type in Enum.GetValues<CityTreasuryEntryType>().Where(x => x != CityTreasuryEntryType.Invalid))
        {
            var text = CityTreasuryHistoryRules.Describe(new CityTreasuryHistoryEntry { Type = type, Detail = "x" }, "Ana");
            text.Should().NotBe("x", $"{type} has its own wording");
            text.Should().MatchRegex("^[\\x20-\\x7E]+$", "game text is plain ASCII");
        }
    }

    [Test]
    public void TheRecord_HasItsOwnId_AndKeepsItsLines()
    {
        var history = new CityTreasuryHistory("city-1");
        history.Id.Should().Be("treasury-history:city-1", "the bare id is the city's own in the DB cache");
        history.CityId.Should().Be("city-1");
        typeof(CityTreasuryHistory).GetProperty(nameof(CityTreasuryHistory.CityId))!
            .GetCustomAttributes(typeof(IndexedAttribute), false).Should().NotBeEmpty();
    }

    [Test]
    public void EveryTreasuryChange_WritesALine()
    {
        var record = "CityTreasuryHistoryLog.Record(";
        var sites = new (string[] Path, string Type)[]
        {
            (new[] { "Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs" }, "Withdrawal"),
            (new[] { "Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs" }, "Deposit"),
            (new[] { "Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs" }, "UpkeepPaid"),
            (new[] { "Feature", "GuiDefinition", "ViewModel", "ManageCitizenshipViewModel.cs" }, "CitizenTaxes"),
            (new[] { "Service", "CityStores.cs" }, "StoreSalesTax"),
            (new[] { "Service", "Shuttle.cs" }, "TransportationTax"),
            (new[] { "Service", "StationBuildings.cs" }, "ShipyardFees"),
            (new[] { "Service", "TradeCity.cs" }, "ExchangeTariffs"),
            (new[] { "Service", "Edict.cs" }, "EdictEnacted"),
            (new[] { "Service", "CityAffairs.cs" }, "SituationGain"),
            (new[] { "Service", "CityBounties.cs" }, "BountyPosted"),
            (new[] { "Service", "CityBounties.cs" }, "BountyReturned"),
            (new[] { "Service", "CityBounties.cs" }, "BountyCollected"),
            (new[] { "Service", "CityProjects.cs" }, "ProjectStarted"),
            (new[] { "Service", "CityProjects.cs" }, "ProjectReturned"),
            (new[] { "Service", "CityProjects.cs" }, "ProjectCompleted"),
            (new[] { "Service", "Tournaments.cs" }, "TournamentPrizePosted"),
            (new[] { "Service", "Tournaments.Settlement.cs" }, "TournamentPrizeAwarded"),
            (new[] { "Service", "Tournaments.Settlement.cs" }, "TournamentFees"),
            (new[] { "Service", "Tournaments.Settlement.cs" }, "TournamentPrizeReturned"),
        };

        foreach (var (path, type) in sites)
            ReadSource(path).Should().MatchRegex($@"CityTreasuryHistoryLog\.Record\(\w+, [^;]*CityTreasuryEntryType\.{type}\b",
                $"{string.Join("/", path)} records {type}");

        // Every direct change to a balance outside the treasury rules is one of the sites above.
        var direct = new[]
        {
            ReadSource("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs"),
            ReadSource("Feature", "GuiDefinition", "ViewModel", "ManageCitizenshipViewModel.cs"),
            ReadSource("Service", "CityStores.cs"), ReadSource("Service", "Shuttle.cs"), ReadSource("Service", "StationBuildings.cs"),
            ReadSource("Service", "TradeCity.cs"),
        };
        foreach (var source in direct)
            source.Should().Contain(record);
    }

    [Test]
    public void TheHistory_IsShownOnlyToThoseWhoMayWithdraw_AndPrunedAtBoot()
    {
        var tab = ReadSource("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.Treasury.cs");
        tab.IndexOf("RefreshPermissions();", StringComparison.Ordinal)
            .Should().BeLessThan(tab.IndexOf("CityTreasuryHistoryLog.Get(city.Id)", StringComparison.Ordinal));
        tab.Should().Contain("if (city == null || !CanAccessTreasury)");

        var tabs = ReadSource("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.Tabs.cs");
        tabs.Should().Contain(".AddTab(TreasuryTabId, TreasuryTabPartial, model => model.RefreshTreasuryTab())");
        ReadSource("Feature", "GuiDefinition", "ManageCityDefinition.cs").Should().Contain(".AddOption(\"Treasury\")");

        var log = ReadSource("Service", "CityTreasuryHistoryLog.cs");
        log.Should().Contain("[NWNEventHandler(ScriptName.OnModuleLoad)]\n        public static void PruneAll()");
        log.Should().Contain("DB.Delete<CityTreasuryHistory>(history.Id);", "a city that is gone takes its history with it");
    }
}
