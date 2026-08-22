namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// One selectable value of a behavior field: what gets stored, and what the builder reads.
    /// </summary>
    /// <param name="Value">
    /// The value written to the local variable. For id-backed sources (key items, skills, visual
    /// effects) this is the number as text, so one option type covers both flavours.
    /// </param>
    /// <param name="Display">What the picker shows.</param>
    /// <param name="Group">Optional gallery group, such as FNF or DUR for visual effects.</param>
    /// <param name="Details">Optional short explanation shown under the selected value.</param>
    /// <param name="ImageUrl">Optional screenshot used by an artwork gallery.</param>
    public sealed record BehaviorChoiceOption(
        string Value,
        string Display,
        string? Group = null,
        string? Details = null,
        string? ImageUrl = null)
    {
        public override string ToString() => Display;
    }
}
