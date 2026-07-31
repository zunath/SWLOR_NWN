using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Contiguous LOOT_TABLE_n editor with registered-table previews.</summary>
    public sealed partial class CreatureLootViewModel : ObservableObject
    {
        private static readonly Lazy<IReadOnlyList<CreatureLootTableInfo>> SharedTables =
            new(CreatureLootTableCatalog.Build);
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action<string>? _openDefinition;
        private bool _loading;

        public IReadOnlyList<CreatureLootTableInfo> Tables { get; }
        public ObservableCollection<CreatureLootEntryViewModel> Entries { get; } = new();

        [ObservableProperty]
        private CreatureLootEntryViewModel? _selectedEntry;

        public IReadOnlyList<CreatureLootTableItemInfo> PreviewItems =>
            SelectedEntry?.Table?.Items ?? Array.Empty<CreatureLootTableItemInfo>();
        public string PreviewTitle => SelectedEntry?.Table == null
            ? "Select a loot row to preview it"
            : SelectedEntry.Table.DisplayName + (SelectedEntry.Table.IsRare ? " · rare table" : string.Empty);
        public string ExpectedDrops => SelectedEntry?.ExpectedDrops ?? string.Empty;
        public string Warning { get; private set; } = string.Empty;
        public bool HasWarning => Warning.Length > 0;
        public bool NeedsNormalization { get; private set; }
        public bool CanOpenDefinition =>
            _openDefinition != null && !string.IsNullOrWhiteSpace(SelectedEntry?.Table?.DefinitionTypeName);

        public CreatureLootViewModel(
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<CreatureLootTableInfo>? tables = null,
            Action<string>? openDefinition = null)
        {
            _store = store;
            _runEdit = runEdit;
            _openDefinition = openDefinition;
            Tables = tables ?? SharedTables.Value;
            Reload();
        }

        [RelayCommand]
        private void Add()
        {
            var first = Tables.FirstOrDefault();
            if (first == null)
                return;
            var entries = Entries.Select(entry => entry.ToEntry())
                .Append(new CreatureLootEntry(first.Id, 100, 1))
                .ToList();
            if (!_runEdit("Add loot table", () => _store.WriteLoot(entries)))
                return;
            Reload();
            SelectedEntry = Entries.LastOrDefault();
        }

        [RelayCommand(CanExecute = nameof(CanOpenDefinition))]
        private void OpenDefinition()
        {
            var typeName = SelectedEntry?.Table?.DefinitionTypeName;
            if (!string.IsNullOrWhiteSpace(typeName))
                _openDefinition?.Invoke(typeName);
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                var entries = _store.ReadLoot(out var hasGap);
                NeedsNormalization = hasGap;
                Entries.Clear();
                foreach (var entry in entries)
                    Entries.Add(new CreatureLootEntryViewModel(entry, Tables, Changed, Remove));
                var unknownCount = entries.Count(entry => Tables.All(table => table.Id != entry.TableId));
                var warnings = new List<string>();
                if (hasGap)
                    warnings.Add("Loot rows had a numbering gap. The next edit or save will make them contiguous.");
                if (unknownCount > 0)
                    warnings.Add(unknownCount == 1
                        ? "One loot row references a table that is no longer registered."
                        : $"{unknownCount} loot rows reference tables that are no longer registered.");
                Warning = string.Join(" ", warnings);
                SelectedEntry = Entries.FirstOrDefault();
            }
            finally
            {
                _loading = false;
            }
            OnPropertyChanged(nameof(Warning));
            OnPropertyChanged(nameof(HasWarning));
            OnPropertyChanged(nameof(NeedsNormalization));
            RefreshPreview();
        }

        public void Normalize()
        {
            var entries = Entries.Select(entry => entry.ToEntry()).ToList();
            _store.WriteLoot(entries);
        }

        private void Changed(CreatureLootEntryViewModel entry)
        {
            if (_loading)
                return;
            var entries = Entries.Select(candidate => candidate.ToEntry()).ToList();
            if (!_runEdit("Change loot table", () => _store.WriteLoot(entries)))
            {
                Reload();
                return;
            }
            SelectedEntry = entry;
            RefreshPreview();
        }

        private void Remove(CreatureLootEntryViewModel entry)
        {
            var entries = Entries.Where(candidate => !ReferenceEquals(candidate, entry))
                .Select(candidate => candidate.ToEntry()).ToList();
            if (!_runEdit("Remove loot table", () => _store.WriteLoot(entries)))
                return;
            Reload();
        }

        partial void OnSelectedEntryChanged(CreatureLootEntryViewModel? value)
        {
            RefreshPreview();
            OnPropertyChanged(nameof(CanOpenDefinition));
            OpenDefinitionCommand.NotifyCanExecuteChanged();
        }

        private void RefreshPreview()
        {
            OnPropertyChanged(nameof(PreviewItems));
            OnPropertyChanged(nameof(PreviewTitle));
            OnPropertyChanged(nameof(ExpectedDrops));
        }
    }
}
