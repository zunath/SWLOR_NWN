using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Shell.Controls
{
    /// <summary>
    /// Lays items out left to right, wrapping onto a new row at the panel's edge, and only builds the
    /// ones near the viewport.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written because Avalonia ships a virtualizing panel for stacks but not for wraps, and the palette
    /// needs both: a grid of tiles, and a category that can hold two thousand of them. With a plain
    /// <see cref="WrapPanel"/> opening the module's largest item category built 2,013 tile controls to
    /// show the forty that fit on screen, which was measured at ~3.4s - a third of it the list's own
    /// containers, the rest the tiles inside them. Neither part has a hotspot to remove; the fix is to
    /// stop building what nobody is looking at.
    /// </para>
    /// <para>
    /// Every item is laid out at the same size, which is what makes the arithmetic here trivial: which
    /// rows a viewport covers, and which items are in those rows, are both a division away, with no need
    /// to measure anything before it is shown or to keep an estimate of what was skipped. That suits a
    /// palette exactly - one grid of same-sized tiles - and is why this is not a general-purpose wrap
    /// panel. The size comes from measuring the items themselves rather than being configured: see
    /// <see cref="UpdateItemSize"/>.
    /// </para>
    /// </remarks>
    public class VirtualizingWrapPanel : VirtualizingPanel
    {
        /// <summary>
        /// How far beyond the viewport, in viewport heights, items are still built. One screenful either
        /// way, so an ordinary scroll lands on tiles that already exist.
        /// </summary>
        private const double BufferFactor = 1.0;

        /// <summary>
        /// How much is realized before the viewport is known, in pixels. The first measure happens before
        /// the panel has ever been arranged, so there is no viewport to consult yet; this is a first
        /// screenful for a large display, replaced the moment a real viewport arrives.
        /// </summary>
        private const double AssumedViewportHeight = 1200;

        /// <summary>Marks a container as poolable, and by what key. Null means it cannot be reused.</summary>
        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingWrapPanel, Control, object?>("RecycleKey");

        /// <summary>Sentinel recycle key for an item that is its own container and so is never pooled.</summary>
        private static readonly object ItemIsItsOwnContainer = new();

        private readonly Dictionary<int, Control> _realized = new();
        private readonly Dictionary<object, Stack<Control>> _recyclePool = new();

        private Rect _viewport;
        private bool _hasViewport;

        private Size _itemSize;
        private int _columns = 1;

        /// <summary>
        /// The container holding focus, kept alive outside <see cref="_realized"/> while it is scrolled
        /// out of view - recycling it would move focus to the panel and lose the user's place.
        /// </summary>
        private Control? _focusedElement;
        private int _focusedIndex = -1;

        public VirtualizingWrapPanel()
        {
            EffectiveViewportChanged += OnEffectiveViewportChanged;
        }

        private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            var viewport = e.EffectiveViewport;
            var changed = !_hasViewport || !AreClose(viewport, _viewport);

            _viewport = viewport;
            _hasViewport = true;

            // Only when the visible band actually moved. This fires on every layout pass of every
            // ancestor, and invalidating measure from inside one of those is how a layout loop starts.
            if (changed)
                InvalidateMeasure();
        }

        /// <summary>
        /// Whether two viewports are the same to within a pixel. Scrolling produces fractional offsets,
        /// and treating a sub-pixel difference as movement would re-measure on every frame.
        /// </summary>
        private static bool AreClose(Rect a, Rect b) =>
            Math.Abs(a.X - b.X) < 1 && Math.Abs(a.Y - b.Y) < 1 &&
            Math.Abs(a.Width - b.Width) < 1 && Math.Abs(a.Height - b.Height) < 1;

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;
            if (items.Count == 0)
            {
                RecycleAll();
                return default;
            }

            var itemSizeChanged = UpdateItemSize(items, availableSize);

            var width = double.IsInfinity(availableSize.Width) ? _itemSize.Width : availableSize.Width;
            _columns = Math.Max(1, (int)(width / _itemSize.Width));
            var rows = (items.Count + _columns - 1) / _columns;

            var (first, last) = VisibleRange(items.Count);

            foreach (var index in _realized.Keys.ToList())
            {
                if (index < first || index > last)
                    Recycle(index);
            }

            var tallest = _itemSize.Height;
            for (var index = first; index <= last; index++)
            {
                var element = Realize(items, index);
                element.Measure(new Size(_itemSize.Width, double.PositiveInfinity));
                tallest = Math.Max(tallest, element.DesiredSize.Height);
            }

            // Rows are as tall as the tallest item seen so far, and that only ever grows while the item
            // width is unchanged. A running maximum rather than a per-pass one because the alternative
            // is rows that change height as you scroll between a screen of one-line names and a screen
            // of two-line ones, which moves the scrollbar under the hand that is dragging it.
            if (tallest > _itemSize.Height)
            {
                _itemSize = _itemSize.WithHeight(tallest);
                itemSizeChanged = true;
            }

            if (itemSizeChanged)
                InvalidateArrange();

            return new Size(width, rows * _itemSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_itemSize.Height <= 0)
                return finalSize;

            foreach (var (index, element) in _realized)
                element.Arrange(BoundsFor(index));

            // Kept where it belongs even while scrolled away, so returning to it does not find it stacked
            // at the origin.
            if (_focusedElement != null && _focusedIndex >= 0 && !_realized.ContainsKey(_focusedIndex))
                _focusedElement.Arrange(BoundsFor(_focusedIndex));

            return finalSize;
        }

        private Rect BoundsFor(int index)
        {
            var row = index / _columns;
            var column = index % _columns;
            return new Rect(
                column * _itemSize.Width, row * _itemSize.Height, _itemSize.Width, _itemSize.Height);
        }

        /// <summary>
        /// The first and last item index worth building: the rows the viewport covers, plus a buffer
        /// either side. Both ends are clamped to the collection.
        /// </summary>
        private (int First, int Last) VisibleRange(int count)
        {
            var height = _hasViewport && _viewport.Height > 0 ? _viewport.Height : AssumedViewportHeight;
            var top = _hasViewport ? _viewport.Y : 0;
            var buffer = height * BufferFactor;

            var firstRow = Math.Max(0, (int)Math.Floor((top - buffer) / _itemSize.Height));
            var lastRow = (int)Math.Ceiling((top + height + buffer) / _itemSize.Height);

            var first = Math.Min(count - 1, firstRow * _columns);
            var last = Math.Min(count - 1, (lastRow + 1) * _columns - 1);
            return (first, Math.Max(first, last));
        }

        /// <summary>
        /// Establishes the size every item is laid out at, by measuring one. Returns whether it changed.
        /// </summary>
        /// <remarks>
        /// Measured rather than configured, because the tile's size is the template's business - it is
        /// bound to the palette's preview-size setting - and a panel that had to be told would be one
        /// more place to keep in step with it. A width that no longer matches means that setting moved,
        /// which is also the one thing that may make rows shorter again, so the running maximum height
        /// starts over with it.
        /// </remarks>
        private bool UpdateItemSize(IReadOnlyList<object?> items, Size availableSize)
        {
            var probe = _realized.Count > 0 ? _realized.Values.First() : Realize(items, 0);
            probe.Measure(new Size(
                double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width,
                double.PositiveInfinity));

            var measured = probe.DesiredSize;
            if (measured.Width <= 0 || measured.Height <= 0)
            {
                // Nothing usable to divide by. One row of one item is wrong but finite, and the next
                // pass - once the template has a size - fixes it.
                if (_itemSize.Width <= 0 || _itemSize.Height <= 0)
                    _itemSize = new Size(Math.Max(1, measured.Width), Math.Max(1, measured.Height));

                return false;
            }

            if (Math.Abs(measured.Width - _itemSize.Width) < 0.5)
                return false;

            _itemSize = measured;
            return true;
        }

        // ----- realization -----

        private Control Realize(IReadOnlyList<object?> items, int index)
        {
            if (_realized.TryGetValue(index, out var existing))
                return existing;

            // The focused container may be parked outside the realized set; take it back rather than
            // build a second container for the same item.
            if (_focusedIndex == index && _focusedElement != null)
            {
                var focused = _focusedElement;
                _focusedElement = null;
                _focusedIndex = -1;
                _realized[index] = focused;
                return focused;
            }

            var item = items[index];
            var generator = ItemContainerGenerator!;

            Control container;
            if (generator.NeedsContainer(item, index, out var recycleKey))
            {
                container = TakeFromPool(item, index, recycleKey) ?? Create(item, index, recycleKey);
            }
            else
            {
                container = (Control)item!;
                if (!container.IsSet(RecycleKeyProperty))
                {
                    generator.PrepareItemContainer(container, item, index);
                    AddInternalChild(container);
                    container.SetValue(RecycleKeyProperty, ItemIsItsOwnContainer);
                    generator.ItemContainerPrepared(container, item, index);
                }

                container.SetCurrentValue(Visual.IsVisibleProperty, true);
            }

            _realized[index] = container;
            return container;
        }

        private Control Create(object? item, int index, object? recycleKey)
        {
            var generator = ItemContainerGenerator!;
            var container = generator.CreateContainer(item, index, recycleKey);

            container.SetValue(RecycleKeyProperty, recycleKey);
            generator.PrepareItemContainer(container, item, index);
            AddInternalChild(container);
            generator.ItemContainerPrepared(container, item, index);
            return container;
        }

        private Control? TakeFromPool(object? item, int index, object? recycleKey)
        {
            if (recycleKey == null ||
                !_recyclePool.TryGetValue(recycleKey, out var pool) ||
                pool.Count == 0)
            {
                return null;
            }

            var generator = ItemContainerGenerator!;
            var container = pool.Pop();
            container.SetCurrentValue(Visual.IsVisibleProperty, true);
            generator.PrepareItemContainer(container, item, index);
            AddInternalChild(container);
            generator.ItemContainerPrepared(container, item, index);
            return container;
        }

        private void Recycle(int index)
        {
            if (!_realized.Remove(index, out var element))
                return;

            var recycleKey = element.GetValue(RecycleKeyProperty);

            if (recycleKey == null)
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                RemoveInternalChild(element);
                return;
            }

            if (ReferenceEquals(recycleKey, ItemIsItsOwnContainer))
            {
                element.SetCurrentValue(Visual.IsVisibleProperty, false);
                return;
            }

            // Held rather than pooled: recycling the container that has focus would drop focus to the
            // panel, which reads as the keyboard losing its place mid-scroll.
            if (element.IsKeyboardFocusWithin ||
                (ItemsControl != null && KeyboardNavigation.GetTabOnceActiveElement(ItemsControl) == element))
            {
                _focusedElement = element;
                _focusedIndex = index;
                return;
            }

            ItemContainerGenerator!.ClearItemContainer(element);
            if (!_recyclePool.TryGetValue(recycleKey, out var pool))
                _recyclePool[recycleKey] = pool = new Stack<Control>();

            pool.Push(element);
            element.SetCurrentValue(Visual.IsVisibleProperty, false);
            RemoveInternalChild(element);
        }

        private void RecycleAll()
        {
            foreach (var index in _realized.Keys.ToList())
                Recycle(index);
        }

        // ----- VirtualizingPanel -----

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            // Rebuilt wholesale rather than patched index by index. The palette republishes its whole
            // grid whenever the category, type or search text changes, so an incremental path would be
            // more code for a case that effectively never happens.
            RecycleAll();

            _focusedElement = null;
            _focusedIndex = -1;

            // The next category's tiles may not be the size of this one's - a tileset preview is not an
            // inventory icon - so the measured size starts over with them.
            _itemSize = default;

            InvalidateMeasure();
        }

        protected override Control? ContainerFromIndex(int index)
        {
            if (_realized.TryGetValue(index, out var element))
                return element;

            return _focusedIndex == index ? _focusedElement : null;
        }

        protected override int IndexFromContainer(Control container)
        {
            foreach (var (index, element) in _realized)
            {
                if (ReferenceEquals(element, container))
                    return index;
            }

            return ReferenceEquals(container, _focusedElement) ? _focusedIndex : -1;
        }

        protected override IEnumerable<Control>? GetRealizedContainers() => _realized.Values.ToList();

        protected override Control? ScrollIntoView(int index)
        {
            var items = Items;
            if (index < 0 || index >= items.Count || _itemSize.Height <= 0)
                return null;

            var element = Realize(items, index);
            element.Measure(new Size(_itemSize.Width, double.PositiveInfinity));
            element.Arrange(BoundsFor(index));
            element.BringIntoView();

            // Left realized: the scroll it just asked for will move the viewport onto it, and the next
            // measure decides whether it is still in range on the merits.
            return element;
        }

        /// <summary>
        /// Answers the arrow keys, Home/End and Page Up/Down, in the two dimensions the grid actually
        /// has - a step sideways is one item, a step up or down is a whole row.
        /// </summary>
        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var count = Items.Count;
            if (count == 0)
                return null;

            var fromControl = from as Control ?? (from as Visual)?.FindAncestorOfType<Control>();
            var current = fromControl == null ? -1 : IndexFromControl(fromControl);
            var rows = (count + _columns - 1) / _columns;
            var page = Math.Max(1, RowsPerViewport()) * _columns;

            var target = direction switch
            {
                NavigationDirection.First => 0,
                NavigationDirection.Last => count - 1,
                NavigationDirection.Next => current + 1,
                NavigationDirection.Previous => current - 1,
                NavigationDirection.Left => current - 1,
                NavigationDirection.Right => current + 1,
                NavigationDirection.Up => current - _columns,
                NavigationDirection.Down => current + _columns,
                NavigationDirection.PageUp => current - page,
                NavigationDirection.PageDown => current + page,
                _ => -1
            };

            if (target < 0 || target >= count)
            {
                if (!wrap || current < 0 || rows == 0)
                    return null;

                target = target < 0 ? count - 1 : 0;
            }

            return ScrollIntoView(target);
        }

        private int RowsPerViewport()
        {
            if (_itemSize.Height <= 0)
                return 1;

            var height = _hasViewport && _viewport.Height > 0 ? _viewport.Height : AssumedViewportHeight;
            return (int)Math.Max(1, Math.Floor(height / _itemSize.Height));
        }

        /// <summary>The index of a container, or of the container that holds it.</summary>
        private int IndexFromControl(Control control)
        {
            for (var candidate = control; candidate != null; candidate = candidate.Parent as Control)
            {
                var index = IndexFromContainer(candidate);
                if (index >= 0)
                    return index;
            }

            return -1;
        }
    }
}
