using Avalonia.Controls;

namespace SWLOR.Toolset.Shell
{
    public partial class MainWindow : Window
    {
        private bool _closeApproved;
        private bool _closePromptOpen;

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

            Closing += async (_, args) =>
            {
                if (_closeApproved)
                    return;

                // Avalonia cannot await a Closing handler, so cancel this attempt first and issue
                // a second Close() only after the save/discard/cancel decision completes.
                args.Cancel = true;
                if (_closePromptOpen)
                    return;

                _closePromptOpen = true;
                try
                {
                    if (await viewModel.TryCloseAsync().ConfigureAwait(true))
                    {
                        _closeApproved = true;
                        Close();
                    }
                }
                finally
                {
                    _closePromptOpen = false;
                }
            };
        }
    }
}
