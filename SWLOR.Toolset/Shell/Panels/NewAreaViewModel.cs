using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The new-area wizard (WP7.3): collects a resref, display name, tileset, terrain, and size,
    /// then creates the area triplet through <see cref="NewAreaWriter"/>. Presented inline by the
    /// Module Explorer (the same overlay pattern the palette browser uses) rather than as a separate
    /// window, so it needs no window lifetime plumbing.
    /// </summary>
    public partial class NewAreaViewModel : ObservableObject
    {
        private readonly ModuleWorkspace _workspace;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly Action<string> _onCreated;
        private readonly Action _onCancelled;

        /// <summary>Every tileset the resource index can see, for the tileset picker.</summary>
        public ObservableCollection<string> Tilesets { get; } = new();

        /// <summary>The selected tileset's fillable terrains - what the new area's floor can be made of.</summary>
        public ObservableCollection<string> Terrains { get; } = new();

        [ObservableProperty]
        private string _resRef = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string? _selectedTileset;

        [ObservableProperty]
        private string? _selectedTerrain;

        [ObservableProperty]
        private double _width = 4;

        [ObservableProperty]
        private double _height = 4;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public NewAreaViewModel(
            ModuleWorkspace workspace,
            TilesetCatalog? tilesetCatalog,
            Action<string> onCreated,
            Action onCancelled)
        {
            _workspace = workspace;
            _tilesetCatalog = tilesetCatalog;
            _onCreated = onCreated;
            _onCancelled = onCancelled;

            if (_tilesetCatalog == null)
            {
                StatusMessage = "Game data is not loaded, so no tilesets are available.";
                return;
            }

            foreach (var name in _tilesetCatalog.GetTilesetNames())
                Tilesets.Add(name);

            // Default to the template's own tileset when it is available, so the wizard opens on a
            // known-good combination.
            SelectedTileset = Tilesets.Contains("tms01") ? "tms01" : Tilesets.FirstOrDefault();
        }

        partial void OnSelectedTilesetChanged(string? value) => RefreshTerrains(value);

        private void RefreshTerrains(string? tilesetResRef)
        {
            Terrains.Clear();
            SelectedTerrain = null;

            if (_tilesetCatalog == null || string.IsNullOrWhiteSpace(tilesetResRef) ||
                !_tilesetCatalog.TryGetTileset(tilesetResRef, out var tileset))
                return;

            foreach (var terrain in TilePainter.FillableTerrains(tileset))
                Terrains.Add(terrain);

            SelectedTerrain = TilePainter.DefaultFillTerrain(tileset) ?? Terrains.FirstOrDefault();
        }

        [RelayCommand]
        private void Create()
        {
            NewAreaWriter.TilesetResolver? resolver = _tilesetCatalog == null
                ? null
                : (string resRef, out TilesetDefinition tileset) => _tilesetCatalog.TryGetTileset(resRef, out tileset);

            if (NewAreaWriter.TryCreate(
                    _workspace, resolver, ResRef, DisplayName, SelectedTileset ?? string.Empty,
                    (int)Width, (int)Height, SelectedTerrain, out var error))
            {
                _onCreated(ResRef.Trim().ToLowerInvariant());
                return;
            }

            StatusMessage = error;
        }

        [RelayCommand]
        private void Cancel() => _onCancelled();
    }
}
