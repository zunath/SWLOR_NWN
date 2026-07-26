using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class OutputView : UserControl
    {
        private INotifyCollectionChanged? _lines;

        public OutputView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => HookLines();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            HookLines();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (_lines != null)
                _lines.CollectionChanged -= OnLinesChanged;
            _lines = null;
            base.OnDetachedFromVisualTree(e);
        }

        private void HookLines()
        {
            if (_lines != null)
                _lines.CollectionChanged -= OnLinesChanged;

            _lines = (DataContext as OutputViewModel)?.Lines;
            if (_lines != null)
                _lines.CollectionChanged += OnLinesChanged;
        }

        /// <summary>
        /// Auto-scrolls the Output list to the newest line as entries arrive, so the latest log
        /// output is always visible. Posted to the UI thread so the item
        /// container exists before scrolling, and so appends from background threads are marshaled
        /// safely.
        /// </summary>
        private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                var count = LinesList.ItemCount;
                if (count > 0)
                    LinesList.ScrollIntoView(count - 1);
            });
        }
    }
}
