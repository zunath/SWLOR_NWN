using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SWLOR.Toolset.Editors
{
    public partial class ConversationEditorView : UserControl
    {
        public ConversationEditorView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Commits an edited line when focus leaves it, rather than on every keystroke.
        /// </summary>
        /// <remarks>
        /// Per-keystroke commits would put one undo step on the stack per character, and every
        /// commit redraws the rail and re-runs the analyzer — so typing a sentence would mean
        /// re-analysing the conversation forty times. Losing focus is the natural boundary: it is
        /// what the writer means by "done with this line".
        /// </remarks>
        private void OnLineLostFocus(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ConversationEditorViewModel viewModel)
                viewModel.CommitLineCommand.Execute(null);
        }

        private void OnChoiceLostFocus(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ConversationEditorViewModel viewModel
                && sender is TextBox { Tag: ChoiceRowViewModel choice } textBox)
            {
                viewModel.CommitChoiceText(choice, textBox.Text);
            }
        }

        private void OnMerchantLostFocus(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ConversationEditorViewModel viewModel)
                viewModel.CommitMerchantCommand.Execute(null);
        }

        private void OnMerchantStoreDropDownOpened(object? sender, EventArgs e)
        {
            if (DataContext is ConversationEditorViewModel viewModel)
                _ = viewModel.LoadMerchantStoresAsync();
        }

        private void OnAdvancedLostFocus(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ConversationEditorViewModel viewModel)
                viewModel.CommitAdvancedCommand.Execute(null);
        }
    }
}
