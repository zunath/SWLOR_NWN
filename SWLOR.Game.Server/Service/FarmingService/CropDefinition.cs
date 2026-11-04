namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>One crop: what it is called, its tier and level, how long it grows, the seed that plants it and the item it yields.</summary>
    public class CropDefinition
    {
        public CropType Type { get; set; }

        public string Name { get; set; }

        /// <summary>1 to 5. Crop Management must reach this tier to plant the crop.</summary>
        public int Tier { get; set; }

        /// <summary>The level that sets the crop's Agriculture XP: 5, 15, 25, 35 or 45, the harvesting vein levels of the tier.</summary>
        public int Level { get; set; }

        /// <summary>Hours from planting to ripe.</summary>
        public int GrowHours { get; set; }

        /// <summary>The seed item planted (one is used up).</summary>
        public string SeedResref { get; set; }

        /// <summary>The item a harvest yields.</summary>
        public string HarvestResref { get; set; }

        /// <summary>
        /// True for a woodlot sapling: it grows only in an outdoor city plot, takes no tending, and yields a flat
        /// <see cref="WoodlotRules.LogsPerHarvest"/> logs whatever perks, fertilizer or bonuses apply to crops.
        /// </summary>
        public bool IsSapling { get; set; }

        /// <summary>What the planting list calls the item that plants this crop: "Orange seed", or "Wood Tree sapling".</summary>
        public string GetSeedLabel() => IsSapling ? $"{Name} sapling" : $"{Name} seed";
    }
}
