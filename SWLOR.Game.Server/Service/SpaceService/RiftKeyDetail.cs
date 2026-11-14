namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A Rift Key (plan section 31): its tier and what its pocket spawns and pays.</summary>
    public class RiftKeyDetail
    {
        public string Resref { get; set; }

        /// <summary>The tier in tenths: 10-50, or 55 for tier 5.5.</summary>
        public int TierTenths { get; set; }

        public string Name { get; set; }

        /// <summary>The creature tag prefix of the pocket's ships: t1-t5, or rr for the Rift Raiders.</summary>
        public string CreaturePrefix { get; set; }

        /// <summary>The elite loot table the payout pulls from.</summary>
        public string LootTable { get; set; }

        /// <summary>The credits a finished run pays at a multiplier of 1.</summary>
        public int BaseCredits { get; set; }

        /// <summary>The template area this key's pocket is an instance of (one per tier 1-5; tier 5.5 the original).</summary>
        public string Template { get; set; }

        public string TierLabel => TierTenths % 10 == 0 ? (TierTenths / 10).ToString() : $"{TierTenths / 10}.{TierTenths % 10}";
    }
}
