using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>One assigned registered ability and its optional effective-level override.</summary>
    public sealed partial class CreatureAbilityEntryViewModel : ObservableObject
    {
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action<CreatureAbilityEntryViewModel> _remove;
        private bool _loading;
        private int _iconRequested;

        public CreatureAbilityInfo Info { get; }
        public string Name => Info.Name;
        public string Description => Info.Description;
        public int FeatId => Info.FeatId;
        public bool HasEffectiveLevel => Info.EffectivePerkId > 0;
        public string EffectiveLevelLabel => HasEffectiveLevel
            ? $"{Info.EffectivePerkName} effective level"
            : string.Empty;
        public int MaximumLevel { get; }
        public string EffectiveLevelHelp => HasEffectiveLevel
            ? $"Leave at {MaximumLevel} to use the perk's maximum level."
            : string.Empty;

        [ObservableProperty]
        private decimal _effectiveLevel;

        [ObservableProperty]
        private Bitmap? _icon;

        public CreatureAbilityEntryViewModel(
            CreatureAbilityInfo info,
            int maximumLevel,
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Action<CreatureAbilityEntryViewModel> remove)
        {
            Info = info;
            MaximumLevel = Math.Max(1, maximumLevel);
            _store = store;
            _runEdit = runEdit;
            _remove = remove;
            Reload();
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                EffectiveLevel = HasEffectiveLevel
                    ? _store.Locals.GetInt($"PERK_LEVEL_{Info.EffectivePerkId}") ?? MaximumLevel
                    : 0;
            }
            finally
            {
                _loading = false;
            }
        }

        partial void OnEffectiveLevelChanged(decimal value)
        {
            if (_loading || !HasEffectiveLevel)
                return;
            if (decimal.Truncate(value) != value || value < 1 || value > MaximumLevel)
            {
                Reload();
                return;
            }

            if (!_runEdit($"Change {Name} effective level", () =>
                {
                    var local = $"PERK_LEVEL_{Info.EffectivePerkId}";
                    if ((int)value == MaximumLevel)
                        _store.Locals.Remove(local);
                    else
                        _store.Locals.SetInt(local, (int)value);
                }))
            {
                Reload();
            }
        }

        [RelayCommand]
        private void Remove() => _remove(this);

        internal bool TryBeginIconRequest() => Interlocked.Exchange(ref _iconRequested, 1) == 0;
    }
}
