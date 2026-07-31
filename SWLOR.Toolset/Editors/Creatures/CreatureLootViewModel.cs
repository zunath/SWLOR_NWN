using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Contiguous LOOT_TABLE_n editor with registered-table previews.</summary>
    public sealed partial class CreatureLootViewModel : ObservableObject, IDisposable
    {
        private static readonly Lazy<IReadOnlyList<CreatureLootTableInfo>> SharedTables =
            new(CreatureLootTableCatalog.Build);
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action<string>? _openDefinition;
        private readonly Func<string, string>? _resolveItemName;
        private bool _loading;

        public IReadOnlyList<CreatureLootTableInfo> Tables { get; }
        public ObservableCollection<CreatureLootEntryViewModel> Entries { get; } = new();

        [ObservableProperty]
        private CreatureLootEntryViewModel? _selectedEntry;

        public IReadOnlyList<CreatureLootPreviewItemViewModel> PreviewItems => SelectedEntry?.Table == null
            ? Array.Empty<CreatureLootPreviewItemViewModel>()
            : SelectedEntry.Table.Items
                .Select(item => new CreatureLootPreviewItemViewModel(item, _resolveItemName))
                .ToList();

        public IReadOnlyList<CreatureExpectedLootItemViewModel> ExpectedItems => BuildExpectedItems();
        public string PreviewTitle => SelectedEntry?.Table == null
            ? "Select a configured drop"
            : SelectedEntry.Table.DisplayName + (SelectedEntry.Table.IsRare ? " · rare table" : string.Empty);
        public string PreviewTableId => SelectedEntry?.Table?.Id ?? string.Empty;
        public string PreviewEmptyMessage => SelectedEntry?.Table == null
            ? string.Empty
            : Tables.Any(table => string.Equals(table.Id, SelectedEntry.Table.Id, StringComparison.OrdinalIgnoreCase))
                ? "This registered table has no items and cannot produce loot."
                : "This table is no longer registered, so its contents are unavailable.";
        public string ExpectedDrops => SelectedEntry?.ExpectedDrops ?? string.Empty;
        public string ExpectedSummary =>
            $"{Entries.Where(entry => entry.Table?.Items.Count > 0).Sum(entry => (double)entry.Chance / 100d * (double)entry.Pulls):0.##} expected successful pull(s) per kill";
        public string Warning { get; private set; } = string.Empty;
        public bool HasWarning => Warning.Length > 0;
        public bool HasEntries => Entries.Count > 0;
        public bool HasConfiguredEntries => Entries.Any(entry => entry.HasTable);
        public bool HasSelectedTable => SelectedEntry?.Table != null;
        public bool HasPreviewItems => PreviewItems.Count > 0;
        public bool HasExpectedItems => ExpectedItems.Count > 0;
        public bool NeedsNormalization { get; private set; }
        public bool CanOpenDefinition =>
            _openDefinition != null && !string.IsNullOrWhiteSpace(SelectedEntry?.Table?.DefinitionTypeName);

        public CreatureLootViewModel(
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<CreatureLootTableInfo>? tables = null,
            Action<string>? openDefinition = null,
            Func<string, string>? resolveItemName = null)
        {
            _store = store;
            _runEdit = runEdit;
            _openDefinition = openDefinition;
            _resolveItemName = resolveItemName;
            Tables = tables ?? SharedTables.Value;
            Reload();
        }

        [RelayCommand]
        private void Add()
        {
            var pending = Entries.FirstOrDefault(entry => entry.IsPending);
            if (pending != null)
            {
                SelectedEntry = pending;
                pending.TablePicker.OpenSearchCommand.Execute(null);
                return;
            }

            var entry = CreateEntry(new CreatureLootEntry(string.Empty, 100, 1), Entries.Count + 1);
            Entries.Add(entry);
            SelectedEntry = entry;
            NotifyCollectionChanged();
            entry.TablePicker.OpenSearchCommand.Execute(null);
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
                DisposeEntries();
                Entries.Clear();
                for (var index = 0; index < entries.Count; index++)
                    Entries.Add(CreateEntry(entries[index], index + 1));
                SelectedEntry = Entries.FirstOrDefault();
                RebuildWarning(hasGap);
            }
            finally
            {
                _loading = false;
            }

            OnPropertyChanged(nameof(NeedsNormalization));
            NotifyCollectionChanged();
            RefreshPreview();
        }

        public void Normalize()
        {
            _store.WriteLoot(Entries.Where(entry => entry.HasTable).Select(entry => entry.ToEntry()));
            NeedsNormalization = false;
            RebuildWarning(false);
        }

        private CreatureLootEntryViewModel CreateEntry(CreatureLootEntry entry, int position) => new(
            entry,
            Tables,
            _store,
            _runEdit,
            WriteTable,
            TableApplied,
            Changed,
            Remove,
            position);

        private void Changed(CreatureLootEntryViewModel entry)
        {
            if (_loading || entry.IsPending)
                return;

            var entries = Entries.Where(candidate => candidate.HasTable)
                .Select(candidate => candidate.ToEntry()).ToList();
            if (!_runEdit("Change loot drop", () => _store.WriteLoot(entries)))
            {
                Reload();
                return;
            }

            NeedsNormalization = false;
            SelectedEntry = entry;
            RebuildWarning(false);
            RefreshPreview();
        }

        /// <summary>Writes a picker selection inside the shared row's existing undo transaction.</summary>
        private void WriteTable(CreatureLootEntryViewModel entry, string tableId)
        {
            var entries = Entries
                .Select(candidate => ReferenceEquals(candidate, entry)
                    ? candidate.ToEntry(tableId)
                    : candidate.ToEntry())
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.TableId))
                .ToList();
            _store.WriteLoot(entries);
        }

        private void TableApplied(CreatureLootEntryViewModel entry)
        {
            NeedsNormalization = false;
            SelectedEntry = entry;
            RebuildWarning(false);
            RefreshPreview();
        }

        private void Remove(CreatureLootEntryViewModel entry)
        {
            if (entry.IsPending)
            {
                entry.Dispose();
                Entries.Remove(entry);
                UpdatePositions();
                SelectedEntry = Entries.FirstOrDefault();
                NotifyCollectionChanged();
                RefreshPreview();
                return;
            }

            var entries = Entries.Where(candidate => !ReferenceEquals(candidate, entry) && candidate.HasTable)
                .Select(candidate => candidate.ToEntry()).ToList();
            if (!_runEdit("Remove loot drop", () => _store.WriteLoot(entries)))
                return;

            entry.Dispose();
            Entries.Remove(entry);
            UpdatePositions();
            NeedsNormalization = false;
            SelectedEntry = Entries.FirstOrDefault();
            RebuildWarning(false);
            NotifyCollectionChanged();
            RefreshPreview();
        }

        private void UpdatePositions()
        {
            for (var index = 0; index < Entries.Count; index++)
                Entries[index].Position = index + 1;
        }

        private void RebuildWarning(bool hasGap)
        {
            var configured = Entries.Where(entry => entry.HasTable).ToList();
            var unknownCount = configured.Count(entry =>
                Tables.All(table => !string.Equals(table.Id, entry.Table?.Id, StringComparison.OrdinalIgnoreCase)));
            var emptyCount = configured.Count(entry =>
                Tables.Any(table => string.Equals(table.Id, entry.Table?.Id, StringComparison.OrdinalIgnoreCase)) &&
                entry.Table?.Items.Count == 0);
            var warnings = new List<string>();
            if (hasGap)
                warnings.Add("Loot rows had a numbering gap. The next edit or save will make them contiguous.");
            if (unknownCount > 0)
                warnings.Add(unknownCount == 1
                    ? "One configured drop references a table that is no longer registered."
                    : $"{unknownCount} configured drops reference tables that are no longer registered.");
            if (emptyCount > 0)
                warnings.Add(emptyCount == 1
                    ? "One configured loot table has no items and cannot produce loot."
                    : $"{emptyCount} configured loot tables have no items and cannot produce loot.");
            Warning = string.Join(" ", warnings);
            OnPropertyChanged(nameof(Warning));
            OnPropertyChanged(nameof(HasWarning));
        }

        private IReadOnlyList<CreatureExpectedLootItemViewModel> BuildExpectedItems()
        {
            var expected = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in Entries.Where(entry => entry.Table?.Items.Count > 0))
            {
                var items = entry.Table!.Items;
                var totalWeight = items.Where(item => item.Weight > 0).Sum(item => item.Weight);
                if (totalWeight <= 0)
                    continue;

                var successfulPulls = (double)entry.Chance / 100d * (double)entry.Pulls;
                foreach (var item in items.Where(item => item.Weight > 0))
                {
                    var averageQuantity = (Math.Max(1, item.MaximumQuantity) + 1d) / 2d;
                    var quantity = successfulPulls * item.Weight / totalWeight * averageQuantity;
                    expected[item.ResRef] = expected.GetValueOrDefault(item.ResRef) + quantity;
                }
            }

            return expected
                .Select(pair => new CreatureExpectedLootItemViewModel(
                    _resolveItemName?.Invoke(pair.Key) ?? pair.Key,
                    pair.Key,
                    pair.Value))
                .OrderByDescending(item => item.ExpectedQuantity)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        partial void OnSelectedEntryChanged(CreatureLootEntryViewModel? value)
        {
            RefreshPreview();
            OnPropertyChanged(nameof(CanOpenDefinition));
            OpenDefinitionCommand.NotifyCanExecuteChanged();
        }

        private void NotifyCollectionChanged()
        {
            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(HasConfiguredEntries));
            OnPropertyChanged(nameof(ExpectedSummary));
            OnPropertyChanged(nameof(ExpectedItems));
            OnPropertyChanged(nameof(HasExpectedItems));
        }

        private void RefreshPreview()
        {
            OnPropertyChanged(nameof(PreviewItems));
            OnPropertyChanged(nameof(PreviewTitle));
            OnPropertyChanged(nameof(PreviewTableId));
            OnPropertyChanged(nameof(PreviewEmptyMessage));
            OnPropertyChanged(nameof(ExpectedDrops));
            OnPropertyChanged(nameof(HasSelectedTable));
            OnPropertyChanged(nameof(HasPreviewItems));
            OnPropertyChanged(nameof(ExpectedSummary));
            OnPropertyChanged(nameof(ExpectedItems));
            OnPropertyChanged(nameof(HasExpectedItems));
            OnPropertyChanged(nameof(CanOpenDefinition));
            OpenDefinitionCommand.NotifyCanExecuteChanged();
        }

        public void Dispose() => DisposeEntries();

        private void DisposeEntries()
        {
            foreach (var entry in Entries)
                entry.Dispose();
        }
    }
}
