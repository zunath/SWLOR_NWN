namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One left/right body-part field pair on a ModelType 3 (armor) blueprint, plus each side's
    /// truncated "x" twin field name when the corpus carries one - null when a side has no twin at
    /// all. See <see cref="ItemAppearanceFieldNames"/> for the verified spellings.
    /// </summary>
    public sealed record ItemArmorPartFieldPair(
        string Label,
        string LeftField,
        string? LeftTwinField,
        string RightField,
        string? RightTwinField);
}
