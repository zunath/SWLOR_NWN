namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>One entry of the Area Contents panel's grouping selector.</summary>
    public sealed class AreaContentsGroupingOption
    {
        public AreaContentsGroupingOption(AreaContentsGrouping value, string label, string description)
        {
            Value = value;
            Label = label;
            Description = description;
        }

        public AreaContentsGrouping Value { get; }

        public string Label { get; }

        /// <summary>The tooltip: what this grouping is good for, in one line.</summary>
        public string Description { get; }

        public override string ToString() => Label;
    }
}
