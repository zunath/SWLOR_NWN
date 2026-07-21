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
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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
        }

        /// <summary>
        /// WP5.1: a click in the 3D view selects the corresponding instance-list row (and vice
        /// versa - see AreaEditorViewModel.ApplySelection/OnSectionSelectionChanged). Routed through
        /// the view model rather than setting AreaView.SelectedInstance directly here, so both
        /// selection directions funnel through the same re-entrancy-guarded code path.
        /// </summary>
        private void OnInstancePicked(InstanceMarker? instance) => _viewModel?.SelectSceneInstance(instance);

        /// <summary>WP5.2: the move gizmo released with a net change - commit it through the view model's InstanceFieldMap-based path.</summary>
        private void OnInstanceMoved(InstanceMarker instance, Vector3 newPosition) =>
            _viewModel?.MoveSelectedInstance(instance, newPosition);

        /// <summary>WP5.2: the rotate gizmo released with a net change.</summary>
        private void OnInstanceRotated(InstanceMarker instance, Vector2 newOrientation) =>
            _viewModel?.RotateSelectedInstance(instance, newOrientation);

        /// <summary>WP5.2: a pending placement resolved to a viewport click.</summary>
        private void OnPlacementPointPicked(Vector3 position) => _viewModel?.CommitPlacement(position);

        /// <summary>WP5.2: a pending placement was cancelled (Esc or right-click in the viewport).</summary>
        private void OnPlacementCancelled() => _viewModel?.CancelPlacement();

        /// <summary>Builds the 3D scene lazily the first time the "3D View" tab is activated - never on area-editor open.</summary>
        private void OnRootTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // The TabControl raises SelectionChanged while the XAML is still populating
            // (during InitializeComponent), before named fields are assigned - guard both.
            if (ViewTab3D?.IsSelected == true)
                _viewModel?.EnsureSceneBuilt();
        }

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

        private void OnHideCeilingsChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (AreaView != null && HideCeilingsCheck != null)
                AreaView.HideCeilings = HideCeilingsCheck.IsChecked == true;
        }

        private void OnShowWalkmeshChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (AreaView != null && ShowWalkmeshCheck != null)
                AreaView.ShowWalkmesh = ShowWalkmeshCheck.IsChecked == true;
        }

        private void OnPlaceableModelsChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (AreaView != null && PlaceableModelsCheck != null)
                AreaView.ShowPlaceableModels = PlaceableModelsCheck.IsChecked == true;
        }
    }
}
