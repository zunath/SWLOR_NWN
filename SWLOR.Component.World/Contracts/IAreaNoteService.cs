namespace SWLOR.Component.World.Contracts
{
    public interface IAreaNoteService
    {
        /// <summary>
        /// Displays area notes for the specified area to the specified player.
        /// </summary>
        /// <param name="player">The player to display notes to</param>
        /// <param name="area">The area to get notes for</param>
        void DisplayAreaNotes(uint player, uint area);
    }
}
