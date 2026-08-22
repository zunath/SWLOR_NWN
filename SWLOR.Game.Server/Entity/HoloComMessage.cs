namespace SWLOR.Game.Server.Entity
{
    public class HoloComMessage: EntityBase
    {
        [Indexed]
        public string SenderPlayerId { get; set; }
        public string SenderFallbackName { get; set; }

        /// <summary>
        /// The sender's identity as observers would perceive it at send time
        /// (Disguise.GetIdentityKey): the disguise identity when disguised, otherwise
        /// the sender's player id. Recipient-facing name resolution keys off this so
        /// a disguised sender's real identity never leaks through a message.
        /// </summary>
        public string SenderIdentityKey { get; set; }

        /// <summary>
        /// The sender's unknown-display descriptor at send time
        /// (Disguise.GetDisplayDescriptor) - the disguise descriptor when disguised,
        /// otherwise the sender's generated descriptor.
        /// </summary>
        public string SenderDescriptor { get; set; }

        /// <summary>
        /// The language (SkillType) the sender had active at send time. Stamped onto
        /// the playback hologram so recordings speak the language they were recorded
        /// in, even when the sender is offline.
        /// </summary>
        public int SenderLanguage { get; set; }

        [Indexed]
        public string RecipientPlayerId { get; set; }

        [Indexed]
        public bool IsRead { get; set; }

        /// <summary>
        /// Whether the recipient has saved this message, exempting it from the
        /// retention cleanup that otherwise deletes expired messages.
        /// </summary>
        [Indexed]
        public bool IsSaved { get; set; }

        public string Text { get; set; }
        public string SenderSnapshotJson { get; set; }

        [Indexed]
        public long SentDateTicks { get; set; }

        /// <summary>
        /// When this message becomes eligible for retention cleanup. Deliberately not
        /// indexed - the DB layer only supports exact-match field searches, not numeric
        /// ranges, so cleanup queries by IsRead/IsSaved and filters expiration in memory.
        /// </summary>
        public long ExpirationDateTicks { get; set; }

        public HoloComMessage()
        {
            SenderPlayerId = string.Empty;
            SenderFallbackName = string.Empty;
            SenderIdentityKey = string.Empty;
            SenderDescriptor = string.Empty;
            SenderLanguage = 0;
            RecipientPlayerId = string.Empty;
            IsRead = false;
            IsSaved = false;
            Text = string.Empty;
            SenderSnapshotJson = string.Empty;
            SentDateTicks = DateTime.UtcNow.Ticks;
            ExpirationDateTicks = 0;
        }
    }
}
