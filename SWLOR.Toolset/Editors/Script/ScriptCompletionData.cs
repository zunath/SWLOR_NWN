using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using SWLOR.Toolset.Domain.Script;

namespace SWLOR.Toolset.Editors.Script
{
    /// <summary>
    /// Adapts a Domain <see cref="CompletionItem"/> to AvaloniaEdit's completion list.
    /// </summary>
    /// <remarks>
    /// Deliberately thin. Ranking and filtering already happened in Domain where they are unit
    /// tested, so this type must not re-sort or re-filter - <see cref="Priority"/> is handed out in
    /// list order so AvaloniaEdit preserves the order it was given.
    /// </remarks>
    public sealed class ScriptCompletionData : ICompletionData
    {
        private readonly CompletionItem _item;
        private readonly int _replaceFrom;

        public ScriptCompletionData(CompletionItem item, int replaceFrom, double priority)
        {
            _item = item;
            _replaceFrom = replaceFrom;
            Priority = priority;
        }

        public IImage? Image => null;

        public string Text => _item.Text;

        /// <summary>The row as drawn: name on the left, the detail hint dimmed on the right.</summary>
        public object Content => _item.Detail == null
            ? _item.Text
            : new Avalonia.Controls.TextBlock { Text = $"{_item.Text}    {_item.Detail}" };

        public object? Description => _item.Documentation;

        public double Priority { get; }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            // Replace from where the partial word started, not from where the popup opened: the user
            // keeps typing while it is open, and AvaloniaEdit's own segment starts at the trigger.
            var start = Math.Min(_replaceFrom, completionSegment.EndOffset);
            var length = completionSegment.EndOffset - start;

            textArea.Document.Replace(start, length, _item.Text);
        }
    }
}
