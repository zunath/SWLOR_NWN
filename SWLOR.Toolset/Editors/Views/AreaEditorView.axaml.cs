using Avalonia.Interactivity;
using System.ComponentModel;
using System.Numerics;
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
        private bool _viewportStateRestored;

        public AreaEditorView()
        {
            InitializeComponent();
            AreaView.RenderStatusChanged += OnGlRenderStatusChanged;
            AreaView.InstancePicked += OnInstancePicked;
            AreaView.InstanceMoved += OnInstanceMoved;
            AreaView.InstanceRotated += OnInstanceRotated;
            AreaView.ManipulationPreviewChanged += OnManipulationPreviewChanged;
            AreaView.PlacementPointPicked += OnPlacementPointPicked;
            AreaView.PlacementCancelled += OnPlacementCancelled;
            AreaView.TileCellPicked += OnTileCellPicked;
            AreaView.TileEdgePicked += OnTileEdgePicked;
            AreaView.TileSelected += OnTileSelected;
            AreaView.TilePlacementCancelled += OnTilePlacementCancelled;
            AreaView.TileRotateRequested += OnTileRotateRequested;
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

            _viewModel = null;

            base.OnDetachedFromVisualTree(e);
        }

        private void OnDisplayPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
            ApplyDisplayOptions();

        private void ApplyDisplayOptions()
        {
            if (_display == null)
                return;

            AreaView.ShowAreaLighting = _display.ShowAreaLighting;
            AreaView.ShowFog = _display.ShowFog;
            AreaView.ShowCeilings = _display.ShowCeilings;
            AreaView.ShowMaterialMaps = _display.ShowMaterialMaps;
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

            _viewModel = next;
            if (_viewModel == null)
                return;

            _viewportStateRestored = false;

            AreaView.ResourceIndex = _viewModel.ResourceIndex;
            AreaView.InvalidateGameResources();
            AreaView.Scene = _viewModel.AreaScene;
            RestoreViewportStateWhenReady();
            AreaView.SelectedInstance = _viewModel.SelectedSceneInstance;
            AreaView.PlacementGhost = _viewModel.PlacementGhost;
            AreaView.IsPlacementActive = _viewModel.IsPlacementPending;
            AreaView.IsTilePlacementActive = _viewModel.IsTilePlacementPending;
            AreaView.TilePlacementTargetsVertex = _viewModel.TilePlacementTargetsVertex;
            AreaView.TilePlacementTargetsEdge = _viewModel.TilePlacementTargetsEdge;
            AreaView.TilePlacementFootprint = _viewModel.TilePlacementFootprint;
            AreaView.TilePlacementModels = _viewModel.TilePlacementModels;
            AreaView.TilePlacementValidator = _viewModel.CanPlaceArmedTileAt;
            AreaView.TilePlacementEdgeValidator = _viewModel.CanPlaceArmedCrosserAt;
            AreaView.SelectedTileCell = _viewModel.SelectedTile;
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
            viewModel.ViewportState = AreaView.CaptureViewportState() ?? viewModel.ViewportState;
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

                PropertiesScroll
                    .GetVisualDescendants()
                    .OfType<Expander>()
                    .FirstOrDefault(expander => ReferenceEquals(expander.DataContext, section))
                    ?.BringIntoView();
            }, DispatcherPriority.Render);
        }

        private void RestoreViewportStateWhenReady()
        {
            if (_viewportStateRestored || _viewModel?.AreaScene == null ||
                _viewModel.ViewportState is not { } state)
                return;

            AreaView.RestoreViewportState(state);
            _viewportStateRestored = true;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel == null)
                return;

            if (e.PropertyName == nameof(AreaEditorViewModel.AreaScene))
            {
                AreaView.Scene = _viewModel.AreaScene;
                RestoreViewportStateWhenReady();
                ConsumePendingCameraFocus();
            }
            else if (e.PropertyName == nameof(AreaEditorViewModel.GameResourceRevision))
                AreaView.InvalidateGameResources();
            else if (e.PropertyName == nameof(AreaEditorViewModel.SelectedSceneInstance))
                AreaView.SelectedInstance = _viewModel.SelectedSceneInstance;
            else if (e.PropertyName == nameof(AreaEditorViewModel.IsPlacementPending))
                AreaView.IsPlacementActive = _viewModel.IsPlacementPending;
            else if (e.PropertyName == nameof(AreaEditorViewModel.PlacementGhost))
                AreaView.PlacementGhost = _viewModel.PlacementGhost;
            else if (e.PropertyName == nameof(AreaEditorViewModel.IsTilePlacementPending))
                AreaView.IsTilePlacementActive = _viewModel.IsTilePlacementPending;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementTargetsVertex))
                AreaView.TilePlacementTargetsVertex = _viewModel.TilePlacementTargetsVertex;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementTargetsEdge))
                AreaView.TilePlacementTargetsEdge = _viewModel.TilePlacementTargetsEdge;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementFootprint))
                AreaView.TilePlacementFootprint = _viewModel.TilePlacementFootprint;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementModels))
                AreaView.TilePlacementModels = _viewModel.TilePlacementModels;
            else if (e.PropertyName == nameof(AreaEditorViewModel.SelectedTile))
                AreaView.SelectedTileCell = _viewModel.SelectedTile;
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
            if (AreaView.Scene == null ||
                _viewModel?.TryTakePendingCameraFocus(out var position) != true)
                return;

            ApplyCameraFocus(position);
        }

        private void ApplyCameraFocus(Vector3 position)
        {
            if (_viewModel != null)
                _viewModel.SelectedRootTabIndex = 0;

            AreaView.FocusOn(position);
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
        /// the view model rather than setting AreaView.SelectedInstance directly here, so both
        /// selection directions funnel through the same re-entrancy-guarded code path.
        /// </summary>
        private void OnInstancePicked(InstanceMarker? instance) => _viewModel?.SelectSceneInstance(instance);

        /// <summary>Feeds the drag readout beside the map; both null when the drag ends.</summary>
        private void OnManipulationPreviewChanged(InstanceMarker? original, InstanceMarker? preview) =>
            _viewModel?.ShowDragReadout(original, preview);

        /// <summary>The move gizmo released with a net change - commit it through the view model's InstanceFieldMap-based path.</summary>
        private void OnInstanceMoved(InstanceMarker instance, Vector3 newPosition) =>
            _viewModel?.MoveSelectedInstance(instance, newPosition);

        /// <summary>The rotate gizmo released with a net change.</summary>
        private void OnInstanceRotated(InstanceMarker instance, Vector2 newOrientation) =>
            _viewModel?.RotateSelectedInstance(instance, newOrientation);

        /// <summary>A pending placement resolved to a viewport click.</summary>
        private void OnPlacementPointPicked(Viewport.PlacementPick pick) =>
            _viewModel?.CommitPlacement(pick.Position, pick.Orientation);

        /// <summary>A pending placement was cancelled (Esc or right-click in the viewport).</summary>
        private void OnPlacementCancelled() => _viewModel?.CancelPlacement();

        // ----- Object rotate. Held, these spin the selection continuously; a tap turns one step.
        // Both go through the viewport's live preview, so the scene is not rebuilt per tick and the
        // whole turn is a single undo entry - see GlAreaControl.NudgeSelectedRotation. -----

        /// <summary>Whether this press has repeated yet - the first tick is the tap step, the rest are the glide.</summary>
        private bool _rotateHasRepeated;

        private void OnRotateSelectionClockwise(object? sender, RoutedEventArgs e) => RotateSelectionTick(-1f);

        private void OnRotateSelectionAnticlockwise(object? sender, RoutedEventArgs e) => RotateSelectionTick(1f);

        private void RotateSelectionTick(float direction)
        {
            AreaView.NudgeSelectedRotation(direction, isFirstStep: !_rotateHasRepeated);
            _rotateHasRepeated = true;
        }

        private void OnRotateSelectionReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e) => EndRotateSelection();

        /// <summary>
        /// Losing the pointer capture ends the rotation too. Without it a press dragged off the button
        /// never releases on it, and the turn would sit uncommitted until something else flushed it.
        /// </summary>
        private void OnRotateSelectionCaptureLost(object? sender, Avalonia.Input.PointerCaptureLostEventArgs e) => EndRotateSelection();

        private void EndRotateSelection()
        {
            _rotateHasRepeated = false;
            AreaView.CommitSelectedRotation();
        }

        // ----- Camera pad. These drive the control's own camera, which the view model does not own. -----

        // The arrows move the camera, so the scene travels the other way - Aurora's left arrow sends
        // the scene right, its up arrow sends the scene down. Up and down travel forward and back
        // across the ground rather than changing altitude.
        private void OnPanLeft(object? sender, RoutedEventArgs e) => AreaView.NudgePan(-1f, 0f);

        private void OnPanRight(object? sender, RoutedEventArgs e) => AreaView.NudgePan(1f, 0f);

        private void OnPanUp(object? sender, RoutedEventArgs e) => AreaView.NudgePan(0f, 1f);

        private void OnPanDown(object? sender, RoutedEventArgs e) => AreaView.NudgePan(0f, -1f);

        private void OnOrbitLeft(object? sender, RoutedEventArgs e) => AreaView.NudgeOrbit(-1f, 0f);

        private void OnOrbitRight(object? sender, RoutedEventArgs e) => AreaView.NudgeOrbit(1f, 0f);

        private void OnOrbitUp(object? sender, RoutedEventArgs e) => AreaView.NudgeOrbit(0f, 1f);

        private void OnOrbitDown(object? sender, RoutedEventArgs e) => AreaView.NudgeOrbit(0f, -1f);

        private void OnZoomIn(object? sender, RoutedEventArgs e) => AreaView.NudgeZoom(1);

        private void OnZoomOut(object? sender, RoutedEventArgs e) => AreaView.NudgeZoom(-1);

        private void OnReorient(object? sender, RoutedEventArgs e) => AreaView.ReorientCamera();

        /// <summary>An armed tile stamp resolved to a grid cell - the anchor is its bottom-left corner.</summary>
        private void OnTileCellPicked(int column, int row) => _viewModel?.CommitTilePlacement(column, row);

        private void OnTileEdgePicked(int column, int row, bool vertical) =>
            _viewModel?.CommitCrosserPaint(column, row, vertical);

        /// <summary>A paint click the solver declined - answer it on the map, where the builder is looking.</summary>
        private void OnPaintRejected() => AreaView.FlashPaintRejection();

        /// <summary>A click on open ground selected a grid cell (or cleared the selection).</summary>
        private void OnTileSelected((int Column, int Row)? cell) => _viewModel?.SelectTile(cell);

        /// <summary>An armed tile stamp was cancelled (Esc or right-click in the viewport).</summary>
        private void OnTilePlacementCancelled() => _viewModel?.CancelTilePlacement();

        /// <summary>R was pressed with a tile armed - turn it before it is stamped.</summary>
        private void OnTileRotateRequested() => _viewModel?.RotatePendingTile();

        private void OnGlRenderStatusChanged(object? sender, string message)
        {
            GlStatusBorder.IsVisible = !string.IsNullOrEmpty(message);
            GlStatusText.Text = message;
        }

        // Camera input arrives via the transparent ViewportInput overlay (OpenGlControlBase is not
        // hit-testable itself) and is forwarded to the GL control. After a press the control
        // captures the pointer, so moves/releases route to its own handlers; the overlay only has
        // to deliver the initial press, uncaptured moves, and wheel events.

        private void OnViewportPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e) =>
            AreaView.HandlePointerPressed(e);

        private void OnViewportPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e) =>
            AreaView.HandlePointerMoved(e);

        private void OnViewportPointerExited(object? sender, Avalonia.Input.PointerEventArgs e) =>
            AreaView.HandlePointerExited(e);

        private void OnViewportPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e) =>
            AreaView.HandlePointerReleased(e);

        private void OnViewportPointerWheel(object? sender, Avalonia.Input.PointerWheelEventArgs e) =>
            AreaView.HandlePointerWheel(e);

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
    }
}
