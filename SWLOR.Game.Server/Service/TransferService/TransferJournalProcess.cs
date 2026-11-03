using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>
    /// The steps of a journaled transfer, in the order that makes a restart safe, the way a City Project donation is ordered
    /// (<see cref="CityProjectService.CityProjectDonationProcess"/>).
    /// <para>
    /// A deposit (items leave the character, the database gains them): the entry is saved; the items are taken and the character file is
    /// saved without them and with a mark naming the entry; the target applies the deposit and saves its receipt in the same write; the
    /// entry is saved as applied; the entry goes; the mark goes.
    /// </para>
    /// <para>
    /// A withdrawal (the database gives something up, the character gains items): the entry is saved; the target applies the change and
    /// saves its receipt in the same write; the entry is saved as applied; the items are given and the character file is saved with them
    /// and a mark naming the entry; the entry goes; the mark goes.
    /// </para>
    /// A restart after any step is settled at the character's next login by <see cref="SettleAtLogin"/>, which reads the three places
    /// and repeats only the steps that did not finish (<see cref="TransferJournalRules.Settle"/>). An entry saved as applied needs no
    /// look at the target, which may have been deleted since; the target's own receipt covers only the instant between its write and
    /// the entry's.
    /// </summary>
    public static class TransferJournalProcess
    {
        /// <summary>Moves the entry's items from the character to the target.</summary>
        /// <returns>
        /// <see cref="TransferSettlement.Apply"/> when the target took them, <see cref="TransferSettlement.ToOverflow"/> when it could not and
        /// they went to the character's overflow store, and <see cref="TransferSettlement.Declined"/> when the items could not be taken or the
        /// character file could not be saved, so nothing was changed.
        /// </returns>
        public static TransferSettlement Deposit(ITransferStore store, TransferEntry entry)
        {
            store.SavePending(entry);
            if (!store.TakeAndMark(entry))
            {
                store.DeletePending(entry.Id);
                return TransferSettlement.Declined;
            }

            return Settle(store, entry, marked: true);
        }

        /// <summary>Applies the change to the target and gives the entry's items to the character.</summary>
        /// <returns>
        /// <see cref="TransferSettlement.Forget"/> when the character holds the items (in their inventory, with the rest in overflow), and
        /// <see cref="TransferSettlement.ToOverflow"/> when the character file could not be saved, so everything went to their overflow store.
        /// </returns>
        public static TransferSettlement Withdraw(ITransferStore store, TransferEntry entry)
        {
            store.SavePending(entry);
            store.Apply(entry);
            store.MarkApplied(entry);
            if (store.GiveAndMark(entry))
            {
                store.DeletePending(entry.Id);
                store.ClearMark(entry.Id);
                return TransferSettlement.Forget;
            }

            store.ToOverflow(entry);
            store.DeletePending(entry.Id);
            return TransferSettlement.ToOverflow;
        }

        /// <summary>
        /// Settles what a restart left behind at a character's login: every entry they have, by the mark in their character file, then every
        /// mark that has no entry, which a transfer that finished leaves when the restart came before its mark was cleared.
        /// </summary>
        /// <param name="store">The records and the character.</param>
        /// <param name="markedIds">The entry ids in the character file's mark.</param>
        /// <param name="entries">The character's entries.</param>
        /// <returns>What was done with each entry.</returns>
        public static List<(TransferEntry Entry, TransferSettlement Settlement)> SettleAtLogin(
            ITransferStore store,
            IReadOnlyCollection<string> markedIds,
            IReadOnlyList<TransferEntry> entries)
        {
            var outcomes = new List<(TransferEntry, TransferSettlement)>();
            foreach (var entry in entries)
            {
                outcomes.Add((entry, Settle(store, entry, markedIds.Contains(entry.Id))));
            }

            var recorded = entries.Select(entry => entry.Id).ToHashSet();
            foreach (var id in markedIds.Where(id => !recorded.Contains(id)))
            {
                store.ClearMark(id);
            }

            return outcomes;
        }

        private static TransferSettlement Settle(ITransferStore store, TransferEntry entry, bool marked)
        {
            var applied = entry.AppliedUtc.HasValue || store.IsApplied(entry);
            var targetTakes = entry.Direction == TransferDirection.Deposit && marked && !applied && store.CanApply(entry);
            var settlement = TransferJournalRules.Settle(entry.Direction, marked, applied, targetTakes);
            switch (settlement)
            {
                case TransferSettlement.Drop:
                    store.DeletePending(entry.Id);
                    break;
                case TransferSettlement.Forget:
                    store.DeletePending(entry.Id);
                    store.ClearMark(entry.Id);
                    break;
                case TransferSettlement.Apply:
                    store.Apply(entry);
                    store.MarkApplied(entry);
                    store.DeletePending(entry.Id);
                    store.ClearMark(entry.Id);
                    break;
                case TransferSettlement.ToOverflow:
                    store.ToOverflow(entry);
                    store.DeletePending(entry.Id);
                    if (marked)
                        store.ClearMark(entry.Id);
                    break;
            }

            return settlement;
        }
    }
}
