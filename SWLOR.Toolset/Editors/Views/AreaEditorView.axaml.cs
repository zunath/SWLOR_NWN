using Avalonia.Interactivity;
﻿using System.ComponentModel;
using System.Numerics;
using Avalonia.Controls;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors
{
    public partial class AreaEditorView : UserControl
    {
        private AreaEditorViewModel? _viewModel;

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
            AreaView.TilePlacementCancelled += OnTilePlacementCancelled;
            AreaView.TileRotateRequested += OnTileRotateRequested;
            DataContextChanged += (_, _) => AttachViewModel();

            // Display switches are global, not per-area (Aurora treats them the same way), so the
            // view takes them straight from the shared options object rather than through its own
            // view model - two open areas disagreeing about fog would only be confusing.
            _display = Avalonia.Application.Current is App app ? app.Services?.GetService(
                typeof(Viewport.ViewportDisplayOptions)) as Viewport.ViewportDisplayOptions : null;
            if (_display != null)
            {
                _display.PropertyChanged += (_, _) => ApplyDisplayOptions();
                ApplyDisplayOptions();
            }
        }

        private readonly Viewport.ViewportDisplayOptions? _display;

        private void ApplyDisplayOptions()
        {
            if (_display == null)
                return;

            AreaView.ShowAreaLighting = _display.ShowAreaLighting;
            AreaView.ShowFog = _display.ShowFog;
        }

        private void AttachViewModel()
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = DataContext as AreaEditorViewModel;
            if (_viewModel == null)
                return;

            AreaView.ResourceIndex = _viewModel.ResourceIndex;
            AreaView.Scene = _viewModel.AreaScene;
            AreaView.SelectedInstance = _viewModel.SelectedSceneInstance;
            AreaView.IsPlacementActive = _viewModel.IsPlacementPending;
            AreaView.PlacementGhost = _viewModel.PlacementGhost;
            AreaView.IsTilePlacementActive = _viewModel.IsTilePlacementPending;
            AreaView.TilePlacementFootprint = _viewModel.TilePlacementFootprint;
            AreaView.TilePlacementModels = _viewModel.TilePlacementModels;
            AreaView.TilePlacementValidator = _viewModel.CanPlaceArmedTileAt;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Opening an area shows its map. Not gated on the 3D View tab being selected: it always is
            // (it is the first tab), and reading IsSelected here raced the TabControl's own setup - the
            // case where a second area opened to an empty viewport that never built.
            _viewModel.EnsureSceneBuilt();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel == null)
                return;

            if (e.PropertyName == nameof(AreaEditorViewModel.AreaScene))
                AreaView.Scene = _viewModel.AreaScene;
            else if (e.PropertyName == nameof(AreaEditorViewModel.SelectedSceneInstance))
                AreaView.SelectedInstance = _viewModel.SelectedSceneInstance;
            else if (e.PropertyName == nameof(AreaEditorViewModel.IsPlacementPending))
                AreaView.IsPlacementActive = _viewModel.IsPlacementPending;
            else if (e.PropertyName == nameof(AreaEditorViewModel.PlacementGhost))
                AreaView.PlacementGhost = _viewModel.PlacementGhost;
            else if (e.PropertyName == nameof(AreaEditorViewModel.IsTilePlacementPending))
                AreaView.IsTilePlacementActive = _viewModel.IsTilePlacementPending;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementFootprint))
                AreaView.TilePlacementFootprint = _viewModel.TilePlacementFootprint;
            else if (e.PropertyName == nameof(AreaEditorViewModel.TilePlacementModels))
                AreaView.TilePlacementModels = _viewModel.TilePlacementModels;
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

        private void OnViewportPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e) =>
            AreaView.HandlePointerReleased(e);

        private void OnViewportPointerWheel(object? sender, Avalonia.Input.PointerWheelEventArgs e) =>
            AreaView.HandlePointerWheel(e);
    }
}
