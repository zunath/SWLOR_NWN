using System.ComponentModel;
using Avalonia.Controls;

namespace SWLOR.Toolset.Editors
{
    public partial class AreaEditorView : UserControl
    {
        private AreaEditorViewModel? _viewModel;

        public AreaEditorView()
        {
            InitializeComponent();
            AreaView.RenderStatusChanged += OnGlRenderStatusChanged;
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
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AreaEditorViewModel.AreaScene) && _viewModel != null)
                AreaView.Scene = _viewModel.AreaScene;
        }

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
    }
}
