using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>
    /// A value a behavior writes on the builder's behalf — the contents of the editor's "what this
    /// behavior manages" block. Applied when the behavior is chosen and cleared when it is swapped
    /// out, so an object never keeps the leavings of a behavior it no longer has.
    /// </summary>
    public sealed class BehaviorManagedValue
    {
        public required string Label { get; init; }

        /// <summary>Field name on the object struct, or the local's name when Storage is Local.</summary>
        public required string Name { get; init; }

        public BehaviorFieldStorage Storage { get; init; } = BehaviorFieldStorage.Field;

        public GffFieldType FieldType { get; init; } = GffFieldType.Int;

        /// <summary>The integer this value is pinned to, when it is a number.</summary>
        public long? IntValue { get; init; }

        /// <summary>The string this value is pinned to, when it is text.</summary>
        public string? StringValue { get; init; }

        /// <summary>The float this value is pinned to.</summary>
        public double? FloatValue { get; init; }

        /// <summary>What the manages block shows; falls back to the pinned value.</summary>
        public string? Display { get; init; }

        /// <summary>
        /// Only written to a placement. A blueprint's TemplateResRef is its own file name, so a
        /// behavior the runtime identifies by resref can set it on an instance but must not try on
        /// the blueprint.
        /// </summary>
        public bool IsInstanceOnly { get; init; }

        /// <summary>
        /// Whether swapping behavior clears this value. False for anything an object cannot be left
        /// without: blanking an instance's TemplateResRef would orphan it, so choosing a different
        /// behavior leaves the blueprint reference alone.
        /// </summary>
        public bool ClearOnSwap { get; init; } = true;

        public string DisplayText =>
            Display
            ?? StringValue
            ?? IntValue?.ToString()
            ?? FloatValue?.ToString("0.0##")
            ?? string.Empty;
    }
}
