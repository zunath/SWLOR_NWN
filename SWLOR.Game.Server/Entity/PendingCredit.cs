namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Credits owed to a player who may be offline (a tournament prize, or an entry fee returned when a tournament is
    /// cancelled or its city is removed). Paid at the player's next login, or at once when they are online. The id is
    /// chosen by whatever owes the credits, so writing the same debt twice leaves one record. A new type, so no migration.
    /// </summary>
    public class PendingCredit : EntityBase
    {
        public PendingCredit()
        {
            PlayerId = string.Empty;
            Reason = string.Empty;
        }

        [Indexed]
        public string PlayerId { get; set; }

        public int Credits { get; set; }

        /// <summary>What the credits are for, as the player is told.</summary>
        public string Reason { get; set; }

        public DateTime CreatedUtc { get; set; }
    }
}
