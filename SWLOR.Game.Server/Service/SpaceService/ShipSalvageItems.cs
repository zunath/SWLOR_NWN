using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The item resrefs salvage and gas harvesting yield (plan section 12). Every string literal here is an item
    /// resref: EconomyObtainabilityCoverageTests and tools/FlagNpcEconomyItems.py read this file as a source.
    /// </summary>
    public static class ShipSalvageItems
    {
        public const string TibannaGas = "gas_tibanna";
        public const string Aurodium = "aurodium";

        /// <summary>Ship Scrap of tiers 1-5, in tier order. The Refinery turns each into that tier's electronics.</summary>
        public static readonly IReadOnlyList<string> ShipScrap = new[]
        {
            "ship_scrap_1",
            "ship_scrap_2",
            "ship_scrap_3",
            "ship_scrap_4",
            "ship_scrap_5",
        };
    }
}
