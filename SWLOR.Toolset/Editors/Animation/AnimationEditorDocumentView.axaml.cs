using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.ComponentModel;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Editors.Animation;

public partial class AnimationEditorDocumentView : UserControl
{
    private AnimationEditorDocumentViewModel? _viewModel;
    private bool _initialized;
    private bool _closed;
    public AnimationEditorDocumentView()
    {
        InitializeComponent();
        _initialized = true;
        DataContextChanged += (_, _) => Attach();
    }
    private void Attach()
    {
        if (_closed) return;
        ReleaseViewModel();
        _viewModel = DataContext as AnimationEditorDocumentViewModel;
        if (_viewModel == null) return;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _viewModel.Closed += OnDocumentClosed;
        _viewModel.PickOpenPath = async (title, patterns) =>
        {
            var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (provider == null) return null;
            var files = await provider.OpenFilePickerAsync(new() { Title = title, AllowMultiple = false,
                FileTypeFilter = [new(title) { Patterns = patterns }] });
            return files.FirstOrDefault()?.TryGetLocalPath();
        };
        _viewModel.PickSavePath = async (title, name) =>
        {
            var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (provider == null) return null;
            var file = await provider.SaveFilePickerAsync(new() { Title = title, SuggestedFileName = name,
                DefaultExtension = System.IO.Path.GetExtension(name).TrimStart('.'), ShowOverwritePrompt = true });
            return file?.TryGetLocalPath();
        };
        UpdatePreview();
    }
    private void ReleaseViewModel()
    {
        if (_viewModel == null) return;
        _viewModel.PropertyChanged -= OnViewModelChanged;
        _viewModel.Closed -= OnDocumentClosed;
        _viewModel.PickOpenPath = null;
        _viewModel.PickSavePath = null;
        _viewModel.Stop();
        _viewModel = null;
    }
    private void OnDocumentClosed(AnimationEditorDocumentViewModel document)
    {
        _closed = true;
        ReleaseViewModel();
        this.FindControl<ModelPreviewControl>("ModelPreview")?.Dispose();
        this.FindControl<ModelPreviewControl>("BeginnerPreview")?.Dispose();
        DataContext = null;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); Attach(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _viewModel?.Stop(); _viewModel?.SetPreviewVisible(false);
        if (_viewModel != null) _viewModel.PropertyChanged -= OnViewModelChanged;
        this.FindControl<ModelPreviewControl>("ModelPreview")?.SetHostVisible(false);
        this.FindControl<ModelPreviewControl>("BeginnerPreview")?.SetHostVisible(false);
        base.OnDetachedFromVisualTree(e);
    }
    private void OnFrameRig(object? sender, RoutedEventArgs e) => this.FindControl<AnimationRigControl>("Rig")?.FrameRig();
    private void OnUndo(object? sender, RoutedEventArgs e) => _viewModel?.Undo();
    private void OnRedo(object? sender, RoutedEventArgs e) => _viewModel?.Redo();
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AnimationEditorDocumentViewModel.IsAdvanced) or nameof(AnimationEditorDocumentViewModel.HasModelPreview)) UpdatePreview();
    }
    private void OnPreviewTabChanged(object? sender, SelectionChangedEventArgs e) { if (ReferenceEquals(e.Source, sender)) UpdatePreview(); }
    private void UpdatePreview()
    {
        if (!_initialized) return;
        var guided = _viewModel?.IsAdvanced == false && _viewModel.HasModelPreview;
        var advanced = _viewModel?.IsAdvanced == true && this.FindControl<TabControl>("PreviewTabs")?.SelectedIndex == 1;
        _viewModel?.SetPreviewVisible(guided || advanced);
        this.FindControl<ModelPreviewControl>("ModelPreview")?.SetHostVisible(advanced);
        this.FindControl<ModelPreviewControl>("BeginnerPreview")?.SetHostVisible(guided);
    }
}
