using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SWLOR.Toolset.Editors.Placeables;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// An interactive 3D view of a single model, driven by an
    /// <see cref="AppearanceSectionViewModel"/>'s one-model scene.
    /// </summary>
    /// <remarks>
    /// Hosts the same <see cref="GlAreaControl"/> the area editor uses rather than a second
    /// renderer: orbit, pan, zoom, lighting, textures and the model cache all come with it, and
    /// there is one GL path to keep working instead of two that drift apart.
    /// </remarks>
    public partial class ModelPreviewControl : UserControl
    {
        private AppearanceSectionViewModel? _viewModel;

        public ModelPreviewControl()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => AttachViewModel();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void AttachViewModel()
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = DataContext as AppearanceSectionViewModel;

            if (_viewModel != null)
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Apply();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is null or nameof(AppearanceSectionViewModel.PreviewScene))
                Apply();
        }

        private void Apply()
        {
            if (_viewModel == null)
                return;

            ModelView.ResourceIndex = _viewModel.ResourceIndex;
            ModelView.Scene = _viewModel.PreviewScene;

            var hasScene = _viewModel.PreviewScene != null;
            ModelView.IsVisible = hasScene;
            ViewportInput.IsVisible = hasScene;
            EmptyNotice.IsVisible = !hasScene;
        }

        private void OnViewportPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e) =>
            ModelView.HandlePointerPressed(e);

        private void OnViewportPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e) =>
            ModelView.HandlePointerMoved(e);

        private void OnViewportPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e) =>
            ModelView.HandlePointerReleased(e);

        private void OnViewportPointerWheel(object? sender, Avalonia.Input.PointerWheelEventArgs e) =>
            ModelView.HandlePointerWheel(e);
    }
}
