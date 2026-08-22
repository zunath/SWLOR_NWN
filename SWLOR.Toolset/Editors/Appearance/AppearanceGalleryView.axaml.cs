using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Editors.Appearance
{
    /// <summary>The searchable appearance grid, shared by every editor that picks one.</summary>
    public partial class AppearanceGalleryView : UserControl
    {
        /// <summary>How close to the end of the grid publishes the next page.</summary>
        private const double LoadAheadPixels = 500;

        public AppearanceGalleryView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Requests a preview when the shared virtualizing panel realizes its cell. This is the same
        /// lifecycle used by the palette, so opening an appearance table does not queue thousands of
        /// model renders before the builder has scrolled to them.
        /// </summary>
        private void OnTileLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is not Control { DataContext: AppearanceTileViewModel tile } ||
                DataContext is not AppearanceGallerySectionViewModel section)
            {
                return;
            }

            section.EnsurePreview(tile);
        }

        /// <summary>
        /// Keeps the grid flowing as the builder scrolls, without realizing a tile and a render for
        /// every row in the table when the tab first opens.
        /// </summary>
        private void OnGalleryScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is not Control control ||
                control.DataContext is not AppearanceGallerySectionViewModel section ||
                !section.CanLoadMore ||
                e.OffsetDelta.Y <= 0)
            {
                return;
            }

            var scrollViewer = control as ScrollViewer ?? control.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var remaining = scrollViewer.Extent.Height -
                            scrollViewer.Offset.Y -
                            scrollViewer.Viewport.Height;
            if (remaining <= LoadAheadPixels)
                section.LoadMoreCommand.Execute(null);
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
