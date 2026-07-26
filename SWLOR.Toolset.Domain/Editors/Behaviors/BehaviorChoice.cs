namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>One named value of a Choice row.</summary>
    /// <remarks>
    /// ToString is the display name and nothing else. A combo box falls back to ToString when it has
    /// no item template, and a record default renders the whole object shape instead.
    /// </remarks>
    public sealed class BehaviorChoice
    {
        /// <summary>The stored number for an integer-backed choice.</summary>
        public long Value { get; }

        /// <summary>The stored text for a string-backed choice.</summary>
        public string? StringValue { get; }

        public string Display { get; }

        public string? ImageResRef { get; }

        public bool IsStringValue => StringValue != null;

        public BehaviorChoice(long value, string display, string? imageResRef = null)
        {
            Value = value;
            Display = display;
            ImageResRef = imageResRef;
        }

        public BehaviorChoice(string value, string display, string? imageResRef = null)
        {
            StringValue = value ?? throw new ArgumentNullException(nameof(value));
            Display = display;
            ImageResRef = imageResRef;
        }

        public override string ToString() => Display;
    }
}
