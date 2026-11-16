using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>An item's custom colours across a model change: capture, queue and the delayed carry.</summary>
[TestFixture]
public class TintMapCarryServiceTests
{
    private const uint Creature = 10;
    private const uint Player = 11;
    private const uint Item = 12;
    private const uint Replacement = 13;

    private TintMapWorld _world = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;
    private TintMapStateService _state = null!;
    private ScriptedProxy _apply = null!;
    private ScriptedProxy _droid = null!;
    private ScriptedProxy _gui = null!;
    private TintMapCarryService _service = null!;
    private bool _isDroid;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _container = new FacadeContainer();
        _installed = _container.Install();
        _world.Add(Creature, ObjectType.Creature);
        _world.Add(Player, ObjectType.Creature, player: true);
        _world.Add(Item, ObjectType.Item);
        _world.Add(Replacement, ObjectType.Item);
        _world.Equip(Creature, InventorySlot.Chest, Item);
        _state = new TintMapStateService();
        _isDroid = false;
        var apply = ScriptedProxy.Create<ITintMapApplyService>(out _apply);
        _apply.On("ApplyCurrentColors", _ => null);
        var droid = ScriptedProxy.Create<IDroidService>(out _droid);
        _droid.On("IsDroid", _ => _isDroid).On("UpdateEquippedItemSnapshot", _ => null);
        var gui = ScriptedProxy.Create<IGuiService>(out _gui);
        _gui.On("PublishRefreshEvent", _ => null);
        var itemColor = ScriptedProxy.Create<ITintMapItemColorService>(out _);
        _service = new TintMapCarryService(apply, droid, gui, itemColor, _state);
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _world.Dispose();
    }

    private static string Variable(string material, TintMapLayerType layer) => TintMapVariable.GetName(material, layer);

    private static TintMapMaterialSelection Selection(string material, TintMapLayerType layer = TintMapLayerType.Cloth1,
        uint paletteSource = Item) =>
        new("model", new TintMapMaterialDefinition("Material", material, layer), paletteSource, Creature,
            usesItemColors: true, AppearanceArmor.Torso);

    private static TintMapItemColorCarry Carry(TintMapLayerType layer, params TintMapItemColorSource[] sources) =>
        new(new Dictionary<TintMapLayerType, IReadOnlyList<TintMapItemColorSource>> { [layer] = sources });

    [Test]
    public void CaptureItemCustomColors_ReturnsNothingForAnInvalidItemOrNoSelections()
    {
        _service.CaptureItemCustomColors(999, new[] { Selection("a") }).Sources.Should().BeEmpty();
        _service.CaptureItemCustomColors(Item, null!).Sources.Should().BeEmpty();
    }

    [Test]
    public void CaptureItemCustomColors_KeepsEverySlotOfALayerThatHoldsACustomOrPresetColor()
    {
        var custom = new TintMapColor(1, 2, 3).ToStoredValue();
        _world.SetInt(Item, Variable("a", TintMapLayerType.Cloth1), custom);
        _world.SetInt(Item, Variable("c", TintMapLayerType.Metal1), 9);

        var carry = _service.CaptureItemCustomColors(Item, new[]
        {
            Selection("a"), Selection("a"), Selection("b"),
            Selection("c", TintMapLayerType.Metal1), Selection("d", TintMapLayerType.Leather1),
            Selection("skin", TintMapLayerType.Skin)
        });

        carry.Sources.Keys.Should().BeEquivalentTo(new[] { TintMapLayerType.Cloth1, TintMapLayerType.Metal1 });
        carry.Sources[TintMapLayerType.Cloth1].Should().Equal(
            new TintMapItemColorSource("a", Variable("a", TintMapLayerType.Cloth1), custom),
            new TintMapItemColorSource("b", Variable("b", TintMapLayerType.Cloth1), null));
        carry.Sources[TintMapLayerType.Metal1].Should().ContainSingle().Which.StoredValue.Should().Be(9);
    }

    [Test]
    public void LinkPendingItemColorCarryReplacement_JoinsTheReplacementToThePendingLineage()
    {
        var registration = _state.RegisterPendingItemColorCarry(Item, Replacement, AppearanceArmor.Torso,
            new[] { TintMapLayerType.Cloth1 });
        _world.Add(14, ObjectType.Item);

        _service.LinkPendingItemColorCarryReplacement(Replacement, 14);

        _state.BelongsToItemColorCarryLineage(14, registration.Lineage).Should().BeTrue();
    }

    [Test]
    public void QueueItemCustomColorCarry_AppliesAtOnceAndDropsTheStaleSourceVariables()
    {
        var stale = Variable("old", TintMapLayerType.Cloth1);
        _world.SetInt(Item, stale, 5);
        var carry = Carry(TintMapLayerType.Cloth1, new TintMapItemColorSource("old", stale, 5));

        _service.QueueItemCustomColorCarry(Creature, Item, Item, Player, InventorySlot.Chest, AppearanceArmor.Torso, carry,
            applyImmediately: true);

        _world.Int(Item, stale).Should().Be(0);
        _apply.Calls.Should().ContainSingle(call => call.Member == "ApplyCurrentColors" && (uint)call.Arguments[0]! == Creature);
        _gui.Calls.Should().ContainSingle(call => call.Member == "PublishRefreshEvent" && (uint)call.Arguments[0]! == Player
            && call.Arguments[1] is AppearanceChangedRefreshEvent);
        _world.Engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void QueueItemCustomColorCarry_SnapshotsAnEquippedDroidItemWhenItRemovedAStaleVariable()
    {
        _isDroid = true;
        var stale = Variable("old", TintMapLayerType.Cloth1);
        _world.SetInt(Item, stale, 5);

        _service.QueueItemCustomColorCarry(Creature, Item, Item, 999, InventorySlot.Chest, AppearanceArmor.Torso,
            Carry(TintMapLayerType.Cloth1, new TintMapItemColorSource("old", stale, 5)), applyImmediately: true);

        _droid.Calls.Should().ContainSingle(call => call.Member == "UpdateEquippedItemSnapshot");
        _gui.Calls.Should().BeEmpty("no valid player has the editor open");
    }

    [Test]
    public void QueueItemCustomColorCarry_WaitsForTheDelayUnlessAskedToApplyAtOnce()
    {
        var stale = Variable("old", TintMapLayerType.Cloth1);
        _world.SetInt(Item, stale, 5);

        _service.QueueItemCustomColorCarry(Creature, Item, Item, Player, InventorySlot.Chest, AppearanceArmor.Torso,
            Carry(TintMapLayerType.Cloth1, new TintMapItemColorSource("old", stale, 5)));

        _world.Int(Item, stale).Should().Be(5);
        _world.Engine.CallsTo("DelayCommand").Should().ContainSingle();
        _world.RunDelayed();
        _world.Int(Item, stale).Should().Be(0);
    }

    [Test]
    public void QueueItemCustomColorCarry_LeavesAnItemThatNoLongerHoldsTheSlot()
    {
        var stale = Variable("old", TintMapLayerType.Cloth1);
        _world.SetInt(Replacement, stale, 5);

        _service.QueueItemCustomColorCarry(Creature, Item, Replacement, Player, InventorySlot.Chest, AppearanceArmor.Torso,
            Carry(TintMapLayerType.Cloth1, new TintMapItemColorSource("old", stale, 5)), applyImmediately: true);

        _world.Int(Replacement, stale).Should().Be(5, "an ordinary equipment change must never redirect colors");
        _apply.Calls.Should().BeEmpty();
    }

    [Test]
    public void QueueItemCustomColorCarry_FollowsTheSlotToARegisteredDescendantOfADestroyedReplacement()
    {
        var stale = Variable("old", TintMapLayerType.Cloth1);
        _world.SetInt(Replacement, stale, 5);
        _service.QueueItemCustomColorCarry(Creature, Item, Item, Player, InventorySlot.Chest, AppearanceArmor.Torso,
            Carry(TintMapLayerType.Cloth1, new TintMapItemColorSource("old", stale, 5)));
        _service.LinkPendingItemColorCarryReplacement(Item, Replacement);
        _world.Remove(Item);
        _world.Equip(Creature, InventorySlot.Chest, Replacement);

        _world.RunDelayed();

        _world.Int(Replacement, stale).Should().Be(0);
        _apply.Calls.Should().ContainSingle(call => call.Member == "ApplyCurrentColors");
    }

    [Test]
    public void QueueItemCustomColorCarry_SkipsALayerThatANewerEditReplaced()
    {
        var stale = Variable("old", TintMapLayerType.Cloth1);
        _world.SetInt(Item, stale, 5);
        _service.QueueItemCustomColorCarry(Creature, Item, Item, Player, InventorySlot.Chest, AppearanceArmor.Torso,
            Carry(TintMapLayerType.Cloth1, new TintMapItemColorSource("old", stale, 5)));
        _state.MarkPendingItemColorEdit(Item, TintMapLayerType.Cloth1, AppearanceArmor.Torso);

        _world.RunDelayed();

        _world.Int(Item, stale).Should().Be(5);
        _apply.Calls.Should().ContainSingle(call => call.Member == "ApplyCurrentColors", "the rest of the carry still renders");
    }

    [Test]
    public void QueueItemCustomColorCarry_IgnoresAnInvalidCreatureAndAnInvalidItem()
    {
        var carry = Carry(TintMapLayerType.Cloth1);

        _service.QueueItemCustomColorCarry(999, Item, Item, Player, InventorySlot.Chest, AppearanceArmor.Torso, carry, true);
        _service.QueueItemCustomColorCarry(Creature, Item, 999, Player, InventorySlot.Chest, AppearanceArmor.Torso, carry, true);

        _apply.Calls.Should().BeEmpty();
    }

    [Test]
    public void QueueItemCustomColorCarry_WithoutACapturedCarryOnlyLinksTheReplacement()
    {
        var registration = _state.RegisterPendingItemColorCarry(Item, Replacement, AppearanceArmor.Torso,
            new[] { TintMapLayerType.Cloth1 });
        _world.Add(14, ObjectType.Item);

        _service.QueueItemCustomColorCarry(Creature, Replacement, 14, Player, InventorySlot.Chest, AppearanceArmor.Torso,
            null!, true);

        _state.BelongsToItemColorCarryLineage(14, registration.Lineage).Should().BeTrue();
        _apply.Calls.Should().BeEmpty();
    }

    [Test]
    public void CarryStoredEquipmentCustomColors_DoesNothingForACreatureWithoutWornMaterials()
    {
        _service.CarryStoredEquipmentCustomColors(Creature);

        _world.LocalsOf(Creature).Should().BeEmpty();
    }
}
