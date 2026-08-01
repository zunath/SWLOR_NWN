using Avalonia.Controls;
using Avalonia.Interactivity;
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

        private void OnAbilityRowLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CreatureEditorViewModel editor || sender is not Control control)
                return;

            switch (control.DataContext)
            {
                case CreatureAbilityChoiceViewModel choice:
                    _ = editor.Abilities.EnsureIconAsync(choice);
                    break;
                case CreatureAbilityEntryViewModel assigned:
                    _ = editor.Abilities.EnsureIconAsync(assigned);
                    break;
            }
        }
    }
}
