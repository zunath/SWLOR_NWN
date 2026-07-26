using Avalonia.Controls;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Editors.Doors
{
    public partial class DoorEditorView : UserControl
    {
        public DoorEditorView()
        {
            InitializeComponent();
        }

        private const double LoadAheadPixels = 500;

        private void OnAppearanceScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is not Control control ||
                control.DataContext is not DoorAppearanceSectionViewModel appearance ||
                !appearance.CanLoadMore)
            {
                return;
            }

            var scrollViewer = sender as ScrollViewer ?? control.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var remaining = scrollViewer.Extent.Height - scrollViewer.Offset.Y - scrollViewer.Viewport.Height;
            if (remaining <= LoadAheadPixels)
                appearance.LoadMoreCommand.Execute(null);
        }
    }
}
