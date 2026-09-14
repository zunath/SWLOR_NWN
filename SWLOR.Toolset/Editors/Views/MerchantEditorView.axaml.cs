using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Editors.Merchants
{
    public partial class MerchantEditorView : UserControl
    {
        private const double ItemCandidateLoadAheadPixels = 500;

        public MerchantEditorView()
        {
            InitializeComponent();
        }

        private void OnItemCandidateScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is not Control control ||
                DataContext is not MerchantEditorViewModel editor ||
                !editor.CanLoadMoreItemCandidates)
            {
                return;
            }

            var scrollViewer = control as ScrollViewer ?? control.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var remaining = scrollViewer.Extent.Height -
                            scrollViewer.Offset.Y -
                            scrollViewer.Viewport.Height;
            if (remaining <= ItemCandidateLoadAheadPixels)
                editor.LoadMoreItemCandidatesCommand.Execute(null);
        }

        private void OnItemCandidateLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is Control { DataContext: MerchantItemCandidateViewModel candidate } &&
                DataContext is MerchantEditorViewModel editor)
            {
                editor.EnsureItemCandidatePreview(candidate);
            }
        }
    }
}
