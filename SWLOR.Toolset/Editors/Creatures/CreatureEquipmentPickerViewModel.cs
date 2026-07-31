using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>
    /// A creature equipment slot using the shared deferred, paged behavior picker.
    /// </summary>
    /// <remarks>
    /// This used to maintain a second search implementation which eagerly realized 100 buttons for
    /// every slot when the Appearance tab opened. Keeping the slot-specific GFF write hooks here and
    /// reusing <see cref="BehaviorRowViewModel"/> means the item catalog is resolved only when this
    /// picker is opened, then published in the same 50-row virtualized pages as every other editor.
    /// </remarks>
    public sealed partial class CreatureEquipmentPickerViewModel : BehaviorRowViewModel
    {
        private readonly int _slot;
        private readonly CreatureValueStore _creatureStore;

        public override string SelectedChoiceDisplay =>
            Choice?.Display ?? _creatureStore.EquippedResRef(_slot) ?? "None";

        public override bool CanClearChoice =>
            !string.IsNullOrWhiteSpace(_creatureStore.EquippedResRef(_slot));

        protected override bool SelectsFirstChoiceWhenUnset => false;

        public CreatureEquipmentPickerViewModel(
            string label,
            int slot,
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Func<IReadOnlyList<CreatureEquipmentChoice>> choices,
            Action changed)
            : base(
                new BehaviorFieldDefinition
                {
                    Label = label,
                    Name = $"equipment_{slot}",
                    Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.ResRef,
                    IsSearchable = true
                },
                store,
                runEdit,
                valueChanged: changed,
                choiceLoader: () => choices()
                    .Select(choice => new BehaviorChoice(choice.ResRef, choice.Display))
                    .ToList())
        {
            _slot = slot;
            _creatureStore = store;
            Reload();
        }

        protected override void ReadValue()
        {
            var resRef = _creatureStore.EquippedResRef(_slot);
            Choice = Choices.FirstOrDefault(choice =>
                string.Equals(choice.StringValue, resRef, StringComparison.OrdinalIgnoreCase));
        }

        protected override void WriteChoice(BehaviorChoiceViewModel value) =>
            _creatureStore.SetEquippedResRef(_slot, value.StringValue);

        protected override void ClearChoice()
        {
            if (!RunEditFunc($"Clear {Label}", () => _creatureStore.SetEquippedResRef(_slot, null)))
            {
                Reload();
                return;
            }

            Reload();
            OnApplied();
        }
    }
}
