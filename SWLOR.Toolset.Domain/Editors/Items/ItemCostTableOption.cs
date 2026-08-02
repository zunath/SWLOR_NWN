namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// One selectable row of an item-property cost table: the CostValue stored on the blueprint, and
    /// the label that row displays.
    /// </summary>
    /// <remarks>
    /// The two are not the same number. iprp_delay's row 11 is labelled "110", so an editor that
    /// shows the stored value is showing a row index while the builder reads it as a delay.
    /// </remarks>
    /// <param name="Value">The CostValue written to the item property.</param>
    /// <param name="Label">
    /// What that row means: its semantic Amount when the table supplies one, otherwise its authored
    /// Label.
    /// </param>
    public readonly record struct ItemCostTableOption(int Value, string Label)
    {
        public override string ToString() => Label;
    }
}
