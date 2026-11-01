namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The acknowledgement that a paid ship transfer (a market purchase or a gift's fee) was completed, keyed by the
    /// payment's id (<c>ShipTransferPayment</c>). It is written as the transfer completes, before the transfer intent is
    /// cleared from the ship, and deleted only after the payer's character-file mark is retired, so a login that finds
    /// the mark keeps the payment whatever has happened to the ship since. A new type, so no migration.
    /// </summary>
    public class ShipTransferReceipt : EntityBase
    {
        public ShipTransferReceipt()
        {
            ShipId = string.Empty;
            NewOwnerPlayerId = string.Empty;
        }

        public string ShipId { get; set; }

        public string NewOwnerPlayerId { get; set; }
    }
}
