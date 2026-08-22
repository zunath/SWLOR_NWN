using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The new-area wizard collects a resref, display name, tileset, and size, then creates
    /// the area triplet through <see cref="NewAreaWriter"/>. Presented inline by the Module Explorer
    /// (the same overlay pattern the palette browser uses) rather than as a separate window, so it
    /// needs no window lifetime plumbing.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT ask for a terrain. An area is not "a Grass area" - it uses as many
    /// terrains as the user paints into it - and the tileset's .set already declares what a blank
    /// area of it is made of ([GENERAL] Floor/Default). Terrain selection lives solely in the area
    /// editor's paint palette.
    /// </remarks>
    public partial class NewAreaViewModel : ObservableObject
    {
        private readonly ModuleWorkspace _workspace;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly Action<string> _onCreated;
        private readonly Action _onCancelled;

        /// <summary>One tileset in the picker: the resref that gets written to the area, shown with its readable name.</summary>
        public sealed record TilesetChoice(string ResRef, string Label)
        {
            public override string ToString() => Label;
        }

        /// <summary>Every tileset the resource index can see, for the tileset picker.</summary>
        public ObservableCollection<TilesetChoice> Tilesets { get; } = new();

        [ObservableProperty]
        private string _resRef = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private TilesetChoice? _selectedTileset;

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
            Action onCancelled,
            Func<bool>? canWrite = null)
        {
            _workspace = workspace;
            _tilesetCatalog = tilesetCatalog;
            _onCreated = onCreated;
            _onCancelled = onCancelled;
            _canWrite = canWrite;

            if (_tilesetCatalog == null)
            {
                StatusMessage = "Game data is not loaded, so no tilesets are available.";
                return;
            }

            foreach (var name in _tilesetCatalog.GetTilesetNames())
                Tilesets.Add(new TilesetChoice(name, _tilesetCatalog.GetDisplayLabel(name)));

            // Open on the same tileset the area template itself uses, so the default is one the
            // clone path is known to handle.
            SelectedTileset = Tilesets.FirstOrDefault(t => t.ResRef.Equals("tms01", StringComparison.OrdinalIgnoreCase))
                              ?? Tilesets.FirstOrDefault();
        }

        /// <summary>Whether the module can be written to right now; null when nothing gates it.</summary>
        private readonly Func<bool>? _canWrite;

        [RelayCommand]
        private void Create()
        {
            // Asked at the moment of the write, not when the wizard opened: the builder can have this
            // dialog on screen for minutes, and a pack that starts in between would otherwise copy an
            // ARE/GIT/GIC triplet mid-creation or a module.ifo that does not yet list it.
            if (_canWrite?.Invoke() == false)
            {
                StatusMessage = "Areas cannot be created while the module is being packed or built.";
                return;
            }

            if (!double.IsFinite(Width) ||
                !double.IsFinite(Height) ||
                Width != Math.Truncate(Width) ||
                Height != Math.Truncate(Height))
            {
                StatusMessage = "Area width and height must be whole numbers.";
                return;
            }

            NewAreaWriter.TilesetResolver? resolver = _tilesetCatalog == null
                ? null
                : (string resRef, out TilesetDefinition tileset) => _tilesetCatalog.TryGetTileset(resRef, out tileset);

            if (NewAreaWriter.TryCreate(
                    _workspace, resolver, ResRef, DisplayName, SelectedTileset?.ResRef ?? string.Empty,
                    (int)Width, (int)Height, out var error))
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
