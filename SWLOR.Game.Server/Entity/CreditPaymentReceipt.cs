namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The proof that a credit payment's change was saved, kept after the record that held the proof is gone. A tournament entrant names
    /// the payment that paid their entry fee, but settling the tournament deletes it, so the settlement first writes one receipt per paid
    /// entrant under the payment's id. A player whose restart left that payment's mark in the character file finds the receipt at their
    /// next login and keeps the payment: the settlement already returned or kept the fee. The id is the payment's, so writing it twice
    /// leaves one record. A player's receipts are deleted at their next login. A new type, so no migration.
    /// </summary>
    public class CreditPaymentReceipt : EntityBase
    {
        public CreditPaymentReceipt()
        {
            PlayerId = string.Empty;
        }

        [Indexed]
        public string PlayerId { get; set; }

        public DateTime CreatedUtc { get; set; }
    }
}
