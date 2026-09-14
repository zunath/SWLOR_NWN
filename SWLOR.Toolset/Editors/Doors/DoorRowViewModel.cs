using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>
    /// One row of the door editor: the shared behavior row plus the two shapes only a door has - an
    /// ordered set of required key items, and the lock/trap consistency messages.
    /// </summary>
    /// <remarks>
    /// The picture gallery the appearance and portrait rows use is the shared row's, not this
    /// class's. It arrived here and in the trigger editor separately; keeping two copies is how the
    /// two came to page and debounce differently.
    /// </remarks>
    public sealed partial class DoorRowViewModel : BehaviorRowViewModel
    {
        private readonly DoorValueStore _store;
        private readonly Func<BehaviorTagScope, string, string?>? _resolveTag;
        private readonly Action<DoorFieldDefinition> _applyDerivedMutation;
        private readonly Action<DoorRowViewModel> _changed;
        private readonly IReadOnlyDictionary<int, string> _knownKeyItems;

        protected override bool SelectsFirstChoiceWhenUnset =>
            Definition.Name != "LinkedToFlags";

        public new DoorFieldDefinition Definition { get; }

        public bool IsMultiChoice => Definition.Kind == BehaviorFieldKind.MultiChoice;

        /// <summary>The statement row prints its own note; the shared note line would repeat it.</summary>
        public override bool HasNote => !IsStatement && base.HasNote;

        public override bool HasValue =>
            IsMultiChoice ? SelectedKeyItems.Count > 0 :
            IsTextEntry || IsParagraph ? !string.IsNullOrWhiteSpace(Text) :
            true;

        /// <summary>Every key item the game code declares, searched by the picker below.</summary>
        public IReadOnlyList<DoorKeyItemViewModel> AvailableKeyItems { get; }

        /// <summary>The filtered slice of <see cref="AvailableKeyItems"/> the picker shows.</summary>
        public ObservableCollection<DoorKeyItemViewModel> MatchingKeyItems { get; } = new();

        public ObservableCollection<DoorKeyItemViewModel> SelectedKeyItems { get; } = new();

        public string KeyItemSearchSummary =>
            MatchingKeyItems.Count == AvailableKeyItems.Count
                ? $"{AvailableKeyItems.Count} key item{(AvailableKeyItems.Count == 1 ? string.Empty : "s")}"
                : MatchingKeyItems.Count == 0
                    ? "No matching key items"
                    : $"{MatchingKeyItems.Count} of {AvailableKeyItems.Count} key items";

        [ObservableProperty]
        private string _keyItemSearchText = string.Empty;

        public DoorRowViewModel(
            DoorFieldDefinition definition,
            DoorValueStore store,
            Func<string, Action, bool> runEdit,
            Func<BehaviorTagScope, string, string?>? resolveTag,
            Action<DoorFieldDefinition> applyDerivedMutation,
            Action<DoorRowViewModel> changed,
            IReadOnlyList<BehaviorChoice>? choices = null,
            IReadOnlyDictionary<int, string>? keyItems = null,
            ChoicePreviewService? previews = null)
            : base(definition, store, runEdit, choices, valueChanged: null, previews)
        {
            Definition = definition;
            _store = store;
            _resolveTag = resolveTag;
            _applyDerivedMutation = applyDerivedMutation;
            _changed = changed;
            _knownKeyItems = keyItems ?? new Dictionary<int, string>();

            AvailableKeyItems = _knownKeyItems
                .Where(entry => entry.Key != 0)
                .OrderBy(entry => entry.Value, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new DoorKeyItemViewModel(entry.Key, $"{entry.Value} ({entry.Key})", true))
                .ToList();

            Reload();
            RebuildMatchingKeyItems();
        }

        protected override void ReadValue()
        {
            switch (Definition.Special)
            {
                case DoorFieldSpecial.SelfClosing:
                    IsChecked = _store.IsSelfClosing;
                    return;
                case DoorFieldSpecial.KeyItemSequence:
                    ReloadKeyItems();
                    return;
                default:
                    base.ReadValue();
                    return;
            }
        }

        protected override void WriteText(string value)
        {
            base.WriteText(value);

            if (Definition.NonEmptySetsField != null)
            {
                _store.SetInteger(
                    BehaviorFieldStorage.Field,
                    Definition.NonEmptySetsField,
                    GffFieldType.Byte,
                    string.IsNullOrWhiteSpace(value) ? 0 : 1);
            }

            _applyDerivedMutation(Definition);
        }

        protected override void WriteNumber(decimal value)
        {
            base.WriteNumber(value);
            _applyDerivedMutation(Definition);
        }

        protected override void WriteCheck(bool value)
        {
            if (Definition.Special == DoorFieldSpecial.SelfClosing)
                _store.SetSelfClosing(value);
            else
                base.WriteCheck(value);

            _applyDerivedMutation(Definition);
        }

        protected override void WriteChoice(BehaviorChoiceViewModel value)
        {
            base.WriteChoice(value);
            _applyDerivedMutation(Definition);
        }

        protected override void OnApplied()
        {
            base.OnApplied();
            NotifyValueShapeChanged();
            _changed(this);
        }

        /// <summary>
        /// Everything the door editor can say about one row: whether its tag resolves, whether a
        /// transition names a destination type, and whether its key items are real.
        /// </summary>
        public override void RefreshStatus()
        {
            var messages = new List<string>();
            var good = true;

            if (Definition.Kind == BehaviorFieldKind.TagReference)
            {
                if (!string.IsNullOrWhiteSpace(Text))
                {
                    var scope = Definition.TagScope;
                    if (Definition.Name == "LinkedTo")
                    {
                        scope = _store.GetInteger(
                            BehaviorFieldStorage.Field,
                            "LinkedToFlags") switch
                        {
                            1 => BehaviorTagScope.Door,
                            2 => BehaviorTagScope.Waypoint,
                            _ => BehaviorTagScope.None
                        };
                    }

                    if (scope == BehaviorTagScope.None)
                    {
                        good = false;
                        messages.Add("⚠ destination type is unset; this transition will do nothing");
                    }
                    else
                    {
                        var resolved = _resolveTag?.Invoke(scope, Text);
                        if (resolved != null)
                        {
                            messages.Add($"✓ {resolved}");
                        }
                        else
                        {
                            good = false;
                            messages.Add(scope switch
                            {
                                BehaviorTagScope.Item => "⚠ no item blueprint carries this tag",
                                BehaviorTagScope.Waypoint => "⚠ no waypoint carries this tag",
                                BehaviorTagScope.Door => "⚠ no door carries this tag",
                                _ => "⚠ no door or waypoint carries this tag"
                            });
                        }
                    }
                }
                else if (Definition.TagScope == BehaviorTagScope.Item &&
                         _store.GetInteger(BehaviorFieldStorage.Field, "KeyRequired") == 1)
                {
                    good = false;
                    messages.Add("⚠ a key is required, but no item tag is set");
                }
            }

            if (IsMultiChoice)
            {
                if (SelectedKeyItems.Count == 0)
                {
                    good = false;
                    messages.Add("⚠ choose at least one valid key item");
                }
                else
                {
                    var invalid = SelectedKeyItems.Where(item => !item.IsValid).Select(item => item.Id).ToList();
                    if (invalid.Count > 0)
                    {
                        good = false;
                        messages.Add($"⚠ invalid KeyItemType value{(invalid.Count == 1 ? string.Empty : "s")}: " +
                                     string.Join(", ", invalid));
                    }
                }
            }

            Status = messages.Count == 0 ? null : string.Join(" · ", messages);
            IsStatusGood = good;
        }

        partial void OnKeyItemSearchTextChanged(string value) => RebuildMatchingKeyItems();

        [RelayCommand]
        private void AddKeyItem(DoorKeyItemViewModel? item)
        {
            if (item == null || SelectedKeyItems.Any(selected => selected.Id == item.Id))
                return;

            var ids = SelectedKeyItems.Select(selected => selected.Id).Append(item.Id).ToList();
            if (!RunEditFunc("Add required key item", () => _store.SetRequiredKeyItemIds(ids)))
                return;

            Reload();
            OnApplied();
        }

        [RelayCommand]
        private void RemoveKeyItem(DoorKeyItemViewModel? item)
        {
            if (item == null)
                return;

            var removed = false;
            var ids = new List<int>();
            foreach (var selected in SelectedKeyItems)
            {
                if (!removed && selected.Id == item.Id)
                {
                    removed = true;
                    continue;
                }

                ids.Add(selected.Id);
            }

            if (!RunEditFunc("Remove required key item", () => _store.SetRequiredKeyItemIds(ids)))
                return;

            Reload();
            OnApplied();
        }

        private void ReloadKeyItems()
        {
            SelectedKeyItems.Clear();
            foreach (var id in _store.GetRequiredKeyItemIds())
            {
                var display = "";
                var known = id != 0 && _knownKeyItems.TryGetValue(id, out display);
                SelectedKeyItems.Add(new DoorKeyItemViewModel(
                    id,
                    known ? $"{display} ({id})" : $"Unknown key item ({id})",
                    known));
            }
        }

        /// <summary>
        /// Republishes the key items matching the search box. Bounded like every other searchable
        /// row: 415 key items is a set a builder searches rather than scrolls.
        /// </summary>
        private void RebuildMatchingKeyItems()
        {
            if (!IsMultiChoice)
                return;

            var query = KeyItemSearchText.Trim();
            MatchingKeyItems.Clear();

            var published = 0;
            foreach (var item in AvailableKeyItems)
            {
                if (query.Length > 0 &&
                    !item.Display.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                MatchingKeyItems.Add(item);
                if (++published >= MaxSearchResults)
                    break;
            }

            OnPropertyChanged(nameof(KeyItemSearchSummary));
        }
    }
}
