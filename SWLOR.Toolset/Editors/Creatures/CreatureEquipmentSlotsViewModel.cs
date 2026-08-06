using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>
    /// Ordinary UTC equipment slots plus the creature's natural-weapon equipment. These exist
    /// independently of whether the selected creature model renders the equipped item.
    /// </summary>
    public sealed class CreatureEquipmentSlotsViewModel : ObservableObject
    {
        public ObservableCollection<CreatureEquipmentPickerViewModel> Slots { get; } = new();
        public ObservableCollection<CreatureWeaponViewModel> NaturalWeapons { get; } = new();

        private CreatureEquipmentPickerViewModel? _selectedSlot;

        /// <summary>
        /// The one equipment slot whose paged gallery is on screen. Untouched slots remain deferred;
        /// slots already visited retain only the pages the builder actually browsed.
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

                if (SetProperty(ref _selectedSlot, value) && value != null)
                    _ = value.ActivateAsync();
            }
        }

        public CreatureEquipmentSlotsViewModel(
            CreatureValueStore store,
            CreatureEquipmentSet equipment,
            Func<string, Action, bool> runEdit,
            Func<Task<IReadOnlyList<CreatureEquipmentChoice>>> allChoices,
            Func<string, CreatureEquipmentChoice?> loadDetails,
            Action changed,
            ChoicePreviewService? previews = null,
            Func<string, int, int, int, Task<IReadOnlyList<CreatureEquipmentChoice>>>? searchChoices = null)
        {
            // Equip_ItemList struct ids are Aurora's equipment bit values, not InventorySlot ordinals.
            Slots.Add(Picker("Armor", 2, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Helmet", 1, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Cloak", 64, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Right Hand", 16, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Left Hand", 32, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Boots", 4, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Arms", 8, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Neck", 512, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Belt", 1024, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Left Ring", 128, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Right Ring", 256, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Arrows", 2048, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Bolts", 8192, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            Slots.Add(Picker("Bullets", 4096, store, runEdit, allChoices, loadDetails, changed, previews, searchChoices));
            NaturalWeapons.Add(new CreatureWeaponViewModel(
                "Primary Natural Weapon", CreaturePropertyCatalog.MainWeaponSlot, equipment, runEdit));
            NaturalWeapons.Add(new CreatureWeaponViewModel(
                "Secondary Natural Weapon", CreaturePropertyCatalog.OffWeaponSlot, equipment, runEdit));
            NaturalWeapons.Add(new CreatureWeaponViewModel(
                "Additional Natural Weapon", CreaturePropertyCatalog.CreatureWeaponSlot, equipment, runEdit));
            SetProperty(ref _selectedSlot, Slots[0]);
        }

        /// <summary>Activates the visible slot when the owning Equipment tab becomes visible.</summary>
        public void ActivateSelected()
        {
            if (SelectedSlot != null)
                _ = SelectedSlot.ActivateAsync();
        }

        public void Reload()
        {
            foreach (var slot in Slots)
                slot.Reload();
            foreach (var weapon in NaturalWeapons)
                weapon.Reload();
            if (SelectedSlot != null)
                _ = SelectedSlot.ActivateAsync(force: true);
        }

        private static CreatureEquipmentPickerViewModel Picker(
            string label,
            int slot,
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Func<Task<IReadOnlyList<CreatureEquipmentChoice>>> allChoices,
            Func<string, CreatureEquipmentChoice?> loadDetails,
            Action changed,
            ChoicePreviewService? previews,
            Func<string, int, int, int, Task<IReadOnlyList<CreatureEquipmentChoice>>>? searchChoices)
        {
            async Task<IReadOnlyList<CreatureEquipmentChoice>> Filtered() =>
                (await allChoices().ConfigureAwait(true))
                .Where(choice => (choice.EquipableSlots & slot) != 0)
                .ToList();

            Task<IReadOnlyList<CreatureEquipmentChoice>> Search(string query, int skip, int take) =>
                searchChoices?.Invoke(query, slot, skip, take) ??
                Task.FromResult<IReadOnlyList<CreatureEquipmentChoice>>(
                    Array.Empty<CreatureEquipmentChoice>());

            return new CreatureEquipmentPickerViewModel(
                label,
                slot,
                store,
                runEdit,
                searchChoices == null ? Filtered : null,
                loadDetails,
                changed,
                previews,
                searchChoices == null ? null : Search);
        }
    }
}
