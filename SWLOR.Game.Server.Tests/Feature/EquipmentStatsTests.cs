using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The equipment handlers run the actions the stat service holds, in the order the item's properties walk, with the stats
/// of a displaced item removed before the new item's are added. The actions here only record; what each real action does
/// is pinned by the harness tests of max HP and Combat Readiness and by the engine tests.
/// </summary>
[NonParallelizable]
public class EquipmentStatsTests
{
    private const uint Wearer = 100;
    private const uint Chest = 201;
    private const uint Glove = 202;
    private const uint Pistol = 203;
    private const uint Displaced = 204;

    private FacadeContainer _facades = null!;
    private IDisposable _installed = null!;
    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private ItemWorld _items = null!;
    private LocalVariables _locals = null!;
    private List<IDisposable> _scopes = null!;
    private List<string> _log = null!;

    [SetUp]
    public void SetUp()
    {
        // A new container each time: the actions are instance state, empty until they are registered.
        _facades = new FacadeContainer();
        _installed = _facades.Install();
        _engine = new FakeNWScript();
        _events = new FakeEventsPlugin();
        _items = new ItemWorld(_engine);
        _locals = new LocalVariables(_engine);
        _log = new List<string>();
        _scopes = new List<IDisposable> { NWScriptRuntime.Override(_engine), EventsPluginRuntime.Override(_events) };
        _engine.SetReturn("OBJECT_SELF", Wearer);
        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturnHandler("StringToObject", args => uint.Parse((string)args[0]!));
        _events.SetReturnHandler("GetEventData", args => (string)args[0]! == "ITEM" ? Chest.ToString() : ((int)InventorySlot.Chest).ToString());

        _items.Add(Wearer);
        _items.Add(Chest, baseItem: BaseItem.Armor);
        _items.AddProperty(Chest, ItemPropertyType.Attack, costTableValue: 1);
        _items.AddProperty(Chest, ItemPropertyType.Invalid, costTableValue: 9);
        _items.AddProperty(Chest, ItemPropertyType.Evasion, costTableValue: 2);
        _items.Add(Glove, baseItem: BaseItem.PowerGlove);
        _items.AddProperty(Glove, ItemPropertyType.Attack, costTableValue: 3);
        _items.Add(Pistol, baseItem: BaseItem.CompanionPistol);
        _items.AddProperty(Pistol, ItemPropertyType.Attack, costTableValue: 4);
        _items.Add(Displaced, baseItem: BaseItem.Armor);
        _items.AddProperty(Displaced, ItemPropertyType.Evasion, costTableValue: 5);

        Stat.RegisterEquipmentStatAction(ItemPropertyType.Attack, (creature, item, _, isAdding) => _log.Add($"Attack:{creature}:{item}:{isAdding}"));
        Stat.RegisterEquipmentStatAction(ItemPropertyType.Evasion, (creature, item, _, isAdding) => _log.Add($"Evasion:{creature}:{item}:{isAdding}"));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _installed.Dispose();
        _facades.Dispose();
    }

    [Test]
    public void ApplyItemStats_RunsTheActionOfEachPropertyInOrderAndSkipsOthers()
    {
        EquipmentStats.ApplyItemStats(Wearer, Chest, true);

        _log.Should().Equal("Attack:100:201:True", "Evasion:100:201:True");
    }

    [Test]
    public void ApplyItemStats_RunsOnEveryCopyOfAPowerGloveAndNeverOnACompanionPistol()
    {
        EquipmentStats.ApplyItemStats(Wearer, Glove, false);
        _log.Should().Equal("Attack:100:202:False", "Attack:100:202:False");

        _log.Clear();
        EquipmentStats.ApplyItemStats(Wearer, Pistol, true);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ApplyItemStats_BeforeTheActionsAreRegisteredDoesNothing()
    {
        using var fresh = new FacadeContainer();
        using var installed = fresh.Install();

        EquipmentStats.ApplyItemStats(Wearer, Chest, true);
        EquipmentStats.ApplyStatAmount(Wearer, Chest, ItemPropertyType.Attack, -1, 5, true);

        _log.Should().BeEmpty();
    }

    [Test]
    public void ApplyStatAmount_RunsTheActionForEachCopyWithAPropertyOfThatSize()
    {
        EquipmentStats.ApplyStatAmount(Wearer, Glove, ItemPropertyType.Attack, -1, 5, true);

        _log.Should().Equal("Attack:100:202:True", "Attack:100:202:True");
        _engine.CallsTo("ItemPropertyCustom").Should().HaveCount(2);
    }

    [Test]
    public void ApplyStatAmount_SkipsNothingToApplyAnUnregisteredTypeAndACompanionPistol()
    {
        EquipmentStats.ApplyStatAmount(Wearer, Chest, ItemPropertyType.Attack, -1, 0, true);
        EquipmentStats.ApplyStatAmount(Wearer, Chest, ItemPropertyType.Accuracy, -1, 5, true);
        EquipmentStats.ApplyStatAmount(Wearer, Pistol, ItemPropertyType.Attack, -1, 5, true);

        _log.Should().BeEmpty();
    }

    [Test]
    public void ApplyStats_RemovesADisplacedItemFirstAndRemembersWhatItFed()
    {
        _items.Equip(Wearer, InventorySlot.Chest, Displaced);

        EquipmentStats.ApplyStats();

        _log.Should().Equal("Evasion:100:204:False", "Attack:100:201:True", "Evasion:100:201:True");
        _locals.Int(Wearer, "EQUIPMENT_DISPLACED_RECORD_STATS").Should().Be((int)SWLOR.Game.Server.Domain.Character.EquipmentRecordStats.Evasion);
    }

    [Test]
    public void ApplyStats_WithAnEmptySlotOnlyAdds()
    {
        EquipmentStats.ApplyStats();

        _log.Should().Equal("Attack:100:201:True", "Evasion:100:201:True");
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void ApplyStatsAndRemoveStats_SkipStaff(bool dm, bool possessed)
    {
        _engine.SetReturn("GetIsDM", dm);
        _engine.SetReturn("GetIsDMPossessed", possessed);
        _items.Equip(Wearer, InventorySlot.Chest, Chest);

        EquipmentStats.ApplyStats();
        EquipmentStats.RemoveStats();

        _log.Should().BeEmpty();
    }

    [Test]
    public void RemoveStats_OnlyRemovesAnItemThatIsStillWorn()
    {
        EquipmentStats.RemoveStats();
        _log.Should().BeEmpty("a queued second unequip names an item that is no longer worn");

        _items.Equip(Wearer, InventorySlot.Chest, Chest);
        EquipmentStats.RemoveStats();
        _log.Should().Equal("Attack:100:201:False", "Evasion:100:201:False");
    }
}
