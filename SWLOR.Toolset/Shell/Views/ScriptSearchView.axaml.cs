using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class ScriptSearchView : UserControl
    {
        public ScriptSearchView() => InitializeComponent();

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void OnRowDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ScriptSearchViewModel { SelectedResult: { } result } vm)
                vm.NavigateCommand.Execute(result);
        }
    }
}
