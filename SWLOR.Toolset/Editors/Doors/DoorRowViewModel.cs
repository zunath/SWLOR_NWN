using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>One editable, composite, or statement row in the door editor.</summary>
    public sealed partial class DoorRowViewModel : ObservableObject
    {
        private readonly DoorValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<BehaviorTagScope, string, string?>? _resolveTag;
        private readonly Action<DoorFieldDefinition> _applyDerivedMutation;
        private readonly Action<DoorRowViewModel> _changed;
        private readonly IReadOnlyDictionary<int, string> _knownKeyItems;
        private bool _loading;

        public DoorFieldDefinition Definition { get; }

        public string Label => Definition.Label;

        public bool IsRequired => Definition.IsRequired;

        public string? Note => Definition.Note;

        public bool HasNote => !IsStatement && !string.IsNullOrWhiteSpace(Note);

        public int MaxLength => Definition.MaxLength;

        public string? Counter => MaxLength > 0 ? $"{Text.Length}/{MaxLength}" : null;

        public bool HasCounter => MaxLength > 0;

        public bool IsTextEntry =>
            Definition.Kind is BehaviorFieldKind.Text or BehaviorFieldKind.Script or
                BehaviorFieldKind.LocalizedText or BehaviorFieldKind.TagReference;

        public bool IsParagraph => Definition.Kind == BehaviorFieldKind.Paragraph;

        public bool IsNumber => Definition.Kind is BehaviorFieldKind.Integer or BehaviorFieldKind.Float;

        public bool IsCheck => Definition.Kind == BehaviorFieldKind.Check;

        public bool IsChoice => Definition.Kind == BehaviorFieldKind.Choice;

        public bool IsStatement => Definition.Kind == BehaviorFieldKind.Statement;

        public bool IsMultiChoice => Definition.Kind == BehaviorFieldKind.MultiChoice;

        public bool IsAppearance => Definition.Special == DoorFieldSpecial.Appearance;

        public bool HasValue =>
            IsMultiChoice ? SelectedKeyItems.Count > 0 :
            IsTextEntry || IsParagraph ? !string.IsNullOrWhiteSpace(Text) :
            true;

        public IReadOnlyList<DoorChoiceViewModel> Choices { get; }

        public IReadOnlyList<DoorAppearanceChoice> AppearanceChoices { get; }

        public IReadOnlyList<DoorKeyItemViewModel> AvailableKeyItems { get; }

        public ObservableCollection<DoorKeyItemViewModel> SelectedKeyItems { get; } = new();

        [ObservableProperty]
        private bool _isVisible = true;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private decimal _number;

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private DoorChoiceViewModel? _choice;

        [ObservableProperty]
        private DoorAppearanceChoice? _appearance;

        [ObservableProperty]
        private DoorKeyItemViewModel? _keyItemToAdd;

        [ObservableProperty]
        private string? _status;

        [ObservableProperty]
        private bool _isStatusGood;

        public DoorRowViewModel(
            DoorFieldDefinition definition,
            DoorValueStore store,
            Func<string, Action, bool> runEdit,
            Func<BehaviorTagScope, string, string?>? resolveTag,
            Action<DoorFieldDefinition> applyDerivedMutation,
            Action<DoorRowViewModel> changed,
            IReadOnlyList<BehaviorChoice>? choices = null,
            IReadOnlyList<DoorAppearanceChoice>? appearances = null,
            IReadOnlyDictionary<int, string>? keyItems = null)
        {
            Definition = definition;
            _store = store;
            _runEdit = runEdit;
            _resolveTag = resolveTag;
            _applyDerivedMutation = applyDerivedMutation;
            _changed = changed;
            _knownKeyItems = keyItems ?? new Dictionary<int, string>();

            Choices = (choices ?? definition.Choices)
                .Select(choice => new DoorChoiceViewModel(choice))
                .ToList();
            AppearanceChoices = appearances ?? Array.Empty<DoorAppearanceChoice>();
            AvailableKeyItems = _knownKeyItems
                .Where(entry => entry.Key != 0)
                .OrderBy(entry => entry.Value, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new DoorKeyItemViewModel(entry.Key, $"{entry.Value} ({entry.Key})", true))
                .ToList();

            Reload();
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                if (Definition.Special == DoorFieldSpecial.SelfClosing)
                {
                    IsChecked = _store.IsSelfClosing;
                }
                else if (Definition.Special == DoorFieldSpecial.Appearance)
                {
                    Appearance = _store.GetAppearance(AppearanceChoices);
                }
                else if (Definition.Special == DoorFieldSpecial.KeyItemSequence)
                {
                    ReloadKeyItems();
                }
                else
                {
                    switch (Definition.Kind)
                    {
                        case BehaviorFieldKind.Statement:
                            break;
                        case BehaviorFieldKind.Check:
                            IsChecked = _store.GetInteger(Definition.Storage, Definition.Name) == 1;
                            break;
                        case BehaviorFieldKind.Integer:
                            Number = _store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                            break;
                        case BehaviorFieldKind.Float:
                            Number = (decimal)(_store.GetFloat(Definition.Storage, Definition.Name) ?? 0);
                            break;
                        case BehaviorFieldKind.Choice:
                            var current = _store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                            Choice = Choices.FirstOrDefault(option => option.Value == current)
                                     ?? Choices.FirstOrDefault();
                            break;
                        case BehaviorFieldKind.LocalizedText:
                            Text = _store.GetLocalizedText(Definition.Name);
                            break;
                        case BehaviorFieldKind.Paragraph
                            when Definition.FieldType == GffFieldType.CExoLocString:
                            Text = _store.GetLocalizedText(Definition.Name);
                            break;
                        default:
                            Text = _store.GetString(Definition.Storage, Definition.Name);
                            break;
                    }
                }
            }
            finally
            {
                _loading = false;
            }

            RefreshStatus();
            OnPropertyChanged(nameof(Counter));
            OnPropertyChanged(nameof(HasValue));
        }

        public void RefreshStatus()
        {
            var messages = new List<string>();
            var good = true;

            if (Definition.Kind == BehaviorFieldKind.TagReference)
            {
                if (!string.IsNullOrWhiteSpace(Text))
                {
                    var resolved = _resolveTag?.Invoke(Definition.TagScope, Text);
                    if (resolved != null)
                        messages.Add($"\u2713 {resolved}");
                    else
                    {
                        good = false;
                        messages.Add(Definition.TagScope switch
                        {
                            BehaviorTagScope.Item => "\u26a0 no item blueprint carries this tag",
                            BehaviorTagScope.Waypoint => "\u26a0 no waypoint carries this tag",
                            _ => "\u26a0 no door or waypoint carries this tag"
                        });
                    }
                }
                else if (Definition.TagScope == BehaviorTagScope.Item &&
                         _store.GetInteger(BehaviorFieldStorage.Field, "KeyRequired") == 1)
                {
                    good = false;
                    messages.Add("\u26a0 a key is required, but no item tag is set");
                }
            }

            if (Definition.Name == "LinkedTo" &&
                !string.IsNullOrWhiteSpace(Text) &&
                _store.GetInteger(BehaviorFieldStorage.Field, "LinkedToFlags") == 0)
            {
                good = false;
                messages.Add("\u26a0 destination type is unset; this transition will do nothing");
            }

            if (IsMultiChoice)
            {
                if (SelectedKeyItems.Count == 0)
                {
                    good = false;
                    messages.Add("\u26a0 choose at least one valid key item");
                }
                else
                {
                    var invalid = SelectedKeyItems.Where(item => !item.IsValid).Select(item => item.Id).ToList();
                    if (invalid.Count > 0)
                    {
                        good = false;
                        messages.Add($"\u26a0 invalid KeyItemType value{(invalid.Count == 1 ? string.Empty : "s")}: " +
                                     string.Join(", ", invalid));
                    }
                }
            }

            Status = messages.Count == 0 ? null : string.Join(" \u00b7 ", messages);
            IsStatusGood = good;
        }

        partial void OnTextChanged(string value)
        {
            if (_loading)
                return;

            var applied = _runEdit($"Change {Label}", () =>
            {
                if (Definition.Kind == BehaviorFieldKind.LocalizedText ||
                    Definition.Kind == BehaviorFieldKind.Paragraph &&
                    Definition.FieldType == GffFieldType.CExoLocString)
                {
                    _store.SetLocalizedText(Definition.Name, value);
                }
                else
                {
                    _store.SetString(Definition.Storage, Definition.Name, Definition.FieldType, value);
                }

                if (Definition.NonEmptySetsField != null)
                {
                    _store.SetInteger(
                        BehaviorFieldStorage.Field,
                        Definition.NonEmptySetsField,
                        GffFieldType.Byte,
                        string.IsNullOrWhiteSpace(value) ? 0 : 1);
                }

                _applyDerivedMutation(Definition);
            });

            if (!applied)
                Reload();
            else
                NotifyChanged();
        }

        partial void OnNumberChanged(decimal value)
        {
            if (_loading)
                return;

            var applied = _runEdit($"Change {Label}", () =>
            {
                if (Definition.Kind == BehaviorFieldKind.Float)
                    _store.SetFloat(Definition.Storage, Definition.Name, (double)value);
                else
                    _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, (long)value);

                _applyDerivedMutation(Definition);
            });

            if (!applied)
                Reload();
            else
                NotifyChanged();
        }

        partial void OnIsCheckedChanged(bool value)
        {
            if (_loading)
                return;

            var applied = _runEdit($"Toggle {Label}", () =>
            {
                if (Definition.Special == DoorFieldSpecial.SelfClosing)
                    _store.SetSelfClosing(value);
                else
                    _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, value ? 1 : 0);

                _applyDerivedMutation(Definition);
            });

            if (!applied)
                Reload();
            else
                NotifyChanged();
        }

        partial void OnChoiceChanged(DoorChoiceViewModel? value)
        {
            if (_loading || value == null)
                return;

            if (!_runEdit($"Change {Label}", () =>
                {
                    _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, value.Value);
                    _applyDerivedMutation(Definition);
                }))
            {
                Reload();
            }
            else
            {
                NotifyChanged();
            }
        }

        partial void OnAppearanceChanged(DoorAppearanceChoice? value)
        {
            if (_loading || value == null)
                return;

            if (!_runEdit($"Change appearance to {value.Display}", () => _store.SetAppearance(value)))
                Reload();
            else
                NotifyChanged();
        }

        [RelayCommand]
        private void AddKeyItem()
        {
            if (KeyItemToAdd == null || SelectedKeyItems.Any(item => item.Id == KeyItemToAdd.Id))
                return;

            var ids = SelectedKeyItems.Select(item => item.Id).Append(KeyItemToAdd.Id).ToList();
            if (!_runEdit("Add required key item", () => _store.SetRequiredKeyItemIds(ids)))
                return;

            KeyItemToAdd = null;
            Reload();
            NotifyChanged();
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

            if (!_runEdit("Remove required key item", () => _store.SetRequiredKeyItemIds(ids)))
                return;

            Reload();
            NotifyChanged();
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

        private void NotifyChanged()
        {
            RefreshStatus();
            OnPropertyChanged(nameof(Counter));
            OnPropertyChanged(nameof(HasValue));
            _changed(this);
        }
    }
}
