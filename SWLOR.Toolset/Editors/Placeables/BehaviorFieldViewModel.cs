using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Placeables;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// One typed control over a behavior's local variable. Reads and writes the VarTable through
    /// the editor's transaction pipeline, so a behavior field undoes exactly like a GFF field.
    /// </summary>
    /// <remarks>
    /// Clearing a value removes the variable rather than storing an empty string. An unset local and
    /// a local set to "" mean the same thing to the game, and leaving the row behind makes a
    /// placeable look configured when it is not.
    /// </remarks>
    public partial class BehaviorFieldViewModel : ObservableObject
    {
        private readonly EditorFieldContext _context;
        private readonly PlaceableBehaviorField _field;
        private readonly BehaviorValueSourceProvider _sources;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private long _number;

        [ObservableProperty]
        private bool _flag;

        [ObservableProperty]
        private BehaviorChoiceOption? _selectedOption;

        [ObservableProperty]
        private BehaviorValueStatus _status;

        [ObservableProperty]
        private string? _statusText;

        public BehaviorFieldViewModel(
            PlaceableBehaviorField field,
            EditorFieldContext context,
            BehaviorValueSourceProvider sources)
        {
            _field = field;
            _context = context;
            _sources = sources;
            Options = sources.GetOptions(field.Source);

            RefreshFromDocument();
        }

        public string Label => _field.Label;
        public string VariableName => _field.VariableName;
        public string? Description => _field.Description;
        public bool IsRequired => _field.IsRequired;

        public IReadOnlyList<BehaviorChoiceOption> Options { get; }

        public bool IsToggle => _field.Kind == PlaceableFieldKind.Toggle;
        public bool IsInteger => _field.Kind == PlaceableFieldKind.Integer;

        /// <summary>
        /// A name-valued choice (loot table, quest, tag). Rendered as a suggestion box rather than a
        /// combo: the tag source alone offers five figures of options, and an unknown stored value
        /// has to remain visible and editable instead of showing blank.
        /// </summary>
        public bool IsNameChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                    _field.VarType == VarTable.TypeString &&
                                    Options.Count > 0;

        /// <summary>An id-valued choice (key item, skill, visual effect), rendered as a combo box.</summary>
        public bool IsIdChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                  _field.VarType == VarTable.TypeInt &&
                                  Options.Count > 0;

        /// <summary>Free text, and the fallback whenever a choice source produced no options.</summary>
        public bool IsText => !IsToggle && !IsInteger && !IsNameChoice && !IsIdChoice;

        public void RefreshFromDocument()
        {
            var wasRefreshing = _context.IsRefreshing;
            _context.IsRefreshing = true;

            try
            {
                var table = new VarTable(_context.Document.Root);
                var entry = table.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, _field.VariableName, StringComparison.Ordinal));

                if (_field.VarType == VarTable.TypeInt)
                {
                    var value = entry?.IntValue ?? 0;
                    Number = value;
                    Flag = value != 0;
                    Text = entry == null ? string.Empty : value.ToString(CultureInfo.InvariantCulture);
                    SelectedOption = Options.FirstOrDefault(option =>
                        string.Equals(option.Value, Text, StringComparison.Ordinal));
                }
                else
                {
                    Text = entry?.StringValue ?? string.Empty;
                    SelectedOption = Options.FirstOrDefault(option =>
                        string.Equals(option.Value, Text, StringComparison.OrdinalIgnoreCase));
                }

                UpdateStatus();
            }
            finally
            {
                _context.IsRefreshing = wasRefreshing;
            }
        }

        partial void OnTextChanged(string value)
        {
            if (_context.IsRefreshing || _field.VarType != VarTable.TypeString)
                return;

            Write(table =>
            {
                if (string.IsNullOrWhiteSpace(value))
                    table.Remove(_field.VariableName);
                else
                    table.SetString(_field.VariableName, value);
            });
        }

        partial void OnNumberChanged(long value)
        {
            if (_context.IsRefreshing || _field.Kind != PlaceableFieldKind.Integer)
                return;

            Write(table => table.SetInt(_field.VariableName, (int)value));
        }

        partial void OnFlagChanged(bool value)
        {
            if (_context.IsRefreshing || !IsToggle)
                return;

            Write(table =>
            {
                if (value)
                    table.SetInt(_field.VariableName, 1);
                else
                    table.Remove(_field.VariableName);
            });
        }

        partial void OnSelectedOptionChanged(BehaviorChoiceOption? value)
        {
            if (_context.IsRefreshing || value == null)
                return;

            if (_field.VarType == VarTable.TypeInt)
            {
                if (int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    Write(table => table.SetInt(_field.VariableName, parsed));

                return;
            }

            Text = value.Value;
        }

        private void Write(Action<VarTable> mutation)
        {
            var applied = _context.RunEdit($"Change {Label}",
                () => mutation(new VarTable(_context.Document.Root)));

            if (applied)
                UpdateStatus();
            else
                RefreshFromDocument();
        }

        private void UpdateStatus()
        {
            var stored = _field.VarType == VarTable.TypeInt
                ? (SelectedOption?.Value ?? Text)
                : Text;

            if (string.IsNullOrWhiteSpace(stored))
            {
                Status = IsRequired ? BehaviorValueStatus.Missing : BehaviorValueStatus.None;
                StatusText = IsRequired ? "required" : null;
                return;
            }

            if (_field.Source == PlaceableValueSource.None)
            {
                Status = BehaviorValueStatus.None;
                StatusText = null;
                return;
            }

            if (_sources.IsKnown(_field.Source, stored))
            {
                Status = BehaviorValueStatus.Resolves;
                StatusText = "resolves";
                return;
            }

            Status = BehaviorValueStatus.Dangling;
            StatusText = _field.Source switch
            {
                PlaceableValueSource.ObjectTags => "no object in the module carries this tag",
                PlaceableValueSource.LootTables => "no loot table is declared with this name",
                PlaceableValueSource.Dialogs => "no conversation class with this name",
                PlaceableValueSource.Quests => "no quest with this id",
                PlaceableValueSource.SpawnTables => "no spawn table with this id",
                _ => "not a known value"
            };
        }
    }
}
