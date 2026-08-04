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
    public partial class PlaceableBehaviorSectionViewModel : ObservableObject, IDisposable
    {
        /// <summary>
        /// Unhooks whatever this section subscribed to outside itself - the module index's
        /// "scan finished" event. Set by <c>EditorService</c>; the index outlives every tab, so a
        /// tab that closed without unhooking would be kept alive by it for the session.
        /// </summary>
        public Action? Detach { get; set; }

        public void Dispose()
        {
            Detach?.Invoke();
            Detach = null;
        }

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
            IScriptSlotHost? scriptSlotHost = null,
            Func<string?, IReadOnlyList<string>>? resourceChoices = null)
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

            // The raw .dlg slot. It used to be the only thing on an Advanced tab that appeared for
            // Custom and vanished for everything else - a tab existing to hold one field belonging
            // to one behavior.
            foreach (var descriptor in UtpSchema.CustomBehaviorConversationFields)
            {
                CustomConversationFields.Add(new ResourcePickerFieldViewModel(
                    descriptor,
                    _context,
                    resourceChoices?.Invoke(descriptor.LookupKey) ?? Array.Empty<string>()));
            }

            foreach (var behavior in PlaceableBehaviorCatalog.Behaviors)
            {
                if (!string.IsNullOrEmpty(behavior.Group) && behavior.Group != CurrentGroup)
                {
                    CurrentGroup = behavior.Group;

                    // A heading that repeats the one row under it puts two rows reading "Custom" in
                    // the list, and the heading is the disabled one. A builder aiming at the word
                    // they want has even odds of hitting the half of it that cannot be clicked,
                    // which is indistinguishable from Custom refusing to be chosen.
                    if (!NamesItsOnlyBehavior(behavior))
                        Items.Add(BehaviorListItemViewModel.ForHeader(behavior.Group));
                }

                Items.Add(BehaviorListItemViewModel.ForBehavior(behavior));
            }

            Current = PlaceableBehaviorDetector.Detect(_context.Document.Root);
            _selectedItem = Items.FirstOrDefault(item => ReferenceEquals(item.Behavior, Current));
            BuildFields();
        }

        private string CurrentGroup { get; set; } = string.Empty;

        /// <summary>Whether a group holds exactly one behavior and is named after it.</summary>
        private static bool NamesItsOnlyBehavior(PlaceableBehavior behavior) =>
            string.Equals(behavior.Group, behavior.Name, StringComparison.Ordinal) &&
            PlaceableBehaviorCatalog.Behaviors.Count(candidate =>
                string.Equals(candidate.Group, behavior.Group, StringComparison.Ordinal)) == 1;

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

        /// <summary>The raw .dlg conversation slot, shown only for the Custom behavior.</summary>
        public ObservableCollection<FieldViewModel> CustomConversationFields { get; } = new();

        /// <summary>What the placeable is wired as right now.</summary>
        public PlaceableBehavior Current { get; private set; }

        public string CurrentName => Current.Name;

        /// <summary>True while the raw variable grid should be available; see the Variables tab.</summary>
        public bool AllowsRawEditing => Current.AllowsRawEditing;

        public bool HasFields => Fields.Count > 0;
        public bool HasEditableFlags => EditableFlagFields.Count > 0;
        public bool ShowsCustomFlags => ReferenceEquals(Current, PlaceableBehaviorCatalog.Custom);
        public bool ShowsCustomScripts => ReferenceEquals(Current, PlaceableBehaviorCatalog.Custom);
        public bool ShowsCustomConversation => ReferenceEquals(Current, PlaceableBehaviorCatalog.Custom);
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
                RestoreSelection();
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
            foreach (var field in CustomConversationFields)
                field.RefreshFromDocument();
        }

        /// <summary>
        /// Re-reads every field's options after a module-wide scan lands.
        /// </summary>
        /// <remarks>
        /// The tag and blueprint scans start when the first placeable opens, so the fields on that
        /// first placeable were built against empty lists — which is what makes a choice field fall
        /// back to plain free text with no suggestions and no resolution check. Nothing put the real
        /// options back afterwards, so a Teleporter destination stayed a bare text box for the life
        /// of the tab.
        /// </remarks>
        public void RefreshChoiceSources()
        {
            foreach (var field in Fields)
                field.RefreshOptions();

            NotifyBehaviorProperties();
        }

        /// <summary>
        /// Puts the list's highlight back on the behavior the placeable actually has, after a switch
        /// that was refused or declined.
        /// </summary>
        /// <remarks>
        /// Posted rather than assigned. The refusal is decided inside the notification the list
        /// raised when it was clicked - for the confirm prompt, several turns later - and a list
        /// told to change its selection while it is still processing that click keeps the highlight
        /// the click put there. The pane then names one behavior while the highlight names another,
        /// which reads as the click not registering at all.
        /// </remarks>
        private void RestoreSelection() =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _switching = true;
                SelectedItem = Items.FirstOrDefault(item => ReferenceEquals(item.Behavior, Current));
                _switching = false;
            });

        partial void OnSelectedItemChanged(BehaviorListItemViewModel? value)
        {
            if (_switching)
                return;

            // A heading is not a behavior; put the selection back where it was.
            if (value?.Behavior == null)
            {
                RestoreSelection();
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
                    RestoreSelection();
                    return;
                }
            }

            var previous = Current;
            var applied = _runEdit(
                $"Change behavior to {target.Name}",
                () => PlaceableBehaviorApplier.Apply(_context.Document.Root, previous, target));

            if (!applied)
            {
                RestoreSelection();
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

        /// <summary>Whether a clean document still needs its selected behavior defaults materialized.</summary>
        public bool NeedsSaveNormalization =>
            PlaceableBehaviorApplier.NeedsExpectedValues(_context.Document.Root, Current);

        /// <summary>
        /// Records the successfully saved or reloaded form state. Later behavior changes warn only
        /// for edits made after this point.
        /// </summary>
        public void MarkSavedBaseline() => CaptureBehaviorBaseline();

        /// <summary>
        /// Rebuilds module-backed choice rows after the background tag/blueprint index changes.
        /// Stored values are re-read from the document, so a just-resolved tag remains selected.
        /// </summary>
        public void RefreshValueSources()
        {
            _sources.InvalidateModuleSources();
            BuildFields();
        }

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
            foreach (var field in CustomConversationFields)
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
            OnPropertyChanged(nameof(ShowsCustomConversation));
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
