using FluentAssertions;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.IntegrationTests.Persistence;

/// <summary>The query shapes the services use, run through the real search indexes: tag filters, escaped tokens, paging, ordering and counts.</summary>
[TestFixture]
public class QueryTests : IntegrationTestBase
{
    private InventoryItem Store(string id, string playerId, string name, long ticks, string storageId = "GLOBAL_BANK")
    {
        var item = new InventoryItem
        {
            Id = id,
            PlayerId = playerId,
            StorageId = storageId,
            Name = name,
            Tag = "tag-" + id,
            Resref = "resref" + id.Replace("-", string.Empty),
            Quantity = 1,
            Data = "blob-" + id,
            DepositedTicks = ticks,
        };
        Db.Set(item);
        return item;
    }

    [Test]
    public void TagFilters_AreAndedAndOnlyMatchTheirOwnType()
    {
        Store("i-1", "player-a", "Blaster", 1);
        Store("i-2", "player-a", "Rifle", 2, storageId: "CITY_BANK");
        Store("i-3", "player-b", "Pistol", 3);
        Db.Set(new MarketItem { Id = "m-1", PlayerId = "player-a", Name = "Blaster", MarketId = "m", IsListed = true });

        var both = new DBQuery<InventoryItem>()
            .AddFieldSearch(nameof(InventoryItem.StorageId), "GLOBAL_BANK", false)
            .AddFieldSearch(nameof(InventoryItem.PlayerId), "player-a", false);
        Db.SearchIds(both).Should().Equal("i-1");

        var byPlayer = new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.PlayerId), "player-a", false);
        Db.SearchIds(byPlayer).Should().BeEquivalentTo("i-1", "i-2");
        Db.SearchCount(byPlayer).Should().Be(2);

        Db.SearchCount(new DBQuery<InventoryItem>()).Should().Be(3, "the market item is another type");
        Db.SearchCount(new DBQuery<MarketItem>()).Should().Be(1);
        Db.SearchCount(new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.IsListed), true)).Should().Be(1);
        Db.SearchCount(new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.IsListed), false)).Should().Be(0);
    }

    [Test]
    public void ListFilters_MatchAnyValue()
    {
        Store("i-1", "player-a", "Blaster", 1);
        Store("i-2", "player-b", "Rifle", 2);
        Store("i-3", "player-c", "Pistol", 3);

        var query = new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.PlayerId), new[] { "player-a", "player-c" });
        Db.SearchIds(query).Should().BeEquivalentTo("i-1", "i-3");

        Db.Set(new MarketItem { Id = "m-1", Category = MarketCategoryType.Vibroblade, IsListed = true });
        Db.Set(new MarketItem { Id = "m-2", Category = MarketCategoryType.Vibroknife, IsListed = true });
        Db.Set(new MarketItem { Id = "m-3", Category = MarketCategoryType.Spear, IsListed = true });
        var categories = new DBQuery<MarketItem>()
            .AddFieldSearch(nameof(MarketItem.Category), new[] { (int)MarketCategoryType.Vibroblade, (int)MarketCategoryType.Spear });
        Db.SearchIds(categories).Should().BeEquivalentTo("m-1", "m-3");
    }

    [TestCase("player-1@example.com")]
    [TestCase("a(b)|c=d>e'f\"g")]
    [TestCase("{curly}!bang-dash")]
    [TestCase("unicode-éè")]
    public void EscapedTokens_FindOnlyTheExactValue(string value)
    {
        Store("i-1", value, "Blaster", 1);
        Store("i-2", value + "x", "Rifle", 2);
        Store("i-3", "other", "Pistol", 3);

        var query = new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.PlayerId), value, false);
        Db.SearchIds(query).Should().Equal("i-1");
        Db.Search(query).Single().PlayerId.Should().Be(value);
        Db.EscapeTokens(value).Should().Be(DBTokenEscaper.Escape(value));
    }

    [Test]
    public void IdsWithEscapableCharacters_AreFoundByIdAndSurviveDelete()
    {
        const string id = "id-with-dashes@and(parens)";
        Store(id, "player-a", "Blaster", 1);

        Db.SearchIds(new DBQuery<InventoryItem>().AddFieldSearch(nameof(EntityBase.Id), id, false)).Should().Equal(id);
        Db.Delete<InventoryItem>(id);
        Db.SearchCount(new DBQuery<InventoryItem>().AddFieldSearch(nameof(EntityBase.Id), id, false)).Should().Be(0);
    }

    [Test]
    public void PartialMatches_RequireEveryWordToStartAWord()
    {
        Store("i-1", "player-a", "Blaster Rifle", 1);
        Store("i-2", "player-a", "Blaster Pistol", 2);
        Store("i-3", "player-a", "Vibroblade", 3);

        Db.SearchIds(new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.Name), "Blas Rif", true)).Should().Equal("i-1");
        Db.SearchIds(new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.Name), "rifle blast", true)).Should().Equal("i-1");
        Db.SearchIds(new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.Name), "Blas", true)).Should().BeEquivalentTo("i-1", "i-2");
        Db.SearchCount(new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.Name), "blade", true)).Should().Be(0,
            "only the start of a word matches");
    }

    [Test]
    public void Paging_WalksTheOrderedResultsInPages()
    {
        for (var i = 1; i <= 12; i++)
            Store($"page-{i:00}", "player-a", "Item " + i, i * 10);

        List<string> Page(int limit, int offset, bool ascending = true) => Db.SearchIds(new DBQuery<InventoryItem>()
            .AddFieldSearch(nameof(InventoryItem.PlayerId), "player-a", false)
            .OrderBy(nameof(InventoryItem.DepositedTicks), ascending)
            .AddPaging(limit, offset)).ToList();

        Page(5, 0).Should().Equal("page-01", "page-02", "page-03", "page-04", "page-05");
        Page(5, 5).Should().Equal("page-06", "page-07", "page-08", "page-09", "page-10");
        Page(5, 10).Should().Equal("page-11", "page-12");
        Page(5, 20).Should().BeEmpty();
        Page(3, 0, ascending: false).Should().Equal("page-12", "page-11", "page-10");

        var counted = new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.PlayerId), "player-a", false).AddPaging(5, 5);
        Db.SearchCount(counted).Should().Be(12, "the count ignores paging");
        Db.Search(counted).Should().HaveCount(5);
        Db.SearchRawJson(counted).Should().HaveCount(5).And.OnlyContain(json => json.Contains("\"PlayerId\":\"player-a\""));
    }

    [Test]
    public void WithoutPaging_AQueryReturnsAtMostFiftyRecords()
    {
        for (var i = 0; i < 55; i++)
            Db.Set(new MarketItem { Id = $"bulk-{i:00}", PlayerId = "seller", IsListed = true });

        var query = new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.PlayerId), "seller", false);
        Db.SearchIds(query).Should().HaveCount(50);
        Db.SearchCount(query).Should().Be(55);
        Db.SearchIds(new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.PlayerId), "seller", false).AddPaging(100, 0))
            .Should().HaveCount(55);
    }

    [Test]
    public void ChangingAnIndexedValue_MovesTheEntityBetweenResults()
    {
        var item = Store("i-1", "player-a", "Blaster", 1);
        item.PlayerId = "player-b";
        Db.Set(item);

        Db.SearchCount(new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.PlayerId), "player-a", false)).Should().Be(0);
        Db.SearchIds(new DBQuery<InventoryItem>().AddFieldSearch(nameof(InventoryItem.PlayerId), "player-b", false)).Should().Equal("i-1");
    }

    [Test]
    public void GetWriteVersion_CountsEverySetAndDelete()
    {
        var before = Db.GetWriteVersion<InventoryItem>();
        var item = Store("i-1", "player-a", "Blaster", 1);
        Db.Set(item);
        Db.Delete<InventoryItem>("i-1");

        Db.GetWriteVersion<InventoryItem>().Should().Be(before + 3);
    }

    [Test]
    public void GetRawJson_ReturnsTheStoredDocumentAndEmptyWhenMissing()
    {
        Store("i-1", "player-a", "Blaster", 1);

        var json = Db.GetRawJson<InventoryItem>("i-1");
        json.Should().Contain("\"Name\":\"Blaster\"").And.Contain("\"Id\":\"i-1\"");
        Db.GetRawJson<InventoryItem>("missing").Should().BeEmpty();
        Db.Get<InventoryItem>("missing").Should().BeNull();
        Db.Exists<InventoryItem>("missing").Should().BeFalse();
    }

    [Test]
    public void SetUnindexedJsonProperty_ChangesOnlyThatPropertyOfTheStoredRecord()
    {
        Store("i-1", "player-a", "Blaster", 1);

        Db.SetUnindexedJsonProperty<InventoryItem>("i-1", nameof(InventoryItem.Data), "\"replaced\"");
        Db.EndFrame();

        var loaded = Db.Get<InventoryItem>("i-1");
        loaded.Data.Should().Be("replaced");
        loaded.Name.Should().Be("Blaster");
        loaded.PlayerId.Should().Be("player-a");

        Db.Invoking(db => db.SetUnindexedJsonProperty<InventoryItem>("i-1", nameof(InventoryItem.Name), "\"x\""))
            .Should().Throw<ArgumentException>("an indexed property also lives in the search entry");
        Db.Invoking(db => db.SetUnindexedJsonProperty<InventoryItem>("missing", nameof(InventoryItem.Data), "\"x\""))
            .Should().Throw<InvalidOperationException>("a partial write never creates a record");
        Redis.KeyExists("InventoryItem:missing").Should().BeFalse();
    }

    [Test]
    public void StreamAdd_AppendsEntriesAndTrimsToTheMaximumLength()
    {
        const string key = "integration:stream";
        for (var i = 0; i < 5; i++)
            Db.StreamAdd(key, new[] { new NameValueEntry("n", i) }, maxLength: 3).IsNull.Should().BeFalse();
        Redis.StreamLength(key).Should().Be(3);

        var async = Db.StreamAddAsync(key, new[] { new NameValueEntry("n", "async") }, maxLength: 3);
        async.GetAwaiter().GetResult().IsNull.Should().BeFalse();
        var entries = Redis.StreamRange(key);
        entries.Should().HaveCount(3);
        entries.Last().Values.Single().Value.ToString().Should().Be("async");
    }

    [Test]
    public void WipingTheDatabase_AlsoDropsTheJsonCachedAcrossFrames()
    {
        Db.Set(new Player("cached-player"));
        Db.EndFrame();
        Db.Get<Player>("cached-player").Should().NotBeNull("the read fills the cross-frame cache of the Player type");

        IntegrationEnvironment.Wipe();

        Db.Get<Player>("cached-player").Should().BeNull("the next test must not see a record an earlier test wrote");
    }
}
