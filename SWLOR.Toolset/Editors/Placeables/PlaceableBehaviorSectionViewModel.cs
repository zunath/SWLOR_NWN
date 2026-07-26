using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.Placeables;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// The Behavior tab: every behavior listed down the left, the selected one's typed fields on the
    /// right, and a note of the script slots and flags it manages.
    /// </summary>
    /// <remarks>
    /// The list is deliberately all on screen rather than behind a picker. Choosing what a placeable
    /// does is the decision the tab exists for, and a builder cannot choose from options they have to
    /// go looking for.
    /// </remarks>
    public partial class PlaceableBehaviorSectionViewModel : ObservableObject
    {
        private readonly EditorFieldContext _context;
        private readonly BehaviorValueSourceProvider _sources;
        private readonly IEditorPromptService _prompts;

        /// <summary>Applies a behavior switch as one undoable step; false when the edit was refused.</summary>
        private readonly Func<string, Action, bool> _runEdit;

        private bool _switching;

        [ObservableProperty]
        private BehaviorListItemViewModel? _selectedItem;

        public PlaceableBehaviorSectionViewModel(
            EditorFieldContext context,
            BehaviorValueSourceProvider sources,
            IEditorPromptService prompts,
            Func<string, Action, bool> runEdit)
        {
            _context = context;
            _sources = sources;
            _prompts = prompts;
            _runEdit = runEdit;

            foreach (var descriptor in UtpSchema.CustomBehaviorFlagFields)
            {
                CustomFlagFields.Add(descriptor.Kind switch
                {
                    EditorKind.Check => new CheckFieldViewModel(descriptor, _context),
                    _ => throw new InvalidOperationException(
                        $"Custom placeable flag '{descriptor.FieldName}' must be a checkbox.")
                });
            }

            foreach (var behavior in PlaceableBehaviorCatalog.Behaviors)
            {
                if (!string.IsNullOrEmpty(behavior.Group) && behavior.Group != CurrentGroup)
                {
                    CurrentGroup = behavior.Group;
                    Items.Add(BehaviorListItemViewModel.ForHeader(behavior.Group));
                }

                Items.Add(BehaviorListItemViewModel.ForBehavior(behavior));
            }

            Current = PlaceableBehaviorDetector.Detect(_context.Document.Root);
            _selectedItem = Items.FirstOrDefault(item => ReferenceEquals(item.Behavior, Current));
            BuildFields();
        }

        private string CurrentGroup { get; set; } = string.Empty;

        /// <summary>Behavior rows and their group headings, in catalog order.</summary>
        public ObservableCollection<BehaviorListItemViewModel> Items { get; } = new();

        /// <summary>The typed fields of the selected behavior.</summary>
        public ObservableCollection<BehaviorFieldViewModel> Fields { get; } = new();

        /// <summary>Raw root flags shown only for the Custom behavior.</summary>
        public ObservableCollection<FieldViewModel> CustomFlagFields { get; } = new();

        /// <summary>What the placeable is wired as right now.</summary>
        public PlaceableBehavior Current { get; private set; }

        public string CurrentName => Current.Name;

        /// <summary>True while the raw variable grid should be available; see the Variables tab.</summary>
        public bool AllowsRawEditing => Current.AllowsRawEditing;

        /// <summary>One line naming the script slots this behavior writes, or null when it writes none.</summary>
        public string? ManagedScripts => Current.Scripts.Count == 0
            ? null
            : string.Join("  ·  ", Current.Scripts.Select(slot => $"{slot.Key} = {slot.Value}"));

        /// <summary>One line naming the flags this behavior requires, or null when it requires none.</summary>
        public string? ManagedFlags => Current.Flags.Count == 0
            ? null
            : string.Join("  ·  ", Current.Flags.Select(flag => flag.Value ? flag.FieldName : $"not {flag.FieldName}"));

        public string? OwnerFile => Current.OwnerFile;

        public bool HasFields => Fields.Count > 0;
        public bool ShowsCustomFlags => ReferenceEquals(Current, PlaceableBehaviorCatalog.Custom);
        public bool HasSettings => HasFields || ShowsCustomFlags;

        /// <summary>Raised when a behavior switch lands, so the editor can refresh its other tabs.</summary>
        public event Action? BehaviorChanged;

        /// <summary>
        /// Re-reads the document after an edit, undo or redo. A newly chosen Custom or variable-only
        /// behavior has no stored signature until the builder fills it in, so an otherwise blank
        /// document must not immediately snap the selection back to Decor.
        /// </summary>
        public void RefreshFromDocument(bool reclassifyAmbiguousSelection = false)
        {
            var detected = PlaceableBehaviorDetector.Detect(_context.Document.Root);
            if (!reclassifyAmbiguousSelection &&
                ReferenceEquals(detected, PlaceableBehaviorCatalog.None) &&
                IsAmbiguousWithoutConfiguredValues(Current))
            {
                detected = Current;
            }

            if (!ReferenceEquals(detected, Current))
            {
                Current = detected;
                _switching = true;
                SelectedItem = Items.FirstOrDefault(item => ReferenceEquals(item.Behavior, Current));
                _switching = false;
                BuildFields();
                NotifyBehaviorProperties();
                return;
            }

            foreach (var field in Fields)
                field.RefreshFromDocument();
            foreach (var field in CustomFlagFields)
                field.RefreshFromDocument();
        }

        partial void OnSelectedItemChanged(BehaviorListItemViewModel? value)
        {
            if (_switching)
                return;

            // A heading is not a behavior; put the selection back where it was.
            if (value?.Behavior == null)
            {
                _switching = true;
                SelectedItem = Items.FirstOrDefault(item => ReferenceEquals(item.Behavior, Current));
                _switching = false;
                return;
            }

            if (ReferenceEquals(value.Behavior, Current))
                return;

            _ = SwitchToAsync(value.Behavior);
        }

        private async Task SwitchToAsync(PlaceableBehavior target)
        {
            var losses = PlaceableBehaviorApplier.ValuesLostBySwitching(
                _context.Document.Root, Current, target);

            if (losses.Count > 0)
            {
                var confirmed = await _prompts.ConfirmDestructiveAsync(
                    $"Switch to {target.Name}?",
                    $"{Current.Name} stores {string.Join(", ", losses)}. Switching to {target.Name} " +
                    "replaces or clears those values. Everything else on this placeable is left alone.",
                    "Switch").ConfigureAwait(true);

                if (!confirmed)
                {
                    _switching = true;
                    SelectedItem = Items.FirstOrDefault(item => ReferenceEquals(item.Behavior, Current));
                    _switching = false;
                    return;
                }
            }

            var previous = Current;
            var applied = _runEdit(
                $"Change behavior to {target.Name}",
                () => PlaceableBehaviorApplier.Apply(_context.Document.Root, previous, target));

            if (!applied)
            {
                _switching = true;
                SelectedItem = Items.FirstOrDefault(item => ReferenceEquals(item.Behavior, Current));
                _switching = false;
                return;
            }

            Current = target;
            BuildFields();
            NotifyBehaviorProperties();
            BehaviorChanged?.Invoke();
        }

        private void BuildFields()
        {
            Fields.Clear();
            foreach (var field in Current.Fields)
                Fields.Add(new BehaviorFieldViewModel(field, _context, _sources));
            foreach (var field in CustomFlagFields)
                field.RefreshFromDocument();

            OnPropertyChanged(nameof(HasFields));
            OnPropertyChanged(nameof(HasSettings));
        }

        private void NotifyBehaviorProperties()
        {
            OnPropertyChanged(nameof(CurrentName));
            OnPropertyChanged(nameof(ManagedScripts));
            OnPropertyChanged(nameof(ManagedFlags));
            OnPropertyChanged(nameof(OwnerFile));
            OnPropertyChanged(nameof(AllowsRawEditing));
            OnPropertyChanged(nameof(ShowsCustomFlags));
            OnPropertyChanged(nameof(HasSettings));
        }

        private static bool IsAmbiguousWithoutConfiguredValues(PlaceableBehavior behavior) =>
            ReferenceEquals(behavior, PlaceableBehaviorCatalog.Custom) ||
            !behavior.IsSentinel && behavior.Scripts.Count == 0;
    }
}
