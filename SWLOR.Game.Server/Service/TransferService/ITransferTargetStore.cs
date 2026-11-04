using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>
    /// The entries that name a database record and the record's receipts, for settling them before the record is deleted
    /// (<see cref="TransferJournalProcess.SettleReceipts"/>). <see cref="TransferJournal"/> implements it over the real records.
    /// </summary>
    public interface ITransferTargetStore
    {
        /// <summary>The entries naming the record that are not yet saved as applied.</summary>
        IReadOnlyList<TransferEntry> GetEntriesOf(string targetId);

        /// <summary>Whether the record holds the entry's receipt.</summary>
        bool IsApplied(TransferEntry entry);

        /// <summary>Saves the entry as applied (<see cref="TransferEntry.AppliedUtc"/>).</summary>
        void MarkApplied(TransferEntry entry);
    }
}
