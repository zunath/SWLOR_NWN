using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One of the three model-color pickers a composite base item (ModelType 2: most weapons,
    /// lightsabers, boots) shows on its Appearance tab. Bottom writes ModelPart1, Middle writes
    /// ModelPart2, and Top writes ModelPart3 - the same bottom/middle/top order
    /// <see cref="Domain.Render.Icons.ItemIconResolver"/> composes the inventory icon in. A tile's
    /// caption is its "model-color" pair (part 14 = model 1 color 4), not the raw stored number.
    /// </summary>
    public sealed partial class CompositePartViewModel : ObservableObject
    {
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly string _fieldName;
        private readonly Action? _appearanceChanged;
        private bool _loading;

        /// <summary>"Bottom", "Middle", or "Top" - which layer this picker writes.</summary>
        public string Label { get; }

        /// <summary>Every part number 0-259 whose layer texture actually resolved, ascending.</summary>
        public IReadOnlyList<BehaviorChoiceViewModel> Options { get; }

        [ObservableProperty]
        private BehaviorChoiceViewModel? _selected;

        public CompositePartViewModel(
            string label,
            string fieldName,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoiceViewModel> options,
            Action? appearanceChanged = null)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _appearanceChanged = appearanceChanged;

            Reload();
        }

        /// <summary>Re-reads this layer's field and marks the matching tile, suppressing write-back.</summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                var stored = ItemAppearanceValues.Read(_store.Item, _fieldName) ?? 0;
                Selected = Options.FirstOrDefault(option => option.Value == stored);
                MarkSelected();
            }
            finally
            {
                _loading = false;
            }
        }

        partial void OnSelectedChanged(BehaviorChoiceViewModel? value)
        {
            if (_loading || value == null)
                return;

            if (!_runEdit($"Change {Label} model", () =>
                    ItemAppearanceValues.Write(_store, _fieldName, checked((int)value.Value))))
            {
                Reload();
                return;
            }

            MarkSelected();
            _appearanceChanged?.Invoke();
        }

        private void MarkSelected()
        {
            foreach (var option in Options)
                option.IsSelected = ReferenceEquals(option, Selected);
        }
    }
}
