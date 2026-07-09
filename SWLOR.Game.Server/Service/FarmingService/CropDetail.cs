using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FarmingService
{
    public class CropDetail
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int RequiredRank { get; set; }
        public string SeedResref { get; set; }
        public Dictionary<string, int> Yields { get; set; }
        public int SecondsPerStage { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// Resref of the pristine variant of this crop's produce.
        /// Empty when the crop has no pristine variant.
        /// </summary>
        public string PristineResref { get; set; }

        /// <summary>
        /// Effective level of the crop, used for XP delta calculations.
        /// Mirrors the recipe unlock offset so a freshly unlocked crop grants favorable XP.
        /// </summary>
        public int Level => RequiredRank + 3;

        public CropDetail()
        {
            Name = string.Empty;
            Description = string.Empty;
            SeedResref = string.Empty;
            Yields = new Dictionary<string, int>();
            IsActive = true;
            PristineResref = string.Empty;
        }
    }
}
