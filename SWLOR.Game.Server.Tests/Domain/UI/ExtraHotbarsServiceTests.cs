using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.UI;

/// <summary>
/// Unit tests of the extra hotbar instance: the engine is a recording fake with a small item world, the database an
/// in-memory one, and the GUI, chat command and item services are scripted stand-ins. The calls into static code that
/// is not converted yet (abilities, quick cast, perks, targeting, the equipment storage) are answered by a subclass.
/// </summary>
[TestFixture]
public class ExtraHotbarsServiceTests
{
    private const uint Player = 1001;
    private const uint Sword = 2001;
    private const uint Pistol = 2002;
    private const uint Shield = 2003;
    private const uint Potion = 2004;
    private const uint Rifle = 2005;
    private const uint Target = 3001;
    private const uint Area = 4001;
    private const string PlayerId = PlayerEngine.PlayerId;

    private PlayerEngine _engine = null!;
    private LocalVariables _locals = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _guiProxy = null!;
    private ScriptedProxy _chatProxy = null!;
    private ScriptedProxy _itemProxy = null!;
    private TestHotbars _service = null!;
    private readonly HashSet<GuiWindowType> _openWindows = new();
    private readonly List<(string Name, object?[] Arguments)> _guiCalls = new();
    private readonly Dictionary<GuiWindowType, GuiPlayerWindow> _windows = new();
    private readonly Dictionary<uint, ItemInfo> _items = new();
    private readonly Dictionary<InventorySlot, uint> _held = new();
    private readonly List<uint> _inventory = new();
    private readonly Dictionary<(string File, string Column, int Row), string> _twoDa = new();
    private readonly HashSet<uint> _withActivation = new();
    private readonly HashSet<ActionMode> _activeModes = new();
    private uint _propertyOwner;
    private int _propertyStep;

    private sealed record ItemInfo(string Resref, string Name, string Uuid, BaseItem Type, bool Ranged = false, int Stack = 1);

    private sealed class TestHotbars : ExtraHotbarsService
    {
        public TestHotbars(IDBService db, IGuiService gui, IChatCommandService chat, IItemService item)
            : base(db, gui, chat, item, FacadeForwarder.For<IAbilityService>(), FacadeForwarder.For<ICraftMinigameRoutingService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IQuickCastService>(), FacadeForwarder.For<ITargetingService>(), FacadeForwarder.For<ITwinPistolEquipmentService>())
        {
        }

        public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();
        public HashSet<FeatType> Known { get; } = new();
        public Dictionary<FeatType, FeatType> Displayed { get; } = new();
        public int StealthLevel { get; set; }
        public bool CraftOpen { get; set; }
        public List<(uint Activator, uint Target, FeatType Feat, Location Location)> Used { get; } = new();
        public List<(uint Item, InventorySlot Slot)> PutAway { get; } = new();
        public HashSet<InventorySlot> Stuck { get; } = new();
        public bool PutAwayThrows { get; set; }
        public string CastError { get; set; } = string.Empty;
        public bool CastResult { get; set; }
        public List<(ObjectType Types, string Message, Action<uint> Action, bool Ground)> Targeting { get; } = new();
        public List<(FeatType Feat, FeatType Alias)> Casts { get; } = new();

        protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => Abilities;
        protected override bool IsFeatRegistered(FeatType feat) => Abilities.ContainsKey(feat);
        protected override AbilityDetail GetAbilityDetail(FeatType feat) => Abilities[feat];
        protected override bool KnowsFeat(uint player, FeatType feat) => Known.Contains(feat);
        protected override FeatType GetDisplayedFeat(uint player, FeatType feat) => Displayed.GetValueOrDefault(feat, feat);

        protected override bool TryCast(uint player, FeatType alias, out string error)
        {
            Casts.Add((alias, alias));
            error = CastError;
            return CastResult;
        }

        protected override bool IsAnyCraftWindowOpen(uint player) => CraftOpen;
        protected override int GetStealthPerkLevel(uint player) => StealthLevel;

        protected override bool TryUseAbility(uint activator, uint target, FeatType feat, Location targetLocation)
        {
            Used.Add((activator, target, feat, targetLocation));
            return true;
        }

        protected override void EnterTargetingMode(uint player, ObjectType objectType, string message, Action<uint> selectionAction, bool allowsLocationTarget = false) =>
            Targeting.Add((objectType, message, selectionAction, allowsLocationTarget));

        protected override bool TryUnequip(uint player, uint item, InventorySlot slot, bool allowRecoveryBag)
        {
            if (PutAwayThrows)
                throw new InvalidOperationException("no room");

            PutAway.Add((item, slot));
            return !Stuck.Contains(slot);
        }
    }

    // The hotbar view models reach the Gui facade, which resolves the GUI service from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _playerUuid = PlayerId;
        _ran.Clear();
        _engine = new PlayerEngine();
        _locals = new LocalVariables(_engine.Engine);
        _db = new InMemoryDBService();
        _db.With(new Player(PlayerId));
        _openWindows.Clear();
        _guiCalls.Clear();
        _windows.Clear();
        _items.Clear();
        _held.Clear();
        _inventory.Clear();
        _twoDa.Clear();
        _withActivation.Clear();
        _activeModes.Clear();

