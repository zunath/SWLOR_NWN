using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The crop growing in one garden plot structure (Player City Revamp, "Farming"). A plot with no record is empty. The
    /// record belongs to its planter: it counts toward their CD key's crop cap, only they harvest it, and the harvest uses
    /// the perk ranks they hold at that moment. The id is <see cref="KeyFor"/> of the plot's structure id, never the structure
    /// id itself, since the DB's per-frame entity cache is keyed by id alone and a Get of this record in the frame that loaded
    /// the structure would return the WorldProperty and throw. A new type, so no migration.
    /// </summary>
    public class GardenPlot : EntityBase
    {
        public GardenPlot()
        {
            PlotId = string.Empty;
            PlanterPlayerId = string.Empty;
            PlanterCdKey = string.Empty;
        }

        /// <summary>The record's key for a plot structure's property id.</summary>
        public static string KeyFor(string plotId) => "garden-plot:" + plotId;

        /// <summary>The garden plot structure's property id.</summary>
        [Indexed]
        public string PlotId { get; set; }

        /// <summary>The character who planted the crop.</summary>
        [Indexed]
        public string PlanterPlayerId { get; set; }

        /// <summary>The planter's public CD key. The crop cap counts every character that shares it.</summary>
        [Indexed]
        public string PlanterCdKey { get; set; }

        public CropType Crop { get; set; }

        public DateTime PlantedUtc { get; set; }

        /// <summary>When the crop can first be tended: a quarter of its grow time after planting.</summary>
        public DateTime TendReadyUtc { get; set; }

        /// <summary>Whether the crop has been tended. It can be tended once.</summary>
        public bool Tended { get; set; }

        /// <summary>Whether the crop was fertilized when it was planted.</summary>
        public bool Fertilized { get; set; }

        /// <summary>When the crop is ripe and can be harvested.</summary>
        public DateTime RipeUtc { get; set; }

        /// <summary>When a crop left ripe withers: <c>FarmingRules.WitherHours</c> after <see cref="RipeUtc"/>.</summary>
        public DateTime WitherUtc { get; set; }

        /// <summary>The last time the plot was brought up to date. Farmhands replay time from here.</summary>
        public DateTime LastProcessedUtc { get; set; }
    }
}
