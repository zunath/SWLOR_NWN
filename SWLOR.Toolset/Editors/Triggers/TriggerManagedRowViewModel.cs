using SWLOR.Toolset.Domain.Editors.Triggers;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>One line of the "what this behavior manages" block: the value, and whether it holds.</summary>
    public sealed class TriggerManagedRowViewModel
    {
        public string Label { get; }

        public string Value { get; }

        /// <summary>False when the document disagrees with the behavior — the tick is the assurance.</summary>
        public bool IsApplied { get; }

        public TriggerManagedRowViewModel(TriggerManagedValue value, bool isApplied)
        {
            Label = value.Label;
            Value = value.DisplayText;
            IsApplied = isApplied;
        }
    }
}
