using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Visible armor, hand, helmet and cloak slots for segmented models.</summary>
    public sealed class CreatureVisibleEquipmentViewModel : ObservableObject
    {
        public ObservableCollection<CreatureEquipmentPickerViewModel> Slots { get; } = new();

        private CreatureEquipmentPickerViewModel? _selectedSlot;

        /// <summary>
        /// The one equipment slot whose chooser is on screen. Moving to another slot collapses the
        /// old chooser and releases its published result rows, so the tab can never accumulate four
        /// realized item lists again.
        /// </summary>
        public CreatureEquipmentPickerViewModel? SelectedSlot
        {
            get => _selectedSlot;
            set
            {
                if (ReferenceEquals(_selectedSlot, value))
                    return;

                if (_selectedSlot?.IsSearchExpanded == true)
                    _selectedSlot.CloseSearchCommand.Execute(null);

                SetProperty(ref _selectedSlot, value);
            }
        }

        public CreatureVisibleEquipmentViewModel(
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Func<IReadOnlyList<CreatureEquipmentChoice>> allChoices,
            Action changed)
        {
            Slots.Add(Picker("Armor", 2, new[] { 16 }, store, runEdit, allChoices, changed));
            Slots.Add(Picker("Main Hand", 16, null, store, runEdit, allChoices, changed));
            Slots.Add(Picker("Helmet", 1, new[] { 17 }, store, runEdit, allChoices, changed));
            Slots.Add(Picker("Cloak", 128, new[] { 80 }, store, runEdit, allChoices, changed));
            SelectedSlot = Slots[0];
        }

        public void Reload()
        {
            foreach (var slot in Slots)
                slot.Reload();
        }

        private static CreatureEquipmentPickerViewModel Picker(
            string label,
            int slot,
            IReadOnlyCollection<int>? baseItems,
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Func<IReadOnlyList<CreatureEquipmentChoice>> allChoices,
            Action changed)
        {
            IReadOnlyList<CreatureEquipmentChoice> Filtered()
            {
                var choices = allChoices();
                return baseItems == null
                    ? choices.Where(choice => choice.BaseItem is not (16 or 17 or 73 or 80) &&
                                              choice.BaseItem is < 69 or > 72).ToList()
                    : choices.Where(choice => baseItems.Contains(choice.BaseItem)).ToList();
            }

            return new CreatureEquipmentPickerViewModel(
                label, slot, store, runEdit, Filtered, changed);
        }
    }
}
