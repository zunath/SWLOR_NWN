using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The Twin Pistol equipment instance: building and tearing down the generated off-hand pistol, the cached pairs and
/// the re-entrancy guards, the appearance mirror, and the saved-spare cleanup. The engine and the item and creature
/// plugins are recording fakes with a small item world, the item service is scripted, and the unconverted droid, skill,
/// equipment storage and appearance calls are answered by a subclass.
/// </summary>
[TestFixture]
public class TwinPistolEquipmentServiceTests
{
    private const uint Owner = 100;
    private const uint Main = 200;
    private const uint Storage = 300;
    private const uint Spare = 400;
    private const uint Bag = 500;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private FakeItemPlugin _itemPlugin = null!;
    private FakeCreaturePlugin _creaturePlugin = null!;
    private ItemWorld _world = null!;
    private LocalVariables _locals = null!;
    private ScriptedProxy _items = null!;
    private TestEquipment _equipment = null!;
    private string _useError = string.Empty;
    private bool _storageExists;
    private bool _moveSucceeds;
    private bool _equipSucceeds;
    private int _mainAppearance;
    private int _spareAppearance;

    [SetUp]
    public void SetUp()
    {
        _useError = string.Empty;
        _storageExists = true;
        _moveSucceeds = true;
        _equipSucceeds = true;
        _mainAppearance = 5;
        _spareAppearance = 5;
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _world = new ItemWorld(_engine);
        _world.Add(Owner, baseItem: BaseItem.Invalid, name: "Owner");
        _world.Add(Main, baseItem: BaseItem.Pistol, name: "Blaster");
        _world.Equip(Owner, InventorySlot.RightHand, Main);
        _world.Add(Storage);
        _engine.SetReturnHandler("GetObjectByTag", args => _storageExists && (string)args[0]! == "TEMP_ITEM_STORAGE" ? Storage : ItemWorld.ObjectInvalid);
        _engine.SetReturnHandler("CreateItemOnObject", args =>
        {
            if ((string)args[0]! != "twp_companion") return ItemWorld.ObjectInvalid;
            _world.Add(Spare, baseItem: BaseItem.CompanionPistol, resref: "twp_companion");
            _world.SetPossessor(Spare, Storage);
            return Spare;
        });
        _engine.SetReturnHandler("GetItemAppearance", args => (uint)args[0]! == Spare ? _spareAppearance : _mainAppearance);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _itemPlugin = new FakeItemPlugin();
        _itemPlugin.SetReturnHandler("MoveTo", args =>
        {
            if (!_moveSucceeds) return false;
            _world.SetPossessor((uint)args[0]!, (uint)args[1]! == Owner ? Owner : Owner);
            return true;
        });
        _scopes.Add(ItemPluginRuntime.Override(_itemPlugin));

        _creaturePlugin = new FakeCreaturePlugin();
        _creaturePlugin.SetReturnHandler("RunEquip", args =>
        {
            if (!_equipSucceeds) return false;
            _world.Equip((uint)args[0]!, (InventorySlot)args[2]!, (uint)args[1]!);
            return true;
        });
        _scopes.Add(CreaturePluginRuntime.Override(_creaturePlugin));

        var item = ScriptedProxy.Create<IItemService>(out _items);
        _items.On("GetCreatureItemUseError", _ => _useError);
        _equipment = new TestEquipment(item, _world);
        MarkMainAsPair();
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--) _scopes[index].Dispose();
        _scopes.Clear();
    }

    private void MarkMainAsPair() => _engine.SetReturnHandler("GetLocalInt", args =>
        (uint)args[0]! == Main && (string)args[1]! == TwinPistolEquipment.PairVariable ? 1 : _locals.Int((uint)args[0]!, (string)args[1]!));

    [Test]
    public void EnsurePairBuildsAndEquipsTheGeneratedOffHandPistol()
    {
        _equipment.EnsurePair(Owner);

        _equipment.IsActivePair(Owner).Should().BeTrue();
        _equipment.GetCompanionWeapon(Owner).Should().Be(Spare);
        _equipment.IsGeneratedCompanion(Spare).Should().BeTrue();
        _equipment.IsLinkedPair(Owner, Main, Spare).Should().BeTrue();
        _equipment.IsChanging(Owner).Should().BeFalse();
        _equipment.ResolveAppearanceOwner(Owner, Spare).Should().Be(Main);
        _equipment.ResolveAppearanceOwner(Owner, Main).Should().Be(Main);
        _itemPlugin.CountOf("SetWeight").Should().Be(1);
        _creaturePlugin.CountOf("RunEquip").Should().Be(1);
    }

    [Test]
    public void EnsurePairMirrorsTheSupportedPropertiesOntoTheSpare()
    {
        _world.AddProperty(Main, ItemPropertyType.DMG);
        _world.AddProperty(Main, ItemPropertyType.EnhancementLevel);

        _equipment.EnsurePair(Owner);

        _engine.CountOf("AddItemProperty").Should().Be(1, "only the mirrored property types are copied");
    }

    [Test]
    public void EnsurePairAgainRefreshesTheCacheWithoutBuildingASecondGun()
    {
        _equipment.EnsurePair(Owner);
        _equipment.EnsurePair(Owner);

        _engine.CountOf("CreateItemOnObject").Should().Be(1);
    }

    [Test]
    public void EnsurePairIgnoresACreatureWithoutAPairItem()
    {
        _engine.SetReturnHandler("GetLocalInt", _ => 0);

        _equipment.EnsurePair(Owner);
        _equipment.EnsurePair(ItemWorld.ObjectInvalid);

        _engine.CountOf("CreateItemOnObject").Should().Be(0);
    }

    [Test]
    public void ANonEmptyOffHandSendsThePairBackThroughTheStorage()
    {
        _world.Add(600, baseItem: BaseItem.Pistol);
        _world.Equip(Owner, InventorySlot.LeftHand, 600);

        _equipment.EnsurePair(Owner);

        _equipment.UnequipRequests.Should().ContainSingle().Which.Should().Be((Owner, Main));
        _engine.CountOf("CreateItemOnObject").Should().Be(0);
    }

    [Test]
    public void ARefusedItemUseSendsThePairBack()
    {
        _useError = "You cannot use this.";

        _equipment.EnsurePair(Owner);

        _equipment.UnequipRequests.Should().HaveCount(1);
    }

    [Test]
    public void MissingTemporaryStorageTellsThePlayerAndKeepsNoPair()
    {
        _storageExists = false;

        _equipment.EnsurePair(Owner);

        _engine.CountOf("SendMessageToPC").Should().BeGreaterThan(0);
        _equipment.IsActivePair(Owner).Should().BeFalse();
        _equipment.UnequipRequests.Should().NotBeEmpty("a failed pair must not leave a lone pistol");
    }

    [Test]
    public void AFullInventoryOrAFailedEquipRemovesTheSpare()
    {
        _moveSucceeds = false;
        _equipment.EnsurePair(Owner);
        _engine.CallsTo("DestroyObject").Should().ContainSingle();
        _engine.CallsTo("SendMessageToPC").Should().NotBeEmpty();

        _moveSucceeds = true;
        _equipSucceeds = false;
        _world.Equip(Owner, InventorySlot.RightHand, Main);
        _locals.ClearCalls();
        _equipment.EnsurePair(Owner);
        _engine.CallsTo("DestroyObject").Should().NotBeEmpty();
    }

    [Test]
    public void AFailedSpareCreationIsReported()
    {
        _engine.SetReturnHandler("CreateItemOnObject", _ => ItemWorld.ObjectInvalid);

        _equipment.EnsurePair(Owner);

        _engine.CountOf("SendMessageToPC").Should().BeGreaterThan(0);
    }

    [Test]
    public void TheSpareFindsRoomInACarriedBagWhenTheCreatureIsFull()
    {
        _world.Add(Bag, baseItem: BaseItem.Invalid);
        _world.Carry(Bag);
        _world.Carry(Owner, Bag);
        _world.SetPossessor(Bag, Owner);
        var moves = new List<uint>();
        _itemPlugin.SetReturnHandler("MoveTo", args =>
        {
            moves.Add((uint)args[1]!);
            if ((uint)args[1]! == Owner) return false;
            _world.SetPossessor((uint)args[0]!, Owner);
            return true;
        });

        _equipment.EnsurePair(Owner);

        moves.Should().Equal(Owner, Bag);
        _equipment.IsActivePair(Owner).Should().BeTrue();
    }

    [Test]
    public void RemovePairReturnsTheRealPistolAndDestroysOnlyTheSpare()
    {
        _equipment.EnsurePair(Owner);

        _equipment.RemovePair(Owner).Should().BeTrue();

        _equipment.UnequipRequests.Should().ContainSingle();
        _equipment.IsChangingDuringUnequip.Should().BeTrue("the nested unequip must not re-enter the pair logic");
        _engine.CallsTo("DestroyObject").Single().Arguments[0].Should().Be(Spare);
        _locals.Int(Spare, "TWIN_PISTOL_REMOVING").Should().Be(1);
    }

    [Test]
    public void RemovePairKeepsThePairWhenNoStorageCanHoldThePistol()
    {
        _equipment.EnsurePair(Owner);
        _locals.ClearCalls();
        _equipment.UnequipSucceeds = false;

        _equipment.RemovePair(Owner).Should().BeFalse();

        _engine.CountOf("SendMessageToPC").Should().Be(1);
        _engine.CountOf("DestroyObject").Should().Be(0);
    }

    [Test]
    public void RemovePairForAnotherItemOrAnInvalidCreatureIsIgnored()
    {
        _equipment.EnsurePair(Owner);
        _world.Add(9999);

        _equipment.RemovePair(Owner, 9999).Should().BeTrue();
        _equipment.RemovePair(ItemWorld.ObjectInvalid).Should().BeTrue();

        _equipment.UnequipRequests.Should().BeEmpty();
        _equipment.RemovePair(Owner, Spare).Should().BeTrue();
        _equipment.UnequipRequests.Should().HaveCount(1);
    }

    [Test]
    public void RemovePairWithoutACachedPairRebuildsItFromTheHands()
    {
        _world.Add(Spare, baseItem: BaseItem.CompanionPistol);
        _world.Equip(Owner, InventorySlot.LeftHand, Spare);
        _engine.SetReturnHandler("GetLocalInt", args => (uint)args[0]! == Spare && (string)args[1]! == "TWIN_PISTOL_SPARE" ? 1
            : (uint)args[0]! == Main && (string)args[1]! == TwinPistolEquipment.PairVariable ? 1 : 0);
        _engine.SetReturnHandler("GetLocalObject", args => (uint)args[0]! == Spare ? Main : ItemWorld.ObjectInvalid);

        _equipment.RemovePair(Owner).Should().BeTrue();

        _equipment.UnequipRequests.Should().ContainSingle();
    }

    [Test]
    public void RemovePairWithNeitherHalfDoesNothing()
    {
        _world.Equip(Owner, InventorySlot.RightHand, ItemWorld.ObjectInvalid);

        _equipment.RemovePair(Owner).Should().BeTrue();

        _equipment.UnequipRequests.Should().BeEmpty();
    }

    [Test]
    public void ReleaseCompanionsDestroysSparesInTheHandAndInBags()
    {
        _equipment.EnsurePair(Owner);
        const uint strayInBag = 700;
        _world.Add(strayInBag, baseItem: BaseItem.CompanionPistol);
        _world.Add(Bag, baseItem: BaseItem.Invalid);
        _world.Carry(Bag, strayInBag);
        _world.Carry(Owner, Bag);
        _engine.SetReturnHandler("GetLocalInt", args => (string)args[1]! == "TWIN_PISTOL_SPARE" ? 1
            : (uint)args[0]! == Main && (string)args[1]! == TwinPistolEquipment.PairVariable ? 1 : 0);
        _locals.ClearCalls();

        _equipment.ReleaseCompanions(Owner);

        _engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).Should().BeEquivalentTo(new[] { Spare, strayInBag });
        _equipment.ReleaseCompanions(ItemWorld.ObjectInvalid);
    }

    [Test]
    public void RestorePairReleasesAndRebuildsAfterADelay()
    {
        _equipment.EnsurePair(Owner);
        _locals.ClearCalls();

        _equipment.RestorePair(Owner);

        _engine.CountOf("DelayCommand").Should().Be(1);
        _equipment.RestorePair(ItemWorld.ObjectInvalid);
    }

    [Test]
    public void ADestroyedOwnerDropsItsCachedPairAndItsSpare()
    {
        _equipment.EnsurePair(Owner);
        _engine.SetReturnHandler("GetLocalObject", args => (uint)args[0]! == Spare && (string)args[1]! == "TWIN_PISTOL_OWNER" ? Owner
            : (uint)args[0]! == Main ? Spare : ItemWorld.ObjectInvalid);
        _locals.ClearCalls();

        _equipment.ReleaseDestroyedOwner(Owner);

        _engine.CallsTo("DestroyObject").Should().ContainSingle();
        _equipment.ReleaseDestroyedOwner(Owner);
        _engine.CountOf("DestroyObject").Should().Be(1, "the cache entry is gone");
    }

    [Test]
    public void TheAppearanceMirrorCopiesDifferingPartsOnceAndNotifiesObservers()
    {
        _equipment.EnsurePair(Owner);
        _spareAppearance = 9;
        _equipment.Refreshed.Clear();

        _equipment.RefreshAppearance(Owner);

        _equipment.Refreshed.Should().Equal((Owner, Spare));
        _itemPlugin.CountOf("SetItemAppearance").Should().Be(6);
    }

    [Test]
    public void AnAlreadyMatchingAppearanceTriggersNoRefresh()
    {
        _equipment.EnsurePair(Owner);
        _equipment.Refreshed.Clear();

        _equipment.RefreshAppearance(Owner);
        _equipment.RefreshAppearance(ItemWorld.ObjectInvalid);

        _equipment.Refreshed.Should().BeEmpty();
    }

    [Test]
    public void AnUnsupportedPropertyOnTheSpareBreaksThePair()
    {
        _equipment.EnsurePair(Owner);
        _world.AddProperty(Spare, ItemPropertyType.ACBonus);

        _equipment.GetCompanionPropertyError(Spare).Should().NotBeEmpty();
        _equipment.IsActivePair(Owner).Should().BeFalse();
        _equipment.GetCompanionWeapon(Owner).Should().Be(ItemWorld.ObjectInvalid);
    }

    [Test]
    public void TheHandsOfAProspectiveEquipAreTheCurrentHandsWithTheSlotReplaced()
    {
        _equipment.GetProspectiveHands(Owner, 888, InventorySlot.RightHand).Should().Be((888u, ItemWorld.ObjectInvalid));
        _equipment.GetProspectiveHands(Owner, 888, InventorySlot.LeftHand).Should().Be((Main, 888u));
        _equipment.GetProspectiveHands(Owner, 888, InventorySlot.Head).Should().Be((Main, ItemWorld.ObjectInvalid));
    }

    [Test]
    public void DestroyingAGeneratedSpareMarksItAndIgnoresOtherItems()
    {
        _equipment.EnsurePair(Owner);

        _equipment.DestroySpare(Main);
        _equipment.DestroySpare(Spare);

        _engine.CallsTo("DestroyObject").Should().ContainSingle();
    }

    [Test]
    public void ThePureRulesAnswerFromTheirArguments()
    {
        _equipment.IsPairItem(BaseItem.Pistol, true).Should().BeTrue();
        _equipment.IsPairItem(BaseItem.Sling, false).Should().BeFalse();
        _equipment.IsPair(BaseItem.Pistol, BaseItem.CompanionPistol).Should().BeTrue();
        _equipment.GetPairCompatibilityError(BaseItem.LegacyPistol, BaseItem.CompanionPistol).Should().NotBeEmpty();
        _equipment.GetPairCompatibilityError(BaseItem.Pistol, BaseItem.CompanionPistol).Should().BeEmpty();
        _equipment.ResolveRequirementSkill(BaseItem.Pistol, SkillType.Pistol, true).Should().Be(SkillType.TwinPistol);
        _equipment.ResolveRequirementSkill(BaseItem.Pistol, SkillType.Pistol, false).Should().Be(SkillType.Pistol);
        _equipment.ResolveSkill(BaseItem.CompanionPistol, null, null).Should().Be(SkillType.Invalid);
        _equipment.ResolveSkill(BaseItem.Rifle, null, null).Should().Be(SkillType.Rifle);
        _equipment.ResolveSkill(BaseItem.Pistol, BaseItem.Pistol, BaseItem.CompanionPistol).Should().Be(SkillType.TwinPistol);
        _equipment.ResolveSkill(BaseItem.Pistol, null, null, SkillType.Force).Should().Be(SkillType.Force);
        _equipment.IsMirroredProperty(ItemPropertyType.DMG).Should().BeTrue();
        _equipment.IsMirroredProperty(ItemPropertyType.ACBonus).Should().BeFalse();
        _equipment.IsSupportedCompanionProperty(ItemPropertyType.Blueprint).Should().BeTrue();
        _equipment.IsSupportedCompanionProperty(ItemPropertyType.ACBonus).Should().BeFalse();
        _equipment.IsPairItem(ItemWorld.ObjectInvalid).Should().BeFalse();
        _equipment.IsDestroyingCompanion(Main).Should().BeFalse();
    }

    [Test]
    public void ADroidsOwnerGetsTheExplanation()
    {
        _storageExists = false;
        _equipment.DroidOwner = 999;
        _engine.SetReturnHandler("GetMaster", _ => (uint)999);

        _equipment.EnsurePair(Owner);

        _engine.CallsTo("SendMessageToPC").Should().NotBeEmpty();
        _engine.CallsTo("SendMessageToPC").First().Arguments[0].Should().Be(999u);
    }

    private sealed class TestEquipment : TwinPistolEquipmentService
    {
        private readonly ItemWorld _itemWorld;
        public readonly List<(uint Creature, uint Item)> UnequipRequests = new();
        public readonly List<(uint Creature, uint Item)> Refreshed = new();
        public bool UnequipSucceeds = true;
        public bool IsChangingDuringUnequip;
        public uint DroidOwner;

        public TestEquipment(IItemService item, ItemWorld world)
            : base(item, FacadeForwarder.LazyFor<ISkillService>())
        {
            _itemWorld = world;
        }

        protected override bool IsDroid(uint creature) => DroidOwner != 0;

        protected override bool TryUnequip(uint creature, uint item, InventorySlot slot, bool allowRecoveryBag)
        {
            IsChangingDuringUnequip = IsChanging(creature);
            UnequipRequests.Add((creature, item));
            if (UnequipSucceeds) _itemWorld.Equip(creature, slot, ItemWorld.ObjectInvalid);
            return UnequipSucceeds;
        }

        protected override void RefreshEquippedItemAppearance(uint creature, uint item) => Refreshed.Add((creature, item));

        protected override SkillType GetSkillTypeByBaseItem(BaseItem baseItem) => baseItem == BaseItem.Rifle ? SkillType.Rifle : SkillType.Invalid;
    }
}
