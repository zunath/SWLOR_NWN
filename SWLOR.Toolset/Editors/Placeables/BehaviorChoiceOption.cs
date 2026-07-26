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
    public sealed record BehaviorChoiceOption(string Value, string Display)
    {
        public override string ToString() => Display;
    }
}
