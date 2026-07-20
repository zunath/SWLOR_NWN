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
    }
}
