namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A station fee a player paid for a craft (a Shipyard's fee for a Capital hull) that the station's treasury has not been credited yet.
    /// The credits leave the player with the craft's debit, which the character file holds only from <see cref="Sequence"/> on; the treasury
    /// is credited once a file holds it, so a crash that rolls the gold back never leaves the fee paid twice. See <c>CraftSessionLifecycle</c>.
    /// </summary>
    public class PendingStationFee
    {
        /// <summary>The fee's id: the craft session's, or a quick craft item's. The station records it once credited (<see cref="WorldProperty.PaidCraftFees"/>).</summary>
        public string Id { get; set; }

        /// <summary>The station property whose treasury takes the fee.</summary>
        public string PropertyId { get; set; }

        public int Fee { get; set; }

        /// <summary>The craft file sequence the fee left the player at. A character file whose sequence is at least this holds the payment.</summary>
        public int Sequence { get; set; }
    }
}
