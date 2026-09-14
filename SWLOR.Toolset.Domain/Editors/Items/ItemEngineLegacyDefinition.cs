namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// One base-game NWN:EE itemproperty row still carried by items in the corpus (armor, on-hit
    /// spells, keen, regeneration, ...) that SWLOR's own item editor stat groups never modeled -
    /// these predate the SWLOR-authored property block and are not something a builder assigns
    /// through this editor, but they must still be accounted for so a corpus coverage sweep does
    /// not flag every armor/weapon blueprint that carries one.
    /// </summary>
    /// <param name="Label">itempropdef.2da's own Label column text for this property.</param>
    /// <param name="PropertyId">itempropdef.2da row - the itemproperty struct's PropertyName value.</param>
    /// <param name="SubtypeTableResRef">
    /// itempropdef.2da's SubTypeResRef value, or null when the property declares none.
    /// </param>
    /// <param name="CostTableId">
    /// itempropdef.2da's CostTableResRef row, or -1 when itempropdef.2da declares none.
    /// </param>
    public sealed record ItemEngineLegacyDefinition(
        string Label,
        int PropertyId,
        string? SubtypeTableResRef,
        int CostTableId);
}
