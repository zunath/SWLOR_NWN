using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace SWLOR.Toolset.Shell.Views
{
    /// <summary>
    /// A modal "this operation was aborted" dialog: a headline, an explanation, and an optional
    /// list of offending details. Used when the toolset refuses to proceed with something rather
    /// than risk touching data it cannot represent correctly (see
    /// <see cref="Domain.Editors.DropdownValueValidator"/>).
    /// </summary>
    public partial class ErrorDialog : Window
    {
        public ErrorDialog()
        {
            InitializeComponent();
        }

        private void OnOkClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

        /// <summary>
        /// Shows the dialog over the main window. Safe to call from any thread and when no main
        /// window exists yet (headless/startup) - in that case the dialog is skipped rather than
        /// throwing, since a failed diagnostic must never take the app down.
        /// </summary>
        public static void Show(string headline, string message, IReadOnlyList<string> details)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => Show(headline, message, details));
                return;
            }

            try
            {
                var owner = (Avalonia.Application.Current?.ApplicationLifetime
                    as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

                var dialog = new ErrorDialog();
                dialog.HeadlineText.Text = headline;
                dialog.MessageText.Text = message;
                dialog.DetailsList.ItemsSource = details;
                dialog.DetailsBorder.IsVisible = details.Count > 0;

                if (owner != null)
                    _ = dialog.ShowDialog(owner);
                else
                    dialog.Show();
            }
            catch
            {
                // A dialog that cannot be shown must not escalate into a crash; the caller has
                // already logged the same information to the Output panel.
            }
        }
    }
}
