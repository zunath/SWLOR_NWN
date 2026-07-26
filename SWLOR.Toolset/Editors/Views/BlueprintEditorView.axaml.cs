using Avalonia.Controls;
using Avalonia.VisualTree;
using SWLOR.Toolset.Editors.Placeables;

namespace SWLOR.Toolset.Editors
{
    public partial class BlueprintEditorView : UserControl
    {
        /// <summary>
        /// How close to the end of the model grid counts as "about to run out", in pixels. A little
        /// several rows of tiles, so with the small page size the next pages are already published by
        /// the time you reach them and the grid never visibly stops.
        /// </summary>
        private const double LoadAheadPixels = 600;

        public BlueprintEditorView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Publishes the next page of appearance models as the end of the grid comes into view.
        /// </summary>
        /// <remarks>
        /// The grid is paged rather than virtualized because it inherits the palette's approach: a
        /// WrapPanel inside a ListBox holds every item it is handed, so what has to be avoided is
        /// giving it all 24,304 rows at once - not scrolling itself. Loading on scroll keeps that
        /// protection while making the paging invisible.
        /// <para>
        /// The event is attached in the Appearance tab's DataTemplate, so the sender is that grid and
        /// its DataContext is the section view model.
        /// </para>
        /// </remarks>
        private void OnAppearanceScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is not Control control ||
                control.DataContext is not AppearanceSectionViewModel appearance ||
                !appearance.CanLoadMore)
                return;

            var scrollViewer = sender as ScrollViewer ?? control.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var remaining = scrollViewer.Extent.Height - scrollViewer.Offset.Y - scrollViewer.Viewport.Height;
            if (remaining <= LoadAheadPixels)
                appearance.LoadMoreCommand.Execute(null);
        }
    }
}
