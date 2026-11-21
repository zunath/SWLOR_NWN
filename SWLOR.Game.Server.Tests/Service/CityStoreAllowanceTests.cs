using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityStoreService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Weekly allowances for limited stock: recreating a store keeps what was consumed, the week resets it, and a stall
/// theme, a city or a traveler each keep their own.
/// </summary>
public class CityStoreAllowanceTests
{
    private static readonly DateTime Monday = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime NextMonday = Monday.AddDays(7);

    private const string City = "city-1";
    private const string Banner = "cs_banner";
    private const string Drink = "cs_drink";

    /// <summary>The records the database would hold, keyed the way the entities are.</summary>
    private sealed class FakeBook
    {
        private readonly Dictionary<string, CityStockAllowance> _records = new();

        public IStockAllowanceRecord Get(string cityId, CityStoreKind kind, string resref)
        {
            return _records.GetValueOrDefault(CityStockAllowance.BuildId(cityId, kind, resref));
        }

        public void Sell(string cityId, CityStoreKind kind, string resref, int quantity, DateTime now)
        {
            var id = CityStockAllowance.BuildId(cityId, kind, resref);
            if (!_records.TryGetValue(id, out var record))
                _records[id] = record = new CityStockAllowance(cityId, kind, resref);

            StockAllowanceRules.RecordSale(record, quantity, CityWeek.GetWeekStart(now));
        }

        /// <summary>What a store of this kind puts on its shelf when it is created at this moment.</summary>
        public IReadOnlyList<CityStoreShelfEntry> CreateStore(
            string cityId, CityStoreKind kind, CityStoreLimitedItem[] items, int level, DateTime now)
        {
            return StockAllowanceRules.PlanShelf(items, level, resref => Get(cityId, kind, resref), CityWeek.GetWeekStart(now));
        }
    }

    private static CityStoreLimitedItem[] Items(int bannerLimit = 3, int bannerLevel = 1)
    {
        return new[]
        {
            new CityStoreLimitedItem(Banner, bannerLevel, bannerLimit),
            new CityStoreLimitedItem(Drink, 1, 5)
        };
    }

    private static int OnShelf(IReadOnlyList<CityStoreShelfEntry> shelf, string resref)
    {
        return shelf.FirstOrDefault(entry => entry.Resref == resref).Quantity;
    }

    #region Recreating a store

    [Test]
    public void ANewStore_ShelvesTheFullAllowance()
    {
        var shelf = new FakeBook().CreateStore(City, CityStoreKind.Cantina, Items(), 1, Monday);

        OnShelf(shelf, Banner).Should().Be(3);
        OnShelf(shelf, Drink).Should().Be(5);
    }

    [Test]
    public void RecreatingAStore_KeepsAConsumedAllowance()
    {
        var book = new FakeBook();
        book.Sell(City, CityStoreKind.Cantina, Banner, 2, Monday.AddDays(1));

        var recreated = book.CreateStore(City, CityStoreKind.Cantina, Items(), 1, Monday.AddDays(2));

        OnShelf(recreated, Banner).Should().Be(1, "two of three were sold, and a new store object does not refill them");
        OnShelf(recreated, Drink).Should().Be(5, "an item nobody bought is untouched");
    }

    [Test]
    public void ASoldOutItem_StaysOffTheShelfWhenTheStoreIsRecreatedAgainAndAgain()
    {
        var book = new FakeBook();
        book.Sell(City, CityStoreKind.Cantina, Banner, 3, Monday.AddHours(5));

        for (var restart = 1; restart <= 3; restart++)
        {
            var shelf = book.CreateStore(City, CityStoreKind.Cantina, Items(), 1, Monday.AddDays(restart));
            shelf.Any(entry => entry.Resref == Banner).Should().BeFalse($"restart {restart} must not refill a sold-out item");
        }
    }

    [Test]
    public void ASaleAfterARecreate_CountsAgainstTheSameAllowance()
    {
        var book = new FakeBook();
        book.Sell(City, CityStoreKind.Cantina, Banner, 1, Monday.AddHours(1));
        book.CreateStore(City, CityStoreKind.Cantina, Items(), 1, Monday.AddHours(2));
        book.Sell(City, CityStoreKind.Cantina, Banner, 1, Monday.AddHours(3));

        var shelf = book.CreateStore(City, CityStoreKind.Cantina, Items(), 1, Monday.AddHours(4));

        OnShelf(shelf, Banner).Should().Be(1);
    }

    #endregion

    #region The week

    [Test]
    public void TheNextWeek_RefillsTheAllowance()
    {
        var book = new FakeBook();
        book.Sell(City, CityStoreKind.Cantina, Banner, 3, Monday.AddDays(3));

        book.CreateStore(City, CityStoreKind.Cantina, Items(), 1, NextMonday.AddMinutes(-1))
            .Any(entry => entry.Resref == Banner).Should().BeFalse("still the same week one minute before the boundary");

        OnShelf(book.CreateStore(City, CityStoreKind.Cantina, Items(), 1, NextMonday), Banner)
            .Should().Be(3, "the weekly boundary resets the allowance");
    }

    [Test]
    public void ASaleInANewWeek_StartsANewCountInsteadOfAddingToTheOldOne()
    {
        var book = new FakeBook();
        book.Sell(City, CityStoreKind.Cantina, Banner, 3, Monday.AddDays(2));
        book.Sell(City, CityStoreKind.Cantina, Banner, 1, NextMonday.AddHours(2));

        var record = book.Get(City, CityStoreKind.Cantina, Banner);
        record.WeekStartUtc.Should().Be(NextMonday);
        record.Sold.Should().Be(1);
        OnShelf(book.CreateStore(City, CityStoreKind.Cantina, Items(), 1, NextMonday.AddDays(1)), Banner).Should().Be(2);
    }

