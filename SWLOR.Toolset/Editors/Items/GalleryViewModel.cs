using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The Appearance tab's single picture gallery, for a base item whose model is one flat part
    /// number - ModelType 0's simple items and ModelType 1's helmets/cloaks/shields. Every tile is a
    /// candidate ModelPart1 value whose icon texture actually resolved; picking one writes it.
    /// </summary>
    public sealed partial class GalleryViewModel : ObservableObject
    {
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _appearanceChanged;
        private bool _loading;

        /// <summary>Every offered ModelPart1 value that resolved to real artwork, ascending.</summary>
        public IReadOnlyList<BehaviorChoiceViewModel> Options { get; }

        [ObservableProperty]
        private BehaviorChoiceViewModel? _selected;

        public GalleryViewModel(
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoiceViewModel> options,
            Action? appearanceChanged = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _appearanceChanged = appearanceChanged;

            Reload();
        }

        /// <summary>Re-reads ModelPart1 and marks the matching tile, suppressing write-back.</summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                var stored = ItemAppearanceValues.Read(_store.Item, ItemAppearanceFieldNames.SimplePart) ?? 0;
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

            if (!_runEdit($"Change model to {value.Display}", () =>
                    ItemAppearanceValues.Write(_store, ItemAppearanceFieldNames.SimplePart, checked((int)value.Value))))
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
