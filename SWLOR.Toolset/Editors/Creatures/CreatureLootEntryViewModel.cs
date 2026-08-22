using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Editable chance and pull count for one selected registered loot table.</summary>
    public sealed partial class CreatureLootEntryViewModel : ObservableObject, IDisposable
    {
        private readonly Action<CreatureLootEntryViewModel> _changed;
        private readonly Action<CreatureLootEntryViewModel> _remove;
        private bool _loading;

        public CreatureLootTablePickerViewModel TablePicker { get; }

        public CreatureLootTableInfo? Table { get; private set; }

        [ObservableProperty]
        private int _position;

        public bool HasTable => Table != null;
        public bool IsPending => Table == null;
        public string EditorTitle => $"Drop {Position}";
        public string ConfigurationSummary =>
            $"{Chance:0}% chance \u00B7 {Pulls:0} {(Pulls == 1 ? "pull" : "pulls")}";

        [ObservableProperty]
        private decimal _chance;

        [ObservableProperty]
        private decimal _pulls;

        public string ExpectedDrops => $"{(double)Chance / 100d * (double)Pulls:0.##} expected pull(s) per kill";

        public CreatureLootEntryViewModel(
            CreatureLootEntry entry,
            IReadOnlyList<CreatureLootTableInfo> tables,
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Action<CreatureLootEntryViewModel, string> writeTable,
            Action<CreatureLootEntryViewModel> tableApplied,
            Action<CreatureLootEntryViewModel> changed,
            Action<CreatureLootEntryViewModel> remove,
            int position = 0)
        {
            _changed = changed;
            _remove = remove;
            _loading = true;
            Position = position;
            Table = string.IsNullOrWhiteSpace(entry.TableId)
                ? null
                : tables.FirstOrDefault(table => table.Id == entry.TableId)
                  ?? new CreatureLootTableInfo(entry.TableId, $"Unknown table ({entry.TableId})", false,
                      Array.Empty<CreatureLootTableItemInfo>());
            Chance = entry.Chance;
            Pulls = entry.Pulls;
            _loading = false;
            TablePicker = new CreatureLootTablePickerViewModel(
                this, store, runEdit, tables, writeTable, tableApplied);
        }

        public CreatureLootEntry ToEntry(string? tableId = null) => new(
            tableId ?? Table?.Id ?? string.Empty,
            Math.Clamp((int)Chance, 1, 100),
            Math.Max(1, (int)Pulls));

        internal void ApplyTable(CreatureLootTableInfo table)
        {
            Table = table;
            OnPropertyChanged(nameof(Table));
            OnPropertyChanged(nameof(HasTable));
            OnPropertyChanged(nameof(IsPending));
        }

        partial void OnPositionChanged(int value) => OnPropertyChanged(nameof(EditorTitle));

        [RelayCommand]
        private void Remove() => _remove(this);

        partial void OnChanceChanged(decimal value)
        {
            if (!_loading && (decimal.Truncate(value) != value || value < 1 || value > 100))
            {
                _loading = true;
                Chance = Math.Clamp(decimal.Truncate(value), 1, 100);
                _loading = false;
            }
            Changed();
        }

        partial void OnPullsChanged(decimal value)
        {
            if (!_loading && (decimal.Truncate(value) != value || value < 1 || value > 100))
            {
                _loading = true;
                Pulls = Math.Clamp(decimal.Truncate(value), 1, 100);
                _loading = false;
            }
            Changed();
        }

        private void Changed()
        {
            OnPropertyChanged(nameof(ExpectedDrops));
            OnPropertyChanged(nameof(ConfigurationSummary));
            if (!_loading)
                _changed(this);
        }

        public void Dispose() => TablePicker.Dispose();
    }
}
