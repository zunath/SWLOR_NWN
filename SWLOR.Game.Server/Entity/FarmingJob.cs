using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Entity
{
    public class FarmingJob: EntityBase
    {
        [Indexed]
        public string ParentPropertyId { get; set; }

        [Indexed]
        public string PlayerId { get; set; }

        public CropType CropType { get; set; }

        public DateTime DatePlanted { get; set; }

        /// <summary>
        /// Duration of each growth stage, snapshotted at planting time with all speed bonuses applied.
        /// </summary>
        public int StageDurationSeconds { get; set; }

        /// <summary>
        /// The highest growth stage (1-based) the player has tended. Each stage may be tended once.
        /// </summary>
        public int LastTendedStage { get; set; }

        /// <summary>
        /// The highest growth stage (1-based) the player has fertilized. Each stage accepts one fertilizer.
        /// </summary>
        public int LastFertilizedStage { get; set; }

        public int TendBonusPercent { get; set; }

        public int PristineChanceBonusPercent { get; set; }
    }
}
