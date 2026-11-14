namespace SWLOR.Game.Server.Service.LivestockService
{
    /// <summary>
    /// The animals a corral holds (Player City Revamp, "Farming: Livestock"): seven in five tiers, each a creature whose meat
    /// cooking already uses. The values are stored on <c>Livestock</c> records, so an animal keeps its number.
    /// </summary>
    public enum LivestockType
    {
        Invalid = 0,

        // Tier 1
        Warocas = 1,

        // Tier 2
        ValleyCairnmog = 2,
        Gimpassa = 3,

        // Tier 3
        Klorslug = 4,
        Aradile = 5,

        // Tier 4
        Womprat = 6,

        // Tier 5
        Thune = 7,
    }
}
