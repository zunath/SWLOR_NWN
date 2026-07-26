using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// The Appearance tab: search the placeable models, look at one, use it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Performance is inherited from the palette rather than reinvented, because the palette's two
    /// mechanisms are exactly what a 24,304-row grid needs. Previews come from
    /// <see cref="ThumbnailService.RequestTileAsync"/>, which is model-resref keyed and sits behind
    /// the same bounded in-memory cache and shared render pool. And the grid is never handed the
    /// whole result set: the palette's own grid is a WrapPanel inside a ListBox, which does not
    /// virtualize, so its speed comes from only ever holding a few hundred tiles at once. This tab
    /// pages for the same reason.
    /// </para>
    /// <para>
    /// Highlighting a tile only previews it. Nothing is written until <c>Use this model</c>, so
    /// browsing never dirties the document.
    /// </para>
    /// </remarks>
    public partial class AppearanceSectionViewModel : ObservableObject
    {
        /// <summary>Tiles added per page. Sized to the palette's own search cap.</summary>
        private const int PageSize = 200;

        private readonly EditorFieldContext _context;
        private readonly PlaceableModelCatalog _catalog;
        private readonly ThumbnailService? _thumbnails;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<PlaceableAppearanceUsageIndex> _usage;

        /// <summary>Builds the render geometry for a model resref; null leaves the 3D view empty.</summary>
        private readonly Func<string, Domain.Render.RenderModel?>? _resolveModel;

        private List<PlaceableModelRow> _matches = new();
        private int _published;

        [ObservableProperty]
        private string _query = string.Empty;

        [ObservableProperty]
        private AppearanceTileViewModel? _highlighted;

        [ObservableProperty]
        private bool _usedInModuleOnly = true;

        [ObservableProperty]
        private bool _namedOnly;

        public AppearanceSectionViewModel(
            EditorFieldContext context,
            PlaceableModelCatalog catalog,
            ThumbnailService? thumbnails,
            Func<PlaceableAppearanceUsageIndex> usage,
            Func<string, Action, bool> runEdit,
            Domain.GameData.Resources.ResourceIndex? resourceIndex = null,
            Func<string, Domain.Render.RenderModel?>? resolveModel = null)
        {
            _context = context;
            _catalog = catalog;
            _thumbnails = thumbnails;
            _usage = usage;
            _runEdit = runEdit;
            _resolveModel = resolveModel;
            ResourceIndex = resourceIndex;

            Rebuild();
            UpdatePreviewScene();
        }

        /// <summary>Texture/material layer the 3D preview resolves through; null renders untextured.</summary>
        public Domain.GameData.Resources.ResourceIndex? ResourceIndex { get; }

        /// <summary>
        /// A one-model scene for the interactive 3D view: whatever tile is highlighted, or the
        /// model the placeable stores when nothing is.
        /// </summary>
        /// <remarks>
        /// Reusing the area viewport rather than writing a second GL control means the preview gets
        /// its orbit, pan, zoom, lighting and texture pipeline for free, and there is only one
        /// renderer to keep working. The scene is an empty 1x1 grid holding a single placeable
        /// marker at the origin, which the control's own framing then centres on.
        /// </remarks>
        public Domain.Render.AreaScene? PreviewScene { get; private set; }

        /// <summary>The published page of matching models.</summary>
        public ObservableCollection<AppearanceTileViewModel> Tiles { get; } = new();

        /// <summary>The appearance row the placeable stores right now.</summary>
        public int CurrentId => (int)(_context.Document.Root.GetOrNull("Appearance")?.GetInteger() ?? 0);

        public string CurrentDescription
        {
            get
            {
                if (_catalog.TryGet(CurrentId, out var row))
                    return $"{row.DisplayName}  ·  row {row.Id}  ·  {row.ModelName}";

                // A row with no model and no label. 2,982 blueprints are on one of these, and the
                // value stays exactly as stored - the editor says so rather than refusing to open.
                return $"row {CurrentId} - no model in placeables.2da";
            }
        }

        public bool CurrentIsUnknown => !_catalog.TryGet(CurrentId, out _);

        public string MatchSummary
        {
            get
            {
                if (_matches.Count == 0)
                    return "No models match";

                return _published >= _matches.Count
                    ? $"{_matches.Count} model{(_matches.Count == 1 ? string.Empty : "s")}"
                    : $"{_published} of {_matches.Count} models";
            }
        }

        public bool CanLoadMore => _published < _matches.Count;

        public bool HasHighlight => Highlighted != null;

        /// <summary>Raised after the stored appearance changes, so the preview can re-resolve.</summary>
        public event Action? AppearanceChanged;

        [RelayCommand]
        private void LoadMore() => PublishPage();

        /// <summary>Writes the highlighted model as this placeable's appearance.</summary>
        [RelayCommand]
        private void UseHighlighted()
        {
            var tile = Highlighted;
            if (tile == null || tile.Id == CurrentId)
                return;

            if (!_runEdit($"Change appearance to {tile.Caption}", () => WriteAppearance(tile.Id)))
                return;

            foreach (var published in Tiles)
                published.IsCurrent = published.Id == tile.Id;

            NotifyCurrentChanged();
            AppearanceChanged?.Invoke();
        }

        /// <summary>Re-reads the stored appearance after an undo, redo or reload.</summary>
        public void RefreshFromDocument()
        {
            var current = CurrentId;
            foreach (var tile in Tiles)
                tile.IsCurrent = tile.Id == current;

            NotifyCurrentChanged();
        }

        partial void OnQueryChanged(string value) => Rebuild();

        partial void OnUsedInModuleOnlyChanged(bool value) => Rebuild();

        partial void OnNamedOnlyChanged(bool value) => Rebuild();

        partial void OnHighlightedChanged(AppearanceTileViewModel? value)
        {
            OnPropertyChanged(nameof(HasHighlight));
            UpdatePreviewScene();
        }

        /// <summary>Rebuilds the single-model scene for whatever should be on screen right now.</summary>
        private void UpdatePreviewScene()
        {
            var modelName = Highlighted?.ModelName;
            if (modelName == null && _catalog.TryGet(CurrentId, out var currentRow))
                modelName = currentRow.ModelName;

            var model = modelName != null ? _resolveModel?.Invoke(modelName) : null;

            PreviewScene = model == null
                ? null
                : new Domain.Render.AreaScene
                {
                    Tileset = string.Empty,
                    Width = 1,
                    Height = 1,
                    Tiles = Array.Empty<Domain.Render.TilePlacement>(),
                    Instances = new[]
                    {
                        new Domain.Render.InstanceMarker
                        {
                            Kind = Domain.Render.InstanceMarkerKind.Placeable,
                            TemplateResRef = modelName!,
                            Tag = string.Empty,
                            Position = new System.Numerics.Vector3(
                                Domain.Render.AreaSceneBuilder.TileSize / 2f,
                                Domain.Render.AreaSceneBuilder.TileSize / 2f,
                                0f),
                            Orientation = new System.Numerics.Vector2(1f, 0f),
                            Model = model
                        }
                    },
                    Diagnostics = new Domain.Render.AreaSceneDiagnostics()
                };

            OnPropertyChanged(nameof(PreviewScene));
        }

        private void Rebuild()
        {
            var usage = _usage();
            var matches = _catalog.Search(Query);

            if (NamedOnly)
                matches = matches.Where(row => row.HasLabel);

            // Only a built index can filter on usage; before the scan lands this would hide
            // everything, which reads as an empty table rather than a pending count.
            if (UsedInModuleOnly && usage.IsBuilt)
                matches = matches.Where(row => usage.CountFor(row.Id) > 0);

            _matches = matches.ToList();
            _published = 0;
            Tiles.Clear();
            PublishPage();
        }

        private void PublishPage()
        {
            var usage = _usage();
            var current = CurrentId;
            var end = Math.Min(_published + PageSize, _matches.Count);

            for (var index = _published; index < end; index++)
            {
                var row = _matches[index];
                var tile = new AppearanceTileViewModel(row, usage.CountFor(row.Id))
                {
                    IsCurrent = row.Id == current
                };

                Tiles.Add(tile);
                RequestPreview(tile);
            }

            _published = end;
            OnPropertyChanged(nameof(MatchSummary));
            OnPropertyChanged(nameof(CanLoadMore));
        }

        private void RequestPreview(AppearanceTileViewModel tile)
        {
            if (_thumbnails == null)
                return;

            var cached = _thumbnails.CachedTile(tile.ModelName);
            if (cached != null)
            {
                tile.Preview = cached;
                return;
            }

            _thumbnails.RequestTileAsync(tile.ModelName, bitmap => tile.Preview = bitmap);
        }

        private void WriteAppearance(int id)
        {
            var field = _context.Document.Root.GetOrNull("Appearance");
            if (field == null)
            {
                var raw = Encoding.ASCII.GetBytes(id.ToString(CultureInfo.InvariantCulture));
                _context.Document.Root.Add("Appearance", JsonGffField.CreateScalar(GffFieldType.Dword, raw));
                return;
            }

            field.SetInteger(id);
        }

        private void NotifyCurrentChanged()
        {
            OnPropertyChanged(nameof(CurrentId));
            OnPropertyChanged(nameof(CurrentDescription));
            OnPropertyChanged(nameof(CurrentIsUnknown));
        }
    }
}
