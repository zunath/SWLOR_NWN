using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Editors.Sources
{
    public sealed record ObjectPlacementRowViewModel(
        ObjectPlacement Placement,
        string AreaName)
    {
        public string AreaResRef => Placement.AreaResRef;
        public string Tag => Placement.Tag;
        public string Position => $"{Placement.X:0.0}, {Placement.Y:0.0}, {Placement.Z:0.0}";
        public string Detail => string.IsNullOrWhiteSpace(Tag)
            ? $"Position {Position}"
            : $"{Tag} · {Position}";
    }

    /// <summary>The shared Source tab for blueprint-backed objects.</summary>
    public sealed partial class ObjectSourceSectionViewModel : ObservableObject
    {
        private readonly Func<ResourceType, string, Task<IReadOnlyList<ObjectPlacement>>> _find;
        private readonly Func<string, string> _resolveAreaName;
        private readonly Action<ObjectPlacement> _goTo;
        private int _loadGeneration;
        private string _resRef;

        public ObjectSourceSectionViewModel(
            ResourceType blueprintType,
            string resRef,
            Func<ResourceType, string, Task<IReadOnlyList<ObjectPlacement>>> find,
            Func<string, string> resolveAreaName,
            Action<ObjectPlacement> goTo)
        {
            BlueprintType = blueprintType;
            _resRef = resRef;
            _find = find ?? throw new ArgumentNullException(nameof(find));
            _resolveAreaName = resolveAreaName ?? throw new ArgumentNullException(nameof(resolveAreaName));
            _goTo = goTo ?? throw new ArgumentNullException(nameof(goTo));
            _ = LoadAsync();
        }

        public ResourceType BlueprintType { get; }
        public ObservableCollection<ObjectPlacementRowViewModel> Placements { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Status))]
        private bool _isLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Status))]
        private string? _loadError;

        public string Status => IsLoading
            ? "Scanning area placements..."
            : LoadError != null
                ? LoadError
                : Placements.Count == 0
                    ? "This blueprint is not placed in any area."
                    : $"{Placements.Count} placed instance{(Placements.Count == 1 ? string.Empty : "s")}";

        public void SetResRef(string resRef)
        {
            if (string.Equals(_resRef, resRef, StringComparison.OrdinalIgnoreCase))
                return;

            _resRef = resRef;
            _ = LoadAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadAsync().ConfigureAwait(true);

        [RelayCommand]
        private void GoTo(ObjectPlacementRowViewModel? row)
        {
            if (row != null)
                _goTo(row.Placement);
        }

        private async Task LoadAsync()
        {
            var generation = ++_loadGeneration;
            IsLoading = true;
            LoadError = null;
            var resRef = _resRef;
            try
            {
                var placements = await _find(BlueprintType, resRef).ConfigureAwait(true);
                if (generation != _loadGeneration)
                    return;

                Placements.Clear();
                foreach (var placement in placements)
                    Placements.Add(new ObjectPlacementRowViewModel(
                        placement, _resolveAreaName(placement.AreaResRef)));
                OnPropertyChanged(nameof(Status));
            }
            catch (Exception ex)
            {
                if (generation == _loadGeneration)
                    LoadError = $"Could not scan placements: {ex.Message}";
            }
            finally
            {
                if (generation == _loadGeneration)
                    IsLoading = false;
            }
        }
    }
}
