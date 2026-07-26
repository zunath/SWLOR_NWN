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
    /// <para>
    /// Hosts the same <see cref="GlAreaControl"/> the area editor uses rather than a second
    /// renderer: orbit, pan, zoom, lighting, textures and the model cache all come with it, and
    /// there is one GL path to keep working instead of two that drift apart.
    /// </para>
    /// <para>
    /// The child controls are resolved with <see cref="Control.FindControl{T}"/> rather than used as
    /// <c>x:Name</c> fields, which is the convention every view in this project that declares its
    /// own <c>InitializeComponent</c> follows (see <c>ScriptEditorView</c>). Declaring one suppresses
    /// the generated name wiring, so the fields compile but are null at runtime - which is exactly
    /// how this view first shipped, throwing a NullReferenceException the moment the Appearance tab
    /// was opened.
    /// </para>
    /// </remarks>
    public partial class ModelPreviewControl : UserControl
    {
        private readonly GlAreaControl? _modelView;
        private readonly Control? _viewportInput;
        private readonly Control? _emptyNotice;

        private AppearanceSectionViewModel? _viewModel;

        public ModelPreviewControl()
        {
            InitializeComponent();

            _modelView = this.FindControl<GlAreaControl>("ModelView");
            _viewportInput = this.FindControl<Control>("ViewportInput");
            _emptyNotice = this.FindControl<Control>("EmptyNotice");

            DataContextChanged += (_, _) => AttachViewModel();
            AttachViewModel();
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
            if (_modelView == null)
                return;

            var scene = _viewModel?.PreviewScene;

            _modelView.ResourceIndex = _viewModel?.ResourceIndex;
            _modelView.Scene = scene;

            var hasScene = scene != null;
            _modelView.IsVisible = hasScene;

            if (_viewportInput != null)
                _viewportInput.IsVisible = hasScene;

            if (_emptyNotice != null)
                _emptyNotice.IsVisible = !hasScene;
        }

        private void OnViewportPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e) =>
            _modelView?.HandlePointerPressed(e);

        private void OnViewportPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e) =>
            _modelView?.HandlePointerMoved(e);

        private void OnViewportPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e) =>
            _modelView?.HandlePointerReleased(e);

        private void OnViewportPointerWheel(object? sender, Avalonia.Input.PointerWheelEventArgs e) =>
            _modelView?.HandlePointerWheel(e);
    }
}
