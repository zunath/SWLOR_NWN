using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// An interactive 3D view of a single model, driven by an
    /// <see cref="IModelPreviewSource"/>'s one-model scene.
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
    public partial class ModelPreviewControl : UserControl, IDisposable
    {
        private readonly GlAreaControl? _modelView;
        private readonly Control? _viewportInput;
        private readonly Control? _emptyNotice;

        /// <summary>
        /// How much of one pad step a pixel of drag is worth. Tuned so a drag across the preview is
        /// a bit more than a full turn - enough to see every side without wearing out a wrist.
        /// </summary>
        private const float OrbitPerPixel = 0.16f;

        private IModelPreviewSource? _viewModel;
        private bool _isAttached;
        private bool _hostVisible;
        private bool _disposed;

        /// <summary>How far a pixel of right-drag slides the camera, in metres.</summary>
        private const float PanPerPixel = 0.02f;

        /// <summary>Where the pointer was last seen during a drag, or null when not dragging.</summary>
        private Avalonia.Point? _dragFrom;

        /// <summary>True while the drag in flight is a right-button pan rather than a turn.</summary>
        private bool _dragPans;

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

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _isAttached = true;
            ApplyAnimation();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _isAttached = false;
            ApplyAnimation();
            base.OnDetachedFromVisualTree(e);
        }

        private void AttachViewModel()
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = DataContext as IModelPreviewSource;

            if (_viewModel != null)
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            ApplyScene();
            ApplyAnimation();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is null or nameof(IModelPreviewSource.PreviewScene))
                ApplyScene();

            if (e.PropertyName is null or
                nameof(IModelPreviewSource.PreviewAnimationName) or
                nameof(IModelPreviewSource.IsAnimationPlaying))
            {
                ApplyAnimation();
            }
        }

        private void ApplyScene()
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

        private void ApplyAnimation()
        {
            if (_modelView == null)
                return;

            _modelView.PreviewAnimationName = _viewModel?.PreviewAnimationName;
            _modelView.PreviewAnimationPlaying = _viewModel?.IsAnimationPlaying == true;
            _modelView.PreviewAnimationActive = !_disposed && _hostVisible && _isAttached;
        }

        /// <summary>
        /// The owning view model calls this when the Appearance tab is selected or hidden. The
        /// control instance survives re-parenting, so visual attachment alone cannot distinguish a
        /// hidden tab from a document switch in progress.
        /// </summary>
        public void SetHostVisible(bool visible)
        {
            _hostVisible = visible;
            ApplyAnimation();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
            DataContext = null;

            if (_modelView != null)
            {
                _modelView.PreviewAnimationActive = false;
                _modelView.Scene = null;
            }
        }

        /// <summary>
        /// Left-drag turns the model. The shared viewport reserves left for picking and orbits on
        /// the middle button, which is right for a map you select things in - but here there is
        /// nothing to select and only one thing to look at, so the plainest gesture should be the
        /// one that turns it. Handled here rather than changed in the area control, so the map keeps
        /// its own semantics.
        /// </summary>
        private void OnViewportPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (_modelView == null)
                return;

            var point = e.GetCurrentPoint(_viewportInput ?? (Control)_modelView);
            if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
            {
                _dragPans = point.Properties.IsRightButtonPressed;
                _dragFrom = point.Position;
                e.Pointer.Capture(_viewportInput);
                e.Handled = true;
                return;
            }

            _modelView.HandlePointerPressed(e);
        }

        private void OnViewportPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (_modelView == null)
                return;

            if (_dragFrom == null)
            {
                _modelView.HandlePointerMoved(e);
                return;
            }

            var position = e.GetPosition(_viewportInput ?? (Control)_modelView);
            var deltaX = (float)(position.X - _dragFrom.Value.X);
            var deltaY = (float)(position.Y - _dragFrom.Value.Y);
            _dragFrom = position;

            if (_dragPans)
                _modelView.NudgePan(-deltaX * PanPerPixel, deltaY * PanPerPixel);
            else
                // Negated so the model follows the mouse: dragging right turns its near face right.
                _modelView.NudgeOrbit(-deltaX * OrbitPerPixel, -deltaY * OrbitPerPixel);
            e.Handled = true;
        }

        private void OnViewportPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            if (_dragFrom != null)
            {
                _dragFrom = null;
                _dragPans = false;
                e.Pointer.Capture(null);
                e.Handled = true;
                return;
            }

            _modelView?.HandlePointerReleased(e);
        }

        private void OnViewportPointerWheel(object? sender, Avalonia.Input.PointerWheelEventArgs e) =>
            _modelView?.HandlePointerWheel(e);
    }
}
