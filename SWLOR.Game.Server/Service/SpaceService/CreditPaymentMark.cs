using System.Globalization;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A credit payment for a change the database saves (founding a station, a hull refit, a customs fine), written in
    /// the payer's character file with the debit and retired, with another export, once the change is saved. A restart
    /// in between leaves a mark the next login settles: the payment is kept when the change was saved (the changed record
    /// names the payment) and returned when it was not. The mark is retired as soon as the change is saved, so a later
    /// change to the same record never makes an old mark look unsettled.
    /// </summary>
    public sealed class CreditPaymentMark
    {
        public string Id { get; init; }
        public CreditPaymentKind Kind { get; init; }

        /// <summary>The record the change is saved in (the station or ship), or empty when the payment changes no record.</summary>
        public string RecordId { get; init; }

        public int Credits { get; init; }

        public string Serialize() =>
            string.Join("|", Id, ((int)Kind).ToString(CultureInfo.InvariantCulture), RecordId ?? string.Empty,
                Credits.ToString(CultureInfo.InvariantCulture));

        public static bool TryParse(string value, out CreditPaymentMark mark)
        {
            mark = null;
            var parts = (value ?? string.Empty).Split('|');
            if (parts.Length != 4 ||
                string.IsNullOrWhiteSpace(parts[0]) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kind) ||
                kind <= 0 ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var credits) ||
                credits < 0)
            {
                return false;
            }

            mark = new CreditPaymentMark { Id = parts[0], Kind = (CreditPaymentKind)kind, RecordId = parts[2], Credits = credits };
            return true;
        }

        /// <summary>
        /// Whether a login keeps the payment. A station is founded before it is paid for, so its payment is always kept
        /// (an unpaid founding is undone instead, <see cref="PaysPendingFounding"/>). A payment that changes no record is
        /// complete once the character file holds it. Any other is kept only when its record names it
        /// (<paramref name="recordLastPaymentId"/>), saved with the change.
        /// </summary>
        public bool KeepsPayment(string recordLastPaymentId) =>
            Kind == CreditPaymentKind.StationFounding ||
            string.IsNullOrWhiteSpace(RecordId) ||
            recordLastPaymentId == Id;

        /// <summary>Whether this mark pays for the station founding still pending under <paramref name="pendingPaymentId"/>.</summary>
        public static bool PaysPendingFounding(CreditPaymentMark mark, string pendingPaymentId) =>
            mark != null && mark.Kind == CreditPaymentKind.StationFounding &&
            !string.IsNullOrWhiteSpace(pendingPaymentId) && mark.Id == pendingPaymentId;
    }
}
