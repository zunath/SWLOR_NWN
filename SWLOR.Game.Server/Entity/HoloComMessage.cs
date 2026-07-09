namespace SWLOR.Game.Server.Entity
{
    public class HoloComMessage: EntityBase
    {
        public string SenderPlayerId { get; set; }
        public string SenderFallbackName { get; set; }

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
            RecipientPlayerId = string.Empty;
            IsRead = false;
            Text = string.Empty;
            SenderSnapshotJson = string.Empty;
            SentDateTicks = DateTime.UtcNow.Ticks;
        }
    }
}
