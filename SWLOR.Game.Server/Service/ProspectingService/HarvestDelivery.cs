namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>What a harvest puts in the player's inventory: rolls of the node's loot table, extra ore rolls and gems.</summary>
    /// <param name="Rolls">
    /// Plain units: one roll of the node's whole loot table for each, with the node's extra loot tables rolled once for each, exactly as
    /// each of today's harvests did.
    /// </param>
    /// <param name="ExtraOreRolls">Extra rolls of the table's non-gem entries, from a rich seam's bonus.</param>
    /// <param name="Gems">Gems drawn from the table's gem entries, from gem pockets.</param>
    public readonly record struct HarvestDelivery(int Rolls, int ExtraOreRolls, int Gems)
    {
        /// <summary>The items delivered; each is one unit of XP.</summary>
        public int Items => Rolls + ExtraOreRolls + Gems;

        public bool IsEmpty => Items == 0;
    }
}
