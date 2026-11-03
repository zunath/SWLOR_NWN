using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// Unit tests of the key item instance: the engine is a recording fake with real local variables, the database an
/// in-memory one. The key item window refresh that giving a key item publishes goes to the GUI service, which has no
/// windows open here and so does nothing; hiding the placeable goes to the visibility service (not converted yet,
/// reads and saves the player through the static database), so the test records that call instead.
/// </summary>
[TestFixture]
public class KeyItemServiceTests
{
    private const uint PlayerObject = 1001;
    private const uint Placeable = 2002;
    private const string PlayerId = PlayerEngine.PlayerId;

    private PlayerEngine _engine = null!;
    private LocalVariables _locals = null!;
    private InMemoryDBService _db = null!;
    private Visibility _service = null!;
    private KeyItemType _first;
    private KeyItemType _second;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _locals = new LocalVariables(_engine.Engine);
        _db = new InMemoryDBService();
        _service = new Visibility(_db);
        _service.LoadData();

        var active = _service.GetActiveKeyItemsByCategory(KeyItemCategoryType.Keys).Keys.ToArray();
        _first = active[0];
        _second = active[1];
        _db.With(new Player(PlayerId));
        _engine.Engine.SetReturn("GetLastUsedBy", PlayerObject);
        _engine.Engine.SetReturn("OBJECT_SELF", Placeable);
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
    }

    // ---- the cache

    [Test]
    public void LoadData_CachesEveryCategoryAndKeyItemWithItsDetails()
    {
        foreach (var category in Enum.GetValues<KeyItemCategoryType>())
            _service.GetKeyItemCategory(category).Should().NotBeNull($"{category} is cached");

        foreach (var keyItem in Enum.GetValues<KeyItemType>())
        {
            var detail = _service.GetKeyItem(keyItem);
            detail.Should().NotBeNull($"{keyItem} is cached");
            _service.GetKeyItemTypeById((int)keyItem).Should().Be(keyItem);
            _service.GetKeyItemTypeByName(keyItem.ToString()).Should().Be(keyItem);
        }
    }

    [Test]
    public void GetKeyItemType_OfAnUnknownIdOrName_IsInvalid()
    {
        _service.GetKeyItemTypeById(-12345).Should().Be(KeyItemType.Invalid);
        _service.GetKeyItemTypeByName("NoSuchKeyItem").Should().Be(KeyItemType.Invalid);
    }

    [Test]
    public void GetActiveCategories_ListsOnlyActiveCategoriesAndHandsOutACopy()
    {
        var active = _service.GetActiveCategories();

        active.Should().NotBeEmpty();
        active.Values.Should().OnlyContain(category => category.IsActive);
        Enum.GetValues<KeyItemCategoryType>()
            .Where(category => !_service.GetKeyItemCategory(category).IsActive)
            .Should().NotIntersectWith(active.Keys);

        active.Clear();
        _service.GetActiveCategories().Should().NotBeEmpty("the caller got a copy");
    }

    [Test]
    public void GetActiveKeyItemsByCategory_ListsOnlyActiveKeyItemsOfThatCategoryAndHandsOutACopy()
    {
        var keys = _service.GetActiveKeyItemsByCategory(KeyItemCategoryType.Keys);

        keys.Should().NotBeEmpty();
        keys.Values.Should().OnlyContain(detail => detail.IsActive && detail.Category == KeyItemCategoryType.Keys);

        keys.Clear();
        _service.GetActiveKeyItemsByCategory(KeyItemCategoryType.Keys).Should().NotBeEmpty("the caller got a copy");
    }

    [Test]
    public void GetActiveKeyItemsByCategory_OfAnInactiveOrUnknownCategory_IsEmpty()
    {
        _service.GetActiveKeyItemsByCategory((KeyItemCategoryType)(-1)).Should().BeEmpty();
    }

    // ---- giving, removing and checking

    [Test]
    public void GiveKeyItem_RecordsItTellsThePlayerAndDoesNothingTheSecondTime()
    {
        _service.GiveKeyItem(PlayerObject, _first);
        _service.GiveKeyItem(PlayerObject, _first);

        var saved = _db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>().Subject;
        saved.KeyItems.Should().ContainKey(_first);
        saved.KeyItems[_first].Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        _engine.Messages().Should().Equal($"You acquire the '{_service.GetKeyItem(_first).Name}' key item.");
    }

    [Test]
    public void GiveKeyItem_IgnoresDMsAndCreaturesThatAreNotPlayers()
    {
        _engine.Engine.SetReturn("GetIsPC", false);
        _service.GiveKeyItem(PlayerObject, _first);

        _engine.Engine.SetReturn("GetIsPC", true);
        _engine.Engine.SetReturn("GetIsDM", true);
        _service.GiveKeyItem(PlayerObject, _first);

        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void RemoveKeyItem_DeletesItTellsThePlayerAndDoesNothingWhenTheyLackIt()
    {
        _service.RemoveKeyItem(PlayerObject, _first);
        _db.Saved.Should().BeEmpty("the player never had it");

        _service.GiveKeyItem(PlayerObject, _first);
        _db.Saved.Clear();
        _service.RemoveKeyItem(PlayerObject, _first);

        var saved = _db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>().Subject;
        saved.KeyItems.Should().NotContainKey(_first);
        _engine.Messages().Should().EndWith($"You lost the '{_service.GetKeyItem(_first).Name}' key item.");
    }

    [Test]
    public void RemoveKeyItem_IgnoresDMsAndCreaturesThatAreNotPlayers()
    {
        _service.GiveKeyItem(PlayerObject, _first);
        _db.Saved.Clear();
        _engine.Engine.SetReturn("GetIsDM", true);

        _service.RemoveKeyItem(PlayerObject, _first);

        _db.Saved.Should().BeEmpty();
        _db.Get<Player>(PlayerId).KeyItems.Should().ContainKey(_first);
    }

    [Test]
    public void HasKeyItem_ReportsWhatThePlayerHoldsAndNeverForDMsOrNonPlayers()
    {
        _service.HasKeyItem(PlayerObject, _first).Should().BeFalse();

        _service.GiveKeyItem(PlayerObject, _first);
        _service.HasKeyItem(PlayerObject, _first).Should().BeTrue();
        _service.HasKeyItem(PlayerObject, _second).Should().BeFalse();

        _engine.Engine.SetReturn("GetIsDM", true);
        _service.HasKeyItem(PlayerObject, _first).Should().BeFalse();
        _engine.Engine.SetReturn("GetIsDM", false);
        _engine.Engine.SetReturn("GetIsPC", false);
        _service.HasKeyItem(PlayerObject, _first).Should().BeFalse();
    }

    [Test]
    public void HasAllKeyItems_NeedsEveryOneAndTreatsNoRequirementAsMet()
    {
        _service.GiveKeyItem(PlayerObject, _first);

        _service.HasAllKeyItems(PlayerObject, new List<KeyItemType> { _first }).Should().BeTrue();
        _service.HasAllKeyItems(PlayerObject, new List<KeyItemType> { _first, _second }).Should().BeFalse();
        _service.HasAllKeyItems(PlayerObject, new List<KeyItemType>()).Should().BeTrue();
        _service.HasAllKeyItems(PlayerObject, null!).Should().BeTrue("no key items specified");

        _engine.Engine.SetReturn("GetIsDM", true);
        _service.HasAllKeyItems(PlayerObject, null!).Should().BeFalse("a DM never holds key items");
    }

    // ---- placeables that hand out key items

    [Test]
    public void ObtainKeyItem_GivesThePlaceablesKeyItemToThePlayerWhoUsedIt()
    {
        _engine.Engine.SetLocalInt(Placeable, "KEY_ITEM_ID", (int)_first);

        _service.ObtainKeyItem();

        _db.Get<Player>(PlayerId).KeyItems.Should().ContainKey(_first);
        _service.Hidden.Should().BeEmpty("the placeable names no visibility object");
    }

    [Test]
    public void ObtainKeyItem_HidesAPlaceableThatHasAVisibilityObject()
    {
        _engine.Engine.SetLocalInt(Placeable, "KEY_ITEM_ID", (int)_first);
        _engine.Engine.SetLocalString(Placeable, "VISIBILITY_OBJECT_ID", "pickup-1");

        _service.ObtainKeyItem();

        _service.Hidden.Should().Equal((PlayerObject, Placeable, VisibilityType.Hidden));
    }

    [Test]
    public void ObtainKeyItem_WhenThePlayerAlreadyHasIt_SaysSoAndGivesNothing()
    {
        _service.GiveKeyItem(PlayerObject, _first);
        _db.Saved.Clear();
        _locals.ClearCalls();
        _engine.Engine.SetLocalInt(Placeable, "KEY_ITEM_ID", (int)_first);

        _service.ObtainKeyItem();

        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().Equal("You already have this key item.");
        _service.Hidden.Should().BeEmpty();
    }

    [Test]
    public void ObtainKeyItem_WithoutAKeyItemOnThePlaceable_DoesNothing()
    {
        _service.ObtainKeyItem();

        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void ObtainKeyItem_IgnoresDMsAndCreaturesThatAreNotPlayers()
    {
        _engine.Engine.SetLocalInt(Placeable, "KEY_ITEM_ID", (int)_first);
        _engine.Engine.SetReturn("GetIsPC", false);

        _service.ObtainKeyItem();

        _db.Saved.Should().BeEmpty();
    }

    /// <summary>The key items with the visibility call recorded instead of reaching the static visibility service.</summary>
    private sealed class Visibility : KeyItemService
    {
        public Visibility(InMemoryDBService db)
            : base(db)
        {
        }

        public List<(uint Player, uint Placeable, VisibilityType Type)> Hidden { get; } = new();

        protected override void AdjustVisibility(uint player, uint placeable, VisibilityType type)
        {
            Hidden.Add((player, placeable, type));
        }
    }
}
