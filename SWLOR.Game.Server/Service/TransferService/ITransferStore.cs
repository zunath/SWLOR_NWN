using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>
    /// The records, the character and the feature's database side that a journaled transfer touches. <see cref="TransferJournalProcess"/>
    /// calls them in a fixed order, so the order can be tested without a database or an engine; <see cref="TransferJournal"/> implements
    /// them over the real records and the feature's <see cref="TransferHandler"/>.
    /// </summary>
    public interface ITransferStore
    {
        /// <summary>Saves the entry.</summary>
        void SavePending(TransferEntry entry);

        /// <summary>Deletes the entry. Deleting one that is gone changes nothing.</summary>
        void DeletePending(string entryId);

        /// <summary>
        /// A deposit: takes the entry's items out of the character's inventory, adds the entry's id to the mark in the character file, and
        /// saves the character file. Returns false, having changed nothing, when the character no longer carries them or the file could
        /// not be saved.
        /// </summary>
        bool TakeAndMark(TransferEntry entry);

        /// <summary>
        /// A withdrawal: puts the entry's items into the character's inventory while there is room and the rest into their overflow store
        /// (under ids made from the entry's, so writing them again changes nothing), adds the entry's id to the mark, and saves the
        /// character file. Returns false, with the items taken out of the inventory again and nothing marked, when the file could not be saved.
        /// </summary>
        bool GiveAndMark(TransferEntry entry);

        /// <summary>
        /// Removes the entry's id from the mark in the character file. The file need not be saved at once: a restart before its next save
        /// leaves a mark with no entry, which the next login clears.
        /// </summary>
        void ClearMark(string entryId);

        /// <summary>Whether the database side holds the entry's change (its receipt is saved).</summary>
        bool IsApplied(TransferEntry entry);

        /// <summary>A deposit: whether the target can still take everything in the entry.</summary>
        bool CanApply(TransferEntry entry);

        /// <summary>Makes the database side's change and saves its receipt in the same write as the change. Applying what is applied changes nothing.</summary>
        void Apply(TransferEntry entry);

        /// <summary>
        /// Saves the entry as applied (<see cref="TransferEntry.AppliedUtc"/>), so it stays applied even if the record that carried the
        /// receipt is deleted before the character's next login.
        /// </summary>
        void MarkApplied(TransferEntry entry);

        /// <summary>
        /// Writes everything in the entry to the character's overflow store under ids made from the entry's, so doing it twice, or after a
        /// part of it was written already, writes the same records.
        /// </summary>
        void ToOverflow(TransferEntry entry);
    }
}
