using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Items;

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
        private readonly Func<string, CreatureEquipmentChoice?> _loadDetails;
        private string? _selectedStatsResRef;

        private IReadOnlyList<ItemStatSummaryGroup> _selectedStatGroups =
            Array.Empty<ItemStatSummaryGroup>();

        public IReadOnlyList<ItemStatSummaryGroup> SelectedStatGroups
        {
            get => _selectedStatGroups;
            private set
            {
                if (ReferenceEquals(_selectedStatGroups, value))
                    return;
                _selectedStatGroups = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedStats));
                OnPropertyChanged(nameof(ShowsSelectedStatsStatus));
                OnPropertyChanged(nameof(SelectedStatsStatus));
            }
        }

        public bool HasSelectedItem =>
            !string.IsNullOrWhiteSpace(_creatureStore.EquippedResRef(_slot));

        public bool HasSelectedStats => SelectedStatGroups.Count > 0;

        public bool ShowsSelectedStatsStatus => HasSelectedItem && !HasSelectedStats;

        public string SelectedStatsStatus =>
            HasSelectedItem && !HasSelectedStats ? "This item has no gameplay stats." : string.Empty;

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
            Func<string, CreatureEquipmentChoice?> loadDetails,
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
                    .Select(choice => new BehaviorChoice(choice.ResRef, choice.Display)
                    {
                        Summary = choice.StatSummary
                    })
                    .ToList())
        {
            _slot = slot;
            _creatureStore = store;
            _loadDetails = loadDetails ?? throw new ArgumentNullException(nameof(loadDetails));
            Reload();
        }

        protected override void ReadValue()
        {
            var resRef = _creatureStore.EquippedResRef(_slot);
            Choice = Choices.FirstOrDefault(choice =>
                string.Equals(choice.StringValue, resRef, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Loads details only for the slot currently visible in the equipment pane.</summary>
        public void Activate(bool force = false) =>
            RefreshSelectedStats(_creatureStore.EquippedResRef(_slot), force);

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

        protected override void OnApplied()
        {
            RefreshSelectedStats(_creatureStore.EquippedResRef(_slot));
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(ShowsSelectedStatsStatus));
            base.OnApplied();
        }

        private void RefreshSelectedStats(string? resRef, bool force = false)
        {
            if (!force && string.Equals(_selectedStatsResRef, resRef, StringComparison.OrdinalIgnoreCase))
                return;

            _selectedStatsResRef = resRef;
            SelectedStatGroups = string.IsNullOrWhiteSpace(resRef)
                ? Array.Empty<ItemStatSummaryGroup>()
                : _loadDetails(resRef)?.Stats ?? Array.Empty<ItemStatSummaryGroup>();
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(ShowsSelectedStatsStatus));
        }
    }
}
