using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Shell
{
    public partial class MainWindow : Window
    {
        private bool _closeApproved;
        private bool _closePromptOpen;
        private ShellViewModel? _viewModel;
        private ToolsetSettings? _settings;

        /// <summary>
        /// The geometry the window last had while it was neither maximised nor minimised - the size and
        /// position worth restoring. Tracked continuously rather than read off the window at close time,
        /// because by then the window may be maximised (reporting the screen's size) or minimised
        /// (reporting a parked off-screen position), and either way what the builder actually set is gone.
        /// </summary>
        private WindowPlacement _restorable = WindowPlacement.Unset;

        private bool _hasRestorable;
        private bool _isMaximized;
        private bool _isOpen;
        private DispatcherTimer? _placementSaveTimer;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates the lightweight first-paint window. Its menus and workspace remain disabled until
        /// <see cref="AttachViewModel"/> replaces the startup panel with the interactive shell while
        /// optional game-data indexes continue loading.
        /// </summary>
        public MainWindow(ToolsetSettings? settings) : this()
        {
            _settings = settings;
            RestorePlacement();

            // Both halves of a placement change arrive on their own event, and a maximise/restore comes
            // through as a plain property change - so all three feed the same tracker.
            PositionChanged += (_, _) => TrackPlacement();
            PropertyChanged += (_, e) =>
            {
                if (e.Property == ClientSizeProperty || e.Property == WindowStateProperty)
                    TrackPlacement();
            };

            Opened += (_, _) =>
            {
                // Nothing before this point is a placement worth recording: a window that has not been
                // shown reports a size that has been set but a position that has not, and saving that
                // pair would trade the remembered position for the origin.
                _isOpen = true;
                TrackPlacement();
            };

            Closing += OnClosing;
        }

        /// <summary>
        /// Compatibility constructor for callers that already have a composed shell. Normal app
        /// startup uses the settings-only constructor so the window can paint before composition.
        /// </summary>
        public MainWindow(ShellViewModel viewModel, ToolsetSettings? settings = null) : this(settings)
        {
            AttachViewModel(viewModel);
            Opened += async (_, _) => await viewModel.InitializeAsync();
        }

        /// <summary>
        /// Activates the fully loaded shell and its shortcuts. Called on the UI thread after the
        /// background bootstrap finishes.
        /// </summary>
        public void AttachViewModel(ShellViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            if (_viewModel != null)
                throw new InvalidOperationException("The main window already has an attached shell.");

            _viewModel = viewModel;
            DataContext = viewModel;
            StartupPanel.IsVisible = false;

            // File > Exit goes through Close() so it hits the same unsaved-changes prompt below
            // rather than dropping edits on the floor.
            viewModel.ExitRequested += Close;

            // The menu's InputGesture text is only a label - these are the actual shortcuts. They
            // live on the window (not on the MenuItems) so they work whether or not the menu has
            // ever been opened. Avalonia stops walking KeyBindings once the event is handled, so a
            // focused text box keeps its own Ctrl+Z for text and only unhandled presses reach the
            // active editor's document history.
            // Save shortcuts flush the focused spinner first: NumericUpDown only pushes its typed
            // text into Value on Enter/spin/focus loss, so a keyboard save with the caret still in
            // the box would otherwise write the file without the number the builder can see.
            Bind(Key.S, KeyModifiers.Control, viewModel.SaveCommand, flushPendingEdits: true);
            Bind(Key.S, KeyModifiers.Control | KeyModifiers.Shift, viewModel.SaveAllCommand, flushPendingEdits: true);
            Bind(Key.Z, KeyModifiers.Control, viewModel.UndoCommand);
            Bind(Key.Y, KeyModifiers.Control, viewModel.RedoCommand);

            // Ctrl+B compiles the active script, Ctrl+Shift+B builds them all. Ctrl+B is also handled
            // by the script editor so it fires with the buffer focused; that path marks the key
            // handled, so it never runs twice.
            Bind(Key.B, KeyModifiers.Control, viewModel.CompileActiveScriptCommand);
            Bind(Key.B, KeyModifiers.Control | KeyModifiers.Shift, viewModel.BuildAllScriptsCommand);
        }

        /// <summary>Leaves the responsive startup surface visible and replaces its progress text.</summary>
        public void ShowStartupError(string message)
        {
            StartupPanel.IsVisible = true;
            StartupProgress.IsIndeterminate = false;
            StartupStatus.Text = message;
        }

        private async void OnClosing(object? sender, WindowClosingEventArgs args)
        {
            // A number still being typed has not reached the document yet, and the title-bar close
            // button does not move keyboard focus - without this the dirty check below would not
            // even know there is an edit to prompt about.
            CommitPendingSpinnerEdit();

            // Recorded on the first close attempt, before the unsaved-changes prompt can cancel
            // it: the prompt is a window of its own, and by the time a cancelled attempt comes
            // back around this window may have been moved by it.
            SavePlacement();

            // Closing during background bootstrap is always safe: no module or editor has been
            // attached yet, and App will dispose the completed service graph instead of attaching it.
            if (_viewModel == null)
                return;

            _viewModel.SaveLayout();

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
                if (await _viewModel.TryCloseAsync().ConfigureAwait(true))
                {
                    _closeApproved = true;
                    Close();
                }
            }
            finally
            {
                _closePromptOpen = false;
            }
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

            if (placement.HasPosition && placement.IsOnAnyScreen(CurrentScreens()))
            {
                // Manual placement only sticks if Avalonia is not also centring the window.
                WindowStartupLocation = WindowStartupLocation.Manual;
                Position = new PixelPoint((int)placement.Left, (int)placement.Top);
            }

            if (placement.IsMaximized)
                WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// The bounds of every connected display, or an empty list when they cannot be read - which is
        /// treated as "no reason to doubt the saved position" rather than as "nothing is on screen".
        /// </summary>
        private IReadOnlyList<ScreenBounds> CurrentScreens()
        {
            try
            {
                var all = Screens?.All;
                if (all == null)
                    return Array.Empty<ScreenBounds>();

                var bounds = new List<ScreenBounds>(all.Count);
                foreach (var screen in all)
                {
                    var area = screen.WorkingArea;
                    bounds.Add(new ScreenBounds(area.X, area.Y, area.Width, area.Height));
                }

                return bounds;
            }
            catch (Exception)
            {
                // Some backends refuse to enumerate screens before the window is shown; a saved position
                // is no worse off than it was before this check existed.
                return Array.Empty<ScreenBounds>();
            }
        }

        /// <summary>
        /// Notes the window's current geometry, and queues a save. Only a normal (neither maximised nor
        /// minimised) window contributes size and position; the other two states contribute nothing but
        /// the maximised flag, so what is remembered stays the window the builder actually sized.
        /// </summary>
        private void TrackPlacement()
        {
            if (_settings == null || !_isOpen || WindowState == WindowState.Minimized)
                return;

            _isMaximized = WindowState == WindowState.Maximized;

            if (!_isMaximized)
            {
                var size = ClientSize;

                // A window reports a few pixels while it is being torn down or first laid out; recording
                // that would restore a window the builder cannot find.
                if (size.Width >= WindowPlacement.MinimumRestorableSize &&
                    size.Height >= WindowPlacement.MinimumRestorableSize)
                {
                    _restorable = new WindowPlacement(
                        size.Width, size.Height, Position.X, Position.Y, false);
                    _hasRestorable = true;
                }
            }

            QueuePlacementSave();
        }

        /// <summary>
        /// Coalesces the stream of events a single drag-resize produces into one settings write, shortly
        /// after the builder lets go. Saving as the window is dragged would write the file per frame;
        /// saving only on close would lose everything if the process is killed rather than closed.
        /// </summary>
        private void QueuePlacementSave()
        {
            if (_placementSaveTimer == null)
            {
                _placementSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _placementSaveTimer.Tick += (_, _) =>
                {
                    _placementSaveTimer!.Stop();
                    SavePlacement();
                };
            }

            _placementSaveTimer.Stop();
            _placementSaveTimer.Start();
        }

        /// <summary>
        /// Writes the tracked placement out. Falls back to what is already saved when this session never
        /// had a normal-state window (started maximised and closed maximised), so restoring a maximised
        /// window still un-maximises to the size from the session before it.
        /// </summary>
        private void SavePlacement()
        {
            if (_settings == null)
                return;

            _placementSaveTimer?.Stop();

            var basis = _hasRestorable ? _restorable : _settings.Window;
            _settings.Window = basis with { IsMaximized = _isMaximized };
        }

        private void Bind(Key key, KeyModifiers modifiers, ICommand command, bool flushPendingEdits = false)
        {
            KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(key, modifiers),
                Command = flushPendingEdits ? new PendingEditFlushingCommand(this, command) : command
            });
        }

        /// <summary>
        /// Commits a spinner edit still sitting in the focused control by cycling focus through the
        /// window, which fires the LostFocus commit the control was waiting for, then hands focus
        /// back so the builder's caret does not jump.
        /// </summary>
        internal void CommitPendingSpinnerEdit()
        {
            if (FocusManager?.GetFocusedElement() is not Control focused)
                return;

            var spinner = focused as NumericUpDown ?? focused.FindAncestorOfType<NumericUpDown>();
            if (spinner == null)
                return;

            Focus();
            focused.Focus();
        }
    }
}
