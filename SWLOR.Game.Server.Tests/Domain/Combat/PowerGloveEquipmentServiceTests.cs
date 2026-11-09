using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The Power Glove equipment instance: generating, linking and tearing down the cosmetic other hand, restoring a
/// saved companion, the appearance mirror, and the cleanup of cached and orphaned companions. The engine and the item
/// and creature plugins are recording fakes with a small item world, the item service is scripted, and the
/// unconverted droid, equipment storage, appearance and fit calls are answered by a subclass.
/// </summary>
[TestFixture]
public class PowerGloveEquipmentServiceTests
{
    private const uint Owner = 100;
    private const uint Main = 200;
    private const uint Storage = 300;
    private const uint Companion = 400;
    private const uint Bag = 500;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private FakeItemPlugin _itemPlugin = null!;
    private FakeCreaturePlugin _creaturePlugin = null!;
    private ItemWorld _world = null!;
    private LocalVariables _locals = null!;
    private ScriptedProxy _items = null!;
    private TestGlove _glove = null!;
    private string _useError = string.Empty;
    private bool _storageExists;
    private bool _moveSucceeds;
    private bool _equipSucceeds;
    private bool _creating;
    private int _mainAppearance;
    private int _companionAppearance;

    [SetUp]
    public void SetUp()
    {
        _useError = string.Empty;
        _storageExists = true;
        _moveSucceeds = true;
        _equipSucceeds = true;
        _creating = false;
        _mainAppearance = 5;
        _companionAppearance = 5;
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _world = new ItemWorld(_engine);
        _world.Add(Owner, name: "Owner");
        _world.Add(Main, baseItem: BaseItem.PowerGlove, name: "Glove");
        _world.Equip(Owner, InventorySlot.RightHand, Main);
        _world.Add(Storage);
        _engine.SetReturnHandler("GetObjectType", args => (uint)args[0]! == Owner ? ObjectType.Creature : ObjectType.Item);
        _engine.SetReturnHandler("GetObjectByTag", args => _storageExists && (string)args[0]! == "TEMP_ITEM_STORAGE" ? Storage : ItemWorld.ObjectInvalid);
        _engine.SetReturnHandler("CreateItemOnObject", args =>
        {
            if ((string)args[0]! != "pg_companion") return ItemWorld.ObjectInvalid;
            _creating = _glove.IsCreatingCompanion;
            _world.Add(Companion, baseItem: BaseItem.CompanionPowerGlove, resref: "pg_companion");
            _world.SetPossessor(Companion, Storage);
            return Companion;
        });
        _engine.SetReturnHandler("GetItemAppearance", args => (uint)args[0]! == Companion ? _companionAppearance : _mainAppearance);
        _engine.SetReturnHandler("GetHiddenWhenEquipped", args => (uint)args[0]! == Main ? 1 : 0);
        _engine.SetReturnHandler("GetIsPC", _ => true);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _itemPlugin = new FakeItemPlugin();
        _itemPlugin.SetReturnHandler("MoveTo", args =>
        {
            if (!_moveSucceeds) return false;
            _world.SetPossessor((uint)args[0]!, Owner);
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
        _glove = new TestGlove(item, _world);
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--) _scopes[index].Dispose();
        _scopes.Clear();
    }

    private void Link()
    {
        _world.Add(Companion, baseItem: BaseItem.CompanionPowerGlove);
        _world.Equip(Owner, InventorySlot.LeftHand, Companion);
        _engine.SetLocalObject(Main, PowerGloveEquipmentService.CompanionVariable, Companion);
        _engine.SetLocalObject(Companion, PowerGloveEquipmentService.MainWeaponVariable, Main);
        _engine.SetLocalObject(Companion, PowerGloveEquipmentService.OwnerVariable, Owner);
    }

    [Test]
    public void EnsurePairGeneratesLinksAndEquipsTheCosmeticHand()
    {
        _glove.EnsurePair(Owner);

        _glove.IsActivePair(Owner).Should().BeTrue();
        _glove.GetCompanion(Owner).Should().Be(Companion);
        _glove.IsEquippedCompanion(Companion).Should().BeTrue();
        _glove.IsCreatingCompanion.Should().BeFalse("the flag is only raised while the item is created");
        _creating.Should().BeTrue();
        _glove.ResolveAppearanceOwner(Owner, Companion).Should().Be(Main);
        _glove.ResolveAppearanceOwner(Owner, Main).Should().Be(Main);
        _itemPlugin.CountOf("SetWeight").Should().BeGreaterThan(0);
        _engine.CountOf("SetObjectVisualTransform").Should().BeGreaterThan(0, "the other hand mirrors the glove's fit");
        _engine.CountOf("SetHiddenWhenEquipped").Should().Be(1);
    }

    [Test]
    public void EnsurePairWithALinkedPairOnlyRefreshesIt()
    {
        _glove.EnsurePair(Owner);
        _locals.ClearCalls();

        _glove.EnsurePair(Owner);

        _engine.CountOf("CreateItemOnObject").Should().Be(0);
    }

    [Test]
    public void EnsurePairIgnoresInvalidCreaturesAndCreaturesWithoutAGlove()
    {
        _glove.EnsurePair(ItemWorld.ObjectInvalid);
        _world.Equip(Owner, InventorySlot.RightHand, ItemWorld.ObjectInvalid);
        _glove.EnsurePair(Owner);

        _engine.CountOf("CreateItemOnObject").Should().Be(0);
        _glove.RemoveRequests.Should().BeEmpty();
    }

    [Test]
    public void ALeftoverCosmeticHandWithoutAGloveIsRemoved()
    {
        _world.Equip(Owner, InventorySlot.RightHand, ItemWorld.ObjectInvalid);
        _world.Add(Companion, baseItem: BaseItem.CompanionPowerGlove);
        _world.Equip(Owner, InventorySlot.LeftHand, Companion);

        _glove.EnsurePair(Owner);

        _engine.CallsTo("DestroyObject").Should().ContainSingle();
    }

    [Test]
    public void ARefusedItemUseOrAForeignOffHandSendsTheGloveBack()
    {
        _useError = "no";
        _glove.EnsurePair(Owner);
        _glove.UnequipRequests.Should().ContainSingle();

        _useError = string.Empty;
        _world.Equip(Owner, InventorySlot.RightHand, Main);
        _glove.UnequipRequests.Clear();
        _world.Add(600, baseItem: BaseItem.Pistol);
        _world.Equip(Owner, InventorySlot.LeftHand, 600);
        _glove.EnsurePair(Owner);

        _glove.UnequipRequests.Should().ContainSingle();
    }

    [Test]
    public void ADetachedOldCosmeticIsDisposedBeforeTheReplacement()
    {
        _glove.EnsurePair(Owner);
        _world.Equip(Owner, InventorySlot.LeftHand, ItemWorld.ObjectInvalid);
        _engine.SetLocalObject(Main, PowerGloveEquipmentService.CompanionVariable, ItemWorld.ObjectInvalid);
        _locals.ClearCalls();

        _glove.EnsurePair(Owner);

        _engine.CountOf("CreateItemOnObject").Should().Be(1);
        _engine.CountOf("DestroyObject").Should().Be(1, "the cached old companion is destroyed");
    }

    [Test]
    public void FailuresWhileGeneratingTellThePlayerAndLeaveNoLoneGlove()
    {
        _storageExists = false;
        _glove.EnsurePair(Owner);
        _engine.CountOf("SendMessageToPC").Should().Be(1);
        _glove.UnequipRequests.Should().ContainSingle();

        _storageExists = true;
        _world.Equip(Owner, InventorySlot.RightHand, Main);
        _glove.UnequipRequests.Clear();
        _locals.ClearCalls();
        _engine.SetReturnHandler("CreateItemOnObject", _ => ItemWorld.ObjectInvalid);
        _glove.EnsurePair(Owner);
        _engine.CountOf("SendMessageToPC").Should().Be(1, "the cosmetic could not be created");

        _engine.SetReturnHandler("CreateItemOnObject", args =>
        {
            _world.Add(Companion, baseItem: BaseItem.CompanionPowerGlove);
            return Companion;
        });
        _world.Equip(Owner, InventorySlot.RightHand, Main);
        _moveSucceeds = false;
        _locals.ClearCalls();
        _glove.EnsurePair(Owner);
        _engine.CountOf("SendMessageToPC").Should().Be(1, "the other hand could not be stored");

        _moveSucceeds = true;
        _equipSucceeds = false;
        _world.Equip(Owner, InventorySlot.RightHand, Main);
        _locals.ClearCalls();
        _glove.EnsurePair(Owner);
        _engine.CountOf("SendMessageToPC").Should().Be(1, "the other hand could not be equipped");
    }

    [Test]
    public void TheCosmeticHandFindsRoomInACarriedBag()
    {
        _world.Add(Bag);
        _world.Carry(Bag);
        _world.Carry(Owner, Bag);
        var targets = new List<uint>();
        _itemPlugin.SetReturnHandler("MoveTo", args =>
        {
            targets.Add((uint)args[1]!);
            if ((uint)args[1]! == Owner) return false;
            _world.SetPossessor((uint)args[0]!, Owner);
            return true;
        });

        _glove.EnsurePair(Owner);

        targets.Should().Equal(Owner, Bag);
        _glove.IsActivePair(Owner).Should().BeTrue();
    }

    [Test]
    public void RemovePairReturnsTheGloveAndDestroysTheCosmeticHand()
    {
        _glove.EnsurePair(Owner);
        _locals.ClearCalls();

        _glove.RemovePair(Owner).Should().BeTrue();

        _glove.UnequipRequests.Should().ContainSingle().Which.Should().Be((Owner, Main, InventorySlot.RightHand));
        _glove.IsChangingDuringUnequip.Should().BeTrue();
        _engine.CallsTo("DestroyObject").Should().ContainSingle();
        _locals.Int(Companion, "POWER_GLOVE_REMOVING").Should().Be(1);
        _glove.DestroyCompanion(Companion);
        _engine.CountOf("DestroyObject").Should().Be(1, "a companion already being destroyed is left alone");
    }

    [Test]
    public void RemovePairFailsWhenNothingCanHoldTheGlove()
    {
        _glove.EnsurePair(Owner);
        _locals.ClearCalls();
        _glove.UnequipSucceeds = false;

        _glove.RemovePair(Owner).Should().BeFalse();

        _engine.CountOf("SendMessageToPC").Should().Be(1);
        _engine.CountOf("DestroyObject").Should().Be(0);
    }

    [Test]
    public void RemovePairIgnoresOtherItemsAndInvalidCreatures()
    {
        _glove.EnsurePair(Owner);
        _world.Add(9999);

        _glove.RemovePair(Owner, 9999).Should().BeFalse();
        _glove.RemovePair(ItemWorld.ObjectInvalid).Should().BeFalse();

        _glove.UnequipRequests.Should().BeEmpty();
    }

    [Test]
    public void RemovePairWithoutACacheRebuildsThePairFromTheHands()
    {
        Link();

        _glove.RemovePair(Owner).Should().BeTrue();

        _glove.UnequipRequests.Should().ContainSingle();
    }

    [Test]
    public void RemovePairFindsTheGloveFromTheCosmeticHandAlone()
    {
        Link();
        _world.Equip(Owner, InventorySlot.RightHand, ItemWorld.ObjectInvalid);
        _world.Add(Main, baseItem: BaseItem.PowerGlove);

        _glove.RemovePair(Owner).Should().BeTrue();

        _engine.CallsTo("DestroyObject").Should().ContainSingle();
    }

    [Test]
    public void RestorePairReleasesCompanionsAndRebuildsAfterADelay()
    {
        _glove.EnsurePair(Owner);
        _locals.ClearCalls();

        _glove.RestorePair(Owner);
        _glove.RestorePair(ItemWorld.ObjectInvalid);

        var delayed = (Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!;
        _world.Equip(Owner, InventorySlot.LeftHand, ItemWorld.ObjectInvalid);
        _world.Add(600, baseItem: BaseItem.Pistol);
        _world.Equip(Owner, InventorySlot.LeftHand, 600);
        delayed();

        _glove.UnequipRequests.Should().Contain(request => request.Slot == InventorySlot.LeftHand && request.Item == 600);
    }

    [Test]
    public void TheDelayedRestoreReportsAFullInventoryAndSkipsAMissingOwner()
    {
        _glove.EnsurePair(Owner);
        _locals.ClearCalls();
        _glove.RestorePair(Owner);
        var delayed = (Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!;
        _world.Add(600, baseItem: BaseItem.Pistol);
        _world.Equip(Owner, InventorySlot.LeftHand, 600);
        _glove.UnequipSucceeds = false;

        delayed();

        _engine.CountOf("SendMessageToPC").Should().Be(2, "the full inventory is reported, then again by the failed rebuild");
        _world.Invalidate(Owner);
        _locals.ClearCalls();
        delayed();
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void ReleaseCompanionsDestroysEveryCosmeticHandTheCreatureCarries()
    {
        _glove.EnsurePair(Owner);
        const uint stray = 700;
        _world.Add(stray, baseItem: BaseItem.CompanionPowerGlove);
        _world.Add(Bag);
        _world.Carry(Bag, stray);
        _world.Carry(Owner, Bag);
        _locals.ClearCalls();

        _glove.ReleaseCompanions(Owner);

        _engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).Should().BeEquivalentTo(new[] { Companion, stray });
        _glove.ReleaseCompanions(ItemWorld.ObjectInvalid);
    }

    [Test]
    public void ADestroyedOwnerReleasesItsStateAndOrphanedCompanion()
    {
        _glove.EnsurePair(Owner);
        _locals.ClearCalls();

        _glove.ReleaseDestroyedOwner(Owner);

        _engine.CallsTo("DestroyObject").Should().ContainSingle();
        _glove.ReleaseDestroyedOwner(Owner);
        _engine.CountOf("DestroyObject").Should().Be(1);
        _glove.RemoveOwnerState(Owner).Should().BeNull();
    }

    [Test]
    public void ASavedCosmeticHandIsRebound_OnlyWhileThePlayerIsLoading()
    {
        Link();
        _engine.SetLocalObject(Main, PowerGloveEquipmentService.CompanionVariable, ItemWorld.ObjectInvalid);
        _engine.SetLocalObject(Companion, PowerGloveEquipmentService.MainWeaponVariable, ItemWorld.ObjectInvalid);
        _world.SetPossessor(Companion, Owner);
        _engine.SetReturnHandler("GetArea", _ => ItemWorld.ObjectInvalid);

        _glove.TryRestoreSavedCompanion(Companion).Should().BeTrue();

        _glove.IsLinkedPair(Owner, Main, Companion).Should().BeTrue();
        _glove.TryRestoreSavedCompanion(Main).Should().BeFalse();
        _world.Add(55);
        _engine.SetReturnHandler("GetArea", _ => 55u);
        _glove.TryRestoreSavedCompanion(Companion).Should().BeFalse("a player already in an area is not loading");
        _world.SetPossessor(Companion, ItemWorld.ObjectInvalid);
        _glove.TryRestoreSavedCompanion(Companion).Should().BeFalse();
    }

    [Test]
    public void SanitizingStripsPropertiesAndGameplayFlags()
    {
        Link();
        _world.AddProperty(Companion, ItemPropertyType.DMG);

        _glove.SanitizeCompanion(Companion);
        _glove.SanitizeCompanion(Main);

        _engine.CountOf("RemoveItemProperty").Should().Be(1);
        _engine.CountOf("SetPlotFlag").Should().Be(1);
        _itemPlugin.CountOf("SetBaseGoldPieceValue").Should().Be(1);
    }

    [Test]
    public void ACompanionWithAPropertyIsNotAnActivePair()
    {
        Link();
        _world.AddProperty(Companion, ItemPropertyType.DMG);

        _glove.IsActivePair(Owner).Should().BeFalse();
        _glove.IsActivePair(ItemWorld.ObjectInvalid).Should().BeFalse();
        _glove.GetCompanion(Owner).Should().Be(ItemWorld.ObjectInvalid);
    }

    [Test]
    public void CollectCompanionsWalksNestedBagsOnce()
    {
        const uint inner = 800;
        const uint innerCompanion = 801;
        _world.Add(Bag);
        _world.Add(inner);
        _world.Add(innerCompanion, baseItem: BaseItem.CompanionPowerGlove);
        _world.Carry(inner, innerCompanion);
        _world.Carry(Bag, inner);
        var found = new HashSet<uint>();

        _glove.CollectCompanions(Bag, found, new HashSet<uint>());
        _glove.CollectCompanions(Bag, found, new HashSet<uint>());
        _glove.CollectCompanions(ItemWorld.ObjectInvalid, found, new HashSet<uint>());

        found.Should().BeEquivalentTo(new[] { innerCompanion });
    }

    [Test]
    public void ThePureRulesAnswerFromTheirArguments()
    {
        _glove.GetEquipmentStatCopies(BaseItem.PowerGlove).Should().Be(2);
        _glove.GetEquipmentStatCopies(BaseItem.Pistol).Should().Be(1);
        _glove.GetPairDescription(string.Empty).Should().Be(PowerGloveEquipmentService.PairDescriptionNote);
        _glove.GetPairDescription("Text.").Should().Be($"Text.\n{PowerGloveEquipmentService.PairDescriptionNote}");
        _glove.GetPairDescription(PowerGloveEquipmentService.PairDescriptionNote).Should().Be(PowerGloveEquipmentService.PairDescriptionNote);
        _glove.CanRestoreSavedPair(true, false, true, true, true).Should().BeTrue();
        _glove.CanRestoreSavedPair(true, true, true, true, true).Should().BeFalse();
        _glove.IsPairItem(ItemWorld.ObjectInvalid).Should().BeFalse();
        _glove.IsGeneratedCompanion(ItemWorld.ObjectInvalid).Should().BeFalse();
        _glove.IsDestroyingCompanion(Main).Should().BeFalse();
    }

    [Test]
    public void ADroidsControllerGetsTheExplanation()
    {
        _storageExists = false;
        _glove.DroidMaster = 999;
        _engine.SetReturnHandler("GetMaster", _ => 999u);

        _glove.EnsurePair(Owner);

        _engine.CallsTo("SendMessageToPC").Single().Arguments[0].Should().Be(999u);
    }

    private sealed class TestGlove : PowerGloveEquipmentService
    {
        private readonly ItemWorld _itemWorld;
        public readonly List<(uint Creature, uint Item, InventorySlot Slot)> UnequipRequests = new();
        public readonly List<uint> RemoveRequests = new();
        public bool UnequipSucceeds = true;
        public bool IsChangingDuringUnequip;
        public uint DroidMaster;

        public TestGlove(IItemService item, ItemWorld world)
            : base(item)
        {
            _itemWorld = world;
        }

        protected override bool IsDroid(uint creature) => DroidMaster != 0;

        protected override bool TryUnequip(uint creature, uint item, InventorySlot slot, bool allowRecoveryBag)
        {
            IsChangingDuringUnequip = IsChanging(creature);
            UnequipRequests.Add((creature, item, slot));
            if (UnequipSucceeds) _itemWorld.Equip(creature, slot, ItemWorld.ObjectInvalid);
            return UnequipSucceeds;
        }

        protected override void RefreshEquippedItemAppearance(uint creature, uint item)
        {
        }

        protected override float GetMirroredTransform(uint main, uint companion, ObjectVisualTransform transform,
            ObjectVisualTransformDataScopeType scope) => 1f;
    }
}
