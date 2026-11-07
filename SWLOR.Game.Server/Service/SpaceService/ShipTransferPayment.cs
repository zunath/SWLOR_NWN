using System.Globalization;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The credits a player paid for a ship to change owner (a market purchase, or a gift's transfer fee), saved in the
    /// payer's character file with the debit before the transfer is saved. A restart between the two leaves a mark the
    /// next login settles: a transfer that was saved keeps the payment, one that never was returns the credits. Whether
    /// it was saved is read from the payment's durable acknowledgement, never from who owns the ship now.
    /// </summary>
    public sealed class ShipTransferPayment
    {
        public string Id { get; init; }
        public string ShipId { get; init; }

        /// <summary>The player the payment makes the ship's owner: the buyer, or a gift's recipient.</summary>
        public string NewOwnerPlayerId { get; init; }

        public int Credits { get; init; }

        public string Serialize() =>
            string.Join("|", Id, ShipId, NewOwnerPlayerId, Credits.ToString(CultureInfo.InvariantCulture));

        public static bool TryParse(string value, out ShipTransferPayment payment)
        {
            payment = null;
            var parts = (value ?? string.Empty).Split('|');
            if (parts.Length != 4 ||
                string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1]) ||
                string.IsNullOrWhiteSpace(parts[2]) ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var credits) ||
                credits < 0)
            {
                return false;
            }

            payment = new ShipTransferPayment { Id = parts[0], ShipId = parts[1], NewOwnerPlayerId = parts[2], Credits = credits };
            return true;
        }

        /// <summary>
        /// Whether the transfer this payment bought was saved: its <see cref="ShipTransferReceipt"/> exists (written as
        /// the transfer completes and kept until the payer's mark is retired), or the ship's saved transfer intent still
        /// names this payment (the module load finishes it and writes the receipt). Who owns the ship now does not
        /// matter: the new owner may already have passed it on.
        /// </summary>
        public bool IsSettledBy(ShipTransferReceipt receipt, PlayerShip ship) =>
            !string.IsNullOrWhiteSpace(Id) &&
            (receipt?.Id == Id || (ship != null && ship.TransferPaymentId == Id));
    }
}
