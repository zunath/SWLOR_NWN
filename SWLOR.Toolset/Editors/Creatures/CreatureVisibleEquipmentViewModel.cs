using System.Collections.ObjectModel;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Visible armor, hand, helmet and cloak slots for segmented models.</summary>
    public sealed class CreatureVisibleEquipmentViewModel
    {
        public ObservableCollection<CreatureEquipmentPickerViewModel> Slots { get; } = new();

        public CreatureVisibleEquipmentViewModel(
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<CreatureEquipmentChoice> allChoices,
            Action changed)
        {
            Slots.Add(Picker("Armor", 2, new[] { 16 }, store, runEdit, allChoices, changed));
            Slots.Add(Picker("Main Hand", 16, null, store, runEdit, allChoices, changed));
            Slots.Add(Picker("Helmet", 1, new[] { 17 }, store, runEdit, allChoices, changed));
            Slots.Add(Picker("Cloak", 128, new[] { 80 }, store, runEdit, allChoices, changed));
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
            IReadOnlyList<CreatureEquipmentChoice> allChoices,
            Action changed)
        {
            var filtered = baseItems == null
                ? allChoices.Where(choice => choice.BaseItem is not (16 or 17 or 73 or 80) &&
                                             choice.BaseItem is < 69 or > 72).ToList()
                : allChoices.Where(choice => baseItems.Contains(choice.BaseItem)).ToList();
            return new CreatureEquipmentPickerViewModel(
                label, slot, store, runEdit, filtered, changed);
        }
    }
}
