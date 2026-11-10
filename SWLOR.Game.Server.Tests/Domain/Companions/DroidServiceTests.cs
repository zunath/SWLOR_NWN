using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;


namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// Unit tests of the droid instance (the static <c>Service.Droid</c> until P2-B12): the decisions, the tables and the
/// controller-backed saved data that the conversion makes reachable without the engine. Spawning, despawning and the
/// item snapshot need a server and stay with the engine tests (<c>DroidInventoryEngineTests</c>).
/// </summary>
[TestFixture]
public class DroidServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Droid = 10;
    private const uint Player = 11;
    private const uint Controller = 12;
    private const uint Disc = 13;
    private const uint Creature = 14;
    private const uint Master = 15;

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private IDisposable _scope = null!;
    private Dictionary<uint, string> _resRefs = null!;
    private List<(ItemPropertyType Type, int SubType, int Value)> _properties = null!;
    private int _cursor;
    private ScriptedProxy _gui = null!;
    private DroidService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _resRefs = new Dictionary<uint, string> { [Droid] = DroidService.DroidResref };
        _properties = new List<(ItemPropertyType, int, int)>();
        _cursor = 0;

        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetResRef", args => _resRefs.GetValueOrDefault((uint)args[0]!, string.Empty));
        var property = EngineStructures.Create<ItemProperty>(1);
        _engine.SetReturnHandler("GetFirstItemProperty", _ => { _cursor = 0; return property; });
        _engine.SetReturnHandler("GetNextItemProperty", _ => { _cursor++; return property; });
        _engine.SetReturnHandler("GetIsItemPropertyValid", _ => _cursor < _properties.Count);
        _engine.SetReturnHandler("GetItemPropertyType", _ => _properties[_cursor].Type);
        _engine.SetReturnHandler("GetItemPropertySubType", _ => _properties[_cursor].SubType);
        _engine.SetReturnHandler("GetItemPropertyCostTableValue", _ => _properties[_cursor].Value);
        _scope = NWScriptRuntime.Override(_engine);

        var gui = ScriptedProxy.Create<IGuiService>(out _gui);
        _service = new DroidService(
            FacadeForwarder.For<IAIService>(),
            FacadeForwarder.For<ICombatRecorderService>(),
            FacadeForwarder.For<ICompanionControlService>(),
            FacadeForwarder.For<IDroidEquipmentDiagnosticsService>(),
            gui,
            FacadeForwarder.For<IItemService>(),
            FacadeForwarder.For<IPerkService>(),
            FacadeForwarder.For<IPowerGloveEquipmentService>(),
            FacadeForwarder.For<IRaceService>(),
            FacadeForwarder.For<IRecastService>(),
            FacadeForwarder.For<IResistanceService>(),
            FacadeForwarder.For<IStatService>(),
            FacadeForwarder.For<IStatusEffectService>(),
            FacadeForwarder.For<ITwinPistolEquipmentService>());
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    // ---- decisions

    [Test]
    public void IsDroid_IsTheResrefOfThePlayerDroid()
    {
        _resRefs[Creature] = "nw_goblin";

        _service.IsDroid(Droid).Should().BeTrue();
        _service.IsDroid(Creature).Should().BeFalse();
    }

    [TestCase(RacialType.Droid, false, true)]
    [TestCase(RacialType.Robot, false, true)]
    [TestCase(RacialType.Construct, false, true)]
    [TestCase(RacialType.Human, false, false)]
    [TestCase(RacialType.Droid, true, false)]
    public void IsBloodlessMachine_IsAnEnemyOrNPCMachineButNeverAPlayer(RacialType racialType, bool isPlayer, bool expected)
    {
        _engine.SetReturn("GetIsPC", isPlayer);
        _engine.SetReturn("GetRacialType", racialType);
        _resRefs[Creature] = "nw_creature";

        _service.IsBloodlessMachine(Creature).Should().Be(expected);
    }

    [Test]
    public void IsBloodlessMachine_KeepsTheNormalBleedOfAPlayerDroid()
    {
        _engine.SetReturn("GetIsPC", false);
        _engine.SetReturn("GetRacialType", RacialType.Droid);

        _service.IsBloodlessMachine(Droid).Should().BeFalse();
    }

    [Test]
    public void IsSpawning_FollowsTheSpawningFlagOnTheDroid()
    {
        _service.IsSpawning(Droid).Should().BeFalse();

        _engine.SetLocalBool(Droid, "DROID_IS_SPAWNING", true);
        _service.IsSpawning(Droid).Should().BeTrue();

        _engine.DeleteLocalBool(Droid, "DROID_IS_SPAWNING");
        _service.IsSpawning(Droid).Should().BeFalse();
    }

    [Test]
    public void GetControllerItem_AndGetDroid_ReadTheLinksSetWhenTheDroidWasSummoned()
    {
        _service.GetControllerItem(Droid).Should().Be(ObjectInvalid);
        _service.GetDroid(Player).Should().Be(ObjectInvalid);

        _engine.SetLocalObject(Droid, "ACTIVE_DROID_ITEM", Controller);
        _engine.SetLocalObject(Player, "ACTIVE_DROID", Droid);

        _service.GetControllerItem(Droid).Should().Be(Controller);
        _service.GetDroid(Player).Should().Be(Droid);
    }

    [Test]
    public void GetInstructionDiscValidationError_NamesTheFirstProblemOfTheSelectedItem()
    {
        _engine.SetReturn("GetItemPossessor", Player);
        _resRefs[Disc] = "droid_disc";

        _service.GetInstructionDiscValidationError(Player, ObjectInvalid)
            .Should().Be("Select an instruction disc from your inventory.");

        _engine.SetReturn("GetItemPossessor", Creature);
        _service.GetInstructionDiscValidationError(Player, Disc)
            .Should().Be("Select an instruction disc from your inventory.");

        _engine.SetReturn("GetItemPossessor", Player);
        _resRefs[Disc] = DroidService.DroidControlItemResref;
        _service.GetInstructionDiscValidationError(Player, Disc)
            .Should().Be("A droid controller cannot be used as an instruction disc.");

        _resRefs[Disc] = "droid_disc";
        _service.GetInstructionDiscValidationError(Player, Disc)
            .Should().Be("That item is not a droid instruction disc.");

        _properties.Add((ItemPropertyType.DroidInstruction, 0, 0));
        _service.GetInstructionDiscValidationError(Player, Disc).Should().BeEmpty();
    }

    [Test]
    public void GetPendingItemRestoreKey_PutsTheSlotAndTheItemIdAfterTheRestorePrefix()
    {
        _service.GetPendingItemRestoreKey(InventorySlot.RightHand, "abc")
            .Should().Be("DROID_RESTORE_ITEM:" + (int)InventorySlot.RightHand + ":abc");
    }

    // ---- the assembly terminal handler

    [Test]
    public void UseDroidAssemblyTerminal_OpensTheWindowForAPlayerButNotForADMOrACreature()
    {
        _gui.On("TogglePlayerWindow", _ => null);
        _engine.SetReturn("OBJECT_SELF", Master);
        _engine.SetReturn("GetLastUsedBy", Player);

        _engine.SetReturn("GetIsPC", false);
        _service.UseDroidAssemblyTerminal();
        _gui.Calls.Should().BeEmpty();

        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturn("GetIsDM", true);
        _service.UseDroidAssemblyTerminal();
        _gui.Calls.Should().BeEmpty();

        _engine.SetReturn("GetIsDM", false);
        _service.UseDroidAssemblyTerminal();
        _gui.Calls.Should().ContainSingle().Which.Member.Should().Be("TogglePlayerWindow");
        _gui.Calls[0].Arguments[0].Should().Be(Player);
        _gui.Calls[0].Arguments[1].Should().Be(GuiWindowType.DroidAssembly);
        _gui.Calls[0].Arguments[3].Should().Be(Master);
    }

    // ---- the controller-backed data

    [Test]
    public void LoadConstructedDroid_WithNothingSaved_GivesTheDefaults()
    {
        var loaded = _service.LoadConstructedDroid(Controller);

        loaded.Name.Should().BeEmpty();
        loaded.PortraitId.Should().Be(-1);
        loaded.SoundSetId.Should().Be(-1);
        loaded.Inventory.Should().BeEmpty();
        loaded.EquippedItems.Should().BeEmpty();
        loaded.AppearanceParts.Should().BeEmpty();
        loaded.TintOverrides.Should().BeEmpty();
    }

    [Test]
    public void SaveConstructedDroid_RoundTripsThroughTheControllerVariable()
    {
        var droid = new ConstructedDroid
        {
            Name = "R2-X",
            PortraitId = 4,
            SoundSetId = 9,
            Inventory = { ["id"] = "serialized" },
            EquippedItems = { [InventorySlot.RightHand] = "rifle" },
            AppearanceParts = { [CreaturePart.Head] = 3 },
            TintOverrides = { ["Head"] = 7 }
        };

        _service.SaveConstructedDroid(Controller, droid);
        var loaded = _service.LoadConstructedDroid(Controller);

        loaded.Name.Should().Be("R2-X");
        loaded.PortraitId.Should().Be(4);
        loaded.SoundSetId.Should().Be(9);
        loaded.Inventory.Should().Equal(droid.Inventory);
        loaded.EquippedItems.Should().Equal(droid.EquippedItems);
        loaded.AppearanceParts.Should().Equal(droid.AppearanceParts);
        loaded.TintOverrides.Should().Equal(droid.TintOverrides);
    }

    [Test]
    public void LoadConstructedDroid_FillsTheListsAndTablesAnOlderSaveLeftOut()
    {
        _engine.SetLocalString(Controller, "CONSTRUCTED_DROID", "{\"Name\":\"Old\",\"Inventory\":null,\"ActivePerks\":null,\"TintOverrides\":null}");

        var loaded = _service.LoadConstructedDroid(Controller);

        loaded.Name.Should().Be("Old");
        loaded.Inventory.Should().NotBeNull().And.BeEmpty();
        loaded.ActivePerks.Should().NotBeNull().And.BeEmpty();
        loaded.LearnedPerks.Should().NotBeNull();
        loaded.TintOverrides.Should().NotBeNull().And.BeEmpty();
    }

    // ---- part details and the tier table

    [Test]
    public void LoadDroidPartItemPropertyDetails_AddsTheStatsAndClampsTheTier()
    {
        _service.CacheData();
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, 9));
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.HP, 20));
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.HP, 5));
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.Pistol, 3));
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceFire, 2));
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceFire, 4));
        _properties.Add((ItemPropertyType.DroidPart, (int)DroidPartItemPropertySubType.Head, 0));

        var details = _service.LoadDroidPartItemPropertyDetails(Disc);

        details.Tier.Should().Be(5, "a tier above the highest is clamped");
        details.Level.Should().Be(45);
        details.HP.Should().Be(25);
        details.Pistol.Should().Be(3);
        details.Resistances[ResistanceType.Fire].Should().Be(6);
        details.PartType.Should().Be(DroidPartItemPropertySubType.Head);
    }

    [TestCase(1, 5)]
    [TestCase(2, 15)]
    [TestCase(3, 25)]
    [TestCase(4, 35)]
    [TestCase(5, 45)]
    public void CacheData_GivesEachTierItsLevel(int tier, int level)
    {
        _service.CacheData();
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, tier));

        _service.LoadDroidPartItemPropertyDetails(Disc).Level.Should().Be(level);
    }

    [Test]
    public void LoadDroidPartItemPropertyDetails_BeforeTheTierTableIsCached_HasNoLevel()
    {
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, 3));

        _service.LoadDroidPartItemPropertyDetails(Disc).Level.Should().Be(0);
    }

    [Test]
    public void LoadDroidPartItemPropertyDetails_IgnoresTheSaberSkillsDroidsCannotUse()
    {
        _service.CacheData();
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.Lightsaber, 4));
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.Saberstaff, 4));
        _properties.Add((ItemPropertyType.DroidStat, (int)DroidStatSubType.Rifle, 2));

        var details = _service.LoadDroidPartItemPropertyDetails(Disc);

        details.Rifle.Should().Be(2);
    }

    [Test]
    public void LoadDroidPartItemPropertyDetails_RejectsAStatItsSwitchDoesNotKnow()
    {
        _properties.Add((ItemPropertyType.DroidStat, 9999, 1));

        var load = () => _service.LoadDroidPartItemPropertyDetails(Disc);

        load.Should().Throw<ArgumentOutOfRangeException>();
    }
}
