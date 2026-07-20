using Avalonia.Controls;

namespace SWLOR.Toolset.Shell
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(ShellViewModel viewModel) : this()
        {
            DataContext = viewModel;

            Opened += async (_, _) =>
            {
                // Startup work (module open + background catalog build) runs after the window is
                // already showing, so the UI never blocks waiting on it.
                await viewModel.InitializeAsync();
            };
        }
    }
}
