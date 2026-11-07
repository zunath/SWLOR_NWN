using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.IntegrationTests.Chains;

/// <summary>The bank and the player market against real search indexes.</summary>
[TestFixture]
public class BankAndMarketChainTests : IntegrationTestBase
{
    private const uint ItemA = 2001;
    private const uint ItemB = 2002;
    private const string Other = "other-uuid-0002";

    [SetUp]
    public void SetUpPlayer()
    {
        ConfigurePlayer();
        Engine.SetReturn("GetItemStackSize", 3);
        Engine.SetReturn("GetResRef", "plasteel");
        Engine.SetReturnHandler("GetName", arguments => arguments[0] is ItemA ? "Plasteel Ingot" : arguments[0] is ItemB ? "Rifle Barrel" : "Test Hero");
        Engine.SetReturnHandler("GetTag", arguments => arguments[0] is ItemA ? "tag_a" : "tag_b");
        Fakes.Object.SetReturn("Serialize", "serialized-item");
    }

    [Test]
    public void DepositThenSearchThenCount_GoesThroughTheIndex()
    {
        var bank = Service<IBankService>();

        var first = bank.DepositItem(Hero, ItemA);
        var second = bank.DepositItem(Hero, ItemB);

        Redis.KeyExists($"InventoryItem:{first.Id}").Should().BeTrue();
        Engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(ItemA, ItemB);
        bank.GetItemCount(HeroId).Should().Be(2);
        bank.GetItemCount(Other).Should().Be(0);

        var page = bank.SearchItems(HeroId, "ingot", 0);
        page.Items.Select(item => item.Id).Should().Equal(first.Id);
        bank.SearchItems(HeroId, string.Empty, 0).Items.Select(item => item.Id).Should().Equal(first.Id, second.Id);
        bank.SearchItems(Other, string.Empty, 0).Items.Should().BeEmpty();

        Db.EndFrame();
        var stored = Db.Get<InventoryItem>(first.Id);
        stored.PlayerId.Should().Be(HeroId);
        stored.Name.Should().Be("Plasteel Ingot");
        stored.Quantity.Should().Be(3);
        stored.Data.Should().Be("serialized-item");
        stored.StorageId.Should().Be("GLOBAL_BANK");
    }

    [Test]
    public void AnotherPlayerCannotWithdrawAnItem_AndTheRecordStays()
    {
        var bank = Service<IBankService>();
        var deposit = bank.DepositItem(Hero, ItemA);
        Engine.SetReturn("GetObjectUUID", Other);

        bank.WithdrawItem(Hero, deposit.Id).Should().Be("That item is no longer in your bank.");

        Redis.KeyExists($"InventoryItem:{deposit.Id}").Should().BeTrue();
        bank.WithdrawItem(Hero, "missing-id").Should().Be("That item is no longer in your bank.");
    }

    [Test]
    public void ExpiredMarketListings_AreDelistedAndTheRestStay()
    {
        var market = Service<IPlayerMarketService>();
        var now = DateTime.UtcNow;
        Db.Set(new MarketItem { Id = "old-listing", PlayerId = HeroId, IsListed = true, DateListed = now.AddDays(-20), Name = "Old" });
        Db.Set(new MarketItem { Id = "new-listing", PlayerId = HeroId, IsListed = true, DateListed = now.AddDays(-2), Name = "New" });
        Db.Set(new MarketItem { Id = "sold-listing", PlayerId = HeroId, IsListed = false, DateListed = now.AddDays(-30), Name = "Sold" });

        market.RemoveOldListings();

        Db.EndFrame();
        Db.Get<MarketItem>("old-listing").IsListed.Should().BeFalse();
        Db.Get<MarketItem>("new-listing").IsListed.Should().BeTrue();
        var listed = new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.IsListed), true);
        Db.SearchIds(listed).Should().Equal("new-listing");
        Db.SearchIds(new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.IsListed), false)).Should().BeEquivalentTo("old-listing", "sold-listing");
    }

    [Test]
    public void EnteringTheModule_ReportsTheCreditsInTheMarketTill()
    {
        Db.Set(new Player(HeroId) { MarketTill = 75 });
        Db.EndFrame();

        Service<IPlayerMarketService>().CheckMarketTill();

        Messages().Should().Equal("75 credits are in your market till.");
    }
}
