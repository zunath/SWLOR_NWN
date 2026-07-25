using Avalonia.Controls;
using Avalonia.Input;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class ModuleExplorerView : UserControl
    {
        public ModuleExplorerView()
        {
            InitializeComponent();
        }

        private void OnItemsDoubleTapped(object? sender, TappedEventArgs e)
        {
            (DataContext as ModuleExplorerViewModel)?.OpenSelectedItem();
        }

        /// <summary>
        /// Selects the row that was right-clicked. Avalonia does not select on right-click, and every
        /// command on the row's menu acts on the selection.
        /// </summary>
        private void OnRowContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (DataContext is ModuleExplorerViewModel viewModel &&
                sender is Control { DataContext: ExplorerNodeViewModel row })
            {
                viewModel.SelectedRow = row;
            }
        }
    }
}
