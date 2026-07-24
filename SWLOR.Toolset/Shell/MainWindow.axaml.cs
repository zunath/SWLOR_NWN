using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;

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

            // File > Exit goes through Close() so it hits the same unsaved-changes prompt below
            // rather than dropping edits on the floor.
            viewModel.ExitRequested += Close;

            // The menu's InputGesture text is only a label - these are the actual shortcuts. They
            // live on the window (not on the MenuItems) so they work whether or not the menu has
            // ever been opened. Avalonia stops walking KeyBindings once the event is handled, so a
            // focused text box keeps its own Ctrl+Z for text and only unhandled presses reach the
            // active editor's document history.
            Bind(Key.S, KeyModifiers.Control, viewModel.SaveCommand);
            Bind(Key.S, KeyModifiers.Control | KeyModifiers.Shift, viewModel.SaveAllCommand);
            Bind(Key.Z, KeyModifiers.Control, viewModel.UndoCommand);
            Bind(Key.Y, KeyModifiers.Control, viewModel.RedoCommand);

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

        private void Bind(Key key, KeyModifiers modifiers, ICommand command)
        {
            KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(key, modifiers),
                Command = command
            });
        }
    }
}
