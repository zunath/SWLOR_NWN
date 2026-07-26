using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>
    /// The shared behavior-editor row. Editors that need an extra control for one field kind place
    /// this alongside it rather than restating the label, status, and note markup.
    /// </summary>
    public partial class BehaviorRowView : UserControl
    {
        /// <summary>How close to the end of the gallery publishes the next page.</summary>
        private const double LoadAheadPixels = 500;

        public BehaviorRowView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Keeps a picture gallery flowing as the builder scrolls, without realizing every choice's
        /// preview when the picker first opens.
        /// </summary>
        private void OnChoiceGalleryScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is not Control control ||
                control.DataContext is not BehaviorRowViewModel row ||
                !row.CanLoadMoreGallery)
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
                row.LoadMoreGalleryCommand.Execute(null);
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
