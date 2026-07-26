using Avalonia.Media.Imaging;
using SWLOR.Toolset.Domain.Editors.Triggers;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// One entry of a choice row, with its artwork when the choice has any. A load screen is a
    /// picture, and picking one by name means guessing what "17 Tatooine" looks like.
    /// </summary>
    public sealed class TriggerChoiceViewModel
    {
        public TriggerChoice Choice { get; }

        public Bitmap? Preview { get; }

        public long Value => Choice.Value;

        public string Display => Choice.Display;

        public bool HasPreview => Preview != null;

        public TriggerChoiceViewModel(TriggerChoice choice, Bitmap? preview)
        {
            Choice = choice;
            Preview = preview;
        }

        /// <summary>The combo box falls back to this for the closed, selected row.</summary>
        public override string ToString() => Display;
    }
}
