using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace SWLOR.Toolset.Editors.Animation;

/// <summary>Draws dense baked keyframes without allocating a control for every key.</summary>
public sealed class AnimationTimelineControl : Control
{
    private AnimationEditorDocumentViewModel? _viewModel;
    private bool _dragging;
    public AnimationTimelineControl()
    {
        Height = 36; MinWidth = 120;
        DataContextChanged += (_, _) => Attach();
    }
    private void Attach()
    {
        if (_viewModel != null) _viewModel.PropertyChanged -= Changed;
        _viewModel = DataContext as AnimationEditorDocumentViewModel;
        if (_viewModel != null) _viewModel.PropertyChanged += Changed;
        InvalidateVisual();
    }
    private void Changed(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); Attach(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _dragging = false; if (_viewModel != null) _viewModel.PropertyChanged -= Changed;
        base.OnDetachedFromVisualTree(e);
    }
    public override void Render(DrawingContext context)
    {
        context.FillRectangle(new SolidColorBrush(Color.Parse("#171e28")), new Rect(Bounds.Size));
        if (_viewModel == null) return;
        var width = Math.Max(1, Bounds.Width - 12); var lastPixel = -1;
        foreach (var key in _viewModel.Project.Keys)
        {
            var pixel = (int)(key.Time / _viewModel.Project.Duration * width);
            if (pixel == lastPixel) continue;
            lastPixel = pixel;
            context.DrawLine(new Pen(Brushes.Goldenrod, 2), new(6 + pixel, 12), new(6 + pixel, 28));
        }
        var cursor = 6 + _viewModel.Playhead / _viewModel.Project.Duration * width;
        context.DrawLine(new Pen(Brushes.White, 2), new(cursor, 2), new(cursor, 34));
    }
    private void Seek(Point position)
    {
        if (_viewModel == null || _viewModel.IsBusy) return;
        var width = Math.Max(1, Bounds.Width - 12);
        var time = Math.Clamp((position.X - 6) / width, 0, 1) * _viewModel.Project.Duration;
        var key = _viewModel.Project.Keys.MinBy(k => Math.Abs(k.Time - time));
        if (key != null && Math.Abs(key.Time - time) / _viewModel.Project.Duration * width <= 6) time = key.Time;
        _viewModel.Stop(); _viewModel.Playhead = time;
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragging = true; e.Pointer.Capture(this); Seek(e.GetPosition(this)); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e) { if (_dragging) Seek(e.GetPosition(this)); }
    protected override void OnPointerReleased(PointerReleasedEventArgs e) { _dragging = false; e.Pointer.Capture(null); }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { _dragging = false; base.OnPointerCaptureLost(e); }
}
