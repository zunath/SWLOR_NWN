namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>What happens to one journal entry, from the entry, the character file's mark and the database (<see cref="TransferJournalRules.Settle"/>).</summary>
    public enum TransferSettlement
    {
        /// <summary>
        /// Nothing moved: a deposit whose items never left the character file, or a withdrawal the database never made. The entry goes.
        /// </summary>
        Drop = 1,

        /// <summary>Both sides are done: the entry and the mark go.</summary>
        Forget = 2,

        /// <summary>A deposit whose items left the character file and whose target can still take them: the target gets them, then the entry and the mark go.</summary>
        Apply = 3,

        /// <summary>
        /// The items are owed to the character and are not in its file: a deposit whose target can no longer take them, or a withdrawal
        /// the database made and the file never saved. They go to the character's overflow store.
        /// </summary>
        ToOverflow = 4,

        /// <summary>The character file could not be saved, so nothing was changed. Only a fresh transfer reports this.</summary>
        Declined = 5
    }
}
