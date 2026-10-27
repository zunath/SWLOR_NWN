using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum.Item;
using MarketCategoryType = SWLOR.Game.Server.Service.PlayerMarketService.MarketCategoryType;

namespace SWLOR.Game.Server.Tests.Domain.Economy;

/// <summary>
/// Unit tests of the player market instance: the engine is replaced by a recording fake and the database by an
/// in-memory one, so no server is needed. The item checks that read caches other services fill when the module loads
/// (the component, ship, fishing and music lists) are not driven here; their results come from those services.
/// </summary>
[TestFixture]
public class PlayerMarketServiceTests
{
    private const uint PlayerObject = 1001;
    private const uint ItemObject = 2002;

    // The item checks ask the Fishing facade whether an item is a rod or bait; the facade resolves from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private PlayerMarketService _service = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _service = new PlayerMarketService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
        _facadeScope.Dispose();
    }

    // ---- the cache

    [Test]
    public void CacheData_caches_the_active_categories_sorted_by_name_and_the_active_regions()
    {
        _service.CacheData();

        var expected = Enum.GetValues(typeof(MarketCategoryType)).Cast<MarketCategoryType>()
            .Select(type => (Type: type, Attribute: type.GetType().GetField(type.ToString())!
                .GetCustomAttributes(typeof(MarketCategoryAttribute), false).Cast<MarketCategoryAttribute>().Single()))
            .Where(pair => pair.Attribute.IsActive)
            .OrderBy(pair => pair.Attribute.Name)
            .Select(pair => pair.Type)
            .ToList();
        _service.GetActiveCategories().Keys.Should().Equal(expected);
        _service.GetMarketRegion(MarketRegionType.Global).MarketId.Should().Be("MARKET_GLOBAL");
    }

    [Test]
    public void Nothing_is_cached_until_CacheData_runs_and_each_instance_keeps_its_own_cache()
    {
        _service.GetActiveCategories().Should().BeEmpty();
        var region = () => _service.GetMarketRegion(MarketRegionType.Global);
        region.Should().Throw<KeyNotFoundException>();

        _service.CacheData();

        new PlayerMarketService(_db).GetActiveCategories().Should().BeEmpty("state lives on the instance, not in static fields");
    }

    [Test]
    public void GetMarketRegion_fails_for_a_region_that_is_not_active()
    {
        _service.CacheData();

        var region = () => _service.GetMarketRegion(MarketRegionType.Invalid);

        region.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void GetActiveCategories_hands_out_a_copy()
    {
        _service.CacheData();

        _service.GetActiveCategories().Clear();

        _service.GetActiveCategories().Should().NotBeEmpty();
    }

    [Test]
    public void CacheData_can_run_again_without_changing_the_cache()
    {
        _service.CacheData();
        var first = _service.GetActiveCategories().Keys.ToList();

        _service.CacheData();

        _service.GetActiveCategories().Keys.Should().Equal(first);
    }

    // ---- old listings

    [Test]
    public void RemoveOldListings_unlists_what_has_sat_on_the_market_for_two_weeks_or_more()
    {
        var old = new MarketItem { IsListed = true, DateListed = DateTime.UtcNow.AddDays(-20) };
        var edge = new MarketItem { IsListed = true, DateListed = DateTime.UtcNow.AddDays(-14).AddMinutes(-1) };
        var fresh = new MarketItem { IsListed = true, DateListed = DateTime.UtcNow.AddDays(-13) };
        var undated = new MarketItem { IsListed = true, DateListed = null };
        _db.With(old).With(edge).With(fresh).With(undated);

        _service.RemoveOldListings();

        old.IsListed.Should().BeFalse();
        edge.IsListed.Should().BeFalse();
        fresh.IsListed.Should().BeTrue();
        undated.IsListed.Should().BeTrue();
        _db.Saved.Should().BeEquivalentTo(new[] { old, edge }, options => options.WithoutStrictOrdering().Using<MarketItem>(ctx => ctx.Subject.Should().BeSameAs(ctx.Expectation)).WhenTypeIs<MarketItem>());
    }

    [Test]
    public void RemoveOldListings_asks_for_the_listed_items_and_pages_through_all_of_them()
    {
        _db.With(new MarketItem { IsListed = true, DateListed = DateTime.UtcNow });

        _service.RemoveOldListings();

        _db.Queries.Should().HaveCount(2);
        _db.Queries.Cast<DBQuery<MarketItem>>().Select(query => query.BuildQuery().QueryString)
            .Should().OnlyContain(text => text.Contains("@IsListed:1"));
        _db.Saved.Should().BeEmpty();
    }

    // ---- the till

    [Test]
    public void CheckMarketTill_tells_the_entering_player_what_is_in_their_till()
    {
        _engine.Engine.SetReturn("GetEnteringObject", PlayerObject);
        var player = new Player { Id = PlayerEngine.PlayerId };
        _db.With(player);

        player.MarketTill = 0;
        _service.CheckMarketTill();
        _engine.Messages().Should().BeEmpty();

        player.MarketTill = 1;
        _service.CheckMarketTill();
        player.MarketTill = 250;
        _service.CheckMarketTill();

        _engine.Messages().Should().Equal("1 credit is in your market till.", "250 credits are in your market till.");
    }

    [Test]
    public void CheckMarketTill_ignores_creatures_that_are_not_players_and_DMs()
    {
        _engine.Engine.SetReturn("GetEnteringObject", PlayerObject);
        _db.With(new Player { Id = PlayerEngine.PlayerId, MarketTill = 99 });

        _engine.Engine.SetReturn("GetIsPC", false);
        _service.CheckMarketTill();
        _engine.Engine.SetReturn("GetIsPC", true);
        _engine.Engine.SetReturn("GetIsDM", true);
        _service.CheckMarketTill();

        _engine.Messages().Should().BeEmpty();
        _db.Reads.Should().BeEmpty();
    }

    // ---- categories by base item

    [TestCase(BaseItem.Longsword, MarketCategoryType.Vibroblade)]
    [TestCase(BaseItem.Dagger, MarketCategoryType.Vibroknife)]
    [TestCase(BaseItem.DireMace, MarketCategoryType.HeavyVibroblade)]
    [TestCase(BaseItem.Halberd, MarketCategoryType.Spear)]
    [TestCase(BaseItem.QuarterStaff, MarketCategoryType.Staff)]
    [TestCase(BaseItem.Pistol, MarketCategoryType.Pistol)]
    [TestCase(BaseItem.ThrowingAxe, MarketCategoryType.Throwing)]
    [TestCase(BaseItem.Rifle, MarketCategoryType.Rifle)]
    [TestCase(BaseItem.DoubleAxe, MarketCategoryType.TwinBlade)]
    [TestCase(BaseItem.Katar, MarketCategoryType.Katar)]
    [TestCase(BaseItem.PowerGlove, MarketCategoryType.PowerGlove)]
    [TestCase(BaseItem.Lightsaber, MarketCategoryType.Lightsaber)]
    [TestCase(BaseItem.Saberstaff, MarketCategoryType.Saberstaff)]
    [TestCase(BaseItem.LargeShield, MarketCategoryType.Shield)]
    [TestCase(BaseItem.SmallShield, MarketCategoryType.Shield)]
    [TestCase(BaseItem.TowerShield, MarketCategoryType.Shield)]
    [TestCase(BaseItem.Cloak, MarketCategoryType.Cloak)]
    [TestCase(BaseItem.Belt, MarketCategoryType.Belt)]
    [TestCase(BaseItem.Ring, MarketCategoryType.Ring)]
    [TestCase(BaseItem.Amulet, MarketCategoryType.Necklace)]
    [TestCase(BaseItem.Helmet, MarketCategoryType.Helmet)]
    [TestCase(BaseItem.Gloves, MarketCategoryType.Glove)]
    [TestCase(BaseItem.Bracer, MarketCategoryType.Glove)]
    [TestCase(BaseItem.Boots, MarketCategoryType.Boot)]
    [TestCase(BaseItem.Armor, MarketCategoryType.Armor)]
    public void GetBaseItemMarketCategory_decides_weapons_and_wearables_by_the_base_item_alone(BaseItem baseItem, MarketCategoryType expected)
    {
        _service.GetBaseItemMarketCategory(baseItem).Should().Be(expected);
    }

    [Test]
    public void GetBaseItemMarketCategory_leaves_every_other_base_item_to_the_items_own_data()
    {
        _service.GetBaseItemMarketCategory(BaseItem.Gold).Should().BeNull();
        _service.GetBaseItemMarketCategory(BaseItem.Invalid).Should().BeNull();
    }

    // ---- categories by item

    private void ItemIs(BaseItem baseItem = BaseItem.Gold, string tag = "", string resref = "")
    {
        _engine.Engine.SetReturn("GetBaseItemType", baseItem);
        _engine.Engine.SetReturn("GetTag", tag);
        _engine.Engine.SetReturn("GetResRef", resref);
    }

    [Test]
    public void GetItemMarketCategory_uses_the_base_item_category_first()
    {
        ItemIs(BaseItem.Armor, tag: "FOOD");

        _service.GetItemMarketCategory(ItemObject).Should().Be(MarketCategoryType.Armor);
    }

    [TestCase("recipe", "", MarketCategoryType.Recipe)]
    [TestCase("RECIPE", "", MarketCategoryType.Recipe)]
    [TestCase("FOOD", "", MarketCategoryType.Food)]
    [TestCase("PET_FOOD", "", MarketCategoryType.PetFood)]
    [TestCase("INCUBATION_ENZYME", "", MarketCategoryType.Incubation)]
    [TestCase("", "beast_dna", MarketCategoryType.Incubation)]
    [TestCase("", "beast_egg", MarketCategoryType.BeastEgg)]
    [TestCase("", "ship_missile", MarketCategoryType.StarshipAmmo)]
    [TestCase("", "acm_ammo", MarketCategoryType.StarshipAmmo)]
    [TestCase("MUSIC_PLAYER", "", MarketCategoryType.Music)]
    [TestCase("", "structure_0007", MarketCategoryType.Structure)]
    [TestCase("", "just_an_item", MarketCategoryType.Miscellaneous)]
    public void GetItemMarketCategory_recognises_items_by_their_tag_and_resref(string tag, string resref, MarketCategoryType expected)
    {
        ItemIs(tag: tag, resref: resref);

        _service.GetItemMarketCategory(ItemObject).Should().Be(expected);
    }

    [Test]
    public void GetItemMarketCategory_recognises_a_structure_item_only_when_its_resref_ends_in_a_structure_id()
    {
        ItemIs(resref: "structure_abcd");

        _service.GetItemMarketCategory(ItemObject).Should().Be(MarketCategoryType.Miscellaneous);
    }

    [Test]
    public void GetItemMarketCategory_recognises_enhancements_by_their_item_property()
    {
        ItemIs();
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, false);
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.ArmorEnhancement);

        _service.GetItemMarketCategory(ItemObject).Should().Be(MarketCategoryType.Enhancement);
    }

    [Test]
    public void GetItemMarketCategory_recognises_blueprints_by_their_recipe()
    {
        ItemIs();
        _engine.Engine.SetReturnHandler("GetLocalInt", arguments => (string)arguments[1]! == "BLUEPRINT_RECIPE_ID" ? 1 : 0);

        _service.GetItemMarketCategory(ItemObject).Should().Be(MarketCategoryType.Blueprint);
    }

    [Test]
    public void The_facade_still_carries_the_listing_limit()
    {
        PlayerMarket.MaxListingsPerMarket.Should().Be(25);
    }
}
