namespace SWLOR.Game.Server.Entity
{
    public class HoloComMessage: EntityBase
    {
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

        public string Text { get; set; }
        public string SenderSnapshotJson { get; set; }

        [Indexed]
        public long SentDateTicks { get; set; }

        public HoloComMessage()
        {
            SenderPlayerId = string.Empty;
            SenderFallbackName = string.Empty;
            SenderIdentityKey = string.Empty;
            SenderDescriptor = string.Empty;
            SenderLanguage = 0;
            RecipientPlayerId = string.Empty;
            IsRead = false;
            Text = string.Empty;
            SenderSnapshotJson = string.Empty;
            SentDateTicks = DateTime.UtcNow.Ticks;
        }
    }
}
