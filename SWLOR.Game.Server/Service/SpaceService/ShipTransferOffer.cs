namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A direct ship transfer waiting for its recipient's answer (plan section 7). Held in memory only.</summary>
    public class ShipTransferOffer
    {
        public string ShipId { get; set; }
        public string GiverPlayerId { get; set; }
        public string RecipientPlayerId { get; set; }
        public int Fee { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
