namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Read-only preview of a registered loot table.</summary>
    public sealed record CreatureLootTableInfo(
        string Id,
        string DisplayName,
        bool IsRare,
        IReadOnlyList<CreatureLootTableItemInfo> Items,
        string DefinitionTypeName = "");
}