    [Test]
    public void TheWeek_RunsFromMondayMidnightUtc()
    {
        CityWeek.GetWeekStart(new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)).Should().Be(Monday);
        CityWeek.GetWeekStart(new DateTime(2026, 11, 2, 0, 0, 1, DateTimeKind.Utc)).Should().Be(Monday);
        CityWeek.GetWeekStart(new DateTime(2026, 11, 8, 23, 59, 59, DateTimeKind.Utc)).Should().Be(Monday, "Sunday night is still the old week");
        CityWeek.GetWeekStart(new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc)).Should().Be(NextMonday);
        CityWeek.GetWeekStart(new DateTime(2026, 11, 4, 15, 30, 0, DateTimeKind.Utc)).Should().Be(Monday);
    }

    [Test]
    public void TheNextBoundary_IsOneWeekAfterTheCurrentOne()
    {
        CityWeek.GetNextWeekStart(Monday.AddDays(3)).Should().Be(NextMonday);
        CityWeek.GetNextWeekStart(Monday).Should().Be(NextMonday);
    }

    #endregion

    #region Whose allowance it is

    [Test]
    public void ASwitchedStallTheme_KeepsEachThemesAllowance_AndGoingBackDoesNotRefill()
    {
        var book = new FakeBook();
        book.Sell(City, CityStoreKind.StallDecor, Banner, 2, Monday.AddHours(6));

        OnShelf(book.CreateStore(City, CityStoreKind.StallGeneralGoods, Items(), 1, Monday.AddHours(7)), Banner)
            .Should().Be(3, "another theme has its own allowance");

        OnShelf(book.CreateStore(City, CityStoreKind.StallDecor, Items(), 1, Monday.AddHours(8)), Banner)
            .Should().Be(1, "switching away and back never refills the decor allowance");
    }

    [Test]
    public void ADifferentCity_HasItsOwnAllowance()
    {
        var book = new FakeBook();
        book.Sell(City, CityStoreKind.Cantina, Banner, 3, Monday.AddHours(1));

        OnShelf(book.CreateStore("city-2", CityStoreKind.Cantina, Items(), 1, Monday.AddHours(2)), Banner).Should().Be(3);
    }

    [Test]
    public void ABuildingsAllowance_BelongsToTheBuildingAndSurvivesAFacilityLevelChange()
    {
        var book = new FakeBook();
        book.Sell(City, CityStoreKind.Cantina, Banner, 2, Monday.AddHours(1));

        OnShelf(book.CreateStore(City, CityStoreKind.Cantina, Items(), 3, Monday.AddHours(2)), Banner)
            .Should().Be(1, "the record does not hold the level");
    }

    [Test]
    public void AnItemFromAHigherLevel_IsStockedOnlyOnceThatLevelIsReached()
    {
        var book = new FakeBook();
        var items = Items(bannerLevel: 3);

        book.CreateStore(City, CityStoreKind.Cantina, items, 2, Monday).Any(entry => entry.Resref == Banner).Should().BeFalse();
        OnShelf(book.CreateStore(City, CityStoreKind.Cantina, items, 3, Monday), Banner).Should().Be(3);
    }

    [Test]
    public void EveryCityRecord_HasOneIdPerCityKindAndItem()
    {
        var ids = new[]
        {
            CityStockAllowance.BuildId("a", CityStoreKind.Cantina, Banner),
            CityStockAllowance.BuildId("b", CityStoreKind.Cantina, Banner),
            CityStockAllowance.BuildId("a", CityStoreKind.StallDecor, Banner),
            CityStockAllowance.BuildId("a", CityStoreKind.Cantina, Drink)
        };

        ids.Distinct().Should().HaveCount(4);
        CityStockAllowance.BuildId("a", CityStoreKind.Cantina, Banner).Should().Be(new CityStockAllowance("a", CityStoreKind.Cantina, Banner).Id);
    }

    [Test]
    public void ATravelersAllowance_BelongsToTheTravelerNotTheCity()
    {
        var record = new TravelerStockAllowance("BeastTrader", "cs_pet");
        StockAllowanceRules.RecordSale(record, 2, CityWeek.GetWeekStart(Monday.AddDays(1)));

        StockAllowanceRules.GetRemaining(5, record, Monday).Should().Be(3);
        StockAllowanceRules.GetRemaining(5, record, NextMonday).Should().Be(5, "it resets at the same weekly boundary");
        record.Id.Should().Be(TravelerStockAllowance.BuildId("BeastTrader", "cs_pet"));
        TravelerStockAllowance.BuildId("BeastTrader", "cs_pet").Should().NotBe(TravelerStockAllowance.BuildId("Prospector", "cs_pet"));
    }

    #endregion

    #region Buying

    [Test]
    public void APurchase_IsRefusedPastWhatIsLeft()
    {
        var record = new CityStockAllowance(City, CityStoreKind.Cantina, Banner);
        StockAllowanceRules.RecordSale(record, 2, Monday);

        StockAllowanceRules.CanSell(3, record, Monday, 1).Should().BeTrue();
        StockAllowanceRules.CanSell(3, record, Monday, 2).Should().BeFalse();
        StockAllowanceRules.CanSell(3, record, NextMonday, 3).Should().BeTrue("the new week starts full");
        StockAllowanceRules.CanSell(3, null, Monday, 0).Should().BeFalse("nothing is bought for nothing");
    }

    [Test]
    public void ARecordedSale_NeverGoesNegative()
    {
        var record = new CityStockAllowance(City, CityStoreKind.Cantina, Banner);
        StockAllowanceRules.RecordSale(record, -4, Monday);

        record.Sold.Should().Be(0);
    }

    #endregion
}
