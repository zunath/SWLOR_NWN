using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>
    /// Adapts one creature loot row to the shared deferred, paged behavior picker.
    /// </summary>
    /// <remarks>
    /// Loot tables used to use a private combo box which eagerly created the entire catalog and
    /// offered no way to find an entry. This adapter keeps the composite LOOT_TABLE_n write in the
    /// creature editor while reusing the same searchable chooser as behaviors and equipment.
    /// </remarks>
    public sealed class CreatureLootTablePickerViewModel : BehaviorRowViewModel
    {
        private readonly CreatureLootEntryViewModel _entry;
        private readonly IReadOnlyDictionary<string, CreatureLootTableInfo> _tablesById;
        private readonly Action<CreatureLootEntryViewModel, string> _writeTable;
        private readonly Action<CreatureLootEntryViewModel> _tableApplied;
        private CreatureLootTableInfo? _pendingTable;

        public override string SelectedChoiceDisplay =>
            Choice?.Display ?? DisplayName(_entry.Table);

        public override string? SelectedChoiceIdentifier =>
            Choice?.Identifier ?? _entry.Table?.Id;

        public override bool HasValue => _entry.Table != null;

        protected override bool SelectsFirstChoiceWhenUnset => false;

        public CreatureLootTablePickerViewModel(
            CreatureLootEntryViewModel entry,
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<CreatureLootTableInfo> tables,
            Action<CreatureLootEntryViewModel, string> writeTable,
            Action<CreatureLootEntryViewModel> tableApplied)
            : base(
                new BehaviorFieldDefinition
                {
                    Label = "Table",
                    Name = "creature_loot_table",
                    Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.CExoString,
                    IsSearchable = true,
                    IsRequired = true
                },
                store,
                runEdit,
                choiceLoader: () => tables
                    .Select(table => new BehaviorChoice(table.Id, DisplayName(table))
                    {
                        Identifier = table.Id
                    })
                    .ToList())
        {
            _entry = entry;
            _tablesById = tables.ToDictionary(table => table.Id, StringComparer.OrdinalIgnoreCase);
            _writeTable = writeTable;
            _tableApplied = tableApplied;
            Reload();
        }

        protected override void ReadValue()
        {
            _pendingTable = null;
            var tableId = _entry.Table?.Id;
            Choice = Choices.FirstOrDefault(choice =>
                string.Equals(choice.StringValue, tableId, StringComparison.OrdinalIgnoreCase));
        }

        protected override void WriteChoice(BehaviorChoiceViewModel value)
        {
            if (value.StringValue == null || !_tablesById.TryGetValue(value.StringValue, out var table))
                return;

            _writeTable(_entry, table.Id);
            _pendingTable = table;
        }

        protected override void OnApplied()
        {
            if (_pendingTable != null)
            {
                _entry.ApplyTable(_pendingTable);
                _pendingTable = null;
                _tableApplied(_entry);
            }

            base.OnApplied();
        }

        private static string DisplayName(CreatureLootTableInfo? table)
        {
            if (table == null)
                return "Choose a loot table...";

            var idSuffix = $" ({table.Id})";
            return table.DisplayName.EndsWith(idSuffix, StringComparison.OrdinalIgnoreCase)
                ? table.DisplayName[..^idSuffix.Length]
                : table.DisplayName;
        }
    }
}
