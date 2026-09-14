using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class ScriptReferenceView : UserControl
    {
        public ScriptReferenceView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => (DataContext as ScriptReferenceViewModel)?.EnsureBuilt();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void OnRowDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ScriptReferenceViewModel vm || vm.SelectedRow is not { } row)
                return;

            // Double-click on a category expands it; on a symbol it inserts, which is what Aurora did
            // and what the button below duplicates for discoverability.
            if (row.IsCategory)
                vm.ToggleCommand.Execute(row);
            else if (vm.InsertCommand.CanExecute(null))
                vm.InsertCommand.Execute(null);
        }
    }
}
