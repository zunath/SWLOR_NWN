using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Economy;

/// <summary>
/// Unit tests of the bank instance: the engine is replaced by recording fakes (NWScript and the object plugin) and the
/// database by an in-memory one, so no server is needed. Moving an item into the inventory needs the native engine
/// (<c>Item.FitsInInventory</c>); that path is covered by the in-engine tests that deposit and withdraw items.
/// </summary>
[TestFixture]
public class BankServiceTests
{
    private const uint PlayerObject = 1001;
    private const uint ItemObject = 2002;
    private const uint BankObject = 3003;
    private const string PlayerId = PlayerEngine.PlayerId;
    private const string GlobalBank = "GLOBAL_BANK";

    private PlayerEngine _engine = null!;
    private FakeObjectPlugin _objectPlugin = null!;
    private IDisposable _objectPluginScope = null!;
    private InMemoryDBService _db = null!;
    private BankService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _objectPlugin = new FakeObjectPlugin();
        _objectPluginScope = ObjectPluginRuntime.Override(_objectPlugin);
        _db = new InMemoryDBService();
        _service = new BankService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _objectPluginScope.Dispose();
        _engine.Dispose();
    }

    private static InventoryItem BankedItem(string playerId = PlayerId, string storageId = GlobalBank, string name = "Blaster") =>
        new() { PlayerId = playerId, StorageId = storageId, Name = name, Resref = "blaster", Quantity = 1, Data = "blob" };

    private static string QueryOf(InMemoryDBService db, int index = 0) =>
        ((DBQuery<InventoryItem>)db.Queries[index]).BuildQuery().QueryString;

    // ---- capacity

    [TestCase(0, 0f)]
    [TestCase(60, 0.5f)]
    [TestCase(119, 119f / 120f)]
    [TestCase(120, 1f)]
    [TestCase(240, 1f)]
    public void GetStoragePercentage_is_the_filled_share_of_a_hundred_and_twenty_and_never_more_than_one(long count, float expected)
    {
        _service.GetStoragePercentage(count).Should().Be(expected);
    }

    [Test]
    public void GetItemCountText_shows_the_count_against_the_capacity()
    {
        _service.GetItemCountText(5).Should().Be("5 / 120 Items");
        _service.GetItemCountText(120).Should().Be("120 / 120 Items");
    }

    [TestCase(0, false)]
    [TestCase(119, false)]
    [TestCase(120, true)]
    [TestCase(500, true)]
    public void IsFull_is_true_from_a_hundred_and_twenty_items(long count, bool expected)
    {
        _service.IsFull(count).Should().Be(expected);
    }

    [Test]
    public void The_storage_id_and_the_capacity_are_the_canonical_ones()
    {
        // Private constants of the instance: the bank is one global storage of a hundred and twenty items.
        var type = typeof(BankService);
        const System.Reflection.BindingFlags NonPublicStatic = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;

        type.GetField("StorageId", NonPublicStatic)!.IsLiteral.Should().BeTrue();
        type.GetField("StorageId", NonPublicStatic)!.GetRawConstantValue().Should().Be(GlobalBank);
        type.GetField("MaxItems", NonPublicStatic)!.IsLiteral.Should().BeTrue();
        type.GetField("MaxItems", NonPublicStatic)!.GetRawConstantValue().Should().Be(120);
    }

    // ---- reading the bank

    [Test]
    public void GetItemCount_counts_the_players_items_in_the_global_bank()
    {
        _db.With(BankedItem()).With(BankedItem(name: "Rifle"));

        _service.GetItemCount(PlayerId).Should().Be(2);

        QueryOf(_db).Should().Contain("@StorageId:GLOBAL_BANK").And.Contain("@PlayerId:player\\-uuid\\-1");
    }

    [Test]
    public void SearchItems_without_text_lists_the_players_items_in_the_global_bank()
    {
        var blaster = BankedItem();
        _db.With(blaster);

        var items = _service.SearchItems(PlayerId, "  ");

        items.Should().ContainSingle().Which.Should().BeSameAs(blaster);
        QueryOf(_db).Should().Contain("@StorageId:GLOBAL_BANK").And.Contain("@PlayerId:player\\-uuid\\-1").And.NotContain("@Name");
    }

    [Test]
    public void SearchItems_with_text_narrows_the_search_by_name()
    {
        _db.With(BankedItem());

        _service.SearchItems(PlayerId, "blast");

        QueryOf(_db).Should().Contain("@StorageId:GLOBAL_BANK").And.Contain("@Name:");
    }

    // ---- depositing

    [Test]
    public void GetDepositFailure_names_the_reason_an_item_cannot_be_stored()
    {
        // Not carried by the player.
        _engine.Engine.SetReturn("GetItemPossessor", 0x7F000000u);
        _service.GetDepositFailure(PlayerObject, ItemObject).Should().Be("Item must be in your inventory.");

        // A container.
        _engine.Engine.SetReturn("GetItemPossessor", PlayerObject);
        _engine.Engine.SetReturn("GetHasInventory", true);
        _service.GetDepositFailure(PlayerObject, ItemObject).Should().Be("Containers cannot be stored.");
        _engine.Engine.SetReturn("GetHasInventory", false);

        // Credits.
        _engine.Engine.SetReturn("GetBaseItemType", BaseItem.Gold);
        _service.GetDepositFailure(PlayerObject, ItemObject).Should().Be("Credits cannot be placed inside.");
        _engine.Engine.SetReturn("GetBaseItemType", BaseItem.Longsword);

        // Cursed.
        _engine.Engine.SetReturn("GetItemCursedFlag", true);
        _service.GetDepositFailure(PlayerObject, ItemObject).Should().Be("That item cannot be stored.");
        _engine.Engine.SetReturn("GetItemCursedFlag", false);

        // Equipped.
        _engine.Engine.SetReturnHandler("GetItemInSlot", arguments => (InventorySlot)arguments[0]! == InventorySlot.Chest ? ItemObject : 0x7F000000u);
        _service.GetDepositFailure(PlayerObject, ItemObject).Should().Be("Unequip the item first.");
    }

    [Test]
    public void GetDepositFailure_refuses_a_droid_controller()
    {
        _engine.Engine.SetReturn("GetItemPossessor", PlayerObject);
        _engine.Engine.SetReturn("GetResRef", SWLOR.Game.Server.Service.Droid.DroidControlItemResref);

        _service.GetDepositFailure(PlayerObject, ItemObject).Should().Be("That item cannot be stored.");
    }

    [Test]
    public void GetDepositFailure_says_the_bank_is_full_at_the_capacity_and_nothing_below_it()
    {
        _engine.Engine.SetReturn("GetItemPossessor", PlayerObject);
        for (var i = 0; i < 119; i++)
            _db.With(BankedItem());

        _service.GetDepositFailure(PlayerObject, ItemObject).Should().BeEmpty();

        _db.With(BankedItem());

        _service.GetDepositFailure(PlayerObject, ItemObject).Should().Be("Your bank is full.");
    }

    [Test]
    public void DepositItem_stores_the_item_under_the_global_bank_destroys_it_and_returns_the_record()
    {
        _engine.Engine.SetReturn("GetName", "Heavy Blaster");
        _engine.Engine.SetReturn("GetTag", "BLASTER_TAG");
        _engine.Engine.SetReturn("GetResRef", "heavy_blaster");
        _engine.Engine.SetReturn("GetItemStackSize", 4);
        _objectPlugin.SetReturn("Serialize", "serialized-item");

        var stored = _service.DepositItem(PlayerObject, ItemObject);

        stored.StorageId.Should().Be(GlobalBank);
        stored.PlayerId.Should().Be(PlayerId);
        stored.Name.Should().Be("Heavy Blaster");
        stored.Tag.Should().Be("BLASTER_TAG");
        stored.Resref.Should().Be("heavy_blaster");
        stored.Quantity.Should().Be(4);
        stored.Data.Should().Be("serialized-item");
        stored.IconResref.Should().NotBeNullOrEmpty();
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(stored);
        _engine.Engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(ItemObject);
    }

    // ---- withdrawing

    [Test]
    public void WithdrawItem_refuses_a_record_that_is_missing_another_players_or_in_another_storage()
    {
        var theirs = BankedItem(playerId: "someone-else");
        var oldStorage = BankedItem(storageId: "OLD_BANK");
        _db.With(theirs).With(oldStorage);

        _service.WithdrawItem(PlayerObject, "no-such-id").Should().Be("That item is no longer in your bank.");
        _service.WithdrawItem(PlayerObject, theirs.Id).Should().Be("That item is no longer in your bank.");
        _service.WithdrawItem(PlayerObject, oldStorage.Id).Should().Be("That item is no longer in your bank.");

        _db.Deleted.Should().BeEmpty();
        _objectPlugin.CallsTo("Deserialize").Should().BeEmpty("a record that is not the player's is never rebuilt");
    }

    [Test]
    public void WithdrawItem_keeps_the_record_when_the_item_cannot_be_rebuilt()
    {
        var mine = BankedItem();
        _db.With(mine);
        // The object plugin answers OBJECT_INVALID, and GetIsObjectValid answers false: the saved data is unusable.

        _service.WithdrawItem(PlayerObject, mine.Id).Should().Be("That item could not be withdrawn.");

        _db.Deleted.Should().BeEmpty();
        _objectPlugin.CallsTo("Deserialize").Should().ContainSingle().Which.Arguments[0].Should().Be("blob");
    }

    // ---- terminals

    [Test]
    public void SetCityBankId_stores_the_city_on_the_terminal()
    {
        _service.SetCityBankId(BankObject, "city-1");

        var call = _engine.Engine.CallsTo("SetLocalString").Should().ContainSingle().Subject;
        call.Arguments.Should().Equal(BankObject, "CITY_ID", "city-1");
    }

    [Test]
    public void NormalizeStorageId_moves_old_storage_ids_to_the_global_bank()
    {
        var old = BankedItem(storageId: "BANK_17");
        var current = BankedItem();

        _service.NormalizeStorageId(old).Should().BeTrue();
        _service.NormalizeStorageId(current).Should().BeFalse();

        old.StorageId.Should().Be(GlobalBank);
        current.StorageId.Should().Be(GlobalBank);
    }

    [Test]
    public void GetCityBankAccessFailure_only_lets_citizens_without_taxes_owed_in()
    {
        var player = new Player { Id = PlayerId, CitizenPropertyId = "city-1" };
        _db.With(player);

        // Not a player.
        _engine.Engine.SetReturn("GetIsPC", false);
        _service.GetCityBankAccessFailure(PlayerObject, BankObject).Should().Be("Only players can access this bank.");
        _engine.Engine.SetReturn("GetIsPC", true);
        _engine.Engine.SetReturn("GetIsDM", true);
        _service.GetCityBankAccessFailure(PlayerObject, BankObject).Should().Be("Only players can access this bank.");
        _engine.Engine.SetReturn("GetIsDM", false);

        // A terminal with no city.
        _engine.Engine.SetReturn("GetLocalString", string.Empty);
        _service.GetCityBankAccessFailure(PlayerObject, BankObject).Should().Be("This bank terminal is not configured for a city.");

        // A city the player does not belong to.
        _engine.Engine.SetReturn("GetLocalString", "city-2");
        _service.GetCityBankAccessFailure(PlayerObject, BankObject).Should().Be("Only citizens may use this terminal.");

        // A citizen who owes taxes, and one who does not.
        _engine.Engine.SetReturn("GetLocalString", "city-1");
        player.PropertyOwedTaxes = 250;
        _service.GetCityBankAccessFailure(PlayerObject, BankObject).Should().Be(
            "You owe 250 credits in taxes to this city. You cannot use its facilities until these are paid. Use the Citizenship Terminal in City Hall to pay these.");
        player.PropertyOwedTaxes = 0;
        _service.GetCityBankAccessFailure(PlayerObject, BankObject).Should().BeEmpty();
    }

    [Test]
    public void Each_instance_reads_its_own_database()
    {
        var other = new BankService(new InMemoryDBService());
        _db.With(BankedItem());

        _service.GetItemCount(PlayerId).Should().Be(1);
        other.GetItemCount(PlayerId).Should().Be(0);
    }
}
