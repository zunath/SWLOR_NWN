using System;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>
    /// What a feature does with the database side of its transfers. The feature builds the entry, hands it to the journal, and registers
    /// one handler per kind (<see cref="TransferJournal"/>).
    /// </summary>
    public sealed class TransferHandler
    {
        /// <param name="kind">The kind the feature's entries carry.</param>
        /// <param name="isApplied">Whether the database holds the entry's change: its receipt is saved, or the record it removes is gone.</param>
        /// <param name="apply">
        /// Makes the change, saving the receipt in the same write. Safe to call again for an entry that is applied. A change of several
        /// records writes its idempotent parts first and the record that carries the receipt last.
        /// </param>
        /// <param name="canApply">A deposit: whether the target can still take the entry's items. Leave null for a withdrawal.</param>
        public TransferHandler(string kind, Func<TransferEntry, bool> isApplied, Action<TransferEntry> apply, Func<TransferEntry, bool> canApply = null)
        {
            Kind = kind;
            IsApplied = isApplied;
            Apply = apply;
            CanApply = canApply ?? (_ => true);
        }

        public string Kind { get; }

        public Func<TransferEntry, bool> IsApplied { get; }

        public Func<TransferEntry, bool> CanApply { get; }

        public Action<TransferEntry> Apply { get; }
    }
}
