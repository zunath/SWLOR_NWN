namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// One itempropdef.2da property whose Subtype ranges over a large or open-ended lookup table
    /// (a full crafting reagent list, every droid instruction, every NPC skill, ...) rather than
    /// the small fixed sets <see cref="ItemStatCatalog"/> and <see cref="ItemRequirementCatalog"/>
    /// expand into one row per subtype. Declared once per property, not once per subtype: the
    /// concrete subtype the item actually stores is a full editor/picker concern this catalog does
    /// not own.
    /// </summary>
    /// <param name="Label">itempropdef.2da's own Label column text for this property.</param>
    /// <param name="PropertyId">itempropdef.2da row - the itemproperty struct's PropertyName value.</param>
    /// <param name="SubtypeTableResRef">
    /// itempropdef.2da's SubTypeResRef value - the 2da a picker would read subtype choices from.
    /// </param>
    /// <param name="CostTableId">
    /// itempropdef.2da's CostTableResRef row, or -1 when itempropdef.2da declares none.
    /// </param>
    /// <param name="Context">
    /// Which <see cref="ItemStatGroup"/> this property's editor surfaces under, or null when
    /// <see cref="IsRequirement"/> is true instead (an equip requirement is never shown alongside
    /// a stat group).
    /// </param>
    /// <param name="IsRequirement">
    /// True for UseLimitationPerk (100) and UseLimitationRacial (64) - these gate equipping
    /// rather than contribute a stat, so they surface through the Requirements section instead of
    /// a stat group.
    /// </param>
    public sealed record ItemMultiEntryDefinition(
        string Label,
        int PropertyId,
        string SubtypeTableResRef,
        int CostTableId,
        ItemStatGroup? Context,
        bool IsRequirement = false);
}
