using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SWLOR.Toolset.Shell.Views
{
    public enum EditorDialogChoice
    {
        Cancel,
        Primary,
        Secondary
    }

    /// <summary>A small modal three-way decision dialog used for destructive editor choices.</summary>
    public partial class EditorChoiceDialog : Window
    {
        private bool _closeScheduled;

        public EditorChoiceDialog()
        {
            InitializeComponent();
        }

        private void OnPrimaryClicked(object? sender, RoutedEventArgs e) =>
            ScheduleClose(EditorDialogChoice.Primary, e);

        private void OnSecondaryClicked(object? sender, RoutedEventArgs e) =>
            ScheduleClose(EditorDialogChoice.Secondary, e);

        private void OnCancelClicked(object? sender, RoutedEventArgs e) =>
            ScheduleClose(EditorDialogChoice.Cancel, e);

        /// <summary>
        /// Lets Avalonia finish routing the button's input before destroying the native window.
        /// Closing synchronously inside the click callback leaves the remainder of the current raw
        /// input pass targeting a TopLevel whose PlatformImpl has already been cleared.
        /// </summary>
        private void ScheduleClose(EditorDialogChoice choice, RoutedEventArgs e)
        {
            e.Handled = true;
            if (_closeScheduled)
                return;

            _closeScheduled = true;
            Dispatcher.UIThread.Post(
                () => Close(choice),
                DispatcherPriority.Background);
        }

        /// <param name="secondaryLabel">
        /// Null for a two-button prompt (act or cancel) - the middle button is hidden rather than given
        /// a filler label, because a confirm-or-cancel question with three buttons reads as a trap.
        /// </param>
        public static Task<EditorDialogChoice> ShowAsync(
            string headline,
            string message,
            string primaryLabel,
            string? secondaryLabel)
        {
            try
            {
                var owner = (Avalonia.Application.Current?.ApplicationLifetime
                    as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (owner == null)
                    return Task.FromResult(EditorDialogChoice.Cancel);

                var dialog = new EditorChoiceDialog();
                dialog.HeadlineText.Text = headline;
                dialog.MessageText.Text = message;
                dialog.PrimaryButton.Content = primaryLabel;
                dialog.SecondaryButton.Content = secondaryLabel;
                dialog.SecondaryButton.IsVisible = secondaryLabel != null;
                return dialog.ShowDialog<EditorDialogChoice>(owner);
            }
            catch
            {
                // A missing owner or UI failure defaults to cancel. Destructive choices must
                // never be inferred when the prompt cannot be shown.
                return Task.FromResult(EditorDialogChoice.Cancel);
            }
        }
    }
}
