using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Waypoints
{
    public sealed partial class WaypointRowViewModel : ObservableObject
    {
        private readonly BehaviorValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private bool _loading;

        public BehaviorFieldDefinition Definition { get; }

        public string Label => Definition.Label;

        public bool IsRequired => Definition.IsRequired;

        public string? Note => Definition.Note;

        public bool HasNote => !string.IsNullOrEmpty(Definition.Note);

        public int MaxLength => Definition.MaxLength;

        public string? Counter => MaxLength > 0 ? $"{Text.Length}/{MaxLength}" : null;

        public bool HasCounter => MaxLength > 0;

        public IReadOnlyList<WaypointChoiceViewModel> Choices { get; }

        public bool IsText => Definition.Kind is BehaviorFieldKind.Text or BehaviorFieldKind.Script;
        public bool IsLocalizedText => Definition.Kind == BehaviorFieldKind.LocalizedText;
        public bool IsParagraph => Definition.Kind == BehaviorFieldKind.Paragraph;
        public bool IsNumber => Definition.Kind is BehaviorFieldKind.Integer or BehaviorFieldKind.Float;
        public bool IsCheck => Definition.Kind == BehaviorFieldKind.Check;
        public bool IsChoice => Definition.Kind == BehaviorFieldKind.Choice;
        public bool IsSearchableChoice => IsChoice && Definition.IsSearchable;
        public bool IsPlainChoice => IsChoice && !Definition.IsSearchable;
        public bool IsStatement => Definition.Kind == BehaviorFieldKind.Statement;
        public bool IsTextEntry => IsText || IsLocalizedText;

        public bool IsEmpty => IsChoice
            ? Choice == null
            : (IsTextEntry || IsParagraph) && string.IsNullOrWhiteSpace(Text);

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private decimal _number;

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private WaypointChoiceViewModel? _choice;

        [ObservableProperty]
        private string _choiceSearchText = string.Empty;

        public WaypointRowViewModel(
            BehaviorFieldDefinition definition,
            BehaviorValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoice>? choices = null,
            Action? valueChanged = null)
        {
            Definition = definition;
            _store = store;
            _runEdit = runEdit;
            _valueChanged = valueChanged;
            Choices = (choices ?? definition.Choices)
                .Select(choice => new WaypointChoiceViewModel(choice))
                .ToList();
            Reload();
        }

        public void Reload()
        {
            if (Definition.Kind == BehaviorFieldKind.Statement)
                return;

            _loading = true;
            try
            {
                switch (Definition.Kind)
                {
                    case BehaviorFieldKind.Check:
                        IsChecked = _store.GetInteger(Definition.Storage, Definition.Name) == 1;
                        break;
                    case BehaviorFieldKind.Integer:
                        Number = _store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                        break;
                    case BehaviorFieldKind.Float:
                        Number = (decimal)(_store.GetFloat(Definition.Storage, Definition.Name) ?? 0);
                        break;
                    case BehaviorFieldKind.Choice when Definition.FieldType is
                        Domain.Gff.GffFieldType.CExoString or Domain.Gff.GffFieldType.ResRef:
                        var currentText = _store.GetString(Definition.Storage, Definition.Name);
                        Choice = Choices.FirstOrDefault(option =>
                            string.Equals(option.StringValue, currentText, StringComparison.Ordinal));
                        break;
                    case BehaviorFieldKind.Choice:
                        var current = _store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                        Choice = Choices.FirstOrDefault(option => option.Value == current)
                                 ?? Choices.FirstOrDefault();
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

            ChoiceSearchText = IsSearchableChoice ? Choice?.Display ?? string.Empty : string.Empty;
            OnPropertyChanged(nameof(Counter));
            OnPropertyChanged(nameof(IsEmpty));
        }

        partial void OnTextChanged(string value)
        {
            if (_loading)
                return;

            var applied = Definition.Kind == BehaviorFieldKind.LocalizedText
                ? _runEdit($"Change {Label}", () => _store.SetLocalizedText(Definition.Name, value))
                : _runEdit(
                    $"Change {Label}",
                    () => _store.SetString(Definition.Storage, Definition.Name, Definition.FieldType, value));

            if (!applied)
                Reload();
            else
                _valueChanged?.Invoke();

            OnPropertyChanged(nameof(Counter));
            OnPropertyChanged(nameof(IsEmpty));
        }

        partial void OnNumberChanged(decimal value)
        {
            if (_loading)
                return;

            var applied = Definition.Kind == BehaviorFieldKind.Float
                ? _runEdit(
                    $"Change {Label}",
                    () => _store.SetFloat(Definition.Storage, Definition.Name, (double)value))
                : _runEdit(
                    $"Change {Label}",
                    () => _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, (long)value));

            if (!applied)
                Reload();
            else
                _valueChanged?.Invoke();
        }

        partial void OnIsCheckedChanged(bool value)
        {
            if (_loading)
                return;

            if (!_runEdit(
                    $"Toggle {Label}",
                    () => _store.SetInteger(
                        Definition.Storage,
                        Definition.Name,
                        Definition.FieldType,
                        value ? 1 : 0)))
                Reload();
            else
                _valueChanged?.Invoke();
        }

        partial void OnChoiceChanged(WaypointChoiceViewModel? value)
        {
            if (_loading || value == null)
                return;

            if (IsSearchableChoice)
                ChoiceSearchText = value.Display;

            var applied = value.Choice.IsStringValue
                ? _runEdit(
                    $"Change {Label}",
                    () => _store.SetString(
                        Definition.Storage,
                        Definition.Name,
                        Definition.FieldType,
                        value.StringValue!))
                : _runEdit(
                    $"Change {Label}",
                    () => _store.SetInteger(
                        Definition.Storage,
                        Definition.Name,
                        Definition.FieldType,
                        value.Value));

            if (!applied)
                Reload();
            else
                _valueChanged?.Invoke();

            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}
