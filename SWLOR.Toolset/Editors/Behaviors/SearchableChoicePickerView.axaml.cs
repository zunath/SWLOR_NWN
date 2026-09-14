using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Editors.Behaviors
{
    public sealed partial class SearchableChoicePickerView : UserControl
    {
        private const double LoadAheadPixels = 400;

        public static readonly StyledProperty<bool> CompactProperty =
            AvaloniaProperty.Register<SearchableChoicePickerView, bool>(nameof(Compact));

        public bool Compact
        {
            get => GetValue(CompactProperty);
            set => SetValue(CompactProperty, value);
        }

        public SearchableChoicePickerView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Uses the same scroll-driven progressive loading as the appearance, equipment, ability,
        /// and merchant pickers. The shared choice picker owns this behavior so categories and every
        /// other long choice list stay consistent without editor-specific paging controls.
        /// </summary>
        private void OnSearchResultsScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is not Control control ||
                control.DataContext is not BehaviorRowViewModel row ||
                !row.CanLoadMoreSearchResults)
            {
                return;
            }

            var scrollViewer = control as ScrollViewer ?? control.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var remaining = scrollViewer.Extent.Height - scrollViewer.Offset.Y - scrollViewer.Viewport.Height;
            if (remaining <= LoadAheadPixels)
                row.LoadMoreSearchResultsCommand.Execute(null);
        }
    }
}
