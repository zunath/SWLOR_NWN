using System.ComponentModel;
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
            DataContextChanged += (_, _) => AttachViewModel();
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
        private void OnPlacementPointPicked(Vector3 position) => _viewModel?.CommitPlacement(position);

        /// <summary>A pending placement was cancelled (Esc or right-click in the viewport).</summary>
        private void OnPlacementCancelled() => _viewModel?.CancelPlacement();

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
