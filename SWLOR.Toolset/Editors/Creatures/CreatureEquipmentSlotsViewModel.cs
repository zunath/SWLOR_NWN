using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>
    /// Ordinary UTC equipment slots. These exist independently of whether the selected creature
    /// model renders the equipped item; natural weapons and the internal stat skin remain on Stats.
    /// </summary>
    public sealed class CreatureEquipmentSlotsViewModel : ObservableObject
    {
        public ObservableCollection<CreatureEquipmentPickerViewModel> Slots { get; } = new();

        private CreatureEquipmentPickerViewModel? _selectedSlot;

        /// <summary>
        /// The one equipment slot whose chooser is on screen. Moving to another slot collapses the
        /// old chooser and releases its published result rows, so the tab never accumulates all of
        /// the equipment catalogs at once.
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

                if (SetProperty(ref _selectedSlot, value))
                    value?.Activate();
            }
        }

        public CreatureEquipmentSlotsViewModel(
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Func<Task<IReadOnlyList<CreatureEquipmentChoice>>> allChoices,
            Func<string, CreatureEquipmentChoice?> loadDetails,
            Action changed)
        {
            // Equip_ItemList struct ids are Aurora's equipment bit values, not InventorySlot ordinals.
            Slots.Add(Picker("Armor", 2, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Helmet", 1, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Cloak", 64, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Right Hand", 16, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Left Hand", 32, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Boots", 4, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Arms", 8, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Neck", 512, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Belt", 1024, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Left Ring", 128, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Right Ring", 256, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Arrows", 2048, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Bolts", 8192, store, runEdit, allChoices, loadDetails, changed));
            Slots.Add(Picker("Bullets", 4096, store, runEdit, allChoices, loadDetails, changed));
            SelectedSlot = Slots[0];
        }

        public void Reload()
        {
            foreach (var slot in Slots)
                slot.Reload();
            SelectedSlot?.Activate(force: true);
        }

        private static CreatureEquipmentPickerViewModel Picker(
            string label,
            int slot,
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Func<Task<IReadOnlyList<CreatureEquipmentChoice>>> allChoices,
            Func<string, CreatureEquipmentChoice?> loadDetails,
            Action changed)
        {
            async Task<IReadOnlyList<CreatureEquipmentChoice>> Filtered() =>
                (await allChoices().ConfigureAwait(true))
                .Where(choice => (choice.EquipableSlots & slot) != 0)
                .ToList();

            return new CreatureEquipmentPickerViewModel(
                label, slot, store, runEdit, Filtered, loadDetails, changed);
        }
    }
}
