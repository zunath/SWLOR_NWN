using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class ProblemsView : UserControl
    {
        public ProblemsView() => InitializeComponent();

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void OnRowDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ProblemsViewModel { SelectedRow: { } row } vm)
                vm.NavigateCommand.Execute(row);
        }
    }
}
