namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>One normalized LOOT_TABLE_n local.</summary>
    public sealed record CreatureLootEntry(string TableId, int Chance, int Pulls);
}
