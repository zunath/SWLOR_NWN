using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Sounds;

namespace SWLOR.Toolset.Editors.Sounds
{
    /// <summary>One scalar or Sounds-list row in the ambient-sound editor.</summary>
    public sealed partial class SoundRowViewModel : ObservableObject
    {
        private readonly SoundValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action _changed;
        private bool _loading;

        public BehaviorFieldDefinition Definition { get; }

        public string Label => Definition.Label;

        public bool IsRequired => Definition.IsRequired;

        public bool IsReadOnly => Definition.IsReadOnly;

        public string? Note => Definition.Note;

        public bool HasNote => !string.IsNullOrEmpty(Note);

        public int MaxLength => Definition.MaxLength;

        public string? Counter => MaxLength > 0 ? $"{Text.Length}/{MaxLength}" : null;

        public bool HasCounter => MaxLength > 0;

        public IReadOnlyList<BehaviorChoice> Choices { get; }

        public bool IsTextEntry =>
            Definition.Kind is BehaviorFieldKind.Text or BehaviorFieldKind.LocalizedText;

        public bool IsNumber =>
            Definition.Kind is BehaviorFieldKind.Integer or BehaviorFieldKind.Float;

        public bool IsFloat => Definition.Kind == BehaviorFieldKind.Float;

        public bool IsCheck => Definition.Kind == BehaviorFieldKind.Check;

        public bool IsChoice => Definition.Kind == BehaviorFieldKind.Choice;

        public bool IsStatement => Definition.Kind == BehaviorFieldKind.Statement;

        public bool IsSoundList => Definition.Kind == BehaviorFieldKind.SoundList;

        public bool HasValue =>
            !IsRequired
            || IsSoundList && SoundList is { HasValidCount: true }
            || IsTextEntry && !string.IsNullOrWhiteSpace(Text)
            || !IsSoundList && !IsTextEntry;

        public SoundListEditorViewModel? SoundList { get; }

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private decimal _number;

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private BehaviorChoice? _choice;

        [ObservableProperty]
        private string _statementText = string.Empty;

        public SoundRowViewModel(
            BehaviorFieldDefinition definition,
            SoundValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoice> choices,
            IReadOnlyList<string> audioResources,
            Action changed)
        {
            Definition = definition;
            _store = store;
            _runEdit = runEdit;
            Choices = choices;
            _changed = changed;

            if (IsSoundList)
            {
                SoundList = new SoundListEditorViewModel(
                    store, runEdit, audioResources, definition.MaxItems, OnListChanged);
            }

            Reload();
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                switch (Definition.Kind)
                {
                    case BehaviorFieldKind.Statement:
                        StatementText = _store.GetInteger(Definition.Storage, Definition.Name)?.ToString()
                                        ?? string.Empty;
                        break;
                    case BehaviorFieldKind.SoundList:
                        SoundList?.Reload();
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
                        var value = _store.GetInteger(Definition.Storage, Definition.Name);
                        Choice = Choices.FirstOrDefault(option => option.Value == value);
                        break;
                    case BehaviorFieldKind.LocalizedText:
                        Text = _store.GetLocalizedText(Definition.Name);
                        break;
                    default:
                        Text = _store.GetString(Definition.Storage, Definition.Name);
                        break;
                }
            }
            finally
            {
                _loading = false;
            }

            OnPropertyChanged(nameof(Counter));
            OnPropertyChanged(nameof(HasValue));
        }

        partial void OnTextChanged(string value)
        {
            if (_loading || IsReadOnly)
                return;

            var applied = Definition.Kind == BehaviorFieldKind.LocalizedText
                ? _runEdit($"Change {Label}", () => _store.SetLocalizedText(Definition.Name, value))
                : _runEdit(
                    $"Change {Label}",
                    () => _store.SetString(Definition.Storage, Definition.Name, Definition.FieldType, value));

            if (!applied)
                Reload();
            else
                _changed();

            OnPropertyChanged(nameof(Counter));
            OnPropertyChanged(nameof(HasValue));
        }

        partial void OnNumberChanged(decimal value)
        {
            if (_loading || IsReadOnly)
                return;

            var applied = IsFloat
                ? _runEdit(
                    $"Change {Label}",
                    () => _store.SetFloat(Definition.Storage, Definition.Name, (double)value))
                : _runEdit(
                    $"Change {Label}",
                    () => _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, (long)value));

            if (!applied)
                Reload();
            else
                _changed();
        }

        partial void OnIsCheckedChanged(bool value)
        {
            if (_loading || IsReadOnly)
                return;

            if (!_runEdit(
                    $"Toggle {Label}",
                    () => _store.SetInteger(
                        Definition.Storage, Definition.Name, Definition.FieldType, value ? 1 : 0)))
                Reload();
            else
                _changed();
        }

        partial void OnChoiceChanged(BehaviorChoice? value)
        {
            if (_loading || IsReadOnly || value == null)
                return;

            if (!_runEdit(
                    $"Change {Label}",
                    () => _store.SetInteger(
                        Definition.Storage, Definition.Name, Definition.FieldType, value.Value)))
                Reload();
            else
                _changed();
        }

        private void OnListChanged()
        {
            OnPropertyChanged(nameof(HasValue));
            _changed();
        }
    }
}
