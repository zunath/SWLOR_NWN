using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    public class CraftedFoodDelivery : EntityBase
    {
        public const string RecoveryVariable = "CRAFTED_FOOD_RECOVERY";

        [Indexed]
        public string PlayerId { get; set; } = string.Empty;

        public List<string> Items { get; set; } = new();
        public int ClaimRevision { get; set; }
    }
}
