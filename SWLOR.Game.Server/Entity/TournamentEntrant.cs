namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A player who signed up for a tournament, kept on the tournament by player id.
    /// </summary>
    public class TournamentEntrant
    {
        public TournamentEntrant()
        {
            PlayerId = string.Empty;
            Label = string.Empty;
            PaymentId = string.Empty;
        }

        public string PlayerId { get; set; }

        /// <summary>How strangers see the entrant, for the Gazette and for players who are not in the ring. Kept fresh while they are online.</summary>
        public string Label { get; set; }

        /// <summary>The entry fee the entrant paid. It is also held in the city's escrow entry for the tournament.</summary>
        public long FeePaid { get; set; }

        /// <summary>The credit payment that paid the fee, so a restart in the middle of signing up can tell whether the sign-up was saved.</summary>
        public string PaymentId { get; set; }

        public DateTime SignedUpUtc { get; set; }
    }
}
