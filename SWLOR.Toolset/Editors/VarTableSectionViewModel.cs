using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;

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

        private readonly EditorFieldContext _context;
        private readonly VarTable _varTable;

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

        public VarTableSectionViewModel(
            EditorFieldContext context, VarTable varTable, IGameCodeIndex? gameCodeIndex)
        {
            _context = context;
            _varTable = varTable;
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

            var applied = NewType switch
            {
                "int" when int.TryParse(NewValue, out var intValue) =>
                    _context.RunEdit($"Set local {name}", () => _varTable.SetInt(name, intValue)),
                "float" when float.TryParse(NewValue, out var floatValue) && float.IsFinite(floatValue) =>
                    _context.RunEdit($"Set local {name}", () => _varTable.SetFloat(name, floatValue)),
                "string" =>
                    _context.RunEdit($"Set local {name}", () => _varTable.SetString(name, NewValue)),
                _ => false
            };

            if (!applied && NewType != "string")
            {
                ValidationHint = $"'{NewValue}' is not a valid {NewType}.";
                return;
            }

            WarnOnUnknownGameCodeValue(name);
            RefreshFromDocument();
        }

        [RelayCommand]
        private void RemoveSelected()
        {
            if (SelectedRow == null)
                return;

            var name = SelectedRow.Name;
            _context.RunEdit($"Remove local {name}", () => _varTable.Remove(name));
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
