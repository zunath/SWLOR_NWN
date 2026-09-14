using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Editors;

public partial class NuiConversationEditorView : UserControl
{
    private const double DragThreshold = 5d;
    private NuiConversationTreeRow? _dragSource;
    private NuiConversationTreeRow? _dropTarget;
    private Control? _dragHandle;
    private ListBoxItem? _dragSourceContainer;
    private Point _dragStart;
    private bool _isDragging;

    public NuiConversationEditorView()
    {
        InitializeComponent();
    }

    private void OnTreeDragHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: NuiConversationTreeRow row } handle ||
            !e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
        {
            return;
        }

        CancelTreeDrag();
        _dragSource = row;
        _dragHandle = handle;
        _dragStart = e.GetPosition(ConversationTree);
        if (DataContext is NuiConversationEditorViewModel viewModel)
            viewModel.SelectedTreeRow = row;
        e.Pointer.Capture(handle);
        e.Handled = true;
    }

    private void OnTreeDragHandleMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSource == null || _dragHandle == null)
            return;
        if (!e.GetCurrentPoint(_dragHandle).Properties.IsLeftButtonPressed)
        {
            CancelTreeDrag(e.Pointer);
            return;
        }

        var point = e.GetPosition(ConversationTree);
        if (!_isDragging)
        {
            var movedFarEnough = Math.Abs(point.X - _dragStart.X) >= DragThreshold ||
                                 Math.Abs(point.Y - _dragStart.Y) >= DragThreshold;
            if (!movedFarEnough)
                return;

            _isDragging = true;
            _dragSourceContainer = FindTreeContainer(_dragSource);
            if (_dragSourceContainer != null)
                _dragSourceContainer.Opacity = 0.38;
            TreeDragPreviewKind.Text = _dragSource.KindLabel;
            TreeDragPreviewKind.Foreground = _dragSource.AccentBrush;
            TreeDragPreviewText.Text = _dragSource.Text;
            TreeDragPreview.BorderBrush = _dragSource.AccentBrush;
        }

        ScrollTreeNearEdge(point);
        UpdateTreeDropPreview(e);
        e.Handled = true;
    }

    private void OnTreeDragHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        var source = _dragSource;
        var target = _dropTarget;
        var commit = _isDragging && source != null && target != null;

        CancelTreeDrag(e.Pointer);
        if (commit && DataContext is NuiConversationEditorViewModel viewModel)
            viewModel.DropTreeRow(source, target);
        e.Handled = true;
    }

    private void OnTreeDragHandleCaptureLost(object? sender, PointerCaptureLostEventArgs e) =>
        CancelTreeDrag();

    private void UpdateTreeDropPreview(PointerEventArgs e)
    {
        if (_dragSource == null || DataContext is not NuiConversationEditorViewModel viewModel)
            return;

        var hoveredContainer = FindTreeContainerAt(e);
        if (hoveredContainer?.DataContext is not NuiConversationTreeRow hovered ||
            !viewModel.CanDropTreeRow(_dragSource, hovered))
        {
            HideTreeDropPreview();
            return;
        }

        _dropTarget = hovered;
        var insertAfter = viewModel.TreeDropInsertsAfter(_dragSource, hovered);

        var previewAnchor = insertAfter ? FindLastVisibleDescendant(hovered, viewModel) : hovered;
        var anchorContainer = FindTreeContainer(previewAnchor);
        var anchorOrigin = anchorContainer?.TranslatePoint(default, TreeDragSurface);
        if (anchorContainer == null || anchorOrigin == null)
        {
            HideTreeDropPreview();
            return;
        }

        var left = Math.Max(0d, anchorOrigin.Value.X + hovered.IndentWidth + 1d);
        var top = anchorOrigin.Value.Y + (insertAfter ? anchorContainer.Bounds.Height : 0d);
        var availableWidth = Math.Max(360d, TreeDragSurface.Bounds.Width - left - 20d);
        TreeDragPreview.Width = Math.Min(720d, availableWidth);
        Canvas.SetLeft(TreeDragPreview, left);
        Canvas.SetTop(TreeDragPreview, Math.Clamp(top, 0d, Math.Max(0d, TreeDragSurface.Bounds.Height - 24d)));
        TreeDragPreview.IsVisible = true;
    }

    private ListBoxItem? FindTreeContainerAt(PointerEventArgs e) =>
        ConversationTree.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .FirstOrDefault(item =>
            {
                var point = e.GetPosition(item);
                return point.X >= 0d && point.X <= item.Bounds.Width &&
                       point.Y >= 0d && point.Y <= item.Bounds.Height;
            });

    private ListBoxItem? FindTreeContainer(NuiConversationTreeRow row) =>
        ConversationTree.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .FirstOrDefault(item => ReferenceEquals(item.DataContext, row));

    private static NuiConversationTreeRow FindLastVisibleDescendant(
        NuiConversationTreeRow row,
        NuiConversationEditorViewModel viewModel)
    {
        var index = viewModel.TreeRows.IndexOf(row);
        var last = row;
        for (var next = index + 1; next >= 0 && next < viewModel.TreeRows.Count; next++)
        {
            if (viewModel.TreeRows[next].Depth <= row.Depth)
                break;
            last = viewModel.TreeRows[next];
        }
        return last;
    }

    private void ScrollTreeNearEdge(Point point)
    {
        var scroll = ConversationTree.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroll == null)
            return;

        const double edge = 24d;
        const double step = 18d;
        var vertical = scroll.Offset.Y;
        if (point.Y < edge)
            vertical -= step;
        else if (point.Y > ConversationTree.Bounds.Height - edge)
            vertical += step;
        else
            return;

        var maximum = Math.Max(0d, scroll.Extent.Height - scroll.Viewport.Height);
        scroll.Offset = new Vector(scroll.Offset.X, Math.Clamp(vertical, 0d, maximum));
    }

    private void HideTreeDropPreview()
    {
        _dropTarget = null;
        TreeDragPreview.IsVisible = false;
    }

    private void CancelTreeDrag(IPointer? pointer = null)
    {
        if (_dragSourceContainer != null)
            _dragSourceContainer.Opacity = 1d;
        TreeDragPreview.IsVisible = false;
        _dragSource = null;
        _dropTarget = null;
        _dragHandle = null;
        _dragSourceContainer = null;
        _isDragging = false;
        pointer?.Capture(null);
    }
}
