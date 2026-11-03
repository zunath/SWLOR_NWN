using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// Unit tests of the wardrobe instance: the engine is replaced by a recording fake (the worn clothes and helmet are
/// scripted objects), the object plugin by a fake that serializes them to text, the database by an in-memory one, and
/// the two calls that write native item data by an override that records them. No server is needed.
/// </summary>
[TestFixture]
public class WardrobeServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Creature = 1001;
    private const uint Clothes = 2001;
    private const uint Helmet = 2002;
    private const uint ClothesTemplate = 3001;
    private const uint HelmetTemplate = 3002;
    private const string PlayerId = PlayerEngine.PlayerId;
    private const string ClothesBlob = "clothes-blob";
    private const string HelmetBlob = "helmet-blob";

    private PlayerEngine _engine = null!;
    private FakeObjectPlugin _objectPlugin = null!;
    private IDisposable _objectPluginScope = null!;
    private InMemoryDBService _db = null!;
    private TestWardrobeService _service = null!;
    private uint _wornClothes;
    private uint _wornHead;
    private BaseItem _headItemType;
    private AppearanceType _creatureAppearance;
    private int _helmetModel;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _objectPlugin = new FakeObjectPlugin();
        _objectPluginScope = ObjectPluginRuntime.Override(_objectPlugin);
        _db = new InMemoryDBService();
        _service = new TestWardrobeService(_db);

        _wornClothes = Clothes;
        _wornHead = Helmet;
        _headItemType = BaseItem.Helmet;
        _creatureAppearance = AppearanceType.Human;
        _helmetModel = ArmorAppearanceCatalog.Definitions[AppearanceType.Human].Helmet[0];

        var engine = _engine.Engine;
        engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        engine.SetReturnHandler("GetItemInSlot", arguments => (InventorySlot)arguments[0]! switch
        {
            InventorySlot.Chest => _wornClothes,
            InventorySlot.Head => _wornHead,
            _ => ObjectInvalid
        });
        engine.SetReturnHandler("GetBaseItemType", arguments => (uint)arguments[0]! == Helmet ? _headItemType : BaseItem.Armor);
        engine.SetReturnHandler("GetItemAppearance", arguments =>
            (ItemAppearanceType)arguments[1]! == ItemAppearanceType.ArmorModel ? 100 + (int)arguments[2]! : _helmetModel);
        engine.SetReturnHandler("GetAppearanceType", _ => _creatureAppearance);
        _objectPlugin.SetReturnHandler("Serialize", arguments => (uint)arguments[0]! == Clothes ? ClothesBlob : HelmetBlob);
        _objectPlugin.SetReturnHandler("Deserialize", arguments => (string)arguments[0]! switch
        {
            ClothesBlob => ClothesTemplate,
            HelmetBlob => HelmetTemplate,
            _ => ObjectInvalid
        });
    }

    [TearDown]
    public void TearDown()
    {
        _objectPluginScope.Dispose();
        _engine.Dispose();
    }

    private static PlayerOutfit Outfit(string name = "Outfit", string? data = ClothesBlob, string? helmetData = null, int helmetId = 0) =>
        new() { PlayerId = PlayerId, Name = name, Data = data!, HelmetData = helmetData!, HelmetId = helmetId };

    private static string QueryOf(InMemoryDBService db, int index = 0) =>
        ((DBQuery<PlayerOutfit>)db.Queries[index]).BuildQuery().QueryString;

    private IReadOnlyList<uint> Destroyed() =>
        _engine.Engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).ToList();

    // ---- slot limit

    [Test]
    public void GetSlotLimit_is_the_players_own_limit()
    {
        _service.GetSlotLimit(new Player { OutfitSlotLimit = 35 }).Should().Be(35);
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void GetSlotLimit_falls_back_to_the_default_for_a_limit_that_is_not_positive(int stored)
    {
        _service.GetSlotLimit(new Player { OutfitSlotLimit = stored }).Should().Be(Player.DefaultOutfitSlotLimit);
    }

    [Test]
    public void GetSlotLimit_falls_back_to_the_default_without_a_player_record()
    {
        _service.GetSlotLimit(null!).Should().Be(Player.DefaultOutfitSlotLimit).And.Be(25);
    }

    [Test]
    public void The_constants_are_the_ones_the_facade_repeats()
    {
        Wardrobe.MaxNameLength.Should().Be(WardrobeService.MaxNameLength).And.Be(32);
        Wardrobe.SlotsPerExpansion.Should().Be(WardrobeService.SlotsPerExpansion).And.Be(5);
        Wardrobe.MaxSlotLimit.Should().Be(WardrobeService.MaxSlotLimit).And.Be(50);
        Wardrobe.ExpansionHint.Should().Be("Vessa Thrane on the Nar Shaddaa Promenade can expand your wardrobe.");
    }

    // ---- listing

    [Test]
    public void SortForDisplay_keeps_one_order_whatever_the_search_returns()
    {
        var names = new[] { "Outfit #10", "outfit #2", "Formal", "Outfit #1", "Battle Dress", "Outfit #2" };
        var outfits = names.Select(name => new PlayerOutfit { Name = name }).ToList();

        var forward = _service.SortForDisplay(outfits).Select(outfit => outfit.Id).ToList();
        var reversed = _service.SortForDisplay(Enumerable.Reverse(outfits)).Select(outfit => outfit.Id).ToList();

        reversed.Should().Equal(forward, "the same outfits always list in the same order");
        var sorted = _service.SortForDisplay(outfits).Select(outfit => outfit.Name).ToList();
        sorted.Take(3).Should().Equal("Battle Dress", "Formal", "Outfit #1");
        sorted.Skip(3).Take(2).Should().BeEquivalentTo(new[] { "outfit #2", "Outfit #2" }, "case is ignored");
        sorted.Last().Should().Be("Outfit #10", "numbers compare by value, so #10 follows #2");
    }

    [Test]
    public void SortForDisplay_sorts_outfits_without_names()
    {
        var outfits = new[] { new PlayerOutfit { Name = null! }, new PlayerOutfit { Name = "Outfit #1" } };

        _service.SortForDisplay(outfits).Select(outfit => outfit.Name).Should().Equal(null, "Outfit #1");
    }

    [Test]
    public void SortForDisplay_breaks_a_tie_between_equal_names_by_id()
    {
        var second = new PlayerOutfit { Id = "b", Name = "Same" };
        var first = new PlayerOutfit { Id = "a", Name = "same" };

        _service.SortForDisplay(new[] { second, first }).Should().Equal(first, second);
    }

    [Test]
    public void Each_instance_sorts_the_same_way()
    {
        var outfits = new[] { new PlayerOutfit { Name = "Outfit #10" }, new PlayerOutfit { Name = "Outfit #2" } };

        new WardrobeService(_db).SortForDisplay(outfits).Select(outfit => outfit.Name)
            .Should().Equal("Outfit #2", "Outfit #10");
    }

    [Test]
    public void GetOutfits_lists_the_players_outfits_in_display_order()
    {
        _db.With(Outfit("Outfit #10")).With(Outfit("Formal")).With(Outfit("Outfit #2"));

        var outfits = _service.GetOutfits(PlayerId);

        outfits.Select(outfit => outfit.Name).Should().Equal("Formal", "Outfit #2", "Outfit #10");
        _db.Queries.Should().ContainSingle();
        QueryOf(_db).Should().Contain("@PlayerId:player\\-uuid\\-1");
    }

    [Test]
    public void GetOutfits_is_empty_for_a_player_with_no_outfits()
    {
        _service.GetOutfits(PlayerId).Should().BeEmpty();
    }

    [Test]
    public void CountOutfits_counts_the_players_outfits()
    {
        _db.With(Outfit("One")).With(Outfit("Two"));

        _service.CountOutfits(PlayerId).Should().Be(2);

        QueryOf(_db).Should().Contain("@PlayerId:player\\-uuid\\-1");
    }

    // ---- expanding the wardrobe

    [Test]
    public void ExpandSlotLimit_does_nothing_for_a_player_with_no_record()
    {
        var limit = _service.ExpandSlotLimit(Creature);

        limit.Should().Be(Player.DefaultOutfitSlotLimit);
        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty("nothing changed, so the player is told nothing");
    }

    [Test]
    public void ExpandSlotLimit_adds_five_slots_saves_the_record_and_tells_the_player()
    {
        var player = new Player { Id = PlayerId, OutfitSlotLimit = 25 };
        _db.With(player);

        var limit = _service.ExpandSlotLimit(Creature);

        limit.Should().Be(30);
        player.OutfitSlotLimit.Should().Be(30);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(player);
        _engine.Messages().Should().Equal("Your wardrobe now holds 30 outfits.");
    }

    [Test]
    public void ExpandSlotLimit_counts_from_the_default_for_a_record_with_no_limit()
    {
        _db.With(new Player { Id = PlayerId, OutfitSlotLimit = 0 });

        _service.ExpandSlotLimit(Creature).Should().Be(Player.DefaultOutfitSlotLimit + WardrobeService.SlotsPerExpansion);
    }

    [Test]
    public void ExpandSlotLimit_stops_at_the_maximum()
    {
        var player = new Player { Id = PlayerId, OutfitSlotLimit = 48 };
        _db.With(player);

        _service.ExpandSlotLimit(Creature).Should().Be(WardrobeService.MaxSlotLimit);

        player.OutfitSlotLimit.Should().Be(50, "five more slots would pass the maximum, so it is cut to it");
    }

    [Test]
    public void ExpandSlotLimit_at_the_maximum_saves_nothing_but_still_tells_the_player()
    {
        _db.With(new Player { Id = PlayerId, OutfitSlotLimit = 50 });

        _service.ExpandSlotLimit(Creature).Should().Be(50);

        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().Equal("Your wardrobe now holds 50 outfits.");
    }

    // ---- names and saved looks

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("   ", "")]
    [TestCase("  Formal  ", "Formal")]
    public void ClampName_trims_the_name_and_turns_no_name_into_an_empty_one(string? name, string expected)
    {
        _service.ClampName(name!).Should().Be(expected);
    }

    [Test]
    public void ClampName_cuts_a_name_to_thirty_two_characters()
    {
        var exactly = new string('x', 32);

        _service.ClampName(exactly).Should().Be(exactly);
        _service.ClampName(exactly + "yz").Should().Be(exactly);
        _service.ClampName("  " + exactly + "yz  ").Should().Be(exactly, "the name is trimmed before it is cut");
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("blob", true)]
    public void HasClothes_is_true_when_clothes_are_saved(string? data, bool expected)
    {
        _service.HasClothes(Outfit(data: data)).Should().Be(expected);
    }

    [Test]
    public void HasClothes_and_HasHelmet_are_false_for_no_outfit()
    {
        _service.HasClothes(null!).Should().BeFalse();
        _service.HasHelmet(null!).Should().BeFalse();
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("  ", false)]
    [TestCase("blob", true)]
    public void HasHelmet_is_true_when_a_helmet_is_saved(string? helmetData, bool expected)
    {
        _service.HasHelmet(Outfit(helmetData: helmetData)).Should().Be(expected);
    }

    // ---- storing the worn look

    [Test]
    public void Store_refuses_when_no_clothes_are_worn()
    {
        _wornClothes = ObjectInvalid;
        var outfit = Outfit(data: null);

        var stored = _service.Store(Creature, outfit, out var message);

        stored.Should().BeFalse();
        message.Should().Be("You do not have any clothes equipped.");
        outfit.Data.Should().BeNull();
        _db.Saved.Should().BeEmpty();
        _objectPlugin.CallsTo("Serialize").Should().BeEmpty();
    }

    [Test]
    public void Store_saves_the_clothes_and_every_armor_part_with_the_helmet()
    {
        var outfit = Outfit(data: null);

        var stored = _service.Store(Creature, outfit, out var message);

        stored.Should().BeTrue();
        message.Should().Be("Saved your clothes and helmet.");
        outfit.Data.Should().Be(ClothesBlob);
        outfit.HelmetData.Should().Be(HelmetBlob);
        outfit.HelmetId.Should().Be(_helmetModel);
        outfit.NeckId.Should().Be(100 + (int)AppearanceArmor.Neck);
        outfit.TorsoId.Should().Be(100 + (int)AppearanceArmor.Torso);
        outfit.BeltId.Should().Be(100 + (int)AppearanceArmor.Belt);
        outfit.PelvisId.Should().Be(100 + (int)AppearanceArmor.Pelvis);
        outfit.RobeId.Should().Be(100 + (int)AppearanceArmor.Robe);
        outfit.LeftBicepId.Should().Be(100 + (int)AppearanceArmor.LeftBicep);
        outfit.LeftFootId.Should().Be(100 + (int)AppearanceArmor.LeftFoot);
        outfit.LeftForearmId.Should().Be(100 + (int)AppearanceArmor.LeftForearm);
        outfit.LeftHandId.Should().Be(100 + (int)AppearanceArmor.LeftHand);
        outfit.LeftShinId.Should().Be(100 + (int)AppearanceArmor.LeftShin);
        outfit.LeftShoulderId.Should().Be(100 + (int)AppearanceArmor.LeftShoulder);
        outfit.LeftThighId.Should().Be(100 + (int)AppearanceArmor.LeftThigh);
        outfit.RightBicepId.Should().Be(100 + (int)AppearanceArmor.RightBicep);
        outfit.RightFootId.Should().Be(100 + (int)AppearanceArmor.RightFoot);
        outfit.RightForearmId.Should().Be(100 + (int)AppearanceArmor.RightForearm);
        outfit.RightHandId.Should().Be(100 + (int)AppearanceArmor.RightHand);
        outfit.RightShinId.Should().Be(100 + (int)AppearanceArmor.RightShin);
        outfit.RightShoulderId.Should().Be(100 + (int)AppearanceArmor.RightShoulder);
        outfit.RightThighId.Should().Be(100 + (int)AppearanceArmor.RightThigh);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(outfit);
    }

    [Test]
    public void Store_clears_an_earlier_helmet_when_none_is_worn()
    {
        _wornHead = ObjectInvalid;
        var outfit = Outfit(helmetData: "old-helmet", helmetId: 9);

        var stored = _service.Store(Creature, outfit, out var message);

        stored.Should().BeTrue();
        message.Should().Be("Saved your clothes. No helmet was worn.");
        outfit.HelmetData.Should().BeEmpty();
        outfit.HelmetId.Should().Be(0);
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void Store_ignores_a_head_item_that_is_not_a_helmet()
    {
        _headItemType = BaseItem.Armor;
        var outfit = Outfit();

        _service.Store(Creature, outfit, out var message);

        message.Should().Be("Saved your clothes. No helmet was worn.");
        outfit.HelmetData.Should().BeEmpty();
    }

    // ---- wearing a saved look

    [Test]
    public void Wear_refuses_an_outfit_with_nothing_saved()
    {
        var worn = _service.Wear(Creature, Outfit(data: null), out var message);

        worn.Should().BeFalse();
        message.Should().Be("No outfit is saved to this slot.");
        _service.AppliedOutfits.Should().BeEmpty();
    }

    [Test]
    public void Wear_refuses_when_no_clothes_are_worn()
    {
        _wornClothes = ObjectInvalid;

        var worn = _service.Wear(Creature, Outfit(), out var message);

        worn.Should().BeFalse();
        message.Should().Be("You do not have any clothes equipped.");
        _objectPlugin.CallsTo("Deserialize").Should().BeEmpty();
    }

    [Test]
    public void Wear_refuses_an_outfit_that_cannot_be_rebuilt()
    {
        var worn = _service.Wear(Creature, Outfit(data: "garbage"), out var message);

        worn.Should().BeFalse();
        message.Should().Be("This outfit could not be loaded.");
        _service.AppliedOutfits.Should().BeEmpty();
        Destroyed().Should().BeEmpty("there is no template to destroy");
    }

    [Test]
    public void Wear_gives_the_worn_clothes_the_outfits_look_and_destroys_the_template()
    {
        var worn = _service.Wear(Creature, Outfit(), out var message);

        worn.Should().BeTrue();
        message.Should().BeEmpty();
        _service.AppliedOutfits.Should().Equal((Creature, Clothes, ClothesTemplate));
        _service.AppliedHelmets.Should().BeEmpty("the outfit saved no helmet");
        Destroyed().Should().Equal(ClothesTemplate);
        _objectPlugin.CallsTo("Deserialize").Select(call => call.Arguments[0]).Should().Equal(ClothesBlob);
    }

    [Test]
    public void Wear_keeps_the_worn_items_and_never_copies_equips_or_unequips()
    {
        _service.Wear(Creature, Outfit(helmetData: HelmetBlob, helmetId: _helmetModel), out _);

        foreach (var member in new[] { "CopyItemAndModify", "CopyItem", "ActionEquipItem", "ActionUnequipItem" })
            _engine.Engine.CallsTo(member).Should().BeEmpty($"loading an outfit must retain the equipped item ({member})");
        _service.AppliedOutfits.Should().Equal((Creature, Clothes, ClothesTemplate));
        _service.AppliedHelmets.Should().Equal((Creature, Helmet, HelmetTemplate));
    }

    [Test]
    public void Wear_tells_the_player_to_equip_a_helmet_when_the_outfit_has_one_and_none_is_worn()
    {
        _wornHead = ObjectInvalid;

        var worn = _service.Wear(Creature, Outfit(helmetData: HelmetBlob, helmetId: _helmetModel), out var message);

        worn.Should().BeTrue("the clothes were still changed");
        message.Should().Be("Equip a helmet to wear this outfit's helmet look.");
        _service.AppliedOutfits.Should().ContainSingle();
        _service.AppliedHelmets.Should().BeEmpty();
    }

    [Test]
    public void Wear_leaves_the_helmet_alone_when_the_outfits_helmet_model_is_not_offered()
    {
        var worn = _service.Wear(Creature, Outfit(helmetData: HelmetBlob, helmetId: 99999), out var message);

        worn.Should().BeTrue();
        message.Should().Be("Your helmet's look cannot be changed to this outfit's helmet.");
        _service.AppliedHelmets.Should().BeEmpty();
    }

    [Test]
    public void Wear_leaves_the_helmet_alone_when_the_worn_helmets_model_is_not_offered()
    {
        _helmetModel = 99999;

        var worn = _service.Wear(Creature, Outfit(helmetData: HelmetBlob, helmetId: ArmorAppearanceCatalog.Definitions[AppearanceType.Human].Helmet[0]), out var message);

        worn.Should().BeTrue();
        message.Should().Be("Your helmet's look cannot be changed to this outfit's helmet.");
        _service.AppliedHelmets.Should().BeEmpty();
    }

    [Test]
    public void Wear_offers_no_helmet_looks_to_an_appearance_without_a_catalog_entry()
    {
        _creatureAppearance = AppearanceType.Invalid;

        var worn = _service.Wear(Creature, Outfit(helmetData: HelmetBlob, helmetId: _helmetModel), out var message);

        worn.Should().BeTrue();
        message.Should().Be("Your helmet's look cannot be changed to this outfit's helmet.");
        _service.AppliedHelmets.Should().BeEmpty();
    }

    [Test]
    public void Wear_destroys_the_helmet_template_too()
    {
        _service.Wear(Creature, Outfit(helmetData: HelmetBlob, helmetId: _helmetModel), out _);

        Destroyed().Should().Equal(ClothesTemplate, HelmetTemplate);
    }

    [Test]
    public void Wear_that_cannot_rebuild_the_helmet_still_reports_success_for_the_clothes()
    {
        var worn = _service.Wear(Creature, Outfit(helmetData: "garbage", helmetId: _helmetModel), out var message);

        worn.Should().BeTrue();
        message.Should().BeEmpty();
        _service.AppliedHelmets.Should().BeEmpty();
    }

    [Test]
    public void Wear_destroys_the_template_even_when_the_appearance_change_fails()
    {
        _service.FailWith = new InvalidOperationException("The native write failed.");

        var wear = () => _service.Wear(Creature, Outfit(), out _);

        wear.Should().Throw<InvalidOperationException>();
        Destroyed().Should().Equal(ClothesTemplate);
    }

    // ---- capturing the worn look

    [Test]
    public void CaptureWornLook_is_nothing_without_clothes()
    {
        _wornClothes = ObjectInvalid;

        _service.CaptureWornLook(Creature).Should().BeNull();
    }

    [Test]
    public void CaptureWornLook_saves_the_clothes_and_the_helmet_without_storing_them()
    {
        var look = _service.CaptureWornLook(Creature);

        look.Should().NotBeNull();
        look!.Data.Should().Be(ClothesBlob);
        look.HelmetData.Should().Be(HelmetBlob);
        look.HelmetId.Should().Be(_helmetModel);
        _db.Saved.Should().BeEmpty("a captured look is only for an undo");
    }

    [Test]
    public void CaptureWornLook_without_a_helmet_has_no_helmet_data()
    {
        _wornHead = ObjectInvalid;

        var look = _service.CaptureWornLook(Creature);

        look!.Data.Should().Be(ClothesBlob);
        look.HelmetData.Should().BeEmpty();
        look.HelmetId.Should().Be(0);
    }

    // ---- the native calls

    [Test]
    public void The_appearance_writers_default_to_the_equipped_item_appearance_service()
    {
        // The defaults are the original EquippedItemAppearance calls. With an invalid creature both stop at their
        // first check, before any native item is touched, so the engine's validity check proves the call was made.
        _engine.Engine.SetReturnHandler("GetIsObjectValid", _ => false);

        _service.RunDefaultApplyOutfit(Creature, Clothes, ClothesTemplate);
        _service.RunDefaultApplyHelmet(Creature, Helmet, HelmetTemplate);

        _engine.Engine.CallsTo("GetIsObjectValid").Select(call => call.Arguments[0]).Should().Equal(Creature, Creature);
    }

    /// <summary>Records the appearance writes instead of making them, which need native item data.</summary>
    private sealed class TestWardrobeService : WardrobeService
    {
        public TestWardrobeService(InMemoryDBService db)
            : base(db)
        {
        }

        public List<(uint Creature, uint Item, uint Template)> AppliedOutfits { get; } = new();

        public List<(uint Creature, uint Item, uint Template)> AppliedHelmets { get; } = new();

        public Exception? FailWith { get; set; }

        public void RunDefaultApplyOutfit(uint creature, uint clothes, uint template) =>
            base.ApplyOutfit(creature, clothes, template);

        public void RunDefaultApplyHelmet(uint creature, uint helmet, uint template) =>
            base.ApplyHelmet(creature, helmet, template);

        protected override void ApplyOutfit(uint creature, uint clothes, uint template)
        {
            if (FailWith != null)
                throw FailWith;
            AppliedOutfits.Add((creature, clothes, template));
        }

        protected override void ApplyHelmet(uint creature, uint helmet, uint template)
        {
            AppliedHelmets.Add((creature, helmet, template));
        }
    }
}
