namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Represents a journal entry in the player's quest log.
    /// </summary>
    public class JournalEntry
    {
        /// <summary>
        /// Gets or sets the name of the journal entry.
        /// </summary>
        public string Name{ get; set; }
        
        /// <summary>
        /// Gets or sets the text content of the journal entry.
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Gets or sets the tag associated with the journal entry.
        /// </summary>
        public string Tag { get; set; }
        
        /// <summary>
        /// Gets or sets the state of the journal entry.
        /// </summary>
        public int State { get; set; }
        
        /// <summary>
        /// Gets or sets the priority of the journal entry.
        /// </summary>
        public int Priority{ get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the quest is completed.
        /// </summary>
        public bool IsQuestCompleted { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the quest is displayed in the journal.
        /// </summary>
        public bool IsQuestDisplayed { get; set; }
        
        /// <summary>
        /// Gets or sets the last updated timestamp.
        /// </summary>
        public int Updated { get; set; }
        
        /// <summary>
        /// Gets or sets the calendar day when the entry was created.
        /// </summary>
        public int CalendarDay{ get; set; }
        
        /// <summary>
        /// Gets or sets the time of day when the entry was created.
        /// </summary>
        public int TimeOfDay{ get; set; }
    }
}
