namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Add a journal quest entry to oCreature.
        ///   - szPlotID: the plot identifier used in the toolset's Journal Editor
        ///   - nState: the state of the plot as seen in the toolset's Journal Editor
        ///   - oCreature
        ///   - bAllPartyMembers: If this is TRUE, the entry will show up in the journal of
        ///   everyone in the party
        ///   - bAllPlayers: If this is TRUE, the entry will show up in the journal of
        ///   everyone in the world
        ///   - bAllowOverrideHigher: If this is TRUE, you can set the state to a lower
        ///   number than the one it is currently on
        /// </summary>
        public static void AddJournalQuestEntry(string szPlotID, int nState, uint oCreature,
            bool bAllPartyMembers = true, bool bAllPlayers = false, bool bAllowOverrideHigher = false)
        {
            global::NWN.Core.NWScript.AddJournalQuestEntry(szPlotID, nState, oCreature, bAllPartyMembers ? 1 : 0, bAllPlayers ? 1 : 0, bAllowOverrideHigher ? 1 : 0);
        }

        /// <summary>
        ///   Remove a journal quest entry from oCreature.
        ///   - szPlotID: the plot identifier used in the toolset's Journal Editor
        ///   - oCreature
        ///   - bAllPartyMembers: If this is TRUE, the entry will be removed from the
        ///   journal of everyone in the party
        ///   - bAllPlayers: If this is TRUE, the entry will be removed from the journal of
        ///   everyone in the world
        /// </summary>
        public static void RemoveJournalQuestEntry(string szPlotID, uint oCreature, bool bAllPartyMembers = true,
            bool bAllPlayers = false)
        {
            global::NWN.Core.NWScript.RemoveJournalQuestEntry(szPlotID, oCreature, bAllPartyMembers ? 1 : 0, bAllPlayers ? 1 : 0);
        }
    }
}