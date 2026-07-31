using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors
{
    /// <summary>One row of the local-variable grid.</summary>
    public sealed record VarTableRow(string Name, string TypeLabel, string Value);

    /// <summary>
    /// The VarTable grid: lists existing locals and supports set (add-or-update) and remove,
    /// with known-key name suggestions sourced from the game-code index (e.g.
    /// QUEST_NPC_GROUP_ID). Value semantics follow VarTable: Type 1=int, 2=float, 3=string.
    /// </summary>
    public partial class VarTableSectionViewModel : ObservableObject
    {
        private static readonly string[] WellKnownKeys =
        {
            "QUEST_NPC_GROUP_ID",
            "CREATURE_SPAWN_TABLE_ID",
            "CREATURE_SPAWN_COUNT"
        };

        private readonly Func<string, Action, bool> _runEdit;
        private readonly VarTable _varTable;
        private readonly Func<string, bool> _include;

        public ObservableCollection<VarTableRow> Rows { get; } = new();

        public IReadOnlyList<string> KnownKeys { get; }

        public IReadOnlyList<string> TypeChoices { get; } = new[] { "int", "float", "string" };

        [ObservableProperty]
        private VarTableRow? _selectedRow;

        [ObservableProperty]
        private string _newName = string.Empty;

        [ObservableProperty]
        private string _newType = "int";

        [ObservableProperty]
        private string _newValue = string.Empty;

        [ObservableProperty]
        private string? _validationHint;

        /// <summary>
        /// Takes the owning editor's edit runner rather than an <c>EditorFieldContext</c>: the grid
        /// addresses its rows through <see cref="VarTable"/> and never through a document field, so
        /// the context's document half was only ever dead weight - and unreachable for the trigger
        /// editor, whose locals may live on an area's instance struct rather than a document root.
        /// </summary>
        public VarTableSectionViewModel(
            Func<string, Action, bool> runEdit,
            VarTable varTable,
            IGameCodeIndex? gameCodeIndex,
            Func<string, bool>? include = null)
        {
            _runEdit = runEdit;
            _varTable = varTable;
            _include = include ?? (_ => true);
            KnownKeys = WellKnownKeys;
            GameCodeIndex = gameCodeIndex;
            RefreshFromDocument();
        }

        public IGameCodeIndex? GameCodeIndex { get; }

        public void RefreshFromDocument()
        {
            Rows.Clear();
            foreach (var entry in _varTable)
            {
                if (!_include(entry.Name))
                    continue;

                var (typeLabel, value) = entry.Type switch
                {
                    VarTable.TypeInt => ("int", entry.IntValue?.ToString() ?? ""),
                    VarTable.TypeFloat => ("float", entry.FloatValue?.ToString() ?? ""),
                    VarTable.TypeString => ("string", entry.StringValue ?? ""),
                    _ => ($"type {entry.Type}", "")
                };
                Rows.Add(new VarTableRow(entry.Name, typeLabel, value));
            }
        }

        [RelayCommand]
        private void SetVariable()
        {
            ValidationHint = null;
            var name = NewName.Trim();
            if (name.Length == 0)
            {
                ValidationHint = "Variable name is required.";
                return;
            }

            if (!_include(name))
            {
                ValidationHint = "This variable has a dedicated editor on another tab.";
                return;
            }

            if (!CanEncodeNwnString(name) ||
                (NewType == "string" && !CanEncodeNwnString(NewValue)))
            {
                ValidationHint =
                    "Variable names and string values may only contain Windows-1252 characters.";
                return;
            }

            var applied = NewType switch
            {
                "int" when int.TryParse(NewValue, out var intValue) =>
                    _runEdit($"Set local {name}", () => _varTable.SetInt(name, intValue)),
                "float" when float.TryParse(NewValue, out var floatValue) && float.IsFinite(floatValue) =>
                    _runEdit($"Set local {name}", () => _varTable.SetFloat(name, floatValue)),
                "string" =>
                    _runEdit($"Set local {name}", () => _varTable.SetString(name, NewValue)),
                _ => false
            };

            if (!applied)
            {
                ValidationHint = NewType == "string"
                    ? "The string variable could not be applied."
                    : $"'{NewValue}' is not a valid {NewType}.";
                return;
            }

            WarnOnUnknownGameCodeValue(name);
            RefreshFromDocument();
        }

        private static bool CanEncodeNwnString(string value)
        {
            try
            {
                JsonStringCodec.Encode(value);
                return true;
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
        }

        [RelayCommand]
        private void RemoveSelected()
        {
            if (SelectedRow == null)
                return;

            var name = SelectedRow.Name;
            _runEdit($"Remove local {name}", () => _varTable.Remove(name));
            RefreshFromDocument();
        }

        partial void OnSelectedRowChanged(VarTableRow? value)
        {
            if (value == null)
                return;

            NewName = value.Name;
            NewType = value.TypeLabel is "int" or "float" or "string" ? value.TypeLabel : "int";
            NewValue = value.Value;
        }

        /// <summary>Live hint (not a blocker) when a well-known key holds an unknown id.</summary>
        private void WarnOnUnknownGameCodeValue(string name)
        {
            if (GameCodeIndex == null)
                return;

            if (name == "QUEST_NPC_GROUP_ID" &&
                _varTable.GetInt(name) is { } groupId && !GameCodeIndex.IsValidNpcGroup(groupId))
            {
                ValidationHint = $"Warning: {groupId} is not a known NPCGroupType value.";
            }
        }
    }
}
