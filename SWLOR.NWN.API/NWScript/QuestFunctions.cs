namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Adds a journal quest entry to the creature.
        /// </summary>
        /// <param name="szPlotID">The plot identifier used in the toolset's Journal Editor</param>
        /// <param name="nState">The state of the plot as seen in the toolset's Journal Editor</param>
        /// <param name="oCreature">The creature to add the journal entry to</param>
        /// <param name="bAllPartyMembers">If TRUE, the entry will show up in the journal of everyone in the party</param>
        /// <param name="bAllPlayers">If TRUE, the entry will show up in the journal of everyone in the world</param>
        /// <param name="bAllowOverrideHigher">If TRUE, you can set the state to a lower number than the one it is currently on</param>
        public static void AddJournalQuestEntry(string szPlotID, int nState, uint oCreature,
            bool bAllPartyMembers = true, bool bAllPlayers = false, bool bAllowOverrideHigher = false)
        {
            global::NWN.Core.NWScript.AddJournalQuestEntry(szPlotID, nState, oCreature, bAllPartyMembers ? 1 : 0, bAllPlayers ? 1 : 0, bAllowOverrideHigher ? 1 : 0);
        }

        /// <summary>
        /// Removes a journal quest entry from the creature.
        /// </summary>
        /// <param name="szPlotID">The plot identifier used in the toolset's Journal Editor</param>
        /// <param name="oCreature">The creature to remove the journal entry from</param>
        /// <param name="bAllPartyMembers">If TRUE, the entry will be removed from the journal of everyone in the party</param>
        /// <param name="bAllPlayers">If TRUE, the entry will be removed from the journal of everyone in the world</param>
        public static void RemoveJournalQuestEntry(string szPlotID, uint oCreature, bool bAllPartyMembers = true,
            bool bAllPlayers = false)
        {
            global::NWN.Core.NWScript.RemoveJournalQuestEntry(szPlotID, oCreature, bAllPartyMembers ? 1 : 0, bAllPlayers ? 1 : 0);
        }
    }
}