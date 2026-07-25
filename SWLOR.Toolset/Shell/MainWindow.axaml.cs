using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Shell
{
    public partial class MainWindow : Window
    {
        private bool _closeApproved;
        private bool _closePromptOpen;
        private ToolsetSettings? _settings;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(ShellViewModel viewModel, ToolsetSettings? settings = null) : this()
        {
            DataContext = viewModel;
            _settings = settings;
            RestorePlacement();

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
                // Recorded on the first close attempt, before the unsaved-changes prompt can cancel
                // it: the prompt is a window of its own, and by the time a cancelled attempt comes
                // back around this window may have been moved by it.
                SavePlacement();

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

        /// <summary>
        /// Puts the window back where it was left. Size and position are applied separately, because a
        /// builder who has a remembered size but ran the previous session on a monitor that is now gone
        /// should still get their size rather than nothing.
        /// </summary>
        private void RestorePlacement()
        {
            if (_settings?.Window is not { } placement)
                return;

            if (placement.HasSize)
            {
                Width = placement.Width;
                Height = placement.Height;
            }

            if (placement.HasPosition)
            {
                // Manual placement only sticks if Avalonia is not also centring the window.
                WindowStartupLocation = WindowStartupLocation.Manual;
                Position = new Avalonia.PixelPoint((int)placement.Left, (int)placement.Top);
            }

            if (placement.IsMaximized)
                WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// Records the window's placement. While maximised, Width/Height report the screen, so the
        /// remembered size is left alone and only the maximised flag is updated - un-maximising after a
        /// restart then gives back the window that was actually being worked in.
        /// </summary>
        private void SavePlacement()
        {
            if (_settings == null)
                return;

            var maximized = WindowState == WindowState.Maximized;
            var previous = _settings.Window;

            var width = maximized ? previous.Width : Width;
            var height = maximized ? previous.Height : Height;
            var left = maximized ? previous.Left : Position.X;
            var top = maximized ? previous.Top : Position.Y;

            _settings.Window = new WindowPlacement(width, height, left, top, maximized);
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