        var gui = ScriptedProxy.Create<IGuiService>(out _guiProxy);
        _guiProxy
            .On("IsWindowOpen", args => _openWindows.Contains((GuiWindowType)args[1]!))
            .On("TogglePlayerWindow", args => Record("TogglePlayerWindow", args))
            .On("ClosePlayerWindow", args => Record("ClosePlayerWindow", args))
            .On("PublishRefreshEvent", args => Record("PublishRefreshEvent", args))
            .On("GetPlayerWindow", args => _windows[(GuiWindowType)args[1]!]);
        var chat = ScriptedProxy.Create<IChatCommandService>(out _chatProxy);
        _chatProxy.On("RunCommand", args => Record("RunCommand", args));
        var item = ScriptedProxy.Create<IItemService>(out _itemProxy);
        _itemProxy
            .On("get_ForceSensitiveWeaponBaseItemTypes", _ => new List<BaseItem> { BaseItem.Lightsaber })
            .On("get_PistolBaseItemTypes", _ => new List<BaseItem> { BaseItem.Pistol })
            .On("get_ShieldBaseItemTypes", _ => new List<BaseItem> { BaseItem.SmallShield })
            .On("CanEquipForceSensitiveWeapon", _ => true)
            .On("GetCreatureItemUseError", _ => string.Empty)
            .On("GetRacialEquipError", _ => string.Empty);
        _service = new TestHotbars(_db, gui, chat, item);

