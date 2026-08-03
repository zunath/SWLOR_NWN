using Avalonia.Controls;
using Avalonia.Input;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class AreaContentsView : UserControl
    {
        public AreaContentsView()
        {
            InitializeComponent();
        }

        /// <summary>Right-click selects the row under the pointer before its menu is evaluated.</summary>
        private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed ||
                sender is not Control { DataContext: AreaContentsNodeViewModel node } ||
                DataContext is not AreaContentsViewModel viewModel)
                return;

            viewModel.SelectedRow = node;
        }

        /// <summary>Branches have no properties menu; their right-click remains inert.</summary>
        private void OnRowContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (DataContext is not AreaContentsViewModel
                { SelectedRow.Kind: AreaContentsNodeKind.Instance })
                e.Handled = true;
        }

        /// <summary>Opening a row sends the camera to the object, or opens the branch.</summary>
        private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is AreaContentsViewModel viewModel)
                viewModel.OpenCommand.Execute(viewModel.SelectedRow);
        }

        /// <summary>
        /// Delete removes the selected objects; Enter opens the row, matching the double-click.
        /// </summary>
        /// <remarks>
        /// Marked handled so the key stops here. A ListBox does nothing with either, but leaving them
        /// to bubble puts Delete in front of whatever the shell adds later, on a keystroke that
        /// destroys content.
        /// </remarks>
        private void OnRowKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not AreaContentsViewModel viewModel)
                return;

            switch (e.Key)
            {
                case Key.Delete:
                    viewModel.DeleteSelectedCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    viewModel.OpenCommand.Execute(viewModel.SelectedRow);
                    e.Handled = true;
                    break;
            }
        }
    }
}
