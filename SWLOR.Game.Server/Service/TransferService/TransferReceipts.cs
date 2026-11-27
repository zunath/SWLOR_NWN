using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>
    /// The receipts a database record keeps of the journal entries it applied. A receipt is saved in the same write as the change it
    /// stands for, so a record that holds an entry's id has applied it and one that does not has not. Only the newest are kept: an
    /// entry is settled at its character's next login, long before this many newer transfers touch one record.
    /// </summary>
    public static class TransferReceipts
    {
        public const int Capacity = 16;

        /// <summary>Whether the receipts name the entry.</summary>
        public static bool Has(IEnumerable<string> receipts, string entryId)
        {
            if (receipts == null || string.IsNullOrWhiteSpace(entryId))
                return false;

            foreach (var receipt in receipts)
            {
                if (receipt == entryId)
                    return true;
            }

            return false;
        }

        /// <summary>Adds the entry's receipt once, dropping the oldest beyond <see cref="Capacity"/>. Returns the list, made if there was none.</summary>
        public static List<string> Add(List<string> receipts, string entryId)
        {
            receipts ??= new List<string>();
            if (string.IsNullOrWhiteSpace(entryId) || receipts.Contains(entryId))
                return receipts;

            receipts.Add(entryId);
            while (receipts.Count > Capacity)
            {
                receipts.RemoveAt(0);
            }

            return receipts;
        }
    }
}
