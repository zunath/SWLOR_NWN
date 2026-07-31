using Avalonia.Controls;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Editors.Creatures
{
    public partial class CreatureEditorView : UserControl
    {
        private const double AbilityLoadAheadPixels = 400;

        public CreatureEditorView()
        {
            InitializeComponent();
        }

        private void OnAbilityScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is not Control control ||
                control.DataContext is not CreatureAbilitiesViewModel abilities ||
                !abilities.CanLoadMore)
            {
                return;
            }

            var scrollViewer = control as ScrollViewer ?? control.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var remaining = scrollViewer.Extent.Height - scrollViewer.Offset.Y - scrollViewer.Viewport.Height;
            if (remaining <= AbilityLoadAheadPixels)
                abilities.LoadMoreCommand.Execute(null);
        }
    }
}
