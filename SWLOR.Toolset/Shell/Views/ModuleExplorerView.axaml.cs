using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class ModuleExplorerView : UserControl
    {
        private const double DragThreshold = 5d;
        private ExplorerNodeViewModel? _dragSource;
        private ExplorerNodeViewModel? _dropTarget;
        private Control? _dragSurface;
        private IPointer? _capturedPointer;
        private ListBoxItem? _dragSourceContainer;
        private ListBoxItem? _dropTargetContainer;
        private Avalonia.Point _dragStart;
        private bool _isDragging;

        public ModuleExplorerView()
        {
            InitializeComponent();
        }

        private void OnItemsDoubleTapped(object? sender, TappedEventArgs e)
        {
            // Context-menu items remain logical children of the row even though Avalonia renders the
            // popup in a separate visual tree. A quick right-click followed by a menu selection can
            // therefore reach this ListBox as a DoubleTapped gesture on Windows. Only a gesture whose
            // visual source is actually inside one of this tree's row containers may open a resource.
            if (e.Source is not Visual source ||
                (source as ListBoxItem ?? source.FindAncestorOfType<ListBoxItem>()) is not
                    { DataContext: ExplorerNodeViewModel row })
            {
                return;
            }

            if (DataContext is ModuleExplorerViewModel viewModel)
            {
                viewModel.SelectedRow = row;
                viewModel.OpenSelectedItem();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Selects the row that was right-clicked. Avalonia does not select on right-click, and every
        /// command on the row's menu acts on the selection.
        /// </summary>
        private void OnRowContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (DataContext is ModuleExplorerViewModel viewModel &&
                sender is Control { DataContext: ExplorerNodeViewModel row })
            {
                viewModel.SelectedRow = row;
            }
        }

        private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Control { DataContext: ExplorerNodeViewModel { IsResource: true } row } surface ||
                !e.GetCurrentPoint(surface).Properties.IsLeftButtonPressed)
            {
                return;
            }

            CancelRowDrag();
            _dragSource = row;
            _dragSurface = surface;
            _dragStart = e.GetPosition(ModuleTree);
            if (DataContext is ModuleExplorerViewModel viewModel)
                viewModel.SelectedRow = row;
            _capturedPointer = e.Pointer;
            e.Pointer.Capture(surface);
        }

        private void OnModuleTreeKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _dragSource != null)
            {
                CancelRowDrag();
                e.Handled = true;
                return;
            }

            if (DataContext is not ModuleExplorerViewModel viewModel)
                return;

            if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control &&
                viewModel.UndoResourceMoveCommand.CanExecute(null))
            {
                viewModel.UndoResourceMoveCommand.Execute(null);
                e.Handled = true;
            }
            else if (((e.Key == Key.Y && e.KeyModifiers == KeyModifiers.Control) ||
                      (e.Key == Key.Z && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))) &&
                     viewModel.RedoResourceMoveCommand.CanExecute(null))
            {
                viewModel.RedoResourceMoveCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void OnRowPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragSource == null || _dragSurface == null)
                return;

            if (!e.GetCurrentPoint(_dragSurface).Properties.IsLeftButtonPressed)
            {
                CancelRowDrag(e.Pointer);
                return;
            }

            var point = e.GetPosition(ModuleTree);
            if (!_isDragging)
            {
                var movedFarEnough = Math.Abs(point.X - _dragStart.X) >= DragThreshold ||
                                     Math.Abs(point.Y - _dragStart.Y) >= DragThreshold;
                if (!movedFarEnough)
                    return;

                _isDragging = true;
                _dragSourceContainer = FindContainer(_dragSource);
                _dragSourceContainer?.Classes.Add("drag-source");
            }

            ScrollNearEdge(point);
            UpdateDropTarget(e);
            e.Handled = true;
        }

        private void OnRowPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var source = _dragSource;
            var target = _dropTarget;
            var commit = _isDragging && source != null && target != null;

            CancelRowDrag(e.Pointer);
            if (commit && DataContext is ModuleExplorerViewModel viewModel)
                viewModel.DropResource(source, target);
            if (commit)
                e.Handled = true;
        }

        private void OnRowPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) =>
            CancelRowDrag();

        private void UpdateDropTarget(PointerEventArgs e)
        {
            ClearDropTarget();
            if (_dragSource == null || DataContext is not ModuleExplorerViewModel viewModel)
                return;

            var container = FindContainerAt(e);
            if (container?.DataContext is not ExplorerNodeViewModel target ||
                !viewModel.CanDropResource(_dragSource, target))
            {
                return;
            }

            _dropTarget = target;
            _dropTargetContainer = container;
            container.Classes.Add("drag-target");
        }

        private ListBoxItem? FindContainerAt(PointerEventArgs e) =>
            ModuleTree.GetVisualDescendants()
                .OfType<ListBoxItem>()
                .FirstOrDefault(item =>
                {
                    var point = e.GetPosition(item);
                    return point.X >= 0d && point.X <= item.Bounds.Width &&
                           point.Y >= 0d && point.Y <= item.Bounds.Height;
                });

        private ListBoxItem? FindContainer(ExplorerNodeViewModel row) =>
            ModuleTree.GetVisualDescendants()
                .OfType<ListBoxItem>()
                .FirstOrDefault(item => ReferenceEquals(item.DataContext, row));

        private void ScrollNearEdge(Avalonia.Point point)
        {
            var scroll = ModuleTree.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (scroll == null)
                return;

            const double edge = 24d;
            const double step = 18d;
            var vertical = scroll.Offset.Y;
            if (point.Y < edge)
                vertical -= step;
            else if (point.Y > ModuleTree.Bounds.Height - edge)
                vertical += step;
            else
                return;

            var maximum = Math.Max(0d, scroll.Extent.Height - scroll.Viewport.Height);
            scroll.Offset = new Avalonia.Vector(scroll.Offset.X, Math.Clamp(vertical, 0d, maximum));
        }

        private void ClearDropTarget()
        {
            _dropTargetContainer?.Classes.Remove("drag-target");
            _dropTargetContainer = null;
            _dropTarget = null;
        }

        private void CancelRowDrag(IPointer? pointer = null)
        {
            var pointerToRelease = pointer ?? _capturedPointer;
            _dragSourceContainer?.Classes.Remove("drag-source");
            ClearDropTarget();
            _dragSource = null;
            _dragSurface = null;
            _capturedPointer = null;
            _dragSourceContainer = null;
            _isDragging = false;
            pointerToRelease?.Capture(null);
        }
    }
}
