namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>One itemprop.2da property row (optionally one subtype of it) as a catalog entry.</summary>
    /// <param name="Group">The section this stat is presented under.</param>
    /// <param name="Label">Display text, already naming the subtype where the property has one.</param>
    /// <param name="PropertyId">itempropdef.2da row - the itemproperty struct's PropertyName value.</param>
    /// <param name="SubtypeId">
    /// itempropdef.2da's SubTypeResRef row, or -1 when this property has no subtype table. A store
    /// carrying that property writes 0 for Subtype in that case, not -1 or 255; see
    /// <see cref="ItemValueStore"/>'s remarks for why 0 and -1 read as the same thing.
    /// </param>
    /// <param name="CostTableId">
    /// itempropdef.2da's CostTableResRef row - the byte an itemproperty struct's CostTable field
    /// stores. -1 when itempropdef.2da declares none (the property's value lives in Subtype
    /// instead, e.g. DroidPartType).
    /// </param>
    /// <param name="DisplayOrder">Stable ordering within <see cref="Group"/>.</param>
    public sealed record ItemStatDefinition(
        ItemStatGroup Group,
        string Label,
        int PropertyId,
        int SubtypeId,
        int CostTableId,
        int DisplayOrder);
}
