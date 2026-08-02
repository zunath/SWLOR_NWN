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
    /// <summary>One model-declared state as it appears in the preview picker.</summary>
    public sealed record PlaceableAnimationOption(Domain.Render.RenderAnimation Animation)
    {
        public string Name => Animation.Name;

        public string Label => string.IsNullOrEmpty(Name)
            ? "Default"
            : char.ToUpperInvariant(Name[0]) + Name[1..];
    }

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
    public partial class AppearanceSectionViewModel : ObservableObject, IDisposable, Viewport.IModelPreviewSource
    {
        private readonly EditorFieldContext _context;
        private readonly PlaceableModelCatalog _catalog;
        private readonly ThumbnailService? _thumbnails;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<PlaceableAppearanceUsageIndex> _usage;

        /// <summary>Builds the render geometry for a model resref; null leaves the 3D view empty.</summary>
        private readonly Func<string, Domain.Render.RenderModel?>? _resolveModel;

        /// <summary>False until the tab has been shown once; see EnsureLoaded.</summary>
        private bool _loaded;

        /// <summary>
        /// The searchable picture grid, shared with the door and creature editors. This section
        /// keeps only what is genuinely a placeable's: the retained 3D view, the animation states
        /// its model declares, and the two filters that narrow which models are offered at all.
        /// </summary>
        public Appearance.AppearanceGallerySectionViewModel Gallery { get; }

        [ObservableProperty]
        private bool _usedInModuleOnly = true;

        [ObservableProperty]
        private bool _namedOnly;

        /// <summary>True while the model catalog is still being read in the background.</summary>
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private PlaceableAnimationOption? _selectedAnimation;

        [ObservableProperty]
        private bool _isAnimationPlaying = true;

        private bool _isTabVisible;
        private bool _disposed;

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

            Gallery = new Appearance.AppearanceGallerySectionViewModel(
                Array.Empty<Appearance.AppearanceOption>(),
                thumbnails,
                () => CurrentId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Apply,
                noun: "model",
                // 24,304 rows: the grid earns its density here in a way the door and creature
                // tables, with hundreds each, do not.
                tileSize: 92);
            Gallery.PropertyChanged += OnGalleryChanged;

            BeginLoading();
        }

        /// <summary>
        /// Starts building the grid off the UI thread as soon as the editor opens, so the tab is
        /// usually ready before anyone clicks it and the window never waits on it.
        /// </summary>
        /// <remarks>
        /// The expensive half is parsing all 32,090 placeables.2da rows, which is pure work over
        /// game data and safe to do on a pool thread. Only the cheap half - filtering to the first
        /// 48 tiles and resolving the preview model - runs back on the UI thread, where it has to,
        /// because it touches the observable collection the grid is bound to.
        /// </remarks>
        private void BeginLoading()
        {
            // The catalog is a singleton shared by every open editor, so once one of them has read
            // the table the rest have nothing to wait for.
            if (_catalog.IsBuilt)
            {
                _loaded = true;
                Rebuild();
                UpdatePreviewScene();
                return;
            }

            IsLoading = true;

            Task.Run(() =>
            {
                var catalogReady = true;
                try
                {
                    // Forces the catalog's lazy parse here rather than on the first keystroke.
                    _ = _catalog.GetAll();
                }
                catch (Exception)
                {
                    // A catalog that will not build leaves an empty grid, which the tab already
                    // reads as "no models match" - not a reason to take the editor down.
                    catalogReady = false;
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_disposed)
                        return;

                    _loaded = true;
                    if (catalogReady)
                    {
                        Rebuild();
                        UpdatePreviewScene();
                    }
                    IsLoading = false;
                });
            });
        }

        /// <summary>
        /// Nothing to do while the background build is in flight - the tab shows its loading state
        /// and fills in when the scan lands. Kept as the hook the tab selection calls.
        /// </summary>
        public void EnsureLoaded()
        {
        }

        /// <summary>Texture/material layer the 3D preview resolves through; null renders untextured.</summary>
        public Domain.GameData.Resources.ResourceIndex? ResourceIndex { get; }

        private Viewport.ModelPreviewControl? _previewView;

        /// <summary>
        /// The 3D view itself, owned by this section rather than built by the tab's template.
        /// </summary>
        /// <remarks>
        /// A control in a view model is not the usual arrangement, and it is here for one reason:
        /// the docking host rebuilds an editor's view every time you switch documents, so a preview
        /// declared in the template meant a fresh OpenGL control - context, shaders, mesh and
        /// texture uploads - on every switch between two placeables. Holding the instance here
        /// makes that a re-parent instead of a rebuild. Created on first use, so an editor whose
        /// Appearance tab is never opened never makes one.
        /// </remarks>
        public Avalonia.Controls.Control PreviewView
        {
            get
            {
                if (_previewView != null)
                    return _previewView;

                _previewView = new Viewport.ModelPreviewControl { DataContext = this };
                _previewView.SetHostVisible(_isTabVisible);
                return _previewView;
            }
        }

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

        /// <summary>States declared by the highlighted model, in file order.</summary>
        public ObservableCollection<PlaceableAnimationOption> AnimationStates { get; } = new();

        public bool HasAnimationControls =>
            AnimationStates.Count > 1 || AnimationStates.Any(option => option.Animation.IsPlayable);

        public bool HasAnimationStatePicker => AnimationStates.Count > 1;

        public bool CanAnimateSelected => SelectedAnimation?.Animation.IsPlayable == true;

        public string AnimationToggleText => IsAnimationPlaying ? "Pause" : "Play";

        public string? PreviewAnimationName => SelectedAnimation?.Name;

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

        public bool HasHighlight => Gallery.Highlighted != null;

        /// <summary>The highlighted model's own name, for the panel beside the 3D view.</summary>
        public string? HighlightedModelName => Gallery.Highlighted?.Option.ModelResRef;

        public string? HighlightedCaption => Gallery.Highlighted?.Caption;

        /// <summary>Raised after the stored appearance changes, so the preview can re-resolve.</summary>
        public event Action? AppearanceChanged;

        [RelayCommand]
        private void ToggleAnimation()
        {
            if (CanAnimateSelected)
                IsAnimationPlaying = !IsAnimationPlaying;
        }

        /// <summary>Re-reads the stored appearance after an undo, redo or reload.</summary>
        public void RefreshFromDocument()
        {
            Gallery.ReloadFromDocument();
            NotifyCurrentChanged();
        }

        /// <summary>
        /// Rebuilds the offered models and usage labels after the module-wide usage scan changes.
        /// If the model table is still loading, its normal completion rebuild will use the new index.
        /// </summary>
        public void RefreshUsage()
        {
            if (_loaded)
                Rebuild();
        }

        partial void OnUsedInModuleOnlyChanged(bool value)
        {
            if (_loaded)
                Rebuild();
        }

        partial void OnNamedOnlyChanged(bool value)
        {
            if (_loaded)
                Rebuild();
        }

        /// <summary>Follows the grid's highlight, which is what the 3D view is showing.</summary>
        private void OnGalleryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not nameof(Gallery.Highlighted))
                return;

            OnPropertyChanged(nameof(HasHighlight));
            OnPropertyChanged(nameof(HighlightedModelName));
            OnPropertyChanged(nameof(HighlightedCaption));
            UpdatePreviewScene();
        }

        partial void OnSelectedAnimationChanged(PlaceableAnimationOption? value)
        {
            IsAnimationPlaying = value?.Animation.IsPlayable == true;
            OnPropertyChanged(nameof(CanAnimateSelected));
            OnPropertyChanged(nameof(PreviewAnimationName));
        }

        partial void OnIsAnimationPlayingChanged(bool value) =>
            OnPropertyChanged(nameof(AnimationToggleText));

        /// <summary>
        /// Picking a model IS the edit. A confirm button in between only asks a builder to say twice
        /// what they already said once, and undo is the real safety net either way.
        /// </summary>
        private bool Apply(Appearance.AppearanceOption option)
        {
            if (!int.TryParse(option.Key, out var id) || id == CurrentId)
                return false;

            if (!_runEdit($"Change appearance to {option.Caption}", () => WriteAppearance(id)))
                return false;

            NotifyCurrentChanged();
            AppearanceChanged?.Invoke();
            return true;
        }

        /// <summary>Rebuilds the single-model scene for whatever should be on screen right now.</summary>
        private void UpdatePreviewScene()
        {
            if (_disposed)
                return;

            var modelName = Gallery.Highlighted?.Option.ModelResRef;
            if (modelName == null && _catalog.TryGet(CurrentId, out var currentRow))
                modelName = currentRow.ModelName;

            var model = modelName != null ? _resolveModel?.Invoke(modelName) : null;
            PublishAnimationStates(model);

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

        public void ReloadGameResources()
        {
            if (_disposed)
                return;

            _loaded = false;
            BeginLoading();
        }

        private void PublishAnimationStates(Domain.Render.RenderModel? model)
        {
            AnimationStates.Clear();
            if (model != null)
            {
                foreach (var animation in model.Animations)
                    AnimationStates.Add(new PlaceableAnimationOption(animation));
            }

            SelectedAnimation = model?.DefaultAnimationName is { } preferred
                ? AnimationStates.FirstOrDefault(
                    option => string.Equals(option.Name, preferred, StringComparison.OrdinalIgnoreCase))
                : AnimationStates.FirstOrDefault();

            OnPropertyChanged(nameof(HasAnimationControls));
            OnPropertyChanged(nameof(HasAnimationStatePicker));
            OnPropertyChanged(nameof(CanAnimateSelected));
        }

        /// <summary>
        /// Called by the owning editor while its retained left-rail preview is hosted. The view is
        /// held by this view model across document switches, so visibility has to be forwarded
        /// explicitly instead of inferred from construction.
        /// </summary>
        public void SetTabVisible(bool visible)
        {
            _isTabVisible = visible && !_disposed;
            _previewView?.SetHostVisible(_isTabVisible);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Gallery.PropertyChanged -= OnGalleryChanged;
            Gallery.Dispose();
            _previewView?.Dispose();
            _previewView = null;
        }

        /// <summary>
        /// Republishes which models the grid offers. The two filters change the set rather than the
        /// query, so they cannot be folded into the search text: a builder's own words have to stay
        /// visible in the box.
        /// </summary>
        private void Rebuild()
        {
            if (_disposed)
                return;

            var usage = _usage();
            IEnumerable<PlaceableModelRow> rows = _catalog.GetAll();

            if (NamedOnly)
                rows = rows.Where(row => row.HasLabel);

            // Only a built index can filter on usage; before the scan lands this would hide
            // everything, which reads as an empty table rather than a pending count.
            if (UsedInModuleOnly && usage.IsBuilt)
                rows = rows.Where(row => usage.CountFor(row.Id) > 0);

            Gallery.SetOptions(rows
                .Select(row => new Appearance.AppearanceOption(
                    row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.DisplayName,
                    row.ModelName,
                    ModelResRef: row.ModelName))
                .ToList());
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
