using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class AreaContentsView : UserControl
    {
        private AreaContentsNodeViewModel? _contextRow;

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

        /// <summary>
        /// The menu belongs to the realised row rather than the ListBox, so empty-space clicks have
        /// no menu to open and can never act on an earlier selection.
        /// </summary>
        private void OnRowContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (sender is not Control
                { DataContext: AreaContentsNodeViewModel { IsInstance: true } node } ||
                DataContext is not AreaContentsViewModel viewModel)
            {
                _contextRow = null;
                e.Handled = true;
                return;
            }

            _contextRow = node;
            viewModel.SelectedRow = node;
        }

        private void OnOpenPropertiesClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is AreaContentsViewModel viewModel && _contextRow != null)
                viewModel.OpenPropertiesCommand.Execute(_contextRow);

            _contextRow = null;
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
