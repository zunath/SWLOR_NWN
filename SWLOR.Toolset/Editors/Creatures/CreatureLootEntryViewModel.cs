using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Editable chance and pull count for one selected registered loot table.</summary>
    public sealed partial class CreatureLootEntryViewModel : ObservableObject
    {
        private readonly Action<CreatureLootEntryViewModel> _changed;
        private readonly Action<CreatureLootEntryViewModel> _remove;
        private bool _loading;

        public IReadOnlyList<CreatureLootTableInfo> Tables { get; }

        [ObservableProperty]
        private CreatureLootTableInfo? _table;

        [ObservableProperty]
        private decimal _chance;

        [ObservableProperty]
        private decimal _pulls;

        public string ExpectedDrops => $"{(double)Chance / 100d * (double)Pulls:0.##} expected pull(s) per kill";

        public CreatureLootEntryViewModel(
            CreatureLootEntry entry,
            IReadOnlyList<CreatureLootTableInfo> tables,
            Action<CreatureLootEntryViewModel> changed,
            Action<CreatureLootEntryViewModel> remove)
        {
            Tables = tables;
            _changed = changed;
            _remove = remove;
            _loading = true;
            Table = tables.FirstOrDefault(table => table.Id == entry.TableId)
                    ?? new CreatureLootTableInfo(entry.TableId, $"Unknown table ({entry.TableId})", false,
                        Array.Empty<CreatureLootTableItemInfo>());
            Chance = entry.Chance;
            Pulls = entry.Pulls;
            _loading = false;
        }

        public CreatureLootEntry ToEntry() => new(
            Table?.Id ?? string.Empty,
            Math.Clamp((int)Chance, 1, 100),
            Math.Max(1, (int)Pulls));

        [RelayCommand]
        private void Remove() => _remove(this);

        partial void OnTableChanged(CreatureLootTableInfo? value) => Changed();

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
            if (!_loading)
                _changed(this);
        }
    }
}
