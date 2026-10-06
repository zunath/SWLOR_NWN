using Nwn.Authoring.Areas.Placement;
using Nwn.Toolset.Avalonia.Areas;
using Avalonia.Interactivity;
using System.ComponentModel;
using System.Numerics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors
{
    public partial class AreaEditorView : UserControl
    {
        private AreaEditorViewModel? _viewModel;
        private Viewport.SwlorAreaViewportMaterialProvider? _materialProvider;
        private SelectionContextMenuState? _selectionMenuState;
        private bool _viewportStateRestored;
        private AreaEditorSurface AreaView => SceneView.Surface;

        public AreaEditorView()
        {
            InitializeComponent();
            AreaView.ContextRequested += OnViewportContextRequested;
            SceneView.RaiseTileRequested += (sender, args) => _viewModel?.RaiseTileCommand.Execute(null);
            SceneView.LowerTileRequested += (sender, args) => _viewModel?.LowerTileCommand.Execute(null);
            AreaView.Viewport.InstancePicked += OnInstancePicked;
            AreaView.Viewport.InstanceMoved += OnInstanceMoved;
            AreaView.Viewport.InstanceRotated += OnInstanceRotated;
            AreaView.Viewport.PlacementPointPicked += OnPlacementPointPicked;
            AreaView.Viewport.PlacementCancelled += OnPlacementCancelled;
            AreaView.Viewport.TileCellPicked += OnTileCellPicked;
            AreaView.Viewport.TileEdgePicked += OnTileEdgePicked;
            AreaView.Viewport.TileSelected += OnTileSelected;
            AreaView.Viewport.TilePlacementCancelled += OnTilePlacementCancelled;
            AreaView.Viewport.TileRotateRequested += OnTileRotateRequested;
            DataContextChanged += (_, _) => AttachViewModel();

            // Display switches are global, not per-area (Aurora treats them the same way), so the
            // view takes them straight from the shared options object rather than through its own
            // view model - two open areas disagreeing about fog would only be confusing.
            _display = Avalonia.Application.Current is App app ? app.Services?.GetService(
                typeof(Viewport.ViewportDisplayOptions)) as Viewport.ViewportDisplayOptions : null;
            ApplyDisplayOptions();
        }

        private readonly Viewport.ViewportDisplayOptions? _display;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (_display != null)
                _display.PropertyChanged += OnDisplayPropertyChanged;

            ApplyDisplayOptions();
            AttachViewModel();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (_display != null)
                _display.PropertyChanged -= OnDisplayPropertyChanged;
            if (_viewModel != null)
            {
                SaveViewState(_viewModel);
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.CameraFocusRequested -= OnCameraFocusRequested;
                _viewModel.InstancePropertiesRequested -= OnInstancePropertiesRequested;
                _viewModel.PaintRejected -= OnPaintRejected;
            }
            DetachSelectionContextMenu();

            _viewModel = null;

            base.OnDetachedFromVisualTree(e);
        }

        private void OnDisplayPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
            ApplyDisplayOptions();

        private void ApplyDisplayOptions()
        {
            if (_display == null)
                return;

            AreaView.Viewport.ShowAreaLighting = _display.ShowAreaLighting;
            AreaView.Viewport.ShowFog = _display.ShowFog;
            AreaView.Viewport.ShowCeilings = _display.ShowCeilings;
            AreaView.Viewport.ShowMaterialMaps = _display.ShowMaterialMaps;
        }

        private void AttachViewModel()
        {
            var next = DataContext as AreaEditorViewModel;
            if (ReferenceEquals(next, _viewModel))
                return;

            if (_viewModel != null)
            {
                SaveViewState(_viewModel);
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.CameraFocusRequested -= OnCameraFocusRequested;
                _viewModel.InstancePropertiesRequested -= OnInstancePropertiesRequested;
                _viewModel.PaintRejected -= OnPaintRejected;
            }
            DetachSelectionContextMenu();

            _viewModel = next;
            UpdateSceneOverlay();
            if (_viewModel == null)
                return;

            _selectionMenuState = new SelectionContextMenuState(_viewModel);
            SceneView.SurfaceContextMenu = new AreaSelectionContextMenu(_selectionMenuState);
            _viewportStateRestored = false;

            _materialProvider = _viewModel.ResourceIndex is { } resources
                ? new Viewport.SwlorAreaViewportMaterialProvider(resources)
                : null;
            AreaView.Viewport.MaterialProvider = _materialProvider;
            AreaView.Viewport.MeshMetadataProvider = _materialProvider;
            AreaView.Viewport.InvalidateGameResources();
            AreaView.Viewport.Scene = _viewModel.AreaScene;
            RestoreViewportStateWhenReady();
            AreaView.Viewport.SelectedInstance = _viewModel.SelectedSceneInstance;
            AreaView.Viewport.PlacementGhost = _viewModel.PlacementGhost;
            AreaView.Viewport.IsPlacementActive = _viewModel.IsPlacementPending;
            AreaView.Viewport.IsTilePlacementActive = _viewModel.IsTilePlacementPending;
            AreaView.Viewport.TilePlacementTargetsVertex = _viewModel.TilePlacementTargetsVertex;
            AreaView.Viewport.TilePlacementTargetsEdge = _viewModel.TilePlacementTargetsEdge;
            AreaView.Viewport.TilePlacementFootprint = _viewModel.TilePlacementFootprint;
            AreaView.Viewport.TilePlacementModels = _viewModel.TilePlacementModels;
            AreaView.Viewport.TilePlacementValidator = _viewModel.CanPlaceArmedTileAt;
            AreaView.Viewport.TilePlacementEdgeValidator = _viewModel.CanPlaceArmedCrosserAt;
            AreaView.Viewport.SelectedTileCell = _viewModel.SelectedTile;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.CameraFocusRequested += OnCameraFocusRequested;
            _viewModel.InstancePropertiesRequested += OnInstancePropertiesRequested;
            _viewModel.PaintRejected += OnPaintRejected;

            ConsumePendingCameraFocus();

            // Opening an area shows its map. Not gated on the 3D View tab being selected: it always is
            // (it is the first tab), and reading IsSelected here raced the TabControl's own setup - the
            // case where a second area opened to an empty viewport that never built.
            _viewModel.EnsureSceneBuilt();

            // Layout owns the scrollable extent, so wait until this view has measured before
            // restoring the document's last offset.
            Dispatcher.UIThread.Post(() =>
            {
                if (_viewModel != null)
                {
                    var offset = _viewModel.PropertiesScrollOffset;
                    PropertiesScroll.Offset = new Avalonia.Vector(offset.X, offset.Y);
                }
            });
        }

        private void SaveViewState(AreaEditorViewModel viewModel)
        {
            viewModel.ViewportState = AreaView.Viewport.CaptureViewportState() ?? viewModel.ViewportState;
            viewModel.PropertiesScrollOffset = new Vector2(
                (float)PropertiesScroll.Offset.X,
                (float)PropertiesScroll.Offset.Y);
        }

        private void OnInstancePropertiesRequested(InstanceListSectionViewModel section)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_viewModel == null)
                    return;

                PropertiesPage.BringSectionIntoView(section);
            }, DispatcherPriority.Render);
        }

        private void RestoreViewportStateWhenReady()
        {
            if (_viewportStateRestored || _viewModel?.AreaScene == null ||
                _viewModel.ViewportState is not { } state)
                return;

            AreaView.Viewport.RestoreViewportState(state);
            _viewportStateRestored = true;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel == null)
                return;

            UpdateSceneOverlay();

            if (e.PropertyName == nameof(AreaEditorViewModel.AreaScene))
            {
                AreaView.Viewport.Scene = _viewModel.AreaScene;
                RestoreViewportStateWhenReady();
                ConsumePendingCameraFocus();
            }
            else if (e.PropertyName == nameof(AreaEditorViewModel.GameResourceRevision))
            {
                _materialProvider?.Invalidate();
                AreaView.Viewport.InvalidateGameResources();
            }
            else if (e.PropertyName == nameof(AreaEditorViewModel.SelectedSceneInstance))
                AreaView.Viewport.SelectedInstance = _viewModel.SelectedSceneInstance;
            else if (e.PropertyName == nameof(AreaEditorViewModel.IsPlacementPending))
                AreaView.Viewport.IsPlacementActive = _viewModel.IsPlacementPending;
            else if (e.PropertyName == nameof(AreaEditorViewModel.PlacementGhost))
                AreaView.Viewport.PlacementGhost = _viewModel.PlacementGhost;
            else if (e.PropertyName == nameof(AreaEditorViewModel.IsTilePlacementPending))
                AreaView.Viewport.IsTilePlacementActive = _viewModel.IsTilePlacementPending;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementTargetsVertex))
                AreaView.Viewport.TilePlacementTargetsVertex = _viewModel.TilePlacementTargetsVertex;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementTargetsEdge))
                AreaView.Viewport.TilePlacementTargetsEdge = _viewModel.TilePlacementTargetsEdge;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementFootprint))
                AreaView.Viewport.TilePlacementFootprint = _viewModel.TilePlacementFootprint;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementModels))
                AreaView.Viewport.TilePlacementModels = _viewModel.TilePlacementModels;
            else if (e.PropertyName == nameof(AreaEditorViewModel.SelectedTile))
                AreaView.Viewport.SelectedTileCell = _viewModel.SelectedTile;
        }

        /// <summary>
        /// The Area Contents tree asked for an object to be shown: bring the map to the front if the
        /// Properties tab is, then send the camera.
        /// </summary>
        /// <remarks>
        /// The tab switch is not optional. Double-clicking a row while Properties is in front would
        /// otherwise move a camera nobody can see, which reads as the row having done nothing.
        /// </remarks>
        private void OnCameraFocusRequested(Vector3 _)
        {
            ConsumePendingCameraFocus();
        }

        /// <summary>
        /// Takes a retained request from the document only after its normal scene-change path has
        /// restored the area's saved camera. Until then the document keeps the position.
        /// </summary>
        private void ConsumePendingCameraFocus()
        {
            // Leave the request on the document until a scene exists. That makes it survive a tab
            // swap while a large area is still loading; the next view consumes it only after it has
            // restored this area's retained camera.
            if (AreaView.Viewport.Scene == null ||
                _viewModel?.TryTakePendingCameraFocus(out var position) != true)
                return;

            ApplyCameraFocus(position);
        }

        private void ApplyCameraFocus(Vector3 position)
        {
            if (_viewModel != null)
                _viewModel.SelectedRootTabIndex = 0;

            AreaView.Viewport.FocusOn(position);
        }

        /// <summary>
        /// Area-object shortcuts that remain after the focused control has had first refusal. Delete
        /// removes the selection; Ctrl+C snapshots it; Ctrl+V arms that snapshot on the map cursor.
        /// An inapplicable shortcut is not handled, so it may continue to a field or grid that wants it.
        /// </summary>
        /// <remarks>
        /// On the view rather than the GL control because the control is only hit-testable through
        /// the transparent input overlay and never takes focus from a click; this sees the key
        /// wherever it lands in the editor, and a focused TextBox has already consumed its own.
        /// </remarks>
        protected override void OnKeyDown(Avalonia.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || _viewModel == null)
                return;

            if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control)
            {
                if (e.Key == Avalonia.Input.Key.C)
                    e.Handled = _viewModel.CopySelectedSceneInstance();
                else if (e.Key == Avalonia.Input.Key.V)
                    e.Handled = _viewModel.PasteCopiedSceneInstance();

                if (e.Handled)
                    return;
            }

            if (e.Key == Avalonia.Input.Key.Delete)
                e.Handled = _viewModel.DeleteSelectedSceneInstance();
        }

        /// <summary>
        /// A click in the 3D view selects the corresponding instance-list row (and vice
        /// versa - see AreaEditorViewModel.ApplySelection/OnSectionSelectionChanged). Routed through
        /// the view model rather than setting AreaView.Viewport.SelectedInstance directly here, so both
        /// selection directions funnel through the same re-entrancy-guarded code path.
        /// </summary>
        private void OnInstancePicked(InstanceMarker? instance) => _viewModel?.SelectSceneInstance(instance);

        /// <summary>The move gizmo released with a net change - commit it through the view model's InstanceFieldMap-based path.</summary>
        private void OnInstanceMoved(InstanceMarker instance, Vector3 newPosition) =>
            _viewModel?.MoveSelectedInstance(instance, newPosition);

        /// <summary>The rotate gizmo released with a net change.</summary>
        private void OnInstanceRotated(InstanceMarker instance, Vector2 newOrientation) =>
            _viewModel?.RotateSelectedInstance(instance, newOrientation);

        /// <summary>A pending placement resolved to a viewport click.</summary>
        private void OnPlacementPointPicked(PlacementPick pick) =>
            _viewModel?.CommitPlacement(pick.Position, pick.Orientation);

        /// <summary>A pending placement was cancelled (Esc or right-click in the viewport).</summary>
        private void OnPlacementCancelled() => _viewModel?.CancelPlacement();

        /// <summary>An armed tile stamp resolved to a grid cell - the anchor is its bottom-left corner.</summary>
        private void OnTileCellPicked(int column, int row) => _viewModel?.CommitTilePlacement(column, row);

        private void OnTileEdgePicked(int column, int row, bool vertical) =>
            _viewModel?.CommitCrosserPaint(column, row, vertical);

        /// <summary>A paint click the solver declined - answer it on the map, where the builder is looking.</summary>
        private void OnPaintRejected() => AreaView.Viewport.FlashPaintRejection();

        /// <summary>A click on open ground selected a grid cell (or cleared the selection).</summary>
        private void OnTileSelected((int Column, int Row)? cell) => _viewModel?.SelectTile(cell);

        /// <summary>An armed tile stamp was cancelled (Esc or right-click in the viewport).</summary>
        private void OnTilePlacementCancelled() => _viewModel?.CancelTilePlacement();

        /// <summary>R was pressed with a tile armed - turn it before it is stamped.</summary>
        private void OnTileRotateRequested() => _viewModel?.RotatePendingTile();

        private void UpdateSceneOverlay()
        {
            SceneView.Overlay = _viewModel is { } model ? new AreaSceneOverlay
            {
                IsBuildingScene = model.IsBuildingScene,
                SceneStatus = model.SceneStatus,
                HasSceneSelection = model.HasSceneSelection,
                HasTileSelection = model.HasTileSelection,
                TileSelectionStatus = model.TileSelectionStatus,
                PlacementStatus = model.PlacementStatus,
                CanRotateSelection = model.CanRotateSelection,
            } : new();
        }

        /// <summary>
        /// Opens the viewport's context menu only when the right-click actually landed on something.
        /// </summary>
        /// <remarks>
        /// The press handler has already resolved the pick by the time this runs, so the menu either
        /// describes the object under the cursor or must not open at all - a menu naming the previous
        /// selection would act on something the builder is no longer pointing at.
        /// </remarks>
        private void OnViewportContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (_viewModel?.HasSceneSelection != true)
                e.Handled = true;
        }

        private void DetachSelectionContextMenu()
        {
            _selectionMenuState?.Dispose();
            _selectionMenuState = null;
            SceneView.SurfaceContextMenu = null;
        }

        private sealed class SelectionContextMenuState : IAreaSelectionContextMenuState, IDisposable
        {
            private readonly AreaEditorViewModel _viewModel;
            public event PropertyChangedEventHandler? PropertyChanged;

            public string SelectionName => _viewModel.SelectionName;
            public string SelectionGlyph => _viewModel.SelectionGlyph;
            public string SelectionKindLabel => _viewModel.SelectionKindLabel;
            public string SelectionResRef => _viewModel.SelectionResRef;
            public bool CanOpenProperties => OpenPropertiesCommand.CanExecute(null);
            public bool CanEditBlueprint => EditBlueprintCommand.CanExecute(null);
            public bool CanEditCopy => EditCopyCommand.CanExecute(null);
            public ICommand OpenPropertiesCommand => _viewModel.OpenSelectedInstancePropertiesCommand;
            public ICommand EditBlueprintCommand => _viewModel.EditSelectedBlueprintCommand;
            public ICommand EditCopyCommand => _viewModel.EditCopySelectedBlueprintCommand;

            public SelectionContextMenuState(AreaEditorViewModel viewModel)
            {
                _viewModel = viewModel;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                OpenPropertiesCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
                EditBlueprintCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
                EditCopyCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
            }

            public void Dispose()
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                OpenPropertiesCommand.CanExecuteChanged -= OnCommandCanExecuteChanged;
                EditBlueprintCommand.CanExecuteChanged -= OnCommandCanExecuteChanged;
                EditCopyCommand.CanExecuteChanged -= OnCommandCanExecuteChanged;
            }

            private void OnCommandCanExecuteChanged(object? sender, EventArgs args)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanOpenProperties)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditBlueprint)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditCopy)));
            }

            private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName is nameof(AreaEditorViewModel.SelectionName)
                    or nameof(AreaEditorViewModel.SelectionGlyph)
                    or nameof(AreaEditorViewModel.SelectionKindLabel)
                    or nameof(AreaEditorViewModel.SelectionResRef))
                    PropertyChanged?.Invoke(this, args);
            }
        }
    }
}
