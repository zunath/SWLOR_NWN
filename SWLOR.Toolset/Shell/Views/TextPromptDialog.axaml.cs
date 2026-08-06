using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;

namespace SWLOR.Toolset.Shell.Views
{
    /// <summary>
    /// A small modal one-field prompt, for the places a name is the whole interaction - creating or
    /// renaming a palette category.
    /// </summary>
    /// <remarks>
    /// Deliberately a dialog rather than in-place editing in the tree. The palette's category rows are
    /// one line tall and already carry a twisty, a pin star and a count; growing an editable text box
    /// inside one costs more layout trouble than the interaction is worth, and a dialog can say what the
    /// name is for.
    /// </remarks>
    public partial class TextPromptDialog : Window
    {
        public TextPromptDialog()
        {
            InitializeComponent();
        }

        private void OnConfirmClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Confirm();

        private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);

        /// <summary>Enter commits from inside the box, which is where the caret already is.</summary>
        private void OnValueKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            Confirm();
        }

        /// <summary>Closes with the trimmed value, or with null when it is blank - a blank name is a cancel.</summary>
        private void Confirm()
        {
            var value = ValueBox.Text?.Trim();
            Close(string.IsNullOrEmpty(value) ? null : value);
        }

        /// <summary>
        /// Shows the prompt and returns the entered text, or null when the user cancelled or left it
        /// blank. Returns null rather than throwing when there is no window to own the dialog.
        /// </summary>
        public static Task<string?> ShowAsync(
            string headline,
            string message,
            string initialValue,
            string confirmLabel)
        {
            try
            {
                var owner = (Avalonia.Application.Current?.ApplicationLifetime
                    as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (owner == null)
                    return Task.FromResult<string?>(null);

                var dialog = new TextPromptDialog();
                dialog.HeadlineText.Text = headline;
                dialog.MessageText.Text = message;
                dialog.ConfirmButton.Content = confirmLabel;
                dialog.ValueBox.Text = initialValue;
                dialog.Opened += (_, _) =>
                {
                    // Focused and pre-selected: renaming is usually a replacement, not an edit.
                    dialog.ValueBox.Focus();
                    dialog.ValueBox.SelectAll();
                };

                return dialog.ShowDialog<string?>(owner);
            }
            catch
            {
                return Task.FromResult<string?>(null);
            }
        }
    }
}
