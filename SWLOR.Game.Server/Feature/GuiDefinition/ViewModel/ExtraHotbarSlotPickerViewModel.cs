using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ExtraHotbarSlotPickerViewModel : GuiViewModelBase<ExtraHotbarSlotPickerViewModel, ExtraHotbarSlotPickerPayload>
    {
        public const string ContentElement = "extra_hotbar_picker_content";
        public const string MainPartial = "EXTRA_HOTBAR_PICKER_MAIN";

        public const int AbilitiesCategory = 0;
        public const int ItemsCategory = 1;
        public const int CommandsCategory = 2;
        public const int ActionModesCategory = 3;
        public const int EquipmentCategory = 4;

        private sealed record Entry(FeatType Feat, uint Item, string Command, ActionMode ActionMode = ActionMode.Detect);

        private readonly List<Entry> _entries = new();
        private int _barIndex;
        private int _slotIndex;
        private uint _mainHand;
        private uint _offHand;

        public string Title { get => Get<string>(); set => Set(value); }
        public int SelectedCategory { get => Get<int>(); set => Set(value); }
        public string SearchText { get => Get<string>(); set => Set(value); }
        public string CommandText { get => Get<string>(); set => Set(value); }
        public bool IsCommandMode { get => Get<bool>(); set => Set(value); }
        public bool IsAssignVisible { get => Get<bool>(); set => Set(value); }
        public string AssignTooltip { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }

        public GuiBindingList<string> EntryIcons { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> EntryNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> EntryTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }

        protected override void Initialize(ExtraHotbarSlotPickerPayload initialPayload)
        {
            _barIndex = initialPayload.BarIndex;
            _slotIndex = initialPayload.SlotIndex;

            Title = $"Hotbar {_barIndex + 1} - Slot {_slotIndex + 1}";
            SelectedCategory = AbilitiesCategory;
            SearchText = string.Empty;
            CommandText = string.Empty;
            IsCommandMode = false;
            IsAssignVisible = false;
            AssignTooltip = string.Empty;
            _mainHand = OBJECT_INVALID;
            _offHand = OBJECT_INVALID;
            StatusText = string.Empty;
            StatusColor = GuiColor.White;
            EntryIcons = new GuiBindingList<string>();
            EntryNames = new GuiBindingList<string>();
            EntryTooltips = new GuiBindingList<string>();

            WatchOnClient(model => model.SelectedCategory);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.CommandText);

            ChangePartialView(ContentElement, MainPartial);
            LoadEntries();
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (propertyName is nameof(SelectedCategory) or nameof(SearchText))
                LoadEntries();
        }

        private void LoadEntries()
        {
            _entries.Clear();
            var icons = new GuiBindingList<string>();
            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var search = SearchText?.Trim() ?? string.Empty;

            bool Matches(string text) =>
                search.Length == 0 || text.Contains(search, System.StringComparison.OrdinalIgnoreCase);

            IsCommandMode = SelectedCategory == CommandsCategory;
            IsAssignVisible = IsCommandMode || SelectedCategory == EquipmentCategory;
            AssignTooltip = IsCommandMode
                ? "Puts this chat command on the slot."
                : "Puts the chosen main-hand and off-hand items on the slot.";
            SetStatus(SelectedCategory switch
            {
                CommandsCategory => "Pick a command, or type one with arguments below.",
                EquipmentCategory => GetEquipmentStatus(),
                _ => "Click an entry to put it on the slot."
            }, GuiColor.White);
            switch (SelectedCategory)
            {
                case AbilitiesCategory:
                    foreach (var feat in ExtraHotbars.GetKnownAbilities(Player)
                                 .OrderBy(feat => Ability.GetAbilityDetail(feat).Name))
                    {
                        var name = Ability.GetAbilityDetail(feat).Name;
                        if (!Matches(name))
                            continue;

                        _entries.Add(new Entry(feat, OBJECT_INVALID, string.Empty));
                        icons.Add(ExtraHotbars.GetAbilityIcon(feat));
                        names.Add(name);
                        tooltips.Add(name);
                    }
                    break;
                case ItemsCategory:
                    foreach (var item in ExtraHotbars.GetUsableItems(Player).OrderBy(item => GetName(item)))
                    {
                        var name = GetName(item);
                        if (!Matches(name))
                            continue;

                        _entries.Add(new Entry(FeatType.Invalid, item, string.Empty));
                        icons.Add(Item.GetIconResref(item));
                        names.Add(name);
                        tooltips.Add(name);
                    }
                    break;
                case CommandsCategory:
                    foreach (var (name, description) in ChatCommand.GetAvailableCommands(Player))
                    {
                        if (!Matches(name) && !Matches(description))
                            continue;

                        _entries.Add(new Entry(FeatType.Invalid, OBJECT_INVALID, "/" + name));
                        icons.Add(ExtraHotbarViewModel.CommandIcon);
                        names.Add("/" + name);
                        tooltips.Add(description);
                    }
                    break;
                case EquipmentCategory:
                    LoadEquipmentEntries(icons, names, tooltips, Matches);
                    break;
                case ActionModesCategory:
                    foreach (var mode in ExtraHotbars.GetAvailableActionModes(Player))
                    {
                        var name = ExtraHotbars.GetActionModeName(mode);
                        if (!Matches(name))
                            continue;

                        _entries.Add(new Entry(FeatType.Invalid, OBJECT_INVALID, string.Empty, mode));
                        icons.Add(ExtraHotbars.GetActionModeIcon(mode));
                        names.Add(name);
                        tooltips.Add($"Click the slot to turn {name} on or off.");
                    }
                    break;
            }

            EntryIcons = icons;
            EntryNames = names;
            EntryTooltips = tooltips;

            if (_entries.Count == 0)
                SetStatus(SelectedCategory switch
                {
                    ItemsCategory => "You have no usable items.",
                    EquipmentCategory => "You have no weapons or shields.",
                    _ => "Nothing matches your search."
                }, GuiColor.White);
        }

        private void LoadEquipmentEntries(
            GuiBindingList<string> icons,
            GuiBindingList<string> names,
            GuiBindingList<string> tooltips,
            Func<string, bool> matches)
        {
            if (!IsHeld(_mainHand))
                _mainHand = OBJECT_INVALID;
            if (!IsHeld(_offHand))
                _offHand = OBJECT_INVALID;

            // Chosen items stay at the top, whatever the search.
            var items = ExtraHotbars.GetHandItems(Player)
                .Where(item => item == _mainHand || item == _offHand || matches(GetName(item)))
                .OrderBy(item => item == _mainHand ? 0 : item == _offHand ? 1 : 2)
                .ThenBy(item => GetName(item));
            foreach (var item in items)
            {
                var name = GetName(item);
                if (item == _mainHand)
                    name = "[Main Hand] " + name;
                else if (item == _offHand)
                    name = "[Off Hand] " + name;
                if (Item.GetItemSlot(Player, item) != InventorySlot.Invalid)
                    name += " (held)";

                _entries.Add(new Entry(FeatType.Invalid, item, string.Empty));
                icons.Add(Item.GetIconResref(item));
                names.Add(name);
                tooltips.Add(item == _mainHand || item == _offHand
                    ? "Click to take this item out of the set."
                    : ExtraHotbars.CanHoldInMainHand(item)
                        ? "Click to choose this item for your main hand, or your off hand once a main hand is chosen."
                        : "Click to choose this item for your off hand.");
            }
        }

        private bool IsHeld(uint item) => GetIsObjectValid(item) && GetItemPossessor(item) == Player;

        private string GetEquipmentStatus()
        {
            return _mainHand == OBJECT_INVALID && _offHand == OBJECT_INVALID
                ? "Click a weapon, then a second weapon or shield."
                : "Click Assign to save this set.";
        }

        public Action OnClickEntry() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _entries.Count)
                return;

            var entry = _entries[index];
            switch (SelectedCategory)
            {
                case AbilitiesCategory:
                    ExtraHotbars.AssignAbility(Player, _barIndex, _slotIndex, entry.Feat);
                    Gui.ClosePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker);
                    break;
                case ItemsCategory:
                    if (!GetIsObjectValid(entry.Item) || GetItemPossessor(entry.Item) != Player)
                    {
                        LoadEntries();
                        SetStatus("You no longer have that item.", GuiColor.Red);
                        return;
                    }

                    ExtraHotbars.AssignItem(Player, _barIndex, _slotIndex, entry.Item);
                    Gui.ClosePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker);
                    break;
                case CommandsCategory:
                    ExtraHotbars.AssignChatCommand(Player, _barIndex, _slotIndex, entry.Command);
                    Gui.ClosePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker);
                    break;
                case EquipmentCategory:
                    if (!IsHeld(entry.Item))
                    {
                        LoadEntries();
                        SetStatus("You no longer have that item.", GuiColor.Red);
                        return;
                    }

                    (_mainHand, _offHand) = ExtraHotbars.PickHandItem(_mainHand, _offHand, entry.Item,
                        ExtraHotbars.CanHoldInMainHand(entry.Item),
                        ExtraHotbars.CanHoldInOffHand(entry.Item),
                        ExtraHotbars.FillsBothHands(entry.Item),
                        GetIsObjectValid(_mainHand) && ExtraHotbars.FillsBothHands(_mainHand));
                    LoadEntries();
                    break;
                case ActionModesCategory:
                    ExtraHotbars.AssignActionMode(Player, _barIndex, _slotIndex, entry.ActionMode);
                    Gui.ClosePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker);
                    break;
            }
        };

        public Action OnClickAssign() => () =>
        {
            if (SelectedCategory == EquipmentCategory)
            {
                AssignEquipment();
                return;
            }

            var command = CommandText?.Trim() ?? string.Empty;
            if (command.Length < 2 || command[0] != '/')
            {
                SetStatus("Chat commands start with a slash, like /sit.", GuiColor.Red);
                return;
            }

            var name = command.Substring(1).Split(' ')[0].ToLower();
            if (ChatCommand.GetAvailableCommands(Player).All(available => available.Name != name))
            {
                SetStatus($"There is no /{name} command you can use.", GuiColor.Red);
                return;
            }

            ExtraHotbars.AssignChatCommand(Player, _barIndex, _slotIndex, command);
            Gui.ClosePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker);
        };

        private void AssignEquipment()
        {
            if (!IsHeld(_mainHand) && !IsHeld(_offHand))
            {
                LoadEntries();
                SetStatus("Choose at least one weapon or shield first.", GuiColor.Red);
                return;
            }

            ExtraHotbars.AssignEquipment(Player, _barIndex, _slotIndex,
                IsHeld(_mainHand) ? _mainHand : OBJECT_INVALID,
                IsHeld(_offHand) ? _offHand : OBJECT_INVALID);
            Gui.ClosePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker);
        }

        public Action OnClickClear() => () =>
        {
            ExtraHotbars.ClearSlot(Player, _barIndex, _slotIndex);
            Gui.ClosePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker);
        };

        private void SetStatus(string text, GuiColor color)
        {
            StatusText = text;
            StatusColor = color;
        }
    }
}