        _engine.Engine.SetReturn("GetEnteringObject", Player);
        _engine.Engine.SetReturn("GetExitingObject", Player);
        _engine.Engine.SetReturn("GetArea", Area);
        InstallItemWorld();
    }

    [TearDown]
    public void TearDown()
    {
        _facadeScope.Dispose();
        _engine.Dispose();
    }

    private object? Record(string name, object?[] args)
    {
        _guiCalls.Add((name, args));
        return null;
    }

    private void InstallItemWorld()
    {
        var engine = _engine.Engine;
        engine.SetReturnHandler("GetIsObjectValid", a => (uint)a[0]! != 0x7F000000 && ((uint)a[0]! == Player || (uint)a[0]! == Target || _items.ContainsKey((uint)a[0]!)));
        engine.SetReturnHandler("GetResRef", a => _items.TryGetValue((uint)a[0]!, out var info) ? info.Resref : string.Empty);
        engine.SetReturnHandler("GetName", a => _items.TryGetValue((uint)a[0]!, out var info) ? info.Name : "Target");
        engine.SetReturnHandler("GetObjectUUID", a => _items.TryGetValue((uint)a[0]!, out var info) ? info.Uuid : _playerUuid);
        engine.SetReturnHandler("GetBaseItemType", a => _items.TryGetValue((uint)a[0]!, out var info) ? info.Type : BaseItem.Invalid);
        engine.SetReturnHandler("GetWeaponRanged", a => _items.TryGetValue((uint)a[0]!, out var info) && info.Ranged);
        engine.SetReturnHandler("GetItemStackSize", a => _items.TryGetValue((uint)a[0]!, out var info) ? info.Stack : 0);
        engine.SetReturnHandler("GetItemInSlot", a => _held.TryGetValue((InventorySlot)a[0]!, out var held) ? held : 0x7F000000u);
        engine.SetReturnHandler("Get2DAString", a => _twoDa.GetValueOrDefault(((string)a[0]!, (string)a[1]!, (int)a[2]!), string.Empty));
        engine.SetReturnHandler("GetFirstItemInInventory", _ =>
        {
            _inventoryCursor = 0;
            return _inventory.Count > 0 ? _inventory[0] : 0x7F000000u;
        });
        engine.SetReturnHandler("GetNextItemInInventory", _ =>
        {
            _inventoryCursor++;
            return _inventoryCursor < _inventory.Count ? _inventory[_inventoryCursor] : 0x7F000000u;
        });
        engine.SetReturnHandler("GetFirstItemProperty", a =>
        {
            _propertyOwner = (uint)a[0]!;
            _propertyStep = 0;
            return EngineStructures.Create<ItemProperty>();
        });
        engine.SetReturnHandler("GetNextItemProperty", _ =>
        {
            _propertyStep++;
            return EngineStructures.Create<ItemProperty>();
        });
        engine.SetReturnHandler("GetIsItemPropertyValid", _ => _propertyStep == 0 && _withActivation.Contains(_propertyOwner));
        engine.SetReturn("GetItemPropertyType", ItemPropertyType.CastSpell);
        // SetActionMode returns nothing, so the engine fake only records it: the mode is on when the last write said so.
        engine.SetReturnHandler("GetActionMode", a => engine.CallsTo("SetActionMode")
            .LastOrDefault(call => (ActionMode)call.Arguments[1]! == (ActionMode)a[1]!) is { } last && Convert.ToInt32(last.Arguments[2]) != 0);
    }

    private int _inventoryCursor;
    private string _playerUuid = PlayerId;

    private void Add(uint id, string resref, string name, string uuid, BaseItem type, bool ranged = false, int stack = 1, bool inventory = true)
    {
        _items[id] = new ItemInfo(resref, name, uuid, type, ranged, stack);
        if (inventory)
            _inventory.Add(id);
    }

    private void SetEquipableSlots(BaseItem type, string slots) => _twoDa[("baseitems", "EquipableSlots", (int)type)] = slots;

    private Player Stored() => _db.Get<Player>(PlayerId)!;

    // Runs the closures the code handed to the engine, including those queued while running them, in engine order.
    private void RunClosures(string member)
    {
        while (true)
        {
            var next = _engine.Engine.CallsTo(member).Select(call => call.Arguments.OfType<Action>().FirstOrDefault())
                .FirstOrDefault(action => action != null && !_ran.Contains(action));
            if (next == null)
                return;

            _ran.Add(next);
            next();
        }
    }

    private readonly HashSet<Action> _ran = new();

    private static IEnumerable<string> Names(IEnumerable<(string Name, object?[] Arguments)> calls) => calls.Select(call => call.Name);

    // --- constants and mapping ---------------------------------------------------------------------------------------

    [Test]
    public void The_bar_constants_match_the_facade()
    {
        ExtraHotbarsService.BarCount.Should().Be(3);
        ExtraHotbarsService.MaxSlots.Should().Be(12);
        ExtraHotbarsService.BarCount.Should().Be(ExtraHotbarsService.BarCount);
        ExtraHotbarsService.MaxSlots.Should().Be(ExtraHotbarsService.MaxSlots);
    }

    [Test]
    public void Window_types_map_to_bar_indexes_and_back()
    {
        _service.GetWindowType(0).Should().Be(GuiWindowType.ExtraHotbar1);
        _service.GetWindowType(2).Should().Be(GuiWindowType.ExtraHotbar3);
        _service.GetBarIndex(GuiWindowType.ExtraHotbar2).Should().Be(1);
        _service.GetBarIndex(GuiWindowType.Emotes).Should().Be(-1);
    }

    // --- bars ---------------------------------------------------------------------------------------------------------

    [Test]
    public void GetBars_adds_missing_bars_and_slots_and_clamps_the_slot_count()
    {
        var dbPlayer = new Player("other") { ExtraHotbars = new List<ExtraHotbar> { new() { Slots = null!, SlotCount = 99 } } };

        var bars = _service.GetBars(dbPlayer);

        bars.Should().HaveCount(3);
        bars.Should().OnlyContain(bar => bar.Slots.Count == 12);
        bars[0].SlotCount.Should().Be(12);

        dbPlayer.ExtraHotbars = null!;
        _service.GetBars(dbPlayer).Should().HaveCount(3);
    }

    [Test]
    public void GetBar_returns_the_stored_bar_of_a_player_and_null_for_anyone_else()
    {
        _service.GetBar(Player, 1).Should().BeSameAs(Stored().ExtraHotbars[1]);

        _engine.Engine.SetReturn("GetIsDM", true);
        _service.GetBar(Player, 0).Should().BeNull();
        _engine.Engine.SetReturn("GetIsDM", false);
        _engine.Engine.SetReturn("GetIsPC", false);
        _service.GetBar(Player, 0).Should().BeNull();
        _engine.Engine.SetReturn("GetIsPC", true);
        _engine.Engine.SetReturn("GetIsDMPossessed", true);
        _service.GetBar(Player, 0).Should().BeNull();
        _engine.Engine.SetReturn("GetIsDMPossessed", false);
        _playerUuid = " ";
        _service.GetBar(Player, 0).Should().BeNull();
    }

    // --- visibility and locking ------------------------------------------------------------------------------------

    [Test]
    public void Area_entry_opens_visible_bars_and_closes_hidden_open_ones()
    {
        var bars = _service.GetBars(Stored());
        bars[0].IsVisible = true;
        bars[1].IsVisible = false;
        _openWindows.Add(GuiWindowType.ExtraHotbar2);
        _windows[GuiWindowType.ExtraHotbar2] = new GuiPlayerWindow(new ExtraHotbarViewModel());

        _service.OpenBarsOnEnter();

        _guiCalls.Select(call => call.Name).Should().Equal("TogglePlayerWindow", "ClosePlayerWindow");
        _guiCalls[0].Arguments[1].Should().Be(GuiWindowType.ExtraHotbar1);
        _guiCalls[1].Arguments[1].Should().Be(GuiWindowType.ExtraHotbar2);
    }

    [Test]
    public void ApplyVisibility_does_nothing_for_a_player_without_a_record()
    {
        _engine.Engine.SetReturn("GetIsDM", true);

        _service.ApplyVisibility(Player);

        _guiCalls.Should().BeEmpty();
    }

    [Test]
    public void ApplyVisibility_leaves_bars_that_are_already_in_the_right_state()
    {
        _service.GetBars(Stored())[0].IsVisible = true;
        _openWindows.Add(GuiWindowType.ExtraHotbar1);

        _service.ApplyVisibility(Player);

        _guiCalls.Should().BeEmpty();
    }

    [Test]
    public void Locking_on_login_forgets_an_unlock_and_on_exit_saves_the_locked_positions()
    {
        _service.SetUnlocked(Player, true);
        _service.IsUnlocked(Player).Should().BeTrue();

        _service.LockBarsOnEnter();
        _service.IsUnlocked(Player).Should().BeFalse();

        _service.SetUnlocked(Player, true);
        var model = new ExtraHotbarViewModel { Geometry = new GuiRectangle(5, 6, 70, 80) };
        _windows[GuiWindowType.ExtraHotbar1] = new GuiPlayerWindow(model);
        _openWindows.Add(GuiWindowType.ExtraHotbar1);
        _db.Saved.Clear();

        _service.LockBarsOnExit();

        _service.IsUnlocked(Player).Should().BeFalse();
        _db.Saved.Should().ContainSingle();
        Stored().WindowGeometries[GuiWindowType.ExtraHotbar1].Should().BeSameAs(model.Geometry);
    }

    [Test]
    public void Leaving_with_bars_locked_saves_nothing()
    {
        _service.LockBarsOnExit();

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Unlocking_refreshes_the_bars_and_moves_open_ones_off_each_other_and_locking_saves_positions()
    {
        _openWindows.Add(GuiWindowType.ExtraHotbar2);
        _windows[GuiWindowType.ExtraHotbar2] = new GuiPlayerWindow(new ExtraHotbarViewModel());

        _service.SetUnlocked(Player, true);

        _guiCalls.Should().ContainSingle(call => call.Name == "PublishRefreshEvent" && call.Arguments[1] is ExtraHotbarRefreshEvent);

        _guiCalls.Clear();
        _db.Saved.Clear();
        _service.SetUnlocked(Player, false);

        _service.IsUnlocked(Player).Should().BeFalse();
        _db.Saved.Should().ContainSingle();
        Stored().WindowGeometries.Should().ContainKey(GuiWindowType.ExtraHotbar2);
    }

    [Test]
    public void UpdateBar_applies_the_change_clamps_saves_and_redraws()
    {
        _service.UpdateBar(Player, 1, bar =>
        {
            bar.SlotCount = 40;
            bar.IsVisible = true;
        });

        Stored().ExtraHotbars[1].SlotCount.Should().Be(12);
        _db.Saved.Should().ContainSingle();
        Names(_guiCalls).Should().Equal("TogglePlayerWindow", "PublishRefreshEvent");
    }

    [Test]
    public void UpdateBar_ignores_a_player_without_a_record()
    {
        _engine.Engine.SetReturn("GetIsPC", false);
        var changed = false;

        _service.UpdateBar(Player, 0, _ => changed = true);

        changed.Should().BeFalse();
        _db.Saved.Should().BeEmpty();
    }

    // --- assigning slots -------------------------------------------------------------------------------------------

    [Test]
    public void AssignAbility_stores_the_original_feat()
    {
        _service.AssignAbility(Player, 0, 2, FeatType.Alertness);

        var slot = Stored().ExtraHotbars[0].Slots[2];
        slot.Type.Should().Be(ExtraHotbarSlotType.Ability);
        slot.Feat.Should().Be(QuickCast.GetOriginalFeat(FeatType.Alertness));
        Names(_guiCalls).Should().Equal("PublishRefreshEvent");
    }

    [Test]
    public void AssignItem_stores_the_resref_and_name()
    {
        Add(Potion, "potion", "Stim", "u-potion", BaseItem.Potions);

        _service.AssignItem(Player, 0, 0, Potion);

        var slot = Stored().ExtraHotbars[0].Slots[0];
        slot.Type.Should().Be(ExtraHotbarSlotType.Item);
        slot.ItemResref.Should().Be("potion");
        slot.ItemName.Should().Be("Stim");
    }

    [Test]
    public void AssignEquipment_stores_each_hand_and_leaves_an_invalid_one_empty()
    {
        Add(Sword, "sword", "Vibroblade", "u-sword", BaseItem.Longsword);

        _service.AssignEquipment(Player, 0, 1, Sword, 0x7F000000);

        var slot = Stored().ExtraHotbars[0].Slots[1];
        slot.Type.Should().Be(ExtraHotbarSlotType.Equipment);
        slot.MainHand.Should().BeEquivalentTo(new ExtraHotbarEquipItem { ItemId = "u-sword", ItemResref = "sword", ItemName = "Vibroblade" });
        slot.OffHand.Should().BeNull();
    }

    [Test]
    public void AssignChatCommand_trims_the_command()
    {
        _service.AssignChatCommand(Player, 2, 3, "  /emote  ");

        var slot = Stored().ExtraHotbars[2].Slots[3];
        slot.Type.Should().Be(ExtraHotbarSlotType.Command);
        slot.Command.Should().Be("/emote");
    }

    [Test]
    public void AssignActionMode_and_ClearSlot_replace_the_slot()
    {
        _service.AssignActionMode(Player, 0, 0, ActionMode.Stealth);
        Stored().ExtraHotbars[0].Slots[0].ActionMode.Should().Be(ActionMode.Stealth);
        Stored().ExtraHotbars[0].Slots[0].Type.Should().Be(ExtraHotbarSlotType.ActionMode);

        _service.ClearSlot(Player, 0, 0);
        Stored().ExtraHotbars[0].Slots[0].Type.Should().Be(ExtraHotbarSlotType.Empty);
    }

    [Test]
    public void Assigning_for_a_player_without_a_record_changes_nothing()
    {
        _engine.Engine.SetReturn("GetIsDM", true);

        _service.ClearSlot(Player, 0, 0);

        _db.Saved.Should().BeEmpty();
        _guiCalls.Should().BeEmpty();
    }

    // --- using slots -----------------------------------------------------------------------------------------------

    [Test]
    public void UseSlot_runs_the_slot_as_the_player()
    {
        _service.UseSlot(Player, new ExtraHotbarSlot { Type = ExtraHotbarSlotType.Command, Command = "/who" });

        _engine.Engine.CountOf("AssignCommand").Should().Be(1);
        _engine.Engine.CallsTo("AssignCommand")[0].Arguments[0].Should().Be(Player);
        RunClosures("AssignCommand");
        _chatProxy.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(Player, "/who");
    }

    [Test]
    public void An_empty_slot_does_nothing()
    {
        _service.UseSlot(Player, new ExtraHotbarSlot());
        RunClosures("AssignCommand");

        _guiCalls.Should().BeEmpty();
        _engine.Engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void Action_modes_toggle_when_available_and_refuse_when_not()
    {
        _service.UseSlot(Player, new ExtraHotbarSlot { Type = ExtraHotbarSlotType.ActionMode, ActionMode = ActionMode.Detect });
        RunClosures("AssignCommand");
        _engine.Engine.CallsTo("SetActionMode").Should().ContainSingle().Which.Arguments.Take(2).Should().Equal(Player, ActionMode.Detect);
        _engine.Engine.CountOf("DelayCommand").Should().Be(1);

        _service.UseSlot(Player, new ExtraHotbarSlot { Type = ExtraHotbarSlotType.ActionMode, ActionMode = ActionMode.Detect });
        RunClosures("AssignCommand");
        _engine.Engine.CallsTo("SetActionMode").Last().Arguments[2].Should().Match(value => Convert.ToInt32(value) == 0);

        _engine.Engine.ClearCalls();
        _service.UseSlot(Player, new ExtraHotbarSlot { Type = ExtraHotbarSlotType.ActionMode, ActionMode = ActionMode.Stealth });
        RunClosures("AssignCommand");
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("You cannot use Stealth Mode.");
    }

    [Test]
    public void Action_mode_lists_depend_on_the_stealth_perk_and_have_names_and_icons()
    {
        _service.GetAvailableActionModes(Player).Should().Equal(ActionMode.Detect);
        _service.StealthLevel = 1;
        _service.GetAvailableActionModes(Player).Should().Equal(ActionMode.Detect, ActionMode.Stealth);

        _service.GetActionModeName(ActionMode.Detect).Should().Be("Detect Mode");
        _service.GetActionModeName(ActionMode.Stealth).Should().Be("Stealth Mode");
        _service.GetActionModeName(ActionMode.Parry).Should().Be("Parry");
        _service.GetActionModeIcon(ActionMode.Detect).Should().Be("ife_awareness");
        _service.GetActionModeIcon(ActionMode.Stealth).Should().Be("ife_stealth1");
        _service.GetActionModeIcon(ActionMode.Parry).Should().BeEmpty();
    }

    // --- abilities -------------------------------------------------------------------------------------------------

    private static AbilityDetail Ability(string name = "Strike", bool impact = true, AbilityTargetingDetail? targeting = null) =>
        new() { Name = name, ImpactAction = impact ? (_, _, _, _) => { } : null!, Targeting = targeting! };

    [Test]
    public void Known_abilities_are_the_registered_originals_the_player_knows()
    {
        _service.Abilities[FeatType.Alertness] = Ability();
        _service.Abilities[FeatType.Ambidexterity] = Ability(impact: false);
        _service.Abilities[FeatType.Cleave] = Ability();
        _service.Known.Add(FeatType.Alertness);
        _service.Known.Add(FeatType.Ambidexterity);

        _service.GetKnownAbilities(Player).Should().Equal(FeatType.Alertness);
    }

    [Test]
    public void The_ability_icon_comes_from_feat_2da()
    {
        _twoDa[("feat", "ICON", (int)FeatType.Alertness)] = "ife_light";

        _service.GetAbilityIcon(FeatType.Alertness).Should().Be("ife_light");
    }

    private void UseAbilitySlot(FeatType feat)
    {
        _service.UseSlot(Player, new ExtraHotbarSlot { Type = ExtraHotbarSlotType.Ability, Feat = feat });
        RunClosures("AssignCommand");
    }

    [Test]
    public void An_unknown_ability_tells_the_player()
    {
        UseAbilitySlot(FeatType.Alertness);

        _engine.Messages().Should().ContainSingle().Which.Should().Contain("You do not know this ability.");
    }

    [Test]
    public void A_self_targeted_ability_is_used_on_the_player()
    {
        _service.Abilities[FeatType.Ambidexterity] = Ability();
        _service.Known.Add(FeatType.Ambidexterity);
        _twoDa[("feat", "TARGETSELF", (int)FeatType.Ambidexterity)] = "1";

        UseAbilitySlot(FeatType.Ambidexterity);

        _service.Used.Should().ContainSingle().Which.Should().Match<(uint Activator, uint Target, FeatType Feat, Location Location)>(
            used => used.Activator == Player && used.Target == Player && used.Feat == FeatType.Ambidexterity);
    }

    [Test]
    public void A_displayed_alias_is_cast_and_its_error_reported()
    {
        _service.Abilities[FeatType.Alertness] = Ability();
        _service.Known.Add(FeatType.Alertness);
        _service.Displayed[FeatType.Alertness] = FeatType.Ambidexterity;
        _service.CastError = "Out of range.";

        UseAbilitySlot(FeatType.Alertness);

        _service.Casts.Should().ContainSingle();
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("Out of range.");

        _engine.Engine.ClearCalls();
        _service.CastError = " ";
        UseAbilitySlot(FeatType.Alertness);
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void A_targeted_ability_enters_targeting_and_uses_the_chosen_object_or_location()
    {
        var targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Sphere, 5f, 2f, AbilityTargetingFlags.None, null!);
        _service.Abilities[FeatType.Alertness] = Ability("Bolt", targeting: targeting);
        _service.Known.Add(FeatType.Alertness);
        _twoDa[("feat", "SPELLID", (int)FeatType.Alertness)] = "7";
        _twoDa[("spells", "TargetType", 7)] = "0x26";

        UseAbilitySlot(FeatType.Alertness);

        _engine.Engine.CountOf("SetEnterTargetingModeData").Should().Be(1);
        var entered = _service.Targeting.Should().ContainSingle().Subject;
        entered.Message.Should().Be("Choose a target for Bolt.");
        entered.Types.Should().Be(ObjectType.Creature | ObjectType.Placeable | ObjectType.Tile);
        entered.Ground.Should().BeTrue();

        entered.Action(Target);
        RunClosures("AssignCommand");
        _service.Used.Should().ContainSingle(used => used.Target == Target && used.Feat == FeatType.Alertness);

        _service.Used.Clear();
        entered.Action(Area);
        RunClosures("AssignCommand");
        _service.Used.Should().ContainSingle(used => used.Target == 0x7F000000);
    }

    [Test]
    public void An_ability_without_client_targeting_skips_the_targeting_data_and_defaults_to_creatures()
    {
        _service.Abilities[FeatType.Alertness] = Ability("Bolt");
        _service.Known.Add(FeatType.Alertness);

        UseAbilitySlot(FeatType.Alertness);

        _engine.Engine.CountOf("SetEnterTargetingModeData").Should().Be(0);
        _service.Targeting.Should().ContainSingle().Which.Types.Should().Be(ObjectType.Creature);
        _service.Targeting[0].Ground.Should().BeFalse();
    }

    // --- items -----------------------------------------------------------------------------------------------------

    private void UseItemSlot(string resref)
    {
        _service.UseSlot(Player, new ExtraHotbarSlot { Type = ExtraHotbarSlotType.Item, ItemResref = resref });
        RunClosures("AssignCommand");
    }

    [Test]
    public void A_missing_item_and_an_unusable_item_tell_the_player()
    {
        UseItemSlot("potion");
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("You do not have that item.");

        // FindItem only matches items with an activation property, so an unusable item is also "not had".
        Add(Potion, "potion", "Stim", "u-potion", BaseItem.Potions);
        _engine.Engine.ClearCalls();
        UseItemSlot("potion");
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("You do not have that item.");
    }

    [Test]
    public void A_self_targeted_item_is_used_on_the_player_and_refreshes_the_status_soon()
    {
        Add(Potion, "potion", "Stim", "u-potion", BaseItem.Potions);
        _withActivation.Add(Potion);
        _twoDa[("iprp_spells", "SpellIndex", 0)] = "9";
        _twoDa[("spells", "TargetType", 9)] = "0x01";

        UseItemSlot("potion");

        _engine.Engine.CountOf("ActionUseItemOnObject").Should().Be(1);
        _engine.Engine.CallsTo("ActionUseItemOnObject")[0].Arguments[0].Should().Be(Potion);
        _engine.Engine.CountOf("DelayCommand").Should().Be(1);
    }

    [Test]
    public void A_targeted_item_enters_targeting_and_uses_the_chosen_object_or_ground()
    {
        Add(Potion, "potion", "Stim", "u-potion", BaseItem.Potions);
        _withActivation.Add(Potion);
        _twoDa[("iprp_spells", "SpellIndex", 0)] = "9";
        _twoDa[("spells", "TargetType", 9)] = "0x06";
        _engine.Engine.SetReturn("GetItemPossessor", Player);

        UseItemSlot("potion");

        var entered = _service.Targeting.Should().ContainSingle().Subject;
        entered.Message.Should().Be("Choose a target for Stim.");
        entered.Ground.Should().BeTrue();

        entered.Action(Target);
        RunClosures("AssignCommand");
        _engine.Engine.CallsTo("ActionUseItemOnObject").Should().ContainSingle();

        entered.Action(Area);
        RunClosures("AssignCommand");
        _engine.Engine.CallsTo("ActionUseItemAtLocation").Should().ContainSingle();

        _engine.Engine.SetReturn("GetItemPossessor", 0x7F000000u);
        _engine.Engine.ClearCalls();
        entered.Action(Target);
        _engine.Engine.CountOf("ActionUseItemOnObject").Should().Be(0);
    }

    [Test]
    public void Usable_items_are_listed_once_per_resref_and_counted_by_stack()
    {
        Add(Potion, "potion", "Stim", "u1", BaseItem.Potions, stack: 3);
        Add(2010, "potion", "Stim", "u2", BaseItem.Potions, stack: 2);
        Add(2011, "grenade", "Grenade", "u3", BaseItem.Grenade);
        _withActivation.Add(Potion);
        _withActivation.Add(2010);

        _service.GetUsableItems(Player).Should().Equal(Potion);
        _service.FindItem(Player, "potion").Should().Be(Potion);
        _service.FindItem(Player, "grenade").Should().Be(0x7F000000);
        _service.CountItems(Player, "potion").Should().Be(5);
        _service.CountItems(Player, "nothing").Should().Be(0);
    }

    // --- equipment -------------------------------------------------------------------------------------------------

    private void SetUpHands()
    {
        SetEquipableSlots(BaseItem.Longsword, "0x30");
        SetEquipableSlots(BaseItem.Pistol, "0x30");
        SetEquipableSlots(BaseItem.SmallShield, "0x20");
        SetEquipableSlots(BaseItem.Rifle, "0x10");
        Add(Sword, "sword", "Vibroblade", "u-sword", BaseItem.Longsword);
        Add(Pistol, "pistol", "Blaster", "u-pistol", BaseItem.Pistol, ranged: true);
        Add(Shield, "shield", "Buckler", "u-shield", BaseItem.SmallShield);
        Add(Rifle, "rifle", "Rifle", "u-rifle", BaseItem.Rifle, ranged: true);
    }

    private static ExtraHotbarEquipItem Equip(string id, string resref, string name) => new() { ItemId = id, ItemResref = resref, ItemName = name };

    private void UseEquipmentSlot(ExtraHotbarEquipItem? main, ExtraHotbarEquipItem? off)
    {
        _service.UseSlot(Player, new ExtraHotbarSlot { Type = ExtraHotbarSlotType.Equipment, MainHand = main!, OffHand = off! });
        RunClosures("AssignCommand");
    }

    [Test]
    public void Hand_checks_follow_the_equipable_slot_bits()
    {
        SetUpHands();

        _service.CanHoldInMainHand(Sword).Should().BeTrue();
        _service.CanHoldInMainHand(Shield).Should().BeFalse();
        _service.CanHoldInOffHand(Sword).Should().BeTrue();
        _service.CanHoldInOffHand(Shield).Should().BeTrue();
        _service.CanHoldInOffHand(Rifle).Should().BeFalse();
        _service.CanHoldInOffHand(Pistol).Should().BeFalse("a ranged weapon fills both hands");
    }

    [Test]
    public void Items_pair_by_the_hand_rules()
    {
        SetUpHands();
        _items[2020] = new ItemInfo("companion", "Companion", "u-c", BaseItem.CompanionPistol);

        _service.CanPairHands(Sword, 0x7F000000).Should().BeTrue();
        _service.CanPairHands(Sword, 2020).Should().BeTrue("a companion belongs to its main-hand item");
        _service.CanPairHands(Sword, Shield).Should().BeTrue();
        _service.CanPairHands(Rifle, Shield).Should().BeFalse();
        _service.CanPairHands(Pistol, Shield).Should().BeTrue();
        _service.CanPairHands(Pistol, Sword).Should().BeFalse();

        _engine.Engine.SetReturnHandler("GetLocalInt", _ => 1);
        _service.CanPairHands(Pistol, Shield).Should().BeFalse("twin pistols leave no room");
    }

    [Test]
    public void PlanHands_keeps_a_hand_the_set_leaves_out_unless_it_cannot_stay()
    {
        bool Pair(uint a, uint b) => a != 7 || b != 8;

        _service.PlanHands(1, 2, 3, 4, Pair).Should().Be((3u, 4u));
        _service.PlanHands(1, 2, 0x7F000000, 4, Pair).Should().Be((1u, 4u));
        _service.PlanHands(1, 2, 3, 0x7F000000, Pair).Should().Be((3u, 2u));
        _service.PlanHands(1, 1, 0x7F000000, 0x7F000000, Pair).Should().Be((0x7F000000u, 1u));
        _service.PlanHands(7, 8, 0x7F000000, 0x7F000000, Pair).Should().Be((0x7F000000u, 8u));
    }

    [Test]
    public void PickHandItem_follows_the_picker_rules()
    {
        const uint None = 0x7F000000;
        bool Pair(uint a, uint b) => b != 99;

        _service.PickHandItem(1, 2, 1, true, true, Pair).Should().Be((None, 2u));
        _service.PickHandItem(1, 2, 2, true, true, Pair).Should().Be((1u, None));
        _service.PickHandItem(1, 2, 5, false, true, Pair).Should().Be((1u, 5u));
        _service.PickHandItem(1, 2, 99, false, true, Pair).Should().Be((None, 99u));
        _service.PickHandItem(1, 2, 5, false, false, Pair).Should().Be((1u, 2u));
        _service.PickHandItem(1, 2, 5, true, true, Pair).Should().Be((1u, 5u));
        _service.PickHandItem(None, 2, 5, true, true, Pair).Should().Be((5u, 2u));
        _service.PickHandItem(1, 99, 5, true, false, Pair).Should().Be((5u, None));
    }

    [Test]
    public void Hand_items_list_what_is_held_then_the_inventory_and_equipment_is_found_by_id_then_by_look()
    {
        SetUpHands();
        _held[InventorySlot.RightHand] = Sword;
        _inventory.Remove(Sword);

        var items = _service.GetHandItems(Player);
        items.Should().Equal(Sword, Pistol, Shield, Rifle);

        var slot = new ExtraHotbarSlot { MainHand = Equip("u-sword", "x", "x"), OffHand = Equip("gone", "shield", "Buckler") };
        _service.FindEquipItems(Player, slot).Should().Be((Sword, Shield));

        var swapped = new ExtraHotbarSlot { MainHand = Equip("gone", "sword", "Vibroblade"), OffHand = null! };
        _service.FindEquipItems(swapped, items).Should().Be((Sword, 0x7F000000u));

        _service.FindEquipItems(new ExtraHotbarSlot { MainHand = Equip("", "nope", "none") }, items).MainHand.Should().Be(0x7F000000u);
    }

    [Test]
    public void IsEquipped_needs_every_valid_item_in_its_hand()
    {
        SetUpHands();
        _held[InventorySlot.RightHand] = Sword;
        _held[InventorySlot.LeftHand] = Shield;

        _service.IsEquipped(Player, Sword, Shield).Should().BeTrue();
        _service.IsEquipped(Player, Sword, 0x7F000000).Should().BeTrue();
        _service.IsEquipped(Player, Pistol, Shield).Should().BeFalse();
        _service.IsEquipped(Player, 0x7F000000, 0x7F000000).Should().BeFalse();
    }

    [Test]
    public void Equipping_a_set_puts_away_what_is_in_the_way_then_equips_and_resumes_the_attack()
    {
        SetUpHands();
        _held[InventorySlot.RightHand] = Rifle;
        _inventory.Remove(Rifle);
        _engine.Engine.SetReturn("GetAttackTarget", Target);

        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), Equip("u-shield", "shield", "Buckler"));

        _service.PutAway.Should().Equal((Rifle, InventorySlot.RightHand));
        _engine.Engine.CallsTo("ActionEquipItem").Select(call => (uint)call.Arguments[0]!).Should().Equal(Sword, Shield);
        _engine.Engine.CountOf("ClearAllActions").Should().Be(1);
        _engine.Engine.CountOf("ActionAttack").Should().Be(1);
    }

    [Test]
    public void A_second_click_on_a_set_in_hand_takes_it_off()
    {
        SetUpHands();
        _held[InventorySlot.RightHand] = Sword;
        _held[InventorySlot.LeftHand] = Shield;

        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), Equip("u-shield", "shield", "Buckler"));

        _service.PutAway.Should().Equal((Sword, InventorySlot.RightHand), (Shield, InventorySlot.LeftHand));
        _engine.Engine.CountOf("ActionEquipItem").Should().Be(0);
    }

    [Test]
    public void Taking_a_set_off_stops_when_the_main_hand_cannot_go()
    {
        SetUpHands();
        _held[InventorySlot.RightHand] = Sword;
        _held[InventorySlot.LeftHand] = Shield;
        _service.Stuck.Add(InventorySlot.RightHand);

        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), Equip("u-shield", "shield", "Buckler"));

        _service.PutAway.Should().Equal((Sword, InventorySlot.RightHand));
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("Free inventory space").And.Contain("Vibroblade");
    }

    [Test]
    public void A_missing_set_item_is_reported_before_anything_changes()
    {
        SetUpHands();

        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), Equip("u-none", "none", "Lost Item"));

        _engine.Messages().Should().ContainSingle().Which.Should().Contain("You do not have Lost Item.");
        _engine.Engine.CountOf("ClearAllActions").Should().Be(0);
    }

    [Test]
    public void An_equip_error_stops_the_change()
    {
        SetUpHands();
        _itemProxy.On("GetRacialEquipError", _ => "Your species cannot wield that.");

        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), null);

        _engine.Messages().Should().ContainSingle().Which.Should().Contain("Your species cannot wield that.");
        _engine.Engine.CountOf("ActionEquipItem").Should().Be(0);
    }

    [Test]
    public void Crafting_and_force_sensitive_rules_block_equipping()
    {
        SetUpHands();
        _service.CraftOpen = true;

        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), null);
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("Items cannot be equipped while crafting.");

        _service.CraftOpen = false;
        _engine.Engine.ClearCalls();
        Add(2030, "saber", "Lightsaber", "u-saber", BaseItem.Lightsaber);
        SetEquipableSlots(BaseItem.Lightsaber, "0x30");
        _itemProxy.On("CanEquipForceSensitiveWeapon", _ => false);

        UseEquipmentSlot(Equip("u-saber", "saber", "Lightsaber"), null);
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("Only Force Sensitive characters may equip that item.");
    }

    [Test]
    public void A_main_hand_that_cannot_go_keeps_the_set_and_returns_the_off_hand()
    {
        SetUpHands();
        _held[InventorySlot.RightHand] = Rifle;
        _held[InventorySlot.LeftHand] = Shield;
        _inventory.Remove(Rifle);
        _inventory.Remove(Shield);
        _service.Stuck.Add(InventorySlot.RightHand);

        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), Equip("u-pistol", "pistol", "Blaster"));

        _service.PutAway.Select(put => put.Slot).Should().Equal(InventorySlot.LeftHand, InventorySlot.RightHand);
        _engine.Engine.CallsTo("ActionEquipItem").Select(call => (uint)call.Arguments[0]!).Should().Equal(Shield);
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("Free inventory space").And.Contain("Rifle");
    }

    [Test]
    public void An_invalid_operation_while_putting_away_counts_as_done_when_another_rule_moved_the_item()
    {
        SetUpHands();
        _held[InventorySlot.RightHand] = Sword;
        _held[InventorySlot.LeftHand] = Shield;
        _service.PutAwayThrows = true;
        _engine.Engine.SetReturn("GetItemPossessor", Player);

        // The item is still in the slot, so the exception means no room: the player is told and the set stays.
        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), Equip("u-shield", "shield", "Buckler"));

        _engine.Messages().Should().ContainSingle().Which.Should().Contain("Free inventory space");
    }

    [Test]
    public void Companions_leave_with_their_main_hand_item()
    {
        SetUpHands();
        _items[2020] = new ItemInfo("companion", "Companion", "u-c", BaseItem.CompanionPistol);
        SetEquipableSlots(BaseItem.CompanionPistol, "0x20");
        _held[InventorySlot.RightHand] = Pistol;
        _held[InventorySlot.LeftHand] = 2020;
        _inventory.Remove(Pistol);

        UseEquipmentSlot(Equip("u-sword", "sword", "Vibroblade"), null);

        _service.PutAway.Select(put => put.Slot).Should().Contain(InventorySlot.RightHand);
        _engine.Engine.CallsTo("ActionEquipItem").Select(call => (uint)call.Arguments[0]!).Should().Contain(Sword);
    }

    // --- status refresh --------------------------------------------------------------------------------------------

    [Test]
    public void Item_and_equipment_events_refresh_the_status_of_players_only()
    {
        _engine.Engine.SetReturn("GetModuleItemAcquiredBy", Player);
        _engine.Engine.SetReturn("GetModuleItemLostBy", Player);
        _engine.Engine.SetReturn("GetPCItemLastEquippedBy", Player);
        _engine.Engine.SetReturn("GetPCItemLastUnequippedBy", Player);
        _engine.Engine.SetReturn("OBJECT_SELF", Player);

        _service.RefreshOnAcquire();
        _service.RefreshOnUnacquire();
        _service.RefreshOnEquip();
        _service.RefreshOnUnequip();
        _service.RefreshOnHeartbeat();

        _guiCalls.Where(call => call.Arguments[1] is ExtraHotbarStatusRefreshEvent).Should().HaveCount(5);

        _guiCalls.Clear();
        _engine.Engine.SetReturn("GetIsDM", true);
        _service.RefreshOnAcquire();
        _guiCalls.Should().BeEmpty();
    }
}
