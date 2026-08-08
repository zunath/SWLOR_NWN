namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>One possible result in a registered loot table.</summary>
    public sealed record CreatureLootTableItemInfo(
        string ResRef,
        int Weight,
        int MaximumQuantity,
        bool IsRare);
}
