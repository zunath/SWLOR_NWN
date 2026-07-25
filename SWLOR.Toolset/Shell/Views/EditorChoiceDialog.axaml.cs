using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

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
        public EditorChoiceDialog()
        {
            InitializeComponent();
        }

        private void OnPrimaryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
            Close(EditorDialogChoice.Primary);

        private void OnSecondaryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
            Close(EditorDialogChoice.Secondary);

        private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
            Close(EditorDialogChoice.Cancel);

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
