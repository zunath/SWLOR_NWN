using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Placeables;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// The Behavior tab: every behavior listed down the left and the selected one's typed fields on
    /// the right. Custom exposes its raw flags and event scripts in the same place.
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

        private JsonGffDocument _behaviorBaseline;
        private bool _switching;

        [ObservableProperty]
        private BehaviorListItemViewModel? _selectedItem;

        public PlaceableBehaviorSectionViewModel(
            EditorFieldContext context,
            BehaviorValueSourceProvider sources,
            IEditorPromptService prompts,
            Func<string, Action, bool> runEdit,
            IScriptSlotHost? scriptSlotHost = null)
        {
            _context = context;
            _sources = sources;
            _prompts = prompts;
            _runEdit = runEdit;
            _behaviorBaseline = CloneDocument();

            foreach (var descriptor in UtpSchema.CustomBehaviorFlagFields)
            {
                CustomFlagFields.Add(descriptor.Kind switch
                {
                    EditorKind.Check => new CheckFieldViewModel(descriptor, _context),
                    _ => throw new InvalidOperationException(
                        $"Custom placeable flag '{descriptor.FieldName}' must be a checkbox.")
                });
            }

            foreach (var descriptor in UtpSchema.CustomBehaviorScriptFields)
                CustomScriptFields.Add(new ScriptFieldViewModel(descriptor, _context, scriptSlotHost));

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

        /// <summary>Root flags this behavior intentionally lets the builder choose.</summary>
        public ObservableCollection<CheckFieldViewModel> EditableFlagFields { get; } = new();

        /// <summary>Raw root flags shown only for the Custom behavior.</summary>
        public ObservableCollection<FieldViewModel> CustomFlagFields { get; } = new();

        /// <summary>Raw event script slots shown only for the Custom behavior.</summary>
        public ObservableCollection<ScriptFieldViewModel> CustomScriptFields { get; } = new();

        /// <summary>What the placeable is wired as right now.</summary>
        public PlaceableBehavior Current { get; private set; }

        public string CurrentName => Current.Name;

        /// <summary>True while the raw variable grid should be available; see the Variables tab.</summary>
        public bool AllowsRawEditing => Current.AllowsRawEditing;

        public bool HasFields => Fields.Count > 0;
        public bool HasEditableFlags => EditableFlagFields.Count > 0;
        public bool ShowsCustomFlags => ReferenceEquals(Current, PlaceableBehaviorCatalog.Custom);
        public bool ShowsCustomScripts => ReferenceEquals(Current, PlaceableBehaviorCatalog.Custom);
        public bool HasSettings => HasFields || HasEditableFlags || ShowsCustomFlags || ShowsCustomScripts;

        /// <summary>Raised when a behavior switch lands, so the editor can refresh its other tabs.</summary>
        public event Action? BehaviorChanged;

        /// <summary>
        /// Re-reads the document after an edit, undo or redo. An explicit choice wins while the
        /// document still matches it: some behaviors share scripts, and variable-only behaviors
        /// have no stored signature until the builder fills in their first setting.
        /// </summary>
        public void RefreshFromDocument(bool reclassifyAmbiguousSelection = false)
        {
            var detected = PlaceableBehaviorDetector.Detect(_context.Document.Root);
            if (!reclassifyAmbiguousSelection &&
                ShouldRetainExplicitSelection(detected))
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
                CaptureBehaviorBaseline();
                return;
            }

            foreach (var field in Fields)
                field.RefreshFromDocument();
            foreach (var field in EditableFlagFields)
                field.RefreshFromDocument();
            foreach (var field in CustomFlagFields)
                field.RefreshFromDocument();
            foreach (var field in CustomScriptFields)
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
            var losses = PlaceableBehaviorApplier.UnsavedValuesLostBySwitching(
                _context.Document.Root,
                _behaviorBaseline.Root,
                Current,
                target);

            if (losses.Count > 0)
            {
                var labels = losses
                    .Select(FriendlyLossName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var confirmed = await _prompts.ConfirmDestructiveAsync(
                    $"Change behavior to {target.Name}?",
                    $"This will discard the values you entered for {JoinNaturally(labels)}.",
                    "Change behavior").ConfigureAwait(true);

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
            CaptureBehaviorBaseline();
            BehaviorChanged?.Invoke();
        }

        /// <summary>
        /// Materializes the selected named behavior's implementation values immediately before the
        /// document is serialized. Authored field values and editable flags, including Decor's
        /// Static choice, are preserved.
        /// </summary>
        public bool EnsureExpectedValuesForSave()
        {
            var applied = _runEdit(
                $"Complete {Current.Name} behavior",
                () => PlaceableBehaviorApplier.EnsureExpectedValues(_context.Document.Root, Current));
            if (applied)
                RefreshFromDocument();

            return applied;
        }

        /// <summary>
        /// Records the successfully saved or reloaded form state. Later behavior changes warn only
        /// for edits made after this point.
        /// </summary>
        public void MarkSavedBaseline() => CaptureBehaviorBaseline();

        private void BuildFields()
        {
            Fields.Clear();
            foreach (var field in Current.Fields.Where(field => field.IsVisible))
                Fields.Add(new BehaviorFieldViewModel(field, _context, _sources));

            EditableFlagFields.Clear();
            foreach (var flag in Current.EditableFlags)
            {
                EditableFlagFields.Add(new CheckFieldViewModel(
                    new FieldDescriptor
                    {
                        Label = flag.Label,
                        FieldName = flag.FieldName,
                        Description = flag.Description,
                        Kind = EditorKind.Check,
                        FieldType = GffFieldType.Byte
                    },
                    _context));
            }

            foreach (var field in CustomFlagFields)
                field.RefreshFromDocument();
            foreach (var field in CustomScriptFields)
                field.RefreshFromDocument();

            OnPropertyChanged(nameof(HasFields));
            OnPropertyChanged(nameof(HasEditableFlags));
            OnPropertyChanged(nameof(HasSettings));
        }

        private void NotifyBehaviorProperties()
        {
            OnPropertyChanged(nameof(CurrentName));
            OnPropertyChanged(nameof(AllowsRawEditing));
            OnPropertyChanged(nameof(HasEditableFlags));
            OnPropertyChanged(nameof(ShowsCustomFlags));
            OnPropertyChanged(nameof(ShowsCustomScripts));
            OnPropertyChanged(nameof(HasSettings));
        }

        private bool ShouldRetainExplicitSelection(PlaceableBehavior detected)
        {
            if (ReferenceEquals(detected, Current))
                return false;

            if (ReferenceEquals(Current, PlaceableBehaviorCatalog.Custom))
            {
                return ReferenceEquals(detected, PlaceableBehaviorCatalog.None) ||
                       ReferenceEquals(detected, PlaceableBehaviorCatalog.Custom);
            }

            if (ReferenceEquals(detected, PlaceableBehaviorCatalog.None) &&
                !Current.IsSentinel &&
                Current.Scripts.Count == 0)
            {
                return true;
            }

            return PlaceableBehaviorDetector.MatchesStoredSignature(
                _context.Document.Root,
                Current);
        }

        private string FriendlyLossName(string loss)
        {
            var field = Current.Fields.FirstOrDefault(candidate =>
                string.Equals(candidate.VariableName, loss, StringComparison.Ordinal));
            if (field != null)
                return field.Label;

            const string ScriptSuffix = " script";
            if (loss.EndsWith(ScriptSuffix, StringComparison.Ordinal))
            {
                var slot = loss[..^ScriptSuffix.Length];
                var descriptor = UtpSchema.CustomBehaviorScriptFields.FirstOrDefault(candidate =>
                    string.Equals(candidate.FieldName, slot, StringComparison.Ordinal));
                if (descriptor != null)
                    return $"{descriptor.Label} script";
            }

            return loss.Replace('_', ' ').ToLowerInvariant();
        }

        private static string JoinNaturally(IReadOnlyList<string> values) => values.Count switch
        {
            0 => "these settings",
            1 => values[0],
            2 => $"{values[0]} and {values[1]}",
            _ => $"{string.Join(", ", values.Take(values.Count - 1))}, and {values[^1]}"
        };

        private JsonGffDocument CloneDocument() =>
            JsonGffDocument.Parse(_context.Document.ToBytes());

        private void CaptureBehaviorBaseline()
        {
            _behaviorBaseline = CloneDocument();
        }
    }
}
