using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ResistanceService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The real equipment actions, run through <see cref="EquipmentStats.ApplyItemStats"/>: a player's record moves and is
/// written once per property, a stat skin takes the amount instead for an NPC, staff are skipped, and the handlers that run
/// after a completed change rebuild the records and refresh the status window for what they corrected.
/// </summary>
[NonParallelizable]
public class EquipmentStatActionTests
{
    private const uint Wearer = 100;
    private const uint Worn = 201;
    private const uint Skin = 202;
    private const uint Made = 203;
    private const string Uuid = "uuid-100";

    private FacadeContainer _facades = null!;
    private IDisposable _installed = null!;
    private InMemoryDBService _db = null!;
    private FakeNWScript _engine = null!;
    private FakeObjectPlugin _object = null!;
    private FakeEventsPlugin _events = null!;
    private ItemWorld _items = null!;
    private List<IDisposable> _scopes = null!;
    private List<object> _refreshes = null!;
    private Player _record = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new InMemoryDBService();
        _refreshes = new List<object>();
        var gui = ScriptedProxy.Create<IGuiService>(out var guiProxy);
        guiProxy.On("PublishRefreshEvent", args =>
        {
            _refreshes.Add(args[1]!);
            return null;
        });
        _facades = new FacadeContainer(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IDBService>(_db));
            services.Replace(ServiceDescriptor.Singleton(gui));
        });
        _installed = _facades.Install();

        _engine = new FakeNWScript();
        _object = new FakeObjectPlugin();
        _events = new FakeEventsPlugin();
        _items = new ItemWorld(_engine);
        _scopes = new List<IDisposable>
        {
            NWScriptRuntime.Override(_engine),
            ObjectPluginRuntime.Override(_object),
            EventsPluginRuntime.Override(_events),
            CreaturePluginRuntime.Override(new FakeCreaturePlugin())
        };
        _engine.SetReturn("OBJECT_SELF", Wearer);
        _engine.SetReturn("GetObjectUUID", Uuid);
        _engine.SetReturnHandler("StringToObject", args => uint.Parse((string)args[0]!));
        _events.SetReturnHandler("GetEventData", args => (string)args[0]! == "ITEM" ? Worn.ToString() : ((int)InventorySlot.Chest).ToString());

        // The properties the code makes go on the engine's walk of this item, so the code can read them back.
        var made = 0;
        _engine.SetReturnHandler("ItemPropertyCustom", args =>
        {
            _items.AddProperty(Made, (ItemPropertyType)args[0]!, (int)args[1]!, (int)args[2]!);
            return _items.Property(Made, made++);
        });

        _items.Add(Wearer);
        _items.Add(Worn, baseItem: BaseItem.Armor);
        _record = new Player(Uuid);
        _db.With(_record);

        EquipmentStats.RegisterStatActions();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _installed.Dispose();
        _facades.Dispose();
    }

    private void IsPlayer() => _engine.SetReturn("GetIsPC", true);

    private void Give(ItemPropertyType type, int subtype, int amount) => _items.AddProperty(Worn, type, subtype, amount);

    private void WearSkin(params (ItemPropertyType Type, int SubType, int Amount)[] properties)
    {
        _items.Add(Skin, baseItem: BaseItem.CreatureItem);
        _items.Equip(Wearer, InventorySlot.CreatureArmor, Skin);
        foreach (var (type, subtype, amount) in properties)
            _items.AddProperty(Skin, type, subtype, amount);
    }

    private static readonly IReadOnlyDictionary<ItemPropertyType, Func<Player, int>> Records = new Dictionary<ItemPropertyType, Func<Player, int>>
    {
        [ItemPropertyType.HPBonus] = player => player.MaxHP,
        [ItemPropertyType.FP] = player => player.MaxFP,
        [ItemPropertyType.FPRegen] = player => player.FPRegen,
        [ItemPropertyType.Stamina] = player => player.MaxStamina,
        [ItemPropertyType.STMRegen] = player => player.STMRegen,
        [ItemPropertyType.Attack] = player => player.Attack,
        [ItemPropertyType.Accuracy] = player => player.Accuracy,
        [ItemPropertyType.ForceAttack] = player => player.ForceAttack,
        [ItemPropertyType.Evasion] = player => player.Evasion,
        [ItemPropertyType.Stealth] = player => player.Stealth,
        [ItemPropertyType.Detection] = player => player.Detection,
        [ItemPropertyType.TrapBonus] = player => player.TrapBonus,
        [ItemPropertyType.Disarm] = player => player.TrapDisarm,
        [ItemPropertyType.PoisonBonus] = player => player.PoisonBonus,
        [ItemPropertyType.Lockpicking] = player => player.Lockpicking,
    };

    private static IEnumerable<ItemPropertyType> SummedTypes => Records.Keys;

    [TestCaseSource(nameof(SummedTypes))]
    public void APlayersRecord_MovesAndIsWrittenOnEquipAndUnequip(ItemPropertyType type)
    {
        IsPlayer();
        Give(type, -1, 5);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);
        Records[type](_record).Should().Be(5);
        _db.Saved.Should().ContainSingle();

        EquipmentStats.ApplyItemStats(Wearer, Worn, false);
        Records[type](_record).Should().Be(0);
        _db.Saved.Should().HaveCount(2);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void Staff_AreSkippedByEveryAction(bool dm, bool possessed)
    {
        IsPlayer();
        _engine.SetReturn("GetIsDM", dm);
        _engine.SetReturn("GetIsDMPossessed", possessed);
        foreach (var type in Records.Keys)
            Give(type, -1, 5);
        Give(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 5);
        Give(ItemPropertyType.Resistance, (int)ResistanceType.Fire, 5);
        Give(ItemPropertyType.Control, 1, 5);
        Give(ItemPropertyType.Craftsmanship, 1, 5);
        Give(ItemPropertyType.CPBonus, 1, 5);
        Give(ItemPropertyType.CombatReadiness, -1, 5);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Defense_AddsToItsDamageTypeAndIgnoresARetiredOne()
    {
        IsPlayer();
        Give(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 5);
        Give(ItemPropertyType.Defense, (int)CombatDamageType.Fire, 9);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);
        _record.Defenses[CombatDamageType.Physical].Should().Be(5);
        _db.Saved.Should().ContainSingle("the retired type does nothing");

        EquipmentStats.ApplyItemStats(Wearer, Worn, false);
        _record.Defenses[CombatDamageType.Physical].Should().Be(0);
    }

    [Test]
    public void Resistance_AddsToItsTypeAndIgnoresAnUnknownOne()
    {
        IsPlayer();
        Give(ItemPropertyType.Resistance, (int)ResistanceType.Fire, 5);
        Give(ItemPropertyType.Resistance, 9999, 5);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);
        _record.Resistances[ResistanceType.Fire].Should().Be(5);
        _db.Saved.Should().ContainSingle();

        EquipmentStats.ApplyItemStats(Wearer, Worn, false);
        _record.Resistances[ResistanceType.Fire].Should().Be(0);
    }

    [TestCase(1, SkillType.Smithery)]
    [TestCase(2, SkillType.Engineering)]
    [TestCase(3, SkillType.Fabrication)]
    [TestCase(4, SkillType.Agriculture)]
    public void TheCraftStats_GoToTheSkillOfTheirSubtype(int subtype, SkillType skill)
    {
        IsPlayer();
        Give(ItemPropertyType.Control, subtype, 2);
        Give(ItemPropertyType.Craftsmanship, subtype, 3);
        Give(ItemPropertyType.CPBonus, subtype, 4);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);
        _record.Control[skill].Should().Be(2);
        _record.Craftsmanship[skill].Should().Be(3);
        _record.CPBonus[skill].Should().Be(4);

        EquipmentStats.ApplyItemStats(Wearer, Worn, false);
        _record.Control[skill].Should().Be(0);
        _record.Craftsmanship[skill].Should().Be(0);
        _record.CPBonus[skill].Should().Be(0);
    }

    [Test]
    public void Control_RefusesAnUnknownSubtype()
    {
        IsPlayer();
        Give(ItemPropertyType.Control, 99, 2);

        var apply = () => EquipmentStats.ApplyItemStats(Wearer, Worn, true);

        apply.Should().Throw<Exception>().WithMessage("*ApplyControl*");
    }

    [Test]
    public void TheCraftStats_OfAnNpcDoNothing()
    {
        Give(ItemPropertyType.Control, 1, 2);
        Give(ItemPropertyType.Craftsmanship, 1, 2);
        Give(ItemPropertyType.CPBonus, 1, 2);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);

        _db.Saved.Should().BeEmpty();
        _engine.CountOf("ItemPropertyCustom").Should().Be(0);
    }

    [Test]
    public void Accuracy_OnAWeaponOrCreatureItemBelongsToThatItem()
    {
        IsPlayer();
        _items.Add(Skin, baseItem: BaseItem.Longsword);
        _items.AddProperty(Skin, ItemPropertyType.Accuracy, -1, 5);

        EquipmentStats.ApplyItemStats(Wearer, Skin, true);

        _record.Accuracy.Should().Be(0);
    }

    [Test]
    public void CombatReadiness_OfAPlayerWaitsForTheCompletedChange()
    {
        IsPlayer();
        Give(ItemPropertyType.CombatReadiness, -1, 5);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);

        _record.CombatReadiness.Should().Be(0);
        _db.Saved.Should().BeEmpty();
    }

    [TestCase(ItemPropertyType.FP)]
    [TestCase(ItemPropertyType.FPRegen)]
    [TestCase(ItemPropertyType.Stamina)]
    [TestCase(ItemPropertyType.STMRegen)]
    [TestCase(ItemPropertyType.CombatReadiness)]
    [TestCase(ItemPropertyType.Attack)]
    [TestCase(ItemPropertyType.Accuracy)]
    [TestCase(ItemPropertyType.ForceAttack)]
    [TestCase(ItemPropertyType.Evasion)]
    [TestCase(ItemPropertyType.Stealth)]
    [TestCase(ItemPropertyType.Detection)]
    [TestCase(ItemPropertyType.TrapBonus)]
    [TestCase(ItemPropertyType.Disarm)]
    [TestCase(ItemPropertyType.PoisonBonus)]
    [TestCase(ItemPropertyType.Lockpicking)]
    public void AnNpcsSkin_TakesTheAmountInsteadAndLosesThePropertyAtZero(ItemPropertyType type)
    {
        WearSkin((type, -1, 2));
        Give(type, -1, 5);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);
        _engine.CallsTo("ItemPropertyCustom").Single().Arguments.Should().StartWith(new object[] { type, -1, 7 });

        _engine.ClearCalls();
        EquipmentStats.ApplyItemStats(Wearer, Worn, false);
        _engine.CountOf("ItemPropertyCustom").Should().Be(0, "2 less 5 removes the property");
        _engine.CountOf("RemoveItemProperty").Should().BeGreaterThan(0);
    }

    [Test]
    public void AnNpcsHitPoints_FollowItsSkin()
    {
        WearSkin((ItemPropertyType.NPCHP, -1, 100));
        Give(ItemPropertyType.HPBonus, -1, 50);
        _engine.SetReturnHandler("GetMaxHitPoints", _ => (int)(_object.CallsTo("SetMaxHitPoints").LastOrDefault()?.Arguments[1] ?? 0));
        _object.SetReturn("GetCurrentHitPoints", 500);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);

        _engine.CallsTo("ItemPropertyCustom").Single().Arguments.Should().StartWith(new object[] { ItemPropertyType.NPCHP, -1, 150 });
        _object.CallsTo("SetMaxHitPoints").Last().Arguments[1].Should().Be(150);

        _engine.ClearCalls();
        EquipmentStats.ApplyItemStats(Wearer, Worn, false);
        _engine.CallsTo("ItemPropertyCustom").Single().Arguments.Should().StartWith(new object[] { ItemPropertyType.NPCHP, -1, 50 });
    }

    [Test]
    public void AnNpcsDefense_CountsOnlyItsDamageTypeAndLosesThePropertyAtZero()
    {
        WearSkin((ItemPropertyType.Defense, (int)CombatDamageType.Physical, 2), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 90));
        Give(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 5);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);
        _engine.CallsTo("ItemPropertyCustom").Single().Arguments.Should().StartWith(new object[] { ItemPropertyType.Defense, (int)CombatDamageType.Physical, 7 });

        _engine.ClearCalls();
        EquipmentStats.ApplyItemStats(Wearer, Worn, false);
        _engine.CountOf("ItemPropertyCustom").Should().Be(0);
    }

    [Test]
    public void AnNpcsResistance_CountsOnlyItsTypeAndLosesThePropertyAtExactlyZero()
    {
        WearSkin((ItemPropertyType.Resistance, (int)ResistanceType.Fire, 5), (ItemPropertyType.Resistance, (int)ResistanceType.Ice, 90));
        Give(ItemPropertyType.Resistance, (int)ResistanceType.Fire, 5);

        EquipmentStats.ApplyItemStats(Wearer, Worn, true);
        _engine.CallsTo("ItemPropertyCustom").Single().Arguments.Should().StartWith(new object[] { ItemPropertyType.Resistance, (int)ResistanceType.Fire, 10 });

        _engine.ClearCalls();
        EquipmentStats.ApplyItemStats(Wearer, Worn, false);
        _engine.CountOf("ItemPropertyCustom").Should().Be(0);
    }

    [Test]
    public void RefreshCombatReadiness_RebuildsTheRecordFromTheSlots()
    {
        IsPlayer();
        Give(ItemPropertyType.CombatReadiness, -1, 7);
        _items.Equip(Wearer, InventorySlot.Chest, Worn);

        EquipmentStats.RefreshCombatReadinessAfterEquipmentChange();

        _record.CombatReadiness.Should().Be(7);
    }

    [Test]
    public void RefreshMaxHP_OnlyRunsForAnItemWithHPAndRefreshesWhatItCorrected()
    {
        IsPlayer();
        _record.MaxHP = SWLOR.Game.Server.Service.Stat.BaseHP;

        EquipmentStats.RefreshMaxHPAfterEquipmentChange();
        _refreshes.Should().BeEmpty();
        _db.Reads.Should().BeEmpty("an item with no HP has no reason to read the record");

        Give(ItemPropertyType.HPBonus, -1, 10);
        _items.Equip(Wearer, InventorySlot.Chest, Worn);
        EquipmentStats.RefreshMaxHPAfterEquipmentChange();

        _record.MaxHP.Should().Be(SWLOR.Game.Server.Service.Stat.BaseHP + 10);
        _refreshes.Should().ContainSingle().Which.GetType().Name.Should().Be("PlayerStatusRefreshEvent");
    }

    [Test]
    public void RefreshMaxHP_OfACorrectRecordRefreshesNothing()
    {
        IsPlayer();
        Give(ItemPropertyType.HPBonus, -1, 10);
        _items.Equip(Wearer, InventorySlot.Chest, Worn);
        _record.MaxHP = SWLOR.Game.Server.Service.Stat.BaseHP + 10;

        EquipmentStats.RefreshMaxHPAfterEquipmentChange();

        _refreshes.Should().BeEmpty();
    }

    [Test]
    public void RefreshEquipmentRecords_RebuildsWhatTheItemAndADisplacedOneFedAndClearsTheMemory()
    {
        IsPlayer();
        _record.MaxFP = 500;
        _record.MaxStamina = 500;
        _record.Evasion = 9;
        Give(ItemPropertyType.FP, -1, 1);
        _engine.SetReturn("GetLocalInt", (int)SWLOR.Game.Server.Service.StatService.EquipmentRecordStats.Stamina);

        EquipmentStats.RefreshEquipmentRecordsAfterEquipmentChange();

        _record.MaxFP.Should().Be(SWLOR.Game.Server.Service.Stat.BaseFP);
        _record.MaxStamina.Should().Be(SWLOR.Game.Server.Service.Stat.BaseSTM);
        _record.Evasion.Should().Be(9, "neither item feeds it");
        _refreshes.Should().HaveCount(2);
        _engine.CountOf("DeleteLocalInt").Should().Be(1);
    }

    [Test]
    public void RefreshEquipmentRecords_RefreshesNothingWhenThePoolsAreRight()
    {
        IsPlayer();
        _record.MaxFP = SWLOR.Game.Server.Service.Stat.BaseFP;

        EquipmentStats.RefreshEquipmentRecordsAfterEquipmentChange();

        _refreshes.Should().BeEmpty();
    }
}
